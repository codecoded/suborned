// Orders: each is checked when given (a rejected order changes nothing and is logged with its reason), takes its time, and lands at
// its end. Turn, Watch and Listen run from the rulebook's events (needs, price lines, outcomes, also, witnesses); the other verbs
// read their numbers from the rulebook's `cell` section and the mission's from the site.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>Give an order now (its tick must be the game's tick). Returns whether it was taken, and why not.</summary>
        public (bool ok, string why) Issue(Order o)
        {
            if (o.Tick != Tick) throw new InvalidOperationException($"order for tick {o.Tick} given at tick {Tick}");
            string why;
            try { why = Over ? "the mission is over" : Check(o); }
            catch (Exception e) when (!(e is OutOfMemoryException)) { why = "engine error: " + e.GetType().Name + ": " + e.Message; }
            bool ok = why.Length == 0;
            Orders.Add((o, ok, why));
            if (!ok) Note("order.rejected", $"{o.Op} {o.Do} {o.Target}{o.To}: {why}");
            return (ok, why);
        }
        public bool Do(Order o) => Issue(o).ok;

        string Check(Order o)
        {
            if (o.Do == "withdraw") { Result = Outcome.Withdrawn; Note("withdraw", "The cell withdraws. The attempt ends."); return ""; }
            var op = OpOf(Sim.Op.Parse(o.Op));
            if (op.Inside) return "the Enforcer is inside the Office";
            if (op.Busy != null) return $"{o.Op} is busy until {op.BusyEnd}";
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
                case "enter": return Enter(op, o);
                case "take": return Take(op, o);
                case "extract": return Extract(op, o);
                default: return $"verb '{o.Do}' is unknown (unknown = reject)";
            }
        }

        void Engage(Op op, Order o, int seconds)
        {
            if (seconds < 1) seconds = 1; // the rulebook bounds every duration to 1 s or more at load (Redline E3); a 1 s floor is the belt

            op.Walk = null; op.WalkMm = 0; // engaging stops the approach walk
            op.Busy = o; op.BusyEnd = Tick + T(seconds);
            Note("order." + o.Do, $"{Sim.Op.Name(op.Id)} {o.Do}{(o.Target.Length > 0 ? " " + o.Target : "")} ({seconds} s)");
        }

        string Move(Op op, Order o)
        {
            Pt to;
            if (o.Point.HasValue) to = o.Point.Value;
            else if (S.HasPlace(o.To)) to = S.Stand(o.To);
            else return $"no place '{o.To}'";
            if (!S.Grid.Free(to)) return "that ground can't be walked (a roof, a wall or a solid)";
            var route = S.Grid.Route(op.Pos, to); if (route == null) return "no walk there";
            op.Walk = new Path(route); op.WalkMm = 0;
            if (op.Walk.Length == 0) op.Walk = null;
            op.Running = o.Run; op.WalkSpeed = PaceOf(op, o.Run);
            Note("order.move", $"{Sim.Op.Name(op.Id)} {(o.Run ? "runs" : "heads")} for {(o.To.Length > 0 ? o.To : to.ToString())}.");
            return "";
        }

        /// <summary>A move's pace: walking (cell.speed), running (cell.runSpeed), or, for an Enforcer carrying Vale, cell.carryRunSpeed.</summary>
        int PaceOf(Op op, bool run)
        {
            if (!run) return opSpeed;
            if (op.Id == OpId.Enforcer && Vale == ValeAt.WithEnforcer) return R.CarryRunSpeed;
            return R.RunSpeed;
        }

        /// <summary>The attention a run draws, a tick at a time. TODO(Motive, briefs/011/motive-fights.md): running's costs — witnesses
        /// noticing a runner, camera and patrol logs of a running operative, Alert — land here once the proposal is ruled. Today: none.</summary>
        void RunAttention(Op op) { }

        // ---------------------------------------------------------------- the person events (rulebook scout.*, turn.*)

        string Present(Person p, out Pt pos)
        {
            pos = default;
            var at = PersonPos(p);
            if (!at.HasValue) return p.Down ? $"{p.Role} is down" : $"{p.Role} is not out on the street";
            pos = at.Value; return "";
        }

        string Scout(Op op, Order o)
        {
            if (op.Id != OpId.Scout) return "only the Scout watches and listens (§03)";
            var p = PersonByRole(o.Target); var miss = Present(p, out var pos); if (miss.Length > 0) return miss;
            var ev = R.Ev("scout." + o.Do);
            int reach = actReach;
            foreach (var c in ev.Needs) if (c.K == "reach") reach = (int)R.Num(c.S("m")) * 100;
            if (!G.Within(op.Pos, pos, reach)) return $"stand within {reach / 100} m of {p.Role}";
            var x = Ctx(p, ev);
            foreach (var c in ev.Needs) if (c.K != "reach" && !Holds(c, x)) return $"needs {Describe(c)}";
            var q = Quote(ev, p);
            Engage(op, o, (int)q.S);
            op.BusyQuote = q; op.BusyPaid = 0;
            return "";
        }

        string Turn(Op op, Order o)
        {
            if (op.Id != OpId.Fixer) return "only the Fixer turns people (§03)";
            if (o.Approach.Length == 0) return "turn needs an approach (ask, pay, help, pressure)";
            var ev = R.Ev("turn." + o.Approach);
            var p = PersonByRole(o.Target); var miss = Present(p, out var pos); if (miss.Length > 0) return miss;
            if (p == ValeP) return "Vale is the target, not a contact";
            var x = Ctx(p, ev);
            x.Reach = G.Within(op.Pos, pos, approachReach);
            foreach (var c in ev.Needs) if (!Holds(c, x)) return c.K == "reach" ? $"stand within {approachReach / 100} m of {p.Role}" : $"needs {Describe(c)}";
            var q = Quote(ev, p);
            if (p == Driver && Tick + T((int)q.S) > T(driverLastS)) return "the driver can be turned only until 5:40 (§07 last chance to turn driver)";
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
                case "watch": case "listen": LandPersonEvent(op, o, R.Ev("scout." + o.Do), PersonByRole(o.Target)); break;
                case "turn": LandPersonEvent(op, o, R.Ev("turn." + o.Approach), PersonByRole(o.Target)); break;
                case "loop":
                    LoopOn = true; AddTrail(R.LoopTrail, "loop"); Actions.Add("loop");
                    Note("loop.on", "One feed repeats. The Tech is tied to the cabinet."); break;
                case "reroute":
                    if (PatrolDown) break;
                    PatrolAwayUntil = Tick + T(R.RerouteAwayS); PatrolChasing = false; PatrolTracking = false;
                    AddTrail(R.RerouteTrail, "reroute"); Actions.Add("reroute"); Chain.Add("patrol-rerouted");
                    Note("patrol.rerouted", "The patrol follows false orders and leaves the entrance sweep."); break;
                case "mislead":
                    if (!RivalsPresent) { Note("mislead.missed", "The rivals are gone before the tip lands."); break; }
                    RivalsDelayedUntil = Tick + T(R.MisleadAwayS); RivalsDelayMethod = "mislead"; MisledOnce();
                    AddTrail(R.MisleadTrail, "mislead"); Actions.Add("mislead"); Chain.Add("rivals-false-tip");
                    Note("rivals.misled", "A false tip sends the rival crew away. They may return."); break;
                case "enter":
                    op.Inside = false; op.Pos = S.Stand(S.ValeService); EntryInProgress = false; Vale = ValeAt.WithEnforcer;
                    Note("enter.out", "Vale and the Enforcer come out at the service lane."); StartFight(); break;
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
                if ((p.Memory[j].Act == "saw-act" || p.Memory[j].Act == "told") && (drop < 0 || p.Memory[j].Tick < p.Memory[drop].Tick)) drop = j;
            if (drop < 0) for (int j = 0; j < p.Memory.Count; j++) if (drop < 0 || p.Memory[j].Tick < p.Memory[drop].Tick) drop = j;
            p.Memory.RemoveAt(drop);
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
                case "known": return p.Knows(c.S("what"));
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
            if (ev.Id.StartsWith("turn.", StringComparison.Ordinal) && p.KnownRefused.Contains(ev.Id.Substring(5))) q.Outcome = "refused"; // a refusal seen: ✗
            if (ev.Id.StartsWith("turn.", StringComparison.Ordinal))
            {
                q.Reports = p.KTemperament ? (Holds(ReportsCond, x) ? "yes" : "no") : Agree(p, t => Holds(ReportsCond, WithTemperament(x, t)) ? "yes" : "no");
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
            return q;
        }

        /// <summary>The dossier's face bit: this person holds the Fixer's face (rulebook memory.faceFacts; §03 the face witnesses remember).</summary>
        public bool HoldsFace(Person p) { foreach (var m in p.Memory) if (R.FaceFact.ContainsKey(m.By)) return true; return false; }

        PCtx WithTemperament(PCtx x, string t)
        {
            var q = new Person { Role = x.P.Role, Id = x.P.Id, Name = x.P.Name, Temperament = t, Need = x.P.Need, Turned = x.P.Turned, NeedMet = x.P.NeedMet, KRoutine = x.P.KRoutine, KNeed = x.P.KNeed, KTemperament = x.P.KTemperament, Place = x.P.Place };
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
            if (p == Inez) return !GuardDown && GuardAtDoor;
            if (p == Driver) return Vale != ValeAt.Car && (BlockMove == INF || Tick >= BlockedUntil + blockMoveT) && Tick < T(driverLastS) - blockMoveT;
            return false;
        }

        string Call(Op op, Order o)
        {
            if (op.Id != OpId.Fixer) return "only the Fixer calls contacts (§03)";
            var p = PersonByRole(o.Target);
            if (p.Down) return $"{p.Role} is down";
            if (!p.Turned) return $"{p.Role} is not a contact";
            if (p == Inez)
            {
                if (GuardDown) return "the guard is down";
                if (!GuardAtDoor) return "the guard is not at the door";
                LureTick = Tick; GuardAwayUntil = Tick + 2 * walkT + stayT;
                AddTrail(R.CallTrail, "call:waitress"); Remember(Inez, "job", "fixer", false);
                Actions.Add("call:waitress"); Chain.Add("waitress-calls-guard");
                Note("call.waitress", "Inez calls the guard. He leaves the entrance and walks toward her café.");
                return "";
            }
            if (p == Driver)
            {
                if (Vale == ValeAt.Car) return "the official car is gone";
                if (BlockMove != INF && Tick < BlockedUntil + blockMoveT) return "the car is still blocking or returning";
                if (Tick >= T(driverLastS) - blockMoveT) return "too late: the car leaves for the pickup at 5:40";
                BlockMove = Tick; BlockedFrom = Tick + blockMoveT; BlockedUntil = Tick + blockMoveT + blockHoldT;
                AddTrail(R.CallTrail, "call:driver"); Remember(Driver, "job", "fixer", false);
                Actions.Add("call:driver"); Chain.Add("driver-blocks-camera");
                Note("call.driver", "The driver starts moving the car across the pickup camera.");
                return "";
            }
            return $"{p.Role} has nothing to do on a call";
        }

        string Loop(Op op, Order o)
        {
            if (op.Id != OpId.Tech) return "only the Tech loops cameras (§03)";
            if (o.Target != "entrance") return "the Tech's cabinet loops the entrance camera";
            if (LoopOn) return "the loop already holds";
            if (!G.Within(op.Pos, S.At("cabinet"), actReach)) return "stand at the cabinet";
            Engage(op, o, R.LoopS); return "";
        }
        string Reroute(Op op, Order o)
        {
            if (op.Id != OpId.Tech) return "only the Tech reroutes patrols (§03)";
            if (PatrolDown) return "the patrol is down";
            if (PatrolAwayUntil > Tick) return "the patrol is already away";
            if (!G.Within(op.Pos, S.At("cabinet"), actReach)) return "stand at the cabinet";
            Engage(op, o, R.RerouteS); return "";
        }
        string Mislead(Op op, Order o)
        {
            if (op.Id != OpId.Scout) return "only the Scout misleads (§03)";
            var r = RivalsPose(); if (!r.HasValue) return "no rivals here";
            if (!G.Within(op.Pos, r.Value.pos, actReach)) return "stand near the rivals";
            Engage(op, o, R.MisleadS); return "";
        }

        string Enter(Op op, Order o)
        {
            if (op.Id != OpId.Enforcer) return "only the Enforcer enters (§03)";
            if (Vale != ValeAt.Office || EntryInProgress) return "Vale is not inside";
            if (!G.Within(op.Pos, S.At(S.GuardPost), actReach)) return "stand at the public door";
            op.Walk = null;
            if (GuardAtDoor) { Fail("entry-seen-guard", "The guard is back at the door. Vale is warned."); return ""; }
            if (CameraWatching("entrance", op.Pos)) { Fail("entry-seen-camera", "The entrance camera reports the breach. Vale is locked down."); return ""; }
            if (PatrolWatching(S.At(S.GuardPost))) { Fail("entry-seen-patrol", "The patrol reports the breach. Vale is locked down."); return ""; }
            RouteKind = "front"; RouteGuard = GuardDown ? "shot" : "waitress"; RouteBot = SweepSeesEntrance(Tick) ? "rerouted" : "gap";
            EntryInProgress = true; op.Inside = true; Chain.Add("enforcer-reaches-vale");
            Engage(op, o, R.EnterS);
            Note("enter.in", "The Enforcer is inside. Watch the service lane.");
            return "";
        }
        string Take(Op op, Order o)
        {
            if (op.Id != OpId.Enforcer) return "only the Enforcer takes Vale (§03)";
            if (Vale != ValeAt.Pickup) return "Vale is not at his car";
            var vp = ValePos()!.Value;
            if (!G.Within(op.Pos, vp, actReach)) return "stand beside Vale";
            op.Walk = null;
            if (!PickupService && CameraWatching("pickup", op.Pos)) { Fail("abduction-seen-camera", "The pickup camera reports the abduction. Vale is rescued."); return ""; }
            var rp = RivalsPose();
            if (RivalsPresent && (!RivalsDrawn || (rp.HasValue && G.Within(op.Pos, rp.Value.pos, R.FireRivalsReachCm))))
            { Fail("rivals-reach-vale", "The rival crew follows Vale's car and reaches the pickup first."); return ""; }
            RouteKind = PickupService ? "service" : "pickup"; RouteRival = RivalsArrived ? RivalsDelayMethod : "";
            Vale = ValeAt.Taking;
            Engage(op, o, R.TakeS);
            return "";
        }
        string Extract(Op op, Order o)
        {
            if (op.Id != OpId.Enforcer) return "only the Enforcer brings Vale to the car";
            if (Vale != ValeAt.WithEnforcer) return "the Enforcer does not have Vale";
            if (!G.Within(op.Pos, S.At(S.GetawayCar), actReach)) return "stand at the getaway car";
            if (CarHeld()) return "the car is held: the fight isn't over (§07)";
            op.Walk = null;
            Vale = ValeAt.Car; Result = Outcome.Success; Chain.Add("vale-in-getaway-car");
            Note("success", "Vale is in the car. The crew escapes.");
            return "";
        }

        string Fire(Op op, Order o)
        {
            if (op.Id != OpId.Enforcer) return "only the Enforcer fires (§03)";
            if (Tick < EnforcerNextShot) return $"the next shot is ready at {EnforcerNextShot}";
            if (o.Target == "hostiles")
            {
                var h = NearestHostile(op.Pos, R.FireReachCm);
                if (h == null) return "no hostile in range with a clear line";
                op.Walk = null; Actions.Add("fire:hostiles"); Note("fire.hostiles", $"The Enforcer fires on {h.Id}.");
                FireHostile(h); return "";
            }
            Pt? target = o.Target switch
            {
                "guard" => GuardPose()?.pos, "waitress" => Inez.Down ? null : PersonPos(Inez), "driver" => DriverPos(),
                "councillor" => Vale == ValeAt.Car ? null : ValePos(), "patrol" => PatrolDown ? null : PatrolPose()?.pos,
                "security" => SecurityPose()?.pos, "rivals" => RivalsPose()?.pos, _ => null,
            };
            if (!target.HasValue) return $"no {o.Target} to fire at";
            int reach = (o.Target == "rivals" ? R.FireRivalsReachCm : R.FireReachCm);
            if (!G.Within(op.Pos, target.Value, reach)) return $"out of range ({reach / 100} m)";
            if (!Sees(op.Pos, target.Value)) return "no clear line";
            op.Walk = null;
            Actions.Add("fire:" + o.Target);
            Note("fire." + o.Target, $"The Enforcer fires on the {o.Target}.");
            switch (o.Target)
            {
                case "guard": FireGuard(op, target.Value); break;
                case "waitress": FireCivilian(Inez, "Inez is shot at the café. Witnesses call security."); break;
                case "driver":
                    carStoppedAt = CarPos(); FireCivilian(Driver, "The official driver is shot. Security rushes to the pickup.");
                    if (Vale != ValeAt.Office) Fail("driver-shot", "The driver is shot during handover. Security pulls Vale away.");
                    break;
                case "councillor":
                    RegisterGunfire("councillor"); RaiseAlert(R.ViolenceSeen, "violence-seen:councillor"); Vale = ValeAt.Dead; ValeP.Down = true;
                    Fail("vale-dead", "Vale is dead. The live capture objective has failed."); break;
                case "security":
                    RegisterGunfire("security");
                    if (--SecurityHealth <= 0) { SecurityDown = true; Chain.Add("response-unit-down"); Note("security.down", "The response unit is down. The wider cordon is still closing."); }
                    break;
                case "patrol":
                    RegisterGunfire("patrol"); CallSecurity(R.RespSeenS, "Gunfire at the patrol is reported.");
                    if (--PatrolHealth <= 0) { patrolDownAt = target.Value; PatrolDown = true; PatrolChasing = false; Chain.Add("patrol-down"); Note("patrol.down", "The patrol goes down. Its distress signal is already out."); }
                    break;
                case "rivals":
                    RegisterGunfire("rivals");
                    CallSecurity(CameraWatching("pickup", op.Pos) ? R.RespSeenS : R.RespRivalsUnseenS, "The firefight with the rivals is reported.");
                    if (--RivalsHealth <= 0)
                    {
                        RivalsHealth = R.Hits; RivalsDelayedUntil = Tick + T(R.RivalsRetreatS); RivalsDelayMethod = "fire";
                        Chain.Add("rivals-driven-back"); Note("rivals.retreat", "The rivals retreat for now.");
                    }
                    break;
            }
            return "";
        }

        void FireGuard(Op op, Pt guardAt)
        {
            var pose = GuardPose()!.Value;
            bool seen = GuardWatching(op.Pos), bot = PatrolWatching(op.Pos), cam = CameraWatching("entrance", op.Pos);
            var seenGuard = WitnessesWithPos(pose.pos, GuardP);
            foreach (var w in WitnessesWithPos(op.Pos, GuardP)) if (!seenGuard.Exists(x => x.p == w.p)) seenGuard.Add(w);
            AddIncident("gunfire", "gunfire:guard", pose.pos, seenGuard);
            var civilians = WitnessesOf(pose.pos, GuardP).FindAll(c => c.Civilian);   // §05: violence seen by a civilian → 3 at once
            bool inezSees = civilians.Contains(Inez), civSees = civilians.Count > 0;
            RegisterGunfire("guard");
            GuardHealth--;
            foreach (var c in civilians) ViolenceSeen("guard", c);
            if (inezSees) { InezWitnessedShooting = true; Note("witness.waitress", "Inez sees the guard shot beside her café. She is now a witness."); }
            else if (civSees) Note("witness.crowd", $"{civilians.Count} on the street see the guard shot.");
            CallSecurity(civSees ? R.RespWitnessS : seen || bot || cam ? R.RespSeenS : R.RespUnseenS,
                inezSees ? "Inez reports the shooting." : civSees ? "A witness reports the shooting." : seen ? "The guard calls in the shooting." : bot ? "The patrol reports gunfire." : cam ? "The entrance camera reports gunfire." : "Shots are heard in the district.");
            if (GuardHealth > 0)
            {
                if (pose.phase != "door") { GuardHeld = true; heldPos = pose.pos; heldFacing = pose.facing; heldPhase = "wounded"; GuardAwayUntil = INF; }
                if (seen) { GuardEngaged = true; GuardNextShot = Tick + T(R.AimS); }
                Note("guard.hit", seen ? "The guard is hit but fires back." : "The guard is hit. He cannot see the Enforcer yet.");
            }
            else
            {
                GuardDown = true; GuardP.Down = true; GuardEngaged = false; MissingNotice = Tick + T(R.CheckInS);
                Chain.Add("guard-shot"); Note("guard.down", "The guard falls. His missed check-in will draw more attention.");
            }
        }

        void FireCivilian(Person p, string text)
        {
            var g = GuardPose(); var at = PersonPos(p);
            if (at.HasValue) { var ws = WitnessesWithPos(at.Value, p); AddIncident("violence", "violence:" + p.Role, at.Value, ws); foreach (var (c, _) in ws) Remember(c, "saw-act", "enforcer", true); }
            RegisterGunfire(p.Role);
            RaiseAlert(R.ViolenceSeen, $"violence-seen:{p.Role}");
            if (incidents.Add("seen:" + p.Role)) { AddTrail(R.WitnessTrail, $"witnessed:violence:{p.Role}"); MoveStanding(-R.StandingStep, $"violence-seen:{p.Role}"); }
            p.Down = true; p.Turned = false;
            if (g.HasValue && g.Value.phase != "door" && !GuardHeld)
            {
                GuardHeld = true; heldPos = g.Value.pos; heldFacing = g.Value.facing; heldPhase = "searching for the shooter"; GuardAwayUntil = INF;
                if (GuardWatching(Enforcer.Pos)) { GuardEngaged = true; GuardNextShot = Tick + T(R.AimS); }
            }
            CallSecurity(R.RespCivilianS, text);
            Chain.Add(p.Role + "-shot");
        }
    }
}
