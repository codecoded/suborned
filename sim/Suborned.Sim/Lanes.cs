// Brief 098 step 1 (law L8; Sky 2026-11-07: "our roads are empty, no reason can't have passing traffic"): the zone's lanes, as tools/sim/lanes.ts
// derives them from the plan (sim/data/handover.lanes.json, suborned.sim.lanes/1): directed lanes on the right of each road, junction boxes with
// their turns, and the mouths where roads leave the window. Read only when a start sets cars (the dial); checked against the site's drive grid at
// load (unknown = reject). From it, once per site and read-only after: the routes from every mouth to every other (the shortest by length, ties by
// lane id) and which turns of a junction conflict (their paths pass nearer than two of the widest car's half widths and the clearance; turns out
// of one lane never conflict, its cars follow one another). Integers only.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>A polyline walked by millimetres (as Path), with its chainage open for the traffic's projections.</summary>
    public sealed class Polyline
    {
        public readonly Pt[] P; public readonly long[] Cum; public readonly long Length; // mm
        public Polyline(Pt[] pts)
        {
            if (pts.Length < 2) throw new FormatException("a polyline has two points at least");
            P = pts; Cum = new long[pts.Length];
            for (int i = 1; i < pts.Length; i++) Cum[i] = Cum[i - 1] + G.Isqrt(G.D2(pts[i - 1], pts[i]) * 100);
            Length = Cum[pts.Length - 1];
        }
        int Seg(long mm) { int i = 1; while (i < P.Length - 1 && Cum[i] < mm) i++; return i; }
        public Pt At(long mm)
        {
            if (mm <= 0) return P[0]; if (mm >= Length) return P[P.Length - 1];
            int i = Seg(mm); return G.Lerp(P[i - 1], P[i], mm - Cum[i - 1], Math.Max(1, Cum[i] - Cum[i - 1]));
        }
        /// <summary>The direction of travel at mm (the segment's vector; zero-length segments skipped).</summary>
        public Pt Heading(long mm)
        {
            int i = Seg(Math.Max(0, Math.Min(Length, mm))); int j = i; while (j < P.Length - 1 && P[j] == P[j - 1]) j++; while (j > 1 && P[j] == P[j - 1]) j--;
            var h = P[j] - P[j - 1]; return h.X == 0 && h.Y == 0 ? new Pt(0, 100) : h;
        }
        /// <summary>The points at from, from + step, … (mm; at most n, none past the end), each with its segment's direction, in order, until `f`
        /// says stop (true). One walk along the segments, so a look ahead costs its samples, not a search per sample.</summary>
        public void Walk(long from, long step, int n, Func<int, Pt, Pt, bool> f)
        {
            if (from < 0) from = 0; int i = Seg(from);
            for (int k = 0; k < n; k++)
            {
                long mm = from + k * step; if (mm > Length) return;
                while (i < P.Length - 1 && Cum[i] < mm) i++;
                Pt a = P[i - 1], b = P[i]; var h = b - a;
                if (h.X == 0 && h.Y == 0) h = Heading(mm);
                if (f(k, G.Lerp(a, b, mm - Cum[i - 1], Math.Max(1, Cum[i] - Cum[i - 1])), h)) return;
            }
        }
        /// <summary>The nearest the two polylines come (cm), sampled every `stepCm`.</summary>
        public static long Nearest(Polyline a, Polyline b, int stepCm)
        {
            long best = long.MaxValue; long step = stepCm * 10L;
            for (long s = 0; s <= a.Length + step - 1; s += step) { var p = a.At(Math.Min(s, a.Length)); for (long t = 0; t <= b.Length + step - 1; t += step) { long d = G.D2(p, b.At(Math.Min(t, b.Length))); if (d < best) best = d; } }
            return G.Isqrt(best);
        }
    }

    /// <summary>A directed lane: its road, where it runs from and to (a junction or a mouth), its speed (cm/s), how far its line is from the road's
    /// centreline and the half carriageway's width (cm), and the turns out of it.</summary>
    public sealed class Lane { public string Id = "", Road = "", From = "", To = ""; public int Speed, Offset, Half; public Polyline Line = null!; public readonly List<Turn> Out = new List<Turn>(); public Lane? Twin; } // Twin: the same road's lane the other way
    public sealed class Turn { public string Id = ""; public Junction J = null!; public Lane From = null!, To = null!; public Polyline Line = null!; public int Index; }
    /// <summary>A junction: its box, its turns, which turns conflict, and for a conflicting pair (a, b) how far along a (mm) a car's rear must be
    /// before it is clear of b's way (ClearAt[a, b]: past the last point of a near b).</summary>
    public sealed class Junction { public string Id = ""; public Pt[] Box = Array.Empty<Pt>(); public readonly List<Turn> Turns = new List<Turn>(); public bool[,] Conflict = new bool[0, 0]; public long[,] ClearAt = new long[0, 0]; }
    public sealed class Mouth { public string Id = "", Road = ""; public Pt Gate; public readonly List<Lane> In = new List<Lane>(), Out = new List<Lane>(); }

    /// <summary>One way through the zone: from a mouth's lane in to another mouth's lane out, its lanes and turns end to end as one polyline, the
    /// stretch of each junction on it (from its stop line to its far side), and where it crosses into and out of the window (mm).</summary>
    public sealed class TrafficRoute
    {
        public string Id = ""; public Mouth From = null!, To = null!; public Polyline Path = null!;
        public readonly List<(Lane lane, long from, long to)> Lanes = new List<(Lane, long, long)>();
        public readonly List<(Turn turn, long enter, long exit)> Turns = new List<(Turn, long, long)>();
        public long InMm, OutMm;
        /// <summary>The lane (or turn) the route is on at mm: the lane index, or -1 inside a turn.</summary>
        public int LaneAt(long mm) { for (int k = 0; k < Lanes.Count; k++) if (mm >= Lanes[k].from && mm < Lanes[k].to) return k; return mm >= Path.Length && Lanes.Count > 0 ? Lanes.Count - 1 : -1; }
    }

    public sealed class LaneNet
    {
        public readonly string Text; public readonly ulong TextHash; public readonly int RunwayCm, X0, Y0, X1, Y1;
        public readonly SortedDictionary<string, Lane> Lanes = new SortedDictionary<string, Lane>(StringComparer.Ordinal);
        public readonly List<Junction> Junctions = new List<Junction>();
        public readonly List<Mouth> Mouths = new List<Mouth>();
        public const string File = "sim/data/handover.lanes.json";

        public LaneNet(string json, Site site)
        {
            Text = JNode.Canon(json); TextHash = Hash.Of(Text);
            var n = JNode.Parse(Text, "lanes");
            n.Only("schema", "from", "frame", "window", "runwayCm", "laneWidthCm", "marginCm", "minLinkCm", "turnRadiusCm", "src", "lanes", "junctions", "mouths");
            if (n["schema"].Str() != "suborned.sim.lanes/1") throw new FormatException("lanes schema is not suborned.sim.lanes/1 (tools/sim/lanes.ts)");
            var w = n["window"]; w.Only("x0", "y0", "x1", "y1"); X0 = w["x0"].Int(); Y0 = w["y0"].Int(); X1 = w["x1"].Int(); Y1 = w["y1"].Int(); RunwayCm = n["runwayCm"].Int();
            var g = site.Drive;
            if (X0 != g.X0 || Y0 != g.Y0 || X1 != g.X0 + g.Nx * g.Cell || Y1 != g.Y0 + g.Ny * g.Cell) throw new FormatException("lanes.window is not the site's walk window (tools/sim/lanes.ts)");
            foreach (var x in n["lanes"].Items)
            {
                x.Only("id", "road", "class", "dir", "k", "from", "to", "speed", "offset", "half", "line");
                var l = new Lane { Id = x["id"].Str(), Road = x["road"].Str(), From = x["from"].Str(), To = x["to"].Str(), Speed = x["speed"].Int(), Offset = x["offset"].Int(), Half = x["half"].Int(), Line = new Polyline(Pts(x["line"])) };
                if (l.Speed < 100 || l.Speed > 5000) throw new FormatException($"{x.Path}.speed: 100..5000 cm/s");
                if (l.Offset <= 0 || l.Half <= l.Offset) throw new FormatException($"{x.Path}: a lane lies inside its half of the road (0 < offset < half)");
                if (Lanes.ContainsKey(l.Id)) throw new FormatException($"{x.Path}: lane {l.Id} repeats");
                OnDrive(l.Id, l.Line, site); Lanes[l.Id] = l;
            }
            foreach (var x in n["junctions"].Items)
            {
                x.Only("id", "nodes", "box", "turns");
                var j = new Junction { Id = x["id"].Str(), Box = Pts(x["box"]) }; x["nodes"].Strs();
                foreach (var t in x["turns"].Items)
                {
                    t.Only("id", "from", "to", "line");
                    var tu = new Turn { Id = t["id"].Str(), J = j, From = LaneOf(t["from"]), To = LaneOf(t["to"]), Line = new Polyline(Pts(t["line"])), Index = j.Turns.Count };
                    if (tu.From.To != j.Id || tu.To.From != j.Id) throw new FormatException($"{t.Path}: a turn of {j.Id} joins a lane into it to a lane out of it");
                    if (tu.Line.P[0] != tu.From.Line.P[tu.From.Line.P.Length - 1] || tu.Line.P[tu.Line.P.Length - 1] != tu.To.Line.P[0]) throw new FormatException($"{t.Path}: a turn starts where its lane in ends and ends where its lane out starts");
                    OnDrive(tu.Id, tu.Line, site); j.Turns.Add(tu); tu.From.Out.Add(tu);
                }
                Junctions.Add(j);
            }
            foreach (var x in n["mouths"].Items)
            {
                x.Only("id", "road", "gate", "in", "out");
                var m = new Mouth { Id = x["id"].Str(), Road = x["road"].Str(), Gate = Pt.Of(x["gate"]) };
                foreach (var id in x["in"].Strs()) { var l = Lanes.TryGetValue(id, out var v) ? v : throw new FormatException($"{x.Path}.in: no lane {id}"); if (l.From != m.Id) throw new FormatException($"{x.Path}.in: {id} does not start at {m.Id}"); m.In.Add(l); }
                foreach (var id in x["out"].Strs()) { var l = Lanes.TryGetValue(id, out var v) ? v : throw new FormatException($"{x.Path}.out: no lane {id}"); if (l.To != m.Id) throw new FormatException($"{x.Path}.out: {id} does not end at {m.Id}"); m.Out.Add(l); }
                if (m.In.Count == 0 || m.Out.Count == 0) throw new FormatException($"{x.Path}: a mouth has a way in and a way out");
                Mouths.Add(m);
            }
            if (Mouths.Count < 2) throw new FormatException("lanes: traffic needs two mouths at least");
            foreach (var l in Lanes.Values) { var dir = l.Id[l.Id.Length - 2]; var tw = l.Id.Substring(0, l.Id.Length - 2) + (dir == '+' ? '-' : '+') + l.Id[l.Id.Length - 1]; if (Lanes.TryGetValue(tw, out var t)) l.Twin = t; }
            foreach (var l in Lanes.Values) if (l.Out.Count == 0 && !Mouths.Exists(m => m.Out.Contains(l))) throw new FormatException($"lanes: {l.Id} leads nowhere (no turn out of it and no mouth)");
            foreach (var m in Mouths) foreach (var o in Mouths) if (m != o && Route(m, o) == null) throw new FormatException($"lanes: no way from {m.Id} to {o.Id}");
            Lane LaneOf(JNode id) => Lanes.TryGetValue(id.Str(), out var v) ? v : throw new FormatException($"{id.Path}: no lane {id.Str()}");
        }
        static Pt[] Pts(JNode n) { var l = new List<Pt>(); foreach (var q in n.Items) l.Add(Pt.Of(q)); return l.ToArray(); }
        /// <summary>Every point of a lane or turn inside the window, every 25 cm, on a drivable cell of the site (rulebook car.drive; unknown = reject).</summary>
        void OnDrive(string id, Polyline p, Site site)
        {
            for (long mm = 0; mm <= p.Length; mm += 250)
            {
                var q = p.At(mm); if (q.X < X0 || q.Y < Y0 || q.X >= X1 || q.Y >= Y1) continue;
                if (!site.Drive.Free(q)) throw new FormatException($"lanes: {id} leaves the drive grid at {q} (tools/sim/lanes.ts; rulebook car.drive)");
            }
        }

        /// <summary>Which turns of each junction conflict for cars no wider than `maxWCm` keeping `clearCm` apart: their paths come nearer than
        /// maxW + 2·clear; two turns out of the same lane never do (its cars follow one another). For each conflicting pair, how far along each a car
        /// has cleared the other's way (the last point of it that near the other, sampled every 25 cm). Computed once per width (lock: one net
        /// serves every game on these inputs).</summary>
        public void Conflicts(int maxWCm, int clearCm)
        {
            lock (routes)
            {
                if (conflictsFor == maxWCm * 10000L + clearCm) return;
                long near = maxWCm + 2L * clearCm;
                foreach (var j in Junctions)
                {
                    int n = j.Turns.Count; var c = new bool[n, n]; var at = new long[n, n];
                    for (int a = 0; a < n; a++) for (int b = a + 1; b < n; b++)
                    {
                        var ta = j.Turns[a]; var tb = j.Turns[b];
                        bool x = ta.From != tb.From && (ta.To == tb.To || Polyline.Nearest(ta.Line, tb.Line, 25) < near);
                        c[a, b] = c[b, a] = x;
                        if (x) { at[a, b] = ta.To == tb.To ? ta.Line.Length : LastNear(ta.Line, tb.Line, near); at[b, a] = ta.To == tb.To ? tb.Line.Length : LastNear(tb.Line, ta.Line, near); }
                    }
                    j.ClearAt = at; j.Conflict = c;
                }
                conflictsFor = maxWCm * 10000L + clearCm;
            }
        }
        long conflictsFor = -1;
        /// <summary>The last mm along a whose point lies within `near` cm of b (sampled every 25 cm on both).</summary>
        static long LastNear(Polyline a, Polyline b, long near)
        {
            long last = 0, n2 = near * near;
            for (long s = 0; s <= a.Length; s += 250) { var p = a.At(s); for (long t = 0; t <= b.Length + 249; t += 250) if (G.D2(p, b.At(Math.Min(t, b.Length))) < n2) { last = s; break; } }
            return Math.Min(a.Length, last + 250);
        }

        readonly Dictionary<string, TrafficRoute?> routes = new Dictionary<string, TrafficRoute?>(StringComparer.Ordinal);
        /// <summary>The way from a mouth in to a mouth out: the shortest by length over lanes and turns (Dijkstra, ties by lane id), built once.</summary>
        public TrafficRoute? Route(Mouth from, Mouth to)
        {
            var seeds = new List<(Lane lane, long cost, Turn? via)>(); foreach (var l in from.In) seeds.Add((l, l.Line.Length, null));
            return Cached(from.Id + ">" + to.Id, from, to, null, seeds);
        }
        /// <summary>The way on from the start of lane `from`, through its turn `first`, to a mouth out (a driver taking another way at a junction).</summary>
        public TrafficRoute? RouteVia(Lane from, Turn first, Mouth to)
        {
            if (first.From != from) return null;
            var seeds = new List<(Lane lane, long cost, Turn? via)> { (first.To, from.Line.Length + first.Line.Length + first.To.Line.Length, first) };
            var m = Mouths.Find(x => x.In.Contains(from)) ?? new Mouth { Id = "lane:" + from.Id };
            return Cached("via:" + from.Id + ">" + first.Id + ">" + to.Id, m, to, from, seeds);
        }
        TrafficRoute? Cached(string key, Mouth from, Mouth to, Lane? head, List<(Lane lane, long cost, Turn? via)> seeds)
        {
            lock (routes) { if (routes.TryGetValue(key, out var r)) return r; }
            var best = new Dictionary<Lane, long>(); var prev = new Dictionary<Lane, Turn?>(); var open = new SortedSet<(long d, string id)>();
            foreach (var (l, cost, via) in seeds) { if (best.TryGetValue(l, out var b) && b <= cost) continue; best[l] = cost; prev[l] = via; open.Add((cost, l.Id)); }
            Lane? end = null;
            while (open.Count > 0)
            {
                var (d, id) = open.Min; open.Remove(open.Min); var l = Lanes[id];
                if (d > best[l]) continue;
                if (to.Out.Contains(l)) { end = l; break; }
                foreach (var t in l.Out)
                {
                    long nd = d + t.Line.Length + t.To.Line.Length;
                    if (best.TryGetValue(t.To, out var od) && od <= nd) continue;
                    if (best.ContainsKey(t.To)) open.Remove((od, t.To.Id));
                    best[t.To] = nd; prev[t.To] = t; open.Add((nd, t.To.Id));
                }
            }
            TrafficRoute? route = null;
            if (end != null)
            {
                var chain = new List<(Lane lane, Turn? turn)>();
                for (var l = end; l != null;) { var t = prev[l]; chain.Insert(0, (l, t)); l = t?.From; if (l != null && l == head) { chain.Insert(0, (l, null)); break; } }
                var pts = new List<Pt>(); route = new TrafficRoute { Id = key, From = from, To = to };
                void add(Polyline p) { foreach (var q in p.P) if (pts.Count == 0 || pts[pts.Count - 1] != q) pts.Add(q); }
                long at = 0;
                foreach (var (lane, turn) in chain)
                {
                    if (turn != null) { route.Turns.Add((turn, at, at + turn.Line.Length)); add(turn.Line); at += turn.Line.Length; }
                    route.Lanes.Add((lane, at, at + lane.Line.Length)); add(lane.Line); at += lane.Line.Length;
                }
                route.Path = new Polyline(pts.ToArray());
                route.InMm = Cross(route.Path, true); route.OutMm = Cross(route.Path, false);
            }
            lock (routes) routes[key] = route;
            return route;
        }
        /// <summary>The first (in) or last (out) mm of a path inside the window.</summary>
        long Cross(Polyline p, bool first)
        {
            bool inside(long mm) { var q = p.At(mm); return q.X >= X0 && q.Y >= Y0 && q.X < X1 && q.Y < Y1; }
            if (first) { for (long mm = 0; mm <= p.Length; mm += 100) if (inside(mm)) return mm; return p.Length; }
            for (long mm = p.Length; mm >= 0; mm -= 100) if (inside(mm)) return mm; return 0;
        }
    }
}
