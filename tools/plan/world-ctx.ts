// What the world rules (world-checks.ts, level-checks.ts, street-checks.ts) share: the generated world for a plan (cached
// by content hash, because generating takes seconds and the checker must stay under two), small geometry helpers, and the
// Check builder. Rules read the generator's output in process; nothing here shells out.
import { createHash } from 'node:crypto';
import { existsSync, mkdirSync, readdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import type { StreetItem, World } from '../../contracts/world/world.schema';
import { worldOf } from '../world/build';
import { look } from '../world/civic/look';
import type { Generated } from '../world/gen';
import { adopted } from '../world/gen/kitrefs';
import type { Check } from './checks';
import type { Civic } from './civic';
import { type V, lineDist, nearLine } from './geom';
import { boxRing } from './street-zones';
import type { Derived, Plan } from './plan';

const root = resolve(import.meta.dir, '../..');
export type WCtx = { plan: Plan; d: Derived; civ: Civic; w: World; gen: Generated; S: any; B: any; L: any; F: any };

const walk = (dir: string): string[] => readdirSync(dir).flatMap(f => { const p = join(dir, f); return statSync(p).isDirectory() ? walk(p) : /\.ts$/.test(f) && !/\.test\.ts$/.test(f) ? [p] : []; });
/**
 * The generator's source, hashed once as this process loaded it (here, after the modules above are evaluated): the code in memory is what generates, so an edit made
 * since is not what a world made now was made with.
 */
const SOURCE = (() => {
  const h = createHash('sha1'), src = [...walk(resolve(root, 'tools/world')), ...['geom', 'plan', 'civic', 'transit', 'block', 'graph', 'street-zones'].map(n => resolve(root, `tools/plan/${n}.ts`))].sort();
  for (const f of src) { h.update(f); h.update(readFileSync(f)); }
  return h.digest('hex');
})();
/**
 * Hash of everything the generated world is a function of, as this process has it: the plan, the civic block, the adopted kit catalogue and the civic look as the
 * generator loaded them, and the generator and the plan geometry it imports as loaded. Never the bytes on disk now: a process that loaded the look before an edit
 * generates from the old values, and a key made from the new file would file that world under the new inputs (out/cache, 2026-10-14). `adopted()` and `look` are
 * what the generator reads, so they follow `withKit` and `withLook` too.
 */
export function worldKey(plan: Plan, civ: Civic): string {
  const h = createHash('sha1');
  h.update(JSON.stringify(plan)); h.update(JSON.stringify(civ)); h.update(JSON.stringify(adopted())); h.update(JSON.stringify(look)); h.update(SOURCE);
  return h.digest('hex').slice(0, 16);
}

const memo = new Map<string, { w: World; gen: Generated }>();
/** The world for this plan: from memory, else out/cache/world-<key>.json, else generated (and cached). Same bytes either way. */
export function generated(plan: Plan, d: Derived, civ: Civic): { w: World; gen: Generated } {
  const key = worldKey(plan, civ), hit = memo.get(key);
  if (hit) return hit;
  const file = resolve(root, `out/cache/world-${plan.id}.json`);
  if (existsSync(file)) {
    try { const c = JSON.parse(readFileSync(file, 'utf8')); if (c.key === key) { memo.set(key, c.v); return c.v; } } catch { /* regenerate */ }
  }
  const { world, gen } = worldOf(plan, d, civ), v = { w: world, gen };
  memo.set(key, v);
  mkdirSync(dirname(file), { recursive: true });
  writeFileSync(file, JSON.stringify({ key, v }));
  return v;
}

export const ctxOf = (plan: Plan, d: Derived, civ: Civic, v?: { w: World; gen: Generated }): WCtx => {
  const g = v ?? generated(plan, d, civ), s = plan.standards;
  return { plan, d, civ, w: g.w, gen: g.gen, S: s.streetscape, B: s.buildings, L: s.levels, F: s.frontage };
};

/** A check from a list of offenders: value = clean/total, detail = the first few offenders. */
export function rule(id: string, group: string, name: string, total: number, bad: string[], limit: string, source: string, ok = !bad.length): Check {
  return { id, group, rule: name, value: `${total - bad.length}/${total}`, limit, ok, detail: bad.length ? `${bad.length} fail: ${bad.slice(0, 6).join('; ')}${bad.length > 6 ? '; …' : ''}` : undefined, source };
}
/** A check that is a statement with a measured value. */
export const stat = (id: string, group: string, name: string, value: string, limit: string, ok: boolean, source: string, detail?: string): Check => ({ id, group, rule: name, value, limit, ok, detail, source });

export const f1 = (x: number) => x.toFixed(1);
export const TOL = 0.011; // metres: the export rounds to 1 mm
/**
 * Metres a regenerated tower's built tip may differ from the tip its order states (K-2). The plan has no tip tolerance in its standards;
 * this is the 0.5 that SK-1 and SG-1 allowed on a tip before K-2, and they now read it from here. Orders state a tip to 0.1 m and the pieces are built to them within 0.05 m
 * (tools/decokit/fit.ts), but two live orders state a ceiling the piece sits under by 0.10 and 0.17 m (lantern-beacon, crescent-tower).
 * A wrong storey is 3.6 m, seven times this, so a height cut that leaves the piece behind always fails.
 */
export const TIP_TOL = 0.5;

export type R4 = [number, number, number, number];
/** Distance from p to rect r (0 inside). */
export const rectDist = (p: V, r: R4) => Math.hypot(Math.max(r[0] - p[0], 0, p[0] - r[1]), Math.max(r[2] - p[1], 0, p[1] - r[3]));
export const inRectXY = (p: V, r: R4, m = 0) => p[0] >= r[0] - m && p[0] <= r[1] + m && p[1] >= r[2] - m && p[1] <= r[3] + m;
export const bb = (p: V[]): R4 => [Math.min(...p.map(a => a[0])), Math.max(...p.map(a => a[0])), Math.min(...p.map(a => a[1])), Math.max(...p.map(a => a[1]))];
export const rectRing = (r: R4): V[] => [[r[0], r[2]], [r[1], r[2]], [r[1], r[3]], [r[0], r[3]]];
export { boxRing, samples9 } from './street-zones';
export { ringGap } from './street-zones';
export { nearLine, lineDist };
export const itemRing = (s: Pick<StreetItem, 'at' | 'size' | 'yaw'>): V[] => boxRing(s.at, s.size[0], s.size[1], s.yaw);
