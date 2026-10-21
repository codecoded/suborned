// tools/game/shots.json (brief 034 item 3): the file the player's shot runner works through is held to its schema, and every row that cannot be taken is
// refused with the shot's name and the reason before any build starts. Run: bun test tools/game
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { DEFAULT_FILE, check, expectedFiles, fileResolver, pngSize, resolveShots, type Resolver } from './shots';

const route = { path: '/r/route-a-board.json', info: { start: {}, until: 5100 } };
const fixture = { path: '/r/fix.json', info: { start: { fightLab: true }, until: 3000 } };
const R: Resolver = {
  script: ref => (ref === 'route-a-board' ? route : ref === 'fixture' ? fixture : undefined),
  checkpoint: n => n === 'cp-loud', scenario: id => (id === 'N10' ? route : undefined),
};
const file = (...shots: object[]) => ({ schema: 'suborned.shots/1', shots });
const problems = (...shots: object[]) => check(file(...shots), R).problems.join('\n');

describe('the committed shots.json', () => {
  const raw = JSON.parse(readFileSync(DEFAULT_FILE, 'utf8'));
  test('is valid against the real route scripts and checkpoints', () => {
    const r = check(raw, fileResolver());
    expect(r.problems).toEqual([]);
    expect(r.shots.length).toBeGreaterThanOrEqual(8);
  });
  test('resolves to a flat file the player can read: defaults filled, paths absolute', () => {
    const out = resolveShots(raw, '/tmp/out'), cafe = out.shots.find(s => s.name === 'cafe-front-day')!;
    expect(out.schema).toBe('suborned.shots.resolved/1');
    expect(cafe).toMatchObject({ scriptPath: expect.stringMatching(/^\/.*route-a-board\.json$/), tick: 600, focus: 'waitress', hud: true, state: 'rest', settle: 0.35, clipSeconds: 0, yaw: 0 });
    expect(out.shots.some(s => s.clipSeconds > 0)).toBe(true);
    expect(out.shots.find(s => s.name === 'scout-side-pose')).toBeUndefined(); // skip: true is left out
  });
});

describe('what a row may say', () => {
  test('a good row passes', () => { expect(problems({ name: 'a', script: 'route-a-board', tick: 600, focus: 'waitress', yaw: 2 })).toBe(''); });
  test('unknown keys, bad names, a repeated name', () => {
    expect(problems({ name: 'a', colour: 'red' })).toContain('colour');
    expect(problems({ name: 'Bad Name' })).toContain('kebab-case');
    expect(problems({ name: 'a' }, { name: 'a' })).toContain('the name is used twice');
  });
  test('the start must exist and be the mission\'s own', () => {
    expect(problems({ name: 'a', script: 'nope' })).toContain('no route script nope');
    expect(problems({ name: 'a', script: 'fixture' })).toContain('starts from a fixture (fightLab)');
    expect(problems({ name: 'a', checkpoint: 'cp-nope' })).toContain('no checkpoint cp-nope');
    expect(problems({ name: 'a', scenario: 'N999' })).toContain('no engine scenario N999');
    expect(problems({ name: 'a', script: 'route-a-board', checkpoint: 'cp-loud' })).toContain('give one start');
    expect(problems({ name: 'a', preset: 'lab' })).toContain('preset goes with a checkpoint');
    expect(problems({ name: 'a', script: 'route-a-board', tick: 9000 })).toContain('past the end of its script (5100)');
  });
  test('the camera: follow, focus, pose and their conflicts', () => {
    expect(problems({ name: 'a', view: 'follow', focus: 'waitress' })).toContain('use face, not focus or at');
    expect(problems({ name: 'a', face: 'waitress' })).toContain('face goes with the follow view');
    expect(problems({ name: 'a', focus: 'x', at: [0, 0] })).toContain('focus and at both say');
    expect(problems({ name: 'a', hud: true, pose: { pos: [0, 1, 0], look: [1, 1, 1] } })).toContain('hud must be false');
    expect(problems({ name: 'a', hud: false, pose: { pos: [0, 1, 0] } })).toContain('pos and look go together');
    expect(problems({ name: 'a', hud: false, pose: { pos: [0, 1, 0], look: [0, 0, 0], view: 'op/scout', offset: [1, 1, 1] } })).toContain('pos and look, or view');
    expect(problems({ name: 'a', hud: false, yaw: 1, pose: { pos: [0, 1, 0], look: [0, 0, 0] } })).toContain('its own camera');
    expect(problems({ name: 'a', hud: false, pose: { pos: [0, 1, 0], look: [0, 0, 0] } })).toBe('');
  });
  test('what the player has done, clips, and the briefing', () => {
    expect(problems({ name: 'a', arm: 'pay' })).toContain('arm needs select');
    expect(problems({ name: 'a', hud: false, state: 'active' })).toContain('state is the HUD budget');
    expect(problems({ name: 'a', clip: { seconds: 3 }, perf: 4 })).toContain('give one');
    expect(problems({ name: 'a', clip: { seconds: 60, fps: 60 } })).toContain('over 1800 frames');
    expect(problems({ name: 'a', clip: { seconds: 0.1 } })).toContain('clip');
    expect(problems({ name: 'a', briefing: true, tick: 100 })).toContain('briefing card is up only at tick 0');
    expect(problems({ name: 'a', pointer: [1.5, 0] })).toContain('pointer');
    expect(problems({ name: 'a', yaw: 4 })).toContain('yaw');
  });
  test('a skipped row is not checked, and defaults sit under the row', () => {
    expect(check(file({ name: 'a', skip: true, script: 'nope' }), R).problems).toEqual([]);
    const r = check({ schema: 'suborned.shots/1', defaults: { hud: false }, shots: [{ name: 'a' }, { name: 'b', hud: true }] }, R);
    expect(r.shots.map(s => s.hud)).toEqual([false, true]);
  });
  test('resolveShots throws once, naming every bad shot', () => {
    expect(() => resolveShots(file({ name: 'a', script: 'nope' }, { name: 'b', checkpoint: 'cp-nope' }), '/o', R)).toThrow(/2 problems/);
  });
});

test('the files a good run leaves', () => {
  const out = resolveShots(file({ name: 'a', script: 'route-a-board' }, { name: 'b', hud: false }, { name: 'c', clip: { seconds: 1 } }), '/o', R);
  expect(out.shots.map(expectedFiles)).toEqual([['a.png', 'a.hud.png', 'a.manifest.json'], ['b.jpg'], ['c/f0000.jpg']]);
});

describe('capture size and format (1920 x 1080 by default)', () => {
  const one = (row: object) => resolveShots(file({ name: 'a', ...row }), '/o', R).shots[0]!;
  test('every shot is 1920 x 1080 unless the row gives a size', () => {
    expect(one({})).toMatchObject({ w: 1920, h: 1080 });
    expect(one({ size: [3840, 2160] })).toMatchObject({ w: 3840, h: 2160 });
    expect(problems({ name: 'a', size: [100, 100] })).toContain('size');
    expect(resolveShots(JSON.parse(readFileSync(DEFAULT_FILE, 'utf8')), '/o').shots.every(s => s.w === 1920 && s.h === 1080)).toBe(true);
  });
  test('png where a tool needs it lossless (a HUD shot), jpg for review copies (world-only shots, clips)', () => {
    expect(one({})).toMatchObject({ jpg: false });
    expect(one({ hud: false })).toMatchObject({ jpg: true });
    expect(one({ clip: { seconds: 1 } })).toMatchObject({ jpg: true });
    expect(one({ hud: false, format: 'png' })).toMatchObject({ jpg: false });
  });
  test('a HUD shot cannot be asked for as jpg: the budget reads it lossless', () => {
    expect(problems({ name: 'a', format: 'jpg' })).toContain('a HUD shot is png');
  });
  test('the size is read from a PNG header, and anything else is refused', () => {
    const b = new Uint8Array(24); b.set([0x89, 0x50, 0x4e, 0x47, 13, 10, 26, 10, 0, 0, 0, 13, 0x49, 0x48, 0x44, 0x52]); new DataView(b.buffer).setUint32(16, 1920); new DataView(b.buffer).setUint32(20, 1080);
    expect(pngSize(b)).toEqual([1920, 1080]); expect(pngSize(new Uint8Array(30))).toBeUndefined();
  });
});
