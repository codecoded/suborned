import { describe, expect, test } from 'bun:test';
import { existsSync, readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { EDITOR_TESTS, caseFiles, runnerCsproj } from './hostlogic';

const ROOT = resolve(import.meta.dir, '../..');
describe('the host logic runner (brief 078)', () => {
  test('its csproj takes the patched copies, never the real Unity or the real player', () => {
    const p = runnerCsproj({ player: '/o/player/check.dll', shim: '/o/unity-v', sim: '/s/Sim.dll', sa: '/sa', nunit: '/n/nunit.dll', tests: ['/t/A.cs'], cases: ['/c/HostLogic.cs', '/c/HostLogic.EndCard.cs'] });
    expect(p).toContain('<Reference Include="/o/player/check.dll" />'); expect(p).toContain('<Reference Include="/o/unity-v/Unity*.dll" />');
    expect(p).not.toContain('Variations/mono/Managed'); expect(p).not.toContain('out/batch/compile/bin');
    expect(p).toContain('<Compile Include="/t/A.cs" />'); expect(p).toContain('<Compile Include="/c/HostLogic.EndCard.cs" />');
  });
  test('it compiles every cases file beside HostLogic.cs, never the assembly patcher', () => {
    const f = caseFiles().map(x => x.split('/').at(-1)); expect(f).toContain('HostLogic.cs'); expect(f).toContain('HostLogic.EndCard.cs'); expect(f).not.toContain('Patch.cs');
  });
  test('the Editor tests it borrows exist and read no Editor API, scene or asset', () => {
    for (const f of EDITOR_TESTS) {
      const path = join(ROOT, 'unity/Assets/Suborned/Tests/Editor', f); expect(existsSync(path)).toBe(true);
      expect(readFileSync(path, 'utf8')).not.toMatch(/using UnityEditor|AssetDatabase|EditorSceneManager|Resources\.Load/);
    }
  });
  test('failing example: an Editor test that opens a scene would be caught', () => {
    expect('EditorSceneManager.OpenScene("x")').toMatch(/using UnityEditor|AssetDatabase|EditorSceneManager|Resources\.Load/);
  });
  test('what it silences is what draws or sounds: the host keeps its decisions', () => {
    const patch = readFileSync(join(ROOT, 'tools/game/hostlogic/Patch.cs'), 'utf8');
    expect(patch.match(/Silent = \{ ([^}]*?) \}/)![1]).toBe('"Suborned.Play.HostViews"');
    expect(patch.match(/SilentHost = \{ ([^}]*) \}/)![1]!.split(', ').map(s => s.trim().replace(/"/g, ''))).toEqual(['AudioInit', 'AudioLog', 'AudioTick', 'AudioFrame', 'Save']);
  });
});
