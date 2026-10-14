// CD-1..CD-4 and the civic block's dressing as the generator places it (brief 008 B3): the live world passes, and each rule fails on the one thing put back wrong.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Mass, World } from '../../../contracts/world/world.schema';
import { civic, toPlanFn } from '../../plan/civic';
import { type V, inPoly } from '../../plan/geom';
import { derive, type Plan } from '../../plan/plan';
import { ctxOf, generated } from '../../plan/world-ctx';
import { type Dressing, dressingOf, footOf, placementKinds } from '../civic/dress';
import { look } from '../civic/look';
import { adoptedBy } from '../gen/kitrefs';
import { adoptedRule, budgetRule, civicDressRules, civicTris, handoverPoints, namedPieces, paletteRule } from './civic-dress';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..');
const read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
const base = generated(plan, d, civ), dr = dressingOf(civ) as Dressing, toPlan = toPlanFn(plan.civic);
const rules = (mutate: (w: World) => void = () => {}) => {
  const w = structuredClone(base.w) as World; mutate(w);
  return Object.fromEntries(civicDressRules(ctxOf(plan, d, civ, { w, gen: base.gen })).map(c => [c.id, c]));
};
const civicMass = (w: World, id: string): Mass => w.civic.find(m => m.id === id)!;
/** Move a placed mass so its kit pivot and ring stand at block-frame `to`. */
const moveTo = (m: Mass, to: V) => { const p = toPlan(to), dx = p[0] - m.kit!.at[0], dy = p[1] - m.kit!.at[1]; m.kit!.at = p; m.ring = m.ring.map(q => [q[0] + dx, q[1] + dy] as V); };

describe('civic dressing: what is placed', () => {
  const count = (id: RegExp) => base.w.civic.filter(m => id.test(m.id)).length;
  test('each dressing entry is one mass, in the counts the block states', () => {
    expect(count(/\/parasol$/)).toBe(dr.parasols.on.length);
    expect(count(/^civic\/lamp-/)).toBe(dr.lamps.at.length);
    expect(count(/^civic\/bol-/)).toBe(dr.bollards.at.length);
    expect(count(/^civic\/run-/)).toBe(dr.planterRuns.runs.reduce((n, r) => n + r.modules.length, 0));
    expect(count(/^civic\/cyp-/)).toBe(dr.cypress.at.length);
    expect(count(/^civic\/top-/)).toBe(dr.topiary.at.length);
    expect(count(/^civic\/band\//)).toBe(look.bands.pieces.length);
    expect([dr.parasols.on.length, dr.lamps.at.length, dr.bollards.at.length, dr.cypress.at.length, dr.topiary.at.length, look.bands.pieces.length]).toEqual([7, 8, 12, 6, 6, 36]);
  });
  test('the pieces stand on what they replace: the structures they cover leave the export, the café is a ground storey under its upper mass', () => {
    const ids = new Set(base.w.civic.map(m => m.id));
    for (const gone of ['civic/office-public-landing', 'civic/office-public-steps', 'civic/office-boarding-pavilion']) expect(ids.has(gone)).toBe(false);
    for (const id of Object.values(look.place)) expect(ids.has(id)).toBe(true);
    const cafe = civicMass(base.w, 'civic/cafe'), ground = civicMass(base.w, 'civic/cafe/ground');
    expect(cafe.groundBy).toBe('civic/cafe/ground');
    expect(cafe.base).toBeCloseTo(ground.base + look.ground.CafeBay!.top, 3);
    expect(ground.kit!.id).toBe('CafeBay');
  });
  test('yaw and pivot follow the block: each entrance piece covers the points and structures it stands for', () => {
    const cover = (id: string, block: V[]) => { const m = civicMass(base.w, id), ring = footOf(adoptedBy(m.kit!.id)!, m.kit!); return block.filter(p => !inPoly(toPlan(p), ring)); };
    expect(cover('civic/cafe/ground', [[-10.3, -29], [-4.3, -29], [-7.1, -35.45], [-7.92, -23.88]])).toEqual([]); // café door, the wall line at the gate, Between tables, Under parasol
    expect(cover('civic/office-public-entrance', [[51.75, -30], [49.5, -30], [50.1, -30]])).toEqual([]); // Office entrance, the first tread, the steps
    expect(cover('civic/office-official-entrance', [[76.25, -30], [75.9, -30]])).toEqual([]); // Official entrance
  });
  test('a parasol stands on its table: pole = table centre', () => {
    for (const s of dr.parasols.on) { const t = civicMass(base.w, `civic/${s.table}`).kit!.at, p = toPlan(s.at); expect(Math.hypot(t[0] - p[0], t[1] - p[1])).toBeLessThan(0.02); }
  });
  test('the piers under the v2 shelter take the stated 1.2, and the table sets are the play-zoom set', () => {
    for (const id of ['civic/square-pavilion-support-0', 'civic/square-pavilion-support-1']) expect(civicMass(base.w, id).kit!.scale).toEqual([1.2, 1.2, 1.2]);
    expect(civicMass(base.w, 'civic/square-boarding-pavilion').kit!.id).toBe('BoardingPavilion_Square_v2');
    expect(base.w.civic.filter(m => /cafe-table-\d$/.test(m.id)).every(m => m.kit?.id === 'CafeSet_Tulip_Play')).toBe(true);
  });
  test('the parasols, the frontages, the shelter and the cypresses take the architecture fade; lamps, bollards and planters do not', () => {
    const faded = new Set(base.w.civic.filter(m => m.fade).map(m => m.kit!.id));
    expect([...faded].sort()).toEqual([...look.fade].sort());
    expect(base.w.civic.filter(m => m.fade && /\/parasol$/.test(m.id))).toHaveLength(7);
  });
  test('crossings: nine civic landings are built, each paired across the carriageway; x-apron-w and the two left out stay dropped', () => {
    const lands = dr.crossings.at.map(c => base.w.street.find(s => s.id.endsWith(`/crossing-${c.id}`)));
    expect(lands.every(Boolean)).toBe(true);
    expect(dr.crossings.at.map(c => c.id)).not.toContain('x-apron-w');
    expect(base.w.street.some(s => s.id.endsWith('x-apron-w'))).toBe(false);
    for (const l of lands) expect(base.w.crossings!.some(c => c.items.includes(l!.id) && c.items.length === 2)).toBe(true);
    const bollards = base.w.street.filter(s => s.kind === 'bollard' && /^street\/records-(square|office)\//.test(s.edge));
    expect(bollards).toEqual([]); // the generator places nothing else on a civic kerb
  });
});

describe('CD-1 keep-clear and Handover points', () => {
  test('the live world passes all four', () => {
    const r = rules();
    expect(Object.values(r).filter(c => !c.ok).map(c => `${c.id} ${c.detail}`)).toEqual([]);
    expect(Object.keys(r)).toEqual(['CD-1', 'CD-2', 'CD-3', 'CD-4']);
  });
  test('table 2\'s parasol stands over "Under parasol" and passes: the canopy is overhead', () => {
    const under = handoverPoints(toPlan).find(h => h.name === 'Under parasol')!, par = civicMass(base.w, 'civic/cafe-table-2/parasol');
    expect(inPoly(under.p, par.ring)).toBe(true);
    expect(rules()['CD-1']!.ok).toBe(true);
  });
  test('fails: a lamp in the crew-line-getaway zone', () => {
    const r = rules(w => moveTo(civicMass(w, 'civic/lamp-court-w'), [4, -47]));
    expect(r['CD-1']!.ok).toBe(false); expect(r['CD-1']!.detail).toContain('lamp-court-w');
  });
  test('fails: a planter run on a Handover point', () => {
    const r = rules(w => moveTo(civicMass(w, 'civic/run-west-cafe/0'), [-6.8, -29.5]));
    expect(r['CD-1']!.ok).toBe(false); expect(r['CD-1']!.detail).toContain("'Courtyard'");
  });
  test('fails: a bollard on a crossing landing, a parasol in the café gate spine, a band in the steps and one in a bed', () => {
    expect(rules(w => moveTo(civicMass(w, 'civic/bol-cross-1'), [30.9, -30.5]))['CD-1']!.detail).toContain('landing x-orbit-axis-n');
    expect(rules(w => moveTo(civicMass(w, 'civic/cafe-table-0/parasol'), [-5, -29]))['CD-1']!.detail).toContain('cafe-gate-spine');
    expect(rules(w => moveTo(civicMass(w, 'civic/band/forecourt-bar.0'), [50.1, -30]))['CD-1']!.detail).toContain('office-public-steps');
    expect(rules(w => moveTo(civicMass(w, 'civic/band/spine-inner.0'), [2.1, -25.6]))['CD-1']!.detail).toContain('fountain-garden');
  });
  test('passes: flat inlay under a Handover point (spine-north under "Courtyard")', () => {
    const band = base.w.civic.find(m => /^civic\/band\/spine-north/.test(m.id) && inPoly(toPlan([-6.8, -29]), m.ring));
    expect(band).toBeDefined();
  });
});

describe('CD-2 adopted, CD-3 budget, CD-4 palette', () => {
  test('CD-2 names every piece and fails on one the kit has not adopted', () => {
    const named = namedPieces(dr);
    expect(named.length).toBeGreaterThanOrEqual(27);
    expect(adoptedRule(named).ok).toBe(true);
    const c = adoptedRule(named, id => id !== 'CafeBay');
    expect(c.ok).toBe(false); expect(c.detail).toContain('CafeBay is not adopted');
    expect(adoptedRule([...named, 'Gazebo']).detail).toContain('Gazebo');
  });
  test('CD-3 counts the LOD0 triangles of the new pieces as placed, and fails over the budget', () => {
    const n = civicTris(base.w.civic);
    expect(n).toBeGreaterThan(60000); expect(n).toBeLessThanOrEqual(look.budget.lod0Tris);
    expect(budgetRule(base.w.civic).ok).toBe(true);
    expect(budgetRule(base.w.civic, n - 1).ok).toBe(false);
  });
  test('CD-4 fails a material near a signal colour, and one the palette does not list', () => {
    expect(paletteRule(look.pieces).ok).toBe(true);
    const near = { ...look.palette, materials: [...look.palette.materials, { material: 'DK_Test_Cones', unlit: '#E8392F' }] };
    expect(paletteRule(['X'], near, () => ['DK_Test_Cones']).detail).toContain('from cones');
    expect(paletteRule(['X'], look.palette, () => ['DK_Unlisted']).detail).toContain('DK_Unlisted (X) has no unlit colour');
  });
  test('every piece brief 008 added is named in the placement kinds or a swap, so none is orphaned', () => {
    const kinds = placementKinds(dr), used = new Set([...kinds.keys(), ...Object.values(look.swaps), ...Object.keys(look.place), ...look.pieces.filter(p => /^(CafeSet_Tulip_Play|PavilionPier)/.test(p))]);
    expect(look.pieces.filter(p => !used.has(p) && !/^PlanterRun_(2|Corner|End)/.test(p))).toEqual([]);
  });
});
