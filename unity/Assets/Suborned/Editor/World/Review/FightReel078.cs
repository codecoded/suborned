using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Suborned.Play;
using Suborned.Sim;
using Suborned.World.Play;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace Suborned.World.Review
{
    /// <summary>Brief 078, a played-out fight on film, in Play mode: `-executeMethod Suborned.World.Review.FightReel078.Run -script &lt;route.json&gt;
    /// -out &lt;file.mp4&gt; -from &lt;tick&gt; [-limit &lt;frames&gt;] [-tail &lt;seconds&gt;] [-crf n]` (batchmode, no -quit). The scene is opened and never saved. The route's
    /// orders are given as LateRun gives them (paused to `from`, then at their ticks in the host's per-tick loop); the clock runs on Time.captureDeltaTime,
    /// and the frames are paced to the wall at 1/30 s (the host's clock reads unscaled time, which captureDeltaTime does not move; a frame that ran over is written again), so the film runs at mission time. Capture only: nothing here is compiled into the player.</summary>
    [InitializeOnLoad]
    public static class FightReel078
    {
        const string Key = "fightreel078";
        internal static string Arg(string n) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }

        static FightReel078() { if (SessionState.GetBool(Key, false)) EditorApplication.update += Step; }

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/HarbourDistrict.unity", OpenSceneMode.Single);
            var host = Object.FindFirstObjectByType<HandoverHost>(); if (!host) { Debug.LogError("fightreel: no host in the scene"); EditorApplication.Exit(2); return; }
            if (!host.carFx) host.carFx = CarFxBuild.Ensure();
            host.startPaused = true;
            SessionState.SetBool(Key, true); SessionState.SetInt(Key + ".stage", 0);
            SessionState.SetString(Key + ".script", Path.GetFullPath(Arg("-script") ?? "")); SessionState.SetString(Key + ".out", Path.GetFullPath(Arg("-out") ?? ""));
            SessionState.SetInt(Key + ".from", int.Parse(Arg("-from") ?? "0", CultureInfo.InvariantCulture)); SessionState.SetInt(Key + ".limit", int.Parse(Arg("-limit") ?? "0", CultureInfo.InvariantCulture));
            SessionState.SetFloat(Key + ".tail", float.Parse(Arg("-tail") ?? "10", CultureInfo.InvariantCulture)); SessionState.SetInt(Key + ".crf", int.Parse(Arg("-crf") ?? "18", CultureInfo.InvariantCulture));
            EditorApplication.update += Step;
            EditorApplication.EnterPlaymode();
        }

        static void Step()
        {
            int stage = SessionState.GetInt(Key + ".stage", 0);
            if (EditorApplication.isPlaying)
            {
                if (stage == 0 && Object.FindFirstObjectByType<HandoverHost>() is { } h && h.Game != null)
                {
                    var s = new GameObject("fight-reel").AddComponent<FightReelShots>();
                    s.Script = SessionState.GetString(Key + ".script", ""); s.Out = SessionState.GetString(Key + ".out", ""); s.From = SessionState.GetInt(Key + ".from", 0);
                    s.Limit = SessionState.GetInt(Key + ".limit", 0); s.Tail = SessionState.GetFloat(Key + ".tail", 10f); s.Crf = SessionState.GetInt(Key + ".crf", 18);
                    SessionState.SetInt(Key + ".stage", 1);
                }
                else if (stage == 1 && FightReelShots.Finished) { SessionState.SetInt(Key + ".stage", 2); EditorApplication.ExitPlaymode(); }
                return;
            }
            if (stage != 2) return;
            EditorApplication.update -= Step; SessionState.EraseBool(Key); SessionState.SetInt(Key + ".stage", 3);
            if (Application.isBatchMode) EditorApplication.Exit(FightReelShots.Failed ? 1 : 0);
        }
    }

    /// <summary>LateUpdate last of all: the figures posed, the camera placed; world into ARGBHalf (the grade sees HDR), sRGB by a Blit, read back, the HUD
    /// layer laid over, raw into ffmpeg with ReviewVideo's arguments. The HUD layer has a stencil: its round masks (portraits, the feed's scope) draw white without one.</summary>
    [DefaultExecutionOrder(32000)]
    public sealed class FightReelShots : MonoBehaviour
    {
        const int W = 1920, H = 1080, Fps = 30;
        public static bool Finished, Failed;
        public string Script = "", Out = ""; public int From, Limit, Crf = 18; public float Tail = 10f;

        HandoverHost h; RenderTexture hdr, ldr, hudRt; Texture2D tex, hudTex; Process ffmpeg; Stream video; byte[] buf;
        List<Order> orders; int oi, given, refused, frames, firstTick = -1, overTick = -1, overFrame = -1; bool recording; Vector3 at; string error = "";
        readonly System.Text.StringBuilder csv = new("frame,tick,ms\n"); double lastWall, t0 = -1; int dups;

        void Start() { Finished = Failed = false; StartCoroutine(Guarded(Go())); }

        IEnumerator Guarded(IEnumerator step)
        {
            while (true)
            {
                object cur; try { if (!step.MoveNext()) yield break; cur = step.Current; } catch (Exception e) { Stop("threw: " + e); yield break; }
                yield return cur;
            }
        }

        IEnumerator Go()
        {
            h = FindAnyObjectByType<HandoverHost>();
            for (int i = 0; i < 30; i++) yield return null;
            h.controls.conversations = ConversationScenes.Never; // as -late: orders go in as the player's, with no scene to wait for
            hdr = new RenderTexture(W, H, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear) { name = "reel-hdr" };
            ldr = new RenderTexture(W, H, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "reel-ldr" };
            hudRt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "reel-hud" };
            tex = new Texture2D(W, H, TextureFormat.RGB24, false); hudTex = new Texture2D(W, H, TextureFormat.RGBA32, false); buf = new byte[W * H * 3];
            if (h.View) h.View.targetTexture = hdr;
            bool hud = HudToolkit.HudInto(hudRt); Debug.Log($"fightreel: hud layer {hud}, screen {Screen.width}x{Screen.height}, build {BuildStamp.Line}");
            var sc = Suborned.Sim.Script.Parse(File.ReadAllText(Script));
            HandoverHost.SeedPin = sc.Start.Seed; Debug.Log($"fightreel: seed {sc.Start.Seed}"); // brief 080: the script's own seed: its orders replay on it
            h.Begin(); h.IntelOpen = false; h.SetSpeed(1);
            orders = sc.Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).ToList();
            yield return null;
            h.StartClock(); h.SetPaused(true);
            while (oi < orders.Count && orders[oi].Tick < From && !h.Game.Over) { var o = orders[oi++]; if (o.Tick > h.Game.Tick) h.RunTo(o.Tick); if (h.Game.Over) break; Give(h, o); }
            if (From > h.Game.Tick && !h.Game.Over) h.RunTo(From);
            Debug.Log($"fightreel: warm at tick {h.Game.Tick}, {given} orders, {refused} refused");
            h.TurnSnap(2); // the Records Office front's quarter (yaw 210), a player's Q/E before the fight
            at = Target(); h.LookAt(at);
            Time.captureDeltaTime = 1f / Fps;
            for (int i = 0; i < 20; i++) yield return null; // paused: the views and the HUD settle on the warm tick
            Directory.CreateDirectory(Path.GetDirectoryName(Out)!);
            var si = new ProcessStartInfo(ReviewVideo.Ffmpeg, ReviewVideo.Args(W, H, W, H, Fps, Out, false, Crf, "medium")) { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardError = true, CreateNoWindow = true };
            ffmpeg = Process.Start(si); ffmpeg.ErrorDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) Debug.Log("fightreel ffmpeg: " + e.Data); }; ffmpeg.BeginErrorReadLine(); video = ffmpeg.StandardInput.BaseStream;
            HandoverHost.BeforeStep = host => { while (oi < orders.Count && orders[oi].Tick <= host.Game.Tick && !host.Game.Over) Give(host, orders[oi++]); };
            h.SetPaused(false); recording = true; lastWall = Time.realtimeSinceStartupAsDouble;
        }

        void Give(HandoverHost host, Order o)
        {
            var g = host.Game; int n0 = g.Orders.Count;
            var c = new Order { Tick = o.Tick, Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How, Run = o.Run, Stance = o.Stance, Force = o.Force, On = o.On, Release = o.Release, Face = o.Face, Count = o.Count, Shots = o.Shots, Then = o.Then };
            if (c.Do is "watch" or "listen" or "turn" or "call") host.GiveScripted(c); else host.Issue(c);
            given++; for (int k = n0; k < g.Orders.Count; k++) if (!g.Orders[k].ok) refused++;
        }

        /// <summary>Where a player would keep the view: on whoever is doing the fighting, by the route's own orders (the cell on security, the two on the rivals,
        /// the Enforcer with Vale, all four at the car).</summary>
        Vector3 Target()
        {
            int t = h.Game.Tick;
            if (FightReel078.Arg("-live") != null || Array.IndexOf(Environment.GetCommandLineArgs(), "-live") >= 0) // brief 080: an adaptive plan's fight falls at no fixed tick: whoever has a fire order, else the Enforcer's walk, else the cell
            {
                var liveSum = Vector3.zero; int ln = 0;
                foreach (var w in new[] { OpId.Scout, OpId.Fixer, OpId.Tech, OpId.Enforcer }) { var op = h.Game.Ops[(int)w]; if (!op.Down && !op.InCar && (op.Gun.Order.Length > 0 || op.Gun.Drawn) && h.Views.Find("op/" + Op.Name(w)) is Transform lx && lx) { liveSum += lx.position; ln++; } }
                if (ln == 0 && h.Views.Find("op/enforcer") is Transform ex && ex) { liveSum = ex.position; ln = 1; }
                return ln > 0 ? liveSum / ln : at;
            }
            string[] who = t < 2286 ? new[] { "scout", "fixer", "tech" } : t < 2614 ? new[] { "scout", "fixer" } : t < 2966 ? new[] { "enforcer" } : new[] { "scout", "fixer", "tech", "enforcer" };
            var sum = Vector3.zero; int n = 0; foreach (var w in who) if (h.Views.Find("op/" + w) is Transform x && x) { sum += x.position; n++; }
            return n > 0 ? sum / n : at;
        }

        void LateUpdate()
        {
            if (!recording || h == null || h.Game == null) return;
            try { Frame(); } catch (Exception e) { Stop("frame threw: " + e); }
        }

        void Frame()
        {
            var g = h.Game; if (firstTick < 0) firstTick = g.Tick;
            float dt = 1f / Fps; var d = Target() - at; var step = d * (1f - Mathf.Exp(-dt / 0.6f)); step = Vector3.ClampMagnitude(step, 12f * dt); at += step; h.LookAt(at); // a player's pan: eased, at most 12 m/s
            if (h.Paused && !g.Over) h.SetPaused(false);
            var cam = h.View; cam.targetTexture = hdr; cam.Render(); Graphics.Blit(hdr, ldr);
            var prev = RenderTexture.active;
            RenderTexture.active = ldr; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            RenderTexture.active = hudRt; hudTex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            RenderTexture.active = prev;
            var a = tex.GetRawTextureData<byte>(); var b = hudTex.GetRawTextureData<Color32>();
            a.CopyTo(buf);
            for (int i = 0; i < b.Length; i++)
            {
                int al = b[i].a; if (al == 0) continue; int j = i * 3, ia = 255 - al;
                buf[j] = (byte)((b[i].r * al + buf[j] * ia) / 255); buf[j + 1] = (byte)((b[i].g * al + buf[j + 1] * ia) / 255); buf[j + 2] = (byte)((b[i].b * al + buf[j + 2] * ia) / 255);
            }
            video.Write(buf, 0, buf.Length); frames++;
            // the host's clock and every look clock read the wall (unscaledDeltaTime ignores captureDeltaTime), so the frames are paced to it: 1/30 s of wall a frame, a frame that ran over written again
            double now = Time.realtimeSinceStartupAsDouble; if (t0 < 0) t0 = now;
            while (now > t0 + frames / (double)Fps) { video.Write(buf, 0, buf.Length); frames++; dups++; now = Time.realtimeSinceStartupAsDouble; }
            while (now < t0 + frames / (double)Fps) { if (t0 + frames / (double)Fps - now > 0.002) System.Threading.Thread.Sleep(1); now = Time.realtimeSinceStartupAsDouble; }
            csv.Append(frames - 1).Append(',').Append(g.Tick).Append(',').Append(((now - lastWall) * 1000.0).ToString("0", CultureInfo.InvariantCulture)).Append('\n'); lastWall = now;
            if (frames % 300 == 0) Debug.Log($"fightreel: frame {frames}, tick {g.Tick}, {Time.realtimeSinceStartup:0} s");
            bool over = g.Over || h.Ending.Length > 0;
            if (over && overTick < 0) { overTick = g.Tick; overFrame = frames; }
            if ((Limit > 0 && frames >= Limit) || (overFrame >= 0 && frames - overFrame >= Tail * Fps) || g.Tick > 5100) Stop("");
        }

        void Stop(string why)
        {
            if (!recording && Finished) return;
            recording = false; error = why; Failed = why.Length > 0; if (Failed) Debug.LogError("fightreel: " + why);
            HandoverHost.BeforeStep = null; Time.captureDeltaTime = 0f;
            if (h && h.View) h.View.targetTexture = null;
            HudToolkit.HudInto(null);
            try { if (video != null) { video.Close(); ffmpeg.WaitForExit(); } } catch (Exception e) { Debug.LogError("fightreel: ffmpeg: " + e); Failed = true; }
            var g = h ? h.Game : null; string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "'").Replace("\n", " ");
            string report = $"{{\n \"schema\": \"suborned.fightreel/1\",\n \"build\": \"{Esc(BuildStamp.Line)}\",\n \"script\": \"{Esc(Script)}\",\n \"fps\": {Fps},\n \"frames\": {frames},\n \"repeatedFrames\": {dups},\n \"firstTick\": {firstTick},\n \"lastTick\": {g?.Tick ?? -1},\n \"overTick\": {overTick},\n \"overFrame\": {overFrame},\n \"outcome\": \"{g?.Result}\",\n \"failCode\": \"{Esc(g?.FailCode)}\",\n \"finalHash\": \"{g?.ClosingHash():x16}\",\n \"ordersGiven\": {given},\n \"ordersRefused\": {refused},\n \"error\": \"{Esc(error)}\"\n}}\n";
            File.WriteAllText(Path.ChangeExtension(Out, ".json"), report); File.WriteAllText(Path.ChangeExtension(Out, ".frames.csv"), csv.ToString());
            Debug.Log("fightreel: done " + report);
            Finished = true;
        }

        void OnDestroy() { HandoverHost.BeforeStep = null; Time.captureDeltaTime = 0f; }
    }
}
