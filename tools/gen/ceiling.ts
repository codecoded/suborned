// The spend ceiling of a run (`--max-usd`): the call about to be made is priced from its prompt (estimate.ts), and
// when the run's spend so far plus that would pass the ceiling, the call is not made. The run ends INCOMPLETE and
// can be continued.
import type { z } from 'zod';
import type { Call } from '../rag/agent';
import { cliSchema, invokeClaude, type Card } from '../rag/claude';
import type { LoadedImage } from '../rag/images';
import { estimateCost } from './estimate';

export const DEFAULT_MAX_USD = 10;

/** The next call would take the run past its ceiling. */
export class SpendCeiling extends Error {
  constructor(readonly maxUsd: number, readonly spentUsd: number, readonly nextUsd: number) {
    const spent = `$${spentUsd.toFixed(2)} spent, the next call is estimated at $${nextUsd.toFixed(2)}`;
    super(`spend ceiling $${maxUsd}: ${spent}`);
  }
}

/** What a run has spent so far, from the calls it recorded. */
export const spentUsd = (calls: Call[]): number => calls.reduce((t, c) => t + (c.costUsd ?? 0), 0);

/** `invokeClaude` that first checks the ceiling. `builder`: a builder's call (long answer) rather than the critic's. */
export function withCeiling(calls: Call[], maxUsd: number, builder: boolean): typeof invokeClaude {
  return async (card: Card, prompt: string, schema: z.ZodType, effort?: string, images: LoadedImage[] = []) => {
    const chars = prompt.length + card.body.length + cliSchema(schema).length;
    const next = estimateCost({ chars, images: images.length, builder }).usd, spent = spentUsd(calls);
    if (spent + next > maxUsd) throw new SpendCeiling(maxUsd, spent, next);
    return invokeClaude(card, prompt, schema, effort, images);
  };
}
