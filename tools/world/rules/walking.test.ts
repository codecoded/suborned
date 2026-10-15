// W-5 on the live world (standards.walking.body; Datum 2026-10-15, rule by Splice): the plan checker's walks keep body.r from the ground solids, and the walks between the block's doors, Handover
// points and crossing landings pass no gap the standard does not allow. Each failing example takes one allowance away, or puts a solid in the way, on a copy of the live world.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { ctxOf, generated } from '../../plan/world-ctx';
import { extraRules } from './index';
import { blockPoints, seatingAreas } from './walking';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), block = read(plan.civic.block), base = (() => { const civ = civic(plan.civic, block); return { civ, ...generated(plan, derive(plan), civ) }; })();
const w5 = (p: Plan = plan, civ = base.civ) => { const d = derive(p); return extraRules(ctxOf(p, d, civ, { w: structuredClone(base.w), gen: base.gen })).find(c => c.id === 'W-5'); };

test('W-5 passes on the live block: 500-odd walks between the doors, the Handover points and the landings, with every narrow place inside the standard\'s exceptions', () => {
  const r = w5()!;
  expect(r.ok, r.detail).toBe(true);
  expect(r.value).toMatch(/^(\d+)\/\1$/); // every walk measured passes
  expect(Number(r.value!.split('/')[0])).toBeGreaterThan(450);
  expect(r.detail).toMatch(/^\d+ walks between 3\d points; \d+ narrow places inside the standard's exceptions/);
});

test('the points the walks run between: the three doors, the Handover points that are not doors, the crossing landings the dressing names; the seating area is the café terrace until the block says', () => {
  const ctx = ctxOf(plan, derive(plan), base.civ, { w: base.w, gen: base.gen });
  const pts = blockPoints(ctx);
  expect(pts.length).toBe(32);
  for (const door of base.civ.entrances) expect(pts.some(p => p.name === door.name)).toBe(true);
  expect(pts.filter(p => /^landing /.test(p.name)).length).toBe(9);
  expect(seatingAreas(ctx)).toHaveLength(1); // cafe-terrace
  const none = ctxOf(plan, derive(plan), { ...base.civ, dressing: { ...(base.civ.dressing as object), seating: ['no-such-zone'] } } as never, { w: base.w, gen: base.gen });
  expect(seatingAreas(none)).toEqual([]);
});

test('without the seating allowance the narrow places inside the café terrace pass; with the constraint taken away they fail (the four gaps Datum measured), naming what they pass between', () => {
  const strict: Plan = structuredClone(plan); (strict.standards.walking as { body: { constrained: { run: number } } }).body.constrained.run = 0; // no gap under clearWidth is allowed for any length
  const seated = w5(strict)!;
  expect(seated.ok, seated.detail).toBe(true); // the terrace's aisles are 1.3 m and its walks keep them; nothing else is narrow
  const civ = { ...base.civ, dressing: { ...(base.civ.dressing as object), seating: ['no-such-zone'] } };
  const open = w5(strict, civ as never)!;
  expect(open.ok).toBe(false);
  expect(open.detail).toMatch(/^4 fail: Office entrance → Behind café passes 1\.87 m for 0\.8 m of the walk, past the 0 m a constrained gap may run between CafeBay\/wall-front-w and cafe-table-1 /); // the 1.85 m Datum measured between the tables and the terrace wall
});

test('a plan that states no body has no W-5; nor has a block with no ground solids', () => {
  const none: Plan = structuredClone(plan); (none.standards.walking as { body?: unknown }).body = undefined;
  expect(w5(none)).toBeUndefined();
  expect(w5(plan, { ...base.civ, solids: undefined, solidIds: undefined } as never)).toBeUndefined();
});
