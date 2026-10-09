// usage: bun run levels:sheets
// Re-renders every recorded round's CAD sheet from its committed plan.json, and writes out/levels/index.html listing all runs.
import { existsSync, readdirSync } from 'node:fs';
import { resolve, relative } from 'node:path';
import { viaCli } from './checker';
import { outRoot, runDirs, runsRoot } from './paths';

const esc = (s: unknown) => String(s).replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]!);
const rows: string[] = [];
for (const planId of (existsSync(runsRoot()) ? readdirSync(runsRoot()) : []).filter(d => !d.startsWith('.')).sort()) {
  for (const runId of runDirs(planId)) {
    const dir = resolve(runsRoot(), planId, runId), od = resolve(outRoot(), planId, runId), run = await Bun.file(`${dir}/run.json`).json();
    const links: string[] = [];
    for (const r of run.rounds as any[]) {
      const sheet = `${od}/sheets/${planId}-v${r.version}.html`;
      if (r.skipped?.includes('checker')) { links.push(`${esc(r.version)} (checker skipped)`); continue; }
      await viaCli(`${dir}/round-${r.round}/plan.json`, sheet, `${od}/round-${r.round}/checks.rerender.json`);
      links.push(`<a href="${esc(relative(outRoot(), sheet))}">${esc(r.version)}</a> ${r.verdict === 'pass' ? '✓' : '✗'}`);
    }
    console.log(`${planId}/${runId}: ${links.length} sheet(s)`);
    rows.push(`<tr><td>${esc(planId)}/${runId}</td><td>${esc(String(run.date).slice(0, 16).replace('T', ' '))}</td><td class="${run.outcome === 'ACCEPTED' ? 'ok' : 'bad'}">${esc(run.outcome)}${run.seeded ? ` <small>(seeded: ${esc(run.seeded)})</small>` : ''}</td><td>${run.rounds.length}</td><td>$${esc(run.cost?.usd ?? '?')}</td><td>${esc(run.finalVersion ?? '')}</td><td>${links.join('<br>')}</td></tr>`);
  }
}
const html = `<!doctype html><meta charset="utf-8"><title>Level runs</title><style>body{font:14px system-ui;margin:2rem}table{border-collapse:collapse}td,th{border:1px solid #ccc;padding:4px 10px;text-align:left;vertical-align:top}.ok{color:#0a7a3d;font-weight:700}.bad{color:#b3261e;font-weight:700}</style>
<h1>Level runs</h1><table><tr><th>Run</th><th>Date</th><th>Outcome</th><th>Rounds</th><th>Cost</th><th>Final version</th><th>Sheets</th></tr>${rows.join('')}</table>`;
await Bun.write(`${outRoot()}/index.html`, html);
console.log(`index → ${outRoot()}/index.html`);
