#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 075 swap: convex XZ shapes with a height band, in a 40 m grid, for the two questions the swap asks: does a point
    /// fall in a shape (what we keep of ours: the zone, the network), and does a box overlap a shape (where the old gives way).</summary>
    internal sealed class SwapShapes
    {
        internal sealed class Shape
        {
            public string Kind, Id; public Vector2[] Poly; public Rect Box; public float Y0 = float.NegativeInfinity, Y1 = float.PositiveInfinity;
            public bool Contains(Vector2 p)
            {
                if (!SwapMesh.Inside(Box, p)) return false;
                bool sign = false, set = false;
                for (int i = 0; i < Poly.Length; i++)
                {
                    Vector2 a = Poly[i], b = Poly[(i + 1) % Poly.Length];
                    float c = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
                    if (Mathf.Abs(c) < 1e-6f) continue;
                    if (!set) { sign = c > 0; set = true; } else if ((c > 0) != sign) return false;
                }
                return true;
            }
            /// <summary>SAT: this convex polygon against an axis-aligned rect.</summary>
            public bool Overlaps(Rect r)
            {
                if (!SwapMesh.Overlaps(Box, r, 0f)) return false;
                var rc = new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) };
                for (int i = 0; i < Poly.Length; i++)
                {
                    Vector2 a = Poly[i], b = Poly[(i + 1) % Poly.Length], ax = new(-(b.y - a.y), b.x - a.x);
                    float p0 = float.MaxValue, p1 = float.MinValue, q0 = float.MaxValue, q1 = float.MinValue;
                    foreach (var v in Poly) { float d = Vector2.Dot(v - a, ax); p0 = Mathf.Min(p0, d); p1 = Mathf.Max(p1, d); }
                    foreach (var v in rc) { float d = Vector2.Dot(v - a, ax); q0 = Mathf.Min(q0, d); q1 = Mathf.Max(q1, d); }
                    if (p1 <= q0 || q1 <= p0) return false;
                }
                return true;
            }
        }

        const float Cell = 40f;
        readonly Dictionary<Vector2Int, List<Shape>> grid = new();
        public readonly List<Shape> All = new();

        public Shape Add(string kind, string id, Vector2[] poly, float y0 = float.NegativeInfinity, float y1 = float.PositiveInfinity)
        {
            if (poly == null || poly.Length < 3) return null;
            float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
            foreach (var p in poly) { x0 = Mathf.Min(x0, p.x); z0 = Mathf.Min(z0, p.y); x1 = Mathf.Max(x1, p.x); z1 = Mathf.Max(z1, p.y); }
            var s = new Shape { Kind = kind, Id = id, Poly = Convex(poly), Box = UnityEngine.Rect.MinMaxRect(x0, z0, x1, z1), Y0 = y0, Y1 = y1 };
            All.Add(s);
            for (int i = Mathf.FloorToInt(x0 / Cell); i <= Mathf.FloorToInt(x1 / Cell); i++)
                for (int j = Mathf.FloorToInt(z0 / Cell); j <= Mathf.FloorToInt(z1 / Cell); j++)
                {
                    var k = new Vector2Int(i, j);
                    if (!grid.TryGetValue(k, out var l)) grid[k] = l = new List<Shape>();
                    l.Add(s);
                }
            return s;
        }

        public Shape Rect(string kind, string id, Rect r, float grow = 0f, float y0 = float.NegativeInfinity, float y1 = float.PositiveInfinity) =>
            Add(kind, id, new[] { new Vector2(r.xMin - grow, r.yMin - grow), new Vector2(r.xMax + grow, r.yMin - grow), new Vector2(r.xMax + grow, r.yMax + grow), new Vector2(r.xMin - grow, r.yMax + grow) }, y0, y1);

        public void Circle(string kind, string id, Vector2 c, float r, float y0 = float.NegativeInfinity, float y1 = float.PositiveInfinity)
        {
            var p = new Vector2[12];
            for (int i = 0; i < 12; i++) p[i] = c + new Vector2(Mathf.Cos(i * Mathf.PI / 6f), Mathf.Sin(i * Mathf.PI / 6f)) * r;
            Add(kind, id, p, y0, y1);
        }

        /// <summary>A polyline as a chain of quads (half width hw), each its own convex shape.</summary>
        public void Ribbon(string kind, string id, IList<Vector2> line, float hw, float y0 = float.NegativeInfinity, float y1 = float.PositiveInfinity, float step = 6f)
        {
            if (line == null || line.Count < 2) return;
            var pts = new List<Vector2> { line[0] };
            float acc = 0f;
            for (int i = 1; i < line.Count; i++) { acc += Vector2.Distance(line[i - 1], line[i]); if (acc >= step || i == line.Count - 1) { pts.Add(line[i]); acc = 0f; } }
            for (int i = 1; i < pts.Count; i++)
            {
                Vector2 a = pts[i - 1], b = pts[i], d = (b - a).normalized, n = new(-d.y, d.x);
                if ((b - a).sqrMagnitude < 1e-6f) continue;
                a -= d * hw * .5f; b += d * hw * .5f;  // overlap the joints
                Add(kind, $"{id}#{i}", new[] { a + n * hw, b + n * hw, b - n * hw, a - n * hw }, y0, y1);
            }
        }

        public IEnumerable<Shape> Near(Rect r)
        {
            var seen = new HashSet<Shape>();
            for (int i = Mathf.FloorToInt(r.xMin / Cell); i <= Mathf.FloorToInt(r.xMax / Cell); i++)
                for (int j = Mathf.FloorToInt(r.yMin / Cell); j <= Mathf.FloorToInt(r.yMax / Cell); j++)
                    if (grid.TryGetValue(new Vector2Int(i, j), out var l))
                        foreach (var s in l) if (seen.Add(s)) yield return s;
        }

        public Shape Hit(Vector2 p) { foreach (var s in Near(new Rect(p, Vector2.zero))) if (s.Contains(p)) return s; return null; }

        /// <summary>The first shape overlapping the box whose band meets the box's height range.</summary>
        public Shape Hit(Rect r, float y0, float y1)
        {
            foreach (var s in Near(r)) if (y1 > s.Y0 && y0 < s.Y1 && s.Overlaps(r)) return s;
            return null;
        }

        /// <summary>Gift-wrap hull (rings from the export may be concave: a bounding hull is the conservative shape for both questions).</summary>
        static Vector2[] Convex(Vector2[] p)
        {
            if (p.Length <= 3) return p;
            var pts = new List<Vector2>(p); pts.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            var h = new List<Vector2>();
            float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
            foreach (var q in pts) { while (h.Count >= 2 && Cross(h[^2], h[^1], q) <= 0) h.RemoveAt(h.Count - 1); h.Add(q); }
            int lo = h.Count + 1;
            for (int i = pts.Count - 2; i >= 0; i--) { var q = pts[i]; while (h.Count >= lo && Cross(h[^2], h[^1], q) <= 0) h.RemoveAt(h.Count - 1); h.Add(q); }
            h.RemoveAt(h.Count - 1);
            return h.Count >= 3 ? h.ToArray() : p;
        }
    }
}
#endif
