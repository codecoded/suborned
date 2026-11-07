// suborned-sim dial street (brief 098 step 1b; Sky 2026-11-07: "people counter is for street"): the people count, measured.
//   dial street [--settings "people=16;people=32;..."] [--seeds n] [--seed-from k] [--script s]
//     per setting and seed: what the census built (asked, the street it holds by routine, everyone of the kind in the zone, who left and came) and the
//     street as the game shows it, every tick of the mission's clock: the crowd of the kind whose place is inside the window and out of doors
//     (CrowdPos), while the script plays (default: no orders) and by routine after it ends, plus the cast's share (CastStreet10: played by role,
//     it never leaves); the cast as played (PersonPos) is shown beside it, over the ticks the game ran. As JSON on stdout.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Suborned.Sim;

static class StreetCli
{
    public static int Run(Engine.Inputs inputs, string root, string[] args)
    {
        int Arg(string name, int dflt) { int k = Array.IndexOf(args, name); return k > 0 && k + 1 < args.Length ? int.Parse(args[k + 1], CultureInfo.InvariantCulture) : dflt; }
        int si = Array.IndexOf(args, "--settings"), sc = Array.IndexOf(args, "--script"); int seeds = Arg("--seeds", 8), from = Arg("--seed-from", 1);
        var today = Population.Today(inputs).Single(c => c.Kind == "people").Today;
        var specs = si > 0 ? args[si + 1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) : new[] { "unset", $"people={today}", $"people={2 * today}", $"people={today / 2}", "people=0", $"people={4 * today}" };
        Script? script = sc > 0 ? Script.Parse(File.ReadAllText(File.Exists(args[sc + 1]) ? args[sc + 1] : System.IO.Path.Combine(root, args[sc + 1]))) : null;
        var pop = inputs.Population!; var w = new JWriter().Obj().Prop("engine", Engine.Version).Prop("today", today).Prop("seeds", seeds).OpenArr("settings");
        Console.Error.WriteLine("setting        seeds  asked  census street  observed (mean)  min..max   in the zone");
        foreach (var spec in specs)
        {
            var counts = spec == "unset" ? null : DialCli.Parse(spec); var obs = new List<double>(); var built = new List<double>(); var totals = new List<int>(); var ends = new List<int>(); var castObs = new List<double>(); var castShare = new List<double>(); int asked = -1;
            for (int seed = from; seed < from + seeds; seed++)
            {
                var start = script != null ? Start.Parse(JNode.Parse(script.Start.Canonical())) : new Start(); start.Seed = seed; if (counts != null) DialCli.Apply(start, counts);
                var g = Engine.New(inputs, start); var orders = (script?.Orders ?? new List<Order>()).OrderBy(o => o.Tick).ToList(); int oi = 0, over = Game.T(inputs.SiteData.Over); long sum = 0, castLive = 0;
                var cast = inputs.SiteData.Cast.Where(c => Population.KindOf(pop, Population.CastJob(c.Role)) == "people").Select(c => g.PersonById(c.Id)!).ToList();
                for (int t = 0; t < over; t++)
                {
                    if (!g.Over && g.Tick < t) { while (oi < orders.Count && orders[oi].Tick == g.Tick) g.Issue(orders[oi++]); g.Step(); }
                    bool live = !g.Over && g.Tick == t;
                    foreach (var x in g.Walkers) if (Population.KindOf(pop, x.P.Job) == "people" && (live ? g.CrowdPos(x) : g.CrowdPos(x, t)) is Pt p && g.S.Grid.InGrid(p)) sum++;
                    if (live) foreach (var p in cast) if (g.PersonPos(p) is Pt q && g.S.Grid.InGrid(q)) castLive++;
                }
                var cc = g.Census.Find(c => c.Kind == "people") ?? Population.Today(inputs).Single(c => c.Kind == "people"); // the crowd as the game shows it, every tick; the cast by its share (played by role, it never leaves)
                ends.Add(g.Over ? g.Tick : over);
                obs.Add((double)sum / over + cc.CastStreet10 / 10.0); castObs.Add((double)castLive / Math.Max(1, g.Over ? g.Tick : over)); castShare.Add(cc.CastStreet10 / 10.0); var cen = g.Census.Find(c => c.Kind == "people");
                built.Add(cen != null ? cen.Street10 / 10.0 : (double)Population.Today(inputs).Single(c => c.Kind == "people").Street10 / 10.0); totals.Add(cen?.Total ?? Population.Today(inputs).Single(c => c.Kind == "people").Total); asked = cen?.Asked ?? -1;
            }
            Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{spec,-14} {seeds,5} {(asked >= 0 ? asked.ToString(CultureInfo.InvariantCulture) : "-"),6} {built.Average(),13:0.0} {obs.Average(),16:0.00} {obs.Min(),5:0.0}..{obs.Max(),-5:0.0} {totals.Average(),8:0}   (cast: {castShare.Average():0.00} by its routine, {castObs.Average():0.00} as played to {Report.Clock((int)ends.Average())} on average)"));
            w.Obj().Prop("setting", spec).Prop("asked", asked).Key("censusStreet").Raw(built.Average().ToString("0.00", CultureInfo.InvariantCulture)).Key("observedMean").Raw(obs.Average().ToString("0.00", CultureInfo.InvariantCulture))
                .Key("observedMin").Raw(obs.Min().ToString("0.00", CultureInfo.InvariantCulture)).Key("observedMax").Raw(obs.Max().ToString("0.00", CultureInfo.InvariantCulture)).Prop("inZoneMean", (int)Math.Round(totals.Average())).EndObj();
        }
        Console.WriteLine(w.EndArr().EndObj().ToString());
        return 0;
    }
}
