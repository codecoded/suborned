// Keel scratch (engine 0.34.1): Motive's per-operative jitter ensemble (briefs/067/motive/sq/ens.ts, JMODE=op: each operative's first step and
// clock-pinned steps 0-2 s late, drawn from FNV-1a of "k:op"; sample 0 as composed) on two real engines: A = the 0.34.0 snapshot, B = the tree.
// usage (repo root): bun briefs/067/keel/ens341.ts <out.tsv> <plan path>...
import { appendFileSync, writeFileSync } from 'node:fs';
import { basename, join, resolve } from 'node:path';
import { compose, loadPlan } from '../../../tools/missions/check/compose';
import { engineAt, playAll, type EngineAt } from '../../../tools/missions/check/engine';
const ROOT = resolve(import.meta.dir, '../../..');
const [outFile, ...plans] = process.argv.slice(2);
const A: EngineAt = { rev: 'dir', commit: 'pre-0.34.1', dir: join(ROOT, 'out/scratch/keel/pre-0.34.1'), hash: '', inputs: 'commit' };
const B = engineAt('tree', 'tree');
const K = [0, 1, 2, 3, 4, 5, 6, 7];
const fnv = (t: string) => { let h = 0x811c9dc5; for (const c of t) { h ^= c.charCodeAt(0); h = Math.imul(h, 0x01000193) >>> 0; } return h; };
const tk = (a: string) => { const [m, r] = a.split(':'), [sec, d] = r!.split('.'); return (Number(m) * 60 + Number(sec)) * 10 + Number(d ?? 0); };
const fmt = (t: number) => `${Math.floor(t / 600)}:${String(Math.floor((t % 600) / 10)).padStart(2, '0')}.${t % 10}`;
writeFileSync(outFile!, 'engine\tplan\tk\toutcome\tfail\tend\tT\tA\tSt\tfight.over\tcomposed\n');
const jobs = [A, B].flatMap(e => plans.flatMap(p => K.map(k => ({ e, p, k }))));
let next = 0;
await Promise.all(Array.from({ length: 6 }, async () => {
  while (next < jobs.length) {
    const { e, p, k } = jobs[next++]!; const id = basename(p).replace(/\.plan\.json$/, ''), tag = e === A ? '0.34.0' : '0.34.1';
    const plan = loadPlan(p); const seen = new Set<string>();
    const delay = (op: string) => (k === 0 ? 0 : (fnv(`${k}:${op}`) % 21) / 10);
    const steps = plan.steps.map((s: any) => {
      let x = s; const d = delay(s.op && s.op !== 'cell' ? s.op : 'cell');
      if (s.at) x = { ...x, at: fmt(tk(s.at) + Math.round(d * 10)) };
      if (s.op && s.op !== 'cell' && !seen.has(s.op)) { seen.add(s.op); x = { ...x, gap: (s.gap ?? 0) + d }; }
      return x;
    });
    const jp = { ...plan, steps }; let script, composed = 'yes';
    try { script = (await compose(jp, e, `${tag}-${id}-k${k}`)).script; } catch (err) {
      const msg = (err as Error).message, n = Number(msg.match(/ step (\d+) /)?.[1] ?? 0);
      composed = `no: ${msg.replace(`${tag}-${id}-k${k} `, '').slice(0, 70)}`;
      if (!n) { appendFileSync(outFile!, `${tag}\t${id}\t${k}\tnone\t\t\t\t\t\t\t${composed}\n`); continue; }
      script = (await compose({ ...jp, steps: steps.slice(0, n - 1) }, e, `${tag}-${id}-k${k}`)).script;
    }
    const [pl] = await playAll([{ name: `${tag}-${id}-k${k}-play`, script }], e, 1, { replay: false });
    const fo = pl!.log.find((x: any) => x.code === 'fight.over');
    appendFileSync(outFile!, `${tag}\t${id}\t${k}\t${pl!.outcome}\t${pl!.fail ?? ''}\t${fmt(pl!.tick)}\t${pl!.trail}\t${pl!.alert}\t${pl!.standing}\t${fo ? fmt(fo.tick) : '-'}\t${composed}\n`);
  }
}));
console.log(`wrote ${outFile}`);
process.exit(0);
