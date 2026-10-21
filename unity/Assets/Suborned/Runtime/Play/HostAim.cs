using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Brief 024, "Revision: action-game controls" and "Firing" (Glyph; Sky 2026-10-21: "find E for actions kinda odd, vs mouse buttons
    /// but if it's the norm wire it in"; "you have to keep shooting yourself, complete control"). Third person only; the commander view keeps its
    /// lasting fire orders (plus controls.commanderHoldFire, off).
    ///  · The prompt is the focus's chip (HostDrive's focus: the thing under the cursor, else the nearest in front within 8 m): what E does.
    ///  · E tap (released under 0.22 s) acts: the prompted way when ContextRank's three checks pass and it is not harmful, else the ring opens.
    ///    E held 0.22 s opens the ring; release over a wedge takes it; a harmful wedge (Kill, Fire on a non-combatant) fills over 0.5 s under the
    ///    pointer while E (or the button) stays down and only then takes.
    ///  · A left click on the prompted target is E (controls.clickActs, on): tap acts, hold opens the ring. Elsewhere a left click does what it did.
    ///  · The right button held 0.22 s aims (draws first if holstered; controls.aimMode Hold or Toggle); the reticle is the cursor; the agent
    ///    faces it. While aiming the left button is the trigger (Keel's logged trigger input, engine 0.17): a hostile of a fight fires on press;
    ///    anything else (and any shot that would fail the mission) takes the 0.5 s hold first. Never on Vale. Not aiming, the left button never fires.
    ///  · A right tap opens the ring at the target, as on the map; a way out of reach walks in and acts without stopping. On the ground it stops the walk.
    ///  · While the trigger is down at a walk the agent plants; running it keeps moving (the engine reads the drive sample's run flag).
    /// What each input does in each state is one table (TpResolve), which the host follows and the proof prints (done-check 1).</summary>
    public sealed partial class HandoverHost
    {
        // ---------------------------------------------------------------- one meaning per input per state (done-check 1)

        public enum TpIn { ETap, EHold, LeftTap, LeftHold, LeftDrag, LeftAiming, RightTap, RightHold, MiddleDrag }
        public enum TpState { Free, Aiming, RingOpen, Walking, Running, CarryingVale }
        public enum TpDo { Act, Ring, TakeWedge, TakeLit, Orbit, Fire, Aim, LowerAim, RingAtTarget, CloseRing, Look, Nothing }

        /// <summary>The third-person input map: exactly one result for each input in each state. Left tap and hold are on the prompted target
        /// (elsewhere a left click selects an agent or does nothing, as on the map).</summary>
        public static TpDo TpResolve(TpIn i, TpState s)
        {
            switch (i)
            {
                case TpIn.ETap: return s == TpState.RingOpen ? TpDo.TakeLit : TpDo.Act;
                case TpIn.EHold: return s == TpState.RingOpen ? TpDo.TakeWedge : TpDo.Ring;
                case TpIn.LeftTap: return s == TpState.Aiming ? TpDo.Fire : s == TpState.RingOpen ? TpDo.TakeWedge : TpDo.Act;
                case TpIn.LeftHold: return s == TpState.Aiming ? TpDo.Fire : s == TpState.RingOpen ? TpDo.TakeWedge : TpDo.Ring;
                case TpIn.LeftDrag: return s == TpState.Aiming ? TpDo.Fire : TpDo.Orbit;
                case TpIn.LeftAiming: return s == TpState.Aiming ? TpDo.Fire : TpDo.Nothing; // no hidden hip fire
                case TpIn.RightTap: return s == TpState.Aiming ? TpDo.LowerAim : s == TpState.RingOpen ? TpDo.CloseRing : TpDo.RingAtTarget;
                case TpIn.RightHold: return s is TpState.RingOpen or TpState.CarryingVale ? TpDo.Nothing : TpDo.Aim; // walking Vale, the Enforcer doesn't fire
                case TpIn.MiddleDrag: return TpDo.Look;
            }
            return TpDo.Nothing;
        }

        /// <summary>The table as text (the proof's inputMap): "ETap: Free=Act Aiming=Act …".</summary>
        public static string TpTable()
        {
            var sb = new System.Text.StringBuilder();
            foreach (TpIn i in Enum.GetValues(typeof(TpIn)))
            {
                sb.Append(i).Append(':');
                foreach (TpState s in Enum.GetValues(typeof(TpState))) sb.Append(' ').Append(s).Append('=').Append(TpResolve(i, s));
                sb.Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>The driven agent's state for the table.</summary>
        public TpState TpNow
        {
            get
            {
                if (Ring != null) return TpState.RingOpen;
                if (Aiming) return TpState.Aiming;
                if (Driving && dAgent == (int)OpId.Enforcer && Game != null && Game.Vale == ValeAt.WithEnforcer) return TpState.CarryingVale;
                if (DriveWalking) return TpState.Walking;
                if (Driving && dMoving && (dRunLatch || controls.Held(Keyboard.current, PlayAction.AddToGroup))) return TpState.Running;
                return TpState.Free;
            }
        }

        // ---------------------------------------------------------------- aim and the trigger

        /// <summary>The right button aims (third person): the reticle at the cursor, the agent facing it.</summary>
        public bool Aiming { get; private set; }
        /// <summary>The reticle (screen px, origin bottom left) and what it is on, with the engine id a trigger there would fire at.</summary>
        public Vector2 AimScreen { get; private set; }
        public Target AimAt { get; private set; }
        public string AimId { get; private set; } = "";
        bool aimForce;
        /// <summary>The trigger is down (a press taken by the engine, not yet released) and on what.</summary>
        public bool TriggerDown { get; private set; }
        public string TriggerTarget { get; private set; } = "";
        /// <summary>The 0.5 s hold before a press on anything not hostile (or a shot that fails the mission): when it began (-1 none), on what.</summary>
        float aimHoldStart = -1f; string aimHoldId = ""; bool aimHoldForce; LoudCard aimHoldCard;
        public float AimHoldProgress => aimHoldStart < 0f || Game == null ? 0f : Mathf.Clamp01((Time.unscaledTime - aimHoldStart) / HoldSeconds);
        public bool AimHolding => aimHoldStart >= 0f;
        /// <summary>The reticle's chip (PreviewFire for the driven agent on the reticle's target): its hit chance and seconds to down, the gait it reads.</summary>
        public FirePreview AimPreview { get; private set; }
        public int AimDownS { get; private set; } = -1;

        /// <summary>The fire log (brief 024 host wiring, Keel 0.18): every trigger input (on or off, target, tick, taken), each change of an
        /// agent's fire state (mode, target, firing, hollow, shot, next shot, cadence, lead), followers and locks, the follow setting. The proof
        /// writes it beside the session (fire-log.tsv).</summary>
        public readonly List<string> FireLog = new();
        void FireLogAdd(string row) { FireLog.Add($"{(Game == null ? -1 : Game.Tick)}\t{Time.frameCount}\t{row}"); if (FireLog.Count > 6000) FireLog.RemoveRange(0, 1000); }

        float rTapAt = -1f; Vector2 rTapFrom; Target rTapTarget; bool rTapPending;
        float lTapAt = -1f; Vector2 lTapFrom; Target lTapTarget; bool lTapPending, lRing;

        /// <summary>Per frame while driven, before the map's pointer code: aim, the trigger, the right tap and the left click on the prompt.
        /// True when it took the pointer this frame.</summary>
        bool TpPointer(UnityEngine.InputSystem.Mouse mouse, Vector2 p, bool onHud)
        {
            if (!Driving || Game == null || !ModalGate.AllowsClicks(Modal)) { if (Aiming) AimEnd(); return false; }
            float now = Time.unscaledTime;
            if (Ring != null) { rTapPending = false; if (!(lRing && lTapPending)) return false; } // the ring has the pointer (RightFrame); a left hold that opened it keeps the release
            // ---- the right button: tap = the ring at the target (or stop the walk on the ground), hold = aim
            if (mouse.rightButton.wasPressedThisFrame && !onHud)
            {
                if (Aiming && controls.aimMode == AimMode.Toggle) { rTapPending = true; rTapAt = now; rTapFrom = p; rTapTarget = null; }
                else if (!Aiming) { rTapPending = true; rTapAt = now; rTapFrom = p; rTapTarget = TargetAt(p); RightClickUsed = true; }
            }
            if (rTapPending)
            {
                if (!mouse.rightButton.isPressed)
                {
                    rTapPending = false;
                    if (now - rTapAt < RingHoldS && (p - rTapFrom).magnitude < RingMovePx * KitScale)
                    {
                        var d = TpResolve(TpIn.RightTap, TpNow);
                        if (d == TpDo.LowerAim) AimEnd();
                        else if (d == TpDo.RingAtTarget) { var t = rTapTarget; if (t == null || t.Kind == "op") ActContext(t, rTapFrom); else RightRing(t, rTapFrom); }
                    }
                    return true;
                }
                if (now - rTapAt >= RingHoldS || (p - rTapFrom).magnitude >= RingMovePx * KitScale)
                {
                    rTapPending = false;
                    if (!Aiming && TpResolve(TpIn.RightHold, TpNow) == TpDo.Aim) AimBegin(p);
                    else if (!Aiming && Game.Vale == ValeAt.WithEnforcer && dAgent == (int)OpId.Enforcer) PopKey("fire-carrying-vale", null, views.DrivePos, Feedback.Refused);
                }
                if (!Aiming) return true;
            }
            if (Aiming && controls.aimMode == AimMode.Hold && !mouse.rightButton.isPressed && !aimByScript) AimEnd();
            if (Aiming) { AimFrame(mouse, p, onHud); return true; }
            // ---- the left button on the prompted target (clickActs): tap acts as E, hold 0.22 s opens the ring
            if (mouse.leftButton.wasPressedThisFrame && !onHud && controls.clickActs && DriveFocus is Target pf)
            {
                var under = TargetAt(p);
                if (under != null && SameTarget(under, pf)) { lTapPending = true; lRing = false; lTapAt = now; lTapFrom = p; lTapTarget = pf; return true; }
            }
            if (lTapPending)
            {
                if ((p - lTapFrom).magnitude >= RingMovePx * KitScale && !lRing) { lTapPending = false; dragFrom = lTapFrom; dragging = true; orbiting = false; return false; } // a drag: the orbit takes it
                if (!mouse.leftButton.isPressed)
                {
                    lTapPending = false;
                    if (lRing) { var rs = Ring; if (rs == null) return true; int s = (p - rs.PointerAtOpen).magnitude > RingDeadPx * KitScale ? RingSegAt(p) : -1; if (s >= 0) RingTakeTry(s, "release"); else { rs.Sticky = true; rs.Always = true; } return true; }
                    if (TpResolve(TpIn.LeftTap, TpNow) == TpDo.Act) TpAct(lTapTarget, "click");
                    return true;
                }
                if (!lRing && now - lTapAt >= RingHoldS && TpResolve(TpIn.LeftHold, TpNow) == TpDo.Ring) { lRing = true; TpRing(lTapTarget, false); if (Ring != null) Ring.Sticky = false; }
                return true;
            }
            return false;
        }

        /// <summary>The right hold: aim. Draws first if holstered (the draw is an order: the going-loud card if a watcher would see it, held by the
        /// right button as any right press).</summary>
        void AimBegin(Vector2 p)
        {
            if (!Driving || dAgent < 0) return;
            if (!Armed(dAgent)) { PopKey("unarmed", null, views.DrivePos, Feedback.Info, "Unarmed."); return; }
            Aiming = true; AimScreen = p; aimHoldStart = -1f; CloseRing();
            if (!Drawn(dAgent)) Fight(new List<Order> { new Order { Op = Op.Name((OpId)dAgent), Do = "draw" } }, "right", p, views.DrivePos, "draw");
            FireLogAdd($"aim\t{Op.Name((OpId)dAgent)}\ton");
        }

        void AimEnd()
        {
            if (TriggerDown) TriggerRelease();
            if (Aiming && dAgent >= 0) FireLogAdd($"aim\t{Op.Name((OpId)dAgent)}\toff");
            Aiming = false; aimHoldStart = -1f; AimAt = null; AimId = ""; AimPreview = null; AimDownS = -1; aimByScript = trigByScript = false;
        }

        /// <summary>While aiming: the reticle's target, the agent facing it, and the left button as the trigger.</summary>
        void AimFrame(UnityEngine.InputSystem.Mouse mouse, Vector2 p, bool onHud)
        {
            AimScreen = p; AimAt = AimTargetAt(p, out var id, out aimForce); AimId = id;
            AimPreview = null; AimDownS = -1;
            if (id.Length > 0 && (Game.Vale != ValeAt.WithEnforcer || dAgent != (int)OpId.Enforcer))
            {   // priced once a tick (PreviewFire searches a walk-in when out of reach)
                var pk = $"{id}|{Game.Tick}|{aimForce}|{dAgent}";
                if (pk != aimPreviewKey) { aimPreviewKey = pk; FirePreview fp = null; aimPreviewKeep = Guard("aim", () => fp = Game.PreviewFire((OpId)dAgent, id, aimForce)) ? fp : null; }
                AimPreview = aimPreviewKeep; AimDownS = PreviewDownS(aimPreviewKeep, dAgent, id);
            }
            // the agent faces the reticle (view and drive facing; the chase camera follows the travel heading, never the aim)
            Vector3? at = AimAt != null ? (id.Length > 0 ? DrawnAt(id) : null) ?? TargetWorld(AimAt) : null;
            if (!at.HasValue && cam && frame.Ground(cam.ScreenPointToRay(p), out var gw)) at = gw;
            if (at.HasValue) { var d = at.Value - views.DrivePos; d.y = 0f; if (d.sqrMagnitude > 0.04f) { AimFace = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; AimFaceOn = true; } }
            if (trigByScript) return; // the proof's scripted trigger: the left button is not read
            if (mouse.leftButton.wasPressedThisFrame && !onHud && TpResolve(TpIn.LeftAiming, TpNow) == TpDo.Fire) FirePress(p);
            if (mouse.leftButton.isPressed)
            {
                if (aimHoldStart >= 0f)
                {
                    if (aimHoldId != id) { aimHoldStart = -1f; Status = "Hold to fire: moved off"; } // moved off the target: the hold lets go
                    else if (AimHoldProgress >= 1f) { aimHoldStart = -1f; TriggerPress(aimHoldId, aimHoldForce, aimHoldCard, p); }
                }
                else if (TriggerDown && id.Length > 0 && id != TriggerTarget && AimAt != null && Combatant(AimAt) && Game.GoingLoud(FireOrder(id, false)) is { Hold: false }) TriggerPress(id, false, null, p); // swept onto another hostile: the trigger follows
            }
            else
            {
                if (aimHoldStart >= 0f) { aimHoldStart = -1f; Status = "Hold to fire: let go"; }
                if (TriggerDown) TriggerRelease();
            }
        }
        /// <summary>The driven agent's facing while aiming (Unity yaw, degrees); the figure and the drive sample read it.</summary>
        string aimPreviewKey = ""; FirePreview aimPreviewKeep;
        public float AimFace { get; private set; }
        public bool AimFaceOn { get; private set; }

        Order FireOrder(string id, bool force) => new Order { Op = Op.Name((OpId)Math.Max(0, dAgent)), Do = "fire", Target = id, Force = force };

        /// <summary>Seconds to down from a preview: the first shot (the next ready tick, or the draw and aim-new), then one each cadence.</summary>
        int PreviewDownS(FirePreview fp, int k, string target = "")
        {
            if (target.StartsWith("car:") && Game.CarOf(target) == null) return -1; // a car not yet shot has no health in the engine's preview (Keel: PreviewFire before EnsureCar)
            if (fp == null || fp.ShotsToDown <= 0 || fp.Refused.Length > 0 && fp.Refused != "out-of-range" && fp.Refused != "no-clear-line" || !fp.Line) return -1;
            var g = Game.Ops[k].Gun; if (g.W == null) return -1;
            int first = Drawn(k) ? Math.Max(0, g.NextShot - Game.Tick) : Game.R.DrawTicks + Game.R.AimNewTicks;
            int ticks = first + (fp.ShotsToDown - 1) * g.W.Every; return (ticks + Game.Hz - 1) / Game.Hz;
        }

        /// <summary>What the reticle is on, and the engine id a trigger fires at there ("" none): a hostile, the patrol, the rivals, the police
        /// (each unit by its own id), the guard, a person (forced: a civilian), a camera, a car. Nothing under it: the nearest fight hostile within
        /// 40 px (a small assist, never onto a non-combatant).</summary>
        Target AimTargetAt(Vector2 p, out string id, out bool force)
        {
            id = ""; force = false;
            var t = TargetAt(p);
            if (t != null && t.Kind != "op") { id = FireIdOf(t, out force); if (id.Length > 0 || t.Kind == "person") return t; }
            Target best = null; float bd = 40f * KitScale;
            foreach (var pk in Pickables())
            {
                var c = pk.T; if (!Combatant(c) || !cam) continue;
                var sp = cam.WorldToScreenPoint(pk.Anchor); if (sp.z <= 0f) continue;
                float d = (new Vector2(sp.x, sp.y) - p).magnitude; if (d < bd) { bd = d; best = c; }
            }
            if (best != null) id = FireIdOf(best, out force);
            return best;
        }

        /// <summary>The engine's fire target for a picked thing ("" when none): Vale is "councillor" (the press refuses it).</summary>
        string FireIdOf(Target t, out bool force)
        {
            force = false; if (t == null) return "";
            switch (t.Kind)
            {
                case "hostile": case "unit": return t.Key;
                case "patrol": case "rivals": case "security": return t.Kind;
                case "camera": return "camera:" + t.Key;
                case "car": return "car:" + t.Key;
                case "person": if (t.Key == "guard") return "guard"; force = true; return t.Key;
                case "crowd": force = true; return t.Id.Length > 0 ? t.Id : "";
                case "bystander": force = true; return t.Key; // engine 0.18: a range bystander (the engine takes it only forced)
            }
            return "";
        }

        /// <summary>The left press while aiming: a hostile of a fight fires at once; anything else, or a shot that fails the mission, starts the
        /// 0.5 s hold (the reticle's ring fills; release or moving off cancels). Vale never.</summary>
        void FirePress(Vector2 p)
        {
            var at = AimAt != null ? TargetWorld(AimAt) : null;
            if (AimAt != null && AimAt.Kind == "person" && AimAt.Key == "councillor") { PopKey("never-vale", p, at, Feedback.Refused, "never fire on Vale: the mission fails"); return; }
            if (AimId.Length == 0) { PopKey("nothing-there", p, null, Feedback.Info); return; }
            if (!Drawn(dAgent) && Game.Ops[dAgent].Gun.DrawReady <= Game.Tick && Loud == null) { PopKey("not-drawn", p, at, Feedback.Info); return; }
            LoudCard c = null; if (!Guard("loud", () => c = Game.GoingLoud(FireOrder(AimId, aimForce)))) return;
            if (c.Refused.Length > 0) { Pop(c.Refused, p, at, Feedback.Refused, 2f); return; }
            if (Combatant(AimAt) && !c.Hold) { TriggerPress(AimId, aimForce, null, p); return; }
            aimHoldStart = Time.unscaledTime; aimHoldId = AimId; aimHoldForce = aimForce; aimHoldCard = c;
            Status = $"Hold to fire on {Name(AimId)}";
        }

        /// <summary>The trigger pressed on `id` (Keel's logged input: op, on, target, tick). A press after a hold that made the run loud says so (the feed).</summary>
        void TriggerPress(string id, bool force, LoudCard card, Vector2? screen)
        {
            if (dAgent < 0) return;
            var o = new Order { Op = Op.Name((OpId)dAgent), Do = "trigger", On = true, Target = id, Force = force };
            bool ok = Issue(o, screen, DrawnAt(id));
            FireLogAdd($"trigger\t{o.Op}\ton\t{id}\t{(ok ? "taken" : "refused")}");
            if (!ok) return;
            TriggerDown = true; TriggerTarget = id;
            if (card != null && card.Loud) { FeedLoud(card, $"fire on {Name(id)}"); if (Text != null && Text.Events.TryGetValue("going-loud", out var e)) WorldBeat(e, null, "", Feedback.Refused, 1); }
        }

        void TriggerRelease()
        {
            if (dAgent < 0 || Game == null) { TriggerDown = false; return; }
            var o = new Order { Op = Op.Name((OpId)dAgent), Do = "trigger", On = false, Target = TriggerTarget };
            bool ok = Issue(o, null, null, quietRefusal: true);
            FireLogAdd($"trigger\t{o.Op}\toff\t{TriggerTarget}\t{(ok ? "taken" : "refused")}");
            TriggerDown = false;
        }

        /// <summary>For the proof and shots: the trigger by script, as the left button gives it while aiming (press, release).</summary>
        public void TriggerScripted(string id, bool on, bool force = false)
        {
            if (!Driving) return;
            if (on) { if (!Aiming) { Aiming = true; aimByScript = true; } trigByScript = true; TriggerPress(id, force, null, null); }
            else if (TriggerDown) TriggerRelease();
        }
        bool aimByScript, trigByScript;
        /// <summary>For the proof and shots: aim on or off with the reticle at a world point (the cursor stays the player's).</summary>
        public void AimScripted(bool on) { if (on && Driving && !Aiming) { AimBegin(cam ? (Vector2)cam.WorldToScreenPoint(views.DrivePos + Vector3.up) : Vector2.zero); aimByScript = Aiming; } else if (!on && Aiming) AimEnd(); } // the right button is not read; the left is

        // ---------------------------------------------------------------- E and the act on the prompt

        float eDown = -1f; bool eWasRing, eOpened;

        /// <summary>Third person's act key (controls Act, E): tap acts on the prompt (else the ring), held 0.22 s the ring; a ring open: a tap takes
        /// the lit way (or the hovered one); held over a harmful wedge it fills and takes when full.</summary>
        void TpE(Keyboard kb)
        {
            bool down = controls.Down(kb, PlayAction.Act), held = controls.Held(kb, PlayAction.Act); float now = Time.unscaledTime;
            if (down && !Paused) { eDown = now; eWasRing = Ring != null; eOpened = false; return; }
            if (eDown < 0f) return;
            if (held)
            {
                if (!eWasRing && !eOpened && now - eDown >= RingHoldS && TpResolve(TpIn.EHold, TpNow) == TpDo.Ring)
                {
                    eOpened = true;
                    if (DriveFocus == null) { dFocusKey = "?"; DriveFocusFrame(); }
                    if (DriveFocus is not Target t) { PopKey("nothing-here", null, views.DrivePos, Feedback.Info); return; }
                    TpRing(t, true); if (Ring != null) Ring.Sticky = false;
                }
                if (Ring is RingState rs && (eOpened || eWasRing))
                {   // E held over a harmful wedge: it fills, and takes when full (RingHoldFrame)
                    var mp = PointerOverride ?? UnityEngine.InputSystem.Mouse.current?.position.ReadValue() ?? rs.Screen; int s = RingSegAt(mp);
                    if (s >= 0 && s < rs.Segs.Count && rs.Segs[s].HoldWedge && rs.HoldSeg != s) { rs.HoldSeg = s; rs.HoldStart = now; rs.HoldSrc = "E"; }
                }
                return;
            }
            float took = now - eDown; eDown = -1f;
            if (eWasRing) { var rs = Ring; if (rs != null && took < RingHoldS) { int s = rs.Hover >= 0 ? rs.Hover : rs.Lit; if (s >= 0) RingTakeTry(s, "release"); } return; }
            if (eOpened)
            {
                var rs = Ring; if (rs == null) return;
                var mp = PointerOverride ?? UnityEngine.InputSystem.Mouse.current?.position.ReadValue() ?? rs.Screen;
                int s = (mp - rs.PointerAtOpen).magnitude > RingDeadPx * KitScale ? RingSegAt(mp) : -1;
                if (s >= 0) RingTakeTry(s, "release"); else { rs.Sticky = true; rs.Always = true; }
                return;
            }
            if (TpResolve(TpIn.ETap, TpNow) == TpDo.Act) TpAct(DriveFocus, "E");
        }

        /// <summary>The ring on a target in third person, centred on it.</summary>
        void TpRing(Target t, bool byE)
        {
            if (t == null || !cam) return;
            var w = TargetWorld(t) ?? views.DrivePos; var sp = cam.WorldToScreenPoint(w + Vector3.up);
            ContextPlan plan = null; if (!Guard("panel", () => plan = ContextFor(t, sp)) || plan == null) return;
            if (plan.Choice.Order.Count == 0) { ActContext(t, sp); return; }
            OpenRing(plan, sp, byE);
        }

        /// <summary>E (or a left click on the prompt): the prompted way when it is one clear best, no gamble and not harmful; else the ring. A
        /// way out of reach walks in and acts (Give hands the agent to the engine's walk). Never guesses.</summary>
        public void TpAct(Target t, string source)
        {
            if (t == null) { dFocusKey = "?"; DriveFocusFrame(); t = DriveFocus; }
            if (t == null || !cam) { PopKey("nothing-here", null, views.DrivePos, Feedback.Info); return; }
            var w = TargetWorld(t) ?? views.DrivePos; var sp = cam.WorldToScreenPoint(w + Vector3.up); var screen = new Vector2(sp.x, sp.y);
            if (t.Kind == "op") { ActContext(t, screen); return; }
            if (t.Kind == "person" && t.Key == "councillor" && IsFire(t)) { PopKey("never-vale", screen, w, Feedback.Refused, "never fire on Vale: the mission fails"); return; }
            if (Picked >= 0) { ActContext(t, screen); return; } // an armed verb (Shift+1-4) is the deliberate path: it acts (a harmful one still goes through the ring)
            ContextPlan plan = null; if (!Guard("panel", () => plan = ContextFor(t, screen)) || plan == null) return;
            var ch = plan.Choice;
            switch (ch.Act)
            {
                case ContextRank.Act.Default:
                    {
                        var r = plan.Row(ch.Pick);
                        if (r?.I?.Order != null && !Harmful(r.I.Order, t, AnyDrawn())) { FireLogAdd($"act\t{source}\t{r.I.Order.Do}:{r.I.Order.Approach}\t{t.Kind}/{t.Key}"); DoRow(r, t, screen, plan.Pn.World, source == "E" ? "E" : "left"); return; }
                        break;
                    }
                case ContextRank.Act.Lock: LockPop(plan.Row(ch.Pick), screen, plan.Pn.World); return;
                case ContextRank.Act.Hint: { var sel = new List<int>(); for (int k = 0; k < 4; k++) if (Selected[k]) sel.Add(k); Pop(WhoCan(t, sel), screen, null, Feedback.Info, 2f); return; }
                case ContextRank.Act.Silent: return;
            }
            OpenRing(plan, screen, source == "E"); if (Ring != null) { Ring.Sticky = true; Ring.Always = true; } // a check failed: the ring, nothing taken
        }

        // ---------------------------------------------------------------- the 0.5 s hold on harmful wedges (brief 038 §4.3, brief 024 rule 2)

        /// <summary>A wedge taken by an input: a harmful one (Kill, Fire on anyone not a combatant) takes only after its 0.5 s hold, with the
        /// input held over it (a release never takes it; the ring stays to hold on it).</summary>
        void RingTakeTry(int seg, string src)
        {
            var rs = Ring; if (rs == null || seg < 0 || seg >= rs.Segs.Count) return;
            if (!rs.Segs[seg].HoldWedge) { RingTake(seg); return; }
            rs.Sticky = true; rs.Always = true; rs.FromHold = false;
            if (src == "release") { Status = "Hold to take it"; return; }
            if (rs.HoldSeg != seg) { rs.HoldSeg = seg; rs.HoldStart = Time.unscaledTime; rs.HoldSrc = src; }
        }

        /// <summary>Per frame with a ring open: the harmful wedge's hold fills while its input is held over it; full, it takes (the going-loud card's
        /// hold is this one: the order goes without a second hold). True when the hold took the input.</summary>
        bool RingHoldFrame(UnityEngine.InputSystem.Mouse mouse, Vector2 p)
        {
            var rs = Ring; if (rs == null || rs.HoldSeg < 0) return false;
            var kb = Keyboard.current;
            bool held = rs.HoldSrc switch { "left" => mouse != null && mouse.leftButton.isPressed, "right" => mouse != null && mouse.rightButton.isPressed, "E" => controls.Held(kb, PlayAction.Act), _ => false };
            bool over = rs.HoldSrc == "right" && rs.HoldCtrl || RingSegAt(p) == rs.HoldSeg;
            if (!held || !over) { rs.HoldSeg = -1; rs.HoldStart = -1f; return true; }
            if (RingHoldProgress < 1f) return true;
            int s = rs.HoldSeg; rs.HoldSeg = -1; holdCommits = true;
            try { RingTake(s); } finally { holdCommits = false; }
            return true;
        }
        public float RingHoldProgress => Ring == null || Ring.HoldSeg < 0 || Game == null ? 0f : Mathf.Clamp01((Time.unscaledTime - Ring.HoldStart) / HoldSeconds);
        /// <summary>Set while a held wedge takes: the going-loud card's own hold is already done (Fight sends at once).</summary>
        bool holdCommits;

        /// <summary>Brief 038 §4.3: Ctrl + right on a person or thing that is not a combatant opens the ring with its Fire wedge under the
        /// button's hold (fills over 0.5 s while held; released early it stays open and takes nothing). Never an instant shot.</summary>
        bool CtrlFireRing(Target t, Vector2 p)
        {
            if (t == null || t.Kind == "op" || !controls.Held(Keyboard.current, PlayAction.Force) || Combatant(t)) return false;
            RightRing(t, p); var rs = Ring; if (rs == null) return true;
            int fi = rs.Segs.FindIndex(x => x.Row?.I?.Order?.Do == "fire");
            if (fi < 0) return true; // no Fire way here (Vale, nobody armed selected): the ring as it is
            rs.Hover = fi; rs.FromHold = false; rs.Sticky = true; rs.Always = true; rs.HoldSeg = fi; rs.HoldStart = Time.unscaledTime; rs.HoldSrc = "right"; rs.HoldCtrl = true;
            return true;
        }

        /// <summary>For the proof: stop the selection's walks and acts (C), and take a ring wedge as a click on it would.</summary>
        public void CancelSelectedScripted() => CancelSelected();
        public void RingTakeScripted(int seg) => RingTakeTry(seg, "release");

        // ---------------------------------------------------------------- the commander's hold-to-fire setting (off by default)

        bool holdFireOn; readonly List<int> holdFireAgents = new();
        /// <summary>controls.commanderHoldFire: a fire order given by a right press is ceased when the right button comes up.</summary>
        void CommanderHoldFireFrame(UnityEngine.InputSystem.Mouse mouse)
        {
            if (!holdFireOn || mouse == null) return;
            if (mouse.rightButton.isPressed || Loud != null) return;
            holdFireOn = false; var list = new List<Order>();
            foreach (var k in holdFireAgents) if (Armed(k) && Drawn(k) && Game.Ops[k].Gun.Order.Length > 0) list.Add(new Order { Op = Op.Name((OpId)k), Do = "cease" });
            holdFireAgents.Clear(); if (list.Count > 0) Send(list, null, SelectedWorld());
        }
        void CommanderHoldFireBegin() { if (!controls.commanderHoldFire || Driving) return; holdFireOn = true; holdFireAgents.Clear(); for (int k = 0; k < 4; k++) if (Selected[k] && Armed(k)) holdFireAgents.Add(k); }

        // ---------------------------------------------------------------- the follow setting (Keel's logged follow order)

        /// <summary>The follow setting changed (pause menu): the engine's follow order (op cell, on: lock), logged like any order.</summary>
        public void SetFollowLock(bool on)
        {
            controls.followLock = on; controls.SaveSettings();
            if (Game == null || Game.Over || Game.FollowLock == on) return;
            Issue(new Order { Op = "cell", Do = "follow", On = on }, null, null, quietRefusal: true);
            FireLogAdd($"follow-setting\t{(on ? "lock" : "with-trigger")}");
        }
        /// <summary>A run begins: the follow setting goes to the engine as an order when it is on (off is the engine's own default).</summary>
        void FollowSettingAtStart() { if (Game != null && controls.followLock && !Game.FollowLock) { Issue(new Order { Op = "cell", Do = "follow", On = true }, null, null, quietRefusal: true); FireLogAdd("follow-setting\tlock"); } }

        // ---------------------------------------------------------------- what fires now, read from the engine (FireStateOf), per frame

        /// <summary>Each agent's fire state this frame (Keel's FireStateOf): the tracer, the marks, the line and Stopped read only this.</summary>
        public readonly FireState[] FireNow = { NoFire, NoFire, NoFire, NoFire };
        /// <summary>Not firing (the struct's default carries null strings).</summary>
        public static readonly FireState NoFire = new() { Mode = "", Target = "", Lead = "", NextShot = -1 };
        /// <summary>When each agent's fire last stopped (unscaled s) and on what: the line reads "Stopped" for 1 s.</summary>
        public readonly float[] FireStoppedAt = { -10f, -10f, -10f, -10f };
        public readonly string[] FireStoppedOn = { "", "", "", "" };
        /// <summary>The agents that follow the driven one now (FollowersOf), and each one's lock (LockOf).</summary>
        public readonly List<OpId> Followers = new();
        public readonly string[] Locks = { "", "", "", "" };
        public const float StoppedS = 1f;
        readonly string[] fireKey = { "", "", "", "" }; readonly int[] shotTickLogged = { -1, -1, -1, -1 }; string followersKey = "";

        void FireFrame()
        {
            var g = Game; if (g == null) return;
            for (int k = 0; k < 4; k++)
            {
                FireState fs = default; if (!Guard("fire state", () => fs = g.FireStateOf((OpId)k))) return;
                bool was = FireNow[k].Firing; string wasOn = FireNow[k].Target ?? "";
                FireNow[k] = fs; Locks[k] = g.LockOf((OpId)k) ?? "";
                if (was && !fs.Firing) { FireStoppedAt[k] = Time.unscaledTime; FireStoppedOn[k] = wasOn; }
                if (fs.Firing) FireStoppedAt[k] = -10f;
                var key = $"{fs.Mode}\t{fs.Target}\t{(fs.Firing ? 1 : 0)}\t{(fs.Hollow ? 1 : 0)}\t{fs.Lead}";
                bool shotNew = fs.Shot && shotTickLogged[k] != g.Tick; if (shotNew) shotTickLogged[k] = g.Tick; // a shot once (Shot holds for its tick, over several frames)
                if (key != fireKey[k] || shotNew) { fireKey[k] = key; FireLogAdd($"state\t{Op.Name((OpId)k)}\t{fs.Mode}\t{fs.Target}\t{(fs.Firing ? "firing" : "-")}\t{(fs.Hollow ? "hollow" : "-")}\t{(fs.Shot ? "shot" : "-")}\t{fs.NextShot}\t{fs.Every}\t{fs.Lead}\tlock:{Locks[k]}"); }
            }
            Followers.Clear();
            if (Driving && dAgent >= 0) Guard("followers", () => Followers.AddRange(g.FollowersOf((OpId)dAgent)));
            var fk = string.Join(",", Followers);
            if (fk != followersKey) { followersKey = fk; FireLogAdd($"followers\t{(dAgent >= 0 ? Op.Name((OpId)dAgent) : "-")}\t{fk}"); }
        }

        /// <summary>The fire line's words for agent k ("Firing at the guard", "down in 3 s"), or "Stopped" for 1 s after its fire ends; "" none.</summary>
        public (string head, string down, bool stopped, bool drawn) FireLine(int k)
        {
            if (Game == null || k < 0 || k > 3) return ("", "", false, false);
            var fs = FireNow[k];
            if (fs.Firing && fs.Target.Length > 0)
            {
                var lk = $"{fs.Target}|{Game.Tick}";
                if (lineKey[k] != lk) { lineKey[k] = lk; int s = -1; FirePreview fp = null; if (Guard("fire line", () => fp = Game.PreviewFire((OpId)k, fs.Target, Game.Ops[k].Gun.TrigForce))) s = PreviewDownS(fp, k, fs.Target); lineDown[k] = s; } // priced once a tick
                return ($"Firing at {FireName(fs.Target)}", lineDown[k] >= 0 ? $"down in {lineDown[k]} s" : "", false, true);
            }
            if (Time.unscaledTime - FireStoppedAt[k] < StoppedS) return ("Stopped", "", true, Drawn(k));
            return ("", "", false, false);
        }

        /// <summary>A fire target's name in the line: a car by the text's car names ("Vale's car"), else the usual name.</summary>
        string FireName(string id) => id.StartsWith("car:") ? Text?.TargetName("car-" + id[4..], "the car") ?? "the car"
            : id.StartsWith("rival") && id != "rivals" ? "a rival" : id.StartsWith("robot") ? "a robot" : id.StartsWith("police") || id.StartsWith("officer") || id.StartsWith("security-") || id.StartsWith("unit") ? "an officer" : Name(id); // a fight hostile by what it is, never its id

        /// <summary>The agent whose line shows: the driven one, else the selected agent (the commander's), else none.</summary>
        public int FireLineAgent => Driving ? dAgent : (int)Agent;
        readonly string[] lineKey = { "", "", "", "" }; readonly int[] lineDown = { -1, -1, -1, -1 };

        /// <summary>For the HUD: whether agent k's fire mark is the return mark (returning fire on its shooter).</summary>
        public bool Returning(int k) => FireNow[k].Firing && FireNow[k].Mode == "return";

        /// <summary>Car effects the HUD draws (shape cues, no text): each car's state and when the host first saw it blow.</summary>
        public readonly Dictionary<string, float> CarBlownAt = new();
        /// <summary>The cars that can be shot now: the Handover's three, or the fight range's spawned cars (engine 0.18).</summary>
        public IEnumerable<string> CarIdsNow()
        {
            var g = Game; if (g == null) yield break;
            if (g.Range) { foreach (var c in g.RangeCars()) yield return c.Id; yield break; }
            foreach (var id in Game.CarIds) yield return id;
        }
        void CarFrame()
        {
            var g = Game; if (g == null) return;
            foreach (var id in CarIdsNow()) { var c = g.CarOf(id); if (c != null && c.Blown && !CarBlownAt.ContainsKey(id)) CarBlownAt[id] = Time.unscaledTime; }
        }
    }
}
