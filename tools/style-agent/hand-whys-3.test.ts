import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { WHY_HAND } from '../../contracts/missions/handover/why.schema.ts';
import { root } from '../rag/config';
import { TEXT_FILE } from './guide';
import { loadShortBriefs } from './short-briefs';
import { checkShort, whyPath } from './short-checks';
import { HandEdits } from './short-cli';
import { loadShortGuide } from './short-guide';
import { fillWords, lozengeWidth, noPlace, placeRoom } from './short-lozenge';
import { sameRubric, SHORT_NOW } from './short-types';
import { room } from './short-whys19';

// Guide /24 (brief 102, Fable 2026-11-09): Sky on the loitering card, "Robots coming / an agent stayed too long beside the café · 2:46": "why is that a bad thing? What
// was the agent doing?" The cause now says who saw whom doing what, and the host adds no place after its short, as after a cut camera's. Labelled test data only.
const FILE = 'briefs/102/fable/hand-whys-3.json', text = JSON.parse(readFileSync(join(root, TEXT_FILE), 'utf8'));
const edits = HandEdits.parse(JSON.parse(readFileSync(join(root, FILE), 'utf8'))).edits;
const at = (key: string, field: string) => whyPath(key, field, text.why).reduce<unknown>((o, s) => (o as Record<string, unknown>)[s], text.why) as string;

describe('guide /24: the loitering card (brief 102)', () => {
  test('/24 reads as /23 in its rubric; only the loiterer\'s room moves, and /19 to /23 keep a place after it', () => {
    expect(SHORT_NOW).toBeGreaterThanOrEqual(24); expect(sameRubric(loadShortGuide(root, 23), loadShortGuide(root, 24))).toBe(true);
    expect(noPlace('loiter', 23)).toBe(false); expect(noPlace('loiter', 24)).toBe(true); expect(noPlace('cut', 23)).toBe(true);
    expect(room('loiter', 23)).toBe(142.9); expect(room('loiter', 24)).toBe(room('cut', 24)); expect(room('seen', 24)).toBe(room('seen', 23));
    const item = loadShortBriefs(root, 24, true).find(b => b.id === 'why/reported')!.items.find(i => i.key === 'alert.loiter')!;
    expect(item.fields.find(f => f.id === 'short')!.lozenge).toBe(room('loiter', 24)); expect(item.tells).toContain('The host adds no place after this short');
  });
  test('the record is one cause\'s title, short and line; text.json carries each, the cause marked; a writer run leaves it out and gives its short', () => {
    expect(edits.map(e => `${e.family} ${e.key} ${e.field}`)).toEqual(['title', 'short', 'line'].map(f => `why/reported alert.loiter ${f}`));
    for (const e of edits) { expect(at(e.key, e.field!)).toBe(e.after!); expect(at(e.key, e.field!)).not.toBe(e.before); }
    expect(text.why.alert.loiter.hand.startsWith(WHY_HAND)).toBe(true); expect(text.why.alert.loiter.hand).toContain('hand-whys-3.json');
    const writer = loadShortBriefs(root).find(b => b.id === 'why/reported')!;
    expect(writer.items.some(i => i.key === 'alert.loiter')).toBe(false); expect(writer.context).toContain(`alert.loiter: "${text.why.alert.loiter.short}"`);
  });
  test('it says who saw whom doing what, explains no rule, and fits the lozenge at its widest; the mast camera is named for wherever it stands', () => {
    const c = text.why.alert.loiter as { title: string; short: string; line: string };
    expect(c.short).toMatch(/^\{who\} saw \{agent\} /); expect(c.line).toMatch(/^\{who\} saw \{agent\} .*\{where\}\.$/);
    for (const x of [c.title, c.short, c.line]) expect(x).not.toMatch(/too long|in sight|staying|reported|for /i);
    const words = fillWords(text), wide = lozengeWidth(words, 'alert', 'loiter', c.short, placeRoom(24), noPlace('loiter', 24));
    expect(wide.filled).toBe('the entrance camera saw the Enforcer idling'); expect(wide.px).toBeLessThanOrEqual(296);
    expect(text.names.targets['camera-mast-z0']).toBe('the mast camera'); expect(text.names.targets['camera-mast-z0']).not.toMatch(/square|café|Office|street/);
    const b = loadShortBriefs(root, SHORT_NOW, true).find(x => x.id === 'why/reported')!;
    const set = { items: b.items.map(i => ({ key: i.key, cut: false, reason: '', fields: i.fields.map(f => ({ id: f.id, lines: i.current[f.id] ?? [] })) })) };
    expect(checkShort(b, set, loadShortGuide(root, SHORT_NOW), { why: text.why }).filter(x => x.reason.includes('loiter'))).toEqual([]);
  });
});
