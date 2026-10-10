// usage: bun run verify. Offline (no Docker, no model): typecheck, replay of every recorded run, plan checker. Exit 0 only if all hold.
import { readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { inProcess } from './levels/checker';
import { runDirs, runsRoot } from './levels/paths';
import { root } from './rag/config';

const PLAN = 'harbour-district', SUBMITTED = 'r006', bad: string[] = [];
const tsc = Bun.spawn(['bun', 'run', 'typecheck'], { cwd: root, stdout: 'pipe', stderr: 'pipe' }); // runs while the replays use this thread
const tscOut = Promise.all([new Response(tsc.stdout).text(), new Response(tsc.stderr).text(), tsc.exited]);

/** Pure: label a replay's drift from its warn lines, and say whether its only failure is the expected "superseded". */
function summarise(lines: string[]) {
  let sup = false; const gdd = new Map<string, number>(), added = new Set<string>(), other: string[] = [];
  for (const l of lines.filter(l => l.startsWith('warn'))) {
    const g = l.match(/recorded against GDD (v[\d.]+)/), r = l.match(/rules added since the run: ([\w\-, ]+?)(?:;|\)|$)/);
    if (g) gdd.set(g[1]!, (gdd.get(g[1]!) ?? 0) + 1);
    if (r) for (const id of r[1]!.split(',')) added.add(id.trim());
    if (/superseded run, pre-pinning/.test(l)) sup = true;
    else if (!g && !r) other.push(l);
  }
  const fails = lines.filter(l => l.startsWith('FAIL')), superseded = fails.length > 0 && fails.every(l => /^FAIL\s+superseded:/.test(l));
  const parts = [...gdd].map(([v, n]) => `recorded against GDD ${v} ×${n}`);
  if (sup) parts.push('superseded: pre-pinning, Oct 5 block');
  if (added.size) parts.push(`rules added ${[...added].sort().join(',')}`);
  if (other.length) parts.push(`other warnings ×${other.length}`);
  return { fails, superseded, drift: parts.length ? parts.join('; ') : 'none' };
}

const t0 = performance.now(), ids = runDirs(PLAN);
// walk-grid checks dominate (about 5 s per round plan), so each run replays in its own process, in parallel
const jobs = ids.map(id => { const p = Bun.spawn(['bun', 'tools/levels/replay.ts', id, PLAN], { cwd: root, stdout: 'pipe', stderr: 'pipe' }); return Promise.all([new Response(p.stdout).text(), new Response(p.stderr).text(), p.exited]); });
let replayed = 0, accepted = '';
for (const [i, id] of ids.entries()) {
  const run = await Bun.file(resolve(runsRoot(), PLAN, id, 'run.json')).json().catch(() => ({}));
  const [o, e, code] = await jobs[i]!, lines = (o + e).split('\n').filter(Boolean), ok = code === 0, s = summarise(lines);
  const status = ok ? 'REPLAY OK' : s.superseded ? 'REPLAY SUPERSEDED (expected)' : 'REPLAY FAILED';
  console.log(`${id}  ${run.outcome ?? '?'}  ${status}  drift: ${s.drift}`);
  if (!ok && !s.superseded) { bad.push(`${id} replay`); for (const f of s.fails) console.log('      ' + f.slice(0, 200)); } else replayed++;
  if (id === SUBMITTED) accepted = run.outcome;
}
const missing = ['r001', 'r002', 'r003', 'r004', 'r005', 'r006'].filter(r => !ids.includes(r)); // committed runs; a marker's own new runs are replayed too
if (missing.length) bad.push(`committed runs missing: ${missing.join(',')}`);
if (accepted !== 'ACCEPTED') bad.push(`${SUBMITTED} is ${accepted || 'missing'}, not ACCEPTED`);

const counts = (r: { checks: { id: string; ok: boolean; waived?: string }[] }) => ({ pass: r.checks.filter(c => c.ok && !c.waived).length, waived: r.checks.filter(c => c.waived).length, fail: r.checks.filter(c => !c.ok) });
const line = (label: string, r: Awaited<ReturnType<typeof inProcess>>) => {
  const c = counts(r); console.log(`checker ${label}: ${c.pass} pass, ${c.waived} waived, ${c.fail.length} fail${c.fail.length ? ` (${c.fail.map(f => f.id).join(',')})` : ''}`);
  if (c.fail.length) bad.push(`checker ${label}: ${c.fail.map(f => f.id).join(',')}`);
};
const plan = JSON.parse(await Bun.file(resolve(root, `contracts/world/${PLAN}.plan.json`)).text());
line('contracts plan (non-strict)', await inProcess(plan, false));
const rd = resolve(runsRoot(), PLAN, SUBMITTED), rounds = readdirSync(rd).filter(d => /^round-\d+$/.test(d)).sort(), last = rounds.at(-1)!;
line(`${SUBMITTED} ${last} plan (strict, pinned block)`, await inProcess(await Bun.file(`${rd}/${last}/plan.json`).json(), true, await Bun.file(`${rd}/input.block.json`).json()));

const [out, err, code] = await tscOut;
console.log(`typecheck: ${code === 0 ? 'clean' : 'FAILED'}`);
if (code !== 0) { bad.push('typecheck'); console.log((out + err).split('\n').slice(0, 15).join('\n')); }
console.log(bad.length ? `VERIFY FAILED: ${bad.join('; ')}` : `VERIFY OK: ${replayed} runs replayed (submitted ${SUBMITTED} ACCEPTED), checker clean, typecheck clean`);
console.error(`(${((performance.now() - t0) / 1000).toFixed(0)} s)`);
process.exit(bad.length ? 1 : 0);
