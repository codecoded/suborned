// CD-1..CD-4: the civic block's dressing (civic-block.json `dressing`, brief 008 B3). Each reads the export's civic masses, so what Unity draws is what is checked.
// CD-1 nothing placed stands in a keep-clear zone that bars its kind, and nothing on the ground within 1.2 m of a Handover point (a canopy overhead and flat inlay excepted; a crossing's signal orb is a canopy, its halo and studs are ground);
// CD-2 every piece the block names is adopted in the kit, and a material a mass draws in place of its piece's own replaces a slot the piece has; CD-3 the pieces brief 008 added (the bed tufts and the terrace among them)
// stay within their LOD0 triangle budget; CD-4 every material they draw (pieces, bed kerb and hedge, replaced slots) has its unlit colour in the look palette. (CD-4 was also a dE2000 >= 20 gate from the signal colours; Sky freed them on 2026-10-16.)
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Mass, World } from '../../../contracts/world/world.schema';
import type { Check } from '../../plan/checks';
import { toPlanFn } from '../../plan/civic';
import { type V, dist, inPoly, segDist } from '../../plan/geom';
import { type WCtx, itemRing, rule, stat } from '../../plan/world-ctx';
import { type Dressing, dressingOf, placementKinds } from '../civic/dress';
import { look } from '../civic/look';
import { discRing } from '../gen/box';
import { adoptedBy } from '../gen/kitrefs';
import { landingsOf, overlaps } from '../gen/street/crossings';

const SOURCE = 'civic-block.json dressing.rules (keepClear, Handover 1.2 m, landings) · lead\'s ruling 2026-10-14 (a canopy overhead may cover a point) · brief 008 B3';
const HANDOVER_CLEAR = 1.2;

export { handoverPoints } from '../civic/handover';
import { handoverPoints } from '../civic/handover';

/** The shortest distance from p to a ring (0 inside it). */
const toRing = (p: V, ring: V[]): number => (inPoly(p, ring) ? 0 : Math.min(...ring.map((a, i) => segDist(p, a, ring[(i + 1) % ring.length]!))));
/** A ring meets an annulus about `c`: some point of it lies between r0 and r1 from c. */
const meetsAnnulus = (ring: V[], c: V, r0: number, r1: number): boolean => Math.max(...ring.map(p => dist(p, c))) >= r0 && toRing(c, ring) <= r1;

type Zone = { id: string; bars: string[]; hits: (ring: V[]) => boolean };
/** The keep-clear zones in plan frame, and the landing of each civic crossing (1.8 m deep, 4 m wide inside the kerb: bars planter, lamp, bollard and tree). */
function zonesOf(c: WCtx, dr: Dressing, toPlan: (p: V) => V): Zone[] {
  const zones: Zone[] = dr.keepClear.map(z => 'polygon' in z
    ? { id: z.id, bars: z.bars, hits: (r: V[]) => overlaps(r, z.polygon.map(toPlan)) }
    : { id: z.id, bars: z.bars, hits: (r: V[]) => meetsAnnulus(r, toPlan(z.ring.centre), z.ring.r0, z.ring.r1) });
  for (const l of dr.crossings.at) {
    const it = landingsOf(c.w).find(s => s.id.endsWith(`/crossing-${l.id}`));
    if (it) { const ring = itemRing(it); zones.push({ id: `landing ${l.id}`, bars: ['planter', 'lamp', 'bollard', 'tree'], hits: (r: V[]) => overlaps(r, ring) }); }
  }
  // flat inlay may not enter a bay, the entrance steps or landing, or a planting bed (dressing.rules)
  const bed = (id: string, ring: V[]): Zone => ({ id, bars: ['band'], hits: (r: V[]) => overlaps(r, ring) });
  zones.push(...c.civ.bays.map(b => bed(`bay ${b.id}`, b.poly)), ...c.civ.structures.filter(s => /^entrance-(landing|steps)$/.test(s.category)).map(s => bed(s.id, s.poly)), ...c.civ.objects.filter(o => o.kind === 'planter').map(o => bed(`bed ${o.id}`, o.poly)));
  return zones;
}

/** What the crossings put on the ground (brief 009): each kerb stud and each signal orb's halo disc, on the ground within reach of a Handover point like any piece of the dressing. The orb itself hovers overhead (canopy class, exempt), and the light bars, dots and
 *  inlay arcs are flush with the paving (flat inlay, exempt); the halo is a glowing disc and the studs are domes, so they count. */
export function crossingGround(w: Pick<World, 'street'>): Ground[] {
  return w.street.flatMap(s => s.kind === 'crossing-stud' ? [{ id: s.id, what: 'crossing stud', ring: itemRing(s) }] : s.signal ? [{ id: s.id, what: 'signal halo', ring: discRing(s.signal.halo.at, s.signal.halo.r) }] : []);
}
type Ground = { id: string; what: string; ring: V[] };

/** CD-1 on the placements `masses` (kind by kit id), against `zones` and the Handover `points`; `ground` = what the crossings stand on the ground (crossingGround), against the points only. */
export function clearRule(masses: Mass[], kinds: Map<string, string>, zones: Zone[], points: { name: string; p: V }[], ground: Ground[] = []): Check {
  const bad: string[] = [];
  for (const g of ground) for (const h of points) if (toRing(h.p, g.ring) < HANDOVER_CLEAR - 0.011) bad.push(`${g.id} (${g.what}) is ${toRing(h.p, g.ring).toFixed(2)} m from '${h.name}'`);
  const placed = masses.filter(m => m.kit && kinds.has(m.kit.id));
  for (const m of placed) {
    const kind = kinds.get(m.kit!.id)!;
    for (const z of zones) if (z.bars.includes(kind) && z.hits(m.ring)) bad.push(`${m.id} (${kind}) stands in ${z.id}`);
    if (kind !== 'parasol' && kind !== 'band') for (const h of points) if (toRing(h.p, m.ring) < HANDOVER_CLEAR - 0.011) bad.push(`${m.id} (${kind}) is ${toRing(h.p, m.ring).toFixed(2)} m from '${h.name}'`);
  }
  return rule('CD-1', 'Civic dressing', `Nothing the block's dressing places stands in a keep-clear zone that bars its kind, or on the ground within ${HANDOVER_CLEAR} m of a Handover point (a parasol's canopy and flat inlay may); a crossing's studs and signal halos are on the ground, its orb is a canopy`, placed.length + ground.length, bad, 'none', SOURCE);
}

/** Every piece id the block's dressing and civic-look.json name. */
export function namedPieces(dr: Dressing): string[] {
  const named = [...dr.pieces.map(p => p.piece), dr.parasols.piece, dr.lamps.piece, dr.bollards.piece, dr.cypress.piece, dr.topiary.piece, ...dr.planterRuns.runs.flatMap(r => r.modules.map(m => m.piece)),
    ...look.bands.pieces.map(b => b.piece), ...look.pieces, ...Object.values(look.swaps), ...Object.values(look.companions).flat(), ...look.beds.tufts.pieces];
  return [...new Set(named)].sort();
}
/** A mass that draws a material in place of its piece's own: the slot is one the piece has (judged only where the catalogue lists the piece's materials: a run's pinned kit digest does not), and the material is one the look palette holds. */
export function overrideProblems(masses: Mass[], slotsOf: (id: string) => string[] | undefined = id => adoptedBy(id)?.materials, palette = look.palette): string[] {
  const held = new Set(palette.materials.map(m => m.material));
  return masses.flatMap(m => Object.entries(m.materials ?? {}).flatMap(([slot, to]) => [
    ...((m.kit ? slotsOf(m.kit.id) : [])?.includes(slot) !== false ? [] : [`${m.id} replaces ${slot}, which ${m.kit?.id ?? 'a mass with no kit piece'} does not draw`]), ...(held.has(to) ? [] : [`${m.id} draws ${slot} as ${to}, which the palette does not list`])]));
}
export function adoptedRule(named: string[], isAdopted: (id: string) => boolean = id => !!adoptedBy(id), overrides: string[] = []): Check {
  return rule('CD-2', 'Civic dressing', 'Every piece the civic block names is adopted in contracts/world/kit.json, and a material a mass draws in place of its piece\'s own replaces a slot the piece has',
    named.length, [...named.filter(id => !isAdopted(id)).map(id => `${id} is not adopted`), ...overrides], 'none', 'Splice: anything not in the rulebook is rejected · brief 008 B3, colour round');
}

/** LOD0 triangles of the pieces brief 008 added, as placed in the export's civic masses. */
export const civicTris = (masses: Mass[]): number => masses.reduce((n, m) => n + (m.kit && look.pieces.includes(m.kit.id) ? (adoptedBy(m.kit.id)?.tris ?? 0) : 0), 0);
export function budgetRule(masses: Mass[], limit = look.budget.lod0Tris): Check {
  const n = civicTris(masses);
  return stat('CD-3', 'Civic dressing', 'The pieces brief 008 added cost at most the LOD0 triangle budget in the civic block', n.toLocaleString('en-GB'), `≤ ${limit.toLocaleString('en-GB')}`, n <= limit, 'brief 008 B3 (Vela counts 68,324 with everything placed) · GDD §11 60 fps at 1080p');
}

/** Materials the civic masses draw outside their pieces' own slots: the bed kerb and hedge, and each replaced slot's material. */
export const drawnApart = (masses: Mass[] = [], b = look.beds): { material: string; by: string }[] => [
  { material: b.kerb.material, by: `bed kerb (${b.kerb.surface})` }, { material: b.hedge.material, by: `bed hedge (${b.hedge.surface})` },
  ...masses.flatMap(m => Object.entries(m.materials ?? {}).map(([slot, to]) => ({ material: to, by: `${m.id} ${slot}` }))), ...Object.values(look.materials).flatMap(o => Object.values(o).map(to => ({ material: to, by: 'civic-look.json materials' })))];
/** CD-4: every material the pieces, the bed kerb and hedge and the replaced slots draw is listed, with its unlit colour, in the look palette (civic-look.json palette.materials); a material the palette does not list fails. No colour is held to a distance from the signal colours (Sky, 2026-10-16). */
export function paletteRule(pieces: string[], p = look.palette, materialsOf = (id: string) => adoptedBy(id)?.materials ?? [], apart: { material: string; by: string }[] = []): Check {
  const by = new Map(p.materials.map(m => [m.material, m.unlit])), bad: string[] = [], seen = new Set<string>();
  const gate = (m: string, who: string) => {
    if (seen.has(m)) return; seen.add(m);
    if (!by.get(m)) bad.push(`${m} (${who}) has no unlit colour in the palette`);
  };
  for (const id of pieces) for (const m of materialsOf(id)) gate(m, id);
  for (const a of apart) gate(a.material, a.by);
  return rule('CD-4', 'Civic dressing', 'Every material the civic block draws (pieces, bed kerb and hedge, replaced slots) has its unlit colour in the look palette', seen.size, bad, 'none', 'civic-look.json palette.materials · out/decokit-008/civic-colour-roles.json');
}

export function civicDressRules(c: WCtx): Check[] {
  const dr = dressingOf(c.civ);
  if (!dr) return []; // a block without dressing (every recorded run's) has nothing to check
  const toPlan = toPlanFn(c.plan.civic);
  return [clearRule(c.w.civic, placementKinds(dr), zonesOf(c, dr, toPlan), handoverPoints(toPlan), crossingGround(c.w)), adoptedRule(namedPieces(dr), undefined, overrideProblems(c.w.civic)), budgetRule(c.w.civic), paletteRule(look.pieces, undefined, undefined, drawnApart(c.w.civic))];
}
