#if UNITY_EDITOR
using System.Collections.Generic;
using Suborned.OldTown.Sky;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// The sky rail (studies: Concepts/Horizon/street_ab skyrail_sweep, planetrise): a closed loop that runs up the
    /// west promenade 24 m over the palms, swings past the plaza gate and circles the sphere station at 34 m before
    /// heading out over the west city. A swept streamline beam (ivory, teal stripe) on tapered pylons every 30 m,
    /// two three-car streamliners on it (SkyTraffic).
    internal static class DecoKitSkyRail
    {
        const float BeamH = 1.6f, BeamW = .6f, PylonStep = 30f, Speed = 22f, FootClear = 3.5f;
        static readonly Vector2 Sphere = new(-130f, 330f);  // DecoKitStreetSkyline: SphereStation

        internal static Vector3[] Route()
        {
            var r = new List<Vector3>
            {
                new(-13f, 24f, -700f), new(-13f, 24f, -40f), new(-13f, 24f, 150f), new(-13f, 24f, 360f),
                new(-18f, 25f, 395f), new(-40f, 28f, 412f), new(-70f, 32f, 410f),
            };
            foreach (var deg in new[] { 90f, 135f, 180f, 225f })  // around the sphere station, r 88 at 34 m
            {
                float a = deg * Mathf.Deg2Rad;
                r.Add(new Vector3(Sphere.x + 88f * Mathf.Cos(a), 34f, Sphere.y + 88f * Mathf.Sin(a)));
            }
            r.AddRange(new Vector3[] { new(-160f, 30f, 170f), new(-150f, 28f, 0f), new(-110f, 26f, -300f), new(-60f, 24f, -660f) });
            return r.ToArray();
        }

        internal static void Build(DecoKitPlacer p, Transform parent)
        {
            // freeze the lead train on the west promenade in the axis shots (z ~ 120)
            Line(p, parent, "Sky rail", Route(), new Vector3(-13f, 24f, 120f));
            Line(p, parent, "Harbour rail", HarbourRoute(), new Vector3(P.QuayX - 8f, 26f, 150f), Station);
            p.Put("SkyRailStation", Station, 0f);  // skyport.py: rail top 22 m over its ground, track along +Z
        }

        // HARBOUR station on the quay promenade, lift tower toward the steps
        static readonly Vector3 Station = new(P.QuayX - 8f, P.PromenadeY, 110f);
        const float StationRail = 22f, StationHalf = 22f;

        /// The harbour line: north up the quay promenade, a balloon loop round the skyport under its landing discs
        /// (R 48, 24 m: the lowest soffit is at 34 m, the widest disc reaches 36 m out), back south over the water.
        internal static Vector3[] HarbourRoute()
        {
            float x = Station.x, sea = P.Skyport.x - 48f, h = Station.y + StationRail, R = 48f;  // the quay leg runs level
                                                                                                    // through the station
            var c = P.Skyport;
            var r = new List<Vector3> { new(x, h, -300f), new(x, h, 140f), new(x + 20f, h - 1f, 160f) };
            for (float deg = 225f; deg <= 540f; deg += 22.5f)  // clockwise from the south-west round to the south
                r.Add(new Vector3(c.x + R * Mathf.Sin(deg * Mathf.Deg2Rad), 24f, c.z + R * Mathf.Cos(deg * Mathf.Deg2Rad)));
            r.AddRange(new Vector3[] { new(sea, h - 1f, 120f), new(sea, h, -300f), new((x + sea) / 2f, h, -318f) });
            return r.ToArray();
        }

        /// One closed line: the swept beam, pylons down to whatever ground is under it, two three-car trains.
        internal static SkyPath Line(DecoKitPlacer p, Transform parent, string name, Vector3[] route, Vector3 freezeNear,
                                     params Vector3[] stations)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            var path = root.gameObject.AddComponent<SkyPath>();
            path.Set(route, true);
            Beam(path, root, System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name).Replace(" ", ""));  // SkyRail_Beam
            Pylons(p, path, root, stations);
            var trains = new GameObject("Trains").AddComponent<SkyTraffic>();
            trains.transform.SetParent(root, false);
            trains.Configure(path, new[] { p.Prefab("SkyRail_Car_Head"), p.Prefab("SkyRail_Car_Mid") }, 2, 3, 18.4f,
                             Speed, 0f, 0f, path.Nearest(freezeNear) / Speed);
            return path;
        }

        /// Rounded 1.2 x 1.6 m beam swept at 2 m steps; its top is the path (the cars' origin); a proud teal stripe.
        static void Beam(SkyPath path, Transform root, string name)
        {
            var body = Section(.22f);
            var stripe = new[] { new Vector2(BeamW + .02f, -1.05f), new Vector2(BeamW + .02f, -.85f) };
            var mesh = new Mesh { name = $"{name}_Beam", indexFormat = IndexFormat.UInt32 };
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var tBody = new List<int>();
            var tStripe = new List<int>();
            int rings = Mathf.CeilToInt(path.Length / 2f) + 1;
            Sweep(path, rings, body, true, v, n, uv, tBody);
            Sweep(path, rings, stripe, false, v, n, uv, tStripe);
            var mirrored = new[] { new Vector2(-BeamW - .02f, -.85f), new Vector2(-BeamW - .02f, -1.05f) };
            Sweep(path, rings, mirrored, false, v, n, uv, tStripe);
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(tBody, 0);
            mesh.SetTriangles(tStripe, 1);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            EnsureFolder(GeneratedDir);
            var asset = $"{GeneratedDir}/{mesh.name}.asset";
            AssetDatabase.DeleteAsset(asset);
            AssetDatabase.CreateAsset(mesh, asset);
            var go = new GameObject("Beam", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(root, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterials = new[] { Mat("DK_Enamel_Ivory"), Mat("DK_Enamel_Teal") };
            go.isStatic = true;
        }

        static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MaterialDir}/{name}.mat");

        /// Closed CCW outline of the beam (x = side, y = up; top at 0) with filleted corners, 4 points per corner.
        static Vector2[] Section(float r)
        {
            var corners = new[] { (-BeamW, -BeamH, 180f), (BeamW, -BeamH, 270f), (BeamW, 0f, 0f), (-BeamW, 0f, 90f) };
            var o = new List<Vector2>();
            foreach (var (x, y, a0) in corners)
            {
                var centre = new Vector2(x - Mathf.Sign(x) * r, y + (y < 0f ? r : -r));
                for (int k = 0; k < 4; k++)
                {
                    float a = (a0 + 30f * k) * Mathf.Deg2Rad;
                    o.Add(centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                }
            }
            return o.ToArray();
        }

        static void Sweep(SkyPath path, int rings, Vector2[] prof, bool closed, List<Vector3> v, List<Vector3> n,
                          List<Vector2> uv, List<int> tris)
        {
            int m = prof.Length, baseIndex = v.Count, segs = closed ? m : m - 1;
            float step = path.Length / (rings - 1);
            for (int r = 0; r < rings; r++)
            {
                float s = r * step;
                Vector3 p = path.Point(s), t = path.Tangent(s);
                Vector3 side = Vector3.Cross(Vector3.up, t).normalized, up = Vector3.up;
                for (int k = 0; k < m; k++)
                {
                    Vector2 a = prof[(k + m - 1) % m], b = prof[(k + 1) % m];
                    Vector2 d = closed ? b - a : prof[m - 1] - prof[0];
                    var nn = new Vector2(d.y, -d.x).normalized;  // CCW outline: outward = right of travel
                    v.Add(p + side * prof[k].x + up * prof[k].y);
                    n.Add((side * nn.x + up * nn.y).normalized);
                    uv.Add(new Vector2(s / 4f, k / (float)m));
                }
            }
            for (int r = 0; r < rings - 1; r++)
                for (int k = 0; k < segs; k++)
                {
                    int a = baseIndex + r * m + k, b = baseIndex + r * m + (k + 1) % m;
                    tris.AddRange(new[] { a, b, a + m, b, b + m, a + m });  // front = outward (Unity: clockwise)
                }
        }

        /// Foot on the walk, a stretched shaft, the cradle under the beam; none in the road, the circus's ring road
        /// (the island takes them) or the plaza floor.
        /// The tall base (skyport.py: 12 m, the stock shaft's section at its top) when built, else the stock foot.
        static void Pylons(DecoKitPlacer p, SkyPath path, Transform root, Vector3[] stations)
        {
            bool tall = p.Has("SkyRail_Pylon_Tall");
            for (float s0 = 0f; s0 < path.Length; s0 += PylonStep)
            {
                if (!Clear(path, s0, out float s)) continue;
                Vector3 c = path.Point(s);
                if (Mathf.Abs(c.x) < L.KerbX + 1f) continue;
                if (P.InCircus(c.x, c.z, P.RingR + 1f) && !P.InCircus(c.x, c.z, P.IslandR - 1.5f)) continue;
                if (System.Array.Exists(stations, st => new Vector2(c.x - st.x, c.z - st.z).magnitude < StationHalf)) continue;
                float yaw = Quaternion.LookRotation(path.Tangent(s)).eulerAngles.y;
                float ground = c.x > P.QuayX ? P.WaterY : c.x > P.StepsX ? P.PromenadeY : L.Walk;
                float headBottom = c.y - BeamH - 2.2f, footTop = ground + (tall ? 12f : 1.2f);
                Parent(p.Put(tall ? "SkyRail_Pylon_Tall" : "SkyRail_Pylon_Foot", new Vector3(c.x, ground, c.z), yaw), root);
                Parent(p.Put("SkyRail_Pylon_Head", new Vector3(c.x, c.y - BeamH, c.z), yaw), root);
                var shaft = p.Put("SkyRail_Pylon_Shaft", new Vector3(c.x, footTop, c.z), yaw);
                if (shaft == null) continue;
                shaft.transform.localScale = new Vector3(1f, headBottom - footTop, 1f);
                Parent(shaft, root);
            }
        }

        /// Nudge a pylon along the line (up to half a step either way, nearest first) until its foot (the tall one's
        /// 4.4 x 5.2 m plinth) is off Southside's blocks and pavements; false where a block is wider than that, and the
        /// beam spans it.
        static bool Clear(SkyPath path, float s0, out float s)
        {
            for (float d = 0f; d <= PylonStep * .5f; d += 1f)
                foreach (var t in new[] { s0 + d, s0 - d })
                {
                    s = Mathf.Repeat(t, path.Length);
                    var c = path.Point(s);
                    if (!System.Array.Exists(DecoKitSouthside.Blocks, b => b.Paved(c, FootClear))) return true;
                }
            s = s0;
            return false;
        }

        static void Parent(GameObject go, Transform root) { if (go) go.transform.SetParent(root, true); }
    }
}
#endif
