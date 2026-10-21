// Everyone watches, and watchers notice loiterers (brief 031, Sky 2026-10-20; rulebook sight.watch.reach, attention). The scenarios of
// brief 031 §7: each agent's reach (inside it, and 1 m past it), behind and in front of a camera, the report at Heat 0 and at Heat 10, a
// looped camera, the drain and the wary floor, an act holds the clock, a wary guard draws, a robot's blind back, the patrol's union, and the
// Fixer's Watch on Inez. Each also runs twice and replays from its orders (the Theory tests).
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>The free ground nearest `near` that `who` can walk to and that `ok` accepts (ties by grid cell), or null.</summary>
        static Pt? FreeWhere(Run r, Pt near, Func<Pt, bool> ok, string who, int within = 4000)
        {
            var g = r.G; var grid = g.S.Grid; var from = r.Op(who).Pos;
            var cands = new List<(long d, int i, int j, Pt c)>();
            for (int i = 0; i < grid.Nx; i++)
                for (int j = 0; j < grid.Ny; j++)
                {
                    if (!grid.FreeCell(i, j)) continue;
                    var c = grid.Centre(i, j);
                    if (!G.Within(c, near, within) || !ok(c)) continue;
                    cands.Add((G.D2(c, near), i, j, c));
                }
            cands.Sort((a, b) => a.d != b.d ? a.d.CompareTo(b.d) : a.i != b.i ? a.i.CompareTo(b.i) : a.j.CompareTo(b.j));
            foreach (var x in cands) if (grid.Route(from, x.c) != null) return x.c;
            return null;
        }
        static Camera Cam(Run r, string id) => r.G.S.Cameras.First(c => c.Id == id);
        /// <summary>A spot within `cm` of the entrance camera, on its axis side (`front`: in its sight; else behind it, out of its sight).</summary>
        static Pt? CameraSpot(Run r, string who, bool front, int cm = 300)
        {
            var g = r.G; var c = Cam(r, "entrance");
            return FreeWhere(r, c.At + Along(c.Facing, front ? cm : -cm), p => G.Within(p, c.At, cm + 50) && g.InSight("camera:entrance", p, false) == front && g.S.Sight(p, c.At, g.R.Eye), who, 600);
        }
        /// <summary>The next tick a camera's watcher reads its screen at or after `tick` (rulebook attention.report.scan, phase from its id).</summary>
        static int NextScan(Game g, string w, int tick) { int period = T(g.R.AttnScanS), phase = (int)(Streams.Fnv1a32(w) % (uint)period); return tick + ((phase - tick % period) % period + period) % period; }

        /// <summary>The Enforcer idles in front of the entrance camera until it reports him; checks the report's timing and what it does.</summary>
        static void LoiterReported(Run r, int reportS)
        {
            var g = r.G; const string cam = "camera:entrance";
            var spot = CameraSpot(r, Enf, true); if (!r.Is(spot.HasValue, "ground 3 m in front of the entrance camera, in its sight")) return;
            r.To(r.SendTo(Enf, spot!.Value));
            int trail = g.Trail, alert = g.Alert;
            r.Is(r.Until(() => g.AttentionReported(cam, Enf), T(reportS + 10)), $"the camera reports the Enforcer idling in its sight");
            r.Eq(g.AttentionTicks(cam, Enf), T(reportS), $"at {reportS} s of standing still in its sight (the band's report mark)");
            var rep = g.AttentionReportsMade().Single(x => x.watcher == cam && x.op == Enf);
            r.Eq(rep.landAt, NextScan(g, cam, g.Tick) + T(g.R.AttnCallS), "it lands at the watcher's next scan + 3 s");
            r.Eq(g.AttentionStage(cam, Enf), 3, "the lens flashes until it lands (reporting)");
            r.To(rep.landAt - 1); r.Eq(g.Alert, alert, "no Alert before the report lands");
            r.To(rep.landAt);
            r.Is(g.Alert >= g.R.AttnAlert && g.Ledger.Exists(l => l.what == "alert" && l.cause == $"loiter:{cam}:{Enf}"), $"a found fault: Alert {g.Alert}, at least 1");
            r.Eq(g.Trail, trail, "no Trail");
            r.Is(g.Sightings.Exists(s => s.Watcher == cam && s.Op == Enf && s.To == g.Tick), "logged as a sighting");
            r.Is(g.Log.Exists(l => l.Tick == g.Tick && l.Code == "alarm.dispatches"), "the dial sends its Alert-1 response");
            r.Is(g.Units.Count(u => u.Robot) >= Math.Min(1, g.DialNow.A1Robots), $"robots are sent ({g.Units.Count(u => u.Robot)}; level {g.SecurityLevel}: {g.DialNow.A1Robots} at Alert 1)");
            r.Eq(g.AttentionReportsMade().Count(x => x.watcher == cam && x.op == Enf), 1, "once per operative per watcher");
        }

        static void AttentionCases()
        {
            Add("N166", "—", "Every agent watches from its own reach (brief 031 §1; rulebook sight.watch.reach): on the entrance camera, the Scout from 30 m, the Tech from 24, the Fixer from 12 and the Enforcer from 3: open just inside the ring on a clear line, too far 1 m past it (the refusal names the agent's reach).", r =>
            {
                var g = r.G; var c = Cam(r, "entrance");
                foreach (var who in new[] { Scout, Tech, Fixer, Enf })
                {
                    int reach = g.R.WatchReachCm(Sim.Op.Parse(who));
                    var inside = FreeWhere(r, c.At, p => !G.Within(p, c.At, reach - 150) && G.Within(p, c.At, reach - 50) && g.S.Sight(p, c.At, g.R.Eye), who, reach + 200);
                    var past = FreeWhere(r, c.At, p => !G.Within(p, c.At, reach + 100) && G.Within(p, c.At, reach + 150) && g.S.Sight(p, c.At, g.R.Eye), who, reach + 400);
                    if (!r.Is(inside.HasValue && past.HasValue, $"the {who}: ground just inside and 1 m past its {reach / 100} m ring, in line")) continue;
                    r.To(r.SendTo(who, past!.Value));
                    var far = g.LearnQuery(who, "watch", "camera:entrance");
                    r.Is(!far.ok && far.code == "too-far" && far.m == reach / 100, $"the {who} 1 m past {reach / 100} m: too far, within {reach / 100} m (got {far.code} {far.m})");
                    r.To(r.SendTo(who, inside!.Value));
                    r.Is(r.Until(() => g.LearnQuery(who, "watch", "camera:entrance").ok, T(20)), $"the {who} just inside {reach / 100} m: its Watch is open ({g.LearnQuery(who, "watch", "camera:entrance").code})");
                    r.Is(g.LearnQuery(who, "listen", Inez).code != "only-scout" || who != Scout, "the Scout's Listen is never only-scout");
                    if (who != Scout) r.Eq(g.LearnQuery(who, "listen", Inez).code, "only-scout", $"the {who}'s Listen is only-scout");
                }
            });

            Add("N167", "—", "Machines see by their cone only (brief 031 L2, Sky D3): the Enforcer standing still 3 m behind the entrance camera keeps it calm (no clock); 3 m in front of it, in its sight, it notices him at 8 s (Heat 0, unknown: noticed 8 s).", r =>
            {
                var g = r.G; const string cam = "camera:entrance";
                r.Eq((g.Heat, g.HeatBand, g.AttentionMarks), (0, "unknown", (8, 20, 45)), "Heat 0: the unknown band, 8/20/45 s");
                var back = CameraSpot(r, Enf, false); var front = CameraSpot(r, Enf, true);
                if (!r.Is(back.HasValue && front.HasValue, "ground 3 m behind and 3 m in front of the camera")) return;
                r.To(r.SendTo(Enf, back!.Value)); r.Wait(T(15));
                r.Eq((g.AttentionTicks(cam, Enf), g.AttentionStage(cam, Enf)), (0, 0), "behind it: no clock, calm, after 15 s standing still");
                r.To(r.SendTo(Enf, front!.Value)); int at = g.Tick;
                r.Is(r.Until(() => g.AttentionStage(cam, Enf) == 1, T(15)), "in front: the camera notices him");
                r.Eq(g.AttentionTicks(cam, Enf), T(8), "at 8 s of standing still in its sight");
                r.Is(g.Tick - at >= T(8) - 1, "and not before 8 s (the tick he stops counts)");
                r.Is(g.Log.Exists(l => l.Code == "attention.noticed" && l.Tick == g.Tick), "logged attention.noticed");
                r.Eq(g.Attention(cam), (1, Enf), "the camera's cue: noticed, toward the Enforcer");
            });

            Add("N168", "—", "A loiterer reported at Heat 0 (brief 031 §2): the Enforcer idling 45 s in front of the live entrance camera; the report lands at the watcher's next scan + 3 s: a found fault, Alert 1, no Trail, a sighting, and the dial's Alert-1 response (robots). A robot sent doesn't sense an agent 2 m behind it (L2), and does in front.", r =>
            {
                var g = r.G;
                LoiterReported(r, 45);
                // the dial's Alert-1 robot at this level is the patrol robot, sent off its sweep (rulebook response: the first robot)
                bool Up(Unit u) => u.Robot && u.OnStreet(g.Tick) && (u.St == "walk" || u.St == "hold" || u.St == "check") && g.UnitPose(u).HasValue;
                r.Is(r.Until(() => g.Units.Exists(Up), T(120)), $"a robot answers the alarm ({string.Join(",", g.Units.Select(u => u.Id + ":" + u.St))})");
                var ro = g.Units.FirstOrDefault(Up);
                if (ro == null) return;
                var (pos, f) = g.UnitPose(ro)!.Value;
                var behind = pos - Along(f, 200); var ahead = pos + Along(f, 200);
                bool Sees(Pt p) => ro.Patrol ? g.PatrolWatching(p, false) : g.InSight(ro.Id, p, false);
                r.Is(g.S.Sight(pos, behind, g.R.Eye), "a clear line to the ground 2 m behind it");
                r.Is(!Sees(behind), $"{ro.Id} doesn't see 2 m behind it (no all-round sense)");
                r.Is(!g.S.Sight(pos, ahead, g.R.Eye) || Sees(ahead), $"{ro.Id} sees 2 m ahead");
            });

            Add("N169", "—", "A looped camera keeps no clock (brief 031 §2: a live camera only): with the entrance feed looped, the Enforcer idles 60 s in front of it: no clock, no report, Alert 0.", r =>
            {
                var g = r.G; const string cam = "camera:entrance";
                r.To(r.Send(Tech, "cabinet")); r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                r.Is(g.LoopOn && g.LoopCam == "entrance", "the entrance feed is looped");
                var spot = CameraSpot(r, Enf, true); if (!r.Is(spot.HasValue, "ground in front of the camera")) return;
                r.To(r.SendTo(Enf, spot!.Value)); r.Wait(T(60));
                r.Eq((g.AttentionTicks(cam, Enf), g.AttentionReported(cam, Enf), g.Alert), (0, false, 0), "no clock, no report, Alert 0");
            });

            Add("N170", "—", "Heat 10, infamous (brief 031 §2.1): the same idle in front of the entrance camera is reported at 18 s, less than a Watch.", r =>
            {
                var g = r.G;
                r.Eq((g.Heat, g.HeatBand, g.AttentionMarks), (10, "infamous", (3, 12, 18)), "Heat 10: the infamous band, 3/12/18 s");
                LoiterReported(r, 18);
            }, new Start { Heat = 10 });

            Add("N171", "—", "The clock drains (brief 031 §2): 10 s standing in the entrance camera's sight, then 10 s walking, and it is back to 0; once wary (20 s), walking away for 30 s leaves it at the wary mark, wary for the rest of the mission.", r =>
            {
                var g = r.G; const string cam = "camera:entrance";
                var spot = CameraSpot(r, Enf, true); if (!r.Is(spot.HasValue, "ground in front of the camera")) return;
                var away = r.Stand("crew:enforcer");
                r.To(r.SendTo(Enf, spot!.Value));
                r.Is(r.Until(() => g.AttentionTicks(cam, Enf) >= T(10), T(15)), "10 s standing still in its sight");
                int c0 = g.AttentionTicks(cam, Enf);
                r.SendTo(Enf, away); r.Wait(c0);
                r.Eq(g.AttentionTicks(cam, Enf), 0, $"walking for as long as he stood ({c0} ticks) drains it to 0");
                r.To(r.SendTo(Enf, spot.Value));
                r.Is(r.Until(() => g.AttentionWary(cam, Enf), T(25)), "standing on: wary at 20 s");
                r.SendTo(Enf, away); r.Wait(T(30));
                r.Eq((g.AttentionTicks(cam, Enf), g.AttentionWary(cam, Enf)), (T(20), true), "30 s walking away: the clock stays at the wary mark, wary");
                r.Eq(g.AttentionStage(cam, Enf), 0, "and out of its sight the cue is calm (a cue shows only an agent in sight)");
            });

            Add("N172", "—", "An act holds the clock (brief 031 §2, Sky L1): the Fixer's Pay to the driver at his car, in the pickup camera's sight: the camera's clock on the Fixer never runs during the act.", r =>
            {
                var g = r.G; const string cam = "camera:pickup";
                r.To(Max(r.Send(Scout, "d:Meet driver"), r.Send(Fixer, "d:Meet driver")));
                r.Do(Scout, "watch", Driver); r.Done(Scout);
                r.Is(g.Driver.KRoutine, "the driver's routine is known (Pay needs it)");
                var a = g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "turn", Target = Driver, Approach = "pay" });
                if (!r.Is(a.ok, $"the Fixer pays the driver ({a.code})")) return;
                var fx = r.Op(Fixer); int c = g.AttentionTicks(cam, Fixer), seen = 0, rose = 0;
                while (fx.Busy != null && !g.Over) { g.Step(); if (fx.Busy == null) break; if (g.SeesNow(cam, fx.Pos)) seen++; int n = g.AttentionTicks(cam, Fixer); if (n > c) rose++; c = n; } // the tick it lands he is idle again
                r.Is(seen > 0, $"the camera sees him through the act ({seen} ticks)");
                r.Eq(rose, 0, "its clock on him never runs during the Pay");
            });

            Add("N173", "—", "A wary guard is aware at a takedown (brief 031 §2, Sky D1): the Enforcer standing 20 s within 3 m of the posted guard, out of his door cone (a person senses anyone within 3 m), makes him wary; the knockout then fails: he sees it coming and draws (Alert at least 2). A walk-in blow from the same spot with no lingering is the route G one (N46).", r =>
            {
                var g = r.G;
                r.To(r.SendTo(Enf, BesideGuard));
                r.Is(!g.GuardWatching(r.Op(Enf).Pos, false), "beside him, out of his door cone");
                r.Is(r.Until(() => g.AttentionWary(GuardR, Enf), T(25)), "within 3 m of him: the guard is wary of the Enforcer at 20 s");
                r.Takedown(GuardR, "knockout");
                r.Is(g.Log.Exists(l => l.Code == "takedown.fails"), "the knockout fails: he sees it coming");
                r.Is(g.GuardEngaged && g.Alert >= 2 && !g.GuardDown, $"he draws: Alert {g.Alert}");
            });

            Add("N174", "—", "The patrol's routine counts watched time once (brief 031 §1): the Scout and the Tech watch it at once; both land, its sight is known with 20 s toward its routine, not 40, and a Watch is still open.", r =>
            {
                var g = r.G;
                r.To(Max(r.SendTo(Scout, WatchSpot(r, "patrol", unseen: false)), r.SendTo(Tech, WatchSpot(r, "patrol", unseen: false, who: Tech))));
                r.Is(r.Until(() => Watchable(r, "patrol") && Watchable(r, "patrol", Tech), T(120)), "the patrol is within both agents' reach and in line");
                r.Do(Scout, "watch", "patrol"); r.Do(Tech, "watch", "patrol");
                r.Done(Scout); r.Done(Tech);
                var k = g.Known("patrol")!;
                r.Is(k.Sight && !k.Routine && k.WatchedS == g.R.SightWatchS, $"two Watches at once: its sight, {k.WatchedS} of {k.RoutineNeedsS} s toward its routine");
                r.Is(r.Until(() => Watchable(r, "patrol", Tech), T(120)) && g.LearnQuery(Tech, "watch", "patrol").ok, "its routine still to learn: a Watch is open");
            });

            Add("N175", "—", "The Fixer watches Inez (brief 031 §1): from within 12 m on a clear line, 20 s; he learns her routine, as the Scout would, and her Pay card still shows '?' (Listen alone reveals temperament).", r =>
            {
                var g = r.G; var post = r.Stand(g.Inez.Place);
                var spot = FreeWhere(r, post, p => G.Within(p, post, g.R.WatchReachCm(OpId.Fixer) - 200) && g.S.Sight(p, post, g.R.Eye), Fixer, 2000);
                if (!r.Is(spot.HasValue, "ground within the Fixer's reach of Inez, in line")) return;
                r.To(r.SendTo(Fixer, spot!.Value));
                r.ForHer("watch", Inez);
                r.Is(r.Until(() => g.LearnQuery(Fixer, "watch", Inez).ok, T(30)), $"the Fixer's Watch on Inez is open ({g.LearnQuery(Fixer, "watch", Inez).code})");
                r.Do(Fixer, "watch", Inez); r.Done(Fixer);
                r.Is(g.Inez.KRoutine && !g.Inez.KTemperament, "her routine is learned, not her temperament");
                r.Is(g.Log.Exists(l => l.Code == "fixer.watch.watched"), "logged as the Fixer's Watch (fixer.watch.watched)");
                r.Eq(g.Quote(g.R.Ev("turn.pay"), g.Inez).Outcome, "?", "the Pay card's outcome shows '?'");
            });
        }
    }
}
