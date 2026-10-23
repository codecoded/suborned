// The living café (brief 022, Pulse; Sky 2026-10-18: "have the waitress also walk around the cafe tables, and when guard comes, they stand
// together"), on the real block with the mission's baked round (sim/data/handover.rounds.json). Pulse's reference poses are matched in
// Suborned.Sim.Tests/Rounds.cs; these play the round with orders.
//  N130 a Listen while she serves a table is taken (engine 0.23.0): the Scout walks to her (the card shows the walk), the Listen starts in reach and holds her at the table
//  N131 the chain hold: a Listen, then a Help given at the tick it lands: one hold, she stands still the whole way though her round was due out
//  N132 the call from mid-walk: she walks back to her post; the guard walks his route and stops 1.2 m short, facing her; they stand together
//  N133 the round ends at the first shot: she walks back from her table and stands her post for the rest of the mission
//  N134 the guard stands at her side (brief 022 addendum): nobody near, he takes the first stop (her left); the Scout at beside-inez is 1.4 m or more from him
//  N135 the Scout stands on the first stop at the call: he takes the other hand
//  N136 both front stops are taken: the stop whose nearest person is farthest
//  N178 a Listen while she walks home from a table: the walk aims ahead, meets her on her path, and the Listen holds her mid-walk; then her round goes on
//  N179 the walk in is an order like a move: another order ends it, a cancel ends it, a stance does not; nothing is paid before the act starts
//  N180 dropped on arrival: the Fixer walks in to Watch her while the Scout's Watch is landing; in place, a Watch teaches nothing more: dropped already-watched, free
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>The cases that read the round itself (they have nothing to compare with the round off).</summary>
        public static readonly HashSet<string> PlaysTheRound = new HashSet<string>(StringComparer.Ordinal) { "N130", "N131", "N132", "N133", "N134", "N135", "N136", "N178", "N179", "N180" };
        static string PhaseOf(Game g) => g.PersonPose(g.Inez)?.phase ?? "";
        static Start KnowingRoutine() { var s = new Start(); s.Known["waitress"] = new List<string> { "routine" }; return s; }
        static Start KnowingSecret() { var s = new Start(); s.Known["waitress"] = new List<string> { "secret" }; return s; }

        static void RoundsCases()
        {
            Add("N130", "—", "A Listen while she serves is taken (engine 0.23.0, rulebook cell.approach; Sky 2026-10-22: \"we shouldn't block actions whilst Inez is moving about\"): Inez's round takes her to table 3 at 0:34 (serving 0:42 to 0:57). The Scout at beside-inez (her routine known from the briefing) is 3 m from her post, about 8 m from the table: a Listen at 0:45 is taken, not refused too-far. The card shows the walk first (WalkInS; the HUD's RoundNext still says when she is back at her post); the Scout walks to her (a walk; the activity says listen), the Listen starts the first tick he stands within 3 m, within 2 s of the card's walk, and from then she is held where she is, at the table, until it lands plus the linger.", r =>
            {
                var g = r.G;
                r.Is(g.RoundsOn, "the mission's data plays her round");
                r.To(r.Send(Scout, "beside-inez"));
                r.To(T(45));
                r.Eq(PhaseOf(g), "serving", "at 0:45 she serves a table");
                int n = g.RoundNext(g.Inez); r.Is(n > 0 && n < T(60), $"the HUD's read: back at her post in {n / Game.Hz} s");
                var her = g.PersonPos(g.Inez)!.Value; int reach = g.R.ListenReachCm;
                r.Is(!G.Within(r.Op(Scout).Pos, her, reach), $"the Scout is out of reach ({G.Dist(r.Op(Scout).Pos, her) / 100} m; Listen {reach / 100} m)");
                var q = g.ScoutQuery("listen", Inez); r.Is(q.ok, $"the card is open, not too-far ({q.code})");
                int walkS = g.WalkInS(Scout, "listen", Inez);
                r.Is(walkS > 0 && walkS < 15, $"the card shows the walk first: {walkS} s");
                int given = g.Tick;
                var a = g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "listen", Target = Inez });
                r.Is(a.ok, $"the Listen is taken ({a.code} {a.why})");
                r.Is(r.Op(Scout).Busy == null && r.Op(Scout).Act != null && r.Op(Scout).Walk != null, "the Scout walks in first: no act yet");
                var act = g.ActivityOf(OpId.Scout); r.Is(act.Phase == "walking" && act.Verb == "listen" && act.Target == Inez, $"the activity: walking, to listen to her ({act.Phase} {act.Verb} {act.Target})");
                r.Is(g.Log.Exists(l => l.Tick == given && l.Code == "order.walk-in"), "logged as a walk in");
                r.Is(r.Until(() => r.Op(Scout).Busy != null, T(30)), "the Listen starts");
                int startT = g.Tick, took = startT - given;
                var at = g.PersonPos(g.Inez)!.Value;
                r.Is(G.Within(r.Op(Scout).Pos, at, reach), $"it starts within {reach / 100} m of her ({G.Dist(r.Op(Scout).Pos, at)} cm)");
                r.Is(Math.Abs(took - T(walkS)) <= T(2), $"after {took / Game.Hz}.{took % Game.Hz} s of walking (the card said {walkS} s)");
                r.Is(G.Dist(at, her) <= 100, "she is still at her table: the Scout came to her");
                g.Step(); r.Eq(PhaseOf(g), "held", "spoken to, she stands still");
                int lands = r.Op(Scout).BusyEnd; bool still = true;
                while (g.Tick < lands + T(g.R.RoundsLingerS)) { if (PhaseOf(g) != "held" || g.PersonPos(g.Inez)!.Value != at) still = false; g.Step(); }
                r.Is(still, $"held where she stands, at the table, from {Report.Clock(startT)} to the landing at {Report.Clock(lands)} plus the linger");
                r.Is(g.Inez.KNeed, "the Listen landed: her need is known");
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

            Add("N178", "—", "A Listen while she walks (engine 0.23.0, rulebook cell.approach): at 0:58 Inez walks home from table 3 (from table, 0:57 to 1:05). The Scout at beside-inez, out of reach, is given a Listen: taken. His walk aims at where her round will have her when he gets there, so they meet on her path; the Listen starts the first tick he is within 3 m, while she is still walking, and holds her there, mid-walk, short of her post. After it lands plus the linger her round goes on from where it stopped: she walks the rest of the way home.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "beside-inez"));
                r.To(T(58));
                r.Eq(PhaseOf(g), "from table", "at 0:58 she walks home from her table");
                var was = g.PersonPos(g.Inez)!.Value; var post = g.S.Stand(g.Inez.Place); int reach = g.R.ListenReachCm;
                r.Is(!G.Within(r.Op(Scout).Pos, was, reach), $"the Scout is out of reach ({G.Dist(r.Op(Scout).Pos, was)} cm)");
                int walkS = g.WalkInS(Scout, "listen", Inez); r.Is(walkS > 0, $"the card shows a walk ({walkS} s)");
                int given = g.Tick;
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "listen", Target = Inez }).ok, "the Listen is taken");
                r.Is(r.Op(Scout).Act != null && r.Op(Scout).Busy == null, "taken: the Scout walks in");
                string before = "";
                r.Is(r.Until(() => { if (r.Op(Scout).Busy != null) return true; before = PhaseOf(g); return false; }, T(20)), "the Listen starts");
                var at = g.PersonPos(g.Inez)!.Value;
                r.Is(Math.Abs(g.Tick - given - T(walkS)) <= T(2), $"after {(g.Tick - given) / Game.Hz}.{(g.Tick - given) % Game.Hz} s of walking (the card said {walkS} s: the intercept, read from her round)");
                r.Eq(before, "from table", "she was still walking home the tick before it started");
                r.Is(G.Within(r.Op(Scout).Pos, at, reach), $"within {reach / 100} m of her ({G.Dist(r.Op(Scout).Pos, at)} cm)");
                r.Is(G.Dist(at, was) >= 100 && G.Dist(at, post) >= 100, $"met on her path: {G.Dist(at, was)} cm on from where she was at the order, {G.Dist(at, post)} cm short of her post");
                g.Step(); r.Eq(PhaseOf(g), "held", "held mid-walk");
                int lands = r.Op(Scout).BusyEnd; bool still = true;
                while (g.Tick < lands + T(g.R.RoundsLingerS)) { if (g.PersonPos(g.Inez)!.Value != at) still = false; g.Step(); }
                r.Is(still, "she stands where the Listen found her until it lands plus the linger");
                r.Is(r.Until(() => PhaseOf(g) == "from table", T(2)), $"then her round goes on: walking home ({PhaseOf(g)})");
                r.Is(r.Until(() => PhaseOf(g) == "at post", T(10)), $"and she reaches her post ({Report.Clock(g.Tick)})");
            }, KnowingRoutine());

            Add("N179", "—", "The walk in is an order like a move (engine 0.23.0, rulebook cell.approach): the Scout's Listen given at 0:45 while Inez serves is taken and walks him in; a move given a second later ends it (no Listen starts, she is never held). The Fixer's Ask given from the Courtyard is taken and walks him in with nothing paid; a cancel ends it where he stands (cancel.move), cash unchanged, no hold.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Scout, "beside-inez"), r.Send(Fixer, "d:Courtyard")));
                r.To(T(45)); r.Eq(PhaseOf(g), "serving", "she serves a table");
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "listen", Target = Inez }).ok, "the Listen is taken"); r.Is(r.Op(Scout).Act != null, "the Scout walks in");
                r.Wait(T(1));
                r.Is(r.Op(Scout).Act != null && r.Op(Scout).Walk != null, "still walking in a second later");
                r.Send(Scout, "beside-inez");
                r.Is(r.Op(Scout).Act == null && g.Log.Exists(l => l.Tick == g.Tick && l.Code == "order.walk-in-ends"), "the move ends the walk in");
                r.To(r.Arrival(Scout) + T(3));
                r.Is(r.Op(Scout).Busy == null && !g.Inez.KNeed, "no Listen started");
                long cash = g.Cash;
                var her = g.PersonPos(g.Inez)!.Value;
                if (G.Within(r.Op(Fixer).Pos, her, g.R.ApproachReachCm)) { r.Is(true, "the Fixer is already in reach (nothing to walk)"); return; }
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "turn", Target = Inez, Approach = "ask" }).ok, "the Ask is taken");
                r.Is(r.Op(Fixer).Act != null && r.Op(Fixer).Busy == null && g.Cash == cash, "the Ask walks him in; nothing paid yet");
                r.Do(Fixer, "cancel");
                r.Is(r.Op(Fixer).Act == null && r.Op(Fixer).Walk == null && g.Log.Exists(l => l.Tick == g.Tick && l.Code == "cancel.move"), "a cancel stops him where he stands");
                r.Wait(T(5));
                r.Is(g.Cash == cash && r.Op(Fixer).Busy == null && PhaseOf(g) != "held", "nothing paid, no Ask, she is not held");
            }, KnowingRoutine());

            Add("N180", "—", "Dropped on arrival (engine 0.23.0, rulebook cell.approach): her secret already known, the Scout Watches Inez from beside-inez; 2 s before it lands the Fixer, at the car, is given a Watch on her too, out of his 12 m: taken, he walks in. The Scout's Watch lands and teaches her routine; when the Fixer comes in reach his Watch is checked again as an order given there, teaches nothing, and is dropped already-watched (order.dropped, Game.Dropped), free.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "beside-inez"));
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "watch", Target = Inez }).ok, "the Scout's Watch is taken in place"); int lands = r.Op(Scout).BusyEnd;
                r.To(lands - T(2));
                var her = g.PersonPos(g.Inez)!.Value;
                r.Is(!G.Within(r.Op(Fixer).Pos, her, g.R.WatchReachCm(OpId.Fixer)), "the Fixer is beyond his Watch reach");
                long cash = g.Cash; int trail = g.Trail;
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "watch", Target = Inez }).ok, "the Fixer's Watch is taken"); r.Is(r.Op(Fixer).Act != null, "taken: the Fixer walks in");
                r.Is(r.Until(() => r.Op(Fixer).Act == null, T(60)), "the walk in ends");
                r.Is(g.Inez.KRoutine && g.Tick > lands, "after the Scout's Watch has landed");
                r.Is(r.Op(Fixer).Busy == null && g.Dropped.Exists(d => d.op == OpId.Fixer && d.code == "already-watched"), $"dropped already-watched ({string.Join(", ", g.Dropped.Select(d => d.code))})");
                r.Is(g.Log.Exists(l => l.Code == "order.dropped"), "logged order.dropped");
                r.Is(g.Cash == cash && g.Trail == trail, "free");
            }, KnowingSecret());
        }
    }
}
