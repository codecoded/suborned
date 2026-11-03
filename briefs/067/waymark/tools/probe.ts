// Waymark c038 probe (scratch): compose plans on the working tree's engine (0.34.1) and play them; one line each; logs and scripts under
// out/scratch/waymark/{logs,scripts}. A plan is a path, or an id looked up in out/scratch/waymark/plans, then the routes' plans.
// JITTER=n: also n per-operative jitter samples (each op's first step 0-2 s late, Motive's JMODE=op stream) and the wins of n+1.
// Usage (repo root): [JITTER=7] [LOG=1] bun out/scratch/waymark/tools/probe.ts <plan>…
import { existsSync, mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { compose, loadPlan, type Plan } from '../../../../tools/missions/check/compose';
import { engineAt, playAll, type Play, type Script } from '../../../../tools/missions/check/engine';

const ROOT = join(import.meta.dir, '../../../..'), S = join(ROOT, 'out/scratch/waymark');
const e = engineAt('tree', 'tree');
for (const d of ['scripts', 'logs']) mkdirSync(join(S, d), { recursive: true });
const clock = (t: number) => `${Math.floor(t / 600)}:${((t % 600) / 10).toFixed(1).padStart(4, '0')}`;
const tk = (a: string) => { const [m, r] = a.split(':'), [sec, d] = r!.split('.'); return (Number(m) * 60 + Number(sec)) * 10 + Number(d ?? 0); };
const fmt = (t: number) => `${Math.floor(t / 600)}:${String(Math.floor((t % 600) / 10)).padStart(2, '0')}.${t % 10}`;
const fnv = (t: string) => { let h = 0x811c9dc5; for (const c of t) { h ^= c.charCodeAt(0); h = Math.imul(h, 0x01000193) >>> 0; } return h; };

export function resolve(id: string): { id: string; plan: Plan } {
  const cands = [id, join(S, 'plans', `${id}.plan.json`), join(ROOT, 'contracts/missions/handover/routes/plans', `${id}.plan.json`)];
  const f = cands.find(c => c.endsWith('.json') && existsSync(c));
  if (!f) throw new Error(`no plan ${id}`);
  return { id: f.split('/').at(-1)!.replace('.plan.json', ''), plan: loadPlan(f) };
}
/** each operative's first step k's own delay (0-2 s), and every `at` with it (Motive's JMODE=op) */
export function jitter(plan: Plan, k: number): Plan {
  if (k === 0) return plan;
  const seen = new Set<string>(), delay = (op: string) => (fnv(`${k}:${op}`) % 21) / 10;
  return { ...plan, steps: plan.steps.map(s => {
    let x = s; const d = delay(s.op && s.op !== 'cell' ? s.op : 'cell');
    if (s.at) x = { ...x, at: fmt(tk(s.at) + Math.round(d * 10)) };
    if (s.op && s.op !== 'cell' && !seen.has(s.op)) { seen.add(s.op); x = { ...x, gap: (s.gap ?? 0) + d }; }
    return x;
  }) };
}
export async function run(name: string, plan: Plan): Promise<{ p: Play; script: Script; partial: string }> {
  let script: Script, partial = '';
  try { script = (await compose(plan, e, `wm-${name}`)).script; } catch (err) {
    const msg = (err as Error).message, n = Number(msg.match(/ step (\d+) /)?.[1] ?? 0);
    partial = ` [PARTIAL: ${msg.replace(`wm-${name} `, '')}]`;
    if (!n) throw err;
    script = (await compose({ ...plan, steps: plan.steps.slice(0, n - 1) }, e, `wm-${name}`)).script;
  }
  const [p] = await playAll([{ name: `wm-${name}-play`, script }], e, 1, { replay: false });
  return { p: p!, script, partial };
}
export function line(name: string, p: Play, partial = '') {
  const first = (c: string, re?: RegExp) => { const l = p.log.find(x => x.code === c && (!re || re.test(x.text))); return l ? clock(l.tick) : '-'; };
  const held = p.log.filter(l => l.code.startsWith('car.held.')).map(l => `${l.code.slice(9)}:${l.text.split(' ')[0]}@${clock(l.tick)}`).join(',');
  const left = (p.debrief?.nextMission?.lines ?? []).filter(l => l.line === 'left-behind').map(l => `${l.who}:${l.detail}`).join(',');
  const rep = p.log.filter(l => l.code === 'attention.reporting').map(l => clock(l.tick)).join(',');
  return `${name}: ${p.outcome}${p.fail ? ' ' + p.fail : ''} at ${clock(p.tick)} T ${p.trail} A ${p.alert} St ${p.standing} gun ${p.gunfire} | in ${first('enter.in')} vale ${first('board.in', /Vale/)} fight ${first('fight.safe') !== '-' ? 'safe ' + first('fight.safe') : first('fight.loud') !== '-' ? 'loud ' + first('fight.loud') : '-'} spring ${first('fight.ambush')} bout ${first('blocker.out')} free ${first('car.free')} over ${first('fight.over')}${held ? ' held ' + held : ''}${rep ? ' REPORT ' + rep : ''}${left ? ' left ' + left : ''}${p.orders.some(o => !o.ok) ? ` rej ${p.orders.filter(o => !o.ok).length}` : ''}${partial}`;
}

if (import.meta.main) {
  const ids = process.argv.slice(2), J = Number(process.env.JITTER ?? 0);
  await Promise.all(ids.map(async raw => {
    const { id, plan } = resolve(raw);
    const ks = Array.from({ length: J + 1 }, (_, k) => k);
    const rows = await Promise.all(ks.map(async k => { try { return { k, ...(await run(`${id}-k${k}`, jitter(plan, k))) }; } catch (err) { return { k, err: (err as Error).message }; } }));
    let wins = 0;
    for (const r of rows) {
      if ('err' in r) { console.log(`${id} k${r.k}: NOT COMPOSED ${r.err}`); continue; }
      if (r.p.outcome === 'success' && !r.partial) wins++;
      if (r.k === 0) {
        writeFileSync(join(S, 'scripts', `${id}.json`), JSON.stringify(r.script, null, 1) + '\n');
        writeFileSync(join(S, 'logs', `${id}.log`), r.p.log.map(l => `${clock(l.tick)}  ${l.code.padEnd(28)} ${l.text}`).join('\n') + '\n');
      }
      if (r.k === 0 || process.env.LOG) console.log(line(`${id} k${r.k}`, r.p, r.partial));
    }
    if (J) console.log(`${id}: wins ${wins}/${J + 1}`);
  }));
  process.exit(0);
}
