// What the built app carries of what the sim step stages for the host (Splice, 2026-10-26; briefs/067/splice.md item 2). tools/sim/unity.ts copies host content into
// unity/Assets/StreamingAssets/sim/ (git-ignored), Unity bundles that folder into the app, and the host reads it from there: the audio cues (HostAudio.cs) first of all, since with no
// copy a player on another Mac plays the values copied into the code ("audio cues: defaults"). `bun run game:check` asks the app itself, not the staging folder: the file is in
// the bundle, byte for byte the contract it was copied from.
import { createHash } from 'node:crypto';
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

/** Host content the app must carry: its file under StreamingAssets/sim and the contract it is a copy of. */
export const STAGED = [{ file: 'cues.json', from: 'contracts/audio/cues.json' }] as const;
/** Where an app keeps StreamingAssets (a macOS player bundle). */
export const appStreaming = (app: string) => join(app, 'Contents', 'Resources', 'Data', 'StreamingAssets', 'sim');

const sha = (b: Buffer) => createHash('sha256').update(b).digest('hex');

export type Ships = { ok: boolean; lines: string[] };
/** Each staged file is in the app and is the contract's own bytes. `root`: the repo. */
export function appShips(app: string, root: string, staged: readonly { file: string; from: string }[] = STAGED): Ships {
  const out: Ships = { ok: true, lines: [] };
  for (const s of staged) {
    const inApp = join(appStreaming(app), s.file), contract = join(root, s.from);
    if (!existsSync(inApp)) { out.ok = false; out.lines.push(`game: FAILED: the app carries no StreamingAssets/sim/${s.file} (a player outside the repo plays the values copied into the code): run bun tools/sim/unity.ts, then build`); continue; }
    if (!existsSync(contract)) { out.ok = false; out.lines.push(`game: FAILED: ${s.from} is not there to compare the app's ${s.file} with`); continue; }
    const a = sha(readFileSync(inApp)), b = sha(readFileSync(contract));
    if (a !== b) { out.ok = false; out.lines.push(`game: FAILED: the app's StreamingAssets/sim/${s.file} (${a.slice(0, 12)}) is not ${s.from} (${b.slice(0, 12)}): the app is older than the file; run bun tools/sim/unity.ts, then build`); continue; }
    out.lines.push(`game: the app carries StreamingAssets/sim/${s.file} = ${s.from} (${a.slice(0, 12)})`);
  }
  return out;
}
