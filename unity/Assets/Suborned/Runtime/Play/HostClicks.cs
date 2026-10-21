using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 041, Sky's pick from the web controls playground (testbed/controls: data.js PRESETS.dbl, app.js leftClick and rightTap):
    /// the Double click scheme. A single left click on a thing selects it (the verb pill stays on it) or, on open ground, walks; a double click
    /// on a thing does its main act (ContextRank's one clear best, walking there first; a gamble or a tie, or only violence, opens the ring
    /// instead: the host never guesses), and on the ground runs. A thing with no calm way (only violence) opens the ring on the first click; a
    /// locked thing says its lock; a passer-by is the ground under it. A right tap (released inside 0.22 s and 6 px) opens the ring; a right
    /// hold opens nothing; with a ring open a right press takes the wedge under it, else closes the ring (the release then opens the next
    /// thing's ring). Fire on a combatant stays on the right press (the fight's sweep and hold-to-fire). No hold opens a ring in either view
    /// (HoldRing, read by TpResolve). Built (controls.preset): 0.2.34's flows, untouched.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>The thing a single click selected (the verb pill stays on it until the ground is clicked, Esc, an act or a ring); null none.</summary>
        public Target SelTarget { get; private set; }
        /// <summary>SelTarget's main act and price (refreshed a few times a second; the pill's words).</summary>
        public Intent SelIntent { get; private set; }
        float selPricedAt = -9f; Target lastThing; float lastThingAt = -9f;
        bool rTapRing; Target rTapT; Vector2 rTapP; float rTapT0;
        /// <summary>Whether a hold opens the ring (E held, the left button held on the prompt): Built only. TpResolve reads it.</summary>
        public static bool HoldRing = false;
        /// <summary>The proof's counters: double clicks that gave a thing's act, single clicks that selected one.</summary>
        public int DoubleActs { get; private set; }
        public int ThingSelects { get; private set; }

        /// <summary>The pause menu changed the scheme: nothing selected, no ring, the hold rule set.</summary>
        public void ControlsChanged() { HoldRing = !controls.Modern; HoldControlsChanged(); SelTarget = null; SelIntent = null; CloseRing(); intentKey = ""; }

        public void ClearSel() { SelTarget = null; SelIntent = null; }

        /// <summary>The proof's probe at a screen point: what a left click and a right click would hit, the thing's ContextRank choice, and
        /// whether the HUD takes the point.</summary>
        public string ProbeAt(Vector2 sp)
        {
            var l = TargetAt(sp, true); var r = TargetAt(sp); string ch = "";
            if (r != null && r.Kind != "op") { ContextPlan plan = null; Guard("panel", () => plan = ContextFor(r, sp)); if (plan != null) ch = plan.Choice.Act + "/" + plan.Choice.Why + "/" + plan.Choice.Order.Count; }
            return $"left {l?.Kind}/{l?.Key}, right {r?.Kind}/{r?.Key}, choice {ch}, hud {OverHud(sp)}{(OverHud(sp) ? " (" + HudToolkit.PickedName(sp) + ")" : "")}";
        }

        void SelectThing(Target t) { SelTarget = t; SelIntent = null; selPricedAt = -9f; ThingSelects++; }

        /// <summary>Per frame: the selected thing's price (the pill), dropped when the thing is gone.</summary>
        void SelFrame()
        {
            HoldRing = !controls.Modern;
            if (SelTarget == null) return;
            if (!controls.DoubleClick || TargetWorld(SelTarget) == null || Ring != null || Panel != null) { ClearSel(); return; }
            if (Time.unscaledTime - selPricedAt < 0.25f) return;
            selPricedAt = Time.unscaledTime; Intent i = null; var t = SelTarget; Guard("price", () => i = ContextIntent(t)); SelIntent = i;
        }

        static bool SafeRing(ContextRank.Choice ch) => ch.Why is "gamble" or "tie"; // a calm best way the player must still choose (never a quick act)

        /// <summary>Double click scheme: a left click (released) on a thing that is not an agent. False: the click is the ground's (walk, run).</summary>
        bool ThingClick(Target t, Vector2 screen, bool shift)
        {
            if (Picked >= 0) { ClearSel(); ActContext(t, screen); return true; } // an armed action (Shift+1–4, Tab) is the deliberate path
            ContextPlan plan = null; if (!Guard("panel", () => plan = ContextFor(t, screen)) || plan == null) return false;
            var ch = plan.Choice;
            if (ch.Act == ContextRank.Act.Silent) return false; // a passer-by, a unit, an empty stash: the ground under it
            bool twice = lastThing != null && SameTarget(lastThing, t) && Time.unscaledTime - lastThingAt < controls.doubleSeconds;
            lastThing = twice ? null : t; lastThingAt = twice ? -9f : Time.unscaledTime;
            switch (ch.Act)
            {
                case ContextRank.Act.Default:
                    if (!twice) { SelectThing(t); return true; }
                    ClearSel(); if (Panel == null && BoardSelection(t, screen)) return true;
                    DoubleActs++; DoRow(plan.Row(ch.Pick), t, screen, plan.Pn.World, "left"); return true;
                case ContextRank.Act.Ring:
                    if (!twice && SafeRing(ch)) { SelectThing(t); return true; }
                    TapRing(t, screen); return true;
                case ContextRank.Act.Lock: ClearSel(); LockPop(plan.Row(ch.Pick), screen, plan.Pn.World); return true;
                case ContextRank.Act.Hint:
                    { ClearSel(); var sel = new System.Collections.Generic.List<int>(); for (int k = 0; k < 4; k++) if (Selected[k]) sel.Add(k); Pop(WhoCan(t, sel), screen, null, Feedback.Info, 2f); return true; }
                default: return false;
            }
        }

        /// <summary>The ring opened by a tap (or a click on a thing with no calm way): it stays until a wedge is taken or a click lands elsewhere.</summary>
        void TapRing(Target t, Vector2 p)
        {
            ClearSel();
            if (t == null || t.Kind == "op" || Panel != null) { ActContext(t, p); return; }
            if (BoardSelection(t, p)) return; // brief 018: the whole cell selected, the car boards each (one way, no ring)
            ContextPlan plan = null; if (!Guard("panel", () => plan = ContextFor(t, p)) || plan == null) return;
            if (plan.Choice.Order.Count == 0) { ActContext(t, p); return; }
            OpenRing(plan, p, false); if (Ring != null) { Ring.Sticky = true; Ring.Always = false; Ring.FromHold = false; }
        }

        /// <summary>Double click scheme, the right button with no ring open: a press starts a tap; its release (quick and still) opens the ring on
        /// a thing, or on the ground stops the walks; a hold or a drag does nothing.</summary>
        void RightTapBegin(Target t, Vector2 p) { rTapRing = true; rTapT = t; rTapP = p; rTapT0 = Time.unscaledTime; }

        bool RightTapFrame(UnityEngine.InputSystem.Mouse mouse, Vector2 p)
        {
            if (!rTapRing) return false;
            if (!mouse.rightButton.isPressed)
            {
                rTapRing = false;
                if (Time.unscaledTime - rTapT0 < RingHoldS && (p - rTapP).magnitude < RingMovePx * KitScale) { if (rTapT == null || rTapT.Kind == "op") ActContext(rTapT, rTapP); else TapRing(rTapT, rTapP); }
                return true;
            }
            if (Time.unscaledTime - rTapT0 >= RingHoldS || (p - rTapP).magnitude >= RingMovePx * KitScale) rTapRing = false; // a hold or a drag: nothing
            return true;
        }

        /// <summary>Double click scheme, a ring open and the right button pressed: the wedge under it is taken; else the ring closes, and a quick
        /// release over another thing opens that thing's ring.</summary>
        bool RingRightDbl(Vector2 p)
        {
            var rs = Ring; if (rs == null) return false;
            if (rs.Hover >= 0) { RingTakeTry(rs.Hover, "right"); return true; }
            var t2 = TargetAt(p); CloseRing();
            if (t2 != null && t2.Kind != "op" && !OverHud(p)) RightTapBegin(t2, p);
            return true;
        }
    }
}
