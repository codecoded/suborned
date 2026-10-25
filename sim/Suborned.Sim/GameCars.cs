// Cars are targets (Sky 2026-10-21, tuning.md §9.7: "shootable, as in Syndicate"; Motive tuning.md §10.7, engine 0.16.0; both presets).
// Every car the Handover simulates is a target by its own id: "car:getaway" (the cell's, at its bay), "car:official" (Vale's, where CarPos
// puts it) and "car:rivals" (the rival crew's, parked at rivals.park once it has parked; a crew drawn by gunfire comes on foot and parks
// none). The range has no cars. The street's parked and passing cars are the host's dressing, not simulated objects, so they are no target.
//  · A car has car.target.health (×healthScale in the lab). At or below car.target.disabledAt (×healthScale) it is disabled: it stops where
//    it is and never drives again; its occupants get out. At 0 it burns (its blast ring shows) and car.target.burn seconds later explodes.
//  · The blast: everyone within car.target.blast.m of the car with a line to it takes car.target.blast.damage (×healthScale), the cell
//    included; occupants still inside take it whatever the line. Each person it hits is hit by the cell (hit-trail, down-trail, a body if
//    they die, as a shot's); an operative it hits costs no Trail. Seen by a civilian it is violence: alert.violenceSeen and a Menace step, once
//    a car. The car's cover points are gone (LiveCover).
//  · Shooting a car is a shooting (CellGunfire: gunfire-trail once, Alert to alert.gunfire, the response); the car itself takes no hit or
//    down Trail and no Standing step (property, as a camera).
//  · The getaway car disabled: the mission fails getaway-lost (every exit is gone). The official car disabled: it stays where it stopped
//    (the pickup's Take is still possible); the driver, inside when it is away from the pickup, gets out; Vale at the car gets out, and is
//    held inside only while the Enforcer is seizing him (Taking): inside at the blast, he takes it. The rival car: nothing more (a broken
//    crew leaves on foot, ai.rivalGone unchanged).
//  · Hostiles never choose a car (StanceTarget and the AI pick combatants only).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>A car a fire order has named: its health (Gun), when it was disabled and set burning, when it blows, and the operative whose
        /// shot set it burning (the blast's culprit).</summary>
        public sealed class CarTarget
        {
            public string Id = ""; public Gun Gun = null!; public int DisabledAt = -1, BurningAt = -1, BlastAt = -1; public bool Blown; public string By = ""; public Pt At;
        }
        readonly SortedDictionary<string, CarTarget> cars = new SortedDictionary<string, CarTarget>(StringComparer.Ordinal); // hashed in HashCars, only once a car is named
        /// <summary>The rival car parked at rivals.park (brief 059: CarSt Parked there): hashed with the cars only.</summary>
        bool rivalCarParked => CarSt == CrewCar.Parked && ccStop == S.RivalsPark;
        List<CoverPoint>? liveCover; // the site's cover less a blown car's points; null while no car has blown
        List<CoverPoint>? liveCoverAt; int liveCoverKey = -1; // brief 059: the cover of the cars where they stand now, cached by which stand
        /// <summary>The cover points standing now: the site's, less those of a car that has blown (tuning.md §10.7: "cars stay cover"), and less
        /// those of a car that is not where its points are (brief 059: a car that drives off takes its cover with it): the getaway car's while it
        /// is away from its first stand or driving, the rival car's while it is not parked at rivals.park.</summary>
        List<CoverPoint> LiveCover
        {
            get
            {
                var basis = liveCover ?? S.Cover;
                if (Range) return basis;
                bool gHome = getawayPath == null && CurStand == S.Stands[0], rHome = rivalCarParked;
                int key = (gHome ? 1 : 0) | (rHome ? 2 : 0) | ((liveCover?.Count ?? -1) + 1) << 2;
                if (gHome && rHome) return basis;
                if (key != liveCoverKey || liveCoverAt == null) { liveCoverAt = basis.FindAll(p => (gHome || p.Of != "car/getaway") && (rHome || p.Of != "car/rivals")); liveCoverKey = key; }
                return liveCoverAt;
            }
        }
        /// <summary>Whether any cover point of `of` (a cover point's `of`, e.g. car/official) still stands (host read; never hashed).</summary>
        public bool CoverStanding(string of) => LiveCover.Exists(p => p.Of == of);

        public static readonly string[] CarIds = { "car:getaway", "car:official", "car:rivals" };
        static bool IsCarId(string id) => id.StartsWith("car:", StringComparison.Ordinal);
        /// <summary>A camera or a car: shot, it is gunfire only (no hit or down Trail, no Standing); never a person.</summary>
        static bool IsPropertyId(string id) => IsCameraId(id) || IsCarId(id);
        static string CarCover(string id) => "car/" + id.Substring(4); // the cover points' `of`: car/getaway, car/official, car/rivals

        /// <summary>Where a car stands now, or null when it is not on the street (the range has none; the rival car until it parks).</summary>
        public Pt? CarAt(string id)
        {
            if (Range) return RangeCarAt(id); // the range's spawned cars (GameRange.cs)
            switch (id)
            {
                case "car:getaway": return Car;
                case "car:official": return CarPos();
                case "car:rivals": return RivalCarPose()?.pos; // brief 059 K7: a target from its entry, wherever it drives
                default: return null;
            }
        }
        public CarTarget? CarOf(string id) => cars.TryGetValue(id, out var c) ? c : null;
        public bool CarDisabled(string id) => cars.TryGetValue(id, out var c) && c.DisabledAt >= 0;
        public bool CarBurning(string id) => cars.TryGetValue(id, out var c) && c.BurningAt >= 0 && !c.Blown;
        /// <summary>The getaway car is wrecked: Board and Go are refused (getaway-lost).</summary>
        bool GetawayLost => CarDisabled("car:getaway");
        bool OfficialOut => CarDisabled("car:official"); // its driver and Vale are out of it
        int CarHealth => R.HealthCar * HealthScale;

        void EnsureCar(string id) { if (CarAt(id).HasValue && !cars.ContainsKey(id)) cars[id] = new CarTarget { Id = id, Gun = NewGun(id, null, false, R.HealthCar) }; }
        /// <summary>A car's gun: the one a fire order made, else a fresh one for a preview (never stored, so a preview changes nothing).</summary>
        Gun? CarGun(string id) => cars.TryGetValue(id, out var c) ? c.Gun : Array.IndexOf(CarIds, id) >= 0 && !Range ? NewGun(id, null, false, R.HealthCar) : null; // a known car not on the street: a target with no place (no-target)

        /// <summary>A shot lands on a car (Damage, after its health moved): disabled at disabledAt, burning at 0.</summary>
        void CarHit(string t, Gun tg, string by)
        {
            var c = cars[t];
            if (IsOpId(by)) c.By = by;
            if (c.DisabledAt < 0 && tg.Health <= R.CarDisabledAt * HealthScale) CarDisable(c);
            if (Over) return;
            if (tg.Health == 0 && c.BurningAt < 0)
            {
                tg.Down = true; c.BurningAt = Tick; c.BlastAt = Tick + T(R.CarBurnS); c.At = CarAt(t) ?? c.At;
                Chain.Add(t.Substring(4) + "-car-burning");
                Note("car.burning", $"The {CarName(t)} is on fire. It will explode.");
                return;
            }
            if (c.DisabledAt != Tick) Note("car.hit", $"The {CarName(t)} is hit.");
        }
        static string CarName(string id) => id == "car:getaway" ? "getaway car" : id == "car:official" ? "official car" : id == "car:rivals" ? "rival car" : "car " + id.Substring(4); // the range's: car range-1, …

        void CarDisable(CarTarget c)
        {
            c.DisabledAt = Tick; c.At = CarAt(c.Id) ?? c.At;
            Chain.Add(c.Id.Substring(4) + "-car-disabled");
            Note("car.disabled", $"The {CarName(c.Id)} is disabled. It can't drive.");
            switch (c.Id)
            {
                case "car:getaway":
                    Fail("getaway-lost", "The getaway car is wrecked.", () => With(Because("wrecked", true, Locate("getaway-car", "wrecked")), OpIn(c.By)));
                    return;
                case "car:rivals": // brief 059: it stops where it is and never drives again; whoever is in it gets out (the wheelman stands by it)
                    { var cp = RivalCarPose(); if (cp.HasValue) CarHalt(CrewCar.Stopped, "", cp.Value.pos, cp.Value.heading); crewNextAt = INF; tailAt = INF;
                      foreach (var m in Crew) if (m.Mode == CrewMode.Aboard && Up(m) && m.OutAt >= INF) m.OutAt = Tick + T(R.Crew.StepS) * m.N; }
                    return;
                default:
                    if (Range) RangeCarOut(c); // the range's car: its occupant gets out (GameRange.cs)
                    return;
                case "car:official":
                    StopOfficial(c.At); // it stops where it is (the driver-shot rule's own stop)
                    bool driverIn = !Driver.Down && valeDriveStart >= 0, valeIn = Vale == ValeAt.InCar; // at the pickup the driver stands beside it already (brief 059: until Vale is in)
                    if (valeIn) { Vale = ValeAt.Pickup; PickupService = false; } // brief 059: he gets out and stands by it
                    if (driverIn || valeIn) Note("car.out", driverIn && valeIn ? "The driver and Vale get out of the official car." : driverIn ? "The driver gets out of the official car." : "Vale gets out of the official car.");
                    return;
            }
        }

        /// <summary>The disabled official car's people stand beside it, the driver on the road side (side -1), Vale on the kerb side (+1): the
        /// nearest free ground 1.5 m either side of the car, across its heading (engine 0.33.1, car.footprint: 0.33.0 put them 1.5 m along +y,
        /// which is along the car in the official bay). The kerb side is the car's right (the block frame turns x to y anticlockwise, and the
        /// city drives on the right), the walk grid's NearestFree round the standing cars, deterministic.</summary>
        Pt BesideCar(Pt car, int side)
        {
            var h = OfficialHeading(); long n = G.Isqrt((long)h.X * h.X + (long)h.Y * h.Y); if (n == 0) { h = new Pt(0, 100); n = 100; }
            var right = new Pt((int)(h.Y * 150L / n), (int)(-h.X * 150L / n)); // the heading turned clockwise, 150 cm
            return Walk.NearestFree(car + new Pt(right.X * side, right.Y * side)) ?? car;
        }

        /// <summary>One tick of burning cars: each blows car.target.burn after it caught fire.</summary>
        void CarsStep()
        {
            if (cars.Count == 0) return;
            foreach (var c in cars.Values)
            {
                if (c.BlastAt < 0 || c.Blown || Tick < c.BlastAt) continue;
                Blast(c); if (Over) return;
            }
        }

        void Blast(CarTarget c)
        {
            c.Blown = true; var at = c.At;
            Chain.Add(c.Id.Substring(4) + "-car-exploded");
            Note("car.exploded", $"The {CarName(c.Id)} explodes.");
            var of = Range ? RangeCarCover(c.Id) : CarCover(c.Id); liveCover = new List<CoverPoint>((liveCover ?? S.Cover).FindAll(p => p.Of != of)); liveCoverKey = -1; // its cover points are gone (brief 059: from the site's, not the cars-where-they-stand filter)
            // violence seen by a civilian (§05; rulebook car.target): Alert to alert.violenceSeen and a Menace step, once a car
            bool civSees = false; foreach (var p in WitnessesOf(at, null, "gunfire")) if (p.Civilian) { civSees = true; break; }
            if (civSees)
            {
                RaiseAlert(R.ViolenceSeen, "violence-seen:blast:" + c.Id);
                if (incidents.Add("seen:blast:" + c.Id)) { AddTrail(R.WitnessTrail, "witnessed:violence:blast:" + c.Id); MoveStanding(-R.StandingStep, "violence-seen:blast:" + c.Id, "seen-violence", c.Id); }
            }
            int dmg = R.CarBlastDamage * HealthScale; long ring = R.CarBlastCm;
            foreach (var id in BlastIds(c))
            {
                var g = GunOf(id); if (g == null || g.Down) continue;
                bool inside = Inside(c, id);
                var pos = PosOf(id);
                if (!inside && (!pos.HasValue || !G.Within(pos.Value, at, (int)ring) || !Sees(at, pos.Value))) continue;
                bool cell = IsOpId(c.By) && !IsOpId(id), untraced = Range || IsFightHostile(id) && !R.FightTraced && FightOverTick == INF;
                resolving = new PendingShot { By = c.By.Length > 0 ? c.By : c.Id, T = id, G = g, At = pos ?? at, From = at, D = pos.HasValue ? Dist(pos.Value, at) : 0, Dmg = dmg, Hit = true };
                Note("car.blast-hits", $"The blast hits {id}.");
                Damage(id, g, IsOpId(id) ? Absorb(OpOf(Sim.Op.Parse(id)), dmg) : dmg, IsOpId(id) ? "blast" : c.By, cell, untraced);
                resolving = null;
                if (Over) return;
            }
        }

        /// <summary>Held inside the car at its blast: Vale while the Enforcer is seizing him at the official car.</summary>
        bool Inside(CarTarget c, string id) => c.Id == "car:official" && id == "councillor" && (Vale == ValeAt.Taking || Vale == ValeAt.InCar); // brief 059: in the back of his car too

        /// <summary>Everyone a blast can reach, in a fixed order: the cell, the city's people by role, the guard and the patrol, the response's
        /// officers, units and robots, the fight's hostiles, then the crowd by id.</summary>
        IEnumerable<string> BlastIds(CarTarget c)
        {
            foreach (var op in Ops) if (!op.Down && !op.Inside && !op.InCar) yield return Sim.Op.Name(op.Id);
            foreach (var r in new[] { "councillor", "driver", "waitress", "guard", "patrol" }) yield return r;
            foreach (var g in SecGuns) yield return g.Id;
            foreach (var u in Units) yield return u.Id;
            foreach (var h in Hostiles) yield return h.Id;
            var crowd = new List<string>(); foreach (var (p, _) in OnTheStreet()) if (p.Civilian && p != ValeP && p != Driver && p != Inez) crowd.Add(p.Id);
            crowd.Sort(StringComparer.Ordinal); foreach (var id in crowd) yield return id;
            foreach (var b in rangeCivs) yield return b.Id; // the range's bystanders (none outside the range)
        }

        internal void HashCars(Hash.H h)
        {
            if (cars.Count == 0) return;
            h.S("cars").Z(rivalCarParked).I(liveCover?.Count ?? -1);
            foreach (var kv in cars) { var c = kv.Value; h.S(c.Id).I(c.DisabledAt).I(c.BurningAt).I(c.BlastAt).Z(c.Blown).S(c.By).P(c.At); Hash.HashGun(h, c.Gun); }
        }
    }
}
