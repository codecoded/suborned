// Boxed in (brief 096, engine 0.40.0, Keel; rulebook car.box; Motive's rules page, Sky 2026-11-04 "all. recommended"). A car standing on another
// car's way out boxes it in. The ways out are the site's, worked out at load (Site.Ground: each stand's drive to its exit lane, then the lane;
// the official car's from the pickup and out of the service bay; from its kerbside stop, fight-block-m straight on), swept at the car's
// half-width plus car.box.side. The cars that stand are StandingCars() less any that has exploded, less the leaving car: a driving car boxes
// nothing, a disabled or burning one still does. Read on demand from the cars' state (nothing kept, nothing hashed): the way clears the tick
// the car in it leaves or explodes.
//  · the getaway car boxed: Go is refused car-boxed (after car-no-driver, before car-held; ids: the cars in the way, then any holders), its
//    Drive drive-boxed (after drive-held); a Drive to a stand whose way out is boxed is stand-boxed;
//  · the official car boxed: it waits at the pickup, and stays at the close (closed-in, cause boxed: GameKerb.cs);
//  · at the close, the cell's car boxed with Vale aboard is closed-in, cause boxed (GameSeed.cs CloseStep).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>The cars standing on this way out (a polyline, swept at the ground's half-width), less `self` and any car that has exploded, in
        /// StandingCars order (car:getaway, car:official, car:rivals). Empty on the range or for no way.</summary>
        List<string> CarsOn(Pt[] way, string self)
        {
            var l = new List<string>(); if (Range || way.Length == 0) return l;
            long half = S.GroundOf(R).Half;
            foreach (var c in StandingCars())
            {
                if (c.Id == self || (CarOf(c.Id) is CarTarget t && t.Blown)) continue;
                if (TrafficCarById(c.Id) is TrafficCar tc && !ForGood(tc)) continue; // brief 098 step 1: a passing car waiting in traffic boxes nobody in (its driver moves up or lets you out); a wreck, an abandoned car, one pulled up at a lockdown does
                if (CarGeo.OnWay(CarGeo.Rect(c.At, c.Heading, c.Hl(R), c.Hw(R)), way, half)) l.Add(c.Id); // brief 098 step 1: a passing car's own footprint
            }
            return l;
        }
        /// <summary>Where the rival car is and which way it points, or null while it is off the map (Off, Away, Gone). Engine 0.41.0: the block on
        /// arrival is the crew's own routine (GameRivalBlock.cs); 0.40.0's test seam Start.RivalBlock is retired into it.</summary>
        public (Pt pos, Pt heading)? RivalCarPose() => RivalCarPoseCrew();
        /// <summary>A stand's way out (car.box), or none (the range).</summary>
        public Pt[] WayOut(string standId) => !Range && S.GroundOf(R).Way.TryGetValue(standId, out var w) ? w : Array.Empty<Pt>();
        /// <summary>The cars boxing the getaway car in where it stands now (none while it drives). A host read: the ring's "Boxed in", Alt's way out.</summary>
        public List<string> GetawayBoxers() => getawayPath != null || CurStand == null ? new List<string>() : CarsOn(WayOut(CurStand.Id), "car:getaway");
        /// <summary>The cars standing on the way out of a stand the getaway car would drive to (stand-boxed: it can't get in).</summary>
        public List<string> StandBoxers(string standId) => CarsOn(WayOut(standId), "car:getaway");
        /// <summary>The cars boxing the official car in where it stands: at the pickup (its route out to the carriageway, then fight-block-m), in the
        /// service bay (its route in, backwards), at its kerbside stop (fight-block-m straight on); none while it drives or once it has stopped elsewhere.</summary>
        public List<string> OfficialBoxers() => Range || !OfficialStanding() ? new List<string>() : CarsOn(OfficialWay(), "car:official");
        /// <summary>The official car's way out where it stands (OfficialBoxers'; engine 0.42.0: the lent car's lane is its first fight-block-m). Empty once it stopped elsewhere.</summary>
        Pt[] OfficialWay()
        {
            var g = S.GroundOf(R);
            if (valeDriveStart < 0 || carToService == null) return LendOn ? PickupWayToKerb(g) : g.PickupWay; // engine 0.42.0 (L10): with the drive taken, only as far as his kerbside stop
            if (carStoppedAt.HasValue) return Array.Empty<Pt>();
            if (CarAtKerb) { var at = CarPos(); return new[] { at, CarGeo.Ahead(at, kerbHeading, g.BlockCm) }; }
            if (CarAtMouth) { var at = CarPos(); return new[] { at, CarGeo.Ahead(at, OfficialHeading(), g.BlockCm) }; }
            return g.ServiceWay;
        }
        /// <summary>car-boxed's and drive-boxed's words: the cars in the way, then (Go) the holders.</summary>
        static string BoxedWords(List<string> bx, List<string>? holders = null) =>
            $"boxed in: {string.Join(", ", bx)} {(bx.Count == 1 ? "is" : "are")} in the way" + (holders != null && holders.Count > 0 ? $"; the car is held too: {string.Join(", ", holders)}" : "");
        /// <summary>The ids car-boxed names: the cars in the way, then any holders.</summary>
        static string BoxedIds(List<string> bx, List<string> holders) { var l = new List<string>(bx); foreach (var h in holders) if (!l.Contains(h)) l.Add(h); return string.Join(",", l); }
    }
}
