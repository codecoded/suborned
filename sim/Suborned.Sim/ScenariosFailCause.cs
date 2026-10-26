// The fail camera's culprit (Sky 2026-10-18: "if you fail mission, the camera should pan around the item / person that failed you so you
// know what it was for next time"; FailCause.cs). Every way the Handover can end without Vale carries its cause: the fail code, why, the
// deciding tick, the culprits (the first is the one to orbit) where they stood and faced, the operatives involved, and, where no single
// culprit decided it (the seal clock), the report that started it. Read-only: none of these runs hashes differently for it.
// Fail kinds that other scenarios already end in carry these checks in place: S09 (vale-dead, shot), N58 (vale-dead, killed),
// N36 (rivals-reach-vale, the Enforcer downed in the safe fight), N68 (sealed, a confirmed report), N69b (entry-seen-camera),
// N112 (the car leaves without Vale: withdraw). The rest are here; several replay the fail route scripts of
// contracts/missions/handover/routes/ (named beside them). Brief 018's rival at the car (rivals-reach-vale, reached-car) has no route
// that reaches it on today's data: sim/Suborned.Sim.Tests/FailCauseTests.cs builds that cause from a placed fixture.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>The run ended with this cause: its code, why, single or not, at the run's last tick; the operatives it names; the first
        /// culprit (the one to orbit) by id and kind, placed. first "": no culprit. Returns the cause (null if missing).</summary>
        static FailCause? CauseIs(Run r, string fail, string why, bool single, string first, string kind, string ops)
        {
            var g = r.G; var c = g.Cause;
            if (!r.Is(c != null, $"the run carries the fail camera's cause ({fail})")) return null;
            r.Eq((c!.Fail, c.Why, c.Single, c.Tick), (fail, why, single, g.Tick), "the cause: fail, why, single, and the deciding tick is the run's last");
            r.Eq(string.Join(",", c.Ops), ops, "the operatives it names");
            if (first.Length == 0) { r.Eq(c.Culprits.Count, 0, "no single culprit"); return c; }
            var b = c.Culprits.FirstOrDefault();
            r.Is(b != null && b.Id == first && b.Kind == kind && b.Placed, $"the culprit to orbit is the {kind} {first}, placed (got {b})");
            return c;
        }
        /// <summary>A watcher culprit stands and faces as the engine has it at the deciding tick (the run is over: its pose now is that tick's).</summary>
        static void PosedAs(Run r, Blamed? b, Pt at, Pt facing, string what)
        {
            if (b == null) return;
            r.Is(b.Placed && b.At == at && b.Faces && b.Facing == facing, $"{what}: at {at} facing {facing} (got {b})");
        }
        /// <summary>A shooter culprit: where it fired from, facing the one it hit, within its weapon's reach.</summary>
        static void ShotFrom(Run r, FailCause? c, string shooter, string victim)
        {
            if (c == null || c.Culprits.Count < 2) { r.Is(false, "a shooter and a victim"); return; }
            var s = c.Culprits[0]; var v = c.Culprits[1];
            r.Is(s.Id == shooter && s.Role == "shot" && v.Id == victim && v.Role == "victim" && v.Placed, $"the shooter {shooter}, then the victim {victim} (got {s}; {v})");
            long dot = (long)s.Facing.X * (v.At.X - s.At.X) + (long)s.Facing.Y * (v.At.Y - s.At.Y);
            r.Is(s.Placed && s.Faces && dot > 0 && G.Within(s.At, v.At, r.G.R.Carbine.MaxCm), $"the shooter faces the victim from within a weapon's reach ({s.At} → {v.At})");
        }
        /// <summary>A route script's orders (suborned.sim.script/1 "orders"), played at their ticks.</summary>
        static void Script(Run r, string orders) => Orders(r, JNode.Parse(orders, "orders").Items.Select(Order.Parse).ToArray());

        static void FailCauseCases()
        {
            Add("N119", "—", "The fail camera: entry seen by the guard (§07). The Enforcer walks to the public door and enters with the guard at it: the culprit is the guard at his post, facing the way he watches the door; the Enforcer is the operative he saw.", r =>
            {
                var g = r.G;
                r.To(r.Send(Enf, "d:Office entrance"));
                r.Do(Enf, "enter");
                r.Eq(g.FailCode, "entry-seen-guard", "the guard at the door sees the entry");
                var c = CauseIs(r, "entry-seen-guard", "seen", true, "guard", "guard", Enf);
                PosedAs(r, c?.Culprits.FirstOrDefault(), r.Stand(g.S.GuardPost), g.S.GuardFacing, "the guard at his post");
                r.Eq(c?.Culprits.FirstOrDefault()?.Role, "saw", "he saw it");
                r.Is(c?.Origin == null, "a single culprit: no origin");
            });

            Add("N120", "—", "Entry and the patrol (§07; engine 0.34.2, Sky 2026-10-26: \"entry seen by patrol\" is \"no, not a failure\"; rulebook cell.enter.seen). The patrol robot that sees the Enter no longer fails the mission: the Enter goes on and the robot radios a report of the seen act, which lands attention.report.call later as a found fault (Alert at least 1, no Trail, a sighting, the dial's dispatch to the door). The yard sweep never sees the public door (brief 067 A), so the robot is brought there by an alarm: route G-kill with the reroute at 0:51 (the patrol back on its sweep at 3:31, after the carry), the guard's missed 4:00 mark walks it round to his post; it finds nothing (report.unfounded) and, standing down, faces the door. The Tech plants a second loop as the first runs out (4:30, live 4:50), and the Enforcer, waiting at the door, enters the first tick the patrol sees it. (0.33.1-0.34.1: entry-seen-patrol, a fail; a rulebook pinned before 0.34.2 still fails there: FailCauseTests.)", r =>
            {
                var g = r.G; var door = g.S.At(g.S.GuardPost);
                r.Is(!g.SweepCoversEntrance, "on its sweep the patrol never sees the public door (the service yard's)");
                Orders(r, Mv(0, Tech, "cabinet", true), MvTo(0, Enf, new Pt(5650, -4750)), MvTo(0, Scout, new Pt(3800, -5000)), MvTo(0, Fixer, new Pt(725, -5025)),
                    Act(310, Scout, "watch", "guard"), MvTo(510, Scout, new Pt(725, -4625)), Act(340, Tech, "reroute", "patrol"), Act(1300, Tech, "loop", "entrance"), MvTo(1400, Enf, new Pt(6520, -4110)),
                    Act(1510, Enf, "takedown", "guard", "kill"), Act(1540, Enf, "carry", "guard"), MvTo(1570, Enf, new Pt(6275, -1875)), Act(1815, Enf, "stash", "stash/office-service-door/door"),
                    Mv(1915, Enf, "d:Office entrance", true));
                r.To(T(270)); r.Do(Tech, "loop", "entrance"); // the second loop as the first runs out (4:30), live 4:50 to 6:50
                r.Is(r.Until(() => g.LoopOn && g.PatrolWatching(door), T(60)), $"the alarm brings the patrol round: it sees the door with the loop live ({Report.Clock(g.Tick)})");
                r.Is(!g.GuardAtDoor && !g.CameraWatching("entrance", r.Op(Enf).Pos), "no guard at the door, the camera looped: only the patrol sees the Enter");
                int alert0 = g.Alert, sight0 = g.Sightings.Count;
                r.Do(Enf, "enter");
                r.Eq(g.Result, Outcome.None, "the patrol's sight of the Enter is no fail");
                r.Is(g.Log.Exists(l => l.Code == "entry.seen" && l.Tick == g.Tick), "the patrol sees it and radios it (entry.seen)");
                r.Eq(g.EntryReport.state, "pending", "its report is on its way");
                r.Eq(g.EntryReport.landAt, g.Tick + T(g.R.AttnCallS), "it lands attention.report.call later (by radio)");
                r.Wait(T(g.R.AttnCallS));
                r.Eq(g.EntryReport.state, "landed", "the report lands");
                r.Is(g.Log.Exists(l => l.Code == "entry.reported") && g.Log.Exists(l => l.Code == "alarm.dispatches" && l.Tick == g.Tick), "a found fault: entry.reported, and the dial dispatches to the door");
                r.Is(g.Alert >= Math.Max(alert0, g.R.AttnAlert), $"Alert at least {g.R.AttnAlert} (Alert {g.Alert}; was {alert0})");
                r.Is(g.Sightings.Count == sight0 + 1 && g.Sightings[^1].Watcher == "patrol" && g.Sightings[^1].Op == Enf && g.Sightings[^1].At == door, "a sighting: the patrol saw the Enforcer at the door");
                r.Done(Enf);
                r.Is(g.FailCode != "entry-seen-patrol" && g.Vale == ValeAt.WithEnforcer, $"the Enter lands: Vale is with the Enforcer ({g.Result} {g.FailCode})");
            });

            Add("N121", "—", "The fail camera: the abduction seen by the pickup camera (§07; route fail-take-seen-camera): the driver unturned, the rivals sent away, the Take at the official car under the live pickup camera. The culprit is the pickup camera, facing the car; Vale is the second, where he stood.", r =>
            {
                var g = r.G;
                Script(r, "[{\"tick\":0,\"op\":\"enforcer\",\"do\":\"move\",\"to\":\"d:Boarding pavilion\"},{\"tick\":0,\"op\":\"scout\",\"do\":\"move\",\"to\":\"rival-park\"},{\"tick\":3200,\"op\":\"scout\",\"do\":\"mislead\",\"target\":\"rivals\"},{\"tick\":3700,\"op\":\"enforcer\",\"do\":\"take\"}]");
                r.Eq(g.FailCode, "abduction-seen-camera", "the pickup camera sees the Take");
                var c = CauseIs(r, "abduction-seen-camera", "seen", true, "camera:pickup", "camera", Enf);
                var cam = g.S.Cameras.Single(x => x.Id == "pickup");
                PosedAs(r, c?.Culprits.FirstOrDefault(), cam.At, cam.Facing, "the pickup camera on its pole");
                r.Is(c != null && c.Culprits.Count == 2 && c.Culprits[1].Kind == "vale" && c.Culprits[1].At == g.ValePos() && g.CarBodyCm("car:official", c.Culprits[1].At) is int vb && vb >= 0 && vb <= g.R.CarReachCm, "then Vale, at his car (engine 0.33.1: at its kerb-side door)");
            });

            Add("N122", "—", "The fail camera: the rivals beat the cell to Vale (§07; route fail-rivals-reach-vale; brief 059 F2): route B without the false tip; the rival car follows Vale's car round and its wheelman reaches Vale's door before the Take in the service bay is done. The culprit is the wheelman where he stands; no operative is named.", r =>
            {
                var g = r.G;
                Script(r, "[{\"tick\":0,\"op\":\"enforcer\",\"do\":\"move\",\"to\":\"d:Service lane\"},{\"tick\":0,\"op\":\"scout\",\"do\":\"move\",\"to\":\"d:Meet driver\"},{\"tick\":0,\"op\":\"fixer\",\"do\":\"move\",\"to\":\"d:Boarding pavilion\"},{\"tick\":776,\"op\":\"scout\",\"do\":\"move\",\"to\":\"d:Meet driver\"},{\"tick\":797,\"op\":\"scout\",\"do\":\"listen\",\"target\":\"driver\"},{\"tick\":1217,\"op\":\"fixer\",\"do\":\"turn\",\"target\":\"driver\",\"approach\":\"pay\"},{\"tick\":1717,\"op\":\"scout\",\"do\":\"move\",\"to\":[9300,-3100]},{\"tick\":3860,\"op\":\"enforcer\",\"do\":\"take\"}]"); r.Until(() => g.Over, T(60)); // c022: the Scout steps to the driver before the Listen (3 m); brief 059: the Take in the service bay once Vale's car parks (6:25.9)
                r.Eq(g.FailCode, "rivals-reach-vale", "the rivals reach Vale first");
                var c = CauseIs(r, "rivals-reach-vale", "tail", true, "rival-5", "rival", "");
                var wq = g.MemberPose(g.Crew[4]); if (!r.Is(wq.HasValue, $"the wheelman is on the street ({g.FailCode} at {Report.Clock(g.Tick)}, {g.Crew[4].Mode} {g.CarSt})")) return;
                var wp = wq!.Value;
                PosedAs(r, c?.Culprits.FirstOrDefault(), wp.pos, wp.facing, "the wheelman");
                r.Is(c != null && c.Culprits.Count == 2 && c.Culprits[1].Kind == "vale", "then Vale");
            });

            Add("N123", "—", "The fail camera: Vale is driven away (§07, Sky's answer 3a; brief 059 K2): nobody touches anything; at 6:13 Vale is in his car and his driver drives him away. The culprit is the official car, then Vale; no operative is named.", r =>
            {
                var g = r.G;
                r.To(T(g.S.Over));
                r.Eq((g.FailCode, g.Tick), ("vale-leaves", T(g.S.ValeAtCar) + T(g.R.ValeInS)), "at 6:13 he is driven away");
                var c = CauseIs(r, "vale-leaves", "driven-away", true, "car", "car", "");
                r.Is(c != null && c.Culprits.Count == 2 && c.Culprits[1].Kind == "vale" && c.Culprits[1].Role == "left", "then Vale leaving in it");
            });

            Add("N124", "—", "The fail camera: Vale leaves in his car at the close (§07; route fail-vale-leaves; brief 059 K4): the driver turned at the start, the Scout sends the rivals away late (the tip runs past the close, so no tail comes back), nobody takes Vale; at 8:30 the driver gives up and drives him on. The culprit is the official car where it stands (the service bay), then Vale.", r =>
            {
                var g = r.G;
                Script(r, "[{\"tick\":0,\"op\":\"scout\",\"do\":\"move\",\"to\":[9300,-3100]},{\"tick\":2361,\"op\":\"scout\",\"do\":\"mislead\",\"target\":\"rivals\"}]");
                r.To(T(g.S.Over));
                r.Eq(g.FailCode, "vale-leaves", "Vale leaves in the official car");
                var c = CauseIs(r, "vale-leaves", "too-late", true, "car", "car", "");
                r.Is(c != null && c.Culprits[0].At == g.CarPos() && g.CarPos() == g.S.CarStand(g.S.CarService) && c.Culprits.Count == 2 && c.Culprits[1].Kind == "vale" && c.Culprits[1].Role == "left", "the official car in the service bay, then Vale leaving in it");
            }, Turned(Driver));

            Add("N125", "—", "The fail camera: the Enforcer down (§07; route fail-enforcer-down): an unprepared shot at the guard from inside his cone; the Enforcer trades fire until the response unit's officer drops him. The culprit is the shooter that downed him, where it fired from and facing him; the Enforcer is the operative and the second culprit (the victim).", r =>
            {
                var g = r.G;
                // brief 030 (weapons.autoHolster): the route's carbine, drawn by its fire order, would holster 4 s after the guard fight goes
                // quiet, and the rivals then find him holstered; drawn with F (a draw order a tick before the fire) it stays drawn, as the
                // route had it, so the response unit's officer is still the one who drops him (route fail-enforcer-down itself moved: Waymark)
                Script(r, "[{\"tick\":0,\"op\":\"enforcer\",\"do\":\"move\",\"to\":\"d:Office approach\"},{\"tick\":353,\"op\":\"enforcer\",\"do\":\"draw\"},{\"tick\":354,\"op\":\"enforcer\",\"do\":\"fire\",\"target\":\"guard\"}]");
                r.Until(() => g.Over, T(120));
                r.Eq(g.FailCode, "enforcer-down", "the Enforcer is down");
                // Changed 2026-10-19 (the door on the west face): which of the two officers drops him is the block's (it was security-1, it is
                // security-2 from the new forecourt); the test is that the culprit is whoever landed the last hit on him. Engine 0.34.0 (brief 067
                // A): the patrol sweeps the service yard and no longer breaks into the guard fight at the first shot, so the Enforcer, at 4 of 6
                // health after the guard, stands at the approach until the rival crew arrives (1:10.9) and a rival drops him (1:30.5), before the
                // response unit fires: the culprit is a rival (the fail camera's kind follows: rival, or officer for the response unit)
                var last = r.Shots().FindLast(s => s.hit && s.target == Enf).by ?? "";
                r.Is(last.StartsWith("security-", StringComparison.Ordinal) || last.StartsWith("rival-", StringComparison.Ordinal), $"a response officer or a rival drops him ({last})");
                var c = CauseIs(r, "enforcer-down", "shot", true, last, last.StartsWith("rival-", StringComparison.Ordinal) ? "rival" : "officer", Enf);
                ShotFrom(r, c, last, Enf);
                r.Is(r.Shots().FindLast(s => s.hit).by == last, "the last hit of the run is the culprit's");
            });

            Add("N126", "—", "The fail camera: the driver shot during the handover (§07): the Enforcer at the boarding pavilion fires on the driver (forced) after Vale is out. The culprit is the Enforcer where he fired from, facing the driver; the driver is the second (the victim), where he fell.", r =>
            {
                var g = r.G;
                r.To(r.Send(Enf, "d:Boarding pavilion"));
                r.To(T(g.S.ValeExits) + T(1));
                r.Fire(Enf, Driver, force: true);
                r.Until(() => g.Over, T(20));
                r.Eq(g.FailCode, "driver-shot", "the driver is shot with Vale out");
                var c = CauseIs(r, "driver-shot", "shot", true, Enf, "op", Enf);
                ShotFrom(r, c, Enf, Driver);
                r.Is(c != null && c.Culprits[0].At == r.Op(Enf).Pos, "he fired from where he stands");
            });

            Add("N127", "—", "The fail camera: the seal, and what started it (§07; route fail-district-seals without the loop, and without its second fire order, refused there: the guard is already down): the entrance camera shows the Enforcer's drawn weapon at 2:25.1 and its watchers call the response; the guard's missed check-in at 2:30 calls it again, earlier, and the district seals 2:25 after that unit is on the street. No single culprit: the origin is the camera's report (the first, which raised the response), where the camera stands; the last move is the desk's report of the empty post, whose clock ran out.", r =>
            {
                var g = r.G;
                Script(r, "[{\"tick\":0,\"op\":\"enforcer\",\"do\":\"move\",\"to\":[5650,-4750]},{\"tick\":1330,\"op\":\"enforcer\",\"do\":\"move\",\"to\":[6925,-4250]},{\"tick\":1450,\"op\":\"enforcer\",\"do\":\"fire\",\"target\":\"guard\"},{\"tick\":1520,\"op\":\"enforcer\",\"do\":\"move\",\"to\":\"d:Behind café\"},{\"tick\":1520,\"op\":\"enforcer\",\"do\":\"stance\",\"stance\":\"hold\"}]"); // tuning.md §7.9 (2026-10-20): he holds his fire behind the café, so the police (who now answer fire from beyond their reach) never chase him down and the seal is the fail // c022: the Enforcer fires from (69.5, -42.5), beside the door on the west face
                r.Until(() => g.Over, T(300));
                r.Eq(g.FailCode, "sealed", "the district seals");
                var c = CauseIs(r, "sealed", "sealed", false, "", "", Enf);
                var o = c?.Origin; var cam = g.S.Cameras.Single(x => x.Id == "entrance");
                r.Is(o != null && o.Because == "response" && o.Report == "drawn-seen:enforcer" && o.Ops.SequenceEqual(new[] { Enf }), $"the origin: the drawn weapon reported, which called the response (got {o?.Report}, {o?.Because})");
                PosedAs(r, o?.Reporter, cam.At, cam.Facing, "its reporter, the entrance camera");
                r.Is(o != null && o.Tick < g.Tick && g.Log.Exists(l => l.Tick == o.Tick && l.Code == "security.called"), "at the tick the response was called");
                var m = c?.LastMove;
                r.Is(m != null && m.Report == "post-empty:guard" && m.Reporter?.Id == "desk" && m.Reporter.At == r.Stand(g.S.GuardPost) && m.Tick > o!.Tick, "the last move: the desk's report of the empty post, at the post");
            }, new Start { OneInput = false }); // brief 039: the recorded fixture's stance hold

            Add("N128", "—", "The fail camera: the seal of a loud final fight (route careless-body-found): the guard killed at his post and left there, the Enter at once; the Tech leaves the cabinet and the entrance camera's watcher reads the dead guard (Alert 2). The Enforcer comes out into the loud fight, whose seal clock runs out at 5:37. No single culprit: the origin is what made the fight loud, the camera's report that raised Alert to 2, where the camera stands.", r =>
            {
                var g = r.G;
                Script(r, "[{\"tick\":0,\"op\":\"tech\",\"do\":\"move\",\"to\":\"cabinet\",\"run\":true},{\"tick\":0,\"op\":\"enforcer\",\"do\":\"move\",\"to\":[6925,-4250]},{\"tick\":0,\"op\":\"scout\",\"do\":\"move\",\"to\":\"d:Office approach\"},{\"tick\":0,\"op\":\"fixer\",\"do\":\"move\",\"to\":\"crew:fixer\"},{\"tick\":166,\"op\":\"tech\",\"do\":\"loop\",\"target\":\"entrance\"},{\"tick\":462,\"op\":\"scout\",\"do\":\"watch\",\"target\":\"guard\"},{\"tick\":662,\"op\":\"scout\",\"do\":\"move\",\"to\":\"crew:scout\"},{\"tick\":1060,\"op\":\"tech\",\"do\":\"reroute\",\"target\":\"patrol\"},{\"tick\":1440,\"op\":\"enforcer\",\"do\":\"move\",\"to\":[6520,-4110]},{\"tick\":1510,\"op\":\"enforcer\",\"do\":\"takedown\",\"target\":\"guard\",\"how\":\"kill\"},{\"tick\":1540,\"op\":\"enforcer\",\"do\":\"enter\"},{\"tick\":1540,\"op\":\"tech\",\"do\":\"move\",\"to\":[675,-4625]},{\"tick\":1540,\"op\":\"scout\",\"do\":\"move\",\"to\":[1175,-4825]},{\"tick\":1540,\"op\":\"fixer\",\"do\":\"move\",\"to\":[1025,-4625]},{\"tick\":1920,\"op\":\"enforcer\",\"do\":\"stance\",\"stance\":\"hold\"}]"); // c022's careless-body-found, without its three free stances and the Enforcer's walk to the car (tuning.md §7.9, 2026-10-20: with the 16 m sidearm the three can no longer hold the officers off from beyond their reach, and the police now answer such fire: walked out, the Enforcer goes down before the seal; he holds with Vale where he came out instead)
                r.To(T(g.S.Over));
                r.Eq(g.FailCode, "sealed", "the loud fight's seal runs out");
                var c = CauseIs(r, "sealed", "sealed", false, "", "", Enf);
                var o = c?.Origin; var cam = g.S.Cameras.Single(x => x.Id == "entrance");
                r.Is(o != null && (o.Because == "loud-fight" || o.Because == "confirmed") && o.Report.StartsWith("report:dead:", StringComparison.Ordinal), $"the origin: the dead guard's report, which made the fight loud (at level 4 a unit confirms the body first, and that starts the clock) (got {o?.Report}, {o?.Because})");
                PosedAs(r, o?.Reporter, cam.At, cam.Facing, "its reporter, the entrance camera");
                r.Is(o != null && g.Ledger.Exists(l => l.tick == o.Tick && l.what == "alert" && l.now >= 2 && l.now - l.delta < 2), "it raised Alert to 2 at that tick");
                r.Is(c?.LastMove == null, "nothing moved the clock after the fight set it");
            }, new Start { Security = 4, OneInput = false }); // brief 039: the recorded fixture's stance hold; tuning.md §7.9 (2026-10-20): at the Handover's level 2 the officers now reach him before its 2:25 seal; level 4 (swarm) seals in 1:30

            Add("N129", "—", "The fail camera: a withdrawal has no culprit (the cell pulled out at 0:30).", r =>
            {
                var g = r.G;
                r.Wait(T(30));
                r.Is(g.Issue(new Order { Tick = g.Tick, Do = "withdraw" }).ok, "withdraw taken");
                r.Eq(g.Result, Outcome.Withdrawn, "the cell withdraws");
                var c = CauseIs(r, "withdraw", "withdrew", false, "", "", "");
                r.Is(c != null && c.Origin == null, "and nothing started it");
            });
        }
    }
}
