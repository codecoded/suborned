using System.Collections.Generic;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>2D polygon work in the plan frame (x east, y north): cleaning, winding, ear clipping, inset, strips.
    /// Pure geometry, no Unity objects.</summary>
    public static class Poly
    {
        const float Eps = 1e-5f;

        /// <summary>Ear-clipping fallbacks taken (a pass found no clean ear). Reset per build, reported in the log.</summary>
        public static int Fallbacks;

        public static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        public static float SignedArea(IReadOnlyList<Vector2> p)
        {
            float s = 0;
            for (int i = 0; i < p.Count; i++) s += Cross(p[i], p[(i + 1) % p.Count]);
            return s * 0.5f;
        }

        /// <summary>Repairs made by Clean (repeated points, zero-width spurs). The export guarantees simple polygons, so
        /// any repair means bad input: reset per build and reported as a warning.</summary>
        public static int Repairs;

        /// <summary>Generic cleaning: drops points that lie straight on between their neighbours (harmless, silent) and
        /// returns the ring counter-clockwise. Repeated points (closing point included) and zero-width spurs are also
        /// dropped, but counted in Repairs: the export promises none.</summary>
        public static Vector2[] Clean(Vector2[] src)
        {
            var p = Tidy(src, out int repairs);
            Repairs += repairs;
            return p;
        }

        /// <summary>Clean without the count, for rings the builder derives itself (an inset may collapse a narrow tab to
        /// a spur, which is expected there).</summary>
        static Vector2[] Tidy(Vector2[] src, out int repairs)
        {
            repairs = 0;
            var p = new List<Vector2>(src);
            bool changed = true;
            while (changed && p.Count > 3)
            {
                changed = false;
                for (int i = 0; i < p.Count && p.Count > 3; i++)
                {
                    var a = p[(i + p.Count - 1) % p.Count]; var b = p[i]; var c = p[(i + 1) % p.Count];
                    var ab = b - a; var bc = c - b;
                    bool repeated = ab.sqrMagnitude <= Eps || bc.sqrMagnitude <= Eps;
                    if (!repeated && Mathf.Abs(Cross(ab, bc)) > 1e-4f * ab.magnitude * bc.magnitude) continue;
                    if (repeated || Vector2.Dot(ab, bc) < 0) repairs++;
                    p.RemoveAt(i); i--; changed = true;
                }
            }
            if (SignedArea(p) < 0) p.Reverse();
            return p.ToArray();
        }

        /// <summary>One ring for ear clipping from a counter-clockwise outer ring and a counter-clockwise hole inside it:
        /// the hole is walked clockwise and joined to the outer ring by a two-way bridge from its rightmost vertex to a
        /// visible outer vertex (Eberly, "Triangulation by Ear Clipping", 2002).</summary>
        public static Vector2[] Bridge(Vector2[] outer, Vector2[] hole)
        {
            int n = outer.Length, h = hole.Length, m = 0;
            for (int i = 1; i < h; i++) if (hole[i].x > hole[m].x) m = i;
            var M = hole[m];
            // Nearest outer edge crossed by the ray from M towards +x.
            int edge = -1; float bestX = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = outer[i], b = outer[(i + 1) % n];
                if ((a.y <= M.y) == (b.y <= M.y)) continue;
                float x = a.x + (M.y - a.y) * (b.x - a.x) / (b.y - a.y);
                if (x >= M.x && x < bestX) { bestX = x; edge = i; }
            }
            if (edge < 0) throw new System.ArgumentException("World: hole is not inside its outer ring");
            var I = new Vector2(bestX, M.y);
            int p = outer[edge].x >= outer[(edge + 1) % n].x ? edge : (edge + 1) % n;
            // A reflex outer vertex inside triangle M, I, P could block the bridge: take the one closest in angle to the ray.
            var P = outer[p]; float bestAngle = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var q = outer[i];
                if (i == p || Cross(q - outer[(i + n - 1) % n], outer[(i + 1) % n] - q) > 0) continue;
                if (!InTriangle(q, M, I, P)) continue;
                float angle = Mathf.Atan2(Mathf.Abs(q.y - M.y), q.x - M.x);
                if (angle < bestAngle) { bestAngle = angle; p = i; }
            }
            var r = new List<Vector2>(n + h + 2);
            for (int i = 0; i <= p; i++) r.Add(outer[i]);
            for (int k = 0; k <= h; k++) r.Add(hole[((m - k) % h + h) % h]);
            r.Add(outer[p]);
            for (int i = p + 1; i < n; i++) r.Add(outer[i]);
            return r.ToArray();
        }

        /// <summary>Even-odd point-in-polygon (any winding).</summary>
        public static bool Contains(Vector2[] poly, Vector2 q)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                Vector2 a = poly[i], b = poly[j];
                if ((a.y > q.y) != (b.y > q.y) && q.x < a.x + (q.y - a.y) * (b.x - a.x) / (b.y - a.y)) inside = !inside;
            }
            return inside;
        }

        static bool InTriangle(Vector2 q, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(b - a, q - a), d2 = Cross(c - b, q - b), d3 = Cross(a - c, q - c);
            bool neg = d1 < -Eps || d2 < -Eps || d3 < -Eps, pos = d1 > Eps || d2 > Eps || d3 > Eps;
            return !(neg && pos);
        }

        /// <summary>Ear clipping of a simple counter-clockwise ring. Returns index triples, counter-clockwise in plan.</summary>
        public static int[] Triangulate(Vector2[] p)
        {
            int n = p.Length;
            var tris = new List<int>((n - 2) * 3);
            if (n < 3) return tris.ToArray();
            var idx = new List<int>(n);
            for (int i = 0; i < n; i++) idx.Add(i);
            int guard = 0;
            while (idx.Count > 3 && guard++ < n * n)
            {
                int m = idx.Count; bool clipped = false;
                for (int k = 0; k < m; k++)
                {
                    int ia = idx[(k + m - 1) % m], ib = idx[k], ic = idx[(k + 1) % m];
                    if (!IsEar(p, idx, ia, ib, ic)) continue;
                    tris.Add(ia); tris.Add(ib); tris.Add(ic);
                    idx.RemoveAt(k); clipped = true; break;
                }
                if (clipped) continue;
                // No clean ear (degenerate input): clip the most convex corner so the build always finishes.
                Fallbacks++;
                int best = 0; float bestCross = float.MinValue;
                for (int k = 0; k < m; k++)
                {
                    var a = p[idx[(k + m - 1) % m]]; var b = p[idx[k]]; var c = p[idx[(k + 1) % m]];
                    float cr = Cross(b - a, c - b);
                    if (cr > bestCross) { bestCross = cr; best = k; }
                }
                if (bestCross > 0) { tris.Add(idx[(best + m - 1) % m]); tris.Add(idx[best]); tris.Add(idx[(best + 1) % m]); }
                idx.RemoveAt(best);
            }
            if (idx.Count == 3 && Cross(p[idx[1]] - p[idx[0]], p[idx[2]] - p[idx[1]]) > 0)
            { tris.Add(idx[0]); tris.Add(idx[1]); tris.Add(idx[2]); }
            return tris.ToArray();
        }

        static bool IsEar(Vector2[] p, List<int> idx, int ia, int ib, int ic)
        {
            Vector2 a = p[ia], b = p[ib], c = p[ic];
            if (Cross(b - a, c - b) <= Eps) return false;
            foreach (int j in idx)
            {
                if (j == ia || j == ib || j == ic) continue;
                var q = p[j];
                if ((q - a).sqrMagnitude < Eps || (q - b).sqrMagnitude < Eps || (q - c).sqrMagnitude < Eps) continue;
                if (Cross(b - a, q - a) >= -Eps && Cross(c - b, q - b) >= -Eps && Cross(a - c, q - c) >= -Eps) return false;
            }
            return true;
        }

        /// <summary>Moves every edge of a counter-clockwise ring inward by d (mitred, clamped), with collapsed tabs tidied
        /// away. Null if it collapses.</summary>
        public static Vector2[] Inset(Vector2[] p, float d)
        {
            int n = p.Length; var r = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                var prev = p[(i + n - 1) % n]; var cur = p[i]; var next = p[(i + 1) % n];
                var n1 = Left((cur - prev).normalized); var n2 = Left((next - cur).normalized);
                r[i] = cur + Miter(n1, n2, d);
            }
            float a0 = SignedArea(p), a1 = SignedArea(r);
            return a1 > 0.05f * a0 ? Tidy(r, out _) : null;
        }

        /// <summary>Left and right edges of a ribbon of the given width along a polyline (closed rings wrap).</summary>
        public static (Vector2[] left, Vector2[] right) Ribbon(Vector2[] line, float width, bool closed)
        {
            int n = line.Length; var l = new Vector2[n]; var r = new Vector2[n]; float h = width * 0.5f;
            for (int i = 0; i < n; i++)
            {
                Vector2 dIn, dOut;
                bool hasPrev = closed || i > 0, hasNext = closed || i < n - 1;
                dOut = hasNext ? (line[(i + 1) % n] - line[i]).normalized : (line[i] - line[i - 1]).normalized;
                dIn = hasPrev ? (line[i] - line[(i + n - 1) % n]).normalized : dOut;
                var m = Miter(Left(dIn), Left(dOut), h);
                l[i] = line[i] + m; r[i] = line[i] - m;
            }
            return (l, r);
        }

        static Vector2 Left(Vector2 d) => new Vector2(-d.y, d.x);

        static Vector2 Miter(Vector2 n1, Vector2 n2, float d)
        {
            var m = n1 + n2;
            if (m.sqrMagnitude < 1e-6f) return n1 * d;
            m.Normalize();
            float cos = Mathf.Max(Vector2.Dot(m, n1), 0.34f); // clamp spikes at sharp corners (~3x)
            return m * (d / cos);
        }

        /// <summary>Removes consecutive duplicate points from an open polyline.</summary>
        public static Vector2[] CleanLine(Vector2[] src)
        {
            var p = new List<Vector2>(src.Length);
            foreach (var v in src) if (p.Count == 0 || (v - p[p.Count - 1]).sqrMagnitude > Eps) p.Add(v);
            return p.ToArray();
        }

        public static Vector2[] Circle(Vector2 c, float r, int segments)
        {
            var p = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float t = 2 * Mathf.PI * i / segments;
                p[i] = c + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * r;
            }
            return p;
        }

        /// <summary>Rectangle centred at c, long side `length` along dir, `width` across. Counter-clockwise.</summary>
        public static Vector2[] Rect(Vector2 c, Vector2 dir, float length, float width)
        {
            var u = dir.sqrMagnitude > 0 ? dir.normalized : Vector2.right; var v = Left(u);
            u *= length * 0.5f; v *= width * 0.5f;
            return new[] { c - u - v, c + u - v, c + u + v, c - u + v };
        }

        /// <summary>Unit tangent of the polyline segment nearest to q.</summary>
        public static Vector2 NearestTangent(Vector2[] line, Vector2 q, out float distance)
        {
            distance = float.MaxValue; var t = Vector2.right;
            for (int i = 0; i + 1 < line.Length; i++)
            {
                var a = line[i]; var ab = line[i + 1] - a; float len2 = ab.sqrMagnitude;
                if (len2 < Eps) continue;
                float s = Mathf.Clamp01(Vector2.Dot(q - a, ab) / len2);
                float d = (a + ab * s - q).magnitude;
                if (d < distance) { distance = d; t = ab.normalized; }
            }
            return t;
        }
    }
}
