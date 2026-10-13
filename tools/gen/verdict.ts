// The schema pieces every objective's crew shares and only the owner names change: the critic's verdict, and the
// builders' hand-offs and answers. An objective passes its own owner enum (transit: Datum | Vantage | Pulse; skyline:
// Datum | Vantage). For the same owners the JSON schema is the one transit recorded;
// tools/levels/test/fixtures/transit-schemas.json holds it and a test compares.
import { z } from 'zod';

export const chunk = z.string().regex(/^[0-9a-f]{24}$/).describe('a 24-hex passage id from the retrieved passages');
export const sources = z.array(chunk).describe('passage ids the proposal rests on');
type Owners = z.ZodEnum<Record<string, string>>;

/**
 * Asks for another builder (not yourself); the engine delivers a blocking one to an owner who runs later in the round.
 */
export const handoffsOf = (owner: Owners) => z.array(z.object({
  owner,
  blocking: z.boolean().describe('true: the candidate cannot be accepted until the owner acts; false: advisory'),
  ask: z.string().min(1).max(400),
})).optional()
  .describe('asks for another builder (not yourself), e.g. a line that must move for a stop to fit; omit when none');

/** One entry per blocking hand-off addressed to the builder. */
export const answersOf = (owner: Owners) => z.array(z.object({
  from: owner, ask: z.string().min(1).max(400).describe('the hand-off ask, copied exactly'),
  done: z.boolean().describe('true only if your output now does what was asked'), note: z.string().max(300),
})).optional().describe('one entry per blocking hand-off addressed to you in the task; omit when there are none');

/**
 * Keel's verdict: each fault names its owner, the rule id (or "critic"), the fix, and at least one retrieved passage
 * that shows it.
 */
export const verdictOut = (owner: Owners) => z.object({
  verdict: z.enum(['pass', 'fail']),
  summary: z.string().max(600),
  faults: z.array(z.object({
    owner, rule: z.string().max(40).describe('plan checker rule id, or "critic" for a fault the checker cannot see'),
    fault: z.string().max(500), fix: z.string().max(400),
    chunks: z.array(chunk).min(1).describe('passage ids that show the rule being broken'),
  })).describe('empty when verdict is pass'),
  sources,
});
