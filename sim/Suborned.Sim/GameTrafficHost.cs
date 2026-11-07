// Brief 098 step 1 (law L8): the passing cars for the host (Latch draws them: each car's model, pose, state and driver every tick) and for the
// checkpoint hash (no bytes unless the start set cars). Reads only: nothing here changes the game.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>One passing car as the host draws it this tick: its id (a target: car:&lt;slot&gt;-&lt;trip&gt;), its model kind (sedan, van, scooter:
    /// out/world/harbour-district.presentation.json models car/*, van/box, scooter/hover), its centre and heading (block frame, cm; the heading a
    /// direction, never (0, 0)), its footprint (cm), its speed (cm/s), its state (moving, stopped, disabled, burning, blown), what its driver is
    /// doing (drive, brake, duck, flee, held, abandoned), the driver's id and whether they are at the wheel, and whether it is on the zone's
    /// roads (inside the window) or on a runway beyond.</summary>
    public sealed class TrafficView
    {
        public string Id = "", Model = "", State = "", Mode = "", Driver = ""; public Pt At, Heading; public int Length, Width, Speed; public bool DriverIn, InZone, Standing;
    }

    public sealed partial class Game
    {
        /// <summary>Brief 098 step 1 (host read, never hashed): every passing car now, in id order. Empty without traffic.</summary>
        public List<TrafficView> TrafficNow()
        {
            var l = new List<TrafficView>(Traffic.Count);
            foreach (var c in Traffic)
            {
                var (at, h) = TrafficPose(c); var t = CarOf(c.Id);
                string state = t != null && t.Blown ? "blown" : t != null && t.BurningAt >= 0 ? "burning" : t != null && t.DisabledAt >= 0 ? "disabled" : c.V > 0 ? "moving" : "stopped";
                l.Add(new TrafficView { Id = c.Id, Model = c.Model, State = state, Mode = c.Mode.ToString().ToLowerInvariant(), Driver = c.Driver.Id, At = at, Heading = h, Length = 2 * c.HalfLen, Width = 2 * c.HalfW, Speed = c.V, DriverIn = c.Out == null, InZone = InZone(c), Standing = c.Standing });
            }
            return l;
        }
        /// <summary>Brief 098 step 1 (host read): how many passing cars are on the zone's roads (inside the window) now.</summary>
        public int TrafficInZone() { int n = 0; foreach (var c in Traffic) if (InZone(c)) n++; return n; }
        /// <summary>Brief 098 step 1 (host read): a passing car's driver, by the driver's id or the car's (at the wheel or out of it), or null.</summary>
        public Person? TrafficDriver(string id) { foreach (var c in Traffic) if (c.Driver.Id == id || c.Id == id) return c.Driver; return null; }

        /// <summary>Brief 098 step 1 (host and test read, never hashed): what holds a passing car now, in plain words: what is ahead in its corridor and
        /// how far, the turn it waits for and why, the closed road. "" for a car that is on its way, or none of that id.</summary>
        public string TrafficWhy(string id)
        {
            var c = TrafficCarById(id); if (c == null || tr == null) return "";
            if (CarDisabled(c.Id)) return "disabled"; if (c.Mode != TrafficMode.Drive && c.Mode != TrafficMode.Flee) return c.Mode.ToString().ToLowerInvariant();
            var w = TrafficWorldAt(Tick, false); var parts = new List<string>();
            var (d, kind, still, _) = Corridor(c, w, c.Front, tr.LookCm * 10L, c.LatGoal);
            if (d != long.MaxValue) parts.Add($"{new[] { "", "a person", "a body", "a car", "a mission car's way", "the kerb", "a mission car" }[kind]} {d / 1000.0:0.0} m ahead{(still ? " (will not move on)" : "")}");
            int k = NextTurn(c); if (k >= 0 && k != c.Turn) { var (t, enter, _) = c.Route.Turns[k]; var why = TurnFree(c, k, Tick, w); parts.Add($"turn {t.Id} {(enter - c.Front) / 1000.0:0.0} m ahead: {(why == "" ? "free" : why)}"); if (why.StartsWith("held:", StringComparison.Ordinal) && TrafficCarById(why.Substring(5)) is TrafficCar o) { var (oat, _) = TrafficPose(o); parts.Add($"[{o.Id} at {oat} v {o.V} {o.Mode}: {TrafficWhyShort(o, w)}]"); } }
            if (trafficClosed && c.Front < c.Route.InMm) parts.Add("the road in is closed");
            return string.Join("; ", parts);
        }

        string TrafficWhyShort(TrafficCar c, TrafficWorld w) { var (d, kind, still, _) = Corridor(c, w, c.Front, tr!.LookCm * 10L, c.LatGoal); return d == long.MaxValue ? "clear ahead" : $"{new[] { "", "a person", "a body", "a car", "a mission car's way", "the kerb", "a mission car" }[kind]} {d / 1000.0:0.0} m ahead{(still ? " (still)" : "")}"; }

        internal void HashTraffic(Hash.H h)
        {
            if (tr == null) return;
            h.S("traffic").I(slots.Length).Z(trafficClosed).I(TrafficTrips);
            foreach (var s in slots) h.I(s.Next).I(s.Trips).S(s.Car?.Id ?? "");
            foreach (var c in Traffic)
            {
                h.S(c.Id).S(c.Model).S(c.Route.Id).I(c.Mm).I(c.V).I(c.Lat).I(c.LatGoal).I((int)c.Mode).I(c.ModeTick).I(c.StoppedAt).I(c.Quiet).I(c.Turn).I(c.AskedAt).I(c.Refused).I(c.RefusedAt).I(c.BlockedAt);
                h.Z(c.Flees).P(c.Alarm).S(c.Driver.Temperament).Z(c.Out != null).Z(c.Standing).Z(c.Settled).S(c.WaitsFor).I(c.Back).I(c.BackedAt).I(c.PlannedAt);
            }
            HashYield(h);
        }
    }
}
