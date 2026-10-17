// SEC-1: an area's security base is a level 0..4 with a reason. Failing examples for each way it breaks.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { resolve } from 'node:path';
import { check } from './checks';
import { civic } from './civic';
import { derive, type Plan } from './plan';

setDefaultTimeout(30000);
const root = resolve(import.meta.dir, '../..'), read = async (p: string) => JSON.parse(await Bun.file(resolve(root, p)).text());
const plan: Plan = await read('contracts/world/harbour-district.plan.json'), block = await read(plan.civic.block);
const sec = (p: Plan) => check(p, derive(p), civic(p.civic, block)).checks.find(c => c.id === 'SEC-1')!;
const edit = (f: (p: Plan) => void) => { const p = structuredClone(plan); f(p); return sec(p); };

test('SEC-1 passes on the committed plan: civic 2, harbour 1', () => {
  const c = sec(plan);
  expect(c.ok).toBe(true);
  expect(c.value).toBe('records 2, harbour 1');
});
test('SEC-1 fails on a level outside 0..4, a fraction, or text', () => {
  for (const v of [5, -1, 1.5, '2' as unknown as number]) expect(edit(p => { p.areas.find(a => a.id === 'harbour')!.securityBase = v; }).ok).toBe(false);
});
test('SEC-1 fails when a security base gives no reason', () => {
  expect(edit(p => { delete p.areas.find(a => a.id === 'records')!.securityWhy; }).ok).toBe(false);
});
test('SEC-1 holds a plan that sets none, so a pinned older plan stays checkable (the Handover\'s level is held by tools/sim/site.ts)', () => {
  const c = edit(p => { for (const a of p.areas) { delete a.securityBase; delete a.securityWhy; } });
  expect(c.ok).toBe(true);
  expect(c.value).toBe('none set');
});
