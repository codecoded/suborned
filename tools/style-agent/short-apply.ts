import { readFileSync, writeFileSync } from 'node:fs';
import { relative, resolve } from 'node:path';
import { TextFile } from '../../contracts/missions/handover/text.schema.ts';
import { whyProblems } from '../../contracts/missions/handover/why.schema.ts';
import { root } from '../rag/config';
import { TEXT_FILE } from './guide';
import { barkKey } from './short-barks';
import { loadShortBriefs } from './short-briefs';
import { mergeWhy } from './short-checks';
import { acceptedByFamily } from './short-cli';
import { writeLabels } from './short-labels';
import type { ShortResult, ShortSet } from './short-types';

/** Writes each family's accepted lines into text.json, keeping the file's own layout: a bark row, a why cause or a why word is one line in the
 *  file, so each is replaced where it stands; the debrief page goes in as `debrief.page`; a HUD label replaces its string where it stands. Only lines from a verified, accepted live run go in. */
type Row = { event: string; speaker: string; lines: string[]; [k: string]: unknown };
const once = (raw: string, from: string, to: string, what: string) => {
  const n = raw.split(from).length - 1;
  if (n !== 1) throw new Error(`${what}: found ${n} times in text.json, expected once`);
  return raw.replace(from, () => to);
};
/** A debrief page in the file's style: a short value on one line, a long object one key a line. */
function fmt(v: unknown, pad: string): string {
  const flat = JSON.stringify(v);
  if (typeof v !== 'object' || v === null || flat.length <= 150) return flat;
  return `{\n${Object.entries(v).map(([k, x]) => `${pad} ${JSON.stringify(k)}: ${fmt(x, pad + ' ')}`).join(',\n')}\n${pad}}`;
}
const set = (o: Record<string, unknown>, path: string[], value: string) => {
  let at = o;
  for (const s of path.slice(0, -1)) at = (at[s] ??= {}) as Record<string, unknown>;
  at[path.at(-1)!] = value;
};

/** `restore`: bark or why families whose lines go back to what they were when their run began (the run's brief keeps them), so a review can undo one family. */
export async function applyShort(dry: boolean, o: { runs?: Map<string, { run: ShortResult; path: string }>; file?: string; restore?: Set<string> } = {}) {
  const best = o.runs ?? await acceptedByFamily(), families = loadShortBriefs(root), file = o.file ?? resolve(root, TEXT_FILE);
  let raw = readFileSync(file, 'utf8');
  const text = JSON.parse(raw), log: string[] = [], cuts: string[] = [];
  /** A family's last round, or with `restore` its items as the run found them. */
  const restored = (run: ShortResult): ShortSet['items'] => !o.restore?.has(run.brief.id) ? run.rounds.at(-1)!.set.items
    : run.brief.items.map(i => ({ key: i.key, cut: false, reason: '', fields: i.fields.map(f => ({ id: f.id, lines: i.current[f.id] ?? [] })) }));
  if (o.restore) for (const id of o.restore) { if (!best.has(id)) throw new Error(`--restore ${id}: no accepted run to restore from`); if (best.get(id)!.run.brief.kind === 'debrief') throw new Error(`--restore ${id}: the page is restored by leaving its family out`); }
  const missing = families.filter(f => !best.has(f.id)).map(f => f.id), held: string[] = [];
  // a row two families have (a family split or renamed at /13 and /14: the cell's scene lines, the guard's replies, the Mislead) is written by the newest run that has it
  const owner = new Map<string, string>();
  for (const [id, { run }] of [...best].filter(([, x]) => x.run.brief.kind === 'bark').sort((a, b) => a[1].run.startedAt - b[1].run.startedAt)) for (const i of run.brief.items) owner.set(i.key, id);
  // barks: each row is one line of the file
  for (const [id, { run, path }] of best) {
    if (run.brief.kind !== 'bark') continue;
    for (const x of restored(run)) {
      const row = (text.dialogue as Row[]).find(r => barkKey(r as never) === x.key);
      if (!row && x.cut) continue; // a cut an earlier apply made (brief 102's sweep): the row is already gone
      if (!row) throw new Error(`${id}: no row ${x.key} in text.json`);
      if (owner.get(x.key) !== id) continue; // a newer family's run writes it
      if (row.sky || row.hand) { held.push(x.key); continue; } // Sky's own words win over any run (brief 102), as "Turn driver by", and so do Fable's hand edits (guide /17)
      if (x.cut) { raw = once(raw, `  ${JSON.stringify(row)},\n`, '', x.key); cuts.push(`${x.key}: ${x.reason}`); continue; }
      const lines = x.fields.find(f => f.id === 'lines')!.lines;
      if (JSON.stringify(lines) !== JSON.stringify(row.lines)) { raw = once(raw, JSON.stringify(row), JSON.stringify({ ...row, lines }), x.key); log.push(`${x.key}`); }
    }
    log.push(`  ← ${relative(root, path)}`);
  }
  // the why block: each cause, way and word is one line of the file
  let why = text.why;
  for (const [, { run, path }] of best) if (run.brief.kind === 'why') { why = mergeWhy(run.brief, { items: restored(run) }, why); log.push(`  ← ${relative(root, path)}`); }
  const units: [string, unknown, unknown][] = [];
  for (const g of ['alert', 'police'] as const) for (const k of Object.keys(text.why[g])) units.push([k, text.why[g][k], why[g][k]]);
  for (const k of ['again', 'effect', 'by', 'places'] as const) units.push([k, text.why[k], why[k]]);
  for (const k of Object.keys(text.why.onWay.ways)) units.push([k, text.why.onWay.ways[k], why.onWay.ways[k]]);
  for (const k of ['to', 'fix', 'nofix', 'stopped'] as const) units.push([k, text.why.onWay[k], why.onWay[k]]);
  for (const k of ['head', 'empty', 'start'] as const) units.push([k, text.why.meter[k], why.meter[k]]);
  for (const [k, before, after] of units) if (JSON.stringify(before) !== JSON.stringify(after)) {
    raw = once(raw, `"${k}": ${JSON.stringify(before)}`, `"${k}": ${JSON.stringify(after)}`, `why ${k}`); log.push(`why ${k}`);
  }
  // the debrief page: one item a key (a single field is the string itself; more fields are an object of them)
  // the page's schema needs every key, so it goes in only once every debrief family is accepted
  const pageRuns = [...best.values()].filter(x => x.run.brief.kind === 'debrief'), pageFamilies = families.filter(f => f.kind === 'debrief');
  if (pageRuns.length && pageRuns.length < pageFamilies.length) log.push(`debrief.page waits: ${pageRuns.length} of ${pageFamilies.length} families accepted`);
  else if (pageRuns.length) {
    const page: Record<string, unknown> = {};
    for (const { run, path } of pageRuns) {
      for (const x of run.rounds.at(-1)!.set.items) {
        const it = run.brief.items.find(i => i.key === x.key)!;
        for (const f of x.fields) set(page, it.fields.length === 1 ? x.key.split('.') : [...x.key.split('.'), f.id], f.lines[0]!);
      }
      log.push(`  ← ${relative(root, path)}`);
    }
    // the page is the debrief block's last key: replace from its key to the block's end (never another block's "page", such as the briefing's)
    const start = raw.indexOf('\n "debrief": {'), end = raw.indexOf('\n },\n "events": {', start);
    if (start < 0 || end < 0) throw new Error('text.json: the debrief block is not where it was');
    const at = text.debrief.page ? raw.lastIndexOf(',\n  "page": {', end) : end;
    if (at < start) throw new Error('text.json: debrief.page is not the debrief block\'s last key');
    raw = raw.slice(0, at) + `,\n  "page": ${fmt(page, '  ')}` + raw.slice(end);
    const rule = text.debrief.rule as string;
    if (!rule.includes(PAGE_RULE)) raw = once(raw, `"rule": ${JSON.stringify(rule)}`, `"rule": ${JSON.stringify(`${rule} ${PAGE_RULE}`)}`, 'debrief.rule');
    log.push(`debrief.page (${Object.keys(page).length} sections), and its rule at the end of debrief.rule`);
  }
  // guide /10: the HUD's labels, each where it stands (a timeline mark's word, a lozenge card's title or line, the shot row's label and the Alt outcome it shares)
  const labels = [...best.values()].filter(x => x.run.brief.kind === 'label');
  if (labels.length) {
    const w = writeLabels(raw, labels.map(({ run }) => ({ id: run.brief.id, items: restored(run) })));
    raw = w.raw; log.push(...w.changed, ...labels.map(({ path }) => `  ← ${relative(root, path)}`));
  }
  // a cut speaker leaves its event; a cut of its last speaker quiets the event: its log codes join silentLogs with the cut's reason (brief 102: the patrol's status lines)
  for (const c of cuts) {
    const [event, ...rest] = c.split(':')[0]!.split('/'), parsed = JSON.parse(raw), ev = parsed.events[event!];
    for (const sp of ev.speakers as string[]) if (!(parsed.dialogue as Row[]).some(r => r.event === event && r.speaker === sp)) {
      if (ev.speakers.length === 1 && Array.isArray(ev.log) && ev.log.length && !(ev.log as string[]).some(k => k in parsed.silentLogs)) {
        raw = once(raw, `  "${event}": ${JSON.stringify(ev)},\n`, '', `event ${event}`);
        const why = `brief 102's sweep (Fable 2026-11-08): the loop cut ${event}'s last line: ${c.slice(c.indexOf(':') + 1).trim()}`;
        raw = once(raw, ' "silentLogs": {\n', ` "silentLogs": {\n${(ev.log as string[]).map(k => `  ${JSON.stringify(k)}: ${JSON.stringify(why)},\n`).join('')}`, 'silentLogs');
        log.push(`${event}: quiet (${(ev.log as string[]).join(', ')} into silentLogs)`); break;
      }
      if (ev.speakers.length === 1) throw new Error(`${event}: its last speaker was cut; silence the event by hand (${rest.join('/')})`);
      raw = once(raw, `"${event}": ${JSON.stringify(ev)}`, `"${event}": ${JSON.stringify({ ...ev, speakers: ev.speakers.filter((s: string) => s !== sp) })}`, `event ${event}`);
    }
  }
  const out = TextFile.parse(JSON.parse(raw)), problems = out.why ? whyProblems(out.why) : [];
  const dup = new Map<string, string>(), dups: string[] = [];
  for (const r of out.dialogue) for (const l of r.lines) { const k = barkKey(r as never), o = dup.get(l); if (o && o !== k) dups.push(`"${l}" in ${o} and ${k}`); dup.set(l, k); }
  console.log(log.join('\n'));
  if (cuts.length) console.log(`\nCut:\n${cuts.join('\n')}`);
  if (missing.length) console.log(`\nNo accepted run yet: ${missing.join(', ')}`);
  if (held.length) console.log(`\nSky's words and Fable's hand edits, kept over the runs: ${[...new Set(held)].join(', ')}`);
  if (problems.length || dups.length) throw new Error(`Not written: ${[...problems, ...dups.map(d => `repeated line ${d}`)].join('; ')}`);
  if (dry) { console.log('\n(dry run: text.json unchanged)'); return; }
  writeFileSync(file, raw);
  console.log(`\nWrote ${TEXT_FILE}`);
}

export const PAGE_RULE = '`page`, brief 099 (Sky 2026-11-07: "that should be a like our book in terms of presentation, nice, visually appealing with explainers and stats"; "yeah, try A"): the debrief page\'s own words, concept A, the front page. The page also reads, unchanged: headline, result and cause (the outcome and its line), the start page\'s rows (book.job.needs.ways) with `how` and the time each came true, investigation and causes, assets (under city.kept), standing (the band and its cause under Standing), and the why block (an Alert rise\'s short under Alert; a row\'s hover is its line). Fills: {clock} m:ss, {op} an agent as the book\'s cell page names them (beside their face), {who} a person or camera as names.targets says them, {n} {trail} {alert} {missions} {guards} {cameras} {place} {by} {list} from the debrief and the ledger. Written through the style agent\'s short-line loop (Sky 2026-11-07: "sure"): Fable wrote, Motive scored 10/10; the runs are in runs/style-agent/short/.';
