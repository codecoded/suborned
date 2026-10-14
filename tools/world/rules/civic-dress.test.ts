// CD-1..CD-4 and the civic block's dressing as the generator places it (brief 008 B3): the live world passes, and each rule fails on the one thing put back wrong.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Mass, World } from '../../../contracts/world/world.schema';
import { civic, toPlanFn } from '../../plan/civic';
import { type V, inPoly } from '../../plan/geom';
import { derive, type Plan } from '../../plan/plan';
import { ctxOf, generated } from '../../plan/world-ctx';
import { civicClearance } from '../build';
import { type Dressing, dressingOf, footOf, placementKinds } from '../civic/dress';
import { look } from '../civic/look';
import { adoptedBy } from '../gen/kitrefs';
import { landingsOf } from '../gen/street/crossings';
import { adoptedRule, budgetRule, civicDressRules, civicTris, drawnApart, handoverPoints, namedPieces, overrideProblems, paletteRule } from './civic-dress';

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
    expect(cover('civic/cafe/ground', [[-10.3, -29]])).toEqual([]); // café door
    expect(cover('civic/cafe/terrace', [[-4.3, -29], [-7.1, -35.45], [-7.92, -23.88], [-4.63, -34.7], [-12.7, -46.2], [-12.7, -11.8]])).toEqual([]); // the wall line at the gate, Between tables, Under parasol, the wall's front corner and both stone ends
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
    const lands = dr.crossings.at.map(c => landingsOf(base.w).find(s => s.id.endsWith(`/crossing-${c.id}`)));
    expect(lands.every(Boolean)).toBe(true);
    expect(dr.crossings.at.map(c => c.id)).not.toContain('x-apron-w');
    expect(landingsOf(base.w).some(s => s.id.endsWith('x-apron-w'))).toBe(false);
    expect(base.w.street.some(s => s.kind === 'crossing')).toBe(false); // a crossing lays no slab: its landings are keep-clear footprints, not street items
    for (const l of lands) expect(base.w.crossings!.some(c => c.landings.some(q => q.id === l!.id) && c.landings.length === 2)).toBe(true);
    const bollards = base.w.street.filter(s => s.kind === 'bollard' && /^street\/records-(square|office)\//.test(s.edge));
    expect(bollards).toEqual([]); // the generator places nothing else on a civic kerb
  });
});

describe('civic dressing: the terrace, the beds, the pool (brief 008 colour round)', () => {
  const beds = base.w.civic.filter(m => m.surface === look.beds.kerb.surface), tufts = base.w.civic.filter(m => /\/tuft-\d+$/.test(m.id)), hedges = base.w.civic.filter(m => m.surface === look.beds.hedge.surface);
  test('the terrace goes with the café bay: same pivot, yaw and ground; the bay keeps the fade, the terrace does not', () => {
    const bay = civicMass(base.w, 'civic/cafe/ground'), terrace = civicMass(base.w, 'civic/cafe/terrace');
    expect({ ...terrace.kit!, id: 'CafeBay' }).toEqual(bay.kit!);
    expect([terrace.kit!.id, bay.kit!.id]).toEqual(['CafeTerrace', 'CafeBay']);
    expect(bay.fade).toBe(true); expect(terrace.fade).toBeUndefined();
    expect(terrace.height).toBeCloseTo(adoptedBy('CafeTerrace')!.size[1], 3);
  });
  test('the terrace box takes in the wall the block measured, and the tables and palms inside it are not overlaps', () => {
    const wall = (dr.pieces.find(p => p.piece === 'CafeBay') as unknown as { solids: { polygon: V[] }[] }).solids.flatMap(s => s.polygon).map(toPlan), ring = civicMass(base.w, 'civic/cafe/terrace').ring;
    const [xs, ys, rx, ry] = [wall.map(p => p[0]), wall.map(p => p[1]), ring.map(p => p[0]), ring.map(p => p[1])];
    for (const [a, b] of [[Math.min(...rx), Math.min(...xs)], [Math.max(...rx), Math.max(...xs)], [Math.min(...ry), Math.min(...ys)], [Math.max(...ry), Math.max(...ys)]] as const) expect(Math.abs(a - b)).toBeLessThan(0.15); // each side within 0.15 m of the wall's own box
    const table = civicMass(base.w, 'civic/cafe-table-0');
    expect(inPoly(table.kit!.at, ring)).toBe(true);
    expect(civicClearance(base.w.civic)).toEqual([]);
  });
  test('seven beds: a kerb and a hedge each, 171 tufts in Vela\'s counts, 15,390 triangles', () => {
    expect(beds.map(m => m.id).sort()).toEqual(['dropoff-west', 'entrance-east', 'entrance-west', 'facade-southeast', 'facade-southwest', 'office-north', 'office-west'].map(s => `civic/civic-${s}`));
    expect(hedges.map(m => m.id)).toEqual(beds.map(m => `${m.id}/hedge`).sort());
    expect(tufts).toHaveLength(171);
    const per = Object.fromEntries(beds.map(b => [b.id.replace('civic/civic-', ''), tufts.filter(t => t.id.startsWith(`${b.id}/`)).length]));
    expect(per).toEqual({ 'dropoff-west': 17, 'entrance-east': 33, 'entrance-west': 46, 'facade-southeast': 13, 'facade-southwest': 12, 'office-north': 16, 'office-west': 34 });
    expect(tufts.reduce((n, t) => n + adoptedBy(t.kit!.id)!.tris, 0)).toBe(15390);
    expect(base.w.civic.some(m => m.surface === 'park' && !m.kit)).toBe(false); // none left as a plain prism
  });
  test('kerb to mass height, hedge a rise on top, tufts on the hedge top less the drop; everything lies inside the bed\'s own ring', () => {
    for (const b of beds) {
      const h = hedges.find(m => m.id === `${b.id}/hedge`)!, own = tufts.filter(t => t.id.startsWith(`${b.id}/`));
      expect(b.height).toBeCloseTo(0.52, 3);
      expect([h.base, h.height]).toEqual([+(b.base + b.height).toFixed(3), look.beds.hedge.rise]);
      expect(h.ring.every(p => inPoly(p, b.ring))).toBe(true);
      for (const t of own) { expect(t.kit!.base).toBeCloseTo(h.base + h.height - look.beds.tufts.drop, 3); expect(t.ring.every(p => inPoly(p, b.ring))).toBe(true); }
    }
  });
  test('variant = (station + bed index) mod 3 over both rows, so neighbours differ; the yaw is the same on every run', () => {
    const order = beds.map(b => b.id).sort();
    for (const [bi, id] of order.entries()) {
      const own = tufts.filter(t => t.id.startsWith(`${id}/`));
      own.forEach((t, i) => expect(t.kit!.id).toBe(look.beds.tufts.pieces[(i + bi) % 3]));
      own.slice(1).forEach((t, i) => expect(t.kit!.id).not.toBe(own[i]!.kit!.id));
    }
    expect(new Set(tufts.map(t => t.kit!.yaw)).size).toBeGreaterThan(100);
    const again = generated(plan, d, civ).w.civic.filter(m => /\/tuft-\d+$/.test(m.id));
    expect(again.map(t => t.kit!.yaw)).toEqual(tufts.map(t => t.kit!.yaw));
  });
  test('the civic masses draw the override table of civic-look.json and no other mass replaces a material; the pool its own water, the planters the brass bead', () => {
    const drawn = Object.fromEntries(base.w.civic.filter(m => m.materials).map(m => [m.id, m.materials]));
    expect(drawn).toEqual(look.materials);
    expect(civicMass(base.w, 'civic/atom-fountain').materials).toEqual({ DK_Enamel_Turquoise: 'DK_Enamel_Seafoam', DK_Lamp: 'DK_Lamp_Deco', DK_Water: 'DK_Pool_Civic' });
    const planters = ['north-east', 'north-west', 'south-west', 'south-east'].map(c => `civic/fountain-garden-${c}`);
    // DK_Accent_Gold is brass #99784D in the kit now, the same colour as DK_Brass_Civic: the slot swap is kept (it is in the export, and Latch builds from it), not needed for the colour
    for (const id of planters) expect(civicMass(base.w, id).materials).toMatchObject({ DK_Accent_Gold: 'DK_Brass_Civic', DK_Faience_Teal: 'DK_Enamel_Seafoam', DK_Leaf_CoreShrub: 'DK_Leaf_CivicDark' });
    expect(planters.map(id => civicMass(base.w, id).materials!.DK_Flower_Coral)).toEqual(['DK_Flower_CoralSoft', 'DK_Flower_Flamingo', 'DK_Flower_CoralSoft', 'DK_Flower_Flamingo']);
    expect(civicMass(base.w, 'civic/foreground-civic-robot').materials).toEqual({ DK_Signal_Amber: 'DK_Glow_Seafoam', DK_Face_Amber: 'DK_Face_Teal', DK_Enamel_Coral: 'DK_Enamel_Coral17' });
    expect(adoptedBy('AtomFountain')!.materials).toContain('DK_Water');
    expect(adoptedBy('PlanterArc')!.materials).toContain('DK_Accent_Gold');
  });
  test('every material a civic mass takes is held by the palette and carried by the kit (a swap target with no file would import pink)', () => {
    const kit = JSON.parse(readFileSync(resolve(root, 'contracts/world/kit.json'), 'utf8')) as { assets: { status: string; files: { path: string }[] }[] };
    const carried = new Set(kit.assets.filter(a => a.status === 'adopted').flatMap(a => a.files.map(f => f.path)));
    const held = new Set(look.palette.materials.map(m => m.material));
    for (const to of new Set(Object.values(look.materials).flatMap(m => Object.values(m)))) { expect(held.has(to), `${to} in the palette`).toBe(true); expect(carried.has(`Materials/${to}.mat`), `${to} in the kit`).toBe(true); }
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
  test('crossings (brief 009): a stud or a signal halo on a Handover point fails; the orb overhead, the dot rows and the arcs flush with the paving pass', () => {
    const at = (kind: string) => base.w.street.find(s => s.kind === kind)!;
    const near = (p: V) => handoverPoints(toPlan).sort((a, b) => Math.hypot(a.p[0] - p[0], a.p[1] - p[1]) - Math.hypot(b.p[0] - p[0], b.p[1] - p[1]))[0]!;
    const onPoint = (w: World, id: string) => { const s = w.street.find(q => q.id === id)!, h = near(s.at); s.at = h.p; if (s.signal) s.signal.halo.at = h.p; return h.name; };
    const stud = at('crossing-stud'), orb = at('signal-orb'), bar = at('crossing-dots'), arc = at('crossing-arc');
    let name = '';
    const a = rules(w => { name = onPoint(w, stud.id); });
    expect(a['CD-1']!.ok).toBe(false); expect(a['CD-1']!.detail).toContain(`${stud.id} (crossing stud) is 0.00 m from '${name}'`);
    const b = rules(w => { name = onPoint(w, orb.id); });
    expect(b['CD-1']!.ok).toBe(false); expect(b['CD-1']!.detail).toContain(`${orb.id} (signal halo) is 0.00 m from '${name}'`);
    const c = rules(w => { for (const id of [bar.id, arc.id]) { const s = w.street.find(q => q.id === id)!; s.at = near(s.at).p; } }); // flat lights and inlay: exempt, as flat inlay is
    expect(c['CD-1']!.ok).toBe(true);
    const d = rules(w => { const s = w.street.find(q => q.id === orb.id)!; s.at = near(s.at).p; }); // the orb hovers overhead: only its halo (left where it was) counts
    expect(d['CD-1']!.ok).toBe(true);
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
  test('CD-2 fails a replaced slot the piece does not have, and a replacement the palette does not list', () => {
    expect(overrideProblems(base.w.civic)).toEqual([]);
    const fountain = (m: Record<string, string>): Mass => ({ ...civicMass(base.w, 'civic/atom-fountain'), materials: m });
    const wrongSlot = rules(w => { civicMass(w, 'civic/atom-fountain').materials = { DK_Nothing: 'DK_Pool_Civic' }; })['CD-2']!;
    expect(wrongSlot.ok).toBe(false); expect(wrongSlot.detail).toContain('replaces DK_Nothing, which AtomFountain does not draw');
    expect(overrideProblems([fountain({ DK_Water: 'DK_Lagoon' })])).toEqual(['civic/atom-fountain draws DK_Water as DK_Lagoon, which the palette does not list']);
    expect(overrideProblems([fountain({ DK_Water: 'DK_Pool_Civic' })], () => undefined)).toEqual([]); // a pinned kit digest lists no materials: the slot cannot be judged, the palette still is
    expect(overrideProblems([{ ...fountain({ DK_Water: 'DK_Pool_Civic' }), kit: undefined }])).toEqual(['civic/atom-fountain replaces DK_Water, which a mass with no kit piece does not draw']);
  });
  test('CD-3 counts the LOD0 triangles of the new pieces as placed (the tufts and the terrace among them), and fails over the budget', () => {
    const n = civicTris(base.w.civic);
    expect(n).toBeGreaterThan(80000); expect(n).toBeLessThanOrEqual(look.budget.lod0Tris);
    expect(n - civicTris(base.w.civic.filter(m => !/\/tuft-\d+$/.test(m.id)))).toBe(15390);
    expect(civicTris(base.w.civic.filter(m => m.id !== 'civic/cafe/terrace'))).toBe(n - 3372);
    expect(budgetRule(base.w.civic).ok).toBe(true);
    expect(budgetRule(base.w.civic, n - 1).ok).toBe(false);
  });
  test('CD-4 fails a material near a signal colour, and one the palette does not list', () => {
    expect(paletteRule(look.pieces).ok).toBe(true);
    const near = { ...look.palette, materials: [...look.palette.materials, { material: 'DK_Test_Cones', unlit: '#E8392F' }] };
    expect(paletteRule(['X'], near, () => ['DK_Test_Cones']).detail).toContain('from cones');
    expect(paletteRule(['X'], look.palette, () => ['DK_Unlisted']).detail).toContain('DK_Unlisted (X) has no unlit colour');
  });
  test('CD-4 also holds the bed kerb and hedge, the replaced slots and the whole look palette', () => {
    const apart = drawnApart(base.w.civic);
    expect(apart.map(a => a.material)).toEqual(expect.arrayContaining(['DK_Limestone', 'DK_Leaf_CivicDark', 'DK_Pool_Civic']));
    expect(paletteRule(look.pieces, look.palette, undefined, apart).ok).toBe(true);
    expect(paletteRule([], look.palette, undefined, [{ material: 'DK_Mauve', by: 'bed kerb' }]).detail).toContain('DK_Mauve (bed kerb) has no unlit colour');
    const near = { ...look.palette, materials: [...look.palette.materials, { material: 'DK_Test_Guard', unlit: '#EE9A22' }] };
    expect(paletteRule([], near).detail).toContain('DK_Test_Guard is');
    expect(paletteRule([], near).detail).toContain('from guard');
    expect(look.palette.materials.map(m => m.material)).toEqual(expect.arrayContaining(['DK_Brass_Civic', 'DK_Banner_Comet_Seafoam', 'DK_Pool_Civic']));
  });
  test('every piece brief 008 added is named in the placement kinds or a swap, so none is orphaned', () => {
    const kinds = placementKinds(dr), used = new Set([...kinds.keys(), ...Object.values(look.swaps), ...Object.keys(look.place), ...Object.values(look.companions).flat(), ...look.beds.tufts.pieces, ...look.pieces.filter(p => /^(CafeSet_Tulip_Play|PavilionPier)/.test(p))]);
    expect(look.pieces.filter(p => !used.has(p) && !/^PlanterRun_(2|Corner|End)/.test(p))).toEqual([]);
  });
});
