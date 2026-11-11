using System.Collections.Generic;
using UnityEngine;
using Sim = Suborned.Sim;
using Game = Suborned.Sim.Game;

namespace Suborned.Play
{
    /// <summary>Brief 109 (Sky on 0.2.330: "if popovic drives away and mission ends, you should see him get in the car and drive away with the driver - never
    /// just end abruptly"). The engine decides the loss on the tick Popovic sits in his car with his unturned driver beside it (Game.ValeTimeline: Vale InCar,
    /// Fail vale-leaves, why driven-away) and steps no more, so the driver stood on the kerb and the car stayed in its bay under the end. That is the
    /// outcome, read as it is; the leaving is the view's, as our getaway's is (HostViews.Leaving), on the wall clock from the frame the view first sees it:
    ///   1. Popovic, at his door, steps into the car (he walks into its side and is gone inside the body: no fade).
    ///   2. His driver walks round to the driver's door on the street side, the near end of the car if he stands on the kerb side, and gets in.
    ///   3. A beat (the doors shut: the model's doors don't open, the beat stays), then the car pulls out along the site's way out from where it stands
    ///      (the pickup's or the service bay's, HostViews.SiteWayOut), steering round the cars drawn or waiting behind one (HostViews.LeavingSteer),
    ///      on out of the frame, gone.
    /// The fail camera follows it and holds the end card until it has gone (HandoverHost.EndPlayout); a run seen again (the fail camera's copy, then the
    /// run itself) does not play it twice. Read only: the engine's car, its people and the hash never see it.</summary>
    public sealed partial class HostViews
    {
        const float BoardWalk = 1.4f, BoardBeat = 0.8f, OfficialTop = 12f; // m/s to the door; s from the last one in to the pull-out; m/s on its way out
        readonly ViewDrive official = new() { key = "car/official" };
        Game officialFor; float officialT; Vector3 valeAt, driverAt, carAt, carFwd, carRight; readonly List<Vector3> driverWay = new();
        bool valeIn, driverIn; float driverS; (object start, int tick) officialDone;

        /// <summary>The run ended with Popovic driven away: he is in, his driver beside the car (or already at the wheel), the car in its bay.</summary>
        static bool DrivenAway(Game g) => g != null && !g.Range && g.Over && g.Cause?.Why == "driven-away" && g.Vale == Sim.ValeAt.InCar && !g.Driver.Down;
        /// <summary>His leaving is playing: people walking to the car, the beat, or the car still in the city's view.</summary>
        public bool OfficialLeavingNow => officialFor != null && officialFor == host.Game && !official.Done;
        /// <summary>Where the leaving is (for the camera): the car once it rolls, else the car's bay.</summary>
        public Vector3? OfficialLeavingAt => OfficialLeavingNow ? things.TryGetValue("car/official", out var th) && th.t ? th.t.position : carAt : null;
        /// <summary>For checks and captures: how far the car has been drawn along its way out (m), and who is in.</summary>
        public string OfficialLeavingState => officialFor == null ? "" : $"{(valeIn ? "popovic in" : "popovic boarding")}, {(driverIn ? "driver in" : "driver walking")}, {official.s:0.0} of {official.len:0} m";

        void OfficialLeaving()
        {
            var g = host.Game;
            if (!DrivenAway(g)) { if (officialFor != null) { officialFor = null; official.heading = null; } return; }
            if (!things.TryGetValue("car/official", out var car) || !car.t) return;
            if (officialFor != g)
            {
                officialFor = g; officialT = 0f; beatAt = -1f; official.Reset(); driverWay.Clear(); driverS = 0f;
                carAt = car.t.position; carFwd = carModels.TryGetValue("car/official", out var cm) && cm ? Flat(cm.forward) : Vector3.forward; carRight = RightOf(carFwd);
                valeAt = things.TryGetValue("councillor", out var vt) && vt.t ? vt.t.position : carAt + carRight * 1.6f;
                driverAt = things.TryGetValue("driver", out var dt0) && dt0.t && dt0.shown ? dt0.t.position : carAt;
                bool done = officialDone.start == g.Start && officialDone.tick == g.Tick; // the fail camera's copy played it: the run as it ended shows it gone
                valeIn = done; driverIn = done || !(things.TryGetValue("driver", out var dt1) && dt1.shown);
                SiteWayOut(g, official.way); official.Measure();
                if (done) official.s = official.len;
                else
                {
                    // the driver's walk: to the street side (away from Popovic's door), round the near end of the car if he stands on the kerb side
                    float kerb = Mathf.Sign(Vector3.Dot(Flat(valeAt - carAt), carRight)); if (kerb == 0f) kerb = 1f;
                    var (half, rad, cz) = cm ? Dims(cm) : (1.45f, 0.9f, 0f); float halfLen = half + rad;
                    var street = carRight * -kerb; var door = carAt + carFwd * (cz + 0.3f) + street * (rad + 0.55f);
                    driverWay.Add(driverAt);
                    if (Vector3.Dot(Flat(driverAt - carAt), street) < rad)
                    {
                        float end = Vector3.Dot(Flat(driverAt - carAt), carFwd) >= 0f ? 1f : -1f;
                        var past = carAt + carFwd * (cz + end * (halfLen + 0.7f));                       // past the near end of the car, on his side, then across
                        driverWay.Add(past - street * (rad + 0.55f)); driverWay.Add(past + street * (rad + 0.55f));
                    }
                    driverWay.Add(door); driverWay.Add(carAt + carFwd * (cz + 0.3f) + street * 0.1f);
                    for (int i = 0; i < driverWay.Count; i++) driverWay[i] = new Vector3(driverWay[i].x, carAt.y, driverWay[i].z);
                    Debug.Log($"HostViews: Popovic is driven away: he gets in, his driver walks {WayLen(driverWay):0.0} m round to his door, the car leaves {official.len:0} m");
                }
            }
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f); officialT += dt;
            var (_, rad2, _) = carModels.TryGetValue("car/official", out var cm2) && cm2 ? Dims(cm2) : (1.45f, 0.9f, 0f);
            // 1. Popovic steps in: from his door into the car's side; gone once inside its body
            if (!valeIn && things.TryGetValue("councillor", out var vv) && vv.t)
            {
                var to = carAt; var d = Flat(to - valeAt); float step = Mathf.Min(d.magnitude, BoardWalk * 0.7f * dt);
                var at = valeAt + d.normalized * step; valeAt = at;
                bool inside = Mathf.Abs(Vector3.Dot(Flat(at - carAt), carRight)) < rad2 * 0.7f;
                if (inside) { valeIn = true; things["councillor"] = (vv.t, at, at, false); }
                else { things["councillor"] = (vv.t, at - d.normalized * BoardWalk * 0.07f, at, true); faces["councillor"] = d.normalized; }
            }
            // 2. the driver walks round to his door and gets in (he starts as Popovic steps in)
            if (!driverIn && officialT > 0.3f && driverWay.Count >= 2 && things.TryGetValue("driver", out var dv) && dv.t)
            {
                driverS += BoardWalk * dt; var (at, dir) = WayAt(driverWay, driverS);
                bool inside = driverS >= WayLen(driverWay) - 0.05f || driverS > 0.5f && Mathf.Abs(Vector3.Dot(Flat(at - carAt), carRight)) < rad2 * 0.7f && Mathf.Abs(Vector3.Dot(Flat(at - carAt), carFwd)) < 2.5f;
                if (inside) { driverIn = true; things["driver"] = (dv.t, at, at, false); }
                else { things["driver"] = (dv.t, at - dir * BoardWalk * 0.1f, at, true); faces["driver"] = dir; }
            }
            if (!valeIn && !things.ContainsKey("councillor")) valeIn = true;
            if (!driverIn && driverWay.Count < 2) driverIn = true;
            // 3. both in: a beat, then the car pulls out and drives on out of view
            if (valeIn && driverIn && official.way.Count >= 2)
            {
                if (officialT < 0.01f || official.Done) { if (official.Done && car.shown) { things["car/official"] = (car.t, car.to, car.to, false); officialDone = (g.Start, g.Tick); } return; }
                if (official.s <= 0f && official.v <= 0f) { if (beatAt < 0f) beatAt = officialT; if (officialT - beatAt < BoardBeat) { things["car/official"] = (car.t, carAt, carAt, true); return; } }
                DriveFrame(g, official, OfficialTop, dt);
                if (official.Done) { officialDone = (g.Start, g.Tick); Debug.Log("HostViews: Popovic's car has driven out of view"); }
            }
            else things["car/official"] = (car.t, carAt, carAt, true);
        }
        float beatAt = -1f;

        static float WayLen(List<Vector3> w) { float l = 0f; for (int i = 1; i < w.Count; i++) l += Vector3.Distance(w[i - 1], w[i]); return l; }
    }
}
