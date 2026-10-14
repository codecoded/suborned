// Integer geometry in centimetres: points, exact integer square roots, polygons, sightlines past occluders, cones.
// Nothing here uses floating point, so the same inputs give the same answers on every machine and in Unity.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public readonly struct Pt : IEquatable<Pt>
    {
        public readonly int X, Y;
        public Pt(int x, int y) { X = x; Y = y; }
        public static Pt operator -(Pt a, Pt b) => new Pt(a.X - b.X, a.Y - b.Y);
        public static Pt operator +(Pt a, Pt b) => new Pt(a.X + b.X, a.Y + b.Y);
        public bool Equals(Pt o) => X == o.X && Y == o.Y;
        public override bool Equals(object? o) => o is Pt p && Equals(p);
        public override int GetHashCode() => unchecked(X * 73856093 ^ Y * 19349663);
        public static bool operator ==(Pt a, Pt b) => a.Equals(b);
        public static bool operator !=(Pt a, Pt b) => !a.Equals(b);
        public override string ToString() => "(" + X.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + Y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
        public static Pt Of(JNode n) { var a = n.Ints(); if (a.Length != 2) throw new FormatException($"{n.Path} is not a point"); return new Pt(a[0], a[1]); }
    }

    public static class G
    {
        /// <summary>floor(sqrt(n)) for n ≥ 0, exactly.</summary>
        public static long Isqrt(long n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (n < 2) return n;
            // Newton's method on integers from a power-of-two start above the root: decreases to floor(sqrt(n)) exactly.
            int bits = 0; for (long t = n; t > 0; t >>= 1) bits++;
            long x = 1L << ((bits + 1) / 2);
            while (true) { long y = (x + n / x) / 2; if (y >= x) return x; x = y; }
        }
        public static long D2(Pt a, Pt b) { long dx = a.X - b.X, dy = a.Y - b.Y; return dx * dx + dy * dy; }
        /// <summary>Distance in cm, rounded down.</summary>
        public static int Dist(Pt a, Pt b) => (int)Isqrt(D2(a, b));
        public static bool Within(Pt a, Pt b, int cm) => D2(a, b) <= (long)cm * cm;
        public static long Cross(Pt o, Pt a, Pt b) => (long)(a.X - o.X) * (b.Y - o.Y) - (long)(a.Y - o.Y) * (b.X - o.X);
        static int Sign(long v) => v > 0 ? 1 : v < 0 ? -1 : 0;

        /// <summary>Point a + (b − a)·num/den, truncated toward zero.</summary>
        public static Pt Lerp(Pt a, Pt b, long num, long den)
        {
            if (den <= 0) return b;
            return new Pt(a.X + (int)((long)(b.X - a.X) * num / den), a.Y + (int)((long)(b.Y - a.Y) * num / den));
        }

        /// <summary>Even-odd point in polygon (a point on an edge counts as inside).</summary>
        public static bool InPoly(Pt p, Pt[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                Pt a = poly[i], b = poly[j];
                if (OnSegment(p, a, b)) return true;
                if ((a.Y > p.Y) != (b.Y > p.Y))
                {
                    // x of the edge at p.Y compared with p.X, without division: sign of the cross product, oriented by edge direction
                    long c = Cross(a, b, p);
                    if ((b.Y > a.Y) ? c < 0 : c > 0) inside = !inside;
                }
            }
            return inside;
        }

        static bool OnSegment(Pt p, Pt a, Pt b) =>
            Cross(a, b, p) == 0 && Math.Min(a.X, b.X) <= p.X && p.X <= Math.Max(a.X, b.X) && Math.Min(a.Y, b.Y) <= p.Y && p.Y <= Math.Max(a.Y, b.Y);

        /// <summary>True if segments ab and cd touch or cross.</summary>
        public static bool SegHit(Pt a, Pt b, Pt c, Pt d)
        {
            int d1 = Sign(Cross(c, d, a)), d2 = Sign(Cross(c, d, b)), d3 = Sign(Cross(a, b, c)), d4 = Sign(Cross(a, b, d));
            if (d1 * d2 < 0 && d3 * d4 < 0) return true;
            return (d1 == 0 && OnSegment(a, c, d)) || (d2 == 0 && OnSegment(b, c, d)) || (d3 == 0 && OnSegment(c, a, b)) || (d4 == 0 && OnSegment(d, a, b));
        }

        /// <summary>True if segment ab enters polygon poly (crosses or touches an edge, or starts inside).</summary>
        public static bool SegHitsPoly(Pt a, Pt b, Pt[] poly)
        {
            if (InPoly(a, poly) || InPoly(b, poly)) return true;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++) if (SegHit(a, b, poly[j], poly[i])) return true;
            return false;
        }

        /// <summary>
        /// A watcher at o facing f sees p if p is within `near`, or ahead of it within `reach` and to the side within half × ahead ÷ reach.
        /// Integer form of the top-down test's insideFacingCone: ahead·|f| = r·f, side·|f| = |r × f|.
        /// </summary>
        public static bool InCone(Pt o, Pt f, int reach, int half, int near, Pt p)
        {
            if (Within(o, p, near)) return true;
            long rx = p.X - o.X, ry = p.Y - o.Y;
            long flen = Isqrt((long)f.X * f.X + (long)f.Y * f.Y); if (flen == 0) return false;
            long fx = (long)f.X * 1024 / flen, fy = (long)f.Y * 1024 / flen; // a facing of length 1,024: products stay far inside a long (Redline E5)
            long dot = rx * fx + ry * fy;
            if (dot < 0) return false;
            long ff = fx * fx + fy * fy; if (ff == 0) return false;
            // ahead ≤ reach  ⇔  dot² ≤ reach²·|f|²
            if (dot * dot > (long)reach * reach * ff) return false;
            long cross = Math.Abs(rx * fy - ry * fx);
            // side ≤ ahead·half/reach  ⇔  |cross|·reach ≤ dot·half
            return cross * reach <= dot * half;
        }
    }

    /// <summary>Something that blocks a sightline: a footprint standing from z to z + h (cm).</summary>
    public sealed class Occluder
    {
        public readonly string Id; public readonly int Z, H; public readonly Pt[] Poly; readonly int x0, x1, y0, y1;
        public Occluder(string id, int z, int h, Pt[] poly)
        {
            Id = id; Z = z; H = h; Poly = poly;
            x0 = int.MaxValue; x1 = int.MinValue; y0 = int.MaxValue; y1 = int.MinValue;
            foreach (var p in poly) { x0 = Math.Min(x0, p.X); x1 = Math.Max(x1, p.X); y0 = Math.Min(y0, p.Y); y1 = Math.Max(y1, p.Y); }
        }
        public bool Blocks(Pt a, Pt b, int eye)
        {
            if (eye < Z || eye > Z + H) return false;
            if (Math.Max(a.X, b.X) < x0 || Math.Min(a.X, b.X) > x1 || Math.Max(a.Y, b.Y) < y0 || Math.Min(a.Y, b.Y) > y1) return false;
            return G.SegHitsPoly(a, b, Poly);
        }
        public bool Contains(Pt p) => p.X >= x0 && p.X <= x1 && p.Y >= y0 && p.Y <= y1 && G.InPoly(p, Poly);
    }
}
