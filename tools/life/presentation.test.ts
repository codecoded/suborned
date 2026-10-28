// Presentation life (brief 075 golden): deterministic, never read by the sim, and on the ground it claims (pavements, lanes, tracks, bays).
import { test, expect, describe } from 'bun:test';
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { build, loadInputs, obstacles, PERIOD, WORLD_FILE, cumul, at, type P } from './presentation';
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

  test('counts near the old reel (2,851 figures, 232 cars) and every kind present', () => {
    const c = p.counts as any;
    expect(c.figures).toBeGreaterThan(2400); expect(c.figures).toBeLessThan(3400);
    expect(c.carsParked).toBeGreaterThan(200); expect(c.carsMovingMean).toBeGreaterThan(120);
    expect(c.trams).toBe(PERIOD / 180 + 2 * PERIOD / 240); expect(c.aircars).toBeGreaterThan(0); expect(c.walkLoopsDropped).toBe(0);
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
    const [x0, x1, y0, y1] = [-400, 300, -780, 320];
    for (const t of [10, 300]) {
      const cars = sample(p, t).filter(q => ['car', 'van', 'scooter'].includes(p.models[q.model].kind) && q.moving);
      expect(cars.length).toBeGreaterThan(50);
      const bad = cars.filter(q => q.x > x0 && q.x < x1 && q.y > y0 && q.y < y1 && Math.hypot(q.x - c.centre[0], q.y - c.centre[1]) > c.radius + 0.5 && !roads.some((r: any) => near(r.pts, [q.x, q.y]) <= r.half - 0.6));
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
        const q = onRoute(r, d)!; if (q.x < -400 || q.x > 300 || q.y < -780 || q.y > 320) continue;
        const off = Math.min(...lines.map((ln: P[]) => { let best = Infinity; const c = cumul(ln); for (let s = 0; s < c.at(-1)!; s += 1) { const a = at(ln, c, s).p; best = Math.min(best, Math.hypot(a[0] - q.x, a[1] - q.y)); } return best; }));
        expect(Math.min(Math.abs(off - trackAt), off)).toBeLessThan(0.5);
      }
      const runs = r.stops.slice(1).map((s: any, i: number) => ({ a: r.stops[i].stop, b: s.stop, sec: s.tArrive - r.stops[i].tDepart }));
      for (const u of runs.filter((u: any) => ['records', 'harbour-court'].includes(u.a) || ['records', 'harbour-court'].includes(u.b))) {
        const want = T.runs.find((x: any) => [x.from, x.to].sort().join() === [`${line}/${u.a}`, `${line}/${u.b}`].sort().join())?.sec; if (want) expect(Math.abs(u.sec - want)).toBeLessThan(0.6);
      }
    }
  });
});
