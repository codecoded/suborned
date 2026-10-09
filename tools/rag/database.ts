// SurrealDB over HTTP: one request per batch of statements, namespace suborned / database game.
import { surrealUrl } from './config';

export async function sql<T = unknown>(query: string, scoped = true): Promise<T[]> {
  const user = process.env.SURREAL_USER ?? 'suborned', password = process.env.SURREAL_PASS ?? 'local-suborned-password';
  const response = await fetch(`${surrealUrl}/sql`, {
    method: 'POST',
    headers: { Authorization: `Basic ${Buffer.from(`${user}:${password}`).toString('base64')}`, Accept: 'application/json', 'Content-Type': 'text/plain', ...(scoped ? { 'surreal-ns': 'suborned', 'surreal-db': 'game' } : {}) },
    body: query, signal: AbortSignal.timeout(30000),
  });
  if (!response.ok) throw new Error(`SurrealDB HTTP ${response.status}: ${await response.text()}`);
  const statements = (await response.json()) as { status: string; result: T }[];
  const errors = [...new Set(statements.filter(s => s.status !== 'OK').map(s => JSON.stringify(s.result)))];
  if (errors.length) throw new Error(`SurrealQL: ${errors.join('; ')}`);
  return statements.map(s => s.result);
}
export const literal = (value: unknown) => JSON.stringify(value);
