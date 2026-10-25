// Engine 0.32.0 (brief 059 K1–K11; GDD v0.25.0 §07; Sky's answers 1a, 2a, 3a; Pulse briefs/059/pulse/rival-routine.md; Motive
// briefs/059/motive/rulings.json). The done-check's scenarios:
//    P59-DAY       no orders: the crew is nowhere before its entry; it enters, drops three by the cell's car, parks at 3:10 and posts its lookout;
//                  the three take the square's spots by the greedy rule (rival-1 ambush-3, rival-2 ambush-1, rival-3 ambush-2); beats turn them
//    P59-B         route B: the driver turned and the lookout warned off, the tip at the car, Vale driven round (car.parks), the getaway car driven
//                  round behind it, the in-car Take, a 3.5 m carry, the safe fight at the yard with the posted three (who walked after the car)
//    P59-NOBODY    the turned driver waits with Vale in the bay; nobody takes him; at 8:30 he drives on: vale-leaves
//    P59-UNTURNED  Sky 3a: an unturned driver drives Vale away at 6:13: vale-leaves
//    P59-FIRST     the getaway car on the service drive first: the turned driver stops at its mouth (K5); the Take there is in-car
//    P59-TAIL      no tip: the rival car follows Vale's car, the wheelman walks to Vale's door and reaches him before the Take is done
//    P59-TIP       the tip at the parked car: the car and wheelman leave the way they came, back at the park 3:50 after it lands
//    P59-EARLY     the cell has Vale before the rivals come: the car comes straight to the cell's stand, all five step out there
//    P59-GUN       gunfire before 2:35: the car enters within 16 s and rides to the Office front; the riders walk to their places there
//    (P59-S1-GO / P59-S1-HOLD retired in engine 0.33.0 with leaveUnfought: Sky's S1 = no; the S1-* scenarios hold the rule)
//    P59-S2-NEAR / P59-S2-NORTH  Sky's S2 both ways: the loud fight at the service drive, officers from the official entrance, or the north street
//    P59-S3-RUN / P59-S3-WALK  Sky's S3 both ways: the posted three go after a Driven car at the run (280) or the walk (160)
//    P59-BEAT      the front, driver unturned, the rival car parked with its wheelman aboard: the Take fails rivals-reach-vale (beat-to-pickup)
//    P59-REFUSE    Drive's and the Take's refusals: drive-no-driver, unknown-stand, drive-here, drive-busy, drive-held, drive-fight, driver-brings-vale, car-not-parked
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>Brief 059: the same inputs with one rulebook text replaced (Sky's S1–S3 both ways), as WithFinal: the texts, and so every hash,
        /// stay the original's (a behaviour-only comparison). Never a run record.</summary>
        public static Engine.Inputs With059(Engine.Inputs i, string pattern, string replace)
        {
            var re = new Regex(pattern);
            if (re.Matches(i.Rulebook).Count != 1) throw new FormatException($"the rulebook has '{pattern}' once");
            string rb = re.Replace(i.Rulebook, replace);
            return new Engine.Inputs
            {
                Rulebook = i.Rulebook, PeopleRules = i.PeopleRules, Site = i.Site, People = i.People, Provisional = i.Provisional, Takedown = i.Takedown, Rounds = i.Rounds,
                Rules = new Rules(rb, i.PeopleRules), SiteData = i.SiteData, Crowd = i.Crowd,
            };
        }
        /// <summary>A body played on other inputs (S1–S3's other value): the check lines go to the scenario's own run.</summary>
        static Run Alt(Run r, Engine.Inputs alt, Start? start = null) { r.Wait(1); return new Run(Engine.New(alt, start ?? r.G.Start)) { Inputs = alt }; } // the scenario's own run steps a tick, so it replays (a run at tick 0 replays to the close)

        static CrewMember Mem(Run r, int n) => r.G.Crew[n - 1];
        /// <summary>Brief 059 (test only): Vale waits at his car past 6:13 (Start.ValeWaits), for the scenarios of other systems.</summary>
        static Start KeepVale(Start s) { s.ValeWaits = true; return s; }
        /// <summary>Where the Scout gives the tip on the civic block: TipSpot(r) as a constant, for the pinned order lists.</summary>
        static readonly Pt TipPt = new Pt(9300, -3100);
        /// <summary>Where the Scout gives the tip: the free ground nearest the rival car parked at rivals.park (act reach of the car).</summary>
        static Pt TipSpot(Run r) { var g = r.G; var c = g.S.CarStand(g.S.RivalsPark); return g.S.Grid.NearestFree(c + new Pt(250, 0)) ?? g.S.Grid.NearestFree(c) ?? c; }

        /// <summary>Route B's opening on the 059 engine: the Enforcer to the service door, the Scout listens to the driver, the Fixer turns him.</summary>
        static void Open059(Run r)
        {
            var g = r.G;
            r.Send(Enf, "d:Service lane");
            r.To(Max(r.Send(Scout, "d:Meet driver"), r.Send(Fixer, "d:Boarding pavilion")));
            r.Do(Scout, "listen", Driver); r.Done(Scout);
            TurnDriver(r);
            r.Is(g.Driver.Turned, "the driver is turned");
        }
        /// <summary>The Scout gives the rival car the false tip once it is parked (as route B's Scout does), or from `from` (a later tip).</summary>
        static void Tip059(Run r, int from = 0)
        {
            var g = r.G;
            r.To(Math.Max(g.Tick, r.SendTo(Scout, TipSpot(r))));
            r.Until(() => g.CarSt == CrewCar.Parked, T(200));
            if (from > g.Tick) r.To(from);
            r.Do(Scout, "mislead", "rivals"); r.Done(Scout);
        }
        /// <summary>The Tech boards the getaway car at the square and drives it round to the service drive; returns the tick it is there.</summary>
        static int DriveRound(Run r, string who = Tech)
        {
            var g = r.G;
            r.To(Math.Max(g.Tick, r.SendTo(who, g.GetawaySeat)));
            r.Do(who, "board", "car"); r.Done(who);
            r.Do(who, "drive-car", "service-drive");
            return r.Op(who).BusyEnd;
        }

        static void Pickup059()
        {
            Add("P59-DAY", "059", "Brief 059 K7, K8, K10 (Pulse §4, §5): no orders. The crew is nowhere before its entry; the car enters, drops three by the cell's car, rounds to the park at 3:10 and puts the lookout out; the posted three take the square's first three spots by the greedy rule; their beats turn them; the wheelman stays aboard.", r =>
            {
                var g = r.G;
                r.To(g.CrewEntry - 1);
                r.Is(g.RivalCarPose() == null && g.CarAt("car:rivals") == null && g.RivalPoses().members.Count == 0 && !g.RivalsPresent, "before its entry the crew is nowhere on the map (GDD §07)");
                r.Eq(g.CrewDropLeave + g.CrewDropArrive * 0, g.CrewDropArrive + T(g.R.Crew.DropS), "the drop stop is rivals.drop long");
                r.To(g.CrewEntry);
                r.Is(g.CarSt == CrewCar.Entering && g.CarAt("car:rivals") == g.S.CarStand(g.S.RivalCarEntry) && g.Log.Exists(l => l.Code == "rivals.coming" && l.Tick == g.CrewEntry), "it enters at rivals.car.entry, a target from then (rivals.coming)");
                r.To(g.CrewDropArrive);
                r.Is(g.CarSt == CrewCar.Dropping && g.Log.Exists(l => l.Code == "rivals.drop" && l.Tick == g.CrewDropArrive), "it stops at the drop (rivals.drop)");
                r.To(g.CrewDropArrive + T(g.R.Crew.StepS) * 3);
                r.Is(Mem(r, 1).OnFoot && Mem(r, 2).OnFoot && Mem(r, 3).OnFoot && Mem(r, 4).Mode == CrewMode.Aboard && Mem(r, 5).Mode == CrewMode.Aboard, "the posted three are out, rivals.step apart; the lookout and wheelman are aboard");
                r.Eq((g.S.Stands[0].Ambush[Mem(r, 1).Spot], g.S.Stands[0].Ambush[Mem(r, 2).Spot], g.S.Stands[0].Ambush[Mem(r, 3).Spot]), ("ambush-3", "ambush-1", "ambush-2"), "the greedy spots (Pulse §3: rival-1 ambush-3, rival-2 ambush-1, rival-3 ambush-2)");
                r.To(g.CrewDropLeave);
                r.Eq(g.CarSt, CrewCar.Rounding, "it leaves the drop rivals.drop after it stopped and rounds to the park");
                r.To(g.RivalAt);
                r.Is(g.CarSt == CrewCar.Parked && g.CarAt("car:rivals") == g.S.CarStand(g.S.RivalsPark) && g.Log.Exists(l => l.Code == "rivals.arrive" && l.Tick == T(190)), "it parks at rivals.park at 3:10 exactly (§07)");
                r.Is(g.S.At(g.S.RivalsPark) == new Pt(9050, -3100), "rival-park is Apron Road's south kerb (90.5, -31): Vantage's 059 point");
                r.To(g.RivalAt + T(g.R.Crew.OutS) + 1);
                r.Is(Mem(r, 4).Mode == CrewMode.Walking && Mem(r, 4).Goal == "lookout", "the lookout gets out rivals.out after the park and walks to his post");
                r.Until(() => Mem(r, 4).Mode == CrewMode.Posted, T(30));
                r.Is(Mem(r, 4).Post == "lookout" && G.Within(g.MemberPos(Mem(r, 4), g.Tick), g.S.Stand(g.S.RivalLookout), 250), "he posts at rivals.posts.lookout, facing the official bay");
                r.Until(() => Mem(r, 3).Mode == CrewMode.Posted, T(60));
                r.Is(Mem(r, 1).Post == "spot" && Mem(r, 2).Post == "spot" && Mem(r, 3).Post == "spot", "the three stand at their spots round our car");
                var f0 = g.MemberPose(Mem(r, 1))!.Value.facing; var seen = new HashSet<(int, int)> { (f0.X, f0.Y) };
                for (int k = 0; k < 42; k++) { r.Wait(T(1)); var f = g.MemberPose(Mem(r, 1))!.Value.facing; seen.Add((f.X, f.Y)); }
                r.Is(seen.Count >= 3, $"his beats turn him through the loop (0, +30, ±60 degrees): {seen.Count} facings in 42 s");
                r.Is(Mem(r, 5).Mode == CrewMode.Aboard && g.MemberBeat("rival-5").Length > 0, "the wheelman stays in the car (his beat is the host's)");
                r.Is(g.MemberBeat("rival-1").Length > 0 && g.RivalNext("car:rivals") is var nx && nx.HasValue && nx.Value.kind == "follows", "host reads: a beat name; the car's next move (follows Vale's car)");
            });

            Add("P59-B", "059", "Brief 059 §1 (route B, Sky 1a, 3a): the driver turned (the lookout is warned off), the tip at the parked rival car, Vale gets in at 6:13 and is driven round (car.parks), the Tech drives the getaway car round behind it, the Enforcer takes Vale in the car (no struggle) and carries him 3.5 m; the safe fight at the yard with the posted three, who walked after the car.", r =>
            {
                var g = r.G;
                Open059(r);
                r.Until(() => Mem(r, 4).Mode == CrewMode.Gone || g.Over, T(330));
                r.Is(Mem(r, 4).Mode == CrewMode.Gone && g.Log.Exists(l => l.Code == "rivals.warned"), "the turned driver's word: the lookout walks over and leaves the map (rivals.warned)");
                Tip059(r);
                r.Is(g.CarSt == CrewCar.TipOut || g.CarSt == CrewCar.Away, "the tip sends the car and wheelman away the way they came");
                r.To(T(370));
                r.Eq(g.Vale, ValeAt.Pickup, "6:10 Vale at his car at the official bay");
                r.To(T(370) + T(g.R.ValeInS));
                r.Is(g.Vale == ValeAt.InCar && g.Log.Exists(l => l.Code == "car.leaves" && l.Tick == g.Tick), "6:13 he gets in and the turned driver drives him round (car.leaves)");
                r.Eq(r.G.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "take" }).code, "car-not-parked", "a Take while the car drives round is refused (car-not-parked)");
                int parks = g.CarInBayTick; r.To(parks);
                r.Is(g.PickupService && g.Log.Exists(l => l.Code == "car.parks" && l.Tick == parks) && g.CarPos() == g.S.CarStand(g.S.CarService), $"it parks in the service bay at {Report.Clock(parks)} (car.parks)");
                int there = DriveRound(r);
                r.Do(Enf, "take"); int takeEnd = r.Op(Enf).BusyEnd;
                r.Is(g.TakeInCar && !g.Acts.Exists(a => a.Take), "the Take is in the car: no struggle, nothing to witness");
                r.To(Math.Max(there, takeEnd));
                r.Is(g.Vale == ValeAt.WithEnforcer && g.FightBranch == "safe" && g.CurStand.Id == "service-drive", "Vale is with the Enforcer; the safe fight at the service drive");
                r.Is(g.Driver.Has("saw-getaway") && g.Driver.Memory.Exists(m => m.Act == "saw-getaway" && m.By == Tech), "the turned driver saw our car pull in behind him (Motive M059-2.4: memory saw-getaway, the cell's car; whether he gives it up is the Talker test)");
                var rivals = g.Hostiles.Where(h => h.Id.StartsWith("rival-", StringComparison.Ordinal)).Select(h => h.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();
                r.Eq(string.Join(",", rivals), "rival-1,rival-2,rival-3", "the fight's bodies are the posted three: the lookout warned off, the wheelman away on the tip (Pulse §7)");
                r.Is(g.FightWhy.Contains("lookout-warned-off") && g.FightWhy.Contains("wheelman-away"), "fight why: lookout-warned-off, wheelman-away");
                r.Is(Mem(r, 1).SpotStand == "service-drive" && Mem(r, 2).SpotStand == "service-drive" && Mem(r, 3).SpotStand == "service-drive", "the three walked after our car to the service drive's spots (F1)");
                long carry = G.Dist(r.Op(Enf).Pos, g.GetawayCarPos());
                r.Is(carry <= 800, $"the carry is short: {(carry / 100.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} m from the service door to our car (brief 059: 3.5 m from Vale's back door)");
                r.FightItOut(120, Scout, Fixer, Tech);
                r.Extract();
                r.Line(g.Result == Outcome.Success, $"route B on the 059 engine wins ({g.Result} {g.FailCode} at {Report.Clock(g.Tick)}): Waymark re-composes route B");
            });

            Add("P59-NOBODY", "059", "Brief 059 K4: the turned driver waits with Vale in the service bay until the close; nobody takes him; at 8:30 he drives on: vale-leaves.", r =>
            {
                var g = r.G;
                Open059(r); Tip059(r, T(g.S.Over - g.R.MisleadAwayS - g.R.MisleadS) + 1); // a tip that runs past the close: no tail comes back before it
                r.To(T(390));
                r.Until(() => g.CarInBayTick > 0 && g.Tick >= g.CarInBayTick, T(60));
                r.Is(g.Vale == ValeAt.InCar && g.PickupService, "Vale waits in his car in the bay");
                r.To(T(g.S.Over) - 1);
                r.Is(!g.Over && g.Vale == ValeAt.InCar, "he waits until the close, past the driver's day in the people file (7:30)");
                r.To(T(g.S.Over));
                r.Eq((g.Result, g.FailCode, g.Tick), (Outcome.Failure, "vale-leaves", T(g.S.Over)), "at 8:30 the driver gives up and drives Vale on");
                r.Is(g.Log.Exists(l => l.Code == "car.leaves" && l.Tick == T(g.S.Over)), "car.leaves at the close");
            });

            Add("P59-UNTURNED", "059", "Brief 059 K2 (Sky 3a): with the driver unturned, Vale gets in at 6:13 and is driven away: the mission fails vale-leaves.", r =>
            {
                var g = r.G;
                r.To(T(370) + T(g.R.ValeInS));
                r.Eq((g.Result, g.FailCode, g.Tick), (Outcome.Failure, "vale-leaves", T(370) + T(g.R.ValeInS)), "vale-leaves at 6:13");
                r.Is(g.Cause != null && g.Cause.Why == "driven-away", "the cause: driven away");
            });

            Add("P59-FIRST", "059", "Brief 059 K5: the getaway car on the service drive before Vale's car comes: the turned driver stops at the drive's mouth on Apron Road and the hand-off happens there, in the car.", r =>
            {
                var g = r.G;
                Open059(r); Tip059(r);
                r.To(Math.Max(g.Tick, DriveRound(r)));
                r.Is(g.CurStand.Id == "service-drive" && g.Log.Exists(l => l.Code == "car.parked"), "our car stands on the service drive");
                r.To(T(370) + T(g.R.ValeInS));
                r.Until(() => g.CarInBayTick > 0 && g.Tick >= g.CarInBayTick, T(60));
                r.Is(g.CarAtMouth && g.Log.Exists(l => l.Code == "car.blocked"), "the driver stops at the drive's mouth (car.blocked)");
                r.Is(g.S.Drive.SurfaceAt(g.CarPos()) == 'r', "on the carriageway (Apron Road), not the drive");
                r.Is(g.PickupService && g.TakeInCar, "the Take there is in-car (brief 059 §1.4)");
                r.To(Math.Max(g.Tick, r.SendTo(Enf, g.S.Grid.NearestFree(g.CarPos() + new Pt(0, -250)) ?? g.CarPos())));
                r.Do(Enf, "take"); r.Done(Enf);
                r.Eq(g.Vale, ValeAt.WithEnforcer, "taken from the back seat at the mouth");
                long carry = G.Dist(r.Op(Enf).Pos, g.GetawayCarPos());
                r.Line(carry > 1000, $"the carry is longer, on the pavement: {(carry / 100.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} m (brief 059: about 18 m)");
            });

            Add("P59-TAIL", "059", "Brief 059 F2, K9 (the yard case): no tip. Vale's car leaves the bay with Vale; the rival car follows rivals.follow later to rivals.car.tail; the wheelman walks to Vale's door and reaches him before the Take is done: rivals-reach-vale.", r =>
            {
                var g = r.G;
                Open059(r);
                r.To(T(370) + T(g.R.ValeInS));
                int moves = g.Tick;
                r.To(moves + T(g.R.Crew.FollowS));
                r.Is(g.CarSt == CrewCar.Tail && g.Log.Exists(l => l.Code == "rivals.follows"), "the rival car pulls out after Vale's car rivals.follow later (rivals.follows)");
                r.Until(() => g.CarSt == CrewCar.Stopped, T(30));
                r.Is(G.Within(g.RivalCarPose()!.Value.pos, g.S.CarStand(g.S.RivalCarTail), 100), "it stops at rivals.car.tail");
                r.Until(() => g.CarInBayTick > 0 && g.Tick >= g.CarInBayTick, T(60));
                r.Do(Enf, "take");
                r.Until(() => g.Over, T(60));
                r.Eq((g.Result, g.FailCode), (Outcome.Failure, "rivals-reach-vale"), $"the wheelman reaches Vale's car before the Take is done ({Report.Clock(g.Tick)})");
                r.Is(g.Cause != null && g.Cause.Culprits.Count > 0 && g.Cause.Culprits[0].Id == "rival-5", "the culprit is the wheelman");
            });

            Add("P59-TIP", "059", "Brief 059 F3 (the driver turned, so the mission runs past 6:13): the tip lands on the parked car: it rides off the way it came with the wheelman, is off the map, re-enters and parks again cell.mislead.away after the landing (rivals.back); the three stay.", r =>
            {
                var g = r.G;
                r.To(r.SendTo(Scout, TipSpot(r)));
                r.Until(() => g.CarSt == CrewCar.Parked, T(220));
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "mislead", Target = "rivals" }).code, "only-scout", "only the Scout gives the tip");
                r.Do(Scout, "mislead", "rivals"); int lands = r.Op(Scout).BusyEnd; r.Done(Scout);
                r.Is(g.Tick == lands && g.CarSt == CrewCar.TipOut && g.Chain.Contains("rivals-false-tip"), "it lands: the car pulls out (TipOut)");
                r.Is(Mem(r, 1).OnFoot && Mem(r, 2).OnFoot && Mem(r, 3).OnFoot, "the three stay at their posts (the tip takes the car and wheelman only)");
                r.Until(() => g.CarSt == CrewCar.Away, T(60));
                r.Is(g.RivalCarPose() == null && g.CarAt("car:rivals") == null, "off the map");
                r.Eq(g.RivalsDelayedUntil, lands + T(g.R.MisleadAwayS), "away until the landing + cell.mislead.away (the HUD's window)");
                r.To(lands + T(g.R.MisleadAwayS));
                r.Is(g.CarSt == CrewCar.Parked && g.Log.Exists(l => l.Code == "rivals.back" && l.Tick == g.Tick), "it parks again at the end of the tip (rivals.back)");
                r.Wait(T(g.R.Crew.FollowS));
                r.Is(g.CarSt == CrewCar.Tail, "Vale's car is gone from the bay: parked again, it follows (F2's second clause)");
            }, Turned(Driver));

            var early = new Start(); early.Turned.Add(Inez);
            Add("P59-EARLY", "059", "Brief 059 F6 (the late-arrival rule): the cell has Vale before the rival car comes. The safe fight starts with the crew still off the map; the car enters as planned, stops at the stand's rival stop, and all five step out there for the stand's spots by role.", r =>
            {
                var g = r.G;
                int at = Max(r.Send(Tech, "cabinet"), r.Send(Enf, "d:Office entrance"));
                r.To(at);
                r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                r.Do(Tech, "reroute", "patrol");
                r.Do(Fixer, "call", Inez);
                r.Until(() => !g.GuardAtDoor, T(120));
                r.Do(Enf, "enter"); r.Done(Enf);
                r.Is(g.Vale == ValeAt.WithEnforcer && g.FightBranch == "safe" && g.Tick < g.CrewEntry, $"the cell has Vale at {Report.Clock(g.Tick)}, before the rivals enter ({Report.Clock(g.CrewEntry)})");
                r.Eq(g.Hostiles.Count(h => h.Id.StartsWith("rival-", StringComparison.Ordinal) && !h.Present), 5, "five bodies, none on the map yet");
                r.To(g.CrewEntry);
                r.Is(g.CarSt == CrewCar.Entering, "the car enters as planned");
                r.Until(() => g.CarSt == CrewCar.Stopped, T(30));
                r.Is(G.Within(g.RivalCarPose()!.Value.pos, g.S.CarStand(g.S.Stands[0].RivalStop), 150), "it stops at the square stand's rival stop");
                r.Wait(T(g.R.Crew.StepS) * 5 + 1);
                r.Eq(g.Hostiles.Count(h => h.Id.StartsWith("rival-", StringComparison.Ordinal) && h.Present), 5, "all five stepped out, rivals.step apart, and walk to their spots");
            }, early);

            Add("P59-GUN", "059", "Brief 059 F5: gunfire before 2:35. The car enters within rivals.drawn (16 s) of the shot and rides to rivals.car.front; the riders step out there and walk to their places at the Office front; the wheelman stays aboard.", r =>
            {
                var g = r.G;
                r.To(r.Send(Enf, "d:Office approach"));
                r.Wait(T(5));
                r.Fire(Enf, "camera:entrance");
                r.Until(() => g.RivalsDrawn, T(20));
                int shot = g.Tick;
                r.Is(g.RivalsDrawn && g.CarSt == CrewCar.Off, "drawn before the car came");
                r.Until(() => g.CarSt != CrewCar.Off, T(30));
                r.Is(g.CarSt == CrewCar.Front && g.Tick <= shot + T(g.R.RivalsDrawnS), $"it enters within 16 s of the shot, for the front ({Report.Clock(g.Tick)})");
                r.Until(() => g.CarSt == CrewCar.Stopped, T(60));
                r.Is(G.Within(g.RivalCarPose()!.Value.pos, g.S.CarStand(g.S.RivalCarFront), 150), "it stops at rivals.car.front");
                r.Until(() => Mem(r, 4).OnFoot, T(30));
                r.Is(Mem(r, 1).Goal == "front" && Mem(r, 5).Mode == CrewMode.Aboard, "the riders walk to the front; the wheelman stays aboard");
            });

            // S1 (Sky 2026-10-25: no): P59-S1-GO and P59-S1-HOLD are retired with fight.safe.leaveUnfought; the S1-* scenarios (ScenariosAlways059.cs) hold the rule.

            // S2: the loud fight at the service drive: the officers come from the nearest entry, or the north street
            void S2(Run r, bool nearest)
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, BesideDoor), r.SendTo(Scout, g.GetawaySeat)));
                r.Do(Scout, "board", "car"); r.Done(Scout); r.Do(Scout, "drive-car", "service-drive");
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech); r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                int mark = g.NextMarkOf(g.GuardP, g.Tick); if (mark - g.Tick < T(30)) r.To(mark + 1);
                r.Fire(Enf, GuardR); r.Is(r.Until(() => g.GuardDown, T(30)), "the Enforcer shoots the guard down: the run is loud");
                r.To(r.Send(Enf, "d:Office entrance")); r.Do(Enf, "enter"); r.Done(Enf);
                r.Is(g.FightBranch == "loud" && g.CurStand.Id == "service-drive", "the loud final fight at the service-drive stand");
                var off = g.Hostiles.Where(h => h.Id.StartsWith("officer-", StringComparison.Ordinal)).ToList();
                if (!r.Is(off.Count > 0 && off[0].Run != null, "officers come")) return;
                var from = off[0].Run!.Points[0];
                long dOfficial = G.Dist(from, g.S.Entrances.Find(e => e.kind == "official").stand), dNorth = G.Dist(from, g.S.Stand(g.S.FightEntry));
                if (nearest) r.Is(dOfficial < 100 && g.Log.Exists(l => l.Code == "fight.loud" && l.Text.Contains("official entrance")), "S2 nearest: from the official entrance");
                else r.Is(dNorth < 100 && g.Log.Exists(l => l.Code == "fight.loud" && l.Text.Contains("north street")), "S2 response: from the north street");
            }
            Add("P59-S2-NEAR", "059", "Sky's S2 (fight.loud.entry nearest, Motive's pick): the loud fight at the service drive takes its officers from the official entrance.", r => S2(r, true));
            Add("P59-S2-NORTH", "059", "Sky's S2 the other way (fight.loud.entry response): from the north street, as before.", r =>
            {
                if (!r.Is(r.Inputs != null, "inputs")) return;
                var alt = Alt(r, With059(r.Inputs!, "\"entry\": \"nearest\"", "\"entry\": \"response\""));
                S2(alt, false); foreach (var c in alt.Checks) r.Is(c.ok, "[S2 response] " + c.what);
            });

            // S3: the posted three go after a Driven car at the run or the walk
            void S3(Run r, int pace)
            {
                var g = r.G;
                r.Until(() => Mem(r, 3).Mode == CrewMode.Posted, T(240));
                r.To(Math.Max(g.Tick, r.SendTo(Tech, g.GetawaySeat)));
                r.Do(Tech, "board", "car"); r.Done(Tech);
                r.Do(Tech, "drive-car", "service-drive"); int left = g.Tick;
                r.To(left + T(g.R.Crew.FollowS));
                r.Is(g.Log.Exists(l => l.Code == "rivals.follow-car" && l.Tick == g.Tick), "rivals.follow later the three go after our car (rivals.follow-car)");
                r.Eq((Mem(r, 1).Speed, Mem(r, 2).Speed, Mem(r, 3).Speed), (pace, pace, pace), $"at {pace} cm/s");
                r.Until(() => Mem(r, 1).Mode == CrewMode.Posted && Mem(r, 2).Mode == CrewMode.Posted && Mem(r, 3).Mode == CrewMode.Posted, T(90));
                r.Is(Mem(r, 1).SpotStand == "service-drive" && Mem(r, 3).SpotStand == "service-drive", $"posted at the service drive's spots ({Report.Clock(g.Tick)}; the car parked {Report.Clock(r.Op(Tech).BusyEnd)})");
            }
            Add("P59-S3-RUN", "059", "Sky's S3 (rivals-follow-cm-s 280, Motive's pick): the posted three run after a Driven car.", r => S3(r, 280));
            Add("P59-S3-WALK", "059", "Sky's S3 the other way (the walk, 160): they walk after it.", r =>
            {
                if (!r.Is(r.Inputs != null, "inputs")) return;
                var alt = Alt(r, With059(r.Inputs!, "\"rivals-follow-cm-s\": \\{ \"v\": 280,", "\"rivals-follow-cm-s\": { \"v\": 160,"));
                S3(alt, 160); foreach (var c in alt.Checks) r.Is(c.ok, "[S3 walk] " + c.what);
            });

            Add("P59-REFUSE", "059", "Brief 059 K3, K6: the refusals. Drive: drive-no-driver, unknown-stand, drive-here, drive-busy, drive-fight. Take: driver-brings-vale at the front with the driver turned.", r =>
            {
                var g = r.G;
                string Code(string op, string verb, string target) => g.Issue(new Order { Tick = g.Tick, Op = op, Do = verb, Target = target }).code;
                r.Eq(Code(Tech, "drive-car", "service-drive"), "drive-no-driver", "not in the car: drive-no-driver");
                r.To(r.SendTo(Tech, g.GetawaySeat)); r.Do(Tech, "board", "car"); r.Done(Tech);
                r.Eq(Code(Tech, "drive-car", "nowhere"), "unknown-stand", "no stand: stand-unknown");
                r.Eq(Code(Tech, "drive-car", "square"), "drive-here", "the stand it is at: drive-here");
                r.To(r.SendTo(Scout, g.GetawaySeat)); r.Do(Scout, "board", "car"); r.Done(Scout);
                r.Do(Tech, "drive-car", "service-drive"); int arr = r.Op(Tech).BusyEnd;
                r.Eq(Code(Scout, "drive-car", "square"), "drive-busy", "a second Drive while it drives (the car's people are mid-act): drive-busy");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "move", To = "d:Courtyard" }).code, "agent-busy", "a passenger rides, busy until it arrives");
                r.To(arr); r.Is(g.CurStand.Id == "service-drive" && G.Within(r.Op(Scout).Pos, g.GetawayCarPos(), 300), "both arrive with the car");
                r.Is(g.LiveCoverOf("car/getaway") == 0, "the getaway car's cover points went with it (brief 059: a car that drives off takes its cover)");
            });

            Add("P59-LOUD-YARD", "059", "Motive M059-5.4 (careless-loud-b-yard, a measurement): route B taken at Alert 2 (the Enforcer shot the pickup camera out early): the in-car Take, a 3.5 m carry, Board, Go. The loud fight at the service drive takes its officers from the official entrance (S2); measured: whether they hold the car before the cell's Go.", r =>
            {
                var g = r.G;
                Open059(r); Tip059(r);
                r.To(Math.Max(g.Tick, T(370) + T(g.R.ValeInS))); r.Until(() => g.PickupService || g.Over, T(60));
                // the earlier shot: the Fixer at the pavilion shoots the pickup camera out as Vale's car parks (late, so the response's seal does not end the run first)
                r.Do(Fixer, "draw"); r.Wait(g.DrawT + 1); r.Fire(Fixer, "camera:pickup"); r.Until(() => g.CameraDark("pickup") || g.Over, T(20));
                r.Is(g.Gunfire > 0 && g.Alert >= 2, $"a shot: Alert {g.Alert}, the run is loud");
                r.Do(Fixer, "holster");
                if (!r.Is(!g.Over && g.PickupService, $"Vale's car parks round the back ({g.Result} {g.FailCode})")) return;
                int there = DriveRound(r);
                r.Do(Enf, "take"); r.To(Math.Max(there, r.Op(Enf).BusyEnd));
                if (!r.Is(g.FightBranch == "loud" && g.CurStand.Id == "service-drive", $"the loud fight at the service drive ({g.FightBranch} {g.Result} {g.FailCode})")) return;
                var off = g.Hostiles.Where(h => h.Id.StartsWith("officer-", StringComparison.Ordinal)).ToList();
                r.Is(off.Count > 0 && off[0].Run != null && G.Within(off[0].Run!.Points[0], g.S.Entrances.Find(e => e.kind == "official").stand, 100), "its officers set out from the official entrance (S2 nearest)");
                int start = g.Tick;
                r.To(Math.Max(g.Tick, r.SendTo(Enf, g.GetawaySeat))); if (!g.Over) { r.Do(Enf, "board", "car"); r.Done(Enf); }
                var a = g.Over ? default : g.Issue(new Order { Tick = g.Tick, Op = "cell", Do = "go" }); // measured, not asserted: held or not is the line below
                r.Line(!a.ok, $"M059-5.4: the yard's loud officers hold the car before the cell can Go ({(a.ok ? "LEFT UNFOUGHT" : "held: " + a.ids)}; Go tried {Report.Clock(g.Tick)}, {((g.Tick - start) / 10.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} s after the fight began): if it leaves unfought, Motive's fallback sends them at the run's first Alert 2");
            });

            var turnedB = new Start(); turnedB.Turned.Add(Driver);
            Add("P59-FRONT", "059", "Brief 059 K3: with the driver turned the Take at the front is refused (driver-brings-vale): he is bringing Vale round the back.", r =>
            {
                var g = r.G;
                r.To(r.Send(Enf, "d:Official drop-off"));
                r.To(T(370));
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "take" }).code, "driver-brings-vale", "driver-brings-vale");
            }, turnedB);
            Add("P59-BEAT", "059", "Brief 059 K3 (GDD v0.25.0 §07): the front with the driver unturned and the rival car parked across from Vale's with its wheelman aboard: the pickup camera looped, the Take at 6:10 fails rivals-reach-vale (beat-to-pickup); the wheelman is the culprit.", r =>
            {
                var g = r.G;
                r.To(r.Send(Tech, "cabinet"));
                r.To(T(300)); r.Do(Tech, "loop", "pickup"); r.Done(Tech);
                r.To(r.SendTo(Enf, Beside(r, g.CarPos())!.Value));
                r.To(T(g.S.ValeAtCar));
                r.Is(g.Crew[4].Mode == CrewMode.Aboard && g.CarSt == CrewCar.Parked, "the rival car is parked with its wheelman aboard");
                r.Do(Enf, "take", Vale);
                r.Eq(g.FailCode, "rivals-reach-vale", "the crew reaches Vale first");
                var c = CauseIs(r, "rivals-reach-vale", "beat-to-pickup", true, g.Crew[4].Id, "rival", Enf);
                var mp = g.MemberPose(g.Crew[4]);
                if (mp.HasValue) PosedAs(r, c?.Culprits.FirstOrDefault(), mp.Value.pos, mp.Value.facing, "the wheelman in the rival car");
            }, KeepVale(new Start()));
        }
    }
}
