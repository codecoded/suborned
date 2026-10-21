// The trigger, locks and follow fire (engine 0.17.0; Sky on firing 2026-10-21, brief 024; Motive, briefs/027/motive/tuning.md §11; rulebook
// weapons.trigger, weapons.follow). Both presets. The driven agent fires with a logged trigger input (op, on or off, target, tick); a lock is a
// lasting fire order (the commander's click); drawn agents near the driven one that are not engaged follow its fire. Every follower is derived
// by the engine from the state and the logged inputs (op id order, integer distances), never issued by the host, so a replay is exact.
//  · A press fires one shot at the first ready tick (a buffer of one) and, held, fires on cadence. Release keeps the target and the count:
//    pressing again on the same target pays no aim-new and resets nothing (the tap reset closed, §11.2 rule 2).
//  · The follow setting (start followLock, or a logged follow order): off, followers fire at the target only while the trigger is held and
//    keep their aim on release; on, a press (or a change of target while pressed) locks every follower at that tick (a fire order with its
//    walk-in spent: it lapses weapons.lineLapse out of line and never walks).
//  · Engaged: locked (a fire order), or returning fire on its own shooter (stance return with a live shooter; at will with its own target).
//    A holding agent never follows and is never locked by a press (tuning.md §13.2, engine 0.18.0): hold fires only when ordered.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>What the host reads for an operative's fire each tick (tracer, marks, the follow mark, the cadence refill). Read-only.</summary>
    public struct FireState
    {
        /// <summary>trigger (pressed, or a press waiting for its shot), trigger-held (released: the aim and the count kept), lock (a lasting
        /// fire order), follow (firing with the driven agent's trigger), follow-held (the trigger released: the follower's aim kept), return
        /// (returning fire on its shooter), free (at will on its own target), "" (not firing).</summary>
        public string Mode;
        public string Target;  // what it aims at ("" none)
        public bool Firing;    // it shoots at Target as its cadence allows this tick: drawn and ready, reach and a line (the tracer is on)
        public bool Hollow;    // a follower (or a lock) with no reach or line to its target: it does not fire (the hollow follow mark)
        public bool Shot;      // it fired this tick (muzzle flash, recoil)
        public int NextShot, Every; // the tick of its next possible shot and its weapon's cadence (ticks): the reticle's refill
        public string Lead;    // the driven agent it follows or locked with ("" none)
    }

    public sealed partial class Game
    {
        /// <summary>Tuning.md §11.5: the follow setting (true: followers lock; false: they fire with the trigger). Start.followLock, then follow orders.</summary>
        public bool FollowLock;
        /// <summary>Tuning.md §11.1: the follow ring's radius round the driven agent (cm; weapons.follow, follow-m 8). The host draws it while the agent aims.</summary>
        public int FollowRingCm => R.FollowCm;
        readonly int[] opShotTick = { -1, -1, -1, -1 }; // the tick each operative last fired (the host's flash; output only: not hashed)
        readonly string[] lockLead = { "", "", "", "" }; // the driven agent a lock was taken with (the host's mark; output only: not hashed)

        /// <summary>The follow setting changed in play (op "cell", on: lock). Logged like any order, so the followers derive from the log alone.</summary>
        string FollowSet(Order o)
        {
            if (o.Op != "cell") return No("follow-not-cell", "the follow setting is the cell's: op \"cell\"");
            if (FollowLock != o.On) Note("fire.follow-setting", o.On ? "Follow: agents near the driven one lock its target." : "Follow: agents near the driven one fire with its trigger.");
            FollowLock = o.On;
            return "";
        }

        /// <summary>The trigger (tuning.md §11.2, §11.4): a press or a release of the driven agent's trigger on a target.</summary>
        string Trigger(Op op, Order o)
        {
            var g = op.Gun; string name = Sim.Op.Name(op.Id);
            if (!o.On) // a release keeps the target, its aim and its count; a press still waiting for its shot fires it (a tap is on, then off)
            {
                if (g.Trig) Note("fire.release", $"{name} lets go of the trigger.");
                g.Trig = false;
                return "";
            }
            if (!op.Driven) return No("not-driven", "the trigger is the driven agent's; a fire order locks any other");
            if (g.W == null) return No("no-weapon", $"{o.Op} carries no weapon (Arms)");
            if (!g.Drawn || g.HolsterAt >= 0) return No("not-drawn", "no weapon drawn");
            if (op.Busy != null) return No("agent-busy", $"{o.Op} is busy until {op.BusyEnd}", tick: op.BusyEnd);
            if (op.Carrying.HasValue || op.CarryBody.Length > 0) return No("fire-carrying", "carrying, the operative doesn't fire");
            if (CarriesVale(op)) return No("fire-carrying-vale", "walking Vale, the Enforcer doesn't fire: the others cover him (brief 018)");
            string t = FireTarget(op, o.Target);
            if (t.Length == 0) return No("no-target", o.Target == "hostiles" ? "no hostile in range with a clear line" : $"no {o.Target} to fire at");
            if (IsOpId(t)) return No("fire-on-cell", "the cell doesn't fire on its own");
            if (Range && !IsFightHostile(t)) return No("target-unknown", $"target '{o.Target}' is not one of the range's spawned hostiles (brief 029)");
            if (IsCameraId(t)) EnsureCameraGun(t);
            if (IsCarId(t)) EnsureCar(t);
            var tg = GunOf(t); if (tg == null) return No("target-unknown", $"target '{o.Target}' is unknown (unknown = reject)");
            if (IsCivilian(t) && !o.Force) return No("civilian-target", $"{t} is a civilian or a contact: a target only when forced (Ctrl)");
            if (!PosOf(t).HasValue || tg.Down) return No("no-target", $"no {o.Target} to fire at");
            // out of reach or line it is taken all the same: it fires once it has both (the driven agent is never walked: brief 030)
            bool fresh = !g.Trig || g.TrigT != t; // a press, or a change of target while pressed (§11.2 rule 3)
            if (g.Order.Length > 0) // the trigger takes over from the agent's own fire order; the aim on that target is kept
            {
                string keep = g.Target; OrderEnd(op, false); if (keep == t) g.Target = keep;
                Note("fire.trigger-takes", $"{name}: the trigger takes over from its fire order.");
            }
            bool aimNew = g.TrigT != t;
            if (aimNew) g.TrigTap = false; // a press waiting on another target loses its shot (§11.4)
            g.TrigT = t; g.Trig = true; g.TrigTap = true; g.TrigForce = o.Force;
            if (aimNew) { Actions.Add("fire:" + t); Note("fire.trigger", $"{name} fires on {t}."); } // a new target: logged once (a press again on it is a press)
            if (fresh && FollowLock) LockFollowers(op, t, o.Force);
            return "";
        }

        /// <summary>Follow on (§11.2 rule 3): every agent that follows the driven one now, with its own reach and line, locks its target. One in
        /// the ring that is locked on another target still up is asked: if that target falls in the tick that follows, the press takes it then
        /// (LateLocks), so a press in the tick a lock's target falls is never lost to the lock (engine 0.18.0).</summary>
        void LockFollowers(Op lead, string t, bool force)
        {
            string ln = Sim.Op.Name(lead.Id);
            var at = PosOf(t); if (!at.HasValue) return;
            foreach (var o in Ops)
            {
                if (o.Gun.Order != t && LiveLock(o.Gun) && CanFollow(o, lead, true)) { o.Gun.LockAskBy = ln; o.Gun.LockAsk = t; o.Gun.LockAskForce = force; continue; }
                if (!CanFollow(o, lead) || !G.Within(o.Pos, at.Value, o.Gun.W!.MaxCm) || !Sees(o.Pos, at.Value)) continue;
                LockOne(o, ln, t, force);
            }
        }

        void LockOne(Op o, string ln, string t, bool force)
        {
            var g = o.Gun;
            if (g.Order.Length > 0) OrderEnd(o, true); // its lock's target is down: that order ends here
            if (g.FollowBy.Length > 0) { g.FollowBy = ""; }
            g.Order = t; g.Force = force; g.LostAt = -1; g.OrderTick = Tick;
            o.CloseSpent = true; o.CloseWalk = false; // its walk-in is spent: out of line it holds lineLapse and lapses, and never walks
            if (Lab) g.Then = ""; // the lab: a lock returns to its stance (a holding agent is never locked: §13.2)
            lockLead[(int)o.Id] = ln;
            Actions.Add("fire:" + t);
            Note("fire.lock", $"{Sim.Op.Name(o.Id)} locks on {t} with the {ln}.");
        }

        /// <summary>The end of a tick's fire (after Resolve): an agent a press asked (LockFollowers) whose lock's target fell this tick locks the
        /// pressed target, if the lead still drives with that target, follow is still on, the target is up, and the agent now follows with its
        /// own reach and line. Every ask is cleared: it lasts the one tick.</summary>
        void LateLocks()
        {
            foreach (var o in Ops)
            {
                var g = o.Gun; if (g.LockAsk.Length == 0) continue;
                string t = g.LockAsk, ln = g.LockAskBy; bool force = g.LockAskForce;
                g.LockAsk = g.LockAskBy = ""; g.LockAskForce = false;
                if (!FollowLock || g.Order.Length == 0 || LiveLock(g)) continue; // the lock's target is still up (or the lock ended otherwise): the press is spent
                var lead = OpOf(Sim.Op.Parse(ln)); var tg = GunOf(t); var at = PosOf(t);
                if (lead.Gun.TrigT != t || tg == null || tg.Down || !at.HasValue) continue;
                if (!CanFollow(o, lead) || !G.Within(o.Pos, at.Value, g.W!.MaxCm) || !Sees(o.Pos, at.Value)) continue;
                LockOne(o, ln, t, force);
            }
        }

        /// <summary>§11.1: `o` follows the driven agent `lead`: drawn, not holding (§13.2), not locked, not returning fire (nor at will on its own target), not busy, not carrying, not inside or in the car, within the follow ring (integer cm). Reach and line are the shot's.</summary>
        bool CanFollow(Op o, Op lead, bool locked = false)
        {
            var g = o.Gun;
            if (o == lead || o.Driven || g.Down || g.W == null || !g.Drawn || g.HolsterAt >= 0) return false;
            if (lead.Down || lead.Inside || lead.InCar || !lead.Driven) return false;
            if (o.Inside || o.InCar || o.Busy != null || o.Carrying.HasValue || o.CarryBody.Length > 0 || o.CarriedBy.HasValue || CarriesVale(o)) return false;
            if (g.Stance == "hold") return false; // tuning.md §13.2 (engine 0.18.0): hold fires only when ordered; following is derived, never ordered, so a holding agent neither follows nor is locked by a press
            if (locked) return G.Within(o.Pos, lead.Pos, R.FollowCm); // LockFollowers' ask: everything but the lock (its stance doesn't fire while locked)
            if (LiveLock(g)) return false; // locked: engaged
            if (!G.Within(o.Pos, lead.Pos, R.FollowCm)) return false;
            return StanceTarget(o).Length == 0; // returning fire on its own shooter (or at will on its own target): engaged
        }

        /// <summary>A lock on a target still up and out on the street: a lock whose target fell this tick ends at the agent's next shot, and
        /// is no engagement meanwhile (a press in that tick takes the agent).</summary>
        bool LiveLock(Gun g) { if (g.Order.Length == 0) return false; var tg = GunOf(g.Order); return tg != null && !tg.Down && PosOf(g.Order).HasValue; }

        /// <summary>The driven agent whose trigger `o` fires with (follow off), "" if none: the first in operative id order that has a target.</summary>
        string FollowLeadOf(Op o)
        {
            if (FollowLock) return "";
            foreach (var l in Ops) if (l.Driven && l.Gun.TrigT.Length > 0 && CanFollow(o, l)) return Sim.Op.Name(l.Id);
            return "";
        }
        bool AnyTrigger() { foreach (var l in Ops) if (l.Driven && l.Gun.TrigT.Length > 0) return true; return false; }

        /// <summary>The trigger ends: a fire order given, cease, holster, the agent down, control ending.</summary>
        static void ClearTrigger(Gun g) { g.Trig = g.TrigTap = g.TrigForce = false; g.TrigT = ""; }

        // ---------------------------------------------------------------- the host's reads (change nothing)

        /// <summary>The agents that follow the driven agent `lead` now (§11.1: the follow mark), in id order, whether or not they have a line.</summary>
        public List<OpId> FollowersOf(OpId lead)
        {
            var l = OpOf(lead); var list = new List<OpId>();
            foreach (var o in Ops) if (CanFollow(o, l)) list.Add(o.Id);
            return list;
        }
        /// <summary>An operative's lock (its lasting fire order's target), "" if none.</summary>
        public string LockOf(OpId id) => OpOf(id).Gun.Order;

        /// <summary>An operative's fire this tick (the tracer, the marks, the cadence refill), read from the engine's own state.</summary>
        public FireState FireStateOf(OpId id)
        {
            var op = OpOf(id); var g = op.Gun;
            var s = new FireState { Mode = "", Target = "", Lead = "", NextShot = g.NextShot, Every = g.W?.Every ?? 0, Shot = opShotTick[(int)id] == Tick };
            if (g.Down || g.W == null || !g.Drawn || g.HolsterAt >= 0 || op.Inside || op.InCar) return s;
            string t = ""; bool fires = true;
            if (g.Order.Length > 0) { s.Mode = "lock"; t = g.Order; s.Lead = lockLead[(int)id]; }
            else if (g.TrigT.Length > 0) { t = g.TrigT; fires = g.Trig || g.TrigTap; s.Mode = fires ? "trigger" : "trigger-held"; }
            else if (g.FollowBy.Length > 0 && !FollowLock)
            {
                var l = OpOf(Sim.Op.Parse(g.FollowBy)); t = l.Gun.TrigT; fires = l.Gun.Trig; s.Mode = fires ? "follow" : "follow-held"; s.Lead = g.FollowBy;
            }
            else { t = StanceTarget(op); if (t.Length > 0) s.Mode = g.Stance == "return" ? "return" : "free"; }
            s.Target = t;
            if (t.Length == 0) return s;
            var at = PosOf(t); var tg = GunOf(t);
            bool line = at.HasValue && tg != null && !tg.Down && G.Within(op.Pos, at.Value, g.W.MaxCm) && Sees(op.Pos, at.Value);
            s.Hollow = !line && (s.Mode == "follow" || s.Mode == "follow-held" || s.Mode == "lock");
            s.Firing = fires && line && Ready(g) && op.Busy == null && !op.Carrying.HasValue && op.CarryBody.Length == 0 && !CarriesVale(op);
            return s;
        }
    }
}
