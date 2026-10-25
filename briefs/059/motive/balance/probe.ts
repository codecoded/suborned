// Motive 059c probe: compose plans (out/scratch/motive/059c/plans/<id>.plan.json) on the scratch engine (tree copy of 0.32 + probe switches)
// and play them; prints outcome lines and writes scripts/<id>.json and logs/<id>.log. Usage: bun probe.ts <id>… [--log]
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
const HERE = import.meta.dir, TREE = join(HERE, 'tree');
const { compose, loadPlan } = await import(join(TREE, 'tools/missions/check/compose.ts'));
const { engineAt, playAll } = await import(join(TREE, 'tools/missions/check/engine.ts'));
const e = engineAt('tree', 'tree');
const ids = process.argv.slice(2).filter(a => !a.startsWith('--'));
mkdirSync(join(HERE, 'scripts'), { recursive: true }); mkdirSync(join(HERE, 'logs'), { recursive: true });
const clock = (t: number) => `${Math.floor(t / 600)}:${((t % 600) / 10).toFixed(1).padStart(4, '0')}`;
await Promise.all(ids.map(async id => {
  const plan = loadPlan(join(HERE, 'plans', `${id}.plan.json`));
  let script;
  let partial = '';
  try { script = (await compose(plan, e, id)).script; } catch (err) {
    const msg = (err as Error).message, n = Number(msg.match(/ step (\d+) /)?.[1] ?? 0);
    if (!n || process.env.PARTIAL === '0') { console.log(`${(process.env.TAG ?? '').padEnd(10)} ${id}: NOT COMPOSED ${msg}`); return; }
    // 2026-10-26: play what composes (the steps before the one not taken) to the plan's end, so the log shows why the step never comes
    partial = ` [PARTIAL: ${msg.replace(id + ' ', '')}]`;
    script = (await compose({ ...plan, steps: plan.steps.slice(0, n - 1) }, e, id)).script;
  }
  script.note = plan.note; writeFileSync(join(HERE, 'scripts', `${id}${process.env.TAG ? '.' + process.env.TAG : ''}.json`), JSON.stringify(script, null, 1) + '\n');
  const [p] = await playAll([{ name: id, script }], e, 1, { replay: false });
  writeFileSync(join(HERE, 'logs', `${id}${process.env.TAG ? '.' + process.env.TAG : ''}.log`), p.log.map((l: any) => `${clock(l.tick)}  ${l.code.padEnd(28)} ${l.text}`).join('\n') + '\n');
  const first = (c: string) => { const l = p.log.find((x: any) => x.code === c); return l ? clock(l.tick) : '-'; };
  const caps = p.log.filter((l: any) => l.code === 'captured').map((l: any) => l.text.split(' ')[1]).join(',');
  console.log(`${(process.env.TAG ?? '').padEnd(10)} ${id}: ${p.outcome}${p.fail ? ' ' + p.fail : ''} at ${clock(p.tick)} T ${p.trail} A ${p.alert} St ${p.standing} gun ${p.gunfire} | spring ${first('fight.ambush')} blocker.out ${first('blocker.out')} over ${first('fight.over')} break ${first('ai.break')} vale-in ${(p.log.find((l: any) => l.code === 'board.in' && /Vale/.test(l.text)) ? clock(p.log.find((l: any) => l.code === 'board.in' && /Vale/.test(l.text)).tick) : '-')}${caps ? ' captured ' + caps : ''}${partial}`);
}));
