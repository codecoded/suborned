// Cover as data (brief 011 fight AI, Pulse: briefs/011/pulse-fight-ai.md §5; contracts/world/civic-block.cover.json, copied into the site
// by tools/sim/site.ts with its sha256). The three functions are tools/cover/select.ts in integers, and give its conformance vectors.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed class CoverPoint
    {
        public string Id = ""; public Pt At, Faces; public int Cos, Q, H; public string Kind = "", Of = ""; public int From = 0, To = int.MaxValue; // present [From, To) mission seconds
        public bool Present(int missionS) => missionS >= From && missionS < To;
    }

    public static class CoverRules
    {
        /// <summary>The point shields a body standing at it from a shooter at s: at least minRange away (point blank is never turned), inside the
        /// cone about `faces` (dot(s − at, faces) ≥ cos × |s − at|, cm·per mille), and present at the mission second.</summary>
        public static bool Protects(CoverPoint c, Pt s, int missionS, int minRangeCm)
        {
            if (!c.Present(missionS)) return false;
            long vx = s.X - c.At.X, vy = s.Y - c.At.Y, d = G.Isqrt(vx * vx + vy * vy);
            if (d < minRangeCm) return false;
            return vx * c.Faces.X + vy * c.Faces.Y >= c.Cos * d;
        }

        /// <summary>The best hit modifier (per mille) a body at p, standing still, gets against a shooter: full, partial, or 1000 (none).</summary>
        public static int Modifier(IReadOnlyList<CoverPoint> points, Pt p, Pt shooter, int missionS, int snapCm, int minRangeCm, int qFull, int qPartial, int modFull, int modPartial)
        {
            int best = 1000; long snap2 = (long)snapCm * snapCm;
            foreach (var c in points)
            {
                if (G.D2(p, c.At) > snap2 || !Protects(c, shooter, missionS, minRangeCm)) continue;
                int m = c.Q >= qFull ? modFull : c.Q >= qPartial ? modPartial : 1000;
                if (m < best) best = m;
            }
            return best;
        }

        /// <summary>The cover point a body runs to, or null. Candidates: not taken, present, at least partial, within seek. With a foe: it shields
        /// against the foe and brings the body no more than stepCloser nearer the foe; without one: no more than retreat farther from the objective.
        /// Minimises distance − quality, then the lower id.</summary>
        public static CoverPoint? Select(IReadOnlyList<CoverPoint> points, Pt me, Pt objective, Pt? foe, ICollection<string> taken, int seekCm, int missionS,
            int minRangeCm, int qPartial, int retreatCm, int stepCloserCm)
        {
            long here = Dist(me, objective);
            CoverPoint? best = null; long bestKey = long.MaxValue;
            foreach (var c in points)
            {
                if (taken.Contains(c.Id) || c.Q < qPartial || !c.Present(missionS)) continue;
                long d = Dist(me, c.At);
                if (d > seekCm) continue;
                if (!foe.HasValue && Dist(c.At, objective) > here + retreatCm) continue;
                if (foe.HasValue && (!Protects(c, foe.Value, missionS, minRangeCm) || Dist(c.At, foe.Value) < Dist(me, foe.Value) - stepCloserCm)) continue;
                long key = d - c.Q;
                if (key < bestKey || (key == bestKey && best != null && string.CompareOrdinal(c.Id, best.Id) < 0)) { best = c; bestKey = key; }
            }
            return best;
        }
        static long Dist(Pt a, Pt b) => G.Isqrt(G.D2(a, b));
    }
}
