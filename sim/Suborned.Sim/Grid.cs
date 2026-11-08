// The walk grid (sim/data/handover.site.json grid): A* over free cells, 8 neighbours, no corner cutting, integer costs, ties broken
// by cell index so every run finds the same path; then string-pulled to an any-angle polyline (the walk.ts way). A Path moves along
// its polyline by millimetres, so a speed in cm/s advances exactly that many mm a tick at 10 Hz. Engine 0.49.0: a grid that carries a
// walk raster (grid.fine) walks and stands on it, and routes over the free ground of each cell (GridFine.cs).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Grid
    {
        public readonly int X0, Y0, Cell, Nx, Ny;
        readonly bool[] free;
        /// <summary>Brief 023 (car.pace): each free cell's surface on a drive grid, 'r' carriageway or 'y' drive or bay; '.' on a walk grid.</summary>
        readonly char[] surface;
        /// <summary>True when every free cell names its surface ('r' or 'y'): a drive grid as tools/sim/drive.ts writes it since car.pace.</summary>
        public readonly bool Surfaces;
        public Grid(JNode g)
        {
            X0 = g["x0"].Int(); Y0 = g["y0"].Int(); Cell = g["cell"].Int(); Nx = g["nx"].Int(); Ny = g["ny"].Int(); Body = g.Opt("body")?.Int() ?? 0;
            if (g.Opt("fine") is JNode fn) ffree = ReadFine(fn, Nx, Ny, Cell, out FCell, out FNx, out FNy, out per); // engine 0.49.0 (GridFine.cs)
            var rows = g["rows"].Items;
            if (rows.Count != Nx) throw new FormatException("grid rows != nx");
            free = new bool[Nx * Ny]; surface = new char[Nx * Ny];
            bool dots = false, marks = false;
            for (int i = 0; i < Nx; i++)
            {
                var r = rows[i].Str(); if (r.Length != Ny) throw new FormatException($"grid row {i} length != ny");
                for (int j = 0; j < Ny; j++)
                {
                    char c = r[j];
                    if (c == '.' || c == 'r' || c == 'y') { free[i * Ny + j] = true; surface[i * Ny + j] = c; if (c == '.') dots = true; else marks = true; }
                    else if (c != '#') throw new FormatException($"grid row {i} has '{c}'");
                }
            }
            if (dots && marks) throw new FormatException("grid: a free cell is '.' (walk grid) or names its surface 'r'/'y' (drive grid), never both in one grid");
            Surfaces = marks;
            // brief 055 (engine 0.27.0): the floor of every cell, block z cm (the pavement 0, the carriageway -18, the Office landing 75), per row as run-length
            // pairs [cm, cells] (tools/sim/solids.ts floorRows). Absent (a drive grid, a range site): every cell is the pavement.
            floor = new short[Nx * Ny];
            if (g.Opt("floor") is JNode fl)
            {
                var fr = fl.Items; if (fr.Count != Nx) throw new FormatException("grid floor rows != nx");
                for (int i = 0; i < Nx; i++)
                {
                    var run = fr[i].Items; if (run.Count % 2 != 0) throw new FormatException($"grid floor row {i}: [cm, cells] pairs");
                    int j = 0;
                    for (int k = 0; k < run.Count; k += 2)
                    {
                        int cm = run[k].Int(), n = run[k + 1].Int();
                        if (cm < -1000 || cm > 10000 || n < 1 || j + n > Ny) throw new FormatException($"grid floor row {i}: a run of {n} at {cm} cm is out of range");
                        for (int m = 0; m < n; m++) floor[i * Ny + j + m] = (short)cm;
                        j += n;
                    }
                    if (j != Ny) throw new FormatException($"grid floor row {i} covers {j} cells, not ny {Ny}");
                }
            }
        }
        readonly short[] floor;
        /// <summary>Brief 055: the floor under p, block z cm (0 off the grid: the pavement datum). Read by the host and the tools; no rule reads it yet (sight stays at eye-cm above the pavement).</summary>
        public int FloorAt(Pt p) { int i = CI(p.X), j = CJ(p.Y); return i >= 0 && j >= 0 && i < Nx && j < Ny ? floor[i * Ny + j] : 0; }
        /// <summary>The surface of the cell at p ('r', 'y', '.'), or '#' off free ground.</summary>
        public char SurfaceAt(Pt p) { int i = CI(p.X), j = CJ(p.Y); return FreeCell(i, j) ? surface[i * Ny + j] : '#'; }

        // ---------------------------------------------------------------- the car's route (brief 023, Keel with Pulse; Aster 2026-10-19)
        // A car keeps off the kerb: on a carriageway its centre keeps `lane` cm from the edge of the drivable ground where it can (half the city's
        // lane: the lane centre on the side it drives), on a drive or in a bay it keeps to the middle. Clearance = the Chebyshev ring distance in
        // cells from a free cell to the nearest cell that is not (exact, by a breadth-first pass from every blocked cell). The route is A* whose
        // step costs the cell's shortfall below its target (carriageway: the lane in cells; drive or bay: ClearCap, so the most clearance there is),
        // then string-pulled only along lines whose cells keep at least the clearance the A* path kept between the two ends: the pulled line never
        // runs nearer an edge than the cells it replaces. Integer costs, ties by cell index: the same route on every run.
        const int ClearCap = 8;
        int[]? clear;
        int[] Clearance()
        {
            var c = clear; if (c != null) return c;
            int n = Nx * Ny; c = new int[n]; var q = new int[n]; int head = 0, tail = 0;
            for (int k = 0; k < n; k++) { if (!free[k]) { c[k] = 0; q[tail++] = k; } else c[k] = int.MaxValue; }
            // cells off the window count as blocked: the free border cells are seeds at distance 1 (after every 0, so the queue stays in order)
            for (int k = 0; k < n; k++)
            {
                int i = k / Ny, j = k % Ny;
                if (free[k] && (i == 0 || j == 0 || i == Nx - 1 || j == Ny - 1)) { c[k] = 1; q[tail++] = k; }
            }
            while (head < tail)
            {
                int k = q[head++], i = k / Ny, j = k % Ny;
                for (int d = 0; d < 8; d++)
                {
                    int ni = i + DI[d], nj = j + DJ[d]; if (ni < 0 || nj < 0 || ni >= Nx || nj >= Ny) continue;
                    int nk = ni * Ny + nj; if (c[nk] <= c[k] + 1) continue;
                    c[nk] = c[k] + 1; q[tail++] = nk;
                }
            }
            for (int k = 0; k < n; k++) if (c[k] > ClearCap) c[k] = ClearCap;
            clear = c; return c; // benign race: every thread computes the same array
        }
        /// <summary>A cell's clearance (0 off free ground), capped at ClearCap.</summary>
        public int ClearanceAt(Pt p) { int i = CI(p.X), j = CJ(p.Y); return FreeCell(i, j) ? Clearance()[i * Ny + j] : 0; }

        /// <summary>The car's route from a to b on this drive grid (see above); `laneCm` the carriageway's keep-off. Null if b is unreachable.
        /// Brief 098 step 1, with traffic only: `m` the cells passing cars stand on, driven round; `fx, fy` each carriageway cell's way of travel
        /// (×100; 0 for none: a junction, a bay, both halves): a cell driven against it costs three cells more, and no pulled line runs against
        /// one, so a mission car keeps to the right as the street does. Null for both: the grid as built.</summary>
        public Pt[]? CarRoute(Pt a, Pt b, int laneCm, bool[]? m = null, short[]? fx = null, short[]? fy = null)
        {
            if ((m != null || fx != null) && CarRoute(a, b, laneCm, m, fx, fy, out var r)) return r;
            return CarRoute(a, b, laneCm, null, null, null, out var r0) ? r0 : null;
        }
        static bool Against(short[]? fx, short[]? fy, int k, long dx, long dy) => fx != null && (fx[k] != 0 || fy![k] != 0) && dx * fx[k] + dy * fy![k] < 0;
        bool CarRoute(Pt a, Pt b, int laneCm, bool[]? m, short[]? fx, short[]? fy, out Pt[]? result)
        {
            result = null;
            Pt? s0 = NearestFree(a); if (!s0.HasValue || !Free(b)) return false;
            var c = Clearance();
            int laneCells = Math.Min(ClearCap, (laneCm + Cell / 2 + Cell - 1) / Cell); // the centre (c − ½ cell from the edge) at least laneCm off
            int Short(int k) { int target = surface[k] == 'r' ? laneCells : ClearCap; return Math.Max(0, target - c[k]); }
            int si = CI(s0.Value.X), sj = CJ(s0.Value.Y), ti = CI(b.X), tj = CJ(b.Y);
            int n = Nx * Ny, s = si * Ny + sj, t = ti * Ny + tj;
            var P = pool ??= new Pool(); P.Fit(n); var heap = P.Heap;
            P.Set(s, 0, -1); heap.Push(H(si, sj, ti, tj), s);
            int orth = Cell, diag = Cell * 1414 / 1000, per = 2 * Cell; // a cell of shortfall costs two cells of distance
            while (heap.Count > 0)
            {
                int k = heap.Pop(); if (P.IsClosed(k)) continue; P.Closed[k] = true;
                if (k == t) break;
                int i = k / Ny, j = k % Ny;
                for (int d = 0; d < 8; d++)
                {
                    int ni = i + DI[d], nj = j + DJ[d];
                    if (!Open(ni, nj, m) && !(ni == ti && nj == tj)) continue;
                    if (d >= 4 && (!Open(i + DI[d], j, m) || !Open(i, j + DJ[d], m))) continue; // no corner cutting
                    int nk = ni * Ny + nj; if (P.IsClosed(nk)) continue;
                    int ng = P.G(k) + (d < 4 ? orth : diag) + per * Short(nk) + (Against(fx, fy, nk, DI[d], DJ[d]) ? 3 * orth : 0);
                    if (ng < P.G(nk)) { P.Set(nk, ng, k); heap.Push(ng + H(ni, nj, ti, tj), nk); }
                }
            }
            if (P.G(t) == int.MaxValue) return false;
            var cells = new List<int>();
            for (int k = t; k != s; k = P.Prev[k]) cells.Add(k);
            cells.Add(s); cells.Reverse();
            var raw = new List<Pt>(); var rc = new List<int>(); // the raw drive and each point's clearance
            raw.Add(a); rc.Add(c[s]);
            foreach (var k in cells) { var ce = Centre(k / Ny, k % Ny); if (ce != raw[raw.Count - 1]) { raw.Add(ce); rc.Add(c[k]); } }
            if (raw[raw.Count - 1] != b) { raw.Add(b); rc.Add(c[t]); }
            var path = new List<Pt> { raw[0] };
            int at = 0;
            while (at < raw.Count - 1)
            {
                int to = raw.Count - 1;
                while (to > at + 1)
                {
                    int keep = int.MaxValue; for (int q = at; q <= to; q++) keep = Math.Min(keep, rc[q]);
                    if (LineKeeps(raw[at], raw[to], keep, c, m, fx, fy)) break;
                    to--;
                }
                path.Add(raw[to]); at = to;
            }
            result = path.ToArray(); return true;
        }
        /// <summary>The straight segment a→b stays on cells of clearance ≥ keep (sampled every 10 cm), off the mask's cells and not against their way.</summary>
        bool LineKeeps(Pt a, Pt b, int keep, int[] c, bool[]? m = null, short[]? fx = null, short[]? fy = null)
        {
            int n = Math.Max(1, G.Dist(a, b) / 10);
            for (int k = 0; k <= n; k++) { var p = G.Lerp(a, b, k, n); int i = CI(p.X), j = CJ(p.Y); if (!FreeCell(i, j) || c[i * Ny + j] < keep || (m != null && m[i * Ny + j] && k > 0 && k < n) || Against(fx, fy, i * Ny + j, b.X - a.X, b.Y - a.Y)) return false; }
            return true;
        }
        public bool FreeCell(int i, int j) => i >= 0 && j >= 0 && i < Nx && j < Ny && free[i * Ny + j];
        /// <summary>Engine 0.33.1 (rulebook car.footprint): a free cell no standing car blocks. `m` is one game's mask of the cells its standing
        /// cars cover (Game.CarCells, index i·Ny + j), or null: the grid as built. The grid itself never changes (one grid serves every game).</summary>
        public bool Open(int i, int j, bool[]? m) => FreeCell(i, j) && (m == null || !m[i * Ny + j]);
        public int CI(int x) => FloorDiv(x - X0, Cell);
        public int CJ(int y) => FloorDiv(y - Y0, Cell);
        static int FloorDiv(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);
        public bool Free(Pt p) => ffree != null ? FineFree(p) : FreeCell(CI(p.X), CJ(p.Y));
        public bool Free(Pt p, bool[]? m) => ffree != null ? FineFree(p) && (m == null || !Masked(p, m)) : Open(CI(p.X), CJ(p.Y), m);
        public Pt Centre(int i, int j) => new Pt(X0 + i * Cell + Cell / 2, Y0 + j * Cell + Cell / 2);

        /// <summary>The nearest free cell centre to p (ties: lowest i, then j), within 6 m. `m`: the cells standing cars block (Open).</summary>
        public Pt? NearestFree(Pt p, bool[]? m = null)
        {
            if (Free(p, m)) return p;
            int ci = CI(p.X), cj = CJ(p.Y); Pt? best = null; long bd = long.MaxValue;
            for (int r = 1; r <= 12; r++)
            {
                for (int i = ci - r; i <= ci + r; i++) for (int j = cj - r; j <= cj + r; j++)
                {
                    if (!Open(i, j, m)) continue;
                    var c = Centre(i, j); long d = G.D2(c, p);
                    if (d < bd) { bd = d; best = c; }
                }
                if (best.HasValue) return best;
            }
            return null;
        }

        /// <summary>Is p inside the grid's window (on a cell, free or not)?</summary>
        public bool InGrid(Pt p) { int i = CI(p.X), j = CJ(p.Y); return i >= 0 && j >= 0 && i < Nx && j < Ny; }
        /// <summary>Engine 0.33.0 (brief 060, Suture's cause A): the free cell nearest p with no distance cap: p's cell clamped into the grid, then
        /// rings outward from it until one holds a free cell; of that ring the centre nearest p (ties: the lower i, then j). Null only for a grid
        /// with no free cell. NearestFree's 6 m search answers first where it can.</summary>
        public Pt? NearestFreeAny(Pt p, bool[]? m = null)
        {
            var near = NearestFree(p, m); if (near.HasValue) return near;
            int ci = Math.Max(0, Math.Min(Nx - 1, CI(p.X))), cj = Math.Max(0, Math.Min(Ny - 1, CJ(p.Y)));
            int rmax = Math.Max(Nx, Ny);
            for (int r = 0; r <= rmax; r++)
            {
                Pt? best = null; long bd = long.MaxValue;
                for (int i = ci - r; i <= ci + r; i++) for (int j = cj - r; j <= cj + r; j++)
                {
                    if (Math.Max(Math.Abs(i - ci), Math.Abs(j - cj)) != r || !Open(i, j, m)) continue; // the ring only
                    var c = Centre(i, j); long d = G.D2(c, p);
                    if (d < bd) { bd = d; best = c; }
                }
                if (best.HasValue) return best;
            }
            return null;
        }

        /// <summary>Engine 0.33.0: each free cell's connected piece of the grid (Route's 8 neighbours, no corner cutting), its id the lowest cell
        /// index in it; -1 for a blocked cell. Built once, on first use, the same on every thread.</summary>
        int[] Pieces()
        {
            var c = pieces; if (c != null) return c;
            lock (piecesLock)
            {
                if (pieces != null) return pieces;
                int n = Nx * Ny; var lab = new int[n]; for (int k = 0; k < n; k++) lab[k] = -1;
                var stack = new Stack<int>();
                for (int k0 = 0; k0 < n; k0++)
                {
                    if (!free[k0] || lab[k0] >= 0) continue;
                    lab[k0] = k0; stack.Push(k0);
                    while (stack.Count > 0)
                    {
                        int k = stack.Pop(), i = k / Ny, j = k % Ny;
                        for (int d = 0; d < 8; d++)
                        {
                            int ni = i + DI[d], nj = j + DJ[d];
                            if (!FreeCell(ni, nj)) continue;
                            if (d >= 4 && (!FreeCell(i + DI[d], j) || !FreeCell(i, j + DJ[d]))) continue;
                            int nk = ni * Ny + nj; if (lab[nk] >= 0) continue;
                            lab[nk] = k0; stack.Push(nk);
                        }
                    }
                }
                pieceSize = new int[n]; for (int k = 0; k < n; k++) if (lab[k] >= 0) pieceSize[lab[k]]++;
                pieces = lab; return lab;
            }
        }
        int[]? pieces, pieceSize; readonly object piecesLock = new object();
        /// <summary>The piece a free point's cell lies in (-1 off free ground) and its size in cells.</summary>
        public int PieceOf(Pt p) { if (ffree != null) return PieceOfFine(p); int i = CI(p.X), j = CJ(p.Y); return FreeCell(i, j) ? Pieces()[i * Ny + j] : -1; }
        public int PieceSize(int piece) { if (ffree != null) return PieceSizeFine(piece); Pieces(); return piece >= 0 ? pieceSize![piece] : 0; }
        /// <summary>The free cell of `piece` nearest p (no cap; ties: the lower cell index), or null when the piece is empty.</summary>
        public Pt? NearestFreeIn(Pt p, int piece)
        {
            if (ffree != null) return NearestFreeInFine(p, piece);
            var lab = Pieces(); Pt? best = null; long bd = long.MaxValue;
            for (int k = 0; k < lab.Length; k++) { if (lab[k] != piece) continue; var c = Centre(k / Ny, k % Ny); long d = G.D2(c, p); if (d < bd) { bd = d; best = c; } }
            return best;
        }

        /// <summary>True if the straight segment a→b stays on free cells (sampled every 10 cm).</summary>
        public bool LineFree(Pt a, Pt b, bool[]? m = null)
        {
            if (ffree != null) return LineFreeFine(a, b, m);
            int n = Math.Max(1, G.Dist(a, b) / 10);
            for (int k = 0; k <= n; k++) if (!Free(G.Lerp(a, b, k, n), m)) return false;
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
            public void Fit(int n) { if (GScore.Length < n) { GScore = new int[n]; Prev = new int[n]; Stamp = new int[n]; Closed = new bool[n]; Gen = 0; } if (++Gen == int.MaxValue) { Array.Clear(Stamp, 0, Stamp.Length); Gen = 1; } Heap.Clear(); } // engine 0.49.0: grown, never cut (a walk's nodes and a drive grid's cells differ in number)
            public int G(int k) => Stamp[k] == Gen ? GScore[k] : int.MaxValue;
            public void Set(int k, int g, int p) { if (Stamp[k] != Gen) { Stamp[k] = Gen; Closed[k] = false; } GScore[k] = g; Prev[k] = p; }
            public bool IsClosed(int k) => Stamp[k] == Gen && Closed[k];
        }
        [ThreadStatic] static Pool? pool;

        public Pt[]? Route(Pt a, Pt b, bool[]? m = null)
        {
            if (ffree != null) return RouteFine(a, b, m);
            Pt? s0 = NearestFree(a, m); if (!s0.HasValue || !Free(b, m)) return null;
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
                    if (!Open(ni, nj, m)) continue;
                    if (d >= 4 && (!Open(i + DI[d], j, m) || !Open(i, j + DJ[d], m))) continue; // no corner cutting
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
            return Pull(raw, m); // string-pull (GridFine.cs)
        }

        /// <summary>Brief 027 (fight.close, EXPERIMENTAL): the nearest free cell centre by walk from `from` (Dijkstra over the walk grid, 8 neighbours,
        /// no corner cutting, ties to the lower cell index) for which `ok` holds, within `maxCm` of walk; null if none. The cell `from` stands on
        /// counts (its centre is tried first).</summary>
        public Pt? Nearest(Pt from, Func<Pt, bool> ok, int maxCm, bool[]? m = null)
        {
            if (ffree != null) return NearestFine(from, ok, maxCm, m);
            Pt? s0 = NearestFree(from, m); if (!s0.HasValue) return null;
            int si = CI(s0.Value.X), sj = CJ(s0.Value.Y), n = Nx * Ny, s = si * Ny + sj;
            var P = pool ??= new Pool(); P.Fit(n); var heap = P.Heap;
            P.Set(s, 0, -1); heap.Push(0, s);
            int orth = Cell, diag = Cell * 1414 / 1000;
            while (heap.Count > 0)
            {
                int k = heap.Pop(); if (P.IsClosed(k)) continue; P.Closed[k] = true;
                int i = k / Ny, j = k % Ny, g = P.G(k);
                if (g > maxCm) return null;
                var c = Centre(i, j); if (ok(c)) return c;
                for (int d = 0; d < 8; d++)
                {
                    int ni = i + DI[d], nj = j + DJ[d];
                    if (!Open(ni, nj, m)) continue;
                    if (d >= 4 && (!Open(i + DI[d], j, m) || !Open(i, j + DJ[d], m))) continue;
                    int nk = ni * Ny + nj; if (P.IsClosed(nk)) continue;
                    int ng = g + (d < 4 ? orth : diag);
                    if (ng < P.G(nk)) { P.Set(nk, ng, k); heap.Push(ng, nk); }
                }
            }
            return null;
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
            int n = Points.Length; if (n < 2) return new Pt(0, 0);
            int i = 1; while (i < n - 1 && cum[i] < mm) i++;
            // brief 022 (Pulse): a walk repeats its end points (a leg keeps both ends), so a zero-length segment is skipped: the next one that goes
            // somewhere, else the last one that did (tools/routines/walk.ts Path.heading); a path with no length still answers (0, 0)
            bool still(int k) => Points[k] == Points[k - 1];
            int j = i; while (j < n - 1 && still(j)) j++;
            if (still(j)) { j = i; while (j > 1 && still(j)) j--; }
            return Points[j] - Points[j - 1];
        }
        /// <summary>Brief 098 L9: the mm along the walk, between fromMm and toMm, of the point nearest p (a body that has fallen behind its own place
        /// rejoins its walk there). Integer: each segment's nearest point by projection, ties to the earlier.</summary>
        public long NearestMm(Pt p, long fromMm, long toMm)
        {
            if (Points.Length < 2) return 0;
            fromMm = Math.Max(0, fromMm); toMm = Math.Min(Length, toMm); long best = fromMm, bd = long.MaxValue;
            for (int i = 1; i < Points.Length; i++)
            {
                if (cum[i] < fromMm) continue; if (cum[i - 1] > toMm) break;
                Pt a = Points[i - 1], b = Points[i]; long dx = b.X - a.X, dy = b.Y - a.Y, l2 = dx * dx + dy * dy, seg = cum[i] - cum[i - 1];
                long t = l2 == 0 ? 0 : Math.Max(0, Math.Min(l2, (long)(p.X - a.X) * dx + (long)(p.Y - a.Y) * dy)); // along the segment, × l2
                long mm = cum[i - 1] + (l2 == 0 ? 0 : seg * t / l2); mm = Math.Max(fromMm, Math.Min(toMm, mm));
                long d = G.D2(At(mm), p); if (d < bd) { bd = d; best = mm; }
            }
            return best;
        }
        /// <summary>The first `mm` of the walk (brief 022: the summoned lover stops short of her post), a path of its own.</summary>
        public Path Cut(long mm)
        {
            if (mm >= Length) return this;
            if (mm <= 0) return new Path(new[] { Points[0], Points[0] });
            var pts = new List<Pt> { Points[0] };
            int i = 1; while (cum[i] < mm) { pts.Add(Points[i]); i++; }
            pts.Add(At(mm));
            return new Path(pts.ToArray());
        }
    }
}
