import type { z } from 'zod';
import { runRounds, validateLimits } from './core';
import { checkShort } from './short-checks';
import { shortPrompt } from './short-prompts';
import { evaluationFor, SHORT_RUN, ShortEvaluation12, shortGuideVersion, shortSetSchema, type AnyEvaluation, type ShortBrief, type ShortContinue, type ShortGuide, type ShortHand, type ShortIssue, type ShortResult, type ShortRound, type ShortSet } from './short-types';
import type { Authorization, Evidence, Invoke, Limits } from './types';

/** All the lines of a family, for the evaluator's quotes. */
export const setText = (s: ShortSet) => s.items.flatMap(i => i.fields.flatMap(f => f.lines)).join('\n');

/** A short-line family through the same loop as a conversation: Fable writes, Motive scores, Fable revises; only 10/10 with no findings
 *  and clean code checks is accepted. `base` is what the checks merge into (the why block), from the same sources as the brief. */
export async function runShort(o: {
  guide: ShortGuide; brief: ShortBrief; invoke: Invoke; limits: Limits; mode: ShortResult['mode']; base?: { why?: unknown; dialogue?: unknown };
  now?: () => number; checkpoint?: (result: ShortResult) => Promise<void>; evidence?: Evidence; authorization?: Authorization;
  /** Pick up an earlier run's last round of this family (brief 102): the first call revises its draft from its findings. */
  continues?: ShortContinue;
  /** A draft written by hand (brief 102, guide /17): scored as the first round, with no writer call. */
  hand?: ShortHand;
}): Promise<ShortResult> {
  validateLimits(o.limits);
  if (o.mode === 'live' && (!o.evidence || !o.authorization?.approvedBy.trim() || !o.authorization.model.trim()))
    throw new Error('Live runs need archived sources and a recorded approval');
  const now = o.now ?? Date.now, started = now();
  const result: ShortResult = { schema: SHORT_RUN, mode: o.mode, status: 'INCOMPLETE', stopReason: 'Not finished', brief: o.brief, guide: o.guide,
    limits: { ...o.limits }, rounds: [], calls: [], startedAt: started, finishedAt: null, evidence: o.evidence ?? null, authorization: o.authorization ?? null, ...(o.continues ? { continues: o.continues } : {}), ...(o.hand ? { hand: o.hand } : {}) };
  if (o.hand && (o.continues || shortGuideVersion(o.guide) < 17)) throw new Error('A hand-written draft is scored on its own, from guide /17');
  const evaluationSchema = evaluationFor(o.guide, o.brief.kind), perRule = evaluationSchema === ShortEvaluation12;
  await runRounds<ShortSet, AnyEvaluation, ShortIssue, ShortRound>({ result, limits: o.limits, invoke: o.invoke, now, started,
    save: () => o.checkpoint?.(result) ?? Promise.resolve(),
    prompt: (stage, candidate, feedback) => shortPrompt(stage, o.guide, o.brief, candidate, feedback),
    draftSchema: shortSetSchema(o.brief), evaluationSchema: evaluationSchema as z.ZodType<AnyEvaluation>,
    // guide /12's barks: each rule is scored on its own, and only a 10 on every one is accepted
    ...(perRule ? { accepts: (e: AnyEvaluation) => e.score === 10 && 'scores' in e && Object.values(e.scores).every(x => x === 10) } : {}),
    check: draft => checkShort(o.brief, draft, o.guide, o.base ?? {}), text: setText,
    quoteError: 'The evaluator quoted words that are not in the lines',
    round: (set, evaluation, hardIssues) => ({ set, evaluation, hardIssues }), history: shortGuideVersion(o.guide) >= 2,
    ...(o.continues ? { start: { draft: o.continues.set, feedback: { evaluation: o.continues.evaluation, hardIssues: o.continues.hardIssues } } } : {}), ...(o.hand ? { given: o.hand.set } : {}) });
  return result;
}

/** The accepted lines of a run: its last round when accepted, else nothing. */
export const accepted = (r: ShortResult): ShortSet | null => r.status === 'ACCEPTED' ? r.rounds.at(-1)!.set : null;
