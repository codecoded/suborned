#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Accumulates flat-shaded massing (boxes, drums, proud ribbon-window bands) into one mesh with a submesh per
    /// material slot. Faces wind clockwise seen from outside (Unity front faces); bottoms are omitted.
    internal sealed class DecoKitMassMesh
    {
        readonly List<Vector3> v = new(), n = new();
        readonly List<Vector2> uv = new();
        readonly List<int>[] tris;

        internal DecoKitMassMesh(int slots)
        {
            tris = new List<int>[slots];
            for (int i = 0; i < slots; i++) tris[i] = new List<int>();
        }

        internal int Triangles { get { int t = 0; foreach (var l in tris) t += l.Count / 3; return t; } }

        /// Quad a-b-c-d, clockwise seen from the side the normal points to.
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, int slot)
        {
            int i = v.Count;
            v.AddRange(new[] { a, b, c, d });
            for (int k = 0; k < 4; k++) n.Add(normal);
            uv.AddRange(new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) });
            tris[slot].AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        }

        /// Axis-aligned box standing on y0: four walls and a roof.
        internal void Box(Vector3 foot, Vector2 half, float y0, float y1, int slot, bool roof = true)
        {
            float x0 = foot.x - half.x, x1 = foot.x + half.x, z0 = foot.z - half.y, z1 = foot.z + half.y;
            Quad(new(x0, y0, z0), new(x0, y1, z0), new(x1, y1, z0), new(x1, y0, z0), Vector3.back, slot);
            Quad(new(x1, y0, z0), new(x1, y1, z0), new(x1, y1, z1), new(x1, y0, z1), Vector3.right, slot);
            Quad(new(x1, y0, z1), new(x1, y1, z1), new(x0, y1, z1), new(x0, y0, z1), Vector3.forward, slot);
            Quad(new(x0, y0, z1), new(x0, y1, z1), new(x0, y1, z0), new(x0, y0, z0), Vector3.left, slot);
            if (roof) Quad(new(x0, y1, z0), new(x0, y1, z1), new(x1, y1, z1), new(x1, y1, z0), Vector3.up, slot);
        }

        /// Ribbon windows: bands of height h every pitch from y0 to y1, standing proud of a box's walls.
        internal void Bands(Vector3 foot, Vector2 half, float y0, float y1, float pitch, float h, int slot)
        {
            var out1 = half + new Vector2(.06f, .06f);
            for (float y = y0; y + h < y1; y += pitch) Box(foot, out1, y, y + h, slot, roof: false);
        }

        /// Upright prism (a drum when sides >= 10), optionally capped.
        internal void Drum(Vector3 foot, float r, float y0, float y1, int sides, int slot, bool roof = true)
        {
            for (int k = 0; k < sides; k++)
            {
                float a0 = k * Mathf.PI * 2f / sides, a1 = (k + 1) * Mathf.PI * 2f / sides;
                Vector3 p0 = foot + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r;
                Vector3 p1 = foot + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r;
                Vector3 nn = new Vector3(Mathf.Cos((a0 + a1) * .5f), 0, Mathf.Sin((a0 + a1) * .5f));
                Quad(p0 + Vector3.up * y0, p0 + Vector3.up * y1, p1 + Vector3.up * y1, p1 + Vector3.up * y0, nn, slot);
                if (roof)
                {
                    int i = v.Count;
                    v.AddRange(new[] { foot + Vector3.up * y1, p1 + Vector3.up * y1, p0 + Vector3.up * y1 });
                    n.AddRange(new[] { Vector3.up, Vector3.up, Vector3.up });
                    uv.AddRange(new[] { Vector2.zero, Vector2.up, Vector2.one });
                    tris[slot].AddRange(new[] { i, i + 1, i + 2 });
                }
            }
        }

        internal Mesh Build(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.subMeshCount = tris.Length;
            for (int i = 0; i < tris.Length; i++) mesh.SetTriangles(tris[i], i);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
#endif
