// Presentation life (brief 075 golden): deterministic, never read by the sim, and on the ground it claims (pavements, lanes, tracks, bays).
import { test, expect, describe } from 'bun:test';
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { build, loadInputs, obstacles, skyObstacles, adoptedCars, PERIOD, WORLD_FILE, cumul, at, type P } from './presentation';
import { sample, onRoute } from './presentation-sample';

const ROOT = join(import.meta.dir, '..', '..');
const hasWorld = existsSync(join(ROOT, WORLD_FILE));

test('the sim, its site builder and the mission tools never read the presentation file', () => {
  const walk = (d: string, ext: RegExp, out: string[] = []) => { if (!existsSync(d)) return out; for (const f of readdirSync(d)) { if (['bin', 'obj', 'node_modules'].includes(f)) continue; const p = join(d, f); if (statSync(p).isDirectory()) walk(p, ext, out); else if (ext.test(f)) out.push(p); } return out; };
  const files = [...walk(join(ROOT, 'tools/sim'), /\.ts$/), ...walk(join(ROOT, 'tools/missions'), /\.ts$/), ...walk(join(ROOT, 'sim'), /\.(cs|json)$/)];
  expect(files.length).toBeGreaterThan(10);
  const bad = files.filter(f => /presentation\.json|life\/presentation|PRESENTATION_FILE|suborned\.presentation/.test(readFileSync(f, 'utf8')));
  expect(bad).toEqual([]);
  // and the generator writes only under out/ (its default and the CLI's), never sim/data or contracts/
  const src = readFileSync(join(import.meta.dir, 'presentation.ts'), 'utf8');
  expect(src).toContain("PRESENTATION_FILE = 'out/world/");
  expect(src).not.toMatch(/sim\/data|writeFileSync\([^)]*contracts/);
});

describe.skipIf(!hasWorld)('presentation life from the world export', () => {
  const I = hasWorld ? loadInputs() : (null as any), p = hasWorld ? build(I) : (null as any), W = I?.world;
  const isFig = (m: string) => p.models[m].kind === 'figure';

  test('deterministic: two builds, byte-identical', () => { expect(JSON.stringify(build(loadInputs()))).toBe(JSON.stringify(p)); });

  // the services the plan's lines make in one period: a line with a loop is one round route, a line without runs both ways (plan.tram, plan.skyrail, plan.timetable)
  const services = (kind: 'tram' | 'skyrail') => (kind === 'tram' ? I.plan.tram : I.plan.skyrail).reduce((n: number, l: any) => {
    const T = I.plan.timetable.lines.find((x: any) => x.line === l.id), loop = [...W.transit.tram, ...W.transit.skyRail].some((t: any) => t.id === `${kind}/${l.id}/loop`);
    return n + (PERIOD / T.headwaySec) * (loop ? 1 : 2);
  }, 0);
  const [sx0, sx1] = I?.plan.sheet.x ?? [0, 0], [sy0, sy1] = I?.plan.sheet.y ?? [0, 0], onSheet = (x: number, y: number) => x > sx0 && x < sx1 && y > sy0 && y < sy1;

  test('counts near the old reel (2,851 figures, 232 cars) and every kind present', () => {
    const c = p.counts as any, bays = W.ground.kerbside.filter((b: any) => b.kind !== 'loading').length;
    expect(c.figures).toBeGreaterThan(2400); expect(c.figures).toBeLessThan(3400);
    expect(c.carsParked).toBeGreaterThan(0.7 * bays); expect(c.carsMovingMean).toBeGreaterThan(120);
    expect(c.trams).toBe(services('tram')); expect(c.skyrailTrains).toBe(services('skyrail')); expect(c.aircars).toBeGreaterThan(0); expect(c.walkLoopsDropped).toBe(0);
    expect(Object.keys(c.dropped).some(k => k.startsWith('aircar lane'))).toBe(false);
    expect(/Vale|[Hh]andover|councillor|waitress/.test(JSON.stringify({ ...p, notSim: '' }))).toBe(false); // no mission content or cast
  });

  test('seamless: everything is where it was one period earlier', () => {
    for (const t of [3, 250.5]) { const a = sample(p, t), b = sample(p, t + PERIOD); expect(b.length).toBe(a.length); a.forEach((q, i) => { expect(Math.abs(q.x - b[i].x) + Math.abs(q.y - b[i].y)).toBeLessThan(1e-6); }); }
  });

  test('figures stand and walk on open ground (no building, megastructure, civic mass, landmark or water)', () => {
    const blocked = obstacles(W);
    for (const t of [0, 137, 411]) { const bad = sample(p, t).filter(q => isFig(q.model) && blocked([q.x, q.y])); expect(bad.map(q => q.id).slice(0, 5)).toEqual([]); }
  });

  test('moving cars keep to the carriageway (a road, the circus ring, or beyond the sheet edge)', () => {
    const roads = W.ground.roads.map((r: any) => ({ pts: r.line as P[], half: r.width / 2 })), c = W.ground.circuses[0];
    const near = (pts: P[], q: P) => { let d = Infinity; for (let i = 1; i < pts.length; i++) { const a = pts[i - 1], b = pts[i], L2 = (b[0] - a[0]) ** 2 + (b[1] - a[1]) ** 2 || 1, k = Math.max(0, Math.min(1, ((q[0] - a[0]) * (b[0] - a[0]) + (q[1] - a[1]) * (b[1] - a[1])) / L2)); d = Math.min(d, Math.hypot(a[0] + (b[0] - a[0]) * k - q[0], a[1] + (b[1] - a[1]) * k - q[1])); } return d; };
    for (const t of [10, 300]) {
      const cars = sample(p, t).filter(q => ['car', 'van', 'scooter'].includes(p.models[q.model].kind) && q.moving);
      expect(cars.length).toBeGreaterThan(50);
      const bad = cars.filter(q => onSheet(q.x, q.y) && Math.hypot(q.x - c.centre[0], q.y - c.centre[1]) > c.radius + 0.5 && !roads.some((r: any) => near(r.pts, [q.x, q.y]) <= r.half - 0.6));
      expect(bad.map(q => `${q.id} ${q.x.toFixed(1)},${q.y.toFixed(1)}`).slice(0, 5)).toEqual([]);
    }
  });

  test('parked cars sit in the world\'s kerbside bays', () => {
    const bays = new Map<string, any>(W.ground.kerbside.map((b: any) => [b.id, b]));
    for (const s of p.still.filter((s: any) => s.clip === 'parked')) { const b = bays.get(s.where), xs = b.ring.map((q: P) => q[0]), ys = b.ring.map((q: P) => q[1]); expect(s.at[0]).toBeGreaterThan(Math.min(...xs) - 0.1); expect(s.at[0]).toBeLessThan(Math.max(...xs) + 0.1); expect(s.at[1]).toBeGreaterThan(Math.min(...ys) - 0.1); expect(s.at[1]).toBeLessThan(Math.max(...ys) + 0.1); }
  });

  test('trams run on their tracks, dwell the timetable\'s seconds and keep its straight runs', () => {
    const tt = I.plan.timetable.lines, trackAt = 1.8;
    for (const r of p.routes.filter((r: any) => r.kind === 'tram')) {
      const line = r.id.split('/')[1], T = tt.find((l: any) => l.line === line), lines = W.transit.tram.filter((x: any) => x.id.startsWith(`tram/${line}/`)).map((x: any) => x.line as P[]);
      for (const s of r.stops) expect(Math.abs(s.tDepart - s.tArrive - T.dwellSec)).toBeLessThan(0.02);
      // every 5 s on the sheet the tram is on a track (centreline +- 1.8, or the loop's single track)
      for (let d = 0; d <= r.t.at(-1); d += 5) {
        const q = onRoute(r, d)!; if (!onSheet(q.x, q.y)) continue;
        const off = Math.min(...lines.map((ln: P[]) => { let best = Infinity; const c = cumul(ln); for (let s = 0; s < c.at(-1)!; s += 1) { const a = at(ln, c, s).p; best = Math.min(best, Math.hypot(a[0] - q.x, a[1] - q.y)); } return best; }));
        expect(Math.min(Math.abs(off - trackAt), off)).toBeLessThan(0.5);
      }
      // a run on straight track that the tram can make in its timetable seconds (accelerating and braking at 1 m/s2) takes them; a curve may be slower
      const runs = r.stops.slice(1).map((s: any, i: number) => ({ a: r.stops[i].stop, b: s.stop, t0: r.stops[i].tDepart, t1: s.tArrive, sec: s.tArrive - r.stops[i].tDepart }));
      let checked = 0;
      for (const u of runs) {
        const want = T.runs.find((x: any) => [x.from, x.to].sort().join() === [`${line}/${u.a}`, `${line}/${u.b}`].sort().join())?.sec; if (!want) continue;
        const pts: P[] = []; for (let d = u.t0; d <= u.t1; d += 1) { const q = onRoute(r, d)!; if (!pts.length || Math.hypot(q.x - pts.at(-1)![0], q.y - pts.at(-1)![1]) > 4) pts.push([q.x, q.y]); }
        let turn = 0; for (let i = 2; i < pts.length; i++) { const h0 = Math.atan2(pts[i - 1][1] - pts[i - 2][1], pts[i - 1][0] - pts[i - 2][0]), h1 = Math.atan2(pts[i][1] - pts[i - 1][1], pts[i][0] - pts[i - 1][0]); turn += Math.abs(Math.atan2(Math.sin(h1 - h0), Math.cos(h1 - h0))); }
        const D = cumul(pts).at(-1)!; if (turn > 0.1 || want * want < 4 * D) continue;
        expect(Math.abs(u.sec - want)).toBeLessThan(0.6); checked++;
      }
      expect(checked).toBeGreaterThan(0);
    }
  });

  test('aircars fly clear of every structure (4 m), with their lateral wander', () => {
    const kit = JSON.parse(readFileSync(join(ROOT, 'contracts/world/kit.json'), 'utf8')), byId = Object.fromEntries(kit.assets.map((a: any) => [a.id, a])), blocked = skyObstacles(W, byId);
    const lanes = p.loops.filter((l: any) => l.kind === 'sky'); expect(lanes.length).toBe(3);
    for (const l of lanes) for (let i = 0; i < l.xyz.length; i += 3) for (const d of [-1.5, 0, 1.5]) {
      const j = (i + 3) % l.xyz.length, dx = l.xyz[j] - l.xyz[i], dy = l.xyz[j + 1] - l.xyz[i + 1], n = Math.hypot(dx, dy) || 1;
      expect(blocked([l.xyz[i] + dy / n * d, l.xyz[i + 1] - dx / n * d], l.xyz[i + 2], 4)).toBe(false);
    }
  });

  test('cars: every adopted kit car, many paints, neighbours differ, the rival sedan rare and never repainted', () => {
    const kit = JSON.parse(readFileSync(join(ROOT, 'contracts/world/kit.json'), 'utf8')), byId = Object.fromEntries(kit.assets.map((a: any) => [a.id, a]));
    const cars = [...p.timed.filter((x: any) => p.routes[x.route].kind === 'car'), ...p.still.filter((x: any) => x.clip === 'parked')];
    const used = new Set(cars.map((x: any) => p.models[x.model].kit));
    for (const id of adoptedCars(byId)) expect(used.has(id)).toBe(true);
    const keys = Object.keys(p.paints); expect(keys.length).toBeGreaterThanOrEqual(8);
    for (const x of cars) { const m = p.models[x.model], drawn = m.paintSlots ? m : p.models[m.fallback ?? '']; if (drawn?.paintSlots) expect(keys).toContain(x.paint); else expect(x.paint).toBeUndefined(); }
    expect(p.models['car/rival'].paintSlots).toBeUndefined();
    const parked = p.still.filter((x: any) => x.clip === 'parked'), share = (xs: any[], f: (x: any) => boolean) => xs.filter(f).length / xs.length;
    expect(share(parked, (x: any) => x.model === 'car/rival')).toBeLessThan(0.06); expect(share(cars, (x: any) => x.model === 'car/rival')).toBeLessThan(0.06);
    // no paint over a fifth of the bays, and no two neighbours (within 9 m) in the same paint
    const tally = new Map<string, number>(); for (const x of parked) if (x.paint) tally.set(x.paint, (tally.get(x.paint) ?? 0) + 1);
    expect(Math.max(...tally.values()) / parked.length).toBeLessThan(0.2);
    const twins = parked.filter((x: any, i: number) => x.paint && parked.slice(0, i).some((y: any) => y.paint === x.paint && Math.hypot(y.at[0] - x.at[0], y.at[1] - x.at[1]) < 9));
    expect(twins.map((x: any) => x.id)).toEqual([]);
    const rivals = parked.filter((x: any) => x.model === 'car/rival');
    expect(rivals.filter((x: any, i: number) => rivals.slice(0, i).some((y: any) => Math.hypot(y.at[0] - x.at[0], y.at[1] - x.at[1]) < 40)).map((x: any) => x.id)).toEqual([]);
  });

  test('presentation draw tiers are presentation-only and run far to near', () => {
    expect(p.lod.scope).toMatch(/Presentation scenes only/); expect(p.lod.scope).toMatch(/Play, the missions/);
    for (const tiers of [p.lod.figures, p.lod.cars]) { for (let i = 1; i < tiers.length; i++) expect(tiers[i].from).toBeLessThan(tiers[i - 1].from); }
    expect(p.lod.figures[0].from).toBe(0.012); // the figure library's own last height: the near tier is the library, unchanged
  });
});
