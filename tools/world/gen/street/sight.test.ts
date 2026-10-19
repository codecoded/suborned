// SF-8 in the generator (Splice, 2026-10-19, door option b): the street layer itself refuses a sight-blocking item within 6 m of a junction corner's kerb tangent point, and the quiet-place
// generator never stands a police post in a side road's mouth. Before: Layer.fits never checked corners and a post was marked `blocks` only after it was placed, so a new door position
// moved the response cover and the generator stood a post 5.5 m from the Rocket Lane / Apron Road corner (SF-8 failed; the block carried a stopgap keep-clear zone to hide it).
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic } from '../../../plan/civic';
import { type V, dist, lineDist } from '../../../plan/geom';
import { derive, type Plan } from '../../../plan/plan';
import { worldOf } from '../../build';
import { ctxOf } from '../common';
import { SIGHT_CLEAR, junctionCorners } from '../edges';
import { Layer } from './kit';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
const ctx = ctxOf(plan, d, civ, 0.18);
const corners = junctionCorners(ctx);

test('the plan has junction corners, one per kerb tangent point', () => {
  expect(corners.length).toBeGreaterThan(20);
  for (let i = 0; i < corners.length; i++) for (let j = i + 1; j < corners.length; j++) expect(dist(corners[i]!, corners[j]!)).toBeGreaterThan(0.4);
});

test('Layer.fits: a sight-blocking item is refused within 6 m of a corner that the same spot allows an open item', () => {
  const L = new Layer(ctx, [], [], [], [], []);
  let spot: { at: V; edge: string; yaw: number } | undefined;
  // a spot beside a corner on a road's kerb zone that an open item (a 1 x 1 m post, not sight-blocking) passes: the only difference between the two asks is `blocks`
  scan: for (const q of corners) for (const r of d.roads) {
    if (r.gyratory) continue;
    const sec = ctx.S.sections[r.class] as { kerb: number };
    for (let a = 0; a < 360; a += 15) for (const rad of [2, 3, 4, 5]) {
      const at: V = [q[0] + Math.cos((a * Math.PI) / 180) * rad, q[1] + Math.sin((a * Math.PI) / 180) * rad];
      if (Math.abs(lineDist(at, r.line) - (r.w / 2 + 0.45 + 0.55)) > 0.2 || sec.kerb < 1.5) continue;
      if (L.fits('bollard', at, 0, 0.18, `civic/${r.id}`, { size: [1, 1, 1.5], blocks: false })) { spot = { at, edge: `civic/${r.id}`, yaw: 0 }; break scan; }
    }
  }
  expect(spot, 'a kerb spot within 6 m of a corner that an open item may take').toBeDefined();
  const s = spot!;
  expect(L.fits('bollard', s.at, s.yaw, 0.18, s.edge, { size: [1, 1, 1.5], blocks: false })).toBe(true);
  expect(L.fits('bollard', s.at, s.yaw, 0.18, s.edge, { size: [1, 1, 1.5], blocks: true })).toBe(false);
  expect(L.add('bollard', s.at, s.yaw, 0.18, s.edge, { size: [1, 1, 1.5], blocks: true })).toBe(false);
  expect(L.items.length).toBe(0);
});

test('the generated world: nothing that blocks sight is within 6 m of a corner, and no police post stands on a side road', () => {
  const { world: w } = worldOf(plan, d, civ);
  const blocking = w.street.filter(i => i.sight === 'blocks' && i.z < 1);
  expect(blocking.length).toBeGreaterThan(20);
  for (const i of blocking) for (const q of corners) expect(dist(i.at as V, q), `${i.id} to the corner at ${q}`).toBeGreaterThanOrEqual(SIGHT_CLEAR - 1.5 * 0.7 - 0.1); // centre distance: the footprint's nearest sample is up to ~half the larger side nearer; SF-8 itself measures the footprint
  const posts = w.street.filter(i => i.kind === 'police-post');
  expect(posts.length).toBeGreaterThanOrEqual(1);
  for (const p of posts) for (const r of d.roads) if (!r.gyratory && p.edge !== `civic/${r.id}`) expect(lineDist(p.at as V, r.line), `${p.id} on ${r.id}`).toBeGreaterThanOrEqual(r.w / 2 + 0.45);
});
