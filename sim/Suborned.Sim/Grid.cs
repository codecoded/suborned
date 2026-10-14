// The walk grid (sim/data/handover.site.json grid): A* over free cells, 8 neighbours, no corner cutting, integer costs, ties broken
// by cell index so every run finds the same path; then string-pulled to an any-angle polyline (the walk.ts way). A Path moves along
// its polyline by millimetres, so a speed in cm/s advances exactly that many mm a tick at 10 Hz.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed class Grid
    {
        public readonly int X0, Y0, Cell, Nx, Ny;
        readonly bool[] free;
        public Grid(JNode g)
        {
            X0 = g["x0"].Int(); Y0 = g["y0"].Int(); Cell = g["cell"].Int(); Nx = g["nx"].Int(); Ny = g["ny"].Int();
            var rows = g["rows"].Items;
            if (rows.Count != Nx) throw new FormatException("grid rows != nx");
            free = new bool[Nx * Ny];
            for (int i = 0; i < Nx; i++)
            {
                var r = rows[i].Str(); if (r.Length != Ny) throw new FormatException($"grid row {i} length != ny");
                for (int j = 0; j < Ny; j++)
                {
                    if (r[j] == '.') free[i * Ny + j] = true; else if (r[j] != '#') throw new FormatException($"grid row {i} has '{r[j]}'");
                }
            }
        }
        public bool FreeCell(int i, int j) => i >= 0 && j >= 0 && i < Nx && j < Ny && free[i * Ny + j];
        public int CI(int x) => FloorDiv(x - X0, Cell);
        public int CJ(int y) => FloorDiv(y - Y0, Cell);
        static int FloorDiv(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);
        public bool Free(Pt p) => FreeCell(CI(p.X), CJ(p.Y));
        public Pt Centre(int i, int j) => new Pt(X0 + i * Cell + Cell / 2, Y0 + j * Cell + Cell / 2);

        /// <summary>The nearest free cell centre to p (ties: lowest i, then j), within 6 m.</summary>
        public Pt? NearestFree(Pt p)
        {
            if (Free(p)) return p;
            int ci = CI(p.X), cj = CJ(p.Y); Pt? best = null; long bd = long.MaxValue;
            for (int r = 1; r <= 12; r++)
            {
                for (int i = ci - r; i <= ci + r; i++) for (int j = cj - r; j <= cj + r; j++)
                {
                    if (!FreeCell(i, j)) continue;
                    var c = Centre(i, j); long d = G.D2(c, p);
                    if (d < bd) { bd = d; best = c; }
                }
                if (best.HasValue) return best;
            }
            return null;
        }

        /// <summary>True if the straight segment a→b stays on free cells (sampled every 10 cm).</summary>
        public bool LineFree(Pt a, Pt b)
        {
            int n = Math.Max(1, G.Dist(a, b) / 10);
            for (int k = 0; k <= n; k++) if (!Free(G.Lerp(a, b, k, n))) return false;
            return true;
        }

        static readonly int[] DI = { 1, -1, 0, 0, 1, 1, -1, -1 };
        static readonly int[] DJ = { 0, 0, 1, -1, 1, -1, 1, -1 };

        /// <summary>The shortest walk from a to b as an any-angle polyline (cm), or null if b is not reachable. b must stand on free ground.</summary>
        /// <summary>A*'s buffers, kept between calls (Redline E6), one set per thread, since one grid serves every game that loads it
        /// (the tests run games in parallel; Unity runs one thread). A cell's entries count only when its stamp is this call's generation,
        /// so reuse can't change a route.</summary>
        sealed class Pool
        {
            public int[] GScore = Array.Empty<int>(), Prev = Array.Empty<int>(), Stamp = Array.Empty<int>(); public bool[] Closed = Array.Empty<bool>(); public int Gen; public readonly Heap Heap = new Heap();
            public void Fit(int n) { if (GScore.Length != n) { GScore = new int[n]; Prev = new int[n]; Stamp = new int[n]; Closed = new bool[n]; Gen = 0; } if (++Gen == int.MaxValue) { Array.Clear(Stamp, 0, n); Gen = 1; } Heap.Clear(); }
            public int G(int k) => Stamp[k] == Gen ? GScore[k] : int.MaxValue;
            public void Set(int k, int g, int p) { if (Stamp[k] != Gen) { Stamp[k] = Gen; Closed[k] = false; } GScore[k] = g; Prev[k] = p; }
            public bool IsClosed(int k) => Stamp[k] == Gen && Closed[k];
        }
        [ThreadStatic] static Pool? pool;

        public Pt[]? Route(Pt a, Pt b)
        {
            Pt? s0 = NearestFree(a); if (!s0.HasValue || !Free(b)) return null;
            int si = CI(s0.Value.X), sj = CJ(s0.Value.Y), ti = CI(b.X), tj = CJ(b.Y);
            int n = Nx * Ny, s = si * Ny + sj, t = ti * Ny + tj;
            var P = pool ??= new Pool(); P.Fit(n); var heap = P.Heap;
            P.Set(s, 0, -1); heap.Push(H(si, sj, ti, tj), s);
            int orth = Cell, diag = Cell * 1414 / 1000;
            while (heap.Count > 0)
            {
                int k = heap.Pop(); if (P.IsClosed(k)) continue; P.Closed[k] = true;
                if (k == t) break;
                int i = k / Ny, j = k % Ny;
                for (int d = 0; d < 8; d++)
                {
                    int ni = i + DI[d], nj = j + DJ[d];
                    if (!FreeCell(ni, nj)) continue;
                    if (d >= 4 && (!FreeCell(i + DI[d], j) || !FreeCell(i, j + DJ[d]))) continue; // no corner cutting
                    int nk = ni * Ny + nj; if (P.IsClosed(nk)) continue;
                    int ng = P.G(k) + (d < 4 ? orth : diag);
                    if (ng < P.G(nk)) { P.Set(nk, ng, k); heap.Push(ng + H(ni, nj, ti, tj), nk); }
                }
            }
            if (P.G(t) == int.MaxValue) return null;
            var cells = new List<Pt>();
            for (int k = t; k != s; k = P.Prev[k]) cells.Add(Centre(k / Ny, k % Ny));
            cells.Reverse();
            // the raw walk: from the start point, through the cell centres, to the exact end point
            var raw = new List<Pt> { a };
            if (s0.Value != a) raw.Add(s0.Value);
            raw.AddRange(cells);
            if (raw[raw.Count - 1] != b) { if (cells.Count > 0) raw[raw.Count - 1] = b; else raw.Add(b); }
            // string-pull: from each kept point, jump to the furthest raw point in a straight free line
            var path = new List<Pt> { raw[0] };
            int at = 0;
            while (at < raw.Count - 1)
            {
                int to = raw.Count - 1;
                while (to > at + 1 && !LineFree(raw[at], raw[to])) to--;
                path.Add(raw[to]); at = to;
            }
            return path.ToArray();
        }

        int H(int i, int j, int ti, int tj)
        {
            int dx = Math.Abs(i - ti), dy = Math.Abs(j - tj), mn = Math.Min(dx, dy), mx = Math.Max(dx, dy);
            return (mx - mn) * Cell + mn * (Cell * 1414 / 1000);
        }

        /// <summary>A binary min-heap of (priority, cell), ties to the lower cell index: the same order on every run.</summary>
        sealed class Heap
        {
            readonly List<long> a = new List<long>();
            public int Count => a.Count;
            public void Clear() => a.Clear();
            public void Push(int pri, int cell)
            {
                a.Add(((long)pri << 32) | (uint)cell);
                int c = a.Count - 1;
                while (c > 0) { int p = (c - 1) / 2; if (a[p] <= a[c]) break; (a[p], a[c]) = (a[c], a[p]); c = p; }
            }
            public int Pop()
            {
                long top = a[0]; long last = a[a.Count - 1]; a.RemoveAt(a.Count - 1);
                if (a.Count > 0)
                {
                    a[0] = last; int c = 0;
                    while (true)
                    {
                        int l = 2 * c + 1, r = l + 1, m = c;
                        if (l < a.Count && a[l] < a[m]) m = l;
                        if (r < a.Count && a[r] < a[m]) m = r;
                        if (m == c) break; (a[m], a[c]) = (a[c], a[m]); c = m;
                    }
                }
                return (int)(top & 0xffffffffL);
            }
        }
    }

    /// <summary>A polyline walked by distance: lengths in mm, so cm/s at 10 Hz is that many mm a tick.</summary>
    public sealed class Path
    {
        public readonly Pt[] Points; readonly long[] cum; public readonly long Length; // mm
        public Path(Pt[] pts)
        {
            Points = pts; cum = new long[pts.Length];
            for (int i = 1; i < pts.Length; i++) cum[i] = cum[i - 1] + G.Isqrt(G.D2(pts[i - 1], pts[i]) * 100);
            Length = cum[pts.Length - 1];
        }
        public Pt End => Points[Points.Length - 1];
        public Pt At(long mm)
        {
            if (mm <= 0) return Points[0];
            if (mm >= Length) return End;
            int i = 1; while (cum[i] < mm) i++;
            long seg = cum[i] - cum[i - 1];
            return G.Lerp(Points[i - 1], Points[i], mm - cum[i - 1], seg);
        }
        /// <summary>The direction of travel at mm (the segment's vector).</summary>
        public Pt Heading(long mm)
        {
            if (Points.Length < 2) return new Pt(0, 0);
            int i = 1; while (i < Points.Length - 1 && cum[i] < mm) i++;
            return Points[i] - Points[i - 1];
        }
    }
}
