// Motive brief 067: the start-delay ensemble. The final fights are knife-edge (a tick moves who takes whom), so one composed run per value
// says little; this composes each plan with every operative's first step k seconds late (a player a beat slower at the start, the world's
// schedule unchanged; steps pinned to a clock time move with it) and counts the outcomes. A plan that will not compose at k plays the steps that do and reports why (a loss).
// Usage (repo root, one at a time: it writes the tree's rulebook): [RB="id=value …"] [PROBE_…=…] TAG=x bun briefs/067/motive/ens.ts <k,k,…> <plan id>…
import { existsSync, mkdirSync, writeFileSync, appendFileSync } from 'node:fs';
import { join } from 'node:path';
const S = join(import.meta.dir, '../../../out/scratch/motive/067'), TREE = join(S, 'tree');
const rb = Bun.spawnSync(['python3', '-I', join(import.meta.dir, 'rb.py'), join(TREE, 'contracts/rules/rulebook.orig.json'), join(TREE, 'contracts/rules/rulebook.json'), ...(process.env.RB ?? '').split(' ').filter(Boolean)], { stdout: 'pipe', stderr: 'pipe' });
if (rb.exitCode !== 0) throw new Error(rb.stderr.toString());
const { compose, loadPlan } = await import(join(TREE, 'tools/missions/check/compose.ts'));
const { engineAt, playAll } = await import(join(TREE, 'tools/missions/check/engine.ts'));
const e = engineAt('tree', 'tree');
const [ks, ...ids] = process.argv.slice(2), tag = process.env.TAG ?? 'x', K = ks!.split(',').map(Number);
const out = join(import.meta.dir, 'results', `ens-${tag}.tsv`); mkdirSync(join(import.meta.dir, 'results'), { recursive: true });
writeFileSync(out, `# ens ${tag} · RB=${process.env.RB ?? ''} · ${Object.entries(process.env).filter(([k]) => k.startsWith('PROBE_')).map(([k, v]) => `${k}=${v}`).join(' ')}\nplan\tk\toutcome\tfail\tend\tT\tA\tSt\tvale-in\tspring\tcomposed\n`);
const clock = (t: number) => `${Math.floor(t / 600)}:${((t % 600) / 10).toFixed(1).padStart(4, '0')}`;
const jobs = ids.flatMap(id => K.map(k => ({ id, k })));
let next = 0;
await Promise.all(Array.from({ length: Math.min(6, jobs.length) }, async () => {
  while (next < jobs.length) {
    const { id, k } = jobs[next++]!;
    const inTree = join(TREE, 'contracts/missions/handover/routes/plans', `${id}.plan.json`);
    const plan = loadPlan(existsSync(inTree) ? inTree : join(S, 'plans', `${id}.plan.json`));
    const seen = new Set<string>();
    // every operative's first step k s late, and every step pinned to a clock time ('at') k s later: the cell's whole timeline k s late
    // against the world's (event-triggered steps follow their events)
    const tk = (a: string) => { const [m, r] = a.split(':'), [sec, d] = r!.split('.'); return (Number(m) * 60 + Number(sec)) * 10 + Number(d ?? 0); };
    const fmt = (t: number) => `${Math.floor(t / 600)}:${String(Math.floor((t % 600) / 10)).padStart(2, '0')}.${t % 10}`;
    // JMODE=op: each operative its own delay, 0-2 s in 0.1 s steps, drawn from sample k's stream (FNV-1a of "k:op"), so the cell's own
    // timing (who shoots first, who walks in first) varies too, not only the cell against the world
    const fnv = (t: string) => { let h = 0x811c9dc5; for (const c of t) { h ^= c.charCodeAt(0); h = Math.imul(h, 0x01000193) >>> 0; } return h; };
    const delay = (op: string) => process.env.JMODE === 'op' ? (k === 0 ? 0 : (fnv(`${k}:${op}`) % 21) / 10) : k;
    const steps = plan.steps.map((s: any) => {
      let x = s; const d = delay(s.op && s.op !== 'cell' ? s.op : 'cell');
      if (s.at) x = { ...x, at: fmt(tk(s.at) + Math.round(d * 10)) };
      if (s.op && s.op !== 'cell' && !seen.has(s.op)) { seen.add(s.op); x = { ...x, gap: (s.gap ?? 0) + d }; }
      return x;
    });
    const jp = { ...plan, steps }; let script, composed = 'yes';
    try { script = (await compose(jp, e, `${id}-k${k}`)).script; } catch (err) {
      const msg = (err as Error).message, n = Number(msg.match(/ step (\d+) /)?.[1] ?? 0);
      composed = `no: ${msg.replace(`${id}-k${k} `, '').slice(0, 70)}`;
      if (!n) { appendFileSync(out, `${id}\t${k}\tnone\t\t\t\t\t\t\t\t${composed}\n`); continue; }
      script = (await compose({ ...jp, steps: steps.slice(0, n - 1) }, e, `${id}-k${k}`)).script;
    }
    const [p] = await playAll([{ name: `${id}-k${k}-play`, script }], e, 1, { replay: false });
    const first = (c: string, re?: RegExp) => { const l = p.log.find((x: any) => x.code === c && (!re || re.test(x.text))); return l ? clock(l.tick) : '-'; };
    appendFileSync(out, `${id}\t${k}\t${p.outcome}\t${p.fail ?? ''}\t${clock(p.tick)}\t${p.trail}\t${p.alert}\t${p.standing}\t${first('board.in', /Vale/)}\t${first('fight.ambush')}\t${composed}\n`);
  }
}));
console.log(`wrote ${out}`);
