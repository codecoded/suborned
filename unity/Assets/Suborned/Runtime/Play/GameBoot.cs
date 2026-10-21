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
            if (CaptureSize.Captures) Debug.Log($"Game: display {r.width}x{r.height}; a capture run (CaptureSize sizes the window)");
            else if (Arg("-windowed")) Screen.SetResolution(r.width * 4 / 5 / 2 * 2, r.height * 4 / 5 / 2 * 2, FullScreenMode.Windowed);
            else Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
            Debug.Log($"Game: display {r.width}x{r.height} @{r.refreshRateRatio.value:0.#} Hz; {(Arg("-windowed") ? "windowed" : "full screen")}");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Proof()
        {
            if (Application.isEditor || Application.isBatchMode) return;
            if (ArgValue("-shots033") is string s33) { var sg = new GameObject("shots-033"); UnityEngine.Object.DontDestroyOnLoad(sg); sg.AddComponent<Shots033>().Dir = s33; return; } // briefs 030, 032, 033 (Latch)
            if (ArgValue("-rangeShots") is string rs) { var sg = new GameObject("range-shots"); UnityEngine.Object.DontDestroyOnLoad(sg); sg.AddComponent<RangeShots>().Dir = rs; return; } // brief 029, engine 0.18 (Latch)
            if (ArgValue("-shots031") is string sd) { var sg = new GameObject("shots-031"); UnityEngine.Object.DontDestroyOnLoad(sg); sg.AddComponent<Shots031>().Dir = sd; return; } // brief 031's shots (Latch)
            if (ArgValue("-drawCheck") is string dc) { var dg = new GameObject("draw-check"); UnityEngine.Object.DontDestroyOnLoad(dg); dg.AddComponent<DrawCheck>().Dir = dc; return; } // Splice 2026-10-21: the short blank-frame check `bun run game` takes after a rebuild
            var dir = ArgValue("-gameProof"); if (dir == null) return;
            var go = new GameObject("game-proof"); UnityEngine.Object.DontDestroyOnLoad(go); go.AddComponent<GameProof>().Dir = dir;
        }
    }

    /// <summary>`-drawCheck &lt;dir&gt;` (Splice, 2026-10-21): the short check that a fresh build draws, for `bun run game` (a second player build in one
    /// Editor domain can ship a black screen; GameBuild.cs). Waits for the host, 3 s for the world to stream in, tests one frame the way GameProof does
    /// (Rendered), writes proof.json (status ok or blank; same schema name, no frames) and quits. No shots, no frame times: `-gameProof` is the full proof.</summary>
    public sealed class DrawCheck : MonoBehaviour
    {
        public string Dir = "";

        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir);
            HandoverHost h = null; float t0 = Time.realtimeSinceStartup;
            while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) { if (Time.realtimeSinceStartup - t0 > 60f) { Write("no host after 60 s"); Application.Quit(2); yield break; } yield return null; }
            yield return new WaitForSecondsRealtime(3f);
            yield return new WaitForEndOfFrame();
            bool ok = GameProof.Rendered();
            Write(ok ? "ok" : "blank");
            Application.Quit(ok ? 0 : 3);
        }

        void Write(string status) => File.WriteAllText(Path.Combine(Dir, "proof.json"), $"{{\n \"schema\": \"suborned.draw-check/1\",\n \"status\": \"{status}\",\n \"unity\": \"{Application.unityVersion}\",\n \"screen\": \"{Screen.width}x{Screen.height}\"\n}}\n");
    }

    /// <summary>`-gameProof &lt;dir&gt;`: the briefing, then play at play zoom, then the pause menu; each a HudToolkit shot (frame, HUD layer,
    /// manifest); frame times over 10 s of play; proof.json; Quit through the menu.</summary>
    public sealed class GameProof : MonoBehaviour
    {
        public string Dir = "";
        readonly List<string> log = new();
        bool rendered;

        /// <summary>The frame is a picture, not one flat colour (a pipeline that failed to start draws only the clear colour).</summary>
        internal static bool Rendered()
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
            h.controls.conversations = ConversationScenes.Never; // brief 032: the proof measures play, never a conversation cutaway (its café spot played one)
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
            // brief 025: take control of the Scout at street level (frame times while driven), E on Inez, let go; the session replays headless (game:check)
            var drive = new List<string>(); var session = new string[1];
            yield return DriveSpot(h, Dir, log, drive, session);
            // Latch 2026-10-21: the input checks (a ground click while driven, WASD taking the stick back; a fire order stopping a walk)
            var driveClick = new string[] { "null" }; var fireStop = new string[] { "null" };
            ProofInput.Focusless(true); log.Add($"input checks: focused {Application.isFocused}, background {UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior}");
            yield return ProofInput.DriveClick(h, log, driveClick);
            yield return ProofInput.FireStop(h, log, fireStop);
            // brief 041: the ring-on-press and hold-ring checks are the Built scheme's (0.2.34 as it was); the Double click scheme (the default) has its own (dblClick)
            h.controls.preset = ControlPreset.Built; h.ControlsChanged(); yield return null;
            var ringCheck = new string[] { "null" }; yield return ProofInput.RingCheck(h, log, ringCheck); // brief 038's done-check
            var act = new string[] { "null" }; yield return ProofFire.Act(h, Dir, log, act);
            h.controls.preset = ControlPreset.DoubleClick; h.ControlsChanged(); yield return null;
            // brief 024 (action-game controls, firing): the input map, the act on the prompt, the trigger and its marks and animation, the cars
            var inputMap = new string[] { "null" }; ProofFire.InputMap(log, inputMap);
            var dblClick = new string[] { "null" }; yield return ProofInput.DblClick(h, Dir, log, dblClick); // brief 041: single and double clicks on a person, a prop and the ground; the right tap and hold
            var fire = new string[] { "null" }; var fireSession = new string[1]; yield return ProofFire.Fire(h, Dir, log, fire, drive, fireSession);
            var cars = new string[] { "null" }; yield return ProofFire.Cars(h, Dir, log, cars);
            var hudClick = new string[] { "null" }; yield return ProofInput.HudClick(h, log, hudClick); // Sky on 0.2.34: a HUD click never reaches the street (the range's half runs last, below)
            ProofInput.Focusless(false);
            // brief 020: what the people cost: the opening (the square and its crowd) with the figures, then with capsules only, the same moment
            var people = new List<string>();
            yield return PeopleCost(h, log, people, 6f, Dir);
            var frames = $"{{\"seconds\": 10, \"frames\": {dts.Count}, \"meanMs\": {F(mean)}, \"fps\": {F(1000f / mean)}, \"p95Ms\": {F(p95)}, \"p99Ms\": {F(p99)}, \"over20msFrames\": {slow}, \"vSyncCount\": {QualitySettings.vSyncCount}, \"targetFrameRate\": {Application.targetFrameRate}, \"zoom\": {F(cam ? cam.orthographicSize : -1)}, \"byYaw\": [{string.Join(", ", byYaw)}], \"follow\": [{string.Join(", ", follow)}], \"people\": [{string.Join(", ", people)}], \"drive\": [{string.Join(", ", drive)}], \"driveSession\": {(session[0] == null ? "null" : "\"" + session[0].Replace("\\", "/") + "\"")}, \"driveClick\": {driveClick[0]}, \"fireStop\": {fireStop[0]}, \"ringCheck\": {ringCheck[0]}, \"inputMap\": {inputMap[0]}, \"act\": {act[0]}, \"fire\": {fire[0]}, \"cars\": {cars[0]}, \"hudClick\": __HUDCLICK__, \"dblClick\": {dblClick[0]}, \"leash\": __LEASH__, \"fireSession\": {(fireSession[0] == null ? "null" : "\"" + fireSession[0].Replace("\\", "/") + "\"")}}}";
            h.OpenMenu(); yield return null;
            log.Add($"menu: open {h.MenuOpen}, modal {h.Modal}, paused {h.Paused}");
            yield return Shot("menu", "active");
            // Sky on 0.2.34, the fight range's panel: its clicks never reach the street (both views). Last, as it leaves the Handover's scene
            h.ResumeFromMenu();
            var rangeClick = new string[] { "null" }; var leash = new string[] { "null" };
            {
                ProofInput.Focusless(true);
                h.SwitchRangeScene(); // the run saved, then the range scene (the pause menu's own switch)
                HandoverHost rh = null; float r0 = Time.realtimeSinceStartup;
                while (((rh = FindAnyObjectByType<HandoverHost>()) == null || !rh.rangeMode || rh.Game == null) && Time.realtimeSinceStartup - r0 < 60f) yield return null;
                if (rh != null && rh.rangeMode && rh.Game != null) { yield return new WaitForSecondsRealtime(1.5f); yield return ProofInput.RangePanelClick(rh, log, rangeClick); yield return ProofInput.Leash(rh, Dir, log, leash); } // brief 041: the leash on the range's open floor
                else log.Add("rangePanelClick: no range host after 60 s");
                ProofInput.Focusless(false);
            }
            frames = frames.Replace("__LEASH__", leash[0]);
            frames = frames.Replace("__HUDCLICK__", $"{{\"ok\": {(hudClick[0].StartsWith("{\"ok\": true") && rangeClick[0].StartsWith("{\"ok\": true") ? "true" : "false")}, \"handover\": {hudClick[0]}, \"range\": {rangeClick[0]}}}");
            Write(frames, rendered ? "ok" : "blank");
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(rendered ? 0 : 3); // the Handover's run was saved by the switch (QuitFromMenu's save)
        }

        /// <summary>Route G's orders up to the way out (briefs/013/play/route-g-by-hand.script.json): the fight starts on the walk out with Vale.</summary>
        internal static readonly Suborned.Sim.Order[] RouteToFight =
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

        /// <summary>Route A (contracts/missions/handover/routes/route-a.json) to the Fixer's call: Inez is turned, then summons the guard to her.</summary>
        internal static readonly Suborned.Sim.Order[] RouteToCall =
        {
            new() { Tick = 0, Op = "scout", Do = "move", To = "d:Courtyard", Run = true }, new() { Tick = 0, Op = "tech", Do = "move", To = "cabinet", Run = true },
            new() { Tick = 0, Op = "fixer", Do = "move", To = "d:Courtyard", Run = true }, new() { Tick = 0, Op = "enforcer", Do = "move", Point = new Suborned.Sim.Pt(4900, -1700) },
            new() { Tick = 54, Op = "scout", Do = "watch", Target = "waitress" }, new() { Tick = 125, Op = "tech", Do = "loop", Target = "entrance" },
            new() { Tick = 254, Op = "scout", Do = "move", To = "beside-inez" }, new() { Tick = 275, Op = "scout", Do = "listen", Target = "waitress" },
            new() { Tick = 695, Op = "fixer", Do = "turn", Target = "waitress", Approach = "help" }, new() { Tick = 745, Op = "tech", Do = "reroute", Target = "patrol" },
            new() { Tick = 1195, Op = "fixer", Do = "turn", Target = "waitress", Approach = "help" }, new() { Tick = 1695, Op = "fixer", Do = "call", Target = "waitress" },
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
                if (spot == "cafe")
                {
                    // brief 022 (Glyph): the café front with the door guard and Inez standing together, so the guards are in frame: route A to
                    // its call (the Fixer turns Inez and calls her; she summons him), the Fixer then walks 8 m out in the street (engine +x)
                    // facing the café door, and the run goes on until the guard stands with her
                    var iz = h.Game.PersonPos(h.Game.Inez).Value; // at her post (tick 0)
                    foreach (var o in RouteToCall) { h.RunTo(o.Tick); h.Issue(new Suborned.Sim.Order { Op = o.Op, Do = o.Do, To = o.To, Point = o.Point, Target = o.Target, Approach = o.Approach, How = o.How, Run = o.Run }); }
                    h.Issue(new Suborned.Sim.Order { Op = "fixer", Do = "move", Point = new Suborned.Sim.Pt(iz.X + 800, iz.Y + 250) }); Arrive(1, 1500);
                    for (int k = 0; k < 1500 && !h.Game.Over && !(h.Game.PersonPose(h.Game.Inez) is { } ip && ip.phase == "together"); k++) h.RunTo(h.Game.Tick + 1);
                    log.Add($"cafe: Inez {h.Game.PersonPose(h.Game.Inez)?.phase} at tick {h.Game.Tick}");
                    who = Suborned.Sim.OpId.Fixer; face = "waitress";
                }
                else if (spot == "pickup") { var md = h.Game.S.At("d:Meet driver"); h.Issue(new Suborned.Sim.Order { Op = "fixer", Do = "move", Point = new Suborned.Sim.Pt(md.X + 500, md.Y - 500), Run = true }); foreach (var (op, dx, dy) in new[] { ("scout", 600, -350), ("tech", 400, -650), ("enforcer", 650, -620) }) h.Issue(new Suborned.Sim.Order { Op = op, Do = "move", Point = new Suborned.Sim.Pt(md.X + dx, md.Y + dy), Run = true }); for (int k = 0; k < 4; k++) Arrive(k, 1500); /* brief 020: the whole cell at the pickup */ who = Suborned.Sim.OpId.Fixer; face = "car/official"; } // 7 m back from the driver's meeting point, his car beyond him
                else
                {
                    foreach (var o in RouteToFight) { h.RunTo(o.Tick); h.Issue(new Suborned.Sim.Order { Op = o.Op, Do = o.Do, To = o.To, Point = o.Point, Target = o.Target, How = o.How, Run = o.Run }); }
                    int g = 0; while (!h.Game.FightOn && g++ < 900 && !h.Game.Over) h.RunTo(h.Game.Tick + 1);
                    h.RunTo(h.Game.Tick + 20); who = Suborned.Sim.OpId.Enforcer; face = "car/getaway";
                }
                h.Select(who, false); h.ClearHover();
                var ds = new List<float>(); var gpu = new float[1];
                // brief 020: the same moment overhead first (play zoom, the agent centred), then behind the agent
                h.LookAt(At("op/" + Suborned.Sim.Op.Name(who))); h.SetPaused(false);
                yield return new WaitForSecondsRealtime(1f); yield return Sample(seconds, ds, gpu);
                rows.Add(Row(spot + "-overhead", ds, gpu[0])); log.Add($"overhead {spot}: {1000f / ds.Average():0.#} fps mean, p99 {ds[(int)(ds.Count * 0.99f)]:0.#} ms, gpu {gpu[0]:0.##} ms");
                h.SetPaused(true); while (HudToolkit.ShotBusy) yield return null;
                log.Add($"overhead-{spot}: " + HudToolkit.Shoot(Path.Combine(dir, "overhead-" + spot), "rest")); yield return null; while (HudToolkit.ShotBusy) yield return null;
                if (!h.Following) h.FollowToggle();
                h.FollowFace(At(face));
                log.Add($"follow {spot}: {who} at tick {h.Game.Tick}, fight {h.Game.FightOn}, over {h.Game.Over}, facing {face}");
                h.SetPaused(false);
                yield return new WaitForSecondsRealtime(1f); yield return Sample(seconds, ds, gpu);
                rows.Add(Row(spot, ds, gpu[0])); log.Add($"follow {spot}: {1000f / ds.Average():0.#} fps mean, p99 {ds[(int)(ds.Count * 0.99f)]:0.#} ms, gpu {gpu[0]:0.##} ms");
                h.SetPaused(true);
                while (HudToolkit.ShotBusy) yield return null;
                log.Add($"follow-{spot}: " + HudToolkit.Shoot(Path.Combine(dir, "follow-" + spot), "rest"));
                yield return null; while (HudToolkit.ShotBusy) yield return null;
                if (h.Following) h.FollowToggle();
            }
            // an avenue: play zoom over the densest stretch of avenue palms (803 WashingtoniaPalm_Street, 19.2k tris, no LODs: the district's
            // biggest frame-time risk), the clock running, 6 s of frames and a shot (overhead-avenue)
            var palms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude).Where(t => t.name == "DK_WashingtoniaPalm_Street").Select(t => t.position).ToList();
            if (palms.Count > 0)
            {
                Vector3 best = palms[0]; int bn = -1;
                for (int i = 0; i < palms.Count; i += 4) { int c = 0; foreach (var q in palms) if ((q - palms[i]).sqrMagnitude < 900f) c++; if (c > bn) { bn = c; best = palms[i]; } }
                h.Begin(); h.IntelOpen = false; h.StartClock(); h.ClearHover(); h.LookAt(best); h.SetPaused(false);
                var da = new List<float>(); var ga = new float[1];
                yield return new WaitForSecondsRealtime(1f); yield return Sample(seconds, da, ga);
                rows.Add(Row("avenue-overhead", da, ga[0])); log.Add($"overhead avenue ({bn} palms within 30 m of {best}): {1000f / da.Average():0.#} fps mean, p99 {da[(int)(da.Count * 0.99f)]:0.#} ms, gpu {ga[0]:0.##} ms");
                h.SetPaused(true); while (HudToolkit.ShotBusy) yield return null;
                log.Add("overhead-avenue: " + HudToolkit.Shoot(Path.Combine(dir, "overhead-avenue"), "rest")); yield return null; while (HudToolkit.ShotBusy) yield return null;
            }
        }

        /// <summary>Brief 025 (take control): a fresh run, the clock running; T on the Scout; the controller walks her to Inez (DriveRoute: the same
        /// camera-relative input the keys give) while 6 s of frame times are taken at street level (drive-street) and a shot (drive-scout); on
        /// arrival the price chip on Inez (drive-e-inez), E (Watch), and T lets go. The session is closed (saved) and its path returned for
        /// game:check to replay headless on every checkpoint.</summary>
        public static IEnumerator DriveSpot(HandoverHost h, string dir, List<string> log, List<string> rows, string[] session)
        {
            h.Begin(); h.IntelOpen = false; h.StartClock();
            h.Select(Suborned.Sim.OpId.Scout, false); h.DriveToggle();
            if (!h.Driving) { log.Add("drive: could not take control"); yield break; }
            var g = h.Game; var route = g.S.Grid.Route(g.Ops[0].Pos, g.S.At("beside-inez"));
            h.DriveRoute = route == null ? null : new List<Suborned.Sim.Pt>(route);
            var ds = new List<float>(); var gpu = new float[1];
            yield return new WaitForSecondsRealtime(1f); yield return Sample(6f, ds, gpu);
            rows.Add(Row("drive-street", ds, gpu[0])); log.Add($"drive street: {1000f / ds.Average():0.#} fps mean, p99 {ds[(int)(ds.Count * 0.99f)]:0.#} ms, gpu {gpu[0]:0.##} ms");
            while (HudToolkit.ShotBusy) yield return null;
            log.Add("drive-scout: " + HudToolkit.Shoot(Path.Combine(dir, "drive-scout"), "rest")); yield return null; while (HudToolkit.ShotBusy) yield return null;
            float t0 = Time.realtimeSinceStartup; while (h.DriveRoute != null && Time.realtimeSinceStartup - t0 < 40f) yield return null;
            t0 = Time.realtimeSinceStartup; while (h.DriveFocus?.Key != "waitress" && Time.realtimeSinceStartup - t0 < 5f) yield return null;
            log.Add($"drive: arrived {h.DriveRoute == null} at tick {g.Tick}, focus {h.DriveFocus?.Kind}/{h.DriveFocus?.Key}, chip {h.HoverIntent?.Label} {h.HoverIntent?.Usd} {h.HoverIntent?.Time}");
            log.Add("drive-e-inez: " + HudToolkit.Shoot(Path.Combine(dir, "drive-e-inez"), "rest")); yield return null; while (HudToolkit.ShotBusy) yield return null;
            int n0 = g.Orders.Count; h.DriveAct(); yield return null;
            var last = g.Orders.Count > n0 ? g.Orders[g.Orders.Count - 1] : default;
            log.Add($"drive E: {(g.Orders.Count > n0 ? $"{last.order.Do} {last.order.Target} ok {last.ok}" : "no order")}");
            yield return new WaitForSecondsRealtime(1f);
            int samples = 0, refused = 0; foreach (var o in g.Orders) if (o.order.Do == "drive" && !o.order.Release) { samples++; if (!o.ok) refused++; }
            int corr = g.DrivePoseOf(Suborned.Sim.OpId.Scout).Corrections;
            h.DriveToggle(); yield return null;
            log.Add($"drive: {samples} samples, {refused} refused, {corr} corrected; let go: driven {g.DrivePoseOf(Suborned.Sim.OpId.Scout).Driven}, overhead {!h.Following}");
            yield return new WaitForSecondsRealtime(1f);
            session[0] = h.SessionPath; h.Begin(); // the restart closes the run at its last tick (saved)
        }

        /// <summary>Mean and p99 frame ms over `seconds`, and the GPU's mean frame ms (FrameTimingManager; -1 when the player has no timing stats).</summary>
        public static IEnumerator Sample(float seconds, List<float> into, float[] gpu)
        {
            into.Clear(); double gsum = 0; int gn = 0; var ft = new FrameTiming[1]; float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < seconds)
            {
                yield return null; into.Add(Time.unscaledDeltaTime * 1000f);
                FrameTimingManager.CaptureFrameTimings(); if (FrameTimingManager.GetLatestTimings(1, ft) == 1 && ft[0].gpuFrameTime > 0) { gsum += ft[0].gpuFrameTime; gn++; }
            }
            into.Sort(); gpu[0] = gn > 0 ? (float)(gsum / gn) : -1f;
        }
        static string Row(string spot, List<float> ds, float gpu) => $"{{\"spot\": \"{spot}\", \"frames\": {ds.Count}, \"meanMs\": {F(ds.Average())}, \"fps\": {F(1000f / ds.Average())}, \"p99Ms\": {F(ds[(int)(ds.Count * 0.99f)])}, \"gpuMs\": {F(gpu)}}}";

        /// <summary>Brief 020: the opening view (the square, the crowd walking) with the figures and with capsules only; overhead.</summary>
        public static IEnumerator PeopleCost(HandoverHost h, List<string> log, List<string> rows, float seconds = 6f, string dir = "")
        {
            // brief 020 (Glint §3, Redline's 4.5 ms for people): the opening square with the figures, with capsules, and the figures with the crowd's
            // shadows off, SSAO off, SMAA off; one change at a time, overhead, the same moment each
            var lib = h.figures; var ds = new List<float>(); var gpu = new float[1]; var cam = h.View;
            var ucd = cam ? cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() : null;
            UnityEngine.Rendering.Universal.ScriptableRendererFeature ssao = null;
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urp)
            {
                var rdf = typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset).GetField("m_RendererDataList", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (rdf?.GetValue(urp) is UnityEngine.Rendering.Universal.ScriptableRendererData[] list) foreach (var rd in list) if (rd != null) foreach (var f in rd.rendererFeatures) if (f != null && f.GetType().Name.Contains("AmbientOcclusion")) ssao = f;
            }
            foreach (var variant in new[] { "figures", "capsules", "no-crowd-shadows", "no-ssao", "no-smaa" })
            {
                h.figures = variant == "capsules" ? null : lib; h.Begin(); h.IntelOpen = false; h.StartClock(); h.RunTo(20);
                if (variant == "no-crowd-shadows") foreach (var r in UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>()) { var a = r.GetComponentInParent<Animator>(); if (a != null && a.transform.parent != null && a.transform.parent.name.StartsWith("crowd/")) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
                if (variant == "no-ssao" && ssao != null) ssao.SetActive(false);
                if (variant == "no-smaa" && ucd != null) ucd.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None;
                yield return new WaitForSecondsRealtime(1f); yield return Sample(seconds, ds, gpu);
                rows.Add(Row("square-" + variant, ds, gpu[0])); log.Add($"square {variant}: {1000f / ds.Average():0.#} fps mean, p99 {ds[(int)(ds.Count * 0.99f)]:0.#} ms, gpu {gpu[0]:0.##} ms{(variant == "no-ssao" && ssao == null ? " (no SSAO feature found)" : "")}");
                if (variant == "figures" && dir.Length > 0) { h.SetPaused(true); while (HudToolkit.ShotBusy) yield return null; log.Add("overhead-square: " + HudToolkit.Shoot(Path.Combine(dir, "overhead-square"), "rest")); yield return null; while (HudToolkit.ShotBusy) yield return null; h.SetPaused(false); }
                if (ssao != null) ssao.SetActive(true); if (ucd != null) ucd.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            }
            // brief 020 (Glint §3): the 50-figure spot. The square shows about 20 figures at the opening, so the same view is filled to 50 visible
            // figures with walking clones of the library's models (on the ground in frame, each with its own animator: what 50 people cost
            // to skin, shadow and draw). Figures there with SMAA on, with SMAA off, with the crowd's shadows off; and the capsules baseline
            // again (no figures at all) so the people's share is the difference.
            var clones = new List<GameObject>();
            foreach (var variant in new[] { "fifty", "fifty-no-smaa", "fifty-no-crowd-shadows", "fifty-capsules" })
            {
                bool caps = variant == "fifty-capsules";
                h.figures = caps ? null : lib; h.Begin(); h.IntelOpen = false; h.StartClock(); h.RunTo(20);
                yield return null; yield return null;
                int visible = 0;
                if (!caps)
                {
                    foreach (var a in UnityEngine.Object.FindObjectsByType<Animator>()) if (a.name == "figure") foreach (var r in a.GetComponentsInChildren<SkinnedMeshRenderer>()) if (r.isVisible) { visible++; break; }
                    int need = Mathf.Max(0, 50 - visible), cols = Mathf.CeilToInt(Mathf.Sqrt(Mathf.Max(1, need)));
                    for (int i = 0; i < need && lib != null && lib.figures.Length > 0 && cam; i++)
                    {
                        var fig = lib.figures[i % lib.figures.Length]; if (fig.model == null) continue;
                        var ray = cam.ViewportPointToRay(new Vector3(0.2f + 0.6f * ((i % cols) + 0.5f) / cols, 0.2f + 0.6f * ((i / cols) + 0.5f) / cols, 0f));
                        if (Mathf.Abs(ray.direction.y) < 1e-3f) continue; var at = ray.origin + ray.direction * (-ray.origin.y / ray.direction.y);
                        var go = UnityEngine.Object.Instantiate(fig.model, at, Quaternion.Euler(0f, i * 47f, 0f)); go.name = "figure-clone";
                        var an = go.GetComponent<Animator>() ?? go.AddComponent<Animator>(); an.runtimeAnimatorController = lib.controller; an.applyRootMotion = false;
                        an.Play(fig.female ? "walk_f" : "walk", 0, (i * 0.137f) % 1f);
                        if (variant == "fifty-no-crowd-shadows") foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        clones.Add(go);
                    }
                    if (variant == "fifty-no-crowd-shadows") foreach (var r in UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>()) { var a = r.GetComponentInParent<Animator>(); if (a != null && a.transform.parent != null && a.transform.parent.name.StartsWith("crowd/")) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
                    if (variant == "fifty-no-smaa" && ucd != null) ucd.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None;
                    yield return null; yield return null; visible = 0;
                    foreach (var a in UnityEngine.Object.FindObjectsByType<Animator>()) if (a.name is "figure" or "figure-clone") foreach (var r in a.GetComponentsInChildren<SkinnedMeshRenderer>()) if (r.isVisible) { visible++; break; }
                }
                yield return new WaitForSecondsRealtime(1f); yield return Sample(seconds, ds, gpu);
                rows.Add(Row("square-" + variant, ds, gpu[0]).Replace("}", $", \"figuresVisible\": {visible}}}")); log.Add($"square {variant}: {visible} figures visible, {1000f / ds.Average():0.#} fps mean, p99 {ds[(int)(ds.Count * 0.99f)]:0.#} ms, gpu {gpu[0]:0.##} ms");
                if (variant == "fifty" && dir.Length > 0) { h.SetPaused(true); while (HudToolkit.ShotBusy) yield return null; log.Add("overhead-square-fifty: " + HudToolkit.Shoot(Path.Combine(dir, "overhead-square-fifty"), "rest")); yield return null; while (HudToolkit.ShotBusy) yield return null; h.SetPaused(false); }
                foreach (var c in clones) if (c) UnityEngine.Object.Destroy(c); clones.Clear();
                if (ucd != null) ucd.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            }
            h.figures = lib; h.Begin();
        }

        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        static float HudScale() => FindAnyObjectByType<UnityEngine.UIElements.UIDocument>() is { } d && d.panelSettings != null ? d.panelSettings.scale : -1;

        IEnumerator Shot(string name, string state)
        {
            while (HudToolkit.ShotBusy) yield return null;
            var r = HudToolkit.Shoot(Path.Combine(Dir, name), state); log.Add($"{name}: {r}");
            yield return null; while (HudToolkit.ShotBusy) yield return null;
        }

        /// <summary>The pictures' size: the picture size when the run's window is another (CaptureSize, one pass), else the screen's.</summary>
        static string Pictures => CaptureSize.Picture is Vector2Int p ? $"{p.x}x{p.y}" : $"{Screen.width}x{Screen.height}";

        void Write(string frames, string status)
        {
            var esc = string.Join(", ", log.Select(l => "\"" + l.Replace("\\", "\\\\").Replace("\"", "'") + "\""));
            File.WriteAllText(Path.Combine(Dir, "proof.json"), $"{{\n \"schema\": \"suborned.game-proof/1\",\n \"status\": \"{status}\",\n \"unity\": \"{Application.unityVersion}\",\n \"product\": \"{Application.productName}\",\n \"screen\": \"{Pictures}\",{(CaptureSize.Run is Vector2Int ? $"\n \"framesAt\": \"{Screen.width}x{Screen.height}\",\n \"framesHudScale\": {F(HudScale())}," : "")}\n \"mode\": \"{Screen.fullScreenMode}\",\n \"hudScale\": {(CaptureSize.Picture is Vector2Int pic ? F(Mathf.Max(1f, Mathf.Round(pic.y / 1080f))) : F(HudScale()))},\n \"frames\": {frames ?? "null"},\n \"log\": [{esc}]\n}}\n");
        }
    }

    /// <summary>`-shots031 &lt;dir&gt;` (brief 031, Latch): day shots of the attention cues in the built player, then Quit. The briefing's Heat chip; the
    /// Watch ring of each agent (Watch picked); the Enforcer standing still 4 m in front of the entrance camera (its lens and the callout at
    /// noticed, wary and reporting), then the response's robots that come for the report (an old robot's own face); a fresh run with the Fixer
    /// standing in the patrol robot's sweep (its stage face and the callout). Frame times at the two play spots. Orders go in as engine
    /// orders, so each run is an ordinary session; shots.json logs what happened.</summary>
    public sealed class Shots031 : MonoBehaviour
    {
        public string Dir = "";
        readonly List<string> log = new(), rows = new();

        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir);
            HandoverHost h = null; float t0 = Time.realtimeSinceStartup;
            while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) { if (Time.realtimeSinceStartup - t0 > 60f) { Write("no host"); Application.Quit(2); yield break; } yield return null; }
            yield return new WaitForSecondsRealtime(3f);
            h.SetNight(false); h.IntelOpen = false; yield return null;
            yield return Shot(h, "01-briefing-heat", "rest");
            h.StartClock(); h.SetPaused(true); yield return new WaitForSecondsRealtime(0.5f);
            var cam = h.View; float play = cam ? cam.orthographicSize : 14f;
            // the Watch ring for each agent, Watch picked (the Fixer's is his fifth verb)
            for (int k = 0; k < 4; k++)
            {
                var id = (Suborned.Sim.OpId)k; h.Select(id, false); var plate = h.PlateOf(id); int slot = Array.FindIndex(plate, x => x.Id == "watch");
                h.Pick(slot); h.ClearHover(); if (h.Views.Find("op/" + Suborned.Sim.Op.Name(id)) is Transform at) h.LookAt(at.position);
                if (cam) cam.orthographicSize = k is 0 or 2 ? 18.5f : 14f; // the Scout's 30 m and the Tech's 24 m at the widest play zoom
                yield return new WaitForSecondsRealtime(0.3f);
                yield return Shot(h, $"0{2 + k}-ring-{Suborned.Sim.Op.Name(id)}", "rest"); h.Cancel();
            }
            if (cam) cam.orthographicSize = play;
            // the entrance camera: the Enforcer idles in front of it
            var g = h.Game; var c = g.S.Cameras.Find(x => x.Id == "entrance"); const string camW = "camera:entrance";
            var spot = Spot(g, c.At, c.Facing, 400, "enforcer", p => g.InSight(camW, p, false) && !g.InSight("guard", p, false) && !g.InSight("patrol", p, false));
            log.Add($"entrance camera spot {spot}");
            if (spot.HasValue)
            {
                h.Issue(new Suborned.Sim.Order { Op = "enforcer", Do = "move", Point = spot.Value }); Arrive(h, 3, 1200);
                h.Select(Suborned.Sim.OpId.Enforcer, false); h.TurnSnap(0); h.LookAt(h.Views.Find("camera/entrance").position);
                foreach (var (stage, name) in new[] { (1, "06-camera-noticed"), (2, "07-camera-wary"), (3, "08-camera-reporting") })
                {
                    int cap = 0; while (h.Cue(camW).stage < stage && cap++ < 900 && !g.Over) { h.RunTo(g.Tick + 1); if (cap % 10 == 0) yield return null; }
                    yield return null; yield return null; log.Add($"{name}: cue {h.Cue(camW)} at {Suborned.Sim.Report.Clock(g.Tick)}, callouts {h.Callouts.Count}");
                    yield return Shot(h, name, "rest");
                    if (stage == 1) { var ds = new List<float>(); var gpu = new float[1]; h.SetPaused(false); yield return GameProof.Sample(6f, ds, gpu); h.SetPaused(true); rows.Add(Row("office-front-yaw30", ds, gpu[0])); }
                }
                // an old robot: the first report brought the patrol robot; a second, from the square's mast (within robot reach of the square's
                // dock), brings a porter from the dock: the Enforcer idles in the mast's sight until one of the response's robots has him
                var mast = g.S.Cameras.Find(x => x.Id == "mast-314");
                var ms = mast == null ? null : Spot(g, mast.At, mast.Facings.Length > 0 ? mast.Facings[0] : mast.Facing, 600, "enforcer", p => g.InSight("camera:mast-314", p, false));
                log.Add($"mast spot {ms}");
                if (ms.HasValue) { h.Issue(new Suborned.Sim.Order { Op = "enforcer", Do = "move", Point = ms.Value }); Arrive(h, 3, 1500); }
                string robot = null; int cap2 = 0;
                while (robot == null && cap2++ < 3000 && !g.Over) { h.RunTo(g.Tick + 1); foreach (var u in g.Units) if (u.Robot && !u.Patrol && h.Cue(u.Id).stage >= 1) { robot = u.Id; break; } if (cap2 % 10 == 0) yield return null; }
                log.Add($"old robot: {robot ?? "none"} at {Suborned.Sim.Report.Clock(g.Tick)}; units {string.Join(",", g.Units.ConvertAll(u => u.Id + ":" + u.St))}");
                if (robot != null && h.Views.Find("unit/" + robot) is Transform rt)
                {
                    yield return null; yield return null; h.Select(Suborned.Sim.OpId.Enforcer, false); h.LookAt(rt.position); yield return Shot(h, "10-robot-old-noticed", "rest");
                    if (cam) { cam.orthographicSize = 7f; yield return Shot(h, "11-robot-old-close", "rest"); cam.orthographicSize = play; }
                }
            }
            // the patrol robot: a fresh run; the Fixer stands 4 m out in front of the middle of its sweep, out of the camera's and the guard's sight
            h.Begin(); yield return null; h.StartClock(); h.SetPaused(true); h.IntelOpen = false; g = h.Game;
            var pp = g.PatrolPose().Value; var mid = Suborned.Sim.G.Lerp(g.S.Stand(g.S.PatrolA), g.S.Stand(g.S.PatrolB), 1, 2);
            var ps = Spot(g, mid, g.S.PatrolFacing, 400, "fixer", p => !g.InSight(camW, p, false) && !g.InSight("guard", p, false));
            log.Add($"patrol spot {ps} (sweep at {pp.pos})");
            if (ps.HasValue)
            {
                h.Issue(new Suborned.Sim.Order { Op = "fixer", Do = "move", Point = ps.Value }); Arrive(h, 1, 1200);
                h.Select(Suborned.Sim.OpId.Fixer, false); h.TurnSnap(0);
                int cap = 0; while (h.Cue("patrol").stage < 1 && cap++ < 1800 && !g.Over) { h.RunTo(g.Tick + 1); if (cap % 10 == 0) yield return null; }
                yield return null; yield return null;
                log.Add($"patrol: cue {h.Cue("patrol")} at {Suborned.Sim.Report.Clock(g.Tick)}, callouts {h.Callouts.Count}");
                if (h.Views.Find("patrol") is Transform pt) h.LookAt(pt.position);
                yield return Shot(h, "12-patrol-noticed", "rest");
                if (cam) { cam.orthographicSize = 7f; yield return Shot(h, "13-patrol-close", "rest"); cam.orthographicSize = play; }
                var ds = new List<float>(); var gpu = new float[1]; h.SetPaused(false); yield return GameProof.Sample(6f, ds, gpu); h.SetPaused(true); rows.Add(Row("patrol-sweep-yaw30", ds, gpu[0]));
                int cap3 = 0; while (h.Cue("patrol").stage < 2 && cap3++ < 600 && !g.Over) { h.RunTo(g.Tick + 1); if (cap3 % 10 == 0) yield return null; }
                yield return null; yield return null; log.Add($"patrol: cue {h.Cue("patrol")} at {Suborned.Sim.Report.Clock(g.Tick)}");
                if (h.Cue("patrol").stage >= 2) { if (h.Views.Find("patrol") is Transform pt2) h.LookAt(pt2.position); yield return Shot(h, "14-patrol-wary", "rest"); }
            }
            Write("ok"); yield return new WaitForSecondsRealtime(0.5f); Application.Quit(0);
        }

        static void Arrive(HandoverHost h, int k, int cap) { int g = 0; while ((h.Game.Ops[k].Walk != null || h.Game.Ops[k].Busy != null) && g++ < cap && !h.Game.Over) h.RunTo(h.Game.Tick + 1); }

        /// <summary>The free ground nearest `cm` out along `facing` from `from` that the agent can walk to and `ok` accepts (within 3 m of it).</summary>
        static Suborned.Sim.Pt? Spot(Suborned.Sim.Game g, Suborned.Sim.Pt from, Suborned.Sim.Pt facing, int cm, string who, Func<Suborned.Sim.Pt, bool> ok)
        {
            double len = Math.Sqrt((double)facing.X * facing.X + (double)facing.Y * facing.Y); if (len < 1) return null;
            var aim = new Suborned.Sim.Pt(from.X + (int)(facing.X / len * cm), from.Y + (int)(facing.Y / len * cm));
            var grid = g.S.Grid; var me = g.Ops[(int)Suborned.Sim.Op.Parse(who)].Pos; Suborned.Sim.Pt? best = null; long bd = long.MaxValue;
            for (int dx = -300; dx <= 300; dx += 25) for (int dy = -300; dy <= 300; dy += 25)
            {
                var p = new Suborned.Sim.Pt(aim.X + dx, aim.Y + dy); long d = (long)dx * dx + (long)dy * dy; if (d >= bd || !grid.Free(p) || !ok(p)) continue;
                if (grid.Route(me, p) == null) continue; best = p; bd = d;
            }
            return best;
        }

        IEnumerator Shot(HandoverHost h, string name, string state)
        {
            yield return new WaitForSecondsRealtime(0.35f); // the callout's 0.15 s fade-in and its 0.1 s ease after a pan or a zoom (it holds 2 s or more)
            while (HudToolkit.ShotBusy) yield return null;
            log.Add($"{name}: " + HudToolkit.Shoot(Path.Combine(Dir, name), state));
            yield return null; while (HudToolkit.ShotBusy) yield return null;
        }

        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        static string Row(string spot, List<float> ds, float gpu) => $"{{\"spot\": \"{spot}\", \"frames\": {ds.Count}, \"meanMs\": {F(ds.Average())}, \"fps\": {F(1000f / ds.Average())}, \"p99Ms\": {F(ds[(int)(ds.Count * 0.99f)])}, \"gpuMs\": {F(gpu)}}}";
        void Write(string status)
        {
            var esc = string.Join(", ", log.Select(l => "\"" + l.Replace("\\", "\\\\").Replace("\"", "'") + "\""));
            File.WriteAllText(Path.Combine(Dir, "shots.json"), $"{{\n \"schema\": \"suborned.shots-031/1\",\n \"status\": \"{status}\",\n \"screen\": \"{Screen.width}x{Screen.height}\",\n \"frames\": [{string.Join(", ", rows)}],\n \"log\": [{esc}]\n}}\n");
        }
    }
}
