// Promoted tables (brief 012): the accepted tables of a dialogue run, kept as a committed source, contracts/dialogue/<plan id>.<scope>.voices.json, with a record
// beside it, as the people cast is (tools/people/promoted.ts). The file is the run's accepted `voices` byte for byte, so it cannot drift from its run silently;
// `checkPromotion` holds it to the record's sha256 and to the run on disk. New tables are a new run, promoted again, never an edit.
// The round the tables were accepted in is the last of the run's rounds (run.json `rounds`), not run.json's `loop`, which is the loop rule's version.
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname } from 'node:path';
import { resolve } from 'node:path';
import { Voices, VoicesPromotion } from '../../contracts/dialogue/voices.schema';
import { sha256 } from '../gen/sha';
import { root as repo } from '../rag/config';
import { promotionFileOf, tablesFileOf } from './texts';

export const voicesBytes = (voices: unknown) => JSON.stringify(voices, null, 1);
export type Promotion = { ok: boolean; problems: string[]; record?: VoicesPromotion; text?: string; label: string };

export function checkPromotion(planId: string, scope: string, base: string = repo): Promotion {
  const rd = (f: string) => resolve(base, f), problems: string[] = [], file = tablesFileOf(planId, scope), recFile = promotionFileOf(planId, scope);
  if (!existsSync(rd(recFile))) return { ok: false, problems: [`no promotion record ${recFile}: tables are promoted from an accepted run, then held to it`], label: file };
  const parsed = VoicesPromotion.safeParse(JSON.parse(readFileSync(rd(recFile), 'utf8')));
  if (!parsed.success) return { ok: false, problems: [`${recFile} fails its schema: ${parsed.error.issues.slice(0, 3).map(i => `${i.path.join('.')} ${i.message}`).join(' | ')}`], label: file };
  const rec = parsed.data, src = rec.source, label = `${file} (run ${src.run} round ${src.round}, sha256 ${rec.voices.sha256.slice(0, 12)}, promoted: ${rec.promoted})`;
  if (rec.plan !== planId) problems.push(`the record is for plan ${rec.plan}, not ${planId}`);
  if (rec.scope !== scope) problems.push(`the record is for scope ${rec.scope}, not ${scope}`);
  if (rec.voices.file !== file) problems.push(`the record names ${rec.voices.file}, not ${file}`);
  if (!existsSync(rd(file))) return { ok: false, problems: [...problems, `${file} is missing`], record: rec, label };
  const bytes = new Uint8Array(readFileSync(rd(file))), text = new TextDecoder().decode(bytes);
  if (sha256(bytes) !== rec.voices.sha256) problems.push(`${file} has sha256 ${sha256(bytes).slice(0, 12)}, the record says ${rec.voices.sha256.slice(0, 12)}: the promoted tables were edited`);
  if (bytes.length !== rec.voices.bytes) problems.push(`${file} is ${bytes.length} bytes, the record says ${rec.voices.bytes}`);
  const runDir = `runs/levels/${planId}/${src.run}`;
  if (!src.file.startsWith(`${runDir}/`)) problems.push(`the source file ${src.file} is not under ${runDir}/`);
  if (!existsSync(rd(src.file))) problems.push(`the run's accepted output ${src.file} is missing`);
  else {
    const outBytes = new Uint8Array(readFileSync(rd(src.file)));
    if (sha256(outBytes) !== src.sha256) problems.push(`${src.file} has sha256 ${sha256(outBytes).slice(0, 12)}, the record says ${src.sha256.slice(0, 12)}: the run's output changed`);
    let theirs: string | undefined;
    try { theirs = voicesBytes(JSON.parse(new TextDecoder().decode(outBytes))[src.path]); } catch { problems.push(`${src.file} has no ${src.path}`); }
    if (theirs !== undefined && theirs !== text) problems.push(`${file} is not the ${src.path} of ${src.file} byte for byte: it drifted from run ${src.run}`);
  }
  if (!existsSync(rd(src.runFile))) problems.push(`the run record ${src.runFile} is missing`);
  else {
    const r = JSON.parse(readFileSync(rd(src.runFile), 'utf8')) as { run?: string; outcome?: string; finalVersion?: string; objective?: string; scope?: string; rounds?: { round: number }[] };
    if (r.run !== src.run) problems.push(`${src.runFile} is run ${r.run}, the record says ${src.run}`);
    if (r.objective !== 'dialogue') problems.push(`run ${src.run} is a ${r.objective} run, not a dialogue run`);
    if (r.scope !== scope) problems.push(`run ${src.run} wrote scope ${r.scope}, the record says ${scope}`);
    if (r.outcome !== src.outcome) problems.push(`run ${src.run} is ${r.outcome}, promoted tables come from an ${src.outcome} run`);
    if (r.finalVersion !== src.version) problems.push(`run ${src.run} ended at ${r.finalVersion}, the record says ${src.version}`);
    if (r.rounds?.at(-1)?.round !== src.round) problems.push(`run ${src.run} was accepted in round ${r.rounds?.at(-1)?.round}, the record says round ${src.round}`);
  }
  try { if (!Voices.safeParse(JSON.parse(text)).success) problems.push(`${file} fails the voices schema`); } catch { problems.push(`${file} is not JSON`); }
  return { ok: !problems.length, problems, record: rec, text, label };
}

/** Promotes the accepted tables of a dialogue run: writes the tables file (the run's `voices`, byte for byte) and its record, then holds the pair to the run. Refuses a run that is
 *  not an ACCEPTED dialogue run of the scope, and an existing pair (replacing is a decision: delete the pair first). `base`: the repo root (tests point it at a scratch copy). */
export function promoteRun(o: { run: string; scope: string; by: string; why: string; plan: string }, base: string = repo): Promotion {
  const rd = (f: string) => resolve(base, f), runFile = `runs/levels/${o.plan}/${o.run}/run.json`, file = tablesFileOf(o.plan, o.scope), recFile = promotionFileOf(o.plan, o.scope);
  const fail = (m: string): Promotion => ({ ok: false, problems: [m], label: file });
  if (!existsSync(rd(runFile))) return fail(`no ${runFile}`);
  const r = JSON.parse(readFileSync(rd(runFile), 'utf8')) as { outcome: string; finalVersion: string; objective: string; scope: string; rounds: { round: number }[] };
  if (r.objective !== 'dialogue' || r.scope !== o.scope || r.outcome !== 'ACCEPTED') return fail(`run ${o.run} is ${r.outcome} (${r.objective}, scope ${r.scope}); only an ACCEPTED dialogue run of scope ${o.scope} is promoted`);
  if (existsSync(rd(file)) || existsSync(rd(recFile))) return fail(`${file} or its record exists: delete the pair first to replace it, by decision`);
  const round = r.rounds.at(-1)!.round, srcFile = `runs/levels/${o.plan}/${o.run}/round-${round}/fable.json`, srcBytes = new Uint8Array(readFileSync(rd(srcFile)));
  const text = voicesBytes(JSON.parse(new TextDecoder().decode(srcBytes)).voices), bytes = new TextEncoder().encode(text);
  mkdirSync(dirname(rd(file)), { recursive: true });
  writeFileSync(rd(file), text);
  writeFileSync(rd(recFile), JSON.stringify({
    schema: 'suborned.voices.promotion/1', plan: o.plan, scope: o.scope, voices: { file, sha256: sha256(bytes), bytes: bytes.length },
    source: { run: o.run, round, version: r.finalVersion, file: srcFile, path: 'voices', sha256: sha256(srcBytes), runFile, outcome: 'ACCEPTED' }, promoted: o.by, why: o.why,
  }, null, 1) + '\n');
  return checkPromotion(o.plan, o.scope, base);
}
