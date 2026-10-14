// SurrealDB over HTTP: one request per batch of statements, namespace suborned, one database per corpus (corpusDb).
// Two machines or trees sharing one server each read the database of their own corpus, so neither ingest wipes the other's.
import { surrealUrl } from './config';

export const corpusDb = (corpusHash: string) => `c_${corpusHash.slice(0, 16)}`;

/** `db`: the database to run in (corpusDb), or false for namespace-level statements. */
export async function sql<T = unknown>(query: string, db: string | false): Promise<T[]> {
  const user = process.env.SURREAL_USER ?? 'suborned', password = process.env.SURREAL_PASS ?? 'local-suborned-password';
  const response = await fetch(`${surrealUrl}/sql`, {
    method: 'POST',
    headers: { Authorization: `Basic ${Buffer.from(`${user}:${password}`).toString('base64')}`, Accept: 'application/json', 'Content-Type': 'text/plain', ...(db ? { 'surreal-ns': 'suborned', 'surreal-db': db } : {}) },
    body: query, signal: AbortSignal.timeout(30000),
  });
  if (!response.ok) throw new Error(`SurrealDB HTTP ${response.status}: ${await response.text()}`);
  const statements = (await response.json()) as { status: string; result: T }[];
  const errors = [...new Set(statements.filter(s => s.status !== 'OK').map(s => JSON.stringify(s.result)))];
  if (errors.length) throw new Error(`SurrealQL: ${errors.join('; ')}`);
  return statements.map(s => s.result);
}
export const literal = (value: unknown) => JSON.stringify(value);
