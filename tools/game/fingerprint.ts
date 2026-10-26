// What a player build is made from, by content (Splice, 2026-10-21; widened 2026-10-26, briefs/067/splice.md item 2). tools/game/cli.ts rebuilds the app only when this
// fingerprint differs from the one recorded after the last build (out/build/play/inputs.json), so a commit that only records files already built changes nothing.
//   git files    unity/Assets, ProjectSettings, Packages, the icon: read from git by blob id (cli.ts)
//   ignored      what the app carries but git does not hold, read here by sha256: the engine DLL (Assets/Suborned/Sim) and what the sim step stages for the host in
//                Assets/StreamingAssets/sim (tools/sim/unity.ts): the engine's inputs, the mission's words, the seats and the audio cues. Before 2026-10-26 only the DLL was
//                read, so a changed cue file (or site, or rulebook) left a stale app standing for `bun run game`.
//   volatile     what a build or the sim step rewrites with the time, the commit or the build number in it: engine.json, headless.json (the commit), build.json (the version).
//                Their content follows from what is read above (the DLL and the inputs), so they would only make every commit look like a change.
import { createHash } from 'node:crypto';
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { join, relative } from 'node:path';

/** What a build reads, from git (relative to the repo root). tools/game/version.json is an output of a build (its counter), so it is not here. */
export const INPUTS = ['unity/Assets', 'unity/ProjectSettings', 'unity/Packages', 'tools/game/app-icon.png'];
/** Git-ignored folders the app carries, read by content (relative to the repo root). */
export const IGNORED_INPUTS = ['unity/Assets/Suborned/Sim', 'unity/Assets/StreamingAssets'];
/** Files under those folders that change with the clock, the commit or the build number and nothing else (matched by file name). */
export const VOLATILE = new Set(['engine.json', 'engine.json.meta', 'headless.json', 'headless.json.meta', 'build.json', 'build.json.meta']);

const sha = (b: Buffer) => 'sha256:' + createHash('sha256').update(b).digest('hex');

/** The ignored inputs under `root`, as repo-relative path → sha256 (the volatile files left out). A folder that is not there reads as empty. */
export function ignoredFiles(root: string, folders: string[] = IGNORED_INPUTS): Map<string, string> {
  const out = new Map<string, string>();
  const walk = (d: string) => {
    if (!existsSync(d)) return;
    for (const e of readdirSync(d, { withFileTypes: true })) {
      const p = join(d, e.name);
      if (e.isDirectory()) walk(p); else if (!VOLATILE.has(e.name)) out.set(relative(root, p), sha(readFileSync(p)));
    }
  };
  for (const f of folders) walk(join(root, f));
  return out;
}

/** One hash of a set of files: sorted by path, one `path<TAB>id` line each. */
export function fingerprint(files: Map<string, string>): string {
  return createHash('sha256').update([...files].sort(([a], [b]) => (a < b ? -1 : 1)).map(([f, h]) => `${f}\t${h}`).join('\n')).digest('hex');
}
