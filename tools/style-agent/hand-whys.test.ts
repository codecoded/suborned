import { describe, expect, test } from 'bun:test';
import { mkdtempSync, readFileSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { WHY_HAND } from '../../contracts/missions/handover/why.schema.ts';
import { root } from '../rag/config';
import { gddNames, readerFor, TEXT_FILE } from './guide';
import { buildShortBriefs, loadShortBriefs } from './short-briefs';
import { applyShort } from './short-apply';
import { checkShort, whyPath } from './short-checks';
import { handSet, HandEdits } from './short-cli';
import { loadShortGuide } from './short-guide';
import { sameRubric, SHORT_NOW, type ShortResult } from './short-types';

// Guide /22 (brief 101, Fable 2026-11-09): the five shorts the lozenge round left weakest, written by hand and scored once by the judge. A why cause marked `hand` is
// kept out of every later writer run and never written by `apply`, as a hand row of the dialogue (guide /17); a hand run scores the family as it plays (guide /18).
// Labelled test data only: no model is called.
const FILE = 'briefs/101/fable/hand-whys.json', text = JSON.parse(readFileSync(join(root, TEXT_FILE), 'utf8')), edits = HandEdits.parse(JSON.parse(readFileSync(join(root, FILE), 'utf8'))).edits;
/** A why item's key and field as the block's path ("alert.woken.variants.vale" → why.alert.woken.variants.vale.short). */
const at = (t: { why: Record<string, unknown> }, key: string) => whyPath(key, 'short', t.why).reduce<unknown>((o, s) => (o as Record<string, unknown>)[s], t.why) as string;
const cause = (t: { why: Record<string, Record<string, unknown>> }, key: string) => { const [g, k] = key.split('.'); return t.why[g!]![k!] as { hand?: string; short: string }; };
const marked = [...new Set(edits.map(e => e.key.split('.').slice(0, 2).join('.')))];
/** text.json as it stood before this round: today's shorts back in place of the hand edits, and no mark. */
const before = () => {
  const t = structuredClone(text);
  for (const k of marked) delete cause(t, k).hand;
  for (const e of edits) { const path = whyPath(e.key, 'short', t.why); let o = t.why; for (const s of path.slice(0, -1)) o = o[s]; o[path.at(-1)!] = e.before; }
  return t;
};
const briefsFrom = (t: unknown, version = SHORT_NOW, hand = false) => { const read = readerFor(root); return buildShortBriefs(f => f === TEXT_FILE ? JSON.stringify(t) : read(f), gddNames(root), version, hand); };

describe('guide /22: why shorts written by hand (brief 101)', () => {
  test('/22 reads as /21 in its rubric, so a /21 run and a /22 run share it', () => {
    const g21 = loadShortGuide(root, 21), g22 = loadShortGuide(root, 22);
    expect(SHORT_NOW).toBeGreaterThanOrEqual(22);
    expect({ ...g21, schema: g22.schema }).toEqual(g22); expect(sameRubric(g21, g22)).toBe(true);
  });
  test('the hand file takes why families, each edit one of today\'s shorts; a hand set is today\'s family with them in place', () => {
    expect(new Set(edits.map(e => e.family))).toEqual(new Set(['why/witnessed', 'why/gunfire']));
    for (const id of ['why/witnessed', 'why/gunfire']) {
      const b = briefsFrom(before(), SHORT_NOW, true).find(x => x.id === id)!, set = handSet(b, edits.map(e => e.judged ? { ...e, after: e.judged.line } : e));
      expect(set.items.map(i => i.key)).toEqual(b.items.map(i => i.key));
      for (const e of edits.filter(x => x.family === id)) expect(set.items.find(i => i.key === e.key)!.fields[0]!.lines).toEqual([e.judged?.line ?? e.after]);
      expect(checkShort(b, set, loadShortGuide(root), { why: before().why })).toEqual([]); // within the lozenge's box and every code check
    }
  });
  test('text.json carries every edit where it ended up, its cause marked; /22 keeps a marked cause out of a writer run and gives its short in the context; /21 and a hand run read it as an item', () => {
    for (const e of edits) { expect(at(text, e.key), e.key).toBe(e.after); expect(at(text, e.key)).not.toBe(e.before); }
    for (const k of marked) expect(cause(text, k).hand?.startsWith(WHY_HAND), k).toBe(true);
    const writer = loadShortBriefs(root), hand = loadShortBriefs(root, SHORT_NOW, true), at21 = loadShortBriefs(root, 21), then = briefsFrom(before());
    for (const e of edits) {
      const ctx = writer.find(b => b.id === e.family)!.context;
      expect(writer.some(b => b.items.some(i => i.key === e.key)), e.key).toBe(false);
      expect(ctx).toContain(`${e.key}: ${JSON.stringify(e.after)}`);
      for (const b of [hand, at21, then]) expect(b.find(x => x.id === e.family)!.items.some(i => i.key === e.key), e.key).toBe(true);
    }
    expect(hand.find(b => b.id === 'why/witnessed')!.context).not.toContain('Shorts Fable wrote by hand');
    expect(then.find(b => b.id === 'why/witnessed')!.context).not.toContain('Shorts Fable wrote by hand'); // no mark, no hand cause
  });
  test('apply never writes a marked cause, and writes it once its mark is gone', async () => {
    const keys = edits.map(e => e.key), raw = readFileSync(join(root, TEXT_FILE), 'utf8'), dir = mkdtempSync(join(tmpdir(), 'suborned-hand-whys-'));
    const run = { brief: { id: 'why/hand-test', kind: 'why', items: keys.map(key => ({ key })) }, startedAt: 0,
      rounds: [{ set: { items: keys.map(key => ({ key, cut: false, reason: '', fields: [{ id: 'short', lines: ['a test short'] }] })) } }] } as unknown as ShortResult;
    const apply = async (t: string) => { const file = join(dir, 'text.json'); writeFileSync(file, t); await applyShort(false, { runs: new Map([[run.brief.id, { run, path: root }]]), file }); return JSON.parse(readFileSync(file, 'utf8')); };
    const held = await apply(raw);
    for (const e of edits) expect(at(held, e.key), e.key).toBe(e.after);
    const one = 'alert.down', unmarked = raw.split('\n').map(l => l.startsWith('   "down": {') ? l.replace(/,"hand":"[^"]*"/, '') : l).join('\n');
    expect(unmarked).not.toBe(raw);
    const written = await apply(unmarked);
    expect(at(written, one)).toBe('a test short'); expect(at(written, 'alert.confirmed')).toBe(edits.find(e => e.key === 'alert.confirmed')!.after);
  });
});
