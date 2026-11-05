// Brief 096's misses round (engine 0.43.0, Keel; Latch's ask): the Pay and Mislead rows on a member of the rival crew before the click, read
// from the engine as Watch and Listen are (LearnQuery), so the host drops its copied refusal checks (HostCrewTurn.CrewLock). Read-only.
// With it, Motive's rivals page (briefs/096/motive/rivals.html §1, rivals.json pay.needs; Sky 2026-11-04 "all recommended"): Pay on a member
// needs a Watch first (rivals.turn.needs "watched": the crew's timetable read by a Watch on any member or their car, by any agent, as Pay on a
// person needs his routine), refused needs-routine before it.
using System;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>The Pay or Mislead row on a member before the click (Latch, as LearnQuery): would this agent's order be taken now, and if not its
        /// refusal code (only-fixer, only-scout, crew-gone, crew-fighting, crew-walking, person-away, refused-before, needs-routine, no-cash, too-far,
        /// no-walk, agent-busy, …), words and numbers, exactly as Issue would answer it. verb: "pay" (the Fixer's turn, approach pay) or "mislead".
        /// For a busy agent, the order's own refusal if it has one, else agent-busy (BusyLearn's rule). Out of reach and able to walk in, it is
        /// taken: the engine walks him in (cell.approach). Read-only.</summary>
        public Answer CrewActQuery(string agent, string verb, string target)
        {
            var saved = refusal; refusal = default; string why;
            try
            {
                var op = OpOf(Sim.Op.Parse(agent));
                var o = verb == "pay" ? new Order { Tick = Tick, Op = agent, Do = "turn", Target = target, Approach = "pay" } : new Order { Tick = Tick, Op = agent, Do = "mislead", Target = target };
                why = Over ? No("mission-over", "the mission is over")
                    : verb != "pay" && verb != "mislead" ? No("unknown-verb", $"verb '{verb}' is no Pay or Mislead")
                    : Range || !IsCrewId(target) ? No("target-unknown", $"'{target}' is none of the rival crew on foot")
                    : op.Down ? No("agent-down", $"the {agent} is down")
                    : op.LeftMap ? No("agent-gone", $"the {agent} has left in a car")
                    : op.Inside ? No("enforcer-inside", "the Enforcer is inside the Office")
                    : op.Busy != null ? BusyCrew(op, o)
                    : op.InCar ? No("in-car", $"the {agent} is in the car: a move steps out first")
                    : CrewAct(op, o, false);
            }
            catch (Exception e) when (!(e is OutOfMemoryException)) { why = No("engine-error", "engine error: " + e.GetType().Name + ": " + e.Message); }
            var a = AnswerOf(why); refusal = saved; return a;
        }
        /// <summary>Pay or Mislead on a member, given (engage) or asked: the order's own checks in Issue's order.</summary>
        string CrewAct(Op op, Order o, bool engage) => o.Do == "turn" ? (op.Id != OpId.Fixer ? No("only-fixer", "only the Fixer turns people (§03)") : CrewTurn(op, o, engage)) : (op.Id != OpId.Scout ? No("only-scout", "only the Scout misleads (§03)") : CrewMislead(op, o, engage));
        /// <summary>A busy agent's row: the order's own refusal outranks agent-busy (waiting won't cure it); reach and line do not.</summary>
        string BusyCrew(Op op, Order o)
        {
            var why = CrewAct(op, o, false);
            if (why.Length > 0 && !(refusal.code is "too-far" or "no-clear-line" or "no-walk")) return why;
            refusal = default;
            return No("agent-busy", $"{o.Op} is busy until {op.BusyEnd}", tick: op.BusyEnd);
        }
    }
}
