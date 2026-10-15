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
                a.Verb = "loop"; a.Target = "entrance"; a.Phase = "holding"; a.TicksLeft = -1; a.OnDone = "holds while the Tech stays at the cabinet";
                a.CanCancel = true; a.CancelCost = "ends"; a.CancelText = "the loop ends and the camera comes back";
                return a;
            }
            if (op.Carrying.HasValue)
            {
                a.Verb = "carry"; a.Target = Sim.Op.Name(op.Carrying.Value); a.Phase = "holding"; a.TicksLeft = -1; a.OnDone = "brought to the car, they escape";
                a.CanCancel = true; a.CancelCost = "put-down"; a.CancelText = $"the {a.Target} is laid down here";
                return a;
            }
            if (id == OpId.Enforcer && Vale == ValeAt.WithEnforcer)
            {
                a.Verb = "take"; a.Target = "councillor"; a.Phase = "holding"; a.TicksLeft = -1; a.OnDone = "Vale in the getaway car: the mission is won";
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
                case "watch": return "learns the routine";
                case "listen": return "learns the need and temperament";
                case "turn": return op.BusyQuote?.Outcome ?? "?";
                case "loop": return "the entrance camera loops";
                case "reroute": return "the patrol leaves its sweep";
                case "mislead": return "the rivals follow a false tip";
                case "enter": return "out at the service lane with Vale";
                case "take": return "Vale is with the Enforcer";
                case "carry": return $"lifts the {o.Target}";
                default: return "";
            }
        }

        (string cost, string text) CancelLine(Op op, string verb)
        {
            switch (verb)
            {
                case "watch": case "listen": return ("lost", "the time is spent; nothing is learned");
                case "turn": return R.CancelTurn == "aborted" ? ("aborted", $"the money held comes back; the {op.Busy!.Target} remembers an aborted approach and the Fixer's face") : ("lost", "the money held comes back; nothing is remembered");
                case "loop": case "reroute": case "mislead": case "carry": return ("lost", "the time is spent; nothing lands");
                case "enter": return R.CancelEnter == "staff-report" ? ("staff-report", "back out at the public door without Vale; the staff report the intruder (a reported incident)") : ("quiet", "back out at the public door without Vale");
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
                        var o = op.Busy!; long paid = op.BusyPaid;
                        op.Busy = null; op.BusyQuote = null; op.BusyPaid = 0; op.BusyEnd = Tick;
                        switch (o.Do)
                        {
                            case "turn":
                                Cash += paid; // held at the order, paid only in the outcome's charge (Redline E2): it comes back
                                if (R.CancelTurn == "aborted") Remember(PersonByRole(o.Target), "aborted", "fixer", false);
                                break;
                            case "enter":
                                op.Inside = false; op.Pos = S.Stand(S.GuardPost); EntryInProgress = false;
                                if (R.CancelEnter == "staff-report") ReportBy("office-staff", S.Stand(S.GuardPost));
                                break;
                            case "take":
                                Vale = ValeAt.Pickup;
                                if (R.CancelTake == "vale-reports") { ReportBy("councillor", ValePos() ?? op.Pos); Remember(ValeP, "aborted", "enforcer", false); } // a lead for the investigators (Motive addendum Q4)
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
                    if (a.Verb == "carry") { var c = OpOf(op.Carrying!.Value); c.CarriedBy = null; op.Carrying = null; Note("cancel.carry", $"{name} lays the {Sim.Op.Name(c.Id)} down."); return ""; }
                    break;
            }
            return No("nothing-to-cancel", "nothing to cancel");
        }

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
