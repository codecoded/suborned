// Kept here as the evidence's source (brief 098, Waymark); not built from here: built in a scratch copy of sim/cli (out/scratch/waymark/probe) with `case "heat": return HeatCli.Run(inputs, root, args);` added to Dial.cs, as Vantage's regions probe (briefs/098/vantage/regions-probe.cs).
// Waymark's scratch probe: where the city walks. dial heat [items] [--seeds n] [--counts c] [--box x0,x1,y0,y1] [--from m:ss] [--to m:ss]
// Per 50 cm cell of the box: body-ticks of everyone not of the cell (the city, guards, rivals), over the window, summed over items and seeds; printed as an ASCII map
// on the walk grid (# solid, digits 1-9 log-scaled traffic, . walkable and unused), with the cell's own stands marked (S, F, T, E) where they stood still 5 s or more.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Suborned.Sim;

static class HeatCli
{
    static string Arg(string[] a, string k, string d) { int i = Array.IndexOf(a, k); return i > 0 && i + 1 < a.Length ? a[i + 1] : d; }
    static int Clk(string s) { var p = s.Split(':'); return (int)Math.Round((int.Parse(p[0], CultureInfo.InvariantCulture) * 60 + double.Parse(p[1], CultureInfo.InvariantCulture)) * Game.Hz); }
    public static int Run(Engine.Inputs inputs, string root, string[] args)
    {
        var items = new List<string>();
        for (int i = 2; i < args.Length; i++) { var a = args[i]; if (a.StartsWith("--", StringComparison.Ordinal)) { i++; continue; } items.Add(a); }
        int seeds = int.Parse(Arg(args, "--seeds", "5"), CultureInfo.InvariantCulture); var cs = Arg(args, "--counts", ""); var counts = cs.Length > 0 ? DialCli.Parse(cs) : null;
        var b = Arg(args, "--box", "-1300,-400,-3400,-1800").Split(',').Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray(); int t0 = Clk(Arg(args, "--from", "0:00")), t1 = Clk(Arg(args, "--to", "3:00"));
        var site = inputs.SiteData; var grid = site.Grid; int cell = grid.Cell;
        int nx = (b[1] - b[0]) / 50 + 1, ny = (b[3] - b[2]) / 50 + 1;
        var jobs = new List<(string item, int seed)>(); foreach (var it in items) for (int s = 1; s <= seeds; s++) jobs.Add((it, s));
        var res = Par.Map(jobs.Count, BatchCli.Jobs(args), j =>
        {
            var (item, seed) = jobs[j]; var heat = new long[nx, ny]; var stand = new Dictionary<(int, int), char>();
            Game g;
            List<Order> orders; int until;
            if (item.EndsWith(".adaptive.json", StringComparison.Ordinal)) { var pl = Adaptive.Play(inputs, Adaptive.Load(item, root), seed, out _, counts); orders = pl.Orders.Select(x => x.Item1).ToList(); until = pl.Tick; g = Engine.New(inputs, pl.Start); }
            else { var sc = Script.Parse(File.ReadAllText(item)); sc.Start.Seed = seed; sc.Start.Counts.Clear(); if (counts != null) DialCli.Apply(sc.Start, counts); orders = sc.Orders.OrderBy(o => o.Tick).ToList(); until = sc.Until > 0 ? sc.Until : Game.T(site.Over); g = Engine.New(inputs, sc.Start); }
            int oi = 0; var still = new Dictionary<string, (Pt at, int since)>();
            void look()
            {
                if (g.Tick < t0 || g.Tick > t1) return;
                foreach (var u in BodyCheck.Of(g))
                {
                    int ix = (u.At.X - b[0]) / 50, iy = (u.At.Y - b[2]) / 50; if (u.At.X < b[0] || u.At.Y < b[2] || ix >= nx || iy >= ny) continue;
                    if (u.Id.StartsWith("op:", StringComparison.Ordinal) || u.Id is "scout" or "fixer" or "tech" or "enforcer")
                    {
                        var k = u.Id.Replace("op:", ""); if (!still.TryGetValue(k, out var s0) || G.D2(s0.at, u.At) > 30 * 30) still[k] = (u.At, g.Tick);
                        else if (g.Tick - s0.since >= Game.T(5)) stand[(ix, iy)] = char.ToUpperInvariant(k[0]);
                        continue;
                    }
                    heat[ix, iy]++;
                }
            }
            look();
            while (g.Tick < until && !g.Over) { while (oi < orders.Count && orders[oi].Tick == g.Tick) g.Issue(orders[oi++]); g.Step(); look(); }
            return (heat, stand);
        });
        var tot = new long[nx, ny]; var st = new Dictionary<(int, int), char>(); int errs = 0;
        foreach (var r in res) { if (r.error != null) { errs++; Console.Error.WriteLine(r.error.Message); continue; } for (int i = 0; i < nx; i++) for (int k = 0; k < ny; k++) tot[i, k] += r.value!.heat[i, k]; foreach (var kv in r.value!.stand) st[kv.Key] = kv.Value; }
        long max = 1; foreach (var v in tot) max = Math.Max(max, v);
        Console.WriteLine($"# heat {string.Join(" ", items.Select(System.IO.Path.GetFileName))} x{seeds} seeds {cs} {Report.Clock(t0)}-{Report.Clock(t1)}; max {max} body-ticks a cell; rows x (south), columns y (east) from {b[2]} every 50 cm");
        for (int i = 0; i < nx; i++)
        {
            var sb = new System.Text.StringBuilder(); int x = b[0] + i * 50 + 25;
            for (int k = 0; k < ny; k++)
            {
                int y = b[2] + k * 50 + 25; var p = new Pt(x, y);
                if (st.TryGetValue((i, k), out var c)) { sb.Append(c); continue; }
                if (!grid.Free(p)) { sb.Append('#'); continue; }
                long v = tot[i, k]; sb.Append(v == 0 ? '.' : (char)('0' + Math.Max(1, Math.Min(9, (int)Math.Ceiling(9 * Math.Log(1 + v) / Math.Log(1 + max))))));
            }
            Console.WriteLine($"{x,6} {sb}");
        }
        return errs == 0 ? 0 : 1;
    }
}
