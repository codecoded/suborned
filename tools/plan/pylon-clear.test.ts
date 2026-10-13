// standards.skyrail.pylonClear (brief 007 round 3, Sky's walk note "pillar in middle of steps"): opt-in, so a recorded plan places
// its pylons exactly as before, and the live plan keeps S1/trunk's pylons off the Crown monument.
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { dist } from './geom';
import { derive, type Plan } from './plan';

const root = resolve(import.meta.dir, '../..');
const read = (p: string): Plan => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const live = read('contracts/world/harbour-district.plan.json');
/** A04 run r008, round 3 (read-only): a plan without the standard. */
const recorded = read('runs/levels/harbour-district/r008/round-3/plan.json');
const tuple = (d: ReturnType<typeof derive>) => d.pylons.map(p => [p.line, p.path, +p.p[0].toFixed(3), +p.p[1].toFixed(3), +p.s.toFixed(2), +p.span.toFixed(2)]);
const monument = live.landmarks.find(l => l.id === 'monument')!;

/** What derive() placed for the recorded plan before pylonClear existed: [line, path, x, y, chainage, span]. */
const BEFORE = [
  ["S1",  "trunk",  -400,  0,  0,  0],
  ["S1",  "trunk",  -370,  0,  30,  30],
  ["S1",  "trunk",  -353.5,  0,  46.5,  16.5],
  ["S1",  "trunk",  -323.5,  0,  76.5,  30],
  ["S1",  "trunk",  -293.5,  0,  106.5,  30],
  ["S1",  "trunk",  -263.5,  0,  136.5,  30],
  ["S1",  "trunk",  -233.5,  0,  166.5,  30],
  ["S1",  "trunk",  -203.5,  0,  196.5,  30],
  ["S1",  "trunk",  -173.5,  0,  226.5,  30],
  ["S1",  "trunk",  -143.5,  0,  256.5,  30],
  ["S1",  "trunk",  -113.5,  0,  286.5,  30],
  ["S1",  "trunk",  -83.5,  0,  316.5,  30],
  ["S1",  "trunk",  -53.5,  0,  346.5,  30],
  ["S1",  "trunk",  -23.5,  0,  376.5,  30],
  ["S1",  "trunk",  6.5,  0,  406.5,  30],
  ["S1",  "trunk",  24,  0,  424,  17.5],
  ["S1",  "trunk",  54,  0,  454,  30],
  ["S1",  "trunk",  84,  0,  484,  30],
  ["S1",  "trunk",  114,  0,  514,  30],
  ["S1",  "trunk",  144,  0,  544,  30],
  ["S1",  "trunk",  174,  0,  574,  30],
  ["S1",  "trunk",  204,  0,  604,  30],
  ["S1",  "trunk",  234,  0,  634,  30],
  ["S1",  "trunk",  264,  0,  664,  30],
  ["S1",  "trunk",  287.5,  0,  687.5,  23.5],
  ["S1",  "trunk",  317.5,  0,  717.5,  30],
  ["S1",  "trunk",  347.5,  0,  747.5,  30],
  ["S1",  "trunk",  377.5,  0,  777.5,  30],
  ["S1",  "trunk",  407.5,  0,  807.5,  30],
  ["S1",  "trunk",  437.5,  0,  837.5,  30],
  ["S1",  "trunk",  467.5,  0,  867.5,  30],
  ["S1",  "trunk",  497.5,  0,  897.5,  30],
  ["S1",  "trunk",  527.5,  0,  927.5,  30],
  ["S1",  "trunk",  557.5,  0,  957.5,  30],
  ["S1",  "trunk",  575,  0,  975,  17.5],
  ["S1",  "loop",  592.596,  4.462,  17.73,  17.73],
  ["S1",  "loop",  608.107,  12.799,  35.48,  17.75],
  ["S1",  "loop",  620.06,  25.869,  53.21,  17.72],
  ["S1",  "loop",  633.922,  36.936,  71.08,  17.88],
  ["S1",  "loop",  651.206,  40.97,  88.98,  17.9],
  ["S1",  "loop",  668.535,  37.128,  106.88,  17.9],
  ["S1",  "loop",  682.479,  26.329,  124.65,  17.77],
  ["S1",  "loop",  690.932,  10.617,  142.64,  17.99],
  ["S1",  "loop",  691.712,  -7.208,  160.63,  17.99],
  ["S1",  "loop",  684.664,  -23.598,  178.63,  17.99],
  ["S1",  "loop",  671.612,  -35.505,  196.41,  17.79],
  ["S1",  "loop",  654.683,  -40.838,  214.31,  17.9],
  ["S1",  "loop",  637.113,  -38.321,  232.21,  17.9],
  ["S1",  "loop",  622.362,  -28.451,  250.11,  17.9],
  ["S1",  "loop",  610.613,  -15.172,  267.84,  17.73],
  ["S1",  "loop",  595.878,  -5.53,  285.59,  17.75],
  ["S1",  "loop",  578.44,  -3,  303.33,  17.74],
];

describe('standards.skyrail.pylonClear', () => {
  test('without the standard derive() places the pylons it always did (recorded A04 plan)', () => {
    expect(recorded.standards.skyrail.pylonClear).toBeUndefined();
    const d = derive(recorded);
    expect(d.pylonGaps).toEqual([]);
    expect(tuple(d)).toEqual(BEFORE);
  });

  test('without the standard a pylon stands in the monument disc (6.5, 0), the walk\'s fault', () => {
    const { pylonClear: _, ...skyrail } = live.standards.skyrail;
    const d = derive({ ...live, standards: { ...live.standards, skyrail } });
    expect(d.pylons.some(p => dist(p.p, monument.c) < monument.r)).toBe(true);
  });

  test('with the standard S1/trunk has none inside the monument disc, and every span is within its limit', () => {
    expect(live.standards.skyrail.pylonClear.landmark).toBe(0.8);
    const d = derive(live);
    expect(d.pylonGaps).toEqual([]);
    for (const p of d.pylons) expect(dist(p.p, monument.c)).toBeGreaterThanOrEqual(monument.r + 0.8 - 1e-6);
    const trunk = d.pylons.filter(p => p.line === 'S1' && p.path === 'trunk').map(p => p.p);
    expect(trunk.filter(p => Math.abs(p[1]) < 1 && Math.abs(p[0]) < 20)).toEqual([[-15, 0], [15, 0]]);
    for (const p of d.pylons) expect(p.span).toBeLessThanOrEqual(live.standards.skyrail.spanStraight + 1e-6);
  });
});
