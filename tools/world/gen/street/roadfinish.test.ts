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
import { baysOf } from '../kerbreturn';
import { withKit } from '../kitrefs';
import { json } from '../../roadfinish-kit';
import { dressRoadFinish } from './roadfinish';
import { withFinishSet } from '../../../plan/finish-set';

const plan0: Plan = json('contracts/world/harbour-district.plan.json'), plan = plan0, d = derive(plan), civ = civic(plan.civic, json(plan.civic.block));
const exported = resolve(import.meta.dir, '../../../../out/world/harbour-district.world.json');
const world = existsSync(exported) ? json('out/world/harbour-district.world.json') : worldOf(plan0, d, civ).world; // the committed plan's crossings and platforms: the export if it is on disk, else generated
const strips: V[][] = world.crossings.map((c: any) => c.strip), platforms: V[][] = world.transit.stops.map((s: any) => s.ring);
const kitFile = resolve(import.meta.dir, '../../../../contracts/world/kit.json');
const writeKit = (k: unknown) => { const f = resolve(mkdtempSync(resolve(tmpdir(), 'roadfinish-')), 'kit.json'); writeFileSync(f, JSON.stringify(k)); return f; };
const run = (p: Plan = plan, kit: string | undefined = kitFile) => withKit(kit, () => dressRoadFinish(ctxOf(p, d, civ), { crossings: strips.map(strip => ({ strip })), platforms }));
const res = run()!;
// brief 048 (Vela, Sky 2026-10-24): the committed enamel set drops the pearl shoulder (strip light at the kerb face, not a white band); the shoulder rules below hold it laid by clearing that one key
const planBand = structuredClone(plan) as any; planBand.standards.streetscape.roads.finish.sets.enamel.drop = [];
const resBand = run(planBand)!;
const near = (p: V, ring: V[], g: number) => inPoly(p, ring) || ring.some((q, i) => segDist(p, q, ring[(i + 1) % ring.length]!) < g);
const R = (plan.standards as any).streetscape.roads, stop: number = R.breaks.stopShort;

describe('the road finish', () => {
  test('lays every family but the shoulder: ribbon, pinstripe, lozenge, seam (the enamel set drops the pearl band); clear `drop` and the shoulder is laid too', () => {
    for (const k of ['road-ribbon', 'road-pinstripe', 'road-lozenge', 'road-seam']) expect(res.log.counts[k], k).toBeGreaterThan(100);
    expect(res.log.counts['road-shoulder']).toBeUndefined();
    expect(resBand.log.counts['road-shoulder']).toBeGreaterThan(100);
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
    for (const k of ['plot', 'circus', 'median', 'mouth', 'bay', 'beyond', 'quay', 'reservation', 'return']) expect(m[k], k).toBeGreaterThan(k === 'bay' ? 10 : 30); // brief 028: the bay's straight faces are the 0.2 m ends and the 6 m back of a lay-by whose corners are 1.4 m arcs (`return`: the arcs)
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
  test('a kerbed bay\'s own faces and arcs are laid on its floor, none across its mouth, and the plot run beside it stops at the mouth corner\'s tangent point (brief 028)', () => {
    const bay = res.items.filter(i => (i.edge.startsWith('roadfinish/bay/') || i.edge.startsWith('roadfinish/return/layby/')) && i.kind !== 'road-pinstripe');
    expect(bay.length).toBeGreaterThan(0); expect(bay.every(i => i.z === 0.02)).toBe(true);
    expect(res.log.kerb.cutM.bay ?? 0).toBe(0); // no break: the geometry stops the run (capstone.kerbRuns)
    expect(res.log.returns.returns).toBe(153); expect(res.log.returns.chords).toBeGreaterThan(1500);
    for (const b of baysOf(ctxOf(plan, d, civ))) for (const i of res.items.filter(x => x.kind === 'road-ribbon' && /^roadfinish\/(rocket-lane|apron-road)/.test(x.edge))) expect(segDist(i.at, b.M[0], b.M[1]), `${i.id} on the mouth of ${b.id}`).toBeGreaterThan(0.5); // no ribbon on a mouth
  });
  test('no two pieces of one kind stand at the same place and heading (a kerb face is laid once)', () => {
    const seen = new Set<string>(), dup: string[] = [];
    for (const i of res.items) { const k = `${i.kind}|${i.at[0].toFixed(1)}|${i.at[1].toFixed(1)}|${i.yaw.toFixed(2)}`; if (seen.has(k)) dup.push(i.id); seen.add(k); }
    expect(dup.slice(0, 5)).toEqual([]);
  });
  test('without the shoulder (the committed enamel set) seams start where the ribbon ends: no seam end nearer a kerb face, a median or a reservation edge than offset + section / 2 + stopShort', () => {
    const want = R.ribbon.offset + R.ribbon.section / 2 + stop, bad: string[] = [], seams = res.items.filter(i => i.kind === 'road-seam');
    expect(seams.length).toBeGreaterThan(1000);
    for (const i of seams) {
      const rd = d.roads.find(r => `roadfinish/seam/${r.id}` === i.edge)!, hw = rd.w / 2, m = ((rd.median ?? 0) || (rd as { reservation?: number }).reservation || 0) / 2, u: V = [Math.sin(i.yaw), -Math.cos(i.yaw)];
      for (const e of [-1, 1]) { const p: V = [i.at[0] + e * u[0] * i.size[1] / 2, i.at[1] + e * u[1] * i.size[1] / 2], ld = lineDist(p, rd.line), k = Math.min(Math.abs(ld - hw), m ? Math.abs(ld - m) : Infinity); if (k < want - 0.1) bad.push(`${i.id} ${rd.id} ${k.toFixed(2)}`); }
    }
    expect(bad.length).toBeLessThan(seams.length * 0.01);
  });
  test('seams start at the shoulder\'s outer chrome: no seam end comes nearer a kerb face, a median or a reservation edge than shoulder.to + chrome (1.015 m), and the outer ones reach it', () => {
    let reach = 0;
    const sh = R.shoulder, want = sh.to + sh.chrome, bad: string[] = [], seams = resBand.items.filter(i => i.kind === 'road-seam');
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


// brief 055 road test (Vela): the clean asphalt set lays the dashed marks and nothing beside the pearl kerb; the default is unchanged
describe('the asphalt finish set', () => {
  const ap = withFinishSet(plan, 'asphalt'), M = (ap.standards as any).streetscape.roads.marks;
  const lay = (p: Plan, kit: string | undefined = kitFile) => withKit(kit, () => dressRoadFinish(ctxOf(p, d, civ), { crossings: strips.map(strip => ({ strip })), platforms }))!;
  const asp = lay(ap), ms: number = M.stopShort;
  test('the committed plan lays the enamel set, and the log says so', () => {
    expect(res.log.set).toBe('enamel'); expect(res.log.marks.dashes).toBe(0);
    expect(res.items.some(i => i.kind === 'road-edge-line' || i.kind === 'road-lane-dash')).toBe(false);
  });
  test('the clean asphalt lays dashes only: no ribbon, shoulder, pinstripe, edge line, lozenge or seam', () => {
    expect(asp.log.set).toBe('asphalt');
    expect(Object.keys(asp.log.counts)).toEqual(['road-lane-dash']);
    expect(asp.log.counts['road-lane-dash']).toBeGreaterThan(500);
  });
  test('dropping nothing lays the ribbon, shoulder and pinstripe as the enamel does; an edge line key lays the edge lines (the first, messy try)', () => {
    const p = structuredClone(ap) as any, R2 = p.standards.streetscape.roads; R2.finish.sets.asphalt.drop = []; R2.marks.edge = R2.marks.edgeOff;
    const r = lay(p);
    for (const k of ['road-ribbon', 'road-shoulder', 'road-pinstripe']) expect(r.log.counts[k], k).toBe(resBand.log.counts[k]);
    expect(r.log.counts['road-edge-line']).toBeGreaterThan(1000);
  });
  test('deterministic', () => {
    expect(JSON.stringify(lay(ap).items)).toBe(JSON.stringify(asp.items));
  });
  test('no dash lies in a crossing strip or within marks.stopShort of one (ends and centres)', () => {
    const bad: string[] = [];
    for (const i of asp.items) {
      const u: V = [Math.cos(i.yaw), Math.sin(i.yaw)];
      for (const p of [i.at, [i.at[0] + u[0] * i.size[0] / 2, i.at[1] + u[1] * i.size[0] / 2], [i.at[0] - u[0] * i.size[0] / 2, i.at[1] - u[1] * i.size[0] / 2]] as V[])
        for (const s of strips) if (near(p, s, ms - 0.25)) { bad.push(`${i.id} at ${p}`); break; }
    }
    expect(bad.slice(0, 5)).toEqual([]);
  });
  test('a centre dash only on an undivided road, at the full width; dividers only on the roads marks.lanes names, thinner, and none on the T2 half (its rails mark it)', () => {
    const by = asp.log.marks.byRoad, divided = new Set(d.roads.filter(r => r.median || (r as any).reservation).map(r => r.id));
    for (const [id, c] of Object.entries(by)) {
      if (divided.has(id)) { expect(c.centre, id).toBe(0); expect(Object.keys(M.lanes), id).toContain(id); }
      else expect(c.divider, id).toBe(0);
    }
    for (const id of Object.keys(M.lanes)) expect(by[id]?.divider, id).toBeGreaterThan(20);
    for (const i of asp.items) expect(i.size[1], i.id).toBeCloseTo(i.edge.startsWith('roadfinish/divider/') ? M.dash.dividerWidth : M.dash.width, 2);
    const tr = d.tram.map(t => ({ pts: t.samples.map(x => x.p), dbl: t.double ? 1.8 : 0 }));
    let inside = 0;
    for (const i of asp.items) for (const t of tr) for (let k = 1; k < t.pts.length; k++) if (segDist(i.at, t.pts[k - 1]!, t.pts[k]!) < t.dbl + 1.825 - 0.05) inside++;
    expect(inside).toBe(0);
  });
  test('dashes keep their length and stopShort apart on one line', () => {
    const by = new Map<string, V[]>(); for (const i of asp.items) (by.get(i.edge) ?? by.set(i.edge, []).get(i.edge)!).push(i.at);
    for (const [edge, pts] of by) for (let a = 0; a < pts.length; a++) for (let b = a + 1; b < pts.length; b++) expect(Math.hypot(pts[a]![0] - pts[b]![0], pts[a]![1] - pts[b]![1]), edge).toBeGreaterThan(M.dash.length + 2 * ms - 0.02);
  });
  test('a kit without the dash piece adopted gets a logged gap, not a crash', () => {
    const k = json('contracts/world/kit.json'); for (const a of k.assets) if (/^RoadMark_Dash/.test(a.id)) a.status = 'candidate';
    const r = lay(ap, writeKit(k));
    expect(r.items).toEqual([]); expect(r.log.gaps[0]!.why).toMatch(/not both adopted/);
  });
});
