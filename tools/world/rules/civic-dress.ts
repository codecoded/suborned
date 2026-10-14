// CD-1..CD-4: the civic block's dressing (civic-block.json `dressing`, brief 008 B3). Each reads the export's civic masses, so what Unity draws is what is checked.
// CD-1 nothing placed stands in a keep-clear zone that bars its kind, and nothing on the ground within 1.2 m of a Handover point (a canopy overhead and flat inlay excepted);
// CD-2 every piece the block names is adopted in the kit; CD-3 the pieces brief 008 added stay within their LOD0 triangle budget; CD-4 every colour they draw is >= 20 dE2000 from the signal colours.
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Mass } from '../../../contracts/world/world.schema';
import type { Check } from '../../plan/checks';
import { toPlanFn } from '../../plan/civic';
import { type V, dist, inPoly, segDist } from '../../plan/geom';
import { type WCtx, itemRing, rule, stat } from '../../plan/world-ctx';
import { type Dressing, dressingOf, placementKinds } from '../civic/dress';
import { look } from '../civic/look';
import { adoptedBy } from '../gen/kitrefs';
import { overlaps } from '../gen/street/crossings';
import { deltaE2000 } from './deltae';

const MISSION = resolve(import.meta.dir, '../../../contracts/missions/handover/mission.json');
const SOURCE = 'civic-block.json dressing.rules (keepClear, Handover 1.2 m, landings) · lead\'s ruling 2026-10-14 (a canopy overhead may cover a point) · brief 008 B3';
const HANDOVER_CLEAR = 1.2;

/** The Handover points (mission.json destinations and crew), plan frame. None where the mission file is absent. */
export function handoverPoints(toPlan: (p: V) => V, file = MISSION): { name: string; p: V }[] {
  try {
    const m = JSON.parse(readFileSync(file, 'utf8')) as { destinations: { name: string; at: V }[]; crew: { name: string; at: V }[] };
    return [...m.destinations, ...m.crew].map(x => ({ name: x.name, p: toPlan(x.at) }));
  } catch { return []; }
}

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
    const it = c.w.street.find(s => s.kind === 'crossing' && s.id.endsWith(`/crossing-${l.id}`));
    if (it) { const ring = itemRing(it); zones.push({ id: `landing ${l.id}`, bars: ['planter', 'lamp', 'bollard', 'tree'], hits: (r: V[]) => overlaps(r, ring) }); }
  }
  // flat inlay may not enter a bay, the entrance steps or landing, or a planting bed (dressing.rules)
  const bed = (id: string, ring: V[]): Zone => ({ id, bars: ['band'], hits: (r: V[]) => overlaps(r, ring) });
  zones.push(...c.civ.bays.map(b => bed(`bay ${b.id}`, b.poly)), ...c.civ.structures.filter(s => /^entrance-(landing|steps)$/.test(s.category)).map(s => bed(s.id, s.poly)), ...c.civ.objects.filter(o => o.kind === 'planter').map(o => bed(`bed ${o.id}`, o.poly)));
  return zones;
}

/** CD-1 on the placements `masses` (kind by kit id), against `zones` and the Handover `points`. */
export function clearRule(masses: Mass[], kinds: Map<string, string>, zones: Zone[], points: { name: string; p: V }[]): Check {
  const bad: string[] = [];
  const placed = masses.filter(m => m.kit && kinds.has(m.kit.id));
  for (const m of placed) {
    const kind = kinds.get(m.kit!.id)!;
    for (const z of zones) if (z.bars.includes(kind) && z.hits(m.ring)) bad.push(`${m.id} (${kind}) stands in ${z.id}`);
    if (kind !== 'parasol' && kind !== 'band') for (const h of points) if (toRing(h.p, m.ring) < HANDOVER_CLEAR - 0.011) bad.push(`${m.id} (${kind}) is ${toRing(h.p, m.ring).toFixed(2)} m from '${h.name}'`);
  }
  return rule('CD-1', 'Civic dressing', `Nothing the block's dressing places stands in a keep-clear zone that bars its kind, or on the ground within ${HANDOVER_CLEAR} m of a Handover point (a parasol's canopy and flat inlay may)`, placed.length, bad, 'none', SOURCE);
}

/** Every piece id the block's dressing and civic-look.json name. */
export function namedPieces(dr: Dressing): string[] {
  const named = [...dr.pieces.map(p => p.piece), dr.parasols.piece, dr.lamps.piece, dr.bollards.piece, dr.cypress.piece, dr.topiary.piece, ...dr.planterRuns.runs.flatMap(r => r.modules.map(m => m.piece)),
    ...look.bands.pieces.map(b => b.piece), ...look.pieces, ...Object.values(look.swaps)];
  return [...new Set(named)].sort();
}
export function adoptedRule(named: string[], isAdopted: (id: string) => boolean = id => !!adoptedBy(id)): Check {
  return rule('CD-2', 'Civic dressing', 'Every piece the civic block names is adopted in contracts/world/kit.json', named.length, named.filter(id => !isAdopted(id)).map(id => `${id} is not adopted`), 'none', 'Splice: anything not in the rulebook is rejected · brief 008 B3');
}

/** LOD0 triangles of the pieces brief 008 added, as placed in the export's civic masses. */
export const civicTris = (masses: Mass[]): number => masses.reduce((n, m) => n + (m.kit && look.pieces.includes(m.kit.id) ? (adoptedBy(m.kit.id)?.tris ?? 0) : 0), 0);
export function budgetRule(masses: Mass[], limit = look.budget.lod0Tris): Check {
  const n = civicTris(masses);
  return stat('CD-3', 'Civic dressing', 'The pieces brief 008 added cost at most the LOD0 triangle budget in the civic block', n.toLocaleString('en-GB'), `≤ ${limit.toLocaleString('en-GB')}`, n <= limit, 'brief 008 B3 (Vela counts 68,324 with everything placed) · GDD §11 60 fps at 1080p');
}

/** CD-4: the unlit palette of the pieces (civic-look.json palette.materials) keeps `minDE` from every signal colour; a material of a piece that the palette does not list fails. */
export function paletteRule(pieces: string[], p = look.palette, materialsOf = (id: string) => adoptedBy(id)?.materials ?? []): Check {
  const by = new Map(p.materials.map(m => [m.material, m.unlit])), bad: string[] = [], seen = new Set<string>();
  for (const id of pieces) for (const m of materialsOf(id)) {
    if (seen.has(m)) continue; seen.add(m);
    const hex = by.get(m); if (!hex) { bad.push(`${m} (${id}) has no unlit colour in the palette`); continue; }
    for (const [sig, h] of Object.entries(p.signals)) { const e = deltaE2000(hex, h); if (e < p.minDE) bad.push(`${m} is ${e.toFixed(1)} dE from ${sig}`); }
  }
  return rule('CD-4', 'Civic dressing', `Every colour the civic pieces draw is at least ${p.minDE} dE2000 from the cones, guard and rivals signal colours (unlit palette)`, seen.size, bad, 'none', 'GDD §10, §15 (ΔE gate on the unlit palette) · out/decokit-008/civic-colour-roles.json');
}

export function civicDressRules(c: WCtx): Check[] {
  const dr = dressingOf(c.civ);
  if (!dr) return []; // a block without dressing (every recorded run's) has nothing to check
  const toPlan = toPlanFn(c.plan.civic);
  return [clearRule(c.w.civic, placementKinds(dr), zonesOf(c, dr, toPlan), handoverPoints(toPlan)), adoptedRule(namedPieces(dr)), budgetRule(c.w.civic), paletteRule(look.pieces)];
}
