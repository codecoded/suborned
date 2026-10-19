// What an operative is doing, and stopping it (Sky 2026-10-15 playtest: "there's nothing showing me what the cell player is doing when
// they're busy, nor is there a way to cancel their action"). Activity(op) is the HUD's read, every tick, and changes nothing. The cancel
// order stops the activity it shows; what that costs or keeps is the rulebook's `cancel` section.
using System;

namespace Suborned.Sim
{
    /// <summary>One operative's current activity, as the HUD shows it.</summary>
    public sealed class Activity
    {
        public string Op = "";
        public string Verb = "";        // move, watch, listen, turn, loop, reroute, mislead, enter, take, carry, fire, draw, holster; "" idle
        public string Target = "";      // the role, place, camera or operative the verb is on ("" if none)
        public string Phase = "idle";   // idle, walking, running, acting, holding, firing, drawing, down
        public int Progress;            // per mille, 0..1000 (integer state: 1000 = done); 0 for an open-ended activity (holding, firing)
        public int TicksLeft;           // ticks until it lands or arrives; for firing, until the next shot; -1 if open-ended
        public string OnDone = "";      // what happens when it lands (an outcome id or a short line), "?" when the card shows "?"
        public string Queued = "";      // the engine keeps no queue: always "" (a queue in the host is the host's to cancel)
        public bool CanCancel;          // whether a cancel order is taken now
        public string CancelCost = "";  // the rulebook cancel line that applies (stop, lost, aborted, ends, staff-report, …) or why not
        public string CancelText = "";  // that consequence in words
    }

    public sealed partial class Game
    {
        /// <summary>Every operative's activity now (scout, fixer, tech, enforcer).</summary>
        public Activity[] Activities() { var a = new Activity[4]; for (int i = 0; i < 4; i++) a[i] = ActivityOf((OpId)i); return a; }

        /// <summary>One operative's activity now: an act in progress first, then a walk or run, then firing, then a draw, then a loop held,
        /// then a downed operative carried; else idle. The cancel order stops exactly this one. Pure: changes nothing.</summary>
        public Activity ActivityOf(OpId id)
        {
            var op = OpOf(id); var g = op.Gun;
            var a = new Activity { Op = Sim.Op.Name(id) };
            if (op.Down) { a.Phase = "down"; a.CancelCost = "down"; a.CancelText = "a downed operative takes no orders"; return a; }
            if (op.Busy != null)
            {
                var o = op.Busy; a.Verb = o.Do; a.Target = o.Target; a.Phase = "acting";
                int span = Math.Max(1, op.BusyEnd - op.BusyStart);
                a.TicksLeft = Math.Max(0, op.BusyEnd - Tick); a.Progress = (int)Math.Min(1000L, (long)(Tick - op.BusyStart) * 1000 / span);
                a.OnDone = OnDone(op, o);
                a.CanCancel = true; (a.CancelCost, a.CancelText) = CancelLine(op, o.Do);
                if (o.Do == "enter" && o.How == "out") { a.CanCancel = false; a.CancelCost = "nothing"; a.CancelText = "both doors are held: he waits inside until one is free"; } // a cancelled Enter, waiting
                return a;
            }
            if (op.InCar) // brief 018: boarded; a move steps out (an order, not a cancel)
            {
                a.Verb = "board"; a.Target = "car"; a.Phase = "holding"; a.TicksLeft = -1; a.OnDone = "leaves with the car on Go";
                a.CanCancel = false; a.CancelCost = "nothing"; a.CancelText = "in the car: a move steps out";
                return a;
            }
            if (op.Walk != null)
            {
                a.Verb = "move"; a.Phase = op.Running ? "running" : "walking"; a.Target = $"{op.Walk.End.X},{op.Walk.End.Y}";
                int speed = op.WalkSpeed > 0 ? op.WalkSpeed : opSpeed; long left = op.Walk.Length - op.WalkMm;
                a.TicksLeft = (int)((left + speed - 1) / speed); a.Progress = op.Walk.Length == 0 ? 1000 : (int)(op.WalkMm * 1000 / op.Walk.Length);
                a.OnDone = "arrives";
                a.CanCancel = true; a.CancelCost = "stop"; a.CancelText = "stops where they stand";
                if (g.Order.Length > 0) a.OnDone = $"arrives (firing on {g.Order} as they walk)";
                return a;
            }
            if (g.Order.Length > 0)
            {
                a.Verb = "fire"; a.Target = g.Order; a.Phase = "firing"; a.Progress = 0;
                a.TicksLeft = Math.Max(0, Math.Max(g.NextShot, g.DrawReady) - Tick); a.OnDone = $"{g.Order} down";
                a.CanCancel = true; a.CancelCost = "cease"; a.CancelText = "ceases fire, the weapon stays drawn";
                return a;
            }
            if (g.Drawn && g.HolsterAt < 0 && Tick < g.DrawReady)
            {
                a.Verb = "draw"; a.Phase = "drawing"; a.TicksLeft = g.DrawReady - Tick;
                a.Progress = (int)Math.Min(1000L, (long)(Tick - g.DrawTick) * 1000 / Math.Max(1, g.DrawReady - g.DrawTick)); a.OnDone = "weapon drawn";
                a.CanCancel = true; a.CancelCost = "holster"; a.CancelText = "the weapon goes back (a draw already seen stays seen)";
                return a;
            }
            if (g.HolsterAt >= 0) { a.Verb = "holster"; a.Phase = "acting"; a.TicksLeft = Math.Max(0, g.HolsterAt - Tick); a.OnDone = "weapon away"; a.CancelCost = "none"; a.CancelText = "holstering finishes in a moment"; return a; }
            if (id == OpId.Tech && LoopOn)
            {
                a.Verb = "loop"; a.Target = LoopCam; a.Phase = "holding"; a.TicksLeft = -1; a.OnDone = "holds while the Tech stays at the cabinet";
                a.CanCancel = true; a.CancelCost = "ends"; a.CancelText = "the loop ends and the camera comes back";
                return a;
            }
            if (op.CarryBody.Length > 0) // brief 013: a body carried
            {
                a.Verb = "carry"; a.Target = op.CarryBody; a.Phase = "holding"; a.TicksLeft = -1; a.OnDone = "a stash hides it; open ground does not";
                a.CanCancel = true; a.CancelCost = "put-down"; a.CancelText = "the body is laid down here";
                return a;
            }
            if (op.Carrying.HasValue)
            {
                a.Verb = "carry"; a.Target = Sim.Op.Name(op.Carrying.Value); a.Phase = "holding"; a.TicksLeft = -1; a.OnDone = "brought to the car, they escape";
                a.CanCancel = true; a.CancelCost = "put-down"; a.CancelText = $"the {a.Target} is laid down here";
                return a;
            }
            if (op.Driven) // brief 025 (EXPERIMENTAL): the host moves it; a release ends that, not a cancel
            {
                a.Verb = "drive"; a.Phase = op.Running ? "running" : "driven"; a.TicksLeft = -1; a.OnDone = "the player moves them";
                a.CanCancel = false; a.CancelCost = "nothing"; a.CancelText = "under the player's control: release to stop driving";
                return a;
            }
            if (id == OpId.Enforcer && Vale == ValeAt.WithEnforcer)
            {
                a.Verb = "take"; a.Target = "councillor"; a.Phase = "holding"; a.TicksLeft = -1; a.OnDone = "Vale boards the getaway car with him; the win is the car leaving with Vale (Go)";
                a.CanCancel = false; a.CancelCost = "keeps-vale"; a.CancelText = "putting Vale down is no cancel";
                return a;
            }
            a.CancelCost = "nothing"; a.CancelText = "nothing to cancel";
            return a;
        }

        string OnDone(Op op, Order o)
        {
            switch (o.Do)
            {
                case "watch":
                    {
                        if (o.Target == "guard") return "learns the routine and his sight";
                        if (IsPersonRoleName(o.Target)) return "learns the routine";
                        var k = Known(o.Target);
                        bool routine = k != null && !k.Routine && k.RoutineNeedsS > 0 && k.WatchedS + R.SightWatchS >= k.RoutineNeedsS;
                        return routine ? "learns the sight and the routine" : "learns the sight";
                    }
                case "listen": return "learns the need and temperament";
                case "turn": return op.BusyQuote?.Outcome ?? "?";
                case "loop": return o.Target == "entrance" ? "the entrance camera loops" : $"the {o.Target} camera loops";
                case "reroute": return "the patrol leaves its sweep";
                case "mislead": return "the rivals follow a false tip";
                case "enter": return o.How == "out" ? "out by a door no unit holds, without Vale" : "out at the service lane with Vale";
                case "take": return "Vale is with the Enforcer";
                case "carry": return IsOpId(o.Target) ? $"lifts the {o.Target}" : $"lifts {o.Target}";
                case "takedown": return o.How == "kill" ? "dead" : "out";
                case "drop": return "laid down";
                case "stash": return "hidden";
                case "restrain": return "tied";
                case "board": return op.Carrying.HasValue ? $"in the car with the {Sim.Op.Name(op.Carrying.Value)}" : op.Id == OpId.Enforcer && Vale == ValeAt.WithEnforcer ? "in the car with Vale" : "in the car";
                case "move": return "steps out of the car"; // only a boarded operative's move is an action
                default: return "";
            }
        }

        (string cost, string text) CancelLine(Op op, string verb)
        {
            switch (verb)
            {
                case "watch": case "listen": return ("lost", "the time is spent; nothing is learned");
                case "turn": return R.CancelTurn == "aborted" ? ("aborted", $"the money held comes back; the {op.Busy!.Target} remembers an aborted approach and the Fixer's face") : ("lost", "the money held comes back; nothing is remembered");
                case "loop": case "reroute": case "mislead": case "carry": case "drop": case "stash": case "restrain": return ("lost", "the time is spent; nothing lands");
                case "takedown": return ("released", "the victim is let go: he saw it, and reports it as a woken victim would");
                case "enter": return R.CancelEnter == "staff-report" ? ("staff-report", $"back out {CancelledEnterOut()} without Vale; the staff report the intruder (a reported incident)") : ("quiet", $"back out {CancelledEnterOut()} without Vale");
                case "take": return R.CancelTake == "vale-reports" ? ("vale-reports", "Vale is let go at his car and reports the attempt (a reported incident)") : ("quiet", "Vale is let go at his car");
                default: return ("lost", "nothing lands");
            }
        }

        /// <summary>The cancel order (rulebook cancel): stops the activity ActivityOf shows, at the cost its line names.</summary>
        string Cancel(Op op)
        {
            var a = ActivityOf(op.Id); string name = Sim.Op.Name(op.Id);
            if (!a.CanCancel) return No(a.CancelCost == "down" ? "agent-down" : a.CancelCost == "keeps-vale" ? "keeps-vale" : a.CancelCost == "none" ? "holstering" : "nothing-to-cancel", a.CancelText);
            switch (a.Phase)
            {
                case "acting":
                    {
                        var o = op.Busy!; long paid = op.BusyPaid; RoundHoldCut(op);
                        op.Busy = null; op.BusyQuote = null; op.BusyPaid = 0; op.BusyEnd = Tick;
                        switch (o.Do)
                        {
                            case "turn":
                                Cash += paid; // held at the order, paid only in the outcome's charge (Redline E2): it comes back
                                if (R.CancelTurn == "aborted") Remember(PersonByRole(o.Target), "aborted", "fixer", false);
                                break;
                            case "enter":
                                EntryInProgress = false;
                                ExitWithoutVale(op, true);
                                if (R.CancelEnter == "staff-report") ReportBy("office-staff", S.Stand(S.GuardPost));
                                break;
                            case "takedown": // brief 013 (rulebook cancel.takedown released): let go, he saw the Enforcer and reports it
                                if (BodyById(o.Target) is Body tb && tb.Seizing) { var ta = ActOf(tb); ta.Aware = true; tb.Seizing = false; Remember(tb.P, "attacked-aware", "enforcer", false); Wake(tb, false); }
                                break;
                            case "take":
                                Vale = ValeAt.Pickup;
                                if (R.CancelTake == "vale-reports")
                                {
                                    // a Take already seen is an incident: Vale's own report is one more report of it (one incident counts once
                                    // toward Alert, brief 013 D), a victim's report as a woken victim's; unseen, it is the reported incident it was
                                    var ta = Acts.Find(x => x.Take);
                                    if (ta != null) LandAct(ta, "woken", "councillor", ValeP, ValePos() ?? op.Pos, SevOf("woken"), "");
                                    else ReportBy("councillor", ValePos() ?? op.Pos);
                                    Remember(ValeP, "aborted", "enforcer", false); // a lead for the investigators (Motive addendum Q4)
                                }
                                break;
                        }
                        Note("cancel." + o.Do, $"{name} stops: {o.Do}{(o.Target.Length > 0 ? " " + o.Target : "")} ({a.CancelCost}: {a.CancelText}).");
                        return "";
                    }
                case "walking": case "running":
                    op.Walk = null; op.WalkMm = 0; op.Running = false;
                    Note("cancel.move", $"{name} stops where they stand.");
                    return "";
                case "firing":
                    op.Gun.Order = ""; op.Gun.Force = false; op.Gun.Target = ""; op.Gun.LostAt = -1;
                    Note("cancel.fire", $"{name} ceases fire.");
                    return "";
                case "drawing":
                    op.Gun.Drawn = false; op.Gun.Target = "";
                    Note("cancel.draw", $"{name} puts the weapon back.");
                    return "";
                case "holding":
                    if (a.Verb == "loop") { LoopOn = false; Note("loop.ends", "The Tech lets the loop go. The camera feed returns."); return ""; }
                    if (a.Verb == "carry" && op.CarryBody.Length > 0) { PutDown(op, "cancel"); return ""; }
                    if (a.Verb == "carry") { var c = OpOf(op.Carrying!.Value); c.CarriedBy = null; op.Carrying = null; Note("cancel.carry", $"{name} lays the {Sim.Op.Name(c.Id)} down."); return ""; }
                    break;
            }
            return No("nothing-to-cancel", "nothing to cancel");
        }

        /// <summary>A cancelled Enter comes out without Vale (rev 4 R4.1, rulebook response: no exit through a held door; Aster 2026-10-16,
        /// brief 013 E3): by the public door unless a unit holds it, else by the service door; both held, he waits inside (an Enter that
        /// cannot be cancelled) and comes out the tick one is free. Re-tried each tick from Land.</summary>
        internal void ExitWithoutVale(Op op, bool first)
        {
            bool frontHeld = DoorHeld(EnterDoor), serviceHeld = DoorHeld(ExitDoor);
            if (frontHeld && serviceHeld)
            {
                op.Inside = true; op.Busy = new Order { Tick = Tick, Op = Sim.Op.Name(op.Id), Do = "enter", How = "out" }; op.BusyEnd = Tick + 1;
                if (first) { op.BusyStart = Tick; Note("cancel.enter-waits", "Both doors are held: the Enforcer waits inside, without Vale."); }
                return;
            }
            op.Inside = false; op.Pos = frontHeld ? S.Stand(S.ValeService) : S.Stand(S.GuardPost);
            if (frontHeld || !first) Note("cancel.enter-out", frontHeld ? "The public door is held: the Enforcer comes out at the service lane without Vale." : "A door is free: the Enforcer comes out at the public door without Vale.");
        }
        /// <summary>Where a cancelled Enter would come out now, in the engine's words (the cancel line).</summary>
        string CancelledEnterOut() => !DoorHeld(EnterDoor) ? "at the public door" : !DoorHeld(ExitDoor) ? "at the service lane (the public door is held)" : "when a door is free (both are held: he waits inside)";

        /// <summary>A reported incident by someone who is not a contact (rulebook alert.byIncident): the Office staff, or Vale.</summary>
        void ReportBy(string who, Pt at)
        {
            Reported++;
            var list = R.AlertByIncident;
            RaiseAlert(list[Math.Min(Reported, list.Length) - 1], "report:" + who);
            AddIncident("report", "report:" + who, at);
            Note("report", $"The {who.Replace('-', ' ')} report{(who == "councillor" ? "s" : "")} the attempt. Alert {Alert}.");
        }
    }
}
