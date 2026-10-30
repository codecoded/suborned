// usage: bun run missions:check [check] <mission> [--record "<note>"] [--engine HEAD|tree|<commit>] [--inputs commit|tree]
//        bun run missions:check bands <mission> [--seeds n] [--seed-from k] [--out dir]   brief 080: the adaptive plans across seeds (bands.ts)
//        bun run missions:check replay <mission> <cNNN>
//        bun run missions:check compose <mission> [<script id>…] [--engine …] [--inputs …] [--jobs n]
// compose: rewrites each route script that has a plan (routes/plans/<id>.plan.json) from the plan, by playing the engine (compose.ts).
// check: plays the mission's route scripts (contracts/missions/<mission>/routes) and their variations headless, prints the GDD §09
// conditions, writes out/missions/<mission>/check.{json,html}; --record also writes runs/missions/<mission>/cNNN/. Exit 0 only if
// the mission ships (every condition holds). replay: replays a recorded check; exit 0 only if every play lands on its recorded hash.
import { mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT, engineAt, hasDotnet } from './engine';
import { check, html, load, record, replay, runsDir } from './run';
import { compose, loadPlan, writeScript } from './compose';
import { missionIdProblems } from './ids';
import { html as bandsHtml, runBands, table as bandsTable } from './bands';

const args = process.argv.slice(2), flag = (k: string) => { const i = args.indexOf(k); return i >= 0 ? args.splice(i, 2)[1] : undefined; };
// `--record` takes the note that goes in the run's record (what changed and why): a bare one used to record nothing and say nothing (Keel, 2026-10-24: `bun run missions:rerecord` with no note). It is refused now, before any play.
const ri = args.indexOf('--record'), bare = ri >= 0 && (args[ri + 1] === undefined || args[ri + 1]!.startsWith('--') || !args[ri + 1]!.trim());
if (bare) { console.error('missions:check: --record needs a note: bun run missions:rerecord "what moved and why" (or --record "<note>"); nothing was played or recorded'); process.exit(2); }
const note = flag('--record'), rev = flag('--engine') ?? 'HEAD', inputs = flag('--inputs') === 'tree' ? 'tree' : 'commit', composeJobs = Math.max(1, Number(flag('--jobs') ?? 4));
const seedsFlag = Number(flag('--seeds') ?? 200), seedFrom = Number(flag('--seed-from') ?? 1), bandsOut = flag('--out');
const cmd = ['replay', 'check', 'compose', 'bands'].includes(args[0] ?? '') ? args.shift()! : 'check';
if (!hasDotnet()) { console.error('the mission checker needs the .NET SDK (dotnet on PATH; DOTNET_ROOT=/opt/homebrew/opt/dotnet/libexec on the Studio)'); process.exit(2); }
const log = (m: string) => console.error(`· ${m}`);

if (cmd === 'replay') {
  const [mission, id] = args;
  if (!mission || !id) { console.error('usage: replay <mission> <cNNN>'); process.exit(2); }
  const r = await replay(join(runsDir(mission), id), log);
  for (const l of r.lines) console.log(l);
  console.log(r.ok ? `REPLAY OK ${mission} ${id}` : `REPLAY FAILED ${mission} ${id}`);
  process.exit(r.ok ? 0 : 1);
}

// brief 080: the bands: the adaptive plans across seeds (bands.ts). A rigid line that fails exits 1; a band that misses is a report for Motive, exit 0.
if (cmd === 'bands') {
  const mission = args[0] ?? 'handover', e = engineAt(rev === 'HEAD' && !process.env.BANDS_HEAD ? 'tree' : rev, inputs);
  const r = await runBands(mission, e, { seeds: seedsFlag, from: seedFrom, log });
  const out = bandsOut ?? join(ROOT, 'out/missions', mission);
  mkdirSync(out, { recursive: true });
  writeFileSync(join(out, 'bands.json'), JSON.stringify({ ...r, rows: undefined }, null, 1) + '\n');
  writeFileSync(join(out, 'bands.html'), bandsHtml(r));
  console.log(bandsTable(r) + '\n');
  for (const l of [...r.rigid, ...r.bands]) console.log(`${l.ok ? 'ok  ' : l.id.startsWith('R-') ? 'FAIL' : 'MISS'}  ${l.id}  ${l.rule}: ${l.value}${l.note ? `\n        ${l.note}` : ''}`);
  for (const w of r.windows) console.log(`window ${w.plan} ${w.rule}: ${w.base} % as planned, ${w.late} % 30 s late`);
  const failed = r.rigid.filter(l => !l.ok).length, missed = r.bands.filter(l => !l.ok).length;
  console.log(`${failed ? 'HOLDS' : 'RIGID OK'} ${mission}: ${r.plans.length} plans × ${r.seeds} seeds in ${r.seconds} s; ${failed} rigid failed, ${missed} of ${r.bands.length} bands missed; report ${join(out, 'bands.html').replace(ROOT + '/', '')}`);
  process.exit(failed ? 1 : 0);
}

if (cmd === 'compose') {
  const mission = args.shift() ?? 'handover', dir = join(ROOT, 'contracts/missions', mission, 'routes'), L = load(dir), e = engineAt(rev, inputs);
  const ids = args.length ? args : L.manifest.scripts.filter(s => s.plan).map(s => s.id);
  // plans compose side by side (each play is named by its script id, so they never share a temp file); a plan that fails to compose
  // fails the command, naming its step, once the others are done
  const jobs = composeJobs, bad: string[] = [];
  let next = 0;
  await Promise.all(Array.from({ length: Math.min(jobs, ids.length) }, async () => {
    while (next < ids.length) {
      const id = ids[next++]!, s = L.manifest.scripts.find(x => x.id === id);
      if (!s?.plan) { bad.push(`${id}: no plan in routes.json`); continue; }
      const plan = loadPlan(join(dir, 'plans', s.plan)), t0 = performance.now();
      try {
        const { script } = await compose(plan, e, id, log);
        writeScript(join(dir, s.file), plan.note, script);
        console.log(`composed ${id}: ${script.orders.length} orders, last at tick ${script.orders.at(-1)!.tick} (${((performance.now() - t0) / 1000).toFixed(1)} s) → routes/${s.file}`);
      } catch (err) { bad.push(`${id}: ${(err as Error).message}`); }
    }
  }));
  for (const b of bad) console.error(`NOT COMPOSED ${b}`);
  process.exit(bad.length ? 1 : 0);
}

// brief 078 B1: the mission's words and points must name ids the site has (ids.ts); a remap rename fails here, before any play
const idBad = missionIdProblems(args[0] ?? 'handover');
if (idBad.length) { for (const b of idBad) console.error(`ID ${b}`); console.error(`HOLDS ${args[0] ?? 'handover'}: ${idBad.length} id(s) the site does not have; nothing was played`); process.exit(1); }
const mission = args[0] ?? 'handover', L = load(join(ROOT, 'contracts/missions', mission, 'routes'));
const t0 = performance.now(), c = await check(L, engineAt(rev, inputs), log);
const out = join(ROOT, 'out/missions', mission);
mkdirSync(out, { recursive: true });
writeFileSync(join(out, 'check.json'), JSON.stringify({ ...c, bands: c.bands && { ...c.bands, rows: undefined }, engine: { ...c.engine, dir: undefined } }, null, 1) + '\n');
writeFileSync(join(out, 'check.html'), html(c, 'unrecorded'));
for (const x of c.report.conditions) console.log(`${x.ok ? 'ok  ' : 'FAIL'}  ${x.id}  ${x.rule}: ${x.value}\n        ${x.detail.replace(/; /g, '\n        ')}`);
if (c.bands) console.log('\n' + bandsTable(c.bands) + '\n');
for (const s of c.slack) console.log(`slack ${s.script}: every planned step can slip ${s.maxS} s (${s.steps.map(x => `${Math.floor(x.tick / 600)}:${String(Math.floor(x.tick / 10) % 60).padStart(2, '0')} ${x.slackS} s${x.breaksAs ? ` → ${x.breaksAs}` : ''}`).join(', ')})`);
console.log(`${c.report.ok ? 'SHIPS' : 'HOLDS'} ${mission}: ${Object.keys(c.plays).length} plays on engine ${c.engine.commit.slice(0, 12)} in ${((performance.now() - t0) / 1000).toFixed(1)} s; report out/missions/${mission}/check.html`);
if (note) console.log(`recorded ${record(c, L, note).replace(ROOT + '/', '')}`);
process.exit(c.report.ok ? 0 : 1);
