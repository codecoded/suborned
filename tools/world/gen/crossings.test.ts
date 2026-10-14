// Kerb-to-kerb crossings (streetscape.crossings.strip, Sky's walk note 1; brief 009: light, not paint): what the generator makes on the live plan, and that
// the standard is opt-in (a plan without it exports the single landing patches it always did, and nothing else moves). A crossing lays no slab and no zebra:
// its landings are keep-clear footprints, and what is drawn is its parts (two light lines of dot rows or bars along the walk, signal orbs, brass studs, quarter inlay arcs), each in one crossing.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic } from '../../plan/civic';
import { type V, dist } from '../../plan/geom';
import { FLAT, boxRing, ringGap } from '../../plan/street-zones';
import { derive, type Plan } from '../../plan/plan';
import { worldOf } from '../build';
import type { StreetItem } from '../../../contracts/world/world.schema';
import { areaOf, clipConvex, landingsOf } from './street/crossings';
import { PART_KINDS } from './street/parts';
import { extraRules } from '../rules';
import { ctxOf } from '../../plan/world-ctx';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
const { world: w, gen } = worldOf(plan, d, civ);
const bare: Plan = structuredClone(plan);
delete bare.standards.streetscape.crossings.strip;
const old = worldOf(bare, derive(bare), civ).world;
const items = new Map(w.street.map(s => [s.id, s]));
const kerb = (w.crossings ?? []).filter(c => c.kind === 'kerb');
const barsPlan: Plan = structuredClone(plan);
barsPlan.standards.streetscape.crossings.strip.lights.form = 'bars';
const bars = worldOf(barsPlan, derive(barsPlan), civ);
/** Position of a point in a crossing's frame: metres along the walk from line[0], and across it (left of the walk positive). */
const frame = (c: { line: [V, V] }, p: V) => { const [a, b] = c.line, l = Math.hypot(b[0] - a[0], b[1] - a[1]), u = [(b[0] - a[0]) / l, (b[1] - a[1]) / l]; return { along: (p[0] - a[0]) * u[0]! + (p[1] - a[1]) * u[1]!, across: -(p[0] - a[0]) * u[1]! + (p[1] - a[1]) * u[0]!, len: l }; };
const ofKind = (x: { street: StreetItem[] }, kind: string) => x.street.filter(s => s.kind === kind);
const kindsOf = new Set<string>(PART_KINDS);
test('no zone rule counts a crossing\'s part as standing in a zone (flush paving or overhead): every part kind is in the plan\'s FLAT set', () => {
  for (const k of PART_KINDS) expect(FLAT.has(k), k).toBe(true);
});

test('the live plan has crossings: 67 kerb pairs, 20 tram stop and 8 station bank patches, 22 refuges', () => {
  expect(w.crossings!.length).toBe(95);
  expect(kerb.length).toBe(67);
  expect(w.crossings!.filter(c => c.kind === 'stop').length).toBe(20);
  expect(w.crossings!.filter(c => c.kind === 'bank').length).toBe(8);
  expect(w.crossings!.reduce((n, c) => n + c.refuges.length, 0)).toBe(22);
  expect(w.counts.crossings).toBe(95);
  expect(gen.crossingLog!.landings).toBe(177); // 168 plot-edge landings and the civic block's 9
});

test('every part is in exactly one crossing and names it; no landing slab is left in the street; a kerb crossing is two landings on opposite kerbs', () => {
  const owner = new Map<string, string[]>();
  for (const c of w.crossings!) for (const id of c.items) owner.set(id, [...(owner.get(id) ?? []), c.id]);
  const parts = w.street.filter(s => s.crossing);
  expect(parts.length).toBe(1909);
  expect(new Set(parts.map(s => s.kind))).toEqual(new Set([...kindsOf].filter(k => k !== 'crossing-bar'))); // dot rows, not bars, by the plan's form
  for (const s of parts) expect(owner.get(s.id), s.id).toEqual([s.crossing!]);
  expect(w.crossings!.reduce((n, c) => n + c.items.length, 0)).toBe(1909);
  expect(w.street.filter(s => s.kind === 'crossing')).toEqual([]);
  const lands = landingsOf(w);
  expect(lands.length).toBe(162); // 134 landings in 67 pairs, 20 stop patches, 8 bank patches
  expect(new Set(lands.map(l => l.id)).size).toBe(162);
  const rings = lands.map(s => ({ s, ring: boxRing(s.at, s.size[0], s.size[1], s.yaw) }));
  for (const [i, a] of rings.entries()) for (const b of rings.slice(i + 1)) if (dist(a.s.at, b.s.at) < 12) expect(ringGap(a.ring, b.ring), `${a.s.id} ~ ${b.s.id}`).toBeGreaterThanOrEqual(1); // no two landings run into each other
  for (const c of kerb) {
    expect(c.landings.length, c.id).toBe(2);
    const [a, b] = c.landings;
    expect(a!.edge, c.id).not.toBe(b!.edge);
    const road = d.roads.find(r => r.id === c.road)!;
    expect(Math.abs(dist(c.line[0], c.line[1]) - road.w), c.id).toBeLessThanOrEqual(1.5);
    expect(c.width).toBe(plan.standards.streetscape.crossings.w);
    expect(c.strip.length).toBe(4);
  }
});

test('the parts, in counts: 980 dot-row modules (768 of 2 m, 212 of 1 m), 156 signal orbs (134 kerb ends and 22 refuges), 536 studs (4 a kerb end), 237 arcs (of 268 corners), no bars', () => {
  expect(ofKind(w, 'crossing-bar').length).toBe(0);
  expect(ofKind(w, 'crossing-dots').length).toBe(980);
  expect(ofKind(w, 'crossing-dots').filter(s => s.size[0] === 2).length).toBe(768);
  expect(ofKind(w, 'crossing-dots').filter(s => s.size[0] === 1).length).toBe(212);
  expect(ofKind(w, 'signal-orb').length).toBe(156);
  expect(ofKind(w, 'signal-orb').filter(s => /\/signal\/r\d+$/.test(s.id)).length).toBe(22);
  expect(ofKind(w, 'crossing-stud').length).toBe(536);
  expect(ofKind(w, 'crossing-arc').length).toBe(237);
  const left = gen.crossingLog!.skipped;
  expect(left.length).toBe(31); // 268 corners, 237 arcs
  expect(left.every(x => /\/arc\//.test(x)), left.join('; ')).toBe(true); // every orb found clear ground for its halo; only arcs are left out
  expect(left.filter(x => /overlap another crossing's arc/.test(x)).length).toBe(30); // two crossings at one corner round it once
  expect(ofKind(w, 'crossing-arc').length + left.length).toBe(4 * kerb.length);
  for (const c of w.crossings!) expect(c.items.some(id => id.includes('/dots/')), c.id).toBe(true); // every crossing, tram stop and bank ways included, has light along it
  for (const c of w.crossings!.filter(c => c.kind !== 'kerb')) expect(c.items.every(id => id.includes('/dots/')), c.id).toBe(true); // a stop or bank crossing has no kerb end: lights only
});

test('a signal orb: the id is the crossing\'s, the state is walk, its pivot is the ground under its halo, it hovers 2.7 m (centre) and its halo falls straight down', () => {
  const plan9 = plan.standards.streetscape.crossings.strip.signal;
  for (const s of ofKind(w, 'signal-orb')) {
    const sg = s.signal!;
    expect(s.id, s.id).toMatch(new RegExp(`^${s.crossing!.replace(/[/]/g, '\\/')}\\/signal\\/(a|b|r\\d+)$`));
    expect(sg.state).toBe('walk');
    expect(sg.hover).toBe(plan9.hover);
    expect(sg.size).toBe(plan9.size);
    expect(s.z).toBe(sg.halo.z); // the piece's pivot is the ground point under the beam
    expect(sg.halo.at).toEqual(s.at);
    expect(sg.halo.r).toBe(plan9.halo);
    expect(sg.halo.z).toBe(0.18); // on a pavement or a refuge island: never the carriageway
    expect(s.size).toEqual([2 * plan9.halo, 2 * plan9.halo, plan9.top]);
    expect(s.surface).toBe('orb');
    expect(s.castShadows).toBe(true);
    expect(Math.abs(Math.cos(sg.faces[0]) * Math.cos(sg.faces[1]) + Math.sin(sg.faces[0]) * Math.sin(sg.faces[1]) + 1)).toBeLessThan(1e-3); // the two faces back to back
  }
  const c = kerb[0]!, a = items.get(`${c.id}/signal/a`)!, b = items.get(`${c.id}/signal/b`)!, way = [c.line[1][0] - c.line[0][0], c.line[1][1] - c.line[0][1]];
  expect(Math.cos(a.yaw) * way[0]! + Math.sin(a.yaw) * way[1]!).toBeGreaterThan(0); // display A looks along the walk to the far kerb
  expect(Math.cos(b.yaw) * way[0]! + Math.sin(b.yaw) * way[1]!).toBeLessThan(0);
  expect(c.walk).toBeCloseTo(Math.atan2(way[1]!, way[0]!), 3); // the walk-axis yaw is exported
});

test('only the orb casts a shadow; every crossing has its walk yaw', () => {
  for (const s of w.street.filter(q => q.crossing)) expect(s.castShadows, s.id).toBe(s.kind === 'signal-orb');
  for (const c of w.crossings!) expect(c.walk, c.id).toBeCloseTo(Math.atan2(c.line[1][1] - c.line[0][1], c.line[1][0] - c.line[0][0]), 3);
});

test('the lights are two lines of dot rows along the walk: 0.43 wide, flush at the carriageway datum, ±1.485 m off the middle, end to end, off the refuges', () => {
  for (const c of w.crossings!) {
    const rows = c.items.map(id => items.get(id)!).filter(s => s.kind === 'crossing-dots');
    expect(new Set(rows.map(s => Math.sign(frame(c, s.at).across))), c.id).toEqual(new Set([1, -1]));
    for (const s of rows) expect(Math.abs(frame(c, s.at).across), s.id).toBeCloseTo(1.485, 2);
    for (const s of rows) {
      expect(s.size[1], s.id).toBe(0.43); expect([1, 2], s.id).toContain(s.size[0]);
      expect(s.surface).toBe('glow');
      expect(Math.abs(Math.cos(s.yaw - c.walk)), s.id).toBeGreaterThan(0.999); // the long axis is the walk's: never stripes across it
      expect(s.z).toBe(c.kind === 'bank' ? 0.18 : c.kind === 'stop' ? 0 : 0); // a station bank's way lies on the pavement top
      expect(s.dots).toEqual({ n: s.size[0] * 2, pitch: 0.5, d: 0.22 });
    }
    for (const sg of [1, -1]) { // end to end: each line's pieces touch or leave a refuge between them, and none runs past 0.5 m of a kerb
      const line = rows.filter(s => Math.sign(frame(c, s.at).across) === sg).sort((p, q) => frame(c, p.at).along - frame(c, q.at).along);
      for (let i = 1; i < line.length; i++) expect(frame(c, line[i]!.at).along - line[i]!.size[0] / 2 - (frame(c, line[i - 1]!.at).along + line[i - 1]!.size[0] / 2), c.id).toBeGreaterThanOrEqual(-0.001);
      expect(frame(c, line[0]!.at).along - line[0]!.size[0] / 2, c.id).toBeGreaterThanOrEqual(0.5 - 0.001);
      const last = line[line.length - 1]!; expect(frame(c, last.at).along + last.size[0] / 2, c.id).toBeLessThanOrEqual(frame(c, c.line[1]).len - 0.5 + 0.001);
    }
  }
  const c = kerb.find(x => x.refuges.length)!, rows = c.items.map(id => items.get(id)!).filter(s => s.kind === 'crossing-dots');
  for (const s of rows) for (const r of c.refuges) expect(areaOf(clipConvex(boxRing(s.at, s.size[0], s.size[1], s.yaw), r)), s.id).toBeLessThan(0.01);
});

test('a kerb end has 4 studs on the kerb edge, the outer two at the landing\'s corners, and two quarter arcs tangent to the kerb line and to the landing\'s side lines', () => {
  const R = 2.83;
  for (const c of kerb) {
    for (const [i, e] of ['a', 'b'].entries()) {
      const k = c.line[i]!, l = c.landings[i]!, n = Math.hypot(l.at[0] - k[0], l.at[1] - k[1]), inward: V = [(l.at[0] - k[0]) / n, (l.at[1] - k[1]) / n], t: V = [-inward[1], inward[0]];
      const at = (p: V) => ({ kerb: (p[0] - k[0]) * t[0] + (p[1] - k[1]) * t[1], depth: (p[0] - k[0]) * inward[0] + (p[1] - k[1]) * inward[1] }); // along the kerb from the strip's kerb point, and into the pavement
      const studs = c.items.map(id => items.get(id)!).filter(s => s.kind === 'crossing-stud' && s.id.includes(`/stud/${e}`));
      expect(studs.length, c.id).toBe(4);
      const along = studs.map(s => at(s.at).kerb).sort((p, q) => p - q);
      expect(along[0]!).toBeCloseTo(-1.8, 2); expect(along[3]!).toBeCloseTo(1.8, 2); // d 0.4: flush with the landing's edge at ±2
      for (const s of studs) expect(at(s.at).depth, s.id).toBeCloseTo(0.2, 2); // 0.2 m in from the kerb
      const arcs = c.items.map(id => items.get(id)!).filter(s => s.kind === 'crossing-arc' && s.id.includes(`/arc/${e}`));
      for (const arc of arcs) {
        expect(Math.abs(at(arc.at).kerb), arc.id).toBeCloseTo(2 + R, 2); // R past the landing's side along the kerb
        expect(at(arc.at).depth, arc.id).toBeCloseTo(R, 2); // and R into the pavement
        expect(arc.arc).toEqual({ r: 2.4, w: 0.8, sweep: 1.5708, outer: R });
        expect(arc.surface).toBe('inlay');
        const toCorner: V = [Math.cos(arc.yaw), Math.sin(arc.yaw)]; // the quarter bulges toward the mouth's corner: down to the kerb and in to the landing's side
        expect(toCorner[0] * inward[0] + toCorner[1] * inward[1], arc.id).toBeCloseTo(-Math.SQRT1_2, 2);
        expect(Math.sign(toCorner[0] * t[0] + toCorner[1] * t[1]), arc.id).toBe(-Math.sign(at(arc.at).kerb));
      }
    }
  }
});

test('the lights are a plan choice: bars give two lines of 4 m LightBar_4 along the walk and no dots, the same orbs, studs and arcs by the same ids, and nothing else moves', () => {
  const bw = bars.world;
  expect(ofKind(bw, 'crossing-dots').length).toBe(0);
  const bs = ofKind(bw, 'crossing-bar');
  expect(bs.length).toBeGreaterThan(200);
  for (const s of bs) { expect(s.surface).toBe('glow'); expect(s.size).toEqual([4, 0.44, 0.006]); expect(s.dots).toBeUndefined(); }
  for (const kind of ['signal-orb', 'crossing-stud', 'crossing-arc']) expect(ofKind(bw, kind).map(s => s.id), kind).toEqual(ofKind(w, kind).map(s => s.id));
  expect(JSON.stringify(bw.street.filter(s => s.kind !== 'crossing-bar'))).toBe(JSON.stringify(w.street.filter(s => s.kind !== 'crossing-dots'))); // every other street item identical
  for (const k of Object.keys(w).filter(k => k !== 'street' && k !== 'counts' && k !== 'crossings')) expect(JSON.stringify((bw as any)[k]), k).toBe(JSON.stringify((w as any)[k]));
  expect(extraRules(ctxOf(barsPlan, derive(barsPlan), civ, { w: bw, gen: bars.gen })).filter(c => !c.ok).map(c => `${c.id} ${c.detail}`)).toEqual([]);
});

test('the default form is dots: a plan with no lights.form draws dot rows', () => {
  const p2: Plan = structuredClone(plan);
  delete p2.standards.streetscape.crossings.strip.lights.form;
  const x = worldOf(p2, derive(p2), civ).world;
  expect(ofKind(x, 'crossing-dots').length).toBe(980);
  expect(ofKind(x, 'crossing-bar').length).toBe(0);
});

test('a plan that has the strip standard but not the parts to place is refused, naming the key', () => {
  const p2: Plan = structuredClone(plan);
  delete p2.standards.streetscape.crossings.strip.signal;
  expect(() => worldOf(p2, derive(p2), civ)).toThrow('streetscape.crossings.strip.signal is missing');
});

test('a refuge is the slice of the strip that crosses a median of refuge width, and only that', () => {
  const need = plan.standards.streetscape.crossings.strip.refuge as number;
  for (const c of kerb) {
    const want = w.ground.medians.filter(m => dist(m.ring[1]!, m.ring[2]!) >= need).map(m => clipConvex(c.strip, m.ring)).filter(r => areaOf(r) > 0.25);
    expect(c.refuges.length, c.id).toBe(want.length);
  }
  const withRefuge = kerb.filter(c => c.refuges.length);
  expect(withRefuge.length).toBeGreaterThan(0);
  expect(withRefuge.every(c => c.control === 'signal')).toBe(true); // the medianed roads are the district roads
});

test('opt-in: a plan without strip exports no crossings and all 196 single patches', () => {
  expect(old.crossings).toBeUndefined();
  expect(old.counts.crossings).toBeUndefined();
  expect(old.street.filter(s => s.kind === 'crossing').length).toBe(196);
});

test('with the standard on, nothing but crossings (their slabs gone, their parts added) and the bollards and beacons that flank them changes', () => {
  const was = new Map(old.street.map(s => [s.id, s])), moves = new Set(['crossing', 'bollard', 'beacon', ...kindsOf]);
  for (const s of w.street) { const o = was.get(s.id); if (o) expect(JSON.stringify(s), s.id).toBe(JSON.stringify(o)); else expect(moves.has(s.kind), s.id).toBe(true); }
  for (const o of old.street) if (!items.has(o.id)) expect(moves.has(o.kind), o.id).toBe(true);
  const keep = (x: { street: { kind: string }[] }) => x.street.filter(s => !moves.has(s.kind)).length;
  expect(keep(w)).toBe(keep(old));
  for (const k of Object.keys(old).filter(k => k !== 'street' && k !== 'counts')) expect(JSON.stringify((w as any)[k]), k).toBe(JSON.stringify((old as any)[k])); // every other layer is the same
});

test('every kerb crossing is a signal crossing on every road class; a stop or a bank crossing has no control (and no orb)', () => {
  expect(plan.standards.streetscape.crossings.strip.control).toEqual({ primary: 'signal', district: 'signal', access: 'signal' });
  for (const c of w.crossings!) expect(c.control, c.id).toBe(c.kind === 'kerb' ? 'signal' : undefined);
  const byRoad = new Set(kerb.map(c => d.roads.find(r => r.id === c.road)!.class));
  expect([...byRoad].sort()).toEqual(['access', 'district']); // the live plan has no primary kerb crossing
});

test('no street item draws the `crossing` look key: a crossing lays no slab, its parts have keys of their own, and a plan without the standard still has its slabs', () => {
  expect(w.street.filter(s => s.surface === 'crossing')).toEqual([]);
  expect(new Set(w.street.filter(s => s.crossing).map(s => `${s.kind}:${s.surface}`))).toEqual(new Set(['crossing-dots:glow', 'crossing-stud:trim', 'crossing-arc:inlay', 'signal-orb:orb']));
  expect(w.street.filter(s => !['phone-booth', 'tub-palm'].includes(s.kind) && !s.crossing).every(s => ['trim', 'park', 'enamel'].includes(s.surface))).toBe(true); // a booth and a palm tub have their own too (surfaces.test.ts)
  expect(old.street.filter(s => s.kind === 'crossing').every(s => s.surface === 'crossing')).toBe(true); // a plan without the strip standard names it
});

test('a landing lies at the level of the surface under it: a stop landing on the carriageway at 0, the rest on the pavement top; none on a lawn', () => {
  for (const s of landingsOf(w)) expect(s.z, s.id).toBe(s.edge.startsWith('stop/') ? 0 : 0.18); // 0.18 = the pavement top
  const gardens = landingsOf(w).find(l => l.id === 'street/bank/S1/gardens/l/crossing-213')!;
  expect(gardens.at).toEqual([-163.2, 15]); // it stood on the lawn 11.5 m out, at (-155.2, 29.5); now it is in the street pavement beside the foot of the tower (x -160)
  expect(old.street.find(s => s.id === gardens.id)!.at).toEqual([-155.2, 29.5]); // a plan without crossings.strip keeps it there
});
