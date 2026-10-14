// What a proposal may name: the concept tiles of the run's pinned index and the kit pieces of its pinned digest
// (the engine's input.kit.digest.json).
import { pinnedKit } from '../../gen/pin-kit';
import { kitStatus, type Known } from './apply';
import { tileSet, type ConceptIndex } from './concepts';

export { pinnedKit };

export async function knownFor(index: ConceptIndex, kitFile: string | undefined): Promise<Known> {
  const kit = kitFile ? kitStatus(JSON.parse(await Bun.file(kitFile).text())) : new Map<string, string>();
  return { tiles: tileSet(index), kit };
}
