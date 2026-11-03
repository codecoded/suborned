// The built app carries the audio cue file (tools/game/ships.ts; briefs/067/splice.md item 2). Run: bun test tools/game/ships.test.ts
import { expect, test } from 'bun:test';
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import { appShips, appStreaming, STAGED } from './ships';

function fixture(o: { contract?: string; inApp?: string }): { root: string; app: string } {
  const root = mkdtempSync(join(tmpdir(), 'ships-')), app = join(root, 'The Suborned.app');
  const put = (f: string, t: string) => { mkdirSync(dirname(f), { recursive: true }); writeFileSync(f, t); };
  if (o.contract !== undefined) put(join(root, 'contracts/audio/cues.json'), o.contract);
  if (o.inApp !== undefined) put(join(appStreaming(app), 'cues.json'), o.inApp);
  else mkdirSync(app, { recursive: true });
  return { root, app };
}

test('the cue file is what the app must carry, from the contract', () => {
  expect(STAGED).toEqual([{ file: 'cues.json', from: 'contracts/audio/cues.json' }]);
  expect(appStreaming('/x/The Suborned.app')).toBe('/x/The Suborned.app/Contents/Resources/Data/StreamingAssets/sim');
});

test('an app that carries the contract\'s bytes passes, naming the file and its hash', () => {
  const f = fixture({ contract: '{"cues":[1]}', inApp: '{"cues":[1]}' });
  try { const r = appShips(f.app, f.root); expect(r.ok).toBe(true); expect(r.lines[0]).toMatch(/the app carries StreamingAssets\/sim\/cues\.json = contracts\/audio\/cues\.json \([0-9a-f]{12}\)/); } finally { rmSync(f.root, { recursive: true, force: true }); }
});

test('an app with no cue file fails, and says how it comes (the player outside the repo plays the code\'s copy)', () => {
  const f = fixture({ contract: '{"cues":[1]}' });
  try { const r = appShips(f.app, f.root); expect(r.ok).toBe(false); expect(r.lines[0]).toContain('the app carries no StreamingAssets/sim/cues.json'); expect(r.lines[0]).toContain('bun tools/sim/unity.ts'); } finally { rmSync(f.root, { recursive: true, force: true }); }
});

test('an app that carries an older cue file than the contract fails, with both hashes', () => {
  const f = fixture({ contract: '{"cues":[2]}', inApp: '{"cues":[1]}' });
  try { const r = appShips(f.app, f.root); expect(r.ok).toBe(false); expect(r.lines[0]).toMatch(/is not contracts\/audio\/cues\.json/); expect(r.lines[0]).toMatch(/\([0-9a-f]{12}\) is not .* \([0-9a-f]{12}\)/); } finally { rmSync(f.root, { recursive: true, force: true }); }
});

test('no contract to compare with is a failure, not a pass', () => {
  const f = fixture({ inApp: '{"cues":[1]}' });
  try { const r = appShips(f.app, f.root); expect(r.ok).toBe(false); expect(r.lines[0]).toContain('is not there to compare'); } finally { rmSync(f.root, { recursive: true, force: true }); }
});
