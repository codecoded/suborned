// What a proposal may name: the concept tiles of the run's pinned index and the kit pieces of its pinned digest
// (the engine's input.kit.digest.json).
import { existsSync } from 'node:fs';
import { KIT_PIN } from '../../gen/pin-kit';
import { kitStatus, type Known } from './apply';
import { tileSet, type ConceptIndex } from './concepts';

/** The pinned kit digest in a run folder, if the run has one (a repo without a kit pins none). */
export const pinnedKit = (dir: string): string | undefined =>
  (existsSync(`${dir}/${KIT_PIN}`) ? `${dir}/${KIT_PIN}` : undefined);

export async function knownFor(index: ConceptIndex, kitFile: string | undefined): Promise<Known> {
  const kit = kitFile ? kitStatus(JSON.parse(await Bun.file(kitFile).text())) : new Map<string, string>();
  return { tiles: tileSet(index), kit };
}
