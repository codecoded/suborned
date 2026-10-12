using System.Collections.Generic;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Surface classes the FacadeLit shader reads from the vertex alpha (A = 1 - class / 8).</summary>
    public static class Cls { public const int Matte = 0, Stone = 1, Enamel = 2, Glass = 3, Chrome = 4, Neon = 5, Brass = 6; }

    /// <summary>Paint builder over a MeshKit: quads, boxes with chamfers, profile extrusions and plates painted with atlas
    /// swatches, each vertex carrying ambient occlusion (R) and surface class (A). Flat shaded: every face has its own vertices.</summary>
    public sealed class Pb
    {
        public readonly MeshKit K; readonly Atlas at;
        public Pb(MeshKit k, Atlas a) { K = k; at = a; }

        static Color32 C(float ao, int cls) => new((byte)Mathf.RoundToInt(Mathf.Clamp01(ao) * 255f), 255, 255, (byte)Mathf.RoundToInt(255f * (1f - cls / 8f)));

        void Tint(int from, int cls, params float[] ao)
        {
            for (int i = from; i < K.VertexCount; i++) K.Colour(i, C(ao[Mathf.Min(i - from, ao.Length - 1)], cls));
        }

        public void Quad(string sw, int cls, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward, float ao0 = 1, float ao1 = 1, float ao2 = 1, float ao3 = 1)
        {
            int f = K.VertexCount;
            int ia = K.Vert(a), ib = K.Vert(b), ic = K.Vert(c), id = K.Vert(d);
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) >= 0) { K.Tri(ia, ib, ic); K.Tri(ia, ic, id); }
            else { K.Tri(ia, ic, ib); K.Tri(ia, id, ic); }
            K.Paint(f, at.Uv(sw)); Tint(f, cls, ao0, ao1, ao2, ao3);
        }

        public void Tri3(string sw, int cls, Vector3 a, Vector3 b, Vector3 c, Vector3 outward, float ao = 1)
        {
            int f = K.VertexCount;
            int ia = K.Vert(a), ib = K.Vert(b), ic = K.Vert(c);
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) >= 0) K.Tri(ia, ib, ic); else K.Tri(ia, ic, ib);
            K.Paint(f, at.Uv(sw)); Tint(f, cls, ao);
        }

        /// <summary>Convex polygon fanned from its centroid.</summary>
        public void Fan(string sw, int cls, IList<Vector3> pts, Vector3 outward, float ao = 1)
        {
            var c = Vector3.zero; foreach (var p in pts) c += p; c /= pts.Count;
            for (int i = 0; i < pts.Count; i++) Tri3(sw, cls, c, pts[i], pts[(i + 1) % pts.Count], outward, ao);
        }

        /// <summary>Convex polygon fanned from its centroid with shared vertices (one colour).</summary>
        public void FanShared(string sw, int cls, IList<Vector3> pts, Vector3 outward, float ao = 1)
        {
            var c = Vector3.zero; foreach (var p in pts) c += p; c /= pts.Count;
            int f = K.VertexCount, ic = K.Vert(c); var ids = new int[pts.Count];
            for (int i = 0; i < pts.Count; i++) ids[i] = K.Vert(pts[i]);
            for (int i = 0; i < pts.Count; i++)
            {
                int a = ids[i], b = ids[(i + 1) % pts.Count];
                if (Vector3.Dot(Vector3.Cross(K.At(a) - K.At(ic), K.At(b) - K.At(ic)), outward) >= 0) K.Tri(ic, a, b); else K.Tri(ic, b, a);
            }
            K.Paint(f, at.Uv(sw)); Tint(f, cls, ao);
        }

        public void Cap(string sw, int cls, Vector2[] ccw, float h)
        {
            int f = K.VertexCount; K.Cap(ccw, h); K.Paint(f, at.Uv(sw)); Tint(f, cls, 1f);
        }

        // ---- wall-frame helpers (s along, z up, d out)

        public void R(string sw, int cls, in WallFrame w, float s0, float s1, float z0, float z1, float d, float aoLo = 1, float aoHi = 1) =>
            Quad(sw, cls, w.P(s0, z0, d), w.P(s0, z1, d), w.P(s1, z1, d), w.P(s1, z0, d), w.Out, aoLo, aoHi, aoHi, aoLo);

        /// <summary>Box from depth d0 to d1; front inset by bevel b with chamfers on all four edges. top = a different top swatch.</summary>
        public void Box(string sw, int cls, in WallFrame w, float s0, float s1, float z0, float z1, float d0, float d1, float b = 0f, string top = null, bool bottom = false, float ao = 0.92f)
        {
            if (s1 - s0 < 0.01f || z1 - z0 < 0.01f) return;
            if (Mathf.Min(s1 - s0, z1 - z0) < 0.5f) b = 0f; // small trim stays square: a bevel costs 44 vertices against 16
            b = Mathf.Min(b, (s1 - s0) * 0.4f, (z1 - z0) * 0.4f, (d1 - d0) * 0.9f);
            float df = d1 - b;
            if (b > 0.004f)
            {
                R(sw, cls, w, s0 + b, s1 - b, z0 + b, z1 - b, d1);
                Quad(sw, cls, w.P(s0, z0 + b, df), w.P(s0, z1 - b, df), w.P(s0 + b, z1 - b, d1), w.P(s0 + b, z0 + b, d1), -w.Along + w.Out);
                Quad(sw, cls, w.P(s1, z0 + b, df), w.P(s1, z1 - b, df), w.P(s1 - b, z1 - b, d1), w.P(s1 - b, z0 + b, d1), w.Along + w.Out);
                Quad(top ?? sw, cls, w.P(s0 + b, z1, df), w.P(s1 - b, z1, df), w.P(s1 - b, z1 - b, d1), w.P(s0 + b, z1 - b, d1), Vector3.up + w.Out);
                Quad(sw, cls, w.P(s0 + b, z0, df), w.P(s1 - b, z0, df), w.P(s1 - b, z0 + b, d1), w.P(s0 + b, z0 + b, d1), Vector3.down + w.Out, 0.7f, 0.7f, 0.85f, 0.85f);
                // corner triangles of the bevel
                Tri3(sw, cls, w.P(s0, z1 - b, df), w.P(s0 + b, z1, df), w.P(s0 + b, z1 - b, d1), Vector3.up - w.Along + w.Out);
                Tri3(sw, cls, w.P(s1, z1 - b, df), w.P(s1 - b, z1, df), w.P(s1 - b, z1 - b, d1), Vector3.up + w.Along + w.Out);
                Tri3(sw, cls, w.P(s0, z0 + b, df), w.P(s0 + b, z0, df), w.P(s0 + b, z0 + b, d1), Vector3.down - w.Along + w.Out, 0.8f);
                Tri3(sw, cls, w.P(s1, z0 + b, df), w.P(s1 - b, z0, df), w.P(s1 - b, z0 + b, d1), Vector3.down + w.Along + w.Out, 0.8f);
            }
            else R(sw, cls, w, s0, s1, z0, z1, d1);
            Quad(sw, cls, w.P(s0, z0, d0), w.P(s0, z1, d0), w.P(s0, z1, df), w.P(s0, z0, df), -w.Along, ao, ao, ao, ao);
            Quad(sw, cls, w.P(s1, z0, d0), w.P(s1, z1, d0), w.P(s1, z1, df), w.P(s1, z0, df), w.Along, ao, ao, ao, ao);
            Quad(top ?? sw, cls, w.P(s0, z1, d0), w.P(s0, z1, df), w.P(s1, z1, df), w.P(s1, z1, d0), Vector3.up);
            if (bottom) Quad(sw, cls, w.P(s0, z0, d0), w.P(s0, z0, df), w.P(s1, z0, df), w.P(s1, z0, d0), Vector3.down, 0.5f, 0.6f, 0.6f, 0.5f);
        }

        /// <summary>Outward normal in the wall's (s, d) plane for a vertical face along the plan vector (ds, dd).</summary>
        public static Vector3 OutOf(in WallFrame w, float ds, float dd)
        {
            float ns = -dd, nd = ds; if (nd < 0) { ns = -ns; nd = -nd; }
            return w.Along * ns + w.Out * nd;
        }

        /// <summary>Polygon of (d, z) points extruded along the wall from s0 to s1 (end caps optional), outward normals from the winding.</summary>
        public void Extrude(string sw, int cls, in WallFrame w, float s0, float s1, IList<Vector2> dz, bool capL = true, bool capR = true, float ao = 1f)
        {
            float area = 0; int n = dz.Count;
            for (int i = 0; i < n; i++) { var a = dz[i]; var b = dz[(i + 1) % n]; area += a.x * b.y - b.x * a.y; }
            float sg = area >= 0 ? 1f : -1f;
            for (int i = 0; i < n; i++)
            {
                var a = dz[i]; var b = dz[(i + 1) % n]; var e = b - a;
                if (e.sqrMagnitude < 1e-6f) continue;
                var nrm = new Vector2(e.y, -e.x) * sg; // right of the direction for a CCW polygon = outward
                var o = w.Out * nrm.x + Vector3.up * nrm.y;
                Quad(sw, cls, w.P(s0, a.y, a.x), w.P(s0, b.y, b.x), w.P(s1, b.y, b.x), w.P(s1, a.y, a.x), o, ao, ao, ao, ao);
            }
            if (!capL && !capR) return;
            var tri = Poly.Triangulate(sg > 0 ? ToArr(dz) : Rev(dz));
            for (int e = 0; e < 2; e++)
            {
                if (e == 0 && !capL || e == 1 && !capR) continue;
                float s = e == 0 ? s0 : s1; var o = e == 0 ? -w.Along : w.Along;
                var pts = sg > 0 ? ToArr(dz) : Rev(dz);
                for (int i = 0; i < tri.Length; i += 3)
                    Tri3(sw, cls, w.P(s, pts[tri[i]].y, pts[tri[i]].x), w.P(s, pts[tri[i + 1]].y, pts[tri[i + 1]].x), w.P(s, pts[tri[i + 2]].y, pts[tri[i + 2]].x), o, ao);
            }
        }

        static Vector2[] ToArr(IList<Vector2> l) { var a = new Vector2[l.Count]; l.CopyTo(a, 0); return a; }
        static Vector2[] Rev(IList<Vector2> l) { var a = ToArr(l); System.Array.Reverse(a); return a; }

        /// <summary>A flat plate: polygon (u, v) mapped into 3D by `map`, thick along `nrm` (both faces and the rim).</summary>
        public void Plate(string sw, int cls, IList<Vector2> poly, System.Func<Vector2, Vector3> map, Vector3 nrm, float thick, bool backFace = true, float ao = 0.95f)
        {
            var pts = ToArr(poly);
            float area = 0; for (int i = 0; i < pts.Length; i++) { var a = pts[i]; var b = pts[(i + 1) % pts.Length]; area += a.x * b.y - b.x * a.y; }
            if (area < 0) System.Array.Reverse(pts);
            var tri = Poly.Triangulate(pts);
            var fo = nrm * (thick * 0.5f);
            for (int i = 0; i < tri.Length; i += 3)
            {
                Tri3(sw, cls, map(pts[tri[i]]) + fo, map(pts[tri[i + 1]]) + fo, map(pts[tri[i + 2]]) + fo, nrm, ao);
                if (backFace) Tri3(sw, cls, map(pts[tri[i]]) - fo, map(pts[tri[i + 1]]) - fo, map(pts[tri[i + 2]]) - fo, -nrm, ao * 0.9f);
            }
            for (int i = 0; i < pts.Length; i++)
            {
                var a = pts[i]; var b = pts[(i + 1) % pts.Length];
                if ((b - a).sqrMagnitude < 1e-6f) continue;
                var pa = map(a); var pb = map(b);
                var mid = (pa + pb) * 0.5f; var cen = Vector3.zero; foreach (var p in pts) cen += map(p); cen /= pts.Length;
                var o = (mid - cen); o -= nrm * Vector3.Dot(o, nrm);
                Quad(sw, cls, pa + fo, pb + fo, pb - fo, pa - fo, o, ao, ao, ao, ao);
            }
        }

        /// <summary>Thin strip along a polygon's edges, inset from the outline: the neon tube.</summary>
        public void Tube(string sw, int cls, IList<Vector2> poly, System.Func<Vector2, Vector3> map, Vector3 nrm, float offset, float inset, float width)
        {
            var pts = ToArr(poly);
            float area = 0; for (int i = 0; i < pts.Length; i++) { var a = pts[i]; var b = pts[(i + 1) % pts.Length]; area += a.x * b.y - b.x * a.y; }
            if (area < 0) System.Array.Reverse(pts);
            for (int i = 0; i < pts.Length; i++)
            {
                var a = pts[i]; var b = pts[(i + 1) % pts.Length]; var e = (b - a);
                if (e.magnitude < 0.05f) continue; e.Normalize();
                var inn = new Vector2(-e.y, e.x); // left of a CCW edge = inside
                var a0 = a + inn * inset; var b0 = b + inn * inset; var a1 = a0 + inn * width; var b1 = b0 + inn * width;
                var o = nrm * offset;
                Quad(sw, cls, map(a0) + o, map(b0) + o, map(b1) + o, map(a1) + o, nrm, 1, 1, 1, 1);
            }
        }
    }
}
