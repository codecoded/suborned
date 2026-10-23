// suborned-sim batch: many scripts in one process (Splice, brief 042). `play <script> --json` pays a process start and Engine.Load (about 0.3 s)
// before it plays anything, and replays every game a second time to check it; a robustness set of tens of thousands of scripts paid that
// each time. Here the inputs load once and the plays run on every core, each on its own Game (the inputs are read-only once loaded; the
// engine keeps its per-thread scratch ThreadStatic, so no play can see another). The output of one script is exactly the line
// `play <script> --json` prints (same bytes), so a batch is a faster `play`, not a different one.
//
//   batch <path | dir | scenario id | -> ...  [--list f] [--no-replay] [--jobs n] [--out f]
//       items: a script file, a directory (its *.json, sorted by name), a scenario id, `-` for paths on stdin (one a line), `--list f` for
//       paths in a file (one a line; blank lines and # lines skipped). Output: one JSON line per item, in item order, to stdout or --out.
//       A script that cannot be played gives {"id":..,"error":..} and the batch goes on. Exit 0 when every item played, passed its checks and
//       replayed identically; 1 otherwise (a count on stderr).
//       --no-replay leaves out the second, self-checking run of each game (about half the sim cost; see briefs/042-fast-checks.md): the line
//       then reads "replayIdentical": null. The default checks every play, as `play` does.
//       --jobs n: worker threads (default every core; env SUBORNED_SIM_JOBS).
//   batch --serve
//       the same as a server for a caller that asks in rounds (the mission composer): stdin lines `tag<TAB>replay|noreplay<TAB>path`, stdout
//       lines `tag<TAB>json` as each play finishes (any order). The server ends at the end of stdin.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Suborned.Sim;

static class BatchCli
{
    const int StackBytes = 64 << 20; // dedicated workers get a deep stack (a pool thread's is the OS default, 512 KB on macOS)

    /// <summary>One play: a scenario id or an order script, played to its end; with `replay` the game is replayed from its own orders and compared
    /// checkpoint by checkpoint (Same), which costs a second run of the sim (Splice, brief 042: `batch --no-replay` leaves it out).</summary>
    internal sealed class Played { public string Id = ""; public Game G = null!; public List<(bool ok, string what)> Checks = new List<(bool, string)>(); public bool Same; public bool Replayed; public bool Passed => Checks.TrueForAll(c => c.ok); }
    internal static Played PlayOne(Engine.Inputs inputs, string root, string what, bool replay = true)
    {
        var p = new Played { Replayed = replay }; string record = "";
        var sc = Scenarios.Find(what);
        if (sc != null) { var run = Scenarios.Run(inputs, sc); p.G = run.G; p.Checks = run.Checks; p.Id = sc.Id; }
        else
        {
            var path = File.Exists(what) ? what : System.IO.Path.Combine(root, what);
            var script = Script.Parse(File.ReadAllText(path));
            p.G = Engine.Play(inputs, script.Start, script.Orders, script.Until); p.Id = System.IO.Path.GetFileName(path);
            if (script.Expect != null) { record = script.Verify(p.G, inputs.Hash); p.Checks.Add((record.Length == 0, record.Length == 0 ? "the replay matches its record (inputs, engine, start, every checkpoint, outcome)" : "record: " + record)); }
        }
        p.Same = replay ? Same(p.G, Engine.Replay(inputs, p.G)) : true;
        return p;
    }

    internal static bool Same(Game a, Game b)
    {
        if (a.Checkpoints.Count != b.Checkpoints.Count) return false;
        for (int i = 0; i < a.Checkpoints.Count; i++) if (a.Checkpoints[i] != b.Checkpoints[i]) return false;
        return true;
    }

    /// <summary>The line `play what --json` prints, or an error line; `ok` when it passed its checks and replayed identically.</summary>
    static string Line(Engine.Inputs inputs, string root, string what, bool replay, out bool ok)
    {
        try
        {
            var p = PlayOne(inputs, root, what, replay);
            ok = p.Passed && p.Same;
            var line = Report.Json(p.Id, p.G, p.Checks, p.Same);
            return replay ? line : Swap(line, "\"replayIdentical\":true", "\"replayIdentical\":null"); // not checked is not "identical"
        }
        catch (Exception e)
        {
            ok = false;
            return new JWriter().Obj().Prop("id", System.IO.Path.GetFileName(what)).Prop("error", e.Message).EndObj().ToString();
        }
    }

    /// <summary>Report.Json writes replayIdentical once, before any free text; replace that one occurrence.</summary>
    static string Swap(string line, string was, string now)
    {
        int k = line.IndexOf(was, StringComparison.Ordinal);
        if (k < 0) throw new InvalidOperationException("Report.Json no longer writes replayIdentical where batch expects it");
        return string.Concat(line.AsSpan(0, k), now, line.AsSpan(k + was.Length));
    }

    internal static int Jobs(string[] args)
    {
        int k = Array.IndexOf(args, "--jobs"); string? env = Environment.GetEnvironmentVariable("SUBORNED_SIM_JOBS");
        int n = k > 0 && k + 1 < args.Length ? int.Parse(args[k + 1], CultureInfo.InvariantCulture) : env != null ? int.Parse(env, CultureInfo.InvariantCulture) : Environment.ProcessorCount;
        return Math.Max(1, n);
    }

    public static int Run(Engine.Inputs inputs, string root, string[] args)
    {
        // the scenario lists build lazily on first use (not thread safe): build them now, before any worker starts
        _ = Scenarios.All.Count;
        int jobs = Jobs(args);
        return Array.IndexOf(args, "--serve") > 0 ? Serve(inputs, root, jobs) : Batch(inputs, root, args, jobs);
    }

    static List<string> Items(string[] args)
    {
        var items = new List<string>();
        for (int i = 1; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "--no-replay" || a == "--serve") continue;
            if (a == "--jobs" || a == "--out") { i++; continue; }
            if (a == "--list")
            {
                if (i + 1 >= args.Length) throw new ArgumentException("--list which file?");
                AddLines(items, File.ReadAllLines(args[++i])); continue;
            }
            if (a == "-") { string? l; var lines = new List<string>(); while ((l = Console.In.ReadLine()) != null) lines.Add(l); AddLines(items, lines); continue; }
            if (a.StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException($"batch: unknown option {a}");
            if (Directory.Exists(a)) { var fs = Directory.GetFiles(a, "*.json"); Array.Sort(fs, StringComparer.Ordinal); items.AddRange(fs); continue; }
            items.Add(a);
        }
        return items;
    }

    static void AddLines(List<string> items, IEnumerable<string> lines)
    {
        foreach (var raw in lines) { var l = raw.Trim(); if (l.Length > 0 && l[0] != '#') items.Add(l); }
    }

    static int Batch(Engine.Inputs inputs, string root, string[] args, int jobs)
    {
        var items = Items(args); bool replay = Array.IndexOf(args, "--no-replay") < 0;
        int oi = Array.IndexOf(args, "--out");
        using var file = oi > 0 ? new FileStream(args[oi + 1], FileMode.Create, FileAccess.Write, FileShare.Read, 1 << 16) : null;
        using var stream = file != null ? (Stream)file : Console.OpenStandardOutput();
        using var w = new StreamWriter(stream, new UTF8Encoding(false), 1 << 16) { NewLine = "\n", AutoFlush = false };
        int n = items.Count, claimed = -1, wrote = 0, bad = 0;
        var lines = new string?[n]; var oks = new bool[n]; var gate = new object(); var sw = Stopwatch.StartNew();
        var errors = new ConcurrentQueue<Exception>();
        void work()
        {
            try
            {
                while (true)
                {
                    int i = Interlocked.Increment(ref claimed); if (i >= n) return;
                    var line = Line(inputs, root, items[i], replay, out var ok);
                    lock (gate)
                    {
                        lines[i] = line; oks[i] = ok;
                        // write what is ready, in item order, and let go of it: memory stays bounded by the spread of the slowest in-flight plays
                        while (wrote < n && lines[wrote] != null) { w.Write(lines[wrote]); w.Write('\n'); if (!oks[wrote]) bad++; lines[wrote] = null; wrote++; }
                    }
                }
            }
            catch (Exception e) { errors.Enqueue(e); }
        }
        var ts = new Thread[Math.Min(jobs, Math.Max(1, n))];
        for (int k = 0; k < ts.Length; k++) { ts[k] = new Thread(work, StackBytes) { IsBackground = false }; ts[k].Start(); }
        foreach (var t in ts) t.Join();
        w.Flush();
        if (!errors.IsEmpty) throw errors.TryPeek(out var first) ? first : new InvalidOperationException("batch worker failed");
        Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"batch: {n} played in {sw.Elapsed.TotalSeconds:0.0} s on {ts.Length} threads{(replay ? "" : ", no replay check")}; {bad} not ok"));
        return bad == 0 ? 0 : 1;
    }

    static int Serve(Engine.Inputs inputs, string root, int jobs)
    {
        var q = new BlockingCollection<(string tag, string what, bool replay)>(jobs * 4);
        var outGate = new object();
        using var stdout = Console.OpenStandardOutput();
        using var w = new StreamWriter(stdout, new UTF8Encoding(false), 1 << 16) { NewLine = "\n", AutoFlush = false };
        void work()
        {
            foreach (var (tag, what, replay) in q.GetConsumingEnumerable())
            {
                var line = Line(inputs, root, what, replay, out _);
                lock (outGate) { w.Write(tag); w.Write('\t'); w.Write(line); w.Write('\n'); w.Flush(); }
            }
        }
        var ts = new Thread[jobs];
        for (int k = 0; k < jobs; k++) { ts[k] = new Thread(work, StackBytes); ts[k].Start(); }
        string? l;
        while ((l = Console.In.ReadLine()) != null)
        {
            if (l.Length == 0) continue;
            var f = l.Split('\t', 3);
            if (f.Length != 3) { lock (outGate) { w.Write(f[0]); w.Write('\t'); w.Write(new JWriter().Obj().Prop("id", "").Prop("error", "serve: want tag<TAB>replay|noreplay<TAB>path").EndObj().ToString()); w.Write('\n'); w.Flush(); } continue; }
            q.Add((f[0], f[2], f[1] != "noreplay"));
        }
        q.CompleteAdding();
        foreach (var t in ts) t.Join();
        return 0;
    }
}

/// <summary>Independent jobs on dedicated worker threads (a deep stack each), results in job order; a job's exception comes back as its result,
/// for the caller to throw where the serial loop would have (fight-lab, Splice brief 042).</summary>
static class Par
{
    public static (T? value, Exception? error)[] Map<T>(int n, int jobs, Func<int, T> f)
    {
        var res = new (T? value, Exception? error)[n]; int next = -1;
        void work() { while (true) { int i = Interlocked.Increment(ref next); if (i >= n) return; try { res[i] = (f(i), null); } catch (Exception e) { res[i] = (default, e); } } }
        var ts = new Thread[Math.Max(1, Math.Min(jobs, n))];
        for (int k = 0; k < ts.Length; k++) { ts[k] = new Thread(work, 64 << 20); ts[k].Start(); }
        foreach (var t in ts) t.Join();
        return res;
    }
}
