// Applies the three proposals to a copy of the plan. Pure: same inputs, same plan. Anything it cannot apply is a
// `problem` the critic sees, never a silent fix.
import { derive } from '../plan/plan';
import type { Lines, Stops, Timetable } from './schemas';

export type Proposals = { datum?: Lines; vantage?: Stops; pulse?: Timetable };
export type Applied = { plan: any; problems: string[] };

export function apply(input: any, p: Proposals, version: string): Applied {
  const plan = structuredClone(input) as any, problems: string[] = [];
  plan.version = version; plan.status = 'candidate (generated, not promoted)';
  if (plan.standards?.transit) delete plan.standards.transit.waived;
  const arr = (mode: string) => (mode === 'tram' ? plan.tram : plan.skyrail) as any[];
  const find = (id: string) => [...plan.tram, ...plan.skyrail].find((l: any) => l.id === id);

  for (const id of p.datum?.remove ?? []) {
    for (const k of ['tram', 'skyrail']) { const i = plan[k].findIndex((l: any) => l.id === id); if (i >= 0) plan[k].splice(i, 1); }
  }
  for (const l of p.datum?.lines ?? []) {
    const other = arr(l.mode === 'tram' ? 'sky' : 'tram').find((x: any) => x.id === l.id);
    if (other) { problems.push(`line ${l.id} already exists as ${l.mode === 'tram' ? 'sky rail' : 'tram'}`); continue; }
    const list = arr(l.mode), old = list.find((x: any) => x.id === l.id);
    const next: any = { id: l.id, name: l.name, beyond: l.beyond, paths: l.paths, ...(l.order.length ? { order: l.order } : {}) };
    const key = l.mode === 'tram' ? 'stops' : 'stations';
    next[key] = old?.[key] ?? [];
    const prev = [...list];
    if (old) list[list.indexOf(old)] = next; else list.push(next);
    try { derive(plan); } catch (e) { list.splice(0, list.length, ...prev); problems.push(`line ${l.id} cannot be built, so it was not applied: ${e instanceof Error ? e.message : e}`); }
  }
  if (p.vantage) {
    const ground = new Set([...plan.roads.map((r: any) => r.id), ...plan.foot.map((w: any) => w.id)]), by = new Map<string, any[]>();
    for (const s of p.vantage.stops) {
      if (!find(s.line)) { problems.push(`stop ${s.line}/${s.id}: no such line`); continue; }
      if (!ground.has(s.fronts)) problems.push(`stop ${s.line}/${s.id} fronts unknown road or foot path ${s.fronts}`);
      const { line: _l, ...rest } = s;
      by.set(s.line, [...(by.get(s.line) ?? []), rest]);
    }
    for (const [id, stops] of by) { const l = find(id); l[plan.tram.includes(l) ? 'stops' : 'stations'] = stops; }
  }
  if (p.pulse) {
    const known = new Set([...plan.tram, ...plan.skyrail].flatMap((l: any) => [...(l.stops ?? l.stations ?? [])].map((s: any) => `${l.id}/${s.id}`)));
    for (const r of [...p.pulse.lines.flatMap(l => l.runs), ...p.pulse.journeys]) for (const x of [r.from, r.to]) if (!known.has(x)) problems.push(`timetable names unknown stop ${x}`);
    for (const l of p.pulse.lines) if (!find(l.line)) problems.push(`timetable names unknown line ${l.line}`);
    const { sources: _s, ...timetable } = p.pulse; plan.timetable = timetable;
  }
  return { plan, problems };
}

/** station-on-plot: moves one station (sky rail first) onto the nearest plot's centre, so the critic has a fault to find. */
export function seedStationOnPlot(v: Stops, plan: any, plots: { id: string; centre: [number, number] }[]): { out: Stops; note: string } {
  const sky = new Set(plan.skyrail.map((l: any) => l.id)), i = v.stops.findIndex(s => sky.has(s.line) && !s.interchange);
  if (i < 0) throw new Error('seed station-on-plot: Vantage proposed no sky rail station without an interchange, so there is nothing to move');
  const s = v.stops[i]!, plot = plots.reduce((b, q) => (Math.hypot(q.centre[0] - s.at[0], q.centre[1] - s.at[1]) < Math.hypot(b.centre[0] - s.at[0], b.centre[1] - s.at[1]) ? q : b));
  const out = structuredClone(v); out.stops[i] = { ...s, at: [Math.round(plot.centre[0]), Math.round(plot.centre[1])] };
  return { out, note: `moved ${s.line}/${s.id} from (${s.at}) onto plot ${plot.id} at (${out.stops[i]!.at})` };
}
