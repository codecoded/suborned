// suborned-sim stance [--seeds n] [--seed-from k] [--step cm] [--radius cm] (brief 101, Waymark's rigid line for the bands; Motive's and
// Vantage's rule, briefs/101/motive/mast-pressure.md §3): the cell's start is a safe place. On every seed, no watcher that keeps a clock (a
// camera, the guard, the patrol robot, the response's officers and robots: the attention rule's kinds) sees any free point within `radius`
// (GDD §07: the seeded stance, ± 2 m) of a start mark at any tick of an idle mission: no orders, the four stand where they start until it ends.
// Read-only: the engine's own sight (SeesNow, crowd off, since people only ever hide), and a person's near sense (attention.near) by
// distance alone, so the test errs toward seeing. Sight lies only in the watcher's pose (no steam in an idle run), so a pose is tested once.
// Prints one JSON line: the seeds, the points tested, and each watcher's first sighting of a mark (seed, tick, point, sight or near).
// Exit 0 when nothing is seen, 1 when something is.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Suborned.Sim;

static class StanceCli
{
    static readonly string[] Marks = { "crew:scout", "crew:fixer", "crew:tech", "crew:enforcer" };
    sealed record Seen(int Seed, int Tick, string Watcher, string Mark, Pt At, string How);

    static int Arg(string[] a, string k, int d) { int i = Array.IndexOf(a, k); return i > 0 && i + 1 < a.Length ? int.Parse(a[i + 1], CultureInfo.InvariantCulture) : d; }

    public static int Run(Engine.Inputs inputs, string[] args)
    {
        int seeds = Arg(args, "--seeds", 200), from = Arg(args, "--seed-from", 1), step = Arg(args, "--step", 10), radius = Arg(args, "--radius", 200);
        var g0 = Engine.New(inputs); var r = g0.R;
        var points = Marks.Select(m => (mark: m, at: g0.S.Stand(m), pts: Round(g0, g0.S.Stand(m), radius, step).ToArray())).ToArray();
        var found = new List<Seen>[seeds]; var ticks = new int[seeds];
        Parallel.For(0, seeds, k =>
        {
            int seed = from + k; var g = Engine.New(inputs, new Start { Seed = seed }); var mine = new List<Seen>(); var first = new HashSet<string>(StringComparer.Ordinal);
            var cache = new Dictionary<string, List<(string mark, Pt at, string how)>>(StringComparer.Ordinal);
            while (true)
            {
                foreach (var w in g.WatcherIds())
                {
                    var kind = Game.WatcherKind(w); if (kind != "camera" && kind != "guard" && kind != "patrol" && kind != "security") continue;
                    var f = g.ConeFan(w); if (f == null || !f.Live) continue;
                    bool person = kind == "guard" ? r.AttnGuardNear : kind == "security" && !w.StartsWith("robot-", StringComparison.Ordinal) && r.AttnOfficerNear;
                    var key = FormattableString.Invariant($"{w}|{f.At.X},{f.At.Y}|{f.Facing.X},{f.Facing.Y}");
                    if (!cache.TryGetValue(key, out var hits))
                    {
                        hits = new List<(string, Pt, string)>(); int reach = Math.Max(f.Reach, person ? r.AttnNearCm : 0);
                        foreach (var (mark, at, pts) in points)
                        {
                            if (!G.Within(f.At, at, reach + radius)) continue;
                            foreach (var p in pts)
                            {
                                if (g.SeesNow(w, p, false)) { hits.Add((mark, p, "sight")); break; }
                                if (person && G.Within(f.At, p, r.AttnNearCm)) { hits.Add((mark, p, "near")); break; }
                            }
                        }
                        cache[key] = hits;
                    }
                    foreach (var (mark, at, how) in hits) if (first.Add(w + "|" + mark)) mine.Add(new Seen(seed, g.Tick, w, mark, at, how));
                }
                if (g.Over) break;
                g.Step();
            }
            found[k] = mine; ticks[k] = g.Tick;
        });
        var all = found.SelectMany(x => x).ToList(); int seen = found.Count(x => x.Count > 0);
        var o = new JWriter().Obj().Prop("engine", Engine.Version).Prop("seeds", seeds).Prop("from", from).Prop("radius", radius).Prop("step", step)
            .Prop("points", points.Sum(p => p.pts.Length)).Prop("ticks", ticks.Sum()).Prop("seedsSeen", seen).OpenArr("marks");
        foreach (var p in points) o.Obj().Prop("mark", p.mark).Prop("x", p.at.X).Prop("y", p.at.Y).Prop("points", p.pts.Length).EndObj();
        o.EndArr().OpenArr("seen");
        foreach (var s in all.Take(40)) o.Obj().Prop("seed", s.Seed).Prop("tick", s.Tick).Prop("watcher", s.Watcher).Prop("mark", s.Mark).Prop("x", s.At.X).Prop("y", s.At.Y).Prop("how", s.How).EndObj();
        o.EndArr().Prop("seenCount", all.Count);
        Console.WriteLine(o.EndObj().ToString());
        return seen == 0 ? 0 : 1;
    }

    /// <summary>Free points on a lattice within `cm` of a mark (the seeded stance round it), as Keel's N67 lattice.</summary>
    static IEnumerable<Pt> Round(Game g, Pt at, int cm, int step)
    {
        for (int dx = -cm; dx <= cm; dx += step) for (int dy = -cm; dy <= cm; dy += step) { var q = new Pt(at.X + dx, at.Y + dy); if (G.Within(q, at, cm) && g.S.Grid.Free(q)) yield return q; }
    }
}
