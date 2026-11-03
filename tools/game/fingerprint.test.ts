// The build's staleness fingerprint reads, by content, what the app carries and git does not hold (briefs/067/splice.md item 2): the engine DLL, and what the sim step stages in
// StreamingAssets/sim, the audio cues among it. It must not move on what a build or the sim step stamps with the clock, the commit or the build number.
// Run: bun test tools/game/fingerprint.test.ts
import { describe, expect, test } from 'bun:test';
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import { IGNORED_INPUTS, VOLATILE, fingerprint, ignoredFiles } from './fingerprint';

const SIM = 'unity/Assets/StreamingAssets/sim', DLL = 'unity/Assets/Suborned/Sim/Plugins/Suborned.Sim.dll';
function tree(files: Record<string, string>): string {
  const root = mkdtempSync(join(tmpdir(), 'fingerprint-'));
  for (const [f, text] of Object.entries(files)) { mkdirSync(dirname(join(root, f)), { recursive: true }); writeFileSync(join(root, f), text); }
  return root;
}
const stamp = (root: string) => fingerprint(ignoredFiles(root));
const base = { [DLL]: 'dll', [`${SIM}/rulebook.json`]: '{"r":1}', [`${SIM}/cues.json`]: '{"cues":[]}', [`${SIM}/engine.json`]: '{"built":"t0","commit":"c0"}', [`${SIM}/headless.json`]: '{"engine":{"commit":"c0"}}', 'unity/Assets/StreamingAssets/build.json': '{"build":1}' };

describe('the ignored inputs of the build fingerprint', () => {
  test('are the engine DLL folder and everything staged under StreamingAssets, read by sha256, repo-relative', () => {
    expect(IGNORED_INPUTS).toEqual(['unity/Assets/Suborned/Sim', 'unity/Assets/StreamingAssets']);
    const root = tree(base);
    try {
      const f = ignoredFiles(root);
      expect([...f.keys()].sort()).toEqual([DLL, `${SIM}/cues.json`, `${SIM}/rulebook.json`].sort());
      for (const v of f.values()) expect(v).toMatch(/^sha256:[0-9a-f]{64}$/);
    } finally { rmSync(root, { recursive: true, force: true }); }
  });

  test('a changed cue file changes the fingerprint (the app is stale), and so does a new, a removed, a changed rulebook or a changed DLL', () => {
    const was = tree(base), cases: Record<string, Record<string, string>> = {
      'cues.json edited': { ...base, [`${SIM}/cues.json`]: '{"cues":[1]}' },
      'cues.json added': { ...base, [`${SIM}/extra.json`]: '{}' },
      'rulebook edited': { ...base, [`${SIM}/rulebook.json`]: '{"r":2}' },
      'dll changed': { ...base, [DLL]: 'dll2' },
    };
    const gone = { ...base }; delete (gone as Record<string, string>)[`${SIM}/cues.json`]; cases['cues.json removed'] = gone;
    try {
      const w = stamp(was);
      for (const [why, files] of Object.entries(cases)) { const r = tree(files); try { expect(stamp(r), why).not.toBe(w); } finally { rmSync(r, { recursive: true, force: true }); } }
    } finally { rmSync(was, { recursive: true, force: true }); }
  });

  test('what carries the clock, the commit or the build number does not: engine.json, headless.json, build.json', () => {
    expect([...VOLATILE].sort()).toEqual(['build.json', 'build.json.meta', 'engine.json', 'engine.json.meta', 'headless.json', 'headless.json.meta']);
    const was = tree(base), later = tree({ ...base, [`${SIM}/engine.json`]: '{"built":"t1","commit":"c1"}', [`${SIM}/headless.json`]: '{"engine":{"commit":"c1"}}', 'unity/Assets/StreamingAssets/build.json': '{"build":2}' });
    try { expect(stamp(later)).toBe(stamp(was)); } finally { rmSync(was, { recursive: true, force: true }); rmSync(later, { recursive: true, force: true }); }
  });

  test('a folder that is not there reads as empty, and the fingerprint of nothing is stable', () => {
    const root = tree({});
    try { expect(ignoredFiles(root).size).toBe(0); expect(fingerprint(new Map())).toBe(fingerprint(new Map())); } finally { rmSync(root, { recursive: true, force: true }); }
  });

  test('fingerprint is order-free: the same files in another order give the same hash', () => {
    const a = new Map([['b', '2'], ['a', '1']]), b = new Map([['a', '1'], ['b', '2']]);
    expect(fingerprint(a)).toBe(fingerprint(b));
    expect(fingerprint(new Map([['a', '1'], ['b', '3']]))).not.toBe(fingerprint(a));
  });
});
