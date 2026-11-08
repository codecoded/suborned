import { block, gddNames, gddNamesFromFiles, gddPair, hash, lineOf, pick, readerFor, readerFromFiles, TEXT_FILE, TEXT_SCHEMA, type Read, type Sha } from './guide';
import { SHORT_GUIDES, SHORT_NOW, VoiceSheet, type ShortGuide, type ShortGuideVersion, type ShortKindRules, type ShortRule } from './short-types';
import { BARK_13, NATURAL_BAR, NATURAL_BRIEF, naturalWords, readNatural, USE_13 } from './short-natural';
import { BARK_12, VOICE_SHEET } from './short-voice';
import { AI_WORDS_FILE, BARK_14, natural14, readAiWords } from './short-natural14';
import type { Source } from './types';

/** The short lines' sources. The briefs read only these, so a saved run rebuilds from its archive. */
export const WHY_SCHEMA = 'contracts/missions/handover/why.schema.ts';
export const DEBRIEF_BRIEF = 'briefs/099-debrief.md';
export const DEBRIEF_WORDS = 'briefs/099/glyph/words.md';
export const DEBRIEF_NOTES = 'briefs/099/glyph/notes.md';
export const RULEBOOK = 'contracts/rules/rulebook.json';
/** Guide /10's sources for the HUD's labels: Sky's rule on numbers in play (brief 101) and Glyph's round 2 (the street layer, the corners, the words it asks for). */
export const HUD_BRIEF = 'briefs/101-hud-sweep.md';
export const HUD_NOTES = 'briefs/101/glyph/r2/notes.md';

/** The rubric Motive scores a short line by, one block of rules per kind: plain everyday words, true to what the engine did, within the
 *  kind's limit, useful to the player, people named as the player knows them, and the speaker's voice. Built only from the approved GDD,
 *  the mission's text contract and voice sheet, and Sky's recorded notes; a moved sentence stops it rather than leaving a guess. */
export function buildShortGuide(read: Read, names: string[], sha: Sha = file => hash(read(file)), version: ShortGuideVersion = SHORT_NOW): ShortGuide {
  const pair = gddPair(names), gdd = read(pair.agent.path), text = JSON.parse(read(TEXT_FILE)), schema = read(TEXT_SCHEMA), why = read(WHY_SCHEMA);
  const sources: Source[] = [];
  const add = (id: string, file: string, locator: string, excerpt: string) => sources.push({ id, file, locator, sha256: sha(file), excerpt });
  add('world', pair.agent.path, '§01 world', [lineOf(gdd, 'world: an alternate history', '§01 world'), lineOf(gdd, '- thinking machines and networks never came', '§01 no networks'),
    lineOf(gdd, '- reads:', '§01 reads')].join('\n'));
  add('lines', pair.agent.path, '§10 lines', lineOf(gdd, '- lines: the goal line', '§10 lines'));
  add('words', pair.agent.path, '§10 player words', lineOf(gdd, 'player words:', '§10 player words'));
  add('endings', pair.agent.path, '§10 fail camera, end card', [lineOf(gdd, 'fail camera:', '§10 fail camera'), lineOf(gdd, 'end card [S]:', '§10 end card')].join('\n'));
  add('fable', pair.agent.path, '§14 Fable', block(gdd, '### Fable'));
  add('motive', pair.agent.path, '§14 Motive', block(gdd, '### Motive'));
  add('voices', TEXT_FILE, '/tone, /names', JSON.stringify({ tone: text.tone, named: text.names.named, speakers: text.names.speakers }));
  const opening = text.silentLogs?.start;
  if (typeof opening !== 'string' || !opening.includes('stupid words')) throw new Error('text.json no longer records Sky\'s cut of the opening barks (silentLogs.start)');
  add('opening', TEXT_FILE, '/silentLogs/start', opening);
  const plain = pick(text.debrief.rule, /^(Brief 096 \(Sky 2026-11-04: "the End card needs to be plain english[^)]*\))/, 'Sky\'s plain-English note (debrief.rule)')[0]!;
  add('plain', TEXT_FILE, '/debrief/rule', plain);
  add('text-shape', TEXT_SCHEMA, 'MAX_LINE, MAX_WORDS, NAMED', schema.split('\n').filter(l => /export const (MAX_LINE|MAX_WORDS|NAMED) /.test(l)).join('\n'));
  add('why-limits', WHY_SCHEMA, 'LIMITS', why.split('\n').filter(l => /^\s+(title|short|line|plate): \{ chars/.test(l)).join('\n'));
  const brief = read(DEBRIEF_BRIEF);
  add('debrief', DEBRIEF_BRIEF, 'Rules, Sky\'s pick', [lineOf(brief, '- Words are everyday', 'brief 099 words rule'), lineOf(brief, '"yeah, try A"', 'brief 099 Sky\'s pick')].join('\n'));
  add('stand-ins', DEBRIEF_WORDS, 'Glyph\'s stand-ins', read(DEBRIEF_WORDS).split('\n')[2]!);
  add('page', DEBRIEF_NOTES, 'Where each number comes from', lineOf(read(DEBRIEF_NOTES), '**A · The front page.**', 'concept A'));
  add('rules', RULEBOOK, 'gunfire-trail, hit-trail, down-trail', read(RULEBOOK).split('\n').filter(l => /"(gunfire|hit|down)-trail": \{/.test(l)).map(l => l.trim().slice(0, 160)).join('\n'));
  add('human-gdd', pair.human.path, 'authoritative human GDD', `Human GDD v${pair.human.version}; it wins on conflict with the agent copy.`);
  if (version >= 10) { // after the /9 sources, so a /1–/9 guide rebuilds with exactly the sources it ran with
    const hud = read(HUD_BRIEF), notes = read(HUD_NOTES);
    add('hud', HUD_BRIEF, 'Sky\'s rule on numbers in play (2026-11-08)', ['- **No hit chance anywhere in play:**', '- **No action durations in play:**', '- **Time stays only for key events in the world:**', '- **An order\'s price**']
      .map(p => lineOf(hud, p, `brief 101 ${p}`)).join('\n'));
    add('hud-words', HUD_NOTES, 'the street layer, the corners, words for Fable', ['- Labels are bold Jost', '- The corners are bare type', '- The news is a lozenge by the Book', '- **New:** one label', '- **Already the game\'s:**']
      .map(p => lineOf(notes, p, `Glyph's round 2 ${p}`)).join('\n'));
  }
  // guide /12 (brief 099 voices): Fable's voice sheet and calibration, after the /10 sources so a /10–/11 guide rebuilds with exactly the sources it ran with
  const sheet = version >= 12 ? VoiceSheet.parse(JSON.parse(read(VOICE_SHEET))) : null;
  if (sheet) add('sheet', VOICE_SHEET, 'voices, calibration (fail, pass), speech', [sheet.status, sheet.why, ...Object.entries(sheet.voices).map(([k, v]) => `${k}: ${v.short.join(' ')}`)].join('\n'));
  // guide /13 (brief 102): Sky's words and the no-mechanics calibration, after the /12 sources so a /12 guide rebuilds with exactly the sources it ran with
  const natural = version >= 13 ? readNatural(read) : null, words102 = version >= 13 ? read(NATURAL_BRIEF) : '';
  if (natural) {
    add('natural-words', NATURAL_BRIEF, 'Sky 2026-11-08, decisions 1 and 2', naturalWords(words102));
    add('natural', NATURAL_BAR, 'kept, fail, pass', [natural.status, natural.why, ...natural.kept.map(x => `kept: ${x.line}`)].join('\n'));
  }
  // guide /14 (brief 102's sweep): the AI-sounding words and Sky's four, after the /13 sources so a /13 guide rebuilds with exactly the sources it ran with
  const ai = version >= 14 ? readAiWords(read) : null, bar = natural && ai ? natural14(natural, ai) : natural;
  if (ai) add('ai-words', AI_WORDS_FILE, 'words, kind, rejected, fail', [ai.status, ai.why, `words: ${ai.words.join(', ')}; their kind: ${ai.kind.join(', ')}`, ...ai.rejected.map(x => `rejected: ${x.line}`)].join('\n'));

  const words = Number(pick(schema, /export const MAX_WORDS = (\d+);/, 'MAX_WORDS')[0]), chars = Number(pick(schema, /export const MAX_LINE = (\d+);/, 'MAX_LINE')[0]);
  const lim = (kind: string) => pick(why, new RegExp(`${kind}: \\{ chars: (\\d+), words: (\\d+)`), `why LIMITS.${kind}`).map(Number);
  const [tc, tw] = lim('title'), [sc, sw] = lim('short'), [lc, lw] = lim('line'), [pc, pw] = lim('plate');
  const cell: string[] = Object.values(text.book.cell.name as Record<string, string>);
  const rule = (id: ShortRule['id'], kind: string, sources: string[], instruction: string): ShortRule => ({ id, kind, instruction, sources }), v4 = version >= 4;
  const named = (text.names.named as string[]).join(' and ');
  const kinds: ShortGuide['kinds'] = {
    bark: { use: `A bark is a speech bubble at the speaker, in play: at most two at once, 3 s each, and every one also lands in the book's Journal. The play HUD shows no other sentences (§10), so a bark is often the only word the player gets about a moment.${natural ? USE_13 : sheet ? USE_12 : ''}`, rules: ai && bar ? BARK_14(words, chars, named, cell, text.tone.world, OPENING, words102, bar, ai) : natural ? BARK_13(words, chars, named, cell, text.tone.world, OPENING, words102, natural) : sheet ? BARK_12(words, chars, named, cell, text.tone.world, OPENING) : v4 ? BARK_4(words, chars, named, cell, text.tone.world) : [
      rule('USEFUL', 'tells the player something they can use', ['opening', 'fable', 'lines'], 'A bark exists to tell the player something they can use now: that an order was taken and what it will do, what just happened, what a person is about to do (call it in, run, fight), what changed and what it opens or costs. Every line carries a fact from its item\'s `tells`. A line that says only that someone is ready, waiting, worried or in a mood is filler and fails. The four opening barks Sky cut ("what is this stupid words at the start of every game?") are the failing example: "The square looks perfectly ordinary. That\'s what worries me.", "Friends first, force last. Remember.", "Waiting. Always waiting.", "Tell me when." Where even the best line would be filler and the item may be cut, cut it and give the reason in one sentence.'),
      rule('TRUE', 'true to what the engine did', ['fable', 'motive', 'world'], 'Say only what the item\'s `tells` and `where` give: no invented facts, numbers, times, places, people, motives or outcomes, and no promise the act may not keep. People say only what they could know: a passer-by never knows the cell, its plan or its roles; the cell never states what it has not watched or heard. Every variant of an item is true whenever that item plays.'),
      rule('PLAIN', 'plain everyday words', ['world', 'voices', 'plain'], `Plain, everyday spoken words of this world: ${text.tone.world} No report or form tone, no design jargon in anyone's mouth (target, hostile, objective, Alert level, Trail, approach), no stage directions.`),
      rule('SHORT', 'short, within the bubble', ['text-shape', 'lines', 'fable'], `A bubble shows for 3 s: at most ${words} words and ${chars} characters (the human GDD: twelve words or fewer), and most lines 3 to 8 words. One thought a line, ending in . ? or !. Each variant of an item says its fact a different way; no variant repeats another, or another item's line.`),
      rule('NAMES', 'people named as the player knows them', ['words', 'voices', 'text-shape'], `Only ${named} are named; everyone else by role as the player knows them: the guard, the driver, the patrol, the police, the desk, a camera by where it is. The cell call each other by role (the Scout, the Fixer, the Tech, the Enforcer); nobody outside the cell knows the cell's roles or names, and nobody says ${cell.join(', ')}. The driver calls Popovic "the Councillor".`),
      rule('VOICE', 'the speaker\'s own voice', ['voices', 'fable'], 'Each speaker keeps their voice from the voice sheet below, and the temperament the item names: the Scout quick and chatty to her own cell, the Fixer warm with steel under it, the Tech calm, practical and dry, the Enforcer one to four words; people short and human, the patrol\'s fixed polite phrases, the police crisp. Never cute. Only a shout may end in an exclamation mark.'),
    ] },
    why: { use: 'The why block (brief 100) says why the Alert rose, why the police came, and that a report is on its way: a title (the card and the Journal row), a short (the crucial card\'s dim line, with {where} after it, and the Alert meter\'s row), a line (one sentence: the Journal, Alt, the meter\'s hover, the debrief\'s timeline) and a plate (over a reporter while the report is on its way, before its seconds).', rules: [
      rule('USEFUL', 'tells the player the cause', ['fable', 'lines'], `Each text tells the player the real cause, who saw or did what, so they can deal with it now (a report that can still be stopped) or next time. No filler, no moralising, nothing that only restates the Alert number or says "something happened".${version >= 9 ? POLICE_9 : ''}`),
      rule('TRUE', 'true to what the engine did', ['motive', 'fable'], 'True to the engine\'s cause as the item\'s `tells` give it: who saw what, how the report travels, what it set off. A fill stands for whatever the engine names, so the text must read true for every value the item lists; never add a reporter, place, time or effect the cause does not carry.'),
      rule('PLAIN', 'plain everyday words', ['plain', 'world', 'words'], 'Plain everyday words, never a report\'s or a police log\'s tone ("a clerk spotted the guard lying at the Office door", never "incident reported at post"). No engine words: ledger, cause, tick, incident, state, unit count.'),
      rule('SHORT', 'within each form\'s limit', ['why-limits', 'fable'], `Each form within its limit at its widest fills (the code checks it): a title at most ${tw} words and ${tc} characters, starting with a capital, no full stop; a short at most ${sw} words, 40 characters as written and ${sc} at its widest, lower case unless it starts with a name, no full stop; a line one sentence of at most ${lw} words and ${lc} characters, ending in a full stop; a plate at most ${pw} words and ${pc} characters, a capital, no full stop.${version >= 5 ? ' Words are counted with each fill as one word; characters with each fill at its widest.' : ''}`),
      rule('NAMES', 'people named as the player knows them', ['words', 'voices'], 'Fills name people as the player knows them ({who} and {about}: the cast by name or role, a passer-by by job, a camera by its place). Fixed words use the glossary: the cell\'s agents by role ({agent}), the guard, the driver, the desk, the patrol, the police, robots.'),
      rule('VOICE', 'the book\'s voice', ['fable', 'words'], 'The book\'s voice: plain, warm, never cute, no exclamation marks, no blame. The same words for the same thing across the block and the game (one glossary): down, knocked out, dead, the getaway car, Popovic\'s car.'),
    ] },
    debrief: { use: 'The debrief page (brief 099, concept A, the front page Sky picked): the book\'s light page when a game ends, win or lose. The clock has stopped, so the player reads at leisure, but every label, note and reason sits in a small slot beside a number, a row or a face.', rules: [
      rule('USEFUL', 'answers the player\'s question at that spot', ['debrief', 'page'], 'The page tells the player why they won or lost and what to try next. Each text answers the question the player has at that spot: what made this number, how did this row come true, what would have saved it. No decoration, praise or scolding.'),
      rule('TRUE', 'true to the run', ['endings', 'page', 'motive'], 'Each text reads true for every run that can show it, with the fills its item lists (the engine\'s numbers, times and names). A Next time line names what to deal with and why, as the fail camera\'s tip does, never the order to give (GDD §10: everyone can be turned more than one way), and agrees with the end card\'s cause.'),
      rule('PLAIN', 'plain everyday words', ['plain', 'debrief', 'words'], 'Plain English, clear and not too formal (Sky: "the End card needs to be plain english and clear but not too formal"); everyday words, never a form\'s or a report\'s tone. No engine words: replay, engine, tick, hash, operative, ledger.'),
      rule('SHORT', 'within the slot', ['fable', 'stand-ins'], `Within each slot's limit, as the item's field gives it in words and characters at the widest fills: a label or a note is a fragment, a reason under a number at most six words, a sentence one line.${version >= 6 ? ' Words are counted with each fill as one word; characters with each fill at its widest.' : ''}`),
      rule('NAMES', 'people named as the player knows them', ['words', 'voices', 'page'], version >= 5
        ? `The page shows the cell's faces, so an agent a fill gives is {op}: the name the book's cell page gives them (${cell.join(', ')}). Where a line speaks of the agent who does a job (the Enforcer, who carries Popovic) and the item has no {op}, the role is how the player knows them, as on the end card and the fail camera. ${named} by name; everyone else by role: the guard, the driver, the patrol, the police, their lookout.`
        : `The page shows the cell's faces, so an agent is {op}: the name the book's cell page gives them (${cell.join(', ')}). ${named} by name; everyone else by role: the guard, the driver, the patrol, the police, their lookout.`),
      rule('VOICE', 'the book\'s voice', ['fable', 'words'], `The book's voice: plain, warm, never cute, no exclamation marks. The same word for the same thing as the start page, the end card and the glossary: turned, down, the getaway car, Popovic's car, Mislead (never false tip).${version >= 9 ? TODAY_9 : ''}`),
    ] },
    ...(version >= 10 ? { label: LABEL_10(named, version) } : {}),
  };
  return { schema: SHORT_GUIDES[version], game: 'The Suborned', gdd: pair.agent.version, sources, kinds, voices: text.tone.voices, avoid: text.tone.avoid, profanity: text.tone.profanity,
    named: text.names.named, cell, ...(sheet ? { sheet: { voices: sheet.voices, fail: sheet.fail, pass: sheet.pass } } : {}), ...(bar ? { natural: { kept: bar.kept, fail: bar.fail, pass: bar.pass } } : {}), ...(ai ? { ai: { words: ai.words, kind: ai.kind, rejected: ai.rejected, fail: ai.fail } } : {}) };
}

export const loadShortGuide = (base: string, version: ShortGuideVersion = SHORT_NOW) => buildShortGuide(readerFor(base), gddNames(base), undefined, version);
export const shortGuideFromFiles = (files: Record<string, string>, shas: Record<string, string>, version: ShortGuideVersion = SHORT_NOW) =>
  buildShortGuide(readerFromFiles(files), gddNamesFromFiles(shas), file => shas[file] ?? hash(readerFromFiles(files)(file)), version);

/** Guide /9 (Aster's review, 2026-11-07): the /5–/7 police titles named their cause ("Shots heard"), so the card read "Shots heard · shots were heard" and never said the police
 *  were coming, the point of Sky's "why police came"; and the meter's head lost its level ("Why Alert rose"). */
const POLICE_9 = ' A police card shows its title over its short: the title says who or what is coming (Police coming, More police, Robots coming, Lockdown) and the short says why. A title that names the cause repeats its short ("Shots heard · shots were heard") and no longer says anyone is coming. The Alert meter\'s head names the level its list explains.';
/** Guide /9: the page's words are what the player has seen; a rerun changes one only to make it truer, plainer or shorter. */
const TODAY_9 = ' An item\'s `today` is what the page shows now: keep it word for word where it already holds every rule, so the page the player knows does not change for nothing.';
/** Guide /10 (brief 101, Sky 2026-11-08): the HUD's labels in play, read at a glance, held to Sky's rule on numbers in play. */
const LABEL_10 = (named: string, version: ShortGuideVersion): ShortKindRules => ({
  use: 'A label is a word or two the HUD draws in play, read at a glance while the clock runs (brief 101, the HUD Sky picked on 2026-11-08: ink labels in the top right, the news lozenge by the Book roundel in the bottom right, and the street layer drawn on people and places), beside an icon, a number, a face or a clock time. The play HUD has no sentences but speech (§10), so a label is often the only word on the thing.',
  rules: [
    { id: 'USEFUL', kind: 'names what the player can act on there', sources: ['hud', 'hud-words', 'lines'], instruction: 'A label names what the player can act on at that spot, in a word or two: what comes next on the clock, what a news card is about and where, what a shot will do to the street. It never only repeats the icon beside it or the number after it, and it never decorates: on a lean HUD a word the player can\'t use is clutter.' },
    { id: 'TRUE', kind: 'true wherever it shows', sources: ['hud', 'motive', 'fable'], instruction: 'True whenever it shows and for every value its fills take: say only what the item\'s `tells` and `where` give, and never promise what the game may not do. A label right before a clock time names the event at that time ("Chime 5:10"); words that read as a countdown before a clock time ("in", "until") tell the player the wrong time, because the HUD\'s countdowns are bare orange numbers with no word.' },
    { id: 'PLAIN', kind: 'plain everyday words', sources: ['words', 'plain', 'world'], instruction: 'The player\'s everyday words from the glossary, one name per thing (turned, down, the getaway car, Popovic\'s car, boxed in, blocked, Mislead), never design or engine words (hostile, target, objective, operative, ledger, tick, state).' },
    { id: 'SHORT', kind: 'within the slot, and no act\'s time', sources: ['hud', 'fable', 'lines'], instruction: `Within the slot the item\'s field gives, in words and characters at the widest fills, each fill counted as one word: a fragment, no full stop, no exclamation mark; a title starts with a capital, and a line under a title starts lower case unless it starts with a name or a verb the game capitalises. Sky\'s rule on numbers in play: no label says how long an act or a walk takes, nor a chance; ${version >= 11 ? SHORT_11 : 'a time belongs only to a key event (the clock, the seal, police arriving, the rivals, the handover, a report about to land, a loop running out), and the game draws that time itself beside the label, so a label carries no number of its own.'}` },
    { id: 'NAMES', kind: 'people named as the player knows them', sources: ['words', 'voices', 'lines'], instruction: `${named} by name; an agent of the cell by the {agent} fill where the item has one (the name the book's cell page gives them, beside their face), else by role as the HUD says it (the Enforcer); everyone else by role: the guard, the driver, the patrol, the police, the rivals, their lookout. A place by the name the city gives it.` },
    { id: 'VOICE', kind: 'the book\'s voice, one glossary', sources: ['fable', 'words', 'hud-words'], instruction: 'The book\'s voice: plain, warm, never cute, no blame. One glossary: the same word for the same thing on the HUD, in the Journal\'s cards and in the debrief, as the item\'s `tells` give them. An item\'s `today` is what the HUD shows now: keep it word for word where it already holds every rule, so the HUD the player knows doesn\'t change for nothing.' },
  ],
});
/** Guide /11 (host logic on the /10 words): a key event's time and the health left are the game's numbers, drawn through a fill; /10 read as "no number at all". */
const SHORT_11 = 'a time belongs only to a key event (the clock, the seal, police arriving or closing in, the rivals, the handover, a report about to land, a loop running out). The game draws every number: a label types no digit of its own, and a count, the health left or a key event\'s time comes in through a fill the item lists ({n}, {police}), which the line keeps wherever the item\'s facts or rules say so.';
/** Guide /12: the speech a bark family is written for is Fable's frequency (the voice sheet's `speech`), not one line per click. */
const USE_12 = ' The cell speaks on a decision, not a click (Fable\'s frequency, for the host once Sky says yes): an order that commits the plan speaks every time, the rest the first time each agent takes them in a game, and clicks the screen already confirms not at all. Each item\'s `where` says when its line is heard.';
const OPENING = 'The four opening barks Sky cut ("what is this stupid words at the start of every game?") are the failing example: "The square looks perfectly ordinary. That\'s what worries me.", "Friends first, force last. Remember.", "Waiting. Always waiting.", "Tell me when."';
/** Guide /4's bark rules: what to say, in voice; never the order's price read back (GDD §02: every order shows its price before you commit). */
const BARK_4 = (words: number, chars: number, named: string, cell: string[], world: string): ShortRule[] => [
  { id: 'USEFUL', kind: 'tells the player something they can use', sources: ['opening', 'fable', 'lines'], instruction: `A bark tells the player something they can use, in the speaker's own words: that this agent took this order, what just happened, what someone is about to do (call it in, run, fight), what changed. Say what the item's \`tells\` gives after "Say:"; what comes after "Facts:" keeps the line true and is not to be read out. An order's price is already on its card (GDD §02: every order shows its full price before you commit), so a line that reads it back is a tooltip, not speech, and fails. A line that says only that someone is ready, waiting, worried or in a mood is filler and fails. ${OPENING} Where even the best line would be filler and the item may be cut, cut it and give the reason in one sentence.` },
  { id: 'TRUE', kind: 'true to what the engine did', sources: ['fable', 'motive', 'world'], instruction: 'Say only what the item\'s `tells` and `where` give: no invented facts, numbers, times, places, people, motives or outcomes, and no promise the act may not keep. People say only what they could know: a passer-by never knows the cell, its plan or its roles; the cell never states what it has not watched or heard. Every variant of an item is true whenever that item plays.' },
  { id: 'PLAIN', kind: 'plain everyday words', sources: ['world', 'voices', 'plain'], instruction: `Plain, everyday spoken words of this world: ${world} No report or form tone, no design jargon in anyone's mouth (target, hostile, objective, Alert level, Trail, approach), no stage directions.` },
  { id: 'SHORT', kind: 'short, within the bubble', sources: ['text-shape', 'lines', 'fable'], instruction: `A bubble shows for 3 s: at most ${words} words and ${chars} characters (the human GDD: twelve words or fewer), and most lines 2 to 7 words; an acknowledgement is often two or three. One thought a line, ending in . ? or !. Each variant of an item says its fact a different way; no variant repeats another, or another item's line.` },
  { id: 'NAMES', kind: 'people named as the player knows them', sources: ['words', 'voices', 'text-shape'], instruction: `Only ${named} are named; everyone else by role as the player knows them: the guard, the driver, the patrol, the police, the desk, a camera by where it is. The cell call each other by role (the Scout, the Fixer, the Tech, the Enforcer); nobody outside the cell knows the cell's roles or names, and nobody says ${cell.join(', ')}. The driver calls Popovic "the Councillor".` },
  { id: 'VOICE', kind: 'the speaker\'s own voice', sources: ['voices', 'fable'], instruction: 'The voice is the point of a bark. Each speaker keeps theirs from the voice sheet below, and the temperament the item names: the Scout quick and chatty to her own cell, the Fixer warm with steel under it, the Tech calm, practical and dry, the Enforcer one to four words; people short and human, the patrol\'s fixed polite phrases, the police crisp. Spoken, not written: contractions and the speaker\'s own turn of phrase, never a manual\'s or a form\'s words. A line another speaker could say unchanged fails, and the four of the cell never share a line. Never cute. Only a shout may end in an exclamation mark.' },
];
