// Brief 098 step 1, law L8 (stopping): going round what will not move on, and finding another way. GameTrafficSense.cs decides when.
//  · The driver eases round within their own half of the road when the room is there: the smallest shift that clears it and the stretch past
//    it, kept on the drive grid, at traffic.pass.latCmS across for passCmS along (kerb side first at a tie); with no room there, out across the
//    centreline as far as the oncoming lane's line when that lane holds no car that could meet it before it is past (an overtake; meanwhile no
//    junction lets a car into that lane). Seen from afar they ease out early, still rolling; stopped too close to swing out, they back off up
//    to backCm where the road behind is clear, and try again. Back to the lane's line once the whole car is past.
//  · Round a person on their feet only once they have stayed in the road 5 waitS, a body's width wider.
//  · A turn refused for something that will not move on, waited out waitS at the line: the driver takes another turn out of the junction, to
//    their own mouth if it leads there, else the nearest other (the rest of the street finds another way).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>The smallest shift within the car's own half of the road (kerb side first at a tie) whose corridor clears what holds it up and the
        /// stretch past it, on the drive grid; else, round something that will not move on (or a person who stays in the road), out across the
        /// centreline as far as the oncoming lane's line, when that lane holds no car that could meet it before it is past (an overtake); none: it waits.</summary>
        void PlanPass(TrafficCar c, TrafficWorld w, Lane lane, long ahead, int wide = 0, bool overtake = false)
        {
            if (Tick - c.PlannedAt < Hz) return; c.PlannedAt = Tick; // a way round found none: look again in a second
            int maxIn = lane.Offset - c.HalfW - tr!.ClearCm, maxOut = lane.Half - lane.Offset - c.HalfW - tr.ClearCm;
            long stretch = ahead + 1500 + 2L * c.HalfLen * 10;
            if (LineWithin(c, stretch + ReturnMm(c, lane))) return; // too near a junction to go round and be back on the line before its stop line: it waits
            bool Clear(int lat) { var (d, kind, _, _) = Corridor(c, w, c.Front, stretch, lat, Math.Max(100, lane.Speed / 2), true, c.V == 0 ? lat : (int?)null, wide); return d == long.MaxValue || ((kind == 3 || kind == 6) && d > ahead + 1000); } // stopped, it creeps across first: the way from there
            for (int s = 25; s <= Math.Max(maxIn, maxOut); s += 25)
                foreach (var lat in new[] { s, -s })
                {
                    if (lat > maxOut || -lat > maxIn || !Clear(lat)) continue;
                    c.LatGoal = lat; c.BlockedAt = -1; c.PlannedAt = -100; return;
                }
            if (!overtake || lane.Twin == null || OncomingNear(c, lane, stretch)) return;
            for (int s = maxIn + 25; s <= 2 * lane.Offset; s += 25) if (Clear(-s)) { c.LatGoal = -s; c.BlockedAt = -1; c.PlannedAt = -100; return; }
        }
        /// <summary>A car on the lane the other way that could meet an overtake of `stretch` before it is past (at its own speed or its lane's).</summary>
        bool OncomingNear(TrafficCar c, Lane lane, long stretch)
        {
            var me = c.Route.Path.At(Math.Min(c.Route.Path.Length, c.Front)); long passTicks = (stretch + 2L * c.HalfLen * 10) * 3 / (2L * Math.Max(1, tr!.PassCmS));
            foreach (var o in Traffic)
            {
                if (o == c) continue; int li = o.Route.LaneAt(o.Mm); if (li < 0 || o.Route.Lanes[li].lane != lane.Twin) continue;
                long reach = stretch + passTicks * Math.Max(o.V, lane.Twin.Speed) + tr.GapCm * 10L;
                if ((long)G.Dist(TrafficPose(o).at, me) * 10 <= reach) return true;
            }
            return false;
        }
        /// <summary>A car out across the centreline of `lane`'s twin (an overtake under way there): no junction lets a car into `lane` meanwhile.</summary>
        bool Overtaking(Lane lane)
        {
            foreach (var o in Traffic) { int li = o.Route.LaneAt(o.Mm); if (li < 0) continue; var l = o.Route.Lanes[li].lane; if (l.Twin == lane && -o.LatGoal > l.Offset - o.HalfW - tr!.ClearCm || l.Twin == lane && -o.Lat > l.Offset - o.HalfW - tr!.ClearCm) return true; }
            return false;
        }

        /// <summary>The turn at route index k is refused for good: take another turn out of the same lane, to the car's own mouth if it leads there,
        /// else to the nearest other mouth; the first (by turn order) whose box and way out are not stood in by something that will not move on.</summary>
        void Reroute(TrafficCar c, int k, TrafficWorld w, int now)
        {
            var (bad, enter, _) = c.Route.Turns[k]; var lane = c.Route.Lanes[k].lane; long laneFrom = c.Route.Lanes[k].from;
            foreach (var t in lane.Out)
            {
                if (t == bad) continue;
                var r = net!.RouteVia(lane, t, c.Route.To); if (r == null) foreach (var m in net.Mouths) { var q = net.RouteVia(lane, t, m); if (q != null && (r == null || q.Path.Length < r.Path.Length)) r = q; }
                if (r == null) continue;
                var (oldRoute, oldMm) = (c.Route, c.Mm); c.Route = r; c.Mm = oldMm - laneFrom; // try it on: the car's own corridor on the new way
                var (_, e2, x2) = r.Turns[0]; var (_, still) = Occupied(c, e2, x2 + 2L * c.HalfLen * 10 + tr!.GapCm * 10L, w);
                if (still) { c.Route = oldRoute; c.Mm = oldMm; continue; }
                c.Refused = -1; c.RefusedAt = -1; c.AskedAt = -1; c.Turn = -1;
                return;
            }
            c.RefusedAt = now; // nowhere else to go: it waits, and asks again after waitS
        }

        /// <summary>The next stop line lies within `mm` of the car's front (a turn not yet granted, or not yet entered).</summary>
        static bool LineWithin(TrafficCar c, long mm) { int k = NextTurn(c); return k >= 0 && c.Route.Turns[k].enter - c.Front <= mm; }
        /// <summary>The mm it takes to come back from the widest shift to the lane's line at latCmS across for passCmS along.</summary>
        long ReturnMm(TrafficCar c, Lane lane) => 2L * lane.Offset * tr!.PassCmS * 10 / Math.Max(1, tr.LatCmS);
        /// <summary>Stopped too close to what holds it to swing out: back off along its route up to traffic.pass.backCm, if the road behind is clear
        /// for it and its gap and it stays on its lane; once a blockage (BackedAt: the blockage it backed off for).</summary>
        void BackOff(TrafficCar c, Lane lane, TrafficWorld w)
        {
            if (c.BackedAt == c.BlockedAt) return;
            int li = c.Route.LaneAt(c.Mm); if (li < 0) return; long room = c.Rear - c.Route.Lanes[li].from; long back = Math.Min(tr!.BackCm * 10L, room - 500);
            if (back < 1000 || !BehindClear(c, w, back + tr.GapCm * 10L)) return;
            c.Back = back; c.BackedAt = c.BlockedAt;
        }
        /// <summary>One tick backing off at half a metre a second; done (or blocked behind), it plans its way round again next tick.</summary>
        void BackStep(TrafficCar c, TrafficWorld w, int now)
        {
            long step = Math.Min(50, c.Back);
            if (!BehindClear(c, w, step + (c.Clears.Length > 0 ? RoomKeepMm : tr!.GapCm * 10L))) { c.Back = 0; return; } // brief 098 step 1b: making room for a mission car, it backs into the gap behind it
            c.Mm -= step; c.Back -= step; c.V = 0;
            if (c.Back == 0) c.BlockedAt = now - T(tr!.PassWaitS) * 5; // ready to plan at once
        }
        /// <summary>The stretch `len` mm behind the car's rear on its route is clear of people, bodies and cars.</summary>
        bool BehindClear(TrafficCar c, TrafficWorld w, long len)
        {
            int r = c.HalfW + tr!.ClearCm; long pr2 = (long)(r + R.CrowdOccludeCm) * (r + R.CrowdOccludeCm);
            for (long mm = c.Rear; mm >= Math.Max(0, c.Rear - len); mm -= TStep)
            {
                var q = c.Route.Path.At(mm); if (c.Lat != 0) { var h = c.Route.Path.Heading(mm); long hn = Math.Max(1, G.Isqrt((long)h.X * h.X + (long)h.Y * h.Y)); q = new Pt(q.X + (int)(h.Y * c.Lat / hn), q.Y + (int)(-h.X * c.Lat / hn)); } // at the offset it rides
                foreach (var p in w.People) if (G.D2(q, p) <= pr2) return false;
                foreach (var p in w.Bodies) if (G.D2(q, p) <= pr2) return false;
                foreach (var b in w.Cars) if (b.Car != c && b.Near(q, r)) return false;
            }
            return true;
        }
    }
}
