// The rerouted patrol walks (brief 057 §2, Sky 2026-10-24: "the robot follows the drawn path and moves smoothly"; engine 0.30.0; rulebook
// cell.reroute.walk). 0.29.0 placed it at the wait spot (patrol-reroute) the tick the Reroute landed and back on its sweep the tick the
// false orders ran out. Now, with cell.reroute.walk in the rulebook, the whole trip happens inside the false orders, so what a Reroute buys is
// unchanged: from where it stands when the Reroute lands it walks the walk grid at that pace to the wait spot and waits there facing away,
// then walks back at the same pace so as to stand on its sweep exactly where SweepPos has it at PatrolAwayUntil (a tick or so early, it
// waits there). While the false orders run it sees nothing, walking or waiting, as before; at PatrolAwayUntil it sweeps on (patrol.returns).
// When the walk out and back would not fit in the false orders (a start far from the wait spot), it turns back short of the wait spot, at
// the farthest point of its walk out from which the walk back still lands on time. Without cell.reroute.walk (older pinned inputs)
// nothing here runs and every run plays as 0.29.0.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        Path? rerouteOut, rerouteBack; // the walk out (from the landing), the walk back (from rerouteLeave to SweepPos(PatrolAwayUntil))
        int rerouteOutAt = -1, rerouteLeave = -1; long rerouteMmT, rerouteTurnMm; // landing tick, the tick it starts back, mm a tick, how far out it goes (mm)

        /// <summary>At the Reroute's landing (PatrolAwayUntil set): the walk out from `from` toward the wait spot and the walk back, planned once.</summary>
        void PlanRerouteWalk(Pt? from)
        {
            rerouteOut = rerouteBack = null; rerouteOutAt = rerouteLeave = -1; rerouteTurnMm = 0;
            if (R.RerouteWalkCmS <= 0 || !from.HasValue) return;
            long v = (long)R.RerouteWalkCmS * 10 / Hz; if (v <= 0) return; // cm/s → mm a tick
            int until = PatrolAwayUntil, span = until - Tick; var back = SweepPos(until);
            var outWalk = WalkPath(from.Value, ClearStop("patrol", S.Stand(S.PatrolReroute))); // brief 081 (engine 0.37.0; fight.spacing): its wait spot, clear of whoever stands there
            int Ticks(long mm) => (int)((mm + v - 1) / v);
            Path backWalk = WalkPath(outWalk.End, back); long turn = outWalk.Length;
            if (Ticks(turn) + Ticks(backWalk.Length) > span) // too far for the false orders: the farthest point out from which the walk back still lands on time
            {
                long lo = 0, hi = outWalk.Length; Path best = WalkPath(from.Value, back);
                while (hi - lo > v)
                {
                    long mid = (lo + hi) / 2; var bw = WalkPath(outWalk.At(mid), back);
                    if (Ticks(mid) + Ticks(bw.Length) <= span) { lo = mid; best = bw; } else hi = mid;
                }
                turn = lo; backWalk = lo == 0 ? WalkPath(from.Value, back) : best;
                if (Ticks(turn) + Ticks(backWalk.Length) > span) return; // not even straight back fits: placed, as 0.29.0
            }
            rerouteMmT = v; rerouteOutAt = Tick; rerouteOut = outWalk; rerouteTurnMm = turn; rerouteBack = backWalk;
            rerouteLeave = until - Ticks(backWalk.Length);
        }
        /// <summary>A's walk to b on the walk grid, starting at a and ending at b exactly (the straight line if the grid has no route).</summary>
        Path WalkPath(Pt a, Pt b)
        {
            var route = Walk.Route(Walk.NearestFree(a) ?? a, Walk.NearestFree(b) ?? b);
            var pts = new List<Pt> { a };
            if (route != null) foreach (var p in route) if (pts[pts.Count - 1] != p) pts.Add(p);
            if (pts[pts.Count - 1] != b) pts.Add(b);
            if (pts.Count == 1) pts.Add(b);
            return new Path(pts.ToArray());
        }
        /// <summary>While the false orders run: walking out, waiting at the wait spot (facing away), walking back, or on its sweep's point
        /// waiting for PatrolAwayUntil (facing as it sweeps). Null when no walk is planned (placed at the wait spot, 0.29.0).</summary>
        (Pt pos, Pt facing)? RerouteWalkPose()
        {
            if (rerouteOut == null || rerouteBack == null || rerouteOutAt < 0 || PatrolAwayUntil <= Tick) return null;
            if (Tick < rerouteLeave)
            {
                long mm = Math.Min((long)(Tick - rerouteOutAt) * rerouteMmT, rerouteTurnMm);
                if (mm >= rerouteOut.Length) return (rerouteOut.End, S.PatrolRerouteFacing);
                return (rerouteOut.At(mm), rerouteOut.Heading(Math.Min(mm, Math.Max(0, rerouteTurnMm - 1))));
            }
            long bm = (long)(Tick - rerouteLeave) * rerouteMmT;
            return bm >= rerouteBack.Length ? (rerouteBack.End, S.PatrolFacing) : (rerouteBack.At(bm), rerouteBack.Heading(bm));
        }
        /// <summary>The planned reroute walk (PatrolLegs): out, the turn (mm along it), the tick it starts back, the walk back; null when none.</summary>
        (Path @out, long turnMm, int outAt, int leave, Path back, long mmT)? RerouteWalkPlan()
            => rerouteOut != null && rerouteBack != null && rerouteOutAt >= 0 && PatrolAwayUntil > Tick ? (rerouteOut, rerouteTurnMm, rerouteOutAt, rerouteLeave, rerouteBack, rerouteMmT) : null;
    }
}
