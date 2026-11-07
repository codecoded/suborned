// Brief 098 step 1, law L8 (Sky 2026-11-07: "Pull a gun out in front of someone they run away ... based on their nature"; "a car is a car"):
// what the street's drivers do, and what a passing car is to everyone else.
//  · Gunfire: a driver at the wheel who hears a shot whose shooter stands within crowd.scatter of the car (the crowd's own rule), or who sees a
//    drawn weapon in their cone (perception.people: the car's heading is their facing; the engine's notice test, as a passer-by's), brakes hard
//    (traffic.drive.hardBrakeCmS2) and stops; after the crowd's startle (the Fearful's shorter) they duck at the wheel until it has been quiet
//    crowd.quiet (traffic.react.duck: no witness while down), or drive off fast (react.flee, fleePm of the lane's speed), still stopping for
//    anyone in the road. A shot while ducked keeps them down.
//  · A car is a car (rulebook car.target, both presets): a passing car is a target by its id, car:<slot>-<trip>. Hit, its driver gets out on
//    the kerb side and runs (the crowd's flight from the shot); it stands abandoned where it stopped. Disabled it stops for good; burning, its
//    driver (if still in) gets out at once; it blows as any car does. A standing car (stopped traffic.stand.standS, or for good) is ground
//    nobody walks through (car.footprint), boxes a way out it stands on (car.box) and, if its kind gives cover, is cover on its four sides (its
//    sides' and ends' points as the site's cars have them) until it blows.
//  · A lockdown (traffic.lockdown.alert): no car comes in (GameTraffic.cs); with then stop, the cars in the zone pull up for good.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>Gunfire heard and drawn guns seen by the drivers at the wheel this tick (after the fight's shots, before the crowd clears them).</summary>
        void TrafficAlarms(int now)
        {
            var drawn = new List<Pt>(); foreach (var op in Ops) if (op.Gun.Drawn && !op.Down && !op.Inside) drawn.Add(op.Pos);
            long scatter2 = (long)R.CrowdScatterCm * R.CrowdScatterCm; int braking = 0;
            foreach (var c in Traffic)
            {
                if (c.Out != null || CarDisabled(c.Id) || c.Mode == TrafficMode.Abandoned) continue;
                var (at, h) = TrafficPose(c); Pt? from = null;
                foreach (var s in shotsThisTick) if (G.D2(s, at) <= scatter2) { from = s; break; }
                if (!from.HasValue && c.Mode != TrafficMode.Duck) foreach (var d in drawn) if (NoticesAt(c.Driver, at, d, "weapon-drawn", Alert, h)) { from = d; break; }
                if (!from.HasValue) continue;
                c.Quiet = now; c.Alarm = from.Value;
                if (c.Mode == TrafficMode.Drive || c.Mode == TrafficMode.Held) { c.Mode = TrafficMode.Brake; c.ModeTick = now; c.Flees = !tr!.Ducks(c.Driver.Temperament); braking++; }
            }
            if (braking > 0) Note("traffic.brakes", braking == 1 ? "A passing car brakes hard." : $"{braking} passing cars brake hard.");
        }

        /// <summary>The driver gets out on the kerb side (their right) and runs from `from`: a passer-by of the crowd from now (they witness, scatter,
        /// block sight), startled first for their nature's time. The car stands where it stopped.</summary>
        void DriverOut(TrafficCar c, Pt from)
        {
            if (c.Out != null) return;
            var (at, h) = TrafficPose(c); long n = Math.Max(1, G.Isqrt((long)h.X * h.X + (long)h.Y * h.Y)); int off = c.HalfW + R.CarClearCm + 25;
            var side = new Pt(at.X + (int)(h.Y * off / n), at.Y + (int)(-h.X * off / n));
            var stand = Walk.NearestFree(side) ?? Walk.NearestFree(at) ?? side;
            var w = new Walker { P = c.Driver, Added = true, St = "startled", At = stand, RunStart = Tick, Until = Tick + (c.Driver.Temperament == "fearful" ? R.CrowdStartleFearfulTicks : R.CrowdStartleTicks) };
            lastShooterFor[c.Driver.Id] = from; Walkers.Add(w); c.Out = w; byIdCache = null; walkerOfCache = null; occludersTick = -1;
            if (c.Mode != TrafficMode.Abandoned) { c.Mode = TrafficMode.Abandoned; c.ModeTick = Tick; }
            c.V = 0; c.Turn = -1; FreeSlot(c);
            Note("traffic.driver-out", $"The driver of a passing {c.Model} gets out and runs.");
        }
        /// <summary>A car that stays (abandoned, disabled) lets its slot run the next trip: the street keeps flowing round it.</summary>
        void FreeSlot(TrafficCar c) { var s = slots[c.Slot - 1]; if (s.Car == c) { s.Car = null; s.Next = Tick + T(XBand(SlotId(s.Index), "off", tr!.OffMinS, tr.OffMaxS, c.Trip)); } }

        /// <summary>CarHit's hook: a shot lands on a passing car; its driver, still in it, gets out and runs from the shooter.</summary>
        void TrafficHit(string id, string by)
        {
            var c = TrafficCarById(id); if (c == null) return;
            DriverOut(c, PosOf(by) ?? (LastShotTick == Tick ? lastShooter : TrafficPose(c).at));
            trafficCoverVer++;
        }
        /// <summary>CarDisable's hook: a passing car disabled stops where it is, for good.</summary>
        void TrafficDisabled(CarTarget t)
        {
            var c = TrafficCarById(t.Id); if (c == null) return;
            c.V = 0; DriverOut(c, LastShotTick == Tick ? lastShooter : TrafficPose(c).at); c.Standing = c.Settled = true; trafficCoverVer++;
        }

        // ---------------------------------------------------------------- standing, settled, cover

        readonly List<CoverPoint> trafficCover = new List<CoverPoint>(); int trafficCoverVer; long[] trafficCoverKey = Array.Empty<long>();
        /// <summary>The end of each traffic tick: who stands and who is settled, and the cover the standing ones give (rebuilt only when it changes).</summary>
        void TrafficFlags(int now)
        {
            var key = new List<long>();
            foreach (var c in Traffic)
            {
                if (c.V == 0) { if (c.StoppedAt < 0) c.StoppedAt = now; } else c.StoppedAt = -1;
                bool still = TrafficStill(c), stood = c.StoppedAt >= 0;
                c.Standing = still || (stood && now - c.StoppedAt >= T(tr!.StandS)); c.Settled = still || (stood && now - c.StoppedAt >= T(tr!.SettleS));
                if (c.Standing && c.Cover && !(CarOf(c.Id) is CarTarget ct && ct.Blown)) { var (at, h) = TrafficPose(c); key.Add(c.Slot * 100000L + c.Trip); key.Add(at.X); key.Add(at.Y); key.Add((long)h.X * 4_000_037L + h.Y); }
            }
            var k = key.ToArray(); if (SameKey(k, trafficCoverKey)) return;
            trafficCoverKey = k; trafficCoverVer++; trafficCover.Clear();
            foreach (var c in Traffic) if (c.Standing && c.Cover && !(CarOf(c.Id) is CarTarget ct && ct.Blown)) trafficCover.AddRange(CoverOf(c));
        }
        /// <summary>A standing car's cover: a point off each side and each end, facing the car, as the site's car points stand (sides 105 cm off
        /// the body, ends 90 cm), on free ground of the site.</summary>
        IEnumerable<CoverPoint> CoverOf(TrafficCar c)
        {
            var (at, h) = TrafficPose(c); long n = Math.Max(1, G.Isqrt((long)h.X * h.X + (long)h.Y * h.Y)); var of = CarCover(c.Id); string id0 = "cv-" + c.Id.Substring(4);
            var pts = new (long along, long across, int cos, int q)[] { (c.HalfLen + 90, 0, 800, 630), (-(c.HalfLen + 90), 0, 800, 630), (0, c.HalfW + 105, 572, 850), (0, -(c.HalfW + 105), 572, 850) };
            for (int k = 0; k < pts.Length; k++)
            {
                var (u, v, cos, q) = pts[k];
                var p = new Pt(at.X + (int)((h.X * u + h.Y * v) / n), at.Y + (int)((h.Y * u - h.X * v) / n));
                if (!S.Grid.Free(p)) continue;
                long fx = at.X - p.X, fy = at.Y - p.Y, fl = Math.Max(1, G.Isqrt(fx * fx + fy * fy));
                yield return new CoverPoint { Id = id0 + "-" + k.ToString(System.Globalization.CultureInfo.InvariantCulture), At = p, Faces = new Pt((int)(fx * 1000 / fl), (int)(fy * 1000 / fl)), Cos = cos, Q = q, H = 145, Kind = "car", Of = of };
            }
        }
        List<CoverPoint>? coverWith; List<CoverPoint>? coverWithBase; int coverWithVer = -1;
        /// <summary>LiveCover with the standing passing cars' cover added (the mission's list untouched when traffic stands nowhere).</summary>
        List<CoverPoint> WithTrafficCover(List<CoverPoint> basis)
        {
            if (trafficCover.Count == 0) return basis;
            if (coverWith == null || !ReferenceEquals(coverWithBase, basis) || coverWithVer != trafficCoverVer) { coverWith = new List<CoverPoint>(basis); coverWith.AddRange(trafficCover); coverWithBase = basis; coverWithVer = trafficCoverVer; }
            return coverWith;
        }

        /// <summary>The passing cars standing now (StandingCars; `settled`: only those the crowd's legs go round), in id order, with their footprints.</summary>
        void TrafficStanding(List<StandingCar> l, bool settled)
        {
            if (tr == null) return;
            foreach (var c in Traffic) if (settled ? c.Settled : c.Standing) { var (at, h) = TrafficPose(c); l.Add(new StandingCar(c.Id, at, h, c.HalfLen, c.HalfW)); }
        }
    }
}
