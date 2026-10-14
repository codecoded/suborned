// The civic block's dressing (civic-block.json `dressing`, brief 008) as export masses: the pieces on the block's two buildings, parasols, lamps, bollards,
// planter runs, cypresses, topiaries and the paving bands. Every placement is read from the block (positions, yaws, the bed a tree stands in) and from
// contracts/world/civic-look.json; nothing is chosen here. A block without `dressing` is left as it is.
import type { KitAsset } from '../../../contracts/world/kit.schema';
import type { Mass } from '../../../contracts/world/world.schema';
import type { Civic } from '../../plan/civic';
import type { V } from '../../plan/geom';
import { adoptedBy } from '../gen/kitrefs';
import { r3, r4 } from '../gen/common';
import { look } from './look';

type At = { id: string; at: V; yawDeg?: number; on?: string };
export type Dressing = {
  pieces: { piece: string; at: V; yawDeg: number; on?: string[] }[];
  parasols: { piece: string; on: { table: string; at: V }[] };
  lamps: { piece: string; at: (At & { yawDeg: number; ring: boolean })[] };
  bollards: { piece: string; at: At[] };
  planterRuns: { runs: { id: string; yawDeg: number; modules: { piece: string; at: V }[] }[] };
  cypress: { piece: string; at: (At & { yawDeg: number })[] };
  topiary: { piece: string; at: At[] };
  crossings: { at: { id: string; at: V; inward: V; plot: string; road: string; partner: { id?: string; edge?: string; kerb?: V } }[]; none: unknown[] };
  keepClear: ({ id: string; bars: string[]; reason: string } & ({ polygon: V[] } | { ring: { centre: V; r0: number; r1: number } }))[];
};
export const dressingOf = (civ: Civic): Dressing | undefined => civ.dressing as Dressing | undefined;

const rv = (p: V): V => [r3(p[0]), r3(p[1])];
/** Block yaw (degrees from +x toward +y, the piece's Blender +X) → plan yaw of the kit piece (kit +X turns to it; its +Z front faces yaw + 90°): block x runs south, y east, plan = (-130 + y, -230 - x). */
export const planYaw = (deg: number): number => { const y = (deg * Math.PI) / 180 + Math.PI / 2; return r4(Math.atan2(Math.sin(y), Math.cos(y))); };

/** A quarter ring of inlay (InlayArc_R<n>_W<w>: outer radius = its size, width w/10 m, pivot at the arc centre, sweeping out of the pivot along kit -x and -z): the sector itself, not its box. */
function arcRing(a: KitAsset, k: { scale: number[]; yaw?: number; at: V }): V[] | undefined {
  const w = /^InlayArc_R\d+_W(\d+)$/.exec(a.id)?.[1]; if (!w) return undefined;
  const R1 = a.size[0], R0 = R1 - Number(w) / 10, y = k.yaw ?? 0, u: V = [Math.cos(y), Math.sin(y)], v: V = [-Math.sin(y), Math.cos(y)], pt = (r: number, t: number): V => [k.at[0] - r * (Math.cos(t) * u[0] + Math.sin(t) * v[0]), k.at[1] - r * (Math.cos(t) * u[1] + Math.sin(t) * v[1])];
  const t = Array.from({ length: 9 }, (_, i) => (i * Math.PI) / 16);
  return [...t.map(x => pt(R1, x)), ...t.reverse().map(x => pt(R0, x))];
}

/** A kit piece's footprint at its scale (turned by its yaw, about its footprint centre): kit x → plan along yaw, kit z → across (front = yaw + 90°); the footprint centre
 *  sits at the pivot + pivotOffset. */
export function footOf(a: KitAsset, k: { scale: number[]; yaw?: number; at: V }): V[] {
  const y = k.yaw ?? 0, cs = Math.cos(y), sn = Math.sin(y), off = a.pivotOffset ?? [0, 0], sx = k.scale[0]!, sz = k.scale[2] ?? sx, hw = (a.size[0] * sx) / 2, hd = (a.size[2] * sz) / 2;
  return ([[-hw, -hd], [hw, -hd], [hw, hd], [-hw, hd]] as V[]).map(([x, z]) => [k.at[0] + (x + off[0] * sx) * cs - (z + off[1] * sz) * sn, k.at[1] + (x + off[0] * sx) * sn + (z + off[1] * sz) * cs] as V);
}

const bbox = (rings: V[][]): V[] => {
  const xs = rings.flat().map(p => p[0]), ys = rings.flat().map(p => p[1]), x0 = Math.min(...xs), x1 = Math.max(...xs), y0 = Math.min(...ys), y1 = Math.max(...ys);
  return [[x0, y0], [x1, y0], [x1, y1], [x0, y1]];
};

/** `masses` (the block's own civic list, kit refs already set) with the dressing applied. `pav` = pavement top; `toPlan` = block frame → plan. */
export function dressCivic(civ: Civic, toPlan: (p: V) => V, masses: Mass[], pav: number): Mass[] {
  const dr = dressingOf(civ);
  if (!dr) return masses;
  let out = masses.slice();
  const fade = new Set(look.fade);
  /** One kit mass: the piece at `at` (block frame) turned by the block yaw, standing on `base`. */
  const put = (id: string, name: string, at: V, yawDeg: number, base: number, o: { scale?: [number, number, number]; ring?: V[]; height?: number } = {}): Mass | undefined => {
    const a = adoptedBy(name); if (!a) return undefined; // a piece the kit has not adopted is placed nowhere; rule CD-2 names it
    const kit = { id: name, scale: o.scale ?? [1, 1, 1] as [number, number, number], yaw: planYaw(yawDeg), at: rv(toPlan(at)), base: r3(base) };
    return { id, surface: 'trim', ring: (o.ring ?? arcRing(a, kit) ?? footOf(a, kit)).map(rv), base: r3(base), height: o.height ?? r3(a.size[1] * kit.scale[1]), source: 'kit', kit, ...(fade.has(name) ? { fade: true as const } : {}) };
  };
  const mass = (id: string) => out.find(m => m.id === id);

  // the block's pieces: on the structures they stand for (the structures stay logical solids in the block, the piece is what is drawn), or as a building's ground storey
  for (const p of dr.pieces) {
    const id = look.place[p.piece]; if (!id) throw new Error(`civic dressing: ${p.piece} has no mass id in civic-look.json place`);
    const g = look.ground[p.piece];
    if (g) {
      const b = mass(`civic/${g.building}`); if (!b) throw new Error(`civic dressing: ${p.piece} stands under ${g.building}, which the block lacks`);
      const ground = put(id, p.piece, p.at, p.yawDeg, b.base, { ring: b.ring, height: g.top }); if (!ground) continue;
      out.splice(out.indexOf(b), 1, { ...b, base: r3(b.base + g.top), height: r3(b.height - g.top), groundBy: id }, ground);
      continue;
    }
    const on = (p.on ?? []).map(s => mass(`civic/${s}`) ?? (() => { throw new Error(`civic dressing: ${p.piece} stands on ${s}, which the block lacks`); })());
    const placed = put(id, p.piece, p.at, p.yawDeg, pav, { ring: bbox(on.map(m => m.ring)) }); if (!placed) continue; // the piece stands on the pavement: the official canopy's mass hangs at 4.23
    const at = out.indexOf(on[0]!);
    out = out.filter(m => !on.includes(m));
    out.splice(Math.min(at, out.length), 0, placed);
  }

  const add: Mass[] = [], keep = (m: Mass | undefined) => { if (m) add.push(m); };
  for (const s of dr.parasols.on) keep(put(`civic/${s.table}/parasol`, dr.parasols.piece, s.at, 0, pav));
  for (const l of dr.lamps.at) keep(put(`civic/${l.id}`, dr.lamps.piece, l.at, l.yawDeg, pav));
  for (const b of dr.bollards.at) keep(put(`civic/${b.id}`, dr.bollards.piece, b.at, 0, pav));
  for (const run of dr.planterRuns.runs) run.modules.forEach((m, i) => keep(put(`civic/${run.id}/${i}`, m.piece, m.at, run.yawDeg, pav)));
  for (const c of dr.cypress.at) { // planted in its bed: the bed's top is the base
    const bed = c.on ? mass(`civic/${c.on}`) : undefined;
    if (c.on && !bed) throw new Error(`civic dressing: ${c.id} stands in ${c.on}, which the block lacks`);
    keep(put(`civic/${c.id}`, dr.cypress.piece, c.at, c.yawDeg, bed ? bed.base + bed.height : pav));
  }
  for (const t of dr.topiary.at) keep(put(`civic/${t.id}`, dr.topiary.piece, t.at, 0, pav));
  for (const b of look.bands.pieces) keep(put(`civic/band/${b.id}`, b.piece, b.at, b.yawDeg, pav, { scale: [b.scaleX ?? 1, 1, 1] }));
  return [...out, ...add].map(m => (m.kit && fade.has(m.kit.id) && !m.fade ? { ...m, fade: true as const } : m)); // the shelter's piers take the fade the shelter does
}

/** The keep-clear kind of each piece the dressing places (dressing.keepClear `bars`): the kit id → planter | lamp | bollard | tree | parasol | band. */
export function placementKinds(dr: Dressing): Map<string, 'planter' | 'lamp' | 'bollard' | 'tree' | 'parasol' | 'band'> {
  const m = new Map<string, 'planter' | 'lamp' | 'bollard' | 'tree' | 'parasol' | 'band'>();
  m.set(dr.parasols.piece, 'parasol'); m.set(dr.lamps.piece, 'lamp'); m.set(dr.bollards.piece, 'bollard'); m.set(dr.cypress.piece, 'tree'); m.set(dr.topiary.piece, 'planter'); // a pot is a planter
  for (const r of dr.planterRuns.runs) for (const x of r.modules) m.set(x.piece, 'planter');
  for (const b of look.bands.pieces) m.set(b.piece, 'band');
  return m;
}
