using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Order = Suborned.Sim.Order;
using OpId = Suborned.Sim.OpId;
using Pt = Suborned.Sim.Pt;

namespace Suborned.Play
{
    /// <summary>Brief 102, engine 0.51.0 (Sky's calls on the guard): the pictures Aster asked for, in the mission, each with what the ring and the HUD say then.
    ///  1 · the guard's call: the guard turned in play (the Fixer watches him, then pays him), the Fixer's ring on him with its open Call, the call taken from its
    ///      wedge, its scene (the cutaway when the words hold a conversation about him; else the street), and the guard leaving his door for Inez's post;
    ///  2 · a Call shut by violence: the Enforcer draws in front of the turned guard; the ring's Call shut with Fable's call-saw-violence lock, and its press's pop
    ///      (nothing given, no card, no line);
    ///  3 · the Already turned lock: route B's turned driver, the Fixer's pill on him and his click's ring: one shut Turn wedge at 12 o'clock, the turned pictogram;
    ///  4 · brief 101 round 4's verb row with the Action plate on (the pause menu's setting, set here and put back): the Enforcer's row at rest, Fire hovered ("Fire · ⇧2"),
    ///      Knock out armed (deep teal, its name under it).
    /// Not gated (pictures for Sky); `-proofOnly r102`.</summary>
    public static class Proof102
    {
        static string B(bool b) => b ? "true" : "false";
        static string J(string x) => (x ?? "").Replace("\\", "/").Replace("\"", "'").Replace("\n", " ");
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static Vector2 Sp(HandoverHost h, Vector3 w) { var s = h.View.WorldToScreenPoint(w); return new Vector2(s.x, s.y); }
        static Vector2 OffStreet => new(Screen.width * 0.5f, 4f);
        /// <summary>The mouse put at a screen point (as Proof101 moves it): UI Toolkit's hover follows it.</summary>
        static void Btn(Vector2 sp) { if (Mouse.current is Mouse m) InputSystem.QueueStateEvent(m, new MouseState { position = sp }); }
        static IEnumerator Shoot(HandoverHost h, string dir, string name, List<string> log, string state)
        {
            bool was = h.Paused; h.SetPaused(true);
            while (HudToolkit.ShotBusy) yield return null;
            log.Add($"{name}: " + HudToolkit.Shoot(Path.Combine(dir, name), state)); yield return null; while (HudToolkit.ShotBusy) yield return null;
            log.Add($"{name}: ring [{h.RingWords}] pill [{HudToolkit.PillWords}] at {HandoverHost.MissionClock(h.Game.Tick)}");
            h.SetPaused(was);
        }
        static IEnumerator Fresh(HandoverHost h) { HandoverHost.PointerWorld = null; HandoverHost.PointerOverride = null; h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null; yield return null; }
        /// <summary>The clock run in half-second steps until `until` holds (or `max` ticks pass); true when it held.</summary>
        static bool StepUntil(HandoverHost h, Func<bool> until, int max) { var g = h.Game; int end = g.Tick + max; while (!until() && g.Tick < end && !g.Over) h.RunTo(g.Tick + 5); return until(); }
        static bool FixerFree(HandoverHost h) => h.Game.Ops[(int)OpId.Fixer].Busy == null && h.PendingOf((int)OpId.Fixer).verb.Length == 0;
        /// <summary>The guard turned in play: the Fixer watches him (his sight and routine), then pays him ($70,000; his door is in the entrance camera's view, so it is taped).</summary>
        static string TurnGuard(HandoverHost h)
        {
            var g = h.Game;
            h.GiveScripted(new Order { Tick = g.Tick, Op = "fixer", Do = "watch", Target = "guard" });
            if (!StepUntil(h, () => g.GuardP.KRoutine && FixerFree(h), 3000)) return $"the Fixer's Watch on the guard never landed ({h.Status})";
            h.GiveScripted(new Order { Tick = g.Tick, Op = "fixer", Do = "turn", Target = "guard", Approach = "pay" });
            if (!StepUntil(h, () => g.GuardP.Turned && FixerFree(h), 3000)) return $"the Pay never turned him ({h.Status})";
            return "";
        }
        /// <summary>A scene the turn left waiting (the Pay's own) ends before the next picture: 0.2.297's call met the Pay's cutaway, which took the click.</summary>
        static IEnumerator NoScene(HandoverHost h) { for (int i = 0; i < 60 && (h.ConvoPending || h.ConvoOn); i++) { h.ConvoEndNow(); yield return null; } }
        /// <summary>The Fixer alone selected, the pointer on a person's head and a left click there: the ring the click opens (null when none did).</summary>
        static IEnumerator ClickOn(HandoverHost h, string role, bool click = true)
        {
            h.Select(OpId.Fixer, false); yield return Frames(2);
            if (h.Views.Find(role) is not Transform t || !t) yield break;
            h.LookAt(t.position); yield return new WaitForSecondsRealtime(0.6f); yield return Frames(2); var head = t.position + Vector3.up; HandoverHost.PointerOverride = null; HandoverHost.PointerWorld = head; yield return Frames(3); // the camera glides: a ring opened mid-glide stood off its person (0.2.297)
            for (float t0 = Time.unscaledTime; HudToolkit.PillWords.Length == 0 && Time.unscaledTime - t0 < 1.2f;) yield return null; // the pill is priced a moment after the hover lands (0.2.297: 40 quick frames read it empty)
            if (click) { h.LeftClickScripted(Sp(h, head)); yield return Frames(4); }
        }

        public static IEnumerator Run(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var parts = new List<string>(); bool ok = true;
            void Check(string name, bool good, string words) { parts.Add($"\"{name}\": {{\"ok\": {B(good)}, \"words\": \"{J(words)}\"}}"); ok &= good; log.Add($"r102 {name}: {(good ? "ok" : "FAILED")}: {words}"); }
            var keepConvo = h.controls.conversations; h.controls.conversations = ConversationScenes.Never; // scenes only for the guard's call (0.2.297: the Pay's and a Listen's cutaways covered two rings)
            bool keepPlan = h.controls.planTogether; h.controls.planTogether = false; // paused, the hold preset would queue the call as a ghost
            string callWord = h.Text != null && h.Text.Actions.TryGetValue("ring-call", out var rc) && rc.Label.Length > 0 ? rc.Label : "?"; // the ring's word for the Call (Fable's ring-call)

            // 1 · the guard's call (option 1): turned in play, his ring's open Call, the call taken from its wedge, its scene, and his walk off the door
            {
                yield return Fresh(h); var g = h.Game; string turn = TurnGuard(h);
                string ring = "", scene = "none", phase = ""; bool offered = g.CallOffered("guard"), called = false;
                if (turn.Length == 0)
                {
                    yield return NoScene(h); yield return ClickOn(h, "guard"); ring = h.RingWords;
                    yield return Shoot(h, dir, "r102-guard-call-ring", log, "active");
                    int l0 = g.Log.Count; h.controls.conversations = ConversationScenes.Always; h.RingTakeScripted("call", "guard"); yield return Frames(2);
                    called = g.Log.Skip(l0).Any(l => l.Code == "call.guard"); HandoverHost.PointerWorld = null; HandoverHost.PointerOverride = OffStreet;
                    for (int i = 0; i < 30 && h.ConvoPending && !h.ConvoOn; i++) yield return null;
                    if (h.ConvoOn) { scene = $"cutaway {h.ConvoNow.Id}: {h.ConvoNow.Lines.Count} lines"; yield return new WaitForSecondsRealtime(1.2f); yield return Shoot(h, dir, "r102-guard-call-scene", log, "rest"); h.ConvoEndNow(); yield return Frames(3); }
                    else { scene = $"no cutaway ({h.ConvoLast})"; yield return Frames(4); yield return Shoot(h, dir, "r102-guard-call-scene", log, "rest"); }
                    h.controls.conversations = ConversationScenes.Never;
                    StepUntil(h, () => g.GuardPose()?.phase == "walking to café" && g.Tick >= g.LureTick + 40, 300); phase = g.GuardPose()?.phase ?? "";
                    if (h.Views.Find("guard") is Transform gt && gt) h.LookAt(gt.position); yield return new WaitForSecondsRealtime(0.4f); yield return Frames(3);
                    yield return Shoot(h, dir, "r102-guard-leaves", log, "rest"); HandoverHost.PointerOverride = null;
                }
                Check("guardCall", turn.Length == 0 && offered && ring.Contains(callWord) && !ring.Contains("~" + callWord) && called && g.LureCalled == "guard" && phase == "walking to café",
                    $"{(turn.Length > 0 ? turn + "; " : "")}offered {offered}; ring '{ring}'; call.guard {called}; {scene}; then he is '{phase}' at {HandoverHost.MissionClock(g.Tick)}");
            }

            // 2 · a Call shut by violence (step 4 from his end): the Enforcer draws in front of the turned guard at his door; the Call stays, shut with its lock
            {
                yield return Fresh(h); var g = h.Game; string turn = TurnGuard(h), ring = "", pop = ""; bool saw = false, quiet = false; string lockShort = h.Text?.ShortLock("call-saw-violence") ?? "";
                if (turn.Length == 0 && g.GuardPose() is { } gp)
                {
                    long len = Math.Max(1L, (long)Math.Sqrt((double)gp.facing.X * gp.facing.X + (double)gp.facing.Y * gp.facing.Y));
                    var front = g.S.Grid.NearestFree(gp.pos + new Pt((int)(gp.facing.X * 300L / len), (int)(gp.facing.Y * 300L / len))) ?? gp.pos; // 3 m along his facing
                    h.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "move", Point = front }); StepUntil(h, () => g.Ops[(int)OpId.Enforcer].Walk == null, 3000);
                    h.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "draw" }); saw = StepUntil(h, () => g.GuardP.Has("saw-weapon-drawn"), 100);
                    yield return NoScene(h); yield return ClickOn(h, "guard"); ring = h.RingWords;
                    yield return Shoot(h, dir, "r102-call-saw-violence", log, "active");
                    int f0 = h.Feed.Count, n0 = g.Orders.Count, t0 = h.Talk.Count; h.RingTakeScripted("call", "guard"); yield return Frames(3);
                    pop = h.Pops.Count > 0 ? h.Pops[^1].Text : ""; quiet = g.Orders.Count == n0 && h.Feed.Count == f0 && h.Talk.Count == t0;
                    yield return Shoot(h, dir, "r102-call-saw-violence-pop", log, "active"); HandoverHost.PointerWorld = null;
                }
                Check("sawViolence", turn.Length == 0 && saw && lockShort.Length > 0 && ring.Contains($"~{callWord} ({lockShort})") && pop == lockShort && quiet,
                    $"{(turn.Length > 0 ? turn + "; " : "")}saw the draw {saw}; ring '{ring}'; the press's pop '{pop}' (the lock '{lockShort}'); nothing given, no card, no line {quiet}");
            }

            // 3 · the Already turned lock: route B's driver, turned by its Pay; the Fixer's pill on him, then his click's ring (one shut Turn wedge, the turned pictogram)
            {
                yield return Fresh(h); var g = h.Game; string pill = "", ring = "", icon = "", lockShort = h.Text?.ShortLock("already-turned") ?? "";
                var orders = Route("route-b");
                foreach (var o in orders) { if (o.Tick > 1100 || g.Over) break; if (o.Tick > g.Tick) h.RunTo(o.Tick); var c = new Order { Tick = o.Tick, Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How, Run = o.Run }; if (c.Do is "watch" or "listen" or "turn" or "call") h.GiveScripted(c); else h.Issue(c); }
                bool turned = StepUntil(h, () => g.Driver.Turned && FixerFree(h), 1500);
                if (turned)
                {
                    yield return NoScene(h); yield return ClickOn(h, "driver", click: false); pill = HudToolkit.PillWords;
                    yield return Shoot(h, dir, "r102-already-turned-pill", log, "active");
                    if (h.Views.Find("driver") is Transform dt && dt) { h.LeftClickScripted(Sp(h, dt.position + Vector3.up)); yield return Frames(4); }
                    ring = h.RingWords; icon = h.Ring != null && h.Ring.Segs.Count > 0 ? h.Ring.Segs[0].Icon : "";
                    yield return Shoot(h, dir, "r102-already-turned", log, "active"); h.CloseRing(); HandoverHost.PointerWorld = null;
                }
                int ways = ring.Split(" | ").Count(w => w.Length > 0 && w != "..."); // the ring's ways but the panel (⋯)
                Check("alreadyTurned", turned && lockShort.Length > 0 && ring.StartsWith($"~") && ring.Contains($"({lockShort})") && icon == "turned" && ways == 1 && !ring.Contains(callWord) && pill.Contains(lockShort),
                    $"driver turned {turned} at {HandoverHost.MissionClock(g.Tick)}; pill '{pill}'; ring '{ring}' ({ways} way, first icon '{icon}')");
            }

            // 4 · the verb row, the Action plate on: at rest, Fire hovered, Knock out armed
            {
                yield return Fresh(h); var keepPlate = h.controls.actionPlate; h.controls.actionPlate = true; string rest = "", hover = "", armed = "";
                h.Select(OpId.Enforcer, false); Btn(OffStreet); yield return Frames(3); yield return new WaitForSecondsRealtime(0.2f); rest = HudToolkit.VerbRowShown;
                yield return Shoot(h, dir, "r102-row-rest", log, "rest");
                var plate = h.PlateOf(OpId.Enforcer); int fire = Array.FindIndex(plate, x => x.Id == "fire"), td = Array.FindIndex(plate, x => x.Id == "takedown");
                if (HudToolkit.VerbTabScreen(fire) is Vector2 fp) { Btn(fp); yield return Frames(4); yield return new WaitForSecondsRealtime(0.2f); hover = HudToolkit.VerbRowWords; yield return Shoot(h, dir, "r102-row-hover", log, "rest"); Btn(OffStreet); yield return Frames(4); }
                if (td >= 0) { h.Pick(td); yield return Frames(4); yield return new WaitForSecondsRealtime(0.2f); armed = HudToolkit.VerbRowWords; yield return Shoot(h, dir, "r102-row-armed", log, "rest"); }
                h.controls.actionPlate = keepPlate;
                Check("verbRow", rest.Length > 0 && fire >= 0 && hover.StartsWith(plate[fire].Label) && hover.Contains("·") && armed.Length > 0 && !armed.Contains("·") && !hover.Contains("%") && !armed.Contains("%"), $"rest '{rest}'; {(fire >= 0 ? plate[fire].Label : "?")} hovered '{hover}'; the takedown armed '{armed}'");
            }

            h.controls.conversations = keepConvo; h.controls.planTogether = keepPlan;
            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
            yield return Fresh(h);
        }

        /// <summary>A route's orders by tick (contracts/missions/handover/routes), read as Proof101 reads them.</summary>
        static List<Order> Route(string name)
        {
            var path = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(FightLab.Dir(""))), "contracts", "missions", "handover", "routes", name + ".json");
            var sc = Suborned.Sim.Script.Parse(File.ReadAllText(path));
            return sc.Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).ToList();
        }
    }
}
