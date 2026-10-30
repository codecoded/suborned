// Engine 0.36.0 (rulebook fight.spacing; brief 079, Sky on the recorded fight: "Rivals were overlapping each other"). Nobody in a fight stands
// in anybody else. Two bodies may pass (a walk crosses a walk); where a body's walk ends, or where it stops, is a place it claims:
//  · a body's claim is where its walk ends, or where it stands with none: a final-fight body, a member of the rival crew on foot, the response
//    unit, a police unit, an operative on the street (the operatives' own moves stay the player's: nothing here moves them);
//  · a walk that would end within fight.spacing.clear of another's claim ends at the free cell nearest it that is clear of them all, by the
//    walk grid's search, and never in the car's hold or lane unless the place itself was (ClearStop). No claim near: the place is unchanged,
//    so a fight nobody crowds plays as before;
//  · a body that stops mid-walk on another's claim takes the step to such a cell (Stop), firing as it walks;
//  · bodies sent in together from one entry come in file, fight.spacing.file apart (GameFight), or the two would be one figure all the way in.
// Stateless: a claim is read from the walks and stands the game already hashes, so the same orders give the same places. A host read never
// calls it. Walks to a car's door (a rival making for the unguarded car, a member boarding) end at the car, where they leave the street.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>Where every other body on the street has claimed: where its walk ends, or where it stands.</summary>
        List<Pt> BodyClaims(string who)
        {
            var l = new List<Pt>();
            foreach (var op in Ops) // brief 081 (engine 0.37.0): the rule always named them; a walk now ends clear of an operative, or of where he is bound
            {
                if (Sim.Op.Name(op.Id) == who || op.Inside || op.InCar || op.CarriedBy.HasValue) continue;
                l.Add(op.Walk != null ? op.Walk.End : op.Pos);
            }
            if (who != "guard" && !GuardDown && GuardPose() is var gd && gd.HasValue) l.Add(GuardAlly && guardAllyPath != null ? guardAllyPath.End : gd.Value.pos); // brief 081: the guard where he stands or is bound
            if (who != "patrol" && !PatrolDown && (PatrolChasing || PatrolAwayUntil > Tick) && PatrolUnit() == null) { if (rerouteOut != null) l.Add(rerouteOut.End); else if (PatrolPose() is var pd && pd.HasValue) l.Add(pd.Value.pos); } // the patrol at rest off its sweep; on its sweep it passes, as the crowd does
            foreach (var who2 in new[] { Inez, Driver }) if (who2.Id != who && who2.Role != who && !who2.Down && PersonPos(who2) is Pt cp && !(who2 == Driver && valeDriveStart >= 0)) l.Add(cp); // the cast at their places; a driver at the wheel is off the street
            foreach (var b in Bodies) if (b.Open && !b.Carrier.HasValue) l.Add(BodyPos(b)); // brief 081: and a body where it lies (the patrol stood 9.8 s on the knocked-out guard in route G)
            foreach (var h in Hostiles) if (h.Id != who && h.Present && Up(h)) l.Add(h.At);
            foreach (var m in Crew)
            {
                if (m.Id == who || m.Fight || !Up(m) || !m.OnFoot) continue;
                if (m.Disp != null) l.Add(m.Disp.Phase == 2 ? m.Disp.P.At(0) : m.Disp.P.End);
                else l.Add(m.Mode == CrewMode.Walking && m.Walk != null ? m.Walk.End : m.Place);
            }
            for (int k = 0; k < SecGuns.Length; k++)
            {
                if (secJoined[k] || SecGuns[k].Id == who) continue; // a joined officer is a final-fight body
                if (secDisp.TryGetValue(k, out var d)) { if (!SecGuns[k].Down) l.Add(d.P.End); }
                else if (SecOfficerPose(k) is var p && p.HasValue) l.Add(p.Value.pos);
            }
            foreach (var u in Units)
            {
                if (u.Id == who || u.Legacy || !u.OnStreet(Tick) || u.Gun.Down) continue;
                if (unitDisp.TryGetValue(u.Id, out var d)) l.Add(d.P.End);
                else if (u.Route != null && u.St == "walk") l.Add(u.Route.End);
                else if (UnitPose(u) is var p && p.HasValue) l.Add(p.Value.pos);
            }
            return l;
        }

        /// <summary>The place a body's walk should end: `to`, unless that stands within fight.spacing.clear of another's claim; then the free cell
        /// nearest `to` clear of them all (and out of the car's hold and lane, if `to` was), or `to` when none lies within six clearances.</summary>
        internal Pt ClearStop(string who, Pt to)
        {
            int clear = R.BodyClearCm; if (clear <= 0) return to;
            var claims = BodyClaims(who); long c2 = (long)clear * clear;
            bool Clear(Pt q) { foreach (var p in claims) if (G.D2(q, p) < c2) return false; return true; }
            if (Clear(to)) return to;
            bool hold = HoldsCar(to), atCar = !Range && AtGetaway(to); // Motive's ruling (brief 080): a place within Board's reach stays within it: a spaced end never makes Board refuse for good
            return Walk.Nearest(to, q => Clear(q) && (hold || !HoldsCar(q)) && (!atCar || AtGetaway(q)), clear * 6) ?? to;
        }
    }
}
