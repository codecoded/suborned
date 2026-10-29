// Brief 077 (Splice): drift check for Latch's sibling leaf shader. DK_LitCutawayLeaf.shader and its four generated pass includes (DK_LeafForward, DK_LeafGBuffer,
// DK_LeafDepthNormals, DK_LeafMeta) must be byte for byte what briefs/077/shader/make_leaf_hlsl.py writes from DK_LitCutaway.shader and the URP package the project
// locks (unity/Packages/packages-lock.json; read from unity/Library/PackageCache). The generator runs in a temporary folder on a copy of DK_LitCutaway.shader, so
// nothing in the tree is written. DK_Leaf.hlsl is hand-written (the generator includes it, never writes it): it is only required to exist.
//   bun tools/kit/leaf-shader.ts      prints each file's verdict; exit 1 on drift, 2 when the URP package is not on this machine
import { spawnSync } from 'node:child_process';
import { copyFileSync, existsSync, mkdtempSync, readdirSync, readFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';

const repo = resolve(import.meta.dir, '../..');
export const SHADERS = join(repo, 'unity/Assets/Suborned/Shaders/DecoKit');
export const GENERATOR = join(repo, 'briefs/077/shader/make_leaf_hlsl.py');
export const GENERATED = ['DK_LitCutawayLeaf.shader', 'DK_LeafForward.hlsl', 'DK_LeafGBuffer.hlsl', 'DK_LeafDepthNormals.hlsl', 'DK_LeafMeta.hlsl'];
const URP = 'com.unity.render-pipelines.universal';

/** The URP package folder whose package.json version is the one the project locks, or undefined (no Library on this machine). */
export function urpDir(): string | undefined {
  const lock = JSON.parse(readFileSync(join(repo, 'unity/Packages/packages-lock.json'), 'utf8')).dependencies?.[URP]?.version as string | undefined;
  const cache = join(repo, 'unity/Library/PackageCache');
  if (!lock || !existsSync(cache)) return undefined;
  return readdirSync(cache).filter(d => d.startsWith(`${URP}@`)).map(d => join(cache, d))
    .find(d => existsSync(join(d, 'package.json')) && JSON.parse(readFileSync(join(d, 'package.json'), 'utf8')).version === lock);
}

export type Drift = { file: string; ok: boolean; why?: string };
/** Regenerates into a temporary folder and compares. `urp` defaults to the locked package; `shaders` to the project's DecoKit shader folder. */
export function leafShaderDrift(urp = urpDir(), shaders = SHADERS): Drift[] {
  if (!urp) throw new Error(`${URP} at the locked version is not in unity/Library/PackageCache: open the project in Unity once, then re-run`);
  const tmp = mkdtempSync(join(tmpdir(), 'leaf077-'));
  try {
    copyFileSync(join(shaders, 'DK_LitCutaway.shader'), join(tmp, 'DK_LitCutaway.shader'));
    const run = spawnSync('python3', ['-I', GENERATOR, urp, tmp], { encoding: 'utf8' });
    if (run.status !== 0) return [{ file: 'make_leaf_hlsl.py', ok: false, why: `the generator failed (an asserted edit no longer matches its source?): ${(run.stderr || run.stdout).trim().split('\n').slice(-2).join(' ')}` }];
    const out: Drift[] = GENERATED.map(f => {
      const have = join(shaders, f);
      if (!existsSync(have)) return { file: f, ok: false, why: 'missing in the project' };
      return readFileSync(have).equals(readFileSync(join(tmp, f))) ? { file: f, ok: true } : { file: f, ok: false, why: 'differs from what make_leaf_hlsl.py writes now: re-run it (Latch) or restore the file' };
    });
    out.push(existsSync(join(shaders, 'DK_Leaf.hlsl')) ? { file: 'DK_Leaf.hlsl', ok: true, why: 'hand-written, present' } : { file: 'DK_Leaf.hlsl', ok: false, why: 'missing: every leaf include needs it' });
    return out;
  } finally { rmSync(tmp, { recursive: true, force: true }); }
}

if (import.meta.main) {
  const urp = urpDir();
  if (!urp) { console.error(`leaf shader: ${URP} (locked version) not in unity/Library/PackageCache; cannot check`); process.exit(2); }
  const r = leafShaderDrift(urp);
  for (const d of r) console.log(`${d.ok ? 'PASS' : 'FAIL'}  ${d.file}${d.why ? `: ${d.why}` : ''}`);
  process.exit(r.every(d => d.ok) ? 0 : 1);
}
