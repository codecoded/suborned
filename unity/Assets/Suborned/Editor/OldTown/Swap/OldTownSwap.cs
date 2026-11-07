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
    ///     -swapHeights rule|old (default rule) -swapHq old|ours (default ours, Sky 2026-10-29) -swapScene OldTown
    ///     -swapOurs Assets/Scenes/probe-oldtown-ours.unity -swapWorld out/scratch/latch/075swap2/harbour.world.json -swapOut briefs/075/oldbuild/swap2
    ///     -swapPlan contracts/world/harbour-district.plan.json
    /// Second build (plan 0.5.5): the old builders run with edit E10's parameters set to the plan (walk 0.18, road deck 0, boulevard kerbs +-10,
    /// the circus ring's outer kerb r 29, the Southside blocks on their plan blocks, the city mass by the plan), the west filled (SwapWest), the
    /// old gives way to the Handover streets and plots (SwapCut), our SpaceHQ_Portal in the old finish (SwapHq, switch default "ours"), and a
    /// play camera with our play look (SwapPlayLook: TacticalShadows, no fog, our grade, no crowd, no MSAA, no soft focus or flare).
    ///   ... .OldTownSwap.Shoot  -swapScene OldTown -swapShots circus,golden,vista,play,district -swapTag rule   (offscreen renders)
    ///   ... .OldTownSwap.Flyby  -swapScene OldTown (the old reel's route, ReviewVideo encode)</summary>
    public static class OldTownSwap
    {
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        /// <summary>Plan (x, y) = old Unity (x, z - 206).</summary>
        internal const float DZ = Suborned.OldTown.OldTownFrame.DZ;
        /// <summary>The Handover zone, plan standards.blocks.zone [x0, x1, y0, y1] = [-226, -84, -372, -148].</summary>
        internal static readonly Rect ZonePlan = Rect.MinMaxRect(-226f, -372f, -84f, -148f);
        internal static Rect ZoneOld => new(ZonePlan.x, ZonePlan.y + DZ, ZonePlan.width, ZonePlan.height);
        internal static Vector2 Old(Vector2 plan) => new(plan.x, plan.y + DZ);
        internal static Vector3 Old3(Vector2 plan, float y) => new(plan.x, y, plan.y + DZ);

        static string Repo => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        static string Abs(string p) => Path.IsPathRooted(p) ? p : Path.GetFullPath(Path.Combine(Repo, p));
        static string Arg(string k, string d) => DecoKitStage.Arg(k, d);
        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        internal static void SettledBatch(Action body) => Batch(()=>WithOldSettings(body));
        // The full joined-surface rewrite is an editor experiment until its memory/build cost is cleared.
        internal static bool JoinedTram => Arg("swapJoinedTram", "off") == "on";
        public static void Build() => Batch(() => WithOldSettings(BuildBody));
        /// <summary>The plan the build read (path, sha256, version, SpaceHQ form), for the exports.</summary>
        internal static Dictionary<string, object> PlanFacts = new();
        public static void Shoot() => Batch(() => WithOldSettings(SwapShots.Run));
        public static void Flyby() => Batch(() => WithOldSettings(SwapShots.Flyby));
        public static void RecordsFlyby() => Batch(() => WithOldSettings(SwapShots.RecordsFlyby));
        public static void Verify() => Batch(SwapVerify.Run);
        /// <summary>The tram bed audit (SwapTramAudit) on a built scene, read only: -swapScene, -swapWorld, -swapOut (tram-audit-&lt;tag&gt;.json, -swapTag).</summary>
        public static void TramAudit() => Batch(() =>
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene($"Assets/Scenes/{Arg("swapScene", "OldTown")}.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
            var world = WorldReader.Load(Abs(Arg("swapWorld", "out/scratch/latch/075swap2/harbour.world.json")));
            var outDir = Abs(Arg("swapOut", "briefs/075/oldbuild/tram")); Directory.CreateDirectory(outDir);
            SwapTramAudit.Run(new Report(), world, Path.Combine(outDir, $"tram-audit-{Arg("swapTag", "scene")}.json"));
        });
        /// <summary>Controlled render-only correction of the pinned provisional scene; no old builders or civic imports rerun.</summary>
        public static void TramFix() => SwapTramSettled.Run();
        /// <summary>Brief 075 Perf A/B item A (SwapPerf): -abScene OldTown|HarbourDistrict, -abPipeline old|ours (OldTown; old = inside WithOldSettings).</summary>
        public static void PerfAB() => Batch(() => { if (Arg("abScene", "OldTown") == "OldTown" && Arg("abPipeline", "old") == "old") WithOldSettings(SwapPerf.Run); else SwapPerf.Run(); });

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
            public readonly List<object> Towers = new();  // SwapTowers: every roof-programme tower tested, standing or not
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
            string heights = Arg("swapHeights", "rule"), hq = Arg("swapHq", "ours");
            string scene = Arg("swapScene", heights == "rule" ? "OldTown" : "OldTown-heights-old");
            string oursScene = Arg("swapOurs", "Assets/Scenes/probe-oldtown-ours.unity");
            string worldPath = Abs(Arg("swapWorld", "out/scratch/latch/075swap2/harbour.world.json"));
            string outDir = Abs(Arg("swapOut", "briefs/075/oldbuild/swap2"));
            var plan = SwapPlan.Load(Abs(Arg("swapPlan", "contracts/world/harbour-district.plan.json")));
            PlanFacts = new Dictionary<string, object> { ["path"] = Path.GetRelativePath(Repo, plan.Path), ["sha256"] = plan.Sha256, ["version"] = plan.Version, ["spacehqForm"] = plan.SpaceHqForm };
            if (heights != "rule" && heights != "old") throw new ArgumentException("-swapHeights rule|old");
            if (hq != "old" && hq != "ours") throw new ArgumentException("-swapHq old|ours");
            Directory.CreateDirectory(outDir);
            var rep = new Report();
            var world = WorldReader.Load(worldPath);
            rep.Log($"export {Path.GetRelativePath(Repo, worldPath)} sha256 {world.Sha256} ({world.Source}); heights {heights}; SpaceHQ {hq}; scene Assets/Scenes/{scene}.unity; ours from {oursScene}");
            rep.Log($"plan {PlanFacts["path"]} {plan.Version} sha256 {plan.Sha256} (SpaceHQ form \"{plan.SpaceHqForm}\"): {plan.Roads.Count} roads, {plan.Plots.Count} plots, {plan.Blocks.Count} blocks");
            if (!world.Source.Contains(plan.Version ?? "?")) throw new InvalidDataException($"[Swap] export {world.Source} is not from plan {plan.Version}");

            DecoKitFrontage.Lower = heights == "rule" ? Rule : null;
            DecoKitHighrise.Stands = SwapTowers.Rule(rep, rep.Towers);  // both height settings: no roof tower where it buries the Handover zone
            // edit E10: plan 0.5.5's street parameters (Datum, "Swap: patch applied" items 1-3, 6), put back in finally
            var oldBlocks = DecoKitSouthside.Blocks;
            L.Walk = .18f; L.BoulevardKerbX = 10f; P.RingR = 29f; DecoKitPlacer.GroundScaleY = .18f / .15f; DecoKitSouthside.SatelliteHalf = 10f;
            DecoKitSouthside.Blocks = SwapPlan.Southside(oldBlocks, rep);
            var reserved = new[] { "SS5", "W1" }.Where(n => DecoKitSouthside.Reserved.Add(n)).ToArray();
            DecoKitCityMass.Rule = plan.MassRule(rep);
            SwapKeep.Set(plan, rep);  // 0.5.6: Datum's handover-street and megastructure classes (old gives way, ours kept)
            rep.Log($"plan 0.5.5 parameters (E10): walk {L.Walk}, boulevard kerbs +-{L.BoulevardKerbX} (Crown Avenue +-{L.KerbX}), circus ring outer kerb r {P.RingR}, ground modules x{DecoKitPlacer.GroundScaleY:0.###} in height, Southside on its plan blocks (SS5, W1 stand down), city mass by the plan");
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
                Vintage(rep, grade, Arg("swapVintage", "v2s"));  // Glint's pick B (parity/READY-vintage-glint), presentation cameras only (the play camera zeroes this volume)
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
                SwapWest.Build(rep, plan, world, placer);  // before the colourways, so the west's old pieces take them too
                Sphere(rep, world);  // 0.5.6: the old SphereStation at the plan's Sphere (landmark/sphere centre)
                DecoKitColourways.Apply(DecoKitStage.Arg("dkColour", DecoKitStreetDemo.Colour));
                demo.GetMethod("Ground", Any).Invoke(null, null);
                SwapWest.Ground(rep);
                SwapWest.LiftRoad(rep);
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
                // our scene carried the plan's (old) SpaceHQ piece: the import drops its meshes, but an emptied "DK_SpaceHQ" would shadow the old
                // builders' one for the switch and the showpiece probe (both find it by name)
                int emptied = 0;
                foreach (var t in ours.Root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("DK_SpaceHQ")).ToArray()) { t.name = "ours-emptied " + t.name; emptied++; }
                if (emptied > 0) rep.Log($"ours: {emptied} emptied SpaceHQ objects from our scene renamed (the old builders' DK_SpaceHQ is the switch's)");

                // ---- 6. the network in the old look on our routes
                var net = new SwapNetwork(rep, world, placer, kit);
                net.Beams(); net.Pylons(); net.Rails(); net.Trams();
                net.ClearBed(plan);  // E12: the old road modules and paint on our tram bed give way

                // ---- 7. the old gives way: the zone, our structures, the beam corridor, the pylons, the tram corridor
                SwapCut.Run(rep, world, ours, net, hq, plan);
                if (JoinedTram)
                {
                    SwapNetwork.ClearImportedGround(rep);
                    SwapTramInlays.Apply(rep);
                    SwapTramKerbCaps.Apply();
                }
                else rep.Log("joined tram rewrite off: -swapJoinedTram on is an explicit editor-only experiment; memory/build cost and kerb finish remain on hold");

                // ---- 8. SpaceHQ switch: both stand, one is off (ours: our SpaceHQ_Portal in the old finish, SwapHq)
                SwapHq.Place(rep, ours.Root, skyGain);
                var sw = SwapSwitches.Make(heights, hq);
                var oldHq = sw.oldSpaceHq; var ourHq = sw.oursSpaceHq;
                rep.Log($"SpaceHQ: old {(oldHq ? "present" : "MISSING")}, ours {(ourHq ? "present" : "MISSING")}; on: {hq}; the plan's form \"{plan.SpaceHqForm}\" (S1 Crown Circus station beside the HQ, as exported)");

                // ---- 9. the old finish: tile merge, cutaway tags, probes, environment bake, the HQ's showpiece probe
                SwapExport.Ground(rep);  // kerbs, pavement cells and roads as placed, before the merge folds them into tiles
                DecoKitTileMerge.Merge(street);
                SwapTramAudit.Run(rep, world, Path.Combine(outDir, "tram-audit.json"));  // E12 evidence: what lies on, or level with, our bed
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
                if (oldHq) { bool was = oldHq.activeSelf; oldHq.SetActive(true); DecoKitShowpiece.Apply("SpaceHQ", skyGain); var pr = GameObject.Find("SpaceHQ showpiece probe"); if (pr) pr.transform.SetParent(oldHq.transform, true); oldHq.SetActive(was); }
                // Sky 2026-10-30: "remove the people for now outside of the handover and vehicles - that's for demo flybys later on". The baked crowd and traffic are left out of the scene (so of the player); the sim's own people and cars are not touched. -swapCrowd on builds them for a flyby.
                if (Arg("swapCrowd", "off") != "on") { int gone = 0; foreach (var r in SwapPerf.CrowdRoots) foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t && t.name == r).ToArray()) { UnityEngine.Object.DestroyImmediate(t.gameObject); gone++; } rep.Log($"baked crowd and traffic: left out ({gone} roots: {string.Join(", ", SwapPerf.CrowdRoots)}); -swapCrowd on keeps them"); }
                var cam = DecoKitStage.Camera("DK street study camera", 60f);
                cam.nearClipPlane = .05f; cam.farClipPlane = 2500f; cam.enabled = false;
                SwapPlayLook.Make(rep, ours, cam, sun, scene);
                DecoKitStage.Save();

                // ---- 10. exports: the old-built geometry for Splice's plan-match check, the occlusion measure's occluders, the report
                SwapExport.Built(rep, Path.Combine(outDir, heights == "rule" ? "built.json" : $"built-heights-{heights}.json"), world, heights, hq, scene);
                SwapExport.Occluders(rep, Path.Combine(outDir, $"occluders-{heights}.json"), heights);
                rep.Log($"built Assets/Scenes/{scene}.unity in {(DateTime.UtcNow - t0).TotalSeconds:0} s");
                SwapExport.WriteReport(rep, Path.Combine(outDir, $"swap-{heights}.json"), heights, hq, scene, world);
            }
            finally
            {
                DecoKitFrontage.Lower = null; DecoKitHighrise.Stands = null; DecoKitCityMass.Rule = null; SwapKeep.Clear();
                L.Walk = .15f; L.BoulevardKerbX = L.KerbX; P.RingR = 26f; DecoKitPlacer.GroundScaleY = 1f; DecoKitSouthside.SatelliteHalf = 4f;
                DecoKitSouthside.Blocks = oldBlocks; foreach (var n in reserved) DecoKitSouthside.Reserved.Remove(n);
            }
        }

        /// <summary>Plan 0.5.6 (form ours) stands the Sphere at the export's landmark/sphere centre (-172, 124), 42 m west of the old Put (-130, 0, 330):
        /// the old SphereStation instance moves there, its yaw and height as the old builders placed it (Datum's landmark-outline note).</summary>
        static void Sphere(Report rep, WorldDto world)
        {
            var lm = world.Landmarks?.FirstOrDefault(l => l.Id == "landmark/sphere");
            if (lm == null) { rep.Log("sphere: no landmark/sphere in the export; the old SphereStation stays at its old Put"); return; }
            int n = 0;
            foreach (var go in Instances("SphereStation").ToArray())
            {
                var from = go.transform.position; var to = Old3(lm.Centre, from.y);
                go.transform.position = to; n++;
                rep.Log($"sphere: old SphereStation moved from old ({F(from.x)}, {F(from.z)}) to ({F(to.x)}, {F(to.z)}) = plan ({F(lm.Centre.x)}, {F(lm.Centre.y)}), the export's landmark/sphere");
            }
            if (n == 0) rep.Log("sphere: no old SphereStation instance found");
        }

        /// <summary>Glint's vintage pick (briefs/075/parity/vintage/variants.json, pick B = GlintVintage's "v2s": the old bloom whole, contrast 13, the
        /// accent lift with the sky and sea hues spared) applied to the old stage's grade profile, by GlintVintage's own Apply (read by reflection,
        /// so Glint's file stays his). "off" keeps the old grade as built.</summary>
        static void Vintage(Report rep, UnityEngine.Rendering.VolumeProfile grade, string id)
        {
            if (id == "off" || grade == null) { rep.Log("vintage: off (the old stage's grade as built)"); return; }
            var t = typeof(GlintVintage);
            var vs = (Array)(t.GetField("Variants", Any)?.GetValue(null) ?? throw new MissingMemberException("GlintVintage.Variants"));
            var v = vs.Cast<object>().FirstOrDefault(x => (string)x.GetType().GetField("id").GetValue(x) == id) ?? throw new ArgumentException($"-swapVintage {id}: no such GlintVintage variant");
            (t.GetMethod("Apply", Any) ?? throw new MissingMethodException("GlintVintage.Apply")).Invoke(null, new[] { grade, v });
            EditorUtility.SetDirty(grade); AssetDatabase.SaveAssets();
            rep.Log($"vintage: Glint's {id} on {grade.name} (presentation cameras; the play camera renders with our play grade): {v.GetType().GetField("note").GetValue(v)}");
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
