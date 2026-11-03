// The walk file and the world export it was made from (Splice, 2026-10-26; briefs/067/splice.md J). `bun run geometry` writes out/world/harbour-district.walk.json from the
// export and records the export's sha256 in it (`world`); the Unity step (PlayInstall.WalkFloor) throws on a walk file made from another export, so a world run that changes the
// export (a prop gone, a bay moved) leaves the player unbuildable until geometry runs again. The batch's world step asks here and runs geometry only when it must.
import { createHash } from 'node:crypto';
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

export const WORLD_FILE = 'out/world/harbour-district.world.json', WALK_FILE = 'out/world/harbour-district.walk.json';

export type WalkState = { fresh: boolean; why: string };
/** Whether the walk file under `root` is made from the world export under `root`. */
export function walkFileState(root: string): WalkState {
  const world = join(root, WORLD_FILE), walk = join(root, WALK_FILE);
  if (!existsSync(world)) return { fresh: false, why: 'no world export' };
  if (!existsSync(walk)) return { fresh: false, why: 'no walk file' };
  let made: unknown;
  try { made = (JSON.parse(readFileSync(walk, 'utf8')) as { world?: unknown }).world; } catch { return { fresh: false, why: 'the walk file is not JSON' }; }
  const have = createHash('sha256').update(readFileSync(world)).digest('hex');
  return made === have ? { fresh: true, why: `made from world ${have.slice(0, 12)}` } : { fresh: false, why: `made from world ${typeof made === 'string' ? made.slice(0, 12) : 'nothing'}, the export is ${have.slice(0, 12)}` };
}
