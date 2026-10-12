using System.Collections.Generic;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>A wall edge a→b in plan: s along it, d outward (right of the direction, outward for a counter-clockwise ring),
    /// z up. P gives the Unity point.</summary>
    public readonly struct WallFrame
    {
        public readonly Vector2 A, U, N; public readonly float Len;
        public WallFrame(Vector2 a, Vector2 b)
        {
            A = a; Len = (b - a).magnitude;
            U = Len > 1e-6f ? (b - a) / Len : Vector2.right; N = new Vector2(U.y, -U.x);
        }
        public Vector3 P(float s, float z, float d) { var q = A + U * s + N * d; return new Vector3(q.x, z, q.y); }
        public Vector3 Out => new(N.x, 0, N.y);
        public Vector3 Along => new(U.x, 0, U.y);
        public float S(Vector2 q) => Vector2.Dot(q - A, U);
        public float D(Vector2 q) => Vector2.Dot(q - A, N);
        /// <summary>The same wall from s0 to s1.</summary>
        public WallFrame Sub(float s0, float s1) => new(A + U * s0, A + U * s1);
    }

    /// <summary>Faces and boxes painted with atlas swatches into MeshKits (flat shaded: every face has its own vertices).</summary>
    public sealed class QuadKit
    {
        readonly Atlas atlas;
        public QuadKit(Atlas atlas) => this.atlas = atlas;

        /// <summary>Quad p0..p3 (a loop) facing `outward`; painted with the swatch.</summary>
        public void Face(MeshKit k, string sw, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 outward)
        {
            int f = k.VertexCount;
            int a = k.Vert(p0), b = k.Vert(p1), c = k.Vert(p2), d = k.Vert(p3);
            if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), outward) >= 0) { k.Tri(a, b, c); k.Tri(a, c, d); }
            else { k.Tri(a, c, b); k.Tri(a, d, c); }
            k.Paint(f, atlas.Uv(sw));
        }

        /// <summary>Wall-aligned rectangle at depth d.</summary>
        public void Rect(MeshKit k, string sw, in WallFrame w, float s0, float s1, float z0, float z1, float d) =>
            Face(k, sw, w.P(s0, z0, d), w.P(s0, z1, d), w.P(s1, z1, d), w.P(s1, z0, d), w.Out);

        /// <summary>Wall-aligned box between depths d0 and d1; front, both ends, top (and optionally bottom).</summary>
        public void Box(MeshKit k, string sw, in WallFrame w, float s0, float s1, float z0, float z1, float d0, float d1, string top = null, bool bottom = false)
        {
            Rect(k, sw, w, s0, s1, z0, z1, d1);
            Face(k, sw, w.P(s0, z0, d0), w.P(s0, z1, d0), w.P(s0, z1, d1), w.P(s0, z0, d1), -w.Along);
            Face(k, sw, w.P(s1, z0, d0), w.P(s1, z1, d0), w.P(s1, z1, d1), w.P(s1, z0, d1), w.Along);
            Face(k, top ?? sw, w.P(s0, z1, d0), w.P(s0, z1, d1), w.P(s1, z1, d1), w.P(s1, z1, d0), Vector3.up);
            if (bottom) Face(k, sw, w.P(s0, z0, d0), w.P(s0, z0, d1), w.P(s1, z0, d1), w.P(s1, z0, d0), Vector3.down);
        }

        /// <summary>Upward-facing polygon (counter-clockwise ring) at height h.</summary>
        public void Cap(MeshKit k, string sw, Vector2[] ccw, float h)
        {
            int f = k.VertexCount;
            k.Cap(ccw, h);
            k.Paint(f, atlas.Uv(sw));
        }

        /// <summary>Flat polygon in the wall plane at depth d, fanned from `centre` (must see every vertex), facing outward. Points are (s, z).</summary>
        public void Fan(MeshKit k, string sw, in WallFrame w, IList<Vector2> pts, Vector2 centre, float d)
        {
            int f = k.VertexCount, c = k.Vert(w.P(centre.x, centre.y, d));
            var ids = new int[pts.Count];
            for (int i = 0; i < pts.Count; i++) ids[i] = k.Vert(w.P(pts[i].x, pts[i].y, d));
            for (int i = 0; i < pts.Count; i++)
            {
                int a = ids[i], b = ids[(i + 1) % pts.Count];
                if (Vector3.Dot(Vector3.Cross(k.At(a) - k.At(c), k.At(b) - k.At(c)), w.Out) >= 0) k.Tri(c, a, b); else k.Tri(c, b, a);
            }
            k.Paint(f, atlas.Uv(sw));
        }

        /// <summary>Paints everything added to the kit from vertex `from` on.</summary>
        public void Paint(MeshKit k, int from, string sw) => k.Paint(from, atlas.Uv(sw));
    }
}
