// The derived timetable (run.json `timetable: 'derived'`). Pulse proposes the choices; code does the arithmetic.
//   run sec      = ceil(gap ÷ speed), gap = the checker's "to next" in whole metres, speed = the line's `speed` (m/s)
//   journey sec  = rides + dwells + interchange walks (the wait for a service is NOT in it: it is read off the headway)
//     ride       = sum of the run secs between a leg's two stops (either direction on the line)
//     dwell      = the line's dwell at every stop passed through inside a leg (not at the boarding or the alighting stop)
//     walk       = ceil(straight-line metres between the two paired stops ÷ WALK_SPEED) at each interchange
// Pulse's own sums are never read: a stale value cannot survive a round in which stops moved.
import { inProcess } from './checker';
import { apply, type Proposals } from './apply';
import type { Pulse, Timetable } from './schemas';

export const WALK_SPEED = 1.4; // m/s [S] starting value: the GDD states no walking pace; tune by playtest
type Stop = { line: string; id: string; at: number[]; chainage: number; next?: number | null; interchange?: string };
export type Derived = { timetable: Timetable; faults: string[] };

export function deriveTimetable(p: Pulse, plan: any, report: { stops: any[]; stations: any[] }): Derived {
  const faults: string[] = [], ics = new Map<string, string | undefined>();
  for (const l of [...plan.tram, ...plan.skyrail]) for (const s of l.stops ?? l.stations ?? []) ics.set(`${l.id}/${s.id}`, s.interchange);
  const by = new Map<string, Stop[]>();
  for (const s of [...report.stops, ...report.stations] as Stop[]) by.set(s.line, [...(by.get(s.line) ?? []), { ...s, interchange: ics.get(`${s.line}/${s.id}`) }]);
  for (const v of by.values()) v.sort((a, b) => a.chainage - b.chainage);
  const prop = new Map<string, Pulse['lines'][number]>();
  for (const l of p.lines) {
    if (!by.has(l.line)) faults.push(`timetable names unknown line ${l.line}`);
    else if (prop.has(l.line)) faults.push(`line ${l.line} appears twice in the timetable`);
    else prop.set(l.line, l);
  }
  for (const id of by.keys()) if (!prop.has(id)) faults.push(`line ${id} has no speed, dwell and headway`);
  const sec = new Map<string, number>(); // "<line>/<from>" → run sec to the next stop
  const lines: Timetable['lines'] = [];
  for (const [id, stops] of by) {
    const l = prop.get(id); if (!l) continue;
    const runs: Timetable['lines'][number]['runs'] = [];
    stops.forEach((s, i) => { const n = stops[i + 1]; if (!n) return; const t = Math.ceil(Math.round(s.next ?? n.chainage - s.chainage) / l.speed); sec.set(`${id}/${s.id}`, t); runs.push({ from: `${id}/${s.id}`, to: `${id}/${n.id}`, sec: t }); });
    lines.push({ line: id, headwaySec: l.headwaySec, dwellSec: l.dwellSec, speed: l.speed, runs } as Timetable['lines'][number]);
  }
  const at = (ref: string) => { const [li, id] = ref.split('/'); const i = (by.get(li!) ?? []).findIndex(s => s.id === id); return i < 0 ? undefined : { li: li!, i, s: by.get(li!)![i]! }; };
  const journeys: Timetable['journeys'] = [];
  p.journeys.forEach((j, n) => {
    const name = `journey ${n + 1} (${j.legs[0]!.from} to ${j.legs.at(-1)!.to})`, before = faults.length;
    let total = 0; const via: string[] = [];
    j.legs.forEach((g, k) => {
      const a = at(g.from), b = at(g.to);
      for (const [r, x] of [[g.from, a], [g.to, b]] as const) if (!x) faults.push(`${name}: leg ${k + 1} names unknown stop ${r}`);
      if (!a || !b) return;
      if (a.li !== b.li) { faults.push(`${name}: leg ${k + 1} runs ${g.from} to ${g.to}, which are on different lines (change lines with a new leg)`); return; }
      if (a.i === b.i) { faults.push(`${name}: leg ${k + 1} starts and ends at ${g.from}`); return; }
      const l = prop.get(a.li); if (!l) return;
      const [lo, hi] = a.i < b.i ? [a.i, b.i] : [b.i, a.i], st = by.get(a.li)!;
      for (let i = lo; i < hi; i++) total += sec.get(`${a.li}/${st[i]!.id}`)!;
      total += (hi - lo - 1) * l.dwellSec;
      if (k) {
        const prev = at(j.legs[k - 1]!.to);
        if (prev) {
          const pr = `${j.legs[k - 1]!.to}`, paired = prev.s.interchange === g.from || a.s.interchange === pr;
          if (prev.li === a.li) faults.push(`${name}: leg ${k + 1} stays on line ${a.li}; a via must change lines`);
          else if (pr === g.from) faults.push(`${name}: via ${pr} is the start of the next leg, not an interchange`);
          else if (!paired) faults.push(`${name}: ${pr} to ${g.from} is not an interchange pair in the plan`);
          else { total += Math.ceil(Math.hypot(prev.s.at[0]! - a.s.at[0]!, prev.s.at[1]! - a.s.at[1]!) / WALK_SPEED); via.push(pr); }
        }
      }
    });
    if (faults.length === before) journeys.push({ from: j.legs[0]!.from, to: j.legs.at(-1)!.to, sec: total, via });
  });
  return { timetable: { lines, journeys, openQuestions: p.openQuestions, notes: p.notes, sources: p.sources, handoffs: p.handoffs } as Timetable, faults };
}

export type AppliedTimetable = { plan: any; problems: string[]; timetable?: Timetable; faults: string[] };
/** apply() with Pulse's proposal turned into a derived timetable first; Pulse's own `runs` and sums do not exist in the schema. */
export async function applyDerived(input: any, outs: Omit<Proposals, 'pulse'> & { pulse?: Pulse }, version: string, block?: unknown): Promise<AppliedTimetable> {
  const base = apply(input, { datum: outs.datum, vantage: outs.vantage }, version);
  if (!outs.pulse) return { ...base, faults: [] };
  if (base.problems.some(x => x.startsWith('line '))) return { ...base, faults: [] };
  let d: Derived;
  try { d = deriveTimetable(outs.pulse, base.plan, await inProcess(base.plan, true, block)); }
  catch (e) { return { ...base, problems: [...base.problems, `timetable not derived: ${e instanceof Error ? e.message : e}`], faults: [] }; }
  const r = apply(input, { datum: outs.datum, vantage: outs.vantage, pulse: d.timetable }, version);
  return { ...r, timetable: d.timetable, faults: d.faults };
}
