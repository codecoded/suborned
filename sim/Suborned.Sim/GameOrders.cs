// Orders: each is checked when given (a rejected order changes nothing and is logged with its reason), takes its time, and lands at
// its end. Turn, Watch and Listen run from the rulebook's events (needs, price lines, outcomes, also, witnesses); the other verbs
// read their numbers from the rulebook's `cell` section and the mission's from the site. The Call is GameCall.cs's (brief 102).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>Give an order now (its tick must be the game's tick). Returns whether it was taken and, if not, a stable refusal code
        /// (Fable's `locks`, contracts/missions/handover/text.json), the engine's words for the log, and the numbers a line needs.
        /// Deconstructs as (ok, why) or (ok, code, why).</summary>
        public Answer Issue(Order o)
        {
            if (o.Tick != Tick) throw new InvalidOperationException($"order for tick {o.Tick} given at tick {Tick}");
            string why; refusal = default;
            try { why = Over ? No("mission-over", "the mission is over") : Check(o); }
            catch (Exception e) when (!(e is OutOfMemoryException)) { why = No("engine-error", "engine error: " + e.GetType().Name + ": " + e.Message); }
            bool ok = why.Length == 0;
            var a = AnswerOf(why);
            if (ok) EndWalkInFor(o); // engine 0.23.0: any other order to an operative walking in to a person act ends that walk in (GameApproach.cs)
            if (!ok && a.code == "uncoded") throw new InvalidOperationException($"a refusal without a code: {why}"); // every refusal says its code (Fable)
            Orders.Add((o, ok, a.code, why)); orderText.Add(o.ToString()); // engine 0.25.0 (brief 045): its canonical words, once, for the hash
            if (!ok) Note("order.rejected", $"{o.Op} {o.Do} {o.Target}{o.To}: {why}");
            return a;
        }
        public bool Do(Order o) => Issue(o).ok;

        (string? code, int m, int cm, long usd, int tick, string ids) refusal;
        /// <summary>A refusal: its stable code and its numbers (m, cm, usd, tick; -1 when it has none), the ids it names (car-held's holders),
        /// and the words for the log.</summary>
        string No(string code, string why, int m = -1, int cm = -1, long usd = -1, int tick = -1, string ids = "")
        {
            if (Array.IndexOf(Answer.Codes, code) < 0) throw new InvalidOperationException($"refusal code '{code}' is not in Answer.Codes");
            refusal = (code, m, cm, usd, tick, ids); return why;
        }
        /// <summary>The answer for these words: taken when empty, else the refusal No() recorded.</summary>
        Answer AnswerOf(string why) => why.Length == 0 ? new Answer { ok = true, code = "", why = "", m = -1, cm = -1, usd = -1, tick = -1, ids = "" }
            : new Answer { ok = false, code = refusal.code ?? "uncoded", why = why, m = refusal.m, cm = refusal.cm, usd = refusal.usd, tick = refusal.tick, ids = refusal.ids ?? "" };
        /// <summary>A rulebook need that does not hold, as its own code (Fable): the knowledge a Turn or Listen needs, the need it acts on,
        /// a refusal remembered, a contact or not, and cash for the worst case (with the worst case in usd).</summary>
        string NeedsNo(Cond c, PCtx x)
        {
            string why = $"needs {Describe(c)}";
            switch (c.K)
            {
                case "known": return No(c.S("what") == "need-told" ? "needs-need" : "needs-" + c.S("what"), why); // engine 0.44.0: Help needs the need told (needs-need, "Listen first")
                case "requires": { var req = R.ApproachRequires(c.S("approach")); return No(req == "need" ? "needs-need" : req == "temperament" ? "needs-temperament" : "needs-routine", why); }
                case "leverage": return No("no-leverage", why);
                case "need-met-by": return No("no-act-need", why);
                case "need-met": return No("need-met", why);
                case "memory": return No("refusal-remembered", why);
                case "turned": return No(c.B("is") ? "not-a-contact" : "already-turned", why);
                case "cash-covers": return No("no-cash", why, usd: Quote(x.Ev, x.P).WorstUsd);
                default: return No("needs-" + c.K, why);
            }
        }

        string Check(Order o)
        {
            if (o.Do == "spawn") return Range ? Spawn(o) : No("unknown-verb", "spawn is the fight range's (brief 029): unknown = reject"); // GameRange.cs
            if (Range && Array.IndexOf(RangeVerbs, o.Do) < 0) return No("unknown-verb", $"verb '{o.Do}' is not the fight range's (brief 029: {string.Join(", ", RangeVerbs)})");
            if (o.Do == "end" || (o.Do == "withdraw" && ValeOutTick != INF)) return EndOrder(); if (o.Do == "withdraw") { RecordsAtEnd(); Result = Outcome.Withdrawn; Withdrew(false); Note("withdraw", "The cell withdraws. The attempt ends."); return ""; }
            if (OneInputOn && (o.Do == "panic" || o.Do == "free" || o.Do == "cease" || o.Do == "stance")) return No("unknown-verb", $"'{o.Do}' is retired (weapons.oneInput, brief 039): F draws or holsters, a right hold focuses"); // unknown = reject
            if (o.Do == "panic") return Panic(o);
            if (o.Do == "follow") return FollowSet(o); // tuning.md §11.5 (engine 0.17.0): the follow setting, op "cell"
            if (o.Do == "free") return FreeSet(o); // tuning.md §14 (engine 0.19.0): weapons free, op "cell"
            if (o.Do == "go") return Go(o); // brief 018: anyone, from the car's card (GameGetaway.cs)
            var op = OpOf(Sim.Op.Parse(o.Op));
            if (op.Down) return No("agent-down", $"the {o.Op} is down");
            if (o.Do == "cancel") return Cancel(op); // before the inside and busy checks: cancel stops exactly those
            if (op.LeftMap) return No("agent-gone", $"the {o.Op} has left in a car"); if (op.Inside) return No("enforcer-inside", "the Enforcer is inside the Office");
            if (o.Do == "trigger") return Trigger(op, o); // tuning.md §11 (engine 0.17.0): before the busy check, so a release is always taken
            if (o.Do == "drive") return Drive(op, o); // brief 025 (EXPERIMENTAL): before the busy check, since moving a busy agent is the step's to judge (cell.drive.onBusy)
            if (o.Do == "drive-car") return DriveCar(op, o); // brief 059 K6: before the busy and in-car checks (it has its own: drive-busy, drive-no-driver)
            if (op.Busy != null) return No("agent-busy", $"{o.Op} is busy until {op.BusyEnd}", tick: op.BusyEnd);
            if (op.InCar) // brief 018: a move steps out first; a stance, holster or cease changes nothing on the street; the rest waits for stepping out
            {
                if (o.Do == "move") return StepOut(op, o);
                if (o.Do != "stance" && o.Do != "holster" && o.Do != "cease") return No("in-car", $"the {o.Op} is in the car: a move steps out first");
            }
            switch (o.Do)
            {
                case "move": return Move(op, o);
                case "watch": case "listen": return Scout(op, o);
                case "turn": return Turn(op, o);
                case "call": return Call(op, o);
                case "loop": return Loop(op, o);
                case "disable": return Disable(op, o); // brief 058 §2 (GameCameraLines.cs); unknown-verb without cell.disable
                case "reroute": return Reroute(op, o);
                case "mislead": return Mislead(op, o);
                case "fire": return Fire(op, o);
                case "volley": return Volley(op, o); // brief 027 (EXPERIMENTAL, fight.lab): target mode
                case "field": return Field(op, o);
                case "draw": return Draw(op, o);
                case "holster": return Holster(op, o);
                case "cease": return Cease(op, o);
                case "stance": return Stance(op, o);
                case "carry": return Carry(op, o);
                case "enter": return Enter(op, o);
                case "take": return Take(op, o);
                case "board": return Board(op, o); // brief 018 (GameGetaway.cs); extract is retired
                case "takedown": return Takedown(op, o);   // brief 013 (GameTakedown.cs)
                case "drop": return Drop(op, o);
                case "stash": return Stash(op, o);
                case "restrain": return Restrain(op, o);
                case "mark": case "talk-down": case "steam": case "covering-fire": return MoveOrder(op, o); // brief 039: one move each (GameCoverPin.cs)
                default: return No("unknown-verb", $"verb '{o.Do}' is unknown (unknown = reject)");
            }
        }

        void Engage(Op op, Order o, int seconds)
        {
            if (seconds < 1) seconds = 1; // the rulebook bounds every duration to 1 s or more at load (Redline E3); a 1 s floor is the belt

            op.Walk = null; op.WalkMm = 0; // engaging stops the approach walk
            if (op.Id == OpId.Enforcer) EndCoveringFire("an act"); // brief 039: an act ends it, as it ends a fire order
            if (op.Gun.Order.Length > 0) { Note("fire.cancelled", $"{Sim.Op.Name(op.Id)} stops firing on {op.Gun.Order} for the {o.Do}."); OrderEnd(op, false); } // engine 0.21.0 (Sky 2026-10-22, brief 041): a new act ends a lasting fire order (a walk or a run keeps it)
            op.Busy = o; op.BusyStart = Tick; op.BusyEnd = Tick + T(seconds);
            Note("order." + o.Do, $"{Sim.Op.Name(op.Id)} {o.Do}{(o.Target.Length > 0 ? " " + o.Target : "")} ({seconds} s)");
        }

        string Move(Op op, Order o)
        {
            Pt to;
            if (o.Point.HasValue) to = o.Point.Value;
            else if (S.HasPlace(o.To)) to = S.Stand(o.To);
            else return No("no-place", $"no place '{o.To}'");
            var walk = Walk; bool onCar = walk.CarCovers(to);
            if (onCar && walk.RouteBeside(op.Pos, to) is Pt[] rb) to = rb[rb.Length - 1]; // engine 0.33.1 (car.footprint): a move onto a standing car's ground ends beside it, on the side he comes to
            if (!walk.Free(to)) return No("unwalkable", "that ground can't be walked (a roof, a wall or a solid)");
            if (R.BodyCellEnds && !Range) to = ClearStop(Sim.Op.Name(op.Id), to); // brief 081 (engine 0.37.0; fight.spacing.cell): an agent sent where someone stands stops beside them, not in them
            if (o.Run) { var no = RunRefusal(op); if (no.Length > 0) return no; }
            var route = Walk.Route(op.Pos, to); if (route == null) return No("no-walk", "no walk there");
            if (op.Driven) EndDrive(op, false); // brief 025: a click move takes control back
            var snap = o.Point.HasValue && !onCar ? SnapPoint(op, to) : null; // brief 027 B5 (EXPERIMENTAL fight.lab.coverSnap): a click on the ground (never a named place: a mission point or a person's) ending by a free cover point ends on it; null with the lab off; engine 0.33.1: a click on a car goes to the car, never to a cover point by it
            if (snap != null) { var rs = Walk.Route(op.Pos, snap.At); if (rs != null) route = rs; else snap = null; }
            if (CoverSnapOn) { if (snap != null) HoldCover(op, snap); else DropCover(op); }
            op.Walk = new Path(route); op.WalkMm = 0; op.CloseWalk = false; op.PlacedAt = to; // brief 027: a click move is the player's walk, not a walk into range; engine 0.49.0: the place the player put him
            if (op.Walk.Length == 0) op.Walk = null;
            op.Running = o.Run; op.WalkSpeed = PaceOf(op, o.Run);
            if (o.Run) { op.RunAlerted = false; op.RunSeen.Clear(); }
            if (op.Gun.Order.Length > 0) op.CloseSpent = true; // tuning.md §8.2 rule 4 (engine 0.15.0): the fire order stands and fires on the move, walk or run; the player's move spends its walk-in
            Note("order.move", $"{Sim.Op.Name(op.Id)} {(o.Run ? "runs" : "heads")} for {(o.To.Length > 0 ? o.To : to.ToString())}{(snap != null ? $" and takes cover at {snap.Id} ({snap.Kind} {snap.Of})" : "")}.");
            return "";
        }

        /// <summary>A move's pace: walking (cell.speed) or running (cell.runSpeed). Who may not run is RunRefusal's (GameCombat.cs).</summary>
        int PaceOf(Op op, bool run) => run ? R.RunSpeed : op.CarryBody.Length > 0 && op.Id != OpId.Enforcer ? R.CarryUntrainedSpeed : cellWalk; // brief 013: only the Enforcer carries a body at a walk

        // ---------------------------------------------------------------- the person events (rulebook scout.*, turn.*)

        string Present(Person p, out Pt pos)
        {
            pos = default;
            var at = PersonPos(p);
            if (!at.HasValue) return p.Down ? No("person-down", $"{p.Role} is down") : No("person-away", $"{p.Role} is not out on the street");
            pos = at.Value; return "";
        }

        /// <summary>The rulebook event of a Watch or a Listen: cell.watch (brief 031: any agent) or scout.listen.</summary>
        public Event LearnEv(string verb) => R.Ev(verb == "watch" ? "cell.watch" : "scout." + verb);

        /// <summary>A Watch or a Listen; `engage` false checks it only (LearnQuery, the card before the click) and changes nothing. A Watch
        /// (brief 031, Sky 2026-10-20) is open to every operative, on a person, a camera, the patrol or a container, within the operative's own
        /// reach (sight.watch.reach) on a clear line; a Listen is the Scout's (only-scout). A watcher known on arrival (the response, the
        /// rivals) is nothing to watch for anyone (target-unknown).</summary>
        string Scout(Op op, Order o, bool engage = true)
        {
            if (IsCrewId(o.Target) && !Range) return CrewLearn(op, o, engage); // engine 0.41.0: a member of the rival crew (GameCrewTurn.cs)
            bool machine = o.Do == "watch" && !IsPersonRoleName(o.Target);
            if (o.Do == "listen" && op.Id != OpId.Scout) return No("only-scout", "only the Scout listens (§03)");
            if (machine && (o.Target == "rivals" || o.Target == "car:rivals")) return WatchRivals(op, o, engage); // brief 059 §2.2: the rival car's timetable
            if (machine && IsContainer(o.Target)) return WatchContainer(op, o, engage); // a container's round (§05; GameStashWatch.cs)
            if (machine) return WatchWatcher(op, o, engage); // a camera or the patrol (rulebook sight)
            if (!IsPersonRoleName(o.Target)) return No("target-unknown", $"'{o.Target}' is no one to listen to here");
            var p = PersonByRole(o.Target);
            if (op.Id == OpId.Tech && Tethered) return No("run-tethered", "the Tech is tethered to the cabinet while the loop holds"); // brief 031: his tether covers Watch (before engine 0.31.0: Loop v2 has no tether)
            if (o.Do == "watch" && R.WatchOffers && !ScoutTeaches(LearnEv(o.Do), p) && WatchRefusal(p) == "nothing-to-see") return No("nothing-to-see", $"watching {p.Role} would show nothing the cell can act on (brief 054 §2): no Watch to give");
            if (!ScoutTeaches(LearnEv(o.Do), p)) return o.Do == "listen" ? No("already-heard", $"the cell knows {p.Role}'s need and temperament already: a Listen teaches nothing more") : No("already-watched", $"the cell knows {p.Role}'s place and when already: a Watch teaches nothing more");
            var miss = Present(p, out var pos); if (miss.Length > 0) return miss;
            var ev = LearnEv(o.Do);
            int reach = o.Do == "watch" ? R.WatchReachCm(op.Id) : actReach; // brief 031: the Watch's reach is the agent's
            foreach (var c in ev.Needs) if (c.K == "reach") reach = (int)R.Num(c.S("m")) * 100;
            var x = Ctx(p, ev);
            // only asking (ScoutQuery, target mode; Latch 2026-10-18): a need the order lacks outranks its reach, since walking closer won't
            // cure it. Issue keeps its order (reach, then needs), so every recorded run replays as before.
            if (!engage) foreach (var c in ev.Needs) if (c.K != "reach" && !Holds(c, x)) return NeedsNo(c, x);
            if (!InPlace(op, o.Do, pos, reach))
            {
                // engine 0.23.0 (rulebook cell.approach, Sky 2026-10-22): out of reach (or, for a Watch, out of line) the order is taken and the
                // operative walks in; checked as if in place first, so only real refusals refuse. Off (or driven): as before, reach then line.
                if (!WalksIn(op))
                {
                    if (!G.Within(op.Pos, pos, reach)) return No("too-far", $"stand within {reach / 100} m of {p.Role}", m: reach / 100);
                    return No("no-clear-line", $"no clear line: move to where the {Sim.Op.Name(op.Id)} can see {p.Role}"); // brief 031: a person's Watch needs a clear line too
                }
                foreach (var c in ev.Needs) if (c.K != "reach" && !Holds(c, x)) return NeedsNo(c, x);
                return WalkIn(op, o, p, engage);
            }
            foreach (var c in ev.Needs) if (c.K != "reach" && !Holds(c, x)) return NeedsNo(c, x);
            if (!engage) return "";
            var q = Quote(ev, p);
            Engage(op, o, (int)q.S);
            op.BusyQuote = q; op.BusyPaid = 0;
            RoundHold(p, op, o.Do, op.BusyEnd); // brief 022: spoken to, a server stands still (Watch holds nothing: rounds.holds)
            return "";
        }

        /// <summary>Whether a Scout event on this person can still teach the cell anything (Sky 2026-10-18: "if I've listened to someone before,
        /// I think I can then listen to them again?"; a repeat that teaches nothing is refused, already-heard or already-watched, at no cost).
        /// Read from the rulebook, not listed here: every fact its outcomes and `also` rows learn of the subject, the temperament if the event is in preview.knownBy, and for the guard's Watch his sight (rulebook
        /// sight: his Watch teaches it too). A secret counts only for someone who has one. Knowledge forgotten opens the event again.</summary>
        bool ScoutTeaches(Event ev, Person p)
        {
            if (ev.Id == "cell.watch" && R.WatchOffers) return WatchTeaches(p); // brief 054 §2.1: a Watch teaches the mood, or security's sight and marks, or nothing
            bool Open(string what) => what == "secret" ? p.Secret.Length > 0 && !p.KSecret : what != "refused" && !p.Knows(what);
            bool Learns(List<Eff> changes) { foreach (var c in changes) if (c.Do == "learn" && c.S("of") == "subject") foreach (var w in c.L("what")) if (Open(w)) return true; return false; }
            foreach (var oc in ev.Outcomes) if (Learns(oc.Changes)) return true;
            foreach (var a in ev.Also) if (Learns(a.Changes)) return true; // brief 054 §9.2 (engine 0.29.0): no Watch row learns a secret; the lover-within exception went with it
            if (R.KnownBy.Contains(ev.Id) && !p.KTemperament) return true;
            if (ev.Id == "cell.watch" && p == GuardP && !(Known("guard")?.Sight ?? true)) return true;
            return false;
        }

        /// <summary>Engine 0.30.0 (the 0.2.116 bot row: Listen on Inez offered while the Scout was busy, then refused needs-routine three times): for a
        /// busy agent, a refusal the order itself carries (needs-routine, needs-need, already-heard, only-scout, nothing-to-see, …) outranks agent-busy,
        /// since waiting for the agent won't cure it; reach and line (too-far, no-clear-line, no-walk) do not, and then the answer is agent-busy.</summary>
        string BusyLearn(Op op, string agent, string verb, string target)
        {
            var why = Scout(op, new Order { Tick = Tick, Op = agent, Do = verb, Target = target }, engage: false);
            if (why.Length > 0 && !(refusal.code is "too-far" or "no-clear-line" or "no-walk")) return why;
            return No("agent-busy", $"{agent} is busy until {op.BusyEnd}", tick: op.BusyEnd);
        }

        /// <summary>The Scout's Watch or Listen row before the click: LearnQuery for the Scout (kept for its callers).</summary>
        public Answer ScoutQuery(string verb, string target) => LearnQuery("scout", verb, target);

        /// <summary>The Watch or Listen row before the click (Latch, as CallQuery), for any operative: would this agent's order on this target be
        /// taken now, and if not, its refusal code (only-scout, run-tethered, already-heard, already-watched, too-far, …), words and numbers,
        /// exactly as Issue would answer it; for a busy agent, the order's own refusal if it has one (engine 0.30.0: BusyLearn), else agent-busy. The Tech's Watch on a machine (Sky 2026-10-18) asks here with agent "tech". Read-only.</summary>
        public Answer LearnQuery(string agent, string verb, string target)
        {
            var saved = refusal; refusal = default; string why;
            try
            {
                var op = OpOf(Sim.Op.Parse(agent));
                why = Over ? No("mission-over", "the mission is over")
                    : verb != "watch" && verb != "listen" ? No("unknown-verb", $"verb '{verb}' is no Watch or Listen")
                    : op.Down ? No("agent-down", $"the {agent} is down")
                    : op.Inside ? No("enforcer-inside", "inside the Office")
                    : op.Busy != null ? BusyLearn(op, agent, verb, target)
                    : Scout(op, new Order { Tick = Tick, Op = agent, Do = verb, Target = target }, engage: false);
            }
            catch (Exception e) when (!(e is OutOfMemoryException)) { why = No("engine-error", "engine error: " + e.GetType().Name + ": " + e.Message); }
            var a = AnswerOf(why);
            refusal = saved;
            return a;
        }

        string Turn(Op op, Order o)
        {
            if (op.Id != OpId.Fixer) return No("only-fixer", "only the Fixer turns people (§03)");
            if (o.Approach.Length == 0) return No("turn-needs-approach", "turn needs an approach (ask, pay, help, pressure)");
            if (IsCrewId(o.Target) && !Range) return CrewTurn(op, o); // engine 0.41.0 (rivals.turn, GameCrewTurn.cs)
            var ev = R.Ev("turn." + o.Approach);
            if (!IsPersonRoleName(o.Target)) return No("target-unknown", $"'{o.Target}' is no one the cell can turn here"); // engine 0.30.0 (Suture's bot pass): refused, not thrown
            var p = PersonByRole(o.Target); var miss = Present(p, out var pos); if (miss.Length > 0) return miss;
            if (p == ValeP) return No("vale-not-contact", $"{Cap(ValeName)} is the target, not a contact");
            var x = Ctx(p, ev);
            x.Reach = G.Within(op.Pos, pos, approachReach);
            bool walk = !x.Reach && WalksIn(op); // engine 0.23.0 (rulebook cell.approach): out of reach, checked as if in reach, then walked in
            if (walk) x.Reach = true;
            foreach (var c in ev.Needs) if (!Holds(c, x)) return c.K == "reach" ? No("too-far", $"stand within {approachReach / 100} m of {p.Role}", m: approachReach / 100) : NeedsNo(c, x);
            var q = Quote(ev, p);
            int walkT = walk ? Math.Max(0, WalkInTicks(op, "turn", p)) : 0; // the driver's last chance counts the walk in
            if (p == Driver && Tick + walkT + T((int)q.S) > T(driverLastS)) return No("driver-too-late", "the driver can be turned only until 5:40 (§07 last chance to turn driver)", tick: T(driverLastS));
            if (walk) return WalkIn(op, o, p, true);
            Cash -= q.Usd; // the price is paid when the order is given; a refusal gives back exactly this (Redline E2)
            Engage(op, o, (int)q.S);
            op.BusyQuote = q; op.BusyPaid = q.Usd;
            RoundHold(p, op, o.Approach, op.BusyEnd); // brief 022: spoken to, a server stands still
            return "";
        }

        /// <summary>An order lands: its effects at the end of its time.</summary>
        void Land(Op op, Order o)
        {
            switch (o.Do)
            {
                case "watch" or "listen" or "turn" or "mislead" when IsCrewId(o.Target): LandCrew(op, o); break; // engine 0.41.0 (GameCrewTurn.cs)
                case "watch" when o.Target == "rivals" || o.Target == "car:rivals": RivalsWatched = true; Note($"{Sim.Op.Name(op.Id)}.watch.watched", $"The rivals' timetable is read: their next move, where each will go, and that their car follows {ValeName}'s."); break; // brief 059 §2.2
                case "watch" when IsContainer(o.Target): LandWatchContainer(op, o); break; // a container's round (GameStashWatch.cs)
                case "watch" when !IsPersonRoleName(o.Target): LandWatchWatcher(op, o); break; // a camera or the patrol (rulebook sight), the Scout's or the Tech's
                case "watch": case "listen":
                    LandPersonEvent(op, o, LearnEv(o.Do), PersonByRole(o.Target));
                    if (o.Do == "watch" && o.Target == "guard") WatchedWatcher("guard", R.SightWatchS); // the guard's Watch teaches his sight too
                    break;
                case "turn": LandPersonEvent(op, o, R.Ev("turn." + o.Approach), PersonByRole(o.Target)); break;
                case "loop": LandLoop(o); break; // GameCameraLines.cs (Loop v2, engine 0.31.0)
                case "disable": LandDisable(o); break; // brief 058 §2 (engine 0.31.0)
                case "reroute":
                    if (PatrolDown) break;
                    var rerouteFrom = PatrolPose()?.pos; // where it leaves from: its sweep, a chase or an alarm (engine 0.30.0)
                    PatrolAwayUntil = Tick + T(R.RerouteAwayS); PatrolChasing = false; PatrolTracking = false;
                    PlanRerouteWalk(rerouteFrom);
                    if (PatrolUnit() is Unit pu) { pu.St = "gone"; PatrolAlarmed = false; } // false orders override the alarm too
                    AddTrail(R.RerouteTrail, "reroute"); Actions.Add("reroute"); Chain.Add("patrol-rerouted");
                    Note("patrol.rerouted", "The patrol follows false orders and leaves its sweep."); break; // engine 0.34.0: the sweep is the yard's now (brief 067 A), so the words name no place
                case "drive-car": LandDriveCar(op); break; // brief 059 K6
                case "ride": break; // a passenger of the Drive: nothing of his own lands
                case "enter" when o.How == "out": ExitWithoutVale(op, false); break; // a cancelled Enter waiting inside for a free door
                case "enter":
                    {
                        // rev 4 R4.1: no exit through a held door. The service door held, out by the public door; both held, he waits inside
                        bool serviceHeld = DoorHeld(ExitDoor), frontHeld = serviceHeld && DoorHeld(EnterDoor);
                        if (frontHeld)
                        {
                            if (!Log.Exists(l => l.Tick == Tick - 1 && l.Code == "enter.in")) Note("enter.in", $"Both doors are held: the Enforcer waits inside with {ValeName}.");
                            op.Busy = o; op.BusyEnd = Tick + 1; break; // re-tried each tick until a door frees
                        }
                        op.Inside = false; op.Pos = serviceHeld ? S.Stand(S.GuardPost) : S.Stand(S.ValeService); EntryInProgress = false; Vale = ValeAt.WithEnforcer;
                        Note("enter.out", serviceHeld ? $"The service door is held: {ValeName} and the Enforcer come out at the public door." : $"{Cap(ValeName)} and the Enforcer come out at the service lane."); StartFight(); break;
                    }
                case "takedown": LandTakedown(op, o); break;
                case "board": if (LentTarget(o.Target)) LandBoardLent(op); else LandBoard(op); break;
                case "move": LandStepOut(op, o); break; // only a boarded operative's move is an action: the step out
                case "drop": case "stash": case "restrain": LandBodyOrder(op, o); break;
                case "carry" when !IsOpId(o.Target): LandBodyOrder(op, o); break;
                case "carry":
                    {
                        var other = OpOf(Sim.Op.Parse(o.Target));
                        if (!other.Down || other.CarriedBy.HasValue) { Note("carry.missed", $"There is nobody to lift at the {o.Target}'s place."); break; }
                        op.Carrying = other.Id; other.CarriedBy = op.Id;
                        Note("carry.lifted", $"The {Sim.Op.Name(op.Id)} lifts the {o.Target}. Walk them to the car.");
                        break;
                    }
                case "take": if (LendOn && TakeInCar) { Lend(op); StartFight(); break; } // engine 0.42.0 (car.lend, Motive L1): the in-car Take ends with the Enforcer at the wheel
                    Vale = ValeAt.WithEnforcer; op.Gun.Order = ""; op.Gun.Force = false; op.Gun.Target = ""; // brief 018: walking Vale, he doesn't fire
                    if (!Driver.Turned) AddTrail(R.TakeTrailUnturned, "take:unturned-driver");
                    Chain.Add("vale-taken-outside"); Note("take.done", $"{Cap(ValeName)} is with the Enforcer. Get to the car."); StartFight(); break;
            }
        }

        void LandPersonEvent(Op op, Order o, Event ev, Person p)
        {
            var x = Ctx(p, ev);
            var q = op.BusyQuote ?? Quote(ev, p); long paid = op.BusyPaid; // charge what the card said when the order was given
            op.BusyQuote = null; op.BusyPaid = 0;
            var outcome = FirstOutcome(ev, x);
            x.Outcome = outcome.Id;
            var changes = new List<Eff>(outcome.Changes);
            foreach (var a in ev.Also) if (AllHold(a.When, x)) changes.AddRange(a.Changes);
            var acts = new List<string>();
            if (!ev.Private) foreach (var w in ev.Witnesses) if (AllHold(w.When, x)) acts.Add(w.Act); // rulebook privacy: a private conversation has no onlookers
            bool witnessed = false;
            if (ev.Id == "cell.watch" && R.MoodOn) p.KMood = MoodNow(p); // brief 054 §2.3: the mood seen at the Watch's end
            witnessTrailOff = RecordTrailPaid(o); // brief 096 (camera): one act, one incident: a record that already charged its Trail
            foreach (var act in acts) witnessed |= WitnessAct(act, p, "fixer");
            witnessTrailOff = false; if (witnessed) ActWitnessed(o); // and a record still to land charges none
            bool charged = false;
            foreach (var c in changes) charged |= Apply(c, p, q, witnessed);
            if (paid > 0 && !charged) { Cash += paid; } // no deal: the money stays with the cell
            if (R.KnownBy.Contains(ev.Id)) p.Learn("temperament"); // rulebook preview.knownBy (Sky 2026-10-15: Listen)
            if (ev.Id.StartsWith("turn.", StringComparison.Ordinal))
            {
                Actions.Add($"{ev.Id}:{p.Role}");
                if (outcome.Id == "turned" && p == Inez) InezMethod = o.Approach;
            }
            // brief 031: a Watch logs as the agent's (scout.watch.watched, fixer.watch.watched, tech.watch.watched, enforcer.watch.watched)
            Note(ev.Id == "cell.watch" ? $"{Sim.Op.Name(op.Id)}.watch.{outcome.Id}" : $"{ev.Id}.{outcome.Id}", $"{p.Name} ({p.Role}): {ev.Id} → {outcome.Id}.");
        }

        /// <summary>One change from a rulebook outcome. Returns true if it charged money.</summary>
        bool Apply(Eff c, Person p, Quote q, bool witnessed)
        {
            switch (c.Do)
            {
                case "charge":
                    var parts = c.L("parts");
                    if (parts.Contains("trail")) AddTrail((int)q.Trail, $"{q.Event}:{p.Role}");
                    return parts.Contains("usd");
                case "turn": p.Turned = true; Chain.Add($"{p.Role}-turned"); return false; // brief 096 (engine 0.41.0): the driver's word to the lookout (F4) is retired
                case "move": LadderMove(p, c.S("on")); return false;
                case "need-met": p.NeedMet = true; return false;
                case "remember": Remember(p, c.S("act"), "fixer", witnessed); return false;
                case "learn":
                    if (c.S("of") != "subject") throw new FormatException($"learn of {c.S("of")} is not a mission effect");
                    foreach (var w in c.L("what")) { if (w == "refused") p.KnownRefused.Add(q.Event.Substring(5)); else p.Learn(w); }
                    return false;
                case "forget": foreach (var w in c.L("what")) p.Forget(w); return false;
                case "report": Report(p); return false;
                case "debrief": DebriefLines.Add((Tick, c.S("line"), p.Role, "")); return false;
                default: throw new FormatException($"effect '{c.Do}' is not a mission effect");
            }
        }

        /// <summary>A report (rulebook reports): an incident for Alert — the nth reported incident raises it to at least byIncident[n−1].</summary>
        void Report(Person p)
        {
            Reported++;
            var list = R.AlertByIncident;
            int at = list[Math.Min(Reported, list.Length) - 1];
            RaiseAlert(at, $"report:{p.Role}");
            AddIncident("report", "report:" + p.Role, PersonPos(p) ?? S.Stand(S.GuardPost), new[] { (p, PersonPos(p) ?? S.Stand(S.GuardPost)) });
            Note("report", $"{p.Name} reports the approach. Alert {Alert}.");
        }

        /// <summary>rulebook witness.sees: anyone (but the subject and the cell) within witness range on a clear sightline sees a traceable act:
        /// one incident adds witness Trail and moves standing once; simulated witnesses remember it.</summary>
        /// <summary>Brief 054 §3.2: the witness.notice act a traceable act is noticed as: a threat (Pressure) as threat; a payment or a debt
        /// cleared in sight as the approach it comes from (pay, help: never, below every Alert).</summary>
        static string NoticeActOf(string traceable) => traceable switch { "threat" => "threat", "payment" => "pay", "debt-cleared" => "help", "weapon-drawn" => "weapon-drawn", _ => throw new InvalidOperationException($"witness.sees: '{traceable}' has no witness.notice act (unknown = reject)") };
        bool WitnessAct(string act, Person subject, string by)
        {
            var at = PersonPos(subject); if (!at.HasValue) return false;
            var seenAt = WitnessesWithPos(at.Value, subject, NoticeActOf(act));
            var seen = seenAt.ConvertAll(x => x.p);
            if (seen.Count == 0) return false;
            var inc = AddIncident(act, act + ":" + subject.Role, at.Value, seenAt);
            inc.Witnesses.Add((subject, at.Value)); // the person the act was done to stood at it: the nearest to question (§05 sightline + distance)
            if (incidents.Add($"{act}:{subject.Role}:{Tick}"))
            {
                AddTrail(witnessTrailOff ? 0 : R.WitnessTrail, $"witnessed:{act}:{subject.Role}");
                MoveStanding(R.Traceable[act] * R.StandingStep, $"witnessed:{act}", "seen-" + act, subject.Role.Length > 0 ? subject.Role : subject.Id);
            }
            foreach (var w in seen) Remember(w, "saw-act", by, true);
            Note("witness." + act, $"{seen.Count} saw the {act}.");
            return true;
        }

        // ---------------------------------------------------------------- the rulebook's conditions, the action card

        sealed class PCtx { public Person P = null!; public Event Ev = null!; public Band Band; public string? Outcome; public bool Reach = true; public long Cash; public string Mood = "neutral"; }
        PCtx Ctx(Person p, Event ev) => new PCtx { P = p, Ev = ev, Band = R.BandOf(Standing), Cash = Cash, Mood = MoodNow(p) }; // brief 054 §4: the fit reads the true mood
        bool AllHold(List<Cond> cs, PCtx x) { foreach (var c in cs) if (!Holds(c, x)) return false; return true; }
        Outcome_ FirstOutcome(Event ev, PCtx x) { foreach (var o in ev.Outcomes) if (AllHold(o.When, x)) return new Outcome_(o); throw new InvalidOperationException("no outcome"); }
        readonly struct Outcome_ { public readonly string Id; public readonly List<Eff> Changes; public Outcome_(ROutcome o) { Id = o.Id; Changes = o.Changes; } }

        static string Describe(Cond c) => c.K switch
        {
            "requires" => "the " + c.S("approach") + " approach's knowledge (Watch for routine, Listen for need)",
            "need-met-by" => "a need this approach acts on",
            "leverage" => "the need it acts on known, or their secret known (Listen)",
            "need-met" => "a need still unmet",
            "memory" => "no remembered refusal",
            "turned" => c.B("is") ? "a turned contact" : "someone not yet turned",
            "cash-covers" => "cash for the worst case",
            "known" => "the " + c.S("what") + " known",
            _ => c.K,
        };

        bool Holds(Cond c, PCtx x)
        {
            var p = x.P;
            switch (c.K)
            {
                case "tier": return c.S("is") == "simulated";
                case "turned": if (c.N.Has("of")) break; return p.Turned == c.B("is");
                case "known": return KnowsGate(p, c.S("what")) == (!c.N.Has("is") || c.B("is"));
                case "leverage": // Motive, motive-lovers.md §2.2: the need it acts on (known), or the person's secret known
                    {
                        var r = R.ApproachRequires(c.S("approach"));
                        return ((r == "nothing" || p.Knows(r)) && R.NeedMetBy(p.Need, c.S("approach"))) || p.Knows("secret");
                    }
                case "requires": { var r = R.ApproachRequires(c.S("approach")); return r == "nothing" || KnowsGate(p, r); }
                case "orders-base": { long n = OrdersBase(x.Ev, x); return (!c.N.Has("above") || n > R.Num(c.S("above"))) && (!c.N.Has("atMost") || n <= R.Num(c.S("atMost"))); }
                case "need-met-by": return R.NeedMetBy(p.Need, c.S("approach"));
                case "need-is": return c.L("in").Contains(p.Need);
                case "need-met": return p.NeedMet == c.B("is");
                case "reach": return x.Reach;
                case "cash-covers": return x.Cash >= Quote(x.Ev, p).WorstUsd;
                case "memory": if (c.N.Has("of") || c.N.Has("this")) break; return p.Has(c.S("act")) == c.B("has");
                case "fit": return c.L("is").Contains(FitId(R.FitWithMood(x.Ev.Id.Substring(5), p.Temperament, x.Band, x.Mood))); // brief 054 §4.2: the mood steps it
                case "temperament": return c.L("in").Contains(p.Temperament);
                case "standing": return c.L("band").Contains(Rules.BandId(x.Band));
                case "orders-reached": return (p.Count(R.HelpedAct) + 1 >= OrdersFor(x.Ev, x)) == c.B("is");
                case "reports": foreach (var rr in R.Reports) if (AllHold(rr, x)) return true; return false;
                case "reports-approach": return R.Temp(p.Temperament).ReportsApproach;
                case "outcome": return x.Outcome != null && c.L("is").Contains(x.Outcome);
                case "talker": return R.IsTalker(p.Sc) == c.B("is");
                case "lies":
                    {
                        bool lies = R.IsTalker(p.Sc) == R.LieTalker && p.Has(R.HelpedAct) == R.LieHelped && p.Sc[Rules.Bravery] >= R.LieBraveryAtLeast;
                        return lies == c.B("is");
                    }
                case "holds-fact": return (p.Memory.Count > 0) == c.B("is");
                case "scale":
                    {
                        int a = p.Sc[Rules.ScaleIndex(c.S("scale"))];
                        var than = c.N["than"]; long b = than.Kind == JKind.Str ? R.Num(than.Str()) : p.Sc[Rules.ScaleIndex(than["scale"].Str())];
                        switch (c.S("op")) { case "<": return a < b; case "<=": return a <= b; case "==": return a == b; case ">=": return a >= b; case ">": return a > b; }
                        throw new FormatException("op");
                    }
                case "trail": return Trail >= R.Num(c.S("atLeast"));
            }
            throw new FormatException($"condition '{c.K}' is not evaluated inside a mission");
        }
        static string FitId(Fit f) => f == Fit.Suits ? "suits" : f == Fit.Worse ? "worse" : f == Fit.Ready ? "ready" : "refused";
        static readonly string[] TemperamentConds = { "fit", "temperament", "reports", "reports-approach", "orders-reached" };
        static bool ReadsTemperament(List<Cond> cs) { foreach (var c in cs) if (Array.IndexOf(TemperamentConds, c.K) >= 0) return true; return false; }

        /// <summary>Brief 054 §4.2: a Help's orders from the lines that hold, before a ready step (lines that take orders away are left out).</summary>
        long OrdersBase(Event ev, PCtx x)
        {
            long n = 0; bool any = false;
            for (int i = 0; i < ev.Price.Count; i++)
            {
                var l = ev.Price[i];
                if (l.Orders != null && l.OrdersV < 0) continue;
                if (l.When.Exists(c => c.K == "orders-base")) continue;
                if (!AllHold(l.When, x)) continue;
                if (l.Orders != null) { n += l.OrdersV; any = true; } else if (i == 0) { n += 1; any = true; }
            }
            return any && n > 0 ? n : 1;
        }
        long OrdersFor(Event ev, PCtx x)
        {
            long n = 0; bool any = false;
            for (int i = 0; i < ev.Price.Count; i++)
            {
                var l = ev.Price[i];
                if (!AllHold(l.When, x)) continue;
                if (l.Orders != null) { n += l.OrdersV; any = true; } else if (i == 0) { n += 1; any = true; }
            }
            return any && n > 0 ? n : 1;
        }

        /// <summary>The action card for an event on a person (§02 full price, K8 line items, Sky Q3 "?").</summary>
        public Quote Quote(Event ev, Person p)
        {
            var q = new Quote();
            var x = Ctx(p, ev);
            for (int i = 0; i < ev.Price.Count; i++)
            {
                var l = ev.Price[i];
                bool truth = AllHold(l.When, x);
                string shown = truth ? "yes" : "no";
                if (!p.KTemperament && ReadsTemperament(l.When)) shown = Agree(p, t => AllHold(l.When, WithTemperament(x, t)) ? "yes" : "no");
                var line = new Quote.L { Line = l.Id, Usd = l.UsdV, S = l.SV, Trail = l.TrailV, Orders = l.Orders != null ? l.OrdersV : (i == 0 ? 1 : 0), Holds = shown };
                q.Lines.Add(line);
                if (truth) { q.Usd += line.Usd; q.S += line.S; q.Trail += line.Trail; }
                if (shown == "?" ? line.Usd > 0 : truth) q.WorstUsd += line.Usd; // brief 085: a "?" line that would lower the price does not lower the worst case
            }
            q.Orders = OrdersFor(ev, x);
            q.Outcome = p.KTemperament ? FirstOutcome(ev, x).Id : Agree(p, t => FirstOutcome(ev, WithTemperament(x, t)).Id);
            q.Event = ev.Id;
            if ((ev.Id.StartsWith("scout.", StringComparison.Ordinal) || ev.Id == "cell.watch") && !ScoutTeaches(ev, p)) q.Refused = ev.Id == "scout.listen" ? "already-heard" : WatchRefusal(p);
            QuoteMood(q, ev, p); QuoteAlone(q, ev, x); // brief 054 §4.3; engine 0.44.0: a line that stands alone (GameHelpOffer.cs) // the same test Issue refuses by
            q.Leverage = ev.Id == "turn.pressure" && p.Knows("secret");
            if (ev.Id.StartsWith("turn.", StringComparison.Ordinal) && p.KnownRefused.Contains(ev.Id.Substring(5))) q.Outcome = "refused"; // a refusal seen: ✗
            if (ev.Id.StartsWith("turn.", StringComparison.Ordinal))
            {
                // would they report it: the event's own report rows (its `also` with a report), so an exposure that silences a Loyal person reads "no"
                bool Rep(PCtx c) { foreach (var a in ev.Also) if (a.Changes.Exists(e => e.Do == "report") && AllHold(a.When, c)) return true; return false; }
                q.Reports = p.KTemperament ? (Rep(x) ? "yes" : "no") : Agree(p, t => Rep(WithTemperament(x, t)) ? "yes" : "no");
                var list = R.AlertByIncident;
                q.AlertIfReported = Math.Max(Alert, list[Math.Min(Reported + 1, list.Length) - 1]);
            }
            var at = PersonPos(p);
            if (at.HasValue && !ev.Private) // who would see it: the same reading as witness.sees (WitnessAct), crowd included; brief 054 §3.5: by the notice rule, with ids
            {
                string wa = ev.Witnesses.Count > 0 ? ev.Witnesses[0].Act : "threat";
                foreach (var (w, _) in WitnessesWithPos(at.Value, p, NoticeActOf(wa))) q.WitnessIds.Add(w.Id);
                q.Witnesses = q.WitnessIds.Count;
            }
            return q;
        }
        static readonly Cond ReportsCond = new Cond(JNode.Parse("{\"k\": \"reports\"}"));

        /// <summary>The action card for an operative's approach to a role: the price, the risks, and the walk to stand within reach.</summary>
        public Quote Preview(string approach, string role, OpId by = OpId.Fixer)
        {
            var p = PersonByRole(role); var q = Quote(R.Ev("turn." + approach), p);
            var at = PersonPos(p); var op = OpOf(by);
            if (at.HasValue && ServerOf(p) != null && !G.Within(op.Pos, at.Value, approachReach)) // brief 022: aim the walk at where her round will have her
            {
                var st = Walk.NearestFree(at.Value); var rt = st.HasValue ? Walk.Route(op.Pos, st.Value) : null;
                if (rt != null) { int k = (int)(new Path(rt).Length / Math.Max(1, cellWalk)); var ahead = PoseAhead(p, k); if (ahead.HasValue) at = ahead.Value.pos; }
            }
            if (at.HasValue && !G.Within(op.Pos, at.Value, approachReach))
            {
                var stand = Walk.NearestFree(at.Value); var route = stand.HasValue ? Walk.Route(op.Pos, stand.Value) : null;
                if (route != null) { long mm = Math.Max(0, new Path(route).Length - approachReach * 10L); q.WalkS = (int)((mm + cellWalk * Hz - 1) / (cellWalk * Hz)); }
            }
            // seen by (rulebook sight.unknown): where the operative stands for the act, its own place when already within reach, else the person's
            if (at.HasValue) q.Seen = SeenAt(G.Within(op.Pos, at.Value, approachReach) ? op.Pos : at.Value);
            QuoteRecord(q, approach, p, op, at.HasValue && G.Within(op.Pos, at.Value, approachReach) ? op.Pos : at, at); // brief 096 (camera): the camera mark (GameCameraRecord.cs)
            return q;
        }

        /// <summary>The dossier's face bit: this person holds the Fixer's face (rulebook memory.faceFacts; §03 the face witnesses remember).</summary>
        public bool HoldsFace(Person p) { foreach (var m in p.Memory) if (R.FaceFact.ContainsKey(m.By)) return true; return false; }

        PCtx WithTemperament(PCtx x, string t)
        {
            var q = new Person { Role = x.P.Role, Id = x.P.Id, Name = x.P.Name, Temperament = t, Need = x.P.Need, Turned = x.P.Turned, NeedMet = x.P.NeedMet, KRoutine = x.P.KRoutine, KNeed = x.P.KNeed, KTemperament = x.P.KTemperament, Place = x.P.Place, Secret = x.P.Secret, KSecret = x.P.KSecret, Lover = x.P.Lover, Record = x.P.Record, Size = x.P.Size, Holder = x.P.Holder, KMood = x.P.KMood };
            Array.Copy(x.P.Sc, q.Sc, 4); q.Memory.AddRange(x.P.Memory);
            // brief 054 §4.3: the card's "?" reads the mood the cell knows (its Watch's), never the true one
            return new PCtx { P = q, Ev = x.Ev, Band = x.Band, Outcome = x.Outcome, Reach = x.Reach, Cash = x.Cash, Mood = x.P.KMood.Length > 0 ? x.P.KMood : "neutral" };
        }
        /// <summary>The answer every temperament gives alike, else "?" (Motive's review, other findings 3).</summary>
        string Agree(Person p, Func<string, string> f)
        {
            string? first = null;
            foreach (var t in R.TemperamentIds) { var a = f(t); if (first == null) first = a; else if (a != first) return "?"; }
            return first ?? "?";
        }

        // ---------------------------------------------------------------- the other verbs

        /// <summary>A camera the cabinet loops (rulebook cell.loop.targets): a site camera named in the list, or any of the city's own
        /// cameras when the list holds "world". One loop at a time.</summary>
        bool LoopTarget(string id)
        {
            var cam = S.Cameras.Find(c => c.Id == id); if (cam == null) return false;
            return R.LoopTargets.Contains(id) || (cam.World && R.LoopTargets.Contains("world"));
        }
        string Reroute(Op op, Order o)
        {
            if (op.Id != OpId.Tech) return No("only-tech", "only the Tech reroutes patrols (§03)");
            if (PatrolDown) return No("patrol-down", "the patrol is down");
            if (PatrolAwayUntil > Tick) return No("patrol-away", "the patrol is already away", tick: PatrolAwayUntil);
            if (!G.Within(op.Pos, S.At("cabinet"), R.AtReachCm)) return No("stand-at-cabinet", "stand at the cabinet", cm: R.AtReachCm);
            Engage(op, o, R.RerouteS); return "";
        }
        string Mislead(Op op, Order o)
        {
            if (op.Id != OpId.Scout) return No("only-scout", "only the Scout misleads (§03)");
            return CrewMislead(op, o); // brief 096 (engine 0.41.0, GameCrewTurn.cs): a member on foot; the false tip on the car is retired
        }

        string Enter(Op op, Order o)
        {
            if (op.Id != OpId.Enforcer) return No("only-enforcer", "only the Enforcer enters (§03)");
            if (op.CarryBody.Length > 0) return No("already-carrying", "carrying a body");
            if (Alert >= 3 && DialNow.DoorsLock) return No("doors-locked", "lockdown: the Office doors are locked (brief 013, the dial)");
            if (DoorHeld(EnterDoor)) return No("door-held", "a unit holds the scene at this door: nobody goes in (rev 4: a found body)");
            if (Vale != ValeAt.Office || EntryInProgress) return No("vale-not-inside", $"{Cap(ValeName)} is not inside");
            if (!G.Within(op.Pos, S.At(S.GuardPost), R.AtReachCm)) return No("stand-at-door", "stand at the public door", cm: R.AtReachCm);
            op.Walk = null;
            if (GuardAtDoor) { Fail("entry-seen-guard", $"The guard is back at the door. {Cap(ValeName)} is warned.", () => With(Because("seen", true, Locate("guard", "saw", op.Pos)), "enforcer")); return ""; }
            if (CameraWatching("entrance", op.Pos)) { Fail("entry-seen-camera", $"The entrance camera reports the breach. {Cap(ValeName)} is locked down.", () => With(Because("seen", true, Locate("camera:entrance", "saw", op.Pos)), "enforcer")); return ""; }
            if (PatrolWatching(S.At(S.GuardPost)))
            {
                if (!R.EnterPatrolReports) { Fail("entry-seen-patrol", $"The patrol reports the breach. {Cap(ValeName)} is locked down.", () => With(Because("seen", true, Locate("patrol", "saw", S.At(S.GuardPost))), "enforcer")); return ""; }
                EntrySeenByPatrol(S.At(S.GuardPost)); // engine 0.34.2 (Sky 2026-10-26: not a failure): a seen act, reported (GameEntrySeen.cs)
            }
            RouteKind = "front"; RouteGuard = BodyOf(GuardP) is Body gb ? (gb.Dead ? "killed" : "knocked-out") : GuardDown ? "shot" : "waitress"; RouteBot = SweepSeesEntrance(Tick) ? "rerouted" : SweepCoversEntrance ? "gap" : ""; // a takedown is no shooting (brief 013 E4); engine 0.34.0: no sweep at the door, no patrol in the front route (brief 067 A)
            EntryInProgress = true; op.Inside = true; Chain.Add("enforcer-reaches-vale");
            AtEntry = new RouteFacts { Lured = LureTick >= 0 && !GuardHeld, LuredBy = LureCalled, Looped = LoopOn && LoopCam == "entrance", PatrolAway = !PatrolDown && PatrolAwayUntil > Tick,
                PatrolSweeping = !PatrolDown && PatrolAwayUntil <= Tick && !PatrolChasing && PatrolUnit() == null }; // engine 0.30.0 (brief 060 #8): the debrief's route lines, output only
            bool alarm = Alert >= 2; // Sky 2026-10-15 Q5 (rulebook cell.enter.alarm): once Alert ≥ 2 the Office staff hide Vale
            op.Gun.Order = ""; op.Gun.Target = "";
            Engage(op, o, R.EnterS + (alarm ? R.EnterAlarmS : 0));
            Note("enter.in", alarm ? $"The Enforcer is inside. The alarm is up: the staff are hiding {ValeName}. Watch the service lane." : "The Enforcer is inside. Watch the service lane.");
            return "";
        }
        string Take(Op op, Order o)
        {
            if (op.Id != OpId.Enforcer) return No("only-enforcer", $"only the Enforcer takes {ValeName} (§03)");
            if (op.CarryBody.Length > 0) return No("already-carrying", "carrying a body");
            // brief 059 K3: with his driver turned Vale is taken in his car round the back, never at the front (driver-brings-vale) and not while
            // it drives round (car-not-parked)
            if (Vale == ValeAt.InCar && !PickupService && valeDriveStart >= 0) return No("car-not-parked", $"{Cap(ValeName)}'s car is driving round: take him once it parks");
            if (Vale == ValeAt.Pickup && Driver.Turned && !Driver.Down && !OfficialOut) return No("driver-brings-vale", $"the turned driver is bringing {ValeName} round the back: take him there");
            if (Vale != ValeAt.Pickup && !(Vale == ValeAt.InCar && PickupService)) return No("vale-not-at-car", $"{Cap(ValeName)} is not at his car");
            var vp = ValePos()!.Value;
            if (!G.Within(op.Pos, vp, actReach)) return No("stand-beside-vale", $"stand beside {ValeName}", m: actReach / 100);
            op.Walk = null;
            if (!PickupService && !TakeInCar && CameraWatching("pickup", op.Pos)) { Fail("abduction-seen-camera", $"The pickup camera reports the abduction. {Cap(ValeName)} is rescued.", () => With(Because("seen", true, Locate("camera:pickup", "saw", op.Pos), Locate("councillor", "victim")), "enforcer")); return ""; }
            if (!PickupService && FrontCrew(op) is string who) // the front: the crew watching the pickup reaches him first (brief 059: the lookout posted, or the car parked with its wheelman; a drawn member within his reach)
            { Fail("rivals-reach-vale", $"The rival crew is watching the pickup and reaches {ValeName} first.", () => With(Because("beat-to-pickup", true, Locate(who, "reached", vp), Locate("councillor", "victim")), "enforcer")); return ""; }
            RouteKind = PickupService ? "service" : "pickup"; RouteRival = RivalsArrived ? RivalsDelayMethod : "";
            AtTake = new RouteFacts { CameraBlocked = !PickupService && !PickupCameraLive() }; // engine 0.30.0 (brief 060 #8): the debrief's route lines, output only
            Vale = ValeAt.Taking;
            Engage(op, o, R.TakeS);
            TakeScan(); // a Take seen is a takedown seen (brief 013 D; rev 4: its struggle, unless inside his own car)
            return "";
        }
    }
}
