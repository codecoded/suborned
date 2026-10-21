// The street layer: what has been placed (so keep-clear distances hold), and the catalogue's items as export records.
import type { StreetItem } from '../../../../contracts/world/world.schema';
import { type V, dist } from '../../../plan/geom';
import { type Ctx, r3, r4, rv } from '../common';
import { SIGHT_CLEAR, junctionCorners } from '../edges';
import { type Rect, inRect, pointInRing } from '../geo';
import { BOOTH, TUB } from '../surfaces';
import { Grid } from '../../../plan/street-geo';
import { FLAT, beamBlock, footprintOf, quayBlock, ringGap, roadAt, samples9, tramBlock, walkBlock } from '../../../plan/street-zones';

/** The look surface a kind draws in (default `trim`). A crossing landing slab (only a plan without streetscape.crossings.strip has them now), a phone booth and a palm tub have their own, so the trim colour (brass, 14 Oct) does not decide them: each colour is one look.json value. */
const SURF: Record<string, string> = { dumpster: 'enamel', 'police-post': 'enamel', tree: 'park', planter: 'park', 'tram-shelter': 'enamel', newsstand: 'enamel', 'kiosk-pavilion': 'enamel', 'kiosk-food': 'enamel', 'kiosk-flowers': 'enamel', 'kiosk-tickets': 'enamel', 'station-entrance': 'enamel', 'advertising-column': 'enamel', mailbox: 'enamel', 'vending-robot': 'enamel', 'civic-robot-dock': 'enamel', crossing: 'crossing', 'phone-booth': BOOTH, 'tub-palm': TUB };
const sightOf = (s: unknown): StreetItem['sight'] => (String(s).startsWith('blocks') ? 'blocks' : String(s).startsWith('glazed') ? 'glazed' : 'open');

/** Tree keep-clear (streetscape.trees.keepClear): a tree stays 3 m from a lamp and 2 m from a hydrant, centre to centre. */
const CLEAR = (a: string, b: string) => (a === 'tree' && b === 'lamp') || (a === 'lamp' && b === 'tree') ? 3 : (a === 'tree' && b === 'hydrant') || (a === 'hydrant' && b === 'tree') ? 2 : 0;

/** m: street items keep this far off a drive (brief 023, SF-2); a bollard or beacon may stand beside it, never on it. */
export const DRIVE_CLEAR = 2;
/** Kinds allowed next to a crossing (streetscape.rules SF-2): bollards and the crossing's beacons. */
const NEAR_CROSSING = new Set(['bollard', 'beacon', 'crossing']);

/** A kerb crossing landing as dressRoads placed it: `flank` = the bollards and beacons placed with it (they go with it if it is dropped). */
export type Landing = { id: string; road: string; cls: string; flank: string[] };

export class Layer {
  items: StreetItem[] = [];
  private uncounted = 0; // items placed under an explicit id that take no number from the ids minted after them (the civic landings)
  landings: Landing[] = [];
  private placed = new Grid<{ p: V; r: number; k: string; ring: V[]; exempt: boolean; id: string }>(8, o => o.p); // items so far, hashed: free() asks about the neighbours only
  private crossings: V[][] = []; // crossing footprints placed so far (SF-2: nothing else within 2 m)
  private corners: V[] | undefined; // junction corners (SF-8), read from the plan on first use
  private boxes: Float64Array; private doorGrid = new Grid<V[]>(8, c => c[0]!);
  private drives: V[][]; // the civic block's drives (brief 023, plan frame): nothing stands on one, street items keep DRIVE_CLEAR off it
  constructor(readonly ctx: Ctx, readonly solids: V[][], readonly decks: Rect[], readonly columns: V[], readonly keep: Rect[], readonly pylons: V[], readonly doors: V[][] = []) {
    this.drives = (ctx.civ.drives ?? []).map(x => x.poly);
    this.boxes = new Float64Array(solids.length * 4);
    solids.forEach((s, i) => { this.boxes[i * 4] = Math.min(...s.map(q => q[0])); this.boxes[i * 4 + 1] = Math.max(...s.map(q => q[0])); this.boxes[i * 4 + 2] = Math.min(...s.map(q => q[1])); this.boxes[i * 4 + 3] = Math.max(...s.map(q => q[1])); });
    for (const d of doors) this.doorGrid.add(d);
  }

  /** True if p is free of solids, deck footprints (ground-level items), columns, pylons and keep-clear rects, and r clear of other items. */
  free(p: V, r: number, ground = true, kind = ''): boolean {
    if (this.placed.near(p, 10).some(o => dist(o.p, p) < Math.max(o.r + r, CLEAR(kind, o.k)))) return false;
    if (ground && (this.columns.some(c => dist(c, p) < 6) && kind === 'tree')) return false;
    if (ground && kind === 'tree' && this.decks.some(d => inRect(p, d))) return false;
    return this.fixed(p, r);
  }
  /** True if p is clear of what never moves: keep-clear rects, pylons and solids. */
  fixed(p: V, r: number): boolean {
    if (this.keep.some(k => inRect(p, k, r))) return false;
    if (this.pylons.some(q => dist(q, p) < 3 + 0.8 + r)) return false;
    const b = this.boxes;
    for (let i = 0; i < this.solids.length; i++) if (p[0] >= b[i * 4]! && p[0] <= b[i * 4 + 1]! && p[1] >= b[i * 4 + 2]! && p[1] <= b[i * 4 + 3]! && pointInRing(p, this.solids[i]!)) return false;
    return true;
  }
  /** SF-2 for a crossing placed after the furniture: nothing but bollards, beacons and tram stop kits within `gap` m of its footprint, and no other crossing within 1 m. */
  clearOf(ring: V[], at: V, gap: number): boolean {
    return !this.crossings.some(c => ringGap(c, ring) < 1) && !this.placed.near(at, 12).some(o => !o.exempt && ringGap(o.ring, ring) < gap);
  }
  /** True if a flat ring (a signal's halo disc) stands `gap` m clear of everything placed on the ground but bollards, beacons and crossing patches: the ground an orb lights must be free. */
  clearGround(ring: V[], at: V, gap: number): boolean {
    return !this.placed.near(at, 12).some(o => !o.exempt && ringGap(o.ring, ring) < gap);
  }
  /** The items of `kinds` standing within `gap` m of a flat ring that a crossing placed here would have to move (the others are `fixed`, or not furniture: kiosks, kits and watchers stay), or undefined if something else is in the way. */
  blockers(ring: V[], at: V, gap: number, kinds: ReadonlySet<string>): string[] | undefined {
    if (this.crossings.some(c => ringGap(c, ring) < 1)) return undefined;
    const out: string[] = [];
    for (const o of this.placed.near(at, 12)) { if (o.exempt || ringGap(o.ring, ring) >= gap) continue; if (!kinds.has(o.k)) return undefined; out.push(o.id); }
    return out;
  }
  /** The items placed so far within `r` m of `p` (footprint circles: `r` is each item's), for a rule that needs to know what stands beside a spot. */
  nearby(p: V, r: number): { p: V; r: number; k: string; id: string }[] { return this.placed.near(p, r + 10).filter(o => dist(o.p, p) < o.r + r); }
  /** Take the items with these ids out of the street (a coverage site claims the ground: nothing else moves, so no id changes). */
  evict(ids: string[]): void {
    const gone = new Set(ids), at = new Map(this.items.filter(x => gone.has(x.id)).map(x => [x.id, x.at]));
    for (const [id, p] of at) for (const o of this.placed.near(p, 0.5)) if (o.id === id) this.placed.remove(o);
    this.items = this.items.filter(x => !gone.has(x.id));
  }

  /** SF-2: metres off a doorway's jambs (plan streetscape.doors, brief 026): 2 m, or the wall-unit clearance for a unit backed on the frontage wall that nobody queues at (the Automat). */
  doorClear(kind: string): number { const D = (this.ctx.S.doors ?? {}) as { clear?: number; wallUnit?: number; wallUnits?: string[] }; return (D.wallUnits ?? []).includes(kind) ? D.wallUnit ?? 2 : D.clear ?? 2; }

  /** Whether `add` would place an item of this kind here: the tests `add` makes before it places (free of neighbours and solids, out of the clear walking zone and the tram's space, 2 m off crossings and doors, off the quay). No change to the layer. */
  fits(kind: string, at: V, yaw: number, z: number, edge: string, o: { size?: [number, number, number]; variant?: string; r?: number; ground?: boolean; force?: boolean; blocks?: boolean } = {}): boolean {
    const c = this.ctx.S.catalogue[kind] as any, size = o.size ?? (c?.size as [number, number, number]) ?? [0.5, 0.5, 1], r = o.r ?? Math.max(size[0], size[1]) / 2;
    if (!o.force && !this.free(at, r, o.ground ?? true, kind)) return false;
    const ring = footprintOf(this.ctx.S, kind, o.variant, at, size as [number, number, number], yaw);
    if ((o.ground ?? true) && !FLAT.has(kind)) {
      // skip, never squeeze: the clear walking zone and the tram's own space stay clear (SF-1, SF-3); crossings keep 2 m clear (SF-2)
      if (!o.force && (walkBlock(this.ctx, samples9(ring), edge) || tramBlock(this.ctx, samples9(ring)))) return false;
      if (this.drives.some(r => ringGap(ring, r) < (NEAR_CROSSING.has(kind) ? 0.011 : DRIVE_CLEAR))) return false; // a drive (brief 023): nothing on it, items 2 m off it
      if (!NEAR_CROSSING.has(kind) && !edge.startsWith('stop/') && (this.crossings.some(c => ringGap(ring, c) < 2) || this.doorGrid.near(at, 10).some(c => ringGap(ring, c) < this.doorClear(kind)))) return false;
      if (kind === 'tree' && !o.force && beamBlock(this.ctx, at, ring, o.variant, z)) return false;
      if (!o.force && kind !== 'lamp' && kind !== 'mooring-cleat' && quayBlock(this.ctx, samples9(ring))) return false; // the quay edge is the water's (SF-14)
      // SF-8: nothing that blocks sight stands within SIGHT_CLEAR m of a junction corner's kerb tangent point (a kind the catalogue calls `blocks`, or one the caller says blocks)
      if (!o.force && (o.blocks ?? sightOf(c?.sight) === 'blocks')) { const pts = samples9(ring); if ((this.corners ??= junctionCorners(this.ctx)).some(q => pts.some(p => dist(p, q) < SIGHT_CLEAR))) return false; }
    }
    if (kind === 'crossing' && this.drives.some(r => ringGap(ring, r) < 0.011)) return false; // a landing never stands on a drive
    return true;
  }

  /** Add an item from the catalogue (or a given size). Returns false (item skipped, never squeezed: SF-6) if it does not fit. */
  add(kind: string, at: V, yaw: number, z: number, edge: string, o: { size?: [number, number, number]; variant?: string; r?: number; ground?: boolean; force?: boolean; blocks?: boolean; id?: string; trade?: string; uncounted?: boolean; kit?: { id: string; scale: [number, number, number]; yaw?: number }; pivot?: V } = {}): boolean {
    const c = this.ctx.S.catalogue[kind] as any, size = o.size ?? (c?.size as [number, number, number]) ?? [0.5, 0.5, 1], r = o.r ?? Math.max(size[0], size[1]) / 2;
    if (!this.fits(kind, at, yaw, z, edge, o)) return false;
    const ring = footprintOf(this.ctx.S, kind, o.variant, at, size as [number, number, number], yaw);
    if (kind === 'crossing') this.crossings.push(ring);
    const staffed = c?.staffed && typeof c.staffed === 'object' ? { trade: o.trade ?? c.staffed.trade, occupation: c.staffed.occupation, hours: (Array.isArray(c.staffed.hours) ? c.staffed.hours : 'service') as [number, number] | 'service' } : undefined;
    if (o.uncounted) this.uncounted++;
    const id = o.id ?? `street/${edge}/${kind}-${this.items.length - this.uncounted}`;
    this.placed.add({ p: at, r, k: kind, ring, exempt: NEAR_CROSSING.has(kind) || edge.startsWith('stop/'), id });
    this.items.push({
      id, kind, at: rv(o.pivot ?? at), yaw: r4(yaw), z: r3(z), size: [r3(size[0]), r3(size[1]), r3(size[2])], surface: SURF[kind] ?? 'trim', edge,
      sight: o.blocks ? 'blocks' : c ? sightOf(c.sight) : 'open', ...(staffed ? { staffed } : {}), ...(o.variant ? { variant: o.variant } : {}),
      ...(o.kit ? { source: 'kit' as const, kit: o.kit } : {}), // the capstone programme (capstone.ts) names its piece: `at` is then the piece's pivot, the footprint the tests read stays about `at` the caller gave
    });
    return true;
  }
}

/**
 * Twin booths (catalogue.phone-booth.twin, brief 026; Sky: the old booth was two booths in one, the one-hood single is not what he wants): every call box becomes the 2.6 x 1.3 m twin
 * where the twin passes the layer's tests (SF-1..SF-8: walk zone, doors, corners, neighbours) and stays the plan's 1 x 1 m single where it cannot. Two booths of one edge whose centres stand
 * <= `pair` m apart become one twin, centred between them, facing as the first did; it counts as `counts` booths (SF-12), a single site's twin counts as two. Deterministic: id order.
 * Returns how many twins stand for pairs and for single sites, and how many singles stayed.
 */
export function twinBooths(ctx: Ctx, L: Layer): { pairs: number; sites: number; singles: number } {
  const T = ctx.S.catalogue['phone-booth']?.twin as { variant: string; size: [number, number, number]; pair: number } | undefined, n = { pairs: 0, sites: 0, singles: 0 };
  if (!T) return n;
  const opt = (x: StreetItem) => ({ size: x.size, id: x.id, uncounted: true as const, ...(x.variant ? { variant: x.variant } : {}) });
  const booths = () => L.items.filter(i => i.kind === 'phone-booth' && !i.variant && !i.kit).sort((a, b) => a.id.localeCompare(b.id));
  const swap = (gone: StreetItem[], at: V, yaw: number, keep: StreetItem): boolean => {
    L.evict(gone.map(g => g.id));
    // SF-5: a kerb-zone item stands >= 0.45 m from the kerb face by its whole footprint; the twin is 1.3 m deep, so it may step back (away from the kerb) up to 0.6 m
    const nx = -Math.sin(yaw), ny = Math.cos(yaw), kerbOK = (p: V) => {
      if (!/(\/e\d+|^circus)$/.test(keep.edge)) return true;
      const r0 = roadAt(ctx, p, keep.edge); if (!r0 || r0.dk < -0.01 || r0.dk > r0.sec.pavement || r0.dk > r0.sec.kerb) return true;
      return Math.min(...samples9(footprintOf(ctx.S, 'phone-booth', T.variant, p, T.size, yaw)).map(q => roadAt(ctx, q, keep.edge)!.dk)) >= 0.45 + 0.005;
    };
    const force = /^(bank|stop)\//.test(keep.edge); // the transit kits' booths were placed forced (their ground is the kit's own); so is their twin
    for (let k = 0; k <= 12; k++) for (const sg of k ? [1, -1] : [1]) {
      const p: V = [at[0] + nx * sg * k * 0.05, at[1] + ny * sg * k * 0.05];
      if (kerbOK(p) && L.fits('phone-booth', p, yaw, keep.z, keep.edge, { size: T.size, variant: T.variant, force })) return L.add('phone-booth', p, yaw, keep.z, keep.edge, { size: T.size, variant: T.variant, id: keep.id, uncounted: true, force });
    }
    for (const g of gone) L.add('phone-booth', g.at, g.yaw, g.z, g.edge, opt(g));
    return false;
  };
  const done = new Set<string>();
  for (const a of booths()) {
    if (done.has(a.id)) continue;
    const b = booths().find(o => o !== a && !done.has(o.id) && o.edge === a.edge && Math.hypot(o.at[0] - a.at[0], o.at[1] - a.at[1]) <= T.pair + 1e-6);
    if (b && swap([a, b], [(a.at[0] + b.at[0]) / 2, (a.at[1] + b.at[1]) / 2], a.yaw, a)) { n.pairs++; done.add(a.id); done.add(b.id); }
  }
  for (const a of booths()) { if (done.has(a.id)) continue; if (swap([a], a.at, a.yaw, a)) n.sites++; else n.singles++; done.add(a.id); }
  return n;
}
