import { afterEach, describe, expect, test } from 'bun:test';
import { mkdtemp, readFile, rm, symlink, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import { tmpdir } from 'node:os';
import { applyPatch, preparePatch } from '../patch';
import { localPath } from '../files';
import type { CodePatch } from '../schema';

const dirs: string[] = [];
afterEach(async () => { await Promise.all(dirs.splice(0).map(p => rm(p, { recursive: true, force: true }))); });
const edit = (path: string, search: string, replacement: string): CodePatch => ({ summary: 'fixture change', edits: [{ path, search, replacement }] });
async function fixture(): Promise<string> {
  const root = await mkdtemp(join(tmpdir(), 'a05-patch-test-')); dirs.push(root); return root;
}

describe('bounded exact code patches', () => {
  test('preparation permits exactly one match, records before/after hashes, and does not mutate the snapshot', () => {
    const before = { 'Game.cs': 'class Before {}' };
    const rows = preparePatch(edit('Game.cs', 'Before', 'After'), ['Game.cs'], before);
    expect(rows[0]?.before).toBe('class Before {}');
    expect(rows[0]?.after).toBe('class After {}');
    expect(rows[0]?.beforeHash).not.toBe(rows[0]?.afterHash);
    expect(before['Game.cs']).toBe('class Before {}');
  });

  test('ambiguous, absent, empty, out-of-scope and no-op replacements fail', () => {
    for (const search of ['old', 'missing', '']) {
      expect(() => preparePatch(edit('Game.cs', search, 'new'), ['Game.cs'], { 'Game.cs': 'old old' })).toThrow();
    }
    expect(() => preparePatch(edit('Else.cs', 'old', 'new'), ['Game.cs'], { 'Game.cs': 'old' })).toThrow('outside');
    expect(() => preparePatch(edit('Game.cs', 'old', 'old'), ['Game.cs'], { 'Game.cs': 'old' })).toThrow('no change');
    expect(() => preparePatch(edit('Game.cs', 'old', 'new'), ['Game.cs'], {})).toThrow('outside');
  });

  test('new files require an empty search and nonempty content', () => {
    expect(preparePatch(edit('New.cs', '', 'class New {}'), ['New.cs'], { 'New.cs': null })[0]?.beforeHash).toBeNull();
    expect(() => preparePatch(edit('New.cs', 'something', 'class New {}'), ['New.cs'], { 'New.cs': null })).toThrow('new file');
    expect(() => preparePatch(edit('New.cs', '', ' \n'), ['New.cs'], { 'New.cs': null })).toThrow('new file');
  });

  test('stale sources are rejected even when the search text would still match', async () => {
    const root = await fixture(); await writeFile(join(root, 'Game.cs'), '// changed\nold');
    await expect(applyPatch(root, edit('Game.cs', 'old', 'new'), ['Game.cs'], { 'Game.cs': 'old' })).rejects.toThrow('changed since');
    expect(await readFile(join(root, 'Game.cs'), 'utf8')).toBe('// changed\nold');
  });

  test('all replacements validate before any file changes', async () => {
    const root = await fixture(); await writeFile(join(root, 'One.cs'), 'one'); await writeFile(join(root, 'Two.cs'), 'two');
    const patch: CodePatch = { summary: 'second edit invalid', edits: [
      { path: 'One.cs', search: 'one', replacement: 'changed' },
      { path: 'Two.cs', search: 'missing', replacement: 'changed' },
    ] };
    await expect(applyPatch(root, patch, ['One.cs', 'Two.cs'], { 'One.cs': 'one', 'Two.cs': 'two' })).rejects.toThrow('exactly once');
    expect(await readFile(join(root, 'One.cs'), 'utf8')).toBe('one');
    expect(await readFile(join(root, 'Two.cs'), 'utf8')).toBe('two');
  });

  test('applies multiple exact edits and new files within the approved snapshot', async () => {
    const root = await fixture(); await writeFile(join(root, 'Game.cs'), 'old method');
    const patch: CodePatch = { summary: 'valid edits', edits: [
      { path: 'Game.cs', search: 'old', replacement: 'new' },
      { path: 'Game.cs', search: 'method', replacement: 'behavior' },
      { path: 'New.cs', search: '', replacement: 'class New {}' },
    ] };
    const changed = await applyPatch(root, patch, ['Game.cs', 'New.cs'], { 'Game.cs': 'old method', 'New.cs': null });
    expect(changed).toHaveLength(2);
    expect(await readFile(join(root, 'Game.cs'), 'utf8')).toBe('new behavior');
    expect(await readFile(join(root, 'New.cs'), 'utf8')).toBe('class New {}');
  });

  test('destination symlinks and symlinked ancestors cannot escape the root', async () => {
    const root = await fixture(), outside = await fixture(); await writeFile(join(outside, 'victim'), 'untouched');
    await symlink(join(outside, 'victim'), join(root, 'Game.cs'));
    await expect(applyPatch(root, edit('Game.cs', 'untouched', 'changed'), ['Game.cs'], { 'Game.cs': 'untouched' })).rejects.toThrow('Symlink');
    await symlink(outside, join(root, 'linked'));
    await expect(localPath(root, 'linked/New.cs')).rejects.toThrow('Symlink');
    for (const path of ['../escape', '/absolute', 'a/../escape', 'a//b', 'a\\b']) await expect(localPath(root, path)).rejects.toThrow('Unsafe');
    expect(await readFile(join(outside, 'victim'), 'utf8')).toBe('untouched');
  });

  test('a staging-path symlink must never overwrite its external target', async () => {
    const root = await fixture(), outside = await fixture(); await writeFile(join(root, 'Game.cs'), 'old');
    const victim = join(outside, 'victim'); await writeFile(victim, 'untouched');
    await symlink(victim, join(root, 'Game.cs.a05-tmp'));
    try { await applyPatch(root, edit('Game.cs', 'old', 'new'), ['Game.cs'], { 'Game.cs': 'old' }); } catch { /* rejection is safe */ }
    expect(await readFile(victim, 'utf8')).toBe('untouched');
  });
});
