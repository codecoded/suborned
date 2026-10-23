// The "Sky is playing" guard (playing.ts): which processes count, the wait, the timeout, --force, the flags. Run: bun test tools/game
import { describe, expect, test } from 'bun:test';
import { MESSAGE, parsePlaying, takeGuardFlags, timeoutText, waitForSky, type GuardConfig } from './playing';

const APP = '/Users/sky/Projects/suborned/out/build/play/The Suborned.app/Contents/MacOS/The Suborned';
const cfg = (o: Partial<GuardConfig> = {}): GuardConfig => ({ force: false, timeoutS: 600, pollMs: 5000, ...o });

describe('which processes are Sky playing', () => {
  test('a launch with no flags, or only a person\'s, is Sky', () => {
    const ps = `  101 ${APP}\n  102 ${APP} -windowed\n  103 ${APP} -screen-width 1920 -screen-height 1080 -popupwindow\n  104 ${APP} -psn_0_12345`;
    expect(parsePlaying(ps).map(p => p.pid)).toEqual([101, 102, 103, 104]);
  });
  test('every automation flag is not Sky: proofs, batch rows, probes, headless runs', () => {
    for (const f of ['-gameProof /o -novsync', '-drawCheck /o -windowed', '-late /r.json /o -novsync', '-tplong /r.json /o', '-shots /r.json /o', '-runScenarios /s', '-redline048 /o', '-batchmode -nographics', '-windowed -capture-size 1920x1080', '-windowed -logFile /x.log']) {
      expect(parsePlaying(`  200 ${APP} ${f}`)).toEqual([]);
    }
  });
  test('other processes, the editor, the helper and a path that only mentions the app are not Sky', () => {
    const ps = [
      '  300 /Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity -projectPath /Users/sky/Projects/suborned/unity',
      '  301 tail -f /Users/sky/Library/Logs/x.log', `  302 ${APP} Helper`.replace(' Helper', 'Helper'), '  303 bun tools/game/batch.ts',
    ].join('\n');
    expect(parsePlaying(ps)).toEqual([]);
  });
  test('a copy of the app elsewhere counts the same (Sky may run any copy)', () => {
    expect(parsePlaying('  400 /Users/sky/Applications/The Suborned.app/Contents/MacOS/The Suborned').map(p => p.pid)).toEqual([400]);
  });
});

describe('waiting', () => {
  const ps = (...states: boolean[]) => { let i = 0; return () => (states[Math.min(i++, states.length - 1)] ? `  7 ${APP}` : '  1 launchd'); };
  const clock = () => { let t = 0; return { now: () => t, sleep: async (ms: number) => { t += ms; } }; };

  test('nobody playing: goes on at once and says nothing', async () => {
    const said: string[] = [], w = await waitForSky('x', { cfg: cfg(), ps: ps(false), say: m => said.push(m), ...clock() });
    expect(w).toMatchObject({ playing: false, timedOut: false, waitedS: 0 }); expect(said).toEqual([]);
  });
  test('Sky is playing, then closes the game: says "Sky is playing; waiting", waits, goes on', async () => {
    const said: string[] = [], w = await waitForSky('late-fight', { cfg: cfg(), ps: ps(true, true, true, false), say: m => said.push(m), ...clock() });
    expect(w).toMatchObject({ playing: true, timedOut: false, forced: false }); expect(w.waitedS).toBeGreaterThanOrEqual(10);
    expect(said[0]).toContain(MESSAGE); expect(said[0]).toContain('late-fight'); expect(said[0]).toContain('pid 7'); expect(said.at(-1)).toContain('closed');
  });
  test('Sky plays past the timeout: gives up, does not go on', async () => {
    const said: string[] = [], w = await waitForSky('tp-long', { cfg: cfg({ timeoutS: 30 }), ps: ps(true), say: m => said.push(m), ...clock() });
    expect(w).toMatchObject({ playing: true, timedOut: true }); expect(w.waitedS).toBeGreaterThanOrEqual(30);
    expect(said.some(m => m.includes(MESSAGE))).toBe(true); expect(timeoutText(w)).toContain('Sky was playing'); expect(timeoutText(w)).toContain('--force');
  });
  test('timeout 0: does not wait at all', async () => {
    const w = await waitForSky('x', { cfg: cfg({ timeoutS: 0 }), ps: ps(true), say: () => {}, ...clock() });
    expect(w).toMatchObject({ timedOut: true, waitedS: 0 });
  });
  test('--force goes on while Sky plays, and says so', async () => {
    const said: string[] = [], w = await waitForSky('x', { cfg: cfg({ force: true }), ps: ps(true), say: m => said.push(m), ...clock() });
    expect(w).toMatchObject({ playing: true, forced: true, timedOut: false, waitedS: 0 }); expect(said[0]).toContain('--force');
  });
  test('the message repeats once a minute, not every poll', async () => {
    const said: string[] = [];
    await waitForSky('x', { cfg: cfg({ timeoutS: 200 }), ps: ps(true), say: m => said.push(m), ...clock() });
    expect(said.filter(m => m.includes(MESSAGE)).length).toBe(4); // at 0, 60, 120, 180 s
  });
});

describe('flags', () => {
  test('--force and --wait-timeout are taken out of the arguments', () => {
    const a = ['--dry-run', '--force', '--wait-timeout', '90', '--only', 'sim'], c = cfg();
    takeGuardFlags(a, c);
    expect(a).toEqual(['--dry-run', '--only', 'sim']); expect(c).toMatchObject({ force: true, timeoutS: 90 });
  });
  test('a bad timeout is refused', () => {
    expect(() => takeGuardFlags(['--wait-timeout', 'soon'], cfg())).toThrow('seconds');
    expect(() => takeGuardFlags(['--wait-timeout'], cfg())).toThrow('seconds');
  });
});
