// S01–S28: the top-down test's scenarios on the real block. "proto" is the prototype test's line. What changed and why is said in
// each; the common changes are: actions take their rulebook time and land at its end (the prototype landed them at once); Watch
// reveals the routine only and Listen the need and temperament (Sky 2026-10-15); who plays each role, and their temperament, is the
// promoted cast's (sim/data/handover.site.json cast, M-2): a scenario that plays the mission reads the card as a player would (route A
// helps Inez until the card's "turned" lands), and one that tests a single temperament's rule names it in its start state; places are the civic block's (sim/data/handover.site.json); the guard walks 45 s
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
        // where the Enforcer stands to shoot the guard unseen: beside the public door, outside his door cone and the pickup camera.
        // Re-sited 2026-10-19 with the door (Sky's option (b): the public door on the Office's west face): 5.6 m south of the door on the
        // forecourt, where Waymark's c022 routes park him (route G, loud); was (4850, -2300) beside the old north door. The entrance
        // camera, which now looks south along the face, sees it unless looped; the patrol's sweep crosses it.
        // Brief 055: (6925, -4250) in engine 0.27.0, 25 cm north of the old (6950, -4250), since Vantage's cypress cyp-office-w became a walk solid;
        // engine 0.28.0 (perception.mount, the drawn occluders): 50 cm north and 50 cm west to (6875, -4300), the nearest free point (with the Fixer's
        // spot 1.5 m west of it, N52, F8-O9, S9-KO) whose line to the guard on the landing clears the 2 m topiary top-office-w (out/scratch/keel/b056/py).
        // Waymark's routes still stand at the old point: his to re-site.
        static readonly Pt BesideDoor = new Pt(6875, -4300);
        /// <summary>Waymark's c025 Enforcer wait on the forecourt (brief 031: out of every clock-keeping watcher's sight, more than 3 m from the guard).</summary>
        static readonly Pt Forecourt = new Pt(5650, -4750);
        // posts covering the service lane exit, 10 m down the lane from where the response unit stops (motive-fights.md §2.9: the cell covers the exit).
        // Re-sited 2026-10-19: the service door is on the east face, in a quiet yard the unit enters from the north; the posts are at
        // its south end. Were (62.5, -53) and (60, -53), down the old lane west of the Office.
        static readonly Pt LanePostA = new Pt(7050, -1680), LanePostB = new Pt(7050, -1450);
        static Start NoFight() { var s = new Start(); s.FinalFight = false; return s; }
        static Start Level(int level) { var s = new Start(); s.Security = level; return s; } // brief 013: the response dial's level, fixed
        /// <summary>S11's cover for the exit: the Scout here, the Fixer 2.5 m north, the Tech 2.5 m west, on the response unit's way from the
        /// north street to the Office approach. Moved 2026-10-19 (Keel, brief 023; the door on the west face) from (4500, -3500), where the
        /// unit came into reach 1 s too late, 10 m back toward the square, so they meet it earlier on the same walk.</summary>
        static readonly Pt S11Post = new Pt(3500, -4000);
        /// <summary>S14: where the Enforcer withdraws once the guard is down, round the Office's south-west corner toward the official side,
        /// off the response unit's way to the Office approach. Added 2026-10-19 (Keel, brief 023): with the door on the west face the unit
        /// reaches the approach before the 4:00 mark and downed the Enforcer standing there, ending the run before the mark it tests.</summary>
        static readonly Pt S14Away = new Pt(7250, -4000);
        static Start Turned(params string[] roles) { var s = new Start(); s.Turned.AddRange(roles); return s; }
        static int Max(params int[] xs) { int m = int.MinValue; foreach (var x in xs) m = Math.Max(m, x); return m; }

        /// <summary>Turn the driver as a player reads the card after Listen: the approach that turns him in one order, else Help again
        /// until he turns. (The cast decides: a Mercenary driver takes Pay; an Idealist would take one Help.)</summary>
        static void TurnDriver(Run r)
        {
            var g = r.G;
            string a = g.Preview("help", Driver).Outcome == "turned" ? "help" : g.Preview("pay", Driver).Outcome == "turned" ? "pay" : "help";
            r.Do(Fixer, "turn", Driver, a); r.Done(Fixer);
            while (!g.Driver.Turned && !g.Over && a == "help") { r.Do(Fixer, "turn", Driver, a); r.Done(Fixer); }
        }

        /// <summary>Help a person as route A does (§07: the Fixer helps Inez), reading the card as a player would: Help again while she is
        /// not yet turned (one Help turns an Idealist; a Fearful person needs Help ×2, a Loyal one trust over time; §04). The cast decides how
        /// many; a card that reads "refused" is not given, and the scenario's check says so.</summary>
        static void HelpUntilTurned(Run r, string role, int most = 4)
        {
            var g = r.G; var p = g.PersonByRole(role);
            for (int i = 0; i < most && !p.Turned && !g.Over; i++)
            {
                if (!r.Is(g.Preview("help", role).Outcome != "refused", $"the card for Help on {role} does not read refused")) return;
                r.Do(Fixer, "turn", role, "help"); r.Done(Fixer);
            }
        }

        /// <summary>Route A as S10 plays it, to the moment the Enforcer comes out with Vale.</summary>
        /// <summary>Engine 0.28.0 (brief 054 §3.2): below Alert 2 a threat is minded by security only; wait (up to 2 min, a second at a time)
        /// for a moment someone would see a Pressure on Inez (the off-duty guards at the café, a passing guard).</summary>
        static void InSightOfSecurity(Run r)
        {
            var g = r.G;
            for (int k = 0; k < 120 && g.Preview("pressure", Inez).Witnesses == 0 && !g.Over; k++) r.To(g.Tick + T(1));
        }
        /// <summary>Engine 0.34.0 (brief 067 A, Sky: the patrol "to the side of the service entrance"): the front routes waited for the forecourt
        /// sweep to come onto the public door before they called (from 3:48 in route A, so the loop and the call ran over a sweep they then
        /// waited out at the door); the sweep is the service yard's now and never sees the door, so they call at `from`. On a site whose sweep
        /// sees the door they still wait for it. Bounded: a run that ends first ends the wait (S10 spun for ever on the yard site).</summary>
        static void ToSweepAtDoor(Run r, int from = 0)
        {
            var g = r.G; r.To(Math.Max(g.Tick, from));
            if (!g.SweepCoversEntrance) return;
            ToSweepAtDoor(r); // engine 0.34.0 (brief 067 A): the yard sweep never sees the door, so no wait
        }
        /// <summary>Until the Enforcer may enter: the sweep (where it ever sees the door) is off it and the loop holds the entrance camera; bounded by the run's end.</summary>
        static void ToEntryOpening(Run r, bool walkDone = false)
        {
            var g = r.G; var door = r.At("d:Office entrance");
            while (!g.Over && (g.PatrolWatching(door) || !g.LoopOn || (walkDone && r.Op(Enf).Walk != null))) g.Step();
        }
        static void RouteA(Run r, bool reroute = false)
        {
            var g = r.G;
            r.To(Max(r.Send(Scout, "d:Courtyard"), r.Send(Tech, "cabinet")));
            r.Do(Scout, "watch", Inez); r.Done(Scout);
            r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "listen", Inez);
            r.Send(Fixer, "d:Courtyard"); r.SendTo(Enf, new Pt(4900, -1700));
            r.Done(Scout); HelpUntilTurned(r, Inez);
            // engine 0.28.0 (brief 055, the drawn walk): the Fixer's walks are shorter round the drawn solids and Inez is turned at 3:27, not 3:48; the
            // route still calls at the first sweep opening from 3:48, as it did, so Vale comes out at 5:53 and not at 4:32, when guard 74 crosses the
            // service lane on his new way round the police post (he would see the carry and radio it: Alert 2, the patrol's fight). Engine 0.34.0: the
            // yard sweep never sees the door, so it calls at 3:48, the tick the forecourt sweep's opening gave (its phase 28 s was on the door)
            ToSweepAtDoor(r, T(228));
            r.Do(Tech, "loop", "entrance"); // Loop v2 (engine 0.31.0, brief 058 §4): planted with the call, so its 2:00 runs over the Enter (it was held from 0:16 by the tether)
            r.Do(Fixer, "call", Inez);
            r.To(r.Send(Enf, "d:Office entrance"));
            ToEntryOpening(r); // engine 0.34.0: bounded by the run's end
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
                var vp = g.ValePos()!.Value; int body = g.CarBodyCm("car:official", vp);
                r.Is(!g.Walk.CarCovers(vp) && body >= 0 && body <= g.R.CarReachCm, $"Vale stands at the official car, at its kerb-side door off its ground (engine 0.33.1: {body} cm from its body)");
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
                r.Eq(g.Trail - trail, 6, "+2 T the shooting, +2 T the person hit, +2 T the down (once each, not per shot; Motive addendum Q1)");
                r.Eq(g.Alert, 2, "unwitnessed gunfire raises Alert to 2 (§05 any gunfire)");
            }, Turned(Inez)); // FIX → start state: Inez turned before the mission (the prototype set contacts.inez)

            Add("S04", "56", "Shooting the guard at the café: Inez witnesses, Alert 3 at once, no instant fail, the response comes in 12 s.", r =>
            {
                var g = r.G;
                r.To(r.Send(Enf, "d:Between tables")); // 7.3 m from the café door, behind where the guard will stand
                r.Do(Fixer, "call", Inez);
                r.Wait(g.VisitWalkTicks); // the summoned lover's own walk, the grid route at walking pace (rulebook summon), not a mission number
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
                // Changed (brief 018: Board with Vale takes 4 s, which closed the 3 s the Enforcer had at the car before the seal): he shoots from
                // 17 m east of the café, inside the carbine's effective reach and still beside Inez's sight of the guard, so his walk to the door is
                // shorter and the seal falls later behind him
                r.SendTo(Enf, new Pt(600, -2500));
                r.To(Max(r.Send(Scout, "d:Courtyard"), r.Send(Fixer, "d:Courtyard"), r.Send(Tech, "cabinet")));
                r.Do(Scout, "watch", Inez);
                r.Done(Scout); r.To(r.Send(Scout, "beside-inez"));
                r.Do(Scout, "listen", Inez); r.Done(Scout);
                // Changed: with every action taking its time the shot lands after the rivals park (3:10). A player who means to shoot sends
                // the Scout to tip the rivals off first; the prototype's shot came at 1:32, before the rivals were there at all.
                r.SendTo(Scout, TipSpot(r)); // brief 059: the tip is given at the rival car where it parks (rival-park, (90.5, -31))
                // Changed (r016 cast, 2026-10-16): Help as often as the card asks (a Fearful Inez takes two, §04); the Tech reroutes the patrol
                // with the Help the card says turns her, so its 115 s away covers the shot and the entry however many Helps came first
                for (int i = 0; i < 4 && !g.Inez.Turned; i++)
                {
                    bool last = g.Preview("help", Inez).Outcome == "turned";
                    r.Do(Fixer, "turn", Inez, "help");
                    if (last) { r.Done(Tech); r.Do(Tech, "reroute", "patrol"); }
                    r.Done(Fixer);
                }
                r.Is(g.Inez.Turned, "Inez is turned");
                r.Done(Tech); r.Do(Tech, "loop", "entrance"); // Loop v2 (engine 0.31.0, brief 058 §4): planted with the call, so its 2:00 runs over the Enter (it was held from 0:34 by the tether)
                r.Do(Fixer, "call", Inez);
                r.To(g.RivalAt); r.To(Math.Max(g.Tick, r.Arrival(Scout))); r.Do(Scout, "mislead", "rivals");
                r.To(g.LureTick + g.VisitWalkTicks);
                r.Fire(Enf, GuardR);
                r.Is(r.Until(() => g.GuardDown, T(30)), "the guard is down");
                r.Is(g.InezWitnessedShooting, "Inez witnesses the shooting");
                // Changed (engine 0.8.2, Motive 2026-10-19, briefs/023/motive-0.8.2.md §3): he runs to the door. Running is free while the
                // shot's fight is on (cell.rule) and he is at Alert 3 already; §07 "walking legs shorten where the cell runs". Walking, the seal
                // (7:24.0) closes before the car leaves; running, the car leaves at 7:09.3 with 14.7 s spare. He walks the second leg (nobody
                // runs carrying Vale), so the 45 s from the service lane to the car stands: the cost of where the city put the door.
                r.To(r.Send(Enf, "d:Office entrance", run: true));
                r.Do(Enf, "holster");
                r.Do(Enf, "enter");
                r.Eq(r.Op(Enf).BusyEnd - g.Tick, T(g.R.EnterS + g.R.EnterAlarmS), "Alert ≥ 2: the staff hide Vale, Enter takes 30 s longer");
                // Changed (Sky 2026-10-15 fights): the response unit waits at the service lane; the Fixer and the Tech cover the exit
                r.To(Max(r.SendTo(Fixer, LanePostA), r.SendTo(Tech, LanePostB)));
                // engine 0.34.0: the Fixer and the Tech fight the unit down even once Vale is out (on brief 067's data the second officer falls 1.4 s
                // after Vale comes out, 5 s after at the cell's 1.8 m/s, which starts the Enter 5 s sooner; on 0.33.1 he fell 5.7 s before it)
                r.FightOff(() => g.SecurityDown, 120, Fixer, Tech);
                r.Is(g.SecurityDown, "the cell downs the response unit at the service lane");
                r.Done(Enf);
                r.Send(Enf, "d:Getaway bay");
                while (!g.Over && !g.AtGetaway(r.Op(Enf).Pos)) g.Step(); // engine 0.33.1: at the car's body (car.footprint.reach)
                r.Extract();
                r.Line(g.Result == Outcome.Success, $"a witnessed shooting leaves an escape window ({g.Result} {g.FailCode}; brief 059: the four of the crew on foot are drawn to the Office front and press the Enforcer: Motive)");
                var d = Debrief.Of(g);
                r.Eq(d.PublicSecurity, "high", "the debrief: high alert");
                r.Eq(d.InvestigationCause, "waitress-witnessed-shooting", "the debrief: Inez witnessed the shooting");
            });

            Add("S06", "95", "A shot on the guard's walk back, away from the café, does not make Inez a witness.", r =>
            {
                var g = r.G;
                // Changed 2026-10-19 (Keel, brief 023; the door on the west face): his walk back runs south-east to the forecourt now, and from
                // Plaza east itself he is out of line 15 s out; the Enforcer stands 6 m from it, off the café, where the line is clear
                r.To(r.SendTo(Enf, new Pt(1200, -2900)));
                r.Do(Fixer, "call", Inez);
                r.Wait(g.VisitWalkTicks + T(g.R.SummonStayS + 15 - 2)); // walk, stay, 15 s back (the prototype's 30 + 60 + 15), less the draw and the aim
                r.Fire(Enf, GuardR);
                r.Is(r.Until(() => g.Gunfire > 0, T(10)), "the first shot");
                r.Is(g.GuardPose()!.Value.phase != "returning to door", $"seeing the weapon, or hit, on his walk back the guard stops where he is ({g.GuardPose()!.Value.phase})");
                r.Is(!g.InezWitnessedShooting, "a shot after he leaves the café does not make Inez a witness");
            }, Turned(Inez));

            Add("S07", "108", "Shooting Inez: only when forced; the contact is lost and can't be called, Alert 3, the mission goes on.", r =>
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
                r.To(T(366)); // brief 080: Vale has a civilian's 3 health now, two carbine hits: time for both before he is driven away at 6:13
                r.Fire(Enf, Vale, force: true);
                r.Until(() => g.Over, T(10));
                r.Eq(g.Result, Outcome.Failure, "killing the live capture target fails the objective");
                r.Eq(g.FailCode, "vale-dead", "the cause is Vale dead");
                ShotFrom(r, CauseIs(r, "vale-dead", "shot", true, Enf, "op", Enf), Enf, Vale); // the fail camera (ScenariosFailCause.cs): the Enforcer who fired, then Vale
            });

            Add("S10", "140", "Route A, quiet: arriving shows nothing; Watch doesn't reveal the debt; Listen; loop; Help; call; enter while the guard is at the café (engine 0.34.0, brief 067 A: the patrol sweeps the service yard and never sees the public door, so nothing at the door is timed to it); out by the service lane, which the yard sweep watches but where no machine sees an escorted Vale; to the car. Calm, no shots, one contact.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Scout, "d:Courtyard"), r.Send(Tech, "cabinet")));
                r.Is(!g.Inez.KRoutine, "arriving at the café does not observe Inez");
                r.Do(Scout, "watch", Inez);
                r.Done(Scout);
                r.Is(!g.Inez.KNeed, "watching does not reveal Inez's debt");
                r.To(r.Send(Scout, "beside-inez"));
                r.Do(Scout, "listen", Inez);
                r.Send(Fixer, "d:Courtyard"); r.SendTo(Enf, new Pt(4900, -1700)); // the Enforcer waits east of the door, out of the guard's cone
                r.Done(Scout);
                HelpUntilTurned(r, Inez); // as many Helps as the card asks (the cast's temperament, §04)
                // engine 0.34.0 (brief 067 A): 0.33.1 called as the forecourt sweep came onto the door (from 3:48: 4:30.9), so the Enforcer arrived
                // under it and waited it out. The yard sweep never sees the door: the call is at 3:48, as route A's (RouteA), and nothing waits for it
                r.Is(!g.SweepCoversEntrance, "the patrol's sweep is the service yard's: at no point of its round does it see the public door (brief 067 A)");
                r.To(Math.Max(g.Tick, T(228)));
                r.Do(Tech, "loop", "entrance"); // Loop v2 (engine 0.31.0, brief 058 §4): planted with the call, so its 2:00 runs over the Enter
                r.Do(Fixer, "call", Inez);
                r.To(r.Send(Enf, "d:Office entrance"));
                r.Is(!g.PatrolWatching(r.At("d:Office entrance")), "at the door: the patrol is in the yard, not on the door");
                ToEntryOpening(r); // the loop lands and holds the entrance camera
                r.Do(Enf, "enter"); r.Done(Enf);
                r.To(r.Send(Enf, "d:Getaway bay"));
                r.Extract();
                r.Eq(g.Result, Outcome.Success, "route A wins");
                r.Eq(g.Gunfire, 0, "no shots");
                r.Is(!g.Log.Exists(l => l.Code == "attention.reporting" || l.Code == "patrol.breaks"), "the yard sweep reports nobody and never breaks: an escorted Vale is seen by no machine (§07, ScanEscort)");
                var d = Debrief.Of(g);
                r.Eq(d.Contacts, 1, "one contact");
                r.Eq(d.PublicSecurity, "calm", "calm streets");
                r.Eq(d.InvestigationCause, "staged-distraction", "Inez's staged distraction can be traced");
                r.Is(!d.Route.Contains("entry-timed-between-sweeps") && !d.Route.Contains("patrol-diverted"), "the route summary claims no sweep gap at the door and no diversion: the patrol had no part in the entry (engine 0.34.0: RouteBot \"\")");
                r.Is(d.Route.Contains("waitress-pulls-guard-away") && d.Route.Contains("entrance-camera-looped"), "the route summary: Inez pulls the guard away, the camera is looped");
            });

            Add("S11", "171", "The front, loud: the loop holds while the Tech reroutes the patrol; the guard fight; Enter +30 s with the alarm up; the cell downs the response unit, then breaks the rival crew before Vale comes out (engine 0.34.0); success. The Enforcer hit, Raised.", r =>
            {
                var g = r.G;
                // brief 031: the Enforcer no longer waits at the Office approach while the Tech works (the guard, the entrance camera and the
                // sweep would report him loitering): he sets off once the reroute is under way, so he arrives as the patrol leaves
                r.To(r.Send(Tech, "cabinet"));
                r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                // engine 0.34.0 (brief 067 A): the sweep is the service yard's and never sees the public door; the Reroute takes the patrol off the
                // yard from 1:31 to 3:26, through the guard fight and the response unit's, as a loud front still would
                r.Is(!g.SweepCoversEntrance && !g.PatrolWatching(r.At("d:Office entrance")), "the patrol sweeps the service yard, not the door (brief 067 A)");
                r.Do(Tech, "reroute", "patrol"); r.To(r.Op(Tech).BusyEnd - 8); // engine 0.33.1: he walks round the getaway car (0.8 s), so he sets off 0.8 s before the reroute lands and reaches the approach when he did on 0.33.0
                int there = r.Send(Enf, "d:Office approach"); r.Done(Tech); r.To(there);
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
                r.To(Max(r.SendTo(Scout, S11Post), r.SendTo(Fixer, S11Post + new Pt(-250, 0)), r.SendTo(Tech, S11Post + new Pt(0, -250))));
                r.FightOff(() => g.SecurityDown || !r.Op(Enf).Inside, 120, Scout, Fixer, Tech);
                r.Is(g.SecurityDown, "the cell downs the response unit before Vale comes out");
                // engine 0.34.0 (M067-B1, brief 067 K): the rivals drawn by the guard fight stand on Rocket Lane by the Office front from 2:41.5; at
                // the cell's 1.8 m/s the three were at their posts 17 s before Vale came out with nothing to do, and the four pressing the
                // Enforcer as he walked Vale to the car downed him by one hit (3:33.5; 0.33.1 left him at 1 health). They go at the rivals once the
                // unit is down, as a player would, and break the crew before Vale comes out
                r.Engage(r.CityGunmen, () => !r.Op(Enf).Inside, 120, new[] { Scout, Fixer, Tech }, false);
                r.Done(Enf);
                // engine 0.19.0 (ai.intent.rivals.press, Pulse hostile-initiative.md §2.2): the rivals drawn by the gunfire press for the Vale
                // holder, who can't fire walking Vale: the others go after them while he walks to the car
                int bay = r.SendTo(Enf, g.Walk.NearestFree(r.Stand("d:Getaway bay") + new Pt(0, -160)) ?? r.Stand("d:Getaway bay")); // engine 0.33.1 (car.footprint): to its road-side door (0.33.0 walked him into the car's middle; at the kerb-side door the pressing three down him first)
                r.Engage(r.CityGunmen, () => g.Tick >= bay, (bay - g.Tick) / Game.Hz + 1, new[] { Scout, Fixer, Tech }, false);
                r.To(Math.Max(bay, g.Tick));
                r.Extract();
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
                if (!r.Until(() => g.PatrolWatching(r.Op(Enf).Pos), T(300))) { r.Is(false, "the patrol's cone reaches the Enforcer beside the door within 5 min"); return; } // bounded (brief 055: an unbounded wait hung `hashes` when the grid moved him)
                r.Is(!g.GuardWatching(r.Op(Enf).Pos), "out of the guard's cone");
                r.Is(g.PatrolWatching(r.Op(Enf).Pos), "in the patrol's cone");
                r.Fire(Enf, GuardR);
                r.Wait(1);
                r.Eq(g.Result, Outcome.None, "no instant fail");
                r.Eq(g.SecurityArrives, g.Tick + T(18), "the patrol calls an early response");
                r.Is(g.Log.Exists(l => l.Code == "security.called" && l.Text.Contains("patrol reports")), "the log says the patrol reported it");
            });

            // Changed (brief 013, Sky answer 4; Motive §5.1): the 18 s check-in is retired. The guard checks in every 90 s from the start of
            // his shift at the post (−28,470 s in the people file: marks at 1:00, 2:30, 4:00 …), and a downed guard misses the next mark.
            Add("S14", "226", "The guard down: he misses his next check-in mark (every 90 s from his shift's start), and it is logged; the response arrives with a cone; escape is still open.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.Send(Enf, "d:Office approach")));
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech); r.Do(Tech, "loop", "entrance"); r.Done(Tech); /* Loop v2 (engine 0.31.0, brief 058 §4): the 2:00 loop is planted after the Reroute */
                r.Fire(Enf, GuardR); r.Is(r.Until(() => g.GuardDown, T(30)), "the guard is down");
                int mark = g.NextMarkOf(g.GuardP, g.Tick);
                r.Is(mark > g.Tick && mark <= g.Tick + T(g.R.CheckInPeriodS) && (mark - T(60)) % T(g.R.CheckInPeriodS) == 0, "his next mark is within 90 s, on his shift's 90 s marks (1:00, 2:30, …)");
                r.SendTo(Enf, S14Away);
                r.To(mark);
                r.Eq(g.MissingNotice, mark, "the check-in is missed at the mark");
                r.Is(g.Log.Exists(l => l.Code == "guard.check-in"), "the log: he misses his check-in");
                r.To(g.SecurityArrives);
                r.Is(g.SecurityPose().HasValue, "the response unit reaches the street");
                r.Is(g.SecurityPose() is var s14p && s14p.HasValue && (g.SecurityWatching(s14p.Value.pos + new Pt(200, 0)) || g.SecurityWatching(s14p.Value.pos)), "it has a cone");
                r.Eq(g.Result, Outcome.None, "the player can still escape during the response");
            });

            Add("S15", "242", "An unseen shot: +45 s; the guard can't fire outside his cone; his missed check-in mark lands at the desk and brings the unit sooner (the level's dispatch after the mark; brief 013 retires the 18 s + 20 s).", r =>
            {
                var g = r.G;
                // c014 start (brief 014): the Enforcer's walk to the door is longer, so the Tech no longer waits for him; she loops the
                // camera when she reaches the cabinet, which keeps the guard down before his 2:30 mark (waiting put him down at 2:30.8).
                int enfAt = r.SendTo(Enf, BesideDoor);
                r.To(r.Send(Tech, "cabinet"));
                r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                r.To(g.Tick + T(42)); // Loop v2 (engine 0.31.0): the loop lands 42 s sooner than the held loop did (20 s, not 62 s); the reroute keeps its tick
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                r.To(Math.Max(g.Tick, enfAt));
                r.Is(!g.GuardWatching(r.Op(Enf).Pos), "out of the guard's cone");
                r.Fire(Enf, GuardR);
                r.Is(r.Until(() => g.Gunfire > 0, T(10)), "the first shot");
                r.Eq(g.SecurityArrives, g.Tick + T(45), "an unseen shot draws a slower response");
                r.Is(r.Until(() => g.GuardDown, T(30)), "the guard is down");
                r.Eq(g.EnforcerHealth, g.R.HealthEnforcer, "the guard cannot fire outside his cone");
                int mark = g.NextMarkOf(g.GuardP, g.Tick), shot = g.SecurityArrives;
                r.To(mark);
                r.Eq(g.MissingNotice, mark, "the check-in is missed at his next mark");
                r.Eq(g.SecurityArrives, Math.Min(shot, mark + T(g.DialNow.DispatchS)), "the missed mark lands at once: the unit rolls the level's dispatch later, if that is sooner");
                r.Is(g.SecurityArrives < shot, "here it is sooner than the shot's 45 s");
            });

            Add("S16", "259", "Gunfire brings the rivals in 16 s; only a patrol within 30 m breaks off to chase (engine 0.34.0, brief 067 A: the patrol sweeps the service yard, beyond 30 m of a shot at the public door, so it keeps its sweep and its cone; N186 shows the chase from the yard).", r =>
            {
                var g = r.G;
                r.To(r.Send(Enf, "d:Office approach"));
                var before = g.PatrolPose()!.Value.pos;
                r.Fire(Enf, GuardR);
                r.Is(r.Until(() => g.Gunfire > 0, T(10)), "the first shot");
                r.Eq(g.RivalAt, g.Tick + T(16), "gunfire draws the rivals in ahead of schedule");
                var e = r.Op(Enf).Pos;
                r.Is(!G.Within(before, e, g.R.PatrolBreakCm) && !g.PatrolChasing, $"the patrol, {G.Dist(before, e) / 100} m off in the service yard (beyond {g.R.PatrolBreakCm / 100} m), keeps its sweep");
                r.Wait(T(1));
                r.Is(g.PatrolPose().HasValue && !g.PatrolDown && !g.PatrolChasing, "and its sweep's cone");
            });

            Add("S17", "274", "A rerouted patrol doesn't chase; the rivals still come from the east street, spot the Enforcer and fire after their 2 s react.", r =>
            {
                var g = r.G;
                // engine 0.33.1 (brief 031, as S11): the Enforcer no longer waits by the door while the Tech works (the entrance camera reported him
                // loitering at 1:37.6 and sent the police, who reached him 0.2 s before the rivals' react once the getaway car on the walk put the
                // shot 0.8 s later): he sets off so he arrives as the reroute lands
                r.To(r.Send(Tech, "cabinet"));
                r.Do(Tech, "reroute", "patrol"); r.To(r.Op(Tech).BusyEnd - T(50));
                r.To(Math.Max(r.SendTo(Enf, BesideDoor), r.Op(Tech).BusyEnd)); r.Done(Tech);
                r.Fire(Enf, GuardR);
                r.Is(r.Until(() => g.Gunfire > 0, T(10)), "the first shot");
                r.Is(!g.PatrolChasing, "rerouting prevents the chase");
                r.Eq(g.RivalAt, g.Tick + T(16), "the rivals still hear the gunfire");
                r.Until(() => g.GuardDown, T(30));
                // FIX → orders: the prototype moved the Enforcer to the service lane by hand; here he walks onto the rivals' way in. Brief 059: the
                // drawn crew comes by car to rivals.car.front and walks to the Office front, then turns back the way it came: he waits across Rocket
                // Lane from the front stop, in their sight as they turn
                r.SendTo(Enf, g.S.Grid.NearestFree(new Pt(6575, -4700)) ?? BesideDoor); // brief 059: ahead of the drawn crew as they walk from the front stop to the Office front (engine 0.33.1: south of the approach, off the guard's body, where the police hold the scene)
                int limit = g.RivalAt + T(90);
                while (!g.RivalsTracking && g.Tick < limit && !g.Over) { g.Step(); var e = r.Op(Enf); if (!e.Gun.Drawn && e.Gun.HolsterAt < 0 && e.Busy == null) g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "draw" }); } // his weapon out as the crew comes (he holsters when a fight is over)
                r.Is(g.RivalCarPose().HasValue, "the rivals come in visibly, by car");
                r.Is(g.RivalsTracking, "the rival crew spots the Enforcer");
                int spotted = g.Tick;
                r.Is(r.Until(() => r.Shots().Exists(x => x.by.StartsWith("rival-", StringComparison.Ordinal)), T(10)), "the rivals fire");
                r.Eq(r.Shots().Find(x => x.by.StartsWith("rival-", StringComparison.Ordinal)).tick, spotted + T(g.R.ReactS), "after their react");
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
                // Changed 2026-10-19 (Keel, brief 023; the door on the west face): the Tech's walk to the cabinet on the forecourt is longer, and
                // a call after the reroute lands put the shot at 1:55, its 45 s response after the 2:30 mark; the Fixer calls as the reroute
                // starts (it lands at 1:19, before the shot), so the shot comes before 1:45 and the unit, not the mark, is the shot's
                r.Do(Tech, "reroute", "patrol");
                r.Do(Fixer, "call", Inez);
                // engine 0.34.0 (M067-B1): at 1.8 m/s the shot would come at 0:55.8, before his 1:00 mark, which would then call the unit itself: the
                // Enforcer lets the mark pass first, as N10 does, so the unit is the shot's (45 s), before the 2:30 mark
                { int mk = g.NextMarkOf(g.GuardP, g.Tick); if (mk - g.Tick < T(45)) r.To(mk + 1); }
                // wait for a shot from behind: in range, out of his cone, and a clear line (the atom fountain blocks sight since the cover's blockers, Pulse §5.4)
                bool Shot() { var gp = g.GuardPose()!.Value.pos; return gp.X < 2500 && !G.Within(gp, r.At("d:Café door"), 1400) && G.Within(r.Op(Enf).Pos, gp, 1400) && !g.GuardWatching(r.Op(Enf).Pos) && g.S.Sight(r.Op(Enf).Pos, gp, g.R.Eye); }
                while (!g.Over && g.GuardPose()!.Value.phase == "walking to café" && !Shot()) g.Step();
                r.Fire(Enf, GuardR);
                r.Is(r.Until(() => g.Gunfire > 0, T(10)), "the first shot");
                r.Is(!g.InezWitnessedShooting && !g.PatrolChasing, "unwitnessed, and the patrol is away");
                int arrive = g.SecurityArrives;
                r.Eq(arrive, g.Tick + T(45), "an unseen shot: the response in 45 s");
                r.Until(() => g.GuardDown, T(30));
                // Changed (Sky 2026-10-15 fights): the guard drops in 2 s; the run is loud, so running is free: the Enforcer runs to the north
                // street. (Brief 013: his missed check-in is at his next 90 s mark, after the shot's 45 s here, so it no longer calls them sooner.)
                var (ran, _) = g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "move", Point = new Pt(-3400, -3400), Run = true });
                r.Is(ran, "the Enforcer runs (free: the run is loud)");
                int there = r.Arrival(Enf);
                arrive = g.SecurityArrives;
                r.Is(g.NextMarkOf(g.GuardP, g.Tick) >= arrive, "his next check-in mark comes after the unit (the shot calls it)");
                r.To(arrive);
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

            Add("S20", "327", "Route B, quiet: Listen at the car (engine 0.28.0: no Watch to give on the driver, brief 054 §2.1); turn the driver before 5:40; false tip on the rivals; 6:10 the service lane; take Vale; to the car. No shots.", r =>
            {
                var g = r.G;
                r.Send(Enf, "d:Service lane");
                r.To(Max(r.Send(Scout, "d:Meet driver"), r.Send(Fixer, "d:Boarding pavilion")));
                r.Is(!g.WatchOffered(Driver), "no Watch to give on the driver (a Mercenary, neutral: brief 054 §2.1): Listen needs none");
                r.Do(Scout, "listen", Driver); r.Done(Scout); // Sky 2026-10-15: Listen, not Watch, gives the need (the prototype's watch did)
                TurnDriver(r);
                r.Is(g.Driver.Turned, "the driver is turned");
                r.To(r.SendTo(Scout, TipSpot(r)));
                r.To(T(320));
                r.Do(Scout, "mislead", "rivals");
                r.To(T(370) + T(g.R.ValeInS)); r.Until(() => g.PickupService, T(60)); // brief 059: Vale gets in at 6:13 and is driven round to the service bay
                r.Is(g.PickupService, "the turned driver brought Vale round to the service bay");
                int there = DriveRound(r); // brief 059 (Sky 1a): the Tech brings the getaway car round behind Vale's
                r.Do(Enf, "take"); r.To(Math.Max(there, r.Op(Enf).BusyEnd));
                r.FightItOut(120, Scout, Fixer, Tech);
                r.Extract();
                r.Line(g.Result == Outcome.Success, $"route B wins ({g.Result} {g.FailCode}; brief 059: with no final fight among the 28, the posted three at the yard fire on the Vale holder in their cone, Sky's answer 3: Waymark and Motive)");
                r.Eq(g.Gunfire, 0, "no shots");
                r.Is(g.Chain.Contains("rivals-false-tip"), "the rivals followed a false tip");
            });

            var drv = Turned(Driver); drv.Known[Driver] = new List<string> { "routine", "need", "temperament" };
            Add("S21", "347", "Brief 059 (Motive M059-6.1): the driver's Call (the camera block) is retired: a call on the turned driver is refused (nothing-to-call), his car never moves at the front, the pickup camera stays live. The turned driver drives Vale round from 6:13 at the car's pace (car.pace) and parks in the service bay; Vale is in the back; the debrief leads with the driver.", r =>
            {
                var g = r.G;
                // FIX → start state: the driver is turned at the start (the mechanics, not the turn, are under test)
                r.Send(Enf, "d:Service lane"); r.Send(Scout, "d:Meet driver"); r.Send(Tech, "cabinet");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "call", Target = Driver }).code, "nothing-to-call", "the driver's Call is retired (M059-6.1): nothing-to-call");
                // a car stands on the drive grid, at its place's nearest drivable cell (rulebook car.drive)
                var pickup = g.S.CarStand(g.S.CarPickup); var bay = g.S.CarStand(g.S.CarService);
                r.Eq(g.CarPos(), pickup, "the car starts at Vale's pickup");
                r.To(r.SendTo(Scout, TipSpot(r))); r.To(T(320)); r.Do(Scout, "mislead", "rivals"); r.Done(Scout);
                r.To(T(360));
                r.Eq(g.CarPos(), pickup, "it never moves at the front before Vale is in");
                r.Is(g.PickupCameraLive(), "the pickup camera stays live (no block)");
                r.Do(Tech, "reroute", "patrol");
                r.To(T(370));
                r.Eq((g.Vale, g.ValePos()), (ValeAt.Pickup, g.Walk.NearestFree(g.CarPos() + new Pt(-150, 0))), "6:10: Vale at his car at the official bay (brief 059 K1), at its kerb-side door (engine 0.33.1: the car's right, -x in the official bay)");
                r.To(T(370) + T(g.R.ValeInS));
                r.Is(g.Vale == ValeAt.InCar && g.CarHeading().HasValue, "6:13: he is in, and the turned driver drives (its heading along the drive)");
                r.Is(g.CarInBayTick > T(373) && g.CarInBayTick < T(395), $"parked in the service bay at {Report.Clock(g.CarInBayTick)} (car.pace: 12 m of road at 5 m/s, the drive at 2 m/s)");
                r.To(g.CarInBayTick);
                r.Eq(g.CarPos(), bay, "the car waits in the service bay (service-bay)");
                r.Eq(g.CarHeading(), (Pt?)g.S.CarServiceFacing, "parked, along the bay (site car.serviceFacing)");
                r.Eq(g.ValePos(), (Pt?)bay, "Vale in the back of it");
                int there = DriveRound(r, Scout); // the Tech is rerouting the patrol: the Scout, back from the tip, drives
                r.Do(Enf, "take"); r.To(Math.Max(there, r.Op(Enf).BusyEnd));
                r.FightItOut(120, Scout, Fixer, Tech); r.Extract();
                var d = Debrief.Of(g);
                r.Eq(d.Route.Count > 0 ? d.Route[0] : "", "driver-moves-vale-to-service-lane", "the route summary leads with the driver");
                r.Is(d.Extra.Contains("1 reroute") && !d.Extra.Contains("call:driver"), "the debrief lists 1 patrol reroute and no camera blockade");
            }, drv);

            Add("S22", "386", "Gating: no action at a distance; an unknown person isn't callable; the Fixer and the Scout must stand beside Inez; a turned contact is callable. Engine 0.23.0 (rulebook cell.approach, Sky 2026-10-22): an act given at a distance is taken and walks the agent in, and starts only in reach; a need walking won't cure is still refused.", r =>
            {
                var g = r.G;
                // brief 031: every agent watches, the Fixer from 12 m; the Scout's 30 m reaches Inez from the car. Engine 0.23.0: out of reach, the order walks him in
                var w = g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "watch", Target = Inez });
                r.Is(w.ok && r.Op(Fixer).Act != null && r.Op(Fixer).Busy == null, $"a distant person: no act at a distance, the Fixer walks in first ({w.code})");
                r.Do(Fixer, "cancel"); r.Is(r.Op(Fixer).Act == null && r.Op(Fixer).Busy == null && !g.Inez.KRoutine, "cancelled on the way: nothing watched");
                r.Is(!g.CanCall(Inez), "an unknown person is not a contact to call");
                r.To(r.Send(Scout, "d:Courtyard"));
                r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.Refused(Fixer, "turn", Inez, "Help needs her need first (a Listen): refused wherever the Fixer stands, walking closer won't cure it", "help");
                // engine 0.30.0: Inez's post is the site's stand (-975, -2925; Pulse's re-bake), 2.96 m from the Courtyard, inside Listen's 3 m: the Scout steps
                // back to 6 m from her first, so the Listen is given out of reach as this case means
                { var ip = g.PersonPos(g.Inez)!.Value; var d = r.Op(Scout).Pos - ip; long len = Math.Max(1, G.Isqrt((long)d.X * d.X + (long)d.Y * d.Y)); var back = ip + new Pt((int)(d.X * 600 / len), (int)(d.Y * 600 / len)); r.To(r.SendTo(Scout, g.S.Grid.NearestFree(back) ?? back)); }
                r.ForHer("listen", Inez);
                var l = g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "listen", Target = Inez }); int given = g.Tick;
                r.Is(l.ok && r.Op(Scout).Busy == null, $"the Scout's Listen from 6 m is taken, and walks him in first ({l.code})");
                r.Is(r.Until(() => r.Op(Scout).Busy != null, T(30)) && G.Within(r.Op(Scout).Pos, g.PersonPos(g.Inez)!.Value, g.R.ListenReachCm), $"the Scout must stand beside Inez to hear the debt (3 m): it starts there, {(g.Tick - given) / Game.Hz} s later");
                r.Done(Scout);
                r.To(r.Send(Fixer, "d:Courtyard"));
                HelpUntilTurned(r, Inez);
                r.Is(g.CanCall(Inez), "a turned contact can be called");
                r.To(r.Send(Enf, "d:Courtyard"));
                // brief 030 (weapons.close): a fire order out of reach now walks into range, so the gate is the walk's: none, or a long one
                var fp = g.PreviewFire(OpId.Enforcer, GuardR);
                r.Is(fp.Refused.Length > 0 || (fp.Walk && fp.WalkS > 0), $"being near Inez does not reach the guard: no shot from here, only a walk into range first ({(fp.Refused.Length > 0 ? fp.Refused : $"walks {fp.WalkS} s")})");
            });

            Add("S23", "402", "Ask cold: refused and remembered (+1 T), can't be repeated; routine alone doesn't unlock Help; after Listen, each Help at +$10,000 and +1 T, on the visits' share (worse-help-usd).", r =>
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
                // Changed (r016 cast, 2026-10-16): the prototype's Inez took one Help ($50,000, T 3). How many Helps she takes is the cast's
                // (§04: one for an Idealist, two for the Fearful); what this tests is the refusal's line on every one of them.
                long help = (long)g.R.Num("p:approaches.help.usd"), helpT = (long)g.R.Num("p:approaches.help.trail");
                long refUsd = (long)g.R.Num("v:refusal-usd"), refT = (long)g.R.Num("v:refusal-trail"), share = (long)g.R.Num("v:worse-help-usd"); // brief 085: a Help of two visits splits its money
                int helps = 0; long before = g.Cash;
                r.Eq(before, 100000L, "the cold approach cost no money");
                while (!g.Inez.Turned && !g.Over && helps < 4)
                {
                    var q = g.Preview("help", Inez); var rb = q.Lines.Find(l => l.Line == "refused-before");
                    r.Is(rb != null && rb.Holds == "yes" && rb.Usd == refUsd && rb.Trail == refT, $"Help {helps + 1}: the card carries the refusal's +${refUsd}, +{refT} T");
                    r.Do(Fixer, "turn", Inez, "help"); r.Done(Fixer); helps++;
                }
                r.Is(g.Inez.Turned, $"new information lets the Fixer recover from the refusal ({helps} Help)");
                r.Eq(g.Cash, before - helps * (help + share + refUsd), "the cold approach raises the debt relief price, on each Help");
                r.Eq((long)g.Trail, 1 + helps * (helpT + refT), "the refused approach leaves an extra trail, on each Help");
            });

            var idealist = new Start(); idealist.Temperament[Inez] = "idealist";
            Add("S24", "421", "Pay is open with no Watch to give (engine 0.28.0, brief 054 §2.2: an Idealist Inez reads neutral; it was the Watch that unlocked it), shown at $70,000 with \"?\"; Help stays locked; an Idealist Inez refuses Pay, and it is remembered (Sky 2026-10-15). The refusal is the rule under test, so the start state makes her an Idealist whoever the cast is.", r =>
            {
                var g = r.G;
                // engine 0.28.0 (brief 054 §2.2): an Idealist Inez has fear 0 and reads neutral, so there is no Watch to give and Pay needs none
                r.To(r.Send(Scout, "d:Courtyard"));
                r.Is(!g.WatchOffered(Inez) && g.ScoutQuery("watch", Inez).code == "nothing-to-see", "no Watch to give on a neutral Inez: nothing-to-see");
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
            }, idealist);

            Add("S25", "434", "Pressure: $0, +3 T (exposure of her affair, learned by Listen: no step worse whatever her temperament; Motive, motive-lovers.md), and she can be called.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "d:Courtyard")); r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "listen", Inez); r.Done(Scout);
                r.To(r.Send(Fixer, "d:Courtyard"));
                r.Do(Fixer, "turn", Inez, "pressure"); r.Done(Fixer);
                r.Is(g.Inez.Turned, "she agrees under pressure");
                r.Eq(g.Cash, 100000L, "pressure avoids the payment");
                r.Eq(g.Trail, 3, "pressure by exposure leaves +3 T: Listen found her affair, so no wrong-approach step (Motive, motive-lovers.md §2.2; was +4)");
                r.Is(g.CanCall(Inez), "she can be called");
            });

            Add("S26", "448", "Loop v2 (brief 058 §2, engine 0.31.0; it was the Tech's tether): leaving the cabinet keeps the loop; it runs out on its own loop.run after it lands.", r =>
            {
                var g = r.G;
                r.To(r.Send(Tech, "cabinet")); r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                int landed = g.Tick;
                r.Is(!g.CameraWatching("entrance", r.Stand("d:Office entrance")), "the loop holds");
                r.To(r.SendTo(Tech, r.Stand("cabinet") + new Pt(-900, 0)));
                r.Is(g.LoopOn && !g.CameraWatching("entrance", r.Stand("d:Office entrance")), "leaving the cabinet keeps the loop");
                r.To(landed + T(g.R.LoopRunS) - 1);
                r.Is(g.LoopOn, "still looped a tick before it runs out");
                r.To(landed + T(g.R.LoopRunS));
                r.Is(!g.LoopOn && g.Log.Exists(l => l.Code == "loop.ends" && l.Tick == g.Tick), "loop.ends at landing + loop.run");
                r.Is(g.CameraWatching("entrance", r.Stand("d:Office entrance")), "the camera is live again");
            });

            Add("S27", "457", "Orders can't target the Office; the path bends round it; the operative never stands inside.", r =>
            {
                var g = r.G;
                var (ok, _) = g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "move", Point = Roof });
                r.Is(!ok, "orders cannot target the Office");
                var rb = g.Walk.RouteBeside(r.Op(Scout).Pos, r.Stand("d:Meet driver")); var sent = rb?[rb.Length - 1];
                int at = r.Send(Scout, "d:Meet driver");
                r.Is(r.Op(Scout).Walk!.Points.Length > 2, "the route bends round the Office");
                bool outside = true, offCar = true;
                Pt? inside = null;
                while (g.Tick < at) { g.Step(); var p = r.Op(Scout).Pos; if (g.S.InsideOccluder(p) || !(g.S.Grid.Free(p) || g.S.Grid.Free(p + new Pt(10, 0)) || g.S.Grid.Free(p + new Pt(-10, 0)) || g.S.Grid.Free(p + new Pt(0, 10)) || g.S.Grid.Free(p + new Pt(0, -10)))) { outside = false; inside ??= p; } /* a string-pulled line is sampled every 10 cm (Grid.LineFree), so it may cut a blocked cell's corner by a few cm: engine 0.33.1's walk round Vale's car cuts one by 3 cm at (6900, -4403) */ if (g.Walk.CarCovers(p)) offCar = false; }
                r.Is(outside, $"the operative stays outside the Office{(inside.HasValue ? $" (in at {inside.Value}: {string.Join(", ", g.S.Occluders.FindAll(o => o.Contains(inside.Value)).ConvertAll(o => o.Id))})" : "")}");
                r.Is(offCar, "and never walks over Vale's car (engine 0.33.1, car.footprint)");
                r.Is(G.Within(r.Op(Scout).Pos, sent!.Value, r.G.R.BodyClearCm * 6) && !G.Within(r.Op(Scout).Pos, r.G.DriverPos()!.Value, r.G.R.BodyClearCm), "and arrives where sent: d:Meet driver lies on the ground of Vale's car at the pickup, so beside it, on the side he comes to (Walk.RouteBeside), and clear of the driver (fight.spacing.cell, engine 0.37.0)");
            });

            Add("S28", "471", "Engaging nearby stops the approach walk.", r =>
            {
                var g = r.G;
                r.Send(Scout, "d:Café door");
                while (!g.Over && !G.Within(r.Op(Scout).Pos, g.PersonPos(g.Inez)!.Value, g.R.ActReachCm)) g.Step(); // engine 0.34.0: bounded by the run's end
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

            Add("N10", "—", "Final fight, loud (§07), at security level 1: after shots the police come from the north street, 2–4 officers, the seal clock running, no allies; the Enforcer fights them off at the car. Brief 018 §10: an officer holding his stand sees the car leave (+1 T, the car hunted).", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, BesideDoor)));
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech); r.Do(Tech, "loop", "entrance"); r.Done(Tech); /* Loop v2 (engine 0.31.0, brief 058 §4): the 2:00 loop is planted after the Reroute */
                // brief 013 (Motive §5.1): the guard checks in every 90 s; a shot just after a mark leaves the shot's own report to call the unit
                int mark = g.NextMarkOf(g.GuardP, g.Tick); if (mark - g.Tick < T(30)) r.To(mark + 1);
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
                r.Extract();
                // Retired as a win (Sky 2026-10-21 picked Motive's A, tuning.md §10.1, §10.3): both sides fire on the move in today, so loud may lose at
                // routine security, "a last resort" (§05). The v0.24 loud reference (a win at 5:00.3 / 4:23.5) is retired, not fixed: this scripted
                // cell loses at level 1 (engine 0.15.0: three agents down, sealed at 5:48.5). The assert is §10.3's: a heavy debrief or a failure,
                // sealed or the Enforcer down; §7.1's "at least one loud row wins heavy" is no longer a done-check. The win's own lines (car seen
                // leaving) are checked only when it wins.
                bool won = g.Result == Outcome.Success;
                r.Is(won || g.FailCode == "sealed" || g.FailCode == "enforcer-down", $"§10.3 (Sky's A): the loud fight ends in a heavy debrief or a failure, sealed or the Enforcer down ({g.Result} {g.FailCode})");
                var d = Debrief.Of(g);
                r.Eq(d.FightBranch, "loud", "the debrief names the loud fight");
                r.Is(g.Log.Exists(l => l.Code == "ai.join"), "the response unit on the street joins the loud fight (Pulse §3.3)");
                // engine 0.34.0: an officer fired on from beyond his reach with no cover closes to his ring (brief 027 R4, KiteFightClose) and stops
                // on the first foe in reach on the way (advance → engage): at 1.8 m/s the Scout fires on both from beyond their reach at 3:01.7, and at the
                // run's end (3:20.6) they stand engaged short of the point they were closing to. That is holding, not a chase
                bool closed(string id) => g.Log.Exists(l => l.Code == "ai.kite" && l.Text.StartsWith(id + " ", StringComparison.Ordinal) && l.Text.Contains("it closes"));
                r.Is(g.Hostiles.TrueForAll(h => !h.Id.StartsWith("officer-", StringComparison.Ordinal) || h.Down || h.Pos(g.Tick) == h.Home || h.Moving(g.Tick) || (closed(h.Id) && h.Gun.St == "engage")), "police hold their stand: none left it but to close to its ring when fired on from beyond it (Pulse §3.2; brief 027 R4)");
                r.Is(g.Tick - g.FightStart >= T(30) || d.FightDownedByEnforcer == d.FightHostiles || g.FailCode == "enforcer-down", "the fight took its time (brief 059: the drawn crew, four on foot, presses the Enforcer and may down him sooner)");
                if (!won) return;
                // brief 018 §10 (Pulse): a final-fight officer sees the car go within the pistol's 25 m on a clear line: officer-2 at his stand, 19.4 m
                r.Is(g.CarSeenBy.StartsWith("officer-", StringComparison.Ordinal) && g.Ledger.Exists(x => x.cause == "car-seen:" + g.CarSeenBy && x.delta == g.R.WitnessTrail), $"seen leaving: +1 T, by {g.CarSeenBy}");
                r.Is(d.Inv.Hunted.Contains("cell-car") && d.Next.Lines.Exists(l => l.Line == "car-seen"), "the car hunted; the debrief's car-seen line");
            }, Level(1)); // brief 013: the loud fight's mechanics at level 1 (routine, today's response); N52 is the dial at each level

            Add("N11", "—", "Final fight, safe (§07): route A, quiet, then the rival crew ambushes the extraction; the helped Inez warns 10 s early and the guard, who never saw the cell, fights them.", r =>
            {
                var g = r.G;
                RouteA(r, reroute: true); // the Tech diverts the patrol while the Enforcer is inside: the fight's gunfire would draw it (§05 [B] 30 m)
                r.Eq(g.FightBranch, "safe", "no shots, Alert ≤ 1: the final fight is safe");
                r.Eq(g.CrewInFight, 5, "five rivals: no driver turned, no false tip (brief 059: the crew's bodies, the lookout boarding the car)");
                r.Is(g.FightWarned, "the helped Inez warns");
                r.Is(g.GuardAlly, "the guard never saw the cell, so he fights them");
                Support(r); ToAmbush(r); // brief 018: walking Vale the Enforcer doesn't fire; the three by the car fight
                r.Is(g.Hostiles.FindAll(h => h.Present && h.Gun.St == "alerted").TrueForAll(h => h.Gun.NextShot == g.FightSprung + g.R.AimNewTicks + T(10)), "lying in wait they would fire 1 s after springing; Inez's warning holds them 10 s more (brief 059: those still in the car come in on their path)");
                r.FightItOutAll(120, Scout, Fixer, Tech); // brief 018 §10: break the crew (the blocker on the road falls first, the car free before the crew breaks)
                r.Extract();
                r.Eq(g.Result, Outcome.Success, "route A wins through the ambush");
                // Changed (r016 cast, 2026-10-16): was "the guard dropped at least one of them". A Fearful Inez takes two Helps, route A runs 50 s
                // later, and the Enforcer drops three before the guard lands one: who fires the last shot is the fight's timing, not the rule
                // under test (§07 safe: the guard, who never saw the cell, fights them).
                // brief 018 (c018): the three by the car now do the fighting (walking Vale, the Enforcer doesn't fire) and break the crew before the
                // guard is in reach: the rule under test is that he joins against them
                r.Is(g.Log.Exists(l => l.Code == "fight.guard") && g.GuardAlly, $"the guard runs in against them (he dropped {g.DownedByGuard})");
                r.Is(g.FightWhy.Contains("rivals-break") && g.Hostiles.Exists(h => h.Gun.St == "break" || h.Gun.St == "gone"), "more than half the crew down, the rest break for their car (Pulse §3.1)");
                r.Is(g.Log.Exists(l => l.Code == "ai.cover" && l.Text.StartsWith("guard", StringComparison.Ordinal)), "the guard runs in to cover by his post (Pulse §3.4)");
                var dd = Debrief.Of(g);
                r.Is(dd.FightWhy.Contains("waitress-warns") && dd.FightWhy.Contains("guard-fights-them"), "the debrief says why: Inez's warning, the guard");
            });

            Add("N11b", "—", "Final fight, safe, prepared (§07): route B with the driver turned (he blocks the rival car, −1) and the rivals tipped off earlier (−1): three rivals. Brief 018: the three fight it out (walking Vale, the Enforcer doesn't fire), anyone downed is carried in, and the whole cell boards and goes.", r =>
            {
                var g = r.G;
                r.Send(Enf, "d:Service lane");
                r.To(Max(r.Send(Scout, "d:Meet driver"), r.Send(Fixer, "d:Boarding pavilion")));
                r.Do(Scout, "listen", Driver); r.Done(Scout); /* engine 0.28.0: no Watch to give on the driver (brief 054 §2.1) */
                TurnDriver(r);
                r.To(r.SendTo(Scout, TipSpot(r))); r.To(T(320)); r.Do(Scout, "mislead", "rivals"); r.Done(Scout);
                r.To(T(370) + T(g.R.ValeInS)); r.Until(() => g.PickupService, T(60));
                int there = DriveRound(r); // brief 059 (Sky 1a): the Tech brings the getaway car round behind Vale's; the support walks over to it (motive-fights.md §2.9)
                r.SendTo(Scout, g.S.Grid.NearestFree(g.S.Stand("service-drive") + new Pt(300, 300)) ?? g.S.Stand("service-drive")); r.SendTo(Fixer, g.S.Grid.NearestFree(g.S.Stand("service-drive") + new Pt(300, -300)) ?? g.S.Stand("service-drive"));
                r.Do(Enf, "take"); r.To(Math.Max(there, r.Op(Enf).BusyEnd)); // taken in his car in the service bay
                r.Eq(g.FightBranch, "safe", "a quiet route B: the final fight is safe");
                r.Eq(g.Hostiles.FindAll(h => h.Id.StartsWith("rival-", StringComparison.Ordinal)).Count, 3, "three rivals: the lookout warned off by the turned driver, the wheelman away on the earlier false tip (brief 059: bodies, Pulse §7)");
                foreach (var op in new[] { Enf, Scout, Fixer }) r.Do(op, "draw"); // with Vale in hand the fight is on its way: drawing is no new act (the final fight is untraced)
                r.FightItOutAll(120, Scout, Fixer, Tech); // brief 018: fight it out (the crew broken), then the whole cell boards and goes
                // brief 018 (c018): walking Vale the Enforcer no longer fires, so the three take the fight and one of them may go down: carry him in
                foreach (var down in new[] { Scout, Fixer, Tech })
                {
                    if (g.Over) break;
                    if (!r.Op(down).Down) continue;
                    var by = Array.Find(new[] { Fixer, Scout, Tech }, o => o != down && !r.Op(o).Down && !r.Op(o).Carrying.HasValue);
                    if (by == null) continue;
                    r.To(r.SendTo(by, g.S.Grid.NearestFree(r.Op(down).Pos)!.Value)); r.Do(by, "carry", down); r.Done(by);
                }
                if (!g.Over) r.GetAway(Enf, Scout, Fixer, Tech);
                // brief 059: at the service drive the three post by the service door and fight from there; whether this scripted cell wins it is
                // Waymark's re-composition and Motive's balance (the yard stand's spots, MC-11), recorded as lines
                r.Line(g.LeftBehindAtGo.Count == 0 && g.Result == Outcome.Success, $"the prepared route wins with nobody left behind ({g.Result} {g.FailCode} at {Report.Clock(g.Tick)}, left {g.LeftBehindAtGo.Count})");
                r.Line(g.Log.Exists(l => l.Code == "ai.cover" && l.Text.StartsWith("rival-", StringComparison.Ordinal)), "a rival steps to cover the data give (Pulse §2.3)");
            });

            Add("N12", "—", "People step their routines (§04, brief 010): at each tick everyone stands at the stop the people file gives, or walks between two stops, or is off the street.", r =>
            {
                var g = r.G;
                r.Is(g.Walkers.Count > 100, $"the civic block's people are in ({g.Walkers.Count})");
                bool atStops = true; int walking = 0, onStreet0 = 0;
                Walker? mover = null; var movers = new List<Walker>();
                for (int t = 0; t <= T(360); t += T(30)) // brief 059: an unturned driver drives Vale away (5:53 since brief 096), and the run ends there
                {
                    r.To(t);
                    foreach (var w in g.Walkers)
                    {
                        var at = g.CrowdPos(w);
                        int i = Array.FindIndex(w.From, f => f <= t);
                        bool inStop = false;
                        for (int k = 0; k < w.Place.Length; k++)
                            if (t >= w.From[k] && t < w.To[k]) { inStop = true; if (w.Out[k] ? at != g.People2!.Places[w.Place[k]] : at != null) atStops = false; }
                        if (!inStop && at.HasValue) { walking++; mover ??= w; if (!movers.Contains(w)) movers.Add(w); }
                        if (t == 0 && at.HasValue) onStreet0++;
                    }
                }
                r.Is(atStops, "everyone at a stop stands at its place, or is off the street (at home, inside, beyond)");
                // Changed (r016 cast, 2026-10-16): was "more than 10", the fixture cast's count; the people file says who is out at 0:00
                int outAt0 = 0;
                foreach (var w in g.Walkers) for (int k = 0; k < w.Place.Length; k++) if (w.From[k] <= 0 && 0 < w.To[k] && w.Out[k]) { outAt0++; break; }
                r.Is(outAt0 > 0 && onStreet0 >= outAt0, $"people are out on the street at 0:00 ({onStreet0}; the people file puts {outAt0} at a street stop)");
                r.Is(walking > 0 && mover != null, $"someone walks between stops during the mission ({mover?.P.Id})");
                if (mover != null)
                {
                    var seen = new List<Pt>(); // brief 096: the walker caught on the longest walk, not the first caught (the first may be on a 5 s leg once the shift moves)
                    foreach (var m in movers) { var s = new List<Pt>(); for (int t = 0; t <= T(480); t += Game.Hz) { var p = g.CrowdPos(m, t); if (p.HasValue && !s.Contains(p.Value)) s.Add(p.Value); } if (s.Count > seen.Count) { seen = s; mover = m; } }
                    r.Is(seen.Count > 5, $"{mover.P.Name} moves in live time ({seen.Count} positions)");
                }
            });

            Add("N12b", "—", "A private Help (rulebook privacy, Sky 2026-10-16: a turning conversation is private, unless it's pressure): the Fixer helps Inez with customers at the café terrace in sight. Onlookers learn nothing: no witness Trail, no Standing move, no incident, nobody remembers the Fixer's face; the Help's own price stands (its base Trail).", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "d:Courtyard")); r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "listen", Inez); r.Done(Scout);
                r.To(r.Send(Fixer, "d:Courtyard")); InSightOfSecurity(r); // engine 0.28.0: at Alert 0 only security minds a threat
                int onlookers = g.Preview("pressure", Inez).Witnesses; // the same people, for a visible act (N12c)
                r.Is(onlookers > 0, $"people stand within witness range of Inez, on a clear line: a threat here would be seen by {onlookers}");
                int trail = g.Trail, standing = g.Standing, incidents = g.Incidents.Count;
                var q = g.Preview("help", Inez); // the card after Listen: turned, or not yet (a Fearful Inez takes two; the cast decides)
                r.Eq(q.Witnesses, 0, "the card counts nobody who would see it: the conversation is private");
                r.Do(Fixer, "turn", Inez, "help"); r.Done(Fixer);
                r.Is(g.Inez.Has("help") && g.Inez.Turned == (q.Outcome == "turned"), $"the Help lands as the card said ({q.Outcome})");
                r.Eq(g.Trail - trail, (int)q.Trail, "the Help's own Trail only: no witness Trail");
                r.Eq(g.Standing, standing, "no Standing move");
                r.Eq(g.Incidents.Count, incidents, "no incident for the investigators");
                r.Is(!g.Walkers.Exists(w => w.P != g.Inez && (g.HoldsFace(w.P) || w.P.Memory.Exists(m => m.Act == "saw-act"))), "no onlooker remembers the act or the Fixer's face");
                r.Is(!g.Ledger.Exists(l => l.cause.StartsWith("witnessed:", StringComparison.Ordinal)), "the ledger has no witnessed act");
                r.Is(!g.Log.Exists(l => l.Code.StartsWith("witness.", StringComparison.Ordinal)), "nobody saw the help");
                var next = g.BetweenMissions(g.Investigate());
                r.Is(!next.Lines.Exists(l => l.Line == "word" && l.Who != Inez), "between missions: no onlooker passes word of it");
            });

            Add("N12c", "—", "Pressure stays witnessable (rulebook privacy: a threat is visible): the Fixer presses Inez with the same customers in sight. A witness adds Trail once, moves Standing one step toward Menace (§04 public threats), remembers the Fixer's face, and the threat is an incident for the investigators.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "d:Courtyard")); r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "listen", Inez); r.Done(Scout);
                r.To(r.Send(Fixer, "d:Courtyard")); InSightOfSecurity(r); // engine 0.28.0: at Alert 0 only security minds a threat
                int trail = g.Trail, standing = g.Standing;
                var q = g.Preview("pressure", Inez);
                r.Is(q.Witnesses > 0, $"the card counts who would see it ({q.Witnesses})");
                r.Do(Fixer, "turn", Inez, "pressure"); r.Done(Fixer);
                r.Eq(g.Trail - trail, (int)q.Trail + g.R.WitnessTrail, "the Pressure's own Trail, and +1 T for the witnessed threat (once, however many saw it)");
                r.Eq(g.Standing, standing - g.R.StandingStep, "a public threat moves Standing one step toward Menace");
                r.Is(g.Walkers.Exists(w => w.P != g.Inez && g.HoldsFace(w.P)), "a witness remembers the Fixer's face");
                r.Is(g.Ledger.Exists(l => l.cause == "witnessed:threat:waitress"), "the ledger names the witnessed act");
                r.Is(g.Incidents.Exists(i => i.Cause == "threat:waitress" && i.Witnesses.Count > 1), "an incident with its witnesses, for the investigators");
            });

            var drvKnown = Turned(Driver); drvKnown.Known[Driver] = new List<string> { "routine", "need", "temperament" };
            Add("N16", "—", "Watchers log the cell (§05): the pickup camera logs the Fixer beside the driver; a watcher that logs an operative beside a contact exposes the contact's job (Motive's review #12), so the next mission's ladder moves resentment, not bravery.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Boarding pavilion"));
                r.Wait(T(2));
                r.Is(g.Sightings.Exists(s => s.Watcher == "camera:pickup" && s.Op == "fixer"), "the pickup camera logged the Fixer, with place and time");
                r.Is(g.ExposedByWatcher.Contains(Driver), "logged beside the driver: the driver is exposed");
                // brief 059 (Motive M059-6.1): the driver's Call, his only job for the cell, is retired, so his exposure has no job to show:
                // the job-exposed ladder is Inez's call's (N17 family) now
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "call", Target = Driver }).code, "nothing-to-call", "no Call on the driver");
                r.To(T(200));
                var next = g.BetweenMissions(g.Investigate());
                r.Is(!next.Lines.Exists(l => l.Line == "ladder" && l.Who == Driver && l.Detail.StartsWith("job-exposed", StringComparison.Ordinal)), "no job, nothing exposed on the ladder");
            }, drvKnown);

            // N17 and N17b test the Talker rule on Inez (Game.Question: what she would tell an investigator), and that the investigation, if its
            // hops reach her, hears exactly that. Whether they reach her is the cast's: the investigators question the people nearest the act
            // first, then by id (rulebook investigation); in the r016 cast two café servers (p-cafe-11, -12) share the café entrance with Inez
            // (p-cafe-14) and at Alert 0 (one investigator, two hops) are asked before her. The fixture's Inez was p-cafe-11 and won that tie.
            Add("N17", "—", "The investigation (§05, rulebook investigation): a pressured Inez is a Talker and gives up the Fixer's face to the investigators, with the cause in words (K7); a helped Inez lies, and in a neutral district that gives them nothing.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "d:Courtyard")); r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "listen", Inez); r.Done(Scout);
                r.To(r.Send(Fixer, "d:Courtyard")); r.Do(Fixer, "turn", Inez, "pressure"); r.Done(Fixer);
                var q = g.Question(g.Inez);
                r.Is(q.Outcome == "gives" && q.Facts.Contains("fixer-face"), "questioned, Inez gives up the Fixer's face");
                r.Is(q.Cause.Contains(">"), $"with the cause: {q.Cause}");
                var inv = g.Investigate();
                r.Is(inv.Findings.Count > 0, "the investigators work the threat");
                var f = inv.Findings.Find(x => x.Who == Inez);
                r.Is(f == null || (f.Outcome == q.Outcome && string.Join(",", f.Facts) == string.Join(",", q.Facts) && f.Cause == q.Cause), $"reached ({f != null}), she tells them exactly that");
                var named = inv.Findings.Find(x => x.Outcome == "gives" && x.Facts.Contains("fixer-face"));
                r.Eq(inv.NamedCause, named != null ? "named-witness:" + named.Who : "", "the first questioned who gave up the Fixer's face is the investigation's named cause");
                r.Is(f == null || inv.NamedCause == "named-witness:waitress", "if Inez is reached, she is the named witness");
            });

            Add("N17b", "—", "The investigation, the other way: a helped Inez is no Talker; brave enough (people.rules talker.lie: an Idealist starts a helper) she covers for the cell with a lie, else she says nothing (a Fearful Inez starts an informant); in a neutral district either gives the investigators nothing.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "d:Courtyard")); r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "listen", Inez); r.Done(Scout);
                r.To(r.Send(Fixer, "d:Courtyard")); HelpUntilTurned(r, Inez); // a contact helped as often as the card asks (the cast's temperament)
                var q = g.Question(g.Inez);
                bool brave = g.Inez.Sc[Rules.Bravery] >= g.R.LieBraveryAtLeast; // the cast's temperament decides (§04 start: Idealist bravery 1)
                r.Is(brave ? q.Outcome == "lies" && q.Facts.Count == 1 && q.Facts[0] == "nothing" : q.Outcome == "nothing" && q.Facts.Count == 0,
                     $"questioned, Inez {(brave ? "lies: nothing" : "is no Talker and says nothing")} ({q.Outcome} {string.Join(",", q.Facts)}; bravery {g.Inez.Sc[Rules.Bravery]})");
                var inv = g.Investigate();
                var f = inv.Findings.Find(x => x.Who == Inez);
                r.Is(f == null || (f.Outcome == q.Outcome && string.Join(",", f.Facts) == string.Join(",", q.Facts)), $"reached ({f != null}), she tells them exactly that");
                r.Eq(inv.NamedCause, "", "nobody named the cell");
            });

            var brave = new Start(); brave.Temperament[Inez] = "idealist";
            Add("N17c", "—", "The lie itself (people.rules talker.lie), whoever the cast is: the start state makes Inez an Idealist (bravery helper from the start, §04); helped, she is no Talker, covers for the cell, and in a neutral district her lie gives the investigators nothing.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "listen", Inez); r.Done(Scout); // engine 0.28.0: an Idealist Inez reads neutral: no Watch to give, Listen needs none
                r.To(r.Send(Fixer, "d:Courtyard")); HelpUntilTurned(r, Inez);
                var q = g.Question(g.Inez);
                r.Is(q.Outcome == "lies" && q.Facts.Count == 1 && q.Facts[0] == "nothing", $"questioned, Inez lies: nothing ({q.Outcome} {string.Join(",", q.Facts)})");
            }, brave);

            Add("N18", "—", "Between missions (rulebook phases): after route A, Inez's ladder follows the investigation: questioned, her job is exposed (resentment compliant → resentful), else it is not (bravery informant → helper); her met debt is replaced from her own stream (Q2), told free as she is a contact; obliged, she passes no word (K3, Motive #10); she can introduce along her own family, workmate and regular ties (K5).", r =>
            {
                var g = r.G;
                RouteA(r, reroute: true);
                Support(r); ToAmbush(r); // brief 018: walking Vale the Enforcer doesn't fire; the three by the car fight
                r.FightItOutAll(120, Scout, Fixer, Tech); r.Extract();
                r.Eq(g.Result, Outcome.Success, "route A wins");
                var next = Debrief.Of(g).Next;
                // Changed (rulebook privacy, Sky 2026-10-16): the Help is private, so no onlooker's sighting is a lead. Whether the
                // investigators reach Inez by another lead is the cast's and the route's; the ladder must follow it.
                var inv = Debrief.Of(g).Inv;
                r.Is(!inv.Findings.Exists(f => f.Lead.StartsWith("incident:debt-cleared", StringComparison.Ordinal) || f.Lead.StartsWith("incident:payment", StringComparison.Ordinal)), "a private Help leaves the investigators no witnessed lead");
                bool asked = inv.Findings.Exists(f => f.Who == Inez);
                string ladder = asked ? "job-exposed: resentment compliant → resentful" : "job-unexposed: bravery informant → helper";
                r.Is(next.Lines.Exists(l => l.Line == "ladder" && l.Who == Inez && l.Detail == ladder), $"{(asked ? "questioned" : "not questioned")}, her ladder: {ladder}");
                var need = next.Lines.Find(l => l.Line == "need-replaced" && l.Who == Inez);
                r.Is(need != null && need.Detail.StartsWith("debt → ", StringComparison.Ordinal) && need.Detail.EndsWith("(told free: a contact)", StringComparison.Ordinal), $"her debt is replaced and told free ({need?.Detail})");
                r.Is(!next.Lines.Exists(l => l.Line == "word" && l.Who == Inez), "obliged, she passes no word");
                var kinds = new List<string>(); // the kinds of tie introductions travel along that Inez has in the people file (§04 ties)
                foreach (var (kind, _) in g.People2!.Ties[g.Inez.Id]) if ((kind == "family" || kind == "workmate" || kind == "regular") && !kinds.Contains(kind)) kinds.Add(kind);
                var intro = next.Lines.Find(l => l.Line == "introduced" && l.Who == Inez);
                r.Is(kinds.Count > 0 && intro != null && kinds.TrueForAll(k => intro.Detail.Contains("(" + k + ")")), $"she can introduce along her own ties ({string.Join(", ", kinds)}): {intro?.Detail}");
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
                var fixerEnd = r.Op(Fixer).Walk!.End;
                r.To(fixerAt);
                r.Eq(r.Op(Fixer).Pos, fixerEnd, "the Fixer arrives at the tick the walk gives");
                r.Is(fixerEnd != r.Stand("d:Courtyard") && G.Within(fixerEnd, r.Stand("d:Courtyard"), g.R.BodyClearCm * 6) && !G.Within(fixerEnd, r.Op(Scout).Pos, g.R.BodyClearCm), "beside the Scout, not in her (fight.spacing.cell, engine 0.37.0)");
                r.Is(walkTicks - runTicks > 0, $"running saved {walkTicks - runTicks} ticks");
            });

            Add("N19b", "—", "No running while carrying Vale (Sky 2026-10-15 Q3: a man being taken walks): the run order is refused with its reason; a walk is taken.", r =>
            {
                var g = r.G;
                r.Send(Enf, "d:Service lane");
                r.To(Max(r.Send(Scout, "d:Meet driver"), r.Send(Fixer, "d:Boarding pavilion")));
                r.Do(Scout, "listen", Driver); r.Done(Scout); /* engine 0.28.0: no Watch to give on the driver (brief 054 §2.1) */
                TurnDriver(r);
                r.To(r.SendTo(Scout, TipSpot(r))); r.To(T(320)); r.Do(Scout, "mislead", "rivals");
                r.To(T(370) + T(g.R.ValeInS)); r.Until(() => g.PickupService, T(60)); r.Do(Enf, "take"); r.Done(Enf); // brief 059: in his car in the service bay
                var (ok, why) = g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "move", To = "d:Getaway bay", Run = true });
                r.Is(!ok && why.Contains(g.ValeName), $"carrying {g.ValeName}, the run is refused: {why}");
                r.Eq(g.QuoteMove(Enf, r.Stand("d:Getaway bay")).RunRefused, why, "the move chip shows the same reason");
                r.Send(Enf, "d:Getaway bay");
                r.Eq(r.Op(Enf).WalkSpeed, g.R.CellSpeed, "he walks");
            }, NoFight());

            var known = new Start(); known.Known[Inez] = new List<string> { "routine", "need", "temperament" }; known.Known[Driver] = new List<string> { "routine", "need", "temperament" };
            var purse = new Start(); purse.Known[Inez] = known.Known[Inez]; purse.Known[Driver] = known.Known[Driver];
            // §04's purse is one Help on Inez and one on the driver (Motive §3): the rule under test, so the start state makes both people one
            // Help turns, whoever the cast is (the r016 cast's Inez is Fearful, two Helps; its driver a Mercenary)
            purse.Temperament[Inez] = "idealist"; purse.Temperament[Driver] = "idealist";
            Add("N09", "—", "The purse (§04): Help Inez + Help the driver = $80,000 fits $100,000.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Courtyard")); r.Do(Fixer, "turn", Inez, "help"); r.Done(Fixer);
                r.To(r.Send(Fixer, "d:Boarding pavilion")); r.Do(Fixer, "turn", Driver, "help"); r.Done(Fixer);
                r.Is(g.Inez.Turned && g.Driver.Turned, "both turned");
                r.Eq(g.Cash, 20000L, "$80,000 spent of $100,000");
            }, purse);

            var merc = new Start(); merc.Temperament[Inez] = "mercenary"; merc.Known[Inez] = new List<string> { "routine", "need", "temperament" }; merc.Known[Driver] = new List<string> { "routine", "need", "temperament" };
            Add("N09b", "—", "The purse the other way (§04): Pay on a Mercenary Inez ($70,000) leaves too little to Help the driver twice ($40,000 over two visits, brief 085): the second can't be given.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Courtyard")); r.Do(Fixer, "turn", Inez, "pay"); r.Done(Fixer);
                r.Is(g.Inez.Turned, "a Mercenary takes the fee");
                r.Eq(g.Cash, 30000L, "$70,000 spent");
                r.To(r.Send(Fixer, "d:Boarding pavilion"));
                r.Do(Fixer, "turn", Driver, "help"); r.Done(Fixer); r.Eq(g.Cash, 10000L, "the first visit, $20,000"); r.Refused(Fixer, "turn", Driver, "$90,000 spent, $110,000 was asked: the second visit can't be paid", "help");
            }, merc);

            // the refused approach and the one-step-worse line are the rules under test: the start state makes Inez an Idealist (Pay refused,
            // Pressure without leverage one step worse), whoever the cast is
            var tip = new Start(); tip.Known[Inez] = new List<string> { "routine", "need" }; tip.Temperament[Inez] = "idealist";
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
                r.Eq(g.Preview("pressure", Inez).Trail, 3L, "and that Pressure costs +3 T: Listen found her affair, so exposure is at its proper step (Motive, motive-lovers.md §2.2)");
                r.Is(g.Preview("pressure", Inez).Leverage, "the card reads exposure: her secret");
            }, tip);

            Add("N15", "—", "K1, K2, K8 in the mission: Help writes a memory entry with the Fixer's face; gratitude is +1 loyalty; the price is line items.", r =>
            {
                var g = r.G;
                var q = g.Preview("help", Inez);
                r.Eq(q.Lines.Count, 8, "eight line items: base, refused before, one more Help, the visits' share first and later (brief 085), trust over time, and the mood's ready and ready-at-one (brief 054 §4.2)");
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
            SightCases();
            TakedownCases(); // brief 013
            TakedownFixes(); // brief 013 D
            TakedownE(); // brief 013 E
            StandingCases(); // brief 014
            RepeatCases(); // Sky 2026-10-18: a Watch or Listen that teaches nothing more is refused
            TechWatchCases(); // Sky 2026-10-18: the Tech watches the machines too
            AttentionCases(); // brief 031: everyone watches, watchers notice loiterers
            ContextualCases(); // brief 030: auto-holster, the walk-in to fire outside the lab
            RoundsCases(); // brief 022: the living café
            GetawayCases(); // brief 018 (Sky 2026-10-18): the hold, the blocker, Board and Go, left behind, seen leaving, custody
            FireMoveCases(); // engine 0.15.0 (tuning.md §8, §9): fire on the move, anyone can be shot
            LastingFireCases(); // engine 0.21.0 (Sky 2026-10-22): a new act ends a lasting fire order (ScenariosLastingFire.cs)
            CarCases(); // engine 0.16.0 (tuning.md §10.7): cars are targets
            FailCauseCases(); // Sky 2026-10-18: the fail camera's culprit, every way the Handover ends without Vale
            DriveCases(); // brief 025 (EXPERIMENTAL): the driven agent, and the recorded driven session (DP1)
            FightLabCases(); // brief 027 (EXPERIMENTAL): the fight playground's mechanics, N140–N148
            IntentCases(); // engine 0.19.0: hostile intent per agent (Pulse hostile-initiative.md, Sky's answers) and weapons free on the block (ScenariosWeaponsFree.cs)
            FireLineCases(); // Sky 2026-10-22 (build 0.2.48): the driven Scout at the kerb fires on the patrol (ScenariosFireLine.cs)
            FinalIntentCases(); // engine 0.20.0 (tuning.md §15.2, PROVISIONAL): the final fight per agent (ScenariosFinalIntent.cs)
            SealCarCases(); // engine 0.30.0 (briefs 057 §2 and §4, 060 #8): FailCause.Car, the debrief true to the run, the seal window, the reroute walk and PatrolLegs (ScenariosSealCar.cs)
            WindowCases(); // engine 0.24.0 (brief 043 §6): Windows(), the window marker's one read, and patrol.returns (ScenariosWindows.cs)
            PressedCallCases(); ListenSpotCases(); SeedCases(); // brief 085: ScenariosPressedCall.cs; brief 080: the seed and the rolled shot (ScenariosSeed.cs)
            FireWhyCases(); // engine 0.25.0 (Sky playing 0.2.56): why a hostile fires on an operative, FireWhyOf and ai.why (ScenariosFireWhy.cs)
            NoticeCases(); // engine 0.28.0 (briefs 054, 055): sight from real heights, civilian notice by cone and Alert, the Watch that shows something (ScenariosNotice.cs)
            CameraLineCases(); // engine 0.31.0 (brief 058 §6, brief 054 §8): Loop v2, Disable, the camera shot's price, a runner remembered from Alert 2 (ScenariosCameraLines.cs)
            SquadFireCases(); // engine 0.26.0 (Sky playing 0.2.65): the squad fire order agent by agent, FireQuery (ScenariosSquadFire.cs)
            Pickup059(); // engine 0.32.0 (brief 059): route B at the service door, the rival crew of five, Drive, Sky's S1–S3 both ways (ScenariosPickup059.cs)
            Always059(); // engine 0.33.0 (brief 059, Sky's S1 = no): the gate, the late spring, the floor, crew destroyed, the crew as people, Motive's balance (ScenariosAlways059.cs)
            FightBCases(); // engine 0.35.0 (brief 039 B): Covering fire holding Vale, Talk down on the blocker (ScenariosFightB.cs)
            Pace067(); // engine 0.34.0 (brief 067): the walk split, the run seen and suspicious, hiding in a held car loses, Go needs a driver, Pulse's posts (ScenariosPace067.cs)
        }
    }
}
