import { z } from 'zod';
import { hash } from './guide';
import type { Call, Invoke, Limits, Role, Stage } from './types';

/** The loop's engine, shared by the conversations (loop.ts) and the short lines (short-loop.ts): every call admitted against the
 *  limits and kept, every draft scored, only 10/10 with no findings and clean code checks accepted. The order of calls, clock reads and
 *  checkpoints is the record's, so a saved run rebuilds byte for byte. */
export class Exhausted extends Error {}
export const defaults: Limits = { maxCalls: 8, maxTokens: 200000, maxRevisions: 3, maxMs: 900000 };
export const CALL_MS = 180000;
export function validateLimits(limits: Limits): void {
  for (const [key, min, max] of [['maxCalls', 1, 20], ['maxTokens', 100, 400000], ['maxRevisions', 0, 3], ['maxMs', 1, 900000]] as const)
    if (!Number.isSafeInteger(limits[key]) || limits[key] < min || limits[key] > max) throw new Error(`Invalid ${key}: expected ${min}..${max}`);
}
/** Fable (dialogue designer) writes and revises; Motive (systems designer) scores, as GDD §14 Fable has it: "Motive reviews". */
export const roleFor = (stage: Stage): Role => stage === 'evaluate' ? 'systems-designer' : 'dialogue-designer';

/** What the evaluator returns, whatever the run writes: a score and findings that quote the draft. */
type Scored = { score: number; findings: { quote: string }[] };
/** What a run records, whatever it writes: the parts the engine fills. */
export type Record_<R> = { status: 'ACCEPTED' | 'INCOMPLETE' | 'ERROR'; stopReason: string; rounds: R[]; calls: Call[]; finishedAt: number | null; limits: Limits };

export async function runRounds<C, E extends Scored, H, R>(o: {
  result: Record_<R>; limits: Limits; invoke: Invoke; now: () => number; started: number; save: () => Promise<void>;
  /** The prompt each stage sees: the draft and the reasons are passed only where the stage takes them. */
  prompt: (stage: Stage, candidate: C | null, feedback: { evaluation: E; hardIssues: H[] } | null) => string;
  draftSchema: z.ZodType<C>; evaluationSchema: z.ZodType<E>;
  check: (candidate: C) => H[]; text: (candidate: C) => string; quoteError: string;
  round: (candidate: C, evaluation: E, hardIssues: H[]) => R;
  /** The evaluator also sees its own last score and findings, so it holds the writer to them and doesn't mark down a fix it asked for. */
  history?: boolean;
  /** When a score accepts, if not a plain 10 (guide /12's barks: a 10 on every rule); findings and code checks must still be empty. */
  accepts?: (evaluation: E) => boolean;
  /** A run that continues another's last round (brief 102): its first call revises that draft from that round's findings, which the evaluator then sees as its own last. */
  start?: { draft: C; feedback: { evaluation: E; hardIssues: H[] } };
}): Promise<void> {
  const { result, limits, now, started, save } = o;
  const ask = async <T>(stage: Stage, schema: z.ZodType<T>, candidate: C | null, feedback: { evaluation: E; hardIssues: H[] } | null): Promise<T> => {
    if (result.calls.length >= limits.maxCalls) throw new Exhausted('Model call limit reached');
    if (result.calls.reduce((sum, c) => sum + (c.tokens ?? 0), 0) >= limits.maxTokens) throw new Exhausted('Token limit reached');
    const admittedAt = now(), remaining = limits.maxMs - (admittedAt - started);
    if (remaining <= 0) throw new Exhausted('Time limit reached');
    const call: Call = { stage, role: roleFor(stage), startedAt: admittedAt, finishedAt: null, tokens: null,
      prompt: o.prompt(stage, candidate, feedback), timeoutMs: Math.min(CALL_MS, remaining), schemaSha256: hash(JSON.stringify(z.toJSONSchema(schema))) };
    result.calls.push(call); await save(); // an attempted call is kept even if the adapter throws
    let parsed: T;
    try {
      const answer = await o.invoke({ stage, role: call.role, prompt: call.prompt, schema, timeoutMs: call.timeoutMs });
      call.output = answer.output; call.raw = answer.raw; call.tokens = answer.tokens;
      if (answer.tokens === null || !Number.isSafeInteger(answer.tokens) || answer.tokens < 0) throw new Error('Unknown token usage; stopping');
      parsed = schema.parse(answer.output);
    } catch (error) { call.error = error instanceof Error ? error.message : String(error); throw error; }
    finally { call.finishedAt = now(); await save(); }
    if (call.finishedAt - started >= limits.maxMs) throw new Exhausted('Time limit reached');
    return parsed;
  };
  try {
    let draft: C = o.start ? await ask('refine', o.draftSchema, o.start.draft, o.start.feedback) : await ask('generate', o.draftSchema, null, null);
    let last: { evaluation: E; hardIssues: H[] } | null = o.start?.feedback ?? null;
    for (let revision = 0; revision <= limits.maxRevisions; revision++) {
      const hardIssues = o.check(draft);
      const evaluation: E = await ask<E>('evaluate', o.evaluationSchema, draft, o.history ? last : null);
      last = { evaluation, hardIssues };
      const text = o.text(draft);
      if (evaluation.findings.some(f => f.quote && !text.includes(f.quote))) throw new Error(o.quoteError);
      result.rounds.push(o.round(draft, evaluation, hardIssues)); await save();
      if ((o.accepts ? o.accepts(evaluation) : evaluation.score === 10) && !evaluation.findings.length && !hardIssues.length) {
        result.status = 'ACCEPTED'; result.stopReason = 'Scored 10/10 with no findings, and the code checks pass'; break;
      }
      if (revision === limits.maxRevisions) { result.stopReason = 'Revision limit reached before a 10/10'; break; }
      draft = await ask('refine', o.draftSchema, draft, { evaluation, hardIssues });
    }
  } catch (error) {
    result.status = error instanceof Exhausted ? 'INCOMPLETE' : 'ERROR';
    result.stopReason = error instanceof Error ? error.message : String(error);
  }
  result.finishedAt = now(); await save();
}
