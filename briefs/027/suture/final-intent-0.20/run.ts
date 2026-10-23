// Suture, brief 027, engine 0.20.0 (ai.intent.final agent, PROVISIONAL): adversarial retest of Motive's "no exploit" claim (tuning.md §15.2).
// Each attempt is a plan (plans/<id>.plan.json, derived from contracts/missions/handover/routes/plans/loud.plan.json), composed by the
// missions composer and played on the working-tree engine through the batch server (brief 042). Also plays any ready script in scripts/.
// Usage: bun briefs/027/suture/final-intent-0.20/run.ts [id ...]   (no ids: every plan and script)
// Env: FINAL=every plays the same composed script on a scratch rulebook with ai.intent.final every (the control), see control.ts.
import { readdirSync, readFileSync, writeFileSync, mkdirSync, existsSync } from 'node:fs';
import { join } from 'node:path';
import { loadPlan, writeScript } from '../../../../tools/missions/check/compose';
import { compose } from './compose-partial';
import { engineAt, play, type Play, type Script, type EngineAt } from '../../../../tools/missions/check/engine';

const DIR = import.meta.dir, OUT = process.env.RESDIR ?? join(DIR, 'results'), PLANS = process.env.PLANDIR ?? join(DIR, 'plans'), SCRIPTS = process.env.SCRIPTDIR ?? join(DIR, 'scripts');
mkdirSync(OUT, { recursive: true }); mkdirSync(SCRIPTS, { recursive: true });
const ops = ['scout', 'fixer', 'tech', 'enforcer'] as const;
export const clock = (t: number) => `${Math.floor(t / 600)}:${((t % 600) / 10).toFixed(1).padStart(4, '0')}`;

/** What matters for the claim: outcome, time, T, Alert, hits on the cell, and, per agent, when it first drew or fired and every shot at it. */
export function summary(id: string, r: Play) {
  const shotAt = (o: string) => r.log.filter(l => /^shot\.(hit|miss)$/.test(l.code) && new RegExp(`^\\S+ fires on ${o} `).test(l.text));
  const hits = r.log.filter(l => l.code === 'shot.hit' && new RegExp(`fires on (${ops.join('|')}) `).test(l.text)).length;
  const down = ops.filter(o => r.log.some(l => l.code === `${o}.down`));
  const left = r.log.filter(l => l.code === 'captured').map(l => l.text.split(' ')[1]);
  const per = ops.map(o => {
    const drew = r.log.find(l => new RegExp(`^${o} draws`, 'i').test(l.text) || (l.code.startsWith('order.') && new RegExp(`^${o} draws`).test(l.text)));
    const fired = r.log.find(l => /^shot\./.test(l.code) && l.text.startsWith(`${o} fires`));
    const sa = shotAt(o), firstAt = sa[0];
    const dirty = Math.min(drew?.tick ?? 1e9, fired?.tick ?? 1e9);
    const whileClean = sa.filter(l => l.tick < dirty);
    return { op: o, drew: drew ? clock(drew.tick) : '-', fired: fired ? clock(fired.tick) : '-', shotAt: sa.length, hitBy: sa.filter(l => l.code === 'shot.hit').length,
      firstShotAt: firstAt ? `${clock(firstAt.tick)} by ${firstAt.text.split(' ')[0]}` : '-', whileClean: whileClean.length };
  });
  const line = `${id.padEnd(34)} ${r.outcome} ${r.fail || ''} ${clock(r.tick)} T${r.trail} A${r.alert} hits ${hits}/18 (${Math.round(hits * 100 / 18)}%) down[${down}] left[${left}] replay ${r.replayIdentical}`;
  return { line, per, hits, down, left };
}

export async function runOne(id: string, e: EngineAt, tag = ''): Promise<string> {
  const planFile = join(PLANS, `${id}.plan.json`), scriptFile = join(SCRIPTS, `${id}.json`);
  let script: Script; let partial = '';
  const t0 = performance.now();
  if (existsSync(planFile)) {
    const plan = loadPlan(planFile);
    const part: { failed?: string } = {};
    try { ({ script } = await compose(plan, e, `s20-${id}${tag}`, () => {}, part)); }
    catch (err) { return `${id.padEnd(34)} NOT COMPOSED ${(err as Error).message.slice(0, 160)}`; }
    if (part.failed) partial = part.failed.replace(/^\S+ /, '');
    writeScript(scriptFile.replace('.json', `${tag}.json`), plan.note, script);
  } else script = JSON.parse(readFileSync(scriptFile, 'utf8'));
  const r = await play(script, `s20-${id}${tag}-final`, e);
  writeFileSync(join(OUT, `${id}${tag}.log.txt`), r.log.map(l => `${clock(l.tick)} ${l.code} ${l.text}`).join('\n') + '\n');
  const s = summary(id + tag, r);
  const refused = r.orders.filter(o => !o.ok).map(o => `${clock(o.order.tick)} ${o.order.op ?? ''} ${o.order.do} ${o.order.target ?? ''}: ${o.why}`);
  const out = [s.line + (partial ? `  PARTIAL (stopped at ${partial})` : '') + `  [${((performance.now() - t0) / 1000).toFixed(1)} s]`,
    ...s.per.map(p => `    ${p.op.padEnd(8)} drew ${p.drew.padEnd(6)} fired ${p.fired.padEnd(6)} shot at ${String(p.shotAt).padStart(2)} (hit ${p.hitBy}) first ${p.firstShotAt}; while clean ${p.whileClean}`),
    ...refused.map(x => `    refused ${x}`)].join('\n');
  return out;
}

if (import.meta.main) {
  const e = engineAt('tree', 'tree');
  const ids = process.argv.slice(2).length ? process.argv.slice(2) : [...new Set([...readdirSync(PLANS).map(f => f.replace('.plan.json', ''))])].sort();
  const t0 = performance.now();
  const res = await Promise.all(ids.map(id => runOne(id, e)));
  for (const r of res) console.log(r);
  console.log(`# ${ids.length} attempts, ${((performance.now() - t0) / 1000).toFixed(1)} s wall`);
}
