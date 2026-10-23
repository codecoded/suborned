// Brief 027, the fight playground (EXPERIMENTAL; rulebook fight.lab; Motive, briefs/027/playground.md §2, §3). Read only by a run whose start
// turns it on (Start.FightLabOn); off, every rule is today's v0.24 and nothing here changes a hash. The rules:
//  · health ×healthScale for every gun; the cell's loadout (each weapon's arms within Arms; the carbine is the cell's), every other gun the lab
//    sidearm; damage by weapon (the riot gun's near band); draw and aimNew from the lab.
//  · pips (on): a shot's chance is one of four steps from range and edges (Rules.LabPips), counted by the accumulator as always.
//  · stances two (free, hold); then free (a fire order whose target is down returns the agent to at will); close (a fire order out of range
//    or with no line walks the grid into the effective ring, then fires).
//  · charge: one bar per agent; charged weapons pay a shot and wait short of it; it comes back while the charged weapon rests. Volleys (target
//    mode) pay the weapon's target charge a shot when given; the field absorbs damage and stops fire.
//  · the police warning: security that sees a drawn weapon before any shot gives policeWarning.ticks to holster.
//  · hostiles fire as they walk.
//  · coverSnap (B5): a move that ends within coverSnap.snap of a free cover point of the site ends on it (a straight walkable line to it, the one
//    that shields against the nearest combatant first); the agent holds the point while it walks there or stands on it.
// A freeze is host time, never sim time: the engine has no frozen state (playground.md §0). The previews here are read-only.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>What the fire chip shows for one agent on one target now (brief 027 P1: pips · damage · hits to down, no seconds). Read-only.</summary>
    public sealed class FirePreview
    {
        /// <summary>Why the agent can't fire on it now (a refusal code: no-weapon, fire-running, fire-carrying, fire-carrying-vale, no-target,
        /// fire-on-cell, target-unknown, civilian-target, out-of-range, no-clear-line), "" when it can (or will, walking into range). Engine 0.15.0:
        /// fire-running and fight-over are no longer given (tuning.md §8, §9).</summary>
        public string Refused = "";
        public int Pips;                  // 1–4 now with pips on; 0 out of range, no line, or with pips off
        public int Acc;                   // the chance now, per mille (the step for the pips; today's multipliers with pips off)
        public int Damage;                // a hit's damage at this distance
        public int Health, Max;           // the target's health now and its full bar
        public int HitsToDown;            // hits to take the bar to 0 (ceil(health / damage))
        public int ShotsToDown;           // shots to those hits by the accumulator, from this agent's aim on it now (-1 when it can't hit)
        public int ChargePerShot;         // the weapon's own charge a shot (0: none)
        public int TargetChargePerShot;   // a targeted shot's charge (target mode), on top of ChargePerShot
        public int Charge, ChargeMax;     // the agent's bar
        public int DistanceCm; public string Band = ""; // point, eff, long, out
        public bool Line, Moving, Cover, Untrained, High; // a clear line, and the edges that apply now
        public bool Walk; public Pt WalkTo; public int WalkS; // fire.close: out of range or no line, the walk into range (route seconds at a walk)
        /// <summary>fire.close: where the walk stops (the first tick's position inside the effective ring with a clear line, as the engine stops
        /// it), the ticks to get there, and the combatants that could fire on the agent on the way (their effective ring and a line to any
        /// tick's position of the walk, as they stand now), in id order. With Walk set, Pips, Acc, Band, DistanceCm, the edges and ShotsToDown
        /// are read from StopAt, standing (a volley holds its shots until it stands there; at-will and focus fire may shoot on the way).</summary>
        public Pt StopAt; public int WalkTicks; public readonly List<string> WalkExposedTo = new List<string>();
        /// <summary>Tuning.md §8.8 (engine 0.15.0): the gait the chance is read at now (stand, walk, run, run-away; stand at StopAt for a walk-in),
        /// whether the target runs now (×weapons.mod.targetRunning past point blank), and the arrival read: when the agent is moving and its move
        /// ends inside the weapon's max ring with a line, the first standing shot from the move's end at the target where it stands now.</summary>
        public string Gait = "stand"; public bool TargetRunning;
        public bool HasEnd; public Pt EndAt; public int AccEnd, PipsEnd, ShotsToDownEnd = -1;
        public int TrailShooting, TrailHit, TrailDown; public bool Untraced; // the price: +T once per shooting (0 once paid), per person hit (0 once hit), per down; untraced in the final fight
        /// <summary>Preview only (Latch, 0.2.33 shots; engine version unchanged): what "down" means for this target: "down" a person or a camera (health to 0), "disabled"
        /// a car not yet disabled (health to car.target.disabledAt ×healthScale: it stops, its occupants get out), "burns" a disabled car (to 0).
        /// HitsToDown, ShotsToDown and ShotsToDownEnd count to it.</summary>
        public string DownIs = "down";
        /// <summary>Preview only: ticks from now to the shot that takes the target down (DownIs), standing where the agent stands: the first shot
        /// (drawn: its next ready tick, a new target aimed on the next tick and waiting aimNew; holstered: the draw and aimNew), then one each cadence; -1 when it can't
        /// hit, or walks in first. Today's pistol: a shot each pistol.every.</summary>
        public int TicksToDown = -1;
    }

    /// <summary>A volley's forecast "if they stand" (brief 027 §1.3 target mode): shot by shot, from the agent's aim on the target now. Read-only.</summary>
    public sealed class VolleyPreview
    {
        public string Refused = "";       // as FirePreview.Refused, or no-charge (the bar can't pay it; charge off: no volleys left this fight)
        public int Pips, Acc, Damage;
        public int ChargeCost;            // the target charge it pays when given (0 with charge off)
        public int ChargeLeft;            // the bar after paying it
        public int VolleysLeft = -1;      // charge off: volleys this agent still has this fight, else -1
        public readonly List<(bool hit, int damage, int healthAfter)> Shots = new List<(bool, int, int)>();
        public bool Down;                 // the chunk reaches 0
        public int HealthAfter;
        /// <summary>Out of range or with no line (fire.close): the volley walks first and holds its shots until it stands at StopAt (WalkTicks
        /// away); the shots above are fired from there. WalkExposedTo: combatants that could fire on the agent on the way. Empty Walk: no walk.</summary>
        public bool Walk; public Pt StopAt; public int WalkTicks; public readonly List<string> WalkExposedTo = new List<string>();
    }

    /// <summary>A cover point as the host draws it (brief 027 P7, B5: the half-shield marks within coverSnap.marks of a selected drawn agent). Read-only.</summary>
    public sealed class CoverMark
    {
        public string Id = "", Kind = "", Of = "";   // the site's point (contracts/world/civic-block.cover.json): planter-run, wall, car, fountain, cabinet, pier
        public Pt At, Faces; public int Cos, Q;      // where the body kneels; the arc it shields (faces per mille, cos per mille); quality per mille
        public bool Full;                            // full cover (q ≥ ai.cover.qFull: −1 pip, weapons.mod.cover), else partial (ai.cover.modPartial)
        public string HeldBy = "";                   // "" free, else who holds it: an operative's name or a hostile's id (it can't be snapped to)
        public bool Shields;                         // it shields against the agent's nearest combatant now (that combatant inside its arc, beyond point blank)
        public int DistanceCm;                       // from the agent
    }

    /// <summary>A police warning in progress (brief 027 P10): who shouted at whom, and when the window to holster closes.</summary>
    public struct LabWarning { public string Watcher; public OpId Op; public int Until, DrawTick; }

    public sealed partial class Game
    {
        /// <summary>Brief 027: the fight lab is on for this run (Start.FightLabOn and the rulebook has fight.lab).</summary>
        public bool Lab { get; private set; }
        /// <summary>The lab's rules (null with the lab off).</summary>
        public LabRules? Labr { get; private set; }
        /// <summary>Every health is multiplied by this (1 with the lab off).</summary>
        public int HealthScale { get; private set; } = 1;
        /// <summary>The weapon every gun outside the cell carries: weapons.pistol, or the lab's sidearm.</summary>
        public Weapon PistolW { get; private set; } = null!;
        /// <summary>Draw and the first shot on a new target, in ticks: weapons.draw and weapons.aimNew, or the lab's.</summary>
        public int DrawT { get; private set; }
        public int AimNewT { get; private set; }
        bool LabPipsOn, LabTwo, LabThenFree, LabClose, LabCharge, LabField, LabWarn, LabFireMoving, LabSnap;
        readonly List<LabWarning> warnings = new List<LabWarning>();
        readonly SortedDictionary<string, int> warnedBy = new SortedDictionary<string, int>(StringComparer.Ordinal); // tuning.md §6.4: shouts by watcher this mission
        readonly SortedSet<string> warnLogged = new SortedSet<string>(StringComparer.Ordinal); // a shout whose heeding has been logged (+Trail once)
        /// <summary>The police warnings open now (brief 027 P10: the "!" and its ring). Read-only.</summary>
        public IReadOnlyList<LabWarning> Warnings => warnings;
        /// <summary>Police warnings shouted so far this mission, every watcher (tuning.md §6.4: at most policeWarning.perWatcher each). Read-only.</summary>
        public int WarningShouts { get { int n = 0; foreach (var v in warnedBy.Values) n += v; return n; } }
        const int CloseSearchCm = 6000; // fire.close: the walk searched for a point in range (60 m of walk)

        void InitLab()
        {
            PistolW = R.Pistol; DrawT = R.DrawTicks; AimNewT = R.AimNewTicks;
            if (!Start.FightLabOn) return;
            var l = R.Lab ?? throw new FormatException("start fightLab.on: the rulebook has no fight.lab (unknown = reject)");
            Lab = true; Labr = l;
            HealthScale = Start.LabHealthScale ?? l.HealthScale;
            LabPipsOn = Start.LabPips ?? l.Pips; LabTwo = (Start.LabStances ?? l.Stances) == "two"; LabThenFree = (Start.LabThen ?? l.Then) == "free";
            LabClose = Start.LabClose ?? l.Close; LabCharge = Start.LabCharge ?? l.Charge; LabField = Start.LabField ?? l.Field;
            LabWarn = Start.LabWarn ?? l.Warn; LabFireMoving = Start.LabFireMoving ?? l.FireMoving;
            LabSnap = Start.LabCoverSnap ?? l.CoverSnap;
            LabKite = Start.LabKite ?? l.Kite; // brief 027 F2 (Pulse kite-response.md)
            PistolW = l.Weapons["sidearm"]; DrawT = l.DrawTicks; AimNewT = l.AimNewTicks;
            for (int k = 0; k < 4; k++) { var a = Start.LabLoadout[k]; if (a != null && a != "none" && !l.Weapons.ContainsKey(a)) throw new FormatException($"start fightLab.loadout.{Sim.Op.Name((OpId)k)}: weapon '{a}' is unknown (sidearm, carbine, machine, riot, rifle, none)"); }
        }
        string LoadoutId(int k) => Start.LabLoadout[k] ?? Labr!.Loadout[k];
        Weapon? LoadoutWeapon(int k) { var a = LoadoutId(k); return a == "none" ? null : Labr!.Weapons[a]; }
        /// <summary>The loadout within the mission's Arms (the carbine is the cell's: arms 0), at most one carbine (unknown = reject).</summary>
        void CheckLoadout(int arms)
        {
            int sum = 0, carbines = 0;
            for (int k = 0; k < 4; k++) { var w = Ops[k].Gun.W; if (w == null) continue; sum += w.Arms; if (w.Id == "carbine") carbines++; }
            if (sum > arms) throw new FormatException($"start fightLab.loadout takes {sum} Arms; the mission has {arms}");
            if (carbines > 1) throw new FormatException("start fightLab.loadout: the cell has one carbine");
        }

        /// <summary>The level a point stands on (street 0, deck 1, concourse 2). The Handover's site carries no levels yet: everyone is on the
        /// street, so the high edge never applies (Datum and Splice, when the site carries them).</summary>
        static int LevelOf(Pt p) => 0;

        /// <summary>A shot's chance in per mille and its pips (0 with pips off): the lab's pips, else today's multipliers (Rules.Accuracy).
        /// Tuning.md §8.3–8.4 (engine 0.15.0): the shooter's gait, a running target past point blank (×weapons.mod.targetRunning, in cover's
        /// place), and inside point blank no cover unless weapons.pointBlankCover. Pips 0 inside the max ring is the pip0 rung (a runner's).</summary>
        int ShotAcc(Gun g, string t, Pt from, Pt at, long d, Gait gait, bool vale, out int pips)
        {
            int cov = CoverMod(t, at, from);
            if (!R.PointBlankCover && d <= R.PointBlankCm) cov = 1000; // Sky 2026-10-21 (tuning.md §8.4, §9.7): can't miss at arm's length, as the lab
            bool running = TargetRunning(t);
            if (Lab && LabPipsOn)
            {
                pips = R.LabPips(g.W!, d, gait, cov < 1000, !g.Trained, LevelOf(from) > LevelOf(at), Labr!.RunPips);
                if (pips < 0) { pips = 0; return 0; }
                int c = Labr.Chance[pips];
                return running && d > R.PointBlankCm ? c * R.ModTargetRunning / 1000 : c;
            }
            pips = 0; return R.Accuracy(g.W!, d, gait, cov, vale, !g.Trained, running);
        }

        /// <summary>Tuning.md §8.3: an operative's gait this tick against a target at `at`: stand (no step), walk (a walk, a walk-in, a driven
        /// walk), run (a run order or a driven run), run away (a run whose step points within weapons.runAwayCone of straight away from the
        /// target). Before its first step a walk reads its heading.</summary>
        Gait OpGait(Op op, Pt at)
        {
            if (op.Walk == null && !op.DriveMoved) return Gait.Stand;
            if (!op.Running) return Gait.Walk;
            var s = op.Pos - op.StepFrom;
            if (s.X == 0 && s.Y == 0 && op.Walk != null) s = op.Walk.Heading(op.WalkMm);
            return R.RunsAway(s, at - op.Pos) ? Gait.RunAway : Gait.Run;
        }
        /// <summary>Any shooter's gait: an operative's own; every other body has one moving pace, a walk (tuning.md §8.3).</summary>
        Gait GaitOf(string id, Pt at) => IsOpId(id) ? OpGait(OpOf(Sim.Op.Parse(id)), at) : Moving(id) ? Gait.Walk : Gait.Stand;
        /// <summary>Tuning.md §8.3: the body runs this tick (a harder target past point blank): a cell runner (a click or a driven run), a
        /// civilian at the crowd's run (fleeing, or a report or a check at a run), a breaking rival. A walker is not.</summary>
        public bool TargetRunning(string id)
        {
            if (IsOpId(id)) { var o = OpOf(Sim.Op.Parse(id)); return o.Running && (o.Walk != null || o.DriveMoved); }
            var h = HostileOf(id); if (h != null) return h.Gun.St == "break" && h.Present && h.Moving(Tick);
            if (Range && RangeCivOf(id) is RangeCiv rc) return rc.St == "fleeing" && rc.Moving(Tick); // a fleeing bystander runs at the crowd's run
            if (Start.Crowd && CivilianOf(id) is Person p && !p.Down && WalkerOf(p) is Walker w && w.Run != null && Tick >= w.RunStart)
            {
                int pace = w.Pace > 0 ? w.Pace : w.St == "fleeing" ? R.CrowdRunSpeed : 0;
                return pace >= R.CrowdRunSpeed && (long)(Tick - w.RunStart) * pace < w.Run.Length;
            }
            return false;
        }

        // ---------------------------------------------------------------- the tick

        /// <summary>Charge comes back once the agent has rested charge.rest since its last charged or targeted shot (tuning.md §6.3).</summary>
        void LabCharges()
        {
            foreach (var op in Ops)
            {
                if (op.Down || op.Charge >= Labr!.ChargeMax) continue;
                if (Tick - op.LastCharged > Labr.ChargeRestTicks) op.Charge = Math.Min(Labr.ChargeMax, op.Charge + Labr.ChargeRegen);
            }
        }

        /// <summary>A shooter's count on a target (brief 027 aim memory, tuning.md §6.1–6.2): today acc.start; the lab, the pair's remembered
        /// count, else fight.lab.accStart for a target it has not fired at.</summary>
        /// <summary>The count a shooter takes a target with: the pair's, for the mission, in both presets (tuning.md §6.1–6.2; today since §13.1,
        /// engine 0.18.0: the tap-switch reset closed); a target it has not fired at starts at acc.start (the lab: fight.lab.accStart).</summary>
        int AccFor(Gun g, string t) => g.Mem != null && g.Mem.TryGetValue(t, out int a) ? a : Lab ? Labr!.AccStart : R.AccStart;

        /// <summary>A fire order cleared elsewhere (a run, a carry, Board, the Take): its volley's unfired charge comes back, its walk into range ends.</summary>
        void LabTidy(Op op)
        {
            if (LabSnap) KeepCover(op);
            var g = op.Gun; if (g.Order.Length > 0) return;
            if (g.VolleyLeft > 0) Refund(op);
            if (op.CloseWalk) { op.CloseWalk = false; op.Walk = null; op.WalkMm = 0; }
        }
        void Refund(Op op)
        {
            var g = op.Gun;
            if (g.VolleyPer > 0) op.Charge = Math.Min(Labr!.ChargeMax, op.Charge + g.VolleyLeft * g.VolleyPer);
            g.VolleyLeft = 0; g.VolleyPer = 0;
        }

        /// <summary>The standing fire order ends (today: order, force, aim and lapse cleared). With the lab: a volley's unfired charge comes back,
        /// the walk into range ends, and when its target is down then free returns the agent to at will (a volley returns to its stance).</summary>
        void OrderEnd(Op op, bool down)
        {
            var g = op.Gun;
            g.Order = ""; g.Force = false; g.Target = ""; g.LostAt = -1;
            if (op.CloseWalk) { op.CloseWalk = false; op.Walk = null; op.WalkMm = 0; } // brief 030: the walk into range ends with the order, lab or not
            if (!Lab) return;
            bool volley = g.VolleyLeft > 0;
            if (volley) Refund(op);
            if (down && !volley && (g.Then == "free" || (g.Then.Length == 0 && LabThenFree))) g.Stance = "free";
            g.Then = "";
        }

        /// <summary>fire.close: walk the grid to the nearest free point inside the weapon's effective ring with a clear line to the target (nearest
        /// by walk, ties to the lower cell). False (nothing changed) if there is none within 60 m of walk.</summary>
        bool CloseIn(Op op, Pt at)
        {
            var path = CloseTo(op, at); if (path == null) return false;
            op.Walk = path; op.WalkMm = 0; op.Running = false; op.WalkSpeed = PaceOf(op, false); op.CloseWalk = true;
            return true;
        }
        /// <summary>The walk CloseIn would take now, or null (none within 60 m of walk). Changes nothing.</summary>
        Path? CloseTo(Op op, Pt at)
        {
            var w = op.Gun.W!; long eff2 = (long)w.EffCm * w.EffCm;
            var to = S.Grid.Nearest(op.Pos, p => G.D2(p, at) <= eff2 && Sees(p, at), CloseSearchCm);
            if (!to.HasValue) return null;
            var route = S.Grid.Route(op.Pos, to.Value); if (route == null) return null;
            var path = new Path(route);
            return path.Length == 0 ? null : path;
        }

        /// <summary>After a cell shot: the charge it drew, and a volley's count (its last shot ends the order: the agent returns to its stance).</summary>
        void AfterCellShot(Op op, string t)
        {
            var g = op.Gun;
            if (LabCharge && g.W!.Charge > 0) { op.Charge -= g.W.Charge; op.LastCharged = Tick; }
            if (g.VolleyLeft > 0 && t == g.Order)
            {
                if (LabCharge) op.LastCharged = Tick; // tuning.md §6.3: a targeted shot rests the bar too (a refund stamps nothing)
                g.VolleyLeft--;
                if (g.VolleyLeft == 0)
                {
                    Note("fire.done", $"{Sim.Op.Name(op.Id)}: the volley on {t} is fired. Back to {g.Stance}.");
                    g.VolleyPer = 0; g.Order = ""; g.Force = false; g.LostAt = -1; g.Then = ""; // the aim stays: at will on the same target keeps its count
                    if (op.CloseWalk) { op.CloseWalk = false; op.Walk = null; op.WalkMm = 0; }
                }
            }
        }

        // ---------------------------------------------------------------- the police warning (P10)

        /// <summary>Security sees a drawn weapon: with the warning on and no shot yet in the run, an officer, the guard, the patrol or a response unit
        /// shouts and gives policeWarning.ticks (Alert to policeWarning.alert, the guard remembers the face); a camera or any later sight is
        /// today's drawn-seen at once.</summary>
        void SawDraw(string w, Op op)
        {
            bool labWarn = Lab && LabWarn && !(Gunfire > 0 || CellFiredOn || FightBranch.Length > 0 || w.StartsWith("camera:", StringComparison.Ordinal));
            if (!labWarn && !TodayWarns(w, op)) { SecuritySawDraw(w, op); return; } // engine 0.19.0: today's police warning (ai.intent.police.warn), the lab's numbers otherwise
            int perWatcher = Lab ? Labr!.WarnPerWatcher : R.Intent.WarnPerWatcher, ticks = Lab ? Labr!.WarnTicks : R.Intent.WarnTicks, alertTo = Lab ? Labr!.WarnAlert : R.Intent.WarnAlert;
            string name = Sim.Op.Name(op.Id);
            // tuning.md §6.4: one shout a watcher a mission. An agent he sees drawn while his shout is open joins it (same window); once it
            // has closed, the next drawn weapon he sees is drawn-seen at once, no second chance.
            int open = warnings.FindIndex(x => x.Watcher == w);
            if (open < 0 && warnedBy.TryGetValue(w, out int given) && given >= perWatcher) { SecuritySawDraw(w, op); return; }
            if (open < 0) warnedBy[w] = (warnedBy.TryGetValue(w, out int g0) ? g0 : 0) + 1;
            warnings.Add(new LabWarning { Watcher = w, Op = op.Id, Until = open >= 0 ? warnings[open].Until : Tick + ticks, DrawTick = op.Gun.DrawTick });
            RaiseAlert(alertTo, $"drawn-warned:{w}:{name}");
            KnowFace(name); // engine 0.19.0 (ai.intent face): the watcher who shouts has the face
            if (w == "guard" && !GuardDown) Remember(GuardP, "saw-weapon-drawn", name, true);
        }
        /// <summary>Each open warning closes when its window ends or a shot is fired: the agent still drawn (the same draw), today's drawn-seen
        /// (Alert to drawSeenSecurity, he engages and calls it in); holstered in time, the watcher logs it: +policeWarning.trail once a shout (tuning.md §6.4).</summary>
        void WarningsStep()
        {
            if (warnings.Count == 0) return;
            bool shot = Gunfire > 0 || CellFiredOn;
            for (int k = 0; k < warnings.Count && !Over; )
            {
                var x = warnings[k];
                if (!shot && Tick < x.Until) { k++; continue; }
                warnings.RemoveAt(k);
                var op = OpOf(x.Op); var g = op.Gun;
                if (g.Drawn && g.HolsterAt < 0 && g.DrawTick == x.DrawTick && !op.Down && !op.Inside) SecuritySawDraw(x.Watcher, op);
                else if (!op.Down && warnLogged.Add($"{x.Watcher}/{x.Until}")) AddTrail(Lab ? Labr!.WarnTrail : R.Intent.WarnTrail, $"warning-heeded:{x.Watcher}"); // tuning.md §6.4: he logs it, once a shout
                if (Over) return;
            }
        }
        /// <summary>Under a warning nobody fires on the agent yet (the window is the point of it).</summary>
        bool Warned(Op op) { foreach (var x in warnings) if (x.Op == op.Id) return true; return false; }

        // ---------------------------------------------------------------- the orders: volley and field

        int FightKey() => FightBranch.Length > 0 ? FightStart : OpenFight != null ? OpenFight.Start : -1;

        /// <summary>Volley {op, target, shots}: exactly `shots` shots at the target at the weapon's cadence, then back to the stance. Paid when given
        /// (the weapon's target charge a shot; charge off: one of target.volleysPerFight this fight); refused no-charge if unpaid.</summary>
        string Volley(Op op, Order o)
        {
            if (!Lab) return No("unknown-verb", "verb 'volley' is the fight lab's (EXPERIMENTAL, brief 027): unknown = reject");
            var g = op.Gun;
            if (g.W == null) return No("no-weapon", $"{o.Op} carries no weapon (Arms)");
            if (o.Shots < 1 || o.Shots > Labr!.MaxShots) return No("no-charge", $"a volley is 1 to {Labr!.MaxShots} shots");
            int cost = LabCharge ? g.W.TargetCharge * o.Shots : 0;
            if (LabCharge && op.Charge < cost) return No("no-charge", $"the volley costs {cost} charge; the {o.Op} has {op.Charge}");
            if (!LabCharge) { if (op.VolleyFight != FightKey()) { op.VolleyFight = FightKey(); op.VolleysUsed = 0; } if (op.VolleysUsed >= Labr.VolleysPerFight) return No("no-charge", $"no volleys left this fight ({Labr.VolleysPerFight})"); }
            var f = new Order { Tick = o.Tick, Op = o.Op, Do = "fire", Target = o.Target, Force = false };
            int volleyLeft = g.VolleyLeft; g.VolleyLeft = 0; // a volley in progress is replaced: its charge comes back below
            string why = Fire(op, f, volley: true);
            g.VolleyLeft = volleyLeft;
            if (why.Length > 0) return why;
            if (g.VolleyLeft > 0) Refund(op);
            if (LabCharge) op.Charge -= cost; else op.VolleysUsed++;
            g.VolleyLeft = o.Shots; g.VolleyPer = LabCharge ? g.W.TargetCharge : 0; g.Then = "";
            Note("fire.volley", $"{Sim.Op.Name(op.Id)} fires a volley of {o.Shots} on {g.Order}{(LabCharge ? $" ({cost} charge)" : "")}.");
            return "";
        }

        /// <summary>Field {op} (fight.lab field on): pays field.cost, absorbs up to field.absorb damage for field.ticks; no fire while it holds.</summary>
        string Field(Op op, Order o)
        {
            if (!Lab || !LabField) return No("unknown-verb", "verb 'field' is the fight lab's, with field on (EXPERIMENTAL, brief 027): unknown = reject");
            if (op.Charge < Labr!.FieldCost) return No("no-charge", $"the field costs {Labr.FieldCost} charge; the {o.Op} has {op.Charge}");
            op.Charge -= Labr.FieldCost; op.FieldUntil = Tick + Labr.FieldTicks; op.FieldLeft = Labr.FieldAbsorb;
            Note("order.field", $"{Sim.Op.Name(op.Id)} raises the field ({Labr.FieldAbsorb} for {Labr.FieldTicks / Hz} s).");
            return "";
        }

        /// <summary>The field takes a hit's damage first (up to what it has left) while it holds.</summary>
        int Absorb(Op op, int dmg)
        {
            if (!Lab || op.FieldUntil <= Tick || op.FieldLeft <= 0) return dmg;
            int a = Math.Min(dmg, op.FieldLeft); op.FieldLeft -= a; return dmg - a;
        }

        // ---------------------------------------------------------------- reads for the host (read-only)

        /// <summary>An agent's charge bar (0 with the lab off).</summary>
        public int ChargeOf(OpId op) => OpOf(op).Charge;
        /// <summary>The field's ticks left on an agent (0 when none holds) and the damage it still absorbs.</summary>
        public (int ticks, int absorb) FieldOf(OpId op) { var o = OpOf(op); return o.FieldUntil > Tick ? (o.FieldUntil - Tick, o.FieldLeft) : (0, 0); }

        /// <summary>Everyone who is a combatant now (a final-fight hostile, or a guard, patrol, officer, rival crew or response unit that has fired
        /// or aims at the cell), up and on the street: whom the bars go over and at-will fire picks from. Read-only.</summary>
        public List<string> Combatants()
        {
            var l = new List<string>();
            foreach (var id in CombatantIds()) if (!l.Contains(id) && Combatant(id)) l.Add(id);
            return l;
        }

        /// <summary>Brief 030: whether a fire order out of reach walks in: the lab's fire.close with the lab on, else the rulebook's weapons.close.</summary>
        bool CloseOn => Lab ? LabClose : R.FireClose;
        /// <summary>Why a fire order on `target` would be refused now, without giving it ("" if it would be taken); `t` the target it resolves to.</summary>
        string FireRefusal(Op op, string target, bool force, out string t, out bool walk, bool ignoreDriven = false)
        {
            t = ""; walk = false; var g = op.Gun;
            if (g.W == null) return "no-weapon";
            if (op.Carrying.HasValue || op.CarryBody.Length > 0) return "fire-carrying";
            if (CarriesVale(op)) return "fire-carrying-vale";
            t = FireTarget(op, target);
            if (t.Length == 0) return "no-target";
            if (IsOpId(t)) return "fire-on-cell";
            if (Range && !RangeTarget(t)) return "target-unknown"; // brief 029: only what the range spawned (hostiles, bystanders, cars)
            var tg = GunOf(t); if (tg == null) return "target-unknown";
            if (IsCivilian(t) && !force) return "civilian-target";
            var at = PosOf(t);
            if (!at.HasValue || tg.Down) return "no-target";
            bool far = !G.Within(op.Pos, at.Value, g.W.MaxCm), blind = !far && !Sees(op.Pos, at.Value);
            if (!far && !blind) return "";
            if (!CloseOn) return far ? "out-of-range" : "no-clear-line";
            walk = !op.Driven || ignoreDriven; return "";
        }

        /// <summary>Hits to "down" (FirePreview.DownIs) at `dmg` a hit: a car not yet disabled counts to car.target.disabledAt ×healthScale, at least
        /// one; anything else (a disabled car, a person, a camera) to 0.</summary>
        int HitsToDown(string t, Gun tg, int dmg, out string downIs)
        {
            downIs = "down"; int left = tg.Health;
            if (IsCarId(t)) { if (CarDisabled(t)) downIs = "burns"; else { downIs = "disabled"; left = tg.Health - R.CarDisabledAt * HealthScale; } }
            return Math.Max(1, (left + dmg - 1) / dmg);
        }

        /// <summary>Brief 027 P1, the chip: pips, damage a hit, hits and shots to down, charge a shot, the walk into range (fire.close) and the
        /// price, for agent `op` on `target` now. Works with the lab off too (pips 0, today's chance). Read-only: nothing changes.</summary>
        /// <summary>Brief 030: the same chip for a driven agent as if the engine walked it in (the host's preview of the walk-in for a fire while
        /// the agent is driven: the engine itself never walks a driven agent; the host releases it, or drives it there). Read-only.</summary>
        public FirePreview PreviewFireWalkIn(OpId opId, string target, bool force = false) => PreviewFire(opId, target, force, ignoreDriven: true);
        public FirePreview PreviewFire(OpId opId, string target, bool force = false) => PreviewFire(opId, target, force, ignoreDriven: false);
        FirePreview PreviewFire(OpId opId, string target, bool force, bool ignoreDriven)
        {
            var p = new FirePreview(); var op = OpOf(opId); var g = op.Gun;
            p.Charge = op.Charge; p.ChargeMax = Lab ? Labr!.ChargeMax : 0;
            if (op.Down) { p.Refused = "agent-down"; return p; }
            p.Refused = FireRefusal(op, target, force, out var t, out bool walk, ignoreDriven);
            if (p.Refused.Length > 0 && p.Refused != "out-of-range" && p.Refused != "no-clear-line") return p;
            var tg = GunOf(t)!; var at = PosOf(t)!.Value; var w = g.W!;
            long d = Dist(op.Pos, at); p.DistanceCm = (int)Math.Min(int.MaxValue, d);
            p.Health = tg.Health; p.Max = tg.Max;
            p.Damage = w.DamageAt(d); p.HitsToDown = HitsToDown(t, tg, p.Damage, out p.DownIs);
            p.ChargePerShot = Lab && LabCharge ? w.Charge : 0; p.TargetChargePerShot = Lab && LabCharge ? w.TargetCharge : 0;
            p.Band = d <= R.PointBlankCm ? "point" : d <= w.EffCm ? "eff" : d <= w.MaxCm ? "long" : "out";
            p.Line = d <= w.MaxCm && Sees(op.Pos, at);
            p.Moving = op.Walk != null || op.DriveMoved; p.Untrained = !g.Trained; p.High = LevelOf(op.Pos) > LevelOf(at); p.Cover = CoverMod(t, at, op.Pos) < 1000;
            Pt from = op.Pos;
            if (walk)
            {
                long eff2 = (long)w.EffCm * w.EffCm;
                var wk = S.Grid.Nearest(op.Pos, q => G.D2(q, at) <= eff2 && Sees(q, at), CloseSearchCm);
                var route = wk.HasValue ? S.Grid.Route(op.Pos, wk.Value) : null;
                var path = route == null ? null : new Path(route);
                if (path == null || path.Length == 0) { p.Refused = p.DistanceCm > w.MaxCm ? "out-of-range" : "no-clear-line"; return p; }
                p.Walk = true; p.WalkTo = wk!.Value;
                p.WalkS = (int)((path.Length + opSpeed * Hz - 1) / (opSpeed * Hz));
                // the engine's walk, tick by tick at a walk (CloseIn: PaceOf walking): it stops at the first tick's position inside the ring with a line
                int pace = PaceOf(op, false), k = 0; Pt q2 = op.Pos; var exposed = new SortedSet<string>(StringComparer.Ordinal); var foes = Combatants();
                while (true)
                {
                    k++; long mm = Math.Min(path.Length, (long)k * pace); q2 = path.At(mm);
                    foreach (var f in foes) { if (exposed.Contains(f)) continue; var fg = GunOf(f); var fp = PosOf(f); if (fg?.W != null && fp.HasValue && G.D2(fp.Value, q2) <= (long)fg.W.EffCm * fg.W.EffCm && Sees(fp.Value, q2)) exposed.Add(f); }
                    if ((G.D2(q2, at) <= eff2 && Sees(q2, at)) || mm >= path.Length) break;
                }
                p.StopAt = q2; p.WalkTicks = k; p.WalkExposedTo.AddRange(exposed);
                from = q2; d = Dist(from, at); p.DistanceCm = (int)Math.Min(int.MaxValue, d);
                p.Damage = w.DamageAt(d); p.HitsToDown = HitsToDown(t, tg, p.Damage, out p.DownIs);
                p.Band = d <= R.PointBlankCm ? "point" : d <= w.EffCm ? "eff" : d <= w.MaxCm ? "long" : "out";
                p.Line = d <= w.MaxCm && Sees(from, at); p.Moving = false; p.High = LevelOf(from) > LevelOf(at); p.Cover = CoverMod(t, at, from) < 1000;
            }
            var gait = walk ? Sim.Gait.Stand : OpGait(op, at);
            p.Gait = gait == Sim.Gait.Walk ? "walk" : gait == Sim.Gait.Run ? "run" : gait == Sim.Gait.RunAway ? "run-away" : "stand"; p.TargetRunning = TargetRunning(t);
            int start = g.Target == t ? g.Acc : AccFor(g, t); bool vale = op.Id == OpId.Enforcer && Vale == ValeAt.WithEnforcer;
            int ToDown(int acc) { if (acc <= 0) return -1; int a = start, hits = 0, shots = 0; while (hits < p.HitsToDown && shots < 1000) { shots++; a += acc; if (a >= 1000) { a -= 1000; hits++; } } return shots; }
            if (p.Line) { p.Acc = ShotAcc(g, t, from, at, d, gait, vale, out int pips); p.Pips = pips; p.ShotsToDown = ToDown(p.Acc); }
            else p.ShotsToDown = -1;
            if (!walk && p.ShotsToDown > 0) // preview only: the time to down at the weapon's cadence (Aim: a new target waits aimNew; Shoot: every)
            {
                int first = !g.Drawn || g.HolsterAt >= 0 ? DrawT + AimNewT : Math.Max(0, Math.Max(g.DrawReady, g.Target == t ? g.NextShot : Math.Max(g.NextShot, Tick + 1 + AimNewT)) - Tick); // an order given now aims on the next tick
                p.TicksToDown = first + (p.ShotsToDown - 1) * w.Every;
            }
            if (!walk && op.Walk != null) // §8.8: now → on arrival
            {
                var e = op.Walk.End; long de = Dist(e, at);
                if (de <= w.MaxCm && Sees(e, at)) { p.HasEnd = true; p.EndAt = e; p.AccEnd = ShotAcc(g, t, e, at, de, Sim.Gait.Stand, vale, out int pe); p.PipsEnd = pe; p.ShotsToDownEnd = ToDown(p.AccEnd); }
            }
            p.Untraced = Range || IsFightHostile(t) && !R.FightTraced && FightOverTick == INF; // brief 029: nothing in the range is traced; tuning.md §9.1: traced after fight.over
            if (!p.Untraced) { p.TrailShooting = fireIncident.ContainsKey(t) ? 0 : R.GunfireTrail; p.TrailHit = tg.Hit || IsPropertyId(t) ? 0 : R.HitTrail; p.TrailDown = IsPropertyId(t) ? 0 : R.DownTrail; } // a camera or a car: gunfire only (§9.2, §10.7)
            return p;
        }

        /// <summary>Brief 027 §3.3, target mode's forecast: shot by shot, hit or miss and the damage, from this agent's aim on the target now
        /// (its accumulator if it is aimed there, else acc.start), the pips now and the target's health now: "if they stand". Read-only.</summary>
        public VolleyPreview PreviewVolley(OpId opId, string target, int shots)
        {
            var v = new VolleyPreview(); var op = OpOf(opId); var g = op.Gun;
            if (!Lab) { v.Refused = "unknown-verb"; return v; }
            if (op.Down) { v.Refused = "agent-down"; return v; }
            if (shots < 1 || shots > Labr!.MaxShots) { v.Refused = "no-charge"; return v; }
            var fp = PreviewFire(opId, target);
            v.Pips = fp.Pips; v.Acc = fp.Acc; v.Damage = fp.Damage; v.HealthAfter = fp.Health;
            v.Walk = fp.Walk; v.StopAt = fp.StopAt; v.WalkTicks = fp.WalkTicks; v.WalkExposedTo.AddRange(fp.WalkExposedTo);
            if (fp.Refused.Length > 0) { v.Refused = fp.Refused; return v; }
            v.ChargeCost = LabCharge ? fp.TargetChargePerShot * shots : 0; v.ChargeLeft = op.Charge - v.ChargeCost;
            if (!LabCharge) { int used = op.VolleyFight == FightKey() ? op.VolleysUsed : 0; v.VolleysLeft = Labr.VolleysPerFight - used; if (v.VolleysLeft <= 0) v.Refused = "no-charge"; }
            else if (v.ChargeLeft < 0) v.Refused = "no-charge";
            string t = FireTarget(op, target);
            int a = g.Target == t ? g.Acc : AccFor(g, t), hp = fp.Health;
            for (int k = 0; k < shots; k++)
            {
                bool hit = false; if (fp.Acc > 0) { a += fp.Acc; hit = a >= 1000; if (hit) a -= 1000; }
                int dm = hit && hp > 0 ? fp.Damage : 0; hp = Math.Max(0, hp - dm);
                v.Shots.Add((hit, dm, hp));
            }
            v.HealthAfter = hp; v.Down = hp == 0;
            return v;
        }

        // ---------------------------------------------------------------- cover snap (B5)

        /// <summary>coverSnap is on for this run (the lab and its switch).</summary>
        public bool CoverSnapOn => Lab && LabSnap;
        /// <summary>The snap radius and the marks' reach, in cm (0 with the snap off).</summary>
        public int CoverSnapCm => CoverSnapOn ? Labr!.CoverSnapCm : 0;
        public int CoverMarksCm => CoverSnapOn ? Labr!.CoverMarksCm : 0;

        /// <summary>Who holds a cover point ("" free): a hostile that took it (GameAi) or an operative that snapped to it.</summary>
        string CoverHolder(string id) => coverTaken.TryGetValue(id, out var who) ? who : "";
        /// <summary>As CoverHolder, and also a hostile up on the street that stands on the point (within ai.cover.snap) or walks to it, whether or
        /// not its bookkeeping still holds it (Motive tuning.md §7.5: the blocker's cv-087 was let go by cover.leave on its walk there, and the
        /// Scout snapped onto the point it was still walking to). Lab only (the snap and the marks).</summary>
        string CoverHolder(CoverPoint c)
        {
            var who = CoverHolder(c.Id); if (who.Length > 0) return who;
            foreach (var h in Hostiles)
            {
                if (h.Down || !h.Present || h.Gun.St == "gone") continue;
                if (G.Within(h.Pos(Tick), c.At, R.CoverSnapCm) || (h.Gun.St != "break" && h.Moving(Tick) && h.Run != null && G.Within(h.Run.End, c.At, R.CoverSnapCm))) return h.Id; // a breaking rival holds only where it still stands
            }
            return "";
        }

        /// <summary>The nearest combatant to a point (by distance, ties by id), or "" when none: what a snapped agent faces, and whom the snap
        /// prefers a point to shield against.</summary>
        string NearestCombatant(Pt p)
        {
            string best = ""; long bd = long.MaxValue;
            foreach (var id in Combatants())
            {
                var at = PosOf(id); if (!at.HasValue) continue; long d = G.D2(p, at.Value);
                if (d < bd || (d == bd && string.CompareOrdinal(id, best) < 0)) { bd = d; best = id; }
            }
            return best;
        }

        /// <summary>Where a move by `op` (drawn, not driven) ending at `to` snaps (null: it doesn't): a cover point of the site within coverSnap.snap of `to`, present
        /// now, at least partial, held by nobody else, joined to `to` by a straight walkable line (never through a wall or the cover object
        /// itself). The one that shields against the nearest combatant first, then the nearest to `to`, then the lower id.</summary>
        CoverPoint? SnapPoint(Op op, Pt to)
        {
            if (!CoverSnapOn || LiveCover.Count == 0 || op.Driven || !op.Gun.Drawn || op.Gun.W == null) return null; // a drawn agent's move only (P7: the marks show for a drawn agent)
            int sec = Tick / Hz; long r2 = (long)Labr!.CoverSnapCm * Labr.CoverSnapCm; string me = Sim.Op.Name(op.Id);
            string foe = NearestCombatant(to); Pt? fp = foe.Length > 0 ? PosOf(foe) : null;
            CoverPoint? best = null; int bestShield = 2; long bestD = long.MaxValue;
            foreach (var c in LiveCover)
            {
                if (!c.Present(sec) || c.Q < R.CoverQPartial) continue;
                long d = G.D2(to, c.At); if (d > r2) continue;
                var who = CoverHolder(c); if (who.Length > 0 && who != me) continue; // retest §7.5: never onto a point a hostile stands on or walks to
                if (!S.Grid.LineFree(to, c.At)) continue;
                int shield = fp.HasValue && CoverRules.Protects(c, fp.Value, sec, R.PointBlankCm) ? 0 : 1;
                if (shield < bestShield || (shield == bestShield && (d < bestD || (d == bestD && string.CompareOrdinal(c.Id, best!.Id) < 0)))) { best = c; bestShield = shield; bestD = d; }
            }
            return best;
        }

        /// <summary>The operative takes the point (it can't be snapped to or chosen by a hostile while held).</summary>
        void HoldCover(Op op, CoverPoint c) { DropCover(op); coverTaken[c.Id] = Sim.Op.Name(op.Id); op.Gun.Cover = c.Id; }
        void DropCover(Op op)
        {
            var g = op.Gun; if (g.Cover.Length == 0) return;
            if (coverTaken.TryGetValue(g.Cover, out var who) && who == Sim.Op.Name(op.Id)) coverTaken.Remove(g.Cover);
            g.Cover = "";
        }
        /// <summary>Each tick: the agent keeps its point while it walks to it or stands within ai.cover.snap of it, up, on the street and present.</summary>
        void KeepCover(Op op)
        {
            var g = op.Gun; if (g.Cover.Length == 0) return;
            var c = CoverById(g.Cover);
            bool keep = c != null && !op.Down && !op.Inside && !op.InCar && c.Present(Tick / Hz)
                && (op.Walk != null ? op.Walk.End == c.At && !op.CloseWalk : G.Within(op.Pos, c.At, R.CoverSnapCm));
            if (!keep) DropCover(op);
        }

        /// <summary>Where a move by the agent to `to` would end now: the snapped cover point, or null (it ends at `to`). Read-only.</summary>
        public CoverPoint? PreviewSnap(OpId opId, Pt to) { var op = OpOf(opId); return op.Down || !S.Grid.Free(to) ? null : SnapPoint(op, to); }
        /// <summary>The cover point the agent holds ("" none).</summary>
        public string CoverOf(OpId opId) => OpOf(opId).Gun.Cover;
        /// <summary>Whom a snapped agent faces: the nearest combatant ("" none: face the point's own Faces). Presentation only.</summary>
        public string FacingOf(OpId opId) => NearestCombatant(OpOf(opId).Pos);
        /// <summary>The marks to draw for an agent (P7): the site's cover points present now, at least partial, within coverSnap.marks of it, by
        /// id. Empty with the snap off, or the agent down or not drawn. Read-only.</summary>
        public List<CoverMark> CoverMarks(OpId opId)
        {
            var l = new List<CoverMark>(); if (!CoverSnapOn) return l;
            var op = OpOf(opId); if (op.Down || !op.Gun.Drawn) return l; // P7: marks for a drawn agent
            int sec = Tick / Hz; long m2 = (long)Labr!.CoverMarksCm * Labr.CoverMarksCm;
            var foe = NearestCombatant(op.Pos); Pt? fp = foe.Length > 0 ? PosOf(foe) : null;
            foreach (var c in LiveCover)
            {
                if (!c.Present(sec) || c.Q < R.CoverQPartial || G.D2(op.Pos, c.At) > m2) continue;
                l.Add(new CoverMark { Id = c.Id, Kind = c.Kind, Of = c.Of, At = c.At, Faces = c.Faces, Cos = c.Cos, Q = c.Q, Full = c.Q >= R.CoverQFull, HeldBy = CoverHolder(c),
                    Shields = fp.HasValue && CoverRules.Protects(c, fp.Value, sec, R.PointBlankCm), DistanceCm = (int)Dist(op.Pos, c.At) });
            }
            l.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return l;
        }

        // ---------------------------------------------------------------- the hash (only with the lab on)

        internal void HashLab(Hash.H h)
        {
            h.S("lab");
            foreach (var op in Ops) { var g = op.Gun; h.I(op.Charge).I(op.LastCharged).I(op.FieldUntil).I(op.FieldLeft).I(op.VolleyFight).I(op.VolleysUsed).Z(op.CloseWalk).I(op.CloseAt).I(g.VolleyLeft).I(g.VolleyPer).S(g.Then); }
            foreach (var x in warnings) h.S(x.Watcher).I((int)x.Op).I(x.Until).I(x.DrawTick);
            foreach (var kv in warnedBy) h.S(kv.Key).I(kv.Value);
            foreach (var x in warnLogged) h.S(x);
            if (LabKite) HashKite(h);
        }
    }
}
