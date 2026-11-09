// How tools/game/cli.ts starts the player for the proof and the draw check. Behind (SUBORNED_BEHIND=1, which `bun run batch --behind` sets for the steps it runs): through
// `open -g -n -W`, as the batch's own player steps do (batch.ts playerCommand): LaunchServices starts it without taking the keyboard and its window opens under the one in
// front (Aster, 2026-11-10: never force the game window to the front, Sky may be using the Studio). The proof's input checks need no focus (ProofDevices.cs). A timeout
// then stops `open`, not the player: the player is found by its -logFile path (unique to the run) and killed by PID, never by a pattern kill (CLAUDE.md).
import type { SpawnSyncOptions, SpawnSyncReturns } from 'node:child_process';

type Spawn = (cmd: string, a: string[], opts?: SpawnSyncOptions) => SpawnSyncReturns<string>;

export const behind = (): boolean => process.env.SUBORNED_BEHIND === '1';

/** The command and arguments that start `bin` (inside `app`) with `args`: the binary itself, or behind through `open -g -n -W`. */
export function launchArgs(bin: string, app: string, args: string[]): [string, string[]] {
  return behind() ? ['open', ['-g', '-n', '-W', app, '--args', ...args]] : [bin, args];
}

/** After a behind launch that timed out or failed to wait: the player still running from this launch (by its -logFile path), killed by PID. The PIDs killed. */
export function stopBehind(args: string[], r: SpawnSyncReturns<string>, spawn: Spawn): string[] {
  if (!behind() || !(r.signal || r.error)) return [];
  const i = args.indexOf('-logFile'), log = i >= 0 ? args[i + 1] : undefined; if (!log) return [];
  const pids = String(spawn('pgrep', ['-f', log], { encoding: 'utf8' }).stdout ?? '').split('\n').map(s => s.trim()).filter(s => /^\d+$/.test(s) && Number(s) !== process.pid);
  for (const pid of pids) spawn('kill', [pid], { encoding: 'utf8' });
  return pids;
}
