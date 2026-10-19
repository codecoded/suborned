// The walk grid as the C# engine walks it (sim/Suborned.Sim/Grid.cs, Geo.cs), ported line for line so a route here is the route there: A* over the site's free
// cells, 8 neighbours, no corner cutting, integer costs, ties to the lower cell index, then string-pulled to an any-angle polyline. Pulse's round reference
// (tools/routines/rounds.ts) walks on it to measure the legs it bakes; Keel's engine walks its own copy of the same grid. Integer centimetres, block frame
// (x south, y east). Nothing here is the engine: it is read-only geometry for the routine tools.
import { readFileSync } from 'node:fs';
import { join } from 'node:path';

export type Pt = [number, number];
const root = join(import.meta.dir, '..', '..');
export const SITE_FILE = 'sim/data/handover.site.json';

const isqrt = (n: number) => Math.floor(Math.sqrt(n)) - (Math.floor(Math.sqrt(n)) ** 2 > n ? 1 : 0); // exact for n < 2^52
export const d2 = (a: Pt, b: Pt) => (a[0] - b[0]) ** 2 + (a[1] - b[1]) ** 2;
export const dist = (a: Pt, b: Pt) => isqrt(d2(a, b)); // cm, rounded down (G.Dist)
const trunc = (v: number) => (v < 0 ? Math.ceil(v) : Math.floor(v));
/** G.Lerp: a + (b - a) * num / den, each axis truncated toward zero. */
export const lerp = (a: Pt, b: Pt, num: number, den: number): Pt => (den <= 0 ? b : [a[0] + trunc(((b[0] - a[0]) * num) / den), a[1] + trunc(((b[1] - a[1]) * num) / den)]);

const DI = [1, -1, 0, 0, 1, 1, -1, -1], DJ = [0, 0, 1, -1, 1, -1, 1, -1];

export class Grid {
  readonly x0: number; readonly y0: number; readonly cell: number; readonly nx: number; readonly ny: number; private free: Uint8Array;
  constructor(g: { x0: number; y0: number; cell: number; nx: number; ny: number; rows: string[] }) {
    this.x0 = g.x0; this.y0 = g.y0; this.cell = g.cell; this.nx = g.nx; this.ny = g.ny;
    this.free = new Uint8Array(g.nx * g.ny);
    for (let i = 0; i < g.nx; i++) for (let j = 0; j < g.ny; j++) this.free[i * g.ny + j] = g.rows[i]![j] === '.' ? 1 : 0;
  }
  static fromSite(file = SITE_FILE): Grid { return new Grid(JSON.parse(readFileSync(join(root, file), 'utf8')).grid); }
  freeCell(i: number, j: number) { return i >= 0 && j >= 0 && i < this.nx && j < this.ny && this.free[i * this.ny + j] === 1; }
  ci(x: number) { return Math.floor((x - this.x0) / this.cell); }
  cj(y: number) { return Math.floor((y - this.y0) / this.cell); }
  isFree(p: Pt) { return this.freeCell(this.ci(p[0]), this.cj(p[1])); }
  centre(i: number, j: number): Pt { return [this.x0 + i * this.cell + Math.floor(this.cell / 2), this.y0 + j * this.cell + Math.floor(this.cell / 2)]; }
  /** The nearest free cell centre to p (ties: the first found, scanning i then j), within 12 rings. */
  nearestFree(p: Pt): Pt | undefined {
    if (this.isFree(p)) return p;
    const ci = this.ci(p[0]), cj = this.cj(p[1]); let best: Pt | undefined, bd = Infinity;
    for (let r = 1; r <= 12; r++) {
      for (let i = ci - r; i <= ci + r; i++) for (let j = cj - r; j <= cj + r; j++) {
        if (!this.freeCell(i, j)) continue;
        const c = this.centre(i, j), d = d2(c, p);
        if (d < bd) { bd = d; best = c; }
      }
      if (best) return best;
    }
    return undefined;
  }
  lineFree(a: Pt, b: Pt) {
    const n = Math.max(1, Math.floor(dist(a, b) / 10));
    for (let k = 0; k <= n; k++) if (!this.isFree(lerp(a, b, k, n))) return false;
    return true;
  }
  /** The shortest walk a → b as an any-angle polyline (cm), or undefined; b must stand on free ground (Grid.Route). */
  route(a: Pt, b: Pt): Pt[] | undefined {
    const s0 = this.nearestFree(a); if (!s0 || !this.isFree(b)) return undefined;
    const si = this.ci(s0[0]), sj = this.cj(s0[1]), ti = this.ci(b[0]), tj = this.cj(b[1]);
    const n = this.nx * this.ny, s = si * this.ny + sj, t = ti * this.ny + tj;
    const gs = new Int32Array(n).fill(2147483647), prev = new Int32Array(n).fill(-1), closed = new Uint8Array(n);
    const orth = this.cell, diag = Math.floor((this.cell * 1414) / 1000);
    const h = (i: number, j: number) => { const dx = Math.abs(i - ti), dy = Math.abs(j - tj), mn = Math.min(dx, dy), mx = Math.max(dx, dy); return (mx - mn) * this.cell + mn * diag; };
    // a binary min-heap of (priority << 32 | cell): the lower cell index first on equal priority, as the engine's
    const heap: number[] = []; const key = (pri: number, c: number) => pri * 4294967296 + c;
    const push = (pri: number, c: number) => { heap.push(key(pri, c)); let k = heap.length - 1; while (k > 0) { const p = (k - 1) >> 1; if (heap[p]! <= heap[k]!) break; [heap[p], heap[k]] = [heap[k]!, heap[p]!]; k = p; } };
    const pop = () => {
      const top = heap[0]!, last = heap.pop()!;
      if (heap.length > 0) { heap[0] = last; let c = 0; for (;;) { const l = 2 * c + 1, r = l + 1; let m = c; if (l < heap.length && heap[l]! < heap[m]!) m = l; if (r < heap.length && heap[r]! < heap[m]!) m = r; if (m === c) break; [heap[m], heap[c]] = [heap[c]!, heap[m]!]; c = m; } }
      return top % 4294967296;
    };
    gs[s] = 0; push(h(si, sj), s);
    while (heap.length > 0) {
      const k = pop(); if (closed[k]) continue; closed[k] = 1; if (k === t) break;
      const i = Math.floor(k / this.ny), j = k % this.ny;
      for (let d = 0; d < 8; d++) {
        const ni = i + DI[d]!, nj = j + DJ[d]!;
        if (!this.freeCell(ni, nj)) continue;
        if (d >= 4 && (!this.freeCell(i + DI[d]!, j) || !this.freeCell(i, j + DJ[d]!))) continue;
        const nk = ni * this.ny + nj; if (closed[nk]) continue;
        const ng = gs[k]! + (d < 4 ? orth : diag);
        if (ng < gs[nk]!) { gs[nk] = ng; prev[nk] = k; push(ng + h(ni, nj), nk); }
      }
    }
    if (gs[t] === 2147483647) return undefined;
    const cells: Pt[] = [];
    for (let k = t; k !== s; k = prev[k]!) cells.push(this.centre(Math.floor(k / this.ny), k % this.ny));
    cells.reverse();
    const raw: Pt[] = [a];
    if (s0[0] !== a[0] || s0[1] !== a[1]) raw.push(s0);
    raw.push(...cells);
    const last = raw[raw.length - 1]!;
    if (last[0] !== b[0] || last[1] !== b[1]) { if (cells.length > 0) raw[raw.length - 1] = b; else raw.push(b); }
    const path: Pt[] = [raw[0]!]; let at = 0;
    while (at < raw.length - 1) { let to = raw.length - 1; while (to > at + 1 && !this.lineFree(raw[at]!, raw[to]!)) to--; path.push(raw[to]!); at = to; }
    return path;
  }
}

/** A polyline walked by distance, in mm (Path): At(mm) is the point that far along; heading the segment's vector. */
export class Path {
  readonly cum: number[] = [0]; readonly length: number;
  constructor(readonly points: Pt[]) {
    for (let i = 1; i < points.length; i++) this.cum.push(this.cum[i - 1]! + isqrt(d2(points[i - 1]!, points[i]!) * 100));
    this.length = this.cum[points.length - 1]!;
  }
  get end(): Pt { return this.points[this.points.length - 1]!; }
  at(mm: number): Pt {
    if (mm <= 0) return this.points[0]!;
    if (mm >= this.length) return this.end;
    let i = 1; while (this.cum[i]! < mm) i++;
    return lerp(this.points[i - 1]!, this.points[i]!, mm - this.cum[i - 1]!, this.cum[i]! - this.cum[i - 1]!);
  }
  /** The segment's vector at mm. A walk repeats its end points (leg keeps both ends), so a zero-length segment is skipped: the next one that goes somewhere, else the last one that did.
   *  (Path.Heading in the engine does not skip: at mm 0 of a leg it answers (0,0). Keel: skip them.) */
  heading(mm: number): Pt {
    const n = this.points.length; if (n < 2) return [0, 0];
    let i = 1; while (i < n - 1 && this.cum[i]! < mm) i++;
    const seg = (k: number): Pt => [this.points[k]![0] - this.points[k - 1]![0], this.points[k]![1] - this.points[k - 1]![1]];
    const still = (k: number) => seg(k)[0] === 0 && seg(k)[1] === 0;
    let j = i; while (j < n - 1 && still(j)) j++;
    if (still(j)) { j = i; while (j > 1 && still(j)) j--; }
    return seg(j);
  }
}
/** The grid walk a → b with both ends kept (Crowd.Leg): a, the route between the nearest free cells, b. */
export function leg(g: Grid, a: Pt, b: Pt): Path {
  const fa = g.nearestFree(a) ?? a, fb = g.nearestFree(b) ?? b, pts: Pt[] = [a], r = g.route(fa, fb);
  if (r) pts.push(...r); else pts.push(fb);
  pts.push(b);
  return new Path(pts);
}
