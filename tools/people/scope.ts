// A scope: the part of the city a people run covers, defined from the civic block and the plan, never from a hand list (brief 010 B).
// `civic-block`: the plots the block's `blocks` stand on (the plan's civic plots), their areas, the buildings on them with their
// entrances and bays, the nearest tram stop and sky rail station, and the home plots within the plan's transit reach.
import { civic, toPlanFn, type Civic } from '../plan/civic';
import { area, centroid, dist, inPoly, type V } from '../plan/geom';
import { derive, type Derived, type Plan } from '../plan/plan';
import type { Place } from '../../contracts/people/people.schema';
import { employerOfBlock, type Rules } from './rules';

export const SCOPES = ['civic-block'] as const;
/** Residents who are simulated people per home plot; the rest of a plot is background (the density target tops the street up). [S] */
export const HOME_SAMPLE = 8;
/** Civic employers keep office hours until the shift chime (mission.json clock: 17:30; GDD §07). [S] */
export const OFFICE_HOURS: [number, number] = [9.5, 17.5];

export type Unit = {
  id: string; plot: string; area: string; trade: string; employer: string; category: string; name: string; kind: 'frontage' | 'civic' | 'kiosk';
  staffPerShift: number; hours: [number, number]; shifts: { from: number; to: number }[];
  door: string; bay?: string; building?: string;
};
export type Home = { plot: string; area: string; residents: number; place: string };
export type Scope = {
  id: string; areas: string[]; plots: { id: string; area: string; use: string; civic?: string }[];
  stops: { place: string; line: string; id: string; mode: 'tram' | 'skyrail'; name: string }[];
  units: Unit[]; places: Place[]; homes: Home[];
  bayPolys: Record<string, V[]>; zones: { id: string; test(p: V): boolean }[];
  /** where a customer of the café sits: the nearest seat to its door
  */
  /** the places a person may stand off-shift without being in a keep-clear zone: seats */
  seats: string[];
  /** the walk speed (m/s) the people are timed by */
  speed: number;
  cafe: string;
  /** the unit whose door the cafe seat stands by: someone sitting at `cafe` is at that unit's customers' side (a regular) */
  cafeUnit: string;
};

const kebab = (s: string) => s.replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
const rectDist = (p: V, r: [number, number, number, number]) => Math.hypot(Math.max(r[0] - p[0], 0, p[0] - r[1]), Math.max(r[2] - p[1], 0, p[1] - r[3]));

/** The shifts a unit's opening hours tile: ceil(span / lengthH) shifts of equal length, minutes of the day clock (to may be < from). */
export function shiftsOf(hours: [number, number], lengthH: number): { from: number; to: number }[] {
  const span = (hours[1] - hours[0] + 24) % 24 || 24, n = Math.ceil(span / lengthH - 1e-9), len = span / n;
  return Array.from({ length: n }, (_, i) => ({ from: Math.round((hours[0] + i * len) * 60) % 1440, to: Math.round((hours[0] + (i + 1) * len) * 60) % 1440 }));
}

export function buildScope(plan: Plan, block: any, life: any, rules: Rules, name: string, d: Derived = derive(plan)): { scope: Scope; civ: Civic; d: Derived } {
  if (!(SCOPES as readonly string[]).includes(name)) throw new Error(`unknown scope ${JSON.stringify(name)}; known: ${SCOPES.join(', ')}`);
  const civ = civic(plan.civic, block), toPlan = toPlanFn(plan.civic);
  const occ = life.generator.occupations as Record<string, { category: string; hours: [number, number] | 'service'; staffPerUnit: number; shifts: { lengthH: number } | string }>;
  const polyOf = (id: string) => d.plots.find(p => p.id === id)!.polyV;
  // plots: the plan's civic plots that one of the block's `blocks` stands on
  const plots = d.plots.filter(p => p.civic && p.civic in plan.civic.blocks).map(p => ({ id: p.id, area: p.area, use: p.use, civic: p.civic }));
  if (!plots.length) throw new Error('the plan has no civic plot for the block');
  const areas = [...new Set(plots.map(p => p.area))].sort();
  const places: Place[] = [], units: Unit[] = [];
  const add = (p: Place) => { places.push(p); return p.id; };
  const plotAt = (p: V) => plots.find(x => inPoly(p, polyOf(x.id)));
  // entrances and bays
  for (const e of civ.entrances) add({ id: e.id, kind: 'door', name: e.name, at: e.p, area: plotAt(e.p)?.area ?? areas[0]!, ...(plotAt(e.p) ? { plot: plotAt(e.p)!.id } : {}) });
  const bayPolys: Record<string, V[]> = {};
  for (const b of civ.bays) { const id = kebab(b.id), c = centroid(b.poly); bayPolys[id] = b.poly; add({ id, kind: 'bay', name: b.id, at: c, area: plotAt(c)?.area ?? areas[0]!, ...(plotAt(c) ? { plot: plotAt(c)!.id } : {}) }); }
  // the unit of each building on a scope plot
  const lengthH = (t: string) => { const s = occ[t]?.shifts; return typeof s === 'object' ? s.lengthH : 8; };
  for (const b of civ.buildings) {
    const plot = plotAt(centroid(b.poly)); if (!plot) throw new Error(`building ${b.id} stands on none of the scope's plots`);
    const entrances = civ.entrances.filter(e => e.building === b.id); if (!entrances.length) throw new Error(`building ${b.id} has no entrance`);
    const bay = places.filter(p => p.kind === 'bay').sort((p, q) => dist(p.at as V, centroid(b.poly)) - dist(q.at as V, centroid(b.poly)) || (p.id < q.id ? -1 : 1))[0]?.id;
    const employer = (life.generator.civicEmployers as { plot: string }[]).find(e => e.plot === plot.id);
    if (employer) {
      // staff: one per staffM2 of floor over the wings and the entrance towers (storeys = height / storey height), summed
      const upper = plan.standards.buildings.upperStorey as number, staffM2 = 30;
      const floors = (h: number) => Math.max(1, Math.round(h / upper));
      const m2 = area(b.poly) * floors(b.height) + (b.towers ?? []).reduce((n, t) => n + (t.poly ? area(t.poly) * floors(t.height) : 0), 0);
      const emp = employerOfBlock(rules, b.id)?.[0] ?? Object.entries(rules.temperamentsBy.employers).find(([, e]) => e.plot === plot.id)?.[0];
      if (!emp) throw new Error(`the rules name no employer for ${b.name} (${b.id}, plot ${plot.id})`);
      units.push({ id: emp, plot: plot.id, area: plot.area, trade: emp, employer: emp, category: 'civic', name: b.name, kind: 'civic', staffPerShift: Math.round(m2 / staffM2), hours: OFFICE_HOURS, shifts: shiftsOf(OFFICE_HOURS, 8), door: entrances[0]!.id, ...(bay ? { bay } : {}), building: b.id });
      continue;
    }
    // the rules map a block building to its employer and trade (the corner café is the diner); a building they do not name is rejected
    const mapped = employerOfBlock(rules, b.id), trade = mapped?.[1].trade;
    if (!mapped || !trade || !occ[trade]) throw new Error(`building ${b.id} (${b.name}) is neither a civic employer nor a trade the rules map`);
    const mix = plan.standards.frontage.mix[plot.area] as Record<string, number>;
    if (!(trade in mix)) throw new Error(`area ${plot.area} has no ${trade} in its frontage mix for ${b.name}`);
    const o = occ[trade]!, hours = o.hours as [number, number];
    units.push({ id: kebab(b.id), plot: plot.id, area: plot.area, trade, employer: mapped[0], category: o.category, name: b.name, kind: 'frontage', staffPerShift: o.staffPerUnit, hours, shifts: shiftsOf(hours, lengthH(trade)), door: entrances[0]!.id, ...(bay ? { bay } : {}), building: b.id });
  }
  // stops: the nearest tram stop and the nearest sky rail station to the scope's plots, within the plan's transit reach
  const rects = plots.map(p => plan.plots.find(x => x.id === p.id)!.rect!);
  const reach = plan.standards.transit.reach as number;
  const far = (p: V) => Math.min(...rects.map(r => rectDist(p, r)));
  const stops: Scope['stops'] = [];
  const service = life.transit.serviceHours as { from: number; to: number };
  for (const [mode, lines, key] of [['tram', plan.tram, 'stops'], ['skyrail', plan.skyrail, 'stations']] as const) {
    const all = lines.flatMap(l => ((l as any)[key] as any[]).map(s => ({ line: l.id, id: s.id as string, name: s.name as string, at: s.at as V })));
    const best = all.filter(s => far(s.at) <= reach).sort((a, b) => far(a.at) - far(b.at) || (a.line + a.id < b.line + b.id ? -1 : 1))[0];
    if (!best) continue;
    const id = kebab(`${best.line}-${best.id}`);
    add({ id, kind: 'stop', name: `${best.name} ${mode === 'tram' ? 'tram stop' : 'sky rail station'}`, at: best.at, area: areas[0]! });
    stops.push({ place: id, line: best.line, id: best.id, mode, name: best.name });
    const trade = mode === 'tram' ? 'tram-kiosk' : 'skyrail-kiosk', hours: [number, number] = [service.from, service.to % 24];
    units.push({ id: `${id}-kiosk`, plot: plots[0]!.id, area: areas[0]!, trade, employer: `${id}-kiosk`, category: 'kiosk', name: `${best.name} kiosk`, kind: 'kiosk', staffPerShift: occ[trade]!.staffPerUnit, hours, shifts: shiftsOf(hours, lengthH(trade)), door: id });
  }
  // seats: the block's tables; a seat inside a keep-clear zone is a place but nobody sits there off-shift
  const dress: any = civ.dressing ?? {};
  const zones = ((dress.keepClear ?? []) as any[]).map(z => ({
    id: z.id as string,
    test: z.ring ? (p: V) => { const c = toPlan(z.ring.centre), r = dist(p, c); return r >= z.ring.r0 && r <= z.ring.r1; } : (p: V) => inPoly(p, (z.polygon as V[]).map(toPlan)),
  }));
  const seats: string[] = [];
  for (const o of civ.objects.filter(x => x.kind === 'table').sort((a, b) => (a.id < b.id ? -1 : 1))) {
    const c = centroid(o.poly), pl = plotAt(c);
    add({ id: o.id, kind: 'seat', name: o.id, at: c, area: pl?.area ?? areas[0]!, ...(pl ? { plot: pl.id } : {}) });
    if (!zones.some(z => z.test(c))) seats.push(o.id);
  }
  // homes: life.generator.homes of the scope's areas within the transit reach of a scope plot; the door is on the face nearest the scope
  const centre = centroid(plots.flatMap(p => polyOf(p.id)));
  const homes: Home[] = [];
  for (const h of (life.generator.homes as { plot: string; area: string; residents: number }[]).filter(h => areas.includes(h.area)).sort((a, b) => (a.plot < b.plot ? -1 : 1))) {
    const r = plan.plots.find(x => x.id === h.plot)?.rect; if (!r || far([(r[0] + r[1]) / 2, (r[2] + r[3]) / 2]) > reach + Math.max(r[1] - r[0], r[3] - r[2])) continue;
    if (rects.every(q => rectDist([(r[0] + r[1]) / 2, (r[2] + r[3]) / 2], q) > reach + Math.max(r[1] - r[0], r[3] - r[2]) / 2)) continue;
    // nearest point of the home plot's edge to the scope centre, a metre outside
    const cx = Math.min(Math.max(centre[0], r[0]), r[1]), cy = Math.min(Math.max(centre[1], r[2]), r[3]);
    const out = Math.min(Math.abs(centre[0] - r[0]), Math.abs(centre[0] - r[1]), Math.abs(centre[1] - r[2]), Math.abs(centre[1] - r[3]));
    void out;
    const gx = centre[0] <= r[0] ? r[0] - 1 : centre[0] >= r[1] ? r[1] + 1 : cx, gy = centre[1] <= r[2] ? r[2] - 1 : centre[1] >= r[3] ? r[3] + 1 : cy;
    const id = `home-${h.plot}`;
    add({ id, kind: 'home', name: `${plan.plots.find(x => x.id === h.plot)?.name ?? h.plot} door`, at: [gx, gy], area: h.area, plot: h.plot });
    homes.push({ plot: h.plot, area: h.area, residents: h.residents, place: id });
  }
  // the café stop of a customer is a table: the eligible seat nearest the café's door (the door itself is a keep-clear zone)
  const cafeDoor = places.find(p => p.id === (units.find(u => u.kind === 'frontage' && u.trade === 'diner') ?? units.find(u => u.kind === 'frontage')!).door)!;
  const cafeUnit = (units.find(u => u.kind === 'frontage' && u.trade === 'diner') ?? units.find(u => u.kind === 'frontage')!).id;
  const cafe = [...seats].sort((a, b) => dist(places.find(p => p.id === a)!.at as V, cafeDoor.at as V) - dist(places.find(p => p.id === b)!.at as V, cafeDoor.at as V) || (a < b ? -1 : 1))[0] ?? cafeDoor.id;
  places.sort((a, b) => (a.id < b.id ? -1 : 1)); units.sort((a, b) => (a.id < b.id ? -1 : 1));
  return { scope: { id: name, areas, plots, stops, units, places, homes, bayPolys, zones, seats, speed: life.pedestrians.walkSpeed, cafe, cafeUnit }, civ, d };
}
