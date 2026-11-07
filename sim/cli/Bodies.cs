// suborned-sim dial bodies (brief 098, law L9 Bodies): scripts and adaptive plans played under the dial's settings with every tick watched by the
// law's independent check (BodyCheck: every upright body as the host reads it, every pair inside two radii), for the law's evidence.
//   dial bodies [items] [--seeds n] [--seed-from k] [--settings "s;s"] [--jobs n] [--show n] [--out f]
//     items: scripts or *.adaptive.json plans (default: contracts/missions/handover/adaptive); settings as dial sweep's (default
//     "unset;people=32;people=64;guards=32;guards=64;cars=8"). Per setting: runs, ticks watched, tick-pairs inside two radii (a body not a tenet
//     in it: the law's failures; tenet with tenet: the tenets' own, listed), the closest pair seen, the law's own counts, outcomes.
//     Exit 1 if any run errs or any pair with a body not a tenet stands inside two radii.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Suborned.Sim;

static class BodiesCli
{
    sealed class Res { public int Ticks, Bad, Tenet, Min = int.MaxValue, LawOverlap, LawTenet, Rejoins, Bodies, Held, CellHeld, Far, Far5; public string MinWhat = "", Outcome = "", Fail = "", FarWhat = ""; public readonly List<string> First = new List<string>(), TenetFirst = new List<string>(); }

    public static int Run(Engine.Inputs inputs, string root, string[] args)
    {
        var items = new List<string>();
        for (int i = 2; i < args.Length; i++) { var a = args[i]; if (a.StartsWith("--", StringComparison.Ordinal)) { i++; continue; } if (Directory.Exists(a)) { var fs = Directory.GetFiles(a, "*.json"); Array.Sort(fs, StringComparer.Ordinal); items.AddRange(fs); } else items.Add(File.Exists(a) ? a : System.IO.Path.Combine(root, a)); }
        if (items.Count == 0) { var fs = Directory.GetFiles(System.IO.Path.Combine(root, "contracts/missions/handover/adaptive"), "*.adaptive.json"); Array.Sort(fs, StringComparer.Ordinal); items.AddRange(fs); }
        int seeds = Arg(args, "--seeds", 3), from = Arg(args, "--seed-from", 1), show = Arg(args, "--show", 3);
        int si = Array.IndexOf(args, "--settings"); var spec = si > 0 ? args[si + 1] : "unset;people=32;people=64;guards=32;guards=64;cars=8";
        var sets = spec.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(s => (name: s, counts: s == "unset" ? null : DialCli.Parse(s))).ToList();
        var jobs = new List<(int set, string item, int seed)>(); for (int s = 0; s < sets.Count; s++) foreach (var it in items) for (int sd = from; sd < from + seeds; sd++) jobs.Add((s, it, sd));
        var sw = Stopwatch.StartNew(); int min = inputs.Rules.BodyMinCm;
        var res = Par.Map(jobs.Count, BatchCli.Jobs(args), j => Watch(inputs, root, jobs[j].item, jobs[j].seed, sets[jobs[j].set].counts, min, show));
        int bad = 0; var w = new JWriter().Obj().Prop("engine", Engine.Version).Prop("minCm", min).Prop("seeds", seeds).Prop("items", items.Count).OpenArr("settings");
        Console.Error.WriteLine($"dial bodies: {jobs.Count} runs ({sets.Count} settings x {items.Count} items x {seeds} seeds) in {sw.Elapsed.TotalSeconds:0.0} s; two radii = {min} cm");
        for (int s = 0; s < sets.Count; s++)
        {
            int runs = 0, ticks = 0, badT = 0, tenT = 0, lawO = 0, lawT = 0, rej = 0, errs = 0, wins = 0, mn = int.MaxValue, far = 0, far5 = 0; long bodies = 0, held = 0, cellHeld = 0; string mnWhat = "", farWhat = ""; var firsts = new List<string>(); var tenets = new List<string>(); var fails = new SortedDictionary<string, int>(StringComparer.Ordinal);
            for (int j = 0; j < jobs.Count; j++)
            {
                if (jobs[j].set != s) continue; runs++;
                if (res[j].error != null) { errs++; bad++; firsts.Add($"{Name(jobs[j].item)} #{jobs[j].seed}: error {res[j].error!.Message}"); continue; }
                var r = res[j].value!; ticks += r.Ticks; badT += r.Bad; tenT += r.Tenet; lawO += r.LawOverlap; lawT += r.LawTenet; rej += r.Rejoins; bodies += r.Bodies; held += r.Held; cellHeld += r.CellHeld; far5 += r.Far5; if (r.Far > far) { far = r.Far; farWhat = $"{r.FarWhat} ({Name(jobs[j].item)} #{jobs[j].seed})"; }
                if (r.Outcome == "success") wins++; else fails[r.Fail.Length > 0 ? r.Fail : r.Outcome] = fails.GetValueOrDefault(r.Fail.Length > 0 ? r.Fail : r.Outcome) + 1;
                if (r.Min < mn) { mn = r.Min; mnWhat = $"{r.MinWhat} ({Name(jobs[j].item)} #{jobs[j].seed})"; }
                if (r.Bad > 0) { bad++; foreach (var f in r.First) if (firsts.Count < show * 3) firsts.Add($"{Name(jobs[j].item)} #{jobs[j].seed}: {f}"); }
                foreach (var f in r.TenetFirst) if (tenets.Count < show * 2) tenets.Add($"{Name(jobs[j].item)} #{jobs[j].seed}: {f}");
            }
            Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{sets[s].name,-14} runs {runs,4}  wins {wins,4}  ticks {ticks,8}  bodies/tick {(double)bodies / Math.Max(1, ticks),6:0.0}  inside two radii: {badT} tick-pairs (tenet with tenet {tenT})  law's own: overlaps {lawO}, tenets {lawT}, last resorts {rej}  closest {(mn == int.MaxValue ? "-" : mn + " cm")} {mnWhat}  errors {errs}\n               held {(double)held / Math.Max(1, ticks):0.00} a tick (cell {(double)cellHeld / Math.Max(1, runs) / Game.Hz:0.0} s a run); farthest from its own place {far / 100.0:0.0} m {farWhat}; ticks with someone 5 m behind {far5}"));
            foreach (var f in firsts.Take(show * 3)) Console.Error.WriteLine("    " + f);
            foreach (var f in tenets) Console.Error.WriteLine("    " + f);
            w.Obj().Prop("setting", sets[s].name).Prop("runs", runs).Prop("wins", wins).Prop("ticks", ticks).Prop("bodyTicks", bodies).Prop("insideTwoRadii", badT).Prop("tenetPairs", tenT).Prop("lawOverlaps", lawO).Prop("lawTenetTicks", lawT).Prop("lastResorts", rej).Prop("closestCm", mn == int.MaxValue ? -1 : mn).Prop("closest", mnWhat).Prop("errors", errs).Prop("heldBodyTicks", held).Prop("cellHeldTicks", cellHeld).Prop("farthestCm", far).Prop("farthest", farWhat).Prop("ticks5mBehind", far5);
            w.Open("fails"); foreach (var kv in fails) w.Prop(kv.Key, kv.Value); w.EndObj(); w.OpenArr("first"); foreach (var f in firsts.Take(show * 3)) w.Str(f); w.EndArr().EndObj();
        }
        var json = w.EndArr().EndObj().ToString(); int oi = Array.IndexOf(args, "--out"); if (oi > 0) File.WriteAllText(args[oi + 1], json + "\n"); else Console.WriteLine(json);
        return bad == 0 ? 0 : 1;
    }
    static string Name(string p) => System.IO.Path.GetFileName(p).Replace(".adaptive.json", "").Replace(".json", "");
    static int Arg(string[] args, string name, int dflt) { int k = Array.IndexOf(args, name); return k > 0 && k + 1 < args.Length ? int.Parse(args[k + 1], CultureInfo.InvariantCulture) : dflt; }

    /// <summary>One item on one seed under counts, every tick watched.</summary>
    static Res Watch(Engine.Inputs inputs, string root, string item, int seed, SortedDictionary<string, int>? counts, int min, int show)
    {
        Script sc;
        if (item.EndsWith(".adaptive.json", StringComparison.Ordinal))
        {
            var pl = Adaptive.Play(inputs, Adaptive.Load(item, root), seed, out _, counts);
            sc = Script.Parse(new JWriter().Obj().Prop("schema", "suborned.sim.script/1").Prop("mission", "handover").Key("start").Raw(pl.Start.Canonical()).OpenArr("orders").EndArr().EndObj().ToString());
            foreach (var (o, _, _, _) in pl.Orders) sc.Orders.Add(o); sc.Until = pl.Tick;
        }
        else { sc = Script.Parse(File.ReadAllText(item)); sc.Start.Seed = seed; sc.Start.Counts.Clear(); if (counts != null) DialCli.Apply(sc.Start, counts); }
        var g = Engine.New(inputs, sc.Start); var r = new Res();
        var orders = sc.Orders.Select((o, k) => (o, k)).OrderBy(x => x.o.Tick).ThenBy(x => x.k).Select(x => x.o).ToList(); int oi = 0, until = sc.Until > 0 ? sc.Until : Game.T(inputs.SiteData.Over);
        void look()
        {
            var up = BodyCheck.Of(g); r.Ticks++; r.Bodies += up.Count; bool any = false, ten = false;
            foreach (var (a, b, cm) in BodyCheck.Closer(up, min))
            {
                if (a.Tenet && b.Tenet) { ten = true; if (r.TenetFirst.Count < show && !r.TenetFirst.Exists(x => x.Contains(a.Id + " (") && x.Contains(b.Id + " ("))) r.TenetFirst.Add($"{Report.Clock(g.Tick)} tenets {a.Id} ({a.Kind}) and {b.Id} ({b.Kind}) {cm} cm at {a.At}"); continue; }
                any = true; if (r.First.Count < show) r.First.Add($"{Report.Clock(g.Tick)} {a.Id} ({a.Kind}) and {b.Id} ({b.Kind}) {cm} cm at {a.At}");
            }
            if (any) r.Bad++; if (ten) r.Tenet++;
            var st = g.BodyStrain(); r.Held += st.held; r.CellHeld += st.cellHeld; if (st.farCm > 500) r.Far5++; if (st.farCm > r.Far) { r.Far = st.farCm; r.FarWhat = $"{st.farId} at {Report.Clock(g.Tick)}"; }
            foreach (var (a, b, cm) in BodyCheck.Closer(up, 400)) { if (cm < r.Min && !(a.Tenet && b.Tenet)) { r.Min = cm; r.MinWhat = $"{a.Id}+{b.Id} at {Report.Clock(g.Tick)}"; } break; }
        }
        look();
        string[]? trace = Environment.GetEnvironmentVariable("SUBORNED_BODY_TRACE")?.Split(','); // id,id,...,fromTick,toTick
        while (g.Tick < until && !g.Over)
        {
            while (oi < orders.Count && orders[oi].Tick == g.Tick) g.Issue(orders[oi++]);
            g.Step(); look();
            if (trace != null && trace.Length >= 3 && g.Tick >= int.Parse(trace[^2], CultureInfo.InvariantCulture) && g.Tick <= int.Parse(trace[^1], CultureInfo.InvariantCulture))
                foreach (var id in trace[..^2])
                {
                    Console.Error.WriteLine($"  {Report.Clock(g.Tick)} {g.BodyTrace(id)}");
                    if (Environment.GetEnvironmentVariable("SUBORNED_BODY_NEAR") is string nr && g.BodiesNow().Find(x => x.id == id) is var me && me.id != null)
                        foreach (var x in g.BodiesNow()) if (x.id != id && G.Within(x.at, me.at, int.Parse(nr, CultureInfo.InvariantCulture))) Console.Error.WriteLine($"      near {x.id} at {x.at} ({G.Dist(x.at, me.at)} cm) cls {x.cls} moving {x.moving}: {g.BodyTrace(x.id)}");
                }
        }
        while (oi < orders.Count && orders[oi].Tick == g.Tick) g.Issue(orders[oi++]);
        g.Finish(); r.Outcome = g.Result.ToString().ToLowerInvariant(); r.Fail = g.FailCode; r.LawOverlap = (int)g.BodyOverlapTicks; r.LawTenet = g.BodyTenetTicks; r.Rejoins = g.BodyRejoins;
        return r;
    }
}
