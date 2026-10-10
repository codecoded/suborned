// Renders report.html from a committed run (runs/levels/<plan>/<run>/) plus evidence/ranks.json.
// usage (repo root): bun courses/a04-content-pipeline/build-report.ts [r001]
import { readdirSync } from 'node:fs';
import { resolve } from 'node:path';

const root = resolve(import.meta.dir, '../..'), here = import.meta.dir;
const runId = process.argv[2] ?? 'r001', plan = 'harbour-district', dir = resolve(root, `runs/levels/${plan}/${runId}`);
const json = (p: string) => Bun.file(p).json();
const esc = (s: unknown) => String(s).replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]!);
const pre = (v: unknown) => `<pre>${esc(typeof v === 'string' ? v : JSON.stringify(v, null, 2))}</pre>`;
const AGENTS = ['datum', 'vantage', 'pulse', 'keel'] as const;
const NAME = { datum: 'Datum · world-planner · lines', vantage: 'Vantage · level-designer · stops and stations', pulse: 'Pulse · schedule-ai-designer · timetable', keel: 'Keel · simulation-engineer · critic' };

type Hit = { key: string; section: string; source: string; signals: string[]; score: number; text: string };
type CallRec = { id: string; role: string; model: string; costUsd: number; seconds: number; retrieval: { queries: string[]; pinned: string[]; hits: Hit[] } };
type Fault = { owner: string; rule: string; fault: string; fix: string; chunks: string[] };

const run = await json(`${dir}/run.json`);
const ranks = await json(`${here}/evidence/ranks.json`);
const rounds = readdirSync(dir).filter(d => d.startsWith('round-')).sort();
const sectionOf = new Map<string, string>();

async function callBlock(round: string, agent: typeof AGENTS[number], seen: Set<string>) {
  const call: CallRec = await json(`${dir}/${round}/${agent}.call.json`), out = await json(`${dir}/${round}/${agent}.json`);
  for (const h of call.retrieval.hits) sectionOf.set(h.key, h.section);
  if (seen.has(call.id)) return `<p class="reused">${esc(NAME[agent])}: reused call <code>${esc(call.id)}</code> from an earlier round (no fault routed to this owner).</p>`;
  seen.add(call.id);
  const hits = call.retrieval.hits.map((h, i) => `<details><summary>${i + 1}. <code>${esc(h.key)}</code> ${esc(h.section)} <span class="sig">${esc(h.signals.join('+'))}${h.score ? ' · ' + h.score.toFixed(4) : ''}</span></summary>${pre(h.text)}</details>`).join('');
  return `<section class="call"><h4>${esc(call.id)} · ${esc(NAME[agent])}</h4>
<p class="meta">${esc(call.model)} · ${call.seconds} s · $${call.costUsd.toFixed(3)} · prompt: <a href="evidence/${runId}/transcripts/${esc(call.id)}.prompt.txt">${esc(call.id)}.prompt.txt</a> · transcript: <a href="evidence/${runId}/transcripts/${esc(call.id)}.attempt-1.events.jsonl">events.jsonl</a></p>
<div class="grid"><div><h5>Query</h5><ol>${call.retrieval.queries.map(q => `<li>${esc(q)}</li>`).join('')}</ol>${call.retrieval.pinned.length ? `<p>Pinned (critic-cited): ${call.retrieval.pinned.map(k => `<code>${esc(k)}</code>`).join(' ')}</p>` : ''}</div>
<div><h5>Retrieved chunks (top ${call.retrieval.hits.length})</h5>${hits}</div>
<div><h5>Output</h5><details open><summary>${esc(agent)}.json</summary>${pre(out)}</details></div></div></section>`;
}

function faultsTable(faults: Fault[]) {
  if (!faults.length) return '<p>No faults.</p>';
  return `<table><tr><th>owner</th><th>rule</th><th>fault</th><th>fix asked</th><th>cited chunks</th></tr>${faults.map(f =>
    `<tr><td>${esc(f.owner)}</td><td>${esc(f.rule)}</td><td>${esc(f.fault)}</td><td>${esc(f.fix)}</td><td>${f.chunks.map(k => `<code>${esc(k)}</code><br><small>${esc(sectionOf.get(k) ?? '')}</small>`).join('<br>')}</td></tr>`).join('')}</table>`;
}

const seen = new Set<string>();
let body = '';
for (const [i, r] of rounds.entries()) {
  const meta = run.rounds[i], checks = await json(`${dir}/${r}/checks.json`), keel = await json(`${dir}/${r}/keel.json`);
  const calls = [];
  for (const a of AGENTS) calls.push(await callBlock(r, a, seen));
  const failed = checks.checks.filter((c: { ok: boolean }) => !c.ok);
  body += `<h3>Round ${meta.round} · <code>${esc(meta.version)}</code> · checker ${meta.checkerOk ? 'clean' : 'FAIL'} (${checks.checks.length - failed.length}/${checks.checks.length}) · Keel ${esc(meta.keelVerdict)}</h3>
<p>Ran: ${esc(meta.ran.join(', '))}${meta.reused.length ? ` · reused: ${esc(meta.reused.join(', '))}` : ''} · sheet: <a href="evidence/${runId}/sheets/${plan}-v${esc(meta.version)}.html">${plan}-v${esc(meta.version)}.html</a></p>
${calls.join('\n')}
<h4>Critic verdict · round ${meta.round}: ${esc(keel.verdict)}</h4><p>${esc(keel.summary)}</p>${faultsTable(keel.faults)}`;
}

const rankRows = Object.entries(ranks).filter(([, v]) => v && typeof v === 'object' && 'rank' in (v as object)).map(([k, v]) => {
  const x = v as { queries: string[]; rank: number | null; inTop8: boolean };
  return `<tr><td>${k === 'old' ? 'before: single query' : 'after: ' + esc(k)}</td><td>${x.queries.map(q => esc(q)).join('<br>')}</td><td class="${x.inTop8 ? 'ok' : 'bad'}">${x.rank ?? 'not in top 12'}</td></tr>`;
}).join('');

const html = `<!doctype html><html lang="en"><meta charset="utf-8"><title>A04 content pipeline · ${runId} report</title>
<style>body{font:14px/1.45 system-ui,sans-serif;margin:2rem auto;max-width:1500px;padding:0 1rem;color:#1d2a30;background:#fbf8f1}h1,h2,h3{color:#0f5d63}code{background:#eee6d6;padding:0 .25em;border-radius:3px}
pre{white-space:pre-wrap;word-break:break-word;background:#f3eee2;padding:.5em;max-height:28em;overflow:auto;font-size:12px}.grid{display:grid;grid-template-columns:1fr 1.4fr 1.6fr;gap:1rem}.call{border-top:2px solid #0f5d63;margin:1.2rem 0;padding-top:.3rem}
.meta,.sig,.reused{color:#5b6b70;font-size:12px}table{border-collapse:collapse;width:100%;margin:.5rem 0}td,th{border:1px solid #d8cfbd;padding:.35em;vertical-align:top;text-align:left}.ok{color:#0a7a3a;font-weight:600}.bad{color:#c0392b;font-weight:600}details summary{cursor:pointer}</style>
<h1>A04 content pipeline · run ${runId} · ${esc(run.outcome)}</h1>
<p>Plan <code>${plan}</code> ${esc(run.inputPlan.version)} → <code>${esc(run.rounds.at(-1).version)}</code> · ${run.rounds.length} rounds · ${run.cost.calls} model calls · $${run.cost.usd.toFixed(2)} (CLI-reported) · ${run.calls.reduce((a: number, c: { seconds: number }) => a + c.seconds, 0)} s of model time · seeded: ${run.seeded} · date ${esc(run.date)}</p>
<p>Corpus <code>${esc(run.corpusHash.slice(0, 12))}</code> · embeddings ${esc(run.embeddingModel)} · checker code <code>${esc(run.toolsHash.slice(0, 12))}</code> · committed record: <code>runs/levels/${plan}/${runId}/</code> · replay: <code>bun run levels:replay ${runId}</code></p>
<h2>Pipeline</h2><p>Objective (layer-4 brief) → hybrid retrieval (BGE HNSW + BM25, reciprocal rank fusion) → crew member (card as system prompt, structured output only, no tools) → zod schema → proposal applied to a copy of the plan → deterministic plan checker (strict, exit code) + CAD sheet → Keel critic (cites chunk ids, names an owner) → faults routed to their owner with the cited chunks pinned (≤ 3 rounds). ACCEPTED = checker exit 0 and Keel pass.</p>
<h2>Sheets</h2><ul><li>Before: <a href="evidence/${runId}/sheets/${plan}-v0.1.0.html">v0.1.0</a> (strict: fails N-R1 reach, S-S2 sky station spacing)</li>${run.rounds.map((r: { version: string; verdict: string }) => `<li><a href="evidence/${runId}/sheets/${plan}-v${esc(r.version)}.html">v${esc(r.version)}</a> · ${esc(r.verdict)}</li>`).join('')}</ul>
<h2>Retrieval tweak: where <code>standards › tram</code> (home of <code>stopSpacing</code>) ranks</h2>
<table><tr><th>query set</th><th>queries</th><th>rank (top 8 = in context)</th></tr>${rankRows}</table>
<h2>Calls: query · retrieved chunks · output</h2>${body}
<p class="meta">Generated by courses/a04-content-pipeline/build-report.ts from committed run data.</p></html>`;
await Bun.write(`${here}/report.html`, html);
console.log(`report → ${here}/report.html`);
