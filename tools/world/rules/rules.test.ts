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
  test('the live world passes all five', () => {
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
    expect(put(w => first(w.street, s => s.kind === 'crossing').at)['SF-18']).toContain('the crossing');
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
      const s = first(w.street, q => q.kind === 'crossing' && /\/e\d+\/crossing-/.test(q.id));
      s.size = [s.size[1], s.size[0], s.size[2]];
    });
    expect(f['SF-19'])
      .toMatch(/1 fail: street\/.*\/crossing-\d+ is 1\.80 m wide along its way \(size 4 × 1\.8\), needs 4/);
    expect(ids(f)).toEqual(['SF-19']);
  });

  describe('SF-20: a crossing is two landings and a strip between their kerbs', () => {
    const kerb = (w: W, f: (c: NonNullable<W['crossings']>[number]) => boolean = () => true) => first(w.crossings!, c => c.kind === 'kerb' && f(c));
    const mid = (a: V, b: V): V => [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2];
    /** A line through the middle of a crossing's strip, across it. */
    const across = (c: { line: [V, V] }): V[] => {
      const [a, b] = c.line, len = Math.hypot(b[0] - a[0], b[1] - a[1]), m = mid(a, b), n: V = [-(b[1] - a[1]) / len, (b[0] - a[0]) / len];
      return [[m[0] - n[0] * 10, m[1] - n[1] * 10], [m[0] + n[0] * 10, m[1] + n[1] * 10]];
    };

    test('a landing with no partner: the crossing that joined it is gone, or its partner is', () => {
      const gone = failing(w => { const x = kerb(w); w.crossings = w.crossings!.filter(c => c.id !== x.id); });
      expect(gone['SF-20']).toMatch(/2 fail: street\/.*\/crossing-\d+ is a landing with no partner \(in 0 crossings\); street\/.*\/crossing-\d+ is a landing with no partner/);
      expect(ids(gone)).toEqual(['SF-20']);
      const half = failing(w => { const x = kerb(w); w.street = w.street.filter(s => s.id !== x.items[1]); });
      expect(half['SF-20']).toContain('names a street item that is not a crossing patch');
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
      const loose = failing(w => { delete kerb(w, c => c.control === 'zebra').control; });
      expect(loose['SF-20']).toMatch(/control is none, the plan gives (district|access) roads zebra/);
      const access = failing(w => { delete kerb(w, c => c.control === 'zebra' && /^crossing\/orbit-walk\//.test(c.id)).control; }); // an access road's crossing is a zebra too
      expect(access['SF-20']).toMatch(/crossing\/orbit-walk\/\d+ control is none, the plan gives access roads zebra/);
    });

    test('a patch lies at the level of the surface under it: a tram stop patch floating over the carriageway at the pavement top fails', () => {
      const f = failing(w => { first(w.street, s => s.kind === 'crossing' && s.edge.startsWith('stop/')).z = 0.18; });
      expect(f['SF-20']).toMatch(/1 fail: street\/stop\/.*\/crossing-\d+ is at z 0\.18 over a surface at 0/);
      expect(ids(f)).toEqual(['SF-20']);
    });

    test('a landing never stands on a lawn: the Sphere Gardens bank patch out on the grass fails', () => {
      const f = failing(w => { first(w.street, s => s.id === 'street/bank/S1/gardens/l/crossing-213').at = [-155.2, 29.5]; });
      expect(f['SF-20']).toMatch(/1 fail: street\/bank\/S1\/gardens\/l\/crossing-213 stands on the lawn at \(-15\d\.\d, 2\d\.\d\): a landing stands on pavement, a path or a platform/);
      expect(ids(f)).toEqual(['SF-20']);
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
