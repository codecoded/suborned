// What a run pinned that its world is built from: the kit digest and the civic look in its folder (each absent in a run made before the pin).
// Not verdict code: it only finds the files; the checker (checker.ts) builds the world from them.
import { pinnedKit } from '../gen/pin-kit';
import { pinnedLook } from '../gen/pin-look';
import type { Pinned } from '../plan/world-rules';

export const pinnedWorld = (dir: string): Pinned => ({ kit: pinnedKit(dir), look: pinnedLook(dir) });
