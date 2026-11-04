// Engine 0.32.0 (brief 059 K1–K11; GDD v0.25.0 §07; Sky's answers 1a, 2a, 3a; Pulse briefs/059/pulse/rival-routine.md; Motive
// briefs/059/motive/rulings.json). The done-check's scenarios that outlive engine 0.41.0's block on arrival (brief 096):
//    P59-NOBODY    the turned driver waits with Vale in the bay; nobody takes him; at the close he drives on: vale-leaves
//    P59-UNTURNED  Sky 3a: an unturned driver drives Vale away at 6:13: vale-leaves
//    P59-FIRST     the getaway car on the service drive first: the turned driver pulls in at the kerb short of it (K5; car.kerb, 0.40.0); no crew
//    P59-S2-NEAR / P59-S2-NORTH  Sky's S2 both ways: the loud fight at the service drive, officers from the official entrance, or the north street
//    P59-REFUSE    Drive's and the Take's refusals: drive-no-driver, unknown-stand, drive-here, drive-busy, drive-held, drive-fight, driver-brings-vale, car-not-parked
//    P59-FRONT     the turned driver brings Vale round: the front Take is refused driver-brings-vale
// Retired by engine 0.41.0 (their rules are retired; the RIV-* scenarios, ScenariosRivals096*.cs, hold what replaced them): P59-DAY (the drop
// and the park: RIV-1, RIV-4), P59-B and P59-LOUD-YARD (the one-car way after the tip: the drive is boxed on arrival; route B ends at the
// block now), P59-TAIL (the car's tail and the wheelman at Vale's door: RIV-6, the lookout follows), P59-TIP (the false tip, Motive Q2 a),
// P59-EARLY (the late arrival: RIV-LATE), P59-GUN (the drawn car to the front: RIV-21), P59-S3-RUN/WALK (F1 after posting: see S3 below),
// P59-BEAT (the parked car's wheelman at the front Take: the lookout's watch, RIV-6b). Also retired: N113 and S1-GATE-EARLY-YARD (a blocker downed and Go past an unbroken crew: where the rival car blocks no blocker is picked; RIV-17, RIV-18, RIV-19).
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
        /// <summary>Engine 0.41.0 (test only): the crew killed before the mission (Start.CrewGone, all five), for the scenarios of other systems
        /// the block on arrival would box in: no car enters; the safe fight is over at its start (crew destroyed).</summary>
        static Start NoCrew(Start? s = null) { s ??= new Start(); foreach (var id in new[] { "rival-1", "rival-2", "rival-3", "rival-4", "rival-5" }) if (!s.CrewGone.Contains(id)) s.CrewGone.Add(id); return s; }
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
        /// <summary>Engine 0.41.0 (brief 096): the false tip is retired; route B's Scout deals with the lookout instead. She walks to beside his post,
        /// waits until he stands there and the clock reads at least `from` (default 4:45, so his 104 s away covers Vale's car leaving at 5:53),
        /// and misleads him (cell.mislead, rivals.lookout).</summary>
        static void Tip059(Run r, int from = 0)
        {
            var g = r.G; var post = g.S.Stand(g.S.RivalLookout);
            r.To(Math.Max(g.Tick, r.SendTo(Scout, g.S.Grid.NearestFree(post + new Pt(-300, 0)) ?? post)));
            r.Until(() => g.Crew[3].Mode == CrewMode.Posted && g.Crew[3].Post == "lookout", T(330));
            r.To(Math.Max(g.Tick, from > 0 ? from : T(285)));
            r.Do(Scout, "mislead", "rival-4"); r.Done(Scout);
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


            Add("P59-NOBODY", "059", "Brief 059 K4: the turned driver waits with Vale in the service bay until the close; nobody takes him; at 8:30 he drives on: vale-leaves.", r =>
            {
                var g = r.G;
                Open059(r); // brief 096: no crew (NoCrew), so nobody follows Vale's car
                r.To(T(390));
                r.Until(() => g.CarInBayTick > 0 && g.Tick >= g.CarInBayTick, T(60));
                r.Is(g.Vale == ValeAt.InCar && g.PickupService, "Vale waits in his car in the bay");
                r.To(T(g.ClockOver) - 1);
                r.Is(!g.Over && g.Vale == ValeAt.InCar, "he waits until the close, past the driver's day in the people file (7:30)");
                r.To(T(g.ClockOver));
                r.Eq((g.Result, g.FailCode, g.Tick), (Outcome.Failure, "vale-leaves", T(g.ClockOver)), "at 8:30 the driver gives up and drives Vale on");
                r.Is(g.Log.Exists(l => l.Code == "car.leaves" && l.Tick == T(g.ClockOver)), "car.leaves at the close");
            }, NoCrew());

            Add("P59-UNTURNED", "059", "Brief 059 K2 (Sky 3a): with the driver unturned, Vale gets in at 6:13 and is driven away: the mission fails vale-leaves.", r =>
            {
                var g = r.G;
                r.To(T(370) + T(g.R.ValeInS));
                r.Eq((g.Result, g.FailCode, g.Tick), (Outcome.Failure, "vale-leaves", T(370) + T(g.R.ValeInS)), "vale-leaves at 6:13");
                r.Is(g.Cause != null && g.Cause.Why == "driven-away", "the cause: driven away");
            });

            Add("P59-FIRST", "059", "Brief 059 K5, brief 096 (car.kerb, engine 0.40.0): the getaway car on the service drive before Vale's car comes: the turned driver pulls in at the kerb short of the drive on Apron Road (no longer across its mouth) and the hand-off happens there, in the car.", r =>
            {
                var g = r.G;
                Open059(r);
                r.To(Math.Max(g.Tick, DriveRound(r)));
                r.Is(g.CurStand.Id == "service-drive" && g.Log.Exists(l => l.Code == "car.parked"), "our car stands on the service drive");
                r.To(T(370) + T(g.R.ValeInS));
                r.Until(() => g.CarInBayTick > 0 && g.Tick >= g.CarInBayTick, T(60));
                r.Is(g.CarAtKerb && g.Log.Exists(l => l.Code == "car.kerbside") && !g.Log.Exists(l => l.Code == "car.blocked"), "the driver pulls in at the kerb short of the drive (car.kerbside), not across its mouth");
                r.Is(g.S.Drive.SurfaceAt(g.CarPos()) == 'r', "on the carriageway (Apron Road), not the drive");
                r.Is(g.PickupService && g.TakeInCar, "the Take there is in-car (brief 059 §1.4)");
                r.To(Math.Max(g.Tick, r.SendTo(Enf, g.S.Grid.NearestFree(g.CarPos() + new Pt(0, -250)) ?? g.CarPos())));
                r.Do(Enf, "take"); r.Done(Enf);
                r.Eq(g.Vale, ValeAt.WithEnforcer, "taken from the back seat at the mouth");
                long carry = G.Dist(r.Op(Enf).Pos, g.GetawayCarPos());
                r.Line(carry > 1000, $"the carry is longer, on the pavement: {(carry / 100.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} m (brief 059: about 18 m)");
            }, NoCrew()); // brief 096: car.kerb's scenario, not the crew's: the crew out of it (an early Drive is boxed by the block on arrival only at the drive's mouth, and the parked car on the drive is noticed)





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

            // S3 (P59-S3-RUN, P59-S3-WALK) retired by engine 0.41.0: the three never stand posted round a car that can still drive (the block on arrival boxes it first); rivals-follow-cm-s is the lookout's run now (RIV-6)

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


            var turnedB = new Start(); turnedB.Turned.Add(Driver);
            Add("P59-FRONT", "059", "Brief 059 K3: with the driver turned the Take at the front is refused (driver-brings-vale): he is bringing Vale round the back.", r =>
            {
                var g = r.G;
                r.To(r.Send(Enf, "d:Official drop-off"));
                r.To(T(370));
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "take" }).code, "driver-brings-vale", "driver-brings-vale");
            }, turnedB);
        }
    }
}
