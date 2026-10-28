#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Suborned.OldTown.Editor.DecoKit;
using Suborned.OldTown.Sky;
using Suborned.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 075 remap step 2, the full swap (Latch). One entry point that builds the whole district with the old DecoKit builders
    /// (verbatim copy in Editor/OldTown/DecoKit, run in the order of DecoKitStreetDemo.Build), cuts the Handover zone out of it and puts ours
    /// there exactly as our pipeline built it (a scene HarbourWorldBuilder built from a pinned export), puts our network on top (the S1/S2 sky
    /// track on our route through the old beam builder, the old pylon pieces at our pylon points, our stations with their stairs, lifts and
    /// mezzanines, the Skyport station, the tram on our routes in the old rail tile with the old tram), and where an old structure clashes
    /// the old one gives way (each removal logged). Not part of the verbatim copy.
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.OldTown.Editor.Swap.OldTownSwap.Build
    ///     -swapHeights rule|old (default rule) -swapHq old|ours (default old) -swapScene OldTown
    ///     -swapOurs Assets/Scenes/probe-oldtown-ours.unity -swapWorld out/scratch/latch/075swap/head.world.json -swapOut briefs/075/oldbuild/swap
    ///   ... .OldTownSwap.Shoot  -swapScene OldTown -swapShots circus,golden,vista,play,district -swapTag rule   (offscreen renders)
    ///   ... .OldTownSwap.Flyby  -swapScene OldTown (the old reel's route, ReviewVideo encode)</summary>
    public static class OldTownSwap
    {
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        /// <summary>Plan (x, y) = old Unity (x, z - 206).</summary>
        internal const float DZ = 206f;
        /// <summary>The Handover zone, plan standards.blocks.zone [x0, x1, y0, y1] = [-226, -84, -372, -148].</summary>
        internal static readonly Rect ZonePlan = Rect.MinMaxRect(-226f, -372f, -84f, -148f);
        internal static Rect ZoneOld => new(ZonePlan.x, ZonePlan.y + DZ, ZonePlan.width, ZonePlan.height);
        internal static Vector2 Old(Vector2 plan) => new(plan.x, plan.y + DZ);
        internal static Vector3 Old3(Vector2 plan, float y) => new(plan.x, y, plan.y + DZ);

        static string Repo => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        static string Abs(string p) => Path.IsPathRooted(p) ? p : Path.GetFullPath(Path.Combine(Repo, p));
        static string Arg(string k, string d) => DecoKitStage.Arg(k, d);
        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        public static void Build() => Batch(() => WithOldSettings(BuildBody));
        public static void Shoot() => Batch(() => WithOldSettings(SwapShots.Run));
        public static void Flyby() => Batch(() => WithOldSettings(SwapShots.Flyby));
        public static void Verify() => Batch(SwapVerify.Run);

        static void Batch(Action body)
        {
            try { body(); }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); else throw; }
        }

        /// <summary>The proof's render-settings swap (OldTownProof.WithOldSettings: the old PC_RPAsset/PC_Renderer and quality level, in memory).</summary>
        /// The proof's restore reads the quality level's pipeline with GetRenderPipelineAssetAt, which can read null in batch mode (its own note):
        /// then it restores null and Unity saves `customRenderPipeline: {fileID: 0}` into ProjectSettings/QualitySettings.asset. So the level's
        /// pipeline is read here from the settings file itself before the swap and put back after, and both settings files are checked byte for byte.
        static void WithOldSettings(Action body)
        {
            var m = typeof(Proof.OldTownProof).GetMethod("WithOldSettings", Any) ?? throw new MissingMethodException("OldTownProof.WithOldSettings");
            string ps = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ProjectSettings"));
            var files = new[] { "QualitySettings.asset", "GraphicsSettings.asset" }.ToDictionary(f => f, f => File.ReadAllBytes(Path.Combine(ps, f)));
            int level = QualitySettings.GetQualityLevel();
            var quality = QualitySettings.GetRenderPipelineAssetAt(level) ?? LevelPipeline(Encoding.UTF8.GetString(files["QualitySettings.asset"]), level);
            try { m.Invoke(null, new object[] { body }); }
            finally
            {
                if (quality != null) QualitySettings.renderPipeline = quality;
                AssetDatabase.SaveAssets();
                foreach (var (f, b) in files)
                    if (!File.ReadAllBytes(Path.Combine(ps, f)).SequenceEqual(b)) Debug.LogWarning($"[Swap] ProjectSettings/{f} differs from before the run after the restore (Unity rewrites it on exit from the restored values)");
            }
        }

        static UnityEngine.Rendering.RenderPipelineAsset LevelPipeline(string quality, int level)
        {
            var ms = System.Text.RegularExpressions.Regex.Matches(quality, @"customRenderPipeline: \{fileID: \d+(?:, guid: ([0-9a-f]{32}))?");
            if (level >= ms.Count || !ms[level].Groups[1].Success) return null;
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(ms[level].Groups[1].Value));
        }

        // ---------------------------------------------------------------- heights
        /// <summary>Sky 2026-10-29: "Tall flagship buildings should remain, only majority of domestic buildings are reduced". The domestic classes
        /// go to their low twin (same bay rhythm, same caps); D10, the crescents (DecoKitFrontage.Raw), CornerTower_12 (Cap.Tower) and every
        /// landmark keep the old height. S4/P4/B4 podiums keep their StreamCorner_4 cells.</summary>
        internal static readonly Dictionary<string, string> RuleMap = new()
        { ["S6"] = "S3", ["S9"] = "S3", ["S9W"] = "S3W", ["P7"] = "P3", ["B8"] = "B3", ["S4"] = "S3", ["P4"] = "P3", ["B4"] = "B3" };

        static DecoKitFrontage.Piece Rule(DecoKitPlacer p, string cls, DecoKitFrontage.Piece old)
        {
            if (!RuleMap.TryGetValue(cls, out var low)) return old;
            var l = DecoKitFrontage.Raw(p, low);
            return new DecoKitFrontage.Piece(l.Bay, l.End, cls is "S4" or "P4" or "B4" ? old.Corner : l.Corner, l.Height);
        }

        // ---------------------------------------------------------------- the build
        internal sealed class Report
        {
            public readonly List<string> Lines = new();
            public readonly Dictionary<string, Dictionary<string, int>> GaveWay = new();  // reason -> piece -> count
            public readonly List<object> GaveWayList = new();
            public void Log(string s) { Lines.Add(s); Debug.Log("[Swap] " + s); }
            public void Gave(string reason, string piece, Vector3 at, string by)
            {
                if (!GaveWay.TryGetValue(reason, out var d)) GaveWay[reason] = d = new Dictionary<string, int>();
                d[piece] = d.TryGetValue(piece, out var n) ? n + 1 : 1;
                GaveWayList.Add(new Dictionary<string, object> { ["reason"] = reason, ["piece"] = piece, ["at"] = new[] { R(at.x), R(at.z - DZ) }, ["by"] = by });
            }
            static float R(float v) => Mathf.Round(v * 100f) / 100f;
        }

        static void BuildBody()
        {
            var t0 = DateTime.UtcNow;
            string heights = Arg("swapHeights", "rule"), hq = Arg("swapHq", "old");
            string scene = Arg("swapScene", heights == "rule" ? "OldTown" : "OldTown-heights-old");
            string oursScene = Arg("swapOurs", "Assets/Scenes/probe-oldtown-ours.unity");
            string worldPath = Abs(Arg("swapWorld", "out/scratch/latch/075swap/head.world.json"));
            string outDir = Abs(Arg("swapOut", "briefs/075/oldbuild/swap"));
            if (heights != "rule" && heights != "old") throw new ArgumentException("-swapHeights rule|old");
            if (hq != "old" && hq != "ours") throw new ArgumentException("-swapHq old|ours");
            Directory.CreateDirectory(outDir);
            var rep = new Report();
            var world = WorldReader.Load(worldPath);
            rep.Log($"export {Path.GetRelativePath(Repo, worldPath)} sha256 {world.Sha256} ({world.Source}); heights {heights}; SpaceHQ {hq}; scene Assets/Scenes/{scene}.unity; ours from {oursScene}");

            DecoKitFrontage.Lower = heights == "rule" ? Rule : null;
            try
            {
                // ---- 1. the old builders, in DecoKitStreetDemo.Build's order (its private tunables read by reflection, so they stay the old ones)
                var demo = typeof(DecoKitStreetDemo);
                T Get<T>(string name) => (T)(demo.GetProperty(name, Any)?.GetValue(null) ?? demo.GetField(name, Any)?.GetValue(null) ?? throw new MissingMemberException(demo.Name, name));
                float exposure = Get<float>("Exposure"), sunI = Get<float>("SunIntensity"), fill = Get<float>("Fill"), skyGain = Get<float>("SkyGain");
                var sunTravel = Get<Vector3>("SunTravel");
                var ambient = Get<DecoKitAtmosphere.Ambient>("Ambient");
                DecoKitStage.NewScene(scene);
                DecoKitSky.Apply("Street", -sunTravel, ambient, skyGain, fill, DecoKitStage.Arg("dkFogDensity", .0005f));
                var sun = DecoKitStage.Directional("Sun", sunTravel, new Color(1f, .93f, .8f), sunI, true);
                var grade = DecoKitStage.Grade("DK_StreetGrade", UnityEngine.Rendering.Universal.TonemappingMode.ACES, exposure, 10f, DecoKitStage.Arg("dkSaturation", 25f));
                DecoKitLens.SoftFocus(grade, DecoKitStage.Arg("dkSoftStart", 300f), DecoKitStage.Arg("dkSoftEnd", 1800f), DecoKitStage.Arg("dkSoftRadius", 1.2f));
                DecoKitLens.SunFlare(sun, DecoKitStage.Arg("dkFlare", 1f));
                var street = new GameObject("Deco Street").transform;
                var placer = new DecoKitPlacer(street);
                L.Build(placer);
                DecoKitCircus.Build(placer);
                DecoKitHarbour.Build(placer);
                DecoKitGardens.Build(placer);
                DecoKitSkyTraffic.Build(placer);
                DecoKitStreetLife.Build(placer);
                DecoKitDistrictLife.Build(street);
                DecoKitSouthside.Build();
                var s1 = world.Transit.SkyTrack.Beams.Where(b => b.Id.StartsWith("skyrail/S1/")).OrderBy(b => b.Id).FirstOrDefault();
                var massRoute = s1 != null ? Thin(s1.Line, 20f).Select(v => Old3(v, s1.Top)).ToArray() : DecoKitSkyRail.Route();
                DecoKitCityMass.Build(massRoute);  // the mid-ground city keeps clear of our S1, as the old kept clear of its own loop
                rep.Log($"city mass kept clear of {(s1 != null ? s1.Id : "the old loop")} ({massRoute.Length} points)");
                DecoKitColourways.Apply(DecoKitStage.Arg("dkColour", DecoKitStreetDemo.Colour));
                demo.GetMethod("Ground", Any).Invoke(null, null);
                DecoKitRoadMarkings.Build(street);
                if (placer.Missing.Count > 0) throw new InvalidOperationException($"[Swap] old prefabs missing: {string.Join(", ", placer.Missing.OrderBy(n => n))} (NP-1: no placeholders)");
                HeightCensus(rep, heights);

                // ---- 2. the old sky rail goes: our network replaces it (Sky 2026-10-28: keep our working network, give it the old route character and look)
                foreach (var n in new[] { "Sky rail", "Harbour rail" })
                    foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.name == n).ToArray())
                    { rep.Gave("old sky rail (our S1/S2 replace it)", n, t.position, "network"); UnityEngine.Object.DestroyImmediate(t.gameObject); }
                foreach (var st in Instances("SkyRailStation").ToArray()) { rep.Gave("old sky rail (our S1/S2 replace it)", "SkyRailStation", st.transform.position, "network"); UnityEngine.Object.DestroyImmediate(st); }

                // ---- 3. keep/drop: old crossings (zebras, traffic lights) give way to the floating eye (GDD §15, keep/drop row "Crossings")
                foreach (var tl in Instances("TrafficLight").ToArray()) { rep.Gave("keep/drop: crossings (floating eye, no present-day lights)", "TrafficLight", tl.transform.position, "keepdrop"); UnityEngine.Object.DestroyImmediate(tl); }
                Zebras(rep);

                // ---- 4. heights rule: the low alley walls behind lowered runs (keep/drop row "Low alley walls")
                var kit = new OurKit();
                if (heights == "rule") AlleySwap(rep, kit);

                // ---- 5. ours: the Handover zone and the network from our pipeline's scene
                var ours = SwapOurs.Import(rep, world, oursScene, hq);

                // ---- 6. the network in the old look on our routes
                var net = new SwapNetwork(rep, world, placer, kit);
                net.Beams(); net.Pylons(); net.Rails(); net.Trams();

                // ---- 7. the old gives way: the zone, our structures, the beam corridor, the pylons, the tram corridor
                SwapCut.Run(rep, world, ours, net, hq);

                // ---- 8. SpaceHQ switch: both stand, one is off
                var sw = SwapSwitches.Make(heights, hq);
                var oldHq = sw.oldSpaceHq; var ourHq = sw.oursSpaceHq;
                rep.Log($"SpaceHQ: old {(oldHq ? "present" : "MISSING")}, ours {(ourHq ? "present" : "MISSING")}; on: {hq}" + (File.Exists(Path.Combine(Repo, "briefs/075/oldbuild/READY-spacehq-datum")) ? "" : "; the S1 station beside the old HQ waits on Datum's plan (no READY-spacehq-datum)"));

                // ---- 9. the old finish: tile merge, cutaway tags, probes, environment bake, the HQ's showpiece probe
                SwapExport.Ground(rep);  // kerbs, pavement cells and roads as placed, before the merge folds them into tiles
                DecoKitTileMerge.Merge(street);
                DecoKitCutawayTags.Apply();
                for (float z = L.Z.x + 20f; z < L.Z.y; z += 40f)
                    DecoKitAtmosphere.Probe($"Street probe {z:0}", new Vector3(0f, 5f, z), new Vector3(2f * L.FrontX, 22f, 44f));
                DecoKitAtmosphere.Probe("Circus probe", P.Circus + Vector3.up * 6f, new Vector3(80f, 24f, 80f));
                DecoKitAtmosphere.Probe("Gardens probe", new Vector3(-78f, 4f, 105f), new Vector3(70f, 16f, 300f));
                foreach (var (name, c, size) in DecoKitSouthside.Probes) DecoKitAtmosphere.Probe(name, c, size);
                for (float z = P.HarbourZ.x + 40f; z < P.HarbourZ.y; z += 80f)
                    DecoKitAtmosphere.Probe($"Harbour probe {z:0}", new Vector3(85f, 3f, z), new Vector3(80f, 20f, 84f));
                var zc = ZoneOld.center;
                DecoKitAtmosphere.Probe("Handover zone probe", new Vector3(zc.x, 8f, zc.y), new Vector3(ZoneOld.width, 30f, ZoneOld.height));  // ours had its own probes (light.json); the old look's probe here
                DecoKitStage.BakeEnvironment(scene);
                if (oldHq) { bool was = oldHq.activeSelf; oldHq.SetActive(true); DecoKitShowpiece.Apply("SpaceHQ", skyGain); oldHq.SetActive(was); }
                var cam = DecoKitStage.Camera("DK street study camera", 60f);
                cam.nearClipPlane = .05f; cam.farClipPlane = 2500f; cam.enabled = false;
                DecoKitStage.Save();

                // ---- 10. exports: the old-built geometry for Splice's plan-match check, the occlusion measure's occluders, the report
                SwapExport.Built(rep, Path.Combine(outDir, heights == "rule" ? "built.json" : $"built-heights-{heights}.json"), world, heights, hq, scene);
                SwapExport.Occluders(rep, Path.Combine(outDir, $"occluders-{heights}.json"), heights);
                rep.Log($"built Assets/Scenes/{scene}.unity in {(DateTime.UtcNow - t0).TotalSeconds:0} s");
                SwapExport.WriteReport(rep, Path.Combine(outDir, $"swap-{heights}.json"), heights, hq, scene, world);
            }
            finally { DecoKitFrontage.Lower = null; }
        }

        /// <summary>DecoKitRoadMarkings paints the zebras into the one "Road markings" mesh with the dashes: a stripe is a 3 x 0.5 m quad (1.5 m²),
        /// a dash 0.12 m wide (under 0.5 m²). The stripes go; the dashes and the coral tactile pads stay (keep/drop: keep the old pads).</summary>
        static void Zebras(Report rep)
        {
            int n = 0;
            foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(m => m.name == "Road markings" && m.sharedMesh).ToArray())
            {
                var rd = new SwapMesh.Read(mf.sharedMesh, mf.transform.localToWorldMatrix);
                var ids = rd.Components(out int count);
                var area = new float[count]; var at = new Vector3[count];
                for (int s = 0; s < rd.Subs.Count; s++)
                    for (int t = 0; t < ids[s].Length; t++)
                    {
                        Vector3 a = rd.Verts[rd.Subs[s][3 * t]].W, b = rd.Verts[rd.Subs[s][3 * t + 1]].W, c = rd.Verts[rd.Subs[s][3 * t + 2]].W;
                        area[ids[s][t]] += Vector3.Cross(b - a, c - a).magnitude * .5f; at[ids[s][t]] = a;
                    }
                var w = new SwapMesh.Write(rd);
                for (int s = 0; s < rd.Subs.Count; s++)
                    for (int t = 0; t < ids[s].Length; t++)
                        if (area[ids[s][t]] < 1.2f) w.Keep(s, rd.Subs[s][3 * t], rd.Subs[s][3 * t + 1], rd.Subs[s][3 * t + 2]);
                for (int k = 0; k < count; k++) if (area[k] >= 1.2f) { n++; rep.Gave("keep/drop: crossings (no zebras)", "zebra stripe", at[k], "keepdrop"); }
                mf.sharedMesh = w.Mesh(mf.sharedMesh.name + " (no zebras)");
            }
            rep.Log($"zebras: {n} stripes taken out of the old road markings (dashes and coral tactile pads kept); the floating-eye crossings are ours, at the plan's crossings");
        }

        internal static IEnumerable<GameObject> Instances(string piece) =>
            UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) && PieceOf(t.name) == piece).Select(t => t.gameObject);

        /// <summary>"DK_StreamBay_6 #12.3" -> "StreamBay_6".</summary>
        internal static string PieceOf(string name)
        {
            var n = name.StartsWith("DK_") ? name[3..] : name;
            int h = n.IndexOf(" #", StringComparison.Ordinal); if (h >= 0) n = n[..h];
            int p = n.IndexOf(" (", StringComparison.Ordinal); if (p >= 0) n = n[..p];
            return n;
        }

        static List<Vector2> Thin(Vector2[] line, float step)
        {
            var o = new List<Vector2> { line[0] }; float acc = 0f;
            for (int i = 1; i < line.Length; i++) { acc += Vector2.Distance(line[i - 1], line[i]); if (acc >= step || i == line.Length - 1) { o.Add(line[i]); acc = 0f; } }
            return o;
        }

        /// <summary>What stands, by frontage piece: count and top (m), so the report says what stayed tall and what came down.</summary>
        static void HeightCensus(Report rep, string heights)
        {
            var by = new SortedDictionary<string, (int n, float top)>(StringComparer.Ordinal);
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (!PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) || !t.name.Contains(" #")) continue;
                var piece = PieceOf(t.name); var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
                float top = rs.Max(r => r.bounds.max.y);
                by[piece] = by.TryGetValue(piece, out var v) ? (v.n + 1, Mathf.Max(v.top, top)) : (1, top);
            }
            rep.Log($"frontage pieces ({heights}): " + string.Join(", ", by.Select(kv => $"{kv.Key} {kv.Value.n} (top {kv.Value.top:0.0})")));
            SwapExport.Census[heights] = by.ToDictionary(kv => kv.Key, kv => (object)new Dictionary<string, object> { ["n"] = kv.Value.n, ["top"] = Mathf.Round(kv.Value.top * 10f) / 10f });
        }

        /// <summary>Rule heights: the 27-43 m alley walls behind the lowered boulevard runs swap for our low alley walls (keep/drop: "the old lacked low backs").</summary>
        static void AlleySwap(Report rep, OurKit kit)
        {
            var map = new Dictionary<string, string> { ["AlleyWall_9"] = "AlleyWall_4", ["AlleyWall_6"] = "AlleyWall_3", ["AlleyWall_6D"] = "AlleyWall_3D", ["AlleyStair_9"] = "AlleyWall_4D" };
            int n = 0;
            foreach (var (from, to) in map)
                foreach (var go in Instances(from).ToArray())
                {
                    var low = kit.Put(to, go.transform.position, go.transform.rotation, go.transform.parent);
                    if (low == null) { rep.Log($"alley swap: our {to} not found; {from} kept"); break; }
                    low.transform.localScale = go.transform.localScale;
                    UnityEngine.Object.DestroyImmediate(go); n++;
                }
            rep.Log($"heights rule: {n} old alley walls (27-43 m) swapped for our low alley walls (AlleyWall_3/3D/4/4D)");
        }
    }

    /// <summary>Our kit's prefabs by piece id (Assets/Suborned/Kit/Prefabs/**/DK_&lt;id&gt;.prefab), for the pieces the old kit never had.</summary>
    internal sealed class OurKit
    {
        readonly Dictionary<string, GameObject> cache = new();
        public GameObject Prefab(string id)
        {
            if (cache.TryGetValue(id, out var p)) return p;
            var guid = AssetDatabase.FindAssets($"DK_{id} t:Prefab", new[] { "Assets/Suborned/Kit/Prefabs" })
                .FirstOrDefault(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)) == $"DK_{id}");
            return cache[id] = guid == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
        }
        public GameObject Put(string id, Vector3 pos, Quaternion rot, Transform parent)
        {
            var p = Prefab(id); if (p == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(p, parent);
            go.transform.SetPositionAndRotation(pos, rot);
            return go;
        }
    }

}
#endif
