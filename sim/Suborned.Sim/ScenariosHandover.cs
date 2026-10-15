// S01–S28: the top-down test's scenarios on the real block. "proto" is the prototype test's line. What changed and why is said in
// each; the common changes are: actions take their rulebook time and land at its end (the prototype landed them at once); Watch
// reveals the routine only and Listen the need and temperament (Sky 2026-10-15); Inez is an Idealist (Sky 2026-10-15), so Pay is
// refused and Pressure costs one step more; places are the civic block's (sim/data/handover.site.json); the guard walks 45 s
// (mission.json windows.front-door); the mission is over at the close mission.json gives (§07 8:00; Sky moves it to 8:30; the prototype ran to 9:50).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        const string Scout = "scout", Fixer = "fixer", Tech = "tech", Enf = "enforcer";
        const string Inez = "waitress", Driver = "driver", GuardR = "guard", Vale = "councillor";
        static readonly Pt Roof = new Pt(6400, -3000); // inside the Records Office
        // where the Enforcer stands to shoot the guard unseen: beside the public door, outside his door cone and both cameras
        static readonly Pt BesideDoor = new Pt(4850, -2300);
        // posts covering the service lane exit, 10 m down the lane from where the response unit stops (motive-fights.md §2.9: the cell covers the exit)
        static readonly Pt LanePostA = new Pt(6250, -5300), LanePostB = new Pt(6000, -5300);
        static Start NoFight() { var s = new Start(); s.FinalFight = false; return s; }
        static Start Turned(params string[] roles) { var s = new Start(); s.Turned.AddRange(roles); return s; }
        static int Max(params int[] xs) { int m = int.MinValue; foreach (var x in xs) m = Math.Max(m, x); return m; }

        /// <summary>Turn the driver as a player reads the card after Listen: the approach that turns him in one order, else Help again
        /// until he turns. (The cast decides: the fixture's driver is a Mercenary, so Pay; an Idealist would take one Help.)</summary>
        static void TurnDriver(Run r)
        {
            var g = r.G;
            string a = g.Preview("help", Driver).Outcome == "turned" ? "help" : g.Preview("pay", Driver).Outcome == "turned" ? "pay" : "help";
            r.Do(Fixer, "turn", Driver, a); r.Done(Fixer);
            while (!g.Driver.Turned && !g.Over && a == "help") { r.Do(Fixer, "turn", Driver, a); r.Done(Fixer); }
        }

        /// <summary>Route A as S10 plays it, to the moment the Enforcer comes out with Vale.</summary>
        static void RouteA(Run r, bool reroute = false)
        {
            var g = r.G;
            r.To(Max(r.Send(Scout, "d:Courtyard"), r.Send(Tech, "cabinet")));
            r.Do(Scout, "watch", Inez); r.Do(Tech, "loop", "entrance"); r.Done(Scout);
            r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "listen", Inez);
            r.Send(Fixer, "d:Courtyard"); r.SendTo(Enf, new Pt(4900, -1700));
            r.Done(Scout); r.Do(Fixer, "turn", Inez, "help"); r.Done(Fixer);
            while (!g.SweepSeesEntrance(g.Tick)) g.Step();
            r.Do(Fixer, "call", Inez);
            r.To(r.Send(Enf, "d:Office entrance"));
            while (g.PatrolWatching(r.At("d:Office entrance"))) g.Step();
            r.Do(Enf, "enter");
            if (reroute) r.Do(Tech, "reroute", "patrol");
            r.Done(Enf);
        }

        static void Handover()
        {
            Add("S01", "13", "The pickup camera covers the car, the entrance camera the door; neither sees through the Office; a loop blinds the entrance camera.", r =>
            {
                var g = r.G;
                r.Is(g.CameraWatching("pickup", r.At("d:Official drop-off")), "the pickup camera covers Vale's car");
                r.Is(!g.CameraWatching("pickup", Roof), "the pickup camera does not see through the Office");
                r.Is(g.CameraWatching("entrance", r.Stand("d:Office entrance")), "the entrance camera covers the door");
                r.Is(!g.CameraWatching("entrance", Roof), "the entrance camera does not see into the Office");
                // FIX → orders: the prototype set the loop directly; here the Tech walks to the cabinet and loops it
                r.To(r.Send(Tech, "cabinet")); r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                r.Is(!g.CameraWatching("entrance", r.Stand("d:Office entrance")), "a loop blinds the entrance camera");
            });

            Add("S02", "23", "5:50 Vale leaves; the unturned driver waits at the pickup; Vale walks in live time; 6:10 he is at the car.", r =>
            {
                var g = r.G;
                r.To(T(350));
                r.Eq(g.Vale, ValeAt.Leaving, "at 5:50 Vale is leaving");
                r.Eq(g.ValePos(), (Pt?)r.Stand("d:Official entrance"), "Vale comes out of the official entrance");
                r.Eq(g.CarPos(), r.At("d:Official drop-off"), "the unturned driver waits at the pickup");
                r.To(T(360));
                var mid = g.ValePos()!.Value;
                r.Is(mid != r.Stand("d:Official entrance") && mid != g.CarPos(), "Vale walks toward the car in live time");
                r.To(T(370));
                r.Eq(g.Vale, ValeAt.Pickup, "at 6:10 Vale is at the car");
                r.Eq(g.ValePos(), (Pt?)g.CarPos(), "Vale stands at the official car");
            });

            Add("S03", "36", "Called, the guard walks away in live time; behind him the Enforcer is out of his cone; a far shot leaves Inez unwitnessed; his 3 health drops him (Sky 2026-10-15 fights: health, not 2 hits); Alert 2.", r =>
            {
                var g = r.G;
                r.To(r.SendTo(Enf, new Pt(4600, -2600))); // behind the guard's walk to the café (the prototype's 46, 63 behind the door)
                r.Do(Fixer, "call", Inez);
                var atCall = g.GuardPose()!.Value.pos;
                r.Wait(T(10));
                var walking = g.GuardPose()!.Value;
                r.Is(walking.pos != atCall, "the guard walks away from the door in live time");
                r.Eq(walking.phase, "walking to café", "the guard is walking to the café");
                r.Is(!g.GuardWatching(r.Op(Enf).Pos), "behind the walking guard the Enforcer is out of his view");
                int trail = g.Trail;
                r.Fire(Enf, GuardR);
                r.Is(r.Until(() => g.GuardDown, T(30)), "fired on until down, the guard drops");
                r.Is(!g.InezWitnessedShooting, "Inez cannot witness a shot far from her café");
                r.Eq(g.Trail - trail, 4, "+2 T for the person hit, +2 T for the down (the cost on the victim, not the shot)");
                r.Eq(g.Alert, 2, "unwitnessed gunfire raises Alert to 2 (§05 any gunfire)");
            }, Turned(Inez)); // FIX → start state: Inez turned before the mission (the prototype set contacts.inez)

            Add("S04", "56", "Shooting the guard at the café: Inez witnesses, Alert 3 at once, no instant fail, the response comes in 12 s.", r =>
            {
                var g = r.G;
                r.To(r.Send(Enf, "d:Between tables")); // 7.3 m from the café door, behind where the guard will stand
                r.Do(Fixer, "call", Inez);
                r.Wait(T(g.S.GuardWalkS)); // mission.json windows.front-door: the walk (45 s, then 47; the prototype's 30)
                r.Eq(g.GuardPose()!.Value.phase, "at café", "the guard is at the café");
                r.Is(g.GuardReachedCafe, "the guard reached Inez");
                r.Fire(Enf, GuardR);
                r.Is(r.Until(() => g.Gunfire > 0, T(10)), "the first shot comes after the draw and the aim");
                r.Is(g.InezWitnessedShooting, "Inez sees violence close to her café");
                r.Eq(g.Alert, 3, "a witness makes this a high-alert route");
                r.Eq(g.Result, Outcome.None, "shooting near Inez is a choice, not an instant fail");
                r.Eq(g.SecurityArrives, g.Tick + T(12), "her report brings a faster response");
            }, Turned(Inez));

            Add("S05", "72", "The front route with the guard shot beside Inez still wins; the alarm is up, so Enter takes +30 s (Sky 2026-10-15 Q5); the debrief reads High alert and Inez witnessed.", r =>
            {
                var g = r.G;
                r.Send(Enf, "d:Between tables");
                r.To(Max(r.Send(Scout, "d:Courtyard"), r.Send(Fixer, "d:Courtyard"), r.Send(Tech, "cabinet")));
                r.Do(Scout, "watch", Inez); r.Do(Tech, "loop", "entrance");
                r.Done(Scout); r.To(r.Send(Scout, "beside-inez"));
                r.Do(Scout, "listen", Inez); r.Done(Scout);
                r.Do(Fixer, "turn", Inez, "help");
                // Changed: with every action taking its time the shot lands after the rivals park (3:10). A player who means to shoot sends
                // the Scout to tip the rivals off first; the prototype's shot came at 1:32, before the rivals were there at all.
                r.SendTo(Scout, new Pt(8250, -3100));
                r.Done(Tech); r.Do(Tech, "reroute", "patrol");
                r.Done(Fixer);
                r.Do(Fixer, "call", Inez);
                r.To(g.RivalAt); r.To(Math.Max(g.Tick, r.Arrival(Scout))); r.Do(Scout, "mislead", "rivals");
                r.To(g.LureTick + T(g.S.GuardWalkS));
                r.Fire(Enf, GuardR);
                r.Is(r.Until(() => g.GuardDown, T(30)), "the guard is down");
                r.Is(g.InezWitnessedShooting, "Inez witnesses the shooting");
                r.To(r.Send(Enf, "d:Office entrance"));
                r.Do(Enf, "holster");
                r.Do(Enf, "enter");
                r.Eq(r.Op(Enf).BusyEnd - g.Tick, T(g.R.EnterS + g.R.EnterAlarmS), "Alert ≥ 2: the staff hide Vale, Enter takes 30 s longer");
                // Changed (Sky 2026-10-15 fights): the response unit waits at the service lane; the Fixer and the Tech cover the exit
                r.To(Max(r.SendTo(Fixer, LanePostA), r.SendTo(Tech, LanePostB)));
                r.FightOff(() => g.SecurityDown || !r.Op(Enf).Inside, 120, Fixer, Tech);
                r.Is(g.SecurityDown, "the cell downs the response unit before Vale comes out");
                r.Done(Enf);
                r.Send(Enf, "d:Getaway bay");
                var car = r.At("d:Getaway bay"); int reach = g.R.AtReachCm;
                while (!g.Over && !G.Within(r.Op(Enf).Pos, car, reach)) g.Step();
                r.Do(Enf, "extract");
                r.Eq(g.Result, Outcome.Success, "a witnessed shooting leaves an escape window");
                var d = Debrief.Of(g);
                r.Eq(d.PublicSecurity, "high", "the debrief: high alert");
                r.Eq(d.InvestigationCause, "waitress-witnessed-shooting", "the debrief: Inez witnessed the shooting");
            });

            Add("S06", "95", "A shot on the guard's walk back, away from the café, does not make Inez a witness.", r =>
            {
                var g = r.G;
                r.To(r.Send(Enf, "d:Plaza east"));
                r.Do(Fixer, "call", Inez);
                r.Wait(T(g.S.GuardWalkS + g.S.GuardStayS + 15 - 2)); // walk, stay, 15 s back (the prototype's 30 + 60 + 15), less the draw and the aim
                r.Fire(Enf, GuardR);
                r.Is(r.Until(() => g.Gunfire > 0, T(10)), "the first shot");
                r.Is(g.GuardPose()!.Value.phase != "returning to door", $"seeing the weapon, or hit, on his walk back the guard stops where he is ({g.GuardPose()!.Value.phase})");
                r.Is(!g.InezWitnessedShooting, "a shot after he leaves the café does not make Inez a witness");
            }, Turned(Inez));

            Add("S07", "108", "Shooting Inez: only when forced (the Ctrl case); the contact is lost and can't be called, Alert 3, the mission goes on.", r =>
            {
                var g = r.G;
                r.To(r.Send(Enf, "d:Courtyard"));
                r.Refused(Enf, "fire", Inez, "a contact is no target unless the order forces it");
                r.Fire(Enf, Inez, force: true);
                r.Is(r.Until(() => g.Inez.Down, T(10)), "Inez is down: a civilian's 2 health, one carbine hit");
                r.Is(!g.Inez.Turned, "her contact is gone");
                r.Is(!g.CanCall(Inez), "shooting a contact removes her options");
                r.Eq(g.Alert, 3, "Alert 3");
                r.Eq(g.Result, Outcome.None, "the player can go on with a harder route");
            }, Turned(Inez));

            Add("S08", "119", "Shooting the driver (forced): contact lost, Alert 3, the driver gone, the office route remains.", r =>
            {
                var g = r.G;
                r.To(r.Send(Enf, "d:Boarding pavilion"));
                r.Fire(Enf, Driver, force: true);
                r.Is(r.Until(() => g.Driver.Down, T(10)), "the driver is down");
                r.Eq(g.Alert, 3, "Alert 3");
                r.Is(!g.Driver.Turned, "no driver contact");
                r.Eq(g.DriverPos(), (Pt?)null, "no driver at the car");
                r.Eq(g.Result, Outcome.None, "the office route remains after the pickup is lost");
            });

            Add("S09", "131", "Shooting Vale at 6:10 (forced) fails the mission.", r =>
            {
                var g = r.G;
                r.To(r.Send(Enf, "d:Boarding pavilion"));
                r.To(T(370));
                r.Fire(Enf, Vale, force: true);
                r.Until(() => g.Over, T(10));
                r.Eq(g.Result, Outcome.Failure, "killing the live capture target fails the objective");
                r.Eq(g.FailCode, "vale-dead", "the cause is Vale dead");
            });

            Add("S10", "140", "Route A, quiet: arriving shows nothing; Watch doesn't reveal the debt; Listen; loop; Help; call; wait out the patrol's sweep; enter; to the car. Calm, no shots, one contact.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Scout, "d:Courtyard"), r.Send(Tech, "cabinet")));
                r.Is(!g.Inez.KRoutine, "arriving at the café does not observe Inez");
                r.Do(Scout, "watch", Inez); r.Do(Tech, "loop", "entrance");
                r.Done(Scout);
                r.Is(!g.Inez.KNeed, "watching does not reveal Inez's debt");
                r.To(r.Send(Scout, "beside-inez"));
                r.Do(Scout, "listen", Inez);
                r.Send(Fixer, "d:Courtyard"); r.SendTo(Enf, new Pt(4900, -1700)); // the Enforcer waits east of the door, out of the guard's cone
                r.Done(Scout);
                r.Do(Fixer, "turn", Inez, "help"); r.Done(Fixer);
                while (g.SweepSeesEntrance(g.Tick) == false) g.Step(); // call when the patrol's sweep covers the door
                r.Do(Fixer, "call", Inez);
                r.To(r.Send(Enf, "d:Office entrance"));
                r.Is(g.PatrolWatching(r.At("d:Office entrance")), "the patrol covers the entrance at the first opening");
                while (g.PatrolWatching(r.At("d:Office entrance"))) g.Step();
                r.Is(!g.PatrolWatching(r.At("d:Office entrance")), "waiting for its cone to pass makes an opening");
                r.Do(Enf, "enter"); r.Done(Enf);
                r.To(r.Send(Enf, "d:Getaway bay"));
                r.Do(Enf, "extract");
                r.Eq(g.Result, Outcome.Success, "route A wins");
                r.Eq(g.Gunfire, 0, "no shots");
                var d = Debrief.Of(g);
                r.Eq(d.Contacts, 1, "one contact");
                r.Eq(d.PublicSecurity, "calm", "calm streets");
                r.Eq(d.InvestigationCause, "staged-distraction", "Inez's staged distraction can be traced");
                r.Is(d.Route.Contains("entry-timed-between-sweeps"), "the route summary: entry timed between sweeps");
            });

            Add("S11", "171", "The front, loud: the loop holds while the Tech reroutes the patrol; the guard fight; Enter +30 s with the alarm up; success. The Enforcer hit, Raised.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.Send(Enf, "d:Office approach")));
                r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                r.Is(g.PatrolWatching(r.At("d:Office entrance")) || g.SweepSeesEntrance(g.Tick + T(45)), "the patrol covers the door on its sweep");
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                r.Is(g.LoopOn, "the Tech holds the loop while diverting the patrol");
                r.Is(!g.PatrolWatching(r.At("d:Office entrance")), "the patrol is away");
                r.Fire(Enf, GuardR);
                r.Is(r.Until(() => g.GuardDown, T(30)), "the guard is down");
                r.Is(g.EnforcerHealth < g.R.HealthEnforcer, "the guard fired back while the Enforcer stood in his cone");
                r.To(r.Send(Enf, "d:Office entrance")); // Enter is at the door (rulebook cell.atReach, Sky 2026-10-15 playtest)
                r.Do(Enf, "holster");
                r.Do(Enf, "enter");
                r.Eq(r.Op(Enf).BusyEnd - g.Tick, T(g.R.EnterS + g.R.EnterAlarmS), "the alarm is up: Enter takes 30 s longer");
                // Changed (Sky 2026-10-15 Q5): the 68 s inside lets the guard's call (18 s) bring the response unit to the service lane
                // before Vale comes out; alone, the Enforcer walks into two officers and goes down. The cell covers the exit.
                // posts beside the Office approach, which the unit crosses on its way to the service lane: they meet it before Vale comes out
                r.To(Max(r.SendTo(Scout, new Pt(4500, -3500)), r.SendTo(Fixer, new Pt(4250, -3500)), r.SendTo(Tech, new Pt(4500, -3750))));
                r.FightOff(() => g.SecurityDown || !r.Op(Enf).Inside, 120, Scout, Fixer, Tech);
                r.Is(g.SecurityDown, "the cell downs the response unit before Vale comes out");
                r.Done(Enf);
                r.To(r.Send(Enf, "d:Getaway bay"));
                r.Do(Enf, "extract");
                r.Eq(g.Result, Outcome.Success, "the loud front wins");
                r.Is(g.EnforcerHealth < g.R.HealthEnforcer, "the Enforcer was hit");
                r.Eq(Debrief.Of(g).PublicSecurity, "raised", "Raised");
            });

            Add("S12", "197", "An unprepared shot: the guard sees the weapon drawn, Alert 2 and his call (+18 s), no instant fail; his cone is directional; he fires back after his 2 s react.", r =>
            {
                var g = r.G;
                r.To(r.Send(Enf, "d:Office approach"));
                r.Fire(Enf, GuardR);
                r.Wait(1);
                var seen = g.Log.Find(l => l.Code == "drawn.seen");
                r.Is(seen.Code == "drawn.seen" && seen.Text.Contains("guard"), "the guard sees the weapon drawn (a drawn weapon is an act)");
                r.Eq(g.Alert, 2, "a drawn weapon seen by security: Alert 2");
                r.Eq(g.SecurityArrives, seen.Tick + T(18), "the guard calls it in: the response in 18 s");
                r.Is(g.GuardWatching(r.Op(Enf).Pos), "the guard sees the Enforcer in front of the door");
                r.Is(!g.GuardWatching(BesideDoor), "the guard has a directional cone");
                r.Is(r.Until(() => r.Shots().Exists(x => x.by == "guard"), T(10)), "the guard fires");
                r.Eq(r.Shots().Find(x => x.by == "guard").tick, seen.Tick + T(g.R.ReactS), "he fires back after his react");
                r.Is(r.Until(() => g.GuardDown, T(30)), "the guard is down");
                r.Eq(g.Result, Outcome.None, "a report escalates the mission instead of ending it");
            });

            Add("S13", "210", "Out of the guard's cone but in the patrol's: the patrol reports the drawn weapon and the gunfire (+18 s).", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, BesideDoor)));
                r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                while (!g.PatrolWatching(r.Op(Enf).Pos)) g.Step();
                r.Is(!g.GuardWatching(r.Op(Enf).Pos), "out of the guard's cone");
                r.Is(g.PatrolWatching(r.Op(Enf).Pos), "in the patrol's cone");
                r.Fire(Enf, GuardR);
                r.Wait(1);
                r.Eq(g.Result, Outcome.None, "no instant fail");
                r.Eq(g.SecurityArrives, g.Tick + T(18), "the patrol calls an early response");
                r.Is(g.Log.Exists(l => l.Code == "security.called" && l.Text.Contains("patrol reports")), "the log says the patrol reported it");
            });

            Add("S14", "226", "The guard down: his missed check-in (+18 s) is logged; the response arrives with a cone; escape is still open.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.Send(Enf, "d:Office approach")));
                r.Do(Tech, "loop", "entrance"); r.Done(Tech); r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                r.Fire(Enf, GuardR); r.Is(r.Until(() => g.GuardDown, T(30)), "the guard is down");
                r.Eq(g.MissingNotice, g.Tick + T(18), "the check-in is missed 18 s later");
                r.To(g.MissingNotice);
                r.Is(g.Log.Exists(l => l.Code == "guard.check-in"), "the log: he misses his check-in");
                r.To(g.SecurityArrives);
                r.Is(g.SecurityPose().HasValue, "the response unit reaches the street");
                r.Is(g.SecurityWatching(g.SecurityPose()!.Value.pos + new Pt(200, 0)) || g.SecurityWatching(g.SecurityPose()!.Value.pos), "it has a cone");
                r.Eq(g.Result, Outcome.None, "the player can still escape during the response");
            });

            Add("S15", "242", "An unseen shot: +45 s; the guard can't fire outside his cone; the missed check-in brings it to +20 s.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, BesideDoor)));
                r.Do(Tech, "loop", "entrance"); r.Done(Tech); r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                r.Is(!g.GuardWatching(r.Op(Enf).Pos), "out of the guard's cone");
                r.Fire(Enf, GuardR);
                r.Is(r.Until(() => g.Gunfire > 0, T(10)), "the first shot");
                r.Eq(g.SecurityArrives, g.Tick + T(45), "an unseen shot draws a slower response");
                r.Is(r.Until(() => g.GuardDown, T(30)), "the guard is down");
                r.Eq(g.EnforcerHealth, g.R.HealthEnforcer, "the guard cannot fire outside his cone");
                r.To(g.MissingNotice);
                r.Eq(g.SecurityArrives, g.Tick + T(20), "the missed check-in speeds up the response");
            });

            Add("S16", "259", "Gunfire brings the rivals in 16 s; a patrol within 30 m breaks off to chase, cone visible.", r =>
            {
                var g = r.G;
                r.To(r.Send(Enf, "d:Office approach"));
                var before = g.PatrolPose()!.Value.pos;
                r.Fire(Enf, GuardR);
                r.Is(r.Until(() => g.Gunfire > 0, T(10)), "the first shot");
                r.Eq(g.RivalAt, g.Tick + T(16), "gunfire draws the rivals in ahead of schedule");
                r.Is(g.PatrolChasing, "the patrol nearby breaks its sweep to pursue");
                r.Wait(T(1));
                var after = g.PatrolPose()!.Value.pos; var e = r.Op(Enf).Pos;
                r.Is(G.D2(after, e) < G.D2(before, e), "the patrol moves toward the shooter in live time");
                r.Is(g.PatrolPose().HasValue && !g.PatrolDown, "the pursuing patrol keeps a visible cone");
            });

            Add("S17", "274", "A rerouted patrol doesn't chase; the rivals still come from the east street, spot the Enforcer and fire after their 2 s react.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, BesideDoor)));
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                r.Fire(Enf, GuardR);
                r.Is(r.Until(() => g.Gunfire > 0, T(10)), "the first shot");
                r.Is(!g.PatrolChasing, "rerouting prevents the chase");
                r.Eq(g.RivalAt, g.Tick + T(16), "the rivals still hear the gunfire");
                r.Until(() => g.GuardDown, T(30));
                // FIX → orders: the prototype moved the Enforcer to the service lane by hand; here he walks onto the rivals' way in
                r.SendTo(Enf, new Pt(4200, -1000));
                int limit = g.RivalAt + T(60);
                while (!g.RivalsTracking && g.Tick < limit && !g.Over) g.Step();
                r.Is(g.RivalsPose().HasValue, "the rivals come in visibly from the east street");
                r.Is(g.RivalsTracking, "the rival crew spots the Enforcer");
                int spotted = g.Tick;
                r.Is(r.Until(() => r.Shots().Exists(x => x.by == "rivals"), T(10)), "the rivals fire");
                r.Eq(r.Shots().Find(x => x.by == "rivals").tick, spotted + T(g.R.ReactS), "after their react");
            });

            Add("S18", "293", "The patrol's 4 health: two carbine hits disable it; its cone goes; the shots draw the rivals.", r =>
            {
                var g = r.G;
                r.To(r.Send(Enf, "d:Office approach"));
                r.To(T(100));
                r.Fire(Enf, "patrol");
                r.Is(r.Until(() => g.PatrolHealth < g.R.HealthPatrol, T(20)), "a hit");
                r.Eq(g.PatrolHealth, g.R.HealthPatrol - g.R.Carbine.Damage, "one carbine hit: 2 of 4");
                r.Is(r.Until(() => g.PatrolDown, T(30)), "two hits disable the patrol");
                r.Is(!g.PatrolWatching(r.Op(Enf).Pos), "its cone is gone");
                r.Is(g.RivalsDrawn, "shooting the patrol also draws the rivals");
            });

            Add("S19", "307", "The response unit (two officers, Sky 2026-10-15) sees across the street, reacts before firing (2 s), and can be downed; the seal clock keeps running.", r =>
            {
                var g = r.G;
                // Changed: the prototype shot the guard at the door, then moved the Enforcer across the map by hand. Here the cell plays it:
                // the patrol is rerouted, the guard is lured and shot from behind in the square (unseen: the response comes in 45 s), and
                // the Enforcer walks to the north street, where the response unit comes in (sim/data site security-entry).
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, new Pt(1400, -3300))));
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                r.Do(Fixer, "call", Inez);
                while (!g.Over && !(g.GuardPose()!.Value.pos.X < 500 && G.Within(r.Op(Enf).Pos, g.GuardPose()!.Value.pos, 1400) && !g.GuardWatching(r.Op(Enf).Pos))) g.Step();
                r.Fire(Enf, GuardR);
                r.Is(r.Until(() => g.Gunfire > 0, T(10)), "the first shot");
                r.Is(!g.InezWitnessedShooting && !g.PatrolChasing, "unwitnessed, and the patrol is away");
                int arrive = g.SecurityArrives;
                r.Eq(arrive, g.Tick + T(45), "an unseen shot: the response in 45 s");
                r.Until(() => g.GuardDown, T(30));
                // Changed (Sky 2026-10-15 fights): the guard drops in 2 s, so his missed check-in calls the unit sooner; the run is loud, so
                // running is free: the Enforcer runs to the north street
                var (ran, _) = g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "move", Point = new Pt(-3400, -3400), Run = true });
                r.Is(ran, "the Enforcer runs (free: the run is loud)");
                int there = r.Arrival(Enf);
                r.To(g.MissingNotice); arrive = g.SecurityArrives; // his missed check-in calls them sooner (the prototype's shot and check-in did too)
                r.Is(there <= arrive, "the Enforcer reaches the north street before the unit");
                r.Is(r.Until(() => g.SecurityWatching(r.Op(Enf).Pos), T(20)), "the arriving unit sees across the street");
                int sighted = g.Tick;
                r.Eq(g.EnforcerHealth, g.R.HealthEnforcer, "security takes aim rather than firing on sight");
                r.Is(r.Until(() => r.Shots().Exists(x => x.by.StartsWith("security-", StringComparison.Ordinal)), T(10)), "the response unit fires");
                r.Eq(r.Shots().Find(x => x.by.StartsWith("security-", StringComparison.Ordinal)).tick, sighted + T(g.R.ReactS), "after its react");
                r.Fire(Enf, "security");
                r.Is(r.Until(() => g.SecGuns[0].Down || g.SecGuns[1].Down, T(20)), "one officer down");
                r.Fire(Enf, "security");
                r.Is(r.Until(() => g.SecurityDown, T(20)), "the Enforcer can fight back against the response unit");
                r.Eq(g.SecurityPose(), null, "the unit is gone");
                r.Is(g.Lockdown > g.Tick, "the wider cordon is still closing");
            }, Turned(Inez));

            Add("S20", "327", "Route B, quiet: Watch and Listen at the car; turn the driver before 5:40; false tip on the rivals; 6:10 the service lane; take Vale; to the car. No shots.", r =>
            {
                var g = r.G;
                r.Send(Enf, "d:Service lane");
                r.To(Max(r.Send(Scout, "d:Meet driver"), r.Send(Fixer, "d:Boarding pavilion")));
                r.Do(Scout, "watch", Driver); r.Done(Scout);
                r.Do(Scout, "listen", Driver); r.Done(Scout); // Sky 2026-10-15: Listen, not Watch, gives the need (the prototype's watch did)
                TurnDriver(r);
                r.Is(g.Driver.Turned, "the driver is turned");
                r.To(r.SendTo(Scout, new Pt(8250, -3100)));
                r.To(T(320));
                r.Do(Scout, "mislead", "rivals");
                r.To(T(370));
                r.Is(g.PickupService, "the pickup moved to the service lane");
                r.Do(Enf, "take"); r.Done(Enf);
                r.To(r.Send(Enf, "d:Getaway bay"));
                r.Do(Enf, "extract");
                r.Eq(g.Result, Outcome.Success, "route B wins");
                r.Eq(g.Gunfire, 0, "no shots");
                r.Is(g.Chain.Contains("rivals-false-tip"), "the rivals followed a false tip");
            });

            var drv = Turned(Driver); drv.Known[Driver] = new List<string> { "routine", "need", "temperament" };
            Add("S21", "347", "Route B, camera-block twice and a reroute: the car blocks after 8 s, returns 2:30 + 8 s later; Vale walks to the moving car; the debrief lists 2 blockades and 1 reroute.", r =>
            {
                var g = r.G;
                // FIX → start state: the driver is turned at the start. With actions taking their time a natural turn lands near 3:20,
                // and two blockades (2 × 2:38) no longer fit before 5:32; the start state keeps the scenario's intent, the mechanics.
                r.Send(Enf, "d:Service lane"); r.Send(Scout, "d:Meet driver"); r.Send(Tech, "cabinet");
                r.Do(Fixer, "call", Driver);
                r.Eq(g.CarPos(), r.At("d:Official drop-off"), "the car starts at Vale's pickup");
                r.Is(g.PickupCameraLive(), "the camera stays live while the car moves");
                r.To(g.BlockedFrom);
                r.Eq(g.CarPos(), r.At("block-spot"), "the car parks in the sightline");
                r.Is(!g.PickupCameraLive(), "the camera is blocked only once the car is there");
                r.To(g.BlockedUntil + T(8));
                r.Eq(g.CarPos(), r.At("d:Official drop-off"), "the car returns after the blockade");
                r.Do(Fixer, "call", Driver);
                r.Do(Tech, "reroute", "patrol");
                r.To(r.SendTo(Scout, new Pt(8250, -3100)));
                r.To(T(320)); r.Do(Scout, "mislead", "rivals");
                r.To(T(355));
                var car = g.CarPos();
                r.Is(car != r.At("d:Official drop-off") && car != r.Stand("d:Service lane"), "the driver takes the car round to the service lane");
                r.Eq(g.Vale, ValeAt.Leaving, "Vale is leaving");
                r.Is(g.ValePos() != r.Stand("d:Official entrance") && g.ValePos() != r.Stand("d:Service lane"), "Vale walks round to meet the moving car");
                r.To(T(370));
                r.Eq(g.CarPos(), r.Stand("d:Service lane"), "the car arrives at the service lane with Vale");
                r.Eq(g.ValePos(), (Pt?)g.CarPos(), "Vale at the car");
                r.Is(g.PickupCameraLive(), "leaving the pickup restores its camera");
                r.Do(Enf, "take"); r.Done(Enf);
                r.To(r.Send(Enf, "d:Getaway bay")); r.Do(Enf, "extract");
                var d = Debrief.Of(g);
                r.Eq(d.Route.Count > 0 ? d.Route[0] : "", "driver-moves-vale-to-service-lane", "the route summary leads with the driver");
                r.Is(d.Extra.Contains("2 call:driver"), "the debrief lists 2 camera blockades");
                r.Is(d.Extra.Contains("1 reroute"), "and 1 patrol reroute");
                r.Eq(d.InvestigationCause, "several-interventions", "several interventions left a trail");
            }, drv);

            Add("S22", "386", "Gating: no action at a distance; an unknown person isn't callable; the Fixer and the Scout must stand beside Inez; a turned contact is callable.", r =>
            {
                var g = r.G;
                r.Refused(Scout, "watch", Inez, "a distant person has no interaction");
                r.Is(!g.CanCall(Inez), "an unknown person is not a contact to call");
                r.To(r.Send(Scout, "d:Courtyard"));
                r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.Refused(Fixer, "turn", Inez, "the Fixer must approach Inez too", "help");
                r.Refused(Scout, "listen", Inez, "the Scout must stand beside Inez to hear the debt (3 m)");
                r.To(r.Send(Scout, "beside-inez"));
                r.Do(Scout, "listen", Inez); r.Done(Scout);
                r.To(r.Send(Fixer, "d:Courtyard"));
                r.Do(Fixer, "turn", Inez, "help"); r.Done(Fixer);
                r.Is(g.CanCall(Inez), "a turned contact can be called");
                r.To(r.Send(Enf, "d:Courtyard"));
                r.Refused(Enf, "fire", GuardR, "being near Inez does not reach the guard (carbine max, a clear line)");
            });

            Add("S23", "402", "Ask cold: refused and remembered (+1 T), can't be repeated; routine alone doesn't unlock Help; after Listen, Help at +$10,000, T 3.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Courtyard"));
                r.Do(Fixer, "turn", Inez, "ask"); r.Done(Fixer);
                r.Is(!g.Inez.Turned, "a cold approach is refused");
                r.Is(g.Inez.Has("refusal"), "Inez remembers the refusal");
                r.Eq(g.Trail, 1, "Inez remembers the suspicious stranger");
                r.Refused(Fixer, "turn", Inez, "the same failed approach cannot be repeated", "ask");
                r.To(r.Send(Scout, "d:Courtyard"));
                r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.Refused(Fixer, "turn", Inez, "routine alone does not unlock debt relief", "help");
                r.To(r.Send(Scout, "beside-inez"));
                r.Do(Scout, "listen", Inez); r.Done(Scout);
                r.Do(Fixer, "turn", Inez, "help"); r.Done(Fixer);
                r.Is(g.Inez.Turned, "new information lets the Fixer recover from the refusal");
                r.Eq(g.Cash, 50000L, "the cold approach raises the debt relief price");
                r.Eq(g.Trail, 3, "the refused approach leaves an extra trail");
            });

            Add("S24", "421", "Watch unlocks Pay, shown at $70,000 with \"?\"; Help stays locked; Inez is an Idealist, so Pay is refused and remembered (Sky 2026-10-15).", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "d:Courtyard"));
                r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.To(r.Send(Fixer, "d:Courtyard"));
                var q = g.Preview("pay", Inez);
                r.Eq(q.Lines[0].Usd, 70000L, "routine unlocks an expensive offer: $70,000");
                r.Eq(q.Outcome, "?", "before Listen the card shows the outcome as \"?\"");
                r.Eq(q.WorstUsd, 90000L, "and asks cash for the worst case");
                r.Refused(Fixer, "turn", Inez, "debt relief is still locked", "help");
                r.Do(Fixer, "turn", Inez, "pay"); r.Done(Fixer);
                r.Is(!g.Inez.Turned, "an Idealist refuses the fee");
                r.Eq(g.Cash, 100000L, "the fee stays with the cell");
                r.Eq(g.Trail, 2, "the offer still leaves its trail");
                r.Is(g.Inez.Has("refusal"), "she remembers the refusal");
                r.Is(!g.Inez.KTemperament && g.Preview("pay", Inez).Outcome == "refused", "the cell learns only that she refuses a fee (✗), not her temperament");
            });

            Add("S25", "434", "Pressure: $0, +4 T (one step worse on an Idealist), and she can be called.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "d:Courtyard")); r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "listen", Inez); r.Done(Scout);
                r.To(r.Send(Fixer, "d:Courtyard"));
                r.Do(Fixer, "turn", Inez, "pressure"); r.Done(Fixer);
                r.Is(g.Inez.Turned, "she agrees under pressure");
                r.Eq(g.Cash, 100000L, "pressure avoids the payment");
                r.Eq(g.Trail, 4, "pressure leaves a larger trail: +3, +1 the wrong approach");
                r.Is(g.CanCall(Inez), "she can be called");
            });

            Add("S26", "448", "The Tech's tether: leaving the cabinet ends the loop.", r =>
            {
                var g = r.G;
                r.To(r.Send(Tech, "cabinet")); r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                r.Is(!g.CameraWatching("entrance", r.Stand("d:Office entrance")), "the loop holds");
                r.To(r.SendTo(Tech, r.Stand("cabinet") + new Pt(-900, 0)));
                r.Is(!g.LoopOn, "leaving the cabinet ends the loop");
                r.Is(g.CameraWatching("entrance", r.Stand("d:Office entrance")), "the camera is live again");
            });

            Add("S27", "457", "Orders can't target the Office; the path bends round it; the operative never stands inside.", r =>
            {
                var g = r.G;
                var (ok, _) = g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "move", Point = Roof });
                r.Is(!ok, "orders cannot target the Office");
                int at = r.Send(Scout, "d:Meet driver");
                r.Is(r.Op(Scout).Walk!.Points.Length > 2, "the route bends round the Office");
                bool outside = true;
                while (g.Tick < at) { g.Step(); var p = r.Op(Scout).Pos; if (g.S.InsideOccluder(p) || !g.S.Grid.Free(p)) outside = false; }
                r.Is(outside, "the operative stays outside the Office");
                r.Eq(r.Op(Scout).Pos, r.Stand("d:Meet driver"), "and arrives where sent");
            });

            Add("S28", "471", "Engaging nearby stops the approach walk.", r =>
            {
                var g = r.G;
                r.Send(Scout, "d:Café door");
                while (!G.Within(r.Op(Scout).Pos, g.PersonPos(g.Inez)!.Value, g.R.ActReachCm)) g.Step();
                r.Is(r.Op(Scout).Walk != null, "the Scout is still approaching");
                r.Do(Scout, "watch", Inez);
                r.Is(r.Op(Scout).Walk == null, "engaging nearby stops movement");
            });

            NCases();
        }

        static void NCases()
        {
            Add("N08", "—", "Withdraw ends the attempt: outcome withdrawn, not a fail (§07).", r =>
            {
                var g = r.G;
                r.Wait(T(30));
                r.Is(g.Issue(new Order { Tick = g.Tick, Do = "withdraw" }).ok, "withdraw is taken");
                r.Eq(g.Result, Outcome.Withdrawn, "the attempt ends as withdrawn");
                r.Eq(g.FailCode, "", "no fail cause");
            });

            Add("N10", "—", "Final fight, loud (§07): after shots the police come from the north street, 2–4 officers, the seal clock running, no allies; the Enforcer fights them off at the car.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, BesideDoor)));
                r.Do(Tech, "loop", "entrance"); r.Done(Tech); r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                r.Fire(Enf, GuardR); r.Until(() => g.GuardDown, T(30));
                r.Is(g.GuardDown, "the guard is down: a loud run");
                r.To(r.Send(Enf, "d:Office entrance"));
                r.Do(Enf, "enter");
                r.Eq(r.Op(Enf).BusyEnd - g.Tick, T(g.R.EnterS + g.R.EnterAlarmS), "Alert ≥ 2: Enter takes 30 s longer (Sky 2026-10-15 Q5)");
                r.Done(Enf);
                r.Eq(g.FightBranch, "loud", "any shot makes the final fight loud");
                r.Is(g.Hostiles.Count >= 2 && g.Hostiles.Count <= 4, $"2–4 officers ({g.Hostiles.Count})");
                r.Is(g.Lockdown != int.MaxValue / 4, "the seal clock is running");
                r.Is(!g.GuardAlly, "no allies");
                foreach (var op in new[] { Scout, Fixer, Tech }) r.Do(op, "draw"); // the fight is on: the whole armed cell (Sky Q1)
                r.Send(Enf, "d:Getaway bay");
                r.FightItOut(120, Scout, Fixer, Tech);
                r.Do(Enf, "extract");
                r.Eq(g.Result, Outcome.Success, "the loud route wins through the fight");
                var d = Debrief.Of(g);
                r.Eq(d.FightBranch, "loud", "the debrief names the loud fight");
                r.Is(g.Tick - g.FightStart >= T(30) || d.FightDownedByEnforcer == d.FightHostiles, "the fight took its time");
            });

            Add("N11", "—", "Final fight, safe (§07): route A, quiet, then the rival crew ambushes the extraction; the helped Inez warns 10 s early and the guard, who never saw the cell, fights them.", r =>
            {
                var g = r.G;
                RouteA(r, reroute: true); // the Tech diverts the patrol while the Enforcer is inside: the fight's gunfire would draw it (§05 [B] 30 m)
                r.Eq(g.FightBranch, "safe", "no shots, Alert ≤ 1: the final fight is safe");
                r.Eq(g.Hostiles.Count, 5, "five rivals: no driver turned, no false tip");
                r.Is(g.FightWarned, "the helped Inez warns");
                r.Is(g.GuardAlly, "the guard never saw the cell, so he fights them");
                r.Do(Enf, "draw");
                r.Send(Enf, "d:Getaway bay");
                while (!g.Over && g.FightSprung == int.MaxValue / 4) g.Step();
                r.Is(g.Hostiles.TrueForAll(h => h.Gun.NextShot == g.FightSprung + g.R.AimNewTicks + T(10)), "lying in wait they would fire 1 s after springing; Inez's warning holds them 10 s more");
                r.FightItOut();
                r.Do(Enf, "extract");
                r.Eq(g.Result, Outcome.Success, "route A wins through the ambush");
                r.Is(g.DownedByGuard > 0, "the guard dropped at least one of them");
                var dd = Debrief.Of(g);
                r.Is(dd.FightWhy.Contains("waitress-warns") && dd.FightWhy.Contains("guard-fights-them"), "the debrief says why: Inez's warning, the guard");
            });

            Add("N11b", "—", "Final fight, safe, prepared (§07): route B with the driver turned (he blocks the rival car, −1) and the rivals tipped off earlier (−1): three rivals.", r =>
            {
                var g = r.G;
                r.Send(Enf, "d:Service lane");
                r.To(Max(r.Send(Scout, "d:Meet driver"), r.Send(Fixer, "d:Boarding pavilion")));
                r.Do(Scout, "watch", Driver); r.Done(Scout); r.Do(Scout, "listen", Driver); r.Done(Scout);
                TurnDriver(r);
                r.Send(Fixer, "d:Getaway bay"); // the support waits at the car (motive-fights.md §2.9)
                r.To(r.SendTo(Scout, new Pt(8250, -3100))); r.To(T(320)); r.Do(Scout, "mislead", "rivals"); r.Done(Scout);
                r.Send(Scout, "d:Getaway bay");
                r.To(T(370)); r.Do(Enf, "take"); r.Done(Enf);
                r.Eq(g.FightBranch, "safe", "a quiet route B: the final fight is safe");
                r.Eq(g.Hostiles.Count, 3, "three rivals: the turned driver and the earlier false tip take one each");
                foreach (var op in new[] { Enf, Scout, Fixer, Tech }) r.Do(op, "draw"); // with Vale in hand the fight is on its way: drawing is no new act (the final fight is untraced)
                r.Send(Enf, "d:Getaway bay");
                r.FightItOut(120, Scout, Fixer, Tech);
                r.Is(Array.TrueForAll(g.Ops, o => !o.Down), "nobody down");
                r.Do(Enf, "extract");
                r.Eq(g.Result, Outcome.Success, "the prepared route wins");
            });

            Add("N12", "—", "People step their routines (§04, brief 010): at each tick everyone stands at the stop the people file gives, or walks between two stops, or is off the street.", r =>
            {
                var g = r.G;
                r.Is(g.Walkers.Count > 100, $"the civic block's people are in ({g.Walkers.Count})");
                bool atStops = true; int walking = 0, onStreet0 = 0;
                Walker? mover = null;
                for (int t = 0; t <= T(480); t += T(30))
                {
                    r.To(t);
                    foreach (var w in g.Walkers)
                    {
                        var at = g.CrowdPos(w);
                        int i = Array.FindIndex(w.From, f => f <= t);
                        bool inStop = false;
                        for (int k = 0; k < w.Place.Length; k++)
                            if (t >= w.From[k] && t < w.To[k]) { inStop = true; if (w.Out[k] ? at != g.People2!.Places[w.Place[k]] : at != null) atStops = false; }
                        if (!inStop && at.HasValue) { walking++; mover ??= w; }
                        if (t == 0 && at.HasValue) onStreet0++;
                    }
                }
                r.Is(atStops, "everyone at a stop stands at its place, or is off the street (at home, inside, beyond)");
                r.Is(onStreet0 > 10, $"people are out on the street at 0:00 ({onStreet0})");
                r.Is(walking > 0 && mover != null, $"someone walks between stops during the mission ({mover?.P.Id})");
                if (mover != null)
                {
                    int k = 0; while (k + 1 < mover.Place.Length && !(mover.To[k] <= g.Tick && g.Tick < mover.From[k + 1])) k++;
                    var seen = new List<Pt>();
                    for (int t = 0; t <= T(480); t += Game.Hz) { var p = g.CrowdPos(mover, t); if (p.HasValue && !seen.Contains(p.Value)) seen.Add(p.Value); }
                    r.Is(seen.Count > 5, $"{mover.P.Name} moves in live time ({seen.Count} positions)");
                }
            });

            Add("N12b", "—", "The crowd witnesses (§05): the Fixer helps Inez with people at the café; a witness adds Trail once, moves standing toward Hero (§04 clear debts publicly) and remembers the Fixer's face.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "d:Courtyard")); r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "listen", Inez); r.Done(Scout);
                r.To(r.Send(Fixer, "d:Courtyard"));
                int nearby = 0; var door = r.At("d:Café door");
                foreach (var w in g.Walkers) { var p = g.CrowdPos(w); if (p.HasValue && G.Within(p.Value, door, 1300)) nearby++; }
                r.Is(nearby > 0, $"people stand near the café door ({nearby})");
                int trail = g.Trail;
                r.Do(Fixer, "turn", Inez, "help"); r.Done(Fixer);
                r.Is(g.Inez.Turned, "Inez is turned");
                r.Eq(g.Trail - trail, 2, "+1 T for the Help, +1 T for the witnessed debt-clearing (once, however many saw it)");
                r.Eq(g.Standing, 1, "a public debt-clearing moves standing toward Hero");
                r.Is(g.Walkers.Exists(w => g.HoldsFace(w.P)), "a witness remembers the Fixer's face");
                r.Is(g.Ledger.Exists(l => l.cause == "witnessed:debt-cleared:waitress"), "the ledger names the witnessed act");
            });

            var drvKnown = Turned(Driver); drvKnown.Known[Driver] = new List<string> { "routine", "need", "temperament" };
            Add("N16", "—", "Watchers log the cell (§05): the pickup camera logs the Fixer beside the driver; a watcher that logs an operative beside a contact exposes the contact's job (Motive's review #12), so the next mission's ladder moves resentment, not bravery.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Boarding pavilion"));
                r.Wait(T(2));
                r.Is(g.Sightings.Exists(s => s.Watcher == "camera:pickup" && s.Op == "fixer"), "the pickup camera logged the Fixer, with place and time");
                r.Is(g.ExposedByWatcher.Contains(Driver), "logged beside the driver: the driver is exposed");
                r.Do(Fixer, "call", Driver);
                r.To(T(200));
                var next = g.BetweenMissions(g.Investigate());
                r.Is(next.Lines.Exists(l => l.Line == "ladder" && l.Who == Driver && l.Detail.StartsWith("job-exposed", StringComparison.Ordinal)), "the driver's job was exposed: resentment, not bravery");
            }, drvKnown);

            Add("N17", "—", "The investigation (§05, rulebook investigation): a pressured Inez is a Talker and gives up the Fixer's face to the investigators, with the cause in words (K7); a helped Inez lies, and in a neutral district that gives them nothing.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "d:Courtyard")); r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "listen", Inez); r.Done(Scout);
                r.To(r.Send(Fixer, "d:Courtyard")); r.Do(Fixer, "turn", Inez, "pressure"); r.Done(Fixer);
                var inv = g.Investigate();
                var f = inv.Findings.Find(x => x.Who == Inez);
                r.Is(f != null && f.Outcome == "gives" && f.Facts.Contains("fixer-face"), "questioned, Inez gives up the Fixer's face");
                r.Is(f != null && f.Cause.Contains(">"), $"with the cause: {f?.Cause}");
                r.Eq(inv.NamedCause, "named-witness:waitress", "a named witness: the investigation's cause");
            });

            Add("N17b", "—", "The investigation, the other way: a helped Inez (bravery helper, loyalty obliged) is no Talker and covers for the cell (people.rules talker.lie); in a neutral district she gives the investigators nothing.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "d:Courtyard")); r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "listen", Inez); r.Done(Scout);
                r.To(r.Send(Fixer, "d:Courtyard")); r.Do(Fixer, "turn", Inez, "help"); r.Done(Fixer);
                var inv = g.Investigate();
                var f = inv.Findings.Find(x => x.Who == Inez);
                r.Is(f != null && f.Outcome == "lies" && f.Facts.Count == 1 && f.Facts[0] == "nothing", "questioned, Inez lies: nothing");
                r.Eq(inv.NamedCause, "", "nobody named the cell");
            });

            Add("N18", "—", "Between missions (rulebook phases): after route A, the investigators question Inez, so her job is exposed (resentment compliant → resentful); her met debt is replaced from her own stream (Q2), told free as she is a contact; obliged, she passes no word (K3, Motive #10); she can introduce along her family and workmate ties (K5).", r =>
            {
                var g = r.G;
                RouteA(r, reroute: true);
                r.Send(Enf, "d:Getaway bay");
                while (!g.Over && g.FightSprung == int.MaxValue / 4) g.Step();
                r.FightItOut(); r.Do(Enf, "extract");
                r.Eq(g.Result, Outcome.Success, "route A wins");
                var next = Debrief.Of(g).Next;
                // café customers saw the debt cleared, so the investigators question Inez: questioned, her job is exposed (rulebook exposure)
                var asked = Debrief.Of(g).Inv.Findings.Exists(f => f.Who == Inez);
                r.Is(asked, "the investigators reached Inez through the witnessed debt-clearing");
                r.Is(next.Lines.Exists(l => l.Line == "ladder" && l.Who == Inez && l.Detail == "job-exposed: resentment compliant → resentful"), "questioned, her job is exposed: resentment compliant → resentful");
                var need = next.Lines.Find(l => l.Line == "need-replaced" && l.Who == Inez);
                r.Is(need != null && need.Detail.StartsWith("debt → ", StringComparison.Ordinal) && need.Detail.EndsWith("(told free: a contact)", StringComparison.Ordinal), $"her debt is replaced and told free ({need?.Detail})");
                r.Is(!next.Lines.Exists(l => l.Line == "word" && l.Who == Inez), "obliged, she passes no word");
                r.Is(next.Lines.Exists(l => l.Line == "introduced" && l.Who == Inez && l.Detail.Contains("(family)")), "she can introduce her family");
                r.Is(!next.Ambush, "no ambush is scheduled");
            });

            Add("N19", "—", "Running (Sky 2026-10-15: double-click runs): the Scout runs to the courtyard at the rulebook's run speed and arrives sooner than the Fixer walking the same distance, by exactly the ticks the two paces give.", r =>
            {
                var g = r.G;
                int t0 = g.Tick;
                int scoutAt = r.Send(Scout, "d:Courtyard", run: true);
                long len = r.Op(Scout).Walk!.Length;
                int fixerAt = r.Send(Fixer, "d:Courtyard");
                long flen = r.Op(Fixer).Walk!.Length;
                int runTicks = (int)((len + g.R.RunSpeed - 1) / g.R.RunSpeed), walkTicks = (int)((flen + g.R.CellSpeed - 1) / g.R.CellSpeed);
                r.Eq(scoutAt - t0, runTicks, $"the run takes ceil({len} mm / {g.R.RunSpeed} mm a tick) ticks");
                r.Eq(fixerAt - t0, walkTicks, $"the walk takes ceil({flen} mm / {g.R.CellSpeed} mm a tick) ticks");
                r.To(scoutAt);
                r.Eq(r.Op(Scout).Pos, r.Stand("d:Courtyard"), "the Scout is there at the tick the run gives");
                r.Is(r.Op(Fixer).Walk != null, "the Fixer is still walking");
                r.To(fixerAt);
                r.Eq(r.Op(Fixer).Pos, r.Stand("d:Courtyard"), "the Fixer arrives at the tick the walk gives");
                r.Is(walkTicks - runTicks > 0, $"running saved {walkTicks - runTicks} ticks");
            });

            Add("N19b", "—", "No running while carrying Vale (Sky 2026-10-15 Q3: a man being taken walks): the run order is refused with its reason; a walk is taken.", r =>
            {
                var g = r.G;
                r.Send(Enf, "d:Service lane");
                r.To(Max(r.Send(Scout, "d:Meet driver"), r.Send(Fixer, "d:Boarding pavilion")));
                r.Do(Scout, "watch", Driver); r.Done(Scout); r.Do(Scout, "listen", Driver); r.Done(Scout);
                TurnDriver(r);
                r.To(r.SendTo(Scout, new Pt(8250, -3100))); r.To(T(320)); r.Do(Scout, "mislead", "rivals");
                r.To(T(370)); r.Do(Enf, "take"); r.Done(Enf);
                var (ok, why) = g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "move", To = "d:Getaway bay", Run = true });
                r.Is(!ok && why.Contains("Vale"), $"carrying Vale, the run is refused: {why}");
                r.Eq(g.QuoteMove(Enf, r.Stand("d:Getaway bay")).RunRefused, why, "the move chip shows the same reason");
                r.Send(Enf, "d:Getaway bay");
                r.Eq(r.Op(Enf).WalkSpeed, g.R.CellSpeed, "he walks");
            }, NoFight());

            var known = new Start(); known.Known[Inez] = new List<string> { "routine", "need", "temperament" }; known.Known[Driver] = new List<string> { "routine", "need", "temperament" };
            var purse = new Start(); purse.Known[Inez] = known.Known[Inez]; purse.Known[Driver] = known.Known[Driver];
            purse.Temperament[Driver] = "idealist"; // §04's purse is for a driver one Help turns (Motive §3); the fixture cast's driver is a Mercenary
            Add("N09", "—", "The purse (§04): Help Inez + Help the driver = $80,000 fits $100,000.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Courtyard")); r.Do(Fixer, "turn", Inez, "help"); r.Done(Fixer);
                r.To(r.Send(Fixer, "d:Boarding pavilion")); r.Do(Fixer, "turn", Driver, "help"); r.Done(Fixer);
                r.Is(g.Inez.Turned && g.Driver.Turned, "both turned");
                r.Eq(g.Cash, 20000L, "$80,000 spent of $100,000");
            }, purse);

            var merc = new Start(); merc.Temperament[Inez] = "mercenary"; merc.Known[Inez] = new List<string> { "routine", "need", "temperament" }; merc.Known[Driver] = new List<string> { "routine", "need", "temperament" };
            Add("N09b", "—", "The purse the other way (§04): Pay on a Mercenary Inez ($70,000) leaves too little to Help the driver ($40,000): the order can't be given.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Courtyard")); r.Do(Fixer, "turn", Inez, "pay"); r.Done(Fixer);
                r.Is(g.Inez.Turned, "a Mercenary takes the fee");
                r.Eq(g.Cash, 30000L, "$70,000 spent");
                r.To(r.Send(Fixer, "d:Boarding pavilion"));
                r.Refused(Fixer, "turn", Driver, "$110,000 is over the purse: the driver can't be helped", "help");
            }, merc);

            var tip = new Start(); tip.Known[Inez] = new List<string> { "routine", "need" };
            Add("N14", "—", "The card (Sky Q3, Q4): \"?\" until the temperament is known; a refused approach fails and is remembered; the wrong approach costs one step.", r =>
            {
                var g = r.G;
                var q = g.Preview("help", Inez);
                r.Eq(q.Outcome, "?", "with the need known but not the temperament, Help's outcome shows \"?\"");
                r.Eq(q.Lines.Find(l => l.Line == "one-more-help")!.Holds, "?", "and its wrong-approach line shows \"?\"");
                r.Eq(g.Preview("ask", Inez).Outcome, "refused", "Ask in a neutral district reads refused: every temperament refuses it");
                r.Eq(g.Preview("pay", Inez).Reports, "?", "whether she would report a fee shows \"?\" (a Loyal person would, Motive's review: the risk line)");
                r.Eq(q.Reports, "no", "Help is reported by no temperament in a neutral district");
                r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "listen", Inez); r.Done(Scout);
                r.Eq(g.Preview("help", Inez).Outcome, "turned", "after Listen the card says Help turns her");
                r.Eq(g.Preview("pay", Inez).Outcome, "refused", "and that Pay is refused");
                r.Eq(g.Preview("pressure", Inez).Trail, 4L, "and that Pressure, the wrong approach, costs +4 T");
            }, tip);

            Add("N15", "—", "K1, K2, K8 in the mission: Help writes a memory entry with the Fixer's face; gratitude is +1 loyalty; the price is line items.", r =>
            {
                var g = r.G;
                var q = g.Preview("help", Inez);
                r.Eq(q.Lines.Count, 4, "four line items: base, refused before, one more Help, trust over time");
                r.Eq(q.Lines[0].Line, "base", "the first is the base");
                r.To(r.Send(Fixer, "d:Courtyard")); r.Do(Fixer, "turn", Inez, "help"); r.Done(Fixer);
                var m = g.Inez.Memory.Find(x => x.Act == "help");
                r.Is(m.Act == "help" && m.By == "fixer" && m.Place == "d:Café door", "memory: help, by the Fixer's face, at the café door (K1)");
                r.Is(g.HoldsFace(g.Inez), "she holds the Fixer's face (the dossier's face bit)");
                r.Eq(g.Inez.Sc[Rules.Loyalty], 1, "gratitude: loyalty obliged (K2)");
                r.Is(g.Ledger.Exists(l => l.what == "trail" && l.cause == "turn.help:waitress"), "the Trail ledger names the cause");
            }, known);

            FightCases();
            CancelCases();
        }
    }
}
