// Embed the corpus and load it into SurrealDB. The manifest is published last, so retrieval refuses a half-loaded index.
// usage: bun run ingest
import { resolve } from 'node:path';
import { root, embeddingModel, writeJson } from './config';
import { CHUNKER, hash, loadCorpus } from './corpus';
import { sql, literal } from './database';
import { embed } from './embeddings';

export async function ingest() {
  const chunks = await loadCorpus(), corpusHash = hash(JSON.stringify(chunks)), vectors: number[][] = [];
  for (let i = 0; i < chunks.length; i += 32) vectors.push(...(await embed(chunks.slice(i, i + 32).map(c => c.text))));
  await sql('DEFINE NAMESPACE IF NOT EXISTS suborned; USE NS suborned; DEFINE DATABASE IF NOT EXISTS game;', false);
  await sql(await Bun.file(resolve(root, 'tools/rag/schema.surql')).text());
  await sql('BEGIN; DELETE corpus:current; DELETE chunk; COMMIT;');
  for (let i = 0; i < chunks.length; i += 20) await sql(`BEGIN; ${chunks.slice(i, i + 20).map((c, j) => `CREATE chunk:${c.key} CONTENT ${literal({ ...c, embedding: vectors[i + j] })};`).join('\n')} COMMIT;`);
  const count = (await sql<{ n: number }[]>('SELECT count() AS n FROM chunk GROUP ALL;'))[0]?.[0];
  if (count?.n !== chunks.length) throw new Error(`Ingested ${count?.n} of ${chunks.length} chunks; manifest withheld`);
  await sql(`UPSERT corpus:current CONTENT ${literal({ corpusHash, embeddingModel, chunks: chunks.length })};`);
  const bySource = new Map<string, { source: string; sha256: string; version: string; chunks: number }>();
  for (const c of chunks) { const e = bySource.get(c.source) ?? { source: c.source, sha256: c.sourceHash, version: c.version, chunks: 0 }; e.chunks++; bySource.set(c.source, e); }
  const manifest = { corpusHash, chunker: CHUNKER, embeddingModel, dimensions: 384, chunks: chunks.length, sources: [...bySource.values()] };
  await writeJson('out/rag/corpus-manifest.json', manifest);
  console.log(`Indexed ${chunks.length} chunks (${manifest.sources.map(s => `${s.source} ${s.chunks}`).join(', ')}); corpus ${corpusHash.slice(0, 12)}`);
  return manifest;
}
if (import.meta.main) await ingest();
