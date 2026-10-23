// The batch player's equality proof (Splice, brief 042): `suborned-sim batch` must give, for every script, the bytes `play <script> --json`
// gives. Takes a sample (rob-loud-* plays from a robustness run, the recorded routes, the scenarios), plays each in its own process
// (the reference), then plays the same list in one batch process (all cores, then one thread), through the server, and without the
// replay check; and compares line by line.
//   bun tools/sim/batch-proof.ts [--rob <dir>] [--rob-n 500] [--root <engine root: sim, contracts/rules; its bin/suborned-sim.dll>]
// With no --root it builds and uses the working tree's engine, which moves under Keel's hands; give a frozen root to prove against one engine.
import { existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { ROOT, engineAt, build, env } from '../missions/check/engine';

const arg = (k: string, d?: string) => { const i = process.argv.indexOf(k); return i > 0 ? process.argv[i + 1]! : d; };
const eroot = arg('--root') ? resolve(arg('--root')!) : ROOT, dll = arg('--root') ? join(eroot, 'bin/suborned-sim.dll') : build(engineAt('tree'));
const robDir = resolve(arg('--rob', join(ROOT, 'out/motive027/s15/tree2/out/missions/plays'))!);
const robN = Number(arg('--rob-n', '500'));
const OUT = join(ROOT, 'out/batch-proof'); mkdirSync(OUT, { recursive: true });

const run = (args: string[], stdin?: string) => { const r = Bun.spawnSync(['dotnet', dll, ...args], { cwd: eroot, env, stdin: stdin === undefined ? undefined : new TextEncoder().encode(stdin), stdout: 'pipe', stderr: 'pipe', maxBuffer: 1 << 30 }); return { out: r.stdout.toString(), err: r.stderr.toString(), code: r.exitCode }; };

// the sample: every 4th... evenly spaced rob-loud plays, every recorded route and plan-composed script, every scenario
const items: string[] = [];
if (existsSync(robDir)) {
  const all = readdirSync(robDir).filter(f => f.startsWith('rob-loud') && f.endsWith('.json')).sort(), step = Math.max(1, Math.floor(all.length / robN));
  for (let i = 0; i < all.length && items.length < robN; i += step) items.push(join(robDir, all[i]!));
}
const nRob = items.length;
for (const d of ['contracts/missions/handover/routes', 'briefs/011/motive-fights']) for (const f of readdirSync(join(ROOT, d)).filter(f => f.endsWith('.json')).sort()) {
  const t = readFileSync(join(ROOT, d, f), 'utf8'); if (t.includes('suborned.sim.script/1')) items.push(join(ROOT, d, f));
}
const nRoute = items.length - nRob;
const ids = run(['list']).out.trim().split('\n').map(l => l.split('\t')[0]!);
items.push(...ids);
console.log(`sample: ${items.length} = ${nRob} rob-loud plays, ${nRoute} route scripts, ${ids.length} scenarios`);

// reference: one process per item, 16 at a time
const ref: string[] = new Array(items.length); let next = 0; const t0 = Date.now();
await Promise.all(Array.from({ length: 16 }, async () => {
  while (next < items.length) { const i = next++; const p = Bun.spawn(['dotnet', dll, 'play', items[i]!, '--json'], { cwd: eroot, env, stdout: 'pipe', stderr: 'pipe' }); ref[i] = (await new Response(p.stdout).text()).replace(/\n$/, ''); await p.exited; }
}));
const tRef = (Date.now() - t0) / 1000;
writeFileSync(join(OUT, 'reference.jsonl'), ref.join('\n') + '\n');
console.log(`reference: ${items.length} processes in ${tRef.toFixed(1)} s`);

const same = (name: string, got: string[], want = ref) => {
  let bad = 0, bytes = 0;
  for (let i = 0; i < items.length; i++) { bytes += want[i]!.length; if (got[i] !== want[i]) { if (bad++ < 3) console.log(`  DIFFERS ${items[i]}\n    want ${want[i]!.slice(0, 160)}\n    got  ${got[i]?.slice(0, 160)}`); } }
  console.log(`${bad === 0 ? 'ok  ' : 'FAIL'} ${name}: ${items.length - bad}/${items.length} byte-identical (${(bytes / 1e6).toFixed(0)} MB)`);
  return bad === 0;
};
let ok = true;
const list = join(OUT, 'items.txt'); writeFileSync(list, items.join('\n') + '\n');
const lines = (s: string) => s.replace(/\n$/, '').split('\n');

for (const jobs of [undefined, '1', '3']) {
  const t = Date.now(), r = run(['batch', '--list', list, ...(jobs ? ['--jobs', jobs] : [])]);
  console.log(`  batch ${jobs ? `--jobs ${jobs}` : 'all cores'}: ${((Date.now() - t) / 1000).toFixed(1)} s; ${r.err.trim()}`);
  ok = same(`batch (${jobs ? `${jobs} thread${jobs === '1' ? '' : 's'}` : 'all cores'})`, lines(r.out)) && ok;
}
{ const t = Date.now(), r = run(['batch', '-'], items.join('\n') + '\n'); console.log(`  batch - (stdin): ${((Date.now() - t) / 1000).toFixed(1)} s`); ok = same('batch from stdin', lines(r.out)) && ok; }
{ // no replay: identical but for the one field, which says it was not checked
  const t = Date.now(), r = run(['batch', '--list', list, '--no-replay']); console.log(`  batch --no-replay: ${((Date.now() - t) / 1000).toFixed(1)} s; ${r.err.trim()}`);
  const want = ref.map(l => l.replace('"replayIdentical":true', '"replayIdentical":null'));
  ok = same('batch --no-replay (replayIdentical null, nothing else changed)', lines(r.out), want) && ok;
  const replayed = ref.filter(l => !l.includes('"replayIdentical":true')).length; console.log(`  reference lines whose replay differs or errored: ${replayed}`);
}
{ // the server: tags, any order
  const input = items.map((p, i) => `t${i}\treplay\t${p}`).join('\n') + '\n', t = Date.now(), r = run(['batch', '--serve'], input);
  const got: string[] = new Array(items.length);
  for (const l of lines(r.out)) { const k = l.indexOf('\t'); got[Number(l.slice(1, k))] = l.slice(k + 1); }
  console.log(`  batch --serve: ${((Date.now() - t) / 1000).toFixed(1)} s`);
  ok = same('batch --serve', got) && ok;
}
console.log(ok ? 'PROOF OK' : 'PROOF FAILED');
process.exit(ok ? 0 : 1);
