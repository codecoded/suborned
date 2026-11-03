// The audio cue file in the player (briefs/067/splice.md item 2): tools/sim/unity.ts copies contracts/audio/cues.json into StreamingAssets/sim/cues.json after judging it
// (tools/sim/cues.ts), so a player on another Mac plays Chime's cues instead of the values copied into HostAudio.cs. Each rule has a failing example.
// Run: bun test tools/sim/cues.test.ts
import { describe, expect, test } from 'bun:test';
import { mkdirSync, mkdtempSync, readFileSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { Cues } from '../../contracts/audio/cues.schema';
import { IGNORED_INPUTS } from '../game/fingerprint';
import { CLIP_ROOT, CUES, judgeCues } from './cues';

const root = join(import.meta.dir, '..', '..'), read = (rel: string) => readFileSync(join(root, rel), 'utf8');
const live = read(CUES), liveJson = () => JSON.parse(live) as { buses: Record<string, unknown>; cues: { id: string; clip: string; bus: string; volume: number }[]; loops: { id: string; clip: string; bus: string }[] };
const clipRoot = join(root, CLIP_ROOT);

/** A scratch Resources folder holding the clips a cue file names (empty files: only their presence is judged). */
function clipsFor(names: string[]): string {
  const dir = mkdtempSync(join(tmpdir(), 'cues-'));
  for (const n of names) { mkdirSync(join(dir, n, '..'), { recursive: true }); writeFileSync(join(dir, `${n}.wav`), ''); }
  return dir;
}

describe('judgeCues', () => {
  test('the committed cue file is fit to ship: it holds its schema, names only its buses, and every clip is in the Unity project', () => {
    const j = judgeCues(live, Cues, clipRoot, 24);
    expect(j.problems).toEqual([]);
    expect(j.ok).toBe(true);
    expect(j.cues).toBeGreaterThanOrEqual(1);
    expect(j.clips).toEqual(expect.arrayContaining(['Audio/rivals_coming', 'Audio/rival_car_engine']));
  });

  test('a cue whose clip is not in the Unity project fails, naming the cue and the clip (it would play nothing and say nothing)', () => {
    const c = liveJson(); c.cues[0]!.clip = 'Audio/not_made_yet';
    const j = judgeCues(JSON.stringify(c), Cues, clipRoot, 24);
    expect(j.ok).toBe(false);
    expect(j.problems.join('\n')).toMatch(new RegExp(`${c.cues[0]!.id}: clip Audio/not_made_yet is not in`));
  });

  test('the clips are looked for under the folder the host loads from: all there passes, one short fails', () => {
    const c = liveJson(), names = [...new Set([...c.cues, ...c.loops].map(e => e.clip))];
    expect(judgeCues(live, Cues, clipsFor(names), 24).ok).toBe(true);
    expect(judgeCues(live, Cues, clipsFor(names.slice(1)), 24).ok).toBe(false);
  });

  test('a bus a cue names but the file does not have fails', () => {
    const c = liveJson(); c.cues[0]!.bus = 'ambience';
    const j = judgeCues(JSON.stringify(c), Cues, clipRoot, 24);
    expect(j.ok).toBe(false);
    expect(j.problems.join('\n')).toContain(`${c.cues[0]!.id}: bus ambience is not in buses`);
  });

  test('the schema holds the file: a volume over 1, an unknown key, a wrong schema id', () => {
    const loud = liveJson(); loud.cues[0]!.volume = 2;
    expect(judgeCues(JSON.stringify(loud), Cues, clipRoot, 24).problems.join('\n')).toMatch(/cues\.json cues\.0\.volume/);
    const extra = { ...liveJson(), surprise: true };
    expect(judgeCues(JSON.stringify(extra), Cues, clipRoot, 24).ok).toBe(false);
    const other = { ...liveJson(), schema: 'suborned.audio-cues/2' };
    expect(judgeCues(JSON.stringify(other), Cues, clipRoot, 24).ok).toBe(false);
  });

  test('text that is not JSON, or nests deeper than the host reads, fails with its own words', () => {
    expect(judgeCues('{ not json', Cues, clipRoot, 24).problems[0]).toMatch(/is not JSON/);
    const deep = judgeCues(live, Cues, clipRoot, 2);
    expect(deep.ok).toBe(false);
    expect(deep.problems[0]).toMatch(/nests \d+ deep \(> 2\)/);
  });
});

describe('the sim step and the host agree on the file', () => {
  test('tools/sim/unity.ts copies the cue file into StreamingAssets/sim/ and names it in engine.json; HostAudio.cs reads that very path', () => {
    const step = read('tools/sim/unity.ts'), host = read('unity/Assets/Suborned/Runtime/Play/HostAudio.cs');
    expect(step).toMatch(/writeFileSync\(join\(stream, 'cues\.json'\), body\)/);
    expect(step).toMatch(/dllSha256: dllSha, inputs, range, text, cues,/);
    expect(host).toMatch(/Combine\(Application\.streamingAssetsPath, "sim", "cues\.json"\)/);
  });

  test('the build fingerprint reads what the sim step stages in StreamingAssets, so a changed cue file makes the app stale (tools/game/fingerprint.ts; its own tests are beside it)', () => {
    expect(IGNORED_INPUTS).toContain('unity/Assets/StreamingAssets');
    expect(read('tools/game/cli.ts')).toMatch(/import \{[^}]*ignoredFiles[^}]*\} from '\.\/fingerprint'/);
  });
});
