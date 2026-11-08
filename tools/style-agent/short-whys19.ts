import { CLOCK, fillWords, LOZENGE, lozengePx, lozengeWidth, NO_PLACE, onLozenge, placeRoom, widestFill } from './short-lozenge';
import type { Field, ShortBrief, ShortGuideVersion, ShortItem, ShortKindRules } from './short-types';

/** Guide /19 (brief 101, Latch's r4, 2026-11-09): the why texts the news lozenge shows, held to its box. The lozenge draws a card's title over one line, "{short}
 *  {where} · m:ss": the why's short, the place the host adds after it, and the clock it came at, in a box 296 px wide (about 40 characters for the short and its
 *  place). So the four cause families write only the short, each with the px it may take at its widest fills; the title and the line stay as they are and are the
 *  item's facts (the line is the long form, on hover and in the Journal). The places, which every short shares the box with, are a family of their own. */
type Facts = { FILL: Record<string, string>; FILL_BY: Record<string, Record<string, [string, string]>>; ALERT: Record<string, [string, string[]]>; POLICE: Record<string, [string, string[]]> };
type W = { title?: string; short?: string; line?: string; variants?: Record<string, W> };

/** The px a cause's short may take under guide `v`: the box less the clock, and less a space and a place where the host adds one. */
export const room = (key: string, v: number) => Math.floor((LOZENGE.box - lozengePx(CLOCK) - (NO_PLACE.has(key) ? 0 : lozengePx(' ') + placeRoom(v))) * 10 + 1e-9) / 10; // 1e-9: 142.9 is 142.8999… in floats
/** About how many characters `px` holds at the lozenge's mean (5.8 px a character at 13 px: Latch's line, 74 characters in 430 px). */
export const aboutChars = (px: number) => Math.floor(px / 5.8);
/** A short a host test reads, kept word for word (brief 100's HUD tests: the meter's "More police" row). */
const KEEP: Record<string, string> = { 'police.more': 'the search goes on' };
/** A place a host test reads (HostLogic.Why100.cs: the shift guard's radio, "… at the Office door"). */
const KEEP_PLACE: Record<string, string> = { 'd:Office entrance': 'at the Office door' };
/** What the places are, for the four that are over their room today and the ones that share a name with another (the plan's destinations and the site's own). */
const PLACE_FACTS: Record<string, string> = {
  cabinet: 'the street\'s control cabinet, where the Tech loops the cameras and reroutes the patrol (the HUD names it "the control cabinet"; an act there says "the cabinet")',
  'service-drive': 'the service drive behind the Office, a lane where a car can stand (the HUD names the stand "the service drive")',
  'd:Under parasol': 'the café\'s terrace, its tables under parasols',
  'd:Getaway bay': 'the kerb bay in the square where the getaway car waits (the HUD names the stand "the square\'s bay")',
  'service-bay': 'the service yard behind the Office', 'security-entry': 'the street the police come in by, north of the square',
  'd:Between tables': 'among the café\'s tables', 'd:Official entrance': 'the Office\'s official door, Popovic\'s way out', 'd:Meet Vale': 'by the official door, where Popovic comes out',
  'd:Official drop-off': 'the pickup, where Popovic\'s car stops', 'd:Boarding pavilion': 'the pickup\'s pavilion', 'd:Meet driver': 'the pickup, where the driver waits',
  'd:Office approach': 'the steps up to the Office', 'd:Office entrance': 'the Office\'s public door, the guard\'s post', 'd:West side': 'the café\'s west side', 'd:East side': 'the café\'s east side',
};
/** Guide /21 (the /20 places run held at 9 for four rounds on "by the cabinet": the hover says "the control cabinet"): the HUD already gives the cabinet two names, and the
 *  short one is the act's (HostActivity.cs ActPlaceName: a loop, a disable or a reroute is "at the cabinet"), so its facts say both. */
const facts21 = (text: any): Record<string, string> => ({ ...PLACE_FACTS, cabinet: `the street's control cabinet, where the Tech loops the cameras and reroutes the patrol. The HUD gives it two names: "${text.names.targets.cabinet}" on hover and in the Intel, and "the cabinet" on the label of an act there (a loop, a disable or a reroute is "at the cabinet"); the short one is the HUD's own` });
/** Guide /23 (Fable 2026-11-09, from the engine): a killing is confirmed by security's own eyes, which the host doesn't name (HostWhy.cs fills only {about}): a robot sent to
 *  look at Alert 1, the alarmed patrol, an officer or the guard (GameResponse.Confirm, from FoundBySecurity and a unit's Verify), and it raises the Alert only after a first
 *  report that said less (a passer-by reports every body as knocked out: GameTakedown.KindOf; an empty post; someone missing). The gun warning is given only by the guard or an
 *  officer, never a camera, the patrol or a robot (GameIntent.TodayWarns). The /19 facts said "Police checked" and "A watcher". */
const ALERT_23: Record<string, string> = {
  confirmed: 'Security saw with its own eyes that a body was dead, a killing, not a knockout (report.confirmed; Alert 2): a robot sent to look, the patrol, an officer or the guard, and the host doesn\'t say which. It raises the Alert only after the body\'s first report said less: a passer-by reports every body as knocked out (they can\'t tell), a missed check-in only says the post is empty, a missing person only that they are gone.',
  warned: 'The guard or an officer saw an agent with a gun out and told them to put it away (never a camera, the patrol or a robot; only before any shot, at Alert 1 or less; Alert 1). Facts, never said: the agent has 2 s to holster before the gun counts as seen and it is called in; holstered in time, the watcher still remembers the face.',
};
const FILL_BY_23: Record<string, Record<string, [string, string]>> = { warned: { who: ['who gave the warning: the guard, or an officer (never a camera, the patrol or a robot)', 'the guard'] } };
const SHOWN_23: Record<Group, string> = {
  alert: '; title: the Journal row (an Alert card on the lozenge has its own title); line: one sentence on hover in the Alert meter\'s list and in the Journal, where the host adds what the Alert set off after it (the effect), and the debrief\'s timeline',
  police: '; title: the police card\'s head over its short, and the Journal row; line: one sentence on hover in the Alert meter\'s list and in the Journal, and the debrief\'s timeline',
};
const SHOWN = 'short: the news lozenge\'s line under the card\'s title, the place and the clock after it (the family\'s context); the Alert meter\'s row; the debrief\'s reason under its Alert number';
type Group = 'alert' | 'police';

/** `hand`: the families of a hand run. Guide /22 (brief 101, Fable 2026-11-09): a cause marked `hand` in text.json (Fable's hand edits, briefs/101/fable/hand-whys.json)
 *  is not an item of a writer run, as a hand row of the dialogue isn't (guide /17), and its family's context gives its shorts; a hand run scores the family as it plays,
 *  those causes and all (guide /18). /19 to /21 read as they ran. */
export function whyBriefs19(text: any, b18: ShortBrief[], f: Facts, v: ShortGuideVersion, hand = false): ShortBrief[] {
  const w = text.why, words = fillWords(text), alertTitle = text.book.feed.cards.alert.title as string, place = placeRoom(v);
  const byHand = (g: Group, k: string) => v >= 22 && !hand && !!(w[g][k] as { hand?: string }).hand;
  /** /23: a hand run scores the title and the line too (the /18 family's fields, which hold the words a host test reads), and the facts follow the engine. */
  const all23 = v >= 23 && hand, fillBy = v >= 23 ? { ...f.FILL_BY, ...Object.fromEntries(Object.entries(FILL_BY_23).map(([k, x]) => [k, { ...f.FILL_BY[k], ...x }])) } : f.FILL_BY;
  const means = (k: string, id: string) => fillBy[k]?.[id]?.[0] ?? f.FILL[id]!, items18 = new Map(b18.flatMap(b => b.items).map(i => [i.key, i]));
  const item = (group: Group, k: string, path: string, x: W, base: W, label: string): ShortItem => {
    const [told, ids] = (group === 'alert' ? f.ALERT : f.POLICE)[k]!, tells = (v >= 23 && group === 'alert' ? ALERT_23[k] : undefined) ?? told;
    const short = x.short ?? base.short!, title = x.title ?? base.title!, line = x.line ?? base.line!;
    const { filled, px } = lozengeWidth(words, group, k, short, place), where = NO_PLACE.has(k) ? '' : v >= 20 ? ' at the service drive' : ' at the official door';
    const field: Field = { id: 'short', form: 'short', words: 6, chars: 56, count: 1, keep: KEEP[`${group}.${k}`] ? [KEEP[`${group}.${k}`]!] : [], patterns: [], lozenge: room(k, v),
      fills: ids.filter(id => !['where', 'clock', 'cause'].includes(id)).map(id => ({ id, means: means(k, id), widest: widestFill(words, group, k, id) })) };
    const old = all23 ? items18.get(path) : undefined, more = (id: 'title' | 'line') => old?.fields.filter(g => g.id === id).map(g => ({ ...g, fills: g.fills.map(y => ({ ...y, means: means(k, y.id) })) })) ?? [];
    const fields = all23 ? [...more('title'), field, ...more('line')] : [field], said = (id: 'title' | 'line') => fields.some(g => g.id === id) ? { [id]: [x[id]!] } : {};
    return { key: path, where: all23 ? SHOWN + SHOWN_23[group] : SHOWN, speaker: null, fields, current: { ...said('title'), short: [short], ...said('line') }, stays: 'The host reads it for every rise of this cause (why.schema.ts).',
      tells: `${label}: ${tells} On the lozenge the card's title over it reads "${group === 'alert' ? alertTitle : title}". ${NO_PLACE.has(k)
        ? 'The host adds no place after this short: the line is the short and the clock.' : `The host adds the place after it ("…${where}", any place up to ${place} px), then the clock.`} ${all23 ? 'Its line says it in full, on hover and in the Journal.' : `Its line stays as it is and says it in full, on hover and in the Journal: "${line}"`} Today at its widest the lozenge reads "${filled}${NO_PLACE.has(k) ? '' : ' [place]'}${CLOCK}", ${px} px of ${LOZENGE.box}${px > LOZENGE.box ? ', cut off' : ''}.` };
  };
  const causes = (group: Group, keys: string[]) => keys.filter(k => onLozenge(group, k) && !byHand(group, k)).flatMap(k => {
    const c = w[group][k] as W, label = `${group === 'alert' ? 'Alert' : 'Police'} cause "${k}"`;
    return [item(group, k, `${group}.${k}`, c, c, label), ...Object.entries(c.variants ?? {}).filter(([, v]) => v.short).map(([v, x]) => item(group, k, `${group}.${k}.variants.${v}`, x, c, `"${k}", its ${v} variant`))];
  });
  const ctx = `The why block's words, keyed by the engine's cause; a fill is filled by the host from the engine. A \`variants\` entry words the same cause in a case the engine names. Guide /19 (brief 101): the news lozenge, bottom right by the Book, shows one card at a time, its title in bold over one small line, "{short} {where} · m:ss": the cause's short, the place the host adds after it (none for a cause with no place), " · " and the clock it came at. The line is one line of Jost Medium at 13 px in a box ${LOZENGE.box} px wide, about 40 characters for the short and its place together; a longer line is cut off with an ellipsis (Latch's layout audit, 2026-11-09: "the square's mast camera saw the Fixer stay too long beside the café · 2:46" took 430 px). A click on the line pans to the place. The same short is the Alert meter's row and the debrief's reason under its Alert number. This family writes only the shorts: each cause's title and line stay as they are, and the line already says who saw what, whom and where in full, one hover away and in the Journal.`;
  /** /22: the shorts of a family's hand causes, given in its context (none before /22, so /19 to /21 read as they ran). */
  const given = (parts: [Group, string[]][]) => {
    const said = parts.flatMap(([g, keys]) => keys.filter(k => onLozenge(g, k) && byHand(g, k)).flatMap(k => { const c = w[g][k] as W;
      return [`${g}.${k}: ${JSON.stringify(c.short)}`, ...Object.entries(c.variants ?? {}).filter(([, x]) => x.short).map(([n, x]) => `${g}.${k}.variants.${n}: ${JSON.stringify(x.short)}`)]; }));
    return said.length ? ` Shorts Fable wrote by hand to the same bar (brief 101), already in the game and not items here: never rewrite them, and keep every short here apart from them: ${said.join('; ')}.` : '';
  };
  /** /23: a hand run scores each cause's title, short and line. */
  const ctx23 = all23 ? ctx.replace(/ This family writes only the shorts: .*$/, ' This hand run scores each cause\'s title, short and line as they play: the short read at a glance on the lozenge, the meter and the debrief; the title on the Journal row; the line, with who saw what, whom and where in full, one hover away in the meter\'s list and in the Journal.') : ctx;
  const brief = (id: string, title: string, parts: [Group, string[]][]): ShortBrief => ({ id, kind: 'why', title, context: ctx23 + given(parts), items: parts.flatMap(([g, keys]) => causes(g, keys)) });
  const old = (id: string) => b18.find(b => b.id === id)!, words18 = old('why/words');
  /** /23: a way Fable wrote by hand (onWay.ways.<way> or onWay.stopped marked `hand`) is held as a hand cause is: no item of a writer run, its words in the context. */
  const way = (key: string): { plate: string; line: string; hand?: string } | undefined => key === 'onWay.stopped' ? w.onWay.stopped : key.startsWith('onWay.ways.') ? w.onWay.ways[key.slice(11)] : undefined;
  const ways18 = old('why/on-the-way'), heldWays = v >= 23 && !hand ? ways18.items.filter(i => way(i.key)?.hand) : [];
  const onTheWay: ShortBrief = !heldWays.length ? ways18 : { ...ways18, items: ways18.items.filter(i => !heldWays.includes(i)),
    context: `${ways18.context} Lines Fable wrote by hand to the same bar (brief 101), already in the game and not items here: never rewrite them, and keep every line here apart from them: ${heldWays.map(i => `${i.key}: plate ${JSON.stringify(way(i.key)!.plate)}, line ${JSON.stringify(way(i.key)!.line)}`).join('; ')}.` };
  const facts = v >= 21 ? facts21(text) : PLACE_FACTS;
  const places = Object.entries(w.places as Record<string, string>).map(([k, word]): ShortItem => ({ key: `places.${k}`, where: 'a phrase filled into {where}', speaker: null, current: { label: [word] },
    stays: 'The host reads it (why.schema.ts).', fields: [{ id: 'label', form: 'label', words: 4, chars: 28, count: 1, fills: [], keep: KEEP_PLACE[k] ? [KEEP_PLACE[k]!] : [], patterns: [], lozenge: place }],
    tells: `{where}: the site's place "${k.replace(/^d:/, '')}"${facts[k] ? `, ${facts[k]}` : ''}, with its preposition (today "${word}", ${lozengePx(word)} px). It follows a short on the lozenge's line ("a takedown seen ___ · 4:44") and ends the line's sentence ("{who} saw {about} taken down ___."), so it reads in both.${KEEP_PLACE[k] ? ' A host test reads it word for word.' : ''}` }));
  const all: ShortBrief[] = [
    brief('why/witnessed', 'The lozenge\'s line: why the Alert rose when someone saw, found or missed something', [['alert', ['seen', 'carry', 'dead', 'down', 'woken', 'noise', 'mark', 'missing', 'cut', 'confirmed']]]),
    brief('why/reported', 'The lozenge\'s line: why the Alert rose on a report, a tape, a car or an entry', [['alert', ['approach', 'staff', 'let-go', 'recorded', 'car', 'entry', 'sighting', 'loiter']]]),
    brief('why/gunfire', 'The lozenge\'s line: why the Alert rose and the police came, on shots and guns', [['alert', ['gunfire', 'violence', 'drawn', 'drawn-people', 'warned', 'fought']],
      ['police', ['heard', 'witness', 'shot-at', 'patrol', 'rivals', 'gang', 'drawn', 'attack']]]),
    brief('why/police', 'The lozenge\'s line: why the police came', [['police', ['post', 'body', 'more', 'cordon', 'loud', 'split']]]),
    onTheWay,
    { ...words18, title: 'The why block\'s glossary: reporters who are no person, report points', items: words18.items.filter(i => !i.key.startsWith('places.')) },
    { id: 'why/places', kind: 'why', title: 'The why block\'s places: {where} on the lozenge\'s line and in the line\'s sentence', items: places,
      context: `The why block's places (why.places): the host fills {where} with the nearest named place within 13 m of where it happened (by the site's place id), else "nearby". Guide /19 (brief 101): on the news lozenge a place follows the cause's short on one line, "{short} {where} · m:ss", in a box ${LOZENGE.box} px wide, so every short shares the box with the widest place; each place has at most ${place} px (about ${aboutChars(place)} characters; today "by the control cabinet" takes ${lozengePx('by the control cabinet')}).${v >= 20 ? ' The room is the width of "at the service drive", so the HUD\'s stand names keep their words: "the square\'s bay", "the service drive".' : ''} A place also ends the line's sentence on hover and in the Journal. Two ids may share words where they are one place to the player (both sides of the café are "beside the café").` },
  ];
  return all.filter(b => b.items.length); // /22: a family left with no writer's items (every cause by hand) is none
}

/** Guide /23 (brief 102's bar, Sky 2026-11-08: "that's exactly the kind of scripting that bugs me"): a hand run scores the titles and lines too, so PLAIN holds them to it as it held a short:
 *  `alert.warned`'s line ended "Holster, or it's called in.", the rule read out. */
const PLAIN_23 = 'A title, a short or a line says what happened, plainly, never a rule of the game: nothing the player must do or else, nothing that happens when, unless or at an Alert (what an Alert sets off is the effect\'s line, which the host adds after a rise\'s line).';
/** Guide /19's why rubric: /9's, with the lozenge's box in SHORT, a short's job at a glance in USEFUL, and the AI-sounding words in PLAIN. */
export function WHY_19(l: { tw: number; tc: number; sw: number; lw: number; lc: number; pw: number; pc: number }, police9: string, v: ShortGuideVersion): ShortKindRules {
  const place = placeRoom(v);
  return {
    use: `The why block (brief 100) says why the Alert rose, why the police came, and that a report is on its way: a title (the Journal row; a police card's head), a short (the news lozenge's line under the card's title, with the place the host adds and the clock it came at; the Alert meter's row; the debrief's reason under its Alert number), a line (one sentence: on hover, in the Journal, the meter's hover, the debrief's timeline), a plate (over a reporter while the report is on its way, before its seconds) and the words the host fills in ({who}, {to}, {where}).`,
    rules: [
      { id: 'USEFUL', kind: 'tells the player the cause', sources: ['fable', 'lines', 'lozenge'], instruction: `Each text tells the player the real cause, so they can deal with it now (a report that can still be stopped) or next time. A short is read at a glance on the news lozenge: it says what happened in the street in a few words, and the place and the clock follow it. Who saw it, whom it was about and the place in full are the line's, one hover away and in the Journal, so a short names a person or a camera only where the box holds them at their widest. No filler, no moralising, nothing that only restates the Alert number or the card's title.${police9}` },
      { id: 'TRUE', kind: 'true to what the engine did', sources: ['motive', 'fable'], instruction: 'True to the engine\'s cause as the item\'s `tells` give it: who saw what, how the report travels, what it set off. A short that names nobody still says only what this cause is (a body found is not a takedown seen, a gun seen is not shots fired), and causes the player must tell apart read apart. A fill stands for whatever the engine names, so the text must read true for every value the item lists; never add a reporter, place, time or effect the cause does not carry.' },
      { id: 'PLAIN', kind: 'plain everyday words', sources: ['plain', 'world', 'words', 'ai-words'], instruction: `Plain everyday words, never a report's or a police log's tone ("a clerk spotted the guard lying at the Office door", never "incident reported at post"). No engine words: ledger, cause, tick, incident, state, unit count. ${v >= 23 ? PLAIN_23 : 'A short says what happened, never a rule of the game (what an Alert sets off is the effect\'s line).'} No AI-sounding words (Sky 2026-11-08: "that noted is AI sounding"): noted, understood, proceeding, acknowledged, affirmative, rest assured and their kind (confirmed, logged, be advised and the like) are a computer's or a form's words, and the book never uses them; the code checks it.` },
      { id: 'SHORT', kind: 'within each form\'s limit', sources: ['why-limits', 'lozenge', 'fable'], instruction: `Each form within its limit at its widest fills (the code checks it): a title at most ${l.tw} words and ${l.tc} characters, starting with a capital, no full stop; a short at most ${l.sw} words and 40 characters as written, lower case unless it starts with a name, no full stop, and on the news lozenge it fits the box: "{short} {where} · m:ss" is one line of Jost Medium at 13 px in ${LOZENGE.box} px, with each fill at the widest it takes ("the square's mast camera" for a camera, "another shopkeeper", "a kiosk attendant", "the Enforcer"), a place of up to ${place} px and the clock " · 4:44", so the short and its place together are about 40 characters; each item's \`lozenge\` gives the px its short may take; a place at most ${place} px with its preposition, about ${aboutChars(place)} characters${v >= 20 ? ', and by the name the HUD gives it' : ''}; a line one sentence of at most ${l.lw} words and ${l.lc} characters, ending in a full stop; a plate at most ${l.pw} words and ${l.pc} characters, a capital, no full stop. Words are counted with each fill as one word; characters with each fill at its widest.` },
      { id: 'NAMES', kind: 'people named as the player knows them', sources: ['words', 'voices'], instruction: 'Fills name people as the player knows them ({who} and {about}: the cast by name or role, a passer-by by job, a camera by its place). Fixed words use the glossary: the cell\'s agents by role ({agent}), the guard, the driver, the desk, the patrol, the police, robots.' },
      { id: 'VOICE', kind: 'the book\'s voice', sources: ['fable', 'words'], instruction: 'The book\'s voice: plain, warm, never cute, no exclamation marks, no blame. The same words for the same thing across the block and the game (one glossary): down, knocked out, dead, the getaway car, Popovic\'s car, a place by the name the HUD gives it. An item\'s today is what the player has seen: keep it word for word where it already fits and holds every rule.' },
    ],
  };
}
