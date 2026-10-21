// tools/game/runtime-compile.ts (brief 034): the player's scripts compile headless, and a script that does not is found with its file and line.
// Needs dotnet and the Unity version the project opens in (skipped where either is missing). Run: bun test tools/game
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { existsSync, mkdtempSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { compile, csproj, managedDir } from './runtime-compile';

setDefaultTimeout(240_000);
const can = !!Bun.which('dotnet') && existsSync(managedDir()) && existsSync(join(import.meta.dir, '../../unity/Library/ScriptAssemblies/Unity.InputSystem.dll'));

test('the project file lists the runtime, the extras and the references', () => {
  const t = csproj({ runtime: '/r', managed: '/m', scriptAssemblies: '/s', sim: '/x/Suborned.Sim.dll', extra: ['/e/A.cs'] });
  expect(t).toContain('<Compile Include="/r/**/*.cs" />'); expect(t).toContain('<Compile Include="/e/A.cs" />'); expect(t).toContain('/m/UnityEngine*.dll'); expect(t).toContain('/s/Unity.RenderPipelines.Universal.Runtime.dll'); expect(t).toContain('/x/Suborned.Sim.dll');
});

describe.skipIf(!can)('against the real project', () => {
  test('the runtime compiles, and a deliberately broken script is reported with file and line', () => {
    const ok = compile();
    expect(ok.errors).toEqual([]); expect(ok.ok).toBe(true);
    const d = mkdtempSync(join(tmpdir(), 'rc-')), bad = join(d, 'Bad.cs');
    writeFileSync(bad, 'namespace Suborned.Play { public static class Bad { public static void Go(HandoverHost h) { h.NoSuchMethod(); } } }\n');
    const r = compile([bad]);
    expect(r.ok).toBe(false); expect(r.errors.join('\n')).toMatch(/Bad\.cs\(1,\d+\): error CS1061.*NoSuchMethod/);
  });
});
