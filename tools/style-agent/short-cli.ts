import { existsSync, readdirSync } from 'node:fs';
import { mkdir, writeFile } from 'node:fs/promises';
import { basename, relative, resolve } from 'node:path';
import { z } from 'zod';
import { root } from '../rag/config';
import { defaults, validateLimits } from './core';
import { archiveEvidence, readRun, roleFiles, RUN_FILE, writeRun } from './evidence';
import { TEXT_FILE } from './guide';
import { liveSetup } from './live';
import { claudeJudge } from './live-claude';
import { loadShortBriefs } from './short-briefs';
import { SHORT_RUNS, verifyShort } from './short-evidence';
import { loadShortGuide } from './short-guide';
import { runShort } from './short-loop';
import { sameRubric, shortGuideVersion, type ShortBrief, type ShortContinue, type ShortResult, type ShortSet } from './short-types';
import type { Authorization } from './types';
import { equal } from './evidence';
import { BARKS_12_APPLY } from './short-voice';

const esc = (v: unknown) => String(v).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;');
const tokens = (r: ShortResult) => r.calls.reduce((n, c) => n + (c.tokens ?? 0), 0);
/** One run's page: each round's lines beside today's, Motive's score, reason and findings, the code checks. */
export function shortRunHtml(r: ShortResult): string {
  const rounds = r.rounds.map((x, i) => `<h2>${i ? `Revision ${i}` : 'First draft'} · SCORE ${x.evaluation.score}/10</h2><p><b>REASON:</b> ${esc(x.evaluation.reason)}</p>
    <ul>${x.evaluation.findings.map(f => `<li><b>${esc(f.rule)}</b> ${esc(f.key)}${f.quote ? ` “${esc(f.quote)}”` : ''}: ${esc(f.reason)}<br><small>Fix: ${esc(f.fix)}</small></li>`).join('')}${x.hardIssues.map(h => `<li><b>${esc(h.rule)}</b> (code check): ${esc(h.reason)}</li>`).join('')}</ul>
    <table><tr><th>key</th><th>today</th><th>this round</th></tr>${x.set.items.map(it => { const b = r.brief.items.find(y => y.key === it.key);
      return `<tr><td><code>${esc(it.key)}</code></td><td>${Object.values(b?.current ?? {}).flat().map(esc).join('<br>')}</td><td>${it.cut ? `<i>cut: ${esc(it.reason)}</i>` : it.fields.map(f => f.lines.map(esc).join('<br>')).join('<br>')}</td></tr>`; }).join('')}</table>`).join('');
  return `<!doctype html><html lang="en"><meta charset="utf-8"><title>${esc(r.brief.id)}</title><style>body{font:15px/1.5 system-ui,sans-serif;max-width:1180px;margin:0 auto;padding:32px;color:#17302f;background:#f7f4ec}
table{border-collapse:collapse;width:100%}td,th{border-bottom:1px solid #cbc6c3;padding:6px;text-align:left;vertical-align:top}th{background:#d9ebe5}code{font-size:12px}</style>
<h1>${esc(r.brief.id)}: ${esc(r.brief.title)}</h1><p><b>${esc(r.status)}</b> · ${esc(r.stopReason)} · ${esc(r.mode)}${r.authorization ? ` · ${esc(r.authorization.model)}${r.authorization.judge ? `, judged by ${esc(r.authorization.judge.model)}` : ''}` : ''} · ${r.calls.length} calls · ${tokens(r)} tokens</p>${rounds}
<p><small>run.json.zst holds every prompt, transcript (Codex, and Claude for a Claude judge), score and revision: <code>bun run style:replay</code>, <code>bun run style:verify</code>.</small></p></html>`;
}

/** Every saved short run that rebuilds from its archive; a run that doesn't is skipped and said. */
export async function savedShortRuns(): Promise<{ run: ShortResult; path: string }[]> {
  const dir = resolve(root, SHORT_RUNS), out: { run: ShortResult; path: string }[] = [];
  if (!existsSync(dir)) return out;
  for (const name of readdirSync(dir).sort()) {
    const file = resolve(dir, name, RUN_FILE);
    if (!existsSync(file)) continue;
    const v = await verifyShort(readRun(file), root);
    if (!v.ok) { console.error(`skipped ${name}: ${v.errors.join('; ')}`); continue; }
    out.push({ run: v.result!, path: resolve(dir, name) });
  }
  return out;
}
/** Guide /4's rubric is the one the lines are written to (/5 adds a check); a run under /1–/3 stays on record, superseded. */
const CURRENT_RULES = 4;
/** A /12 bark run never applies: Sky's answer to its sample was to quiet the agents (brief 102: "Maybe even drop them?"). From /13 bark runs apply: Sky said yes to the /13
 *  sample on 2026-11-08 ("yes - rewrites are much better", briefs/102/fable/index.html), and the sweep runs to that bar at /14. */
export const waitsForSky = (r: ShortResult) => r.brief.kind === 'bark' && shortGuideVersion(r.guide) === 12 && !BARKS_12_APPLY;
/** The run a family's lines come from: its latest accepted live run under the current rubric. `skip` leaves families out (today's lines stay). */
export async function acceptedByFamily(skip = new Set<string>()): Promise<Map<string, { run: ShortResult; path: string }>> {
  const best = new Map<string, { run: ShortResult; path: string }>();
  for (const x of await savedShortRuns()) if (x.run.mode === 'live' && x.run.status === 'ACCEPTED' && shortGuideVersion(x.run.guide) >= CURRENT_RULES && !skip.has(x.run.brief.id) && !waitsForSky(x.run)) best.set(x.run.brief.id, x);
  return best;
}

/** For `chain --short --continue` (brief 102): each family's latest live run that ran out of revisions on this very rubric and family, to pick up from its last round. */
export async function continuable(briefs: ShortBrief[]): Promise<Map<string, ShortContinue>> {
  const guide = loadShortGuide(root), out = new Map<string, ShortContinue>();
  for (const { run, path } of await savedShortRuns()) {
    const b = briefs.find(x => x.id === run.brief.id);
    if (!b || run.hand || run.mode !== 'live' || run.status !== 'INCOMPLETE' || !run.rounds.length || !run.stopReason.startsWith('Revision limit') || !(equal(run.guide, guide) || sameRubric(run.guide, guide)) || !equal(run.brief, b)) continue;
    out.set(b.id, { run: basename(path), ...run.rounds.at(-1)! }); // the latest wins (the runs are in time order)
  }
  return out;
}

export async function liveShort(options: Map<string, string>, briefs: ShortBrief[], concurrency = 1, continues = new Map<string, ShortContinue>()) {
  const limits = { ...defaults, maxCalls: Number(options.get('--max-calls')), maxTokens: Number(options.get('--max-tokens')),
    maxRevisions: options.has('--max-revisions') ? Number(options.get('--max-revisions')) : defaults.maxRevisions };
  validateLimits(limits);
  const probe = Bun.spawnSync([process.env.CODEX_BIN ?? 'codex', '--version'], { stdout: 'pipe', stderr: 'pipe' });
  if (probe.exitCode !== 0) throw new Error('Codex CLI not found: install it and sign in with ChatGPT (codex login)');
  const { invoke, authorization, roles } = liveSetup(options);
  const guide = loadShortGuide(root), evidence = archiveEvidence(root, guide, undefined, roles), text = JSON.parse(await Bun.file(resolve(root, TEXT_FILE)).text());
  const queue = [...briefs], results: ShortResult[] = [];
  const worker = async () => {
    for (let brief = queue.shift(); brief; brief = queue.shift()) {
      const dir = resolve(root, SHORT_RUNS, `${new Date().toISOString().replaceAll(':', '-')}-${brief.id.replaceAll('/', '-')}`);
      await mkdir(dir, { recursive: true });
      const checkpoint = async (r: ShortResult) => { writeRun(dir, r); await writeFile(resolve(dir, 'index.html'), shortRunHtml(r)); };
      const r = await runShort({ guide, brief, invoke, limits, mode: 'live', checkpoint, evidence, authorization, base: { why: text.why, dialogue: text.dialogue }, continues: continues.get(brief.id) });
      results.push(r);
      console.log(JSON.stringify({ family: r.brief.id, ...(r.continues ? { continues: r.continues.run, from: r.continues.evaluation.score } : {}), status: r.status, stopReason: r.stopReason, scores: r.rounds.map(x => x.evaluation.score),
        findings: r.rounds.map(x => x.evaluation.findings.length + x.hardIssues.length), calls: r.calls.length, tokens: tokens(r), run: relative(root, dir) }));
    }
  };
  await Promise.all(Array.from({ length: Math.min(concurrency, briefs.length) }, worker));
  process.exitCode = results.every(r => r.status === 'ACCEPTED') ? 0 : results.some(r => r.status === 'ERROR') ? 1 : 2;
}

/** Brief 102 (guide /17): a family's failing lines rewritten by hand, each edit one of today's lines (`before`) and its new words (`after`), with why. */
export const HandEdits = z.object({ schema: z.literal('suborned.hand-barks/1'), status: z.string().min(1), by: z.string().min(1), why: z.string().min(1),
  edits: z.array(z.object({ family: z.string().regex(/^bark\/[a-z0-9-]+$/), key: z.string().min(1), before: z.string().min(1), after: z.string().min(1), kind: z.string().min(1), why: z.string().min(1),
    /** A line the judge read and found fault with, and what it said, when `after` was rewritten from that finding. */
    judged: z.object({ line: z.string().min(1), rule: z.string().min(1), finding: z.string().min(1) }).strict().optional() }).strict()).min(1) }).strict();
export type HandEdits = z.infer<typeof HandEdits>;
/** The family as it stands today with the hand edits in: every item kept, each of today's lines replaced where an edit names it. */
export function handSet(brief: ShortBrief, edits: HandEdits['edits']): ShortSet {
  const mine = edits.filter(e => e.family === brief.id);
  if (!mine.length) throw new Error(`${brief.id}: no hand edits`);
  for (const e of mine) if (!brief.items.some(i => i.key === e.key && Object.values(i.current).flat().includes(e.before))) throw new Error(`${e.key}: "${e.before}" is not one of today's lines in ${brief.id}`);
  return { items: brief.items.map(i => ({ key: i.key, cut: false, reason: '', fields: i.fields.map(f => ({ id: f.id, lines: (i.current[f.id] ?? []).map(l => mine.find(e => e.key === i.key && e.before === l)?.after ?? l) })) })) };
}
/** `judge FAMILY --hand FILE`: Motive scores the hand-written family once on Claude (no writer call, no revision), recorded and replayable like any run. */
export async function judgeHand(options: Map<string, string>, brief: ShortBrief, file: string) {
  const approvedBy = options.get('--approved-by') ?? '', model = options.get('--judge-model') ?? '';
  const probe = Bun.spawnSync([process.env.CLAUDE_BIN ?? 'claude', '--version'], { stdout: 'pipe', stderr: 'pipe' });
  if (probe.exitCode !== 0) throw new Error('Claude CLI not found: install Claude Code and sign in (claude)');
  const invoke = claudeJudge(model, approvedBy), authorization: Authorization = { provider: 'hand', model: 'fable', approvedBy, judge: { provider: 'claude', model } };
  const f = HandEdits.parse(JSON.parse(await Bun.file(resolve(root, file)).text())), hand = { by: f.by, why: f.why, set: handSet(brief, f.edits) };
  const guide = loadShortGuide(root), evidence = archiveEvidence(root, guide, undefined, roleFiles(authorization)), text = JSON.parse(await Bun.file(resolve(root, TEXT_FILE)).text());
  const dir = resolve(root, SHORT_RUNS, `${new Date().toISOString().replaceAll(':', '-')}-${brief.id.replaceAll('/', '-')}`);
  await mkdir(dir, { recursive: true });
  const checkpoint = async (r: ShortResult) => { writeRun(dir, r); await writeFile(resolve(dir, 'index.html'), shortRunHtml(r)); };
  const r = await runShort({ guide, brief, invoke, limits: { ...defaults, maxCalls: 1, maxRevisions: 0 }, mode: 'live', checkpoint, evidence, authorization, base: { why: text.why, dialogue: text.dialogue }, hand });
  const last = r.rounds.at(-1);
  console.log(JSON.stringify({ family: brief.id, hand: true, status: r.status, stopReason: r.stopReason, score: last?.evaluation.score, scores: last && 'scores' in last.evaluation ? last.evaluation.scores : undefined,
    findings: last?.evaluation.findings.length, codeChecks: last?.hardIssues.length, tokens: tokens(r), run: relative(root, dir) }));
  process.exitCode = r.status === 'ACCEPTED' ? 0 : r.status === 'ERROR' ? 1 : 2;
}

export function listShort() {
  for (const b of loadShortBriefs(root))
    console.log(`${b.id.padEnd(34)} ${String(b.items.length).padStart(3)} items · ${String(b.items.reduce((n, i) => n + i.fields.reduce((m, f) => m + f.count, 0), 0)).padStart(3)} lines · ${b.title}`);
}

/** A saved short run, step by step: what Fable wrote, what Motive scored and why. Checked first, so it is the real run. */
export async function replayShort(file: string) {
  const v = await verifyShort(readRun(file), root);
  if (!v.ok) throw new Error(`Not a valid run: ${v.errors.join('; ')}`);
  const r = v.result!, say = (s = '') => console.log(s);
  say(`${r.brief.id} · ${r.brief.items.length} items · ${r.mode}${r.authorization ? `, ${r.authorization.model}${r.authorization.judge ? `, judged by ${r.authorization.judge.model}` : ''}` : ''}`);
  if (r.continues) say(`Continues ${r.continues.run}, from its last round (SCORE ${r.continues.evaluation.score}/10)`);
  r.rounds.forEach((round, i) => {
    say(); say(i === 0 && !r.continues ? '▶ Fable writes the first draft' : '▶ Fable revises from Motive\'s reasons');
    for (const it of round.set.items) say(`  ${it.key.padEnd(40)} ${it.cut ? `CUT: ${it.reason}` : it.fields.map(f => f.lines.join(' | ')).join(' ‖ ')}`);
    say(); say(`▶ Motive scores it: SCORE ${round.evaluation.score}/10`); say(`  REASON: ${round.evaluation.reason}`);
    for (const f of round.evaluation.findings) say(`  - ${f.rule} ${f.key}${f.quote ? ` "${f.quote}"` : ''}: ${f.reason}\n    fix: ${f.fix}`);
    for (const h of round.hardIssues) say(`  - ${h.rule} (code check): ${h.reason}`);
  });
  say(); say(`■ ${r.status}: ${r.stopReason} · ${r.calls.length} calls · ${tokens(r)} tokens`);
  say('  Checked: rebuilt from its archived sources and recorded answers; no model was called.');
}
