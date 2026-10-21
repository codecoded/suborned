// Brief 013 D (Keel, 2026-10-16): the engine fixes and rule changes after Waymark played the Handover with takedowns
// (briefs/013/waymark-handover.md §5, §6; Aster's hand-off). One scenario a fix or change, on the real block and the promoted cast:
//  N68 the report chain no longer throws when a check ends and its walk to the phone is added (5:34.1, "Collection was modified")
//  N69 a takedown under the live entrance camera goes through the camera's report chain; N69b the §07 entry rule it was mistaken for
//  N70 one incident, one dispatch (a crowd's reports); N71 a later report at the same Alert sends nothing; N72 the pickup camera loops
//  N73 a Take seen is a takedown seen (guard 74 at the official entrance at 6:10)
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>Orders at their ticks, as a route script gives them (the scenario then checks what it needs).</summary>
        static void Orders(Run r, params Order[] os)
        {
            foreach (var o in os)
            {
                r.To(o.Tick); o.Tick = r.G.Tick;
                var (ok, why) = r.G.Issue(o);
                r.Is(ok, $"{o.Op} {o.Do} {o.Target}{o.To} taken at {Report.Clock(r.G.Tick)}{(ok ? "" : ": " + why)}");
            }
        }
        static Order Mv(int tick, string op, string to, bool run = false) => new Order { Tick = tick, Op = op, Do = "move", To = to, Run = run };
        static Order MvTo(int tick, string op, Pt at) => new Order { Tick = tick, Op = op, Do = "move", Point = at };
        static Order Act(int tick, string op, string verb, string target = "", string how = "") => new Order { Tick = tick, Op = op, Do = verb, Target = target, How = how };

        static void TakedownFixes()
        {
            Add("N68", "—", "A check that ends while reports are stepped (Waymark §6, 'Collection was modified' at 5:34.1): route G-kill with the body left at the guard's post and the Enforcer waiting with Vale in the service lane (brief 018: at the getaway bay the loud fight's blocker now downs him first). The entrance camera reads the dead guard when the Tech leaves the cabinet (Alert 2); at the chime the clerks stream out and find him; Idealist clerk 09 walks to look (clerk 03 before Pulse's leave stagger, brief 023), and the Mercenaries among them report it, Alert being 2, and at 5:34.1 his check ends and his walk to the phone starts as a new report while the reports are being stepped. The engine goes on, his report is a dead body, and the run is sealed at 6:16 (6:12 before the door moved to the west face). Brief 018: the fixture now runs without the final fight (Start.FinalFight off): with it, the loud fight's blocker takes the cover beside the getaway car and downs the cell long before 5:34, so the stepped report is never reached; without it the 5:34.1 path is the same and the dial's seal falls at 6:16 (5:37 was the loud fight's own seal).", r =>
            {
                var g = r.G;
                Orders(r, new Order { Tick = 0, Op = Tech, Do = "move", To = "cabinet", Run = true }, MvTo(0, Enf, BesideDoor), Mv(0, Scout, "d:Office approach"), Mv(0, Fixer, "crew:fixer"),
                    Act(166, Tech, "loop", "entrance"), Act(462, Scout, "watch", "guard"), Mv(662, Scout, "crew:scout"), Act(1060, Tech, "reroute", "patrol"), // as c022's route G-kill (2026-10-19, the door on the west face: the loop at 0:16.6, the watch at 0:46.2; were 0:13.6 and 0:35.5)
                    MvTo(1440, Enf, BesideGuard), Act(1510, Enf, "takedown", "guard", "kill"), Act(1540, Enf, "enter"),
                    MvTo(1540, Tech, new Pt(675, -4625)), MvTo(1540, Scout, new Pt(1175, -4825)), MvTo(1540, Fixer, new Pt(1025, -4625)), Mv(1920, Enf, "d:Getaway bay"));
                const string Clerk = "p-records-office-09";
                r.Is(r.Until(() => g.Reps.Exists(x => x.By == Clerk && x.Phase == "check"), T(340) - g.Tick), "the Idealist clerk finds the body after the chime and walks to look");
                var check = g.Reps.FirstOrDefault(x => x.By == Clerk && x.Phase == "check"); if (check == null) return;
                // Sky merged call 4 (moved here from N65 on 2026-10-19: the leave stagger sends no Mercenary past N65's body): quiet at Alert 0, a
                // Mercenary who finds a body reports it once Alert is 2 or more; here the entrance camera's report has Alert at 2 before the chime
                r.Is(g.Alert >= g.R.MercenaryAlert && g.Reps.Exists(x => x.P != null && x.P.Temperament == "mercenary" && x.Kind == "down" && x.Phase == "walk"), "a Mercenary who finds it now reports it: quiet at Alert 0, he reports once Alert is 2 or more (Sky merged call 4)");
                r.To(check.PhaseUntil);
                r.Is(g.FailCode != "engine-error", $"no engine error when the check ends ({Report.Clock(g.Tick)}){(g.FailCode == "engine-error" ? ": " + g.FailText : "")}");
                r.Is(check.Phase == "stopped" && g.Reps.Exists(x => x.By == Clerk && x.Kind == "dead" && x.Phase == "walk" && x.Seq > check.Seq), "his check is done: he walks to the phone with a dead body (a new report, stepped the same tick)");
                r.Until(() => g.Over, T(60));
                r.Eq(g.FailCode, "sealed", "sealed, the fixture's failure");
                r.Eq(g.Tick, T(376), "at 6:16 (the dial's seal: no final fight, brief 018; 6:12 before 2026-10-19: the unit's walk to the post on the west face confirms him 4 s later)");
                var fc = CauseIs(r, "sealed", "sealed", false, "", "", Enf); // the fail camera (ScenariosFailCause.cs): no single culprit; the report the confirmation sealed on
                var o = fc?.Origin;
                r.Is(o != null && o.Because == "confirmed" && o.Report.StartsWith("report:dead:", StringComparison.Ordinal) && o.Reporter?.Id == "camera:entrance" && o.Confirmer != null && o.Confirmer.Placed, $"the origin: the entrance camera's report of the dead guard, confirmed by a unit (got {o?.Report}, {o?.Because}, {o?.Reporter}, {o?.Confirmer})");
            }, new Start { FinalFight = false });

            Add("N69", "—", "A takedown under the live entrance camera (Waymark §6): route G without the Loop. The camera is a watcher like any other: it records the act, its watcher reads the screen at the next scan and the report lands 3 s later (Alert 3, a takedown seen); nothing fails at the act or when he is out. At this level Alert 3 locks the Office doors: the Enter is refused, and the response comes by the dial. (The old guard-down rule is kept only for a guard shot: his missed mark calls the response, as gunfire's own rule.)", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, Forecourt) /* c025 (brief 031): waits out of sight */));
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                int mark = g.NextMarkOf(g.GuardP, g.Tick); r.To(mark + 1);
                r.To(r.SendTo(Enf, BesideGuard));
                r.Is(g.CameraWatching("entrance", r.Op(Enf).Pos), "the entrance camera is live on the post");
                r.Takedown("guard", "knockout");
                var a = ActOn(r, GuardId);
                r.Is(a.Noticed.Contains("camera:entrance"), "the camera records the act");
                var rep = g.Reps.Single(x => x.Act == a.Id && x.By == "camera:entrance");
                r.Is(rep.Phase == "watcher" && rep.LandAt > g.Tick && rep.LandAt <= g.Tick + T(g.R.CameraScanS) + T(g.R.PostCallS), $"its report lands at the next scan + 3 s ({Report.Clock(rep.LandAt)})");
                r.Done(Enf);
                r.Is(!g.Over && g.GuardDown, "he is out, and nothing has failed");
                r.Eq(g.Alert, 0, "no Alert before the report lands (a silent act)");
                r.To(rep.LandAt - 1); r.Is(!g.Over && g.Alert == 0, "still quiet the tick before");
                r.To(rep.LandAt);
                r.Eq(g.Alert, 3, "the camera's report lands: Alert 3 (a takedown seen)");
                r.Is(!g.Over && g.Log.Exists(l => l.Code == "alarm.dispatches"), "the response is sent; the mission goes on");
                r.To(r.SendTo(Enf, r.Stand(g.S.GuardPost)));
                if (g.DialNow.DoorsLock) r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "enter" }).code, "doors-locked", "Alert 3 at this level: the doors are locked");
            });

            Add("N69b", "—", "What Waymark's careless camera fixture met (§6): the Enter given the moment the guard is out, under the live entrance camera, fails at once by §07's entry rule ('entry seen by guard, live camera or patrol': the Enforcer at the door in its view), before the camera's report of the takedown lands. The takedown's own report is still on its way: the fail is the entry, not the takedown.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, BesideDoor)));
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                int mark = g.NextMarkOf(g.GuardP, g.Tick); r.To(mark + 1);
                r.To(r.SendTo(Enf, BesideGuard));
                r.Takedown("guard", "knockout"); r.Done(Enf);
                var a = ActOn(r, GuardId);
                var rep = g.Reps.Single(x => x.Act == a.Id && x.By == "camera:entrance");
                r.Is(!g.Over && rep.LandAt > g.Tick, "the guard is out; the camera's report has not landed");
                g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "enter" });
                r.Eq(g.FailCode, "entry-seen-camera", "the Enter in the camera's view fails (§07)");
                var cam = g.S.Cameras.Single(x => x.Id == "entrance"); // the fail camera (ScenariosFailCause.cs): the entrance camera, as it faces
                PosedAs(r, CauseIs(r, "entry-seen-camera", "seen", true, "camera:entrance", "camera", Enf)?.Culprits.FirstOrDefault(), cam.At, cam.Facing, "the entrance camera on its pole");
                r.Is(rep.Phase == "watcher" && !a.Landed, "the takedown's report never landed: it was not the takedown that failed");
            });

            Add("N70", "—", "One incident, one dispatch (Aster 2026-10-16; Waymark §6: six dispatches in 0.1 s): the guard knocked out after 5:30 in front of the clerks streaming out after the chime. Many report it, one incident; the first report to land dispatches the response; a later report that raises the incident's Alert tops the same dispatch up to the new Alert's counts (from the destinations the first set); every other later report lands (logged) and sends nothing.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, Forecourt) /* c025 (brief 031): waits out of sight */));
                r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                r.To(T(286)); r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                r.To(r.SendTo(Enf, BesideGuard));
                r.Takedown("guard", "knockout");
                var a = ActOn(r, GuardId);
                var reps = g.Reps.Where(x => x.Act == a.Id && x.Phase != "stopped").ToList();
                r.Is(reps.Count >= 3, $"{reps.Count} reports of the one takedown on their way");
                int first = reps.Min(x => x.LandAt), last = Math.Min(reps.Max(x => x.LandAt), first + T(g.DialNow.ReinforceS) - 1);
                r.To(first);
                r.Eq(g.Log.Count(l => l.Code == "alarm.dispatches"), 1, "the first report dispatches");
                var dests = g.AlarmDestinations.ToList();
                int steps = 0, quiet = 0; bool unitsOnlyOnStep = true;
                while (g.Tick < last && !g.Over)
                {
                    int alert = g.Alert, units = g.Units.Count, landed = g.Reps.Count(x => x.Act == a.Id && x.Phase == "landed");
                    g.Step();
                    int now = g.Reps.Count(x => x.Act == a.Id && x.Phase == "landed");
                    if (g.Alert > alert) steps++;
                    else { if (now > landed) quiet += now - landed; if (g.Units.Count != units) unitsOnlyOnStep = false; }
                }
                r.Is(quiet >= 2, $"{quiet} later reports landed without raising the Alert");
                r.Is(unitsOnlyOnStep, "they sent no unit (units come only with an Alert step)");
                r.Eq(g.Log.Count(l => l.Code == "alarm.dispatches"), 1 + steps, $"one dispatch, topped up {steps} time(s) by an Alert step, never once a report");
                r.Eq(g.Alert, 3, "Alert 3 (a takedown seen)");
                r.Is(g.Units.Count(u => u.Robot) <= Math.Max(g.DialNow.A1Robots, g.DialNow.A2Robots) && g.Units.Count(u => !u.Robot && !u.Cordon && !u.Legacy) <= g.DialNow.A2Units * g.R.ResponseOfficers, "the dial's counts, never a second set");
                r.Is(g.AlarmDestinations.SequenceEqual(dests), "the destinations stand as the first report set them");
                r.Eq(g.Reported, 1, "one reported incident");
            });

            Add("N71", "—", "A later report at the same Alert sends nothing (one incident, one dispatch): route G-KO, the guard knocked out unseen after his 2:30 mark and left at the post. His 4:00 mark lands (Alert 1: the level's robots); guard 74 finds him and radios it: a knockout stays Alert 1, so his report lands and the robots out stay out, with no second dispatch. A later report that raises the Alert tops the same dispatch up to the new level's counts (N62: the dead guard's report, Alert 1 to 2). His report is the body found, so its scene unit comes (rev 4, N74): that is the lockdown, not a dispatch.", r =>
            {
                var g = r.G;
                GuardQuiet(r);
                r.Takedown("guard", "knockout"); r.Done(Enf);
                r.SendTo(Enf, BesideDoor);
                r.To(T(240));
                var a = ActOn(r, GuardId);
                r.Is(a.MarkMissed && g.Alert == 1, "the 4:00 mark lands: Alert 1");
                int robots = g.Units.Count(u => u.Robot); var dests = g.AlarmDestinations.ToList();
                r.Eq(g.Log.Count(l => l.Code == "alarm.dispatches"), 1, "one dispatch");
                r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.P != null && x.Phase == "landed"), T(60)), "a passer-by's report of the body lands");
                r.Eq(g.Alert, 1, "a knockout stays Alert 1");
                r.Eq(g.Log.Count(l => l.Code == "alarm.dispatches"), 1, "no second dispatch");
                r.Eq(g.Units.Count(u => u.Robot), robots, "no robot sent twice");
                r.Is(g.Units.Count(u => !u.Robot) <= g.R.ResponseOfficers && g.SceneHolds.Count == 1, "the one unit that comes is the found body's scene unit (rev 4 R4.1), not a second dispatch");
                r.Is(g.AlarmDestinations.SequenceEqual(dests), "the destinations stand");
            });

            Add("N72", "—", "The Tech loops the pickup camera (Sky answer 6: 'if the tech takes out the camera, then taking out the guard and driver is possible'; Waymark §5.1; rulebook cell.loop.targets): one camera at a time, the same 62 s and +1 T. Looped, it sees nothing: official-entrance guard 29 knocked out beside the car is not on its screen (the driver beside him, a Mercenary at Alert 0, keeps quiet). The entrance camera stays live; one loop at a time.", r =>
            {
                var g = r.G;
                r.To(r.Send(Tech, "cabinet"));
                int trail = g.Trail;
                r.Do(Tech, "loop", "pickup"); r.Done(Tech);
                r.Is(g.LoopOn && g.LoopCam == "pickup" && g.Trail == trail + g.R.LoopTrail, "the pickup camera is looped (+1 T)");
                r.Is(!g.CameraWatching("pickup", r.At("d:Official drop-off")) && g.CameraWatching("entrance", r.Stand("d:Office entrance")), "it sees nothing; the entrance camera stays live");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "loop", Target = "entrance" }).code, "loop-holds", "one loop at a time");
                var w = WalkerById(r, "p-records-office-29"); var at = g.CrowdPos(w)!.Value;
                var spot = Beside(r, at) ?? throw new InvalidOperationException("no ground beside guard 29");
                r.To(r.SendTo(Enf, spot));
                r.Takedown("p-records-office-29", "knockout"); r.Done(Enf);
                var a = ActOn(r, "p-records-office-29");
                r.Is(!a.Noticed.Contains("camera:pickup") && !g.Reps.Exists(x => x.Act == a.Id && x.By == "camera:pickup"), "the looped pickup camera does not report it");
            });

            Add("N73", "—", "A Take seen is a takedown seen (Aster 2026-10-16; Waymark §5.2: guard 74 stood 4 m from Vale at 6:10 and did nothing): the pickup camera looped and the rivals sent away, the Enforcer takes Vale at his car at 6:10 in front of guard 74 at the official entrance. At sight: the witnesses' reactions, +1 witness Trail and one step toward Menace, no Alert; guard 74 (staff) radios it, and when the first report lands, Alert 3 (a takedown seen) and the response. One incident; no assault Trail (the Take keeps its own, +2 T on an unturned driver, when it lands). An unseen Take opens nothing (N57b).", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Scout, new Pt(8600, -3400)))); // c025 (brief 031): the tip waits outside the pickup camera's cone
                r.To(T(g.S.RivalsArrive) + 1); r.Do(Scout, "mislead", "rivals");
                r.To(T(300)); r.Do(Tech, "loop", "pickup"); r.Done(Tech);
                r.To(r.SendTo(Enf, Beside(r, g.CarPos())!.Value));
                r.To(T(g.S.ValeAtCar));
                var w74 = WalkerById(r, "p-records-office-74");
                r.Is(g.CrowdPos(w74) is Pt p74 && G.Within(p74, g.ValePos()!.Value, g.R.WitnessRangeCm), "guard 74 stands within witness range of Vale at his car");
                int trail = g.Trail, standing = g.Standing;
                r.Do(Enf, "take", Vale);
                r.Eq(g.Vale, ValeAt.Taking, "the Take is under way (the pickup camera looped: no §07 camera fail)");
                var a = g.Acts.SingleOrDefault(x => x.Take);
                r.Is(a != null && a.Witnessed && a.Noticed.Contains("p-records-office-74"), "seen: guard 74 notices it");
                if (a == null) return;
                r.Eq(g.Alert, 0, "no Alert at sight: it reaches Alert when a report lands");
                r.Eq(g.Trail, trail + g.R.WitnessTrail, "+1 witness Trail at sight, once");
                r.Eq(g.Standing, standing - g.R.StandingStep, "one step toward Menace at sight");
                r.Is(g.Reps.Exists(x => x.Act == a.Id && x.By == "p-records-office-74" && x.Phase == "radio"), "guard 74 radios it in (staff)");
                int land = g.Reps.Where(x => x.Act == a.Id && x.Phase != "stopped").Min(x => x.LandAt);
                r.To(land - 1); r.Eq(g.Alert, 0, "quiet until the first report lands");
                r.To(land);
                r.Eq(g.Alert, 3, "Alert 3 when it lands (a takedown seen)");
                r.Is(g.Log.Exists(l => l.Code == "alarm.dispatches") && g.SecurityArrives == land + T(g.DialNow.DispatchS), "the response: the unit the level's dispatch after the landing");
                r.Is(!a.ActTrail && g.Trail == trail + g.R.WitnessTrail, "no assault Trail: the Take keeps its own");
                r.Eq(g.Acts.Count(x => x.Take), 1, "one Take, one incident");
            });

            Add("N74", "—", "A found body holds the scene (Motive rev 4 R4.1; Sky 2026-10-16): the guard knocked out unseen at his post; the card says that, found here, he would hold the Office door. The Tech lets the loop go, so the entrance camera reads him at its next scan: a body found. A unit of officers comes at Alert 1 (no robot reach for officers; not one of the dial's units) and holds the Office door, which he lies at: Enter is refused (door-held) from its arrival, through its check and 60 s sweep, until it stands down; the Alert stays 1 (a knockout), and at Alert 1 the sweep opens no stash.", r =>
            {
                var g = r.G;
                GuardQuiet(r);
                var q = g.QuoteTakedown("guard", "knockout");
                r.Is(q.HoldsDoors.Contains(g.EnterDoor) && q.HoldS == g.R.CheckS + g.R.SearchS && q.PoliceInS > 0, $"the card: found here, he holds {string.Join(", ", q.HoldsDoors)} ~{q.HoldS} s; a unit in ~{q.PoliceInS} s");
                r.Takedown("guard", "knockout"); r.Done(Enf);
                var a = ActOn(r, GuardId);
                r.SendTo(Enf, BesideDoor);
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "cancel" }).ok && !g.LoopOn, "the Tech lets the loop go: the camera is live");
                r.Is(r.Until(() => a.Landed, T(30)), "the camera reads him: a report of a body lands");
                r.Eq(g.Alert, 1, "a knockout: Alert 1");
                r.Is(g.SceneHolds.Count == 1 && g.SceneHolds[0].Doors.Contains(g.EnterDoor) && !g.SceneHolds[0].Doors.Contains(g.ExitDoor), $"the scene holds the doors in its sight: {string.Join(", ", g.SceneHolds[0].Doors)} (the service door is round the corner)");
                var unit = g.Units.FirstOrDefault(u => !u.Robot && u.Act == a.Id && u.Scene);
                r.Is(unit != null && unit.Launch == g.Tick + T(g.DialNow.DispatchS), "a unit of officers is sent at Alert 1, the level's dispatch after the landing");
                if (unit == null) return;
                r.Is(!g.DoorHeld(g.EnterDoor), "not held until it arrives");
                r.Is(r.Until(() => unit.St == "check", T(120)), "it arrives");
                r.To(r.SendTo(Enf, r.Stand(g.S.GuardPost)));
                r.Is(g.DoorHeld(g.EnterDoor), "the Office door is held");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "enter" }).code, "door-held", "Enter is refused");
                r.Is(r.Until(() => unit.St == "search", T(20)), "it sweeps the scene");
                r.Is(unit.ToOpen.Count == 0, "at Alert 1 the sweep opens no stash");
                r.Is(r.Until(() => !g.DoorHeld(g.EnterDoor), T(200)), $"the door is held until the unit stands down ({Report.Clock(g.Tick)})");
                r.Is(unit.St == "back" || unit.St == "gone", "the unit goes back");
                r.Eq(g.Alert, 1, "Alert stays 1");
            });

            Add("N74b", "—", "A missed check-in is an empty post, not a body (rev 4 R4.1): the guard killed unseen and put through the service door; his 4:00 mark lands (Alert 1) and the robots come, but no door is held and no officer unit is sent.", r =>
            {
                var g = r.G;
                GuardQuiet(r);
                r.Takedown("guard", "kill"); r.Done(Enf);
                r.Body(Enf, "carry", "guard"); r.Done(Enf);
                r.To(r.SendTo(Enf, ServiceDoor)); r.Body(Enf, "stash", ServiceDoorId); r.Done(Enf);
                r.To(T(240) + 1);
                r.Eq(g.Alert, 1, "the missed mark: Alert 1");
                r.Is(g.SceneHolds.Count == 0 && !g.Units.Exists(u => !u.Robot), "no scene held, no officers: nobody found a body");
            });

            Add("N75", "—", "No exit through a held door (rev 4 R4.1): the guard killed unseen after his 2:30 mark (dead, he never walks back to his door) and laid at the Office's service door; guard 74, walking the lane from 4:04, finds him and radios it; with the patrol rerouted again, the Enforcer goes in. The unit holds the service door from its arrival. The Enter under way (68 s at Alert 2) finishes, and the Enforcer comes out with Vale by the public door, which is not held.", r =>
            {
                var g = r.G;
                GuardQuiet(r);
                r.Takedown("guard", "kill"); r.Done(Enf);
                r.Body(Enf, "carry", "guard"); r.Done(Enf);
                r.To(r.SendTo(Enf, ServiceDoor)); r.Body(Enf, "drop"); r.Done(Enf);
                var a = ActOn(r, GuardId);
                r.To(r.Send(Enf, "d:Office entrance"));
                r.Is(r.Until(() => g.SceneHolds.Count > 0, T(150)), "the body by the service door is found (guard 74, walking the lane)");
                r.To(Math.Max(g.Tick, g.PatrolAwayUntil)); r.Do(Tech, "reroute", "patrol"); r.Done(Tech); // the patrol away again for the Enter
                r.Do(Enf, "enter");
                r.Is(g.SceneHolds.Count > 0 && g.SceneHolds[0].Doors.Contains(g.ExitDoor) && !g.SceneHolds[0].Doors.Contains(g.EnterDoor), "its scene holds the service door, not the public door");
                r.Is(r.Until(() => g.DoorHeld(g.ExitDoor), T(60)), "the unit arrives and holds it");
                r.Is(r.Op(Enf).Inside, "the Enforcer is still inside");
                r.Done(Enf);
                r.Eq(g.Vale, ValeAt.WithEnforcer, "the Enter under way finishes");
                r.Is(G.Within(r.Op(Enf).Pos, r.Stand(g.S.GuardPost), 100) && g.Log.Exists(l => l.Code == "enter.out" && l.Text.Contains("public door")), "out by the public door, not through the held service door");
            });

            Add("N76", "—", "Stash capacity (rev 4 R4.3): a service door opens on a storeroom and holds 3 bodies (takedown.stash.doorHolds), the Dumpster 1 (its site entry). Two guards knocked out unseen in the lane: both go through the service door; the Dumpster, holding one, refuses a second.", r =>
            {
                var g = r.G;
                r.Is(g.S.Stashes.All(s => g.HoldsOf(s) == (s.Kind == "door" ? 3 : 1)), "every service door holds 3, the Dumpster 1");
                GuardQuiet(r);
                r.Takedown("guard", "knockout"); r.Done(Enf);
                r.Body(Enf, "carry", "guard"); r.Done(Enf);
                r.To(r.SendTo(Enf, Dumpster)); r.Body(Enf, "stash", DumpsterId); r.Done(Enf);
                r.Is(g.BodyById("guard")!.Stash == DumpsterId, "the guard is in the Dumpster");
                var w = WalkerById(r, "p-records-office-74");
                r.To(MeetFromBehind(r, w, T(250)));
                r.Is(r.Until(() => g.CrowdPos(w) is Pt p && G.Within(p, r.Op(Enf).Pos, g.R.TakedownReachCm), T(30)), "guard 74 walks into reach");
                r.Takedown("p-records-office-74", "knockout"); r.Done(Enf);
                r.Body(Enf, "carry", "p-records-office-74"); r.Done(Enf);
                r.To(r.SendTo(Enf, Dumpster));
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "stash", Target = DumpsterId }).code, "stash-full", "the Dumpster holds one");
                r.To(r.SendTo(Enf, ServiceDoor)); r.Body(Enf, "stash", ServiceDoorId); r.Done(Enf);
                r.Is(g.BodyById("p-records-office-74")!.Stash == ServiceDoorId, "the service door takes him");
            });

            Add("N77", "—", "Route B under rev 4 (Motive R4.2; Sky 2026-10-16), Waymark's route-b orders: the Take at Vale's own car with its driver turned happens inside the car, off-screen: no witness window, though the clerks fill the lane at 6:10; the turned driver is the act's accomplice, never its witness. The escort to the getaway car (by the lane's south end, as the route walks it) is seen only by Vale's kin, or by anyone within 3 m who sees the grip: nobody. 7:39, 3 T (the Pay is private since Sky 2026-10-16; 4 T before), Alert 0.", r =>
            {
                var g = r.G;
                // c025 (Waymark 2026-10-21, engine 0.14.1; brief 031): route-b re-pinned: the Scout watches the driver from the SW corner and tips the
                // rivals from (8600, -3400), 2.8 m outside the pickup cone; the three's posts by the car within 15 m of the ambush
                Orders(r, Mv(0, Enf, "d:Service lane"), MvTo(0, Scout, new Pt(7950, -4000)), MvTo(0, Fixer, new Pt(7950, -3850)), Act(620, Scout, "watch", Driver),
                    Mv(760, Fixer, "d:Boarding pavilion"), Mv(820, Scout, "d:Meet driver"), new Order { Tick = 820, Op = Fixer, Do = "turn", Target = Driver, Approach = "pay" }, Act(838, Scout, "listen", Driver),
                    MvTo(1258, Scout, new Pt(8600, -3400)), MvTo(1320, Fixer, new Pt(1025, -4625)), Act(1900, Scout, "mislead", "rivals"), MvTo(2340, Scout, new Pt(1175, -4825)), MvTo(2340, Tech, new Pt(675, -4625)));
                r.To(T(370));
                r.Is(g.PickupService && g.TakeInCar, "Vale at his own car in the lane, its driver turned");
                int near = 0; var vp = g.ValePos()!.Value; foreach (var w in g.Walkers) if (g.CrowdPos(w) is Pt p && G.Within(p, vp, g.R.WitnessRangeCm)) near++;
                // the lane is not empty: someone stands within witness range of the car, so a Take outside it would be seen (Waymark 2026-10-19:
                // three clerks at 6:10 on this tree; the check asks for one, since the crowd's own pace moves who is there)
                r.Is(near >= 1, $"{near} people within witness range of the car");
                Orders(r, Act(3700, Enf, "take"));
                r.Done(Enf);
                r.Is(!g.Acts.Exists(x => x.Take), "inside the car: nobody saw a Take, the driver no witness");
                r.Eq(g.Alert, 0, "Alert 0");
                Orders(r, MvTo(4080, Enf, new Pt(2400, -6000)), Act(4470, "cell", "panic"), Mv(4570, Enf, "d:Getaway bay"), Act(4699, Enf, "board", "car"), Act(4739, "cell", "go")); // c025: Board (with Vale, 4 s) and Go
                r.Is(!g.Acts.Exists(x => x.Take), "the escort: no kin of Vale near, nobody within 3 m of the grip");
                r.Eq(g.Result, Outcome.Success, "route B wins");
                r.Eq((g.Tick, g.Trail, g.Alert), (4739, 3, 0), "at 7:53.9 (Waymark's c025 route-b, 2026-10-21, engine 0.14.1), 3 T, Alert 0 (rulebook privacy, Sky 2026-10-16: the driver's Pay is private, so the clerk who saw it adds no witness Trail; it was 4 T)");
            });

            Add("N78", "—", "The escort seen (rev 4 R4.2): Vale taken inside his car in the lane (the driver turned), then walked straight at a clerk standing in the lane: within 3 m she sees the grip. A carry seen: +1 witness Trail and one step toward Menace at sight; her report lands at Alert 2. Workmate is not kin (rev 4): Vale's workmates further off see two men walking.", r =>
            {
                var g = r.G;
                r.Send(Enf, "d:Service lane");
                r.To(Max(r.Send(Scout, "d:Meet driver"), r.Send(Fixer, "d:Boarding pavilion")));
                r.Do(Scout, "watch", Driver); r.Done(Scout); r.Do(Scout, "listen", Driver); r.Done(Scout);
                TurnDriver(r);
                r.To(r.SendTo(Scout, new Pt(8250, -3100))); r.To(T(320)); r.Do(Scout, "mislead", "rivals");
                r.To(T(370)); r.Do(Enf, "take"); r.Done(Enf);
                // the nearest person on the street now who is no accomplice, and walk Vale to her
                Walker? near = null; long bd = long.MaxValue; var at = r.Op(Enf).Pos;
                foreach (var w in g.Walkers) if (w.P != g.Driver && g.CrowdPos(w) is Pt p && g.S.Grid.Free(p)) { long d = G.D2(p, at); if (d < bd) { bd = d; near = w; } }
                r.Is(near != null, "someone is on the street");
                if (near == null) return;
                int trail = g.Trail, standing = g.Standing;
                for (int k = 0; k < 40 && !g.Acts.Exists(x => x.Take); k++) { if (g.CrowdPos(near) is Pt p) r.SendTo(Enf, g.S.Grid.NearestFree(p) ?? p); r.Wait(5); }
                var a = g.Acts.FirstOrDefault(x => x.Take);
                r.Is(a != null && a.Noticed.Contains(near.P.Id), $"{near.P.Id}, within 3 m, sees the grip");
                if (a == null) return;
                r.Is(near.P.Has("saw-carry"), "she holds what she saw: a carry");
                r.Eq(g.Standing, standing - g.R.StandingStep, "one step toward Menace at sight");
                r.Is(g.Trail <= trail + g.R.WitnessTrail, "witness Trail at most +1, once");
                var reps = g.Reps.Where(x => x.Act == a.Id && x.Phase != "stopped").ToList();
                if (reps.Count > 0) { r.To(reps.Min(x => x.LandAt)); r.Eq(g.Alert, 2, "her report lands: Alert 2 (a carry seen)"); }
                else r.Is(a.Noticed.Count >= 1, "she keeps quiet by her row (no report to land)");
            });
        }
    }
}
