// The plan timetable's journeys (Pulse, brief 075 remap): the boarding prices the timetable quotes for rides that change lines.
// Choice (Pulse): every ride from a stop or station to one on another line with one change at a plan interchange (a stop's `interchange`),
// both ways, neither end being the interchange itself (a ride that ends where it changes is a one-line ride, read off the runs).
// Arithmetic (code): tools/levels/timetable.ts deriveTimetable, the same sums the A04 transit pipeline uses (rides + dwells passed through +
// the interchange walk at 1.4 m/s; the wait is read off the headway, never in the sum), from the checker's chainages.
// The lines (runs) are Datum's (tools/plan/oldtown/transcribe.ts writes them); this tool only checks that the derived runs agree with them.
//   bun run life:journeys            writes plan.timetable.journeys (and its note), keeping every other byte of the plan
//   bun run life:journeys --check    exits 1 if the plan's journeys are not what the plan's lines and stops give
import { readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { inProcess } from '../levels/checker';
import { deriveTimetable } from '../levels/timetable';
import type { Pulse } from '../levels/schemas';

const ROOT = join(import.meta.dir, '..', '..');
export const PLAN_FILE = 'contracts/world/harbour-district.plan.json';
export const NOTE = 'journeys (Pulse, brief 075 remap): every ride with one change at a plan interchange, both ways, neither end the interchange; sums by tools/levels/timetable.ts (rides + dwells passed + walk at 1.4 m/s; the wait is the headway\'s, not in the sum). bun run life:journeys';

type Ref = { line: string; id: string; interchange?: string };
/** the journeys the plan's lines and interchanges allow (one change), in a fixed order */
export function proposal(plan: any): Pulse['journeys'] {
  const stops: Ref[] = [...(plan.tram ?? []).flatMap((l: any) => l.stops.map((s: any) => ({ line: l.id, id: s.id, interchange: s.interchange }))),
    ...(plan.skyrail ?? []).flatMap((l: any) => l.stations.map((s: any) => ({ line: l.id, id: s.id, interchange: s.interchange })))];
  const ref = (s: Ref) => `${s.line}/${s.id}`, byRef = new Map(stops.map(s => [ref(s), s]));
  const out: Pulse['journeys'] = [];
  for (const x of stops) { // x: the interchange stop on the first line, y = its partner on the second
    if (!x.interchange) continue; const y = byRef.get(x.interchange); if (!y || y.line === x.line) continue;
    for (const a of stops) if (a.line === x.line && ref(a) !== ref(x)) for (const b of stops) if (b.line === y.line && ref(b) !== ref(y))
      out.push({ legs: [{ from: ref(a), to: ref(x) }, { from: ref(y), to: ref(b) }] });
  }
  return out;
}

export async function derive(plan: any) {
  const lines = (plan.timetable?.lines ?? []).map((l: any) => ({ line: l.line, speed: l.speed, headwaySec: l.headwaySec, dwellSec: l.dwellSec }));
  const p: Pulse = { lines, journeys: proposal(plan), openQuestions: [], notes: [], sources: [], handoffs: [], answers: [] } as unknown as Pulse;
  const d = deriveTimetable(p, plan, await inProcess(plan, true) as any);
  const runs = (ls: any[]) => JSON.stringify(ls.map(l => [l.line, l.runs.map((r: any) => [r.from, r.to, r.sec])]).sort());
  if (runs(d.timetable.lines) !== runs(plan.timetable.lines)) d.faults.push(`the plan's runs are not what its stops give (derived ${runs(d.timetable.lines)}); the lines are Datum's (tools/plan/oldtown/transcribe.ts)`);
  return { journeys: d.timetable.journeys, faults: d.faults };
}

if (import.meta.main) {
  const file = join(ROOT, PLAN_FILE), text = readFileSync(file, 'utf8'), plan = JSON.parse(text);
  const { journeys, faults } = await derive(plan);
  if (faults.length) { console.error(faults.join('\n')); process.exit(1); }
  if (process.argv.includes('--check')) {
    const ok = JSON.stringify(plan.timetable.journeys) === JSON.stringify(journeys);
    console.log(ok ? `journeys: ${journeys.length}, as the plan's lines and stops give` : 'journeys differ from what the plan\'s lines and stops give: run bun run life:journeys');
    process.exit(ok ? 0 : 1);
  }
  const notes = [...(plan.timetable.notes ?? []).filter((n: string) => !n.startsWith('journeys (Pulse')), NOTE];
  plan.timetable = { ...plan.timetable, journeys, notes };
  const next = JSON.stringify(plan, null, 1) + '\n';
  if (next !== text) writeFileSync(file, next);
  console.log(`${next === text ? 'unchanged' : 'wrote'} ${PLAN_FILE}: ${journeys.length} journeys`);
  for (const j of journeys) console.log(`  ${j.from} -> ${j.to} ${j.sec} s via ${j.via.join(', ')}`);
}
