// Orders: each is checked when given (a rejected order changes nothing and is logged with its reason), takes its time, and lands at
// its end. Turn, Watch and Listen run from the rulebook's events (needs, price lines, outcomes, also, witnesses); the other verbs
// read their numbers from the rulebook's `cell` section and the mission's from the site.
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
            var a = ok ? new Answer { ok = true, code = "", why = "", m = -1, cm = -1, usd = -1, tick = -1 } : new Answer { ok = false, code = refusal.code ?? "uncoded", why = why, m = refusal.m, cm = refusal.cm, usd = refusal.usd, tick = refusal.tick };
            if (!ok && a.code == "uncoded") throw new InvalidOperationException($"a refusal without a code: {why}"); // every refusal says its code (Fable)
            Orders.Add((o, ok, a.code, why));
            if (!ok) Note("order.rejected", $"{o.Op} {o.Do} {o.Target}{o.To}: {why}");
            return a;
        }
        public bool Do(Order o) => Issue(o).ok;

        (string? code, int m, int cm, long usd, int tick) refusal;
        /// <summary>A refusal: its stable code and its numbers (m, cm, usd, tick; -1 when it has none), and the words for the log.</summary>
        string No(string code, string why, int m = -1, int cm = -1, long usd = -1, int tick = -1)
        {
            if (Array.IndexOf(Answer.Codes, code) < 0) throw new InvalidOperationException($"refusal code '{code}' is not in Answer.Codes");
            refusal = (code, m, cm, usd, tick); return why;
        }
        /// <summary>A rulebook need that does not hold, as its own code (Fable): the knowledge a Turn or Listen needs, the need it acts on,
        /// a refusal remembered, a contact or not, and cash for the worst case (with the worst case in usd).</summary>
        string NeedsNo(Cond c, PCtx x)
        {
            string why = $"needs {Describe(c)}";
            switch (c.K)
            {
                case "known": return No("needs-" + c.S("what"), why);
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
            if (o.Do == "withdraw") { Result = Outcome.Withdrawn; Note("withdraw", "The cell withdraws. The attempt ends."); return ""; }
            if (o.Do == "panic") return Panic(o);
            var op = OpOf(Sim.Op.Parse(o.Op));
            if (op.Down) return No("agent-down", $"the {o.Op} is down");
            if (o.Do == "cancel") return Cancel(op); // before the inside and busy checks: cancel stops exactly those
            if (op.Inside) return No("enforcer-inside", "the Enforcer is inside the Office");
            if (op.Busy != null) return No("agent-busy", $"{o.Op} is busy until {op.BusyEnd}", tick: op.BusyEnd);
            switch (o.Do)
            {
                case "move": return Move(op, o);
                case "watch": case "listen": return Scout(op, o);
                case "turn": return Turn(op, o);
                case "call": return Call(op, o);
                case "loop": return Loop(op, o);
                case "reroute": return Reroute(op, o);
                case "mislead": return Mislead(op, o);
                case "fire": return Fire(op, o);
                case "draw": return Draw(op, o);
                case "holster": return Holster(op, o);
                case "cease": return Cease(op, o);
                case "stance": return Stance(op, o);
                case "carry": return Carry(op, o);
                case "enter": return Enter(op, o);
                case "take": return Take(op, o);
                case "extract": return Extract(op, o);
                case "takedown": return Takedown(op, o);   // brief 013 (GameTakedown.cs)
                case "drop": return Drop(op, o);
                case "stash": return Stash(op, o);
                case "restrain": return Restrain(op, o);
                default: return No("unknown-verb", $"verb '{o.Do}' is unknown (unknown = reject)");
            }
        }

        void Engage(Op op, Order o, int seconds)
        {
            if (seconds < 1) seconds = 1; // the rulebook bounds every duration to 1 s or more at load (Redline E3); a 1 s floor is the belt

            op.Walk = null; op.WalkMm = 0; // engaging stops the approach walk
            op.Busy = o; op.BusyStart = Tick; op.BusyEnd = Tick + T(seconds);
            Note("order." + o.Do, $"{Sim.Op.Name(op.Id)} {o.Do}{(o.Target.Length > 0 ? " " + o.Target : "")} ({seconds} s)");
        }

        string Move(Op op, Order o)
        {
            Pt to;
            if (o.Point.HasValue) to = o.Point.Value;
            else if (S.HasPlace(o.To)) to = S.Stand(o.To);
            else return No("no-place", $"no place '{o.To}'");
            if (!S.Grid.Free(to)) return No("unwalkable", "that ground can't be walked (a roof, a wall or a solid)");
            if (o.Run) { var no = RunRefusal(op); if (no.Length > 0) return no; }
            var route = S.Grid.Route(op.Pos, to); if (route == null) return No("no-walk", "no walk there");
            op.Walk = new Path(route); op.WalkMm = 0;
            if (op.Walk.Length == 0) op.Walk = null;
            op.Running = o.Run; op.WalkSpeed = PaceOf(op, o.Run);
            if (o.Run) { op.RunAlerted = false; op.RunSeen.Clear(); if (op.Gun.Order.Length > 0) { op.Gun.Order = ""; op.Gun.Force = false; op.Gun.Target = ""; Note("fire.cancelled", $"{Sim.Op.Name(op.Id)} runs: the fire order ends."); } }
            Note("order.move", $"{Sim.Op.Name(op.Id)} {(o.Run ? "runs" : "heads")} for {(o.To.Length > 0 ? o.To : to.ToString())}.");
            return "";
        }

        /// <summary>A move's pace: walking (cell.speed) or running (cell.runSpeed). Who may not run is RunRefusal's (GameCombat.cs).</summary>
        int PaceOf(Op op, bool run) => run ? R.RunSpeed : op.CarryBody.Length > 0 && op.Id != OpId.Enforcer ? R.CarryUntrainedSpeed : opSpeed; // brief 013: only the Enforcer carries a body at a walk

        // ---------------------------------------------------------------- the person events (rulebook scout.*, turn.*)

        string Present(Person p, out Pt pos)
        {
            pos = default;
            var at = PersonPos(p);
            if (!at.HasValue) return p.Down ? No("person-down", $"{p.Role} is down") : No("person-away", $"{p.Role} is not out on the street");
            pos = at.Value; return "";
        }

        string Scout(Op op, Order o)
        {
            if (op.Id != OpId.Scout) return No("only-scout", "only the Scout watches and listens (§03)");
            if (o.Do == "watch" && !IsPersonRoleName(o.Target)) return WatchWatcher(op, o); // a camera or the patrol (rulebook sight)
            if (!IsPersonRoleName(o.Target)) return No("target-unknown", $"'{o.Target}' is no one to listen to here");
            var p = PersonByRole(o.Target); var miss = Present(p, out var pos); if (miss.Length > 0) return miss;
            var ev = R.Ev("scout." + o.Do);
            int reach = actReach;
            foreach (var c in ev.Needs) if (c.K == "reach") reach = (int)R.Num(c.S("m")) * 100;
            if (!G.Within(op.Pos, pos, reach)) return No("too-far", $"stand within {reach / 100} m of {p.Role}", m: reach / 100);
            var x = Ctx(p, ev);
            foreach (var c in ev.Needs) if (c.K != "reach" && !Holds(c, x)) return NeedsNo(c, x);
            var q = Quote(ev, p);
            Engage(op, o, (int)q.S);
            op.BusyQuote = q; op.BusyPaid = 0;
            return "";
        }

        string Turn(Op op, Order o)
        {
            if (op.Id != OpId.Fixer) return No("only-fixer", "only the Fixer turns people (§03)");
            if (o.Approach.Length == 0) return No("turn-needs-approach", "turn needs an approach (ask, pay, help, pressure)");
            var ev = R.Ev("turn." + o.Approach);
            var p = PersonByRole(o.Target); var miss = Present(p, out var pos); if (miss.Length > 0) return miss;
            if (p == ValeP) return No("vale-not-contact", "Vale is the target, not a contact");
            var x = Ctx(p, ev);
            x.Reach = G.Within(op.Pos, pos, approachReach);
            foreach (var c in ev.Needs) if (!Holds(c, x)) return c.K == "reach" ? No("too-far", $"stand within {approachReach / 100} m of {p.Role}", m: approachReach / 100) : NeedsNo(c, x);
            var q = Quote(ev, p);
            if (p == Driver && Tick + T((int)q.S) > T(driverLastS)) return No("driver-too-late", "the driver can be turned only until 5:40 (§07 last chance to turn driver)", tick: T(driverLastS));
            Cash -= q.Usd; // the price is paid when the order is given; a refusal gives back exactly this (Redline E2)
            Engage(op, o, (int)q.S);
            op.BusyQuote = q; op.BusyPaid = q.Usd;
            return "";
        }

        /// <summary>An order lands: its effects at the end of its time.</summary>
        void Land(Op op, Order o)
        {
            switch (o.Do)
            {
                case "watch" when !IsPersonRoleName(o.Target): LandWatchWatcher(o); break; // a camera or the patrol (rulebook sight)
                case "watch": case "listen":
                    LandPersonEvent(op, o, R.Ev("scout." + o.Do), PersonByRole(o.Target));
                    if (o.Do == "watch" && o.Target == "guard") WatchedWatcher("guard", R.SightWatchS); // the guard's Watch teaches his sight too
                    break;
                case "turn": LandPersonEvent(op, o, R.Ev("turn." + o.Approach), PersonByRole(o.Target)); break;
                case "loop":
                    LoopOn = true; LoopCam = o.Target; AddTrail(R.LoopTrail, "loop"); Actions.Add(o.Target == "entrance" ? "loop" : "loop:" + o.Target);
                    Note("loop.on", "One feed repeats. The Tech is tied to the cabinet."); break;
                case "reroute":
                    if (PatrolDown) break;
                    PatrolAwayUntil = Tick + T(R.RerouteAwayS); PatrolChasing = false; PatrolTracking = false;
                    if (PatrolUnit() is Unit pu) { pu.St = "gone"; PatrolAlarmed = false; } // false orders override the alarm too
                    AddTrail(R.RerouteTrail, "reroute"); Actions.Add("reroute"); Chain.Add("patrol-rerouted");
                    Note("patrol.rerouted", "The patrol follows false orders and leaves the entrance sweep."); break;
                case "mislead":
                    if (!RivalsPresent) { Note("mislead.missed", "The rivals are gone before the tip lands."); break; }
                    RivalsDelayedUntil = Tick + T(R.MisleadAwayS); RivalsDelayMethod = "mislead"; MisledOnce();
                    AddTrail(R.MisleadTrail, "mislead"); Actions.Add("mislead"); Chain.Add("rivals-false-tip");
                    Note("rivals.misled", "A false tip sends the rival crew away. They may return."); break;
                case "enter":
                    {
                        // rev 4 R4.1: no exit through a held door. The service door held, out by the public door; both held, he waits inside
                        bool serviceHeld = DoorHeld(ExitDoor), frontHeld = serviceHeld && DoorHeld(EnterDoor);
                        if (frontHeld)
                        {
                            if (!Log.Exists(l => l.Tick == Tick - 1 && l.Code == "enter.in")) Note("enter.in", "Both doors are held: the Enforcer waits inside with Vale.");
                            op.Busy = o; op.BusyEnd = Tick + 1; break; // re-tried each tick until a door frees
                        }
                        op.Inside = false; op.Pos = serviceHeld ? S.Stand(S.GuardPost) : S.Stand(S.ValeService); EntryInProgress = false; Vale = ValeAt.WithEnforcer;
                        Note("enter.out", serviceHeld ? "The service door is held: Vale and the Enforcer come out at the public door." : "Vale and the Enforcer come out at the service lane."); StartFight(); break;
                    }
                case "takedown": LandTakedown(op, o); break;
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
                case "take":
                    Vale = ValeAt.WithEnforcer;
                    if (!Driver.Turned) AddTrail(R.TakeTrailUnturned, "take:unturned-driver");
                    Chain.Add("vale-taken-outside"); Note("take.done", "Vale is with the Enforcer. Get to the car."); StartFight(); break;
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
            foreach (var w in ev.Witnesses) if (AllHold(w.When, x)) acts.Add(w.Act);
            bool witnessed = false;
            foreach (var act in acts) witnessed |= WitnessAct(act, p, "fixer");
            bool charged = false;
            foreach (var c in changes) charged |= Apply(c, p, q, witnessed);
            if (paid > 0 && !charged) { Cash += paid; } // no deal: the money stays with the cell
            if (R.KnownBy.Contains(ev.Id)) p.Learn("temperament"); // rulebook preview.knownBy (Sky 2026-10-15: Listen)
            if (ev.Id.StartsWith("turn.", StringComparison.Ordinal))
            {
                Actions.Add($"{ev.Id}:{p.Role}");
                if (outcome.Id == "turned" && p == Inez) InezMethod = o.Approach;
            }
            Note($"{ev.Id}.{outcome.Id}", $"{p.Name} ({p.Role}): {ev.Id} → {outcome.Id}.");
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
                case "turn": p.Turned = true; Chain.Add($"{p.Role}-turned"); return false;
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

        void LadderMove(Person p, string move)
        {
            foreach (var (k, by) in R.Ladders[move]) p.Sc[k] = R.Clamp(k, p.Sc[k] + by);
        }

        /// <summary>rulebook memory (K1): an entry on the subject; when full, the oldest sighting or told fact goes first (Motive's review #9).</summary>
        void Remember(Person p, string act, string by, bool witnessed)
        {
            p.Memory.Add(new Mem { Act = act, By = by, Place = p.Place, Tick = Tick, Witnessed = witnessed });
            if (p.Memory.Count <= R.MemoryCap) return;
            int drop = -1;
            for (int j = 0; j < p.Memory.Count; j++)
                if (Sighting(p.Memory[j].Act) && (drop < 0 || p.Memory[j].Tick < p.Memory[drop].Tick)) drop = j;
            if (drop < 0) for (int j = 0; j < p.Memory.Count; j++) if (drop < 0 || p.Memory[j].Tick < p.Memory[drop].Tick) drop = j;
            p.Memory.RemoveAt(drop);
        }

        /// <summary>rulebook memory.whenFull: what a person saw (or was told) goes before what was done to them.</summary>
        static bool Sighting(string act) => act == "saw-act" || act == "saw-runner" || act == "saw-gunfire" || act == "saw-violence" || act == "saw-weapon-drawn" || act == "told"
            || act == "saw-takedown" || act == "saw-carry" || act == "found-body" || act == "heard-alarm"; // brief 013's sightings

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
        bool WitnessAct(string act, Person subject, string by)
        {
            var at = PersonPos(subject); if (!at.HasValue) return false;
            var seenAt = WitnessesWithPos(at.Value, subject);
            var seen = seenAt.ConvertAll(x => x.p);
            if (seen.Count == 0) return false;
            var inc = AddIncident(act, act + ":" + subject.Role, at.Value, seenAt);
            inc.Witnesses.Add((subject, at.Value)); // the person the act was done to stood at it: the nearest to question (§05 sightline + distance)
            if (incidents.Add($"{act}:{subject.Role}:{Tick}"))
            {
                AddTrail(R.WitnessTrail, $"witnessed:{act}:{subject.Role}");
                MoveStanding(R.Traceable[act] * R.StandingStep, $"witnessed:{act}");
            }
            foreach (var w in seen) Remember(w, "saw-act", by, true);
            Note("witness." + act, $"{seen.Count} saw the {act}.");
            return true;
        }

        // ---------------------------------------------------------------- the rulebook's conditions, the action card

        sealed class PCtx { public Person P = null!; public Event Ev = null!; public Band Band; public string? Outcome; public bool Reach = true; public long Cash; }
        PCtx Ctx(Person p, Event ev) => new PCtx { P = p, Ev = ev, Band = R.BandOf(Standing), Cash = Cash };
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
                case "known": return p.Knows(c.S("what")) == (!c.N.Has("is") || c.B("is"));
                case "leverage": // Motive, motive-lovers.md §2.2: the need it acts on (known), or the person's secret known
                    {
                        var r = R.ApproachRequires(c.S("approach"));
                        return ((r == "nothing" || p.Knows(r)) && R.NeedMetBy(p.Need, c.S("approach"))) || p.Knows("secret");
                    }
                case "lover-within": { if (p.Lover == null) return false; var a = PersonPos(p); var b = PersonPos(p.Lover); return a.HasValue && b.HasValue && G.Within(a.Value, b.Value, (int)R.Num(c.S("m")) * 100); }
                case "requires": { var r = R.ApproachRequires(c.S("approach")); return r == "nothing" || p.Knows(r); }
                case "need-met-by": return R.NeedMetBy(p.Need, c.S("approach"));
                case "need-is": return c.L("in").Contains(p.Need);
                case "need-met": return p.NeedMet == c.B("is");
                case "reach": return x.Reach;
                case "cash-covers": return x.Cash >= Quote(x.Ev, p).WorstUsd;
                case "memory": if (c.N.Has("of") || c.N.Has("this")) break; return p.Has(c.S("act")) == c.B("has");
                case "fit": return c.L("is").Contains(FitId(R.FitOf(x.Ev.Id.Substring(5), p.Temperament, x.Band)));
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
        static string FitId(Fit f) => f == Fit.Suits ? "suits" : f == Fit.Worse ? "worse" : "refused";
        static readonly string[] TemperamentConds = { "fit", "temperament", "reports", "reports-approach", "orders-reached" };
        static bool ReadsTemperament(List<Cond> cs) { foreach (var c in cs) if (Array.IndexOf(TemperamentConds, c.K) >= 0) return true; return false; }

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
                if (truth || shown == "?") q.WorstUsd += line.Usd;
            }
            q.Orders = OrdersFor(ev, x);
            q.Outcome = p.KTemperament ? FirstOutcome(ev, x).Id : Agree(p, t => FirstOutcome(ev, WithTemperament(x, t)).Id);
            q.Event = ev.Id;
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
            if (at.HasValue) foreach (var w in People) { if (w == p) continue; var wp = PersonPos(w); if (wp.HasValue && G.Within(wp.Value, at.Value, witnessRange) && Sees(wp.Value, at.Value)) q.Witnesses++; }
            return q;
        }
        static readonly Cond ReportsCond = new Cond(JNode.Parse("{\"k\": \"reports\"}"));

        /// <summary>The action card for an operative's approach to a role: the price, the risks, and the walk to stand within reach.</summary>
        public Quote Preview(string approach, string role, OpId by = OpId.Fixer)
        {
            var p = PersonByRole(role); var q = Quote(R.Ev("turn." + approach), p);
            var at = PersonPos(p); var op = OpOf(by);
            if (at.HasValue && !G.Within(op.Pos, at.Value, approachReach))
            {
                var stand = S.Grid.NearestFree(at.Value); var route = stand.HasValue ? S.Grid.Route(op.Pos, stand.Value) : null;
                if (route != null) { long mm = Math.Max(0, new Path(route).Length - approachReach * 10L); q.WalkS = (int)((mm + opSpeed * Hz - 1) / (opSpeed * Hz)); }
            }
            // seen by (rulebook sight.unknown): where the operative stands for the act, its own place when already within reach, else the person's
            if (at.HasValue) q.Seen = SeenAt(G.Within(op.Pos, at.Value, approachReach) ? op.Pos : at.Value);
            return q;
        }

        /// <summary>The dossier's face bit: this person holds the Fixer's face (rulebook memory.faceFacts; §03 the face witnesses remember).</summary>
        public bool HoldsFace(Person p) { foreach (var m in p.Memory) if (R.FaceFact.ContainsKey(m.By)) return true; return false; }

        PCtx WithTemperament(PCtx x, string t)
        {
            var q = new Person { Role = x.P.Role, Id = x.P.Id, Name = x.P.Name, Temperament = t, Need = x.P.Need, Turned = x.P.Turned, NeedMet = x.P.NeedMet, KRoutine = x.P.KRoutine, KNeed = x.P.KNeed, KTemperament = x.P.KTemperament, Place = x.P.Place, Secret = x.P.Secret, KSecret = x.P.KSecret, Lover = x.P.Lover };
            Array.Copy(x.P.Sc, q.Sc, 4); q.Memory.AddRange(x.P.Memory);
            return new PCtx { P = q, Ev = x.Ev, Band = x.Band, Outcome = x.Outcome, Reach = x.Reach, Cash = x.Cash };
        }
        /// <summary>The answer every temperament gives alike, else "?" (Motive's review, other findings 3).</summary>
        string Agree(Person p, Func<string, string> f)
        {
            string? first = null;
            foreach (var t in R.TemperamentIds) { var a = f(t); if (first == null) first = a; else if (a != first) return "?"; }
            return first ?? "?";
        }

        // ---------------------------------------------------------------- the other verbs

        /// <summary>Whether the Fixer could call this contact now (the action card's Call), without calling.</summary>
        public bool CanCall(string role)
        {
            var p = PersonByRole(role); var f = OpOf(OpId.Fixer);
            if (Over || f.Busy != null || p.Down || !p.Turned) return false;
            if (p == Driver) return Vale != ValeAt.Car && (BlockMove == INF || Tick >= BlockedUntil + blockMoveT) && Tick < T(driverLastS) - blockMoveT;
            return p.Lover != null && SummonRefusal(p, out _).code == null;
        }

        /// <summary>Why the contact's lover won't come to a call now (rulebook summon), or code null; walkTicks: his walk to the caller.</summary>
        (string? code, string why) SummonRefusal(Person caller, out int walkTicks)
        {
            walkTicks = 0;
            var lover = caller.Lover!;
            if (lover != GuardP) return ("summon-no-visit", $"{lover.Role} has no post to leave (only a lover with a post is modelled)");
            if (GuardDown || lover.Down) return ("summon-busy", "he's down");
            var at = GuardPose(); if (!at.HasValue) return ("summon-off-street", "he isn't on the street");
            if (GuardHeld || GuardEngaged || !GuardAtDoor) return ("summon-busy", "he's busy");
            if (Alert > R.SummonAlertMax) return ("summon-alert", "security is searching: he won't leave");
            foreach (var m in lover.Memory)
                if ((m.Act == "saw-gunfire" || m.Act == "saw-violence" || m.Act == "saw-weapon-drawn") && (m.By == "scout" || m.By == "fixer" || m.By == "tech" || m.By == "enforcer"))
                    return ("summon-saw-violence", "he saw what you did");
            if (lover.Summoned >= R.SummonsPerShift) return ("summon-came-once", "he came once already");
            var to = PersonPos(caller); if (!to.HasValue) return ("summon-off-street", "she isn't on the street");
            var route = S.Grid.Route(S.Grid.NearestFree(at.Value.pos) ?? at.Value.pos, S.Grid.NearestFree(to.Value) ?? to.Value);
            if (route == null) return ("summon-too-far", "no walk between them");
            long len = new Path(route).Length; walkTicks = (int)((len + opSpeed - 1) / opSpeed); // at walking pace (cell.speed)
            if (walkTicks > T(R.SummonWalkMaxS)) return ("summon-too-far", $"too far: {walkTicks / Hz} s away");
            return (null, "");
        }

        /// <summary>The call card, before the click (Latch): would the Fixer's call on this role be taken now, and if not, its refusal code,
        /// words and numbers, exactly as Issue would answer it. Read-only: it changes nothing.</summary>
        public Answer CallQuery(string role)
        {
            var saved = refusal; refusal = default; string why;
            try
            {
                var op = OpOf(OpId.Fixer);
                why = Over ? No("mission-over", "the mission is over")
                    : op.Down ? No("agent-down", "the fixer is down")
                    : op.Busy != null ? No("agent-busy", $"fixer is busy until {op.BusyEnd}", tick: op.BusyEnd)
                    : CallRefusal(op, role);
            }
            catch (Exception e) when (!(e is OutOfMemoryException)) { why = No("engine-error", "engine error: " + e.GetType().Name + ": " + e.Message); }
            var a = why.Length == 0 ? new Answer { ok = true, code = "", why = "", m = -1, cm = -1, usd = -1, tick = -1 }
                : new Answer { ok = false, code = refusal.code ?? "uncoded", why = why, m = refusal.m, cm = refusal.cm, usd = refusal.usd, tick = refusal.tick };
            refusal = saved;
            return a;
        }

        /// <summary>Why the Fixer's call on a role is refused now, or "" (the checks of Call, without its effects).</summary>
        string CallRefusal(Op op, string role)
        {
            if (op.Id != OpId.Fixer) return No("only-fixer", "only the Fixer calls contacts (§03)");
            var p = PersonByRole(role);
            if (p.Down) return No("person-down", $"{p.Role} is down");
            if (!p.Turned) return No("not-a-contact", $"{p.Role} is not a contact");
            if (p == Driver) // the driver's call is his role job, the camera block (unchanged)
            {
                if (Vale == ValeAt.Car) return No("car-gone", "the official car is gone");
                if (BlockMove != INF && Tick < BlockedUntil + blockMoveT) return No("car-busy", "the car is still blocking or returning", tick: BlockedUntil + blockMoveT);
                if (Tick >= T(driverLastS) - blockMoveT) return No("call-too-late", "too late: the car leaves for the pickup at 5:40", tick: T(driverLastS) - blockMoveT);
                return "";
            }
            if (p.Lover != null)
            {
                var (code, why) = SummonRefusal(p, out int walk);
                return code != null ? No(code, why, tick: code == "summon-too-far" ? walk : -1) : "";
            }
            return No("nothing-to-call", $"{p.Role} has nothing to do on a call");
        }

        string Call(Op op, Order o)
        {
            var no = CallRefusal(op, o.Target); if (no.Length > 0) return no;
            var p = PersonByRole(o.Target);
            if (p == Driver) // the driver's call is his role job, the camera block (unchanged)
            {
                BlockMove = Tick; BlockedFrom = Tick + blockMoveT; BlockedUntil = Tick + blockMoveT + blockHoldT;
                AddTrail(R.CallTrail, "call:driver"); Remember(Driver, "job", "fixer", false);
                Actions.Add("call:driver"); Chain.Add("driver-blocks-camera");
                Note("call.driver", "The driver starts moving the car across the pickup camera.");
                return "";
            }
            return Summon(p); // CallRefusal let it through: the lover comes
        }

        /// <summary>The summons (rulebook summon; Motive, motive-lovers.md §1.3): a turned contact's call sends for their lover, read from the
        /// city's ties. He walks the grid to her at walking pace, stays summon.stay and walks back; refused at no cost, with its code.</summary>
        string Summon(Person p)
        {
            SummonRefusal(p, out int walk);
            var lover = p.Lover!;
            var from = GuardPose()!.Value.pos; var to = PersonPos(p)!.Value;
            guardOut = new Path(S.Grid.Route(S.Grid.NearestFree(from) ?? from, S.Grid.NearestFree(to) ?? to)!);
            guardBack = new Path(S.Grid.Route(S.Grid.NearestFree(to) ?? to, S.Grid.NearestFree(from) ?? from)!);
            walkT = walk; stayT = T(R.SummonStayS); guardRunning = false;
            LureTick = Tick; GuardAwayUntil = Tick + 2 * walkT + stayT; lover.Summoned++;
            AddTrail(R.CallTrail, "call:" + p.Role); Remember(p, "job", "fixer", false);
            Actions.Add("call:" + p.Role); Chain.Add($"{p.Role}-calls-{lover.Role}");
            Note("call." + p.Role, $"{p.Name} calls {lover.Name}. He leaves his post and walks over to her ({walkT / Hz} s), to stay {R.SummonStayS} s.");
            return "";
        }

        string Loop(Op op, Order o)
        {
            if (op.Id != OpId.Tech) return No("only-tech", "only the Tech loops cameras (§03)");
            if (!LoopTarget(o.Target)) return No("loop-wrong-target", "the Tech's cabinet loops the entrance camera, the pickup camera and the city's own cameras (the Records Square mast)");
            if (LoopOn) return No("loop-holds", "the loop already holds");
            if (!G.Within(op.Pos, S.At("cabinet"), R.AtReachCm)) return No("stand-at-cabinet", "stand at the cabinet", cm: R.AtReachCm);
            Engage(op, o, R.LoopS); return "";
        }
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
            var r = RivalsPose(); if (!r.HasValue) return No("no-rivals", "no rivals here");
            if (!G.Within(op.Pos, r.Value.pos, actReach)) return No("stand-near-rivals", "stand near the rivals", m: actReach / 100);
            Engage(op, o, R.MisleadS); return "";
        }

        string Enter(Op op, Order o)
        {
            if (op.Id != OpId.Enforcer) return No("only-enforcer", "only the Enforcer enters (§03)");
            if (op.CarryBody.Length > 0) return No("already-carrying", "carrying a body");
            if (Alert >= 3 && DialNow.DoorsLock) return No("doors-locked", "lockdown: the Office doors are locked (brief 013, the dial)");
            if (DoorHeld(EnterDoor)) return No("door-held", "a unit holds the scene at this door: nobody goes in (rev 4: a found body)");
            if (Vale != ValeAt.Office || EntryInProgress) return No("vale-not-inside", "Vale is not inside");
            if (!G.Within(op.Pos, S.At(S.GuardPost), R.AtReachCm)) return No("stand-at-door", "stand at the public door", cm: R.AtReachCm);
            op.Walk = null;
            if (GuardAtDoor) { Fail("entry-seen-guard", "The guard is back at the door. Vale is warned."); return ""; }
            if (CameraWatching("entrance", op.Pos)) { Fail("entry-seen-camera", "The entrance camera reports the breach. Vale is locked down."); return ""; }
            if (PatrolWatching(S.At(S.GuardPost))) { Fail("entry-seen-patrol", "The patrol reports the breach. Vale is locked down."); return ""; }
            RouteKind = "front"; RouteGuard = GuardDown ? "shot" : "waitress"; RouteBot = SweepSeesEntrance(Tick) ? "rerouted" : "gap";
            EntryInProgress = true; op.Inside = true; Chain.Add("enforcer-reaches-vale");
            bool alarm = Alert >= 2; // Sky 2026-10-15 Q5 (rulebook cell.enter.alarm): once Alert ≥ 2 the Office staff hide Vale
            op.Gun.Order = ""; op.Gun.Target = "";
            Engage(op, o, R.EnterS + (alarm ? R.EnterAlarmS : 0));
            Note("enter.in", alarm ? "The Enforcer is inside. The alarm is up: the staff are hiding Vale. Watch the service lane." : "The Enforcer is inside. Watch the service lane.");
            return "";
        }
        string Take(Op op, Order o)
        {
            if (op.Id != OpId.Enforcer) return No("only-enforcer", "only the Enforcer takes Vale (§03)");
            if (op.CarryBody.Length > 0) return No("already-carrying", "carrying a body");
            if (Vale != ValeAt.Pickup) return No("vale-not-at-car", "Vale is not at his car");
            var vp = ValePos()!.Value;
            if (!G.Within(op.Pos, vp, actReach)) return No("stand-beside-vale", "stand beside Vale", m: actReach / 100);
            op.Walk = null;
            if (!PickupService && !TakeInCar && CameraWatching("pickup", op.Pos)) { Fail("abduction-seen-camera", "The pickup camera reports the abduction. Vale is rescued."); return ""; }
            var rp = RivalsPose();
            if (RivalsPresent && (!RivalsDrawn || (rp.HasValue && G.Within(op.Pos, rp.Value.pos, RivalsGun.W!.EffCm)))) // drawn rivals within their weapon's effective reach
            { Fail("rivals-reach-vale", "The rival crew follows Vale's car and reaches the pickup first."); return ""; }
            RouteKind = PickupService ? "service" : "pickup"; RouteRival = RivalsArrived ? RivalsDelayMethod : "";
            Vale = ValeAt.Taking;
            Engage(op, o, R.TakeS);
            TakeScan(); // a Take seen is a takedown seen (brief 013 D; rev 4: its struggle, unless inside his own car)
            return "";
        }
        string Extract(Op op, Order o)
        {
            if (op.Id != OpId.Enforcer) return No("only-enforcer", "only the Enforcer brings Vale to the car");
            if (Vale != ValeAt.WithEnforcer) return No("no-vale-yet", "the Enforcer does not have Vale");
            if (!G.Within(op.Pos, S.At(S.GetawayCar), R.AtReachCm)) return No("stand-at-getaway", "stand at the getaway car", cm: R.AtReachCm);
            if (CarHeld()) return No("car-held", "the car is held: the fight isn't over (§07)");
            op.Walk = null;
            Vale = ValeAt.Car; Result = Outcome.Success; Chain.Add("vale-in-getaway-car");
            Note("success", "Vale is in the car. The crew escapes.");
            Captures();
            return "";
        }
    }
}
