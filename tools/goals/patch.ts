import { mkdir, rename, writeFile } from 'node:fs/promises';
import { randomUUID } from 'node:crypto';
import { dirname } from 'node:path';
import { hash } from '../rag/corpus';
import { fileState, localPath, type FileState } from './files';
import type { CodePatch } from './schema';

export type ChangedFile = { path: string; before: string | null; after: string; beforeHash: string | null; afterHash: string };

/** Exact replacements make the generated change reviewable and prevent a patch matching the wrong version. */
export function preparePatch(patch: CodePatch, allowed: string[], before: FileState): ChangedFile[] {
  const after = { ...before };
  for (const edit of patch.edits) {
    if (!allowed.includes(edit.path) || !Object.hasOwn(before, edit.path)) throw new Error(`Path outside the write scope: ${edit.path}`);
    if (edit.replacement.length > 100_000) throw new Error('A single replacement exceeds 100 KB');
    const old = after[edit.path];
    if (old === null) {
      if (edit.search !== '' || !edit.replacement.trim()) throw new Error(`A new file needs empty search and nonempty content: ${edit.path}`);
      after[edit.path] = edit.replacement;
    } else {
      if (!edit.search || old === undefined) throw new Error(`Existing files require an exact search: ${edit.path}`);
      const at = old.indexOf(edit.search);
      if (at < 0 || old.indexOf(edit.search, at + 1) >= 0) throw new Error(`Search must match exactly once: ${edit.path}`);
      after[edit.path] = old.slice(0, at) + edit.replacement + old.slice(at + edit.search.length);
    }
  }
  const changed = Object.keys(before).filter(p => before[p] !== after[p]).map(path => ({
    path, before: before[path]!, after: after[path]!, beforeHash: before[path] === null ? null : hash(before[path]!), afterHash: hash(after[path]!),
  }));
  if (!changed.length) throw new Error('Patch makes no change');
  return changed;
}

export async function applyPatch(root: string, patch: CodePatch, allowed: string[], expected: FileState): Promise<ChangedFile[]> {
  const now = await fileState(root, allowed);
  for (const path of allowed) if (now[path] !== expected[path]) throw new Error(`Source changed since the scan: ${path}`);
  const changed = preparePatch(patch, allowed, now);
  // Validate every path and every replacement before the first write. Record the patch before invoking this function.
  const targets = await Promise.all(changed.map(c => localPath(root, c.path)));
  for (const [i, file] of changed.entries()) {
    const target = targets[i]!;
    await mkdir(dirname(target), { recursive: true });
    const staging = target + '.a05-' + randomUUID() + '.tmp';
    await writeFile(staging, file.after, { flag: 'wx' });
    await rename(staging, target);
  }
  return changed;
}
