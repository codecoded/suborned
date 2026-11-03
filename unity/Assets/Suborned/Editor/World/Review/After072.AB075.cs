using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Review
{
    /// <summary>Brief 075 Perf A/B (Latch, 2026-10-29, Redline's item A): After072's offscreen estimator (Counts: the 16:9 frustum census with
    /// the LOD pick, the SetPass estimate, the reproduced shadow cascades, batchmode frame ms) on any open scene and camera, so the old-built
    /// town (OldTown.unity, the old camera, no TacticalShadows) and our district are counted by the same code. Adds per view: draws (sub-meshes,
    /// before batching or instancing; main and per cascade), distinct shaders, the share of renderers and triangles under named roots (the old
    /// glTF crowd) and on named pieces (the old per-metre ground), the reflection probes touching the frame, the sun's place in the frame and
    /// the flare's state, render counters (ProfilerRecorder, if it reads offscreen) and frame ms per GPU Resident Drawer mode. Driven from the
    /// swap assembly (Editor/OldTown/Swap/SwapPerf.cs), which owns the old render-settings swap; nothing here saves anything.</summary>
    public static partial class After072
    {
        public sealed class AbView
        {
            public string id, kind;  // kind: play | golden | street
            public Action<Camera> pose;
            public Vector2 focusPlan; public float yaw;
        }

        public sealed class AbSpec
        {
            public Camera cam; public TacticalShadows shadows; public ArchitectureFade fade; public float standOff = Baseline072.DefaultStandOff;
            public List<AbView> views = new();
            public string[] groupRoots = Array.Empty<string>();    // e.g. "District crowd and traffic"
            public string[] groundPieces = Array.Empty<string>();  // name fragments, e.g. "DK_Tile_TramRail"
            public GPUResidentDrawerMode[] drawers = { GPUResidentDrawerMode.Disabled, GPUResidentDrawerMode.InstancedDrawing };
        }

        const int ModeWarm = 60;
        static readonly string[] AbCounters = { "SetPass Calls Count", "Draw Calls Count", "Batches Count", "Triangles Count", "Vertices Count", "Shadow Casters Count", "Visible Skinned Meshes Count" };

        /// <summary>The counts per view, as plain dictionaries and lists (the caller writes them as JSON).</summary>
        public static List<object> AbCapture(AbSpec s)
        {
            var urp = UniversalRenderPipeline.asset ?? throw new InvalidOperationException("075ab: no URP asset active");
            var c = new Ctx { cam = s.cam, shadows = s.shadows, fade = s.fade, standOff = s.standOff };
            var meshTris = new Dictionary<Mesh, long[]>();
            var px = new Texture2D(1, 1, TextureFormat.RGBAHalf, false);
            var outList = new List<object>(); var msByView = new List<Dictionary<string, object>>();
            var drawerWas = urp.gpuResidentDrawerMode;
            using var capture = new Capture();
            var rt = capture.Target(W, H);
            bool ssao = SsaoOn();
            var probes = UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Exclude).Where(p => p.enabled).ToArray();
            var sun = RenderSettings.sun ? RenderSettings.sun : UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude).FirstOrDefault(l => l.type == LightType.Directional && l.enabled);
            var flare = sun ? sun.GetComponent<LensFlareComponentSRP>() : null;
            try
            {
                foreach (var av in s.views)
                {
                    urp.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;
                    av.pose(s.cam);
                    var v = new Baseline072.View { Id = av.id, Label = av.id, Focus = av.focusPlan, Yaw = av.yaw };
                    var visible = new List<Renderer>(); var lodOut = new HashSet<Renderer>();
                    Baseline072.Census(s.cam, v, meshTris, visible, lodOut);
                    var n = new Count { v = v };
                    var mats = new HashSet<Material>(); var fwd = new HashSet<string>(); var dn = new HashSet<string>(); var shaders = new HashSet<Shader>();
                    long mainDraws = 0;
                    var group = s.groupRoots.ToDictionary(g => g, _ => new long[3]);   // renderers, tris, draws
                    var ground = s.groundPieces.ToDictionary(g => g, _ => new long[3]);
                    foreach (var r in visible)
                    {
                        if (r.shadowCastingMode == ShadowCastingMode.ShadowsOnly) { n.shadowsOnlyInFrustum++; continue; }
                        n.mainRenderers++;
                        var t = Tris(r, meshTris); var sm = r.sharedMaterials; long rt0 = 0;
                        for (int i = 0; i < t.Length; i++)
                        {
                            n.mainTris += t[i]; rt0 += t[i];
                            var m = i < sm.Length ? sm[i] : (sm.Length > 0 ? sm[^1] : null); if (!m || !m.shader) continue;
                            mats.Add(m); shaders.Add(m.shader); fwd.Add(Variant(m, null));
                            if (m.renderQueue < 2500) { n.opaqueTris += t[i]; if (ssao && m.FindPass("DepthNormals") >= 0) dn.Add(Variant(m, new[] { "_ALPHATEST_ON", "_NORMALMAP" })); }
                        }
                        mainDraws += t.Length;
                        var g = Under(r.transform, s.groupRoots); if (g != null) { var a = group[g]; a[0]++; a[1] += rt0; a[2] += t.Length; }
                        var p = Named(r.transform, s.groundPieces); if (p != null) { var a = ground[p]; a[0]++; a[1] += rt0; a[2] += t.Length; }
                    }
                    n.materials = mats.Count; n.fwdVariants = fwd.Count; n.dnVariants = dn.Count;
                    ShadowCensus(c, n, lodOut, meshTris, rt, px);
                    // probes whose box touches the frustum
                    s.cam.aspect = (float)W / H; var planes = GeometryUtility.CalculateFrustumPlanes(s.cam); s.cam.ResetAspect();
                    var probesIn = probes.Where(pr => GeometryUtility.TestPlanesAABB(planes, pr.bounds)).Select(pr => pr.name).ToList();
                    // the sun in the frame (the flare draws only when its source is on screen and not occluded)
                    object sunIn = null;
                    if (sun)
                    {
                        var vp = s.cam.WorldToViewportPoint(s.cam.transform.position - sun.transform.forward * 5000f);
                        bool inFrame = !s.cam.orthographic ? vp.z > 0 && vp.x >= 0 && vp.x <= 1 && vp.y >= 0 && vp.y <= 1
                                                         : Vector3.Dot(-sun.transform.forward, s.cam.transform.forward) > 0.999f;  // ortho: only a sun straight down the view axis
                        sunIn = new Dictionary<string, object> { ["inFrame"] = inFrame, ["viewport"] = new[] { R(vp.x), R(vp.y) }, ["angleFromViewAxis"] = R(Vector3.Angle(-sun.transform.forward, s.cam.transform.forward)) };
                    }
                    var ms = new Dictionary<string, object>(); msByView.Add(ms);  // filled by the timing passes below, one drawer mode at a time
                    urp.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;
                    long shadowDraws = n.cascades.Sum(k => (long)k.draws);
                    outList.Add(new Dictionary<string, object>
                    {
                        ["view"] = av.id, ["kind"] = av.kind, ["focusPlan"] = new[] { av.focusPlan.x, av.focusPlan.y }, ["yaw"] = av.yaw,
                        ["camera"] = s.cam.orthographic ? $"ortho {s.cam.orthographicSize:0.#}, pitch {s.cam.transform.eulerAngles.x:0.#}, yaw {s.cam.transform.eulerAngles.y:0.#}" : $"perspective fov {s.cam.fieldOfView:0.#} at ({s.cam.transform.position.x:0.#}, {s.cam.transform.position.y:0.#}, {s.cam.transform.position.z:0.#}) pitch {s.cam.transform.eulerAngles.x:0.#} yaw {s.cam.transform.eulerAngles.y:0.#}",
                        ["setPassEstimate"] = new Dictionary<string, object> { ["forward"] = n.fwdVariants, ["depthNormals"] = n.dnVariants, ["shadow"] = n.shadowVariants, ["total"] = n.fwdVariants + n.dnVariants + n.shadowVariants },
                        ["draws"] = new Dictionary<string, object> { ["main"] = mainDraws, ["shadow"] = shadowDraws, ["perCascade"] = n.cascades.Select(k => (object)k.draws).ToList() },
                        ["mainRenderers"] = n.mainRenderers, ["mainTriangles"] = n.mainTris, ["opaqueTriangles"] = n.opaqueTris, ["shadowsOnlyInFrustum"] = n.shadowsOnlyInFrustum,
                        ["censusVisibleRenderers"] = v.Visible, ["censusTriangles"] = v.VisibleTris, ["lodCulled"] = v.LodCulled,
                        ["shadow"] = new Dictionary<string, object> { ["casters"] = n.uniqueCasters, ["triangles"] = n.shadowTris, ["note"] = n.cascadeNote, ["cascades"] = n.cascades.Select(k => (object)new Dictionary<string, object> { ["casters"] = k.casters, ["draws"] = k.draws, ["triangles"] = k.tris, ["variants"] = k.variants, ["sphereRadius"] = R(k.radius) }).ToList() },
                        ["materials"] = n.materials, ["shaders"] = shaders.Count, ["shaderVariantsForward"] = n.fwdVariants,
                        ["groups"] = group.ToDictionary(kv => kv.Key, kv => (object)Share(kv.Value, n.mainRenderers, n.mainTris, mainDraws)),
                        ["ground"] = ground.ToDictionary(kv => kv.Key, kv => (object)Share(kv.Value, n.mainRenderers, n.mainTris, mainDraws)),
                        ["probesInFrame"] = probesIn.Count, ["probes"] = probesIn,
                        ["sun"] = sunIn, ["flare"] = flare ? $"LensFlareComponentSRP {(flare.enabled && flare.gameObject.activeInHierarchy ? "enabled" : "disabled")}, occlusion {(flare.useOcclusion ? "on" : "off")}, intensity {flare.intensity:0.##}" : "none on the sun",
                        ["byLayer"] = v.ByLayer.OrderByDescending(kv => kv.Value.tris).Take(14).ToDictionary(kv => kv.Key, kv => (object)new Dictionary<string, object> { ["renderers"] = kv.Value.n, ["triangles"] = kv.Value.tris }),
                        ["frameMs"] = ms,
                    });
                    Debug.Log($"075ab: {av.id}: main {n.mainRenderers} renderers {n.mainTris:N0} tris {mainDraws} draws, shadow {n.uniqueCasters} casters {n.shadowTris:N0} tris {shadowDraws} draws, SetPass est {n.fwdVariants + n.dnVariants + n.shadowVariants}, probes {probesIn.Count}");
                }
                // frame ms: one pass over every view per drawer mode (switching the drawer re-uploads every instance: it is switched once
                // and warmed before the pass, so no view pays for it), and the render counters if they read offscreen
                foreach (var mode in s.drawers)
                {
                    urp.gpuResidentDrawerMode = mode;
                    s.views[0].pose(s.cam); s.cam.targetTexture = rt; s.cam.ResetAspect();
                    for (int i = 0; i < ModeWarm; i++) Sync(s.cam, rt, px);
                    for (int vi = 0; vi < s.views.Count; vi++)
                    {
                        s.cam.targetTexture = null; s.views[vi].pose(s.cam); s.cam.targetTexture = rt; s.cam.ResetAspect();
                        for (int i = 0; i < WarmFrames; i++) Sync(s.cam, rt, px);
                        var recs = AbCounters.Select(k => (k, rec: ProfilerRecorder.StartNew(ProfilerCategory.Render, k, 4))).ToList();
                        var before = recs.Select(x => x.rec.CurrentValue).ToArray();
                        var times = new List<double>();
                        for (int i = 0; i < TimedFrames; i++) { var sw = System.Diagnostics.Stopwatch.StartNew(); Sync(s.cam, rt, px); times.Add(sw.Elapsed.TotalMilliseconds); }
                        var counters = new Dictionary<string, object>();
                        for (int i = 0; i < recs.Count; i++) { var r = recs[i].rec; counters[recs[i].k] = r.CurrentValue - before[i]; r.Dispose(); }
                        times.Sort();
                        msByView[vi][mode.ToString()] = new Dictionary<string, object>
                        {
                            ["median"] = Math.Round(times[times.Count / 2], 2), ["p90"] = Math.Round(times[(int)(times.Count * 0.9)], 2),
                            ["drawerActive"] = DrawerActive(), ["renderCountersDelta"] = counters,
                        };
                    }
                    s.cam.targetTexture = null; s.cam.ResetAspect();
                    Debug.Log($"075ab: drawer {mode} ({DrawerActive()}): " + string.Join(", ", s.views.Select((v, i) => $"{v.id} {((Dictionary<string, object>)msByView[i][mode.ToString()])["median"]}")));
                }
            }
            finally { urp.gpuResidentDrawerMode = drawerWas; s.cam.targetTexture = null; s.cam.ResetAspect(); UnityEngine.Object.DestroyImmediate(px); }
            return outList;
        }

        static Dictionary<string, object> Share(long[] a, long renderers, long tris, long draws) => new()
        {
            ["renderers"] = a[0], ["triangles"] = a[1], ["draws"] = a[2],
            ["shareRenderers"] = renderers > 0 ? Math.Round((double)a[0] / renderers, 4) : 0, ["shareTriangles"] = tris > 0 ? Math.Round((double)a[1] / tris, 4) : 0, ["shareDraws"] = draws > 0 ? Math.Round((double)a[2] / draws, 4) : 0,
        };

        static string Under(Transform t, string[] roots) { if (roots.Length == 0) return null; for (var x = t; x; x = x.parent) foreach (var r in roots) if (x.name == r) return r; return null; }
        static string Named(Transform t, string[] parts) { if (parts.Length == 0) return null; for (var x = t; x; x = x.parent) foreach (var p in parts) if (x.name.Contains(p)) return p; return null; }
        static float R(float v) => Mathf.Round(v * 1000f) / 1000f;

        /// <summary>Whether URP's GPU Resident Drawer is running now (its internal instance), or why it cannot be read.</summary>
        public static string DrawerActive()
        {
            try
            {
                var t = typeof(GPUResidentDrawer);
                var p = t.GetProperty("instance", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                var f = p == null ? t.GetField("s_Instance", BindingFlags.NonPublic | BindingFlags.Static) : null;
                object inst = p != null ? p.GetValue(null) : f?.GetValue(null);
                if (p == null && f == null) return "unknown (no instance member)";
                return inst != null ? "active" : "inactive";
            }
            catch (Exception e) { return "unknown (" + e.Message + ")"; }
        }

        /// <summary>The asset's GPU Resident Drawer mode as serialized (RenderSession forces Disabled in memory).</summary>
        public static string AbDrawerSerialized(UniversalRenderPipelineAsset urp)
        {
            // the asset file, not the object: EditorDrawerGuard sets the drawer Disabled in memory whenever the Editor loads (brief 011 D)
            var path = AssetDatabase.GetAssetPath(urp);
            var m = System.Text.RegularExpressions.Regex.Match(System.IO.File.ReadAllText(System.IO.Path.Combine(HarbourWorldBuilder.RepoRoot, "unity", path)), @"m_GPUResidentDrawerMode: (\d+)");
            return m.Success ? $"{(GPUResidentDrawerMode)int.Parse(m.Groups[1].Value)} (the asset file; the Editor runs with it off, the counts run off and each timing sets its mode)" : "unknown";
        }

        /// <summary>HandoverHost.PlaceCamera's play pose (Baseline072.Pose), for the swap assembly.</summary>
        public static void AbPlayPose(Camera cam, TacticalShadows shadows, ArchitectureFade fade, Vector2 focusWorld, float yaw, float standOff) =>
            Baseline072.Pose(cam, shadows, fade, focusWorld, yaw, standOff);

        /// <summary>The scene's texture memory (After072 texmem) to a file.</summary>
        public static void AbTexMem(string file, string export, string tag) => TexMem(file, export, tag);

        /// <summary>GPU size (bytes) of a texture from its format, size, mips and layers (TexMem's formula).</summary>
        public static long AbGpuBytes(Texture t)
        {
            int layers = t is Texture2DArray ta ? ta.depth : t is Cubemap ? 6 : t is Texture3D t3 ? t3.depth : 1;
            long sum = 0; int w = t.width, h = t.height;
            for (int i = 0; i < Mathf.Max(1, t.mipmapCount); i++)
            {
                sum += (long)UnityEngine.Experimental.Rendering.GraphicsFormatUtility.ComputeMipmapSize(Mathf.Max(1, w), Mathf.Max(1, h), t.graphicsFormat);
                w >>= 1; h >>= 1;
            }
            return sum * layers;
        }

        /// <summary>The first render after a load comes out flat: one throwaway frame.</summary>
        public static void AbWarm(Camera cam)
        {
            using var session = new Capture();
            session.Grab(cam, 64, 64);
        }
    }
}
