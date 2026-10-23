// suborned-sim: the engine headless (brief 011 A).
//   play <scenario id | script.json> [--json]   play a scenario (S01…S28, N8…) or an order script; print outcome, log, checksums
//   batch [paths | dir | - ] [--list f] [--no-replay] [--jobs n] [--out f]   many scripts in one process (inputs loaded once, every core): one `play --json` line each, in order
//   batch --serve                               the same as a server: lines `tag<TAB>replay|noreplay<TAB>path` on stdin, `tag<TAB>json` out as each finishes (tools/missions/check/engine.ts)
//   list                                        the scenarios
//   script <scenario>                           the scenario's start state and orders as an order script (suborned.sim.script/1)
//   perf [runs]                                 sim-seconds per wall-second over route A
//   probe                                       distances and windows on the real block (for writing scenarios)
//   fight-lab [--runs ..] [--presets ..]       brief 027's balance matrix: L1–L4, P-A, P-G, P-B under today, lab, lab charge off, lab field on
//   drive-session [out]                         record the driven session the DP1 scenario replays (brief 025; Fixtures/drive-session.script.json)
//   range list|play|script|hashes               brief 029's fight range on its own site (sim/cli/Range.cs)
//   codes                                       every refusal code an order can get (Fable's text.json locks read these)
//   cover-vectors                               the cover each fight post would take (car as foe, and advancing), for tools/cover/select.ts to agree
//   fans [tick] [--crowd]                       every watcher's sight fan at a tick of an idle run (Game.ConeFan), with its cost
//   hashes [--start-text <t>] [--legacy-start]  every scenario's and route script's checkpoint hashes as JSON; --legacy-start leaves a fixed
//                                               security level out of the start state (a behaviour-only comparison across brief 013); --start-text replaces the inputs
//                                               hash in the start state (a behaviour-only comparison across an input edit: Keel, cones 2026-10-16);
//                                               --standing-as <n> hashes Standing as if standing.start were n (across brief 014's start move)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Suborned.Sim;

static class Program
{
    /// <summary>Pulse §8.3 on the site's own cover: for each fight post (stand), the point selectCover gives with the getaway car as the foe and
    /// advancing to it (seek 800 cm, mission second 200, when every car is present); "" for none. tools/sim/sim.test.ts compares select.ts.</summary>
    static string CoverVectors(Engine.Inputs i)
    {
        var g = Engine.New(i); var s = g.S; var r = g.R; var car = s.Stand(s.GetawayCar);
        var w = new JWriter().Obj();
        var posts = new List<string>(s.FightAmbush); posts.AddRange(s.FightPolice); posts.Add(s.FightGuard);
        foreach (var p in posts)
        {
            var me = s.Stand(p); var none = new HashSet<string>();
            var foe = CoverRules.Select(s.Cover, me, car, car, none, 800, 200, r.PointBlankCm, r.CoverQPartial, r.CoverRetreatCm, r.CoverStepCloserCm);
            var adv = CoverRules.Select(s.Cover, me, car, null, none, 800, 200, r.PointBlankCm, r.CoverQPartial, r.CoverRetreatCm, r.CoverStepCloserCm);
            w.Open(p).Prop("foe", foe?.Id ?? "").Prop("advance", adv?.Id ?? "").EndObj();
        }
        return w.EndObj().ToString();
    }

    static int Main(string[] args)
    {
        try
        {
            var root = Engine.FindRoot(Directory.GetCurrentDirectory());
            var inputs = Engine.Load(root);
            if (args.Length == 0) { Console.Error.WriteLine("usage: play <scenario|script.json> [--json] | list | script <scenario> | perf [runs] | probe"); return 2; }
            bool json = Array.IndexOf(args, "--json") >= 0;
            switch (args[0])
            {
                case "list": foreach (var s in Scenarios.All) Console.WriteLine($"{s.Id}\t{s.Intent}"); return 0;
                case "script":
                    {
                        var sc = Scenarios.Find(args.Length > 1 ? args[1] : "") ?? throw new ArgumentException("script which scenario?");
                        Console.WriteLine(Script.Write(Scenarios.Run(inputs, sc).G, inputs.Hash)); return 0;
                    }
                case "batch": return BatchCli.Run(inputs, root, args); // brief 042: many scripts, one process, every core
                case "play": return Play(inputs, root, args.Length > 1 ? args[1] : throw new ArgumentException("play what?"), json);
                case "perf": return Perf(inputs, args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 20, args.Length > 2 ? args[2] : "S10");
                case "probe": Probe.Run(inputs); return 0;
                case "range": return RangeCli.Run(root, args); // brief 029: the fight range, on its own inputs
                case "fight-lab": return FightLabCli.Run(inputs, root, args); // brief 027 §6.5: the balance matrix (briefs/027/keel/fight-lab.json)
                case "drive-session": return DriveSession.Record(inputs, root, args.Length > 1 ? args[1] : null); // brief 025: record DP1's session
                case "cover-vectors": Console.WriteLine(CoverVectors(inputs)); return 0;
                case "fans": return Fans(inputs, args.Length > 1 && args[1] != "--crowd" ? int.Parse(args[1], CultureInfo.InvariantCulture) : 0, Array.IndexOf(args, "--crowd") > 0);
                case "hashes":
                {
                    int k = Array.IndexOf(args, "--start-text"), sa = Array.IndexOf(args, "--standing-as"); Start.LegacyCanonical = Array.IndexOf(args, "--legacy-start") > 0;
                    if (sa > 0) Hash.StandingAs = int.Parse(args[sa + 1], CultureInfo.InvariantCulture) - inputs.Rules.StandingStart;
                    Console.WriteLine(Hashes(inputs, root, k > 0 ? args[k + 1] : null)); return 0;
                }
                case "codes": { var w = new JWriter().Arr(); foreach (var c in Answer.Codes) w.Str(c); Console.WriteLine(w.EndArr().ToString()); return 0; }
                default: Console.Error.WriteLine($"unknown command {args[0]}"); return 2;
            }
        }
        catch (Exception e) { Console.Error.WriteLine(e.Message); return 1; }
    }

    static int Play(Engine.Inputs inputs, string root, string what, bool json)
    {
        var p = BatchCli.PlayOne(inputs, root, what); var g = p.G; var id = p.Id; var checks = p.Checks; bool same = p.Same;
        bool passed = p.Passed;
        if (json) { Console.WriteLine(Report.Json(id, g, checks, same)); }
        else
        {
            Console.WriteLine($"{id}: {g.Result}{(g.FailCode.Length > 0 ? " (" + g.FailCode + ")" : "")} at {Report.Clock(g.Tick)}  cash {g.Cash}  trail {g.Trail}  alert {g.Alert}  shots {g.Gunfire}");
            foreach (var l in g.Log) Console.WriteLine($"  {Report.Clock(l.Tick)}  {l.Code,-28} {l.Text}");
            foreach (var c in checks) Console.WriteLine($"  [{(c.ok ? "ok" : "FAIL")}] {c.what}");
            if (g.Result == Outcome.Success) { var w = new JWriter(); Debrief.Of(g).Write(w); Console.WriteLine($"  debrief {w}"); }
            if (g.Cause != null) Console.WriteLine($"  cause {g.Cause.Json()}");
            Console.WriteLine($"  start {Hash.Hex(g.StartHash)}  final {Hash.Hex(g.Checkpoints[g.Checkpoints.Count - 1].hash)}  checkpoints {g.Checkpoints.Count}  replay {(same ? "identical" : "DIFFERS")}");
        }
        return passed && same ? 0 : 1;
    }

    /// <summary>Every scenario's and recorded route script's checkpoints, with the start state's text given (default: the inputs hash).</summary>
    static string Hashes(Engine.Inputs i, string root, string? startText)
    {
        string st = startText ?? i.Hash.ToString(CultureInfo.InvariantCulture);
        var w = new JWriter().Obj().Prop("inputs", i.Hash.ToString(CultureInfo.InvariantCulture)).Prop("startText", st);
        void put(string id, Game g) { w.OpenArr(id); foreach (var (t, h) in g.Checkpoints) w.Str(t.ToString(CultureInfo.InvariantCulture) + ":" + Hash.Hex(h)); w.Str("result:" + g.Result + ":" + g.FailCode); w.EndArr(); }
        foreach (var sc in Scenarios.All)
        {
            var r = new Run(new Game(i.Rules, i.SiteData, sc.Start, st, i.Crowd)); sc.Body(r); r.G.Finish(); put(sc.Id, r.G);
        }
        var files = new List<string>();
        foreach (var d in new[] { "contracts/missions/handover/routes", "briefs/011/motive-fights" })
            if (Directory.Exists(System.IO.Path.Combine(root, d))) foreach (var f in Directory.GetFiles(System.IO.Path.Combine(root, d), "*.json")) files.Add(f);
        files.Sort(StringComparer.Ordinal);
        foreach (var f in files)
        {
            var txt = File.ReadAllText(f); if (!txt.Contains("suborned.sim.script/1")) continue;
            var sc = Script.Parse(txt); var g = new Game(i.Rules, i.SiteData, sc.Start, st, i.Crowd);
            // Engine.Play with the given start text
            var list = new List<(int idx, Order o)>(); for (int k = 0; k < sc.Orders.Count; k++) list.Add((k, sc.Orders[k]));
            list.Sort((a, b) => a.o.Tick != b.o.Tick ? a.o.Tick.CompareTo(b.o.Tick) : a.idx.CompareTo(b.idx));
            int until = sc.Until <= 0 ? Game.T(i.SiteData.Over) : sc.Until;
            foreach (var (_, o) in list) { if (o.Tick > until) break; g.RunTo(o.Tick); if (g.Over && g.Tick != o.Tick) break; if (g.Tick == o.Tick) g.Issue(o); }
            g.RunTo(until); g.Finish();
            put(System.IO.Path.GetRelativePath(root, f), g);
        }
        return w.EndObj().ToString();
    }

    /// <summary>The fans of an idle run at a tick, as JSON, and what building and reading them cost (Keel, cones 2026-10-16).</summary>
    static int Fans(Engine.Inputs i, int tick, bool crowd)
    {
        var g = Engine.New(i); g.RunTo(tick);
        var w = new JWriter().Obj().Prop("tick", g.Tick).Prop("crowd", crowd).OpenArr("fans");
        foreach (var id in g.WatcherIds())
        {
            var f = g.ConeFan(id, crowd); if (f == null) continue;
            w.Obj().Prop("watcher", f.Watcher).Prop("kind", f.Kind).Prop("live", f.Live).Prop("reach", f.Reach).Prop("points", f.Points.Length);
            w.Key("poly").Raw("[" + string.Join(",", Array.ConvertAll(f.Points, p => $"[{p.X},{p.Y}]")) + "]");
            w.Key("stop").Raw("[" + string.Join(",", Array.ConvertAll(f.Stop, x => "\"" + x + "\"")) + "]");
            w.EndObj();
        }
        w.EndArr();
        // cost: a cold build of every fan (a fresh game each time, so nothing is cached), and a warm read (cached by pose)
        double coldMs = 0; for (int k = 0; k < 50; k++) { var h = Engine.New(i); h.RunTo(tick); var c = Stopwatch.StartNew(); foreach (var id in h.WatcherIds()) h.ConeFan(id, crowd); c.Stop(); coldMs += c.Elapsed.TotalMilliseconds; }
        var warm = Stopwatch.StartNew(); for (int k = 0; k < 10000; k++) foreach (var id in g.WatcherIds()) g.ConeFan(id, crowd); warm.Stop();
        w.Key("coldMsAllFans").Raw((coldMs / 50).ToString("0.0000", CultureInfo.InvariantCulture)).Key("warmUsAllFans").Raw((warm.Elapsed.TotalMilliseconds * 1000 / 10000).ToString("0.000", CultureInfo.InvariantCulture));
        Console.WriteLine(w.EndObj().ToString());
        return 0;
    }

    static int Perf(Engine.Inputs inputs, int runs, string id)
    {
        var sc = Scenarios.Find(id) ?? throw new ArgumentException($"no scenario {id}");
        Scenarios.Run(inputs, sc); // warm
        var sw = Stopwatch.StartNew(); long ticks = 0;
        for (int i = 0; i < runs; i++) ticks += Scenarios.Run(inputs, sc).G.Tick;
        sw.Stop();
        double simS = ticks / 10.0, wallS = sw.Elapsed.TotalSeconds;
        Console.WriteLine(FormattableString.Invariant($"{sc.Id} x{runs}: {simS:0} sim-s in {wallS:0.000} wall-s = {simS / wallS:0} sim-s per wall-s ({wallS * 1000 / runs:0.0} ms a run)"));
        return 0;
    }
}
