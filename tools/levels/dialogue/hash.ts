// A hash of the code a dialogue verdict rests on (recorded in run.json, so replay can tell "the checker changed" from "the record changed"): the expander and checker
// (tools/dialogue, less its command lines and the code that reads the people), the dialogue and people contracts, the rulebook's reference code, the Handover's text
// schema, the seeded hash, this objective's own steps, and the corpus code the pinned texts are cut by. Tests are not code a verdict rests on.
import { readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { root } from '../../rag/config';
import { hash } from '../../rag/corpus';

const ts = (base: string, dir: string): string[] =>
  readdirSync(resolve(base, dir), { recursive: true, encoding: 'utf8' }).filter(f => f.endsWith('.ts') && !f.endsWith('.test.ts') && !f.startsWith('test/')).sort().map(f => `${dir}/${f}`);

export const VERDICT_FILES = [
  'tools/dialogue/check.ts', 'tools/dialogue/expand.ts', 'tools/dialogue/fill.ts', 'tools/dialogue/fixture.ts', 'tools/dialogue/rules.ts', 'tools/dialogue/situations.ts', 'tools/dialogue/texts.ts',
  'contracts/people/people.rules.schema.ts', 'contracts/people/people.schema.ts', 'contracts/rules/rulebook.ref.ts', 'contracts/rules/rulebook.schema.ts', 'contracts/missions/handover/text.schema.ts',
  'tools/gen/sha.ts', 'tools/levels/pins.ts', 'tools/levels/rerun.ts',
  'tools/rag/config.ts', 'tools/rag/corpus.ts', 'tools/rag/gdd-sources.ts', 'tools/rag/corpus-html.ts', 'tools/rag/people-corpus.ts', 'tools/rag/plan-corpus.ts', 'tools/rag/plan-skyline.ts', 'tools/rag/kit-corpus.ts',
  'tools/levels/dialogue/cast.ts', 'tools/levels/dialogue/pins.ts', 'tools/levels/dialogue/hash.ts',
];
export const dialogueFiles = (base: string = root): string[] => [...ts(base, 'contracts/dialogue'), ...VERDICT_FILES];

export async function dialogueHash(base: string = root): Promise<string> {
  const text = async (f: string) => `${f}\n${await Bun.file(resolve(base, f)).text()}`;
  return hash((await Promise.all(dialogueFiles(base).map(text))).join('\n--\n'));
}
