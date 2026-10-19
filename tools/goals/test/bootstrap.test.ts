import { describe, expect, test } from 'bun:test';
import { embeddingModel } from '../../rag/config';
import { hash, type Chunk } from '../../rag/corpus';
import { corpusDb } from '../../rag/database';
import { ensureRetrieval, type BootstrapDependencies } from '../bootstrap';

function fixture() {
  let chunks: Chunk[] = [{ key: 'a', source: 'GDD/test.md', sourceHash: 'source-a', section: '§01', lineStart: 1, lineEnd: 1, text: 'Rule A', authority: 'GDD', version: 'v1' }];
  let health = { database: true, embeddings: true };
  const stored = new Map<string, { corpusHash: string; embeddingModel: string; chunks: number }>();
  const calls = { starts: 0, ingests: 0 };
  const manifest = () => ({ corpusHash: hash(JSON.stringify(chunks)), embeddingModel, chunks: chunks.length });
  const deps: BootstrapDependencies = {
    endpoints: { surreal: 'http://127.0.0.1:18504', embeddings: 'http://localhost:18505' },
    corpus: async () => chunks,
    healthy: async () => health,
    manifest: async database => stored.get(database),
    start: async () => { calls.starts++; health = { database: true, embeddings: true }; },
    ingest: async () => { calls.ingests++; const result = manifest(); stored.set(corpusDb(result.corpusHash), result); return result; },
  };
  return { deps, calls, stored, manifest, setHealth: (value: typeof health) => { health = value; }, changeCorpus: () => { chunks = [{ ...chunks[0]!, text: 'Rule B', sourceHash: 'source-b' }]; } };
}

describe('retrieval bootstrap (offline)', () => {
  test('fresh checkout starts its local services and publishes an index; second run reuses it', async () => {
    const f = fixture(); f.setHealth({ database: false, embeddings: false });
    const fresh = await ensureRetrieval({}, f.deps);
    expect(fresh).toMatchObject({ indexed: true, reused: false, started: true, chunks: 1 });
    expect(fresh.database).toBe(corpusDb(fresh.corpusHash));
    const second = await ensureRetrieval({}, f.deps);
    expect(second).toMatchObject({ indexed: false, reused: true, started: false, corpusHash: fresh.corpusHash });
    expect(f.calls).toEqual({ starts: 1, ingests: 1 });
  });

  test('source changes create a new index and preserve the previous corpus', async () => {
    const f = fixture(); const first = await ensureRetrieval({}, f.deps);
    f.changeCorpus(); const second = await ensureRetrieval({}, f.deps);
    expect(second.corpusHash).not.toBe(first.corpusHash);
    expect(second.database).not.toBe(first.database);
    expect(f.stored.size).toBe(2);
    expect(f.calls.ingests).toBe(2);
  });

  test('healthy remote services ingest normally without Docker', async () => {
    const f = fixture(); f.deps.endpoints.surreal = 'http://studio.example:18504';
    expect((await ensureRetrieval({}, f.deps)).indexed).toBe(true);
    expect(f.calls.starts).toBe(0);
  });

  for (const [label, endpoints, startLocal] of [
    ['remote', { surreal: 'http://studio.example:18504', embeddings: 'http://127.0.0.1:18505' }, true],
    ['custom local ports', { surreal: 'http://localhost:8000', embeddings: 'http://localhost:8080' }, true],
    ['startup disabled', { surreal: 'http://127.0.0.1:18504', embeddings: 'http://127.0.0.1:18505' }, false],
  ] as const) test(`${label}: failure must not start Docker or ingest`, async () => {
    const f = fixture(); f.deps.endpoints = endpoints; f.setHealth({ database: false, embeddings: true });
    await expect(ensureRetrieval({ startLocal }, f.deps)).rejects.toThrow('SurrealDB unavailable');
    expect(f.calls).toEqual({ starts: 0, ingests: 0 });
  });

  test('stale embedding model or incomplete manifest requires ingestion', async () => {
    const f = fixture(), current = f.manifest(), db = corpusDb(current.corpusHash);
    f.stored.set(db, { ...current, embeddingModel: 'old-model' });
    expect((await ensureRetrieval({}, f.deps)).indexed).toBe(true);
    f.stored.set(db, { ...current, chunks: 0 });
    expect((await ensureRetrieval({}, f.deps)).indexed).toBe(true);
    expect(f.calls.ingests).toBe(2);
  });

  test('ingest success without a published manifest is not accepted', async () => {
    const f = fixture(); f.deps.ingest = async () => f.manifest();
    await expect(ensureRetrieval({}, f.deps)).rejects.toThrow('did not publish');
  });

  test('source mutation during ingestion fails instead of reporting the wrong corpus', async () => {
    const f = fixture(); const ingest = f.deps.ingest;
    f.deps.ingest = async () => { f.changeCorpus(); return ingest(); };
    await expect(ensureRetrieval({}, f.deps)).rejects.toThrow('Corpus changed');
  });

  test('startup failure preserves the cause while redacting credentials', async () => {
    const f = fixture(); f.setHealth({ database: false, embeddings: false });
    const prior = process.env.GOALS_TEST_SECRET;
    process.env.GOALS_TEST_SECRET = 'test-bootstrap-secret';
    try {
      f.deps.start = async () => { throw new Error('Docker unavailable; token test-bootstrap-secret at http://u:p@host:123'); };
      await expect(ensureRetrieval({}, f.deps)).rejects.toThrow('Docker unavailable; token [redacted] at [endpoint]');
      expect(f.calls.ingests).toBe(0);
    } finally { if (prior === undefined) delete process.env.GOALS_TEST_SECRET; else process.env.GOALS_TEST_SECRET = prior; }
  });

  test('services that remain unhealthy after startup stop before ingestion', async () => {
    const f = fixture(); f.setHealth({ database: true, embeddings: false });
    f.deps.start = async () => { f.calls.starts++; };
    await expect(ensureRetrieval({}, f.deps)).rejects.toThrow('still unavailable');
    expect(f.calls).toEqual({ starts: 1, ingests: 0 });
  });
});
