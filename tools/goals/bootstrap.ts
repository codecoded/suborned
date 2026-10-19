// A fresh checkout rebuilds its retrieval index from source; no database dump is needed.
import { root, surrealUrl, embeddingUrl, embeddingModel, isLocal } from '../rag/config';
import { hash, loadCorpus } from '../rag/corpus';
import { corpusDb, sql } from '../rag/database';
import { ingest } from '../rag/ingest';

type Manifest = { corpusHash: string; embeddingModel: string; chunks: number };
export type BootstrapReport = Manifest & { database: string; indexed: boolean; reused: boolean; started: boolean };
export type BootstrapDependencies = {
  endpoints: { surreal: string; embeddings: string };
  corpus: typeof loadCorpus;
  healthy: () => Promise<{ database: boolean; embeddings: boolean }>;
  manifest: (database: string) => Promise<Manifest | undefined>;
  ingest: () => Promise<Manifest>;
  start: () => Promise<void>;
};

// Failure evidence can be retained by the coordinator without copying credentials into a run.
function safeError(error: unknown): string {
  let message = error instanceof Error ? error.message : String(error);
  const secrets = Object.entries(process.env).filter(([key, value]) => /PASS|SECRET|TOKEN|CREDENTIAL|API_KEY|URL/i.test(key) && value && value.length > 3).map(([, value]) => value!);
  for (const secret of secrets.sort((a, b) => b.length - a.length)) message = message.split(secret).join('[redacted]');
  return message.replace(/https?:\/\/[^\s]+/g, '[endpoint]').slice(-3000);
}

async function docker(args: string[], timeoutMs: number): Promise<string> {
  const child = Bun.spawn(['docker', ...args], { cwd: root, stdout: 'pipe', stderr: 'pipe' });
  let timedOut = false;
  const timer = setTimeout(() => { timedOut = true; child.kill('SIGKILL'); }, timeoutMs);
  try {
    const [code, stdout, stderr] = await Promise.all([child.exited, new Response(child.stdout).text(), new Response(child.stderr).text()]);
    if (timedOut) throw new Error(`Docker startup timed out after ${timeoutMs / 1000}s. ${stderr.slice(-2000)}`);
    if (code !== 0) throw new Error(`Docker exited ${code}: ${stderr.slice(-2000)}`);
    return stdout;
  } finally { clearTimeout(timer); }
}

const defaults: BootstrapDependencies = {
  endpoints: { surreal: surrealUrl, embeddings: embeddingUrl },
  corpus: loadCorpus,
  healthy: async () => {
    const [database, embeddings] = await Promise.all([
      sql('RETURN 1;', false).then(() => true, () => false),
      fetch(`${embeddingUrl}/health`, { signal: AbortSignal.timeout(5000) }).then(async response => {
        if (!response.ok) return false;
        const health = await response.json() as { model?: string; dimensions?: number };
        return health.model === embeddingModel && health.dimensions === 384;
      }).catch(() => false),
    ]);
    return { database, embeddings };
  },
  manifest: async database => (await sql<Manifest[]>('SELECT * FROM corpus:current;', database))[0]?.[0],
  ingest,
  start: async () => {
    // The Compose project is shared across worktrees. Never recreate a running Studio stack
    // just because this checkout has no .env and is probing the default loopback endpoints.
    const running = await docker(['ps', '--filter', 'label=com.docker.compose.project=suborned', '--format', '{{.Names}}'], 15000);
    if (running.trim()) throw new Error('A suborned Compose stack is already running. Check SURREAL_URL / EMBEDDING_URL and its health with bun run doctor; bootstrap will not reconfigure it.');
    await docker(['compose', 'up', '-d', '--build', '--wait', '--wait-timeout', '240'], 300000);
  },
};

function defaultLocalEndpoint(endpoint: string, port: string): boolean {
  const url = new URL(endpoint);
  return isLocal(endpoint) && url.protocol === 'http:' && url.port === port && (url.pathname === '' || url.pathname === '/') && !url.search && !url.username && !url.password;
}

/** Startup is bounded; ingestion uses the existing RAG batching/timeouts and publishes its manifest last. */
export async function ensureRetrieval(options: { startLocal?: boolean } = {}, deps: BootstrapDependencies = defaults): Promise<BootstrapReport> {
  try {
    const chunks = await deps.corpus(), corpusHash = hash(JSON.stringify(chunks)), database = corpusDb(corpusHash);
    let health = await deps.healthy(), started = false;
    if (!health.database || !health.embeddings) {
      const missing = [!health.database && 'SurrealDB', !health.embeddings && 'embeddings'].filter(Boolean).join(' and ');
      const canStart = defaultLocalEndpoint(deps.endpoints.surreal, '18504') && defaultLocalEndpoint(deps.endpoints.embeddings, '18505');
      if (options.startLocal === false || !canStart) throw new Error(`${missing} unavailable or incompatible. Check configured services with bun run doctor; automatic Docker startup is limited to the default local endpoints.`);
      await deps.start();
      started = true;
      health = await deps.healthy();
      if (!health.database || !health.embeddings) throw new Error('Retrieval services are still unavailable or incompatible after startup. Run bun run doctor.');
    }
    // A fresh database need not exist yet. ingest creates it; other SQL errors will surface there.
    const existing = await deps.manifest(database).catch(() => undefined);
    const reused = existing?.corpusHash === corpusHash && existing.embeddingModel === embeddingModel && existing.chunks === chunks.length;
    if (!reused) {
      const result = await deps.ingest();
      if (result.corpusHash !== corpusHash || result.chunks !== chunks.length || result.embeddingModel !== embeddingModel)
        throw new Error('Corpus changed during bootstrap or ingestion returned an incompatible manifest. Retry against a stable checkout.');
      const published = await deps.manifest(database);
      if (published?.corpusHash !== corpusHash || published.embeddingModel !== embeddingModel || published.chunks !== chunks.length)
        throw new Error('Ingestion did not publish a complete matching corpus manifest.');
    }
    return { corpusHash, database, chunks: chunks.length, embeddingModel, indexed: !reused, reused, started };
  } catch (error) { throw new Error(`Retrieval bootstrap failed: ${safeError(error)}`); }
}
