import { z } from 'zod';
import { lineOf, type Read } from './guide';
import type { ShortRule } from './short-types';
import { BARK_12 } from './short-voice';

/** Guide /13 (brief 102, Sky 2026-11-08): the agents go quiet in play, and nobody explains the game. Sky on "He bought it. He'll be back, and he knows my face.":
 *  "that's exactly the kind of scripting that bugs me. 'I will remember you.' lines etc - so unnatural". The bar's calibration is staged in NATURAL_BAR until Sky's yes. */
export const NATURAL_BAR = 'briefs/102/fable/natural.json';
export const NATURAL_BRIEF = 'briefs/102-natural-words.md';
const Calibrated = z.object({ key: z.string().regex(/^(sky|[a-z-]+(\/[a-z0-9-]+)+)$/).describe('the text.json row the line is from, or "sky" for one Sky or the brief quotes'),
  line: z.string().min(1), why: z.string().min(1) }).strict();
export const NaturalBar = z.object({ schema: z.literal('suborned.natural-bar/1'), status: z.string().min(1), why: z.string().min(1),
  kept: z.array(Calibrated).min(2), fail: z.array(Calibrated).min(8), pass: z.array(Calibrated).min(4) }).strict();
export type NaturalBar = z.infer<typeof NaturalBar>;
/** What a /13 guide carries of the bar: Sky's kept lines, and the calibration (failing and passing). */
export const GuideNatural = NaturalBar.pick({ kept: true, fail: true, pass: true });
export type GuideNatural = z.infer<typeof GuideNatural>;
export const readNatural = (read: Read) => NaturalBar.parse(JSON.parse(read(NATURAL_BAR)));
/** The brief's own words for the guide's sources: Sky's three notes, the two decisions, what fails and what passes. A moved line stops the guide. */
export const naturalWords = (brief: string) => ['- On Fable\'s sample', '- On a proposed line', '1. **The agents go quiet in play.**', '   - **The agents keep their voices only in scenes with people:**',
  '   - **What fails:**', '   - **What passes:**', '   - **The standard is the lines Sky kept:**'].map(p => lineOf(brief, p, `brief 102 ${p.trim()}`)).join('\n');
export const whatFails = (brief: string) => lineOf(brief, '   - **What fails:**', 'brief 102 what fails').replace(/^\s*- \*\*What fails:\*\*\s*/, '');

/** Guide /13's bark rubric: /12's eight rules, with WORTH and VOICE for the world's people and the cell's scene lines, NATURAL and TRUE holding the no-mechanics rule,
 *  and MECHANICS without /12's forecast example ("It'll be back when they run out." foretells a rule). VARIANTS, NAMES and SHORT read as at /12. */
export function BARK_13(words: number, chars: number, named: string, cell: string[], world: string, opening: string, brief: string, bar: GuideNatural): ShortRule[] {
  const at12 = new Map(BARK_12(words, chars, named, cell, world, opening).map(r => [r.id, r])), kept = bar.kept.map(x => JSON.stringify(x.line)).join(' and ');
  return [
    { id: 'WORTH', kind: 'earns its place, or is silence', sources: ['opening', 'fable', 'lines', 'natural-words'], instruction: `Every line must earn its place. The world's people talk so the street feels alive and the player can read them: who this person is, how they take this moment, what they mean to do. A line that only labels the event or reads the HUD back fails, however well it is put. The cell are quiet in play (brief 102, Sky 2026-11-08: "Maybe even drop them? They will feel repetitive"): the HUD carries their news, and they speak only in a scene with a person, so each line of theirs here is said to that person (whoever the Fixer is approaching or telephoning, Popovic as the Enforcer takes him, Popovic in the car), never to the commander or the cell. Each item's \`where\` says how often it plays: a line heard once a game must be worth hearing once, and a line heard every time must bear repeating. Ask of each line: would the player miss it if it were silence? ${opening} Where even the best line would be filler and the item may be cut, cut it and give the reason in one sentence.` },
    { id: 'VOICE', kind: 'the speaker and nobody else', sources: ['voices', 'sheet', 'fable'], instruction: 'The voice is the point of a bark, and it is scored on its own: a line can be true, short and plain and still fail it. Each speaker sounds like themselves and nobody else, by the voices below: Inez\'s quick café patter, the guard\'s clipped officialese, the driver gruff and practical, Popovic by his temperament, the police crisp, the patrol\'s fixed polite phrases, the rival crew cold and businesslike, a passer-by short and human; the Fixer and the Enforcer by their voice sheet. Read the family side by side: a line another speaker could say unchanged fails, and so does a turn of phrase two speakers share. The voice lives in word choice and rhythm, never in a tag. Never cute. Only a shout ends in an exclamation mark.' },
    { id: 'NATURAL', kind: 'a person talking, and nobody explains the game', sources: ['natural-words', 'natural', 'world', 'plain'], instruction: `Spoken, not written: whole thoughts, the way this person talks to whoever is in front of them, with contractions and a natural rhythm; never a status code or telegraphese ("Holstered.", "In place.", "Clear view. Logging."), never a report's or a form's tone. And nobody explains the game (brief 102, Sky 2026-11-08, on "He bought it. He'll be back, and he knows my face.": "that's exactly the kind of scripting that bugs me. 'I will remember you.' lines etc - so unnatural"). No line tells the player a rule or a consequence: that someone will remember them, know their face or never forget, will be back, will report it or write it down, will notice, will wake, could be found by a search; that a later offer will cost more, that one more visit will do it, what to do first, or what would happen if. What fails, in the brief's words: ${whatFails(brief)} People speak for their own reasons, to whoever is there: they thank, refuse, bargain, threaten, plead, panic, give orders and shout warnings. What follows is shown by the world and the HUD (she walks off to the telephone, the plate counts the report down, the guard wakes and looks round), never explained by a person. Ask of every line: would this person say it aloud, now, to whoever is there, for their own reasons? If its job is to tell the player what the game will do, it fails, however natural its words. The clockwork patrol's fixed polite phrases are the city's own regulations ("Walking is encouraged."), its voice; nothing else recites a rule. The standard is the lines Sky kept: ${kept}. Plain everyday words of this world: ${world}` },
    at12.get('VARIANTS')!,
    { id: 'MECHANICS', kind: 'no numbers or rule words', sources: ['hud', 'words', 'natural-words'], instruction: 'Nobody talks mechanics: no seconds, minutes, metres, shots or other counts, no chance, and none of the game\'s own words for its rules (cone, Alert, Trail, approach, target, hostile, objective, a search, a report or a check-in as a rule). Sky\'s rule on numbers in play (brief 101): a time belongs only to a key event in the world (the chime, the handover, the police arriving, a car leaving), and the HUD draws even those, so a person names a time only in their own words about such an event, where the item\'s facts give it ("Not before the chime."). The police and the person at a camera screen speak their trade on the radio ("All units. Shots at the Records Office."): that is their job, not the game\'s rules. No digits in a line.' },
    { id: 'TRUE', kind: 'true to what the speaker knows, now', sources: ['fable', 'motive', 'world', 'words', 'natural-words', 'natural'], instruction: 'Say only what the item\'s `tells` and `where` give: no invented facts, numbers, times, places, people, motives or outcomes, and no promise the act may not keep. Each line is true at the moment it plays (the item says when). People say only what they could know: a passer-by never knows the cell, its plan or its roles; nobody outside the cell knows its members or calls the other crew rivals, and a guard who has never seen the cell doesn\'t know whose side anyone is on. Nobody knows the game\'s future (brief 102): a person knows what they feel, see and mean to do now, never what the game will do next, so no line foretells it (that they\'ll remember a face, that someone will be back, that a knocked-out man wakes with a sore head, that a search could find a body, that they\'ll notice), even where the game will do it. A person may say what they mean to do now, in their own words ("Say when.", "I\'m getting the police!"), and that must be true: the game does it. In an item\'s `tells`, what comes after "Facts" keeps the line true and is never said. One glossary (GDD §10): Popovic comes out of the Office, or leaves it; he is out only once a car of ours has left with him (the win). Every variant of an item is true whenever that item plays.' },
    at12.get('NAMES')!,
    at12.get('SHORT')!,
  ];
}

/** Guide /13: the bark kind's use, after /12's: who speaks now. */
export const USE_13 = ' From brief 102 (Sky 2026-11-08) the cell are quiet in play, and the HUD carries the news (the portraits, the rings, the cards, the street marks). A bark is the world\'s people talking (Inez, the guard, the driver, Popovic, the police, the rival crew, the patrol, the person at a camera screen, a passer-by), or the cell in a scene with a person: the Fixer\'s opener and last word in an approach, her telephone call to Inez, the Enforcer\'s words to Popovic as he takes him, and the Fixer\'s answer to Popovic in the car. Each item\'s `where` says when its line is heard.';

/** The calibration at /13: Sky's kept lines, the lines that explain the game, and today's lines that already hold the bar. */
const listing = (xs: GuideNatural['fail']) => xs.map(x => `- ${x.key === 'sky' ? 'quoted in brief 102' : x.key}: ${JSON.stringify(x.line)} ${x.why}`).join('\n');
export const calibration13 = (bar: GuideNatural) => [
  'Calibration (brief 102, Sky 2026-11-08: "that\'s exactly the kind of scripting that bugs me. \'I will remember you.\' lines etc - so unnatural"). A line Sky would wince at fails its rule outright.',
  `Sky kept these: the standard.\n${listing(bar.kept)}`,
  `These fail: they explain the game (brief 102's examples, then lines in today's text.json):\n${listing(bar.fail)}`,
  `These pass (today's lines that already hold the bar: the bar, not lines to copy into another row):\n${listing(bar.pass)}`,
].join('\n\n');

/** The stage tasks at /13, for a bark family. */
export const TASKS_13 = {
  generate: 'Write every item of this family for The Suborned\'s Handover, to the rubric: WORTH, VOICE and NATURAL lead, and nobody explains the game. Each item is a moment the host gives a line (its `where` says when and how often): write what this person would say aloud in it, to whoever is there, for their own reasons, so the player reads them and the street feels alive. What an item\'s `tells` gives after "Facts" keeps the line true and is never said. Return each item once: its key, cut false, an empty reason, and each field it asks for with exactly that many variants, each a different take. An item may be cut only where it says mayCut true, and only when even its best line would tell the player nothing and land nobody: then return cut true, the reason in one sentence, and no fields.',
  evaluate: 'Score this family against the rubric, each rule on its own: give every rule a score from 1 to 10 in `scores`, then SCORE as the lowest of them, a REASON that explains it, and a finding for each problem: the rule, the item key, an exact quote of the words at fault (empty only for something missing or for a cut), why it breaks the rule, and how to fix it. Be the editor Sky is: 10 on a rule means you would ship every line unchanged and want to hear it in play. Read every line aloud and ask whether this person would say it, now, for their own reasons. Any line that tells the player a rule or what follows (remembering a face, coming back, reporting it, waking, a search, a price going up, what to do first) fails NATURAL and TRUE, however natural its words. Hold the calibration in mind: any line like the failing ones fails its rule. A cut is right only where the item may be cut and even its best line would be filler; say so if a cut is wrong, or if a kept line should be cut. You are independent of the writer: do not trust its claims.',
  refine: 'Revise the family to fix every finding and code check below, so every rule scores 10. Change only the lines a finding or a code check names, and return every other line exactly as it is. Keep every item\'s facts and keys. Return the whole family, every item.',
};

/** A code check at /13 for the family Sky named: a line that promises memory or foretells the game, whoever says it. A questioned row is exempt: when an investigator asks, remembering is the answer. */
export const FORETELLS = /\b(?:I'll|I will|you'll|you will|we'll|they'll|he'll|she'll) (?:remember|know (?:you|him|her|them|that face))\b|\b(?:won't|never) forget\b|\bknows? (?:my|your|our|his|her|their|that) faces?\b|\bsearch could\b|\b(?:they'll|they will|someone'll) notice\b|\b(?:he|she|they|it)'ll be back\b|\b(?:they|he|she)'ll wake\b|\bwhen (?:they|he|she) wakes?\b|\bsore head\b|\bI(?:'ll| will) note that\b|\bI notice\b|\bgoes in (?:my|the) (?:log|book)\b|\b(?:favours?|bribes?) get reported\b/i;

// ---------------------------------------------------------------- the families at /13: the world's people, and the cell's scene lines

type Row = { event: string; verb?: string; speaker: string; outcome?: string };
const APPROACH = ['ask', 'pay', 'help', 'pressure'];
/** The cell's lines that stay (text.schema.ts SCENE_ROWS), as families of their own; the guard's replies leave the old family they shared with the take. */
export const FAMILIES_13: { id: string; title: string; pick: (r: Row) => boolean }[] = [
  { id: 'bark/fixer-scenes', title: 'The Fixer face to face: her opener and last word in an approach, and her call to Inez',
    pick: r => r.speaker === 'fixer' && ((r.event === 'arrive' && APPROACH.includes(r.verb!)) || (r.event === 'done' && r.verb === 'turn') || (r.event === 'ack' && r.verb === 'call')) },
  { id: 'bark/take', title: 'The Enforcer takes Popovic: his words to him, and Popovic\'s', pick: r => r.event === 'taken' || (r.speaker === 'enforcer' && r.event === 'done' && ['take', 'enter'].includes(r.verb!)) },
  { id: 'bark/reply-guard', title: 'The guard answers', pick: r => r.event === 'reply' && r.speaker === 'guard' },
];
/** The /12 families that go at /13: the agents' acknowledgements, arrivals, landings and reads, and the one the guard shared with the take. */
export const GONE_13 = /^bark\/(ack-|read-|arrive$|done$|reply-guard-popovic$)/;

/** A reply at /13: what the person says (their answer, in their words) apart from the facts the game shows (never said). /4–/12 gave the writer one line of both,
 *  and put the temperament's rule ("if the city fears the cell they report any approach") on the side to say: the root of "Even a favour gets reported". */
const ANSWER_13: Record<string, [string, string]> = {
  turned: ['yes: they take it and are with the cell, and will do their part when the Fixer calls on them', 'nothing beyond the yes'],
  refused: ['no', 'they remember being asked, so a later offer costs more'],
  'not-yet': ['not yet: they are warmer, but not won', 'one more Help would turn them'],
  kept: ['they were already with the cell and took more help: grateful, their need met', 'nothing beyond the thanks'],
  suspicious: ['no, and it alarms them: they want nothing to do with it', 'they will report the approach to the Office or the owner; the HUD shows the report on its way'],
  'small-offer': ['no: it is too little for what they owe', 'the cell offered before it knew their need; knowing it first (a Listen) makes the offer right'],
};
/** Who has no Call: a line that offers to come when called promises what the game won't do. */
const NO_CALL: Record<string, string> = { guard: ' The guard has no Call: nothing will call on him, so no line offers to come when called.', driver: ' The driver has no Call: once turned he acts on his own, bringing Popovic\'s car round the back.' };
export const reply13 = (who: string, verb: string, outcome: string, speaker: string, temp: string, offer: string) => {
  const [say, facts] = ANSWER_13[outcome] ?? [outcome, ''];
  return `Say: ${who}'s answer to the Fixer's ${verb}, to the Fixer's face, in their own words: ${say}. Facts, never said (the HUD and the world show them): ${facts}.${temp} The offer, ${offer}${['turned', 'kept'].includes(outcome) ? NO_CALL[speaker] ?? '' : ''} Plays as their bubble, and as their answer in the approach scene after the Fixer's offer.`;
};
/** The Fixer's last word at /13, by the person's answer: said to their face, the facts never said. */
const LAST_13: Record<string, string> = {
  turned: 'they said yes and are with the cell', refused: 'they said no (facts, never said: they remember being asked, so a later offer costs more)',
  'not-yet': 'they are warmer but not won (facts, never said: one more Help would turn them, and the Fixer can come back)', kept: 'they were already with the cell and took more help, grateful',
  suspicious: 'they said no, alarmed (facts, never said: they will report the approach; the HUD shows the report on its way)',
  'small-offer': 'they turned down an offer too small for what they owe (facts, never said: the cell offered before it knew their need)',
};
/** The cell's scene lines at /13, each said to the person in the scene. */
export function scene13(r: Row, does: string): { tells: string; where: string } | null {
  if (r.event === 'arrive' && APPROACH.includes(r.verb!)) return { tells: `Say: the Fixer's first words to the person, face to face, as she walks up and before her offer; the scene plays the offer and their answer next. Said to the person, never to the commander. Facts, never said: "${does}"`, where: 'The approach scene\'s first line, face to face (brief 085), then the Journal.' };
  if (r.event === 'done' && r.verb === 'turn') return { tells: `Say: the Fixer's last word to the person's face, after their answer: ${LAST_13[r.outcome!] ?? r.outcome}. Said to them, first person or plain; never to the commander, and never what the game does next.`, where: 'The approach scene\'s last line, to the person\'s face after their reply (brief 085), then the Journal.' };
  if (r.event === 'ack' && r.verb === 'call') return { tells: 'Say: the Fixer on the telephone to Inez at the café: it is time to call the guard over, as they agreed. Inez answers next (yes, she will call him). Said to Inez, never to the commander.', where: 'The call scene\'s first line, on the telephone (text.json scenes, kind call), then the Journal.' };
  if (r.speaker === 'enforcer' && r.event === 'done' && r.verb === 'take') return { tells: 'Say: the Enforcer\'s words to Popovic as he takes hold of him at his car, after Popovic\'s first words (he blusters, bargains or begs, by his temperament); then he walks him to our car. Said to Popovic: few, heavy words.', where: 'At the car, after Popovic\'s first words, as a bubble, then the Journal.' };
  if (r.speaker === 'enforcer' && r.event === 'done' && r.verb === 'enter') return { tells: 'Say: the Enforcer\'s words to Popovic as he brings him out of the Office (inside was off-screen); Popovic\'s own words follow at once. Said to Popovic: few, heavy words.', where: 'At the Office door as they come out, a bubble before Popovic\'s, then the Journal.' };
  return null;
}
/** The world's moments whose /4–/12 facts read as the line to say (the witness who "remembers", the plan narrated, the guard who knows the rival crew): at /13, what to say apart from what the game does. */
export const WORLD_13: Record<string, string> = {
  'saw-gunfire': 'Say: this person has just seen a shot fired, close and clearly, and who fired it. Facts, never said: asked later, they can say where, when and who (the game keeps it).',
  'report-begins': 'Say: a witness starts to make a report, staff on the radio to the desk or a passer-by going for a telephone: what they say as they start. Facts, never said: the report travels to the police, and the HUD shows it on its way.',
  'witness-reacts': 'Say: a person saw a takedown or a body carried, or found a body: their first words as they react. Facts, never said: whether they report it (the HUD shows a report on its way).',
  'fight-guard': 'Say: the guard, who has never seen the cell, sees armed men by the car and runs in against them. Facts, never said: he knows neither the cell nor the other crew; he fights whoever is armed by the car.',
  'fight-warned': 'Say: Inez, whom the cell helped, shouts a warning in time: armed men by the car. Facts, never said: she doesn\'t know who they are.',
  'rivals-for-car': 'Say: Popovic is in our blocked car and nobody of the cell is outside it with a gun drawn, so the rival crew walk up to take him: their orders to each other.',
  'ai-advance': 'Say: a rival with nobody in reach walks in on the nearest agent: his order to his crew, or his own words, as a gunman would put them.',
};
/** A witness's reaction at /13, by what they do next: the deed, never the report announced. */
export const REACTS_13: Record<string, string> = {
  report: 'they set off for the police', flee: 'they are too frightened to do anything but get away', avoid: 'they look away and keep walking', check: 'they go over to look',
  quiet: 'they keep out of it', radio: 'the guard reaches for his radio',
};
