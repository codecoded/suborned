/** Brief 101 (Latch's r4, 2026-11-09): the news lozenge, bottom right, shows a card's title over one line: the why block's short and its place (an Alert or
 *  Police card), or the card's own line, then " · " and the clock it came at. Latch's layout audit measured the sweep's loiter why at 430 px in the line's
 *  296 px box ("the square's mast camera saw the Fixer stay too long beside the café · 2:46"), drawn with an ellipsis. Guide /19 holds every such line to
 *  the box at its widest fills, measured here as the HUD draws it: Jost Medium at 13 px (HudToolkit.Crucial.cs, `k-medium`), each character's advance from
 *  the font, no kerning (so a line measures 0.4 % wider than Unity draws it: 431.7 px for Latch's 430). */

/** The line's box at 1080 (HudToolkit.Crucial.cs CrLineMax; the audit's box), its type size and face. `font` and `sha256` say which file the advances are from
 *  (lozenge.test.ts reads the font and checks every advance). A guide's families and checks are built from these numbers, so a new box is a new guide version. */
export const LOZENGE = { box: 296, size: 13, font: 'unity/Assets/Suborned/UI/Resources/Kit/fonts/Jost-Medium.ttf',
  sha256: 'de6e9ecad75e4dc1fc7dde242599e2d6e5c3a1d3de679a173aa4ebe2d71af104', unitsPerEm: 1000 } as const;
/** The room a place may take on the line: /19 105 px ("at the official door" is 104.9); from /20 108.8, the width of "at the service drive", so the HUD's stand
 *  names keep their words ("at the square's bay" is 107.7; the /19 places run couldn't fit them, and the judge wouldn't rename them). */
export const placeRoom = (v: number) => (v >= 20 ? 108.8 : 105);
const CHARS = '0123456789 !"#$%&\'()*+,-./:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~éèêëàâäçôöûüùïîñÉ’‘“”·—–…×›‹°';
const UNITS = [625, 474, 604, 576, 629, 597, 614, 556, 579, 614, 300, 318, 486, 630, 597, 819, 706, 294, 323, 323, 592, 639, 314, 215, 309, 553, 309, 355, 640, 620, 640, 585, 770,
  690, 601, 691, 701, 571, 521, 791, 744, 275, 276, 619, 481, 843, 783, 803, 578, 820, 595, 579, 504, 656, 690, 1020, 608, 601, 575, 325, 528, 325, 510, 513, 375, 504, 589, 484,
  589, 539, 309, 586, 543, 267, 259, 495, 246, 796, 545, 564, 589, 589, 371, 424, 271, 545, 475, 713, 465, 473, 485, 346, 306, 346, 575, 539, 539, 539, 539, 509, 509, 509, 484,
  564, 564, 545, 545, 545, 267, 267, 545, 571, 298, 298, 510, 510, 309, 910, 758, 910, 596, 350, 350, 286];
export const ADVANCE: ReadonlyMap<string, number> = new Map([...CHARS].map((c, i) => [c, UNITS[i]!]));
const WIDE = Math.max(...UNITS); // a character the table lacks counts as the widest
/** A line's width in px at 1080, as the lozenge draws it. */
export const lozengePx = (s: string) => Math.round([...s].reduce((n, c) => n + (ADVANCE.get(c) ?? WIDE), 0) * LOZENGE.size / LOZENGE.unitsPerEm * 10) / 10;
/** The clock a card came at, the widest the Handover can show (0:00 to 9:59): " · 4:44". */
export const CLOCK = (() => { let best = ''; for (let m = 0; m < 10; m++) for (let s = 0; s < 60; s++) { const c = ` · ${m}:${String(s).padStart(2, '0')}`; if (lozengePx(c) > lozengePx(best)) best = c; } return best; })();

/** The causes the host shows with no place after the short (HostWhy.cs NoPlace). */
export const NO_PLACE = new Set(['start', 'more', 'cordon', 'loud', 'split', 'seal', 'gang', 'staff', 'let-go', 'cut']);
/** Guide /24 (brief 102, Sky on the loitering card, 2026-11-09: "why is that a bad thing? What was the agent doing?"): a loiterer's report names who saw whom, and a camera
 *  names its own place as a cut one does, so the host adds no place after it (HostWhy.cs NoPlace). /19 to /23 measure it with a place, as they ran. */
export const NO_PLACE_24 = new Set([...NO_PLACE, 'loiter']);
/** Whether the host adds no place after a cause's short under guide `v`. */
export const noPlace = (key: string, v: number) => (v >= 24 ? NO_PLACE_24 : NO_PLACE).has(key);
/** The why causes a lozenge card shows (HostCrucial TakeWhy): every Alert rise but the start (no card: the Alert was already up), every police call but the seal
 *  (never a card); `report` and `robots` show their cause's short and place ({cause}), so they are held through the Alert causes. */
export const onLozenge = (group: 'alert' | 'police', k: string) => group === 'alert' ? k !== 'start' : !['seal', 'report', 'robots'].includes(k);

/** The words a fill can take, from text.json's glossary, by what it names: the widest of them is the fill's widest on the lozenge (a camera by its place, a person
 *  by name, role or job, `another {job}` when the reporter shares a job with whom it's about, a watcher that is no person, an agent). */
export function fillWords(text: any) {
  const T = text.names.targets as Record<string, string>, by = text.why.by as Record<string, string>, walk = text.less.done.walk.who as Record<string, string>;
  const cameras = [...Object.entries(T).filter(([k]) => k.startsWith('camera-')).map(([, v]) => v), by.camera!];
  const cast = ['waitress', 'driver', 'guard', 'councillor'].map(k => T[k]!), crowd = Object.entries(T).filter(([k]) => k === 'crowd' || k.startsWith('crowd-')).map(([, v]) => v);
  const jobs = Object.entries(walk).filter(([k]) => k === 'driver' || k === 'guard' || k === 'crowd' || k.startsWith('crowd-')).map(([, v]) => v);
  const another = [...new Set(jobs)].map(j => by.another!.replace('{job}', j));
  const units = [T.patrol!, by.robot!, by.police!, by.round!], person = [...cast, ...crowd], staff = [T.guard!, T['crowd-guard']!, T['crowd-officer']!];
  return {
    watcher: [...cameras, ...person, ...another, ...units, by.witness!], person, personWho: [...person, ...another, by.witness!], cameras, staff,
    target: [...person, T.patrol!, text.names.holders.rival.one as string], agent: ['scout', 'fixer', 'tech', 'enforcer'].map(k => `the ${text.names.speakers[k]}`),
    turnable: [T.waitress!, T.driver!, T.guard!], paid: [T.waitress!, T.driver!, T.guard!, text.names.holders.rival.one as string], checkin: [T.guard!, T['crowd-guard']!, T['crowd-clerk']!],
  };
}
type Words = ReturnType<typeof fillWords>;
/** What each fill names, cause by cause (short-whys.ts FILL_BY's meanings), as the glossary's sets. */
const SETS: Record<string, Partial<Record<'who' | 'about' | 'agent', keyof Words>>> = {
  seen: { who: 'watcher', about: 'person' }, carry: { who: 'watcher', about: 'person' }, dead: { who: 'watcher', about: 'person' }, down: { who: 'watcher', about: 'person' },
  woken: { about: 'person' }, noise: { who: 'personWho', about: 'person' }, mark: { about: 'checkin' }, missing: { who: 'personWho', about: 'person' }, cut: { about: 'cameras' },
  approach: { who: 'turnable' }, confirmed: { about: 'person' }, gunfire: { agent: 'agent', about: 'target', who: 'staff' }, violence: { who: 'personWho', about: 'person' },
  drawn: { who: 'watcher', agent: 'agent' }, 'drawn-people': { agent: 'agent' }, warned: { who: 'staff', agent: 'agent' }, fought: { about: 'staff' },
  loiter: { who: 'watcher', agent: 'agent' }, recorded: { who: 'cameras', about: 'paid' }, car: { who: 'watcher' }, sighting: { who: 'watcher', agent: 'agent' },
  'police.witness': { who: 'watcher' }, 'police.shot-at': { about: 'person' }, 'police.drawn': { who: 'watcher', agent: 'agent' }, 'police.attack': { about: 'staff' },
};
/** The widest word a fill takes for a cause, by the lozenge's measure. */
export function widestFill(words: Words, group: 'alert' | 'police', key: string, fill: string): string {
  const set = SETS[group === 'police' ? `police.${key}` : key]?.[fill as 'who'] ?? (fill === 'agent' ? 'agent' : fill === 'about' ? 'person' : fill === 'who' ? 'watcher' : undefined);
  if (!set) throw new Error(`${group}.${key}: no words for {${fill}} on the lozenge`);
  return words[set].reduce((a, b) => (lozengePx(b) > lozengePx(a) ? b : a));
}
/** A short as the lozenge draws it at its widest: the short with each fill at its widest, the place (`place` px of room, none for a cause with no place: `none`, /19's
 *  set unless the caller passes its guide's, noPlace), the clock. */
export function lozengeWidth(words: Words, group: 'alert' | 'police', key: string, short: string, place: number, none = NO_PLACE.has(key)) {
  const filled = short.replace(/\{([a-z]+)\}/g, (_, f: string) => widestFill(words, group, key, f));
  const px = lozengePx(filled) + (none ? 0 : lozengePx(' ') + place) + lozengePx(CLOCK);
  return { filled, px: Math.round(px * 10) / 10 };
}
