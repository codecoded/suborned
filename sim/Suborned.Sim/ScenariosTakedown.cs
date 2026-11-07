// Brief 013 B (Keel; Sky 2026-10-16): takedowns, bodies, the people who report them and the response dial, as scenarios on the real block
// and the promoted cast (r016). Each runs twice and replays from its orders (the Theory tests). The numbers are the rulebook's, read
// through the engine; the clocks (the guard's 90 s marks from his shift's start, guard 74's walk, the clerks after the chime) are the
// people file's. Where a scenario fixes something in its start state (a level, a temperament), it says why.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        const string GuardId = "p-records-office-01";
        /// <summary>Beside the door guard, outside his door cone (he faces west over the forecourt): where the Enforcer stands to take him
        /// from the side. 1.2 m south of the public door on its landing, as Waymark's c022 route G (2026-10-19, the door on the west face; was (51.75, -28.8)).</summary>
        static readonly Pt BesideGuard = new Pt(6520, -4110);
        // the service lane's stashes (sim/data/handover.takedown.json: the world's rules put a Dumpster beside the Office's service door)
        const string DumpsterId = "stash/office-service-door/container", ServiceDoorId = "stash/office-service-door/door";
        static readonly Pt Dumpster = new Pt(5975, -1725), ServiceDoor = new Pt(6275, -1875); // their stands (2026-10-19: the service door on the east face; were (60.25, -42.75) and (63.25, -41.25))

        /// <summary>The quiet setup for the guard (Motive §6.1, route G): the Tech loops the entrance camera and reroutes the patrol; the
        /// Enforcer waits beside the door; then, just after the guard's check-in mark, the Enforcer stands beside him.</summary>
        static void GuardQuiet(Run r, bool resplice = true)
        {
            var g = r.G;
            // 2026-10-19 (the door on the west face): the cabinet is 48 m from the Tech's start, so she runs to it as Waymark's c022 route G
            // does, and the loop and the reroute are done before the guard's 2:30 mark; the Enforcer walks to the door meanwhile. She starts
            // the reroute so it lands 5 s before the mark, as the walk to the old cabinet had it: the patrol's 115 s away then covers the
            // takedown and the 4:00 mark (back on its sweep at 4:00 it would take the alarm itself, Pulse §12.2, which N46 doesn't test).
            int enfAt = r.SendTo(Enf, BesideDoor);
            r.To(r.Send(Tech, "cabinet", run: true));
            // Loop v2 (engine 0.31.0, brief 058 §4): the loop runs 2:00 from its landing (it was held from 0:36 by the tether). The Enforcer
            // waits beside the door in the camera's sight, so the Tech keeps it blind by re-splicing: once before the reroute and once after
            // it (+2 T over the held loop), blind without a break from 0:36 to 4:45, over the wait, the takedown after the mark, the 4:00
            // mark and the finds these scenarios test. Every other order keeps its tick.
            r.Do(Tech, "loop", "entrance"); r.Done(Tech);
            int rerouteAt = Math.Max(g.Tick + T(g.R.LoopS), g.NextMarkOf(g.GuardP, g.Tick + T(g.R.RerouteS)) - T(g.R.RerouteS + 5));
            r.To(rerouteAt - T(g.R.LoopS)); r.Do(Tech, "loop", "entrance"); r.Done(Tech); // the first re-splice lands as the reroute is ordered
            r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
            if (resplice) r.Do(Tech, "loop", "entrance"); // the second: lands 20 s on, a fresh 2:00 from there (without it the loop runs out at 3:40)
            r.To(Math.Max(g.Tick, enfAt));
            int mark = g.NextMarkOf(g.GuardP, g.Tick); r.To(mark + 1);
            r.To(r.SendTo(Enf, BesideGuard));
        }
        /// <summary>Loop v2 (engine 0.31.0): the Trail but the loops' (GuardQuiet re-splices while the scenario runs, charged when each lands),
        /// so a check on the takedown's own Trail reads it alone.</summary>
        static int ActTrail(Game g) { int t = g.Trail; foreach (var l in g.Ledger) if (l.what == "trail" && l.cause == "loop") t -= l.delta; return t; }
        /// <summary>The last takedown on `id` (a person id or a role); a clear failure, not a LINQ throw, when there is none.</summary>
        static TdAct ActOn(Run r, string id)
        {
            var a = r.G.Acts.LastOrDefault(x => x.Victim == id || x.P.Role == id);
            if (a != null) return a;
            throw new InvalidOperationException($"no takedown on '{id}' at {Report.Clock(r.G.Tick)}: the order was refused (see the failed takedown check before this) or never issued");
        }
        /// <summary>Free ground within reach of a person standing at `at` (1.2 m round, in a fixed order), the Enforcer can walk to, and
        /// not in `facing`'s cone from `at` (behind or beside him); null if none.</summary>
        static Pt? Beside(Run r, Pt at, Pt? facing = null)
        {
            var g = r.G;
            foreach (var d in new[] { new Pt(0, 120), new Pt(0, -120), new Pt(-120, 0), new Pt(120, 0), new Pt(85, 85), new Pt(85, -85), new Pt(-85, 85), new Pt(-85, -85),
                                      new Pt(0, 200), new Pt(0, -200), new Pt(-200, 0), new Pt(200, 0) }) // engine 0.33.1: 2 m out where a standing car covers 1.2 m round the point (car.footprint)
            {
                var q = at + d; if (!g.Walk.Free(q) || g.Walk.Route(r.Op(Enf).Pos, q) == null) continue; // engine 0.33.1: off a standing car's ground (car.footprint)
                if (facing.HasValue && G.InCone(at, facing.Value, g.R.FacingReachCm, g.R.FacingHalfCm, 0, q)) continue;
                return q;
            }
            return null;
        }
        /// <summary>Meet a walking person from behind: stand beside where their routine puts them at `tick`, a little behind their heading,
        /// and wait there; returns the tick to order the takedown (they are within reach and the Enforcer is out of their facing cone).</summary>
        static int MeetFromBehind(Run r, Walker w, int tick)
        {
            var g = r.G; var at = g.CrowdPos(w, tick)!.Value; var ahead = g.CrowdPos(w, tick + 10)!.Value;
            var h = ahead - at; long len = Math.Max(1, G.Isqrt((long)h.X * h.X + (long)h.Y * h.Y));
            Pt spot = at;
            foreach (int side in new[] { 1, -1 })
            {
                var q = new Pt(at.X - (int)(h.X * 70 / len) - side * (int)(h.Y * 100 / len), at.Y - (int)(h.Y * 70 / len) + side * (int)(h.X * 100 / len));
                if (g.S.Grid.Free(q) && g.S.Grid.Route(r.Op(Enf).Pos, q) != null) { spot = q; break; }
            }
            r.SendTo(Enf, spot);
            return tick;
        }
        /// <summary>Meet a walking person head on (engine 0.28.0, brief 054 §3.3): stand a little ahead of where their routine puts them at
        /// `tick`, to one side of their heading, so they see him coming (aware: they struggle and scream); returns the tick.</summary>
        static int MeetFromFront(Run r, Walker w, int tick)
        {
            var g = r.G; var at = g.CrowdPos(w, tick)!.Value; var ahead = g.CrowdPos(w, tick + 10)!.Value;
            var h = ahead - at; long len = Math.Max(1, G.Isqrt((long)h.X * h.X + (long)h.Y * h.Y));
            Pt spot = at;
            foreach (int side in new[] { 1, -1 })
            {
                var q = new Pt(at.X + (int)(h.X * 90 / len) - side * (int)(h.Y * 60 / len), at.Y + (int)(h.Y * 90 / len) + side * (int)(h.X * 60 / len));
                if (g.S.Grid.Free(q) && g.S.Grid.Route(r.Op(Enf).Pos, q) != null) { spot = q; break; }
            }
            r.SendTo(Enf, spot);
            return tick;
        }
        static Walker WalkerById(Run r, string id) => r.G.Walkers.First(w => w.P.Id == id);
        static Start DriverBlind() { var s = new Start(); s.Turned.Add("driver"); s.Crowd = false; return s; }

        static void TakedownCases()
        {
            Add("N46", "—", "An unseen knockout (brief 013, route G-KO): the camera looped, the patrol rerouted, the guard taken from beside just after his 2:30 check-in mark. Nobody sees it: no Trail, no Standing, no Alert. Engine 0.34.0 (brief 067 L: Pulse's waiting points put guard 74's walk to the official entrance past the public door at about 3:40, a minute sooner): she finds him and radios it before his 4:00 mark; the report lands (Alert 1, an inquiry): the level's Alert-1 robots, on the street its dispatch later. The unit sent to hold the scene brings him round before his 4:00 mark, which he makes; his statement (+4 T, once); a knockout never takes Alert above 1 by itself; no seal at Alert 1. (0.33.1: the mark landed first and the patrol found him at 4:35.)", r =>
            {
                var g = r.G;
                GuardQuiet(r);
                int trail = ActTrail(g), standing = g.Standing;
                var q = g.QuoteTakedown("guard", "knockout");
                r.Eq(q.S, g.R.KoS, "the card: 6 s, from the side (he doesn't see it coming)");
                r.Eq(q.TrailIfFound, g.R.HitTrail + g.R.DownTrail, "the card: +4 T if found");
                r.Is(q.Usd == 0 && q.Aware == "?" && q.People.Count == 0 && q.IfNeverFound == "comes-home" && q.WakesAt == g.Tick + T(g.R.KoS + g.R.WakeS), "the card: $0; 'sees it coming: ?' (nobody has watched the guard's sight); nobody within sight now; wakes m:ss; comes home if never found");
                r.Is(q.Seen.Seen != "yes" && q.AlertIfFound == 1 && q.PoliceInS > g.R.WitnessReactS + g.R.PostCallS + g.DialNow.DispatchS, $"the card: no known watcher sees it (the camera looped, the patrol away), Alert 1 if found, the response ~{q.PoliceInS} s if staff found him now (a staff radio, the dispatch, the walk from the entry)");
                r.Takedown("guard", "knockout");
                r.Is(g.GuardDown && g.BodyById("guard") != null, "he is seized at once: a body, off his post");
                r.Done(Enf);
                var a = ActOn(r, GuardId);
                r.Is(!a.Witnessed && a.Noticed.Count == 0, "nobody saw it");
                r.Eq((ActTrail(g), g.Standing, g.Alert), (trail, standing, 0), "no Trail, no Standing, no Alert");
                r.Eq(g.BodyById("guard")!.WakeAt, g.Tick + T(g.R.WakeS), "he wakes in 120 s");
                int mark = g.NextMarkOf(g.GuardP, g.Tick);
                r.Eq(mark, T(240), "his next mark: 4:00");
                r.Is(r.Until(() => g.Alert >= 1, mark - 1 - g.Tick), $"before the mark a report lands ({Report.Clock(g.Tick)})");
                var first = g.Log.FindLast(l => l.Code == "report.landed");
                r.Is(first.Text.StartsWith("p-records-office-74's report lands: down", StringComparison.Ordinal), $"guard 74, walking past the door to her post, found him and radioed it (engine 0.34.0, brief 067 L): {first.Text}");
                r.Eq(g.Alert, 1, "Alert 1 when it lands (an inquiry)");
                r.Eq(g.Units.Count(u => u.Robot), g.DialNow.A1Robots, $"the level's Alert-1 robots ({g.DialNow.A1Robots} at level {g.SecurityLevel})");
                var launches = g.Units.Where(u => u.Robot).Select(u => u.Launch).OrderBy(x => x).ToList();
                r.Is(launches[0] == g.Tick + T(g.DialNow.DispatchS) && launches.Zip(launches.Skip(1), (x, y) => y - x).All(d => d == g.R.LaunchGapTicks), "the robots on the street the level's dispatch after the landing, one every 1.5 s (a found body also sends a unit to hold the scene)");
                r.To(mark);
                r.Is(!a.MarkMissed && !g.GuardDown, "the unit holding the scene brought him round (his statement) before 4:00: he makes his mark");
                r.Eq(g.Alert, 1, "Alert 1 still");
                r.Until(() => a.ActTrail, T(200));
                r.Is(a.Confirmed || a.Woke, "found by the passing guard and confirmed (one incident), then he wakes and radios it in");
                r.Eq(g.Alert, 1, "a knockout never takes Alert above 1 by itself");
                r.Is(a.ActTrail && ActTrail(g) == trail + g.R.HitTrail + g.R.DownTrail, "his statement: +4 T, once");
                r.Eq(g.Lockdown, int.MaxValue / 4, "no seal at Alert 1");
            });

            Add("N47", "—", "An unseen kill, carried and hidden (route G-kill): the guard killed from beside just after his 2:30 mark, carried at a walk to the Office's service door and put through it (10 s). Nobody sees the act or the carry. His 4:00 mark is missed (Alert 1); the robots find an empty post; at Alert 1 the search opens nothing, and guard 74 walks past the shut door. At the mission's end the guard is a missing person: +2 T, no Menace, his lover's worry, the block's watch and an open case.", r =>
            {
                var g = r.G;
                GuardQuiet(r);
                int trail = ActTrail(g);
                r.Takedown("guard", "kill"); r.Done(Enf);
                var a = ActOn(r, GuardId); var b = g.BodyById("guard")!;
                r.Is(b.Dead && b.Open, "dead, lying at his post");
                r.Body(Enf, "carry", "guard"); r.Done(Enf);
                r.Eq(r.Op(Enf).CarryBody, GuardId, "the Enforcer carries him");
                int at = r.SendTo(Enf, ServiceDoor);
                r.Eq(r.Op(Enf).WalkSpeed, g.R.CellSpeed, "the Enforcer carries at a walk");
                var run = g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "move", Point = ServiceDoor, Run = true });
                r.Eq(run.code, "run-carrying-downed", "no running with a body");
                r.To(at);
                r.Body(Enf, "stash", ServiceDoorId); r.Done(Enf);
                r.Is(!b.Open && b.Stash == ServiceDoorId && r.Op(Enf).CarryBody.Length == 0, "hidden behind the service door");
                r.Is(!a.Witnessed && a.Noticed.Count == 0, "nobody saw the kill or the carry");
                r.Eq((ActTrail(g), g.Alert), (trail, 0), "no Trail, no Alert yet");
                r.To(T(240));
                r.Eq(g.Alert, 1, "the 4:00 mark is missed: Alert 1");
                r.To(T(370)); // brief 059: to 6:10, before the unturned driver drives Vale away at 6:13 (it was 7:00); the debrief test reads the live run
                r.Is(!a.Confirmed && b.Stash.Length > 0, "the door stays shut: at Alert 1 the search opens nothing, and nobody sees behind it");
                r.Eq(g.Alert, 1, "Alert stays 1");
                r.Eq(g.Lockdown, int.MaxValue / 4, "no seal");
                var next = g.BetweenMissions(g.Investigate());
                r.Is(next.Lines.Exists(l => l.Line == "missing" && l.Who == "guard"), "between missions: the guard is missing");
                r.Eq(next.CampaignTrail, g.R.MissingTrail, "+2 T (a file, not a homicide)");
                r.Eq(next.CampaignStanding, 0, "no Menace: nothing was found and nothing seen");
                r.Is(next.Lines.Exists(l => l.Line == "worry" && l.Who == "waitress"), "his lover Inez worries (fear +1)");
                r.Is(next.AreaWatch.Count == 1 && next.AreaWatch[0].guards == g.R.AreaWatchStep && next.AreaWatch[0].missions == g.R.AreaWatchMissions, "the block gets +1 guard and +1 camera for 3 missions");
                r.Is(next.Cases.Count == 1 && next.Cases[0].kind == "missing", "one open case");
                r.Eq(next.NextLevel, g.S.SecurityBase, "one open case: the district's level stands (2 cases raise it 1)");
            });

            Add("N48", "—", "A takedown seen by civilians (Sky merged calls 1, 5): a customer knocked out at a café terrace table in front of the other customers and Inez at the door (Pulse's café fix: the other servers work inside). At sight: +1 witness Trail and one step toward Menace, no Alert. The Loyal and Idealist customers report; when the first report lands, Alert 3 and the act's +4 T; the response goes by the dial (the unit, the robots and, at Alert 3, the cordon; the doors lock at this level). The card listed the witnesses with '?' for unknown temperaments.", r =>
            {
                var g = r.G;
                const string Victim = "p-pylon-row-east-05"; // a Mercenary customer at terrace table 2 from 0:30
                r.To(T(240));
                var seat = g.CrowdPos(WalkerById(r, Victim)) ?? throw new InvalidOperationException($"{Victim} is not on the street at 4:00");
                var spot = Beside(r, seat) ?? throw new InvalidOperationException("no ground beside the terrace table");
                r.To(r.SendTo(Enf, spot));
                var q = g.QuoteTakedown(Victim, "knockout");
                r.Is(q.People.Count >= 2 && q.People.Exists(x => x.verb == "?"), $"the card: {q.People.Count} would see it, verbs '?' until their temperaments are known"); // engine 0.30.0: 2, was 3: p-garden-row-east-02 at (-15.5, -15.1) is behind the café's curved bay front, which the site's occluders lacked until Vela's CafeBay_v2 volumes (kit.collide.json, 2026-10-24: sightOccluders 193 → 245); the bay counts as a civic piece that stops sight (tools/sim/solids.ts). Whether its glazing should let people see through is Motive's and Vela's question
                r.Eq(q.AlertIfSeen, 3, "the card: Alert 3 if seen");
                int trail = g.Trail, standing = g.Standing;
                r.Takedown(Victim, "knockout");
                var a = ActOn(r, Victim);
                r.Is(a.Witnessed, "seen");
                r.Eq(g.Trail, trail + g.R.WitnessTrail, "+1 witness Trail at sight, once");
                r.Eq(g.Standing, standing - g.R.StandingStep, "one step toward Menace at sight");
                r.Eq(g.Alert, 0, "no Alert at sight: a silent act reaches Alert when a report lands");
                var reps = g.Reps.Where(x => x.Act == a.Id && x.P != null).ToList();
                r.Is(reps.Count >= 2 && reps.Count <= g.R.ReportCap, $"{reps.Count} reporters, at most the cap");
                int land = reps.Min(x => x.LandAt);
                r.To(land - 1); r.Eq(g.Alert, 0, "still nothing before the first report lands");
                r.To(land);
                r.Eq(g.Alert, 3, "Alert 3 when the report lands (a takedown seen)");
                // engine 0.19.0 (ai.intent face): the report carries the face its witness saw do it; at Alert 3 the response fires on him,
                // so the lock is read where he stands now (the refusal comes before the door's reach), not after a walk to the door
                r.Is(g.KnownFaces.Contains(Enf), "the report carries the Enforcer's face (ai.intent face)");
                if (g.DialNow.DoorsLock) r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "enter" }).code, "doors-locked", "Alert 3 at this level: the doors are locked");
                r.Is(a.ActTrail, "the act's Trail is charged when a seen takedown's report lands");
                r.Is(g.SecurityArrives == land + T(g.DialNow.DispatchS), "the response unit rolls the level's dispatch after the landing");
                r.Is(g.Units.Count(u => u.Robot) == g.DialNow.A2Robots, "the level's Alert-2 robots");
                r.Is(g.AlarmDestinations.Count >= 1 && G.Within(g.AlarmDestinations[0], a.Scene, g.R.WitnessRangeCm), $"the alarm's places ({g.AlarmDestinations.Count}): the scene first, then where the victim's and the first caller's ties are meant to be (one dispatch, brief 013 D)");
                r.Is(g.Units.Count(u => u.Cordon) == g.DialNow.A3Units * g.R.ResponseOfficers, "Alert 3: the cordon to the mouths");
                r.Eq(g.Lockdown, int.MaxValue / 4, "the seal waits for a confirmation");
                var told = g.Walkers.FirstOrDefault(w => w.P.Memory.Exists(m => m.Act == "told"));
                r.Is(told != null && !told.P.Memory.Exists(m => m.Act == "told" && m.By != reps[0].By && g.Walkers.Exists(x => x.P.Id == m.By && x.P.Memory.Exists(mm => mm.Act == "told"))), "a reporter tells a tie by telephone (facts only, one hop: a told person never retells)");
                r.Until(() => a.Confirmed, T(120));
                r.Is(a.Confirmed && g.Lockdown == g.Tick + T(g.SealS), "a unit confirms him at the scene: the seal starts then");
                // a witness who stays at her stop: the Mercenary customer at table 3 (quiet at Alert 0, so she stays seated)
                const string Stays = "p-garden-row-east-03";
                var ws = WalkerById(r, Stays);
                r.Is(ws.P.Has("saw-takedown") && g.CrowdPos(ws) is Pt ps && G.Within(ps, seat, 600) && g.QuoteTakedown(Stays, "knockout").Aware == "yes", "a witness stays aware of the Enforcer for the rest of the mission (Motive rev 3): taking her now is the seen-coming case, though she sits at a stop");
            });

            // Engine 0.28.0 (brief 055 §7): the official entrance pier (office-official-entrance#0, 0.5 x 0.7 m, 5.65 m tall) stands between the pickup
            // camera and guard 29's post at any lens height, so the camera never sees him; the one person it sees on the street is guard 57,
            // crossing the official entrance from 6:30.9 for 14 s (out/scratch/keel/b056/probe camall). The takedown moves to him there.
            Add("N50", "—", "A takedown seen by the door guard (armed security): guard 73 knocked out from behind as she walks up the forecourt in the guard's cone. The guard engages at once: Alert 2 at sight, the Enforcer is a foe, the incident is confirmed by his own eyes and the seal starts; the response comes by the dial.", r =>
            {
                var g = r.G;
                const string Victim = "p-records-office-73";
                var w = WalkerById(r, Victim);
                int t = Enumerable.Range(T(300), T(30)).FirstOrDefault(x => g.CrowdPos(w, x) is Pt p && g.GuardWatching(p, false) && !G.Within(p, g.S.Stand(g.S.GuardPost), 300));
                if (!r.Is(t > 0, "guard 73 crosses the door guard's cone on her way to the door, between 5:00 and 5:30")) return;
                r.To(t - T(70)); int when = MeetFromBehind(r, w, t); // the Enforcer's walk from the start to the forecourt is ~50 s
                r.Is(r.Arrival(Enf) <= when, $"the Enforcer is in place before she passes ({Report.Clock(r.Arrival(Enf))})");
                r.To(when);
                r.Takedown(Victim, "knockout");
                var a = ActOn(r, Victim);
                r.Is(a.SecuritySaw && g.GuardEngaged, "the guard sees it and engages");
                r.Eq(g.Alert, g.R.DrawSeenSecurity, "Alert 2 at sight (security sees violence)");
                r.Is(a.Confirmed && g.SecurityArrives == g.Tick + T(g.DialNow.DispatchS) && g.Lockdown == g.SecurityArrives + T(g.SealS), "confirmed by his own eyes; the seal starts when the response unit is on the street (the unit's cordon, Motive rev 3)");
                r.Is(g.SecurityArrives == g.Tick + T(g.DialNow.DispatchS), "the response unit rolls the level's dispatch later");
                r.Is(g.MarkedFoe(Enf), "the Enforcer is his foe now, weapon out or not: he fires once the Enforcer is in his weapon's effective reach");
            });

            Add("N51", "—", "An armed target sees it coming (Motive §1.3): the Enforcer stands in front of the door guard, in his cone, and orders the takedown. It fails into a fight: the guard draws and engages (Alert 2 at sight, the drawn-weapon rule), calls it in, and nobody is down. The card said so before the order ('aware: yes', a fight) once his sight was known.", r =>
            {
                var g = r.G;
                var front = AheadOfGuard(r, 120); // within takedown reach, in his cone (2026-10-19: west of the door; was (50.55, -30) north of it)
                r.To(r.SendTo(Enf, front));
                r.Is(g.GuardWatching(r.Op(Enf).Pos), "in his door cone");
                r.Eq(g.QuoteTakedown("guard", "knockout").Aware, "?", "the card: '?' while his sight is unknown");
                r.Takedown("guard", "knockout");
                r.Is(g.Log.Exists(l => l.Code == "takedown.fails"), "the takedown fails");
                r.Is(!g.GuardDown && g.GuardEngaged && g.Bodies.Count == 0, "nobody is down; the guard engages");
                r.Eq(g.Alert, g.R.DrawSeenSecurity, "Alert 2 at sight");
                r.Is(g.SecurityArrives < int.MaxValue / 4, "he calls it in");
                r.Is(r.Op(Enf).Busy == null, "the Enforcer is not held in an act");
            });

            Add("N52", "—", "Carry, restrain and stash (Motive §3.3, §4): the guard knocked out unseen; the Fixer lifts him (3 s) and carries at half the walk (0.7 m/s); the Enforcer ties him (8 s), carries him at a walk to the Office's service door and puts him through it (10 s, off-screen). Hidden and tied, he wakes and makes no sound; guard 74 walks the lane past the door and hears nothing. Between missions he comes home and gives his statement (+4 T).", r =>
            {
                var g = r.G;
                r.SendTo(Fixer, BesideDoor + new Pt(0, -150));
                GuardQuiet(r);
                r.Takedown("guard", "knockout"); r.Done(Enf);
                var b = g.BodyById("guard")!; var a = ActOn(r, GuardId);
                r.SendTo(Enf, BesideDoor);
                r.To(r.SendTo(Fixer, BesideGuard));
                r.Body(Fixer, "carry", "guard"); r.Done(Fixer);
                r.SendTo(Fixer, ServiceDoor);
                r.Eq(r.Op(Fixer).WalkSpeed, g.R.CarryUntrainedSpeed, "the Fixer carries at 0.7 m/s");
                r.Wait(T(4));
                r.Eq(r.Op(Fixer).WalkMm, (long)g.R.CarryUntrainedSpeed * T(4), "4 s: 2.8 m along the way (the Enforcer walks 5.6)");
                r.Body(Fixer, "drop"); r.Done(Fixer);
                r.Is(b.Open && !b.Carrier.HasValue, "laid down");
                r.To(r.SendTo(Enf, Beside(r, b.At) ?? b.At));
                r.Body(Enf, "restrain", "guard"); r.Done(Enf);
                r.Is(b.Restrained, "tied");
                r.Body(Enf, "carry", "guard"); r.Done(Enf);
                r.To(r.SendTo(Enf, ServiceDoor));
                r.Body(Enf, "stash", ServiceDoorId);
                r.Eq(r.Op(Enf).BusyEnd - g.Tick, T(g.R.StashDoorS), "a service door: 10 s");
                r.Done(Enf);
                r.Is(!b.Open && b.Restrained, "hidden and tied");
                r.Is(!a.Witnessed, "nobody saw any of it");
                r.To(b.WakeAt + T(60));
                r.Is(!g.Log.Exists(l => l.Code == "victim.calls-out"), "tied, he makes no sound");
                r.Is(b.P.Down && g.Bodies.Contains(b), "still hidden");
                var next = g.BetweenMissions(g.Investigate());
                r.Is(next.Lines.Exists(l => l.Line == "statement" && l.Who == "guard"), "he comes home and gives his statement");
                r.Is(next.CampaignTrail >= g.R.HitTrail + g.R.DownTrail || a.ActTrail, "+4 T");
            });

            Add("N53", "—", "Hidden but not tied (Motive §3.3): the guard knocked out unseen and stashed in the Dumpster; awake, he calls out every 10 s, heard within 20 m with no line. Engine 0.34.0 (brief 067 L): nobody is within 20 m to hear him now (guard 74 waits at her point 1.2 m farther along the Office's face than the post she stood on, Pulse's note), so his calls go unheard; his 4:00 mark is missed (he is hidden: Alert 1, the level's robots) and the stash's round opens it at 5:22 and finds him: Alert 1, his own report, +4 T. (0.33.1: guard 74 heard him at 4:40 and radioed a noise.)", r =>
            {
                var g = r.G;
                GuardQuiet(r);
                r.Takedown("guard", "knockout"); r.Done(Enf);
                var b = g.BodyById("guard")!; var a = ActOn(r, GuardId);
                r.Body(Enf, "carry", "guard"); r.Done(Enf);
                r.To(r.SendTo(Enf, Dumpster));
                r.Body(Enf, "stash", DumpsterId); r.Done(Enf);
                r.SendTo(Enf, BesideDoor);
                r.To(b.WakeAt + 1);
                r.Is(r.Until(() => g.Log.Exists(l => l.Code == "victim.calls-out"), T(20)), "awake in the Dumpster, he calls out");
                r.Is(a.MarkMissed && g.Log.Exists(l => l.Code == "post.missed"), "hidden, he missed his 4:00 mark");
                r.Is(!r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.Kind == "noise"), T(40)), "nobody within 20 m hears him (engine 0.34.0: guard 74 waits 1.2 m farther along the face, brief 067 L)");
                r.Is(r.Until(() => a.Confirmed || a.Statement || a.Woke, T(240)), "the Dumpster is opened (by the robot sent to it, or by its round at 5:22 if that comes first) and he is out");
                r.Eq(g.Alert, 1, "Alert 1: a knockout never more by itself");
                r.Is(r.Until(() => a.ActTrail, T(30)), "his statement or his own report: +4 T");
            });

            Add("N54", "—", "A body found by routine (Pulse §4.1): the guard killed unseen and left at his post. Engine 0.34.0 (brief 067 L and A): the routine that finds him is guard 74's, walking past the public door to her post at the official entrance at about 3:40 (Pulse's waiting points put her walk a minute sooner); the patrol sweeps the service yard now and never sees the door. She radios a dead body (a person radios: react, then the call): Alert 2; the response unit is sent to the scene; no seal on a report alone; a unit confirms him and the seal starts; +6 T for a kill; a found killing is one step toward Menace though nobody saw it. (0.33.1: his 4:00 mark landed first, Alert 1, and the forecourt patrol found him at 4:34.)", r =>
            {
                var g = r.G;
                // Changed 2026-10-19 (the public door on the west face): nobody's routine passes the door between 4:00 and 5:00 now (guard 74
                // walks the square's south side and the service yard; guard 73, coming on shift, walks up the forecourt at 5:06). The routine
                // that finds him is the patrol's: rerouted before the 2:30 mark, it is back on its sweep at 4:20 and sees the body at the door
                // on its way along the forecourt. A robot rings the desk (robot.report, 4 s) where a person radios (5 s); the rest is as before.
                GuardQuiet(r);
                int trail = ActTrail(g), standing = g.Standing;
                r.Takedown("guard", "kill"); r.Done(Enf);
                r.SendTo(Enf, BesideDoor);
                var a = ActOn(r, GuardId);
                r.Is(!a.Witnessed && a.Noticed.Count == 0, "nobody saw it");
                // engine 0.34.0 (brief 067 L): guard 74 walks past the door at about 3:40 on her way to her waiting point at the official entrance
                r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.By == "p-records-office-74"), T(240) - g.Tick), $"guard 74, walking past the door to her post, finds the body before his 4:00 mark ({Report.Clock(g.Tick)})");
                var rep = g.Reps.FirstOrDefault(x => x.Act == a.Id && x.By == "p-records-office-74");
                if (rep == null) return; // the check above has failed: a clear failure, not a LINQ throw
                r.Eq((rep.Kind, rep.LandAt - g.Tick), ("dead", T(g.R.PostCallS) + T(g.R.WitnessReactS)), "she knows a dead body and radios it: react, then the call");
                r.To(rep.LandAt);
                r.Eq(g.Alert, 2, "a dead body reported: Alert 2");
                r.Is(g.SecurityArrives == g.Tick + T(g.DialNow.DispatchS), "the response unit is sent to the scene");
                r.Eq(g.Lockdown, int.MaxValue / 4, "no seal on a report alone");
                r.Is(r.Until(() => a.Confirmed, T(200)), "a unit confirms him");
                r.Eq(g.Lockdown, g.Tick + T(g.SealS), "the seal starts at the confirmation (Alert 2)");
                r.Eq(ActTrail(g), trail + g.R.HitTrail + g.R.DownTrail + g.R.KillTrail, "the act's Trail: +6 T for a kill");
                r.Eq(g.Standing, standing + g.R.FoundKillStanding, "a found killing: one step toward Menace though nobody saw it (Sky answer 1)");
            });

            Add("N55", "—", "The Dumpster's round (Pulse §4.1; Sky call 9, in the mission only): its phase comes from its id (5:22 for the service lane's, then every 15 minutes, learnable by watching it). The guard is killed unseen and put in the Dumpster at about 2:55; his 4:00 mark is missed (Alert 1, no search at Alert 1). At 5:22 the round empties the Dumpster, finds him and rings the desk: a dead body, Alert 2; a unit confirms him and the seal starts.", r =>
            {
                var g = r.G;
                int period = T(g.R.ServicePeriodS), phase = (int)(Streams.Fnv1a32(DumpsterId) % (uint)period);
                r.Eq(phase, T(322), "the round's phase from the Dumpster's id: 5:22");
                GuardQuiet(r);
                r.Takedown("guard", "kill"); r.Done(Enf);
                var a = ActOn(r, GuardId); var b = g.BodyById("guard")!;
                r.Body(Enf, "carry", "guard"); r.Done(Enf);
                r.To(r.SendTo(Enf, Dumpster));
                r.Body(Enf, "stash", DumpsterId); r.Done(Enf);
                r.SendTo(Enf, BesideDoor);
                r.Is(!a.Witnessed && !b.Open, "unseen, hidden");
                r.To(phase - 1);
                r.Is(!b.Open && g.Alert == 1, "shut until the round (the 4:00 mark: Alert 1, no search)");
                r.To(phase);
                r.Is(g.Log.Exists(l => l.Code == "stash.serviced") && b.Open, "the round opens the Dumpster and finds him");
                var rep = g.Reps.FirstOrDefault(x => x.Act == a.Id && x.By == "round:" + DumpsterId);
                r.Is(rep != null && rep.Kind == "dead" && rep.LandAt == phase + T(g.R.RobotReportS), "and rings the desk (a robot: 4 s)");
                r.To(rep!.LandAt); r.Eq(g.Alert, 2, "a dead body: Alert 2");
                r.Is(r.Until(() => a.Confirmed, T(200)), "a unit confirms him");
                r.Eq(g.Lockdown, g.Tick + T(g.SealS), "the seal starts at the confirmation");
            }, new Start { ValeWaits = true }); // brief 059: Vale waits at his car past 6:13 (test only)

            Add("N56", "—", "A woken victim's report (Motive §3; Pulse §5): guard 73, crossing the square to his post, is knocked out from behind with nobody near and carried to the bins on Rocket Lane's west pavement. He wakes 120 s later, staggers 4 s and radios it in (staff): Alert 1, +4 T, once; he holds what happened to him (attacked: place and time, no face: he never saw the Enforcer), and his fear rises.", r =>
            {
                var g = r.G;
                var w = WalkerById(r, "p-records-office-73");
                int t = T(245); // brief 055: 4:05, was 4:00: with the drawn solids p-pylon-row-east-04 walks into view of the 4:00 takedown (4:02 to 4:20 are clear)
                r.To(t - T(25)); MeetFromBehind(r, w, t); r.To(t);
                int fear = w.P.Sc[Rules.Fear];
                r.Takedown("p-records-office-73", "knockout"); r.Done(Enf);
                var a = ActOn(r, "p-records-office-73"); var b = g.BodyById("p-records-office-73")!;
                r.Is(!a.Witnessed && !a.Aware, "unseen, from behind");
                // out of the way of the robots the world's dock (by the café) will send to his post and his ties at 5:30: the bins on
                // Rocket Lane's west pavement, which nobody passes all mission (Vantage K5)
                r.Body(Enf, "carry", "p-records-office-73"); r.Done(Enf);
                r.To(r.SendTo(Enf, g.S.Grid.NearestFree(new Pt(740, -6180))!.Value));
                r.Body(Enf, "drop"); r.Done(Enf);
                r.Is(!a.Witnessed, "the carry unseen too");
                r.SendTo(Enf, r.Op(Enf).Pos + new Pt(-1500, 0));
                r.To(b.WakeAt);
                r.Is(!a.Landed || a.MarkMissed, "nobody else found him first (or only his missed check-in did)");
                r.Is(g.Log.Exists(l => l.Code == "victim.wakes") && !w.P.Down, "he wakes");
                var rep = g.Reps.FirstOrDefault(x => x.Act == a.Id && x.Kind == "woken");
                r.Is(rep != null && rep.LandAt == g.Tick + T(g.R.StaggerS + g.R.PostCallS), "he staggers 4 s and radios it in (3 s)");
                r.To(rep!.LandAt);
                r.Eq(g.Alert, 1, "Alert 1: a knockout's victim never takes it higher by himself");
                r.Is(a.ActTrail, "+4 T: the assault is on file");
                r.Is(w.P.Has("attacked") && !g.FactsOf(w.P).Contains("enforcer-face"), "he knows where and when, not the face");
                r.Eq(w.P.Sc[Rules.Fear], Math.Min(fear + 1, g.R.ScaleMax[Rules.Fear]), "his fear rises");
            }, new Start { ValeWaits = true }); // brief 059: Vale waits at his car past 6:13 (test only)

            Add("N65", "—", "A witness stopped before the report lands (Pulse §3.2): guard 73 is killed unseen crossing the square at 4:00 and carried to the bins on Rocket Lane's west pavement (nobody passes them); at 5:30, when the café's walkers are home and before the clerks leave, he is carried on to the north edge, unseen. At 6:47 clerk 34 (Loyal, and 73's workmate: he runs, Pulse row 2), walking home ahead of his group, finds the body and runs to the nearest phone (a call box) to report it. The Enforcer meets him on the way and takes him down: his report never lands. The next finder's report lands later: the incident still reaches the desk, by the next earliest.", r =>
            {
                var g = r.G;
                var w73 = WalkerById(r, "p-records-office-73");
                int t = T(245); // brief 055: 4:05, was 4:00, as N56 (p-pylon-row-east-04 now walks into view at 4:00)
                r.To(t - T(25)); MeetFromBehind(r, w73, t); r.To(t);
                r.Takedown("p-records-office-73", "kill"); r.Done(Enf);
                var a = ActOn(r, "p-records-office-73");
                r.Body(Enf, "carry", "p-records-office-73"); r.Done(Enf);
                // Pulse's café fix (c013): the terrace's residents walk home past the Office side 3:30-5:30, on the carry's way north. Wait
                // them out at the bins (Vantage K5, as N56), then carry on at 5:30: they are home, and the clerks have not left yet.
                r.To(r.SendTo(Enf, g.S.Grid.NearestFree(new Pt(740, -6180))!.Value));
                r.Body(Enf, "drop"); r.Done(Enf);
                r.To(T(330));
                r.Body(Enf, "carry", "p-records-office-73"); r.Done(Enf);
                var spot = new Pt(4201, 1066); // the probe's spot: first found by clerk 34 alone (6:47), 28 m from a door
                r.To(r.SendTo(Enf, spot));
                r.Body(Enf, "drop"); r.Done(Enf);
                r.Is(!a.Witnessed, "unseen: the kill and the carry");
                r.SendTo(Enf, new Pt(4600, -700)); // where clerk 34 will pass on his way home
                r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.By == "p-records-office-34"), T(200)), "clerk 34 finds the body and goes to report it");
                var rep = g.Reps.First(x => x.Act == a.Id && x.By == "p-records-office-34");
                r.Is(rep.Phase == "walk" && rep.LandAt > g.Tick + T(10), "he walks to a door: the report is in transit");
                var w34 = WalkerById(r, "p-records-office-34");
                // meet him on his walk: the point of his route the Enforcer reaches first
                Pt meet = w34.Run!.End; int meetT = int.MaxValue;
                for (long mm = 0; mm <= w34.Run.Length; mm += 200)
                {
                    var q = w34.Run.At(mm); int his = w34.RunStart + (int)(mm / w34.Pace);
                    var route = g.S.Grid.Route(r.Op(Enf).Pos, g.S.Grid.NearestFree(q) ?? q); if (route == null) continue;
                    int mine = g.Tick + (int)(new Path(route).Length / g.R.RunSpeed) + T(2);
                    if (mine < his) { meet = q; meetT = his; break; }
                }
                r.Is(meetT < rep.LandAt, "the Enforcer can be on his way before he reaches the door");
                g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "move", Point = g.S.Grid.NearestFree(meet) ?? meet, Run = true });
                r.Until(() => g.CrowdPos(w34) is Pt p34 && G.Within(p34, r.Op(Enf).Pos, g.R.TakedownReachCm), T(60));
                r.Takedown("p-records-office-34", "knockout");
                r.Is(rep.Phase == "stopped" && g.Tick < rep.LandAt, "taken down before his report lands: it is dropped");
                var a34 = ActOn(r, "p-records-office-34");
                r.Is(a34.Aware && g.Log.Exists(l => l.Code == "witness.scream" && l.Text.StartsWith("p-records-office-34")), "he saw the Enforcer coming: he struggles (+3 s) and screams");
                // engine 0.30.0 (the face margin's walks): clerk 26 now sees the knockout and radios it (Alert 3), so the response comes and police-1 finds and
                // confirms the dead guard at 7:25 before any other walker's report lands: the body is still found by someone else, later
                r.Is(r.Until(() => a.Confirmed || g.Reps.Exists(x => x.Act == a.Id && x.Phase == "landed"), T(150)), $"the body is found by the next finder or a unit ({Report.Clock(g.Tick)})");
                var next = g.Reps.FirstOrDefault(x => x.Act == a.Id && x.Phase == "landed");
                r.Is(a.Confirmed || (next != null && next.By != "p-records-office-34"), $"by someone else, later ({(next != null ? next.By : "a unit's confirmation")})");
                // the Mercenary check (Sky merged call 4) moved to N68 on 2026-10-19 (Keel, brief 023): with Pulse's leave stagger no Mercenary passes this body
                r.Is(g.PersonById("p-records-office-34")!.Has("attacked-aware") && g.FactsOf(g.PersonById("p-records-office-34")!).Contains("enforcer-face"), "clerk 34 holds the Enforcer's face");
            }, new Start { ValeWaits = true }); // brief 059: Vale waits at his car past 6:13 (test only)

            Add("N66", "—", "The search opens a stash (Motive §2.3): the patrol rerouted but the entrance camera live, the guard is killed at his post and put in the Dumpster 18 m away. The camera's watcher reports it (Alert 3); the unit finds no body at the post (unfounded) and, at Alert 2 or more, searches 60 s, opening every stash within 30 m, nearest first, 10 s each: the Dumpster. He is found and confirmed; the seal starts then.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, BesideDoor)));
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                r.To(r.SendTo(Enf, BesideGuard));
                r.Takedown("guard", "kill"); r.Done(Enf);
                var a = ActOn(r, GuardId); var b = g.BodyById("guard")!;
                r.Is(g.Reps.Exists(x => x.Act == a.Id && x.By == "camera:entrance"), "the entrance camera records the kill");
                r.Body(Enf, "carry", "guard"); r.Done(Enf);
                r.To(r.SendTo(Enf, Dumpster));
                r.Body(Enf, "stash", DumpsterId); r.Done(Enf);
                r.Is(G.Within(Dumpster, g.S.Stand(g.S.GuardPost), g.R.SearchStashCm), "the Dumpster is within 30 m of the post");
                r.SendTo(Enf, new Pt(6250, -5300));
                r.Is(r.Until(() => g.Alert == 3, T(40)), "the camera's report lands: Alert 3");
                r.Is(r.Until(() => a.Confirmed, T(300)), "the unit searches: it opens the stashes within 30 m, nearest first, and finds him in the Dumpster");
                r.Is(g.Log.Exists(l => l.Code == "report.unfounded") && g.Log.Exists(l => l.Code == "stash.opened" && l.Text.Contains(DumpsterId + ": guard")) && b.Open, "unfounded at the post first; found when the Dumpster is opened");
                r.Eq(g.Lockdown, g.Tick + T(g.SealS), "the seal starts at the confirmation");
            });

            Add("N67", "—", "The Records Square mast (the world's camera, built by its rule SF-9; sim/data/handover.takedown.json): three heads, 70° and 30 m each. It sees East side and Plaza east and Vantage's R7, not the café door behind the café's wall nor the Office side. The Scout learns its sight with a Watch like any camera; the Tech's cabinet loops it like the entrance camera (one loop at a time), and looped it sees nothing.", r =>
            {
                var g = r.G; const string Mast = "camera:mast-z0";
                r.Is(g.WatcherIds().Contains(Mast) && g.S.Cameras.Exists(c => c.Id == "mast-z0" && c.World && c.Facings.Length == 3), "the mast is a watcher, three heads");
                r.Is(g.CameraWatching("mast-z0", r.Stand("d:Plaza east"), false) && g.CameraWatching("mast-z0", r.Stand("d:East side"), false), "it sees Plaza east and East side");
                int r7 = 0, r7seen = 0; for (int dx = -300; dx <= 300; dx += 50) for (int dy = -300; dy <= 300; dy += 50) { var q = new Pt(-1880 + dx, -1630 + dy); if (!g.S.Grid.Free(q)) continue; r7++; if (g.CameraWatching("mast-z0", q, false)) r7seen++; }
                r.Is(r7seen * 2 > r7, $"and most of Vantage's R7 (the café's east end: {r7seen} of {r7} free points within 3 m; the spot itself is under the pole, between two heads)");
                r.Is(!g.CameraWatching("mast-z0", r.Stand("d:Café door"), false) && !g.CameraWatching("mast-z0", r.Stand("d:Office entrance"), false), "not the café door (the wall) nor the Office side (beyond 30 m)");
                var spot = WatchSpot(r, Mast);
                r.To(r.SendTo(Scout, spot)); r.Do(Scout, "watch", Mast); r.Done(Scout);
                r.Is(g.Known(Mast)!.Sight && g.ConeFan(Mast)!.More.Length == 2, "watched: its sight is known, a fan for each head");
                r.To(r.Send(Tech, "cabinet"));
                r.Do(Tech, "loop", "mast-z0"); r.Done(Tech);
                r.Is(g.LoopOn && g.LoopCam == "mast-z0" && !g.CameraWatching("mast-z0", r.Stand("d:Plaza east"), false), "looped: it sees nothing");
                r.Is(g.CameraWatching("entrance", r.Stand("d:Office entrance"), false), "the entrance camera stays live");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "loop", Target = "entrance" }).code, "loop-holds", "one loop at a time");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "loop", Target = "pickup" }).code, "loop-holds", "the pickup camera is on the cabinet too (brief 013 D), but one loop at a time");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "loop", Target = "gate" }).code, "loop-wrong-target", "no camera of that name (unknown = reject)");
            });

            Add("N57", "—", "A missing tie (Sky answer 6; Motive §5.2; Pulse §11): the Tech loops the pickup camera at 4:50 (brief 059: the driver's Call, the camera block, is retired); the Enforcer kills the driver at his post, unseen, and puts him in the Dumpster. Vale comes out at 5:50 to a car with no driver: after suspicion-s (20 s, flat by default) at the place without him he walks to the official door and calls it in. A missing-person report: Alert 1, one incident with the killing; the alarm sends the response to the bay, to the driver's ties and to Vale's. (Start: the driver is a contact; the crowd is off, since guard 29 stands 4.1 m from the driver until 5:30.)", r =>
            {
                var g = r.G;
                r.To(r.Send(Tech, "cabinet"));
                // engine 0.34.0 (brief 067 A): the patrol sweeps the service yard, where the Dumpster stands: the Tech reroutes it so the carry goes
                // unseen (lands 4:25, away to 6:20, back on its sweep before Vale's report lands; at 1.8 m/s the carry is in its view at 5:21)
                r.To(T(220)); r.Do(Tech, "reroute", "patrol");
                r.To(T(240)); r.To(r.SendTo(Enf, g.S.Stand(g.S.CarBlock) + new Pt(-300, -300)));
                r.To(T(290)); r.Do(Tech, "loop", "pickup"); r.Done(Tech);
                r.Is(!g.CameraWatching("pickup", g.DriverPos()!.Value, false), "the pickup camera is looped");
                r.To(r.SendTo(Enf, Beside(r, g.DriverPos()!.Value)!.Value));
                r.Takedown("driver", "kill"); r.Done(Enf);
                var a = ActOn(r, "driver");
                r.Body(Enf, "carry", "driver"); r.Done(Enf);
                r.To(r.SendTo(Enf, Dumpster));
                r.Body(Enf, "stash", DumpsterId); r.Done(Enf);
                r.SendTo(Enf, BesideDoor);
                r.Is(!a.Witnessed && !a.Landed, "unseen, unknown");
                r.To(T(g.ClockValeExits) + 1);
                r.Eq(g.Vale, ValeAt.Leaving, "5:50: Vale comes out to his car (the city doesn't know)");
                r.Is(r.Until(() => g.Log.Exists(l => l.Code == "person.missed"), T(60)), "he grows suspicious");
                r.Eq(g.Tick, T(g.ClockValeAtCar) + T(g.R.SuspicionS), "20 s after he reached the car (6:10): 6:30");
                var rep = g.Reps.FirstOrDefault(x => x.Act == a.Id && x.Kind == "missing");
                if (!r.Is(rep != null, $"a missing-person report is made ({string.Join(", ", g.Reps.Where(x => x.Act == a.Id).Select(x => x.Kind + ":" + x.By))})")) return;
                r.Is(rep!.P == g.ValeP, "Vale calls it in");
                r.To(rep.LandAt);
                r.Eq(g.Alert, 1, "a missing-person report: Alert 1");
                var d = g.AlarmDestinations;
                r.Is(d.Count >= 1 && G.Within(d[0], g.People2!.Places["office-bay"], g.R.WitnessRangeCm), $"the alarm's places ({d.Count}): the bay first, where the driver should be (his car stands by it; his and Vale's ties are indoors or off the block, the crowd being off)");
                var robots = g.Units.Where(u => u.Robot).ToList();
                r.Is(robots.Count >= 1 && robots.Count <= g.DialNow.A1Robots && robots.All(u => d.Any(x => G.Within(x, u.Dest, 100)) && G.Within(u.From, u.Dest, g.R.RobotReachCm)), $"robots to the places, each only within 60 m of where it starts ({robots.Count} of the level's {g.DialNow.A1Robots}: the dock by the café is too far from the bay)");
                r.Is(robots.Exists(u => u.Patrol && G.Within(u.Dest, d[0], 100)), "the patrol, near enough, takes the bay");
                r.Is(g.ValeP.Has("missed-expected") && g.ValeP.Has("reported"), "Vale remembers it, and that he reported it (a volunteer when questioned)");
                r.Eq(g.Question(g.ValeP).Outcome, "volunteers", "questioned, he volunteers what he holds (no Talker test)");
            }, DriverBlind());

            Add("N57b", "—", "A held person reports nothing (Motive §5.2): as N57, but the Enforcer takes Vale at the car the moment he arrives, behind the blinded camera; suspicion never fires.", r =>
            {
                var g = r.G;
                r.Send(Tech, "cabinet");
                r.SendTo(Scout, TipSpot(r)); r.To(T(g.S.RivalsArrive) + 1); /* brief 096: the false tip is retired; no crew here (NoCrew) */ // the false tip (route B), so the rivals don't reach Vale first
                r.To(T(220)); r.Do(Tech, "reroute", "patrol"); // engine 0.34.0 (brief 067 A): as N57, the yard patrol away over the carry to the Dumpster
                r.To(T(240)); r.To(r.SendTo(Enf, g.S.Stand(g.S.CarBlock) + new Pt(-300, -300)));
                r.To(T(290)); r.Do(Tech, "loop", "pickup"); r.Done(Tech); // brief 059: the camera block is retired; the loop blinds it to 7:10
                r.To(r.SendTo(Enf, Beside(r, g.DriverPos()!.Value)!.Value));
                r.Takedown("driver", "kill"); r.Done(Enf);
                r.Body(Enf, "carry", "driver"); r.Done(Enf);
                r.To(r.SendTo(Enf, Dumpster));
                r.Body(Enf, "stash", DumpsterId); r.Done(Enf);
                r.To(r.SendTo(Enf, Beside(r, g.CarPos())!.Value));
                r.To(T(g.ClockValeAtCar));
                r.Do(Enf, "take", Vale);
                r.Wait(T(g.R.SuspicionS + 5));
                r.Is(!g.Over && g.Vale == ValeAt.Taking || g.Vale == ValeAt.WithEnforcer, "Vale is being taken");
                r.Is(!g.Log.Exists(l => l.Code == "person.missed"), "being taken, Vale reports nothing");
                r.Is(!g.Acts.Exists(x => x.Take), "an unseen Take opens no incident (brief 013 D: a Take is seen only by whoever sees it)");
            }, NoCrew(DriverBlind()));

            Add("N59", "—", "An alarm beyond robot reach (Pulse §12.2; Motive rev 3): the Tech runs to the cabinet, the guard is killed unseen just after the reroute lands (2:01) and put through the service door before his 2:30 mark. The mark lands (Alert 1). The patrol is away and the world's one dock (by the café) is more than 60 m from the post, so the level's robots go only where they can: to where his lover is meant to be. Nobody checks the post: nothing is confirmed, Alert stays 1, no seal, the door stays shut. (A report found unfounded at the scene: N66.)", r =>
            {
                var g = r.G;
                g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "move", To = "cabinet", Run = true }); r.SendTo(Enf, BesideDoor);
                r.Until(() => r.Op(Tech).Walk == null, T(60));
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech); r.Do(Tech, "loop", "entrance"); r.Done(Tech); /* Loop v2 (engine 0.31.0, brief 058 §4): the 2:00 loop is planted after the Reroute */
                r.To(r.SendTo(Enf, BesideGuard));
                r.Is(g.NextMarkOf(g.GuardP, g.Tick) == T(150), "before his 2:30 mark");
                r.Takedown("guard", "kill"); r.Done(Enf);
                r.Body(Enf, "carry", "guard"); r.Done(Enf);
                r.To(r.SendTo(Enf, ServiceDoor));
                r.Body(Enf, "stash", ServiceDoorId); r.Done(Enf);
                var a = ActOn(r, GuardId);
                r.Is(!a.Witnessed, "unseen");
                r.SendTo(Enf, BesideDoor);
                r.To(T(150)); r.Eq(g.Alert, 1, "the 2:30 mark is missed: Alert 1");
                var post = g.S.Stand(g.S.GuardPost); var robots = g.Units.Where(u => u.Robot).ToList();
                r.Is(robots.Count > 0 && robots.All(u => !u.Patrol && G.Within(u.From, u.Dest, g.R.RobotReachCm) && !G.Within(u.Dest, post, g.R.WitnessRangeCm)), "robots only within 60 m of their dock: to his lover's place, not his post");
                r.Is(G.Within(g.CrowdPosOrStand(g.Inez), robots[0].Dest, g.R.WitnessRangeCm), "the first robot to Inez, at the café (his lover)");
                r.To(T(330));
                r.Is(!a.Confirmed && g.Alert == 1 && g.Lockdown == int.MaxValue / 4, "nobody checks the post: nothing confirmed, Alert 1, no seal");
                r.Is(g.BodyById("guard")!.Stash.Length > 0, "the service door stays shut");
            });

            Add("N60", "—", "The response dial at level 0 (Sky 2026-10-16: the swarm is data; Motive §5.3): the guard killed unseen and left at his post. Engine 0.34.0 (brief 067 L: Pulse's waiting points put guard 74's walk to the official entrance past the public door at about 3:40, a minute sooner): she finds the body before his 4:00 mark and radios it (Alert 2): the response unit the level's dispatch after the landing, its units and robots (the Alert-2 count), and a reinforcement after the level's reinforce seconds where it has one. (0.33.1: his 4:00 mark landed first, Alert 1 and the level's Alert-1 robots; N46 keeps that step.)", r =>
            {
                var g = r.G; var lv = g.R.Dial[0];
                r.Eq(g.SecurityLevel, 0, "the level");
                GuardQuiet(r);
                r.Takedown("guard", "kill"); r.Done(Enf);
                r.SendTo(Enf, BesideDoor);
                var a = ActOn(r, GuardId);
                r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.Kind == "dead" && x.Phase == "landed"), T(240) - g.Tick), $"the dead body is reported before his 4:00 mark ({Report.Clock(g.Tick)})");
                r.Is(g.Log.Exists(l => l.Code == "report.landed" && l.Text.StartsWith("p-records-office-74's report lands: dead", StringComparison.Ordinal)), "by guard 74, walking past the door to her post (engine 0.34.0, brief 067 L)");
                r.Eq(g.Alert, 2, "Alert 2");
                int landed = g.Tick;
                r.Eq(g.SecurityArrives, landed + T(lv.DispatchS), "the response unit: the level's dispatch after the landing");
                int officers = g.Units.Count(u => !u.Robot && !u.Cordon);
                r.Eq(officers, lv.A2Units * g.R.ResponseOfficers, $"Alert 2: {lv.A2Units} unit(s) of {g.R.ResponseOfficers}");
                r.Eq(g.Units.Count(u => u.Robot), lv.A2Robots, "robots: the Alert-2 count (engine 0.34.0: no Alert-1 robots went out before it; those out stay out, N46)");
                if (lv.ReinforceS > 0 && lv.UnitCap > lv.A2Units)
                {
                    r.To(landed + T(lv.DispatchS) + T(lv.ReinforceS));
                    r.Eq(g.Units.Count(u => !u.Robot && !u.Cordon), (lv.A2Units + 1) * g.R.ResponseOfficers, $"one more unit {lv.ReinforceS} s after the first rolled");
                }
            }, Level(0));

            Add("N61", "—", "The response dial at level 1 (Sky 2026-10-16: the swarm is data; Motive §5.3): the guard killed unseen and left at his post. Engine 0.34.0 (brief 067 L: Pulse's waiting points put guard 74's walk to the official entrance past the public door at about 3:40, a minute sooner): she finds the body before his 4:00 mark and radios it (Alert 2): the response unit the level's dispatch after the landing, its units and robots (the Alert-2 count), and a reinforcement after the level's reinforce seconds where it has one. (0.33.1: his 4:00 mark landed first, Alert 1 and the level's Alert-1 robots; N46 keeps that step.)", r =>
            {
                var g = r.G; var lv = g.R.Dial[1];
                r.Eq(g.SecurityLevel, 1, "the level");
                GuardQuiet(r);
                r.Takedown("guard", "kill"); r.Done(Enf);
                r.SendTo(Enf, BesideDoor);
                var a = ActOn(r, GuardId);
                r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.Kind == "dead" && x.Phase == "landed"), T(240) - g.Tick), $"the dead body is reported before his 4:00 mark ({Report.Clock(g.Tick)})");
                r.Is(g.Log.Exists(l => l.Code == "report.landed" && l.Text.StartsWith("p-records-office-74's report lands: dead", StringComparison.Ordinal)), "by guard 74, walking past the door to her post (engine 0.34.0, brief 067 L)");
                r.Eq(g.Alert, 2, "Alert 2");
                int landed = g.Tick;
                r.Eq(g.SecurityArrives, landed + T(lv.DispatchS), "the response unit: the level's dispatch after the landing");
                int officers = g.Units.Count(u => !u.Robot && !u.Cordon);
                r.Eq(officers, lv.A2Units * g.R.ResponseOfficers, $"Alert 2: {lv.A2Units} unit(s) of {g.R.ResponseOfficers}");
                r.Eq(g.Units.Count(u => u.Robot), lv.A2Robots, "robots: the Alert-2 count (engine 0.34.0: no Alert-1 robots went out before it; those out stay out, N46)");
                if (lv.ReinforceS > 0 && lv.UnitCap > lv.A2Units)
                {
                    r.To(landed + T(lv.DispatchS) + T(lv.ReinforceS));
                    r.Eq(g.Units.Count(u => !u.Robot && !u.Cordon), (lv.A2Units + 1) * g.R.ResponseOfficers, $"one more unit {lv.ReinforceS} s after the first rolled");
                }
            }, Level(1));

            Add("N62", "—", "The response dial at level 2 (Sky 2026-10-16: the swarm is data; Motive §5.3): the guard killed unseen and left at his post. Engine 0.34.0 (brief 067 L: Pulse's waiting points put guard 74's walk to the official entrance past the public door at about 3:40, a minute sooner): she finds the body before his 4:00 mark and radios it (Alert 2): the response unit the level's dispatch after the landing, its units and robots (the Alert-2 count), and a reinforcement after the level's reinforce seconds where it has one. (0.33.1: his 4:00 mark landed first, Alert 1 and the level's Alert-1 robots; N46 keeps that step.)", r =>
            {
                var g = r.G; var lv = g.R.Dial[2];
                r.Eq(g.SecurityLevel, 2, "the level");
                GuardQuiet(r);
                r.Takedown("guard", "kill"); r.Done(Enf);
                r.SendTo(Enf, BesideDoor);
                var a = ActOn(r, GuardId);
                r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.Kind == "dead" && x.Phase == "landed"), T(240) - g.Tick), $"the dead body is reported before his 4:00 mark ({Report.Clock(g.Tick)})");
                r.Is(g.Log.Exists(l => l.Code == "report.landed" && l.Text.StartsWith("p-records-office-74's report lands: dead", StringComparison.Ordinal)), "by guard 74, walking past the door to her post (engine 0.34.0, brief 067 L)");
                r.Eq(g.Alert, 2, "Alert 2");
                int landed = g.Tick;
                r.Eq(g.SecurityArrives, landed + T(lv.DispatchS), "the response unit: the level's dispatch after the landing");
                int officers = g.Units.Count(u => !u.Robot && !u.Cordon);
                r.Eq(officers, lv.A2Units * g.R.ResponseOfficers, $"Alert 2: {lv.A2Units} unit(s) of {g.R.ResponseOfficers}");
                r.Eq(g.Units.Count(u => u.Robot), lv.A2Robots, "robots: the Alert-2 count (engine 0.34.0: no Alert-1 robots went out before it; those out stay out, N46)");
                if (lv.ReinforceS > 0 && lv.UnitCap > lv.A2Units)
                {
                    r.To(landed + T(lv.DispatchS) + T(lv.ReinforceS));
                    r.Eq(g.Units.Count(u => !u.Robot && !u.Cordon), (lv.A2Units + 1) * g.R.ResponseOfficers, $"one more unit {lv.ReinforceS} s after the first rolled");
                }
            }, Level(2));

            Add("N63", "—", "The response dial at level 3 (Sky 2026-10-16: the swarm is data; Motive §5.3): the guard killed unseen and left at his post. Engine 0.34.0 (brief 067 L: Pulse's waiting points put guard 74's walk to the official entrance past the public door at about 3:40, a minute sooner): she finds the body before his 4:00 mark and radios it (Alert 2): the response unit the level's dispatch after the landing, its units and robots (the Alert-2 count), and a reinforcement after the level's reinforce seconds where it has one. (0.33.1: his 4:00 mark landed first, Alert 1 and the level's Alert-1 robots; N46 keeps that step.)", r =>
            {
                var g = r.G; var lv = g.R.Dial[3];
                r.Eq(g.SecurityLevel, 3, "the level");
                GuardQuiet(r);
                r.Takedown("guard", "kill"); r.Done(Enf);
                r.SendTo(Enf, BesideDoor);
                var a = ActOn(r, GuardId);
                r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.Kind == "dead" && x.Phase == "landed"), T(240) - g.Tick), $"the dead body is reported before his 4:00 mark ({Report.Clock(g.Tick)})");
                r.Is(g.Log.Exists(l => l.Code == "report.landed" && l.Text.StartsWith("p-records-office-74's report lands: dead", StringComparison.Ordinal)), "by guard 74, walking past the door to her post (engine 0.34.0, brief 067 L)");
                r.Eq(g.Alert, 2, "Alert 2");
                int landed = g.Tick;
                r.Eq(g.SecurityArrives, landed + T(lv.DispatchS), "the response unit: the level's dispatch after the landing");
                int officers = g.Units.Count(u => !u.Robot && !u.Cordon);
                r.Eq(officers, lv.A2Units * g.R.ResponseOfficers, $"Alert 2: {lv.A2Units} unit(s) of {g.R.ResponseOfficers}");
                r.Eq(g.Units.Count(u => u.Robot), lv.A2Robots, "robots: the Alert-2 count (engine 0.34.0: no Alert-1 robots went out before it; those out stay out, N46)");
                if (lv.ReinforceS > 0 && lv.UnitCap > lv.A2Units)
                {
                    r.To(landed + T(lv.DispatchS) + T(lv.ReinforceS));
                    r.Eq(g.Units.Count(u => !u.Robot && !u.Cordon), (lv.A2Units + 1) * g.R.ResponseOfficers, $"one more unit {lv.ReinforceS} s after the first rolled");
                }
            }, Level(3));

            Add("N64", "—", "The response dial at level 4 (Sky 2026-10-16: the swarm is data; Motive §5.3): the guard killed unseen and left at his post. Engine 0.34.0 (brief 067 L: Pulse's waiting points put guard 74's walk to the official entrance past the public door at about 3:40, a minute sooner): she finds the body before his 4:00 mark and radios it (Alert 2): the response unit the level's dispatch after the landing, its units and robots (the Alert-2 count), and a reinforcement after the level's reinforce seconds where it has one. (0.33.1: his 4:00 mark landed first, Alert 1 and the level's Alert-1 robots; N46 keeps that step.)", r =>
            {
                var g = r.G; var lv = g.R.Dial[4];
                r.Eq(g.SecurityLevel, 4, "the level");
                GuardQuiet(r);
                r.Takedown("guard", "kill"); r.Done(Enf);
                r.SendTo(Enf, BesideDoor);
                var a = ActOn(r, GuardId);
                r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.Kind == "dead" && x.Phase == "landed"), T(240) - g.Tick), $"the dead body is reported before his 4:00 mark ({Report.Clock(g.Tick)})");
                r.Is(g.Log.Exists(l => l.Code == "report.landed" && l.Text.StartsWith("p-records-office-74's report lands: dead", StringComparison.Ordinal)), "by guard 74, walking past the door to her post (engine 0.34.0, brief 067 L)");
                r.Eq(g.Alert, 2, "Alert 2");
                int landed = g.Tick;
                r.Eq(g.SecurityArrives, landed + T(lv.DispatchS), "the response unit: the level's dispatch after the landing");
                int officers = g.Units.Count(u => !u.Robot && !u.Cordon);
                r.Eq(officers, lv.A2Units * g.R.ResponseOfficers, $"Alert 2: {lv.A2Units} unit(s) of {g.R.ResponseOfficers}");
                r.Eq(g.Units.Count(u => u.Robot), lv.A2Robots, "robots: the Alert-2 count (engine 0.34.0: no Alert-1 robots went out before it; those out stay out, N46)");
                if (lv.ReinforceS > 0 && lv.UnitCap > lv.A2Units)
                {
                    r.To(landed + T(lv.DispatchS) + T(lv.ReinforceS));
                    r.Eq(g.Units.Count(u => !u.Robot && !u.Cordon), (lv.A2Units + 1) * g.R.ResponseOfficers, $"one more unit {lv.ReinforceS} s after the first rolled");
                }
            }, Level(4));
        }
    }
}
