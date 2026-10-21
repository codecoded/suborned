// Kit references (brief 006 A): every element a kit piece can fill gets `kit` (scale, yaw), the rest `source: "procedural"`.
// The slot decides (tools/kit/fit.ts): a piece is scaled within its range, never the slot. Choice is seeded by element id, with
// neighbour contrast: no same piece within `NEAR[kind]` metres of the same kind (street items) or on an adjacent tenancy (signs).
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Kit, KitAsset } from '../../../contracts/world/kit.schema';
import type { Frontage, KitRef, Landmark, Mass, Prim, StreetItem } from '../../../contracts/world/world.schema';
import { Grid } from '../../plan/street-geo';
import { type Slot, type Scale, fit } from '../../kit/fit';
import { signBox, signObstacles } from './box';
import { type Ctx, r3, r4, rng, rv } from './common';
import type { V } from '../../plan/geom';
import type { Landmark as PlanLandmark } from '../../plan/plan';
import { footOf } from '../civic/foot';
import { CAPSTONE_KINDS } from './street/capstone-rules';

const KIT = resolve(import.meta.dir, '../../../contracts/world/kit.json');
const catalogues = new Map<string, KitAsset[]>();
let pinned: string | undefined;
/** The adopted pieces of contracts/world/kit.json (candidates and rejects never reach the generator), or of the catalogue `withKit` names. */
export function adopted(): KitAsset[] {
  const file = pinned ?? KIT;
  let assets = catalogues.get(file);
  if (!assets) {
    assets = (JSON.parse(readFileSync(file, 'utf8')) as Kit).assets.filter(a => a.status === 'adopted');
    catalogues.set(file, assets);
  }
  return assets;
}
/**
 * Runs `fn` with `file` as the catalogue: a kit.json, or the kit digest a run pinned (both list `assets[]` with status, size and tags,
 * which is all the generator reads). A pinned run re-checks against the pieces it ran with, not whatever Vela has adopted since.
 */
export function withKit<T>(file: string | undefined, fn: () => T): T {
  const before = pinned;
  pinned = file;
  try { return fn(); } finally { pinned = before; }
}
const everything = new Map<string, KitAsset[]>();
/** A piece of the catalogue by id whatever its status (candidate, adopted, rejected): the generator measures a candidate it is about to place a stand-in for, and reads its tags (a Dumpster's `stash`), but never draws it. */
export function catalogued(id: string): KitAsset | undefined {
  const file = pinned ?? KIT;
  let assets = everything.get(file);
  if (!assets) { assets = (JSON.parse(readFileSync(file, 'utf8')) as Kit).assets; everything.set(file, assets); }
  return assets.find(a => a.id === id);
}
/** The first catalogue piece, whatever its status, whose `use` names this street kind (catalogue order). */
export function cataloguedFor(use: string): KitAsset | undefined {
  const file = pinned ?? KIT;
  catalogued('');
  return everything.get(file)!.find(a => a.tags.use.includes(use));
}
export const forUse = (use: string) => adopted().filter(a => a.tags.use.includes(use));

/** Metres within which two items of one kind must not use the same piece. */
const NEAR: Record<string, number> = { tree: 16, 'tree:washingtonia': 6, 'tub-palm': 12, 'tub-palm:planted': 12, planter: 24, 'tram-shelter': 60, newsstand: 60, 'kiosk-food': 60, lamp: 50, bench: 40, bin: 40, bollard: 6, hydrant: 100, 'phone-booth': 100, 'ticket-machine': 40, 'pole-camera': 40, 'canopy-camera': 20, 'advertising-column': 100 };
/** Kerb zone depth: the narrowest in the plan (standards.streetscape.sections). */
export const kerbZone = (ctx: Ctx): number => Math.min(...Object.values<any>(ctx.S.sections).map(s => s?.kerb).filter((k): k is number => typeof k === 'number'));

/** The slot an item offers a kit piece, or null where the kit has nothing for the kind. */
export function slotOf(ctx: Ctx, s: Pick<StreetItem, 'kind' | 'variant' | 'size'>): Slot | null {
  const [w, d, h] = s.size, kerb = kerbZone(ctx);
  switch (s.kind) {
    case 'lamp': return { h, along: 2.5, depth: kerb };
    case 'bollard': return { h, hTol: 0.25, along: 0.8, depth: 0.8 };
    case 'bench': return { h, along: w * 1.2, depth: d * 1.3 };
    case 'bin': return { h, hTol: 0.25, along: 1.3, depth: 0.6 };
    case 'hydrant': return { h, along: 0.6, depth: 0.6 };
    case 'phone-booth': if (s.variant === 'twin') return { h, maxH: ctx.S.catalogue['phone-booth'].cap, along: w * 1.1, depth: d * 1.3 }; return { h, maxH: ctx.S.catalogue['phone-booth'].cap, along: w * 1.4, depth: d * 1.3 }; // the item's footprint is the plan's K6 capsule 1 × 1 m; brief 026: Sky wants the old booth back, so the room is the one-hood PhoneBooth_Single (1.32 × 1.26 m). The twin pod (2.58 m) takes two items, see twinBooths
    case 'dumpster': return { h, hTol: 0.25, along: w * 1.1, depth: d * 1.2 }; // brief 013: the stash container beside a service door; only an adopted Dumpster (status adopted) is ever drawn
    case 'ticket-machine': return { h, along: 1.1, depth: 0.8 };
    case 'pole-camera': return { h, along: 0.8, depth: kerb };
    case 'canopy-camera': return { h, hTol: 0.5, along: 0.6, depth: 0.6 };
    case 'advertising-column': return { h, along: w * 1.3, depth: d * 1.3 };
    // brief 020 (Vela's pick; the plan's sizes stay the plan's, the slot says how far a piece may differ from them):
    case 'planter': return { h, hTol: 0.4, along: w * 1.4, depth: d }; // the deck's planting bed 1.5 x 1.5 x 0.9: a trough 2.0 m long may stand across it, an urn inside it
    case 'mailbox': return { h: 2.34, hTol: 0.1, along: 1.0, depth: 1.0 }; // the plan's 0.6 x 0.6 box stays the footprint; its 1.3 m height is not held: PneumaticPost_Play is the full original post, 2.34 m (brief 026)
    case 'public-clock': return { h, hTol: 0.1, along: (ctx.S.catalogue['public-clock'].face as number) * 1.1, depth: 0.8 }; // the double face is 1.2 m wide
    case 'tram-shelter': { const r = ctx.S.catalogue['tram-shelter'].roof as [number, number, number]; return { h, hTol: 0.15, along: r[0], depth: r[1] }; } // the roof's footprint 9.0 x 2.4
    case 'kiosk-pavilion': return { h, hTol: 0.05, along: w * 1.05, depth: d * 1.2 }; // brief 026: the old CurvedKiosk 6.08 x 4.36 x 6.5 m at its own size
    case 'newsstand': return { h, hTol: 0.15, along: w * 1.1, depth: d * 1.4 }; // brief 026: the curved kiosk at 0.46, 2.80 × 2.01 m in plan, 2.99 m tall (the plan's box is 3.0 × 1.5 × 2.8; the kerb zone is 2.0 plus 0.45 widening)
    case 'kiosk-food': { const r = ctx.S.catalogue['kiosk-food'].roof as [number, number, number]; return { h, hTol: 0.2, along: r[0], depth: r[1] }; }
    case 'tub-palm': { const sp = ctx.S.trees.species[s.variant ?? ''] as { top: number; spread: number } | undefined; return sp ? { h: sp.top, hTol: 0.5, along: sp.spread, depth: sp.spread } : null; } // brief 020: a tub palm may stand to half its plan height (FanPalm's crown is 2.8 m wide: 4.5 m at that crown is a 5 m crown in a 3 m room)
    case 'tree': { const sp = ctx.S.trees.species[s.variant ?? ''] as { top: number; spread: number } | undefined; return sp ? { h: sp.top, hTol: 0.35, along: sp.spread, depth: sp.spread } : null; }
    default: return CAPSTONE_KINDS.has(s.kind) ? { h: s.size[2], hTol: 0.01, along: s.size[0] + 0.001, depth: s.size[1] + 0.001 } : null; // the capstone programme's pieces are placed at the size their item states: the slot is the item
  }
}

/** Kit yaw (turns the piece's +X; its +Z front faces yaw + 90°) that points the piece's reach axis along `dir`: a lamp's head over the road. */
export const reachYaw = (dir: number, axis: '+x' | '-x' | '+z' | '-z'): number => {
  const y = dir + { '+x': 0, '-x': Math.PI, '+z': -Math.PI / 2, '-z': Math.PI / 2 }[axis];
  return r4(Math.atan2(Math.sin(y), Math.cos(y)));
};

type Fit = { a: KitAsset; k: Scale };
/** Fitting pieces for `use`, in catalogue order. */
const fitting = (use: string, slot: Slot, ok: (a: KitAsset) => boolean = () => true): Fit[] => forUse(use).filter(ok).flatMap(a => { const k = fit(a, slot); return k ? [{ a, k }] : []; });

/** Seeded choice that avoids the neighbours' pieces, then the adjacent group, then its nearest member. */
export function choose(id: string, options: Fit[], taken: string[], adjacent: string[] = []): Fit | null {
  if (!options.length) return null;
  const r = rng(`kit/${id}`), fresh = options.filter(o => !taken.includes(o.a.id)), second = options.filter(o => !adjacent.includes(o.a.id)), nearest = options.filter(o => o.a.id !== adjacent[0]);
  return r.pick(fresh.length ? fresh : second.length ? second : nearest.length ? nearest : options);
}

export function streetKit(ctx: Ctx, items: StreetItem[]): StreetItem[] {
  const grids = new Map<string, Grid<{ at: V; id: string }>>();
  const planted = new Set(items.filter(i => i.kind === 'planter').map(i => `${i.at[0].toFixed(1)},${i.at[1].toFixed(1)}`)); // a palm standing in a planter's bed (a deck's planting bed carries one): it takes a bare palm, never a potted piece
  const palmAt = new Set(items.filter(i => i.kind === 'tub-palm').map(i => `${i.at[0].toFixed(1)},${i.at[1].toFixed(1)}`)); // a planter with a palm planted in it is a bed (the trough), never an urn: the trunk would stand through the urn wall (brief 026, Vela round 2)
  return items.map(it => {
    if (it.kit) return { ...it, source: 'kit' as const }; // already named by its programme (street/capstone.ts)
    const slot = slotOf(ctx, it), use = it.kind === 'tub-palm' && planted.has(`${it.at[0].toFixed(1)},${it.at[1].toFixed(1)}`) ? 'tub-palm:planted' : it.kind === 'planter' && palmAt.has(`${it.at[0].toFixed(1)},${it.at[1].toFixed(1)}`) ? 'planter:planted' : it.kind === 'phone-booth' && it.variant === 'twin' ? 'phone-booth:twin' : it.kind === 'tree' && it.variant === 'washingtonia' ? 'tree:washingtonia' : it.kind;
    const g = grids.get(use) ?? grids.set(use, new Grid(8, o => o.at)).get(use)!, near = g.near(it.at, NEAR[use] ?? 20).filter(o => Math.hypot(o.at[0] - it.at[0], o.at[1] - it.at[1]) < (NEAR[use] ?? 20));
    const gap = (o: { at: V }) => Math.hypot(o.at[0] - it.at[0], o.at[1] - it.at[1]);
    near.sort((a, b) => gap(a) - gap(b));
    const close = (near[0] ? gap(near[0]) : Infinity) * 1.25; // adjacent = within 25% of the nearest, nearest first
    const pick = slot ? choose(it.id, fitting(use, slot), near.map(o => o.id), near.filter(o => gap(o) <= close).map(o => o.id)) : null;
    if (!pick) return { ...it, source: 'procedural' as const };
    g.add({ at: it.at, id: pick.a.id });
    const reach = pick.a.tags.reach;
    const yaw = it.kind === 'tree' || it.kind === 'tub-palm' ? r4(rng(`kit/${it.id}/yaw`).f() * Math.PI * 2) : reach ? reachYaw(it.yaw, reach.axis) : undefined;
    const kit: KitRef = { id: pick.a.id, scale: pick.k, ...(yaw === undefined ? {} : { yaw }) };
    return { ...it, source: 'kit' as const, kit };
  });
}

/** Metres a blade sign's lowest point stands above the top of its tenancy's canopy slab. */
export const BLADE_CLEAR = 0.1;
/**
 * Signs: a blade or wall sign beside the fascia on tenancies the kit has a sign for; adjacent tenancies never share a
 * piece, and a sign whose box would pass through anything (F-7: a canopy, a gallery roof, a podium) is not placed.
 */
export function signKit(
  ctx: Ctx, fronts: Frontage[], megas: { prims: Prim[]; parts: Mass[] }[], buildings: Mass[],
): Frontage[] {
  const g = new Grid<{ at: V; w: number; id: string }>(8, o => o.at);
  const obstacles = signObstacles(fronts, megas, buildings);
  return fronts.map(f => {
    const none = { ...f, source: 'procedural' as const }, r = rng(`kit/${f.id}/sign`), sg = f.sign;
    if (!r.chance(0.7)) return none;
    const wall = r.chance(0.35), c = sg.c as V, u: V = [-f.normal[1], f.normal[0]]; // along the facade
    const slot: Slot = wall ? { h: 1.8, hTol: 0.1, along: Math.min(f.w, 3), depth: 0.8 } : { h: Math.max(3, sg.base + sg.size[2] - f.z - 2.6), hTol: 0.1, along: 0.8, depth: 1.2 };
    const use = wall ? 'wall-sign' : 'blade-sign';
    const neigh = g.near(c, 40).filter(o => Math.hypot(o.at[0] - c[0], o.at[1] - c[1]) < (o.w + f.w) / 2 + 2);
    const pick = choose(f.id, fitting(use, slot, a => !!a.tags.trade?.includes(f.trade)), neigh.map(o => o.id), neigh.map(o => o.id));
    if (!pick || neigh.some(o => o.id === pick.a.id)) return none; // no contrasting piece: the fascia alone
    const x: V = [f.normal[1], -f.normal[0]];
    // a blade's pivot is its wall plane: put it on the shopfront face (the front prim's depth from the facade line), not proud of the fascia
    const proud = (c[0] - f.a[0]) * f.normal[0] + (c[1] - f.a[1]) * f.normal[1] - f.front.size[1], out = wall ? 0.2 : -proud, side = wall ? 0 : sg.size[0] / 2 + 0.3;
    const at: V = [c[0] + f.normal[0] * out + u[0] * side, c[1] + f.normal[1] * out + u[1] * side];
    // a wall sign stands over the sign board; a blade rises from just over the canopy slab (F-7: it used to hang
    // from the fascia down to head height, through the awning)
    const base = wall ? sg.base + sg.size[2] + 0.2 : f.canopy.base + f.canopy.size[2] + BLADE_CLEAR;
    const mount = wall ? 'wall' as const : 'blade' as const;
    const sign = { id: pick.a.id, scale: pick.k, yaw: r4(Math.atan2(x[1], x[0])), at: [r3(at[0]), r3(at[1])] as V, base: r3(base), mount };
    if (obstacles.hits(signBox(pick.a, sign)).length) return none; // it would pass through something: the fascia alone
    g.add({ at: c, w: f.w, id: pick.a.id });
    return { ...f, source: 'kit' as const, signKit: sign };
  });
}

/** An adopted piece by id, or undefined (a plan that names a piece the kit has not adopted gets the procedural stand-in). */
export const adoptedBy = (id: string | undefined): KitAsset | undefined => (id ? adopted().find(a => a.id === id) : undefined);

/** Landmarks: the kit piece scaled to stand inside the plan's disc and under its height; if it cannot, procedural and why.
 *  The skyport carries `pads`: SkyPad_Roof × 1.0 at each (c, yaw°) on the procedural caisson. */
export function landmarkKit(l: Landmark, plan: PlanLandmark): Landmark {
  const a = adoptedBy(plan.kit) ?? forUse(`landmark:${plan.id}`)[0];
  if (!a) return { ...l, source: 'procedural', why: 'no adopted kit piece' };
  if (plan.pads?.length) return { ...l, source: 'kit', kits: plan.pads.map(p => ({ id: a.id, scale: [1, 1, 1] as Scale, yaw: r4((p.yaw * Math.PI) / 180), at: [r3(p.c[0]), r3(p.c[1])] as V })) };
  // a caisson landmark (the liner berth) stands on the caisson deck: the piece may rise from there to the plan's top
  const k = fit(a, { foot: 2 * plan.r, maxH: plan.caisson && plan.top ? plan.top - plan.caisson.deck : l.height });
  if (!k) return { ...l, source: 'procedural', why: `${a.id} ${a.size.join('×')} m does not fit the plan disc Ø${2 * plan.r} × ${r3(l.height)} m within scale ${a.tags.scale.join('–')}` };
  return { ...l, source: 'kit', kit: { id: a.id, scale: k } };
}

/**
 * An adopted piece standing on the roof slab (brief 020): its footprint centre at (lu, lv) in the roof frame (u along `frame.yaw`, v across), turned by `frame.yaw + yawAdd`, at scale 1.
 * None if its footprint (pivotOffset included) leaves the roof rect (the usable rect inside the parapet), or if `maxTop` is given and the piece rises above it.
 */
export function roofPiece(a: KitAsset, frame: { c: V; yaw: number; W: number; D: number }, lu: number, lv: number, o: { yawAdd?: number; maxTop?: number; z?: number } = {}): (KitRef & { at: V }) | undefined {
  if (o.maxTop !== undefined && (o.z ?? 0) + a.size[1] > o.maxTop + 1e-6) return undefined;
  const y = frame.yaw + (o.yawAdd ?? 0), U: V = [Math.cos(frame.yaw), Math.sin(frame.yaw)], Vv: V = [-U[1], U[0]], off = a.pivotOffset ?? [0, 0], ky: V = [Math.cos(y), Math.sin(y)];
  const at: V = [frame.c[0] + U[0] * lu + Vv[0] * lv, frame.c[1] + U[1] * lu + Vv[1] * lv];
  const pivot: V = [at[0] - (off[0] * ky[0] - off[1] * ky[1]), at[1] - (off[0] * ky[1] + off[1] * ky[0])]; // the footprint centre lands on `at`: pivot + offset (kit x along the yaw, kit z across)
  const ring = footOf(a, { scale: [1, 1, 1], yaw: y, at: pivot });
  const E = 1e-6;
  if (ring.some(q => { const dx = q[0] - frame.c[0], dy = q[1] - frame.c[1]; return Math.abs(dx * U[0] + dy * U[1]) > frame.W / 2 + E || Math.abs(dx * Vv[0] + dy * Vv[1]) > frame.D / 2 + E; })) return undefined;
  return { id: a.id, scale: [1, 1, 1], yaw: r4(y), at: [r3(pivot[0]), r3(pivot[1])] };
}

/** A roof programme's kit piece (plan roofs.kit.programmes.<name>.kit) at `at` on the slab, if adopted and it fits the roof rect. */
export function roofKit(prog: { kit?: string; minRoof: number[] }, roof: { W: number; D: number }, at: V, yaw: number): (KitRef & { at: V }) | undefined {
  const a = adoptedBy(prog.kit);
  if (!a || a.size[0] > roof.W + 1e-6 || a.size[2] > roof.D + 1e-6) return undefined;
  return { id: a.id, scale: [1, 1, 1], yaw: r4(yaw), at: [r3(at[0]), r3(at[1])] };
}
