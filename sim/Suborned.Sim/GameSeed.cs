// Brief 080 (engine 0.37.0; Sky 2026-10-30: "let the game play organically"; Motive's design, briefs/080/design.html, Sky: "1. ok 2. ok 3. ok").
// Each new game has a seed (Start.Seed), saved with the run: seed + orders replay exactly. It does two things and nothing else:
//  · the start (rulebook seed): the crowd's place, phase and pace, where the patrol begins its sweep, which rival takes which post, the camera desks'
//    scan phases and the service round, the rivals' arrival. All drawn once, when the game starts; from then on routines and people play out with no
//    dice, so whatever a Watch shows holds for the whole game. The start stays fair: the patrol never begins with the cell's stands in its cone.
//  · the shots (rulebook weapons.sheet): each shot draws once to hit. Nothing else rolls: no Turn, takedown, witness, route or arrival; damage is fixed.
// Every draw is a pure function of (seed, character id, stage, index): no shared list, so one more shot by one shooter never reshuffles anyone else's
// luck, a host read draws nothing, and a later cast of 1000 fits without a rewrite. Each character has a fuzziness (seed.fuzz, per mille of every
// band; 0 = always the same: Vale, Inez) and an aim offset (weapons.sheet.offset). Seed 0, and a script with no seed, is the neutral start: every start
// draw at the middle of its band, which is the timetable and the id-only phases the scenarios read; its shots still roll, on seed 0's streams.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Suborned.Sim
{
    /// <summary>The sum behind a shot's chance (the fire chip's Alt): aim + range + cover + movement, then kept to the sheet's limits.</summary>
    public struct RollSum { public int Aim, Range, Cover, Move, Pct; }

    /// <summary>weapons.sheet: the stat sheet (brief 080). Every number is the rulebook's.</summary>
    public sealed class SheetRules
    {
        readonly System.Collections.Generic.Dictionary<string, int> aim = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal), offset = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
        public readonly int RangePoint, RangeEff, RangeLong, CoverFull, CoverPartial, TargetRunning, Walk, Run, RunAway, Pinned, Min, Max, StreakHits, StreakMisses, StreakFloor, IntakeTicks, WarnHealth, GuardSpeed;
        public static readonly string[] Classes = { "civilian", "robot", "guard", "tech", "fixer", "officer", "scout", "rival", "enforcer" };
        public SheetRules(JNode n, Func<JNode, int> ri)
        {
            n.Only("aim", "offset", "range", "cover", "move", "limit", "streak", "intake", "warnHealth", "guardSpeed", "rule");
            n["aim"].Only(Classes); foreach (var c in Classes) aim[c] = ri(n["aim"][c]);
            foreach (var kv in n["offset"].Entries) offset[kv.Key] = ri(kv.Value);
            var r = n["range"]; r.Only("point", "eff", "long"); RangePoint = ri(r["point"]); RangeEff = ri(r["eff"]); RangeLong = ri(r["long"]);
            var c2 = n["cover"]; c2.Only("full", "partial", "targetRunning"); CoverFull = ri(c2["full"]); CoverPartial = ri(c2["partial"]); TargetRunning = ri(c2["targetRunning"]);
            var m = n["move"]; m.Only("walk", "run", "runAway", "pinned"); Walk = ri(m["walk"]); Run = ri(m["run"]); RunAway = ri(m["runAway"]); Pinned = ri(m["pinned"]);
            var l = n["limit"]; l.Only("min", "max"); Min = ri(l["min"]); Max = ri(l["max"]);
            if (Min < 1 || Max > 99 || Min >= Max) throw new FormatException("weapons.sheet.limit: no shot is ever certain: 1 <= min < max <= 99");
            var s = n["streak"]; s.Only("hits", "misses", "floor"); StreakHits = ri(s["hits"]); StreakMisses = ri(s["misses"]); StreakFloor = ri(s["floor"]);
            if (StreakHits < 1 || StreakMisses < 1) throw new FormatException("weapons.sheet.streak: a cap of at least 1");
            IntakeTicks = ri(n["intake"]); if (IntakeTicks < 1) throw new FormatException("weapons.sheet.intake is at least 1 tick");
            WarnHealth = ri(n["warnHealth"]); GuardSpeed = ri(n["guardSpeed"]); if (GuardSpeed < 1) throw new FormatException("weapons.sheet.guardSpeed is at least 1 cm/s");
            if (n["rule"].Str().Length == 0) throw new FormatException("weapons.sheet.rule is empty");
        }
        /// <summary>A shooter's class from its id, as the guns are named: the cell by name, the guard, the patrol and robots, officers, rivals; anyone else is a civilian.</summary>
        public static string ClassOf(string id)
        {
            switch (id) { case "scout": case "fixer": case "tech": case "enforcer": case "guard": return id; case "patrol": return "robot"; }
            if (id.StartsWith("robot-", StringComparison.Ordinal)) return "robot";
            if (id.StartsWith("rival", StringComparison.Ordinal)) return "rival";
            if (id.StartsWith("officer-", StringComparison.Ordinal) || id.StartsWith("security-", StringComparison.Ordinal) || id.StartsWith("police-", StringComparison.Ordinal) || id.StartsWith("unit-", StringComparison.Ordinal)) return "officer";
            return "civilian";
        }
        /// <summary>The class's aim plus the character's offset (0 unless the sheet names them).</summary>
        public int AimOf(string id) => aim[ClassOf(id)] + (offset.TryGetValue(id, out int o) ? o : 0);
        /// <summary>The chance of a shot: aim + range + cover + movement, kept between the limits, so nobody ever hits for certain or never can. cov is the
        /// cover multiplier the street gives (Game.CoverMod): 1000 none. A running target past point blank takes its step in cover's place, as it always has.</summary>
        public RollSum Sum(int aimPct, Rules r, Weapon w, long d, Gait gait, int cov, bool targetRunning, bool pinned)
        {
            var s = new RollSum { Aim = aimPct };
            if (d > w.MaxCm) return s; // no shot at all: nobody fires past the max ring
            bool close = d <= r.PointBlankCm;
            s.Range = close ? RangePoint : d <= w.EffCm ? RangeEff : RangeLong;
            s.Cover = targetRunning && !close ? TargetRunning : cov >= 1000 ? 0 : cov == r.ModCover ? CoverFull : CoverPartial;
            s.Move = (gait == Gait.Walk ? Walk : gait == Gait.Run ? Run : gait == Gait.RunAway ? RunAway : 0) + (pinned ? Pinned : 0);
            s.Pct = Math.Max(Min, Math.Min(Max, s.Aim + s.Range + s.Cover + s.Move));
            return s;
        }
    }

    /// <summary>The rulebook's seed: the bands of the start, and who is always the same.</summary>
    public sealed class SeedRules
    {
        public readonly int CrowdPhaseS, RivalsArriveS, FairFirstS, FairTries, FuzzDefault; public readonly string RivalsId, PatrolId; public readonly bool StaffFixed;
        readonly System.Collections.Generic.Dictionary<string, int> fuzz = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
        public SeedRules(JNode n, Func<JNode, int> ri)
        {
            n.Only("crowd", "rivals", "patrol", "fair", "fuzz", "rule", "tag", "src", "proposed", "reason");
            n["crowd"].Only("phase", "staffFixed"); CrowdPhaseS = ri(n["crowd"]["phase"]); StaffFixed = n["crowd"]["staffFixed"].Bool();
            n["rivals"].Only("id", "arrive"); RivalsId = n["rivals"]["id"].Str(); RivalsArriveS = ri(n["rivals"]["arrive"]);
            n["patrol"].Only("id"); PatrolId = n["patrol"]["id"].Str();
            n["fair"].Only("firstS", "tries"); FairFirstS = ri(n["fair"]["firstS"]); FairTries = ri(n["fair"]["tries"]);
            n["fuzz"].Only("default", "people"); FuzzDefault = ri(n["fuzz"]["default"]);
            foreach (var kv in n["fuzz"]["people"].Entries) { int v = ri(kv.Value); if (v < 0 || v > 1000) throw new FormatException($"{kv.Value.Path}: fuzziness 0..1000 per mille"); fuzz[kv.Key] = v; }
            if (CrowdPhaseS < 0 || RivalsArriveS < 0 || FairFirstS < 0 || FairTries < 1 || FuzzDefault < 0 || FuzzDefault > 1000) throw new FormatException("seed: bands 0 or more, tries at least 1, fuzziness 0..1000 per mille");
        }
        /// <summary>A character's fuzziness, per mille of every band: 0 = always the same.</summary>
        public int FuzzOf(string id) => fuzz.TryGetValue(id, out int v) ? v : FuzzDefault;
    }

    public sealed partial class Game
    {
        const int OldClockS = 20; // the pre-080 clock (Vale out 5:50) on the site's: 60 s on 0.37.0's 4:50, 20 s on brief 096's 5:30 (Sky 2026-11-04); the people file moves with clock.startsAt, so the old routines come back to the second too
        /// <summary>The tick the close's warning went out (rulebook fight.close), -1 not yet: the host's clock turns; the log says close.warning.</summary>
        public int CloseWarnedAt = -1;
        /// <summary>Rulebook fight.close: with Vale in the cell's hands (on foot or in the car), `warn` before the close his absence is noticed, once, and the HUD says so; at the close, a mission
        /// still here is closed in, and the fail names why: no-driver (nobody aboard can drive), car-held (who holds it), on-foot (Vale still walking), no-go (the car was free and nobody said go).</summary>
        void CloseStep()
        {
            bool held = Vale == ValeAt.WithEnforcer || Vale == ValeAt.Car; if (!held) return;
            if (CloseWarnedAt < 0 && Tick >= T(overS) - T(R.CloseWarnS))
            {
                CloseWarnedAt = Tick; int left = Math.Max(0, T(overS) - Tick) / Hz;
                Note("close.warning", $"{Cap(ValeName)}'s absence is noticed. The district closes in {left} s.");
            }
            if (Tick < T(overS)) return;
            string why; Blamed? who = null;
            if (Vale == ValeAt.WithEnforcer) { why = "on-foot"; who = Locate(NearestHostileToVale(), "closed-in"); }
            else if (!CarHasDriver()) { why = "no-driver"; string by = ""; int last = -1; foreach (var o in Ops) if (o.Gun.Down && o.Gun.ShotByTick > last) { last = o.Gun.ShotByTick; by = o.Gun.ShotBy; } who = Locate(by.Length > 0 ? by : "getaway-car", "closed-in"); }
            else { var hs = CarHolders(); var bx = GetawayBoxers(); why = bx.Count > 0 ? "boxed" : hs.Count > 0 ? "car-held" : "no-go"; who = Locate(bx.Count > 0 ? LocateCar(bx[0]) : hs.Count > 0 ? hs[0] : "getaway-car", "closed-in"); } // brief 096 (car.box): boxed in first
            string words = why == "no-driver" ? "nobody left who can drive" : why == "boxed" ? $"the car boxed in by {string.Join(", ", GetawayBoxers())}" : why == "car-held" ? "the car held" : why == "on-foot" ? $"{ValeName} still on foot" : "the car never told to go";
            Fail("closed-in", $"The district closes on the cell: {words}.", () => Because(why, true, who));
        }
        string NearestHostileToVale()
        {
            var at = ValePos() ?? Enforcer.Pos; string best = ""; long bd = long.MaxValue;
            foreach (var id in CombatantIds()) { if (!Combatant(id)) continue; var p = PosOf(id); if (!p.HasValue) continue; long d = G.D2(p.Value, at); if (d < bd) { bd = d; best = id; } }
            return best.Length > 0 ? best : "enforcer";
        }
        /// <summary>The mission's clock this game plays (seconds): the site's schedule, OldClockS later on a test's Start.OldClock (the pre-080 clock).</summary>
        public int ClockOver => overS; public int ClockValeExits => valeExitsS; public int ClockValeAtCar => valeAtCarS; public int ClockDriverLast => driverLastS;
        internal bool NowStaff(Walker w) => IsStaff(w.P);
        /// <summary>The run's seed: the host shows it with a saved run. A read.</summary>
        public int Seed => Start.Seed;
        /// <summary>Shots roll, on the stat sheet: every run but the fight lab's (its own pips and counted chance) and a Pre080 test.</summary>
        public bool Rolled => !Lab && !Start.Pre080;
        /// <summary>The tick the Enforcer's warning went out (rulebook weapons.sheet.warnHealth), -1 not yet: the host's roster flash; the log says enforcer.warning.</summary>
        public int EnforcerWarnedAt = -1;
        /// <summary>Where on its sweep the patrol began (ticks into the period); 0 at seed 0. Fixed for the run: the sweep's schedule reads it.</summary>
        public int PatrolPhase;
        int[]? rivalPosts; // which post each posted rival takes (index: member N - 1); null at seed 0 (nearest free, as always)

        /// <summary>Brief 081 (host read): the official driver is at the wheel: he is in the car, not on the street (DriverPos reads the car's centre while he is).</summary>
        public bool DriverAboard => !Driver.Down && valeDriveStart >= 0 && !OfficialOut;
        int cellFirstShot = -1, cellLastShot = -1;
        /// <summary>What a band reads (output only, never hashed): shots by or at the cell, how many hit, the seconds from the first to the last, HP the cell lost, agents down.</summary>
        public int CellShots, CellHits;
        public int CellFightS => cellFirstShot < 0 ? 0 : (cellLastShot - cellFirstShot) / Hz;
        public int CellHpLost { get { int n = 0; foreach (var o in Ops) n += o.Gun.Max - o.Gun.Health; return n; } }
        public int CellDownCount { get { int n = 0; foreach (var o in Ops) if (o.Gun.Down) n++; return n; } }

        /// <summary>The key of a character's stream for one stage: game seed + id + stage (rulebook streams: every stage takes its own key).</summary>
        uint StreamKey(string id, string stage) => Streams.Fnv1a32(Seed.ToString(CultureInfo.InvariantCulture) + "/" + id + "/" + stage);
        /// <summary>A start draw in -band..+band, scaled by the character's fuzziness; 0 at seed 0 or for someone always the same.</summary>
        int DrawBand(string id, string stage, int band, int index = 1)
        {
            if (Seed == 0) return 0;
            int b = band * R.Seeds.FuzzOf(id) / 1000; if (b <= 0) return 0;
            return (int)(Streams.Nth(StreamKey(id, stage), index) % (uint)(2 * b + 1)) - b;
        }
        /// <summary>A start draw in 0..n-1; 0 at seed 0 or for someone always the same.</summary>
        int DrawBelow(string id, string stage, int n, int index = 1) => Seed == 0 || n <= 1 || R.Seeds.FuzzOf(id) == 0 ? 0 : (int)(Streams.Nth(StreamKey(id, stage), index) % (uint)n);

        /// <summary>A watcher's scan phase, or a service round's: from the id alone at seed 0 (as before 0.37.0), else its own draw. Fixed for the run.</summary>
        int ScanPhase(string id, int period) => Seed == 0 || R.Seeds.FuzzOf(id) == 0 ? (int)(Streams.Fnv1a32(id) % (uint)period) : (int)(Streams.Nth(StreamKey(id, "scan"), 1) % (uint)period);
        /// <summary>The extra per cent a person walks a routine leg at (crowd.pace): from the id alone at seed 0, else their own draw.</summary>
        int PaceOf(Walker w) => Seed == 0 || R.Seeds.FuzzOf(w.P.Id) == 0 ? (int)(Streams.Fnv1a32(w.P.Id + "/pace") % (uint)(R.CrowdPaceSpreadPct + 1)) : (int)(Streams.Nth(StreamKey(w.P.Id, "pace"), 1) % (uint)(R.CrowdPaceSpreadPct + 1));

        /// <summary>The start's draws that need the whole cast standing (end of the constructor). Nothing at seed 0.</summary>
        void SeedStart()
        {
            if (Start.OldClock) foreach (var w in Walkers) // test only: the routines OldClockS later, as before the shift came forward (brief 080)
            {
                var f = (int[])w.From.Clone(); var to = (int[])w.To.Clone();
                for (int i = 0; i < f.Length; i++) f[i] += T(OldClockS);
                for (int i = 0; i < to.Length; i++) to[i] += T(OldClockS);
                w.From = f; w.To = to;
            }
            if (Seed == 0) return;
            string pid = R.Seeds.PatrolId;
            if (R.Seeds.FuzzOf(pid) > 0)
            {
                int period = T(R.PatrolPeriodS);
                for (int k = 1; k <= R.Seeds.FairTries; k++)
                {
                    int ph = DrawBelow(pid, "phase", period, k);
                    if (SweepFair(ph)) { PatrolPhase = ph; break; } // no fair draw in `tries`: it begins at the sweep's end, as the neutral start does
                }
            }
            foreach (var w in Walkers)
            {
                if (R.Seeds.StaffFixed && (IsStaff(w.P) || ServerOf(w.P) != null)) continue; // who keeps a post or a round keeps its clock: check-in marks and the round read it
                int k = T(DrawBand(w.P.Id, "phase", R.Seeds.CrowdPhaseS)); if (k == 0) continue;
                var f = (int[])w.From.Clone(); var to = (int[])w.To.Clone(); // the routine's arrays are shared by every game of these inputs
                for (int i = 1; i < f.Length; i++) f[i] = Math.Max(0, f[i] + k);
                for (int i = 0; i + 1 < to.Length; i++) to[i] = Math.Max(1, to[i] + k);
                w.From = f; w.To = to;
            }
            if (R.Seeds.FuzzOf(R.Seeds.RivalsId) > 0) // Fisher-Yates over the posts, one draw a step
            {
                var p = new int[Cr.Posted]; for (int i = 0; i < p.Length; i++) p[i] = i;
                for (int i = p.Length - 1; i > 0; i--) { int j = DrawBelow(R.Seeds.RivalsId, "posts", i + 1, p.Length - i); int x = p[i]; p[i] = p[j]; p[j] = x; }
                rivalPosts = p;
            }
        }

        /// <summary>The fairness limit (seed.fair): begun at this phase, the sweep has none of the cell's stands in its cone for its first seconds, so no
        /// start is noticed before the player has moved.</summary>
        bool SweepFair(int phase)
        {
            int keep = PatrolPhase; PatrolPhase = phase;
            try
            {
                for (int s = 0; s <= R.Seeds.FairFirstS; s++)
                {
                    var pos = SweepPos(T(s));
                    foreach (var op in Ops) if (InSweepCone(pos, S.PatrolFacing, op.Pos) && EyeSees(pos, R.MountPatrolCm, op.Pos)) return false;
                }
                return true;
            }
            finally { PatrolPhase = keep; }
        }

        /// <summary>Brief 080 (host read; draws nothing): who of the crowd, on their routine, would notice `act` at `at` within the next `seconds` and doesn't now: a card's "about to arrive". A person
        /// scattered off their routine isn't foretold. Sampled every half second, as they walk.</summary>
        public List<(Person p, int inS)> ComingToSee(Pt at, int seconds, string act, Person? except = null)
        {
            var l = new List<(Person, int)>(); if (People2 == null || !Start.Crowd) return l;
            int end = T(seconds), reach = witnessRange + T(seconds) * R.CrowdRunSpeed / 10; // cm: nobody further can come
            foreach (var w in Walkers)
            {
                if (w.P == except || w.P.Down || w.St.Length > 0) continue;
                var now = CrowdPos(w, Tick);
                if (now.HasValue && !G.Within(now.Value, at, reach)) continue;
                if (now.HasValue && Notices(w.P, now.Value, at, act)) continue; // on the card already
                for (int k = 5; k <= end; k += 5)
                {
                    var q = RoutinePos(w, Tick + k); if (!q.HasValue || !G.Within(q.Value, at, witnessRange)) continue;
                    if (NoticesAt(w.P, q.Value, at, act, Alert, RoutineFacing(w, Tick + k) ?? new Pt(0, 0))) { l.Add((w.P, k / Hz)); break; }
                }
            }
            return l;
        }

        /// <summary>The sum behind a rolled shot's chance, for the shot and for the chip (a read: nothing moves, nothing draws).</summary>
        RollSum SumOf(Gun g, string t, Pt from, Pt at, long d, Gait gait)
        {
            int cov = MarkedFor(g, t) ? 1000 : CoverMod(t, at, from); // Mark: his cover doesn't count against the cell
            if (!R.PointBlankCover && d <= R.PointBlankCm) cov = 1000; // no cover at arm's length
            return R.Sheet.Sum(g.Aim, R, g.W!, d, gait, cov, TargetRunning(t), PinOn && Pinned(g));
        }

        /// <summary>A shot rolls: one draw from the shooter's own stream, always, whatever the streak does, so a capped shot moves nobody's later luck. Then the
        /// caps, leaning the player's way on purpose (Sky 2026-10-30): a hostile's hit on an agent after `hits` in a row misses; an agent's miss after `misses` in a
        /// row hits, when the chance is at least `floor`. The pair's streak sits in Acc and Mem, as the count before it did.</summary>
        bool RollShot(Gun g, string t, int acc)
        {
            var k = R.Sheet; g.ShotN++;
            bool hit = Streams.Nth(StreamKey(g.Id, "shot"), g.ShotN) % 1000u < (uint)Math.Max(0, acc);
            bool cell = IsOpId(g.Id), onCell = !cell && IsOpId(t);
            if (onCell && hit && Tick < OpOf(Sim.Op.Parse(t)).Gun.IntakeUntil) { hit = false; g.Acc = 0; } // weapons.sheet.intake: one hit on one agent per gap, whoever fires: the shot still drew
            else if (onCell && hit && g.Acc >= k.StreakHits) { hit = false; g.Acc = 0; }
            else if (cell && !hit && g.Acc >= k.StreakMisses && acc >= k.StreakFloor * 10) { hit = true; g.Acc = 0; }
            else if (onCell) g.Acc = hit ? g.Acc + 1 : 0;
            else if (cell) g.Acc = hit ? 0 : g.Acc + 1;
            else g.Acc = 0;
            if (onCell && hit) OpOf(Sim.Op.Parse(t)).Gun.IntakeUntil = Tick + k.IntakeTicks;
            return hit;
        }
    }
}
