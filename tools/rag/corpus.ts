// The retrieval corpus: agent GDD (by § section, then paragraph), human GDD (by heading) and the district plan (by part).
// A source change changes chunk keys and the corpus hash, so retrieval refuses a stale index until re-ingest.
import { createHash } from 'node:crypto';
import { readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { root } from './config';
import { chunkHtml } from './corpus-html';
import { planChunks } from './plan-corpus';

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

const latest = (part: string, ext: string) => `GDD/${readdirSync(resolve(root, 'GDD')).filter(f => f.includes(part) && f.endsWith(ext)).sort().at(-1)}`;
const versionOf = (file: string) => file.match(/v\d+\.\d+\.\d+/)?.[0] ?? 'unversioned';
export const sources = { agentGdd: () => latest('agent-gdd', '.md'), humanGdd: () => latest('-gdd-v', '.html'), plans: () => readdirSync(resolve(root, 'contracts/world')).filter(f => f.endsWith('.plan.json')).sort().map(f => `contracts/world/${f}`) };

/** The agent GDD split at its `## §` headings, so each chunk carries its section and true file lines. */
function agentGdd(source: string, text: string): Chunk[] {
  const lines = text.split('\n'), heads = lines.flatMap((l, i) => (l.startsWith('## §') ? [i] : []));
  const meta = { authority: 'Agent GDD (the human GDD wins on conflict)', version: versionOf(source), kind: 'gdd-agent', sourceHash: hash(text) };
  return heads.flatMap((h, j) => chunkDocument(source, lines.slice(h, heads[j + 1] ?? lines.length).join('\n'), { ...meta, offset: h }));
}

export async function loadCorpus(): Promise<Chunk[]> {
  const read = (p: string) => Bun.file(resolve(root, p)).text();
  const agent = sources.agentGdd(), human = sources.humanGdd();
  const chunks = agentGdd(agent, await read(agent));
  chunks.push(...chunkHtml({ source: human, kind: 'gdd-human', authority: 'Human GDD (wins on conflict)', version: versionOf(human) }, await read(human)));
  for (const p of sources.plans()) chunks.push(...planChunks(p, await read(p)));
  return chunks;
}
