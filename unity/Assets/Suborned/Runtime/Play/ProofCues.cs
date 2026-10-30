using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Path = System.IO.Path;

namespace Suborned.Play
{
    /// <summary>`-cueShots &lt;dir&gt; [-capture-size 1920x1080] [-cueOnly range|handover] [-cueFloor r,g,b]` (brief 084): the fight cues in the built player, at real
    /// time. In the range, a busy fight as built and on the pick, each caught as a burst of consecutive frames from a shot; frames per flash and the frame
    /// cost; the capture clock's answer; the edge icons with the hostiles off screen; the panel's clicks. In the Handover, the Blind switch on a rerouted
    /// patrol. cues.json says what each found. Its own flag, its own runner: no proof section runs it.</summary>
    public sealed class ProofCues : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (GameBoot.ArgValue("-cueShots") is string d) { Application.runInBackground = true; FightCues.ForceLab = true; var go = new GameObject("cue-shots"); DontDestroyOnLoad(go); go.AddComponent<ProofCues>().Dir = d; } }

        string Dir = ""; readonly List<string> log = new(); static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        void Say(string s) { log.Add(s); Debug.Log("cues: " + s); File.WriteAllLines(Path.Combine(Dir, "cues.log"), log); }

        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir); float t0 = Time.realtimeSinceStartup; Say($"start, frame {Time.frameCount}");
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; Application.runInBackground = true;
            HandoverHost h; while ((h = FindAnyObjectByType<HandoverHost>()) == null && Time.realtimeSinceStartup - t0 < 90f) yield return null;
            Say($"host found {h != null}, {Screen.width}x{Screen.height}"); yield return CaptureSize.Apply(1920, 1080); Say($"sized {Screen.width}x{Screen.height}");
            var only = GameBoot.ArgValue("-cueOnly") ?? "";
            if (only != "handover")
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("FightRange"); t0 = Time.realtimeSinceStartup;
                while ((h = FindAnyObjectByType<HandoverHost>()) == null || !h.rangeMode || h.Game == null) { if (Time.realtimeSinceStartup - t0 > 60f) { Say("no range host"); Application.Quit(2); yield break; } yield return null; }
                yield return new WaitForSecondsRealtime(1.5f);
                if (GameBoot.ArgValue("-cueFloor") is string fl) { var p = fl.Split(','); FightCues.V.floorR = float.Parse(p[0], CultureInfo.InvariantCulture); FightCues.V.floorG = float.Parse(p[1], CultureInfo.InvariantCulture); FightCues.V.floorB = float.Parse(p[2], CultureInfo.InvariantCulture); FightCues.FloorVersion++; }
                Say($"range up: {Screen.width}x{Screen.height}, one input {h.OneInput}, panels at {string.Join(" ", RangePanel.Rects())}, opened on the pick {FightCues.AllPick}, floor paving {FightCues.V.paving}");
                yield return Panel(h);
                RangePanel.Folded = true;
                yield return Fight(h, false, "built");
                yield return Fight(h, true, "pick");
                yield return Clock();
                yield return Edge(h);
                RangePanel.Folded = false; FightCues.SetAll(true);
                yield return Fresh(h, "open", false); Spawn(h, "police", "eff", 3); yield return new WaitForSecondsRealtime(3f); Say($"panels at {string.Join(" ", RangePanel.Rects())}, controls {FightCuesPanel.Controls.Count}"); yield return Burst("panel", 1);
            }
            if (only != "range")
            {
                if (only != "handover") { UnityEngine.SceneManagement.SceneManager.LoadScene(FrontEnd.GameScene); t0 = Time.realtimeSinceStartup; while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.rangeMode || h.Game == null) { if (Time.realtimeSinceStartup - t0 > 120f) { Say("no Handover host"); Application.Quit(2); yield break; } yield return null; } }
                yield return new WaitForSecondsRealtime(2f);
                yield return Blind(h);
            }
            Say("done"); yield return new WaitForSecondsRealtime(0.3f); Application.Quit(0);
        }

        static bool Spawn(HandoverHost h, string what, string to, int n) => h.Issue(new Order { Do = "spawn", Target = what, To = to, Count = n });
        static IEnumerator Fresh(HandoverHost h, string layout, bool paused)
        {
            HandoverHost.PointerOverride = null; h.ClearHover(); h.rangeLayout = layout; h.Begin(); yield return null; yield return null;
            h.StartClock(); h.SetPaused(paused); if (h.View) h.View.orthographicSize = 10f;
        }

        /// <summary>Consecutive frames at real time, each read at the end of its frame; encoded after, so the writing never slows the burst.</summary>
        IEnumerator Burst(string name, int n)
        {
            var got = new List<Texture2D>(); var ms = new List<float>(); float last = Time.realtimeSinceStartup;
            for (int i = 0; i < n; i++) { yield return new WaitForEndOfFrame(); got.Add(ScreenCapture.CaptureScreenshotAsTexture()); ms.Add((Time.realtimeSinceStartup - last) * 1000f); last = Time.realtimeSinceStartup; yield return null; }
            for (int i = 0; i < got.Count; i++) { File.WriteAllBytes(Path.Combine(Dir, n > 1 ? $"{name}-{i:00}.png" : name + ".png"), got[i].EncodeToPNG()); Destroy(got[i]); }
            Say($"{name}: {n} frames, ms apart {string.Join(" ", ms.Select(x => x.ToString("0", CultureInfo.InvariantCulture)))}");
        }

        static int ShotsIn(Game g, ref int from) { int n = 0; for (; from < g.Log.Count; from++) if (g.Log[from].Code.StartsWith("shot.")) n++; return n; }

        /// <summary>The same busy fight: three police at the effective ring, three rivals in cover, the four drawn, the Enforcer on a named target.</summary>
        IEnumerator Fight(HandoverHost h, bool pick, string name)
        {
            FightCues.SetAll(pick); FightCues.CostTicks = 0; FightCues.CostFrames = 0;
            yield return Fresh(h, "open", false); var g = h.Game;
            bool s1 = Spawn(h, "police", "eff", 3), s2 = Spawn(h, "rivals", "cover", 3);
            for (int k = 0; k < 4; k++) if (!g.Ops[k].Gun.Drawn) h.DrawOrHolsterAgent(k);
            h.Select(OpId.Fixer, false);
            h.LookAt(h.frame.World(g.RangeMark) + new Vector3(9f, 0f, 0f));
            yield return null;
            var first = g.RangeHostiles().FirstOrDefault(x => !x.Gone)?.Id ?? ""; h.Issue(new Order { Op = "enforcer", Do = "fire", Target = first });
            int from = g.Log.Count, shots = 0; float t0 = Time.realtimeSinceStartup;
            while (shots < 2 && Time.realtimeSinceStartup - t0 < 8f) { shots += ShotsIn(g, ref from); yield return null; }
            // measured with nothing capturing: 3 s of the fight at real time
            int fl0 = HudToolkit.ShotFlashes; shots = 0; int frames = 0; float m0 = Time.realtimeSinceStartup; var dts = new List<float>();
            while (Time.realtimeSinceStartup - m0 < 3f) { shots += ShotsIn(g, ref from); frames++; dts.Add(Time.unscaledDeltaTime * 1000f); yield return null; }
            float secs = Time.realtimeSinceStartup - m0; dts.Sort();
            Say($"{name}: spawned {s1} {s2}, one input {h.OneInput}, fight on {g.FightOn}, free {g.WeaponsFree}; over {F(secs)} s: {frames} frames, mean {F(secs * 1000f / frames)} ms, p95 {F(dts[(int)(dts.Count * 0.95f)])} ms, {shots} shots, {HudToolkit.ShotFlashes - fl0} flash frames ({F(shots > 0 ? (HudToolkit.ShotFlashes - fl0) / (float)shots : 0f)} a shot); the cues' own time {F(FightCues.CostMs)} ms a frame ({FightCues.CostFrames} frames)");
            string agents = string.Join(" ", Enumerable.Range(0, 4).Select(k => $"{Op.Name((OpId)k)}:{(g.Ops[k].Down ? "down" : h.FireNow[k].Mode + ">" + h.FireNow[k].Target)}")); Say($"{name}: fire states {agents}; hostiles up {g.RangeHostiles().Count(x => !x.Down && !x.Gone)}; flinches {h.BodyFlinches}; reticles {HudToolkit.ReticlesShown}");
            // a burst from the next frame with a hit on a fresh shot
            int stop = Mathf.Max(0, h.ShotFx.Count); t0 = Time.realtimeSinceStartup; bool caught = false;
            while (Time.realtimeSinceStartup - t0 < 6f) { if (ShotsIn(g, ref from) > 0 && h.ShotFx.Count > 0 && Time.unscaledTime - h.ShotFx[h.ShotFx.Count - 1].born < 0.05f && h.ShotFx[h.ShotFx.Count - 1].hit) { caught = true; break; } yield return null; }
            yield return Burst(name + "-busy", 16); Say($"{name}: burst from a hit: {caught}");
        }

        /// <summary>The capture clock: with Time.captureFramerate set, does unscaledTime follow the game clock or the wall? A 120 ms stall a frame stands for a heavy capture.</summary>
        IEnumerator Clock()
        {
            for (int mode = 0; mode < 2; mode++)
            {
                Time.captureFramerate = mode == 1 ? 30 : 0; yield return null; yield return null;
                float u0 = Time.unscaledTime, t0 = Time.time, w0 = Time.realtimeSinceStartup; const int N = 12;
                for (int i = 0; i < N; i++) { System.Threading.Thread.Sleep(120); yield return null; }
                Say($"clock, captureFramerate {(mode == 1 ? 30 : 0)}, {N} frames each stalled 120 ms: wall {F(Time.realtimeSinceStartup - w0)} s, unscaledTime {F(Time.unscaledTime - u0)} s, time {F(Time.time - t0)} s");
            }
            Time.captureFramerate = 0;
        }

        /// <summary>The hostiles off screen, the camera panned away: the one list, then a click on a chip jumps the camera and gives no order.</summary>
        IEnumerator Edge(HandoverHost h)
        {
            FightCues.SetAll(true); yield return Fresh(h, "open", false); var g = h.Game;
            Spawn(h, "police", "eff", 3); Spawn(h, "rivals", "cover", 2); for (int k = 0; k < 4; k++) if (!g.Ops[k].Gun.Drawn) h.DrawOrHolsterAgent(k);
            h.Select(OpId.Fixer, false);
            yield return new WaitForSecondsRealtime(2.5f);
            h.LookAt(h.frame.World(g.RangeMark) + new Vector3(-9f, 0f, -3f)); yield return new WaitForSecondsRealtime(1f);
            Say($"edge: chips {HudToolkit.EdgeShown}; hostile up {g.RangeHostiles().Count(x => !x.Down && !x.Gone)}");
            yield return Burst("edge", 1);
            if (HudToolkit.EdgeChipScreen is Vector2 sp)
            {
                var c0 = h.View.transform.position; int n0 = g.Orders.Count, w0 = h.WorldPresses;
                MouseAt(sp, false); yield return null; yield return null; MouseAt(sp, true); yield return null; yield return null; MouseAt(sp, false); yield return null; yield return null; yield return null;
                Say($"edge click at {sp}: camera moved {F((h.View.transform.position - c0).magnitude)} m, orders {g.Orders.Count - n0}, presses through {h.WorldPresses - w0}, blocked {HandoverHost.OverHud(sp)}");
                yield return new WaitForSecondsRealtime(0.3f); yield return Burst("edge-after-click", 1);
            }
            else Say("edge: no chip to click");
        }

        static void MouseAt(Vector2 screen, bool left) { if (Mouse.current is Mouse m) InputSystem.QueueStateEvent(m, new MouseState { position = screen, buttons = (ushort)(left ? 1 : 0) }); }

        /// <summary>A click on every row of the cue box reaches no street: pressed through the gate as a mouse would be, no press through, no order, no ring. IMGUI hears only the OS's mouse, so a scripted press cannot throw the switch itself: that is for a person (or a run with a real mouse).</summary>
        IEnumerator Panel(HandoverHost h)
        {
            yield return Fresh(h, "open", true); var g = h.Game; h.Select(OpId.Scout, false); yield return new WaitForSecondsRealtime(0.5f);
            int bad = 0;
            foreach (var kv in FightCuesPanel.Controls.ToList())
            {
                var r = kv.Value; var sp = new Vector2(r.x + r.width * 0.75f, Screen.height - r.center.y);
                bool blocked = HandoverHost.OverHud(sp); int n0 = g.Orders.Count, w0 = h.WorldPresses;
                MouseAt(sp, false); yield return null; yield return null; MouseAt(sp, true); yield return null; yield return null; MouseAt(sp, false); yield return null; yield return null; yield return null;
                bool clean = blocked && h.WorldPresses == w0 && g.Orders.Count == n0 && h.Ring == null; if (!clean) bad++;
                Say($"row {kv.Key}: over the HUD {blocked}, presses through {h.WorldPresses - w0}, orders {g.Orders.Count - n0}, ring {h.Ring != null}");
                h.CloseRing();
            }
            Say($"rows that leaked into the street: {bad} of {FightCuesPanel.Controls.Count}");
            // the switches' effect is read from what each draws: every one flipped in code and its count read
            foreach (var (name, on, off) in new (string, System.Action, System.Action)[] { ("floor grey", () => { FightCues.V.paving = false; FightCues.FloorVersion++; }, () => { FightCues.V.paving = true; FightCues.FloorVersion++; }) }) { on(); yield return new WaitForSecondsRealtime(0.2f); yield return Burst("floor-grey", 1); off(); yield return new WaitForSecondsRealtime(0.2f); }
            FightCues.SetAll(true);
        }

        /// <summary>The Handover with the switch on, as the P panel gives it: a rerouted patrol's cone, as built and hollow; Alt; hover; its last 10 s.</summary>
        IEnumerator Blind(HandoverHost h)
        {
            FightCues.Sync(false); Say($"handover opens as built: any {FightCues.Any}");
            h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null;
            var g = h.Game; var ra = Proof29.Route("route-a"); if (!ra.Exists(o => o.Do == "reroute")) ra = Proof29.Route("route-b");
            int rr = ra.Find(o => o.Do == "reroute")?.Tick ?? 786; Proof29.Play(h, ra, rr + 1, rr + 4);
            for (int i = 0; i < 900 && !(g.PatrolAwayUntil > g.Tick) && !g.Over; i++) h.RunTo(g.Tick + 1);
            h.GiveWatchScripted(OpId.Scout, "patrol"); for (int i = 0; i < 700 && g.Known("patrol") is not { Sight: true } && !g.Over; i++) h.RunTo(g.Tick + 1); // Sky's rule: a watcher never watched has no cone
            h.SetPaused(false); yield return new WaitForSecondsRealtime(0.3f); h.SetPaused(true);
            Say($"patrol away until {g.PatrolAwayUntil}, tick {g.Tick}, known fans [{string.Join(" ", h.CueFans.Select(f => f.Watcher + (f.Live ? "" : ":blind")))}], windows [{string.Join(" ", g.Windows().Select(w => w.ToString()))}]");
            var pt = h.Views.Find("patrol"); if (pt) { h.LookAt(pt.position); h.View.orthographicSize = 12f; }
            yield return new WaitForSecondsRealtime(0.5f); FightCues.Blind = false; yield return new WaitForSecondsRealtime(0.5f); yield return Burst("blind-built", 1);
            FightCues.Blind = true; yield return new WaitForSecondsRealtime(0.5f); Say($"hollow: {HudToolkit.BlindShown}"); yield return Burst("blind-hollow", 1);
            h.PeekLatch = true; yield return new WaitForSecondsRealtime(0.5f); yield return Burst("blind-alt", 1); h.PeekLatch = false;
            if (pt) { HandoverHost.PointerWorld = pt.position + Vector3.up * 1f; yield return new WaitForSecondsRealtime(0.6f); yield return Burst("blind-hover", 1); HandoverHost.PointerWorld = null; }
            h.RunTo(g.PatrolAwayUntil - 50); pt = h.Views.Find("patrol"); if (pt) h.LookAt(pt.position); yield return new WaitForSecondsRealtime(0.5f); Say($"last 10 s: tick {g.Tick}, hollow {HudToolkit.BlindShown}"); yield return Burst("blind-last10", 1);
            FightCues.SetAll(false);
        }
    }
}
