// Running and real-time shoot-outs (Sky 2026-10-15: Motive's briefs/011/motive-fights.md approved, all five answers as recommended):
// a run seen by people and by security, runs refused, hits counted not rolled, a drawn weapon as an act, the going-loud card, panic,
// and carrying a downed operative.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static int SawRunner(Game g)
        {
            int n = 0;
            foreach (var p in g.People) n += p.Count("saw-runner");
            foreach (var w in g.Walkers) n += w.P.Count("saw-runner");
            return n;
        }
        /// <summary>A point in front of the guard at his door, within point blank (3 m along his facing), on free ground.</summary>
        static Pt FrontOfGuard(Run r) => r.Stand("d:Office entrance") + new Pt(-300, 0);

        static void FightCases()
        {
            Add("N20", "—", "A running witness (motive-fights.md §1.1): the Scout runs past the Office; people in range remember where and when (no Trail); security that sees the run counts one found fault, Alert 1, once a run; the guard who saw it is no ally in a safe fight.", r =>
            {
                var g = r.G;
                int trail = g.Trail;
                var q = g.QuoteMove(Scout, r.Stand("d:Office approach"));
                r.Is(q.RunS * 2 <= q.WalkS + 1 && q.RunS > 0, $"the chip: run {q.RunS} s against walk {q.WalkS} s (2.8 m/s against 1.4)");
                r.Is(q.RunSeenBy.Count > 0 && q.AlertIfRun == 1, $"the chip names who would see the run ({string.Join(", ", q.RunSeenBy)}) and Alert → 1");
                r.To(r.Send(Scout, "d:Office approach", run: true));
                r.Eq(g.Trail, trail, "running is no traceable act: no Trail");
                r.Eq(g.Alert, 1, "security saw a runner: a found fault, Alert 1");
                r.Eq(g.Ledger.FindAll(l => l.cause.StartsWith("runner-seen:", StringComparison.Ordinal)).Count, 1, "once a run, however many watchers saw it");
                r.Is(SawRunner(g) > 0, $"people remember the runner, place and time ({SawRunner(g)})");
                r.Is(g.GuardP.Has("saw-runner"), "the guard saw him: he holds a memory, so he won't fight at the cell's side (§07 never saw you)");
            });

            Add("N20b", "—", "Running is free once the run is loud (motive-fights.md §1.1): after gunfire nobody counts a runner.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, BesideDoor)));
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                r.Fire(Enf, GuardR); r.Is(r.Until(() => g.GuardDown, T(30)), "the guard is down: the run is loud");
                int seen = SawRunner(g), ledger = g.Ledger.Count;
                r.Is(g.QuoteMove(Scout, r.Stand("d:Office approach")).RunFree, "the chip says running is free");
                r.To(r.Send(Scout, "d:Office approach", run: true));
                r.Eq(SawRunner(g), seen, "no one counts the runner");
                r.Is(!g.Ledger.GetRange(ledger, g.Ledger.Count - ledger).Exists(l => l.cause.StartsWith("runner-seen:", StringComparison.Ordinal)), "and no found fault");
            });

            Add("N21", "—", "Runs refused, each with its reason (motive-fights.md §1.1): the Tech tethered to a loop, an operative in the middle of an action; walking stays open.", r =>
            {
                var g = r.G;
                r.To(r.Send(Tech, "cabinet")); r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                var (ok, why) = g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "move", To = "d:Courtyard", Run = true });
                r.Is(!ok && why.Contains("tethered"), $"the Tech can't run while the loop holds: {why}");
                r.Eq(g.QuoteMove(Tech, r.Stand("d:Courtyard")).RunRefused, why, "the chip says so before the double-click");
                r.To(r.Send(Scout, "d:Courtyard")); r.Do(Scout, "watch", Inez);
                (ok, why) = g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "move", To = "d:Office approach", Run = true });
                r.Is(!ok && why.Contains("busy"), $"mid-action, no run: {why}");
                r.Done(Scout);
                r.Send(Scout, "d:Office approach", run: true);
                r.Is(r.Op(Scout).Running, "the action done, the Scout runs");
                r.Send(Tech, "d:Courtyard");
                r.Is(r.Op(Tech).Walk != null && !r.Op(Tech).Running, "the Tech may still walk away (and the loop ends at the tether)");
            });

            Add("N22", "—", "Hits are counted, not rolled (motive-fights.md §2.2): the untrained Scout's pistol at point blank is 750 ‰, so exactly three hits in four, the third shot the miss; the guard's 3 health falls on the fourth shot. +2 T for the hit, +2 for the down.", r =>
            {
                var g = r.G;
                r.Eq(g.R.Accuracy(g.R.Pistol, 300, false, false, false, true), 750, "point blank × untrained: 750 ‰");
                r.Eq(g.R.Accuracy(g.R.Pistol, 1200, true, false, false, false), 375, "effective, walking: 375 ‰");
                r.Eq(g.R.Accuracy(g.R.Carbine, 1800, false, false, true, false), 562, "the Enforcer holding Vale fires one-handed: 562 ‰");
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Scout, FrontOfGuard(r))));
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                int trail = g.Trail;
                r.Fire(Scout, GuardR);
                r.Is(r.Until(() => g.GuardDown, T(20)), "the guard is down");
                var mine = r.Shots().FindAll(x => x.by == Scout && x.target == GuardR);
                r.Eq(string.Join(",", mine.ConvertAll(x => x.hit ? "hit" : "miss")), "hit,hit,miss,hit", "exactly three in four, in this order, every run");
                r.Eq(mine[1].tick - mine[0].tick, g.R.Pistol.Every, "a pistol fires every 1.5 s");
                r.Eq(g.Trail - trail, g.R.GunfireTrail + g.R.HitTrail + g.R.DownTrail, "+2 T the shooting (once), +2 T hit, +2 T down: not per shot (Motive addendum Q1)");
                r.Eq(g.Gunfire, 4, "four shots fired");
                r.Is(!r.Op(Scout).Down, $"the guard fired back but the Scout stands ({r.Op(Scout).Gun.Health}/{r.Op(Scout).Gun.Max})");
            });

            Add("N23", "—", "A drawn weapon is an act (motive-fights.md §2.3): drawn among people out of security's sight, one witnessed incident (+1 T, Alert 1), and they hold the Fixer's face; drawn in the guard's cone, Alert 2, he engages and calls it in.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Courtyard"));
                var card = g.GoingLoud(new Order { Tick = g.Tick, Op = Fixer, Do = "draw" });
                r.Is(!card.Loud && card.Why == "unseen-draw", $"no security sees him: drawing isn't going loud ({card.Why})");
                int trail = g.Trail;
                r.Do(Fixer, "draw"); r.Wait(1);
                r.Eq(g.Trail - trail, g.R.WitnessTrail, "one witnessed incident: +1 T");
                r.Eq(g.Alert, card.AlertTo, $"Alert as the card said ({card.AlertTo})");
                r.Eq(g.Alert, 1, "Alert 1");
                r.Is(g.Incidents.Exists(i => i.Kind == "weapon-drawn" && i.Witnesses.Count > 0), "the incident with its witnesses (for the investigators)");
                r.Is(g.Walkers.Exists(w => g.HoldsFace(w.P)), "a witness holds the Fixer's face");
                r.Eq(g.Standing, -1, "a gun shown on the street: one step toward Menace (Motive addendum Q5)");
                r.Wait(T(3)); r.Eq(g.Trail - trail, g.R.WitnessTrail, "once a draw, not once a tick");
                r.SendTo(Scout, r.Op(Fixer).Pos + new Pt(0, 100)); r.Until(() => r.Op(Scout).Walk == null, T(120));
                int incidents = g.Incidents.FindAll(i => i.Kind == "weapon-drawn").Count;
                r.Do(Fixer, "holster"); r.Wait(T(1)); r.Do(Fixer, "draw"); r.Do(Scout, "draw"); r.Wait(T(2));
                r.Eq(g.Incidents.FindAll(i => i.Kind == "weapon-drawn").Count, incidents + 1, "two drawing together at one place: one incident (Motive addendum Q1)");
                r.Eq(g.Trail - trail, 2 * g.R.WitnessTrail, "+1 T for it, not +2");
                r.To(r.Send(Fixer, "d:Office approach"));
                r.Eq(g.Alert, 2, "walking into the guard's cone with the weapon out: Alert 2");
                r.Is(g.GuardEngaged, "the guard engages");
                r.Is(g.SecurityArrives < int.MaxValue / 4, "and calls it in");
            });

            Add("N24", "—", "Going loud (motive-fights.md §2.6; Sky Q4: commit by holding, no pause): the card says what a shot would set off, changes nothing, and the run then does what it said.", r =>
            {
                var g = r.G;
                r.To(r.Send(Enf, "d:Office approach"));
                ulong before = Hash.State(g);
                var c = g.GoingLoud(new Order { Tick = g.Tick, Op = Enf, Do = "fire", Target = GuardR });
                r.Eq(Hash.State(g), before, "reading the card changes nothing");
                r.Is(c.Loud && c.Why == "fire", "a shot before any fight goes loud");
                r.Is(c.AlertTo >= 2, $"Alert → {c.AlertTo} ({c.Civilians} civilians watching)");
                r.Eq(c.PoliceInS, c.Civilians > 0 ? g.R.RespWitnessS : g.R.RespSeenS, "police from the north street, by who reports it");
                r.Eq(c.Seal, c.SecurityArrives + T(g.R.SealS), "the seal follows their arrival");
                r.Eq(c.FightAfter, "loud", "the final fight turns loud");
                r.Is(c.Officers >= g.R.LoudOfficers && c.ResponseOfficers == g.R.ResponseOfficers, $"{c.Officers} officers and the response unit's {c.ResponseOfficers}");
                r.Is(c.GuardAllyLost, "the guard won't help");
                r.Eq((c.TrailPerShooting, c.TrailPerHit, c.TrailPerDown, c.HoldTicks), (2, 2, 2, 5), "+2 T the shooting, +2 T a person hit, +2 T a down; hold 0.5 s to commit");
                r.Is(c.DownedCaptured, "the loss line: anyone downed is captured unless carried (Motive addendum Q3)");
                r.Fire(Enf, GuardR);
                r.Until(() => g.Gunfire > 0, T(10));
                r.Eq(g.Alert, c.AlertTo, "Alert as the card said");
                r.Is(g.SecurityArrives >= c.SecurityArrives && g.SecurityArrives <= c.SecurityArrives + g.R.DrawTicks + g.R.AimNewTicks, "the response as the card said, within the draw and the aim");
                var after = g.GoingLoud(new Order { Tick = g.Tick, Op = Scout, Do = "fire", Target = GuardR });
                r.Is(!after.Loud, $"once loud, no card ({after.Why}{after.Refused})");
            });

            Add("N25", "—", "Panic (motive-fights.md §2.4): refused with no fight on; once the guard fires on the cell, every armed operative draws and fires at will.", r =>
            {
                var g = r.G;
                r.Refused("cell", "panic", "", "no fight: panic is refused");
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Scout, FrontOfGuard(r)), r.SendTo(Enf, BesideDoor)));
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                r.Do(Scout, "draw");
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "stance", Stance = "hold" }).ok, "the Scout holds fire");
                r.Is(r.Until(() => g.CellFiredOn, T(10)), "the guard sees the weapon and fires on the Scout: a fight is on");
                r.Is(!r.Shots().Exists(x => x.by == Scout), "holding, the Scout hasn't fired");
                r.Do("cell", "panic");
                r.Is(Array.TrueForAll(g.Ops, o => o.Gun.W == null || (o.Gun.Drawn && o.Gun.Stance == "free")), "every armed operative draws and fires at will");
                r.Is(r.Until(() => g.GuardDown, T(20)), "the guard is down");
                r.Is(r.Shots().Exists(x => x.by == Scout && x.target == GuardR) && r.Shots().Exists(x => x.by == Enf && x.target == GuardR), "the Scout and the Enforcer fired on him without a fire order");
                r.Is(!r.Shots().Exists(x => x.by != GuardR && x.target == "patrol"), "at will is at combatants only: nobody fired on the patrol, which never engaged");
            });

            var pressure = new Start(); pressure.Known[Inez] = new List<string> { "routine", "need", "temperament" };
            Add("N36", "—", "The safe fight lost (Motive addendum Q7): unprepared (Inez pressured, so no warning; the guard saw the Enforcer, so no ally), the Enforcer alone at the car goes down, and the fail is the rivals' goal: they take Vale.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "d:Office approach", run: true)); // running past the door: the guard holds a memory, so no ally (Motive §1.1)
                r.Is(g.GuardP.Has("saw-runner"), "the guard saw the Scout run");
                r.To(Max(r.Send(Fixer, "d:Courtyard"), r.Send(Tech, "cabinet"), r.Send(Scout, "d:Courtyard"))); r.Do(Tech, "loop", "entrance");
                r.Do(Fixer, "turn", Inez, "pressure"); r.Done(Fixer); r.Done(Tech);
                r.SendTo(Enf, new Pt(4900, -1700)); r.To(r.Arrival(Enf));
                while (!g.SweepSeesEntrance(g.Tick)) g.Step();
                r.Do(Fixer, "call", Inez);
                r.To(r.Send(Enf, "d:Office entrance"));
                while (g.PatrolWatching(r.At("d:Office entrance"))) g.Step();
                r.Do(Enf, "enter"); r.Do(Tech, "reroute", "patrol"); r.Done(Enf);
                r.Is(!g.FightWarned, "pressured, Inez warns nobody");
                r.Is(!g.GuardAlly, "the guard saw the cell: no ally");
                r.Send(Enf, "d:Getaway bay");
                r.FightItOut(120);
                r.Eq(g.Result, Outcome.Failure, "the lone, unprepared Enforcer loses the safe fight");
                r.Eq(g.FailCode, "rivals-reach-vale", "and the fail names what the rivals came for");
            }, pressure);

            Add("N37", "—", "The crowd scatters (Pulse §4): a weapon drawn among people on the square startles those in sight within witness range; after the startle they run to a refuge farther from it than they stood, shelter off the street, and walk back when it has been quiet.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Courtyard"));
                var at = r.Op(Fixer).Pos;
                var near = g.Walkers.FindAll(w => { var p = g.CrowdPos(w); return p.HasValue && G.Within(p.Value, at, g.R.WitnessRangeCm) && g.S.Sight(at, p.Value, g.R.Eye); });
                r.Is(near.Count > 0, $"people in sight of the Fixer ({near.Count})");
                r.Do(Fixer, "draw"); r.Wait(1);
                r.Is(near.TrueForAll(w => w.St == "startled"), "each of them is startled");
                var stood = new Dictionary<string, long>(); foreach (var w in near) stood[w.P.Id] = G.D2(w.At, at);
                r.Wait(g.R.CrowdStartleTicks + 1);
                r.Is(near.TrueForAll(w => w.St == "fleeing"), "after the startle they run");
                bool away = true; foreach (var w in near) { var rf = g.S.Refuges.Find(x => x.id == w.Refuge); if (G.D2(rf.at, at) <= stood[w.P.Id] && g.S.Refuges.Exists(x => G.D2(x.at, at) > stood[w.P.Id] && G.Within(x.at, w.At, g.R.CrowdFleeMaxCm))) away = false; }
                r.Is(away, "each to a refuge farther from the weapon than they stood");
                r.Is(r.Until(() => near.TrueForAll(w => w.St == "sheltered"), T(60)), "they reach shelter");
                r.Is(near.TrueForAll(w => !g.CrowdPos(w).HasValue), "off the street");
                r.Do(Fixer, "holster");
                r.Is(r.Until(() => near.TrueForAll(w => w.St == "" || w.St == "returning"), T(g.R.CrowdQuietS + 5)), "quiet for crowd.quiet, they come back");
            });

            Add("N38", "—", "A shot startles everyone within crowd.scatter, seen or not; a person on a watcher's sightline blocks it (Pulse §4: watchers only).", r =>
            {
                var g = r.G;
                r.To(r.Send(Enf, "d:Office approach"));
                var at = r.Op(Enf).Pos;
                var within = g.Walkers.FindAll(w => { var p = g.CrowdPos(w); return p.HasValue && G.Within(p.Value, at, g.R.CrowdScatterCm); });
                r.Fire(Enf, GuardR); r.Until(() => g.Gunfire > 0, T(10)); r.Wait(1);
                r.Is(within.TrueForAll(w => w.St != ""), $"everyone within 30 m is startled or already running ({within.Count})");
                // a sightline through a person on the street is blocked; one that passes 1 m clear of everyone is not
                var w0 = g.Walkers.Find(w => w.St == "" && g.CrowdPos(w).HasValue)!; var p0 = g.CrowdPos(w0)!.Value;
                r.Is(g.CrowdBlocks(p0 + new Pt(-1000, 0), p0 + new Pt(1000, 0)), "a line through a person is blocked (crowd.occlude)");
                int found = -1;
                for (int dy = 100; dy <= 3000 && found < 0; dy += 100)
                {
                    bool clear = true;
                    foreach (var w in g.Walkers) { var p = g.CrowdPos(w); if (p.HasValue && p.Value.X >= p0.X - 1100 && p.Value.X <= p0.X + 1100 && Math.Abs(p.Value.Y - (p0.Y + dy)) < 100) clear = false; }
                    if (clear) found = dy;
                }
                r.Is(found > 0 && !g.CrowdBlocks(p0 + new Pt(-1000, found), p0 + new Pt(1000, found)), $"a line 1 m clear of everyone is not ({found} cm off)");
            });

            Add("N38b", "—", "Shots in a crowd are witnessed (Pulse §4; Latch's play test asked): people within witness range on a clear line of the shooter or the target at the shot remember saw-gunfire with the Fixer's face; people beyond it only startle.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Courtyard"));
                r.Do(Fixer, "draw"); r.Wait(g.R.DrawTicks + 1); // drawn first, so the shot is the only new act
                var inez = g.PersonPos(g.Inez)!.Value;
                r.Fire(Fixer, Inez, force: true);
                r.Is(r.Until(() => g.Gunfire > 0, T(10)), "the shot");
                var at = r.Op(Fixer).Pos;
                var inRange = g.Walkers.FindAll(w => { var p = g.CrowdPos(w); return p.HasValue && ((G.Within(p.Value, at, g.R.WitnessRangeCm) && g.S.Sight(p.Value, at, g.R.Eye)) || (G.Within(p.Value, inez, g.R.WitnessRangeCm) && g.S.Sight(p.Value, inez, g.R.Eye))); });
                r.Is(inRange.Count > 0, $"people stand within witness range of the shot ({inRange.Count})");
                r.Is(inRange.TrueForAll(w => w.P.Has("saw-gunfire")), "each remembers saw-gunfire");
                r.Is(inRange.Exists(w => g.HoldsFace(w.P)), "with the Fixer's face");
                r.Is(g.Incidents.Exists(i => i.Cause == "violence:waitress" && i.Witnesses.Count >= inRange.Count), "and they are the incident's witnesses (for the investigators)");
            });

            Add("N39", "—", "Route A's window is the lover tie (Motive, briefs/011/motive-lovers.md; Sky: Inez and the door guard are lovers): the turned Inez's call sends for the guard, read from the city's ties; he walks the grid to her, stays 60 s (summon.stay) and walks back.", r =>
            {
                var g = r.G;
                r.Is(g.Inez.Lover == g.GuardP && g.GuardP.Lover == g.Inez, "the cast's people file ties them as lovers, both ends");
                r.Do(Fixer, "call", Inez);
                var c = g.Log.FindLast(l => l.Code == "call.waitress");
                r.Is(c.Code == "call.waitress", $"the call sends for him: {c.Text}");
                int walk = g.VisitWalkTicks;
                r.Eq(g.GuardAwayUntil - g.LureTick, 2 * walk + T(g.R.SummonStayS), "away for his walk there, 60 s, and his walk back");
                r.Is(walk > 0 && walk <= T(g.R.SummonWalkMaxS), $"his walk from the door is {walk / 10} s, within summon.walkMax");
                r.Wait(T(5));
                r.Is(!g.GuardAtDoor, "the door is empty: route A's window");
                r.To(g.LureTick + walk);
                r.Eq(g.GuardPose()!.Value.phase, "at café", "he is with her");
                r.To(g.GuardAwayUntil);
                r.Is(g.GuardAtDoor, "and back at his post when the window closes");
                r.Is(g.Chain.Contains("waitress-calls-guard"), "the debrief chain names it");
            }, Turned(Inez));

            var untied = Turned(Inez); untied.Untie.Add(Inez);
            Add("N39b", "—", "With no lover tie the call doesn't pull him (systems over scripts: the lure is the tie, never the mission): a turned Inez has nothing to do on a call.", r =>
            {
                var g = r.G;
                r.Is(g.Inez.Lover == null && g.Inez.Secret.Length == 0, "the tie and her affair are cut (a test's start state)");
                var (ok, code, _) = g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "call", Target = Inez });
                r.Is(!ok && code == "nothing-to-call", $"refused: {code}");
                r.Wait(T(5));
                r.Is(g.GuardAtDoor && g.Trail == 0, "the guard stays at the door, and the refused call costs nothing");
            }, untied);

            Add("N39c", "—", "The summons refused, each at no cost with its code (rulebook summon): already away (busy), come once this shift, and Alert above summon.alertMax; and Alert reaching 2 mid-visit sends him running back.", r =>
            {
                var g = r.G;
                r.Do(Fixer, "call", Inez);
                int trail = g.Trail;
                var (ok, code, _) = g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "call", Target = Inez });
                r.Is(!ok && code == "summon-busy" && g.Trail == trail, $"while he's away: {code}, no cost");
                r.To(g.GuardAwayUntil + 1);
                (ok, code, _) = g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "call", Target = Inez });
                r.Is(!ok && code == "summon-came-once" && g.Trail == trail, $"back at his post: {code}, no cost");
                r.Is(!g.CanCall(Inez), "the card says the call can't be given");
            }, Turned(Inez));

            Add("N39d", "—", "Alert reaching 2 ends the visit (rulebook summon): he runs back to his post at security's pace; and a call at Alert 2 is refused (summon-alert).", r =>
            {
                var g = r.G;
                r.To(r.SendTo(Enf, BesideDoor));
                r.Do(Fixer, "call", Inez);
                int planned = g.GuardAwayUntil;
                r.Wait(T(20));
                r.Fire(Enf, "patrol");
                r.Is(r.Until(() => g.Alert >= 2, T(60)), "gunfire: Alert 2");
                r.Wait(1);
                r.Is(g.Log.Exists(l => l.Code == "guard.recalled"), "the visit ends: he runs back");
                r.Is(g.GuardAwayUntil < planned, $"back sooner than the planned window ({Report.Clock(g.GuardAwayUntil)} < {Report.Clock(planned)})");
            }, Turned(Inez));

            // the Loyal guard's exposure rule is under test (§2.2: a Loyal man pressed by exposure doesn't report it), so the start state makes the
            // guard Loyal whoever the cast is (§04 restricted: a guard is Loyal or Mercenary; the r016 cast's guard is a Mercenary)
            var known = new Start(); known.Known[Inez] = new List<string> { "routine", "need", "temperament" }; known.Temperament[GuardR] = "loyal";
            Add("N40", "—", "Watching the lovers together teaches the affair on both (Motive, motive-lovers.md §1.2); with the guard's secret known, Pressure on the Loyal guard is at its proper step, needs no pressable need, and he doesn't report it (§2.2).", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Fixer, "d:Courtyard"), r.Send(Scout, "d:Courtyard")));
                HelpUntilTurned(r, Inez);
                r.Is(!g.Inez.KSecret && !g.GuardP.KSecret, "help needs no secret: the affair is still unknown");
                r.Eq(g.Preview("pressure", GuardR).Outcome == "?" || !g.Preview("pressure", GuardR).Leverage, true, "no leverage on the guard yet");
                r.Do(Fixer, "call", Inez);
                r.To(g.LureTick + g.VisitWalkTicks);
                r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.Is(g.Inez.KSecret && g.GuardP.KSecret, "a Watch that ends with them together teaches the affair, on both");
                var q = g.Preview("pressure", GuardR);
                r.Is(q.Leverage, "the card reads exposure: his secret");
                r.Eq(q.Reports, "no", "a Loyal man pressed by exposure doesn't report it");
                r.Eq(q.Trail, (long)g.R.Num("p:approaches.pressure.trail"), "at its proper step: no wrong-approach line for a Loyal guard");
                var gp = g.GuardPose()!.Value.pos;
                if (!G.Within(r.Op(Fixer).Pos, gp, g.R.ApproachReachCm)) r.To(r.SendTo(Fixer, g.S.Grid.NearestFree(gp)!.Value));
                r.Is(g.GuardPose()!.Value.phase == "at café", "he is still with her");
                {
                    int alert = g.Alert;
                    r.Do(Fixer, "turn", GuardR, "pressure"); r.Done(Fixer);
                    r.Is(g.GuardP.Turned && g.Alert == alert, "turned, and no report");
                }
            }, known);

            Add("N26", "—", "Downed and carried (motive-fights.md §2.2, §1.1): the guard downs the Scout, who holds fire; the Fixer lifts him in 3 s; the carrier can't run or fire; the Scout's orders are refused.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Scout, FrontOfGuard(r))));
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                r.Do(Scout, "draw"); g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "stance", Stance = "hold" });
                r.Send(Fixer, "d:Office approach");
                r.Is(r.Until(() => r.Op(Scout).Down, T(30)), "the Scout is down");
                r.Refused(Scout, "move", "", "a downed operative takes no orders");
                r.To(r.SendTo(Fixer, r.Op(Scout).Pos));
                r.Do(Fixer, "carry", Scout);
                r.Eq(r.Op(Fixer).BusyEnd - g.Tick, T(g.R.CarryS), "lifting takes 3 s");
                r.Done(Fixer);
                r.Eq(r.Op(Fixer).Carrying, (OpId?)OpId.Scout, "the Fixer carries the Scout");
                var (ok, why) = g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "move", To = "d:Getaway bay", Run = true });
                r.Is(!ok && why.Contains("carrying"), $"carrying, no run: {why}");
                r.Refused(Fixer, "fire", GuardR, "carrying, no fire");
                r.Send(Fixer, "d:Getaway bay"); r.Wait(T(5));
                r.Eq(r.Op(Scout).Pos, r.Op(Fixer).Pos, "the carried Scout goes where the Fixer goes");
                // carried is not yet safe (Waymark asked): only at the car, within the same reach as Extract (cell.atReach)
                r.Is(!G.Within(r.Op(Fixer).Pos, r.At("d:Getaway bay"), 2000) && g.WouldBeCaptured().Contains(Scout), "carried 20 m and more from the car, an extract now would leave him: captured");
                r.Until(() => r.Op(Fixer).Walk == null, T(120));
                r.Is(G.Within(r.Op(Fixer).Pos, r.At("d:Getaway bay"), g.R.AtReachCm) && g.WouldBeCaptured().Count == 0, "carried to the car, within Extract's reach: he escapes");
            });
        }
    }
}
