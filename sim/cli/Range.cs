// suborned-sim range: the fight range headless (brief 029, Keel). On the range's own inputs (Engine.LoadRange: the rulebook, the people
// rules and sim/data/range.site.json or range-dense.site.json), never the Handover's.
//   range list                                     the range scenarios (R01…)
//   range play <scenario | script.json> [--json]   play one (a script names mission "range"; its layout: --layout open|dense, default open)
//   range script <scenario>                        the scenario's run as an order script (suborned.sim.script/1, mission range)
//   range hashes [--start-text <t>]                every range scenario's checkpoint hashes as JSON (--start-text: in place of the inputs hash)
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Suborned.Sim;

static class RangeCli
{
    public static int Run(string root, string[] args)
    {
        string sub = args.Length > 1 ? args[1] : "list"; bool json = Array.IndexOf(args, "--json") > 0;
        int li = Array.IndexOf(args, "--layout"); string layout = li > 0 && li + 1 < args.Length ? args[li + 1] : "open";
        switch (sub)
        {
            case "list": foreach (var s in Scenarios.Range) Console.WriteLine($"{s.Id}\t{s.Layout}\t{s.Intent}"); return 0;
            case "script":
                {
                    var sc = Scenarios.FindRange(args.Length > 2 ? args[2] : "") ?? throw new ArgumentException("range script which scenario?");
                    var run = Scenarios.RunRange(root, sc); Console.WriteLine(Script.Write(run.G, run.Inputs!.Hash)); return 0;
                }
            case "hashes":
                {
                    var w = new JWriter().Obj();
                    int sk = Array.IndexOf(args, "--start-text"); string? st = sk > 0 && sk + 1 < args.Length ? args[sk + 1] : null; // engine-change proof: hash behaviour, not the inputs (as hashes --start-text)
                    foreach (var sc in Scenarios.Range)
                    {
                        Game g;
                        if (st == null) g = Scenarios.RunRange(root, sc).G;
                        else { var ri = Engine.LoadRange(root, sc.Layout); var rr = new Run(new Game(ri.Rules, ri.SiteData, sc.Start, st, ri.Crowd)) { Inputs = ri }; sc.Body(rr); rr.G.Finish(); g = rr.G; }
                        w.OpenArr(sc.Id); foreach (var (t, h) in g.Checkpoints) w.Str(t.ToString(CultureInfo.InvariantCulture) + ":" + Hash.Hex(h)); w.Str("result:" + g.Result + ":" + g.FailCode); w.EndArr();
                    }
                    Console.WriteLine(w.EndObj().ToString()); return 0;
                }
            case "play":
                {
                    var what = args.Length > 2 ? args[2] : throw new ArgumentException("range play what?");
                    Game g; Engine.Inputs inputs; var checks = new List<(bool ok, string what)>(); string id;
                    var sc = Scenarios.FindRange(what);
                    if (sc != null) { var run = Scenarios.RunRange(root, sc); g = run.G; inputs = run.Inputs!; checks = run.Checks; id = sc.Id; }
                    else
                    {
                        var path = File.Exists(what) ? what : System.IO.Path.Combine(root, what);
                        var script = Script.Parse(File.ReadAllText(path));
                        if (script.Mission != "range") throw new ArgumentException($"{what} is a {script.Mission} script: play it with `play`");
                        inputs = Engine.LoadRange(root, layout);
                        g = Engine.Play(inputs, script.Start, script.Orders, script.Until); id = System.IO.Path.GetFileName(path);
                        if (script.Expect != null) { var rec = script.Verify(g, inputs.Hash); checks.Add((rec.Length == 0, rec.Length == 0 ? "the replay matches its record (inputs, engine, start, every checkpoint, outcome)" : "record: " + rec)); }
                    }
                    var replay = Engine.Replay(inputs, g);
                    bool same = replay.Checkpoints.Count == g.Checkpoints.Count; for (int i = 0; same && i < g.Checkpoints.Count; i++) same = g.Checkpoints[i] == replay.Checkpoints[i];
                    bool passed = checks.TrueForAll(c => c.ok);
                    if (json) Console.WriteLine(Report.Json(id, g, checks, same));
                    else
                    {
                        Console.WriteLine($"{id}: {g.Result} at {Report.Clock(g.Tick)}  hostiles {g.Hostiles.Count}  down {g.Hostiles.FindAll(h => h.Down).Count}  shots {g.FightShots}");
                        foreach (var l in g.Log) Console.WriteLine($"  {Report.Clock(l.Tick)}  {l.Code,-28} {l.Text}");
                        foreach (var c in checks) Console.WriteLine($"  [{(c.ok ? "ok" : "FAIL")}] {c.what}");
                        Console.WriteLine($"  start {Hash.Hex(g.StartHash)}  final {Hash.Hex(g.Checkpoints[g.Checkpoints.Count - 1].hash)}  checkpoints {g.Checkpoints.Count}  replay {(same ? "identical" : "DIFFERS")}");
                    }
                    return passed && same ? 0 : 1;
                }
            default: Console.Error.WriteLine($"unknown range command {sub} (list, play, script, hashes)"); return 2;
        }
    }
}
