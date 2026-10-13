// What the skyline crew is shown about the plan besides retrieved passages: the parts it edits, trimmed to what a
// proposal needs. Prose longer than LEAN is left out of the structures (a field a proposal leaves out keeps its value,
// so nothing is lost by it); the standards are shown whole, because their prose is the rule.
import type { Plan } from '../../plan/plan';
import type { ConceptIndex } from './concepts';

const LEAN = 200;
const j = (x: unknown) => JSON.stringify(x);

/** `x` without any string longer than `max` characters, at any depth. */
export function lean(x: unknown, max = LEAN): unknown {
  if (Array.isArray(x)) return x.map(v => lean(v, max));
  if (x === null || typeof x !== 'object') return x;
  return Object.fromEntries(Object.entries(x)
    .filter(([, v]) => !(typeof v === 'string' && v.length > max))
    .map(([k, v]) => [k, lean(v, max)]));
}

type Std = { skyline?: unknown; skyrail?: unknown; transit?: unknown; levels?: { station?: unknown };
  buildings: { towers: { sites: unknown[] } }; streetscape?: { transit?: { stationEntrance?: unknown } } };
const std = (plan: Plan) => plan.standards as unknown as Std;

/**
 * The standards the skyline is held to: heights and crowns, the sky rail and its portals, towers, the transit rules.
 */
export function skylineStandards(plan: Plan): string {
  const { sites: _sites, ...towers } = std(plan).buildings.towers;
  return j({ skyline: std(plan).skyline, skyrail: std(plan).skyrail, towers, transit: std(plan).transit });
}

/** The standards the stations are held to: the sky rail, the transit reach, a station's levels and its entrance. */
export function stationStandards(plan: Plan): string {
  const s = std(plan);
  return j({
    skyrail: s.skyrail, transit: s.transit, station: s.levels?.station,
    stationEntrance: s.streetscape?.transit?.stationEntrance,
  });
}

export const signaturesView = (plan: Plan) => j(lean(plan.signatures ?? []));
export const sitesView = (plan: Plan) => j(lean(std(plan).buildings.towers.sites));

/** Megastructures with the parts a proposal edits or must clear: footprint, portals, supports. */
export function structuresView(plan: Plan): string {
  const megas = (plan.megastructures ?? []) as Record<string, unknown>[];
  return j(megas.map(m => lean({
    id: m.id, name: m.name, kind: m.kind, areas: m.areas, footprint: m.footprint, height: m.height,
    portals: m.portals, supports: m.supports,
  })));
}

/** Sky rail lines with their paths and stations (`in`, `kit` and `fronts` as the plan has them). */
export const skyrailView = (plan: Plan) => j(lean(plan.skyrail));
export const areasView = (plan: Plan) => j(plan.areas.map(a => ({ id: a.id, name: (a as { name?: string }).name })));

/** The concept sheets by tile id, in the order the images are attached. */
export const conceptsView = (index: ConceptIndex) => index.sheets
  .map((s, i) => `Image ${i + 1}, ${s.id} (${s.path.split('/').slice(-2).join('/')}): ${s.tiles.join(', ')}`)
  .join('\n');
