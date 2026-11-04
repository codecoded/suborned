// Brief 096, engine 0.40.0 (Keel): boxed in and the kerbside stop (rulebook car.box, car.kerb; Motive's rules page, tests BOX-1..9, KERB-1..5;
// Sky 2026-11-04 "all. recommended"). Engine 0.41.0 (brief 096's rivals round): the rival car blocks on arrival by its own routine, so these
// scenarios drive our car to the service drive early and let the rival car come to the drive's mouth; 0.40.0's test seam Start.RivalBlock is
// retired. BOX-3 (the car drives off) is RIV-18 and RIV-22 now; BOX-6 (the far kerb) went with the park; BOX-8 (stand-boxed) is no longer
// reachable on the Handover with its one rival car (the box comes before any Drive could be refused it); the refusal stays the engine's.
//  BOX-1 way-out-derived     every stand's way out runs from its car to its exit lane's end on drivable ground; a site whose stand has none is rejected, named
//  BOX-2 go-and-drive-boxed  a car at the service drive's block point: Go car-boxed naming car:rivals, the Go card the same, Drive drive-boxed
//  BOX-4 disabled-boxes      shot down to disabled: still boxed; burning: still boxed; exploded: free
//  BOX-5 driving-nothing     the official car driving down the service drive (the service-drive stand's way out) boxes nothing
//  BOX-7 cell-car-boxes-official our car behind Popovic's in the yard boxes his car in; at the close he stays: closed-in, cause boxed
//  BOX-9 boxed-and-held      boxed and held at once: car-boxed first; the words name the holders too
//  KERB-1 drive-taken        our car on the drive first: Lukac pulls in at the kerb, 1 m clear of the drive and of our way out; nothing boxes us
//  KERB-2 never-boxes        200 seeds: the same stop every time, never boxing our car
//  KERB-3 take-and-carry     the Take at the kerb takes the bay's time; the carry to our car works; the pickup camera never sees the stop
//  KERB-4 driving-counts     our car still driving to the drive when he comes up: he pulls in at the kerb
//  KERB-5 generated-site     the derivation gives a legal point on the site, and a site with no legal point fails its load, named
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>Engine 0.41.0: run until the rival car stands at its block point (its own routine); false if it never does by then.</summary>
        static bool UntilBlocked(Run r, int byS = 300) => r.Until(() => r.G.CarSt == CrewCar.Blocking, T(byS)) && r.G.CarSt == CrewCar.Blocking;
        static Start NoRivals(Start? s = null) { s ??= new Start(); s.RivalsAtS = 900; return s; } // the crew never comes before the close: the car rules alone
        static Start TurnedNoRivals() { var s = NoRivals(); s.Turned.Add(Driver); return s; }
        /// <summary>Inputs with the site text changed once (a test site), loaded as a host would: the load's answer.</summary>
        static string LoadWith(Engine.Inputs i, string pattern, string replace)
        {
            var re = new Regex(pattern); if (re.Matches(i.Site).Count != 1) return Iv($"the site has '{pattern}' {re.Matches(i.Site).Count} times");
            try { Engine.FromTexts(i.Rulebook, i.PeopleRules, re.Replace(i.Site, replace), i.People, i.Provisional, i.Takedown, i.Rounds); return "loaded"; }
            catch (FormatException e) { return e.Message; }
        }
        /// <summary>The footprint of a standing car, grown by `grow`, meets the polygon / the way out (the scenario's own reading of car.kerb's test).</summary>
        static bool Meets(Game g, Game.StandingCar c, int grow, Pt[] poly) => CarGeo.PolysMeet(CarGeo.Rect(c.At, c.Heading, g.R.CarLenCm / 2 + grow, g.R.CarWCm / 2 + grow), poly);
        static bool OnWayOut(Game g, Game.StandingCar c, int grow, string stand) => CarGeo.OnWay(CarGeo.Rect(c.At, c.Heading, g.R.CarLenCm / 2 + grow, g.R.CarWCm / 2 + grow), g.WayOut(stand), g.S.GroundOf(g.R).Half);
        static int DriveAtStart(Run r, string stand = "service-drive") { r.To(Math.Max(r.G.Tick, r.SendTo(Tech, r.G.GetawaySeat))); r.Do(Tech, "board", "car"); r.Done(Tech); r.Do(Tech, "drive-car", stand); return r.Op(Tech).BusyEnd; }

        static void Box096()
        {
            Add("BOX-1", "096", "Motive BOX-1 (car.box): every stand's way out is worked out from the site at load: from its car to its exit lane's end, on drivable ground; a site whose stand's way out has no start on the drive grid is rejected, the stand and the rule named.", r =>
            {
                var g = r.G; r.Wait(1);
                foreach (var st in g.S.Stands)
                {
                    var w = g.WayOut(st.Id); bool onGrid = true;
                    for (int k = 1; k < w.Length; k++) { long n = Math.Max(1, G.Dist(w[k - 1], w[k]) / 25); for (long t = 0; t <= n; t++) onGrid &= g.S.Drive.Free(G.Lerp(w[k - 1], w[k], t, n)); }
                    r.Is(w.Length >= 2 && G.Within(w[0], st.CarCell, 100) && w[w.Length - 1] == st.Exit[st.Exit.Length - 1] && onGrid, Iv($"stand {st.Id}: its way out runs from its car to its exit's end on drivable ground ({w.Length} points)"));
                }
                var why = LoadWith(r.Inputs!, @"\[\s*\[\s*8600\s*,\s*-1500\s*\]\s*,\s*\[\s*8600\s*,\s*1500\s*\]\s*\]", "[[6400,-3000],[6400,0]]");
                r.Is(why.Contains("service-drive") && why.Contains("car.box"), Iv($"a stand whose exit starts inside the Office is rejected at load: {why}"));
            });

            Add("BOX-2", "096", "Motive BOX-2: our car on the service drive early: the rival car comes to the drive's mouth (its block point) and stops across it: Go is refused car-boxed naming car:rivals, the Go card reads the same, and Drive is refused drive-boxed.", r =>
            {
                var g = r.G; int there = DriveAtStart(r); r.To(there);
                if (!r.Is(g.CurStand.Id == "service-drive" && there < g.CrewEntry, Iv($"our car is on the service drive at {Report.Clock(there)}, before the rivals come"))) return;
                if (!r.Is(UntilBlocked(r), Iv($"the rival car stops across the drive's mouth at {Report.Clock(g.Tick)}"))) return;
                var a = g.Issue(Go(g)); r.Eq((a.ok, a.code, a.ids), (false, "car-boxed", "car:rivals"), Iv($"Go is refused car-boxed ({a.why})"));
                var q = g.GoQuery(); r.Eq((q.ok, q.code, q.ids), (false, "car-boxed", "car:rivals"), "the Go card reads the same");
                var d = g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "drive-car", Target = "square" }); r.Eq((d.ok, d.code, d.ids), (false, "drive-boxed", "car:rivals"), Iv($"Drive is refused drive-boxed ({d.why})"));
            });


            Add("BOX-4", "096", "Motive BOX-4: the Scout shoots the rival car across the drive. Disabled, it still stands and still boxes; burning, still; exploded, the way is free.", r =>
            {
                var g = r.G; r.To(DriveAtStart(r));
                var spot = g.S.Grid.NearestFree(new Pt(7800, -2600))!.Value; r.To(r.SendTo(Scout, spot)); if (!r.Is(UntilBlocked(r), "the rival car stops across the drive's mouth")) return;
                r.Fire(Scout, "car:rivals", true);
                r.Until(() => g.CarDisabled("car:rivals") || g.Over, T(240));
                r.Is(g.CarDisabled("car:rivals") && g.GetawayBoxers().Contains("car:rivals"), Iv($"disabled at {Report.Clock(g.Tick)}: still boxed"));
                r.Until(() => g.CarBurning("car:rivals") || g.Over, T(240));
                r.Is(g.CarBurning("car:rivals") && g.GetawayBoxers().Contains("car:rivals"), Iv($"burning at {Report.Clock(g.Tick)}: still boxed"));
                r.Until(() => g.CarOf("car:rivals")?.Blown == true || g.Over, T(30));
                r.Is(g.CarOf("car:rivals")?.Blown == true && g.GetawayBoxers().Count == 0 && g.GoQuery().code != "car-boxed", Iv($"exploded at {Report.Clock(g.Tick)}: the way is free"));
            });

            Add("BOX-5", "096", "Motive BOX-5: a car driving across a way out boxes nothing: Popovic's car drives down the service drive (the service-drive stand's way out) while our car waits at the square; the Drive there is never refused stand-boxed while it moves, and refused by nothing once it is parked in the bay.", r =>
            {
                var g = r.G; r.To(r.SendTo(Tech, g.GetawaySeat)); r.Do(Tech, "board", "car"); r.Done(Tech);
                r.Until(() => g.CarInBayTick > 0, T(420));
                int across = 0, refused = 0; Pt last = g.CarPos();
                while (!g.Over && g.Tick < g.CarInBayTick)
                {
                    g.Step(); var at = g.CarPos(); var h = at - last; last = at; if (h.X == 0 && h.Y == 0) continue;
                    if (CarGeo.OnWay(CarGeo.Rect(at, h, g.R.CarLenCm / 2, g.R.CarWCm / 2), g.WayOut("service-drive"), g.S.GroundOf(g.R).Half)) { across++; if (g.StandBoxers("service-drive").Count > 0) refused++; }
                }
                r.Is(across > 0 && refused == 0, Iv($"{across} ticks his moving car crossed the way out; boxed in none of them"));
                r.Wait(1); r.Eq(g.StandBoxers("service-drive").Count, 0, "parked in the bay, he is off the stand's way out");
            }, TurnedNoRivals());


            Add("BOX-7", "096", "Motive BOX-7: our car drives round behind Popovic's in the service yard: his way out of the bay is boxed by car:getaway. Nobody takes Vale; at the close the turned driver would give up and drive on, but he is boxed in: he stays, and the verdict is closed-in, cause boxed, naming our car.", r =>
            {
                var g = r.G; r.Until(() => g.CarInBayTick > 0 && g.Tick >= g.CarInBayTick, T(480));
                r.Is(g.PickupService && !g.CarAtKerb, "Popovic's car is parked in the service bay with Vale aboard");
                r.Eq(g.OfficialBoxers().Count, 0, "free to go before our car comes");
                r.To(r.SendTo(Tech, g.GetawaySeat)); r.To(DriveAtStart(r));
                r.Eq(string.Join(",", g.OfficialBoxers()), "car:getaway", "our car behind him boxes his car in");
                r.To(T(g.ClockOver) + 1);
                r.Eq((g.FailCode, g.Cause?.Why), ("closed-in", "boxed"), Iv($"at the close: closed-in, cause boxed ({g.FailText})"));
                r.Is(g.Cause?.Culprits.FirstOrDefault()?.Id == "getaway-car", "the culprit is our car");
            }, TurnedNoRivals());


            Add("BOX-9", "096", "Motive BOX-9: boxed and held at once (Vale aboard on the service drive before the rival car enters: the gate holds it, and the rival car stands across the drive): Go is refused car-boxed, the physical fact first, and its words name the holders too.", r =>
            {
                var g = r.G; YardEarly(r); UntilBlocked(r); // engine 0.41.0: the rival car comes to the drive's mouth on its own
                if (!r.Is(g.Vale == ValeAt.Car && g.CarHolders().Count > 0 && g.GetawayBoxers().Count > 0, Iv($"Vale aboard at {Report.Clock(g.Tick)}, the car held ({string.Join(",", g.CarHolders())}) and boxed ({string.Join(",", g.GetawayBoxers())})"))) return;
                var a = g.Issue(Go(g));
                r.Is(!a.ok && a.code == "car-boxed" && a.ids.StartsWith("car:rivals", StringComparison.Ordinal) && a.why.Contains("held"), Iv($"car-boxed before car-held: {a.ids}: {a.why}"));
                r.Eq(g.GoQuery().code, "car-boxed", "the Go card reads the same");
            }, Turned(Inez));

            Kerb096();
        }

        static void Kerb096()
        {
            Add("KERB-1", "096", "Motive KERB-1 (car.kerb): our car on the service drive first: Lukac pulls in at the kerb short of it (car.kerbside), nose the way he drove; his car, grown by car.kerb.clear, meets neither the drive nor our way out; nothing boxes our car.", r =>
            {
                var g = r.G; r.To(DriveAtStart(r));
                r.Until(() => g.CarInBayTick > 0 && g.Tick >= g.CarInBayTick, T(480));
                r.Is(g.CarAtKerb && g.Log.Exists(l => l.Code == "car.kerbside"), "he pulls in at the kerb (car.kerbside)");
                var off = g.StandingCars().Find(c => c.Id == "car:official"); var dr = g.S.Drives[0];
                r.Is(!Meets(g, off, g.R.KerbClearCm, dr.Poly) && !OnWayOut(g, off, g.R.KerbClearCm, "service-drive"), Iv($"at ({off.At.X / 100.0:0.00}, {off.At.Y / 100.0:0.00}): 1 m clear of the drive and of our way out"));
                r.Is(g.S.Drive.SurfaceAt(off.At) == 'r', "on the carriageway, at the kerb");
                r.Eq(g.GetawayBoxers().Count, 0, "his car never boxes ours");
                r.Is(g.PickupService && g.TakeInCar, "the Take there is in the car, as at the bay");
            }, TurnedNoRivals());

            Add("KERB-2", "096", "Motive KERB-2, 200 seeds: on every seed where the drive is taken, Lukac stops at the same kerbside point and never boxes our car.", r =>
            {
                var g = r.G; r.Wait(1); var stops = new HashSet<(int, int)>(); int ok = 0; var bad = new List<string>();
                for (int seed = 1; seed <= 200; seed++)
                {
                    var st = NoCrew(TurnedNoRivals()); st.Seed = seed; st.RivalsAtS = null; // engine 0.41.0: no crew (the rival car would block the drive's mouth: KERB-2 is about Lukac's stop)
                    var s = new Run(Engine.New(r.Inputs!, st)) { Inputs = r.Inputs };
                    s.To(DriveAtStart(s)); s.Until(() => s.G.CarInBayTick > 0 && s.G.Tick >= s.G.CarInBayTick, T(480));
                    if (s.G.CarAtKerb && s.G.GetawayBoxers().Count == 0) { ok++; stops.Add((s.G.CarPos().X, s.G.CarPos().Y)); } else if (bad.Count < 4) bad.Add(Iv($"seed {seed}: kerb {s.G.CarAtKerb}, boxed by {string.Join(",", s.G.GetawayBoxers())}, {s.G.FailCode}"));
                }
                r.Eq(ok, 200, Iv($"he pulled in at the kerb without boxing our car{(bad.Count > 0 ? ": " + string.Join("; ", bad) : "")}"));
                r.Eq(stops.Count, 1, Iv($"at one point on every seed ({string.Join(" ", stops.Select(p => Iv($"({p.Item1}, {p.Item2})")))})"));
            }, NoCrew());

            Add("KERB-3", "096", "Motive KERB-3: the Take at the kerb takes the same time as at the bay; the Enforcer carries Vale to our car and boards; the pickup camera never sees the stop.", r =>
            {
                var g = r.G; r.To(DriveAtStart(r));
                r.Until(() => g.CarInBayTick > 0 && g.Tick >= g.CarInBayTick, T(480));
                r.Is(g.CarAtKerb && !g.CameraWatching("pickup", g.CarPos(), false), "at the kerb, out of the pickup camera's sight");
                r.To(Math.Max(g.Tick, r.SendTo(Enf, g.S.Grid.NearestFree(g.CarPos() + new Pt(-250, 0)) ?? g.CarPos())));
                r.Do(Enf, "take"); int took = r.Op(Enf).BusyEnd - g.Tick;
                var bay = new Run(Engine.New(r.Inputs!, TurnedNoRivals())) { Inputs = r.Inputs }; bay.Until(() => bay.G.CarInBayTick > 0 && bay.G.Tick >= bay.G.CarInBayTick, T(480));
                bay.To(Math.Max(bay.G.Tick, bay.SendTo(Enf, bay.G.S.Stand("d:Service lane")))); bay.Do(Enf, "take"); int atBay = bay.Op(Enf).BusyEnd - bay.G.Tick;
                r.Eq(took, atBay, Iv($"the Take at the kerb takes the bay's {atBay / 10.0:0.0} s"));
                r.Done(Enf); r.Eq(g.Vale, ValeAt.WithEnforcer, "Vale is with the Enforcer");
                r.To(r.SendTo(Enf, g.GetawaySeat)); r.Do(Enf, "board", "car"); r.Done(Enf);
                r.Eq(g.Vale, ValeAt.Car, "carried to our car and aboard");
            }, TurnedNoRivals());

            Add("KERB-4", "096", "Motive KERB-4: our car still driving to the service drive when Lukac comes up to the kerbside stop: the drive counts as taken and he pulls in at the kerb.", r =>
            {
                var g = r.G; r.To(r.SendTo(Tech, g.GetawaySeat)); r.Do(Tech, "board", "car"); r.Done(Tech);
                int valeIn = T(g.ClockValeAtCar) + T(g.R.ValeInS), drive = g.DriveTicks("service-drive");
                r.To(valeIn - drive / 2); r.Do(Tech, "drive-car", "service-drive");
                r.Until(() => g.CarAtKerb || g.Tick > valeIn + T(30), T(120));
                var at = g.Log.Find(l => l.Code == "car.kerbside");
                r.Is(g.CarAtKerb && at.Code != null && g.GetawayDriving, Iv($"he pulls in at the kerb at {Report.Clock(at.Tick)} while our car is still driving there"));
            }, TurnedNoRivals());

            Add("KERB-5", "096", "Motive KERB-5: the kerbside stop is worked out from a site's data: on the Handover's, a legal point (on the carriageway, 1 m clear of the drive and of the service-drive stand's way out, at the kerb); on a test site whose drive runs along the kerb, so no point is clear, the load fails naming car.kerb.", r =>
            {
                var g = r.G; r.Wait(1);
                var k = g.S.GroundOf(g.R).Kerb["service-drive"]; var c = new Game.StandingCar("car:official", k.At, k.Heading);
                r.Is(g.S.Drive.SurfaceAt(k.At) == 'r' && !Meets(g, c, g.R.KerbClearCm, g.S.Drives[0].Poly) && !OnWayOut(g, c, g.R.KerbClearCm, "service-drive"), Iv($"the site's stop ({k.At.X / 100.0:0.00}, {k.At.Y / 100.0:0.00}) is legal"));
                var why = LoadWith(r.Inputs!, @"\[\s*\[\s*6700\s*,\s*-1650\s*\]\s*,\s*\[\s*8000\s*,\s*-1650\s*\]\s*,\s*\[\s*8000\s*,\s*-1350\s*\]\s*,\s*\[\s*6700\s*,\s*-1350\s*\]\s*\]", "[[6700,-6000],[8000,-6000],[8000,-1350],[6700,-1350]]");
                r.Is(why.Contains("car.kerb"), Iv($"a drive along the whole kerb leaves no stop: the load fails: {why}"));
            });
        }
    }
}
