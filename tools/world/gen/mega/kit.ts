// Helpers shared by the megastructure builders.
import { type V, lineDist } from '../../../plan/geom';
import type { Mass, Prim } from '../../../../contracts/world/world.schema';
import { type Ctx, r3, r4, rv } from '../common';
import { type Rect, bbox, rectPts } from '../geo';
import type { DeckRec, Host, HostEdge } from '../types';

export const INSET = 0.3; // megastructure bodies sit this far inside their rects so side faces never coincide with a neighbour's

export const zOf = (ctx: Ctx, level: string, m?: { levels: { level: string; z?: number }[] }): number => {
  const l = ctx.L.list.find((x: any) => x.id === level);
  return l ? l.z : (m?.levels.find(x => x.level === level)?.z ?? 0);
};

export const rectMass = (id: string, surface: string, r: Rect, base: number, top: number): Mass =>
  ({ id, surface, ring: rectPts(r).map(rv), base: r3(base), height: r3(top - base) });

export const box = (id: string, surface: string, c: V, size: [number, number, number], base: number, yaw = 0): Prim =>
  ({ id, shape: 'box', surface, c: rv(c), base: r3(base), size: [r3(size[0]), r3(size[1]), r3(size[2])], yaw: r4(yaw) });
export const prim = (id: string, shape: Prim['shape'], surface: string, c: V, size: [number, number, number], base: number, yaw = 0): Prim =>
  ({ id, shape, surface, c: rv(c), base: r3(base), size: [r3(size[0]), r3(size[1]), r3(size[2])], yaw: r4(yaw) });

export function deckRec(ctx: Ctx, host: string, id: string, level: string, rect: Rect, columns: V[], thickness: number, m?: any): DeckRec {
  return { id, host, level, rect, surface: 'deck', columnSurface: 'column', ring: rectPts(rect).map(rv), columns: columns.map(rv), thickness, columnSize: columns.length ? ctx.L.column.size : 0, ground: ctx.pav, group: id };
}

/** Which side of a footprint faces the nearest foot path or road: axis of the face and the direction into the footprint along it. */
export function faceOf(ctx: Ctx, f: Rect): { axis: 0 | 1; dir: 1 | -1 } {
  const mid: [V, 0 | 1, 1 | -1][] = [[[(f[0] + f[1]) / 2, f[2]], 1, 1], [[(f[0] + f[1]) / 2, f[3]], 1, -1], [[f[0], (f[2] + f[3]) / 2], 0, 1], [[f[1], (f[2] + f[3]) / 2], 0, -1]];
  let best = Infinity, out = mid[0]!;
  for (const x of mid) {
    const g = Math.min(...ctx.plan.foot.map(o => lineDist(x[0], o.pts) - o.w / 2), ...ctx.d.roads.filter(r => !r.gyratory).map(r => lineDist(x[0], r.line) - r.w / 2));
    if (g < best) { best = g; out = x; }
  }
  return { axis: out[1], dir: out[2] };
}

/** A slice of rect r measured from its face: depths d0..d1 along `axis` going `dir`. */
export function slice(r: Rect, axis: 0 | 1, dir: 1 | -1, d0: number, d1: number): Rect {
  const [lo, hi] = axis === 1 ? [r[2], r[3]] : [r[0], r[1]], a = dir > 0 ? lo + d0 : hi - d1, b = dir > 0 ? lo + d1 : hi - d0;
  return axis === 1 ? [r[0], r[1], a, b] : [a, b, r[2], r[3]];
}

/** A host edge along one side of a rect, facing outward on that side (CCW order). `side`: 'x0' | 'x1' | 'y0' | 'y1'. */
export function sideEdge(r: Rect, side: 'x0' | 'x1' | 'y0' | 'y1', kind: string, street: string): HostEdge {
  const [x0, x1, y0, y1] = r;
  const [a, b, n]: [V, V, V] = side === 'y0' ? [[x0, y0], [x1, y0], [0, -1]] : side === 'x1' ? [[x1, y0], [x1, y1], [1, 0]] : side === 'y1' ? [[x1, y1], [x0, y1], [0, 1]] : [[x0, y1], [x0, y0], [-1, 0]];
  return { a, b, n, kind, street, cornerA: false, cornerB: false };
}
/** The side of rect r facing outward along (axis, -dir): the face itself. */
export const faceSide = (axis: 0 | 1, dir: 1 | -1): 'x0' | 'x1' | 'y0' | 'y1' => (axis === 1 ? (dir > 0 ? 'y0' : 'y1') : dir > 0 ? 'x0' : 'x1');

export const host = (id: string, level: string, z: number, area: string, mix: Record<string, number> | undefined, edges: HostEdge[], pavement: number, free = false): Host =>
  ({ id, level, z: r3(z), area, mix, edges, free, pavement });
export const bboxRect = (p: V[]): Rect => bbox(p);

/** Ids of the deck pieces a core's rect lands on: those within `tol` metres (a core serves the decks it stands beside). */
export function servedBy(rect: Rect, pieces: { id: string; rect: Rect }[], tol = 9): string[] {
  return pieces.filter(p => Math.hypot(Math.max(0, rect[0] - p.rect[1], p.rect[0] - rect[1]), Math.max(0, rect[2] - p.rect[3], p.rect[2] - rect[3])) <= tol).map(p => p.id);
}
