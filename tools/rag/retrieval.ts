// Hybrid retrieval: per query, an HNSW cosine list and a BM25 list, fused by reciprocal rank.
// Chunks a critic cited are pinned, so a revision sees the evidence it was judged on.
// usage: bun run retrieve "<query>"
import { embeddingModel } from './config';
import { hash, loadCorpus, type Chunk } from './corpus';
import { sql, literal } from './database';
import { embed } from './embeddings';

export type Hit = Chunk & { score: number; signals: string[] };
const FIELDS = 'key,source,sourceHash,section,lineStart,lineEnd,text,authority,version,kind';

export function fuse(lists: { signal: string; rows: Chunk[] }[], limit: number): Hit[] {
  const hits = new Map<string, Hit>();
  for (const list of lists) list.rows.forEach((row, i) => {
    const hit = hits.get(row.key) ?? { ...row, score: 0, signals: [] };
    hit.score += 1 / (60 + i + 1); hit.signals.push(list.signal); hits.set(row.key, hit);
  });
  return [...hits.values()].sort((a, b) => b.score - a.score || a.key.localeCompare(b.key)).slice(0, limit);
}

const STOP = new Set(['the', 'and', 'for', 'with', 'that', 'this', 'from', 'not', 'are', 'was', 'who', 'which', 'each', 'its', 'into', 'than', 'then', 'only', 'when', 'what', 'does', 'their', 'have', 'has', 'can', 'may', 'must', 'any', 'all', 'one']);
export const terms = (q: string) => [...new Set(q.toLowerCase().match(/[a-z0-9§]+(?:-[a-z0-9]+)*/g) ?? [])].filter(t => t.length > 2 && !STOP.has(t)).slice(0, 24);

/** BM25 OR semantics: SurrealDB's @@ needs every term, so per-term scores are summed in one batched request. */
async function keyword(q: string): Promise<Chunk[]> {
  const ts = terms(q);
  if (!ts.length) return [];
  const results = await sql<(Chunk & { bm25: number })[]>(ts.map(t => `SELECT ${FIELDS}, search::score(1) AS bm25 FROM chunk WHERE text @1@ ${literal(t)};`).join('\n'));
  const score = new Map<string, { row: Chunk; s: number }>();
  for (const rows of results) for (const row of rows ?? []) { const e = score.get(row.key) ?? { row, s: 0 }; e.s += row.bm25; score.set(row.key, e); }
  return [...score.values()].sort((a, b) => b.s - a.s || a.row.key.localeCompare(b.row.key)).slice(0, 16).map(e => e.row);
}

export async function retrieve(query: string | string[], limit = 8, pinned: string[] = []) {
  const queries = (Array.isArray(query) ? query : [query]).map(q => q.trim()).filter(Boolean);
  if (!queries.length || !Number.isInteger(limit) || limit < 1 || limit > 12) throw new Error('Use a nonempty query and limit 1-12');
  const manifest = (await sql<{ corpusHash: string; embeddingModel: string }[]>('SELECT * FROM corpus:current;'))[0]?.[0];
  if (!manifest) throw new Error('No corpus indexed. Run bun run ingest first.');
  if (manifest.embeddingModel !== embeddingModel || manifest.corpusHash !== hash(JSON.stringify(await loadCorpus()))) throw new Error('GDD, plan or embedding model changed since the last ingest. Run bun run ingest.');
  const vectors = await embed(queries, 'query'), lists: { signal: string; rows: Chunk[] }[] = [];
  for (const [i, q] of queries.entries()) {
    const tag = queries.length > 1 ? `:${i + 1}` : '';
    const [semantic] = await sql<Chunk[]>(`SELECT ${FIELDS}, vector::distance::knn() AS distance FROM chunk WHERE embedding <|16,120|> ${literal(vectors[i])} ORDER BY distance;`);
    lists.push({ signal: 'semantic' + tag, rows: semantic ?? [] }, { signal: 'keyword' + tag, rows: await keyword(q) });
  }
  const hits = fuse(lists, limit);
  const wanted = [...new Set(pinned)].filter(k => !hits.some(h => h.key === k));
  if (wanted.length) {
    const [rows] = await sql<Chunk[]>(`SELECT ${FIELDS} FROM chunk WHERE key IN ${literal(wanted)};`);
    for (const row of rows ?? []) hits.push({ ...row, score: 0, signals: ['critic-cited'] });
  }
  return { query: queries.join(' ‖ '), queries, pinned: [...new Set(pinned)], method: 'HNSW cosine + full-text BM25 per query, reciprocal rank fusion; critic-cited chunks pinned', corpusHash: manifest.corpusHash, embeddingModel, hits };
}
export type Retrieval = Awaited<ReturnType<typeof retrieve>>;

/** Passages as the agent sees them: id, source lines, authority, text. */
export const context = (r: Retrieval) =>
  r.hits.map(h => `[${h.key}] ${h.source}${h.lineStart ? `:${h.lineStart}-${h.lineEnd}` : ''}${h.signals.includes('critic-cited') ? ' (cited by the critic)' : ''}\nAuthority: ${h.authority}; version: ${h.version}\n${h.text}`).join('\n\n');

if (import.meta.main) {
  const r = await retrieve(process.argv.slice(2).join(' '));
  for (const h of r.hits) console.log(`${h.score.toFixed(4)} [${h.signals.join('+')}] ${h.key} ${h.source} › ${h.section}`);
}
