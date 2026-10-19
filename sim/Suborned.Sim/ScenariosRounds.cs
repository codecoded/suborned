// The living café (brief 022, Pulse; Sky 2026-10-18: "have the waitress also walk around the cafe tables, and when guard comes, they stand
// together"), on the real block with the mission's baked round (sim/data/handover.rounds.json). Pulse's reference poses are matched in
// Suborned.Sim.Tests/Rounds.cs; these play the round with orders.
//  N130 a Listen refused too-far while she serves a table; the HUD's "back in n s" (RoundNext) and PoseAhead say when she is back; then taken
//  N131 the chain hold: a Listen, then a Help given at the tick it lands: one hold, she stands still the whole way though her round was due out
//  N132 the call from mid-walk: she walks back to her post; the guard walks his route and stops 1.2 m short, facing her; they stand together
//  N133 the round ends at the first shot: she walks back from her table and stands her post for the rest of the mission
//  N134 the guard stands at her side (brief 022 addendum): nobody near, he takes the first stop (her left); the Scout at beside-inez is 1.4 m or more from him
//  N135 the Scout stands on the first stop at the call: he takes the other hand
//  N136 both front stops are taken: the stop whose nearest person is farthest
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>The cases that read the round itself (they have nothing to compare with the round off).</summary>
        public static readonly HashSet<string> PlaysTheRound = new HashSet<string>(StringComparer.Ordinal) { "N130", "N131", "N132", "N133", "N134", "N135", "N136" };
        static string PhaseOf(Game g) => g.PersonPose(g.Inez)?.phase ?? "";
        static Start KnowingRoutine() { var s = new Start(); s.Known["waitress"] = new List<string> { "routine" }; return s; }

        static void RoundsCases()
        {
            Add("N130", "—", "A Listen refused while she serves (brief 022): Inez's round takes her to table 3 at 0:34 (8 s out, serving 0:42 to 0:57). The Scout at beside-inez (her routine known from the briefing) is 3 m from her post, 8 m from the table: a Listen at 0:45 is refused too-far. RoundNext says how long until she is back at her post and PoseAhead where she will be; the Listen given then is taken, and holds her.", r =>
            {
                var g = r.G;
                r.Is(g.RoundsOn, "the mission's data plays her round");
                r.To(r.Send(Scout, "beside-inez"));
                r.To(T(45));
                r.Eq(PhaseOf(g), "serving", "at 0:45 she serves a table");
                var a = g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "listen", Target = Inez });
                r.Eq((a.ok, a.code), (false, "too-far"), "the Listen is refused: she is out of reach");
                int n = g.RoundNext(g.Inez);
                r.Is(n > 0 && n < T(60), $"back at her post in {n / Game.Hz} s (the HUD's back in n s)");
                var ahead = g.PoseAhead(g.Inez, n);
                r.Is(ahead.HasValue && ahead.Value.phase == "at post" && ahead.Value.pos == g.S.Stand(g.Inez.Place), "PoseAhead puts her at her post then");
                r.To(g.Tick + n);
                r.Eq(PhaseOf(g), "at post", "she is back");
                a = g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "listen", Target = Inez });
                r.Is(a.ok, $"the Listen is taken ({a.why})");
                r.Wait(T(2)); r.Eq(PhaseOf(g), "held", "spoken to, she stands still");
            }, KnowingRoutine());

            Add("N131", "—", "The chain hold (brief 022, rounds.holds and linger): the Scout listens to Inez at 0:20 (her routine known from the briefing); the Fixer's Help is given at the tick the Listen lands. One hold from 0:20 to the Help's landing plus 3 s: she stands still the whole way, though her round would have taken her to table 3 at 0:34; the round clock stopped, so she goes out 14 s after the hold (her clock was at 0:20).", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Scout, "beside-inez"), r.Send(Fixer, "d:Courtyard")));
                r.To(T(20));
                var a = g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "listen", Target = Inez }); r.Is(a.ok, $"Listen at 0:20 ({a.why})");
                int from = g.Tick; r.Done(Scout);
                a = g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "turn", Target = Inez, Approach = "help" }); r.Is(a.ok, $"Help at the Listen's landing ({a.why})");
                int lands = r.Op(Fixer).BusyEnd;
                bool still = true; while (g.Tick < lands + T(g.R.RoundsLingerS)) { if (PhaseOf(g) != "held") still = false; g.Step(); }
                r.Is(still, $"held from {Report.Clock(from)} to {Report.Clock(lands)} + 3 s, through 0:34 when her round was due out");
                int back = g.Tick;
                r.Eq(PhaseOf(g), "at post", "the hold over, she stands her post");
                r.Is(r.Until(() => PhaseOf(g) == "to table", T(30)) && Math.Abs(g.Tick - back - T(14)) <= 1, $"her round goes on where it stopped: out {(g.Tick - back) / Game.Hz} s later (14 s: her clock stood at 0:20)");
            }, KnowingRoutine());

            Add("N132", "—", "The call from mid-walk (brief 022, the summons): Inez turned, the Fixer calls at 0:36, 2 s into her walk to table 3. She turns back and walks to her post; the guard walks the route he always walked to her post and stops 1.2 m short of it, facing her (his walk 1.2 m shorter than to the post); from his arrival they stand together until he turns to go, plus 2 s; then her round resumes at the start of its next rest.", r =>
            {
                var g = r.G;
                r.To(T(36));
                r.Eq(PhaseOf(g), "to table", "0:36: she is walking to a table");
                r.Do(Fixer, "call", Inez);
                g.Step(); r.Eq(PhaseOf(g), "to post", "the call: she walks back to her post");
                int arrive = g.LureTick + g.VisitWalkTicks;
                var st0 = g.S.Rounds!.GuardStops[0]; int cut = (g.S.Rounds.GuardStopWalkCm * 10 + g.R.CellSpeed - 1) / g.R.CellSpeed, full = (g.S.Rounds.GuardWalkCm * 10 + g.R.CellSpeed - 1) / g.R.CellSpeed;
                r.Is(Math.Abs(g.VisitWalkTicks - cut) <= 3 && g.VisitWalkTicks < full + T(1), $"his walk is to his stop at her side: {g.VisitWalkTicks / Game.Hz} s (to her post it was {full / Game.Hz} s)");
                r.To(arrive + 1);
                var gp = g.GuardPose()!.Value; var post = g.S.Stand(g.Inez.Place);
                int d = G.Dist(gp.pos, post);
                r.Is(gp.phase == "at café" && d >= g.R.PartnersCloseCm - 30 && d <= g.R.PartnersCloseCm + 30, $"he stops {d} cm from her post");
                r.Is(G.Within(gp.pos, g.S.Rounds!.GuardStops[0], 5), "at the first stop in the file (nobody stands near it)");
                r.Is(G.Within(g.PersonPos(g.Inez)!.Value, post, 1) && PhaseOf(g) == "together", "she is at her post, together with him");
                long dot = (long)gp.facing.X * (post.X - gp.pos.X) + (long)gp.facing.Y * (post.Y - gp.pos.Y);
                r.Is(dot > 0, "he faces her");
                r.To(arrive + T(g.R.SummonStayS) + T(g.R.PartnersPartS) + 1);
                r.Eq(PhaseOf(g), "at post", "he has gone: her round resumes at its next rest");
            }, Turned(Inez));

            Add("N133", "—", "The round ends at the first shot (brief 022, rounds.ends): Inez serves table 3 at 0:45 when the Enforcer fires on the guard at the door. The round ends (round.ends, gunfire): she walks back from the table and stands her post for the rest of the mission.", r =>
            {
                var g = r.G;
                r.To(r.SendTo(Enf, BesideDoor));
                r.To(T(44)); r.Eq(PhaseOf(g), "serving", "she serves a table");
                r.Fire(Enf, GuardR);
                r.Is(r.Until(() => r.Shots().Count > 0, T(10)), "the first shot");
                r.Is(g.Log.Exists(l => l.Code == "round.ends"), "the round ends at it");
                r.Is(r.Until(() => PhaseOf(g) == "at post", T(16)), $"she is back at her post ({Report.Clock(g.Tick)})");
                bool stays = true; for (int k = 0; k < T(90) && !g.Over; k += 5) { if (PhaseOf(g) != "at post") stays = false; g.Advance(5); }
                r.Is(stays, "and stays there");
            }, Level(1));

            Add("N134", "—", "The guard stands at her side (brief 022 addendum, partners.summoned.side): Inez turned, the Scout at beside-inez (2.1 m from her post), the Fixer at the Courtyard. The call at 0:20 sends the guard to the first stop in the file (her left, 45 degrees off her facing); nobody stands within a metre of it, and the Scout is 1.4 m or more from him, not 0.9 m.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Scout, "beside-inez"), r.Send(Fixer, "d:Courtyard")));
                r.To(T(20)); r.Do(Fixer, "call", Inez);
                var arrive = g.LureTick + g.VisitWalkTicks; r.To(arrive + 1);
                var gp = g.GuardPose()!.Value; var stops = g.S.Rounds!.GuardStops;
                r.Is(gp.phase == "at café" && G.Within(gp.pos, stops[0], 5), $"he stands at the first stop {stops[0]} (at {gp.pos})");
                var scout = r.Op(Scout).Pos; int d = G.Dist(gp.pos, scout);
                r.Is(d >= 140, $"he is {d} cm from the Scout, not on top of him (was 90 cm at the old stop)");
                r.Is(G.Dist(gp.pos, g.S.Stand(g.Inez.Place)) >= g.R.PartnersCloseCm - 5 && G.Dist(gp.pos, g.S.Stand(g.Inez.Place)) <= g.R.PartnersCloseCm + 5, "1.2 m from her post");
            }, Turned(Inez));

            Add("N135", "—", "The other hand (brief 022 addendum): the Scout stands on the first stop when the call is given (Listen done from there, 1.2 m from Inez's post). The guard takes the second stop, her right, instead of stopping on him.", r =>
            {
                var g = r.G; var stops = g.S.Rounds!.GuardStops;
                r.To(Max(r.SendTo(Scout, stops[0]), r.Send(Fixer, "d:Courtyard")));
                r.To(T(20)); r.Do(Fixer, "call", Inez);
                r.To(g.LureTick + g.VisitWalkTicks + 1);
                var gp = g.GuardPose()!.Value;
                r.Is(G.Within(r.Op(Scout).Pos, stops[0], 30), "the Scout is on the first stop");
                r.Is(G.Within(gp.pos, stops[1], 5), $"he takes the second stop {stops[1]} (at {gp.pos})");
                r.Is(G.Dist(gp.pos, r.Op(Scout).Pos) >= g.R.PartnersClearCm, "a metre or more from the Scout");
            }, Turned(Inez));

            Add("N136", "—", "Every stop taken (brief 022 addendum): the Scout on the first stop, the Fixer on the second, the Tech on the third. The guard takes the stop whose nearest person is farthest (the earlier in the file on a tie), the same pick every time.", r =>
            {
                var g = r.G; var stops = g.S.Rounds!.GuardStops;
                r.To(Max(r.SendTo(Scout, stops[0]), Max(r.SendTo(Fixer, stops[1]), r.SendTo(Tech, stops[2]))));
                r.To(g.Tick + T(1)); r.Do(Fixer, "call", Inez);
                r.To(g.LureTick + g.VisitWalkTicks + 1);
                var gp = g.GuardPose()!.Value; var people = new List<Pt> { r.Op(Scout).Pos, r.Op(Fixer).Pos, r.Op(Tech).Pos, r.Op(Enf).Pos };
                long best = -1; Pt want = stops[0];
                foreach (var st in stops) { long near = long.MaxValue; foreach (var q in people) near = Math.Min(near, G.Dist(st, q)); if (near > best) { best = near; want = st; } }
                r.Is(G.Within(gp.pos, want, 5), $"he stands at {want}, the stop farthest from all four (at {gp.pos}, {best} cm clear)");
            }, Turned(Inez));
        }
    }
}
