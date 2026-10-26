// The proof's gates (tools/game/proofgates.ts): r059 gates like r118 (briefs/067/splice.md item 3). Each rule has a failing example.
// Run: bun test tools/game/proofgates.test.ts
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { GATED, judgeGates } from './proofgates';

const root = join(import.meta.dir, '..', '..'), read = (rel: string) => readFileSync(join(root, rel), 'utf8');
const allOk = () => Object.fromEntries(GATED.map(k => [k, { ok: true }])) as Record<string, unknown>;

describe('judgeGates', () => {
  test('r059 is gated, after r118, as the proof writes it', () => {
    expect(GATED).toContain('r059');
    expect(GATED.indexOf('r059')).toBeGreaterThan(GATED.indexOf('r118'));
    expect(judgeGates(allOk())).toMatchObject({ ok: true, failed: [], missing: [], notReached: [] });
  });

  test('a proof whose r059 says ok false fails, and names r059 and what it found (arrive, watch, pickup)', () => {
    const frames = { ...allOk(), r059: { ok: false, arrive: { ok: false, onFootBeforeCar: 3 }, watch: { ok: true } } };
    const g = judgeGates(frames);
    expect(g.ok).toBe(false);
    expect(g.failed).toEqual(['r059']);
    expect(g.lines.find(l => l.startsWith('game: r059 FAILED:'))).toContain('"onFootBeforeCar":3');
  });

  test('r059 null, which the build writes when the section threw or never ran, fails too', () => {
    const g = judgeGates({ ...allOk(), r059: null });
    expect(g.ok).toBe(false);
    expect(g.failed).toEqual(['r059']);
  });

  test('a proof from a build with no Proof059 has no r059: said, not failed', () => {
    const frames = allOk(); delete frames.r059;
    const g = judgeGates(frames);
    expect(g.ok).toBe(true);
    expect(g.missing).toEqual(['r059']);
    expect(g.lines).toContain('game: r059: not in this proof (an older build)');
  });

  test('ok null (the section was not reached: no hostile in range, say) is reported and goes on, as before', () => {
    const g = judgeGates({ ...allOk(), fire: { ok: null, why: 'no hostile in range' } });
    expect(g.ok).toBe(true);
    expect(g.notReached).toEqual(['fire']);
    expect(g.lines.find(l => l.startsWith('game: fire not reached:'))).toContain('no hostile in range');
  });

  test('r118 still gates, and so does every other section: a false anywhere fails', () => {
    for (const k of GATED) { const g = judgeGates({ ...allOk(), [k]: { ok: false } }); expect(g.ok, k).toBe(false); expect(g.failed).toEqual([k]); }
  });

  test('no proof at all (no frames) fails nothing here: the proof status and the scenarios say it', () => {
    expect(judgeGates(undefined).ok).toBe(true);
    expect(judgeGates(undefined).missing.length).toBe(GATED.length);
  });
});

describe('the player writes what the gates read', () => {
  const boot = read('unity/Assets/Suborned/Runtime/Play/GameBoot.cs');
  test('GameBoot.cs puts every gated section into the proof\'s frames (a gate on a key it never writes would read as "an older build" for ever)', () => {
    for (const k of GATED) expect(boot.includes(`\\"${k}\\": `), `${k} in the frames line of GameBoot.cs`).toBe(true);
  });

  test('Proof059.cs ends with the object r059 gates on: {"ok": …} over its arrive, watch and pickup parts', () => {
    const p = read('unity/Assets/Suborned/Runtime/Play/Proof059.cs');
    expect(p).toContain('json[0] = $"{{\\"ok\\": {B(ok)}, {string.Join(", ", parts)}}}";');
    for (const part of ['arrive', 'watch', 'pickup']) expect(p.includes(`parts.Add($"\\"${part}\\": {{\\"ok\\": `), part).toBe(true);
  });
});

describe('game:check uses them', () => {
  test('tools/game/cli.ts judges the proof with judgeGates and asks the app for its cue file (a section a later build adds is gated by adding it to GATED, nowhere else)', () => {
    const cli = read('tools/game/cli.ts');
    expect(cli).toMatch(/import \{ judgeGates \} from '\.\/proofgates'/);
    expect(cli).toMatch(/judgeGates\(pj\?\.frames\)/);
    expect(cli).toMatch(/appShips\(APP, ROOT\)/);
    expect(cli).not.toMatch(/'r118'\]\)/); // the list lives in proofgates.ts now
  });
});
