import { gddNames, gddNamesFromFiles, readerFor, readerFromFiles, TEXT_FILE, type Read } from './guide';
import { barkBriefs } from './short-barks';
import { debriefBriefs } from './short-debrief';
import { labelBriefs } from './short-labels';
import { RULEBOOK } from './short-guide';
import { whyBriefs } from './short-whys';
import { SHORT_NOW, type ShortBrief, type ShortGuideVersion } from './short-types';

/** Every short-line family, a run each: the barks, the why block, the debrief page, and from guide /10 the HUD's labels. Read only from the guide's sources. */
/** `version` is the guide's: a run's families are rebuilt as they were for it. `hand`: a hand run's families (guide /18: its hand rows are items). */
export function buildShortBriefs(read: Read, names: string[], version: ShortGuideVersion = SHORT_NOW, hand = false): ShortBrief[] {
  const text = JSON.parse(read(TEXT_FILE));
  const all = [...barkBriefs(read, names, text, version, hand), ...whyBriefs(text, version), ...debriefBriefs(read, text, read(RULEBOOK), version), ...(version >= 10 ? labelBriefs(text, version) : [])];
  const ids = new Set<string>();
  for (const b of all) {
    if (ids.has(b.id)) throw new Error(`Two families share the id ${b.id}`);
    ids.add(b.id);
    const keys = new Set(b.items.map(i => i.key));
    if (keys.size !== b.items.length) throw new Error(`${b.id}: two items share a key`);
  }
  return all;
}
export const loadShortBriefs = (base: string, version: ShortGuideVersion = SHORT_NOW, hand = false) => buildShortBriefs(readerFor(base), gddNames(base), version, hand);
export const shortBriefsFromFiles = (files: Record<string, string>, shas: Record<string, string>, version: ShortGuideVersion = SHORT_NOW, hand = false) =>
  buildShortBriefs(readerFromFiles(files), gddNamesFromFiles(shas), version, hand);
