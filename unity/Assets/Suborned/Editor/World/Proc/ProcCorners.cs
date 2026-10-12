using System.Collections.Generic;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Corner treatments on a plan ring: a radiused corner (the cafe's 5.3 m arc family) or a chamfer replaces each
    /// convex corner with arc / cut vertices; the short edges between them are flagged facets.</summary>
    public static class Corners
    {
        public sealed class Result { public Vector2[] Pts; public bool[] Facet; public float[] Turn; }

        public static Result Plain(Vector2[] ring)
        {
            var r = new Result { Pts = ring, Facet = new bool[ring.Length], Turn = new float[ring.Length] };
            for (int i = 0; i < ring.Length; i++) r.Turn[i] = TurnAt(ring, i);
            return r;
        }

        static float TurnAt(Vector2[] p, int i)
        {
            var a = p[(i + p.Length - 1) % p.Length]; var b = p[i]; var c = p[(i + 1) % p.Length];
            var u1 = (b - a).normalized; var u2 = (c - b).normalized;
            return Mathf.Atan2(u1.x * u2.y - u1.y * u2.x, Vector2.Dot(u1, u2));
        }

        public static Result Round(Vector2[] ring, Grammar g)
        {
            var pts = new List<Vector2>(); var facet = new List<bool>(); var turn = new List<float>();
            int n = ring.Length;
            for (int i = 0; i < n; i++)
            {
                var a = ring[(i + n - 1) % n]; var b = ring[i]; var c = ring[(i + 1) % n];
                var u1 = (b - a).normalized; var u2 = (c - b).normalized;
                float th = Mathf.Atan2(u1.x * u2.y - u1.y * u2.x, Vector2.Dot(u1, u2));
                float la = (b - a).magnitude, lc = (c - b).magnitude;
                float r = Mathf.Min(g.Radius, la / 2.2f, lc / 2.2f);
                if (th < 0.3f || r < 1.2f) { pts.Add(b); facet.Add(false); turn.Add(th); continue; }
                float t = r * Mathf.Tan(th * 0.5f);
                var p0 = b - u1 * t; var p1 = b + u2 * t;
                if (g.Corner == "chamfer") { pts.Add(p0); facet.Add(true); turn.Add(th * 0.5f); pts.Add(p1); facet.Add(false); turn.Add(th * 0.5f); continue; }
                int seg = Mathf.Max(3, Mathf.CeilToInt(th / (Mathf.PI / 9f)));
                var inward = new Vector2(-u1.y, u1.x); var cen = p0 + inward * r;
                float a0 = Mathf.Atan2(p0.y - cen.y, p0.x - cen.x);
                for (int k = 0; k <= seg; k++)
                {
                    float an = a0 + th * k / seg;
                    pts.Add(cen + new Vector2(Mathf.Cos(an), Mathf.Sin(an)) * r);
                    facet.Add(k < seg); turn.Add(k == 0 || k == seg ? th / seg * 0.5f : th / seg);
                }
            }
            return new Result { Pts = pts.ToArray(), Facet = facet.ToArray(), Turn = turn.ToArray() };
        }
    }
}
