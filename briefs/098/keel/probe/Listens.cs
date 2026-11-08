// Kept here as the evidence's source (brief 098, engine 0.49.0, Keel; briefs/098/keel/walk-law.json); not built from here: built in a scratch copy of sim/cli (out/scratch/keel/probe) with its case added to Dial.cs, as Vantage's regions probe.
// Keel's scratch probe (brief 098, engine 0.49.0): every Listen the adaptive plans give, on seeds: the wait from the order to the act, and whether it sat.
//   dial listens [--seeds n] [--settings s]
using System; using System.Collections.Generic; using System.Globalization; using System.IO; using System.Linq; using Suborned.Sim;
static class ListensCli
{
    public static int Run(Engine.Inputs inputs, string root, string[] args)
    {
        int seeds = 20; int k = Array.IndexOf(args, "--seeds"); if (k > 0) seeds = int.Parse(args[k + 1], CultureInfo.InvariantCulture);
        var dir = System.IO.Path.Combine(root, "contracts/missions/handover/adaptive"); var rows = new List<(string plan, int seed, int wait, bool sat, string chair)>();
        var items = Directory.GetFiles(dir, "*.adaptive.json").OrderBy(x => x, StringComparer.Ordinal).ToList();
        var lk = new object();
        System.Threading.Tasks.Parallel.ForEach(items.SelectMany(f => Enumerable.Range(1, seeds).Select(s => (f, s))), new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 2) }, it =>
        {
            var plan = Adaptive.Load(it.f, root); var pl = Adaptive.Play(inputs, plan, it.s, out _);
            var start = Start.Parse(plan.StartNode); start.Seed = it.s; var g = Engine.New(inputs, start); var orders = pl.Orders.Select(o => o.Item1).OrderBy(o => o.Tick).ToList(); int oi = 0, given = -1;
            var mine = new List<(string, int, int, bool, string)>();
            while (g.Tick < pl.Tick && !g.Over)
            {
                while (oi < orders.Count && orders[oi].Tick == g.Tick) { var o = orders[oi++]; g.Issue(o); if (o.Do == "listen" && o.Op == "scout") given = g.Tick; }
                bool was = g.Ops[0].Busy?.Do == "listen"; g.Step();
                if (!was && g.Ops[0].Busy?.Do == "listen" && given >= 0) { var se = g.SeatedAt("scout"); mine.Add((System.IO.Path.GetFileName(it.f).Replace(".adaptive.json", ""), it.s, g.Tick - given, se.HasValue, se?.chair ?? "")); given = -1; }
            }
            lock (lk) rows.AddRange(mine);
        });
        foreach (var grp in rows.GroupBy(r => r.plan).OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var w = grp.Select(r => r.wait / 10.0).OrderBy(x => x).ToList();
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{grp.Key,-24} listens {grp.Count(),3}  sat {grp.Count(r => r.sat),3}  wait p50 {w[w.Count / 2],6:0.0} p90 {w[(int)(w.Count * 0.9)],6:0.0} max {w.Last(),6:0.0} s"));
        }
        foreach (var r in rows.Where(r => !r.sat && r.plan == "quiet-a").OrderBy(r => r.seed)) Console.WriteLine($"  quiet-a #{r.seed} stood, wait {r.wait / 10.0:0.0} s");
        var all = rows.Select(r => r.wait / 10.0).OrderBy(x => x).ToList();
        if (all.Count > 0) Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"ALL listens {rows.Count} sat {rows.Count(r => r.sat)} wait p50 {all[all.Count / 2]:0.0} p90 {all[(int)(all.Count * 0.9)]:0.0} max {all.Last():0.0}"));
        return 0;
    }
}
