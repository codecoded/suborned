// maxTravel (standards.levels.spacing[level].maxTravel): no point on a deck is more than the limit, walked, from a way down.
// A grid walk (8 neighbours) over the union of one level's touching deck pieces, round obstacles (halls), from every way down
// that lands on a piece of the group. `fillTravel` adds ways until the limit holds.
import type { V } from '../../plan/geom';
import type { Rect } from './geo';

export type Piece = { id: string; level: string; rect: Rect };
export type Way = { rect: Rect; serves: string[] };
export type Reach = { max: number; worst: V; far: number; stuck: number; farPts: V[] };

const SQ = Math.SQRT2;
const gapTo = (p: V, r: Rect) => Math.hypot(Math.max(0, r[0] - p[0], p[0] - r[1]), Math.max(0, r[2] - p[1], p[1] - r[3]));
const inside = (p: V, r: Rect, m = 0) => p[0] > r[0] + m && p[0] < r[1] - m && p[1] > r[2] + m && p[1] < r[3] - m;
const hit = (a: Rect, b: Rect) => a[0] < b[1] && b[0] < a[1] && a[2] < b[3] && b[2] < a[3];
const NB: [number, number, number][] = [[1, 0, 1], [-1, 0, 1], [0, 1, 1], [0, -1, 1], [1, 1, SQ], [1, -1, SQ], [-1, 1, SQ], [-1, -1, SQ]];

/** The walkable grid of one group of pieces, built once; `run` measures a set of ways over it, with some obstacles taken away. */
export class Walk {
  private nx: number; private ny: number; private x0: number; private y0: number;
  private base: Uint8Array; private real: Uint8Array; private owner: Int32Array; private blocked: Uint16Array; private cellsOf: Int32Array[];
  private idx: Map<string, number>;
  constructor(readonly pieces: Piece[], readonly obstacles: Rect[], readonly cell = 1) {
    this.x0 = Math.min(...pieces.map(p => p.rect[0])); this.y0 = Math.min(...pieces.map(p => p.rect[2]));
    const x1 = Math.max(...pieces.map(p => p.rect[1])), y1 = Math.max(...pieces.map(p => p.rect[3]));
    this.nx = Math.max(1, Math.ceil((x1 - this.x0) / cell)); this.ny = Math.max(1, Math.ceil((y1 - this.y0) / cell));
    const N = this.nx * this.ny, grow = -(0.3 + cell * 0.5); // touching pieces (<= 0.6 m apart) join: each grown by this much
    this.base = new Uint8Array(N); this.real = new Uint8Array(N); this.owner = new Int32Array(N).fill(-1); this.blocked = new Uint16Array(N);
    this.idx = new Map(pieces.map((p, i) => [p.id, i]));
    for (let i = 0; i < N; i++) {
      const p = this.at(i), k = pieces.findIndex(q => inside(p, q.rect, grow));
      if (k >= 0) { this.base[i] = 1; this.owner[i] = k; this.real[i] = pieces.some(q => inside(p, q.rect)) ? 1 : 0; }
    }
    this.cellsOf = obstacles.map(o => {
      const cs: number[] = [];
      for (let i = 0; i < N; i++) if (this.base[i] && inside(this.at(i), o)) { cs.push(i); this.blocked[i]!++; }
      return Int32Array.from(cs);
    });
  }
  private at(i: number): V { return [this.x0 + ((i % this.nx) + 0.5) * this.cell, this.y0 + (Math.floor(i / this.nx) + 0.5) * this.cell]; }

  /** Walking distance from `ways` to every real cell. `drop`: indices of obstacles that are not there. */
  run(ways: Way[], limit: number, drop: Set<number> = new Set()): Reach {
    const { nx, ny, cell } = this, N = nx * ny;
    for (const k of drop) for (const i of this.cellsOf[k]!) this.blocked[i]!--;
    try {
      const walk = (i: number) => this.base[i] === 1 && this.blocked[i] === 0;
      const dist = new Float64Array(N).fill(Infinity), heap: [number, number][] = [];
      const push = (d: number, i: number) => { heap.push([d, i]); let c = heap.length - 1; while (c > 0) { const p = (c - 1) >> 1; if (heap[p]![0] <= heap[c]![0]) break; [heap[p], heap[c]] = [heap[c]!, heap[p]!]; c = p; } };
      const pop = (): [number, number] => {
        const top = heap[0]!, last = heap.pop()!;
        if (heap.length) { heap[0] = last; let c = 0; for (;;) { const l = 2 * c + 1, r = l + 1; let m = c; if (l < heap.length && heap[l]![0] < heap[m]![0]) m = l; if (r < heap.length && heap[r]![0] < heap[m]![0]) m = r; if (m === c) break; [heap[m], heap[c]] = [heap[c]!, heap[m]!]; c = m; } }
        return top;
      };
      for (const w of ways) for (const sid of w.serves) {
        const k = this.idx.get(sid);
        if (k === undefined) continue;
        // the cells of the piece nearest the way (a way lands on a deck it stands beside): a local window first, the whole piece if that finds none
        const win = (a: number, b: number, c: number, d: number) => {
          let best = Infinity; const cand: number[] = [];
          for (let j = Math.max(0, c); j <= Math.min(ny - 1, d); j++) for (let i = Math.max(0, a); i <= Math.min(nx - 1, b); i++) {
            const n = j * nx + i;
            if (this.owner[n] !== k || !walk(n)) continue;
            const g = gapTo(this.at(n), w.rect);
            if (g < best - 1e-9) { best = g; cand.length = 0; cand.push(n); } else if (g <= best + cell * 0.75) cand.push(n);
          }
          return { best, cand };
        };
        const wx0 = Math.floor((w.rect[0] - this.x0) / cell) - 1, wx1 = Math.ceil((w.rect[1] - this.x0) / cell) + 1, wy0 = Math.floor((w.rect[2] - this.y0) / cell) - 1, wy1 = Math.ceil((w.rect[3] - this.y0) / cell) + 1;
        let r = win(wx0, wx1, wy0, wy1);
        if (r.best > cell) r = win(0, nx - 1, 0, ny - 1);
        for (const n of r.cand) if (gapTo(this.at(n), w.rect) <= r.best + cell * 0.75 && dist[n]! > 0) { dist[n] = 0; push(0, n); }
      }
      while (heap.length) {
        const [d, i] = pop();
        if (d > dist[i]!) continue;
        const ix = i % nx, iy = (i - ix) / nx;
        for (const [dx, dy, c] of NB) {
          const jx = ix + dx, jy = iy + dy;
          if (jx < 0 || jy < 0 || jx >= nx || jy >= ny) continue;
          const j = jy * nx + jx;
          if (!walk(j) || (dx && dy && !walk(iy * nx + jx) && !walk(jy * nx + ix))) continue;
          const nd = d + c * cell;
          if (nd < dist[j]!) { dist[j] = nd; push(nd, j); }
        }
      }
      let max = 0, worst: V = this.at(0), far = 0, stuck = 0; const farPts: V[] = [];
      for (let i = 0; i < N; i++) {
        if (!walk(i) || !this.real[i]) continue;
        const d = dist[i]!;
        if (!Number.isFinite(d)) { if (!stuck) worst = this.at(i); stuck++; max = Infinity; continue; }
        if (d > limit) { far++; farPts.push(this.at(i)); }
        if (d > max && !stuck) { max = d; worst = this.at(i); }
      }
      return { max, worst, far, stuck, farPts };
    } finally { for (const k of drop) for (const i of this.cellsOf[k]!) this.blocked[i]!++; }
  }
}

/** One-off measure: the group's reach from `ways` round `obstacles`. */
export const reach = (pieces: Piece[], ways: Way[], obstacles: Rect[], limit: number, cell = 1): Reach =>
  pieces.length ? new Walk(pieces, obstacles, cell).run(ways, limit) : { max: 0, worst: [0, 0], far: 0, stuck: 0, farPts: [] };

/** Add ways (from `candidates`, nearest the middle of the far points first) until every point is within `limit` or nothing helps.
 *  A way stands where it is put: it replaces any obstacle it overlaps (the returned `obstacles` are those that remain). */
export function fillTravel(o: { pieces: Piece[]; ways: Way[]; obstacles: Rect[]; limit: number; cell?: number; candidates: (centre: V, taken: Rect[], tier: 0 | 1) => Rect[]; serves: (r: Rect) => string[] }): { added: Rect[]; reach: Reach; obstacles: Rect[] } {
  const w = new Walk(o.pieces, o.obstacles, o.cell), added: Rect[] = [], ways = [...o.ways], score = (r: Reach) => r.far + r.stuck * 1000, gone = new Set<number>();
  let cur = w.run(ways, o.limit, gone);
  for (let it = 0; it < 80 && (cur.max > o.limit || cur.stuck); it++) {
    // aim at the middle of the out-of-reach points near the worst one: a way there covers the most of them
    const near = cur.farPts.filter(p => Math.hypot(p[0] - cur.worst[0], p[1] - cur.worst[1]) <= o.limit), n = Math.max(1, near.length);
    const centre: V = near.length ? [near.reduce((a, p) => a + p[0], 0) / n, near.reduce((a, p) => a + p[1], 0) / n] : cur.worst;
    let best: { r: Rect; got: Reach; drop: Set<number> } | null = null;
    for (const tier of [0, 1] as const) { // tier 0: spots that displace nothing; tier 1: any spot
      for (const r of o.candidates(centre, added, tier).slice(0, 12)) {
        const drop = new Set(gone); o.obstacles.forEach((q, k) => { if (hit(q, r)) drop.add(k); });
        const got = w.run([...ways, { rect: r, serves: o.serves(r) }], o.limit, drop);
        if (score(got) < score(cur) && (!best || score(got) < score(best.got))) best = { r, got, drop };
      }
      if (best) break;
    }
    if (!best) break;
    added.push(best.r); ways.push({ rect: best.r, serves: o.serves(best.r) }); cur = best.got; best.drop.forEach(k => gone.add(k));
  }
  return { added, reach: cur, obstacles: o.obstacles.filter((_, k) => !gone.has(k)) };
}
