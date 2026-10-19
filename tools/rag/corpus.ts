// The retrieval corpus: agent GDD (by § section, then paragraph), human GDD (by heading), the district plan (by part, with its skyline), the DecoKit catalogue, and (v4) life.json, the civic block, the missions and the people rules.
// A source change changes chunk keys and the corpus hash, so retrieval refuses a stale index until re-ingest.
import { createHash } from 'node:crypto';
import { existsSync, readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { root } from './config';
import { chunkHtml } from './corpus-html';
import { KIT_FILE, kitChunks } from './kit-corpus';
import { planChunks } from './plan-corpus';
import { blockChunks, lifeChunks, missionChunks, peopleRulesChunks } from './people-corpus';
import { selectLatestGdd } from './gdd-sources';

export const hash = (text: string) => createHash('sha256').update(text).digest('hex');
export type Chunk = { key: string; source: string; sourceHash: string; section: string; lineStart: number; lineEnd: number; text: string; authority: string; version: string; kind?: string };
type Meta = { authority: string; version: string; kind?: string; sourceHash?: string; offset?: number };

/** Paragraph chunks of one markdown section; pipe tables and tight key: value blocks stay together. */
export function chunkDocument(source: string, text: string, meta: Meta = { authority: 'GDD', version: '' }): Chunk[] {
  const lines = text.split('\n'), offset = meta.offset ?? 0, sourceHash = meta.sourceHash ?? hash(text);
  const section = lines.find(l => l.startsWith('## '))?.slice(3) ?? '§01 GAME + CORE';
  const chunks: Chunk[] = [];
  let buffer: string[] = [], start = 1;
  const flush = (lineEnd: number) => {
    const body = buffer.join('\n').trim();
    if (body) chunks.push({ key: hash(`${source}:${offset + start}:${body}`).slice(0, 24), source, sourceHash, section, lineStart: offset + start, lineEnd: offset + lineEnd, text: `${section}\n${body}`, authority: meta.authority, version: meta.version, kind: meta.kind });
    buffer = [];
  };
  for (let i = 0; i < lines.length; i++) {
    if (buffer.join('\n').length > 1100 && !lines[i]?.startsWith('|')) { flush(i); start = i + 1; }
    buffer.push(lines[i]!);
    if (!lines[i]?.trim() && buffer.join('\n').length > 250) { flush(i + 1); start = i + 2; }
  }
  flush(lines.length);
  return chunks;
}

const latestGdd = () => selectLatestGdd(readdirSync(resolve(root, 'GDD')));
const versionOf = (file: string) => file.match(/v\d+\.\d+\.\d+/)?.[0] ?? 'unversioned';
export const sources = { agentGdd: () => latestGdd().agent.path, humanGdd: () => latestGdd().human.path, plans: () => { const dir = process.env.SUBORNED_PLANS ?? 'contracts/world'; return readdirSync(resolve(root, dir)).filter(f => f.endsWith('.plan.json')).sort().map(f => `${dir}/${f}`); },
  life: () => { const dir = 'contracts/world'; return readdirSync(resolve(root, dir)).filter(f => f.endsWith('.life.json')).sort().map(f => `${dir}/${f}`); },
  block: () => { const f = 'contracts/world/civic-block.json'; return existsSync(resolve(root, f)) ? [f] : []; },
  missions: () => { const dir = 'contracts/missions'; return readdirSync(resolve(root, dir), { withFileTypes: true }).filter(d => d.isDirectory() && existsSync(resolve(root, dir, d.name, 'mission.json'))).map(d => `${dir}/${d.name}/mission.json`).sort(); },
  peopleRules: () => { const f = 'contracts/people/people.rules.json'; return existsSync(resolve(root, f)) ? [f] : []; },
  kit: () => { const f = process.env.SUBORNED_KIT ?? KIT_FILE; return existsSync(resolve(root, f)) ? f : undefined; } }; // SUBORNED_KIT: tests point it at a scratch kit

/** Bump when chunking changes: runs record it, and replay labels a mismatch drift (runs without the field are v1: the whole § by paragraph).
 *  v3 (brief 007 F): the plan's signatures, megastructures and tower sites, and the DecoKit catalogue, are chunks (additive: no v2 chunk changed).
 *  v4 (brief 010 B): life.json, the civic block, the missions and the people rules are chunks (additive, appended after the kit: no v3 chunk changed).
 *  v5 (brief 010 follow-up): the people rules' knowledge, mix, introduces and readBy blocks are chunks of their own (additive, appended after the v4 people-rules chunks: no v4 chunk changed). */
export const CHUNKER = 5;
export const SPLIT_OVER = 1000, SPLIT_OVER_SUB = 2000, MERGE_UNDER = 120; // a `###` subsection is already one focused entry (a role block), so it holds together longer
const ENTRY = /^([A-Za-z][\w ()/,+·'.–-]{0,90}?)( \[[BSL]\])?:(\s|$)/;

/**
 * Chunk rule (v2, unchanged in v3), generic for every § of the agent GDD:
 * 1. a unit is a § (or a `###` subsection of one); a § of <= SPLIT_OVER chars (a subsection: SPLIT_OVER_SUB) stays whole;
 * 2. a longer unit splits at its top-level entries, a line starting `name [tag]:` (tables and bullets stay with their entry);
 *    an entry shorter than MERGE_UNDER chars joins the next one unless that one is tagged ([B]/[S]/[L]), so a rule is never cut from its table;
 * 3. every chunk is prefixed with its title, `§02 LOOP + RESOURCES › travel` (parentheticals dropped from titles and names), which is also its `section`, so it reads on its own.
 */
export function chunkSection(source: string, lines: string[], offset: number, meta: Meta): Chunk[] {
  const sourceHash = meta.sourceHash ?? hash(source), section = lines[0]!.slice(3).replace(/\s*\(.*$/, '');
  const subs = lines.flatMap((l, i) => (l.startsWith('### ') ? [i] : [])), cuts = [0, ...subs.filter(i => i > 0), lines.length].filter((v, i, a) => a.indexOf(v) === i);
  const out: Chunk[] = [];
  const emit = (title: string, from: number, to: number) => {
    const body = lines.slice(from, to).join('\n').trim();
    if (body) out.push({ key: hash(`${source}:${offset + from + 1}:${body}`).slice(0, 24), source, sourceHash, section: title, lineStart: offset + from + 1, lineEnd: offset + to, text: `${title}\n${body}`, authority: meta.authority, version: meta.version, kind: meta.kind });
  };
  for (let u = 0; u + 1 < cuts.length; u++) {
    const from = cuts[u]!, to = cuts[u + 1]!, head = lines[from]!.startsWith('### ') ? lines[from]!.slice(4).split(' · ')[0]! : '';
    const unit = head ? `${section} › ${head}` : section, first = head ? from : from + 1; // a subsection keeps its own heading line (role, model); a § heading is the title already
    const body = lines.slice(first, to);
    if (body.join('\n').trim().length <= (head ? SPLIT_OVER_SUB : SPLIT_OVER)) { emit(unit, first, to); continue; }
    const starts: { at: number; name: string; tagged: boolean }[] = [{ at: first, name: '', tagged: false }];
    body.forEach((l, i) => { const m = ENTRY.exec(l); if (m) starts.push({ at: first + i, name: m[1]!.replace(/\s*\(.*$/, '').trim(), tagged: !!m[2] }); });
    if (starts.length > 1 && starts[1]!.at === first) starts.shift();
    for (let k = 0; k < starts.length; k++) {
      let end = k + 1 < starts.length ? starts[k + 1]!.at : to;
      while (k + 1 < starts.length && lines.slice(starts[k]!.at, end).join('\n').trim().length < MERGE_UNDER && !starts[k + 1]!.tagged) { starts.splice(k + 1, 1); end = k + 1 < starts.length ? starts[k + 1]!.at : to; }
      emit(starts[k]!.name ? `${unit} › ${starts[k]!.name}` : unit, starts[k]!.at, end);
    }
  }
  return out;
}

/** The agent GDD split at its `## §` headings, then into entries (chunkSection), so each chunk carries its title and true file lines. */
function agentGdd(source: string, text: string): Chunk[] {
  const lines = text.split('\n'), heads = lines.flatMap((l, i) => (l.startsWith('## §') ? [i] : []));
  const meta = { authority: 'Agent GDD (the human GDD wins on conflict)', version: versionOf(source), kind: 'gdd-agent', sourceHash: hash(text) };
  return heads.flatMap((h, j) => chunkSection(source, lines.slice(h, heads[j + 1] ?? lines.length), h, meta));
}

/** `override` maps a plan (or kit) path to the text to use instead of the file on disk (replay passes the text a run recorded, so later edits do not break it). */
export async function loadCorpus(override: Record<string, string> = {}): Promise<Chunk[]> {
  const over = new Map(Object.entries(override).map(([k, v]) => [resolve(root, k), v]));
  const read = (p: string) => over.get(resolve(root, p)) ?? Bun.file(resolve(root, p)).text();
  const agent = sources.agentGdd(), human = sources.humanGdd();
  const chunks = agentGdd(agent, await read(agent));
  chunks.push(...chunkHtml({ source: human, kind: 'gdd-human', authority: 'Human GDD (wins on conflict)', version: versionOf(human) }, await read(human)));
  for (const p of sources.plans()) chunks.push(...planChunks(p, await read(p)));
  const kit = sources.kit();
  if (kit) chunks.push(...kitChunks(kit, await read(kit))); // a run pins the kit digest, so a later adoption does not change its corpus
  for (const f of sources.life()) chunks.push(...lifeChunks(f, await read(f)));
  for (const f of sources.block()) chunks.push(...blockChunks(f, await read(f)));
  for (const f of sources.missions()) chunks.push(...missionChunks(f, await read(f)));
  for (const f of sources.peopleRules()) chunks.push(...peopleRulesChunks(f, await read(f)));
  return chunks;
}
