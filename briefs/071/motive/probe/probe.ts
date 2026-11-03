// Motive brief 071 probe (split getaway): compose plans on the scratch engine (HEAD 82eaae77 sim + PROBE_SPLIT switch) and play them.
// Usage (repo root, under the guard): [PROBE_SPLIT=1] TAG=x bun out/scratch/motive-split/probe.ts <plan id>...
import { existsSync, mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
const S = import.meta.dir, TREE = join(S, 'tree');
const { compose, loadPlan } = await import(join(TREE, 'tools/missions/check/compose.ts'));
const { engineAt, playAll } = await import(join(TREE, 'tools/missions/check/engine.ts'));
const e = engineAt('tree', 'tree');
const ids = process.argv.slice(2), tag = process.env.TAG ?? 'x';
mkdirSync(join(S, 'scripts'), { recursive: true }); mkdirSync(join(S, 'logs'), { recursive: true });
const clock = (t: number) => `${Math.floor(t / 600)}:${((t % 600) / 10).toFixed(1).padStart(4, '0')}`;
for (const id of ids) {
  const mine = join(S, 'plans', `${id}.plan.json`), ref = join(TREE, 'contracts/missions/handover/routes/plans', `${id}.plan.json`);
  const plan = loadPlan(existsSync(mine) ? mine : ref);
  let script, partial = '';
  try { script = (await compose(plan, e, id)).script; } catch (err) {
    const msg = (err as Error).message, n = Number(msg.match(/ step (\d+) /)?.[1] ?? 0);
    if (!n) { console.log(`${tag} ${id}: NOT COMPOSED ${msg}`); continue; }
    partial = ` [PARTIAL: ${msg.replace(id + ' ', '')}]`;
    script = (await compose({ ...plan, steps: plan.steps.slice(0, n - 1) }, e, id)).script;
  }
  writeFileSync(join(S, 'scripts', `${id}.${tag}.json`), JSON.stringify(script, null, 1) + '\n');
  const [p] = await playAll([{ name: id, script }], e, 1, { replay: false });
  writeFileSync(join(S, 'logs', `${id}.${tag}.log`), p.log.map((l: any) => `${clock(l.tick)}  ${l.code.padEnd(28)} ${l.text}`).join('\n') + '\n');
  const first = (c: string) => { const l = p.log.find((x: any) => x.code === c); return l ? clock(l.tick) : '-'; };
  const n = (re: RegExp) => p.log.filter((x: any) => re.test(x.code)).length;
  console.log(`${tag} ${id}: ${p.outcome}${p.fail ? ' ' + p.fail : ''} at ${clock(p.tick)} T ${p.trail} A ${p.alert} St ${p.standing} | take ${first('take.done')} vale-out ${first('split.vale-out')} spring ${first('fight.ambush')} blocker.out ${first('blocker.out')} over ${first('fight.over')} | op hits ${n(/^(scout|fixer|tech|enforcer)\.hit/)} downs ${n(/\.down$/)} left ${JSON.stringify(p.left ?? {})}${partial}`);
}
