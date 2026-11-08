import { resolve } from 'node:path';
import { z } from 'zod';
import { archivedFiles, equal, readRun, requireThat, roleFiles, RUN_FILE, RUNS, STORE, toolingManifest, verifyProvider } from './evidence';
import { hash, TEXT_FILE } from './guide';
import { shortBriefsFromFiles } from './short-briefs';
import { shortGuideFromFiles } from './short-guide';
import { runShort } from './short-loop';
import { sameRubric, shortGuideVersion, ShortResultSchema, type ShortResult } from './short-types';

/** Short-line runs sit beside the conversations, in runs/style-agent/short/<time>-<family>/, and share the sources store. */
export const SHORT_RUNS = `${RUNS}/short`;
export type ShortVerification = { ok: boolean; errors: string[]; notes: string[]; status: ShortResult['status'] | null; result?: ShortResult };

/** Rebuild the rubric, the family, every prompt, check, score and stop decision from the archived sources, without a model. */
export async function verifyShort(input: unknown, base: string, store = resolve(base, STORE), runs = resolve(base, SHORT_RUNS)): Promise<ShortVerification> {
  try {
    const r = ShortResultSchema.parse(input) as ShortResult;
    requireThat(r.evidence, 'Missing archived sources');
    requireThat(r.finishedAt !== null, 'The run has no final checkpoint');
    requireThat(hash(JSON.stringify(r.evidence.tooling)) === r.evidence.toolingSha256, 'The tooling record was edited');
    const changed = toolingManifest(base).filter(t => r.evidence!.tooling.find(x => x.file === t.file)?.sha256 !== t.sha256).map(t => t.file);
    const notes = changed.length ? [`Replayed with code changed since the run: ${changed.join(', ')}`] : [];
    const files = archivedFiles(r.evidence, store), shas = Object.fromEntries(r.evidence.sources.map(s => [s.file, s.sha256]));
    const guide = shortGuideFromFiles(files, shas, shortGuideVersion(r.guide)); // a run replays with the protocol it ran with
    requireThat(equal(Object.keys(shas).sort(), [...new Set([...guide.sources.map(s => s.file), ...roleFiles(r.authorization)])].sort()), 'Unexpected or missing sources');
    requireThat(equal(guide, r.guide), 'The recorded rubric differs from its sources');
    const brief = shortBriefsFromFiles(files, shas, shortGuideVersion(r.guide)).find(b => b.id === r.brief.id);
    requireThat(brief && equal(brief, r.brief), 'The recorded family differs from its sources');
    let previous = r.startedAt;
    for (const call of r.calls) {
      requireThat(call.finishedAt !== null && call.startedAt >= previous && call.finishedAt >= call.startedAt, 'Invalid or unfinished call times');
      previous = call.finishedAt;
    }
    requireThat(r.finishedAt >= previous, 'The final time precedes a call');
    if (r.mode === 'live') verifyProvider(r, files);
    else requireThat(r.authorization === null, 'Fixtures cannot carry live approval');
    // a continuation (brief 102) starts from an earlier run's last round: that run, unaccepted and finished, on the same family, rubric and approval, ended on exactly that round
    if (r.continues) {
      const prev = ShortResultSchema.parse(readRun(resolve(runs, r.continues.run, RUN_FILE))) as ShortResult, { run: _, ...round } = r.continues;
      requireThat(prev.status !== 'ACCEPTED' && prev.finishedAt !== null && prev.finishedAt <= r.startedAt && equal(prev.brief, r.brief) && (equal(prev.guide, r.guide) || sameRubric(prev.guide, r.guide))
        && equal(prev.authorization, r.authorization) && equal(prev.rounds.at(-1), round), 'The run it continues differs from its record');
    }
    const times = [r.startedAt, ...r.calls.flatMap(c => [c.startedAt, c.finishedAt!]), r.finishedAt];
    let t = 0, index = 0;
    const rebuilt = await runShort({ guide, brief, mode: r.mode, limits: r.limits, evidence: r.evidence, continues: r.continues, base: { why: JSON.parse(files[TEXT_FILE]!).why, dialogue: JSON.parse(files[TEXT_FILE]!).dialogue },
      authorization: r.authorization ?? undefined, now: () => times[Math.min(t++, times.length - 1)]!,
      invoke: async request => {
        const call = r.calls[index++];
        requireThat(call && request.stage === call.stage && request.role === call.role && request.prompt === call.prompt && request.timeoutMs === call.timeoutMs,
          'A recorded call does not match the rebuilt stage, role, prompt or timeout');
        requireThat(hash(JSON.stringify(z.toJSONSchema(request.schema))) === call.schemaSha256, 'Recorded schema mismatch');
        if (call.output === undefined && call.error) throw new Error(call.error);
        return { output: call.output, tokens: call.tokens, raw: call.raw };
      } });
    requireThat(index === r.calls.length && equal(rebuilt, r), 'The record differs from the rebuilt loop (calls, reasons, checks, limits or status)');
    return { ok: true, errors: [], notes, status: r.status, result: r };
  } catch (error) {
    return { ok: false, errors: [error instanceof Error ? error.message : String(error)], notes: [], status: null };
  }
}
