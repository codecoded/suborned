// Splice's builder change for the road seams (Glyph 0.2.115: "two straight seam lines crossing the Boulevard x Market junction as an X, a soft shaded edge on
// one side only"; prototype out/scratch/splice/roadclip.ts). No export change. Replaces the 3 mm sink (RoadSinks), which left a real step at the lower road's
// edge inside every junction. Each road's carriageway is its ribbon quads minus the quads of every higher-ranked road (width descending, then id) and minus the
// circus disc, the clippers shrunk 5 mm (a coplanar sliver, never a crack): every point of carriageway is drawn by one sheet at the road's own level.
using System.Collections.Generic;
using UnityEngine;

namespace Suborned.World
{
    public static class RoadClip
    {
        const float Eps = 0.005f;      // the clipper is shrunk this much: pieces overlap it by a sliver, never leave a crack
        const int CircleSides = 64;

        /// <summary>Road id -> its carriageway as convex pieces (counter-clockwise or clockwise as made; MeshKit.Flat takes either).</summary>
        public static Dictionary<string, List<Vector2[]>> Sheets(GroundDto g)
        {
            var roads = new List<RoadDto>(g.Roads);
            roads.Sort((a, b) => b.Width != a.Width ? b.Width.CompareTo(a.Width) : string.CompareOrdinal(a.Id, b.Id));
            var quads = new Dictionary<string, List<Vector2[]>>();
            foreach (var r in roads) quads[r.Id] = Quads(r);
            var discs = new List<Vector2[]>();
            foreach (var c in g.Circuses) discs.Add(Circle(c.Centre, c.Radius - 0.02f));
            var res = new Dictionary<string, List<Vector2[]>>();
            for (int i = 0; i < roads.Count; i++)
            {
                var pieces = new List<Vector2[]>(quads[roads[i].Id]);
                foreach (var d in discs) Subtract(pieces, d);
                for (int j = 0; j < i; j++) foreach (var q in quads[roads[j].Id]) Subtract(pieces, Shrink(q, Eps));
                res[roads[i].Id] = pieces;
            }
            return res;
        }

        /// <summary>The ribbon's segment quads as MeshKit.Ribbon lays them (Poly.Ribbon offsets), counter-clockwise.</summary>
        static List<Vector2[]> Quads(RoadDto r)
        {
            var line = Poly.CleanLine(r.Line); var (l, rr) = Poly.Ribbon(line, r.Width, false); var res = new List<Vector2[]>();
            for (int i = 0; i + 1 < line.Length; i++) { var q = new[] { rr[i], rr[i + 1], l[i + 1], l[i] }; if (Area(q) < 0f) System.Array.Reverse(q); res.Add(q); }
            return res;
        }

        static float Area(Vector2[] p) { float s = 0f; for (int i = 0; i < p.Length; i++) { var a = p[i]; var b = p[(i + 1) % p.Length]; s += a.x * b.y - b.x * a.y; } return s * 0.5f; }

        static Vector2[] Circle(Vector2 c, float r) { var p = new Vector2[CircleSides]; for (int i = 0; i < CircleSides; i++) { float a = i * Mathf.PI * 2f / CircleSides; p[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; } return p; }

        static Vector2[] Shrink(Vector2[] p, float d)
        {
            var c = Vector2.zero; foreach (var v in p) c += v; c /= p.Length; var o = new Vector2[p.Length];
            for (int i = 0; i < p.Length; i++) { var dv = p[i] - c; float len = dv.magnitude; o[i] = len < 1e-6f ? p[i] : c + dv * (Mathf.Max(0f, len - d) / len); }
            return o;
        }

        /// <summary>pieces := pieces minus the convex counter-clockwise polygon B. A piece whose box misses B's box is kept whole.</summary>
        static void Subtract(List<Vector2[]> pieces, Vector2[] B)
        {
            Rect bb = Box(B); var next = new List<Vector2[]>(pieces.Count);
            foreach (var P in pieces)
            {
                if (!bb.Overlaps(Box(P))) { next.Add(P); continue; }
                var rest = P;
                for (int k = 0; k < B.Length && rest.Length >= 3; k++)
                {
                    var a = B[k]; var b = B[(k + 1) % B.Length];
                    var outside = Half(rest, a, b, false); if (outside.Length >= 3 && Mathf.Abs(Area(outside)) > 1e-6f) next.Add(outside);
                    rest = Half(rest, a, b, true);
                }
            }
            pieces.Clear(); pieces.AddRange(next);
        }

        static Rect Box(Vector2[] p) { float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue; foreach (var v in p) { x0 = Mathf.Min(x0, v.x); y0 = Mathf.Min(y0, v.y); x1 = Mathf.Max(x1, v.x); y1 = Mathf.Max(y1, v.y); } return Rect.MinMaxRect(x0, y0, x1, y1); }

        /// <summary>The part of convex P on the left of a->b (keepLeft) or on its right (Sutherland-Hodgman, one half-plane).</summary>
        static Vector2[] Half(Vector2[] P, Vector2 a, Vector2 b, bool keepLeft)
        {
            float F(Vector2 p) => ((b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x)) * (keepLeft ? 1f : -1f);
            var o = new List<Vector2>(P.Length + 2);
            for (int i = 0; i < P.Length; i++)
            {
                var p = P[i]; var q = P[(i + 1) % P.Length]; float fp = F(p), fq = F(q);
                if (fp >= 0f) o.Add(p);
                if ((fp >= 0f) != (fq >= 0f)) { float t = fp / (fp - fq); o.Add(p + (q - p) * t); }
            }
            return o.ToArray();
        }
    }
}
