// The fan (split from GameSight.cs, brief 101; files stay small, brief 084): a watcher's sight drawn as a flat polygon on the ground, built by the
// engine's own test (rulebook perception: its cone or sweep, the near circle, the solids at eye height, people on the street if asked). Rays go to
// every corner that can end the sight; each ends at the shape's edge, the first solid or the first person across it. Integers only, read-only.
// ConeFan and KnownFans (GameSight.cs) cache and serve it; the shadows of the solids an eye sees over or under are GameFanShadows.cs.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        static readonly int[] Circle64 =
        {
            10000, 0, 9952, 980, 9808, 1951, 9569, 2903, 9239, 3827, 8819, 4714, 8315, 5556, 7730, 6344, 7071, 7071, 6344, 7730, 5556, 8315, 4714, 8819,
            3827, 9239, 2903, 9569, 1951, 9808, 980, 9952, 0, 10000, -980, 9952, -1951, 9808, -2903, 9569, -3827, 9239, -4714, 8819, -5556, 8315,
            -6344, 7730, -7071, 7071, -7730, 6344, -8315, 5556, -8819, 4714, -9239, 3827, -9569, 2903, -9808, 1951, -9952, 980, -10000, 0, -9952,
            -980, -9808, -1951, -9569, -2903, -9239, -3827, -8819, -4714, -8315, -5556, -7730, -6344, -7071, -7071, -6344, -7730, -5556, -8315,
            -4714, -8819, -3827, -9239, -2903, -9569, -1951, -9808, -980, -9952, 0, -10000, 980, -9952, 1951, -9808, 2903, -9569, 3827, -9239,
            4714, -8819, 5556, -8315, 6344, -7730, 7071, -7071, 7730, -6344, 8315, -5556, 8819, -4714, 9239, -3827, 9569, -2903, 9808, -1951, 9952, -980,
        };
        const long RayLen = 65536; // every ray direction is scaled to about this length, so the products below stay far inside a long

        Fan BuildFan(string watcher, Shape s, bool crowd)
        {
            var fan = new Fan { Watcher = watcher, Kind = WatcherKind(watcher), At = s.At, Facing = s.F, Level = 0, Live = s.Live };
            var o = s.At;
            // a watcher standing in a solid sees nothing (Occluder.Blocks: a sightline that starts inside it is blocked); brief 055 §7: its own piece aside
            var weye = WatcherEye(watcher, o);
            foreach (var oc in S.Occluders) if ((weye.HasValue ? !weye.Value.own && FullBlocker(oc, weye.Value.z, o) : eye >= oc.Z && eye <= oc.Z + oc.H) && G.InPoly(o, oc.Poly)) return fan;

            // the shape as a convex polygon about o (the cone's triangle, the sweep's trapezoid), and its near circle
            var poly = new List<Pt>(); bool apex;
            long fl0 = G.Isqrt((long)s.F.X * s.F.X + (long)s.F.Y * s.F.Y);
            if (s.Sweep)
            {
                apex = true; // o is on the base: a corner of the fan
                if (fl0 > 0)
                {
                    long fx = s.F.X, fy = s.F.Y, hb = s.HalfBase, hf = s.HalfBase + (long)s.Depth * s.PerMille / 1000, d = s.Depth;
                    Pt P(long a, long side) => new Pt(o.X + (int)((fx * a - fy * side) / fl0), o.Y + (int)((fy * a + fx * side) / fl0));
                    poly.Add(P(0, -hb)); poly.Add(P(d, -hf)); poly.Add(P(d, hf)); poly.Add(P(0, hb));
                }
            }
            else
            {
                apex = s.Near == 0;
                if (fl0 > 0)
                {
                    long fx = (long)s.F.X * 1024 / fl0, fy = (long)s.F.Y * 1024 / fl0; long fl = G.Isqrt(fx * fx + fy * fy); // as G.InCone scales it
                    if (fl > 0)
                    {
                        Pt P(long a, long side) => new Pt(o.X + (int)((fx * a - fy * side) / fl), o.Y + (int)((fy * a + fx * side) / fl));
                        poly.Add(o); poly.Add(P(s.Reach, -s.Half)); poly.Add(P(s.Reach, s.Half));
                    }
                }
            }
            long maxR = s.Near; foreach (var p in poly) maxR = Math.Max(maxR, G.Isqrt(G.D2(o, p)));
            if (maxR == 0) return fan;

            // the occluders that matter: at eye height, within maxR of o
            int bx0 = (int)(o.X - maxR), bx1 = (int)(o.X + maxR), by0 = (int)(o.Y - maxR), by1 = (int)(o.Y + maxR);
            var occ = new List<Occluder>();
            foreach (var oc in S.Occluders) if ((weye.HasValue ? !(weye.Value.own && G.InPoly(o, oc.Poly)) && FullBlocker(oc, weye.Value.z, o) : eye >= oc.Z && eye <= oc.Z + oc.H) && oc.BoxMeets(bx0, by0, bx1, by1)) occ.Add(oc); // brief 055 §7: the solids that hide everything behind them from this eye
            var walkers = new List<Pt>();
            long occR = R.CrowdOccludeCm;
            if (crowd) foreach (var w in Walkers) { var wp = CrowdPos(w); if (wp.HasValue && G.Within(o, wp.Value, (int)(maxR + 2 * occR)) && !(weye.HasValue && weye.Value.z > FloorAt(wp.Value) + R.MountHeadTopCm)) walkers.Add(wp.Value); } // brief 055 §7: an eye over a person's head sees past them but for a short stretch (FanShadows)

            // the rays: toward the shape's corners, its near circle, every occluder corner in reach and where an occluder edge crosses the
            // shape's edge (each also a hair either side, so a shadow's edge is caught on both sides), and every person's edges
            var dirs = new List<Pt>(); var seen = new HashSet<long>();
            void Ray(long vx, long vy)
            {
                if (vx == 0 && vy == 0) return;
                long len = G.Isqrt(vx * vx + vy * vy); if (len == 0) return;
                int dx = (int)(vx * RayLen / len), dy = (int)(vy * RayLen / len);
                if (seen.Add(unchecked(((long)dx << 32) ^ (uint)dy))) dirs.Add(new Pt(dx, dy));
            }
            void Ray3(Pt to)
            {
                long vx = to.X - o.X, vy = to.Y - o.Y; if (vx == 0 && vy == 0) return;
                long len = G.Isqrt(vx * vx + vy * vy); long dx = vx * RayLen / len, dy = vy * RayLen / len;
                Ray(dx, dy); Ray(dx - dy / 512, dy + dx / 512); Ray(dx + dy / 512, dy - dx / 512);
            }
            foreach (var p in poly) Ray3(p);
            if (s.Near > 0) for (int k = 0; k < 64; k++) Ray(Circle64[2 * k], Circle64[2 * k + 1]);
            long maxR2 = maxR * maxR;
            foreach (var oc in occ)
            {
                var pg = oc.Poly;
                for (int i = 0, j = pg.Length - 1; i < pg.Length; j = i++)
                {
                    if (G.D2(o, pg[i]) <= maxR2) Ray3(pg[i]);
                    for (int a = 0, b = poly.Count - 1; a < poly.Count; b = a++)
                        if (SegPoint(poly[b], poly[a], pg[j], pg[i]) is Pt x) Ray3(x);
                }
            }
            // where two solids' edges cross (a low wall into a building's face): a corner of the shadow that is no corner of either
            for (int a = 0; a < occ.Count; a++)
                for (int b = a + 1; b < occ.Count; b++)
                {
                    var pa = occ[a].Poly; var pb = occ[b].Poly;
                    for (int i = 0, j = pa.Length - 1; i < pa.Length; j = i++)
                        for (int m = 0, n = pb.Length - 1; m < pb.Length; n = m++)
                            if (SegPoint(pa[j], pa[i], pb[n], pb[m]) is Pt x && G.D2(o, x) <= maxR2) Ray3(x);
                }
            // a person's shadow edges are the two tangents from o to the circle of crowd.occlude round them, at asin(occ / d) either side of the
            // line to them, built as directions (never as rounded points beside them): a point a centimetre off at a person 3 m away swings the
            // ray by more than its hair, and both hairs then fell in the shadow, dropping the clear wedge beside it (Keel 2026-10-19: the
            // Records Square mast's crowd fan at 0:30, a person 3 m off its cone's edge)
            void RayDir3(long dx, long dy) { while (Math.Abs(dx) > 1L << 30 || Math.Abs(dy) > 1L << 30) { dx /= 2; dy /= 2; } long len = G.Isqrt(dx * dx + dy * dy); if (len == 0) return; dx = dx * RayLen / len; dy = dy * RayLen / len; Ray(dx, dy); Ray(dx - dy / 512, dy + dx / 512); Ray(dx + dy / 512, dy - dx / 512); }
            foreach (var w in walkers)
            {
                long vx = w.X - o.X, vy = w.Y - o.Y, d2 = vx * vx + vy * vy, len = G.Isqrt(d2); if (len == 0) continue;
                Ray3(w);
                if (len <= occR) continue; // o inside the body: no tangent
                long c = G.Isqrt(d2 - occR * occR); // d·cos θ, with sin θ = occ / d
                RayDir3(vx * c - vy * occR, vy * c + vx * occR); RayDir3(vx * c + vy * occR, vy * c - vx * occR);
            }

            // each ray's end: the farther of the shape's exit and the near circle, cut at the first occluder (or person) across it
            var ends = new List<(Pt dir, Pt end, string stop, int idx)>();
            for (int n = 0; n < dirs.Count; n++)
            {
                var d = dirs[n];
                Pt end = o; string stop = "apex";
                if (poly.Count >= 3) { var x = PolyExit(o, d, poly); if (x.HasValue) { end = x.Value; stop = "reach"; } }
                if (s.Near > 0)
                {
                    long len = G.Isqrt((long)d.X * d.X + (long)d.Y * d.Y);
                    var c = new Pt(o.X + (int)((long)d.X * s.Near / len), o.Y + (int)((long)d.Y * s.Near / len));
                    if (G.D2(o, c) > G.D2(o, end)) { end = c; stop = "near"; }
                }
                if (end == o) continue;
                long best = G.D2(o, end);
                foreach (var oc in occ)
                {
                    if (!oc.BoxMeets(Math.Min(o.X, end.X), Math.Min(o.Y, end.Y), Math.Max(o.X, end.X), Math.Max(o.Y, end.Y))) continue;
                    var pg = oc.Poly;
                    for (int i = 0, j = pg.Length - 1; i < pg.Length; j = i++)
                        if (SegPoint(o, end, pg[j], pg[i]) is Pt x) { long dd = G.D2(o, x); if (dd < best) { best = dd; end = x; stop = "blocked"; } }
                }
                if (walkers.Count > 0) { var cut = CrowdCut(o, end, walkers, occR); if (cut.HasValue) { end = cut.Value; stop = "crowd"; } }
                if (end == o) continue;
                ends.Add((d, end, stop, n));
            }

            // in angle order, counter-clockwise from straight behind the watcher (ties: the ray made first)
            var back = fl0 > 0 ? new Pt(-s.F.X, -s.F.Y) : new Pt(-1, 0);
            ends.Sort((a, b) =>
            {
                int ha = Half(back, a.dir), hb2 = Half(back, b.dir);
                if (ha != hb2) return ha.CompareTo(hb2);
                long c = G.Cross(new Pt(0, 0), a.dir, b.dir);
                if (c != 0) return c > 0 ? -1 : 1;
                return a.idx.CompareTo(b.idx);
            });
            var pts = new List<Pt>(); var stops = new List<string>();
            if (apex) { pts.Add(o); stops.Add("apex"); }
            foreach (var e in ends)
            {
                if (pts.Count > 0 && pts[pts.Count - 1] == e.end) continue;
                pts.Add(e.end); stops.Add(e.stop);
            }
            if (pts.Count > 1 && pts[0] == pts[pts.Count - 1]) { pts.RemoveAt(pts.Count - 1); stops.RemoveAt(stops.Count - 1); }
            // drop a point within a centimetre of the straight line between its neighbours, all three ended alike by a straight edge (the
            // shape's edge or a solid's), so a straight edge is two points; the near circle keeps its 64
            for (int i = pts.Count - 2; i >= 1; i--)
            {
                if (stops[i] != stops[i - 1] || stops[i] != stops[i + 1] || (stops[i] != "reach" && stops[i] != "blocked")) continue;
                long c = Math.Abs(G.Cross(pts[i - 1], pts[i], pts[i + 1])), l = G.Isqrt(G.D2(pts[i - 1], pts[i + 1]));
                if (l > 0 && c <= l) { pts.RemoveAt(i); stops.RemoveAt(i); }
            }
            if (pts.Count < 3) return fan;
            fan.Points = pts.ToArray(); fan.Stop = stops.ToArray();
            long far = 0; foreach (var p in pts) far = Math.Max(far, G.D2(o, p)); fan.Reach = (int)G.Isqrt(far);
            return fan;
        }

        /// <summary>0 for directions in [0, π) counter-clockwise from r, 1 for [π, 2π).</summary>
        static int Half(Pt r, Pt u)
        {
            long c = (long)r.X * u.Y - (long)r.Y * u.X, d = (long)r.X * u.X + (long)r.Y * u.Y;
            return c > 0 || (c == 0 && d > 0) ? 0 : 1;
        }

        /// <summary>Where segments ab and cd meet (one point; for overlapping collinear segments, the end of cd nearest a), else null.</summary>
        static Pt? SegPoint(Pt a, Pt b, Pt c, Pt d)
        {
            long sx = b.X - a.X, sy = b.Y - a.Y, ex = d.X - c.X, ey = d.Y - c.Y;
            long den = sx * ey - sy * ex, cx = c.X - a.X, cy = c.Y - a.Y;
            if (den == 0)
            {
                if (cx * sy - cy * sx != 0) return null; // parallel, apart
                long ss = sx * sx + sy * sy; if (ss == 0) return null;
                Pt? best = null; long bd = long.MaxValue;
                foreach (var q in new[] { c, d }) { long t = (long)(q.X - a.X) * sx + (long)(q.Y - a.Y) * sy; if (t < 0 || t > ss) continue; long dd = G.D2(a, q); if (dd < bd) { bd = dd; best = q; } }
                if (best == null && ((long)(c.X - a.X) * sx + (long)(c.Y - a.Y) * sy < 0) != ((long)(d.X - a.X) * sx + (long)(d.Y - a.Y) * sy < 0)) best = a; // a lies inside cd
                return best;
            }
            long tn = cx * ey - cy * ex, un = cx * sy - cy * sx;
            if (den < 0) { den = -den; tn = -tn; un = -un; }
            if (tn < 0 || tn > den || un < 0 || un > den) return null;
            return new Pt(a.X + (int)(sx * tn / den), a.Y + (int)(sy * tn / den));
        }

        /// <summary>Where a ray from o along d leaves the convex polygon (o inside it or on its edge), or null if it never enters.</summary>
        static Pt? PolyExit(Pt o, Pt d, List<Pt> poly)
        {
            Pt? best = null; long bd = 0;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                Pt c = poly[j], e = poly[i];
                long ex = e.X - c.X, ey = e.Y - c.Y, den = (long)d.X * ey - (long)d.Y * ex, cx = c.X - o.X, cy = c.Y - o.Y;
                if (den == 0)
                {
                    if (cx * d.Y - cy * d.X != 0) continue; // parallel to the edge, off it
                    foreach (var q in new[] { c, e }) { long t = (long)(q.X - o.X) * d.X + (long)(q.Y - o.Y) * d.Y; if (t > 0) { long dd = G.D2(o, q); if (dd > bd) { bd = dd; best = q; } } }
                    continue;
                }
                long tn = cx * ey - cy * ex, un = cx * d.Y - cy * d.X;
                if (den < 0) { den = -den; tn = -tn; un = -un; }
                if (tn <= 0 || un < 0 || un > den) continue;
                var x = new Pt(o.X + (int)((long)d.X * tn / den), o.Y + (int)((long)d.Y * tn / den));
                long dx = G.D2(o, x); if (dx > bd) { bd = dx; best = x; }
            }
            return best;
        }

        /// <summary>Where people on the street cut the sightline o→end (CrowdBlocks: within occlude of the line, two bodies' width from
        /// either end), or null.</summary>
        static Pt? CrowdCut(Pt o, Pt end, List<Pt> walkers, long occ)
        {
            long sx = end.X - o.X, sy = end.Y - o.Y, l2 = sx * sx + sy * sy; if (l2 == 0) return null;
            long len = G.Isqrt(l2), limit = len, end2 = 4 * occ * occ;
            foreach (var p in walkers)
            {
                long px = p.X - o.X, py = p.Y - o.Y, dot = px * sx + py * sy;
                if (dot <= 0 || G.D2(p, o) <= end2) continue;
                long cross = px * sy - py * sx;
                if (cross * cross > occ * occ * l2) continue;
                long proj = dot / len, perp2 = cross * cross / l2;
                long clear = proj + G.Isqrt(Math.Max(0, end2 - perp2)) + 1;
                if (clear < limit) limit = clear;
            }
            if (limit >= len) return null;
            return new Pt(o.X + (int)(sx * limit / len), o.Y + (int)(sy * limit / len));
        }
    }
}
