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
    sealed partial class DistrictStreetArchitecture
    {
        const string Mats = "Assets/Suborned/OldTownArt/DecoKit/Materials/DK_";
        sealed partial class Surface
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
            public void Picture(int mat, float x, float y, float z, float width, float height)
            {
                int first = vertices.Count;
                float l = x - width/2, r = x + width/2;
                vertices.AddRange(new[] { new Vector3(l,y,z),new Vector3(r,y,z),new Vector3(r,y+height,z),new Vector3(l,y+height,z) });
                uv.AddRange(new[] { Vector2.zero,Vector2.right,Vector2.one,Vector2.up });
                indices[mat].AddRange(new[] { first, first+1, first+2, first, first+2, first+3 });
            }
            // The original kit's photographed fronds and blossom clusters carry their own silhouettes.
            // Two fixed, angled cards overlap in the close mesh. The distant mesh keeps one per clump.
            public void Sprig(int mat, Vector3 foot, Vector3 side, Vector3 lift)
            {
                int i = vertices.Count;
                vertices.AddRange(new[] { foot-side*.5f,foot+side*.5f,foot+side*.5f+lift,foot-side*.5f+lift });
                uv.AddRange(new[] { Vector2.zero,Vector2.right,Vector2.one,Vector2.up });
                indices[mat].AddRange(new[] { i,i+1,i+2,i,i+2,i+3 });
            }
            public void TerracePlants(int frond, int shrub, int bloom, Vector3 root, float span, int seed, bool near)
            {
                int clumps = near ? 6 : 4;
                for (int c=0;c<clumps;c++)
                {
                    float phase=seed*1.13f+c*2.40f;
                    var at=root+new Vector3(((c+.5f)/clumps-.5f)*span,-.10f,.14f*Mathf.Sin(phase));
                    float size=.64f+.23f*Mathf.Cos(phase);
                    Sprig(c%3==1 ? bloom : shrub,at,new Vector3(size,.02f,.17f*Mathf.Sin(phase)),new Vector3(0,size*.88f,.08f));
                    if (near) Sprig(shrub,at+Vector3.forward*.08f,new Vector3(.34f,0,.44f),new Vector3(.09f,size*.80f,.08f));
                    if ((c+seed)%2==0)
                    {
                        float lean=c%2==0 ? 1f : -1f;
                        Sprig(frond,at+Vector3.up*.14f,new Vector3(lean*.70f,.10f,.14f),new Vector3(lean*.17f,size*1.17f,.14f));
                    }
                    // Draping growth extends in front of the lip and hangs below it.
                    if (near || c%2==0)
                        Sprig(shrub,at+new Vector3(.07f,-size*.53f,.36f),new Vector3(.39f,.04f,.03f),new Vector3(.05f,size*.83f,-.08f));
                }
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
            if (name == "Leaf_CoreShrub")
            {
                // Broad waxy leaves on a sunlit balcony, also legible from their shaded underside.
                m.SetColor("_BaseColor", new Color(.34f, .50f, .22f));
                m.SetFloat("_Smoothness", .37f);
            }
            if (name == "Leaf_PalmFeather" || name == "Leaf_Shrub" || name == "Flower_Coral")
            {
                m.SetFloat("_Cull", 0f);
                m.SetFloat("_AlphaClip", 1f);
                m.EnableKeyword("_ALPHATEST_ON");
            }
            palette.Add(key, m);
            return m;
        }

        static Mesh BuildingMesh(DistrictGround.Building b, bool near)
        {
            var s = new Surface(8);
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
            s.TerracePlants(5,6,7,new Vector3(0,y+.28f,d*.32f),w*.65f,b.Floors,near);
            return s.Finish("Rounded neighbourhood frontage " + (near ? "near" : "far"));
        }

        static Mesh BedMesh(bool near)
        {
            var s = new Surface(5);
            s.Solid(0,0,0,3.75f,1.75f,0,.34f,.86f);
            s.Solid(1,0,0,3.8f,1.8f,.34f,.11f,.88f);
            s.Solid(4,0,0,3.52f,1.52f,.45f,.04f,.72f);
            int count = near ? 15 : 7;
            for (int i = 0; i < count; i++)
            {
                float a = i * 2.39996f, r = Mathf.Sqrt((i + .5f) / count);
                var p = new Vector3(Mathf.Cos(a) * r * 1.52f, .48f, Mathf.Sin(a) * r * .61f);
                var across = new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)) * (near ? .93f : 1.13f);
                var up = new Vector3(.06f,.51f+.1f*Mathf.Sin(a),.03f);
                s.Sprig(i%3==1 ? 3 : 2,p,across,up);
                if (near) s.Sprig(i%3==1 ? 3 : 2,p,Vector3.Cross(across,Vector3.up),up);
            }
            return s.Finish("Oval planted bed " + (near ? "near" : "far"));
        }

        static Mesh GalleryMesh(DistrictGround.Gallery bay, bool near)
        {
            // Local +Z is the pavement. The lower windows stand proud of the retained
            // podium, with a continuous transom and a curved canopy above the footway.
            float w = bay.WidthCm * .01f;
            var s = new Surface(12);
            s.Solid(0,0,.12f,w,.40f,0,.48f,.16f);
            s.Solid(1,0,.18f,w-.20f,.36f,.48f,3.02f,.12f);
            s.Solid(3,0,.25f,w,.47f,2.73f,.09f,.16f);
            s.Solid(0,0,.60f,w+.27f,1.63f,3.56f,.23f,.61f);
            s.Solid(2,0,.62f,w+.34f,1.72f,3.79f,.18f,.67f);
            s.Solid(0,0,.20f,w,.50f,3.97f,.28f,.19f);
            s.Solid(1,0,.12f,w-.46f,.28f,4.32f,1.32f,.11f);
            s.Solid(3,0,.18f,w-.28f,.39f,5.64f,.09f,.13f);
            s.Solid(0,0,.15f,w-.17f,.40f,5.73f,.32f,.14f);
            // Occupied terraces in the store studies have layered shade, broad leaves
            // and open lengths of rail. Keep gaps between planted balconies.
            if (bay.Variant != 2)
            {
                float length=w*(bay.Variant==0?.57f:.37f);
                float x=w*(bay.Variant==1?.24f:-.15f);
                s.Solid(0,x,.88f,length,1.03f,3.97f,.34f,.40f);
                s.Solid(3,x,.89f,length+.08f,1.08f,4.30f,.07f,.40f);
                s.TerracePlants(10,11,6,new Vector3(x,4.36f,1.15f),length*.91f,bay.Variant,near);
            }
            for (int k = 0; k <= 4; k++)
            {
                float x = (k * .25f - .5f) * (w - .28f);
                s.Solid(3,x,.37f,.075f,.10f,.44f,3.13f,.03f,1);
                s.Solid(0,x,.27f,.12f,.13f,4.28f,1.38f,.04f,1);
            }
            if (near)
            {
                // Bronze shop doors, a lit reveal beneath the eave and a display with real depth.
                float door = w * (bay.Variant % 2 == 0 ? -.27f : .27f);
                s.Solid(3,door,.40f,1.64f,.19f,.04f,2.68f,.07f);
                s.Solid(1,door,.51f,1.44f,.07f,.10f,2.50f,.03f);
                s.Solid(3,door+.46f,.58f,.065f,.10f,1.0f,.48f,.026f);
                s.Solid(4,0,1.21f,w-.87f,.075f,3.535f,.045f,.03f,1);
                if (bay.Variant == 0 || bay.Variant == 3)
                {
                    float x = -door;
                    s.Solid(0,x,.43f,2.05f,.73f,.04f,.39f,.30f);
                    s.Solid(2,x,.43f,2.12f,.78f,.43f,.10f,.32f);
                    for (int f = -2; f <= 2; f++)
                    {
                        var v = new Vector3(x+f*.34f,.65f,.50f);
                        s.Foliage(5,v,new Vector3(.34f,.33f,.24f),6);
                        if (f%2==0) s.Foliage(6,v+Vector3.up*.22f,new Vector3(.13f,.14f,.12f),5);
                    }
                }
                else
                {
                    // A shallow mounted poster box faces the pavement between two window frames.
                    float x = -door;
                    s.Solid(3,x,.42f,1.29f,.20f,.81f,1.67f,.11f);
                    float z=.532f;
                    s.Picture(7,x,.90f,z,1.10f,1.46f);
                }
                if (bay.Variant == 1)
                {
                    s.Solid(2,w*.40f,.45f,.22f,.91f,4.34f,2.38f,.09f);
                    s.Solid(4,w*.40f,.90f,.12f,.08f,4.56f,1.94f,.035f);
                }
            }
            UpperLoggia(s, w, bay.Variant, near);
            return s.Finish("Street gallery " + (near ? "near" : "far"));
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
            var mats = new[] { "Limestone_Tex", "Window_Teal", b.Palette % 2 == 0 ? "Enamel_Seafoam" : "Enamel_Coral", "Bronze","Leaf_CoreShrub","Leaf_PalmFeather","Leaf_Shrub","Flower_Coral" };
            Draw(root,"Neighbourhood corner",p,b.Yaw,model,mats.Select(m=>m == "Window_Teal" ? ShopGlass(b.Palette % 4) : Colour(m,true)).ToArray(),true);
        }
        public void Gallery(Transform root, Vector3 p, DistrictGround.Gallery bay)
        {
            string key = $"gallery/{bay.WidthCm}/{bay.Variant}";
            if (!meshes.TryGetValue(key, out var model)) meshes[key] = model = (GalleryMesh(bay,true), GalleryMesh(bay,false));
            var mats = new[] { "Limestone_Tex", "Window_Teal", bay.Variant==1 ? "Enamel_Coral" : "Enamel_Seafoam",
                "Bronze", "Glow_Amber", "Leaf_CoreShrub", "Flower_Coral", "Lightbox_Travel", "Fabric_Ivory", bay.Variant % 2 == 0 ? "Enamel_Seafoam" : "Fabric_Coral", "Leaf_PalmFeather", "Leaf_Shrub" };
            p.y=.18f;
            Draw(root,"Housing gallery",p,bay.Yaw,model,mats.Select(m=>m == "Window_Teal" ? ShopGlass(bay.Variant) : Colour(m,true)).ToArray(),true);
        }
        public void PlantedBed(Transform root, Vector3 p, int yaw)
        {
            if (!meshes.TryGetValue("bed",out var model)) meshes["bed"] = model = (BedMesh(true),BedMesh(false));
            var mats = new[] { "Limestone_Tex","Enamel_Seafoam","Leaf_Shrub","Flower_Coral","Soil" };
            Draw(root,"DK_OvalGardenBed",p,yaw,model,mats.Select(m=>Colour(m,false)).ToArray(),false);
        }
    }
}
