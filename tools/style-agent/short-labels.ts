import type { Field, Fill, Form, Pattern, ShortBrief, ShortGuideVersion, ShortItem, ShortSet } from './short-types';

/** Guide /10 (brief 101, Sky 2026-11-08, the HUD sweep's mix): the words the new HUD draws, one family. The top right takes Street labels' style (the clock and the
 *  seal as ink labels, the next key event's word before its clock time); the bottom right takes Corner tokens' news lozenge and Book roundel (a card's title over
 *  its line and the clock it came at, "1 of n ›" under it; the roundel has no word); the street layer heads the first shot's row with a label. Built only from
 *  text.json, so a run rebuilds from its archive. Today's words are each item's `today`; `less.street.loud` is the one new key. Left out on purpose: the police
 *  card (its title is the why block's, held by brief 100's tests, and its arrival sits on the ticket as a bare time), the seal card (never a card since
 *  2026-11-05), the seen card's line (the host clears it), the fix chips (no room on the lozenge) and the why block (its own families). */
const field = (id: string, form: Form, words: number, chars: number, fills: Fill[] = [], patterns: Pattern[] = []): Field => ({ id, form, words, chars, count: 1, fills, keep: [], patterns });
const fill = (id: string, means: string, widest: string): Fill => ({ id, means, widest });
const STAYS = 'The HUD draws it (brief 101): Latch reads it by this key.';
/** Sky's own words, which no run replaces (an item's key, its field): "Turn driver by" over the loop's "Driver cutoff" (Sky 2026-11-08, 0184fef4). From /12 the brief keeps
 *  them word for word, and `apply` writes them whatever a run of any version says. */
export const SKY_PICKS: Record<string, Record<string, string>> = { 'briefing.timeline.5:20': { short: 'Turn driver by' } };

const STREET = 'The street layer, by the target (Glyph\'s S-aim and T-aim mocks): when the next shot would turn a quiet run loud, a small orange pill with this label heads a row, then the Alert lamps (to 2), the police badge (police come), the seal\'s lock (the district seals) and the Trail icon with its +n. In the final fight a shot costs nothing new, so there is no row.';
const CORNER = 'The top right, in ink under the clock and the row of Alert lamps, Trail and money (Glyph\'s S mocks): the next key event the cell knows, as an icon, this word, then the event\'s time on the mission clock ("Chime 5:10" while the clock reads 3:28). An event the cell doesn\'t know yet shows "?"; hovered, the corner shows "Next · " and the long form. The time after the word is a clock time, not a countdown: the corner\'s one countdown, the seal\'s, is a bare orange number by a lock. The same word names the mark beside its time on the book\'s start-page clock and in the debrief\'s Time fold, so it also reads as an event in a list of times.';
/** Each item's `where` names its place; the family's context says each place once (the prompt shows every item's `where`). */
const AT = { street: 'The first shot\'s row on the street (the family\'s context).', corner: 'The next key event, top right (the family\'s context).', lozenge: 'The news lozenge, bottom right (the family\'s context).' };
const LOZENGE = 'The bottom right\'s news lozenge, left of the Book roundel (Glyph\'s T mocks): one card at a time, most urgent first, kept until clicked; a click reads it and shows the next, a click on its line pans to the place. The card\'s badge or face sits on an orange roundel at its left end; the title is bold; the line under it is small and dim, followed by " · " and the clock it came at ("their car across our way · 2:46"); "1 of n ›" hangs under it. The lozenge grows with its words, so a shorter line keeps it lean. The same card goes to the book\'s Journal.';
/** The lozenge's slots: a title of four words (GDD §10) and today's widest (24); a line Glyph's mock holds lean (its boxed line is 24 before the clock): 28 at /10,
 *  32 from /11, room for the fills a host test reads (Latch's lozenge clips a line at 300 px, about 38 characters before " · " and the clock). */
const TITLE = (fills: Fill[] = []) => field('title', 'title', 4, 24, fills);
const LINE = (v: ShortGuideVersion, fills: Fill[] = [], patterns: Pattern[] = []) => field('line', 'label', 6, v >= 11 ? 32 : 28, fills, v >= 11 ? patterns : []);
/** Guide /11: what brief 096's host tests read on a card's line (tools/game/hostlogic), held by pattern so the loop can't drop it. */
const HELD = (re: string, why: string): Pattern => ({ re, flags: '', must: true, why });
const HEALTH = HELD('\\{n\\} health', 'the health left shows live as "{n} health" (HostLogic.Book096.cs reads it)');

type Card = [kind: string, tells: string, title: Fill[], line: Fill[] | null, held?: Pattern[], also?: string];

export function labelBriefs(text: any, v: ShortGuideVersion): ShortBrief[] {
  const cards = text.book.feed.cards as Record<string, { title: string; line?: string }>, timeline = text.briefing.timeline as { at: string; label: string; short: string; known: boolean }[];
  const cell = Object.values(text.book.cell.name as Record<string, string>).sort((a, b) => b.length - a.length)[0]!;
  const agent = fill('agent', 'an agent of the cell, by the name the book\'s cell page gives them, beside their face', cell);
  const who = fill('who', 'the person the card is about, as the HUD names them, capitalised: Inez, Popovic, The driver, The guard', 'The driver');
  const item = (key: string, where: string, tells: string, fields: Field[], current: Record<string, string | undefined>): ShortItem => ({ key, where, tells, speaker: null, fields,
    current: Object.fromEntries(Object.entries(current).filter((x): x is [string, string] => !!x[1]).map(([k, v]) => [k, [v]])), stays: STAYS });
  const CARDS: Card[] = [
    ['down', 'An agent of the cell has gone down in a fight; the roundel shows their hurt face, and the card leaves once they are up. No line under the title.', [agent], null],
    ['hurt-carry', 'The most urgent card: the Enforcer is badly hurt (log enforcer.warning; {n} is his health left, of 8) while he carries Popovic. The line says what the rest of the cell should do: cover him. The Journal\'s card for the same log says "The Enforcer is badly hurt" and "{n} health left. Get him into cover or out of the line."', [agent], [fill('n', 'the Enforcer\'s health left (of 8)', '8')], [HEALTH, HELD('Popovic', 'the line names Popovic (HostLogic.Book096.cs reads it)')],
      ' The line shows his health left, live, as "{n} health", and names Popovic: a host test reads both; what the cell should do follows if it fits.'],
    ['hurt', 'The Enforcer is badly hurt (log enforcer.warning; {n} is his health left, of 8) and not carrying anyone. The line says what to do: get him into cover. The Journal\'s card for the same log says "The Enforcer is badly hurt" and "{n} health left. Get him into cover or out of the line."', [agent], [fill('n', 'the Enforcer\'s health left (of 8)', '8')], [HEALTH], ' The line shows his health left, live, as "{n} health", as the carrying card does.'],
    ['for-car', 'Rivals are making for our getaway car while Popovic is in it and nobody of the cell outside it has a gun drawn (log rivals.for-car); the first to reach it takes him. {n} counts them. The Journal\'s card says "Rivals moving on the car". (Its fix chips, Step out and draw or down them, are not in this family.)', [], [fill('n', 'how many rivals are going for the car', '5')]],
    ['alert', 'The Alert rose; the lozenge shows the lamps beside the title, and the line is the why block\'s cause (not in this family). No line of its own.', [], null],
    ['seen', 'A watcher saw one of the cell do something traceable, and the Trail rose; the roundel shows the eye. The host draws no line under it.', [], null],
    ['target-out', 'Popovic walks out of the Office (log vale.exits, 5:30 on the timeline) towards his car; the roundel shows his badge, and a click on the line pans to him. The line says which door: the official door.', [], []],
    ['rival-car', 'The rival car is on its way in (log rivals.coming, 2:35): it turns in off Orbit Walk, out of sight, after Popovic too; the roundel shows the rivals\' badge. The line says where it comes from. The Journal\'s card says "A rival car turns in" and "Off Orbit Walk."', [], []],
    ['turned', 'A turn landed: {who} (Inez, the driver, the guard) is now the cell\'s, turned in the glossary\'s word. No line.', [who], null],
    ['refused', 'A turn failed: {who} refused. No line.', [who], null],
    ['body', 'A report landed that someone found a downed person the cell left, dead or knocked out (log report.confirmed); the Journal\'s card says "Body found" with "found the body" or "found the knocked-out body". No line.', [], null],
    ['vale-out', 'A car of the cell\'s has left with Popovic (log vale.out): the win is fixed, and the mission runs on until the police close in at {police} on the mission clock; anyone not away in a car by then is captured. The line says to get the rest of the cell clear by then ("the rest get clear" is the glossary\'s phrase).', [], [fill('police', 'when the police close in, a time on the mission clock, m:ss', '10:30')], [HELD('\\{police\\}', 'the line says when the police close in (HostLogic.Lend096.cs reads the time)')],
      ' The time the police close in is a key event\'s time, so the line carries it through {police}: the moment the rest must be clear by. A host test reads it; the countdown under the clock shows the same moment.'],
    ['lent', 'Popovic\'s own car is now the cell\'s, the Enforcer at its wheel (log lend.lent); its Go takes Popovic away and wins. The line says when to press Go: once the way out is clear.', [], []],
    ['lookout', 'The rivals posted a lookout (log rivals.lookout-posted): if Popovic\'s car leaves he follows it and reaches him first, so the cell deals with him before then. (His fix chips, Mislead, Pay and Take, are not in this family.)', [], []],
    ['boxed', 'The rival car has stopped across our getaway car\'s way out (log rivals.block, or rivals.back when it returns), so our car can\'t drive off until that changes; "boxed in" is the glossary\'s word (a car across a way out). Glyph\'s lozenge mock shortened today\'s line to "their car across our way".', [], []],
  ];
  const items: ShortItem[] = [
    item('less.street.loud', AT.street, 'Firing ends the quiet run: once the shot is fired, the Alert goes to 2, the police are called, the district seals and the shot adds Trail. The icons after the label show each, so the label names the change itself. On Alt, any Fire\'s outcome is the same thing in the same words (less.alt.outcome.loud, today "Goes loud"): one glossary, so this label is written there too. A shot shows no hit chance and no time to down (Sky\'s rule).',
      [field('label', 'title', 2, 12)], { label: text.less.street.loud }),
    ...timeline.map(m => item(`briefing.timeline.${m.at}`, AT.corner, `At ${m.at} on the mission clock: ${m.label}. ${m.known ? 'The briefing tells the cell, so the corner names it once it is the next mark.' : 'Not on the briefing: once it is the next mark, the corner shows "?" until the cell learns it.'}`,
      [{ ...field('short', 'title', 3, 14), keep: v >= 12 && SKY_PICKS[`briefing.timeline.${m.at}`]?.short ? [SKY_PICKS[`briefing.timeline.${m.at}`]!.short!] : [] }], { short: m.short })),
    ...CARDS.map(([k, tells, t, l, held, also]) => item(`book.feed.cards.${k}`, AT.lozenge, v >= 11 && also ? tells + also : tells, l ? [TITLE(t), LINE(v, l, held)] : [TITLE(t)], { title: cards[k]!.title, line: l ? cards[k]!.line : undefined })),
    item('book.feed.of', AT.lozenge, 'Under the lozenge, before a "›" the game draws: this card\'s place in the queue and how many wait; the host always shows the first, so {i} is 1.', [field('label', 'label', 3, 8, [fill('i', 'this card\'s place in the queue', '1'), fill('n', 'how many cards wait, this one included', '12')])], { label: text.book.feed.of }),
  ];
  // a card is briefed with its line where the file has one; the seen card's line is the one left out (HostCrucial clears it)
  for (const [k, , , l] of CARDS) if (!cards[k] || (l !== null) !== (cards[k].line !== undefined && k !== 'seen')) throw new Error(`book.feed.cards.${k}: not the card this family briefs (title, and a line only where it has one)`);
  return [{ id: 'label/hud', kind: 'label', title: 'The HUD\'s labels (brief 101): the first shot\'s row, the next key event, the news lozenge', items,
    context: `Sky picked the HUD on 2026-11-08 as a mix of Glyph's round 2 (brief 101): the top right in Street labels' style, bare ink type with a cream keyline (the clock, the seal's lock and orange count, the Alert lamps, Trail and money, the next key event, the key people's faces); the bottom right Corner tokens' news lozenge and Book roundel; everything else drawn on people and places in the street. It covers half of today's HUD or less. The first shot's row is new; the next key event's words and the lozenge's cards are the file's today, moving to a new place. The three places each item's where names: the first shot's row: ${STREET} The next key event: ${CORNER} The news lozenge: ${LOZENGE}` }];
}

/** Where an item's field is written in text.json: the label kind's keys are paths, a field `label` the key itself, else a key under it. `less.street.loud` is also
 *  the Alt outcome of a Fire (one glossary); a timeline mark is found by its clock. */
export function labelPaths(key: string, id: string, text: any): (string | number)[][] {
  if (key === 'less.street.loud') return [['less', 'street', 'loud'], ['less', 'alt', 'outcome', 'loud']];
  const mark = key.match(/^briefing\.timeline\.(\d+:\d\d)$/);
  if (mark) { const at = (text.briefing.timeline as { at: string }[]).findIndex(x => x.at === mark[1]); if (at < 0) throw new Error(`${key}: no timeline mark at ${mark[1]}`); return [['briefing', 'timeline', at, id]]; }
  return [id === 'label' ? key.split('.') : [...key.split('.'), id]];
}

/** Every string value's span in the raw JSON, by its path (keys and array indices joined by \u0001), so a value is replaced where it stands and the file's layout stays. */
export function stringSpans(raw: string): Map<string, [number, number]> {
  const out = new Map<string, [number, number]>(), path: (string | number)[] = [];
  let i = 0;
  const ws = () => { while (i < raw.length && ' \t\r\n'.includes(raw[i]!)) i++; };
  const str = (): [number, number] => { const s = i++; while (raw[i] !== '"') i += raw[i] === '\\' ? 2 : 1; return [s, ++i]; };
  const value = (): void => {
    ws();
    if (raw[i] === '{' || raw[i] === '[') {
      const obj = raw[i++] === '{', close = obj ? '}' : ']';
      ws(); if (raw[i] === close) { i++; return; }
      for (let n = 0; ; n++) {
        if (obj) { ws(); const [s, e] = str(); ws(); i++; path.push(JSON.parse(raw.slice(s, e)) as string); } else path.push(n);
        value(); path.pop(); ws();
        if (raw[i++] === close) return;
      }
    }
    if (raw[i] === '"') { out.set(path.join('\u0001'), str()); return; }
    while (i < raw.length && !',]} \t\r\n'.includes(raw[i]!)) i++; // a number, true, false or null
  };
  value();
  return out;
}

/** The label families' lines into the raw file where they stand; returns the file and the paths it changed. */
export function writeLabels(raw: string, sets: { id: string; items: ShortSet['items'] }[]): { raw: string; changed: string[] } {
  const spans = stringSpans(raw), text = JSON.parse(raw), edits: { at: [number, number]; to: string; path: string }[] = [];
  for (const { id, items } of sets) for (const x of items) for (const f of x.fields) {
    const line = SKY_PICKS[x.key]?.[f.id] ?? f.lines[0]; // Sky's own words win over any run's
    if (!line) continue;
    for (const p of labelPaths(x.key, f.id, text)) {
      const at = spans.get(p.join('\u0001'));
      if (!at) throw new Error(`${id}: ${x.key} ${f.id} has no string at ${p.join('.')} in text.json`);
      if (JSON.parse(raw.slice(at[0], at[1])) !== line) edits.push({ at, to: JSON.stringify(line), path: p.join('.') });
    }
  }
  for (const e of [...edits].sort((a, b) => b.at[0] - a.at[0])) raw = raw.slice(0, e.at[0]) + e.to + raw.slice(e.at[1]);
  return { raw, changed: edits.map(e => e.path) };
}
