import { isDeepStrictEqual } from 'node:util';
import { fileHashes, fileState, sourceFiles } from './files';
import type { RunRecord } from './record';

export const inputDirectories = ['sim', 'contracts', 'runs/missions', 'briefs/011/motive-fights'];

/** Concurrent edits cannot silently become agent-authored work or change its acceptance checks. */
export async function auditInputs(root: string, record: RunRecord) {
  const immutable = Object.keys(record.inputs).filter(p => !record.scope.writable.includes(p));
  const hashes = await fileHashes(root, immutable);
  for (const path of immutable) if (hashes[path] !== record.inputs[path]) throw new Error(`Pinned input changed during run: ${path}`);
  const expected = { ...record.baseline };
  for (const round of record.rounds) for (const change of round.changes) expected[change.path] = change.after;
  if (!isDeepStrictEqual(await fileState(root, record.scope.writable), expected)) throw new Error('Writable source differs from the recorded generated changes');
  const files = await sourceFiles(root, inputDirectories);
  const initial = Object.keys(record.inputs).filter(p => inputDirectories.some(d => p.startsWith(d + '/')));
  const allowed = new Set([...initial, ...Object.keys(expected).filter(p => expected[p] !== null)]);
  if (files.length !== allowed.size || files.some(p => !allowed.has(p))) throw new Error('Input inventory changed during run');
}
