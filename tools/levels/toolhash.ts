// A hash of the code that decides a plan's fate, recorded in run.json so replay can say "the checker changed" apart from "the record changed". A transit
// verdict rests on the plan checker and its apply step, and, since the world rules run on a transit candidate (brief 007 F review M1), on the checker's plumbing
// and on the world generator they read (tools/world, tools/kit/fit.ts): that code moves every day, so transit's code `moves` like skyline's (a rerun that
// still matches is labelled drift; a rule whose result differs needs a declared cause). Tests are not code a verdict rests on.
import { readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { hash } from '../rag/corpus';
import { root } from '../rag/config';

const ts = (base: string, dir: string): string[] =>
  readdirSync(resolve(base, dir), { recursive: true, encoding: 'utf8' }).filter(f => f.endsWith('.ts') && !f.endsWith('.test.ts')).sort().map(f => `${dir}/${f}`);

/** The files of the hash, under `base` (the repo root; a test points it at a copy). */
export const transitFiles = (base: string = root): string[] => [
  ...ts(base, 'tools/plan'), 'tools/levels/apply.ts', 'tools/levels/checker.ts', 'tools/kit/fit.ts', 'tools/missions/handover/overlay.ts', 'tools/missions/handover/cast.ts', 'tools/rag/config.ts',
  ...ts(base, 'tools/world'),
  // rule M-2 (the overlay) expands the promoted cast with the people tools when it runs on a live plan (Redline H1, 2026-10-15); a recorded run has no roles, so it never does
  ...ts(base, 'tools/people'), ...ts(base, 'contracts/people'), 'tools/gen/sha.ts',
];

export async function toolsHash(base: string = root): Promise<string> {
  return hash((await Promise.all(transitFiles(base).map(async f => `${f}\n${await Bun.file(resolve(base, f)).text()}`))).join('\n--\n'));
}
