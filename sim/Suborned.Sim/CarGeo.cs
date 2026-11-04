// A car's footprint on the ground and the integer tests the getaway rules of brief 096 make with it (engine 0.40.0, Keel; Motive's rules page):
// box-in (rulebook car.box: does a standing car's footprint meet another car's way out, a polyline swept at a half-width?) and the kerbside stop
// (car.kerb: does a footprint, grown, meet a drive's polygon or a way out?). Integers only (cm, products in long), so every run makes the same call.
using System;

namespace Suborned.Sim
{
    public static class CarGeo
    {
        /// <summary>The corners of a footprint centred at c, heading h, half-length hl along h and half-width hw across it (cm), in order round it.</summary>
        public static Pt[] Rect(Pt c, Pt h, long hl, long hw)
        {
            if (h.X == 0 && h.Y == 0) h = new Pt(0, 100);
            long n = Math.Max(1, G.Isqrt((long)h.X * h.X + (long)h.Y * h.Y));
            long ax = h.X * hl / n, ay = h.Y * hl / n, px = -h.Y * hw / n, py = h.X * hw / n;
            return new[] { P(c.X + ax + px, c.Y + ay + py), P(c.X + ax - px, c.Y + ay - py), P(c.X - ax - px, c.Y - ay - py), P(c.X - ax + px, c.Y - ay + py) };
        }
        static Pt P(long x, long y) => new Pt((int)x, (int)y);
        static long Cross(Pt o, Pt a, Pt b) => (long)(a.X - o.X) * (b.Y - o.Y) - (long)(a.Y - o.Y) * (b.X - o.X);
        static bool OnSeg(Pt a, Pt b, Pt p) => Math.Min(a.X, b.X) <= p.X && p.X <= Math.Max(a.X, b.X) && Math.Min(a.Y, b.Y) <= p.Y && p.Y <= Math.Max(a.Y, b.Y);
        /// <summary>Segments ab and cd meet (touching counts).</summary>
        public static bool SegsMeet(Pt a, Pt b, Pt c, Pt d)
        {
            long d1 = Cross(c, d, a), d2 = Cross(c, d, b), d3 = Cross(a, b, c), d4 = Cross(a, b, d);
            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0))) return true;
            return (d1 == 0 && OnSeg(c, d, a)) || (d2 == 0 && OnSeg(c, d, b)) || (d3 == 0 && OnSeg(a, b, c)) || (d4 == 0 && OnSeg(a, b, d));
        }
        /// <summary>p inside the polygon or on its edge (crossing number; integers).</summary>
        public static bool Inside(Pt p, Pt[] poly)
        {
            bool c = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                Pt a = poly[i], b = poly[j];
                if (Cross(a, b, p) == 0 && OnSeg(a, b, p)) return true;
                if ((a.Y > p.Y) != (b.Y > p.Y))
                {
                    long lhs = (long)(p.X - a.X) * (b.Y - a.Y), rhs = (long)(b.X - a.X) * (p.Y - a.Y);
                    if (b.Y - a.Y > 0 ? lhs < rhs : lhs > rhs) c = !c;
                }
            }
            return c;
        }
        /// <summary>Two polygons meet: a corner of one inside the other, or two edges meet.</summary>
        public static bool PolysMeet(Pt[] a, Pt[] b)
        {
            foreach (var p in a) if (Inside(p, b)) return true;
            foreach (var p in b) if (Inside(p, a)) return true;
            for (int i = 0; i < a.Length; i++) for (int j = 0; j < b.Length; j++) if (SegsMeet(a[i], a[(i + 1) % a.Length], b[j], b[(j + 1) % b.Length])) return true;
            return false;
        }
        /// <summary>p within `half` cm of segment ab.</summary>
        public static bool NearSeg(Pt p, Pt a, Pt b, long half)
        {
            long abx = b.X - a.X, aby = b.Y - a.Y, apx = p.X - a.X, apy = p.Y - a.Y, ab2 = abx * abx + aby * aby, dot = apx * abx + apy * aby;
            if (ab2 == 0 || dot <= 0) return G.D2(p, a) <= half * half;
            if (dot >= ab2) return G.D2(p, b) <= half * half;
            long cross = apx * aby - apy * abx;
            return cross * cross <= half * half * ab2;
        }
        static bool SegNearSeg(Pt a, Pt b, Pt c, Pt d, long half) => SegsMeet(a, b, c, d) || NearSeg(a, c, d, half) || NearSeg(b, c, d, half) || NearSeg(c, a, b, half) || NearSeg(d, a, b, half);
        /// <summary>The polygon (a footprint) meets the ground within `half` cm of the polyline (a way out).</summary>
        public static bool OnWay(Pt[] poly, Pt[] way, long half)
        {
            if (way.Length == 0) return false;
            foreach (var w in way) if (Inside(w, poly)) return true;
            if (way.Length == 1) { foreach (var c in poly) if (G.Within(c, way[0], (int)half)) return true; for (int k = 0; k < poly.Length; k++) if (NearSeg(way[0], poly[k], poly[(k + 1) % poly.Length], half)) return true; return false; }
            for (int i = 1; i < way.Length; i++)
                for (int k = 0; k < poly.Length; k++)
                    if (SegNearSeg(way[i - 1], way[i], poly[k], poly[(k + 1) % poly.Length], half)) return true;
            return false;
        }
        /// <summary>The first `mm` of a path then `aheadMm` on along it: a way out as a polyline (the path's points to the cut, and the cut).</summary>
        public static Pt[] Upto(Path p, long mm) => p.Cut(Math.Max(1, mm)).Points;
        /// <summary>The point `cm` straight on from `at` along heading h.</summary>
        public static Pt Ahead(Pt at, Pt h, long cm)
        {
            if (h.X == 0 && h.Y == 0) h = new Pt(0, 100);
            long n = Math.Max(1, G.Isqrt((long)h.X * h.X + (long)h.Y * h.Y));
            return P(at.X + h.X * cm / n, at.Y + h.Y * cm / n);
        }
    }
}
