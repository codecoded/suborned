// The batch's folders and the helpers its parts share (Splice, 2026-11-11: out of batch.ts, which is over 500 lines; batch-sky.ts and batch-player.ts read them too).
import { existsSync, readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { ROOT } from './shots';

export const UNITY = join(ROOT, 'unity');
/** SUBORNED_BATCH_OUT: the tests' scratch folder, and the behind batch's own (out/batch/full-behind, batch-sky.ts). */
export const OUT = process.env.SUBORNED_BATCH_OUT ? resolve(process.env.SUBORNED_BATCH_OUT) : join(ROOT, 'out/batch');
/** Sky's player: what `bun run batch --sky` builds and what Sky plays. */
export const PLAY_DIR = join(ROOT, 'out/build/play');
/** SUBORNED_PLAYER_DIR: the player this batch builds and proves. Unset: Sky's (out/build/play). The behind batch (`--sky --then-full`) sets out/build/full: its build is a
 *  scratch build (GameBuild.CliScratch through tools/game/cli.ts, SUBORNED_GAME_SCRATCH): no build number spent, and never the app Sky is about to run. */
export const BUILD_DIR = process.env.SUBORNED_PLAYER_DIR ? resolve(ROOT, process.env.SUBORNED_PLAYER_DIR) : PLAY_DIR;
export const OWN_PLAYER = BUILD_DIR !== PLAY_DIR;
export const APP = join(BUILD_DIR, 'The Suborned.app'), PROOF = join(BUILD_DIR, 'proof/proof.json'), STAMP = join(BUILD_DIR, 'build.json');
/** The children's environment: dotnet quiet; with its own player folder, cli.ts builds and proves there (SUBORNED_GAME_OUT) as a scratch build (SUBORNED_GAME_SCRATCH). */
export const env: Record<string, string | undefined> = {
  ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1', ...(process.env.DOTNET_ROOT || !existsSync('/opt/homebrew/opt/dotnet/libexec') ? {} : { DOTNET_ROOT: '/opt/homebrew/opt/dotnet/libexec' }),
  ...(OWN_PLAYER ? { SUBORNED_GAME_OUT: BUILD_DIR, SUBORNED_GAME_SCRATCH: '1' } : {}),
};
/** The gated steps that read what the kit and world steps write (Kit/**, kit.json as re-catalogued, the export): gated on a fresh snapshot once either ran (batch.ts `gated`). */
export const AFTER_WORLD = ['unity-world', 'unity-match', 'late-fight', 'tp-long'];
export const rel = (p: string) => p.startsWith(ROOT + '/') ? p.slice(ROOT.length + 1) : p;
export const readJson = (f: string): any => { try { return JSON.parse(readFileSync(f, 'utf8')); } catch { return undefined; } };
export function unityExe(): string {
  const ver = readFileSync(join(UNITY, 'ProjectSettings/ProjectVersion.txt'), 'utf8').match(/m_EditorVersion: (\S+)/)?.[1];
  return `/Applications/Unity/Hub/Editor/${ver}/Unity.app/Contents/MacOS/Unity`;
}
