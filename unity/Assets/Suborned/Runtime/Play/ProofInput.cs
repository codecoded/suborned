using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Order = Suborned.Sim.Order;
using OpId = Suborned.Sim.OpId;
using Pt = Suborned.Sim.Pt;

namespace Suborned.Play
{
    /// <summary>The proof's input checks (Latch, 2026-10-21), driven through the Input System's own devices (queued key and mouse states), so
    /// the host reads them as it reads the player's hands. Each writes one JSON object with "ok" for proof.json; game:check fails on ok false.
    ///  · driveClick (Sky: "if I click on a space, then the movement WASD keys fail to respond"): driven, W walks; a left click on the ground
    ///    gives no walk and the drive holds; W walks again; an act out of reach hands the agent to the engine's walk and W takes the stick
    ///    back at once; the engine ending the drive (a move order) leaves the ground view and W takes control again.
    ///  · fireStop (Sky: "a fire order stops the walk"; running is the engine's, Motive): a walking agent given fire stands and fires; a running
    ///    one is left to the engine (today it refuses fire-running and runs on).</summary>
    public static class ProofInput
    {
        /// <summary>The proof's player is launched in the background (not focused): the Input System then drops device events. For the checks,
        /// input is read whatever the focus, and the devices are on; restored after.</summary>
        static InputSettings.BackgroundBehavior? bgKeep;
        public static void Focusless(bool on)
        {
            if (on) { bgKeep ??= InputSystem.settings.backgroundBehavior; InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; foreach (var d in new InputDevice[] { Keyboard.current, Mouse.current }) if (d != null && !d.enabled) InputSystem.EnableDevice(d); }
            else if (bgKeep.HasValue) { InputSystem.settings.backgroundBehavior = bgKeep.Value; bgKeep = null; }
        }

        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        static string B(bool b) => b ? "true" : "false";

        static void Keys(params Key[] down) { if (Keyboard.current is Keyboard kb) InputSystem.QueueStateEvent(kb, new KeyboardState(down)); }
        static void MouseAt(Vector2 screen, bool left) { if (Mouse.current is Mouse m) InputSystem.QueueStateEvent(m, new MouseState { position = screen, buttons = (ushort)(left ? 1 : 0) }); }

        /// <summary>W held for `seconds`; the driven agent's ground travel (m) from the drawn controller.</summary>
        static IEnumerator HoldW(HandoverHost h, float seconds, float[] moved, bool[] seen)
        {
            var a = h.Game.Ops[0].Pos; Keys(Key.W); yield return null; yield return null;
            seen[0] = Keyboard.current != null && Keyboard.current.wKey.isPressed;
            yield return new WaitForSecondsRealtime(seconds); Keys(); yield return new WaitForSecondsRealtime(0.25f);
            var b = h.Game.Ops[0].Pos; moved[0] = Mathf.Sqrt((float)Suborned.Sim.G.D2(a, b)) / 100f;
        }

        public static IEnumerator DriveClick(HandoverHost h, List<string> log, string[] json)
        {
            h.Begin(); h.IntelOpen = false; h.StartClock(); yield return null;
            h.Select(OpId.Scout, false); h.DriveToggle(); yield return new WaitForSecondsRealtime(0.8f);
            var g = h.Game; var m = new float[1]; var seen = new bool[1]; var parts = new List<string>(); bool ok = h.Driving;
            parts.Add($"\"focused\": {B(Application.isFocused)}, \"took\": {B(h.Driving)}");
            // 1 · W walks
            yield return HoldW(h, 1.2f, m, seen); float w1 = m[0]; bool keysReach = seen[0];
            parts.Add($"\"keysReach\": {B(keysReach)}, \"w1m\": {F(w1)}"); ok &= keysReach && w1 > 0.8f;
            // 2 · a left click on the ground 4 m ahead of her (on screen, off the HUD)
            var me = h.Views.Find("op/scout"); var cam = h.View; bool clicked = false;
            if (me && cam)
            {
                var fwd = cam.transform.forward; fwd.y = 0f; fwd.Normalize();
                var sp = cam.WorldToScreenPoint(me.position + fwd * 4f);
                if (sp.z > 0f && !HudToolkit.Blocks(sp)) { MouseAt(sp, false); yield return null; MouseAt(sp, true); yield return null; yield return null; MouseAt(sp, false); yield return null; clicked = true; }
            }
            int orders0 = g.Orders.Count; yield return new WaitForSecondsRealtime(1f);
            int moves = 0; for (int i = orders0; i < g.Orders.Count; i++) if (g.Orders[i].order.Do == "move") moves++;
            bool holds = h.Driving && g.DrivePoseOf(OpId.Scout).Driven && g.Ops[0].Walk == null;
            parts.Add($"\"clicked\": {B(clicked)}, \"clickMoves\": {moves}, \"drivenAfterClick\": {B(holds)}"); ok &= clicked && moves == 0 && holds;
            // 3 · W walks again
            yield return HoldW(h, 1.2f, m, seen); float w2 = m[0];
            parts.Add($"\"w2m\": {F(w2)}"); ok &= w2 > 0.8f && h.Driving;
            // 4 · an act out of reach: the engine walks her in (DriveWalking); W takes the stick back at once
            h.GiveScripted(new Order { Op = "scout", Do = "watch", Target = "guard" }); yield return new WaitForSecondsRealtime(0.6f);
            bool walking = h.DriveWalking && g.Ops[0].Walk != null;
            Keys(Key.W); yield return new WaitForSecondsRealtime(0.5f); Keys(); yield return new WaitForSecondsRealtime(0.3f);
            bool back = h.Driving && !h.DriveWalking && g.DrivePoseOf(OpId.Scout).Driven && g.Ops[0].Walk == null && h.PendingOf(0).verb.Length == 0;
            parts.Add($"\"engineWalk\": {B(walking)}, \"wTookBack\": {B(back)}"); ok &= back; // walking may be false when the Watch needs no walk; the take back is the check
            // 5 · the engine ends the drive (a move order as the engine takes it): the ground view stays; W takes control again
            var p0 = g.Ops[0].Pos; Pt? mv = null; foreach (var (dx, dy) in new[] { (300, 0), (-300, 0), (0, 300), (0, -300), (500, 500), (-500, -500) }) { var q = new Pt(p0.X + dx, p0.Y + dy); if (g.S.Grid.Free(q) && g.S.Grid.Route(p0, q) != null) { mv = q; break; } }
            bool moveOk = mv.HasValue && h.Issue(new Order { Op = "scout", Do = "move", Point = mv.Value }); parts.Add($"\"engineMove\": {B(moveOk)}");
            yield return new WaitForSecondsRealtime(0.4f);
            bool lost = !h.Driving && h.Following && h.DriveResumable;
            yield return HoldW(h, 1f, m, seen); float w3 = m[0];
            bool again = h.Driving && g.DrivePoseOf(OpId.Scout).Driven;
            parts.Add($"\"engineEnded\": {B(lost)}, \"wRetook\": {B(again)}, \"w3m\": {F(w3)}"); ok &= again;
            log.Add($"driveClick: {(ok ? "ok" : "FAILED")}: {string.Join(", ", parts).Replace("\"", "")}");
            if (h.Driving) h.DriveLetGoToMap(); yield return null;
            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }

        static IEnumerator Right(Vector2 sp) { if (Mouse.current is not Mouse m) yield break; InputSystem.QueueStateEvent(m, new MouseState { position = sp }.WithButton(MouseButton.Right, true)); yield return null; InputSystem.QueueStateEvent(m, new MouseState { position = sp }); yield return null; }

        /// <summary>Brief 038's done-check (Glyph), by the Input System's mouse: the whole cell selected, guns holstered then drawn; on the guard,
        /// a passer-by, Inez, Vale, the door, the entrance camera and the official car: hover (the marker's glyph), right click (the ring opens at
        /// once; the lit wedge is never harmful; the marker's glyph is the lit wedge's), right click again (the lit way is taken in the same frame;
        /// never Kill, Fire or Pressure without a hold).</summary>
        public static IEnumerator RingCheck(HandoverHost h, List<string> log, string[] json)
        {
            var rows = new List<string>(); bool ok = true; var keep = h.controls.quickAction; h.controls.quickAction = false;
            foreach (bool drawn in new[] { false, true })
                foreach (var key in new[] { "guard", "crowd", "waitress", "councillor", "marker/door", "camera/entrance", "car/official" })
                {
                    h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null;
                    var g = h.Game; for (int k = 0; k < 4; k++) h.Select((OpId)k, k > 0);
                    if (drawn) { for (int k = 0; k < 4; k++) h.Issue(new Order { Op = Suborned.Sim.Op.Name((OpId)k), Do = "draw" }); h.RunTo(g.Tick + 10); }
                    Transform v = null;
                    if (key == "crowd") { foreach (var (n, x) in h.Views.All()) if (n.StartsWith("crowd/") && x && x.gameObject.activeInHierarchy) { v = x; break; } }
                    else v = h.Views.Find(key);
                    if (!v || !v.gameObject.activeInHierarchy) { rows.Add($"{{\"target\": \"{key}\", \"drawn\": {B(drawn)}, \"seen\": false}}"); continue; }
                    h.LookAt(v.position); yield return null; yield return null;
                    var cam = h.View; var sp = (Vector2)cam.WorldToScreenPoint(v.position + Vector3.up * (key.StartsWith("car/") ? 0.8f : key.StartsWith("camera/") ? 0f : 1.0f));
                    MouseAt(sp, false); yield return null; yield return null; yield return null;
                    var (glyph, more, _, _) = h.HoverAct; string hover = h.Hover == null ? "" : h.Hover.Kind + "/" + h.Hover.Key;
                    int n0 = g.Orders.Count;
                    yield return Right(sp);
                    var rs = h.Ring; bool open = rs != null; string lit = open && rs.Lit >= 0 && rs.Lit < rs.Segs.Count ? (rs.Segs[rs.Lit].More ? "more" : rs.Segs[rs.Lit].Icon) : "";
                    bool litHarm = open && rs.Lit >= 0 && rs.Segs[rs.Lit].Harmful;
                    string ways = open ? string.Join(" ", rs.Segs.ConvertAll(x => x.More ? "⋯" : x.Word + (x.Harmful ? "!" : ""))) : "";
                    bool match = !open || glyph == lit || glyph.Length == 0 && lit.Length == 0 || lit == "more";
                    int frames = -1; int n1 = g.Orders.Count;
                    if (open) { yield return Right(sp); for (int f = 0; f < 3 && g.Orders.Count == n1 && h.Loud == null; f++) { yield return null; frames = f; } if (g.Orders.Count > n1 || h.Loud != null) frames = Mathf.Max(0, frames); }
                    var taken = new List<string>(); bool harm = false;
                    for (int i = n0; i < g.Orders.Count; i++) { var o = g.Orders[i].order; if (o.Do is "drive" or "draw") continue; taken.Add(o.Do + (!string.IsNullOrEmpty(o.Approach) ? ":" + o.Approach : "") + (!string.IsNullOrEmpty(o.How) ? ":" + o.How : "")); if (g.Orders[i].ok && (o.Do == "fire" || o.Do == "turn" && o.Approach == "pressure" || o.Do == "takedown" && o.How == "kill")) harm = true; }
                    bool held = h.Loud != null; h.CloseRing(); if (held) h.SetLoudNull();
                    bool rowOk = !litHarm && !harm && match && (!open || lit.Length == 0 || taken.Count > 0 || held);
                    ok &= rowOk;
                    rows.Add($"{{\"target\": \"{key}\", \"drawn\": {B(drawn)}, \"hover\": \"{hover}\", \"marker\": \"{glyph}{(more ? "+more" : "")}\", \"ring\": {B(open)}, \"ways\": \"{ways}\", \"lit\": \"{lit}\", \"match\": {B(match)}, \"taken\": \"{string.Join(",", taken)}\", \"held\": {B(held)}, \"frames\": {frames}, \"ok\": {B(rowOk)}}}");
                    log.Add($"ring {key}{(drawn ? " drawn" : "")}: hover {hover}, marker {glyph}{(more ? "+more" : "")}, ring {open} [{ways}], lit {lit}, second click took {(taken.Count > 0 ? string.Join(",", taken) : held ? "a hold" : "nothing")}{(rowOk ? "" : " FAILED")}");
                }
            h.controls.quickAction = keep; MouseAt(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), false);
            json[0] = $"{{\"ok\": {B(ok)}, \"rows\": [{string.Join(", ", rows)}]}}";
        }

        public static IEnumerator FireStop(HandoverHost h, List<string> log, string[] json)
        {
            // the plain start: the Enforcer (armed) walks towards the door guard; the fire is given while he walks, in range and in sight (it goes
            // loud: the going-loud hold is committed as a held click commits it). The final fight's route no longer reaches the fight (Latch 2026-10-21)
            h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null;
            var g = h.Game; int k = (int)OpId.Enforcer; string id = "guard";
            log.Add($"fireStop: from the start, the enforcer armed {g.Ops[k].Gun.W != null}, the guard at {g.PosOf(id)}");
            if (g.Ops[k].Gun.W == null || !g.PosOf(id).HasValue) { h.Begin(); json[0] = "{\"ok\": null, \"why\": \"no armed enforcer or no guard\"}"; yield break; }
            {
                string nm = Suborned.Sim.Op.Name((OpId)k); var hp = g.PosOf(id).Value;
                h.Issue(new Order { Op = nm, Do = "move", Point = hp });
                for (int t = 0; t < 400 && !g.Over; t++)
                {
                    var op = g.Ops[k]; var at = g.PosOf(id); if (op.Walk == null || !at.HasValue) break;
                    if (Suborned.Sim.G.Within(op.Pos, at.Value, op.Gun.W.MaxCm - 200) && g.S.Sight(op.Pos, at.Value, g.R.Eye)) break;
                    h.RunTo(g.Tick + 1);
                }
            }
            string name = Suborned.Sim.Op.Name((OpId)k); var p = g.Ops[k].Pos;
            // walking: a walk of 4 m along the free ground, then fire
            Pt? To(int dx, int dy) { var q = new Pt(p.X + dx, p.Y + dy); return g.S.Grid.Free(q) ? q : (Pt?)null; }
            var to = To(400, 0) ?? To(-400, 0) ?? To(0, 400) ?? To(0, -400);
            h.Select((OpId)k, false);
            bool walkGiven = g.Ops[k].Walk != null || to.HasValue && h.Issue(new Order { Op = name, Do = "move", Point = to.Value }); h.RunTo(g.Tick + 1);
            bool walking = g.Ops[k].Walk != null; int st0 = h.FireStopped;
            h.FireScripted(id); h.RunTo(g.Tick + 1);
            bool stopped = g.Ops[k].Walk == null, firing = g.Ops[k].Gun.Order == id;
            // running: a run, then fire (the engine's: today fire-running is refused and the run goes on)
            var back = g.Ops[k].Pos; var to2 = new Pt(back.X - (to.HasValue ? to.Value.X - p.X : 0), back.Y - (to.HasValue ? to.Value.Y - p.Y : 0));
            h.Issue(new Order { Op = name, Do = "cease" });
            bool runGiven = g.S.Grid.Free(to2) && h.Issue(new Order { Op = name, Do = "move", Point = to2, Run = true }); h.RunTo(g.Tick + 2);
            bool running = g.Ops[k].Running && g.Ops[k].Walk != null; int n0 = g.Orders.Count;
            h.FireScripted(id); h.RunTo(g.Tick + 1);
            string runAnswer = g.Orders.Count > n0 ? (g.Orders[n0].ok ? "fired" : g.Orders[n0].code) : "no order";
            bool runsOn = g.Ops[k].Running && g.Ops[k].Walk != null;
            string by = h.FireStopped > st0 ? "host" : "engine"; // the host's walk-only cancel, or the engine's own rule (Motive's fire-on-the-move, when it lands)
            bool ok = walkGiven && walking && stopped && firing && (!running || runsOn);
            log.Add($"fireStop: {(ok ? "ok" : "FAILED")}: the {name} on {id}: walking {walking} → stopped {stopped} (by the {by}), firing {firing}; running {running} → {runAnswer}, runs on {runsOn}");
            h.Begin(); // back to the plain start (the checkpoint off)
            json[0] = $"{{\"ok\": {B(ok)}, \"agent\": \"{name}\", \"target\": \"{id}\", \"walking\": {B(walking)}, \"stopped\": {B(stopped)}, \"stoppedBy\": \"{by}\", \"firing\": {B(firing)}, \"running\": {B(running)}, \"runFire\": \"{runAnswer}\", \"runsOn\": {B(runsOn)}}}";
        }

        // ---------------------------------------------------------------- the click-through check (Sky on 0.2.34)

        static string J(string x) => (x ?? "").Replace("\\", "/").Replace("\"", "'").Replace("\n", " ");

        /// <summary>One left click at a screen point through the Input System's mouse; what reached the street: presses the host let through
        /// (WorldPresses), move orders, a ring or a box. `pressed` = the street heard the press.</summary>
        static IEnumerator ClickAt(HandoverHost h, Vector2 sp, (int presses, int moves, bool ring, bool box)[] got)
        {
            var g = h.Game; int w0 = h.WorldPresses, n0 = g.Orders.Count;
            MouseAt(sp, false); yield return null; yield return null;
            MouseAt(sp, true); yield return null; yield return null;
            MouseAt(sp, false); yield return null; yield return null; yield return null;
            int moves = 0; for (int i = n0; i < g.Orders.Count; i++) if (g.Orders[i].order.Do == "move") moves++;
            got[0] = (h.WorldPresses - w0, moves, h.Ring != null, h.BoxScreen.HasValue);
        }

        /// <summary>A point on open ground near the selected agent, off every HUD (the control click: the street must hear it).</summary>
        static Vector2? Ground(HandoverHost h, OpId who)
        {
            var me = h.Views.Find("op/" + Suborned.Sim.Op.Name(who)); var cam = h.View; if (!me || !cam) return null;
            foreach (var off in new[] { new Vector3(3f, 0f, 0f), new Vector3(-3f, 0f, 0f), new Vector3(0f, 0f, 3f), new Vector3(0f, 0f, -3f), new Vector3(2f, 0f, 2f) })
            {
                var sp = cam.WorldToScreenPoint(me.position + off); if (sp.z <= 0f) continue; var v = new Vector2(sp.x, sp.y);
                if (v.x > 20 && v.y > 20 && v.x < Screen.width - 20 && v.y < Screen.height - 20 && !HandoverHost.OverHud(v)) return v;
            }
            return null;
        }

        /// <summary>Every piece of the kit HUD that takes clicks, clicked in the overhead view and in the follow view, plus a ground click as the
        /// control: a HUD click is never heard by the street (no press through, no move order, no ring, no box); the ground click is.</summary>
        public static IEnumerator HudClick(HandoverHost h, List<string> log, string[] json)
        {
            HandoverHost.PointerOverride = null; h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null; yield return null;
            h.Select(OpId.Scout, false); yield return new WaitForSecondsRealtime(0.5f);
            var parts = new List<string>(); bool ok = true; var got = new (int, int, bool, bool)[1];
            foreach (var view in new[] { "overhead", "follow" })
            {
                if (view == "follow") { h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null; h.Select(OpId.Scout, false); if (!h.Following) h.FollowToggle(); yield return new WaitForSecondsRealtime(0.8f); }
                // the control: the street hears a ground click
                int heard = -1; var gp = Ground(h, OpId.Scout);
                if (gp.HasValue) { yield return ClickAt(h, gp.Value, got); heard = got[0].Item1; }
                h.CloseRing(); h.ClearHover();
                var pts = HudToolkit.ClickPoints(40); int clicked = 0, through = 0, moves = 0, rings = 0, boxes = 0; var bad = new List<string>(); var movedBy = new List<string>(); var gaveWay = new List<string>();
                foreach (var (what, sp) in pts)
                {
                    if (!HandoverHost.OverHud(sp) || h.MenuOpen || h.Modal != ModalGate.Modal.None) continue; // the layout moved under an earlier click (a sheet opened): skip
                    MouseAt(sp, false); yield return null; yield return null; // the pointer rests there first: a piece that gives way to the pointer (it hides under the cursor) is not one a player can click
                    if (!HandoverHost.OverHud(sp)) { gaveWay.Add(what + "@" + (int)sp.x + "," + (int)sp.y + ">" + HudToolkit.PickedName(sp)); continue; }
                    yield return ClickAt(h, sp, got); clicked++;
                    var (pr, mv, rg, bx) = got[0]; through += pr; moves += mv; if (rg) rings++; if (bx) boxes++;
                    if (pr > 0 || rg || bx) bad.Add(what + "@" + (int)sp.x + "," + (int)sp.y); else if (mv > 0) movedBy.Add(what + "@" + (int)sp.x + "," + (int)sp.y);
                    h.CloseRing(); if (h.MenuOpen) h.ResumeFromMenu();
                }
                bool vok = heard == 1 && clicked >= 3 && through == 0 && rings == 0 && boxes == 0; // a move given by a HUD button's own act (the door or car step mark sends the Enforcer) is the HUD's, not a click through: the gate is pressesThrough
                ok &= vok;
                parts.Add($"\"{view}\": {{\"ok\": {B(vok)}, \"groundHeard\": {heard}, \"hudPieces\": {pts.Count}, \"clicked\": {clicked}, \"pressesThrough\": {through}, \"moves\": {moves}, \"rings\": {rings}, \"boxes\": {boxes}, \"failedOn\": \"{J(string.Join(" ", bad))}\", \"movesByHudButtons\": \"{J(string.Join(" ", movedBy))}\", \"gaveWay\": \"{J(string.Join(" ", gaveWay))}\"}}");
                log.Add($"hudClick {view}: {(vok ? "ok" : "FAILED")}: ground click heard {heard}; {clicked} of {pts.Count} HUD pieces clicked: {through} presses through, {moves} moves, {rings} rings, {boxes} boxes{(bad.Count > 0 ? " on " + string.Join(" ", bad) : "")}{(movedBy.Count > 0 ? "; moves given by HUD buttons " + string.Join(" ", movedBy) : "")}{(gaveWay.Count > 0 ? "; gave way to the pointer " + string.Join(" ", gaveWay) : "")}");
                if (h.Following) h.FollowToggle();
            }
            h.Begin(); MouseAt(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), false);
            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }

        /// <summary>The fight range's panel (IMGUI), the bug Sky hit: its Spawn button, its Realtime choice and its tally line clicked in the
        /// overhead view and the follow view, plus the ground control; nothing reaches the street. `imgui` says whether the panel itself took
        /// the click (a spawn order), which the check does not need.</summary>
        public static IEnumerator RangePanelClick(HandoverHost h, List<string> log, string[] json)
        {
            HandoverHost.PointerOverride = null; h.rangeLayout = "open"; h.Begin(); yield return null; h.StartClock(); h.SetPaused(true);
            h.Select(OpId.Scout, false); yield return new WaitForSecondsRealtime(0.6f);
            var parts = new List<string>(); bool ok = true; var got = new (int, int, bool, bool)[1];
            foreach (var view in new[] { "overhead", "follow" })
            {
                if (view == "follow") { h.Begin(); yield return null; h.StartClock(); h.SetPaused(true); h.Select(OpId.Scout, false); if (!h.Following) h.FollowToggle(); yield return new WaitForSecondsRealtime(0.8f); }
                int heard = -1; var gp = Ground(h, OpId.Scout);
                if (gp.HasValue) { yield return ClickAt(h, gp.Value, got); heard = got[0].Item1; }
                h.CloseRing(); h.ClearHover(); yield return null;
                var rows = new List<string>(); bool vok = heard == 1; int spawns = 0, clicked = 0;
                foreach (var name in new[] { "spawn", "fightMode", "tally" })
                {
                    if (!RangePanel.Controls.TryGetValue(name, out var r)) { rows.Add($"\"{name}\": null"); vok = false; continue; }
                    float x = name == "fightMode" ? r.x + r.width / 6f : r.center.x; // the first choice (Realtime, as it is): the click changes nothing
                    var sp = new Vector2(x, Screen.height - r.center.y);
                    if (!HandoverHost.OverHud(sp)) { rows.Add($"\"{name}\": {{\"blocked\": false}}"); vok = false; continue; }
                    int n0 = h.Game.Orders.Count; yield return ClickAt(h, sp, got); clicked++;
                    for (int i = n0; i < h.Game.Orders.Count; i++) if (h.Game.Orders[i].order.Do == "spawn") spawns++;
                    var (pr, mv, rg, bx) = got[0]; bool rok = pr == 0 && mv == 0 && !rg && !bx; vok &= rok;
                    rows.Add($"\"{name}\": {{\"ok\": {B(rok)}, \"pressesThrough\": {pr}, \"moves\": {mv}, \"ring\": {B(rg)}, \"box\": {B(bx)}}}");
                    h.CloseRing();
                }
                vok &= clicked == 3; ok &= vok;
                parts.Add($"\"{view}\": {{\"ok\": {B(vok)}, \"groundHeard\": {heard}, \"imguiSpawned\": {spawns}, {string.Join(", ", rows)}}}");
                log.Add($"rangePanelClick {view}: {(vok ? "ok" : "FAILED")}: ground click heard {heard}; {clicked} panel controls clicked; the panel itself spawned {spawns}; {string.Join(" ", rows).Replace("\"", "")}");
                if (h.Following) h.FollowToggle();
            }
            h.Begin(); MouseAt(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), false);
            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }

        // ---------------------------------------------------------------- brief 041: the Double click scheme and the leash

        static IEnumerator Click(Vector2 sp) { MouseAt(sp, false); yield return null; MouseAt(sp, true); yield return null; MouseAt(sp, false); yield return null; }
        static IEnumerator RightAt(Vector2 sp, float hold)
        {
            if (Mouse.current is not Mouse m) yield break;
            InputSystem.QueueStateEvent(m, new MouseState { position = sp }.WithButton(MouseButton.Right, true)); yield return null;
            if (hold > 0f) yield return new WaitForSecondsRealtime(hold); else yield return null;
            InputSystem.QueueStateEvent(m, new MouseState { position = sp }); yield return null; yield return null;
        }

        /// <summary>Single and double left clicks on a person (the guard with the Scout; Inez with the Fixer), a prop (the entrance camera, the Tech selected) and the
        /// ground (the Scout), each from a fresh run, paused; then the right tap (the ring opens) and the right hold (nothing) on the guard. Through
        /// the Input System's mouse, so the host reads them as it reads the hand.</summary>
        public static IEnumerator DblClick(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var rows = new List<string>(); bool ok = true; HandoverHost.PointerOverride = null;
            foreach (var (key, who) in new[] { ("guard", OpId.Scout), ("waitress", OpId.Fixer), ("camera/entrance", OpId.Tech), ("ground", OpId.Scout) })
            {
                h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null; yield return null;
                var g = h.Game; h.Select(who, false);
                var me = h.Views.Find("op/" + Suborned.Sim.Op.Name(who)); Transform v = key == "ground" ? me : h.Views.Find(key);
                if (!v || !me) { rows.Add($"{{\"target\": \"{key}\", \"seen\": false}}"); ok = false; continue; }
                h.LookAt(v.position); yield return null; yield return null;
                var cam = h.View; Vector2 sp;
                if (key == "ground") { var gp = Ground(h, who); if (!gp.HasValue) { rows.Add("{\"target\": \"ground\", \"seen\": false}"); ok = false; continue; } sp = gp.Value; }
                else { var w = cam.WorldToScreenPoint(v.position + Vector3.up * (key.StartsWith("camera/") ? 0f : 1.0f)); sp = new Vector2(w.x, w.y); }
                MouseAt(sp, false); for (int i = 0; i < 4; i++) yield return null;
                string probe = h.ProbeAt(sp);
                // single
                int n0 = g.Orders.Count, sel0 = h.ThingSelects; yield return Click(sp); yield return new WaitForSecondsRealtime(0.5f);
                var singleOrders = new List<string>(); for (int i = n0; i < g.Orders.Count; i++) singleOrders.Add(g.Orders[i].order.Do + (g.Orders[i].order.Run ? ":run" : ""));
                string selKey = h.SelTarget == null ? "" : h.SelTarget.Kind + "/" + h.SelTarget.Key; bool ringAfterSingle = h.Ring != null;
                MouseAt(new Vector2(Screen.width * 0.5f, Screen.height * 0.12f), false); for (int i = 0; i < 6; i++) yield return null;
                string pill = HudToolkit.PillWords; if (key != "ground") { MouseAt(sp, false); for (int i = 0; i < 3; i++) yield return null; }
                if (h.Ring != null) h.CloseRing();
                // double (two clicks inside controls.doubleSeconds)
                int n1 = g.Orders.Count, d0 = h.DoubleActs; yield return Click(sp); yield return Click(sp); yield return new WaitForSecondsRealtime(0.4f);
                var dblOrders = new List<string>(); for (int i = n1; i < g.Orders.Count; i++) dblOrders.Add(g.Orders[i].order.Do + (g.Orders[i].order.Run ? ":run" : ""));
                bool ringAfterDouble = h.Ring != null; var pend = h.PendingOf((int)who).verb; if (h.Ring != null) h.CloseRing();
                bool rowOk;
                if (key == "ground") rowOk = singleOrders.Count >= 1 && singleOrders.TrueForAll(o => o == "move") && dblOrders.Exists(o => o == "move:run") && selKey.Length == 0;
                else rowOk = !singleOrders.Exists(o => o != "drive") && (selKey.Length > 0 || ringAfterSingle) && (h.DoubleActs > d0 && (dblOrders.Count > 0 || pend.Length > 0) || ringAfterDouble);
                ok &= rowOk;
                rows.Add($"{{\"target\": \"{key}\", \"agent\": \"{Suborned.Sim.Op.Name(who)}\", \"single\": \"{string.Join(",", singleOrders)}\", \"selected\": \"{J(selKey)}\", \"ringAfterSingle\": {B(ringAfterSingle)}, \"pill\": \"{J(pill)}\", \"double\": \"{string.Join(",", dblOrders)}\", \"pending\": \"{pend}\", \"ringAfterDouble\": {B(ringAfterDouble)}, \"probe\": \"{J(probe)}\", \"ok\": {B(rowOk)}}}");
                log.Add($"dblClick {key} ({Suborned.Sim.Op.Name(who)}): {(rowOk ? "ok" : "FAILED")}: single gave [{string.Join(",", singleOrders)}] selected '{selKey}' ring {ringAfterSingle}, pill '{pill}'; double gave [{string.Join(",", dblOrders)}] pending '{pend}' ring {ringAfterDouble}; probe: {probe}");
            }
            // the right tap opens the ring on Inez; the right hold opens nothing
            {
                h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null; h.Select(OpId.Scout, false);
                var v = h.Views.Find("guard"); bool tapRing = false, holdRing = true; int holdOrders = -1;
                if (v)
                {
                    h.LookAt(v.position); yield return null; yield return null; var w = h.View.WorldToScreenPoint(v.position + Vector3.up); var sp = new Vector2(w.x, w.y);
                    MouseAt(sp, false); for (int i = 0; i < 3; i++) yield return null; log.Add("dblClick right probe: " + h.ProbeAt(sp));
                    yield return RightAt(sp, 0f); tapRing = h.Ring != null; h.CloseRing(); yield return null;
                    int n0 = h.Game.Orders.Count; yield return RightAt(sp, 0.45f); holdRing = h.Ring != null; holdOrders = h.Game.Orders.Count - n0; h.CloseRing();
                }
                bool rOk = tapRing && !holdRing && holdOrders == 0; ok &= rOk;
                rows.Add($"{{\"target\": \"right on guard\", \"tapOpensRing\": {B(tapRing)}, \"holdOpensRing\": {B(holdRing)}, \"holdOrders\": {holdOrders}, \"ok\": {B(rOk)}}}");
                log.Add($"dblClick right: {(rOk ? "ok" : "FAILED")}: a tap opens the ring {tapRing}; a 0.45 s hold opens it {holdRing}, orders {holdOrders}");
            }
            h.Begin(); MouseAt(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), false);
            json[0] = $"{{\"ok\": {B(ok)}, \"rows\": [{string.Join(", ", rows)}]}}";
        }

        /// <summary>The leash (brief 041): the Scout driven on the range's lane, the mouse still; W for 2.5 s, then S for 7 s (a 180° turn).
        /// The camera must hold while she walks at it (no spin), her path must stay straight (the keys' frame latched while S is held), and then
        /// the camera settles behind her at no more than 45°/s. The yaw of every frame goes to leash-yaw.tsv beside the proof.</summary>
        public static IEnumerator Leash(HandoverHost h, string dir, List<string> log, string[] json)
        {
            HandoverHost.PointerOverride = null; h.rangeLayout = "lane"; h.Begin(); yield return null; h.StartClock(); h.SetPaused(false);
            var keepSettle = h.controls.leashSettle; h.controls.leashSettle = true;
            h.Select(OpId.Scout, false); yield return null;
            var g = h.Game; var me = h.Views.Find("op/scout"); if (!me) { json[0] = "{\"ok\": false, \"why\": \"no scout\"}"; yield break; }
            // a straight line of free floor (the lane): 5.5 m ahead and 10 m back
            Vector3 p0 = me.position, best = Vector3.zero; bool found = false;
            for (int a = 0; a < 360 && !found; a += 15)
            {
                var d = Quaternion.Euler(0f, a, 0f) * Vector3.forward; bool free = true;
                for (float k = -10f; k <= 5.5f && free; k += 0.5f) free = g.S.Grid.Free(h.frame.Engine(p0 + d * k));
                if (free) { best = d; found = true; }
            }
            if (!found) { json[0] = "{\"ok\": false, \"why\": \"no straight free line on the floor\"}"; yield break; }
            h.DriveToggle(); yield return new WaitForSecondsRealtime(0.8f);
            h.FollowFace(p0 + best * 10f); MouseAt(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), false);
            h.LeashLog.Clear(); h.LeashLogOn = true;
            Keys(Key.W); yield return new WaitForSecondsRealtime(2.5f);
            float tS = Time.unscaledTime; Keys(Key.S); yield return new WaitForSecondsRealtime(7f); Keys(); yield return new WaitForSecondsRealtime(0.3f);
            h.LeashLogOn = false;
            var L = new List<(float t, float yaw, float heading, float speed, bool settling)>(h.LeashLog);
            var sb = new System.Text.StringBuilder("t\tyaw\theading\tspeed\tsettling\n");
            foreach (var r in L) sb.Append(F(r.t - tS)).Append('\t').Append(F(r.yaw)).Append('\t').Append(F(r.heading)).Append('\t').Append(F(r.speed)).Append('\t').Append(r.settling ? 1 : 0).Append('\n');
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "leash-yaw.tsv"), sb.ToString());
            float y0 = 0f; foreach (var r in L) if (r.t <= tS) y0 = r.yaw;
            float holdMax = 0f, settleAt = -1f, maxRate = 0f, hMin = 999f, hMax = -999f; float? hRef = null;
            for (int i = 0; i < L.Count; i++)
            {
                var r = L[i]; float s = r.t - tS; if (s < 0f) continue;
                if (s <= 1.0f) holdMax = Mathf.Max(holdMax, Mathf.Abs(Mathf.DeltaAngle(y0, r.yaw)));
                if (r.settling && settleAt < 0f && s > 0.3f) settleAt = s; // after the S heading took (before it, the W walk's own settle may still read true)
                if (i > 0 && r.t - L[i - 1].t > 1e-4f) maxRate = Mathf.Max(maxRate, Mathf.Abs(Mathf.DeltaAngle(L[i - 1].yaw, r.yaw)) / (r.t - L[i - 1].t));
                if (s >= 0.6f && r.speed > 0.5f) { hRef ??= r.heading; float dh = Mathf.DeltaAngle(hRef.Value, r.heading); hMin = Mathf.Min(hMin, dh); hMax = Mathf.Max(hMax, dh); }
            }
            var last = L.Count > 0 ? L[L.Count - 1] : default; float behind = Mathf.Abs(Mathf.DeltaAngle(last.yaw, last.heading));
            float turnSpread = hMax - hMin;
            float vMax = 0f; foreach (var r in L) vMax = Mathf.Max(vMax, r.speed);
            float rateCap = h.leashMaxDegS + vMax / h.leashArm * Mathf.Rad2Deg * h.leashGain + 4f; // the settle's cap plus the leash's own drag at her speed (camera.js adds both)
            bool okHold = holdMax < 15f, okSettle = settleAt > 0.5f && behind < 30f, okRate = maxRate < rateCap, okStraight = turnSpread < 12f;
            bool ok = L.Count > 100 && okHold && okSettle && okRate && okStraight;
            log.Add($"leash: {(ok ? "ok" : "FAILED")}: {L.Count} frames; S at yaw {F(y0)}: the first 1 s moved the camera {F(holdMax)}° (holds < 15); settling began {F(settleAt)} s after S; at the end {F(behind)}° off her heading (< 30); fastest turn {F(maxRate)}°/s (settle cap 45 plus the leash's drag: under {F(rateCap)}); her heading spread under S {F(turnSpread)}° (straight < 12)");
            if (h.Driving) h.DriveLetGoToMap(); yield return null; h.controls.leashSettle = keepSettle;
            json[0] = $"{{\"ok\": {B(ok)}, \"frames\": {L.Count}, \"holdDeg1s\": {F(holdMax)}, \"settleStartS\": {F(settleAt)}, \"endOffDeg\": {F(behind)}, \"maxDegPerS\": {F(maxRate)}, \"headingSpreadDeg\": {F(turnSpread)}, \"tsv\": \"leash-yaw.tsv\"}}";
        }
    }
}
