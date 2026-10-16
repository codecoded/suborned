// A scope: the part of the city a people run covers, defined from the civic block and the plan, never from a hand list (brief 010 B).
// `civic-block`: the plots the block's `blocks` stand on (the plan's civic plots), their areas, the buildings on them with their
// entrances and bays, the nearest tram stop and sky rail station, and the home plots within the plan's transit reach.
import { civic, toPlanFn, type Civic } from '../plan/civic';
import { area, centroid, dist, inPoly, type V } from '../plan/geom';
import { derive, type Derived, type Plan } from '../plan/plan';
import { blockersOf } from '../plan/walk';
import { bodyOf } from '../plan/walk-solids';
import type { Place } from '../../contracts/people/people.schema';
import { employerOfBlock, type Rules } from './rules';
import { cafeSets, seatId, seatPoints, standId, standingPoints, type Table } from './seats';

export const SCOPES = ['civic-block'] as const;
/** Residents who are simulated people per home plot; the rest of a plot is background (the density target tops the street up). [S] */
/** Standing points at a tram stop for the residents who run an errand there. [S] */
export const STOP_QUEUE = 24;
export const HOME_SAMPLE = 8;
/** Civic employers keep office hours until the shift chime (mission.json clock: 17:30; GDD §07). [S] */
export const OFFICE_HOURS: [number, number] = [9.5, 17.5];

export type Unit = {
  id: string; plot: string; area: string; trade: string; employer: string; category: string; name: string; kind: 'frontage' | 'civic' | 'kiosk';
  staffPerShift: number; hours: [number, number]; shifts: { from: number; to: number }[];
  door: string; bay?: string; building?: string;
  /** a civic employer's second entrance, the one nearest its bay: the service door the official car stands at (Pulse, Handover fix) */
  official?: string;
  /** a civic employer's watch: the 8 h shifts that tile the hours the office is shut; a cast row's `watch` positions work them */
  watch?: { from: number; to: number }[];
};
export type Home = { plot: string; area: string; residents: number; place: string };
export type Scope = {
  id: string; areas: string[]; plots: { id: string; area: string; use: string; civic?: string }[];
  stops: { place: string; line: string; id: string; mode: 'tram' | 'skyrail'; name: string }[];
  units: Unit[]; places: Place[]; homes: Home[];
  bayPolys: Record<string, V[]>; zones: { id: string; bars: string[]; test(p: V): boolean }[];
  /** the chairs a person may sit in off-shift: every chair of the café sets, bar a zone that bars tables (seats.ts) */
  seats: string[];
  /** the café's tables with a chair a person may take, nearest the café door first; `seats` are their chair places in the order they are taken */
  tables: Table[];
  /** every café set the world draws, with its chairs (place ids and positions): what P-13 holds the seats to */
  sets: { id: string; piece?: string; at: V; yaw: number; seats: { place: string; at: V }[] }[];
  /** the standing points beside the café (kind 'stand'), nearest its door first: taken when every chair is held */
  stands: string[];
  /** the places that are the café's customers' side: its chairs and its standing points (see atCafeSide) */
  cafeSide: string[];
  /** the walk speed (m/s) the people are timed by */
  speed: number;
  /** the first chair of the table nearest the café's door: where a pre-shift stop is aimed, and the placeholder before seats are shared out */
  cafe: string;
  /** the unit whose door the café's seats stand by */
  cafeUnit: string;
  /** the counter window (Pulse, brief 014): a stop at it takes a standing point nearest the café's door; a customer who takes a coffee to go stands here */
  counter: Table;
  /** the standing points at the tram stop (kind 'stand', `<stop>-stand-NN`): a resident's errand there takes one, so a stop is a queue and never one point */
  stopTable?: Table;
  /** how the café's customers come and go (life.json: the food category's draw by band, its dwell): a resident's visits to the café are drawn from it (day.ts visitTimes) */
  food: { bands: { from: number; to: number; draw: number }[]; dwellMin: number };
};

/** A stop at the café unit's customers' side, for a regular tie: before the shift, at one of its chairs or standing points (§05 the guard's café stop; §08 the café keeper tips on the
 *  guard). A meal or an evening at a café chair is a customer's, and counts for no tie unless the cast row aims a regular tie at it (`Tie.at: 'evening'`, Pulse 2026-10-15: the
 *  day door guard who stops at the café after his shift is a regular of the afternoon's server, who is on at 17:32); `visits` says which stops count. One definition for the
 *  expander, P-7 and the review. */
export const atCafeSide = (scope: Pick<Scope, 'cafeSide'>, place: string, doing: string, visits: readonly string[] = ['pre-shift']) => visits.includes(doing) && scope.cafeSide.includes(place);

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
      // the service door: the entrance other than the public one that is nearest the bay (the councillor's official car stands there)
      const bayAt = bay ? (places.find(p => p.id === bay)!.at as V) : (centroid(b.poly) as V);
      const official = entrances.slice(1).sort((p, q) => dist(p.p, bayAt) - dist(q.p, bayAt) || (p.id < q.id ? -1 : 1))[0]?.id;
      units.push({ id: emp, plot: plot.id, area: plot.area, trade: emp, employer: emp, category: 'civic', name: b.name, kind: 'civic', staffPerShift: Math.round(m2 / staffM2), hours: OFFICE_HOURS, shifts: shiftsOf(OFFICE_HOURS, 8), door: entrances[0]!.id, ...(official ? { official } : {}), watch: shiftsOf([OFFICE_HOURS[1], OFFICE_HOURS[0]], 8), ...(bay ? { bay } : {}), building: b.id });
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
  // seats: the chairs of the café sets the world draws on the block's tables (seats.ts). A zone bars the kinds it lists and none lists a table, so a chair
  // stands where its table does; a zone that did bar tables would take its chairs out of `seats`.
  const dress: any = civ.dressing ?? {};
  const zones = ((dress.keepClear ?? []) as any[]).map(z => ({
    id: z.id as string, bars: (z.bars ?? []) as string[],
    test: z.ring ? (p: V) => { const c = toPlan(z.ring.centre), r = dist(p, c); return r >= z.ring.r0 && r <= z.ring.r1; } : (p: V) => inPoly(p, (z.polygon as V[]).map(toPlan)),
  }));
  const sets = cafeSets(civ), seats: string[] = [], eligible = (p: V) => !zones.some(z => z.bars.includes('table') && z.test(p));
  const setsOut: Scope['sets'] = [];
  for (const o of sets) {
    const pts = seatPoints(o), ids: { place: string; at: V }[] = [];
    pts.forEach((c, k) => {
      const pl = plotAt(c), id = seatId(o.id, k);
      add({ id, kind: 'seat', name: `${o.id} seat ${k}`, at: c, area: pl?.area ?? areas[0]!, ...(pl ? { plot: pl.id } : {}) });
      ids.push({ place: id, at: c });
    });
    setsOut.push({ id: o.id, ...(o.piece ? { piece: o.piece } : {}), at: o.at, yaw: o.yaw, seats: ids });
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
  // the café's customers sit at its tables: the tables nearest the café's door first; beside them, standing points that keep every keep-clear zone clear
  const cafeUnitRow = units.find(u => u.kind === 'frontage' && u.trade === 'diner') ?? units.find(u => u.kind === 'frontage')!;
  const cafeDoor = places.find(p => p.id === cafeUnitRow.door)!, cafeUnit = cafeUnitRow.id;
  const tables: Table[] = setsOut.map(o => ({ id: o.id, at: o.at, seats: o.seats.filter(x => eligible(x.at)).map(x => x.place) })).filter(t => t.seats.length)
    .sort((a, b) => dist(a.at, cafeDoor.at as V) - dist(b.at, cafeDoor.at as V) || (a.id < b.id ? -1 : 1));
  for (const t of tables) seats.push(...t.seats);
  const { polys, rounds } = blockersOf(plan, d, civ), bays = Object.values(bayPolys);
  const free = (p: V) => !polys.some(b => inPoly(p, b)) && !rounds.some(l => dist(p, l.c) < l.r);
  const clear = (p: V) => !zones.some(z => z.test(p)) && !bays.some(b => inPoly(p, b));
  const stands = standingPoints(civ, cafeDoor.at as V, setsOut.flatMap(o => o.seats.map(x => x.at)).concat(sets.map(o => o.at)), free, clear, bodyOf(plan)), standIds: string[] = [];
  stands.forEach((c, k) => { const pl = plotAt(c), id = standId(k); add({ id, kind: 'stand', name: `café standing point ${k}`, at: c, area: pl?.area ?? areas[0]!, ...(pl ? { plot: pl.id } : {}) }); standIds.push(id); });
  const cafe = tables[0]?.seats[0] ?? cafeDoor.id;
  const foodCat = life.pedestrians.category.food as { dwell: number; draw: Record<string, number> };
  const food = { bands: (life.clock.bands as { id: string; from: number; to: number }[]).map(b => ({ from: b.from, to: b.to, draw: foodCat.draw[b.id] ?? 0 })), dwellMin: Math.round(foodCat.dwell / 60) };
  const counter: Table = { id: 'cafe-counter', at: cafeDoor.at as V, seats: [], standing: standIds };
  // the tram stop's queue: standing points around the stop, on free ground clear of every zone and bay (the café's lattice, centred on the stop)
  let stopTable: Table | undefined;
  const tramStop = stops.find(s => s.mode === 'tram');
  if (tramStop) {
    const sp = places.find(p => p.id === tramStop.place)!, ids: string[] = [];
    standingPoints(civ, sp.at as V, [], free, clear, bodyOf(plan), STOP_QUEUE).forEach((c, k) => { const pl = plotAt(c), id = `${sp.id}-stand-${String(k).padStart(2, '0')}`; add({ id, kind: 'stand', name: `${sp.name} queue ${k}`, at: c, area: pl?.area ?? sp.area, ...(pl ? { plot: pl.id } : {}) }); ids.push(id); });
    if (ids.length) stopTable = { id: sp.id, at: sp.at as V, seats: [], standing: ids };
  }
  places.sort((a, b) => (a.id < b.id ? -1 : 1)); units.sort((a, b) => (a.id < b.id ? -1 : 1));
  return { scope: { id: name, areas, plots, stops, units, places, homes, bayPolys, zones, seats, tables, sets: setsOut, stands: standIds, cafeSide: [...setsOut.flatMap(o => o.seats.map(x => x.place)), ...standIds].sort(), speed: life.pedestrians.walkSpeed, cafe, cafeUnit, counter, ...(stopTable ? { stopTable } : {}), food }, civ, d };
}
