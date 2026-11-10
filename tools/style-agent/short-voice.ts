import type { GuideSheet, ShortGuide, ShortRule, VoiceSheet } from './short-types';

/** Guide /12 (brief 099 voices, Sky 2026-11-08: "a lot of poor wording stil throughout. Especially agents talk to player"): Fable's voice sheet, the calibration
 *  lines and the speech tiers, staged here until Sky's yes; then the voices go into text.json `tone.voices` and the tiers to the host (Latch). */
export const VOICE_SHEET = 'briefs/099/fable/voices/sheet.json';
export const AGENTS = ['scout', 'fixer', 'tech', 'enforcer'] as const;
const isAgent = (s: string): s is (typeof AGENTS)[number] => (AGENTS as readonly string[]).includes(s);
/** /12 bark runs wait on Sky's yes to the sample (briefs/099/fable/voices/index.html): until then `apply` leaves them out. Step 2 sets this. */
export const BARKS_12_APPLY = false;

/** Guide /12's bark rubric: WORTH and VOICE lead, each rule scored on its own. `opening` is Sky's cut of the opening barks, the first failing example. */
export const BARK_12 = (words: number, chars: number, named: string, cell: string[], world: string, opening: string): ShortRule[] => [
  { id: 'WORTH', kind: 'earns its place, or is silence', sources: ['opening', 'fable', 'lines', 'sheet'], instruction: `Every line must earn its place. A bark exists only if it tells the player something they don't already have, or lands this character at the right moment; otherwise it is silence, and cutting it is a good result. The player already has the order they just gave and its target, what the screen shows (the walk, the ring starting, a body falling, the car moving), the order's card and its price, and what a chip or the Intel row says. So a line that reads the click back ("On it, I'm watching.", "Taking him."), says where the speaker now is ("Beside the car.", "In place.") or restates the card ("A search could find it.", "People may notice.") fails, however well it is put. A line lands a character when it shows how this person takes this act, now: the Fixer sorry for the rope, the Enforcer's weight on a kill, the Tech's dry fondness for a clockwork patrol, the Scout pleased with a habit spotted. Each item's \`where\` says how often it plays: a line heard once a game must be worth hearing once, and a line heard on every such order must bear repeating. Ask of each line: would the player miss it if it were silence? ${opening} Where even the best line would be filler and the item may be cut, cut it and give the reason in one sentence.` },
  { id: 'VOICE', kind: 'the speaker and nobody else', sources: ['sheet', 'voices', 'fable'], instruction: 'The voice is the point of a bark, and it is scored on its own: a line can be true, short and plain and still fail it. Each agent sounds like themselves and nobody else, by the voice sheet below: their traits, their habits, how they speak to the commander, what they care about, and the lines they would never say. Read the four side by side: a line another agent could say unchanged fails, and so does a turn of phrase two of them share ("Noted." from two speakers, "Right, …" opening the lines of three). The voice lives in word choice and rhythm, never in a tag: the Scout\'s quick concrete detail, the Fixer\'s unhurried "we", the Tech\'s dry aside, the Enforcer\'s few heavy words. People outside the cell keep the voices the voice sheet gives them. Never cute. Only a shout ends in an exclamation mark.' },
  { id: 'NATURAL', kind: 'a person talking, not a status code', sources: ['world', 'plain', 'sheet'], instruction: `Spoken, not written: whole thoughts, the way a person talks to people they trust while they work, with contractions and a natural rhythm. Never a status code or telegraphese: "Holstered.", "In place.", "Clear view. Logging.", "Here. Watching.", "Noted. Done." fail. A short line is right where a person would clip it (a shout, a held breath, a tired man's two words), never to save words. No report or form tone, no stage directions, no narrating one's own act like a log ("Hiding them now."). Plain everyday words of this world: ${world}` },
  { id: 'VARIANTS', kind: 'different takes, not synonyms', sources: ['sheet', 'text-shape'], instruction: 'An item\'s variants are different takes on the same moment: a different thought, feeling or detail, each true whenever the item plays and each in the speaker\'s voice. Synonyms and reshuffles of one fragment fail ("Inside. Looking for him." / "In. Looking for Popovic." / "Inside. Finding Popovic."). The player hears one at a time and another in a later game, so each must be as good as the best, and no line repeats another item\'s.' },
  { id: 'MECHANICS', kind: 'no seconds, counts or rule words', sources: ['hud', 'words', 'sheet'], instruction: 'Nobody talks mechanics: no seconds, minutes, metres, shots or other counts, no chance, and none of the game\'s own words for its rules (a search, a report or a check-in as a rule, cone, Alert, Trail, approach, target, hostile, objective). Sky\'s rule on numbers in play (brief 101): a time belongs only to a key event in the world (the clock, the seal, the police arriving, the rivals, the handover, a report about to land, a loop running out), and the HUD draws even those, so a line says "a minute" only where its item\'s facts give that key event. Say what the rule means to people instead: "It\'ll be back when they run out.", never "Rerouted for 1:55.". No digits in a line.' },
  { id: 'TRUE', kind: 'true to what the engine did', sources: ['fable', 'motive', 'world', 'words'], instruction: 'Say only what the item\'s `tells` and `where` give: no invented facts, numbers, times, places, people, motives or outcomes, and no promise the act may not keep. Each line is true at the moment it plays (the item says when): a landing line is said once the act has landed, not while it runs. People say only what they could know: a passer-by never knows the cell, its plan or its roles; the cell never states what it has not watched or heard. One glossary (GDD §10): Popovic comes out of the Office, or leaves it; he is out only once a car of ours has left with him (the win). Every variant of an item is true whenever that item plays.' },
  { id: 'NAMES', kind: 'people named as the player knows them', sources: ['words', 'voices', 'text-shape'], instruction: `Only ${named} are named; everyone else by role as the player knows them: the guard, the driver, the patrol, the police, the desk, a camera by where it is. The cell call each other by role (the Scout, the Fixer, the Tech, the Enforcer); nobody outside the cell knows the cell's roles or names, and nobody says ${cell.join(', ')}. The driver calls Popovic "the Councillor".` },
  { id: 'SHORT', kind: 'within the bubble', sources: ['text-shape', 'lines', 'fable'], instruction: `A bubble shows for 3 s: at most ${words} words and ${chars} characters (the human GDD: twelve words or fewer). A line is as long as its thought, often four to ten words, and never clipped into a status code to fit. One or two sentences, each ending in . ? or !.` },
];

/** The prompt's voice block at /12: the sheet's entry for each agent of the family, the mission's one-line voice for everyone else. */
export const voices12 = (guide: ShortGuide, speakers: string[]) => Object.fromEntries(speakers.map(s => {
  const v = isAgent(s) ? guide.sheet?.voices[s] : undefined;
  return [s, v ? { voice: v.short.join(' '), traits: v.traits, habits: v.habits, toTheCommander: v.commander, caresAbout: v.cares, neverSays: v.never } : guide.voices[s] ?? 'short and human'];
}));
/** The calibration: lines in today's text.json that the old bar let through and this one fails, and Fable's gold set that passes, each with its reason. */
const listing = (xs: GuideSheet['fail']) => xs.map(x => `- ${x.key}: ${JSON.stringify(x.line)} ${x.why}`).join('\n');
export const calibration12 = (sheet: GuideSheet) => [
  'Calibration (Sky 2026-11-08, playing: "a lot of poor wording stil throughout. Especially agents talk to player"). A line Sky would wince at fails its rule outright.',
  `These fail (lines in today's text.json that the old bar let through):\n${listing(sheet.fail)}`,
  `These pass (Fable's gold set: the bar, not lines to copy into another row):\n${listing(sheet.pass)}`,
].join('\n\n');

/** The stage tasks at /12, for a bark family. */
export const TASKS_12 = {
  generate: 'Write every item of this family for The Suborned\'s Handover, to the rubric: WORTH and VOICE lead. Each item is a moment the host gives a line (its `where` says when and how often); write a line only this speaker would say, that the player would miss if it were silence. Return each item once: its key, cut false, an empty reason, and each field it asks for with exactly that many variants, each a different take. An item may be cut only where it says mayCut true, and only when even its best line would tell the player nothing and land nobody: then return cut true, the reason in one sentence, and no fields.',
  evaluate: 'Score this family against the rubric, each rule on its own: give every rule a score from 1 to 10 in `scores`, then SCORE as the lowest of them, a REASON that explains it, and a finding for each problem: the rule, the item key, an exact quote of the words at fault (empty only for something missing or for a cut), why it breaks the rule, and how to fix it. Be the editor Sky is: 10 on a rule means you would ship every line unchanged and want to hear it in play. WORTH and VOICE lead: a family of true, short, plain lines that tells the player nothing new, or that any of the four could say, scores low on them however clean it is. Hold the calibration\'s failing lines in mind: any line like them fails its rule. A cut is right only where the item may be cut and even its best line would be filler; say so if a cut is wrong, or if a kept line should be cut. You are independent of the writer: do not trust its claims.',
  refine: 'Revise the family to fix every finding and code check below, so every rule scores 10. Change only the lines a finding or a code check names, and return every other line exactly as it is. Keep every item\'s facts and keys. Return the whole family, every item.',
};

// ---------------------------------------------------------------- which rows speak (the sheet's `speech`), and when

type Row = { event: string; verb?: string; speaker: string; outcome?: string; lines: string[]; parked?: string; pending?: string };
type Text = { dialogue: Row[]; events: Record<string, { kind: string; speakers: string[]; log?: string[]; clock?: string }>; exchange: Record<string, string[]>; actions: Record<string, { by: string[] }> };
/** A key is a verb, or verb/outcome for one row; a verb covers its rows with any outcome except one whose own key is listed (the sheet's `keys`). */
const listed = (keys: string[], r: Row, all: string[]) => keys.some(k => k === `${r.verb}/${r.outcome}` || (k === r.verb && !all.includes(`${r.verb}/${r.outcome}`)));
const NEVER: Record<string, string> = {
  holster: 'the sidearm mark on the portrait shows it', board: 'the car shows who is aboard', 'take-car': 'the agent at its wheel and the car-taken card show it', 'drive-car': 'the car moving shows it', drop: 'the body on the ground shows it',
  fire: 'the first shot has its own going-loud line; the muzzle flash shows the rest', go: 'the end\'s own lines follow at once (the win, or the withdrawal)', withdraw: 'the withdrawal\'s own line follows at once',
  takedown: 'the order\'s way (knockout or kill) picks its own row, once the host passes it',
};
const AT = { always: 'Every time this order is given (Fable\'s frequency): a bubble at the agent, then the Journal.',
  first: 'Only the first time this agent takes this order in a game (Fable\'s frequency): a bubble, then the Journal.' };

/** Guide /12: why an agent's row is silent under the sheet's tiers (null: it speaks), and when one that speaks is heard. People's rows are unchanged. */
export function speech12(text: Text, s: VoiceSheet['speech']) {
  const rows = text.dialogue.filter(r => !r.parked && !r.pending), ackAll = [...s.ack.always, ...s.ack.first, ...s.ack.never];
  /** Who speaks for an event, in its list order: up to two voices (one at a clock mark), at most `agents` of them agents; the end keeps `end`. */
  const voices = (id: string, outcome?: string): string[] => {
    const e = text.events[id]; if (!e) return [];
    const end = e.kind === 'end', cap = end ? Math.max(2, s.events.end) : e.clock && !e.log?.length ? 1 : 2, agents = end ? s.events.end : s.events.agents, out: string[] = [];
    for (const sp of e.speakers) {
      if (out.length >= cap) break;
      if (!rows.some(x => x.event === id && x.speaker === sp && (!x.outcome || !outcome || x.outcome === outcome))) continue;
      if (isAgent(sp) && out.filter(isAgent).length >= agents) continue;
      out.push(sp);
    }
    return out;
  };
  /** An event of an act's exchange after its landing: the act's own landing line speaks for its agent, so the event's line for them is silent. */
  const doubled = (r: Row) => Object.entries(text.exchange).some(([verb, ex]) => ex.indexOf(r.event) > ex.indexOf('done') && ex.includes('done') && (text.actions[verb]?.by ?? []).includes(r.speaker));
  const silent = (r: Row): string | null => {
    if (!isAgent(r.speaker)) return null;
    if (r.event === 'read') return 'Silent since brief 044 (Sky): the chip over the person or machine says what a Watch or Listen learned.';
    if (r.event === 'ack') return listed(s.ack.never, r, ackAll) ? `Silent (Fable's frequency): the click is its own confirmation; ${NEVER[r.verb ?? ''] ?? 'the ring, the badge or the car shows it'}.` : null;
    if (r.event === 'arrive') return listed(s.arrive, r, s.arrive) ? null : 'Silent (Fable\'s frequency): the ring starting shows the act begin, and a Journal line here is clutter.';
    if (r.event === 'done') return listed(s.done, r, s.done) ? null : 'Silent (Fable\'s frequency): the player sees it land (a body down, tied or hidden).';
    if (r.event === 'agent-down') return r.speaker === 'enforcer' ? 'Never plays: the Enforcer down ends the job, and the fail speaks.' : null;
    if (doubled(r)) return 'Silent (Fable\'s frequency): the act\'s own landing line speaks for this agent; an event of the same exchange doesn\'t say it twice.';
    const v = voices(r.event, r.outcome);
    if (v.includes(r.speaker)) return null;
    const agent = v.find(isAgent);
    return agent ? `Silent (Fable's frequency): one agent speaks for this moment${text.events[r.event]?.kind === 'end' ? ' (two at the end)' : ''}, and the ${agent} has it.` : 'Silent, as today: the moment\'s people speak first, within its two voices.';
  };
  const when = (r: Row): string | null => {
    if (!isAgent(r.speaker)) return null;
    if (r.event === 'ack') return listed(s.ack.always, r, ackAll) ? AT.always : AT.first;
    if (r.event === 'arrive') return r.outcome === 'struggle' ? 'A bubble at the Enforcer as the struggle starts (Fable\'s frequency: the one arrival that is news, a noisy struggle), then the Journal.'
      : 'The approach scene\'s first line, face to face in the cutaway (brief 085), then the Journal.';
    if (r.event === 'done') return r.verb === 'turn' ? 'The approach scene\'s last line, to the person\'s face after their reply (brief 085), then the Journal.' : 'A bubble as the act lands, then the Journal.';
    return null;
  };
  return { silent, when };
}
