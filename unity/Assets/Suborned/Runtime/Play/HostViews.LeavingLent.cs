using System.Collections.Generic;
using UnityEngine;
using Game = Suborned.Sim.Game;
using Pt = Suborned.Sim.Pt;

namespace Suborned.Play
{
    /// <summary>Brief 096, the lend (engine 0.42.0, car.lend L6): the Go on the second cell car takes it out down its way while the mission runs on (Game.LentGone). The engine
    /// keeps no drive for it, so, as for our car (HostViews.Leaving), the leaving is the view's, on the wall clock from the frame the view first sees it.
    ///  · Popovic's car: from where it stands along the site's way out from the pickup or the service bay (whichever it stands on: Site.Ground.PickupWay, ServiceWay),
    ///    straight on past its end, out of the frame, gone.
    ///  · Engine 0.53.0 (brief 105, car.take): any car taken (Game.LentId: the rival car, a passing car): straight on from where it stood, the way it faced (the engine's
    ///    lane: a block straight ahead), on past it out of the frame. The rival car's and the passing car's own views stay up while it drives off (TakenLeaving): the
    ///    engine has them gone the tick of the Go.
    /// Read only.</summary>
    public sealed partial class HostViews
    {
        Game lentFor; readonly List<Vector3> lentWay = new(); float lentS, lentV, lentLen; Vector3? lentHeading; string lentKey = "car/official";
        Vector3 takenAt, takenDir = Vector3.forward; bool takenSeen;

        static string LentKeyOf(Game g) => g.LentId.StartsWith("car:") ? "car/" + g.LentId.Substring(4) : "car/official";
        /// <summary>The taken car (not Popovic's own) with this view key has left and its view is still driving off: its own view code must not hide or move it.</summary>
        bool TakenLeaving(string key)
        {
            var g = host.Game; if (g == null || g.Range || !g.Lent || !g.LentGone || g.LentId == "car:official" || LentKeyOf(g) != key) return false;
            return !(lentFor == g && lentLen > 0f && lentS >= lentLen);
        }

        void LentLeaving()
        {
            var g = host.Game;
            if (g == null || g.Range || !g.Lent) { lentFor = null; lentHeading = null; takenSeen = false; return; }
            lentKey = LentKeyOf(g);
            if (!g.LentGone) { lentFor = null; lentHeading = null; TakenPose(g); return; }
            if (!things.TryGetValue(lentKey, out var th)) return;
            if (lentFor != g) { lentFor = g; lentS = lentV = lentLen = 0f; lentHeading = null; if (g.LentId == "car:official") LentWay(g); else TakenWay(g); }
            if (lentWay.Count < 2) return;
            if (lentS >= lentLen) { if (th.shown) things[lentKey] = (th.t, th.to, th.to, false); return; }
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            lentV = PushCap(lentKey, Mathf.Min(LeaveTop, lentV + LeaveAccel * dt), dt); lentS += lentV * dt; // brief 098 step 1b: it brakes while it shoves a wreck aside
            var (at, dir) = Along(lentWay, lentS); things[lentKey] = (th.t, at, at, true); lentHeading = dir; // the car model faces it (CarFacing, RivalCarFacing, the passing car's heading)
            if (trafficCars.TryGetValue(g.LentId, out var cv)) { cv.HeadFrom = cv.HeadTo = dir; }
        }
        /// <summary>The taken car's drawn place and facing while it stands ours: where its drive off starts.</summary>
        void TakenPose(Game g)
        {
            if (g.LentId == "car:official" || !things.TryGetValue(lentKey, out var th) || !th.shown || !th.t) return;
            takenAt = th.t.position; takenSeen = true;
            Vector3? d = g.LentId == "car:rivals" ? rivalHeading : trafficCars.TryGetValue(g.LentId, out var cv) ? cv.HeadTo : (Vector3?)null;
            if (d is Vector3 v && v.sqrMagnitude > 1e-6f) { v.y = 0f; takenDir = v.normalized; }
        }
        /// <summary>A taken car's way: straight on from where it stood, a block (the engine's lane), then on out of the frame.</summary>
        void TakenWay(Game g)
        {
            lentWay.Clear(); if (!takenSeen) return;
            float block = 60f; try { block = g.S.GroundOf(g.R).BlockCm / 100f; } catch (System.Exception) { }
            lentWay.Add(takenAt); lentWay.Add(takenAt + takenDir * block); lentWay.Add(takenAt + takenDir * (block + LeaveBeyond));
            for (int i = 1; i < lentWay.Count; i++) lentLen += Vector3.Distance(lentWay[i - 1], lentWay[i]);
        }

        /// <summary>The site's way out the car stands on (the one whose line passes nearest it), from its nearest point on, then on past its end.</summary>
        void LentWay(Game g)
        {
            lentWay.Clear(); Pt car = g.CarPos(); Pt[] best = null; long bd = long.MaxValue; int from = 0;
            Suborned.Sim.Site.Ground gr = null; try { gr = g.S.GroundOf(g.R); } catch (System.Exception) { }
            if (gr == null) return;
            foreach (var way in new[] { gr.PickupWay, gr.ServiceWay })
            {
                if (way == null || way.Length < 2) continue;
                for (int i = 0; i < way.Length; i++) { long dx = way[i].X - car.X, dy = way[i].Y - car.Y, d = dx * dx + dy * dy; if (d < bd) { bd = d; best = way; from = i; } }
            }
            if (best == null || from >= best.Length - 1) return;
            var pts = new List<Vector3> { frame.World(car) };
            for (int i = from + 1; i < best.Length; i++) Add(pts, frame.World(best[i]));
            var end = frame.World(best[best.Length - 1]); var d0 = end - frame.World(best[best.Length - 2]); d0.y = 0f;
            if (d0.sqrMagnitude > 1e-4f) { var far = end + d0.normalized * LeaveBeyond; far.y = end.y; pts.Add(far); }
            for (int i = 0; i < 4; i++) Round(pts);
            lentWay.AddRange(pts); for (int i = 1; i < lentWay.Count; i++) lentLen += Vector3.Distance(lentWay[i - 1], lentWay[i]);
        }

        static (Vector3 at, Vector3 dir) Along(List<Vector3> way, float s)
        {
            for (int i = 1; i < way.Count; i++)
            {
                var a = way[i - 1]; var b = way[i]; float len = Vector3.Distance(a, b);
                if (s > len && i + 1 < way.Count) { s -= len; continue; }
                var d = b - a; d.y = 0f;
                return (Vector3.Lerp(a, b, len > 1e-4f ? Mathf.Clamp01(s / len) : 1f), d.sqrMagnitude > 1e-8f ? d.normalized : Vector3.forward);
            }
            return (way[way.Count - 1], Vector3.forward);
        }
    }
}
