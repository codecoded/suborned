#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.Rendering;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Flat and stepped ground surfaces with world-metre UVs (metric materials tile themselves): rectangles cut by
    /// discs (cells clipped against the circle's tangent: < 5 mm error at R 26 on 1 m cells), exact annuli, vertical
    /// faces. Accumulate into one builder per surface, then Emit saves the mesh and places it.
    internal sealed class DecoKitGroundMesh
    {
        readonly List<Vector3> v = new();
        readonly List<Vector2> uv = new();
        readonly List<Vector3> n = new();
        readonly List<int> t = new();

        /// Convex polygon on y, CCW seen from above (x right, z up), upward normal.
        void Poly(IList<Vector2> p, float y)
        {
            if (p.Count < 3) return;
            int i0 = v.Count;
            foreach (var q in p) { v.Add(new Vector3(q.x, y, q.y)); uv.Add(q); n.Add(Vector3.up); }
            for (int k = 1; k < p.Count - 1; k++) t.AddRange(new[] { i0, i0 + k + 1, i0 + k });  // clockwise from above = front
        }

        internal DecoKitGroundMesh Rect(Rect r, float y)
        {
            Poly(new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) }, y);
            return this;
        }

        /// Oriented rectangle on y: centre c, unit direction t (x/z), half length along t, half width across it.
        internal DecoKitGroundMesh Quad(Vector2 c, Vector2 t, float halfLength, float halfWidth, float y)
        {
            var a = t * halfLength;
            var b = new Vector2(-t.y, t.x) * halfWidth;
            Poly(new[] { c - a - b, c + a - b, c + a + b, c - a + b }, y);
            return this;
        }

        /// A band of `halfWidth` either side of a polyline on y (closed: back to its start), mitred at the joints.
        internal DecoKitGroundMesh Strip(IList<Vector2> line, float halfWidth, float y, bool closed)
        {
            int n = line.Count, segs = closed ? n : n - 1;
            Vector2 Normal(int i)
            {
                Vector2 a = line[(i - 1 + n) % n], b = line[i], c = line[(i + 1) % n];
                var d0 = !closed && i == 0 ? c - b : b - a;
                var d1 = !closed && i == n - 1 ? b - a : c - b;
                var m = new Vector2(-(d0.normalized + d1.normalized).y, (d0.normalized + d1.normalized).x).normalized;
                return m / Mathf.Max(.5f, Vector2.Dot(m, new Vector2(-d1.normalized.y, d1.normalized.x)));
            }
            for (int i = 0; i < segs; i++)
            {
                int j = (i + 1) % n;
                Vector2 ni = Normal(i) * halfWidth, nj = Normal(j) * halfWidth;
                var q = new[] { line[i] - ni, line[j] - nj, line[j] + nj, line[i] + ni };
                if (Cross(q[1] - q[0], q[2] - q[0]) < 0f) System.Array.Reverse(q);  // keep it CCW from above
                Poly(q, y);
            }
            return this;
        }

        /// A star of `points` tips (outer radius r0, inner r1) at c on y, first tip at `rotation` degrees.
        internal DecoKitGroundMesh Star(Vector2 c, float r0, float r1, int points, float rotation, float y)
        {
            for (int k = 0; k < points * 2; k++)
            {
                float a0 = (rotation + k * 180f / points) * Mathf.Deg2Rad, a1 = (rotation + (k + 1) * 180f / points) * Mathf.Deg2Rad;
                float ra = k % 2 == 0 ? r0 : r1, rb = k % 2 == 0 ? r1 : r0;
                Poly(new[] { c, c + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * ra, c + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * rb }, y);
            }
            return this;
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        /// r minus the disc (c, radius) (or minus its outside when `inside`), meshed in cells of `cell` metres.
        internal DecoKitGroundMesh RectCut(Rect r, Vector2 c, float radius, float y, float cell = 1f, bool inside = false)
        {
            for (float x = r.xMin; x < r.xMax - 1e-3f; x += cell)
                for (float z = r.yMin; z < r.yMax - 1e-3f; z += cell)
                {
                    var q = new List<Vector2>
                    {
                        new(x, z), new(Mathf.Min(x + cell, r.xMax), z),
                        new(Mathf.Min(x + cell, r.xMax), Mathf.Min(z + cell, r.yMax)), new(x, Mathf.Min(z + cell, r.yMax)),
                    };
                    var mid = (q[0] + q[2]) * .5f - c;
                    if (mid.sqrMagnitude < 1e-6f) mid = Vector2.right;
                    var dir = mid.normalized;
                    // keep the side of the tangent line at the circle point nearest the cell: outside (or inside)
                    Poly(Clip(q, c + dir * radius, inside ? -dir : dir), y);
                }
            return this;
        }

        /// Annulus r0..r1 about c, `seg` segments; `gap` > 0 leaves a strip |x - c.x| < gap open (a road through it).
        internal DecoKitGroundMesh Annulus(Vector2 c, float r0, float r1, float y, int seg = 128, float gap = 0f)
        {
            for (int k = 0; k < seg; k++)
            {
                float a0 = k * Mathf.PI * 2f / seg, a1 = (k + 1) * Mathf.PI * 2f / seg;
                Vector2 d0 = new(Mathf.Sin(a0), Mathf.Cos(a0)), d1 = new(Mathf.Sin(a1), Mathf.Cos(a1));
                var q = new List<Vector2> { c + d0 * r0, c + d1 * r0, c + d1 * r1, c + d0 * r1 };  // CCW from above
                if (gap <= 0f) { Poly(q, y); continue; }
                Poly(Clip(q, c + Vector2.right * gap, Vector2.right), y);
                Poly(Clip(q, c + Vector2.left * gap, Vector2.left), y);
            }
            return this;
        }

        internal DecoKitGroundMesh Disc(Vector2 c, float r, float y, int seg = 96) => Annulus(c, 0f, r, y, seg);

        /// Vertical face from a to b (plan), y0..y1, facing the right of a->b seen from above.
        internal DecoKitGroundMesh Face(Vector2 a, Vector2 b, float y0, float y1)
        {
            int i0 = v.Count;
            var d = (b - a).normalized;
            var nn = new Vector3(d.y, 0f, -d.x);
            float len = Vector2.Distance(a, b);
            v.AddRange(new[] { new Vector3(a.x, y0, a.y), new Vector3(a.x, y1, a.y), new Vector3(b.x, y1, b.y), new Vector3(b.x, y0, b.y) });
            uv.AddRange(new[] { new Vector2(0, y0), new Vector2(0, y1), new Vector2(len, y1), new Vector2(len, y0) });
            for (int k = 0; k < 4; k++) n.Add(nn);
            t.AddRange(new[] { i0, i0 + 1, i0 + 2, i0, i0 + 2, i0 + 3 });
            return this;
        }

        /// Vertical cylinder wall of radius r about c, facing out (or in, toward c).
        internal DecoKitGroundMesh Wall(Vector2 c, float r, float y0, float y1, bool outward, int seg = 96)
        {
            for (int k = 0; k < seg; k++)
            {
                float a0 = k * Mathf.PI * 2f / seg, a1 = (k + 1) * Mathf.PI * 2f / seg;
                Vector2 p0 = c + new Vector2(Mathf.Sin(a0), Mathf.Cos(a0)) * r, p1 = c + new Vector2(Mathf.Sin(a1), Mathf.Cos(a1)) * r;
                if (outward) Face(p1, p0, y0, y1); else Face(p0, p1, y0, y1);  // clockwise travel faces the centre
            }
            return this;
        }

        /// A vertical prism on a convex footprint (either winding): top on y1, sides down to y0.
        internal DecoKitGroundMesh Prism(IList<Vector2> footprint, float y0, float y1)
        {
            var q = new List<Vector2>(footprint);
            float area = 0f;
            for (int i = 0; i < q.Count; i++) area += q[i].x * q[(i + 1) % q.Count].y - q[(i + 1) % q.Count].x * q[i].y;
            if (area < 0f) q.Reverse();  // CCW from above: outward on the right of each edge
            Poly(q, y1);
            for (int i = 0; i < q.Count; i++) Face(q[i], q[(i + 1) % q.Count], y0, y1);
            return this;
        }

        /// A solid step block: top on y1, the four sides down to y0.
        internal DecoKitGroundMesh Block(Rect r, float y0, float y1)
        {
            Rect(r, y1);
            Vector2 a = new(r.xMin, r.yMin), b = new(r.xMax, r.yMin), c = new(r.xMax, r.yMax), d = new(r.xMin, r.yMax);
            return Face(a, b, y0, y1).Face(b, c, y0, y1).Face(c, d, y0, y1).Face(d, a, y0, y1);  // CCW: outward on the right
        }

        static List<Vector2> Clip(List<Vector2> poly, Vector2 onLine, Vector2 keep)
        {
            var o = new List<Vector2>();
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 a = poly[i], b = poly[(i + 1) % poly.Count];
                float da = Vector2.Dot(a - onLine, keep), db = Vector2.Dot(b - onLine, keep);
                if (da >= 0f) o.Add(a);
                if (da * db < 0f) o.Add(a + (b - a) * (da / (da - db)));
            }
            return o;
        }

        /// Save the mesh asset and place it (a collider when walkable).
        internal GameObject Emit(string name, Material mat, bool collider = true, Transform parent = null)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            EnsureFolder(GeneratedDir);
            var path = $"{GeneratedDir}/{name.Replace(' ', '_')}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            if (parent) go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.BatchingStatic
                | StaticEditorFlags.ContributeGI);
            return go;
        }

        internal static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MaterialDir}/{name}.mat");

        /// A copy of a UV-mapped kit material tiled per metre: the 4 m paving sheet laid as one surface.
        internal static Material Metric(string name, string source, float metres)
        {
            EnsureFolder(GeneratedDir);
            var path = $"{GeneratedDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) AssetDatabase.CreateAsset(mat = new Material(Mat(source)), path);
            else { mat.shader = Mat(source).shader; mat.CopyPropertiesFromMaterial(Mat(source)); }
            mat.SetTextureScale("_BaseMap", Vector2.one / metres);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// A Lit material made here on a ground sheet (ground_sheets.py): albedo + normal tiled every `metres`.
        internal static Material Sheet(string name, string texture, float metres, float smoothness)
        {
            var mat = Plain(name, "#FFFFFF", smoothness);
            mat.SetTexture("_BaseMap", DecoKitTextures.Albedo($"{texture}.png", false));
            mat.SetTextureScale("_BaseMap", Vector2.one / metres);
            mat.SetTexture("_BumpMap", DecoKitTextures.Normal($"{texture}_normal.png"));
            mat.SetFloat("_BumpScale", 1f);
            BaseShaderGUI.SetMaterialKeywords(mat, LitGUI.SetMaterialKeywords);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// Mown lawn (8 m sheet: 1 m stripes, clover drifts) and the road surface (16 m sheet: patches, sealed cracks).
        internal static Material Lawn() => Sheet("DK_Lawn", "lawn_v1", 8f, .12f);
        internal static Material Road() => Sheet("DK_Asphalt_Road", "asphalt_road_v1", 16f, .28f);

        /// A plain Lit material made here (pool floor, paint): colour in sRGB hex.
        internal static Material Plain(string name, string hex, float smoothness)
        {
            EnsureFolder(GeneratedDir);
            var path = $"{GeneratedDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) AssetDatabase.CreateAsset(mat = new Material(Shader.Find("OldTown/DecoKit/Lit Cutaway")), path);
            else mat.shader = Shader.Find("OldTown/DecoKit/Lit Cutaway");  // roof lawns stand in the cutaway's reach
            ColorUtility.TryParseHtmlString(hex, out var c);
            mat.SetColor("_BaseColor", c);
            mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
#endif
