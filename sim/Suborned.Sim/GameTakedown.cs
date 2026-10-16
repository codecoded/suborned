// Takedowns, bodies and the people who find them (brief 013 B, Keel; Sky 2026-10-16: "anyone can be targeted, but it's all about being seen
// or not"). The rules are the rulebook's `takedown` and `witness` sections (Motive, briefs/013/motive-takedowns.md; Pulse,
// briefs/013/pulse-witnesses.md). One rule set for every simulated person, cast or crowd: no named target list.
//  · The Enforcer takes a person down (knockout or kill) within reach; the victim is seized at the order and lies where he fell: a body.
//  · A body persists. Anyone whose sight passes an open body finds it; only a stash (a container, a service door) hides one.
//  · Silent acts reach Alert only through people: a witness or a finder decides by Pulse's list, walks to a phone and calls; the report
//    lands at the desk and Alert rises then (Sky merged call 1). Gunfire keeps its rule at sight.
//  · One takedown is one incident (TdAct): every discovery of it (seen, found, carried, woken, missed, missing) raises Alert once, to
//    the worst level, and its Trail is charged once.
// Integers only; people in OnTheStreet order (the cast by role, then the crowd by id); acts, bodies and reports in creation order.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>A person taken down: where they lie, who carries them, the stash that hides them, whether they are tied (brief 013).</summary>
    public sealed class Body
    {
        public string Id = ""; public Person P = null!; public Walker? W; public int Act;
        public bool Dead, Seizing, Restrained; public Pt At; public OpId? Carrier; public string Stash = "";
        public int DownTick, WakeAt, Phase, NextCallOut = -1, FreeAt = -1;
        public bool Open => Stash.Length == 0;
    }

    /// <summary>One takedown, one incident (Motive §2.3): what the city has learned of it. Take: Vale's abduction seen (How "take", no
    /// body; Aster 2026-10-16, brief 013 D: a Take seen within witness range is a takedown seen).</summary>
    public sealed class TdAct
    {
        public int Id; public string Victim = "", How = ""; public Person P = null!; public Pt At, Scene; public int Tick; public bool Aware, Kill;
        public int Alert; public bool Take, Landed, Witnessed, TrailPaid, StandingPaid, ActTrail, Confirmed, SecuritySaw, MarkMissed, Missed, Woke, Statement;
        public readonly SortedSet<string> Noticed = new SortedSet<string>(StringComparer.Ordinal); // who reacted (people by id, watchers by id)
        public readonly SortedSet<string> Heard = new SortedSet<string>(StringComparer.Ordinal);
        public readonly SortedSet<string> Opened = new SortedSet<string>(StringComparer.Ordinal);  // stashes the search opened
        public Incident? Lead;                                                                     // the investigation's lead for it
    }

    /// <summary>A report on its way to the desk (Pulse §3): who carries it, what it says, when it lands.</summary>
    public sealed class Rep
    {
        public int Seq, Act; public string Kind = "", By = "", Stash = ""; public Person? P; public Walker? W; public Pt Place; public int Sev, LandAt;
        public string Phase = "";   // react, check, walk, call, radio, watcher
        public int PhaseUntil;      // check: when the check ends
    }

    /// <summary>What the takedown card shows before the order is given (Motive §5.4). "?" where the cell doesn't know.</summary>
    public sealed class TakedownQuote
    {
        public string Refused = "", RefusedCode = "";
        public long Usd; public int S;                     // time in seconds (the struggle included when the target sees it coming)
        public string Aware = "?";                         // yes, no, ? (from behind: no)
        public bool Armed; public string IfAware = "";     // "struggle" or "fight" (an armed target who sees it coming)
        public SeenQuote Seen = new SeenQuote();           // watchers: true answer if known, "?" if not (rulebook sight.unknown)
        public readonly List<(string id, string name, string verb, int landS)> People = new List<(string, string, string, int)>(); // within witness range now: verb "?" until the temperament is known
        public int AlertIfSeen, AlertIfFound, TrailIfFound, TrailIfMissing; public bool MenaceIfFound;
        public int PoliceInS = -1;                         // a body found here now by staff: the response on the scene in ~n s (rev 4: a unit comes at any Alert)
        public readonly List<string> HoldsDoors = new List<string>(); // rev 4 R4.1: the entrances a found body here would hold (Enter refused, no exit)
        public int HoldS;                                  // and for about how long once the unit is there (s): its sweep, or the search stand-down at Alert 2
        public string IfNeverFound = "";                   // comes-home or missing
        public int WakesAt = -1, NextMark = -1, Ties;      // ticks; the next check-in mark of a staff victim once his routine is known; ties (kill)
    }

    public sealed partial class Game
    {
        public readonly List<TdAct> Acts = new List<TdAct>();
        public readonly List<Body> Bodies = new List<Body>();
        public readonly List<Rep> Reps = new List<Rep>();
        readonly SortedSet<string> markedFoes = new SortedSet<string>(StringComparer.Ordinal); // operatives security saw do violence (they engage them)
        int repSeq;
        /// <summary>Security saw this operative do violence (brief 013): they engage him on sight, weapon out or not.</summary>
        public bool MarkedFoe(string op) => markedFoes.Contains(op);
        Dictionary<string, Person>? byIdCache; Dictionary<Person, Walker>? walkerOfCache;

        // ---------------------------------------------------------------- who is who

        Dictionary<string, Person> ById { get { if (byIdCache == null) BuildIndex(); return byIdCache!; } }
        Dictionary<Person, Walker> WalkerOfMap { get { if (walkerOfCache == null) BuildIndex(); return walkerOfCache!; } }
        void BuildIndex()
        {
            byIdCache = new Dictionary<string, Person>(StringComparer.Ordinal); walkerOfCache = new Dictionary<Person, Walker>();
            foreach (var p in People) byIdCache[p.Id] = p;
            foreach (var w in Walkers) { byIdCache[w.P.Id] = w.P; walkerOfCache[w.P] = w; }
        }
        /// <summary>A simulated person by role (waitress, driver, guard, councillor) or by id, cast or crowd; null if none.</summary>
        public Person? PersonById(string idOrRole)
        {
            if (IsPersonRoleName(idOrRole)) return PersonByRole(idOrRole);
            return ById.TryGetValue(idOrRole, out var p) ? p : null;
        }
        /// <summary>Where a person stands now, or (off the street) where the mission keeps them; for the scenarios.</summary>
        public Pt CrowdPosOrStand(Person p) => PosOfPerson(p) ?? (p.Place.Length > 0 && S.HasPlace(p.Place) ? S.Stand(p.Place) : default);
        public Walker? WalkerOf(Person p) => WalkerOfMap.TryGetValue(p, out var w) ? w : null;
        /// <summary>Where a person stands on the street now, cast or crowd; null if down, inside or off the street.</summary>
        public Pt? PosOfPerson(Person p) { var w = WalkerOf(p); return w != null ? (Start.Crowd ? CrowdPos(w) : null) : PersonPos(p); }
        bool IsStaff(Person p) => p == GuardP || R.StaffJobs.Contains(p.Job);
        bool IsCellPerson(Person p) => p.Turned || p.Sc[Rules.Loyalty] >= R.WordQuietLoyalty;
        /// <summary>The kinds of tie a has with b (the people file's ties; the cast's lover tie as the engine holds it).</summary>
        List<string> TiesBetween(Person a, Person b)
        {
            var l = new List<string>();
            if (People2 != null && People2.Ties.TryGetValue(a.Id, out var ts)) foreach (var (k, w) in ts) if (w == b.Id && !l.Contains(k)) l.Add(k);
            if (a.Lover == b && !l.Contains("lover")) l.Add("lover");
            return l;
        }
        bool IsKin(Person a, Person victim) { foreach (var k in TiesBetween(a, victim)) if (R.KinTies.Contains(k)) return true; return false; }
        public Body? BodyOf(Person p) { foreach (var b in Bodies) if (b.P == p) return b; return null; }
        public Body? BodyById(string idOrRole) { var p = PersonById(idOrRole); return p == null ? null : BodyOf(p); }
        /// <summary>Where a body is now: with its carrier, in its stash, or where it lies.</summary>
        public Pt BodyPos(Body b) => b.Carrier.HasValue ? OpOf(b.Carrier.Value).Pos : b.At;
        TdAct ActOf(Body b) => Acts[b.Act];
        StashPlace? StashById(string id) { foreach (var s in S.Stashes) if (s.Id == id) return s; return null; }
        int InStash(string id) { int n = 0; foreach (var b in Bodies) if (b.Stash == id) n++; return n; }
        /// <summary>How many bodies a stash holds (rev 4 R4.3): a service door takedown.stash.doorHolds (a storeroom), a container what its
        /// site entry says (the Dumpster 1). The site's door entries carry 1 from before rev 4; the rulebook's number is the rule.</summary>
        public int HoldsOf(StashPlace s) => s.Kind == "door" ? R.StashDoorHolds : s.Holds;

        // ---------------------------------------------------------------- the takedown order

        /// <summary>Is the target aware of the Enforcer as the order is given (Motive §1.3)? The guard by his own cone (his door cone at the
        /// door); anyone walking by the facing cone along the way they walk; anyone who has witnessed one of the cell's acts this mission;
        /// otherwise a person standing at a stop has no facing yet (Pulse), so is surprised. A runner within witness range is heard.
        /// Armed: carries a weapon (the door guard).</summary>
        (bool aware, bool armed) Awareness(Person p, Pt at)
        {
            var e = Enforcer; bool armed = p == GuardP && GuardGun.W != null && !GuardAlly;
            bool aware = e.Running && e.Walk != null && G.Within(e.Pos, at, witnessRange);
            if (!aware && p == GuardP) aware = GuardWatching(e.Pos, false);
            // Motive rev 3 §1.3: a person who witnessed one of the cell's acts (a takedown, a carry, a drawn weapon) stays aware of the
            // Enforcer for the rest of the mission (until Pulse gives a stop its facing)
            if (!aware) foreach (var m in p.Memory) if (m.Act == "saw-takedown" || m.Act == "saw-carry" || m.Act == "saw-weapon-drawn") { aware = true; break; }
            if (!aware) { var f = FacingOf(p); if (f.HasValue && G.InCone(at, f.Value, R.FacingReachCm, R.FacingHalfCm, 0, e.Pos) && Sees(at, e.Pos)) aware = true; }
            return (aware, armed);
        }
        /// <summary>The way a person faces now, if the engine knows it: a walker on the move, the way they walk; else none.</summary>
        Pt? FacingOf(Person p)
        {
            var w = WalkerOf(p); if (w == null || !Start.Crowd || w.P.Down) return null;
            if (w.St.Length > 0)
            {
                if (w.Run != null && (w.St == "fleeing" || w.St == "returning" || w.St == "reporting" || w.St == "checking") && Tick >= w.RunStart)
                { long mm = (long)(Tick - w.RunStart) * (w.Pace > 0 ? w.Pace : w.St == "fleeing" ? R.CrowdRunSpeed : opSpeed); if (mm < w.Run.Length) return w.Run.Heading(mm); }
                return null;
            }
            var c = People2!;
            for (int i = 0; i + 1 < w.Place.Length; i++)
                if (Tick >= w.To[i] && Tick < w.From[i + 1] && w.Place[i] != "beyond" && w.Place[i + 1] != "beyond")
                {
                    var path = CrowdPath(w.Place[i], w.Place[i + 1]); int span = w.From[i + 1] - w.To[i];
                    return span <= 0 ? (Pt?)null : path.Heading(path.Length * (Tick - w.To[i]) / span);
                }
            return null;
        }

        string Takedown(Op op, Order o)
        {
            if (op.Id != OpId.Enforcer) return No("only-enforcer", "only the Enforcer takes people down (§03; brief 013)");
            if (Vale == ValeAt.WithEnforcer || Vale == ValeAt.Taking) return No("carry-vale", "the Enforcer is carrying Vale");
            if (op.Carrying.HasValue || op.CarryBody.Length > 0) return No("already-carrying", "already carrying");
            if (o.How != "knockout" && o.How != "kill") return No("takedown-how", "a takedown is a knockout or a kill");
            var p = PersonById(o.Target);
            if (p == null) return No("target-unknown", $"'{o.Target}' is no one here (unknown = reject)");
            if (p == ValeP && o.How == "knockout") return No("takedown-vale", "Vale is taken, not knocked out: use Take");
            var at = PosOfPerson(p);
            if (!at.HasValue) return p.Down ? No("person-down", $"{o.Target} is down") : No("person-away", $"{o.Target} is not out on the street");
            if (!G.Within(op.Pos, at.Value, R.TakedownReachCm)) return No("too-far", $"stand within {R.TakedownReachCm} cm of {o.Target}", cm: R.TakedownReachCm);
            var (aware, armed) = Awareness(p, at.Value);
            op.Walk = null; op.WalkMm = 0; op.Running = false;
            Actions.Add("takedown:" + o.How);
            if (aware && armed) { TakedownFails(op, p, at.Value); return ""; }
            var a = new TdAct { Id = Acts.Count, Victim = p.Id, How = o.How, P = p, At = at.Value, Scene = at.Value, Tick = Tick, Aware = aware, Kill = o.How == "kill" };
            Acts.Add(a);
            var b = new Body { Id = p.Id, P = p, W = WalkerOf(p), Act = a.Id, Seizing = true, At = at.Value, Dead = false, WakeAt = INF, Phase = (int)(Streams.Fnv1a32(p.Id) % (uint)R.BodyCheckTicks) };
            Seize(p, at.Value);
            Bodies.Add(b);
            int secs = (a.Kill ? R.KillS : R.KoS) + (aware ? R.StruggleS : 0);
            Engage(op, o, secs);
            Note("takedown.starts", $"The Enforcer takes {Who(p)} down ({o.How}, {secs} s){(aware ? ": he sees it coming and struggles" : "")}.");
            if (aware) Shout(a, at.Value, p);
            ScanAct(a, at.Value, "act", b);
            return "";
        }

        /// <summary>The armed target saw him coming (Motive §1.3): the takedown fails into a fight, under the drawn-weapon rule.</summary>
        void TakedownFails(Op op, Person p, Pt at)
        {
            Note("takedown.fails", $"{Who(p)} sees the Enforcer coming and draws: the takedown fails.");
            markedFoes.Add("enforcer");
            var pose = GuardPose();
            if (pose.HasValue && pose.Value.phase != "door" && !GuardHeld) { GuardHeld = true; heldPos = pose.Value.pos; heldFacing = pose.Value.facing; heldPhase = "drawing on the cell"; GuardAwayUntil = INF; }
            GuardEngaged = true; Remember(GuardP, "attacked-aware", "enforcer", true);
            RaiseAlert(R.DrawSeenSecurity, "takedown-fails:" + p.Id);
            CallSecurity(R.RespSeenS, "The guard calls in an attack.", at);
        }

        /// <summary>The person is taken off the street (a body from the order on: no witness, no reporter, no walk).</summary>
        void Seize(Person p, Pt at)
        {
            p.Down = true;
            var w = WalkerOf(p); if (w != null) { w.St = ""; w.Run = null; w.Pace = 0; }
            if (p == GuardP) { GuardDown = true; GuardEngaged = false; GuardHeld = false; }
            if (p == Driver) carStoppedAt = CarPos();
            foreach (var r in Reps) if (r.P == p && r.LandAt > Tick) r.Phase = "stopped";
        }
        static string Who(Person p) => p.Role.Length > 0 ? p.Role : p.Id;

        void LandTakedown(Op op, Order o)
        {
            var b = BodyById(o.Target); if (b == null || !b.Seizing) return;
            var a = ActOf(b); var p = b.P;
            b.Seizing = false; b.DownTick = Tick; b.Dead = a.Kill; b.At = a.At;
            Remember(p, a.Aware ? "attacked-aware" : "attacked", a.Aware ? "enforcer" : "", false);
            a.Lead = AddIncident("takedown", "takedown:" + p.Id, a.At);
            if (a.Kill)
            {
                b.WakeAt = INF;
                Chain.Add("killed:" + Who(p)); Note("takedown.dead", $"{Who(p)} is dead. The body lies where it fell.");
                if (p == ValeP) { Vale = ValeAt.Dead; Fail("vale-dead", "Vale is dead. The live capture objective has failed."); }
            }
            else
            {
                b.WakeAt = Tick + T(R.WakeS);
                Chain.Add("knocked-out:" + Who(p)); Note("takedown.out", $"{Who(p)} is out. He wakes in {R.WakeS} s.");
            }
        }

        // ---------------------------------------------------------------- carry, drop, stash, restrain (any operative)

        string CarryBody(Op op, Order o)
        {
            var b = BodyById(o.Target);
            if (b == null || b.Seizing) return No("no-body", $"there is no body of {o.Target} to carry");
            if (b.Carrier.HasValue) return No("already-carried", $"{o.Target} is already carried");
            if (!b.Open) return No("body-stashed", $"{o.Target} is hidden");
            if (!G.Within(op.Pos, BodyPos(b), actReach)) return No("stand-beside-body", $"stand beside {o.Target}", m: actReach / 100);
            Engage(op, o, R.CarryS); op.Gun.Order = ""; op.Gun.Target = "";
            return "";
        }
        string Drop(Op op, Order o)
        {
            if (op.CarryBody.Length == 0) return No("not-carrying", "carrying nobody");
            Engage(op, o, R.DropS); return "";
        }
        string Stash(Op op, Order o)
        {
            if (op.CarryBody.Length == 0) return No("not-carrying", "carry the body to the stash first");
            var s = StashById(o.Target); if (s == null) return No("target-unknown", $"no stash '{o.Target}' (unknown = reject)");
            if (!G.Within(op.Pos, s.Stand, R.AtReachCm)) return No("stand-at-stash", "stand at the stash", cm: R.AtReachCm);
            if (InStash(s.Id) >= HoldsOf(s)) return No("stash-full", $"the {s.Id} is full");
            Engage(op, o, s.Kind == "door" ? R.StashDoorS : R.StashContainerS); return "";
        }
        string Restrain(Op op, Order o)
        {
            var b = BodyById(o.Target);
            if (b == null || b.Seizing) return No("no-body", $"there is no body of {o.Target} to tie");
            if (b.Dead) return No("body-dead", $"{o.Target} is dead");
            if (b.Restrained) return No("already-restrained", $"{o.Target} is already tied");
            if (!b.Open || b.Carrier.HasValue) return No("body-stashed", $"{o.Target} is not lying here");
            if (b.P.Down == false) return No("no-body", $"{o.Target} is awake");
            if (!G.Within(op.Pos, BodyPos(b), actReach)) return No("stand-beside-body", $"stand beside {o.Target}", m: actReach / 100);
            Engage(op, o, R.RestrainS); return "";
        }

        /// <summary>The body-handling orders land (Land's switch hands them here).</summary>
        void LandBodyOrder(Op op, Order o)
        {
            switch (o.Do)
            {
                case "carry":
                    {
                        var b = BodyById(o.Target);
                        if (b == null || b.Carrier.HasValue || !b.Open || !b.P.Down) { Note("carry.missed", $"There is nobody to lift at {o.Target}'s place."); return; }
                        b.Carrier = op.Id; op.CarryBody = b.Id;
                        Note("carry.body", $"The {Sim.Op.Name(op.Id)} lifts {Who(b.P)}{(op.Id == OpId.Enforcer ? "" : " (slowly: only the Enforcer carries at a walk)")}.");
                        return;
                    }
                case "drop": PutDown(op, "drop"); return;
                case "stash":
                    {
                        var b = CarriedBody(op); var s = StashById(o.Target);
                        if (b == null || s == null) return;
                        b.Carrier = null; op.CarryBody = ""; b.Stash = s.Id; b.At = s.Stand;
                        if (!b.Dead && b.NextCallOut < 0 && b.WakeAt <= Tick) b.NextCallOut = Tick + T(R.CallOutS);
                        Actions.Add("stash:" + s.Kind); Note("stash." + s.Kind, $"{Who(b.P)} is hidden in the {s.Id}.");
                        return;
                    }
                case "restrain":
                    {
                        var b = BodyById(o.Target); if (b == null || b.Dead || !b.Open || !b.P.Down) return;
                        b.Restrained = true; b.NextCallOut = -1;
                        Note("restrain.done", $"{Who(b.P)} is tied.");
                        return;
                    }
            }
        }
        Body? CarriedBody(Op op) { if (op.CarryBody.Length == 0) return null; foreach (var b in Bodies) if (b.Id == op.CarryBody && b.Carrier == op.Id) return b; return null; }
        /// <summary>A carried body is laid down where the carrier stands (drop, cancel, the carrier hit or down).</summary>
        void PutDown(Op op, string why)
        {
            var b = CarriedBody(op); op.CarryBody = ""; if (b == null) return;
            b.Carrier = null; b.At = op.Pos;
            Note("carry.down", $"{Who(b.P)} is laid down ({why}).");
        }

        // ---------------------------------------------------------------- what the city sees (Pulse §2)

        /// <summary>Everyone on the street within witness range on a clear line of `at`, and every watcher that sees it, notices `what`
        /// (act, carry, body) once per incident (Pulse §2.2). b is null for a Take (Vale is no body). dry: only whether anyone would notice
        /// now, changing nothing (the Take's incident opens at its first sighting, so an unseen Take leaves no trace).</summary>
        bool ScanAct(TdAct a, Pt at, string what, Body? b, bool dry = false)
        {
            foreach (var (p, wp) in OnTheStreet())
            {
                if (p == a.P || a.Noticed.Contains(p.Id) || Accomplice(a, p)) continue; // rev 4: an accomplice is never a witness
                if (!G.Within(wp, at, witnessRange) || !Sees(wp, at)) continue;
                if (dry) return true;
                a.Noticed.Add(p.Id);
                PersonNotices(a, p, wp, what, b);
                if (Over) return true;
            }
            foreach (var w in WatchersNow())
            {
                if (a.Noticed.Contains(w)) continue;
                if (w == "guard") continue; // the door guard is a person: he notices as one (above), within witness range, and by his cone (below)
                if (!WatcherSees(w, at)) continue;
                if (dry) return true;
                a.Noticed.Add(w);
                WatcherNotices(a, w, what, b);
                if (Over) return true;
            }
            if (!a.Noticed.Contains(GuardP.Id) && !GuardAlly && !GuardDown && GuardP != a.P && GuardWatching(at))
            {
                if (dry) return true;
                a.Noticed.Add(GuardP.Id); PersonNotices(a, GuardP, GuardPose()!.Value.pos, what, b);
            }
            return false;
        }
        /// <summary>Where an act is now: its body (with its carrier, in its stash, where it lies), or for a Take, Vale where he is being taken.</summary>
        Pt ActAt(TdAct a, Body? b) => b != null ? BodyPos(b) : a.Take ? (ValePos() ?? a.At) : a.At;

        /// <summary>The city's eyes now: the cameras, the guard, the patrol, the response unit's officers and the response's units, by id.</summary>
        List<string> WatchersNow()
        {
            var l = new List<string>();
            foreach (var c in S.Cameras) l.Add("camera:" + c.Id);
            l.Add("guard"); l.Add("patrol");
            if (Tick >= SecurityArrives) l.Add("security");
            foreach (var u in Units) if (u.OnStreet(Tick) && !u.Patrol && !u.Legacy && !u.Gun.Down) l.Add(u.Id);
            return l;
        }

        /// <summary>A person notices an act, a carry or a body: the sighting (Trail and Standing once an incident, a turned contact's
        /// saw-violence move), then their reaction by the list (Pulse §2.3).</summary>
        void PersonNotices(TdAct a, Person p, Pt wp, string what, Body? b)
        {
            string by = what == "act" ? "enforcer" : what == "carry" && b != null && b.Carrier.HasValue ? Sim.Op.Name(b.Carrier.Value) : "";
            Remember(p, what == "act" ? "saw-takedown" : what == "carry" ? "saw-carry" : "found-body", by, true);
            if (a.Lead != null && !a.Lead.Witnesses.Exists(x => x.p == p)) a.Lead.Witnesses.Add((p, wp));
            if (what != "body") AtSight(a, p);
            if (p == GuardP && what != "body" && !GuardAlly) { SecuritySees(a, "guard", what, b); return; } // armed security engages (Motive §1.4)
            React(a, p, wp, what, b);
        }

        /// <summary>A witnessed act at sight (Motive §1.4): +witness Trail once an incident unless the witness owes the cell (Sky call 5),
        /// Standing once whoever saw it, a turned contact's saw-violence move.</summary>
        void AtSight(TdAct a, Person p)
        {
            a.Witnessed = true;
            if (!IsCellPerson(p) && !a.TrailPaid) { a.TrailPaid = true; AddTrail(R.WitnessTrail, "witnessed:takedown:" + a.Victim); }
            if (!a.StandingPaid) { a.StandingPaid = true; MoveStanding(R.Traceable["takedown"] * R.StandingStep, "witnessed:takedown:" + a.Victim); }
            if (p.Turned) LadderMove(p, "saw-violence");
        }

        /// <summary>Pulse's decision list, first row that holds (rulebook witness.rule).</summary>
        string Decide(TdAct a, Person p, string what)
        {
            int f = p.Sc[Rules.Fear];
            if (IsStaff(p)) return "radio";
            if (IsKin(p, a.P)) return f >= R.TerrifiedFear ? "flee" : "report";
            if (IsCellPerson(p)) return "quiet";
            if (p.Sc[Rules.Resentment] >= R.ScaleMax[Rules.Resentment]) return "report";
            if (f >= R.TerrifiedFear) return "flee";
            switch (p.Temperament)
            {
                case "loyal": return "report";
                case "idealist": return what == "body" ? "check" : "report";
                case "mercenary": return Alert >= R.MercenaryAlert ? "report" : "quiet";
                case "fearful": return what == "body" ? "avoid" : "flee";
                default: return "quiet";
            }
        }
        /// <summary>The verb a card shows for this person (Pulse §3.6): "?" until the cell knows the temperament, unless a row before the
        /// temperament's decides it (staff, kin, a contact).</summary>
        string VerbShown(TdAct a, Person p, string what)
        {
            if (IsStaff(p) || IsKin(p, a.P) || IsCellPerson(p) || p.KTemperament) return Decide(a, p, what);
            return "?";
        }

        void React(TdAct a, Person p, Pt wp, string what, Body? b)
        {
            string verb = Decide(a, p, what);
            bool kin = IsKin(p, a.P);
            bool shout = kin || (what != "body" && ((verb == "report" && p.Temperament == "idealist") || (verb == "flee" && p.Sc[Rules.Fear] >= R.ScreamFear)));
            Note("witness.react", $"{Who(p)} {(what == "act" ? "sees the takedown" : what == "carry" ? "sees a body carried" : "finds a body")}: {verb}.");
            if (shout) Shout(a, ActAt(a, b), p);
            var w = WalkerOf(p);
            switch (verb)
            {
                case "radio": StartReport(a, p, KindOf(a, b, what, true), wp, "radio", b); break;
                case "report": StartReport(a, p, KindOf(a, b, what, false), wp, what == "body" && !kin ? "walk" : "run", b); break; // kin run without hesitation (Pulse row 2)
                case "check": StartReport(a, p, "down", wp, "check", b); break;
                case "flee":
                    if (w != null && (w.St == "" || w.St == "returning")) { w.St = "startled"; w.At = wp; w.Run = null; w.RunStart = Tick; w.Pace = 0; lastShooterFor[p.Id] = ActAt(a, b); w.Until = Tick + (p.Temperament == "fearful" ? R.CrowdStartleFearfulTicks : R.CrowdStartleTicks); }
                    Tell(a, p, wp); break;
                default: Tell(a, p, wp); break; // quiet, avoid
            }
            if (b != null && b.Restrained && !b.Dead && (verb == "report" || verb == "check" || verb == "radio") && b.FreeAt < 0) b.FreeAt = Tick + T(R.WitnessReactS) + T(R.FreeS); // Pulse §2.1 free
        }
        /// <summary>What a report says (Motive §2.3): a takedown seen, a carry seen, a dead body (when the finder knows: staff), a person lying.</summary>
        string KindOf(TdAct a, Body? b, string what, bool knows) => what == "act" ? "seen" : what == "carry" ? "carry" : knows && b != null && b.Dead ? "dead" : "down";
        int SevOf(string kind) => kind switch
        {
            "seen" => R.AlertSeen, "carry" => R.AlertCarry, "dead" => R.AlertDead, "down" => R.AlertDown, "woken" => R.AlertWoken,
            "noise" => R.AlertNoise, "mark" => R.AlertMark, "missing" => R.AlertMissing, _ => R.AlertDown,
        };

        /// <summary>A scream (Pulse §2.1 shout): heard within witness.scream with no line. A hearer with a line within witness range has
        /// seen it already; the rest hold heard-alarm (a lead: place and time).</summary>
        void Shout(TdAct a, Pt at, Person by)
        {
            Note("witness.scream", $"{Who(by)} screams.");
            foreach (var (p, wp) in OnTheStreet())
            {
                if (p == by || p == a.P || a.Noticed.Contains(p.Id) || a.Heard.Contains(p.Id)) continue;
                if (!G.Within(wp, at, R.ScreamCm)) continue;
                a.Heard.Add(p.Id); Remember(p, "heard-alarm", "", false);
            }
        }

        /// <summary>A watcher notices (Pulse §3.2): a camera's watcher reports at its next scan; the patrol robot rings the desk; armed
        /// security that sees an act or a carry engages (Motive §1.4); the response's units find a body for the scene they hold.</summary>
        void WatcherNotices(TdAct a, string w, string what, Body? b)
        {
            string kind = KindOf(a, b, what, true);
            if (w.StartsWith("camera:", StringComparison.Ordinal))
            {
                int phase = (int)(Streams.Fnv1a32(w) % (uint)T(R.CameraScanS)), period = T(R.CameraScanS);
                int next = Tick + ((phase - Tick % period) % period + period) % period;
                AddRep(a, kind, w, null, ActAt(a, b), next + T(R.PostCallS), "watcher");
                Note("witness.camera", $"The {w.Substring(7)} camera records it; its watcher reads the screen at {Sim.Report.Clock(next)}.");
                return;
            }
            if (what != "body") { SecuritySees(a, w, what, b); return; }
            if (b == null) return;
            if (w == "patrol") { AddRep(a, kind, w, null, BodyPos(b), Tick + T(R.RobotReportS), "watcher"); Note("witness.patrol", "The patrol finds a body and rings the desk."); return; }
            FoundBySecurity(b, w); // the response finds it
        }

        /// <summary>Armed security sees violence (Motive §1.4): Alert to the drawn-weapon level at sight, the operative is a foe they engage,
        /// and they call it in: the incident lands at once and is confirmed by their own eyes.</summary>
        void SecuritySees(TdAct a, string w, string what, Body? b)
        {
            string actor = what == "carry" && b != null && b.Carrier.HasValue ? Sim.Op.Name(b.Carrier.Value) : "enforcer";
            markedFoes.Add(actor);
            Note("takedown.seen", $"The {w.Replace("camera:", "")} sees {(what == "carry" ? "a body carried" : a.Take ? "Vale taken" : "the takedown")}: Alert {Math.Max(Alert, R.DrawSeenSecurity)}.");
            if (w == "guard" && !GuardDown)
            {
                var pose = GuardPose(); if (pose.HasValue && pose.Value.phase != "door" && !GuardHeld) { GuardHeld = true; heldPos = pose.Value.pos; heldFacing = pose.Value.facing; heldPhase = "drawing on the cell"; GuardAwayUntil = INF; }
                GuardEngaged = true;
            }
            if (w == "patrol" && !PatrolChasing && !PatrolDown && PatrolAwayUntil <= Tick && PatrolUnit() == null)
            { chasePos = PatrolPose()!.Value.pos; PatrolChasing = true; chaseOp = Sim.Op.Parse(actor); patrolOrigin = OpOf(chaseOp).Pos; patrolReturning = false; PatrolGun.NoFoeSince = -1; Note("patrol.breaks", "The patrol breaks its route and turns on the cell."); }
            a.SecuritySaw = true;
            LandAct(a, what == "act" ? "seen" : "carry", w, null, ActAt(a, b), R.DrawSeenSecurity, "");
            Confirm(a, b, w);
        }

        // ---------------------------------------------------------------- reports in transit (Pulse §3.2)

        void AddRep(TdAct a, string kind, string by, Person? p, Pt place, int landAt, string phase, string stash = "")
        {
            var r = new Rep { Seq = repSeq++, Act = a.Id, Kind = kind, By = by, P = p, W = p == null ? null : WalkerOf(p), Place = place, Sev = SevOf(kind), LandAt = landAt, Phase = phase, Stash = stash };
            Reps.Add(r);
        }
        int Pending(TdAct a) { int n = 0; foreach (var r in Reps) if (r.Act == a.Id && r.Phase != "stopped" && r.Phase != "landed" && r.P != null) n++; return n; }

        /// <summary>The nearest report point by the grid walk (ties on id): the site's staffed doors and its call boxes. Returns its stand and
        /// the walk there, or null if none can be walked to.</summary>
        (string id, Pt at, Path path)? ReportPoint(Pt from)
        {
            (string id, Pt at, Path path)? best = null;
            var pts = new List<(string, Pt)>();
            foreach (var (id, at, kind) in S.Refuges) if (kind == "door") pts.Add((id, at));
            foreach (var cb in S.CallBoxes) pts.Add(cb);
            var a = S.Grid.NearestFree(from) ?? from;
            // nearest by the straight line first: a walk is never shorter than it, so once the next point's line is longer than the best
            // walk found nothing after it can win (the same choice as walking to every point, at a few A* instead of one a point)
            var order = new List<(long line, string id, Pt at, Pt b)>();
            foreach (var (id, at) in pts) { var b0 = S.Grid.NearestFree(at) ?? at; order.Add((G.Isqrt(G.D2(a, b0)) * 10, id, at, b0)); }
            order.Sort((x, y) => x.line != y.line ? x.line.CompareTo(y.line) : string.CompareOrdinal(x.id, y.id));
            foreach (var (line, id, at, b) in order)
            {
                if (best != null && line > best.Value.path.Length + 1000) break; // 1 m of slack for the integer rounding of both lengths
                var route = S.Grid.Route(a, b);
                var path = route == null ? null : new Path(route.Length >= 2 ? route : new[] { a, b });
                if (path == null) continue;
                if (best == null || path.Length < best.Value.path.Length || (path.Length == best.Value.path.Length && string.CompareOrdinal(id, best.Value.id) < 0)) best = (id, at, path);
            }
            return best;
        }

        /// <summary>A person starts a report (Pulse §3.2): staff radio from where they stand (react + postCall); anyone else walks to the
        /// nearest report point (walking for a body, running for an act or a carry) and calls (call); an Idealist first walks to the body
        /// and checks it (check). The cast stand where the mission keeps them and are timed by the same walk. At most witness.cap in transit.</summary>
        void StartReport(TdAct a, Person p, string kind, Pt from, string mode, Body? b)
        {
            if (Pending(a) >= R.ReportCap) return;
            var w = WalkerOf(p);
            int react = T(R.WitnessReactS);
            if (mode == "radio") { AddRep(a, kind, p.Id, p, ActAt(a, b), Tick + react + T(R.PostCallS), "radio"); Note("report.started", $"{Who(p)} radios it in."); return; }
            if (mode == "check" && b != null)
            {
                var to = BodyPos(b); var path = RouteOf(from, to); int speed = opSpeed;
                int arrive = Tick + react + (int)((Math.Max(0, path.Length - near * 10L) + speed - 1) / speed);
                if (w != null) { w.St = "checking"; w.At = from; w.Run = path; w.RunStart = Tick + react; w.Pace = speed; }
                var r = new Rep { Seq = repSeq++, Act = a.Id, Kind = "down", By = p.Id, P = p, W = w, Place = to, Sev = SevOf("down"), LandAt = INF, Phase = "check", PhaseUntil = arrive + T(R.BodyCheckS) };
                Reps.Add(r); Note("report.started", $"{Who(p)} goes to look."); return;
            }
            WalkToPhone(a, p, w, kind, from, mode == "run" ? R.CrowdRunSpeed : opSpeed, Tick + react, ActAt(a, b), "");
        }
        void WalkToPhone(TdAct a, Person p, Walker? w, string kind, Pt from, int pace, int start, Pt place, string stash)
        {
            var rp = ReportPoint(from);
            int walk = rp == null ? 0 : (int)((rp.Value.path.Length + pace - 1) / pace);
            int land = start + walk + T(R.CallS);
            if (w != null && rp != null) { w.St = "reporting"; w.At = from; w.Run = rp.Value.path; w.RunStart = start; w.Pace = pace; w.Refuge = rp.Value.id; }
            AddRep(a, kind, p.Id, p, place, land, "walk", stash);
            Note("report.started", $"{Who(p)} {(pace > opSpeed ? "runs" : "walks")} to {(rp?.id ?? "a phone")} to report it (lands {Sim.Report.Clock(land)}).");
        }

        /// <summary>One tick of the reports: a reporter taken down or carried drops his report; a checker who reaches the body learns its
        /// state and goes to the phone; a walker at the phone is off the street calling; a report lands at its tick (by order of landing,
        /// then of starting). By index: a finished check starts its walk to the phone as a new report (WalkToPhone adds to Reps), so the
        /// list grows inside the loop; a report added this tick is stepped this tick too, after the ones before it (brief 013 D: Waymark's
        /// "Collection was modified" at 5:34.1, an Idealist's check ending while the clerks stream out).</summary>
        void ReportsStep()
        {
            for (int i = 0; i < Reps.Count; i++)
            {
                var r = Reps[i];
                if (r.Phase == "stopped" || r.Phase == "landed") continue;
                if (r.P != null && r.P.Down && r.Kind != "woken") { r.Phase = "stopped"; Note("report.stopped", $"{Who(r.P)}'s report never lands."); continue; }
                if (r.Phase == "check" && Tick >= r.PhaseUntil)
                {
                    var b = BodyOf(Acts[r.Act].P);
                    bool there = b != null && b.Open && G.Within(BodyPos(b), r.Place, witnessRange);
                    string kind = there && b!.Dead ? "dead" : "down";
                    var from = PosOfWalkerNow(r.W) ?? r.Place;
                    r.Phase = "stopped"; // the check is done: the report walks on as a new one (it keeps its place in the cap)
                    WalkToPhone(Acts[r.Act], r.P!, r.W, kind, from, opSpeed, Tick, r.Place, "");
                    continue;
                }
                if (r.Phase == "walk" && r.W != null && r.W.St == "reporting" && r.W.Run != null && Tick >= r.W.RunStart && (long)(Tick - r.W.RunStart) * r.W.Pace >= r.W.Run.Length)
                { r.W.St = "calling"; r.W.At = r.W.Run.End; }
            }
            Rep? next;
            while ((next = NextLanding()) != null)
            {
                next.Phase = "landed";
                var a = Acts[next.Act];
                if (next.W != null && (next.W.St == "calling" || next.W.St == "reporting")) Return(next.W, next.W.Run?.End ?? next.W.At);
                if (next.P != null) Remember(next.P, "reported", "", false);
                LandAct(a, next.Kind, next.By, next.P, next.Place, next.Sev, next.Stash);
                if (next.P != null && next.Kind != "woken" && next.Kind != "mark") Tell(a, next.P, PosOfPerson(next.P) ?? next.Place);
                if (Over) return;
            }
        }
        Rep? NextLanding() { Rep? best = null; foreach (var r in Reps) if (r.Phase != "stopped" && r.Phase != "landed" && r.LandAt <= Tick && (best == null || r.LandAt < best.LandAt)) best = r; return best; }
        Pt? PosOfWalkerNow(Walker? w) => w == null ? null : CrowdPos(w);
        /// <summary>Back to their routine, at walking pace, from where they stand (the crowd's returning walk).</summary>
        void Return(Walker w, Pt from)
        {
            var home = RoutinePos(w, Tick);
            if (!home.HasValue) { w.St = ""; w.Run = null; w.Pace = 0; return; }
            w.Run = RouteOf(from, home.Value); w.RunStart = Tick; w.At = from; w.St = "returning"; w.Pace = 0;
        }

        /// <summary>A report lands at the desk (Motive §2.3; rulebook report.lands): Alert to its level (a knockout at most koAlertMax by
        /// itself), the incident's highest; the nth reported incident at least byIncident[n-1]; a seen takedown's and a woken victim's
        /// report charge the act's Trail; the response by the dial.</summary>
        void LandAct(TdAct a, string kind, string by, Person? p, Pt place, int sev, string stash)
        {
            if (!a.Kill && (kind == "down" || kind == "dead" || kind == "woken" || kind == "noise" || kind == "mark" || kind == "missing")) sev = Math.Min(sev, R.KoAlertMax);
            bool first = !a.Landed; a.Landed = true;
            int to = sev;
            if (first) { Reported++; var list = R.AlertByIncident; to = Math.Max(sev, list[Math.Min(Reported, list.Length) - 1]); }
            a.Alert = Math.Max(a.Alert, to);
            int before = Alert;
            RaiseAlert(to, $"report:{kind}:{a.Victim}");
            if ((kind == "seen" || kind == "woken") && !a.Take) ChargeAct(a); // a Take keeps its own Trail (cell.take)
            AddIncident("report", $"report:{kind}:{a.Victim}", place, p != null ? new[] { (p, PosOfPerson(p) ?? place) } : null);
            a.Scene = place;
            Note("report.landed", $"{(p != null ? Who(p) : by.Replace("camera:", "the camera "))}'s report lands: {kind} ({Who(a.P)}). Alert {Alert}.");
            // one incident, one dispatch (Aster 2026-10-16, brief 013 D: the desk sent the response again for every report of the same
            // incident). The first landed report dispatches; a later one that raises Alert tops the same dispatch up to the new Alert's
            // counts (the dial's counts, never a second set); any other later report only sends a unit already out to a new scene.
            if (first) Alarm(a, place, p, stash);
            else if (Alert > before) Alarm(a, place, p, stash, step: true);
            else CoverScene(a, place, stash, Tick + T(DialNow.DispatchS));
            HoldScene(a, kind, place); // rev 4: a found body brings the unit and holds the doors in its sight
        }

        /// <summary>The act's own Trail, once (Motive §2.3): the hit and the down a shot person costs, and the kill's.</summary>
        void ChargeAct(TdAct a)
        {
            if (a.ActTrail) return; a.ActTrail = true;
            AddTrail(R.HitTrail + R.DownTrail + (a.Kill ? R.KillTrail : 0), "takedown:" + a.How + ":" + a.Victim);
        }

        /// <summary>Telling a tie (Pulse §3.4): the first tie of the kinds in witness.tells, by id, who is indoors at a unit (a phone) or on
        /// the street within witness.tell: a told entry, facts only, one hop. A told Loyal person reports it from where they are.</summary>
        void Tell(TdAct a, Person p, Pt from)
        {
            if (People2 == null || !People2.Ties.TryGetValue(p.Id, out var ts)) return;
            if (IsCellPerson(p) && !IsKin(p, a.P)) return; // a person who owes the cell passes nothing on
            foreach (var kind in R.TellTies)
            {
                var ids = new List<string>(); foreach (var (k, wid) in ts) if (k == kind) ids.Add(wid);
                ids.Sort(StringComparer.Ordinal);
                foreach (var id in ids)
                {
                    var q = PersonById(id); if (q == null || q.Down || q == a.P || a.Noticed.Contains(q.Id)) continue;
                    var qw = WalkerOf(q); var qp = PosOfPerson(q);
                    bool indoors = qw != null && Indoors(qw);
                    if (!indoors && !(qp.HasValue && G.Within(qp.Value, from, R.TellCm))) continue;
                    var facts = FactsOfWitnessed(p);
                    q.Memory.Add(new Mem { Act = "told", By = p.Id, Place = q.Place, Tick = Tick, Witnessed = false, Told = string.Join("|", facts) });
                    a.Noticed.Add(q.Id);
                    Note("witness.told", $"{Who(p)} tells {Who(q)} ({kind}).");
                    if (q.Temperament == "loyal" && !IsCellPerson(q))
                    {
                        if (indoors) AddRep(a, "down", q.Id, q, a.Scene, Tick + T(R.CallS) + T(R.CallS), "radio"); // hears the call, then calls the desk
                        else if (qp.HasValue) WalkToPhone(a, q, qw, "down", qp.Value, opSpeed, Tick + T(R.WitnessReactS), a.Scene, "");
                    }
                    return;
                }
            }
        }
        /// <summary>The person's routine has them inside a unit now (a stop not on the street, not beyond the sheet).</summary>
        bool Indoors(Walker w)
        {
            for (int i = 0; i < w.Place.Length; i++) if (Tick >= w.From[i] && Tick < w.To[i]) return !w.Out[i] && w.Place[i] != "beyond" && !w.Place[i].StartsWith("home", StringComparison.Ordinal);
            return false;
        }

        // ---------------------------------------------------------------- the Take, witnessed (brief 013 D; Motive rev 4 R4.2, Sky 2026-10-16)

        /// <summary>A Take at Vale's own car with its driver turned happens inside the car (Sky, rev 4): an interior, off-screen (§01), with
        /// no witness window and nothing for the pickup camera to see; onlookers see a councillor get into his official car.</summary>
        public bool TakeInCar => Driver.Turned && !Driver.Down;
        /// <summary>An accomplice of the act is never its witness (rev 4 R4.2, the first row, ahead of kin): a person turned to enable this
        /// very act. For the Take (and the escort that is its tail), the driver turned to move the pickup or blind its camera.</summary>
        bool Accomplice(TdAct a, Person p) => a.Take && p == Driver && Driver.Turned;
        TdAct? TakeActNow() { foreach (var x in Acts) if (x.Take) return x; return null; }

        /// <summary>Vale's abduction as people see it (rev 4 R4.2; rulebook witness): any Take but the one inside his own car reads as an act for
        /// its first cell.take.struggle s (Vale resists when seized): everyone within witness range on a clear line and every watcher that
        /// sees him notices a takedown, with its reactions, report chain and Alert; the rest of the Take reads as nothing. Escorted on foot
        /// (held, with the Enforcer, out of the Office), he looks wrong only to his kin (lover, family) within witness range on a clear
        /// line, or to anyone within near on a clear line, who sees the grip: a carry seen (Alert 2 on landing). One incident for the Take
        /// and its escort, opened at the first sighting: an unseen Take leaves nothing. It is no assault: the Take keeps its own Trail
        /// (cell.take.trailUnturned); witness Trail and Standing move at sight. A live pickup camera still fails a Take in the open at the
        /// order (§07, abduction-seen-camera).</summary>
        void TakeScan()
        {
            if (Over) return;
            var at = ValePos(); if (!at.HasValue) return;
            if (Vale == ValeAt.Taking)
            {
                if (TakeInCar) return;
                var e = Enforcer; if (e.Busy == null || e.Busy.Do != "take") return;
                if (Tick >= e.BusyEnd - T(R.TakeS) + T(R.TakeStruggleS)) return; // past the struggle the Take reads as nothing
                var a = TakeActNow();
                if (a == null)
                {
                    if (!ScanAct(new TdAct { P = ValeP, Take = true, At = at.Value }, at.Value, "act", null, dry: true)) return;
                    a = OpenTakeAct(at.Value);
                }
                ScanAct(a, at.Value, "act", null);
            }
            else if (Vale == ValeAt.WithEnforcer && !Enforcer.Inside)
            {
                var a = TakeActNow();
                if (a == null)
                {
                    if (!ScanEscort(new TdAct { P = ValeP, Take = true, At = at.Value }, at.Value, dry: true)) return;
                    a = OpenTakeAct(at.Value);
                }
                ScanEscort(a, at.Value);
            }
        }
        TdAct OpenTakeAct(Pt at)
        {
            var a = new TdAct { Id = Acts.Count, Victim = ValeP.Id, How = "take", P = ValeP, At = at, Scene = at, Tick = Tick, Take = true };
            Acts.Add(a);
            a.Lead = AddIncident("takedown", "take:" + ValeP.Id, at); // the reactions log it (witness.react, witness.camera, takedown.seen)
            return a;
        }
        /// <summary>The escort (rev 4 R4.2): Vale's kin within witness range, or anyone within near, on a clear line, see the grip.</summary>
        bool ScanEscort(TdAct a, Pt at, bool dry = false)
        {
            foreach (var (p, wp) in OnTheStreet())
            {
                if (p == a.P || a.Noticed.Contains(p.Id) || Accomplice(a, p)) continue;
                if (!(G.Within(wp, at, near) || (G.Within(wp, at, witnessRange) && IsKin(p, a.P))) || !Sees(wp, at)) continue;
                if (dry) return true;
                a.Noticed.Add(p.Id);
                PersonNotices(a, p, wp, "carry", null);
                if (Over) return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- the bodies, a tick at a time

        void BodiesStep()
        {
            foreach (var b in Bodies.ToArray())
            {
                if (Over) return;
                var a = ActOf(b);
                if (b.Seizing) { ScanAct(a, BodyPos(b), "act", b); continue; } // the act is seen at any tick of it
                if (b.Carrier.HasValue) { ScanAct(a, BodyPos(b), "carry", b); continue; }
                if (b.FreeAt >= 0 && Tick >= b.FreeAt && b.Restrained) { b.Restrained = false; b.FreeAt = -1; Note("restrain.freed", $"{Who(b.P)} is untied."); }
                if (!b.Dead && Tick >= b.WakeAt && b.P.Down)
                {
                    if (b.Restrained) { } // awake, tied: waits for a finder
                    else if (!b.Open) { if (b.NextCallOut < 0) b.NextCallOut = Tick; } // awake in a stash: calls out
                    else { Wake(b, false); continue; }
                }
                if (!b.Open && !b.Dead && !b.Restrained && b.NextCallOut >= 0 && Tick >= b.NextCallOut && Tick >= b.WakeAt) { CallOut(b); b.NextCallOut = Tick + T(R.CallOutS); }
                if (!b.Open) { ServiceRound(b); continue; }
                if ((Tick - b.Phase) % R.BodyCheckTicks == 0) ScanAct(a, BodyPos(b), "body", b);
            }
        }

        /// <summary>A knocked-out victim wakes (Motive §3; Pulse §5): he staggers, then reports the assault whatever his temperament (a
        /// contact who owes the cell says nothing); staff radio. brought: the response brought him round (his statement, no report).</summary>
        void Wake(Body b, bool brought)
        {
            var a = ActOf(b); var p = b.P; var at = BodyPos(b);
            Bodies.Remove(b); p.Down = false; a.Woke = true;
            p.Sc[Rules.Fear] = R.Clamp(Rules.Fear, p.Sc[Rules.Fear] + 1); if (a.Aware) p.Sc[Rules.Resentment] = R.Clamp(Rules.Resentment, p.Sc[Rules.Resentment] + 1);
            if (p == GuardP) { GuardDown = false; GuardHeld = true; heldPos = at; heldFacing = S.GuardFacing; heldPhase = "woken"; GuardAwayUntil = INF; guardBackAt = Tick + T(R.StaggerS) + (brought ? 0 : T(R.PostCallS)); } // he radios, then walks back to his post
            if (p == Driver) carStoppedAt = null;
            var w = WalkerOf(p);
            if (w != null) { w.St = "woken"; w.At = at; w.Run = null; w.Pace = 0; w.Until = Tick + T(R.StaggerS); w.RunStart = Tick; }
            if (brought) { a.Statement = true; Note("victim.statement", $"{Who(p)} is brought round and gives his statement."); ChargeAct(a); if (w != null) w.Until = Tick; return; }
            Note("victim.wakes", $"{Who(p)} wakes.");
            if (IsCellPerson(p)) return; // a contact who owes the cell says nothing (Pulse §5)
            int start = Tick + T(R.StaggerS);
            if (IsStaff(p)) { AddRep(a, "woken", p.Id, p, at, start + T(R.PostCallS), "radio"); return; }
            var rp = ReportPoint(at); int walk = rp == null ? 0 : (int)((rp.Value.path.Length + opSpeed - 1) / opSpeed);
            AddRep(a, "woken", p.Id, p, at, start + walk + T(R.CallS), "walk");
            if (w != null && rp != null) { w.Run = rp.Value.path; w.Pace = opSpeed; w.Refuge = rp.Value.id; w.RunStart = start; }
        }

        /// <summary>A hidden, awake, untied victim calls out (Motive §3.3): heard within witness.scream with no line; a hearer who would report
        /// a body reports a noise from the stash (Alert noise), and its verify opens it.</summary>
        void CallOut(Body b)
        {
            var a = ActOf(b); var at = BodyPos(b);
            Note("victim.calls-out", $"A muffled voice from the {b.Stash}.");
            foreach (var (p, wp) in OnTheStreet())
            {
                if (a.Noticed.Contains(p.Id) || !G.Within(wp, at, R.ScreamCm)) continue;
                a.Noticed.Add(p.Id); Remember(p, "heard-alarm", "", false);
                string verb = Decide(a, p, "body");
                if (verb == "radio") AddRep(a, "noise", p.Id, p, at, Tick + T(R.WitnessReactS) + T(R.PostCallS), "radio", b.Stash);
                else if (verb == "report" || verb == "check") WalkToPhone(a, p, WalkerOf(p), "noise", wp, opSpeed, Tick + T(R.WitnessReactS), at, b.Stash);
            }
        }

        /// <summary>A container's round (Pulse §4.1; Sky call 9: in the mission only): every service.period, its phase from the container's
        /// id; the round robot opens it and rings the desk on a body.</summary>
        void ServiceRound(Body b)
        {
            var s = StashById(b.Stash); if (s == null || s.Kind != "container") return;
            int period = T(R.ServicePeriodS), phase = (int)(Streams.Fnv1a32(s.Id) % (uint)period);
            if ((Tick - phase) % period != 0) return;
            var a = ActOf(b);
            if (a.Noticed.Contains("round:" + s.Id)) return;
            a.Noticed.Add("round:" + s.Id);
            Note("stash.serviced", $"The round empties the {s.Id} and finds {Who(b.P)}.");
            b.Stash = ""; // out of the bin, beside it
            AddRep(a, b.Dead ? "dead" : "down", "round:" + s.Id, null, s.Stand, Tick + T(R.RobotReportS), "watcher");
        }

        // ---------------------------------------------------------------- the card

        /// <summary>The takedown card (Motive §5.4): the price, who would see it now (people with their verb, "?" until the temperament is
        /// known; watchers by the sight rule), what follows if seen, found or never found. Changes nothing.</summary>
        public TakedownQuote QuoteTakedown(string target, string how)
        {
            var q = new TakedownQuote();
            var p = PersonById(target); var e = Enforcer;
            if (p == null) { q.Refused = "no one here"; q.RefusedCode = "target-unknown"; return q; }
            if (p == ValeP && how == "knockout") { q.Refused = "Vale is taken, not knocked out: use Take"; q.RefusedCode = "takedown-vale"; }
            var at = PosOfPerson(p);
            if (!at.HasValue) { q.Refused = p.Down ? "down" : "not on the street"; q.RefusedCode = p.Down ? "person-down" : "person-away"; return q; }
            bool kill = how == "kill";
            var (aware, armed) = Awareness(p, at.Value);
            q.Armed = p == GuardP;
            bool known = p != GuardP || (Known("guard")?.Sight ?? false);
            q.Aware = known ? (aware ? "yes" : "no") : "?";
            q.IfAware = q.Armed ? "fight" : "struggle";
            q.S = (kill ? R.KillS : R.KoS) + (aware && known && !armed ? R.StruggleS : 0);
            q.Seen = SeenAt(at.Value);
            var probe = new TdAct { P = p, Kill = kill };
            foreach (var (w, wp) in OnTheStreet())
            {
                if (w == p || !G.Within(wp, at.Value, witnessRange) || !Sees(wp, at.Value)) continue;
                string verb = VerbShown(probe, w, "act");
                int land = -1;
                if (verb == "radio") land = R.WitnessReactS + R.PostCallS;
                else if (verb == "report") { var rp = ReportPoint(wp); land = R.WitnessReactS + R.CallS + (rp == null ? 0 : (int)((rp.Value.path.Length / R.CrowdRunSpeed + 9) / 10)); }
                q.People.Add((w.Id, w.Name, verb, land));
            }
            q.AlertIfSeen = Math.Max(Alert, R.AlertSeen);
            q.AlertIfFound = Math.Max(Alert, kill ? R.AlertDead : Math.Min(R.AlertDown, R.KoAlertMax));
            q.TrailIfFound = R.HitTrail + R.DownTrail + (kill ? R.KillTrail : 0); q.MenaceIfFound = kill;
            q.TrailIfMissing = kill ? R.MissingTrail : R.HitTrail + R.DownTrail;
            q.IfNeverFound = kill ? "missing" : "comes-home";
            if (!kill) q.WakesAt = Tick + T((kill ? R.KillS : R.KoS) + R.WakeS);
            if (IsStaff(p) && p.KRoutine) q.NextMark = NextMarkOf(p, Tick);
            if (kill && People2 != null && People2.Ties.TryGetValue(p.Id, out var ts)) foreach (var (k, _) in ts) if (R.CloseTies.Contains(k)) q.Ties++;
            // the response, were the body found here now by staff: the forecast landing, the level's dispatch, the officers' walk (rev 4
            // R4.1: a found body brings a unit at any Alert, from the nearest post, else the response entry; at Alert 2 the response unit)
            var lv = DialNow; int alertThen = q.AlertIfFound;
            {
                int landS = R.WitnessReactS + R.PostCallS;
                var origin = alertThen >= 2 && SecurityArrives == INF ? S.Stand(S.SecEntry) : OriginOf(at.Value, S.Posts);
                var route = S.Grid.Route(S.Grid.NearestFree(origin) ?? origin, S.Grid.NearestFree(at.Value) ?? at.Value);
                int walkS = route == null ? 0 : (int)((new Path(route).Length + secSpeed * Hz - 1) / (secSpeed * Hz));
                q.PoliceInS = landS + lv.DispatchS + walkS;
            }
            q.HoldsDoors.AddRange(DoorsInSightOf(at.Value));
            q.HoldS = alertThen >= 2 ? Math.Max(R.CheckS + R.SearchS, R.StandDownSearchS) : R.CheckS + R.SearchS;
            return q;
        }

        /// <summary>The state the hash reads: nothing at all until a takedown, a body, a report, a unit or a marked foe exists, so every run
        /// without them hashes as it always did.</summary>
        internal void HashTakedown(Hash.H h)
        {
            if (Acts.Count == 0 && Bodies.Count == 0 && Reps.Count == 0 && Units.Count == 0 && markedFoes.Count == 0 && missedMarks.Count == 0 && suspicious.Count == 0 && extraCivGuns.Count == 0) return;
            h.S("takedown");
            foreach (var a in Acts)
            {
                h.I(a.Id).S(a.Victim).S(a.How).P(a.At).P(a.Scene).I(a.Tick).Z(a.Aware).I(a.Alert).Z(a.Landed).Z(a.Witnessed).Z(a.TrailPaid).Z(a.StandingPaid).Z(a.ActTrail).Z(a.Confirmed).Z(a.SecuritySaw).Z(a.MarkMissed).Z(a.Woke).Z(a.Statement);
                foreach (var x in a.Noticed) h.S(x); h.I(-1); foreach (var x in a.Heard) h.S(x); h.I(-1); foreach (var x in a.Opened) h.S(x); h.I(-1);
            }
            foreach (var b in Bodies) { h.S(b.Id).I(b.Act).Z(b.Dead).Z(b.Seizing).Z(b.Restrained).P(BodyPos(b)).I(b.Carrier.HasValue ? (int)b.Carrier.Value : -1).S(b.Stash).I(b.DownTick).I(b.WakeAt).I(b.NextCallOut).I(b.FreeAt); }
            foreach (var r in Reps) h.I(r.Seq).I(r.Act).S(r.Kind).S(r.By).P(r.Place).I(r.Sev).I(r.LandAt).S(r.Phase).I(r.PhaseUntil).S(r.Stash);
            foreach (var x in markedFoes) h.S(x);
            foreach (var x in missedMarks) h.S(x);
            foreach (var kv in suspicious) h.S(kv.Key).I(kv.Value);
            foreach (var g in extraCivGuns) Hash.HashGun(h, civGuns[g]);
            foreach (var op in Ops) h.S(op.CarryBody);
            HashResponse(h);
        }
    }
}
