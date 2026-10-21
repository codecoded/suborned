using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using Order = Suborned.Sim.Order;
using OpId = Suborned.Sim.OpId;

namespace Suborned.Play
{
    // ---- the resolved shots file (tools/game/shots.ts writes it from tools/game/shots.json; JsonUtility reads it, so plain public fields)
    [Serializable] public sealed class ShotPose { public bool on; public string view = ""; public float[] pos = new float[0], look = new float[0], offset = new float[0], lookOffset = new float[0]; public float fov = 39.6f; }
    [Serializable] public sealed class ShotSpec
    {
        public string hover = ""; public bool mark = true;
        public string name = "", scriptPath = "", checkpoint = "", preset = "today", agent = "scout", face = "", focus = "", state = "rest", select = "", arm = "";
        public int tick, yaw = -1; public bool night, setNight, briefing, follow, hasAt, hud = true, hasPointer, intel;
        public float atX, atY, zoom, px, py, settle = 0.35f, perf, clipSeconds; public int clipFps = 30, w = 1920, h = 1080; public bool jpg; public ShotPose pose = new ShotPose();
    }
    [Serializable] public sealed class ShotFile { public string schema = "", outDir = ""; public ShotSpec[] shots = new ShotSpec[0]; }

    /// <summary>`-shots &lt;resolved.json&gt; &lt;dir&gt;` (brief 034, Splice): the one shot runner of the built player. It works through the shots file in order
    /// (tools/game/shots.json, resolved by tools/game/shots.ts) and replaces the per-round scratch scripts and the bespoke Shots0NN runners: a
    /// new picture is a new row in the file. Each shot gets its run (the plain start, a route script's orders given as a click gives them, or a
    /// fight-lab checkpoint), its tick, time of day, camera (overhead or follow, a view key or a point to look at, yaw, zoom; or a free pose
    /// camera), what the player has done (agent, armed verb, cursor, Intel) and its capture: a HudToolkit shot (frame, HUD layer, manifest, parts
    /// for tools/hud-budget, and the layout audit; PNG), a world-only frame with the HUD hidden (JPEG), or a clip (JPEG frames) of frames at real time. shots.json in the
    /// folder reports each shot (status, files, audit, frame times). Shots that share a start go on in one run while their ticks do not go back.
    /// Nothing here touches a run's rules: orders are the host's own, so every run is an ordinary session and replays. Exits 0 when every shot
    /// was taken and every audit was clean, 1 otherwise, 2 when the file would not read.</summary>
    public sealed class ShotRunner : MonoBehaviour
    {
        public string Path_ = "", Dir = "";
        readonly List<Result> results = new();
        sealed class Result
        {
            public string Name = "", Status = "ok", Error = "", Size = ""; public int Tick, Frames; public bool Audited, AuditClean = true;
            public readonly List<string> Files = new(), Issues = new(); public float MeanMs = -1, P99Ms = -1, GpuMs = -1;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Application.isEditor || Application.isBatchMode) return;
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-shots");
            if (i < 0 || i + 2 >= a.Length) return;
            var go = new GameObject("shot-runner"); DontDestroyOnLoad(go);
            var r = go.AddComponent<ShotRunner>(); r.Path_ = a[i + 1]; r.Dir = a[i + 2];
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir);
            ShotFile file;
            try { file = JsonUtility.FromJson<ShotFile>(File.ReadAllText(Path_)); if (file == null || file.shots == null) throw new FormatException("no shots"); }
            catch (Exception e) { Write("unreadable: " + e.Message); Application.Quit(2); yield break; }
            HandoverHost h = null; float t0 = Time.realtimeSinceStartup;
            while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) { if (Time.realtimeSinceStartup - t0 > 60f) { Write("no host after 60 s"); Application.Quit(2); yield break; } yield return null; }
            yield return new WaitForSecondsRealtime(3f); // the HUD built, the briefing up, the world streamed in
            var keep = h.controls.conversations; h.controls.conversations = ConversationScenes.Never; // orders go in as the player's, with no scene to wait for
            string runKey = null; List<Order> orders = null; int oi = 0, runStart = 0;
            foreach (var s in file.shots)
            {
                var res = new Result { Name = s.name }; results.Add(res);
                string error = null;
                // 0 · the capture size: 1920 x 1080 unless the row says (CaptureSize; -capture-native leaves the display's own)
                if (!GameBoot.Arg("-capture-native"))
                {
                    yield return CaptureSize.Apply(s.w, s.h);
                    if (Screen.width != s.w || Screen.height != s.h) { Fail(res, $"wanted {s.w}x{s.h}, the player is {Screen.width}x{Screen.height}: the display would not give that size"); continue; }
                }
                res.Size = $"{Screen.width}x{Screen.height}";
                // 1 · the run: the plain start, a script, or a checkpoint; continued while the shots share it and the ticks do not go back
                string key = s.scriptPath + "|" + s.checkpoint + "|" + s.preset;
                if (runKey != key || h.Game == null || Math.Max(s.tick, runStart) < h.Game.Tick)
                {
                    error = Guard(() =>
                    {
                        h.Fights.checkpoint = s.checkpoint;
                        if (s.checkpoint.Length > 0 && h.Fights.preset != s.preset) h.Fights.Load(s.preset);
                        h.Begin();
                        h.IntelOpen = false;
                        orders = null; oi = 0;
                        if (s.scriptPath.Length > 0) { var sc = Suborned.Sim.Script.Parse(File.ReadAllText(s.scriptPath)); orders = sc.Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).ToList(); }
                        runKey = key; runStart = h.Game.Tick;
                    });
                    yield return null;
                }
                if (error == null) error = Guard(() =>
                {
                    if (s.briefing) { h.ShowBriefing = true; } else { h.StartClock(); }
                    h.SetPaused(true);
                    if (orders != null)
                        while (oi < orders.Count && orders[oi].Tick <= s.tick && !h.Game.Over)
                        {
                            var o = orders[oi++]; if (o.Tick > h.Game.Tick) h.RunTo(o.Tick);
                            if (h.Game.Over) break;
                            var c = Copy(o); if (c.Do is "watch" or "listen" or "turn" or "call") h.GiveScripted(c); else h.Issue(c);
                        }
                    if (s.tick > h.Game.Tick && !h.Game.Over) h.RunTo(s.tick);
                    res.Tick = h.Game.Tick;
                    if (h.Game.Over && h.Game.Tick < s.tick) res.Error = $"the run ended at tick {h.Game.Tick} ({h.Game.Result}), before tick {s.tick}";
                    if (s.setNight) h.SetNight(s.night);
                    h.IntelOpen = s.intel;
                    HandoverHost.PointerOverride = s.hasPointer ? new Vector2(Screen.width * s.px, Screen.height * s.py) : (Vector2?)null;
                    h.ClearHover();
                    if (s.select.Length > 0) h.Select(Suborned.Sim.Op.Parse(s.select), false);
                    if (!s.pose.on) Camera_(h, s);
                    HudToolkit.ActMarkOff = !s.mark;
                    if (s.hover.Length > 0) // the pointer on a view key (after the camera stands): what a hover shows
                    {
                        var hv = h.Views.Find(s.hover); if (hv == null) throw new InvalidOperationException($"no view '{s.hover}' to hover");
                        float up = s.hover.StartsWith("car/") ? 0.8f : s.hover.StartsWith("camera/") ? 0f : 1.0f;
                        var hp = h.View.WorldToScreenPoint(hv.position + Vector3.up * up); HandoverHost.PointerOverride = new Vector2(hp.x, hp.y);
                    }
                    if (s.arm.Length > 0)
                    {
                        var plate = h.PlateOf(Suborned.Sim.Op.Parse(s.select)); int slot = Array.FindIndex(plate, x => x.Id == s.arm);
                        if (slot < 0) throw new InvalidOperationException($"no verb '{s.arm}' on the {s.select}'s plate ({string.Join(", ", plate.Select(x => x.Id))})");
                        h.Pick(slot);
                    }
                });
                if (error != null) { Fail(res, error); continue; }
                // 2 · frame times with the clock running (it advances the game by that many seconds)
                if (s.perf > 0)
                {
                    var ds = new List<float>(); var gpu = new float[1];
                    h.SetPaused(false); yield return new WaitForSecondsRealtime(1f); yield return GameProof.Sample(s.perf, ds, gpu); h.SetPaused(true);
                    res.Frames = ds.Count; res.MeanMs = ds.Average(); res.P99Ms = ds[(int)(ds.Count * 0.99f)]; res.GpuMs = gpu[0]; res.Tick = h.Game.Tick;
                }
                yield return new WaitForSecondsRealtime(s.settle); // the camera's ease, the HUD's fades
                // 3 · the capture
                var path = System.IO.Path.Combine(Dir, s.name);
                if (s.clipSeconds > 0) { yield return Clip(h, s, res, path); }
                else if (s.pose.on) { error = Guard(() => { RenderPose(h, s, path + Ext(s)); res.Files.Add(s.name + Ext(s)); }); }
                else if (s.hud)
                {
                    while (HudToolkit.ShotBusy) yield return null;
                    var q = HudToolkit.Shoot(path, s.state);
                    if (q != "queued") error = "the HUD capture said: " + q;
                    else
                    {
                        yield return null; while (HudToolkit.ShotBusy) yield return null;
                        foreach (var ext in new[] { ".png", ".hud.png", ".manifest.json", ".parts.json" }) if (File.Exists(path + ext)) res.Files.Add(s.name + ext);
                        var bad = HudToolkit.AuditText(); res.Audited = true; res.AuditClean = bad.Count == 0; res.Issues.AddRange(bad);
                    }
                }
                else
                {
                    var docs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None); var shown = docs.Select(d => d.rootVisualElement.style.display).ToArray();
                    foreach (var d in docs) d.rootVisualElement.style.display = DisplayStyle.None;
                    yield return null; yield return new WaitForEndOfFrame();
                    error = Guard(() => { var t = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(path + Ext(s), Encode(t, s)); Destroy(t); res.Files.Add(s.name + Ext(s)); });
                    for (int k = 0; k < docs.Length; k++) if (docs[k] != null) docs[k].rootVisualElement.style.display = shown[k];
                }
                if (s.arm.Length > 0) h.Cancel();
                if (error != null) Fail(res, error);
                else if (res.Error.Length > 0) res.Status = "note";
                else if (res.Audited && !res.AuditClean) res.Status = "audit";
            }
            h.controls.conversations = keep; HandoverHost.PointerOverride = null; HudToolkit.ActMarkOff = false;
            bool ok = results.All(r => r.Status is "ok" or "note");
            Write(ok ? "ok" : "failed"); yield return new WaitForSecondsRealtime(0.5f); Application.Quit(ok ? 0 : 1);
        }

        /// <summary>The camera a still wants: overhead (a view key or a point to look at, a quarter turn, a zoom) or the follow view behind an agent.</summary>
        static void Camera_(HandoverHost h, ShotSpec s)
        {
            Vector3 At(string k) { var t = h.Views.Find(k); if (t == null) throw new InvalidOperationException($"no view '{k}' (the host's keys: op/scout, waitress, guard, car/getaway, camera/entrance, patrol…)"); return t.position; }
            if (s.follow)
            {
                h.Select(Suborned.Sim.Op.Parse(s.agent), false);
                if (!h.Following) h.FollowToggle();
                if (s.face.Length > 0) h.FollowFace(At(s.face));
                return;
            }
            if (h.Following) h.FollowToggle();
            if (s.yaw >= 0) h.TurnSnap(s.yaw);
            if (s.zoom > 0 && h.View) h.View.orthographicSize = s.zoom;
            if (s.focus.Length > 0) h.LookAt(At(s.focus));
            else if (s.hasAt) h.LookAt(h.frame.World(new Suborned.Sim.Pt((int)s.atX, (int)s.atY)));
        }

        // ---- a free pose camera: the world only (what out/latch/gamecap2.cs did by hand)
        Camera poseCam;
        void RenderPose(HandoverHost h, ShotSpec s, string file)
        {
            if (poseCam == null)
            {
                var go = new GameObject("shot-pose-cam"); poseCam = go.AddComponent<Camera>(); poseCam.nearClipPlane = 0.1f; poseCam.farClipPlane = 600f; poseCam.enabled = false;
                go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = true;
            }
            var p = s.pose; poseCam.fieldOfView = p.fov;
            Vector3 V(float[] a) => new Vector3(a[0], a[1], a[2]);
            if (p.view.Length > 0)
            {
                var t = h.Views.Find(p.view); if (t == null) throw new InvalidOperationException($"no view '{p.view}' for the pose camera");
                poseCam.transform.position = t.TransformPoint(V(p.offset)); poseCam.transform.LookAt(t.TransformPoint(p.lookOffset.Length == 3 ? V(p.lookOffset) : new Vector3(0f, 0.9f, 0f)));
            }
            else { poseCam.transform.position = V(p.pos); poseCam.transform.LookAt(V(p.look)); }
            var rt = new RenderTexture(s.w, s.h, 24); var tex = new Texture2D(s.w, s.h, TextureFormat.RGB24, false);
            poseCam.enabled = true; poseCam.targetTexture = rt; poseCam.Render(); poseCam.targetTexture = null; poseCam.enabled = false;
            var prev = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, s.w, s.h), 0, 0); tex.Apply(); RenderTexture.active = prev;
            File.WriteAllBytes(file, Encode(tex, s)); Destroy(tex); rt.Release(); Destroy(rt);
        }

        /// <summary>Frames at real time: the clock runs and each frame advances the game 1/fps (Time.captureFramerate), n seconds of it, into the folder `path`.</summary>
        IEnumerator Clip(HandoverHost h, ShotSpec s, Result res, string path)
        {
            Directory.CreateDirectory(path);
            int fps = Mathf.Clamp(s.clipFps, 5, 60), n = Mathf.CeilToInt(s.clipSeconds * fps);
            var docs = s.hud ? new UIDocument[0] : FindObjectsByType<UIDocument>(FindObjectsSortMode.None); var shown = docs.Select(d => d.rootVisualElement.style.display).ToArray();
            foreach (var d in docs) d.rootVisualElement.style.display = DisplayStyle.None;
            Time.captureFramerate = fps; h.SetPaused(false); yield return null;
            var ds = new List<float>(); string error = null;
            for (int f = 0; f < n && error == null; f++)
            {
                yield return new WaitForEndOfFrame(); ds.Add(Time.unscaledDeltaTime * 1000f);
                var file = System.IO.Path.Combine(path, $"f{f:0000}{Ext(s)}");
                error = Guard(() =>
                {
                    if (s.pose.on) RenderPose(h, s, file);
                    else { var t = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(file, Encode(t, s)); Destroy(t); }
                });
                if (error == null) res.Frames++;
            }
            Time.captureFramerate = 0; h.SetPaused(true);
            for (int k = 0; k < docs.Length; k++) if (docs[k] != null) docs[k].rootVisualElement.style.display = shown[k];
            res.Tick = h.Game.Tick; res.Files.Add(s.name + "/f0000" + Ext(s)); res.Files.Add($"{s.name}/ ({res.Frames} frames at {fps} fps)");
            if (error != null) Fail(res, error);
        }

        /// <summary>A review copy is JPEG (quality 90); a HUD shot, whose frame the budget reads lossless, is PNG.</summary>
        static string Ext(ShotSpec s) => s.jpg ? ".jpg" : ".png";
        static byte[] Encode(Texture2D t, ShotSpec s) => s.jpg ? t.EncodeToJPG(90) : t.EncodeToPNG();

        static Order Copy(Order o) => new() { Tick = o.Tick, Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How, Run = o.Run };

        string Guard(Action a) { try { a(); return null; } catch (Exception e) { return e.GetType().Name + ": " + e.Message; } }
        void Fail(Result r, string why) { r.Status = "failed"; r.Error = why; Debug.LogError($"ShotRunner: {r.Name}: {why}"); }

        static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "'").Replace("\n", " ");
        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        void Write(string status)
        {
            var sb = new StringBuilder();
            sb.Append("{\n \"schema\": \"suborned.shots.report/1\",\n \"status\": \"").Append(Esc(status)).Append("\",\n \"screen\": \"").Append(Screen.width).Append('x').Append(Screen.height)
              .Append("\",\n \"build\": \"").Append(Esc(BuildStamp.Line)).Append("\",\n \"shots\": [");
            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                sb.Append(i == 0 ? "\n" : ",\n").Append("  {\"name\": \"").Append(Esc(r.Name)).Append("\", \"status\": \"").Append(r.Status).Append("\", \"tick\": ").Append(r.Tick)
                  .Append(", \"error\": \"").Append(Esc(r.Error)).Append("\", \"files\": [").Append(string.Join(", ", r.Files.Select(f => "\"" + Esc(f) + "\""))).Append(']');
                if (r.Audited) sb.Append(", \"audit\": {\"clean\": ").Append(r.AuditClean ? "true" : "false").Append(", \"issues\": [").Append(string.Join(", ", r.Issues.Select(x => "\"" + Esc(x) + "\""))).Append("]}");
                if (r.MeanMs >= 0) sb.Append(", \"frames\": {\"count\": ").Append(r.Frames).Append(", \"meanMs\": ").Append(F(r.MeanMs)).Append(", \"fps\": ").Append(F(1000f / r.MeanMs)).Append(", \"p99Ms\": ").Append(F(r.P99Ms)).Append(", \"gpuMs\": ").Append(F(r.GpuMs)).Append('}');
                else if (r.Frames > 0) sb.Append(", \"clipFrames\": ").Append(r.Frames);
                sb.Append('}');
            }
            sb.Append("\n ]\n}\n");
            File.WriteAllText(System.IO.Path.Combine(Dir, "shots.json"), sb.ToString());
        }
    }
}
