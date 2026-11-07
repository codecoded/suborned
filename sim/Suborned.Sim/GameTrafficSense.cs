// Brief 098 step 1, law L8 (lanes and stopping): what a driver sees ahead and how they drive it. GameTraffic.cs calls it once a tick per car.
//  · The world at the tick's start (TrafficWorldAt): everyone on the street (the cell, the cast, the crowd, security, the crew, hostiles), the
//    bodies, every passing car's footprint, and the mission's cars where they are and, while they drive, where they will be over the next
//    traffic.yield.aheadS (their own timetabled legs): traffic gives way to them.
//  · The corridor: the car's own width plus traffic.drive.clearCm each side, along its route from its front, sampled every 50 cm up to lookCm.
//    The first thing in it is what the driver brakes for: a person (stopping peopleGapCm short), a body, a car (gapCm plus headwayDs of its
//    speed behind it), a mission car's next seconds; a junction's stop line until the turn is granted; at a lockdown the window's edge.
//  · The speed: at most the lane's (traffic.react.fleePm of it when fleeing, passCmS while easing round), up by accelCmS2 a second, never more
//    than lets it stop before what is ahead at brakeCmS2 (a sudden thing stops it at once: nobody is ever driven into).
//  · Something ahead that will not move on (a standing mission car, a wreck, an abandoned car, a ducked driver's car, a body), or a person
//    who stays in the road: the driver eases round it (GameTrafficPass.cs).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>A footprint in the traffic's world: where, which way, its half length and width (cm), the passing car it is (null: a mission car
        /// or a mission car's next seconds), whether it moves and whether it will not move on; and its box for a quick reject.</summary>
        sealed class TBox
        {
            public Pt At, H, From; public int HL, HW; public TrafficCar? Car; public bool Moving, Still, Sweep; public int X0, Y0, X1, Y1; public long N; public string Id = ""; // From: a sweep's mission car where it is now; Id: a mission car's
            public TBox(Pt at, Pt h, int hl, int hw) { At = at; H = h.X == 0 && h.Y == 0 ? new Pt(0, 100) : h; HL = hl; HW = hw; N = Math.Max(1, G.Isqrt((long)H.X * H.X + (long)H.Y * H.Y)); int r = hl + hw; X0 = at.X - r; X1 = at.X + r; Y0 = at.Y - r; Y1 = at.Y + r; }
            /// <summary>p within `r` cm of the footprint (an exact integer test in its own frame).</summary>
            public bool Near(Pt p, long r)
            {
                long dx = p.X - At.X, dy = p.Y - At.Y, along = Math.Abs(dx * H.X + dy * H.Y) / N, across = Math.Abs(dx * H.Y - dy * H.X) / N;
                long ox = Math.Max(0, along - HL), oy = Math.Max(0, across - HW); return ox * ox + oy * oy <= r * r;
            }
        }
        sealed class TrafficWorld { public readonly List<Pt> People = new List<Pt>(), Bodies = new List<Pt>(); public readonly List<TBox> Cars = new List<TBox>(); }
        readonly TrafficWorld tw = new TrafficWorld();

        TrafficWorld TrafficWorldAt(int now, bool warm)
        {
            var w = tw; w.People.Clear(); w.Bodies.Clear(); w.Cars.Clear();
            if (!warm) TrafficPeople(w.People, w.Bodies);
            foreach (var c in Traffic) { var (at, h) = TrafficPose(c); w.Cars.Add(new TBox(at, h, c.HalfLen, c.HalfW) { Car = c, Moving = c.V > 0, Still = TrafficStill(c) }); }
            int hl = R.CarLenCm / 2, hw = R.CarWCm / 2;
            foreach (var (id, at, h, moving) in MissionCarsNow()) w.Cars.Add(new TBox(at, h, hl, hw) { Moving = moving, Still = !moving, Id = id });
            foreach (var id in CarIds) // where each will be: its leg, or the one its timetable starts soon
            {
                Pt from = default; bool on = false; foreach (var (mid, at, _, _) in MissionCarsNow()) if (mid == id) { from = at; on = true; }
                for (int dt = 5; dt <= T(tr!.AheadS); dt += 5) if (MissionCarAt(id, Tick + dt) is var f && f.HasValue) { var b = new TBox(f.Value.at, f.Value.h, hl, hw) { Moving = true, Sweep = true, Id = id }; b.From = on ? from : f.Value.at; w.Cars.Add(b); }
            }
            return w;
        }
        /// <summary>A passing car that will not move on: disabled, burning or blown, abandoned, its driver ducked or pulled up for good.</summary>
        bool TrafficStill(TrafficCar c) => CarDisabled(c.Id) || c.Mode == TrafficMode.Abandoned || c.Mode == TrafficMode.Duck || c.Mode == TrafficMode.Held;

        /// <summary>Everyone on the street now, on their feet or down: read from the poses the game already holds (no gun is made, nothing moves).</summary>
        void TrafficPeople(List<Pt> alive, List<Pt> bodies)
        {
            foreach (var op in Ops) { if (op.Inside || op.InCar || op.LeftMap || op.CarriedBy.HasValue) continue; (op.Down ? bodies : alive).Add(op.Pos); }
            foreach (var (_, at) in OnTheStreet()) alive.Add(at); // the cast by role, the crowd (drivers out of their cars among them)
            if (!PatrolDown && PatrolPose() is var pp && pp.HasValue) alive.Add(pp.Value.pos);
            for (int k = 0; k < SecGuns.Length; k++) if (!SecGuns[k].Down && SecOfficerPose(k) is var sp && sp.HasValue) alive.Add(sp.Value.pos);
            foreach (var u in Units) if (!u.Gun.Down && u.OnStreet(Tick) && UnitPose(u) is var up && up.HasValue) alive.Add(up.Value.pos);
            foreach (var h in Hostiles) if (h.Present && !h.Down) alive.Add(h.Pos(Tick));
            foreach (var m in Crew) if (!m.Fight && Up(m) && m.OnFoot && MemberPose(m) is var mp && mp.HasValue) alive.Add(mp.Value.pos);
            foreach (var b in Bodies) if (b.Open && !b.Carrier.HasValue) bodies.Add(BodyPos(b));
        }
        /// <summary>The mission's cars on the street now: the getaway car, the official car, the rival car; where they point and whether they drive.</summary>
        IEnumerable<(string id, Pt at, Pt h, bool moving)> MissionCarsNow()
        {
            if (CurStand != null) yield return ("car:getaway", GetawayCarPos(), GetawayHeading(), getawayPath != null);
            if (!lentLeft) yield return ("car:official", CarPos(), OfficialHeading(), !OfficialStanding());
            if (RivalCarPose() is var rp && rp.HasValue) yield return ("car:rivals", rp.Value.pos, rp.Value.heading, CarMoving);
        }
        /// <summary>Where a mission car will be at `t`: along the leg it drives (its own profile), or at the start of the one its timetable begins by
        /// then (the rival car turning in at its entry, again after a break; the turned driver pulling out of the pickup with Vale aboard); null
        /// when it neither drives nor is due to.</summary>
        (Pt at, Pt h)? MissionCarAt(string id, int t)
        {
            switch (id)
            {
                case "car:getaway": if (getawayPath == null) return null; { int k = Math.Max(0, Math.Min(t - getawayStart, getawayProf!.Length - 1)); return (getawayPath.At(getawayProf[k]), getawayPath.Heading(getawayProf[k])); }
                case "car:official":
                    if (lentLeft || carStoppedAt.HasValue || OfficialOut) return null;
                    if (valeDriveStart >= 0 && carToService != null && carDrive != null) { if (t >= CarInBayTick) return null; long mm = CarDriveMm(t); return (carToService.At(mm), carToService.Heading(mm)); }
                    { int due = T(valeAtCarS) + T(R.ValeInS); if (!Driver.Turned || Driver.Down || Start.ValeWaits || (Vale != ValeAt.Leaving && Vale != ValeAt.Pickup) || t < due || Tick > due) return null; // due, not yet past: a drive that did not start is no car coming
                      officialOut ??= new Path(S.CarServicePath); officialProf ??= CarProfile(officialOut); int k = Math.Min(t - due, officialProf.Length - 1); return (officialOut.At(officialProf[k]), officialOut.Heading(officialProf[k])); }
                case "car:rivals":
                    if (CarMoving) { long mm = CcMm(t); return (ccPath!.At(mm), ccPath.Heading(mm)); }
                    if ((CarSt == CrewCar.Off && t >= CrewEntryNow && Tick <= CrewEntryNow && CrewEntryNow < INF) || (CarSt == CrewCar.Away && crewNext == "back" && t >= crewNextAt && Tick <= crewNextAt)) return (S.CarStand(S.RivalCarEntry), S.RivalFacingEntry); // due, not yet past: an entry that did not happen is no car coming
                    return null;
            }
            return null;
        }
        Path? officialOut; long[]? officialProf; // the turned driver's way round from the pickup (car.parks), for the street to see it coming

        // ---------------------------------------------------------------- the corridor

        const int TStep = 500; // mm between the corridor's samples
        string lastHit = ""; // the mission car the last corridor met, if it met one
        readonly List<Pt> qs = new List<Pt>(), hs = new List<Pt>(); readonly List<TBox> nearC = new List<TBox>(); readonly List<Pt> nearP = new List<Pt>(), nearB = new List<Pt>();
        /// <summary>The lateral offset (cm) the car will ride at `d` mm on: from `lat` toward `goal` at latCmS across for passCmS along.</summary>
        int LatAt(int lat, int goal, long d) { int gap = goal - lat; if (gap == 0) return lat; long m = Math.Max(0, d) * tr!.LatCmS / (tr.PassCmS * 10L); return lat + (int)(Math.Sign(gap) * Math.Min(Math.Abs(gap), m)); }

        /// <summary>The first thing in c's corridor from `from` mm on its route for `len` mm, riding toward `goal`: (mm from `from`, what: 0 nothing,
        /// 1 a person, 2 a body, 3 a car, 4 a mission car's next seconds; whether it will not move on; its speed if a car). Cars moving at least
        /// `skipFast` mm a tick are passed over (a way out's room: they go on); with `grid`, a sample whose footprint leaves the drive grid is a wall
        /// (kind 5). `latFrom`: the offset the car rides at now (its own, unless a test asks as if it rode elsewhere).</summary>
        (long d, int kind, bool still, int v) Corridor(TrafficCar c, TrafficWorld w, long from, long len, int goal, int skipFast = int.MaxValue, bool grid = false, int? latFrom = null, int wide = 0)
        {
            qs.Clear(); hs.Clear(); nearC.Clear(); nearP.Clear(); nearB.Clear();
            int n = (int)(len / TStep) + 1, x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue, lat0 = latFrom ?? c.Lat; long front = c.Front;
            Pt lastH = default; long hn = 1;
            c.Route.Path.Walk(from, TStep, n, (k, p, h) =>
            {
                int lat = lat0 == 0 && goal == 0 ? 0 : LatAt(lat0, goal, from - front + (long)k * TStep);
                if (lat != 0 && h != lastH) { hn = Math.Max(1, G.Isqrt((long)h.X * h.X + (long)h.Y * h.Y)); lastH = h; } // a segment's length, once a segment
                var q = lat == 0 ? p : new Pt(p.X + (int)(h.Y * lat / hn), p.Y + (int)(-h.X * lat / hn)); qs.Add(q); if (grid) hs.Add(h);
                x0 = Math.Min(x0, q.X); y0 = Math.Min(y0, q.Y); x1 = Math.Max(x1, q.X); y1 = Math.Max(y1, q.Y); return false;
            });
            if (qs.Count == 0) return (long.MaxValue, 0, false, 0); // past its route's end: the road runs on beyond the runway
            int r = c.HalfW + tr!.ClearCm, pr = r + R.CrowdOccludeCm + wide, br = r + 2 * R.CrowdOccludeCm, m = r + 400; lastHit = "";
            foreach (var p in w.People) if (p.X >= x0 - pr && p.X <= x1 + pr && p.Y >= y0 - pr && p.Y <= y1 + pr) nearP.Add(p);
            foreach (var p in w.Bodies) if (p.X >= x0 - br && p.X <= x1 + br && p.Y >= y0 - br && p.Y <= y1 + br) nearB.Add(p);
            var ch = c.Route.Path.Heading(c.Mm); var cf = c.Route.Path.At(Math.Min(c.Route.Path.Length, c.Front));
            foreach (var b in w.Cars)
            {
                if (b.Car == c || b.X1 < x0 - m || b.X0 > x1 + m || b.Y1 < y0 - m || b.Y0 > y1 + m || (b.Car != null && b.Car.V >= skipFast)) continue;
                if (b.Sweep && ((long)(b.From.X - cf.X) * ch.X + (long)(b.From.Y - cf.Y) * ch.Y < 0 || c.Turn >= 0)) continue; // a mission car coming up from behind, or the driver already given a junction: they drive on (out of its way)
                nearC.Add(b);
            }
            long pr2 = (long)pr * pr, br2 = (long)br * br;
            for (int k = 0; k < qs.Count; k++)
            {
                var q = qs[k]; long d = (long)k * TStep;
                foreach (var p in nearP) if (G.D2(q, p) <= pr2) return (d, 1, false, 0);
                foreach (var p in nearB) if (G.D2(q, p) <= br2) return (d, 2, true, 0);
                foreach (var b in nearC) if (b.Near(q, r)) { lastHit = b.Id; return (d, b.Sweep ? 4 : b.Car == null && b.Moving ? 6 : 3, b.Still, b.Car?.V ?? 0); } // 6: a mission car on its leg
                if (grid && !OnDriveGrid(q, hs[k], c)) return (d, 5, true, 0);
            }
            return (long.MaxValue, 0, false, 0);
        }
        /// <summary>A footprint at q heading h lies on the drive grid (its four corners, where they are inside the window).</summary>
        bool OnDriveGrid(Pt q, Pt h, TrafficCar c)
        {
            long n = Math.Max(1, G.Isqrt((long)h.X * h.X + (long)h.Y * h.Y));
            foreach (var (u, v) in new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) })
            {
                var p = new Pt(q.X + (int)((h.X * c.HalfLen * u + h.Y * c.HalfW * v) / n), q.Y + (int)((h.Y * c.HalfLen * u - h.X * c.HalfW * v) / n));
                if (S.Drive.InGrid(p) && !S.Drive.Free(p)) return false;
            }
            return true;
        }
        /// <summary>Somebody stands inside the car's own footprint (stepped into it while it stood): it does not move until they are out.</summary>
        bool SomeoneIn(TrafficCar c, TrafficWorld w)
        {
            var (at, h) = TrafficPose(c); var b = new TBox(at, h, c.HalfLen, c.HalfW); long r = tr!.ClearCm / 2;
            foreach (var p in w.People) if (b.Near(p, r)) return true;
            return false;
        }

        /// <summary>The stretch [from, to] of c's route (at its line) holds a car that is not moving on fast, or something that will not move on.</summary>
        (bool hit, bool still) Occupied(TrafficCar c, long from, long to, TrafficWorld w)
        {
            var (d, kind, still, _) = Corridor(c, w, from, to - from, 0);
            if (d == long.MaxValue || kind == 1) return (false, false); // people move: the box waits for them, not the ask
            return (true, still);
        }
        /// <summary>The first `len` mm of a mouth's lane in hold no passing car (a trip's start: nobody is put on top of another). Only a car on that
        /// lane can be there (it starts on a runway, 30 m out of the window, lanes 6 m apart): its rear along the lane says.</summary>
        bool LaneClear(Lane lane, long len)
        {
            foreach (var o in Traffic) { int li = o.Route.LaneAt(o.Mm); if (li < 0 || o.Route.Lanes[li].lane != lane) continue; if (o.Rear - o.Route.Lanes[li].from < len) return false; }
            return true;
        }
        /// <summary>A moving mission car comes within c's corridor between [from, to] of its route over the next `ticks` (its own timetable).</summary>
        bool MissionCrosses(TrafficCar c, long from, long to, int now, int ticks)
        {
            int hl = R.CarLenCm / 2, hw = R.CarWCm / 2, r = c.HalfW + tr!.ClearCm;
            foreach (var id in CarIds)
                for (int dt = 0; dt <= ticks; dt += 5)
                {
                    if (!(MissionCarAt(id, Tick + dt) is var f) || !f.HasValue) continue; // not driving then (a leg may yet start within the time)
                    var b = new TBox(f.Value.at, f.Value.h, hl, hw);
                    for (long mm = from; mm <= to; mm += TStep) if (b.Near(c.Route.Path.At(mm), r)) return true;
                }
            return false;
        }

        // ---------------------------------------------------------------- driving

        void DriveCar(TrafficCar c, int now, TrafficWorld w, bool warm)
        {
            if (c.Gone) return;
            if (CarDisabled(c.Id) || c.Mode == TrafficMode.Abandoned) { c.V = 0; return; }
            if (c.Mode == TrafficMode.Duck) { if (now - c.Quiet < T(R.CrowdQuietS)) { c.V = 0; return; } c.Mode = TrafficMode.Drive; c.ModeTick = now; }
            if (c.Back > 0) { BackStep(c, w, now); return; } // backing off to find room round what holds it (GameTrafficPass.cs)
            int hard = tr!.HardBrakeCmS2 / 10, accel = Math.Max(1, tr.AccelCmS2 / 10), brake = Math.Max(1, tr.BrakeCmS2 / 10);
            if (c.Mode == TrafficMode.Held) { c.V = Math.Max(0, c.V - hard); c.Mm += c.V; return; }
            int li = c.Route.LaneAt(c.Mm); var lane = li >= 0 ? c.Route.Lanes[li].lane : null;
            int speed = lane?.Speed ?? NextLaneSpeed(c), vmax = c.Mode == TrafficMode.Flee ? speed * tr.FleePm / 1000 : speed;
            if (c.Lat != 0 || c.LatGoal != 0) vmax = Math.Min(vmax, tr.PassCmS);
            // what is ahead: the corridor, the stop line of a turn not granted, the window's edge in a lockdown
            long look = tr.LookCm * 10L, best = long.MaxValue; int gapKind = 0; bool still = false; int aheadV = 0;
            var (d, kind, st, av) = Corridor(c, w, c.Front, look, c.LatGoal);
            c.WaitsFor = kind == 4 || kind == 6 ? lastHit : "";
            if (d != long.MaxValue) { best = Math.Max(0, d - TStep); gapKind = kind; still = st; aheadV = av; }
            if (SomeoneIn(c, w)) { best = 0; gapKind = 1; }
            long allowedMm = best == long.MaxValue ? long.MaxValue : best - Gap(c, gapKind);
            int k = NextTurn(c);
            if (k >= 0 && k != c.Turn) allowedMm = Math.Min(allowedMm, c.Route.Turns[k].enter - c.Front - 100);
            if (trafficClosed && !warm && c.Front < c.Route.InMm) allowedMm = Math.Min(allowedMm, c.Route.InMm - c.Front - 100);
            int allowed = allowedMm == long.MaxValue ? int.MaxValue : allowedMm <= 0 ? 0 : (int)Math.Min(int.MaxValue, G.Isqrt(2L * brake * allowedMm));
            int v = Math.Min(Math.Min(c.V + accel, vmax), allowed);
            if (c.Mode == TrafficMode.Brake) v = Math.Min(v, Math.Max(0, c.V - hard));
            c.V = Math.Max(0, v); c.Mm += c.V;
            if (c.V > 0 && c.Lat != c.LatGoal) // sideways only where the whole car is clear at the new offset (someone may have come alongside since it chose)
            { int sideways = Math.Max(1, c.V * tr.LatCmS / (tr.PassCmS * 10)), gap = c.LatGoal - c.Lat, nl = c.Lat + Math.Sign(gap) * Math.Min(Math.Abs(gap), sideways); if (Corridor(c, w, c.Rear, c.Front - c.Rear, nl, latFrom: nl).d == long.MaxValue) c.Lat = nl; }
            if (c.Mode == TrafficMode.Brake && c.V == 0 && now - c.ModeTick >= (c.Driver.Temperament == "fearful" ? R.CrowdStartleFearfulTicks : R.CrowdStartleTicks))
            { c.Mode = c.Flees ? TrafficMode.Flee : TrafficMode.Duck; c.ModeTick = now; if (!c.Flees) c.Quiet = Math.Max(c.Quiet, now); }
            // held up behind something that will not move on: ease round it; once the whole car is past, back to the line (GameTrafficPass.cs)
            bool driving = c.Mode == TrafficMode.Drive || c.Mode == TrafficMode.Flee;
            if ((gapKind == 4 || gapKind == 6) && lane != null && c.LatGoal == 0 && best <= 6000 && driving) PlanPass(c, w, lane, best); // a mission car coming at it: pull aside within its half if that lets it by
            else if (c.V > 0 && still && gapKind != 4 && lane != null && c.LatGoal == 0 && driving) PlanPass(c, w, lane, best, 0, true); // seen from afar: it eases out early, still rolling
            else if (c.V == 0 && (still || gapKind == 1) && best <= 4000 && driving)
            {
                if (c.BlockedAt < 0) c.BlockedAt = now; // a person who stays in the road 5 waitS: the driver eases round them, a body's width wider
                if (lane != null && now - c.BlockedAt >= T(tr.PassWaitS) * (gapKind == 1 ? 5 : 1)) { PlanPass(c, w, lane, best, gapKind == 1 ? R.CrowdOccludeCm * 2 : 0, true); if (c.LatGoal == 0) BackOff(c, lane, w); }
            }
            else if (c.V > 0) c.BlockedAt = -1;
            if (c.LatGoal != 0 && lane != null && (!(c.V == 0 && still) || LineWithin(c, StopMm(c.V) + c.V + Math.Abs(c.Lat) * (long)tr.PassCmS * 10 / Math.Max(1, tr.LatCmS) + 1000))
                && Corridor(c, w, c.Rear, (c.Front - c.Rear) + 2000, 0, latFrom: 0).d == long.MaxValue && (LineWithin(c, look) || Corridor(c, w, c.Rear, look + 2L * c.HalfLen * 10, 0, latFrom: 0).d == long.MaxValue)) c.LatGoal = 0; // back on its line once the whole car is clear there (before a junction, as soon as it is)
            if (c.Refused >= 0 && c.V == 0 && now - c.RefusedAt >= T(tr.PassWaitS)) Reroute(c, c.Refused, w, now);
            if (c.Front >= c.Route.Path.Length) c.Gone = true; // at its runway's end, 30 m out of the zone: it has left
        }
        /// <summary>The gap a driver keeps behind what is ahead (mm): a person or a body peopleGapCm, a car gapCm plus headwayDs of its own speed.</summary>
        long Gap(TrafficCar c, int kind) => kind == 1 || kind == 2 ? tr!.PeopleGapCm * 10L : kind == 3 || kind == 6 ? tr!.GapCm * 10L + (long)c.V * tr.HeadwayDs : kind == 4 ? tr!.GapCm * 10L : 0;
        static int NextLaneSpeed(TrafficCar c) { foreach (var (t, a, b) in c.Route.Turns) if (c.Mm >= a && c.Mm < b) return t.To.Speed; return c.Route.Lanes[c.Route.Lanes.Count - 1].lane.Speed; }

    }
}
