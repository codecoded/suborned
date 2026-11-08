// Kept here as the evidence's source (brief 098, Waymark); not built from here: built in a scratch copy of sim/cli (out/scratch/waymark/probe) with `case "track": return TrackCli.Run(inputs, root, args);` added to Dial.cs, as Vantage's regions probe (briefs/098/vantage/regions-probe.cs).
// Waymark's scratch probe (brief 098 cleanup, not built from here): play one item (an adaptive plan or a script) on a seed under counts, and print who stands where.
//   dial track <item> [--seed n] [--counts guards=32] [--from m:ss] [--to m:ss] [--every s] [--near x,y,r] [--ids a,b] [--log regex]
// Each sampled tick: the four ops (pos, busy act, walk), the named ids, and every upright body within r cm of (x,y) with its held count.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Suborned.Sim;

static class TrackCli
{
    static string Arg(string[] a, string k, string d) { int i = Array.IndexOf(a, k); return i > 0 && i + 1 < a.Length ? a[i + 1] : d; }
    static int Clk(string s) { var p = s.Split(':'); return (int)Math.Round((int.Parse(p[0], CultureInfo.InvariantCulture) * 60 + double.Parse(p[1], CultureInfo.InvariantCulture)) * Game.Hz); }
    public static int Run(Engine.Inputs inputs, string root, string[] args)
    {
        var item = args[2]; int seed = int.Parse(Arg(args, "--seed", "1"), CultureInfo.InvariantCulture);
        var cs = Arg(args, "--counts", ""); var counts = cs.Length > 0 ? DialCli.Parse(cs) : null;
        int t0 = Clk(Arg(args, "--from", "0:00")), t1 = Clk(Arg(args, "--to", "9:00")), every = (int)Math.Round(double.Parse(Arg(args, "--every", "1"), CultureInfo.InvariantCulture) * Game.Hz);
        var near = Arg(args, "--near", ""); Pt? nc = null; long nr2 = 0; if (near.Length > 0) { var n = near.Split(','); nc = new Pt(int.Parse(n[0], CultureInfo.InvariantCulture), int.Parse(n[1], CultureInfo.InvariantCulture)); long r = long.Parse(n[2], CultureInfo.InvariantCulture); nr2 = r * r; }
        var ids = Arg(args, "--ids", "").Split(',', StringSplitOptions.RemoveEmptyEntries); var logRx = new Regex(Arg(args, "--log", "^$")); var card = Arg(args, "--card", "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        Script sc;
        if (item.EndsWith(".adaptive.json", StringComparison.Ordinal))
        {
            var pl = Adaptive.Play(inputs, Adaptive.Load(item, root), seed, out var x, counts);
            Console.WriteLine($"# {System.IO.Path.GetFileName(item)} #{seed} {cs}: {pl.Result} {pl.FailCode} at {Report.Clock(pl.Tick)}; given {string.Join(" ", x.Given.Select(kv => kv.Key + "@" + Report.Clock(kv.Value)))}; never {string.Join(",", x.Never)}");
            sc = Script.Parse(new JWriter().Obj().Prop("schema", "suborned.sim.script/1").Prop("mission", "handover").Key("start").Raw(pl.Start.Canonical()).OpenArr("orders").EndArr().EndObj().ToString());
            foreach (var (o, _, _, _) in pl.Orders) sc.Orders.Add(o); sc.Until = pl.Tick;
        }
        else { sc = Script.Parse(File.ReadAllText(item)); sc.Start.Seed = seed; sc.Start.Counts.Clear(); if (counts != null) DialCli.Apply(sc.Start, counts); }
        var g = Engine.New(inputs, sc.Start); var orders = sc.Orders.Select((o, k) => (o, k)).OrderBy(q => q.o.Tick).ThenBy(q => q.k).Select(q => q.o).ToList(); int oi = 0, until = sc.Until > 0 ? sc.Until : Game.T(inputs.SiteData.Over);
        var stuck = new Dictionary<string, int>(StringComparer.Ordinal); int li = 0;
        void look()
        {
            for (; li < g.Log.Count; li++) if (logRx.IsMatch(g.Log[li].Code)) Console.WriteLine($"  {Report.Clock(g.Log[li].Tick)} {g.Log[li].Code} {g.Log[li].Text}");
            if (g.Tick < t0 || g.Tick > t1 || (g.Tick - t0) % every != 0) return;
            g.BodyStuckInto(stuck); var up = BodyCheck.Of(g);
            var line = new List<string>();
            foreach (var o in g.Ops) line.Add($"{Op.Name(o.Id)}({o.Pos.X},{o.Pos.Y}){(o.Busy != null ? "[" + o.Busy.Do + "]" : "")}{(o.Walk != null ? "~" : "")}{(o.Inside ? "in" : "")}");
            foreach (var id in ids) { var p = g.PosOf(id); line.Add($"{id}{(p is Pt q ? $"({q.X},{q.Y})" : "(-)")}{(stuck.TryGetValue(id, out var h) && h > 0 ? $" held{h / 10.0:0.0}s" : "")}"); }
            if (nc is Pt c) foreach (var u in up) if (G.D2(u.At, c) <= nr2 && Array.IndexOf(ids, u.Id) < 0 && !u.Id.StartsWith("op:", StringComparison.Ordinal)) line.Add($"{u.Id}({u.At.X},{u.At.Y}){(stuck.TryGetValue(u.Id, out var h) && h > 0 ? $" held{h / 10.0:0.0}s" : "")}");
            if (card.Length == 2) { var q = g.QuoteTakedown(card[0], card[1]); line.Add($"| card aware {q.Aware} people [{string.Join(" ", q.People.Select(w => w.Item1 + ":" + w.Item3))}] coming [{string.Join(" ", q.Coming.Select(w => w.Item1 + ":" + w.Item3 + "@" + w.Item4))}]"); }
            Console.WriteLine($"{Report.Clock(g.Tick)} " + string.Join("  ", line));
        }
        look();
        while (g.Tick < until && !g.Over) { while (oi < orders.Count && orders[oi].Tick == g.Tick) g.Issue(orders[oi++]); g.Step(); look(); }
        g.Finish(); Console.WriteLine($"# end {g.Result} {g.FailCode} at {Report.Clock(g.Tick)}");
        return 0;
    }
}
