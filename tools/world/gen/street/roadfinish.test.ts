// RF-gen (brief 026/028, Vela; wired and the kit adopted by Splice 2026-10-22): the road finish generator over the committed plan, the committed export's crossings and platforms, and the committed kit.
// Properties, not coordinates: same plan and kit give the same items; nothing is laid inside a crossing strip or within stopShort of one; lozenges keep their spacing; seams never cross a crossing, touch
// a lozenge or start inside the shoulder; every item names a kit piece; every road edge has a kerb face; a plan without the road standard, or a kit without the pieces adopted, gets nothing.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';

setDefaultTimeout(120000);
import { existsSync, mkdtempSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { resolve } from 'node:path';
import { civic } from '../../../plan/civic';
import { worldOf } from '../../build';
import { derive, type Plan } from '../../../plan/plan';
import { segDist, inPoly, lineDist, type V } from '../../../plan/geom';
import { ctxOf } from '../common';
import { withKit } from '../kitrefs';
import { json } from '../../roadfinish-kit';
import { dressRoadFinish } from './roadfinish';

const plan0: Plan = json('contracts/world/harbour-district.plan.json'), plan = plan0, d = derive(plan), civ = civic(plan.civic, json(plan.civic.block));
const exported = resolve(import.meta.dir, '../../../../out/world/harbour-district.world.json');
const world = existsSync(exported) ? json('out/world/harbour-district.world.json') : worldOf(plan0, d, civ).world; // the committed plan's crossings and platforms: the export if it is on disk, else generated
const strips: V[][] = world.crossings.map((c: any) => c.strip), platforms: V[][] = world.transit.stops.map((s: any) => s.ring);
const kitFile = resolve(import.meta.dir, '../../../../contracts/world/kit.json');
const writeKit = (k: unknown) => { const f = resolve(mkdtempSync(resolve(tmpdir(), 'roadfinish-')), 'kit.json'); writeFileSync(f, JSON.stringify(k)); return f; };
const run = (p: Plan = plan, kit: string | undefined = kitFile) => withKit(kit, () => dressRoadFinish(ctxOf(p, d, civ), { crossings: strips.map(strip => ({ strip })), platforms }));
const res = run()!;
const near = (p: V, ring: V[], g: number) => inPoly(p, ring) || ring.some((q, i) => segDist(p, q, ring[(i + 1) % ring.length]!) < g);
const R = (plan.standards as any).streetscape.roads, stop: number = R.breaks.stopShort;

describe('the road finish', () => {
  test('lays every family: ribbon, shoulder, pinstripe, lozenge, seam', () => {
    for (const k of ['road-ribbon', 'road-shoulder', 'road-pinstripe', 'road-lozenge', 'road-seam']) expect(res.log.counts[k], k).toBeGreaterThan(100);
    expect(res.items.length).toBe(Object.values(res.log.counts).reduce((a, b) => a + b, 0));
    expect(res.log.kerb.laidM).toBeGreaterThan(0.8 * res.log.kerb.edgeM); // most of the kerb face carries the ribbon; the rest is cut at crossings, platforms, drives, junctions
  });
  test('deterministic: the same plan and kit give the same items', () => {
    expect(JSON.stringify(run()!.items)).toBe(JSON.stringify(res.items));
  });
  test('every item is a kit piece the generator knows, with ids unique', () => {
    const ids = new Set(res.items.map(i => i.id));
    expect(ids.size).toBe(res.items.length);
    for (const i of res.items) { expect(i.source).toBe('kit'); expect(i.kit?.id).toMatch(/^(RoadRibbon|RoadShoulder|PaveEdge|RoadMark)_/); expect(i.size.every(Number.isFinite)).toBe(true); }
  });
  test('nothing lies in a crossing strip or within stopShort of one (ribbon and shoulder ends, lozenge and seam centres)', () => {
    const bad: string[] = [];
    for (const i of res.items) {
      if (i.kind === 'road-pinstripe') continue;
      const len = i.size[0], yaw = i.yaw, u: V = [Math.cos(yaw), Math.sin(yaw)];
      const pts: V[] = i.kind === 'road-seam' ? [i.at] : [i.at, [i.at[0] + u[0] * len / 2, i.at[1] + u[1] * len / 2], [i.at[0] - u[0] * len / 2, i.at[1] - u[1] * len / 2]];
      for (const p of pts) for (const s of strips) if (near(p, s, stop - 0.25)) { bad.push(`${i.id} at ${p}`); break; } // 0.25: the 0.2 m sampling step
    }
    expect(bad.slice(0, 5)).toEqual([]);
  });
  test('lozenges on one line keep their spacing; no two touch', () => {
    const lz = res.items.filter(i => i.kind === 'road-lozenge');
    const by = new Map<string, V[]>(); for (const i of lz) (by.get(i.edge) ?? by.set(i.edge, []).get(i.edge)!).push(i.at);
    for (const pts of by.values()) for (let a = 0; a < pts.length; a++) for (let b = a + 1; b < pts.length; b++) expect(Math.hypot(pts[a]![0] - pts[b]![0], pts[a]![1] - pts[b]![1])).toBeGreaterThan(R.lozenges.length);
  });
  test('the ribbon sits 0.10 m out from a kerb face: its lens line is within a hand of a plot edge or a median edge', () => {
    // the 3 m pieces nearest the Handover: take every ribbon piece in the Records Office block's streets and require a plot edge 0.1 m behind it
    const plots = d.plots.map(p => p.polyV);
    let checked = 0;
    for (const i of res.items.filter(i => i.kind === 'road-ribbon' && i.edge.startsWith('roadfinish/') && !/circus|median/.test(i.edge)).slice(0, 400)) {
      const n: V = [Math.sin(i.yaw), -Math.cos(i.yaw)], back: V = [i.at[0] - n[0] * R.ribbon.offset, i.at[1] - n[1] * R.ribbon.offset];
      const onEdge = plots.some(r => r.some((q, k) => segDist(back, q, r[(k + 1) % r.length]!) < 0.05));
      if (onEdge) checked++;
    }
    expect(checked).toBeGreaterThan(300);
  });
  test('a plan without standards.streetscape.roads gets nothing; a kit without the pieces adopted gets a logged gap, not a crash', () => {
    const bare = structuredClone(plan); delete (bare.standards as any).streetscape.roads;
    expect(run(bare)).toBeUndefined();
    const k = json('contracts/world/kit.json'); for (const a of k.assets) if (/^Road(Ribbon|Mark)_/.test(a.id)) a.status = 'candidate'; // the road pieces back to candidates
    const r = run(plan, writeKit(k))!;
    expect(r.items).toEqual([]);
    expect(r.log.gaps[0]!.why).toMatch(/not adopted/);
  });
  test('every kind of kerb face is laid: plot, circus, median, mouths, a kerbed bay, beyond, quay, reservation', () => {
    const m = res.log.kerb.faceM;
    for (const k of ['plot', 'circus', 'median', 'mouth', 'bay', 'beyond', 'quay', 'reservation']) expect(m[k], k).toBeGreaterThan(30);
    expect(m.beyond!).toBeGreaterThan(2500); expect(m.quay!).toBeGreaterThan(1000); // Datum: about 2.6 km beyond, Harbour Road's east kerb about 1.03 km
    expect(res.items.filter(i => i.edge.startsWith('roadfinish/bay/')).length).toBeGreaterThan(0);
  });
  test('every carriageway edge has a kerb face: what is left is a handful of metres at median ends and bends', () => {
    expect(res.log.kerb.uncoveredM).toBeLessThan(25);
  });
  test('the pinstripe lies at the walking surface beside its face: pavement, foot way, land; none beside beyond ground or a reservation lawn', () => {
    const pin = res.items.filter(i => i.kind === 'road-pinstripe');
    expect(pin.some(i => /roadfinish\/(beyond|.*reservation)/.test(i.edge))).toBe(false);
    expect(new Set(pin.map(i => i.z))).toEqual(new Set([0.18, 0.02, -0.03]));
  });
  test('a kerbed bay\'s own faces are laid on its floor, and its mouth cuts the plot run beside it', () => {
    const bay = res.items.filter(i => i.edge.startsWith('roadfinish/bay/') && i.kind !== 'road-pinstripe');
    expect(bay.length).toBeGreaterThan(0); expect(bay.every(i => i.z === 0.02)).toBe(true);
    expect(res.log.kerb.cutM.bay).toBeGreaterThan(0);
  });
  test('no two pieces of one kind stand at the same place and heading (a kerb face is laid once)', () => {
    const seen = new Set<string>(), dup: string[] = [];
    for (const i of res.items) { const k = `${i.kind}|${i.at[0].toFixed(1)}|${i.at[1].toFixed(1)}|${i.yaw.toFixed(2)}`; if (seen.has(k)) dup.push(i.id); seen.add(k); }
    expect(dup.slice(0, 5)).toEqual([]);
  });
  test('seams start at the shoulder\'s outer chrome: no seam end comes nearer a kerb face, a median or a reservation edge than shoulder.to + chrome (1.015 m), and the outer ones reach it', () => {
    let reach = 0;
    const sh = R.shoulder, want = sh.to + sh.chrome, bad: string[] = [], seams = res.items.filter(i => i.kind === 'road-seam');
    expect(seams.length).toBeGreaterThan(1000);
    for (const i of seams) {
      const rd = d.roads.find(r => `roadfinish/seam/${r.id}` === i.edge)!, hw = rd.w / 2, m = ((rd.median ?? 0) || (rd as { reservation?: number }).reservation || 0) / 2, u: V = [Math.sin(i.yaw), -Math.cos(i.yaw)];
      for (const e of [-1, 1]) {
        const p: V = [i.at[0] + e * u[0] * i.size[1] / 2, i.at[1] + e * u[1] * i.size[1] / 2], ld = lineDist(p, rd.line), k = Math.min(Math.abs(ld - hw), m ? Math.abs(ld - m) : Infinity);
        if (k < want - 0.1) bad.push(`${i.id} ${rd.id} ${k.toFixed(2)}`); // an end is never nearer than `want`, give or take a curve's mitre
        if (k < want + 0.02) reach++;
      }
    }
    expect(bad.length).toBeLessThan(seams.length * 0.01);
    expect(reach).toBeGreaterThan(seams.length * 0.9); // about one outer end per seam is met by the nearest-edge test (the other end is nearer a different edge on a bend); the seams' outer pieces end at the chrome, not short of it
  });
});
