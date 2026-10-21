// Failing examples for the world rules (B, L, MS, F, SF): each rule fails on a world with one thing broken, and passes on the
// world it was copied from. The base is the generated world of the live plan, cloned per test; it moves with the plan, but the
// mutations are written to fail whatever the plan says. F-2 fails on the live world today (Datum: trades' `wants`), so the
// clean-pass test names it as known.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic } from './civic';
import { lineDist, type V } from './geom';
import { boxRing, footprintOf, quayEdges, ringGap } from './street-zones';
import { derive, type Plan } from './plan';
import { ctxOf, generated } from './world-ctx';
import { worldRules } from './world-rules';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
const base = generated(plan, d, civ);
const KNOWN = ['F-2']; // fails on the live world: F-2 reported to Datum; not weakened. (R-REPORT was known from 2026-10-19 until the north-west mouth of the block was covered from the square's corner pavement.)

type W = any;
/** The rules on a clone of the world with `mutate(w, gen, plan)` applied; returns the ids that fail. */
const failing = (mutate: (w: W, g: W, p: W) => void): string[] => {
  const w = structuredClone(base.w) as W, g = { ...base.gen } as W, p = structuredClone(plan) as W;
  g.connectors = w.connectors; g.frontages = w.frontages; g.street = w.street; g.blds = base.gen.blds.map(b => ({ ...b })); g.decks = base.gen.decks.map(x => ({ ...x })); g.travel = base.gen.travel.map(t => ({ ...t }));
  mutate(w, g, p);
  return worldRules(ctxOf(p, d, civ, { w, gen: g })).filter(c => !c.ok).map(c => c.id);
};
const first = <T>(a: T[], f: (t: T) => boolean) => a.find(f)!;
const centre = (r: number[][]): number[] => [r.reduce((a, p) => a + p[0]!, 0) / r.length, r.reduce((a, p) => a + p[1]!, 0) / r.length];

describe('world rules', () => {
  test('the unmodified world passes every rule but the known failures', () => {
    expect(failing(() => {}).filter(id => !KNOWN.includes(id))).toEqual([]);
  });
  const cases: [string, string, (w: W, g: W, p: W) => void][] = [
    ['B-2', 'a building taller than its band', (_w, g) => { first(g.blds, (b: W) => !b.tower && b.kind !== 'shed').z += 40; }],
    ['B-3', 'a building shoved off its plot', (w) => { for (const m of w.buildings) m.ring = m.ring.map((v: number[]) => [v[0] + 60, v[1]]); }],
    ['B-4', 'two buildings overlapping', (w) => { const b = w.buildings.filter((m: W) => m.base < 1); b[1].ring = b[0].ring.map((v: number[]) => [...v]); }],
    ['L-1', 'a deck with no way down', (w, g) => { w.connectors.length = 0; g.connectors.length = 0; }],
    ['B-1', 'a plot with no band', (_w, _g, p) => { p.plots[0].use = 'no-such-use'; }],
    ['B-5', 'a tower under its minimum storeys', (_w, g) => { first(g.blds, (b: W) => b.tower).n = 1; }],
    ['B-6', 'a building part standing in the sky rail beam', (w, _g, p) => { const [x, y] = d.pylons[0]!.p; w.buildings[0] = { ...w.buildings[0], ring: [[x - 4, y - 4], [x + 4, y - 4], [x + 4, y + 4], [x - 4, y + 4]], base: 6, height: 20 }; }],
    ['L-2', 'a deck whose worst point is too far from a way down', (_w, g) => { g.travel[0].max = g.travel[0].limit + 5; }],
    ['L-3', 'a stair 1 m wide', (w) => { first(w.connectors, (k: W) => k.kind === 'stair').width = 1; }],
    ['L-4', 'a deck level 3 m up over a carriageway', (w) => { w.levels = w.levels.map((l: W) => l.name === 'deck' ? { ...l, height: 3 } : l); }],
    ['L-5', 'stations with no stair bank, run or lift', (w, g) => { w.connectors = w.connectors.filter((k: W) => !/^bank\//.test(k.id)); g.connectors = w.connectors; }],
    ['L-7', 'the Skyport lift as first built: 0.18 -> 89.6 from the south-west leg (the leg stops at 34)', (w) => { const k = w.connectors.find((q: W) => q.id === 'mega/skyport-tower/lift-sw'); k.top = 89.6; k.to = 'saucer'; }],
    ['L-7', 'a lift shaft standing alone on open ground', (w) => { const k = first(w.connectors, (q: W) => q.kind === 'lift' && q.id.startsWith('bank/')); k.ring = k.ring.map((p: number[]) => [p[0] + 200, p[1] + 400]); }],
    ['SG-1', 'a plot building standing on the SpaceHQ ground', (w) => { const g = plan.signatures![0]!.ground; w.buildings.push({ id: 'bld/parade-north/99/body', surface: 'commercial', ring: [[g[0] + 5, g[2] + 5], [g[0] + 25, g[2] + 5], [g[0] + 25, g[2] + 25], [g[0] + 5, g[2] + 25]], base: 0.18, height: 20 }); }],
    ['P-1', 'a part standing in the Parade gate opening (the gate block without its opening)', (w) => { w.megastructures[0].parts.push({ id: 'mega/parade-galleries/gate-block', surface: 'limestone', ring: [[264, -20], [288, -20], [288, 20], [264, 20]], base: 9.43, height: 25.37 }); }],
    ['SK-1', 'LinerDock as built, 205.1 m, on the liner berth', (w) => { const s = w.signatures.find((x: W) => x.id === 'tower/avenue-needle'); s.tip = 205.1; }],
    ['K-2', 'a height cut the placed tower piece was not regenerated for (avenue-needle, tip -14.4 m)', (_w, _g, p) => { p.standards.buildings.towers.sites.find((x: W) => x.id === 'avenue-needle').regen.tip -= 14.4; }],
    ['SK-2', 'two crowns of one type 90 m apart that are not a pair', (_w, _g, p) => { const s = p.standards.buildings.towers.sites; s.find((x: W) => x.id === 'avenue-needle').crownType = 'disc'; }],
    ['MS-1', 'a megastructure with no reason', (_w, _g, p) => { p.megastructures[0].why = ''; }],
    ['MS-2', 'a footprint off the land it spans', (_w, _g, p) => { p.megastructures[0].footprint = [[900, 1000, 900, 1000]]; }],
    ['MS-3', 'a bridge over a sky rail pylon', (_w, _g, p) => { const [x, y] = d.pylons[0]!.p; const m = p.megastructures.find((q: W) => q.bridges?.length); m.bridges[0].rect = [x - 1, x + 1, y - 1, y + 1]; }],
    ['MS-4', 'a pier leg on the sky rail line', (_w, _g, p) => { const [x, y] = d.pylons[0]!.p; p.megastructures.find((q: W) => q.supports.portal).legs[0] = [x - .2, x + .2, y - .2, y + .2]; }],
    ['MS-5', 'a deck column in the carriageway', (w) => { const m = w.levels.flatMap((l: W) => l.decks).find((x: W) => x.id.startsWith('mega/')); m.columns = [[200, 0]]; }],
    ['F-1', 'a tenancy off its building line', (w) => { const f = w.frontages[5]; f.a = [f.a[0] + 7, f.a[1] + 7]; f.b = [f.b[0] + 7, f.b[1] + 7]; }],
    ['F-2', 'a tenancy on a primary distributor', (w, _g, p) => { w.frontages[0].street = p.roads.find((r: W) => r.class === 'primary').id; }],
    ['F-4', 'a service bay of the wrong size', (w) => { const f = first(w.frontages, (q: W) => q.service); f.service = { ...f.service, size: [9, f.service.size[1], f.service.size[2]] }; }],
    ['F-5', 'buildings with no lobby', (w) => { for (const f of w.frontages) if (f.trade === 'lobby') f.trade = 'florist'; }],
    ['SF-2', 'a lamp in a doorway', (w) => { first(w.street, (s: W) => s.kind === 'lamp' && s.z < 1).at = w.frontages.find((f: W) => f.z < 1 && f.entrance).entrance.c.slice(0, 2); }],
    ['SF-2', 'a bollard standing on the service drive (brief 023: nothing stands on a drive)', (w) => { first(w.street, (s: W) => s.kind === 'bollard' && s.z < 1).at = centre(w.ground.drives[0].ring); }],
    ['SF-2', 'a lamp 1 m from the service drive (street items keep 2 m off it)', (w) => { const r = w.ground.drives[0].ring, x = Math.max(...r.map((p: number[]) => p[0])), y = (r[0][1] + r[1][1]) / 2; first(w.street, (s: W) => s.kind === 'lamp' && s.z < 1).at = [x + 1.6, y]; }],
    ['SF-18', 'a sky rail pylon on the service drive', (w) => { w.transit.skyRail[0].pylons[0] = centre(w.ground.drives[0].ring); }],
    ['SF-20', 'a crossing landing on the service drive', (w) => { w.crossings[0].landings[0].at = centre(w.ground.drives[0].ring); }],
    ['SF-20', 'a crossing strip across the service drive', (w) => { w.crossings[0].strip = w.ground.drives[0].ring.map((p: number[]) => [...p]); }],
    ['SF-3', 'a lamp on the tram reservation', (w) => { first(w.street, (s: W) => s.kind === 'lamp' && s.z < 1).at = d.tram[0]!.samples[10]!.p; }],
    ['SF-4', 'a tree under the sky rail beam', (w) => { const t = first(w.street, (s: W) => s.kind === 'tree' && s.z < 1); t.at = d.pylons[0]!.p; }],
    ['SF-6', 'a lamp that does not fit its zone', (w) => { const t = first(w.street, (s: W) => s.kind === 'lamp' && s.z < 1); t.at = [t.at[0] + 1.5, t.at[1] + 1.5]; }],
    ['SF-7', 'trees closer than the spacing', (w) => { const t = first(w.street, (s: W) => s.kind === 'tree' && s.z < 1); w.street.push({ ...t, id: `${t.id}-x`, at: [t.at[0] + 1, t.at[1]] }); }],
    ['SF-8', 'a sight-blocking item at a junction corner', (w) => { for (const s of w.street) if (s.z < 1) s.sight = 'blocks'; }],
    ['SF-9', 'a station with no watchers', (w) => { w.street = w.street.filter((s: W) => !/camera|robot/.test(s.kind)); }],
    ['SF-11', 'a tree on a pylon face', (w) => { first(w.street, (s: W) => s.kind === 'tree' && s.z < 1).at = d.pylons[0]!.p; }],
    ['SF-12', 'a tram stop with no kit', (w) => { w.street = w.street.filter((s: W) => !s.edge.startsWith('stop/')); }],
    ['SF-13', 'a station bank in the SpaceHQ hall lane', (w, _g, p) => { const sg = p.signatures.find((q: W) => q.id === 'spacehq' || q.ground), k = first(w.connectors, (q: W) => /^bank\/.+\/tower$/.test(q.id)), x = (sg.ground[0] + sg.ground[1]) / 2; k.ring = [[x - 1, -1], [x + 1, -1], [x + 1, 1], [x - 1, 1]]; }],
    ['SF-13', 'a station bank in the beam envelope', (w) => { const [x, y] = d.pylons[0]!.p, k = first(w.connectors, (q: W) => /^bank\/.+\/tower$/.test(q.id)); k.ring = [[x - 1, y - 1], [x + 1, y - 1], [x + 1, y + 1], [x - 1, y + 1]]; }],
    ['SF-15', 'a roof with an unknown programme', (w) => { w.roofs = w.roofs.map((r: W) => ({ ...r })); w.roofs[0].programme = 'no-such-programme'; }],
    ['SF-16', 'spill deeper than the frontage zone', (w) => { w.frontages[0].spill = { kind: 'tables', depth: 99 }; }],
    ['SF-5', 'a hydrant on the kerb face', (w) => { // pulled straight toward its road's centre line until 0.1 m off the kerb face
      const h = first(w.street, (s: W) => s.kind === 'hydrant' && s.z < 1 && /\/e\d/.test(s.edge)), r = d.roads.reduce((a, b) => lineDist(h.at, b.line) - b.w / 2 < lineDist(h.at, a.line) - a.w / 2 ? b : a);
      let best: V = r.line[0]!, bd = Infinity;
      for (let i = 1; i < r.line.length; i++) { const a = r.line[i - 1]!, b = r.line[i]!, l2 = (b[0] - a[0]) ** 2 + (b[1] - a[1]) ** 2, t = Math.max(0, Math.min(1, ((h.at[0] - a[0]) * (b[0] - a[0]) + (h.at[1] - a[1]) * (b[1] - a[1])) / l2)), q: V = [a[0] + t * (b[0] - a[0]), a[1] + t * (b[1] - a[1])], dd = Math.hypot(q[0] - h.at[0], q[1] - h.at[1]); if (dd < bd) { bd = dd; best = q; } }
      const k = (bd - r.w / 2 - 0.1) / bd; h.at = [h.at[0] + (best[0] - h.at[0]) * k, h.at[1] + (best[1] - h.at[1]) * k];
    }],
    ['L-6', 'a duplicate id', (w) => { w.street[1].id = w.street[0].id; }],
    ['F-3', 'the same trade side by side', (w) => { const f = w.frontages; const i = f.findIndex((x: W, k: number) => k && f[k - 1].host === x.host && f[k - 1].street === x.street && x.trade !== f[k - 1].trade); f[i].trade = f[i - 1].trade; }],
    ['F-6', 'a trade not in the area mix', (w) => { w.frontages[3].trade = 'no-such-trade'; }],
    ['SF-1', 'a lamp moved into the walking zone', (w) => { const t = first(w.street, (s: W) => s.kind === 'lamp' && s.z < 1); t.at = [t.at[0] + 1.5, t.at[1] + 1.5]; }],
    ['SF-10', 'a staffed item with no hours', (w) => { first(w.street, (s: W) => s.staffed).staffed.hours = undefined; }],
    ['SF-14', 'a bench on the quay edge', (w, _g, p) => { // 0.3 m inside the quay from the middle of a water edge (quays are rectangles)
      const [a, b] = quayEdges({ plan: p, d: derive(p) })[0]!, q = p.quay.find((r: number[][]) => r.some(v => v[0] === a[0] && v[1] === a[1]))!, xs = q.map((v: number[]) => v[0]), ys = q.map((v: number[]) => v[1]);
      const m = [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2], cx = (Math.min(...xs) + Math.max(...xs)) / 2, cy = (Math.min(...ys) + Math.max(...ys)) / 2;
      first(w.street, (s: W) => s.kind === 'bench' && s.z < 1).at = a[0] === b[0] ? [m[0] + Math.sign(cx - m[0]) * .3, m[1]] : [m[0], m[1] + Math.sign(cy - m[1]) * .3];
    }],
    ['SF-17', 'a sign outside the city palette', (w) => { w.frontages[0].sign.palette = 'no-such-colour'; }],
  ];
  for (const [id, what, mutate] of cases) test(`${id} fails on ${what}`, () => { expect(failing(mutate)).toContain(id); });
});

// Brief 026 (the old city's models back): a twin booth serves a booth pair, the curved kiosk stands on open ground, and a wall unit
// nobody queues at (streetscape.doors.wallUnits) keeps 0.6 m off a door's jambs instead of 2 m.
describe('brief 026 plan standards', () => {
  /** An Automat moved along its wall until its footprint is `gap` m from the nearest street door's jambs; `kind` re-labels it. */
  const nearDoor = (gap: number, kind?: string) => (w: W) => {
    const a = first(w.street, (s: W) => s.kind === 'automat' && s.z < 1), S = plan.standards.streetscape;
    const doors = w.frontages.filter((f: W) => f.level === 'street').map((f: W) => boxRing(f.entrance.c, f.entrance.size[0], f.entrance.size[1], f.entrance.yaw));
    const gapNow = () => Math.min(...doors.map((r: V[]) => ringGap(footprintOf(S, a.kind, a.variant, a.at, a.size, a.yaw), r)));
    const door = doors.reduce((p: V[], r: V[]) => ringGap(footprintOf(S, a.kind, a.variant, a.at, a.size, a.yaw), r) < ringGap(footprintOf(S, a.kind, a.variant, a.at, a.size, a.yaw), p) ? r : p);
    const dc: V = [(door[0]![0] + door[2]![0]) / 2, (door[0]![1] + door[2]![1]) / 2], u: V = [Math.cos(a.yaw), Math.sin(a.yaw)], side = Math.sign((a.at[0] - dc[0]) * u[0] + (a.at[1] - dc[1]) * u[1]) || 1;
    for (let i = 0; i < 4000 && Math.abs(gapNow() - gap) > 0.005; i++) { const k = Math.sign(gapNow() - gap) * 0.005; a.at = [a.at[0] - u[0] * side * k, a.at[1] - u[1] * side * k]; } // toward the door when farther than `gap`, away when nearer (the generator now puts an Automat 0.6 m off its door)
    expect(Math.abs(gapNow() - gap)).toBeLessThan(0.02);
    if (kind) a.kind = kind;
  };
  test('an Automat 1.0 m from a door passes SF-2 (a wall unit: 0.6 m)', () => { expect(failing(nearDoor(1.0))).not.toContain('SF-2'); });
  test('an Automat 0.4 m from a door fails SF-2', () => { expect(failing(nearDoor(0.4))).toContain('SF-2'); });
  test('a phone booth 1.0 m from a door still fails SF-2 (2 m)', () => { expect(failing(nearDoor(1.0, 'phone-booth'))).toContain('SF-2'); });

  // The generator merges each booth pair into one twin (variant 'twin', counts 2). A bank served by its twin passes SF-12; the same item turned into one single booth does not.
  const twinAs = (edge: string, single: boolean) => (w: W) => {
    const t = first(w.street, (s: W) => s.kind === 'phone-booth' && s.edge === edge && s.variant === 'twin');
    if (single) { delete t.variant; t.size = [1, 1, 2.5]; }
  };
  test('a station bank served by one twin booth passes SF-12 (counts as two)', () => { expect(failing(twinAs('bank/S1/gardens/l', false))).not.toContain('SF-12'); });
  test('a station bank with one single booth fails SF-12', () => { expect(failing(twinAs('bank/S1/gardens/l', true))).toContain('SF-12'); });

  const pavilion = (...at: V[]) => (w: W) => { w.street = w.street.filter((s: W) => s.kind !== 'kiosk-pavilion'); at.forEach((p, i) => w.street.push({ id: `street/test/kiosk-pavilion-${i}`, kind: 'kiosk-pavilion', at: p, yaw: 0, z: 0.18, size: [6.1, 4.4, 6.5], surface: 'enamel', edge: 'test', sight: 'blocks', staffed: { trade: 'soda-fountain', hours: [8, 22] } })); };
  test('a kiosk pavilion on Orbit Gardens passes SF-7', () => { expect(failing(pavilion([-120, -360]))).not.toContain('SF-7'); });
  test('two kiosk pavilions on Orbit Gardens fail SF-7', () => { expect(failing(pavilion([-120, -360], [-100, -340]))).toContain('SF-7'); });
  test('a kiosk pavilion on Records Square (the civic block) fails SF-7', () => { expect(failing(pavilion([-160, -250]))).toContain('SF-7'); });
  test('a kiosk pavilion on a residential plot fails SF-7', () => { const r = first(d.plots, p => p.use === 'residential').polyV; expect(failing(pavilion([(r[0]![0] + r[2]![0]) / 2, (r[0]![1] + r[2]![1]) / 2]))).toContain('SF-7'); });
});
