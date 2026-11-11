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
        public static readonly HashSet<string> PlaysTheRound = new HashSet<string>(StringComparer.Ordinal) { "LS-CHAIR", "LS-EARSHOT", /* engine 0.56.0: they read the round */ "N130", "N131", "N132", "N133", "N134", "N135", "N136", "N178", "N179", "N180", "CLOSE-VERDICT", "BOARD-REACH", "GUARD-LEAVES", "SHOT-INTAKE", "CARD-COMING", "SEED-REPLAY", "SEED-DIFFER", "SEED-NEUTRAL", "SEED-FIXED", "SEED-READS", "SEED-FAIR", "PACE-CLOCK", "SHOT-SUM", "SHOT-STREAK", "CAM-5", "CAM-8", "CAM-9", "BOX-1", "KERB-2", "KERB-3", "KERB-5", "PARK-1", "PARK-8" }; // brief 080: these play their own games on the run's inputs (brief 096: and these)
        static string PhaseOf(Game g) => g.PersonPose(g.Inez)?.phase ?? "";
        /// <summary>Engine 0.55.0 (brief 110, cell.listen.follow): from the Listen's start to its landing she is never held, the Scout ends no tick farther
        /// than reach from her for longer than the follow's re-aim allows, the act's clock ran exactly its seconds of ticks in reach, and it lands with her need known.</summary>
        static void ListenFollows(Run r, int startT)
        {
            var g = r.G; int reach = g.R.ListenReachCm, inReach = 0, held = 0;
            while (r.Op(Scout).Busy?.Do == "listen" && !g.Over && g.Tick < startT + T(300))
            {
                if (PhaseOf(g) == "held") held++;
                g.Step();
                if (r.Op(Scout).Busy?.Do == "listen" || g.Inez.KNeed) { var her = g.PersonPos(g.Inez); if (her.HasValue && G.Within(r.Op(Scout).Pos, her.Value, reach)) inReach++; }
            }
            r.Is(held == 0, $"never held for the Listen ({held} ticks held)");
            r.Is(g.Inez.KNeed, $"the Listen landed at {Report.Clock(g.Tick)}: her need is known");
            int listen = T((int)g.R.Ev("scout.listen").Price[0].SV);
            r.Is(inReach >= listen - 1, $"its clock ran in reach: {inReach} ticks in reach of {g.Tick - startT} (the act is {listen})");
        }
        static Start KnowingRoutine() { var s = new Start(); s.Known["waitress"] = new List<string> { "routine" }; return s; }
        static Start KnowingSecret() { var s = new Start(); s.Known["waitress"] = new List<string> { "secret" }; return s; }

        static void RoundsCases()
        {
            Add("N130", "—", "A Listen while she serves is taken (engine 0.23.0, rulebook cell.approach; Sky 2026-10-22: \"we shouldn't block actions whilst Inez is moving about\"): Inez's round takes her to a table out of the Scout's reach. The Scout at beside-inez (her routine known from the briefing) is 3 m from her post: a Listen a third into her serving there is taken, not refused too-far. The card shows the walk first (WalkInS; the HUD's RoundNext still says when she is back at her post); the Scout walks to her (a walk; the activity says listen), the Listen starts in reach, never later than the card said (and no sooner than its look-ahead step and its whole seconds allow). Engine 0.55.0 (brief 110): from then she is not held: she goes on with her round, the Scout keeps to her, and the Listen's clock runs only while he is in reach (ListenFollows). Engine 0.48.0: the table is her round's (the first she serves out of the Scout's reach), not table 3 at 0:34; the round was baked anew on today's clock. Brief 101: the Tech loops the square's mast first, which watches her round now.", r =>
            {
                var g = r.G;
                r.Is(g.RoundsOn, "the mission's data plays her round");
                var sv = g.S.Rounds!.Servers[g.Inez.Id]; var beside = g.S.Stand("beside-inez");
                var ex = sv.Exc.FirstOrDefault(x => !G.Within(beside, x.At, g.R.ListenReachCm) && x.Serve >= 6);
                if (!r.Is(ex != null, "her round serves a table out of the Scout's reach")) return;
                r.Send(Scout, "beside-inez"); LoopMast(r); // brief 101: the square's mast watches her round now, and a Listen's spot is out of every cone
                r.To(Math.Max(g.Tick, T(sv.Origin + ex!.Start + ex.Out) + T(ex.Serve) / 3));
                r.Eq(PhaseOf(g), "serving", $"at {Report.Clock(g.Tick)} she serves {ex.Table}");
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
                r.Is(took <= T(walkS) && took >= T(walkS) - T(g.R.ListenStepS) - T(1), $"after {took / Game.Hz}.{took % Game.Hz} s of walking (the card said {walkS} s: it looks ahead {g.R.ListenStepS} s at a time, in whole seconds)");
                r.Is(G.Dist(at, her) <= 100, "she is still at her table: the Scout came to her");
                ListenFollows(r, startT); // engine 0.55.0 (brief 110, cell.listen.follow): she is not held; the Scout keeps to her and the clock runs only in reach
            }, KnowingRoutine());

            Add("N131", "—", "The Help's hold (brief 022, rounds.holds and linger): the Scout listens to Inez at 0:20 (her routine known from the briefing); the Fixer's Help is given after it lands, 10 s before her round is next due out. Engine 0.55.0 (brief 110): a Listen holds nobody, so the hold runs from the Help's start to its landing plus 3 s: she stands still the whole way, though her round would have taken her out to a table meanwhile; the round clock stopped, so she goes out what was left of it after the hold. Was (0.22.0 to 0.54.0): one hold from the Listen's start to the Help's landing.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Scout, "beside-inez"), r.Send(Fixer, "d:Courtyard")));
                r.To(T(20));
                var a = g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "listen", Target = Inez }); r.Is(a.ok, $"Listen at 0:20 ({a.why})");
                r.Is(r.Until(() => r.Op(Scout).Busy != null, T(60)), "the Listen starts"); r.Done(Scout); // engine 0.55.0 (brief 110): a Listen holds nobody (rounds.holds); the hold is the Help's
                { var sv0 = g.S.Rounds!.Servers[g.Inez.Id]; var nx = sv0.Exc.FirstOrDefault(x => T(sv0.Origin + x.Start) >= g.Tick + T(12)); if (nx != null) r.To(Math.Max(g.Tick, T(sv0.Origin + nx.Start) - T(10))); } // engine 0.55.0: the Help 10 s before her round is next due out, so the hold covers it
                string held = PhaseOf(g); // where her round stood the tick before the hold (read before the order: in reach, the Help holds her at once)
                a = g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "turn", Target = Inez, Approach = "help" }); r.Is(a.ok, $"Help after the Listen, before her round is due out ({a.why})");
                r.Is(r.Until(() => { if (r.Op(Fixer).Busy != null) return true; held = PhaseOf(g); return false; }, T(60)), "the Help starts once the Fixer is in reach of her, held where she stands"); int from = g.Tick;
                int lands = r.Op(Fixer).BusyEnd;
                var sv = g.S.Rounds!.Servers[g.Inez.Id]; var ex = sv.Exc.FirstOrDefault(x => T(sv.Origin + x.Start) >= from);
                if (!r.Is(ex != null, "her round has an excursion after the hold begins")) return;
                int due = T(sv.Origin + ex!.Start);
                r.Is(due < lands + T(g.R.RoundsLingerS), $"her round was due out to {ex.Table} at {Report.Clock(due)}, inside the hold");
                bool still = true; while (g.Tick < lands + T(g.R.RoundsLingerS)) { if (PhaseOf(g) != "held") still = false; g.Step(); }
                r.Is(still, $"held from {Report.Clock(from)} to {Report.Clock(lands)} + 3 s, through {Report.Clock(due)} when her round was due out");
                int back = g.Tick;
                var order = new[] { "at post", "to table", "serving", "from table" }; int hk = Array.IndexOf(order, held);
                r.Is(PhaseOf(g) == held || (hk >= 0 && PhaseOf(g) == order[(hk + 1) % order.Length]), $"the hold over, her round goes on from where it stood: {held} when held, {PhaseOf(g)} now (the walk she was on may end the tick it resumes)");
                if (held == "at post") r.Is(r.Until(() => PhaseOf(g) == "to table", T(30)) && Math.Abs(g.Tick - back - (due - from)) <= 1, $"her round goes on where it stopped: out {(g.Tick - back) / Game.Hz} s later ({(due - from) / Game.Hz} s: her clock stood at {Report.Clock(from)}, her round due out at {Report.Clock(due)})");
                else r.Is(r.Until(() => PhaseOf(g) != held, T(60)), $"her round goes on where it stopped: on from {held} to {PhaseOf(g)}");
            }, KnowingRoutine());

            Add("N132", "—", "The call from mid-walk (brief 022, the summons): Inez turned, the Fixer calls at 0:36, 2 s into her walk to table 3. She turns back and walks to her post; the guard walks the route he always walked to her post and stops 1.2 m short of it, facing her (his walk 1.2 m shorter than to the post); from his arrival they stand together until he turns to go, plus 2 s; then her round resumes at the start of its next rest.", r =>
            {
                var g = r.G;
                r.To(T(36));
                r.Eq(PhaseOf(g), "to table", "0:36: she is walking to a table");
                r.Do(Fixer, "call", Inez);
                g.Step(); r.Eq(PhaseOf(g), "to post", "the call: she walks back to her post");
                int arrive = g.LureTick + g.VisitWalkTicks;
                var st0 = g.S.Rounds!.GuardStops[0]; int cut = (g.S.Rounds.GuardStopWalkCm * 10 + g.R.PeopleWalk - 1) / g.R.PeopleWalk, full = (g.S.Rounds.GuardWalkCm * 10 + g.R.PeopleWalk - 1) / g.R.PeopleWalk; // engine 0.34.0 (M067-B1): the summoned guard walks at the city's pace (crowd.walk), not the cell's
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
                var arrive = g.LureTick + g.VisitWalkTicks; r.To(arrive + 1); var stops = g.S.Rounds!.GuardStops;
                // brief 098 L9 (engine 0.47.1): a tenet walking steps round people at a walk, so he may come to stand on the stop a moment after his walk's clock
                r.Is(r.Until(() => g.GuardPose() is { } p && p.phase == "at café" && G.Within(p.pos, stops[0], 5), T(3)), $"he comes to stand at the first stop {stops[0]} (at {g.GuardPose()?.pos}, {Report.Clock(g.Tick)})");
                var gp = g.GuardPose()!.Value;
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
                // engine 0.30.0 (Pulse's re-bake on the face-margin grid): the file now holds stops every 15° (90°, 75°, 60°, 45° ...), the next three within
                // a metre of the first; he takes the first in the file clear of the Scout by partners-clear-cm, (-883, -3003) on her right (was stops[1], (-953, -2807))
                var clear = stops.FirstOrDefault(q => G.Dist(q, r.Op(Scout).Pos) >= g.R.PartnersClearCm);
                r.Is(G.Within(gp.pos, clear, 5) && clear != stops[0], $"he takes the first stop clear of the Scout, {clear} (at {gp.pos})");
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

            Add("N178", "—", "A Listen while she walks (engine 0.23.0, rulebook cell.approach): at 0:58 Inez walks home from table 3 (from table, 0:57 to 1:05). The Scout at beside-inez, out of reach, is given a Listen: taken. His walk aims at where her round will have her when he gets there, so they meet on her path; the Listen starts the first tick he is within 3 m, while she is still walking. Engine 0.55.0 (brief 110, cell.listen.follow): she is not held; she walks on home and the Scout keeps to her, the Listen's clock running only in reach (ListenFollows). Was: held mid-walk until it landed plus the linger. Brief 101: the Tech loops the square's mast first, which watches her walk now.", r =>
            {
                var g = r.G;
                r.Send(Scout, "beside-inez"); LoopMast(r); // brief 101: the square's mast watches her walk home now, and a Listen's spot is out of every cone
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
                g.Step(); r.Is(PhaseOf(g) != "held", $"not held: she walks on ({PhaseOf(g)}; engine 0.55.0, brief 110)");
                ListenFollows(r, g.Tick - 1);
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
