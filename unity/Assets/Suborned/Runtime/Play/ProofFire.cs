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
    /// <summary>Brief 024 (action-game controls and firing), the proof's checks in the built player at real time, driven through the Input System's
    /// own devices where the input matters (queued key and mouse states) and the host's scripted calls where the engine's input is the subject.
    /// Each writes one JSON object with "ok" for proof.json; game:check fails on ok false. Pictures (1920 x 1080, HudToolkit shots with their
    /// HUD layer and manifest) go in the proof folder, copied to briefs/024/build/ by the batch.
    ///  · inputMap: Glyph's done-check 1, the one table every third-person input follows (HandoverHost.TpResolve), with its key expectations.
    ///  · act: done-checks 2, 3, 6, 7, 8 at Inez: E and a left click on the prompt do what the prompt says (or open the ring), never harm blind
    ///    (20 E taps, 20 left clicks, 20 aim-and-click presses, holstered and drawn), the right tap's ring and its walk, WASD after every kind of
    ///    click, and clickActs off.
    ///  · fire: the trigger (press, hold, release, tap), the follower, a lock on a second agent, Stopped in the frame the engine stops, no fire
    ///    order surviving control, every mark read from FireStateOf; the per-frame animation log of every cell figure (the recoil per engine
    ///    shot); the commander view firing; the session saved for game:check to replay headless.
    ///  · cars: the official car shot to smoke, flame and the blast; the getaway car shot to the getaway-lost fail card.</summary>
    public static class ProofFire
    {
        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        static string B(bool b) => b ? "true" : "false";
        static void Keys(params Key[] down) { if (Keyboard.current is Keyboard kb) InputSystem.QueueStateEvent(kb, new KeyboardState(down)); }
        static void MouseAt(Vector2 screen, bool left = false, bool right = false)
        {
            if (Mouse.current is not Mouse m) return;
            var st = new MouseState { position = screen }; if (left) st = st.WithButton(MouseButton.Left, true); if (right) st = st.WithButton(MouseButton.Right, true);
            InputSystem.QueueStateEvent(m, st);
        }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static Vector3 At(HandoverHost h, string key) { var t = h.Views.Find(key); return t ? t.position : Vector3.zero; }
        static Vector2? ScreenOf(HandoverHost h, string key, float up = 1.0f) { var t = h.Views.Find(key); if (!t || !h.View) return null; var sp = h.View.WorldToScreenPoint(t.position + Vector3.up * up); return sp.z > 0f ? new Vector2(sp.x, sp.y) : null; }

        /// <summary>Walk agent k (engine move orders, the clock stepped by RunTo) to a free point `cm` short of `to` on the line from where it stands.</summary>
        static bool Approach(HandoverHost h, int k, Pt to, int cm, int sideCm = 0)
        {
            var g = h.Game; var from = g.Ops[k].Pos; var d = new Vector2(from.X - to.X, from.Y - to.Y); if (d.sqrMagnitude < 1f) d = Vector2.right; d.Normalize();
            var n = new Vector2(-d.y, d.x); var want = new Pt(to.X + Mathf.RoundToInt(d.x * cm + n.x * sideCm), to.Y + Mathf.RoundToInt(d.y * cm + n.y * sideCm));
            Pt? p = g.S.Grid.Free(want) ? want : g.S.Grid.NearestFree(want); if (!p.HasValue) return false;
            if (!h.Issue(new Order { Op = Suborned.Sim.Op.Name((OpId)k), Do = "move", Point = p.Value, Run = true })) return false;
            return true;
        }
        /// <summary>Walk agent k to a free point about `cm` from `to` with a clear line of fire to it (16 bearings, the one nearest where k stands).</summary>
        static bool ApproachInSight(HandoverHost h, int k, Pt to, int cm)
        {
            var g = h.Game; var from = g.Ops[k].Pos; Pt? best = null; long bd = long.MaxValue;
            foreach (int r in new[] { cm, cm + 200, cm - 150, cm + 400 })
                for (int i = 0; i < 16; i++)
                {
                    float a = i * Mathf.PI * 2f / 16f; var q = new Pt(to.X + Mathf.RoundToInt(Mathf.Cos(a) * r), to.Y + Mathf.RoundToInt(Mathf.Sin(a) * r));
                    if (!g.S.Grid.Free(q) || !g.S.Sight(q, to, g.R.Eye) || g.S.Grid.Route(from, q) == null) continue;
                    long d = Suborned.Sim.G.D2(q, from); if (d < bd) { bd = d; best = q; }
                }
            if (!best.HasValue) return Approach(h, k, to, cm);
            return h.Issue(new Order { Op = Suborned.Sim.Op.Name((OpId)k), Do = "move", Point = best.Value, Run = true });
        }

        static void Arrive(HandoverHost h, int cap) { for (int i = 0; i < cap && !h.Game.Over; i++) { bool any = false; for (int k = 0; k < 4; k++) any |= h.Game.Ops[k].Walk != null; if (!any) break; h.RunTo(h.Game.Tick + 1); } }

        static IEnumerator Shoot(string dir, string name, string state, List<string> log)
        {
            while (HudToolkit.ShotBusy) yield return null;
            log.Add($"{name}: " + HudToolkit.Shoot(Path.Combine(dir, name), state)); yield return null; while (HudToolkit.ShotBusy) yield return null;
        }

        // ---------------------------------------------------------------- done-check 1

        public static void InputMap(List<string> log, string[] json)
        {
            var H = typeof(HandoverHost);
            bool ok = true; var bad = new List<string>();
            void Want(HandoverHost.TpIn i, HandoverHost.TpState s, HandoverHost.TpDo d) { var got = HandoverHost.TpResolve(i, s); if (got != d) { ok = false; bad.Add($"{i}/{s}: {got}, want {d}"); } }
            foreach (HandoverHost.TpState s in System.Enum.GetValues(typeof(HandoverHost.TpState)))
            {
                Want(HandoverHost.TpIn.MiddleDrag, s, HandoverHost.TpDo.Look);
                if (s != HandoverHost.TpState.Aiming) Want(HandoverHost.TpIn.LeftAiming, s, HandoverHost.TpDo.Nothing); // no hidden hip fire
                else { Want(HandoverHost.TpIn.LeftTap, s, HandoverHost.TpDo.Fire); Want(HandoverHost.TpIn.LeftAiming, s, HandoverHost.TpDo.Fire); }
            }
            Want(HandoverHost.TpIn.ETap, HandoverHost.TpState.Free, HandoverHost.TpDo.Act); Want(HandoverHost.TpIn.EHold, HandoverHost.TpState.Free, HandoverHost.HoldRing ? HandoverHost.TpDo.Ring : HandoverHost.TpDo.Act); // brief 041: the Double click scheme opens nothing on a hold
            Want(HandoverHost.TpIn.LeftHold, HandoverHost.TpState.Free, HandoverHost.HoldRing ? HandoverHost.TpDo.Ring : HandoverHost.TpDo.Act);
            Want(HandoverHost.TpIn.RightTap, HandoverHost.TpState.Free, HandoverHost.TpDo.RingAtTarget); Want(HandoverHost.TpIn.RightHold, HandoverHost.TpState.Free, HandoverHost.TpDo.Aim);
            Want(HandoverHost.TpIn.RightHold, HandoverHost.TpState.CarryingVale, HandoverHost.TpDo.Nothing); Want(HandoverHost.TpIn.LeftDrag, HandoverHost.TpState.Walking, HandoverHost.TpDo.Orbit);
            int n = System.Enum.GetValues(typeof(HandoverHost.TpIn)).Length * System.Enum.GetValues(typeof(HandoverHost.TpState)).Length;
            log.Add("inputMap: " + HandoverHost.TpTable().Replace("\n", " | "));
            json[0] = $"{{\"ok\": {B(ok)}, \"holdRing\": {B(HandoverHost.HoldRing)}, \"pairs\": {n}, \"bad\": [{string.Join(", ", bad.Select(b => "\"" + b + "\""))}], \"table\": \"{HandoverHost.TpTable().Replace("\n", " | ")}\"}}";
        }

        // ---------------------------------------------------------------- done-checks 2, 3, 6, 7, 8

        static bool IsHarm(Order o, bool ok) => ok && (o.Do == "fire" || o.Do == "trigger" && o.On || o.Do == "takedown" || o.Do == "turn" && o.Approach == "pressure");

        public static IEnumerator Act(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var parts = new List<string>(); bool ok = true;
            h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null;
            var g = h.Game; var keepClick = h.controls.clickActs; h.controls.clickActs = true;
            var iz = g.PersonPos(g.Inez); if (!iz.HasValue) { json[0] = "{\"ok\": null, \"why\": \"no Inez\"}"; yield break; }
            Approach(h, 0, iz.Value, 300); Arrive(h, 900);
            h.Select(OpId.Scout, false); h.DriveToggle(); h.SetPaused(false);
            if (!h.Driving) { json[0] = "{\"ok\": false, \"why\": \"could not take control\"}"; yield break; }
            h.FollowFace(At(h, "waitress")); yield return new WaitForSecondsRealtime(0.8f);
            Vector2 IzScreen() => ScreenOf(h, "waitress") ?? new Vector2(Screen.width / 2f, Screen.height / 2f);
            MouseAt(IzScreen()); yield return Frames(4);
            string focus = h.DriveFocus == null ? "" : h.DriveFocus.Kind + "/" + h.DriveFocus.Key;
            parts.Add($"\"focus\": \"{focus}\""); ok &= focus == "person/waitress";
            yield return Shoot(dir, "tp-prompt", "rest", log);
            // E held 0.3 s opens the ring (the options); Esc closes it
            Keys(Key.E); yield return new WaitForSecondsRealtime(0.32f); bool ringByHold = h.Ring != null; yield return Shoot(dir, "tp-options", "active", log); Keys(); yield return Frames(3);
            Keys(Key.Escape); yield return Frames(2); Keys(); yield return Frames(2); parts.Add($"\"eHoldRing\": {B(ringByHold)}, \"escClosed\": {B(h.Ring == null)}"); ok &= ringByHold && h.Ring == null;
            if (h.MenuOpen) h.ResumeFromMenu();
            // 2 · prompt = act: E tap, then a left click on the prompt (clickActs on): the prompt's way is what is given, or the ring opens
            // what the prompt promises, by TpAct's rule: the one clear way (not harmful) is given; a lock, a hint or nothing gives nothing; else the ring
            string Expect(HandoverHost.ContextPlan pl) => pl == null ? "" : pl.Choice.Act switch { ContextRank.Act.Default => pl.Row(pl.Choice.Pick)?.I?.Order?.Do ?? "", ContextRank.Act.Lock or ContextRank.Act.Hint or ContextRank.Act.Silent => "", _ => "ring" };
            HandoverHost.ContextPlan plan = h.DriveFocus != null ? h.ContextFor(h.DriveFocus, IzScreen()) : null;
            string want = Expect(plan);
            string Did(int from) { for (int i = from; i < g.Orders.Count; i++) { var o = g.Orders[i].order; if (o.Do is "drive" or "move" or "draw") continue; return o.Do; } return h.Ring != null ? "ring" : ""; }
            int n0 = g.Orders.Count; Keys(Key.E); yield return Frames(2); Keys(); yield return Frames(3);
            string eDid = Did(n0); h.CloseRing(); parts.Add($"\"promptWay\": \"{want}\", \"eDid\": \"{eDid}\""); ok &= eDid == want;
            yield return new WaitForSecondsRealtime(0.3f);
            if (h.DriveWalking || g.Ops[0].Busy != null) { h.CancelSelectedScripted(); yield return Frames(3); }
            plan = h.DriveFocus != null ? h.ContextFor(h.DriveFocus, IzScreen()) : null;
            string want2 = Expect(plan);
            n0 = g.Orders.Count; var sp = IzScreen(); MouseAt(sp); yield return null; MouseAt(sp, left: true); yield return Frames(2); MouseAt(sp); yield return Frames(3);
            string cDid = Did(n0); h.CloseRing(); parts.Add($"\"clickWay\": \"{want2}\", \"clickDid\": \"{cDid}\""); ok &= cDid == want2;
            // 8 · clickActs off: a left tap on the prompt does nothing; the prompt shows only the E cap (HudToolkit hides the mouse)
            h.controls.clickActs = false; n0 = g.Orders.Count; MouseAt(sp); yield return null; MouseAt(sp, left: true); yield return Frames(2); MouseAt(sp); yield return Frames(3);
            string offDid = Did(n0); h.CloseRing(); parts.Add($"\"clickOffDid\": \"{offDid}\""); ok &= offDid == ""; h.controls.clickActs = true;
            if (h.DriveWalking || g.Ops[0].Busy != null) { h.CancelSelectedScripted(); yield return Frames(3); }
            // 3 · no blind harm: 20 E taps, 20 left clicks, 20 aim-and-click presses on each target in reach, holstered then drawn
            var tested = new List<string>(); int harm = 0, presses = 0;
            foreach (bool drawn in new[] { false, true })
            {
                if (drawn) { h.Issue(new Order { Op = "scout", Do = "draw" }); h.RunTo(g.Tick + 8); }
                foreach (var key in new[] { "waitress", "guard", "marker/door", "councillor", "crowd" })
                {
                    string vk = key; if (key == "crowd") { vk = ""; foreach (var (nm, x) in h.Views.All()) if (nm.StartsWith("crowd/") && x && x.gameObject.activeInHierarchy && (x.position - h.Views.DrivePos).magnitude < 12f) { vk = nm; break; } if (vk.Length == 0) continue; }
                    var vt = h.Views.Find(vk); if (!vt || !vt.gameObject.activeInHierarchy || (vt.position - h.Views.DrivePos).magnitude > 15f) continue;
                    var tsp = ScreenOf(h, vk, vk.StartsWith("marker/") ? 0.3f : 1.0f); if (!tsp.HasValue) continue;
                    tested.Add(vk + (drawn ? "+drawn" : "")); int m0 = g.Orders.Count;
                    for (int r = 0; r < 20; r++) { MouseAt(tsp.Value); yield return null; Keys(Key.E); yield return null; Keys(); yield return Frames(2); h.CloseRing(); h.SetLoudNull(); presses++; }
                    for (int r = 0; r < 20; r++) { MouseAt(tsp.Value); yield return null; MouseAt(tsp.Value, left: true); yield return null; MouseAt(tsp.Value); yield return Frames(2); h.CloseRing(); h.SetLoudNull(); presses++; }
                    for (int r = 0; r < 20; r++) { MouseAt(tsp.Value); h.AimScripted(true); yield return null; MouseAt(tsp.Value, left: true); yield return null; MouseAt(tsp.Value); yield return Frames(2); h.AimScripted(false); h.SetLoudNull(); presses++; }
                    for (int i = m0; i < g.Orders.Count; i++) if (IsHarm(g.Orders[i].order, g.Orders[i].ok)) { harm++; log.Add($"act: harm without a hold on {vk}: {g.Orders[i].order.Do} {g.Orders[i].order.Target}"); }
                    if (h.DriveWalking || g.Ops[0].Busy != null) { h.CancelSelectedScripted(); yield return Frames(2); }
                }
            }
            parts.Add($"\"harmTested\": \"{string.Join(" ", tested)}\", \"presses\": {presses}, \"harmWithoutHold\": {harm}"); ok &= harm == 0 && tested.Count > 0;
            // 7 · W moves within a frame after: a HUD (portrait) click, a ring open and close, a right tap on the ground, a left click on the prompt, Esc
            var after = new List<string>();
            IEnumerator WMoves(string what)
            {
                var a = h.Views.DrivePos; Keys(Key.W); int took = -1;
                for (int f = 1; f <= 4 && took < 0; f++) { yield return null; if (h.Views.DriveSpeed > 0.01f || (h.Views.DrivePos - a).sqrMagnitude > 1e-6f) took = f; }
                Keys(); yield return Frames(4);
                after.Add($"{what}={(took > 0 ? $"moves@{took}" : $"STUCK(driving {h.Driving} agent {h.DriveAgent} busy {g.Ops[0].Busy?.Do ?? "-"} modal {h.Modal} ring {h.Ring != null})")}"); ok &= took > 0 && took <= 2 && h.Driving;
            }
            if (HudToolkit.PortraitScreen(1) is Vector2 ps) { MouseAt(ps); yield return null; MouseAt(ps, left: true); yield return null; MouseAt(ps); yield return Frames(2); h.Select(OpId.Scout, false); yield return Frames(3); yield return WMoves("portrait"); }
            Keys(Key.E); yield return new WaitForSecondsRealtime(0.3f); Keys(); yield return Frames(2); Keys(Key.Escape); yield return Frames(2); Keys(); if (h.MenuOpen) h.ResumeFromMenu(); yield return WMoves("ring");
            if (h.View) { var gsp = (Vector2)h.View.WorldToScreenPoint(h.Views.DrivePos + h.View.transform.right * 3f); MouseAt(gsp); yield return null; MouseAt(gsp, right: true); yield return null; MouseAt(gsp); yield return Frames(2); yield return WMoves("right-tap-ground"); }
            sp = IzScreen(); MouseAt(sp); yield return null; MouseAt(sp, left: true); yield return null; MouseAt(sp); yield return Frames(2); h.CloseRing(); if (h.DriveWalking || g.Ops[0].Busy != null) h.CancelSelectedScripted(); yield return WMoves("prompt-click");
            Keys(Key.Escape); yield return Frames(2); Keys(); if (h.MenuOpen) h.ResumeFromMenu(); yield return Frames(2); yield return WMoves("esc");
            parts.Add($"\"wasdAfter\": \"{string.Join(" ", after)}\"");
            if (h.Driving) h.DriveLetGoToMap(); yield return null;
            // 6 · the right tap on a target out of reach (a fresh run: the Fixer 9 m from Inez): the ring at the target; a way that walks is
            // taken; the agent walks in without a stop; W takes the stick back and drops the walk and its act
            h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null; g = h.Game;
            Approach(h, 1, iz.Value, 900); Arrive(h, 1200);
            h.Select(OpId.Fixer, false); h.DriveToggle(); h.SetPaused(false); h.FollowFace(At(h, "waitress")); yield return new WaitForSecondsRealtime(0.8f);
            sp = IzScreen(); MouseAt(sp); yield return Frames(2); MouseAt(sp, right: true); yield return Frames(2); MouseAt(sp); yield return Frames(3);
            var rr = h.Ring; bool rRing = rr != null; int take = -1; string way = "";
            if (rRing) for (int i = 0; i < rr.Segs.Count && take < 0; i++) { var sg = rr.Segs[i]; if (!sg.More && !sg.Harmful && sg.Row?.I != null && (sg.Row.I.WalkS > 0 || sg.Row.I.Walk.Length > 0) && sg.Row.Available) { take = i; way = sg.Row.I.Order.Do + ":" + sg.Row.I.Order.Approach; } }
            if (rRing) yield return Shoot(dir, "tp-right-ring", "active", log);
            if (take >= 0) h.RingTakeScripted(take); yield return Frames(3);
            bool walking = h.DriveWalking && g.Ops[1].Walk != null; float stillS = 0f;
            for (int f = 0; f < 40 && walking; f++) { var a = h.Views.Find("op/fixer").position; yield return null; if ((h.Views.Find("op/fixer").position - a).sqrMagnitude < 1e-8f) stillS += Time.unscaledDeltaTime; }
            Keys(Key.W); yield return new WaitForSecondsRealtime(0.3f); Keys(); yield return Frames(2); // the stick goes back at once; the engine drops its walk at the next tick's drive sample
            bool back = h.Driving && !h.DriveWalking && g.Ops[1].Walk == null && h.PendingOf(1).verb.Length == 0;
            parts.Add($"\"rightTapRing\": {B(rRing)}, \"walkWay\": \"{way}\", \"walksIn\": {B(walking)}, \"stillS\": {F(stillS)}, \"wCancels\": {B(back)}"); ok &= rRing && take >= 0 && walking && stillS <= 0.12f && back; // no pause: at most one tick's hand-over
            h.controls.clickActs = keepClick; MouseAt(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            log.Add($"act: {(ok ? "ok" : "FAILED")}: {string.Join(", ", parts).Replace("\"", "")}");
            if (h.Driving) h.DriveLetGoToMap(); yield return null; h.Begin();
            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }

        // ---------------------------------------------------------------- firing: the trigger, follow, lock, Stopped, the animation log

        public static IEnumerator Fire(HandoverHost h, string dir, List<string> log, string[] json, List<string> rows, string[] session)
        {
            var parts = new List<string>(); bool ok = true;
            h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null;
            var g = h.Game; const int E = (int)OpId.Enforcer, S = (int)OpId.Scout, X = (int)OpId.Fixer; const string T = "car:official", TV = "car/official";
            // a target that stands and takes many shots: Vale's car at the pickup (Vale is in the Office, so no fail); the Enforcer outside its
            // blast ring, the Scout 3 m beside him (inside the 8 m follow ring), the Fixer 5 m off
            var cp = g.CarAt(T); if (!cp.HasValue || g.Ops[E].Gun.W == null) { json[0] = "{\"ok\": null, \"why\": \"no official car or no armed enforcer\"}"; yield break; }
            int stand = Mathf.Min(1300, g.R.CarBlastCm + 450);
            ApproachInSight(h, E, cp.Value, stand); Arrive(h, 1500);
            var ep = g.Ops[E].Pos; Approach(h, S, ep, 300, 200); Arrive(h, 1500); if (!g.S.Sight(g.Ops[S].Pos, cp.Value, g.R.Eye)) { Approach(h, S, ep, 250, -200); Arrive(h, 900); }
            ApproachInSight(h, X, cp.Value, stand + 250); Arrive(h, 1500); // the Fixer (the lock) with its own line to the car
            foreach (var k in new[] { E, S, X }) h.Issue(new Order { Op = Suborned.Sim.Op.Name((OpId)k), Do = "draw" }); h.RunTo(g.Tick + 10);
            parts.Add($"\"enforcerLine\": {B(g.S.Sight(g.Ops[E].Pos, cp.Value, g.R.Eye))}, \"scoutLine\": {B(g.S.Sight(g.Ops[S].Pos, cp.Value, g.R.Eye))}");
            parts.Add($"\"target\": \"{T}\", \"enforcerToCarM\": {F(Mathf.Sqrt((float)Suborned.Sim.G.D2(g.Ops[E].Pos, cp.Value)) / 100f)}, \"scoutToEnforcerM\": {F(Mathf.Sqrt((float)Suborned.Sim.G.D2(g.Ops[S].Pos, g.Ops[E].Pos)) / 100f)}, \"blastM\": {F(g.R.CarBlastCm / 100f)}");
            // the commander view first, while the car is whole: the Fixer selected, a lasting fire order (the map's right click), overhead at play zoom
            h.Select(OpId.Fixer, false); h.FireScripted(T); h.LookAt((At(h, "op/fixer") + At(h, TV)) * 0.5f); h.SetPaused(false);
            var cd = new List<float>(); float c0 = Time.realtimeSinceStartup; bool cShot = false, cFiring = false;
            while (Time.realtimeSinceStartup - c0 < 3f) { yield return null; cd.Add(Time.unscaledDeltaTime * 1000f); cFiring |= h.FireNow[X].Firing; if (!cShot && h.FireNow[X].Firing && Time.realtimeSinceStartup - c0 > 1.2f && h.ShotFx.Count > 0 && Time.unscaledTime - h.ShotFx[h.ShotFx.Count - 1].born < 0.06f) { cShot = true; yield return Shoot(dir, "commander-firing", "rest", log); } }
            if (!cShot) yield return Shoot(dir, "commander-firing", "rest", log);
            if (cd.Count > 10) { cd.Sort(); rows.Add($"{{\"spot\": \"fire-commander\", \"frames\": {cd.Count}, \"meanMs\": {F(cd.Average())}, \"fps\": {F(1000f / cd.Average())}, \"p99Ms\": {F(cd[(int)(cd.Count * 0.99f)])}, \"gpuMs\": -1}}"); }
            parts.Add($"\"commanderFixerFiring\": {B(cFiring)}"); ok &= cFiring;
            h.Issue(new Order { Op = "fixer", Do = "cease" }); h.SetPaused(true);
            // no fire order survives taking control
            h.Issue(new Order { Op = "enforcer", Do = "fire", Target = T }); bool hadOrder = g.Ops[E].Gun.Order.Length > 0;
            h.Select(OpId.Enforcer, false); h.DriveToggle(); bool dropped = g.Ops[E].Gun.Order.Length == 0;
            parts.Add($"\"orderBeforeControl\": {B(hadOrder)}, \"orderDroppedOnControl\": {B(dropped)}"); ok &= h.Driving && dropped;
            h.FollowFace(At(h, TV)); h.SetPaused(false);
            h.Views.AnimLog = new List<string>(); int shots0 = g.Log.Count;
            yield return new WaitForSecondsRealtime(0.8f);
            void Point() { HandoverHost.PointerWorld = At(h, TV) + Vector3.up * 0.8f; } // the reticle held on the car whatever the window's size
            Point(); h.AimScripted(true); yield return new WaitForSecondsRealtime(0.4f); Point();
            yield return Shoot(dir, "tp-aim", "rest", log);
            // press and hold: the tracer, the mark and the line read from the engine's fire state in the frame it starts
            int mism = 0, frames = 0, firingFrames = 0, followFrames = 0; float firstFire = -1f, pressAt = Time.realtimeSinceStartup;
            h.TriggerScripted(T, true);
            bool pressTaken = h.TriggerDown; parts.Add($"\"triggerPressTaken\": {B(pressTaken)}"); ok &= pressTaken;
            var fd = new List<float>(); bool shotTaken = false, followerShot = false;
            while (Time.realtimeSinceStartup - pressAt < 4f && !g.Over)
            {
                yield return null; frames++; fd.Add(Time.unscaledDeltaTime * 1000f); Point();
                var eng = g.FireStateOf(OpId.Enforcer); var host = h.FireNow[E];
                if (eng.Firing != host.Firing || eng.Mode != host.Mode || eng.Target != host.Target) mism++; // the host's marks read the engine's state (same frame)
                if (host.Firing) { firingFrames++; if (firstFire < 0f) firstFire = Time.realtimeSinceStartup - pressAt; }
                if (h.Followers.Count > 0) followFrames++;
                if (!shotTaken && host.Firing && h.ShotFx.Count > 0 && h.ShotFx[h.ShotFx.Count - 1].side == "cell" && Time.unscaledTime - h.ShotFx[h.ShotFx.Count - 1].born < 0.05f) { shotTaken = true; yield return Shoot(dir, "tp-firing", "rest", log); }
                else if (!followerShot && shotTaken && h.FireNow[S].Mode == "follow" && h.FireNow[S].Firing) { followerShot = true; yield return Shoot(dir, "tp-follower", "rest", log); }
            }
            if (fd.Count > 10) { fd.Sort(); rows.Add($"{{\"spot\": \"fire-third\", \"frames\": {fd.Count}, \"meanMs\": {F(fd.Average())}, \"fps\": {F(1000f / fd.Average())}, \"p99Ms\": {F(fd[(int)(fd.Count * 0.99f)])}, \"gpuMs\": -1}}"); }
            parts.Add($"\"framesHeld\": {frames}, \"firingFrames\": {firingFrames}, \"firstFiringS\": {F(firstFire)}, \"stateMismatchFrames\": {mism}, \"followerFrames\": {followFrames}, \"followers\": \"{string.Join(",", h.Followers)}\"");
            ok &= mism == 0 && firingFrames > frames / 2 && followFrames > 0;
            // done-check 4 and 5: at a walk the trigger plants the agent; a runner keeps moving and firing at worse odds; the camera holds its distance and pitch
            {
                var camT = h.View.transform; float dist0 = (camT.position - h.Views.DrivePos).magnitude, pitch0 = camT.eulerAngles.x;
                var a0 = h.Views.DrivePos; h.DriveInputOverride = new Vector2(1f, 0f); h.DriveRunOverride = false; yield return new WaitForSecondsRealtime(0.6f);
                float walkMoved = (h.Views.DrivePos - a0).magnitude; int accStand = g.PreviewFire(OpId.Enforcer, T).Acc;
                var a1 = h.Views.DrivePos; h.DriveRunOverride = true; yield return new WaitForSecondsRealtime(0.7f);
                float runMoved = (h.Views.DrivePos - a1).magnitude; bool runFiring = h.FireNow[E].Mode == "trigger"; int accRun = g.PreviewFire(OpId.Enforcer, T).Acc;
                h.DriveInputOverride = null; h.DriveRunOverride = false; yield return new WaitForSecondsRealtime(0.8f); // the chase settles behind the new heading
                float dist1 = (camT.position - h.Views.DrivePos).magnitude, pitch1 = camT.eulerAngles.x;
                bool camOk = Mathf.Abs(dist1 - dist0) <= 0.01f * dist0 + 0.05f && Mathf.Abs(Mathf.DeltaAngle(pitch1, pitch0)) <= 0.01f * Mathf.Max(1f, pitch0) + 0.05f;
                parts.Add($"\"walkWhileFiringM\": {F(walkMoved)}, \"runWhileFiringM\": {F(runMoved)}, \"runKeepsTrigger\": {B(runFiring)}, \"accStand\": {accStand}, \"accRun\": {accRun}, \"camDist\": [{F(dist0)}, {F(dist1)}], \"camPitch\": [{F(pitch0)}, {F(pitch1)}], \"camHolds\": {B(camOk)}");
                ok &= walkMoved < 0.1f && runMoved > 0.5f && runFiring && camOk;
            }
            // release: the tracer cuts and the line reads Stopped in that frame
            h.TriggerScripted(T, false); yield return null;
            var (head, _, stopped, _) = h.FireLine(E); bool cut = !h.FireNow[E].Firing;
            parts.Add($"\"releaseCut\": {B(cut)}, \"releaseLine\": \"{head}\""); ok &= cut && stopped;
            yield return new WaitForSecondsRealtime(0.15f); yield return Shoot(dir, "tp-stopped", "rest", log);
            // a tap: one press, released the next frame: one shot when the cadence allows (a buffer of one)
            yield return new WaitForSecondsRealtime(2.4f);
            int tapLog0 = g.Log.Count; bool up = !(g.CarOf(T) is { Blown: true });
            if (up) { h.TriggerScripted(T, true); yield return null; h.TriggerScripted(T, false); yield return new WaitForSecondsRealtime(1.4f); }
            int tapShots = 0; for (int i = tapLog0; i < g.Log.Count; i++) if (g.Log[i].Code.StartsWith("shot.") && g.Log[i].Text.StartsWith("enforcer fires on")) tapShots++;
            parts.Add($"\"tapShots\": {tapShots}"); ok &= !up || tapShots == 1;
            // a lock on a second agent (the commander's lasting order on the Fixer) while the driven agent fires
            h.Issue(new Order { Op = "fixer", Do = "fire", Target = T }); h.RunTo(g.Tick + 1);
            string fixLock = g.LockOf(OpId.Fixer); yield return Frames(2);
            parts.Add($"\"secondLock\": \"{fixLock}\", \"secondMode\": \"{h.FireNow[X].Mode}\""); ok &= fixLock == T && h.FireNow[X].Mode == "lock";
            h.TriggerScripted(T, true); yield return new WaitForSecondsRealtime(1.2f); Point();
            yield return Shoot(dir, "tp-lock", "rest", log);
            yield return new WaitForSecondsRealtime(0.8f);
            h.TriggerScripted(T, false); h.AimScripted(false); HandoverHost.PointerOverride = null; HandoverHost.PointerWorld = null;
            yield return new WaitForSecondsRealtime(1.4f); // the gun lowers
            h.DriveLetGoToMap(); yield return null;
            h.SetPaused(true);
            // the animation log against the engine's shots: every shot by a cell agent restarts its recoil (shoot from its start), on its own rig
            var alog = h.Views.AnimLog; h.Views.AnimLog = null;
            var engineShots = new Dictionary<string, int>(); for (int i = shots0; i < g.Log.Count; i++) { var l = g.Log[i]; if (!l.Code.StartsWith("shot.")) continue; foreach (var a in new[] { "scout", "fixer", "tech", "enforcer" }) if (l.Text.StartsWith(a + " fires on")) engineShots[a] = (engineShots.TryGetValue(a, out var c) ? c : 0) + 1; }
            var recoils = new Dictionary<string, int>(); var restarts = new Dictionary<string, int>(); var clipsSeen = new Dictionary<string, HashSet<string>>(); var rigs = new Dictionary<string, string>(); var lowered = new Dictionary<string, bool>(); var moving = new Dictionary<string, int>();
            for (int i = 0; i < alog.Count; i++)
            {
                var c = alog[i].Split('\t'); string who = c[2][3..]; rigs[who] = c[9] == "1" ? "humanoid" : "generic";
                if (!clipsSeen.TryGetValue(who, out var cs)) clipsSeen[who] = cs = new HashSet<string>(); cs.Add(c[3] + ":" + c[4]);
                if (c[10] == "1") { recoils[who] = (recoils.TryGetValue(who, out var r) ? r : 0) + 1; if (c[3] == "shoot") restarts[who] = (restarts.TryGetValue(who, out var q) ? q : 0) + 1; else if (float.TryParse(c[8], NumberStyles.Float, CultureInfo.InvariantCulture, out var gs) && gs > 0.15f) moving[who] = (moving.TryGetValue(who, out var mv) ? mv : 0) + 1; }
                if (c[3] is "idle" or "idle_alt") lowered[who] = true;
            }
            var arow = new List<string>(); bool animOk = true; int fired = 0;
            foreach (var a in new[] { "scout", "fixer", "tech", "enforcer" })
            {
                int es = engineShots.TryGetValue(a, out var x) ? x : 0, rc = recoils.TryGetValue(a, out var y) ? y : 0, rs = restarts.TryGetValue(a, out var z) ? z : 0; if (es > 0) fired++;
                bool shootClip = clipsSeen.TryGetValue(a, out var cs2) && cs2.Any(v => v.StartsWith("shoot:") && v.Length > 6);
                int mvN = moving.TryGetValue(a, out var mm) ? mm : 0;
                bool aok = rc == es && (es == 0 || rs + mvN == es && rs > 0 && shootClip); // every standing shot restarts the recoil; a shot on the move keeps the gait
                animOk &= aok; arow.Add($"{{\"agent\": \"{a}\", \"rig\": \"{(rigs.TryGetValue(a, out var rg) ? rg : "?")}\", \"engineShots\": {es}, \"recoils\": {rc}, \"shootRestarts\": {rs}, \"onTheMove\": {(moving.TryGetValue(a, out var om) ? om : 0)}, \"shootClip\": {B(shootClip)}, \"clips\": \"{(clipsSeen.TryGetValue(a, out var cs3) ? string.Join(" ", cs3.Where(v => v.StartsWith("shoot") || v.StartsWith("aim") || v.StartsWith("draw"))) : "")}\", \"lowered\": {B(lowered.ContainsKey(a))}, \"ok\": {B(aok)}}}");
            }
            File.WriteAllText(Path.Combine(dir, "animlog.tsv"), HostViews.AnimLogHeader + "\n" + string.Join("\n", alog) + "\n");
            File.WriteAllText(Path.Combine(dir, "fire-log.tsv"), "tick\tframe\tkind\tagent\t...\n" + string.Join("\n", h.FireLog) + "\n");
            parts.Add($"\"anim\": [{string.Join(", ", arow)}], \"animOk\": {B(animOk)}, \"agentsFired\": {fired}, \"animRows\": {alog.Count}"); ok &= animOk && fired >= 3;
            log.Add($"fire: {(ok ? "ok" : "FAILED")}: {string.Join(", ", parts).Replace("\"", "")}");
            session[0] = h.SessionPath; h.Begin(); // the restart closes the run (saved) for game:check's headless replay
            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }

        // ---------------------------------------------------------------- cars

        public static IEnumerator Cars(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var parts = new List<string>(); bool ok = true;
            h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null;
            var g = h.Game; const int E = (int)OpId.Enforcer;
            var cp = g.CarAt("car:official"); if (!cp.HasValue) { json[0] = "{\"ok\": null, \"why\": \"no official car\"}"; yield break; }
            int stand = Mathf.Min(1300, g.R.CarBlastCm + 400);
            ApproachInSight(h, E, cp.Value, stand); ApproachInSight(h, 0, cp.Value, stand + 150); ApproachInSight(h, 1, cp.Value, stand + 300); Arrive(h, 1500);
            foreach (var a in new[] { "scout", "fixer", "enforcer" }) h.Issue(new Order { Op = a, Do = "draw" }); h.RunTo(g.Tick + 10);
            // the commander's lasting orders (each agent locked on the car), seen overhead at play zoom
            foreach (var a in new[] { "scout", "fixer", "enforcer" }) h.Issue(new Order { Op = a, Do = "fire", Target = "car:official" });
            h.Select(OpId.Enforcer, false); h.LookAt((At(h, "car/official") + At(h, "op/enforcer")) * 0.5f); h.SetPaused(false);
            bool smoke = false, burn = false, blown = false; float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 60f && !g.Over)
            {
                yield return null; var c = g.CarOf("car:official"); if (c == null) continue;
                if (!smoke && c.DisabledAt >= 0 && c.BurningAt < 0) { smoke = true; yield return new WaitForSecondsRealtime(0.8f); yield return Shoot(dir, "car-smoke", "rest", log); }
                if (!burn && c.BurningAt >= 0 && !c.Blown) { burn = true; foreach (var a in new[] { "scout", "fixer", "enforcer" }) h.Issue(new Order { Op = a, Do = "cease" }); yield return new WaitForSecondsRealtime(0.7f); yield return Shoot(dir, "car-burning", "rest", log); }
                if (c.Blown) { blown = true; yield return new WaitForSecondsRealtime(0.22f); yield return Shoot(dir, "car-blast", "rest", log); break; }
            }
            parts.Add($"\"smoke\": {B(smoke)}, \"burning\": {B(burn)}, \"blown\": {B(blown)}, \"over\": {B(g.Over)}, \"result\": \"{g.FailCode}\", \"blastM\": {F(g.R.CarBlastCm / 100f)}"); ok &= smoke && burn && blown;
            // the getaway car wrecked: Fable's getaway-lost fail card
            h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(false); yield return null; g = h.Game;
            h.Select(OpId.Enforcer, false); h.FireScripted("car:getaway");
            foreach (var a in new[] { "scout", "fixer", "tech" }) h.Issue(new Order { Op = a, Do = "fire", Target = "car:getaway" });
            t0 = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t0 < 45f && h.Ending.Length == 0) yield return null;
            t0 = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t0 < 12f && h.Modal != ModalGate.Modal.EndCard) yield return null;
            yield return new WaitForSecondsRealtime(0.6f);
            parts.Add($"\"getawayFail\": \"{g.FailCode}\", \"endCard\": {B(h.Modal == ModalGate.Modal.EndCard)}"); ok &= g.FailCode == "getaway-lost";
            yield return Shoot(dir, "fail-getaway-lost", "active", log);
            log.Add($"cars: {(ok ? "ok" : "FAILED")}: {string.Join(", ", parts).Replace("\"", "")}");
            h.Begin();
            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }
    }
}
