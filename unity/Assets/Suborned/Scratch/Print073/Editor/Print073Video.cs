using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Suborned.Play;
using Suborned.World;
using Suborned.World.Review;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Suborned.Scratch
{
    /// <summary>Brief 073 SCRATCH (Glint, 2026-10-28): offscreen videos of the print prototype and its siblings, frame by frame in batchmode.
    /// The play camera renders through Capture's HDR path (RenderSession.Grab) at 1920 x 1080 on a fixed timestep (1 / fps), and each frame's
    /// RGB24 goes straight down a pipe into ffmpeg (H.264, yuv420p). Clips:
    ///   tp    : a SCRATCH third-person presentation camera (not the game's camera rule, not the game's follow view): perspective 55 degrees,
    ///           far 350 (the follow tier's haze and grade, as HostFollow sets it), 3.6 m behind and 2.3 m above the scout, who walks (her own
    ///           walk clip, sampled) the last 40 m of an A* path on the site's walk kinds from the café square (Plaza west) to the Records
    ///           Office approach, then stands;
    ///   play  : play zoom (ortho 14 at 38), an eased pan from the cell's opening view to the Office front at yaw 30, then an eased quarter
    ///           turn to yaw 120 about the Office front, with the fight-lab signal frame (cones, lines; After072.SignalFrame) at the Office front;
    ///   night : the play clip at night (LightPhase night, the lamp cap following the focus).
    ///   view-phase (round 3, Sky "also evening / night time"): tp-evening, tp-night, play-evening, play-night. The phase is set through the
    ///           game's own day/night system (LightPhase.SetEvening held at full Dusk, LightPhase.Set(true) with the night floor and the lamp cap),
    ///           and the per-frame parts edit mode does not run are stepped by hand every frame, plus one step and a warm grab after every switch:
    ///           AtmosphereLens.Update (the evening grade), RoadLight (strip light, lit windows, people fill; a scratch instance, the runtime
    ///           copies it makes are in memory only), SignalHead.Step on every head (phase colours; the cycle runs on the clip clock).
    ///   Looks take "+stencil" (Print073.Protected: cones, crossing heads and dots, lamp heads skip every print pass).
    /// The runtime look is installed in memory first (AtmosphereLens as Glint072 does it, DistanceHaze for the perspective clip); the crowd
    /// is hidden. Water, the fountain and the crossing heads hold still (edit-mode time does not run). Per clip: mean synced-grab ms and a
    /// flicker measure (mean |L(t+1) - 2L(t) + L(t-1)| over the frame, and the share of pixels above 24 levels).
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.Scratch.Print073Video.Cli -looks off,default -clips tp,play [-fps 30] [-seconds 4] -logFile &lt;log&gt;
    ///   -probe: the anchor's y sign check only (default@ground panned by 12 px), no video.</summary>
    public static class Print073Video
    {
        const int W = 1920, H = 1080, SignalLayer = 8;
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        const string Ffmpeg = "/opt/homebrew/bin/ffmpeg";

        public static void Cli() => HarbourWorldBuilder.Batch(Run);

        static string Arg(string name, string fallback)
        {
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : fallback;
        }
        static bool Flag(string name) => Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;

        // the scene's bits
        static Camera cam; static TacticalShadows shadows; static ArchitectureFade fade; static HandoverHost host; static LightPhase phase;
        static float standOff; static Vector3 focusNow;
        static Vector2 cellOpen;

        static void Run()
        {
            var root = HarbourWorldBuilder.RepoRoot;
            var outDir = Path.GetFullPath(Path.Combine(root, Arg("-out", Path.Combine("briefs", "073", "video", "raw"))));
            var looks = Arg("-looks", "off,default").Split(',');
            var clips = Arg("-clips", "tp,play").Split(',');
            int fps = int.Parse(Arg("-fps", "30"));
            float secScale = float.Parse(Arg("-seconds", "0"), System.Globalization.CultureInfo.InvariantCulture); // >0: every clip cut to this many seconds (a smoke run)
            Directory.CreateDirectory(outDir);

            EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, OpenSceneMode.Single);
            var playGo = GameObject.Find("cameras/play") ?? throw new InvalidOperationException("video073: no cameras/play");
            cam = playGo.GetComponent<Camera>(); shadows = playGo.GetComponent<TacticalShadows>(); fade = playGo.GetComponent<ArchitectureFade>();
            host = Object.FindAnyObjectByType<HandoverHost>(FindObjectsInactive.Include) ?? throw new InvalidOperationException("video073: no HandoverHost");
            standOff = host.standOff;
            cellOpen = Baseline072.CellOpenFallback;
            try { var w = host.frame.World(HandoverHost.OpeningFocus); if (!float.IsNaN(w.x) && (w.x != 0 || w.z != 0)) cellOpen = new Vector2(w.x, w.z); } catch { }
            var people = GameObject.Find("people"); if (people) people.SetActive(false);
            if (shadows) { shadows.enabled = false; shadows.enabled = true; }
            if (fade) { fade.enabled = false; fade.enabled = true; }
            foreach (var oc in Camera.allCameras) if (oc != cam && oc.targetTexture == null) oc.enabled = false;

            phase = Object.FindAnyObjectByType<LightPhase>(FindObjectsInactive.Include) ?? throw new InvalidOperationException("video073: no LightPhase");
            typeof(LightPhase).GetMethod("OnEnable", Any).Invoke(phase, null);
            var lensArgs = new object[] { null };
            var lens = (AtmosphereLens)typeof(Glint072).GetMethod("InstallLens", Any).Invoke(null, lensArgs); var lensGo = (GameObject)lensArgs[0];
            var hazeGo = InstallHaze();
            phase.Focus = () => focusNow;

            var report = new StringBuilder();
            var made = new List<Object>(); var notes = new List<string>();
            GameObject figure = null;
            int maskWas = cam.cullingMask;
            var saved = (pos: cam.transform.position, rot: cam.transform.rotation, ortho: cam.orthographic, size: cam.orthographicSize, fov: cam.fieldOfView, near: cam.nearClipPlane, far: cam.farClipPlane);
            try
            {
                using var session = new RenderSession();
                Baseline072.Pose(cam, shadows, fade, Baseline072.OfficeFront, 30f, standOff); session.Grab(cam, 64, 64); // the first render after a load comes out flat

                if (Flag("-probe")) { Probe(session, report); return; }

                // the signal frame at the Office front (cones on the default layer, lines on the signal layer drawn by SignalsFeature)
                var sf = typeof(After072).GetMethod("SignalFrame", Any) ?? throw new InvalidOperationException("video073: no After072.SignalFrame");
                var sfArgs = new List<object> { cam, host, made, notes }; if (sf.GetParameters().Length == 5) sfArgs.Add(Baseline072.OfficeFront);
                cam.orthographic = true; cam.orthographicSize = Baseline072.PlayZoom; // SignalFrame sizes its lines from the ortho size
                sf.Invoke(null, sfArgs.ToArray());
                var signalObjs = made.OfType<GameObject>().ToList();
                cam.cullingMask |= 1 << SignalLayer;
                InstallRoad(made, notes);
                heads = Object.FindObjectsByType<SignalHead>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).OrderBy(h => h.name, StringComparer.Ordinal).ToArray();
                notes.Add($"signal heads stepped: {heads.Length}");
                var want = Type.GetType("Suborned.SignalsFeature, Suborned.Runtime")?.GetMethod("Want");

                // the walk: path + figure
                Walk walk = null;
                if (clips.Any(c => c.StartsWith("tp"))) { figure = Figure(made, notes); walk = Walk.Build(host.frame, notes); }
                if (looks.Any(l => l.Contains("+stencil"))) ProtectList(signalObjs, notes);

                foreach (var look in looks)
                {
                    if (look == "off") Print073.Disable(); else Print073.Enable(look);
                    try
                    {
                        foreach (var clip in clips)
                        {
                            bool tp = clip.StartsWith("tp"); string ph = clip == "night" || clip.EndsWith("-night") ? "night" : clip.EndsWith("-evening") ? "evening" : "day";
                            bool night = ph == "night";
                            float T = tp ? walk.Duration : 18f; if (secScale > 0f) T = Mathf.Min(T, secScale);
                            int frames = Mathf.RoundToInt(T * fps); float dt = 1f / fps;
                            foreach (var g in signalObjs) if (g) g.SetActive(!tp);
                            if (figure) figure.SetActive(tp);
                            SetPhase(ph);
                            if (tp) walk.Pose(cam, figure, 0f); else PlayPose(0f, T);
                            StepFrame(0f, night); want?.Invoke(null, null); session.Grab(cam, 64, 64); // the one-frame step after the switch (RoadLight and the heads read LightPhase.Current once a frame)
                            var file = Path.Combine(outDir, $"{look.Replace('@', '-')}-{clip}.mp4");
                            var flick = new Flicker(); double grabMs = 0; int timed = 0;
                            using (var enc = new Encoder(file, fps))
                            {
                                for (int f = -2; f < frames; f++) // two warm frames at t = 0
                                {
                                    float t = Mathf.Max(0, f) * dt;
                                    if (tp) walk.Pose(cam, figure, t); else PlayPose(t, T);
                                    StepFrame(t, night);
                                    want?.Invoke(null, null);
                                    var sw = Stopwatch.StartNew();
                                    var tex = session.Grab(cam, W, H);
                                    if (f >= 0) { grabMs += sw.Elapsed.TotalMilliseconds; timed++; }
                                    if (f < 0) continue;
                                    var raw = tex.GetRawTextureData<byte>().ToArray();
                                    flick.Add(raw);
                                    enc.Write(raw);
                                }
                            }
                            SetPhase("day"); StepFrame(0f, false);
                            var line = $"\"{look}-{clip}\": {{ \"frames\": {frames}, \"fps\": {fps}, \"grabMsMean\": {grabMs / Math.Max(1, timed):F2}, \"flickerMean\": {flick.Mean:F3}, \"flickerShare24\": {flick.Share:F5}, \"file\": \"{Path.GetFileName(file)}\" }}";
                            report.AppendLine(line); Debug.Log("video073: " + line);
                        }
                    }
                    finally { Print073.Disable(); }
                }
                report.AppendLine("\"notes\": " + Json(string.Join(" | ", notes)));
            }
            finally
            {
                cam.cullingMask = maskWas;
                cam.transform.SetPositionAndRotation(saved.pos, saved.rot); cam.orthographic = saved.ortho; cam.orthographicSize = saved.size; cam.fieldOfView = saved.fov; cam.nearClipPlane = saved.near; cam.farClipPlane = saved.far;
                Print073.Disable();
                foreach (var o in made) if (o) Object.DestroyImmediate(o);
                typeof(AtmosphereLens).GetMethod("OnDisable", Any)?.Invoke(lens, null);
                Object.DestroyImmediate(lensGo);
                if (hazeGo) { var h = hazeGo.GetComponent<DistanceHaze>(); typeof(DistanceHaze).GetMethod("OnDisable", Any)?.Invoke(h, null); Object.DestroyImmediate(hazeGo); }
                var stamp = DateTime.Now.ToString("HHmmss");
                File.WriteAllText(Path.Combine(outDir, $"video073-{string.Join("_", looks).Replace('@', '-')}-{stamp}.jsonl"), report.ToString());
            }
            Debug.Log($"video073: done → {outDir}\n{report}");
        }

        // ---- round 3: phases ----
        static RoadLight road; static SignalHead[] heads = Array.Empty<SignalHead>();
        static readonly FieldInfo RoadFirst = typeof(RoadLight).GetField("first", Any), HeadFirst = typeof(SignalHead).GetField("first", Any),
            HeadWrote = typeof(SignalHead).GetField("wroteHead", Any), DotsWrote = typeof(SignalHead).GetField("wroteDots", Any);
        static readonly MethodInfo RoadUpdate = typeof(RoadLight).GetMethod("Update", Any), HeadStep = typeof(SignalHead).GetMethod("Step", Any),
            LensUpdate = typeof(AtmosphereLens).GetMethod("Update", Any), PhaseCap = typeof(LightPhase).GetMethod("Cap", Any), PhaseDusk = typeof(LightPhase).GetMethod("ApplyDusk", Any);

        /// <summary>day | evening | night through LightPhase, as the game does it; evening is held at full Dusk (the ease runs in Update, which edit mode does not call).</summary>
        static void SetPhase(string ph)
        {
            var dusk = typeof(LightPhase).GetProperty("Dusk");
            if (ph == "night") { phase.Set(true); return; }
            if (ph == "evening") { phase.SetEvening(); dusk.SetValue(phase, 1f); }
            else { phase.Set(false); dusk.SetValue(phase, 0f); }
            PhaseDusk.Invoke(phase, null);
        }

        /// <summary>What play mode's frame would run for the look: the lens's evening grade, RoadLight (snapped, no ease), every signal head (snapped,
        /// written), the night lamp cap at the focus. The heads' cycle runs on the clip clock (MissionSeconds = t).</summary>
        static void StepFrame(float t, bool night)
        {
            var lens = AtmosphereLens.Instance; if (lens) LensUpdate?.Invoke(lens, null);
            if (road) { RoadFirst.SetValue(road, true); RoadUpdate.Invoke(road, null); }
            SignalHead.MissionSeconds = t;
            foreach (var h in heads) { if (!h) continue; HeadFirst.SetValue(h, true); HeadWrote.SetValue(h, false); DotsWrote.SetValue(h, false); HeadStep.Invoke(h, null); }
            if (night) PhaseCap.Invoke(phase, new object[] { true });
        }

        /// <summary>RoadLight as its Install does (play mode only: RuntimeInitializeOnLoadMethod, DontDestroyOnLoad), on a scratch object.</summary>
        static void InstallRoad(List<Object> made, List<string> notes)
        {
            var text = Resources.Load<TextAsset>(RoadLight.ResourcePath) ?? throw new InvalidOperationException("video073: no roadlight.json");
            var d = JsonUtility.FromJson<RoadLight.Data>(text.text);
            var why = (string)typeof(RoadLight).GetMethod("Check", Any).Invoke(null, new object[] { d });
            if (why != null) throw new InvalidDataException("video073: roadlight.json rejected: " + why);
            var go = new GameObject("road light (video073)") { hideFlags = HideFlags.DontSave }; made.Add(go);
            road = go.AddComponent<RoadLight>(); typeof(RoadLight).GetField("d", Any).SetValue(road, d);
            notes.Add("road light installed (scratch)");
        }

        /// <summary>+stencil: the cones (the signal frame's world objects off the signal layer), every crossing head's parts and dot rows, and the
        /// lamp heads (every submesh drawn with an emissive material of a DK_Streetlamp / DK_PlazaSphereLamp piece, anywhere in the scene, which
        /// catches the civic square's batched lamps; plus the café's opal and lens lamp materials).</summary>
        static void ProtectList(List<GameObject> signalObjs, List<string> notes)
        {
            var P = Print073.Protected; P.Clear();
            int cones = 0, headParts = 0, lampSubs = 0;
            foreach (var g in signalObjs) if (g) foreach (var r in g.GetComponentsInChildren<Renderer>(true)) if (r.gameObject.layer != SignalLayer) { P.Add((r, -1)); cones++; }
            foreach (var h in heads)
            {
                foreach (var r in new[] { h.Rim, h.Figure, h.Tens, h.Units, h.Ring, h.Beam, h.Halo }) if (r) { P.Add((r, -1)); headParts++; }
                foreach (var r in h.Dots) if (r) { P.Add((r, -1)); headParts++; }
            }
            static bool Emissive(Material m) => m && (m.IsKeywordEnabled("_EMISSION") || m.HasProperty("_EmissionColor") && m.GetColor("_EmissionColor").maxColorComponent > 0.05f);
            var lampMats = new HashSet<Material>();
            var all = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var r in all)
            {
                bool lampPiece = false; for (var t = r.transform; t; t = t.parent) if (t.name.StartsWith("DK_Streetlamp") || t.name.StartsWith("DK_PlazaSphereLamp")) { lampPiece = true; break; }
                foreach (var m in r.sharedMaterials)
                    if (Emissive(m) && m.name != "DK_Road_Lens" && (lampPiece || System.Text.RegularExpressions.Regex.IsMatch(m.name, "Opal|Globe|LampHead|Lamp_Head|Uplight|Sphere_?Lamp", System.Text.RegularExpressions.RegexOptions.IgnoreCase))) lampMats.Add(m);
            }
            // the night lamps' own fixtures: every emissive material on a renderer whose bounds hold a lamp head (a lamp light's position + 0.4 m)
            var heads0 = phase.lamps.Where(l => l).Select(l => l.transform.position + (l.name.StartsWith("street/") ? Vector3.up * 0.4f : Vector3.zero)).ToArray();
            var near = new Dictionary<string, int>();
            foreach (var r in all)
            {
                var b = r.bounds; b.Expand(0.3f); if (!heads0.Any(h => b.Contains(h))) continue;
                foreach (var m in r.sharedMaterials) if (m) { var key = m.name + (Emissive(m) ? "*" : ""); near[key] = near.TryGetValue(key, out var n) ? n + 1 : 1; if (Emissive(m) && m.name != "DK_Road_Lens" && !m.name.StartsWith("DK_GlassDark")) lampMats.Add(m); }
            }
            notes.Add("materials at the lamp heads (* emissive): " + string.Join(", ", near.OrderByDescending(kv => kv.Value).Take(30).Select(kv => $"{kv.Key} {kv.Value}")));
            foreach (var r in all)
            {
                var ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++) if (ms[i] && lampMats.Contains(ms[i])) { P.Add((r, i)); lampSubs++; }
            }
            notes.Add($"+stencil protected: {cones} cone renderers, {headParts} crossing-head parts and dot rows ({heads.Length} heads), {lampSubs} lamp submeshes on [{string.Join(", ", lampMats.Select(m => m.name).OrderBy(n => n, StringComparer.Ordinal))}]");
        }

        static string Json(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        /// <summary>The play clip: hold 1.5 s at the cell's opening view, an eased pan to the Office front (1.5 to 10.5 s), hold, an eased quarter
        /// turn 30 → 120 about the Office front (11.5 to 15.5 s), hold.</summary>
        static void PlayPose(float t, float T)
        {
            float a = Ease(Mathf.InverseLerp(1.5f, 10.5f, t)), b = Ease(Mathf.InverseLerp(11.5f, 15.5f, t));
            var f = Vector2.Lerp(cellOpen, Baseline072.OfficeFront, a);
            float yaw = Mathf.Lerp(30f, 120f, b);
            focusNow = new Vector3(f.x, 0f, f.y);
            Baseline072.Pose(cam, shadows, fade, f, yaw, standOff);
        }
        static float Ease(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

        /// <summary>The anchor's y sign: default@ground with flip +1 and -1, the camera moved 12 px up the screen between two frames; the print's
        /// pattern (frame / off frame) should move with the picture. Logs the mean error of each pairing.</summary>
        static void Probe(RenderSession session, StringBuilder report)
        {
            Baseline072.Pose(cam, shadows, fade, Baseline072.OfficeFront, 30f, standOff); focusNow = new Vector3(Baseline072.OfficeFront.x, 0, Baseline072.OfficeFront.y);
            float mpp = 2f * cam.orthographicSize / H; int shift = 12;
            var p0 = cam.transform.position; var up = cam.transform.up;
            float[] L(bool moved) { cam.transform.position = moved ? p0 + up * (shift * mpp) : p0; session.Grab(cam, W, H); var raw = session.Grab(cam, W, H).GetRawTextureData<byte>().ToArray(); var o = new float[W * H]; for (int i = 0; i < o.Length; i++) o[i] = 0.299f * raw[3 * i] + 0.587f * raw[3 * i + 1] + 0.114f * raw[3 * i + 2]; return o; }
            Print073.Disable(); var offA = L(false); var offB = L(true);
            foreach (var flip in new[] { 1f, -1f, 0f })
            {
                Print073.AnchorFlipY = flip == 0f ? 1f : flip;
                Print073.Enable(flip == 0f ? "default" : "default@ground");
                var a = L(false); var b = L(true); Print073.Disable();
                // camera up by `shift` px: the picture moves DOWN the screen; in the raw (bottom-up rows) a row r of A is row r - shift of B
                foreach (var s in new[] { -shift, 0, shift })
                {
                    double e = 0; int n = 0;
                    for (int y = 200; y < H - 200; y += 2) for (int x = 200; x < W - 200; x += 2)
                        {
                            int ia = y * W + x, ib = (y + s) * W + x;
                            float ra = a[ia] / Mathf.Max(8f, offA[ia]), rb = b[ib] / Mathf.Max(8f, offB[ib]);
                            e += Mathf.Abs(ra - rb); n++;
                        }
                    var line = $"probe flip {(flip == 0f ? "screen" : flip.ToString("+0;-0"))} rowShift {s:+0;-0;0}: mean |ratioA - ratioB| {e / n:F4}";
                    report.AppendLine(line); Debug.Log("video073: " + line);
                }
            }
            Print073.AnchorFlipY = -1f;
        }

        /// <summary>DistanceHaze as its Install does (minus DontDestroyOnLoad, play mode only), so the perspective clip gets the follow tier's haze.</summary>
        static GameObject InstallHaze()
        {
            if (DistanceHaze.Instance) return null;
            var t = typeof(DistanceHaze); var dataT = t.GetNestedType("Data", Any);
            var text = Resources.Load<TextAsset>(DistanceHaze.ResourcePath) ?? throw new InvalidOperationException("video073: no haze.json");
            var d = JsonUtility.FromJson(text.text, dataT);
            var check = t.GetMethods(Any).First(m => m.Name == "Check" && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == dataT);
            var args = new object[] { d, null }; var why = (string)check.Invoke(null, args);
            if (why != null) throw new InvalidDataException("video073: haze.json rejected: " + why);
            var go = new GameObject("distance haze (video073)"); go.SetActive(false);
            var h = go.AddComponent<DistanceHaze>();
            t.GetField("data", Any).SetValue(h, d); t.GetField("colour", Any).SetValue(h, (Color)args[1]);
            var follow = dataT.GetField("follow", Any).GetValue(d); t.GetField("followEnd", Any).SetValue(h, (float)follow.GetType().GetField("end", Any).GetValue(follow));
            go.SetActive(true);
            t.GetMethod("OnEnable", Any).Invoke(h, null); // edit mode does not call it
            return go;
        }

        /// <summary>The scout (FigureLibrary "scout") with the game's own controller (lib.ControllerFor), posed by hand each frame:
        /// Animator.Play(state, 0, normalised time) then Update(0) (edit mode, no clock).</summary>
        static GameObject Figure(List<Object> made, List<string> notes)
        {
            var lib = AssetDatabase.LoadAssetAtPath<FigureLibrary>("Assets/Suborned/People/Generated/FigureLibrary.asset") ?? throw new InvalidOperationException("video073: no FigureLibrary");
            var e = lib.Find("scout") ?? throw new InvalidOperationException("video073: no scout figure");
            var rig = new GameObject("scout rig (video073)") { hideFlags = HideFlags.DontSave }; made.Add(rig);
            var inst = Object.Instantiate(e.model, rig.transform, false); inst.name = "scout";
            if (inst.GetComponentInChildren<LODGroup>() is LODGroup lg && e.lodHeights.Length > 0)
            { var lods = lg.GetLODs(); for (int i = 0; i < lods.Length; i++) lods[i].screenRelativeTransitionHeight = e.lodHeights[Mathf.Min(i, e.lodHeights.Length - 1)]; lg.SetLODs(lods); }
            var an = inst.GetComponent<Animator>() ?? inst.AddComponent<Animator>();
            an.runtimeAnimatorController = lib.ControllerFor(e); an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate; an.enabled = true;
            Walk.Anim = an; Walk.ClipSpeed = lib.Speed("scout", "walk") > 0f ? lib.Speed("scout", "walk") : 1.33f;
            float Len(string st) { an.Rebind(); an.Play(st, 0, 0f); an.Update(0f); var i = an.GetCurrentAnimatorStateInfo(0); return i.IsName(st) ? i.length : 0f; }
            Walk.WalkLen = Len("walk"); Walk.IdleLen = Len("idle");
            notes.Add($"figure scout from {AssetDatabase.GetAssetPath(e.model)}, controller {an.runtimeAnimatorController?.name}; walk {Walk.WalkLen:F2} s at {Walk.ClipSpeed} m/s, idle {Walk.IdleLen:F2} s");
            rig.SetActive(false);
            return rig;
        }

        /// <summary>A* on the walk kinds (2 and 4 solid) with 0.7 m clearance, string-pulled and Chaikin-smoothed, in world space.</summary>
        sealed class Walk
        {
            public static Animator Anim; public static float WalkLen, IdleLen, ClipSpeed = 1.33f;
            const float Back = 3.6f, Up = 2.3f, Ahead = 8f, LookUp = 1.35f, Fov = 55f, Far = 350f, Clear = 0.7f, Take = 45f, Lead = 0.5f, Tail = 1.5f, MaxSpeed = 1.6f;
            List<Vector3> pts = new(); List<float> cum = new(); float speed, walkT;
            public float Duration => Lead + walkT + Tail;
            float camYaw = float.NaN, camY = float.NaN, figYaw = float.NaN, lastT = -1f;

            public static Walk Build(SiteFrame fr, List<string> notes)
            {
                if (!fr.HasKinds) throw new InvalidOperationException("video073: the host frame has no walk kinds baked");
                int nx = fr.nx, ny = fr.ny; float c = fr.cell; int r = Mathf.CeilToInt(Clear / c);
                var solid = new bool[nx * ny]; for (int i = 0; i < solid.Length; i++) solid[i] = fr.kinds[i] == 2 || fr.kinds[i] == 4;
                // dilate by r (square, separable)
                var tmp = new bool[nx * ny]; var blocked = new bool[nx * ny];
                for (int i = 0; i < nx; i++) for (int j = 0; j < ny; j++) { bool b = false; for (int k = Math.Max(0, j - r); k <= Math.Min(ny - 1, j + r) && !b; k++) b = solid[i * ny + k]; tmp[i * ny + j] = b; }
                for (int i = 0; i < nx; i++) for (int j = 0; j < ny; j++) { bool b = false; for (int k = Math.Max(0, i - r); k <= Math.Min(nx - 1, i + r) && !b; k++) b = tmp[k * ny + j]; blocked[i * ny + j] = b; }
                (int, int) Cell(Vector2 b) => (Mathf.Clamp(Mathf.FloorToInt((b.x - fr.x0) / c), 0, nx - 1), Mathf.Clamp(Mathf.FloorToInt((b.y - fr.y0) / c), 0, ny - 1));
                (int, int) Free((int i, int j) p) { for (int d = 0; d < 40; d++) for (int a = -d; a <= d; a++) for (int b = -d; b <= d; b++) { int i = p.i + a, j = p.j + b; if (i >= 0 && j >= 0 && i < nx && j < ny && !blocked[i * ny + j]) return (i, j); } return p; }
                var from = Free(Cell(new Vector2(6.5f, -34.5f))); var to = Free(Cell(new Vector2(64f, -44.4f))); // Plaza west → Office approach (mission destinations, block m)
                // A*
                var gScore = new float[nx * ny]; for (int i = 0; i < gScore.Length; i++) gScore[i] = float.MaxValue;
                var prev = new int[nx * ny]; var shut = new bool[nx * ny];
                var open = new SortedSet<(float f, int id)>();
                int Id((int i, int j) p) => p.i * ny + p.j;
                float Hh(int id) { int i = id / ny, j = id % ny; return Mathf.Sqrt((i - to.Item1) * (i - to.Item1) + (j - to.Item2) * (j - to.Item2)); }
                int s0 = Id(from), goal = Id(to); gScore[s0] = 0; open.Add((Hh(s0), s0)); prev[s0] = -1;
                while (open.Count > 0)
                {
                    var cur = open.Min; open.Remove(cur); int id = cur.id; if (shut[id]) continue; shut[id] = true; if (id == goal) break;
                    int ci = id / ny, cj = id % ny;
                    for (int a = -1; a <= 1; a++) for (int b = -1; b <= 1; b++)
                        {
                            if (a == 0 && b == 0) continue; int i = ci + a, j = cj + b; if (i < 0 || j < 0 || i >= nx || j >= ny) continue;
                            int n = i * ny + j; if (blocked[n] || shut[n]) continue;
                            float g = gScore[id] + (a != 0 && b != 0 ? 1.41421f : 1f);
                            if (g < gScore[n]) { gScore[n] = g; prev[n] = id; open.Add((g + Hh(n), n)); }
                        }
                }
                if (!shut[goal]) throw new InvalidOperationException("video073: no walk path from Plaza west to the Office approach");
                var cells = new List<int>(); for (int id = goal; id != -1; id = prev[id]) cells.Add(id); cells.Reverse();
                Vector2 B(int id) => new(fr.x0 + (id / ny + 0.5f) * c, fr.y0 + (id % ny + 0.5f) * c);
                bool Sight(Vector2 a, Vector2 b) { int n = Mathf.CeilToInt(Vector2.Distance(a, b) / (c * 0.5f)); for (int k = 0; k <= n; k++) { var p = Vector2.Lerp(a, b, k / (float)Mathf.Max(1, n)); var (i, j) = Cell(p); if (blocked[i * ny + j]) return false; } return true; }
                // string-pull
                var bl = new List<Vector2> { B(cells[0]) }; int at = 0;
                while (at < cells.Count - 1) { int best = at + 1; for (int k = cells.Count - 1; k > at + 1; k--) if (Sight(B(cells[at]), B(cells[k]))) { best = k; break; } bl.Add(B(cells[best])); at = best; }
                // Chaikin x3
                for (int it = 0; it < 3; it++) { var o = new List<Vector2> { bl[0] }; for (int k = 0; k < bl.Count - 1; k++) { o.Add(Vector2.Lerp(bl[k], bl[k + 1], 0.25f)); o.Add(Vector2.Lerp(bl[k], bl[k + 1], 0.75f)); } o.Add(bl[^1]); bl = o; }
                // densify to 0.1 m, world space
                var w = new Walk(); Vector2 last = bl[0];
                void Push(Vector2 b) { var p = fr.ToPlan(b.x, b.y); w.pts.Add(new Vector3(p.x, fr.Height(b.x, b.y), p.y)); }
                Push(bl[0]);
                for (int k = 1; k < bl.Count; k++) { int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(bl[k - 1], bl[k]) / 0.1f)); for (int q = 1; q <= n; q++) Push(Vector2.Lerp(bl[k - 1], bl[k], q / (float)n)); }
                float total = 0; w.cum.Add(0); for (int k = 1; k < w.pts.Count; k++) { total += Vector2.Distance(new Vector2(w.pts[k].x, w.pts[k].z), new Vector2(w.pts[k - 1].x, w.pts[k - 1].z)); w.cum.Add(total); }
                // keep the last `Take` m
                float cut = Mathf.Max(0f, total - Take); int s = w.cum.FindIndex(x => x >= cut);
                w.pts = w.pts.GetRange(s, w.pts.Count - s); var c0 = w.cum[s]; w.cum = w.cum.GetRange(s, w.cum.Count - s).Select(x => x - c0).ToList();
                float len = w.cum[^1];
                w.speed = Mathf.Clamp(len / 28f, ClipSpeed, MaxSpeed); w.walkT = len / w.speed;
                notes.Add($"walk: A* {cells.Count} cells ({c} m), {bl.Count} smoothed points, path {total:F1} m, kept the last {len:F1} m from plan ({w.pts[0].x:F1}, {w.pts[0].z:F1}) to ({w.pts[^1].x:F1}, {w.pts[^1].z:F1}), {w.speed:F2} m/s, {w.walkT:F1} s walking, clip {w.Duration:F1} s");
                return w;
            }

            Vector3 At(float d)
            {
                d = Mathf.Clamp(d, 0f, cum[^1]); int k = cum.BinarySearch(d); if (k < 0) k = ~k; k = Mathf.Clamp(k, 1, pts.Count - 1);
                float a = Mathf.InverseLerp(cum[k - 1], cum[k], d); return Vector3.Lerp(pts[k - 1], pts[k], a);
            }
            float Heading(float d, float back, float ahead) { var v = At(d + ahead) - At(d - back); v.y = 0; if (v.sqrMagnitude < 1e-6f) v = At(cum[^1]) - At(cum[^1] - 1f); return Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg; }

            /// <summary>Pose the camera and the figure at clip time t (deterministic: the smoothing state steps in t order from t = 0).</summary>
            public void Pose(Camera cam, GameObject fig, float t)
            {
                if (t <= 0f || t < lastT) { camYaw = float.NaN; camY = float.NaN; figYaw = float.NaN; }
                float dtt = lastT < 0f || t <= lastT ? 0f : t - lastT; lastT = t;
                float tw = Mathf.Clamp(t - Lead, 0f, walkT); float d = tw * speed; bool walking = t >= Lead && t - Lead < walkT;
                var p = At(d);
                // figure: faces the path, a little smoothed; walk cycle at the speed's rate, idle once she stops
                float fy = Heading(d, 0.4f, 0.8f); figYaw = float.IsNaN(figYaw) ? fy : Mathf.LerpAngle(figYaw, fy, 1f - Mathf.Exp(-dtt / 0.12f));
                fig.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, figYaw, 0f));
                if (Anim)
                {
                    if (walking && WalkLen > 0f) Anim.Play("walk", 0, ((tw * speed / ClipSpeed) / WalkLen) % 1f);
                    else if (IdleLen > 0f) Anim.Play("idle", 0, (t / IdleLen) % 1f);
                    Anim.Update(0f);
                    Anim.transform.localPosition = Vector3.zero; Anim.transform.localRotation = Quaternion.identity; // root motion stays with the rig
                }
                // camera: behind and above, a lazy yaw (0.5 s), the height low-passed over kerbs and steps
                float cy = Heading(d, 1.5f, 4f); camYaw = float.IsNaN(camYaw) ? cy : Mathf.LerpAngle(camYaw, cy, 1f - Mathf.Exp(-dtt / 0.5f));
                camY = float.IsNaN(camY) ? p.y : Mathf.Lerp(camY, p.y, 1f - Mathf.Exp(-dtt / 0.4f));
                var fwd = Quaternion.Euler(0f, camYaw, 0f) * Vector3.forward;
                var basePos = new Vector3(p.x, camY, p.z);
                var pos = basePos - fwd * Back + Vector3.up * Up;
                var look = basePos + fwd * Ahead + Vector3.up * LookUp;
                cam.orthographic = false; cam.fieldOfView = Fov; cam.nearClipPlane = 0.2f; cam.farClipPlane = Far;
                cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos, Vector3.up));
                focusNow = basePos;
            }
        }

        /// <summary>RGB24 frames down a pipe into ffmpeg: H.264 high, CRF 14 (low enough that the grain and dots survive the encode), yuv420p.</summary>
        sealed class Encoder : IDisposable
        {
            readonly Process p; readonly Stream s; readonly string file;
            public Encoder(string file, int fps)
            {
                this.file = file;
                var psi = new ProcessStartInfo(Ffmpeg, $"-y -loglevel error -f rawvideo -pix_fmt rgb24 -s {W}x{H} -r {fps} -i - -vf vflip -c:v libx264 -preset medium -crf 14 -pix_fmt yuv420p -movflags +faststart \"{file}\"")
                { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardError = true, CreateNoWindow = true };
                p = Process.Start(psi) ?? throw new InvalidOperationException("video073: ffmpeg did not start");
                s = p.StandardInput.BaseStream;
            }
            public void Write(byte[] b) => s.Write(b, 0, b.Length);
            public void Dispose()
            {
                s.Flush(); s.Close();
                var err = p.StandardError.ReadToEnd(); p.WaitForExit();
                if (p.ExitCode != 0) Debug.LogError($"video073: ffmpeg {p.ExitCode} for {file}: {err}");
                p.Dispose();
            }
        }

        /// <summary>Temporal flicker: per frame, mean |L(t+1) - 2 L(t) + L(t-1)| over the frame (8-bit luma, every 2nd pixel), and the share above 24.</summary>
        sealed class Flicker
        {
            byte[] a, b; double sum; long over, n; int frames;
            public void Add(byte[] raw)
            {
                var l = new byte[W * H / 4]; int k = 0;
                for (int y = 0; y < H; y += 2) for (int x = 0; x < W; x += 2) { int i = 3 * (y * W + x); l[k++] = (byte)((77 * raw[i] + 150 * raw[i + 1] + 29 * raw[i + 2]) >> 8); }
                if (a != null && b != null)
                {
                    double s = 0; for (int i = 0; i < l.Length; i++) { int d = Math.Abs(l[i] - 2 * b[i] + a[i]); s += d; if (d > 24) over++; }
                    sum += s / l.Length; n += l.Length; frames++;
                }
                a = b; b = l;
            }
            public double Mean => frames == 0 ? 0 : sum / frames;
            public double Share => n == 0 ? 0 : over / (double)n;
        }
    }
}
