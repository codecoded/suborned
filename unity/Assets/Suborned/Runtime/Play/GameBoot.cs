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
    /// the four yaws the view turns to, brief 016, and in the test follow view at three spots, FollowSpots), writes
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
            // the test follow view (Sky 2026-10-18): frame times behind an agent at the café front, the pickup and mid-fight (FollowSpots)
            var follow = new List<string>();
            yield return FollowSpots(h, Dir, log, follow);
            var frames = $"{{\"seconds\": 10, \"frames\": {dts.Count}, \"meanMs\": {F(mean)}, \"fps\": {F(1000f / mean)}, \"p95Ms\": {F(p95)}, \"p99Ms\": {F(p99)}, \"over20msFrames\": {slow}, \"vSyncCount\": {QualitySettings.vSyncCount}, \"targetFrameRate\": {Application.targetFrameRate}, \"zoom\": {F(cam ? cam.orthographicSize : -1)}, \"byYaw\": [{string.Join(", ", byYaw)}], \"follow\": [{string.Join(", ", follow)}]}}";
            h.OpenMenu(); yield return null;
            log.Add($"menu: open {h.MenuOpen}, modal {h.Modal}, paused {h.Paused}");
            yield return Shot("menu", "active");
            Write(frames, rendered ? "ok" : "blank");
            yield return new WaitForSecondsRealtime(0.5f);
            if (rendered) h.QuitFromMenu(); else Application.Quit(3);
        }

        /// <summary>Route G's orders up to the way out (briefs/013/play/route-g-by-hand.script.json): the fight starts on the walk out with Vale.</summary>
        static readonly Suborned.Sim.Order[] RouteToFight =
        {
            new() { Tick = 0, Op = "tech", Do = "move", To = "cabinet", Run = true }, new() { Tick = 0, Op = "enforcer", Do = "move", Point = new Suborned.Sim.Pt(4850, -2300) },
            new() { Tick = 0, Op = "scout", Do = "move", To = "d:Office approach" }, new() { Tick = 0, Op = "fixer", Do = "move", To = "crew:fixer" },
            new() { Tick = 136, Op = "tech", Do = "move", To = "cabinet" }, new() { Tick = 147, Op = "tech", Do = "loop", Target = "entrance" },
            new() { Tick = 346, Op = "scout", Do = "move", Point = new Suborned.Sim.Pt(4679, -3207) }, new() { Tick = 350, Op = "scout", Do = "watch", Target = "guard" },
            new() { Tick = 550, Op = "scout", Do = "move", To = "crew:scout" }, new() { Tick = 756, Op = "enforcer", Do = "move", Point = new Suborned.Sim.Pt(5175, -2880) },
            new() { Tick = 767, Op = "tech", Do = "reroute", Target = "patrol" }, new() { Tick = 1217, Op = "enforcer", Do = "move", Point = new Suborned.Sim.Pt(5175, -2910) },
            new() { Tick = 1220, Op = "enforcer", Do = "takedown", Target = "guard", How = "knockout" }, new() { Tick = 1280, Op = "enforcer", Do = "enter" },
            new() { Tick = 1280, Op = "tech", Do = "move", To = "crew:tech" }, new() { Tick = 1660, Op = "enforcer", Do = "move", Point = new Suborned.Sim.Pt(2400, -6000) },
        };

        /// <summary>The test follow view at three spots, each a fresh run: the Fixer 8 m out from the café door facing Inez and the front, the Fixer 7 m back from the pickup (the
        /// driver's meeting point) facing Vale's car, and the Enforcer mid-fight on route G's way out. At each: T's toggle, 1 s to settle, 6 s
        /// of frame times with the clock running, a shot (follow-&lt;spot&gt; in dir). Ends on the overhead view. The Editor's scratch driver
        /// runs the same routine.</summary>
        public static IEnumerator FollowSpots(HandoverHost h, string dir, List<string> log, List<string> rows, float seconds = 6f)
        {
            void Arrive(int k, int cap) { int g = 0; while ((h.Game.Ops[k].Walk != null || h.Game.Ops[k].Busy != null) && g++ < cap && !h.Game.Over) h.RunTo(h.Game.Tick + 1); }
            Vector3 At(string key) { var t = h.Views.Find(key); return t ? t.position : Vector3.zero; }
            foreach (var spot in new[] { "cafe", "pickup", "fight" })
            {
                h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true);
                string face; Suborned.Sim.OpId who;
                if (spot == "cafe") { var iz = h.Game.PersonPos(h.Game.Inez).Value; h.Issue(new Suborned.Sim.Order { Op = "fixer", Do = "move", Point = new Suborned.Sim.Pt(iz.X + 800, iz.Y + 250), Run = true }); Arrive(1, 1500); who = Suborned.Sim.OpId.Fixer; face = "waitress"; } // 8 m out in the street (engine +x), facing the café door and Inez
                else if (spot == "pickup") { var md = h.Game.S.At("d:Meet driver"); h.Issue(new Suborned.Sim.Order { Op = "fixer", Do = "move", Point = new Suborned.Sim.Pt(md.X + 500, md.Y - 500), Run = true }); Arrive(1, 1500); who = Suborned.Sim.OpId.Fixer; face = "car/official"; } // 7 m back from the driver's meeting point, his car beyond him
                else
                {
                    foreach (var o in RouteToFight) { h.RunTo(o.Tick); h.Issue(new Suborned.Sim.Order { Op = o.Op, Do = o.Do, To = o.To, Point = o.Point, Target = o.Target, How = o.How, Run = o.Run }); }
                    int g = 0; while (!h.Game.FightOn && g++ < 900 && !h.Game.Over) h.RunTo(h.Game.Tick + 1);
                    h.RunTo(h.Game.Tick + 20); who = Suborned.Sim.OpId.Enforcer; face = "car/getaway";
                }
                h.Select(who, false); h.ClearHover();
                if (!h.Following) h.FollowToggle();
                h.FollowFace(At(face));
                log.Add($"follow {spot}: {who} at tick {h.Game.Tick}, fight {h.Game.FightOn}, over {h.Game.Over}, facing {face}");
                h.SetPaused(false);
                yield return new WaitForSecondsRealtime(1f);
                var ds = new List<float>(); float t0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - t0 < seconds) { yield return null; ds.Add(Time.unscaledDeltaTime * 1000f); }
                ds.Sort(); float m = ds.Average();
                rows.Add($"{{\"spot\": \"{spot}\", \"frames\": {ds.Count}, \"meanMs\": {F(m)}, \"fps\": {F(1000f / m)}, \"p95Ms\": {F(ds[(int)(ds.Count * 0.95f)])}, \"p99Ms\": {F(ds[(int)(ds.Count * 0.99f)])}, \"over20msFrames\": {ds.Count(d => d > 1000f / 60f * 1.2f)}, \"fightOn\": {(h.Game.FightOn ? "true" : "false")}}}");
                log.Add($"follow {spot}: {1000f / m:0.#} fps mean, p99 {ds[(int)(ds.Count * 0.99f)]:0.#} ms");
                h.SetPaused(true);
                while (HudToolkit.ShotBusy) yield return null;
                log.Add($"follow-{spot}: " + HudToolkit.Shoot(Path.Combine(dir, "follow-" + spot), "rest"));
                yield return null; while (HudToolkit.ShotBusy) yield return null;
                if (h.Following) h.FollowToggle();
            }
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
