// Who sits where in the café (Pulse, brief 022 follow-up; Latch asked, for Forma's `sit` clip): a view-only bake, read by the Unity view and by nothing in the engine.
// Reads the people the engine already reads (sim/data/handover.people.json: a stop at a place `cafe-table-N-seat-K` is a person seated on chair K of table N, from tick `from` to tick `to`,
// 10 Hz from mission 0, `out` 1 = on the street) and the places the engine already holds (the seat's centimetres, tools/people/seats.ts from the café set's chair circle). It adds no place, no
// stop and no number to any file the engine hashes: the engine plays the same walk to the same point whether or not this file exists, so no route, scenario or hash moves.
// Chair K of a table is the K-th angle of the café set (-60, 60, 180 degrees off the table's seeded yaw, radius 72 cm); it faces the table centre.
// The four cast roles are left out as the rounds' occupancy leaves them out: the engine plays them by role, and their raw seats in the people file are never played.
// Same inputs, same bytes. Run: bun run seats [--out <file>] [--check]   (default out contracts/people/handover.seats.json; --check exits 1 if the file is not what the inputs make)
import { mkdirSync, existsSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { PEOPLE, SITE, BLOCK, Hz, pretty, readInputs, type Inputs } from './rounds';
import type { Pt } from './walk';

const root = join(import.meta.dir, '..', '..');
export const SEATS_FILE = 'contracts/people/handover.seats.json';
const SEAT = /^(cafe-table-\d+)-seat-(\d+)$/;

export type Chair = { chair: number; at: Pt; face: Pt; yawDeg: number }; // at: the chair's centre; face: unit vector x100 toward the table; yawDeg: atan2(face y, face x) in whole degrees, block frame (x south, y east)
export type Sit = { table: string; chair: number; from: number; to: number }; // ticks from mission 0 (10 Hz), the engine's stop window at that chair
export type Seats = {
  schema: 'suborned.view.seats/1'; from: string[]; frame: string;
  tables: Record<string, { at: Pt; chairs: Chair[] }>;
  seated: Record<string, Sit[]>;
};

const unit = (v: Pt): Pt => { const l = Math.hypot(v[0], v[1]) || 1; return [Math.round((v[0] / l) * 100), Math.round((v[1] / l) * 100)]; };

export function build(inp: Inputs = readInputs()): { seats: Seats; text: string } {
  const { people, site, tables, rules } = inp, horizon = rules.rounds.horizonS.v * Hz;
  const cast = new Set(Object.values(site.cast).map((c) => c.id).filter((x): x is string => typeof x === 'string'));
  const T: Seats['tables'] = {};
  for (const [pl, at] of Object.entries(people.places).sort(([a], [b]) => (a < b ? -1 : 1))) {
    const m = SEAT.exec(pl); if (!m) continue;
    const c = tables[m[1]!]; if (!c) throw new Error(`${pl}: no table ${m[1]} in the civic block`);
    const f = unit([c[0] - at[0], c[1] - at[1]]);
    (T[m[1]!] ??= { at: c, chairs: [] }).chairs.push({ chair: Number(m[2]), at, face: f, yawDeg: Math.round((Math.atan2(f[1], f[0]) * 180) / Math.PI) });
  }
  for (const t of Object.values(T)) t.chairs.sort((a, b) => a.chair - b.chair);
  const seated: Seats['seated'] = {};
  for (const p of [...people.people].sort((a, b) => (a.id < b.id ? -1 : 1))) {
    if (cast.has(p.id)) continue;
    for (const [pl, from, to, out] of p.stops) {
      const m = SEAT.exec(pl);
      if (!m || out !== 1 || to <= 0 || from >= horizon) continue; // a seat on the street, held at or after tick 0 and inside the mission's horizon
      (seated[p.id] ??= []).push({ table: m[1]!, chair: Number(m[2]), from, to });
    }
  }
  const seats: Seats = {
    schema: 'suborned.view.seats/1', from: [PEOPLE, SITE, BLOCK, 'tools/routines/seats.ts'],
    frame: 'integer centimetres, block frame (x south, y east), as sim/data/handover.site.json; ticks are 10 Hz from mission 0. View only: no engine reader, not hashed. A person in `seated` is at `chairs[chair].at` of `table` from `from` to `to` (the engine walks them there); draw them sitting, facing `face`.',
    tables: T, seated,
  };
  return { seats, text: pretty(seats) + '\n' };
}

if (import.meta.main) {
  const argv = process.argv.slice(2), oi = argv.indexOf('--out'), check = argv.includes('--check');
  const file = join(root, oi >= 0 ? argv[oi + 1]! : SEATS_FILE), { seats, text } = build();
  if (check) {
    const have = existsSync(file) ? readFileSync(file, 'utf8') : '';
    if (have !== text) { console.error(`${file} is not what the inputs make (bun run seats)`); process.exit(1); }
    console.log('ok  the seats file is what the inputs make');
  } else { mkdirSync(dirname(file), { recursive: true }); writeFileSync(file, text); console.log(`wrote ${file} (${text.length} bytes)`); }
  console.log(`tables: ${Object.keys(seats.tables).length}; seated people: ${Object.keys(seats.seated).length}; sits: ${Object.values(seats.seated).reduce((n, s) => n + s.length, 0)}`);
}
