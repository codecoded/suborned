// Engine 0.34.0 (brief 067; Motive briefs/067/motive.md §2.3, motive-values.json M067-K3; rulebook fight.unguarded): the end of a fight the
// cell can no longer fight.
//  (a) Engine 0.53.0 (brief 105, Motive briefs/105/motive/unguarded-car.html; Sky 2026-11-10: "they can't take popovic from the car - they can
//      only shoot at the car"; "everyone was around the car, can't just make me lose for that"): nobody takes anyone out of a car. 0.34.0's
//      walk-in (the rivals walking to an unguarded held car, the first at its body taking Vale) and brief 018's reach (RivalReachesVale,
//      RivalReachesLent) are retired, for every car. In their place the target rule (CarFoe): people inside a car are no target, the car is
//      (as in Syndicate). A rival fires at an agent outside within his reach, drawn or not (FoesInReach); only when nobody of the cell is
//      outside (up, on the street, not in a car) and Vale is aboard a cell car does he fire at that car, from where he stands or from cover, by
//      the fight AI's own advance to his reach: he never walks to the door. The blocker keeps his lane and shoots too. A car shot out puts
//      everyone out with Vale (CarShotOut, GameCars.cs), and the fight goes on.
//  (b) The cell down: the tick the last operative goes down (OpHit), the mission fails cell-down.
//  (c) Go needs a driver: refused car-no-driver with only the downed aboard (Go, GoQuery).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>Engine 0.53.0 (brief 105): an operative standing out in the fight: up, on the street, not in a car, still on the map. Drawn or
        /// not, he is a person the rivals shoot at (0.35.2's "drawn only" is retired).</summary>
        static bool Outside(Op o) => !o.Down && !o.InCar && !o.Inside && !o.LeftMap;

        /// <summary>Engine 0.53.0 (brief 105): the cell car Vale sits in (car:getaway, or the lent or taken car), "" when he is in none.</summary>
        string ValeCarId() => Vale == ValeAt.Car && !GetawayGone ? "car:getaway" : Vale == ValeAt.Lent && !lentLeft ? lentId : "";

        /// <summary>Engine 0.53.0 (fight.unguarded a): a rival's foe when he has none: the car Vale sits in, while nobody of the cell is outside, in
        /// the final fight still on. Null when the rule doesn't apply; the reach and the line are the caller's, as for any foe.</summary>
        (string id, Pt at)? CarFoe(string id)
        {
            if (Range || !IsRival(id) || FightOverTick != INF || Array.Exists(Ops, Outside)) return null;
            var c = ValeCarId(); if (c.Length == 0 || CarDisabled(c)) return null;
            var at = CarAt(c); if (!at.HasValue) return null;
            EnsureCar(c);
            return (c, at.Value);
        }

        /// <summary>Engine 0.53.0 (brief 105; Sky: "a shot-out car is not a loss"): the getaway car disabled by anyone but the cell (the rivals'
        /// fire): everyone aboard gets out at its side, Vale with the Enforcer, before any burn or blast; Board, Go and Drive are refused car-disabled
        /// from now on, and the fight goes on on foot (or in a taken car, car.take).</summary>
        void GetawayShotOut()
        {
            var side = Walk.NearestFree(GetawaySeat) ?? GetawaySeat; bool any = false, vale = Vale == ValeAt.Car;
            foreach (var o in Ops) if (o.InCar && !o.InLent && !o.LeftMap) { o.InCar = false; o.Pos = side; any = true; }
            Chain.Add("getaway-car-shot-out");
            if (any || vale) ShotOutNote("car:getaway", vale);
            if (vale) ValeOutOfCar();
        }
        /// <summary>Vale out of a shot-out cell car: held by the Enforcer, standing; with the Enforcer down, the downed-Enforcer law (OpHit's).</summary>
        void ValeOutOfCar()
        {
            Vale = ValeAt.WithEnforcer;
            if (!Enforcer.Down && !Enforcer.LeftMap) return;
            if (FightBranch == "safe") Fail("rivals-reach-vale", $"The Enforcer is down and the rival crew takes {ValeName} off him.", () => ShotDown("downed-enforcer", "enforcer"));
            else Fail("enforcer-down", "The Enforcer is down.", () => ShotDown("shot", "enforcer"));
        }
        void ShotOutNote(string car, bool vale) => Note("car.shot-out", $"The {(car == lentId && car != "car:getaway" ? LentName.Replace("the ", "") : CarName(car))} is shot out: everyone gets out{(vale ? " with " + ValeName : "")}, and the fight goes on on foot.");

        /// <summary>Engine 0.53.0 (brief 105): the HUD's lines while Vale sits in the car the rivals hit: rivals.fire-car at the first hit, then
        /// car.health at every hit with the health left (the car stops for good at car.target.disabledAt).</summary>
        void RivalsHitCar(string t, Gun tg, string by)
        {
            if (!IsRival(by) || ValeCarId() != t) return;
            string name = CarName(t);
            if (!Log.Exists(l => l.Code == "rivals.fire-car" && l.Text.Contains(name))) Note("rivals.fire-car", $"The rivals are shooting the {name} with {ValeName} inside: nobody of the cell is out to fight them.");
            Note("car.health", $"The {name} is hit: {tg.Health / HealthScale} of {R.HealthCar} left. It stops at {R.CarDisabledAt}.");
        }

        /// <summary>K3 (b): the tick the last operative goes down (in the car or not), the mission fails.</summary>
        bool CellDown(string name)
        {
            if (Range || !Array.TrueForAll(Ops, o => o.Down)) return false;
            Fail("cell-down", "The whole cell is down.", () => ShotDown("shot", name)); // the fail camera: the shot that downed the last to fall (why shot, as enforcer-down's)
            return true;
        }

        /// <summary>K3 (c): someone standing is in the getaway car to drive it.</summary>
        bool CarHasDriver() => Array.Exists(Ops, o => o.InCar && !o.InLent && !o.Down); // engine 0.42.0: the cell's car (the lent car's is LentHasDriver)
    }
}
