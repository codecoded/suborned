// Kept here as the evidence's source (brief 098, Waymark); not built from here: built in a scratch copy of sim/cli (out/scratch/waymark/probe) with `case "spots": return SpotsCli.Run(inputs, root, args);` added to Dial.cs, as Vantage's regions probe (briefs/098/vantage/regions-probe.cs).
// Waymark's scratch probe: spots for a discreet Listen (rulebook cell.listen) round a point, ranked by the city's traffic over them.
//   dial spots <x,y> [--rmin cm] [--rmax cm] [--at m:ss] [--heat plan.adaptive.json] [--seeds n] [--counts c] [--hfrom m:ss] [--hto m:ss]
// A spot: a free cell rmin..rmax from the point, beside something solid (a 50 cm neighbour blocked), on a clear line to it (Site.Sight at 160 cm),
// out of every watcher's view at the tick (InSight over WatcherIds). Traffic: body-ticks of the city within 60 cm of the spot over the window, summed over seeds.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Suborned.Sim;

static class SpotsCli
{
    static string Arg(string[] a, string k, string d) { int i = Array.IndexOf(a, k); return i > 0 && i + 1 < a.Length ? a[i + 1] : d; }
    static int Clk(string s) { var p = s.Split(':'); return (int)Math.Round((int.Parse(p[0], CultureInfo.InvariantCulture) * 60 + double.Parse(p[1], CultureInfo.InvariantCulture)) * Game.Hz); }
    public static int Run(Engine.Inputs inputs, string root, string[] args)
    {
        var c = args[2].Split(',').Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray(); var her = new Pt(c[0], c[1]);
        int rmin = int.Parse(Arg(args, "--rmin", "300"), CultureInfo.InvariantCulture), rmax = int.Parse(Arg(args, "--rmax", "500"), CultureInfo.InvariantCulture), at = Clk(Arg(args, "--at", "0:25"));
        var grid = inputs.SiteData.Grid; int cell = grid.Cell;
        var g0 = Engine.New(inputs); g0.RunTo(at); var watchers = g0.WatcherIds().ToList();
        var cands = new List<Pt>();
        for (int dx = -rmax; dx <= rmax; dx += cell) for (int dy = -rmax; dy <= rmax; dy += cell)
            {
                var q = new Pt((her.X + dx) / cell * cell + cell / 2 * Math.Sign(her.X + dx), (her.Y + dy) / cell * cell + cell / 2 * Math.Sign(her.Y + dy));
                q = new Pt(her.X + dx, her.Y + dy);
                if (!grid.Free(q) || !G.Within(q, her, rmax) || G.Within(q, her, rmin)) continue;
                bool beside = !grid.Free(q + new Pt(cell, 0)) || !grid.Free(q - new Pt(cell, 0)) || !grid.Free(q + new Pt(0, cell)) || !grid.Free(q - new Pt(0, cell));
                if (!beside || !inputs.SiteData.Sight(q, her, 160)) continue;
                if (watchers.Any(w => g0.InSight(w, q, false))) continue;
                cands.Add(q);
            }
        var heatPlan = Arg(args, "--heat", ""); var traffic = new long[cands.Count];
        if (heatPlan.Length > 0)
        {
            int seeds = int.Parse(Arg(args, "--seeds", "5"), CultureInfo.InvariantCulture), hf = Clk(Arg(args, "--hfrom", "0:00")), ht = Clk(Arg(args, "--hto", "3:00")); var cs = Arg(args, "--counts", ""); var counts = cs.Length > 0 ? DialCli.Parse(cs) : null;
            var res = Par.Map(seeds, BatchCli.Jobs(args), s =>
            {
                var pl = Adaptive.Play(inputs, Adaptive.Load(heatPlan, root), s + 1, out _, counts); var orders = pl.Orders.Select(x => x.Item1).ToList(); var g = Engine.New(inputs, pl.Start); int oi = 0; var t = new long[cands.Count];
                while (g.Tick < pl.Tick && !g.Over)
                {
                    while (oi < orders.Count && orders[oi].Tick == g.Tick) g.Issue(orders[oi++]); g.Step();
                    if (g.Tick < hf || g.Tick > ht) continue;
                    foreach (var u in BodyCheck.Of(g)) { if (u.Id is "scout" or "fixer" or "tech" or "enforcer" || u.Id.StartsWith("op:", StringComparison.Ordinal)) continue; for (int k = 0; k < cands.Count; k++) if (G.Within(u.At, cands[k], 60)) t[k]++; }
                }
                return t;
            });
            foreach (var r in res) if (r.value != null) for (int k = 0; k < cands.Count; k++) traffic[k] += r.value[k];
        }
        foreach (var k in Enumerable.Range(0, cands.Count).OrderBy(k => traffic[k]).ThenBy(k => G.Dist(cands[k], her)))
            Console.WriteLine($"({cands[k].X},{cands[k].Y})  {G.Dist(cands[k], her)} cm  traffic {traffic[k]}");
        return 0;
    }
}
