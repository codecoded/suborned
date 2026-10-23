// The fight range (brief 029, Keel; Sky 2026-10-20: "a proper lab rather than in game - a sort of graybox shooting area with other NPCs
// firing"). A run on a range site (sim/data/range.site.json or range-dense.site.json, tools/sim/range.ts: mission "range") is the real
// engine with nothing of the mission in it:
//  · no schedule (no guard, car, rivals or Vale timeline), no watchers, patrol, crowd, takedowns, reports, Alert, Trail, Standing, seal or
//    close: the range is one fight from its first tick (FightBranch "range", FightOn) and nothing in it is traced;
//  · the cell of four at the near end (the site's crew:*), its orders as in the mission (move, drive, draw, holster, stance, fire, volley,
//    field, panic, cancel, carry a downed agent), the fight lab's presets as any run's start (Start.fightLab: today = off, lab = on);
//    any other verb is refused unknown-verb; a fire order names only what the range spawned (target-unknown otherwise);
//  · hostiles only by the spawn order {do: spawn, target: police|rivals|response, to: near|eff|beyond|cover, count: 1..6}, onto the
//    site's spawn slots (spawn:<to>:1..6, the first free ones), into the final fight's machine (GameAi.cs FightBody, GameKite.cs with the
//    lab kite): police are officers that hold the slot as their stand (trained, health-officer); rivals a crew that waits weapons.aimNew,
//    then engages or advances on the range's mark by the site's cover, and breaks (more than half of its own spawn down) for the far
//    corner; the response unit officers that walk in from the far end (range:entry) to the slot as their stand. Behind cover: the
//    hostile takes the cover point its slot stands on.
//  · bystanders and shootable cars by the same order (target bystanders | car): GameRangeKinds.cs;
//  · the car-contest rule (fight.lab.policeContest: the first officers' stands pulled into their ring of the getaway car) is the loud final
//    fight's, and the range has no getaway car: it is not applied. The AI's objective, the car it fights over in the Handover (site getaway.car),
//    is the range's mark (range:mark, the centre of the cell's line): rivals advance on it, a police body's kite cover is no farther from it
//    than its stand. No blocker, no hold, no Board or Go.
// Reads for the host (read-only): RangeHostiles (whom to draw) and RangeLines (the "numbers": every line of fire, its chance, and the rules
// in play on it in plain words); the cars, bystanders, slots and standing cover in GameRangeKinds.cs.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>A range hostile as the host draws it (brief 029). Read-only.</summary>
    public sealed class RangeHostile
    {
        public string Id = "", Side = "";      // Side: police (officer-n), rivals (rival-n), response (response-n)
        public Pt Pos, Facing;                 // Facing: toward its target, else along its walk, else toward the mark
        public int Health, Max, Wave;          // Wave: the spawn order it came with (1, 2, ...)
        public string State = "";              // the fight AI's state: alerted, advance, engage, hold, break, gone; "down"
        public string Target = "";             // whom it aims at ("" none)
        public string Cover = "";              // the cover point it holds ("" none)
        public bool Moving, Down, Gone;
    }

    /// <summary>One line of fire now (brief 029 "show the numbers"): shooter → target, the chance the next shot has, and every fight rule
    /// in play on it (engine 0.18: the settle, follow and locks, police answering every recent shooter, the pair's count, fire on the move),
    /// as fields and as one line of plain words (Text). Each agent to each hostile, bystander and car, then each hostile to each agent.
    /// Read-only, never hashed.</summary>
    public sealed class FireLine
    {
        public string Shooter = "", Target = "";
        public Pt From, To;
        public int DistanceCm;
        public string Band = "";               // point (inside weapons.pointBlank), eff (inside the shooter's effective ring), long (inside its max), out
        public bool Line;                      // within the shooter's max ring on a clear line (an agent can fire on it)
        public bool InReach;                   // the shooter would fire on it now: an agent, Line; a hostile, the fight AI's own foes in reach
                                               // (its effective ring, or its max ring when it is police answering this agent's fire)
        public bool Aimed;                     // the shooter's aim is on this target now
        public int Acc;                        // the chance of the next shot, per mille (0 with no line)
        public int Pips;                       // 1–4 with the lab's pips on, else 0
        public int Damage;                     // a hit's damage at this distance
        public string Cover = "none";          // the target's cover against this shooter as the shot counts it now: none, partial, full
        public string CoverId = "";            // the cover point that gives it (or that would, while the target moves)
        public bool CoverLost;                 // a point shields the target from this shooter but it is moving, so the shot ignores it
        public bool Moving;                    // the shooter is moving (walking or running): Gait says which
        public bool Untrained;                 // the shooter is untrained
        // engine 0.16–0.18 rules on this line
        public string Gait = "";               // the shooter's step now (fire on the move): stand, walk, run, run-away
        public bool TargetRunning;             // the target runs: harder to hit past point blank
        public int Count;                      // the pair's count toward its next hit, per mille: the shooter's aim on this target, kept for the
                                               // whole fight, between shots and between targets (a target it hasn't fired at starts at acc.start)
        public int ShotsToHit;                 // shots until the next hit at this chance and count (0: no chance)
        public bool AimKept;                   // the shooter fired on this target before and takes it back at that count
        public int ReadyIn;                    // ticks until the shooter can fire again (cadence, drawing, the settle); -1 holstered
        public bool Steadying;                 // the settle: it stopped running and waits aim-new before its next shot
        public string Mode = "";               // an agent's fire on this target: trigger, trigger-held, lock, follow, follow-held, return, free
        public string Lead = "";               // the agent it follows, or whose press locked it
        public bool Answers;                   // police this agent fired on in the last line-lapse (3 s): it answers out to its max ring
        public bool Bystander, Car;            // the target is a bystander (fired on only when the order is forced: the host's hold sends it) or a car
        public string Text = "";               // all of it in one line of plain words, for the numbers overlay
    }

    public sealed partial class Game
    {
        /// <summary>This run is on a fight range site (brief 029): the mission's rules are off, the fight's are on.</summary>
        public readonly bool Range;
        /// <summary>The verbs the range takes; any other is refused unknown-verb.</summary>
        public static readonly string[] RangeVerbs = { "move", "drive", "cancel", "draw", "holster", "cease", "stance", "fire", "volley", "field", "panic", "carry", "spawn", "withdraw", "trigger", "follow", "free" }; // engine 0.19.0: free (tuning.md §14)
        public static readonly string[] RangeSides = { "police", "rivals", "response" };
        public static readonly string[] RangePlacements = { "near", "eff", "beyond", "cover" };
        public const int RangeSlots = 6;
        int rangeWaves; bool rangeClear = true;
        const int SlotHeldCm = 100;

        /// <summary>The range's mark: the centre of the cell's line, from which every spawn distance is measured (site getaway.car).</summary>
        public Pt RangeMark => S.At(S.GetawayCar);
        /// <summary>A spawn slot's stand (placement near, eff, beyond, cover; k 1..6).</summary>
        public Pt RangeSlot(string placement, int k) => S.Stand($"spawn:{placement}:{k}");

        void InitRange()
        {
            foreach (var p in RangePlacements)
                for (int k = 1; k <= RangeSlots; k++)
                {
                    var id = $"spawn:{p}:{k}";
                    if (!S.HasPlace(id)) throw new FormatException($"range site: no spawn slot '{id}' (tools/sim/range.ts)");
                    if (p == "cover" && !S.Cover.Exists(c => c.At == S.Stand(id))) throw new FormatException($"range site: '{id}' stands on no cover point (unknown = reject)");
                }
            if (!S.HasPlace("range:entry")) throw new FormatException("range site: no 'range:entry' (where the response unit walks in)");
            InitRangeKinds(); // the car slots and their cover (GameRangeKinds.cs)
            FightBranch = "range"; FightStart = 0;
            Note("start", "The fight range: the cell at the near end, nothing else until something is spawned.");
        }

        void RangeTick()
        {
            if (!Over) RangeStep();
            if (!Over) Combat();
            if (!Over) CarsStep(); // a burning car blows (GameCars.cs; nothing while no car is spawned)
            if (!Over) RangeCivStep(); else rangeShots.Clear(); // the bystanders (GameRangeKinds.cs)
            if (Tick % 100 == 0 || Over) Checkpoints.Add((Tick, Hash.State(this)));
        }

        /// <summary>The range has no end: it notes when the last hostile up is down, broken or gone (range.clear), once until the next spawn.</summary>
        void RangeStep()
        {
            if (rangeClear) return;
            foreach (var h in Hostiles) if (!h.Down && h.Present && h.Gun.St != "break" && h.Gun.St != "gone") return;
            rangeClear = true; Note("range.clear", "Every hostile is down, broken or gone.");
        }

        static string SideOf(string id) => id.StartsWith("rival-", StringComparison.Ordinal) ? "rivals" : id.StartsWith("officer-", StringComparison.Ordinal) ? "police" : id.StartsWith("response-", StringComparison.Ordinal) ? "response" : "";

        /// <summary>A slot is held by a hostile up and on the floor standing on it, or (police, the response unit) holding it as its stand.</summary>
        bool SlotHeld(Pt p)
        {
            foreach (var h in Hostiles)
            {
                if (h.Down || !h.Present || h.Gun.St == "gone") continue;
                if (G.Within(h.Pos(Tick), p, SlotHeldCm) || (IsHolder(h) && G.Within(h.Home, p, SlotHeldCm))) return true;
            }
            foreach (var c in rangeCivs) if (!c.Gun.Down && (G.Within(c.Pos(Tick), p, SlotHeldCm) || G.Within(c.Home, p, SlotHeldCm))) return true; // a bystander comes back to its slot
            return false;
        }

        /// <summary>The spawn order (brief 029): `count` hostiles of a side onto the first free slots of a placement, into the fight AI now.</summary>
        string Spawn(Order o)
        {
            string side = o.Target, at = o.To; int n = o.Count == 0 ? 1 : o.Count;
            if (side == "car") return SpawnCar(o); // GameRangeKinds.cs
            if (o.With.Length > 0) return No("target-unknown", "spawn: with is for a car");
            if (side == "bystanders") return SpawnBystanders(o);
            if (Array.IndexOf(RangeSides, side) < 0) return No("target-unknown", $"spawn: '{side}' is not what the range spawns ({string.Join(", ", RangeKinds)})");
            if (o.Point.HasValue || Array.IndexOf(RangePlacements, at) < 0) return No("no-place", $"spawn: '{at}' is not a placement of the range ({string.Join(", ", RangePlacements)})");
            if (n < 1 || n > RangeSlots) return No("no-place", $"spawn: count {n} is outside 1..{RangeSlots}");
            var slots = new List<int>();
            for (int k = 1; k <= RangeSlots && slots.Count < n; k++) if (!SlotHeld(RangeSlot(at, k))) slots.Add(k);
            if (slots.Count < n) return No("no-place", $"spawn: {slots.Count} of the {RangeSlots} {at} slots are free, {n} asked");
            rangeWaves++; rangeClear = false;
            string prefix = side == "rivals" ? "rival-" : side == "police" ? "officer-" : "response-";
            int num = 0; foreach (var h in Hostiles) if (h.Id.StartsWith(prefix, StringComparison.Ordinal)) num++;
            var ids = new List<string>();
            foreach (var k in slots)
            {
                var slot = RangeSlot(at, k); bool rival = side == "rivals";
                var gun = NewGun(prefix + (++num).ToString(System.Globalization.CultureInfo.InvariantCulture), PistolW, !rival, rival ? R.HealthRival : R.HealthOfficer);
                gun.Engaged = true; // the range is a fight: no first-sight react (Pulse §3.2, as the summoned officers)
                var h = new Hostile { Id = gun.Id, At = slot, Home = slot, Speed = rival ? rivSpeed : secSpeed, Present = true, Gun = gun, Wave = rangeWaves };
                Hostiles.Add(h); ids.Add(gun.Id);
                if (at == "cover") { var c = S.Cover.Find(x => x.At == slot); if (c != null && !coverTaken.ContainsKey(c.Id)) TakeCover(gun, c); }
                switch (side)
                {
                    case "police": gun.St = "hold"; break; // a holder at its stand: it engages a foe inside its effective ring, and holds
                    case "rivals": gun.St = "alerted"; gun.Until = gun.NextShot = Tick + AimNewT; break; // as the ambush springs: the wait is the aim
                    default: gun.St = "advance"; h.At = S.Stand("range:entry"); MoveTo(h, slot); break; // walks in from the far end to its stand
                }
            }
            Note("range.spawn", $"{n} {side} {(side == "response" ? "walk in to" : "at")} {at}: {string.Join(", ", ids)}.");
            return "";
        }

        // ---------------------------------------------------------------- reads for the host (read-only)

        /// <summary>Every hostile the range has spawned, up or not, in spawn order: what the host draws (a downed one lies; a gone one is not drawn).</summary>
        public List<RangeHostile> RangeHostiles()
        {
            var l = new List<RangeHostile>(); var mark = RangeMark;
            foreach (var h in Hostiles)
            {
                var g = h.Gun; var pos = h.Pos(Tick); bool moving = h.Moving(Tick);
                Pt face = new Pt(0, 0);
                if (g.Target.Length > 0 && PosOf(g.Target) is Pt tp && tp != pos) face = tp - pos;
                else if (moving && h.Run != null) face = h.Run.Heading((long)Math.Max(0, Tick - h.RunStart) * h.Speed);
                if (face == new Pt(0, 0)) face = mark - pos == new Pt(0, 0) ? new Pt(-100, 0) : mark - pos;
                l.Add(new RangeHostile { Id = h.Id, Side = SideOf(h.Id), Pos = pos, Facing = face, Health = g.Health, Max = g.Max, Wave = h.Wave, State = g.Down ? "down" : g.St,
                    Target = g.Target, Cover = g.Cover, Moving = moving && !g.Down, Down = g.Down, Gone = g.St == "gone" });
            }
            return l;
        }

        /// <summary>The cover a target standing at `at` has against a shooter at `from` (ai.cover: a point within cover.snap that shields it,
        /// past point blank): full (q ≥ qFull), partial (≥ qPartial) or none, and the point; the best, then the lower id.</summary>
        (string state, string id) CoverAgainst(Pt at, Pt from)
        {
            int best = 0; string bid = ""; long snap2 = (long)R.CoverSnapCm * R.CoverSnapCm; int sec = Tick / Hz;
            foreach (var c in LiveCover) // the cover standing (a car slot's only while a car stands there)
            {
                if (G.D2(at, c.At) > snap2 || !CoverRules.Protects(c, from, sec, R.PointBlankCm)) continue;
                int cls = c.Q >= R.CoverQFull ? 2 : c.Q >= R.CoverQPartial ? 1 : 0; if (cls == 0) continue;
                if (cls > best || (cls == best && string.CompareOrdinal(c.Id, bid) < 0)) { best = cls; bid = c.Id; }
            }
            return (best == 2 ? "full" : best == 1 ? "partial" : "none", bid);
        }

        FireLine LineOf(string by, Gun g, Pt from, bool moving, bool agent, string t, Pt at)
        {
            var w = g.W!; long d = Dist(from, at); bool car = IsCarId(t);
            var f = new FireLine { Shooter = by, Target = t, From = from, To = at, DistanceCm = (int)Math.Min(int.MaxValue, d), Moving = moving, Untrained = !g.Trained, Aimed = g.Target == t, Damage = w.DamageAt(d),
                Bystander = RangeCivOf(t) != null, Car = car };
            f.Band = d <= R.PointBlankCm ? "point" : d <= w.EffCm ? "eff" : d <= w.MaxCm ? "long" : "out";
            bool sees = Sees(from, at);
            f.Line = d <= w.MaxCm && sees;
            if (agent) f.InReach = f.Line;
            else if (IsFightHostile(by)) { foreach (var (id, _) in FoesInReach(by, g, from, p => Sees(from, p), true)) if (id == t) f.InReach = true; f.Answers = IsOpId(t) && PoliceBody(by) && ShotByOpRecently(g, t); }
            var (cs, cid) = car ? ("none", "") : CoverAgainst(at, from); f.CoverId = cid;
            bool tMoving = Moving(t); // a moving target is not beside cover (CoverMod)
            f.Cover = tMoving ? "none" : cs; f.CoverLost = tMoving && cs != "none";
            var gait = GaitOf(by, at); f.Gait = gait == Gait.Walk ? "walk" : gait == Gait.Run ? "run" : gait == Gait.RunAway ? "run-away" : "stand";
            f.TargetRunning = TargetRunning(t);
            if (f.Line) { f.Acc = ShotAcc(g, t, from, at, d, gait, false, out int pips); f.Pips = pips; }
            f.Count = f.Aimed ? g.Acc : AccFor(g, t);
            f.AimKept = !f.Aimed && g.Mem != null && g.Mem.ContainsKey(t);
            f.ShotsToHit = f.Acc > 0 ? Math.Max(1, (1000 - f.Count + f.Acc - 1) / f.Acc) : 0;
            if (agent)
            {
                var op = OpOf(Sim.Op.Parse(by));
                f.ReadyIn = !g.Drawn || g.HolsterAt >= 0 ? -1 : Math.Max(0, Math.Max(g.NextShot, g.DrawReady) - Tick);
                f.Steadying = op.SettleUntil > Tick;
                var fs = FireStateOf(op.Id); if (fs.Target == t) { f.Mode = fs.Mode; f.Lead = fs.Lead; }
            }
            else f.ReadyIn = Math.Max(0, g.NextShot - Tick);
            f.Text = Say(f, agent);
            return f;
        }

        /// <summary>A line of fire in plain words (no design terms): the chance, the distance, then each rule in play on it.</summary>
        string Say(FireLine f, bool agent)
        {
            var p = new List<string>();
            string Name(string id) => IsOpId(id) ? "the " + char.ToUpperInvariant(id[0]) + id.Substring(1) : id;
            if (!f.Line) p.Add(f.Band == "out" ? "out of range" : "no clear shot");
            else p.Add(Lab && LabPipsOn ? new string('●', Math.Min(4, f.Pips)) + new string('○', 4 - Math.Min(4, f.Pips)) : $"{f.Acc / 10}%");
            p.Add($"{f.DistanceCm / 100} m" + (f.Band == "point" ? " point blank" : f.Band == "long" ? " long shot" : ""));
            if (f.Car) { var c = CarOf(f.Target); p.Add(c == null ? "car" : c.BurningAt >= 0 ? "car burning" : c.DisabledAt >= 0 ? "car disabled" : "car"); }
            if (f.Bystander) p.Add("bystander"); // brief 041 (Sky): the hold fires at anyone, no Ctrl; the host says how
            if (f.Cover != "none") p.Add(f.Cover == "full" ? "full cover" : "half cover");
            if (f.CoverLost) p.Add("moving: cover doesn't count");
            if (f.Line && f.Acc > 0) p.Add(f.ShotsToHit == 1 ? "hits next shot" : $"hits in {f.ShotsToHit} shots");
            if (f.AimKept) p.Add("aim kept from before");
            else if (!f.Aimed && f.Line && f.ReadyIn >= 0) p.Add("new target: aims first");
            if (f.Steadying) p.Add("steadying after the run");
            if (f.Gait == "walk") p.Add("walking");
            else if (f.Gait == "run") p.Add("running");
            else if (f.Gait == "run-away") p.Add("running away");
            if (f.TargetRunning) p.Add("target running");
            switch (f.Mode)
            {
                case "trigger": p.Add("trigger held"); break;
                case "trigger-held": p.Add("trigger up, aim kept"); break;
                case "lock": p.Add(f.Lead.Length > 0 ? $"locked on by {Name(f.Lead)}" : "locked on"); break;
                case "follow": p.Add($"following {Name(f.Lead)}"); break;
                case "follow-held": p.Add($"following {Name(f.Lead)}, waiting"); break;
                case "return": p.Add("firing back"); break;
                case "free": p.Add("firing at will"); break;
            }
            if (agent && f.ReadyIn < 0) p.Add("holstered");
            if (f.Answers) p.Add("answering your fire");
            if (f.Untrained) p.Add("untrained");
            return string.Join(" · ", p);
        }

        /// <summary>Brief 029 "show the numbers": every line of fire now, each agent (up, on the floor, armed) to each hostile (up and present),
        /// bystander (on the floor) and car (standing, not burnt out), then each hostile to each agent, in that order. Read-only: nothing
        /// changes, nothing is hashed. Empty outside the range.</summary>
        public List<FireLine> RangeLines()
        {
            var l = new List<FireLine>(); if (!Range) return l;
            var hs = new List<Hostile>(); foreach (var h in Hostiles) if (!h.Down && h.Present && h.Gun.St != "gone") hs.Add(h);
            var others = new List<(string id, Pt at)>();
            foreach (var c in rangeCivs) if (PosOf(c.Id) is Pt cp) others.Add((c.Id, cp));
            foreach (var c in rangeCars) if (!c.Replaced && PosOf(c.Id) is Pt kp) others.Add((c.Id, kp));
            foreach (var op in Ops)
            {
                if (op.Down || op.Inside || op.Gun.W == null) continue;
                string name = Sim.Op.Name(op.Id); bool moving = op.Walk != null || op.DriveMoved;
                foreach (var h in hs) l.Add(LineOf(name, op.Gun, op.Pos, moving, true, h.Id, h.Pos(Tick)));
                foreach (var (id, at) in others) l.Add(LineOf(name, op.Gun, op.Pos, moving, true, id, at));
            }
            foreach (var h in hs)
            {
                if (h.Gun.W == null) continue;
                foreach (var op in Ops) { if (op.Down || op.Inside) continue; l.Add(LineOf(h.Id, h.Gun, h.Pos(Tick), h.Moving(Tick), false, Sim.Op.Name(op.Id), op.Pos)); }
            }
            return l;
        }

        /// <summary>The range's own state for the checkpoint hash: only a range run adds these bytes.</summary>
        internal void HashRange(Hash.H h)
        {
            h.S("range").I(rangeWaves).Z(rangeClear);
            foreach (var x in Hostiles) h.I(x.Wave);
            HashRangeKinds(h); // cars and bystanders: no bytes until one is spawned
        }
    }
}
