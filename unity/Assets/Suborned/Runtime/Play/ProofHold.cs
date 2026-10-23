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
using Math = System.Math;
using G = Suborned.Sim.G;

namespace Suborned.Play
{
    /// <summary>Brief 041's proofs for the hold preset ("Click walks, hold acts"), through the Input System's own mouse and keyboard so the host
    /// reads them as it reads the hand; the pointer is held on a world point (HandoverHost.PointerWorld) so a picture's resize never moves it.
    /// Each check writes one JSON object with "ok" (game:check fails on false). The Handover's half: a click walks and gives no act, a hold acts,
    /// an early release cancels, a double click on the ground runs; paused orders start together on one tick; Tab steps an overlap. The range's
    /// half: a right hold fills and, let go when full, gives a lasting fire order with the draw in it; a bystander takes the same hold; a squad's
    /// right hold fires with every selected drawn agent; once a fight is on a right click focuses; the under-fire cue; G toggles weapons free and X
    /// stands down; a portrait dropped on an officer locks it at ¼ speed; third person's right hold commits on release. Brief 041 0.2.44 adds to
    /// the Handover half: commit on release, the latch on a walking Inez and the 80 px cancel, squad-leave and the own-agent click.
    /// Pictures (1920 x 1080, HUD shots) go in the proof folder as hold-*.</summary>
    public static class ProofHold
    {
        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        static string B(bool b) => b ? "true" : "false";
        static string J(string x) => (x ?? "").Replace("\\", "/").Replace("\"", "'").Replace("\n", " ");
        static void Keys(params Key[] down) { if (Keyboard.current is Keyboard kb) InputSystem.QueueStateEvent(kb, new KeyboardState(down)); }
        static void Btn(Vector2 sp, bool left = false, bool right = false)
        {
            if (Mouse.current is not Mouse m) return;
            var st = new MouseState { position = sp }; if (left) st = st.WithButton(MouseButton.Left, true); if (right) st = st.WithButton(MouseButton.Right, true);
            InputSystem.QueueStateEvent(m, st);
        }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static Vector2 Sp(HandoverHost h, Vector3 w) { var s = h.View.WorldToScreenPoint(w); return new Vector2(s.x, s.y); }
        /// <summary>Hold the pointer on a world point; the mouse is put there too (the HUD's hit test reads it).</summary>
        static Vector2 Aim(HandoverHost h, Vector3 w) { HandoverHost.PointerWorld = w; var sp = Sp(h, w); Btn(sp); return sp; }

        static IEnumerator Shoot(string dir, string name, List<string> log)
        {
            while (HudToolkit.ShotBusy) yield return null;
            log.Add($"{name}: " + HudToolkit.Shoot(Path.Combine(dir, name), "rest")); yield return null; while (HudToolkit.ShotBusy) yield return null;
        }

        static List<string> OrdersSince(HandoverHost h, int n0) { var l = new List<string>(); var g = h.Game; for (int i = n0; i < g.Orders.Count; i++) { var o = g.Orders[i].order; if (o.Do == "drive") continue; l.Add($"{o.Op}:{o.Do}{(o.Run ? ":run" : "")}{(o.Target.Length > 0 ? ":" + o.Target : "")}{(g.Orders[i].ok ? "" : ":refused")}"); } return l; }

        /// <summary>A left press held `hold` seconds at the pointer, then released.</summary>
        static IEnumerator Left(HandoverHost h, Vector3 w, float hold)
        {
            var sp = Aim(h, w); yield return Frames(2); Btn(sp, left: true); yield return null;
            if (hold > 0f) yield return new WaitForSecondsRealtime(hold); else yield return null;
            Btn(Sp(h, w)); yield return Frames(3);
        }

        // ---------------------------------------------------------------- the Handover: click, hold, cancel, run; the plan; Tab

        public static IEnumerator Handover(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var parts = new List<string>(); bool ok = true; var keepPlan = h.controls.planTogether;
            // item 13 (briefs/041/next-batch.md): the Scout watching Inez is busy: asked for a squad fire order on the guard, the engine refuses her
            // (agent-busy) and her plate says "busy"; the armed rest are taken. Asked through FireQuery only: no order is given (no going-loud card)
            {
                h.Begin(); h.IntelOpen = false; h.StartClock(); yield return null; var g0 = h.Game;
                h.GiveScripted(new Order { Op = "scout", Do = "watch", Target = "waitress" }); int lim = g0.Tick + 900; while (g0.Ops[0].Busy == null && g0.Tick < lim) h.RunTo(g0.Tick + 2);
                h.SelectAll(); var who = h.ShootersFor(new HandoverHost.Target { Kind = "person", Key = "guard" }, true); yield return null;
                var armed = new List<int>(); for (int k = 1; k < 4; k++) if (g0.Ops[k].Gun.W != null) armed.Add(k);
                string plate = h.PlateWords(0); var (code, _) = h.FireRefusalOf(0);
                bool busyOk = g0.Ops[0].Busy != null && code == "agent-busy" && plate == "busy" && !who.Contains(0) && armed.TrueForAll(who.Contains);
                parts.Add($"\"squadBusy\": {{\"ok\": {B(busyOk)}, \"code\": \"{code}\", \"plate\": \"{J(plate)}\", \"taken\": \"{string.Join(",", who)}\", \"armed\": \"{string.Join(",", armed)}\"}}"); ok &= busyOk;
                log.Add($"holdActs squad busy: {(busyOk ? "ok" : "FAILED")}: Scout {code} plate '{plate}', taken [{string.Join(",", who)}], armed others [{string.Join(",", armed)}]");
            }
            // 1 · the Fixer and Inez (his main calm act: Watch, from 12 m): a click walks there and gives no act; the hold acts; an early release cancels
            h.Begin(); h.IntelOpen = false; h.StartClock(); yield return null; var g = h.Game;
            h.Select(OpId.Fixer, false); var iz = h.Views.Find("waitress"); h.LookAt(iz.position); yield return Frames(3);
            var izw = iz.position + Vector3.up * 1.0f; Aim(h, izw); yield return Frames(4);
            string probe = h.ProbeAt(Sp(h, izw)); string offer = HudToolkit.PillWords;
            yield return Shoot(dir, "hold-act-offer", log);
            int n0 = g.Orders.Count; yield return Left(h, izw, 0f); yield return new WaitForSecondsRealtime(0.5f);
            var click = OrdersSince(h, n0); string pendClick = h.PendingOf(1).verb;
            bool clickOk = click.Count > 0 && click.TrueForAll(o => o.StartsWith("fixer:move")) && pendClick.Length == 0 && h.ThingWalks > 0;
            parts.Add($"\"click\": {{\"ok\": {B(clickOk)}, \"orders\": \"{string.Join(",", click)}\", \"pending\": \"{pendClick}\", \"probe\": \"{J(probe)}\", \"offer\": \"{J(offer)}\"}}"); ok &= clickOk;
            log.Add($"holdActs click: {(clickOk ? "ok" : "FAILED")}: [{string.Join(",", click)}] pending '{pendClick}'; offer '{offer}'; probe {probe}");
            // the early release (0.28 s: the charge shows, then the button comes up)
            int c0 = h.HoldCancels; n0 = g.Orders.Count; yield return Left(h, izw, 0.28f); yield return Frames(3);
            var early = OrdersSince(h, n0); bool earlyOk = h.HoldCancels == c0 + 1 && early.Count == 0;
            parts.Add($"\"earlyRelease\": {{\"ok\": {B(earlyOk)}, \"orders\": \"{string.Join(",", early)}\", \"cancels\": {h.HoldCancels - c0}}}"); ok &= earlyOk;
            // the hold, commit on release (Sky 2026-10-22): full at 0.4 s, the pill says "Let go · …" and nothing is given until the button comes up
            n0 = g.Orders.Count; int a0 = h.HoldActsDone;
            { var sp = Aim(h, izw); yield return Frames(2); Btn(sp, left: true); yield return new WaitForSecondsRealtime(0.25f); }
            string charging = HudToolkit.PillWords; string chState = HudToolkit.PillState;
            yield return new WaitForSecondsRealtime(0.4f); string ready = HudToolkit.PillWords; string readyState = HudToolkit.PillState; var beforeRelease = OrdersSince(h, n0);
            Btn(Sp(h, izw)); yield return new WaitForSecondsRealtime(0.4f);
            var held = OrdersSince(h, n0); string pend = h.PendingOf(1).verb; string walking = HudToolkit.PillWords;
            h.SetPaused(true); yield return Shoot(dir, "hold-act-walking", log); h.SetPaused(false);
            // the pictures, apart from the check (a picture's resize lets go of a held button): a half-filled charge (slowed), then a full one ("Let go")
            { HandoverHost.ChargeScale = 6f; var sp2 = Aim(h, izw); yield return Frames(2); Btn(sp2, left: true); yield return new WaitForSecondsRealtime(1.2f); log.Add($"act charge picture: {HudToolkit.PillState} '{HudToolkit.PillWords}' sweep {HudToolkit.SweepShownNow:0.##}"); h.SetPaused(true); yield return Shoot(dir, "hold-act-charge", log); h.SetPaused(false); Btn(Sp(h, izw)); // 0.4 s × 6: about half at 1.2 s
                // the token's "drain" on an early let-go: the sweep runs back to empty (sampled per frame, not a picture: it lasts about 0.15 s)
                var drain = new List<string>(); for (int f = 0; f < 14; f++) { yield return null; drain.Add($"{HudToolkit.SweepShownNow:0.##}"); }
                log.Add($"act drain after let-go: [{string.Join(" ", drain)}]"); HandoverHost.ChargeScale = 1f; }
            if (ProofDone.Still) // the full hold: "Let go", the ring and glow (2x run only: a held button survives the picture), then pulled away to cancel
            {
                var sp3 = Aim(h, izw); yield return Frames(2); Btn(sp3, left: true); yield return new WaitForSecondsRealtime(0.6f);
                log.Add($"act ready picture: {HudToolkit.PillState} '{HudToolkit.PillWords}'"); h.SetPaused(true); yield return Shoot(dir, "hold-act-ready", log); h.SetPaused(false);
                HandoverHost.PointerWorld = null; var away = sp3 + new Vector2(0f, -260f * Mathf.Max(1f, Mathf.Round(Screen.height / 1080f))); Btn(away, left: true); yield return Frames(3); Btn(away); yield return Frames(3);
            }
            bool holdOk = h.HoldActsDone > a0 && (pend == "watch" || held.Exists(o => o.StartsWith("fixer:watch"))) && chState == "charge" && charging.StartsWith("Hold ·") && readyState == "ready" && ready.StartsWith("Let go ·") && !beforeRelease.Exists(o => o.StartsWith("fixer:watch") || o.StartsWith("fixer:move"));
            parts.Add($"\"hold\": {{\"ok\": {B(holdOk)}, \"orders\": \"{string.Join(",", held)}\", \"beforeRelease\": \"{string.Join(",", beforeRelease)}\", \"pending\": \"{pend}\", \"charging\": \"{J(charging)}\", \"ready\": \"{J(ready)}\", \"walking\": \"{J(walking)}\"}}"); ok &= holdOk;
            log.Add($"holdActs hold (commit on release): {(holdOk ? "ok" : "FAILED")}: before release [{string.Join(",", beforeRelease)}], after [{string.Join(",", held)}] pending '{pend}'; charge '{charging}'; ready '{ready}' ({readyState}); walking '{walking}'");
            // the latch: a hold on Inez while she walks her round, the pointer left where it was pressed: the pill follows her and the release commits on her;
            // then a hold dragged 120 px away cancels (no order)
            h.Begin(); h.IntelOpen = false; h.StartClock(); yield return null; g = h.Game; h.Select(OpId.Fixer, false); iz = h.Views.Find("waitress");
            float moved = 0f; for (int i = 0; i < 1200 && moved < 0.05f; i++) { var p0 = iz.position; h.RunTo(g.Tick + 1); yield return null; moved = (iz.position - p0).magnitude; }
            h.LookAt(iz.position); yield return Frames(2); var pressW = iz.position + Vector3.up * 1.0f; var startPos = iz.position;
            n0 = g.Orders.Count; { var sp = Aim(h, pressW); yield return Frames(2); Btn(sp, left: true); yield return new WaitForSecondsRealtime(0.65f); }
            string latchWords = HudToolkit.PillWords, latchState = HudToolkit.PillState; float walked = (iz.position - startPos).magnitude; var under = h.ProbeAt(Sp(h, pressW));
            Btn(Sp(h, pressW)); yield return new WaitForSecondsRealtime(0.4f); var latch = OrdersSince(h, n0); string latchPend = h.PendingOf(1).verb;
            bool latchOk = latchState == "ready" && latchWords.StartsWith("Let go ·") && (latchPend.Length > 0 || latch.Exists(o => o.StartsWith("fixer:") && !o.StartsWith("fixer:cancel"))) && (h.PendingTarget(1) == "waitress" || latch.Exists(o => o.StartsWith("fixer:") && o.Contains("waitress")));
            float ks = Mathf.Max(1f, Mathf.Round(Screen.height / 1080f)); int tc0 = h.HoldTravelCancels; n0 = g.Orders.Count; // 120 kit px (the cancel is at 80) h.Select(OpId.Scout, false); yield return null;
            { var sp = Aim(h, pressW = iz.position + Vector3.up); yield return Frames(2); Btn(sp, left: true); yield return new WaitForSecondsRealtime(0.15f); HandoverHost.PointerWorld = null; for (int s2 = 1; s2 <= 8; s2++) { Btn(sp + new Vector2(15f * ks * s2, 0f), left: true); yield return null; } yield return new WaitForSecondsRealtime(0.4f); HandoverHost.PointerWorld = null; Btn(sp + new Vector2(120f * ks, 0f)); yield return Frames(4); }
            var dragged = OrdersSince(h, n0); bool travelOk = h.HoldTravelCancels == tc0 + 1 && !dragged.Exists(o => o.StartsWith("scout:watch") || o.StartsWith("scout:move"));
            parts.Add($"\"holdLatch\": {{\"ok\": {B(latchOk && travelOk)}, \"walkedM\": {F(walked)}, \"ready\": \"{J(latchWords)}\", \"orders\": \"{string.Join(",", latch)}\", \"pending\": \"{latchPend}\", \"underPointerAtRelease\": \"{J(under)}\", \"travelCancel\": {B(travelOk)}, \"travelOrders\": \"{string.Join(",", dragged)}\"}}"); ok &= latchOk && travelOk;
            log.Add($"holdActs latch: {(latchOk && travelOk ? "ok" : "FAILED")}: Inez walked {F(walked)} m during the hold; '{latchWords}'; [{string.Join(",", latch)}] pending '{latchPend}'; 120 px drag cancels {travelOk} [{string.Join(",", dragged)}]");
            // squad-leave and the own-agent click: all four selected, a hold on Inez gives it to the best placed, who leaves the squad; a click on that agent
            // (busy, out of the squad) selects it alone
            h.Begin(); h.IntelOpen = false; h.StartClock(); yield return null; g = h.Game; h.SelectAll(); yield return null; iz = h.Views.Find("waitress"); h.LookAt(iz.position); yield return Frames(2);
            izw = iz.position + Vector3.up; n0 = g.Orders.Count; int sl0 = h.SquadLeaves;
            { var sp = Aim(h, izw); yield return Frames(2); Btn(sp, left: true); yield return new WaitForSecondsRealtime(0.6f); Btn(Sp(h, izw)); yield return new WaitForSecondsRealtime(0.3f); }
            var squadGive = OrdersSince(h, n0); int doer = -1; for (int k = 0; k < 4; k++) if (h.PendingOf(k).verb.Length > 0 || g.Ops[k].Busy != null) { doer = k; break; }
            int selN = h.Selected.Count(x => x); bool leftOk = doer >= 0 && !h.Selected[doer] && selN == 3 && h.SquadLeaves == sl0 + 1;
            h.SetPaused(true); HandoverHost.PointerWorld = null; yield return Shoot(dir, "hold-squad-leave", log); h.SetPaused(false);
            bool ownOk = false; string ownWhy = "";
            if (doer >= 0 && h.Views.Find("op/" + Suborned.Sim.Op.Name((OpId)doer)) is Transform dt)
            {
                n0 = g.Orders.Count; var aw = dt.position + Vector3.up * 1.0f; yield return Left(h, aw, 0f); yield return Frames(2);
                var ownAfter = OrdersSince(h, n0); ownOk = h.Selected[doer] && h.Selected.Count(x => x) == 1 && !ownAfter.Exists(o => o.Contains(":move")); ownWhy = $"selected [{string.Join(",", Enumerable.Range(0, 4).Where(k => h.Selected[k]))}], orders [{string.Join(",", ownAfter)}]";
            }
            parts.Add($"\"squadLeave\": {{\"ok\": {B(leftOk)}, \"doer\": {doer}, \"selectedAfter\": {selN}, \"orders\": \"{string.Join(",", squadGive)}\"}}, \"ownAgentClick\": {{\"ok\": {B(ownOk)}, \"why\": \"{J(ownWhy)}\"}}"); ok &= leftOk && ownOk;
            log.Add($"holdActs squad-leave: {(leftOk ? "ok" : "FAILED")}: doer {doer}, {selN} left selected; own-agent click {(ownOk ? "ok" : "FAILED")}: {ownWhy}");
            // a double click on the ground runs (the whole cell, in formation: their destination rings)
            h.SelectAll(); yield return null; var me = h.Views.Find("op/scout").position; Vector3 gw = me + new Vector3(4f, 0f, 3f); bool found = false;
            foreach (var off in new[] { new Vector3(4f, 0, 3f), new Vector3(-4f, 0, 3f), new Vector3(4f, 0, -3f), new Vector3(-4f, 0, -3f), new Vector3(6f, 0, 0) }) { var q = h.frame.Engine(me + off); if (g.S.Grid.Free(q)) { gw = me + off; found = true; break; } }
            n0 = g.Orders.Count;
            { var sp = Aim(h, gw); yield return Frames(2); Btn(sp, left: true); yield return null; Btn(sp); yield return Frames(2); Btn(sp, left: true); yield return null; Btn(sp); yield return Frames(3); }
            var dbl = OrdersSince(h, n0); bool runs = dbl.Exists(o => o.Contains(":move:run")); int movers = dbl.Select(o => o.Split(':')[0]).Distinct().Count();
            h.LookAt(gw); yield return new WaitForSecondsRealtime(0.3f); h.SetPaused(true); HandoverHost.PointerWorld = null; Btn(new Vector2(Screen.width * 0.5f, Screen.height * 0.08f)); yield return Frames(3);
            yield return Shoot(dir, "hold-squad-formation", log); h.SetPaused(false);
            bool dblOk = found && runs && movers >= 3;
            parts.Add($"\"doubleRuns\": {{\"ok\": {B(dblOk)}, \"orders\": \"{string.Join(",", dbl)}\", \"agents\": {movers}}}"); ok &= dblOk;
            log.Add($"holdActs double click ground: {(dblOk ? "ok" : "FAILED")}: [{string.Join(",", dbl)}]");
            // 2 · pause and plan: paused, the Scout walks and the Tech holds on the cabinet: nothing is given; Space gives both on one tick
            h.Begin(); h.IntelOpen = false; h.StartClock(); yield return null; g = h.Game; h.controls.planTogether = true; h.SetPaused(true); yield return null;
            h.Select(OpId.Scout, false); var sc = h.Views.Find("op/scout").position; Vector3 sw = sc + new Vector3(5f, 0, 0);
            foreach (var off in new[] { new Vector3(5f, 0, 0), new Vector3(-5f, 0, 0), new Vector3(0, 0, 5f), new Vector3(0, 0, -5f) }) if (g.S.Grid.Free(h.frame.Engine(sc + off))) { sw = sc + off; break; }
            h.LookAt(sc); yield return Frames(2); n0 = g.Orders.Count; yield return Left(h, sw, 0f);
            h.Select(OpId.Tech, false); var cab = h.Views.Find("marker/cabinet"); int queued = 0;
            if (cab) { h.LookAt((cab.position + sc) * 0.5f); yield return Frames(2); var cw = cab.position + Vector3.up * 0.3f; var sp = Aim(h, cw); yield return Frames(3); Btn(sp, left: true); yield return new WaitForSecondsRealtime(0.6f); Btn(Sp(h, cw)); yield return Frames(3); }
            queued = h.Plan.Count; int whilePaused = OrdersSince(h, n0).Count;
            HandoverHost.PointerWorld = null; Btn(new Vector2(Screen.width * 0.5f, Screen.height * 0.08f)); yield return Frames(3);
            yield return Shoot(dir, "hold-plan-paused", log);
            n0 = g.Orders.Count; Keys(Key.Space); yield return null; Keys(); yield return Frames(4);
            var go = new List<(string, int)>(); for (int i = n0; i < g.Orders.Count; i++) if (g.Orders[i].order.Do != "drive") go.Add((g.Orders[i].order.Op + ":" + g.Orders[i].order.Do, g.Orders[i].order.Tick));
            bool together = go.Count >= 2 && go.Select(x => x.Item2).Distinct().Count() == 1 && go.Exists(x => x.Item1.StartsWith("scout")) && go.Exists(x => x.Item1.StartsWith("tech"));
            bool planOk = queued >= 2 && whilePaused == 0 && together && !h.Paused;
            parts.Add($"\"plan\": {{\"ok\": {B(planOk)}, \"queued\": {queued}, \"ordersWhilePaused\": {whilePaused}, \"onUnpause\": \"{string.Join(",", go.Select(x => x.Item1 + "@" + x.Item2))}\"}}"); ok &= planOk;
            log.Add($"holdActs plan: {(planOk ? "ok" : "FAILED")}: {queued} queued, {whilePaused} orders while paused; on Space [{string.Join(",", go.Select(x => x.Item1 + "@" + x.Item2))}]");
            // 3 · Tab through an overlap: the guard at the public door (his box and the door's), else any two pickables under one point
            h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null; g = h.Game; h.Select(OpId.Scout, false);
            Vector3? ow = null; int stackN = 0; string before = "", after = "";
            var gd = h.Views.Find("guard"); if (gd) { h.LookAt(gd.position); yield return Frames(3); }
            var cands = new List<Vector3>(); if (gd) { cands.Add(gd.position + Vector3.up * 1.0f); cands.Add(gd.position + Vector3.up * 0.4f); }
            var pk = h.Pickables().Where(x => x.T.Kind != "op").ToList();
            for (int i = 0; i < pk.Count; i++) for (int j = i + 1; j < pk.Count; j++) if ((Sp(h, pk[i].Anchor) - Sp(h, pk[j].Anchor)).magnitude < 14f) cands.Add((pk[i].Anchor + pk[j].Anchor) * 0.5f);
            foreach (var c in cands) { Aim(h, c); yield return Frames(3); if (h.HoverStackCount >= 2) { ow = c; stackN = h.HoverStackCount; break; } }
            int t0 = h.TabSteps;
            if (ow.HasValue) { before = h.Hover == null ? "" : h.Hover.Kind + "/" + h.Hover.Key; Keys(Key.Tab); yield return null; Keys(); yield return Frames(3); after = h.Hover == null ? "" : h.Hover.Kind + "/" + h.Hover.Key; }
            bool tabOk = ow.HasValue && h.TabSteps == t0 + 1 && after.Length > 0 && after != before;
            parts.Add($"\"tab\": {{\"ok\": {B(tabOk)}, \"stack\": {stackN}, \"before\": \"{before}\", \"after\": \"{after}\"}}"); ok &= tabOk;
            log.Add($"holdActs tab: {(tabOk ? "ok" : "FAILED")}: stack {stackN}, {before} → {after}");
            // the Watch's reach ring and the cones: the Scout on the entrance camera
            var ec = h.Views.Find("camera/entrance"); if (ec) { h.Select(OpId.Scout, false); h.LookAt(ec.position); yield return Frames(2); Aim(h, ec.position); yield return Frames(4); log.Add($"watch hover: pill '{HudToolkit.PillWords}', reach {h.WatchReach.HasValue}"); yield return Shoot(dir, "hold-watch-reach", log); }
            // 4 · Sky after 0.2.56: third person (T) takes the view, not the work: the Scout walking, running, or given a Listen carries on after
            // T with no drive sample sent (the engine still walks and acts her); movement input (W) then takes the stick and ends the walk
            HandoverHost.PointerWorld = null; var keeps = new List<string>(); bool keepOk = true;
            foreach (var kind in new[] { "walk", "run", "listen" })
            {
                h.Begin(); h.IntelOpen = false; h.StartClock(); yield return null; g = h.Game; h.Select(OpId.Scout, false); h.SetPaused(false);
                var from = g.Ops[0].Pos; bool given;
                if (kind == "listen")
                {   // a Listen needs her routine (Watch first): the Watch is stepped through, then the Listen walks the Scout in to her
                    h.Issue(new Order { Op = "scout", Do = "watch", Target = "waitress" }); int lim = g.Tick + 1500;
                    while (g.Tick < lim && !g.PersonByRole("waitress").KRoutine) h.RunTo(g.Tick + 5);
                    h.RunTo(g.Tick + 5); from = g.Ops[0].Pos;
                    given = h.Issue(new Order { Op = "scout", Do = "listen", Target = "waitress" });
                }
                else { var to = g.S.Grid.NearestFree(new Pt(from.X + 1600, from.Y + 600)) ?? from; given = h.Issue(new Order { Op = "scout", Do = "move", Point = to, Run = kind == "run" }); }
                yield return new WaitForSecondsRealtime(0.3f); int d0 = g.Orders.Count; var at0 = g.Ops[0].Pos;
                h.DriveToggle(); yield return new WaitForSecondsRealtime(1.5f); int dd0 = g.Orders.Count;
                var op = g.Ops[0]; bool trailing = h.DriveTrailing, driven = g.DrivePoseOf(OpId.Scout).Driven, working = op.Walk != null || op.Busy != null || op.Act != null;
                bool ran = kind != "run" || op.Running; float tpMoved = new Vector2(op.Pos.X - at0.X, op.Pos.Y - at0.Y).magnitude;
                int samples = 0; for (int i = d0; i < g.Orders.Count; i++) if (g.Orders[i].order.Do is "drive" or "cease" or "move") samples++;
                if (kind == "walk") { h.SetPaused(true); yield return Shoot(dir, "hold-tp-keeps-walk", log); h.SetPaused(false); }
                bool landed = true;
                int lateSamples = 0; bool trailedToEnd = true;
                if (kind == "listen")
                {   // the Listen walks in and lands with the view in third person (the clock at 4x: the act is ~50 s): no drive sample until it lands
                    landed = false; h.SetSpeed(4); float tl = Time.realtimeSinceStartup;
                    while (!landed && Time.realtimeSinceStartup - tl < 40f) { for (int i = 0; i < g.Log.Count && !landed; i++) if ((g.Log[i].Code ?? "").StartsWith("scout.listen.")) landed = true; if (!landed) { trailedToEnd &= h.DriveTrailing; yield return null; } }
                    for (int i = dd0; i < g.Orders.Count; i++) if (g.Orders[i].order.Do is "drive" or "cease") lateSamples++;
                    h.SetSpeed(1);
                }
                bool taken = true; string afterW = "";
                if (kind == "walk") { Keys(Key.W); yield return new WaitForSecondsRealtime(0.6f); Keys(); yield return Frames(3); taken = !h.DriveTrailing && g.DrivePoseOf(OpId.Scout).Driven && g.Ops[0].Walk == null; afterW = $"trailing {h.DriveTrailing}, driven {g.DrivePoseOf(OpId.Scout).Driven}, walk {g.Ops[0].Walk != null}"; }
                bool one = given && trailing && !driven && working && ran && (kind == "listen" || tpMoved > 50f) && samples == 0 && landed && taken && lateSamples == 0 && trailedToEnd;
                keeps.Add($"\"{kind}\": {{\"ok\": {B(one)}, \"given\": {B(given)}, \"trailing\": {B(trailing)}, \"driven\": {B(driven)}, \"working\": {B(working)}, \"running\": {B(op.Running)}, \"movedCm\": {F(tpMoved)}, \"samplesOrCease\": {samples}, \"landed\": {B(landed)}, \"samplesBeforeLanding\": {lateSamples}, \"trailedToLanding\": {B(trailedToEnd)}, \"wTakes\": \"{J(afterW)}\"}}");
                log.Add($"holdActs tp keeps {kind}: {(one ? "ok" : "FAILED")}: given {given}, trailing {trailing}, driven {driven}, working {working}, running {op.Running}, moved {F(tpMoved)} cm, samples/cease {samples}, landed {landed}{(afterW.Length > 0 ? ", W: " + afterW : "")}");
                keepOk &= one; if (h.Driving) h.DriveLetGoToMap(); yield return null;
            }
            parts.Add($"\"tpKeeps\": {{\"ok\": {B(keepOk)}, {string.Join(", ", keeps)}}}"); ok &= keepOk;
            HandoverHost.PointerWorld = null; h.controls.planTogether = keepPlan; h.Begin(); Btn(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }

        // ---------------------------------------------------------------- the range: right hold fire, bystander, squad, portrait lock, third person

        static Vector3? Hostile(HandoverHost h, int i = 0) { var hs = h.Game.RangeHostiles().Where(x => !x.Down && !x.Gone).ToList(); return i < hs.Count ? h.DrawnAt(hs[i].Id) : null; }
        static string HostileId(HandoverHost h, int i = 0) { var hs = h.Game.RangeHostiles().Where(x => !x.Down && !x.Gone).ToList(); return i < hs.Count ? hs[i].Id : ""; }

        /// <summary>The agents of the last "hold-fire" row in the host's fire log since `from` ("tick, frame, hold-fire, agents, id").</summary>
        static string HoldFireLine(HandoverHost h, int from, bool focusToo = false) { string r = ""; for (int i = from; i < h.FireLog.Count; i++) { var p = h.FireLog[i].Split('\t'); if (p.Length > 3 && (p[2] == "hold-fire" || focusToo && p[2] == "focus")) r = p[3]; } return r; }

        static IEnumerator Fresh(HandoverHost h)
        {
            HandoverHost.PointerWorld = null; h.ClearHover(); h.rangeLayout = "open"; h.Begin(); yield return null; yield return null;
            h.StartClock(); h.SetPaused(true); if (h.View) h.View.orthographicSize = 14f;
            h.Issue(new Order { Do = "spawn", Target = "police", To = "eff", Count = 2 }); h.Issue(new Order { Do = "spawn", Target = "bystanders", To = "near", Count = 2 }); h.RunTo(h.Game.Tick + 1);
        }

        public static IEnumerator Range(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var parts = new List<string>(); bool ok = true;
            // 1 · the Enforcer, holstered: a right hold on an officer fills; full, the pill says "Let go · Fire at …" and nothing is given yet; the
            // release commits a lasting fire order (draw, aim, fire on its own until down): no cease on release (Sky 2026-10-22: commit on release)
            yield return Fresh(h); var g = h.Game; h.Select(OpId.Enforcer, false);
            var hw = Hostile(h); string hid = HostileId(h);
            if (!hw.HasValue) { json[0] = "{\"ok\": false, \"why\": \"no officer spawned\"}"; yield break; }
            h.LookAt((hw.Value + h.Views.Find("op/enforcer").position) * 0.5f); yield return Frames(2);
            var aimAt = hw.Value + Vector3.up * 1.0f; var sp = Aim(h, aimAt); yield return Frames(3); h.SetPaused(false);
            if (g.Ops[3].Gun.Drawn) { h.Issue(new Order { Op = "enforcer", Do = "holster" }); h.RunTo(g.Tick + 15); }
            bool drawn0 = g.Ops[3].Gun.Drawn; int n0 = g.Orders.Count; var words = new List<string>();
            Btn(sp, right: true); yield return new WaitForSecondsRealtime(0.3f); words.Add(HudToolkit.PillWords);
            yield return new WaitForSecondsRealtime(0.3f); string fullWords = HudToolkit.PillWords, fullState = HudToolkit.PillState; var during = OrdersSince(h, n0);
            int n1 = g.Orders.Count; Btn(Sp(h, aimAt)); yield return Frames(4); var after = OrdersSince(h, n1);
            float t1 = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t1 < 2.2f) { yield return null; var w = HudToolkit.PillWords; if (w.Length > 0 && (words.Count == 0 || words[words.Count - 1] != w)) words.Add(w); }
            bool drawnNow = g.Ops[3].Gun.Drawn; bool lasting = g.Ops[3].Gun.Order == hid || g.GunOf(hid)?.Down == true;
            bool fireOk = !drawn0 && drawnNow && !during.Exists(o => o.StartsWith("enforcer:fire")) && fullState == "ready" && fullWords.StartsWith("Let go · Fire") && after.Exists(o => o == "enforcer:fire:" + hid) && !after.Contains("enforcer:cease") && lasting && words.Exists(w => w.StartsWith("Right hold · Fire"));
            parts.Add($"\"rightHoldFire\": {{\"ok\": {B(fireOk)}, \"drawnBefore\": {B(drawn0)}, \"drawnAfter\": {B(drawnNow)}, \"beforeRelease\": \"{string.Join(",", during)}\", \"release\": \"{string.Join(",", after)}\", \"lasting\": {B(lasting)}, \"ready\": \"{J(fullWords)}\", \"words\": \"{J(string.Join(" | ", words))}\", \"readout\": \"{J(RangePanel.FireReadout)}\"}}"); ok &= fireOk;
            // the pictures, apart from the check (a picture's resize lets go of a held button): a slowed charge, then a hold firing
            yield return Fresh(h); g = h.Game; h.Select(OpId.Enforcer, false); hw = Hostile(h);
            if (hw.HasValue)
            {
                h.LookAt(hw.Value); yield return Frames(2); var pa = hw.Value + Vector3.up * 1.0f; HandoverHost.ChargeScale = 10f; var psp = Aim(h, pa); yield return Frames(3); // centred on the target: the pill clear of the range panel (Glyph 0.2.65)
                Btn(psp, right: true); yield return new WaitForSecondsRealtime(1.2f); log.Add($"fire charge picture: {HudToolkit.PillState} '{HudToolkit.PillWords}'"); h.SetPaused(true); yield return Shoot(dir, "hold-fire-charge", log); h.SetPaused(false); Btn(Sp(h, pa)); yield return Frames(3); HandoverHost.ChargeScale = 1f; // 0.2 s × 10: about 60 % at 1.2 s
                h.SetPaused(false); Btn(Sp(h, pa), right: true); yield return new WaitForSecondsRealtime(0.55f); log.Add($"fire ready picture: {HudToolkit.PillState} '{HudToolkit.PillWords}'"); h.SetPaused(true); yield return Shoot(dir, "hold-fire-ready", log); h.SetPaused(false); Btn(Sp(h, pa)); yield return Frames(3);
                h.SetPaused(false); Btn(Sp(h, pa), right: true); yield return new WaitForSecondsRealtime(0.6f); Btn(Sp(h, pa)); float t2 = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t2 < 3f && !HudToolkit.PillWords.StartsWith("Firing at")) yield return null;
                log.Add($"fire picture: '{HudToolkit.PillWords}'"); h.SetPaused(true); yield return Shoot(dir, "hold-fire-firing", log); h.SetPaused(false);
            }
            log.Add($"holdRange right hold (commit on release): {(fireOk ? "ok" : "FAILED")}: drawn {drawn0}→{drawnNow}; before release [{string.Join(",", during)}] '{fullWords}' ({fullState}); release [{string.Join(",", after)}] lasting {lasting}; words {string.Join(" | ", words)}");
            // 2 · a bystander takes the same hold, no Ctrl (forced in the order)
            yield return Fresh(h); g = h.Game; h.Select(OpId.Enforcer, false); var bs = g.RangeBystanders().FirstOrDefault(b => !b.Down);
            bool byOk = false; string byOrders = ""; string ringHover = "", ringOurs = ""; bool hoverOk = false, oursSeen = false;
            if (bs != null && h.Views.BystanderKey(g, bs.Id) is string bk && h.Views.Find(bk) is Transform bt)
            {
                h.LookAt(bt.position); yield return Frames(2); var bw = bt.position + Vector3.up * 1.0f; var bsp = Aim(h, bw); yield return Frames(3);
                // Glyph 0.2.79 #2 (brief 049): a passer-by under the cursor wears the cream hover ring
                ringHover = ProofRings.RingsNow(h); hoverOk = h.TargetRings.Any(r => r.Id == bs.Id && r.Hover && r.Side == HandoverHost.RingSide.Civilian);
                yield return Shoot(dir, "ring-hover-passerby", log); Aim(h, bw); yield return Frames(2);
                n0 = g.Orders.Count; Btn(bsp, right: true); yield return new WaitForSecondsRealtime(0.7f); Btn(Sp(h, bw)); yield return Frames(3);
                // ... and, once the Enforcer aims at it, the cell's four cream ticks on its cream ring (a non-hostile)
                { bool was = h.Paused; h.SetPaused(false); float ta = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - ta < 2.5f && !h.TargetRings.Any(r => r.Id == bs.Id && r.Ours)) yield return null; h.SetPaused(true);
                  ringOurs = ProofRings.RingsNow(h); oursSeen = h.TargetRings.Any(r => r.Id == bs.Id && r.Ours && r.Side == HandoverHost.RingSide.Civilian);
                  yield return Shoot(dir, "ring-ours-bystander", log); h.SetPaused(was); }
                var bo = new List<string>(); for (int i = n0; i < g.Orders.Count; i++) { var o = g.Orders[i].order; if (o.Do == "fire") bo.Add($"{o.Op}:{o.Target}:force {o.Force}:{(g.Orders[i].ok ? "taken" : g.Orders[i].code)}"); }
                byOrders = string.Join(",", bo); byOk = bo.Exists(x => x.Contains(bs.Id) && x.Contains("force True") && x.EndsWith("taken")) && Keyboard.current != null && !Keyboard.current.ctrlKey.isPressed;
            }
            parts.Add($"\"bystander\": {{\"ok\": {B(byOk)}, \"orders\": \"{J(byOrders)}\"}}"); ok &= byOk;
            parts.Add($"\"ringHover\": {{\"ok\": {B(hoverOk)}, \"rings\": \"{J(ringHover)}\"}}"); ok &= hoverOk;
            parts.Add($"\"ringOurs\": {{\"seen\": {B(oursSeen)}, \"rings\": \"{J(ringOurs)}\"}}");
            log.Add($"rings range: hover {(hoverOk ? "ok" : "FAILED")} [{ringHover}]; ours {(oursSeen ? "seen" : "not seen")} [{ringOurs}]");
            log.Add($"holdRange bystander: {(byOk ? "ok" : "FAILED")}: [{byOrders}]");
            // 3 · the squad: all four selected, the Scout and the Fixer drawn: a right hold gives every selected agent the engine takes the order (item
            // 13, FireQuery: the holstered Enforcer and Tech draw), so the fight 3b needs is on
            yield return Fresh(h); g = h.Game; h.SelectAll(); h.Issue(new Order { Op = "scout", Do = "draw" }); h.Issue(new Order { Op = "fixer", Do = "draw" }); h.RunTo(g.Tick + 12);
            hw = Hostile(h); hid = HostileId(h); var squadFire = new List<string>(); var squadCease = new List<string>(); string holdLine = "";
            if (hw.HasValue)
            {
                h.LookAt(hw.Value); yield return Frames(2); aimAt = hw.Value + Vector3.up * 1.0f; sp = Aim(h, aimAt); yield return Frames(3);
                int f0 = h.FireLog.Count; n0 = g.Orders.Count; Btn(sp, right: true); yield return new WaitForSecondsRealtime(0.7f); Btn(Sp(h, aimAt)); yield return Frames(4);
                for (int i = n0; i < g.Orders.Count; i++) if (g.Orders[i].order.Do == "fire" && g.Orders[i].ok) squadFire.Add(g.Orders[i].order.Op);
                for (int i = n0; i < g.Orders.Count; i++) if (g.Orders[i].order.Do == "cease") squadCease.Add(g.Orders[i].order.Op);
                holdLine = HoldFireLine(h, f0, focusToo: true); // the two drawn draw the police's answer: the fight may be on, and then the press is a focus
            }
            bool squadOk = squadFire.Count == 4 && holdLine == "0,1,2,3" && squadCease.Count == 0; // committed on release: no cease
            parts.Add($"\"squadRightHold\": {{\"ok\": {B(squadOk)}, \"fired\": \"{string.Join(",", squadFire)}\", \"holdFire\": \"{holdLine}\", \"ceased\": \"{string.Join(",", squadCease)}\"}}"); ok &= squadOk;
            log.Add($"holdRange squad: {(squadOk ? "ok" : "FAILED")}: fired [{string.Join(",", squadFire)}], hold-fire {holdLine}, ceased [{string.Join(",", squadCease)}]");
            // 3b · Syndicate fights (engine 0.19): once the fight is on, a right click (a tap, no hold) on the other officer focuses every selected drawn
            // agent on it (fire with focus); someone shoots back: the under-fire cue (UnderFireOf, the face, the ground chevrons); G toggles weapons free
            // (a logged cell order); X stands the drawn agents down under weapons free
            { float tw = Time.realtimeSinceStartup; while (!g.FreeFightOn && Time.realtimeSinceStartup - tw < 4f) yield return null; }
            bool fightOn = g.FreeFightOn; string hid2 = HostileId(h, 1); var hw2 = Hostile(h, 1); var focus = new List<string>(); int fc0 = h.FocusClicks;
            if (fightOn && hw2.HasValue)
            {
                var a2 = hw2.Value + Vector3.up * 1.0f; var s2 = Aim(h, a2); yield return Frames(3); n0 = g.Orders.Count;
                Btn(s2, right: true); yield return Frames(2); Btn(Sp(h, a2)); yield return Frames(4);
                for (int i = n0; i < g.Orders.Count; i++) { var o = g.Orders[i].order; if (o.Do == "fire") focus.Add($"{o.Op}:{o.Target}:{(o.Focus ? "focus" : "plain")}:{(g.Orders[i].ok ? "taken" : g.Orders[i].code)}"); }
                yield return new WaitForSecondsRealtime(0.4f); h.SetPaused(true); yield return Shoot(dir, "hold-focus", log); h.SetPaused(false);
            }
            bool focusOk = fightOn && h.FocusClicks == fc0 + 1 && focus.Count >= 2 && focus.TrueForAll(x => x.Contains(":" + hid2 + ":focus:taken")) && h.Ring == null;
            parts.Add($"\"focusInFight\": {{\"ok\": {B(focusOk)}, \"fightOn\": {B(fightOn)}, \"orders\": \"{string.Join(",", focus)}\"}}"); ok &= focusOk;
            log.Add($"holdRange focus: {(focusOk ? "ok" : "FAILED")}: fight on {fightOn}; [{string.Join(",", focus)}]");
            int ufK = -1; { float tu = Time.realtimeSinceStartup; while (ufK < 0 && Time.realtimeSinceStartup - tu < 8f) { for (int k = 0; k < 4 && ufK < 0; k++) if (h.UnderFireNow[k].Count > 0) ufK = k; if (ufK < 0) yield return null; } }
            string ufFace = ufK >= 0 ? h.FaceOf(ufK).ToString() : "", ufBy = ufK >= 0 ? string.Join(",", h.UnderFireNow[ufK].Select(x => x.By)) : "";
            if (ufK >= 0) { yield return Frames(2); ufFace = h.FaceOf(ufK).ToString(); h.SetPaused(true); HandoverHost.PointerWorld = null; Btn(new Vector2(Screen.width * 0.5f, Screen.height * 0.08f)); yield return Frames(2); yield return Shoot(dir, "hold-under-fire", log); h.SetPaused(false); }
            bool ufOk = ufK >= 0 && ufFace == "UnderFire" && ufBy.Length > 0;
            string ufWhy = ufK >= 0 ? string.Join(",", h.FireWhyNow[ufK].Select(x => x.by + ":" + x.main)) : ""; ufOk &= ufWhy.Length > 0; // engine 0.25.0: why each shooter fires (data)
            parts.Add($"\"underFire\": {{\"ok\": {B(ufOk)}, \"agent\": {ufK}, \"by\": \"{ufBy}\", \"face\": \"{ufFace}\", \"why\": \"{J(ufWhy)}\", \"drawnBefore\": {B(ufK >= 0 && g.Ops[ufK].Gun.Drawn)}}}"); ok &= ufOk;
            log.Add($"holdRange under fire: {(ufOk ? "ok" : "FAILED")}: agent {ufK} shot at by [{ufBy}], face {ufFace}");
            bool wf0 = g.WeaponsFree; n0 = g.Orders.Count; Keys(Key.G); yield return null; Keys(); yield return Frames(3); bool wf1 = g.WeaponsFree; string freeWord = h.Status;
            h.SetPaused(true); yield return Shoot(dir, "hold-free-toggle", log); h.SetPaused(false);
            Keys(Key.G); yield return null; Keys(); yield return Frames(3); bool wf2 = g.WeaponsFree; var freeOrders = OrdersSince(h, n0);
            h.SelectAll(); yield return null; n0 = g.Orders.Count; Keys(Key.X); yield return null; Keys(); yield return Frames(3); var xs = OrdersSince(h, n0);
            int stood = Enumerable.Range(0, 4).Count(k => g.StandDownOf((OpId)k) >= 0);
            bool freeOk = wf0 && !wf1 && wf2 && freeOrders.Count(o => o.StartsWith("cell:free")) == 2 && freeWord == "Return fire only" && (!g.FreeFightOn || xs.Exists(o => o.EndsWith(":cease")) && stood > 0);
            parts.Add($"\"weaponsFree\": {{\"ok\": {B(freeOk)}, \"states\": \"{wf0},{wf1},{wf2}\", \"orders\": \"{string.Join(",", freeOrders)}\", \"word\": \"{J(freeWord)}\", \"x\": \"{string.Join(",", xs)}\", \"stoodDown\": {stood}, \"fightOnAtX\": {B(g.FreeFightOn)}}}"); ok &= freeOk;
            log.Add($"holdRange weapons free: {(freeOk ? "ok" : "FAILED")}: {wf0}→{wf1}→{wf2} [{string.Join(",", freeOrders)}] '{freeWord}'; X [{string.Join(",", xs)}] stood down {stood}");
            // 4 · the Tech's portrait dragged onto an officer: a lock (a lasting fire order), the world at ¼ speed while dragging and 0.5 s after
            yield return Fresh(h); g = h.Game; h.Select(OpId.Scout, false); h.SetPaused(false);
            hw = Hostile(h); hid = HostileId(h); var ps = HudToolkit.PortraitScreen(2); bool slow = false; var lockOrders = new List<string>(); int tickA = 0, tickB = 0; float realA = 0, realB = 0;
            if (hw.HasValue && ps.HasValue)
            {
                HandoverHost.PointerWorld = null; h.LookAt(hw.Value); yield return Frames(3); var to = Sp(h, hw.Value + Vector3.up * 1.0f);
                Btn(ps.Value); yield return Frames(2); Btn(ps.Value, left: true); yield return Frames(3);
                bool viaKit = h.PortraitHeld; if (!viaKit) h.PortraitPress(2, false); log.Add($"portrait press: {(viaKit ? "the kit's PointerDown" : "not delivered to the kit (player unfocused: " + !Application.isFocused + "); pressed through its handler")}");
                for (int s = 1; s <= 12; s++) { Btn(Vector2.Lerp(ps.Value, to, s / 12f), left: true); yield return null; }
                tickA = g.Tick; realA = Time.realtimeSinceStartup; yield return new WaitForSecondsRealtime(1.2f); tickB = g.Tick; realB = Time.realtimeSinceStartup; slow = h.PortraitDragging;
                n0 = g.Orders.Count; Btn(to); yield return Frames(4);
                for (int i = n0; i < g.Orders.Count; i++) if (g.Orders[i].order.Do == "fire") lockOrders.Add($"{g.Orders[i].order.Op}:{g.Orders[i].order.Target}:{(g.Orders[i].ok ? "taken" : g.Orders[i].code)}");
                yield return new WaitForSecondsRealtime(0.6f); h.SetPaused(true); yield return Shoot(dir, "hold-portrait-lock", log); h.SetPaused(false);
            }
            float ticksPerS = realB > realA ? (tickB - tickA) / (realB - realA) : -1f; // 10 at full speed; ~2.5 at a quarter
            bool lockOk = lockOrders.Exists(x => x == $"tech:{hid}:taken") && slow && ticksPerS > 0f && ticksPerS < 4.5f && g.Ops[2].Gun.Order == hid;
            parts.Add($"\"portraitLock\": {{\"ok\": {B(lockOk)}, \"orders\": \"{string.Join(",", lockOrders)}\", \"dragging\": {B(slow)}, \"ticksPerSecond\": {F(ticksPerS)}}}"); ok &= lockOk;
            log.Add($"holdRange portrait lock: {(lockOk ? "ok" : "FAILED")}: [{string.Join(",", lockOrders)}], {F(ticksPerS)} ticks/s while dragging");
            // 5 · third person, "Right hold fires": the right hold fills, let go when full, the driven Enforcer gets a lasting fire order
            yield return Fresh(h); g = h.Game; h.Select(OpId.Enforcer, false); h.SetPaused(false); h.controls.tpRightFires = true; h.DriveToggle(); yield return new WaitForSecondsRealtime(0.8f);
            hw = Hostile(h); hid = HostileId(h); var trig = new List<string>(); string tpReady = "";
            if (h.Driving && hw.HasValue)
            {
                h.FollowFace(hw.Value); yield return Frames(3); aimAt = hw.Value + Vector3.up * 1.0f; sp = Aim(h, aimAt); yield return Frames(3);
                n0 = g.Orders.Count; Btn(sp, right: true); yield return new WaitForSecondsRealtime(0.6f); tpReady = HudToolkit.PillWords; Btn(Sp(h, aimAt)); yield return new WaitForSecondsRealtime(0.8f);
                h.SetPaused(true); yield return Shoot(dir, "hold-tp-fire", log); h.SetPaused(false);
                for (int i = n0; i < g.Orders.Count; i++) { var o = g.Orders[i].order; if (o.Do is "fire" or "trigger" or "cease") trig.Add($"{o.Op}:{o.Do}:{o.Target}:{(g.Orders[i].ok ? "taken" : g.Orders[i].code)}"); }
            }
            bool tpOk = trig.Contains($"enforcer:fire:{hid}:taken") && !trig.Exists(x => x.Contains(":cease:")) && (g.Ops[3].Gun.Order == hid || g.GunOf(hid)?.Down == true) && tpReady.StartsWith("Let go");
            parts.Add($"\"tpRightHold\": {{\"ok\": {B(tpOk)}, \"ready\": \"{J(tpReady)}\", \"orders\": \"{string.Join(",", trig)}\"}}"); ok &= tpOk;
            log.Add($"holdRange third person: {(tpOk ? "ok" : "FAILED")}: '{tpReady}'; [{string.Join(",", trig)}]");
            if (h.Driving) h.DriveLetGoToMap(); yield return null;
            // 5b · Sky after 0.2.56: T on an agent holding a lasting fire order keeps it (no cease; the engine keeps firing) until input
            yield return Fresh(h); g = h.Game; h.Select(OpId.Enforcer, false); h.SetPaused(false); hid = HostileId(h); bool fireGiven = false;
            if (hid.Length > 0) { h.Issue(new Order { Op = "enforcer", Do = "draw" }); h.RunTo(g.Tick + 12); fireGiven = h.Issue(new Order { Op = "enforcer", Do = "fire", Target = hid }); }
            yield return new WaitForSecondsRealtime(0.2f); n0 = g.Orders.Count; h.DriveToggle(); yield return new WaitForSecondsRealtime(0.8f);
            var tpo = OrdersSince(h, n0); bool keptFire = g.Ops[3].Gun.Order == hid || g.GunOf(hid)?.Down == true; bool tpTrail = h.DriveTrailing || g.GunOf(hid)?.Down == true;
            bool tpFireKeepOk = fireGiven && keptFire && tpTrail && !tpo.Exists(o => o.Contains(":cease"));
            parts.Add($"\"tpKeepsFire\": {{\"ok\": {B(tpFireKeepOk)}, \"given\": {B(fireGiven)}, \"kept\": {B(keptFire)}, \"trailing\": {B(h.DriveTrailing)}, \"orders\": \"{string.Join(",", tpo)}\"}}"); ok &= tpFireKeepOk;
            log.Add($"holdRange tp keeps fire: {(tpFireKeepOk ? "ok" : "FAILED")}: given {fireGiven}, kept {keptFire}, trailing {h.DriveTrailing}; [{string.Join(",", tpo)}]");
            if (h.Driving) h.DriveLetGoToMap(); yield return null;
            // the range with the new controls: the panel and its readout
            yield return Fresh(h); h.Select(OpId.Enforcer, false); hw = Hostile(h);
            if (hw.HasValue) { h.LookAt(hw.Value + new Vector3(-4f, 0, 0)); aimAt = hw.Value + Vector3.up; sp = Aim(h, aimAt); yield return Frames(3); h.SetPaused(false); Btn(sp, right: true); yield return new WaitForSecondsRealtime(0.6f); Btn(Sp(h, aimAt)); yield return new WaitForSecondsRealtime(2.0f); h.SetPaused(true); yield return Shoot(dir, "hold-range-panel", log); log.Add($"range readout: '{RangePanel.FireReadout}', fight line '{RangePanel.FightLine}'"); h.SetPaused(false); }
            HandoverHost.PointerWorld = null; Btn(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)); h.Begin();
            // 3 · the squad (briefs/041/next-batch.md item 13, engine 0.26 FireQuery): all four selected, the Enforcer and the Fixer drawn, the
            // Scout and the Tech holstered, the Tech about 21 m off: a right hold gives every one of them the order (the holstered draw, the Tech
            // walks in) and all four fire
            // on the range's car (QF-ALL's target: it takes the four of them for seconds; no police, so nobody is shot first)
            HandoverHost.PointerWorld = null; h.ClearHover(); h.rangeLayout = "open"; h.Begin(); yield return null; yield return null; h.StartClock(); h.SetPaused(true); if (h.View) h.View.orthographicSize = 14f; g = h.Game;
            h.Issue(new Order { Do = "spawn", Target = "car", To = "car", Count = 1 }); h.RunTo(g.Tick + 1); h.SelectAll();
            hid = g.RangeCars().Count > 0 ? g.RangeCars()[0].Id : ""; // "car:range-1" hw = hid.Length > 0 ? h.DrawnAt(hid) : null;
            var allFire = new List<string>(); var allCease = new List<string>(); var fired = new HashSet<int>(); string allHold = ""; string techM = ""; double techD = 0; bool targetDown = false;
            if (hw.HasValue && (g.PosOf(hid) ?? g.CarAt(hid)) is Pt op1)
            {
                Pt? want = null; var tp = g.Ops[2].Pos; double a0 = Math.Atan2(tp.Y - op1.Y, tp.X - op1.X);
                for (int i = 0; i < 24 && want == null; i++) { double a = a0 + (i % 2 == 0 ? 1 : -1) * (i / 2) * Math.PI / 12; var w = new Pt(op1.X + (int)(Math.Cos(a) * 2100), op1.Y + (int)(Math.Sin(a) * 2100)); if (g.S.Grid.Free(w)) want = w; } // 21 m from the car on open ground (the side the Tech is on first)
                if (want is Pt to) { h.Issue(new Order { Op = "tech", Do = "move", Point = to, Run = true }); int lim = g.Tick + 400; while (g.Ops[2].Walk != null && g.Tick < lim) h.RunTo(g.Tick + 2); }
                techD = Math.Sqrt(G.D2(g.Ops[2].Pos, op1)) / 100.0; techM = $"{techD:0.0}";
                h.Issue(new Order { Op = "enforcer", Do = "draw" }); h.Issue(new Order { Op = "fixer", Do = "draw" }); h.RunTo(g.Tick + 12);
                hw = h.DrawnAt(hid) ?? hw; h.LookAt(hw.Value); yield return Frames(2); aimAt = hw.Value + Vector3.up * 1.0f; sp = Aim(h, aimAt); yield return Frames(3);
                int f0 = h.FireLog.Count; n0 = g.Orders.Count; Btn(sp, right: true); yield return new WaitForSecondsRealtime(0.7f); Btn(Sp(h, aimAt)); yield return Frames(4);
                for (int i = n0; i < g.Orders.Count; i++) if (g.Orders[i].order.Do == "fire" && g.Orders[i].ok) allFire.Add(g.Orders[i].order.Op);
                for (int i = n0; i < g.Orders.Count; i++) if (g.Orders[i].order.Do == "cease") allCease.Add(g.Orders[i].order.Op);
                allHold = HoldFireLine(h, f0);
                h.SetPaused(false); float t0 = Time.realtimeSinceStartup; int s0 = g.Log.Count;
                while (Time.realtimeSinceStartup - t0 < 15f && fired.Count < 4) { for (int k = 0; k < 4; k++) if (h.FireNow[k].Firing) fired.Add(k); targetDown = g.CarOf(hid) is { } cc && cc.DisabledAt >= 0; yield return null; } // the engine fires on a disabled car too (QF-ALL counts its shots): watch until all four have fired
                h.SetPaused(true);
            }
            bool allOk = allFire.Count == 4 && allHold == "0,1,2,3" && allCease.Count == 0 && fired.Count == 4 && techD >= 17.0; // the Tech beyond his 16 m sidearm: he walks in // committed on release: no cease
            parts.Add($"\"squadAll\": {{\"ok\": {B(allOk)}, \"fired\": \"{string.Join(",", allFire)}\", \"holdFire\": \"{allHold}\", \"firing\": \"{string.Join(",", fired.OrderBy(x => x))}\", \"techM\": \"{techM}\", \"targetDown\": {B(targetDown)}, \"ceased\": \"{string.Join(",", allCease)}\"}}"); ok &= allOk;
            log.Add($"holdRange squad all four: {(allOk ? "ok" : "FAILED")}: orders [{string.Join(",", allFire)}], hold-fire {allHold}, firing [{string.Join(",", fired.OrderBy(x => x))}] (Tech {techM} m off; target down {targetDown}), ceased [{string.Join(",", allCease)}]");
            // 3a · Arms 1 (the Scout and the Enforcer armed): the Fixer's and the Tech's plates say "no gun", the Scout draws and fires
            HandoverHost.ArmsOverride = 1; yield return Fresh(h); HandoverHost.ArmsOverride = null; g = h.Game; h.SelectAll();
            hw = Hostile(h); hid = HostileId(h); string armsPlates = "", armsOrders = ""; bool scoutDrew = false;
            if (hw.HasValue)
            {
                h.LookAt(hw.Value); yield return Frames(2); aimAt = hw.Value + Vector3.up * 1.0f; sp = Aim(h, aimAt); yield return Frames(3);
                n0 = g.Orders.Count; Btn(sp, right: true); yield return new WaitForSecondsRealtime(0.7f); Btn(Sp(h, aimAt)); yield return Frames(3);
                var ao = new List<string>(); for (int i = n0; i < g.Orders.Count; i++) if (g.Orders[i].order.Do == "fire") ao.Add($"{g.Orders[i].order.Op}:{(g.Orders[i].ok ? "taken" : g.Orders[i].code)}"); armsOrders = string.Join(",", ao);
                armsPlates = $"{h.PlateWords(1)}|{h.PlateWords(2)}"; h.SetPaused(false); float t0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - t0 < 4f && !scoutDrew) { scoutDrew = g.Ops[0].Gun.Drawn; yield return null; }
                h.SetPaused(true); log.Add($"arms1 plates: '{armsPlates}'"); yield return Shoot(dir, "hold-squad-nogun", log);
            }
            bool armsOk = armsPlates == "no gun|no gun" && armsOrders.Contains("scout:taken") && !armsOrders.Contains("fixer") && !armsOrders.Contains("tech") && scoutDrew;
            parts.Add($"\"squadArms1\": {{\"ok\": {B(armsOk)}, \"orders\": \"{armsOrders}\", \"plates\": \"{J(armsPlates)}\", \"scoutDrew\": {B(scoutDrew)}}}"); ok &= armsOk;
            log.Add($"holdRange squad arms 1: {(armsOk ? "ok" : "FAILED")}: [{armsOrders}] plates '{armsPlates}', Scout drew {scoutDrew}");
            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }
    }
}
