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
import { CARD_NAMES } from './short-slip23';
import type { ShortResult } from './short-types';

// Guide /23 (brief 101, Fable 2026-11-09): the why block's titles and lines to brief 102's bar (no rule of the game, no AI-sounding word), the Opus judge's findings on
// them, and Aster's two questions from Glyph's round 3 (the take-control pop, Inez's slip). Labelled test data only: no model is called.
const FILE = 'briefs/101/fable/hand-whys-2.json', text = JSON.parse(readFileSync(join(root, TEXT_FILE), 'utf8'));
const edits = HandEdits.parse(JSON.parse(readFileSync(join(root, FILE), 'utf8'))).edits, why = edits.filter(e => e.family.startsWith('why/')), cut = edits.find(e => e.cut)!;
const at = (t: { why: Record<string, unknown> }, key: string, field: string) => whyPath(key, field, t.why).reduce<unknown>((o, s) => (o as Record<string, unknown>)[s], t.why) as string;
/** The cause or way an edit is in: what carries the mark. */
const unit = (t: any, key: string) => { const p = key.split('.'); return p[0] === 'onWay' ? t.why.onWay.ways[p[2]!] : t.why[p[0]!][p[1]!]; };
/** The causes round 1 marked (briefs/101/fable/hand-whys.json): before this round they were marked already. */
const ROUND1 = new Set(['alert.down', 'alert.woken', 'alert.confirmed', 'alert.warned']);
/** text.json as it stood before this round: each line back at its field, and the marks this round added gone. */
const before = () => {
  const t = structuredClone(text);
  for (const e of [...why].reverse()) {
    const p = whyPath(e.key, e.field!, t.why); let o = t.why; for (const s of p.slice(0, -1)) o = o[s]; o[p.at(-1)!] = e.before;
    if (!ROUND1.has(e.key.split('.').slice(0, 2).join('.'))) delete unit(t, e.key).hand;
  }
  return t;
};
const briefsFrom = (t: unknown, version: 22 | 23, hand: boolean) => { const read = readerFor(root); return buildShortBriefs(f => f === TEXT_FILE ? JSON.stringify(t) : read(f), gddNames(root), version, hand); };
const plain = (g: ReturnType<typeof loadShortGuide>) => g.kinds.why.rules.find(r => r.id === 'PLAIN')!.instruction;

describe('guide /23: the why block\'s titles and lines by hand (brief 101)', () => {
  test('/23 reads as /22 but for PLAIN, which holds a title and a line to what happened, as it held a short', () => {
    const g22 = loadShortGuide(root, 22), g23 = loadShortGuide(root, 23);
    expect(plain(g22)).toContain('A short says what happened, never a rule of the game');
    expect(plain(g23)).toContain('A title, a short or a line says what happened, plainly, never a rule of the game');
    const rules = (g: typeof g22) => g.kinds.why.rules.map(r => r.id === 'PLAIN' ? { ...r, instruction: '' } : r);
    expect(rules(g23)).toEqual(rules(g22));
    expect({ ...g22, schema: g23.schema, kinds: { ...g22.kinds, why: g23.kinds.why } }).toEqual(g23);
  });
  test('a /23 hand run\'s cause families carry each cause\'s title, short and line, told as the engine plays them; a writer run\'s are /22\'s', () => {
    const hand = loadShortBriefs(root, 23, true), w = hand.find(b => b.id === 'why/witnessed')!, ids = (key: string) => w.items.find(i => i.key === key)!.fields.map(f => f.id);
    expect(ids('alert.seen')).toEqual(['title', 'short', 'line']);
    expect(ids('alert.seen.variants.vale')).toEqual(['short', 'line']); // a variant's title is its base's
    expect(ids('alert.carry.variants.vale')).toEqual(['title', 'short', 'line']);
    expect(w.items.find(i => i.key === 'alert.confirmed')!.tells).toContain('a robot sent to look, the patrol, an officer or the guard');
    expect(hand.find(b => b.id === 'why/gunfire')!.items.find(i => i.key === 'alert.warned')!.fields[0]!.fills.find(f => f.id === 'who')!.means).toContain('never a camera, the patrol or a robot');
    expect(w.context).toContain('This hand run scores each cause\'s title, short and line');
    for (const b of loadShortBriefs(root, 23).filter(x => x.kind === 'why' && !['why/on-the-way', 'why/words', 'why/places'].includes(x.id)))
      for (const i of b.items) expect(i.fields.map(f => f.id), i.key).toEqual(['short']); // a writer run writes the shorts only, as at /19
  });
  test('the two hand families from Glyph\'s round 3 are in a /23 hand run only', () => {
    const has = (bs: { id: string }[]) => ['label/inez-slip', 'label/take-control'].map(id => bs.some(b => b.id === id));
    expect(has(loadShortBriefs(root, 23, true))).toEqual([true, true]);
    expect(has(loadShortBriefs(root, 23))).toEqual([false, false]);
    expect(has(loadShortBriefs(root, 22, true))).toEqual([false, false]);
  });
  test('each edit is one of the lines its family\'s hand run read; the words in text.json pass every /23 code check; today\'s confirmed fails the AI-word check', () => {
    const then = before(), hand = briefsFrom(then, 23, true), guide = loadShortGuide(root, 23);
    for (const e of edits) expect(hand.find(b => b.id === e.family)!.items.find(i => i.key === e.key)?.current[e.field!], `${e.key}.${e.field}`).toContain(e.before);
    for (const id of [...new Set(why.map(e => e.family))]) {
      const b = loadShortBriefs(root, 23, true).find(x => x.id === id)!, now = { items: b.items.map(i => ({ key: i.key, cut: false, reason: '', fields: i.fields.map(f => ({ id: f.id, lines: i.current[f.id] ?? [] })) })) };
      expect(checkShort(b, now, guide, { why: text.why, dialogue: text.dialogue }), id).toEqual([]); // the limits at the widest fills, the lozenge, held words, AI words
    }
    const w = hand.find(b => b.id === 'why/witnessed')!, today = handSet(w, why.filter(e => e.family === 'why/witnessed').map(e => ({ ...e, after: e.before })));
    const ai = checkShort(w, today, guide, { why: then.why }).filter(x => x.reason.includes('AI-sounding')).map(x => x.reason.split(':')[0]);
    expect(ai).toEqual(['alert.confirmed.title', 'alert.confirmed.line']);
  });
  test('text.json carries every edit at its field, each cause and way marked; a writer run leaves them out and gives their words', () => {
    for (const e of why) { expect(at(text, e.key, e.field!), `${e.key}.${e.field}`).toBe(e.after!); expect(unit(text, e.key).hand?.startsWith(WHY_HAND), e.key).toBe(true); }
    const writer = loadShortBriefs(root, 23), family = (id: string) => writer.find(b => b.id === id)!;
    expect(family('why/gunfire').items.some(i => i.key === 'police.gang')).toBe(false);
    expect(family('why/gunfire').context).toContain('police.gang: "nobody saw who fired"');
    expect(family('why/on-the-way').items.some(i => i.key === 'onWay.ways.checkin')).toBe(false);
    expect(family('why/on-the-way').context).toContain(`onWay.ways.checkin: plate "Check-in due · {s} s", line ${JSON.stringify(at(text, 'onWay.ways.checkin', 'line'))}`);
    const w22 = loadShortBriefs(root, 22), same = (id: string) => expect(family(id), id).toEqual(w22.find(b => b.id === id)!);
    for (const b of writer) if (b.id !== 'why/on-the-way') same(b.id); // /23's writer families are /22's, but the held ways
  });
  test('apply never writes a marked cause or way, and writes it once its mark is gone', async () => {
    const raw = readFileSync(join(root, TEXT_FILE), 'utf8'), dir = mkdtempSync(join(tmpdir(), 'suborned-hand-whys-2-'));
    const run = { brief: { id: 'why/hand-test-23', kind: 'why', items: [{ key: 'onWay.ways.checkin' }, { key: 'police.gang' }] }, startedAt: 0,
      rounds: [{ set: { items: [{ key: 'onWay.ways.checkin', cut: false, reason: '', fields: [{ id: 'line', lines: ['A test line.'] }] },
        { key: 'police.gang', cut: false, reason: '', fields: [{ id: 'line', lines: ['Another test line.'] }] }] } }] } as unknown as ShortResult;
    const apply = async (t: string) => { const file = join(dir, 'text.json'); writeFileSync(file, t); await applyShort(false, { runs: new Map([[run.brief.id, { run, path: root }]]), file }); return JSON.parse(readFileSync(file, 'utf8')); };
    const held = await apply(raw);
    expect(at(held, 'onWay.ways.checkin', 'line')).toBe(at(text, 'onWay.ways.checkin', 'line')); expect(at(held, 'police.gang', 'line')).toBe(at(text, 'police.gang', 'line'));
    const unmarked = raw.split('\n').map(l => l.startsWith('    "checkin": {') ? l.replace(/,"hand":"[^"]*"/, '') : l).join('\n');
    expect(unmarked).not.toBe(raw);
    const written = await apply(unmarked);
    expect(at(written, 'onWay.ways.checkin', 'line')).toBe('A test line.'); expect(at(written, 'police.gang', 'line')).toBe(at(text, 'police.gang', 'line'));
  });
  test('the take-control pop: cut by hand, and its word stays while the host still pops it (a pop with no word prints its key)', () => {
    expect([cut.key, cut.family, cut.after]).toEqual(['less.pop.take-control', 'label/take-control', undefined]);
    const set = handSet(loadShortBriefs(root, 23, true).find(b => b.id === 'label/take-control')!, [cut]);
    expect(set.items).toEqual([{ key: 'less.pop.take-control', cut: true, reason: cut.why, fields: [{ id: 'label', lines: [] }] }]);
    const pops = readFileSync(join(root, 'unity/Assets/Suborned/Runtime/Play/HostDrive.cs'), 'utf8').includes('PopKey("take-control"');
    if (pops) expect(text.less.pop['take-control'], 'the host still pops it: its word stays until Latch drops PopKey("take-control"), then Fable removes it').toBeTruthy();
  });
  test('Inez\'s slip: each row\'s label word is the part before its colon, which the slip draws as its icon or its capsule', () => {
    const c = (text.scenes.conversations.items as { id: string; learned: string[]; opens: string[] }[]).find(x => x.id === 'listen-inez')!;
    expect(c.learned.map(l => l.split(': ')[0])).toEqual(['Debt', 'Secret']);
    expect(c.opens.map(l => l.split(': ')[0])).toEqual(['Help', 'Pressure']);
    expect(new Set(Object.values(text.scenes.conversations.names as Record<string, string>))).toEqual(new Set(CARD_NAMES)); // the names a card row may say
  });
});
