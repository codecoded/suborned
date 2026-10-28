// BG-1..BG-5 (brief 075): the committed plan passes; each failing example is the committed plan with one thing changed.
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic } from './civic';
import { derive, type Plan } from './plan';
import { blockGrammarChecks } from './block-grammar';

const root = resolve(import.meta.dir, '../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), block = read(plan.civic.block);
const run = (mutate: (p: any) => void = () => {}) => { const p: any = structuredClone(plan); mutate(p); return blockGrammarChecks(p, derive(p), civic(p.civic, block)); };
const one = (id: string, mutate: (p: any) => void) => run(mutate).find(c => c.id === id)!;
const blk = (p: any, id: string) => p.blocks.find((b: any) => b.id === id);

describe('BG', () => {
  test('the committed plan passes all five', () => { const c = run(); expect(c.map(x => x.id)).toEqual(['BG-1', 'BG-2', 'BG-3', 'BG-4', 'BG-5']); for (const x of c) expect([x.id, x.ok, x.detail]).toEqual([x.id, true, undefined]); });
  test('a plan without standards.blocks has no check', () => { expect(run(p => { delete p.standards.blocks; })).toEqual([]); });
  test('BG-1: a built plot neither laid nor exempt, and a widened zone, fail', () => {
    expect(one('BG-1', p => { p.standards.blocks.plots = p.standards.blocks.plots.filter((x: string) => x !== 'lantern-west'); }).detail).toMatch(/lantern-west is neither laid as blocks nor exempt/);
    expect(one('BG-1', p => { p.standards.blocks.zone = [-300, -84, -372, -148]; }).detail).toMatch(/is not the capstone zone/);
    expect(one('BG-1', p => { p.standards.blocks.plots = p.standards.blocks.plots.filter((x: string) => x !== 'boulevard-west'); p.standards.blocks.exempt['boulevard-west'] = 'handover: near it'; p.blocks = p.blocks.filter((b: any) => b.plot !== 'boulevard-west'); }).detail).toMatch(/boulevard-west: "handover: near it" does not hold/);
  });
  test('BG-2: a run padded off the 6 m bay, and a corner cap with no partner, fail', () => {
    expect(one('BG-2', p => { blk(p, 'boulevard-west').runs[0].b[1] += 1; }).detail).toMatch(/caps \+ 6 × 4 = 40/);
    expect(one('BG-2', p => { const r = blk(p, 'terrace-s').runs.find((x: any) => x.face === 'n'); r.caps = ['end', 'corner']; r.bays += 1; }).detail).toMatch(/meets no perpendicular corner cap|caps \+ 6/);
  });
  test('BG-3: a band on a tower podium and a block under 28 deep fail', () => {
    expect(one('BG-3', p => { const r = blk(p, 'lantern-south').runs.find((x: any) => x.face === 'w'); r.b[1] += 24; r.bays += 4; }).detail).toMatch(/lantern-south w: band enters the reserved mass \[-180, -66, -484, -419\]/); // north into the Lantern Terraces
    expect(one('BG-3', p => { blk(p, 'boulevard-west').rect[0] = -40; }).detail).toMatch(/22\.0 m deep, under 28/);
  });
  test('BG-4: a street corner without its corner cell fails', () => {
    expect(one('BG-4', p => { p.blocks = p.blocks.filter((b: any) => b.id !== 'ss2'); }).detail).toMatch(/ss2: street corner \(18\.0,-459\.5\)/);
  });
  test('BG-5: a crescent bay too many leaves its building line; a crescent off the circus centre and a missing crescent fail', () => {
    expect(one('BG-5', p => { const c = p.circus.crescents.find((x: any) => x.id === 'crescent-se'); c.bays += 1; c.deg[1] -= c.stepDeg; }).detail).toMatch(/crescent-se bay 5: leaves east-wall's building line/);
    expect(one('BG-5', p => { p.circus.crescents.find((x: any) => x.id === 'crescent-sw').c = [5, 0]; }).detail).toMatch(/crescent-sw: does not start on lamplight-east's N-S facade line/); // read about its own centre, not the plan origin
    expect(one('BG-5', p => { p.circus.crescents.pop(); }).detail).toMatch(/0 crescents, one per circus plot/);
  });
});
