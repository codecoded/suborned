// The massing grammar (contracts/world/harbour-district.grammar.md, Vela) is a pinned input of the skyline objective:
// crown rules and the sky rail portal standard cite it. A run copies the file into its folder and records the sha256;
// Datum is shown the sections that bear on the skyline (4 towers, 6 roofs and crowns, 8 sky rail through buildings)
// from the copy. Replay checks the copy against the record.
import { resolve } from 'node:path';
import { root } from '../../rag/config';
import type { Check } from '../../gen/plugin';
import { sha256 } from '../../gen/sha';

export const GRAMMAR_FILE = 'contracts/world/harbour-district.grammar.md';
export const GRAMMAR_PIN = 'input.grammar.md';
export const GRAMMAR_SECTIONS = ['4', '6', '8'];

export type GrammarRecord = { source: string; sha256: string };

/** At run start: copies the grammar into the run folder; returns its text and what run.json records. */
export async function pinGrammar(rdir: string, file = GRAMMAR_FILE): Promise<{ text: string; record: GrammarRecord }> {
  const text = await Bun.file(resolve(root, file)).text();
  await Bun.write(`${rdir}/${GRAMMAR_PIN}`, text);
  return { text, record: { source: file, sha256: sha256(text) } };
}

/** Replay: the pinned copy matches its sha256. */
export async function checkGrammar(c: Check): Promise<string | undefined> {
  const rec: GrammarRecord | undefined = c.run.inputGrammar;
  if (!rec) { c.fail('run.json has no inputGrammar'); return undefined; }
  const text = await Bun.file(`${c.dir}/${GRAMMAR_PIN}`).text().catch(() => undefined);
  if (text === undefined) { c.fail(`${GRAMMAR_PIN} is missing`); return undefined; }
  if (sha256(text) !== rec.sha256) { c.fail(`${GRAMMAR_PIN} does not match the recorded sha256`); return undefined; }
  c.ok(`grammar ${rec.sha256.slice(0, 12)}`);
  return text;
}

/** The numbered sections ("## 6. ...") of the grammar, as written, in the order asked. */
export function sectionsOf(text: string, numbers: string[] = GRAMMAR_SECTIONS): string {
  const parts = text.split(/^(?=## )/m);
  return numbers
    .flatMap(n => parts.filter(p => new RegExp(`^## ${n}[.\\s]`).test(p)))
    .map(p => p.trimEnd())
    .join('\n\n');
}
