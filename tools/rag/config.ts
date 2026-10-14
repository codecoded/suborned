// Retrieval stack endpoints (compose.yaml) and repo paths.
import { resolve } from 'node:path';

export const root = resolve(import.meta.dir, '../..');
export const surrealUrl = process.env.SURREAL_URL ?? 'http://127.0.0.1:18504';
export const embeddingUrl = process.env.EMBEDDING_URL ?? 'http://127.0.0.1:18505';
/** A stack on another machine (SURREAL_URL / EMBEDDING_URL in .env, e.g. the Mac Studio over Tailscale): no local docker or ports to check. */
export const isLocal = (url: string) => ['127.0.0.1', 'localhost', '[::1]'].includes(new URL(url).hostname);
export const embeddingModel = 'BAAI/bge-small-en-v1.5';
export const writeJson = (path: string, data: unknown) => Bun.write(resolve(root, path), JSON.stringify(data, null, 2) + '\n');
