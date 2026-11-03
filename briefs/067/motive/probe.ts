// Motive brief 067 probe: compose plans on the scratch engine (engine 0.33.1 + engine-probe.patch, the rulebook the tree holds now) and play
// them; prints one outcome line each and writes scripts/<id>.<tag>.json and logs/<id>.<tag>.log under out/scratch/motive/067.
// A plan id is looked up in the tree's routes/plans (the reference ids, after cand.sh's swap), then in out/scratch/motive/067/plans.
// Usage (repo root): [RB="id=value …"] [PROBE_…=…] TAG=x bun briefs/067/motive/probe.ts <plan id>…  (run one at a time: it writes the tree's rulebook)
import { existsSync, mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
const S = join(import.meta.dir, '../../../out/scratch/motive/067'), TREE = join(S, 'tree');
// the rulebook for this run: RB="id=value …" over the committed one (rulebook.orig.json), written before the engine starts
const rb = Bun.spawnSync(['python3', '-I', join(import.meta.dir, 'rb.py'), join(TREE, 'contracts/rules/rulebook.orig.json'), join(TREE, 'contracts/rules/rulebook.json'), ...(process.env.RB ?? '').split(' ').filter(Boolean)], { stdout: 'pipe', stderr: 'pipe' });
if (rb.exitCode !== 0) throw new Error(rb.stderr.toString());
const { compose, loadPlan } = await import(join(TREE, 'tools/missions/check/compose.ts'));
const { engineAt, playAll } = await import(join(TREE, 'tools/missions/check/engine.ts'));
const e = engineAt('tree', 'tree');
const ids = process.argv.slice(2).filter(a => !a.startsWith('--')), tag = process.env.TAG ?? 'x';
mkdirSync(join(S, 'scripts'), { recursive: true }); mkdirSync(join(S, 'logs'), { recursive: true });
const clock = (t: number) => `${Math.floor(t / 600)}:${((t % 600) / 10).toFixed(1).padStart(4, '0')}`;
await Promise.all(ids.map(async id => {
  const inTree = join(TREE, 'contracts/missions/handover/routes/plans', `${id}.plan.json`);
  const plan = loadPlan(existsSync(inTree) ? inTree : join(S, 'plans', `${id}.plan.json`));
  let script, partial = '';
  try { script = (await compose(plan, e, id)).script; } catch (err) {
    const msg = (err as Error).message, n = Number(msg.match(/ step (\d+) /)?.[1] ?? 0);
    if (!n) { console.log(`${tag.padEnd(8)} ${id}: NOT COMPOSED ${msg}`); return; }
    partial = ` [PARTIAL: ${msg.replace(id + ' ', '')}]`; // play what composes (the steps before the one not taken), so the log shows why
    script = (await compose({ ...plan, steps: plan.steps.slice(0, n - 1) }, e, id)).script;
  }
  script.note = plan.note; writeFileSync(join(S, 'scripts', `${id}.${tag}.json`), JSON.stringify(script, null, 1) + '\n');
  const [p] = await playAll([{ name: id, script }], e, 1, { replay: false });
  writeFileSync(join(S, 'logs', `${id}.${tag}.log`), p.log.map((l: any) => `${clock(l.tick)}  ${l.code.padEnd(28)} ${l.text}`).join('\n') + '\n');
  const first = (c: string, re?: RegExp) => { const l = p.log.find((x: any) => x.code === c && (!re || re.test(x.text))); return l ? clock(l.tick) : '-'; };
  const caps = p.log.filter((l: any) => l.code === 'captured').map((l: any) => l.text.split(' ')[1]).join(',');
  console.log(`${tag.padEnd(8)} ${id}: ${p.outcome}${p.fail ? ' ' + p.fail : ''} at ${clock(p.tick)} T ${p.trail} A ${p.alert} St ${p.standing} gun ${p.gunfire} | spring ${first('fight.ambush')} blocker.out ${first('blocker.out')} over ${first('fight.over')} vale-in ${first('board.in', /Vale/)}${caps ? ' captured ' + caps : ''}${partial}`);
}));
