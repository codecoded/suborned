// "Sky is playing" guard (Splice, 2026-10-22; briefs/048-draw-distance.md, Perf: crew load tests and player batches ran on the Studio while Sky played, 20:15 and 20:22).
// Nothing heavy runs on the Studio while Sky has the game open: `bun run batch` (at the start and before each step that starts a player), the late-fight and tp-long rows,
// and any load test or heavy sweep started through `bun run guard -- <command>`. The rule is in CLAUDE.md (Rules).
//
// How it knows: a player process of The Suborned (any copy: out/build/play, a Finder or `bun run game` launch) whose flags are only the ones a person's launch carries
// (-windowed, -screen-*, -popupwindow, the macOS -psn_ / -NS / -Apple ones). Any other flag (-gameProof, -drawCheck, -late, -tplong, -shots, -runScenarios, -redline048,
// -batchmode, -nographics, -capture-size, -logFile ...) is automation: a proof, a batch row, a probe. `bun run game` opens the app with `open`, which returns at once, so
// there is no process of ours to hold a marker file; the process list is the one truth. A player that Sky starts with `-windowed` counts the same.
//
// Waiting: prints "Sky is playing; waiting" (and the pids), polls every few seconds, goes on when the game closes. After the timeout (default 20 min) it gives up with
// exit 3 (the step fails, or the guarded command does not run). `--force` (or SUBORNED_FORCE_PLAYING=1) skips the wait: only when Sky has said so.
//
// usage: bun tools/game/playing.ts [--check]                          exit 0 when nobody is playing, 1 when Sky is (and says who)
//        bun tools/game/playing.ts [--force] [--wait-timeout s] <command...>   wait while Sky plays, then run the command (a CPU burner, a sweep) and exit with its code
//        bun run guard <command...>                                    the same
import { spawn } from 'node:child_process';

export type Player = { pid: number; args: string[] };
/** Flags a person's launch of the game carries; value words after them are not flags. */
export const PERSON_FLAGS: RegExp[] = [/^-windowed$/, /^-popupwindow$/, /^-screen-/, /^-window-mode$/, /^-monitor$/, /^-psn_/, /^-NS/, /^-Apple/, /^-ApplePersistenceIgnoreState$/];
const PLAYER = '/Contents/MacOS/The Suborned';

/** The players in `ps -Ao pid=,command=` output that are a person's: running The Suborned, no flag but a person's. */
export function parsePlaying(ps: string): Player[] {
  const out: Player[] = [];
  for (const line of ps.split('\n')) {
    const m = line.match(/^\s*(\d+)\s+(.*)$/); if (!m) continue;
    const cmd = m[2]!, at = cmd.indexOf(PLAYER); if (at < 0) continue;
    const rest = cmd.slice(at + PLAYER.length);
    if (rest !== '' && !/^\s/.test(rest)) continue; // "The Suborned Helper" and the like
    const args = rest.trim() === '' ? [] : rest.trim().split(/\s+/);
    if (args.some(a => a.startsWith('-') && !PERSON_FLAGS.some(r => r.test(a)))) continue;
    out.push({ pid: Number(m[1]), args });
  }
  return out.filter(p => p.pid !== process.pid);
}

export const psNow = (): string => Bun.spawnSync(['ps', '-Ao', 'pid=,command='], { stdout: 'pipe', stderr: 'ignore' }).stdout.toString();
/** Who is playing right now (empty: nobody). */
export const whoIsPlaying = (ps: () => string = psNow): Player[] => parsePlaying(ps());

export type GuardConfig = { force: boolean; timeoutS: number; pollMs: number };
export const DEFAULT_TIMEOUT_S = 1200;
export const guardConfig: GuardConfig = { force: process.env.SUBORNED_FORCE_PLAYING === '1', timeoutS: DEFAULT_TIMEOUT_S, pollMs: 5000 };

/** Takes `--force` and `--wait-timeout <s>` out of `args` (in place) into the guard's settings. Throws on a bad timeout. */
export function takeGuardFlags(args: string[], cfg: GuardConfig = guardConfig): void {
  const f = args.indexOf('--force'); if (f >= 0) { args.splice(f, 1); cfg.force = true; }
  const t = args.indexOf('--wait-timeout');
  if (t >= 0) {
    const v = Number(args[t + 1]); if (!Number.isFinite(v) || v < 0) throw new Error('--wait-timeout takes seconds (0 = do not wait)');
    args.splice(t, 2); cfg.timeoutS = v;
  }
}

export type Waited = { playing: boolean; waitedS: number; timedOut: boolean; forced: boolean; players: Player[] };
export type WaitOpts = { cfg?: GuardConfig; ps?: () => string; say?: (m: string) => void; sleep?: (ms: number) => Promise<void>; now?: () => number };
export const MESSAGE = 'Sky is playing; waiting';

/** Returns when nobody is playing (at once when nobody is). Prints "Sky is playing; waiting" on the first look and once a minute after. `timedOut` when the timeout passed with Sky still in the game. */
export async function waitForSky(label: string, o: WaitOpts = {}): Promise<Waited> {
  const cfg = o.cfg ?? guardConfig, say = o.say ?? (m => console.error(m)), sleep = o.sleep ?? (ms => new Promise<void>(r => setTimeout(r, ms))), now = o.now ?? Date.now;
  let players = whoIsPlaying(o.ps);
  if (!players.length) return { playing: false, waitedS: 0, timedOut: false, forced: false, players };
  if (cfg.force) { say(`${label}: Sky is playing (pid ${players.map(p => p.pid).join(', ')}); --force, going on`); return { playing: true, waitedS: 0, timedOut: false, forced: true, players }; }
  const t0 = now(); let lastSay = -Infinity;
  while (players.length) {
    const waited = (now() - t0) / 1000;
    if (waited >= cfg.timeoutS) { say(`${label}: still playing after ${Math.round(waited)} s; giving up (--force to go on anyway)`); return { playing: true, waitedS: waited, timedOut: true, forced: false, players }; }
    if (waited - lastSay >= 60 || lastSay === -Infinity) { say(`${label}: ${MESSAGE} (pid ${players.map(p => p.pid).join(', ')}${waited ? `, ${Math.round(waited)} s` : ''}; times out at ${cfg.timeoutS} s; --force skips)`); lastSay = waited; }
    await sleep(Math.min(cfg.pollMs, Math.max(0, (cfg.timeoutS - waited) * 1000)) || cfg.pollMs);
    players = whoIsPlaying(o.ps);
  }
  const waitedS = (now() - t0) / 1000;
  say(`${label}: the game is closed; going on after ${Math.round(waitedS)} s`);
  return { playing: true, waitedS, timedOut: false, forced: false, players: [] };
}

/** The line a failed step gives. */
export const timeoutText = (w: Waited): string => `Sky was playing (pid ${w.players.map(p => p.pid).join(', ')}) for the whole ${Math.round(w.waitedS)} s wait; nothing heavy runs while he plays. Run again later, or --force if he has said to.`;

if (import.meta.main) {
  const args = process.argv.slice(2);
  try { takeGuardFlags(args); } catch (e) { console.error((e as Error).message); process.exit(2); }
  if (args[0] === '--') args.shift();
  if (args.length === 0 || args[0] === '--check') {
    const p = whoIsPlaying();
    console.log(p.length ? `Sky is playing (pid ${p.map(x => x.pid).join(', ')})` : 'nobody is playing');
    process.exit(p.length ? 1 : 0);
  }
  const w = await waitForSky(args[0]!);
  if (w.timedOut) { console.error(timeoutText(w)); process.exit(3); }
  const child = spawn(args[0]!, args.slice(1), { stdio: 'inherit' });
  child.on('error', e => { console.error(`${args[0]}: ${e.message}`); process.exit(127); });
  child.on('exit', (code, sig) => process.exit(code ?? (sig ? 128 : 1)));
}
