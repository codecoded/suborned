#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Pure plan-frame geometry; opt-in only, no editor callback or scene writes.
    /// Build ONE track at a time against its ordered route (not a bag of intersecting routes).
    /// Positions/tangents and actual exported step are authoritative. IDs are diagnostic only.
    /// All calculations are double precision; convert to Unity floats only at the consumer boundary.</summary>
    internal static class SwapTramJoin
    {
        public readonly struct P
        {
            public readonly double X, Y;
            public P(double x, double y) { X = x; Y = y; }
            public static P operator +(P a, P b) => new P(a.X + b.X, a.Y + b.Y);
            public static P operator -(P a, P b) => new P(a.X - b.X, a.Y - b.Y);
            public static P operator *(P a, double k) => new P(a.X * k, a.Y * k);
            public double Length => Math.Sqrt(Dot(this, this));
        }
        public sealed class Tile
        {
            public readonly string Id;
            public readonly P Centre, Tangent;
            public readonly double Step;
            public Tile(string id, P centre, P tangent, double step)
            { Id = id; Centre = centre; Tangent = tangent; Step = step; }
        }
        public sealed class Seam
        {
            public readonly P Right, Left;
            internal Seam(P right, P left) { Right = right; Left = left; }
            public P At(double lateral, double halfWidth) => Right + (Left - Right) * ((lateral / halfWidth + 1) * .5);
        }
        public sealed class Footprint
        {
            public readonly Tile Source;
            public readonly P Direction, Normal;
            public readonly Seam Start, End;
            public readonly double HalfWidth;
            internal Footprint(Tile tile, P direction, Seam start, Seam end, double width)
            { Source = tile; Direction = direction; Normal = Left(direction); Start = start; End = end; HalfWidth = width / 2; }
            // CCW. Consumers must use this same polygon for bed ownership, clipping and audit.
            public P[] Polygon => new[] { Start.Right, End.Right, End.Left, Start.Left };
            public bool Contains(P q, double epsilon = 1e-8)
            {
                var p = Polygon;
                for (int i = 0; i < 4; i++) if (Cross(p[(i + 1) % 4] - p[i], q - p[i]) < -epsilon) return false;
                return true;
            }
            /// <summary>Map original tile along coordinate [-1,1] to its shared seams.
            /// Lateral is metres, NEVER scaled; y/height must be copied verbatim by the mesh consumer.
            /// Piecewise along mapping fixes every cross-section at along=0 (including both rail heads).
            /// Subdivide triangles at along=0 before applying; preserve submeshes/UV policy separately.
            /// This is an affine tangent strip, not a claim of smooth curved-rail continuity.</summary>
            public P Map(double along, double lateral)
            {
                if (!Finite(along) || !Finite(lateral) || Math.Abs(along) > 1 || Math.Abs(lateral) > HalfWidth)
                    throw new ArgumentException("vertex outside source tile domain");
                P mid = Source.Centre + Normal * lateral;
                P edge = (along < 0 ? Start : End).At(lateral, HalfWidth);
                // Retain exact lateral section on the source tangent despite roundoff at the seam.
                return mid + Direction * (Dot(edge - mid, Direction) * Math.Abs(along));
            }
        }
        public static double Dot(P a, P b) => a.X * b.X + a.Y * b.Y;
        public static double Cross(P a, P b) => a.X * b.Y - a.Y * b.X;
        static P Left(P a) => new P(-a.Y, a.X);
        static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
        static void Check(P p) { if (!Finite(p.X) || !Finite(p.Y)) throw new ArgumentException("nonfinite geometry"); }
        static P Unit(P p) { Check(p); if (p.Length < 1e-9) throw new ArgumentException("zero tangent/route edge"); return p * (1 / p.Length); }

        // Lossless provenance path: ordered intervals, not nearest-projection guesses.
        public sealed class Segment
        {
            public readonly string Key;
            public readonly P Start, Direction;
            public readonly double Length, Curvature;
            public Segment(string key, P start, P direction, double length, double curvature)
            {
                Check(start); Direction = Unit(direction);
                if (!Finite(length) || length <= 0 || !Finite(curvature)) throw new ArgumentException("invalid source segment");
                Key = key; Start = start; Length = length; Curvature = curvature;
            }
            public P Tangent(double s) { double a = s * Curvature; return Direction * Math.Cos(a) + Left(Direction) * Math.Sin(a); }
            public P At(double s)
            {
                if (Curvature == 0) return Start + Direction * s;
                double a = s * Curvature;
                return Start + Direction * (Math.Sin(a) / Curvature) + Left(Direction) * (2 * Math.Sin(a / 2) * Math.Sin(a / 2) / Curvature);
            }
            public double CapError(double step)
            {
                if (Curvature == 0) return 0;
                double a = Math.Abs(Curvature) * step / 2;
                return Math.Sqrt(Math.Pow(2 * Math.Sin(a / 2) * Math.Sin(a / 2), 2) + Math.Pow(a - Math.Sin(a), 2)) / Math.Abs(Curvature);
            }
        }
        public sealed class Interval
        {
            public readonly Tile Tile; public readonly Segment Segment; public readonly double From, To;
            public Interval(Tile tile, Segment segment, double from, double to) { Tile = tile; Segment = segment; From = from; To = to; }
        }
        public static Footprint[] BuildSource(IReadOnlyList<Interval> source, bool closed = false, double width = 2)
        {
            const double roundoff = 1e-9;
            if (source == null || source.Count == 0 || !Finite(width) || width <= 0) throw new ArgumentException("empty source");
            var ordered = new List<(Tile tile, P dir, double station)>();
            var seen = new HashSet<string>(); double station = 0;
            for (int i = 0; i < source.Count; i++)
            {
                var x = source[i]; var g = x.Segment; var t = x.Tile;
                if (!Finite(x.From) || !Finite(x.To) || x.From < -roundoff || x.To > g.Length + roundoff || x.To <= x.From ||
                    Math.Abs(t.Step - (x.To - x.From)) > roundoff ||
                    (t.Centre - g.At((x.From + x.To) / 2)).Length > roundoff ||
                    (t.Tangent - g.Tangent((x.From + x.To) / 2)).Length > roundoff)
                    throw new ArgumentException("source interval/geometry mismatch: " + t.Id);
                if (i == 0 || !ReferenceEquals(g, source[i - 1].Segment))
                {
                    if (Math.Abs(x.From) > roundoff || !seen.Add(g.Key)) throw new ArgumentException("missing/repeated source segment");
                    if (i > 0 && (Math.Abs(source[i - 1].To - source[i - 1].Segment.Length) > roundoff ||
                        (source[i - 1].Segment.At(source[i - 1].To) - g.Start).Length > roundoff ||
                        Dot(source[i - 1].Segment.Tangent(source[i - 1].To), g.Direction) < 1 - 1e-9))
                        throw new ArgumentException("disconnected/cusped source boundary");
                }
                else if (Math.Abs(x.From - source[i - 1].To) > roundoff) throw new ArgumentException("missing/overlapping source interval");
                ordered.Add((t, Unit(t.Tangent), station)); station += t.Step;
            }
            var last = source[source.Count - 1];
            if (Math.Abs(last.To - last.Segment.Length) > roundoff) throw new ArgumentException("missing source end");
            if (closed && ((last.Segment.At(last.To) - source[0].Segment.Start).Length > roundoff ||
                Dot(last.Segment.Tangent(last.To), source[0].Segment.Direction) < 1 - 1e-9)) throw new ArgumentException("disconnected closing source");
            return Finish(ordered, closed, width, (i, j) => source[i].Segment.CapError(source[i].Tile.Step) + source[j].Segment.CapError(source[j].Tile.Step) + roundoff, true);
        }

        /// <summary>Sort by geometric projection onto an ordered per-track route. At crossings,
        /// equally close nonlocal stations reject rather than guess. No per-ID/sample exceptions.
        /// closed routes omit a repeated last point. Duplicate/zero edges, >=90 degree joins,
        /// missing tiles, parallel displaced tangents and folded footprints are unsupported/rejected.
        /// tolerance is input quantisation tolerance, NOT padding; it never enlarges a polygon.</summary>
        public static Footprint[] Build(IReadOnlyList<P> route, IEnumerable<Tile> tiles,
            bool closed = false, double width = 2, double tolerance = .002)
        {
            if (route == null || route.Count < (closed ? 3 : 2) || !Finite(width) || width <= 0 ||
                !Finite(tolerance) || tolerance <= 0) throw new ArgumentException("invalid track contract");
            int edges = closed ? route.Count : route.Count - 1;
            var dirs = new P[edges]; var lengths = new double[edges]; var stations = new double[edges + 1];
            for (int i = 0; i < edges; i++)
            {
                Check(route[i]); P delta = route[(i + 1) % route.Count] - route[i];
                dirs[i] = Unit(delta); lengths[i] = delta.Length; stations[i + 1] = stations[i] + lengths[i];
                if (i > 0 && Dot(dirs[i - 1], dirs[i]) <= 0) throw new ArgumentException("route cusp/unsupported turn");
            }
            if (closed && Dot(dirs[edges - 1], dirs[0]) <= 0) throw new ArgumentException("route closing cusp");
            double total = stations[edges];
            double Distance(double a, double b) { double d = Math.Abs(a - b); return closed ? Math.Min(d, total - d) : d; }
            var ordered = new List<(Tile tile, P dir, double station)>();
            foreach (var tile in tiles)
            {
                if (tile == null || !Finite(tile.Step) || tile.Step <= 0) throw new ArgumentException("invalid step");
                Check(tile.Centre); P tangent = Unit(tile.Tangent);
                var candidates = new List<(double distance, double station, P direction)>();
                for (int i = 0; i < edges; i++)
                {
                    double s = Math.Max(0, Math.Min(lengths[i], Dot(tile.Centre - route[i], dirs[i])));
                    candidates.Add(((tile.Centre - route[i] - dirs[i] * s).Length, stations[i] + s, dirs[i]));
                }
                var best = candidates.OrderBy(x => x.distance).First();
                if (best.distance > width / 2 + tolerance) throw new ArgumentException("tile not on supplied track route: " + tile.Id);
                foreach (var c in candidates)
                    if (Math.Abs(c.distance - best.distance) <= 1e-8 && Distance(c.station, best.station) > tolerance)
                        throw new ArgumentException("ambiguous route projection: " + tile.Id);
                if (Math.Abs(Dot(tangent, best.direction)) < .9) throw new ArgumentException("tile not tangent to route: " + tile.Id);
                if (Dot(tangent, best.direction) < 0) tangent = tangent * -1;
                ordered.Add((tile, tangent, best.station));
            }
            ordered.Sort((a, b) => a.station.CompareTo(b.station));
            return Finish(ordered, closed, width, (i, j) => tolerance * 4 + .05 * Math.Min(ordered[i].tile.Step, ordered[j].tile.Step));
        }
        static Footprint[] Finish(List<(Tile tile, P dir, double station)> ordered, bool closed, double width, Func<int, int, double> capBound, bool provenance = false)
        {
            int n = ordered.Count;
            if (n == 0 || (closed && n < 3)) throw new ArgumentException("empty/incomplete track");
            for (int i = 1; i < n; i++) if (ordered[i].station - ordered[i - 1].station <= 1e-9)
                throw new ArgumentException("duplicate/ambiguous track ordering");
            var starts = new Seam[n]; var ends = new Seam[n]; double hw = width / 2;
            var startLimit = ordered.Select(x => x.tile.Step / 2 + 1e-8).ToArray(); var endLimit = (double[])startLimit.Clone();
            Seam Cap(int i, double sign)
            {
                var a = ordered[i]; P c = a.tile.Centre + a.dir * (sign * a.tile.Step / 2), normal = Left(a.dir);
                return new Seam(c - normal * hw, c + normal * hw);
            }
            if (!closed) { starts[0] = Cap(0, -1); ends[n - 1] = Cap(n - 1, 1); }
            for (int i = 0; i < (closed ? n : n - 1); i++)
            {
                int j = (i + 1) % n; var a = ordered[i]; var b = ordered[j];
                if (Dot(a.dir, b.dir) <= 0) throw new ArgumentException("cusp/unsupported mitre");
                P ae = a.tile.Centre + a.dir * (a.tile.Step / 2), bs = b.tile.Centre - b.dir * (b.tile.Step / 2);
                // Only contiguous intervals may join. Bound is a validation threshold for arc-vs-tangent
                // endpoints and rounded exports, not a displacement, extension or clipping margin.
                if ((ae - bs).Length > capBound(i, j))
                    throw new ArgumentException("nonadjacent/missing interval: " + a.tile.Id + " -> " + b.tile.Id + " endpoint separation=" + (ae - bs).Length);
                P Side(double lateral)
                {
                    P p = a.tile.Centre + Left(a.dir) * lateral, q = b.tile.Centre + Left(b.dir) * lateral;
                    double den = Cross(a.dir, b.dir);
                    if (Math.Abs(den) < 1e-9)
                    {
                        if (Math.Abs(Cross(q - p, a.dir)) > 1e-8) throw new ArgumentException("parallel displaced tangents: " + a.tile.Id + " -> " + b.tile.Id + " offset=" + Cross(q - p, a.dir));
                        return p + a.dir * Dot((ae + bs) * .5 - p, a.dir);
                    }
                    return p + a.dir * (Cross(q - p, b.dir) / den);
                }
                var seam = new Seam(Side(-hw), Side(hw)); ends[i] = starts[j] = seam;
                // A short straight between arcs legitimately owns a long mitre: do not reject
                // by an arbitrary multiple of its step. Bound seam extension from cap deviation
                // and the side-line intersection equation, including unchanged half-width.
                double sin = Math.Abs(Cross(a.dir, b.dir)), cos = Dot(a.dir,b.dir);
                double extra = sin < 1e-9 ? capBound(i,j) : capBound(i,j)/sin + hw*sin/(1+cos);
                endLimit[i] += extra; startLimit[j] += extra;
            }
            var result = new Footprint[n];
            for (int i = 0; i < n; i++)
            {
                var a = ordered[i]; var f = new Footprint(a.tile, a.dir, starts[i], ends[i], width);
                foreach (double lateral in new[] { -hw, 0, hw })
                {
                    double s = Dot(f.Start.At(lateral, hw) - a.tile.Centre, a.dir);
                    double e = Dot(f.End.At(lateral, hw) - a.tile.Centre, a.dir);
                    if (!Finite(s) || !Finite(e) || s >= -1e-8 || e <= 1e-8 ||
                        (provenance ? (-s > startLimit[i] || e > endLimit[i]) : Math.Max(-s, e) > a.tile.Step * 2))
                        throw new ArgumentException("folded/spiking mitre: " + a.tile.Id);
                }
                var p = f.Polygon;
                for (int k = 0; k < 4; k++) if (Cross(p[(k + 1) % 4] - p[k], p[(k + 2) % 4] - p[(k + 1) % 4]) <= 1e-10)
                    throw new ArgumentException("nonconvex mitre");
                result[i] = f;
            }
            return result;
        }
    }
}
#endif
