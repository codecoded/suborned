// `bun run qa` (brief 060, Splice): the one entry point of a QA pass, for Sky's hand and for the nightly schedule (02:00; Latch's -bot row joins the lanes later).
//   1. waits while Sky is playing (the same guard as `bun run guard`: `--wait-timeout <s>`, default 1200; `--force` only when Sky has said so); gives up with exit 3 when he is still in the game
//   2. runs the pass (tools/qa/run.ts runPass): the Handover's routes and Sky's saved sessions and their mutants on each lane's engine, every play through the invariant checks, then the static plan, world and
//      geometry checks (KR-3 among them: statics.ts); writes out/qa/<run>/report.html (the change since the last pass, issues ranked for Sky, every table) and briefs/060/qa/<run>.summary.json
//   3. rotates out/qa: the last 5 passes (and the last 2 `--dev` ones) stay, and any run folder holding a KEEP file; `bun run storage` lists out/qa by run
// Exit codes: 0 whenever the pass ran, WITH FINDINGS (a finding is the product, not a failure); 1 the pass itself broke (an exception: no lane would start, the host crashed); 2 a bad flag; 3 Sky was playing for the whole wait.
// usage: bun run qa [--wait-timeout s] [--force] [--dev] [--no-statics] [--keep n] [--no-rotate] [pass flags of tools/qa/run.ts: --lanes build,tree --route-mutants n --session-mutants n --sessions dir --max-sessions n --jobs n --no-replay --run id --no-summary]
//        bun run qa --rotate [--dry] [--keep n]      only rotate out/qa
import { ROOT } from '../missions/check/engine';
import { takeGuardFlags, timeoutText, waitForSky } from '../game/playing';
import { rotate } from './rotate';
import { DEFAULTS, runPass, type PassOpts } from './run';
import { SESSIONS_DIR } from './seeds';

const args = process.argv.slice(2);
try { takeGuardFlags(args); } catch (e) { console.error((e as Error).message); process.exit(2); }
const flag = (k: string) => { const i = args.indexOf(k); if (i < 0) return false; args.splice(i, 1); return true; };
const val = (k: string): string | undefined => { const i = args.indexOf(k); if (i < 0) return undefined; const v = args[i + 1]; args.splice(i, 2); return v; };
const keepN = val('--keep') ? Number(val('--keep')) : undefined;
const runId = () => new Date().toISOString().replace(/[-:]/g, '').replace(/\..*/, '').replace('T', '-');
const show = (r: ReturnType<typeof rotate>, dry: boolean) => console.log(`out/qa rotation${dry ? ' (dry run)' : ''}: kept ${r.kept.length}${r.removed.length ? `, ${dry ? 'would remove' : 'removed'} ${r.removed.length} (${(r.bytes / 1048576).toFixed(1)} MB: ${r.removed.join(', ')})` : ''}${r.held.length ? `, held by KEEP ${r.held.join(', ')}` : ''}`);

if (flag('--rotate')) { const dry = flag('--dry'); show(rotate(ROOT, { keep: keepN, dry }), dry); process.exit(0); }
const dev = flag('--dev'), noStatics = flag('--no-statics'), noRotate = flag('--no-rotate'), noReplay = flag('--no-replay'), noSummary = flag('--no-summary');
const o: Partial<PassOpts> = {
  lanes: (val('--lanes') ?? DEFAULTS.lanes.join(',')).split(',').filter(Boolean),
  routeMutants: Number(val('--route-mutants') ?? DEFAULTS.routeMutants), sessionMutants: Number(val('--session-mutants') ?? DEFAULTS.sessionMutants), sessionsDir: val('--sessions') ?? SESSIONS_DIR,
  maxSessions: val('--max-sessions') ? Number(val('--max-sessions')) : undefined, jobs: dev ? 2 : val('--jobs') ? Number(val('--jobs')) : undefined,
  replay: !noReplay, run: val('--run') ?? (dev ? 'dev-' + runId() : ''), writeSummary: !dev && !noSummary, maxPlays: dev ? 300 : undefined, statics: !noStatics,
};
if (args.length) { console.error(`qa: unknown argument ${args.join(' ')}`); process.exit(2); }
if (!dev) { const w = await waitForSky('qa'); if (w.timedOut) { console.error(timeoutText(w)); process.exit(3); } }
const t0 = performance.now();
try {
  const { pass, dir, summaryFile } = await runPass(o);
  const sky = pass.issues.filter(i => i.severity !== 'info').slice(0, 10);
  console.log(`qa ${pass.run}: ${pass.lanes.map(l => `${l.lane.name} ${l.plays} plays (${l.errors} errors)`).join(', ')}${pass.statics ? `; ${pass.statics.checks} static rules, ${pass.statics.failed.length} failing` : ''} in ${((performance.now() - t0) / 1000).toFixed(1)} s`);
  for (const [k, i] of sky.entries()) console.log(`  ${k + 1}. ${i.severity} ${i.check} ${i.title} [${Object.entries(i.plays).map(([l, n]) => `${l} ${n}`).join(', ')}] → ${i.owner}${i.change ? ` (${i.change})` : ''}`);
  if (pass.statics?.failed.length) console.log(`static: ${pass.statics.failed.map(f => f.id).join(', ')}${pass.staticsNew?.length ? ` (new: ${pass.staticsNew.join(', ')})` : ''}`);
  if (pass.fixed.length || pass.staticsFixed?.length) console.log(`fixed since ${pass.previous ?? 'the last pass'}: ${[...pass.fixed, ...(pass.staticsFixed ?? [])].join('; ')}`);
  console.log(`report ${dir}/report.html${summaryFile ? `\nsummary ${summaryFile}` : ''}`);
  if (!noRotate) show(rotate(ROOT, { keep: keepN }), false);
} catch (e) { console.error(`qa: the pass broke: ${(e as Error).stack ?? e}`); process.exit(1); }
process.exit(0);
