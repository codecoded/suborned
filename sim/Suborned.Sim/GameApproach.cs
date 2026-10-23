// The walk in (engine 0.23.0; rulebook cell.approach; Sky 2026-10-22 playing: "I can't listen to Inez a lot of the time with the Scout - I
// think we shouldn't block actions whilst Inez is moving about"). A person act (Watch on a person, Listen, Ask, Pay, Help, Pressure) given
// out of reach is taken, not refused too-far: checked as if given in reach, then the operative walks toward the person, aimed at where she
// will be (a server's round read ahead), re-aimed every cell.approach.repath s and whenever the walk ends. The act starts on the first tick
// the operative is in place (within the event's reach; a Watch also on a clear line), checked again exactly as an order given there; a
// refusal then drops it (nothing was paid or held). Any other order to the operative but a stance, draw, holster or cease ends the walk in.
// A driven operative is never walked: out of reach stays too-far (cell.drive). The walk in is a walk for every reader (sight, attention).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>A person act dropped on arrival (or on the way): the tick, the operative, the order, the refusal's code and words. The host's
        /// read (a pop on the agent); derived from the order log, never hashed.</summary>
        public readonly List<(int tick, OpId op, Order o, string code, string why)> Dropped = new List<(int, OpId, Order, string, string)>();

        /// <summary>Whether this operative walks in to a person act out of reach (the rulebook's cell.approach.walkIn, and not driven).</summary>
        bool WalksIn(Op op) => R.ApproachWalkIn && !op.Driven;

        /// <summary>Orders that leave a walk in standing (they change no walk and start no act): a stance, a draw, a holster, a cease, and the cell's settings.</summary>
        static bool KeepsWalkIn(string verb) => verb == "stance" || verb == "draw" || verb == "holster" || verb == "cease" || verb == "follow" || verb == "free" || verb == "trigger";

        /// <summary>The reach a person act on this operative's part is in place within (cm): a Watch the agent's own (sight.watch.reach), a Listen
        /// its event's (listen-reach-m), an approach approach-reach-m. The same numbers Scout and Turn read.</summary>
        int ActReachOf(Op op, string verb)
        {
            if (verb == "turn") return approachReach;
            var ev = LearnEv(verb); int reach = verb == "watch" ? R.WatchReachCm(op.Id) : actReach;
            foreach (var c in ev.Needs) if (c.K == "reach") reach = (int)R.Num(c.S("m")) * 100;
            return reach;
        }

        /// <summary>In place for a person act: within reach, and for a Watch on a clear line (walls and people), as Scout reads it.</summary>
        bool InPlace(Op op, string verb, Pt at, int reach) => G.Within(op.Pos, at, reach) && (verb != "watch" || (Sees(op.Pos, at) && !CrowdBlocks(op.Pos, at)));

        /// <summary>The walk in now and its ticks to the act's start: the grid route from the operative to where the person will be when the
        /// operative comes within `reach` of her (a server's round read ahead, a second at a time, up to two minutes: the first second t at
        /// which the walk to her pose at t, less the reach, is no longer than t); anyone else where they stand. Null: no walk (or off the street).</summary>
        (Path path, int ticks)? Intercept(Op op, Person p, int reach)
        {
            var at = PersonPos(p); if (!at.HasValue) return null;
            int speed = Math.Max(1, PaceOf(op, false));
            (Path, int)? To(Pt where)
            {
                var st = S.Grid.NearestFree(where); var rt = st.HasValue ? S.Grid.Route(op.Pos, st.Value) : null; if (rt == null) return null;
                var path = new Path(rt); long mm = Math.Max(0, path.Length - reach * 10L); return (path, (int)((mm + speed - 1) / speed));
            }
            var now = To(at.Value); if (ServerOf(p) == null || now == null) return now;
            (Path, int)? last = now;
            for (int t = Hz; t <= T(120); t += Hz)
            {
                var ahead = PoseAhead(p, t); if (!ahead.HasValue) break;
                var w = To(ahead.Value.pos); if (w == null) continue;
                last = w;
                if (w.Value.Item2 <= t) return (w.Value.Item1, Math.Max(w.Value.Item2, t - Hz));
            }
            return last;
        }
        /// <summary>Whether Intercept finds a walk (non-null), without the walk: it is null exactly when the person is off the street or the grid
        /// has no route from the operative to where she stands now (a server's round read ahead only chooses among walks once that one exists).
        /// One route instead of up to 120 (brief 045 F5: the host's gates ask Listen three times a frame).</summary>
        bool CanWalkIn(Op op, Person p)
        {
            var at = PersonPos(p); if (!at.HasValue) return false;
            var st = S.Grid.NearestFree(at.Value); return st.HasValue && S.Grid.Route(op.Pos, st.Value) != null;
        }
        Path? WalkInRoute(Op op, Person p, string verb) => Intercept(op, p, ActReachOf(op, verb))?.path;

        /// <summary>Ticks this operative walks before the act starts, as the card shows it (the intercept's walk less the act's reach, at a walk):
        /// 0 in place, -1 no walk. A best estimate from the person's known routine; the walk itself re-aims as she moves. A Watch may walk on
        /// for its clear line.</summary>
        int WalkInTicks(Op op, string verb, Person p)
        {
            var at = PersonPos(p); if (!at.HasValue) return -1;
            int reach = ActReachOf(op, verb);
            if (InPlace(op, verb, at.Value, reach)) return 0;
            var w = Intercept(op, p, reach); return w == null ? -1 : w.Value.ticks;
        }

        /// <summary>The card's walk for a person act, in whole seconds (engine 0.23.0): how long the agent walks before the act starts; 0 in place,
        /// -1 no walk (or the person off the street), and -1 for a driven agent (never walked: the order is too-far). Read-only. The card's time
        /// is this plus the act's own (Quote.S); Preview's WalkS for the approaches is the same reading.</summary>
        public int WalkInS(string agent, string verb, string role)
        {
            var op = OpOf(Sim.Op.Parse(agent)); if (!IsPersonRoleName(role)) return -1;
            var p = PersonByRole(role); var at = PersonPos(p); if (!at.HasValue) return -1;
            string v = verb == "ask" || verb == "pay" || verb == "help" || verb == "pressure" ? "turn" : verb;
            if (op.Driven && !InPlace(op, v, at.Value, ActReachOf(op, v))) return -1;
            int t = WalkInTicks(op, v, p);
            return t < 0 ? -1 : (t + Hz - 1) / Hz;
        }

        /// <summary>Take a person act out of reach: walk in (engage), or say it could be (a query). Refused no-walk when no route reaches the person.</summary>
        string WalkIn(Op op, Order o, Person p, bool engage)
        {
            if (!engage) return CanWalkIn(op, p) ? "" : No("no-walk", $"no walk to {p.Role}"); // engine 0.25.0 (brief 045 F5): a query needs only whether, not the walk
            var path = WalkInRoute(op, p, o.Do); if (path == null) return No("no-walk", $"no walk to {p.Role}");
            if (CoverSnapOn) DropCover(op); // brief 027 B5: walking off a cover point, as a click move
            op.Walk = path.Length > 0 ? path : null; op.WalkMm = 0; op.CloseWalk = false; op.Running = false; op.WalkSpeed = PaceOf(op, false);
            if (op.Gun.Order.Length > 0) op.CloseSpent = true; // a standing fire order fires on the walk (engine 0.21.0) and its walk-in is spent, as a move
            op.Act = o; op.ActNext = Tick + T(R.ApproachRepathS);
            Note("order.walk-in", $"{Sim.Op.Name(op.Id)} walks to {p.Role} to {(o.Do == "turn" ? o.Approach : o.Do)}.");
            return "";
        }

        /// <summary>Each tick, after the operative's step: a walk in starts its act once in place (checked again as an order given there), else
        /// re-aims at the person every repath s and when its walk ends. Dropped (with the refusal) if the act is refused then, or the person
        /// leaves the street, or the operative can no longer walk (down, inside, in the car, carried, driven).</summary>
        void ApproachStep(Op op)
        {
            var o = op.Act!; string name = Sim.Op.Name(op.Id);
            if (op.Down || op.Inside || op.InCar || op.Driven || op.CarriedBy.HasValue) { DropAct(op, o, op.Down ? "agent-down" : op.Inside ? "enforcer-inside" : op.InCar ? "in-car" : "agent-busy", "the walk in ends"); return; }
            if (op.Busy != null) return; // an act of its own started (none should: any act given ends the walk in); wait
            var p = PersonByRole(o.Target); var at = PersonPos(p);
            if (!at.HasValue) { DropAct(op, o, p.Down ? "person-down" : "person-away", p.Down ? $"{p.Role} is down" : $"{p.Role} is not out on the street"); return; }
            if (InPlace(op, o.Do, at.Value, ActReachOf(op, o.Do)))
            {
                op.Act = null; op.Walk = null; op.WalkMm = 0;
                var saved = refusal; refusal = default; string why;
                try { why = o.Do == "turn" ? Turn(op, o) : Scout(op, o); }
                catch (Exception e) when (!(e is OutOfMemoryException)) { why = No("engine-error", "engine error: " + e.GetType().Name + ": " + e.Message); }
                var code = refusal.code ?? "uncoded"; refusal = saved;
                if (why.Length > 0) { Dropped.Add((Tick, op.Id, o, code, why)); Note("order.dropped", $"{name} {(o.Do == "turn" ? o.Approach : o.Do)} {o.Target}: {code}: {why}"); }
                return;
            }
            if (op.Walk == null || Tick >= op.ActNext)
            {
                var path = WalkInRoute(op, p, o.Do);
                if (path == null) { DropAct(op, o, "no-walk", $"no walk to {p.Role}"); return; }
                op.Walk = path.Length > 0 ? path : null; op.WalkMm = 0; op.ActNext = Tick + T(R.ApproachRepathS);
            }
        }

        void DropAct(Op op, Order o, string code, string why)
        {
            op.Act = null; if (!op.Down && !op.Inside && !op.InCar && !op.Driven && !op.CarriedBy.HasValue) { op.Walk = null; op.WalkMm = 0; }
            Dropped.Add((Tick, op.Id, o, code, why));
            Note("order.dropped", $"{Sim.Op.Name(op.Id)} {(o.Do == "turn" ? o.Approach : o.Do)} {o.Target}: {code}: {why}");
        }

        /// <summary>After an order is taken: any order to an operative walking in, but one that keeps it (KeepsWalkIn) or the walk in itself, ends it.</summary>
        void EndWalkInFor(Order o)
        {
            if (o.Op != "scout" && o.Op != "fixer" && o.Op != "tech" && o.Op != "enforcer") return;
            var op = OpOf(Sim.Op.Parse(o.Op));
            if (op.Act == null || ReferenceEquals(op.Act, o) || KeepsWalkIn(o.Do)) return;
            Note("order.walk-in-ends", $"{o.Op}: the walk to {op.Act.Target} ends ({o.Do}).");
            op.Act = null;
        }
    }
}
