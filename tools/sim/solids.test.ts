// The drawn solids the engine walks and sees against (brief 055, Keel): tools/sim/solids.ts.
// Run: bun test tools/sim/solids.test.ts
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { civic, toPlanFn } from '../plan/civic';
import { derive } from '../plan/plan';
import { walkGrid, WINDOW } from '../plan/walk-grid';
import { generated } from '../plan/world-ctx';
import { collideDigest, drawnSolids, floorRows, HEAD, hull, pavementOf, simGrid, solidDigest, STEP } from './solids';
import { buildSite, optionsOfSite, sourceOfSite } from './site';
import { inPoly, type V } from '../plan/geom';

const root = join(import.meta.dir, '..', '..');
const read = (f: string) => JSON.parse(readFileSync(join(root, f), 'utf8'));
const plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
const eye = read('contracts/rules/rulebook.json').values['eye-cm'].v / 100;
const G = simGrid(plan, d, civ, eye), B = walkGrid(plan, d, civ);
const site = read('sim/data/handover.site.json');
const at = (x: number, y: number) => { const [i, j] = G.cellOf([x, y]); return { i, j, k: i * G.ny + j }; };

describe('the walk grid with the drawn solids', () => {
  test('holds every cell the block-only grid blocks, and more', () => {
    let lost = 0, added = 0;
    for (let i = 0; i < G.nx; i++) for (let j = 0; j < G.ny; j++) { if (B.rows[i]![j] === '#' && G.rows[i]![j] === '.') lost++; if (B.rows[i]![j] === '.' && G.rows[i]![j] === '#') added++; }
    expect(lost).toBe(0);
    expect(added).toBe(G.added.size);
    expect(added).toBeGreaterThan(500);
  });
  test('every kit piece the window places has declared volumes (kit.collide.json), so nothing drawn is walked through for want of one', () => {
    expect(G.drawn.unmeasured).toEqual([]);
  });
  test('a phone booth on the walk is a solid (G-2a\'s example): its centre is blocked', () => {
    const booth = G.drawn.solids.find((s) => s.id === 'street/civic/call-box-0')!;
    const c = at((booth.x0 + booth.x1) / 2, (booth.y0 + booth.y1) / 2);
    expect(G.rows[c.i]![c.j]).toBe('#');
    expect(B.rows[c.i]![c.j]).toBe('.');
  });
  test('the site writes this grid when it holds the drawn walk (grid.drawn.walk), else the block-only grid; the floor and the count either way (bun run sim:site)', () => {
    expect(site.grid.rows).toEqual(site.grid.drawn.walk ? G.rows : B.rows);
    expect(site.grid.drawn.walkCells).toBe(G.added.size);
    expect(site.grid.floor).toEqual(floorRows(G.floor, G.nx, G.ny));
    expect(site.grid.collide).toBe(site.grid.drawn.digest === 'solid' ? G.drawn.solidSha : G.drawn.collideSha);
  });
});

describe('brief 072: a civic building with a kit piece, and what of kit.collide.json the grid answers to', () => {
  test('the Office stands on the block\'s footprint and its towers\' (civic-block.json polygons); its kit volumes count only where they stand proud of them', () => {
    const block = read(plan.civic.block), off = block.buildings.find((b: { id: string }) => b.id === 'civic-office');
    const walls: V[][] = [off.polygon, ...off.towers.map((t: { polygon: V[] }) => t.polygon)];
    const ids = new Set(['civic/civic-office', ...off.towers.map((t: { id: string }) => `civic/civic-office/${t.id}`)]), own = G.drawn.solids.filter((s) => ids.has(s.id));
    expect(own.filter((s) => !s.piece).length).toBe(1 + off.towers.length); // the footprint and one prism per tower
    for (const s of own.filter((x) => x.piece)) expect(walls.some((P) => inPoly(s.circle ? s.circle.c : [(s.x0 + s.x1) / 2, (s.y0 + s.y1) / 2], P))).toBe(false);
  });
  test('the digest reads only the pieces it is given (and the schema, frame and rule), never the file\'s version or another piece', () => {
    const c = read('contracts/world/kit.collide.json'), kits = Object.keys(c.pieces).slice(0, 3), a = collideDigest(c, kits);
    expect(collideDigest({ ...c, version: c.version + 1, pieces: { ...c.pieces, ZZ_Filler: { sha256: 'x', bounds: [[0, 0, 0], [1, 1, 1]], class: 'prop', volumes: [] } } }, kits)).toBe(a);
    expect(collideDigest(c, [...kits].reverse())).toBe(a);
    const moved = structuredClone(c); moved.pieces[kits[0]!].bounds[1][1] += 0.01;
    expect(collideDigest(moved, kits)).not.toBe(a);
    expect(collideDigest({ ...c, rule: `${c.rule} ` }, kits)).not.toBe(a);
  });
});

describe('the floor of every cell', () => {
  test('the pavement is 0, the carriageway -18 cm (standards.levels.datum)', () => {
    const counts = new Map<number, number>(); for (const v of G.floor) counts.set(Math.round(v * 100), (counts.get(Math.round(v * 100)) ?? 0) + 1);
    expect(counts.get(0)! > counts.get(-18)!).toBe(true);
    expect(counts.get(-18)! > 1000).toBe(true);
  });
  test('the Office public landing is 75 cm and its six steps climb to it by the block\'s rise (civic-block.json floors)', () => {
    const landing = civ.floors.find((f) => f.id === 'office-public-landing')!, steps = civ.floors.find((f) => f.id === 'office-public-steps')!;
    expect(G.floor[at(64, -41.25).k]).toBeCloseTo(landing.top, 6);
    const tops = new Set<number>(); for (let x = 61.75; x < 66.3; x += 0.5) for (let y = -43.75; y < -42; y += 0.5) tops.add(Math.round(G.floor[at(x, y).k]! * 1000));
    for (const t of tops) expect(t % Math.round(steps.rise! * 1000)).toBe(0);
    expect(Math.max(...tops)).toBeLessThanOrEqual(Math.round(steps.top * 1000));
  });
  test('the run-length rows cover the grid exactly', () => {
    for (const row of site.grid.floor as number[][]) expect(row.filter((_, k) => k % 2 === 1).reduce((a, b) => a + b, 0)).toBe(G.ny);
  });
});

describe('the drawn occluders (adopted in engine 0.28.0 with the cameras\' mount heights, brief 055 §7)', () => {
  test('each spans eye height and stands on a piece that stops sight', () => {
    expect(G.occluders.length).toBeGreaterThan(50);
    for (const o of G.occluders) { expect(o.z).toBeLessThanOrEqual(eye); expect(o.z + o.h).toBeGreaterThanOrEqual(eye); }
    expect(G.occluders.some((o) => o.id.startsWith('street/civic/call-box'))).toBe(false); // glazed: the world says sight passes
  });
  test('the site carries them (grid.drawn.sight), and the fountain stands in sight by no 4.2 m cylinder', () => {
    expect(site.grid.drawn.sight).toBe(true);
    expect((site.occluders as { id: string }[]).some((o) => o.id.startsWith('solid:'))).toBe(true);
    expect([...site.occluders, ...site.cover.blockers].some((o: { id: string; h: number }) => /fountain/.test(o.id) && o.h >= 400)).toBe(false);
  });
  test('the walk rule\'s numbers are kit.collide.json\'s', () => {
    expect(read('contracts/world/kit.collide.json').rule).toContain(`y0 < f + ${HEAD}`);
    expect(read('contracts/world/kit.collide.json').rule).toContain(`y1 > f + ${STEP}`);
  });
  test('hull: a square and its centre give the square', () => {
    expect(hull([[0, 0], [1, 0], [1, 1], [0, 1], [0.5, 0.5]])).toEqual([[0, 0], [1, 0], [1, 1], [0, 1]]);
  });
});

describe('brief 077: render-only planting (Sky 2026-10-30, "yeah, see through"): declared, never guessed', () => {
  const w = generated(plan, d, civ).w, collide = read('contracts/world/kit.collide.json'), pave = pavementOf(plan), toPlan = toPlanFn(plan.civic);
  // a walk cell with 3 m of free ground round it: a test planting 1.2 m across and 1.9 m tall (over the eye) stands there
  const spot = (() => { for (let i = 12; i < G.nx - 12; i++) for (let j = 12; j < G.ny - 12; j++) { let ok = true; for (let a = -12; a <= 12 && ok; a++) for (let b = -12; b <= 12 && ok; b++) ok = G.cellFree(i + a, j + b); if (ok) return { i, j }; } throw new Error('no 6 m square of free ground'); })();
  const c0 = toPlan(G.centre(spot.i, spot.j)), sq = ([[-0.6, -0.6], [0.6, -0.6], [0.6, 0.6], [-0.6, 0.6]] as V[]).map(([x, y]): V => [c0[0] + x, c0[1] + y]);
  const withMass = (kit: string) => ({ ...w, civic: [...w.civic, { id: 'civic/zz-planting', surface: 'trim', ring: sq, base: pave, height: 1.9, source: 'kit', kit: { id: kit, scale: [1, 1, 1], yaw: 0, at: c0, base: pave } }] }) as typeof w;
  const tall = { sha256: '', bounds: [[-0.6, 0, -0.6], [0.6, 1.9, 0.6]], class: 'solid', volumes: [{ shape: 'box', min: [-0.6, 0, -0.6], max: [0.6, 1.9, 0.6] }] };
  const withPieces = (pieces: Record<string, unknown>) => ({ ...collide, pieces: { ...collide.pieces, ...pieces } });
  const base = drawnSolids(plan, civ, w, WINDOW, 2, collide);
  const own = (dr: ReturnType<typeof drawnSolids>) => dr.solids.filter((s) => s.id === 'civic/zz-planting');

  test('a piece that declares no volumes (volumes: []) stands no solid: no walk cell, no occluder, and the solid digest does not move', () => {
    const dr = drawnSolids(plan, civ, withMass('ZZ_Planting'), WINDOW, 2, withPieces({ ZZ_Planting: { ...tall, volumes: [] } }));
    expect(own(dr)).toEqual([]);
    expect(dr.renderOnly).toEqual([...base.renderOnly, 'ZZ_Planting'].sort());
    expect(dr.unmeasured).toEqual([]);
    expect(dr.volumes).toBe(base.volumes);
    expect(dr.solidSha).toBe(base.solidSha);
    expect(dr.collideSha).not.toBe(base.collideSha); // the digest before 077 named every piece placed: why adopting planting moved every recording's inputs
  });
  test('the control: the same piece with one volume blocks (a solid over the eye) and moves the solid digest', () => {
    const dr = drawnSolids(plan, civ, withMass('ZZ_Planting'), WINDOW, 2, withPieces({ ZZ_Planting: tall }));
    expect(own(dr).map((s) => [s.piece, +(s.z1 - s.z0).toFixed(2)])).toEqual([['ZZ_Planting', 1.9]]);
    expect(dr.renderOnly).not.toContain('ZZ_Planting');
    expect(dr.solidSha).not.toBe(base.solidSha);
  });
  test('no entry at all is not render-only: it is named unmeasured and stands as its prism, as before (the site refuses to build with it)', () => {
    const dr = drawnSolids(plan, civ, withMass('ZZ_Unmeasured'), WINDOW, 2, collide);
    expect(dr.unmeasured).toEqual(['ZZ_Unmeasured']);
    expect(own(dr).map((s) => [s.piece ?? 'prism', +(s.z1 - s.z0).toFixed(2)])).toEqual([['prism', 1.9]]);
  });
  test('an entry without a volumes list is an error, never read as render-only', () => {
    const { volumes: _, ...noList } = tall;
    expect(() => drawnSolids(plan, civ, withMass('ZZ_NoList'), WINDOW, 2, withPieces({ ZZ_NoList: noList }))).toThrow(/ZZ_NoList has no volumes list/);
  });
  test('solidDigest: render-only pieces coming or going (or re-measured) leave it; any volume or bounds change of a piece that stands a solid, or its leaving, moves it', () => {
    const kits = Object.keys(collide.pieces).filter((k) => collide.pieces[k].volumes.length > 0).slice(0, 3), a = solidDigest(collide, kits);
    const ro = withPieces({ ZZ_Planting: { ...tall, volumes: [] } });
    expect(solidDigest(ro, [...kits, 'ZZ_Planting'])).toBe(a);
    const reMeasured = withPieces({ ZZ_Planting: { ...tall, bounds: [[-1, 0, -1], [1, 2.5, 1]], volumes: [] } });
    expect(solidDigest(reMeasured, [...kits, 'ZZ_Planting'])).toBe(a);
    const moved = structuredClone(collide); moved.pieces[kits[0]!].volumes[0].shape === 'box' ? (moved.pieces[kits[0]!].volumes[0].max[1] += 0.01) : (moved.pieces[kits[0]!].volumes[0].y1 += 0.01);
    expect(solidDigest(moved, kits)).not.toBe(a);
    const rebound = structuredClone(collide); rebound.pieces[kits[0]!].bounds[0][1] -= 0.01;
    expect(solidDigest(rebound, kits)).not.toBe(a);
    expect(solidDigest(collide, kits.slice(1))).not.toBe(a);
    // and the volume a real piece stands: drop one of the window's own solids' volumes, and the drawn digest moves
    const piece = base.solids.find((s) => s.piece && s.piece !== 'CCTVPole')!.piece!, cut = structuredClone(collide); cut.pieces[piece].volumes = cut.pieces[piece].volumes.slice(1);
    expect(drawnSolids(plan, civ, w, WINDOW, 2, cut).solidSha).not.toBe(base.solidSha);
  });
  test('the site keeps the digest it was built with: off, a rebuild is byte-identical; on (sim:site --solid-digest), only grid.collide and grid.drawn.digest differ', () => {
    const committed = readFileSync(join(root, 'sim/data/handover.site.json'), 'utf8'), opts = optionsOfSite();
    const off = buildSite(sourceOfSite(), { ...opts, solidDigest: false }).json, on = JSON.parse(buildSite(sourceOfSite(), { ...opts, solidDigest: true }).json);
    if (!opts.solidDigest) expect(off).toBe(committed);
    expect(on.grid.collide).toBe(G.drawn.solidSha);
    expect(on.grid.drawn.digest).toBe('solid');
    const strip = (x: { grid: { collide?: string; drawn: { digest?: string } } }) => { const y = structuredClone(x); delete y.grid.collide; delete y.grid.drawn.digest; return JSON.stringify(y); };
    expect(strip(on)).toBe(strip(JSON.parse(off)));
  });
});
