#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Suborned.OldTown.Editor.DecoKit;
using Suborned.OldTown.Sky;
using Suborned.World;
using Suborned.World.Review;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 075 Perf A/B, item A (Latch, 2026-10-29, Redline's ask in briefs/075/notes.md "Perf A/B"): the Studio counts of the
    /// old-built town and our district by one estimator (After072.AbCapture: After072's census, SetPass estimate, reproduced cascades and
    /// batchmode ms), batchmode, offscreen, nothing saved. One run is one (scene, pipeline):
    ///   ... -executeMethod Suborned.OldTown.Editor.Swap.OldTownSwap.PerfAB -abScene HarbourDistrict
    ///   ... .OldTownSwap.PerfAB -abScene OldTown -abPipeline old   (inside OldTownSwap.WithOldSettings: the old PC_RPAsset, as Sky saw it)
    ///   ... .OldTownSwap.PerfAB -abScene OldTown -abPipeline ours  (OldTown under our pipeline asset and renderer)
    /// Each view is counted once and timed with the GPU Resident Drawer off and on. Views: Redline's play stops (SwapShots' pose: ortho 14,
    /// pitch 38, stand-off 160; our scene through Baseline072.Pose with TacticalShadows and the fade, as After072), the golden view (the old
    /// reel at t 10, perspective) and a street-level pose (HostFollow's defaults behind a walker at the opening + (0, -20), heading 180).
    /// Out: -abOut (default briefs/075/oldbuild/perf-capture)/&lt;scene&gt;-&lt;pipeline&gt;.json and texmem-&lt;scene&gt;-&lt;pipeline&gt;.json.</summary>
    internal static class SwapPerf
    {
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        static string Repo => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        static string Arg(string k, string d) => DecoKitStage.Arg(k, d);

        internal static readonly (string id, Vector2 focus, float yaw)[] Stops =
        {
            ("cell-open-30", new(-170.5f, -227.5f), 30f), ("office-front-30", new(-160f, -281f), 30f), ("office-front-210", new(-160f, -281f), 210f),
            ("cafe-square-210", new(-170.5f, -227.5f), 210f), ("circus-30", new(0f, -30f), 30f), ("harbour-terrace-30", new(60f, -120f), 30f),
            ("harbour-station-30", new(117f, -140f), 30f), ("southside-30", new(30f, -280f), 30f),
        };
        internal static readonly string[] CrowdRoots = { "District crowd and traffic", "Southside crowd and traffic" };
        internal static readonly string[] GroundPieces = { "tiles merged", "DK_Tile_TramRail", "DK_Pave_", "DK_Kerb_" };  // the merged 2 m tiles (Ground / Southside tiles merged) and any tile left whole
        /// <summary>The street pose: a walker at the opening focus + (0, -20) (TpLongRun's tp-street walk), heading 180, HostFollow's defaults.</summary>
        static readonly Vector2 Walker = new(-170.5f, -247.5f);
        const float FollowFov = 55f, FollowDistance = 5.5f, FollowHeight = 1.3f, FollowRise = .32f, FollowAhead = 8f, FollowLook = 1.35f, FollowFar = 350f;

        internal static void Run()
        {
            var t0 = DateTime.UtcNow;
            string scene = Arg("abScene", "OldTown"), pipeline = scene == "OldTown" ? Arg("abPipeline", "old") : "ours";
            string outDir = Path.Combine(Repo, Arg("abOut", "briefs/075/oldbuild/perf-capture"));
            Directory.CreateDirectory(outDir);
            bool old = scene == "OldTown";
            string path = old ? "Assets/Scenes/OldTown.unity" : HarbourWorldBuilder.ScenePath;
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var urp = UniversalRenderPipeline.asset ?? throw new InvalidOperationException("075ab: no URP asset");
            var facts = new Dictionary<string, object>
            {
                ["scene"] = path, ["pipeline"] = pipeline, ["urpAsset"] = AssetDatabase.GetAssetPath(urp),
                ["renderer"] = string.Join(", ", urp.rendererDataList.ToArray().Where(d => d).Select(AssetDatabase.GetAssetPath)),
                ["machine"] = $"Studio, Unity {Application.unityVersion} batchmode, offscreen: {SystemInfo.deviceModel}, {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType})",
                ["when"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            };

            Camera cam; TacticalShadows ts = null; ArchitectureFade fade = null; float standOff = 160f; string exportLine;
            if (old)
            {
                DecoKitHarbour.Ocean();
                cam = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c.name == "DK street study camera") ?? throw new InvalidOperationException("075ab: no DK street study camera in OldTown");
                var sw = SwapSwitches.Read();
                exportLine = sw != null ? $"OldTown, heights {sw.heights}, SpaceHQ {sw.spaceHq}" : "OldTown (no SwapSwitches)";
            }
            else
            {
                var playGo = GameObject.Find("cameras/play") ?? throw new InvalidOperationException("075ab: no cameras/play in HarbourDistrict");
                cam = playGo.GetComponent<Camera>(); ts = playGo.GetComponent<TacticalShadows>(); fade = playGo.GetComponent<ArchitectureFade>();
                var host = Object.FindAnyObjectByType<global::Suborned.Play.HandoverHost>(FindObjectsInactive.Include);
                if (host) standOff = host.standOff;
                var people = GameObject.Find("people"); if (people) people.SetActive(false);  // the perf players strip the people layer (PerfStrip)
                if (ts) { ts.enabled = false; ts.enabled = true; }
                if (fade) { fade.enabled = false; fade.enabled = true; }
                var stamp = Object.FindAnyObjectByType<WorldStamp>();
                exportLine = stamp ? $"{stamp.exportPath} sha256 {stamp.exportSha256}" : "no WorldStamp";
            }
            facts["export"] = exportLine;
            cam.enabled = true; cam.useOcclusionCulling = false;
            foreach (var oc in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) if (oc != cam && oc.targetTexture == null) oc.enabled = false;
            facts["questions"] = Questions(old, cam, urp);

            facts["drawerAsBuilt"] = After072.AbDrawerSerialized(urp);  // before RenderSession forces it off in memory
            using var session = new RenderSession();  // async shader compilation off, GRD off until each timing sets it, the cascades put back after
            // -abShadow <m>: the shadow distance for this run (SwapShots rendered the old town's play frames Sky saw at 200 m; the old asset says 50)
            float sd = DecoKitStage.Arg("abShadow", 0f);
            if (sd > 0f) { session.ShadowDistance = sd; facts["shadowDistanceOverride"] = $"{sd} m (-abShadow; the asset's own is restored after)"; }
            facts["shadowDistance"] = urp.shadowDistance; facts["cascades"] = urp.shadowCascadeCount; facts["msaa"] = urp.msaaSampleCount;
            facts["depthTexture"] = urp.supportsCameraDepthTexture; facts["opaqueTexture"] = urp.supportsCameraOpaqueTexture; facts["lodBias"] = QualitySettings.lodBias;

            var spec = new After072.AbSpec { cam = cam, shadows = ts, fade = fade, standOff = standOff, groupRoots = CrowdRoots, groundPieces = GroundPieces };
            foreach (var (id, f, yaw) in Stops)
                spec.views.Add(new After072.AbView
                {
                    id = id, kind = "play", focusPlan = f, yaw = yaw,
                    pose = old ? c => SwapShots.Play(c, f, yaw) : c => After072.AbPlayPose(c, ts, fade, f, yaw, standOff),
                });
            spec.views.Add(new After072.AbView { id = "golden", kind = "golden", focusPlan = new Vector2(float.NaN, float.NaN), yaw = float.NaN, pose = c => Golden(c, old) });
            spec.views.Add(new After072.AbView { id = "tp-street", kind = "street", focusPlan = Walker, yaw = 180f, pose = c => Street(c, old) });
            // first render after a load comes out flat
            spec.views[0].pose(cam); After072.AbWarm(cam);
            var views = After072.AbCapture(spec);
            cam.orthographic = false;

            string tag = $"{(old ? "oldtown" : "harbour")}-{pipeline}" + (sd > 0f ? $"-sd{sd:0}" : "");
            After072.AbTexMem(Path.Combine(outDir, $"texmem-{tag}.json"), exportLine, tag);
            facts["textures"] = TextureGroups();
            File.WriteAllText(Path.Combine(outDir, $"{tag}.json"), SwapExport.Json(new Dictionary<string, object>
            {
                ["schema"] = "latch.075ab-capture/1", ["facts"] = facts, ["views"] = views,
                ["notUnityStats"] = "Estimates, not UnityStats (that needs the visible Game view, against the offscreen rule). After072's method: 16:9 frustum census with the LOD pick (the people layer off); SetPass estimate = distinct shader variants (forward + SSAO depth-normals prepass if an SSAO feature is active + shadow per cascade); draws = sub-meshes, before any batching or instancing (an upper bound with GRD on or static batching); shadow = URP's cascades for this camera reproduced (culling planes and sphere), casters by bounds. frameMs = Camera.Render into a 1920x1080 HDR target with the asset's MSAA, GPU waited on, 8 warm + 30 timed, batchmode offscreen: indicative only. renderCounters = ProfilerRecorder render counters read around the 30 timed renders (zero when they do not count offscreen renders).",
            }));
            Debug.Log($"075ab: {tag}: {views.Count} views in {(DateTime.UtcNow - t0).TotalSeconds:0} s → {outDir}");
        }

        /// <summary>Redline's three questions, answered from the open scene and the active pipeline.</summary>
        static Dictionary<string, object> Questions(bool old, Camera cam, UniversalRenderPipelineAsset urp)
        {
            var q = new Dictionary<string, object>();
            var cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            q["cameras"] = cams.Select(c => (object)$"{c.name}: {(c.orthographic ? "ortho" : "perspective")}, TacticalShadows {(c.GetComponent<TacticalShadows>() ? "yes" : "no")}, ArchitectureFade {(c.GetComponent<ArchitectureFade>() ? "yes" : "no")}, renderShadows {c.GetUniversalAdditionalCameraData().renderShadows}, postProcessing {c.GetUniversalAdditionalCameraData().renderPostProcessing}, AA {c.GetUniversalAdditionalCameraData().antialiasing}").ToList();
            q["tacticalShadowsInScene"] = Object.FindObjectsByType<TacticalShadows>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            var feats = new List<object>();
            foreach (var d in urp.rendererDataList) if (d) foreach (var f in d.rendererFeatures) if (f)
            {
                string extra = "";
                if (f is ScreenSpaceAmbientOcclusion)
                {
                    try
                    {
                        var st = typeof(ScreenSpaceAmbientOcclusion).GetField("m_Settings", Any).GetValue(f); var t = st.GetType();
                        extra = string.Join(", ", new[] { "Source", "AfterOpaque", "Intensity", "Radius", "Downsample" }.Select(n => t.GetField(n, Any) is { } fi ? $"{n} {fi.GetValue(st)}" : null).Where(x => x != null));
                    }
                    catch (Exception e) { extra = e.Message; }
                }
                feats.Add($"{f.GetType().Name} '{f.name}' {(f.isActive ? "active" : "inactive")}{(extra.Length > 0 ? " (" + extra + ")" : "")}");
            }
            q["rendererFeatures"] = feats;
            var sun = RenderSettings.sun ? RenderSettings.sun : Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional && l.enabled);
            var flare = sun ? sun.GetComponent<LensFlareComponentSRP>() : null;
            q["sun"] = sun ? $"{sun.name}: forward ({sun.transform.forward.x:0.###}, {sun.transform.forward.y:0.###}, {sun.transform.forward.z:0.###}), elevation {Mathf.Asin(-sun.transform.forward.y) * Mathf.Rad2Deg:0.#}°, shadows {sun.shadows}" : "none";
            q["flare"] = flare ? $"LensFlareComponentSRP on {sun.name}: {(flare.enabled && flare.gameObject.activeInHierarchy ? "enabled" : "disabled")}, occlusion {(flare.useOcclusion ? "on" : "off")}, intensity {flare.intensity:0.##}, data {(flare.lensFlareData ? flare.lensFlareData.name : "none")}" : "no LensFlareComponentSRP on the sun";
            // GRD needs DOTS instancing in every shader it draws (DOTS_INSTANCING_ON); count the renderers whose materials all have it
            var byShader = new Dictionary<Shader, (int renderers, bool dots)>();
            int ok = 0, not = 0, mpb = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!r.enabled) continue;
                bool all = true;
                foreach (var m in r.sharedMaterials)
                {
                    if (!m || !m.shader) continue;
                    if (!byShader.TryGetValue(m.shader, out var e)) e = (0, m.shader.keywordSpace.keywordNames.Contains("DOTS_INSTANCING_ON"));
                    byShader[m.shader] = (e.renderers + 1, e.dots); all &= e.dots;
                }
                if (r.HasPropertyBlock()) mpb++;
                if (all) ok++; else not++;
            }
            q["grdShaders"] = byShader.OrderByDescending(kv => kv.Value.renderers).Select(kv => (object)$"{kv.Key.name}: {(kv.Value.dots ? "DOTS instancing" : "NO DOTS instancing")} ({kv.Value.renderers} renderer-material uses)").ToList();
            q["grdRenderers"] = new Dictionary<string, object> { ["allShadersDots"] = ok, ["someShaderWithoutDots"] = not, ["withPropertyBlock"] = mpb };
            q["renderingPath"] = urp.rendererDataList.ToArray().Where(d => d is UniversalRendererData).Select(d => (object)$"{d.name}: {((UniversalRendererData)d).renderingMode}").ToList();
            return q;
        }

        /// <summary>Probe cubemaps and the glTF crowd's textures, listed apart from the texmem total.</summary>
        static Dictionary<string, object> TextureGroups()
        {
            var probes = new Dictionary<Texture, string>();
            foreach (var p in Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var t = p.mode == ReflectionProbeMode.Custom ? p.customBakedTexture : p.bakedTexture; if (t) probes[t] = p.name;
            }
            var crowd = new HashSet<Texture>();
            foreach (var root in CrowdRoots.Select(n => GameObject.Find(n)).Where(g => g))
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                    foreach (var m in r.sharedMaterials) if (m) foreach (var n in m.GetTexturePropertyNames()) { Texture t; try { t = m.GetTexture(n); } catch { continue; } if (t) crowd.Add(t); }
            long pb = probes.Keys.Sum(After072.AbGpuBytes), cb = crowd.Sum(After072.AbGpuBytes);
            return new Dictionary<string, object>
            {
                ["probeCubemaps"] = new Dictionary<string, object> { ["count"] = probes.Count, ["gpuMiB"] = Math.Round(pb / 1048576.0, 2), ["list"] = probes.Select(kv => (object)$"{kv.Value}: {kv.Key.width}² {kv.Key.graphicsFormat} {After072.AbGpuBytes(kv.Key) / 1024} KiB").ToList() },
                ["crowdTextures"] = new Dictionary<string, object> { ["count"] = crowd.Count, ["gpuMiB"] = Math.Round(cb / 1048576.0, 2) },
            };
        }

        /// <summary>The old reel at t 10 (DecoKitFlyby.Pose, SwapShots' golden), our scene shifted to plan coordinates (z - 206).</summary>
        static void Golden(Camera cam, bool old)
        {
            var tr = old ? Object.FindObjectsByType<SkyTraffic>(FindObjectsSortMode.None) : new SkyTraffic[0];
            var ro = old ? Object.FindObjectsByType<RocketFlight>(FindObjectsSortMode.None) : new RocketFlight[0];
            typeof(DecoKitFlyby).GetMethod("Pose", Any).Invoke(null, new object[] { cam, tr, ro, 10f });
            cam.orthographic = false; cam.nearClipPlane = .05f; cam.farClipPlane = 2500f;
            if (!old) cam.transform.position -= new Vector3(0f, 0f, OldTownSwap.DZ);
        }

        static void Street(Camera cam, bool old)
        {
            var dir = new Vector3(0f, 0f, -1f);  // heading 180
            var feet = old ? OldTownSwap.Old3(Walker, 0f) : new Vector3(Walker.x, 0f, Walker.y);
            cam.orthographic = false; cam.fieldOfView = FollowFov; cam.nearClipPlane = .2f; cam.farClipPlane = FollowFar;
            cam.transform.position = feet - dir * FollowDistance + Vector3.up * (FollowHeight + FollowRise * FollowDistance);
            cam.transform.LookAt(feet + dir * FollowAhead + Vector3.up * FollowLook, Vector3.up);
        }
    }
}
#endif
