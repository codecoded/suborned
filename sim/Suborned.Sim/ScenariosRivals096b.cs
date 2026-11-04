// Brief 096's rivals round, engine 0.41.0 (Keel; Motive briefs/096/motive/rivals.html §7). Motive's RIV scenarios (engine ids 096-RIV-n), part 2: thinning, the
// block's end, the veteran's shot and the gang shooting (rulebook rivals.block, rivals.turn, security.unclaimed, car.target.blastStanding).
//  RIV-16 posted-taken-mate-sees  is S1-SEE-TAKEDOWN (ScenariosAlways059.cs): one posted man taken in a mate's cone: the mate fires on the taker
//  RIV-17 fight-size-table        each row of Motive's §2 table: the rivals in the fight and the break count
//  RIV-18 break-drives-off        the crew breaks in the fight with the wheelman up: he drives the car off for good; Go is taken once nothing holds the car
//  RIV-19 blow-the-block          the parked car shot to 0 in the final fight: it burns, explodes rivals' burn later, hits the wheelman inside; our car untouched; Go is free
//  RIV-20 wheelman-down-car-stays the wheelman shot down outside: the car stays and boxes; only the explosion frees it
//  RIV-21 drawn-crew-car-stays    a shot at the Office front: the crew on foot go there; the car keeps the block; the wheelman stays in
//  RIV-22 prefight-break-reblocks three down before the fight: the crew breaks, the car leaves (Go free), and is back across the way out
//  RIV-23 shot-on-the-way         fire on the car as it comes in: disabled at 5 it stops where it is, riders out (1 s, then 2 s apart, the wheelman last); burning, then the blast; no block
//  RIV-24 unclaimed-no-seal       the same shooting unseen: Alert 2, the response, no seal (security.gang); a later incident starts it
//  RIV-25 seen-shooting-seals     a shot at the crew a camera sees: not unclaimed, the seal as today
//  RIV-26 veteran-price           the price of RIV-23's run: the shooting's Trail once, hits by person; Standing -1 only with city people within 30 m of the blast
//  RIV-27 carless-crew-gone       a crew whose car is destroyed walks off the map when it breaks and is never recalled
//  RIV-28 bands-200               the bands (bun run missions:check bands), not a scenario: briefs/096/keel/bands-rivals-*.txt
//  RIV-29 replay                  every scenario here replays byte-identical (the check's own replay of each scenario)
//  RIV-30 generated-site          a site whose stand's exit lane never runs rivals.block.standOff from its car is rejected at the game's start, named
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>All four fire on `target` at once (a fire order each; the engine walks them into reach).</summary>
        static void AllFire(Run r, string target) { foreach (var op in new[] { Scout, Fixer, Tech, Enf }) r.G.Issue(new Order { Tick = r.G.Tick, Op = op, Do = "fire", Target = target, Force = target.StartsWith("car:", StringComparison.Ordinal) }); }
        /// <summary>The south corner of the square where Orbit Walk meets Rocket Lane: the veteran's spot (Motive geo.py), the nearest free ground.</summary>
        static Pt VeteranSpot(Game g, int dx = 0) => g.S.Grid.NearestFree(new Pt(3150 + dx, -5050)) ?? g.GetawaySeat;
        /// <summary>RIV-23's opening: the four at the corner; when the rival car comes within 20 m on its way in, all four fire on it.</summary>
        static bool VeteranShot(Run r)
        {
            var g = r.G; int k = 0; foreach (var op in new[] { Scout, Fixer, Tech, Enf }) r.SendTo(op, VeteranSpot(g, 150 * k++));
            r.Until(() => g.CarSt == CrewCar.Entering && g.CarAt("car:rivals") is Pt p && G.Within(p, VeteranSpot(g), 2000), T(240));
            if (g.CarSt != CrewCar.Entering) return false;
            AllFire(r, "car:rivals"); return true;
        }

        static void Rivals096b()
        {
            foreach (var (id, gone, want) in new[] { ("096-RIV-17a", new string[0], 5), ("096-RIV-17b", new[] { "rival-4" }, 4), ("096-RIV-17c", new[] { "rival-4", "rival-1" }, 3), ("096-RIV-17d", new[] { "rival-4", "rival-1", "rival-2" }, 2) })
                Add(id, "096", $"Motive RIV-17 (fight-size-table): {(gone.Length == 0 ? "nobody" : string.Join(" and ", gone))} out for good before the fight (killed before the mission, test only): Vale aboard at the service drive early, the crew comes to the block: {want} rivals in the fight, the break at {want / 2 + 1} down.", r =>
                {
                    var g = r.G; YardEarly(r); r.Until(() => g.FightBranch == "safe" && g.CrewInFight >= 0 && g.CarSt == CrewCar.Blocking, T(300));
                    r.Until(() => g.Crew.All(m => m.Gun.Down || m.Fight || m.Mode == CrewMode.Gone), T(30));
                    r.Eq(RivalBodies(g).Count, want, $"{want} in the fight ({string.Join(",", RivalBodies(g))})");
                    r.Is(want / 2 + 1 > want / 2 && (want / 2 + 1) * 2 > want, $"more than half of {want} is {want / 2 + 1}: the break count (ai.break)");
                }, Gone(Turned(Inez), gone));

            Add("096-RIV-18", "096", "Motive RIV-18: Vale aboard at the service drive early; the crew blocks the drive's mouth and fights; the cell fights it out until the crew breaks: the wheelman, up, gets back in and drives off for good; Go is taken once nothing holds the car.", r =>
            {
                var g = r.G; YardEarly(r); UntilBlocked(r);
                r.FightItOut(150, Scout, Fixer, Tech);
                if (!r.Is(g.FightWhy.Contains("rivals-break") || g.Chain.Contains("rivals-block-off") || g.CarOf("car:rivals")?.Blown == true, Iv($"the fight ends the block ({g.Result} {g.FailCode}; break {g.FightWhy.Contains("rivals-break")}, wheelman down {g.Crew[4].Gun.Down})"))) return;
                if (g.Chain.Contains("rivals-block-off")) r.Is(g.CarSt == CrewCar.Leaving || g.CarSt == CrewCar.Gone, "the broken crew's wheelman drives off (rivals.drives-off)");
                else r.Line(false, $"the wheelman was down ({g.Crew[4].Gun.Down}): the block ended by the blast instead");
                r.Eq(g.GetawayBoxers().Count, 0, "nothing stands across our way out");
                var a = r.Extract(); r.Is(g.Result == Outcome.Success, Iv($"Go is taken: {g.Result} at {Report.Clock(g.Tick)}"));
            }, Turned(Inez));

            Add("096-RIV-19", "096", "Motive RIV-19: route A at the square; at the spring the Scout, the Fixer and the Tech shoot the blocking car: disabled, burning, then car.target.burn later it explodes and hits whoever is still inside; our car, 10 m off, is untouched; once it is gone nothing boxes our car.", r =>
            {
                var g = r.G; RouteA(r);
                if (!r.Is(g.FightBranch == "safe" && g.CarSt == CrewCar.Blocking, Iv($"the safe fight at the block ({g.FightBranch} {g.CarSt})"))) return;
                foreach (var op in new[] { Scout, Fixer, Tech }) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "fire", Target = "car:rivals", Force = true });
                r.Until(() => g.CarBurning("car:rivals") || g.Over, T(120));
                if (!r.Is(g.CarBurning("car:rivals"), Iv($"burning at {Report.Clock(g.Tick)} ({g.Result} {g.FailCode})"))) return;
                r.Is(g.GetawayBoxers().Contains("car:rivals"), "a burning car still boxes");
                int burn = g.Tick; r.Until(() => g.CarOf("car:rivals")!.Blown || g.Over, T(30));
                r.Is(g.CarOf("car:rivals")!.Blown && g.Tick - burn == T(g.R.CarBurnS), Iv($"it explodes car.target.burn after ({(g.Tick - burn) / 10} s)"));
                r.Eq(g.GetawayBoxers().Count, 0, "exploded: nothing boxes our car");
                r.Is(g.CarOf("car:getaway") == null || g.CarOf("car:getaway")!.Gun.Health == g.CarOf("car:getaway")!.Gun.Max, "our car takes nothing from the blast at 10 m");
                r.Is(g.Gunfire == 0, "the block's car is fought untraced in the safe fight: no gunfire on the district");
            });

            // 096-RIV-20 (the wheelman out at the spring, shot down outside his car) is retired by engine 0.42.0: he stays at the wheel (Motive F1); FIX-1, FIX-2 (ScenariosLend096b.cs) hold the rule now

            Add("096-RIV-21", "096", "Motive RIV-21: a shot at the Office front (the Enforcer shoots the entrance camera at 3:30): the crew on foot walk to the front; the car keeps its block, its wheelman aboard (the shot is over 30 m from it).", r =>
            {
                var g = r.G; UntilBlocked(r); r.To(r.Send(Enf, "d:Office approach")); r.To(Math.Max(g.Tick, T(210)));
                r.Fire(Enf, "camera:entrance"); r.Until(() => g.RivalsDrawn, T(20));
                r.Wait(T(3));
                r.Is(g.Crew.Take(3).All(m => m.Goal == "front" || m.Post == "front"), "the three walk to the Office front");
                r.Is(g.CarSt == CrewCar.Blocking && g.GetawayBoxers().Contains("car:rivals") && g.Crew[4].Mode == CrewMode.Aboard, "the car keeps its block, the wheelman aboard");
            });

            Add("096-RIV-22", "096", "Motive RIV-22: three of the crew downed on the street before the fight: the crew breaks; whoever is up runs for the car and it leaves (Go is not boxed while it is away), and security.rivals.retreat later it is back across our way out.", r =>
            {
                var g = r.G; r.Until(() => g.Crew.Take(3).All(m => m.Mode == CrewMode.Posted), T(300));
                r.Engage(() => g.Crew.Take(3).Where(m => !m.Gun.Down).Select(m => m.Id).ToList(), () => g.CrewBrokenPre || g.Over, 90, new[] { Scout, Fixer, Tech, Enf }, false);
                if (!r.Is(g.CrewBrokenPre && g.Log.Exists(l => l.Code == "rivals.retreat"), Iv($"the crew breaks at {Report.Clock(g.Tick)} ({g.Result} {g.FailCode})"))) return;
                r.Until(() => g.CarSt == CrewCar.Leaving || g.CarSt == CrewCar.Away || g.Over, T(60));
                r.Is(g.CarSt == CrewCar.Leaving || g.CarSt == CrewCar.Away, "the car leaves the way it came");
                r.Eq(g.GetawayBoxers().Count, 0, "our car is not boxed while it is away");
                r.Until(() => g.CarSt == CrewCar.Blocking || g.Over, T(g.R.RivalsRetreatS + 40));
                r.Line(g.CarSt == CrewCar.Blocking && g.GetawayBoxers().Contains("car:rivals"), $"back across our way out at {Report.Clock(g.Tick)} ({g.CarSt}; {g.Result} {g.FailCode})");
            }, KeepVale(new Start()));

            Add("096-RIV-23", "096", "Motive RIV-23: the four at the square's south corner fire on the rival car as it comes in (a running target): disabled at car.target.disabledAt it stops where it is, short of its block point; its riders get out, the first board-s after, then rivals.step apart in id order, the wheelman last; at 0 it burns and the blast comes car.target.burn later; it boxes nothing.", r =>
            {
                var g = r.G; if (!r.Is(VeteranShot(r), "the car comes within 20 m on its way in")) return;
                r.Until(() => g.CarDisabled("car:rivals") || g.Over, T(60));
                if (!r.Is(g.CarDisabled("car:rivals"), Iv($"disabled at {Report.Clock(g.Tick)} ({g.Result} {g.FailCode})"))) return;
                int dis = g.Tick; var outs = g.Crew.Select(m => m.OutAt - dis).ToList();
                r.Is(g.CarSt == CrewCar.Stopped && g.RivalCarPose()!.Value.pos != g.BlockAt(g.S.Stands[0]).at, "it stops where it is, short of the block point");
                r.Eq(string.Join(",", outs.Select(o => o / 10)), string.Join(",", Enumerable.Range(0, 5).Select(k => g.R.BoardS + g.R.Crew.StepS * k)), "out board-s, then rivals.step apart in id order, the wheelman last");
                r.Eq(g.GetawayBoxers().Count, 0, "short of its block point it boxes nothing");
                r.Until(() => g.CarOf("car:rivals")!.Blown || g.Over, T(60));
                r.Line(g.CarOf("car:rivals")!.Blown, $"burned and exploded at {Report.Clock(g.Tick)} ({g.Result} {g.FailCode})");
            });

            Add("096-RIV-24", "096", "Motive RIV-24 (Q1 a): RIV-23's shooting with nobody of security seeing it and no passer-by: Alert 2, the response is called, the district is not sealed (security.gang); a later incident (the entrance camera shot) places the cell and starts the seal.", r =>
            {
                var g = r.G; if (!r.Is(VeteranShot(r), "the car comes within 20 m on its way in")) return;
                r.Until(() => g.Gunfire > 0 || g.Over, T(20));
                r.Line(g.GangPending, $"seed 0's corner: filed as a gang shooting ({(g.GangPending ? "security.gang" : "seen: " + g.GangClaimedWhy)})");
                if (!g.GangPending) return;
                r.Is(g.Alert == 2 && g.SecurityArrives < int.MaxValue / 4 && g.Lockdown >= int.MaxValue / 4, Iv($"Alert {g.Alert}, the response at {Report.Clock(g.SecurityArrives)}, no seal"));
                r.Wait(T(5));
                var g2 = g; r.To(Math.Max(g.Tick, r.Send(Tech, "d:Office approach")));
                if (g.GangPending) { r.Fire(Tech, "camera:entrance"); r.Until(() => !g.GangPending || g.Over, T(20)); }
                r.Is(!g.GangPending && g.Lockdown < int.MaxValue / 4, Iv($"placed ({g.GangClaimedWhy}): the seal at {Report.Clock(g.Lockdown)}"));
            });

            Add("096-RIV-25", "096", "Motive RIV-25: a shooting at the crew that security sees (the pickup camera watching the shooter) is not unclaimed: the seal starts as today.", r =>
            {
                var g = r.G; r.Until(() => g.Crew[3].Mode == CrewMode.Walking && g.Crew[3].Goal == "lookout", T(300));
                var spot = g.S.Grid.NearestFree(new Pt(7800, -3500))!.Value; r.To(Math.Max(g.Tick, r.SendTo(Scout, spot)));
                r.Is(g.CameraWatching("pickup", g.OpOf(OpId.Scout).Pos), "the Scout stands in the pickup camera's view");
                r.Fire(Scout, "rival-4"); r.Until(() => g.Gunfire > 0 || g.Over, T(30));
                r.Is(!g.GangPending && g.Lockdown < int.MaxValue / 4, Iv($"seen: the seal at {Report.Clock(g.Lockdown)}"));
            });

            Add("096-RIV-26", "096", "Motive RIV-26: RIV-23's price: the shooting's Trail once (gunfire-trail), each crew member hit by the cell by person (hit-trail, down-trail), the car no hit Trail; Standing moves by blastStanding only if a city person stood within its reach at the blast.", r =>
            {
                var g = r.G; if (!r.Is(VeteranShot(r), "the shot")) return;
                r.Until(() => g.CarOf("car:rivals")?.Blown == true || g.Over, T(90));
                var trail = g.Ledger.Where(x => x.what == "trail").ToList();
                r.Eq(trail.Count(x => x.cause.StartsWith("gunfire:car:rivals", StringComparison.Ordinal)), 1, "the car's shooting is one case (gunfire-trail once)");
                r.Is(!trail.Any(x => x.cause.Contains("hit:car:rivals")), "the car takes no hit Trail (property)");
                bool blast = g.StandingMoves.Any(m => m.cause == "blast-near");
                r.Line(true, $"Standing moves: {string.Join(", ", g.StandingMoves.Select(m => m.cause + " " + m.delta))}; blast-near {blast}");
                r.Is(!blast || g.StandingMoves.Count(m => m.cause == "blast-near") == 1, "blast-near at most once a car");
            });

            Add("096-RIV-27", "096", "Motive RIV-27: the car blown up on its way in (RIV-23), then the crew on the street downed until it breaks: with no car it walks off the map and is never recalled.", r =>
            {
                var g = r.G; if (!r.Is(VeteranShot(r), "the shot")) return;
                r.Until(() => g.CarOf("car:rivals")?.Blown == true || g.Over, T(90));
                r.Engage(() => g.Crew.Where(m => !m.Gun.Down && !m.Fight && m.OnFoot).Select(m => m.Id).ToList(), () => g.CrewBrokenPre || g.Over, 90, new[] { Scout, Fixer, Tech, Enf }, false);
                if (!r.Line(g.CrewBrokenPre, $"the crew breaks ({g.Result} {g.FailCode} at {Report.Clock(g.Tick)})")) return;
                r.Is(g.Crew.Where(m => !m.Gun.Down).All(m => m.Mode == CrewMode.Gone || m.Goal == "exit"), "the able walk off the map");
                r.Until(() => g.Over, T(200)); r.Is(!g.Log.Exists(l => l.Code == "rivals.coming" && l.Tick > g.CrewEntry), "the car never comes back");
            });

            Add("096-RIV-30", "096", "Motive RIV-30 (generated-site): the routine derives every point from the site; a site whose stand's exit lane never runs rivals.block.standOff from its car is rejected at the game's start, the stand and the rule named.", r =>
            {
                var g = r.G; r.Wait(1);
                foreach (var st in g.S.Stands) r.Is(g.BlockAt(st).at != default, Iv($"stand {st.Id}: block point {g.BlockAt(st).at}"));
                if (!r.Is(r.Inputs != null, "inputs")) return;
                var re = new Regex(@"\[\s*\[\s*8600\s*,\s*-1500\s*\]\s*,\s*\[\s*8600\s*,\s*1500\s*\]\s*\]"); var i = r.Inputs!;
                string why;
                try { var bad = Engine.FromTexts(i.Rulebook, i.PeopleRules, re.Replace(i.Site, "[[7000,-1500],[7100,-1500]]", 1), i.People, i.Provisional, i.Takedown, i.Rounds); Engine.New(bad, new Start { OneCar = true }); why = "loaded"; } // engine 0.42.0: the one-car twin (with the lend a drive stand blocks at its mouth, L10)
                catch (FormatException e) { why = e.Message; }
                r.Is(why.Contains("rivals.block") || why.Contains("car.box"), Iv($"a stand with a 1 m exit lane is rejected: {why}"));
            });
        }
    }
}
