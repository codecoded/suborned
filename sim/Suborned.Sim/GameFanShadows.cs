// The shadows in a fan (split from GameSight.cs, brief 101; files stay small, brief 084): brief 055 §7 (perception.mount), an eye sees over a solid
// lower than it (a lens over a planting: a stretch behind it hidden) or under one raised above it; a person on the street under an eye over their head
// hides a short stretch behind them. A point is drawn in sight when it is in the fan's polygon and in no shadow, and that is the engine's own answer
// (SightTests): engine 0.52.0 reads each target's own floor along a shadow's rays (HiddenTo). Integers only, read-only.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>Brief 055 §7: the head and chest planes a target behind this occluder stands at (its base, the floor it stands on, + eye or chest).</summary>
        (int head, int chest) TargetPlanes(Occluder oc, Pt from)
        {
            // the floor just behind the solid, seen from `from` (60 cm past its middle along the line): where a hidden target would stand
            long cx = 0, cy = 0; foreach (var q in oc.Poly) { cx += q.X; cy += q.Y; } var c = new Pt((int)(cx / oc.Poly.Length), (int)(cy / oc.Poly.Length));
            long dx = c.X - from.X, dy = c.Y - from.Y, len = Math.Max(1, G.Isqrt(dx * dx + dy * dy));
            var behind = new Pt(c.X + (int)(dx * 60 / len), c.Y + (int)(dy * 60 / len));
            int f = FloorAt(behind); // engine 0.49.0: the floor a target stands on there (was the higher of it and the solid's base: the police post, measured as built, stands 12 cm up on its plinth, so the fan put heads 12 cm high and ended its shadow short)
            return (f + eye, f + R.MountChestCm);
        }
        /// <summary>Brief 055 §7: from an eye at z, this solid hides every head and chest behind it (it rises over the eye and the head, and its base
        /// is under the eye and the chest): the fan cuts its rays there.</summary>
        bool FullBlocker(Occluder oc, int z, Pt from)
        {
            var (hd, ch) = TargetPlanes(oc, from); int top = oc.Z + oc.H;
            return oc.Z <= Math.Min(z, ch) && top >= Math.Max(z, hd);
        }
        /// <summary>Brief 055 §7: the shadows of the solids this watcher sees over (a lens above a planting: a stretch behind it, the footprint scaled
        /// from 1 out to where a target on its own floor shows over the top, HiddenTo) or under (a low eye below a planting: from (head − eye)/(top − eye)
        /// out to the reach): each the convex hull of the footprint and its far ends, within the shape's reach. Integers only.</summary>
        Pt[][] FanShadows(string watcher, Shape s, bool crowd)
        {
            var e = WatcherEye(watcher, s.At); if (!e.HasValue) return Array.Empty<Pt[]>();
            int z = e.Value.z; var o = s.At; long reach = Math.Max(Math.Max(s.Reach, s.Near), FarthestSight(s)); if (s.Sweep) reach = Math.Max(reach, s.Depth + s.HalfBase + (long)s.Depth * s.PerMille / 1000); // engine 0.52.0: to the cone's far corners (a mast's 36.6 m), not its 30 m ahead
            var l = new List<Pt[]>();
            foreach (var oc in S.Occluders)
            {
                if (!oc.BoxMeets(o.X - (int)reach, o.Y - (int)reach, o.X + (int)reach, o.Y + (int)reach) || FullBlocker(oc, z, o) || G.InPoly(o, oc.Poly)) continue;
                var (hd, ch) = TargetPlanes(oc, o); int top = oc.Z + oc.H;
                long num0, den0, num1, den1; // the two scales, as fractions
                if (z > top && top >= hd && oc.Z <= ch)
                {
                    // each footprint corner's ray on its own: the stretch ends where a target standing there shows over the top, on its own floor
                    // (engine 0.52.0, brief 101: the floor may rise behind the solid, the Office landing, or fall, the carriageway behind the square's
                    // police post, where the mast's north head saw nobody 6 m further than the fan drew)
                    var vp = new List<Pt>(); long cap = 2 * reach;
                    foreach (var v in oc.Poly) { vp.Add(v); vp.Add(AlongRay(o, v, HiddenTo(o, z, top, v, cap))); }
                    var vh = Hull(vp); if (vh.Length >= 3) l.Add(vh);
                    continue;
                }
                else if (z < hd && top > z && top < hd && oc.Z <= z) { num0 = hd - z; den0 = top - z; num1 = 0; den1 = 0; }    // seen under: from far on
                else continue;
                long near2 = long.MaxValue; foreach (var q in oc.Poly) near2 = Math.Min(near2, G.D2(o, q));
                long nearD = Math.Max(1, G.Isqrt(near2));
                if (den1 == 0) { num1 = reach * 2; den1 = nearD; } // out past the reach
                if (num0 * nearD > reach * den0 * 2) continue;     // the shadow starts beyond the reach
                var pts = new List<Pt>();
                foreach (var q in oc.Poly) foreach (var (n, d) in new[] { (num0, den0), (num1, den1) })
                        pts.Add(new Pt(o.X + (int)((long)(q.X - o.X) * n / d), o.Y + (int)((long)(q.Y - o.Y) * n / d)));
                var hull = Hull(pts); if (hull.Length >= 3) l.Add(hull);
            }
            // a person on the street under an eye over their head top: a short stretch behind them (brief 055 §7 rule 4), CrowdBlocksZ's own shape
            // (engine 0.52.0): the wedge of lines passing within crowd.occlude of them, from where a target stands two bodies' width from them (a chord
            // across each half of the wedge; the engine's circle bulges 2 cm past it) out to where the line clears their head top, on that target's own
            // floor: a person on the Office landing hides the pavement below it longer, and past the landing's edge again. Each half of the wedge is
            // marched on its own and drawn as one piece a hidden stretch. It was their octagon scaled along the ray, which shaded from one body's
            // width past them and dropped the wedge's edges (SightTests, the square's mast)
            if (crowd && People2 != null && Start.Crowd)
                foreach (var w in Walkers)
                {
                    var wp = CrowdPos(w); if (!wp.HasValue || !G.Within(o, wp.Value, (int)reach)) continue;
                    int topw = FloorAt(wp.Value) + R.MountHeadTopCm; if (z <= topw) continue;
                    long d = G.Isqrt(G.D2(o, wp.Value)), occ = R.CrowdOccludeCm; if (d <= 2 * occ) continue;
                    long ux = wp.Value.X - o.X, uy = wp.Value.Y - o.Y, c = Math.Max(1, G.Isqrt(d * d - occ * occ)); // the wedge's half-width x cm past them: occ·(d + x)/c
                    long inner = 2 * occ, outer = G.Isqrt(3 * occ * occ); // two bodies' width from them, at the wedge's middle and at its edge (occ·√3)
                    foreach (int side in new[] { -1, 1 })
                    {
                        Pt At(long x, long y) => new Pt(wp.Value.X + (int)((ux * x - uy * side * y) / d), wp.Value.Y + (int)((uy * x + ux * side * y) / d)); // x along, y across to this side
                        long Edge(long x) => occ * (d + x) / c; // across, x cm past them: the wedge's edge
                        long Chord(long x) => Edge(outer) * (inner - x) / (inner - outer); // and the chord's
                        var mid = At(0, occ / 2); long back = G.Isqrt(G.D2(o, mid));
                        foreach (var (from, to) in Hidden(o, z, topw, mid, 2 * reach + d))
                        {
                            long xa = Math.Max(from - back, outer), xb = to - back; if (xb <= xa) continue;
                            var pts = new List<Pt> { At(xa, Edge(xa)), At(xb, Edge(xb)), At(xb, xb >= inner ? 0 : Chord(xb)), At(xa, xa >= inner ? 0 : Chord(xa)) };
                            if (xa < inner && xb > inner) pts.Add(At(inner, 0));
                            var piece = Hull(pts); if (piece.Length >= 3) l.Add(piece);
                        }
                    }
                }
            return l.ToArray();
        }
        /// <summary>The far end of a seen-over shadow along one ray (engine 0.52.0): the end of its first hidden stretch when that starts at the
        /// footprint corner v (Hidden), else v's own distance: no stretch behind it.</summary>
        long HiddenTo(Pt o, int z, int top, Pt v, long cap) { var h = Hidden(o, z, top, v, cap); long len = G.Isqrt(G.D2(o, v)); return h.Count > 0 && h[0].from == len ? h[0].to : len; }
        /// <summary>Engine 0.52.0: past v along the ray from the eye at o (z cm up), the stretches where something whose top is `top` at v hides a target
        /// standing there, as distances from o, in order, to at most `cap`: hidden while r·(z − top) ≤ L·(z − floor − eye), L v's own distance, which
        /// is Blocks3's test of the line to its head (the chest is lower: hidden with it). The floor is one height a walk cell, so the ray is cut where
        /// it crosses the cells' edges and each stretch is solved exactly: a floor that rises behind it (the Office landing) ends a stretch sooner, one
        /// that falls (the carriageway behind the square's police post) later, and a fall further on can hide a target again.</summary>
        List<(long from, long to)> Hidden(Pt o, int z, int top, Pt v, long cap)
        {
            var l = new List<(long, long)>();
            long dx = v.X - o.X, dy = v.Y - o.Y, len = G.Isqrt(dx * dx + dy * dy), kd = z - top; if (len == 0 || kd <= 0 || cap <= len) return l;
            var cuts = new List<long> { len, cap }; var g = S.Grid;
            void Edges(long from, long d, int origin) // the grid lines x (or y) = origin + cell·k the ray crosses between len and cap
            {
                if (d == 0) return;
                long a = from + d, b = from + d * cap / len, lo = Math.Min(a, b), hi = Math.Max(a, b);
                for (long k = (lo - origin) / g.Cell - 1, x = origin + k * g.Cell; x <= hi; k++, x = origin + k * g.Cell)
                    if (x > lo && x <= hi) { long r = (x - from) * len / d; if (r > len && r < cap) cuts.Add(r); }
            }
            Edges(o.X, dx, g.X0); Edges(o.Y, dy, g.Y0);
            cuts.Sort();
            long open = -1; // where the stretch being walked became hidden, or -1
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                long a = cuts[i], b = cuts[i + 1]; if (b <= a) continue;
                long seen = len * (z - FloorAt(AlongRay(o, v, (a + b) / 2)) - eye) / kd; // the farthest r a target on this stretch's floor is hidden at
                if (seen >= b) { if (open < 0) open = a; continue; }
                if (seen >= a) l.Add((open < 0 ? a : open, seen)); else if (open >= 0) l.Add((open, a));
                open = -1;
            }
            if (open >= 0) l.Add((open, cap));
            return l;
        }
        /// <summary>The point r cm from o along the ray through v.</summary>
        static Pt AlongRay(Pt o, Pt v, long r)
        {
            long dx = v.X - o.X, dy = v.Y - o.Y, len = Math.Max(1, G.Isqrt(dx * dx + dy * dy));
            return new Pt(o.X + (int)(dx * r / len), o.Y + (int)(dy * r / len));
        }
        /// <summary>The convex hull, counter-clockwise (Andrew's monotone chain, integers).</summary>
        static Pt[] Hull(List<Pt> pts)
        {
            pts.Sort((a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
            var h = new List<Pt>();
            for (int pass = 0; pass < 2; pass++)
            {
                int start = h.Count;
                for (int i = 0; i < pts.Count; i++)
                {
                    var p = pass == 0 ? pts[i] : pts[pts.Count - 1 - i];
                    while (h.Count >= start + 2 && G.Cross(h[h.Count - 2], h[h.Count - 1], p) <= 0) h.RemoveAt(h.Count - 1);
                    h.Add(p);
                }
                h.RemoveAt(h.Count - 1);
            }
            return h.ToArray();
        }
    }
}
