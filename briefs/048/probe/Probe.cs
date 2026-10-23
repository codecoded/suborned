// Brief 048 (Redline): draw distance probe. Loaded into a COPY of the built player (out/redline/048/app), never the shipped one, by
// run.sh: the DLL is added to ScriptingAssemblies.json and its Boot to RuntimeInitializeOnLoads.json. Nothing in unity/ changes.
// `-redline048 <dir> [-r48mode discover|measure] [-r48view <agent>,<headingDeg>] [-r48secs n]`
//   discover: inventory + a census of what is drawn from the overhead view and from the follow view behind each agent at 8 headings (+ a jpg each)
//   measure : frame/GPU/CPU times and render counters at play zoom (4 yaws, with variants) and in the follow view at one heading (with variants)
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Suborned.Play;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Redline048
{
    public sealed class Probe : MonoBehaviour
    {
        string dir = "", mode = "discover", cands = "", only = ""; int vAgent = 0; float vHeading = 0f, secs = 5f; Vector2 walk;
        readonly StringBuilder log = new();
        readonly List<string> rows = new();
        HandoverHost h; Camera cam; UniversalRenderPipelineAsset urp; Light sun;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-redline048");
            if (i < 0 || i + 1 >= a.Length) return;
            var go = new GameObject("redline-048"); DontDestroyOnLoad(go); var p = go.AddComponent<Probe>(); p.dir = a[i + 1];
            string Val(string n) { int k = Array.IndexOf(a, n); return k >= 0 && k + 1 < a.Length ? a[k + 1] : null; }
            if (Val("-r48mode") is string m) p.mode = m;
            if (Val("-r48view") is string v) { var s = v.Split(','); p.vAgent = int.Parse(s[0]); p.vHeading = float.Parse(s[1], CultureInfo.InvariantCulture); }
            if (Val("-r48secs") is string ss) p.secs = float.Parse(ss, CultureInfo.InvariantCulture);
            if (Val("-r48walk") is string w) { var s = w.Split(','); p.walk = new Vector2(float.Parse(s[0], CultureInfo.InvariantCulture), float.Parse(s[1], CultureInfo.InvariantCulture)); }
            if (Val("-r48cands") is string c) p.cands = c;
            if (Val("-r48only") is string o) p.only = o;
        }

        static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        void L(string s) { log.AppendLine(s); Debug.Log("R48: " + s); }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            float t0 = Time.realtimeSinceStartup;
            while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) { if (Time.realtimeSinceStartup - t0 > 60f) { L("no host"); Flush(); Application.Quit(2); yield break; } yield return null; }
            yield return new WaitForSecondsRealtime(4f);
            h.controls.conversations = ConversationScenes.Never; h.IntelOpen = false; h.StartClock();
            yield return new WaitForSecondsRealtime(2f);
            cam = h.View; urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            sun = FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.type == LightType.Directional && l.shadows != LightShadows.None).OrderByDescending(l => l.intensity).FirstOrDefault();
            L($"screen {Screen.width}x{Screen.height} {Screen.fullScreenMode}, renderScale {urp?.renderScale}, build {BuildStamp.Line}");
            Inventory();
            try { if (mode == "discover") yield return Discover(); else yield return Measure(); }
            finally { }
            Flush();
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(0);
        }

        void Flush()
        {
            File.WriteAllText(Path.Combine(dir, "log.txt"), log.ToString());
            File.WriteAllText(Path.Combine(dir, mode + ".json"), "{\n \"screen\": \"" + Screen.width + "x" + Screen.height + "\", \"renderScale\": " + F(urp ? urp.renderScale : -1) + ",\n \"rows\": [\n  " + string.Join(",\n  ", rows) + "\n ]\n}\n");
        }

        // ---------------------------------------------------------------- inventory
        void Inventory()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"camera {cam.name}: ortho {cam.orthographic} size {F(cam.orthographicSize)} fov {F(cam.fieldOfView)} near {F(cam.nearClipPlane)} far {F(cam.farClipPlane)} occlusionCulling {cam.useOcclusionCulling} layerCullDistances>0: {cam.layerCullDistances.Count(x => x > 0)} pos {cam.transform.position} rot {cam.transform.eulerAngles}");
            var ucd = cam.GetComponent<UniversalAdditionalCameraData>();
            if (ucd) sb.AppendLine($"camera data: AA {ucd.antialiasing} renderShadows {ucd.renderShadows} postFX {ucd.renderPostProcessing} depthTex {ucd.requiresDepthTexture}");
            sb.AppendLine($"cameras enabled: {string.Join(", ", FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c => c.enabled && c.gameObject.activeInHierarchy).Select(c => c.name + (c.targetTexture ? "(RT)" : "") + " far " + F(c.farClipPlane)))}");
            if (urp) sb.AppendLine($"urp {urp.name}: shadowDistance {F(urp.shadowDistance)} cascades {urp.shadowCascadeCount} mainShadowRes {urp.mainLightShadowmapResolution} renderScale {F(urp.renderScale)} msaa {urp.msaaSampleCount} hdr {urp.supportsHDR} GRD {urp.gpuResidentDrawerMode} GRDocclusion {urp.gpuResidentDrawerEnableOcclusionCullingInCameras} smallMesh% {F(urp.smallMeshScreenPercentage)} lodCrossFade {urp.enableLODCrossFade} softShadows {urp.supportsSoftShadows}");
            sb.AppendLine($"quality {QualitySettings.names[QualitySettings.GetQualityLevel()]}: lodBias {F(QualitySettings.lodBias)} maxLOD {QualitySettings.maximumLODLevel} shadowDistance(q) {F(QualitySettings.shadowDistance)} vSync {QualitySettings.vSyncCount}");
            sb.AppendLine($"fog {RenderSettings.fog} mode {RenderSettings.fogMode} start {F(RenderSettings.fogStartDistance)} end {F(RenderSettings.fogEndDistance)} density {F(RenderSettings.fogDensity)} colour {RenderSettings.fogColor}");
            sb.AppendLine($"sun {(sun ? sun.name : "none")} shadows {(sun ? sun.shadows.ToString() : "-")}; lights with shadows: {FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l => l.enabled && l.shadows != LightShadows.None)}; lights: {FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l => l.enabled)}");
            Refresh();
            long tris = 0, trisLod0 = 0; int inLg = 0;
            foreach (var x in all) { tris += x.tris; if (x.lg >= 0) inLg++; if (x.lg < 0 || x.lod == 0) trisLod0 += x.tris; }
            sb.AppendLine($"renderers (mesh+skinned, active+enabled): {all.Count}, skinned {all.Count(x => x.skinned)}, in LODGroups {inLg}, static-batched {all.Count(x => x.staticBatch)} (in LODGroups {all.Count(x => x.staticBatch && x.lg >= 0)}); triangles all levels {tris:N0}, at LOD0/no-LOD {trisLod0:N0}");
            int never = 0, culls = 0; var levels = new Dictionary<int, int>();
            foreach (var g in groups) { int n = g.lods.Length; levels[n] = levels.TryGetValue(n, out var c) ? c + 1 : 1; if (g.lods[n - 1].screenRelativeTransitionHeight <= 0.0001f) never++; else culls++; }
            sb.AppendLine($"LODGroups {groups.Count} (enabled {groups.Count(g => g.g.enabled)}): levels {string.Join(", ", levels.OrderBy(k => k.Key).Select(k => k.Key + "→" + k.Value))}; last level never culls {never}, culls {culls}");
            var lastH = groups.Select(g => g.lods[g.lods.Length - 1].screenRelativeTransitionHeight).OrderBy(v => v).ToArray();
            if (lastH.Length > 0) sb.AppendLine($"last-LOD cull heights: min {F(lastH[0])} median {F(lastH[lastH.Length / 2])} max {F(lastH[lastH.Length - 1])}");
            // by category, LOD coverage
            foreach (var cat in all.GroupBy(x => x.cat).OrderByDescending(gg => gg.Sum(x => (long)x.tris)).Take(30))
                sb.AppendLine($"  cat {cat.Key}: renderers {cat.Count()}, inLOD {cat.Count(x => x.lg >= 0)}, static {cat.Count(x => x.staticBatch)}, tris {cat.Sum(x => (long)x.tris):N0}, casters {cat.Count(x => x.r.shadowCastingMode != ShadowCastingMode.Off)}");
            foreach (var m in all.Where(x => x.lg < 0).GroupBy(x => x.mesh).OrderByDescending(gg => gg.Sum(x => (long)x.tris)).Take(25))
                sb.AppendLine($"  no-LOD mesh {m.Key}: n {m.Count()} × {m.First().tris} = {m.Sum(x => (long)x.tris):N0}");
            File.WriteAllText(Path.Combine(dir, "inventory.txt"), sb.ToString());
            L("inventory written");
        }

        // ---------------------------------------------------------------- the scene's renderers and LOD groups
        sealed class R { public Renderer r; public int tris, lg = -1, lod = -1; public string cat = "", mesh = ""; public bool skinned, staticBatch; }
        sealed class G { public LODGroup g; public LOD[] lods; public int active; }
        readonly List<R> all = new(); readonly List<G> groups = new();
        readonly Dictionary<Mesh, int> meshTris = new();

        int Tris(Mesh m)
        {
            if (!m) return 0;
            if (meshTris.TryGetValue(m, out var t)) return t;
            long n = 0; for (int i = 0; i < m.subMeshCount; i++) if (m.GetTopology(i) == MeshTopology.Triangles) n += m.GetIndexCount(i) / 3;
            meshTris[m] = (int)n; return (int)n;
        }

        static string Cat(Transform t)
        {
            var path = new List<string>(); for (var p = t; p != null; p = p.parent) path.Add(p.name);
            path.Reverse();
            string Clean(string s) { int k = s.IndexOfAny(new[] { '/', ' ', '(' , '#'}); var c = k > 0 ? s.Substring(0, k) : s; return c.TrimEnd('0','1','2','3','4','5','6','7','8','9','_','-'); }
            return path.Count == 1 ? Clean(path[0]) : path.Count == 2 ? Clean(path[0]) + "/" + Clean(path[1]) : Clean(path[0]) + "/" + Clean(path[1]) + "/" + Clean(path[2]);
        }

        void Refresh()
        {
            all.Clear(); groups.Clear();
            var map = new Dictionary<Renderer, (int g, int l)>();
            foreach (var g in FindObjectsByType<LODGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var lods = g.GetLODs(); if (lods.Length == 0) continue;
                int gi = groups.Count; groups.Add(new G { g = g, lods = lods });
                for (int i = 0; i < lods.Length; i++) foreach (var r in lods[i].renderers) if (r) map[r] = (gi, i);
            }
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!r.enabled) continue;
                Mesh m = null; bool sk = false;
                if (r is MeshRenderer) { var mf = r.GetComponent<MeshFilter>(); m = mf ? mf.sharedMesh : null; }
                else if (r is SkinnedMeshRenderer smr) { m = smr.sharedMesh; sk = true; }
                else continue;
                int tris = Tris(m); bool sb = r.isPartOfStaticBatch;
                if (sb && m && r is MeshRenderer mr) { long tt = 0; int s0 = mr.subMeshStartIndex, nm = r.sharedMaterials.Length; for (int i = s0; i < Mathf.Min(m.subMeshCount, s0 + nm); i++) tt += m.GetIndexCount(i) / 3; tris = (int)tt; }
                var x = new R { r = r, tris = tris, mesh = m ? (sb ? "static:" + r.gameObject.name : m.name) : "(none)", skinned = sk, cat = Cat(r.transform), staticBatch = sb };
                if (map.TryGetValue(r, out var gl)) { x.lg = gl.g; x.lod = gl.l; }
                all.Add(x);
            }
        }

        // ---------------------------------------------------------------- census: what this camera draws, LOD-aware, by distance
        static readonly float[] Bins = { 50, 150, 300, 600, float.MaxValue };
        string Census(string name, bool detail)
        {
            Refresh();
            var planes = GeometryUtility.CalculateFrustumPlanes(cam); var cp = cam.transform.position;
            float tanHalf = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad), bias = QualitySettings.lodBias;
            foreach (var g in groups)
            {
                if (!g.g.enabled) { g.active = 0; continue; }
                var t = g.g.transform; var refp = t.TransformPoint(g.g.localReferencePoint); var ls = t.lossyScale;
                float size = g.g.size * Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
                float rel = cam.orthographic ? size * 0.5f / cam.orthographicSize : size * 0.5f / (Mathf.Max(0.001f, Vector3.Distance(refp, cp)) * tanHalf);
                rel *= bias; g.active = -1;
                for (int i = 0; i < g.lods.Length; i++) if (rel >= g.lods[i].screenRelativeTransitionHeight) { g.active = i; break; }
            }
            var n = new int[Bins.Length]; var tr = new long[Bins.Length]; int casters = 0, isVis = 0, lodCulled = 0, lodLower = 0; long casterTris = 0;
            var farMesh = new Dictionary<string, (int n, long t)>(); var farCat = new Dictionary<string, (int n, long t)>(); var allCat = new Dictionary<string, (int n, long t)>();
            float sd = urp ? urp.shadowDistance : 0f;
            foreach (var x in all)
            {
                var r = x.r; if (!r || !r.gameObject.activeInHierarchy || r.forceRenderingOff || (cam.cullingMask & (1 << r.gameObject.layer)) == 0) continue;
                if (r.isVisible) isVis++;
                if (x.lg >= 0) { int a = groups[x.lg].active; if (a < 0) { lodCulled++; continue; } if (a != x.lod) continue; if (a > 0) lodLower++; }
                var b = r.bounds; float d = Mathf.Sqrt(b.SqrDistance(cp));
                if (r.shadowCastingMode != ShadowCastingMode.Off && d < sd) { casters++; casterTris += x.tris; }
                if (!GeometryUtility.TestPlanesAABB(planes, b)) continue;
                int k = 0; while (d >= Bins[k]) k++;
                n[k]++; tr[k] += x.tris;
                allCat[x.cat] = allCat.TryGetValue(x.cat, out var ac) ? (ac.n + 1, ac.t + x.tris) : (1, x.tris);
                if (d >= 150f)
                {
                    farMesh[x.mesh] = farMesh.TryGetValue(x.mesh, out var fm) ? (fm.n + 1, fm.t + x.tris) : (1, x.tris);
                    farCat[x.cat] = farCat.TryGetValue(x.cat, out var fc) ? (fc.n + 1, fc.t + x.tris) : (1, x.tris);
                }
            }
            string J((int n, long t) v) => $"[{v.n}, {v.t}]";
            var s = new StringBuilder();
            s.Append($"\"census\": {{\"camFar\": {F(cam.farClipPlane)}, \"ortho\": {(cam.orthographic ? "true" : "false")}, \"pos\": [{F(cp.x)}, {F(cp.y)}, {F(cp.z)}], \"yawDeg\": {F(cam.transform.eulerAngles.y)}, \"pitchDeg\": {F(cam.transform.eulerAngles.x)}, ");
            s.Append($"\"bins\": [\"<50\", \"50-150\", \"150-300\", \"300-600\", \">600\"], \"renderers\": [{string.Join(", ", n)}], \"tris\": [{string.Join(", ", tr)}], ");
            s.Append($"\"drawn\": {n.Sum()}, \"drawnTris\": {tr.Sum()}, \"beyond150\": {n[2] + n[3] + n[4]}, \"beyond150Tris\": {tr[2] + tr[3] + tr[4]}, \"lodCulledRenderers\": {lodCulled}, \"atLowerLod\": {lodLower}, \"unityIsVisible\": {isVis}, ");
            s.Append($"\"shadowDistance\": {F(sd)}, \"castersInShadowDistance\": {casters}, \"casterTris\": {casterTris}");
            if (detail)
            {
                s.Append(", \"farCats\": {" + string.Join(", ", farCat.OrderByDescending(k => k.Value.t).Take(15).Select(k => $"\"{k.Key}\": {J(k.Value)}")) + "}");
                s.Append(", \"farMeshes\": {" + string.Join(", ", farMesh.OrderByDescending(k => k.Value.t).Take(15).Select(k => $"\"{k.Key}\": {J(k.Value)}")) + "}");
                s.Append(", \"cats\": {" + string.Join(", ", allCat.OrderByDescending(k => k.Value.t).Take(15).Select(k => $"\"{k.Key}\": {J(k.Value)}")) + "}");
            }
            s.Append("}");
            L($"{name}: drawn {n.Sum()} ({tr.Sum():N0} tris), beyond 150 m {n[2] + n[3] + n[4]} ({tr[2] + tr[3] + tr[4]:N0} tris), casters {casters}");
            return s.ToString();
        }

        // ---------------------------------------------------------------- timing + counters
        IEnumerator Time_(string name, string census, float seconds)
        {
            var recs = new List<(string n, ProfilerRecorder r)>(); var hs = new List<ProfilerRecorderHandle>(); ProfilerRecorderHandle.GetAvailable(hs);
            foreach (var hh in hs)
            {
                var d = ProfilerRecorderHandle.GetDescription(hh);
                if (d.Category.Name != ProfilerCategory.Render.Name) continue;
                if (d.UnitType != ProfilerMarkerDataUnit.Count && d.UnitType != ProfilerMarkerDataUnit.Bytes) continue;
                var r = new ProfilerRecorder(hh, 1, ProfilerRecorderOptions.Default); if (!r.IsRunning) r.Start(); recs.Add((d.Name, r));
            }
            yield return null; yield return null;
            var ms = new List<double>(); var sums = new double[recs.Count]; var maxs = new long[recs.Count];
            double gpu = 0, main = 0, rt = 0; int gn = 0; var ft = new FrameTiming[1];
            double last = Time.realtimeSinceStartupAsDouble, t0 = last;
            while (Time.realtimeSinceStartupAsDouble - t0 < seconds)
            {
                yield return null; double now = Time.realtimeSinceStartupAsDouble; ms.Add((now - last) * 1000.0); last = now;
                FrameTimingManager.CaptureFrameTimings();
                if (FrameTimingManager.GetLatestTimings(1, ft) == 1 && ft[0].gpuFrameTime > 0) { gpu += ft[0].gpuFrameTime; main += ft[0].cpuMainThreadFrameTime; rt += ft[0].cpuRenderThreadFrameTime; gn++; }
                for (int i = 0; i < recs.Count; i++) { long v = recs[i].r.LastValue; sums[i] += v; if (v > maxs[i]) maxs[i] = v; }
            }
            ms.Sort(); double mean = ms.Average();
            var ctr = new List<string>();
            for (int i = 0; i < recs.Count; i++) { if (sums[i] > 0) ctr.Add($"\"{recs[i].n}\": {F(sums[i] / ms.Count)}"); recs[i].r.Dispose(); }
            string row = $"{{\"name\": \"{name}\", \"frames\": {ms.Count}, \"meanMs\": {F(mean)}, \"fps\": {F(1000 / mean)}, \"p50Ms\": {F(ms[ms.Count / 2])}, \"p95Ms\": {F(ms[(int)(ms.Count * 0.95)])}, \"p99Ms\": {F(ms[(int)(ms.Count * 0.99)])}, \"maxMs\": {F(ms[ms.Count - 1])}, " +
                $"\"gpuMs\": {F(gn > 0 ? gpu / gn : -1)}, \"cpuMainMs\": {F(gn > 0 ? main / gn : -1)}, \"cpuRenderMs\": {F(gn > 0 ? rt / gn : -1)}, \"counters\": {{{string.Join(", ", ctr)}}}{(census != null ? ", " + census : "")}}}";
            rows.Add(row);
            L($"{name}: {F(mean)} ms mean ({F(1000 / mean)} fps), p99 {F(ms[(int)(ms.Count * 0.99)])}, gpu {F(gn > 0 ? gpu / gn : -1)}, main {F(gn > 0 ? main / gn : -1)}, render {F(gn > 0 ? rt / gn : -1)}");
            Flush();
        }

        IEnumerator Shot(string name, int maxW = 1920)
        {
            yield return new WaitForEndOfFrame();
            var t = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(dir, name + ".jpg"), t.EncodeToJPG(80)); Destroy(t);
        }

        // ---------------------------------------------------------------- views
        Vector3 OpPos(int k) { var t = h.Views.Find("op/" + Suborned.Sim.Op.Name((Suborned.Sim.OpId)k)); return t ? t.position : Vector3.zero; }

        IEnumerator FollowAt(int k, float heading)
        {
            if (h.Following) h.FollowToggle();
            h.Select((Suborned.Sim.OpId)k, false); yield return null;
            h.FollowToggle(); yield return null;
            var p = OpPos(k); var dirv = new Vector3(Mathf.Sin(heading * Mathf.Deg2Rad), 0f, Mathf.Cos(heading * Mathf.Deg2Rad));
            h.FollowFace(p + dirv * 50f);
            yield return new WaitForSecondsRealtime(0.8f);
            h.FollowFace(p + dirv * 50f);
        }

        /// <summary>Walk agent k (paused, the engine stepped) to its start + (dx, dz) world metres; every agent comes along so the follow view has the cell in it.</summary>
        void Walk(Vector2 d)
        {
            if (d == Vector2.zero) return;
            h.SetPaused(true);
            for (int k = 0; k < 4; k++)
            {
                var p = OpPos(k) + new Vector3(d.x, 0f, d.y) + new Vector3(k * 1.2f, 0f, 0f); var pt = h.frame.Engine(p);
                h.Issue(new Suborned.Sim.Order { Op = Suborned.Sim.Op.Name((Suborned.Sim.OpId)k), Do = "move", Point = pt });
            }
            for (int g = 0; g < 2000 && !h.Game.Over && (h.Game.Ops[0].Walk != null || h.Game.Ops[1].Walk != null || h.Game.Ops[2].Walk != null || h.Game.Ops[3].Walk != null); g++) h.RunTo(h.Game.Tick + 1);
            L($"walked {d} to {OpPos(0)} at tick {h.Game.Tick}");
        }

        IEnumerator Candidates()
        {
            h.SetPaused(true); Vector2 at = Vector2.zero;
            foreach (var c in cands.Split(';'))
            {
                var p = c.Split(','); var d = new Vector2(float.Parse(p[0], CultureInfo.InvariantCulture), float.Parse(p[1], CultureInfo.InvariantCulture)); float hd = float.Parse(p[2], CultureInfo.InvariantCulture);
                if (d != at) { if (h.Following) h.FollowToggle(); Walk(d - at); at = d; yield return null; }
                yield return FollowAt(0, hd); yield return null;
                string nm = $"c-{p[0]}_{p[1]}-h{p[2]}";
                rows.Add($"{{\"name\": \"{nm}\", \"walk\": [{p[0]}, {p[1]}], \"heading\": {p[2]}, \"opPos\": [{F(OpPos(0).x)}, {F(OpPos(0).z)}], {Census(nm, false)}}}");
                yield return Shot(nm);
            }
            if (h.Following) h.FollowToggle();
            Flush();
        }

        IEnumerator Discover()
        {
            if (cands.Length > 0) { yield return Candidates(); yield break; }
            h.SetPaused(true);
            rows.Add($"{{\"name\": \"overhead-open\", {Census("overhead-open", true)}}}"); yield return Shot("overhead-open");
            for (int k = 0; k < 4; k++)
            {
                L($"agent {k} at {OpPos(k)}");
                for (int a = 0; a < 8; a++)
                {
                    yield return FollowAt(k, a * 45f); yield return null;
                    string nm = $"tp-a{k}-h{a * 45}";
                    rows.Add($"{{\"name\": \"{nm}\", \"agent\": {k}, \"heading\": {a * 45}, \"opPos\": [{F(OpPos(k).x)}, {F(OpPos(k).z)}], {Census(nm, false)}}}");
                    yield return Shot(nm);
                }
            }
            if (h.Following) h.FollowToggle();
            Flush();
        }

        ScriptableRendererFeature Ssao()
        {
            var rdf = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (rdf?.GetValue(urp) is ScriptableRendererData[] list) foreach (var rd in list) if (rd != null) foreach (var f in rd.rendererFeatures) if (f != null && f.GetType().Name.Contains("AmbientOcclusion")) return f;
            return null;
        }

        IEnumerator Variant(string name, Action on, Action off, bool census = false)
        {
            on?.Invoke(); yield return new WaitForSecondsRealtime(1.5f);
            string c = census ? Census(name, false) : null;
            yield return Time_(name, c, secs);
            off?.Invoke(); yield return null;
        }

        /// <summary>Second set (third person only): GRD GPU occlusion culling, GRD small-mesh culling, a cull level on small LOD groups.</summary>
        IEnumerator Tp2()
        {
            Walk(walk); h.SetPaused(false);
            yield return FollowAt(vAgent, vHeading); yield return new WaitForSecondsRealtime(1.5f);
            yield return Time_("tp", Census("tp", true), secs);
            bool occ = urp.gpuResidentDrawerEnableOcclusionCullingInCameras; float sm = urp.smallMeshScreenPercentage;
            yield return Variant("tp/grd-occlusion", () => urp.gpuResidentDrawerEnableOcclusionCullingInCameras = true, () => urp.gpuResidentDrawerEnableOcclusionCullingInCameras = occ);
            yield return Shot("m-tp-occl");
            yield return Variant("tp/small-mesh-1pct", () => urp.smallMeshScreenPercentage = 1f, () => urp.smallMeshScreenPercentage = sm);
            yield return Variant("tp/small-mesh-3pct", () => urp.smallMeshScreenPercentage = 3f, () => urp.smallMeshScreenPercentage = sm);
            // a cull level: every LOD group smaller than 8 m culls below 1 % of screen height (props, palms, lamps far away); bigger groups below 0.4 %
            var saved = new List<(LODGroup g, LOD[] l)>();
            yield return Variant("tp/lod-cull", () =>
            {
                foreach (var g in FindObjectsByType<LODGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    var l = g.GetLODs(); if (l.Length == 0) continue; saved.Add((g, (LOD[])l.Clone()));
                    var n = (LOD[])l.Clone(); float cut = g.size < 8f ? 0.01f : 0.004f;
                    if (n[n.Length - 1].screenRelativeTransitionHeight < cut) { n[n.Length - 1].screenRelativeTransitionHeight = cut; for (int i = n.Length - 2; i >= 0; i--) n[i].screenRelativeTransitionHeight = Mathf.Max(n[i].screenRelativeTransitionHeight, n[i + 1].screenRelativeTransitionHeight + 0.001f); g.SetLODs(n); }
                }
            }, null, true);
            yield return Shot("m-tp-lodcull");
            yield return Variant("tp/lod-cull+occlusion+far300", () => { urp.gpuResidentDrawerEnableOcclusionCullingInCameras = true; cam.farClipPlane = 300f; }, () => { urp.gpuResidentDrawerEnableOcclusionCullingInCameras = occ; cam.farClipPlane = 600f; }, true);
            foreach (var (g, l) in saved) if (g) g.SetLODs(l);
            yield return Variant("tp/restored", null, null, true);
        }

        /// <summary>Third set: far clips around Glint's haze far clip (450 m), alone and with the third-person shadow fit (80 m, 2 cascades).</summary>
        IEnumerator Tp3()
        {
            Walk(walk); h.SetPaused(false);
            yield return FollowAt(vAgent, vHeading); yield return new WaitForSecondsRealtime(1.5f);
            float far = cam.farClipPlane, sd = urp.shadowDistance, c2 = urp.cascade2Split; int cc = urp.shadowCascadeCount;
            yield return Time_("tp", Census("tp", true), secs);
            foreach (var f in new[] { 450f, 350f, 250f })
                yield return Variant($"tp/far{f}", () => cam.farClipPlane = f, () => cam.farClipPlane = far, true);
            foreach (var f in new[] { 450f, 300f })
                yield return Variant($"tp/far{f}+shadow80-2c", () => { cam.farClipPlane = f; urp.shadowDistance = 80f; urp.shadowCascadeCount = 2; urp.cascade2Split = 0.3f; }, () => { cam.farClipPlane = far; urp.shadowDistance = sd; urp.shadowCascadeCount = cc; urp.cascade2Split = c2; }, true);
            yield return Variant("tp/again", null, null, true);
        }

        IEnumerator Measure()
        {
            var ssao = Ssao(); var ucd = cam.GetComponent<UniversalAdditionalCameraData>();
            if (only == "tp2") { yield return Tp2(); yield break; }
            if (only == "tp3") { yield return Tp3(); yield break; }
            // the overhead play view at the opening (play zoom), the clock running
            if (h.Following) h.FollowToggle();
            h.SetPaused(false); yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("m-overhead-open");
            yield return Time_("overhead-open", Census("overhead-open", true), secs);
            yield return Variant("overhead-open/no-sun-shadows", () => sun.shadows = LightShadows.None, () => sun.shadows = LightShadows.Soft);
            yield return Variant("overhead-open/no-ssao", () => ssao?.SetActive(false), () => ssao?.SetActive(true));
            float sb0 = QualitySettings.lodBias;
            yield return Variant("overhead-open/lodBias0.5", () => QualitySettings.lodBias = sb0 * 0.5f, () => QualitySettings.lodBias = sb0, true);
            for (int k = 0; k < 4; k++)
            {
                h.TurnSnap(k); yield return new WaitForSecondsRealtime(1.5f);
                yield return Time_($"overhead-yaw{F(h.Yaw)}", Census($"overhead-yaw{k}", false), secs);
            }
            h.TurnSnap(0);
            // the third-person (follow) view at the chosen heading
            Walk(walk); h.SetPaused(false);
            yield return FollowAt(vAgent, vHeading); yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("m-tp");
            var lightShadows = sun.shadows; float sd = urp.shadowDistance; int cc = urp.shadowCascadeCount; float c2 = urp.cascade2Split; float far = cam.farClipPlane;
            yield return Time_("tp", Census("tp", true), secs);
            yield return Variant("tp/far300", () => cam.farClipPlane = 300f, () => cam.farClipPlane = far, true);
            yield return Variant("tp/far150", () => cam.farClipPlane = 150f, () => cam.farClipPlane = far, true);
            yield return Variant("tp/shadow80-2c", () => { urp.shadowDistance = 80f; urp.shadowCascadeCount = 2; urp.cascade2Split = 0.3f; }, () => { urp.shadowDistance = sd; urp.shadowCascadeCount = cc; urp.cascade2Split = c2; }, true);
            yield return Variant("tp/no-sun-shadows", () => sun.shadows = LightShadows.None, () => sun.shadows = lightShadows);
            yield return Variant("tp/lodBias0.5", () => QualitySettings.lodBias = sb0 * 0.5f, () => QualitySettings.lodBias = sb0, true);
            yield return Variant("tp/lodBias0.25", () => QualitySettings.lodBias = sb0 * 0.25f, () => QualitySettings.lodBias = sb0, true);
            yield return Variant("tp/no-ssao", () => ssao?.SetActive(false), () => ssao?.SetActive(true));
            yield return Variant("tp/combo-far250-shadow80-lod0.5", () => { cam.farClipPlane = 250f; urp.shadowDistance = 80f; urp.shadowCascadeCount = 2; urp.cascade2Split = 0.3f; QualitySettings.lodBias = sb0 * 0.5f; },
                () => { cam.farClipPlane = far; urp.shadowDistance = sd; urp.shadowCascadeCount = cc; urp.cascade2Split = c2; QualitySettings.lodBias = sb0; }, true);
            yield return Shot("m-tp-after");
            // the drawer off last (it rebuilds its state)
            var grd = urp.gpuResidentDrawerMode;
            yield return Variant("tp/grd-off", () => urp.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled, null);
            if (h.Following) h.FollowToggle(); yield return new WaitForSecondsRealtime(1.5f);
            yield return Time_("overhead/grd-off", null, secs);
            urp.gpuResidentDrawerMode = grd;
        }
    }
}
