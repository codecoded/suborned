// usage: bun run levels:replay r001 [plan-id] [--allow-drift]
// Offline: no Docker, no model, no out/. Validates a recorded run from runs/ alone (the engine, tools/gen/replay.ts, reads the run's objective and uses its hooks).
import { replayRun } from '../gen/replay';
import { plugins } from './plugins';

export const replay = (planId: string, runId: string, allowDrift = false) => replayRun(plugins, planId, runId, allowDrift);

if (import.meta.main) {
  const args = process.argv.slice(2).filter(a => !a.startsWith('--')), runId = args[0], planId = args[1] ?? 'harbour-district';
  if (!runId || !/^r\d{3}$/.test(runId)) { console.error('usage: bun run levels:replay r001 [plan-id] [--allow-drift]'); process.exit(2); }
  const { ok, lines, notCompleted } = await replay(planId, runId, process.argv.includes('--allow-drift'));
  if (notCompleted) { // a run that never finished: listed, not replayed, not a failure (exit 3 tells `bun run verify`)
    console.log(lines.join('\n') + `\nNOT COMPLETED ${planId}/${runId}: ${notCompleted}`);
    process.exit(3);
  }
  console.log(lines.join('\n') + `\n${ok ? 'REPLAY OK' : 'REPLAY FAILED'} ${planId}/${runId}${ok && lines.includes('ok    outcome REJECTED (recorded)') ? ': outcome REJECTED (recorded)' : ''}`);
  process.exit(ok ? 0 : 1);
}
