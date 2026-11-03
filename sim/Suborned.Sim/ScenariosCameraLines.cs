// Engine 0.31.0 scenarios: brief 058 §6 (Loop v2, Disable, the camera shot's price; Sky 2026-10-25: a loop runs 2:00, Disable is an
// incident) and brief 054 §8 (Sky 2026-10-25: civilians remember a runner from Alert 2). GameCameraLines.cs, GameCombat.cs RunAttention.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>The Trail the ledger charged for one cause.</summary>
        static int TrailFor(Game g, string cause) { int t = 0; foreach (var l in g.Ledger) if (l.what == "trail" && l.cause == cause) t += l.delta; return t; }
        /// <summary>A spot the operative can walk to with a clear line to the camera within `cm`, nearest the operative first; null if none.</summary>
        static Pt? CameraSpot(Run r, string op, string camId, int cm = 1200)
        {
            var g = r.G; var cam = g.S.Cameras.First(c => c.Id == camId);
            return g.S.Grid.Nearest(r.Op(op).Pos, q => G.D2(q, cam.At) <= (long)cm * cm && g.S.Sight(q, cam.At, g.R.Eye), 9000);
        }
        /// <summary>The Tech at the cabinet, running there from the start; returns the tick she arrives.</summary>
        static int TechAtCabinet(Run r) { r.To(r.Send(Tech, "cabinet", run: true)); return r.G.Tick; }

        static void CameraLineCases()
        {
            Add("LOOP-RUN", "—", "Loop v2 (brief 058 §2, rulebook cell.loop.run): the loop lands after loop.s (+1 T); the Tech walks 30 m and more away and the entrance camera stays looped until the landing + 2:00, its window counting to that tick; then loop.ends and it sees again.", r =>
            {
                var g = r.G; if (!r.Is(g.R.LoopRuns, "Loop v2 in the rulebook (cell.loop.run)")) return; var door = r.Stand("d:Office entrance");
                TechAtCabinet(r);
                r.Do(Tech, "loop", "entrance"); r.Eq(r.Op(Tech).BusyEnd - g.Tick, T(g.R.LoopS), "the splice takes loop.s (20 s)"); r.Done(Tech);
                int landed = g.Tick, ends = landed + T(g.R.LoopRunS);
                r.Eq(TrailFor(g, "loop"), g.R.LoopTrail, "+1 T when it lands");
                r.Is(g.LoopOn && !g.CameraWatching("entrance", door, crowd: false), "looped: the camera sees nothing at the door");
                var w = g.Windows().Find(x => x.Kind == "loop" && x.Thing == "entrance");
                r.Is(w != null && w.Until == ends, $"the window counts to the landing + loop.run ({Report.Clock(ends)})");
                var away = r.Stand("d:Courtyard");
                r.To(r.SendTo(Tech, away));
                r.Is(G.Dist(r.Op(Tech).Pos, g.S.At("cabinet")) >= 3000, $"the Tech walks off {G.Dist(r.Op(Tech).Pos, g.S.At("cabinet")) / 100} m");
                r.Is(g.LoopOn && !g.CameraWatching("entrance", door, crowd: false), "and the loop runs on (no tether)");
                r.To(ends - 1);
                r.Is(g.LoopOn && !g.CameraWatching("entrance", door, crowd: false), "a tick before the end: still looped");
                r.To(ends);
                r.Is(!g.LoopOn && g.Log.Exists(l => l.Code == "loop.ends" && l.Tick == ends), $"loop.ends at {Report.Clock(ends)}");
                r.Is(g.CameraWatching("entrance", door, crowd: false), "the camera sees the door again");
                r.Is(g.Windows().All(x => x.Kind != "loop"), "the window is gone");
            });

            Add("LOOP-RE", "—", "Loop v2 (brief 058 §2): a re-splice of the camera already looping, landing 1:50 into its run (ordered at 1:30), runs a fresh 2:00 from that landing with no gap (no stacking), +2 T in all. Ordered later than 1:40 the first run ends before the re-splice lands and the camera is live in between.", r =>
            {
                var g = r.G; if (!r.Is(g.R.LoopRuns, "Loop v2 in the rulebook (cell.loop.run)")) return; var door = r.Stand("d:Office entrance");
                TechAtCabinet(r);
                r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                int first = g.Tick;
                r.To(first + T(110 - g.R.LoopS));
                r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                int second = g.Tick, ends = second + T(g.R.LoopRunS);
                r.Eq(second, first + T(110), "the re-splice lands 1:50 into the first run");
                r.Is(!g.Log.Exists(l => l.Code == "loop.ends"), "no gap: the first run had not ended");
                r.Eq(g.LoopUntil, ends, "a fresh 2:00 from the new landing, not added to the first");
                r.Eq(TrailFor(g, "loop"), 2 * g.R.LoopTrail, "+2 T in all: each splice is charged");
                r.To(first + T(g.R.LoopRunS));
                r.Is(g.LoopOn && !g.CameraWatching("entrance", door, crowd: false), "past the first run's end: still looped");
                r.To(ends);
                r.Is(!g.LoopOn && g.Log.Count(l => l.Code == "loop.ends") == 1 && g.Log.Exists(l => l.Code == "loop.ends" && l.Tick == ends), $"one loop.ends, at {Report.Clock(ends)}");
            });

            Add("LOOP-ONE", "—", "One loop at a time (brief 058 §2): with the entrance loop running, a Loop on the pickup camera is refused loop-holds at no cost, naming the tick the running loop ends; once it has run out the pickup camera can be looped.", r =>
            {
                var g = r.G; if (!r.Is(g.R.LoopRuns, "Loop v2 in the rulebook (cell.loop.run)")) return;
                TechAtCabinet(r);
                r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                int trail = g.Trail;
                var a = g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "loop", Target = "pickup" });
                r.Eq((a.ok, a.code, a.tick), (false, "loop-holds", g.LoopUntil), "refused loop-holds, with the tick the entrance loop ends");
                r.Is(g.Trail == trail && r.Op(Tech).Busy == null && g.LoopCam == "entrance", "free: nothing charged, the Tech idle, the entrance loop still running");
                r.To(g.LoopUntil);
                r.Is(!g.LoopOn, "the entrance loop runs out");
                r.Do(Tech, "loop", "pickup"); r.Done(Tech);
                r.Is(g.LoopOn && g.LoopCam == "pickup", "then the pickup camera loops");
            });

            Add("DIS-REPORT", "—", "Disable (brief 058 §2, rulebook cell.disable; Sky 2026-10-25: an incident): the Tech cuts the entrance camera at 0:20 (10 s, +2 T): dead to the end. Its report is a missed check-in that lands at the desk's next scan of that screen + post-call-s: Alert 1, the dial's Alert-1 robots to the camera, standing down 60 s after; no scene held, no face, no Standing. The guard knocked out unseen after his 2:30 mark: engine 0.34.0 (brief 067 L), guard 74 walking past the door to her post finds him before his 4:00 mark, and her report is the second incident, Alert 2 (0.33.1: the 4:00 missed mark was).", r =>
            {
                var g = r.G; if (!r.Is(g.R.DisableOn, "cell.disable in the rulebook")) return; var cam = g.S.Cameras.First(c => c.Id == "entrance"); var door = r.Stand("d:Office entrance");
                TechAtCabinet(r);
                r.To(Math.Max(g.Tick, T(20)));
                int standing = g.Standing;
                r.Do(Tech, "disable", "entrance"); r.Eq(r.Op(Tech).BusyEnd - g.Tick, T(g.R.DisableS), "the cut takes disable.s (10 s)");
                int predicted = g.CutNoticedAt("entrance", r.Op(Tech).BusyEnd);
                r.Done(Tech);
                r.Send(Tech, "d:Courtyard"); // she leaves the cabinet: the robots the report brings would find her loitering there
                r.Eq(TrailFor(g, "disable:entrance"), g.R.DisableTrail, "+2 T when it lands");
                r.Is(g.CameraDark("entrance") && g.CameraDeadAs("entrance") == "cut" && !g.CameraWatching("entrance", door, crowd: false), "the camera is dead (cut, not shot)");
                var w = g.Windows().Find(x => x.Kind == "disable" && x.Thing == "entrance");
                r.Is(w != null && w.Until == predicted && w.From == g.Tick, $"the desk's clock: its report lands at {Report.Clock(predicted)} (the order line's read, CutNoticedAt)");
                r.Eq(g.AlertIfCut(), 2, "a second cut now would be the second incident: Alert 2");
                r.Eq(g.Alert, 0, "nothing until the desk reads the screen");
                r.To(predicted - 1); r.Eq(g.Alert, 0, "a tick before: still Alert 0");
                r.To(predicted);
                r.Is(g.Alert == 1 && g.Reported == 1 && g.Ledger.Exists(l => l.what == "alert" && l.cause == "report:cut:camera:entrance" && l.tick == predicted), "the report lands: a reported incident, Alert 1");
                r.Is(g.Log.Exists(l => l.Code == "camera.cut-reported" && l.Tick == predicted) && g.Log.Exists(l => l.Code == "alarm.dispatches" && l.Tick == predicted), "camera.cut-reported, and the desk dispatches");
                r.Is(g.Windows().All(x => x.Kind != "disable"), "the desk's clock is gone");
                var robots = g.Units.Where(u => u.Robot).ToList();
                // the dial's Alert-1 robots, those within robot reach of the camera (Pulse §12.2): at the entrance camera only the patrol on its sweep
                r.Is(robots.Count >= 1 && robots.Count <= g.DialNow.A1Robots && robots[0].Patrol, $"the dial's Alert-1 robots within reach, the patrol first ({robots.Count} of {g.DialNow.A1Robots} at level {g.SecurityLevel}: {string.Join(", ", robots.Select(u => u.Id))})");
                r.Is(robots.All(u => G.Within(u.Dest, g.S.Grid.NearestFree(cam.At) ?? cam.At, g.R.WitnessRangeCm)), "sent to the camera");
                r.Is(g.SceneHolds.Count == 0, "no scene held: an empty post with no body");
                r.Is(g.KnownFaces.Count == 0 && g.Standing == standing && g.Gunfire == 0, "no face, no Standing, no gunfire");
                r.To(T(65)); int techAt = r.Send(Tech, "cabinet"); // back to the cabinet at a walk as the robots stand down (a run past the guard is a found fault)
                r.To(T(80)); int enfAt = r.SendTo(Enf, BesideDoor); // the Enforcer walks to the door
                r.Is(r.Until(() => robots.All(u => u.St == "back" || u.St == "gone"), T(g.R.StandDownInquiryS + 60)) && g.Tick >= predicted + T(g.R.StandDownInquiryS), $"they stand down stand-down-inquiry-s after the report ({Report.Clock(g.Tick)}: {string.Join(", ", robots.Select(u => u.Id + " " + u.St))})");
                // route G with Disable in place of the Loop: the reroute lands 5 s before the 2:30 mark, the guard taken just after it, unseen
                r.To(Math.Max(g.Tick, techAt));
                int rerouteAt = Math.Max(g.Tick, g.NextMarkOf(g.GuardP, g.Tick + T(g.R.RerouteS)) - T(g.R.RerouteS + 5));
                r.To(rerouteAt); r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                r.To(Math.Max(g.Tick, enfAt));
                int mark = g.NextMarkOf(g.GuardP, g.Tick); r.To(mark + 1);
                r.To(r.SendTo(Enf, BesideGuard));
                r.Takedown("guard", "knockout"); r.Done(Enf);
                r.Eq(g.Alert, 1, "unseen: still Alert 1 (the dead camera sees nothing)");
                int next = g.NextMarkOf(g.GuardP, g.Tick);
                r.Eq(Report.Clock(next), "4:00.0", "his next mark: 4:00");
                // engine 0.34.0 (brief 067 L): guard 74, walking past the door to her post (a minute sooner since Pulse's waiting points), finds him
                // first: her radio is the second reported incident, before the mark (0.33.1: the 4:00 missed mark was)
                r.Is(r.Until(() => g.Alert >= 2, next - g.Tick), $"before his mark a second report lands ({Report.Clock(g.Tick)})");
                r.Is(g.Alert == 2 && g.Reported == 2 && g.Ledger.Exists(l => l.what == "alert" && l.tick == g.Tick && l.cause.Contains(GuardId)), $"guard 74's report of him is the second incident: Alert 2 ({string.Join(", ", g.Ledger.Where(l => l.what == "alert").Select(l => l.cause))})");
                r.Is(g.CameraDark("entrance"), "the camera is dead to the end");
            });

            Add("DIS-SECOND", "—", "Disable uses the first free incident (brief 058 §2): the entrance camera cut and reported (Alert 1); then a knockout behind a lone clerk's back, found by its first report: the second reported incident, Alert 2, though a knockout alone never takes Alert above 1.", r =>
            {
                var g = r.G; if (!r.Is(g.R.DisableOn, "cell.disable in the rulebook")) return;
                TechAtCabinet(r);
                r.Do(Tech, "disable", "entrance"); r.Done(Tech);
                int land = g.CutReps[0].LandAt; r.To(land);
                r.Eq((g.Alert, g.Reported), (1, 1), "the cut's report: Alert 1, the first incident");
                // the guard knocked out unseen just after a mark, the patrol on false orders; whichever report of it lands first is the second incident
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                int enfAt = r.SendTo(Enf, BesideDoor); r.To(Math.Max(g.Tick, enfAt));
                int mark = g.NextMarkOf(g.GuardP, g.Tick); r.To(mark + 1);
                r.To(r.SendTo(Enf, BesideGuard));
                r.Takedown("guard", "knockout"); r.Done(Enf);
                var a = ActOn(r, GuardId);
                r.Is(!a.Witnessed && g.Alert == 1, "nobody saw it: still Alert 1");
                r.Is(r.Until(() => a.Landed, T(240)), $"a report of him lands ({Report.Clock(g.Tick)})");
                r.Is(g.Alert == 2 && g.Reported == 2, $"the second reported incident: Alert 2 ({g.Ledger.LastOrDefault(l => l.what == "alert").cause})");
                r.Is(g.Alert < 3 && g.Gunfire == 0, "a knockout: no higher");
            });

            Add("DIS-TWO", "—", "Two cameras cut (brief 058 §2): each report is its own incident; the second lands at Alert 2. The refusals: a Loop or a Disable on a dead camera (camera-dead), a camera the cabinet doesn't reach (disable-wrong-target), anyone but the Tech (only-tech), away from the cabinet (stand-at-cabinet).", r =>
            {
                var g = r.G; if (!r.Is(g.R.DisableOn, "cell.disable in the rulebook")) return;
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "disable", Target = "entrance" }).code, "only-tech", "only the Tech cuts a line");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "disable", Target = "entrance" }).code, "stand-at-cabinet", "at the cabinet");
                TechAtCabinet(r);
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "disable", Target = "gate" }).code, "disable-wrong-target", "no camera of that name on the cabinet (unknown = reject)");
                r.Do(Tech, "disable", "entrance"); r.Done(Tech);
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "disable", Target = "entrance" }).code, "camera-dead", "a dead camera can't be cut again");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "loop", Target = "entrance" }).code, "camera-dead", "nor looped");
                r.Do(Tech, "disable", "pickup"); r.Done(Tech);
                r.Eq(g.CutCams.Count, 2, "two cameras cut");
                r.Eq(TrailFor(g, "disable:entrance") + TrailFor(g, "disable:pickup"), 2 * g.R.DisableTrail, "+2 T each");
                var lands = g.CutReps.Select(c => c.LandAt).OrderBy(x => x).ToList();
                r.To(lands[0]); r.Eq((g.Alert, g.Reported), (1, 1), $"the first report ({Report.Clock(lands[0])}): Alert 1");
                r.To(lands[1]); r.Eq((g.Alert, g.Reported), (2, 2), $"the second ({Report.Clock(lands[1])}): Alert 2");
                r.Eq(g.Log.Count(l => l.Code == "camera.cut-reported"), 2, "each reported once");
                r.Is(g.CameraDeadAs("pickup") == "cut" && g.CameraDeadAs("entrance") == "cut", "both dead, cut");
            });

            Add("CAM-SHOT-PRICE", "—", "A camera shot (brief 058 §3, tuning.md §9.2): PreviewFire prices it +2 T, the shooting once, no hit or down Trail; the run charges exactly that, once, and the preview then says +0 (paid); the dead camera reads shot.", r =>
            {
                var g = r.G;
                var spot = CameraSpot(r, Scout, "entrance");
                if (!r.Is(spot.HasValue, "a spot with a line to the entrance camera")) return;
                r.To(r.SendTo(Scout, spot!.Value));
                var pv = g.PreviewFire(OpId.Scout, "camera:entrance");
                r.Eq((pv.Refused, pv.TrailShooting, pv.TrailHit, pv.TrailDown), ("", g.R.GunfireTrail, 0, 0), "the preview: +2 (the shooting), no hit, no down");
                int trail = g.Trail;
                r.Is(r.Fire(Scout, "camera:entrance"), "fire on the camera");
                r.Is(r.Until(() => g.CameraDark("entrance"), T(10)), "it goes dark");
                r.Eq(g.CameraDeadAs("entrance"), "shot", "dead as shot, not cut");
                r.Eq(g.Trail - trail, pv.TrailShooting + pv.TrailHit + pv.TrailDown, "the run charges what the preview said");
                r.Is(g.Ledger.Count(l => l.what == "trail" && l.tick > 0 && l.cause.Contains("camera:entrance")) == 1, "once");
                r.Is(!g.Ledger.Exists(l => l.what == "trail" && (l.cause.StartsWith("hit", StringComparison.Ordinal) || l.cause.StartsWith("down", StringComparison.Ordinal))), "never a hit or down Trail");
                r.Eq(g.PreviewFire(OpId.Scout, "camera:entrance").TrailShooting, 0, "the shooting is paid: the preview says +0 now");
            });

            Add("CAM-ANY", "—", "Everyone armed can shoot a camera (brief 058 §3, Sky 2026-10-24: \"everyone can shoot them\"): the Scout, the Fixer and the Tech each shoot the entrance camera dark, each in their own run.", r =>
            {
                foreach (var op in new[] { Scout, Fixer, Tech })
                {
                    var rr = op == Scout ? r : new Run(Twin(r.G, new Start())); var g = rr.G;
                    var spot = CameraSpot(rr, op, "entrance");
                    if (!r.Is(spot.HasValue, $"{op}: a spot with a line to the entrance camera")) continue;
                    rr.To(rr.SendTo(op, spot!.Value));
                    var (ok, why) = g.Issue(new Order { Tick = g.Tick, Op = op, Do = "fire", Target = "camera:entrance" });
                    r.Is(ok, $"{op} fires on the entrance camera{(ok ? "" : ": " + why)}");
                    rr.Until(() => g.CameraDark("entrance"), T(20));
                    r.Is(g.CameraDark("entrance") && g.CameraDeadAs("entrance") == "shot", $"{op}: the entrance camera is shot out ({Report.Clock(g.Tick)})");
                }
            });

            Add("NC-5", "—", "Civilians remember a runner from Alert 2 (brief 054 §3.2 and §8, Sky 2026-10-25): at Alert 1 (one cut camera reported) a civilian who sees a run in her cone keeps nothing; at Alert 2 (a second cut, no gunfire, no fight) the move card names her and she remembers the runner's face (saw-runner, a sighting): no found fault, Alert unchanged, no Trail.", r =>
            {
                var g = r.G; if (!r.Is(g.R.DisableOn && g.R.PeopleCone, "cell.disable and perception.people cone in the rulebook")) return;
                TechAtCabinet(r);
                r.Do(Tech, "disable", "pickup"); r.Done(Tech);
                r.To(g.CutReps[0].LandAt); r.Eq(g.Alert, 1, "the first cut reported: Alert 1");
                bool RunPast(string op, out string who)
                {
                    who = "";
                    var c = ACivilian(g, w => !w.P.Memory.Exists(m => m.Act == "saw-runner")); if (c == null) return false;
                    var (w, _, ahead, _) = c.Value; who = w.P.Id;
                    var q = g.QuoteMove(op, ahead);
                    bool carded = q.RunRememberedBy.Contains(who);
                    var (ok, _) = g.Issue(new Order { Tick = g.Tick, Op = op, Do = "move", Point = ahead, Run = true });
                    if (!ok) return false;
                    r.To(r.Arrival(op));
                    return carded;
                }
                int trail = g.Trail;
                bool carded1 = RunPast(Fixer, out var c1);
                r.Is(c1.Length > 0, $"a civilian ahead of the Fixer's run ({c1})");
                var p1 = g.PersonById(c1);
                r.Is(!carded1 && p1 != null && !p1.Memory.Exists(m => m.Act == "saw-runner"), "at Alert 1: not on the card, and she keeps nothing");
                r.Do(Tech, "disable", "entrance"); r.Done(Tech);
                r.To(g.CutReps[1].LandAt); r.Eq(g.Alert, 2, "the second cut reported: Alert 2, no gunfire");
                r.Is(g.Gunfire == 0 && !g.FightOn, "not a loud run: no shot, no fight");
                int alert = g.Alert, faults = g.Ledger.Count(l => l.cause.StartsWith("runner-seen", StringComparison.Ordinal));
                bool carded2 = RunPast(Scout, out var c2);
                var p2 = g.PersonById(c2);
                r.Is(c2.Length > 0 && p2 != null, $"a civilian ahead of the Scout's run ({c2})");
                r.Is(carded2, "the move card names her (RunRememberedBy)");
                r.Is(p2 != null && p2.Memory.Exists(m => m.Act == "saw-runner" && m.By == "scout"), "she remembers the Scout running (saw-runner, the face)");
                r.Is(g.Alert == alert && g.Ledger.Count(l => l.cause.StartsWith("runner-seen", StringComparison.Ordinal)) == faults, "no found fault, Alert unchanged");
                r.Eq(g.Trail - trail, g.R.DisableTrail, "no Trail but the second cut's");
            });
        }
    }
}
