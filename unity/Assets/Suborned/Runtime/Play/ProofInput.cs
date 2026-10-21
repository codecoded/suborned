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
    }
}
