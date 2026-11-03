// Motive 039 open-posts: the per-operative jitter ensemble (8 samples; sample 0 as composed, else each operative's first step and
// clock-pinned steps 0-2 s late, FNV-1a of "k:op"), as briefs/067/motive/sq/ens.ts, but on the working tree's engine and data
// (engine 0.35.2, today's mission.json and site). No rulebook overrides unless RB="id=value ..." (written to a scratch copy of the tree's
// engine inputs, never the tree). Usage (repo root): bun run guard bun briefs/039/motive/open-posts/tools/ens.ts <tag> <plan id>...
import { existsSync, mkdirSync, writeFileSync, readFileSync, cpSync } from 'node:fs';
import { join } from 'node:path';
const ROOT = join(import.meta.dir, '../../../../..');
const { compose, loadPlan } = await import(join(ROOT, 'tools/missions/check/compose.ts'));
const { engineAt, playAll } = await import(join(ROOT, 'tools/missions/check/engine.ts'));
let e = engineAt('tree', 'tree');
if (process.env.RB || process.env.SITE) {
  // a scratch engine: the tree's sim + data + rules copied, the rulebook values overridden
  const dir = join(ROOT, 'out/scratch/motive/039op/eng-' + (process.env.TAGE ?? 'rb'));
  for (const f of ['sim/Suborned.Sim', 'sim/cli', 'sim/data', 'contracts/rules', 'contracts/people/people.rules.json'])
    cpSync(join(ROOT, f), join(dir, f), { recursive: true, filter: s => !/\/(bin|obj)(\/|$)/.test(s) });
  const rbf = join(dir, 'contracts/rules/rulebook.json'), rb = JSON.parse(readFileSync(rbf, 'utf8'));
  for (const kv of (process.env.RB ?? "").split(' ').filter(Boolean)) { const [k, v] = kv.split('='); if (!rb[k!]) throw new Error(`no rule ${k}`); rb[k!].v = Number(v); }
  writeFileSync(rbf, JSON.stringify(rb, null, 1));
  if (process.env.SITE) cpSync(process.env.SITE, join(dir, 'sim/data/handover.site.json'));
  const { engineHash } = await import(join(ROOT, 'tools/missions/check/engine.ts'));
  e = { rev: 'scratch', commit: 'scratch', dir, hash: engineHash(dir), inputs: 'tree' } as any;
}
const [tag, ...ids] = process.argv.slice(2);
const K = [0, 1, 2, 3, 4, 5, 6, 7];
const clock = (t: number) => `${Math.floor(t / 600)}:${((t % 600) / 10).toFixed(1).padStart(4, '0')}`;
const tk = (a: string) => { const [m, r] = a.split(':'), [sec, d] = r!.split('.'); return (Number(m) * 60 + Number(sec)) * 10 + Number(d ?? 0); };
const fmt = (t: number) => `${Math.floor(t / 600)}:${String(Math.floor((t % 600) / 10)).padStart(2, '0')}.${t % 10}`;
const fnv = (t: string) => { let h = 0x811c9dc5; for (const c of t) { h ^= c.charCodeAt(0); h = Math.imul(h, 0x01000193) >>> 0; } return h; };
const outDir = join(import.meta.dir, '../results'); mkdirSync(outDir, { recursive: true });
const jobs: any[] = [];
for (const id of ids) for (const k of K) {
  const plan = loadPlan(join(import.meta.dir, '../plans', `${id}.plan.json`));
  const seen = new Set<string>(), delay = (op: string) => (k === 0 ? 0 : (fnv(`${k}:${op}`) % 21) / 10);
  const steps = plan.steps.map((s: any) => {
    let x = s; const d = delay(s.op && s.op !== 'cell' ? s.op : 'cell');
    if (s.at) x = { ...x, at: fmt(tk(s.at) + Math.round(d * 10)) };
    if (s.op && s.op !== 'cell' && !seen.has(s.op)) { seen.add(s.op); x = { ...x, gap: (s.gap ?? 0) + d }; }
    return x;
  });
  let script, composed = 'yes';
  try { script = (await compose({ ...plan, steps }, e, `${id}-k${k}`)).script; }
  catch (err) { composed = 'no: ' + (err as Error).message.slice(0, 90); const n = Number((err as Error).message.match(/ step (\d+) /)?.[1] ?? 0); if (!n) { jobs.push({ id, k, composed }); continue; } script = (await compose({ ...plan, steps: steps.slice(0, n - 1) }, e, `${id}-k${k}`)).script; }
  // JDRAW=1: each operative's F (draw) 0-2 s after the spring (FNV-1a of "d<k>:op"; sample 0 as composed); LATE=<s>: every F that many s late.
  // Two passes: the approach as composed, then the draws pinned to their composed tick + the delay and the rest recomposed after them.
  if (script && (process.env.JDRAW || process.env.LATE)) {
    const dd = (op: string) => process.env.LATE ? Number(process.env.LATE) : (k === 0 ? 0 : (fnv(`d${k}:${op}`) % 21) / 10);
    const draws = script.orders.filter((o: any) => o.do === 'draw');
    const steps2 = steps.map((s: any) => { if (s.do !== 'draw') return s; const o = draws.find((x: any) => x.op === s.op); return o ? { ...s, at: fmt(o.tick + Math.round(dd(s.op) * 10)), every: 0.1 } : s; });
    try { script = (await compose({ ...plan, steps: steps2 }, e, `${id}-k${k}d`)).script; } catch (err) { composed = 'no(draw): ' + (err as Error).message.slice(0, 90); }
  }
  jobs.push({ id, k, composed, script });
}
const plays = await playAll(jobs.filter(j => j.script).map(j => ({ name: `op39-${j.id}-k${j.k}`, script: j.script })), e, 8, { replay: false });
let i = 0; const rows: any[] = [];
for (const j of jobs) { if (!j.script) { rows.push({ ...j }); continue; } const p = plays[i++]!; rows.push({ id: j.id, k: j.k, composed: j.composed, outcome: p.outcome, fail: p.fail, tick: p.tick, trail: p.trail, alert: p.alert, standing: p.standing, log: p.log }); }
writeFileSync(join(outDir, `ens-${tag}.json`), JSON.stringify({ tag, rb: process.env.RB ?? '', site: process.env.SITE ?? 'tree', rows }));
for (const r of rows) console.log([r.id, r.k, r.outcome, r.fail ?? '', r.tick != null ? clock(r.tick) : '', r.trail, r.alert, r.standing, r.composed].join('\t'));
