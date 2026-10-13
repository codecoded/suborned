// The concept sheets of the skyline objective (brief 007 F, Sky 2026-10-14): the crew sees the two contact sheets as
// images, and every site it proposes cites the tiles it rests on. contracts/world/skyline-concepts.json is the index:
// sheet path, sha256 and tile ids, no descriptions. A run pins the index (a copy in the run folder) and the sheets by
// sha256 (the call records carry them: tools/rag/images.ts); the picture bytes are never copied per run.
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { z } from 'zod';
import { imagePath } from '../../rag/images';
import { root } from '../../rag/config';
import type { Check } from '../../gen/plugin';
import { sha256 } from '../../gen/sha';

export const CONCEPTS_FILE = 'contracts/world/skyline-concepts.json';
export const CONCEPTS_PIN = 'input.concepts.json';

const hex = z.string().regex(/^[0-9a-f]{64}$/);
export const ConceptIndex = z.object({
  schema: z.literal('suborned.skyline-concepts/1'),
  note: z.string().optional(),
  sheets: z.array(z.object({
    id: z.string(), path: z.string(), sha256: hex, provenance: z.string(), provenanceSha256: hex,
    tiles: z.array(z.string().regex(/^[a-z]+\/[a-z0-9_]+$/)).min(1),
  })).min(1),
});
export type ConceptIndex = z.infer<typeof ConceptIndex>;

export const parseConcepts = (raw: string): ConceptIndex => ConceptIndex.parse(JSON.parse(raw));
export const tileSet = (index: ConceptIndex): Set<string> => new Set(index.sheets.flatMap(s => s.tiles));
/** The sheets as the images of a call, in index order (Blocks, then AtomicAge). */
export const sheetPaths = (index: ConceptIndex): string[] => index.sheets.map(s => s.path);

/** The sheets the index names, read when the objective is defined: what every skyline call is shown. */
export const conceptSheets = (file = CONCEPTS_FILE): string[] =>
  sheetPaths(parseConcepts(readFileSync(resolve(root, file), 'utf8')));

/**
 * What run.json records about the concept inputs. The sheets' own sha256 are also in every call record that showed
 * them.
 */
export type ConceptsRecord = { source: string; sha256: string; sheets: { id: string; path: string; sha256: string }[] };

/** At run start: the index is copied into the run folder, and each sheet on disk must be the one the index names. */
export async function pinConcepts(
  rdir: string, file = CONCEPTS_FILE,
): Promise<{ index: ConceptIndex; record: ConceptsRecord }> {
  const raw = await Bun.file(resolve(root, file)).text(), index = parseConcepts(raw);
  for (const s of index.sheets) {
    const bytes = await Bun.file(imagePath(s.path)).bytes().catch(() => undefined);
    if (!bytes) throw new Error(`concept sheet ${s.path} is missing`);
    if (sha256(bytes) !== s.sha256) {
      throw new Error(`concept sheet ${s.path} is not the one ${file} names (sha256 differs)`);
    }
  }
  await Bun.write(`${rdir}/${CONCEPTS_PIN}`, raw);
  const sheets = index.sheets.map(s => ({ id: s.id, path: s.path, sha256: s.sha256 }));
  return { index, record: { source: file, sha256: sha256(raw), sheets } };
}

/**
 * Replay: the pinned index matches its sha256 and parses. The sheets are checked where they are, when this machine has
 * them.
 */
export async function checkConcepts(c: Check): Promise<ConceptIndex | undefined> {
  const rec: ConceptsRecord | undefined = c.run.inputConcepts;
  if (!rec) { c.fail('run.json has no inputConcepts'); return undefined; }
  const raw = await Bun.file(`${c.dir}/${CONCEPTS_PIN}`).text().catch(() => undefined);
  if (raw === undefined) { c.fail(`${CONCEPTS_PIN} is missing`); return undefined; }
  if (sha256(raw) !== rec.sha256) { c.fail(`${CONCEPTS_PIN} does not match the recorded sha256`); return undefined; }
  const index = parseConcepts(raw);
  c.ok(`concept index ${rec.sha256.slice(0, 12)} (${tileSet(index).size} tiles on ${index.sheets.length} sheets)`);
  for (const s of rec.sheets) {
    const bytes = await Bun.file(imagePath(s.path)).bytes().catch(() => undefined), short = s.sha256.slice(0, 12);
    if (!bytes) c.warn(`concept sheet ${s.path} is not on this machine: its sha256 ${short} is not rechecked`);
    else if (sha256(bytes) === s.sha256) c.ok(`concept sheet ${s.id} ${short}`);
    else c.fail(`concept sheet ${s.path} differs from the one the run was made with (sha256 ${short} recorded)`);
  }
  return index;
}
