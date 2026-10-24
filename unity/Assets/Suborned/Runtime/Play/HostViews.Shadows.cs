using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Engine 0.28 (brief 055 §7, perception.mount; hud29 securityFan.shadows): a fan's Fan.Shadows, the stretches inside its polygon the
    /// watcher does not see into (behind a solid it sees over or under), are cut out of the fill and the rim; each cut gets a rim of its own,
    /// the fan's colour 0.045 m over ink-deep 0.08 m (alpha 0.45), so a cut reads as a cut. No hatch: the ground shows through. The polygons are
    /// the engine's (convex, block frame cm); nothing here decides what is seen.</summary>
    public sealed partial class HostViews
    {
        const float ShadowRimW = 0.045f, ShadowInkW = 0.08f, ShadowInkA = 0.45f;
        static readonly Color InkDeepC = new(0x0b / 255f, 0x16 / 255f, 0x15 / 255f);
        readonly List<Vector3[]> shadows = new();
        readonly List<Vector3> underV = new(); readonly List<int> underT = new();

        /// <summary>This fan's shadows in world space (xz used; y from the ground under each corner).</summary>
        void ShadowsOf(Fan f)
        {
            shadows.Clear();
            foreach (var sp in f.Shadows) { if (sp == null || sp.Length < 3) continue; var w = new Vector3[sp.Length]; for (int i = 0; i < sp.Length; i++) w[i] = frame.World(sp[i]); shadows.Add(w); }
        }

        static float Cross(Vector3 a, Vector3 b, Vector3 p) => (b.x - a.x) * (p.z - a.z) - (b.z - a.z) * (p.x - a.x);
        /// <summary>Inside a convex polygon (either winding), on the ground plane.</summary>
        static bool InConvex(Vector3[] poly, Vector3 p)
        {
            int n = poly.Length; float sign = 0f;
            for (int i = 0; i < n; i++) { float c = Cross(poly[i], poly[(i + 1) % n], p); if (Mathf.Abs(c) < 1e-6f) continue; if (sign == 0f) sign = Mathf.Sign(c); else if (Mathf.Sign(c) != sign) return false; }
            return true;
        }
        bool InShadow(Vector3 p) { foreach (var s in shadows) if (InConvex(s, p)) return true; return false; }
        static float Winding(Vector3[] poly) { float a = 0f; for (int i = 0; i < poly.Length; i++) { var p = poly[i]; var q = poly[(i + 1) % poly.Length]; a += p.x * q.z - q.x * p.z; } return Mathf.Sign(a); }

        /// <summary>Sutherland–Hodgman against one edge's half-plane: keep = the side `side` (+1 left, -1 right of a→b on the ground).</summary>
        static List<Vector3> HalfPlane(List<Vector3> poly, Vector3 a, Vector3 b, float side)
        {
            var o = new List<Vector3>(poly.Count + 2);
            for (int i = 0; i < poly.Count; i++)
            {
                var p = poly[i]; var q = poly[(i + 1) % poly.Count]; float cp = Cross(a, b, p) * side, cq = Cross(a, b, q) * side;
                if (cp >= 0f) o.Add(p);
                if ((cp >= 0f) != (cq >= 0f)) o.Add(Vector3.Lerp(p, q, cp / (cp - cq)));
            }
            return o;
        }

        /// <summary>The fill's triangles (verts/tris as Build left them) with every shadow cut out: a convex piece minus a convex shadow is the
        /// pieces outside each of the shadow's edges in turn.</summary>
        void CutFill()
        {
            if (shadows.Count == 0) return;
            var pieces = new List<List<Vector3>>();
            for (int t = 0; t + 2 < tris.Count; t += 3) pieces.Add(new List<Vector3> { verts[tris[t]], verts[tris[t + 1]], verts[tris[t + 2]] });
            foreach (var sh in shadows)
            {
                float wind = Winding(sh); Vector3 lo = sh[0], hi = sh[0]; foreach (var p in sh) { lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p); }
                var next = new List<List<Vector3>>();
                foreach (var pc in pieces)
                {
                    bool apart = true; foreach (var p in pc) if (p.x >= lo.x && p.x <= hi.x && p.z >= lo.z && p.z <= hi.z) { apart = false; break; }
                    if (apart) { float px0 = float.MaxValue, px1 = float.MinValue, pz0 = float.MaxValue, pz1 = float.MinValue; foreach (var p in pc) { px0 = Mathf.Min(px0, p.x); px1 = Mathf.Max(px1, p.x); pz0 = Mathf.Min(pz0, p.z); pz1 = Mathf.Max(pz1, p.z); } apart = px1 < lo.x || px0 > hi.x || pz1 < lo.z || pz0 > hi.z; }
                    if (apart) { next.Add(pc); continue; }
                    var rest = pc;
                    for (int i = 0; i < sh.Length && rest.Count >= 3; i++)
                    {
                        var a = sh[i]; var b = sh[(i + 1) % sh.Length];
                        var outside = HalfPlane(rest, a, b, -wind); if (outside.Count >= 3) next.Add(outside);
                        rest = HalfPlane(rest, a, b, wind);
                    }
                }
                pieces = next;
            }
            verts.Clear(); tris.Clear();
            foreach (var pc in pieces) { int b0 = verts.Count; verts.AddRange(pc); for (int i = 1; i + 1 < pc.Count; i++) { tris.Add(b0); tris.Add(b0 + i); tris.Add(b0 + i + 1); } }
        }

        /// <summary>A rim quad with the stretches inside a shadow left out.</summary>
        void QuadCut(Vector3 a, Vector3 b, float w)
        {
            if (shadows.Count == 0) { Quad(a, b, w); return; }
            const int n = 24; int start = -1;
            for (int i = 0; i <= n; i++)
            {
                bool vis = i < n && !InShadow(Vector3.Lerp(a, b, (i + 0.5f) / n));
                if (vis && start < 0) start = i;
                if (!vis && start >= 0) { Quad(Vector3.Lerp(a, b, start / (float)n), Vector3.Lerp(a, b, i / (float)n), w); start = -1; }
            }
        }

        static bool InRing(List<Vector3> ring, Vector3 p)
        {
            bool inside = false; int n = ring.Count;
            for (int i = 0, j = n - 1; i < n; j = i++) { var a = ring[i]; var b = ring[j]; if ((a.z > p.z) != (b.z > p.z) && p.x < (b.x - a.x) * (p.z - a.z) / (b.z - a.z) + a.x) inside = !inside; }
            return inside;
        }

        /// <summary>Each shadow's edges where they run inside the fan (and outside every other shadow): the fan's colour on the rim mesh, ink-deep
        /// under it in the fan's own under mesh.</summary>
        void ShadowRims(List<(List<Vector3> ring, string[] stop)> polys, Color c, FanDraw d)
        {
            underV.Clear(); underT.Clear();
            for (int s = 0; s < shadows.Count; s++)
            {
                var sh = shadows[s];
                for (int i = 0; i < sh.Length; i++)
                {
                    var a = sh[i]; var b = sh[(i + 1) % sh.Length]; float len = Vector3.Distance(Flat(a), Flat(b)); int n = Mathf.Max(2, Mathf.CeilToInt(len / 0.1f)); int start = -1;
                    for (int k = 0; k <= n; k++)
                    {
                        bool on = false;
                        if (k < n)
                        {
                            var p = Vector3.Lerp(a, b, (k + 0.5f) / n); foreach (var (ring, _) in polys) if (ring.Count >= 3 && InRing(ring, p)) { on = true; break; }
                            if (on) for (int o = 0; o < shadows.Count; o++) if (o != s && InConvex(shadows[o], p)) { on = false; break; }
                        }
                        if (on && start < 0) start = k;
                        if (!on && start >= 0)
                        {
                            var p0 = Vector3.Lerp(a, b, start / (float)n) + Vector3.up * (RimY + 0.004f); var p1 = Vector3.Lerp(a, b, k / (float)n) + Vector3.up * (RimY + 0.004f);
                            Quad(p0, p1, ShadowRimW); UnderQuad(p0 - Vector3.up * 0.009f, p1 - Vector3.up * 0.009f, ShadowInkW); start = -1;
                        }
                    }
                }
            }
            d.Under.Clear(); d.Under.SetVertices(underV); d.Under.SetTriangles(underT, 0); d.Under.RecalculateBounds();
            var um = FanMat(InkDeepC, ShadowInkA); if (d.UnderR.sharedMaterial != um) d.UnderR.sharedMaterial = um;
        }

        void UnderQuad(Vector3 a, Vector3 b, float w)
        {
            var side = Vector3.Cross(Vector3.up, Flat(b - a)).normalized * (w * 0.5f); var ext = Flat(b - a).normalized * (w * 0.25f); int k = underV.Count;
            underV.Add(a - ext - side); underV.Add(a - ext + side); underV.Add(b + ext + side); underV.Add(b + ext - side);
            underT.Add(k); underT.Add(k + 1); underT.Add(k + 2); underT.Add(k); underT.Add(k + 2); underT.Add(k + 3);
        }
    }
}
