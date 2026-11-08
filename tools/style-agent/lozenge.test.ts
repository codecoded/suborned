import { describe, expect, test } from 'bun:test';
import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { root } from '../rag/config';
import { TEXT_FILE } from './guide';
import { loadShortBriefs } from './short-briefs';
import { checkShort, mergeWhy } from './short-checks';
import { loadShortGuide } from './short-guide';
import { ADVANCE, CLOCK, fillWords, LOZENGE, lozengePx, lozengeWidth, NO_PLACE, onLozenge, placeRoom } from './short-lozenge';
import { shortPrompt } from './short-prompts';
import { sameRubric, SHORT_NOW, type ShortBrief, type ShortSet } from './short-types';
import { room } from './short-whys19';

// Guides /19 to /21 (brief 101, Latch's r4): the news lozenge's line held to its box. Labelled test data only: no model is called.
const V = 21, PLACE = placeRoom(V), text = JSON.parse(readFileSync(join(root, TEXT_FILE), 'utf8')), guide = loadShortGuide(root, V), briefs = loadShortBriefs(root, V);
const family = (id: string) => briefs.find(b => b.id === id)!;
const CAUSES = ['why/witnessed', 'why/reported', 'why/gunfire', 'why/police'];
const today = (b: ShortBrief): ShortSet => ({ items: b.items.map(i => ({ key: i.key, cut: false, reason: '', fields: i.fields.map(f => ({ id: f.id, lines: i.current[f.id] ?? [] })) })) });
const edit = (s: ShortSet, key: string, line: string): ShortSet => ({ items: s.items.map(i => i.key !== key ? i : { ...i, fields: i.fields.map(f => ({ ...f, lines: [line] })) }) });
const issues = (b: ShortBrief, s: ShortSet) => checkShort(b, s, guide, { why: text.why }).map(i => `${i.rule} ${i.reason}`);

/** A TrueType font's advance widths by code point (head, hhea, hmtx, cmap format 4 or 12): the check that ADVANCE is the font the HUD draws with. */
function ttfAdvances(buf: Buffer) {
  const u16 = (o: number) => buf.readUInt16BE(o), u32 = (o: number) => buf.readUInt32BE(o), tables = new Map<string, number>();
  for (let i = 0; i < u16(4); i++) tables.set(buf.toString('latin1', 12 + 16 * i, 16 + 16 * i), u32(20 + 16 * i));
  const head = tables.get('head')!, hhea = tables.get('hhea')!, hmtx = tables.get('hmtx')!, cmap = tables.get('cmap')!, metrics = u16(hhea + 34);
  let f12 = -1, f4 = -1;
  for (let i = 0; i < u16(cmap + 2); i++) { const p = u16(cmap + 4 + 8 * i), e = u16(cmap + 6 + 8 * i), o = cmap + u32(cmap + 8 + 8 * i); if (u16(o) === 12 && (p === 0 || e === 10)) f12 = o; if (u16(o) === 4 && (p === 0 || e === 1)) f4 = o; }
  const glyph = (cp: number) => {
    if (f12 >= 0) { for (let i = 0; i < u32(f12 + 12); i++) { const g = f12 + 16 + 12 * i; if (cp >= u32(g) && cp <= u32(g + 4)) return u32(g + 8) + cp - u32(g); } return 0; }
    const seg = u16(f4 + 6) / 2, ends = f4 + 14, starts = ends + 2 * seg + 2, deltas = starts + 2 * seg, ranges = deltas + 2 * seg;
    for (let i = 0; i < seg; i++) {
      if (cp > u16(ends + 2 * i)) continue;
      const s = u16(starts + 2 * i), d = u16(deltas + 2 * i), r = u16(ranges + 2 * i); if (cp < s) return 0;
      if (r === 0) return (cp + d) & 0xffff;
      const g = u16(ranges + 2 * i + r + 2 * (cp - s)); return g === 0 ? 0 : (g + d) & 0xffff;
    }
    return 0;
  };
  return { unitsPerEm: u16(head + 18), advance: (cp: number) => u16(hmtx + 4 * Math.min(glyph(cp), metrics - 1)) };
}

describe('guides /19 to /21: the news lozenge\'s line (brief 101)', () => {
  test('the widths are the HUD\'s font: every advance is Jost Medium\'s, and Latch\'s line measures as his audit did (430 px), a shade wider', () => {
    const buf = readFileSync(join(root, LOZENGE.font)), font = ttfAdvances(buf);
    expect(createHash('sha256').update(buf).digest('hex')).toBe(LOZENGE.sha256);
    expect(font.unitsPerEm).toBe(LOZENGE.unitsPerEm);
    for (const [c, a] of ADVANCE) expect(font.advance(c.codePointAt(0)!), c).toBe(a);
    const latch = lozengePx('the square\'s mast camera saw the Fixer stay too long beside the café · 2:46');
    expect(latch).toBeGreaterThanOrEqual(430); expect(latch).toBeLessThan(430 * 1.005); // no kerning: never narrower than the HUD draws it
    expect(CLOCK).toBe(' · 4:44');
  });
  test('the four cause families write only the short of every why the lozenge shows, each with its room; the title and the line stay', () => {
    const items = CAUSES.flatMap(id => family(id).items), keys = items.map(i => i.key);
    const shown = (['alert', 'police'] as const).flatMap(g => Object.entries(text.why[g] as Record<string, { variants?: Record<string, { short?: string }> }>).filter(([k]) => onLozenge(g, k))
      .flatMap(([k, c]) => [`${g}.${k}`, ...Object.entries(c.variants ?? {}).filter(([, v]) => v.short).map(([v]) => `${g}.${k}.variants.${v}`)]));
    expect(keys.sort()).toEqual(shown.sort());
    for (const i of items) {
      expect(i.fields.map(f => f.id), i.key).toEqual(['short']);
      const k = i.key.split('.')[1]!; expect(i.fields[0]!.lozenge, i.key).toBe(room(k, V));
      expect(i.tells, i.key).toContain('stays as it is');
    }
    expect(room('loiter', V)).toBeLessThan(room('cut', V)); expect(NO_PLACE.has('cut')).toBe(true); // a cause with no place has the place's room too
    expect(SHORT_NOW).toBe(V); expect(room('loiter', 19)).toBe(146.7); expect(room('loiter', 20)).toBe(142.9); expect(room('loiter', 21)).toBe(142.9); // /19's runs rebuild with /19's room
    expect(sameRubric(loadShortGuide(root, 20), guide)).toBe(true); // /21 changes the places' facts only, so a /20 run continues under it
    expect(family('why/places').items.find(i => i.key === 'places.cabinet')!.tells).toContain('"the cabinet" on the label of an act there');
    expect(placeRoom(20)).toBe(lozengePx('at the service drive')); expect(lozengePx('at the square\'s bay')).toBeLessThanOrEqual(placeRoom(20)); // the HUD's stand names fit from /20
    const set = edit(today(family('why/witnessed')), 'alert.seen', 'a takedown seen'), merged = mergeWhy(family('why/witnessed'), set, text.why) as typeof text.why;
    expect(merged.alert.seen).toEqual({ ...text.why.alert.seen, short: 'a takedown seen' });
    expect(family('why/police').items.find(i => i.key === 'police.more')!.fields[0]!.keep).toEqual(['the search goes on']); // the meter's "More police" row (HostLogic.Why100.cs)
  });
  test('/18\'s families rebuild as they ran: title, short and line, the places in why/words, no room', () => {
    const b18 = loadShortBriefs(root, 18), w18 = b18.find(b => b.id === 'why/witnessed')!;
    expect(w18.items[0]!.fields.map(f => f.id)).toEqual(['title', 'short', 'line']);
    expect(w18.items.flatMap(i => i.fields).every(f => f.lozenge === undefined)).toBe(true);
    expect(b18.find(b => b.id === 'why/words')!.items.some(i => i.key.startsWith('places.'))).toBe(true); expect(b18.some(b => b.id === 'why/places')).toBe(false);
    expect(family('why/words').items.some(i => i.key.startsWith('places.'))).toBe(false); expect(family('why/places').items).toHaveLength(Object.keys(text.why.places).length);
  });
  test('the code measures each short at its widest on the lozenge: over its room it fails, within it passes; an AI-sounding word fails a why', () => {
    const b = family('why/reported'), words = fillWords(text), base = today(b);
    const long = issues(b, edit(base, 'alert.loiter', '{who} saw {agent} stay too long'));
    expect(long.some(x => x.startsWith('SHORT alert.loiter.short:') && x.includes('the square\'s mast camera saw the Enforcer stay too long') && x.includes(`has ${room('loiter', V)} px`))).toBe(true);
    const fits = issues(b, edit(base, 'alert.loiter', '{agent} hung about'));
    expect(fits.filter(x => x.includes('alert.loiter'))).toEqual([]);
    expect(lozengeWidth(words, 'alert', 'loiter', '{agent} hung about', PLACE).px).toBeLessThanOrEqual(LOZENGE.box);
    const g = family('why/witnessed'), ai = issues(g, edit(today(g), 'alert.confirmed', 'a killing confirmed'));
    expect(ai.some(x => x.startsWith('PLAIN alert.confirmed.short: "confirmed" is an AI-sounding word'))).toBe(true);
    const places = family('why/places'), wide = issues(places, today(places));
    expect(wide.some(x => x.startsWith('SHORT places.cabinet.label:'))).toBe(lozengePx(text.why.places.cabinet) > PLACE);
    expect(places.items.find(i => i.key === 'places.d:Office entrance')!.fields[0]!.keep).toEqual(['at the Office door']); // the shift guard's radio (HostLogic.Why100.cs)
  });
  test('the writer is told each short\'s room in px and about how many characters', () => {
    const p = shortPrompt('generate', guide, family('why/reported'), null, null);
    expect(p).toContain(`at most ${room('loiter', V)} px on the news lozenge's line with every fill at its widest`);
    expect(p).toContain('is one line of Jost Medium at 13 px in 296 px'); expect(p).toContain(`"lozenge": "at most ${room('staff', V)} px on the news lozenge's line, about 44 characters`);
    expect(guide.sources.find(s => s.id === 'lozenge')!.excerpt).toContain('the news lozenge\'s why lines must fit the box');
  });
  test('today\'s why lines pass the current guide\'s code checks, every family', () => {
    for (const b of briefs.filter(x => x.kind === 'why')) expect(issues(b, today(b)), b.id).toEqual([]);
  });
  test('today\'s text fits the lozenge: every why it shows at its widest fills, every place within its room, every card line', () => {
    const words = fillWords(text), over: string[] = [];
    for (const g of ['alert', 'police'] as const) for (const [k, c] of Object.entries(text.why[g] as Record<string, { short: string; variants?: Record<string, { short?: string }> }>)) {
      if (!onLozenge(g, k)) continue;
      for (const s of [c.short, ...Object.values(c.variants ?? {}).map(v => v.short).filter((x): x is string => !!x)]) {
        const { filled, px } = lozengeWidth(words, g, k, s, PLACE); if (px > LOZENGE.box) over.push(`${g}.${k}: ${px} px "${filled}"`);
      }
    }
    for (const [k, p] of Object.entries(text.why.places as Record<string, string>)) if (lozengePx(p) > PLACE) over.push(`places.${k}: ${lozengePx(p)} px "${p}"`);
    const fills: Record<string, string> = { n: '8', police: '9:59', seal: '9:59' };
    for (const [k, c] of Object.entries(text.book.feed.cards as Record<string, { line?: string }>)) {
      if (!c.line || k === 'seen' || k === 'seal') continue; // the host clears the seen card's line; the seal is never a card
      const line = c.line.split(' · ').filter(s => !s.includes('{s}')).join(' · ').replace(/\{([a-z]+)\}/g, (_, f: string) => fills[f]!);
      if (line && lozengePx(line + CLOCK) > LOZENGE.box) over.push(`book.feed.cards.${k}: ${lozengePx(line + CLOCK)} px "${line}${CLOCK}"`);
    }
    expect(over).toEqual([]);
  });
});
