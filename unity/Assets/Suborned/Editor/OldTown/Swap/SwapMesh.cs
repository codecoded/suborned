#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 075 swap (Latch): the mesh operations the cut needs, on any mesh (merged tiles, slabs, city mass chunks, our batches):
    /// connected components (vertices welded at 1 mm in world space) and an exact XZ clip of triangles against a rectangle, keeping either
    /// the part inside it or the part outside (the outside of a rectangle is four disjoint convex regions, each a Sutherland-Hodgman clip).
    /// Attributes (normal, tangent, uv0, uv1, colour) are interpolated along the cut edges.</summary>
    internal static class SwapMesh
    {
        internal struct V
        {
            public Vector3 P, W, N; public Vector4 T; public Vector2 U0, U1; public Color C;
            public static V Lerp(V a, V b, float t) => new V
            {
                P = Vector3.LerpUnclamped(a.P, b.P, t), W = Vector3.LerpUnclamped(a.W, b.W, t), N = Vector3.LerpUnclamped(a.N, b.N, t).normalized,
                T = Vector4.LerpUnclamped(a.T, b.T, t), U0 = Vector2.LerpUnclamped(a.U0, b.U0, t), U1 = Vector2.LerpUnclamped(a.U1, b.U1, t), C = Color.LerpUnclamped(a.C, b.C, t),
            };
        }

        /// <summary>A mesh read out: per vertex attributes (world position from l2w), per submesh triangles.</summary>
        internal sealed class Read
        {
            public V[] Verts; public List<int[]> Subs = new(); public bool HasN, HasT, HasU0, HasU1, HasC;
            public Read(Mesh m, Matrix4x4 l2w)
            {
                var p = m.vertices; var n = m.normals; var t = m.tangents; var u0 = new List<Vector2>(); m.GetUVs(0, u0); var u1 = new List<Vector2>(); m.GetUVs(1, u1); var c = m.colors;
                HasN = n.Length == p.Length; HasT = t.Length == p.Length; HasU0 = u0.Count == p.Length; HasU1 = u1.Count == p.Length; HasC = c.Length == p.Length;
                Verts = new V[p.Length];
                for (int i = 0; i < p.Length; i++)
                    Verts[i] = new V { P = p[i], W = l2w.MultiplyPoint3x4(p[i]), N = HasN ? n[i] : Vector3.up, T = HasT ? t[i] : Vector4.zero, U0 = HasU0 ? u0[i] : default, U1 = HasU1 ? u1[i] : default, C = HasC ? c[i] : Color.white };
                for (int s = 0; s < m.subMeshCount; s++) Subs.Add(m.GetSubMesh(s).topology == MeshTopology.Triangles ? m.GetTriangles(s) : new int[0]);
            }

            /// <summary>Component id per triangle (per submesh), vertices welded at 1 mm in world space.</summary>
            public List<int[]> Components(out int count)
            {
                var weld = new Dictionary<Vector3Int, int>(Verts.Length);
                var wid = new int[Verts.Length];
                for (int i = 0; i < Verts.Length; i++)
                {
                    var k = Vector3Int.RoundToInt(Verts[i].W * 1000f);
                    if (!weld.TryGetValue(k, out var id)) weld[k] = id = weld.Count;
                    wid[i] = id;
                }
                var parent = new int[weld.Count];
                for (int i = 0; i < parent.Length; i++) parent[i] = i;
                int Find(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }
                foreach (var tris in Subs)
                    for (int i = 0; i + 2 < tris.Length; i += 3)
                    {
                        int a = Find(wid[tris[i]]), b = Find(wid[tris[i + 1]]), c = Find(wid[tris[i + 2]]);
                        if (a != b) parent[a] = b; b = Find(b); if (b != c) parent[b] = c;
                    }
                var remap = new Dictionary<int, int>();
                var outIds = new List<int[]>();
                foreach (var tris in Subs)
                {
                    var ids = new int[tris.Length / 3];
                    for (int i = 0; i < ids.Length; i++)
                    {
                        int r = Find(wid[tris[3 * i]]);
                        if (!remap.TryGetValue(r, out var c)) remap[r] = c = remap.Count;
                        ids[i] = c;
                    }
                    outIds.Add(ids);
                }
                count = remap.Count;
                return outIds;
            }
        }

        /// <summary>Collects output triangles (vertex structs) per submesh and writes a mesh in the source's local space.</summary>
        internal sealed class Write
        {
            readonly List<V> verts = new(); readonly List<List<int>> subs = new(); readonly Read src;
            readonly Dictionary<int, int> fromSrc = new();
            public int Triangles { get; private set; }
            public Write(Read src) { this.src = src; for (int s = 0; s < src.Subs.Count; s++) subs.Add(new List<int>()); }
            public void Keep(int sub, int a, int b, int c) { subs[sub].Add(Src(a)); subs[sub].Add(Src(b)); subs[sub].Add(Src(c)); Triangles++; }
            int Src(int i) { if (!fromSrc.TryGetValue(i, out var o)) { fromSrc[i] = o = verts.Count; verts.Add(src.Verts[i]); } return o; }
            public void Poly(int sub, List<V> poly)
            {
                if (poly.Count < 3) return;
                int b = verts.Count; verts.AddRange(poly);
                for (int i = 1; i + 1 < poly.Count; i++) { subs[sub].Add(b); subs[sub].Add(b + i); subs[sub].Add(b + i + 1); Triangles++; }
            }
            public Mesh Mesh(string name)
            {
                var m = new Mesh { name = name, indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                var p = new Vector3[verts.Count]; for (int i = 0; i < p.Length; i++) p[i] = verts[i].P; m.vertices = p;
                if (src.HasN) { var a = new Vector3[verts.Count]; for (int i = 0; i < a.Length; i++) a[i] = verts[i].N; m.normals = a; }
                if (src.HasT) { var a = new Vector4[verts.Count]; for (int i = 0; i < a.Length; i++) a[i] = verts[i].T; m.tangents = a; }
                if (src.HasU0) { var a = new Vector2[verts.Count]; for (int i = 0; i < a.Length; i++) a[i] = verts[i].U0; m.uv = a; }
                if (src.HasU1) { var a = new Vector2[verts.Count]; for (int i = 0; i < a.Length; i++) a[i] = verts[i].U1; m.uv2 = a; }
                if (src.HasC) { var a = new Color[verts.Count]; for (int i = 0; i < a.Length; i++) a[i] = verts[i].C; m.colors = a; }
                m.subMeshCount = subs.Count;
                for (int s = 0; s < subs.Count; s++) m.SetTriangles(subs[s], s, false);
                m.RecalculateBounds();
                return m;
            }
        }

        /// <summary>Half-plane a*x + b*z + c >= 0 in world XZ.</summary>
        internal readonly struct Half { public readonly float A, B, C; public Half(float a, float b, float c) { A = a; B = b; C = c; } public float D(Vector3 w) => A * w.x + B * w.z + C; }

        internal static List<V> Clip(List<V> poly, Half h)
        {
            var o = new List<V>(poly.Count + 2);
            for (int i = 0; i < poly.Count; i++)
            {
                V a = poly[i], b = poly[(i + 1) % poly.Count];
                float da = h.D(a.W), db = h.D(b.W);
                if (da >= 0f) o.Add(a);
                if ((da >= 0f) != (db >= 0f)) o.Add(V.Lerp(a, b, da / (da - db)));
            }
            return o;
        }

        /// <summary>The convex regions of the plane outside rect (x0..x1, z0..z1): left, right, below-middle, above-middle.</summary>
        internal static Half[][] Outside(Rect r) => new[]
        {
            new[] { new Half(-1, 0, r.xMin) },
            new[] { new Half(1, 0, -r.xMax) },
            new[] { new Half(1, 0, -r.xMin), new Half(-1, 0, r.xMax), new Half(0, -1, r.yMin) },
            new[] { new Half(1, 0, -r.xMin), new Half(-1, 0, r.xMax), new Half(0, 1, -r.yMax) },
        };
        internal static Half[][] Inside(Rect r) => new[] { new[] { new Half(1, 0, -r.xMin), new Half(-1, 0, r.xMax), new Half(0, 1, -r.yMin), new Half(0, -1, r.yMax) } };

        /// <summary>One triangle into the output, clipped to the union of the given disjoint convex regions.</summary>
        internal static void ClipInto(Write w, int sub, V a, V b, V c, Half[][] regions)
        {
            foreach (var reg in regions)
            {
                var poly = new List<V> { a, b, c };
                foreach (var h in reg) { poly = Clip(poly, h); if (poly.Count < 3) break; }
                if (poly.Count >= 3) w.Poly(sub, poly);
            }
        }

        internal static bool Overlaps(Rect a, Rect b, float eps = 0.01f) => a.xMin < b.xMax - eps && a.xMax > b.xMin + eps && a.yMin < b.yMax - eps && a.yMax > b.yMin + eps;
        internal static bool Inside(Rect r, Vector2 p) => p.x >= r.xMin && p.x <= r.xMax && p.y >= r.yMin && p.y <= r.yMax;
        internal static Rect XZ(Bounds b) => Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z);
    }
}
#endif
