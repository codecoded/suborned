using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>The game build at launch (Aster, 2026-10-17; GameBuild in the Editor): full screen at the display's native resolution, or a
    /// window of 80% of it with `-windowed`. The HUD's scale is HudToolkit.CrispScale's whole-number rule (round(height / 1080), at least 1),
    /// so @2x art lands on whole pixels at native Retina. Frames follow the display (vSync 1) unless `-novsync`. The host opens on the briefing (Begin). Flags for the proof (`bun run game` checks):
    /// `-runScenarios &lt;out.json&gt;` plays every scenario against headless.json and quits (SimCheck, the Editor's check); `-gameProof &lt;dir&gt;`
    /// shoots the briefing, play and the pause menu (with their HUD layers, for the budget), samples frame times at play zoom (and at each of
    /// the four yaws the view turns to, brief 016), writes
    /// proof.json and quits through the menu's Quit. Nothing runs in the Editor.</summary>
    public static class GameBoot
    {
        public static bool Arg(string name) => Environment.GetCommandLineArgs().Contains(name);
        public static string ArgValue(string name) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Screen0()
        {
            if (Application.isEditor || Application.isBatchMode) return;
            var r = Screen.currentResolution;
            if (!Arg("-novsync")) QualitySettings.vSyncCount = 1; // the display's own rate, no tearing, no heat for frames nobody sees (-novsync: uncapped, for the proof's headroom)
            if (Arg("-windowed")) Screen.SetResolution(r.width * 4 / 5 / 2 * 2, r.height * 4 / 5 / 2 * 2, FullScreenMode.Windowed);
            else Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
            Debug.Log($"Game: display {r.width}x{r.height} @{r.refreshRateRatio.value:0.#} Hz; {(Arg("-windowed") ? "windowed" : "full screen")}");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Proof()
        {
            if (Application.isEditor || Application.isBatchMode) return;
            var dir = ArgValue("-gameProof"); if (dir == null) return;
            var go = new GameObject("game-proof"); UnityEngine.Object.DontDestroyOnLoad(go); go.AddComponent<GameProof>().Dir = dir;
        }
    }

    /// <summary>`-gameProof &lt;dir&gt;`: the briefing, then play at play zoom, then the pause menu; each a HudToolkit shot (frame, HUD layer,
    /// manifest); frame times over 10 s of play; proof.json; Quit through the menu.</summary>
    public sealed class GameProof : MonoBehaviour
    {
        public string Dir = "";
        readonly List<string> log = new();
        bool rendered;

        /// <summary>The frame is a picture, not one flat colour (a pipeline that failed to start draws only the clear colour).</summary>
        static bool Rendered()
        {
            var t = ScreenCapture.CaptureScreenshotAsTexture(); var seen = new HashSet<Color32>();
            for (int y = 0; y < t.height; y += Mathf.Max(1, t.height / 40)) for (int x = 0; x < t.width; x += Mathf.Max(1, t.width / 40)) seen.Add(t.GetPixel(x, y));
            Destroy(t); return seen.Count > 16;
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir);
            HandoverHost h = null; float t0 = Time.realtimeSinceStartup;
            while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) { if (Time.realtimeSinceStartup - t0 > 60f) { Write(null, "no host after 60 s"); Application.Quit(2); yield break; } yield return null; }
            yield return new WaitForSecondsRealtime(3f); // the HUD built, the briefing up, the world streamed in
            log.Add($"briefing up: {h.ShowBriefing}, modal {h.Modal}, screen {Screen.width}x{Screen.height} {Screen.fullScreenMode}, HUD scale {HudScale()}");
            yield return Shot("briefing", "active");
            yield return new WaitForEndOfFrame(); rendered = Rendered();
            log.Add($"rendered: {rendered}");
            h.StartClock(); // Begin
            yield return new WaitForSecondsRealtime(4f);
            var cam = h.View; log.Add($"play: modal {h.Modal}, paused {h.Paused}, tick {h.Game.Tick}, zoom {(cam ? cam.orthographicSize : -1):0.#}");
            yield return Shot("play", "rest");
            // frame times at play zoom, the clock running
            var dts = new List<float>(); float s0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - s0 < 10f) { yield return null; dts.Add(Time.unscaledDeltaTime * 1000f); }
            dts.Sort(); float mean = dts.Average(), p95 = dts[(int)(dts.Count * 0.95f)], p99 = dts[(int)(dts.Count * 0.99f)];
            int slow = dts.Count(d => d > 1000f / 60f * 1.2f);
            // brief 016: the same at each of the four yaws (the view turned with the host's own snap; 1 s to settle, 6 s sampled each)
            var byYaw = new List<string>();
            for (int k = 0; k < 4; k++)
            {
                h.TurnSnap(k); yield return new WaitForSecondsRealtime(1f);
                var ys = new List<float>(); float y0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - y0 < 6f) { yield return null; ys.Add(Time.unscaledDeltaTime * 1000f); }
                ys.Sort(); float ym = ys.Average();
                byYaw.Add($"{{\"yaw\": {F(h.Yaw)}, \"frames\": {ys.Count}, \"meanMs\": {F(ym)}, \"fps\": {F(1000f / ym)}, \"p95Ms\": {F(ys[(int)(ys.Count * 0.95f)])}, \"p99Ms\": {F(ys[(int)(ys.Count * 0.99f)])}, \"over20msFrames\": {ys.Count(d => d > 1000f / 60f * 1.2f)}}}");
                log.Add($"yaw {h.Yaw:0}: {1000f / ym:0.#} fps mean");
            }
            h.TurnSnap(0);
            var frames = $"{{\"seconds\": 10, \"frames\": {dts.Count}, \"meanMs\": {F(mean)}, \"fps\": {F(1000f / mean)}, \"p95Ms\": {F(p95)}, \"p99Ms\": {F(p99)}, \"over20msFrames\": {slow}, \"vSyncCount\": {QualitySettings.vSyncCount}, \"targetFrameRate\": {Application.targetFrameRate}, \"zoom\": {F(cam ? cam.orthographicSize : -1)}, \"byYaw\": [{string.Join(", ", byYaw)}]}}";
            h.OpenMenu(); yield return null;
            log.Add($"menu: open {h.MenuOpen}, modal {h.Modal}, paused {h.Paused}");
            yield return Shot("menu", "active");
            Write(frames, rendered ? "ok" : "blank");
            yield return new WaitForSecondsRealtime(0.5f);
            if (rendered) h.QuitFromMenu(); else Application.Quit(3);
        }

        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        static float HudScale() => FindAnyObjectByType<UnityEngine.UIElements.UIDocument>() is { } d && d.panelSettings != null ? d.panelSettings.scale : -1;

        IEnumerator Shot(string name, string state)
        {
            while (HudToolkit.ShotBusy) yield return null;
            var r = HudToolkit.Shoot(Path.Combine(Dir, name), state); log.Add($"{name}: {r}");
            yield return null; while (HudToolkit.ShotBusy) yield return null;
        }

        void Write(string frames, string status)
        {
            var esc = string.Join(", ", log.Select(l => "\"" + l.Replace("\\", "\\\\").Replace("\"", "'") + "\""));
            File.WriteAllText(Path.Combine(Dir, "proof.json"), $"{{\n \"schema\": \"suborned.game-proof/1\",\n \"status\": \"{status}\",\n \"unity\": \"{Application.unityVersion}\",\n \"product\": \"{Application.productName}\",\n \"screen\": \"{Screen.width}x{Screen.height}\",\n \"mode\": \"{Screen.fullScreenMode}\",\n \"hudScale\": {F(HudScale())},\n \"frames\": {frames ?? "null"},\n \"log\": [{esc}]\n}}\n");
        }
    }
}
