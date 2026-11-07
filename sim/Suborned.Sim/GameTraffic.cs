// Brief 098 step 1 (law L8; Sky 2026-11-07: "our roads are empty, no reason can't have passing traffic"; "bring in more cars etc - the game
// then just does that, no long processing"): passing traffic in the Handover zone, when the start sets cars (the dial; Population.Cars). With
// no cars set nothing here runs, draws or is hashed, so every other run plays and hashes as before.
//  · The count is how many cars are on the zone's roads at a time on average. Enough slots run a car each: a trip in at a seeded mouth, out at
//    another (the lanes' shortest way), then a seeded pause (traffic.trips.offS) before the slot's next trip, a new car. The game starts with
//    traffic.trips.warmS of traffic already run (cars only: nobody walks before 0:00), so the roads are full at 0:00.
//  · Each tick, after the fight's shots and before the crowd's (GameTrafficLaw.cs reads the shots): the slots due start a trip where the lane
//    in is clear; the law's reactions; the junctions (each car asks for the turn ahead before it must brake for its stop line; asks are granted
//    in the order they were made, ties by id, when no turn it conflicts with is held, its way out of the box has room, nothing that will not
//    move on stands in it and no mission car will cross it soon); then every car drives (GameTrafficSense.cs) and the cars past their mouth leave.
// Every draw is the slot's own stream, (seed, slot, stage, trip), and a driver's temperament their own: a higher count adds slots and moves no
// car already there. Integers only.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        TrafficRules? tr; LaneNet? net; TrafficSlot[] slots = Array.Empty<TrafficSlot>(); DialKind? driverKind;
        /// <summary>Brief 098 step 1 (host read): the passing cars on the zone's roads and runways now, by id. Empty without traffic.</summary>
        public readonly List<TrafficCar> Traffic = new List<TrafficCar>();
        /// <summary>Traffic runs: the start set cars (more than none) and the dial's rules and lanes are loaded.</summary>
        public bool TrafficOn => tr != null;
        int TrafficAsked => Start.Counts.TryGetValue(Population.Cars, out var n) ? n : 0;
        /// <summary>Brief 098 step 1 (host read, never hashed): the sum over ticks of the cars inside the window, and the ticks counted (the mean).</summary>
        public long TrafficZoneSum, TrafficZoneTicks; public int TrafficTrips;
        bool trafficClosed;

        /// <summary>The dial's census of cars (InitWalkers): only when the start asks for cars; a count above the most plays the most.</summary>
        void CensusCars()
        {
            if (!Start.Counts.TryGetValue(Population.Cars, out var asked)) return;
            var t = dial!.Traffic; var c = new KindCensus { Kind = Population.Cars, Max = t.Max, Asked = asked, Count = Math.Min(t.Max, asked) };
            if (asked > t.Max) c.Note = $"{t.Max} is the most the dial allows";
            Census.Add(c);
            if (c.Count > 0 && dial.Lanes == null) throw new FormatException($"start.counts.cars: the dial's cars need their lanes ({LaneNet.File}); this host loaded none (brief 098 step 1)");
        }

        /// <summary>The end of the constructor: the slots for the count, then warmS of traffic run (cars only), so 0:00 starts on a busy street.</summary>
        void InitTraffic()
        {
            int n = dial == null ? 0 : Math.Min(dial.Traffic.Max, TrafficAsked); if (n <= 0 || Range) return;
            tr = dial!.Traffic; net = dial.Lanes!; net.Conflicts(tr.MaxW, tr.ClearCm);
            driverKind = new DialKind { Kind = "people", Role = tr.DriverRole, Job = tr.DriverJob, IdPrefix = tr.DriverId, Name = tr.DriverName, Allow = tr.DriverAllow };
            // a slot's share of time on the zone's roads: the routes' ticks inside the window over a trip and a pause, every pair of mouths alike
            long zone = 0, trip = 0; int pairs = 0;
            foreach (var a in net.Mouths) foreach (var b in net.Mouths) { if (a == b) continue; var r = net.Route(a, b)!; zone += RouteTicks(r, r.InMm, r.OutMm); trip += RouteTicks(r, 0, r.Path.Length); pairs++; }
            long cycle = (trip + (long)pairs * T(tr.OffMinS + tr.OffMaxS) / 2) / pairs, inZone = Math.Max(1, zone / pairs);
            int count = (int)Math.Min(4 * tr.Max, (n * cycle + inZone - 1) / inZone);
            slots = new TrafficSlot[count];
            for (int k = 0; k < count; k++) slots[k] = new TrafficSlot { Index = k + 1, Next = -T(tr.WarmS) + (int)(XDraw(SlotId(k + 1), "first") % (uint)Math.Max(1, cycle)) };
            for (int t = -T(tr.WarmS); t < 0; t++) TrafficTick(t, true);
            TrafficFlags(0);
        }
        /// <summary>A route's ticks between two of its mm at its lanes' speeds (a turn at the speed of the lane it leads to).</summary>
        static long RouteTicks(TrafficRoute r, long from, long to)
        {
            long t = 0;
            foreach (var (lane, a, b) in r.Lanes) { long lo = Math.Max(a, from), hi = Math.Min(b, to); if (hi > lo) t += (hi - lo) / Math.Max(1, lane.Speed); }
            foreach (var (turn, a, b) in r.Turns) { long lo = Math.Max(a, from), hi = Math.Min(b, to); if (hi > lo) t += (hi - lo) / Math.Max(1, turn.To.Speed); }
            return Math.Max(1, t);
        }
        string SlotId(int slot) => tr!.IdPrefix + slot.ToString(slots.Length >= 1000 ? "0000" : "000", System.Globalization.CultureInfo.InvariantCulture);
        string CarIdOf(TrafficSlot s, int trip) => "car:" + SlotId(s.Index) + "-" + trip.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
        bool IsTrafficId(string id) => tr != null && id.StartsWith("car:" + tr.IdPrefix, StringComparison.Ordinal);
        /// <summary>A passing car by its id, or null (none of that id is on the roads now).</summary>
        public TrafficCar? TrafficCarById(string id) { if (!IsTrafficId(id)) return null; foreach (var c in Traffic) if (c.Id == id) return c; return null; }

        void TrafficStep() { if (tr != null && !Over) TrafficTick(Tick, false); }

        /// <summary>One tick of traffic at `now` (negative in the warm-up, when nobody walks and nothing is alarmed).</summary>
        void TrafficTick(int now, bool warm)
        {
            if (!warm && !trafficClosed && Alert >= tr!.LockAlert) { trafficClosed = true; Note("traffic.closed", "The roads into the district are closed: no car comes in."); if (tr.LockStop) foreach (var c in Traffic) if (InZone(c) && c.Mode == TrafficMode.Drive) { c.Mode = TrafficMode.Held; c.ModeTick = now; } }
            foreach (var s in slots) if (s.Car == null && now >= s.Next && !(trafficClosed && !warm)) TrySpawn(s, now, warm);
            var w = TrafficWorldAt(now, warm);
            if (!warm) TrafficAlarms(now); // GameTrafficLaw.cs: gunfire heard, a drawn gun seen
            TrafficJunctions(now, w);
            foreach (var c in Traffic) DriveCar(c, now, w, warm);
            for (int i = Traffic.Count - 1; i >= 0; i--)
            {
                var c = Traffic[i]; if (!c.Gone) continue;
                var s = slots[c.Slot - 1]; if (s.Car == c) { s.Car = null; s.Next = now + T(XBand(SlotId(s.Index), "off", tr!.OffMinS, tr.OffMaxS, c.Trip)); }
                Traffic.RemoveAt(i);
            }
            TrafficFlags(now);
            if (!warm) { foreach (var c in Traffic) if (InZone(c)) TrafficZoneSum++; TrafficZoneTicks++; }
        }

        /// <summary>A slot's next trip: a seeded mouth in and another out, a seeded kind, a driver of their own; it starts only where the lane in is clear
        /// for its length and gap (else it waits a tick).</summary>
        void TrySpawn(TrafficSlot s, int now, bool warm)
        {
            int trip = s.Trips + 1; var (route, kind) = TripDraws(s.Index, trip);
            int hl = tr!.LenCm[kind] / 2, hw = tr.WCm[kind] / 2;
            if (!LaneClear(route.Lanes[0].lane, 2L * hl * 10 + tr.GapCm * 10L)) { s.Next = now + Hz; return; } // its mouth's lane is full: it tries again in a second
            var id = CarIdOf(s, trip);
            var p = new Person { Id = tr.DriverId + id.Substring(4 + tr.IdPrefix.Length), Job = tr.DriverJob };
            p.Name = tr.DriverName + " " + s.Index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "." + trip.ToString(System.Globalization.CultureInfo.InvariantCulture);
            p.Temperament = DialTemperament(driverKind!, p.Id, People2!); var tm = R.Temp(p.Temperament); for (int q = 0; q < 4; q++) p.Sc[q] = tm.Start[q];
            var c = new TrafficCar { Id = id, Model = tr.Models[kind], Slot = s.Index, Trip = trip, Kind = kind, HalfLen = hl, HalfW = hw, Cover = tr.Cover[kind], Route = route, Mm = hl * 10L, V = route.Lanes[0].lane.Speed, Mode = TrafficMode.Drive, BornAt = now, Driver = p };
            s.Car = c; s.Trips = trip; TrafficTrips += warm ? 0 : 1;
            int at = Traffic.FindIndex(x => string.CompareOrdinal(x.Id, id) > 0); if (at < 0) Traffic.Add(c); else Traffic.Insert(at, c);
        }

        /// <summary>A slot's trip as its own draws make it (seed, slot, stage, trip): its route (a mouth in, another out) and its kind.</summary>
        (TrafficRoute route, int kind) TripDraws(int slot, int trip)
        {
            var sid = SlotId(slot); var ms = net!.Mouths;
            int mi = (int)(XDraw(sid, "in", trip) % (uint)ms.Count), mo = (int)(XDraw(sid, "out", trip) % (uint)(ms.Count - 1)); if (mo >= mi) mo++;
            int sum = 0; foreach (var x in tr!.Share) sum += x; int pick = (int)(XDraw(sid, "kind", trip) % (uint)sum), kind = 0;
            while (kind < tr.Share.Length - 1 && pick >= tr.Share[kind]) { pick -= tr.Share[kind]; kind++; }
            return (net.Route(ms[mi], ms[mo])!, kind);
        }
        /// <summary>Brief 098 step 1 (test and host read; nothing changes): what a slot's trip will be, from its own draws: the mouths in and out, the
        /// kind, and its driver's temperament. The same for a slot whatever the count (a higher count adds slots, never moves one).</summary>
        public (string from, string to, string model, string temperament) TrafficTripOf(int slot, int trip)
        {
            if (tr == null || slot < 1 || slot > slots.Length) return ("", "", "", "");
            var (r, kind) = TripDraws(slot, trip); var id = "car:" + SlotId(slot) + "-" + trip.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
            return (r.From.Id, r.To.Id, tr.Models[kind], DialTemperament(driverKind!, tr.DriverId + id.Substring(4 + tr.IdPrefix.Length), People2!));
        }
        /// <summary>Brief 098 step 1 (host read): the slots the count runs (more than the count: some of each slot's time is off the zone's roads).</summary>
        public int TrafficSlots => slots.Length;

        /// <summary>The car's centre and heading now: on its route's line, `Lat` cm to its right.</summary>
        internal (Pt at, Pt h) TrafficPose(TrafficCar c)
        {
            var p = c.Route.Path; long mm = Math.Max(0, Math.Min(p.Length, c.Mm)); var on = p.At(mm); var h = p.Heading(mm);
            if (c.Lat == 0) return (on, h);
            long n = Math.Max(1, G.Isqrt((long)h.X * h.X + (long)h.Y * h.Y));
            return (new Pt(on.X + (int)(h.Y * c.Lat / n), on.Y + (int)(-h.X * c.Lat / n)), h); // the block frame's right of h is (h.y, -h.x)
        }
        /// <summary>The car's centre lies inside the window: it is on the zone's roads (the count's sense).</summary>
        bool InZone(TrafficCar c) => c.Mm >= c.Route.InMm && c.Mm <= c.Route.OutMm;

        // ---------------------------------------------------------------- junctions

        /// <summary>Release the turns left behind; every car that will need its next turn before it could stop asks; asks granted oldest first.</summary>
        void TrafficJunctions(int now, TrafficWorld w)
        {
            var asks = new List<(TrafficCar c, int k)>();
            foreach (var c in Traffic)
            {
                if (c.Turn >= 0 && c.Rear >= c.Route.Turns[c.Turn].exit) c.Turn = -1;
                int k = NextTurn(c); if (k < 0 || k == c.Turn || c.Turn >= 0) continue;
                var (_, enter, _) = c.Route.Turns[k];
                if (c.Front >= enter) { c.Turn = k; c.AskedAt = -1; continue; } // already over its line (a warm-up's or a reroute's): it holds what it is in
                if (!StopsFor(c) || enter - c.Front > StopMm(c.V) + c.V + 2000) continue; // asks only once it would have to brake for the line
                if (c.AskedAt < 0) c.AskedAt = now; asks.Add((c, k));
            }
            asks.Sort((a, b) => a.c.AskedAt != b.c.AskedAt ? a.c.AskedAt.CompareTo(b.c.AskedAt) : string.CompareOrdinal(a.c.Id, b.c.Id));
            var waited = new List<Turn>(); // turns whose drivers have waited traffic.pass.waitS: crossing traffic lets them in first
            foreach (var (c, k) in asks)
            {
                var t = c.Route.Turns[k].turn; string why = "";
                foreach (var x in waited) if (x.J == t.J && t.J.Conflict[x.Index, t.Index]) { why = "yield"; break; }
                if (why == "") why = TurnFree(c, k, now, w);
                if ((why == "yield" || why.StartsWith("held", StringComparison.Ordinal)) && now - c.AskedAt >= T(tr!.PassWaitS)) waited.Add(t); // waiting for the box itself (not for room past it)
                if (why == "") { c.Turn = k; c.AskedAt = -1; c.Refused = -1; c.RefusedAt = -1; continue; }
                if (why == "still") { if (c.Refused != k) { c.Refused = k; c.RefusedAt = now; } } else if (c.Refused == k) { c.Refused = -1; c.RefusedAt = -1; }
                // (held:<id>, room, mission: wait at the line)
            }
        }
        /// <summary>The route's next turn not yet left behind (its index), or -1.</summary>
        static int NextTurn(TrafficCar c) { var ts = c.Route.Turns; for (int k = 0; k < ts.Count; k++) if (ts[k].exit > c.Rear && k != c.Turn) return k; return -1; }
        /// <summary>Whether a car stops for what is ahead at all (a car whose driver bailed, ducked or pulled up asks for nothing).</summary>
        bool StopsFor(TrafficCar c) => c.Mode == TrafficMode.Drive || c.Mode == TrafficMode.Flee || c.Mode == TrafficMode.Brake;
        /// <summary>The mm a car at v (mm a tick) needs to stop at the brake (traffic.drive.brakeCmS2).</summary>
        long StopMm(int v) { long b = Math.Max(1, tr!.BrakeCmS2 / 10); return (long)v * (v + b) / (2 * b); }

        /// <summary>Why car c may not take its route's turn k now: "" (it may), "held" (a conflicting turn is held), "room" (no room past the box),
        /// "mission" (a mission car will cross it soon), "still" (something that will not move on stands in it or its way out).</summary>
        string TurnFree(TrafficCar c, int k, int now, TrafficWorld w)
        {
            var (t, enter, exit) = c.Route.Turns[k];
            foreach (var o in Traffic)
            {
                if (o == c || o.Turn < 0) continue; var (ot, oEnter, _) = o.Route.Turns[o.Turn];
                if (ot.J == t.J && t.J.Conflict[ot.Index, t.Index] && o.Rear - oEnter <= t.J.ClearAt[ot.Index, t.Index]) return "held:" + o.Id; // a car on a crossing turn not yet clear of this one's way
            }
            if (Overtaking(t.To)) return "room"; // a car overtaking the other way is out in that lane
            long need = 2L * c.HalfLen * 10 + tr!.GapCm * 10L; // room past the box for this car, and for each car granted into the same lane still before it
            foreach (var o in Traffic) { if (o == c || o.Turn < 0) continue; var (ot, _, oExit) = o.Route.Turns[o.Turn]; if (ot.To == t.To && o.Rear < oExit) need += 2L * o.HalfLen * 10 + tr.GapCm * 10L; }
            long room = exit + need;
            var (hit, still) = Occupied(c, enter, room, w);
            if (still) return "still";
            if (hit) return "room";
            int cross = (int)Math.Min(T(20), (room - c.Front) / Math.Max(Math.Max(100, c.V), t.To.Speed / 2) + 1); // the ticks to clear it, pulling away to half its lane's speed
            if (MissionCrosses(c, enter, room, now, T(tr.AheadS) + cross)) return "mission";
            return "";
        }
    }
}
