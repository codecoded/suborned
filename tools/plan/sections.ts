// A road's pavement section (brief 075 ask 1): the class section (standards.streetscape.sections[class]) unless the road carries its own
// (roads[].pavement: w = kerb + walk + frontage, from the kerb face to the building line), which replaces the class's width and zones for that road only.
// One reader for the checker and the generator: the building line (gen/edges.ts pavementOf), the street zones (street-zones.ts roadAt), B-3, KR-1, the
// megastructure spans, the frontage spill and the kerb furniture all take the road's section from here. R-PV1 (checks.ts) holds the field to its rules.
// Opt-in: a plan without roads[].pavement reads exactly the class sections, so recorded plans rederive what they were made with.
export type OwnPavement = { w: number; kerb: number; walk: number; frontage: number; why?: string; source?: string };
export type Section = { pavement: number; kerb: number; walk: number; frontage: number; [k: string]: unknown };

const cache = new WeakMap<object, Map<string, OwnPavement>>();
/** roads[].pavement by road id (only roads that carry one). */
export function ownPavements(plan: { roads: { id: string; pavement?: OwnPavement }[] }): Map<string, OwnPavement> {
  let m = cache.get(plan);
  if (!m) cache.set(plan, (m = new Map(plan.roads.filter(r => r.pavement).map(r => [r.id, r.pavement!]))));
  return m;
}

/** The section of road `id` of class `cls`: the class section, with pavement, kerb, walk and frontage replaced by the road's own when it has one. */
export function sectionOf(sections: Record<string, any>, plan: { roads: { id: string; pavement?: OwnPavement }[] }, cls: string, id?: string): Section {
  const base = sections[cls] as Section, own = id ? ownPavements(plan).get(id) : undefined;
  return own ? { ...base, pavement: own.w, kerb: own.kerb, walk: own.walk, frontage: own.frontage, own: true } : base;
}

/** R-PV1's findings for one road's own pavement ('' = sound): the zones sum to w, the clear walk is no narrower than its class's (SF-1 keeps its width),
 *  the kerb zone holds an item 0.45 m off the kerb face (streetscape note, Manual for Streets) with room for a 0.3 m lamp column, and a reason is given. */
export function ownPavementFault(sections: Record<string, any>, cls: string, p: OwnPavement): string {
  const num = (x: unknown) => typeof x === 'number' && Number.isFinite(x) && x >= 0;
  if (!['primary', 'district', 'access'].includes(cls)) return `a ${cls} road has no pavement section to replace`;
  if (![p.w, p.kerb, p.walk, p.frontage].every(num)) return 'w, kerb, walk and frontage must be numbers ≥ 0';
  if (Math.abs(p.kerb + p.walk + p.frontage - p.w) > 0.01) return `kerb ${p.kerb} + walk ${p.walk} + frontage ${p.frontage} ≠ w ${p.w}`;
  const cw = sections[cls]?.walk as number;
  if (p.walk < cw - 0.01) return `walk ${p.walk} m is narrower than the ${cls} section's clear walk ${cw} m (SF-1)`;
  if (p.kerb < 0.75 - 0.01) return `kerb zone ${p.kerb} m holds no item 0.45 m off the kerb face (needs 0.75)`;
  if (typeof p.why !== 'string' || !p.why.trim()) return 'no why';
  return '';
}
