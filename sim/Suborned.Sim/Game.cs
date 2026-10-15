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
    public enum ValeAt { Office, Leaving, Pickup, Taking, WithEnforcer, Car, Dead }

    /// <summary>The start state's choices beyond the site: what a scenario fixes before tick 0 (hashed into the start state).</summary>
    public sealed class Start
    {
        public int? RivalsAtS;                                            // §07 3:10 unless a scenario moves it
        public readonly List<string> Turned = new List<string>();         // roles turned before the mission (a scenario fixture)
        public readonly Dictionary<string, List<string>> Known = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        public readonly Dictionary<string, string> Temperament = new Dictionary<string, string>(StringComparer.Ordinal);
        public long? Cash;
        public bool FinalFight = true;  // §07: the final fight is always fought; the top-down test's 28 had none
        public bool Crowd = true;       // §05 witnesses among the city's people; Suture's T4 arm switches it off
        public int? Arms;               // the mission's Arms (site arms) unless a scenario fixes it: one sidearm per weapons.sidearmArms
        public static Start Parse(JNode? n)
        {
            var s = new Start(); if (n == null) return s;
            n.Only("rivalsAt", "turned", "known", "temperament", "cash", "finalFight", "crowd", "arms");
            if (n.Opt("arms") is JNode ar) { s.Arms = ar.Int(); if (s.Arms < 0 || s.Arms > 100) throw new FormatException($"{ar.Path}: arms 0..100"); }
            if (n.Opt("finalFight") is JNode ff) s.FinalFight = ff.Bool();
            if (n.Opt("crowd") is JNode cr) s.Crowd = cr.Bool();
            if (n.Opt("rivalsAt") is JNode r) s.RivalsAtS = r.Int();
            if (n.Opt("turned") is JNode t) s.Turned.AddRange(t.Strs());
            if (n.Opt("known") is JNode k) foreach (var kv in k.Entries) s.Known[kv.Key] = kv.Value.Strs();
            if (n.Opt("temperament") is JNode tm) foreach (var kv in tm.Entries) s.Temperament[kv.Key] = kv.Value.Str();
            if (n.Opt("cash") is JNode c) s.Cash = c.Long();
            return s;
        }
        public string Canonical()
        {
            var w = new JWriter().Obj();
            if (RivalsAtS.HasValue) w.Prop("rivalsAt", RivalsAtS.Value);
            if (Turned.Count > 0) { w.OpenArr("turned"); foreach (var x in Turned) w.Str(x); w.EndArr(); }
            if (Known.Count > 0) { w.Open("known"); var ks = new List<string>(Known.Keys); ks.Sort(StringComparer.Ordinal); foreach (var k in ks) { w.OpenArr(k); foreach (var x in Known[k]) w.Str(x); w.EndArr(); } w.EndObj(); }
            if (Temperament.Count > 0) { w.Open("temperament"); var ks = new List<string>(Temperament.Keys); ks.Sort(StringComparer.Ordinal); foreach (var k in ks) w.Prop(k, Temperament[k]); w.EndObj(); }
            if (Cash.HasValue) w.Prop("cash", Cash.Value);
            if (!FinalFight) w.Prop("finalFight", false);
            if (!Crowd) w.Prop("crowd", false);
            if (Arms.HasValue) w.Prop("arms", Arms.Value);
            return w.EndObj().ToString();
        }
    }

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
        public Outcome Result; public string FailCode = "", FailText = "";
        public readonly List<LogLine> Log = new List<LogLine>();
        public readonly List<(Order order, bool ok, string why)> Orders = new List<(Order, bool, string)>();
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
        // the guard (§05 guard on door routine; mission.json windows.front-door)
        public int LureTick = -1, GuardAwayUntil, MissingNotice = INF;
        public int GuardHealth => GuardGun.Health;
        public bool GuardHeld, GuardEngaged, GuardReachedCafe, GuardDown;
        Pt heldPos, heldFacing; string heldPhase = "";
        readonly Path guardOut, guardBack;
        // the official car and the driver
        public int BlockMove = INF, BlockedFrom = INF, BlockedUntil;
        Pt? carStoppedAt; Path? carToService;
        readonly Path blockPath;
        // Vale
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
        Path? rivalsPath, rivalsRestartPath;
        // the response unit (§05 response unit on report; district seals after it arrives)
        public int SecurityArrives = INF, Lockdown = INF; public bool SecurityDown, SecurityTracking;
        public int SecurityHealth { get { int n = 0; foreach (var g in SecGuns) n += g.Health; return n; } }
        readonly Path securityPath;
        // the debrief's facts
        public bool InezWitnessedShooting; public string InezMethod = "";
        public string RouteKind = "", RouteGuard = "", RouteBot = "", RouteRival = "";

        // numbers read once from the rules and the site
        readonly int eye, near, actReach, approachReach, listenReach, witnessRange;
        readonly int opSpeed, secSpeed, rivSpeed, chaseSpeed;
        readonly int walkT, stayT, rivalsArriveS, driverLastS, valeExitsS, valeAtCarS, overS, blockMoveT, blockHoldT;

        public Game(Rules rules, Site site, Start? start = null, string? startText = null, Crowd? crowd = null)
        {
            R = rules; S = site; Start = start ?? new Start(); People2 = crowd;
            if (crowd != null) foreach (var w in crowd.Walkers) Walkers.Add(w.Fresh());
            StartHash = Hash.Of(startText ?? "") ^ Hash.Of(Start.Canonical());
            eye = R.Eye; near = R.NearCm;
            actReach = R.ActReachCm; approachReach = R.ApproachReachCm; listenReach = R.ListenReachCm;
            witnessRange = R.WitnessRangeCm;
            opSpeed = R.CellSpeed; secSpeed = R.SecuritySpeed; rivSpeed = R.RivalsSpeed; chaseSpeed = R.PatrolChase;
            walkT = T(S.GuardWalkS); stayT = T(S.GuardStayS);
            rivalsArriveS = S.RivalsArrive; driverLastS = S.DriverLast; valeExitsS = S.ValeExits; valeAtCarS = S.ValeAtCar; overS = S.Over;
            blockMoveT = T(S.BlockMoveS); blockHoldT = T(S.BlockHoldS);

            Cash = Start.Cash ?? S.Purse;
            Standing = R.StandingStart;
            string[] crew = { "crew:scout", "crew:fixer", "crew:tech", "crew:enforcer" };
            // Arms (Sky 2026-10-15 Q1): the Enforcer's carbine is the cell's; each sidearm takes weapons.sidearmArms, issued Scout, Fixer, Tech
            int sidearms = (Start.Arms ?? S.Arms) / R.SidearmArms;
            for (int i = 0; i < 4; i++)
            {
                var id = (OpId)i; bool enf = id == OpId.Enforcer;
                var gun = new Gun(Sim.Op.Name(id)) { W = enf ? R.Carbine : i < sidearms ? R.Pistol : null, Trained = enf, Health = enf ? R.HealthEnforcer : R.HealthOp };
                gun.Max = gun.Health;
                Ops[i] = new Op { Id = id, Pos = S.Stand(crew[i]), Gun = gun };
            }
            GuardGun = NewGun("guard", R.Pistol, true, R.HealthGuard);
            PatrolGun = NewGun("patrol", R.Pistol, true, R.HealthPatrol);
            RivalsGun = NewGun("rivals", R.Pistol, false, R.HealthRival);
            SecGuns = new Gun[R.ResponseOfficers];
            for (int k = 0; k < SecGuns.Length; k++) SecGuns[k] = NewGun($"security-{k + 1}", R.Pistol, true, R.HealthOfficer);

            string[] places = { S.GuardCafe, S.CarDriver, S.GuardPost, S.ValeExit };
            string[] roles = { "waitress", "driver", "guard", "councillor" };
            for (int i = 0; i < 4; i++)
            {
                var c = S.Role(roles[i]);
                var p = new Person { Role = roles[i], Id = c.Id, Name = c.Name, Temperament = Start.Temperament.TryGetValue(roles[i], out var tm) ? tm : c.Temperament, Need = c.Need, Place = places[i] };
                var t = R.Temp(p.Temperament); R.CheckNeed(p.Need);
                for (int k = 0; k < 4; k++) p.Sc[k] = t.Start[k];
                People.Add(p);
                civGuns[roles[i]] = NewGun(roles[i], null, false, R.HealthCivilian);
            }
            Inez.Suspected = Inez.Need; Driver.Suspected = Driver.Need; // Sky 2026-10-15: "in debt" shows as suspected from 0:00, from the briefing
            foreach (var role in Start.Turned) PersonByRole(role).Turned = true;
            foreach (var kv in Start.Known) foreach (var w in kv.Value) PersonByRole(kv.Key).Learn(w);
            RivalAt = T(Start.RivalsAtS ?? rivalsArriveS);

            var post = S.Stand(S.GuardPost); var cafe = S.Stand(S.GuardCafe);
            guardOut = new Path(S.Grid.Route(post, cafe) ?? throw new FormatException("no walk from the guard's post to the café"));
            guardBack = new Path(S.Grid.Route(cafe, post) ?? throw new FormatException("no walk from the café to the guard's post"));
            blockPath = new Path(new[] { S.At(S.CarPickup), S.At(S.CarBlock) });
            var sec1 = S.Grid.Route(S.Stand(S.SecEntry), S.Stand(S.SecVia)) ?? throw new FormatException("no walk for the response unit");
            var sec2 = S.Grid.Route(S.Stand(S.SecVia), S.Stand(S.SecTo)) ?? throw new FormatException("no walk for the response unit");
            var sp = new List<Pt>(sec1); for (int i = 1; i < sec2.Length; i++) sp.Add(sec2[i]);
            securityPath = new Path(sp.ToArray());
            Note("start", "Vale is inside. His official car will collect him later. The city is already moving.");
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
        void Fail(string code, string text) { if (Result != Outcome.None) return; Result = Outcome.Failure; FailCode = code; FailText = text; Note("fail." + code, text); }
        public bool Over => Result != Outcome.None;

        // ---------------------------------------------------------------- the tick

        /// <summary>One tick. It never throws: rules and site are checked at load (Redline E1); if the engine itself fails, the run ends
        /// as a failure with code engine-error and the reason in the log, deterministically, instead of taking the host (Unity) down.</summary>
        public void Step()
        {
            if (Over) return;
            try { StepInner(); }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                Result = Outcome.Failure; FailCode = "engine-error"; FailText = e.GetType().Name + ": " + e.Message;
                Note("fail.engine-error", FailText);
                Checkpoints.Add((Tick, Hash.State(this)));
            }
        }

        void StepInner()
        {
            Tick++;
            GuardTimeline(); CarTimeline(); RivalsTimeline(); ValeTimeline();
            foreach (var op in Ops)
            {
                if (op.Busy != null && op.BusyEnd <= Tick) { var o = op.Busy; op.Busy = null; Land(op, o); if (Over) break; }
                if (op.Walk != null)
                {
                    op.WalkMm += op.WalkSpeed > 0 ? op.WalkSpeed : opSpeed; op.Pos = op.Walk.At(op.WalkMm);
                    if (op.Running) RunAttention(op);
                    if (op.WalkMm >= op.Walk.Length) { op.Walk = null; op.WalkMm = 0; op.Running = false; }
                }
            }
            foreach (var op in Ops) if (op.CarriedBy.HasValue) op.Pos = OpOf(op.CarriedBy.Value).Pos; // a carried operative goes where the carrier goes
            if (!Over) Tether();
            if (!Over) Watchers();
            if (!Over) PatrolChase();
            if (!Over && Tick == MissingNotice)
            {
                Note("guard.check-in", "The guard misses his check-in. Security now knows the post is empty.");
                CallSecurity(R.RespCheckInS, "The empty guard post is reported.");
            }
            if (!Over) DrawnSeen();
            if (!Over) FightStep();
            if (!Over) Combat();
            if (!Over && Tick >= Lockdown && Vale != ValeAt.Car) Fail("sealed", "Security seals the district before Vale reaches the getaway car.");
            if (!Over && Tick == T(overS) && Vale == ValeAt.WithEnforcer)
                Note("horizon.open", Sim.Report.Clock(Tick) + " (the mission's close) with Vale in the Enforcer's hands: whether the mission closes here is a ruling for Waymark and Motive (Redline E8); the run goes on.");
            if (!Over && Tick >= T(overS) && Vale != ValeAt.WithEnforcer && Vale != ValeAt.Car)
            {
                if (RivalsPresent) Fail("rivals-reach-vale", "The rival crew reaches Vale first.");
                else Fail("vale-leaves", "Vale leaves the district in the official car.");
            }
            if (Tick % 100 == 0 || Over) Checkpoints.Add((Tick, Hash.State(this)));
        }

        public void RunTo(int tick) { while (Tick < tick && !Over) Step(); }
        public void Advance(int ticks) => RunTo(Tick + ticks);

        void GuardTimeline()
        {
            if (LureTick < 0 || GuardHeld || GuardDown) return;
            if (Tick == LureTick + walkT) { GuardReachedCafe = true; Note("guard.at-cafe", "The guard reaches Inez at the café. She spills coffee and keeps him away from the door."); }
            if (Tick == LureTick + walkT + stayT) Note("guard.leaves-cafe", "The guard leaves the café and walks back toward his post.");
            if (Tick == GuardAwayUntil) Note("guard.back", "The guard returns to the office entrance.");
        }
        void CarTimeline()
        {
            if (Tick == BlockedFrom) Note("car.blocks", "The car now blocks the pickup camera.");
            if (Tick == BlockedUntil && Tick < T(driverLastS)) Note("car.clears", "The car clears the pickup camera. Its feed is live again.");
            if (Tick == T(driverLastS) && Driver.Turned && !Driver.Down)
            {
                var from = ParkedCar(Tick);
                carToService = new Path(S.Grid.Route(S.Grid.NearestFree(from) ?? from, S.Stand(S.CarService)) ?? new[] { from, S.Stand(S.CarService) });
                Note("car.leaves", "The official car leaves the bay for the service lane.");
            }
        }
        void RivalsTimeline()
        {
            if (Tick != RivalAt) return;
            RivalsArrived = true;
            Note("rivals.arrive", RivalsDrawn ? "The rival crew comes in from the east street, following the gunfire." : "A rival car parks near Vale's pickup. Its crew starts watching the building.");
        }
        void ValeTimeline()
        {
            if (Tick == T(valeExitsS) && Vale == ValeAt.Office && !EntryInProgress && !Driver.Down)
            {
                Vale = ValeAt.Leaving; PickupService = Driver.Turned;
                var to = PickupService ? S.Stand(S.ValeService) : S.Stand(S.ValeCar);
                valePath = new Path(S.Grid.Route(S.Stand(S.ValeExit), to) ?? new[] { S.Stand(S.ValeExit), to });
                Note("vale.exits", PickupService ? "Vale comes out and walks round to the service lane, where his car is pulling in." : "Vale steps out of the official entrance and walks to his car.");
            }
            if (Tick == T(valeAtCarS) && Vale == ValeAt.Leaving)
            {
                Vale = ValeAt.Pickup;
                Note("vale.at-car", PickupService ? "Vale reaches the car in the service lane." : "Vale reaches the official car under the pickup camera.");
            }
        }

        void Tether()
        {
            var tech = OpOf(OpId.Tech);
            if (LoopOn && (tech.Down || !G.Within(tech.Pos, S.At("cabinet"), R.TetherCm)))
            {
                LoopOn = false; Note("loop.ends", tech.Down ? "The Tech is down. The camera feed returns." : "The Tech leaves the cabinet. The camera feed returns.");
            }
        }

        void PatrolChase()
        {
            if (!PatrolChasing || PatrolDown || PatrolAwayUntil > Tick || Tick % Hz != 0) return;
            if (PatrolGun.Target.Length > 0 && !patrolReturning) return; // engaging: it stops (Pulse §3.6)
            var target = patrolReturning ? SweepPos(Tick) : patrolOrigin; // to the shot's origin; resuming, back to its sweep
            if (patrolReturning && G.Within(chasePos, target, 100)) { PatrolChasing = false; patrolReturning = false; PatrolGun.NoFoeSince = -1; Note("patrol.sweep", "The patrol is back on its sweep."); return; }
            var t = S.Grid.NearestFree(target); var from = S.Grid.NearestFree(chasePos);
            if (!t.HasValue || !from.HasValue) return;
            var route = S.Grid.Route(from.Value, t.Value); if (route == null) return;
            chasePos = new Path(route).At(chaseSpeed * Hz); // a second's chase, as the top-down test moved it
        }

        // ---------------------------------------------------------------- where things are

        public (Pt pos, Pt facing, string phase)? GuardPose()
        {
            if (GuardDown) return null;
            if (GuardHeld) return (heldPos, heldFacing, heldPhase);
            var post = S.Stand(S.GuardPost);
            if (LureTick < 0 || Tick >= GuardAwayUntil) return (post, S.GuardFacing, "door");
            int age = Tick - LureTick;
            if (age < walkT) { long mm = guardOut.Length * age / walkT; return (guardOut.At(mm), guardOut.Heading(mm), "walking to café"); }
            if (age < walkT + stayT) return (guardOut.End, S.CafeFacing, "at café");
            long back = guardBack.Length * (age - walkT - stayT) / walkT;
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
            if (!Driver.Turned || Tick < T(driverLastS) || carToService == null) return ParkedCar(Tick);
            if (Tick >= T(valeAtCarS)) return carToService.End;
            return carToService.At(carToService.Length * (Tick - T(driverLastS)) / (T(valeAtCarS) - T(driverLastS)));
        }
        public Pt? DriverPos()
        {
            if (Driver.Down || Vale == ValeAt.Car) return null;
            var car = CarPos();
            return car == blockPath.Points[0] ? S.Stand(S.CarDriver) : car;
        }
        public Pt? ValePos()
        {
            switch (Vale)
            {
                case ValeAt.Leaving: { int span = T(valeAtCarS) - T(valeExitsS); return valePath!.At(valePath.Length * Math.Min(span, Tick - T(valeExitsS)) / span); }
                case ValeAt.Pickup: case ValeAt.Taking: return PickupService ? S.Stand(S.ValeService) : CarPos();
                case ValeAt.WithEnforcer: return Enforcer.Inside ? (Pt?)null : Enforcer.Pos + new Pt(100, 0);
                default: return null;
            }
        }
        public Pt? PersonPos(Person p)
        {
            if (p.Down) return null;
            if (p == Inez) return S.Stand(Inez.Place);
            if (p == Driver) return DriverPos();
            if (p == GuardP) return GuardPose()?.pos;
            return ValePos();
        }

        public (Pt pos, Pt facing)? PatrolPose()
        {
            if (PatrolDown) return (patrolDownAt, new Pt(0, 0));
            if (PatrolAwayUntil > Tick) return (S.Stand(S.PatrolReroute), S.PatrolRerouteFacing);
            if (PatrolChasing) return (chasePos, Enforcer.Pos - chasePos);
            return (SweepPos(Tick), S.PatrolFacing);
        }
        Pt SweepPos(int t)
        {
            int period = T(R.PatrolPeriodS), half = period / 2, ph = t % period;
            return G.Lerp(S.Stand(S.PatrolA), S.Stand(S.PatrolB), ph <= half ? ph : period - ph, half);
        }

        public bool RivalsPresent => RivalsArrived && RivalsDelayedUntil <= Tick && !rivalsAbsorbed;
        public (Pt pos, Pt facing)? RivalsPose()
        {
            if (!RivalsPresent) return null;
            if (!RivalsDrawn)
            {
                bool atService = Tick >= T(valeAtCarS) && PickupService;
                return (S.Stand(atService ? S.ValeService : S.RivalsPark), new Pt(0, 100));
            }
            bool restarting = RivalsDelayedUntil > RivalsDrawnTick;
            var path = restarting ? (rivalsRestartPath ??= new Path(S.Grid.Route(S.Stand(S.RivalsEntry), S.Stand(S.RivalsFront))!)) : rivalsPath!;
            int start = restarting ? RivalsDelayedUntil : RivalsDrawnTick;
            long mm = (long)Math.Max(0, Tick - start) * rivSpeed;
            return mm >= path.Length ? (path.End, Back(path)) : (path.At(mm), path.Heading(mm));
        }

        /// <summary>The response unit: its lead officer still up, and where the unit faces (its officers walk in file, `SecFileMm` apart).</summary>
        public (Pt pos, Pt facing)? SecurityPose()
        {
            if (SecurityDown || Tick < SecurityArrives) return null;
            for (int k = 0; k < SecGuns.Length; k++) if (!SecGuns[k].Down) return SecOfficerPose(k);
            return null;
        }
        const long SecFileMm = 1500; // the officers' spacing in file: placement only (no rule reads it), until Pulse's hostile spec places them
        public (Pt pos, Pt facing)? SecOfficerPose(int k)
        {
            if (Tick < SecurityArrives || SecGuns[k].Down) return null;
            if (secJoined[k]) { var h = HostileOf(SecGuns[k].Id)!; var p = h.Pos(Tick); return (p, Car - p == new Pt(0, 0) ? new Pt(100, 0) : Car - p); } // joined the loud fight: faces the car
            long mm = (long)(Tick - SecurityArrives) * secSpeed - k * SecFileMm, end = Math.Max(0, securityPath.Length - k * SecFileMm);
            if (mm < 0) mm = 0;
            if (mm >= end) return (securityPath.At(end), Back(securityPath));
            return (securityPath.At(mm), securityPath.Heading(mm));
        }

        /// <summary>At the end of its run a unit holds facing back the way it came, as the top-down test's rivals and response unit did.</summary>
        static Pt Back(Path p) { var h = p.Heading(p.Length); return new Pt(-h.X, -h.Y); }

        // ---------------------------------------------------------------- who sees what (rulebook perception)

        bool Sees(Pt from, Pt to) => S.Sight(from, to, eye);
        public bool CameraWatching(string id, Pt p)
        {
            if (id == "entrance" && LoopOn) return false;
            if (id == "pickup" && !PickupCameraLive()) return false;
            foreach (var c in S.Cameras) if (c.Id == id) return G.InCone(c.At, c.Facing, c.Reach, c.Half, 0, p) && Sees(c.At, p);
            throw new FormatException($"site has no camera '{id}'");
        }
        public bool PickupCameraLive() => (Driver.Turned && Tick >= T(driverLastS)) || Tick < BlockedFrom || Tick >= BlockedUntil;
        public bool GuardWatching(Pt p)
        {
            var g = GuardPose(); if (!g.HasValue) return false;
            var (pos, f, phase) = g.Value;
            if (phase == "door") return G.InCone(pos, f, R.DoorReachCm, R.DoorHalfCm, 0, p) && Sees(pos, p);
            return G.InCone(pos, f, R.FacingReachCm, R.FacingHalfCm, near, p) && Sees(pos, p);
        }
        public bool PatrolWatching(Pt p)
        {
            if (PatrolDown || PatrolAwayUntil > Tick) return false;
            var (pos, f) = PatrolPose()!.Value;
            if (PatrolChasing) return G.InCone(pos, f, R.FacingReachCm, R.FacingHalfCm, near, p) && Sees(pos, p);
            return InSweepCone(pos, f, p) && Sees(pos, p);
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
            return InSweepCone(pos, S.PatrolFacing, door) && Sees(pos, door);
        }
        public bool SecurityWatching(Pt p)
        {
            for (int k = 0; k < SecGuns.Length; k++) if (SecOfficerWatching(k, p)) return true;
            return false;
        }
        bool SecOfficerWatching(int k, Pt p)
        {
            var s = SecOfficerPose(k); if (!s.HasValue) return false;
            return G.InCone(s.Value.pos, s.Value.facing, R.SecReachCm, R.SecHalfCm, near, p) && Sees(s.Value.pos, p);
        }
        public bool RivalsWatching(Pt p)
        {
            if (!RivalsDrawn) return false;
            var r = RivalsPose(); if (!r.HasValue) return false;
            return G.InCone(r.Value.pos, r.Value.facing, R.RivReachCm, R.RivHalfCm, near, p) && Sees(r.Value.pos, p);
        }

        // ---------------------------------------------------------------- security, rivals, patrol answering gunfire

        void CallSecurity(int delayS, string text)
        {
            int arrival = Tick + T(delayS);
            if (arrival >= SecurityArrives) return;
            SecurityArrives = arrival; Lockdown = arrival + T(R.SealS);
            Note("security.called", $"{text} Security is coming from the north street.");
        }
        void DrawRivals()
        {
            if (RivalsDrawn) return;
            var start = RivalsArrived && RivalsPose() is var rp && rp.HasValue ? rp.Value.pos : S.Stand(S.RivalsEntry);
            RivalsDrawn = true;
            if (!RivalsArrived) RivalAt = Math.Min(RivalAt, Tick + T(R.RivalsDrawnS));
            RivalsDrawnTick = RivalsArrived ? Tick : RivalAt;
            rivalsPath = new Path(S.Grid.Route(S.Grid.NearestFree(start) ?? start, S.Stand(S.RivalsFront)) ?? new[] { start, S.Stand(S.RivalsFront) });
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
            Remember(witness, "saw-act", by, true);
            if (witness.Turned) LadderMove(witness, "saw-violence");
            if (!incidents.Add("seen:" + incident)) return;
            AddTrail(R.WitnessTrail, $"witnessed:{incident}");
            MoveStanding(-R.StandingStep, $"violence-seen:{incident}");
        }

        /// <summary>Every change to Trail, Alert and standing with its cause, in order: the answer to "why is the investigation active?" (§13 T3).</summary>
        public readonly List<(int tick, string what, int delta, int now, string cause)> Ledger = new List<(int, string, int, int, string)>();
        void AddTrail(int n, string cause) { if (n == 0) return; Trail += n; Ledger.Add((Tick, "trail", n, Trail, cause)); }
        void RaiseAlert(int to, string cause) { if (to <= Alert) return; int d = to - Alert; Alert = to; Ledger.Add((Tick, "alert", d, Alert, cause)); }
        void MoveStanding(int d, string cause) { if (d == 0) return; Standing += d; Ledger.Add((Tick, "standing", d, Standing, cause)); }

        public void Finish()
        {
            if (Checkpoints.Count == 0 || Checkpoints[Checkpoints.Count - 1].tick != Tick) Checkpoints.Add((Tick, Hash.State(this)));
        }
    }
}
