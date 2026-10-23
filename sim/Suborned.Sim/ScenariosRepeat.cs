// A Watch or Listen that can teach nothing more is refused, free (Sky 2026-10-18, playing: "if I've listened to someone before, I think I
// can then listen to them again?"). Listen on a person whose need and temperament (and secret, if any) the cell knows: already-heard. Watch
// when everything a Watch can teach of the target is known: a person's routine (and an affair a lover's visit could show), the guard's
// sight too, a camera's sight, the patrol's sight and routine (open until the cell's Watches total its routine seconds): already-watched.
// What a Scout event teaches is read from the rulebook (GameOrders.ScoutTeaches). The card (Quote.Refused) and the query before the click
// (ScoutQuery) give the same answer Issue gives. Each scenario also runs twice and replays from its orders (the Theory tests).
using System;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>A refused Scout order costs nothing: no time, cash, Trail or Alert, the Scout stays free, and the query and the card
        /// said the same code before it was given.</summary>
        static void RefusedFree(Run r, string verb, string target, string code, Person? p = null)
        {
            var g = r.G; var s = g.OpOf(OpId.Scout);
            var (tick, cash, trail, alert, mem) = (g.Tick, g.Cash, g.Trail, g.Alert, p?.Memory.Count ?? 0);
            r.Eq(g.ScoutQuery(verb, target).code, code, $"before the click, {verb} {target} reads {code} (ScoutQuery)");
            if (p != null) r.Eq(g.Quote(g.LearnEv(verb), p).Refused, code, $"the {verb} card on {target} carries {code}");
            var a = g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = verb, Target = target });
            r.Is(!a.ok && a.code == code, $"{verb} {target} again is refused {code} (got {(a.ok ? "taken" : a.code)})");
            r.Is(s.Busy == null && g.Tick == tick && g.Cash == cash && g.Trail == trail && g.Alert == alert && (p?.Memory.Count ?? 0) == mem,
                "the refusal costs nothing: no time, cash, Trail or Alert, the Scout free, nobody remembers it");
            r.Is(g.Log.Exists(l => l.Tick == g.Tick && l.Code == "order.rejected" && l.Text.Contains(target)), "the log keeps the refused order");
        }

        static void RepeatCases()
        {
            Add("N102", "—", "Listen to Inez twice (Sky 2026-10-18): route A's opening, Watch then Listen; the cell now knows her need, her temperament and her secret (the affair), so a second Listen is refused already-heard, free, and the card and the query said so first. Her routine and the affair known, a Watch on her teaches nothing either: already-watched.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "d:Courtyard"));
                { var q = g.ScoutQuery("listen", Inez); r.Is(!q.ok && q.code != "already-heard", $"before a Watch, Listen is refused, but not for knowledge ({q.code})"); }
                r.Is(g.ScoutQuery("watch", Inez).ok && g.Quote(g.R.Ev("cell.watch"), g.Inez).Refused == "", "the first Watch is open: card and query");
                r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.To(r.Send(Scout, "beside-inez"));
                r.Is(g.ScoutQuery("listen", Inez).ok && g.Quote(g.R.Ev("scout.listen"), g.Inez).Refused == "", "the first Listen is open: card and query");
                r.Do(Scout, "listen", Inez); r.Done(Scout);
                r.Is(g.Inez.KNeed && g.Inez.KTemperament && g.Inez.KSecret, "heard: her need, her temperament and the affair");
                RefusedFree(r, "listen", Inez, "already-heard", g.Inez);
                RefusedFree(r, "watch", Inez, "already-watched", g.Inez);
            });

            Add("N103", "—", "Watch the entrance camera twice (Sky 2026-10-18): the first Watch teaches its sight and, a camera being fixed, its routine with it; a second is refused already-watched, free. The refusal is about knowledge, not ground: from far off the query says already-watched, not too-far.", r =>
            {
                var g = r.G;
                r.To(r.SendTo(Scout, WatchSpot(r, "camera:entrance")));
                r.Is(g.ScoutQuery("watch", "camera:entrance").ok, "the first Watch is open");
                r.Do(Scout, "watch", "camera:entrance"); r.Done(Scout);
                var k = g.Known("camera:entrance")!;
                r.Is(k.Sight && k.Routine, "its sight is known, and its fixed routine with it");
                RefusedFree(r, "watch", "camera:entrance", "already-watched");
                r.To(r.Send(Scout, "crew:fixer"));
                r.Eq(g.ScoutQuery("watch", "camera:entrance").code, "already-watched", "far off, still already-watched (not too-far)");
                r.Is(g.ScoutQuery("watch", "camera:pickup").code != "already-watched", "the pickup camera, never watched, is not refused for knowledge");
            });

            Add("N104", "—", "Watch the patrol until its routine is known (Sky 2026-10-18; rulebook sight.watch.routine 40 s): the first Watch teaches its sight; the second is not refused, since its routine is still to learn (20 of 40 s), and teaches it; the third is refused already-watched, free.", r =>
            {
                var g = r.G;
                r.To(r.SendTo(Scout, WatchSpot(r, "patrol", unseen: false)));
                r.Is(r.Until(() => Watchable(r, "patrol"), T(120)), "the patrol comes within reach and in line");
                r.Do(Scout, "watch", "patrol"); r.Done(Scout);
                var k1 = g.Known("patrol")!;
                r.Is(k1.Sight && !k1.Routine, $"one Watch: its sight, not its routine ({k1.WatchedS} of {k1.RoutineNeedsS} s)");
                r.Is(r.Until(() => Watchable(r, "patrol"), T(120)), "in reach and in line again");
                r.Is(g.ScoutQuery("watch", "patrol").ok, "its sight known, its routine not: the second Watch is open");
                r.Do(Scout, "watch", "patrol"); r.Done(Scout);
                r.Is(g.Known("patrol")!.Routine, "two Watches: its routine is known");
                r.Is(r.Until(() => Watchable(r, "patrol"), T(120)), "in reach and in line a third time");
                RefusedFree(r, "watch", "patrol", "already-watched");
            });

            Add("N105", "—", "A Watch that still has something to teach is not refused (Sky 2026-10-18): the guard's routine, need, temperament and the affair told before the mission, but not his sight; the Watch on him is taken and teaches his sight (rulebook sight: the guard's Watch teaches it), then a second is refused already-watched. Inez watched once (her routine known) but never heard: the affair is still to learn, and a Watch while her lover is beside her could show it, so a second Watch on her is taken too.", r =>
            {
                var g = r.G;
                r.Is(g.GuardP.KRoutine && g.GuardP.KSecret && !g.Known(GuardR)!.Sight, "told his routine and the affair, not his sight");
                r.To(r.SendTo(Scout, AheadOfGuard(r, 400)));
                r.Is(g.ScoutQuery("watch", GuardR).ok && g.Quote(g.R.Ev("cell.watch"), g.GuardP).Refused == "", "his sight unknown: the Watch is open, card and query");
                r.Do(Scout, "watch", GuardR); r.Done(Scout);
                r.Is(g.Known(GuardR)!.Sight, "his sight is learned");
                RefusedFree(r, "watch", GuardR, "already-watched", g.GuardP);
            }, Told(GuardR, "routine", "need", "temperament", "secret"));

            Add("N105b", "—", "A Watch that may still show an affair is not refused (Sky 2026-10-18; Motive, motive-lovers.md §1.2: a Watch that ends with two lovers together reveals it): Inez watched once, her routine known, never heard, her lover the guard: the second Watch on her is taken, and Listen is open.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "d:Courtyard"));
                r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.Is(g.Inez.KRoutine && !g.Inez.KSecret && g.Inez.Lover == g.GuardP, "her routine known, the affair not, the guard her lover");
                r.Is(g.Quote(g.R.Ev("cell.watch"), g.Inez).Refused == "" && g.ScoutQuery("watch", Inez).ok, "a second Watch is open: card and query");
                r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.Is(g.Quote(g.R.Ev("scout.listen"), g.Inez).Refused == "", "Listen is still open: her need and temperament unknown");
            });
            Add("N106", "—", "The query asks needs before reach (Latch 2026-10-18): at 0:02 the Scout stands far from Inez and her routine is unknown. ScoutQuery says needs-routine (walking closer won't cure it); the order itself, given there, says the same since engine 0.23.0 (rulebook cell.approach: out of reach it would walk in, so the needs are checked first; before it, Issue answered too-far). Within reach, both say needs-routine.", r =>
            {
                var g = r.G;
                r.To(T(2));
                var at = g.PersonPos(g.Inez)!.Value;
                r.Is(!G.Within(r.Op(Scout).Pos, at, g.R.ListenReachCm), "the Scout stands out of Listen's reach");
                r.Eq(g.ScoutQuery("listen", Inez).code, "needs-routine", "at a distance the query gives the missing need, not too-far");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "listen", Target = Inez }).code, "needs-routine", "the order itself: needs-routine (engine 0.23.0: no walk in for a need walking won't cure)");
                r.To(r.Send(Scout, "beside-inez"));
                r.Eq(g.ScoutQuery("listen", Inez).code, "needs-routine", "within reach, the query: needs-routine");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "listen", Target = Inez }).code, "needs-routine", "within reach, the order: needs-routine");
            });
        }

        static Start Told(string role, params string[] what) { var s = new Start(); s.Known[role] = new System.Collections.Generic.List<string>(what); return s; }
    }
}
