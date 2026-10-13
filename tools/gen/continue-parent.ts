// A continuation spends money on a parent: before it starts, the parent has to replay (brief 007 F retest, M2). A
// folder edited or half deleted since the run is not the state its run.json describes, and repairing from it pays for
// a round that cannot repair anything.
import { ContinueRefused } from './continue';
import type { Plugin } from './plugin';
import { replayRun } from './replay';

/** Replays the parent run offline; throws ContinueRefused, naming what failed, unless it replays OK. */
export async function replayParent(p: Plugin, planId: string, runId: string): Promise<void> {
  const r = await replayRun({ [p.id]: p }, planId, runId);
  if (r.ok) return;
  const failed = r.lines.filter(l => l.startsWith('FAIL')).map(l => l.slice(4).trim());
  const why = failed.slice(0, 3).join('; ') || 'see replay';
  throw new ContinueRefused(`cannot continue ${runId}: it does not replay (${why})`);
}
