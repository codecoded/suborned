// suborned-sim spread (brief 080): how far runs spread across seeds. For each script: plays it on seed 0 and on seeds k..k+n-1, with its fixed ticks, and
// prints wins, fails by cause, the end clock, the fight's length, HP lost, agents down, Trail and Alert, when the log first differs from seed 0, and how
// many people stand more than a metre from where they stood on seed 0 at each clock mark. Read-only: orders go in as any script's do. --retry-go issues the
// script's last Go again each second after its tick (Motive's probe: "Go when the car is free", until Waymark's adaptive plans exist). --json: one line a script.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Suborned.Sim;

static class SpreadCli
{
    sealed class Rec { public string Res = "", Fail = ""; public int End, FightS, Hp, Down, Trail, Alert, FirstLog = -2, Shots, Hits; public readonly Dictionary<int, (int cast, int crowd)> Moved = new Dictionary<int, (int, int)>(); public List<string> Log = new List<string>(); public List<Dictionary<string, Pt>> Samples = new List<Dictionary<string, Pt>>(); }
    static readonly int[] Marks = { 60, 120, 180, 240, 300, 360, 420 };

    static Rec Play(Engine.Inputs i, string text, int seed, bool retryGo, Rec? baseline)
    {
        var sc = Script.Parse(text); sc.Start.Seed = seed;
        var g = Engine.New(i, sc.Start); int until = sc.Until <= 0 ? Game.T(i.SiteData.Over) : sc.Until;
        var list = sc.Orders.Select((o, k) => (k, o)).OrderBy(x => x.o.Tick).ThenBy(x => x.k).ToList(); int oi = 0;
        var go = retryGo ? sc.Orders.LastOrDefault(o => o.Do == "go") : null;
        int goTick = go?.Tick ?? int.MaxValue;
        var run = new Rec(); int sample = 0;
        for (int t = 0; t <= until; t++)
        {
            if (t > 0) g.RunTo(t);
            if (g.Over && g.Tick != t) break;
            while (oi < list.Count && list[oi].o.Tick == t) { g.Issue(list[oi].o); oi++; }
            if (t % 100 == 0)
            {
                var pos = new Dictionary<string, Pt>();
                foreach (var o in g.Ops) if (!o.Inside && !o.InCar) pos[Op.Name(o.Id)] = o.Pos;
                foreach (var m in g.Crew) if (g.PosOf(m.Id) is Pt mp) pos[m.Id] = mp;
                foreach (var h in g.Hostiles) if (g.PosOf(h.Id) is Pt hp) pos[h.Id] = hp;
                foreach (var id in new[] { "guard", "patrol", "waitress", "driver", "councillor" }) if (g.PosOf(id) is Pt ip) pos[id] = ip;
                foreach (var w in g.Walkers) if (g.CrowdPos(w) is Pt wp) pos["w:" + w.P.Id] = wp;
                run.Samples.Add(pos);
                if (baseline != null && sample < baseline.Samples.Count)
                {
                    int cast = 0, crowd = 0; foreach (var kv in pos) if (baseline.Samples[sample].TryGetValue(kv.Key, out var b) && G.D2(kv.Value, b) > 100L * 100) { if (kv.Key.StartsWith("w:", StringComparison.Ordinal)) crowd++; else cast++; }
                    run.Moved[t / 10] = (cast, crowd);
                }
                sample++;
            }
            if (g.Over) break;
            if (go != null && t > goTick && t % 10 == 0) { go.Tick = t; g.Issue(go); }
        }
        if (!g.Over) g.RunTo(until);
        g.Finish();
        run.Res = g.Result.ToString(); run.Fail = g.FailCode; run.End = g.Tick; run.Alert = g.Alert; run.Trail = g.Trail; run.FightS = g.CellFightS; run.Shots = g.CellShots; run.Hits = g.CellHits; run.Hp = g.CellHpLost; run.Down = g.CellDownCount;
        foreach (var l in g.Log) run.Log.Add(l.Tick.ToString(CultureInfo.InvariantCulture) + "|" + l.Code);
        if (baseline != null)
        {
            run.FirstLog = -1; int n = Math.Min(run.Log.Count, baseline.Log.Count);
            for (int k = 0; k < n; k++) if (run.Log[k] != baseline.Log[k]) { run.FirstLog = int.Parse(run.Log[k].Split('|')[0], CultureInfo.InvariantCulture); break; }
            if (run.FirstLog < 0 && run.Log.Count != baseline.Log.Count) run.FirstLog = g.Tick;
            run.Samples = new List<Dictionary<string, Pt>>(); run.Log = new List<string>();
        }
        return run;
    }

    static string Clock(int ticks) => $"{ticks / 600}:{ticks / 10 % 60:00}";
    static int Q(List<int> sorted, int pct) => sorted.Count == 0 ? 0 : sorted[Math.Min(sorted.Count - 1, sorted.Count * pct / 100)];

    public static int Run(Engine.Inputs inputs, string root, string[] args)
    {
        _ = Scenarios.All.Count;
        int si = Array.IndexOf(args, "--seeds"), fi = Array.IndexOf(args, "--seed-from"), ji = Array.IndexOf(args, "--json");
        int n = si > 0 ? int.Parse(args[si + 1], CultureInfo.InvariantCulture) : 60, from = fi > 0 ? int.Parse(args[fi + 1], CultureInfo.InvariantCulture) : 1; bool retry = Array.IndexOf(args, "--retry-go") > 0;
        var files = new List<string>();
        for (int k = 1; k < args.Length; k++)
        {
            var a = args[k]; if (a == "--seeds" || a == "--seed-from" || a == "--json" || a == "--jobs") { k++; continue; }
            if (a == "--retry-go") continue;
            if (a.StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException($"spread: unknown option {a}");
            if (Directory.Exists(a)) { var fs = Directory.GetFiles(a, "*.json"); Array.Sort(fs, StringComparer.Ordinal); files.AddRange(fs); } else files.Add(a);
        }
        int jobs = BatchCli.Jobs(args); var json = ji > 0 ? new StreamWriter(args[ji + 1]) : null;
        foreach (var file in files)
        {
            var text = File.ReadAllText(file); var name = System.IO.Path.GetFileNameWithoutExtension(file);
            var b = Scenarios.Bounded(() => Play(inputs, text, 0, false, null));
            var res = Par.Map(n, jobs, k => Scenarios.Bounded(() => Play(inputs, text, from + k, retry, b)));
            var rs = new List<Rec>(); int errors = 0; foreach (var (v, e) in res) { if (e != null) { errors++; Console.Error.WriteLine($"{name}: {e.Message}"); } else rs.Add(v!); }
            int wins = rs.Count(r => r.Res == "Success");
            var fails = rs.Where(r => r.Res != "Success").GroupBy(r => r.Res == "Failure" ? r.Fail : r.Res).OrderByDescending(x => x.Count()).ThenBy(x => x.Key, StringComparer.Ordinal).Select(x => $"{x.Key}x{x.Count()}");
            var fl = rs.Where(r => r.FightS > 0).Select(r => r.FightS).OrderBy(x => x).ToList(); var hp = rs.Select(r => r.Hp).OrderBy(x => x).ToList(); var en = rs.Select(r => r.End).OrderBy(x => x).ToList();
            var tr = rs.Select(r => r.Trail).OrderBy(x => x).ToList(); var diff = rs.Where(r => r.FirstLog >= 0).Select(r => r.FirstLog).OrderBy(x => x).ToList(); long shots = rs.Sum(r => (long)r.Shots), hits = rs.Sum(r => (long)r.Hits);
            string moved = string.Join(" ", Marks.Select(m => { var x = rs.Where(r => r.Moved.ContainsKey(m)).ToList(); if (x.Count == 0) return $"{m / 60}:{m % 60:00}=-"; return $"{m / 60}:{m % 60:00}={x.Select(r => r.Moved[m].cast).OrderBy(v => v).ElementAt(x.Count / 2)}/{x.Select(r => r.Moved[m].crowd).OrderBy(v => v).ElementAt(x.Count / 2)}"; }));
            Console.WriteLine($"## {name}: seed 0 {b.Res}{(b.Fail.Length > 0 ? " " + b.Fail : "")} end {Clock(b.End)} fight {b.FightS}s hp {b.Hp} T {b.Trail} A {b.Alert}");
            Console.WriteLine($"   wins {wins}/{rs.Count} ({wins * 100 / Math.Max(1, rs.Count)}%) [{string.Join(", ", fails)}] end {Clock(Q(en, 0))}..{Clock(Q(en, 100))}");
            Console.WriteLine($"   fight s min/p25/med/p75/p95/max {Q(fl, 0)}/{Q(fl, 25)}/{Q(fl, 50)}/{Q(fl, 75)}/{Q(fl, 95)}/{Q(fl, 100)} (n {fl.Count})  hp lost med {Q(hp, 50)} max {Q(hp, 100)}  down/run {rs.Sum(r => r.Down) / (double)Math.Max(1, rs.Count):0.00}  hit {(shots == 0 ? 0 : hits * 100 / shots)}% of {shots / Math.Max(1, rs.Count)} shots/run  T {Q(tr, 0)}..{Q(tr, 100)}");
            Console.WriteLine($"   log differs from seed 0: {diff.Count}/{rs.Count}, first {(diff.Count == 0 ? "-" : Clock(diff[0]))} med {(diff.Count == 0 ? "-" : Clock(Q(diff, 50)))}  moved>1m cast/crowd med {moved}");
            json?.WriteLine(new JWriter().Obj().Prop("script", name).Prop("seeds", rs.Count).Prop("wins", wins).Prop("fails", string.Join(",", fails)).Prop("fightMed", Q(fl, 50)).Prop("fightP25", Q(fl, 25)).Prop("fightP75", Q(fl, 75)).Prop("fightP95", Q(fl, 95)).Prop("hpMed", Q(hp, 50)).Prop("firstDiffMed", Q(diff, 50)).Prop("errors", errors).EndObj().ToString());
        }
        json?.Dispose();
        return 0;
    }
}
