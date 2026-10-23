using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    /// <summary>Brief 043 (the done moment) and next-batch items 7–11, proved in the built player through the Input System's mouse, as the hand
    /// gives them: Listen on Inez given by a hold walks in to a serving Inez (the plate "→ Inez", the pill "to Inez · Listen"), lands, and its
    /// chip shows the facts (need, temperament, the lover) with Help in its unlock row; Help then leads the hold's offer with a lit pip; a hold
    /// that would drop the Scout's Watch shows "Cancels Watch · n s lost" before you let go, and the pop after; the Fixer's Call on Inez opens a
    /// window marker on the guard (its seconds) and "Guard back" pops when it closes; in third person a right hold on the calm patrol with the
    /// cursor off it fires (the pill and the hold agree, the fire log shows tp-fire). Each check one JSON object with "ok". Pictures go in the
    /// proof folder as done-* (GameBoot copies none; the batch's caller copies them to briefs/043/build).</summary>
    public static class ProofDone
    {
        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        static string B(bool b) => b ? "true" : "false";
        static string J(string x) => (x ?? "").Replace("\\", "/").Replace("\"", "'").Replace("\n", " ");
        static void Btn(Vector2 sp, bool left = false, bool right = false)
        {
            if (Mouse.current is not Mouse m) return;
            var st = new MouseState { position = sp }; if (left) st = st.WithButton(MouseButton.Left, true); if (right) st = st.WithButton(MouseButton.Right, true);
            InputSystem.QueueStateEvent(m, st);
        }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static Vector2 Sp(HandoverHost h, Vector3 w) { var s = h.View.WorldToScreenPoint(w); return new Vector2(s.x, s.y); }
        static Vector2 Aim(HandoverHost h, Vector3 w) { HandoverHost.PointerWorld = w; var sp = Sp(h, w); Btn(sp); return sp; }
        static IEnumerator Shoot(HandoverHost h, string dir, string name, List<string> log)
        {
            bool was = h.Paused; h.SetPaused(true);
            while (HudToolkit.ShotBusy) yield return null;
            log.Add($"{name}: " + HudToolkit.Shoot(Path.Combine(dir, name), "rest")); yield return null; while (HudToolkit.ShotBusy) yield return null;
            h.SetPaused(was);
        }
        static List<string> OrdersSince(HandoverHost h, int n0) { var l = new List<string>(); var g = h.Game; for (int i = n0; i < g.Orders.Count; i++) { var o = g.Orders[i].order; if (o.Do == "drive") continue; l.Add($"{o.Op}:{o.Do}{(o.Approach.Length > 0 ? ":" + o.Approach : "")}{(o.Target.Length > 0 ? ":" + o.Target : "")}{(g.Orders[i].ok ? "" : ":" + g.Orders[i].code)}"); } return l; }

        /// <summary>The clock run in steps of half a second until `until` holds (or `max` ticks pass); true when it held. A landing found this
        /// way is at most half a second old when the frame reads it (the chip is for landings in the last 2 s).</summary>
        static bool StepUntil(HandoverHost h, Func<bool> until, int max)
        {
            var g = h.Game; int end = g.Tick + max;
            while (!until() && g.Tick < end && !g.Over) h.RunTo(g.Tick + 5);
            return until();
        }
        static bool Landed(HandoverHost h, int from, Func<string, bool> code) { var g = h.Game; for (int i = from; i < g.Log.Count; i++) if (code(g.Log[i].Code ?? "")) return true; return false; }

        /// <summary>A left hold on a world point: pressed, held `hold` s (past the 0.4 s charge), released (commit on release).</summary>
        static IEnumerator Hold(HandoverHost h, Vector3 w, float hold)
        {
            var sp = Aim(h, w); yield return Frames(2); Btn(sp, left: true); yield return new WaitForSecondsRealtime(hold); Btn(Sp(h, w)); yield return Frames(3);
        }

        /// <summary>The window never changes size in this run (no `-frames-size`: a 2x run at `-capture-size 3840x2160`), so a picture never lets go
        /// of a held button: the extra frames for Glyph's 2x review are taken then (hold states, the amber window, the badges, Watched spot, the
        /// cabinet's ring). The batch's own proof (Air-sized frames, 1080p pictures) skips them.</summary>
        public static bool Still => CaptureSize.Picture == null && Screen.height >= 2000;

        /// <summary>"Watched spot" on the pill: the Scout learns the guard's cone, stands in it, and hovers the guard (her own spot is the one quoted).</summary>
        static IEnumerator WatchedSpot(HandoverHost h, string dir, List<string> log)
        {
            var g = h.Game; int l0 = g.Log.Count; h.GiveScripted(new Order { Op = "scout", Do = "watch", Target = "guard" });
            bool learned = StepUntil(h, () => Landed(h, l0, c => c == "scout.watch.watched") || g.Known("guard") is { Sight: true }, 1800);
            var fan = g.ConeFan("guard"); string seen = "no fan";
            if (fan != null && fan.Points.Length > 0)
            {
                long sx = 0, sy = 0; foreach (var p in fan.Points) { sx += p.X; sy += p.Y; } var c = new Pt((int)(sx / fan.Points.Length), (int)(sy / fan.Points.Length));
                var mid = new Pt((c.X + fan.At.X) / 2, (c.Y + fan.At.Y) / 2); var to = g.S.Grid.Free(mid) ? mid : g.S.Grid.NearestFree(mid);
                if (to.HasValue) { h.Issue(new Order { Op = "scout", Do = "move", Point = to.Value }); StepUntil(h, () => g.Ops[0].Walk == null, 600); }
                h.Select(OpId.Scout, false); if (h.Views.Find("guard") is Transform gd) { h.LookAt(gd.position); yield return Frames(3); for (int i = 0; i < 5; i++) { Aim(h, gd.position + Vector3.up * 1.0f); yield return null; } }
                seen = $"hoverSeen {h.HoverSeen}, pill '{HudToolkit.PillWords}'";
            }
            log.Add($"watched spot: learned {learned}; {seen}"); yield return Shoot(h, dir, "done-watched-spot", log);
            HandoverHost.PointerWorld = null;
        }

        /// <summary>The cabinet's ring for the Tech (Sky: three "Loop · 66 s" wedges, "looping what?"): each Loop names its camera, and the wedge
        /// under the pointer shows that camera in the world.</summary>
        static IEnumerator LoopRing(HandoverHost h, string dir, List<string> log)
        {
            var cab = h.Views.Find("marker/cabinet"); if (cab == null) { log.Add("loop ring: no cabinet"); yield break; }
            bool keepQ = h.controls.quickAction; h.controls.quickAction = false;
            h.Select(OpId.Tech, false); h.LookAt(cab.position); yield return Frames(3);
            var sp = Aim(h, cab.position); yield return Frames(3); HandoverHost.PointerWorld = null;
            Btn(sp, right: true); yield return null; Btn(sp); yield return Frames(3);
            var rs = h.Ring; string ways = rs == null ? "" : string.Join(" | ", rs.Segs.ConvertAll(x => x.More ? "⋯" : x.Word));
            int pick = rs == null ? -1 : rs.Segs.FindIndex(x => x.Word.Contains("pickup")); if (pick < 0 && rs != null) pick = rs.Segs.FindIndex(x => x.Named.Length > 0);
            if (rs != null && pick >= 0)
            {
                float k = Mathf.Max(1f, Mathf.Round(Screen.height / 1080f)), a = pick * 360f / rs.Segs.Count * Mathf.Deg2Rad, r = (HandoverHost.RingOuter + HandoverHost.RingInner) / 2f;
                var at = rs.Screen + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * r * k; Btn(at); yield return Frames(4);
            }
            log.Add($"loop ring: open {rs != null}, ways '{ways}', hover {(rs != null ? rs.Hover : -1)}, focus '{HudToolkit.RingFocusName}'");
            yield return Shoot(h, dir, "ring-loop-names", log);
            h.CloseRing(); h.controls.quickAction = keepQ; yield return Frames(2);
        }

        public static IEnumerator Handover(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var parts = new List<string>(); bool ok = true; var keepConvo = h.controls.conversations; h.controls.conversations = ConversationScenes.Never;
            h.Begin(); h.IntelOpen = false; h.StartClock(); yield return null; var g = h.Game;
            // ---- 1 · the Scout watches Inez (scripted), then moves 14 m off; Inez out serving a table
            int l0 = g.Log.Count; h.GiveScripted(new Order { Op = "scout", Do = "watch", Target = "waitress" });
            bool watched = StepUntil(h, () => Landed(h, l0, c => c == "scout.watch.watched"), 1500); yield return Frames(3);
            var watchChip = HudToolkit.ChipWords;
            var inezPos = g.PersonPos(g.Inez); var sc = g.Ops[0].Pos;
            if (inezPos.HasValue) { var dx = sc.X - inezPos.Value.X; var dy = sc.Y - inezPos.Value.Y; double d = Math.Max(1, Math.Sqrt((double)dx * dx + (double)dy * dy)); var want = new Pt(inezPos.Value.X + (int)(dx / d * 1400), inezPos.Value.Y + (int)(dy / d * 1400)); var to = g.S.Grid.Free(want) ? want : g.S.Grid.NearestFree(want); if (to.HasValue) { h.Issue(new Order { Op = "scout", Do = "move", Point = to.Value }); StepUntil(h, () => g.Ops[0].Walk == null, 600); } }
            bool serving = StepUntil(h, () => g.PersonPose(g.Inez) is { } pp && pp.phase == "serving", 1800); yield return Frames(2);
            // ---- 2 · Listen by a hold: taken at once (engine 0.23 walk-in), the plate and the pill say where the Scout is going
            h.Select(OpId.Scout, false); var iz = h.Views.Find("waitress"); h.LookAt(iz.position); yield return Frames(3);
            var izw = iz.position + Vector3.up * 1.0f; Aim(h, izw); yield return Frames(4); string offer = HudToolkit.PillWords;
            int n0 = g.Orders.Count; var hsp = Sp(h, izw); int wp0 = h.WorldPresses;
            string why = $"following {h.Following}, driving {h.Driving}, aiming {h.Aiming}, holdOn {h.controls.HoldActs}, overHud {HandoverHost.OverHud(hsp)} '{HudToolkit.PickedName(hsp)}', hover '{h.Hover?.Kind}/{h.Hover?.Key}', at ({(int)hsp.x},{(int)hsp.y}) of {Screen.width}x{Screen.height}";
            yield return Hold(h, izw, 0.6f); yield return Frames(2); why += $", presses {h.WorldPresses - wp0}";
            var given = OrdersSince(h, n0); bool walkingIn = g.Ops[0].Act != null && g.Ops[0].Act.Do == "listen"; string plate = h.PlateWords(0);
            yield return new WaitForSecondsRealtime(1.0f); for (int i = 0; i < 4; i++) { Aim(h, iz.position + Vector3.up * 1.0f); yield return null; } string walkPill = HudToolkit.PillWords; // past the pill's 0.8 s "ordered"
            yield return Shoot(h, dir, "done-walkin", log);
            bool walkOk = given.Contains("scout:listen:waitress") && walkingIn && plate.StartsWith("→") && walkPill.Contains("to Inez · Listen");
            parts.Add($"\"walkIn\": {{\"ok\": {B(walkOk)}, \"serving\": {B(serving)}, \"orders\": \"{string.Join(",", given)}\", \"walkingIn\": {B(walkingIn)}, \"plate\": \"{J(plate)}\", \"pill\": \"{J(walkPill)}\", \"offer\": \"{J(offer)}\"}}"); ok &= walkOk;
            log.Add($"done walk-in: {(walkOk ? "ok" : "FAILED")}: serving {serving}; [{string.Join(",", given)}] walking in {walkingIn}; plate '{plate}'; pill '{walkPill}'; {why}");
            // ---- 3 · the Listen lands: the chip (need, temperament, the lover) with Help in its unlock row, the plate's word, a row in the notes
            l0 = g.Log.Count; int rows0 = h.Feed.Count;
            bool heard = StepUntil(h, () => Landed(h, l0, c => c.StartsWith("scout.listen.")), 1500); HandoverHost.PointerWorld = null; Btn(new Vector2(Screen.width * 0.5f, Screen.height * 0.12f)); yield return Frames(4);
            var chip = h.LastChip; string chipWords = HudToolkit.ChipWords; int shown = HudToolkit.ChipsShown; string donePlate = h.PlateWords(0);
            if (Still) yield return new WaitForSecondsRealtime(0.5f); // Glyph 0.2.65: the pill's fade was caught behind the bubble; the chip lives 4.2 s
            yield return Shoot(h, dir, "done-listen-chip", log);
            bool chipOk = heard && chip != null && chip.Do == "listen" && chip.Lines.Count >= 2 && chip.Unlocks.Exists(u => u.Word == "Help") && shown >= 1 && h.Feed.Count > rows0;
            parts.Add($"\"listenChip\": {{\"ok\": {B(chipOk)}, \"watchChip\": \"{J(watchChip)}\", \"chip\": \"{J(chipWords)}\", \"lines\": \"{J(chip == null ? "" : string.Join(" · ", chip.Lines.Select(x => x.Word)))}\", \"unlocks\": \"{J(chip == null ? "" : string.Join(",", chip.Unlocks.Select(u => u.Word)))}\", \"shown\": {shown}, \"plate\": \"{J(donePlate)}\", \"rows\": {h.Feed.Count - rows0}}}"); ok &= chipOk;
            log.Add($"done listen chip: {(chipOk ? "ok" : "FAILED")}: '{chipWords}' ({shown} shown), plate '{donePlate}', watch chip '{watchChip}'");
            // ---- 4 · Help is the main action now for the Fixer, with the lit pip
            h.Select(OpId.Fixer, false); iz = h.Views.Find("waitress"); h.LookAt(iz.position); yield return Frames(2); for (int i = 0; i < 6; i++) { izw = iz.position + Vector3.up * 1.0f; Aim(h, izw); yield return null; }
            string helpPill = HudToolkit.PillWords; var hi = h.HoverIntent?.Order; bool pip = HudToolkit.PipShown;
            yield return Shoot(h, dir, "done-help-pip", log);
            bool helpOk = hi != null && hi.Do == "turn" && hi.Approach == "help" && pip;
            parts.Add($"\"helpMain\": {{\"ok\": {B(helpOk)}, \"pill\": \"{J(helpPill)}\", \"order\": \"{(hi == null ? "" : hi.Do + ":" + hi.Approach)}\", \"pip\": {B(pip)}}}"); ok &= helpOk;
            log.Add($"done help main: {(helpOk ? "ok" : "FAILED")}: '{helpPill}', pip {pip}");
            // ---- 5 · a cancelling order: the Scout watches the guard; a hold on the patrol says "Cancels Watch · n s lost" before the release, then pops
            h.Select(OpId.Scout, false); l0 = g.Log.Count; h.GiveScripted(new Order { Op = "scout", Do = "watch", Target = "guard" });
            StepUntil(h, () => g.Ops[0].Busy != null && g.Ops[0].Busy.Do == "watch", 900); h.RunTo(g.Tick + 40); yield return Frames(2);
            var pat = h.Views.Find("patrol"); string tagPill = "", tagReady = "", cancelPop = ""; int pops0 = h.DoneCancelPops; var cancelOrders = new List<string>(); string cancelPlate = ""; bool busyBefore = g.Ops[0].Busy != null;
            if (pat != null)
            {
                h.LookAt(pat.position); yield return Frames(3); var pw = pat.position + Vector3.up * 1.0f; Aim(h, pw); yield return Frames(5); tagPill = HudToolkit.CancelTagState;
                yield return Shoot(h, dir, "done-cancel-warn", log);
                n0 = g.Orders.Count; var sp = Aim(h, pw); yield return Frames(2); Btn(sp, left: true); yield return new WaitForSecondsRealtime(0.6f); tagReady = HudToolkit.CancelTagState; Btn(Sp(h, pw)); yield return Frames(4);
                cancelOrders = OrdersSince(h, n0); cancelPop = h.LastCancelPop; cancelPlate = h.PlateWords(0);
                if (h.Views.Find("op/scout") is Transform st) h.LookAt(st.position); HandoverHost.PointerWorld = null; Btn(new Vector2(Screen.width * 0.5f, Screen.height * 0.12f)); yield return Frames(2);
                yield return Shoot(h, dir, "done-cancel-pop", log);
            }
            bool cancelOk = busyBefore && (HudToolkit.CancelTagOn ? tagPill.StartsWith("pill:Cancels Watch") : tagPill.Length == 0 && tagReady.Length == 0) /* brief 046: the tag is off; the pop and plate stay */ && h.DoneCancelPops > pops0 && cancelPop.StartsWith("Watch cancelled") && cancelPlate == "cancelled" && cancelOrders.Exists(o => o.StartsWith("scout:watch:patrol"));
            parts.Add($"\"cancel\": {{\"ok\": {B(cancelOk)}, \"busyBefore\": {B(busyBefore)}, \"tag\": \"{J(tagPill)}\", \"tagReady\": \"{J(tagReady)}\", \"pop\": \"{J(cancelPop)}\", \"plate\": \"{J(cancelPlate)}\", \"orders\": \"{string.Join(",", cancelOrders)}\"}}"); ok &= cancelOk;
            log.Add($"done cancel: {(cancelOk ? "ok" : "FAILED")}: tag '{tagPill}' / '{tagReady}', pop '{cancelPop}', [{string.Join(",", cancelOrders)}]");
            // ---- 6 · the Fixer helps Inez until she is a contact, then calls her: the guard's window (its seconds), then "Guard back"
            string turnOut = ""; for (int tries = 0; tries < 3 && !g.Inez.Turned; tries++)
            {
                l0 = g.Log.Count; h.GiveScripted(new Order { Op = "fixer", Do = "turn", Target = "waitress", Approach = "help" });
                StepUntil(h, () => Landed(h, l0, c => c.StartsWith("turn.help.") || c == "order.dropped"), 2400); turnOut += (turnOut.Length > 0 ? "," : "") + (g.Log.FindLastIndex(x => (x.Code ?? "").StartsWith("turn.help.")) is int li && li >= 0 ? g.Log[li].Code : "none");
                if (Still && tries == 0 && h.Views.Find("waitress") is Transform tw) { h.LookAt(tw.position); HandoverHost.PointerWorld = null; Btn(new Vector2(Screen.width * 0.5f, Screen.height * 0.12f)); yield return Frames(4); yield return new WaitForSecondsRealtime(0.3f); /* past the chip's 0.12 s fade-in */ log.Add($"turn chip 1: '{HudToolkit.ChipWords}' tone {h.LastChip?.Tone}"); yield return Shoot(h, dir, "done-turn-chip", log); } // the badge (mark_half on "not yet")
            }
            yield return Frames(3); string turnChip = HudToolkit.ChipWords;
            bool canCall = StepUntil(h, () => g.CallQuery("waitress").ok, 3000);
            l0 = g.Log.Count; int cw0 = h.WindowClosePops; if (canCall) h.GiveScripted(new Order { Op = "fixer", Do = "call", Target = "waitress" });
            bool called = StepUntil(h, () => Landed(h, l0, c => c == "call.waitress") && h.Game.Windows().Exists(w => w.Kind == "call" && w.Thing == "guard"), 300);
            var gd = h.Views.Find("guard"); if (gd != null) h.LookAt(gd.position); HandoverHost.PointerWorld = null; Btn(new Vector2(Screen.width * 0.5f, Screen.height * 0.12f)); yield return Frames(5);
            string winWords = HudToolkit.WindowWords, callChip = HudToolkit.ChipWords; int winShown = HudToolkit.WindowMarksShown; var lastChip = h.LastChip;
            yield return Shoot(h, dir, "done-call-window", log);
            bool winOk = called && winShown >= 1 && winWords.Contains("call:guard:") && lastChip != null && lastChip.AnchorKey == "guard";
            parts.Add($"\"callWindow\": {{\"ok\": {B(winOk)}, \"turns\": \"{turnOut}\", \"turnChip\": \"{J(turnChip)}\", \"canCall\": {B(canCall)}, \"windows\": \"{J(winWords)}\", \"chip\": \"{J(callChip)}\"}}"); ok &= winOk;
            log.Add($"done call window: {(winOk ? "ok" : "FAILED")}: turns [{turnOut}], markers '{winWords}', chip '{callChip}'");
            if (Still) // the window's last 10 s: the underline amber, the plate blinking
            {
                bool soon = StepUntil(h, () => h.Game.Windows().Exists(w => w.Kind == "call" && w.Thing == "guard" && w.Until >= 0 && w.Until - h.Game.Tick <= 7 * Suborned.Sim.Game.Hz), 600); yield return Frames(4); // the engine's window (the HUD's marks refresh per frame, not per step)
                log.Add($"window soon: {soon} '{HudToolkit.WindowWords}'"); yield return Shoot(h, dir, "done-window-soon", log);
            }
            bool back = StepUntil(h, () => !h.Game.Windows().Exists(w => w.Kind == "call" && w.Thing == "guard"), 3000); yield return Frames(3);
            string closeWord = h.LastClosePop; bool backOk = back && h.WindowClosePops > cw0 && closeWord == "Guard back";
            yield return Shoot(h, dir, "done-guard-back", log);
            parts.Add($"\"guardBack\": {{\"ok\": {B(backOk)}, \"pop\": \"{J(closeWord)}\", \"closes\": {h.WindowClosePops - cw0}}}"); ok &= backOk;
            log.Add($"done guard back: {(backOk ? "ok" : "FAILED")}: '{closeWord}'");
            if (Still) { yield return WatchedSpot(h, dir, log); yield return LoopRing(h, dir, log); }
            // ---- 7 · third person (Keel's FL-KERB, Sky on 0.2.48): the Scout at the kerb before the Office, the calm patrol ahead, the cursor off it
            h.Begin(); h.IntelOpen = false; h.StartClock(); yield return null; g = h.Game;
            if (Still) // a refused turn's chip (mark_no): the Fixer asks Inez cold (headless: turn.ask.refused 25 s after the order)
            {
                int lr = g.Log.Count; h.GiveScripted(new Order { Op = "fixer", Do = "turn", Target = "waitress", Approach = "ask" });
                bool refused = StepUntil(h, () => Landed(h, lr, c => c.StartsWith("turn.ask.")), 900); if (h.Views.Find("waitress") is Transform tw2) h.LookAt(tw2.position);
                HandoverHost.PointerWorld = null; Btn(new Vector2(Screen.width * 0.5f, Screen.height * 0.12f)); yield return Frames(4); yield return new WaitForSecondsRealtime(0.3f);
                log.Add($"refused chip: {refused} '{HudToolkit.ChipWords}' tone {h.LastChip?.Tone}"); yield return Shoot(h, dir, "done-refused-chip", log);
            }
            var kerb = new Pt(6800, -5075); h.Issue(new Order { Op = "scout", Do = "move", Point = kerb, Run = true }); StepUntil(h, () => g.Ops[0].Walk == null && g.Tick >= 75 * Suborned.Sim.Game.Hz, 1500);
            var pp2 = g.PosOf("patrol"); string tpOffer = "", tpReady = "", focus = ""; var tpOrders = new List<string>(); bool tpLog = false;
            if (pp2.HasValue)
            {
                var a = g.Ops[0].Pos; double dd = Math.Sqrt(Suborned.Sim.G.D2(a, pp2.Value)); if (dd > 650) { var near = new Pt(pp2.Value.X + (int)((a.X - pp2.Value.X) * 600 / dd), pp2.Value.Y + (int)((a.Y - pp2.Value.Y) * 600 / dd)); var to = g.S.Grid.Free(near) ? near : g.S.Grid.NearestFree(near); if (to.HasValue) { h.Issue(new Order { Op = "scout", Do = "move", Point = to.Value }); StepUntil(h, () => g.Ops[0].Walk == null, 300); } }
                h.Select(OpId.Scout, false); h.controls.tpRightFires = true; h.DriveToggle(); yield return new WaitForSecondsRealtime(0.8f);
                var pw = h.frame.World(g.PosOf("patrol") ?? pp2.Value); h.FollowFace(pw); yield return new WaitForSecondsRealtime(0.4f);
                var me = h.frame.World(g.Ops[0].Pos); var fwd = (pw - me); fwd.y = 0f; var side = Vector3.Cross(Vector3.up, fwd.normalized);
                var off = me + fwd * 0.55f + side * 2.6f; off.y = 0.02f; Aim(h, off); yield return Frames(6);
                focus = h.DriveFocus == null ? "" : h.DriveFocus.Kind + ":" + h.DriveFocus.Key; tpOffer = HudToolkit.PillWords;
                int f0 = h.FireLog.Count; n0 = g.Orders.Count; var sp = Sp(h, off); Btn(sp, right: true); yield return new WaitForSecondsRealtime(0.6f); tpReady = HudToolkit.PillWords; Btn(Sp(h, off)); yield return new WaitForSecondsRealtime(0.8f);
                tpOrders = OrdersSince(h, n0); for (int i = f0; i < h.FireLog.Count; i++) if (h.FireLog[i].Contains("tp-fire")) tpLog = true;
                yield return Shoot(h, dir, "done-tp-fire", log);
                if (h.Driving) h.DriveLetGoToMap(); yield return null;
            }
            bool tpOk = focus == "patrol:patrol" && tpReady.StartsWith("Let go · Fire") && tpOrders.Contains("scout:fire:patrol") && tpLog;
            parts.Add($"\"tpFire\": {{\"ok\": {B(tpOk)}, \"focus\": \"{J(focus)}\", \"offer\": \"{J(tpOffer)}\", \"ready\": \"{J(tpReady)}\", \"orders\": \"{string.Join(",", tpOrders)}\", \"tpFireLog\": {B(tpLog)}}}"); ok &= tpOk;
            log.Add($"done tp fire: {(tpOk ? "ok" : "FAILED")}: focus '{focus}', offer '{tpOffer}', ready '{tpReady}', [{string.Join(",", tpOrders)}], log {tpLog}");
            h.controls.conversations = keepConvo; HandoverHost.PointerWorld = null; Btn(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)); h.Begin();
            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }
    }
}
