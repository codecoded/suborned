using System.Collections.Generic;
using System.Linq;
using Suborned.Sim;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World.Play
{
    // Shared profiles for street-scale corners and the oval planted beds in the store studies.
    // A whole frontage at each LOD is one mesh. No object, animator or light per window or flower.
    sealed class DistrictStreetArchitecture
    {
        const string Mats = "Assets/Suborned/OldTownArt/DecoKit/Materials/DK_";
        sealed class Surface
        {
            readonly List<Vector3> vertices = new();
            readonly List<Vector2> uv = new();
            readonly List<int>[] indices;
            public Surface(int count) { indices = Enumerable.Range(0, count).Select(_ => new List<int>()).ToArray(); }
            void Vertex(Vector3 p, bool deck) { vertices.Add(p); uv.Add(deck ? new Vector2(p.x, p.z) : new Vector2(p.x + p.z, p.y)); }
            public void Face(int mat, Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool top = false)
            {
                int i = vertices.Count;
                Vertex(a, top); Vertex(b, top); Vertex(c, top); Vertex(d, top);
                indices[mat].AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }
            // The clockwise profile includes a vertex at each end of each straight.
            public void Solid(int mat, float x, float z, float width, float depth, float y, float h, float radius, int steps = 3)
            {
                float r = Mathf.Min(radius, Mathf.Min(width, depth) * .49f), w = width / 2, d = depth / 2;
                var ring = new List<Vector3>(4 * (steps + 1));
                for (int k = 0; k < 4; k++)
                {
                    float cx = k <= 1 ? w - r : -w + r, cz = k == 0 || k == 3 ? d - r : -d + r;
                    for (int n = 0; n <= steps; n++)
                    {
                        float a = (90f - k * 90f - n * 90f / steps) * Mathf.Deg2Rad;
                        ring.Add(new Vector3(x + cx + r * Mathf.Cos(a), y, z + cz + r * Mathf.Sin(a)));
                    }
                }
                var up = Vector3.up * h;
                for (int k = 0; k < ring.Count; k++)
                {
                    var a = ring[k]; var b = ring[(k + 1) % ring.Count];
                    Face(mat, a, b, b + up, a + up);
                }
                int first = vertices.Count;
                foreach (var v in ring) Vertex(v + up, true);
                for (int i = 1; i < ring.Count - 1; i++) indices[mat].AddRange(new[] { first, first + i, first + i + 1 });
                first = vertices.Count;
                foreach (var v in ring) Vertex(v, true);
                for (int i = 1; i < ring.Count - 1; i++) indices[mat].AddRange(new[] { first, first + i + 1, first + i });
            }
            public void Foliage(int mat, Vector3 centre, Vector3 size, int sides)
            {
                int first = vertices.Count;
                for (int r = 0; r <= 3; r++)
                {
                    float lat = Mathf.Lerp(-.48f, 1.4f, r / 3f);
                    for (int i = 0; i < sides; i++)
                    {
                        float a = (i + (r % 2) * .25f) * Mathf.PI * 2f / sides;
                        Vertex(centre + Vector3.Scale(size, new Vector3(Mathf.Cos(a) * Mathf.Cos(lat), Mathf.Sin(lat), Mathf.Sin(a) * Mathf.Cos(lat))), true);
                    }
                }
                for (int r = 0; r < 3; r++) for (int i = 0; i < sides; i++)
                {
                    int a = first + r * sides + i, b = first + r * sides + (i + 1) % sides;
                    indices[mat].AddRange(new[] { a, a + sides, b, b, a + sides, b + sides });
                }
            }
            public Mesh Finish(string name)
            {
                var m = new Mesh { name = name };
                m.SetVertices(vertices); m.SetUVs(0, uv); m.subMeshCount = indices.Length;
                for (int i = 0; i < indices.Length; i++) m.SetTriangles(indices[i], i);
                m.RecalculateNormals(); m.RecalculateBounds(); m.RecalculateTangents(); return m;
            }
        }

        readonly Dictionary<string, (Mesh near, Mesh far)> meshes = new();
        readonly Dictionary<string, Material> palette = new();
        Material Colour(string name, bool architecture)
        {
            string key = (architecture ? "wall/" : "garden/") + name;
            if (palette.TryGetValue(key, out var m)) return m;
            var source = AssetDatabase.LoadAssetAtPath<Material>(Mats + name + ".mat");
            if (!source) throw new BuildFailedException("Missing streetscape surface " + name);
            m = new Material(source) { name = key, enableInstancing = true };
            if (architecture) m.shader = Shader.Find("OldTown/DecoKit/Lit Cutaway") ?? throw new BuildFailedException("Missing architectural cutaway shader");
            palette.Add(key, m);
            return m;
        }

        static Mesh BuildingMesh(DistrictGround.Building b, bool near)
        {
            var s = new Surface(5);
            float w = b.Width, d = b.Depth, y = 3.65f;
            s.Solid(0, 0, 0, w, d, 0, .48f, 1.45f);
            s.Solid(1, 0, 0, w - .30f, d - .30f, .48f, 3.2f, 1.45f);
            s.Solid(0, 0, .5f, w + .72f, d + 1.55f, y, .26f, 1.9f);
            s.Solid(2, 0, .5f, w + .76f, d + 1.60f, y + .26f, .17f, 1.9f);
            s.Solid(0, 0, .22f, w + .10f, d + .68f, y + .43f, .49f, 1.6f);
            if (near)
            {
                for (float x = -w / 2 + 1; x < w / 2; x += 1.65f)
                    s.Solid(3, x, d / 2 - .13f, .08f, .09f, .50f, 3.15f, .02f, 1);
                for (int k = -1; k <= 1; k += 2)
                    s.Solid(0, k * w * .35f, d / 2 + .75f, .31f, .31f, .05f, y, .1f, 2);
                s.Solid(3, 0, d / 2 + .04f, 1.78f, .20f, .13f, 2.65f, .06f);
                s.Solid(1, 0, d / 2 + .17f, 1.49f, .07f, .15f, 2.48f, .04f);
            }
            y += .91f;
            for (int f = 1; f < b.Floors; f++)
            {
                bool last = f == b.Floors - 1;
                float faceW = w - (last ? 1.6f : .4f), faceD = d - (last ? 1.4f : .3f);
                s.Solid(0, 0, 0, faceW + .28f, faceD + .28f, y, .74f, 1.55f);
                s.Solid(1, 0, 0, faceW, faceD, y + .74f, 1.65f, 1.5f);
                s.Solid(0, 0, 0, faceW + .52f, faceD + .52f, y + 2.39f, .54f, 1.7f);
                if (near)
                {
                    for (float x = -faceW / 2 + 1.65f; x <= faceW / 2 - .75f; x += 2.05f)
                        s.Solid(3, x, faceD / 2, .105f, .13f, y + .74f, 1.66f, .03f, 1);
                    s.Solid(2, 0, 0, faceW + .54f, faceD + .54f, y + 2.47f, .08f, 1.7f);
                }
                y += 2.93f;
            }
            s.Solid(0, -w * .14f, -.55f, w * .57f, d * .52f, y, 1.05f, 1.0f);
            s.Solid(2, -w * .14f, -.55f, w * .58f, d * .54f, y + 1.05f, .16f, 1.1f);
            // The planted upper terrace reads in both the shoulder view and the overhead camera.
            for (int k = 0; k < 5; k++)
            {
                float x = w * (-.31f + .15f * k);
                s.Foliage(4, new Vector3(x, y + .22f, d * .32f), new Vector3(.92f, .57f, .48f), near ? 7 : 4);
            }
            return s.Finish("Rounded neighbourhood frontage " + (near ? "near" : "far"));
        }

        static Mesh BedMesh(bool near)
        {
            var s = new Surface(5);
            s.Solid(0,0,0,3.75f,1.75f,0,.34f,.86f);
            s.Solid(1,0,0,3.8f,1.8f,.34f,.11f,.88f);
            s.Solid(4,0,0,3.52f,1.52f,.45f,.04f,.72f);
            int count = near ? 18 : 8;
            for (int i = 0; i < count; i++)
            {
                float a = i * 2.39996f, r = Mathf.Sqrt((i + .5f) / count);
                var p = new Vector3(Mathf.Cos(a) * r * 1.53f, .52f, Mathf.Sin(a) * r * .61f);
                float d = near ? .38f : .56f;
                s.Foliage(2,p,new Vector3(d,.28f,d*.78f),near?7:5);
                if (i % 3 != 0)
                {
                    s.Foliage(3,p + Vector3.up * .24f,new Vector3(.15f,.12f,.15f),5);
                    if (near) s.Foliage(3,p + new Vector3(.16f,.17f,-.09f),new Vector3(.1f,.09f,.1f),4);
                }
            }
            return s.Finish("Oval planted bed " + (near ? "near" : "far"));
        }

        static void Draw(Transform root, string name, Vector3 p, float yaw, (Mesh near, Mesh far) model, Material[] mats, bool building)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false); go.transform.SetPositionAndRotation(p, Quaternion.Euler(0,yaw,0));
            MeshRenderer Detail(Mesh mesh)
            {
                var obj = new GameObject(mesh.name);
                obj.transform.SetParent(go.transform,false);
                obj.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = obj.AddComponent<MeshRenderer>(); r.sharedMaterials = mats;
                if (building) r.renderingLayerMask |= ArchitectureFade.Layer;
                return r;
            }
            var lod = go.AddComponent<LODGroup>();
            var hi = Detail(model.near); var lo = Detail(model.far);
            if (!building) lo.shadowCastingMode = ShadowCastingMode.Off;
            lod.SetLODs(new[] { new LOD(building ? .19f : .059f,new Renderer[]{hi}), new LOD(building ? .005f : .008f,new Renderer[]{lo}) });
            lod.RecalculateBounds();
        }

        public void Frontage(Transform root, Vector3 p, DistrictGround.Building b)
        {
            string key = $"front/{b.Width}/{b.Depth}/{b.Floors}";
            if (!meshes.TryGetValue(key,out var model)) meshes[key] = model = (BuildingMesh(b,true),BuildingMesh(b,false));
            var mats = new[] { "Limestone_Tex", "Window_Teal", b.Palette % 2 == 0 ? "Enamel_Seafoam" : "Enamel_Coral", "Bronze","Leaf_CoreShrub" };
            Draw(root,"Neighbourhood corner",p,b.Yaw,model,mats.Select(m=>Colour(m,true)).ToArray(),true);
        }
        public void PlantedBed(Transform root, Vector3 p, int yaw)
        {
            if (!meshes.TryGetValue("bed",out var model)) meshes["bed"] = model = (BedMesh(true),BedMesh(false));
            var mats = new[] { "Limestone_Tex","Enamel_Seafoam","Leaf_CoreShrub","Flower_Coral","Soil" };
            Draw(root,"DK_OvalGardenBed",p,yaw,model,mats.Select(m=>Colour(m,false)).ToArray(),false);
        }
    }
}
