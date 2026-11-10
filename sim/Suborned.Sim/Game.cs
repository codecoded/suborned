// The Handover, stepped at 10 Hz on integer state (GDD §11). The top-down test's causal model
// (~/Projects/the-suborned/testbed/reference/suborned-world-sim.cjs) moved onto the real civic block (sim/data/handover.site.json)
// and onto the rules as data (contracts/rules/rulebook.json, contracts/people/people.rules.json). Every number comes from the
// rulebook, the people rules or the site; this file holds behaviour only. Orders are the only input after the start state, so a
// run replays exactly from its start state and its orders, and its checkpoint hashes say so.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public enum Outcome { None, Success, Failure, Withdrawn }
    /// <summary>Where Vale is. Pickup: standing at his car at the official bay (6:10); InCar (brief 059 K1): in the back of his own car, from
    /// car.pickup.valeIn after he reaches it (appended last so every earlier value hashes as before).</summary>
    public enum ValeAt { Office, Leaving, Pickup, Taking, WithEnforcer, Car, Dead, InCar, Lent, Out } // engine 0.42.0 (car.lend): in the back of his lent car; out (the win fixed)

    public sealed partial class Game
    {
        const int INF = int.MaxValue / 4;
        public const int Hz = 10;
        public static int T(int seconds) => seconds * Hz;

        public readonly Rules R; public readonly Site S; public readonly Start Start;
        public readonly ulong StartHash;
        public int Tick;
        public readonly Op[] Ops = new Op[4];
        public readonly List<Person> People = new List<Person>(); // waitress, driver, guard, councillor
        public Person Inez => People[0];
        public Person Driver => People[1];
        public Person GuardP => People[2];
        public Person ValeP => People[3];
        public long Cash; public int Trail, Alert, Gunfire, Reported, Standing;
        public readonly int StandingAtStart; // brief 014: where the district's Standing stood at tick 0 (the debrief's Standing line reads from it)
        public Outcome Result; public string FailCode = "", FailText = "";
        public readonly List<LogLine> Log = new List<LogLine>();
        public readonly List<(Order order, bool ok, string code, string why)> Orders = new List<(Order, bool, string, string)>();
        readonly List<string> orderText = new List<string>(); // Orders[k].order.ToString() as it was issued (engine 0.25.0, brief 045: Hash.State no longer writes every order's JSON)
        /// <summary>Order k's canonical text, as Hash.State reads it: taken when it was issued (an order is never changed after), else written now.</summary>
        internal string OrderText(int k) => k < orderText.Count ? orderText[k] : Orders[k].order.ToString();
        public readonly List<(int tick, ulong hash)> Checkpoints = new List<(int, ulong)>();
        public readonly List<string> Chain = new List<string>();
        public readonly List<string> Actions = new List<string>();
        public readonly List<(int tick, string line, string who, string detail)> DebriefLines = new List<(int, string, string, string)>();
        readonly HashSet<string> incidents = new HashSet<string>(StringComparer.Ordinal);
        /// <summary>The incidents counted so far, in ordinal order (for the checkpoint hash).</summary>
        public List<string> IncidentKeys() { var l = new List<string>(incidents); l.Sort(StringComparer.Ordinal); return l; }

        // the cell
        public int EnforcerHealth => Enforcer.Gun.Health;
        public bool LoopOn;
        public string LoopCam = "entrance"; // the camera the loop holds: the entrance pole, or a world camera (brief 013: the Records Square mast)
        // the guard (§05 guard on door routine; mission.json windows.front-door)
        public int LureTick = -1, GuardAwayUntil, MissingNotice = INF;
        public int VisitWalkTicks => walkT; // the summoned lover's walk, from the grid route at walking pace (rulebook summon)
        public int GuardHealth => GuardGun.Health;
        public bool GuardHeld, GuardEngaged, GuardReachedCafe, GuardDown;
        Pt heldPos, heldFacing; string heldPhase = "";
        Path guardOut = null!, guardBack = null!, guardRun = null!; bool guardRunning; // the visit's walks (the summons), and its run back
        Pt cafeFace; // which way he faces at her (the site's café facing; brief 022: toward her post when she works a round)
        // the official car and the driver (the driver's Call, the camera block, is retired by brief 059: BlockMove stays INF, hashed as before)
        public int BlockMove = INF, BlockedFrom = INF, BlockedUntil;
        Pt? carStoppedAt; Path? carToService;
        long[]? carDrive; // car.pace (brief 023): mm along carToService at each tick from valeDriveStart, to its end; then parked in the bay (or at the drive's mouth)
        int valeDriveStart = -1; long carMouthMm = -1; bool mouthDecided; // brief 059 K2, K5: the turned driver's drive round with Vale in the back, and where it meets the drive
        /// <summary>Brief 059 K5: the turned driver found the service drive taken by the getaway car and stopped at its mouth on Apron Road.</summary>
        public bool CarAtMouth;
        readonly Path blockPath;
        // Vale
        /// <summary>PickupService (brief 059): Vale is in his car round the back, parked (in the service bay, or at the drive's mouth), where the
        /// Take is in-car. False at the front and while the car drives round.</summary>
        public ValeAt Vale = ValeAt.Office; public bool PickupService, EntryInProgress;
        Path? valePath;
        // the patrol (§05 patrol sweep)
        public bool PatrolDown, PatrolChasing, PatrolTracking; public int PatrolAwayUntil;
        public int PatrolHealth => PatrolGun.Health;
        Pt chasePos, patrolDownAt; OpId chaseOp = OpId.Enforcer;
        // the rivals (§05 rivals)
        public int RivalAt; public bool RivalsArrived, RivalsDrawn, RivalsTracking; public int RivalsDelayedUntil, RivalsDrawnTick;
        public int RivalsHealth => RivalsGun.Health;
        public string RivalsDelayMethod = "";
        // brief 059: the drawn crew walks member by member (GameCrew.cs); the one crew path is retired
        // the response unit (§05 response unit on report; district seals after it arrives)
        public int SecurityArrives = INF, Lockdown = INF; public bool SecurityDown, SecurityTracking;
        public int SecurityHealth { get { int n = 0; foreach (var g in SecGuns) n += g.Health; return n; } }
        Path securityPath; // the response unit's walk: the north street to the Office front (§05), or to a silent alarm's scene (brief 013)
        // the debrief's facts
        public bool InezWitnessedShooting; public string InezMethod = "";
        public string RouteKind = "", RouteGuard = "", RouteBot = "", RouteRival = "";
        /// <summary>Engine 0.30.0 (brief 060 #8): what held at the Enter (AtEntry) and at the Take (AtTake) that the debrief's route lines claim;
        /// null until then. Output only: never hashed, never read by a rule.</summary>
        public RouteFacts? AtEntry, AtTake;

        // numbers read once from the rules and the site
        readonly int eye, near, actReach, approachReach, listenReach, witnessRange;
        readonly int cellWalk, peopleWalk, secSpeed, guardSpeed, rivSpeed, chaseSpeed; // engine 0.34.0 (brief 067, Motive M067-B1): the cell's walk (cell.speed) and the city's (crowd.walk)
        int walkT, stayT, backT; readonly int rivalsArriveS, driverLastS, valeExitsS, valeAtCarS, overS, blockMoveT, blockHoldT;

        public Game(Rules rules, Site site, Start? start = null, string? startText = null, Crowd? crowd = null, PopulationRules? population = null)
        {
            R = rules; S = site; Start = start ?? new Start(); People2 = crowd; Range = site.IsRange;
            StepLimit = HarnessStepLimit; // engine 0.34.0: a harness's bound (0 in the host)
            FollowLock = Start.FollowLock ?? false; // tuning.md §11.5
            WeaponsFree = (Start.OneInput ?? R.OneInput) && !Start.FightLabOn && !Start.Pre039 ? true : Start.WeaponsFree ?? R.FreeStart; // tuning.md §14 (engine 0.19.0); brief 039 (weapons.oneInput): always on, out of the lab
            InitWalkers(crowd, population); // the people file's walkers, fresh; brief 098 step 0: with the start's counts, fewer or more (GamePopulation.cs)
            StartHash = Hash.Of(startText ?? "") ^ Hash.Of(Start.Canonical()) ^ DialHash(); // brief 098: the dial's rules only when the start sets counts
            eye = R.Eye; near = R.NearCm;
            if (R.MountOn && !Range) foreach (var c in S.Cameras) if (!R.MountCameraCm.ContainsKey(c.Kind)) throw new FormatException($"site camera {c.Id}: kind '{c.Kind}' has no lens height in perception.mount.cameras (brief 055 §7: unknown = reject)");
            ResolveStopFaces(); // brief 054 §3.3: every on-street stop's facing, or the start is rejected
            actReach = R.ActReachCm; approachReach = R.ApproachReachCm; listenReach = R.ListenReachCm;
            witnessRange = R.WitnessRangeCm;
            cellWalk = R.CellSpeed; peopleWalk = R.PeopleWalk; secSpeed = R.SecuritySpeed; guardSpeed = R.Sheet.GuardSpeed; rivSpeed = R.RivalsSpeed; chaseSpeed = R.PatrolChase;
            rivalsArriveS = S.RivalsArrive; int oldClock = Start.OldClock && !Range ? OldClockS : 0; driverLastS = S.DriverLast + oldClock; valeExitsS = S.ValeExits + oldClock; valeAtCarS = S.ValeAtCar + oldClock; overS = S.Over + oldClock; // brief 080 pacing: test only
            blockMoveT = T(S.BlockMoveS); blockHoldT = T(S.BlockHoldS);

            Cash = Start.Cash ?? S.Purse;
            Standing = StandingAtStart = Start.Standing ?? R.StandingStart;
            string[] crew = { "crew:scout", "crew:fixer", "crew:tech", "crew:enforcer" };
            // Arms (Sky 2026-10-15 Q1): the Enforcer's carbine is the cell's; each sidearm takes weapons.sidearmArms, issued Scout, Fixer, Tech
            int sidearms = (Start.Arms ?? S.Arms) / R.SidearmArms;
            InitLab(); InitKite(); // brief 027; tuning.md §7.9 the kite response in today too. brief 027 (EXPERIMENTAL): the fight lab's switches, weapons and health scale; off, today's (GameFightLab.cs)
            for (int i = 0; i < 4; i++)
            {
                var id = (OpId)i; bool enf = id == OpId.Enforcer;
                var gun = new Gun(Sim.Op.Name(id)) { W = Lab ? LoadoutWeapon(i) : enf ? R.Carbine : i < sidearms ? R.Pistol : null, Trained = enf, Health = (enf ? R.HealthEnforcer : R.HealthOp) * HealthScale, Aim = R.Sheet.AimOf(Sim.Op.Name(id)) };
                gun.Max = gun.Health;
                Ops[i] = new Op { Id = id, Pos = S.Stand(crew[i]), Gun = gun, Charge = Lab ? Labr!.ChargeMax : 0 };
            }
            if (Lab) CheckLoadout(Start.Arms ?? S.Arms);
            GuardGun = NewGun("guard", PistolW, true, R.HealthGuard);
            PatrolGun = NewGun("patrol", PistolW, true, R.HealthPatrol);
            RivalsGun = NewGun("rivals", PistolW, false, R.HealthRival);
            SecGuns = new Gun[R.ResponseOfficers];
            for (int k = 0; k < SecGuns.Length; k++) SecGuns[k] = NewGun($"security-{k + 1}", PistolW, true, R.HealthOfficer);

            string[] places = { S.GuardCafe, S.CarDriver, S.GuardPost, S.ValeExit };
            string[] roles = { "waitress", "driver", "guard", "councillor" };
            for (int i = 0; i < 4; i++)
            {
                var c = S.Role(roles[i]);
                var p = new Person { Role = roles[i], Id = c.Id, Name = c.Name, Temperament = Start.Temperament.TryGetValue(roles[i], out var tm) ? tm : c.Temperament, Need = c.Need, Place = places[i], Secret = c.Secret };
                if (p.Secret.Length > 0 && !R.SecretIds.Contains(p.Secret)) throw new FormatException($"cast {roles[i]}: secret '{p.Secret}' is not in people.rules secrets (unknown = reject)");
                var t = R.Temp(p.Temperament); R.CheckNeed(p.Need);
                if (crowd != null) // the need's record (brief 017): the people file's, of the kind the site casts them with
                {
                    if (!crowd.RecordOf.TryGetValue(c.Id, out var rr)) throw new FormatException($"cast {roles[i]}: {c.Id} is not in the people file");
                    if (crowd.Records[rr.record].kind != p.Need) throw new FormatException($"cast {roles[i]}: need record '{rr.record}' is not a {p.Need} record");
                    p.Record = rr.record; p.Size = rr.size; p.Holder = rr.holder;
                }
                for (int k = 0; k < 4; k++) p.Sc[k] = t.Start[k];
                People.Add(p);
                civGuns[roles[i]] = NewGun(roles[i], null, false, R.HealthCivilian);
            }
            // lovers (people.rules ties.kinds.lover): mutual, and an affair is exactly a lover tie (Motive, motive-lovers.md)
            for (int i = 0; i < 4; i++)
            {
                var c = S.Role(roles[i]); if (c.Lover.Length == 0 || Start.Untie.Contains(roles[i]) || Start.Untie.Contains(c.Lover)) continue;
                var other = PersonByRole(c.Lover);
                if (S.Role(c.Lover).Lover != roles[i]) throw new FormatException($"cast {roles[i]}: a lover tie is mutual, and {c.Lover} names no lover back");
                People[i].Lover = other;
            }
            foreach (var (door, _, _) in Start.Holds) if (!S.Entrances.Exists(e => e.id == door)) throw new FormatException($"start holds: '{door}' is not an entrance of the site (unknown = reject)");
            foreach (var p in People) if (p.Secret == "affair" && p.Lover == null && (Start.Untie.Contains(p.Role) || Start.Untie.Exists(r => S.Role(r).Lover == p.Role))) p.Secret = "";
            foreach (var p in People) if ((p.Secret == "affair") != (p.Lover != null)) throw new FormatException($"cast {p.Role}: an affair is a lover tie (people.rules secrets.affair requires lover-tie)");
            Inez.Suspected = Inez.Need; Driver.Suspected = Driver.Need; // Sky 2026-10-15: "in debt" shows as suspected from 0:00, from the briefing
            foreach (var role in Start.Turned) PersonByRole(role).Turned = true;
            foreach (var kv in Start.Known)
                foreach (var w in kv.Value)
                {
                    if (w == "sight" || !IsPersonRoleName(kv.Key)) ToldWatcher(kv.Key, w); // told of a watcher (rulebook sight): the briefing, a previous mission
                    else PersonByRole(kv.Key).Learn(w);
                }
            RivalAt = T(Start.RivalsAtS ?? rivalsArriveS + DrawBand(R.Seeds.RivalsId, "arrive", R.Seeds.RivalsArriveS)); // brief 080: every beat of the car moves with it (InitCrew)
            CurStand = S.Stands[0]; // brief 059 K6: the getaway car starts at its first stand (getaway.car)
            if (!Range) { InitCrew(); } // brief 059 K7: the crew's timetable, computed back from the park; a driver turned before the mission turned at 0:00 (F4)

            blockPath = new Path(S.CarBlockPath); // car.drive (brief 023): the block move keeps to drivable cells, from the pickup to the block's stand
            // engine 0.36.0 (car.footprint, brief 079, Sky: "walking through the car"): the response unit's walk is made once at load, and goes round the mission's start cars as the crowd's legs do (0.33.1 left it on the bare grid)
            var secCars = HomeCars(S, R);
            var sec1 = S.Grid.Route(S.Stand(S.SecEntry), S.Stand(S.SecVia), secCars) ?? S.Grid.Route(S.Stand(S.SecEntry), S.Stand(S.SecVia)) ?? throw new FormatException("no walk for the response unit");
            var sec2 = S.Grid.Route(S.Stand(S.SecVia), S.Stand(S.SecTo), secCars) ?? S.Grid.Route(S.Stand(S.SecVia), S.Stand(S.SecTo)) ?? throw new FormatException("no walk for the response unit");
            var sp = new List<Pt>(sec1); for (int i = 1; i < sec2.Length; i++) sp.Add(sec2[i]);
            securityPath = new Path(sp.ToArray());
            if (Range) InitRange(); // brief 029 (GameRange.cs): the fight range: no mission, no schedule, no cast but what is spawned
            else Note("start", $"{Cap(ValeName)} is inside. His official car will collect him later. The city is already moving.");
            CarEpochStep(); // engine 0.33.1 (car.footprint): the cars standing at the start (the crowd's legs built at load go round them)
            if (!Range) SeedStart(); // brief 080 (GameSeed.cs): the crowd's phases, the patrol's, the rivals' posts; nothing at seed 0
            InitTraffic(); BodyStep(); // brief 098 step 1 (GameTraffic.cs): passing cars, when the start sets cars; nothing otherwise; L9: the bodies laid at the start (GameBodies.cs)
        }

        public Person PersonByRole(string role)
        {
            foreach (var p in People) if (p.Role == role) return p;
            throw new FormatException($"no person in the role '{role}' (waitress, driver, guard, councillor)");
        }
        public Op OpOf(OpId id) => Ops[(int)id];
        public Op Enforcer => Ops[(int)OpId.Enforcer];
        public bool Within(Pt a, Pt b, int cm) => G.Within(a, b, cm);
        public long D2(Pt a, Pt b) => G.D2(a, b);
        public void Note(string code, string text) => Log.Add(new LogLine { Tick = Tick, Code = code, Text = text });
        /// <summary>The run fails: once, with its code, its words, and its cause (FailCause.cs: read-only, never hashed).</summary>
        void Fail(string code, string text, Func<FailCause>? cause = null) { if (Result != Outcome.None || LendSwallows(code)) return; RecordsAtEnd(); Result = Outcome.Failure; FailCode = code; FailText = text; Cause = MakeCause(code, cause); Note("fail." + code, text); }
        public bool Over => Result != Outcome.None;

        // ---------------------------------------------------------------- the tick

        /// <summary>Engine 0.34.0 (brief 067: S10 spun for ever once the patrol left the forecourt): the most Step calls a game made on this
        /// thread answers, a finished run's included, while a headless harness runs (Scenarios.Run, the CLI's script plays); 0 = no limit.
        /// A game copies it when made, so every game a scenario body makes is bounded too. The host never sets it: its Step never throws.</summary>
        [ThreadStatic] public static int HarnessStepLimit;
        /// <summary>This game's limit on Step calls (HarnessStepLimit when it was made); past it Step throws RunawayException. Never hashed.</summary>
        public int StepLimit;
        int stepCalls;
        /// <summary>Step calls so far, a finished run's included (never hashed): a caller that keeps stepping a finished run waits for nothing.</summary>
        public int StepCalls => stepCalls;

        /// <summary>One tick. It never throws: rules and site are checked at load (Redline E1); if the engine itself fails, the run ends
        /// as a failure with code engine-error and the reason in the log, deterministically, instead of taking the host (Unity) down.
        /// The one exception is a harness's StepLimit (0 in the host): a caller that waits for something that can never come (a scenario's
        /// loop on a condition, stepping a run that has ended) gets RunawayException instead of spinning for ever.</summary>
        public void Step()
        {
            if (StepLimit > 0 && ++stepCalls > StepLimit) throw new RunawayException(this);
            if (Over) return;
            try { StepInner(); }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                Result = Outcome.Failure; FailCode = "engine-error"; FailText = e.GetType().Name + ": " + e.Message;
                Cause = new FailCause { Fail = "engine-error", Why = "error", Tick = Tick }; // no culprit: the engine itself
                Note("fail.engine-error", FailText);
                Checkpoints.Add((Tick, Hash.State(this)));
            }
        }

        void StepInner()
        {
            Tick++; MissionYieldStep(); // brief 098 step 1 (GameTrafficYield.cs): a mission car meeting a passing car on its way holds a tick; nothing without traffic
            CarStandStep(); // engine 0.36.0 (car.footprint): a car that has come to stand clears its ground
            CarAheadStep(); // engine 0.37.0 (car.footprint.ahead; brief 081): and an agent steps off the ground it is about to stop on
            CarEpochStep(); // engine 0.33.1 (car.footprint): the standing cars as this tick starts, for the crowd's legs that set out now
            if (!Range) { GuardTimeline(); CarTimeline(); CrewStep(); ValeTimeline(); PatrolTimeline(); } // brief 029: the range has no schedule; brief 059: the crew's step (GameCrew.cs)
            foreach (var op in Ops)
            {
                op.StepFrom = op.Pos; // tuning.md §8.3: the gait's direction is this tick's step
                if (op.Busy != null && op.BusyEnd <= Tick) { var o = op.Busy; op.Busy = null; Land(op, o); if (Over) break; }
                if (op.Walk != null)
                {
                    op.WalkMm += op.WalkSpeed > 0 ? op.WalkSpeed : cellWalk; op.Pos = op.Walk.At(op.WalkMm) + OpLay(op); // brief 098 L9: his walk keeps the lay the law gave it
                    if (op.Running) RunAttention(op);
                    if (op.WalkMm >= op.Walk.Length) { op.Walk = null; op.WalkMm = 0; op.Running = false; }
                }
                else if (op.Driven) { StepDrive(op); if (Over) break; } // brief 025 (EXPERIMENTAL): the host's sample, checked (GameDrive.cs)
                if (op.Act != null) ApproachStep(op); // engine 0.23.0 (rulebook cell.approach): a person act walking in starts once in place (GameApproach.cs)
            }
            foreach (var op in Ops) if (op.CarriedBy.HasValue) op.Pos = OpOf(op.CarriedBy.Value).Pos; // a carried operative goes where the carrier goes
            if (getawayPath != null) foreach (var op in Ops) if (op.InCar) op.Pos = Car; if (!Over) GoWaitStep(); // brief 059 K6: everyone in the getaway car rides its Drive; engine 0.43.0 (car.go, Motive G1): a Go waiting for a Board lands the tick it lands (GameGoWaits.cs)
            if (Range) { RangeTick(); return; } // brief 029 (GameRange.cs): the fight range runs the fight and nothing of the mission
            if (!Over) Tether();
            if (!Over) Watchers();
            if (!Over) AttentionStep(); // brief 031: the watchers' attention clocks and the loiterers' reports (GameAttention.cs)
            if (!Over) PatrolChase();
            if (!Over) DrawnSeen();
            if (!Over) FightStep();
            if (!Over) Combat();
            if (!Over) CarsStep(); // tuning.md §10.7: a burning car blows (GameCars.cs; nothing while no car is named)
            if (!Over) GetawayAfterFire(); // brief 018: the blocker picked among those still standing after this tick's fire; blocker.out, car.free
            if (!Over) TrafficStep(); // brief 098 step 1 (GameTraffic.cs): passing cars hear this tick's shots before the crowd does; nothing without traffic
            if (!Over) CrowdStep();
            if (!Over) TakedownStep(); // brief 013: bodies, reports, check-in marks, suspicion, the response's units
            if (!Over && Tick >= Lockdown) Fail("sealed", "Security seals the district before the getaway car leaves.", Sealed); // brief 018: the car must leave, not only be reached
            if (!Over && !Range) CloseStep(); // Motive's ruling on Redline E8 (brief 080; GameSeed.cs): Vale's absence is noticed, then the close is a verdict
            // brief 059 K4: at the close the turned driver gives up and drives Vale on (a Take begun before it holds him until it ends); with no
            // driver, Vale leaves by other means (the rivals reach Vale only physically now: the tail, the fight)
            if (!Over && Tick >= T(overS) && Vale != ValeAt.WithEnforcer && Vale != ValeAt.Car && Vale != ValeAt.Taking && Vale != ValeAt.Lent && Vale != ValeAt.Out) GivesUp(); // brief 096 (car.box): a boxed official car stays: closed-in (GameKerb.cs)
            FightEnds(); BodyStep(); // fight.public: a fight ends quiet after its last shot, the final fight at its end, any fight with the mission; brief 098 L9: every body laid (GameBodies.cs)
            if (Tick % 100 == 0 || Over) Checkpoints.Add((Tick, Hash.State(this)));
        }

        public void RunTo(int tick) { while (Tick < tick && !Over) Step(); }
        public void Advance(int ticks) => RunTo(Tick + ticks);

        void ValeTimeline()
        {
            // the car waits for its driver: shot (security pulls Vale away, as before), or taken down and the city knows it, Vale stays in;
            // a driver taken down quietly isn't known, so Vale comes out to an empty car (brief 013, Sky answer 6)
            bool driverKnownDown = civGuns["driver"].Down || (Driver.Down && BodyOf(Driver) is Body db && Acts[db.Act].Landed);
            if (Tick == T(valeExitsS) && Vale == ValeAt.Office && !EntryInProgress && !driverKnownDown)
            {
                Vale = ValeAt.Leaving; // brief 059 K1: always to the official bay
                var to = BesideCar(CarPos(), 1); // engine 0.33.1 (car.footprint): to his car's kerb-side door, off its ground (0.33.0: its centre, S.ValeCar)
                valePath = new Path(Walk.Route(S.Stand(S.ValeExit), to) ?? new[] { S.Stand(S.ValeExit), to });
                Note("vale.exits", $"{Cap(ValeName)} steps out of the official entrance and walks to his car.");
            }
            if (Tick == T(valeAtCarS) && Vale == ValeAt.Leaving)
            {
                Vale = ValeAt.Pickup;
                Note("vale.at-car", $"{Cap(ValeName)} reaches the official car under the pickup camera.");
            }
            // brief 059 K1, K2 (car.pickup.valeIn): he gets in. With his driver at the car, unturned he drives Vale away (the mission is over, Sky 3a);
            // turned, he drives Vale round to the service bay. With no driver (down) or the car disabled, Vale waits by it.
            if (Tick == T(valeAtCarS) + T(R.ValeInS) && Vale == ValeAt.Pickup && !Driver.Down && !OfficialOut && !Start.ValeWaits)
            {
                Vale = ValeAt.InCar;
                if (!Driver.Turned)
                {
                    Note("car.leaves", $"{Cap(ValeName)} gets into his car and his driver drives him away.");
                    Fail("vale-leaves", $"{Cap(ValeName)} leaves the district in the official car.", () => Because("driven-away", true, Locate("car", "left"), Locate("councillor", "left")));
                    return;
                }
                StartValeDrive(); // brief 096 (car.box): unless his way out is boxed; then he waits with Vale aboard (GameKerb.cs)
            }
        }

        void PatrolChase()
        {
            if (!PatrolChasing || PatrolDown || PatrolAwayUntil > Tick || Tick % Hz != 0) return;
            if (PatrolGun.Target.Length > 0 && !patrolReturning) return; // engaging: it stops (Pulse §3.6)
            var target = patrolReturning ? SweepPos(Tick) : patrolOrigin; // to the shot's origin; resuming, back to its sweep
            if (patrolReturning && G.Within(chasePos, target, 100)) { PatrolChasing = false; patrolReturning = false; PatrolGun.NoFoeSince = -1; Note("patrol.sweep", "The patrol is back on its sweep."); return; }
            var t = Walk.NearestFree(target); var from = Walk.NearestFree(chasePos);
            if (!t.HasValue || !from.HasValue) return;
            var route = Walk.Route(from.Value, t.Value); if (route == null) return;
            chasePos = new Path(route).At(chaseSpeed * Hz); // a second's chase, as the top-down test moved it
        }

        // ---------------------------------------------------------------- where things are

        (Pt pos, Pt facing, string phase)? GuardPoseRaw() // brief 098 L9: GuardPose (GameBodies.cs) lays it
        {
            if (GuardDown) return null;
            if (GuardHeld) return (heldPos, heldFacing, heldPhase);
            var post = S.Stand(S.GuardPost);
            if (LureTick < 0 || Tick >= GuardAwayUntil) return (post, S.GuardFacing, "door");
            if (guardRunning) { long rm = (long)(Tick - LureTick) * guardSpeed; return (guardRun.At(rm), guardRun.Heading(rm), "returning to door"); }
            int age = Tick - LureTick;
            if (age < walkT) { long mm = guardOut.Length * age / walkT; return (guardOut.At(mm), guardOut.Heading(mm), "walking to café"); }
            if (age < walkT + stayT) return (guardOut.End, cafeFace, "at café"); // brief 022: facing her when she has a round
            long back = guardBack.Length * (age - walkT - stayT) / backT;
            return (guardBack.At(back), guardBack.Heading(back), "returning to door");
        }
        public bool GuardAtDoor => !GuardDown && !GuardHeld && (LureTick < 0 || Tick >= GuardAwayUntil);

        Pt ParkedCar(int t)
        {
            var pickup = blockPath.Points[0];
            if (t < BlockMove) return pickup;
            if (t < BlockedFrom) return blockPath.At(blockPath.Length * (t - BlockMove) / blockMoveT);
            if (t < BlockedUntil) return blockPath.End;
            if (t < BlockedUntil + blockMoveT) return blockPath.At(blockPath.Length - blockPath.Length * (t - BlockedUntil) / blockMoveT);
            return pickup;
        }
        public Pt CarPos()
        {
            if (carStoppedAt.HasValue) return carStoppedAt.Value;
            if (valeDriveStart < 0 || carToService == null) return ParkedCar(Tick);
            return carToService.At(CarDriveMm(Tick));
        }
        /// <summary>car.pace (Motive 2026-10-19, brief 023): the drive to the service bay, tick by tick from driverLast: each tick the car moves
        /// its cell's pace (car.pace.road on a carriageway, car.pace.yard on a drive or in a bay; cm/s = mm a tick), read where it stands, to the
        /// route's end, where it waits parked for Vale. Integer, computed once when the drive starts.</summary>
        long[] CarProfile(Path p)
        {
            var mm = new List<long> { 0 };
            long at = 0;
            while (at < p.Length)
            {
                int pace = S.Drive.SurfaceAt(p.At(at)) == 'r' ? R.CarRoadSpeed : R.CarYardSpeed;
                at = Math.Min(p.Length, at + Math.Max(1, pace)); mm.Add(at);
            }
            return mm.ToArray();
        }
        long CarDriveMm(int tick) { var d = carDrive!; int k = tick - valeDriveStart; return k <= 0 ? 0 : k >= d.Length ? d[d.Length - 1] : d[k]; }
        /// <summary>The tick the turned driver's car is parked round the back (in the service bay, or at the drive's mouth: brief 059 K5), or -1
        /// before it drives. Brief 059: it drives from Vale-in (car.pickup.valeIn), with Vale in the back.</summary>
        public int CarInBayTick => carDrive == null ? -1 : valeDriveStart + carDrive.Length - 1;
        /// <summary>Brief 059 (host read): the official car is parked round the back with Vale in it: the in-car Take's window is open.</summary>
        public bool ValeCarParked => PickupService && (Vale == ValeAt.InCar || Vale == ValeAt.Taking);
        /// <summary>Which way the official car points, for the host (read-only, never hashed; brief 023): engine 0.33.1 (brief 041, Sky's
        /// "cars-walk-through" shot: the host's own rule drew it across the bay, rear on the kerb by the door) always gives one, the same the
        /// walk's car.footprint uses: car.blockFacing (the official bay's long axis) at the pickup, along its drive while it moves,
        /// car.serviceFacing parked in the service bay, along the road at the drive's mouth, and the way it was going where it stopped.
        /// Never null now; nullable for the hosts that read it so.</summary>
        public Pt? CarHeading() => OfficialHeading();
        Pt? DriverPosRaw() // brief 098 L9: DriverPos (GameBodies.cs) lays it
        {
            if (Driver.Down) return null; if (driverOff != null) return LentDriverPos(); // engine 0.53.0: walking off since the lend (whatever car is taken after) // engine 0.42.0 (car.lend): out of his car, walking off
            var car = CarPos();
            if (valeDriveStart < 0 && car == blockPath.Points[0]) return S.Stand(S.CarDriver); // brief 059: on duty by his car at the pickup until Vale is in
            return OfficialOut ? BesideCar(car, -1) : car; // tuning.md §10.7: out of a disabled car (engine 0.33.1: the road side)
        }
        Pt? ValePosRaw() // brief 098 L9: ValePos (GameBodies.cs) lays it
        {
            switch (Vale)
            {
                case ValeAt.Leaving: { int span = T(valeAtCarS) - T(valeExitsS); return valePath!.At(valePath.Length * Math.Min(span, Tick - T(valeExitsS)) / span); }
                case ValeAt.Pickup: return BesideCar(CarPos(), 1); // engine 0.33.1 (car.footprint): at his car's kerb-side door until he gets in, or waiting by it (0.33.0: its centre, inside the body, unless disabled)
                case ValeAt.InCar: return CarPos(); case ValeAt.Lent: return LentPos(); // engine 0.53.0 (car.take): in the back of the taken car // brief 059 K1: in the back of his car (engine 0.42.0: lent too)
                case ValeAt.Taking: return OfficialOut || !PickupService ? BesideCar(CarPos(), 1) : CarPos(); // seized at the door at the front; in the back round the back
                case ValeAt.WithEnforcer: return Enforcer.Inside ? (Pt?)null : Enforcer.Pos + new Pt(100, 0);
                default: return null;
            }
        }
        Pt? PersonPosRaw(Person p) // brief 098 L9: PersonPos (GameBodies.cs) lays the cast
        {
            if (p.Down) return null;
            if (CrewOfPerson(p) is CrewMember cm) return !cm.Fight && cm.OnFoot ? MemberPose(cm)?.pos : null; // engine 0.33.0: a rival on the street (aboard or in the final fight, no takedown target)
            if (ServerOf(p) is RoundsData.Server sv) return RoundPoseAt(sv, EvRead(sv), Tick).Pos; // brief 022: a server's round
            if (p == Inez) return S.Stand(Inez.Place);
            if (p == Driver) return DriverPosRaw();
            if (p == GuardP) return GuardPoseRaw()?.pos;
            return ValePosRaw();
        }

        (Pt pos, Pt facing)? PatrolPoseRaw() // brief 098 L9: PatrolPose (GameBodies.cs) lays it
        {
            if (PatrolDown) return (patrolDownAt, new Pt(0, 0));
            if (PatrolUnit() is Unit pu && UnitPose(pu) is var up && up.HasValue) return up.Value; // brief 013: answering an alarm
            if (PatrolAwayUntil > Tick) return RerouteWalkPose() ?? (S.Stand(S.PatrolReroute), S.PatrolRerouteFacing); // engine 0.30.0: its walk out, wait and walk back (GameRerouteWalk.cs), else placed at the wait spot
            if (PatrolChasing) { var fc = OpOf(chaseOp).Pos - chasePos; return (chasePos, fc == new Pt(0, 0) ? Enforcer.Pos - chasePos : fc); } // brief 031 L2: with no all-round sense, the chasing patrol faces the operative it chases (was: always the Enforcer)
            return (SweepPos(Tick), S.PatrolFacing);
        }
        Pt SweepPos(int t)
        {
            int period = T(R.PatrolPeriodS), half = period / 2, ph = (t + PatrolPhase) % period;
            return G.Lerp(S.Stand(S.PatrolA), S.Stand(S.PatrolB), ph <= half ? ph : period - ph, half);
        }

        /// <summary>Brief 059: the crew is on the map (the car has entered and is not off it on a tip, or a member is on foot), before the final
        /// fight takes them in.</summary>
        public bool RivalsPresent { get { if (Range || rivalsAbsorbed) return false; if (CarSt != CrewCar.Off && CarSt != CrewCar.Away && CarSt != CrewCar.Gone) return true; foreach (var m in Crew) if (Up(m) && m.OnFoot) return true; return false; } }
        /// <summary>Retired by brief 059 (RivalPoses: five members and the car). Kept for hosts mid-migration: the rival car's pose while it is on the
        /// map, else the first member on foot. Never read by a rule.</summary>
        public (Pt pos, Pt facing)? RivalsPose()
        {
            if (!RivalsPresent) return null;
            var cp = RivalCarPose(); if (cp.HasValue) return (cp.Value.pos, cp.Value.heading);
            foreach (var m in Crew) if (MemberPose(m) is var p && p.HasValue) return p;
            return null;
        }
        /// <summary>Brief 059 K8 (host read): each member's place, facing and mode ("aboard", "walking", "posted", "talking", "boarding", "gone",
        /// "down", "fight"), and the car's place, heading and state ("off" while it is not on the map).</summary>
        public (List<(string id, Pt pos, Pt facing, string mode)> members, (Pt pos, Pt heading, string state) car) RivalPoses()
        {
            var l = new List<(string, Pt, Pt, string)>();
            foreach (var m in Crew)
            {
                var p = MemberPose(m); string mode = m.Gun.Down ? "down" : m.Fight ? "fight" : m.Mode.ToString().ToLowerInvariant();
                if (m.Mode == CrewMode.Gone && m.Gun.Down) continue; // killed before the mission (Start.CrewGone, test only): never on the map
                if (p.HasValue) l.Add((m.Id, p.Value.pos, p.Value.facing, mode));
                else if (m.Gun.Down) { if (BodyOf(m.P) is not Body hb || hb.Open) l.Add((m.Id, MemberPos(m, Tick), new Pt(0, 100), mode)); } // engine 0.33.0: where he fell, in the fight too (0.32.0 listed a fight body at (0,0)); carried, at his carrier; a stashed body not at all
                else { var cp0 = RivalCarPose(); if (cp0.HasValue && m.Mode == CrewMode.Aboard) l.Add((m.Id, cp0.Value.pos, cp0.Value.heading, mode)); }
            }
            var cp = RivalCarPose();
            return (l, cp.HasValue ? (cp.Value.pos, cp.Value.heading, CarSt.ToString().ToLowerInvariant()) : (default(Pt), new Pt(0, 100), "off"));
        }

        /// <summary>The response unit: its lead officer still up, and where the unit faces (its officers walk in file, `SecFileMm` apart).</summary>
        public (Pt pos, Pt facing)? SecurityPose()
        {
            if (SecurityDown || Tick < SecurityArrives) return null;
            for (int k = 0; k < SecGuns.Length; k++) if (!SecGuns[k].Down) return SecOfficerPose(k);
            return null;
        }
        long SecFileMm => R.BodiesOn ? R.BodyCrewCm * 10L : 1500; // the officers' spacing in file (mm); brief 098 L9 (groups spread): bodies.crew apart, was 1.5 m
        (Pt pos, Pt facing)? SecOfficerPoseRaw(int k) // brief 098 L9: SecOfficerPose (GameBodies.cs) lays it
        {
            if (Tick < SecurityArrives || SecGuns[k].Down) return null;
            if (!secJoined[k] && secDisp.TryGetValue(k, out var kd)) return DispPose(kd, secSpeed, Tick); // brief 027 F2 (lab kite): closing on a shooter
            if (secJoined[k]) { var h = HostileOf(SecGuns[k].Id)!; var p = h.Pos(Tick); return (p, Car - p == new Pt(0, 0) ? new Pt(100, 0) : Car - p); } // joined the loud fight: faces the car
            long mm = (long)(Tick - SecurityArrives) * secSpeed - k * SecFileMm, end = Math.Max(0, securityPath.Length - k * SecFileMm);
            if (mm < 0) mm = 0;
            if (mm >= end) return (securityPath.At(end), SectorFace(Back(securityPath), k)); // brief 098 L9: each faces his own sector
            return (securityPath.At(mm), securityPath.Heading(mm));
        }

        /// <summary>At the end of its run a unit holds facing back the way it came, as the top-down test's rivals and response unit did.</summary>
        static Pt Back(Path p) { var h = p.Heading(p.Length); return new Pt(-h.X, -h.Y); }

        // ---------------------------------------------------------------- who sees what (rulebook perception)

        /// <summary>A person's sight from `from` to `to` (rulebook perception; brief 055 §7 perception.mount: from the eye above its floor to the
        /// target's head or chest above its own, in 3D; off: one eye height over a flat datum). Fire lines read it too.</summary>
        bool Sees(Pt from, Pt to) => !Steamed(from, to) && (R.MountOn ? SeesFrom(from, FloorAt(from) + eye, to) : S.Sight(from, to, eye)); // brief 039: the Tech's steam
        /// <summary>The floor under p, block z cm (site grid.floor; brief 055).</summary>
        int FloorAt(Pt p) => S.Grid.FloorAt(p);
        /// <summary>Brief 055 §7: from an eye at height z (block cm) at `from`, the line to the target's head (its floor + eye) or its chest (its
        /// floor + chest) is clear: either is enough. own: the eye is a camera's lens, whose own piece never blocks it.</summary>
        bool SeesFrom(Pt from, int z, Pt to, bool own = false)
        {
            if (Steamed(from, to)) return false; // brief 039: the Tech's steam blocks every line, a camera's too
            int f = FloorAt(to);
            return S.Sight3(from, z, to, f + eye, own) || S.Sight3(from, z, to, f + R.MountChestCm, own);
        }
        /// <summary>Brief 055 §7: a camera's lens, block z cm: its floor plus its kind's mount (perception.mount.cameras).</summary>
        int LensZ(Camera c) => FloorAt(c.At) + (R.MountCameraCm.TryGetValue(c.Kind, out var m) ? m : throw new FormatException($"camera {c.Id}: kind '{c.Kind}' has no lens height in perception.mount.cameras"));
        /// <summary>Brief 055 §7: a line to a target by its id: a camera (Watch, shooting it out) is its lens point, whose own piece never blocks it;
        /// anything else is a person's head or chest (Sees).</summary>
        bool SeesId(Pt from, string id, Pt at)
        {
            if (R.MountOn && id.StartsWith("camera:", StringComparison.Ordinal)) { var cid = id.Substring(7); foreach (var c in S.Cameras) if (c.Id == cid) return !Steamed(from, c.At) && S.Sight3(from, FloorAt(from) + eye, c.At, LensZ(c), false, true); } // brief 039: the steam too
            return Sees(from, at);
        }
        bool CrowdId(Pt from, string id, Pt at)
        {
            if (R.MountOn && id.StartsWith("camera:", StringComparison.Ordinal)) { var cid = id.Substring(7); foreach (var c in S.Cameras) if (c.Id == cid) return CrowdBlocksZ(from, FloorAt(from) + eye, c.At, LensZ(c)); }
            return CrowdBlocks(from, at);
        }
        bool CamSees(Camera c, Pt p) => R.MountOn ? SeesFrom(c.At, LensZ(c), p, true) : Sees(c.At, p);
        bool CamCrowd(Camera c, Pt p) => R.MountOn ? CrowdBlocksZ(c.At, LensZ(c), p, FloorAt(p) + eye) : CrowdBlocks(c.At, p);
        /// <summary>Brief 055 §7: a machine's own eye (the patrol robot's face, the response robots' lamp) above its floor, and its crowd test.</summary>
        bool EyeSees(Pt from, int mount, Pt to) => R.MountOn ? SeesFrom(from, FloorAt(from) + mount, to) : Sees(from, to);
        bool EyeCrowd(Pt from, int mount, Pt to) => R.MountOn ? CrowdBlocksZ(from, FloorAt(from) + mount, to, FloorAt(to) + eye) : CrowdBlocks(from, to);
        public bool CameraWatching(string id, Pt p, bool crowd = true)
        {
            if (CameraDark(id)) return false; // tuning.md §9.2 gap 2: shot out, it watches nothing
            if (id == LoopCam && LoopOn) return false;
            if (id == "pickup" && !PickupCameraLive()) return false;
            foreach (var c in S.Cameras) if (c.Id == id) return InHeads(c, p) && CamSees(c, p) && !(crowd && CamCrowd(c, p));
            throw new FormatException($"site has no camera '{id}'");
        }
        /// <summary>In any head's cone of the camera (a mast carries several).</summary>
        static bool InHeads(Camera c, Pt p) { foreach (var f in c.Facings) if (G.InCone(c.At, f, c.Reach, c.Half, 0, p)) return true; return false; }
        public bool PickupCameraLive() => Tick < BlockedFrom || Tick >= BlockedUntil; // brief 059: the driver's camera block is retired (BlockedFrom stays INF)
        /// <summary>The guard's cone; crowd: people on the street block it (Pulse §4: watchers), false for his own aim (§1.6).</summary>
        public bool GuardWatching(Pt p, bool crowd = true)
        {
            var g = GuardPose(); if (!g.HasValue) return false;
            var (pos, f, phase) = g.Value;
            if (phase == "door") return G.InCone(pos, f, R.DoorReachCm, R.DoorHalfCm, 0, p) && Sees(pos, p) && !(crowd && CrowdBlocks(pos, p));
            return G.InCone(pos, f, R.FacingReachCm, R.FacingHalfCm, near, p) && Sees(pos, p) && !(crowd && CrowdBlocks(pos, p));
        }
        public bool PatrolWatching(Pt p, bool crowd = true)
        {
            if (PatrolDown || PatrolAwayUntil > Tick) return false;
            if (PatrolUnit() is Unit pu) return UnitSees(pu, p, crowd); // brief 013: on an alarm it looks where it walks and holds
            var (pos, f) = PatrolPose()!.Value;
            if (PatrolChasing) return G.InCone(pos, f, R.FacingReachCm, R.FacingHalfCm, 0, p) && EyeSees(pos, R.MountPatrolCm, p) && !(crowd && EyeCrowd(pos, R.MountPatrolCm, p)); // brief 031 L2: a machine sees by its cone only
            return InSweepCone(pos, f, p) && EyeSees(pos, R.MountPatrolCm, p) && !(crowd && EyeCrowd(pos, R.MountPatrolCm, p));
        }
        bool InSweepCone(Pt pos, Pt f, Pt p)
        {
            long ff = (long)f.X * f.X + (long)f.Y * f.Y, fl = G.Isqrt(ff); if (fl == 0) return false;
            long rx = p.X - pos.X, ry = p.Y - pos.Y;
            long depth = (rx * f.X + ry * f.Y) / fl, side = Math.Abs(rx * f.Y - ry * f.X) / fl;
            if (depth < 0 || depth > R.PatrolDepthCm) return false;
            return side <= R.PatrolHalfBase + depth * R.PatrolHalfPerMille / 1000;
        }
        /// <summary>The top-down test's botSeesEntranceOnPatrol: would the sweep, not rerouted, see the public door now?</summary>
        public bool SweepSeesEntrance(int t)
        {
            var pos = SweepPos(t); var door = S.At(S.GuardPost);
            return InSweepCone(pos, S.PatrolFacing, door) && EyeSees(pos, R.MountPatrolCm, door);
        }
        int sweepCovers = -1;
        /// <summary>Engine 0.34.0 (brief 067 A: the sweep left the forecourt for the service yard): does the sweep see the public door at any tick
        /// of its round? False on today's site, so no route is "timed between sweeps" at the door. Derived from the inputs once; never hashed.</summary>
        public bool SweepCoversEntrance
        {
            get
            {
                if (sweepCovers < 0) { sweepCovers = 0; for (int t = 0, n = T(R.PatrolPeriodS); t < n; t++) if (SweepSeesEntrance(t)) { sweepCovers = 1; break; } }
                return sweepCovers == 1;
            }
        }
        /// <summary>Engine 0.34.0 (Motive briefs/067/motive-square.md §5.3, for Waymark's MC-3 `needs: "patrol-watches-entry"`): the site fact
        /// patrolWatchesEntry: the sweep, not rerouted, sees the public door (S.GuardPost) by its cone at its eye, the crowd aside, at one whole
        /// second of its round at least (SweepSeesEntrance at each second of one period). A rerouted patrol watches nothing (PatrolWatching), so
        /// its wait adds nothing. False on the service yard's sweep (brief 067 A), true on 0.33.1's forecourt. Derived from the inputs alone;
        /// never hashed. Read-only (suborned-sim facts).</summary>
        public bool PatrolWatchesEntry => SweepSecondsOnEntry > 0;
        /// <summary>The whole seconds of the sweep's round (rulebook patrol.period) at which it sees the public door: PatrolWatchesEntry's count.</summary>
        public int SweepSecondsOnEntry { get { int n = 0; for (int s = 0; s < R.PatrolPeriodS; s++) if (SweepSeesEntrance(T(s))) n++; return n; } }
        public bool SecurityWatching(Pt p)
        {
            for (int k = 0; k < SecGuns.Length; k++) if (SecOfficerWatching(k, p)) return true;
            return false;
        }
        public bool SecOfficerWatching(int k, Pt p, bool crowd = true)
        {
            var s = SecOfficerPose(k); if (!s.HasValue) return false;
            return G.InCone(s.Value.pos, s.Value.facing, R.SecReachCm, R.SecHalfCm, near, p) && Sees(s.Value.pos, p) && !(crowd && CrowdBlocks(s.Value.pos, p));
        }
        /// <summary>Any member of the drawn crew sees p (brief 059: each member's own cone).</summary>
        public bool RivalsWatching(Pt p) => RivalsDrawn && CrewSees(p);

        // ---------------------------------------------------------------- security, rivals, patrol answering gunfire

        /// <summary>A report of gunfire (or a downed guard's missed mark) calls the response unit: it arrives by the reporter's delay
        /// (level 1's arrivals, shifted by the level's dispatch), the district seals the level's seal after it arrives, and the level's
        /// robots, reinforcements and cordon follow (brief 013, the dial). landing: the report lands now (the dispatch alone).</summary>
        void CallSecurity(int delayS, string text, Pt? scene = null, bool landing = false, Func<CauseOrigin?>? origin = null)
        {
            int arrival = landing ? Tick + T(DialNow.DispatchS) : GunfireArrival(delayS);
            if (arrival >= SecurityArrives) return;
            bool first = SecurityArrives == INF;
            int lockBefore = Lockdown;
            SecurityArrives = arrival; if (!gangOn) Lockdown = arrival + T(SealS); SealMoved(lockBefore); // brief 096: a gang shooting's call sets no seal (GameGangShooting.cs)
            if (origin != null) NoteSealOrigin(lockBefore, origin); // what the seal clock began from (FailCause.cs, output only)
            Note("security.called", $"{text} Security is coming from the north street.");
            if (R.SealWarns && Lockdown != lockBefore) // engine 0.30.0 (Suture's bot pass: the district sealed with no warning): every path that sets the seal clock says so
            {
                var so = SealOrigin; string why = so == null ? "" : $", from {so.Report}" + (so.Reporter != null ? $" (reported by {so.Reporter.Id})" : "");
                Note("seal.starts", $"The response is called: the district seals {SealS} s after the unit is on the street ({Sim.Report.Clock(arrival)}), at {Sim.Report.Clock(Lockdown)}{why}.");
            }
            if (first) GunfireAlarm(arrival, scene ?? S.Stand(S.SecTo));
        }
        /// <summary>Gunfire draws the crew (brief 059 F5; security.rivals.drawn): on the map, at once, on foot to the Office front and the car to
        /// rivals.car.front; off it, the car enters within rivals.drawn s and rides there; away on a tip, it comes back to the front when the tip ends.</summary>
        void DrawRivals()
        {
            if (RivalsDrawn || Range || rivalsAbsorbed) return;
            RivalsDrawn = true;
            if (CarSt == CrewCar.Off)
            {
                RivalAt = Math.Min(RivalAt, Tick + T(R.RivalsDrawnS)); crewDrawnEntry = Math.Max(Tick + 1, Math.Min(CrewEntry, Tick + T(R.RivalsDrawnS)));
                RivalsDrawnTick = crewDrawnEntry;
            }
            else { RivalsDrawnTick = Tick; CrewDrawn(); }
            Note("rivals.drawn", "The rivals hear gunfire and head for the Office.");
        }
        void WakePatrol(Pt at, OpId who)
        {
            if (PatrolDown || PatrolAwayUntil > Tick || PatrolChasing) return;
            var pos = PatrolPose()!.Value.pos;
            if (!G.Within(pos, at, R.PatrolBreakCm)) return;
            chasePos = pos; PatrolChasing = true; chaseOp = who; patrolOrigin = at; patrolReturning = false; PatrolGun.NoFoeSince = -1;
            Note("patrol.breaks", "The patrol breaks its route and turns toward the gunfire.");
        }

        /// <summary>A civilian who sees violence (§05: 3 at once; rulebook witness.sees, traceable violence): one incident, once.</summary>
        void ViolenceSeen(string incident, Person witness, string by)
        {
            RaiseAlert(R.ViolenceSeen, $"violence-seen:{incident}:{(witness.Role.Length > 0 ? witness.Role : witness.Id)}");
            Remember(witness, "saw-violence", by, true);
            if (witness.Turned) LadderMove(witness, "saw-violence");
            if (!incidents.Add("seen:" + incident)) return;
            AddTrail(R.WitnessTrail, $"witnessed:{incident}");
            // Standing: a civilian shot at is violence (seen-violence); anyone else shot at (the guard, the rivals, officers) is the fight's
            // to judge, once, when it ends (fight.public, Sky 2026-10-16): the incident's gunfire is traceable gunfire, Standing none
            if (CivilianOf(incident) != null) MoveStanding(-R.StandingStep, $"violence-seen:{incident}", "seen-violence", incident);
        }

        /// <summary>Every change to Trail, Alert and standing with its cause, in order: the answer to "why is the investigation active?" (§13 T3).</summary>
        public readonly List<(int tick, string what, int delta, int now, string cause)> Ledger = new List<(int, string, int, int, string)>();
        void AddTrail(int n, string cause) { if (n == 0) return; Trail += n; Ledger.Add((Tick, "trail", n, Trail, cause)); }
        void RaiseAlert(int to, string cause) { if (to >= 3) GangIncident("alert-3"); if (to <= Alert) return; int d = to - Alert; bool two = Alert < 2 && to >= 2; Alert = to; Ledger.Add((Tick, "alert", d, Alert, cause)); if (two) AlertTwo(cause); if (Alert >= 2) RoundsEnd("alert-2"); }
        /// <summary>Move the district's Standing: `cause` for the ledger, `why` a rulebook standing.causes id and `who` the person it names, for the
        /// debrief's Standing line (brief 014). StandingMoves is output only, never hashed: Standing itself is.</summary>
        void MoveStanding(int d, string cause, string why, string who)
        {
            if (d == 0) return;
            if (!R.StandingCauses.Contains(why)) throw new InvalidOperationException($"standing cause '{why}' is not in the rulebook (unknown = reject)");
            Standing += d; Ledger.Add((Tick, "standing", d, Standing, cause)); StandingMoves.Add((Tick, d, why, who));
        }
        /// <summary>Every move of Standing in the mission, in order, with its rulebook cause and the person it names (brief 014).</summary>
        public readonly List<(int tick, int delta, string cause, string who)> StandingMoves = new List<(int, int, string, string)>();

        public void Finish()
        {
            // a fight still open when the run ends ends with it (fight.public): its move replaces a checkpoint already taken at this tick
            if (EndOpenFight() | fightClosedLate) { fightClosedLate = false; if (Checkpoints.Count > 0 && Checkpoints[Checkpoints.Count - 1].tick == Tick) Checkpoints.RemoveAt(Checkpoints.Count - 1); }
            if (Checkpoints.Count == 0 || Checkpoints[Checkpoints.Count - 1].tick != Tick) Checkpoints.Add((Tick, Hash.State(this)));
        }

        /// <summary>Engine 0.25.0 (brief 045 F3, Redline): the closing checkpoint Finish would take now, without changing the live game: the
        /// hash Engine.Play(start, orders, Tick) ends with, so a host's save needs no replay. A fight still open is closed (EndFight: Standing,
        /// the ledger, the log, the fight's record), hashed, and every piece of it put back as it was; with no fight to close it is the
        /// checkpoint already taken at this tick, else the state's hash. Costs one Hash.State.</summary>
        public ulong ClosingHash()
        {
            var f = OpenFight;
            if (f == null && !fightClosedLate) return Checkpoints.Count > 0 && Checkpoints[Checkpoints.Count - 1].tick == Tick ? Checkpoints[Checkpoints.Count - 1].hash : Hash.State(this);
            if (f == null) return Hash.State(this); // closed late (Go, the investigation's end): Finish re-takes this tick's checkpoint
            int end = f.End, delta = f.Delta, holster = autoHolsterAt, fights = Fights.Count, ledger = Ledger.Count, moves = StandingMoves.Count, log = Log.Count, standing = Standing;
            string outcome = f.Outcome, cause = f.Cause; bool judged = finalFightJudged;
            try { EndFight(); return Hash.State(this); }
            finally
            {
                OpenFight = f; f.End = end; f.Delta = delta; f.Outcome = outcome; f.Cause = cause; autoHolsterAt = holster; finalFightJudged = judged; Standing = standing;
                Fights.RemoveRange(fights, Fights.Count - fights); Ledger.RemoveRange(ledger, Ledger.Count - ledger); StandingMoves.RemoveRange(moves, StandingMoves.Count - moves); Log.RemoveRange(log, Log.Count - log);
            }
        }
    }
}
