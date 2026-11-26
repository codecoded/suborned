using System.Collections.Generic;
using System.Linq;
using Suborned.Play;
using Suborned.Sim;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

using UnityEngine.SceneManagement;

namespace Suborned.World.Play
{
    // Generated into the processed scene, from the exact same quay strips and fixture seats as the simulation.
    static class DistrictPublicRealm
    {
        const string Name = "Harbour public realm";
        const string Kit = "Assets/Suborned/Kit/Prefabs/";
        const string Deco = "Assets/Suborned/OldTownArt/DecoKit/";

        sealed class Quads
        {
            readonly List<Vector3> points = new();
            readonly List<Vector2> uv = new();
            readonly List<int> triangles = new();

            public void Add(Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool top)
            {
                int first = points.Count;
                foreach (var v in new[] { a, b, c, d })
                {
                    points.Add(v);
                    uv.Add(top ? new Vector2(v.x, v.z) : new Vector2(v.x + v.z, v.y));
                }
                triangles.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
            }

            public void Fan(Vector3[] loop)
            {
                int first = points.Count;
                foreach (var p in loop) { points.Add(p); uv.Add(new Vector2(p.x, p.z)); }
                for (int i = 1; i < loop.Length - 1; i++) triangles.AddRange(new[] { first, first + i, first + i + 1 });
            }

            public void Emit(Transform root, string name, Material mat)
            {
                if (points.Count == 0) return;
                var mesh = new Mesh { name = name };
                mesh.SetVertices(points); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
                mesh.RecalculateBounds(); mesh.RecalculateNormals(); mesh.RecalculateTangents();
                var obj = new GameObject(name);
                obj.transform.SetParent(root, false);
                obj.AddComponent<MeshFilter>().sharedMesh = mesh;
                obj.AddComponent<MeshRenderer>().sharedMaterial = mat;
            }
        }

        public static void Install(HandoverHost host, Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            if (roots.Any(o => o.name == Name)) return;
            var root = new GameObject(Name);
            SceneManager.MoveGameObjectToScene(root, scene);
            var ground = DistrictMap.Harbour.Ground;
            var frame = host.frame;
            foreach (var go in roots)
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                if (t.parent && t.parent.name == "Harbour" &&
                    (t.name == "Harbour terrace" || t.name == "Harbour steps" ||
                     t.name == "Quay promenade" || t.name == "Quay wall"))
                    t.gameObject.SetActive(false);

            var paving = MaterialAt(Deco + "Generated/DK_Pave_Flags_Metric.mat");
            var edge = MaterialAt(Deco + "Materials/DK_Granite_Kerb.mat");
            var limestone = MaterialAt(Deco + "Materials/DK_Limestone_Tex.mat");
            var strips = ground.Waterfront;
            // Split faces at every pier corner so the granite edge opens where the apron joins the quay.
            var xs = new SortedSet<int>(strips.SelectMany(s => new[] { s.A.Y, s.B.Y }));
            var ys = new SortedSet<int>(strips.SelectMany(s => new[] { s.A.X, s.B.X }));
            for (int x = ys.Min + 5400, last = ys.Max; x < last; x += 5400) ys.Add(x);
            var east = xs.ToArray(); var south = ys.ToArray();
            bool Contains(Pt p) => strips.Any(s => s.Contains(p));
            Vector3 At(int x, int y, int cm)
            {
                var plan = frame.ToPlan(x * .01f, y * .01f);
                return new Vector3(plan.x, cm * .01f, plan.y);
            }
            for (int j = 1; j < south.Length; j++)
            {
                var top = new Quads(); var wall = new Quads(); var coping = new Quads();
                int north = south[j - 1], bottom = south[j], cx = (north + bottom) / 2;
                for (int k = 1; k < east.Length; k++)
                {
                    int west = east[k - 1], right = east[k], cy = (west + right) / 2;
                    var centre = new Pt(cx, cy);
                    int index = System.Array.FindIndex(strips, s => s.Contains(centre));
                    if (index < 0) continue;
                    var strip = strips[index];
                    var wh = strip.Elevation(new Pt(cx, west)); var eh = strip.Elevation(new Pt(cx, right));
                    var sw = At(bottom, west, wh); var nw = At(north, west, wh);
                    var ne = At(north, right, eh); var se = At(bottom, right, eh);
                    top.Add(sw, nw, ne, se, true);
                    void Rim(Vector3 a, Vector3 b, Pt outside, bool sea)
                    {
                        if (Contains(outside)) return;
                        var ab = b - a;
                        var inward = new Vector3(ab.z, 0, -ab.x).normalized * .35f;
                        Vector3 lowA = a, lowB = b; lowA.y = lowB.y = -2.6f;
                        wall.Add(b, a, lowA, lowB, false);
                        if (sea)
                        {
                            a.y += .025f; b.y += .025f;
                            coping.Add(a + inward, a, b, b + inward, true);
                        }
                    }
                    Rim(sw, nw, new Pt(cx, west - 1), false);
                    Rim(nw, ne, new Pt(north - 1, cy), strip.Id != "quay/0");
                    Rim(ne, se, new Pt(cx, right + 1), true);
                    Rim(se, sw, new Pt(bottom + 1, cy), strip.Id != "quay/0");
                }
                top.Emit(root.transform, "Quayside paving " + j, paving);
                wall.Emit(root.transform, "Limestone pier faces " + j, limestone);
                coping.Emit(root.transform, "Granite pier edges " + j, edge);
            }

            // The flyby's coastline continues past both playable boundaries.
            var promenade = strips.First(s => s.Id == "quay/0" && s.WestCm == -85 && s.EastCm == -85);
            foreach (bool northern in new[] { true, false })
            {
                int near = northern ? south[0] : south[south.Length - 1];
                int far = near + (northern ? -155000 : 155000);
                int a = System.Math.Max(near, far), b = System.Math.Min(near, far);
                var surface = new Quads();
                surface.Add(At(a, promenade.A.Y, -85), At(b, promenade.A.Y, -85),
                    At(b, promenade.B.Y, -85), At(a, promenade.B.Y, -85), true);
                surface.Emit(root.transform, "Distant promenade " + near, paving);
            }

            var ivory = MaterialAt(Deco + "Materials/DK_Terrazzo_Ivory.mat");
            var inlay = MaterialAt(Deco + "Materials/DK_Terrazzo_Seafoam.mat");
            foreach (var court in ground.Courts)
            {
                var outer = court.Ring.Select(p =>
                {
                    var at = frame.World(p);
                    at.y = (ground.TransportElevationCm(p) ?? 18) * .01f + .013f;
                    return at;
                }).ToArray();
                if (Vector3.Cross(outer[1] - outer[0], outer[2] - outer[0]).y < 0) System.Array.Reverse(outer);
                var centre = outer.Aggregate(Vector3.zero, (sum, p) => sum + p) / outer.Length;
                var inner = outer.Select(p => Vector3.Lerp(p, centre, .045f)).ToArray();
                var band = new Quads(); var slab = new Quads();
                for (int i = 0; i < outer.Length; i++)
                {
                    int n = (i + 1) % outer.Length;
                    band.Add(outer[i], outer[n], inner[n], inner[i], true);
                }
                slab.Fan(inner);
                slab.Emit(root.transform, court.Name + " walking terrace", court.Style == "homes" ? paving : ivory);
                band.Emit(root.transform, court.Name + " inlay", inlay);
            }
            var templates = new DistrictStreetArchitecture();
            var prefabs = new Dictionary<string, GameObject>();
            foreach (var building in ground.Buildings) templates.Frontage(root.transform, frame.World(building.At), building);
            void PlaceFixture(DistrictGround.Fixture fixture, string name)
            {
                var pos = frame.World(fixture.At);
                pos.y = ground.ElevationCm(fixture.At) * .01f;
                if (fixture.Kit == "GardenBed") { templates.PlantedBed(root.transform, pos, fixture.Yaw); return; }
                if (!prefabs.TryGetValue(fixture.Kit, out var prefab))
                {
                    // Built prefabs bind the imported materials and all available authored mesh LODs.
                    foreach (var dir in new[] { "Built", "Generated", "Furnish", "Street", "Plants", "Structures" })
                    {
                        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + dir + "/DK_" + fixture.Kit + ".prefab");
                        if (prefab) break;
                    }
                    if (!prefab) throw new BuildFailedException("Missing district fixture " + fixture.Kit);
                    prefabs.Add(fixture.Kit, prefab);
                }
                var prop = Object.Instantiate(prefab, root.transform);
                prop.name = "DK_" + fixture.Kit + " • " + name;
                prop.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, fixture.Yaw, 0));
                if (prop.GetComponentInChildren<LODGroup>()) return;
                var renderers = prop.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) return;
                var lod = prop.AddComponent<LODGroup>();
                lod.SetLODs(new[] { new LOD(.013f, renderers) });
                lod.RecalculateBounds();
            }
            foreach (var venue in ground.Venues)
                foreach (var fixture in venue.Fixtures) PlaceFixture(fixture, venue.Name);
            foreach (var fixture in ground.StreetFixtures) PlaceFixture(fixture, "street");
        }

        static Material MaterialAt(string path) => AssetDatabase.LoadAssetAtPath<Material>(path)
            ?? throw new BuildFailedException("Missing waterfront material " + path);
    }
}
