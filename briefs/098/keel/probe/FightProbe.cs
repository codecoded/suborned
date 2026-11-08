// Kept here as the evidence's source (brief 098, engine 0.50.0; not built from here: built in a scratch copy of sim/cli, out/scratch/keel/probe, with
// `case "fight": return FightProbe.Run(inputs, root, args);` added to Dial.cs, as Vantage's regions probe). Keel's probe: one adaptive plan on a seed, every tick in a window, the named
// hostiles' fight state, and which cover near them shields them from each operative.
//   dial fight <plan.adaptive.json> --seed n --from m:ss --to m:ss --ids rival-1,rival-4 [--every ticks]
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Suborned.Sim;

static class FightProbe
{
    static string Arg(string[] a, string k, string d) { int i = Array.IndexOf(a, k); return i > 0 && i + 1 < a.Length ? a[i + 1] : d; }
    static int Clk(string s) { var p = s.Split(':'); return (int)Math.Round((int.Parse(p[0], CultureInfo.InvariantCulture) * 60 + double.Parse(p[1], CultureInfo.InvariantCulture)) * Game.Hz); }
    public static int Run(Engine.Inputs inputs, string root, string[] args)
    {
        var item = args[2]; int seed = int.Parse(Arg(args, "--seed", "1"), CultureInfo.InvariantCulture);
        var cs = Arg(args, "--counts", ""); var counts = cs.Length > 0 ? DialCli.Parse(cs) : null;
        int t0 = Clk(Arg(args, "--from", "0:00")), t1 = Clk(Arg(args, "--to", "9:00")), every = int.Parse(Arg(args, "--every", "1"), CultureInfo.InvariantCulture);
        var ids = Arg(args, "--ids", "rival-1").Split(',', StringSplitOptions.RemoveEmptyEntries);
        Script sc;
        if (item.EndsWith(".adaptive.json", StringComparison.Ordinal))
        {
            var pl = Adaptive.Play(inputs, Adaptive.Load(item, root), seed, out var x, counts);
            Console.WriteLine($"# {System.IO.Path.GetFileName(item)} #{seed} {cs}: {pl.Result} {pl.FailCode} at {Report.Clock(pl.Tick)}");
            sc = Script.Parse(new JWriter().Obj().Prop("schema", "suborned.sim.script/1").Prop("mission", "handover").Key("start").Raw(pl.Start.Canonical()).OpenArr("orders").EndArr().EndObj().ToString());
            foreach (var (o, _, _, _) in pl.Orders) sc.Orders.Add(o); sc.Until = pl.Tick;
        }
        else { sc = Script.Parse(System.IO.File.ReadAllText(item)); sc.Start.Seed = seed; sc.Start.Counts.Clear(); if (counts != null) DialCli.Apply(sc.Start, counts); if (sc.Until <= 0) sc.Until = Game.T(inputs.SiteData.Over); }
        var g = Engine.New(inputs, sc.Start); var orders = sc.Orders.Select((o, k) => (o, k)).OrderBy(q => q.o.Tick).ThenBy(q => q.k).Select(q => q.o).ToList(); int oi = 0;
        var r = g.R; int li = 0;
        void look()
        {
            for (; li < g.Log.Count; li++) if (g.Log[li].Tick >= t0 && g.Log[li].Tick <= t1 && (g.Log[li].Code.StartsWith("ai.", StringComparison.Ordinal) || g.Log[li].Code.StartsWith("shot.", StringComparison.Ordinal))) Console.WriteLine($"  {Report.Clock(g.Log[li].Tick)} {g.Log[li].Code} {g.Log[li].Text}");
            if (g.Tick < t0 || g.Tick > t1 || (g.Tick - t0) % every != 0) return;
            int sec = g.Tick / Game.Hz;
            foreach (var id in ids)
            {
                var h = g.Hostiles.FirstOrDefault(q => q.Id == id); if (h == null || !h.Present) continue;
                var gun = h.Gun; var p = h.Pos(g.Tick); var raw = h.RawPos(g.Tick);
                var parts = new List<string> { $"{Report.Clock(g.Tick)} {id} st={gun.St} pos=({p.X},{p.Y}) raw=({raw.X},{raw.Y}) at=({h.At.X},{h.At.Y}) mv={(h.Moving(g.Tick) ? 1 : 0)} why={h.Why} cl={(h.Closing ? 1 : 0)} cov={gun.Cover} stepAt={(gun.StepAt < 0 ? "-" : Report.Clock(gun.StepAt))} nofoe={(gun.NoFoeSince < 0 ? "-" : Report.Clock(gun.NoFoeSince))} unprot={(gun.UnprotSince < 0 ? "-" : Report.Clock(gun.UnprotSince))} kite={(gun.KiteAt < 0 ? "-" : gun.KiteBy + "@" + Report.Clock(gun.KiteSeen))} tgt={gun.Target} hp={gun.Health} eff={gun.W?.EffCm}" };
                foreach (var o in g.Ops)
                {
                    if (o.Down || o.Inside) continue;
                    int m = CoverRules.Modifier(g.S.Cover, p, o.Pos, sec, r.CoverSnapCm, r.PointBlankCm, r.CoverQFull, r.CoverQPartial, r.ModCover, r.ModCoverPartial);
                    parts.Add($"{Op.Name(o.Id)[0]}:{G.Isqrt(G.D2(p, o.Pos)) / 100.0:0.0}m mod{m}");
                }
                foreach (var c in g.S.Cover)
                {
                    long d = G.Isqrt(G.D2(p, c.At)); if (d > 300) continue;
                    var sh = string.Join("", g.Ops.Where(o => !o.Down && !o.Inside && CoverRules.Protects(c, o.Pos, sec, r.PointBlankCm)).Select(o => Op.Name(o.Id)[0]));
                    parts.Add($"[{c.Id} {d}cm q{c.Q} shields:{sh}]");
                }
                Console.WriteLine(string.Join("  ", parts));
            }
        }
        look();
        while (g.Tick < sc.Until && !g.Over) { while (oi < orders.Count && orders[oi].Tick == g.Tick) g.Issue(orders[oi++]); g.Step(); look(); if (g.Tick > t1) break; }
        return 0;
    }
}
