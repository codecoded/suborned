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
        /// <summary>Beside the door guard, outside his door cone (he faces the square): where the Enforcer stands to take him from the side.</summary>
        static readonly Pt BesideGuard = new Pt(5175, -2880);
        // the service lane's stashes (sim/data/handover.takedown.json: the world's rules put a Dumpster beside the Office's service door)
        const string DumpsterId = "stash/office-service-door/container", ServiceDoorId = "stash/office-service-door/door";
        static readonly Pt Dumpster = new Pt(6025, -4275), ServiceDoor = new Pt(6325, -4125); // their stands

        /// <summary>The quiet setup for the guard (Motive §6.1, route G): the Tech loops the entrance camera and reroutes the patrol; the
        /// Enforcer waits beside the door; then, just after the guard's check-in mark, the Enforcer stands beside him.</summary>
        static void GuardQuiet(Run r)
        {
            var g = r.G;
            r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, BesideDoor)));
            r.Do(Tech, "loop", "entrance"); r.Done(Tech); r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
            int mark = g.NextMarkOf(g.GuardP, g.Tick); r.To(mark + 1);
            r.To(r.SendTo(Enf, BesideGuard));
        }
        static TdAct ActOn(Run r, string id) => r.G.Acts.Last(a => a.Victim == id || a.P.Role == id);
        /// <summary>Free ground within reach of a person standing at `at` (1.2 m round, in a fixed order), the Enforcer can walk to, and
        /// not in `facing`'s cone from `at` (behind or beside him); null if none.</summary>
        static Pt? Beside(Run r, Pt at, Pt? facing = null)
        {
            var g = r.G;
            foreach (var d in new[] { new Pt(0, 120), new Pt(0, -120), new Pt(-120, 0), new Pt(120, 0), new Pt(85, 85), new Pt(85, -85), new Pt(-85, 85), new Pt(-85, -85) })
            {
                var q = at + d; if (!g.S.Grid.Free(q) || g.S.Grid.Route(r.Op(Enf).Pos, q) == null) continue;
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
        static Walker WalkerById(Run r, string id) => r.G.Walkers.First(w => w.P.Id == id);
        static Start DriverBlind() { var s = new Start(); s.Turned.Add("driver"); s.Crowd = false; return s; }

        static void TakedownCases()
        {
            Add("N46", "—", "An unseen knockout (brief 013, route G-KO): the camera looped, the patrol rerouted, the guard taken from beside just after his 2:30 check-in mark. Nobody sees it: no Trail, no Standing, no Alert. His next mark (4:00) is missed: the report lands at the mark, Alert 1, and the level's Alert-1 robots go; a knockout never takes Alert above 1 by itself; found and brought round, he gives his statement (+4 T), and no seal starts.", r =>
            {
                var g = r.G;
                GuardQuiet(r);
                int trail = g.Trail, standing = g.Standing;
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
                r.Eq((g.Trail, g.Standing, g.Alert), (trail, standing, 0), "no Trail, no Standing, no Alert");
                r.Eq(g.BodyById("guard")!.WakeAt, g.Tick + T(g.R.WakeS), "he wakes in 120 s");
                int mark = g.NextMarkOf(g.GuardP, g.Tick);
                r.Eq(mark, T(240), "his next mark: 4:00");
                r.To(mark - 1); r.Eq(g.Alert, 0, "quiet until the mark");
                r.To(mark);
                r.Is(a.MarkMissed && g.Log.Exists(l => l.Code == "post.missed"), "the missed mark is reported");
                r.Eq(g.Alert, 1, "Alert 1 when it lands (an inquiry)");
                r.Eq(g.Units.Count(u => u.Robot), g.DialNow.A1Robots, $"the level's Alert-1 robots ({g.DialNow.A1Robots} at level {g.SecurityLevel})");
                var launches = g.Units.Select(u => u.Launch).OrderBy(x => x).ToList();
                r.Is(launches[0] == g.Tick + T(g.DialNow.DispatchS) && launches.Zip(launches.Skip(1), (x, y) => y - x).All(d => d == g.R.LaunchGapTicks), "on the street the level's dispatch after the landing, one every 1.5 s");
                r.Until(() => a.ActTrail, T(200));
                r.Is(a.Confirmed || a.Woke, "found by the passing guard and the patrol (one incident), then he wakes and radios it in");
                r.Eq(g.Alert, 1, "a knockout never takes Alert above 1 by itself");
                r.Is(a.ActTrail && g.Trail == trail + g.R.HitTrail + g.R.DownTrail, "his statement: +4 T, once");
                r.Eq(g.Lockdown, int.MaxValue / 4, "no seal at Alert 1");
            });

            Add("N47", "—", "An unseen kill, carried and hidden (route G-kill): the guard killed from beside just after his 2:30 mark, carried at a walk to the Office's service door and put through it (10 s). Nobody sees the act or the carry. His 4:00 mark is missed (Alert 1); the robots find an empty post; at Alert 1 the search opens nothing, and guard 74 walks past the shut door. At the mission's end the guard is a missing person: +2 T, no Menace, his lover's worry, the block's watch and an open case.", r =>
            {
                var g = r.G;
                GuardQuiet(r);
                int trail = g.Trail;
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
                r.Eq((g.Trail, g.Alert), (trail, 0), "no Trail, no Alert yet");
                r.To(T(240));
                r.Eq(g.Alert, 1, "the 4:00 mark is missed: Alert 1");
                r.To(T(420));
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

            Add("N48", "—", "A takedown seen by civilians (Sky merged calls 1, 5): a server knocked out at the café door in front of the other servers and Inez. At sight: +1 witness Trail and one step toward Menace, no Alert. The Loyal servers report from the café's door; when the first report lands, Alert 3 and the act's +4 T; the response goes by the dial (the unit, the robots and, at Alert 3, the cordon; the doors lock at this level). The card listed the witnesses with '?' for unknown temperaments.", r =>
            {
                var g = r.G;
                var cafe = r.At("d:Café door");
                var spot = Beside(r, cafe) ?? throw new InvalidOperationException("no ground beside the café door");
                r.To(T(240)); r.To(r.SendTo(Enf, spot));
                var q = g.QuoteTakedown("p-cafe-12", "knockout");
                r.Is(q.People.Count >= 3 && q.People.Exists(x => x.verb == "?"), $"the card: {q.People.Count} would see it, verbs '?' until their temperaments are known");
                r.Eq(q.AlertIfSeen, 3, "the card: Alert 3 if seen");
                int trail = g.Trail, standing = g.Standing;
                r.Takedown("p-cafe-12", "knockout");
                var a = ActOn(r, "p-cafe-12");
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
                r.Is(a.ActTrail, "the act's Trail is charged when a seen takedown's report lands");
                r.Is(g.SecurityArrives == land + T(g.DialNow.DispatchS), "the response unit rolls the level's dispatch after the landing");
                r.Is(g.Units.Count(u => u.Robot) == g.DialNow.A2Robots, "the level's Alert-2 robots");
                r.Is(g.AlarmDestinations.Count >= 1 && G.Within(g.AlarmDestinations[0], a.Scene, g.R.WitnessRangeCm), $"the alarm's places ({g.AlarmDestinations.Count}): the scene first, then where the victim's and the first caller's ties are meant to be (the servers' ties stand at the café: one place; one dispatch, brief 013 D)");
                r.Is(g.Units.Count(u => u.Cordon) == g.DialNow.A3Units * g.R.ResponseOfficers, "Alert 3: the cordon to the mouths");
                r.Eq(g.Lockdown, int.MaxValue / 4, "the seal waits for a confirmation");
                var told = g.Walkers.FirstOrDefault(w => w.P.Memory.Exists(m => m.Act == "told"));
                r.Is(told != null && !told.P.Memory.Exists(m => m.Act == "told" && m.By != reps[0].By && g.Walkers.Exists(x => x.P.Id == m.By && x.P.Memory.Exists(mm => mm.Act == "told"))), "a reporter tells a tie by telephone (facts only, one hop: a told person never retells)");
                r.Until(() => a.Confirmed, T(120));
                r.Is(a.Confirmed && g.Lockdown == g.Tick + T(g.SealS), "a unit confirms him at the scene: the seal starts then");
                var w15 = WalkerById(r, "p-cafe-15");
                r.Is(w15.P.Has("saw-takedown") && g.CrowdPos(w15) is Pt p15 && G.Within(p15, cafe, 300) && g.QuoteTakedown("p-cafe-15", "knockout").Aware == "yes", "a witness stays aware of the Enforcer for the rest of the mission (Motive rev 3): taking her now is the seen-coming case, though she stands at a stop");
                if (g.DialNow.DoorsLock) { r.To(r.Send(Enf, "d:Office entrance")); r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "enter" }).code, "doors-locked", "Alert 3 at this level: the doors are locked"); }
            });

            Add("N49", "—", "A takedown seen by a camera: guard 29 at the official entrance knocked out under the pickup camera. The driver beside him is a Mercenary and keeps quiet at Alert 0; the camera's watcher reads the screen at its next scan (every 20 s, phase from the camera's id) and calls it in 3 s later: Alert 3.", r =>
            {
                var g = r.G;
                var w = WalkerById(r, "p-records-office-29"); var at = g.CrowdPos(w)!.Value;
                var spot = Beside(r, at) ?? throw new InvalidOperationException("no ground beside guard 29");
                r.To(r.SendTo(Enf, spot));
                r.Takedown("p-records-office-29", "knockout");
                var a = ActOn(r, "p-records-office-29");
                var cam = g.Reps.FirstOrDefault(x => x.Act == a.Id && x.By == "camera:pickup");
                r.Is(cam != null, "the pickup camera records it");
                int period = T(g.R.CameraScanS), phase = (int)(Streams.Fnv1a32("camera:pickup") % (uint)period);
                r.Is(cam != null && (cam.LandAt - T(g.R.PostCallS) - phase) % period == 0 && cam.LandAt - T(g.R.PostCallS) >= a.Tick && cam.LandAt - T(g.R.PostCallS) < a.Tick + period, "its watcher reads the screen at the next scan, then calls it in");
                r.Is(a.Noticed.Contains(g.Driver.Id) && !g.Reps.Exists(x => x.P == g.Driver), "the driver saw it and keeps quiet (a Mercenary at Alert 0)");
                r.Is(g.Driver.Has("saw-takedown") && g.FactsOf(g.Driver).Contains("enforcer-face"), "he holds the Enforcer's face (Sky D3)");
                r.To(cam!.LandAt);
                r.Eq(g.Alert, 3, "the camera's report lands: Alert 3");
            });

            Add("N50", "—", "A takedown seen by the door guard (armed security): guard 74 knocked out from behind as he crosses the square in the guard's cone. The guard engages at once: Alert 2 at sight, the Enforcer is a foe, the incident is confirmed by his own eyes and the seal starts; the response comes by the dial.", r =>
            {
                var g = r.G;
                var w = WalkerById(r, "p-records-office-74");
                int t = Enumerable.Range(T(225), T(30)).First(x => g.CrowdPos(w, x) is Pt p && g.GuardWatching(p, false));
                r.To(t - T(20)); int when = MeetFromBehind(r, w, t);
                r.To(when);
                r.Takedown("p-records-office-74", "knockout");
                var a = ActOn(r, "p-records-office-74");
                r.Is(a.SecuritySaw && g.GuardEngaged, "the guard sees it and engages");
                r.Eq(g.Alert, g.R.DrawSeenSecurity, "Alert 2 at sight (security sees violence)");
                r.Is(a.Confirmed && g.SecurityArrives == g.Tick + T(g.DialNow.DispatchS) && g.Lockdown == g.SecurityArrives + T(g.SealS), "confirmed by his own eyes; the seal starts when the response unit is on the street (the unit's cordon, Motive rev 3)");
                r.Is(g.SecurityArrives == g.Tick + T(g.DialNow.DispatchS), "the response unit rolls the level's dispatch later");
                r.Is(g.MarkedFoe(Enf), "the Enforcer is his foe now, weapon out or not: he fires once the Enforcer is in his weapon's effective reach");
            });

            Add("N51", "—", "An armed target sees it coming (Motive §1.3): the Enforcer stands in front of the door guard, in his cone, and orders the takedown. It fails into a fight: the guard draws and engages (Alert 2 at sight, the drawn-weapon rule), calls it in, and nobody is down. The card said so before the order ('aware: yes', a fight) once his sight was known.", r =>
            {
                var g = r.G;
                var front = new Pt(5055, -3000);
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

            Add("N53", "—", "Hidden but not tied (Motive §3.3): the guard knocked out unseen and stashed in the Dumpster; awake, he calls out every 10 s, heard within 20 m with no line. Guard 74 walks the lane, hears him and radios a noise (Alert 1); a robot is sent to the Dumpster, opens it and finds him, brings him round: his statement, +4 T. A knockout never takes Alert above 1.", r =>
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
                r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.Kind == "noise"), T(120)), "someone hears him and reports a noise");
                r.Is(r.Until(() => a.Confirmed || a.Statement || a.Woke, T(240)), "the Dumpster is opened (by the robot sent to it, or by its round at 5:22 if that comes first) and he is out");
                r.Eq(g.Alert, 1, "Alert 1: a knockout never more by itself");
                r.Is(r.Until(() => a.ActTrail, T(30)), "his statement or his own report: +4 T");
            });

            Add("N54", "—", "A body found by routine (Pulse §4.1): the guard killed unseen and left at his post. His 4:00 mark is missed (Alert 1); at 4:13 guard 74, on his way along the square, finds him and radios a dead body: Alert 2 when it lands, the level's units and robots. A unit confirms him: the act's +6 T, a found killing's step toward Menace (Sky 1), and the seal starts at the confirmation, not before.", r =>
            {
                var g = r.G;
                GuardQuiet(r);
                int trail = g.Trail, standing = g.Standing;
                r.Takedown("guard", "kill"); r.Done(Enf);
                r.SendTo(Enf, BesideDoor);
                var a = ActOn(r, GuardId);
                r.To(T(240)); r.Eq(g.Alert, 1, "the missed mark: Alert 1");
                r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.By == "p-records-office-74"), T(60)), "guard 74 finds the body on his routine");
                var rep = g.Reps.First(x => x.Act == a.Id && x.By == "p-records-office-74");
                r.Eq((rep.Kind, rep.LandAt - g.Tick), ("dead", T(g.R.WitnessReactS + g.R.PostCallS)), "staff know a dead body and radio it: 5 s");
                r.To(rep.LandAt);
                r.Eq(g.Alert, 2, "a dead body reported: Alert 2");
                r.Is(g.SecurityArrives == g.Tick + T(g.DialNow.DispatchS), "the response unit is sent to the scene");
                r.Eq(g.Lockdown, int.MaxValue / 4, "no seal on a report alone");
                r.Is(r.Until(() => a.Confirmed, T(200)), "a unit confirms him");
                r.Eq(g.Lockdown, g.Tick + T(g.SealS), "the seal starts at the confirmation (Alert 2)");
                r.Eq(g.Trail, trail + g.R.HitTrail + g.R.DownTrail + g.R.KillTrail, "the act's Trail: +6 T for a kill");
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
            });

            Add("N56", "—", "A woken victim's report (Motive §3; Pulse §5): guard 73, crossing the square to his post, is knocked out from behind with nobody near and carried to the bins on Rocket Lane's west pavement. He wakes 120 s later, staggers 4 s and radios it in (staff): Alert 1, +4 T, once; he holds what happened to him (attacked: place and time, no face: he never saw the Enforcer), and his fear rises.", r =>
            {
                var g = r.G;
                var w = WalkerById(r, "p-records-office-73");
                int t = T(240);
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
            });

            Add("N65", "—", "A witness stopped before the report lands (Pulse §3.2): guard 73 is killed unseen crossing the square at 4:00 and carried to the north edge. At 6:47 clerk 34 (Loyal, and 73's workmate: he runs, Pulse row 2), walking home ahead of his group, finds the body and runs to the nearest phone (a call box) to report it. The Enforcer meets him on the way and takes him down: his report never lands. The next finder's report lands later: the incident still reaches the desk, by the next earliest.", r =>
            {
                var g = r.G;
                var w73 = WalkerById(r, "p-records-office-73");
                int t = T(240);
                r.To(t - T(25)); MeetFromBehind(r, w73, t); r.To(t);
                r.Takedown("p-records-office-73", "kill"); r.Done(Enf);
                var a = ActOn(r, "p-records-office-73");
                r.Body(Enf, "carry", "p-records-office-73"); r.Done(Enf);
                var spot = new Pt(4201, 1066); // the probe's spot: first found by clerk 34 alone (6:47), the next finder 37 s later, 28 m from a door
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
                r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.Phase == "landed"), T(150)), "the next finder's report lands");
                var next = g.Reps.First(x => x.Act == a.Id && x.Phase == "landed");
                r.Is(next.By != "p-records-office-34", "by someone else, later");
                r.Is(g.Reps.Exists(x => x.Act == a.Id && x.P != null && x.P.Temperament == "mercenary" && x.Phase != "stopped"), "a Mercenary who finds it now reports it: quiet at Alert 0, he reports once Alert is 2 or more (Sky merged call 4)");
                r.Is(g.PersonById("p-records-office-34")!.Has("attacked-aware") && g.FactsOf(g.PersonById("p-records-office-34")!).Contains("enforcer-face"), "clerk 34 holds the Enforcer's face");
            });

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
                var g = r.G; const string Mast = "camera:mast-314";
                r.Is(g.WatcherIds().Contains(Mast) && g.S.Cameras.Exists(c => c.Id == "mast-314" && c.World && c.Facings.Length == 3), "the mast is a watcher, three heads");
                r.Is(g.CameraWatching("mast-314", r.Stand("d:Plaza east"), false) && g.CameraWatching("mast-314", r.Stand("d:East side"), false), "it sees Plaza east and East side");
                int r7 = 0, r7seen = 0; for (int dx = -300; dx <= 300; dx += 50) for (int dy = -300; dy <= 300; dy += 50) { var q = new Pt(-1880 + dx, -1630 + dy); if (!g.S.Grid.Free(q)) continue; r7++; if (g.CameraWatching("mast-314", q, false)) r7seen++; }
                r.Is(r7seen * 2 > r7, $"and most of Vantage's R7 (the café's east end: {r7seen} of {r7} free points within 3 m; the spot itself is under the pole, between two heads)");
                r.Is(!g.CameraWatching("mast-314", r.Stand("d:Café door"), false) && !g.CameraWatching("mast-314", r.Stand("d:Office entrance"), false), "not the café door (the wall) nor the Office side (beyond 30 m)");
                var spot = WatchSpot(r, Mast);
                r.To(r.SendTo(Scout, spot)); r.Do(Scout, "watch", Mast); r.Done(Scout);
                r.Is(g.Known(Mast)!.Sight && g.ConeFan(Mast)!.More.Length == 2, "watched: its sight is known, a fan for each head");
                r.To(r.Send(Tech, "cabinet"));
                r.Do(Tech, "loop", "mast-314"); r.Done(Tech);
                r.Is(g.LoopOn && g.LoopCam == "mast-314" && !g.CameraWatching("mast-314", r.Stand("d:Plaza east"), false), "looped: it sees nothing");
                r.Is(g.CameraWatching("entrance", r.Stand("d:Office entrance"), false), "the entrance camera stays live");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "loop", Target = "entrance" }).code, "loop-holds", "one loop at a time");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "loop", Target = "pickup" }).code, "loop-holds", "the pickup camera is on the cabinet too (brief 013 D), but one loop at a time");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "loop", Target = "gate" }).code, "loop-wrong-target", "no camera of that name (unknown = reject)");
            });

            Add("N57", "—", "A missing tie (Sky answer 6; Motive §5.2; Pulse §11): the turned driver, called at 5:00, moves the car across the pickup camera; the Enforcer kills him there, unseen, and puts him in the Dumpster. Vale comes out at 5:50 to a car with no driver: after suspicion-s (20 s, flat by default) at the place without him he walks to the official door and calls it in. A missing-person report: Alert 1, one incident with the killing; the alarm sends the response to the bay, to the driver's ties and to Vale's. (Start: the driver is a contact, so his car can blind the camera; the crowd is off, since guard 29 stands 4.1 m from the driver until 5:30: a pickup loop would do it in play, idle-study T1.)", r =>
            {
                var g = r.G;
                r.To(T(240)); r.To(r.SendTo(Enf, g.S.Stand(g.S.CarBlock) + new Pt(-300, -300)));
                r.To(T(300)); r.Do(Fixer, "call", Driver);
                r.To(g.BlockedFrom);
                r.Is(!g.PickupCameraLive(), "the camera is blind behind the car");
                r.To(r.SendTo(Enf, Beside(r, g.DriverPos()!.Value)!.Value));
                r.Takedown("driver", "kill"); r.Done(Enf);
                var a = ActOn(r, "driver");
                r.Body(Enf, "carry", "driver"); r.Done(Enf);
                r.To(r.SendTo(Enf, Dumpster));
                r.Body(Enf, "stash", DumpsterId); r.Done(Enf);
                r.SendTo(Enf, BesideDoor);
                r.Is(!a.Witnessed && !a.Landed, "unseen, unknown");
                r.To(T(g.S.ValeExits) + 1);
                r.Eq(g.Vale, ValeAt.Leaving, "5:50: Vale comes out to his car (the city doesn't know)");
                r.Is(r.Until(() => g.Log.Exists(l => l.Code == "person.missed"), T(60)), "he grows suspicious");
                r.Eq(g.Tick, T(g.S.ValeAtCar) + T(g.R.SuspicionS), "20 s after he reached the car (6:10): 6:30");
                var rep = g.Reps.First(x => x.Act == a.Id && x.Kind == "missing");
                r.Is(rep.P == g.ValeP, "Vale calls it in");
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
                r.SendTo(Scout, Beside(r, g.S.Stand(g.S.RivalsPark)) ?? g.S.Stand(g.S.RivalsPark)); r.To(T(g.S.RivalsArrive) + 1); r.Do(Scout, "mislead", "rivals"); // the false tip (route B), so the rivals don't reach Vale first
                r.To(T(240)); r.To(r.SendTo(Enf, g.S.Stand(g.S.CarBlock) + new Pt(-300, -300)));
                r.To(T(300)); r.Do(Fixer, "call", Driver);
                r.To(g.BlockedFrom);
                r.To(r.SendTo(Enf, Beside(r, g.DriverPos()!.Value)!.Value));
                r.Takedown("driver", "kill"); r.Done(Enf);
                r.Body(Enf, "carry", "driver"); r.Done(Enf);
                r.To(r.SendTo(Enf, Dumpster));
                r.Body(Enf, "stash", DumpsterId); r.Done(Enf);
                r.To(r.SendTo(Enf, Beside(r, g.CarPos())!.Value));
                r.To(T(g.S.ValeAtCar));
                r.Do(Enf, "take", Vale);
                r.Wait(T(g.R.SuspicionS + 5));
                r.Is(!g.Over && g.Vale == ValeAt.Taking || g.Vale == ValeAt.WithEnforcer, "Vale is being taken");
                r.Is(!g.Log.Exists(l => l.Code == "person.missed"), "being taken, Vale reports nothing");
                r.Is(!g.Acts.Exists(x => x.Take), "an unseen Take opens no incident (brief 013 D: a Take is seen only by whoever sees it)");
            }, DriverBlind());

            Add("N58", "—", "Who can be taken (Sky 2026-10-16: anyone; one rule, no named list): only the Enforcer; within his reach; anyone on the street, cast or crowd, by role or id; Vale only killed (knockout: use Take), and killing him fails the mission; the patrol and cameras are no targets (Reroute and Loop are their verbs).", r =>
            {
                var g = r.G;
                var x = new Order { Tick = g.Tick, Op = Fixer, Do = "takedown", Target = "p-cafe-12", How = "kill" };
                r.Eq(g.Issue(x).code, "only-enforcer", "only the Enforcer");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "takedown", Target = "patrol", How = "kill" }).code, "target-unknown", "the patrol is no target");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "takedown", Target = "p-cafe-12", How = "stun" }).code, "takedown-how", "a knockout or a kill");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "takedown", Target = "p-cafe-12", How = "kill" }).code, "too-far", "within reach");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "takedown", Target = "councillor", How = "kill" }).code, "person-away", "Vale is inside");
                r.To(T(g.S.ValeAtCar)); r.To(r.SendTo(Enf, Beside(r, g.ValePos()!.Value)!.Value));
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "takedown", Target = "councillor", How = "knockout" }).code, "takedown-vale", "Vale is taken, not knocked out");
                r.Takedown("councillor", "kill"); r.Done(Enf);
                r.Eq((g.Result, g.FailCode), (Outcome.Failure, "vale-dead"), "Vale killed: the mission fails");
            });

            Add("N59", "—", "An alarm beyond robot reach (Pulse §12.2; Motive rev 3): the Tech runs to the cabinet, the guard is killed unseen just after the reroute lands (2:01) and put through the service door before his 2:30 mark. The mark lands (Alert 1). The patrol is away and the world's one dock (by the café) is more than 60 m from the post, so the level's robots go only where they can: to where his lover is meant to be. Nobody checks the post: nothing is confirmed, Alert stays 1, no seal, the door stays shut. (A report found unfounded at the scene: N66.)", r =>
            {
                var g = r.G;
                g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "move", To = "cabinet", Run = true }); r.SendTo(Enf, BesideDoor);
                r.Until(() => r.Op(Tech).Walk == null, T(60));
                r.Do(Tech, "loop", "entrance"); r.Done(Tech); r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
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

            Add("N60", "—", "The response dial at level 0 (Sky 2026-10-16: the swarm is data; Motive §5.3): the guard killed unseen and left at his post. His 4:00 mark lands (Alert 1): the level's Alert-1 robots, on the street its dispatch later. Guard 74 finds the body and radios it (Alert 2): the response unit and the level's Alert-2 units and robots, then one more unit every reinforce up to the cap. Only the level's numbers differ.", r =>
            {
                var g = r.G; var lv = g.R.Dial[0];
                r.Eq(g.SecurityLevel, 0, "the level");
                GuardQuiet(r);
                r.Takedown("guard", "kill"); r.Done(Enf);
                r.SendTo(Enf, BesideDoor);
                r.To(T(240));
                r.Eq(g.Units.Count(u => u.Robot), lv.A1Robots, $"Alert 1: {lv.A1Robots} robot(s)");
                r.Is(g.Units.Where(u => u.Robot && !u.Patrol).All(u => u.Launch >= g.Tick + T(lv.DispatchS)), "on the street the dispatch after the landing");
                var a = ActOn(r, GuardId);
                r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.Kind == "dead" && x.Phase == "landed"), T(60)), "the dead body is reported");
                r.Eq(g.Alert, 2, "Alert 2");
                int landed = g.Tick;
                r.Eq(g.SecurityArrives, landed + T(lv.DispatchS), "the response unit: the level's dispatch after the landing");
                int officers = g.Units.Count(u => !u.Robot && !u.Cordon);
                r.Eq(officers, lv.A2Units * g.R.ResponseOfficers, $"Alert 2: {lv.A2Units} unit(s) of {g.R.ResponseOfficers}");
                r.Eq(g.Units.Count(u => u.Robot), Math.Max(lv.A1Robots, lv.A2Robots), "robots: the Alert-2 count, those out stay out");
                if (lv.ReinforceS > 0 && lv.UnitCap > lv.A2Units)
                {
                    r.To(landed + T(lv.DispatchS) + T(lv.ReinforceS));
                    r.Eq(g.Units.Count(u => !u.Robot && !u.Cordon), (lv.A2Units + 1) * g.R.ResponseOfficers, $"one more unit {lv.ReinforceS} s after the first rolled");
                }
            }, Level(0));

            Add("N61", "—", "The response dial at level 1 (Sky 2026-10-16: the swarm is data; Motive §5.3): the guard killed unseen and left at his post. His 4:00 mark lands (Alert 1): the level's Alert-1 robots, on the street its dispatch later. Guard 74 finds the body and radios it (Alert 2): the response unit and the level's Alert-2 units and robots, then one more unit every reinforce up to the cap. Only the level's numbers differ.", r =>
            {
                var g = r.G; var lv = g.R.Dial[1];
                r.Eq(g.SecurityLevel, 1, "the level");
                GuardQuiet(r);
                r.Takedown("guard", "kill"); r.Done(Enf);
                r.SendTo(Enf, BesideDoor);
                r.To(T(240));
                r.Eq(g.Units.Count(u => u.Robot), lv.A1Robots, $"Alert 1: {lv.A1Robots} robot(s)");
                r.Is(g.Units.Where(u => u.Robot && !u.Patrol).All(u => u.Launch >= g.Tick + T(lv.DispatchS)), "on the street the dispatch after the landing");
                var a = ActOn(r, GuardId);
                r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.Kind == "dead" && x.Phase == "landed"), T(60)), "the dead body is reported");
                r.Eq(g.Alert, 2, "Alert 2");
                int landed = g.Tick;
                r.Eq(g.SecurityArrives, landed + T(lv.DispatchS), "the response unit: the level's dispatch after the landing");
                int officers = g.Units.Count(u => !u.Robot && !u.Cordon);
                r.Eq(officers, lv.A2Units * g.R.ResponseOfficers, $"Alert 2: {lv.A2Units} unit(s) of {g.R.ResponseOfficers}");
                r.Eq(g.Units.Count(u => u.Robot), Math.Max(lv.A1Robots, lv.A2Robots), "robots: the Alert-2 count, those out stay out");
                if (lv.ReinforceS > 0 && lv.UnitCap > lv.A2Units)
                {
                    r.To(landed + T(lv.DispatchS) + T(lv.ReinforceS));
                    r.Eq(g.Units.Count(u => !u.Robot && !u.Cordon), (lv.A2Units + 1) * g.R.ResponseOfficers, $"one more unit {lv.ReinforceS} s after the first rolled");
                }
            }, Level(1));

            Add("N62", "—", "The response dial at level 2 (Sky 2026-10-16: the swarm is data; Motive §5.3): the guard killed unseen and left at his post. His 4:00 mark lands (Alert 1): the level's Alert-1 robots, on the street its dispatch later. Guard 74 finds the body and radios it (Alert 2): the response unit and the level's Alert-2 units and robots, then one more unit every reinforce up to the cap. Only the level's numbers differ.", r =>
            {
                var g = r.G; var lv = g.R.Dial[2];
                r.Eq(g.SecurityLevel, 2, "the level");
                GuardQuiet(r);
                r.Takedown("guard", "kill"); r.Done(Enf);
                r.SendTo(Enf, BesideDoor);
                r.To(T(240));
                r.Eq(g.Units.Count(u => u.Robot), lv.A1Robots, $"Alert 1: {lv.A1Robots} robot(s)");
                r.Is(g.Units.Where(u => u.Robot && !u.Patrol).All(u => u.Launch >= g.Tick + T(lv.DispatchS)), "on the street the dispatch after the landing");
                var a = ActOn(r, GuardId);
                r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.Kind == "dead" && x.Phase == "landed"), T(60)), "the dead body is reported");
                r.Eq(g.Alert, 2, "Alert 2");
                int landed = g.Tick;
                r.Eq(g.SecurityArrives, landed + T(lv.DispatchS), "the response unit: the level's dispatch after the landing");
                int officers = g.Units.Count(u => !u.Robot && !u.Cordon);
                r.Eq(officers, lv.A2Units * g.R.ResponseOfficers, $"Alert 2: {lv.A2Units} unit(s) of {g.R.ResponseOfficers}");
                r.Eq(g.Units.Count(u => u.Robot), Math.Max(lv.A1Robots, lv.A2Robots), "robots: the Alert-2 count, those out stay out");
                if (lv.ReinforceS > 0 && lv.UnitCap > lv.A2Units)
                {
                    r.To(landed + T(lv.DispatchS) + T(lv.ReinforceS));
                    r.Eq(g.Units.Count(u => !u.Robot && !u.Cordon), (lv.A2Units + 1) * g.R.ResponseOfficers, $"one more unit {lv.ReinforceS} s after the first rolled");
                }
            }, Level(2));

            Add("N63", "—", "The response dial at level 3 (Sky 2026-10-16: the swarm is data; Motive §5.3): the guard killed unseen and left at his post. His 4:00 mark lands (Alert 1): the level's Alert-1 robots, on the street its dispatch later. Guard 74 finds the body and radios it (Alert 2): the response unit and the level's Alert-2 units and robots, then one more unit every reinforce up to the cap. Only the level's numbers differ.", r =>
            {
                var g = r.G; var lv = g.R.Dial[3];
                r.Eq(g.SecurityLevel, 3, "the level");
                GuardQuiet(r);
                r.Takedown("guard", "kill"); r.Done(Enf);
                r.SendTo(Enf, BesideDoor);
                r.To(T(240));
                r.Eq(g.Units.Count(u => u.Robot), lv.A1Robots, $"Alert 1: {lv.A1Robots} robot(s)");
                r.Is(g.Units.Where(u => u.Robot && !u.Patrol).All(u => u.Launch >= g.Tick + T(lv.DispatchS)), "on the street the dispatch after the landing");
                var a = ActOn(r, GuardId);
                r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.Kind == "dead" && x.Phase == "landed"), T(60)), "the dead body is reported");
                r.Eq(g.Alert, 2, "Alert 2");
                int landed = g.Tick;
                r.Eq(g.SecurityArrives, landed + T(lv.DispatchS), "the response unit: the level's dispatch after the landing");
                int officers = g.Units.Count(u => !u.Robot && !u.Cordon);
                r.Eq(officers, lv.A2Units * g.R.ResponseOfficers, $"Alert 2: {lv.A2Units} unit(s) of {g.R.ResponseOfficers}");
                r.Eq(g.Units.Count(u => u.Robot), Math.Max(lv.A1Robots, lv.A2Robots), "robots: the Alert-2 count, those out stay out");
                if (lv.ReinforceS > 0 && lv.UnitCap > lv.A2Units)
                {
                    r.To(landed + T(lv.DispatchS) + T(lv.ReinforceS));
                    r.Eq(g.Units.Count(u => !u.Robot && !u.Cordon), (lv.A2Units + 1) * g.R.ResponseOfficers, $"one more unit {lv.ReinforceS} s after the first rolled");
                }
            }, Level(3));

            Add("N64", "—", "The response dial at level 4 (Sky 2026-10-16: the swarm is data; Motive §5.3): the guard killed unseen and left at his post. His 4:00 mark lands (Alert 1): the level's Alert-1 robots, on the street its dispatch later. Guard 74 finds the body and radios it (Alert 2): the response unit and the level's Alert-2 units and robots, then one more unit every reinforce up to the cap. Only the level's numbers differ.", r =>
            {
                var g = r.G; var lv = g.R.Dial[4];
                r.Eq(g.SecurityLevel, 4, "the level");
                GuardQuiet(r);
                r.Takedown("guard", "kill"); r.Done(Enf);
                r.SendTo(Enf, BesideDoor);
                r.To(T(240));
                r.Eq(g.Units.Count(u => u.Robot), lv.A1Robots, $"Alert 1: {lv.A1Robots} robot(s)");
                r.Is(g.Units.Where(u => u.Robot && !u.Patrol).All(u => u.Launch >= g.Tick + T(lv.DispatchS)), "on the street the dispatch after the landing");
                var a = ActOn(r, GuardId);
                r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.Kind == "dead" && x.Phase == "landed"), T(60)), "the dead body is reported");
                r.Eq(g.Alert, 2, "Alert 2");
                int landed = g.Tick;
                r.Eq(g.SecurityArrives, landed + T(lv.DispatchS), "the response unit: the level's dispatch after the landing");
                int officers = g.Units.Count(u => !u.Robot && !u.Cordon);
                r.Eq(officers, lv.A2Units * g.R.ResponseOfficers, $"Alert 2: {lv.A2Units} unit(s) of {g.R.ResponseOfficers}");
                r.Eq(g.Units.Count(u => u.Robot), Math.Max(lv.A1Robots, lv.A2Robots), "robots: the Alert-2 count, those out stay out");
                if (lv.ReinforceS > 0 && lv.UnitCap > lv.A2Units)
                {
                    r.To(landed + T(lv.DispatchS) + T(lv.ReinforceS));
                    r.Eq(g.Units.Count(u => !u.Robot && !u.Cordon), (lv.A2Units + 1) * g.R.ResponseOfficers, $"one more unit {lv.ReinforceS} s after the first rolled");
                }
            }, Level(4));
        }
    }
}
