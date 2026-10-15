// suborned-sim: the engine headless (brief 011 A).
//   play <scenario id | script.json> [--json]   play a scenario (S01…S28, N8…) or an order script; print outcome, log, checksums
//   list                                        the scenarios
//   script <scenario>                           the scenario's start state and orders as an order script (suborned.sim.script/1)
//   perf [runs]                                 sim-seconds per wall-second over route A
//   probe                                       distances and windows on the real block (for writing scenarios)
//   cover-vectors                               the cover each fight post would take (car as foe, and advancing), for tools/cover/select.ts to agree
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
                case "play": return Play(inputs, root, args.Length > 1 ? args[1] : throw new ArgumentException("play what?"), json);
                case "perf": return Perf(inputs, args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 20, args.Length > 2 ? args[2] : "S10");
                case "probe": Probe.Run(inputs); return 0;
                case "cover-vectors": Console.WriteLine(CoverVectors(inputs)); return 0;
                default: Console.Error.WriteLine($"unknown command {args[0]}"); return 2;
            }
        }
        catch (Exception e) { Console.Error.WriteLine(e.Message); return 1; }
    }

    static int Play(Engine.Inputs inputs, string root, string what, bool json)
    {
        Game g; List<(bool ok, string what)> checks = new List<(bool, string)>(); string id; string record = "";
        var sc = Scenarios.Find(what);
        if (sc != null) { var run = Scenarios.Run(inputs, sc); g = run.G; checks = run.Checks; id = sc.Id; }
        else
        {
            var path = File.Exists(what) ? what : System.IO.Path.Combine(root, what);
            var script = Script.Parse(File.ReadAllText(path));
            g = Engine.Play(inputs, script.Start, script.Orders, script.Until); id = System.IO.Path.GetFileName(path);
            if (script.Expect != null) { record = script.Verify(g, inputs.Hash); checks.Add((record.Length == 0, record.Length == 0 ? "the replay matches its record (inputs, engine, start, every checkpoint, outcome)" : "record: " + record)); }
        }
        var replay = Engine.Replay(inputs, g);
        bool same = Same(g, replay);
        bool passed = checks.TrueForAll(c => c.ok);
        if (json) { Console.WriteLine(Report.Json(id, g, checks, same)); }
        else
        {
            Console.WriteLine($"{id}: {g.Result}{(g.FailCode.Length > 0 ? " (" + g.FailCode + ")" : "")} at {Report.Clock(g.Tick)}  cash {g.Cash}  trail {g.Trail}  alert {g.Alert}  shots {g.Gunfire}");
            foreach (var l in g.Log) Console.WriteLine($"  {Report.Clock(l.Tick)}  {l.Code,-28} {l.Text}");
            foreach (var c in checks) Console.WriteLine($"  [{(c.ok ? "ok" : "FAIL")}] {c.what}");
            if (g.Result == Outcome.Success) { var w = new JWriter(); Debrief.Of(g).Write(w); Console.WriteLine($"  debrief {w}"); }
            Console.WriteLine($"  start {Hash.Hex(g.StartHash)}  final {Hash.Hex(g.Checkpoints[g.Checkpoints.Count - 1].hash)}  checkpoints {g.Checkpoints.Count}  replay {(same ? "identical" : "DIFFERS")}");
        }
        return passed && same ? 0 : 1;
    }

    static bool Same(Game a, Game b)
    {
        if (a.Checkpoints.Count != b.Checkpoints.Count) return false;
        for (int i = 0; i < a.Checkpoints.Count; i++) if (a.Checkpoints[i] != b.Checkpoints[i]) return false;
        return true;
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
