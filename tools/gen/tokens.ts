// Token counts of a call as run.json records them. An optional field of a run's `calls`: runs made before it have none,
// and replay accepts both.
import { z } from 'zod';

const n = z.number().int().nonnegative();
export const Tokens = z.object({ input: n, cacheRead: n, output: n });
export type Tokens = z.infer<typeof Tokens>;

const count = (u: Record<string, unknown>, key: string) => (typeof u[key] === 'number' ? (u[key] as number) : 0);

/**
 * `input` = fresh input (prompt tokens not read from cache), `cacheRead` = prompt tokens read from cache, `output` =
 * tokens written.
 */
export function tokensOf(usage: unknown): Tokens | undefined {
  if (!usage || typeof usage !== 'object') return undefined;
  const u = usage as Record<string, unknown>;
  const tokens = {
    input: count(u, 'input_tokens') + count(u, 'cache_creation_input_tokens'),
    cacheRead: count(u, 'cache_read_input_tokens'), output: count(u, 'output_tokens'),
  };
  return tokens.input + tokens.cacheRead + tokens.output > 0 ? tokens : undefined;
}
