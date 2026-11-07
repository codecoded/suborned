// Brief 098 step 1, law L8 between the mission's cars and the street's. The mission's cars drive their own legs on their own timetables (tenets);
// traffic gives way to them (GameTrafficSense.cs: their next seconds are a car in the way; a junction they will cross is not entered; a driver
// pulls aside within their half for one coming at them). And a mission car is a car too.
// Brief 098 step 1b (Sky 2026-11-07, on "a queue boxes nobody in": "should be opposite - shot cars can get past, others block"):
//  · at each tick's start, a mission car whose next place on its leg would meet a passing car that has not been shot holds where it is, its leg's
//    clock waiting a tick, for as long as that car is in its way: ordinary traffic (moving, waiting, queued, its driver ducked) is solid, so a
//    jam can hold a mission car (step 1's holdS, after which it drove on through, is gone);
//  · one whose next place would meet a wreck (disabled, burning, blown, or abandoned at a shot) pushes it aside (GameTrafficShove.cs) and holds
//    only while it pushes;
//  · a passing car that stands in a mission car's way while waiting for that very car (each would wait for the other) is told to get out of its
//    way (TrafficCar.Clears; GameTrafficShove.cs ClearFor), so no two cars wait for each other for good.
// Every hold is kept for the host and the sweep (MissionHolds: who, from when, how long, and what held it). Nothing here runs without traffic.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>Brief 098 step 1b (host and sweep read, never hashed): one spell a mission car was held by passing traffic: the mission car, the tick
    /// it began, how many ticks it lasted (so far, while Open), what held it at its start (Why: moving, queued, junction, person, body, ducked,
    /// braking, fleeing, pulled-up, yields, wreck-pinned, pushing) and the passing car (By).</summary>
    public sealed class MissionHold { public string Car = "", Why = "", By = ""; public int From, Ticks; public bool Open; }

    public sealed partial class Game
    {
        readonly SortedDictionary<string, int> missionHeld = new SortedDictionary<string, int>(StringComparer.Ordinal); // a mission car's ticks held in a row (hashed with the traffic)
        /// <summary>Brief 098 step 1 (host read, never hashed): how many ticks each mission car has waited for traffic this game, in all.</summary>
        public readonly SortedDictionary<string, int> MissionWaits = new SortedDictionary<string, int>(StringComparer.Ordinal);
        /// <summary>Brief 098 step 1b (host and sweep read, never hashed): every spell a mission car was held by traffic this game, in the order they began.</summary>
        public readonly List<MissionHold> MissionHolds = new List<MissionHold>();

        /// <summary>The tick's first act with traffic on: each mission car driving a leg holds a tick if its next place meets a passing car that has
        /// not been shot; a wreck there it pushes aside.</summary>
        void MissionYieldStep()
        {
            if (tr == null || Range) return;
            foreach (var c in Traffic) if (c.Clears.Length > 0 && !missionHeld.ContainsKey(c.Clears)) c.Clears = ""; // the mission car it was in the way of has gone on
            foreach (var id in CarIds)
            {
                if (!MissionDriving(id)) { EndHold(id); continue; }
                var was = MissionCarAt(id, Tick - 1); // where it stood last tick, and where its leg takes it now
                var now = MissionCarAt(id, Tick); if (!now.HasValue || !was.HasValue || now.Value.at == was.Value.at) { EndHold(id); continue; }
                var me = CarGeo.Rect(now.Value.at, now.Value.h, R.CarLenCm / 2, R.CarWCm / 2); var before = CarGeo.Rect(was.Value.at, was.Value.h, R.CarLenCm / 2, R.CarWCm / 2);
                TrafficCar? solid = null, wreck = null; bool pinned = false;
                foreach (var c in Traffic)
                {
                    var (at, h) = TrafficPose(c); var them = CarGeo.Rect(at, h, c.HalfLen, c.HalfW); if (!CarGeo.PolysMeet(me, them)) continue;
                    if (CarGeo.PolysMeet(before, them) && G.D2(now.Value.at, at) > G.D2(was.Value.at, at)) continue; // touching already (a turn's swing) and moving apart: it drives on
                    if (!Shot(c)) { solid ??= c; continue; } // ordinary traffic is solid: it waits behind it, however long it stands
                    var (clear, moved) = Shove(c, id, now.Value.at, now.Value.h, me); // a wreck: pushed aside as it goes by
                    if (!clear) { wreck ??= c; if (!moved) pinned = true; }
                }
                var by = solid ?? wreck; if (by == null) { EndHold(id); continue; }
                int held = missionHeld.TryGetValue(id, out var n) ? n : 0;
                missionHeld[id] = held + 1; MissionWaits[id] = (MissionWaits.TryGetValue(id, out var w) ? w : 0) + 1;
                if (solid != null && solid.WaitsFor == id && solid.V == 0 && held + 1 >= T(tr.PassWaitS)) solid.Clears = id; // each waits for the other: the passing car makes room
                OpenHold(id, by, () => solid == null ? (pinned ? "wreck-pinned" : "pushing") : HoldWhy(solid, id));
                switch (id) // its leg's clock waits a tick
                {
                    case "car:getaway": getawayStart++; foreach (var op in Ops) if (op.InCar && op.Busy != null) op.BusyEnd++; break;
                    case "car:official": valeDriveStart++; break;
                    case "car:rivals": ccStart++; break;
                }
            }
        }
        /// <summary>A passing car that has been shot: disabled (burning and blown are disabled too), or abandoned by its driver at the shot. It can be
        /// got past: it boxes nobody in, and a mission car pushes it aside (Sky 2026-11-07: "shot cars can get past, others block").</summary>
        internal bool Shot(TrafficCar c) => CarDisabled(c.Id) || c.Mode == TrafficMode.Abandoned;
        /// <summary>What holds a mission car, in a word: what the passing car in its way is doing.</summary>
        string HoldWhy(TrafficCar c, string id)
        {
            if (c.WaitsFor == id) return "yields"; // it waits for this very car
            switch (c.Mode) { case TrafficMode.Duck: return "ducked"; case TrafficMode.Brake: return "braking"; case TrafficMode.Held: return "pulled-up"; case TrafficMode.Flee: if (c.V > 0) return "fleeing"; break; }
            if (c.V > 0) return "moving";
            int k = NextTurn(c); if (k >= 0 && k != c.Turn && c.Route.Turns[k].enter - c.Front <= 1500) return "junction";
            var (d, kind, _, _) = Corridor(c, TrafficWorldAt(Tick, false), c.Front, tr!.LookCm * 10L, c.LatGoal);
            return d == long.MaxValue ? "stopped" : kind == 1 ? "person" : kind == 2 ? "body" : kind == 4 || kind == 6 ? "yields" : "queued";
        }
        /// <summary>A held tick: the mission car's open spell runs on, or a new one begins (its reason read once, at its start).</summary>
        void OpenHold(string id, TrafficCar by, Func<string> why)
        {
            var last = MissionHolds.FindLast(x => x.Car == id);
            if (last != null && last.Open) { last.Ticks++; return; }
            MissionHolds.Add(new MissionHold { Car = id, From = Tick, Ticks = 1, Why = why(), By = by.Id, Open = true });
        }
        void EndHold(string id)
        {
            if (!missionHeld.Remove(id)) return;
            var last = MissionHolds.FindLast(x => x.Car == id); if (last != null) last.Open = false;
        }
        /// <summary>Brief 098 step 1b (host read): what holds a mission car now, in plain words ("" when nothing does).</summary>
        public string MissionHoldNow(string id)
        {
            var last = MissionHolds.FindLast(x => x.Car == id); if (last == null || !last.Open) return "";
            string what = last.Why switch
            {
                "yields" => "waits for us", "queued" => "queued", "junction" => "waiting at the junction", "person" => "stopped for someone in the road", "body" => "stopped at a body",
                "ducked" => "its driver ducked", "braking" => "braking", "pulled-up" => "pulled up", "fleeing" => "driving off", "moving" => "driving across", "pushing" => "a wreck being pushed aside",
                "wreck-pinned" => "a wreck that can't be pushed aside", _ => "stopped",
            };
            return $"held {(Tick - last.From + 1 + Hz - 1) / Hz} s by {last.By} ({what})";
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
        /// <summary>With traffic, the drive cells' way of travel (the lanes'), for a mission car's new leg to keep right; null without.</summary>
        (short[] fx, short[] fy)? TrafficField() => tr == null ? ((short[], short[])?)null : net!.Field(S.Drive);
        internal void HashYield(Hash.H h) { if (missionHeld.Count == 0) return; h.S("yield"); foreach (var kv in missionHeld) h.S(kv.Key).I(kv.Value); }
    }
}
