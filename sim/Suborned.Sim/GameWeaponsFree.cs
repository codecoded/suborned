// Weapons free (engine 0.19.0; Motive, briefs/027/motive/tuning.md §14; Sky, brief 041 "Syndicate fights" adopted 2026-10-21, and Sky's §14
// note: "fight lasts as long as energy / health is there, or player moves away"; rulebook weapons.free). Both presets.
//  · A logged cell input ({do: free, op: cell, on}), on its tick; at the start the start's weaponsFree, else weapons.free.start (on).
//  · While it is on and a fight is on, a drawn agent at stance return or free with no fire order (a lock or a focus), no trigger and no
//    follow picks its own target: its attacker first (a combatant aiming at it, or its last shooter within weapons.lineLapse), else the
//    nearest combatant in its max ring on a line (integer distance, ties in CombatantIds order); it keeps that target while it stays a
//    combatant in reach and line, and changes only for an attacker when the one in hand is not one. Never a non-combatant, never at hold,
//    never a draw, never a walk.
//  · Precedence (CellShooter): a fire order, the trigger, follow, then weapons free. In a fight under weapons free follow takes only a
//    target that is no combatant (CanFollow): the opening shot, a car, a camera, a calm guard.
//  · Cease (X) stands an agent down (Gun.StandDown): it picks again once shot at after that, given a fire order or a trigger, or once the
//    fight goes off.
//  · A fight is on from a shot by or at the cell, while any combatant up aims at an operative up on the street inside its weapon's max ring
//    on a line (contact), and goes off weapons.free.settle (line-lapse-s) after the last contact or shot. No clock beyond that settle.
//  · A focus (a fire order with focus) is a lock: lasting until its target is down, its walk-in spent.
// Every pick is derived from hashed state (positions, lines, each hostile's Target, ShotBy, ShotByTick, the fight's ticks) and logged
// (fire.free); the host never issues one. Off, a drawn agent returns fire exactly as engine 0.18.0.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>Who is shooting at an operative now (the host's under-fire cue; Sky's answer 1, hostile-initiative.md: no auto-draw, but the
    /// avatar shows clearly it is being shot at): the shooter, the tick its shots on this agent began, the tick of its last one, and whether
    /// its gun is still on the agent. Read-only.</summary>
    public struct UnderFireBy { public string By; public int Since, Last; public bool Aiming; }

    public sealed partial class Game
    {
        /// <summary>Weapons free (tuning.md §14): the start's weaponsFree, else the rulebook's weapons.free.start; then free orders.</summary>
        public bool WeaponsFree;
        int wfFrom = -1, wfLast = -1, wfShot = -1; // the fight on since, its last contact or shot, the last tick a shot by or at the cell landed
        /// <summary>A fight is on for weapons free (tuning.md §14; Sky's §14 note): from a shot by or at the cell, while there is contact, until
        /// weapons.free.settle after the last contact or shot. Read-only for the host.</summary>
        public bool FreeFightOn => wfFrom >= 0;
        /// <summary>The tick the fight came on, -1 when none is on.</summary>
        public int FreeFightSince => wfFrom;

        /// <summary>The weapons free setting changed in play (op "cell", on). Logged like any order.</summary>
        string FreeSet(Order o)
        {
            if (o.Op != "cell") return No("free-not-cell", "weapons free is the cell's: op \"cell\"");
            if (WeaponsFree != o.On) Note("fire.free-setting", o.On ? "Weapons free: drawn agents fire on their own in a fight." : "Weapons free off: drawn agents return fire only.");
            WeaponsFree = o.On;
            if (!o.On) foreach (var op in Ops) op.Gun.StandDown = -1; // off, no stand-down is left to read
            return "";
        }

        bool FreeApplies => WeaponsFree && wfFrom >= 0;

        /// <summary>A hostile `id` is attacking `op`: its gun aims at it, or it is the agent's last shooter within weapons.lineLapse.</summary>
        bool Attacks(Op op, string id)
        {
            var hg = GunOf(id); if (hg == null) return false;
            var g = op.Gun; string me = Sim.Op.Name(op.Id);
            return hg.Target == me || (g.ShotBy == id && g.ShotByTick >= 0 && Tick - g.ShotByTick <= T(R.LineLapseS));
        }

        /// <summary>The weapons-free pick (§14.1): attacker first, else the nearest combatant in the max ring on a line; the one in hand is kept
        /// unless an attacker appears and it is not one. "" when stood down and not shot at since. Read-only.</summary>
        string FreeTarget(Op op)
        {
            var g = op.Gun;
            if (g.StandDown >= 0 && !(g.ShotByTick > g.StandDown)) return "";
            long mx = (long)g.W!.MaxCm * g.W.MaxCm;
            string best = ""; long bd = long.MaxValue; int bc = 9;
            foreach (var id in CombatantIds())
            {
                if (!Combatant(id)) continue;
                var at = PosOf(id); if (!at.HasValue) continue;
                long d = G.D2(op.Pos, at.Value); if (d > mx || !Sees(op.Pos, at.Value)) continue;
                int c = Attacks(op, id) ? 1 : 2;
                if (c < bc || (c == bc && d < bd)) { bc = c; bd = d; best = id; }
            }
            if (g.Target.Length > 0 && g.Target != best && !IsOpId(g.Target) && Combatant(g.Target) && InLine(op.Pos, g.Target, g.W.MaxCm) && (Attacks(op, g.Target) ? 1 : 2) <= bc) return g.Target; // kept
            return best;
        }

        /// <summary>The stance's own target as engine 0.18.0 had it: hold nobody, return its shooter, free the nearest combatant.</summary>
        string BaseStanceTarget(Op op)
        {
            var g = op.Gun;
            if (g.Stance == "hold") return "";
            if (g.Stance == "return") return g.ShotBy.Length > 0 && Combatant(g.ShotBy) && InLine(op.Pos, g.ShotBy, g.W!.MaxCm) ? g.ShotBy : "";
            string best = ""; long bd = long.MaxValue;
            foreach (var id in CombatantIds())
            {
                if (!Combatant(id)) continue;
                var at = PosOf(id); if (!at.HasValue) continue;
                long d = G.D2(op.Pos, at.Value);
                if (d < bd && d <= (long)g.W!.MaxCm * g.W.MaxCm && Sees(op.Pos, at.Value)) { bd = d; best = id; }
            }
            return best;
        }

        /// <summary>Contact (Sky's §14 note): a combatant up whose gun aims at an operative up on the street, inside its weapon's max ring, on a line.</summary>
        bool Contact()
        {
            foreach (var id in CombatantIds())
            {
                if (!Combatant(id)) continue;
                var hg = GunOf(id)!; if (hg.W == null || !IsOpId(hg.Target)) continue;
                var at = PosOf(id); var o = OpOf(Sim.Op.Parse(hg.Target));
                if (!at.HasValue || o.Down || o.Inside || o.InCar) continue;
                if (G.D2(at.Value, o.Pos) <= (long)hg.W.MaxCm * hg.W.MaxCm && Sees(at.Value, o.Pos)) return true;
            }
            return false;
        }

        /// <summary>The end of a fight tick (after Resolve): the fight comes on at a shot by or at the cell, stays on while there is contact,
        /// and goes off weapons.free.settle after the last contact or shot; a stand-down shot at since ends; the fight off ends them all.</summary>
        void FreeStep()
        {
            foreach (var op in Ops) { var g = op.Gun; if (g.StandDown >= 0 && g.ShotByTick > g.StandDown) g.StandDown = -1; }
            if (wfShot == Tick)
            {
                if (wfFrom < 0) { wfFrom = Tick; if (WeaponsFree) Note("fire.free-on", "A fight is on: drawn agents fire on their own."); }
                wfLast = Tick; return;
            }
            if (wfFrom < 0) return;
            if (Contact()) { wfLast = Tick; return; }
            if (Tick - wfLast < T(R.FreeSettleS)) return;
            wfFrom = -1; wfLast = -1;
            foreach (var op in Ops) op.Gun.StandDown = -1;
            if (WeaponsFree) Note("fire.free-off", "No contact: the fight is off. Drawn agents hold their fire.");
        }

        /// <summary>Weapons free's state for the checkpoint hash: the setting when it is not the start's, the fight's ticks while one is on.</summary>
        internal void HashFree(Hash.H h)
        {
            if (WeaponsFree != (Start.WeaponsFree ?? R.FreeStart)) h.S("free").Z(WeaponsFree);
            if (wfFrom >= 0) h.S("freefight").I(wfFrom).I(wfLast);
        }

        // ---------------------------------------------------------------- the host's reads (change nothing)

        readonly Dictionary<string, (int since, int last)>[] underFire = { new(StringComparer.Ordinal), new(StringComparer.Ordinal), new(StringComparer.Ordinal), new(StringComparer.Ordinal) }; // output only: not hashed (as opShotTick)

        /// <summary>A shot at an operative landed (Resolve): the host's under-fire record (output only).</summary>
        void UnderFireShot(string by, string t)
        {
            var d = underFire[(int)Sim.Op.Parse(t)];
            if (d.TryGetValue(by, out var x) && Tick - x.last <= T(R.LineLapseS)) d[by] = (x.since, Tick); else d[by] = (Tick, Tick);
        }

        /// <summary>Who is shooting at this operative now (Sky's answer 1: the under-fire cue on the portrait and in the world): every shooter
        /// whose last shot at it landed within weapons.lineLapse, in id order, with when its spell began, its last shot, and whether it still
        /// aims at the agent. Empty when nobody is. Read-only.</summary>
        public List<UnderFireBy> UnderFireOf(OpId id)
        {
            var l = new List<UnderFireBy>(); var d = underFire[(int)id]; string me = Sim.Op.Name(id);
            var keys = new List<string>(d.Keys); keys.Sort(StringComparer.Ordinal);
            foreach (var k in keys)
            {
                var (since, last) = d[k]; if (Tick - last > T(R.LineLapseS)) continue;
                var g = GunOf(k); if (g == null || g.Down) continue;
                l.Add(new UnderFireBy { By = k, Since = since, Last = last, Aiming = g.Target == me });
            }
            return l;
        }
        /// <summary>The agent's stand-down tick (cease under weapons free), -1 none. Read-only.</summary>
        public int StandDownOf(OpId id) => OpOf(id).Gun.StandDown;
    }
}
