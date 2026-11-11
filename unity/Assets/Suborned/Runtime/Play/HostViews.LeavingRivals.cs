using System.Collections.Generic;
using UnityEngine;
using Pt = Suborned.Sim.Pt;
using Sim = Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 109 round 2 (Sky on 0.2.325: "it drove through the rivals car"): the engine ends the mission on the Go's tick and steps no more, so a
    /// rival car that was driving off (CrewCar.Leaving: the beaten crew's wheelman, rivals.drives-off, often only a second before the Go) stood frozen
    /// across our way out, and our drive-off went through it. Like ours (HostViews.Leaving, brief 079), its leaving is now the view's once the clock
    /// has stopped: on along its own leg, the grid's route from where it stands to its entry (the engine's CarRouteP to S.RivalCarEntry, read only), then
    /// straight on out of the city's view, from a rolling start up to the street's speed. Our car meanwhile waits behind it or steers round it
    /// (HostViews.LeavingSteer). Read only: the engine's car and the hash never see it; a new run starts it again.</summary>
    public sealed partial class HostViews
    {
        const float RivalLeaveV0 = 4f, RivalLeaveTop = 14f, RivalLeaveBeyond = 80f; // m/s rolling when the clock stopped, m/s, m past its entry
        Sim.Game rivalLeaveFor; readonly List<Vector3> rivalWay = new(); float rivalS, rivalV, rivalLen; Vector3? rivalLeaveHeading;
        /// <summary>For checks and captures: how far the rival car has been drawn along its way out after the end (m); 0 before.</summary>
        public float RivalLeftM => rivalLeaveFor != null ? rivalS : 0f;

        void RivalsLeaving()
        {
            var g = host.Game;
            if (g == null || g.Range || !g.Over || g.CarSt != Sim.CrewCar.Leaving) { if (rivalLeaveFor != null) { rivalLeaveFor = null; rivalLeaveHeading = null; } return; }
            if (!things.TryGetValue("car/rivals", out var th)) return;
            if (rivalLeaveFor != g)
            {
                rivalLeaveFor = g; rivalS = 0f; rivalV = RivalLeaveV0; rivalLen = 0f; rivalWay.Clear();
                if (g.RivalCarPose() is not { } cp) return;
                Pt[] route = null; try { route = g.S.Drive.CarRoute(g.S.Drive.NearestFree(cp.pos) ?? cp.pos, g.S.CarStand(g.S.RivalCarEntry), g.S.DriveLaneCm); } catch (System.Exception) { }
                var pts = new List<Vector3> { th.t.position };
                if (route != null) foreach (var p in route) Add(pts, frame.World(p)); else Add(pts, frame.World(g.S.CarStand(g.S.RivalCarEntry)));
                if (pts.Count >= 2) { var d = pts[^1] - pts[^2]; d.y = 0f; if (d.sqrMagnitude > 1e-4f) { var far = pts[^1] + d.normalized * RivalLeaveBeyond; far.y = pts[^1].y; pts.Add(far); } }
                for (int i = 0; i < 4; i++) Round(pts);
                rivalWay.AddRange(pts); for (int i = 1; i < rivalWay.Count; i++) rivalLen += Vector3.Distance(rivalWay[i - 1], rivalWay[i]);
                Debug.Log($"HostViews: the clock stopped with the rival car driving off: it drives on out, {rivalLen:0} m");
            }
            if (rivalWay.Count < 2) return;
            if (rivalS >= rivalLen) { if (th.shown) things["car/rivals"] = (th.t, th.to, th.to, false); return; } // out of the city's view
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            rivalV = Mathf.Min(RivalLeaveTop, rivalV + LeaveAccel * dt); rivalS += rivalV * dt;
            var (at, dir) = WayAt(rivalWay, rivalS); rivalLeaveHeading = dir;
            things["car/rivals"] = (th.t, at, at, true);
        }

        /// <summary>A point `s` metres along a way and the way's direction there.</summary>
        static (Vector3 at, Vector3 dir) WayAt(List<Vector3> way, float s)
        {
            for (int i = 1; i < way.Count; i++)
            {
                var a = way[i - 1]; var b = way[i]; float len = Vector3.Distance(a, b);
                if (s > len && i + 1 < way.Count) { s -= len; continue; }
                var d = b - a; d.y = 0f;
                return (Vector3.Lerp(a, b, len > 1e-4f ? Mathf.Clamp01(s / len) : 1f), d.sqrMagnitude > 1e-8f ? d.normalized : Vector3.forward);
            }
            return (way[^1], Vector3.forward);
        }
    }
}
