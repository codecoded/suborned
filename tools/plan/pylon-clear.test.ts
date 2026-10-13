// standards.skyrail.pylonClear (brief 007 round 3, Sky's walk note "pillar in middle of steps"): opt-in, so a recorded plan places
// its pylons exactly as before, and the live plan keeps S1/trunk's pylons off the Crown monument.
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { dist } from './geom';
import { derive, spanMax, type Plan } from './plan';

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

  /** The live plan with the pylonClear keys given (and no others) in place of its own. */
  const withClear = (keys: Record<string, unknown> | undefined, from: Plan = live) => {
    const { pylonClear: _, ...skyrail } = from.standards.skyrail;
    return derive({ ...from, standards: { ...from.standards, skyrail: keys ? { ...skyrail, pylonClear: { ...keys } } : skyrail } });
  };
  const std = live.standards.skyrail, pc = std.pylonClear;
  const trunk = (d: ReturnType<typeof derive>) => d.pylons.filter(p => p.line === 'S1' && p.path === 'trunk');
  const around = (d: ReturnType<typeof derive>) => trunk(d).filter(p => Math.abs(p.p[0]) <= 90).map(p => [p.p[0], p.span]);

  test('with the standard no pylon stands within 3 m (its face) of the monument disc, and the beam steps over it in one span', () => {
    expect([pc.landmark, pc.span, pc.remnant]).toEqual([3.8, 36, 0.5]);
    const d = derive(live);
    expect(d.pylonGaps).toEqual([]);
    for (const p of d.pylons) expect(dist(p.p, monument.c) - 0.8 - monument.r).toBeGreaterThanOrEqual(3 - 1e-6); // pylonSize 1.6, keepClear.pylonFace 3
    const over = trunk(d).find(p => p.p[0] === 18)!; // from -18 to 18: 36 m, the longest span anywhere
    expect([over.span, Math.max(...d.pylons.map(p => p.span))]).toEqual([36, 36]);
    for (const p of d.pylons) expect(p.span).toBeLessThanOrEqual(36 + 1e-6);
    for (const p of d.pylons.filter(q => q.p[0] < -18 || q.p[0] > 18)) expect(p.span).toBeLessThanOrEqual(std.spanStraight + 1e-6);
  });

  test('span: without pylonClear.span the beam cannot step over the disc, and 36 m is the limit only over a landmark', () => {
    const d = withClear({ tag: pc.tag, landmark: pc.landmark });
    expect(d.pylonGaps.length).toBeGreaterThan(0);
    expect(d.pylonGaps[0]).toMatchObject({ line: 'S1', path: 'trunk' });
    const path = derive(live).sky.find(p => p.line === 'S1' && p.id === 'trunk')!, lm = live.landmarks;
    expect(spanMax(std, lm, path, 382, 418)).toBe(36); // over the monument
    expect(spanMax(std, lm, path, 418, 448)).toBe(30); // anywhere else: by curvature
    expect(spanMax({ ...std, pylonClear: undefined }, lm, path, 382, 418)).toBe(30);
  });

  test('remnant: S1/trunk west of the monument is set again as 22 / 16 / 27.5 m; without the key a 5.5 m stub stands before it', () => {
    expect(around(derive(live))).toEqual([[-83.5, 30], [-61.5, 22], [-45.5, 16], [-18, 27.5], [18, 36], [48, 30]]);
    expect(around(withClear({ landmark: pc.landmark, span: pc.span }))).toEqual([[-83.5, 30], [-53.5, 30], [-23.5, 30], [-18, 5.5], [18, 36], [48, 30], [53, 5]]);
  });

  test('remnant: the last pylon before a portal stands where the greedy left it when the stand-off pylon would leave a stub', () => {
    const had = withClear({ landmark: pc.landmark, span: pc.span }), now = derive(live);
    for (const x of [53, 261, 520]) { // each 3 m outside a portal's jamb: stubs of 5, 2 and 12 m beside 48, 259 and 508
      expect(trunk(had).some(p => p.p[0] === x)).toBe(true);
      expect(trunk(now).some(p => p.p[0] === x)).toBe(false);
    }
    expect(trunk(now).map(p => p.p[0]).filter(x => x > 100 && x < 520)).toEqual(trunk(had).map(p => p.p[0]).filter(x => x > 100 && x < 520 && x !== 261));
    const short = trunk(now).filter(p => p.span > 0 && p.span < 15);
    expect(short.map(p => [p.p[0], p.span])).toEqual([[575, 5]]); // the terminus: the pin past the Skyport portal and the line's end are both forced
  });

  test('S1/loop\'s pylons are the standard-free plan\'s', () => {
    const had = withClear(undefined), now = derive(live);
    expect(now.pylons.filter(p => p.path === 'loop').map(p => p.p)).toEqual(had.pylons.filter(p => p.path === 'loop').map(p => p.p));
  });
});
