// Brief 098 L9 (engine 0.50.0; Aster's ruling on the 0.49.0 report): a doorway is a way (rulebook bodies.door). Every entrance of the site has
// its way: the ground in front of its door, out to bodies.door.depth along the open side of the foot ground round it (OpenSide), and
// bodies.door.half either side of the door's line. A guard at a door takes a post beside it, never in front of the doorway, so people pass
// between: the dial's door beat point (Population.SitePoints) is the nearest point to the door, on its open side, a body's radius outside its
// way, that leaves a passage (two personal spaces, centre to centre: a person walks between at personal space from both) from the places the door's own people hold (the cast
// stands, the door's waiting points, its going-in point);
// no beat point stands in a door's way; and a guard at a post takes no place inside a door's way (GameBodyForms.cs: no crew spot when
// he shares it, no step aside when someone else stands on it, no ring round it, no lay held or spot kept there, the nearest room off it). The door's own people keep their places, and anyone walks through. Worked out once per site; integer cm, a fixed order.
// Why: engine 0.49.0 stood the dial's door beat 2 m straight out from the Office's public door, on its steps, and the guards sharing it spread on
// their 2.5 m crew ring across them: at x4, holds of 5 s or more at the public door went from 21 to 75.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Suborned.Sim
{
    /// <summary>A door's way: the door's point, the way out (a unit vector, ×100) and along the facade (Out turned 90°, ×100).</summary>
    public sealed class DoorWay { public string Id = ""; public Pt At, Out, Side; }

    public static class DoorWays
    {
        const int OpenCm = 200, OpenStepCm = 10, StepCm = 10; // a door's way out, read from the foot ground within OpenCm of it every OpenStepCm; candidates beside it every StepCm
        /// <summary>Foot ground: walkable and no car's (a carriageway, a drive, a bay: a car may stand there).</summary>
        public static bool Foot(Site s, Pt p) => s.Grid.Free(p) && !s.Drive.Free(p);
        /// <summary>Engine 0.49.0: the way out of a door, the sum of the offsets of the foot ground within OpenCm of it (null: none free), ×100.</summary>
        public static Pt? OpenSide(Site s, Pt at)
        {
            long sx = 0, sy = 0;
            for (int dx = -OpenCm; dx <= OpenCm; dx += OpenStepCm) for (int dy = -OpenCm; dy <= OpenCm; dy += OpenStepCm)
                    if ((long)dx * dx + (long)dy * dy <= (long)OpenCm * OpenCm && Foot(s, new Pt(at.X + dx, at.Y + dy))) { sx += dx; sy += dy; }
            long n = G.Isqrt(sx * sx + sy * sy); return n == 0 ? null : new Pt((int)(sx * 100 / n), (int)(sy * 100 / n));
        }

        static readonly ConditionalWeakTable<Site, List<DoorWay>> cache = new ConditionalWeakTable<Site, List<DoorWay>>();
        /// <summary>The site's door ways, one per entrance with foot ground round it, in the site's order (computed once per site; read-only after).</summary>
        public static List<DoorWay> Of(Site s)
        {
            lock (cache)
            {
                if (cache.TryGetValue(s, out var l)) return l;
                l = new List<DoorWay>();
                foreach (var e in s.Entrances)
                {
                    var o = OpenSide(s, e.at); if (!o.HasValue) continue;
                    l.Add(new DoorWay { Id = e.id, At = e.at, Out = o.Value, Side = new Pt(-o.Value.Y, o.Value.X) });
                }
                cache.Add(s, l); return l;
            }
        }

        /// <summary>p stands in a door's way grown by `growCm` (a body's radius: its disc would reach into the way).</summary>
        public static bool In(IReadOnlyList<DoorWay> ways, Pt p, int depthCm, int halfCm, int growCm)
        {
            foreach (var w in ways)
            {
                long dx = p.X - w.At.X, dy = p.Y - w.At.Y, along = (dx * w.Out.X + dy * w.Out.Y) / 100, across = Math.Abs(dx * w.Side.X + dy * w.Side.Y) / 100;
                if (along >= -growCm && along <= depthCm + growCm && across < halfCm + growCm) return true;
            }
            return false;
        }

        /// <summary>A post beside door w: of the points on its open side (0 to `depthCm` out) beside its way (a body's radius past `halfCm`, out to
        /// `depthCm` more), every StepCm, the nearest to the door on ground `ok` accepts that stands outside every door's way by a radius and keeps
        /// `clearCm` from every point in `held` (ties: the first in a fixed order, the side +Side first). Null for none.</summary>
        public static Pt? Beside(DoorWay w, IReadOnlyList<DoorWay> ways, IReadOnlyList<Pt> held, int depthCm, int halfCm, int radiusCm, int clearCm, Func<Pt, bool> ok)
        {
            Pt? best = null; long bd = long.MaxValue;
            for (int a = 0; a <= depthCm; a += StepCm)
                for (int c = halfCm + radiusCm; c <= halfCm + radiusCm + depthCm; c += StepCm)
                    for (int sgn = 1; sgn >= -1; sgn -= 2)
                    {
                        var p = new Pt(w.At.X + (w.Out.X * a + w.Side.X * c * sgn) / 100, w.At.Y + (w.Out.Y * a + w.Side.Y * c * sgn) / 100);
                        long d = G.D2(p, w.At); if (d >= bd || !ok(p) || In(ways, p, depthCm, halfCm, radiusCm)) continue;
                        bool clear = true; foreach (var h in held) if (G.Within(p, h, clearCm - 1)) { clear = false; break; }
                        if (clear) { best = p; bd = d; }
                    }
            return best;
        }
    }
}
