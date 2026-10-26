// The rival crew (brief 059 §2, K7–K11; GDD v0.25.0 §07 rivals; Pulse briefs/059/pulse/rival-routine.md, Motive briefs/059/motive/rulings.json;
// rulebook `rivals`). Five people and one car, on their own timetable, computed back from the 3:10 park so it is a fixed function of the site
// and the rulebook:
//  · car:rivals drives the drive grid at car.pace between the site's rivals.car points (entry → drop → park), its legs each a CarProfile in
//    whole ticks. Before it enters the crew is nowhere on the map (Off). rival-1..3 step out at the drop rivals.step apart and walk to the first
//    rivals.posted spots of the getaway car's stand (each in id order taking the shortest grid walk, ties the lower spot), and post there with
//    their beats facing the car; rival-4, the lookout, steps out rivals.out after the park and posts at rivals.posts.lookout facing the official
//    bay; rival-5, the wheelman, stays aboard.
//  · Follows (Pulse §6): the getaway car's Drive (F1: the three re-plan rivals.follow later and go at rivals.followPace, Sky's S3); Vale's car
//    leaving the bay with Vale in it (F2: the car to rivals.car.tail, the wheelman on foot to rivals.posts.valeDoor; within act reach of Vale's
//    car before the Take is done, the rivals reach Vale); the false tip (F3: the car and wheelman off the way they came, back at the park
//    cell.mislead.away after it lands); the turned driver's word (F4: the lookout walks over, they speak rivals.warn, he leaves the map); gunfire
//    (F5: the crew makes for the Office front, the car to rivals.car.front); the safe final fight (F6: the car to the stand's rival stop, the
//    riders out there for the stand's spots by role; the crew on the map are the fight's hostiles, no fresh spawns).
//  · Every state is a function of ticks and orders, hashed (HashCrew). Beat names and RivalNext are host reads, never hashed; no rule reads a
//    beat name. Ties break on the lower id, spots included. The crew raises no Alert or Trail by any of this (rivals don't report).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public enum CrewCar { Off, Entering, Dropping, Rounding, Parked, Tail, TipOut, Away, TipBack, ToStand, Stopped, Front }
    public enum CrewMode { Aboard, Walking, Posted, Talking, Boarding, Gone }

    /// <summary>One of the crew (brief 059 K8): rival-1..3 the posted three, rival-4 the lookout, rival-5 the wheelman. His Gun is the same gun
    /// in the final fight; H is his final-fight body once he is in it (it then owns his place).</summary>
    public sealed class CrewMember
    {
        public string Id = "", Role = ""; public int N; public Gun Gun = null!;
        public CrewMode Mode = CrewMode.Aboard; public int ModeTick;
        public int OutAt = int.MaxValue / 4;                     // the tick he steps out of the car (aboard until then)
        public Path? Walk; public int WalkStart, Speed;          // Walking
        public Pt Place, Base; public int PostTick = -1; public string Post = ""; // Posted (spot, lookout, door, front) or Talking (word)
        public int Spot = -1; public string SpotStand = "";      // his ambush spot (0-based) and the stand it belongs to
        public string Goal = "";                                 // what he does when his walk ends: spot, lookout, word, exit, door, front, board, fight
        public int Until = int.MaxValue / 4;                     // Talking, Boarding: when it ends
        public Pt DownAt; public bool Fight;                     // where he fell; he is a final-fight body (Hostile H)
        public Hostile? H;
        public KiteDisp? Disp;                                   // drawn: the kite's walk off his line, or the press (fight.kite, ai.intent.rivals.press)
        /// <summary>Engine 0.33.0 (Motive M059-3.1): the member as a person for the takedown and the body orders (job "rival"; never in Game.People,
        /// so no witness, report, statement, check-in or missing rule reads him).</summary>
        public Person P = null!;
        /// <summary>Engine 0.33.0 (Motive M059-S1.6, M059-3.4): the agents he saw take one of the crew down, tried it on him, or took him down:
        /// his cause to fire on them (ai.intent foe crewmate).</summary>
        public readonly SortedSet<string> Avenge = new SortedSet<string>(StringComparer.Ordinal);
        public bool OnFoot => Mode == CrewMode.Walking || Mode == CrewMode.Posted || Mode == CrewMode.Talking;
    }

    public sealed partial class Game
    {
        public readonly List<CrewMember> Crew = new List<CrewMember>();
        public CrewCar CarSt = CrewCar.Off; public int CarStTick;
        Path? ccPath; long[]? ccProf; int ccStart; Pt ccAt, ccHead; string ccStop = "";
        /// <summary>The default timetable's ticks (Pulse §4): the car enters, stops at the drop, leaves it, parks (RivalAt).</summary>
        public int CrewEntry, CrewDropArrive, CrewDropLeave;
        int crewNextAt = INF; string crewNext = "";               // the car's next scheduled move (leave the drop, re-enter after a tip, …)
        /// <summary>The car's next scheduled move and its tick (a read; never hashed beyond the state it is).</summary>
        public (int tick, string what) CrewNextMove => (crewNextAt, crewNext);
        int followAt = INF; string followStand = "";             // F1: the three re-plan for this stand at this tick
        int tailAt = INF;                                        // F2: the car follows Vale's car at this tick
        int warnAt = INF, driverTurnedAt = -1; bool warnDecided;  // F4: the lookout leaves his post for the word at this tick
        bool crewLate;                                           // F6: the car has been sent to the fight's stand
        public bool RivalsWatched;                               // a Watch on the rivals has taught their timetable (RivalNext shows)
        public string CrewStandId = "";                          // the stand the three are posted round (or walking to)
        public bool CrewBrokenPre;                               // the crew broke before the final fight (Motive M059-3.5)

        CrewRules Cr => R.Crew;
        static readonly string[] CrewIds = { "rival-1", "rival-2", "rival-3", "rival-4", "rival-5" };
        public CrewMember? CrewOf(string id) { foreach (var m in Crew) if (m.Id == id) return m; return null; }
        static bool IsCrewId(string id) => Array.IndexOf(CrewIds, id) >= 0;
        CrewMember Lookout => Crew[3];
        CrewMember Wheelman => Crew[4];

        void InitCrew()
        {
            string[] roles = { "posted", "posted", "posted", "lookout", "wheelman" };
            for (int i = 0; i < 5; i++) Crew.Add(new CrewMember { Id = CrewIds[i], N = i + 1, Role = roles[i], Gun = NewGun(CrewIds[i], PistolW, false, R.HealthRival), P = new Person { Id = CrewIds[i], Job = Person.RivalJob } });
            foreach (var id in Start.CrewGone) { var gm = CrewOf(id) ?? throw new FormatException($"start crewGone: no member '{id}'"); gm.Gun.Down = true; gm.Mode = CrewMode.Gone; } // test only: killed before the mission, no body
            var a = CarRoute(S.RivalCarEntry, S.RivalCarDrop); var b = CarRoute(S.RivalCarDrop, S.RivalsPark);
            CrewDropLeave = RivalAt - (CarProfile(b).Length - 1);
            CrewDropArrive = CrewDropLeave - T(Cr.DropS);
            CrewEntry = CrewDropArrive - (CarProfile(a).Length - 1);
            if (CrewEntry < 1) // a start that moves the park this early (a test's rivalsAt): the car appears at the drop already stopped
            { CrewEntry = 1; if (CrewDropArrive < 1) { CrewDropArrive = 1; CrewDropLeave = 1 + T(Cr.DropS); } }
        }

        Path CarRoute(string from, string to) => CarRouteP(S.CarStand(from), S.CarStand(to));
        /// <summary>A read (scenarios, the debug view; never hashed): the ticks of the rival car's drive between two of its points (the profile's length).</summary>
        public int CarProfileTicks(string from, string to) => CarProfile(CarRoute(from, to)).Length;
        Path CarRouteP(Pt from, Pt to) => new Path(S.Drive.CarRoute(from, to, S.DriveLaneCm) ?? new[] { from, to });

        // ---------------------------------------------------------------- the car

        void CarLeg(CrewCar st, Path p, string stop)
        {
            CarSt = st; CarStTick = Tick; ccPath = p; ccProf = CarProfile(p); ccStart = Tick; ccStop = stop;
            if (ccProf.Length <= 1) CarArrive(); // already there
        }
        void CarHalt(CrewCar st, string stop, Pt at, Pt head) { CarSt = st; CarStTick = Tick; ccPath = null; ccProf = null; ccAt = at; ccHead = head; ccStop = stop; }
        bool CarMoving => ccPath != null && ccProf != null && (CarSt == CrewCar.Entering || CarSt == CrewCar.Rounding || CarSt == CrewCar.Tail || CarSt == CrewCar.TipOut || CarSt == CrewCar.TipBack || CarSt == CrewCar.ToStand || CarSt == CrewCar.Front);
        long CcMm(int tick) { int k = tick - ccStart; var d = ccProf!; return k <= 0 ? 0 : k >= d.Length ? d[d.Length - 1] : d[k]; }
        /// <summary>Where the rival car is and which way it points, or null while it is off the map (Off, Away).</summary>
        public (Pt pos, Pt heading)? RivalCarPose()
        {
            if (CarSt == CrewCar.Off || CarSt == CrewCar.Away) return null;
            if (CarMoving) { long mm = CcMm(Tick); return (ccPath!.At(mm), ccPath.Heading(mm)); }
            return (ccAt, ccHead);
        }
        Pt FacingOf(string stop) => stop == S.RivalCarEntry ? S.RivalFacingEntry : stop == S.RivalCarDrop ? S.RivalFacingDrop : stop == S.RivalsPark ? S.RivalFacingPark : stop == S.RivalCarTail ? S.RivalFacingTail : stop == S.RivalCarFront ? S.RivalFacingFront : (ccPath != null ? ccPath.Heading(ccPath.Length) : new Pt(0, 100));
        /// <summary>The car reaches the end of its leg.</summary>
        void CarArrive()
        {
            var end = ccPath!.End; var head = FacingOf(ccStop); var was = CarSt;
            switch (was)
            {
                case CrewCar.Entering when ccStop == S.RivalCarDrop && !crewLate:
                    CarHalt(CrewCar.Dropping, ccStop, end, head);
                    for (int k = 0; k < Cr.Posted; k++) if (Crew[k].Mode == CrewMode.Aboard && Up(Crew[k])) Crew[k].OutAt = Tick + T(Cr.StepS) * (k + 1);
                    crewNextAt = CrewDropLeave > Tick ? CrewDropLeave : Tick + T(Cr.DropS); crewNext = "leave-drop";
                    Note("rivals.drop", "The rival car stops by the square. Three men get out and walk to posts round our car."); return;
                case CrewCar.Rounding: case CrewCar.TipBack:
                    CarHalt(CrewCar.Parked, ccStop, end, head);
                    if (was == CrewCar.Rounding)
                    {
                        Note("rivals.arrive", "A rival car parks across from Vale's car. A lookout gets out to watch the official door.");
                        if (Lookout.Mode == CrewMode.Aboard && Up(Lookout)) Lookout.OutAt = Tick + T(Cr.OutS);
                    }
                    else
                    {
                        Note("rivals.back", "The rival car is back. It parks across from Vale's car.");
                        if (CrewBrokenPre) // M059-3.5: back from a break: whoever is aboard but the wheelman gets out to post again
                        {
                            CrewBrokenPre = false; int k = 0;
                            foreach (var m in Crew) if (m.Mode == CrewMode.Aboard && Up(m) && m != Wheelman) m.OutAt = Tick + T(Cr.OutS) + T(Cr.StepS) * k++;
                        }
                        TailCheck();
                    }
                    return;
                case CrewCar.TipOut:
                    CarSt = CrewCar.Away; CarStTick = Tick; ccPath = null; ccProf = null;
                    Note("rivals.tip-leaves", "The rival car leaves the district on the false tip."); return;
                case CrewCar.Tail:
                    CarHalt(CrewCar.Stopped, ccStop, end, head);
                    if (Wheelman.Mode == CrewMode.Aboard && Up(Wheelman)) { Wheelman.OutAt = Tick + T(Cr.OutS); Wheelman.Goal = "door"; }
                    return;
                default: // ToStand, Front, a late Entering: stopped for good, the riders out in id order
                    CarHalt(CrewCar.Stopped, ccStop, end, head);
                    int n = 0;
                    foreach (var m in Crew)
                    {
                        if (m.Mode != CrewMode.Aboard || !Up(m) || m.OutAt < INF) continue;
                        if (was == CrewCar.Front && m == Wheelman) continue; // drawn: the wheelman stays aboard (Pulse §6 F5)
                        int at = Tick + (was == CrewCar.Entering ? T(Cr.StepS) * (m.N) : T(Cr.OutS) + T(Cr.StepS) * n);
                        m.OutAt = Math.Max(at, Tick + 1); n++;
                    }
                    if (was == CrewCar.Front) Note("rivals.drawn-stop", "The rival car stops on Rocket Lane by the Office front.");
                    else if (was == CrewCar.ToStand || was == CrewCar.Entering) Note("rivals.late", "The rival car pulls up for the fight. The rest of the crew get out.");
                    return;
            }
        }

        /// <summary>One tick of the crew (the car first, then its riders, then the others, in id order). Called in the schedule's place.</summary>
        void CrewStep()
        {
            if (Range) return;
            // the drawn crew comes in from off the map at RivalAt (F5), else the timetable's entry
            if (CarSt == CrewCar.Off && Tick == CrewEntryNow && !(Wheelman.Gun.Down && Wheelman.Mode == CrewMode.Gone)) Enter(); // a wheelman killed before the mission (Start.CrewGone, test only) drives no car in
            if (CarMoving && Tick >= ccStart + ccProf!.Length - 1) CarArrive();
            if (crewNextAt == Tick) CrewNext();
            if (tailAt == Tick) Tail();
            if (followAt == Tick) FollowCar();
            WarnStep();
            foreach (var m in Crew) MemberStep(m);
            YardReach();
        }
        int CrewEntryNow => RivalsDrawn && crewDrawnEntry < INF ? crewDrawnEntry : CrewEntry;
        int crewDrawnEntry = INF;

        void Enter()
        {
            RivalsArrived = true;
            if (RivalsDrawn) // F5, drawn before 2:35: straight to the Office front
            { CarLeg(CrewCar.Front, CarRoute(S.RivalCarEntry, S.RivalCarFront), S.RivalCarFront); Note("rivals.coming", "A rival car turns in off Orbit Walk, racing for the gunfire."); return; }
            if (crewLate) // F6: the cell has Vale before they come: straight to the stand's rival stop
            { var st = FightStand; CarLeg(CrewCar.Entering, CarRoute(S.RivalCarEntry, st.RivalStop), st.RivalStop); Note("rivals.coming", "A rival car turns in off Orbit Walk. It is after Vale too."); return; }
            CarLeg(CrewCar.Entering, CarRoute(S.RivalCarEntry, S.RivalCarDrop), S.RivalCarDrop);
            Note("rivals.coming", "A rival car turns in off Orbit Walk. It is after Vale too.");
        }

        void CrewNext()
        {
            var what = crewNext; crewNextAt = INF; crewNext = "";
            switch (what)
            {
                case "leave-drop":
                    if (CarSt != CrewCar.Dropping) return;
                    if (crewLate) { LateFromDrop(); return; }
                    CarLeg(CrewCar.Rounding, CarRoute(S.RivalCarDrop, S.RivalsPark), S.RivalsPark); return;
                case "tip-back": // the tip runs out: it re-enters to park again (or, drawn, for the front; in a fight, for the stand's stop)
                    if (CarSt != CrewCar.Away) return;
                    if (RivalsDrawn) { CarLeg(CrewCar.Front, CarRoute(S.RivalCarEntry, S.RivalCarFront), S.RivalCarFront); return; }
                    if (FightBranch == "safe" && FightOverTick == INF) { CarLeg(CrewCar.ToStand, CarRoute(S.RivalCarEntry, FightStand.RivalStop), FightStand.RivalStop); Note("rivals.back", "The rival car is back from the false tip, and comes for our car."); return; }
                    CarLeg(CrewCar.TipBack, CarRoute(S.RivalCarEntry, S.RivalsPark), S.RivalsPark); return;
                case "lookout-ride": // F6: the lookout is aboard: the car rides to the stand's stop
                    if (CarSt != CrewCar.Parked) return;
                    CarLeg(CrewCar.ToStand, CarRouteP(ccAt, S.CarStand(FightStand.RivalStop)), FightStand.RivalStop); return;
            }
        }

        // ---------------------------------------------------------------- members

        static bool Up(CrewMember m) => !m.Gun.Down && m.Mode != CrewMode.Gone;
        Pt StepOutAt() { var c = RivalCarPose()?.pos ?? S.Stand(S.RivalsPark); return Walk.NearestFree(c) ?? c; }

        void StartWalk(CrewMember m, Pt from, Pt to, string goal, int speed)
        {
            var a = Walk.NearestFree(from) ?? from; var b = Walk.NearestFree(to) ?? to;
            var route = (goal == "spot" ? RouteRoundHold(a, b) : null) ?? Walk.Route(a, b) ?? new[] { a, b }; // engine 0.34.0 (Motive M067-SQ1): to his post round the car's hold
            m.Walk = new Path(route); m.WalkStart = Tick; m.Speed = Math.Max(1, speed); m.Goal = goal; m.Mode = CrewMode.Walking; m.ModeTick = Tick; m.Disp = null;
            if (m.Walk.Length == 0) ArriveWalk(m);
        }
        Pt WalkPos(CrewMember m, int tick) => m.Walk!.At((long)Math.Max(0, tick - m.WalkStart) * m.Speed);
        bool WalkDone(CrewMember m, int tick) => (long)Math.Max(0, tick - m.WalkStart) * m.Speed >= m.Walk!.Length;

        void MemberStep(CrewMember m)
        {
            if (m.Fight || !Up(m)) return; // a fight body is the fight's (FightBody); a member down or gone does nothing
            if (m.Mode == CrewMode.Aboard && Tick >= m.OutAt) { StepOut(m); if (m.Fight) return; }
            if (m.Mode == CrewMode.Walking && m.Disp == null && m.Walk != null && WalkDone(m, Tick)) ArriveWalk(m);
            if ((m.Mode == CrewMode.Talking || m.Mode == CrewMode.Boarding) && Tick >= m.Until) EndAct(m);
        }

        /// <summary>A rider steps out beside the car and walks to where he goes.</summary>
        void StepOut(CrewMember m)
        {
            m.OutAt = INF; var from = StepOutAt(); m.Place = from;
            if (FightBranch == "safe" && FightOverTick == INF && JoinFightOnFoot(m, from)) return; // F6: out for the fight
            if (RivalsDrawn) { StartWalk(m, from, FrontPlace(m), "front", rivSpeed); return; } // F5
            if (m.Goal == "door") { StartWalk(m, from, S.Stand(S.RivalValeDoor), "door", rivSpeed); m.Goal = "door"; return; } // F2
            if (m == Lookout) { StartWalk(m, from, S.Stand(S.RivalLookout), "lookout", rivSpeed); return; }
            if (m.N <= Cr.Posted) { var st = StandForCrew(); AssignSpot(m, st, from); StartWalk(m, from, S.Stand(st.Ambush[m.Spot]), "spot", rivSpeed); return; }
            m.Mode = CrewMode.Posted; m.Post = "car"; m.PostTick = Tick; m.Base = RivalCarPose()?.heading ?? new Pt(0, 100); // the wheelman out with nowhere to go: stands by the car
        }

        void ArriveWalk(CrewMember m)
        {
            var end = m.Walk!.End; var back = Back(m.Walk);
            m.Walk = null; m.Place = end;
            switch (m.Goal)
            {
                case "spot": Post(m, "spot", end, FaceFrom(end, CrewStand.CarAt)); Note("rivals.post", $"{m.Id} takes his post round our car."); break;
                case "lookout": Post(m, "lookout", end, FaceFrom(end, S.At(S.CarPickup))); Note("rivals.post", $"{m.Id}, the lookout, watches the official door."); break;
                case "door": Post(m, "door", end, FaceFrom(end, CarPos())); Note("rivals.at-vale", "The rival wheelman reaches Vale's car."); break;
                case "front": Post(m, "front", end, back); break;
                case "word": m.Mode = CrewMode.Talking; m.ModeTick = Tick; m.Until = Tick + T(Cr.WarnS); m.Post = "word"; m.Base = FaceFrom(end, DriverPos() ?? S.Stand(S.CarDriver)); break;
                case "exit": m.Mode = CrewMode.Gone; m.ModeTick = Tick; Note("rivals.lookout-gone", "The rival lookout walks off down Apron Road and is gone."); break;
                case "board": m.Mode = CrewMode.Boarding; m.ModeTick = Tick; m.Until = Tick + T(Cr.StepS); break;
                case "aboard": m.Mode = CrewMode.Aboard; m.ModeTick = Tick; m.Post = ""; break; // engine 0.33.0: a woken member back in his car (M059-3.4)
                case "leave": m.Mode = CrewMode.Gone; m.ModeTick = Tick; Note("rivals.gone", $"{m.Id} of the rival crew walks off the map."); break; // engine 0.33.0: a woken member with his car gone
                case "flee": // M059-3.5: aboard; when the last of them is in, the car leaves
                    m.Mode = CrewMode.Aboard; m.ModeTick = Tick; m.Post = ""; m.Spot = -1; m.SpotStand = "";
                    if (!Crew.Exists(x => Up(x) && x.Mode == CrewMode.Walking && x.Goal == "flee")) CrewLeaves();
                    break;
                default: Post(m, "front", end, back); break;
            }
        }
        void Post(CrewMember m, string post, Pt at, Pt face) { m.Mode = CrewMode.Posted; m.ModeTick = Tick; m.Post = post; m.Place = at; m.Base = face; m.PostTick = Tick; }
        static Pt FaceFrom(Pt from, Pt to) { var f = to - from; return f == new Pt(0, 0) ? new Pt(0, 100) : f; }

        void EndAct(CrewMember m)
        {
            if (m.Mode == CrewMode.Talking) // F4: the word is over: he leaves the map
            {
                m.Until = INF; lookoutWarned = true; StartWalk(m, m.Place, S.Stand(S.RivalLookoutExit), "exit", rivSpeed);
                Note("rivals.warned", "The driver's word is out: the rival lookout leaves his post.");
                return;
            }
            // boarding (F6): he is in the car; the car rides to the stand's stop
            m.Until = INF; m.Mode = CrewMode.Aboard; m.ModeTick = Tick;
            if (CarSt == CrewCar.Parked) { crewNextAt = Tick + 1; crewNext = "lookout-ride"; }
        }
        bool lookoutWarned;

        // ---------------------------------------------------------------- where the three post

        /// <summary>The stand the cell's car is at, or driving to (the stand that decides where the three post).</summary>
        Stand StandForCrew() => GetawayDest ?? CurStand;
        Stand CrewStand => S.StandById(CrewStandId) ?? CurStand;
        /// <summary>The spot assignment (Pulse §3): in id order each takes, of the stand's first rivals.posted spots not yet taken by a lower id, the
        /// one with the shortest grid walk from where he stands; ties the lower spot.</summary>
        void AssignSpot(CrewMember m, Stand st, Pt from)
        {
            CrewStandId = st.Id;
            var taken = new HashSet<int>();
            foreach (var o in Crew) if (o != m && o.N < m.N && o.N <= Cr.Posted && o.SpotStand == st.Id && o.Spot >= 0 && Up(o)) taken.Add(o.Spot);
            int best = -1; long bd = long.MaxValue;
            for (int k = 0; k < Cr.Posted && k < st.Ambush.Length; k++)
            {
                if (taken.Contains(k)) continue;
                long d = WalkMm(from, S.Stand(st.Ambush[k]));
                if (d < bd) { bd = d; best = k; }
            }
            m.Spot = best < 0 ? Math.Min(m.N - 1, st.Ambush.Length - 1) : best; m.SpotStand = st.Id;
        }
        long WalkMm(Pt a, Pt b) { var r = Walk.Route(Walk.NearestFree(a) ?? a, Walk.NearestFree(b) ?? b); return r == null ? long.MaxValue / 4 : new Path(r).Length; }

        /// <summary>F1: the getaway car leaves its stand on a Drive: the posted three (posted, walking to a post, or still aboard at the drop) re-plan
        /// rivals.follow later for the new stand's spots, in id order from where each stands, at rivals.followPace.</summary>
        void CrewCarMoves(Stand to)
        {
            if (Range || FightBranch.Length > 0) return;
            followAt = Tick + T(Cr.FollowS); followStand = to.Id;
        }
        void FollowCar()
        {
            followAt = INF; var st = S.StandById(followStand); if (st == null || RivalsDrawn || FightBranch.Length > 0) return;
            if (CrewStandId == st.Id && Crew.TrueForAll(m => m.N > Cr.Posted || m.SpotStand == st.Id || !Up(m))) return; // already theirs: a no-op
            CrewStandId = st.Id; bool any = false;
            for (int k = 0; k < Cr.Posted; k++)
            {
                var m = Crew[k]; if (!Up(m) || m.Fight) continue;
                if (m.Mode == CrewMode.Aboard) { m.SpotStand = ""; m.Spot = -1; continue; } // still in the car at the drop: he decides at his step-out
                if (!(m.Mode == CrewMode.Posted && m.Post == "spot") && !(m.Mode == CrewMode.Walking && m.Goal == "spot")) continue;
                var from = MemberPos(m, Tick); AssignSpot(m, st, from);
                StartWalk(m, from, S.Stand(st.Ambush[m.Spot]), "spot", Cr.FollowCmS); any = true;
            }
            if (any) Note("rivals.follow-car", "The rivals posted round our car go after it.");
        }

        // ---------------------------------------------------------------- the tail (F2)

        /// <summary>Vale's car starts to drive from the official bay with Vale in it (CarTimeline): the rival car follows rivals.follow later.</summary>
        void ValeCarMoves() { if (CarSt == CrewCar.Parked) tailAt = Tick + T(Cr.FollowS); }
        /// <summary>F2's second clause: the car parks again while Vale's car is gone and Vale not yet taken: it follows.</summary>
        void TailCheck() { if (valeDriveStart >= 0 && (Vale == ValeAt.InCar || Vale == ValeAt.Taking)) tailAt = Tick + T(Cr.FollowS); }
        void Tail()
        {
            tailAt = INF;
            if (CarSt != CrewCar.Parked || FightBranch.Length > 0 || RivalsDrawn || Wheelman.Mode != CrewMode.Aboard || !Up(Wheelman)) return;
            if (Vale != ValeAt.InCar && Vale != ValeAt.Taking) return;
            CarLeg(CrewCar.Tail, CarRouteP(ccAt, S.CarStand(S.RivalCarTail)), S.RivalCarTail);
            Note("rivals.follows", "The rival car pulls out after Vale's car.");
        }
        /// <summary>K9 (the yard case): the wheelman on foot within act reach of Vale's car before the Take is done reaches Vale.</summary>
        void YardReach()
        {
            if (Over || FightBranch.Length > 0) return;
            if (Vale != ValeAt.InCar && Vale != ValeAt.Taking) return;
            var w = Wheelman; if (!Up(w) || w.Fight || !w.OnFoot) return;
            var car = CarPos(); var at = MemberPos(w, Tick);
            if (!G.Within(at, car, actReach)) return;
            Fail("rivals-reach-vale", "The rival wheelman reaches Vale's car first and takes him.", () => With(Because("tail", true, Locate(w.Id, "reached", at), Locate("councillor", "victim")), ""));
        }

        // ---------------------------------------------------------------- the false tip (F3)

        /// <summary>The tip is given at the parked car with the wheelman aboard (cell.mislead); null when it can't be.</summary>
        string MisleadRefusal(Op op)
        {
            var cp = RivalCarPose();
            if (!cp.HasValue || CarSt != CrewCar.Parked || ccStop != S.RivalsPark || Wheelman.Mode != CrewMode.Aboard || !Up(Wheelman) || RivalsDrawn || FightBranch.Length > 0) return No("no-rivals", "no rival car parked here with its wheelman in it");
            if (!G.Within(op.Pos, cp.Value.pos, actReach)) return No("stand-near-rivals", "stand near the rival car", m: actReach / 100);
            return "";
        }
        /// <summary>The tip lands: the car and wheelman go the way they came, back at the park cell.mislead.away after it.</summary>
        bool TipLands()
        {
            if (CarSt != CrewCar.Parked || ccStop != S.RivalsPark || Wheelman.Mode != CrewMode.Aboard || !Up(Wheelman) || RivalsDrawn || FightBranch.Length > 0) return false;
            var back = CarRoute(S.RivalCarEntry, S.RivalsPark);
            int parks = Tick + T(R.MisleadAwayS);
            RivalsDelayedUntil = parks;
            CarLeg(CrewCar.TipOut, CarRouteP(ccAt, S.CarStand(S.RivalCarEntry)), S.RivalCarEntry);
            crewNextAt = Math.Max(Tick + 1, parks - (CarProfile(back).Length - 1)); crewNext = "tip-back";
            tailAt = INF;
            return true;
        }

        // ---------------------------------------------------------------- the driver's word (F4)

        void DriverTurned() { if (driverTurnedAt < 0) driverTurnedAt = Tick; }
        void WarnStep()
        {
            var l = Lookout;
            if (!warnDecided && driverTurnedAt >= 0 && Driver.Turned && l.Mode == CrewMode.Posted && l.Post == "lookout" && Up(l) && !RivalsDrawn && FightBranch.Length == 0)
            {
                warnDecided = true;
                int s0 = Math.Max(driverTurnedAt + T(Cr.WarnLagS), Tick);
                var word = WordPlace();
                long walk = WalkMm(l.Place, word); int walkT = (int)((walk + rivSpeed - 1) / rivSpeed);
                if (s0 + walkT + T(Cr.WarnS) <= T(driverLastS)) warnAt = s0; // the visit is made only if the word is over by the last chance
                else Note("rivals.no-warning", "The driver was turned too late to warn the rival lookout off before 5:40.");
            }
            if (warnAt == Tick)
            {
                warnAt = INF;
                if (l.Mode == CrewMode.Posted && l.Post == "lookout" && Up(l) && Driver.Turned && !Driver.Down && !RivalsDrawn && FightBranch.Length == 0)
                    StartWalk(l, MemberPos(l, Tick), WordPlace(), "word", rivSpeed);
            }
            // cancelled before the word ends: he walks back to his post (not retried)
            if ((l.Mode == CrewMode.Talking || (l.Mode == CrewMode.Walking && l.Goal == "word")) && (Driver.Down || !Driver.Turned || RivalsDrawn || FightBranch.Length > 0) && Up(l) && !l.Fight)
            {
                l.Until = INF; StartWalk(l, MemberPos(l, Tick), S.Stand(S.RivalLookout), "lookout", rivSpeed);
                Note("rivals.word-off", "The rival lookout walks back to his post.");
            }
        }
        /// <summary>Where the lookout stands to speak to the driver (Pulse §1): the free walk cell nearest the point 1.5 m from the driver's post
        /// toward the lookout's.</summary>
        Pt WordPlace()
        {
            Pt d = S.Stand(S.CarDriver), lk = S.Stand(S.RivalLookout); long dist = Math.Max(1, G.Dist(d, lk));
            var p = new Pt(d.X + (int)((long)(lk.X - d.X) * 150 / dist), d.Y + (int)((long)(lk.Y - d.Y) * 150 / dist));
            return Walk.NearestFree(p) ?? p;
        }

        // ---------------------------------------------------------------- gunfire draws them (F5)

        /// <summary>A drawn crew's places at the Office front (Pulse §1): rivals.front + k × 90 cm across the stop's line to it, k = 0, +1, −1, +2.</summary>
        Pt FrontPlace(CrewMember m)
        {
            Pt a = S.Stand(S.RivalsFront), c = S.At(S.RivalCarFront); var f = a - c; long len = Math.Max(1, G.Isqrt((long)f.X * f.X + (long)f.Y * f.Y));
            int[] ks = { 0, 1, -1, 2, -2 }; int k = ks[Math.Min(m.N - 1, ks.Length - 1)];
            var p = new Pt(a.X + (int)(-f.Y * 90L * k / len), a.Y + (int)(f.X * 90L * k / len));
            return Walk.NearestFree(p) ?? a;
        }
        /// <summary>F5: the crew already on the map makes for the Office front now; off the map, the car comes for it.</summary>
        void CrewDrawn()
        {
            foreach (var m in Crew)
            {
                if (!Up(m) || m.Fight || !m.OnFoot) continue;
                m.Until = INF; StartWalk(m, MemberPos(m, Tick), FrontPlace(m), "front", rivSpeed);
            }
            switch (CarSt)
            {
                case CrewCar.Entering: case CrewCar.Dropping: case CrewCar.Rounding: case CrewCar.Parked: case CrewCar.Tail: case CrewCar.Stopped: case CrewCar.ToStand:
                    var from = RivalCarPose()!.Value.pos;
                    foreach (var m in Crew) if (m.Mode == CrewMode.Aboard && m.OutAt < INF) m.OutAt = INF; // they get out at the front instead
                    CarLeg(CrewCar.Front, CarRouteP(S.Drive.NearestFree(from) ?? from, S.CarStand(S.RivalCarFront)), S.RivalCarFront);
                    crewNextAt = INF; crewNext = ""; tailAt = INF; break;
            }
        }

        // ---------------------------------------------------------------- the safe fight (F6, K9)

        /// <summary>The stand the safe fight is fought at: the getaway car's (or the one it is driving to) at the fight's start.</summary>
        Stand FightStand => S.StandById(fightStandId) ?? CurStand;
        string fightStandId = "";

        /// <summary>K9: the safe fight's hostiles are the crew members on the roster (not gone, down or away with the car); no fresh spawns. The car
        /// goes to the stand's rival stop and the riders step out there.</summary>
        void CrewToFight()
        {
            var st = StandForCrew(); fightStandId = st.Id; crewLate = true; followAt = INF; tailAt = INF; warnAt = INF;
            // who is in the fight: on the map, or still to come in the car
            bool carAway = CarSt == CrewCar.Away || CarSt == CrewCar.TipOut; wheelmanAwayAtStart = carAway && Up(Wheelman) && Wheelman.Mode == CrewMode.Aboard;
            var l = Lookout; bool lookoutBoards = CarSt == CrewCar.Parked && Up(l) && !lookoutWarned && l.Mode == CrewMode.Posted && l.Post == "lookout";
            foreach (var m in Crew)
            {
                if (!Up(m)) continue;
                if (m == l && (lookoutWarned || (m.Mode == CrewMode.Walking && m.Goal == "exit"))) continue; // warned off: gone
                if (m.Mode == CrewMode.Aboard && carAway) continue; // the wheelman away with the tipped car (he joins if it comes back in time)
                if (m == l && (m.Mode == CrewMode.Talking || (m.Mode == CrewMode.Walking && m.Goal == "word"))) { m.Until = INF; m.Mode = CrewMode.Posted; m.Post = "word"; m.Place = MemberPos(m, Tick); } // the word is cut short: he fights from where he stands
                if (m == l && lookoutBoards) { m.Disp = null; StartWalk(m, MemberPos(m, Tick), StepOutAt(), "board", rivSpeed); continue; } // F6: to the car; his body joins at the stand
                MakeFightBody(m, st);
            }
            switch (CarSt)
            {
                case CrewCar.Off: break; // it enters as planned and stops at the stand's stop (Enter)
                case CrewCar.Entering: // it keeps riding, and stops at the stand's stop instead of the drop
                    if (ccStop != st.RivalStop) { var from = RivalCarPose()!.Value.pos; CarLeg(CrewCar.Entering, CarRouteP(S.Drive.NearestFree(from) ?? from, S.CarStand(st.RivalStop)), st.RivalStop); }
                    break;
                case CrewCar.Dropping: // the three already out are as above; the rest step out in their slots (or ride on to the stand's stop elsewhere)
                    if (ccStop == st.RivalStop) { foreach (var m in Crew) if (m.Mode == CrewMode.Aboard && Up(m) && m.OutAt >= INF) m.OutAt = Math.Max(Tick + 1, CarStTick + T(Cr.StepS) * m.N); crewNextAt = INF; CarSt = CrewCar.Stopped; }
                    break;
                case CrewCar.Rounding: case CrewCar.Tail: case CrewCar.Stopped:
                    if (CarSt == CrewCar.Stopped && ccStop == st.RivalStop) break;
                    { var from = RivalCarPose()!.Value.pos; CarLeg(CrewCar.ToStand, CarRouteP(S.Drive.NearestFree(from) ?? from, S.CarStand(st.RivalStop)), st.RivalStop); }
                    break;
                case CrewCar.Parked: // the lookout walks to the car and boards (then it rides: EndAct); with no lookout to wait for it rides at once
                    if (!lookoutBoards) CarLeg(CrewCar.ToStand, CarRouteP(ccAt, S.CarStand(st.RivalStop)), st.RivalStop);
                    break;
                case CrewCar.TipBack: // coming back: on to the stand's stop instead of the park
                    { var from = RivalCarPose()!.Value.pos; CarLeg(CrewCar.ToStand, CarRouteP(S.Drive.NearestFree(from) ?? from, S.CarStand(st.RivalStop)), st.RivalStop); }
                    break;
            }
            CrewFloor(st, lookoutBoards); // engine 0.33.0 (Motive M059-S1.4, S1.5): the floor, then crew destroyed (GameCrewBodies.cs)
            // Pulse §7: the count is a consequence, never a subtraction; a crew above its size is an engine fault
            int bodies = Hostiles.FindAll(h => IsRival(h.Id)).Count + (lookoutBoards ? 1 : 0);
            if (bodies > Cr.Size) throw new InvalidOperationException($"the safe fight holds {bodies} of a crew of {Cr.Size}");
        }

        /// <summary>Host read: the safe fight's crew, its bodies and those still to come with the car (the lookout boarding it; not the wheelman
        /// away on a tip, who joins only if it ends in time). 0 outside the safe fight.</summary>
        public int CrewInFight
        {
            get
            {
                if (FightBranch != "safe") return 0;
                int n = 0; foreach (var h in Hostiles) if (IsRival(h.Id)) n++;
                foreach (var m in Crew) if (m.H == null && Up(m) && !m.Fight && (m.Mode == CrewMode.Boarding || (m.Mode == CrewMode.Walking && m.Goal == "board") || (m.Mode == CrewMode.Aboard && CarSt != CrewCar.Away && CarSt != CrewCar.TipOut))) n++;
                return n;
            }
        }
        /// <summary>The safe fight's crew still to come that it counted at its start: the lookout boarding the parked car (he joins at the stand):
        /// the fight is not over until he is down or gone. A wheelman away on a tip at the start is not counted: he joins a fight still running
        /// when he comes back, but the fight does not wait for him (Pulse §7's [S] alternative, for Motive: a fight held open minutes for a man not
        /// yet on the map read as nothing on the street).</summary>
        bool CrewDue()
        {
            if (FightBranch != "safe") return false;
            foreach (var m in Crew) if (m.H == null && Up(m) && !m.Fight && !(m == Wheelman && wheelmanAwayAtStart) && (m.Mode == CrewMode.Aboard || m.Mode == CrewMode.Boarding || (m.Mode == CrewMode.Walking && m.Goal == "board")) && (CarSt != CrewCar.Stopped || m.OutAt < INF)) return true;
            return false;
        }
        bool wheelmanAwayAtStart;

        /// <summary>The front Take (brief 059): the crew reaches Vale first when its car is parked at rivals.park with the wheelman aboard (not
        /// tipped: the old rule, "the crew present and not drawn", read with bodies), or a drawn member on foot is within his effective reach of the
        /// Enforcer. The lookout alone does not (no rule gives him that; Motive and Pulse to confirm). The member's id, or null.</summary>
        string? FrontCrew(Op op)
        {
            if (Range || FightBranch.Length > 0) return null;
            if (!RivalsDrawn && rivalCarParked && Wheelman.Mode == CrewMode.Aboard && Up(Wheelman)) return Wheelman.Id;
            if (RivalsDrawn) foreach (var m in Crew) { var p = MemberPose(m); if (p.HasValue && !m.Fight && G.Within(op.Pos, p.Value.pos, m.Gun.W!.EffCm)) return m.Id; }
            return null;
        }

        /// <summary>A Watch on the rivals (brief 059 §2.2; brief 054: only when it shows something): the rival car on the map with a next move,
        /// within the agent's watch reach on a clear line. It teaches the crew's timetable (RivalNext). Refused already-watched, too-far, no-clear-line,
        /// target-unknown before the car comes.</summary>
        string WatchRivals(Op op, Order o, bool engage)
        {
            var cp = RivalCarPose(); if (!cp.HasValue || Range) return No("target-unknown", "the rival car is not on the street");
            if (RivalsWatched) return No("already-watched", "the cell knows the rivals' timetable already: a Watch teaches nothing more");
            int reach = R.WatchReachCm(op.Id);
            if (!G.Within(op.Pos, cp.Value.pos, reach)) return No("too-far", $"stand within {reach / 100} m of the rival car", m: reach / 100);
            if (!Sees(op.Pos, cp.Value.pos)) return No("no-clear-line", $"no clear line: move to where the {Sim.Op.Name(op.Id)} can see the rival car");
            if (!engage) return "";
            Engage(op, o, R.SightWatchS); op.BusyQuote = null; op.BusyPaid = 0;
            return "";
        }

        /// <summary>A member's final-fight body: posted or walking, he lurks at his place or keeps his walk to it; aboard, he is not present until he
        /// steps out (then walks to his spot on the stand's).</summary>
        void MakeFightBody(CrewMember m, Stand st)
        {
            var g = m.Gun; g.St = "routine"; g.Target = ""; LoseTarget(g);
            var h = new Hostile { Id = m.Id, Gun = g, Speed = rivSpeed };
            // Motive M059-B1 (engine 0.33.0): where he stands and his spot are read before he becomes a fight body. MemberPos answers m.H's place once
            // m.Fight is set, and 0.32.0 set it first, so every posted or walking member started the fight at the new body's (0,0)
            bool onFoot = m.Mode != CrewMode.Aboard && m.Mode != CrewMode.Boarding;
            Pt where = MemberPos(m, Tick); Pt spot = onFoot ? SpotFor(m, st) : default;
            m.H = h; m.Fight = true; m.Disp = null;
            if (!onFoot)
            {
                h.Present = false; var at = RivalCarPose()?.pos ?? S.Stand(S.RivalsPark); h.At = h.Home = at; m.Fight = false; // the crew routine brings him; he joins at his step-out
                if (m.Mode == CrewMode.Boarding) { m.Mode = CrewMode.Aboard; m.Until = INF; }
            }
            else if (m.Mode == CrewMode.Walking && m.Goal != "exit")
            {
                bool toSpot = m.Goal == "spot" && m.SpotStand == st.Id;
                h.Present = true;
                if (toSpot) { h.Run = m.Walk; h.RunStart = m.WalkStart; h.Speed = m.Speed; h.At = h.Home = m.Walk!.End; }
                else { h.At = where; h.Home = spot; MoveToFight(h, spot); }
            }
            else // posted (a spot, the lookout's post, the yard door, the front) or talking: he lurks where he stands; the lookout and wheelman walk to their spots
            {
                h.Present = true; h.At = h.Home = where;
                if ((m.Post != "spot" || m.SpotStand != st.Id) && m.Post != "door") { h.Home = spot; MoveToFight(h, spot); }
            }
            Hostiles.Add(h);
        }
        /// <summary>A member's spot at the fight's stand: the posted three their assigned spot (or the greedy pick), the lookout the 4th, the wheelman the 5th.</summary>
        Pt SpotFor(CrewMember m, Stand st)
        {
            if (m.N <= Cr.Posted)
            {
                if (m.SpotStand != st.Id || m.Spot < 0) AssignSpot(m, st, MemberPos(m, Tick));
                return S.Stand(st.Ambush[m.Spot]);
            }
            return S.Stand(st.Ambush[Math.Min(m.N - 1, st.Ambush.Length - 1)]);
        }
        void MoveToFight(Hostile h, Pt to)
        {
            var from = h.Pos(Tick); var a = Walk.NearestFree(from) ?? from; var b = Walk.NearestFree(to) ?? to;
            var route = RouteRoundHold(a, b) ?? Walk.Route(a, b); if (route == null || route.Length < 2) { h.At = from; h.Run = null; return; } // engine 0.34.0 (Motive M067-SQ1): to his spot round the car's hold
            h.Run = new Path(route); h.RunStart = Tick; h.At = h.Run.End; if (h.Run.Length == 0) h.Run = null;
        }
        /// <summary>F6: a rider steps out at the fight's stand (or a member still on foot when the fight takes him): his body joins the fight, walking
        /// to his spot. False when there is no fight for him.</summary>
        bool JoinFightOnFoot(CrewMember m, Pt from, string joins = "")
        {
            var st = FightStand;
            var h = m.H; if (h == null) { var g = m.Gun; g.St = "routine"; h = new Hostile { Id = m.Id, Gun = g, Speed = rivSpeed }; m.H = h; Hostiles.Add(h); Note(joins.Length > 0 ? "rivals.recalled" : "rivals.joins", joins.Length > 0 ? joins : $"{m.Id} gets out of the rival car and joins the fight."); }
            m.Fight = true; m.Mode = CrewMode.Walking; m.ModeTick = Tick; m.Disp = null;
            h.Present = true; h.At = from; h.Run = null; var spot = SpotFor(m, st); h.Home = spot; MoveToFight(h, spot); // his body stands at `from` before his spot is read (MemberPos answers it)
            if (FightSprung != INF)
            {
                h.Gun.Engaged = true; h.Gun.St = "advance"; // after the spring he comes in on his path, as police do
                if (R.FightAlways)
                {
                    h.Gun.NextShot = Math.Max(h.Gun.NextShot, Tick + AimNewT); // Motive M059-S1.3: each later arrival fires weapons.aimNew after he steps out
                    if (pickLater && !blockersPicked) { pickLater = false; pickFrom = "rivals"; } // the late spring: the blocker is picked at the first step-out, among those on foot
                }
            }
            return true;
        }
        /// <summary>F6, Dropping: the car left the drop for a stand elsewhere.</summary>
        void LateFromDrop()
        {
            var st = FightStand;
            CarLeg(CrewCar.ToStand, CarRoute(S.RivalCarDrop, st.RivalStop), st.RivalStop);
        }

        // ---------------------------------------------------------------- poses and reads

        /// <summary>Where a member stands now (his fight body's place once he fights), or the car's while aboard.</summary>
        public Pt MemberPos(CrewMember m, int tick)
        {
            if (m.Fight && m.H != null) return m.H.Pos(tick);
            if (m.Gun.Down) return BodyOf(m.P) is Body b ? BodyPos(b) : m.DownAt; // engine 0.33.0: a body taken down, carried or stashed
            switch (m.Mode)
            {
                case CrewMode.Walking: return m.Disp != null ? DispPose(m.Disp, rivSpeed, tick).pos : WalkPos(m, tick);
                case CrewMode.Posted:
                    if (m.Disp != null) return DispPose(m.Disp, rivSpeed, tick).pos;
                    if (m.Post == "lookout") { var (b, into) = CrewRules.BeatAt(Cr.LookoutLoop, m.Id, tick - m.PostTick); int pace = Cr.LookoutPaceCm; int off = b.Move > 0 ? pace * into / (b.S * Hz) : b.Move < 0 ? pace - pace * into / (b.S * Hz) : b.AtPace ? pace : 0; return m.Place + new Pt(0, off); }
                    return m.Place;
                case CrewMode.Talking: case CrewMode.Boarding: return m.Place;
                default: return RivalCarPose()?.pos ?? S.Stand(S.RivalsPark);
            }
        }
        /// <summary>A member on the street now (on foot, up, not gone): his place and facing; null while aboard, down or gone (off the street).</summary>
        public (Pt pos, Pt facing)? MemberPose(CrewMember m)
        {
            if (m.Gun.Down || m.Mode == CrewMode.Gone) return null;
            if (m.Fight && m.H != null)
            {
                var h = m.H; if (!h.Present) return null; var p = h.Pos(Tick);
                var tp = h.Gun.Target.Length > 0 ? PosOf(h.Gun.Target) : null;
                if (tp.HasValue && tp.Value != p) return (p, tp.Value - p);
                if (h.Run != null && h.Moving(Tick)) return (p, h.Run.Heading((long)Math.Max(0, Tick - h.RunStart) * h.Speed));
                return (p, FaceFrom(p, Car));
            }
            switch (m.Mode)
            {
                case CrewMode.Walking:
                    if (m.Disp != null) return DispPose(m.Disp, rivSpeed, Tick);
                    { long mm = (long)Math.Max(0, Tick - m.WalkStart) * m.Speed; return (m.Walk!.At(mm), mm >= m.Walk.Length ? Back(m.Walk) : m.Walk.Heading(mm)); }
                case CrewMode.Posted:
                    if (m.Disp != null) return DispPose(m.Disp, rivSpeed, Tick);
                    return (MemberPos(m, Tick), Yaw(m.Base, BeatOf(m).beat.Yaw));
                case CrewMode.Talking: case CrewMode.Boarding: return (m.Place, m.Base);
                default: return null;
            }
        }
        (CrewBeat beat, int into) BeatOf(CrewMember m)
        {
            var loop = m.Post == "lookout" ? Cr.LookoutLoop : Cr.PostedLoop;
            var id = m.Post == "lookout" ? m.Id : m.N <= Cr.Posted ? m.Id : "rival-1"; // the wheelman on foot plays the posted loop on rival-1's phase
            return CrewRules.BeatAt(loop, loop.PhaseS.ContainsKey(id) ? id : "rival-1", Tick - Math.Max(0, m.PostTick));
        }
        /// <summary>The base facing turned by a beat's yaw (rulebook rivals.beats.rule): cos/sin per mille 30 → (866, 500), 60 → (500, 866).</summary>
        static Pt Yaw(Pt f, int yaw)
        {
            if (yaw == 0) return f;
            int c = Math.Abs(yaw) == 30 ? 866 : 500, s = (Math.Abs(yaw) == 30 ? 500 : 866) * Math.Sign(yaw);
            var r = new Pt((int)(((long)f.X * c - (long)f.Y * s) / 1000), (int)(((long)f.X * s + (long)f.Y * c) / 1000));
            return r == new Pt(0, 0) ? f : r;
        }
        /// <summary>Host read (never hashed): the beat a member plays now ("" while aboard or in the fight), for his pose.</summary>
        public string MemberBeat(string id)
        {
            var m = CrewOf(id); if (m == null || m.Fight || m.Gun.Down) return "";
            if (m.Mode == CrewMode.Aboard) return m == Wheelman ? CrewRules.BeatAt(Cr.WheelmanLoop, m.Id, Tick - CarStTick).beat.Pose : "";
            if (m.Mode == CrewMode.Talking) return "talk";
            if (m.Mode == CrewMode.Boarding) return "board";
            if (m.Mode == CrewMode.Walking) return "walk";
            if (m.Mode != CrewMode.Posted) return "";
            var b = BeatOf(m).beat.Pose;
            if (b == "lean") { bool cover = false; foreach (var c in LiveCover) if (G.Within(c.At, m.Place, R.CoverSnapCm)) { cover = true; break; } if (!cover) b = "watch"; }
            return b;
        }

        /// <summary>A member's cone sees p (perception.rivals; its own sight ignores people): on foot, on the street, drawn or watching it from
        /// their arrival (ai.intent.rivals.street).</summary>
        public bool MemberWatching(CrewMember m, Pt p)
        {
            if (!RivalsDrawn && !R.Intent.RivalsStreet) return false;
            var pose = MemberPose(m); if (!pose.HasValue) return false;
            return G.InCone(pose.Value.pos, pose.Value.facing, R.RivReachCm, R.RivHalfCm, near, p) && Sees(pose.Value.pos, p);
        }
        /// <summary>Any member of the crew on foot sees p (RivalsStreetWatching's crew-wide answer).</summary>
        bool CrewSees(Pt p) { foreach (var m in Crew) if (!m.Fight && MemberWatching(m, p)) return true; return false; }
        /// <summary>The crew on the map and watching the street (ai.intent.rivals.street, or drawn), any member on foot and up.</summary>
        bool CrewStreetOn { get { if (!RivalsDrawn && !R.Intent.RivalsStreet) return false; foreach (var m in Crew) if (!m.Fight && Up(m) && m.OnFoot) return true; return false; } }

        /// <summary>Pulse §8 (host read, never hashed): what a Watch on the rivals shows of a member or the car: the next move's kind, its tick (−1
        /// for none) and where. The car: stops (its next stop's arrival), leaves (the drop), follows (Vale's car), back (the tip's park); the posted
        /// three: spring (the spring ring round the cell's car); the lookout: leaves after the word.</summary>
        public (string kind, int tick, Pt at)? RivalNext(string id)
        {
            if (id == "car:rivals" || id == "rivals")
            {
                switch (CarSt)
                {
                    case CrewCar.Off: return null;
                    case CrewCar.Dropping: return ("leaves", crewNextAt < INF ? crewNextAt : -1, ccAt);
                    case CrewCar.Away: return ("back", RivalsDelayedUntil, S.CarStand(S.RivalsPark));
                    case CrewCar.Parked:
                        if (tailAt < INF) return ("follows", tailAt, CarPos());
                        if (Vale == ValeAt.Office || Vale == ValeAt.Leaving || Vale == ValeAt.Pickup) return ("follows", T(valeAtCarS) + T(R.ValeInS) + T(Cr.FollowS), CarPos());
                        return null;
                    default: if (CarMoving) return ("stops", ccStart + ccProf!.Length - 1, ccPath!.End); return null;
                }
            }
            var m = CrewOf(id); if (m == null || !Up(m)) return null;
            if (m.Mode == CrewMode.Posted && m.Post == "spot") return ("spring", -1, Car);
            if (m == Lookout && warnAt < INF) return ("leaves", warnAt, WordPlace());
            return null;
        }

        // ---------------------------------------------------------------- shot on the street (Motive M059-3.1, 3.5)

        /// <summary>A member of the crew on the street is hit (Damage, its Trail already charged as any person's). Down, he lies where he fell; if
        /// more than half of the crew on the map is down, the crew breaks: the rest go to their car and leave in it for security.rivals.retreat,
        /// and come back with whoever is still up (Motive M059-3.5).</summary>
        void MemberHit(CrewMember m, bool down, string by)
        {
            if (!down) { Note("rivals.hit", $"{m.Id} of the rival crew is hit."); return; }
            m.DownAt = RawMemberPos(m, Tick); m.Disp = null; m.Until = INF; ReleaseCover(m.Gun); LoseTarget(m.Gun);
            Note("rivals.down", $"{m.Id} of the rival crew is down."); Chain.Add(m.Id + "-down");
            CrewBreakCheck();
        }
        /// <summary>Motive M059-3.5: the crew breaks before the final fight: whoever is up on foot goes to the car (it stops where it is) and boards;
        /// it leaves the way it came and is back at the park security.rivals.retreat after the break. Off the map, they walk off it.</summary>
        void CrewBreaks()
        {
            CrewBrokenPre = true; CrewBroken(); RivalsDelayMethod = "fire"; Chain.Add("rivals-driven-back"); Note("rivals.retreat", "The rival crew breaks and makes for its car.");
            var cp = RivalCarPose(); bool car = cp.HasValue && !CarDisabled("car:rivals");
            if (car && CarMoving) CarHalt(CrewCar.Stopped, "", cp!.Value.pos, cp.Value.heading);
            crewNextAt = INF; tailAt = INF; followAt = INF; warnAt = INF;
            bool any = false;
            foreach (var x in Crew)
            {
                if (!Up(x) || x.Fight || !x.OnFoot) continue;
                x.Until = INF; x.Disp = null;
                if (car) { StartWalk(x, MemberPos(x, Tick), StepOutAt(), "flee", rivSpeed); any = true; }
                else StartWalk(x, MemberPos(x, Tick), S.Stand(S.RivalLookoutExit), "exit", rivSpeed);
            }
            if (car && !any) CrewLeaves();
        }
        /// <summary>The broken crew is aboard: the car leaves the way it came, back at the park security.rivals.retreat after the break.</summary>
        void CrewLeaves()
        {
            var cp = RivalCarPose(); if (!cp.HasValue) return;
            int parks = Tick + T(R.RivalsRetreatS); RivalsDelayedUntil = parks;
            CarLeg(CrewCar.TipOut, CarRouteP(S.Drive.NearestFree(cp.Value.pos) ?? cp.Value.pos, S.CarStand(S.RivalCarEntry)), S.RivalCarEntry);
            crewNextAt = Math.Max(Tick + 1, parks - (CarProfile(CarRoute(S.RivalCarEntry, S.RivalsPark)).Length - 1)); crewNext = "tip-back";
        }
        /// <summary>Where a member stands, ignoring that he is down (for where he fell).</summary>
        Pt RawMemberPos(CrewMember m, int tick)
        {
            switch (m.Mode)
            {
                case CrewMode.Walking: return m.Disp != null ? DispPose(m.Disp, rivSpeed, tick).pos : WalkPos(m, tick);
                case CrewMode.Posted: case CrewMode.Talking: case CrewMode.Boarding: return m.Disp != null ? DispPose(m.Disp, rivSpeed, tick).pos : m.Place;
                default: return RivalCarPose()?.pos ?? S.Stand(S.RivalsPark);
            }
        }

        /// <summary>The crew adds bytes to the checkpoint hash (and the kite's): once the car has entered, the crew is drawn, or a member was hit.</summary>
        bool CrewHashes => !Range && (CarSt != CrewCar.Off || RivalsDrawn || Crew.Exists(m => m.Gun.Hit || m.Gun.Down));

        /// <summary>The crew for the checkpoint hash: no bytes until the car has entered (so every run before 2:35 hashes as it did).</summary>
        internal void HashCrew(Hash.H h)
        {
            if (!CrewHashes) return;
            h.S("crew").I((int)CarSt).I(CarStTick).I(ccStart).S(ccStop).I(CrewEntry).I(crewNextAt).S(crewNext).I(followAt).S(followStand).I(tailAt).I(warnAt).I(driverTurnedAt).Z(warnDecided).Z(crewLate).Z(lookoutWarned).Z(wheelmanAwayAtStart).Z(RivalsWatched).S(CrewStandId).S(fightStandId).Z(CrewBrokenPre);
            var cp = RivalCarPose(); h.Z(cp.HasValue); if (cp.HasValue) h.P(cp.Value.pos);
            foreach (var m in Crew)
            {
                h.S(m.Id).I((int)m.Mode).I(m.ModeTick).I(m.OutAt).I(m.WalkStart).I(m.Speed).I(m.PostTick).S(m.Post).I(m.Spot).S(m.SpotStand).S(m.Goal).I(m.Until).Z(m.Fight).Z(m.Disp != null);
                if (m.Avenge.Count > 0) { h.S("avenge"); foreach (var x in m.Avenge) h.S(x); } // engine 0.33.0: only a run where the crew saw a takedown adds bytes
                var p = MemberPose(m); h.Z(p.HasValue); if (p.HasValue) h.P(p.Value.pos).P(p.Value.facing);
                Hash.HashGun(h, m.Gun);
            }
        }
    }
}
