// The kit digest a run's corpus was made from (chunker v3). The engine pins it for every run, whatever the objective: kit.json changes whenever
// Vela adopts a piece, and without the pin that edit would change the corpus hash of every earlier run. Replay rebuilds the corpus from the pin.
import { resolve } from 'node:path';
import { root } from '../rag/config';
import { sources } from '../rag/corpus';
import { kitDigest } from '../rag/kit-corpus';
import type { Check } from './plugin';
import { sha256 as sha } from './sha';

export const KIT_PIN = 'input.kit.digest.json';

/** At run start: writes the digest into the run folder; returns the fields run.json records (none when the repo has no kit). */
export async function pinKit(rdir: string): Promise<{ inputKit?: { source: string; sourceSha256: string; sha256: string; assets: number } }> {
  const kit = sources.kit();
  if (!kit) return {};
  const raw = await Bun.file(resolve(root, kit)).text(), digest = kitDigest(raw);
  await Bun.write(`${rdir}/${KIT_PIN}`, digest);
  return { inputKit: { source: kit, sourceSha256: sha(raw), sha256: sha(digest), assets: JSON.parse(digest).assets.length } };
}

/** Replay: the pinned digest matches its sha256; returns the corpus override for the kit ({} for a run recorded before the corpus held the kit). */
export async function checkKit(c: Check, chunker: number): Promise<Record<string, string>> {
  const rec = c.run.inputKit;
  if (!rec) {
    if (chunker >= 3) c.fail('run.json has no inputKit, but a chunker v3 run pins the kit digest');
    return {};
  }
  const text = await Bun.file(`${c.dir}/${KIT_PIN}`).text().catch(() => undefined);
  if (text === undefined) { c.fail(`${KIT_PIN} is missing`); return {}; }
  if (sha(text) !== rec.sha256) { c.fail(`${KIT_PIN} does not match the recorded sha256`); return {}; }
  c.ok(`kit digest ${rec.sha256.slice(0, 12)} (${rec.assets} pieces)`);
  return { [rec.source]: text };
}
