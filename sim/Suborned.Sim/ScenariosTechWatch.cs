// The Tech watches the machines too (Sky 2026-10-18: "I think tech should be able to watch camera / robots like scout can"; rulebook
// sight.kinds.*.by). A Watch on a camera or the patrol is open to the Scout and the Tech, with the same reach, time and teaching; what either
// learns is the cell's, and the patrol's watched seconds add up across both. A person's Watch (the guard, Inez, the driver) and every Listen
// stay the Scout's (only-scout). The Tech doesn't Watch while his loop holds (tethered to the cabinet: run-tethered). The query before the
// click (LearnQuery, for any agent) answers as Issue does. Each scenario also runs twice and replays from its orders (the Theory tests).
using System;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>A refused Watch or Listen costs nothing: no time, cash, Trail or Alert, the agent stays free, and LearnQuery said the
        /// same code before it was given.</summary>
        static void RefusedFreeBy(Run r, string who, string verb, string target, string code)
        {
            var g = r.G; var o = r.Op(who);
            var (tick, cash, trail, alert) = (g.Tick, g.Cash, g.Trail, g.Alert);
            r.Eq(g.LearnQuery(who, verb, target).code, code, $"before the click, the {who}'s {verb} {target} reads {code} (LearnQuery)");
            var a = g.Issue(new Order { Tick = g.Tick, Op = who, Do = verb, Target = target });
            r.Is(!a.ok && a.code == code, $"the {who}'s {verb} {target} is refused {code} (got {(a.ok ? "taken" : a.code)})");
            r.Is(o.Busy == null && g.Tick == tick && g.Cash == cash && g.Trail == trail && g.Alert == alert, $"the refusal costs nothing: no time, cash, Trail or Alert, the {who} free");
        }

        static void TechWatchCases()
        {
            Add("N107", "—", "The Tech watches the entrance camera (Sky 2026-10-18; rulebook sight.kinds.camera.by): from 18 m on a clear line, the same 20 s Watch the Scout's is, and its sight is learned (its fixed routine with it), the cell's knowledge: drawn on Alt, and a Watch by the Scout is now refused already-watched too. The Tech's repeat Watch is refused already-watched, free. ScoutQuery and LearnQuery for the Scout agree.", r =>
            {
                var g = r.G;
                r.Is(!g.Known("camera:entrance")!.Sight, "the camera's sight is unknown at the start");
                r.Eq(g.LearnQuery(Tech, "watch", "camera:entrance").code, "too-far", "from the start, the Tech is too far (not only-scout)");
                r.To(r.SendTo(Tech, WatchSpot(r, "camera:entrance", who: Tech)));
                r.Is(g.LearnQuery(Tech, "watch", "camera:entrance").ok, "within reach and in line, the Tech's Watch is open (LearnQuery)");
                r.Do(Tech, "watch", "camera:entrance");
                var a = g.ActivityOf(OpId.Tech);
                r.Is(a.Verb == "watch" && a.Target == "camera:entrance" && a.OnDone == "learns the sight" && a.TicksLeft == T(g.R.SightWatchS), $"the HUD reads the Tech's Watch: {a.OnDone}, {a.TicksLeft} ticks");
                r.Is(!g.Known("camera:entrance")!.Sight, "nothing is learned before the Watch lands");
                r.Done(Tech);
                var k = g.Known("camera:entrance")!;
                r.Is(k.Sight && k.Routine && k.How == "watched" && k.SightAt == g.Tick && k.WatchedS == g.R.SightWatchS, "the camera's sight is known, watched, and its fixed routine with it");
                r.Is(g.Log.Exists(l => l.Code == "tech.watch.watched" && l.Text.Contains("Tech") && l.Text.Contains("entrance")), "the Tech says what was learned");
                r.Eq(string.Join(",", g.KnownFans().Select(f => f.Watcher)), "camera:entrance", "the Alt view: the one cone learned");
                RefusedFreeBy(r, Tech, "watch", "camera:entrance", "already-watched");
                r.Eq(g.ScoutQuery("watch", "camera:entrance").code, "already-watched", "the cell knows it: the Scout's Watch is refused for knowledge too (ScoutQuery)");
                r.Eq(g.LearnQuery(Scout, "watch", "camera:entrance").code, "already-watched", "LearnQuery for the Scout says the same");
                r.Is(g.LearnQuery(Tech, "watch", "camera:pickup").code != "already-watched", "the pickup camera, never watched, is not refused for knowledge");
            });

            Add("N108", "—", "The Scout and the Tech together reach the patrol's routine (Sky 2026-10-18; rulebook sight.watch.routine 40 s): the Scout's Watch teaches its sight (20 of 40 s); the Tech's second Watch is open, since its routine is still to learn, and lands it: the cell's seconds add up across both. Then neither may Watch it again (already-watched).", r =>
            {
                var g = r.G;
                r.To(r.SendTo(Scout, WatchSpot(r, "patrol", unseen: false)));
                r.Is(r.Until(() => Watchable(r, "patrol"), T(120)), "the patrol comes within the Scout's reach and in line");
                r.Do(Scout, "watch", "patrol"); r.Done(Scout);
                var k1 = g.Known("patrol")!;
                r.Is(k1.Sight && !k1.Routine && k1.WatchedS == g.R.SightWatchS, $"the Scout's Watch: its sight, not its routine ({k1.WatchedS} of {k1.RoutineNeedsS} s)");
                r.To(r.SendTo(Tech, WatchSpot(r, "patrol", unseen: false, who: Tech)));
                r.Is(r.Until(() => Watchable(r, "patrol", Tech), T(120)), "the patrol comes within the Tech's reach and in line");
                r.Is(g.LearnQuery(Tech, "watch", "patrol").ok, "its routine still to learn: the Tech's Watch is open");
                r.Do(Tech, "watch", "patrol");
                r.Eq(g.ActivityOf(OpId.Tech).OnDone, "learns the sight and the routine", "the HUD says the Tech's Watch teaches the routine");
                r.Done(Tech);
                var k2 = g.Known("patrol")!;
                r.Is(k2.Routine && k2.RoutineAt == g.Tick && k2.WatchedS == 2 * g.R.SightWatchS, $"the Scout's 20 s and the Tech's 20 s: its routine is known ({k2.WatchedS} s)");
                r.Is(g.Routine("patrol", T(g.R.PatrolPeriodS)) is var ev && ev != null && ev.Exists(e => e.What == "turn-at-a"), "and its schedule is read");
                r.Is(r.Until(() => Watchable(r, "patrol", Tech), T(120)), "in the Tech's reach and in line again");
                RefusedFreeBy(r, Tech, "watch", "patrol", "already-watched");
                r.Eq(g.LearnQuery(Scout, "watch", "patrol").code, "already-watched", "the Scout's Watch is refused for knowledge too");
            });

            Add("N109", "—", "People stay the Scout's (Sky 2026-10-18): the Tech's Watch on the guard, Inez or the driver, and his Listen, are refused only-scout, free, even standing beside the guard; the response unit is watched by no one (target-unknown: its sight is known when it comes); the Fixer and the Enforcer watch no camera.", r =>
            {
                var g = r.G;
                r.To(r.SendTo(Tech, AheadOfGuard(r, 400)));
                RefusedFreeBy(r, Tech, "watch", GuardR, "only-scout");
                RefusedFreeBy(r, Tech, "listen", GuardR, "only-scout");
                RefusedFreeBy(r, Tech, "watch", Inez, "only-scout");
                RefusedFreeBy(r, Tech, "watch", Driver, "only-scout");
                RefusedFreeBy(r, Tech, "watch", "security-1", "target-unknown");
                RefusedFreeBy(r, Scout, "watch", "security-1", "target-unknown");
                RefusedFreeBy(r, Fixer, "watch", "camera:entrance", "only-scout");
                RefusedFreeBy(r, Enf, "watch", "patrol", "only-scout");
                r.Is(!g.Known(GuardR)!.Sight && !g.GuardP.KRoutine, "nothing learned of the guard");
            });

            Add("N110", "—", "The Tech doesn't Watch while his loop holds (rulebook sight: tethered to the cabinet): looping the mast camera, he is refused run-tethered on any camera, free, the loop still holding; when he lets the loop go, he walks within reach of the mast camera, watches it and learns it.", r =>
            {
                var g = r.G;
                r.To(r.Send(Tech, "cabinet"));
                r.Do(Tech, "loop", "mast-314"); r.Done(Tech);
                r.Is(g.LoopOn && g.LoopCam == "mast-314", "the mast camera's loop holds");
                var cams = g.WatcherIds().Where(w => w.StartsWith("camera:", StringComparison.Ordinal)).ToList();
                foreach (var c in cams) RefusedFreeBy(r, Tech, "watch", c, "run-tethered");
                r.Is(g.LoopOn, "the loop still holds");
                r.Is(g.Do(new Order { Tick = g.Tick, Op = Tech, Do = "cancel" }), "the Tech lets the loop go");
                r.Is(!g.LoopOn, "the loop ends");
                var spot = WatchSpot(r, "camera:mast-314", unseen: false, who: Tech);
                r.To(r.SendTo(Tech, spot));
                r.Do(Tech, "watch", "camera:mast-314"); r.Done(Tech);
                r.Is(g.Known("camera:mast-314")!.Sight, "the mast camera's sight is learned by the Tech");
            });
        }
    }
}
