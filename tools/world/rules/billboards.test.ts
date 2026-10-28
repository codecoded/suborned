// BB-1..BB-4 (brief 075-F category 1). The live export passes; each failing example puts one fault in.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { World } from '../../../contracts/world/world.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { ctxOf, generated } from '../../plan/world-ctx';
import { BB } from '../gen/billboards';
import { billboardRules } from './billboards';

setDefaultTimeout(180000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block)), base = generated(plan, d, civ);
const run = (w: World) => Object.fromEntries(billboardRules(ctxOf(plan, d, civ, { w, gen: base.gen })).map(c => [c.id, c]));
const clone = (w: World): World => structuredClone(w);

test('the live export: BB-1..BB-4 pass; every sign-frame roof outside the play streets has its board, the one in the Records Precinct is left out for the cap', () => {
  const r = run(base.w);
  for (const id of ['BB-1', 'BB-2', 'BB-3', 'BB-4', 'BB-5']) expect(r[id]!.ok, `${id}: ${r[id]!.detail}`).toBe(true);
  const log = base.gen.billboards!;
  expect(log.rooftop.roofs).toBe(6); // brief 075 (the old buildings): only the remap's 4-storey corner buildings keep the sign frame among their roof programmes (roofs.ts), was 20
  expect(log.rooftop.placed + log.rooftop.left.length + log.rooftop.cap.length).toBe(6);
  expect(log.rooftop.cap.map(c => c.roof)).toEqual(['bld/lamplight-east/3/roof']); // lamplight (a play street): roof 19.88 m, board to 29.05 m, cap 20.4 (Sky's question)
  expect(base.w.roofs.filter(x => x.programme === 'sign-frame' && x.kit).length).toBe(log.rooftop.placed);
  expect(base.w.street.filter(s => s.kind === 'billboard-wall').length).toBe(log.wall.placed);
  expect(base.w.street.filter(s => s.kind === 'billboard-pylon').length).toBe(log.pylon.placed);
  expect(log.pylon.parade).toBe(2); // the vista pair at the Parade's circus mouth
  expect(base.w.street.filter(s => s.kind === 'advertising-column').length).toBe(log.column.before + log.column.placed);
});

test('the counts against the old town (14 billboards: 7 rooftop, 4 wall, 3 pylon; 8 advertising columns)', () => {
  const log = base.gen.billboards!;
  expect([log.rooftop.placed, log.wall.placed, log.pylon.placed, log.column.before + log.column.placed]).toEqual([5, 1, 10, 20]); // brief 075 (the old buildings): 5 rooftop boards on the 4-storey corners (the old town 7), 1 wall board (the 3-storey blocks show little party wall over a lower neighbour; the old town 4), 10 pylons (Datum's plan, 2026-10-28), 20 columns. Was [19, 8, 9, 20]: // brief 075 roll-out: 20 columns, one more district corner takes a column once its kerb furniture walks the 10 m palm grid (oldstreet.ts). brief 075 (golden view): 8 wall boards, was 9: the boulevard's old storey codes (oldstreet.ts) stand most of its east wall at 9 storeys side by side, so fewer party walls show over a lower neighbour
});

test('failing example (BB-1): a board on the Records Precinct roof (20.58 m) tops out at 29.75 m over the 20.4 m cap', () => {
  const w = clone(base.w), from = w.roofs.find(x => x.programme === 'sign-frame' && x.kit)!, to = w.roofs.find(x => x.id === 'bld/boulevard-west/3/roof')!;
  to.kit = { ...from.kit!, at: [to.c[0], to.c[1]] };
  expect(run(w)['BB-1']!.ok).toBe(false);
  expect(run(w)['BB-1']!.detail).toContain('over the play-street cap 20.4');
});

test('failing example (BB-1): the procedural frame left standing with the board', () => {
  const w = clone(base.w), r = w.roofs.find(x => x.programme === 'sign-frame' && x.kit)!;
  r.parts.push({ id: `${r.id}/frame`, shape: 'box', surface: 'sign', c: r.c, base: r.z + 2, size: [18, 1, 6], yaw: r.yaw });
  expect(run(w)['BB-1']!.detail).toContain('the procedural frame');
});

test('failing example (BB-2): a wall board lifted past the top of its wall', () => {
  const w = clone(base.w), s = w.street.find(x => x.kind === 'billboard-wall')!;
  s.z += 60;
  expect(run(w)['BB-2']!.ok).toBe(false);
  expect(run(w)['BB-2']!.detail).toContain('rises over the wall');
});

test('failing example (BB-2): a wall board dropped onto the lower neighbour\'s roof', () => {
  const w = clone(base.w), s = w.street.find(x => x.kind === 'billboard-wall')!;
  s.z = 0.18;
  expect(run(w)['BB-2']!.detail).toContain('is not clear of');
});

test('failing example (BB-3): the Parade\'s vista pylon moved into the mall\'s walk band', () => {
  const w = clone(base.w), s = w.street.find(x => x.kind === 'billboard-pylon' && x.edge === 'foot/crown-parade')!;
  s.at = [s.at[0], Math.sign(s.at[1]) * 14];
  expect(run(w)['BB-3']!.ok).toBe(false);
  expect(run(w)['BB-3']!.detail).toContain("crown-parade's walk zone");
});

test('failing example (BB-3): an advertising column grown to 1.2 reaches past the kerb zone', () => {
  const w = clone(base.w), s = w.street.find(x => x.kind === 'advertising-column' && /\/e\d+$/.test(x.edge))!;
  s.kit = { ...s.kit!, scale: [1.2, 1.2, 1.2] };
  expect(run(w)['BB-3']!.ok).toBe(false);
});

test('failing example (BB-4): a BladeSign_Hotel on a diner; two advertising columns 10 m apart', () => {
  const w = clone(base.w), f = w.frontages.find(x => x.trade === 'diner' && x.signKit)!;
  f.signKit = { ...f.signKit!, id: 'BladeSign_Hotel' };
  expect(run(w)['BB-4']!.detail).toContain('BladeSign_Hotel on a diner');
  const w2 = clone(base.w), cols = w2.street.filter(x => x.kind === 'advertising-column');
  cols[1]!.at = [cols[0]!.at[0] + 10, cols[0]!.at[1]];
  expect(run(w2)['BB-4']!.detail).toContain(`(< ${BB.columnGap})`);
});

test('failing example (BB-4, posters): two boards in one view showing the same ad; a board with no pick', () => {
  const w = clone(base.w), quay = w.street.filter(x => x.kind === 'billboard-pylon' && x.edge === 'foot/crown-parade'), [a, b] = quay.map(q => w.posters!.placed.find(p => p.host === q.id)!);
  b!.key = a!.key; b!.slots = { ...a!.slots };
  expect(run(w)['BB-4']!.detail).toContain('show the same ad in one view');
  const w2 = clone(base.w); w2.posters!.placed = w2.posters!.placed.filter(p => !p.host.startsWith('bld/'));
  expect(run(w2)['BB-4']!.detail).toContain('has no poster pick');
});

test('BB-5: every CornerTower_12 stands on a building with a hotel tenancy; failing example: one moved onto a building without', () => {
  const w = clone(base.w), hotels = new Set(w.frontages.filter(f => f.level === 'street' && f.trade === 'hotel-lobby').map(f => f.host));
  // brief 075: the old blocks' corner cells take the class's StreamCorner (Aster's remap: no 9-storey hotel corners left), so the export may hold no CornerTower_12; the
  // failing example then makes one from a corner piece
  if (!w.facades.some(f => f.kit.id === 'CornerTower_12')) { const c = w.facades.find(f => f.form === 'corner' && hotels.has(f.building)) ?? w.facades.find(f => f.form === 'corner')!; c.kit = { ...c.kit, id: 'CornerTower_12' }; if (!hotels.has(c.building)) hotels.add(c.building), w.frontages.push({ ...w.frontages.find(f => f.level === 'street' && f.trade === 'hotel-lobby')!, host: c.building }); }
  const towers = w.facades.filter(f => f.kit.id === 'CornerTower_12');
  expect(towers.length).toBeGreaterThan(0);
  expect(towers.every(f => hotels.has(f.building))).toBe(true);
  const other = w.buildings.map(m => m.id.split('/').slice(0, 3).join('/')).find(id => id.startsWith('bld/') && !hotels.has(id))!;
  towers[0]!.building = other;
  const r = run(w)['BB-5']!;
  expect(r.ok).toBe(false);
  expect(r.detail).toContain(other);
});
