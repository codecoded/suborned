// Engine 0.34.0 (brief 067; Motive briefs/067/motive-square.md, motive-values.json M067-SQ1 and M067-SQ2, on the square without its planters;
// Pulse's motion, Keel lands it). Measured on Motive's probe as PROBE_SQ=7 (his bits 1, 2 and 4).
//  SQ1 · the hold is entered only on purpose: in the final fight a hostile's answer to fire never ends inside the getaway car's hold
//        (fight.hold of the car) or on its exit lane (HoldsCar): a rival's step out of a shooter's line (the shielding cover, else the nearest
//        free spot within 8 m with no line to him), a rival's step to cover while engaged, a police body's cover or close. If no spot outside
//        serves, it stays where it stands. A walk to a post goes round the hold (the walk grid with the hold's ground closed: the rival crew to
//        its spots, police and the response unit to their stands, MoveToFight) unless the post or the walker is already inside it. Exceptions: the blocker (his lane is the road) and the rivals walking to a held car
//        with nobody outside (M067-K3 a). An advance on the car or on an operative may enter the hold: that is coming for the car, and it holds.
//  SQ2 · the safe fight can't stall: a rival holding with no foe in his effective reach for ai.engageLapse advances again, on the nearest
//        operative standing on the street he has cause to fire on (the Vale holder counts): to the cover that shields him from that operative,
//        else to his stop point of that operative (ai.stopReach of his effective reach, the first point of the walk to him that has the line:
//        StopPointSeen), not of the car's point. Police and the blocker hold. With nowhere to go he holds and says nothing.
//        Engine 0.34.1 (Aster's ruling within SQ2, brief 067): an advance closes on that operative. A cover is taken only if it is strictly
//        nearer him by walk than where the rival stands, and inside the rival's effective reach of him or nearer him than he stands
//        (ClosingCover); else the stop point; else he holds. And the advance is not turned back: on its walk he takes no kite step out of
//        a line (Hostile.Closing, KiteTick), as on K3's walk to a held car: he walks on until a foe is in reach (then stops and engages, as
//        any advance) or he is there. On 0.34.0 each advance left out the cover he
//        held, so in ra-h3 rival-3 went cv-030 → cv-029 → cv-030 every 1.8 s (4:13.9–4:21.1), never nearer the Tech: a rival shuffling in
//        the open fight Sky reads; with the cover rule alone the kite step turned each walk out back to cv-029 (every 2.2–2.5 s).
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
        /// fire on: to cover that shields him from that operative and closes on him (ClosingCover), else his stop point of him. Nothing when
        /// there is no such operative, or nowhere that closes.</summary>
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
            var c = ClosingCover(pos, best.Pos, g.W!.EffCm); // engine 0.34.1 (Aster's ruling within SQ2): the advance closes on him
            var to = c?.At ?? StopPointSeen(pos, best.Pos, g.W!.EffCm);
            if (to == pos) return; // nowhere to go (no way to him): he holds, and looks again in ai.engageLapse, saying nothing
            Note("ai.advance", $"{h.Id} has had nobody in reach for {R.EngageLapseS} s: he advances on the {Sim.Op.Name(best.Id)}{(c != null ? " to " + c.Id : "")}.");
            g.St = "advance";
            if (c != null) TakeCover(g, c);
            MoveTo(h, to); h.Why = "advance"; h.Closing = h.Run != null; // engine 0.34.1: no kite step turns it back (KiteTick)
        }
        const long NoWalkMm = long.MaxValue / 4; // WalkMm's answer when no walk joins the two
        /// <summary>Engine 0.34.1 (Aster's ruling within SQ2): the cover an advance on an operative at `op` takes. Of the points SelectCover
        /// would give (not taken, at least partial, within ai.seek.rival, shielding him from the operative, at most cover.stepCloser nearer him
        /// in a straight line), in its order (distance − quality, then the lower id), the first that closes: its walk to the operative strictly
        /// shorter than the walk from where the rival stands (a point where he stands, or on the same walk, never closes), and itself within his
        /// effective reach of the operative or nearer the operative than he stands. Null for none, or when no walk reaches the operative.</summary>
        internal CoverPoint? ClosingCover(Pt pos, Pt op, int effCm)
        {
            if (LiveCover.Count == 0) return null;
            long here = WalkMm(pos, op); if (here >= NoWalkMm) return null; // the walk as a body walks it (GameCrew's WalkMm: MoveTo's ends, round the cars)
            long d0 = G.Dist(pos, op); var skip = Taken();
            for (;;)
            {
                var c = CoverRules.Select(LiveCover, pos, Car, op, skip, R.SeekRivalCm, Tick / Hz, R.PointBlankCm, R.CoverQPartial, R.CoverRetreatCm, R.CoverStepCloserCm);
                if (c == null) return null;
                long d = G.Dist(c.At, op), w = WalkMm(c.At, op);
                if (w < here && (d <= effCm || d < d0)) return c; // no walk from it (NoWalkMm) never closes
                skip.Add(c.Id); // the next in SelectCover's order
            }
        }
        /// <summary>SQ2: where an advance on an operative halts: the first point of the walk to him within ai.stopReach of the effective reach
        /// from which he sees him (a rival inside that ring with a wall between them walks on until he has the line; Motive's sketch stopped
        /// where it stood and asked again every second: N143's rival-1, 12 m from the Enforcer and blind to him), else the walk's end.</summary>
        Pt StopPointSeen(Pt from, Pt target, int effCm)
        {
            long stop = (long)effCm * R.StopReachPerMille / 1000, s2 = stop * stop;
            if (G.D2(from, target) <= s2 && Sees(from, target)) return from;
            var a = Walk.NearestFree(from) ?? from; var b = Walk.NearestFree(target) ?? target;
            var route = Walk.Route(a, b); if (route == null) return from;
            var p = new Path(route);
            for (long mm = 0; mm <= p.Length; mm += 500) { var q = p.At(mm); if (G.D2(q, target) <= s2 && Sees(q, target)) return q; } // every 50 cm (Path is in mm), as StopPoint
            return p.End;
        }
    }
}
