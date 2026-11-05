// Motive's misses scenarios (engine 0.43.0, brief 096; briefs/096/motive/misses.json "scenarios_for_keel"; Sky 2026-11-05 "all recommended"):
//  VET-5 fired-unknown (V3: a holstered agent who fired is no foe for having fired while the gang shooting is unplaced) · VET-6 fired-placed (once
//  placed, by a shot or a gun seen, "fired" counts again) · LEND-13b police-75 (L8 at 75 s; a late vale.out meets the 8:10 close first) ·
//  LEND-22 go-waits (G1: Go while an agent climbs in lands the tick his Board lands; the card leaves nobody behind) · FIX-5 broken-returns (F2's
//  wording: a crew broken before the final fight with its car working drives off and comes back to block; broken in the final fight it never
//  returns) · PAY-6 pay-needs-watch (rivals.turn.needs: Pay on a member needs a Watch first) · Q-1 crew-query (CrewActQuery answers as Issue does)
//  · Q-2 lent-watcher (LentCarWatcher reads as the lent car's seen-leaving). LEND-B is the bands (briefs/096/keel/bands-misses-after-200.txt).
using System;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>The police and robot bodies whose causes V3 reads: the guard (police) and the patrol (robots).</summary>
        static readonly string[] V3Bodies = { "guard", "patrol" };
        /// <summary>The district at security level 0 (lax: the longest seal), so a scenario can watch the rival car come back after the gunfire.</summary>
        static Start Lax(Start s) { s.Security = 0; return s; }

        static void Misses096()
        {
            Add("096-VET-5", "096", "Motive VET-5 (V3): a gang shooting pending (the veteran's shot, nobody of security saw it), the four holster: an agent who fired is no foe to the police or the robots for having fired; nothing places the cell for it.", r =>
            {
                var g = r.G; if (!r.Is(VeteranShot(r), "the shot")) return;
                r.Until(() => g.CarDisabled("car:rivals") || g.Over, T(60));
                if (!g.GangPending) { r.Line(false, $"seed 0's corner was seen ({g.GangClaimedWhy}): nothing unplaced to read"); return; }
                foreach (var op in new[] { Scout, Fixer, Tech, Enf }) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "holster" });
                r.Wait(T(2));
                var fired = new[] { Scout, Fixer, Tech, Enf }.Where(o => r.Op(o).Gun.Fired).ToList();
                if (!r.Is(fired.Count > 0, Iv($"{fired.Count} of the four fired"))) return;
                r.Is(g.GangPending, "still unplaced");
                foreach (var b in V3Bodies) foreach (var o in fired) r.Is(!g.FireCauses(b, Op.Parse(o)).Contains("fired"), Iv($"{b} counts the {o} no foe for having fired ({string.Join(",", g.FireCauses(b, Op.Parse(o)))})"));
            });

            Add("096-VET-6", "096", "Motive VET-6 (V3): after the veteran's shot the Tech fires on the entrance camera in its sight: the shooting is seen, the cell placed, and \"fired\" counts again for every agent who fired.", r =>
            {
                var g = r.G; if (!r.Is(VeteranShot(r), "the shot")) return;
                r.Until(() => g.CarDisabled("car:rivals") || g.Over, T(60));
                if (!g.GangPending) { r.Line(false, $"seed 0's corner was seen ({g.GangClaimedWhy}): nothing to place"); return; }
                foreach (var op in new[] { Scout, Fixer, Tech, Enf }) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "holster" });
                r.Wait(T(2)); r.Is(!g.FireCauses("guard", OpId.Enforcer).Contains("fired") || !r.Op(Enf).Gun.Fired, "unplaced: the Enforcer is no foe for having fired");
                r.Fire(Tech, "camera:entrance", true);
                r.Until(() => !g.GangPending || g.Over, T(60));
                if (!r.Is(!g.GangPending, Iv($"placed: {g.GangClaimedWhy}"))) return;
                foreach (var o in new[] { Scout, Fixer, Tech, Enf }.Where(x => r.Op(x).Gun.Fired))
                    r.Is(g.FireCauses("guard", Op.Parse(o)).Contains("fired"), Iv($"placed: the guard counts the {o} a foe for having fired"));
            });

            Add("096-LEND-13b", "096", "Motive LEND-13b (L8 at 75 s): split-police-s is 75; Go on the lent car as the Take ends (about 7:20 with the three away) and the 8:10 close comes before vale.out + 75 s: the mission ends a win at the close.", r =>
            {
                var g = r.G; r.Eq(g.R.SplitPoliceS, 75, "split-police-s");
                ThreeAway(r); if (!LendAt(r)) { r.Is(false, "lent"); return; }
                r.Is(g.Issue(GoOn(g, "car:official")).ok, Iv($"Go on the lent car at {Report.Clock(g.Tick)}"));
                int close = T(g.ClockOver); r.Eq(g.SplitPoliceAt, Math.Min(close, g.ValeOutTick + T(75)), "the countdown reads the sooner of vale.out + 75 s and the close");
                r.Is(g.ValeOutTick + T(75) > close, "the close comes first");
                r.Until(() => g.Over, T(80));
                r.Is(g.Result == Outcome.Success && g.Tick == close, Iv($"a win at {Report.Clock(g.Tick)}"));
            }, LendStart());

            Add("096-LEND-22", "096", "Motive LEND-22 (G1): after vale.out, the Scout and the Fixer in our car, the Tech climbing in: Go waits for his Board and the car leaves the tick it lands, all three aboard; the Go's card left nobody behind.", r =>
            {
                var g = r.G; if (!LendAt(r)) { r.Is(false, "lent"); return; }
                r.Is(g.Issue(GoOn(g, "car:official")).ok, "vale.out");
                foreach (var op in new[] { Scout, Fixer, Tech }) r.SendTo(op, g.GetawaySeat);
                r.Until(() => new[] { Scout, Fixer, Tech }.All(o => g.AtGetaway(r.Op(o).Pos) && r.Op(o).Walk == null), T(60));
                r.Do(Scout, "board", "car"); r.Do(Fixer, "board", "car"); r.Wait(T(g.R.BoardS) + 1);
                if (!r.Is(r.Op(Scout).InCar && r.Op(Fixer).InCar && !g.Over, "two aboard")) return;
                r.Do(Tech, "board", "car"); int lands = r.Op(Tech).BusyEnd;
                r.Is(g.GoWouldWait("car"), "a Go now would wait for the Tech");
                r.Is(!g.LeftBehind().Any(x => x.op == "tech"), Iv($"the card leaves nobody behind ({string.Join(",", g.LeftBehind().Select(x => x.op))})"));
                var a = g.Issue(GoOn(g, "car")); r.Is(a.ok && g.GoWaiting("car") && !g.GetawayGone, Iv($"taken, waiting ({a.code} {a.why})"));
                r.Until(() => g.GetawayGone || g.Over, T(g.R.BoardS + g.R.CarryS + 2));
                r.Eq(g.CarLeftTick, lands, "the car leaves the tick the Board lands");
                r.Is(r.Op(Tech).LeftMap && g.Result == Outcome.Success && g.Captured.Count == 0, Iv($"all three out, nobody captured ({g.Result}, {string.Join(",", g.Captured)})"));
            }, NoRivals(Turned(Driver)));

            Add("096-LEND-22b", "096", "G1's twin: the climbing agent shot down or called off mid-Board: the Go lands the next tick with whoever is in; an empty car with nobody climbing in still refuses car-empty.", r =>
            {
                var g = r.G; r.To(r.SendTo(Scout, g.GetawaySeat));
                var e = g.Issue(GoOn(g, "car")); r.Eq((e.ok, e.code), (false, "car-empty"), "nobody in or climbing in: car-empty");
                r.Do(Scout, "board", "car");
                var a = g.Issue(GoOn(g, "car")); r.Is(a.ok && g.GoWaiting("car"), Iv($"the Scout climbing in: the Go waits ({a.code})"));
                r.Do(Scout, "cancel"); r.Wait(1);
                r.Is(!g.GoWaiting("car") && g.Log.Exists(l => l.Code == "go.missed"), "called off: the Go lands, refused car-empty (go.missed)");
            }, NoRivals(Turned(Driver)));

            Add("096-FIX-5", "096", "Motive FIX-5 (F2's wording): the crew broken before the final fight, its car working, boards and drives off holstered, out of every fight while away, and is back to block again security.rivals.retreat later; broken in the final fight it never returns.", r =>
            {
                var g = r.G; r.Until(() => g.Crew.Take(3).All(m => m.Mode == CrewMode.Posted), T(300));
                r.Engage(() => g.Crew.Take(3).Where(m => !m.Gun.Down).Select(m => m.Id).ToList(), () => g.CrewBrokenPre || g.Over, 90, new[] { Scout, Fixer, Tech, Enf }, false);
                if (!r.Is(g.CrewBrokenPre, Iv($"broken before the final fight at {Report.Clock(g.Tick)} ({g.Result} {g.FailCode})"))) return;
                r.Until(() => g.CarSt == CrewCar.Away || g.Over, T(60));
                r.Is(g.Crew.Where(m => !m.Gun.Down).All(m => !m.Fight && !m.Gun.Drawn), "away: holstered, in no fight");
                r.Until(() => g.CarSt == CrewCar.Returning || g.CarSt == CrewCar.Blocking || g.Over, T(g.R.RivalsRetreatS + 40));
                r.Is(g.CarSt == CrewCar.Returning || g.CarSt == CrewCar.Blocking, Iv($"it comes back at {Report.Clock(g.Tick)} ({g.CarSt}; {g.Result} {g.FailCode})"));
                r.Until(() => g.CarSt == CrewCar.Blocking || g.Over, T(60));
                r.Line(g.CarSt == CrewCar.Blocking && g.GetawayBoxers().Contains("car:rivals") && !g.CrewBrokenPre, $"back across our way out, no longer broken ({g.CarSt}, broken {g.CrewBrokenPre}; {g.Result} {g.FailCode}: the seal can come first)");
            }, Lax(KeepVale(new Start())));

            Add("096-FIX-5b", "096", "FIX-5's twin (F1, F2): the crew broken in the final fight: the wheelman drives off for good; the car never comes back.", r =>
            {
                var g = r.G; if (!r.Is(LendAndBreak(r), "the crew breaks in the final fight")) return;
                int br = LogTickOf(g, "ai.break");
                r.Until(() => g.Over || g.Tick >= br + T(g.R.RivalsRetreatS + 20), T(g.R.RivalsRetreatS + 30));
                r.Is(!g.Log.Exists(l => l.Code == "rivals.back" && l.Tick > br), "never back");
            }, LendStart());

            Add("096-PAY-6", "096", "Motive's rivals page (pay.needs, Sky 2026-11-04): Pay on a member needs a Watch first: refused needs-routine before it, by the query and the order alike; after the Fixer's Watch on him it is taken.", r =>
            {
                var g = r.G; r.To(Math.Max(g.Tick, r.SendTo(Fixer, BesideLookout(g))));
                r.Until(() => LookoutAtPost(g), T(330)); if (!r.Is(LookoutAtPost(g), "the lookout posted")) return;
                var q = g.CrewActQuery("fixer", "pay", "rival-4"); r.Eq((q.ok, q.code), (false, "needs-routine"), Iv($"the query: {q.why}"));
                var a = g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "turn", Target = "rival-4", Approach = "pay" }); r.Eq((a.ok, a.code), (false, "needs-routine"), "the order");
                r.Do(Fixer, "watch", "rival-4"); r.Done(Fixer); r.Is(g.RivalsWatched, "watched");
                long cash = g.Cash; var q2 = g.CrewActQuery("fixer", "pay", "rival-4"); r.Is(q2.ok && g.Cash == cash, Iv($"the query says yes and costs nothing ({q2.code})"));
                r.Is(r.Do(Fixer, "turn", "rival-4", "pay"), "Pay taken");
            }, Turned(Driver));

            Add("096-Q-1", "096", "CrewActQuery (Latch): the Pay and Mislead rows answer as Issue does: only-fixer, only-scout, crew-no-need's twin target-unknown, agent-busy, too-far for a driven agent, and yes in reach.", r =>
            {
                var g = r.G; r.Until(() => LookoutAtPost(g), T(330)); if (!r.Is(LookoutAtPost(g), "the lookout posted")) return;
                r.Eq(g.CrewActQuery("scout", "pay", "rival-4").code, "only-fixer", "Pay is the Fixer's");
                r.Eq(g.CrewActQuery("fixer", "mislead", "rival-4").code, "only-scout", "Mislead is the Scout's");
                r.Eq(g.CrewActQuery("scout", "mislead", "driver").code, "target-unknown", "a member of the crew only");
                r.To(Math.Max(g.Tick, r.SendTo(Scout, BesideLookout(g))));
                var m = g.CrewActQuery("scout", "mislead", "rival-4"); var o = new Order { Tick = g.Tick, Op = Scout, Do = "mislead", Target = "rival-4" };
                r.Is(m.ok, Iv($"in reach: yes ({m.code} {m.why})"));
                r.Is(g.Issue(o).ok, "and Issue agrees");
                var b = g.CrewActQuery("scout", "mislead", "rival-4"); r.Is(!b.ok && (b.code == "agent-busy" || b.code == "crew-walking"), Iv($"while the Mislead runs: {b.code}"));
            }, Turned(Driver));

            Add("096-Q-2", "096", "LentCarWatcher (Latch, the lent car's Go card's \"+1 T if seen\"): \"\" before the lend; once lent it is the body whose sighting the Go charges (lent.seen), and \"\" again once the car has left.", r =>
            {
                var g = r.G; r.Eq(g.LentCarWatcher(), "", "not lent: nobody");
                if (!LendAt(r)) { r.Is(false, "lent"); return; }
                var w = g.LentCarWatcher(); int t0 = g.Trail;
                r.Is(g.Issue(GoOn(g, "car:official")).ok, "Go on the lent car");
                r.Eq(g.LentSeenBy, w, "the read is who saw it leave");
                r.Eq(g.Trail - t0 >= (w.Length > 0 ? g.R.WitnessTrail : 0), true, Iv($"+{g.Trail - t0} T"));
                r.Eq(g.LentCarWatcher(), "", "gone: nobody");
            }, NoRivals(Turned(Driver)));
        }
    }
}
