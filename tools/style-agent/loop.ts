import { z } from 'zod';
import { checkConversation } from './checks';
import { defaults, roleFor, runRounds, validateLimits } from './core';
import { prompt } from './prompts';
import { Conversation, evaluationFor, Line, type Authorization, type Brief, type Evaluation, type Evidence, type Guide, type HardIssue, type Invoke,
  type Limits, type Result, type Round } from './types';

export { defaults, roleFor, validateLimits };
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
  await runRounds<Conversation, Evaluation, HardIssue, Round>({ result, limits: o.limits, invoke: o.invoke, now, started, save: () => o.checkpoint?.(result) ?? Promise.resolve(),
    // The steer shapes the first draft only; the evaluator and the reviser never see it.
    prompt: (stage, candidate, feedback) => prompt(stage, o.guide, o.brief, stage === 'generate' ? o.steer : null, candidate, feedback),
    draftSchema: conversationSchema(o.brief), evaluationSchema: evaluationFor(o.guide),
    check: draft => checkConversation(o.brief, draft, o.guide), text: draft => draft.lines.map(l => l.line).join('\n'),
    quoteError: 'The evaluator quoted words that are not in the conversation',
    round: (conversation, evaluation, hardIssues) => ({ conversation, evaluation, hardIssues }) });
  return result;
}

/** A run shows the loop working when a scored draft below 10 was revised to an accepted 10. */
export const corrected = (r: Result) => r.status === 'ACCEPTED' && r.rounds.length > 1 && r.rounds[0]!.evaluation.score < 10;
