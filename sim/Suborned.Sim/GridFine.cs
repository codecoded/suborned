// What is walkable is what you see (brief 098, Sky 2026-11-08: "Gap between planter and fountain should be walkable, don't be one of those game
// engines where you can't get past even though visibly it's clear you have a lot of space either side. We can do better"; engine 0.49.0, Keel).
// A walk grid that carries a walk raster (site grid.fine: tools/sim/walkfine.ts) walks on it: every solid keeps the walk clearance (the rulebook's
// bodies.radius + bodies.margin, the site's grid.body) from a free raster cell's centre, so free ground is what a body fits on, to the raster's cell.
//  · Free, LineFree: the raster (a line sampled every raster cell), and the standing cars' 50 cm cells (the mask).
//  · the route: A* over nodes, each the free raster cells of one 50 cm cell that touch each other (most cells: one node, at its centre); a node is
//    passed at its own point (the cell's centre when free, else its free raster cell nearest the centre). Two nodes of neighbouring cells join where
//    the straight line between their points is free on the raster: so a corner is cut where the true ground clears it, and a gap narrower than a
//    50 cm cell still joins its two sides. Costs by the 50 cm step (50, 70), ties to the lower node: the same route on every run; then string-pulled.
//  · pieces, Nearest, NearestFreeIn: over the same nodes.
// A grid without a raster (the drive grid, the range sites) walks its 50 cm cells as before (Grid.cs).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Grid
    {
        /// <summary>Engine 0.49.0: the walk raster (null: none), its cell (cm), size, and 50 cm cells to a raster cell's side.</summary>
        readonly bool[]? ffree; public readonly int FCell, FNx, FNy; readonly int per;
        /// <summary>The site's grid.body: the clearance (cm) every solid keeps from a free raster cell's centre (0: the grid states none).</summary>
        public readonly int Body;
        public bool HasFine => ffree != null;

        static bool[] ReadFine(JNode f, int nx, int ny, int cell, out int fc, out int fnx, out int fny, out int k)
        {
            fc = f["cell"].Int(); fnx = f["nx"].Int(); fny = f["ny"].Int();
            if (fc < 1 || cell % fc != 0) throw new FormatException("grid.fine: its cell divides the grid's cell");
            k = cell / fc; if (fnx != nx * k || fny != ny * k) throw new FormatException("grid.fine: nx, ny are the grid's times its cells to a side");
            var rows = f["rows"].Items; if (rows.Count != fnx) throw new FormatException("grid.fine rows != nx");
            var a = new bool[fnx * fny];
            for (int i = 0; i < fnx; i++)
            {
                int j = 0; bool free = true;
                foreach (var run in rows[i].Items)
                {
                    int n = run.Int(); if (n < 0 || j + n > fny) throw new FormatException($"grid.fine row {i}: a run of {n} overruns ny");
                    if (free) for (int m = 0; m < n; m++) a[i * fny + j + m] = true;
                    j += n; free = !free;
                }
                if (j != fny) throw new FormatException($"grid.fine row {i} covers {j} cells, not ny {fny}");
            }
            return a;
        }

        /// <summary>The ground a body stands on, cell by cell (engine 0.49.0): the walk raster's cells, or the grid's own without one. StandFree(i, j) is
        /// Free at that cell's centre; the driven agent slides along these (GameDrive.cs) and the drive's cases find their walls on them.</summary>
        public int StandCell => ffree != null ? FCell : Cell;
        public int SI(int x) => FloorDiv(x - X0, StandCell);
        public int SJ(int y) => FloorDiv(y - Y0, StandCell);
        public bool StandFree(int i, int j) => ffree != null ? i >= 0 && j >= 0 && i < FNx && j < FNy && ffree[i * FNy + j] : FreeCell(i, j);
        public Pt StandCentre(int i, int j) => new Pt(X0 + i * StandCell + StandCell / 2, Y0 + j * StandCell + StandCell / 2);
        int FI(int x) => FloorDiv(x - X0, FCell);
        int FJ(int y) => FloorDiv(y - Y0, FCell);
        /// <summary>p stands on a free raster cell.</summary>
        bool FineFree(Pt p) { int i = FI(p.X), j = FJ(p.Y); return i >= 0 && j >= 0 && i < FNx && j < FNy && ffree![i * FNy + j]; }
        bool Masked(Pt p, bool[] m) { int i = CI(p.X), j = CJ(p.Y); return i >= 0 && j >= 0 && i < Nx && j < Ny && m[i * Ny + j]; }

        /// <summary>The raster's straight line: every raster cell's width along a→b on free ground, off the mask's cells.</summary>
        bool LineFreeFine(Pt a, Pt b, bool[]? m)
        {
            int n = Math.Max(1, G.Dist(a, b) / FCell);
            for (int k = 0; k <= n; k++) { var p = G.Lerp(a, b, k, n); if (!FineFree(p) || (m != null && Masked(p, m))) return false; }
            return true;
        }

        sealed class FineGraph
        {
            /// <summary>Per 50 cm cell its first node (the last entry: the node count); per raster cell its node within its 50 cm cell (1-based, 0 blocked).</summary>
            public int[] Base = null!; public byte[] Comp = null!;
            /// <summary>Per node: the point a walk passes it at, its 50 cm cell, its edges (CSR: EStart[u] to EStart[u + 1]) with the 50 cm step each makes (DI/DJ), its piece.</summary>
            public Pt[] Rep = null!; public int[] CellOf = null!, EStart = null!, ETo = null!, Piece = null!, PieceSize = null!; public byte[] EDir = null!;
        }
        FineGraph? fineGraph; readonly object fineLock = new object();
        FineGraph Fine()
        {
            var f = System.Threading.Volatile.Read(ref fineGraph); if (f != null) return f;
            lock (fineLock) { f = fineGraph; if (f == null) { f = BuildFine(); System.Threading.Volatile.Write(ref fineGraph, f); } return f; } // built once, the same on every thread
        }

        FineGraph BuildFine()
        {
            var F = new FineGraph(); int nc = Nx * Ny; F.Base = new int[nc + 1]; F.Comp = new byte[FNx * FNy];
            var reps = new List<Pt>(); var cellOf = new List<int>(); var stack = new Stack<int>();
            for (int i = 0; i < Nx; i++) for (int j = 0; j < Ny; j++)
                {
                    int k = i * Ny + j; F.Base[k] = reps.Count; int comps = 0;
                    for (int a = 0; a < per; a++) for (int b = 0; b < per; b++)
                        {
                            int q0 = (i * per + a) * FNy + j * per + b; if (!ffree![q0] || F.Comp[q0] != 0) continue;
                            if (comps == 255) throw new FormatException($"grid.fine: cell ({i}, {j}) holds over 255 pieces of free ground");
                            byte c = (byte)++comps; F.Comp[q0] = c; stack.Push(q0);
                            while (stack.Count > 0) // the free raster cells of this 50 cm cell that touch (four ways)
                            {
                                int q = stack.Pop(), qi = q / FNy, qj = q % FNy;
                                for (int d = 0; d < 4; d++)
                                {
                                    int ni = qi + DI[d], nj = qj + DJ[d]; if (ni < i * per || ni >= (i + 1) * per || nj < j * per || nj >= (j + 1) * per) continue;
                                    int nq = ni * FNy + nj; if (!ffree[nq] || F.Comp[nq] != 0) continue;
                                    F.Comp[nq] = c; stack.Push(nq);
                                }
                            }
                        }
                    var ctr = Centre(i, j); int cq = (i * per + per / 2) * FNy + j * per + per / 2; // the raster cell holding the cell's centre
                    for (int c = 1; c <= comps; c++)
                    {
                        Pt rep = default; long bd = long.MaxValue;
                        if (F.Comp[cq] == c) rep = ctr; // at its centre, as the 50 cm grid walked
                        else
                            for (int a = 0; a < per; a++) for (int b = 0; b < per; b++)
                                {
                                    int q = (i * per + a) * FNy + j * per + b; if (F.Comp[q] != c) continue;
                                    var p = new Pt(X0 + (i * per + a) * FCell + FCell / 2, Y0 + (j * per + b) * FCell + FCell / 2); long dd = G.D2(p, ctr);
                                    if (dd < bd) { bd = dd; rep = p; }
                                }
                        reps.Add(rep); cellOf.Add(k);
                    }
                }
            F.Base[nc] = reps.Count; F.Rep = reps.ToArray(); F.CellOf = cellOf.ToArray();
            // edges: each pair of nodes in neighbouring cells whose points see each other on the raster, tested once (the lower node first), both ways
            int n = F.Rep.Length; var eu = new List<int>(); var ev = new List<int>(); var ed = new List<byte>();
            for (int u = 0; u < n; u++)
            {
                int k = F.CellOf[u], i = k / Ny, j = k % Ny;
                for (int d = 0; d < 8; d++)
                {
                    int ni = i + DI[d], nj = j + DJ[d]; if (ni < 0 || nj < 0 || ni >= Nx || nj >= Ny) continue;
                    int nk = ni * Ny + nj;
                    for (int v = F.Base[nk]; v < F.Base[nk + 1]; v++)
                    {
                        bool ok = u < v ? LineFreeFine(F.Rep[u], F.Rep[v], null) : LineFreeFine(F.Rep[v], F.Rep[u], null);
                        if (ok) { eu.Add(u); ev.Add(v); ed.Add((byte)d); }
                    }
                }
            }
            F.EStart = new int[n + 1]; foreach (var u in eu) F.EStart[u + 1]++;
            for (int u = 0; u < n; u++) F.EStart[u + 1] += F.EStart[u];
            F.ETo = new int[eu.Count]; F.EDir = new byte[eu.Count]; var fill = new int[n]; // in the order found: each node's edges by step, then by node
            for (int e = 0; e < eu.Count; e++) { int u = eu[e], at = F.EStart[u] + fill[u]++; F.ETo[at] = ev[e]; F.EDir[at] = ed[e]; }
            // pieces: each node's connected piece, its id the lowest node in it
            F.Piece = new int[n]; for (int u = 0; u < n; u++) F.Piece[u] = -1;
            for (int u0 = 0; u0 < n; u0++)
            {
                if (F.Piece[u0] >= 0) continue; F.Piece[u0] = u0; stack.Push(u0);
                while (stack.Count > 0) { int u = stack.Pop(); for (int e = F.EStart[u]; e < F.EStart[u + 1]; e++) { int v = F.ETo[e]; if (F.Piece[v] < 0) { F.Piece[v] = u0; stack.Push(v); } } }
            }
            F.PieceSize = new int[n]; for (int u = 0; u < n; u++) F.PieceSize[F.Piece[u]]++;
            return F;
        }

        /// <summary>The node whose free ground p stands on, or -1.</summary>
        int NodeAt(FineGraph F, Pt p)
        {
            int fi = FI(p.X), fj = FJ(p.Y); if (fi < 0 || fj < 0 || fi >= FNx || fj >= FNy) return -1;
            int c = F.Comp[fi * FNy + fj]; return c == 0 ? -1 : F.Base[(fi / per) * Ny + fj / per] + c - 1;
        }
        /// <summary>A standing car's cells (the mask) close a node, and a corner of them is never cut.</summary>
        bool Shut(FineGraph F, int v, int d, int i, int j, bool[]? m) => m != null && (m[F.CellOf[v]] || (d >= 4 && (m[(i + DI[d]) * Ny + j] || m[i * Ny + j + DJ[d]])));

        Pt[]? RouteFine(Pt a, Pt b, bool[]? m)
        {
            Pt? s0 = NearestFree(a, m); if (!s0.HasValue || !Free(b, m)) return null;
            var F = Fine(); int s = NodeAt(F, s0.Value), t = NodeAt(F, b); if (s < 0 || t < 0) return null;
            int ti = CI(b.X), tj = CJ(b.Y), n = F.Rep.Length;
            var P = pool ??= new Pool(); P.Fit(n); var heap = P.Heap;
            P.Set(s, 0, -1); heap.Push(H(F.CellOf[s] / Ny, F.CellOf[s] % Ny, ti, tj), s);
            int orth = Cell, diag = Cell * 1414 / 1000;
            while (heap.Count > 0)
            {
                int u = heap.Pop(); if (P.IsClosed(u)) continue; P.Closed[u] = true;
                if (u == t) break;
                int k = F.CellOf[u], i = k / Ny, j = k % Ny;
                for (int e = F.EStart[u]; e < F.EStart[u + 1]; e++)
                {
                    int v = F.ETo[e], d = F.EDir[e]; if (Shut(F, v, d, i, j, m) || P.IsClosed(v)) continue;
                    int ng = P.G(u) + (d < 4 ? orth : diag);
                    if (ng < P.G(v)) { int kv = F.CellOf[v]; P.Set(v, ng, u); heap.Push(ng + H(kv / Ny, kv % Ny, ti, tj), v); }
                }
            }
            if (P.G(t) == int.MaxValue) return null;
            var pts = new List<Pt>();
            for (int u = t; u != s; u = P.Prev[u]) pts.Add(F.Rep[u]);
            pts.Reverse();
            var raw = new List<Pt> { a };
            if (s0.Value != a) raw.Add(s0.Value);
            raw.AddRange(pts);
            if (raw[raw.Count - 1] != b) { if (pts.Count > 0) raw[raw.Count - 1] = b; else raw.Add(b); }
            return Pull(raw, m);
        }
        /// <summary>String-pull: from each kept point, the furthest raw point in a straight free line.</summary>
        Pt[] Pull(List<Pt> raw, bool[]? m)
        {
            var path = new List<Pt> { raw[0] };
            int at = 0;
            while (at < raw.Count - 1)
            {
                int to = raw.Count - 1;
                while (to > at + 1 && !LineFree(raw[at], raw[to], m)) to--;
                path.Add(raw[to]); at = to;
            }
            return path.ToArray();
        }

        Pt? NearestFine(Pt from, Func<Pt, bool> ok, int maxCm, bool[]? m)
        {
            Pt? s0 = NearestFree(from, m); if (!s0.HasValue) return null;
            var F = Fine(); int s = NodeAt(F, s0.Value); if (s < 0) return null;
            var P = pool ??= new Pool(); P.Fit(F.Rep.Length); var heap = P.Heap;
            P.Set(s, 0, -1); heap.Push(0, s);
            int orth = Cell, diag = Cell * 1414 / 1000;
            while (heap.Count > 0)
            {
                int u = heap.Pop(); if (P.IsClosed(u)) continue; P.Closed[u] = true;
                int g = P.G(u); if (g > maxCm) return null;
                var c = F.Rep[u]; if (ok(c)) return c;
                int k = F.CellOf[u], i = k / Ny, j = k % Ny;
                for (int e = F.EStart[u]; e < F.EStart[u + 1]; e++)
                {
                    int v = F.ETo[e], d = F.EDir[e]; if (Shut(F, v, d, i, j, m) || P.IsClosed(v)) continue;
                    int ng = g + (d < 4 ? orth : diag);
                    if (ng < P.G(v)) { P.Set(v, ng, u); heap.Push(ng, v); }
                }
            }
            return null;
        }

        int PieceOfFine(Pt p) { var F = Fine(); int u = NodeAt(F, p); return u < 0 ? -1 : F.Piece[u]; }
        int PieceSizeFine(int piece) => piece >= 0 ? Fine().PieceSize[piece] : 0;
        Pt? NearestFreeInFine(Pt p, int piece)
        {
            var F = Fine(); Pt? best = null; long bd = long.MaxValue;
            for (int u = 0; u < F.Rep.Length; u++) { if (F.Piece[u] != piece) continue; long d = G.D2(F.Rep[u], p); if (d < bd) { bd = d; best = F.Rep[u]; } }
            return best;
        }

        /// <summary>Engine 0.49.0 (cell.listen.pass): would a body standing at q cut a passage? The free ground within `outerCm` of q, with the disc of
        /// `innerCm` round q taken out (nobody's centre comes nearer it), falls into more pieces that reach the edge of that ground than it did with q
        /// left open. Read on the walk raster (a grid without one: its cells). A pocket inside the ground is no passage and is not counted.</summary>
        public bool Cuts(Pt q, int innerCm, int outerCm) => RimPieces(q, innerCm, outerCm) > RimPieces(q, 0, outerCm);
        int RimPieces(Pt q, int innerCm, int outerCm)
        {
            bool fine = ffree != null; int c = fine ? FCell : Cell, ci = FloorDiv(q.X - X0, c), cj = FloorDiv(q.Y - Y0, c), r = outerCm / c + 1, w = 2 * r + 1;
            long in2 = (long)innerCm * innerCm, out2 = (long)outerCm * outerCm, rim2 = (long)(outerCm - c) * (outerCm - c);
            var seen = new bool[w * w]; var stack = new Stack<int>(); int pieces = 0;
            bool open(int a, int b, out long d2)
            {
                int i = ci + a - r, j = cj + b - r; var p = new Pt(X0 + i * c + c / 2, Y0 + j * c + c / 2); d2 = G.D2(p, q);
                if (d2 > out2 || d2 < in2 || i < 0 || j < 0) return false;
                return fine ? i < FNx && j < FNy && ffree![i * FNy + j] : FreeCell(i, j);
            }
            for (int a0 = 0; a0 < w; a0++) for (int b0 = 0; b0 < w; b0++)
                {
                    if (seen[a0 * w + b0] || !open(a0, b0, out _)) continue;
                    bool rim = false; seen[a0 * w + b0] = true; stack.Push(a0 * w + b0);
                    while (stack.Count > 0)
                    {
                        int k = stack.Pop(), a = k / w, b = k % w; open(a, b, out var d2); if (d2 >= rim2) rim = true;
                        for (int d = 0; d < 4; d++) { int na = a + DI[d], nb = b + DJ[d]; if (na < 0 || nb < 0 || na >= w || nb >= w || seen[na * w + nb] || !open(na, nb, out _)) continue; seen[na * w + nb] = true; stack.Push(na * w + nb); }
                    }
                    if (rim) pieces++;
                }
            return pieces;
        }

        /// <summary>Host and tool reads (never hashed): the walk's nodes and edges, for the zone-wide gap check (sim/cli/Gaps.cs) and the host's debug view.</summary>
        public int FineNodes => HasFine ? Fine().Rep.Length : 0;
        public int FineEdges => HasFine ? Fine().ETo.Length : 0;
    }
}
