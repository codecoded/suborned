// The civic look a run's world rules read (contracts/world/civic-look.json), pinned the way the block is: the engine copies it into the run folder at
// start and records its sha256 in run.json; replay checks the copy and builds the world from it. The CD rules and the civic dressing read the look,
// and Vela changes it as the colour work goes on, so a run that did not pin it would replay against whatever is on disk. A run without the pin
// (every run made before it) replays as it always did.
import { existsSync } from 'node:fs';
import { resolve } from 'node:path';
import { root } from '../rag/config';
import type { Check } from './plugin';
import { sha256 as sha } from './sha';

export const LOOK_PIN = 'input.civic-look.json';
export const LOOK_FILE = 'contracts/world/civic-look.json';
/** SUBORNED_LOOK: tests point the pin at a scratch look (as SUBORNED_KIT does for the kit). */
const source = () => process.env.SUBORNED_LOOK ?? LOOK_FILE;

/** The pinned look in a run folder, if the run has one. */
export const pinnedLook = (dir: string): string | undefined => (existsSync(`${dir}/${LOOK_PIN}`) ? `${dir}/${LOOK_PIN}` : undefined);

/** At run start: writes the look into the run folder; returns the field run.json records (none when the repo has no look). */
export async function pinLook(rdir: string): Promise<{ inputLook?: { source: string; sha256: string } }> {
  const file = resolve(root, source());
  if (!existsSync(file)) return {};
  const text = await Bun.file(file).text();
  await Bun.write(`${rdir}/${LOOK_PIN}`, text);
  return { inputLook: { source: source(), sha256: sha(text) } };
}

/** Replay: the pinned copy matches its recorded sha256. A run with no inputLook is a run made before the pin, and has no copy either. */
export async function checkLook(c: Check): Promise<void> {
  const rec = c.run.inputLook, text = await Bun.file(`${c.dir}/${LOOK_PIN}`).text().catch(() => undefined);
  if (!rec) { if (text !== undefined) c.fail(`${LOOK_PIN} is in the run folder, but run.json has no inputLook`); return; }
  if (text === undefined) c.fail(`${LOOK_PIN} is missing`);
  else if (sha(text) !== rec.sha256) c.fail(`${LOOK_PIN} does not match the recorded sha256`);
  else c.ok(`civic look ${rec.sha256.slice(0, 12)}`);
}
