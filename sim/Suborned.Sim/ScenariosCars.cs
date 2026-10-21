// Cars are targets (Sky 2026-10-21, tuning.md §9.7; Motive tuning.md §10.7; engine 0.16.0; GameCars.cs). The cases Motive asks for in
// §10.8: a car shot (the price), disabled, burned and exploded with people in the ring (the cell included), Vale in the official car, the
// getaway car lost; and the lab's ×healthScale. Ids are CAR-* (not S-prefixed: Add keeps S1–S28 apart).
using System;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>A free spot `minCm`–`maxCm` from `at` with a line to it at eye height, nearest `from` (the walk grid's search).</summary>
        static Pt? SpotFor(Run r, Pt from, Pt at, int minCm, int maxCm) =>
            r.G.S.Grid.Nearest(from, q => G.D2(q, at) >= (long)minCm * minCm && G.D2(q, at) <= (long)maxCm * maxCm && r.G.S.Sight(q, at, r.G.R.Eye), 12000);

        static void CarCases()
        {
            Add("CAR-SHOT", "—", "§10.7: a car is a target; the shot is a shooting (gunfire-trail once, Alert ≥ 2, the response); the car takes no hit or down Trail and no Standing", r =>
            {
                var g = r.G; var car = g.CarAt("car:official")!.Value;
                var spot = SpotFor(r, r.Op(Scout).Pos, car, 800, 1200); if (!r.Is(spot.HasValue, "a spot 8–12 m from the official car with a line")) return;
                r.To(r.SendTo(Scout, spot!.Value));
                r.Do(Scout, "draw"); r.Wait(T(2)); // drawing in sight of people has its own price (witnessed weapon-drawn): measure the shot alone
                int trail = g.Trail, standing = g.Standing;
                var pv = g.PreviewFire(OpId.Scout, "car:official");
                r.Eq((pv.Refused, pv.TrailHit, pv.TrailDown), ("", 0, 0), "the chip: a target, no hit or down Trail");
                r.Eq(g.GoingLoud(new Order { Tick = g.Tick, Op = Scout, Do = "fire", Target = "car:official" }).Fails, "", "Vale is inside the Office: no fail on the card");
                r.Is(r.Fire(Scout, "car:official"), "fire on the official car taken (no Ctrl needed: the host offers it in the ring or on Ctrl)");
                r.Is(r.Until(() => g.CarOf("car:official")!.Gun.Hit, T(15)), "the car is hit");
                g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "cease" });
                r.Is(g.Alert >= 2, $"gunfire: Alert ≥ 2 ({g.Alert})");
                r.Is(g.Log.Exists(l => l.Code == "security.called"), "the response is called");
                r.Eq(g.Trail - trail, g.R.GunfireTrail, "gunfire-trail once, no hit or down Trail");
                r.Eq(g.Standing, standing, "no Standing step");
                r.Is(g.Log.Exists(l => l.Code == "car.hit"), "logged car.hit");
            });

            foreach (var lab in new[] { false, true })
            {
                string sfx = lab ? "-LAB" : "";
                Add("CAR-BLAST" + sfx, "—", "§10.7: the official car shot to disabled (it stops, the driver gets out), burning at 0, exploding car.target.burn later: everyone in the ring with a line takes the blast, the cell included (no Trail for an operative), its cover points are gone" + (lab ? "; the lab ×healthScale" : ""), r =>
                {
                    var g = r.G; var car = g.CarAt("car:official")!.Value; int k = g.HealthScale;
                    var near = SpotFor(r, r.Op(Tech).Pos, car, 250, 400); var far = SpotFor(r, r.Op(Enf).Pos, car, 1000, 1300);
                    if (!r.Is(near.HasValue && far.HasValue, "a spot in the ring and one 10–13 m out, both with a line")) return;
                    r.To(Math.Max(r.SendTo(Tech, near!.Value), r.SendTo(Enf, far!.Value)));
                    r.Do(Enf, "draw"); r.Wait(T(1));
                    r.Is(r.Fire(Enf, "car:official"), "fire on the official car taken");
                    var c = g.CarOf("car:official")!;
                    r.Eq(c.Gun.Max, g.R.HealthCar * k, $"its health car.target.health ×{k}");
                    r.Is(r.Until(() => c.DisabledAt >= 0, T(30)), "it is disabled");
                    r.Is(c.Gun.Health <= g.R.CarDisabledAt * k, $"at or below disabledAt ({c.Gun.Health} ≤ {g.R.CarDisabledAt * k})");
                    var stopped = g.CarPos(); r.Is(g.Log.Exists(l => l.Code == "car.disabled"), "logged car.disabled");
                    r.Is(r.Until(() => c.BurningAt >= 0, T(30)), "shot on to 0, it burns");
                    r.Wait(1); r.Is(!g.PosOf("car:official").HasValue && r.Op(Enf).Gun.Order.Length == 0, "burning, it is no target: the fire order ends");
                    r.Eq(c.BlastAt - c.BurningAt, T(g.R.CarBurnS), "it blows car.target.burn after it caught fire");
                    var tech = r.Op(Tech); int techHp = tech.Gun.Health, trail = g.Trail;
                    bool coverBefore = g.CoverStanding("car/official");
                    r.Is(r.Until(() => c.Blown, T(g.R.CarBurnS + 1)), "it explodes");
                    r.Eq(g.CarPos(), stopped, "it never moved after it was disabled");
                    r.Eq(techHp - tech.Gun.Health, Math.Min(techHp, g.R.CarBlastDamage * k), $"the Tech in the ring takes car.target.blast.damage ×{k}");
                    r.Is(!g.Log.Exists(l => l.Code == "car.blast-hits" && l.Text.Contains("enforcer")), "the Enforcer 10 m out is outside the ring");
                    var atBlast = g.Ledger.Where(e => e.tick == c.BlastAt && e.what == "trail").Select(e => (e.cause, e.delta)).ToList();
                    r.Is(!atBlast.Exists(e => e.cause.Contains("tech")), "the Tech's blast hit costs no Trail");
                    var hit = g.Log.Where(l => l.Code == "car.blast-hits" && !l.Text.Contains("tech")).Select(l => l.Text.Substring(15).TrimEnd('.')).ToList();
                    foreach (var who in hit) r.Is(atBlast.Contains(("hit:" + who, g.R.HitTrail)) || g.GunOf(who)!.Hit && !atBlast.Exists(e => e.cause == "hit:" + who), $"{who} hit by the blast: hit-trail (the cell's, as a shot's)");
                    r.Is(hit.Count > 0, $"someone besides the cell stood in the ring ({string.Join(", ", hit)})");
                    if (hit.Contains(Driver)) r.Is(g.Driver.Down && atBlast.Contains(("down:" + Driver, g.R.DownTrail)) && g.BodyOf(g.Driver) is Body db && db.Dead, "the driver, out beside it, dies: down-trail and a body");
                    r.Is(coverBefore && !g.CoverStanding("car/official"), "its cover points are gone");
                    r.Is(r.Shots().TrueForAll(s => !s.target.StartsWith("car:", StringComparison.Ordinal) || new[] { Scout, Fixer, Tech, Enf }.Contains(s.by)), "only the cell ever fired on a car (hostiles never choose one)");
                }, lab ? LabStart() : null);
            }

            Add("CAR-VALE", "—", "§10.7: Vale at the official car: the card's fail is vale-dead (always the hold); disabled, Vale gets out beside it; the blast kills him: vale-dead", r =>
            {
                var g = r.G; var car = g.CarAt("car:official")!.Value;
                var far = SpotFor(r, r.Op(Enf).Pos, car, 1000, 1300); if (!r.Is(far.HasValue, "a spot 10–13 m from the official car")) return;
                r.To(Math.Max(r.SendTo(Enf, far!.Value), T(g.S.ValeAtCar) + 1));
                r.Is(g.Vale == ValeAt.Pickup, "Vale is at the official car");
                var card = g.GoingLoud(new Order { Tick = g.Tick, Op = Enf, Do = "fire", Target = "car:official" });
                r.Eq((card.Fails, card.Hold), ("vale-dead", true), "the card: vale-dead, the hold");
                r.Do(Enf, "draw"); r.Wait(T(1));
                r.Is(r.Fire(Enf, "car:official"), "fire on the official car taken");
                r.Is(r.Until(() => g.CarDisabled("car:official") || g.Over, T(30)), "it is disabled");
                var vp = g.ValePos(); r.Is(vp.HasValue && vp.Value != g.CarPos(), "Vale is out beside it");
                r.Until(() => g.Over, T(40));
                r.Eq(g.FailCode, "vale-dead", "the blast kills Vale: vale-dead");
            });

            Add("CAR-GETAWAY", "—", "§10.7: the getaway car disabled fails the mission getaway-lost (the card says so first; the cause is the wrecked car)", r =>
            {
                var g = r.G; var car = g.CarAt("car:getaway")!.Value;
                var spot = SpotFor(r, r.Op(Scout).Pos, car, 600, 1000); if (!r.Is(spot.HasValue, "a spot 6–10 m from the getaway car")) return;
                r.To(r.SendTo(Scout, spot!.Value));
                r.Eq(g.GoingLoud(new Order { Tick = g.Tick, Op = Scout, Do = "fire", Target = "car:getaway" }).Fails, "getaway-lost", "the card: getaway-lost, the hold");
                r.Is(r.Fire(Scout, "car:getaway"), "fire on the getaway car taken");
                r.Is(r.Until(() => g.Over, T(60)), "the run ends");
                r.Eq(g.FailCode, "getaway-lost", "getaway-lost");
                r.Is(g.CarDisabled("car:getaway") && !g.CarBurning("car:getaway"), "at disabled, before it burns");
                r.Is(g.Cause != null && g.Cause.Why == "wrecked" && g.Cause.Culprits.Count > 0 && g.Cause.Culprits[0].Kind == "car", "the cause: the wrecked getaway car");
            });

            Add("CAR-RIVALS", "—", "§10.7: the rival car is a target once it has parked, not before", r =>
            {
                var g = r.G;
                r.Is(!g.PosOf("car:rivals").HasValue, "before the rivals arrive: no rival car");
                r.Do(Scout, "draw"); r.Wait(T(1));
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "fire", Target = "car:rivals" }).code, "no-target", "fire on it: no-target");
                r.To(T(g.S.RivalsArrive) + 1);
                r.Eq(g.PosOf("car:rivals"), (Pt?)g.S.Stand(g.S.RivalsPark), "parked: a target at rivals.park");
            });
        }
    }
}
