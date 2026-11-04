// The rival crew (brief 059 §2, K7–K11; GDD §07 rivals; Pulse briefs/059/pulse/rival-routine.md; rulebook `rivals`). Five people and one car on
// their own timetable. Engine 0.41.0 (brief 096; Motive briefs/096/motive/rivals.html, rivals.json; Sky 2026-11-04 "all recommended"): the car
// comes for the cell's car on arrival and boxes it in (the car's routine: GameRivalBlock.cs); the lookout watches the target's car and is the
// cell's job; the crew can be listened to, paid or misled (GameCrewTurn.cs). Here: the members.
//  · rival-1..3 step out at the block rivals.step apart and walk to the first rivals.posted spots of the getaway car's stand (in id order each
//    taking the shortest grid walk, ties the lower spot; the seed may say who takes which), and post there facing the car; rival-4, the
//    lookout, steps out next and posts at rivals.posts.lookout facing the target's car; rival-5, the wheelman, stays aboard.
//  · Follows: the getaway car's Drive (F1: the three re-plan rivals.follow later and go at rivals.followPace, Sky's S3); gunfire (F5: the crew on
//    foot makes for the Office front; the car keeps its block); the safe final fight (F6: the crew on the map are its hostiles, no fresh spawns).
//  · Retired by 0.41.0: the park, the car's tail (F2), the false tip (F3), the turned driver's word (F4) and the drawn car's stop at the front.
//  · Every state is a function of ticks and orders, hashed (HashCrew). Beat names and RivalNext are host reads, never hashed; no rule reads a
//    beat name. Ties break on the lower id, spots included. The crew raises no Alert or Trail by any of this (rivals don't report).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>The rival car (engine 0.41.0): off the map, driving in to its block point, standing there, leaving (a pre-fight break, or for good),
    /// away, coming back to block again, stopped short (disabled), gone for good.</summary>
    public enum CrewCar { Off, Entering, Blocking, Leaving, Away, Returning, Stopped, Gone }
    public enum CrewMode { Aboard, Walking, Posted, Talking, Boarding, Gone }

    /// <summary>One of the crew (brief 059 K8): rival-1..3 the posted three, rival-4 the lookout, rival-5 the wheelman. His Gun is the same gun
    /// in the final fight; H is his final-fight body once he is in it (it then owns his place).</summary>
    public sealed class CrewMember
    {
        public string Id = "", Role = ""; public int N; public Gun Gun = null!;
        public CrewMode Mode = CrewMode.Aboard; public int ModeTick;
        public int OutAt = int.MaxValue / 4;                     // the tick he steps out of the car (aboard until then)
        public Path? Walk; public int WalkStart, Speed;          // Walking
        public Pt Place, Base; public int PostTick = -1; public string Post = ""; // Posted (spot, lookout, misled, follow, front, car)
        public int Spot = -1; public string SpotStand = "";      // his ambush spot (0-based) and the stand it belongs to
        public string Goal = "";                                 // what he does when his walk ends: spot, lookout, misled, back, follow, exit, front, aboard, leave, flee
        public int Until = int.MaxValue / 4;                     // Posted "misled": when he walks back; Talking: when the word ends
        public Pt DownAt; public bool Fight;                     // where he fell; he is a final-fight body (Hostile H)
        public Hostile? H;
        public KiteDisp? Disp;                                   // drawn: the kite's walk off his line, or the press (fight.kite, ai.intent.rivals.press)
        /// <summary>Engine 0.33.0 (Motive M059-3.1): the member as a person for the takedown and the body orders (job "rival"; never in Game.People,
        /// so no witness, report, statement, check-in or missing rule reads him).</summary>
        public Person P = null!;
        /// <summary>Engine 0.33.0 (Motive M059-S1.6, M059-3.4): the agents he saw take one of the crew down, tried it on him, or took him down:
        /// his cause to fire on them (ai.intent foe crewmate).</summary>
        public readonly SortedSet<string> Avenge = new SortedSet<string>(StringComparer.Ordinal);
        /// <summary>Engine 0.41.0 (rivals.turn): his temperament (mercenary, loyal), whether the cell knows it (a Listen), whether a Pay was
        /// refused, whether he took one (gone for good, never recalled), and the faces he holds (a misled man knows the Scout's).</summary>
        public string Temperament = ""; public bool KTemperament, Refused, Paid, Misled;
        public bool OnFoot => Mode == CrewMode.Walking || Mode == CrewMode.Posted || Mode == CrewMode.Talking;
    }

    public sealed partial class Game
    {
        public readonly List<CrewMember> Crew = new List<CrewMember>();
        /// <summary>The default timetable's ticks (Pulse §4), unchanged since 0.32.0 so every seed's turn-in is where it was: computed back from the
        /// park (RivalAt) through the old drop. Engine 0.41.0 reads only CrewEntry (the turn-in, 2:35 at seed 0); the drop and the park are retired.</summary>
        public int CrewEntry, CrewDropArrive, CrewDropLeave;
        int followAt = INF; string followStand = "";             // F1: the three re-plan for this stand at this tick
        bool crewLate;                                           // F6: the safe fight has begun (members out of the car join it)
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
            if (CrewEntry < 1) // a start that moves the park this early (a test's rivalsAt): the car turns in on the first tick
            { CrewEntry = 1; if (CrewDropArrive < 1) { CrewDropArrive = 1; CrewDropLeave = 1 + T(Cr.DropS); } }
            DrawTemperaments(); // engine 0.41.0 (GameCrewTurn.cs): by id, never the seed
            CheckBlockPoints(); if (S.Entrances.Count > 0) OtherDoor(); // engine 0.41.0 (RIV-30): every stand has a block point, the target an other door (unknown = reject at the start; a site with no takedown file has no entrances: the Mislead of the lookout rejects then)
        }

        Path CarRoute(string from, string to) => CarRouteP(S.CarStand(from), S.CarStand(to));
        /// <summary>A read (scenarios, the debug view; never hashed): the ticks of the rival car's drive between two of its points (the profile's length).</summary>
        public int CarProfileTicks(string from, string to) => CarProfile(CarRoute(from, to)).Length;
        Path CarRouteP(Pt from, Pt to) => new Path(S.Drive.CarRoute(from, to, S.DriveLaneCm) ?? new[] { from, to });

        /// <summary>One tick of the crew (the car first, then its riders, then the others, in id order). Called in the schedule's place.</summary>
        void CrewStep()
        {
            if (Range) return;
            if (CarSt == CrewCar.Off && Tick == CrewEntryNow && !(Wheelman.Gun.Down && Wheelman.Mode == CrewMode.Gone)) Enter(); // a wheelman killed before the mission (Start.CrewGone, test only) drives no car in
            if (CarMoving && Tick >= ccStart + ccProf!.Length - 1) CarArrive();
            if (crewNextAt == Tick) CrewNext();
            if (followAt == Tick) FollowCar();
            WheelmanStep(); LookoutWatch(); // GameRivalBlock.cs
            foreach (var m in Crew) MemberStep(m);
            LookoutReach(); BlockEnds(); GangStep(); // GameGangShooting.cs
        }
        int CrewEntryNow => RivalsDrawn && crewDrawnEntry < INF ? crewDrawnEntry : CrewEntry;
        int crewDrawnEntry = INF;

        // ---------------------------------------------------------------- members

        static bool Up(CrewMember m) => !m.Gun.Down && m.Mode != CrewMode.Gone;
        Pt StepOutAt() { var c = RivalCarPose()?.pos ?? S.CarStand(S.RivalCarEntry); return Walk.NearestFree(c) ?? c; }

        void StartWalk(CrewMember m, Pt from, Pt to, string goal, int speed)
        {
            var a = Walk.NearestFree(from) ?? from; var b = Walk.NearestFree(to) ?? to;
            if (goal == "spot" || goal == "misled" || goal == "follow") b = ClearStop(m.Id, b); // brief 081 (engine 0.37.0; fight.spacing): a place on the street, clear of who stands there; a walk to the car ends at the car
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
            if (m.Mode == CrewMode.Posted && m.Post == "misled" && Tick >= m.Until) MisledBack(m); // GameCrewTurn.cs
            if (m.Mode == CrewMode.Walking && m.Goal == "follow" && Tick >= m.Until) FollowAim(m); // GameRivalBlock.cs: re-aimed each second
        }

        /// <summary>A rider steps out beside the car and walks to where he goes.</summary>
        void StepOut(CrewMember m)
        {
            m.OutAt = INF; var from = StepOutAt(); m.Place = from;
            if (FightBranch == "safe" && FightOverTick == INF && JoinFightOnFoot(m, from)) return; // F6: out for the fight
            if (RivalsDrawn) { StartWalk(m, from, FrontPlace(m), "front", rivSpeed); return; } // F5
            if (m == Lookout) { StartWalk(m, from, S.Stand(S.RivalLookout), "lookout", rivSpeed); return; }
            if (m.N <= Cr.Posted) { var st = StandForCrew(); AssignSpot(m, st, from); StartWalk(m, from, S.Stand(st.Ambush[m.Spot]), "spot", rivSpeed); return; }
            Post(m, "car", from, RivalCarPose()?.heading ?? new Pt(0, 100)); // the wheelman out with no fight to join: he stands by his car
        }

        void ArriveWalk(CrewMember m)
        {
            var end = m.Walk!.End; var back = Back(m.Walk);
            m.Walk = null; m.Place = end;
            switch (m.Goal)
            {
                case "spot": case "back" when m.N <= Cr.Posted: Post(m, "spot", end, FaceFrom(end, CrewStand.CarAt)); Note("rivals.post", $"{m.Id} takes his post round our car."); break;
                case "lookout": case "back":
                    Post(m, "lookout", end, FaceFrom(end, S.At(S.CarPickup)));
                    if (m.Goal == "lookout" && !lookoutPostedOnce) { lookoutPostedOnce = true; Note("rivals.lookout-posted", $"A rival lookout takes up a post watching {ValeName}'s car. If it goes round, he follows it."); } // brief 096: the crucial card (Latch)
                    else Note("rivals.post", $"{m.Id}, the lookout, is back watching {ValeName}'s car.");
                    break;
                case "misled": Post(m, "misled", end, back); m.Until = Tick + T(Cr.MisledS); break; // GameCrewTurn.cs: he watches where he was sent
                case "follow": Post(m, "follow", end, FaceFrom(end, CarPos())); break; // he stands at the target's car (LookoutReach)
                case "front": Post(m, "front", end, back); break;
                case "exit": m.Mode = CrewMode.Gone; m.ModeTick = Tick; Note("rivals.gone", $"{m.Id} of the rival crew walks off the map."); break;
                case "aboard": m.Mode = CrewMode.Aboard; m.ModeTick = Tick; m.Post = ""; break; // engine 0.33.0: a woken member back in his car (M059-3.4)
                case "leave": m.Mode = CrewMode.Gone; m.ModeTick = Tick; Note("rivals.gone", $"{m.Id} of the rival crew walks off the map."); break; // engine 0.33.0: a woken member with his car gone
                case "flee": // M059-3.5: aboard; when the last of them is in, the car leaves
                    m.Mode = CrewMode.Aboard; m.ModeTick = Tick; m.Post = ""; m.Spot = -1; m.SpotStand = "";
                    if (!Crew.Exists(x => Up(x) && x.Mode == CrewMode.Walking && x.Goal == "flee")) CrewLeaves();
                    break;
                default: Post(m, "front", end, back); break;
            }
        }
        bool lookoutPostedOnce;
        void Post(CrewMember m, string post, Pt at, Pt face) { m.Mode = CrewMode.Posted; m.ModeTick = Tick; m.Post = post; m.Place = at; m.Base = face; m.PostTick = Tick; }
        static Pt FaceFrom(Pt from, Pt to) { var f = to - from; return f == new Pt(0, 0) ? new Pt(0, 100) : f; }

        // ---------------------------------------------------------------- where the three post

        /// <summary>The stand the cell's car is at, or driving to (the stand that decides where the three post and where the car blocks).</summary>
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
            if (rivalPosts != null && m.N - 1 < rivalPosts.Length && rivalPosts[m.N - 1] < st.Ambush.Length && !taken.Contains(rivalPosts[m.N - 1])) best = rivalPosts[m.N - 1]; // brief 080: the seed says who takes which post
            m.Spot = best < 0 ? Math.Min(m.N - 1, st.Ambush.Length - 1) : best; m.SpotStand = st.Id;
        }
        long WalkMm(Pt a, Pt b) { var r = Walk.Route(Walk.NearestFree(a) ?? a, Walk.NearestFree(b) ?? b); return r == null ? long.MaxValue / 4 : new Path(r).Length; }

        /// <summary>F1: the getaway car leaves its stand on a Drive: the posted three (posted, walking to a post, or still aboard) re-plan
        /// rivals.follow later for the new stand's spots, in id order from where each stands, at rivals.followPace; the rival car still driving in
        /// turns for the new stand's block point (GameRivalBlock.cs).</summary>
        void CrewCarMoves(Stand to)
        {
            if (Range) return;
            BlockRetarget(to);
            if (FightBranch.Length > 0) return;
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
                if (m.Mode == CrewMode.Aboard) { m.SpotStand = ""; m.Spot = -1; continue; } // still in the car: he decides at his step-out
                if (!(m.Mode == CrewMode.Posted && m.Post == "spot") && !(m.Mode == CrewMode.Walking && m.Goal == "spot")) continue;
                var from = MemberPos(m, Tick); AssignSpot(m, st, from);
                StartWalk(m, from, S.Stand(st.Ambush[m.Spot]), "spot", Cr.FollowCmS); any = true;
            }
            if (any) Note("rivals.follow-car", "The rivals posted round our car go after it.");
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
        /// <summary>F5: the crew on foot makes for the Office front now; the car keeps its block (engine 0.41.0, Motive rivals.json routine.drawn: one
        /// shot anywhere would otherwise unbox the cell's car). A paid man is gone; a member following the target's car keeps after it.</summary>
        void CrewDrawn()
        {
            foreach (var m in Crew)
            {
                if (!Up(m) || m.Fight || !m.OnFoot || (m.Mode == CrewMode.Walking && m.Goal is "follow" or "exit")) continue;
                m.Until = INF; StartWalk(m, MemberPos(m, Tick), FrontPlace(m), "front", rivSpeed);
            }
        }

        // ---------------------------------------------------------------- the safe fight (F6, K9)

        /// <summary>The stand the safe fight is fought at: the getaway car's (or the one it is driving to) at the fight's start.</summary>
        Stand FightStand => S.StandById(fightStandId) ?? CurStand;
        string fightStandId = "";

        /// <summary>K9: the safe fight's hostiles are the crew members on the roster (not paid off, gone, down or away with the car); no fresh spawns.
        /// Engine 0.41.0: the car is already the block (or still coming to it); the wheelman stays aboard until the spring; the lookout comes from
        /// wherever he is; a misled man joins from his walk.</summary>
        void CrewToFight()
        {
            var st = StandForCrew(); fightStandId = st.Id; crewLate = true; followAt = INF;
            bool carAway = CarSt == CrewCar.Away || CarSt == CrewCar.Leaving || CarSt == CrewCar.Gone;
            foreach (var m in Crew)
            {
                if (!Up(m)) continue;
                if (m.Mode == CrewMode.Walking && m.Goal is "exit" or "leave") continue; // walking off the map: gone
                if (m.Mode == CrewMode.Aboard && carAway) continue; // away with the car (the floor may call it back)
                if (m.Mode == CrewMode.Talking) { m.Until = INF; m.Mode = CrewMode.Posted; m.Post = "word"; m.Place = MemberPos(m, Tick); }
                MakeFightBody(m, st);
            }
            if (CarSt == CrewCar.Entering || CarSt == CrewCar.Returning) BlockRetarget(st); // still coming: to this stand's block point
            CrewFloor(st); // engine 0.33.0 (Motive M059-S1.4, S1.5): the floor, then crew destroyed (GameCrewBodies.cs)
            int bodies = Hostiles.FindAll(h => IsRival(h.Id)).Count; // Pulse §7: the count is a consequence, never a subtraction; a crew above its size is an engine fault
            if (bodies > Cr.Size) throw new InvalidOperationException($"the safe fight holds {bodies} of a crew of {Cr.Size}");
            int thinned = Crew.FindAll(m => m.Paid || (m.Gun.Down && !CrewWaking(m))).Count;
            if (thinned > 0) FightWhy.Add("crew-thinned"); // each out for good before the fight is one fewer (the count is RivalBodies)
        }

        /// <summary>Host read: the safe fight's crew, its bodies and those still to come with the car. 0 outside the safe fight.</summary>
        public int CrewInFight
        {
            get
            {
                if (FightBranch != "safe") return 0;
                int n = 0; foreach (var h in Hostiles) if (IsRival(h.Id)) n++;
                foreach (var m in Crew) if (m.H == null && Up(m) && !m.Fight && (m.Mode == CrewMode.Aboard && CrewCarHere)) n++;
                return n;
            }
        }
        /// <summary>The rival car is on the map (not leaving, away or gone).</summary>
        bool CrewCarHere => CarSt != CrewCar.Off && CarSt != CrewCar.Away && CarSt != CrewCar.Leaving && CarSt != CrewCar.Gone;
        /// <summary>The safe fight's crew still to come: a rider in a car on the map, or one called back in it. The fight is not over until he is down or gone.</summary>
        bool CrewDue()
        {
            if (FightBranch != "safe") return false;
            foreach (var m in Crew) if (m.H == null && Up(m) && !m.Fight && m.Mode == CrewMode.Aboard && (CrewCarHere || CarSt == CrewCar.Off) && (CarSt != CrewCar.Stopped || m.OutAt < INF) && !(m == Wheelman && m.OutAt >= INF && CarSt == CrewCar.Blocking)) return true; // engine 0.42.0 (F1): the wheelman at the wheel of the block is no one still to come
            return false;
        }

        /// <summary>The front Take (brief 059): a drawn member on foot within his effective reach of the Enforcer reaches Vale first. Engine 0.41.0:
        /// the parked car's clause is retired with the park; the lookout's watch on the target's car is LookoutWatch's (he runs to a Take he sees).</summary>
        string? FrontCrew(Op op)
        {
            if (Range || FightBranch.Length > 0 || !RivalsDrawn) return null;
            foreach (var m in Crew) { var p = MemberPose(m); if (p.HasValue && !m.Fight && G.Within(op.Pos, p.Value.pos, m.Gun.W!.EffCm)) return m.Id; }
            return null;
        }

        /// <summary>A Watch on the rivals (brief 059 §2.2; brief 054: only when it shows something): the rival car on the map, within the agent's
        /// watch reach on a clear line. It teaches the crew's timetable (RivalNext). Refused already-watched, too-far, no-clear-line,
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
            // Motive M059-B1 (engine 0.33.0): where he stands and his spot are read before he becomes a fight body
            bool onFoot = m.Mode != CrewMode.Aboard && m.Mode != CrewMode.Boarding;
            Pt where = MemberPos(m, Tick); Pt spot = onFoot ? SpotFor(m, st) : default;
            m.H = h; m.Fight = true; m.Disp = null;
            if (!onFoot)
            {
                h.Present = false; var at = RivalCarPose()?.pos ?? S.CarStand(S.RivalCarEntry); h.At = h.Home = at; m.Fight = false; // the crew routine brings him; he joins at his step-out
                if (m.Mode == CrewMode.Boarding) { m.Mode = CrewMode.Aboard; m.Until = INF; }
            }
            else if (m.Mode == CrewMode.Walking)
            {
                bool toSpot = m.Goal == "spot" && m.SpotStand == st.Id;
                h.Present = true;
                if (toSpot) { h.Run = m.Walk; h.RunStart = m.WalkStart; h.Speed = m.Speed; h.At = h.Home = m.Walk!.End; }
                else { h.At = where; h.Home = spot; MoveToFight(h, spot); }
            }
            else // posted (a spot, the lookout's post, a misled watch, the target's car, the front) or talking: he lurks where he stands, or walks to his spot
            {
                h.Present = true; h.At = h.Home = where;
                if (m.Post != "spot" || m.SpotStand != st.Id) { h.Home = spot; MoveToFight(h, spot); }
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
        /// <summary>A final-fight body's walk to its stand, spot or post (Why "post"), round the car's hold where it can (Motive M067-SQ1:
        /// RouteRoundHold), else the plain walk; it stands where it is when there is no walk at all.</summary>
        void MoveToFight(Hostile h, Pt to)
        {
            var from = h.Pos(Tick); var a = Walk.NearestFree(from) ?? from; var b = Walk.NearestFree(to) ?? to;
            var free = ClearStop(h.Id, b); if (free != b) { if (h.Home == to) h.Home = free; b = free; } // engine 0.36.0 (fight.spacing): his stand or spot is taken: the free place beside it is his stand now
            var route = RouteRoundHold(a, b) ?? Walk.Route(a, b); if (route == null || route.Length < 2) { h.At = from; h.Run = null; return; }
            h.Run = new Path(route); h.RunStart = Tick; h.At = h.Run.End; h.Why = "post"; h.Closing = false; if (h.Run.Length == 0) h.Run = null;
        }
        /// <summary>F6: a rider steps out at the fight's stand (or a member still on foot when the fight takes him): his body joins the fight, walking
        /// to his spot. False when there is no fight for him.</summary>
        bool JoinFightOnFoot(CrewMember m, Pt from, string joins = "")
        {
            var st = FightStand;
            var h = m.H; if (h == null) { var g = m.Gun; g.St = "routine"; h = new Hostile { Id = m.Id, Gun = g, Speed = rivSpeed }; m.H = h; Hostiles.Add(h); Note(joins.Length > 0 ? "rivals.recalled" : "rivals.joins", joins.Length > 0 ? joins : $"{m.Id} gets out of the rival car and joins the fight."); }
            m.Fight = true; m.Mode = CrewMode.Walking; m.ModeTick = Tick; m.Disp = null;
            h.Present = true; h.At = from; h.Run = null; var spot = SpotFor(m, st); h.Home = spot; MoveToFight(h, spot);
            if (FightSprung != INF)
            {
                h.Gun.Engaged = true; h.Gun.St = "advance"; // after the spring he comes in on his path, as police do
                foreach (var f in springFoes) m.Avenge.Add(f); // engine 0.41.0 (rivals.turn): a Loyal man told his crew of the Fixer
                if (R.FightAlways)
                {
                    h.Gun.NextShot = Math.Max(h.Gun.NextShot, Tick + AimNewT); // Motive M059-S1.3: each later arrival fires weapons.aimNew after he steps out
                    if (pickLater && !blockersPicked && !RivalBlockHere) { pickLater = false; pickFrom = "rivals"; } // the late spring: the blocker is picked at the first step-out, among those on foot (none where the car is the block)
                }
            }
            return true;
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
        /// for none) and where. The car: stops (its block point's arrival), back (after a break). The posted three: spring (the ring round the
        /// cell's car). The lookout (engine 0.41.0): follows (the target's car, from when it is due to leave), back (a misled man's return to his post).</summary>
        public (string kind, int tick, Pt at)? RivalNext(string id)
        {
            if (id == "car:rivals" || id == "rivals")
            {
                switch (CarSt)
                {
                    case CrewCar.Away: return ("back", RivalsDelayedUntil, BlockAt(StandForCrew()).at);
                    case CrewCar.Entering: case CrewCar.Returning: if (CarMoving) return ("stops", ccStart + ccProf!.Length - 1, ccPath!.End); return null;
                    default: return null;
                }
            }
            var m = CrewOf(id); if (m == null || !Up(m)) return null;
            if (m.Mode == CrewMode.Posted && m.Post == "spot") return ("spring", -1, Car);
            if (m.Mode == CrewMode.Posted && m.Post == "misled") return ("back", MisledBackTick(m), m == Lookout ? S.Stand(S.RivalLookout) : S.Stand(CrewStand.Ambush[Math.Max(0, m.Spot)]));
            if (m == Lookout && m.Mode == CrewMode.Posted && m.Post == "lookout" && (Vale == ValeAt.Office || Vale == ValeAt.Leaving || Vale == ValeAt.Pickup)) return ("follows", T(valeAtCarS) + T(R.ValeInS), CarPos());
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
        /// it leaves the way it came and is back to block again security.rivals.retreat after the break. With no car (destroyed) they walk off the
        /// map, gone for the mission (engine 0.41.0, RIV-27). A paid man is gone already; a member following the target's car keeps after it.</summary>
        void CrewBreaks()
        {
            CrewBrokenPre = true; CrewBroken(); RivalsDelayMethod = "fire"; Chain.Add("rivals-driven-back"); Note("rivals.retreat", "The rival crew breaks and makes for its car.");
            var cp = RivalCarPose(); bool car = cp.HasValue && !CarDisabled("car:rivals") && CrewCarHere;
            if (car && CarMoving) CarHalt(CrewCar.Stopped, "", cp!.Value.pos, cp.Value.heading);
            crewNextAt = INF; followAt = INF;
            bool any = false;
            foreach (var x in Crew)
            {
                if (!Up(x) || x.Fight || !x.OnFoot) continue;
                var from = MemberPos(x, Tick); x.Until = INF; x.Disp = null; x.Gun.Drawn = false; LoseTarget(x.Gun); // brief 081 (engine 0.37.0): read before the detour is dropped: he leaves from where he stands; engine 0.42.0 (Motive F2): a broken man puts his gun away
                if (car) { StartWalk(x, from, StepOutAt(), "flee", rivSpeed); any = true; }
                else { StartWalk(x, from, S.Stand(S.RivalLookoutExit), "exit", Cr.BrokenCmS); crewCarless = true; } // F2: off the map at brokenPace
            }
            if (!car) { crewCarless = true; foreach (var x in Crew) if (Up(x) && x.Mode == CrewMode.Aboard) { x.Mode = CrewMode.Gone; x.ModeTick = Tick; } } // aboard a wreck: out and gone with them
            if (car && !any) CrewLeaves();
        }
        /// <summary>Engine 0.41.0 (RIV-27): the crew broke with its car destroyed: it walked off the map and is gone for the mission (never recalled).</summary>
        bool crewCarless;
        /// <summary>The broken crew is aboard: the car leaves the way it came, and is back to block again security.rivals.retreat after the break.</summary>
        void CrewLeaves()
        {
            var cp = RivalCarPose(); if (!cp.HasValue) return;
            int back = Tick + T(R.RivalsRetreatS); RivalsDelayedUntil = back;
            CarLeg(CrewCar.Leaving, CarRouteP(S.Drive.NearestFree(cp.Value.pos) ?? cp.Value.pos, S.CarStand(S.RivalCarEntry)), S.RivalCarEntry);
            crewNextAt = Math.Max(Tick + 1, back - (CarProfile(CarRouteP(S.CarStand(S.RivalCarEntry), BlockAt(StandForCrew()).at)).Length - 1)); crewNext = "back";
        }
        /// <summary>Where a member stands, ignoring that he is down (for where he fell).</summary>
        Pt RawMemberPos(CrewMember m, int tick)
        {
            switch (m.Mode)
            {
                case CrewMode.Walking: return m.Disp != null ? DispPose(m.Disp, rivSpeed, tick).pos : WalkPos(m, tick);
                case CrewMode.Posted: case CrewMode.Talking: case CrewMode.Boarding: return m.Disp != null ? DispPose(m.Disp, rivSpeed, tick).pos : m.Place;
                default: return RivalCarPose()?.pos ?? S.CarStand(S.RivalCarEntry);
            }
        }

        /// <summary>The crew adds bytes to the checkpoint hash (and the kite's): once the car has entered, the crew is drawn, or a member was hit.</summary>
        bool CrewHashes => !Range && (CarSt != CrewCar.Off || RivalsDrawn || Crew.Exists(m => m.Gun.Hit || m.Gun.Down));

        /// <summary>The crew for the checkpoint hash: no bytes until the car has entered (so every run before 2:35 hashes as it did).</summary>
        internal void HashCrew(Hash.H h)
        {
            if (!CrewHashes) return;
            h.S("crew").I((int)CarSt).I(CarStTick).I(ccStart).S(ccStop).I(CrewEntry).I(crewNextAt).S(crewNext).I(followAt).S(followStand).Z(crewLate).Z(RivalsWatched).S(CrewStandId).S(fightStandId).Z(CrewBrokenPre);
            h.S("block").S(blockStandId).Z(wheelmanCalled).Z(crewCarless).Z(lookoutPostedOnce); foreach (var f in springFoes) h.S(f); // engine 0.41.0
            var cp = RivalCarPose(); h.Z(cp.HasValue); if (cp.HasValue) h.P(cp.Value.pos);
            foreach (var m in Crew)
            {
                h.S(m.Id).I((int)m.Mode).I(m.ModeTick).I(m.OutAt).I(m.WalkStart).I(m.Speed).I(m.PostTick).S(m.Post).I(m.Spot).S(m.SpotStand).S(m.Goal).I(m.Until).Z(m.Fight).Z(m.Disp != null);
                h.S(m.Temperament).Z(m.KTemperament).Z(m.Refused).Z(m.Paid).Z(m.Misled); // engine 0.41.0 (rivals.turn)
                if (m.Avenge.Count > 0) { h.S("avenge"); foreach (var x in m.Avenge) h.S(x); } // engine 0.33.0: only a run where the crew saw a takedown adds bytes
                var p = MemberPose(m); h.Z(p.HasValue); if (p.HasValue) h.P(p.Value.pos).P(p.Value.facing);
                Hash.HashGun(h, m.Gun);
            }
        }
    }
}
