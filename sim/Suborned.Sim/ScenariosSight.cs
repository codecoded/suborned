// Cones are the Scout's skill (Sky 2026-10-16; Glyph's review §3.4; the rulebook's `sight`): what the cell knows of each watcher, how
// a Watch teaches it, what the cards show before and after, the routine a timed Watch teaches, and the detection fill. Each scenario also
// runs twice and replays from its orders (the Theory tests). The fan's geometry is checked point by point in the tests (SightTests).
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>The free ground nearest the Scout, or `who` (that it can walk to) from which a Watch on this watcher is taken now: within
        /// sight.watch.reach less 2 m, on a clear line (or, line false, behind a solid), and (unseen) in no watcher's sight. Ties by grid
        /// cell, so the same run finds the same spot.</summary>
        static Pt WatchSpot(Run r, string watcher, bool unseen = true, bool line = true, string who = Scout) => FindWatchSpot(r, watcher, unseen, line, who) ?? throw new InvalidOperationException($"no spot to watch {watcher} from");
        static Pt? FindWatchSpot(Run r, string watcher, bool unseen, bool line, string who = Scout)
        {
            var g = r.G; var at = g.ConeFan(watcher)!.At; var grid = g.S.Grid; var from = r.Op(who).Pos;
            var cands = new List<(long d, int i, int j, Pt c)>(); int reach = g.R.WatchReachCm(r.Op(who).Id) - 200; // brief 031: the agent's own reach
            for (int i = 0; i < grid.Nx; i++)
                for (int j = 0; j < grid.Ny; j++)
                {
                    if (!grid.FreeCell(i, j)) continue;
                    var c = grid.Centre(i, j);
                    if (!G.Within(c, at, reach) || g.S.Sight(c, at, g.R.Eye) != line || (line && g.CrowdBlocks(c, at))) continue; // brief 031: people on the street block a Watch's line
                    if (unseen && g.WatcherIds().Exists(w => g.SeesNow(w, c, false))) continue;
                    cands.Add((G.D2(c, from), i, j, c));
                }
            cands.Sort((a, b) => a.d != b.d ? a.d.CompareTo(b.d) : a.i != b.i ? a.i.CompareTo(b.i) : a.j.CompareTo(b.j));
            foreach (var x in cands) if (grid.Route(from, x.c) != null) return x.c; // the nearest the Scout can walk to
            return null;
        }

        /// <summary>The Scout (or `who`) stands within sight.watch.reach of the watcher on a clear line now (what a Watch on it needs).</summary>
        static bool Watchable(Run r, string watcher, string who = Scout)
        {
            var f = r.G.ConeFan(watcher); var at = r.Op(who).Pos;
            return f != null && G.Within(at, f.At, r.G.R.WatchReachCm(r.Op(who).Id)) && r.G.S.Sight(at, f.At, r.G.R.Eye) && !r.G.CrowdBlocks(at, f.At); // brief 031: the agent's reach, a clear line
        }

        static void SightCases()
        {
            Add("N41", "—", "Cones are the Scout's skill (Sky 2026-10-16; rulebook sight): no watcher's sight is known at the start and no fan is drawn; the card's 'seen by' shows '?' for a watcher that could see the route, never its answer. Only the Scout and (Sky 2026-10-18) the Tech watch a camera, within 18 m on a clear line; the response unit is not watched. A 20 s Watch on the entrance camera teaches its sight (its routine with it: a camera is fixed), and the card then shows the true answer.", r =>
            {
                var g = r.G;
                r.Is(g.WatcherIds().TrueForAll(w => !g.Known(w)!.Sight), "no watcher's sight is known at the start");
                r.Eq(g.KnownFans().Count, 0, "no cone is drawn by default");
                var door = r.Stand("d:Office entrance");
                var q0 = g.QuoteMove(Scout, door);
                r.Is(q0.RunSeenBy.Contains("camera:entrance"), "the engine's truth: the entrance camera would see the run");
                r.Is(q0.Seen.Seen == "?" && q0.Seen.SeenBy.Count == 0 && q0.Seen.MaybeSeenBy.Contains("camera:entrance"), $"the card shows '?' (seen {q0.Seen.Seen}, maybe {string.Join(",", q0.Seen.MaybeSeenBy)})");
                r.Eq(q0.Seen.AlertIfSeen, g.R.RunAlert, "and the Alert a seen run would raise");
                var far = g.QuoteMove(Scout, r.Stand("crew:fixer"));
                r.Is(!far.Seen.MaybeSeenBy.Contains("camera:entrance"), "a route no camera could see, whatever its facing, shows no '?' for it");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "watch", Target = "camera:entrance" }).code, "too-far", "the Tech watches the machines too (Sky 2026-10-18), but not from the start: too far"); // was only-scout before Sky 2026-10-18
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "watch", Target = "camera:entrance" }).code, "too-far", "too far to watch the camera from the start");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "watch", Target = "security-1" }).code, "target-unknown", "the response unit's sight is known when it comes: nothing to watch");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "watch", Target = "camera:roof" }).code, "target-unknown", "no such camera");
                // within reach but behind a solid: the first watcher (in WatcherIds order) with such ground the Scout can walk to
                var hid = g.WatcherIds().Where(w => w.StartsWith("camera:", StringComparison.Ordinal) || w == "patrol").Select(w => (w, at: FindWatchSpot(r, w, false, false))).FirstOrDefault(x => x.at.HasValue);
                if (r.Is(hid.at.HasValue, $"there is ground within reach of a watcher but behind a solid ({hid.w})"))
                {
                    r.To(r.SendTo(Scout, hid.at!.Value));
                    r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "watch", Target = hid.w }).code, "no-clear-line", $"within reach of the {hid.w} but behind a solid: no clear line");
                }
                var spot = WatchSpot(r, "camera:entrance");
                r.To(r.SendTo(Scout, spot));
                r.Do(Scout, "watch", "camera:entrance");
                var a = g.ActivityOf(OpId.Scout);
                r.Is(a.Verb == "watch" && a.Target == "camera:entrance" && a.OnDone == "learns the sight" && a.TicksLeft == T(g.R.SightWatchS), $"the HUD reads Watch on the camera: {a.OnDone}, {a.TicksLeft} ticks");
                r.Is(!g.Known("camera:entrance")!.Sight, "nothing is learned before the Watch lands");
                r.Done(Scout);
                var k = g.Known("camera:entrance")!;
                r.Is(k.Sight && k.Routine && k.How == "watched" && k.SightAt == g.Tick && k.WatchedS == g.R.SightWatchS, $"the camera's sight is known, watched at {Report.Clock(k.SightAt)}, and its fixed routine with it");
                r.Is(g.Log.Exists(l => l.Code == "scout.watch.watched" && l.Text.Contains("entrance")), "the Scout says what was learned");
                r.Eq(string.Join(",", g.KnownFans().Select(f => f.Watcher)), "camera:entrance", "the Alt view: the one cone learned");
                var q1 = g.QuoteMove(Scout, door);
                r.Is(!q1.Seen.MaybeSeenBy.Contains("camera:entrance") && q1.Seen.SeenBy.Contains("camera:entrance") == q1.RunSeenBy.Contains("camera:entrance"), "the card now shows the camera's true answer");
                var c = g.Preview("help", Inez).Seen;
                r.Is(c != null && !c.MaybeSeenBy.Contains("camera:entrance"), "the action card's 'seen by' reads the same knowledge");
                r.Eq(g.Routine("camera:entrance", T(60))!.Count, 0, "a camera has no schedule: it does not move");
                r.Is(g.Routine("patrol", T(60)) == null, "the patrol's routine is unknown");
            });

            Add("N42", "—", "A timed Watch teaches a routine (stage 2; rulebook sight.watch.routine): one Watch of the patrol teaches its sight, the second (40 s watched) its routine. Its schedule then names each turn at an end of the sweep and each change in whether it sees the public door, and the run meets them at those ticks; a reroute moves it.", r =>
            {
                var g = r.G;
                var spot = WatchSpot(r, "patrol", unseen: false);
                r.To(r.SendTo(Scout, spot));
                r.Is(r.Until(() => Watchable(r, "patrol"), T(120)), "the patrol comes within reach and in line");
                r.Do(Scout, "watch", "patrol"); r.Done(Scout);
                var k1 = g.Known("patrol")!;
                r.Is(k1.Sight && !k1.Routine && k1.WatchedS == 20 && k1.RoutineNeedsS == 40, $"one Watch: its sight, not its routine ({k1.WatchedS} of {k1.RoutineNeedsS} s)");
                r.Is(g.Routine("patrol", T(100)) == null, "no schedule yet");
                r.Is(r.Until(() => Watchable(r, "patrol"), T(120)), "in reach and in line again");
                r.Do(Scout, "watch", "patrol");
                r.Eq(g.ActivityOf(OpId.Scout).OnDone, "learns the sight and the routine", "the HUD says the second Watch teaches the routine");
                r.Done(Scout);
                var k2 = g.Known("patrol")!;
                r.Is(k2.Routine && k2.RoutineAt == g.Tick && k2.WatchedS == 40, "two Watches: the routine is known");
                var ev = g.Routine("patrol", T(g.R.PatrolPeriodS))!;
                r.Is(ev.Count > 0 && ev[0].Tick == g.Tick && (ev[0].What == "sees-door") == g.SweepSeesEntrance(g.Tick), $"the schedule starts with where it stands now ({ev[0].What})");
                r.Is(ev.Exists(e => e.What == "turn-at-a") && ev.Exists(e => e.What == "turn-at-b"), "a whole period names both turns");
                foreach (var e in ev.Take(6))
                {
                    r.To(e.Tick);
                    if (e.What == "turn-at-a") r.Eq(g.PatrolPose()!.Value.pos, r.Stand(g.S.PatrolA), $"{Report.Clock(e.Tick)}: at the sweep's end A, as the schedule said");
                    else if (e.What == "turn-at-b") r.Eq(g.PatrolPose()!.Value.pos, r.Stand(g.S.PatrolB), $"{Report.Clock(e.Tick)}: at the sweep's end B, as the schedule said");
                    else r.Eq(g.SweepSeesEntrance(g.Tick), e.What == "sees-door", $"{Report.Clock(e.Tick)}: {e.What}, as the schedule said");
                }
                r.To(r.Send(Tech, "cabinet"));
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                var after = g.Routine("patrol", T(200))!;
                r.Is(after.Count > 0 && after[0].What == "back-on-sweep" && after[0].Tick == g.PatrolAwayUntil, "rerouted: the schedule says when it is back on its sweep");
                r.Is(!g.ConeFan("patrol")!.Live && !g.Detection().Exists(n => n.Watcher == "patrol"), "away, it watches nothing");
            });

            Add("N43", "—", "The guard is watched as the person he is (scout.watch, 6 m): the Watch teaches his routine and his sight. Detection needs no knowledge: in his cone the Scout is noticed (the fill) with '?' over him until his sight is known, and known after.", r =>
            {
                var g = r.G;
                var front = AheadOfGuard(r, 400);
                r.To(r.SendTo(Scout, front));
                var n0 = g.Detection().Find(n => n.Watcher == "guard" && n.Op == Scout);
                r.Is(n0 != null && n0.PerMille == 1000 && !n0.Known && n0.From == g.GuardPose()!.Value.pos && n0.To == r.Op(Scout).Pos, "in his cone, the guard notices the Scout: the fill, and '?' over him");
                r.Eq(g.DetectionOf("guard", Scout), 1000, "one pair's fill");
                r.Eq(g.DetectionOf("guard", Fixer), 0, "the Fixer, far off, is not noticed");
                r.Do(Scout, "watch", GuardR);
                r.Eq(g.ActivityOf(OpId.Scout).OnDone, "learns the routine and his sight", "the HUD says what the guard's Watch teaches");
                r.Done(Scout);
                var k = g.Known(GuardR)!;
                r.Is(k.Sight && k.Routine && k.How == "watched" && g.GuardP.KRoutine, "his routine (the person's) and his sight are known");
                r.Eq(g.Routine(GuardR, T(120))!.Count, 0, "at his door he holds it: no schedule");
                var n1 = g.Detection().Find(n => n.Watcher == "guard" && n.Op == Scout);
                r.Is(n1 != null && n1.Known, "now the fill is a known watcher's");
                var f = g.ConeFan(GuardR)!;
                r.Is(f.Live && f.Kind == "guard" && f.Points.Length >= 3 && G.InPoly(r.Op(Scout).Pos, f.Points), "the Scout stands inside the guard's fan");
            });

            Add("N44", "—", "Learned knowledge is the cell's, not the Scout's (Sky 2026-10-16): the Scout learns the entrance camera's sight, then goes down in front of the guard; the sight stays known and drawn. A downed Scout watches nothing.", r =>
            {
                var g = r.G;
                r.To(r.SendTo(Scout, WatchSpot(r, "camera:entrance")));
                r.Do(Scout, "watch", "camera:entrance"); r.Done(Scout);
                r.Is(g.Known("camera:entrance")!.Sight, "the camera's sight is learned");
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Scout, FrontOfGuard(r))));
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                r.Do(Scout, "draw"); g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "stance", Stance = "hold" });
                r.Is(r.Until(() => r.Op(Scout).Down, T(30)), "the Scout is down");
                var k = g.Known("camera:entrance")!;
                r.Is(k.Sight && k.How == "watched", "the camera's sight is still known");
                r.Is(g.KnownFans().Exists(f => f.Watcher == "camera:entrance"), "and still drawn on Alt");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "watch", Target = "camera:pickup" }).code, "agent-down", "a downed Scout watches nothing");
                r.Is(!g.Detection().Exists(n => n.Op == Scout), "a downed operative is not noticed");
            });

            Add("N45", "—", "The response unit and the rivals come for the cell: their sight is known from the moment they are on the street (rulebook sight.kinds learn arrival), never watched, never told. Gunfire at the guard draws both.", r =>
            {
                var g = r.G;
                r.Is(!g.Known("security-1")!.Sight && !g.Known("rivals")!.Sight, "neither is known before they come");
                r.Is(g.ConeFan("security-1") == null && g.ConeFan("rivals") == null, "nor on the street");
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, BesideDoor)));
                r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                r.Fire(Enf, GuardR);
                r.Is(r.Until(() => g.SecurityArrives != int.MaxValue / 4, T(5)), "the response unit is called");
                r.Is(!g.Known("security-1")!.Sight, "called is not come: not known yet");
                r.Is(r.Until(() => g.Tick >= g.SecurityArrives || g.Over, T(120)) && !g.Over, "it reaches the street");
                var k = g.Known("security-1")!;
                r.Is(k.Sight && k.Routine && k.How == "arrival" && k.SightAt == g.SecurityArrives, "its sight is known on arrival");
                r.Is(g.ConeFan("security-1") is Fan f && f.Kind == "security" && f.Live && f.Points.Length >= 3, "and has a fan");
                r.Is(g.KnownFans().Exists(x => x.Watcher == "security-1"), "drawn on Alt");
                r.Is(g.RivalsDrawn, "the gunfire drew the rivals");
                r.Is(r.Until(() => g.RivalsPresent || g.Over, T(120)) && g.RivalsPresent, "they come");
                var kr = g.Known("rivals")!;
                r.Is(kr.Sight && kr.How == "arrival", "their sight is known when they come");
            }, NoFight());
        }
    }
}
