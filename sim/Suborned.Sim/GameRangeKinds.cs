// The fight range's cars and bystanders (Keel, 2026-10-21; Sky: "Make sure the fight lab is up to date"), so every fight rule of engine
// 0.18 can be tried on the range. Range only: a Handover run has no range car and no bystander, and none of this is read there.
//  · Cars (tuning.md §10.7, the rulebook's car.target, as in the Handover): the spawn order {do: spawn, target: car, to: car | car:<k>,
//    count: 1..(car slots), with: police | rivals | bystander} puts a car on the site's car slots (spawn:car:<k>, tools/sim/range.ts: a
//    car-box's footprint, long side across the firing line, nose +y; to "car" takes the first free slots, "car:<k>" that one), its id
//    car:range-<n>. It is shot like any car: disabled at car.target.disabledAt (its occupant gets out), burning at 0, exploding
//    car.target.burn later (everyone within car.target.blast.m with a line takes car.target.blast.damage, the cell included; ×healthScale in
//    the lab). Its cover points (the site's, of car/slot-<k>) stand only while a car stands on the slot and go when it explodes (CoverNow).
//    A wreck stays until a new car is spawned on its slot. The walk grid is the floor's: a car does not block a walk (the range is a
//    graybox; Latch draws it, nothing walks through it on purpose).
//  · The occupant gets out when the car is disabled, on the side away from the cell: police an officer that holds there (the car's own
//    cover point if one shields it from the mark), rivals a rival of the car's own spawn (it waits its aim, then fights), a bystander who
//    runs at once.
//  · Bystanders: {do: spawn, target: bystanders, to: near | eff | beyond | cover, count: 1..6} onto the same slots as hostiles, unarmed,
//    weapons.health.civilian (×healthScale in the lab). They react as the street's crowd does (rulebook crowd, Pulse §4), with the range's
//    two far points as their refuges (range:entry, range:retreat): startled by a shot within crowd.scatter or an agent's drawn weapon
//    within witness range in sight, they stand crowd.startle ticks, then run at crowd.run to the refuge (the crowd's rule: within
//    crowd.fleeMax, ending farther from the shooter, nearest; else the farthest), are safe there (off the floor), and walk back to their
//    slot crowd.quiet after the last shot. One hit and they run at once. An agent's shot they see (within witness range, a clear line) makes
//    them a witness of that agent (Saw), which the street would carry to a phone: the range has no reports, Alert or Trail, so it is
//    shown, never charged. A fire order on a bystander is a civilian's: taken only when forced (Ctrl), refused civilian-target otherwise.
//  · Nothing in the range is traced (the shots, the blast, a bystander down): it stays a fight with nothing of the mission in it.
// Host reads (read-only, never hashed): RangeCars, RangeBystanders, RangeSlotList, CoverNow, RangeKinds, RangeCarWith, RangeLayouts.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>A range car as the host draws it. Read-only.</summary>
    public sealed class RangeCarView
    {
        public string Id = "";                 // car:range-<n>
        public int Slot;                       // its car slot (spawn:car:<k>)
        public Pt At, Facing;                  // where it stands; its nose (+y: the long side across the firing line)
        public int Health, Max, DisabledAt;    // DisabledAt: the health at or below which it is disabled (×healthScale in the lab)
        public string State = "";              // ok, disabled, burning, wrecked
        public int BlastIn = -1;               // ticks until it explodes while burning, else -1
        public int BlastCm;                    // the blast ring's radius
        public string With = "", Occupant = ""; // who sits in it (police, rivals, bystander; "" nobody) and, once out, their id
        public bool Inside;                    // the occupant is still inside
    }

    /// <summary>A range bystander as the host draws it. Read-only.</summary>
    public sealed class RangeBystanderView
    {
        public string Id = "";                 // bystander-<n>
        public Pt Pos, Facing;
        public int Health, Max;
        public string State = "";              // standing, startled, fleeing, safe (off the floor), returning, down
        public string Saw = "";                // the agent they saw fire ("" none): a witness
        public bool Moving, Down;
    }

    /// <summary>A spawn slot of the range's layout, as a spawn panel lists it. Read-only.</summary>
    public sealed class RangeSlotView
    {
        public string Placement = "";          // near, eff, beyond, cover (people), car (cars)
        public int K;                          // 1..
        public Pt At;
        public int DistanceCm;                 // from the mark (the centre of the cell's line)
        public bool Held;                      // a spawn there now would not fit
    }

    /// <summary>A range bystander (engine state; hashed in HashRange).</summary>
    public sealed class RangeCiv
    {
        public string Id = ""; public Gun Gun = null!; public Pt Home, At, From; public Path? Run; public int RunStart, Speed, Until, Wave;
        public string St = "", Saw = "";        // St: "" standing, startled, fleeing, safe, returning
        public Pt Pos(int tick) => Run == null ? At : Run.At((long)Math.Max(0, tick - RunStart) * Speed);
        public bool Moving(int tick) => Run != null && (long)Math.Max(0, tick - RunStart) * Speed < Run.Length;
    }

    public sealed partial class Game
    {
        /// <summary>Everything the range's spawn order can place (the hostile sides, then bystanders, then a car).</summary>
        public static readonly string[] RangeKinds = { "police", "rivals", "response", "bystanders", "car" };
        /// <summary>Who may sit in a spawned car (the order's `with`; "" nobody).</summary>
        public static readonly string[] RangeCarWith = { "police", "rivals", "bystander" };
        /// <summary>The range's layouts (Engine.RangeSiteFiles).</summary>
        public static IEnumerable<string> RangeLayouts => Engine.RangeSiteFiles.Keys;
        /// <summary>The car slots of this range's layout (spawn:car:1..n).</summary>
        public int RangeCarSlots { get; private set; }
        static readonly Pt CarNose = new Pt(0, 100); // every range car: its long side across the firing line, nose +y (tools/sim/range.ts)

        sealed class RangeCar { public string Id = "", With = "", Occupant = ""; public int Slot, Wave; public bool Replaced; }
        readonly List<RangeCar> rangeCars = new List<RangeCar>();
        readonly List<RangeCiv> rangeCivs = new List<RangeCiv>();
        readonly List<(string by, Pt from)> rangeShots = new List<(string, Pt)>(); // this tick's shots (Resolve), for the bystanders; never hashed (derived)
        int rangeCarSeq, rangeCivSeq;

        RangeCiv? RangeCivOf(string id) { foreach (var c in rangeCivs) if (c.Id == id) return c; return null; }
        RangeCar? RangeCarOf(string id) { foreach (var c in rangeCars) if (c.Id == id) return c; return null; }
        /// <summary>A target the range spawned: a hostile, a bystander, or a car not cleared by a newer one on its slot.</summary>
        bool RangeTarget(string id) => IsFightHostile(id) || RangeCivOf(id) != null || (RangeCarOf(id) is RangeCar c && !c.Replaced);
        Pt? RangeCarAt(string id) => RangeCarOf(id) is RangeCar c && !c.Replaced ? S.At($"spawn:car:{c.Slot}") : (Pt?)null;
        string RangeCarCover(string id) => RangeCarOf(id) is RangeCar c ? $"car/slot-{c.Slot}" : "";
        static bool IsSlotCover(CoverPoint p) => p.Of.StartsWith("car/slot-", StringComparison.Ordinal);

        void InitRangeKinds()
        {
            while (S.HasPlace($"spawn:car:{RangeCarSlots + 1}")) RangeCarSlots++;
            for (int k = 1; k <= RangeCarSlots; k++) if (!S.Cover.Exists(p => p.Of == $"car/slot-{k}")) throw new FormatException($"range site: car slot {k} has no cover point (tools/sim/range.ts)");
            if (S.Cover.Exists(IsSlotCover)) liveCover = S.Cover.FindAll(p => !IsSlotCover(p)); // no car stands yet: no slot's cover (the same list, in order, as a site without car slots)
        }

        /// <summary>The cover points standing now (host read: the cover marks to draw). Never hashed.</summary>
        public List<CoverPoint> CoverNow() => new List<CoverPoint>(LiveCover);

        /// <summary>The cover standing: the site's, less the car slots with no car standing (none, or blown).</summary>
        void RangeCoverNow()
        {
            var up = new HashSet<string>(StringComparer.Ordinal);
            foreach (var c in rangeCars) if (!c.Replaced && !(CarOf(c.Id) is CarTarget ct && ct.Blown)) up.Add($"car/slot-{c.Slot}");
            liveCover = S.Cover.FindAll(p => !IsSlotCover(p) || up.Contains(p.Of));
        }

        bool CarSlotHeld(int k) { foreach (var c in rangeCars) if (c.Slot == k && !c.Replaced && !(CarOf(c.Id) is CarTarget ct && ct.Blown)) return true; return false; }

        string SpawnCar(Order o)
        {
            if (RangeCarSlots == 0) return No("no-place", "spawn: this layout has no car slot");
            if (o.With.Length > 0 && Array.IndexOf(RangeCarWith, o.With) < 0) return No("target-unknown", $"spawn: '{o.With}' can't sit in a car ({string.Join(", ", RangeCarWith)})");
            var slots = new List<int>(); int n = o.Count == 0 ? 1 : o.Count;
            if (o.Point.HasValue) return No("no-place", "spawn: a car goes on a car slot (car, or car:<k>)");
            if (o.To == "car" || o.To.Length == 0)
            {
                if (n < 1 || n > RangeCarSlots) return No("no-place", $"spawn: count {n} is outside 1..{RangeCarSlots} (the car slots)");
                for (int k = 1; k <= RangeCarSlots && slots.Count < n; k++) if (!CarSlotHeld(k)) slots.Add(k);
                if (slots.Count < n) return No("no-place", $"spawn: {slots.Count} of the {RangeCarSlots} car slots are free, {n} asked");
            }
            else if (o.To.StartsWith("car:", StringComparison.Ordinal) && int.TryParse(o.To.Substring(4), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int k1) && k1 >= 1 && k1 <= RangeCarSlots)
            {
                if (n != 1) return No("no-place", "spawn: one car on one slot");
                if (CarSlotHeld(k1)) return No("no-place", $"spawn: car slot {k1} is held");
                slots.Add(k1);
            }
            else return No("no-place", $"spawn: '{o.To}' is not a car slot (car, or car:1..{RangeCarSlots})");
            rangeWaves++;
            var ids = new List<string>();
            foreach (var k in slots)
            {
                foreach (var old in rangeCars) if (old.Slot == k) old.Replaced = true; // a wreck on the slot is cleared
                var c = new RangeCar { Id = $"car:range-{++rangeCarSeq}", Slot = k, With = o.With, Wave = rangeWaves };
                rangeCars.Add(c); ids.Add(c.Id);
                cars[c.Id] = new CarTarget { Id = c.Id, Gun = NewGun(c.Id, null, false, R.HealthCar), At = S.At($"spawn:car:{k}") };
            }
            RangeCoverNow();
            Note("range.spawn", $"{slots.Count} car{(slots.Count == 1 ? "" : "s")}{(o.With.Length > 0 ? " with " + o.With : "")} on car slot {string.Join(", ", slots)}: {string.Join(", ", ids)}.");
            return "";
        }

        string SpawnBystanders(Order o)
        {
            string at = o.To; int n = o.Count == 0 ? 1 : o.Count;
            if (o.Point.HasValue || Array.IndexOf(RangePlacements, at) < 0) return No("no-place", $"spawn: '{at}' is not a placement of the range ({string.Join(", ", RangePlacements)})");
            if (n < 1 || n > RangeSlots) return No("no-place", $"spawn: count {n} is outside 1..{RangeSlots}");
            var slots = new List<int>();
            for (int k = 1; k <= RangeSlots && slots.Count < n; k++) if (!SlotHeld(RangeSlot(at, k))) slots.Add(k);
            if (slots.Count < n) return No("no-place", $"spawn: {slots.Count} of the {RangeSlots} {at} slots are free, {n} asked");
            rangeWaves++;
            var ids = new List<string>();
            foreach (var k in slots) { var c = NewBystander(RangeSlot(at, k)); rangeCivs.Add(c); ids.Add(c.Id); }
            Note("range.spawn", $"{n} bystanders at {at}: {string.Join(", ", ids)}.");
            return "";
        }

        RangeCiv NewBystander(Pt home) { var id = $"bystander-{++rangeCivSeq}"; return new RangeCiv { Id = id, Gun = NewGun(id, null, false, R.HealthCivilian), Home = home, At = home, From = home, Wave = rangeWaves }; }

        /// <summary>The car is disabled (CarDisable): its occupant gets out on the side away from the cell, and acts.</summary>
        void RangeCarOut(CarTarget ct)
        {
            var c = RangeCarOf(ct.Id); if (c == null || c.With.Length == 0 || c.Occupant.Length > 0) return;
            var car = S.At($"spawn:car:{c.Slot}"); var mark = RangeMark; int sec = Tick / Hz;
            // the car's own cover point on its far side that shields against the mark, else the nearest free ground 1.8 m out on that side
            CoverPoint? cp = null;
            foreach (var p in LiveCover)
                if (p.Of == $"car/slot-{c.Slot}" && !coverTaken.ContainsKey(p.Id) && CoverRules.Protects(p, mark, sec, R.PointBlankCm) && (cp == null || G.D2(p.At, car + new Pt(180, 0)) < G.D2(cp.At, car + new Pt(180, 0)))) cp = p;
            var exit = cp?.At ?? S.Grid.NearestFree(car + new Pt(180, 0)) ?? car;
            if (c.With == "bystander")
            {
                var b = NewBystander(exit); b.Wave = c.Wave; rangeCivs.Add(b); c.Occupant = b.Id;
                RangeCivFlee(b, exit, Tick, ct.By.Length > 0 && PosOf(ct.By) is Pt sp ? sp : mark);
            }
            else
            {
                bool rival = c.With == "rivals"; string prefix = rival ? "rival-" : "officer-";
                int num = 0; foreach (var h0 in Hostiles) if (h0.Id.StartsWith(prefix, StringComparison.Ordinal)) num++;
                var gun = NewGun(prefix + (num + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), PistolW, !rival, rival ? R.HealthRival : R.HealthOfficer);
                gun.Engaged = true;
                var h = new Hostile { Id = gun.Id, At = exit, Home = exit, Speed = rival ? rivSpeed : secSpeed, Present = true, Gun = gun, Wave = c.Wave };
                Hostiles.Add(h); c.Occupant = gun.Id; rangeClear = false;
                if (cp != null) TakeCover(gun, cp);
                if (rival) { gun.St = "alerted"; gun.Until = gun.NextShot = Tick + AimNewT; } else gun.St = "hold";
            }
            Note("range.car-out", $"{c.Occupant} gets out of the {CarName(c.Id)}.");
        }

        // ---------------------------------------------------------------- bystanders, each tick

        /// <summary>Flee now from `from` to the range's refuge (the crowd's rule over range:entry and range:retreat).</summary>
        void RangeCivFlee(RangeCiv c, Pt pos, int tick, Pt from)
        {
            long mine = G.D2(pos, from), max2 = (long)R.CrowdFleeMaxCm * R.CrowdFleeMaxCm; Pt? best = null; long bd = long.MaxValue;
            foreach (var id in new[] { "range:entry", "range:retreat" }) // id order
            {
                var at = S.Stand(id); long d = G.D2(pos, at);
                if (d > max2 || G.D2(at, from) <= mine) continue;
                if (d < bd) { bd = d; best = at; }
            }
            if (!best.HasValue) { long far = -1; foreach (var id in new[] { "range:entry", "range:retreat" }) { var at = S.Stand(id); long d = G.D2(at, from); if (d > far) { far = d; best = at; } } }
            c.At = pos; c.From = from; c.Run = RouteOf(pos, best!.Value); c.RunStart = tick; c.Speed = R.CrowdRunSpeed; c.St = "fleeing";
        }

        /// <summary>One tick of the range's bystanders (after the fight's shots and the cars): witness, startle, flee, safe, back.</summary>
        void RangeCivStep()
        {
            if (rangeCivs.Count == 0) { rangeShots.Clear(); return; }
            long scatter2 = (long)R.CrowdScatterCm * R.CrowdScatterCm;
            int startled = 0;
            foreach (var c in rangeCivs)
            {
                if (c.Gun.Down) continue;
                var pos = c.Pos(Tick);
                if (c.St != "safe" && c.Saw.Length == 0)
                    foreach (var (by, from) in rangeShots)
                        if (IsOpId(by) && G.Within(pos, from, witnessRange) && Sees(pos, from)) { c.Saw = by; Note("range.witness", $"{c.Id} sees the {by} fire."); break; }
                switch (c.St)
                {
                    case "": case "returning":
                        {
                            Pt? from = null;
                            foreach (var (_, s) in rangeShots) if (G.D2(s, pos) <= scatter2) { from = s; break; }
                            if (!from.HasValue) foreach (var op in Ops) if (op.Gun.Drawn && !op.Down && !op.Inside && G.Within(op.Pos, pos, witnessRange) && Sees(op.Pos, pos)) { from = op.Pos; break; }
                            if (from.HasValue) { c.At = pos; c.Run = null; c.RunStart = Tick; c.From = from.Value; c.St = "startled"; c.Until = Tick + R.CrowdStartleTicks; startled++; break; }
                            if (c.St == "returning" && !c.Moving(Tick)) { c.At = c.Home; c.Run = null; c.St = ""; Note("range.bystander-back", $"{c.Id} is back."); }
                            break;
                        }
                    case "startled":
                        if (Tick < c.Until) break;
                        RangeCivFlee(c, pos, Tick, LastShotTick >= c.RunStart ? lastShooter : c.From);
                        Note("range.bystander-runs", $"{c.Id} runs for safety.");
                        break;
                    case "fleeing":
                        if (c.Moving(Tick)) break;
                        c.At = pos; c.Run = null; c.St = "safe"; c.Until = Tick;
                        Note("range.bystander-safe", c.Saw.Length > 0 ? $"{c.Id} is safe, and saw the {c.Saw} fire." : $"{c.Id} is safe.");
                        break;
                    case "safe":
                        if (Tick - Math.Max(LastShotTick, c.Until) < T(R.CrowdQuietS)) break;
                        c.Run = RouteOf(c.At, c.Home); c.RunStart = Tick; c.Speed = opSpeed; c.St = "returning";
                        break;
                }
            }
            rangeShots.Clear();
            if (startled > 0) Note("range.bystander-startled", $"{startled} bystander{(startled == 1 ? " is" : "s are")} startled.");
        }

        /// <summary>A shot (or the blast) lands on a bystander: hit, they run at once; down, they lie where they fell.</summary>
        void RangeCivHit(RangeCiv c, bool down, string by)
        {
            var pos = c.Pos(Tick);
            if (down) { c.At = pos; c.Run = null; c.St = ""; Note("range.bystander-down", $"{c.Id} is down."); return; }
            Note("range.bystander-hit", $"{c.Id} is hit.");
            if (c.St != "fleeing") RangeCivFlee(c, pos, Tick, PosOf(by) ?? resolving?.From ?? RangeMark);
        }

        // ---------------------------------------------------------------- reads for the host (read-only)

        /// <summary>Every car the range spawned and has not cleared, in spawn order (a wreck stays until a car is spawned on its slot).</summary>
        public List<RangeCarView> RangeCars()
        {
            var l = new List<RangeCarView>();
            foreach (var c in rangeCars)
            {
                if (c.Replaced) continue; var ct = CarOf(c.Id)!; var g = ct.Gun;
                l.Add(new RangeCarView { Id = c.Id, Slot = c.Slot, At = S.At($"spawn:car:{c.Slot}"), Facing = CarNose, Health = g.Health, Max = g.Max, DisabledAt = R.CarDisabledAt * HealthScale,
                    State = ct.Blown ? "wrecked" : ct.BurningAt >= 0 ? "burning" : ct.DisabledAt >= 0 ? "disabled" : "ok", BlastIn = ct.BurningAt >= 0 && !ct.Blown ? Math.Max(0, ct.BlastAt - Tick) : -1,
                    BlastCm = R.CarBlastCm, With = c.With, Occupant = c.Occupant, Inside = c.With.Length > 0 && c.Occupant.Length == 0 });
            }
            return l;
        }

        /// <summary>Every bystander the range spawned, in spawn order.</summary>
        public List<RangeBystanderView> RangeBystanders()
        {
            var l = new List<RangeBystanderView>();
            foreach (var c in rangeCivs)
            {
                var pos = c.Pos(Tick); bool moving = c.Moving(Tick) && !c.Gun.Down;
                var face = moving && c.Run != null ? c.Run.Heading((long)Math.Max(0, Tick - c.RunStart) * c.Speed) : c.St == "startled" ? c.From - pos : RangeMark - pos;
                if (face == new Pt(0, 0)) face = new Pt(-100, 0);
                l.Add(new RangeBystanderView { Id = c.Id, Pos = pos, Facing = face, Health = c.Gun.Health, Max = c.Gun.Max, State = c.Gun.Down ? "down" : c.St.Length == 0 ? "standing" : c.St, Saw = c.Saw, Moving = moving, Down = c.Gun.Down });
            }
            return l;
        }

        /// <summary>Every spawn slot of this layout: the people's (near, eff, beyond, cover; 1..6), then the car slots; with whether it is held.</summary>
        public List<RangeSlotView> RangeSlotList()
        {
            var l = new List<RangeSlotView>(); if (!Range) return l; var mark = RangeMark;
            foreach (var p in RangePlacements) for (int k = 1; k <= RangeSlots; k++) { var at = RangeSlot(p, k); l.Add(new RangeSlotView { Placement = p, K = k, At = at, DistanceCm = (int)Dist(at, mark), Held = SlotHeld(at) }); }
            for (int k = 1; k <= RangeCarSlots; k++) { var at = S.At($"spawn:car:{k}"); l.Add(new RangeSlotView { Placement = "car", K = k, At = at, DistanceCm = (int)Dist(at, mark), Held = CarSlotHeld(k) }); }
            return l;
        }

        /// <summary>The cars and bystanders for the checkpoint hash: no bytes until one is spawned.</summary>
        void HashRangeKinds(Hash.H h)
        {
            if (rangeCars.Count > 0) { h.S("rcars"); foreach (var c in rangeCars) h.S(c.Id).I(c.Slot).S(c.With).S(c.Occupant).Z(c.Replaced).I(c.Wave); }
            if (rangeCivs.Count > 0)
            {
                h.S("rcivs");
                foreach (var c in rangeCivs) { h.S(c.Id).P(c.Pos(Tick)).S(c.St).I(c.Until).I(c.RunStart).I(c.Speed).P(c.At).P(c.Home).P(c.From).S(c.Saw).I(c.Wave); Hash.HashGun(h, c.Gun); }
            }
        }
    }
}
