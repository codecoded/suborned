// The response as a dial (brief 013 B, Keel; Sky 2026-10-16: "it's easy to swarm the play area with robots if we need to temper the game
// play"): the rulebook's `response` section (Motive's levels, briefs/013/motive-takedowns.md §5.3; Pulse's behaviour, briefs/013/
// pulse-witnesses.md §12, §13). The level is data (the district's base + the mission's bonus, sim/data/handover.provisional.json until the
// plan and mission carry it; a scenario may fix it). A landed alarm sends robots at Alert 1, units and robots at Alert 2, the cordon at 3;
// units verify the scene (confirmed or unfounded), search, hold, and stand down. Also here: the staff check-in marks (Motive §5.1) and a
// person who misses an expected tie (§5.2).
// The response unit of §05 (the two officers of SecGuns, walking from the security entry) stays the first unit, so a run at level 1 with
// gunfire only is the run it was.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>One member of the response (a robot or an officer) sent by an alarm: where it starts and goes, what it is doing.</summary>
    public sealed class Unit
    {
        public string Id = ""; public bool Robot, Patrol, Legacy; public Gun Gun = null!; public int Speed;
        public Pt From, Dest; public int Launch; public Path? Route; public int Act = -1; public bool Scene, Cordon;
        public string St = "wait";   // wait (not yet on the street), walk, check, search, hold, back, gone
        public int Until, Arrive; public bool Verified;
        public readonly List<string> ToOpen = new List<string>(); public int NextOpen = -1;
        public bool OnStreet(int tick) => St != "wait" && St != "gone" && (Legacy || tick >= Launch);
    }

    public sealed partial class Game
    {
        public readonly List<Unit> Units = new List<Unit>();
        readonly SortedSet<string> missedMarks = new SortedSet<string>(StringComparer.Ordinal);       // staff whose missed check-in has landed
        readonly SortedDictionary<string, int> suspicious = new SortedDictionary<string, int>(StringComparer.Ordinal); // "waiter>expected": since when
        readonly List<string> extraCivGuns = new List<string>();                                         // crowd people shot at, by id
        int robotsSent, unitsSent, policeSeq, robotSeq, reinforceAt = INF, lastAlarm = -1, guardBackAt = -1;
        List<Pt> dests = new List<Pt>();

        /// <summary>The security level of this run (0..4): the scenario's, else the district's base + the mission's bonus.</summary>
        public int SecurityLevel => Start.Security ?? S.SecurityLevel;
        /// <summary>The last alarm's destinations, in order (Pulse §12.1): the scene first. For the HUD and the scenarios.</summary>
        public IReadOnlyList<Pt> AlarmDestinations => dests;
        public DialLevel DialNow => R.Dial[SecurityLevel];
        /// <summary>The seal after a confirmation (or a gunfire response's arrival), at this level.</summary>
        public int SealS => DialNow.SealS;
        /// <summary>A gunfire report's arrival at this level (rulebook security.rule): the delays are level 1's arrivals; the report lands the
        /// same seconds after the shot, the unit rolls this level's dispatch later. At level 1 exactly the delay.</summary>
        int GunfireArrival(int delayS) => Math.Max(Tick + 1, Tick + T(delayS) + T(DialNow.DispatchS) - T(R.Dial[1].DispatchS));

        // ---------------------------------------------------------------- an alarm

        /// <summary>A landed alarm (Pulse §12): the destination list, then the response the level sends at the Alert it leaves, topped up,
        /// never repeated. raiser: who called it (their ties are tier 3); the act's victim is the missing person (tier 2). step: a later
        /// report of an incident already dispatched raised Alert: the same destinations, topped up to the new Alert's counts.</summary>
        TdAct? lastAct;
        void Alarm(TdAct? a, Pt scene, Person? raiser, string stash, bool step = false)
        {
            lastAlarm = Tick; lastAct = a;
            var lv = DialNow;
            if (!step || dests.Count == 0) BuildDests(a, scene, raiser);
            int land = Tick;
            // robots: a1 at Alert 1, a2 from Alert 2 (the dial's count, topped up)
            int wantR = Alert >= 2 ? lv.A2Robots : Alert >= 1 ? lv.A1Robots : 0; // a level's count, topped up: robots already out stay out
            int launch = land + T(lv.DispatchS);
            int k = SendRobots(a, wantR, land, launch, stash);
            // units at Alert 2: the response unit first (§05), then police units; the cordon at Alert 3
            if (Alert >= 2)
            {
                int wantU = lv.A2Units;
                if (unitsSent < wantU && SecurityArrives == INF) { CallScene(launch, scene, a); }
                for (int i = 0; unitsSent < wantU && i < 16; i++) { if (!SendPolice(a, scene, true, launch + k * R.LaunchGapTicks, stash)) break; k++; }
                if (reinforceAt == INF && lv.ReinforceS > 0) reinforceAt = launch + T(lv.ReinforceS);
            }
            if (Alert >= 3 && !cordonSent) Cordon(launch + k * R.LaunchGapTicks);
            CoverScene(a, scene, stash, launch);
            Note("alarm.dispatches", $"{(step ? $"Alert {Alert}: the desk tops the response up" : "The desk sends the response")} (level {lv.Level}, {lv.Name}): {robotsSent} robot{(robotsSent == 1 ? "" : "s")}, {unitsSent} unit{(unitsSent == 1 ? "" : "s")}, on the street {Sim.Report.Clock(launch)}.");
        }
        bool cordonSent;
        /// <summary>A landed alarm's scene must be checked (Pulse §12.2: re-run at the next landed alarm): if no unit is sent to it, the
        /// response member nearest it in walking time (a robot or an officer, not the cordon; ties on id) is sent there instead.</summary>
        void CoverScene(TdAct? a, Pt scene, string stash, int launch)
        {
            if (a == null) return;
            var at = S.Grid.NearestFree(scene) ?? scene;
            foreach (var u in Units) if (u.St != "gone" && u.Act == a.Id && u.Scene && G.Within(u.Dest, at, witnessRange)) { if (stash.Length > 0 && !u.ToOpen.Contains(stash) && u.St != "hold") u.ToOpen.Add(stash); return; }
            Unit? best = null; int bt = int.MaxValue;
            foreach (var u in Units)
            {
                if (u.St == "gone" || u.St == "back" || u.Legacy || u.Cordon || u.Gun.Down) continue;
                if (u.Robot && !G.Within(u.Patrol ? SweepPos(Tick) : u.From, at, R.RobotReachCm)) continue; // beyond robot reach: for officers
                var from = u.St == "wait" ? (u.Patrol ? PatrolPose()!.Value.pos : u.From) : UnitPose(u)?.pos ?? u.From;
                int t = TravelTicks(from, at, u.Speed);
                if (t < bt || (t == bt && best != null && string.CompareOrdinal(u.Id, best.Id) < 0)) { bt = t; best = u; }
            }
            if (best == null) return;
            if (best.St != "wait")
            {
                var from = UnitPose(best)!.Value.pos; var route = S.Grid.Route(S.Grid.NearestFree(from) ?? from, at);
                best.Route = new Path(route != null && route.Length >= 2 ? route : new[] { from, at }); best.Launch = Tick; best.St = "walk"; // radioed: it turns now
            }
            best.Dest = at; best.Act = a.Id; best.Scene = true; best.Verified = false; best.ToOpen.Clear(); if (stash.Length > 0) best.ToOpen.Add(stash);
            Note("alarm.reassigns", $"{best.Id} is sent to the new scene.");
        }
        /// <summary>A gunfire report's response (Motive §5.3: gunfire is an alarm): the response unit is the first unit, then the level's
        /// Alert 2 robots to the scene and its destinations, the reinforcement clock and, at Alert 3, the cordon.</summary>
        void GunfireAlarm(int arrival, Pt scene)
        {
            lastAlarm = Tick; var lv = DialNow;
            unitsSent++;
            BuildDests(null, scene, null);
            int want = Alert >= 2 ? lv.A2Robots : lv.A1Robots;
            int land = arrival - T(lv.DispatchS);
            int k = SendRobots(null, want, land, arrival, "");
            if (reinforceAt == INF && lv.ReinforceS > 0 && Alert >= 2) reinforceAt = arrival + T(lv.ReinforceS);
            if (Alert >= 3 && !cordonSent) Cordon(arrival + k * R.LaunchGapTicks);
        }
        /// <summary>Robots to the alarm's places in order, one each, then round again while the level wants more (Pulse §12.2); a place no
        /// robot can take (beyond robot reach of every free robot) is left to the officers. Returns the robots sent.</summary>
        int SendRobots(TdAct? a, int want, int land, int launch, string stash)
        {
            int k = 0;
            for (int pass = 0; robotsSent < want && pass < 8; pass++)
            {
                bool any = false;
                foreach (var d in dests) { if (robotsSent >= want) break; if (SendRobot(a, d, land, launch + k * R.LaunchGapTicks, stash)) { k++; any = true; } }
                if (!any) break;
            }
            return k;
        }

        /// <summary>The destination list (Pulse §12.1; Aster 2026-10-16, both tiers): the scene; the missing person's post, car and ties' stands
        /// now; the raiser's ties' stands now; duplicates within witness range dropped.</summary>
        void BuildDests(TdAct? a, Pt scene, Person? raiser)
        {
            var l = new List<Pt>();
            void add(Pt? p) { if (!p.HasValue) return; var f = S.Grid.NearestFree(p.Value) ?? p.Value; foreach (var x in l) if (G.Within(x, f, witnessRange)) return; l.Add(f); }
            add(scene);
            if (a != null)
            {
                var v = a.P;
                var post = PostOf(v, Tick); if (post.HasValue) add(post.Value.at);
                if (v == Driver) add(CarPos());
                int n = 0; foreach (var t in TiePlaces(v, raiser)) { if (n++ >= R.AlarmSpread) break; add(t); }
            }
            if (raiser != null) { int n = 0; foreach (var t in TiePlaces(raiser, a?.P)) { if (n++ >= R.AlarmSpread) break; add(t); } }
            dests = l;
        }
        /// <summary>Where a person's lover, family and workmates are meant to be now (their routine; indoors, their unit's door), by kind
        /// then id; `skip` left out.</summary>
        IEnumerable<Pt> TiePlaces(Person p, Person? skip)
        {
            if (People2 == null || !People2.Ties.TryGetValue(p.Id, out var ts)) yield break;
            foreach (var kind in R.CloseTies.Count == 0 ? new List<string>() : new List<string> { "lover", "family", "workmate" })
            {
                var ids = new List<string>(); foreach (var (k, id) in ts) if (k == kind && R.CloseTies.Contains(k)) ids.Add(id);
                ids.Sort(StringComparer.Ordinal);
                foreach (var id in ids)
                {
                    var q = PersonById(id); if (q == null || q == skip) continue;
                    var at = PosOfPerson(q);
                    if (at.HasValue) { yield return at.Value; continue; }
                    var w = WalkerOf(q); if (w == null) continue;
                    var door = DoorOfStop(w, Tick); if (door.HasValue) yield return door.Value;
                }
            }
        }
        /// <summary>The staffed door of the unit a person's current stop is at (indoors), if any: "door:" + the place's id among the refuges.</summary>
        Pt? DoorOfStop(Walker w, int tick)
        {
            for (int i = 0; i < w.Place.Length; i++)
                if (tick >= w.From[i] && tick < w.To[i]) { foreach (var (id, at, kind) in S.Refuges) if (kind == "door" && id == "door:" + w.Place[i]) return at; return null; }
            return null;
        }

        /// <summary>The robot for a destination (Pulse §12.2): the patrol when it is on its sweep and nearer, else a robot from the nearest dock
        /// (none placed yet: the response unit's entry), one every launch.gap.</summary>
        bool SendRobot(TdAct? a, Pt dest, int land, int launch, string stash)
        {
            if (CombatantCount() >= R.CombatantCap) return false;
            bool patrolFree = !PatrolDown && !PatrolChasing && PatrolAwayUntil <= Tick && PatrolUnit() == null && !PatrolAlarmed;
            var origin = OriginOf(dest, S.Docks);
            // robot reach (Pulse §12.2; Motive rev 3): a robot takes a place only within robotReach of where it starts
            bool patrolCan = patrolFree && G.Within(PatrolPose()!.Value.pos, dest, R.RobotReachCm), dockCan = G.Within(origin, dest, R.RobotReachCm);
            if (!patrolCan && !dockCan) return false;
            Unit u;
            if (patrolCan && (!dockCan || TravelTicks(PatrolPose()!.Value.pos, dest, R.RobotSpeed) <= TravelTicks(origin, dest, R.RobotSpeed) + (launch - (land + T(R.RobotWireS)))))
            {
                u = new Unit { Id = "patrol", Robot = true, Patrol = true, Gun = PatrolGun, Speed = R.RobotSpeed, Launch = land + T(R.RobotWireS), Dest = dest, From = PatrolPose()!.Value.pos };
                PatrolAlarmed = true;
            }
            else u = new Unit { Id = "robot-" + (++robotSeq), Robot = true, Gun = NewGun("robot-" + robotSeq, R.Pistol, true, R.HealthPatrol), Speed = R.RobotSpeed, Launch = launch, From = origin, Dest = dest };
            u.Act = a?.Id ?? -1; u.Scene = dests.Count > 0 && G.Within(dest, dests[0], 100); if (u.Scene && stash.Length > 0) u.ToOpen.Add(stash);
            Units.Add(u); robotsSent++;
            return true;
        }
        bool PatrolAlarmed;
        bool SendPolice(TdAct? a, Pt dest, bool scene, int launch, string stash)
        {
            if (CombatantCount() + R.ResponseOfficers > R.CombatantCap) return false;
            var origin = OriginOf(dest, S.Posts);
            for (int k = 0; k < R.ResponseOfficers; k++)
            {
                var id = "police-" + (++policeSeq);
                var u = new Unit { Id = id, Gun = NewGun(id, R.Pistol, true, R.HealthOfficer), Speed = secSpeed, Launch = launch + k * R.LaunchGapTicks, From = origin, Dest = dest, Act = a?.Id ?? -1, Scene = scene };
                if (scene && stash.Length > 0) u.ToOpen.Add(stash);
                Units.Add(u);
            }
            unitsSent++;
            return true;
        }
        /// <summary>The response unit (§05's two officers) to a silent alarm's scene: it rolls at `arrival` from its entry; the seal waits for
        /// a confirmation (Motive §2.3). Its officers verify and search like any unit.</summary>
        void CallScene(int arrival, Pt scene, TdAct? a)
        {
            var from = S.Stand(S.SecEntry); var to = S.Grid.NearestFree(scene) ?? scene;
            var route = S.Grid.Route(from, to);
            if (route != null && route.Length >= 2) securityPath = new Path(route);
            SecurityArrives = arrival; unitsSent++;
            if (sealOnArrival) { sealOnArrival = false; SealFrom(arrival); }
            for (int k = 0; k < SecGuns.Length; k++) Units.Add(new Unit { Id = SecGuns[k].Id, Legacy = true, Gun = SecGuns[k], Speed = secSpeed, Launch = arrival, Dest = to, Act = a?.Id ?? -1, Scene = true, St = "walk" });
            Note("security.called", "The response unit is sent to the scene. Security is coming from the north street.");
        }
        void Cordon(int launch)
        {
            cordonSent = true; var lv = DialNow;
            var mouths = new List<Pt>(); foreach (var (id, at, kind) in S.Refuges) if (kind == "mouth") mouths.Add(S.Grid.NearestFree(at) ?? at);
            for (int i = 0; i < lv.A3Units && mouths.Count > 0; i++) { var u0 = Units.Count; if (!SendPolice(null, mouths[i % mouths.Count], false, launch + i * R.LaunchGapTicks, "")) break; for (int j = u0; j < Units.Count; j++) Units[j].Cordon = true; }
            if (lv.DoorsLock) Note("doors.lock", "Lockdown: the Office doors are locked.");
        }
        Pt OriginOf(Pt dest, List<(string id, Pt at)> places)
        {
            if (places.Count == 0) return S.Stand(S.SecEntry);
            (string id, Pt at) best = places[0]; long bd = long.MaxValue;
            foreach (var p in places) { long d = G.D2(p.at, dest); if (d < bd || (d == bd && string.CompareOrdinal(p.id, best.id) < 0)) { bd = d; best = p; } }
            return best.at;
        }
        int TravelTicks(Pt from, Pt to, int speed)
        {
            var a = S.Grid.NearestFree(from) ?? from; var b = S.Grid.NearestFree(to) ?? to;
            var route = S.Grid.Route(a, b); long len = route == null ? G.Dist(a, b) * 10L : new Path(route).Length;
            return (int)((len + speed - 1) / speed);
        }
        int CombatantCount()
        {
            int n = 0;
            if (!GuardDown && !GuardGun.Down) n++; if (!PatrolDown) n++;
            foreach (var g in SecGuns) if (!g.Down) n++;
            if (RivalsPresent) n++;
            foreach (var h in Hostiles) if (!h.Down && h.Present) n++;
            foreach (var u in Units) if (!u.Patrol && !u.Legacy && !u.Gun.Down && u.St != "gone") n++;
            return n;
        }
        public Unit? PatrolUnit() { foreach (var u in Units) if (u.Patrol && u.St != "gone" && u.St != "wait") return u; return null; }
        Unit? UnitOf(string id) { foreach (var u in Units) if (u.Id == id && !u.Legacy && !u.Patrol) return u; return null; }

        // ---------------------------------------------------------------- the units, a tick at a time

        /// <summary>Where a unit stands and faces now, or null off the street.</summary>
        public (Pt pos, Pt facing)? UnitPose(Unit u)
        {
            if (u.Legacy) { int k = SecIndex(u.Id); return k >= 0 ? SecOfficerPose(k) : null; }
            if (u.Gun.Down) return null;
            switch (u.St)
            {
                case "wait": case "gone": return null;
                case "walk": case "back":
                    {
                        if (u.Route == null) return (u.From, new Pt(100, 0));
                        long mm = (long)(Tick - u.Launch) * u.Speed; if (mm < 0) mm = 0;
                        return mm >= u.Route.Length ? (u.Route.End, u.Route.Heading(u.Route.Length)) : (u.Route.At(mm), u.Route.Heading(mm));
                    }
                default: return u.Route == null ? (u.Dest, new Pt(100, 0)) : (u.Route.End, u.Route.Heading(u.Route.Length));
            }
        }
        bool UnitSees(Unit u, Pt p, bool crowd = true)
        {
            var s = UnitPose(u); if (!s.HasValue) return false;
            return G.InCone(s.Value.pos, s.Value.facing, R.SecReachCm, R.SecHalfCm, near, p) && Sees(s.Value.pos, p) && !(crowd && CrowdBlocks(s.Value.pos, p));
        }

        void UnitsStep()
        {
            if (Units.Count == 0 && reinforceAt == INF) return;
            // an Alert step re-runs the dispatch (Pulse §12.2): units at Alert 2 after an Alert 1 alarm, the cordon at 3
            if (lastAlarm >= 0)
            {
                var lv0 = DialNow; int launch0 = Tick + T(lv0.DispatchS);
                if (Alert >= 2 && unitsSent == 0) { var sc = dests.Count > 0 ? dests[0] : S.Stand(S.SecTo); CallScene(launch0, sc, lastAct); if (reinforceAt == INF && lv0.ReinforceS > 0) reinforceAt = launch0 + T(lv0.ReinforceS); }
                if (Alert >= 2 && robotsSent < lv0.A2Robots) SendRobots(lastAct, lv0.A2Robots, Tick, launch0, "");
                if (Alert >= 3 && !cordonSent) Cordon(launch0);
            }
            // reinforcement (Motive §5.3): one more unit every `reinforce` while Alert is 2 or more, up to the cap (Alert 3's own on top)
            var lv = DialNow;
            if (Tick >= reinforceAt && Alert >= 2)
            {
                int cap = lv.UnitCap;
                if (unitsSent < cap && lv.ReinforceS > 0)
                {
                    var dest = FewestUnits();
                    if (SendPolice(null, dest, dests.Count > 0 && G.Within(dest, dests[0], 100), Tick, "")) Note("response.reinforces", $"Another unit is sent ({unitsSent} of {cap}).");
                }
                reinforceAt = unitsSent < cap && lv.ReinforceS > 0 ? Tick + T(lv.ReinforceS) : INF;
            }
            bool standDown = StoodDown();
            // by index: a search that opens a stash can find the body of an incident not yet reported, which lands and dispatches (Units
            // grows inside the loop; the same class of fault as ReportsStep's, brief 013 D). A unit added now waits for its launch.
            for (int ui = 0; ui < Units.Count; ui++)
            {
                var u = Units[ui];
                if (u.Gun.Down && u.St != "gone") { u.St = "gone"; if (u.Patrol) PatrolAlarmed = false; continue; }
                switch (u.St)
                {
                    case "wait":
                        if (Tick < u.Launch) break;
                        {
                            var from = u.Patrol ? PatrolPose()!.Value.pos : u.From;
                            var a = S.Grid.NearestFree(from) ?? from; var b = S.Grid.NearestFree(u.Dest) ?? u.Dest;
                            var route = S.Grid.Route(a, b); u.Route = new Path(route != null && route.Length >= 2 ? route : new[] { a, b });
                            u.Launch = Tick; u.St = "walk";
                            if (u.Patrol) Note("patrol.alarm", "The patrol leaves its sweep for the alarm.");
                        }
                        break;
                    case "walk":
                        if (u.Legacy) { if (Tick >= SecurityArrives && (long)(Tick - SecurityArrives) * secSpeed >= securityPath.Length) { u.St = "check"; u.Until = Tick + T(R.CheckS); u.Arrive = Tick; } break; }
                        if (u.Route != null && (long)(Tick - u.Launch) * u.Speed >= u.Route.Length) { u.St = "check"; u.Until = Tick + T(R.CheckS); u.Arrive = Tick; }
                        break;
                    case "check":
                        if (Tick < u.Until) break;
                        if (!u.Robot && u.Scene && (u.ToOpen.Count > 0 || Alert >= 2 || HeldScene(u))) // rev 4: a held scene's officers sweep it on arrival
                        {
                            u.St = "search"; u.Until = Tick + T(R.SearchS); u.NextOpen = Tick + T(R.SearchStashS);
                            if (Alert >= 2) foreach (var s in StashesNear(u.Act >= 0 ? Acts[u.Act].Scene : u.Dest)) if (!u.ToOpen.Contains(s) && !Units.Exists(o => o != u && o.St == "search" && o.ToOpen.Contains(s))) u.ToOpen.Add(s); // the unit's officers share the stashes out
                        }
                        else if (u.Robot && u.ToOpen.Count > 0) { u.St = "search"; u.Until = Tick + T(R.SearchS); u.NextOpen = Tick + T(R.SearchStashS); }
                        else u.St = "hold";
                        break;
                    case "search":
                        // the stash comes off the list before it is opened: a body inside lands its incident, which may send this very unit on
                        // to a new scene (CoverScene clears its list and turns it to walk; Keel 2026-10-16, an ArgumentOutOfRange at 7:58.5
                        // on a driver-route variant). Then the search is over here.
                        if (u.ToOpen.Count > 0 && Tick >= u.NextOpen) { var sid = u.ToOpen[0]; u.ToOpen.RemoveAt(0); u.NextOpen = Tick + T(R.SearchStashS); OpenStash(u, sid); if (u.St != "search") break; }
                        if (Tick >= u.Until && u.ToOpen.Count == 0) u.St = "hold";
                        break;
                }
                if (standDown && (u.St == "hold" || (u.St == "check" && !HeldScene(u))) && !u.Legacy && !u.Cordon)
                {
                    if (u.Patrol) { var p = UnitPose(u)!.Value.pos; u.St = "gone"; PatrolAlarmed = false; chasePos = p; PatrolChasing = true; patrolReturning = true; PatrolGun.NoFoeSince = -1; Note("patrol.stands-down", "The patrol goes back to its sweep."); continue; }
                    var here = UnitPose(u)!.Value.pos; var route = S.Grid.Route(S.Grid.NearestFree(here) ?? here, S.Grid.NearestFree(u.From) ?? u.From);
                    u.Route = new Path(route != null && route.Length >= 2 ? route : new[] { here, u.From }); u.Launch = Tick; u.St = "back";
                }
                if (u.St == "back" && u.Route != null && (long)(Tick - u.Launch) * u.Speed >= u.Route.Length) u.St = "gone";
                // verify the scene (Pulse §3.5): within witness range of it on a clear line
                if (u.Scene && !u.Verified && u.Act >= 0 && u.OnStreet(Tick) && !u.Gun.Down) Verify(u);
            }
            // what the response sees on its way and where it holds: bodies (Pulse §3.5)
            if (Tick % R.BodyCheckTicks == 0)
                foreach (var b in Bodies.ToArray())
                {
                    if (!b.Open || b.Seizing) continue;
                    foreach (var u in Units) if (u.OnStreet(Tick) && !u.Gun.Down && UnitSees(u, BodyPos(b))) { FoundBySecurity(b, u.Id); break; }
                }
        }
        Pt FewestUnits()
        {
            if (dests.Count == 0) return S.Stand(S.SecTo);
            Pt best = dests[0]; int bn = int.MaxValue;
            foreach (var d in dests) { int n = 0; foreach (var u in Units) if (!u.Robot && u.St != "gone" && G.Within(u.Dest, d, 100)) n++; if (n < bn) { bn = n; best = d; } }
            return best;
        }
        List<string> StashesNear(Pt at)
        {
            var l = new List<(long d, string id)>();
            foreach (var s in S.Stashes) { long d = G.D2(s.Stand, at); if (d <= (long)R.SearchStashCm * R.SearchStashCm) l.Add((d, s.Id)); }
            l.Sort((x, y) => x.d != y.d ? x.d.CompareTo(y.d) : string.CompareOrdinal(x.id, y.id));
            return l.ConvertAll(x => x.id);
        }

        /// <summary>A unit at the scene: an open body of the act there (or carried there) is confirmed; else unfounded (Pulse §3.5).</summary>
        void Verify(Unit u)
        {
            var a = Acts[u.Act]; var pose = UnitPose(u); if (!pose.HasValue) return;
            var scene = a.Scene;
            if (!G.Within(pose.Value.pos, scene, witnessRange) || !Sees(pose.Value.pos, scene)) return;
            u.Verified = true;
            var b = BodyOf(a.P);
            if (b != null && b.Open && !b.Seizing && G.Within(BodyPos(b), scene, witnessRange) && Sees(pose.Value.pos, BodyPos(b))) { FoundBySecurity(b, u.Id); return; }
            if (!a.Confirmed) Note("report.unfounded", $"{u.Id} finds nothing where {Who(a.P)} was reported.");
        }

        /// <summary>A stash opened by the search or a report from it: a body inside is found (out of it, beside it).</summary>
        void OpenStash(Unit u, string id)
        {
            var s = StashById(id); if (s == null) return;
            foreach (var b in Bodies.ToArray())
            {
                if (b.Stash != id) continue;
                var a = ActOf(b); a.Opened.Add(id);
                b.Stash = ""; b.At = s.Stand;
                Note("stash.opened", $"{u.Id} opens the {id}: {Who(b.P)} is inside.");
                FoundBySecurity(b, u.Id);
                return;
            }
            Note("stash.opened", $"{u.Id} opens the {id}: nothing.");
        }

        /// <summary>The response finds a body (its own eyes): its incident lands if it hadn't, and is confirmed.</summary>
        void FoundBySecurity(Body b, string by)
        {
            var a = ActOf(b);
            if (!a.Landed) LandAct(a, b.Dead ? "dead" : "down", by, null, BodyPos(b), SevOf(b.Dead ? "dead" : "down"), "");
            else HoldScene(a, b.Dead ? "dead" : "down", BodyPos(b)); // found by the response's own eyes after another report (a missed mark): the scene is held
            Confirm(a, b, by);
        }

        /// <summary>A confirmation (Motive §2.3; rulebook report.verifies): Alert to the confirmed level (a kill a search, a knockout stays),
        /// the act's Trail, a found killing's Standing, the seal at a confirmation at Alert 2 or more; a knocked-out victim is brought round.</summary>
        void Confirm(TdAct a, Body? b, string by)
        {
            if (a.Confirmed) return; a.Confirmed = true;
            if (a.Kill) RaiseAlert(Math.Max(a.Alert, R.AlertDead), "confirmed:dead:" + a.Victim);
            a.Alert = Math.Max(a.Alert, Alert);
            if (!a.Take) ChargeAct(a); // a Take keeps its own Trail (cell.take)
            if (a.Kill && !a.StandingPaid) { a.StandingPaid = true; MoveStanding(R.FoundKillStanding, "found-killing:" + a.Victim, "found-killing", Who(a.P)); }
            Note("report.confirmed", $"{by.Replace("camera:", "the camera ")} confirms {Who(a.P)}: {(a.Take ? "taken" : a.Kill ? "dead" : "out")}.");
            if (Alert >= 2) SealFrom(Math.Max(Tick, SecurityArrives)); // the seal is the unit's cordon: from the later of the confirmation and the unit on the street (Motive rev 3)
            if (b != null && !a.Kill && b.P.Down && Bodies.Contains(b) && !b.Seizing) Wake(b, true);
        }

        bool sealOnArrival; // confirmed at Alert 2 or more before the response unit was called: the seal waits for it
        void SealFrom(int at)
        {
            if (at >= INF) { sealOnArrival = true; return; }
            int seal = at + T(SealS);
            if (seal < Lockdown) { Lockdown = seal; Note("seal.starts", $"Confirmed at Alert {Alert}: the district seals {SealS} s after the unit is on the street ({Sim.Report.Clock(at)})."); }
        }

        // ---------------------------------------------------------------- the scene held (Motive rev 4 R4.1; Sky 2026-10-16)

        /// <summary>A found body's scene: where it lay when its report landed, and the entrances the unit there holds.</summary>
        public sealed class SceneHold { public int Act; public Pt At; public readonly List<string> Doors = new List<string>(); }
        public readonly List<SceneHold> SceneHolds = new List<SceneHold>();

        /// <summary>The entrances a unit at `at` holds (rev 4 R4.1): the one the body lies at (within near) and every entrance within witness
        /// range on a clear line (the witness test, so the price can show it). In the site's order.</summary>
        public List<string> DoorsInSightOf(Pt at)
        {
            var l = new List<string>();
            foreach (var e in S.Entrances) if (G.Within(e.at, at, near) || (G.Within(e.stand, at, witnessRange) && Sees(at, e.stand))) l.Add(e.id);
            return l;
        }
        string NearestEntrance(Pt p) { string best = ""; long bd = long.MaxValue; foreach (var e in S.Entrances) { long d = G.D2(e.at, p); if (d < bd) { bd = d; best = e.id; } } return best; }
        /// <summary>The door Enter goes in by (the public door at the guard's post) and the one the Enforcer comes out by with Vale (the
        /// service door by the lane): the site's entrances nearest each.</summary>
        public string EnterDoor => NearestEntrance(S.At(S.GuardPost));
        public string ExitDoor => NearestEntrance(S.Stand(S.ValeService));
        /// <summary>The response stands down (existing timers): stand-down.inquiry after the last alarm at Alert 1, .search at 2, never at 3.</summary>
        bool StoodDown() => Alert < 3 && lastAlarm >= 0 && Tick - lastAlarm >= T(Alert >= 2 ? R.StandDownSearchS : R.StandDownInquiryS);
        bool HeldScene(Unit u) => !u.Robot && u.Scene && u.Act >= 0 && SceneHolds.Exists(h => h.Act == u.Act);
        /// <summary>A scene is held from an officer's arrival at it (checking, searching) until the unit stands down.</summary>
        bool HoldActive(SceneHold h)
        {
            foreach (var u in Units)
            {
                if (u.Robot || u.Act != h.Act || !u.Scene || u.Gun.Down) continue;
                if (!(u.St == "check" || u.St == "search" || (u.St == "hold" && !StoodDown()))) continue;
                var p = UnitPose(u); if (p.HasValue && G.Within(p.Value.pos, h.At, witnessRange)) return true;
            }
            return false;
        }
        /// <summary>Is this entrance held now (no Enter, no exit through it)?</summary>
        public bool DoorHeld(string id) { if (id.Length == 0) return false; foreach (var h in SceneHolds) if (h.Doors.Contains(id) && HoldActive(h)) return true; return false; }

        /// <summary>A report that a body lies here has landed (rev 4 R4.1): the scene is held. "Found" is a landed report of a person lying
        /// (down, dead, a takedown seen) whose body is still there, open, or a woken victim's own report where he woke; an empty post (a
        /// missed mark), a missing person, a carry or a body already moved holds nothing. The unit comes at any Alert, officers (no robot
        /// reach), the level's dispatch after the landing, unless one is already sent to this act's scene (Alert 2's response unit); it is
        /// not one of the dial's counted units. One hold an act.</summary>
        void HoldScene(TdAct a, string kind, Pt place)
        {
            if (a.Take || !(kind == "down" || kind == "dead" || kind == "seen" || kind == "woken")) return;
            foreach (var x in SceneHolds) if (x.Act == a.Id) return;
            Pt at; var b = BodyOf(a.P);
            if (b != null && b.Open && !b.Carrier.HasValue && G.Within(b.At, place, witnessRange)) at = b.At;
            else if (kind == "woken") at = place;
            else return;
            var h = new SceneHold { Act = a.Id, At = at }; h.Doors.AddRange(DoorsInSightOf(at));
            SceneHolds.Add(h);
            if (!Units.Exists(u => !u.Robot && u.Act == a.Id && u.Scene && u.St != "gone" && u.St != "back" && G.Within(u.Dest, S.Grid.NearestFree(at) ?? at, witnessRange))) // a unit already bound for this scene (Alert 2's response unit)
            {
                int counted = unitsSent;
                if (SendPolice(a, S.Grid.NearestFree(at) ?? at, true, Tick + T(DialNow.DispatchS), "")) { unitsSent = counted; Note("security.called", $"A body is found: a unit is sent to hold the scene{(h.Doors.Count > 0 ? " (" + string.Join(", ", h.Doors) + ")" : "")}. Security is coming."); }
            }
        }

        // ---------------------------------------------------------------- check-in marks (Motive §5.1)

        /// <summary>A staff person's post now or next in their routine (a stop at a staffed door's place), and its shift (contiguous stops
        /// there). The cast guard's post is the mission's (he holds it all mission).</summary>
        (string place, Pt at, int from, int to)? PostOf(Person p, int tick)
        {
            if (!IsStaff(p)) return null;
            var stops = StopsOf(p); if (stops == null) return null;
            for (int i = 0; i < stops.Count; i++)
            {
                var (place, from, to, _) = stops[i];
                Pt? door = null; foreach (var (id, at, kind) in S.Refuges) if (kind == "door" && id == "door:" + place) door = at;
                if (!door.HasValue) continue;
                int j = i; while (j + 1 < stops.Count && stops[j + 1].place == place && stops[j + 1].from == stops[j].to) j++;
                int end = p == GuardP ? INF : stops[j].to;
                if (end > tick) return (place, door.Value, from, end);
                i = j;
            }
            return null;
        }
        List<(string place, int from, int to, bool outside)>? StopsOf(Person p) => People2 != null && People2.Stops.TryGetValue(p.Id, out var s) ? s : null;
        /// <summary>The next check-in mark at or after `tick` of a staff person (ticks), or -1.</summary>
        public int NextMarkOf(Person p, int tick)
        {
            var post = PostOf(p, tick); if (!post.HasValue) return -1;
            int period = T(R.CheckInPeriodS), from = post.Value.from;
            int k = tick <= from ? 0 : (tick - from + period - 1) / period;
            int m = from + k * period;
            return Math.Min(m, post.Value.to);
        }

        /// <summary>At each mark, a staff person who is down, held or hidden misses it: the report lands at the mark, at the post (Alert mark).
        /// The guard shot (no takedown) is the same: his missed mark calls the response (Motive §5.1; retires the 18 s check-in).</summary>
        void CheckIns()
        {
            int period = T(R.CheckInPeriodS);
            foreach (var b in Bodies)
            {
                var p = b.P; if (!IsStaff(p) || missedMarks.Contains(p.Id)) continue;
                var post = PostOf(p, Tick); if (!post.HasValue || Tick < post.Value.from) continue;
                if ((Tick - post.Value.from) % period != 0 && Tick != post.Value.to) continue;
                missedMarks.Add(p.Id);
                var a = ActOf(b); a.MarkMissed = true;
                Note("post.missed", $"{Who(p)} misses his check-in at {Sim.Report.Clock(Tick)}. The post is reported empty.");
                LandAct(a, "mark", "desk", null, post.Value.at, R.AlertMark, "");
            }
            if (GuardDown && BodyOf(GuardP) == null && !missedMarks.Contains(GuardP.Id))
            {
                var post = PostOf(GuardP, Tick);
                if (post.HasValue && Tick >= post.Value.from && (Tick - post.Value.from) % period == 0)
                {
                    missedMarks.Add(GuardP.Id); MissingNotice = Tick;
                    Note("guard.check-in", "The guard misses his check-in. Security now knows the post is empty.");
                    CallSecurity(0, "The empty guard post is reported.", S.Stand(S.GuardPost), landing: true);
                }
            }
        }

        // ---------------------------------------------------------------- a missing expected person (Motive §5.2; Pulse §11)

        /// <summary>Once a second: a person who shares a stop at one place with a lover, family member or workmate (both routines; never
        /// authored), is at it, and finds them down, held or hidden while their routine puts them there, raises the alarm after
        /// suspicion.s (× suspicion.pm by temperament): a missing-person report from where they stand. A turned waiter (not a lover) and a
        /// person being taken report nothing.</summary>
        void Suspicion()
        {
            if (Tick % Hz != 0 || People2 == null) return;
            foreach (var b in Bodies)
            {
                var q = b.P; var qs = StopsOf(q); if (qs == null) continue;
                foreach (var (place, qFrom, qTo, _) in qs)
                {
                    if (Tick < qFrom || Tick >= qTo || place == "beyond") continue;
                    if (!People2.Places.TryGetValue(place, out var placeAt)) continue;
                    if (!People2.Ties.TryGetValue(q.Id, out var ts)) continue;
                    var ids = new List<(string kind, string id)>(); foreach (var (k, id) in ts) if (R.CloseTies.Contains(k)) ids.Add((k, id));
                    ids.Sort((x, y) => string.CompareOrdinal(x.id, y.id));
                    foreach (var (kind, id) in ids)
                    {
                        var p = PersonById(id); if (p == null || p.Down) continue;
                        if (p == ValeP && (Vale == ValeAt.Taking || Vale == ValeAt.WithEnforcer)) continue; // a held person reports nothing
                        string key = p.Id + ">" + q.Id;
                        if (ActOf(b).Noticed.Contains(p.Id)) continue; // they saw it, found him or were told: no suspicion, they know
                        if (!AtPlace(p, place, placeAt, q)) { if (suspicious.TryGetValue(key, out var s0) && s0 >= 0) suspicious.Remove(key); continue; }
                        if (IsCellPerson(p) && kind != "lover") continue;
                        if (!suspicious.TryGetValue(key, out var since)) { suspicious[key] = Tick; continue; }
                        if (since < 0) continue; // raised already
                        int pm = R.SuspicionPm.TryGetValue(p.Temperament, out var v) ? v : 1000;
                        if (Tick - since < T(R.SuspicionS) * pm / 1000) continue;
                        suspicious[key] = -1;
                        var a = ActOf(b);
                        Remember(p, "missed-expected", "", false);
                        Note("person.missed", $"{Who(p)} expected {Who(q)} at {place} and grows suspicious.");
                        var at = PosOfPerson(p) ?? placeAt;
                        if (IsStaff(p) || PosOfPerson(p) == null) AddRep(a, "missing", p.Id, p, placeAt, Tick + T(IsStaff(p) ? R.PostCallS : R.CallS), "radio");
                        else WalkToPhone(a, p, WalkerOf(p), "missing", at, opSpeed, Tick, placeAt, "");
                    }
                }
            }
        }
        /// <summary>Walking somewhere: a person of the crowd between two stops or not on their routine, Vale on his way to the car, the guard
        /// on his walk.</summary>
        bool OnTheWay(Person p)
        {
            if (WalkerOf(p) is Walker w)
            {
                if (w.St.Length > 0) return true;
                for (int i = 0; i < w.Place.Length; i++) if (Tick >= w.From[i] && Tick < w.To[i]) return false;
                return true;
            }
            if (p == ValeP) return Vale == ValeAt.Leaving;
            if (p == GuardP) { var gp = GuardPose(); return gp.HasValue && gp.Value.phase != "door" && gp.Value.phase != "at café"; }
            return false;
        }
        /// <summary>The vehicle bound to a person (Pulse §12.1): the official car is its driver's; where it stands now.</summary>
        Pt? VehicleOf(Person q) => q == Driver ? CarPos() : (Pt?)null;
        /// <summary>The person is at the place: on the street within meet of it or of the expected person's own vehicle (waiting at the car the
        /// driver left), or (indoors) their routine's stop is that place now.</summary>
        bool AtPlace(Person p, string place, Pt placeAt, Person q)
        {
            var at = PosOfPerson(p);
            if (at.HasValue && OnTheWay(p)) return false; // the expectation opens when they arrive (Pulse §11.1): on the way, nobody is waiting yet
            if (at.HasValue) return G.Within(at.Value, placeAt, R.MeetCm) || (VehicleOf(q) is Pt car && G.Within(at.Value, car, R.MeetCm));
            if (WalkerOf(p) is Walker w) for (int i = 0; i < w.Place.Length; i++) if (Tick >= w.From[i] && Tick < w.To[i]) return w.Place[i] == place && !w.Out[i];
            return false;
        }

        // ---------------------------------------------------------------- the step, and the units in a fight

        /// <summary>Brief 013's tick, after the crowd's: bodies, reports, check-in marks, suspicion, the response's units. Nothing runs, and
        /// nothing is hashed, until a takedown, a downed guard or an alarm.</summary>
        void TakedownStep()
        {
            TakeScan(); if (Over) return; // before the early return: an unseen Take opens nothing
            if (Acts.Count == 0 && Units.Count == 0 && !GuardDown && reinforceAt == INF && guardBackAt < 0) return;
            if (guardBackAt >= 0 && Tick >= guardBackAt) GuardBack();
            BodiesStep(); if (Over) return;
            ReportsStep(); if (Over) return;
            CheckIns(); if (Over) return;
            Suspicion(); if (Over) return;
            UnitsStep();
        }
        void GuardBack()
        {
            guardBackAt = -1; if (GuardDown || !GuardHeld || heldPhase != "woken") return;
            var from = heldPos; GuardHeld = false;
            guardRun = new Path(S.Grid.Route(S.Grid.NearestFree(from) ?? from, S.Stand(S.GuardPost)) ?? new[] { from, S.Stand(S.GuardPost) });
            guardRunning = true; LureTick = Tick; GuardAwayUntil = Tick + (int)((guardRun.Length + secSpeed - 1) / secSpeed);
            Note("guard.back-to-post", "The guard walks back to his post.");
        }

        /// <summary>The response's units fire like the city's other gunmen (Pulse §1): before the final fight, at an operative with a weapon
        /// out, who has fired, who holds Vale, or whom security saw do violence.</summary>
        void UnitsCombat()
        {
            foreach (var u in Units)
            {
                if (u.Legacy || u.Gun.Down || !u.OnStreet(Tick)) continue;
                var pose = UnitPose(u); if (!pose.HasValue) continue;
                var uu = u; PreFight(u.Id, u.Gun, pose.Value.pos, p => UnitSees(uu, p, false));
            }
        }

        internal void HashResponse(Hash.H h)
        {
            h.I(robotsSent).I(unitsSent).I(reinforceAt).I(lastAlarm).I(guardBackAt).Z(cordonSent).Z(PatrolAlarmed).Z(sealOnArrival);
            if (SceneHolds.Count > 0) { h.S("holds"); foreach (var x in SceneHolds) { h.I(x.Act).P(x.At); foreach (var d in x.Doors) h.S(d); h.I(-1); } } // rev 4: only runs with a held scene add bytes
            foreach (var d in dests) h.P(d);
            foreach (var u in Units)
            {
                h.S(u.Id).S(u.St).I(u.Launch).I(u.Until).I(u.Arrive).Z(u.Verified).I(u.Act).Z(u.Scene).Z(u.Cordon).P(u.Dest).I(u.NextOpen);
                foreach (var s in u.ToOpen) h.S(s);
                var p = UnitPose(u); h.Z(p.HasValue); if (p.HasValue) h.P(p.Value.pos);
                if (!u.Legacy && !u.Patrol) Hash.HashGun(h, u.Gun);
            }
        }
    }
}
