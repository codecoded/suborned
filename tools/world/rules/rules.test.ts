// Failing examples for the world rules that live outside tools/plan (F-7, SF-18, L-8, SF-19, SF-20): each fails on the live
// world with one thing put back as Sky's walk found it, and passes on the world it was copied from.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { World } from '../../../contracts/world/world.schema';
import { civic } from '../../plan/civic';
import type { V } from '../../plan/geom';
import { derive, type Plan } from '../../plan/plan';
import { ctxOf, generated } from '../../plan/world-ctx';
import { carIn } from '../gen/connectors';
import { landingsOf } from '../gen/street/crossings';
import { extraRules } from './index';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..');
const read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan);
const civ = civic(plan.civic, read(plan.civic.block)), base = generated(plan, d, civ);
type W = World;
/** The rules on a clone of the world with `mutate` applied: id → detail of every failing rule. */
const failing = (mutate: (w: W) => void = () => {}): Record<string, string> => {
  const w = structuredClone(base.w) as W;
  mutate(w);
  const checks = extraRules(ctxOf(plan, d, civ, { w, gen: base.gen })).filter(c => !c.ok);
  return Object.fromEntries(checks.map(c => [c.id, c.detail ?? '']));
};
const ids = (f: Record<string, string>) => Object.keys(f);
const first = <T>(a: T[], f: (t: T) => boolean): T => a.find(f)!;
const centre = (ring: V[]): V => [0, 1].map(i => ring.reduce((t, p) => t + p[i]! / ring.length, 0)) as V;
/** The pylon nearest the monument: with standards.skyrail.pylonClear it stands 3 m off the disc (−18, 0). */
const monument = first(plan.landmarks, l => l.id === 'monument').c;
const monumentPylon = (w: W) => {
  const pylons = w.transit.skyRail[0]!.pylons, near = (p: V) => Math.hypot(p[0] - monument[0], p[1] - monument[1]);
  return pylons.reduce((best, p, i) => (near(p) < near(pylons[best]!) ? i : best), 0);
};

describe('extra world rules', () => {
  test('the live world passes them all', () => {
    expect(ids(failing())).toEqual([]);
  });

  test('SF-18: the pylon put back at 6.5, 0 (the plan before pylonClear) stands in the Crown Circus monument', () => {
    const f = failing(w => { w.transit.skyRail[0]!.pylons[monumentPylon(w)] = [6.5, 0]; });
    expect(f['SF-18']).toContain('1 fail: skyrail/S1/trunk pylon 6.5, 0 stands on the landmark landmark/monument');
    expect(ids(f)).toEqual(['SF-18']);
  });

  test('SF-18: the pylon where the plan puts it passes; on a crossing or a bay it fails', () => {
    const put = (at: (w: W) => V) => failing(w => { w.transit.skyRail[0]!.pylons[monumentPylon(w)] = at(w); });
    expect(put(w => w.transit.skyRail[0]!.pylons[monumentPylon(w)]!)['SF-18']).toBeUndefined();
    expect(put(w => landingsOf(w)[0]!.at)['SF-18']).toContain('the crossing');
    expect(put(w => centre(first(w.ground.flats, f => f.surface === 'bay').ring))['SF-18']).toContain('the bay');
  });

  test('F-7: a blade hung from the fascia through the canopy slab (as built before the walk)', () => {
    const f = failing(w => {
      const s = first(w.frontages, q => q.signKit?.mount === 'blade');
      s.signKit!.base = s.canopy.base - 0.4;
    });
    expect(f['F-7']).toMatch(/1 fail: .* blade sign passes through the canopy of /);
    expect(ids(f)).toEqual(['F-7']);
  });

  test('F-7: a sign through a gallery roof or a podium (the rule reaches megastructure parts and buildings)', () => {
    const f = failing(w => {
      const k = first(w.frontages, q => !!q.signKit).signKit!;
      w.megastructures[0]!.prims.push({
        id: 'test/roof', shape: 'box', surface: 'trim', c: k.at, base: k.base - 0.3, size: [4, 4, 0.5], yaw: 0,
      });
    });
    expect(f['F-7']).toContain('passes through test/roof');
  });

  test('L-8: an escalator that stops 8 m short of its mezzanine (the run as built before the walk)', () => {
    const f = failing(w => {
      const k = first(w.connectors, q => q.id.endsWith('/run-a'));
      const v = [k.head[0] - k.foot[0], k.head[1] - k.foot[1]];
      k.head = [k.foot[0] + v[0]! * (13 / 21), k.foot[1] + v[1]! * (13 / 21)];
    });
    expect(f['L-8']).toMatch(/run-a: head [\d.]+ m from bank\/.*\/mezzanine/);
    expect(ids(f)).toEqual(['L-8']);
  });

  test('L-8: a lift car 13.6 m from the deck it serves; a top 3 m off its level', () => {
    const far = failing(w => {
      const k = first(w.connectors, q => q.kind === 'lift' && q.id.startsWith('mega/crown-terraces/tower-core'));
      k.ring = k.ring.map(p => [p[0] + 14, p[1]] as V);
    });
    expect(far['L-8']).toMatch(/tower-core-\d\/lift: head \d+\.\d m from/);
    const high = failing(w => { first(w.connectors, q => q.kind === 'stair' && q.to === 'deck').top += 3; });
    expect(high['L-8']).toMatch(/top [\d.]+ m is not at deck \(9\.43 m\)/);
  });

  test('carIn: the car stays at the corner where it lands, else slides against the edge facing the deck', () => {
    const core: [number, number, number, number] = [0, 7, 0, 8], car: [number, number] = [2.4, 2.4];
    const is = (landing: [number, number, number, number], want: number[]) =>
      carIn(core, car, [landing], 0.6).forEach((v, i) => expect(v).toBeCloseTo(want[i]!, 6));
    is([-5, 0.2, -1, 9], [0, 2.4, 0, 2.4]); // the corner already touches it
    is([0, 7, 8.2, 20], [2.3, 4.7, 5.6, 8]); // north edge, centred on the overlap
    is([30, 40, 0, 20], [4.6, 7, 2.8, 5.2]); // east edge, centred on the core's side
  });

  test('SF-19: a crossing 4 m across the road and 1.8 m along it (the size as built before the walk)', () => {
    const f = failing(w => {
      const s = first(landingsOf(w), q => /\/e\d+\/crossing-/.test(q.id));
      s.size = [s.size[1], s.size[0], s.size[2]];
    });
    expect(f['SF-19'])
      .toMatch(/1 fail: street\/.*\/crossing-\d+ is 1\.80 m wide along its way \(size 4 × 1\.8\), needs 4/);
    expect(ids(f)).toEqual(['SF-19', 'SF-20']); // the studs stand on the kerb edge of the landing it was built with
  });

  describe('SF-20: a crossing is two landings and a strip between their kerbs, with the parts the plan gives it', () => {
    const kerb = (w: W, f: (c: NonNullable<W['crossings']>[number]) => boolean = () => true) => first(w.crossings!, c => c.kind === 'kerb' && f(c));
    const mid = (a: V, b: V): V => [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2];
    /** A line through the middle of a crossing's strip, across it. */
    const across = (c: { line: [V, V] }): V[] => {
      const [a, b] = c.line, len = Math.hypot(b[0] - a[0], b[1] - a[1]), m = mid(a, b), n: V = [-(b[1] - a[1]) / len, (b[0] - a[0]) / len];
      return [[m[0] - n[0] * 10, m[1] - n[1] * 10], [m[0] + n[0] * 10, m[1] + n[1] * 10]];
    };
    const part = (w: W, c: { items: string[] }, kind: string, f: (s: W['street'][number]) => boolean = () => true) => first(w.street, s => c.items.includes(s.id) && s.kind === kind && f(s));
    /** The one failing rule's detail, after `mutate` on a kerb crossing (with a refuge, if `refuge`). */
    const parts = (mutate: (w: W, c: NonNullable<W['crossings']>[number]) => void, refuge = false) => failing(w => mutate(w, kerb(w, c => !refuge || c.refuges.length > 0)));

    test('a kerb crossing with one landing, a landing in two crossings, a slab left in the street', () => {
      const one = failing(w => { kerb(w).landings.pop(); });
      expect(one['SF-20']).toMatch(/crossing\/.* has 1 landings, needs 2/);
      expect(ids(one)).toEqual(['SF-20']);
      const twice = failing(w => { const [x, y] = w.crossings!.filter(c => c.kind === 'kerb'); y!.landings[0] = x!.landings[0]!; });
      expect(twice['SF-20']).toMatch(/street\/.*\/crossing-\d+ is a landing of 2 crossings, needs 1/);
      const slab = failing(w => { const l = landingsOf(w)[0]!; w.street.push({ ...l, kind: 'crossing', surface: 'crossing', sight: 'open' }); });
      expect(slab['SF-20']).toMatch(/is a landing slab in the street: a crossing lays none/);
      expect(ids(slab)).toContain('SF-20');
    });

    test('a strip across a tram track fails; so does one across a bay', () => {
      const track = failing(w => { const x = kerb(w); w.transit.tram[0]!.line = across(x) as V[]; });
      expect(track['SF-20']).toMatch(/crossing\/.* strip crosses a tram track and the plan does not say a kerb crossing may/);
      const bay = failing(w => { const x = kerb(w), m = mid(x.line[0], x.line[1]); w.ground.flats.push({ id: 'test/bay', surface: 'bay', level: 0, ring: [[m[0] - 1, m[1] - 1], [m[0] + 1, m[1] - 1], [m[0] + 1, m[1] + 1], [m[0] - 1, m[1] + 1]] }); });
      expect(bay['SF-20']).toMatch(/crossing\/.* strip crosses a bay and the plan does not say a kerb crossing may/);
    });

    test('the plan can say a kind may cross a track: the tram stop crossings are the way to the platform', () => {
      const p2: Plan = structuredClone(plan);
      p2.standards.streetscape.crossings.strip.across.track = [];
      const f = Object.fromEntries(extraRules(ctxOf(p2, derive(p2), civ, { w: structuredClone(base.w), gen: base.gen })).filter(c => !c.ok).map(c => [c.id, c.detail ?? '']));
      expect(f['SF-20']).toMatch(/crossing\/stop\/.* strip crosses a tram track and the plan does not say a stop crossing may/);
    });

    test('the refuge and the control are kept where the median and the plan say', () => {
      const none = failing(w => { kerb(w, c => c.refuges.length > 0).refuges = []; });
      expect(none['SF-20']).toMatch(/crossing\/.* has 0 refuges, the median it crosses needs 1/);
      const loose = failing(w => { delete kerb(w, c => c.control === 'signal').control; });
      expect(loose['SF-20']).toMatch(/control is none, the plan gives (district|access) roads signal/);
      const access = failing(w => { delete kerb(w, c => c.control === 'signal' && /^crossing\/orbit-walk\//.test(c.id)).control; }); // an access road's crossing is a signal crossing too
      expect(access['SF-20']).toMatch(/crossing\/orbit-walk\/\d+ control is none, the plan gives access roads signal/);
    });

    test('a landing lies at the level of the surface under it: a tram stop landing floating over the carriageway at the pavement top fails', () => {
      const f = failing(w => { first(landingsOf(w), s => s.edge.startsWith('stop/')).z = 0.18; });
      expect(f['SF-20']).toMatch(/street\/stop\/.*\/crossing-\d+ is at z 0\.18 over a surface at 0/); // and its lights, laid at the level it was built on, no longer lie on it
      expect(ids(f)).toEqual(['SF-20']);
    });

    test('a landing never stands on a lawn: the Sphere Gardens bank landing out on the grass fails', () => {
      const f = failing(w => { first(landingsOf(w), s => s.id === 'street/bank/S1/gardens/l/crossing-213').at = [-155.2, 29.5]; });
      expect(f['SF-20']).toMatch(/street\/bank\/S1\/gardens\/l\/crossing-213 stands on the lawn at \(-15\d\.\d, 2\d\.\d\): a landing stands on pavement, a path or a platform/);
      expect(ids(f)).toEqual(['SF-20']);
    });

    describe('the parts (brief 009)', () => {
      const fails = (f: Record<string, string>, re: RegExp) => { expect(f['SF-20']).toMatch(re); expect(ids(f)).toEqual(['SF-20']); };
      const nrm = (c: { line: [V, V] }): V => { const n: V = [-(c.line[1][1] - c.line[0][1]), c.line[1][0] - c.line[0][0]], l = Math.hypot(n[0], n[1]); return [n[0] / l, n[1] / l]; };
      const drop = (w: W, c: { items: string[] }, f: (id: string) => boolean) => { const gone = c.items.filter(f); w.street = w.street.filter(s => !gone.includes(s.id)); c.items = c.items.filter(id => !gone.includes(id)); };
      test('a part that names no crossing, or two; one the street does not have', () => {
        fails(failing(w => { const [x, y] = w.crossings!.filter(c => c.kind === 'kerb'); y!.items.push(x!.items[0]!); }), /crossing\/.*\/dots\/l0 is a part of 2 crossings, needs 1/);
        fails(failing(w => { const x = kerb(w); w.street = w.street.filter(s => s.id !== x.items[0]); }), /crossing\/.* names a street item that is not one of its parts/);
        fails(failing(w => { const x = kerb(w); x.items.pop(); }), /is a part of 0 crossings, needs 1/);
        fails(parts((w, c) => { part(w, c, 'crossing-dots').crossing = 'crossing/nowhere/0'; }), /has a part that names another crossing/);
        fails(parts((w, c) => { c.walk += 0.5; }), /has a walk yaw that is not the way from its first kerb to its second/);
      });
      test('lights: two lines along the walk, one near each edge; a piece off the strip, over a refuge, near the kerb, the wrong size, level, look or count; bars where the plan says dots', () => {
        fails(parts((w, c) => { const s = part(w, c, 'crossing-dots'), n = nrm(c); s.at = [s.at[0] + n[0] * 1.2, s.at[1] + n[1] * 1.2]; }), /\/dots\/l0 (reaches off the strip|lies [\d.]+ m off the middle of the strip)/);
        fails(parts((w, c) => { const s = part(w, c, 'crossing-dots'), n = nrm(c); s.at = [s.at[0] - n[0] * 0.3, s.at[1] - n[1] * 0.3]; }), /\/dots\/l0 lies 1\.\d+ m off the middle of the strip, the plan's lines are at ±1\.75/);
        fails(parts((w, c) => { const s = part(w, c, 'crossing-dots'), n = nrm(c), u: V = [n[1], -n[0]], start = (s.at[0] - c.line[0][0]) * u[0] + (s.at[1] - c.line[0][1]) * u[1] - s.size[0] / 2; s.at = [s.at[0] - u[0] * (start - 0.2), s.at[1] - u[1] * (start - 0.2)]; }), /\/dots\/l0 is within 0\.5 m of a kerb or a refuge/); // its end 0.2 m from the kerb
        fails(parts((w, c) => { const r = c.refuges[0]!, m = centre(r), n = nrm(c), s = part(w, c, 'crossing-dots'); s.at = [m[0] + n[0] * 1.75, m[1] + n[1] * 1.75]; }, true), /\/dots\/\w+ (lies over a refuge|is within 0\.5 m of a kerb or a refuge)/);
        fails(parts((w, c) => { part(w, c, 'crossing-dots').size = [3, 0.43, 0.006]; }), /\/dots\/l0 is 3 × 0\.43 m, the plan's dot row modules are 2 or 1 × 0\.43/);
        fails(parts((w, c) => { part(w, c, 'crossing-dots').z = 0.18; }), /\/dots\/l0 is at z 0\.18 over a surface at 0/);
        fails(parts((w, c) => { part(w, c, 'crossing-dots').surface = 'trim'; }), /\/dots\/l0 draws trim, not glow/);
        fails(parts((w, c) => { part(w, c, 'crossing-dots').dots!.d = 0.3; }), /\/dots\/l0 is not a row of the plan's dots/);
        fails(parts((w, c) => { drop(w, c, id => id.includes('/dots/r')); }), /has lights in 1 lines, the plan's are 2, one near each edge of the strip/);
        fails(parts((w, c) => { drop(w, c, id => id.includes('/dots/')); }), /has no dot row along it/);
        fails(parts((w, c) => { drop(w, c, id => id.endsWith('/dots/l1')); }), /has a light line of \[[\d,]+\] m, the strip and the plan's modules call for \[[\d,]+\]/);
        fails(parts((w, c) => { const b = part(w, c, 'crossing-dots'); w.street.push({ ...b, id: `${c.id}/bar/l0`, kind: 'crossing-bar', size: [4, 0.44, 0.006], dots: undefined }); c.items.push(`${c.id}/bar/l0`); }), /has 1 light bars, the plan's lights form is dots/);
      });
      test('the brass edge line: one on each long edge at the strip\'s edge, whole modules kerb to kerb, thin trim; a missing, misplaced, wrong or short one fails', () => {
        fails(parts((w, c) => { drop(w, c, id => id.includes('/edge/')); }), /has no brass edge line along its long edges/);
        fails(parts((w, c) => { drop(w, c, id => id.includes('/edge/r')); }), /has an edge line of \[\]? ?[\d,]* ?\]? ?m on one side|has an edge line of \[[\d,]*\] m on one side/);
        fails(parts((w, c) => { const s = part(w, c, 'crossing-edge'), n = nrm(c); s.at = [s.at[0] - n[0] * 0.2, s.at[1] - n[1] * 0.2]; }), /\/edge\/l0 lies 1\.8\d m off the middle of the strip, the plan's edge lines are at ±2\.00/);
        fails(parts((w, c) => { part(w, c, 'crossing-edge').surface = 'glow'; }), /\/edge\/l0 draws glow, not trim/);
        fails(parts((w, c) => { part(w, c, 'crossing-edge').size = [2, 0.3, 0.006]; }), /\/edge\/l0 is 2 × 0\.3 m, the plan's edge line modules are 2 or 1 × 0\.04/);
        fails(parts((w, c) => { const s = part(w, c, 'crossing-dots'), n = nrm(c); s.at = [s.at[0] + n[0] * 0.04, s.at[1] + n[1] * 0.04]; }), /\/dots\/l0 lies 1\.79 m off the middle of the strip, the plan's lines are at ±1\.75/);
      });
      test('no light piece lies across the walk: turned a quarter it runs along the carriageway, within 30 degrees of its axis; turned 60 degrees it is off the walk, not the road', () => {
        fails(parts((w, c) => { part(w, c, 'crossing-dots').yaw += Math.PI / 2; }), /\/dots\/l0 lies 0 degrees from the carriageway's axis: a light piece runs along the walk, 30 degrees or more off the road/);
        fails(parts((w, c) => { part(w, c, 'crossing-dots').yaw += (75 * Math.PI) / 180; }), /\/dots\/l0 lies 15 degrees from the carriageway's axis/); // 15 degrees off the road: within 30
        fails(parts((w, c) => { part(w, c, 'crossing-dots').yaw += (60 * Math.PI) / 180; }), /\/dots\/l0 does not lie along the walk/); // 30 degrees from the walk, 60 from the road: the 30 degree rule passes, the walk rule does not
        fails(parts((w, c) => { const s = part(w, c, 'crossing-dots'); s.size = [s.size[1], s.size[0], s.size[2]]; }), /\/dots\/l0 (is 0\.43 × \d m|lies 0 degrees)/); // the long side turned across the walk
      });
      test('orbs: one missing, off its landing, hovering low, showing stop, its halo off its ground or on a tree, its pivot off the ground; no shadow from the body, a shadow from a stud', () => {
        fails(parts((w, c) => { drop(w, c, id => id.endsWith('/signal/b')); }), /has signal orbs \[a\], its kerb ends and refuges call for \[a,b\]/);
        fails(parts((w, c) => { const s = part(w, c, 'signal-orb', q => q.id.endsWith('/signal/a')); s.at = [s.at[0] + 3, s.at[1] + 3]; s.signal!.halo.at = s.at; }), /\/signal\/a hangs off its landing/);
        fails(parts((w, c) => { part(w, c, 'signal-orb').signal!.hover = 1.9; }), /hovers 1\.9 m \(an orb 0\.84 m across\), the plan's hover is 2\.7 \(0\.84\)/);
        fails(parts((w, c) => { part(w, c, 'signal-orb').signal!.size = 0.6; }), /an orb 0\.6 m across/);
        fails(parts((w, c) => { part(w, c, 'signal-orb').signal!.state = 'stop'; }), /shows stop, the plan's default is walk/);
        fails(parts((w, c) => { part(w, c, 'signal-orb').signal!.halo.at = [0, 0]; }), /has its halo off the orb's own ground or not the plan's size/);
        fails(parts((w, c) => { part(w, c, 'signal-orb').z = 2.28; }), /has its halo off the orb's own ground or not the plan's size/); // z is the pivot on the ground, not the orb's underside
        fails(parts((w, c) => { const s = part(w, c, 'signal-orb'); w.street.push({ ...s, id: 'test/tree', kind: 'tree', crossing: undefined, signal: undefined, z: 0.18, size: [0.5, 0.5, 8], surface: 'park' }); }), /\/signal\/\w+ has its halo on test\/tree/);
        fails(parts((w, c) => { part(w, c, 'signal-orb').signal!.faces = [0.3, 0.3 + Math.PI]; }), /has faces that are not its yaw and the way back/);
        fails(parts((w, c) => { const s = part(w, c, 'signal-orb', q => q.id.endsWith('/signal/a')); s.yaw += Math.PI / 2; s.signal!.faces = [s.yaw, s.yaw + Math.PI]; }), /does not face along the walk/);
        fails(parts((w, c) => { part(w, c, 'signal-orb', q => q.id.endsWith('/signal/r0')).signal!.halo.z = 0; }, true), /\/signal\/r0 (has its halo off the orb's own ground|has its halo at z 0 over a surface at 0\.18)/);
        fails(parts((w, c) => { part(w, c, 'signal-orb').castShadows = false; }), /does not cast a shadow, and the orb's body does/);
        fails(parts((w, c) => { part(w, c, 'crossing-stud').castShadows = true; }), /\/stud\/\w+ casts a shadow, and only the orb's body does/);
      });
      test('studs and arcs: a stud off the kerb edge, missing, the wrong size; an arc of another radius, moved, missing where its band lies on paving, kept where it would run over lawn; two arcs overlapping', () => {
        fails(parts((w, c) => { const s = part(w, c, 'crossing-stud'); s.at = [s.at[0] + 0.5, s.at[1] + 0.5]; }), /\/stud\/\w+ is not on the kerb edge of its landing/);
        fails(parts((w, c) => { drop(w, c, id => id.endsWith('/stud/a0')); }), /has 7 studs, 2 kerb ends call for 8/);
        fails(parts((w, c) => { part(w, c, 'crossing-stud').size = [0.6, 0.6, 0.08]; }), /is not the plan's brass stud/);
        fails(parts((w, c) => { part(w, c, 'crossing-arc').arc!.r = 2.5; }), /\/arc\/\w+ is not the plan's quarter arc \(r 2\.4, w 0\.8, outer 2\.83\)/);
        fails(parts((w, c) => { const s = part(w, c, 'crossing-arc'); s.at = [s.at[0] + 1, s.at[1] + 1]; }), /\/arc\/\w+ is not round the corner of its mouth, in inlay, facing the corner/);
        fails(parts((w, c) => { const s = part(w, c, 'crossing-arc'); s.yaw += 1; }), /\/arc\/\w+ is not round the corner of its mouth, in inlay, facing the corner/);
        fails(parts((w, c) => { drop(w, c, id => id.endsWith('/arc/a0')); }), /has no arc a0 round the corner of its mouth, where its band lies on paving|has an arc that is at no kerb end/);
        fails(failing(w => { const [x, y] = w.crossings!.filter(c => c.kind === 'kerb'); const a = part(w, x!, 'crossing-arc'), b = part(w, y!, 'crossing-arc'); b.at = [a.at[0] + 0.5, a.at[1] + 0.5]; }), /overlap/);
      });
      test('a plan whose control is none: no orbs; an orb on a crossing that has none fails', () => {
        const p2: Plan = structuredClone(plan);
        p2.standards.streetscape.crossings.strip.control = { primary: 'signal' };
        const f = Object.fromEntries(extraRules(ctxOf(p2, derive(p2), civ, { w: structuredClone(base.w), gen: base.gen })).filter(c => !c.ok).map(c => [c.id, c.detail ?? '']));
        expect(f['SF-20']).toMatch(/control is signal, the plan gives (district|access) roads none/); // the live export has orbs everywhere the plan says none now
      });
    });

    test('opt-in: a plan without streetscape.crossings.strip is not held to it', () => {
      const p2: Plan = structuredClone(plan);
      delete p2.standards.streetscape.crossings.strip;
      const rules = extraRules(ctxOf(p2, derive(p2), civ, { w: structuredClone(base.w), gen: base.gen })).map(c => c.id);
      expect(rules).toContain('SF-19');
      expect(rules).not.toContain('SF-20');
    });
  });
});
