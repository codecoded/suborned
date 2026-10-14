// Embed the corpus and load it into its own SurrealDB database. The manifest is published last, so retrieval refuses a half-loaded index.
// A corpus already indexed with this embedding model (by this machine or another sharing the server) is reused, not embedded again.
// usage: bun run ingest
import { resolve } from 'node:path';
import { root, embeddingModel, writeJson } from './config';
import { CHUNKER, hash, loadCorpus } from './corpus';
import { corpusDb, sql, literal } from './database';
import { embed } from './embeddings';

export async function ingest() {
  const chunks = await loadCorpus(), corpusHash = hash(JSON.stringify(chunks)), db = corpusDb(corpusHash);
  await sql(`DEFINE NAMESPACE IF NOT EXISTS suborned; USE NS suborned; DEFINE DATABASE IF NOT EXISTS ${db};`, false);
  await sql(await Bun.file(resolve(root, 'tools/rag/schema.surql')).text(), db);
  const have = (await sql<{ corpusHash: string; embeddingModel: string; chunks: number }[]>('SELECT * FROM corpus:current;', db))[0]?.[0];
  const reused = have?.corpusHash === corpusHash && have.embeddingModel === embeddingModel && have.chunks === chunks.length;
  if (!reused) {
    const vectors: number[][] = [];
    for (let i = 0; i < chunks.length; i += 32) vectors.push(...(await embed(chunks.slice(i, i + 32).map(c => c.text))));
    await sql('BEGIN; DELETE corpus:current; DELETE chunk; COMMIT;', db);
    for (let i = 0; i < chunks.length; i += 20) await sql(`BEGIN; ${chunks.slice(i, i + 20).map((c, j) => `CREATE chunk:${c.key} CONTENT ${literal({ ...c, embedding: vectors[i + j] })};`).join('\n')} COMMIT;`, db);
    const count = (await sql<{ n: number }[]>('SELECT count() AS n FROM chunk GROUP ALL;', db))[0]?.[0];
    if (count?.n !== chunks.length) throw new Error(`Ingested ${count?.n} of ${chunks.length} chunks; manifest withheld`);
    await sql(`UPSERT corpus:current CONTENT ${literal({ corpusHash, embeddingModel, chunks: chunks.length })};`, db);
  }
  const bySource = new Map<string, { source: string; sha256: string; version: string; chunks: number }>();
  for (const c of chunks) { const e = bySource.get(c.source) ?? { source: c.source, sha256: c.sourceHash, version: c.version, chunks: 0 }; e.chunks++; bySource.set(c.source, e); }
  const manifest = { corpusHash, chunker: CHUNKER, embeddingModel, dimensions: 384, chunks: chunks.length, sources: [...bySource.values()] };
  await writeJson('out/rag/corpus-manifest.json', manifest);
  console.log(`${reused ? 'Already indexed' : 'Indexed'} ${chunks.length} chunks (${manifest.sources.map(s => `${s.source} ${s.chunks}`).join(', ')}); corpus ${corpusHash.slice(0, 12)}, database ${db}`);
  return manifest;
}
if (import.meta.main) await ingest();
