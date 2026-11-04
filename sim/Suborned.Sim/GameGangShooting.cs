// The gang shooting (engine 0.41.0, brief 096; Motive briefs/096/motive/rivals.html §4 and Q1 (a), rivals.json veteran; Sky 2026-11-04 "all
// recommended"; rulebook security.unclaimed, car.target.blastStanding). A shooting at the rival crew or its car that nobody of security saw
// (its reporter is "heard"), that no city person saw (the shooter or the target), with nobody but the crew hit, is filed as a gang shooting:
// Alert 2 and the response come as for any shooting, and its Trail and Standing are charged, but the district does not seal. The seal waits for
// the cell to be confirmed: an operative in a response officer's or robot's sight from its arrival until security.unclaimed.hold after it, a
// known face in its sight later, a later shooting that is not unclaimed, any later incident, or Alert 3. Then the seal runs from the later of
// that moment and the response's arrival, as any. Also here: a blast the cell set off with city people near is a menace act (blastStanding).
// Hashed with the response (HashGang), only once a gang shooting has been filed.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>A gang shooting is filed and the cell not yet placed: the seal waits.</summary>
        public bool GangPending { get; private set; }
        /// <summary>The tick the cell was placed at a gang shooting (the seal ran from then), -1 never; and why. Output only.</summary>
        public int GangClaimedAt { get; private set; } = -1; public string GangClaimedWhy { get; private set; } = "";
        int gangHoldUntil = INF; bool gangFiled, gangShotNow, gangSpoiled; CauseOrigin? gangOrigin; readonly HashSet<string> blastStood = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>The cell's shot at this target is at the crew or its car (a candidate for a gang shooting): set over CellGunfire's own incidents.</summary>
        static bool GangTarget(string t) => IsCrewId(t) || t == "car:rivals";

        /// <summary>CellGunfire, once its reporter is known: an unclaimed shot calls the response with no seal; any other claims a pending one.</summary>
        void GangOrCall(string t, string who, bool civSaw, int delay, string text, Pt scene, Func<CauseOrigin?> origin)
        {
            bool gang = !Range && GangTarget(t) && who == "heard" && !civSaw && !gangSpoiled && Alert < 3;
            if (!gang)
            {
                if (GangTarget(t) || IsPersonTarget(t)) gangSpoiled = true; // a seen shooting: no later shot of this mission is unclaimed
                if (GangPending) GangClaimed(civSaw ? "a passer-by saw the shooting" : who != "heard" ? $"the shooting was seen ({who})" : "another shooting");
                CallSecurity(delay, text, scene, origin: origin);
                return;
            }
            int lockBefore = Lockdown; gangOn = true;
            CallSecurity(delay, text, scene, origin: origin);
            gangOn = false;
            if (!gangFiled) { gangFiled = true; GangPending = true; gangOrigin = origin(); Note("security.gang", "Nobody saw who fired: the shooting is filed as a gang shooting. Police come, but the district isn't sealed unless they place the cell."); Chain.Add("gang-shooting"); }
            if (GangPending) gangHoldUntil = SecurityArrives + T(R.UnclaimedHoldS);
            if (Lockdown != lockBefore) { Lockdown = lockBefore; } // belt: CallSecurity sets no seal while gangOn
        }
        /// <summary>CallSecurity reads it: the call of a gang shooting sets the response's arrival and no seal.</summary>
        bool gangOn;

        /// <summary>The cell is placed: the seal runs from the later of now and the response's arrival.</summary>
        void GangClaimed(string why)
        {
            if (!GangPending) return;
            GangPending = false; GangClaimedAt = Tick; GangClaimedWhy = why;
            int at = Math.Max(Tick, SecurityArrives); if (at >= INF) { Note("security.gang-claimed", $"The cell is placed at the gang shooting: {why}."); return; }
            int lockBefore = Lockdown, seal = at + T(SealS);
            if (seal < Lockdown) { Lockdown = seal; SealMoved(lockBefore); NoteSealOrigin(lockBefore, () => gangOrigin); }
            Note("security.gang-claimed", $"The cell is placed at the gang shooting: {why}.");
            if (Lockdown != lockBefore) Note("seal.starts", $"The cell is placed: the district seals {SealS} s after the unit is on the street ({Sim.Report.Clock(at)}), at {Sim.Report.Clock(Lockdown)}.");
        }

        /// <summary>Once a tick: an operative in the response's sight from its arrival to the hold's end claims it; later, a known face in its sight.</summary>
        void GangStep()
        {
            if (!GangPending || Over || Tick < SecurityArrives) return;
            bool hold = Tick <= gangHoldUntil;
            foreach (var op in Ops)
            {
                if (op.Down || op.Inside || op.InCar) continue;
                string name = Sim.Op.Name(op.Id);
                if (!hold && !faces.Contains(name)) continue;
                if (ResponseSees(op.Pos)) { GangClaimed(hold ? $"the police saw the {name} at the scene" : $"the police know the {name}'s face"); return; }
            }
        }
        /// <summary>A response officer or robot on the street has p in its sight (their cones, as the car's watchers read them).</summary>
        bool ResponseSees(Pt p)
        {
            for (int k = 0; k < SecGuns.Length; k++) if (!SecGuns[k].Down && SecOfficerWatching(k, p)) return true;
            foreach (var u in Units) if (!u.Legacy && !u.Patrol && u.OnStreet(Tick) && !u.Gun.Down && UnitSees(u, p)) return true;
            return false;
        }
        /// <summary>AddIncident and RaiseAlert read it: a new incident (not the gang shooting's own) or Alert 3 places the cell.</summary>
        void GangIncident(string kind) { if (GangPending && !gangShotNow) GangClaimed($"a new incident ({kind})"); }

        /// <summary>Blast (GameCars.cs): a car the cell set burning explodes with a city person on the street within blastStanding.near: Standing
        /// moves by blastStanding.v, once a car.</summary>
        void BlastStandingStep(CarTarget c, Pt at)
        {
            if (Range || !IsOpId(c.By) || R.BlastStanding == 0 || !blastStood.Add(c.Id)) return;
            foreach (var (p, wp) in OnTheStreet())
                if (p.Civilian && G.Within(wp, at, R.BlastNearCm)) { MoveStanding(R.BlastStanding, "blast-near:" + c.Id, "blast-near", c.Id); Note("standing.blast", $"The {CarName(c.Id)} blows up with city people nearby."); return; }
        }

        internal void HashGang(Hash.H h)
        {
            if (!gangFiled) return;
            h.S("gang").Z(GangPending).I(GangClaimedAt).I(gangHoldUntil).Z(gangSpoiled);
        }
    }
}
