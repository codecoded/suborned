using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Review
{
    /// <summary>Brief 072-E (Latch, 2026-10-27): the every-face pass's before/after capture, kept so phase 2 reruns exactly the same views.
    /// In the Editor (not batchmode: the counts are the Game view's UnityStats), on the open HarbourDistrict scene, outside Play Mode.
    /// The scene's own cameras/play camera is posed the way HandoverHost.PlaceCamera poses it (orthographic size 14, pitch 38, the view's
    /// yaw, standing back the host's standOff along its view from a street focus, far = standOff + 1500, TacticalShadows.focusDepth =
    /// standOff, ArchitectureFade.focus = the focus), the Game view is set to a fixed 1920 x 1080, the editor snapshot crowd (`people`) is
    /// hidden as the host hides it, and for each view:
    ///  · counts: UnityStats after the Game view's own render (triangles, vertices, SetPass, draw calls, batches, shadow casters), mean of
    ///    12 repaints after 6 warm ones, with the GPU Resident Drawer as the asset has it;
    ///  · a census of what the camera can draw: renderers inside the 16:9 frustum after an LODGroup pick (ortho: world size / (2 x ortho
    ///    size) x lodBias against each LOD's transition height; an estimate), their triangles (LOD-picked index counts) and the share by
    ///    layer (HarbourDistrict/&lt;layer&gt; or batches/&lt;group&gt;), and how many cast shadows;
    ///  · editor frame ms: the camera rendered by hand at 1920 x 1080 into Capture's HDR target, the GPU waited on after each render
    ///    (1 px read-back), 8 warm then median and p90 of 30. Studio editor, indicative only (the Air is the reference, GDD §11).
    /// Shots (Capture's honest path through RenderSession, as Baseline): every count view, plus one perspective framing of the sky planets
    /// (Horizon_PlanetRinged, Horizon_Moon) from above the Records square. Everything posed is put back; nothing is saved.
    /// Views: (a) office-front = brief 026's pinned stop (plan -160, -281: the Records Office's public west front, the café north of it;
    /// yaw 210 looks at its east face and the service yard); (b) cell-open = HandoverHost.OpeningFocus through the scene host's frame
    /// (falls back to -170.5, -227.5). Yaws 30, 120, 210, 300.
    /// Menus: Suborned/Review/072 Capture (before) → briefs/072/latch/baseline + briefs/072/shots/before;
    ///        Suborned/Review/072 Capture (after)  → briefs/072/latch/after    + briefs/072/shots/after.</summary>
    public static class Baseline072
    {
        internal const int W = 1920, H = 1080; const int WarmRepaints = 6, CountRepaints = 12, WarmFrames = 8, TimedFrames = 30;
        internal const float PlayZoom = 14f, PlayPitch = 38f, DefaultStandOff = 160f;
        internal static readonly float[] Yaws = { 30f, 120f, 210f, 300f };
        internal static readonly Vector2 OfficeFront = new(-160f, -281f), CellOpenFallback = new(-170.5f, -227.5f);

        [MenuItem("Suborned/Review/072 Capture (before)")]
        static void Before() => Start("baseline", "before");

        [MenuItem("Suborned/Review/072 Capture (after)")]
        static void After() => Start("after", "after");

        /// <summary>Brief 072-E phase 2: the same views with the GPU Resident Drawer on (the asset's own mode, in memory only; EditorDrawerGuard
        /// takes it off again after). Counts and frame ms only (the shots are the drawer-off pass's); → briefs/072/latch/after/grd-on.</summary>
        [MenuItem("Suborned/Review/072 Capture (after, GPU Resident Drawer on)")]
        static void AfterGrd() => Start(Path.Combine("after", "grd-on"), null, grd: true);

        /// <summary>Both after passes in a fresh Editor, then quit (exit 0 done, 1 failed). Not batchmode (the counts are the Game view's UnityStats):
        ///   Unity -projectPath unity -executeMethod Suborned.World.Review.Baseline072.AfterCli [-grd off|on|both] [-tag textured] -logFile &lt;log&gt;</summary>
        public static void AfterCli()
        {
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-grd"); string grd = i >= 0 && i + 1 < a.Length ? a[i + 1] : "both";
            exitWhenDone = true;
            // 072-F: `-tag textured` writes to briefs/072/latch/textured/unitystats (and shots to briefs/072/shots/textured-gameview)
            int ti = Array.IndexOf(a, "-tag"); string tag = ti >= 0 && ti + 1 < a.Length ? a[ti + 1] : null;
            string dir = tag == null ? "after" : Path.Combine(tag, "unitystats"), shots = tag == null ? "after" : tag + "-gameview";
            if (grd != "on") queue.Enqueue(() => Start(dir, shots));
            if (grd != "off") queue.Enqueue(() => Start(Path.Combine(dir, "grd-on"), null, grd: true));
            EditorApplication.delayCall += () => EditorApplication.delayCall += Next; // after the scene and the Game view are up
        }

        static readonly Queue<Action> queue = new();
        static bool exitWhenDone, failed;
        static void Next()
        {
            if (queue.Count > 0) { queue.Dequeue()(); if (run == null) { failed = true; Next(); } return; }
            if (exitWhenDone) { Debug.Log($"072: AfterCli {(failed ? "FAILED" : "done")}"); EditorApplication.Exit(failed ? 1 : 0); }
        }

        static IEnumerator<object> run;

        static void Start(string countsDir, string shotsDir, bool grd = false)
        {
            if (Application.isBatchMode) { Debug.LogError("072: the capture reads the Game view's stats; run it in the Editor, not batchmode"); return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("072: leave Play Mode first"); return; }
            if (run != null) { Debug.LogError("072: a capture is already running"); return; }
            if (!RenderSession.EnsureScene()) return;
            var root = HarbourWorldBuilder.RepoRoot;
            run = Body(Path.Combine(root, "briefs", "072", "latch", countsDir), shotsDir == null ? null : Path.Combine(root, "briefs", "072", "shots", shotsDir), shotsDir ?? "after-grd-on", grd);
            EditorApplication.update += Tick;
            Debug.Log($"072: capture started (Game view at 1920x1080, GPU Resident Drawer {(grd ? "ON" : "off")}; keep the Editor open until `072: done`)");
        }

        static void Tick()
        {
            try { if (run == null || !run.MoveNext()) Stop(); }
            catch (Exception e) { Debug.LogException(e); failed = true; Stop(); }
        }

        static void Stop() { EditorApplication.update -= Tick; run = null; if (queue.Count > 0 || exitWhenDone) EditorApplication.delayCall += Next; }

        internal sealed class View
        {
            public string Id, Label; public Vector2 Focus; public float Yaw;
            public Dictionary<string, double> Stats = new();
            public int Visible, VisibleCasters, LodCulled; public long VisibleTris;
            public int ModColumns, ModModules, ModProxies; public long ModTris, ModProxyTris; // brief 072-E: every-face modules in the frustum (columns = placed instances)
            public Dictionary<string, (int n, long tris)> ByLayer = new();
            public double Median, P90;
            public string Shot;
        }

        static IEnumerator<object> Body(string countsDir, string shotsDir, string phase, bool grd = false)
        {
            Directory.CreateDirectory(countsDir); if (shotsDir != null) Directory.CreateDirectory(shotsDir);
            var playGo = GameObject.Find("cameras/play") ?? throw new InvalidOperationException("072: no cameras/play in the scene");
            var cam = playGo.GetComponent<Camera>();
            var shadows = playGo.GetComponent<TacticalShadows>(); var fade = playGo.GetComponent<ArchitectureFade>();
            var host = UnityEngine.Object.FindAnyObjectByType<global::Suborned.Play.HandoverHost>(FindObjectsInactive.Include);
            float standOff = host ? host.standOff : DefaultStandOff;
            Vector2 cellOpen = CellOpenFallback; string cellFrom = "fallback (HostLess.OpeningFocus comment)";
            try { if (host != null) { var w = host.frame.World(global::Suborned.Play.HandoverHost.OpeningFocus); if (!float.IsNaN(w.x) && (w.x != 0 || w.z != 0)) { cellOpen = new Vector2(w.x, w.z); cellFrom = "HandoverHost.frame.World(OpeningFocus)"; } } }
            catch (Exception e) { Debug.LogWarning($"072: the host frame could not place OpeningFocus ({e.Message}); using {CellOpenFallback}"); }

            var views = new List<View>();
            foreach (var (id, label, f) in new[] { ("office-front", "Records Office front (brief 026 stop; café north)", OfficeFront), ("cell-open", "the cell's opening view (" + cellFrom + ")", cellOpen) })
                foreach (var y in Yaws) views.Add(new View { Id = id, Label = label, Focus = f, Yaw = y });

            // what we change, to put back
            var saved = (pos: cam.transform.position, rot: cam.transform.rotation, ortho: cam.orthographic, size: cam.orthographicSize, far: cam.farClipPlane, near: cam.nearClipPlane,
                         depth: shadows ? shadows.focusDepth : 0f, focus: fade ? fade.focus : Vector3.zero, target: cam.targetTexture, lodBias: QualitySettings.lodBias);
            var people = GameObject.Find("people"); bool peopleWas = people && people.activeSelf;
            var otherCams = Camera.allCameras.Where(c => c != cam && c.enabled && c.targetTexture == null).ToList();
            var gvType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            var gv = EditorWindow.GetWindow(gvType, false, "Game", false);
            var repaint = gvType.GetMethod("RepaintImmediately", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            string sizeNote = SetGameViewSize(gv, gvType);
            int seenW = 0, seenH = 0;
            void Seen(ScriptableRenderContext _, Camera c) { if (c == cam && c.cameraType == CameraType.Game && c.targetTexture == null) { seenW = c.pixelWidth; seenH = c.pixelHeight; } }
            RenderPipelineManager.beginCameraRendering += Seen;
            var urp = UniversalRenderPipeline.asset;
            var drawerWas = urp ? urp.gpuResidentDrawerMode : GPUResidentDrawerMode.Disabled;
            if (grd && urp)
            {   // the asset's own mode, in memory; EditorDrawerGuard.Off puts the Editor back after (finally below)
                urp.gpuResidentDrawerMode = Play.EditorDrawerGuard.Own == GPUResidentDrawerMode.Disabled ? GPUResidentDrawerMode.InstancedDrawing : Play.EditorDrawerGuard.Own;
                for (int i = 0; i < 30; i++) yield return null; // the drawer registers the scene's renderers over the next frames
            }
            string drawer = urp ? (grd ? $"{urp.gpuResidentDrawerMode} (switched on in memory for this pass; the asset and the player run {Play.EditorDrawerGuard.Own}; EditorDrawerGuard takes it off after)"
                                       : $"{urp.gpuResidentDrawerMode} in the Editor (EditorDrawerGuard keeps it off; the asset and the player run {Play.EditorDrawerGuard.Own})") : "?";
            var meshTris = new Dictionary<Mesh, long[]>();
            string planetShot = null, planetNote = "";
            try
            {
                if (people) people.SetActive(false);
                foreach (var c in otherCams) c.enabled = false; // the Game view draws only the play camera
                for (int i = 0; i < 4; i++) { yield return null; repaint.Invoke(gv, null); }

                foreach (var v in views)
                {
                    Pose(cam, shadows, fade, v.Focus, v.Yaw, standOff);
                    for (int i = 0; i < WarmRepaints; i++) { yield return null; repaint.Invoke(gv, null); }
                    var acc = new Dictionary<string, double>();
                    for (int i = 0; i < CountRepaints; i++)
                    {
                        yield return null; repaint.Invoke(gv, null);
                        foreach (var (k, val) in Stats()) acc[k] = (acc.TryGetValue(k, out var a) ? a : 0) + val;
                    }
                    foreach (var k in acc.Keys.ToList()) v.Stats[k] = acc[k] / CountRepaints;
                    Census(cam, v, meshTris);
                    Debug.Log($"072: {v.Id} yaw {v.Yaw}: tris {S(v, "triangles"):F0}, SetPass {S(v, "setPassCalls"):F0}, draw calls {S(v, "drawCalls"):F0}, casters {S(v, "shadowCasters"):F0}, visible renderers {v.Visible}");
                }

                // editor frame ms, the camera rendered by hand at 1080p (the drawer as the asset has it)
                using (var capture = new Capture())
                {
                    var rt = capture.Target(W, H);
                    var px = new Texture2D(1, 1, TextureFormat.RGBAHalf, false);
                    var times = new List<double>();
                    try
                    {
                        foreach (var v in views)
                        {
                            Pose(cam, shadows, fade, v.Focus, v.Yaw, standOff);
                            cam.targetTexture = rt; cam.ResetAspect();
                            for (int i = 0; i < WarmFrames; i++) Sync(cam, rt, px);
                            times.Clear();
                            for (int i = 0; i < TimedFrames; i++) { var sw = System.Diagnostics.Stopwatch.StartNew(); Sync(cam, rt, px); times.Add(sw.Elapsed.TotalMilliseconds); }
                            cam.targetTexture = saved.target; cam.ResetAspect();
                            times.Sort(); v.Median = times[times.Count / 2]; v.P90 = times[(int)(times.Count * 0.9)];
                            yield return null;
                        }
                    }
                    finally { cam.targetTexture = saved.target; cam.ResetAspect(); UnityEngine.Object.DestroyImmediate(px); }
                }

                // shots (the drawer-off pass only)
                if (shotsDir != null)
                using (var session = new RenderSession())
                {
                    Pose(cam, shadows, fade, views[0].Focus, views[0].Yaw, standOff);
                    session.Grab(cam, 64, 64); // the first render after a load comes out flat
                    foreach (var v in views)
                    {
                        Pose(cam, shadows, fade, v.Focus, v.Yaw, standOff);
                        v.Shot = $"{v.Id}-y{v.Yaw:000}.png";
                        File.WriteAllBytes(Path.Combine(shotsDir, v.Shot), session.Grab(cam, W, H).EncodeToPNG());
                    }
                    // the planets: a perspective view from above the Records square toward both bodies, fitted to them
                    if (PlanetShot(session, cam, shadows, Path.Combine(shotsDir, "planets.png"), out planetNote)) planetShot = "planets.png";
                }
            }
            finally
            {
                RenderPipelineManager.beginCameraRendering -= Seen;
                cam.transform.SetPositionAndRotation(saved.pos, saved.rot); cam.orthographic = saved.ortho; cam.orthographicSize = saved.size;
                cam.farClipPlane = saved.far; cam.nearClipPlane = saved.near; cam.targetTexture = saved.target; cam.ResetAspect();
                if (shadows) shadows.focusDepth = saved.depth; if (fade) fade.focus = saved.focus;
                QualitySettings.lodBias = saved.lodBias;
                if (people) people.SetActive(peopleWas);
                foreach (var c in otherCams) if (c) c.enabled = true;
                if (grd && urp) { urp.gpuResidentDrawerMode = drawerWas; Play.EditorDrawerGuard.Off(); }
            }

            var stamp = UnityEngine.Object.FindAnyObjectByType<WorldStamp>();
            if (shotsDir == null) shotsDir = Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs", "072", "shots", "after");
            var json = Json(views, phase, stamp, standOff, drawer, sizeNote, seenW, seenH, planetShot, planetNote, shotsDir);
            File.WriteAllText(Path.Combine(countsDir, "counts.json"), json);
            File.WriteAllText(Path.Combine(countsDir, "counts.md"), Table(views, phase, stamp, drawer, seenW, seenH));
            Debug.Log($"072: done: {views.Count} views → {countsDir} (counts.json, counts.md); shots → {shotsDir}");
        }

        /// <summary>The planets framing: a perspective view from above the Records square toward the planet and moons, fitted to
        /// them. The camera is left posed; the caller puts it back.</summary>
        internal static bool PlanetShot(RenderSession session, Camera cam, TacticalShadows shadows, string file, out string note)
        {
            var bodies = Planets(out note);
            if (bodies.Count == 0) return false;
            var eye = new Vector3(OfficeFront.x, 40f, OfficeFront.y);
            var dirs = bodies.Select(b => (b.at - eye).normalized).ToList();
            var mid = dirs.Aggregate(Vector3.zero, (s, d) => s + d).normalized;
            float half = dirs.Max(d => Vector3.Angle(mid, d));
            cam.orthographic = false; cam.fieldOfView = Mathf.Clamp(Camera.HorizontalToVerticalFieldOfView(2f * half + 24f, (float)W / H), 35f, 75f);
            cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(mid, Vector3.up));
            cam.nearClipPlane = 0.5f; cam.farClipPlane = bodies.Max(b => Vector3.Distance(eye, b.at)) * 1.5f;
            if (shadows) shadows.focusDepth = 200f;
            File.WriteAllBytes(file, session.Grab(cam, W, H).EncodeToPNG());
            note += $"; eye {F(eye.x)},{F(eye.y)},{F(eye.z)} looking {F(mid.x)},{F(mid.y)},{F(mid.z)}, vfov {F(cam.fieldOfView)}";
            return true;
        }

        /// <summary>Only the planets framing, in batchmode (072 issue 1, the sky bodies' maps):
        ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Review.Baseline072.PlanetsCli -out &lt;png&gt;
        /// on the built HarbourDistrict scene; nothing is saved.</summary>
        public static void PlanetsCli()
        {
            var file = HarbourWorldBuilder.Arg("-out", Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs", "072", "shots", "before", "planets-fixed.png"));
            if (!Path.IsPathRooted(file)) file = Path.Combine(HarbourWorldBuilder.RepoRoot, file);
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, OpenSceneMode.Single);
            var cam = (GameObject.Find("cameras/play") ?? throw new InvalidOperationException("072: no cameras/play in the scene")).GetComponent<Camera>();
            var shadows = cam.GetComponent<TacticalShadows>();
            var people = GameObject.Find("people"); if (people) people.SetActive(false);
            using var session = new RenderSession();
            Pose(cam, shadows, cam.GetComponent<ArchitectureFade>(), OfficeFront, 30f, DefaultStandOff);
            session.Grab(cam, 64, 64); // the first render after a load comes out flat
            if (!PlanetShot(session, cam, shadows, file, out var note)) throw new InvalidOperationException("072: " + note);
            Debug.Log($"072: planets → {file} ({note})");
        }

        /// <summary>HandoverHost.PlaceCamera at the overhead preset: ortho 14, pitch 38, the yaw; focus on the street datum.</summary>
        internal static void Pose(Camera cam, TacticalShadows shadows, ArchitectureFade fade, Vector2 f, float yaw, float standOff, float zoom = PlayZoom)
        {
            var focus = new Vector3(f.x, 0f, f.y);
            cam.orthographic = true; cam.orthographicSize = zoom;
            cam.transform.rotation = Quaternion.Euler(PlayPitch, yaw, 0f);
            cam.transform.position = focus - cam.transform.forward * standOff;
            cam.nearClipPlane = 0.3f; cam.farClipPlane = Mathf.Max(standOff + 1500f, 0f);
            if (shadows) shadows.focusDepth = standOff;
            if (fade) fade.focus = focus;
        }

        /// <summary>Every numeric static property UnityStats has in this Unity (the set differs between versions); times in ms.</summary>
        static IEnumerable<(string, double)> Stats()
        {
            foreach (var p in typeof(UnityStats).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                object o; try { o = p.GetValue(null); } catch { continue; }
                switch (o)
                {
                    case int x: yield return (p.Name, x); break;
                    case long x: yield return (p.Name, x); break;
                    case float x: yield return (p.Name.EndsWith("Time") ? p.Name + "Ms" : p.Name, p.Name.EndsWith("Time") ? x * 1000.0 : x); break;
                }
            }
        }

        static double S(View v, string k) => v.Stats.TryGetValue(k, out var x) ? x : double.NaN;

        static void Sync(Camera cam, RenderTexture rt, Texture2D px)
        {
            cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            px.ReadPixels(new Rect(0, 0, 1, 1), 0, 0); // waits for the GPU
            RenderTexture.active = prev;
        }

        /// <summary>Renderers the 16:9 frustum holds after an LOD pick (an estimate of what the camera draws), their triangles, by layer.</summary>
        internal static void Census(Camera cam, View v, Dictionary<Mesh, long[]> meshTris, List<Renderer> visibleOut = null, HashSet<Renderer> lodOut = null)
        {
            cam.aspect = (float)W / H;
            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            cam.ResetAspect();
            // brief 072-E: the every-face modules in the frustum (EveryFace.modules: a baked column stands for the n modules its name carries, Column_<n>_<hash>)
            var ef = UnityEngine.Object.FindAnyObjectByType<EveryFace>(FindObjectsInactive.Include);
            if (ef) foreach (var m in ef.modules)
            {
                if (!m || !m.activeInHierarchy) continue;
                var lod0 = KitLods.Lod0(m).ToList(); if (lod0.Count == 0) continue;
                var b = lod0[0].bounds; foreach (var r in lod0) b.Encapsulate(r.bounds);
                if (!GeometryUtility.TestPlanesAABB(planes, b)) continue;
                var mm = Regex.Match(m.name, @"^Column_(\d+)_");
                v.ModColumns++; v.ModModules += mm.Success ? int.Parse(mm.Groups[1].Value) : 1;
                foreach (var r in lod0) { var mesh = r.GetComponent<MeshFilter>().sharedMesh; for (int i = 0; i < mesh.subMeshCount; i++) v.ModTris += mesh.GetIndexCount(i) / 3; }
            }
            foreach (var sp in UnityEngine.Object.FindObjectsByType<ShadowProxy>(FindObjectsInactive.Exclude))
            {
                if (!sp.module || !sp.On || !sp.proxy) continue;
                v.ModProxies++; var mesh = sp.proxy.GetComponent<MeshFilter>().sharedMesh; for (int i = 0; i < mesh.subMeshCount; i++) v.ModProxyTris += mesh.GetIndexCount(i) / 3;
            }
            var pickedOut = new HashSet<Renderer>(); var lodOf = new Dictionary<Renderer, int>();
            foreach (var g in UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Exclude))
            {
                if (!g.enabled) continue;
                var lods = g.GetLODs(); if (lods.Length == 0) continue;
                var s = g.transform.lossyScale; float size = g.size * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                // brief 075ab: a perspective camera (golden, street) picks by distance to the group's reference point, as Unity does
                float rel = (cam.orthographic ? size / (2f * cam.orthographicSize)
                    : size / (2f * Mathf.Max(0.01f, Vector3.Distance(cam.transform.position, g.transform.TransformPoint(g.localReferencePoint))) * Mathf.Tan(0.5f * cam.fieldOfView * Mathf.Deg2Rad))) * QualitySettings.lodBias;
                int pick = -1; for (int i = 0; i < lods.Length; i++) if (rel >= lods[i].screenRelativeTransitionHeight) { pick = i; break; }
                for (int i = 0; i < lods.Length; i++) foreach (var r in lods[i].renderers) if (r) { if (i != pick) pickedOut.Add(r); else { pickedOut.Remove(r); lodOf[r] = i; } }
            }
            if (lodOut != null) foreach (var r in pickedOut) if (!lodOf.ContainsKey(r)) lodOut.Add(r); // brief 072-E after: the renderers an LOD pick leaves out (the shadow census skips them too)
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
            {
                if (!r.enabled || r.forceRenderingOff || (cam.cullingMask & (1 << r.gameObject.layer)) == 0) continue;
                if (r is not MeshRenderer && r is not SkinnedMeshRenderer) continue;
                if (!GeometryUtility.TestPlanesAABB(planes, r.bounds)) continue;
                if (pickedOut.Contains(r) && !lodOf.ContainsKey(r)) { v.LodCulled++; continue; }
                var mesh = r is SkinnedMeshRenderer sk ? sk.sharedMesh : r.TryGetComponent<MeshFilter>(out var mf) ? mf.sharedMesh : null;
                long tris = 0;
                if (mesh)
                {
                    if (!meshTris.TryGetValue(mesh, out var t))
                    {
                        t = new long[mesh.subMeshCount];
                        for (int i = 0; i < mesh.subMeshCount; i++) t[i] = mesh.GetTopology(i) == MeshTopology.Triangles ? (long)mesh.GetIndexCount(i) / 3 : 0;
                        meshTris[mesh] = t;
                    }
                    foreach (var x in t) tris += x;
                }
                v.Visible++; v.VisibleTris += tris; visibleOut?.Add(r);
                if (r.shadowCastingMode != ShadowCastingMode.Off) v.VisibleCasters++;
                var key = Layer(r.transform);
                v.ByLayer[key] = v.ByLayer.TryGetValue(key, out var e) ? (e.n + 1, e.tris + tris) : (1, tris);
            }
        }

        /// <summary>HarbourDistrict/&lt;layer&gt;, batches/&lt;group&gt;, or the root's name.</summary>
        static string Layer(Transform t)
        {
            var chain = new List<string>();
            for (var x = t; x; x = x.parent) chain.Add(x.name);
            chain.Reverse();
            return chain.Count >= 2 ? chain[0] + "/" + Regex.Replace(chain[1], @"\s*\(\d+\)$", "") : chain[0];
        }

        /// <summary>The planet and moons: placed renderers by name if the scene keeps them, else the export's horizon pieces (at, base).</summary>
        static List<(string name, Vector3 at)> Planets(out string note)
        {
            var found = new List<(string, Vector3)>();
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
                for (var x = r.transform; x; x = x.parent)
                    if (x.name.StartsWith("Horizon_PlanetRinged") || x.name.StartsWith("Horizon_Moon") || x.name.StartsWith("horizon/planet") || x.name.StartsWith("horizon/moon"))
                    { found.Add((x.name, r.bounds.center)); break; }
            if (found.Count > 0)
            {
                var byName = found.GroupBy(f => f.Item1).Select(g => (g.Key, g.Select(p => p.Item2).Aggregate(Vector3.zero, (s, p) => s + p) / g.Count())).ToList();
                note = "from the scene's renderers: " + string.Join(", ", byName.Select(b => b.Key));
                return byName;
            }
            var stamp = UnityEngine.Object.FindAnyObjectByType<WorldStamp>();
            var path = stamp ? Path.Combine(HarbourWorldBuilder.RepoRoot, stamp.exportPath) : RenderSession.ExportPath;
            var text = File.Exists(path) ? File.ReadAllText(path) : "";
            foreach (Match m in Regex.Matches(text, "\"id\":\\s*\"(horizon/(?:planet|moon)/\\d+)\"[^{}]*\"kit\":\\s*\\{[^{}]*\"at\":\\s*\\[([-\\d.]+),\\s*([-\\d.]+)\\][^{}]*\\}[^{}]*\"base\":\\s*([-\\d.]+)"))
                found.Add((m.Groups[1].Value, new Vector3(P(m.Groups[2].Value), P(m.Groups[4].Value), P(m.Groups[3].Value))));
            note = found.Count > 0 ? "from the export's horizon pieces (kit at, base): " + string.Join(", ", found.Select(f => f.Item1)) : "no planet or moon found (scene or export)";
            return found;
        }

        static float P(string s) => float.Parse(s, CultureInfo.InvariantCulture);
        static string F(double d) => d.ToString("0.###", CultureInfo.InvariantCulture);

        /// <summary>A fixed 1920 x 1080 entry in the Game view's current size group, added once, then selected.</summary>
        static string SetGameViewSize(EditorWindow gv, Type gvType)
        {
            try
            {
                var asm = typeof(EditorWindow).Assembly;
                var sizesType = asm.GetType("UnityEditor.GameViewSizes");
                var single = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var sizes = single.GetProperty("instance").GetValue(null);
                var group = sizesType.GetProperty("currentGroup").GetValue(sizes);
                var gt = group.GetType();
                int total = (int)gt.GetMethod("GetTotalCount").Invoke(group, null);
                var getSize = gt.GetMethod("GetGameViewSize");
                int index = -1;
                for (int i = 0; i < total; i++)
                {
                    var s = getSize.Invoke(group, new object[] { i }); var st = s.GetType();
                    if ((int)st.GetProperty("width").GetValue(s) == W && (int)st.GetProperty("height").GetValue(s) == H && st.GetProperty("sizeType").GetValue(s).ToString() == "FixedResolution") { index = i; break; }
                }
                if (index < 0)
                {
                    var sizeType = asm.GetType("UnityEditor.GameViewSize"); var kind = asm.GetType("UnityEditor.GameViewSizeType");
                    var entry = Activator.CreateInstance(sizeType, Enum.Parse(kind, "FixedResolution"), W, H, "072 1080p");
                    gt.GetMethod("AddCustomSize").Invoke(group, new[] { entry });
                    index = (int)gt.GetMethod("GetTotalCount").Invoke(group, null) - 1;
                }
                var sel = gvType.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (sel != null) sel.Invoke(gv, new object[] { index, null });
                else gvType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(gv, index);
                return $"Game view fixed {W}x{H} (size index {index})";
            }
            catch (Exception e) { Debug.LogWarning($"072: could not set the Game view to {W}x{H}: {e.Message}"); return $"Game view size not set ({e.Message})"; }
        }

        static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

        static string Json(List<View> views, string phase, WorldStamp stamp, float standOff, string drawer, string sizeNote, int seenW, int seenH, string planetShot, string planetNote, string shotsDir)
        {
            var sb = new StringBuilder("{\n");
            void KV(string k, string v, bool comma = true) => sb.Append($"  \"{k}\": \"{Esc(v)}\"{(comma ? "," : "")}\n");
            KV("schema", "suborned.072-capture/1"); KV("phase", phase);
            KV("when", DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            KV("machine", $"Studio editor (indicative only): {SystemInfo.deviceModel}, {SystemInfo.processorType}, {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType})");
            KV("unity", Application.unityVersion);
            KV("scene", HarbourWorldBuilder.ScenePath);
            KV("export", stamp ? $"{stamp.exportPath} sha256 {stamp.exportSha256}" : "no WorldStamp");
            KV("camera", $"cameras/play posed as HandoverHost.PlaceCamera: orthographic size {PlayZoom}, pitch {PlayPitch}, standOff {F(standOff)} m, far standOff + 1500; TacticalShadows and ArchitectureFade as built (ArchitectureFade outside Play Mode draws tagged renderers with its cutting variant)");
            KV("gameView", $"{sizeNote}; play camera seen rendering at {seenW}x{seenH}");
            KV("drawer", $"GPU Resident Drawer {drawer}: counts and frame ms are the no-drawer path; shots through RenderSession, drawer off");
            KV("quality", $"{QualitySettings.names[QualitySettings.GetQualityLevel()]}, lodBias {F(QualitySettings.lodBias)}, URP {(UniversalRenderPipeline.asset ? UniversalRenderPipeline.asset.name : "?")}, shadow distance {(UniversalRenderPipeline.asset ? F(UniversalRenderPipeline.asset.shadowDistance) : "?")}, MSAA {Capture.Msaa}");
            KV("excluded", "the editor snapshot crowd (`people`, hidden as the host hides it); no HUD, sim, cast or engine crowd");
            KV("counts", $"UnityStats after the Game view's own render, mean of {CountRepaints} repaints after {WarmRepaints}");
            KV("census", "renderers in the 16:9 frustum after an LODGroup pick (estimate), LOD-picked index triangles of the main view only (UnityStats triangles add the shadow passes), by layer");
            KV("frameMs", $"Studio editor, indicative only: Camera.Render at {W}x{H} into Capture's HDR target, GPU waited on (1 px read-back), {WarmFrames} warm, median and p90 of {TimedFrames}");
            KV("shots", Path.GetRelativePath(HarbourWorldBuilder.RepoRoot, shotsDir).Replace('\\', '/'));
            KV("planets", (planetShot ?? "none") + ": " + planetNote);
            sb.Append("  \"views\": [\n");
            for (int i = 0; i < views.Count; i++)
            {
                var v = views[i];
                sb.Append($"    {{\"view\": \"{v.Id}\", \"label\": \"{Esc(v.Label)}\", \"focus\": [{F(v.Focus.x)}, {F(v.Focus.y)}], \"yaw\": {F(v.Yaw)},\n      \"stats\": {{");
                sb.Append(string.Join(", ", v.Stats.Select(kv => $"\"{kv.Key}\": {F(Math.Round(kv.Value, 2))}")));
                sb.Append($"}},\n      \"modules\": {{\"columnsInFrustum\": {v.ModColumns}, \"modulesInFrustum\": {v.ModModules}, \"l0Triangles\": {v.ModTris}, \"proxiesOnInScene\": {v.ModProxies}, \"proxyTrianglesInScene\": {v.ModProxyTris}}}");
                sb.Append($",\n      \"census\": {{\"visibleRenderers\": {v.Visible}, \"visibleCasters\": {v.VisibleCasters}, \"lodCulled\": {v.LodCulled}, \"triangles\": {v.VisibleTris}, \"byLayer\": {{");
                sb.Append(string.Join(", ", v.ByLayer.OrderByDescending(kv => kv.Value.tris).Select(kv => $"\"{Esc(kv.Key)}\": {{\"renderers\": {kv.Value.n}, \"triangles\": {kv.Value.tris}}}")));
                sb.Append($"}}}},\n      \"editorFrameMs\": {{\"median\": {F(Math.Round(v.Median, 2))}, \"p90\": {F(Math.Round(v.P90, 2))}}}, \"shot\": \"{v.Shot}\"}}{(i + 1 < views.Count ? "," : "")}\n");
            }
            sb.Append("  ]\n}\n");
            return sb.ToString();
        }

        static string Table(List<View> views, string phase, WorldStamp stamp, string drawer, int seenW, int seenH)
        {
            var sb = new StringBuilder();
            sb.Append($"# 072 capture ({phase}), {DateTime.Now:yyyy-MM-dd HH:mm}\n\n");
            sb.Append($"Scene from {(stamp ? stamp.exportPath + " sha256 " + stamp.exportSha256 : "?")}. Play camera (ortho 14, pitch 38), Game view {seenW}x{seenH}, GPU Resident Drawer {drawer}. ");
            sb.Append("Counts are UnityStats from the Game view (tris and verts include the shadow passes; draw calls = the Statistics window's Batches); renderers and census tris are the frustum + LOD estimate of the main view; ms is the Studio editor, indicative only.\n\n");
            sb.Append("| view | yaw | tris | verts | SetPass | draw calls | shadow casters | visible renderers | census tris | modules in frustum (columns) | module L0 tris | editor ms median / p90 |\n");
            sb.Append("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|\n");
            foreach (var v in views)
                sb.Append($"| {v.Id} | {v.Yaw:0} | {S(v, "triangles"):N0} | {S(v, "vertices"):N0} | {S(v, "setPassCalls"):F0} | {S(v, "drawCalls"):F0} | {S(v, "shadowCasters"):F0} | {v.Visible} | {v.VisibleTris:N0} | {v.ModModules} ({v.ModColumns}) | {v.ModTris:N0} | {v.Median:F1} / {v.P90:F1} |\n");
            return sb.ToString();
        }
    }
}
