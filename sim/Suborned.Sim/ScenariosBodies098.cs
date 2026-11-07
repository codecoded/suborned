// Brief 098, law L9 (Bodies; engine 0.46.0): the scenarios the law moved, rewritten as the law reads them (the brief: "a scenario that tests a
// moment the law now moves: rewrite it as a law test if its rule still matters, retire it if the law replaces it, keep it if it still holds").
// Each says what it was and what changed. The law's own tests are sim/Suborned.Sim.Tests/Bodies.cs.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static void Bodies098()
        {
            Add("N12", "—", "People step their routines (§04, brief 010): at each tick everyone stands at the stop the people file gives, or walks between two stops, or is off the street. Rewritten for brief 098, law L9 (Bodies): at a stop a person stands on its place, or beside it where the law lays them when another holds it (a queue, an arc, a guard's crew spot: within that spread), and no two people on their feet stand inside two radii.", r =>
            {
                var g = r.G;
                r.Is(g.Walkers.Count > 100, $"the civic block's people are in ({g.Walkers.Count})");
                bool atStops = true, apart = true; int walking = 0, onStreet0 = 0, spread = Math.Max(5 * g.R.BodyQueueCm, 2 * g.R.BodyCrewCm); // L9: a queue of five, or a guard's second crew ring
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
                            if (t >= w.From[k] && t < w.To[k]) { inStop = true; if (w.Out[k] ? !(at.HasValue && G.Within(at.Value, g.People2!.Places[w.Place[k]], spread)) : at != null) atStops = false; }
                        if (!inStop && at.HasValue) { walking++; mover ??= w; if (!movers.Contains(w)) movers.Add(w); }
                        if (t == 0 && at.HasValue) onStreet0++;
                    }
                    foreach (var (a, b, _) in BodyCheck.Closer(BodyCheck.Of(g), g.R.BodyMinCm)) if (!(a.Tenet && b.Tenet)) apart = false;
                }
                r.Is(atStops, "everyone at a stop stands at its place or beside it, within the law's spread, or is off the street (at home, inside, beyond)");
                r.Is(apart, "no two on their feet stand inside two radii (L9)");
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

            Add("N49", "—", "A takedown seen by a camera: guard 57 knocked out under the pickup camera as he crosses the official entrance at 6:31 (brief 055: the official entrance pier hides guard 29's post from it). The camera's watcher reads the screen at its next scan (every 20 s, phase from the camera's id) and calls it in 3 s later: Alert 3. Rewritten for brief 098, law L9 (Bodies): the Enforcer waits where the timetable has guard 57 cross, and takes him when he is in the camera's view and in reach, as the law lays him (the shift change's press can hold him a moment), not at the timetable's tick.", r =>
            {
                var g = r.G;
                const string Victim = "p-records-office-57";
                var w = WalkerById(r, Victim);
                int t = Enumerable.Range(T(380), T(30)).FirstOrDefault(x => g.CrowdPos(w, x) is Pt p && g.CameraWatching("pickup", p, false));
                if (!r.Is(t > 0, "guard 57 crosses the pickup camera's view between 6:20 and 6:50")) return;
                int when = t + T(4);
                var at = g.CrowdPos(w, when) ?? throw new InvalidOperationException("guard 57 is not on the street then");
                var spot = Beside(r, at) ?? throw new InvalidOperationException("no ground beside guard 57");
                r.To(when - T(75)); r.SendTo(Enf, spot); r.Until(() => r.Op(Enf).Walk == null, T(75)); r.To(Math.Max(g.Tick, when - T(4))); // he walks in late, so the camera has no time to mark him loitering
                r.Is(g.Alert == 0, "no Alert before the takedown");
                bool inView() => g.CrowdPos(w) is Pt p && g.CameraWatching("pickup", p, false);
                bool inReach() => g.CrowdPos(w) is Pt p && G.Within(p, r.Op(Enf).Pos, g.R.TakedownReachCm - 10);
                r.Is(r.Until(() => inView() && inReach(), T(20)) || r.CloseOn(Victim, T(20)), $"guard 57 walks into reach in the camera's view ({Report.Clock(g.Tick)})"); // the law lays him round the Enforcer as he walks by (engine 0.48.0: the case takes him when both hold, its rule, not at the first tick he is in reach)
                r.Is(inView(), "guard 57 is in the pickup camera's view as he is taken");
                r.Takedown(Victim, "knockout");
                var a = ActOn(r, Victim);
                var cam = g.Reps.FirstOrDefault(x => x.Act == a.Id && x.By == "camera:pickup");
                if (!r.Is(cam != null, "the pickup camera records it")) return;
                int period = T(g.R.CameraScanS), phase = (int)(Streams.Fnv1a32("camera:pickup") % (uint)period);
                r.Is((cam!.LandAt - T(g.R.PostCallS) - phase) % period == 0 && cam.LandAt - T(g.R.PostCallS) >= a.Tick && cam.LandAt - T(g.R.PostCallS) < a.Tick + period, "its watcher reads the screen at the next scan, then calls it in");
                r.To(cam.LandAt);
                r.Eq(g.Alert, 3, "the camera's report lands: Alert 3");
            }, new Start { ValeWaits = true }); // brief 059: Vale waits at his car past 6:13 (test only)

            // Changed 2026-10-19 (the public door on the Office's west face): guard 74's way from the café to the official entrance no longer
            // crosses the door guard's cone (it runs east along the square and through the service yard, 22 m from the door at the nearest).
            // Guard 73 walks up the forecourt to the public door for her 5:30 shift and enters his cone at 5:23: she is the one taken.
            Add("N58", "—", "Who can be taken (Sky 2026-10-16: anyone; one rule, no named list): only the Enforcer; within his reach; anyone on the street, cast or crowd, by role or id; Vale only killed (knockout: use Take), and killing him fails the mission; the patrol and cameras are no targets (Reroute and Loop are their verbs). Brief 098, law L9 (Bodies): the Enforcer's walk to Vale waits for his body (it meets the shift change's file in the passage between the official car and the Office), so the case waits for him to arrive, not for the timetable's tick.", r =>
            {
                var g = r.G;
                var x = new Order { Tick = g.Tick, Op = Fixer, Do = "takedown", Target = "p-cafe-14", How = "kill" }; // Inez, standing the café door (on the street all mission)
                r.Eq(g.Issue(x).code, "only-enforcer", "only the Enforcer");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "takedown", Target = "patrol", How = "kill" }).code, "target-unknown", "the patrol is no target");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "takedown", Target = "p-cafe-14", How = "stun" }).code, "takedown-how", "a knockout or a kill");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "takedown", Target = "p-cafe-14", How = "kill" }).code, "too-far", "within reach");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "takedown", Target = "councillor", How = "kill" }).code, "person-away", "Vale is inside");
                r.To(T(g.ClockValeAtCar)); r.SendTo(Enf, Beside(r, g.ValePos()!.Value)!.Value); r.Until(() => r.Op(Enf).Walk == null, T(120)); r.CloseOn("councillor", T(30));
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "takedown", Target = "councillor", How = "knockout" }).code, "takedown-vale", "Vale is taken, not knocked out");
                r.Takedown("councillor", "kill"); r.Done(Enf);
                r.Eq((g.Result, g.FailCode), (Outcome.Failure, "vale-dead"), "Vale killed: the mission fails");
                var fc = CauseIs(r, "vale-dead", "killed", true, Enf, "op", Enf); // the fail camera (ScenariosFailCause.cs): the Enforcer who killed him, then Vale where he lies
                r.Is(fc != null && fc.Culprits.Count == 2 && fc.Culprits[1].Kind == "vale" && fc.Culprits[1].Role == "victim" && fc.Culprits[1].At == g.BodyOf(g.ValeP)!.At, "then Vale, where he lies");
            }, new Start { ValeWaits = true }); // brief 059: Vale waits at his car past 6:13 (test only)

        }
    }
}
