// Brief 098 step 1b (Sky 2026-11-07, on jams: "should be opposite - shot cars can get past, others block"; the brief: "Make it visible and
// physical, not a ghost: for example the wreck is pushed toward the kerb as the car goes by, with the host told so it can draw it").
//  · A wreck (a passing car disabled, burning, blown, or abandoned at a shot) in a mission car's next place is pushed aside: away from the
//    mission car's line, toward the side the wreck's centre is on (its right, the kerb, at a tie), at traffic.shove.cmS, never into a building,
//    street furniture (the walk grid's solids) or another car, and traffic.shove.maxCm at most in all. It keeps the side it was first pushed to.
//    From its first push it stands where it was pushed (TrafficCar.Moved: its pose off its route), its footprint, cover and box moving with it.
//  · A Go (the getaway car's, or the lent car's) is never boxed by a wreck: the wrecks on its way out are pushed off the way as the car leaves.
//  · A passing car standing in a mission car's way while waiting for that very car (TrafficCar.Clears) gets out of the way: sideways within its
//    half where its whole body clears the mission car's coming places and stays on the drive grid, else backing off up to traffic.pass.backCm
//    where the road behind is clear; meanwhile it no longer waits for that car's coming places, only for the car itself.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>Push wreck c out of mission car `by`'s next place `me` (at `at`, heading `h`) for one tick: (clear: it no longer meets `me`; moved:
        /// it moved this tick). Not moved and not clear: it is pinned (a wall, furniture or a car beyond it, or pushed as far as a wreck goes).</summary>
        (bool clear, bool moved) Shove(TrafficCar c, string by, Pt at, Pt h, Pt[] me)
        {
            var (wat, wh) = TrafficPose(c);
            int left = tr!.ShoveMaxCm - c.ShovedCm; if (left <= 0) return (false, false);
            long n = Math.Max(1, G.Isqrt((long)h.X * h.X + (long)h.Y * h.Y));
            long across = ((long)(wat.X - at.X) * h.Y - (long)(wat.Y - at.Y) * h.X) / n; // cm right of the mission car's line (its right is (h.y, -h.x))
            int first = c.ShovedBy == by && c.ShoveSide != 0 ? c.ShoveSide : across >= 0 ? 1 : -1;
            int step = Math.Min(left, Math.Max(5, tr.ShoveCmS / Hz));
            foreach (var s in Math.Abs(across) <= 30 && c.ShoveSide == 0 ? new[] { first, -first } : new[] { first }) // dead ahead: either side that is free; else only away from its line
            {
                Pt Off(int d) => new Pt(wat.X + (int)(h.Y * s * d / n), wat.Y + (int)(-h.X * s * d / n));
                int done = 0;
                for (int d = 5; d <= step; d += 5) { var q = Off(d); if (!WreckFits(c, q, wh, by)) break; done = d; if (!CarGeo.PolysMeet(me, CarGeo.Rect(q, wh, c.HalfLen, c.HalfW))) break; }
                if (done == 0) continue;
                MoveWreck(c, Off(done), wh, by, done, s);
                return (!CarGeo.PolysMeet(me, CarGeo.Rect(c.MovedAt, wh, c.HalfLen, c.HalfW)), true);
            }
            return (false, false);
        }
        /// <summary>The wreck's footprint at q fits there: its corners and centre on the site's open ground where they are inside the window (not a
        /// building or street furniture), meeting no other passing car and no mission car other than the one pushing it.</summary>
        bool WreckFits(TrafficCar c, Pt q, Pt h, string by)
        {
            var box = CarGeo.Rect(q, h, c.HalfLen, c.HalfW);
            foreach (var p in box) if (S.Grid.InGrid(p) && !S.Grid.Free(p)) return false;
            if (S.Grid.InGrid(q) && !S.Grid.Free(q)) return false;
            foreach (var o in Traffic) { if (o == c) continue; var (oa, oh) = TrafficPose(o); if (CarGeo.PolysMeet(box, CarGeo.Rect(oa, oh, o.HalfLen, o.HalfW))) return false; }
            foreach (var (mid, ma, mh, _) in MissionCarsNow()) if (mid != by && CarGeo.PolysMeet(box, CarGeo.Rect(ma, mh, R.CarLenCm / 2, R.CarWCm / 2))) return false;
            return true;
        }
        /// <summary>The wreck stands at q from now (its pose off its route), pushed by `by`; its cover and footprint move with it.</summary>
        void MoveWreck(TrafficCar c, Pt q, Pt h, string by, int cm, int side)
        {
            bool firstBy = c.ShovedBy != by;
            c.Moved = true; c.MovedAt = q; c.MovedH = h; c.ShovedAt = Tick; c.ShovedCm += cm; c.ShovedBy = by; c.ShoveSide = side;
            TrafficCoverRefresh(); standingAt = -1;
            if (firstBy) Note("traffic.shoved", $"The {CarName(by)} pushes the wrecked passing {c.Model} aside.");
        }
        /// <summary>A Go whose way out a wreck stands on is taken (a wreck boxes nobody in): each wreck on the way is pushed off it, square to the way's
        /// stretch nearest it and away from it, as far as it fits and traffic.shove.maxCm allows, as the car leaves.</summary>
        void ShoveOffWay(Pt[] way, string by)
        {
            if (tr == null || way.Length < 2) return;
            long half = S.GroundOf(R).Half;
            foreach (var c in Traffic)
            {
                if (!Shot(c)) continue;
                var (at, h) = TrafficPose(c); if (!CarGeo.OnWay(CarGeo.Rect(at, h, c.HalfLen, c.HalfW), way, half)) continue;
                int best = 0; long bd = long.MaxValue; // the way's stretch nearest the wreck
                for (int k = 0; k + 1 < way.Length; k++) { long d = SegD2(at, way[k], way[k + 1]); if (d < bd) { bd = d; best = k; } }
                var a = way[best]; var b = way[best + 1]; var dir = new Pt(b.X - a.X, b.Y - a.Y); if (dir.X == 0 && dir.Y == 0) continue;
                long n = Math.Max(1, G.Isqrt((long)dir.X * dir.X + (long)dir.Y * dir.Y)), across = ((long)(at.X - a.X) * dir.Y - (long)(at.Y - a.Y) * dir.X) / n;
                int s = across >= 0 ? 1 : -1, done = 0;
                for (int d = 5; d <= tr.ShoveMaxCm - c.ShovedCm; d += 5)
                {
                    var q = new Pt(at.X + (int)(dir.Y * s * d / n), at.Y + (int)(-dir.X * s * d / n)); if (!WreckFits(c, q, h, by)) break;
                    done = d; if (!CarGeo.OnWay(CarGeo.Rect(q, h, c.HalfLen, c.HalfW), way, half)) break;
                }
                if (done > 0) MoveWreck(c, new Pt(at.X + (int)(dir.Y * s * done / n), at.Y + (int)(-dir.X * s * done / n)), h, by, done, s);
            }
        }

        /// <summary>How close a car backing off to make room for a mission car comes to what is behind it (mm): into the standstill gap, not onto it.</summary>
        const long RoomKeepMm = 500;
        /// <summary>The getaway car leaves (Go): the wrecks on its way out are pushed off it.</summary>
        void ShoveOffGetaway() { if (tr != null && CurStand != null) ShoveOffWay(WayOut(CurStand.Id), "car:getaway"); }
        /// <summary>The squared distance (cm²) from p to segment ab.</summary>
        static long SegD2(Pt p, Pt a, Pt b)
        {
            long abx = b.X - a.X, aby = b.Y - a.Y, apx = p.X - a.X, apy = p.Y - a.Y, ab2 = abx * abx + aby * aby, dot = apx * abx + apy * aby;
            if (ab2 == 0 || dot <= 0) return G.D2(p, a);
            if (dot >= ab2) return G.D2(p, b);
            long cross = apx * aby - apy * abx; return cross * cross / ab2; // the block is under 200 m across: no overflow
        }
        /// <summary>Car c stands in mission car c.Clears's way, waiting for it: it makes room for that car's next second of road (or, where it cannot,
        /// its next few places: the mission car comes on as the way clears, and the car backs again if it must). It backs off along
        /// its route (on a lane or in a junction's turn) by the least that clears it, up to traffic.pass.backCm, where the road behind is clear; failing
        /// that, on a lane, it pulls aside, away from the mission car, by the least shift whose whole body clears it on the drive grid: within its half,
        /// or across the centreline as far as the other lane's line when that lane holds no car that could meet it (an overtake's room).
        /// Neither: it stays (a jam), and the mission car waits.</summary>
        void ClearFor(TrafficCar c, TrafficWorld w, Lane? lane)
        {
            if (c.Back > 0 || !(c.Mode == TrafficMode.Drive || c.Mode == TrafficMode.Flee)) return;
            var coming = new List<Pt[]>(); int grow = tr!.ClearCm; Pt? them = null;
            for (int dt = 0; dt <= Hz; dt++) if (MissionCarAt(c.Clears, Tick + dt) is var f && f.HasValue) { coming.Add(CarGeo.Rect(f.Value.at, f.Value.h, R.CarLenCm / 2 + grow, R.CarWCm / 2 + grow)); them ??= f.Value.at; }
            if (coming.Count == 0) return;
            bool ClearAt(long mm, int lat) { var (at, h) = PoseOn(c, mm, lat); var me = CarGeo.Rect(at, h, c.HalfLen, c.HalfW); foreach (var r in coming) if (CarGeo.PolysMeet(me, r)) return false; return true; }
            if (ClearAt(c.Mm, c.Lat)) return; // out of its way already: it stays there until it has gone by (the line can wait)
            if (c.LatGoal != c.Lat && ClearAt(c.Mm, c.LatGoal)) return; // on its way aside to where it clears: it carries on
            for (int pass = 0; pass < 2; pass++, coming.RemoveRange(4, Math.Max(0, coming.Count - 4))) // its next second of road; failing that its next few places (it comes on as the way clears)
                for (long b = 250; b <= tr.BackCm * 10L && c.Rear - b >= 500; b += 250)
                    if (ClearAt(c.Mm - b, c.Lat)) { if (BehindClear(c, w, b + RoomKeepMm)) { c.Back = b; return; } break; } // into the gap behind it, keeping RoomKeepMm (BackStep)
            if (lane == null) return;
            var (me0, h0) = TrafficPose(c); long hn = Math.Max(1, G.Isqrt((long)h0.X * h0.X + (long)h0.Y * h0.Y));
            int away = ((long)(them!.Value.X - me0.X) * h0.Y - (long)(them.Value.Y - me0.Y) * h0.X) / hn >= 0 ? -1 : 1; // they are on its right: it moves left, and the other way round
            int maxIn = lane.Offset - c.HalfW - tr.ClearCm, maxOut = lane.Half - lane.Offset - c.HalfW - tr.ClearCm;
            if (away < 0 && lane.Twin != null && !OncomingNear(c, lane, 2L * c.HalfLen * 10)) maxIn = 2 * lane.Offset; // the other way is clear: as far over as its lane's line, as an overtake goes
            for (int lat = c.Lat + 25 * away; lat <= maxOut && -lat <= maxIn; lat += 25 * away)
                if (ClearAt(c.Mm, lat) && PoseOn(c, c.Mm, lat) is var q && OnDriveGrid(q.at, q.h, c)) { c.LatGoal = lat; return; }
        }

        /// <summary>Car c's footprint `mm` along its route at offset `lat` meets no other car's footprint (a mission car's coming places are no car) and
        /// covers nobody on foot or down: where a car making room may move.</summary>
        bool FootprintFree(TrafficCar c, long mm, int lat, TrafficWorld w)
        {
            var (at, h) = PoseOn(c, mm, lat); var me = CarGeo.Rect(at, h, c.HalfLen, c.HalfW);
            foreach (var b in w.Cars) if (b.Car != c && !b.Sweep && CarGeo.PolysMeet(me, CarGeo.Rect(b.At, b.H, b.HL, b.HW))) return false;
            foreach (var p in w.People) if (CarGeo.Inside(p, me)) return false;
            foreach (var p in w.Bodies) if (CarGeo.Inside(p, me)) return false;
            return true;
        }
        /// <summary>The standing passing cars' cover, rebuilt only when one stands somewhere new, starts or stops (TrafficFlags' reading, and a push's).</summary>
        void TrafficCoverRefresh()
        {
            var key = new List<long>();
            foreach (var c in Traffic) if (c.Standing && c.Cover && !(CarOf(c.Id) is CarTarget ct && ct.Blown)) { var (at, h) = TrafficPose(c); key.Add(c.Slot * 100000L + c.Trip); key.Add(at.X); key.Add(at.Y); key.Add((long)h.X * 4_000_037L + h.Y); }
            var k = key.ToArray(); if (SameKey(k, trafficCoverKey)) return;
            trafficCoverKey = k; trafficCoverVer++; trafficCover.Clear();
            foreach (var c in Traffic) if (c.Standing && c.Cover && !(CarOf(c.Id) is CarTarget ct && ct.Blown)) trafficCover.AddRange(CoverOf(c));
        }
    }
}
