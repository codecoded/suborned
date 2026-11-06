// Brief 097: drift check for the DecoKit cutaway shaders' 097 edits (tools/shaders/make_dk097.py): DK_Forward097.hlsl must be byte for byte what the
// generator writes from the locked URP package, and the patched shaders must carry their wrappers and includes. Skipped when the URP package is not in
// unity/Library/PackageCache (a machine that never opened the project).
import { expect, test } from 'bun:test';
import { spawnSync } from 'node:child_process';
import { resolve } from 'node:path';
import { urpDir } from '../kit/leaf-shader';

test.skipIf(!urpDir())('make_dk097.py --check: the forward copy and the patched shaders are as generated', () => {
  const r = spawnSync('python3', ['-I', resolve(import.meta.dir, 'make_dk097.py'), '--check'], { encoding: 'utf8' });
  expect(r.stdout + r.stderr).not.toContain('FAIL');
  expect(r.status).toBe(0);
});
