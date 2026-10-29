// Brief 077: the leaf shader drift check (leaf-shader.ts) passes on the project's files and fails on a copy with one generated file edited, or with
// DK_LitCutaway.shader changed under it. Needs the URP package in unity/Library/PackageCache; without it the tests are skipped and say so.
import { describe, expect, test } from 'bun:test';
import { appendFileSync, cpSync, mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { GENERATED, SHADERS, leafShaderDrift, urpDir } from './leaf-shader';

const urp = urpDir();
if (!urp) console.warn('leaf-shader.test: URP not in unity/Library/PackageCache; the drift tests are skipped');
const copy = () => { const d = mkdtempSync(join(tmpdir(), 'leafcopy-')); cpSync(SHADERS, d, { recursive: true }); return d; };

describe.skipIf(!urp)('DK_LitCutawayLeaf drift (brief 077)', () => {
  test('the project\'s sibling shader and its four includes are what make_leaf_hlsl.py writes', () => {
    const r = leafShaderDrift(urp);
    expect(r.filter(d => !d.ok)).toEqual([]);
    expect(r.map(d => d.file)).toEqual([...GENERATED, 'DK_Leaf.hlsl']);
  });
  test('a hand edit to a generated include fails', () => {
    const d = copy();
    try { appendFileSync(join(d, 'DK_LeafForward.hlsl'), '// edited\n'); expect(leafShaderDrift(urp, d).filter(x => !x.ok).map(x => x.file)).toEqual(['DK_LeafForward.hlsl']); }
    finally { rmSync(d, { recursive: true, force: true }); }
  });
  test('DK_LitCutaway.shader changed without re-running the generator fails the sibling', () => {
    const d = copy();
    try { appendFileSync(join(d, 'DK_LitCutaway.shader'), '\n// changed\n'); expect(leafShaderDrift(urp, d).filter(x => !x.ok).map(x => x.file)).toEqual(['DK_LitCutawayLeaf.shader']); }
    finally { rmSync(d, { recursive: true, force: true }); }
  });
});
