// Where a street item stands, for the SF rules: its footprint (a tree in its pit) and the points it is tested at.
import type { StreetItem } from '../../contracts/world/world.schema';
import type { V } from './geom';
import { footprintOf, samples9 } from './street-zones';
import type { WCtx } from './world-ctx';

export { FLAT, roadAt, indexes, walkBlock, tramBlock } from './street-zones';
export type Spot = { s: StreetItem; ring: V[]; pts: V[]; ground: boolean; kit: boolean };
export const WATCHERS = ['pole-camera', 'mast-camera', 'canopy-camera', 'civic-robot-dock'];

export const spotsOf = (c: WCtx): Spot[] => c.w.street.map(s => {
  const ring = footprintOf(c.S, s.kind, s.variant, s.at, s.size, s.yaw);
  return { s, ring, pts: samples9(ring), ground: s.z < 1, kit: s.edge.startsWith('stop/') };
});

/** A spatial hash of anything with a position: near(p, r) returns the cells' contents within r. */
export class Grid<T> {
  private m = new Map<string, T[]>();
  constructor(private cell: number, private at: (t: T) => V) {}
  add(t: T) { const [x, y] = this.at(t), k = `${Math.floor(x / this.cell)},${Math.floor(y / this.cell)}`; (this.m.get(k) ?? this.m.set(k, []).get(k)!).push(t); }
  remove(t: T) { const [x, y] = this.at(t), l = this.m.get(`${Math.floor(x / this.cell)},${Math.floor(y / this.cell)}`), i = l ? l.indexOf(t) : -1; if (l && i >= 0) l.splice(i, 1); }
  near(p: V, r: number): T[] {
    const out: T[] = [], c = this.cell;
    for (let i = Math.floor((p[0] - r) / c); i <= Math.floor((p[0] + r) / c); i++) for (let j = Math.floor((p[1] - r) / c); j <= Math.floor((p[1] + r) / c); j++) out.push(...(this.m.get(`${i},${j}`) ?? []));
    return out;
  }
}
