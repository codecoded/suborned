// Suture, brief 027, engine 0.20.0: each attempt composed and played twice: on the working tree (ai.intent.final agent, as built) and on
// a scratch copy of the engine whose rulebook says ai.intent.final every (out/suture020/every; the control: what the same player gets under
// the old rule). Each is composed on its own engine (the composer re-times every conditional step), so the comparison is player against player.
// Usage: bun briefs/027/suture/final-intent-0.20/both.ts [id ...]   (no ids: every plan in plans/)   ONLY=agent|every to run one side.
import { readdirSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { engineAt, engineHash, type EngineAt } from '../../../../tools/missions/check/engine';
import { runOne } from './run';

const DIR = import.meta.dir, ROOT = resolve(DIR, '../../../..');
const every: EngineAt = (() => { const dir = join(ROOT, 'out/suture020/every'); return { rev: 'every', commit: 'every', dir, hash: engineHash(dir), inputs: 'tree' }; })();
const agent = engineAt('tree', 'tree');
const ids = process.argv.slice(2).length ? process.argv.slice(2) : readdirSync(process.env.PLANDIR ?? join(DIR, 'plans')).map(f => f.replace('.plan.json', '')).sort();
const t0 = performance.now();
const sides = (process.env.ONLY ? [process.env.ONLY] : ['agent', 'every']) as ('agent' | 'every')[];
const res = await Promise.all(ids.flatMap(id => sides.map(s => runOne(id, s === 'agent' ? agent : every, s === 'agent' ? '' : '.every').then(r => [id, s, r] as const))));
for (const id of ids) for (const s of sides) { const r = res.find(x => x[0] === id && x[1] === s)!; console.log(`[${s}] ${r[2]}`); }
console.log(`# ${ids.length} attempts x ${sides.length} engines, ${((performance.now() - t0) / 1000).toFixed(1)} s wall`);
