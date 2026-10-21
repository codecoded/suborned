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
    /// <summary>Brief 041's proofs for the hold preset ("Click walks, hold acts"), through the Input System's own mouse and keyboard so the host
    /// reads them as it reads the hand; the pointer is held on a world point (HandoverHost.PointerWorld) so a picture's resize never moves it.
    /// Each check writes one JSON object with "ok" (game:check fails on false). The Handover's half: a click walks and gives no act, a hold acts,
    /// an early release cancels, a double click on the ground runs; paused orders start together on one tick; Tab steps an overlap. The range's
    /// half: a right hold fires with the draw in it and the release ceases; a bystander takes the same hold; a squad's right hold fires with
    /// every selected drawn agent; a portrait dropped on an officer locks it at ¼ speed; third person's right hold presses the trigger.
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
            // the hold (0.7 s): the charge fills, the Watch is given (walk-then-act); the pill's words while walking
            n0 = g.Orders.Count; int a0 = h.HoldActsDone;
            { var sp = Aim(h, izw); yield return Frames(2); Btn(sp, left: true); yield return new WaitForSecondsRealtime(0.3f); }
            string charging = HudToolkit.PillWords; string chState = HudToolkit.PillState;
            yield return new WaitForSecondsRealtime(0.5f); Btn(Sp(h, izw)); yield return new WaitForSecondsRealtime(0.4f);
            var held = OrdersSince(h, n0); string pend = h.PendingOf(1).verb; string walking = HudToolkit.PillWords;
            h.SetPaused(true); yield return Shoot(dir, "hold-act-walking", log); h.SetPaused(false);
            // the picture of a half-filled charge, apart from the check (a picture's resize lets go of a held button): the charge slowed, then released (a cancel)
            { HandoverHost.ChargeScale = 30f; var sp2 = Aim(h, izw); yield return Frames(2); Btn(sp2, left: true); yield return new WaitForSecondsRealtime(1.2f); h.SetPaused(true); yield return Shoot(dir, "hold-act-charge", log); h.SetPaused(false); Btn(Sp(h, izw)); yield return Frames(3); HandoverHost.ChargeScale = 1f; }
            bool holdOk = h.HoldActsDone > a0 && (pend == "watch" || held.Exists(o => o.StartsWith("fixer:watch"))) && chState == "charge" && charging.StartsWith("Hold to");
            parts.Add($"\"hold\": {{\"ok\": {B(holdOk)}, \"orders\": \"{string.Join(",", held)}\", \"pending\": \"{pend}\", \"charging\": \"{J(charging)}\", \"walking\": \"{J(walking)}\"}}"); ok &= holdOk;
            log.Add($"holdActs hold: {(holdOk ? "ok" : "FAILED")}: [{string.Join(",", held)}] pending '{pend}'; charge '{charging}'; walking '{walking}'");
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
            HandoverHost.PointerWorld = null; h.controls.planTogether = keepPlan; h.Begin(); Btn(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }

        // ---------------------------------------------------------------- the range: right hold fire, bystander, squad, portrait lock, third person

        static Vector3? Hostile(HandoverHost h, int i = 0) { var hs = h.Game.RangeHostiles().Where(x => !x.Down && !x.Gone).ToList(); return i < hs.Count ? h.DrawnAt(hs[i].Id) : null; }
        static string HostileId(HandoverHost h, int i = 0) { var hs = h.Game.RangeHostiles().Where(x => !x.Down && !x.Gone).ToList(); return i < hs.Count ? hs[i].Id : ""; }

        static IEnumerator Fresh(HandoverHost h)
        {
            HandoverHost.PointerWorld = null; h.ClearHover(); h.rangeLayout = "open"; h.Begin(); yield return null; yield return null;
            h.StartClock(); h.SetPaused(true); if (h.View) h.View.orthographicSize = 14f;
            h.Issue(new Order { Do = "spawn", Target = "police", To = "eff", Count = 2 }); h.Issue(new Order { Do = "spawn", Target = "bystanders", To = "near", Count = 2 }); h.RunTo(h.Game.Tick + 1);
        }

        public static IEnumerator Range(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var parts = new List<string>(); bool ok = true;
            // 1 · the Enforcer, holstered: a right hold on an officer charges, fires with the draw in it, keeps firing while held; the release ceases
            yield return Fresh(h); var g = h.Game; h.Select(OpId.Enforcer, false);
            var hw = Hostile(h); string hid = HostileId(h);
            if (!hw.HasValue) { json[0] = "{\"ok\": false, \"why\": \"no officer spawned\"}"; yield break; }
            h.LookAt((hw.Value + h.Views.Find("op/enforcer").position) * 0.5f); yield return Frames(2);
            var aimAt = hw.Value + Vector3.up * 1.0f; var sp = Aim(h, aimAt); yield return Frames(3); h.SetPaused(false);
            if (g.Ops[3].Gun.Drawn) { h.Issue(new Order { Op = "enforcer", Do = "holster" }); h.RunTo(g.Tick + 15); }
            bool drawn0 = g.Ops[3].Gun.Drawn; int n0 = g.Orders.Count; var words = new List<string>();
            Btn(sp, right: true); yield return new WaitForSecondsRealtime(0.3f); words.Add(HudToolkit.PillWords);
            float t1 = Time.realtimeSinceStartup; bool shotTaken = false;
            while (Time.realtimeSinceStartup - t1 < 2.6f) { yield return null; var w = HudToolkit.PillWords; if (w.Length > 0 && (words.Count == 0 || words[words.Count - 1] != w)) words.Add(w); }
            var during = OrdersSince(h, n0); bool drawnNow = g.Ops[3].Gun.Drawn; bool firingHeld = g.Ops[3].Gun.Order == hid || g.GunOf(hid)?.Down == true;
            int n1 = g.Orders.Count; Btn(Sp(h, aimAt)); yield return Frames(4); var after = OrdersSince(h, n1);
            bool fireOk = !drawn0 && drawnNow && during.Exists(o => o == "enforcer:fire:" + hid) && firingHeld && (after.Contains("enforcer:cease") || g.GunOf(hid)?.Down == true) && words.Exists(w => w.StartsWith("Hold to fire"));
            parts.Add($"\"rightHoldFire\": {{\"ok\": {B(fireOk)}, \"drawnBefore\": {B(drawn0)}, \"drawnAfter\": {B(drawnNow)}, \"orders\": \"{string.Join(",", during)}\", \"release\": \"{string.Join(",", after)}\", \"words\": \"{J(string.Join(" | ", words))}\", \"readout\": \"{J(RangePanel.FireReadout)}\"}}"); ok &= fireOk;
            // the pictures, apart from the check (a picture's resize lets go of a held button): a slowed charge, then a hold firing
            yield return Fresh(h); g = h.Game; h.Select(OpId.Enforcer, false); hw = Hostile(h);
            if (hw.HasValue)
            {
                h.LookAt((hw.Value + h.Views.Find("op/enforcer").position) * 0.5f); yield return Frames(2); var pa = hw.Value + Vector3.up * 1.0f; HandoverHost.ChargeScale = 30f; var psp = Aim(h, pa); yield return Frames(3);
                Btn(psp, right: true); yield return new WaitForSecondsRealtime(1.2f); h.SetPaused(true); yield return Shoot(dir, "hold-fire-charge", log); h.SetPaused(false); Btn(Sp(h, pa)); yield return Frames(3); HandoverHost.ChargeScale = 1f;
                h.SetPaused(false); Btn(Sp(h, pa), right: true); float t2 = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t2 < 3f && !HudToolkit.PillWords.StartsWith("Firing at")) yield return null;
                log.Add($"fire picture: '{HudToolkit.PillWords}'"); h.SetPaused(true); yield return Shoot(dir, "hold-fire-firing", log); h.SetPaused(false); Btn(Sp(h, pa)); yield return Frames(3);
            }
            log.Add($"holdRange right hold: {(fireOk ? "ok" : "FAILED")}: drawn {drawn0}→{drawnNow}; [{string.Join(",", during)}] release [{string.Join(",", after)}]; words {string.Join(" | ", words)}");
            // 2 · a bystander takes the same hold, no Ctrl (forced in the order)
            yield return Fresh(h); g = h.Game; h.Select(OpId.Enforcer, false); var bs = g.RangeBystanders().FirstOrDefault(b => !b.Down);
            bool byOk = false; string byOrders = "";
            if (bs != null && h.Views.BystanderKey(g, bs.Id) is string bk && h.Views.Find(bk) is Transform bt)
            {
                h.LookAt(bt.position); yield return Frames(2); var bw = bt.position + Vector3.up * 1.0f; var bsp = Aim(h, bw); yield return Frames(3);
                n0 = g.Orders.Count; Btn(bsp, right: true); yield return new WaitForSecondsRealtime(0.7f); Btn(Sp(h, bw)); yield return Frames(3);
                var bo = new List<string>(); for (int i = n0; i < g.Orders.Count; i++) { var o = g.Orders[i].order; if (o.Do == "fire") bo.Add($"{o.Op}:{o.Target}:force {o.Force}:{(g.Orders[i].ok ? "taken" : g.Orders[i].code)}"); }
                byOrders = string.Join(",", bo); byOk = bo.Exists(x => x.Contains(bs.Id) && x.Contains("force True") && x.EndsWith("taken")) && Keyboard.current != null && !Keyboard.current.ctrlKey.isPressed;
            }
            parts.Add($"\"bystander\": {{\"ok\": {B(byOk)}, \"orders\": \"{J(byOrders)}\"}}"); ok &= byOk;
            log.Add($"holdRange bystander: {(byOk ? "ok" : "FAILED")}: [{byOrders}]");
            // 3 · the squad: all four selected, the Scout and the Fixer drawn: a right hold fires with every selected drawn agent
            yield return Fresh(h); g = h.Game; h.SelectAll(); h.Issue(new Order { Op = "scout", Do = "draw" }); h.Issue(new Order { Op = "fixer", Do = "draw" }); h.RunTo(g.Tick + 12);
            hw = Hostile(h); hid = HostileId(h); var squadFire = new List<string>(); var squadCease = new List<string>();
            if (hw.HasValue)
            {
                h.LookAt(hw.Value); yield return Frames(2); aimAt = hw.Value + Vector3.up * 1.0f; sp = Aim(h, aimAt); yield return Frames(3);
                n0 = g.Orders.Count; Btn(sp, right: true); yield return new WaitForSecondsRealtime(0.7f);
                for (int i = n0; i < g.Orders.Count; i++) if (g.Orders[i].order.Do == "fire" && g.Orders[i].ok) squadFire.Add(g.Orders[i].order.Op);
                n1 = g.Orders.Count; Btn(Sp(h, aimAt)); yield return Frames(4);
                for (int i = n1; i < g.Orders.Count; i++) if (g.Orders[i].order.Do == "cease") squadCease.Add(g.Orders[i].order.Op);
            }
            bool squadOk = squadFire.Count == 2 && squadFire.Contains("scout") && squadFire.Contains("fixer") && squadCease.Count == 2;
            parts.Add($"\"squadRightHold\": {{\"ok\": {B(squadOk)}, \"fired\": \"{string.Join(",", squadFire)}\", \"ceased\": \"{string.Join(",", squadCease)}\"}}"); ok &= squadOk;
            log.Add($"holdRange squad: {(squadOk ? "ok" : "FAILED")}: fired [{string.Join(",", squadFire)}], ceased [{string.Join(",", squadCease)}]");
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
            // 5 · third person, "Right hold fires": the driven Enforcer draws and presses the trigger; the release lets go
            yield return Fresh(h); g = h.Game; h.Select(OpId.Enforcer, false); h.SetPaused(false); h.controls.tpRightFires = true; h.DriveToggle(); yield return new WaitForSecondsRealtime(0.8f);
            hw = Hostile(h); hid = HostileId(h); var trig = new List<string>(); bool aimed = false;
            if (h.Driving && hw.HasValue)
            {
                h.FollowFace(hw.Value); yield return Frames(3); aimAt = hw.Value + Vector3.up * 1.0f; sp = Aim(h, aimAt); yield return Frames(3);
                n0 = g.Orders.Count; Btn(sp, right: true); yield return new WaitForSecondsRealtime(1.2f); aimed = h.Aiming && h.TriggerDown;
                h.SetPaused(true); yield return Shoot(dir, "hold-tp-fire", log); h.SetPaused(false);
                Btn(Sp(h, aimAt)); yield return Frames(4);
                for (int i = n0; i < g.Orders.Count; i++) { var o = g.Orders[i].order; if (o.Do is "trigger" or "draw") trig.Add($"{o.Do}:{(o.Do == "trigger" ? (o.On ? "on" : "off") : "")}:{(g.Orders[i].ok ? "taken" : g.Orders[i].code)}"); }
            }
            bool tpOk = aimed && trig.Contains("trigger:on:taken") && trig.Exists(x => x.StartsWith("trigger:off")) && !h.TriggerDown;
            parts.Add($"\"tpRightHold\": {{\"ok\": {B(tpOk)}, \"aimedAndPressed\": {B(aimed)}, \"orders\": \"{string.Join(",", trig)}\"}}"); ok &= tpOk;
            log.Add($"holdRange third person: {(tpOk ? "ok" : "FAILED")}: aimed+pressed {aimed}; [{string.Join(",", trig)}]");
            if (h.Driving) h.DriveLetGoToMap(); yield return null;
            // the range with the new controls: the panel and its readout
            yield return Fresh(h); h.Select(OpId.Enforcer, false); hw = Hostile(h);
            if (hw.HasValue) { h.LookAt(hw.Value + new Vector3(-4f, 0, 0)); aimAt = hw.Value + Vector3.up; sp = Aim(h, aimAt); yield return Frames(3); h.SetPaused(false); Btn(sp, right: true); yield return new WaitForSecondsRealtime(2.6f); h.SetPaused(true); yield return Shoot(dir, "hold-range-panel", log); log.Add($"range readout: '{RangePanel.FireReadout}'"); h.SetPaused(false); Btn(Sp(h, aimAt)); yield return Frames(3); }
            HandoverHost.PointerWorld = null; Btn(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)); h.Begin();
            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }
    }
}
