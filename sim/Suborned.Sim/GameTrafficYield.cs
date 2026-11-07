// Brief 098 step 1, law L8 between the mission's cars and the street's (the brief: "Traffic yields to them by the same law (stop, queue). Make
// sure no seed leaves a mission car stuck behind traffic for good"). The mission's cars drive their own legs on their own timetables (tenets);
// traffic gives way to them (GameTrafficSense.cs: their next seconds are a car in the way; a junction they will cross is not entered; a driver
// pulls aside within their half for one coming at them). And a mission car is a car too: at each tick's start, one whose next place on its leg
// would meet a passing car that is on its way (not one that will never move) holds where it is that tick, its leg's clock waiting a tick, for at
// most traffic.yield.holdS in a row; then it drives on regardless, so traffic never holds a mission car for good. Nothing here runs without traffic.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        readonly SortedDictionary<string, int> missionHeld = new SortedDictionary<string, int>(StringComparer.Ordinal); // a mission car's ticks held in a row (hashed with the traffic)
        /// <summary>Brief 098 step 1 (host read, never hashed): how many ticks each mission car has waited for traffic this game, in all.</summary>
        public readonly SortedDictionary<string, int> MissionWaits = new SortedDictionary<string, int>(StringComparer.Ordinal);

        /// <summary>The tick's first act with traffic on: each mission car driving a leg holds a tick if its next place meets a passing car on its way.</summary>
        void MissionYieldStep()
        {
            if (tr == null || Range) return;
            foreach (var id in CarIds)
            {
                if (!MissionDriving(id)) { missionHeld.Remove(id); continue; }
                var was = MissionCarAt(id, Tick - 1); // where it stood last tick, and where its leg takes it now
                var now = MissionCarAt(id, Tick); if (!now.HasValue || !was.HasValue || now.Value.at == was.Value.at) { missionHeld.Remove(id); continue; }
                var me = CarGeo.Rect(now.Value.at, now.Value.h, R.CarLenCm / 2, R.CarWCm / 2); bool meets = false;
                int cap = T(tr.HoldS);
                foreach (var c in Traffic)
                {
                    if (ForGood(c)) continue; var (at, h) = TrafficPose(c); if (!CarGeo.PolysMeet(me, CarGeo.Rect(at, h, c.HalfLen, c.HalfW))) continue;
                    meets = true; if (c.WaitsFor == id) cap = Math.Min(cap, T(tr.PassWaitS)); // a driver waiting for this very car: it goes first after waitS
                } // a ducked driver's car it waits out; a wreck it never would
                int held = missionHeld.TryGetValue(id, out var n) ? n : 0;
                if (!meets || held >= cap) { if (!meets) missionHeld.Remove(id); else missionHeld[id] = held + 1; continue; } // past its cap it drives on: never held for good
                missionHeld[id] = held + 1; MissionWaits[id] = (MissionWaits.TryGetValue(id, out var w) ? w : 0) + 1;
                switch (id) // its leg's clock waits a tick
                {
                    case "car:getaway": getawayStart++; foreach (var op in Ops) if (op.InCar && op.Busy != null) op.BusyEnd++; break;
                    case "car:official": valeDriveStart++; break;
                    case "car:rivals": ccStart++; break;
                }
            }
        }
        /// <summary>The mission car drives one of its legs now (not standing, not just due to start one).</summary>
        bool MissionDriving(string id) => id switch
        {
            "car:getaway" => getawayPath != null,
            "car:official" => valeDriveStart >= 0 && carToService != null && carDrive != null && !carStoppedAt.HasValue && !lentLeft && Tick < CarInBayTick,
            "car:rivals" => CarMoving,
            _ => false,
        };
        /// <summary>A passing car that stands for good: disabled, burning or blown, abandoned, pulled up at a lockdown (a ducked driver sits up again).</summary>
        bool ForGood(TrafficCar c) => CarDisabled(c.Id) || c.Mode == TrafficMode.Abandoned || c.Mode == TrafficMode.Held;
        /// <summary>The drive cells the passing cars standing as a mission car's leg starts cover (stopped, or for good), grown by a sedan's half width
        /// and the clearance, for that leg to go round (Grid.CarRoute); null without traffic or with none, so every leg is as before.</summary>
        bool[]? TrafficStillMask()
        {
            if (tr == null) return null;
            bool[]? m = null; var g = S.Drive; int grow = R.CarWCm / 2 + R.CarClearCm;
            foreach (var c in Traffic) if (TrafficStill(c) || c.V == 0) { var (at, h) = TrafficPose(c); CoverCells(g, R, at, h, grow, (i, j) => { if (g.FreeCell(i, j)) (m ??= new bool[g.Nx * g.Ny])[i * g.Ny + j] = true; }, c.HalfLen, c.HalfW); }
            return m;
        }
        internal void HashYield(Hash.H h) { if (missionHeld.Count == 0) return; h.S("yield"); foreach (var kv in missionHeld) h.S(kv.Key).I(kv.Value); }
    }
}
