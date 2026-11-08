import { existsSync, readdirSync } from 'node:fs';
import { mkdir, writeFile } from 'node:fs/promises';
import { relative, resolve } from 'node:path';
import { root } from '../rag/config';
import { loadBriefs, todaysLines } from './briefs';
import { archiveEvidence, readRun, RUN_FILE, RUNS, verifyRun, writeRun } from './evidence';
import { loadGuide, readerFor } from './guide';
import { liveSetup } from './live';
import { corrected, defaults, runLoop, validateLimits } from './loop';
import { DEMOS, runHtml, submissionHtml, WHO } from './report';
import { applyShort } from './short-apply';
import { loadShortBriefs } from './short-briefs';
import { acceptedByFamily, continuable, judgeHand, listShort, liveShort, replayShort } from './short-cli';
import { verifyShort } from './short-evidence';
import { SHORT_RUN } from './short-types';
import type { Brief, Result } from './types';

const COURSE = 'out/style-agent';
const help = `Style agent: conversations that reveal (brief 054)

Offline (no model, no login):
  bun run style:guide                 the sourced style guide (JSON)
  bun run style:briefs                the Inez storyline: each conversation's act, length and reveals
  bun run style:replay RUN            play a saved run step by step (tone | lore | length | a brief id | a run folder)
  bun run style:verify RUN            rebuild a saved run from its archived sources and check it (a run folder or file)
  bun run style:report                check every saved run and write ${COURSE}/index.html and the proposals
  bun tools/style-agent/cli.ts shorts the short-line families: barks, the why block, the debrief page, the HUD's labels
  bun tools/style-agent/cli.ts apply  write every family's accepted lines into text.json (--dry to only show them; --skip a,b leaves those families out; --restore a,b puts their lines back as their runs found them)

Live (Codex subscription; needs SUBORNED_LIVE=1 and a recorded approval):
  bun run style:demo tone|lore|length LIVE
  bun run style:run BRIEF_ID LIVE
  bun run style:chain LIVE [--concurrency N] [--again]
  bun run style:run FAMILY LIVE       one short-line family (bark/…, why/…, debrief/…, label/…; see shorts)
  bun run style:chain --short LIVE [--kind bark|why|debrief|label] [--only a,b] [--concurrency N] [--again] [--continue]
  --continue: each family picks up its latest run that ran out of revisions on this rubric, from that run's last round (brief 102)
  LIVE = --model MODEL --approved-by NAME --max-calls N --max-tokens N [--max-revisions N] [--judge-model CLAUDE_MODEL]
  --judge-model: Motive's evaluations on Claude through the claude CLI (brief 102: claude-opus-5-5); Fable still writes on --model
  bun tools/style-agent/cli.ts judge FAMILY --hand FILE --approved-by NAME --judge-model CLAUDE_MODEL
                                      Motive scores a family's hand edits once on Claude, no writer call (brief 102, guide /17)

Runs are saved in ${RUNS}/<time>-<brief>/; nothing in the game is changed.`;

function parse(args: string[], known: string[], switches: string[] = []) {
  const options = new Map<string, string>(), positional: string[] = [];
  while (args.length) {
    const a = args.shift()!;
    if (switches.includes(a)) { options.set(a, 'true'); continue; }
    if (!a.startsWith('--')) { positional.push(a); continue; }
    const v = args.shift();
    if (!known.includes(a) || !v || v.startsWith('--') || options.has(a)) throw new Error(`Invalid or repeated ${a}\n\n${help}`);
    options.set(a, v);
  }
  return { options, positional };
}
const LIVE = ['--model', '--approved-by', '--max-calls', '--max-tokens', '--max-revisions', '--judge-model'];

async function live(options: Map<string, string>, jobs: { brief: Brief; steer: string | null; tag: string }[], concurrency = 1) {
  const limits = { ...defaults, maxCalls: Number(options.get('--max-calls')), maxTokens: Number(options.get('--max-tokens')),
    maxRevisions: options.has('--max-revisions') ? Number(options.get('--max-revisions')) : defaults.maxRevisions };
  validateLimits(limits); // before any directory or credential is touched
  const probe = Bun.spawnSync([process.env.CODEX_BIN ?? 'codex', '--version'], { stdout: 'pipe', stderr: 'pipe' });
  if (probe.exitCode !== 0) throw new Error('Codex CLI not found: install it and sign in with ChatGPT (codex login)');
  const { invoke, authorization, roles } = liveSetup(options);
  const guide = loadGuide(root), evidence = archiveEvidence(root, guide, undefined, roles);
  const queue = [...jobs], results: Result[] = [];
  const worker = async () => {
    for (let job = queue.shift(); job; job = queue.shift()) {
      const dir = resolve(root, RUNS, `${new Date().toISOString().replaceAll(':', '-')}-${job.tag}`);
      await mkdir(dir, { recursive: true });
      const checkpoint = async (r: Result) => {
        writeRun(dir, r);
        await writeFile(resolve(dir, 'index.html'), runHtml(r));
      };
      const r = await runLoop({ guide, brief: job.brief, steer: job.steer, invoke, limits, mode: 'live', checkpoint, evidence, authorization });
      results.push(r);
      console.log(JSON.stringify({ brief: r.brief.id, status: r.status, stopReason: r.stopReason, scores: r.rounds.map(x => x.evaluation.score),
        calls: r.calls.length, tokens: r.calls.reduce((n, c) => n + (c.tokens ?? 0), 0), corrected: corrected(r), run: relative(root, dir) }));
    }
  };
  await Promise.all(Array.from({ length: Math.min(concurrency, jobs.length) }, worker));
  process.exitCode = results.every(r => r.status === 'ACCEPTED') ? 0 : results.some(r => r.status === 'ERROR') ? 1 : 2;
}

const runFile = (path: string) => /\.json(\.zst)?$/.test(path) ? resolve(path) : resolve(path, RUN_FILE);

async function savedRuns() {
  const dir = resolve(root, RUNS);
  if (!existsSync(dir)) return [];
  const out: { run: Result; path: string }[] = [];
  for (const name of readdirSync(dir).filter(n => n !== 'sources').sort()) {
    const file = resolve(dir, name, RUN_FILE);
    if (!existsSync(file)) continue;
    const v = await verifyRun(readRun(file), root);
    if (!v.ok) { console.error(`skipped ${name}: ${v.errors.join('; ')}`); continue; }
    out.push({ run: v.result!, path: resolve(dir, name) });
  }
  return out;
}

async function report() {
  const guide = loadGuide(root), briefs = loadBriefs(root), runs = (await savedRuns()).filter(r => r.run.mode === 'live');
  const today = Object.fromEntries(briefs.map(b => [b.id, todaysLines(readerFor(root), b.scene)]));
  const course = resolve(root, COURSE), link = (p: string) => relative(course, p);
  const demos = DEMOS.flatMap(demo => {
    const mine = runs.filter(r => r.run.steer === demo.steer && r.run.brief.id === demo.brief);
    const best = mine.filter(r => corrected(r.run)).at(-1) ?? mine.at(-1);
    return best ? [{ demo, run: best.run, path: link(best.path) }] : [];
  });
  const chain = briefs.map(brief => {
    const mine = runs.filter(r => r.run.steer === null && r.run.brief.id === brief.id);
    const best = mine.filter(r => r.run.status === 'ACCEPTED').at(-1) ?? mine.at(-1);
    return { brief, run: best?.run ?? null, path: best ? link(best.path) : null };
  });
  await mkdir(resolve(course, 'proposals'), { recursive: true });
  await writeFile(resolve(course, 'index.html'), submissionHtml({ guide, demos, chain, today, all: runs.map(r => ({ run: r.run, path: link(r.path) })) }));
  await writeFile(resolve(course, 'proposals/inez-conversations.json'), JSON.stringify({
    schema: 'suborned.conversation-proposals/1', brief: 'briefs/054-conversations-that-reveal.md',
    note: 'Proposals for contracts/missions/handover/text.json scenes.items; the game is unchanged until they are reviewed and the scene limits raised.',
    conversations: chain.filter(c => c.run?.status === 'ACCEPTED').map(({ brief, run, path }) => ({
      id: brief.id, scene: brief.scene, verb: brief.verb, temperament: brief.temperament, outcome: brief.outcome, seconds: brief.seconds,
      score: run!.rounds.at(-1)!.evaluation.score, run: path, lines: run!.rounds.at(-1)!.conversation.lines })),
  }, null, 2) + '\n');
  console.log(JSON.stringify({ report: `${COURSE}/index.html`, demos: demos.map(d => ({ id: d.demo.id, corrected: corrected(d.run) })),
    accepted: chain.filter(c => c.run?.status === 'ACCEPTED').length, of: chain.length }, null, 2));
}

/** A saved run, step by step: what Fable wrote, what Motive scored and why, what changed. Checked first, so it is the real run. */
async function replay(args: string[]) {
  if (args.length !== 1) throw new Error(help);
  // A demo name (tone, lore, length) or a brief id picks its accepted run; otherwise a run folder or its run file.
  const name = args[0]!, demo = DEMOS.find(d => d.id === name), saved = demo || name.startsWith('inez/') ? await savedRuns() : [];
  const pick = saved.filter(x => x.run.mode === 'live' && (demo ? x.run.steer === demo.steer && x.run.brief.id === demo.brief : x.run.steer === null && x.run.brief.id === name));
  const chosen = pick.filter(x => x.run.status === 'ACCEPTED').at(-1) ?? pick.at(-1);
  if ((demo || name.startsWith('inez/')) && !chosen) throw new Error(`No saved run for ${name}`);
  const file = chosen ? resolve(chosen.path, RUN_FILE) : runFile(name);
  const v = await verifyRun(readRun(file), root);
  if (!v.ok) throw new Error(`Not a valid run: ${v.errors.join('; ')}`);
  const r = v.result!, b = r.brief, say = (s = '') => console.log(s);
  const show = (lines: Result['rounds'][number]['conversation']['lines'], before?: string[]) => lines.forEach((l, i) =>
    say(`  ${before && before[i] !== l.line ? '~' : ' '} ${(WHO[l.speaker] ?? l.speaker).padEnd(9)} ${l.line}`));
  say(`${b.id} · ${b.verb}, ${b.seconds} s, ${b.lines[0]}–${b.lines[1]} lines · ${r.mode}${r.authorization ? `, ${r.authorization.model}${r.authorization.judge ? `, judged by ${r.authorization.judge.model}` : ''}` : ''}`);
  say(`Must reveal: ${b.reveals.join('; ')}`);
  say(`Sets up:     ${b.leadsTo}`);
  if (r.steer) say(`Wrong push (first draft only): ${r.steer}`);
  r.rounds.forEach((round, i) => {
    const prev = r.rounds[i - 1]?.conversation.lines.map(l => l.line);
    say(); say(i === 0 ? '▶ Fable writes the first draft' : `▶ Fable revises from Motive's reasons (~ = changed line)`);
    show(round.conversation.lines, prev);
    say(); say(`▶ Motive scores it: SCORE ${round.evaluation.score}/10`);
    say(`  REASON: ${round.evaluation.reason}`);
    for (const f of round.evaluation.findings) say(`  - ${f.rule}${f.quote ? ` "${f.quote}"` : ''}: ${f.reason}\n    fix: ${f.fix}`);
    for (const h of round.hardIssues) say(`  - ${h.rule} (code check): ${h.reason}`);
  });
  say(); say(`■ ${r.status}: ${r.stopReason} · ${r.calls.length} calls · ${r.calls.reduce((n, c) => n + (c.tokens ?? 0), 0)} tokens`);
  say(`  Checked: rebuilt from its archived sources and recorded answers; no model was called.`);
}

async function main() {
  const args = process.argv.slice(2), command = args.shift();
  if (!command || command === '--help' || args.includes('--help')) { console.log(help); return; }
  if (command === 'guide') { console.log(JSON.stringify(loadGuide(root), null, 2)); return; }
  if (command === 'briefs') {
    for (const b of loadBriefs(root)) console.log(`${b.id.padEnd(22)} scene ${String(b.scene).padStart(2)} · ${b.seconds} s · ${b.lines[0]}–${b.lines[1]} lines · ${b.reveals.join(' · ')}`);
    return;
  }
  if (command === 'shorts') return listShort();
  if (command === 'apply') {
    const list = (flag: string) => new Set((args.includes(flag) ? args[args.indexOf(flag) + 1] ?? '' : '').split(',').filter(Boolean));
    return applyShort(args.includes('--dry'), { runs: await acceptedByFamily(list('--skip')), restore: args.includes('--restore') ? list('--restore') : undefined });
  }
  if (command === 'verify') {
    if (args.length !== 1) throw new Error(help);
    const input = readRun(runFile(args[0]!)) as { schema?: string };
    const { result: _, ...v } = input.schema === SHORT_RUN ? await verifyShort(input, root) : await verifyRun(input, root);
    console.log(JSON.stringify(v, null, 2)); process.exitCode = v.ok ? 0 : 1; return;
  }
  if (command === 'report') return report();
  if (command === 'replay') {
    if (args.length === 1 && !/^(inez\/|tone$|lore$|length$)/.test(args[0]!) && (readRun(runFile(args[0]!)) as { schema?: string }).schema === SHORT_RUN) return replayShort(runFile(args[0]!));
    return replay(args);
  }
  const briefs = loadBriefs(root), byId = (id: string) => briefs.find(b => b.id === id) ?? (() => { throw new Error(`No brief ${id}; see bun run style:briefs`); })();
  const tag = (b: Brief, suffix = '') => b.id.replaceAll('/', '-') + suffix;
  if (command === 'demo') {
    const { options, positional } = parse(args, LIVE), demo = DEMOS.find(d => d.id === positional[0]);
    if (!demo || positional.length !== 1) throw new Error(`demo needs one of ${DEMOS.map(d => d.id).join(', ')}`);
    return live(options, [{ brief: byId(demo.brief), steer: demo.steer, tag: tag(byId(demo.brief), `-demo-${demo.id}`) }]);
  }
  if (command === 'judge') {
    const { options, positional } = parse(args, ['--hand', '--approved-by', '--judge-model']), family = loadShortBriefs(root).find(b => b.id === positional[0]);
    if (positional.length !== 1 || !family || !options.has('--hand')) throw new Error(`judge needs one family and --hand FILE; see: bun tools/style-agent/cli.ts shorts`);
    return judgeHand(options, family, options.get('--hand')!);
  }
  if (command === 'run' && /^(bark|why|debrief|label)\//.test(args.find(a => !a.startsWith('--') && /\//.test(a)) ?? '')) {
    const { options, positional } = parse(args, LIVE), family = loadShortBriefs(root).find(b => b.id === positional[0]);
    if (positional.length !== 1 || !family) throw new Error(`No family ${positional[0]}; see: bun tools/style-agent/cli.ts shorts`);
    return liveShort(options, [family]);
  }
  if (command === 'run') {
    const { options, positional } = parse(args, LIVE);
    if (positional.length !== 1) throw new Error(help);
    return live(options, [{ brief: byId(positional[0]!), steer: null, tag: tag(byId(positional[0]!)) }]);
  }
  if (command === 'chain' && args.includes('--short')) {
    const { options } = parse(args.filter(a => a !== '--short'), [...LIVE, '--concurrency', '--kind', '--only'], ['--again', '--continue']);
    const only = options.has('--only') ? new Set(options.get('--only')!.split(',')) : null; // --only a,b: these families, accepted or not
    const done = options.has('--again') || only ? new Set<string>() : new Set((await acceptedByFamily()).keys()), kind = options.get('--kind');
    const concurrency = Number(options.get('--concurrency') ?? 3);
    if (!Number.isSafeInteger(concurrency) || concurrency < 1 || concurrency > 4) throw new Error('--concurrency must be 1..4');
    const families = loadShortBriefs(root).filter(b => !done.has(b.id) && (!kind || b.kind === kind) && (!only || only.has(b.id)));
    if (only && families.length !== only.size) throw new Error(`No such family in --only: ${[...only].filter(id => !families.some(b => b.id === id)).join(', ')}`);
    const continues = options.has('--continue') ? await continuable(families) : undefined;
    if (continues) { const none = families.filter(b => !continues.has(b.id)).map(b => b.id); if (none.length) throw new Error(`--continue: no run to continue for ${none.join(', ')}`); }
    for (const k of ['--concurrency', '--again', '--kind', '--only', '--continue']) options.delete(k);
    return liveShort(options, families, concurrency, continues);
  }
  if (command === 'chain') {
    const { options } = parse(args, [...LIVE, '--concurrency'], ['--again']);
    const done = new Set(options.has('--again') ? [] : (await savedRuns()).filter(r => r.run.mode === 'live' && r.run.steer === null && r.run.status === 'ACCEPTED').map(r => r.run.brief.id));
    const concurrency = Number(options.get('--concurrency') ?? 3);
    if (!Number.isSafeInteger(concurrency) || concurrency < 1 || concurrency > 4) throw new Error('--concurrency must be 1..4');
    options.delete('--concurrency'); options.delete('--again');
    return live(options, briefs.filter(b => !done.has(b.id)).map(b => ({ brief: b, steer: null, tag: tag(b) })), concurrency);
  }
  throw new Error(help);
}
main().catch(error => { console.error(error instanceof Error ? error.message : String(error)); process.exitCode = 1; });
