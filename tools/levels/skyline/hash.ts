// A hash of the code a skyline verdict rests on (recorded in run.json, so replay can tell "the checker changed" from
// "the record changed"). Transit's covers the plan checker and its apply. A skyline verdict also rests on the world
// rules, which read what the generator makes (tools/world, tools/kit/fit.ts), on the Handover rules, on the steps that
// build and judge a candidate (this objective's apply, retime, check, mission and candidate), and on the files those
// import. A test crawls the imports of the verdict code and fails when a file it reaches is not listed here.
// Tests are not code a verdict rests on.
import { readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { root } from '../../rag/config';
import { hash } from '../../rag/corpus';

const ts = (base: string, dir: string): string[] =>
  readdirSync(resolve(base, dir), { recursive: true, encoding: 'utf8' })
  .filter(f => f.endsWith('.ts') && !f.endsWith('.test.ts'))
  .sort()
  .map(f => `${dir}/${f}`);

/** The files outside tools/plan and tools/world that the verdict code reaches. */
export const VERDICT_FILES = [
  'tools/levels/apply.ts', 'tools/levels/checker.ts', 'tools/levels/timetable.ts', 'tools/kit/fit.ts',
  'tools/missions/handover/overlay.ts', 'tools/missions/handover/cast.ts', 'tools/missions/handover/visibility.ts', 'tools/missions/handover/summon.ts', 'tools/missions/check/engine.ts', 'tools/missions/sight.ts', 'tools/gen/pin-kit.ts', 'tools/gen/sha.ts',
  'tools/rag/config.ts', 'tools/rag/corpus.ts', 'tools/rag/gdd-sources.ts', 'tools/rag/corpus-html.ts', 'tools/rag/images.ts',
  'tools/rag/kit-corpus.ts', 'tools/rag/people-corpus.ts', 'tools/rag/plan-corpus.ts', 'tools/rag/plan-skyline.ts',
  ...['apply', 'candidate', 'check', 'concepts', 'known', 'mission', 'retime'].map(f => `tools/levels/skyline/${f}.ts`),
];

/** The files of the hash, under `base` (the repo root; a test points it at a copy). */
export const skylineFiles = (base: string = root): string[] => [
  ...ts(base, 'tools/plan'), ...ts(base, 'tools/world'), ...ts(base, 'tools/people'), ...ts(base, 'contracts/people'), ...VERDICT_FILES, // tools/people, contracts/people: rule M-2 (the overlay) expands the promoted cast (Redline H1)
];

export async function skylineHash(base: string = root): Promise<string> {
  const text = async (f: string) => `${f}\n${await Bun.file(resolve(base, f)).text()}`;
  const texts = await Promise.all(skylineFiles(base).map(text));
  return hash(texts.join('\n--\n'));
}
