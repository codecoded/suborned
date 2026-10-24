// Brief 060 §3 (Suture): the pass report (out/qa/<run>/report.html, self-contained) and the commit-ready summary
// (briefs/060/qa/<run>.summary.json: counts per check, the build and engine, the ranked issue keys, the balance headlines).
import { existsSync, readFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import type { Issue } from './issues';
import type { Agg, Tables } from './tables';
import type { Lane } from './sim';
import type { Statics } from './statics';

export type LaneStat = { lane: Lane; faithful: number; /** Sky's own sessions among the seeds (the rest are automation) */ skySeeds?: number; seeds: number; mutants: number; plays: number; errors: number; replayChecked: number; seconds: number; host: string };
export type Pass = { run: string; at: string; seconds: number; lanes: LaneStat[]; issues: Issue[]; tables: Tables; checks: Record<string, Record<string, number>>; previous?: string; fixed: string[]; root: string; /** the static checks beside the plays (statics.ts); absent when the pass ran without them */ statics?: Statics; staticsNew?: string[]; staticsFixed?: string[] };

const esc = (s: unknown) => String(s ?? '').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]!);
const pct = (x: number) => `${(x * 100).toFixed(x < 0.1 && x > 0 ? 1 : 0)}%`;
const top = (r: Record<string, number>, n = 4) => Object.entries(r).sort((a, b) => b[1] - a[1]).slice(0, n).map(([k, v]) => `${k} ${v}`).join(', ');
const winCell = (a: Agg) => a.win ? `${a.win.s.toFixed(0)} s · $${Math.round(a.win.spent).toLocaleString('en-US')} · T ${a.win.trail.toFixed(1)} · A ${a.win.alert.toFixed(1)}` : '–';

export function replayCommand(scriptPath: string, lane: string): string {
  return `bun tools/qa/run.ts --replay ${scriptPath} --engine ${lane === 'bot' ? 'build' : lane}`; // the bot's replay is the built player's session: its engine is the build lane's
}

/** A bot shot as a small embedded JPEG (sips: 720 px wide, quality 70), else "". */
export function thumb(file: string, root: string): string {
  const abs = file.startsWith('/') ? file : join(root, file); if (!existsSync(abs)) return '';
  const tmp = join(tmpdir(), `qa-thumb-${process.pid}-${Math.random().toString(36).slice(2)}.jpg`);
  const r = Bun.spawnSync(['sips', '-Z', '720', '-s', 'format', 'jpeg', '-s', 'formatOptions', '70', abs, '--out', tmp], { stdout: 'ignore', stderr: 'ignore' });
  if (r.exitCode !== 0 || !existsSync(tmp)) return '';
  const b = readFileSync(tmp).toString('base64'); rmSync(tmp, { force: true }); return `data:image/jpeg;base64,${b}`;
}

export function summary(p: Pass) {
  return {
    schema: 'suborned.qa.summary/1', run: p.run, at: p.at, seconds: Math.round(p.seconds), previous: p.previous ?? null,
    lanes: p.lanes.map(l => ({ name: l.lane.name, label: l.lane.label, engine: l.lane.engine, inputs: l.lane.inputs, build: l.lane.build ?? null, seeds: l.seeds, skySessions: l.skySeeds ?? 0, sessionsExact: l.faithful, mutants: l.mutants, plays: l.plays, errors: l.errors, replayChecked: l.replayChecked, seconds: Math.round(l.seconds) })),
    checks: p.checks,
    issues: p.issues.filter(i => i.severity !== 'info').map(i => ({ key: i.key, check: i.check, severity: i.severity, owner: i.owner, title: i.title, plays: i.plays, share: Object.fromEntries(Object.entries(i.share).map(([k, v]) => [k, Number(v.toFixed(4))])), score: Number(i.score.toFixed(4)), change: i.change ?? null, firstSeen: i.firstSeen ?? null })),
    fixed: p.fixed,
    ...(p.statics ? { statics: { seconds: Math.round(p.statics.seconds), checks: p.statics.checks, failed: p.statics.failed.map(f => ({ id: f.id, group: f.group, value: f.value, ...(f.ratchet !== undefined ? { ratchet: f.ratchet } : {}), ...(f.detail ? { detail: f.detail.slice(0, 300) } : {}) })), info: p.statics.info.map(f => ({ id: f.id, ok: f.ok, value: f.value })), geometry: p.statics.geometry.map(f => ({ id: f.id, ok: f.ok, value: f.value, ratchet: f.ratchet ?? null })), new: p.staticsNew ?? [], fixed: p.staticsFixed ?? [] } } : {}),
    balance: {
      t2: p.tables.t2.map(t => ({ lane: t.lane, loudWin: Number(t.loud.winRate.toFixed(3)), quietWin: Number(t.quiet.winRate.toFixed(3)), loudWinS: t.loud.win?.s ?? null, quietWinS: t.quiet.win?.s ?? null, loudFasterAndCheaper: t.loudFasterAndCheaper })),
      t9: p.tables.t9.map(t => ({ lane: t.lane, unseenPlays: t.unseen.plays, unseenWinAtAlert1: Number(t.unseen.winAtAlert1.toFixed(3)), seenPlays: t.seen.plays, seenFailOrLoud: Number(t.seen.failOrLoud.toFixed(3)), pass: t.pass })),
      byFamily: p.tables.byFamily.map(f => ({ lane: f.lane, seeds: f.seedKind, family: f.family, plays: f.agg.plays, win: Number(f.agg.winRate.toFixed(3)), flipped: f.flipped })),
    },
  };
}

export function html(p: Pass): string {
  const sky = p.issues.filter(i => i.severity !== 'info').slice(0, 10);
  const lanesLine = p.lanes.map(l => `<b>${esc(l.lane.name)}</b>: ${esc(l.lane.label)}, ${esc(l.lane.engine)}, inputs ${esc(l.lane.inputs)} · ${l.seeds} seeds (${l.skySeeds ?? 0} of them Sky's own sessions, ${l.faithful} of which replay exactly as he played them), ${l.mutants} mutants, ${l.plays} plays in ${l.seconds.toFixed(0)} s (${esc(l.host)})`).join('<br>');
  const issueCard = (i: Issue, n: number) => {
    const ex = i.exemplars[0];
    const rows = i.exemplars.map(e => `<li><code>${esc(e.lane)}</code> ${esc(e.item)}${e.mutations.length ? ' — ' + esc(e.mutations.join('; ')) : ' (unmutated)'}<br><span class="dim">expected:</span> ${esc(e.finding.expected)}<br><span class="dim">observed:</span> ${esc(e.finding.observed)}${e.script ? `<br><code class="cmd">${esc(replayCommand(e.script, e.lane))}</code>` : ''}</li>`).join('');
    return `<section class="issue ${i.severity}"><h3>${n}. <span class="sev">${i.severity}</span> ${esc(i.title)}</h3>
<p class="meta">${esc(i.check)} · owner <b>${esc(i.owner)}</b> · ${Object.entries(i.plays).map(([l, k]) => `${esc(l)}: ${k} plays (${pct(i.share[l] ?? 0)})`).join(' · ')} · ${i.change ? esc(i.change) : 'new'}${i.firstSeen ? ' · first seen ' + esc(i.firstSeen) : ''} · <code>${esc(i.key)}</code></p>
${ex ? `<p><b>Steps:</b> ${ex.lane === 'bot' ? `the built player played itself (${esc(ex.seed.replace(/^bot /, ''))}); at ${esc(ex.finding.observed.match(/, (\d+:\d\d)\)$/)?.[1] ?? 'the end')} of the mission clock the check saw it. Replay the session to that moment with the command below, or look at the shot.` : `play ${ex.seedKind === 'session' ? (ex.seedBy === 'sky' ? "Sky's saved session" : 'the saved proof session') : 'the route'} <code>${esc(ex.seed)}</code>${ex.mutations.length ? ', changed: ' + esc(ex.mutations.join('; ')) : ' as recorded'}, then read the log at ${esc(ex.finding.observed.match(/\d+:\d\d\.\d/)?.[0] ?? 'the end')}.`}</p>` : ''}
<ul>${rows}</ul>${(() => { const sh = i.exemplars.find(e => e.shot)?.shot; const src = sh ? thumb(sh, p.root) : ''; return src ? `<p><img src="${src}" alt="${esc(i.title)}" style="max-width:720px;width:100%;border-radius:4px"><br><span class="dim">${esc(sh)}</span></p>` : '<p class="dim">Shot: none (tier 1 plays the engine headless; the bot lane keeps one at each anomaly).</p>'; })()}</section>`;
  };
  const table = (head: string[], body: string[][]) => `<table><tr>${head.map(h => `<th>${esc(h)}</th>`).join('')}</tr>${body.map(r => `<tr>${r.map(c => `<td>${c}</td>`).join('')}</tr>`).join('')}</table>`;
  const all = table(['#', 'sev', 'check', 'issue', 'owner', 'plays', 'score', 'change'], p.issues.map((i, k) => [String(k + 1), i.severity, esc(i.check), esc(i.title), esc(i.owner), esc(Object.entries(i.plays).map(([l, n]) => `${l} ${n}`).join(', ')), i.score.toFixed(3), esc(i.change ?? 'new')]));
  const fam = table(['lane', 'seeds', 'family', 'plays', 'win', 'outcome moved', 'wins: time · $ · Trail · Alert', 'fails', 'top refusals'], p.tables.byFamily.sort((a, b) => a.lane.localeCompare(b.lane) || a.seedKind.localeCompare(b.seedKind) || a.family.localeCompare(b.family)).map(f => [esc(f.lane), esc(f.seedKind), esc(f.family), String(f.agg.plays), pct(f.agg.winRate), pct(f.flipped / Math.max(1, f.agg.plays)), winCell(f.agg), esc(top(f.agg.fails)), esc(f.topRefusals.map(([c, n]) => `${c} ${n}`).join(', '))]));
  const routeRows = p.tables.bySeed.filter(s => s.seedKind === 'route').sort((a, b) => a.lane.localeCompare(b.lane) || a.seed.localeCompare(b.seed));
  const seeds = table(['lane', 'route', 'kind', 'as written', 'mutants', 'win', 'wins: time · $ · Trail · Alert', 'fails', 'other'], routeRows.map(s => [esc(s.lane), esc(s.seed), esc(s.routeKind), esc(s.seedOutcome), String(s.mutants.plays), pct(s.mutants.winRate), winCell(s.mutants), esc(top(s.mutants.fails)), esc(top(s.mutants.other))]));
  const sess = p.tables.bySeed.filter(s => s.seedKind === 'session');
  const sessLine = [...new Set(sess.map(s => s.lane))].map(ln => {
    const ss = sess.filter(s => s.lane === ln), out: Record<string, number> = {};
    for (const s of ss) out[s.seedOutcome] = (out[s.seedOutcome] ?? 0) + 1;
    return `<b>${esc(ln)}</b>: ${ss.length} sessions replayed: ${esc(top(out, 8))}`;
  }).join('<br>');
  const t2 = table(['lane', 'loud win', 'loud wins', 'quiet win', 'quiet wins', 'reading'], p.tables.t2.map(t => [esc(t.lane), pct(t.loud.winRate), winCell(t.loud), pct(t.quiet.winRate), winCell(t.quiet), esc(t.note)]));
  const t9 = table(['lane', 'unseen takedown plays', 'won at Alert ≤ 1', 'seen takedown plays', 'fail or loud', 'pass'], p.tables.t9.map(t => [esc(t.lane), String(t.unseen.plays), pct(t.unseen.winAtAlert1), String(t.seen.plays), pct(t.seen.failOrLoud), t.pass === null ? 'no seen takedowns' : t.pass ? 'yes' : '<b>no</b>']));
  const ref = table(['lane', 'refusal code', 'orders'], p.tables.refusals.slice(0, 30).map(r => [esc(r.lane), esc(r.code), String(r.n)]));
  const checks = table(['check', ...p.lanes.map(l => l.lane.name + ' plays with a finding')], Object.entries(p.checks).map(([c, v]) => [esc(c), ...p.lanes.map(l => String(v[l.lane.name] ?? 0))]));
  const statics = p.statics ? `<h2>Static checks: the plan, the world, the geometry</h2><p class="dim">${p.statics.checks} rules in ${p.statics.seconds.toFixed(0)} s, ${p.statics.failed.length} failing${p.staticsNew?.length ? ` · <b>new since ${esc(p.previous ?? 'the last pass')}:</b> ${p.staticsNew.map(esc).join(', ')}` : ''}${p.staticsFixed?.length ? ` · <b>fixed:</b> ${p.staticsFixed.map(esc).join(', ')}` : ''}. KR-3 holds that no kerb stone lies across a lay-by's mouth (a kerb stands only where pavement meets carriageway).</p>${table(['rule', 'group', 'status', 'reading'], [...p.statics.failed.map(f => [esc(f.id), esc(f.group), 'FAIL' + (f.ratchet !== undefined ? ` (ratchet ${f.ratchet})` : ''), esc(f.value + (f.detail ? ' · ' + f.detail.slice(0, 260) : ''))]), ...p.statics.info.filter(f => f.ok).map(f => [esc(f.id), esc(f.group), 'ok', esc(f.value)])])}` : '';
  return `<!doctype html><html lang="en"><head><meta charset="utf-8"><title>QA ${esc(p.run)}</title><style>
body{font:14px/1.45 -apple-system,system-ui,sans-serif;margin:24px auto;max-width:1180px;color:#1d2330;background:#fbf8f2}
h1{font-size:22px}h2{margin-top:32px;border-bottom:1px solid #d8d0c0}h3{font-size:15px;margin:0 0 4px}
table{border-collapse:collapse;width:100%;font-size:12.5px;margin:8px 0}td,th{border-bottom:1px solid #e5ddd0;padding:4px 6px;text-align:left;vertical-align:top}th{background:#f1ebe0}
.issue{background:#fff;border-left:4px solid #aaa;padding:10px 14px;margin:12px 0;border-radius:4px}.issue.S1{border-color:#c0392b}.issue.S2{border-color:#d68910}.issue.S3{border-color:#2874a6}.issue.S4{border-color:#7f8c8d}
.sev{font-weight:700}.meta,.dim{color:#6b6458;font-size:12.5px}code{font:12px ui-monospace,Menlo,monospace;background:#f3eee5;padding:0 3px}.cmd{display:inline-block;margin-top:3px}li{margin:6px 0}
</style></head><body>
<h1>Bot playtest, ${p.lanes.some(l => l.lane.name === 'bot') ? 'tiers 1 and 2 (sim fuzz and the built player)' : 'tier 1 (sim fuzz)'} · ${esc(p.run)}</h1>
<p class="dim">${esc(p.at)} · ${p.seconds.toFixed(0)} s in all · brief 060 · Suture<br>${lanesLine}</p>
<p class="dim">Severity: S1 the run breaks (crash, stuck, unfair fail, a card that contradicts itself) · S2 the rules lie (offered then refused, a missing warning) · S3 a visible oddity · S4 polish. Ranked by severity × the share of a lane's plays affected.${p.previous ? ' Change against ' + esc(p.previous) + '.' : ' No earlier pass to compare.'}</p>
<h2>For Sky: ${sky.length} issue${sky.length === 1 ? '' : 's'}</h2>
${sky.map((i, k) => issueCard(i, k + 1)).join('\n')}
${p.fixed.length ? `<h2>Fixed since the last pass</h2><p>${p.fixed.map(esc).join('<br>')}</p>` : ''}
<h2>Every issue</h2>${all}
${statics}
<h2>Checks</h2>${checks}
<p class="dim">The built player's checks (lane bot: Q-GIVER, Q-OFFER on the real offer, Q-WARN-HUD, Q-CARD, Q-FEET, Q-SOLID, Q-ERR, Q-FRAME, Q-MEM, Q-STUCK) are regraded here before they rank (tools/qa/grade.ts): Q-SOLID by its cause against the drawn ground, a refusal for a doer who went down in the order's own tick as a race, Q-MEM by growth that holds, the feet share. What a regrade clears stays in the full table as info. Waiting for other owners: Q-SOLID and Q-FEET in the engine (Keel's trace + Splice's checker), Q-PREVIEW (a Quote or Preview beside each Issue), Q-DRAWN. Q-LEARN here is a proxy from the log (the same completion line twice), exact once the trace carries the knowledge diff.</p>
<h2>What works: by mutation family</h2>${fam}
<h2>What works: by route (its mutants)</h2>${seeds}
<p>${sessLine}</p>
<h2>T2 proxy: does loud dominate?</h2>${t2}
<h2>T9 proxy: does a takedown cost what it shows?</h2>${t9}
<h2>Refusal codes</h2>${ref}
</body></html>`;
}
