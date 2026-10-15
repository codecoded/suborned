// The promoted cast (Sky; r016 since 2026-10-16, r013 before it) is the accepted cast of its run, byte for byte, and cannot drift from its run silently.
// Nothing here names a run: the record says which, and the tests read it, so a new promotion does not break them.
// Run: bun test contracts/people/promoted.test.ts
import { describe, expect, test } from 'bun:test';
import { cpSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import { castBytes, castFileOf, checkPromotion, promotionFileOf } from '../../tools/people/promoted.ts';
import { sha256 } from '../../tools/gen/sha.ts';

const root = join(import.meta.dir, '..', '..'), PLAN = 'harbour-district';
const rec = JSON.parse(readFileSync(join(root, promotionFileOf(PLAN)), 'utf8'));

/** A scratch repo with the promoted cast, its record and the run's files; `edit` changes them before the check. */
function scratch(edit: (dir: string) => void) {
  const dir = mkdtempSync(join(tmpdir(), 'promoted-'));
  try {
    for (const f of [castFileOf(PLAN), promotionFileOf(PLAN), rec.source.file, rec.source.runFile]) { mkdirSync(dirname(join(dir, f)), { recursive: true }); cpSync(join(root, f), join(dir, f)); }
    edit(dir);
    return checkPromotion(PLAN, dir);
  } finally { rmSync(dir, { recursive: true, force: true }); }
}

describe('the promoted cast', () => {
  test('is its run\'s accepted round byte for byte, held to its record and to the run on disk', () => {
    const r = checkPromotion(PLAN);
    expect(r.problems).toEqual([]); expect(r.ok).toBe(true);
    expect(rec).toMatchObject({ plan: PLAN, source: { outcome: 'ACCEPTED' } });
    expect(rec.promoted).toMatch(/^Sky, 20\d\d-\d\d-\d\d/);
    // the accepted round is the last of the run's rounds, whatever the loop rule's version is (run.json `loop`)
    const record = JSON.parse(readFileSync(join(root, rec.source.runFile), 'utf8')) as { rounds: { round: number }[] };
    expect(rec.source.round).toBe(record.rounds.at(-1)!.round);
    expect(rec.source.file).toBe(`runs/levels/${PLAN}/${rec.source.run}/round-${rec.source.round}/pulse.json`);
    const run = JSON.parse(readFileSync(join(root, rec.source.file), 'utf8')), file = readFileSync(join(root, castFileOf(PLAN)), 'utf8');
    expect(file).toBe(castBytes(run.cast));
    expect(sha256(file)).toBe(rec.cast.sha256);
    expect(sha256(readFileSync(join(root, rec.source.file)))).toBe(rec.source.sha256);
  });
  test('an edit to the cast fails: its sha256 and its bytes against the run', () => {
    const r = scratch(d => { const f = join(d, castFileOf(PLAN)); writeFileSync(f, readFileSync(f, 'utf8').replace('"weight": 60', '"weight": 61')); });
    expect(r.ok).toBe(false); expect(r.problems.join(' | ')).toContain('was edited'); expect(r.problems.join(' | ')).toContain('byte for byte');
  });
  test('a cast with a trailing newline is not the run\'s cast', () => {
    const r = scratch(d => { const f = join(d, castFileOf(PLAN)); writeFileSync(f, readFileSync(f, 'utf8') + '\n'); });
    expect(r.ok).toBe(false); expect(r.problems.join(' | ')).toContain('byte for byte');
  });
  test('a run whose output changed fails', () => {
    const r = scratch(d => { const f = join(d, rec.source.file), o = JSON.parse(readFileSync(f, 'utf8')); o.notes = [...(o.notes ?? []), 'edited']; writeFileSync(f, JSON.stringify(o, null, 2)); });
    expect(r.ok).toBe(false); expect(r.problems.join(' | ')).toContain('the run\'s output changed');
  });
  test('a run that was not accepted, or a different round, fails', () => {
    const r = scratch(d => { const f = join(d, rec.source.runFile), o = JSON.parse(readFileSync(f, 'utf8')); o.outcome = 'REJECTED'; o.rounds.push({ round: o.rounds.at(-1).round + 1 }); writeFileSync(f, JSON.stringify(o)); });
    expect(r.ok).toBe(false); expect(r.problems.join(' | ')).toContain('is REJECTED'); expect(r.problems.join(' | ')).toContain(`round ${rec.source.round + 1}`);
  });
  test('the round is the last of the run\'s rounds, not run.json `loop` (the loop rule\'s version): a loop that differs breaks nothing, one that equals the record\'s round proves nothing', () => {
    const same = scratch(d => { const f = join(d, rec.source.runFile), o = JSON.parse(readFileSync(f, 'utf8')); o.loop = 99; writeFileSync(f, JSON.stringify(o)); });
    expect(same.problems).toEqual([]);
    const forged = scratch(d => { const f = join(d, rec.source.runFile), o = JSON.parse(readFileSync(f, 'utf8')); o.loop = rec.source.round; o.rounds = [...o.rounds, { round: rec.source.round + 1 }]; writeFileSync(f, JSON.stringify(o)); });
    expect(forged.ok).toBe(false); expect(forged.problems.join(' | ')).toContain(`was accepted in round ${rec.source.round + 1}`);
  });
  test('a cast with no record, and a missing run, fail', () => {
    expect((scratch(d => rmSync(join(d, promotionFileOf(PLAN))))).problems[0]).toContain('no promotion record');
    expect((scratch(d => rmSync(join(d, rec.source.file)))).problems.join(' | ')).toContain('is missing');
  });
});
