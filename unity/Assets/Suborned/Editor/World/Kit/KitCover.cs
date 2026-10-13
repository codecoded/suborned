using System.Collections.Generic;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Where kit pieces stand over procedural geometry (brief 007 E), so the procedural layer leaves that space to them:
    /// a kit facade piece (bay, end, corner) hides its building's procedural walls inside its footprint up to its top, and a plan
    /// signature's ground (SpaceHQ's drum) cuts the plot slabs under it. Footprints come from the placed prefab's own mesh bounds
    /// (scaled and turned as KitPlacer places it), shrunk 0.1 m at the sides so a party wall on the boundary stays. Static, reset per build.</summary>
    public static class KitCover
    {
        sealed class Cover { public Vector2[] Quad; public float Lo, Hi; }

        static readonly Dictionary<string, List<Cover>> byHost = new(System.StringComparer.Ordinal);
        static readonly List<Rect> grounds = new();
        public static int Hidden { get; private set; } // wall spans hidden this build (evidence)

        public static void Init(WorldDto w, KitPlacer placer)
        {
            byHost.Clear(); grounds.Clear(); Hidden = 0;
            foreach (var f in w.Facades)
            {
                var b = placer.LocalBounds(f.Kit.Id); var s = f.Kit.Scale; var rot = KitPlacer.Rotation(f.Kit.Yaw);
                var pivot = placer.PivotForCentre(f.Kit, f.Kit.At);
                float x0 = b.min.x * s.x + 0.1f, x1 = b.max.x * s.x - 0.1f, z0 = b.min.z * s.z, z1 = b.max.z * s.z;
                var quad = new Vector2[4];
                var loc = new[] { new Vector3(x0, 0, z0), new Vector3(x1, 0, z0), new Vector3(x1, 0, z1), new Vector3(x0, 0, z1) };
                for (int i = 0; i < 4; i++) { var p = rot * loc[i]; quad[i] = pivot + new Vector2(p.x, p.z); }
                if (!byHost.TryGetValue(f.Building, out var list)) byHost[f.Building] = list = new List<Cover>();
                list.Add(new Cover { Quad = quad, Lo = f.Base + b.min.y * s.y, Hi = placer.Top(f.Kit, f.Base) });
            }
            foreach (var sg in w.Signatures)
                if (sg.Ground is { Length: 4 } g) grounds.Add(Rect.MinMaxRect(g[0], g[2], g[1], g[3]));
        }

        public static bool HasCovers(string host) => byHost.ContainsKey(host);

        /// <summary>Spans of wall w (s from 0 to w.Len) inside a kit piece of `host` whose bottom is at or below `lo` + 0.5 m: (s0, s1, top of the piece).</summary>
        public static List<(float s0, float s1, float top)> Spans(string host, in WallFrame w, float lo)
        {
            var r = new List<(float, float, float)>();
            if (!byHost.TryGetValue(host, out var list)) return r;
            foreach (var c in list)
            {
                if (c.Lo > lo + 0.5f || c.Hi <= lo) continue;
                if (Clip(c.Quad, w.A, w.U * w.Len, out float t0, out float t1) && (t1 - t0) * w.Len > 0.05f) r.Add((t0 * w.Len, t1 * w.Len, c.Hi));
            }
            if (r.Count > 0) Hidden++;
            return r;
        }

        /// <summary>Cyrus–Beck: the part of segment p + t·d (t in 0..1) inside convex quad q (either winding).</summary>
        static bool Clip(Vector2[] q, Vector2 p, Vector2 d, out float t0, out float t1)
        {
            t0 = 0f; t1 = 1f;
            float sign = Poly.SignedArea(q) >= 0 ? 1f : -1f;
            for (int i = 0; i < q.Length; i++)
            {
                var a = q[i]; var e = q[(i + 1) % q.Length] - a;
                var n = new Vector2(-e.y, e.x) * sign; // inward normal
                float num = Vector2.Dot(p - a, n), den = Vector2.Dot(d, n);
                if (Mathf.Abs(den) < 1e-9f) { if (num < 0) return false; continue; }
                float t = -num / den;
                if (den > 0) t0 = Mathf.Max(t0, t); else t1 = Mathf.Min(t1, t);
                if (t0 >= t1) return false;
            }
            return true;
        }

        /// <summary>Sub-spans of [0, len] with the highest cover top over each (float.NegativeInfinity where nothing covers).</summary>
        public static List<(float s0, float s1, float top)> Split(List<(float s0, float s1, float top)> spans, float len)
        {
            var cuts = new List<float> { 0f, len };
            foreach (var (a, b, _) in spans) { cuts.Add(Mathf.Clamp(a, 0, len)); cuts.Add(Mathf.Clamp(b, 0, len)); }
            cuts.Sort();
            var r = new List<(float, float, float)>();
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                float a = cuts[i], b = cuts[i + 1]; if (b - a < 0.02f) continue;
                float m = (a + b) * 0.5f, top = float.NegativeInfinity;
                foreach (var (s0, s1, t) in spans) if (m > s0 && m < s1) top = Mathf.Max(top, t);
                if (r.Count > 0 && Mathf.Approximately(r[^1].Item3, top)) r[^1] = (r[^1].Item1, b, top); else r.Add((a, b, top));
            }
            return r;
        }

        /// <summary>A ring with every signature ground cut out of it (convex pieces, each clipped by the half-planes outside one ground);
        /// the ring itself when no ground overlaps it.</summary>
        public static List<Vector2[]> OutsideGrounds(Vector2[] ring)
        {
            var parts = new List<Vector2[]> { ring };
            foreach (var g in grounds)
            {
                var next = new List<Vector2[]>();
                foreach (var p in parts)
                {
                    if (!Overlaps(p, g)) { next.Add(p); continue; }
                    // west, east, then the middle band's south and north
                    Add(next, Half(p, new Vector2(-1, 0), -g.xMin));
                    Add(next, Half(p, new Vector2(1, 0), g.xMax));
                    var mid = Half(Half(p, new Vector2(1, 0), g.xMin), new Vector2(-1, 0), -g.xMax);
                    Add(next, Half(mid, new Vector2(0, -1), -g.yMin));
                    Add(next, Half(mid, new Vector2(0, 1), g.yMax));
                }
                parts = next;
            }
            return parts;
        }

        static void Add(List<Vector2[]> l, Vector2[] p) { if (p.Length >= 3 && Mathf.Abs(Poly.SignedArea(p)) > 0.01f) l.Add(p); }

        static bool Overlaps(Vector2[] p, Rect g)
        {
            Vector2 lo = p[0], hi = p[0];
            foreach (var q in p) { lo = Vector2.Min(lo, q); hi = Vector2.Max(hi, q); }
            return hi.x > g.xMin && lo.x < g.xMax && hi.y > g.yMin && lo.y < g.yMax;
        }

        /// <summary>Sutherland–Hodgman: the part of polygon p where dot(n, x) >= k.</summary>
        static Vector2[] Half(Vector2[] p, Vector2 n, float k)
        {
            var r = new List<Vector2>();
            for (int i = 0; i < p.Length; i++)
            {
                var a = p[i]; var b = p[(i + 1) % p.Length];
                float da = Vector2.Dot(n, a) - k, db = Vector2.Dot(n, b) - k;
                if (da >= 0) r.Add(a);
                if ((da >= 0) != (db >= 0)) r.Add(a + (b - a) * (da / (da - db)));
            }
            return r.ToArray();
        }
    }
}
