// The drawn world as a plan-frame raster (brief 055, Splice): for every cell of the site window, the ground the player sees and the solid it stands on or against.
// Cell 0.25 m. Each cell holds the visible ground top G (slab, road, kerb, bay) and the height intervals [lo, hi] that kit pieces and masses fill above it,
// each tagged with its owner (the element or piece id). `finish()` reads each column once: the floor a walker stands on (the stack of surfaces from the ground up,
// a gap of a riser or a tread's underside bridged), what is overhead, and which cells a walker can reach by steps from the open ground.
import { inPoly, type V } from '../plan/geom';

export const CELL = 0.25;
/** A walker climbs from one cell to the next by at most this (a kerb, a stair tread, a threshold); more is a wall to it. */
export const STEP = 0.35;
/** A surface this far above the one under it still carries a walker (a riser's gap, a tread over nothing). */
export const BRIDGE = 0.5;
/** Head room a walker needs over the floor. */
export const HEAD = 1.8;
/** A floor within this of the ground layer is the ground. */
export const FLUSH = 0.05;

export type Column = {
  /** Visible ground top (slab, road, kerb) before any piece. */
  ground: number;
  /** Top of the surface stack a walker stands on (== ground when nothing stands there). */
  top: number;
  /** top - ground. */
  rise: number;
  /** Owner of the interval that makes `top` (the ground layer's id when nothing stands there). */
  owner: string;
  /** Lowest underside above `top` (Infinity when open sky). */
  roof: number;
  roofOwner: string;
  /** A walker reaches this floor by steps from the open ground, with head room. */
  walkable: boolean;
};

export class Raster {
  readonly nx: number; readonly ny: number;
  readonly ground: Float32Array;
  readonly groundOwner: Uint16Array;
  readonly iv: (number[] | undefined)[];
  readonly owners: string[] = []; private ownerIx = new Map<string, number>();
  // filled by finish()
  top!: Float32Array; roof!: Float32Array; topOwner!: Uint16Array; roofOwner!: Uint16Array; walk!: Uint8Array;
  constructor(readonly x0: number, readonly y0: number, readonly x1: number, readonly y1: number, readonly cell = CELL) {
    this.nx = Math.ceil((x1 - x0) / cell); this.ny = Math.ceil((y1 - y0) / cell);
    this.ground = new Float32Array(this.nx * this.ny); this.groundOwner = new Uint16Array(this.nx * this.ny);
    this.iv = new Array(this.nx * this.ny);
  }
  owner(id: string): number { let i = this.ownerIx.get(id); if (i === undefined) { i = this.owners.length; this.owners.push(id); this.ownerIx.set(id, i); } return i; }
  idx(i: number, j: number) { return i * this.ny + j; }
  inside(i: number, j: number) { return i >= 0 && j >= 0 && i < this.nx && j < this.ny; }
  cellOf(p: V): [number, number] { return [Math.floor((p[0] - this.x0) / this.cell), Math.floor((p[1] - this.y0) / this.cell)]; }
  centre(i: number, j: number): V { return [this.x0 + (i + 0.5) * this.cell, this.y0 + (j + 0.5) * this.cell]; }
  /** Cells whose centres a box covers (clipped to the raster). */
  range(bx0: number, by0: number, bx1: number, by1: number): [number, number, number, number] {
    return [Math.max(0, Math.floor((bx0 - this.x0) / this.cell)), Math.max(0, Math.floor((by0 - this.y0) / this.cell)), Math.min(this.nx - 1, Math.floor((bx1 - this.x0) / this.cell)), Math.min(this.ny - 1, Math.floor((by1 - this.y0) / this.cell))];
  }
  add(i: number, j: number, lo: number, hi: number, owner: number) {
    const k = this.idx(i, j); (this.iv[k] ??= []).push(lo, hi, owner);
  }
  /** Fill a polygon with a solid [lo, hi]. */
  prism(ring: V[], lo: number, hi: number, owner: string) {
    const o = this.owner(owner), xs = ring.map(p => p[0]), ys = ring.map(p => p[1]);
    const [i0, j0, i1, j1] = this.range(Math.min(...xs), Math.min(...ys), Math.max(...xs), Math.max(...ys));
    for (let i = i0; i <= i1; i++) for (let j = j0; j <= j1; j++) if (inPoly(this.centre(i, j), ring)) this.add(i, j, lo, hi, o);
  }

  /** Read every column once and find what a walker can reach. Call after all stamping. */
  finish() {
    const n = this.nx * this.ny;
    this.top = new Float32Array(n); this.roof = new Float32Array(n).fill(Infinity); this.topOwner = new Uint16Array(n); this.roofOwner = new Uint16Array(n); this.walk = new Uint8Array(n);
    for (let k = 0; k < n; k++) {
      let top = this.ground[k]!, own = this.groundOwner[k]!, roof = Infinity, rown = 0;
      const list = this.iv[k];
      if (list) {
        const items: [number, number, number][] = [];
        for (let m = 0; m < list.length; m += 3) items.push([list[m]!, list[m + 1]!, list[m + 2]!]);
        items.sort((a, b) => a[0] - b[0] || a[1] - b[1]);
        for (const [lo, hi, o] of items) {
          if (hi <= top) continue;
          if (lo <= top + BRIDGE) { top = hi; own = o; }
          else { roof = lo; rown = o; break; }
        }
      }
      this.top[k] = top; this.topOwner[k] = own; this.roof[k] = roof; this.roofOwner[k] = rown;
    }
    // reach by steps from the open ground (a cell whose floor is the ground layer and has head room)
    const queue: number[] = [];
    for (let k = 0; k < n; k++) if (this.top[k]! - this.ground[k]! <= FLUSH && this.roof[k]! - this.top[k]! >= HEAD) { this.walk[k] = 1; queue.push(k); }
    for (let q = 0; q < queue.length; q++) {
      const k = queue[q]!, i = Math.floor(k / this.ny), j = k % this.ny;
      for (let di = -1; di <= 1; di++) for (let dj = -1; dj <= 1; dj++) {
        if (!di && !dj) continue; const a = i + di, b = j + dj; if (!this.inside(a, b)) continue;
        const nk = this.idx(a, b); if (this.walk[nk]) continue;
        if (Math.abs(this.top[nk]! - this.top[k]!) > STEP || this.roof[nk]! - this.top[nk]! < HEAD) continue;
        this.walk[nk] = 1; queue.push(nk);
      }
    }
  }

  /** What stands at a cell (after finish()). */
  column(i: number, j: number): Column {
    const k = this.idx(i, j), g = this.ground[k]!, t = this.top[k]!;
    return { ground: g, top: t, rise: t - g, owner: this.owners[this.topOwner[k]!] ?? 'ground', roof: this.roof[k]!, roofOwner: this.owners[this.roofOwner[k]!] ?? '', walkable: this.walk[k] === 1 };
  }
  /** The owner of the tallest solid interval over the cell overlapping [z0, z1] by at least minOverlap (null: nothing). Ground counts as a solid up to its top. */
  inBody(i: number, j: number, z0: number, z1: number, minOverlap = 0.05): { owner: string; hi: number } | null {
    const k = this.idx(i, j), list = this.iv[k];
    if (!list) return null;
    let best: { owner: string; hi: number } | null = null, bo = 0;
    for (let m = 0; m < list.length; m += 3) {
      const lo = list[m]!, hi = list[m + 1]!, ov = Math.min(hi, z1) - Math.max(lo, z0);
      if (ov >= minOverlap && ov > bo) { bo = ov; best = { owner: this.owners[list[m + 2]!]!, hi }; }
    }
    return best;
  }
  /** The owner of any solid dz above the ground layer of the cell (null: open air there): eye and chest heights are above a person's feet, and the feet stand on the ground. */
  solidRel(i: number, j: number, dz: number): string | null { return this.solidAt(i, j, this.ground[this.idx(i, j)]! + dz); }
  /** The owner of any solid at height z over the cell (null: open air there). */
  solidAt(i: number, j: number, z: number): string | null {
    const k = this.idx(i, j), list = this.iv[k];
    if (this.ground[k]! > z) return this.owners[this.groundOwner[k]!] ?? 'ground';
    if (!list) return null;
    for (let m = 0; m < list.length; m += 3) if (list[m]! <= z && list[m + 1]! >= z) return this.owners[list[m + 2]!]!;
    return null;
  }
}
