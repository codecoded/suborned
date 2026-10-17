// usage: bun run verify. Offline (no Docker, no model): typecheck, replay of every recorded run, plan checker, the promoted cast and any promoted dialogue tables held to their runs, the HUD budget of every committed 1x play shot. Exit 0 only if all hold.
import { existsSync, readdirSync, statSync } from 'node:fs';
import { resolve } from 'node:path';
import { inProcess } from './levels/checker';
import { runDirs, runsRoot } from './gen/paths';
import { checkPromotion as checkVoices } from './dialogue/promoted';
import { checkPromotion } from './people/promoted';
import { root } from './rag/config';

const PLAN = 'harbour-district', SUBMITTED = 'r008', bad: string[] = [];
const tsc = Bun.spawn(['bun', 'run', 'typecheck'], { cwd: root, stdout: 'pipe', stderr: 'pipe' }); // runs while the replays use this thread
const tscOut = Promise.all([new Response(tsc.stdout).text(), new Response(tsc.stderr).text(), tsc.exited]);

// HUD budget (brief 014, tools/hud-budget): every committed 1x play shot (briefs/014/play and its folders, each shot with its .hud.png mask and .manifest.json) must be within budget.
// Left out: polish/ (2x crops and history, reading surfaces) and before/ (an old capture with no mask).
const PLAY = resolve(root, 'briefs/014/play'), PLAY_SKIP = new Set(['polish', 'before']);
const playDirs = (d: string): string[] => [d, ...readdirSync(d).filter(n => !PLAY_SKIP.has(n) && statSync(resolve(d, n)).isDirectory()).flatMap(n => playDirs(resolve(d, n)))];
const budgetJobs = existsSync(PLAY) ? playDirs(PLAY).filter(d => readdirSync(d).some(f => f.endsWith('.png'))).map(d => { const p = Bun.spawn(['bun', 'tools/hud-budget/cli.ts', d], { cwd: root, stdout: 'pipe', stderr: 'pipe' }); return Promise.all([Promise.resolve(d), new Response(p.stdout).text(), new Response(p.stderr).text(), p.exited]); }) : [];

/** Pure: label a replay's drift from its warn lines, and say whether its only failure is the expected "superseded". */
function summarise(lines: string[]) {
  let sup = false, moved = 0;
  const gdd = new Map<string, number>(), added = new Set<string>(), other: string[] = [], chunk = new Map<string, number>();
  for (const l of lines.filter(l => l.startsWith('warn'))) {
    const g = l.match(/recorded against GDD (v[\d.]+)/), r = l.match(/rules added since the run: ([\w\-, ]+?)(?:;|\)|$)/);
    if (g) gdd.set(g[1]!, (gdd.get(g[1]!) ?? 0) + 1);
    if (r) for (const id of r[1]!.split(',')) added.add(id.trim());
    const c = l.match(/recorded with chunker (v\d+)/); // a line can carry both causes (GDD version and chunker); each is counted
    if (c) chunk.set(c[1]!, (chunk.get(c[1]!) ?? 0) + 1);
    if (/superseded run, pre-pinning/.test(l)) sup = true;
    else if (/labelled drift: .*code changed since the run/.test(l)) moved++; // the world generator moved (skyline runs)
    else if (!g && !r && !c) other.push(l);
  }
  const fails = lines.filter(l => l.startsWith('FAIL')), superseded = fails.length > 0 && fails.every(l => /^FAIL\s+superseded:/.test(l));
  const parts = [...gdd].map(([v, n]) => `recorded against GDD ${v} ×${n}`);
  for (const [v, n] of chunk) parts.push(`recorded with chunker ${v} ×${n}`);
  if (sup) parts.push('superseded: pre-pinning, Oct 5 block');
  if (moved) parts.push(`generator code changed ×${moved}`);
  if (added.size) parts.push(`rules added ${[...added].sort().join(',')}`);
  if (other.length) parts.push(`other warnings ×${other.length}`);
  return { fails, superseded, drift: parts.length ? parts.join('; ') : 'none' };
}

const t0 = performance.now(), ids = runDirs(PLAN);
// walk-grid checks dominate (about 5 s per round plan), so each run replays in its own process, in parallel
const jobs = ids.map(id => { const p = Bun.spawn(['bun', 'tools/levels/replay.ts', id, PLAN], { cwd: root, stdout: 'pipe', stderr: 'pipe' }); return Promise.all([new Response(p.stdout).text(), new Response(p.stderr).text(), p.exited]); });
let replayed = 0, superseded = 0, accepted = '';
const unfinished: string[] = []; // ERROR, INCOMPLETE, or no run.json: listed, never counted as replayed, never a failure
for (const [i, id] of ids.entries()) {
  const run = await Bun.file(resolve(runsRoot(), PLAN, id, 'run.json')).json().catch(() => ({}));
  const [o, e, code] = await jobs[i]!, lines = (o + e).split('\n').filter(Boolean), ok = code === 0, s = summarise(lines);
  if (code === 3) { // tools/levels/replay.ts: the run never finished
    const why = lines.find(l => l.startsWith('NOT COMPLETED'))?.replace(/^NOT COMPLETED [^:]+: /, '') ?? 'not completed';
    console.log(`${id}  ${run.outcome ?? 'no run.json'}  NOT COMPLETED (not replayed)  ${why.slice(0, 120)}`);
    unfinished.push(id);
    if (id === SUBMITTED) accepted = run.outcome;
    continue;
  }
  const status = ok ? 'REPLAY OK' : s.superseded ? 'REPLAY SUPERSEDED (expected)' : 'REPLAY FAILED';
  console.log(`${id}  ${run.outcome ?? '?'}  ${status}  drift: ${s.drift}`);
  if (!ok && !s.superseded) {
    bad.push(`${id} replay`);
    for (const f of s.fails) console.log('      ' + f.slice(0, 200));
  } else if (ok) replayed++; else superseded++;
  if (id === SUBMITTED) accepted = run.outcome;
}
const missing = ['r001', 'r002', 'r003', 'r004', 'r005', 'r006', 'r007', 'r008', 'r009', 'r010'].filter(r => !ids.includes(r)); // committed runs; a marker's own new runs are replayed too
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

const promo = checkPromotion(PLAN); // the promoted cast is its accepted run's output byte for byte (contracts/people/<plan>.cast.promotion.json)
console.log(`promoted cast: ${promo.ok ? 'matches its run' : 'DRIFTED'}  ${promo.label}`);
if (!promo.ok) { bad.push('promoted cast'); for (const p of promo.problems) console.log('      ' + p.slice(0, 200)); }

// promoted dialogue tables (contracts/dialogue/<plan>.<scope>.voices.json, brief 012) are held to their runs once they exist; a record with no tables, or tables with no record, fails too
const dlgDir = resolve(root, 'contracts/dialogue'), dlgScopes = !existsSync(dlgDir) ? [] : [...new Set(readdirSync(dlgDir).flatMap(f => { const m = f.match(new RegExp(`^${PLAN}\\.([a-z0-9-]+)\\.voices(?:\\.promotion)?\\.json$`)); return m ? [m[1]!] : []; }))].sort();
if (!dlgScopes.length) console.log('promoted dialogue tables: none yet');
for (const scope of dlgScopes) {
  const v = checkVoices(PLAN, scope);
  console.log(`promoted dialogue tables (${scope}): ${v.ok ? 'match their run' : 'DRIFTED'}  ${v.label}`);
  if (!v.ok) { bad.push(`promoted dialogue tables ${scope}`); for (const p of v.problems) console.log('      ' + p.slice(0, 200)); }
}

// HUD budget: one PASS/FAIL line per shot; a folder that fails lists its failing shots with the reasons
let shotsChecked = 0, shotsOver = 0;
for (const [d, o, e, c] of await Promise.all(budgetJobs)) {
  const lines = o.split('\n');
  shotsChecked += lines.filter(l => /^(PASS|FAIL)\s/.test(l)).length; shotsOver += lines.filter(l => l.startsWith('FAIL')).length;
  if (c !== 0) {
    bad.push(`HUD budget ${d.slice(root.length + 1)}`);
    for (const l of (o + e).split('\n').filter(l => /^(FAIL|\s+[-~] )|rror/.test(l))) console.log('      ' + l.slice(0, 220));
  }
}
console.log(`HUD budget: ${shotsChecked} 1x play shots, ${shotsOver ? `${shotsOver} OVER BUDGET` : 'all within budget'}`);
if (!shotsChecked) bad.push('HUD budget: no play shots found');

const [out, err, code] = await tscOut;
console.log(`typecheck: ${code === 0 ? 'clean' : 'FAILED'}`);
if (code !== 0) { bad.push('typecheck'); console.log((out + err).split('\n').slice(0, 15).join('\n')); }
const unfinishedNote = unfinished.length ? `, ${unfinished.length} not completed (${unfinished.join(',')}; listed above)` : '';
const superseding = superseded ? `, ${superseded} superseded (expected)` : '';
const okLine = `VERIFY OK: ${replayed} runs replayed${superseding}${unfinishedNote} (submitted ${SUBMITTED} ACCEPTED), checker clean, ${shotsChecked} play shots within HUD budget, typecheck clean`;
console.log(bad.length ? `VERIFY FAILED: ${bad.join('; ')}` : okLine);
console.error(`(${((performance.now() - t0) / 1000).toFixed(0)} s)`);
process.exit(bad.length ? 1 : 0);
