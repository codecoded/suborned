// Signatures and stations in the export (brief 075 remap step 2): the old SpaceHQ at the old builders' scale and yaw, and every station at its plan place on its line's heading.
import { expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { ctxOf } from './common';
import { generateSignatures, generateStructures, stationYaw } from './signatures';

const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), ctx = ctxOf(plan, d, civic(plan.civic, read(plan.civic.block)));

test('a signature with `scale` stands its kit piece scaled, to base + size.y × scale; `oldYaw` (Unity degrees) gives kit yaw −oldYaw', () => {
  const p = structuredClone(plan) as Plan & { signatures: any[] }, s = p.signatures.find(x => x.id === 'space-hq')!;
  Object.assign(s, { kit: 'SpaceHQ', scale: 1.35, oldYaw: 0 }); delete s.regen;
  const o = generateSignatures({ ...ctx, plan: p }).find(x => x.id === 'signature/space-hq')!;
  expect(o.source).toBe('kit'); expect(o.kit?.id).toBe('SpaceHQ'); expect(o.kit?.scale).toEqual([1.35, 1.35, 1.35]); expect(o.kit?.yaw).toBe(0);
  expect(o.tip).toBeCloseTo(ctx.pav + 252 * 1.35, 3);
  s.oldYaw = 90; expect(generateSignatures({ ...ctx, plan: p }).find(x => x.id === 'signature/space-hq')!.kit?.yaw).toBeCloseTo(-Math.PI / 2, 4);
  // failing example: no scale reads as × 1, the 252 m tip the old builders do not stand
  delete s.scale; expect(generateSignatures({ ...ctx, plan: p }).find(x => x.id === 'signature/space-hq')!.tip).toBeCloseTo(ctx.pav + 252, 3);
});

test('every station stands at its plan place, on its line\'s heading (east–west π/2, north–south 0)', () => {
  const st = generateStructures(ctx).filter(x => x.kind === 'station');
  for (const l of plan.skyrail) for (const s of (l.stations ?? []) as { id: string; path?: string; at: [number, number]; kit?: string }[]) {
    if (!s.kit) continue;
    const o = st.find(x => x.id === `station/${l.id}/${s.id}`)!;
    expect(o.kit?.at, o.id).toEqual(s.at);
    expect(o.kit?.yaw, o.id).toBe(stationYaw(ctx, l.id, s));
  }
  const harbour = st.find(x => x.id === 'station/S2/harbour')!; // Datum's find: it stood at (117, 0), east–west
  expect(harbour.kit?.at).toEqual([117, -140]); expect(harbour.kit?.yaw).toBe(0);
  expect(stationYaw(ctx, 'S1', { path: 'trunk', at: [-191, 0] })).toBe(1.5708);
});
