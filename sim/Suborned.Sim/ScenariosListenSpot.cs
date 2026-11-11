// The discreet Listen (engine 0.39.0, brief 085; Motive briefs/085/motive/discreet-listen.md) as it plays since engine 0.55.0 (brief 110; Sky 2026-11-11:
// "If I order a listen command, then it should be actioned. No commands should be waiting. It either is accepted or not available."; rulebook
// cell.listen.follow, GameListenFollow.cs). What it holds:
//    LS-SPOT     no spot ahead and no wait: the card reads none, the Scout walks to her at once and is moving or in reach on every tick to the start
//    LS-CHAIR    she stands at her post: from the start he settles near her, seated or by a wall, nearStill or farther, in reach, out of every cone
//    LS-WAIT     the 0.39.0 waiting case (a point ahead on her walk, the Help stopping her short of it): nobody waits; the read never says waiting
//    LS-STILL    someone who stays put (the guard, the driver): heard from 3-5 m, and they don't move for it
//    LS-PLACED   an agent already well placed stays put and starts at once
//    LS-NONE     nothing discreet to be had (a ring with no room, by the rulebook): he listens from where he stands in reach
//    LS-UNSEEN   never noticed: no Trail, Alert or ledger line, and nobody's clock on him moves past where it stood when he began
//    LS-READS    the card's reads choose nothing: reading every tick leaves the final hash as it was
//    LS-CANCEL   a cancel stops the Listen where he stands; the next Listen is its own
//    LS-BOTH     the Scout's Listen and the Fixer's Pay on Inez on one tick: both taken, both act at once, both land
//    P110-PACE   the act-pace dial at 50: every act it names takes half, the card agrees, a world clock stays
//    LS-OLD      a rulebook without cell.listen.follow (pinned before 0.55.0) still walks to the spot ahead and holds her, as 0.54.0 did
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static Start ListenStart(params string[] roles) => new Start(); // engine 0.55.0: a Listen needs no routine known (brief 110); the roles are the scenario's words only

        static Start ListenWait() { var s = new Start(); s.Known[Inez] = new List<string> { "need", "temperament" }; return s; }

        /// <summary>The ticks from the order to the start of the act: true when on each one the Scout either moved or already stood in reach (nobody waits).</summary>
        static bool ActsAtOnce(Run r, string role, int limit, out int took)
        {
            var g = r.G; int from = g.Tick; bool ok = true; var p = g.PersonByRole(role);
            while (r.Op(Scout).Busy == null && r.Op(Scout).Act != null && !g.Over && g.Tick < from + limit)
            {
                var was = r.Op(Scout).Pos; g.Step();
                var her = g.PersonPos(p); bool inReach = her.HasValue && G.Within(r.Op(Scout).Pos, her.Value, g.R.ListenReachCm);
                if (r.Op(Scout).Busy == null && r.Op(Scout).Pos == was && !inReach) ok = false;
            }
            took = g.Tick - from; return ok && r.Op(Scout).Busy?.Do == "listen";
        }

        static void ListenSpotCases()
        {
            Add("LS-SPOT", "110", "No spot ahead and no wait (engine 0.55.0, brief 110): out of reach, the card reads no spot and its walk is the plain walk in; the order is taken, the Scout sets off at once and on every tick to the start he moves or already stands in reach; the act starts in reach and lands with her need known.", r =>
            {
                var g = r.G;
                r.To(T(20));
                r.Is(!g.ListenSpotAhead(Scout, Inez).HasValue, "the card reads no spot ahead");
                int walkS = g.WalkInS(Scout, "listen", Inez); r.Is(walkS >= 0, $"the card's walk: {walkS} s");
                r.Do(Scout, "listen", Inez);
                r.Is(r.Op(Scout).Act != null || r.Op(Scout).Busy != null, "taken: walking in, or at once in reach");
                r.Is(ActsAtOnce(r, Inez, T(120), out int took), $"he acts at once: moving or in reach on every tick, the Listen under way after {took / Game.Hz} s");
                r.Done(Scout);
                r.Is(g.Log.Exists(l => l.Code == "scout.listen.heard"), "heard");
            }, ListenStart(Inez));

            Add("LS-CHAIR", "110", "She stands (engine 0.55.0, cell.listen.follow): the Scout listens from where he reaches her and, while she stays put, settles near her as people do: seated in a free chair or standing by a wall out of the way, no nearer than nearStill, in reach, out of every cone; with nothing discreet in reach he stays where he is in reach. She is never held.", r =>
            {
                var g = r.G;
                if (!r.Is(r.Until(() => g.PersonPose(g.Inez)?.phase == "at post" && g.RoundNext(g.Inez) == 0, T(120)), "she stands at her post")) return;
                r.Do(Scout, "listen", Inez);
                if (!r.Is(r.Until(() => r.Op(Scout).Busy?.Do == "listen", T(90)), "the Listen starts")) return;
                bool settled = false, held = false; string how = "";
                while (r.Op(Scout).Busy?.Do == "listen" && !g.Over)
                {
                    g.Step(); if (g.PersonPose(g.Inez)?.phase == "held") held = true;
                    var her = g.PersonPos(g.Inez); if (!her.HasValue || r.Op(Scout).Walk != null) continue;
                    var at = r.Op(Scout).Pos; bool reach = G.Within(at, her.Value, g.R.ListenReachCm), far = !G.Within(at, her.Value, g.R.ListenNearStillCm);
                    var seat = g.SeatedAt(Scout); bool wall = g.BesideSolid(at);
                    if (reach && far && (seat.HasValue || wall) && !g.WatchedAt(at)) { settled = true; how = seat.HasValue ? "in " + seat.Value.chair : "by a wall"; }
                    else if (reach && g.ListenSpotNow(Scout) == null && !settled) how = "in reach where he stood (nothing discreet to be had)";
                }
                r.Is(!held, "she was never held for it");
                r.Is(settled || how.Length > 0, $"settled: {how}");
                r.Is(g.Inez.KNeed, "heard");
            }, ListenStart(Inez));

            Add("LS-WAIT", "110", "The 0.39.0 waiting case (engine 0.55.0, brief 110: nobody waits): the Scout stands at a point ahead on her walk and the Fixer's Help stops her short of it (rounds.holds). The Listen is taken and the Scout walks to her at once: the read never says waiting, he moves or is in reach on every tick, and the act starts in reach.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Courtyard"));
                Pt? ahead = null; for (int k = 30; k <= 120 && !ahead.HasValue; k += 5) if (g.PoseAhead(g.Inez, T(k))?.pos is Pt q0 && !G.Within(q0, g.PersonPos(g.Inez)!.Value, 1200)) ahead = g.Walk.NearestFree(q0);
                if (!r.Is(ahead.HasValue, "a point ahead on her walk")) return;
                r.To(r.SendTo(Scout, ahead!.Value));
                r.Do(Fixer, "turn", Inez, "help");
                r.Do(Scout, "listen", Inez);
                bool waited = false; int from = g.Tick;
                while (r.Op(Scout).Busy == null && r.Op(Scout).Act != null && !g.Over && g.Tick < from + T(200)) { if (g.ListenSpotNow(Scout) is { waiting: true }) waited = true; g.Step(); }
                r.Is(!waited, "the read never says waiting");
                r.Is(r.Op(Scout).Busy?.Do == "listen", $"the Listen started at {Report.Clock(g.Tick)}, {(g.Tick - from) / Game.Hz} s after the order");
                var her = g.PersonPos(g.Inez)!.Value; r.Is(G.Within(r.Op(Scout).Pos, her, g.R.ListenReachCm), "in reach of her");
            }, ListenWait());

            Add("LS-STILL", "085", "Someone who stays put (the guard, the driver): the Scout hears them from 3-5 m once settled (engine 0.55.0: walked in at once, then a spot nearStill or farther off, in reach); they don't move for it.", r =>
            {
                var g = r.G;
                foreach (var who in new[] { GuardR, Driver })
                {
                    if (g.PersonPos(g.PersonByRole(who)) is not Pt was) { r.Is(false, who + " is on the street"); return; }
                    r.Do(Scout, "listen", who);
                    r.Is(r.Until(() => r.Op(Scout).Busy != null, T(120)), who + ": the Listen starts");
                    bool far = false;
                    while (r.Op(Scout).Busy?.Do == "listen" && !g.Over) { g.Step(); if (r.Op(Scout).Walk == null && !G.Within(r.Op(Scout).Pos, was, g.R.ListenNearStillCm) && G.Within(r.Op(Scout).Pos, was, g.R.ListenReachCm)) far = true; }
                    r.Is(far, $"{who}: heard from 3-5 m off");
                    r.Eq(g.PersonPos(g.PersonByRole(who)), was, who + " stayed put");
                    r.Is(g.PersonByRole(who).KNeed, who + ": heard");
                }
            }, ListenStart(GuardR, Driver));

            Add("LS-PLACED", "085", "An agent already 3-5 m off and beside something solid is in place: the Listen starts at once and he stays where he was put. So the player can place him and be right.", r =>
            {
                var g = r.G; var at = g.PersonPos(g.GuardP)!.Value; Pt? spot = null;
                for (int dx = -500; dx <= 500 && !spot.HasValue; dx += 20) for (int dy = -500; dy <= 500 && !spot.HasValue; dy += 20)
                    { var q = new Pt(at.X + dx, at.Y + dy); if (g.Walk.Free(q) && G.Within(q, at, 480) && !G.Within(q, at, 320) && g.BesideSolid(q) && !g.WatchedAt(q) && g.Walk.Route(r.Op(Scout).Pos, q) != null) spot = q; }
                if (!r.Is(spot.HasValue, "a place by a wall 3-5 m from the guard")) return;
                r.To(r.SendTo(Scout, spot!.Value)); var put = r.Op(Scout).Pos;
                r.Is(g.WalkInS(Scout, "listen", GuardR) == 0, "the card says no walk");
                r.Do(Scout, "listen", GuardR);
                r.Is(r.Op(Scout).Busy != null && r.Op(Scout).Act == null, "the Listen starts at once");
                r.Done(Scout);
                r.Eq(r.Op(Scout).Pos, put, "he stayed where he was put");
            }, ListenStart(GuardR));

            Add("LS-NONE", "085", "Nothing discreet to be had (a ring with no room, by the rulebook: nearStill 5 m): the plain walk in; he listens from where he stands in reach and hears.", r =>
            {
                if (r.Inputs == null) { r.Wait(1); return; } // a test that plays bodies on its own inputs
                var alt = Alt(r, With059(r.Inputs!, "\"listen-near-still-cm\": \\{ \"v\": 300,", "\"listen-near-still-cm\": { \"v\": 500,"));
                var g = alt.G; var at = g.PersonPos(g.PersonByRole(GuardR))!.Value;
                g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "listen", Target = GuardR });
                r.Is(!g.ListenSpotAhead(Scout, GuardR).HasValue, "no spot to be had");
                r.Is(alt.Op(Scout).Act != null, "he walks in");
                int limit = T(120); while (alt.Op(Scout).Busy == null && limit-- > 0) g.Step();
                r.Is(alt.Op(Scout).Busy != null, "the Listen starts");
                r.Is(G.Within(alt.Op(Scout).Pos, at, g.R.ListenReachCm), "from within reach");
                limit = T(120); while (alt.Op(Scout).Busy != null && limit-- > 0) g.Step();
                r.Is(g.GuardP.KNeed, "heard");
            }, ListenStart(GuardR));

            Add("LS-UNSEEN", "085", "Listening is never noticed: from the order to the heard line, no Trail, no Alert, no ledger line, no witness act, and nobody's clock on him moves past where it stood when he began.", r =>
            {
                var g = r.G;
                foreach (var who in new[] { Inez, GuardR })
                {
                    var trail = g.Trail; int alert = g.Alert, ledger = g.Ledger.Count, lines = g.Log.Count;
                    r.Do(Scout, "listen", who);
                    r.Is(r.Until(() => r.Op(Scout).Busy != null, T(120)), who + ": the Listen starts");
                    var ids = g.WatcherIds(); var at = ids.ToDictionary(i => i, i => g.AttentionTicks(i, Scout));
                    r.Done(Scout);
                    r.Eq((g.Trail, g.Alert, g.Ledger.Count), (trail, alert, ledger), who + ": no Trail, Alert or ledger line");
                    foreach (var i in ids) r.Is(g.AttentionTicks(i, Scout) <= at[i], $"{who}: {i}'s clock held through it");
                    r.Is(!g.Log.Skip(lines).Any(l => l.Code.StartsWith("attention.", StringComparison.Ordinal) && l.Text.Contains("cout")), who + ": nobody noticed him");
                }
            }, ListenStart(Inez, GuardR));

            Add("LS-READS", "085", "The card's reads choose nothing: a run that reads the spot every tick ends on the same hash as one that doesn't, Listen and all.", r =>
            {
                if (r.Inputs == null) { r.Wait(1); return; } // a test that plays bodies on its own inputs
                var i = r.Inputs;
                ulong Play(bool read)
                {
                    var h = Engine.New(i, ListenStart(Inez));
                    h.RunTo(T(20)); h.Issue(new Order { Tick = h.Tick, Op = Scout, Do = "listen", Target = Inez });
                    while (h.Tick < T(90)) { if (read) { h.ListenSpotAhead(Scout, GuardR); h.WalkInS(Scout, "listen", Inez); h.ListenSpotNow(Scout); h.SeatedAt(Scout); } h.Step(); }
                    return Hash.State(h);
                }
                r.Wait(1);
                r.Eq(Play(true), Play(false), "the same hash");
            }, ListenStart(Inez));

            Add("LS-CANCEL", "085", "A cancel stops the Listen where he stands (engine 0.55.0: a follow's walk too), nothing heard; the next Listen is its own.", r =>
            {
                var g = r.G;
                r.To(T(20)); r.Do(Scout, "listen", Inez);
                if (!r.Is(r.Until(() => r.Op(Scout).Busy?.Do == "listen", T(90)), "the Listen starts")) return;
                r.Wait(T(3)); r.Do(Scout, "cancel");
                r.Is(r.Op(Scout).Busy == null && r.Op(Scout).Walk == null && !g.ListenSpotNow(Scout).HasValue, "the cancel ends it: no act, no walk, no spot");
                r.Is(!g.Inez.KNeed, "nothing heard");
                r.Do(Scout, "listen", Inez);
                r.Is(r.Op(Scout).Act != null || r.Op(Scout).Busy != null, "and a new Listen is taken");
            }, ListenStart(Inez));

            Add("LS-OLD", "085", "A rulebook pinned before engine 0.55.0 (no cell.listen.follow) plays the discreet Listen as 0.54.0 did: a Listen given out of place reads a spot ahead, and once under way she is held for it (rounds.holds with listen).", r =>
            {
                if (r.Inputs == null) { r.Wait(1); return; } // a test that plays bodies on its own inputs
                var alt = Alt(r, With059(Recast(r.Inputs!, " \"follow\": \"v:listen-follow-cm\",", ""), "\"holds\": \\[\"ask\",", "\"holds\": [\"listen\", \"ask\","), KnowRoutine(Inez));
                var g = alt.G; if (!r.Is(!g.R.ListenFollowOn, "no follow in this rulebook")) return;
                alt.To(T(20));
                r.Is(g.ListenSpotAhead(Scout, Inez).HasValue, "the card reads a spot ahead");
                g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "listen", Target = Inez });
                r.Is(g.ListenSpotNow(Scout).HasValue, "the walk goes to the spot");
                int limit = T(200); while (alt.Op(Scout).Busy == null && limit-- > 0) g.Step();
                if (!r.Is(alt.Op(Scout).Busy != null, "the Listen starts")) return;
                g.Step(); r.Eq(g.PersonPose(g.Inez)?.phase, "held", "and holds her");
            });
            ListenBothCases();
        }
        /// <summary>Inputs whose rulebook text carries one change, its rules read from it (so a With059 on top composes; LS-OLD only).</summary>
        static Engine.Inputs Recast(Engine.Inputs i, string pattern, string replace)
        {
            var re = new System.Text.RegularExpressions.Regex(pattern); if (re.Matches(i.Rulebook).Count != 1) throw new FormatException($"the rulebook has '{pattern}' once");
            var rb = re.Replace(i.Rulebook, replace);
            return new Engine.Inputs { Rulebook = rb, PeopleRules = i.PeopleRules, Site = i.Site, People = i.People, Provisional = i.Provisional, Takedown = i.Takedown, Rounds = i.Rounds, Rules = new Rules(rb, i.PeopleRules), SiteData = i.SiteData, Crowd = i.Crowd };
        }
        static Start KnowRoutine(string role) { var s = new Start(); s.Known[role] = new List<string> { "routine" }; return s; }
        static void ListenBothCases()
        {
            Add("LS-BOTH", "110", "Two of the cell at one person at once (brief 110; Sky 2026-11-11: \"paying Inez felt forever (though I think that was because it was blocked by scout)\"): nothing makes one wait for the other. Her routine known, the Scout's Listen and the Fixer's Pay are given on the same tick: both are taken, both act at once (walking in or under way), the Pay holds her (rounds.holds) and the Listen keeps to her, and both land.", r =>
            {
                var g = r.G;
                r.To(T(20));
                var l = g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "listen", Target = Inez });
                var p = g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "turn", Target = Inez, Approach = "pay" });
                r.Is(l.ok && p.ok, $"both taken on the same tick (listen {l.code}, pay {p.code})");
                r.Is((r.Op(Scout).Act ?? r.Op(Scout).Busy) != null && (r.Op(Fixer).Act ?? r.Op(Fixer).Busy) != null, "both act at once: walking in or under way");
                r.Is(r.Until(() => r.Op(Scout).Busy == null && r.Op(Scout).Act == null && r.Op(Fixer).Busy == null && r.Op(Fixer).Act == null, T(240)), "both end");
                r.Is(g.Inez.KNeed, "the Listen landed: her need is heard");
                r.Is(g.Log.Exists(x => x.Code.StartsWith("turn.pay.", StringComparison.Ordinal)), "the Pay landed");
                r.Is(!g.Dropped.Exists(d => d.op == OpId.Scout || d.op == OpId.Fixer), "neither was dropped");
            }, KnowRoutine(Inez));

            Add("P110-PACE", "110", "The act-pace dial (rulebook pace, engine 0.55.0; Sky 2026-11-11: \"I think we need to reduce the time things take to happen\"): at 50 every act the dial names takes half its seconds, the card says so and the act takes exactly that: a Watch 10 s, a Listen 15 s (its clock in reach), a Loop 10 s; a world clock it does not name (the loop's run, 120 s) stays.", r =>
            {
                if (r.Inputs == null) { r.Wait(1); return; } // a test that plays bodies on its own inputs
                var alt = Alt(r, With059(r.Inputs!, "\"act-pace-pct\": \\{ \"v\": 100,", "\"act-pace-pct\": { \"v\": 50,"));
                var g = alt.G; r.Eq(g.R.ActPacePct, 50, "the dial at 50");
                r.Eq((g.R.Num("v:watch-s"), g.R.Num("v:listen-s"), g.R.Num("v:loop-s"), g.R.Num("p:approaches.pay.timeS"), g.R.Num("v:loop-run-s")), (10L, 15L, 10L, 18L, 120L), "watch 10, listen 15, loop 10, pay 18 (35 x 0.5, rounded); the loop's run 120 (no act)");
                r.Eq(g.Quote(g.R.Ev("scout.listen"), g.Inez).S, 15L, "the Listen's card: 15 s");
                alt.To(alt.Send(Tech, "cabinet")); alt.Do(Tech, "loop", "entrance"); r.Eq(alt.Op(Tech).BusyEnd - g.Tick, T(10), "the Loop takes 10 s"); alt.Done(Tech);
                alt.To(alt.Send(Scout, "beside-inez")); alt.Do(Scout, "listen", Inez);
                if (!r.Is(alt.Until(() => alt.Op(Scout).Busy?.Do == "listen", T(60)), "the Listen starts")) return;
                int start = g.Tick, inReach = 0; while (alt.Op(Scout).Busy?.Do == "listen" && !g.Over) { g.Step(); var her = g.PersonPos(g.Inez); if (her.HasValue && G.Within(alt.Op(Scout).Pos, her.Value, g.R.ListenReachCm)) inReach++; }
                r.Is(g.Inez.KNeed && Math.Abs(inReach - T(15)) <= 1, $"heard after {inReach} ticks in reach ({(g.Tick - start) / Game.Hz} s in all)");
                foreach (var c in alt.Checks) if (!c.ok) r.Is(false, "alt: " + c.what);
            });
        }
    }
}
