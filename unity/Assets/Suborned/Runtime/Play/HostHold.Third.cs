using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Third person in the hold preset (moved from HostHold.cs, brief 085). The buttons mean what they mean on the map (Sky 2026-11-02: "same"): a left tap on a thing opens its ring, nothing taken; a right tap does nothing; a right hold (the setting "Right hold fires", the
    /// default) fills and, let go when full, gives the driven agent and the squad a lasting fire order (as the map's); in a fight a right click on a hostile focuses at once; a left hold acts on the
    /// thing under the cursor (or the focus), committing on release; a left drag orbits.
    /// Sky on 0.2.212, "the action ring doesn't come up": a left tap on a thing gave nothing, while the map's left click rings and the table (TpResolve) said Ring, so the habit of the map failed
    /// silently; and a tap with 6 px of travel, as a trackpad click has, gave nothing either.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>A tap's travel (kit px): under it, a press on a thing that comes up is a tap. Under the ring's dead zone, so the ring it opens has nothing under the pointer.</summary>
        public const float TpTapSlopPx = 16f;
        /// <summary>The proof reads it: left taps in third person that opened a ring.</summary>
        public int TpLeftRings { get; private set; }

        /// <summary>What a press is on when it is on something: under the pointer, or the focus when the pointer is just off it (a patrol walking out from under a still cursor). Null: the ground.</summary>
        Target TpUnder(Vector2 p)
        {
            var t = TargetAt(p); if (t != null && t.Kind != "op") return t;
            if (DriveFocus is not Target f || f.Kind == "op" || !cam || TargetWorld(f) is not Vector3 w) return null;
            var sp = cam.WorldToScreenPoint(w + Vector3.up); return sp.z > 0f && ((Vector2)sp - p).magnitude < FireAcquirePx * KitScale ? f : null;
        }

        /// <summary>A left tap: the thing's ring, nothing taken (as the map's click: ClickRing), or who can if the selection has no way on it. On the ground, nothing: WASD walks.</summary>
        void TpLeftTap(Target t, Vector2 at, Vector2 p)
        {
            if (t == null || (p - at).magnitude >= TpTapSlopPx * KitScale || TpResolve(TpIn.LeftTap, TpNow) != TpDo.Ring) return;
            if (ClickRing(t, at, out var plan)) { TpLeftRings++; SideLog("click", "ring", t); return; }
            if (t.Kind != "crowd" && plan != null && plan.Pn.Rows.Count == 0 && WhoCanPop(t, at)) SideLog("click", "who-can", t);
        }

        /// <summary>Per frame while driven. True when it took the pointer.</summary>
        bool TpHold(UnityEngine.InputSystem.Mouse mouse, Vector2 p, bool onHud)
        {
            if (!HoldOn || Ring != null) return false;
            var kb = Keyboard.current; bool shift = controls.Held(kb, PlayAction.AddToGroup); float now = Time.unscaledTime;
            HoldLatchFrame(p);
            if (controls.tpRightFires)
            {
                if (mouse.rightButton.wasPressedThisFrame && !onHud && !Aiming)
                {
                    RightClickUsed = true; rp = new Press { On = true, At = p, T0 = now, T = TargetAt(p) };
                    if (lp.On && Charge is { Button: 0, Done: false }) { CancelHold(); rp.Used = true; return true; } // a right click cancels a left hold
                    var ft = rp.T != null && rp.T.Kind != "op" ? rp.T : TpFireFocus() ?? TpHostileAhead(); // item 7: the cursor off a target, the focus the pill names
                    if (FocusTarget(ft, out var fid, out var fforce) && Driving && dAgent >= 0) { rp.Used = true; FocusFire(ft, fid, fforce, p, null); }
                    return true; // under 0.2 s it is a tap (nothing); the charge begins at 0.2 s
                }
                if (rp.On && mouse.rightButton.isPressed && Charge?.Button != 1 && !Aiming && !rp.Used && !rp.Cancelled && now - rp.T0 >= RightTapS)
                {
                    var (id, force) = HoldFireId(rp.T);
                    if (id.Length == 0 && (rp.T == null || rp.T.Kind == "op") && TpFireFocus() is Target df) { rp.T = df; (id, force) = HoldFireId(df); } // item 7 (Keel 2026-10-22): the focus the pill offers fire on, as the left hold falls back to it
                    if (id.Length == 0 && TpHostileAhead() is Target ha) { rp.T = ha; (id, force) = HoldFireId(ha); }
                    if (id.Length > 0) Charge = new HoldCharge { T = rp.T, Button = 1, Start = rp.T0, Fire = true, FireId = id, Force = force, Bystander = force, Verb = "Fire", Extra = FireExtra(id, force) };
                    else rp.Used = true;
                }
                if (rp.On)
                {
                    if (!mouse.rightButton.isPressed)
                    {
                        var was = rp; rp = default; var c = Charge?.Button == 1 ? Charge : null;
                        if (was.Used || was.Cancelled) { if (c != null) Charge = null; return true; }
                        if (c != null) { Charge = null; if (c.Ready) TpFireLock(c, p); else { HoldCancels++; Status = "Fire cancelled"; SideLog("click", "fire-cancelled", c.T); } return true; }
                        RightTaps++; // no charge began: a tap, which does nothing, as on the map (item 33)
                        return true;
                    }
                    return true;
                }
            }
            if (mouse.leftButton.wasPressedThisFrame && !onHud && !Aiming)
            {
                lTapTarget = TpUnder(p); var t = lTapTarget ?? DriveFocus;
                var (plan, way) = MainOf(t, p);
                lp = new Press { On = true, At = p, T0 = now, T = t };
                if (way >= 0) Charge = new HoldCharge { T = t, Button = 0, Start = now, Plan = plan, Way = way, Run = shift, Verb = plan.Row(way).I.Label, Doers = DoersOf(plan, way) };
                return true;
            }
            if (lp.On)
            {
                var c = Charge?.Button == 0 ? Charge : null;
                if (lp.Cancelled) { if (!mouse.leftButton.isPressed) lp = default; return true; }
                if (c == null && (p - lp.At).magnitude >= (lTapTarget != null ? TpTapSlopPx : RingMovePx) * KitScale && !lp.Used) { var from = lp.At; lp = default; dragFrom = from; dragging = true; orbiting = false; return false; } // a drag with no hold: the orbit takes it; on a thing a little more travel, so a trackpad click stays a click
                if (!mouse.leftButton.isPressed)
                {
                    var was = lp; lp = default;
                    if (c == null) { TpLeftTap(lTapTarget, was.At, p); return true; } // a thing with no calm way (a rival: Fire alone) rings as any other
                    Charge = null;
                    if (c.Ready) HoldGive(c.Plan, c.Way, c.T, was.At, c.Run || shift, "left", false, c.Doers);
                    else if (now - c.Start >= ChargeShowS) { HoldCancels++; Status = $"{c.Verb} cancelled"; }
                    else TpLeftTap(lTapTarget, was.At, p); // under the pill's 0.2 s: a tap, not a hold let go (TpResolve: LeftTap opens the ring)
                    return true;
                }
                return true;
            }
            return false;
        }

        /// <summary>Third person, a right hold let go when full: the lasting fire order of the driven agent and the squad (the engine never walks a driven agent in:
        /// out of reach, its order holds until it is in range).</summary>
        void TpFireLock(HoldCharge c, Vector2 p)
        {
            if (!Driving || dAgent < 0) return;
            var who = Shooters(false, c.FireId, c.Force, mark: true); if (who.Count == 0) { NobodyFires(p, c.T); return; } // brief 085: the squad fires with the agent in hand, each as on the map (holstered draws, far walks in, can't says why)
            var list = new List<Order>(); foreach (var k in who) list.Add(new Order { Op = Op.Name((OpId)k), Do = "fire", Target = c.FireId, Force = c.Force });
            holdCommits = true; try { Fight(list, "right", p, TargetWorld(c.T), $"fire at {Name(c.FireId)}"); } finally { holdCommits = false; }
            LocksDrop(who); FireLocks.Add(new FireSession { Agents = who, Id = c.FireId, Name = FireName(c.FireId), Force = c.Force, Start = Time.unscaledTime });
            FireHoldsBegun++; FireLogAdd($"tp-fire\t{string.Join(",", who)}\t{c.FireId}");
        }
    }
}
