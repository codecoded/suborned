// usage:
//   bun run levels transit [--plan <plan.json>] [--seed station-on-plot|fare-claim|fare-claim-uncited] [--rounds 3]
//                          [--continue <run>]
//   bun run levels skyline [--plan <plan.json>] [--rounds 3] [--continue <run>] [--preview]
//   a live run also needs: --approved-by "<who approved it>"   [--max-usd 10]
// --continue <run>: start from that REJECTED, ERROR or INCOMPLETE run's last completed round, with its open faults,
//   instead of the base plan (a new run, with its own 3 rounds).
// --preview: no model call. Round 1 with the live retrieval; writes the exact prompts and an estimate under
//   out/preview/ and prints the table.
// --max-usd: stop before a call whose estimate would take the run past this much (outcome INCOMPLETE; default 10).
// --approved-by: recorded in run.json; required when the run reaches the real model (CLAUDE_BIN is no stand-in).
// Needs `bun run up` + `bun run ingest` and a signed-in Claude Code. The final plan is never written to contracts/.
// A live call from an agent session is refused (liveGuard in tools/rag/claude.ts); an unknown option is an error.
import { RunRefused } from '../gen/continue';
import { DEFAULT_MAX_USD } from '../gen/ceiling';
import { generate } from '../gen/loop';
import { runPreview } from '../gen/preview-run';
import { isLive, liveGuard } from '../rag/claude';
import { retrieve } from '../rag/retrieval';
import { loopFrom, maxUsdFrom, parseArgs } from './cli-args';
import { plugins } from './plugins';

function refuse(m: string): never { console.error(m); return process.exit(2); }
const parsed = parseArgs(process.argv.slice(2));
if ('error' in parsed) refuse(parsed.error);
const args = parsed as Exclude<typeof parsed, { error: string }>;
const opt = (k: string) => args.values.get(k);
const plugin = Object.hasOwn(plugins, args.objective ?? '') ? plugins[args.objective!] : undefined;
if (!plugin) {
  for (const p of Object.values(plugins)) {
    const seeds = p.seeds ? ` [--seed ${p.seeds.names.join('|')}]` : '';
    const more = '[--rounds 3] [--continue <run>] [--preview]';
    console.error(`usage: bun run levels ${p.id} [--plan <plan.json>]${seeds} ${more}`);
  }
  process.exit(2);
}
const seed = opt('--seed'), continued = opt('--continue'), preview = args.flags.has('--preview');
if (seed && !plugin.seeds?.names.includes(seed)) {
  refuse(`unknown seed ${seed}; known: ${plugin.seeds?.names.join(', ')}`);
}
if (preview && (seed || continued || args.values.has('--rounds'))) {
  refuse('--preview shows round 1 of a fresh run; it cannot be combined with --seed, --continue or --rounds');
}
const offline = process.env.LEVELS_TEST_RETRIEVER === 'lexical', live = isLive();
if (offline && live) {
  refuse('LEVELS_TEST_RETRIEVER=lexical is for stand-in runs only; this run would reach the real model');
}
const retriever = offline ? (await import('./test/lexical')).lexical : retrieve;
const requested = Number(opt('--rounds') ?? 3);
const rounds = Number.isFinite(requested) ? Math.min(3, Math.max(1, Math.trunc(requested))) : 3;
// tests only: run under an older loop rule (tools/gen/rules.ts)
const loop = loopFrom(process.env.LEVELS_LOOP), maxUsd = maxUsdFrom(opt('--max-usd'), DEFAULT_MAX_USD);
if (typeof loop === 'object') refuse(loop.error);
if (typeof maxUsd === 'object') refuse(maxUsd.error);
const input = opt('--plan') ?? plugin.defaultInput, retrieval = offline ? 'lexical (test only)' : undefined;

if (preview) {
  try {
    console.log(await runPreview(plugin, { input, retriever, retrieval }));
    process.exit(0);
  } catch (e) {
    if (!(e instanceof RunRefused)) throw e;
    refuse(`refused: ${e.message}`);
  }
}
const refused = liveGuard(); // before a run folder exists: a refused run leaves nothing in runs/
if (refused) refuse(refused);
const approvedBy = opt('--approved-by')?.trim();
if (live && !approvedBy) refuse('a live run needs --approved-by "<who approved it>"; it is recorded in run.json');
try {
  const o = { input, seed, rounds, retriever, retrieval, continue: continued, loop, maxUsd, approvedBy, live };
  const r = await generate(plugin, o);
  process.exit(r.outcome === 'ACCEPTED' ? 0 : 1);
} catch (e) {
  if (!(e instanceof RunRefused)) throw e;
  refuse(`refused: ${e.message}`);
}
