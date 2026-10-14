// The promoted cast (brief 010; Sky, 2026-10-15): the accepted cast tables of a people run, kept as a committed source,
// contracts/people/<plan id>.cast.json, with its record contracts/people/<plan id>.cast.promotion.json beside it. `bun run people`
// with no --cast expands it. The file is the run's accepted `cast` byte for byte, so it cannot drift from its run silently:
// `checkPromotion` holds it to the record's sha256 and to the run on disk, and runs at every `bun run people`, `bun run verify`
// and in the contracts tests. A new cast is a new run, promoted again, never an edit.
import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { Cast, CastPromotion } from '../../contracts/people/cast.schema';
import { sha256 } from '../gen/sha';
import { root as repo } from '../rag/config';

export const castFileOf = (planId: string) => `contracts/people/${planId}.cast.json`;
export const promotionFileOf = (planId: string) => `contracts/people/${planId}.cast.promotion.json`;

export type Promotion = { ok: boolean; problems: string[]; record?: CastPromotion; castText?: string; label: string };

/** The bytes a cast file must have to be a run's `cast`: JSON.stringify(cast, null, 1), no trailing newline. */
export const castBytes = (cast: unknown) => JSON.stringify(cast, null, 1);

/** Hold the promoted cast of `planId` to its record and to the run it was promoted from. `base`: the repo root (tests point it at a copy). */
export function checkPromotion(planId: string, base: string = repo): Promotion {
  const rd = (f: string) => resolve(base, f), problems: string[] = [], file = castFileOf(planId), recFile = promotionFileOf(planId);
  if (!existsSync(rd(recFile))) return { ok: false, problems: [`no promotion record ${recFile}: a cast is promoted from an accepted run, then held to it`], label: file };
  const parsed = CastPromotion.safeParse(JSON.parse(readFileSync(rd(recFile), 'utf8')));
  if (!parsed.success) return { ok: false, problems: [`${recFile} fails its schema: ${parsed.error.issues.slice(0, 3).map(i => `${i.path.join('.')} ${i.message}`).join(' | ')}`], label: file };
  const rec = parsed.data, src = rec.source, label = `${file} (run ${src.run} round ${src.round}, sha256 ${rec.cast.sha256.slice(0, 12)}, promoted: ${rec.promoted})`;
  if (rec.plan !== planId) problems.push(`the record is for plan ${rec.plan}, not ${planId}`);
  if (rec.cast.file !== file) problems.push(`the record names ${rec.cast.file}, not ${file}`);
  if (!existsSync(rd(file))) return { ok: false, problems: [...problems, `${file} is missing`], record: rec, label };
  const bytes = new Uint8Array(readFileSync(rd(file))), text = new TextDecoder().decode(bytes);
  // the file against its record
  if (sha256(bytes) !== rec.cast.sha256) problems.push(`${file} has sha256 ${sha256(bytes).slice(0, 12)}, the record says ${rec.cast.sha256.slice(0, 12)}: the promoted cast was edited`);
  if (bytes.length !== rec.cast.bytes) problems.push(`${file} is ${bytes.length} bytes, the record says ${rec.cast.bytes}`);
  // the run against its record, and the file against the run: byte for byte
  const runDir = `runs/levels/${planId}/${src.run}`;
  if (!src.file.startsWith(`${runDir}/`)) problems.push(`the source file ${src.file} is not under ${runDir}/`);
  if (!existsSync(rd(src.file))) problems.push(`the run's accepted output ${src.file} is missing`);
  else {
    const outBytes = new Uint8Array(readFileSync(rd(src.file)));
    if (sha256(outBytes) !== src.sha256) problems.push(`${src.file} has sha256 ${sha256(outBytes).slice(0, 12)}, the record says ${src.sha256.slice(0, 12)}: the run's output changed`);
    let theirs: string | undefined;
    try { theirs = castBytes(JSON.parse(new TextDecoder().decode(outBytes))[src.path]); } catch { problems.push(`${src.file} has no ${src.path}`); }
    if (theirs !== undefined && theirs !== text) problems.push(`${file} is not the ${src.path} of ${src.file} byte for byte (${text.length} chars, the run's is ${theirs.length}): it drifted from run ${src.run}`);
  }
  if (!existsSync(rd(src.runFile))) problems.push(`the run record ${src.runFile} is missing`);
  else {
    const r = JSON.parse(readFileSync(rd(src.runFile), 'utf8')) as { run?: string; outcome?: string; finalVersion?: string; loop?: number };
    if (r.run !== src.run) problems.push(`${src.runFile} is run ${r.run}, the record says ${src.run}`);
    if (r.outcome !== src.outcome) problems.push(`run ${src.run} is ${r.outcome}, a promoted cast comes from an ${src.outcome} run`);
    if (r.finalVersion !== src.version) problems.push(`run ${src.run} ended at ${r.finalVersion}, the record says ${src.version}`);
    if (r.loop !== src.round) problems.push(`run ${src.run} was accepted in round ${r.loop}, the record says round ${src.round}`);
  }
  try { if (!Cast.safeParse(JSON.parse(text)).success) problems.push(`${file} fails the cast schema`); } catch { problems.push(`${file} is not JSON`); }
  return { ok: !problems.length, problems, record: rec, castText: text, label };
}
