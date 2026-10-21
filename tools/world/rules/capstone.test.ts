// CAP-1..CAP-4 on the live world (brief 026): each passes on the world the generator makes and fails on one thing put wrong, on a copy of it. Also the programme's determinism and its coverage.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { StreetItem, World } from '../../../contracts/world/world.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { ctxOf, generated } from '../../plan/world-ctx';
import { handoverPoints } from '../civic/handover';
import { toPlanFn } from '../../plan/civic';
import { capstoneRules } from './capstone';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block)), base = generated(plan, d, civ);
const failing = (mutate: (w: World) => void = () => {}): Record<string, string> => {
  const w = structuredClone(base.w) as World; mutate(w);
  return Object.fromEntries(capstoneRules(ctxOf(plan, d, civ, { w, gen: base.gen })).filter(c => !c.ok).map(c => [c.id, c.detail ?? '']));
};
const first = (w: World, kind: string) => [...(w.ground.pieces ?? []), ...w.street].find(s => s.kind === kind)!;
const move = (s: StreetItem, to: [number, number]) => { s.at = to; };

describe('the capstone programme', () => {
  test('the live world passes CAP-1..CAP-7', () => { expect(failing()).toEqual({}); });
  test('it places the ground, the robots by trade, vending pairs, kerb programme and planting, and says what it could not place', () => {
    const n = (k: string) => [...(base.w.ground.pieces ?? []), ...base.w.street].filter(s => s.kind === k && s.source === 'kit').length;
    for (const k of ['kerb', 'pave-flags', 'tree-pit', 'window-box', 'tactile', 'terrazzo', 'drain', 'manhole', 'service-robot', 'vending-machine', 'bike-rack', 'sandwich-board', 'hanging-basket']) expect(n(k), k).toBeGreaterThan(0);
    expect(base.gen.capstone!.gaps.every(g => g.rule && g.where && g.why)).toBe(true);
    const roles = new Set(base.w.street.filter(s => s.kind === 'service-robot').map(s => s.variant));
    expect([...roles].sort()).toEqual(expect.arrayContaining(['butler', 'porter', 'shopkeeper', 'waiter']));
  });
  test('every programme item stands at the same place twice (deterministic)', () => {
    const again = generated(plan, d, civ), ids = (w: World) => w.street.filter(s => s.id.startsWith('street/capstone/')).map(s => `${s.id}@${s.at}`);
    expect(ids(again.w)).toEqual(ids(base.w));
  });
  test('CAP-1 fails on a ground piece of the wrong kind and on a drain across a crossing strip', () => {
    expect(failing(w => { first(w, 'drain').kit = { id: 'Bench', scale: [1, 1, 1] }; })['CAP-1']).toContain('not an adopted ground:drain');
    expect(failing(w => { const x = w.crossings![0]!; move(first(w, 'drain'), [(x.strip[0]![0] + x.strip[2]![0]) / 2, (x.strip[0]![1] + x.strip[2]![1]) / 2]); })['CAP-1']).toContain('crossing strip');
  });
  test('CAP-2 fails on a vending machine in the middle of a block, a robot with no job and a nanny with no bench', () => {
    expect(failing(w => move(first(w, 'vending-machine'), [-150, -250]))['CAP-2']).toContain('not backed');
    expect(failing(w => { first(w, 'service-robot').variant = 'juggler'; })['CAP-2']).toContain('no job');
    expect(failing(w => { const r = first(w, 'service-robot'); r.variant = 'nanny'; move(r, [-150, -250]); })['CAP-2']).toContain('nanny');
  });
  test('CAP-3 fails on a piece on a Handover point and on a standing piece on the civic square', () => {
    const p = handoverPoints(toPlanFn(plan.civic))[0]!.p;
    expect(failing(w => move(first(w, 'drain'), p))['CAP-3']).toContain('from');
    expect(failing(w => move(first(w, 'bike-rack'), [-160, -250]))['CAP-3']).toContain('civic');
  });
  test('CAP-4 fails on a piece that is not the kit piece of its kind', () => {
    expect(failing(w => { first(w, 'drain').kit = { id: 'Bench', scale: [1, 1, 1] }; })['CAP-4']).toContain('not a drain');
    expect(failing(w => { first(w, 'bike-rack').kit = { id: 'NoSuchPiece', scale: [1, 1, 1] }; })['CAP-4']).toContain('not adopted');
  });
  test('CAP-6 fails on an urn standing on a palm, and kerb stones and pits are placed by rule', () => {
    expect(failing(w => { const t = w.street.find(x => x.kind === 'tub-palm')!, p = w.street.find(x => x.kind === 'planter')!; p.at = t.at; p.kit = { id: 'Planter_Urn_Play', scale: [1, 1, 1] }; })['CAP-6']).toContain('shares its spot');
    expect(failing(w => move(first(w, 'kerb'), [-150, -250]))['CAP-1']).toContain('off a carriageway');
    expect(failing(w => move(first(w, 'tree-pit'), [-153.1, -250.3]))['CAP-1']).toContain('no tree');
  });
  test('CAP-7 fails on a board in front of a machine and on two pieces on one footprint', () => {
    expect(failing(w => { const m = w.street.find(x => x.kind === 'vending-machine')!, b = w.street.find(x => x.kind === 'sandwich-board')!, f = [-Math.sin(m.yaw), Math.cos(m.yaw)]; b.at = [m.at[0] + f[0] * 0.9, m.at[1] + f[1] * 0.9]; })['CAP-7']).toContain('in front of');
    expect(failing(w => { const a = w.street.find(x => x.kind === 'bike-rack')!, b = w.street.find(x => x.kind === 'info-lightbox')!; b.at = a.at; })['CAP-7']).toContain('share a footprint');
  });
});
