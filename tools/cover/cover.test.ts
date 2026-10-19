// The cover generator, the cover rules and the engine's reference choices (brief 011, Pulse; briefs/011/pulse-fight-ai.md). Synthetic grounds prove the geometry and give each rule
// a failing example; the Handover's own file is held fresh against its inputs, and its findings are locked until the world answers them (§6 of the spec).
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { Cover, type CoverPoint } from '../../contracts/world/cover.schema';
import { checkCover, hashes } from './check';
import { buildCover, chains, COVER_FILE, gridOf, pointsOf, quality } from './gen';
import { findings } from './report';
import { COVER, coverModifier, isqrt, protects, selectCover } from './select';

const root = join(import.meta.dir, '..', '..'), read = (f: string) => readFileSync(join(root, f), 'utf8');
const plan = JSON.parse(read('contracts/world/harbour-district.plan.json')), block = JSON.parse(read(plan.civic.block)), siteText = read('sim/data/handover.site.json'), site = JSON.parse(siteText);
const grid = gridOf(site), freeCm = (x: number, y: number) => grid.free(Math.floor((x - grid.x0) / grid.cell), Math.floor((y - grid.y0) / grid.cell));
const placeAt = (id: string): [number, number] | undefined => { const p = site.places[id], a = p?.stand ?? p?.at; return a ? [a[0], a[1]] : undefined; };
const today = () => hashes({ block: read(plan.civic.block), site: siteText, kit: read('contracts/world/kit.json'), plan: read('contracts/world/harbour-district.plan.json') });
const clone = <T>(x: T): T => JSON.parse(JSON.stringify(x));
/** An open ground: every cell free, 50 cm cells over 40 x 40 m around the origin. */
const open = { cell: 50, nx: 80, ny: 80, x0: -2000, y0: -2000, centre: (i: number, j: number): [number, number] => [(-2000 + (i + 0.5) * 50) / 100, (-2000 + (j + 0.5) * 50) / 100], free: (i: number, j: number) => i >= 0 && j >= 0 && i < 80 && j < 80 };
const rect = (cx: number, cy: number, w: number, d: number): [number, number][] => [[cx - w / 2, cy - d / 2], [cx + w / 2, cy - d / 2], [cx + w / 2, cy + d / 2], [cx - w / 2, cy + d / 2]];
const run = { id: 'civic/run-t/0', kind: 'planter-run' as const, poly: rect(0, 0, 4.3, 0.68), h: 1.1 };

describe('the generator on a synthetic ground', () => {
  test('a planter run is cover from both sides, facing each other, wide and full quality', () => {
    const pts = pointsOf([run], open);
    expect(pts).toHaveLength(2);
    const [a, b] = pts as [typeof pts[number], typeof pts[number]];
    expect([a.faces, b.faces].sort()).toEqual([[0, -1000], [0, 1000]]);
    for (const p of pts) { expect(Math.abs(p.at[1])).toBeGreaterThanOrEqual(65 + 34); expect(Math.abs(p.at[1])).toBeLessThanOrEqual(135 + 34); expect(p.arc).toBeGreaterThanOrEqual(50); expect(p.q).toBeGreaterThanOrEqual(COVER.qFull); expect(p.against).toBe('fire'); expect(p.h).toBe(110); }
  });
  test('a body-high bollard, a bed under 0.9 m and a lamp\'s foot are not cover', () => {
    expect(pointsOf([{ ...run, id: 'b', poly: rect(0, 0, 0.42, 0.42) }], open)).toEqual([]); // narrower than a body
    expect(pointsOf([{ ...run, id: 'bed', h: 0.52 }], open)).toEqual([]); // a person is not hidden by a 0.52 m bed
  });
  test('an object at eye height or more blocks sight too, and a long face has a point each 4.6 m', () => {
    const wall = { id: 'w', kind: 'wall' as const, poly: rect(0, 0, 11, 0.5), h: 2.4 };
    const pts = pointsOf([wall], open);
    expect(pts.every(p => p.against === 'fire+sight')).toBe(true);
    expect(pts.length).toBe(6); // 11 m: three points a side
  });
  test('a point stands only on free ground and never nearer another object than a body', () => {
    const blocked = { ...open, free: (i: number, j: number) => open.free(i, j) && open.centre(i, j)[1] < 0 }; // the south side's cells are not free
    const pts = pointsOf([run], blocked);
    expect(pts).toHaveLength(1); expect(pts[0]!.faces).toEqual([0, 1000]);
    const near = pointsOf([run, { ...run, id: 'near', poly: rect(0, 1.9, 4.3, 0.68) }], open); // a second run 1.9 m off: the lane between is 1.22 m, a body fits, the side facing it is 0.6 m from each
    expect(near.every(p => !(p.of === run.id && p.at[1] > 0))).toBe(true);
  });
  test('a car is cover only while it is there', () => {
    const pts = pointsOf([{ id: 'car/x', kind: 'car', poly: rect(0, 0, 4.6, 1.9), h: 1.45, present: [100, 200] }], open);
    expect(pts.length).toBeGreaterThanOrEqual(4);
    expect(pts.every(p => p.present?.join() === '100,200' && p.against === 'fire')).toBe(true);
  });
  test('chains: a rectangle is four faces; a circle is faces of at most 30° turn', () => {
    expect(chains(rect(0, 0, 4, 1))).toHaveLength(4);
    const circle = Array.from({ length: 32 }, (_, i) => [3 * Math.cos((i * 2 * Math.PI) / 32), 3 * Math.sin((i * 2 * Math.PI) / 32)] as [number, number]);
    const c = chains(circle); expect(c.length).toBeGreaterThanOrEqual(10); expect(c.reduce((n, x) => n + x.length - 1, 0)).toBe(32);
  });
  test('quality is the height class times the arc: waist-high and narrow is partial, eye-high and wide is full', () => {
    expect([quality(110, 5), quality(110, 30), quality(110, 55), quality(130, 55), quality(196, 60), quality(196, 89)]).toEqual([249, 500, 749, 843, 1000, 1000]);
    expect(quality(110, 30)).toBeLessThan(COVER.qFull);
    expect(quality(110, 40)).toBeGreaterThanOrEqual(COVER.qFull);   // 40°: 800 × 750 / 1000
    expect(quality(110, 8)).toBeLessThan(COVER.qPartial);            // a post's arc: not cover
  });
});

describe('what the engine does with a point, in integers', () => {
  const c: Pick<CoverPoint, 'at' | 'faces' | 'cos' | 'present'> = { at: [0, 0], faces: [0, 1000], cos: Math.floor(1000 * Math.cos((50 * Math.PI) / 180)), present: undefined };
  test('protects: inside the cone and beyond point blank', () => {
    expect(protects(c, [0, 1500])).toBe(true);        // dead ahead, 15 m
    expect(protects(c, [1000, 1500])).toBe(true);     // 34° off
    expect(protects(c, [1800, 1500])).toBe(false);    // 50.2° off: outside
    expect(protects(c, [0, -1500])).toBe(false);      // behind
    expect(protects(c, [0, 499])).toBe(false);        // inside 5 m: point blank, never turned
    expect(protects(c, [0, 500])).toBe(true);
  });
  test('protects: a point present from 100 s to 200 s', () => {
    const car = { ...c, present: [100, 200] as [number, number] };
    expect([protects(car, [0, 1500], 99), protects(car, [0, 1500], 100), protects(car, [0, 1500], 199), protects(car, [0, 1500], 200)]).toEqual([false, true, true, false]);
  });
  const mk = (id: string, at: [number, number], faces: [number, number], q: number, extra: Partial<CoverPoint> = {}): CoverPoint => ({ id, at, faces, arc: 50, cos: 642, h: 110, against: 'fire', q, kind: 'planter-run', of: 'civic/run-t/0', ...extra });
  test('coverModifier: beside a full point 500, beside a partial one 750, away from it or from the wrong side 1000', () => {
    const pts = [mk('cv-001', [0, 0], [0, 1000], 700), mk('cv-002', [5000, 0], [0, 1000], 400)];
    expect(coverModifier(pts, [0, 40], [0, 2000])).toBe(500);
    expect(coverModifier(pts, [5000, 60], [5000, 2000])).toBe(750);
    expect(coverModifier(pts, [0, 40], [0, -2000])).toBe(1000);
    expect(coverModifier(pts, [0, 120], [0, 2000])).toBe(1000); // 1.2 m off: not beside it
    expect(coverModifier(pts, [0, 40], [0, 300])).toBe(1000);   // point blank
  });
  test('selectCover: distance minus quality, within seek, not retreating, shielding against the foe, not taken, then the lower id', () => {
    const pts = [mk('cv-001', [0, 250], [0, 1000], 650), mk('cv-002', [0, 290], [0, 1000], 900), mk('cv-003', [0, -250], [0, -1000], 900), mk('cv-004', [0, 1300], [0, 1000], 900), mk('cv-008', [0, 450], [0, 1000], 900)];
    const me = { at: [0, 0] as [number, number], objective: [0, 3000] as [number, number], foe: [0, 3000] as [number, number] };
    expect(selectCover(pts, me, new Set())?.id).toBe('cv-002');                // 290 − 900 = −610 beats 250 − 650 = −400; cv-008 is 4.5 m nearer the foe: too far forward
    expect(selectCover(pts, me, new Set(['cv-002']))?.id).toBe('cv-001');
    expect(selectCover(pts, me, new Set(['cv-001', 'cv-002']), { seekCm: 800 })).toBeUndefined(); // cv-004 is 1300 away, cv-008 too far forward, cv-003 faces the wrong way
    expect(selectCover(pts, { ...me, foe: [0, -3000] }, new Set())?.id).toBe('cv-003'); // against a foe behind it shields from the other side
    expect(selectCover([mk('cv-005', [0, 0], [0, 1000], 650), mk('cv-006', [0, 0], [0, 1000], 650)], me, new Set())?.id).toBe('cv-005'); // tie: the lower id
    // advancing (no foe): never back more than 2 m from the objective; with a foe it may fall back to a point that shields
    expect(selectCover([mk('cv-007', [0, -900], [0, 1000], 900)], { ...me, foe: undefined }, new Set(), { seekCm: 1000 })).toBeUndefined();
    expect(selectCover([mk('cv-007', [0, -900], [0, 1000], 900)], me, new Set(), { seekCm: 1000 })?.id).toBe('cv-007');
  });
  test('isqrt', () => { expect([0, 1, 2, 3, 4, 15, 16, 99999999].map(isqrt)).toEqual([0, 1, 1, 1, 2, 3, 4, 9999]); });
});

describe('the Handover\'s cover file', () => {
  const made = buildCover();
  test('is what the plan, the block, the kit and the site make today (bun run cover)', () => {
    expect(read(COVER_FILE)).toBe(made.json);
    expect(buildCover().json).toBe(made.json); // twice
    expect(Cover.safeParse(JSON.parse(read(COVER_FILE))).success).toBe(true);
  });
  const rules = () => checkCover(JSON.parse(read(COVER_FILE)), { free: freeCm, placeAt, inputs: today() });
  test('CV-1 holds', () => { expect(rules().find(c => c.id === 'CV-1')!.ok).toBe(true); });
  test('the points are beside what the world draws: planter runs, the café wall, the fountain, the pavilion piers, the cabinets, the cars', () => {
    const kinds = new Set(made.cover.points.map(p => p.kind));
    expect([...kinds].sort()).toEqual(['cabinet', 'car', 'fountain', 'pier', 'planter-run', 'wall']);
    expect(made.cover.points.filter(p => p.kind === 'planter-run').every(p => p.of.startsWith('civic/run-'))).toBe(true);
    expect(made.cover.points.filter(p => p.of === 'car/getaway').every(p => p.present![0] === 0 && p.present![1] === site.schedule.over)).toBe(true);
    expect(made.cover.points.filter(p => p.of === 'car/rivals').every(p => p.present![0] === site.schedule.rivalsArrive)).toBe(true);
  });
  // The finding (spec §6), as the world stands after Vantage's two planter runs and Waymark's move of the posts off the lane and onto the café wall's arc: every post has cover.
  // When the world or the posts change, this test changes with them.
  test('every ambush and police post has cover that shields it from the car, the cell side is answered everywhere, and CV-2 passes', () => {
    const by = Object.fromEntries(made.cover.areas.map(a => [a.id, a]));
    expect(by['getaway-safe']!.hostileCover.map(h => [h.post, h.points.length > 0])).toEqual([['ambush-1', true], ['ambush-2', true], ['ambush-3', true], ['ambush-4', true], ['ambush-5', true]]);
    expect(by['getaway-loud']!.hostileCover.map(h => [h.post, h.points.length > 0])).toEqual([['police-1', true], ['police-2', true], ['police-3', true], ['police-4', true]]);
    for (const a of made.cover.areas) expect(a.cellCover.every(h => h.points.length > 0)).toBe(true);
    const cv2 = rules().find(c => c.id === 'CV-2')!;
    expect(cv2.ok, cv2.detail).toBe(true);
    expect(cv2.value).toBe('getaway-safe hostile 5/5, cell 5/5; getaway-loud hostile 4/4, cell 4/4');
    expect(made.cover.points.filter(p => p.of.startsWith('civic/run-sq-')).map(p => p.of).sort()).toEqual(['civic/run-sq-south/0', 'civic/run-sq-south/0', 'civic/run-sq-south/1', 'civic/run-sq-south/1', 'civic/run-sq-west/0', 'civic/run-sq-west/0']);
  });
  test('the report is empty when no post lacks cover; for a post that does it names the nearest shielding cover and a planter run that would shield it', () => {
    expect(findings(made.cover, plan, block, site)).toEqual([]);
    const bare = clone(made.cover), ambush2 = bare.areas[0]!.hostileCover.find(h => h.post === 'ambush-2')!; ambush2.points = []; // as if the wall were not there
    const fs = findings(bare, plan, block, site);
    expect(fs.map(f => f.post)).toEqual(['ambush-2']);
    expect(fs[0]!.on).toMatch(/^plot records-square/);
    expect(fs[0]!.nearest).toBeDefined();
    if (fs[0]!.proposal) expect(fs[0]!.proposal.thenShielded).toBe(true);
  });
  test('the objects at eye height or more are the sight blockers the engine adds to its occluders: the fountain, the robot, the pavilion piers', () => {
    expect(made.cover.blockers.map(b => b.id)).toEqual(['civic/atom-fountain', 'civic/foreground-civic-robot', 'civic/square-pavilion-support-0', 'civic/square-pavilion-support-1']);
    expect(made.cover.blockers.every(b => b.h >= made.cover.rules.eyeCm && b.poly.length >= 3)).toBe(true);
    expect(made.cover.points.filter(p => p.against === 'fire+sight').every(p => made.cover.blockers.some(b => b.id === p.of) || /wall|car/.test(p.kind))).toBe(true);
  });
  test('the conformance vectors the spec gives Keel: the point a body at each post would run to, with the car as its foe and advancing', () => {
    const pts = made.cover.points, car = placeAt('d:Getaway bay')!, pick = (id: string, foe: boolean) => selectCover(pts, { at: placeAt(id)!, objective: car, ...(foe ? { foe: car } : {}) }, new Set())?.id ?? null;
    const got = Object.fromEntries(['ambush-1', 'ambush-2', 'ambush-3', 'ambush-4', 'ambush-5', 'police-1', 'police-2', 'police-3', 'police-4', 'guard-ally'].map(id => [id, [pick(id, true), pick(id, false)]]));
    expect(got).toEqual({
      // 2026-10-19 (door option b): the same picks, numbered one lower from cv-078 up: the cover points are numbered in order and one earlier point went (civic-service-cabinet-1's: the block's cabinets changed with the doors, Vantage)
      'ambush-1': ['cv-078', 'cv-078'], 'ambush-2': ['cv-046', 'cv-046'], 'ambush-3': ['cv-080', 'cv-080'], 'ambush-4': ['cv-043', 'cv-045'], 'ambush-5': ['cv-081', 'cv-081'],
      'police-1': ['cv-043', 'cv-043'], 'police-2': ['cv-041', 'cv-041'], 'police-3': ['cv-046', 'cv-097'], 'police-4': ['cv-041', 'cv-042'], 'guard-ally': ['cv-078', 'cv-077'],
    });
    expect(selectCover(pts, { at: placeAt('ambush-2')!, objective: car, foe: car }, new Set(['cv-046']))?.id).not.toBe('cv-046'); // two bodies never take one point
    expect(pts.filter(p => p.of === 'car/getaway').map(p => p.id)).toEqual(['cv-001', 'cv-002', 'cv-003', 'cv-004', 'cv-005', 'cv-006']);
  });
  test('a hostile at ambush-4 runs to the café wall, and two of them take two points', () => {
    const pts = made.cover.points, car = placeAt('d:Getaway bay')!, a4 = placeAt('ambush-4')!;
    const first = selectCover(pts, { at: a4, objective: car, foe: car }, new Set());
    expect(first?.of).toMatch(/^civic\/cafe\/wall-/);
    const second = selectCover(pts, { at: a4, objective: car, foe: car }, new Set([first!.id]));
    expect(second?.id).not.toBe(first!.id);
  });
});

describe('the rules, each with a failing example', () => {
  const good = () => clone(JSON.parse(read(COVER_FILE))) as Cover;
  const run1 = (c: Cover, inputs = today()) => checkCover(c, { free: freeCm, placeAt, inputs });
  const failing = (c: Cover, id: string) => run1(c).find(x => x.id === id)!;
  test('CV-1: a point on blocked ground, a faces that is not a unit, a wrong cos or quality, a stale input, a car with no window', () => {
    expect(failing(good(), 'CV-1').ok).toBe(true);
    const c1 = good(); c1.points[0]!.at = [-9999, -9999]; expect(failing(c1, 'CV-1').detail).toMatch(/not free ground/);
    const c2 = good(); c2.points[0]!.faces = [700, 700]; expect(failing(c2, 'CV-1').detail).toMatch(/not a unit vector/);
    const c3 = good(); c3.points[0]!.cos = 1; expect(failing(c3, 'CV-1').detail).toMatch(/cos 1 is not/);
    const c4 = good(); c4.points[0]!.q = 999; expect(failing(c4, 'CV-1').detail).toMatch(/q 999 is not the quality/);
    const c5 = good(); c5.from.block = '0'.repeat(64); expect(failing(c5, 'CV-1').detail).toMatch(/made from another block/);
    const c6 = good(); const car = c6.points.find(p => p.kind === 'car')!; delete car.present; expect(failing(c6, 'CV-1').detail).toMatch(/car lacks a presence window/);
    const c7 = good(); c7.areas[0]!.hostileCover[0]!.points = ['cv-999']; expect(failing(c7, 'CV-1').detail).toMatch(/cv-999, which is not in the file/);
    const c9 = good(); c9.blockers[0]!.h = 100; expect(failing(c9, 'CV-1').detail).toMatch(/blocker of 100 cm is under eye height/);
    const c8 = good(); c8.points.splice(3, 1); expect(failing(c8, 'CV-1').detail).toMatch(/not the 4th point/);
  });
  test('CV-2: passes when half the posts each way are covered, fails when the hostile side or the cell side is bare', () => {
    const c = good();
    for (const a of c.areas) { a.hostileCover.forEach(h => { if (!h.points.length) h.points = ['cv-001']; }); }
    expect(failing(c, 'CV-2').ok).toBe(true);
    const bareHostile = good(); for (const a of bareHostile.areas) a.hostileCover.forEach(h => { h.points = []; });
    expect(failing(bareHostile, 'CV-2').detail).toMatch(/only 0 of 5 hostile posts have cover \(need 3\)/);
    const bareCell = good(); for (const a of bareCell.areas) { a.hostileCover.forEach(h => { h.points = ['cv-001']; }); a.cellCover.forEach(h => { h.points = []; }); }
    expect(failing(bareCell, 'CV-2').detail).toMatch(/the cell is answered at only 0 of 5 hostile posts/);
  });
  test('every rule has a source', () => { expect(run1(good()).every(c => !!c.source)).toBe(true); });
});

describe('the proposed rulebook values (briefs/011/pulse-fight-ai.values.json)', () => {
  const doc = JSON.parse(read('briefs/011/pulse-fight-ai.values.json')), spec = read('briefs/011/pulse-fight-ai.md'), rulebook = JSON.parse(read('contracts/rules/rulebook.json')).values;
  test('each entry has a value, a unit, a tag, a source and a reason; the ones Keel has adopted into the rulebook carry the same value', () => {
    const ids = Object.keys(doc.values);
    expect(ids.length).toBe(28);
    for (const id of ids) {
      const e = doc.values[id]; expect(typeof e.v === 'number' && e.unit && e.tag === 'S' && e.src && e.reason, id).toBeTruthy();
      if (rulebook[id]) expect(rulebook[id].v, `${id}: the rulebook has ${rulebook[id].v}, the proposal ${e.v}`).toBe(e.v);
    }
    expect(ids.filter(id => rulebook[id]).length).toBeGreaterThanOrEqual(20); // not yet adopted: the guard's leash and the crowd's values
  });
  test('the spec says what each is for (its id, or its group, is named)', () => {
    for (const id of Object.keys(doc.values)) {
      const group = id.replace(/^ai-seek-[a-z]+-cm$/, 'ai-seek').replace(/^crowd-.*$/, 'crowd-').replace(/-(s|cm|m|pct|ticks)$/, '');
      expect(spec.includes(id) || spec.includes(group) || spec.includes(id.replace(/-cm$|-s$/, '')), id).toBe(true);
    }
  });
  test('the cover constants of the file are the cover values', () => {
    const r = made2.cover.rules; expect([r.snapCm, r.qFull, r.qPartial]).toEqual([doc.values['cover-snap-cm'].v, doc.values['cover-q-full'].v, doc.values['cover-q-partial'].v]);
  });
});
const made2 = buildCover();
