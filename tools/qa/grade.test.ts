// Brief 060 tier 2 smoke triage (Suture): the offline regrade of the bot's anomalies (grade.ts), on a small synthetic walk file and rows.
import { expect, test } from 'bun:test';
import { mkdirSync, mkdtempSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { feetShare, gradeGiver, gradeMem, gradeSolid, insideRun, isRace, loadWalk, offerOf, type FeetRow, type MemRow } from './grade';
import { findingsOf, gradeRows, type BotReportRow } from './bot';

// a 4 m × 4 m walk window at block (0, 0), 0.25 m cells, one solid cell column at block x 2.0-2.25 (i = 8); identity frame (Unity x = block x, z = block y)
function walkFile(dir: string): string {
  const nx = 16, ny = 16, kind = new Uint8Array(nx * ny); for (let j = 0; j < ny; j++) kind[8 * ny + j] = 2;
  const f = join(dir, 'walk.json');
  writeFileSync(f, JSON.stringify({ schema: 'suborned.walk/1', plan: 'p', world: 'w', window: { x0: 0, y0: 0, x1: 4, y1: 4 }, cell: 0.25, nx, ny, frame: { origin: [0, 0], x: [1, 0], y: [0, 1] }, floor: Buffer.from(new Int16Array(nx * ny).buffer).toString('base64'), kind: Buffer.from(kind).toString('base64'), counts: {} }));
  return f;
}
const at = (key: string, x: number, z: number, tick = 100, y = 0, floor = 0): FeetRow => ({ t: tick / 10, tick, key, x, y, z, floor });

test('Q-SOLID: a body overlapping a drawn solid by more than 5 cm is real; a figure clear of it, at a café chair or off the map is the check', () => {
  const w = loadWalk(walkFile(mkdtempSync(join(tmpdir(), 'qa-grade-'))))!;
  expect(w).toBeDefined();
  const chairs: [number, number][] = [[1, 3]];
  expect(gradeSolid({ key: 'security/1', tick: 100 }, [at('security/1', 1.9, 1)], w, chairs)!.cause).toBe('follower-offset'); // 10 cm from the solid: 15 cm overlap
  expect(gradeSolid({ key: 'crowd/7', tick: 100 }, [at('crowd/7', 1.9, 1)], w, chairs)!.cause).toBe('inside');
  expect(gradeSolid({ key: 'crowd/7', tick: 100 }, [at('crowd/7', 1.7, 1)], w, chairs)!.cause).toBe('margin'); // 30 cm clear
  expect(gradeSolid({ key: 'rivals/2', tick: 100 }, [at('rivals/2', 1.78, 1)], w, chairs)!.severity).toBe('info'); // 22 cm: 3 cm overlap, under the 5 cm tolerance
  expect(gradeSolid({ key: 'crowd/7', tick: 100 }, [at('crowd/7', 1.9, 3)], w, chairs)!.cause).toBe('cafe-seat'); // 0.9 m from a chair
  expect(gradeSolid({ key: 'crowd/7', tick: 100 }, [at('crowd/7', -30, 1)], w, chairs)!.cause).toBe('off-map');
  expect(gradeSolid({ key: 'crowd/7', tick: 100 }, [at('crowd/7', 1.7, 1, 400)], w, chairs)).toBeUndefined(); // no sample near the hit: the player's word stands
  expect(gradeSolid({ key: 'crowd/7', tick: 100 }, [at('crowd/7', 1.9, 1)], undefined, chairs)).toBeUndefined();
});

test('Q-SOLID (0.2.122 human-b): a crowd figure inside drawn solids for 3 s or more is on a straight routine leg (Keel); one beside the cast was stepped aside into the wall (Latch)', () => {
  const w = loadWalk(walkFile(mkdtempSync(join(tmpdir(), 'qa-grade-'))))!, chairs: [number, number][] = [];
  // walking through the solid column for 4 s (ticks 100-140), flagged at the wall's face 0.5 s before it went in
  const leg = [at('crowd/7', 1.9, 1, 95), ...Array.from({ length: 9 }, (_, k) => at('crowd/7', 2.1, 1 + k * 0.3, 100 + k * 5)), at('crowd/7', 2.6, 4, 145)];
  expect(insideRun(leg, w).s).toBeCloseTo(4, 5);
  expect(gradeSolid({ key: 'crowd/7', tick: 95 }, leg, w, chairs)!.cause).toBe('straight-leg');
  // the same figure passing a post 40 s later: still the straight leg (the run lies within 60 s before)
  expect(gradeSolid({ key: 'crowd/7', tick: 500 }, [...leg, at('crowd/7', 1.9, 2, 500)], w, chairs)!.cause).toBe('straight-leg');
  // standing 10 cm off the wall, 0.7 m from the guard: the host's give-way
  expect(gradeSolid({ key: 'crowd/8', tick: 100 }, [at('crowd/8', 1.9, 1), at('guard', 1.25, 0.7)], w, chairs)!.cause).toBe('give-way');
  // a crowd figure grazing the wall with nobody of the cast near and no long run inside: still 'inside'
  expect(gradeSolid({ key: 'crowd/8', tick: 100 }, [at('crowd/8', 1.9, 1), at('guard', 0.2, 3)], w, chairs)!.cause).toBe('inside');
  // a follower stays the follower offset, whatever else
  expect(gradeSolid({ key: 'rivals/1', tick: 95 }, leg.map(r => ({ ...r, key: 'rivals/1' })), w, chairs)!.cause).toBe('follower-offset');
});

test('Q-GIVER: Go offered by an agent in the car is the check (GDD §06: anyone, once someone is in it); other unable givers stay findings', () => {
  expect(gradeGiver({ observed: 'ring:enforcer in the car: offered for Go on car/getaway' })).toMatchObject({ cause: 'go-from-car', severity: 'info' });
  expect(gradeGiver({ observed: 'open-ring:enforcer in the car 0.7 s: still offered for Go on car/getaway' })!.cause).toBe('go-from-car');
  expect(gradeGiver({ observed: 'plan:fixer in the car: a giver of Go on car/getaway' })!.cause).toBe('go-from-car');
  expect(gradeGiver({ observed: 'panel:tech in the car: offered Go on car/getaway' })!.cause).toBe('go-from-car');
  expect(gradeGiver({ observed: 'ring:enforcer in the car: offered for Fire on unit/police-1' })).toBeUndefined();
  expect(gradeGiver({ observed: 'ring:scout down: offered for Go on car/getaway' })).toBeUndefined();
  expect(gradeGiver({ observed: 'open-ring:scout down 0.4 s: still offered for Board on car/getaway' })).toBeUndefined();
});

test('Q-MEM: a rise that holds is a leak; one step (the response spawning in), a warm-up dip or a short row is not', () => {
  const series = (f: (t: number) => number, s = 600, managed = (_t: number) => 30): MemRow[] => Array.from({ length: s }, (_, t) => ({ t, total: f(t) + (t % 37 === 0 ? 40 : 0), managed: managed(t) })); // a shot's spike every 37 s
  expect(gradeMem(series(t => 1250 + t * 0.1)).leak).toBe(true); // 6 MB/min, every third
  expect(gradeMem(series(t => 1280 + (t > 300 ? 25 : 0))).leak).toBe(false); // one step of 25 MB
  expect(gradeMem(series(t => (t < 90 ? 1250 : 1280))).leak).toBe(false); // the post-boot dip and refill
  expect(gradeMem(series(() => 1280, 600, t => 30 + t * 0.03)).leak).toBe(true); // managed 1.8 MB/min
  const short = gradeMem(series(t => 1250 + t * 0.2, 236)); expect(short.judged).toBe(false); expect(short.note).toContain('not judged');
});

test('Q-OFFER: the doer and code read through a target with colons; a doer down in the order tick or the one before is a race', () => {
  expect(offerOf('scout carry tech: agent-down the scout is down')).toEqual({ op: 'scout', verb: 'carry', code: 'agent-down' });
  expect(offerOf('tech fire camera:mast-314: no-target no camera:mast-314 to fire at')).toEqual({ op: 'tech', verb: 'fire', code: 'no-target' });
  expect(isRace([{ tick: 2049, code: 'scout.down' }], 'scout', 2049)).toBe(true);
  expect(isRace([{ tick: 2048, code: 'scout.down' }], 'scout', 2049)).toBe(true);
  expect(isRace([{ tick: 2030, code: 'scout.down' }], 'scout', 2049)).toBe(false);
  expect(isRace([{ tick: 2049, code: 'tech.down' }], 'scout', 2049)).toBe(false);
});

test('Q-FEET share: within 5 cm on 99.9% of samples, lifted and carried figures left out', () => {
  const ok = Array.from({ length: 2000 }, (_, i) => at('crowd/1', 1, 1, i, i === 5 ? 0.1 : 0));
  expect(feetShare(ok).ok).toBe(true);
  expect(feetShare([...ok, ...Array.from({ length: 10 }, (_, i) => at('crowd/2', 1, 1, i, 0.09))]).ok).toBe(false);
  expect(feetShare([...ok, ...Array.from({ length: 50 }, (_, i) => at('body/1', 1, 1, i, 1.3))]).ok).toBe(true); // on a shoulder: skipped
  expect(feetShare([...ok, ...Array.from({ length: 50 }, (_, i) => ({ ...at('body/1', 1, 1, i, 0.3), skip: true }))]).ok).toBe(true);
});

test('gradeRows: the row\'s anomalies regraded in place, the findings keyed by cause in plain words; a race replays to info', async () => {
  const dir = mkdtempSync(join(tmpdir(), 'qa-graderows-')), walk = walkFile(dir), seats = join(dir, 'seats.json'), rowDir = join(dir, 'random-a');
  mkdirSync(rowDir, { recursive: true }); writeFileSync(seats, JSON.stringify({ tables: { t: { chairs: [{ at: [100, 300] }] } } }));
  writeFileSync(join(rowDir, 'feet.csv'), 't,tick,key,x,y,z,floor,free\n' + [at('security/1', 1.9, 1), at('crowd/3', 1.7, 1)].map(r => `${r.t},${r.tick},${r.key},${r.x},${r.y},${r.z},${r.floor},0`).join('\n') + '\n');
  writeFileSync(join(rowDir, 'mem.csv'), 't,managedMB,totalMB\n' + Array.from({ length: 236 }, (_, t) => `${t},30,${1250 + t * 0.2}`).join('\n') + '\n');
  const A = (check: string, key: string, observed: string, script = '') => ({ check, key, title: 'x', expected: 'e', observed, tick: 100, clock: '0:10', at: [0, 0] as [number, number], shot: '', script });
  const row = { name: 'random-a', policy: 'random', seed: 5, status: 'ok', preset: 'hold', seconds: 236, endTick: 2286, outcome: 'failure', failCode: 'sealed', cardHead: '',
    input: { decisions: 0, clicks: 0, rings: 0, ringMisses: 0, taken: 0, moves: 0, runs: 0, pans: 0, turns: 0 }, orders: { given: 0, refused: 0, codes: {} }, frames: { count: 0, meanMs: 0, p99Ms: 0, maxMs: 0, over50: 0 },
    memory: { startMB: 0, endMB: 0, slopeMBPerMin: 12 }, consoleErrors: 0, feetSamples: 2, figures: 2, shots: 0, counts: {},
    anomalies: [A('Q-SOLID', 'security/1', 'security/1 at'), A('Q-SOLID', 'crowd/3', 'crowd/3 at'), A('Q-OFFER', 'carry:agent-down', 'scout carry tech: agent-down the scout is down', 'random-a/anomalies/1.script.json'),
      A('Q-OFFER', 'fire:no-target', 'tech fire camera:mast-314: no-target no camera'), A('Q-MEM', 'slope', 'managed 1 MB/min, total 12 MB/min')] } as BotReportRow;
  let asked: string[] = [];
  await gradeRows([row], dir, { walkFile: walk, seatsFile: seats, replay: async s => { asked = s; return [[{ tick: 100, code: 'scout.down' }]]; } });
  expect(asked).toEqual([join(dir, 'random-a/anomalies/1.script.json')]);
  const fs = findingsOf(row, 'random-a', dir), by = Object.fromEntries(fs.map(f => [f.key, f]));
  expect(by['Q-SOLID:follower-offset']!.severity).toBe('S3'); expect(by['Q-SOLID:margin']!.severity).toBe('info');
  expect(by['Q-OFFER:race']!.severity).toBe('info'); expect(by['Q-OFFER:fire:no-target']!.title).toBe('The ring offers Fire, then the game refuses it: there is nothing there to act on any more');
  expect(by['Q-MEM:not-judged']!.severity).toBe('info'); expect(fs.some(f => f.key === 'Q-MEM:slope')).toBe(false);
});
