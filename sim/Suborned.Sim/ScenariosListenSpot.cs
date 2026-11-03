// Engine 0.39.0 (brief 085; Motive briefs/085/motive/discreet-listen.md; rulebook cell.listen). What it holds:
//    LS-SPOT     someone on a walk: the earliest spot, in the ring, by something solid or a chair, out of every cone; the host's reads
//    LS-CHAIR    a free chair near her walk: the Scout takes it, and the reads name it
//    LS-WAIT     at the spot before she comes: waiting; the attention clock holds; she stops within reach, with her own facing
//    LS-STILL    someone who stays put: a spot nearStill or farther off
//    LS-PLACED   an agent already well placed stays put and starts at once
//    LS-NONE     nothing free: the plain walk in, stopping at reach
//    LS-UNSEEN   never noticed: no Trail, Alert, ledger line, report or clock across the whole Listen
//    LS-READS    the card's read chooses nothing: reading every tick leaves the final hash as it was
//    LS-CANCEL   another order ends it: the read clears, and the next Listen chooses afresh
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static Start ListenStart(params string[] roles) { var s = new Start(); foreach (var ro in roles) s.Known[ro] = new List<string> { "routine" }; return s; }

        /// <summary>Plays a Listen on `role` until it starts; false if it never does. The spot read at the start comes back.</summary>
        static (Pt at, string chair)? ListenToStart(Run r, string role, int limit = 900)
        {
            var g = r.G; r.Do(Scout, "listen", role);
            if (!r.Is(r.Until(() => r.Op(Scout).Busy != null || g.Over, limit) && r.Op(Scout).Busy != null, "the Listen starts")) return null;
            return g.ListenSpotNow(Scout) is var n && n.HasValue ? (n.Value.at, n.Value.chair) : ((Pt, string)?)null;
        }

        static Start ListenWait() { var s = new Start(); s.Known[Inez] = new List<string> { "routine", "need", "temperament" }; return s; }

        static void ListenSpotCases()
        {
            Add("LS-SPOT", "085", "Someone on a walk: the Scout takes the earliest spot her walk brings within reach, no nearer than nearWalking at the choice, beside something solid or a chair, out of every watcher's cone; the card reads the same spot the engine then takes.", r =>
            {
                var g = r.G;
                r.To(T(20));
                var a = g.ListenSpotAhead(Scout, Inez);
                if (!r.Is(a.HasValue, "the card reads a spot")) return;
                r.Is(g.WalkInS(Scout, "listen", Inez) == a!.Value.startS && a.Value.startS >= a.Value.walkS, $"the card's time is the start, {a.Value.startS} s, the walk {a.Value.walkS} s and any wait");
                r.Is(!g.WatchedAt(a.Value.at), "out of every cone at the choice");
                r.Do(Scout, "listen", Inez);
                var n = g.ListenSpotNow(Scout);
                r.Is(n.HasValue && n.Value.at == a.Value.at && n.Value.chair == a.Value.chair && n.Value.target == Inez, "the engine takes the spot the card read");
                r.Is(n!.Value.chair.Length > 0 || g.BesideSolid(n.Value.at), "a chair, or ground beside something solid");
                r.Is(r.Until(() => r.Op(Scout).Busy != null, T(120)), "the Listen starts");
                var her = g.PersonPos(g.Inez)!.Value;
                r.Is(G.Within(r.Op(Scout).Pos, her, g.R.ListenReachCm), "she is within reach");
                r.Is(G.Within(r.Op(Scout).Pos, n.Value.at, g.R.ListenAtSpotCm), "from the spot");
                r.Is(g.ListenSpotNow(Scout).HasValue, "the read stands through the act");
                r.Done(Scout);
                r.Is(g.Log.Exists(l => l.Code == "scout.listen.heard"), "heard");
                r.Is(!g.ListenSpotNow(Scout).HasValue, "and clears when the Listen lands");
            }, ListenStart(Inez));

            Add("LS-CHAIR", "085", "A free chair near her walk: placed by a table of a chair that stays free, the Scout takes the chair: the reads name it and give its walkable point.", r =>
            {
                var g = r.G;
                r.To(T(5));
                foreach (var name in g.People2!.Chairs)
                {
                    var st = g.Walk.NearestFree(g.People2.Places[name]); if (!st.HasValue || g.Walk.Route(r.Op(Scout).Pos, st.Value) == null) continue;
                    r.To(r.SendTo(Scout, st.Value));
                    if (g.ListenSpotAhead(Scout, Inez) is var a && a.HasValue && a.Value.chair.Length > 0)
                    {
                        r.Do(Scout, "listen", Inez);
                        var n = g.ListenSpotNow(Scout);
                        r.Is(n.HasValue && n.Value.chair == a.Value.chair && g.People2.Places.ContainsKey(n.Value.chair), $"the chair is named: {a.Value.chair}");
                        r.Is(G.Dist(n!.Value.at, g.People2.Places[n.Value.chair]) <= g.R.ListenChairGroundCm, "and its walkable point is by it");
                        r.Is(r.Until(() => r.Op(Scout).Busy != null, T(120)), "the Listen starts from the chair");
                        return;
                    }
                }
                r.Is(false, "no chair came up from any table");
            }, ListenStart(Inez));

            Add("LS-WAIT", "085", "At the spot before she comes: the Scout waits. The read says so, no watcher's clock runs on him while he waits, and she stops within reach keeping her own facing.", r =>
            {
                var g = r.G;
                // the Scout has a spot on her walk; then the Fixer's Help stops her short of it (rounds.holds), so he stands there waiting for her
                r.To(r.Send(Fixer, "d:Courtyard"));
                Pt? ahead = null; for (int k = 30; k <= 120 && !ahead.HasValue; k += 5) if (g.PoseAhead(g.Inez, T(k))?.pos is Pt q0 && !G.Within(q0, g.PersonPos(g.Inez)!.Value, 1200)) ahead = g.Walk.NearestFree(q0);
                if (!r.Is(ahead.HasValue, "a point ahead on her walk")) return;
                r.To(r.SendTo(Scout, ahead!.Value));
                r.Do(Scout, "listen", Inez);
                r.Do(Fixer, "turn", Inez, "help");
                bool waited = false, ranOn = false; string w = "";
                while (r.Op(Scout).Busy == null && !g.Over && g.Tick < T(200))
                {
                    var n = g.ListenSpotNow(Scout);
                    if (n.HasValue && n.Value.waiting)
                    {
                        if (!waited) { waited = true; w = Report.Clock(g.Tick); }
                        var ids = g.WatcherIds(); var was = ids.Select(id => g.AttentionTicks(id, Scout)).ToList(); g.Step();
                        for (int k = 0; k < ids.Count; k++) if (g.AttentionTicks(ids[k], Scout) > was[k]) ranOn = true;
                        continue;
                    }
                    g.Step();
                }
                r.Is(r.Op(Scout).Busy != null, "the Listen starts");
                r.Is(waited, $"he waited at the spot from {w}");
                r.Is(!ranOn, "no clock ran on him while he waited");
                var q = g.PoseAhead(g.Inez, 0)!.Value; var sv = g.ServerFace(g.Inez)!.Value;
                r.Eq(q.phase, "held", "she stands still for it");
                r.Eq(q.facing, sv, "she keeps her own facing, not turned to him");
            }, ListenWait());

            Add("LS-STILL", "085", "Someone who stays put (the guard, the driver): the Scout takes a spot nearStill or farther off and within reach, and hears them from it; they don't move for it.", r =>
            {
                var g = r.G;
                foreach (var who in new[] { GuardR, Driver })
                {
                    if (g.PersonPos(g.PersonByRole(who)) is not Pt was) { r.Is(false, who + " is on the street"); return; }
                    r.Do(Scout, "listen", who);
                    var n = g.ListenSpotNow(Scout);
                    if (!r.Is(n.HasValue, who + ": a spot")) return;
                    r.Is(!G.Within(n!.Value.at, was, g.R.ListenNearStillCm) && G.Within(n.Value.at, was, g.R.ListenReachCm), $"{who}: {G.Dist(n.Value.at, was)} cm off, 3-5 m");
                    r.Is(r.Until(() => r.Op(Scout).Busy != null, T(120)), who + ": the Listen starts");
                    r.Is(G.Within(r.Op(Scout).Pos, n.Value.at, g.R.ListenAtSpotCm), who + ": from the spot");
                    r.Eq(g.PersonPos(g.PersonByRole(who)), was, who + " stayed put");
                    r.Done(Scout);
                }
            }, ListenStart(GuardR, Driver));

            Add("LS-PLACED", "085", "An agent already 3-5 m off and beside something solid is in place: no walk, no spot, the Listen starts at once. So the player can place him and be right.", r =>
            {
                var g = r.G;
                var a = g.ListenSpotAhead(Scout, GuardR); if (!r.Is(a.HasValue && a.Value.chair.Length == 0, "a spot on the ground to go to")) return;
                r.To(r.SendTo(Scout, a!.Value.at));
                r.Is(!g.ListenSpotAhead(Scout, GuardR).HasValue, "there, no spot is needed");
                r.Is(g.WalkInS(Scout, "listen", GuardR) == 0, "the card says no walk");
                r.Do(Scout, "listen", GuardR);
                r.Is(r.Op(Scout).Busy != null && r.Op(Scout).Act == null, "the Listen starts at once");
                r.Is(!g.ListenSpotNow(Scout).HasValue, "with no spot of its own");
            }, ListenStart(GuardR));

            Add("LS-NONE", "085", "Nothing free (a ring with no room, by the rulebook): the plain walk in: no spot, he stops at reach and hears.", r =>
            {
                if (r.Inputs == null) { r.Wait(1); return; } // a test that plays bodies on its own inputs
                var alt = Alt(r, With059(r.Inputs!, "\"listen-near-still-cm\": \\{ \"v\": 300,", "\"listen-near-still-cm\": { \"v\": 500,"));
                var g = alt.G; var at = g.PersonPos(g.PersonByRole(GuardR))!.Value;
                g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "listen", Target = GuardR });
                r.Is(!g.ListenSpotNow(Scout).HasValue && !g.ListenSpotAhead(Scout, GuardR).HasValue, "no spot to be had");
                r.Is(alt.Op(Scout).Act != null, "he walks in");
                int limit = T(120); while (alt.Op(Scout).Busy == null && limit-- > 0) g.Step();
                r.Is(alt.Op(Scout).Busy != null, "the Listen starts");
                r.Is(G.Within(alt.Op(Scout).Pos, at, g.R.ListenReachCm), "from within reach");
            }, ListenStart(GuardR));

            Add("LS-UNSEEN", "085", "Listening is never noticed: from the order to the heard line, no Trail, no Alert, no ledger line, no witness act, and nobody's clock on him moves past where it stood when he reached the spot or began.", r =>
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

            Add("LS-READS", "085", "The card's read chooses nothing: a run that reads the spot every tick ends on the same hash as one that doesn't, Listen and all.", r =>
            {
                if (r.Inputs == null) { r.Wait(1); return; } // a test that plays bodies on its own inputs
                var i = r.Inputs;
                ulong Play(bool read)
                {
                    var h = Engine.New(i, ListenStart(Inez));
                    h.RunTo(T(20)); h.Issue(new Order { Tick = h.Tick, Op = Scout, Do = "listen", Target = Inez });
                    while (h.Tick < T(90)) { if (read) { h.ListenSpotAhead(Scout, GuardR); h.WalkInS(Scout, "listen", Inez); h.ListenSpotNow(Scout); } h.Step(); }
                    return Hash.State(h);
                }
                r.Wait(1);
                r.Eq(Play(true), Play(false), "the same hash");
            }, ListenStart(Inez));

            Add("LS-CANCEL", "085", "Another order ends the walk to the spot: the read clears, and the next Listen chooses afresh.", r =>
            {
                var g = r.G;
                r.To(T(20)); r.Do(Scout, "listen", Inez);
                r.Is(g.ListenSpotNow(Scout).HasValue, "a spot");
                r.Wait(T(1)); r.SendTo(Scout, r.Op(Scout).Pos);
                r.Is(!g.ListenSpotNow(Scout).HasValue, "the move ends it");
                r.Do(Scout, "listen", Inez);
                r.Is(g.ListenSpotNow(Scout).HasValue, "and a new Listen has its own");
            }, ListenStart(Inez));
        }
    }
}
