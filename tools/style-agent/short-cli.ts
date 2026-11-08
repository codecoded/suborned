import { existsSync, readdirSync } from 'node:fs';
import { mkdir, writeFile } from 'node:fs/promises';
import { relative, resolve } from 'node:path';
import { root } from '../rag/config';
import { defaults, validateLimits } from './core';
import { archiveEvidence, readRun, RUN_FILE, writeRun } from './evidence';
import { TEXT_FILE } from './guide';
import { liveCaller } from './live';
import { loadShortBriefs } from './short-briefs';
import { SHORT_RUNS, verifyShort } from './short-evidence';
import { loadShortGuide } from './short-guide';
import { runShort } from './short-loop';
import { shortGuideVersion, type ShortBrief, type ShortResult } from './short-types';
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
<h1>${esc(r.brief.id)}: ${esc(r.brief.title)}</h1><p><b>${esc(r.status)}</b> · ${esc(r.stopReason)} · ${esc(r.mode)}${r.authorization ? ` · ${esc(r.authorization.model)}` : ''} · ${r.calls.length} calls · ${tokens(r)} tokens</p>${rounds}
<p><small>run.json.zst holds every prompt, Codex transcript, score and revision: <code>bun run style:replay</code>, <code>bun run style:verify</code>.</small></p></html>`;
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
/** A /12 or later bark run waits on Sky's yes (brief 099 voices; at /13, brief 102's sample, briefs/102/fable/index.html): until then its family keeps the lines of its last run before /12. */
export const waitsForSky = (r: ShortResult) => r.brief.kind === 'bark' && shortGuideVersion(r.guide) >= 12 && !BARKS_12_APPLY;
/** The run a family's lines come from: its latest accepted live run under the current rubric. `skip` leaves families out (today's lines stay). */
export async function acceptedByFamily(skip = new Set<string>()): Promise<Map<string, { run: ShortResult; path: string }>> {
  const best = new Map<string, { run: ShortResult; path: string }>();
  for (const x of await savedShortRuns()) if (x.run.mode === 'live' && x.run.status === 'ACCEPTED' && shortGuideVersion(x.run.guide) >= CURRENT_RULES && !skip.has(x.run.brief.id) && !waitsForSky(x.run)) best.set(x.run.brief.id, x);
  return best;
}

export async function liveShort(options: Map<string, string>, briefs: ShortBrief[], concurrency = 1) {
  const limits = { ...defaults, maxCalls: Number(options.get('--max-calls')), maxTokens: Number(options.get('--max-tokens')),
    maxRevisions: options.has('--max-revisions') ? Number(options.get('--max-revisions')) : defaults.maxRevisions };
  validateLimits(limits);
  const probe = Bun.spawnSync([process.env.CODEX_BIN ?? 'codex', '--version'], { stdout: 'pipe', stderr: 'pipe' });
  if (probe.exitCode !== 0) throw new Error('Codex CLI not found: install it and sign in with ChatGPT (codex login)');
  const invoke = liveCaller(options.get('--model') ?? '', options.get('--approved-by') ?? '');
  const guide = loadShortGuide(root), evidence = archiveEvidence(root, guide), text = JSON.parse(await Bun.file(resolve(root, TEXT_FILE)).text());
  const authorization = { provider: 'codex' as const, model: options.get('--model')!, approvedBy: options.get('--approved-by')! };
  const queue = [...briefs], results: ShortResult[] = [];
  const worker = async () => {
    for (let brief = queue.shift(); brief; brief = queue.shift()) {
      const dir = resolve(root, SHORT_RUNS, `${new Date().toISOString().replaceAll(':', '-')}-${brief.id.replaceAll('/', '-')}`);
      await mkdir(dir, { recursive: true });
      const checkpoint = async (r: ShortResult) => { writeRun(dir, r); await writeFile(resolve(dir, 'index.html'), shortRunHtml(r)); };
      const r = await runShort({ guide, brief, invoke, limits, mode: 'live', checkpoint, evidence, authorization, base: { why: text.why, dialogue: text.dialogue } });
      results.push(r);
      console.log(JSON.stringify({ family: r.brief.id, status: r.status, stopReason: r.stopReason, scores: r.rounds.map(x => x.evaluation.score),
        findings: r.rounds.map(x => x.evaluation.findings.length + x.hardIssues.length), calls: r.calls.length, tokens: tokens(r), run: relative(root, dir) }));
    }
  };
  await Promise.all(Array.from({ length: Math.min(concurrency, briefs.length) }, worker));
  process.exitCode = results.every(r => r.status === 'ACCEPTED') ? 0 : results.some(r => r.status === 'ERROR') ? 1 : 2;
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
  say(`${r.brief.id} · ${r.brief.items.length} items · ${r.mode}${r.authorization ? `, ${r.authorization.model}` : ''}`);
  r.rounds.forEach((round, i) => {
    say(); say(i === 0 ? '▶ Fable writes the first draft' : '▶ Fable revises from Motive\'s reasons');
    for (const it of round.set.items) say(`  ${it.key.padEnd(40)} ${it.cut ? `CUT: ${it.reason}` : it.fields.map(f => f.lines.join(' | ')).join(' ‖ ')}`);
    say(); say(`▶ Motive scores it: SCORE ${round.evaluation.score}/10`); say(`  REASON: ${round.evaluation.reason}`);
    for (const f of round.evaluation.findings) say(`  - ${f.rule} ${f.key}${f.quote ? ` "${f.quote}"` : ''}: ${f.reason}\n    fix: ${f.fix}`);
    for (const h of round.hardIssues) say(`  - ${h.rule} (code check): ${h.reason}`);
  });
  say(); say(`■ ${r.status}: ${r.stopReason} · ${r.calls.length} calls · ${tokens(r)} tokens`);
  say('  Checked: rebuilt from its archived sources and recorded answers; no model was called.');
}
