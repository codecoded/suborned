// Engine 0.34.0 (brief 067, Sky playing 0.2.122): Motive's pace and punishment values and his rulings on Keel's 0.33.1 items
// (briefs/067/motive-values.json), Pulse's two notes (briefs/067/pulse.md §2.5), each shown by the rule it is.
//    PACE-SPLIT     M067-B1: the cell walks at cell.speed (1.8 m/s), the city at crowd.walk (1.4 m/s): the call's walk, the rounds file
//    RUN-NOTICED    M067-B2: a run in a camera's sight is noticed at once, wary 11.9 s and reported 36.9 s of run in sight later (Heat 0)
//    RUN-NOT-ALERT  M067-B2: route A's opening runs past the shift guards: sighted and remembered, no Alert (0.33.1: Alert 1)
//    RUN-FREE       M067-B2: at Alert 2 a run moves no clock and logs no sighting
//    K3-PRESS       M067-K3 (a): the cell all in the held car: the rivals walk to it and take Vale (rivals-reach-vale)
//    K3-CELL-DOWN   M067-K3 (b): the cell shot down by the car with Vale in it: cell-down the tick the last falls
//    K3-BACK        M067-K3 (a): an operative standing outside again sends them back to cover
//    K3-DRIVER      M067-K3 (c): Go with only the downed aboard is refused car-no-driver; a standing operative boards and Go is taken
//    POST-BESIDE    Pulse note 3: the relief who stands beside the cast guard's post keeps her check-in marks (PostOf reads the waiting point)
//    DOOR-KERB      Pulse note 4: a guard posted at an Office door faces out of it (kerb), not along the wall
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>Every operative in `ops` walks to the getaway car (unless at it) and boards (BoardVale's walk, no Go).</summary>
        static void BoardAll(Run r, params string[] ops)
        {
            var g = r.G;
            foreach (var name in ops)
            {
                if (g.Over) return;
                var o = r.Op(name); if (o.Down || o.InCar) continue;
                g.Issue(new Order { Tick = g.Tick, Op = name, Do = "cease" });
                if (!g.AtGetaway(o.Pos)) { int at = r.SendTo(name, g.GetawaySeat); while (!g.Over && g.Tick < at && !o.Down) g.Step(); }
                if (g.Over) return;
                if (o.Down) continue; // fell on the way
                r.Do(name, "board", "car"); r.Done(name); if (!g.Over) g.Step();
            }
        }
        /// <summary>Route A into the safe fight, the Enforcer boarding with Vale under the three's cover, the blocker spared (the car stays held).</summary>
        static bool RouteAAboard(Run r)
        {
            var g = r.G;
            RouteA(r, reroute: true);
            if (!r.Is(g.FightBranch == "safe", $"the safe fight ({g.FightBranch} {g.Result} {g.FailCode} at {Report.Clock(g.Tick)})")) return false;
            Support(r); ToAmbush(r);
            // the three draw the fire off him without breaking the crew (DrawFire: one shot at a rival aiming at him, then cease), so the fight stays on
            // and the car held; the weapons free setting is off, so nobody fires on their own
            foreach (var op in new[] { Scout, Fixer, Tech }) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "stance", Stance = "hold" });
            int at = r.SendTo(Enf, r.Stand("d:Getaway bay"));
            DrawFire(r, () => g.Tick >= at, 60);
            if (g.Over) { r.Is(false, $"the Enforcer walks Vale to the car ({g.Result} {g.FailCode} at {Report.Clock(g.Tick)})"); return false; }
            r.Do(Enf, "board", "car");
            DrawFire(r, () => r.Op(Enf).InCar, 10);
            if (g.Over) { r.Is(false, $"the Enforcer boards with Vale ({g.Result} {g.FailCode} at {Report.Clock(g.Tick)})"); return false; }
            return r.Is(g.Vale == ValeAt.Car && r.Op(Enf).InCar, $"Vale and the Enforcer in the car at {Report.Clock(g.Tick)} (Vale {g.Vale})");
        }

        /// <summary>A run leg the entrance camera sees end to end: from `a` (free, in its view) to the free cell 8 to 12 m off whose grid walk the
        /// camera sees at every metre (Motive's run-camera probe ran to (70, -43), a blocked cell on every site: its legs were refused).</summary>
        static Pt? CameraLeg(Run r, Pt a)
        {
            var g = r.G; if (!g.Walk.Free(a) || !g.CameraWatching("entrance", a)) return null;
            for (int d = 1000; d <= 1200; d += 100)
                for (int k = 0; k < 16; k++)
                {
                    double t = Math.PI * 2 * k / 16; var b = g.Walk.NearestFree(new Pt(a.X + (int)Math.Round(d * Math.Cos(t)), a.Y + (int)Math.Round(d * Math.Sin(t))));
                    if (!b.HasValue || !g.CameraWatching("entrance", b.Value)) continue;
                    var rt = g.Walk.Route(a, b.Value); if (rt == null) continue;
                    var p = new Path(rt); bool all = true;
                    for (long mm = 0; mm <= p.Length && all; mm += 1000) all = g.CameraWatching("entrance", p.At(mm));
                    if (all && p.Length >= 8000) return b.Value;
                }
            return null;
        }
        /// <summary>Route A into the safe fight with the three in the car first (they never draw, so the rivals have no cause on them under
        /// ai.intent.final agent), then the Enforcer walks Vale to it and boards: nobody of the cell stands outside the held car.</summary>
        static bool RouteAHidden(Run r)
        {
            var g = r.G;
            RouteA(r, reroute: true);
            if (!r.Is(g.FightBranch == "safe" || g.FightBranch == "", $"no loud fight ({g.FightBranch} {g.Result} {g.FailCode} at {Report.Clock(g.Tick)})")) return false;
            int until = g.Tick + T(120);
            while (!g.Over && g.Tick < until && !new[] { Scout, Fixer, Tech }.All(o => r.Op(o).InCar))
            {
                foreach (var op in new[] { Scout, Fixer, Tech })
                {
                    var o = r.Op(op); if (o.InCar || o.Busy != null || o.Walk != null) continue;
                    if (g.AtGetaway(o.Pos)) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "board", Target = "car" });
                    else g.Issue(new Order { Tick = g.Tick, Op = op, Do = "move", Point = g.GetawaySeat });
                }
                g.Step();
            }
            if (!r.Is(new[] { Scout, Fixer, Tech }.All(o => r.Op(o).InCar), $"the three are in the car at {Report.Clock(g.Tick)}")) return false;
            BoardVale(r);
            return r.Is(g.Vale == ValeAt.Car && r.Op(Enf).InCar && !g.Over, $"Vale and the Enforcer in the car at {Report.Clock(g.Tick)} ({g.Result} {g.FailCode})");
        }

        static void Pace067()
        {
            Add("PACE-SPLIT", "067", "Motive M067-B1 (engine 0.34.0): the cell walks at cell.speed, 1.8 m/s; everyone the engine walks itself at crowd.walk, 1.4 m/s. An operative's 18 m straight walk takes 10.0 s (14 s at the old walk), its run 5.0 s; route A's call still sends the guard walking at the city's pace, 57 s to Inez (44 s at the cell's; 0.33.1's 58 s was on the grid before brief 067 K took the square's planters, which shortened his walk by 36 cm: the same 57 s at the old cell pace and on HEAD's data); the rounds file baked at 1.4 m/s loads (rounds.pace is crowd.walk's).", r =>
            {
                var g = r.G;
                r.Eq((g.R.CellSpeed, g.R.PeopleWalk, g.R.RunSpeed), (180, 140, 360), "the paces: the cell 180, the city 140, the run 360 cm/s");
                r.Is(g.RoundsOn, "handover.rounds.json loads: its pace (140) is crowd.walk's, so no rebake");
                // an 18 m straight walk from the Scout's start: the first axis with a free straight line
                var from = r.Op(Scout).Pos; Pt? to = null;
                foreach (var d in new[] { new Pt(1800, 0), new Pt(-1800, 0), new Pt(0, 1800), new Pt(0, -1800) })
                {
                    var q = from + d; if (!g.Walk.Free(q)) continue;
                    var rt = g.Walk.Route(from, q); if (rt != null && rt.Length == 2 && new Path(rt).Length == 18000) { to = q; break; }
                }
                if (!r.Is(to.HasValue, $"an 18 m straight walk from the Scout's start ({from})")) return;
                var mq = g.QuoteMove(Scout, to!.Value);
                r.Eq((mq.WalkS, mq.RunS), (10, 5), "the chip: 18 m is a 10 s walk and a 5 s run");
                int t0 = g.Tick, arrive = r.SendTo(Scout, to.Value);
                r.Eq(arrive - t0, T(10), "she arrives 10.0 s later (18 m at 1.8 m/s)");
                r.To(arrive); r.Eq(r.Op(Scout).Pos, to.Value, "and stands there");
                // the call: the summoned guard walks at the city's pace
                r.To(Max(r.Send(Scout, "d:Courtyard"), r.Send(Tech, "cabinet")));
                r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "listen", Inez);
                r.Send(Fixer, "d:Courtyard"); r.Done(Scout); HelpUntilTurned(r, Inez);
                r.To(Math.Max(g.Tick, T(228)));
                r.Do(Fixer, "call", Inez);
                var call = g.Log.FindLast(l => l.Code == "call.waitress");
                r.Is(call.Code == "call.waitress" && call.Text.Contains("walks over to her (57 s)"), $"the guard walks over to her in 57 s, the city's pace (at the cell's 1.8 m/s it would be 44 s): \"{call.Text}\"");
            });

            // Motive's run-camera script (briefs/067/motive/plans/run-camera.script.json): the Scout runs 16 legs of 10 m across the entrance camera's view
            Add("RUN-NOTICED", "067", "Motive M067-B2 (attention.run, engine 0.34.0): the Scout runs back and forth across the entrance camera's view. At the run's first tick in its sight the camera notices her (its clock jumps to the notice mark), the sighting is logged (run.seen) and Alert stays 0; each tick of run in sight counts as standing still, so at Heat 0 it is wary 11.9 s later and reports her 36.9 s later; the report lands at its watcher's next scan as a loiterer's: Alert 1.", r =>
            {
                var g = r.G; var a = new Pt(6000, -4500);
                var bq = CameraLeg(r, a); if (!r.Is(bq.HasValue, $"a run leg the entrance camera sees end to end from {a}")) return; var b = bq!.Value;
                int legs = 0;
                r.To(r.SendTo(Scout, a, run: true));
                for (int k = 0; k < 30 && !g.Over && LogTick(g, "attention.reporting") < 0; k++) { r.To(r.SendTo(Scout, k % 2 == 0 ? b : a, run: true)); legs++; }
                r.Is(legs >= 8 && !g.Log.Exists(l => l.Code == "order.rejected"), $"she ran {legs} legs between {a} and {b}, every one taken, until the report");
                r.To(r.SendTo(Scout, new Pt(4600, -5600)));
                r.Until(() => LogTick(g, "attention.landed") >= 0 || g.Over, T(60));
                int seen = LogTick(g, "run.seen"), noticed = LogTick(g, "attention.noticed"), wary = LogTick(g, "attention.wary"), reporting = LogTick(g, "attention.reporting"), landed = LogTick(g, "attention.landed");
                var marks = g.AttentionMarks;
                r.Is(seen >= 0 && noticed == seen && g.Log.Find(l => l.Code == "attention.noticed").Text.Contains("running"), $"the camera notices her running on the run's first tick in its sight ({Report.Clock(noticed)}), the tick the sighting is logged ({Report.Clock(seen)})");
                r.Is(!g.Ledger.Exists(l => l.cause.StartsWith("runner-seen:", StringComparison.Ordinal)), "no found fault from the run (cell.runAlert is retired)");
                r.Eq(wary - noticed, T(marks.wary - marks.notice) - 1, $"wary {Report.Clock(wary)}: (wary - notice) s of run in sight after it, less the jump's tick (Heat 0: {marks.wary - marks.notice} s)");
                r.Eq(reporting - noticed, T(marks.report - marks.notice) - 1, $"the report made at {Report.Clock(reporting)}: (report - notice) s after it, less the jump's tick (Heat 0: {marks.report - marks.notice} s)");
                r.Is(landed > reporting && g.Ledger.Exists(l => l.cause == "loiter:camera:entrance:scout"), $"it lands at {Report.Clock(landed)} as a loiterer's report (loiter:camera:entrance:scout)");
                r.Eq(g.Alert, 1, "Alert 1: the report, not the run");
                r.Is(g.Log.Where(l => l.Code == "attention.noticed" || l.Code == "attention.wary" || l.Code == "attention.reporting").All(l => l.Tick <= reporting), "every stage while she runs in its sight");
            });

            Add("RUN-NOT-ALERT", "067", "Motive M067-B2: route A's opening (the Scout runs to Inez, the Tech to the cabinet, the Fixer to the courtyard) runs past the Office's shift guards. Engine 0.34.0: they remember the runners and the sighting is logged (run.seen), but a run is no found fault: Alert 0 (0.33.1: Alert 1 at 0:02.8).", r =>
            {
                var g = r.G;
                g.Issue(new Order { Tick = 0, Op = Scout, Do = "move", To = "beside-inez", Run = true });
                g.Issue(new Order { Tick = 0, Op = Tech, Do = "move", To = "cabinet", Run = true });
                g.Issue(new Order { Tick = 0, Op = Fixer, Do = "move", To = "d:Courtyard", Run = true });
                g.Issue(new Order { Tick = 0, Op = Enf, Do = "move", Point = new Pt(5650, -4750) });
                r.To(T(30));
                int seen = g.Log.Count(l => l.Code == "run.seen");
                r.Is(seen >= 1, $"the runs are seen: {seen} sighting(s) logged ({string.Join(" | ", g.Log.Where(l => l.Code == "run.seen").Select(l => Report.Clock(l.Tick) + " " + l.Text))})");
                r.Is(SawRunner(g) > 0, $"security staff remember the runners ({SawRunner(g)})");
                r.Eq(g.Alert, 0, "no Alert: seen and suspicious, never an Alert on its own");
                r.Is(!g.Ledger.Exists(l => l.cause.StartsWith("runner-seen:", StringComparison.Ordinal)), "no found fault");
            });

            Add("RUN-FREE", "067", "Motive M067-B2: from Alert 2 security is free of a run (cell.run's free case, as built): the Enforcer draws where the entrance camera sees it (Alert 2, no shot, no fight); then the Scout runs across the camera's view: no clock moves for her, no sighting is logged.", r =>
            {
                var g = r.G; var a = new Pt(6000, -4500);
                var bq = CameraLeg(r, a); if (!r.Is(bq.HasValue, $"a run leg the entrance camera sees end to end from {a}")) return; var b = bq!.Value;
                r.To(r.SendTo(Enf, a));
                r.Do(Enf, "draw"); r.Until(() => g.Alert >= 2, T(10));
                if (!r.Is(g.Alert == 2 && g.Gunfire == 0 && !g.FightOn, $"Alert {g.Alert} from the drawn weapon, no gunfire, no fight")) return;
                int c0 = g.AttentionTicks("camera:entrance", Scout), seen0 = g.Log.Count(l => l.Code == "run.seen"), most = c0;
                for (int k = 0; k < 5 && !g.Over; k++)
                {
                    int at = r.SendTo(Scout, k % 2 == 0 ? b : g.Walk.NearestFree(a + new Pt(0, 300)) ?? a, run: true);
                    while (!g.Over && g.Tick < at) { g.Step(); most = Math.Max(most, g.AttentionTicks("camera:entrance", Scout)); }
                }
                r.Is(most <= c0 + 1, $"the camera's clock on her never jumps while she runs (at most {most} ticks from {c0}: the tick she stands at each turn is a loiterer's, drained on the next leg)");
                r.Eq(g.Log.Count(l => l.Code == "run.seen"), seen0, "no sighting logged");
                r.Is(!g.Log.Exists(l => l.Code == "attention.noticed" && l.Text.Contains("running")), "nobody noticed her running");
            }, new Start { Crowd = false });

            Add("K3-PRESS", "067", "Motive M067-K3 (a) (fight.unguarded, engine 0.34.0): route A into the safe fight, the three drawing the rivals' fire off the Enforcer (one shot each at a rival aiming at him, never breaking the crew, so the car stays held) while he walks Vale in and boards; then the three make for the car together. The Scout falls on the way; the Tech and the Fixer get in. With nobody of the cell standing outside, the rivals up and not broken (the blocker keeps his lane) walk to the car firing as they walk, and the first at its body takes Vale: hiding in a held car loses (rivals-reach-vale). 0.33.1 held the car to the close.", r =>
            {
                var g = r.G;
                if (!RouteAAboard(r)) return;
                foreach (var op in new[] { Scout, Fixer, Tech }) { var o = r.Op(op); if (!o.Down && !o.InCar) { g.Issue(new Order { Tick = g.Tick, Op = op, Do = "cease" }); r.SendTo(op, g.GetawaySeat); } }
                int until = g.Tick + T(60), outside = -1;
                while (!g.Over && g.Tick < until)
                {
                    foreach (var op in new[] { Scout, Fixer, Tech }) { var o = r.Op(op); if (!o.Down && !o.InCar && o.Busy == null && o.Walk == null && g.AtGetaway(o.Pos)) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "board", Target = "car" }); }
                    g.Step();
                    if (outside < 0 && Array.TrueForAll(g.Ops, o => o.Down || o.InCar)) outside = g.Tick;
                }
                int walk = LogTick(g, "rivals.for-car");
                r.Is(outside >= 0 && walk == outside + 1, $"the tick after nobody of the cell stands outside ({Report.Clock(outside)}), the rivals go for the car ({Report.Clock(walk)}: read as the tick starts)");
                r.Is(!g.Log.Exists(l => l.Code == "rivals.for-car" && g.Hostiles.Exists(h => h.Blocker && l.Text.StartsWith(h.Id + " ", StringComparison.Ordinal))), "the blocker keeps his lane");
                r.Eq(g.FailCode, "rivals-reach-vale", $"the first at the car takes Vale: hiding in a held car loses ({g.Result} {g.FailCode} at {Report.Clock(g.Tick)})");
                r.Is(g.Cause != null && g.Cause.Culprits.Count > 0 && g.Cause.Culprits[0].Kind == "rival" && g.Cause.Culprits[0].Role == "reached", $"the fail camera on the rival who reached the car ({g.Cause?.Culprits.FirstOrDefault()})");
            }, new Start { WeaponsFree = false });

            Add("K3-CELL-DOWN", "067", "Motive M067-K3 (b): as K3-PRESS until the Enforcer is in with Vale; then he steps out again and the four stand in the open by the car, each having fired once (a cause for the rivals under ai.intent.final agent), holding their fire. Someone of the cell always stands outside, so the rivals keep their cover (no walk to the car); they shoot the cell down one by one, and the tick the last operative goes down the mission fails at once (cell-down: the whole cell is down), the fail camera on that shot. 0.33.1 ran on with the whole cell down to the close.", r =>
            {
                var g = r.G;
                if (!RouteAAboard(r)) return;
                var (ok, why) = g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "move", Point = g.Walk.NearestFree(g.GetawaySeat + new Pt(0, 300)) ?? g.GetawaySeat });
                if (!r.Is(ok, $"the Enforcer steps out, Vale stays in ({why})")) return;
                r.Until(() => !r.Op(Enf).InCar || g.Over, T(5));
                foreach (var op in new[] { Scout, Fixer, Tech, Enf })
                {
                    g.Issue(new Order { Tick = g.Tick, Op = op, Do = "stance", Stance = "hold" });
                    var o = r.Op(op); if (o.Down || o.Gun.W == null) continue;
                    var t = g.Hostiles.Where(h => h.Present && !h.Down && h.Gun.St != "break" && h.Gun.St != "gone" && G.Within(h.Pos(g.Tick), o.Pos, o.Gun.W.MaxCm) && g.S.Sight(o.Pos, h.Pos(g.Tick), g.R.Eye)).OrderBy(h => G.D2(h.Pos(g.Tick), o.Pos)).FirstOrDefault();
                    if (t != null) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "fire", Target = t.Id });
                }
                int shotAt = g.Tick, seen = g.Log.Count;
                while (!g.Over && g.Tick < shotAt + T(90))
                {
                    for (; seen < g.Log.Count; seen++) { var l = g.Log[seen]; if (l.Code != "shot.hit" && l.Code != "shot.miss") continue; foreach (var op in new[] { Scout, Fixer, Tech, Enf }) if (l.Text.StartsWith(op + " fires on ", StringComparison.Ordinal)) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "cease" }); }
                    g.Step();
                }
                int walk = LogTick(g, "rivals.for-car", shotAt), last = -1;
                foreach (var op in new[] { Scout, Fixer, Tech, Enf }) last = Math.Max(last, LogTick(g, op + ".down"));
                r.Is(walk < 0, $"someone of the cell stood outside until the last fell: the rivals never walked to the car ({(walk < 0 ? "-" : Report.Clock(walk))})");
                r.Eq(g.FailCode, "cell-down", $"the whole cell down fails the mission ({g.Result} {g.FailCode} at {Report.Clock(g.Tick)})");
                r.Eq(g.Tick, last, $"at once: the tick the last operative went down ({Report.Clock(last)})");
                r.Is(g.Cause != null && g.Cause.Why == "cell-down" && g.Cause.Culprits.Count >= 2 && g.Cause.Culprits[0].Kind == "rival" && g.Cause.Culprits[1].Kind == "op", $"the fail camera: the shooter, then the last to fall ({string.Join(" ", g.Cause?.Culprits.Select(c => c.ToString()) ?? new string[0])})");
            }, new Start { WeaponsFree = false });

            Add("K3-BACK", "067", "Motive M067-K3 (a): as K3-PRESS until the rivals go for the car; then the Scout steps out of it (she never drew: no foe of theirs). An operative standing outside again sends them back to cover (rivals.back-to-cover); none of them takes Vale while she stands.", r =>
            {
                var g = r.G;
                if (!RouteAHidden(r)) return;
                r.Until(() => g.RivalsForCar.Count > 0 || g.Over, T(2));
                if (!r.Is(g.RivalsForCar.Count > 0, "the rivals go for the car")) return;
                var going = g.RivalsForCar.ToList(); int press = g.Tick;
                var (ok, why) = g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "move", Point = g.Walk.NearestFree(g.GetawaySeat + new Pt(0, 300)) ?? g.GetawaySeat });
                r.Is(ok, $"the Scout steps out ({why})");
                r.Until(() => !r.Op(Scout).InCar || g.Over, T(5));
                r.Until(() => g.RivalsForCar.Count == 0 || g.Over, T(2));
                r.Is(!g.Over && g.RivalsForCar.Count == 0 && LogTick(g, "rivals.back-to-cover", press) >= 0, $"with her outside they go back to cover ({Report.Clock(LogTick(g, "rivals.back-to-cover", press))}): {string.Join(", ", going)}");
                int fail = g.Tick; r.Until(() => g.Over || r.Op(Scout).Down, T(5));
                r.Is(g.FailCode != "rivals-reach-vale" || r.Op(Scout).Down, $"nobody takes Vale while she stands ({g.Result} {g.FailCode} at {Report.Clock(g.Tick)})");
            }, new Start { WeaponsFree = false });

            Add("K3-DRIVER", "067", "Motive M067-K3 (c): Go needs a driver. The guard shoots the Scout down (she drew in front of him, weapons free off, so she never fires); the Enforcer carries her into the getaway car and steps out: with only the downed aboard, Go is refused car-no-driver (the Go card says so too); he boards again and Go is taken (the cell withdraws by car, Vale not aboard).", r =>
            {
                var g = r.G;
                r.To(r.SendTo(Scout, FrontOfGuard(r)));
                r.Do(Scout, "draw");
                r.Until(() => r.Op(Scout).Down || g.Over, T(40));
                if (!r.Is(r.Op(Scout).Down && !g.Over, $"the guard shoots the drawn Scout down ({(r.Op(Scout).Down ? "down" : "up")} at {Report.Clock(g.Tick)})")) return;
                r.To(r.SendTo(Enf, g.Walk.NearestFree(r.Op(Scout).Pos) ?? r.Op(Scout).Pos));
                r.Body(Enf, "carry", Scout); r.Done(Enf);
                if (!r.Is(r.Op(Enf).Carrying == OpId.Scout, "the Enforcer carries her")) return;
                BoardAll(r, Enf);
                if (!r.Is(r.Op(Scout).InCar && r.Op(Enf).InCar, $"both in the car ({Report.Clock(g.Tick)})")) return;
                var (ok, why) = g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "move", Point = g.Walk.NearestFree(g.GetawaySeat + new Pt(0, 300)) ?? g.GetawaySeat });
                r.Is(ok, $"the Enforcer steps out ({why})");
                r.Until(() => !r.Op(Enf).InCar || g.Over, T(5));
                r.Is(r.Op(Scout).InCar && r.Op(Scout).Down && !r.Op(Enf).InCar, "only the downed Scout is in the car");
                var q = g.GoQuery(); r.Eq(q.code, "car-no-driver", "the Go card: nobody aboard can drive");
                var go = g.Issue(Go(g)); r.Eq(go.code, "car-no-driver", "Go is refused car-no-driver");
                r.Is(!g.Over, "and the mission goes on");
                BoardAll(r, Enf);
                var go2 = g.Issue(Go(g));
                r.Is(go2.ok, $"with the Enforcer aboard Go is taken ({go2.code})");
                r.Eq(g.Result, Outcome.Withdrawn, "the cell withdraws by car (Vale was not aboard)");
            }, new Start { WeaponsFree = false });

            Add("POST-BESIDE", "067", "Pulse's note 3 (brief 067 L; GDD §05: every staff post checks in every 90 s from its shift start): guard 73 stands her shift at office-entrance-stand-00, beside the public door the cast guard holds through the mission; the waiting point is her post, so her marks run from 5:30 every 90 s, as when she stood on the door (engine 0.34.0: PostOf reads the people file's beside). Guard 74's minute at official-entrance-stand-00 before her shift is her walk's last, not her shift: her first mark is still 5:30, on the door.", r =>
            {
                var g = r.G;
                r.To(1); // one tick: a run that never steps replays to the close
                var p73 = g.PersonById("p-records-office-73"); var p74 = g.PersonById("p-records-office-74");
                if (!r.Is(p73 != null && p74 != null, "guards 73 and 74 are on the sheet")) return;
                r.Eq(g.NextMarkOf(p73!, T(300)), T(330), "73's first mark: 5:30, her shift's start at her waiting point");
                r.Eq(g.NextMarkOf(p73!, T(331)), T(420), "then every 90 s: 7:00");
                r.Eq(g.NextMarkOf(p74!, T(260)), T(330), "74's first mark: 5:30 on the official door (her 4:30 minute at the point is her walk's, not her shift)");
            });

            Add("DOOR-KERB", "067", "Pulse's note 4 (GDD §05: a stop's facing is out of a door or toward the kerb): a guard posted at the Office's official entrance faces out of it, south toward Apron Road (the people file's kerb), not along the wall (0.33.1's door rule read the entrance's stand, 25 cm off along the face).", r =>
            {
                var g = r.G;
                r.To(T(60));
                var c = g.PersonCone("p-records-office-29"); var f = g.FacingNow("p-records-office-29");
                r.Is(c.At == g.S.Grid.NearestFree(c.At) || c.At.X > 0, $"guard 29 stands at the official entrance ({c.At})");
                r.Is(f.HasValue && f.Value.X > 0 && Math.Abs(f.Value.Y) * 4 < f.Value.X, $"he faces south, out of the door: {f}");
            });
        }
    }
}
