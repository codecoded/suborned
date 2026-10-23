import { z } from 'zod';
import { checkConversation } from './checks';
import { hash } from './guide';
import { prompt } from './prompts';
import { Conversation, Evaluation, Line, type Authorization, type Brief, type Evidence, type Guide, type Invoke, type Limits,
  type Result, type Role, type Stage } from './types';

class Exhausted extends Error {}
export const defaults: Limits = { maxCalls: 8, maxTokens: 200000, maxRevisions: 3, maxMs: 900000 };
const CALL_MS = 180000;
export function validateLimits(limits: Limits): void {
  for (const [key, min, max] of [['maxCalls', 1, 20], ['maxTokens', 100, 400000], ['maxRevisions', 0, 3], ['maxMs', 1, 900000]] as const)
    if (!Number.isSafeInteger(limits[key]) || limits[key] < min || limits[key] > max) throw new Error(`Invalid ${key}: expected ${min}..${max}`);
}
/** Fable (dialogue designer) writes and revises; Motive (systems designer) scores, as GDD §14 Fable has it: "Motive reviews". */
export const roleFor = (stage: Stage): Role => stage === 'evaluate' ? 'systems-designer' : 'dialogue-designer';
/** The writer may only use the brief's speakers; the schema tells the model so, and the code checks it again. */
export const conversationSchema = (brief: Brief) =>
  Conversation.extend({ lines: z.array(Line.extend({ speaker: z.enum(brief.cast as [string, ...string[]]) })).min(1).max(40) });

/** No manual step and no writes to the game. Every draft is scored, including the last; only 10/10 with clean code checks is accepted. */
export async function runLoop(o: {
  guide: Guide; brief: Brief; steer: string | null; invoke: Invoke; limits: Limits; mode: Result['mode'];
  now?: () => number; checkpoint?: (result: Result) => Promise<void>; evidence?: Evidence; authorization?: Authorization;
}): Promise<Result> {
  validateLimits(o.limits);
  if (o.mode === 'live' && (!o.evidence || !o.authorization?.approvedBy.trim() || !o.authorization.model.trim()))
    throw new Error('Live runs need archived sources and a recorded approval');
  const now = o.now ?? Date.now, started = now();
  const result: Result = { schema: 'suborned.style-run/2', mode: o.mode, status: 'INCOMPLETE', stopReason: 'Not finished',
    brief: o.brief, steer: o.steer, guide: o.guide, limits: { ...o.limits }, rounds: [], calls: [],
    startedAt: started, finishedAt: null, evidence: o.evidence ?? null, authorization: o.authorization ?? null };
  const save = () => o.checkpoint?.(result) ?? Promise.resolve();
  const ask = async <T>(stage: Stage, schema: z.ZodType<T>, candidate: Conversation | null, feedback: Parameters<typeof prompt>[5]): Promise<T> => {
    if (result.calls.length >= o.limits.maxCalls) throw new Exhausted('Model call limit reached');
    if (result.calls.reduce((sum, c) => sum + (c.tokens ?? 0), 0) >= o.limits.maxTokens) throw new Exhausted('Token limit reached');
    const admittedAt = now(), remaining = o.limits.maxMs - (admittedAt - started);
    if (remaining <= 0) throw new Exhausted('Time limit reached');
    // The steer shapes the first draft only; the evaluator and the reviser never see it.
    const call: Result['calls'][number] = { stage, role: roleFor(stage), startedAt: admittedAt, finishedAt: null, tokens: null,
      prompt: prompt(stage, o.guide, o.brief, stage === 'generate' ? o.steer : null, candidate, feedback),
      timeoutMs: Math.min(CALL_MS, remaining), schemaSha256: hash(JSON.stringify(z.toJSONSchema(schema))) };
    result.calls.push(call); await save(); // an attempted call is kept even if the adapter throws
    let parsed: T;
    try {
      const answer = await o.invoke({ stage, role: call.role, prompt: call.prompt, schema, timeoutMs: call.timeoutMs });
      call.output = answer.output; call.raw = answer.raw; call.tokens = answer.tokens;
      if (answer.tokens === null || !Number.isSafeInteger(answer.tokens) || answer.tokens < 0) throw new Error('Unknown token usage; stopping');
      parsed = schema.parse(answer.output);
    } catch (error) { call.error = error instanceof Error ? error.message : String(error); throw error; }
    finally { call.finishedAt = now(); await save(); }
    if (call.finishedAt - started >= o.limits.maxMs) throw new Exhausted('Time limit reached');
    return parsed;
  };
  try {
    const schema = conversationSchema(o.brief);
    let draft: Conversation = await ask('generate', schema, null, null);
    for (let revision = 0; revision <= o.limits.maxRevisions; revision++) {
      const hardIssues = checkConversation(o.brief, draft, o.guide);
      const evaluation = await ask('evaluate', Evaluation, draft, null);
      const text = draft.lines.map(l => l.line).join('\n');
      if (evaluation.findings.some(f => f.quote && !text.includes(f.quote))) throw new Error('The evaluator quoted words that are not in the conversation');
      result.rounds.push({ conversation: draft, evaluation, hardIssues }); await save();
      if (evaluation.score === 10 && !evaluation.findings.length && !hardIssues.length) {
        result.status = 'ACCEPTED'; result.stopReason = 'Scored 10/10 with no findings, and the code checks pass'; break;
      }
      if (revision === o.limits.maxRevisions) { result.stopReason = 'Revision limit reached before a 10/10'; break; }
      draft = await ask('refine', schema, draft, { evaluation, hardIssues });
    }
  } catch (error) {
    result.status = error instanceof Exhausted ? 'INCOMPLETE' : 'ERROR';
    result.stopReason = error instanceof Error ? error.message : String(error);
  }
  result.finishedAt = now(); await save(); return result;
}

/** A run shows the loop working when a scored draft below 10 was revised to an accepted 10. */
export const corrected = (r: Result) => r.status === 'ACCEPTED' && r.rounds.length > 1 && r.rounds[0]!.evaluation.score < 10;
