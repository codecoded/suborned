// Suture's QA host (brief 060, tier 1, day 1). Plays order scripts on the engine library exactly as Engine.Play does (the same order sort,
// the same RunTo and Issue sequence), on every core, and prints per script the line `suborned-sim play <script> --json` prints, plus one
// extra key "qa" with read-only observations the play JSON lacks today:
//   debrief   Debrief.Of(g) on every ending (the play JSON writes it on a success only; HandoverHost.cs builds it on every ending, so a fail
//             card's claims come from here)
//   record    "" when the script's own record (expect) matches this engine, else the first difference ("none" when the script has no record)
//   stuck     intervals of at least --stuck-min-s where an operative had an open order (a walk, a walk-in act, an act past its landing) and
//             did not move; sampled every second of game time
//   vale      Vale's state changes (tick, state)
//   seen      the first tick each hostile is Present on the street, and where
//   lockdown  each change of the seal tick (tick, lockdown tick)
// Nothing here is hashed or read by a rule; the replay check (Engine.Replay against this play) shows the sampling moved nothing.
// A stopgap until Keel's `batch --trace` (brief 060 §1.3): the stuck and spawn samples move to the trace when it lands.
//
// usage: suborned-qa --about | --list <file> --out <file | ->  [--jobs n] [--no-replay] [--stuck-min-s s] [--inputs-dir d]
//        inputs: the repo's (Engine.Load, the engine's root at or above the cwd), or --inputs-dir: a built player's StreamingAssets/sim
//        (rulebook.json, people.rules.json, handover.site.json, handover.people.json and the optional provisional, takedown, rounds)
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Suborned.Sim;

static class QaHost
{
    static int stuckMinTicks = 50;

    static int Main(string[] args)
    {
        try
        {
            var dir = Arg(args, "--inputs-dir");
            var inputs = dir != null ? FromDir(dir) : Engine.Load(Engine.FindRoot(Directory.GetCurrentDirectory()));
            if (Array.IndexOf(args, "--about") >= 0) { Console.WriteLine(new JWriter().Obj().Prop("engine", Engine.Version).Prop("mvid", Engine.Mvid).Prop("inputs", Hash.Hex(inputs.Hash)).EndObj().ToString()); return 0; }
            string? list = Arg(args, "--list"), outPath = Arg(args, "--out");
            if (list == null || outPath == null) { Console.Error.WriteLine("usage: suborned-qa --list <file> --out <file> [--jobs n] [--no-replay] [--stuck-min-s s]"); return 2; }
            bool replay = Array.IndexOf(args, "--no-replay") < 0;
            int jobs = int.Parse(Arg(args, "--jobs") ?? Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            stuckMinTicks = Game.T(int.Parse(Arg(args, "--stuck-min-s") ?? "5", CultureInfo.InvariantCulture));
            var items = new List<string>();
            foreach (var raw in File.ReadAllLines(list)) { var l = raw.Trim(); if (l.Length > 0 && l[0] != '#') items.Add(l); }
            int n = items.Count, claimed = -1, wrote = 0, bad = 0;
            var lines = new string?[n]; var gate = new object(); var sw = System.Diagnostics.Stopwatch.StartNew();
            using var file = outPath == "-" ? Console.OpenStandardOutput() : new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.Read, 1 << 16);
            using var w = new StreamWriter(file, new UTF8Encoding(false), 1 << 16) { NewLine = "\n" };
            void work()
            {
                while (true)
                {
                    int i = Interlocked.Increment(ref claimed); if (i >= n) return;
                    string line; bool ok;
                    try { line = PlayLine(inputs, items[i], replay, out ok); }
                    catch (Exception e) { ok = false; line = new JWriter().Obj().Prop("id", System.IO.Path.GetFileName(items[i])).Prop("error", e.GetType().Name + ": " + e.Message).EndObj().ToString(); }
                    lock (gate)
                    {
                        lines[i] = line; if (!ok) bad++;
                        while (wrote < n && lines[wrote] != null) { w.Write(lines[wrote]); w.Write('\n'); lines[wrote] = null; wrote++; }
                    }
                }
            }
            var ts = new Thread[Math.Max(1, Math.Min(jobs, n))];
            for (int k = 0; k < ts.Length; k++) { ts[k] = new Thread(work, 64 << 20); ts[k].Start(); }
            foreach (var t in ts) t.Join();
            w.Flush();
            Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"qa: {n} played in {sw.Elapsed.TotalSeconds:0.0} s on {ts.Length} threads{(replay ? "" : ", no replay check")}; {bad} errors or replay differences"));
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e.Message); return 1; }
    }

    static Engine.Inputs FromDir(string d)
    {
        string rd(string f) => File.ReadAllText(System.IO.Path.Combine(d, f));
        string? opt(string f) => File.Exists(System.IO.Path.Combine(d, f)) ? rd(f) : null;
        return Engine.FromTexts(rd("rulebook.json"), rd("people.rules.json"), rd("handover.site.json"), rd("handover.people.json"), opt("handover.provisional.json"), opt("handover.takedown.json"), opt("handover.rounds.json"));
    }

    static string? Arg(string[] a, string k) { int i = Array.IndexOf(a, k); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }

    static string PlayLine(Engine.Inputs inputs, string path, bool replay, out bool ok)
    {
        var script = Script.Parse(File.ReadAllText(path));
        if (script.Mission != "handover") throw new FormatException("qa plays the Handover only");
        var g = Engine.New(inputs, script.Start); var obs = new Obs();
        int until = script.Until <= 0 ? Game.T(inputs.SiteData.Over) : script.Until;
        // Engine.Play, step for step (Debrief.cs), with a sample every 10 ticks
        var sorted = new List<(int idx, Order o)>(); for (int k = 0; k < script.Orders.Count; k++) sorted.Add((k, script.Orders[k]));
        sorted.Sort((a, b) => a.o.Tick != b.o.Tick ? a.o.Tick.CompareTo(b.o.Tick) : a.idx.CompareTo(b.idx));
        obs.Sample(g);
        foreach (var (_, o) in sorted)
        {
            if (o.Tick > until) break;
            RunTo(g, o.Tick, obs);
            if (g.Over && g.Tick != o.Tick) break;
            if (g.Tick == o.Tick) g.Issue(o);
        }
        RunTo(g, until, obs); g.Finish(); obs.End(g);
        bool same = !replay || Same(g, Engine.Replay(inputs, g));
        ok = same;
        var checks = new List<(bool ok, string what)>();
        string record = script.Expect == null ? "none" : script.Verify(g, inputs.Hash);
        var line = Report.Json(System.IO.Path.GetFileName(path), g, checks, same);
        if (!replay)
        {
            const string was = "\"replayIdentical\":true"; int at = line.IndexOf(was, StringComparison.Ordinal);
            if (at >= 0) line = line.Substring(0, at) + "\"replayIdentical\":null" + line.Substring(at + was.Length);
        }
        var q = new JWriter().Obj().Prop("record", record);
        try { q.Key("debrief"); var d = new JWriter(); Debrief.Of(g).Write(d); q.Raw(d.ToString()); }
        catch (Exception e) { q.Raw("null").Prop("debriefError", e.GetType().Name + ": " + e.Message); }
        obs.Write(q);
        var qa = q.EndObj().ToString();
        int close = line.LastIndexOf('}');
        return line.Substring(0, close) + ",\"qa\":" + qa + "}";
    }

    static void RunTo(Game g, int tick, Obs obs)
    {
        while (g.Tick < tick && !g.Over)
        {
            int next = Math.Min(tick, (g.Tick / 10 + 1) * 10);
            g.RunTo(next);
            if (g.Tick % 10 == 0 || g.Over) obs.Sample(g);
        }
    }

    static bool Same(Game a, Game b)
    {
        if (a.Checkpoints.Count != b.Checkpoints.Count) return false;
        for (int i = 0; i < a.Checkpoints.Count; i++) if (a.Checkpoints[i] != b.Checkpoints[i]) return false;
        return true;
    }

    /// <summary>The read-only samples of one play.</summary>
    sealed class Obs
    {
        sealed class Open { public string Kind = "", Order = ""; public Pt At; public int From, Last; }
        readonly Open?[] open = new Open?[4];
        readonly List<(string op, string kind, string order, int from, int to, Pt at)> stuck = new List<(string, string, string, int, int, Pt)>();
        readonly List<(int tick, string state)> vale = new List<(int, string)>();
        readonly Dictionary<string, (int tick, Pt at)> seen = new Dictionary<string, (int, Pt)>(StringComparer.Ordinal);
        readonly List<string> seenOrder = new List<string>();
        readonly List<(int tick, int lockdown)> lockdown = new List<(int, int)>();
        int lastLock = int.MinValue; string lastVale = "";

        public void Sample(Game g)
        {
            int t = g.Tick;
            var vs = g.Vale.ToString();
            if (vs != lastVale) { vale.Add((t, vs)); lastVale = vs; }
            if (g.Lockdown != lastLock) { if (lastLock != int.MinValue) lockdown.Add((t, g.Lockdown)); lastLock = g.Lockdown; }
            foreach (var h in g.Hostiles)
                if (h.Present && !seen.ContainsKey(h.Id)) { seen[h.Id] = (t, h.Pos(t)); seenOrder.Add(h.Id); }
            for (int k = 0; k < 4; k++)
            {
                var op = g.Ops[k]; string kind = "", order = "";
                bool off = op.Down || op.InCar || op.Inside || op.CarriedBy.HasValue || op.Driven || op.Gun.Order.Length > 0;
                if (!off)
                {
                    if (op.Busy != null && op.BusyEnd > 0 && t > op.BusyEnd) { kind = "busy-overdue"; order = op.Busy.Do + (op.Busy.Target.Length > 0 ? ":" + op.Busy.Target : ""); }
                    else if (op.Busy == null && op.Act != null) { kind = "act"; order = op.Act.Do + (op.Act.Target.Length > 0 ? ":" + op.Act.Target : ""); }
                    else if (op.Busy == null && op.Walk != null) { kind = "walk"; order = "move"; }
                }
                var o = open[k];
                if (o != null && (kind != o.Kind || order != o.Order || op.Pos.X != o.At.X || op.Pos.Y != o.At.Y)) { Close(k, o); o = open[k] = null; }
                if (kind.Length > 0)
                {
                    if (o == null) open[k] = new Open { Kind = kind, Order = order, At = op.Pos, From = t, Last = t };
                    else o.Last = t;
                }
            }
        }
        void Close(int k, Open o) { if (o.Last - o.From >= stuckMinTicks) stuck.Add((Op.Name((OpId)k), o.Kind, o.Order, o.From, o.Last, o.At)); }
        public void End(Game g) { for (int k = 0; k < 4; k++) if (open[k] != null) { Close(k, open[k]!); open[k] = null; } }

        public void Write(JWriter w)
        {
            w.OpenArr("stuck");
            foreach (var s in stuck) w.Obj().Prop("op", s.op).Prop("kind", s.kind).Prop("order", s.order).Prop("from", s.from).Prop("to", s.to).Prop("x", s.at.X).Prop("y", s.at.Y).EndObj();
            w.EndArr().OpenArr("vale");
            foreach (var v in vale) w.Obj().Prop("tick", v.tick).Prop("state", v.state).EndObj();
            w.EndArr().OpenArr("seen");
            foreach (var id in seenOrder) { var s = seen[id]; w.Obj().Prop("id", id).Prop("tick", s.tick).Prop("x", s.at.X).Prop("y", s.at.Y).EndObj(); }
            w.EndArr().OpenArr("lockdown");
            foreach (var l in lockdown) w.Obj().Prop("tick", l.tick).Prop("at", l.lockdown).EndObj();
            w.EndArr();
        }
    }
}
