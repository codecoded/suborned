// OT-1..OT-3 (brief 075 remap: the old town's ground and props). The live export passes; each failing example breaks one rule on a copy of it.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { World } from '../../../contracts/world/world.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { ctxOf, generated } from '../../plan/world-ctx';
import { oldTownRules } from './oldtown';
import { OLD_GROUND } from '../gen/oldtown/ground';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block)), base = generated(plan, d, civ);
const ot = (w: World) => Object.fromEntries(oldTownRules(ctxOf(plan, d, civ, { w, gen: base.gen })).map(c => [c.id, c]));
const clone = (w: World): World => structuredClone(w);

test('the live export: OT-1..OT-3 pass; the gardens, the harbour and the circus ring are laid, and the old props stand', () => {
  const r = ot(base.w);
  for (const id of ['OT-1', 'OT-2', 'OT-3']) expect(r[id]?.ok, `${id}: ${r[id]?.detail}`).toBe(true);
  const g = base.w.ground;
  expect(g.water.some(f => f.id === 'oldtown/gardens/basin/water')).toBe(true);
  expect(g.water.some(f => f.id === 'oldtown/gardens/canal/water')).toBe(true);
  expect(g.flats.filter(f => f.id.startsWith('oldtown/harbour/')).map(f => f.surface).sort()).toEqual(['flags', 'flags', 'limestone']);
  expect(g.quays.filter(q => q.id.startsWith('oldtown/circus/pavement-')).length).toBeGreaterThan(0);
  const old = base.w.street.filter(s => s.id.startsWith('street/old/'));
  expect(old.length).toBeGreaterThan(200);
  for (const want of ['PlazaFountain', 'Cypress', 'DecoPavilion', 'Streetlamp', 'LifebuoyStand', 'CoinTelescope', 'Bollard', 'Deckchair', 'Planter_Urn']) expect(old.some(s => s.kit?.id === want), want).toBe(true);
});

test('a plan without oldtown (a pinned plan) gets no old ground, no old props and no OT rules', () => {
  const { oldtown: _o, ...rest } = plan as Plan & { oldtown?: unknown };
  const p = rest as Plan, dp = derive(p), g = generated(p, dp, civ);
  expect(g.w.street.some(s => s.id.startsWith('street/old/'))).toBe(false);
  expect(g.w.ground.flats.some(f => f.id.startsWith('oldtown/'))).toBe(false);
  expect(oldTownRules(ctxOf(p, dp, civ, { w: g.w, gen: g.gen }))).toEqual([]);
});

test('failing example: the promenade flags taken up leave the coast quay bare (OT-1)', () => {
  const w = clone(base.w);
  w.ground.flats = w.ground.flats.filter(f => f.id !== 'oldtown/harbour/promenade');
  const r = ot(w)['OT-1']!;
  expect(r.ok).toBe(false);
  expect(r.detail).toContain('coast quay bare');
});

test('failing example: a park plot\'s lawn border missing fails OT-1', () => {
  const w = clone(base.w), id = base.w.ground.flats.find(f => /^oldtown\/lawn\/[^/]+\/border$/.test(f.id))!.id;
  w.ground.flats = w.ground.flats.filter(f => f.id !== id);
  const r = ot(w)['OT-1']!;
  expect(r.ok).toBe(false);
  expect(r.detail).toContain('no lawn border');
  expect(OLD_GROUND.lawn.border).toBeGreaterThan(0);
});

test('failing example: a bench moved into the canal fails OT-2', () => {
  const w = clone(base.w), canal = w.ground.water.find(f => f.id === 'oldtown/gardens/canal/water')!, b = w.street.find(s => s.kind === 'bench')!;
  const xs = canal.ring.map(p => p[0]), ys = canal.ring.map(p => p[1]);
  b.at = [(Math.min(...xs) + Math.max(...xs)) / 2, (Math.min(...ys) + Math.max(...ys)) / 2];
  const r = ot(w)['OT-2']!;
  expect(r.ok).toBe(false);
  expect(r.detail).toContain('oldtown/gardens/canal/water');
});

test('failing examples: an old prop scaled, one on the boulevard carriageway, one in the Handover zone, one naming a rejected piece (OT-3)', () => {
  const pick = (w: World, k: number) => w.street.filter(s => s.id.startsWith('street/old/'))[k]!;
  let w = clone(base.w); pick(w, 0).kit!.scale = [1.4, 1.4, 1.4];
  expect(ot(w)['OT-3']!.detail).toContain('scaled');
  w = clone(base.w); pick(w, 1).at = [0, -100];
  expect(ot(w)['OT-3']!.detail).toContain('carriageway of boulevard');
  w = clone(base.w); pick(w, 2).at = [-150, -260];
  expect(ot(w)['OT-3']!.detail).toContain('capstone zone');
  w = clone(base.w); pick(w, 3).kit!.id = 'TramWire_Pole';
  const r = ot(w)['OT-3']!;
  expect(r.ok).toBe(false);
  expect(r.detail).toContain('not adopted');
});
