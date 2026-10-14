// Builds the Handover fixture cast from r013's accepted cast: every row is r013's except records/records-office,
// whose positions, ties and routines are re-cut so the Handover can be played (Suture 2026-10-15; Aster's four asks).
// usage: bun briefs/010/pulse/make-fixture.ts  -> briefs/010/pulse/handover-fix.v1.cast.json (current vocabulary only)
//                                          briefs/010/pulse/handover-fix.v2.cast.json (with the additions in contracts/people/cast.schema.ts of the prototype)
import { readFileSync, writeFileSync } from 'node:fs';
const base = JSON.parse(readFileSync('briefs/010/r013.cast.json', 'utf8'));
const clone = <T>(x: T): T => JSON.parse(JSON.stringify(x));
const ROW = 'records/records-office';
const idx = base.rows.findIndex((r: any) => `${r.area}/${r.occupation}` === ROW);
const old = base.rows[idx];

const needs = old.needs; // unchanged: the driver weights cash alone; councillors, clerks and guards share the others

// ---- v2: the new vocabulary
const v2 = clone(base); const row = clone(old);
row.why = 'Civic office, one shift 9:30-17:30, 72 staff: 66 clerks; one councillor and his driver, by official car to the bay; four guards (door, service door, two on rounds) and a 24 h watch of two. Guards call at the cafe before and after.';
row.positions = [
  { position: 'clerk', weight: 62, post: 'inside' },
  { position: 'guard', weight: 5, post: 'inside', side: 'official', count: 4, posts: [{ post: 'door', count: 1 }, { post: 'official', count: 1 }], watch: { count: 2 } },
  { position: 'official-driver', weight: 1, post: 'bay', count: 1 },
  { position: 'councillor', weight: 1, post: 'inside', side: 'official', count: 1 },
];
row.needs = needs;
row.ties = [
  { kind: 'workmate', share: 0.7 },
  { kind: 'regular', share: 0.3, positions: ['clerk'] },
  { kind: 'regular', share: 1, positions: ['guard'], with: 'server', count: 2 }, // one aimed at the stop before the shift (the morning's server), one at the stop after it (the afternoon's)
  { kind: 'workmate', share: 1, positions: ['official-driver'], with: 'councillor' },
  { kind: 'family', share: 0.3 },
];
row.routines = [
  { id: 'guard-cafe-both-ends', skeleton: 'worker', weight: 1, leaveBeforeMin: [0, 4], commute: 'walk', meal: 'on-site', preShift: 'cafe', evening: 'square', positions: ['guard'] },
  { id: 'staff-walk', skeleton: 'worker', weight: 50, leaveBeforeMin: [0, 6], commute: 'walk', meal: 'square', preShift: 'none', evening: 'home', positions: ['clerk'] },
  { id: 'staff-tram', skeleton: 'worker', weight: 50, leaveBeforeMin: [0, 4], commute: 'tram', meal: 'on-site', preShift: 'none', evening: 'square', positions: ['clerk'] },
  { id: 'official-car', skeleton: 'worker', weight: 1, leaveBeforeMin: [0, 0], commute: 'car', meal: 'on-site', preShift: 'none', evening: 'home', positions: ['official-driver', 'councillor'] },
];
v2.rows[idx] = row;
writeFileSync('briefs/010/pulse/handover-fix.v2.cast.json', JSON.stringify(v2, null, 1));

// ---- v1: the vocabulary the expander has today (no count, posts, side, watch, car, aimed ties)
const v1 = clone(base); const r1 = clone(old);
r1.why = 'Civic office, one day shift 9:30-17:30, 72 staff. Mostly clerks inside; one guard at the door, who calls at the cafe before the shift and after it; one councillor; one official driver at the bay, on cash.';
r1.positions = [ // weights chosen so the quota gives exactly one guard, one driver, one councillor (every position gets one first; 100:0.1:0.1:0.1 leaves the rest to the clerks)
  { position: 'clerk', weight: 100, post: 'inside' },
  { position: 'guard', weight: 0.1, post: 'door' },
  { position: 'official-driver', weight: 0.1, post: 'bay' },
  { position: 'councillor', weight: 0.1, post: 'inside' },
];
r1.ties = [{ kind: 'workmate', share: 0.7 }, { kind: 'regular', share: 0.7 }, { kind: 'family', share: 0.3 }, { kind: 'rival', share: 0.1 }];
r1.routines = [
  { id: 'guard-cafe-both-ends', skeleton: 'worker', weight: 1, leaveBeforeMin: [0, 4], commute: 'walk', meal: 'on-site', preShift: 'cafe', evening: 'square', positions: ['guard'] },
  { id: 'staff-walk', skeleton: 'worker', weight: 50, leaveBeforeMin: [0, 6], commute: 'walk', meal: 'square', preShift: 'none', evening: 'home', positions: ['clerk', 'official-driver', 'councillor'] },
  { id: 'staff-tram', skeleton: 'worker', weight: 50, leaveBeforeMin: [0, 4], commute: 'tram', meal: 'on-site', preShift: 'none', evening: 'square', positions: ['clerk', 'official-driver', 'councillor'] },
];
v1.rows[idx] = r1;
writeFileSync('briefs/010/pulse/handover-fix.v1.cast.json', JSON.stringify(v1, null, 1));
console.log('wrote v1 and v2');
