// The batch's sim step sees what tools/sim/unity.ts reads (Latch, 2026-11-11): its gate (tools/game/proofdelta.ts stepGate over tools/game/proofmap.json) skipped a change to
// unity.ts with a new input it reads, contracts/audio/weather.json (Chime's brief 107 rows, joined into StreamingAssets/sim/sound.json), so a --sky would have shipped the
// old 38-cue sound.json. Here every repo path the step names (in unity.ts and the local modules it imports, found in the source, not a hand list) and the text schema's
// imports must be on a proofmap row that the snapshot hashes and whose steps run the sim step. A new inline read fails here until its row says so.
import { describe, expect, test } from 'bun:test';
import { existsSync, readFileSync, statSync } from 'node:fs';
import { join, posix } from 'node:path';
import { GATED_STEPS, classify, emptyLedger, loadMap, recordSteps, snapshotOf, stepGate, type MapRow } from '../game/proofdelta';

const ROOT = join(import.meta.dir, '../..'), STEP = 'sim', m = loadMap();
const TEXT = 'contracts/missions/handover/text.json', TEXT_SCHEMA = 'contracts/missions/handover/text.schema.ts', MISSION = 'contracts/missions/handover/mission.json';
/** Reads another gated step answers for, or none needs to: each with why. A stale entry (no longer read) fails too. */
const OTHER: Record<string, { step?: string; why: string }> = {
  [TEXT]: { step: 'words', why: 'the words step (tools/game/words.ts) judges and writes the same StreamingAssets/sim/text.json, with no engine build' },
  [TEXT_SCHEMA]: { step: 'words', why: 'the words step, as text.json' },
  'contracts/missions/handover/routes': { why: 'read only with --routes (the hand-played comparison); the batch runs `unity.ts --tree`' },
  'unity/Assets/Suborned/Resources': { why: 'the clips are only checked to exist (tools/sim/cues.ts judgeCues), never copied; their own rows run r059 and r083' },
};
/** A file the snapshot keys by field (proofdelta missionParts): the part the step reads. */
const AS: Record<string, string> = { [MISSION]: `${MISSION}#listenPartners` };

/** unity.ts and the tools/sim modules it imports. */
function sources(): string[] {
  const out: string[] = [], todo = ['tools/sim/unity.ts'];
  while (todo.length) {
    const f = todo.pop()!; if (out.includes(f)) continue; out.push(f);
    for (const [, spec] of readFileSync(join(ROOT, f), 'utf8').matchAll(/\bfrom\s+['"](\.\/[^'"]+)['"]/g)) todo.push(posix.join(posix.dirname(f), spec!.endsWith('.ts') ? spec! : `${spec}.ts`));
  }
  return out;
}
/** Every repo path a source names as a whole string literal. */
function named(): string[] {
  const out = new Set<string>();
  for (const f of sources()) for (const [, p] of readFileSync(join(ROOT, f), 'utf8').matchAll(/['"`]((?:contracts|sim|briefs|tools|unity)\/[^'"`$\s]*)['"`]/g)) out.add(p!);
  return [...out].sort();
}
/** What text.schema.ts imports, and what they import (unity.ts copies them by its relative imports). */
function textImports(): string[] {
  const out: string[] = [], todo = [TEXT_SCHEMA];
  while (todo.length) {
    const f = todo.pop()!;
    for (const [, spec] of readFileSync(join(ROOT, f), 'utf8').matchAll(/\bfrom\s+['"](\.{1,2}\/[^'"]+)['"]/g)) { const d = posix.join(posix.dirname(f), spec!); if (!out.includes(d)) { out.push(d); todo.push(d); } }
  }
  return out.sort();
}
/** The snapshot paths a named path stands for: a tracked folder's files, else the path (a build output under a hashed folder, or a file). */
function filesOf(p: string): string[] {
  if (existsSync(join(ROOT, p)) && statSync(join(ROOT, p)).isDirectory()) {
    const r = Bun.spawnSync(['git', 'ls-files', '-z', '--', p], { cwd: ROOT, stdout: 'pipe' }).stdout.toString().split('\0').filter(Boolean);
    if (r.length) return r;
  }
  return [AS[p] ?? p];
}
const runs = (r: MapRow | undefined, step: string) => !!r && r.run !== 'none' && (r.steps ? r.steps.includes(step) : r.run === 'full');

describe('what tools/sim/unity.ts reads', () => {
  const reads = named();
  test('the scan finds the step\'s inputs (the engine, its inputs, the cues, the sound pack and its weather rows)', () => {
    for (const p of ['sim/Suborned.Sim', 'sim/data/handover.site.json', 'contracts/rules/rulebook.json', 'contracts/audio/cues.json', 'briefs/087/cues.json', 'contracts/audio/weather.json', MISSION, 'contracts/people/handover.seats.json']) expect(reads, p).toContain(p);
  });
  test('every path it reads is hashed by the snapshot and runs the sim step when it changes', () => {
    const miss = reads.filter(p => !OTHER[p]).flatMap(p => filesOf(p).filter(f => !runs(classify(m, f), STEP)).map(f => `${f}: ${JSON.stringify(classify(m, f)?.match[0] ?? 'unplaced')} ${classify(m, f)?.run === 'none' ? '(not hashed)' : `steps ${classify(m, f)?.steps ?? '-'}`}`));
    expect(miss).toEqual([]);
  });
  test('the reads another step answers for are still read, and that step runs on them; the text schema\'s imports too', () => {
    for (const [p, o] of Object.entries(OTHER)) { expect(reads, `${p}: ${o.why}`).toContain(p); if (o.step) expect(runs(classify(m, p), o.step), p).toBe(true); }
    for (const f of textImports()) expect(runs(classify(m, f), 'words') || runs(classify(m, f), STEP), f).toBe(true);
  });
});

describe('the gate, end to end', () => {
  const base = snapshotOf(new Map([['contracts/audio/weather.json', 'a'], ['briefs/087/cues.json', 'b'], ['tools/people/gen.ts', 'c']]));
  const passed = recordSteps(emptyLedger(), base, ['sim'], { version: '0.2.0', commit: 'x' });
  const after = (f: string) => snapshotOf(new Map([...Object.entries(base.files), [f, 'changed']]));
  test('a change to the weather rows or the 087 cues runs the sim step', () => {
    expect(GATED_STEPS).toContain(STEP);
    expect(stepGate(m, after('contracts/audio/weather.json'), passed, STEP).run).toBe(true);
    expect(stepGate(m, after('briefs/087/cues.json'), passed, STEP).run).toBe(true);
  });
  test('the control: nothing changed, or only a path no step reads, skips it', () => {
    expect(stepGate(m, base, passed, STEP).run).toBe(false);
    expect(stepGate(m, after('tools/people/gen.ts'), passed, STEP).run).toBe(false);
  });
});
