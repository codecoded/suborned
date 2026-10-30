using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Order = Suborned.Sim.Order;
using OpId = Suborned.Sim.OpId;

namespace Suborned.Play
{
    /// <summary>Brief 083, Sky's play notes on 0.2.159, through the Input System's own devices. One JSON object `r083`; pictures `r083-*` at play zoom.
    ///  · pan: a left drag pans (on the ground and from a person), its release is no click; a click still walks; a still press is still a hold.
    ///  · busy: on an act the first ground click asks and stops nothing, a double click is one slip, a second click stops and walks; a squad click leaves the busy agent at its act; the same act given again restarts nothing.
    ///  · third: T in and T out leave the act running with no order; the first WASD press asks, the second stops and takes the stick; the button in Squad's place goes back to the map.
    ///  · wasd: Sky's run of 22:18 to 22:21: a restart in third person, and T under the briefing, each come up with WASD live and a ground click giving no walk.</summary>
    public static class Proof083
    {
        static string B(bool b) => b ? "true" : "false";
        static string J(string x) => (x ?? "").Replace("\\", "/").Replace("\"", "'").Replace("\n", " ");
        static void Keys(params Key[] down) { if (Keyboard.current is Keyboard kb) InputSystem.QueueStateEvent(kb, new KeyboardState(down)); }
        static void Btn(Vector2 sp, bool left = false) { if (Mouse.current is Mouse m) { var st = new MouseState { position = sp }; if (left) st = st.WithButton(MouseButton.Left, true); InputSystem.QueueStateEvent(m, st); } }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator Wait(float s) { yield return new WaitForSecondsRealtime(s); }
        static IEnumerator Click(Vector2 sp) { Btn(sp); yield return Frames(2); Btn(sp, true); yield return Frames(2); Btn(sp); yield return Frames(3); }
        static IEnumerator Shoot(string dir, string name, List<string> log) { while (HudToolkit.ShotBusy) yield return null; log.Add($"{name}: " + HudToolkit.Shoot(Path.Combine(dir, name), "rest")); yield return null; while (HudToolkit.ShotBusy) yield return null; }
        static List<string> Since(HandoverHost h, int n0) { var l = new List<string>(); var g = h.Game; for (int i = n0; i < g.Orders.Count; i++) { var o = g.Orders[i].order; if (o.Do != "drive" || o.Release) l.Add(o.Op + ":" + o.Do + (o.Release ? ":release" : "")); } return l; }
        static int Drives(HandoverHost h, int n0) { int c = 0; var g = h.Game; for (int i = n0; i < g.Orders.Count; i++) if (g.Orders[i].order.Do == "drive" && !g.Orders[i].order.Release) c++; return c; }
        /// <summary>Open ground near the agent, off the HUD and off every thing.</summary>
        static Vector2? Ground(HandoverHost h, OpId who, float m = 4f)
        {
            var me = h.Views.Find("op/" + Suborned.Sim.Op.Name(who)); var cam = h.View; if (!me || !cam) return null;
            foreach (var d in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back, new Vector3(.7f, 0, .7f), new Vector3(-.7f, 0, -.7f), new Vector3(.7f, 0, -.7f), new Vector3(-.7f, 0, .7f) })
            {
                var sp = cam.WorldToScreenPoint(me.position + d * m); if (sp.z <= 0f) continue; var v = new Vector2(sp.x, sp.y);
                if (v.x > 200 && v.y > 150 && v.x < Screen.width - 200 && v.y < Screen.height - 150 && !HandoverHost.OverHud(v) && h.ProbeAt(v).StartsWith("left /, right /")) return v;
            }
            return null;
        }
        static IEnumerator Fresh(HandoverHost h)
        {
            HandoverHost.PointerWorld = null; HandoverHost.PointerOverride = null; Keys(); Btn(new Vector2(Screen.width * .5f, Screen.height * .5f));
            if (h.Driving) h.DriveLetGoToMap(); if (h.Following) h.FollowToggle();
            h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(false); yield return Frames(3);
            h.View.orthographicSize = 10f; h.Select(OpId.Scout, false); yield return Frames(2);
        }
        /// <summary>The Scout at a Watch on Inez (she stands in reach by engine order).</summary>
        static IEnumerator ScoutWatching(HandoverHost h)
        {
            yield return Fresh(h); var g = h.Game; h.GiveWatchScripted(OpId.Scout, "waitress");
            for (int i = 0; i < 90 && g.Ops[0].Busy == null; i++) { h.RunTo(g.Tick + 3); yield return null; }
            if (h.Views.Find("op/scout") is Transform st) h.LookAt(st.position); yield return Frames(3);
        }

        public static IEnumerator Run(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var parts = new List<string>(); bool ok = true;
            void Row(string name, bool good, string detail) { parts.Add($"\"{name}\": {{\"ok\": {B(good)}, \"detail\": \"{J(detail)}\"}}"); log.Add($"r083 {name}: {(good ? "ok" : "FAILED")}: {detail}"); ok &= good; }

            // ---- pan
            yield return Fresh(h); var g = h.Game; h.SetPaused(true); yield return Frames(2);
            if (Ground(h, OpId.Scout) is Vector2 gp)
            {
                var c0 = h.View.transform.position; int n0 = g.Orders.Count; int pd = h.PanDrags; int ring = 0; bool sel0 = h.Selected[0];
                Btn(gp); yield return Frames(2); Btn(gp, true); yield return Frames(2);
                for (int i = 1; i <= 12; i++) { Btn(gp + new Vector2(14f * i, 6f * i), true); yield return null; if (h.Ring != null) ring++; if (h.BoxScreen.HasValue) ring += 100; }
                yield return Frames(2); float moved = (h.View.transform.position - c0).magnitude; Btn(gp + new Vector2(168f, 72f)); yield return Frames(4);
                var after = Since(h, n0);
                Row("panGround", moved > 1f && after.Count == 0 && ring == 0 && h.PanDrags == pd + 1 && h.Selected[0] == sel0 && h.Ring == null, $"camera moved {moved:0.0} m, orders [{string.Join(",", after)}], rings or boxes {ring}");
                // a click still walks and moves no camera (the clock running: paused, "start together" keeps it as a ghost)
                h.SetPaused(false); yield return Frames(2);
                if (Ground(h, OpId.Scout) is Vector2 gc) { var c1 = h.View.transform.position; n0 = g.Orders.Count; yield return Click(gc); var cl = Since(h, n0); Row("clickWalks", cl.Contains("scout:move") && (h.View.transform.position - c1).magnitude < 0.01f, $"[{string.Join(",", cl)}]"); }
                else Row("clickWalks", false, "no ground");
                // a click with a shaky hand (under the threshold) is still a click
                if (Ground(h, OpId.Scout, 3f) is Vector2 gs) { n0 = g.Orders.Count; Btn(gs); yield return Frames(2); Btn(gs, true); yield return Frames(1); Btn(gs + new Vector2(4f, 3f) * Mathf.Max(1f, Mathf.Round(Screen.height / 1080f)), true); yield return Frames(1); Btn(gs + new Vector2(4f, 3f) * Mathf.Max(1f, Mathf.Round(Screen.height / 1080f))); yield return Frames(3); var sh = Since(h, n0); Row("shakyClick", sh.Contains("scout:move"), $"[{string.Join(",", sh)}]"); }
            }
            else Row("panGround", false, "no open ground");
            // a drag from a person pans; a still press on her is a hold
            if (h.Views.Find("waitress") is Transform iz)
            {
                h.LookAt(iz.position); yield return Frames(3); var ip = (Vector2)h.View.WorldToScreenPoint(iz.position + Vector3.up); var c0 = h.View.transform.position; int n0 = g.Orders.Count;
                Btn(ip); yield return Frames(4); Btn(ip, true); yield return Frames(2);
                for (int i = 1; i <= 8; i++) { Btn(ip + new Vector2(0f, 22f * i), true); yield return null; }
                bool noCharge = h.Charge == null; float moved = (h.View.transform.position - c0).magnitude; Btn(ip + new Vector2(0f, 176f)); yield return Frames(4); var o1 = Since(h, n0);
                Row("panFromPerson", moved > 1f && noCharge && o1.Count == 0 && h.Ring == null, $"moved {moved:0.0} m, charge gone {noCharge}, orders [{string.Join(",", o1)}]");
                h.LookAt(iz.position); yield return Frames(3); ip = h.View.WorldToScreenPoint(iz.position + Vector3.up); Btn(ip); yield return Frames(4); Btn(ip, true); yield return Wait(0.6f);
                bool ready = h.Charge is { Ready: true }; Btn(ip + new Vector2(0f, -400f)); yield return Frames(1); HandoverHost.PointerOverride = null; h.CancelHoldScripted(); Btn(ip); yield return Frames(3);
                Row("holdStillHolds", ready, $"ready {ready}");
            }

            // ---- busy: the click
            yield return ScoutWatching(h); g = h.Game; bool acting = g.Ops[0].Busy != null; string marks0 = HudToolkit.ActMarksShown; int carets = HudToolkit.SelectedCarets;
            Row("actMark", acting && marks0.Contains("scout:watch:") && carets == 1, $"marks '{marks0}', selected chevrons {carets}");
            h.SetPaused(true); yield return Frames(2); yield return Shoot(dir, "r083-acting-selected", log); h.Select(OpId.Fixer, false); yield return Frames(3); yield return Shoot(dir, "r083-acting-not-selected", log); h.Select(OpId.Scout, false); h.SetPaused(false); yield return Frames(2);
            if (Ground(h, OpId.Scout) is Vector2 bp)
            {
                int n0 = g.Orders.Count, asks = h.BusyAsks; yield return Click(bp); var first = Since(h, n0); string words = h.BusyWords; bool still = g.Ops[0].Busy != null;
                h.SetPaused(true); yield return Frames(2); yield return Shoot(dir, "r083-busy-prompt", log); h.SetPaused(false); yield return Frames(2);
                Row("firstClickAsks", first.Count == 0 && still && h.BusyAsks == asks + 1 && words == "Click again to stop" && HudToolkit.ActMarksShown.Contains("scout:watch:"), $"orders [{string.Join(",", first)}], '{words}'");
                yield return Wait(3.3f); // the window closed: a click asks again, and straight after it a double click's second is the same slip
                n0 = g.Orders.Count; asks = h.BusyAsks; Btn(bp); yield return Frames(1); Btn(bp, true); yield return null; Btn(bp); yield return null; Btn(bp, true); yield return null; Btn(bp); yield return Frames(3);
                var dbl = Since(h, n0); Row("doubleClickIsOneSlip", dbl.Count == 0 && g.Ops[0].Busy != null, $"orders [{string.Join(",", dbl)}]");
                yield return Wait(0.6f); n0 = g.Orders.Count; int stops = h.BusyStops; yield return Click(bp); var second = Since(h, n0);
                Row("secondClickStops", second.Contains("scout:cancel") && second.Contains("scout:move") && h.BusyStops > stops, $"[{string.Join(",", second)}], pop '{h.LastCancelPop}'");
            }
            else Row("firstClickAsks", false, "no ground");
            // squad: the free go, the busy stay
            yield return ScoutWatching(h); g = h.Game; h.SelectAll(); yield return Frames(2);
            if (Ground(h, OpId.Scout) is Vector2 sq) { int n0 = g.Orders.Count, sk = h.BusySkips; yield return Click(sq); var so = Since(h, n0); Row("squadSkipsBusy", g.Ops[0].Busy != null && !so.Contains("scout:cancel") && !so.Contains("scout:move") && so.Contains("fixer:move") && h.BusySkips > sk, $"[{string.Join(",", so)}]"); }
            else Row("squadSkipsBusy", false, "no ground");
            // the same act given again restarts nothing (Sky's tick 172: "Watch cancelled · 3 s lost")
            yield return ScoutWatching(h); g = h.Game;
            { int end = g.Ops[0].BusyEnd, n0 = g.Orders.Count, kept = h.SameKept; h.GiveWatchRepeatScripted(OpId.Scout, "waitress"); yield return Frames(3); var ag = Since(h, n0); Row("sameActKept", g.Ops[0].Busy != null && g.Ops[0].BusyEnd == end && ag.Count == 0 && h.SameKept > kept, $"[{string.Join(",", ag)}], kept {h.SameKept - kept}"); }

            // ---- third person
            yield return ScoutWatching(h); g = h.Game;
            {
                int n0 = g.Orders.Count, end = g.Ops[0].BusyEnd; h.DriveEnter083(); yield return Wait(0.8f); bool inTp = h.Driving && h.DriveTrailing; string tpMarks = HudToolkit.ActMarksShown; var e1 = Since(h, n0); int d1 = Drives(h, n0);
                h.SetPaused(true); yield return Frames(2); yield return Shoot(dir, "r083-third-person-acting", log); h.SetPaused(false); yield return Frames(2);
                Row("thirdEnterKeeps", inTp && g.Ops[0].Busy != null && g.Ops[0].BusyEnd == end && e1.Count == 0 && d1 == 0 && tpMarks.Contains("scout:watch:") && HudToolkit.ActMarkDiscsSeen == 1 && HudToolkit.SelectedCarets == 1, $"orders [{string.Join(",", e1)}], drive samples {d1}, marks '{tpMarks}', discs seen {HudToolkit.ActMarkDiscsSeen}");
                // first press asks, nothing moves or cancels
                int asks = h.BusyAsks; n0 = g.Orders.Count; Keys(Key.W); yield return Wait(0.5f); Keys(); yield return Wait(0.2f);
                bool asked = h.BusyAsks == asks + 1 && g.Ops[0].Busy != null && g.Ops[0].BusyEnd == end && Drives(h, n0) == 0 && !Since(h, n0).Contains("scout:cancel");
                h.SetPaused(true); yield return Frames(2); yield return Shoot(dir, "r083-third-person-busy-prompt", log); h.SetPaused(false); yield return Frames(2);
                Row("wasdFirstPressAsks", asked, $"'{h.BusyWords}', samples {Drives(h, n0)}");
                // T out: nothing changes
                yield return Wait(3.1f); n0 = g.Orders.Count; h.DriveLetGoToMap(true); yield return Wait(0.9f); var e2 = Since(h, n0);
                Row("thirdLeaveKeeps", !h.Following && g.Ops[0].Busy != null && g.Ops[0].BusyEnd == end && !e2.Contains("scout:cancel") && !e2.Contains("scout:move"), $"orders [{string.Join(",", e2)}]");
                // the way back is a real button: in Squad's place, in third person, with the agent at her act
                h.DriveEnter083(); yield return Wait(0.8f); n0 = g.Orders.Count;
                if (HudToolkit.SquadBtnScreen is Vector2 sb) { Btn(sb); yield return Frames(3); bool over = HandoverHost.OverHud(sb) && !HudToolkit.FollowChipShown; Btn(sb, true); yield return Frames(2); Btn(sb); yield return Wait(0.8f); var e3 = Since(h, n0); Row("backToMapButton", over && !h.Following && !h.Driving && g.Ops[0].Busy != null && g.Ops[0].BusyEnd == end && !e3.Contains("scout:cancel"), $"over HUD {over}, word '{h.BackToMapWord}', following {h.Following}, orders [{string.Join(",", e3)}]"); }
                else Row("backToMapButton", false, "no button");
                // press, press: the act stops and the stick is hers
                h.DriveEnter083(); yield return Wait(0.8f); n0 = g.Orders.Count; var p0 = g.Ops[0].Pos; Keys(Key.W); yield return Wait(0.2f); Keys(); yield return Wait(0.3f); Keys(Key.W); yield return Wait(1f); Keys(); yield return Wait(0.3f); var e4 = Since(h, n0);
                float walked = Mathf.Sqrt((float)Suborned.Sim.G.D2(p0, g.Ops[0].Pos)) / 100f;
                Row("wasdSecondPressStops", e4.Contains("scout:cancel") && g.Ops[0].Busy == null && walked > 0.5f, $"[{string.Join(",", e4)}], walked {walked:0.0} m");
                // a press carried across the switch is no click on the map
                yield return ScoutWatching(h); g = h.Game; h.DriveEnter083(); yield return Wait(0.7f); var mp = new Vector2(Screen.width * .55f, Screen.height * .45f); Btn(mp); yield return Frames(2); Btn(mp, true); yield return Frames(2); n0 = g.Orders.Count; h.DriveLetGoToMap(); yield return Frames(2); Btn(mp); yield return Frames(4); var e5 = Since(h, n0);
                Row("pressAcrossSwitch", !e5.Contains("scout:cancel") && !e5.Contains("scout:move") && g.Ops[0].Busy != null, $"[{string.Join(",", e5)}]");
            }

            // ---- WASD dead: the two ways in, each reproduced
            yield return Fresh(h); g = h.Game; h.DriveEnter083(); yield return Wait(0.6f);
            h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(false); yield return Wait(0.8f); yield return Dead(h, "restartInThird", Row);
            yield return Fresh(h); g = h.Game; h.ShowBriefing = true; yield return Frames(2); Keys(Key.T); yield return Frames(2); Keys(); yield return Frames(2); bool under = h.Following && !h.Driving; h.ShowBriefing = false; yield return Wait(0.8f);
            log.Add($"r083 T under the briefing: follow view with no agent while modal {under}"); yield return Dead(h, "tUnderBriefing", Row);
            // the lab's own follow view is still asked for and left alone
            yield return Fresh(h); h.FollowToggle(); yield return Wait(0.8f); Row("labFollowOnly", h.Following && !h.Driving, $"following {h.Following}, driving {h.Driving}"); h.FollowToggle(); yield return Frames(2);

            if (h.Driving) h.DriveLetGoToMap(); if (h.Following) h.FollowToggle(); HandoverHost.PointerOverride = null; Keys(); Btn(new Vector2(Screen.width * .5f, Screen.height * .5f)); yield return Frames(2);
            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }

        /// <summary>In third person the agent is in hand, W walks her and a ground click gives no walk.</summary>
        static IEnumerator Dead(HandoverHost h, string name, System.Action<string, bool, string> row)
        {
            var g = h.Game; bool driving = h.Following && h.Driving; int who = h.DriveAgent >= 0 ? h.DriveAgent : 0; var p0 = g.Ops[who].Pos;
            Keys(Key.W); yield return Wait(1f); Keys(); yield return Wait(0.3f); float w = Mathf.Sqrt((float)Suborned.Sim.G.D2(p0, g.Ops[who].Pos)) / 100f;
            var cam = h.View; int n0 = g.Orders.Count; var me = h.Views.Find("op/" + Suborned.Sim.Op.Name((OpId)who)); var sp = cam.WorldToScreenPoint(me.position + cam.transform.forward * 4f + cam.transform.right); yield return Click(new Vector2(sp.x, sp.y)); var o = Since(h, n0);
            row(name, driving && w > 0.5f && !o.Exists(x => x.EndsWith(":move")), $"driving {driving}, W walked {w:0.0} m, click orders [{string.Join(",", o)}]");
        }
    }
}
