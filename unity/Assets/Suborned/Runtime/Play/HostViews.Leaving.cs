using System.Collections.Generic;
using UnityEngine;
using Game = Suborned.Sim.Game;
using Pt = Suborned.Sim.Pt;

namespace Suborned.Play
{
    /// <summary>Brief 079 (Sky's fight video: "The car leaves", and it stayed in its bay): the engine ends the mission on the tick of Go (Game.Leave sets
    /// CarLeftTick and the result; nothing steps after it, and the engine has no drive for it), so the leaving is the view's: from the car's cell along the
    /// drive grid's own route to the end of the stand's exit lane (Game.CurStand.Exit), on down the road, out of the frame, gone. Read only: the
    /// engine's car stays at its stand, the hash and the replays never see this. It runs behind the end card and in any run that reaches the tick
    /// (a replay, the fail camera's), on the wall clock from the frame the view first sees it; a new run starts it again.</summary>
    public sealed partial class HostViews
    {
        const float LeaveAccel = 3.5f, LeaveTop = 15f, LeaveBeyond = 110f, LeaveSmooth = 5f; // m/s², m/s, m past the lane's end (Rocket Lane runs straight on 140 m past it; Apron Road 90 m), m of corner smoothing
        const string GetawayKey = "car/getaway";
        Game leaveFor; readonly ViewDrive getaway = new() { key = GetawayKey };
        Vector3? leaveHeading => getaway.heading;

        /// <summary>For checks and captures: how far along its way out the car has been drawn (m); 0 before it left.</summary>
        public float GetawayLeftM => leaveFor != null ? getaway.s : 0f;
        /// <summary>The getaway's drive-off is under way and its car still in the city's view (brief 109: the end card waits for it, HandoverHost.EndPlayout).</summary>
        public bool GetawayLeavingNow => leaveFor != null && getaway.way.Count >= 2 && !getaway.Done;

        void GetawayLeaving()
        {
            var g = host.Game;
            if (g == null || g.Range || g.CarLeftTick > g.Tick) /* brief 096 (engine 0.42.0): with the lend our car can leave while the mission runs on */ { if (leaveFor != null) { leaveFor = null; getaway.heading = null; } return; }
            if (!things.TryGetValue(GetawayKey, out var th)) return;
            if (leaveFor != g)
            {
                leaveFor = g; getaway.Reset(); LeaveWay(g);
            }
            if (getaway.way.Count < 2) return;
            if (getaway.Done) { if (th.shown) things[GetawayKey] = (th.t, th.to, th.to, false); return; } // out of the city's view
            // brief 098 step 1b: it brakes while it shoves a wreck aside (HostViews.TrafficShove); brief 109 round 2: round the other cars, or waits behind one (HostViews.LeavingSteer)
            DriveFrame(g, getaway, LeaveTop, Mathf.Min(Time.unscaledDeltaTime, 0.1f));
        }

        /// <summary>The way out in world points, its corners rounded: the grid's route (the engine's own, read only) from the car to the end of its
        /// lane, then straight on along the lane. A stand with no lane: nowhere to go, the car stays.</summary>
        void LeaveWay(Game g)
        {
            var leaveWay = getaway.way; leaveWay.Clear();
            var lane = g.CurStand.Exit; if (lane == null || lane.Length < 2) return;
            Pt from = g.CurStand.CarCell, end = lane[lane.Length - 1];
            Pt[] route = null;
            try { route = g.S.Drive.CarRoute(g.S.Drive.NearestFree(from) ?? from, end, g.S.DriveLaneCm); } catch (System.Exception) { }
            var pts = new List<Vector3> { frame.World(g.CurStand.CarAt) };
            if (route != null) foreach (var p in route) Add(pts, frame.World(p));
            else { Add(pts, frame.World(lane[0])); Add(pts, frame.World(end)); }
            Add(pts, frame.World(end));
            var d = frame.World(end) - frame.World(lane[lane.Length - 2]); d.y = 0f;
            if (d.sqrMagnitude > 1e-4f) { var far = frame.World(end) + d.normalized * LeaveBeyond; far.y = frame.World(end).y; pts.Add(far); }
            for (int i = 0; i < 4; i++) Round(pts); // Chaikin: a grid route's cells would steer the car in steps
            leaveWay.AddRange(pts); getaway.Measure();
        }

        static void Add(List<Vector3> pts, Vector3 w) { if ((pts[pts.Count - 1] - w).sqrMagnitude > 1f) pts.Add(w); } // a metre apart: the grid's fine cells carry no steering of their own

        static void Round(List<Vector3> pts)
        {
            var o = new List<Vector3>(pts.Count * 2) { pts[0] };
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                float len = Vector3.Distance(pts[i], pts[i + 1]), k = Mathf.Min(0.25f, LeaveSmooth * 0.25f / Mathf.Max(0.01f, len));
                o.Add(Vector3.Lerp(pts[i], pts[i + 1], k)); o.Add(Vector3.Lerp(pts[i], pts[i + 1], 1f - k));
            }
            o.Add(pts[pts.Count - 1]); pts.Clear(); pts.AddRange(o);
        }
    }
}
