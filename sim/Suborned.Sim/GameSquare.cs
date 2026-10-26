// Engine 0.34.0 (brief 067; Motive briefs/067/motive-square.md, motive-values.json M067-SQ1 and M067-SQ2, on the square without its planters;
// Pulse's motion, Keel lands it). Measured on Motive's probe as PROBE_SQ=7 (his bits 1, 2 and 4).
//  SQ1 · the hold is entered only on purpose: in the final fight a hostile's answer to fire never ends inside the getaway car's hold
//        (fight.hold of the car) or on its exit lane (HoldsCar): a rival's step out of a shooter's line (the shielding cover, else the nearest
//        free spot within 8 m with no line to him), a rival's step to cover while engaged, a police body's cover or close. If no spot outside
//        serves, it stays where it stands. A crew member's walk to his post goes round the hold (the walk grid with the hold's ground closed)
//        unless the post or the walker is already inside it. Exceptions: the blocker (his lane is the road) and the rivals walking to a held car
//        with nobody outside (M067-K3 a). An advance on the car or on an operative may enter the hold: that is coming for the car, and it holds.
//  SQ2 · the safe fight can't stall: a rival holding with no foe in his effective reach for ai.engageLapse advances again, on the nearest
//        operative standing on the street he has cause to fire on (the Vale holder counts): to the cover that shields him from that operative,
//        else to his stop point of that operative (ai.stopReach of his effective reach), not of the car's point. Police and the blocker hold.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>SQ1: the cover points a hostile answering fire may not end on: taken, or inside the car's hold or on its exit lane.</summary>
        HashSet<string> TakenOrHold() { var t = Taken(); foreach (var c in LiveCover) if (HoldsCar(c.At)) t.Add(c.Id); return t; }
        /// <summary>SQ1: SelectCover with the hold and the lane left out.</summary>
        CoverPoint? SelectCoverOut(Pt me, Pt objective, Pt? foe, int seekCm) =>
            LiveCover.Count == 0 ? null : CoverRules.Select(LiveCover, me, objective, foe, TakenOrHold(), seekCm, Tick / Hz, R.PointBlankCm, R.CoverQPartial, R.CoverRetreatCm, R.CoverStepCloserCm);
        /// <summary>SQ1: KiteHide with the hold and the lane left out (the shielding cover, else the nearest blind point within 8 m).</summary>
        Pt? KiteHideOut(Gun g, Pt pos, Pt sp, Pt objective, out CoverPoint? c)
        {
            c = SelectCoverOut(pos, objective, sp, R.SeekRivalCm);
            if (c != null) return c.At;
            return Walk.Nearest(pos, q => !Sees(q, sp) && !HoldsCar(q), KiteBlindCm);
        }
        /// <summary>SQ1: the walk from a to b round the car's hold (the walk grid with the cells within fight.hold of the car closed), or null for
        /// the plain walk: when a or b lies inside the hold, outside a fight's stand, or there is no way round.</summary>
        Pt[]? RouteRoundHold(Pt a, Pt b)
        {
            if (Range || S.Stands.Count == 0 || G.Within(a, Car, R.HoldCm) || G.Within(b, Car, R.HoldCm)) return null;
            var w = Walk; var g = w.Grid; int n = g.Nx * g.Ny; var m = new bool[n]; if (w.Mask != null) Array.Copy(w.Mask, m, n);
            var car = Car; int r = R.HoldCm; int i0 = Math.Max(0, g.CI(car.X - r)), i1 = Math.Min(g.Nx - 1, g.CI(car.X + r)), j0 = Math.Max(0, g.CJ(car.Y - r)), j1 = Math.Min(g.Ny - 1, g.CJ(car.Y + r));
            for (int i = i0; i <= i1; i++) for (int j = j0; j <= j1; j++) if (G.Within(g.Centre(i, j), car, r)) m[i * g.Ny + j] = true;
            var route = g.Route(a, b, m);
            return route != null && route.Length >= 2 ? route : null;
        }
        /// <summary>SQ2: a rival with no foe in reach for ai.engageLapse advances on the nearest operative standing on the street he has cause to
        /// fire on (cover that shields him from that operative, else his stop point of him). Nothing when there is no such operative.</summary>
        void AdvanceOnOp(Hostile h, Pt pos)
        {
            var g = h.Gun; Op? best = null; long bd = long.MaxValue;
            foreach (var o in Ops)
            {
                if (o.Down || o.Inside || o.InCar) continue;
                if (!(R.Intent.FinalEvery || Provoked(h.Id, o) || (o.Id == OpId.Enforcer && Vale == ValeAt.WithEnforcer))) continue; // only one he has cause to fire on
                long d = G.D2(pos, o.Pos); if (d < bd) { bd = d; best = o; }
            }
            if (best == null) return;
            var c = SelectCover(pos, Car, best.Pos, R.SeekRivalCm);
            Note("ai.advance", $"{h.Id} has had nobody in reach for {R.EngageLapseS} s: he advances on the {Sim.Op.Name(best.Id)}{(c != null && c.At != pos ? " to " + c.Id : "")}.");
            g.St = "advance";
            if (c != null && c.At != pos) { TakeCover(g, c); MoveTo(h, c.At); return; }
            MoveTo(h, StopPoint(pos, best.Pos, g.W!.EffCm));
        }
    }
}
