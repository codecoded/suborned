// The next-event row (kind `next`, brief 101; Glyph's limit, briefs/101/glyph/r3/notes.md "The next event's limit"): the timeline mark under the clock stack's stats row
// (HudToolkit.Clock.cs: the next-event icon, the name in Jost SemiBold 15, the time in Bold 17; its hover tip keeps the full name). Its text is `<name> <m:ss>`, or the time
// alone while the cell can't know the event. It is not a window marker (MARKER_MAX, two words: a lock and m:ss), so it has its own caps:
// - the name at most 3 words (taken in at a glance: between the lock pill's 2-word reason and the goal line's 6) and 16 characters, spaces included: 115 to 139 px in its
//   type, so the row (184 to 207 px) stays inside the stack the stats row sets (251 px at $100,000, 231 px at $5,000) and never widens it as the event changes;
// - exactly one m:ss, at its end (the time is not part of the name);
// - one line, no full stop, one on screen (budget.ts counts its rows and the rows on screen).
// Until the host tags the row `next`, a `marker` with letters before a trailing clock is read as one (budget.ts recognise): in the captures of 0.2.280 to 0.2.287 only the
// next row's texts are ("Chime 5:10", "Turn driver by 5:20", "Popovic exits 5:30"); a bare clock stays a marker, whoever shows it.

/** [S] Glyph, 2026-11-09. `times`: clocks the row shows, exactly. */
export const NEXT_MAX = { lines: 1, nameWords: 3, nameChars: 16, times: 1, onScreen: 1 } as const;

const CLOCK = '\\d{1,2}:\\d{2}(?::\\d{2})?', CLOCKS = new RegExp(`(?<![\\d:])${CLOCK}(?![\\d:])`, 'g'), TRAILING = new RegExp(`(?:^|\\s)${CLOCK}\\s*$`);
/** A marker's text that is the next-event row's: letters, then a clock at its end. */
export const isNextText = (t: string | undefined): boolean => /\p{L}/u.test(t ?? '') && TRAILING.test(t ?? '');
/** The row's name (its words less every clock; empty while the event is unknown), the clocks it shows, and whether one ends it. */
export function nextParts(text: string): { name: string; clocks: number; atEnd: boolean } {
  const t = text.trim();
  return { name: t.replace(CLOCKS, ' ').replace(/\s+/g, ' ').trim(), clocks: (t.match(CLOCKS) ?? []).length, atEnd: TRAILING.test(t) };
}
/** Words as shown: a run of letters or figures between spaces (a lone separator is no word). */
export const nameWords = (name: string): number => name.split(/\s+/).filter(x => /[\p{L}\p{N}]/u.test(x)).length;
/** What is wrong with one next-event row's words. Its rows on screen, and how many rows there are, are the reader's (budget.ts). */
export function nextFaults(text: string): string[] {
  const { name, clocks, atEnd } = nextParts(text), what = `the next-event row "${text.trim()}"`, words = nameWords(name), chars = [...name].length, out: string[] = [];
  if (words > NEXT_MAX.nameWords) out.push(`${what}: its name is ${words} words (at most ${NEXT_MAX.nameWords})`);
  if (chars > NEXT_MAX.nameChars) out.push(`${what}: its name is ${chars} characters (at most ${NEXT_MAX.nameChars}, spaces included)`);
  if (clocks !== NEXT_MAX.times || !atEnd) out.push(`${what} shows ${clocks} clock${clocks === 1 ? '' : 's'}${clocks && !atEnd ? ', not at its end' : ''} (exactly ${NEXT_MAX.times} m:ss, at the end)`);
  if (/[.!?]/.test(name)) out.push(`${what} has a full stop (a name and its time, never a sentence)`);
  return out;
}
