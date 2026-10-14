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
        public static Start Parse(JNode? n)
        {
            var s = new Start(); if (n == null) return s;
            n.Only("rivalsAt", "turned", "known", "temperament", "cash");
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

        // the cell
        public int EnforcerHealth, EnforcerNextShot;
        public bool LoopOn;
        // the guard (§05 guard on door routine; mission.json windows.front-door)
        public int LureTick = -1, GuardAwayUntil, GuardHealth, GuardNextShot, MissingNotice = INF;
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
        public bool PatrolDown, PatrolChasing, PatrolTracking; public int PatrolHealth, PatrolAwayUntil, PatrolNextShot;
        Pt chasePos, patrolDownAt;
        // the rivals (§05 rivals)
        public int RivalAt; public bool RivalsArrived, RivalsDrawn, RivalsTracking; public int RivalsDelayedUntil, RivalsDrawnTick, RivalsHealth, RivalsNextShot;
        public string RivalsDelayMethod = "";
        Path? rivalsPath, rivalsRestartPath;
        // the response unit (§05 response unit on report; district seals after it arrives)
        public int SecurityArrives = INF, Lockdown = INF, SecurityHealth, SecurityNextShot; public bool SecurityDown, SecurityTracking;
        readonly Path securityPath;
        // the debrief's facts
        public bool InezWitnessedShooting; public string InezMethod = "";
        public string RouteKind = "", RouteGuard = "", RouteBot = "", RouteRival = "";

        // numbers read once from the rules and the site
        readonly int eye, near, actReach, approachReach, listenReach, witnessRange;
        readonly int opSpeed, secSpeed, rivSpeed, chaseSpeed;
        readonly int walkT, stayT, rivalsArriveS, driverLastS, valeExitsS, valeAtCarS, overS, blockMoveT, blockHoldT;

        public Game(Rules rules, Site site, Start? start = null, string? startText = null)
        {
            R = rules; S = site; Start = start ?? new Start();
            StartHash = Hash.Of(startText ?? "") ^ Hash.Of(Start.Canonical());
            eye = R.RefI("perception", "eye"); near = R.RefI("perception", "near") * 100;
            actReach = R.RefI("cell", "reach") * 100; approachReach = R.VI("approach-reach-m") * 100; listenReach = R.VI("listen-reach-m") * 100;
            witnessRange = R.VI("witness-range-m") * 100;
            opSpeed = R.RefI("cell", "speed"); secSpeed = R.RefI("security", "speed"); rivSpeed = R.RefI("security", "rivals", "speed"); chaseSpeed = R.RefI("security", "patrol", "chase");
            walkT = T(S.S("guardWalkS")); stayT = T(S.S("guardStayS"));
            rivalsArriveS = S.S("rivalsArrive"); driverLastS = S.S("driverLast"); valeExitsS = S.S("valeExits"); valeAtCarS = S.S("valeAtCar"); overS = S.S("over");
            blockMoveT = T(S.S("blockMoveS")); blockHoldT = T(S.S("blockHoldS"));

            Cash = Start.Cash ?? S.Root["purse"].Long();
            Standing = (int)R.Ref("standing", "start");
            string[] crew = { "crew:scout", "crew:fixer", "crew:tech", "crew:enforcer" };
            for (int i = 0; i < 4; i++) Ops[i] = new Op { Id = (OpId)i, Pos = S.Stand(crew[i]) };
            EnforcerHealth = R.RefI("cell", "fire", "enforcerHealth");
            int hits = R.RefI("cell", "fire", "hits");
            GuardHealth = PatrolHealth = RivalsHealth = SecurityHealth = hits;

            string[] places = { S.Ref("guard", "cafe"), S.Ref("car", "driver"), S.Ref("guard", "post"), S.Ref("vale", "exit") };
            string[] roles = { "waitress", "driver", "guard", "councillor" };
            for (int i = 0; i < 4; i++)
            {
                var c = S.Role(roles[i]);
                var p = new Person { Role = roles[i], Id = c.Id, Name = c.Name, Temperament = Start.Temperament.TryGetValue(roles[i], out var tm) ? tm : c.Temperament, Need = c.Need, Place = places[i] };
                var t = R.Temp(p.Temperament); R.CheckNeed(p.Need);
                for (int k = 0; k < 4; k++) p.Sc[k] = t.Start[k];
                People.Add(p);
            }
            Inez.Suspected = Inez.Need; Driver.Suspected = Driver.Need; // Sky 2026-10-15: "in debt" shows as suspected from 0:00, from the briefing
            foreach (var role in Start.Turned) PersonByRole(role).Turned = true;
            foreach (var kv in Start.Known) foreach (var w in kv.Value) PersonByRole(kv.Key).Learn(w);
            RivalAt = T(Start.RivalsAtS ?? rivalsArriveS);

            var post = S.Stand(S.Ref("guard", "post")); var cafe = S.Stand(S.Ref("guard", "cafe"));
            guardOut = new Path(S.Grid.Route(post, cafe) ?? throw new FormatException("no walk from the guard's post to the café"));
            guardBack = new Path(S.Grid.Route(cafe, post) ?? throw new FormatException("no walk from the café to the guard's post"));
            blockPath = new Path(new[] { S.At(S.Ref("car", "pickup")), S.At(S.Ref("car", "block")) });
            var sec1 = S.Grid.Route(S.Stand(S.Ref("security", "entry")), S.Stand(S.Ref("security", "via"))) ?? throw new FormatException("no walk for the response unit");
            var sec2 = S.Grid.Route(S.Stand(S.Ref("security", "via")), S.Stand(S.Ref("security", "to"))) ?? throw new FormatException("no walk for the response unit");
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
        public void Note(string code, string text) => Log.Add(new LogLine { Tick = Tick, Code = code, Text = text });
        void Fail(string code, string text) { if (Result != Outcome.None) return; Result = Outcome.Failure; FailCode = code; FailText = text; Note("fail." + code, text); }
        public bool Over => Result != Outcome.None;

        // ---------------------------------------------------------------- the tick

        public void Step()
        {
            if (Over) return;
            Tick++;
            GuardTimeline(); CarTimeline(); RivalsTimeline(); ValeTimeline();
            foreach (var op in Ops)
            {
                if (op.Busy != null && op.BusyEnd == Tick) { var o = op.Busy; op.Busy = null; Land(op, o); if (Over) break; }
                if (op.Walk != null)
                {
                    op.WalkMm += opSpeed; op.Pos = op.Walk.At(op.WalkMm);
                    if (op.WalkMm >= op.Walk.Length) { op.Walk = null; op.WalkMm = 0; }
                }
            }
            if (!Over) Tether();
            if (!Over) PatrolChase();
            if (!Over && Tick == MissingNotice)
            {
                Note("guard.check-in", "The guard misses his check-in. Security now knows the post is empty.");
                CallSecurity(R.RefI("security", "response", "checkIn"), "The empty guard post is reported.");
            }
            if (!Over) Shooters();
            if (!Over && Tick >= Lockdown && Vale != ValeAt.Car) Fail("sealed", "Security seals the district before Vale reaches the getaway car.");
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
                carToService = new Path(S.Grid.Route(S.Grid.NearestFree(from) ?? from, S.Stand(S.Ref("car", "service"))) ?? new[] { from, S.Stand(S.Ref("car", "service")) });
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
                var to = PickupService ? S.Stand(S.Ref("vale", "service")) : S.Stand(S.Ref("vale", "car"));
                valePath = new Path(S.Grid.Route(S.Stand(S.Ref("vale", "exit")), to) ?? new[] { S.Stand(S.Ref("vale", "exit")), to });
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
            if (LoopOn && !G.Within(tech.Pos, S.At("cabinet"), R.RefI("cell", "loop", "tether") * 100))
            {
                LoopOn = false; Note("loop.ends", "The Tech leaves the cabinet. The camera feed returns.");
            }
        }

        void PatrolChase()
        {
            if (!PatrolChasing || PatrolDown || PatrolAwayUntil > Tick || Tick % Hz != 0) return;
            var e = Enforcer; var target = e.Inside ? S.Stand(S.Ref("security", "to")) : e.Pos;
            var t = S.Grid.NearestFree(target); var from = S.Grid.NearestFree(chasePos);
            if (!t.HasValue || !from.HasValue) return;
            var route = S.Grid.Route(from.Value, t.Value); if (route == null) return;
            chasePos = new Path(route).At(chaseSpeed * Hz); // a second's chase, as the top-down test moved it
        }

        void Shooters()
        {
            var e = Enforcer; if (e.Inside) { PatrolTracking = SecurityTracking = RivalsTracking = false; return; }
            int aim = T(R.RefI("security", "aim"));
            if (GuardEngaged && !GuardDown && GuardWatching(e.Pos) && Tick >= GuardNextShot)
            {
                GuardNextShot = Tick + T(R.RefI("security", "fireEvery", "guard"));
                HitEnforcer("guard", "The guard hits the Enforcer."); if (Over) return;
            }
            bool bot = PatrolChasing && PatrolWatching(e.Pos);
            if (bot && !PatrolTracking) { PatrolTracking = true; PatrolNextShot = Tick + aim; Note("patrol.locks", "The patrol locks onto the Enforcer."); }
            else if (!bot) PatrolTracking = false;
            if (bot && Tick >= PatrolNextShot) { PatrolNextShot = Tick + T(R.RefI("security", "fireEvery", "patrol")); HitEnforcer("patrol", "The patrol fires on the Enforcer."); if (Over) return; }
            bool sec = SecurityWatching(e.Pos);
            if (sec && !SecurityTracking) { SecurityTracking = true; SecurityNextShot = Tick + aim; Note("security.aims", "Security spots the Enforcer and raises its weapons."); }
            else if (!sec) SecurityTracking = false;
            if (sec && Tick >= SecurityNextShot) { SecurityNextShot = Tick + T(R.RefI("security", "fireEvery", "security")); HitEnforcer("security", "Security fires on the Enforcer."); if (Over) return; }
            bool riv = RivalsWatching(e.Pos);
            if (riv && !RivalsTracking) { RivalsTracking = true; RivalsNextShot = Tick + aim; Note("rivals.aim", "The rival crew spots the Enforcer and takes aim."); }
            else if (!riv) RivalsTracking = false;
            if (riv && Tick >= RivalsNextShot) { RivalsNextShot = Tick + T(R.RefI("security", "fireEvery", "rivals")); HitEnforcer("rivals", "The rival crew fires on the Enforcer."); }
        }
        void HitEnforcer(string by, string text)
        {
            EnforcerHealth--; Note("enforcer.hit." + by, $"{text} {EnforcerHealth} health left.");
            if (EnforcerHealth <= 0) Fail("enforcer-down", "The Enforcer is down.");
        }

        // ---------------------------------------------------------------- where things are

        public (Pt pos, Pt facing, string phase)? GuardPose()
        {
            if (GuardDown) return null;
            if (GuardHeld) return (heldPos, heldFacing, heldPhase);
            var post = S.Stand(S.Ref("guard", "post"));
            if (LureTick < 0 || Tick >= GuardAwayUntil) return (post, S.Vec("guard", "facing"), "door");
            int age = Tick - LureTick;
            if (age < walkT) { long mm = guardOut.Length * age / walkT; return (guardOut.At(mm), guardOut.Heading(mm), "walking to café"); }
            if (age < walkT + stayT) return (guardOut.End, S.Vec("guard", "cafeFacing"), "at café");
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
            return car == blockPath.Points[0] ? S.Stand(S.Ref("car", "driver")) : car;
        }
        public Pt? ValePos()
        {
            switch (Vale)
            {
                case ValeAt.Leaving: { int span = T(valeAtCarS) - T(valeExitsS); return valePath!.At(valePath.Length * Math.Min(span, Tick - T(valeExitsS)) / span); }
                case ValeAt.Pickup: case ValeAt.Taking: return PickupService ? S.Stand(S.Ref("vale", "service")) : CarPos();
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
            if (PatrolAwayUntil > Tick) return (S.Stand(S.Ref("patrol", "reroute")), S.Vec("patrol", "rerouteFacing"));
            if (PatrolChasing) return (chasePos, Enforcer.Pos - chasePos);
            return (SweepPos(Tick), S.Vec("patrol", "facing"));
        }
        Pt SweepPos(int t)
        {
            int period = T(R.RefI("security", "patrol", "period")), half = period / 2, ph = t % period;
            return G.Lerp(S.Stand(S.Ref("patrol", "a")), S.Stand(S.Ref("patrol", "b")), ph <= half ? ph : period - ph, half);
        }

        public bool RivalsPresent => RivalsArrived && RivalsDelayedUntil <= Tick;
        public (Pt pos, Pt facing)? RivalsPose()
        {
            if (!RivalsPresent) return null;
            if (!RivalsDrawn)
            {
                bool atService = Tick >= T(valeAtCarS) && PickupService;
                return (S.Stand(atService ? S.Ref("vale", "service") : S.Ref("rivals", "park")), new Pt(0, 100));
            }
            bool restarting = RivalsDelayedUntil > RivalsDrawnTick;
            var path = restarting ? (rivalsRestartPath ??= new Path(S.Grid.Route(S.Stand(S.Ref("rivals", "entry")), S.Stand(S.Ref("rivals", "front")))!)) : rivalsPath!;
            int start = restarting ? RivalsDelayedUntil : RivalsDrawnTick;
            long mm = (long)Math.Max(0, Tick - start) * rivSpeed;
            return mm >= path.Length ? (path.End, Back(path)) : (path.At(mm), path.Heading(mm));
        }

        public (Pt pos, Pt facing)? SecurityPose()
        {
            if (SecurityDown || Tick < SecurityArrives) return null;
            long mm = (long)(Tick - SecurityArrives) * secSpeed;
            if (mm >= securityPath.Length) return (securityPath.End, Back(securityPath));
            return (securityPath.At(mm), securityPath.Heading(mm));
        }

        /// <summary>At the end of its run a unit holds facing back the way it came, as the top-down test's rivals and response unit did.</summary>
        static Pt Back(Path p) { var h = p.Heading(p.Length); return new Pt(-h.X, -h.Y); }

        // ---------------------------------------------------------------- who sees what (rulebook perception)

        bool Sees(Pt from, Pt to) => S.Sight(from, to, eye);
        int M(params string[] p) => R.RefI(p) * 100;
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
            if (phase == "door") return G.InCone(pos, f, M("perception", "door", "reach"), M("perception", "door", "half"), 0, p) && Sees(pos, p);
            return G.InCone(pos, f, M("perception", "facing", "reach"), M("perception", "facing", "half"), near, p) && Sees(pos, p);
        }
        public bool PatrolWatching(Pt p)
        {
            if (PatrolDown || PatrolAwayUntil > Tick) return false;
            var (pos, f) = PatrolPose()!.Value;
            if (PatrolChasing) return G.InCone(pos, f, M("perception", "facing", "reach"), M("perception", "facing", "half"), near, p) && Sees(pos, p);
            return InSweepCone(pos, f, p) && Sees(pos, p);
        }
        bool InSweepCone(Pt pos, Pt f, Pt p)
        {
            long ff = (long)f.X * f.X + (long)f.Y * f.Y, fl = G.Isqrt(ff); if (fl == 0) return false;
            long rx = p.X - pos.X, ry = p.Y - pos.Y;
            long depth = (rx * f.X + ry * f.Y) / fl, side = Math.Abs(rx * f.Y - ry * f.X) / fl;
            if (depth < 0 || depth > M("perception", "patrol", "depth")) return false;
            return side <= R.RefI("perception", "patrol", "halfBase") + depth * R.RefI("perception", "patrol", "halfPerMille") / 1000;
        }
        /// <summary>The top-down test's botSeesEntranceOnPatrol: would the sweep, not rerouted, see the public door now?</summary>
        public bool SweepSeesEntrance(int t)
        {
            var pos = SweepPos(t); var door = S.At(S.Ref("guard", "post"));
            return InSweepCone(pos, S.Vec("patrol", "facing"), door) && Sees(pos, door);
        }
        public bool SecurityWatching(Pt p)
        {
            var s = SecurityPose(); if (!s.HasValue) return false;
            return G.InCone(s.Value.pos, s.Value.facing, M("perception", "security", "reach"), M("perception", "security", "half"), near, p) && Sees(s.Value.pos, p);
        }
        public bool RivalsWatching(Pt p)
        {
            if (!RivalsDrawn) return false;
            var r = RivalsPose(); if (!r.HasValue) return false;
            return G.InCone(r.Value.pos, r.Value.facing, M("perception", "rivals", "reach"), M("perception", "rivals", "half"), near, p) && Sees(r.Value.pos, p);
        }

        // ---------------------------------------------------------------- security, rivals, patrol answering gunfire

        void CallSecurity(int delayS, string text)
        {
            int arrival = Tick + T(delayS);
            if (arrival >= SecurityArrives) return;
            SecurityArrives = arrival; Lockdown = arrival + T(R.RefI("security", "seal"));
            Note("security.called", $"{text} Security is coming from the north street.");
        }
        void RegisterGunfire(string target)
        {
            Gunfire++; AddTrail(R.RefI("cell", "fire", "trail"), "gunfire:" + target);
            RaiseAlert(Math.Min(3, Alert + R.RefI("alert", "perShot")), "gunfire:" + target);
            EnforcerNextShot = Tick + T(R.RefI("cell", "fire", "every"));
            incidents.Add("fire:" + target);
            DrawRivals(); WakePatrol();
        }
        void DrawRivals()
        {
            if (RivalsDrawn) return;
            var start = RivalsArrived && RivalsPose() is var rp && rp.HasValue ? rp.Value.pos : S.Stand(S.Ref("rivals", "entry"));
            RivalsDrawn = true;
            if (!RivalsArrived) RivalAt = Math.Min(RivalAt, Tick + T(R.RefI("security", "rivals", "drawn")));
            RivalsDrawnTick = RivalsArrived ? Tick : RivalAt;
            rivalsPath = new Path(S.Grid.Route(S.Grid.NearestFree(start) ?? start, S.Stand(S.Ref("rivals", "front"))) ?? new[] { start, S.Stand(S.Ref("rivals", "front")) });
            Note("rivals.drawn", "The rivals hear gunfire and head for the Office.");
        }
        void WakePatrol()
        {
            if (PatrolDown || PatrolAwayUntil > Tick || PatrolChasing) return;
            var pos = PatrolPose()!.Value.pos;
            if (!G.Within(pos, Enforcer.Pos, R.RefI("security", "patrol", "breakTo") * 100)) return;
            chasePos = pos; PatrolChasing = true;
            Note("patrol.breaks", "The patrol breaks its route and turns toward the gunfire.");
        }

        /// <summary>A civilian who sees violence (§05: 3 at once; rulebook witness.sees, traceable violence): one incident, once.</summary>
        void ViolenceSeen(string incident, Person witness)
        {
            RaiseAlert(R.RefI("alert", "violenceSeen"), $"violence-seen:{incident}:{witness.Role}");
            if (!incidents.Add("seen:" + incident)) return;
            AddTrail(R.VI("witness-trail"), $"witnessed:{incident}:{witness.Role}");
            MoveStanding(-R.VI("standing-step"), $"violence-seen:{incident}");
            Remember(witness, "saw-act", "enforcer", true);
            if (witness.Turned) LadderMove(witness, "saw-violence");
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
