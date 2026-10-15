// Builds the Handover-fix cast (tools/people/test/handover-fix.cast.json) from the promoted cast (r013 round 2): every row is r013's except records/records-office, whose
// positions, ties and routines are re-cut so that the Handover can be played (Suture 2026-10-15; Aster's four asks; Pulse). Not promoted: a live run writes the real one.
//   bun tools/people/test/make-handover-fix.ts
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Cast } from '../../../contracts/people/cast.schema';
const root = resolve(import.meta.dir, '../../..');
const base: Cast = JSON.parse(readFileSync(resolve(root, 'contracts/people/harbour-district.cast.json'), 'utf8'));
const rows = base.rows.map(r => {
  if (`${r.area}/${r.occupation}` !== 'records/records-office') return r;
  return {
    ...r,
    why: 'Civic office, one shift 9:30-17:30, 72 staff: 66 clerks; one councillor and his driver, by official car to the bay; four guards (door, service door, two on rounds) and a 24 h watch of two. Guards call at the cafe before and after.',
    positions: [
      { position: 'clerk', weight: 62, post: 'inside' as const },
      // exactly four guards a shift: the public door, the service door, two on rounds inside (counted at the official side); a watch of two outside office hours
      { position: 'guard', weight: 5, post: 'inside' as const, side: 'official' as const, count: 4, posts: [{ post: 'door' as const, count: 1 }, { post: 'official' as const, count: 1 }], watch: { count: 2 } },
      { position: 'official-driver', weight: 1, post: 'bay' as const, count: 1 },
      { position: 'councillor', weight: 1, post: 'inside' as const, side: 'official' as const, count: 1 },
    ],
    ties: [
      { kind: 'workmate', share: 0.7 },
      { kind: 'regular', share: 0.3, positions: ['clerk'] },
      { kind: 'regular', share: 1, positions: ['guard'], with: 'server', at: 'pre-shift' as const }, // the morning's server, whom he meets before his shift
      { kind: 'regular', share: 1, positions: ['guard'], with: 'server', at: 'evening' as const },   // the afternoon's, whom he meets after it (the watch's pre-shift stop is the same hour)
      // Inez and the door guard (brief 011, Sky's canon): a lover tie from the door guard to the afternoon's server, drawn along the regular tie above; only the door guard carries it
      { kind: 'lover' as const, share: 1, positions: ['guard'], posts: ['door' as const], with: 'server', at: 'evening' as const },
      { kind: 'workmate', share: 1, positions: ['official-driver'], with: 'councillor' },
      { kind: 'family', share: 0.3 },
    ],
    routines: [
      { id: 'guard-cafe-both-ends', skeleton: 'worker' as const, weight: 1, leaveBeforeMin: [0, 4], commute: 'walk' as const, meal: 'on-site' as const, preShift: 'cafe' as const, evening: 'square' as const, positions: ['guard'] },
      { id: 'staff-walk', skeleton: 'worker' as const, weight: 50, leaveBeforeMin: [0, 6], commute: 'walk' as const, meal: 'square' as const, preShift: 'none' as const, evening: 'home' as const, positions: ['clerk'] },
      { id: 'staff-tram', skeleton: 'worker' as const, weight: 50, leaveBeforeMin: [0, 4], commute: 'tram' as const, meal: 'on-site' as const, preShift: 'none' as const, evening: 'square' as const, positions: ['clerk'] },
      { id: 'official-car', skeleton: 'worker' as const, weight: 1, leaveBeforeMin: [0, 0], commute: 'car' as const, meal: 'on-site' as const, preShift: 'none' as const, evening: 'home' as const, positions: ['official-driver', 'councillor'] },
    ],
  };
});
const out = resolve(import.meta.dir, 'handover-fix.cast.json');
writeFileSync(out, JSON.stringify({ ...base, rows }, null, 1) + '\n');
console.log(`wrote ${out}`);
