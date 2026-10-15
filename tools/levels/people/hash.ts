// A hash of the code a people verdict rests on (recorded in run.json, so replay can tell "the checker changed" from "the record
// changed"): the expander and checker (tools/people), the people contracts, the plan checker's walk and geometry (tools/plan),
// the seeded random source, this objective's own steps, and the corpus code the pinned texts are cut by. Tests are not code a
// verdict rests on.
import { readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { root } from '../../rag/config';
import { hash } from '../../rag/corpus';

const ts = (base: string, dir: string, deep = true): string[] =>
  readdirSync(resolve(base, dir), { recursive: deep, encoding: 'utf8' })
  .filter(f => f.endsWith('.ts') && !f.endsWith('.test.ts') && !f.startsWith('test/'))
  .sort()
  .map(f => `${dir}/${f}`);

export const VERDICT_FILES = [
  'tools/gen/sha.ts', 'tools/levels/pins.ts', 'tools/levels/rerun.ts',
  'tools/rag/config.ts', 'tools/rag/corpus.ts', 'tools/rag/corpus-html.ts', 'tools/rag/people-corpus.ts', 'tools/rag/plan-corpus.ts',
  'tools/rag/plan-skyline.ts', 'tools/rag/kit-corpus.ts',
  'tools/levels/people/cast.ts', 'tools/levels/people/pins.ts', 'tools/missions/handover/cast.ts',
  'tools/missions/handover/overlay.ts', // `bun run people` reports M-2 on the people it makes through the overlay's own check (a candidate cast is held to the Handover before it is promoted)
  'tools/levels/people/hash.ts', // the list above is part of what a hash means (and `bun run people` records the hash in the file it writes)
  'tools/world/civic/cafe.ts', // where the world draws the café sets: seats.ts places the chairs from it
  'tools/missions/handover/visibility.ts', 'tools/missions/sight.ts', // the overlay's sight rules (Vantage): visibility.ts draws the world's masses, so the world generator is part of what a people verdict reaches (below)
  'tools/kit/fit.ts',
];
export const peopleFiles = (base: string = root): string[] => [
  ...ts(base, 'tools/people'), ...ts(base, 'tools/plan'), ...ts(base, 'contracts/people'), ...ts(base, 'tools/world/gen'), ...ts(base, 'tools/world/civic'), 'tools/world/build.ts', ...VERDICT_FILES,
];

export async function peopleHash(base: string = root): Promise<string> {
  const text = async (f: string) => `${f}\n${await Bun.file(resolve(base, f)).text()}`;
  return hash((await Promise.all(peopleFiles(base).map(text))).join('\n--\n'));
}
