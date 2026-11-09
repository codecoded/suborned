// Words as Glyph counts them (glyph-budget §0), shared by the budget's kinds (budget.ts) and the hover lines (hover.ts): as shown, a keycap one word, the separator `·` not a word,
// a figure with its unit one word (`8 s`, `1:55`, `$20,000`, `+2`); a segment that is only a label and a figure is one price mark (`siren 1`, `draws 1.0 s`).

const UNIT = /^(s|ms|m|%|px|x)$/;
/** A line's words, Glyph's way. */
export function glyphWords(text: string): number { return segments(text).reduce((a, g) => a + segWords(g), 0); }
/** A pill's fields: its text split at the separator dots. */
export const segments = (text: string) => text.replace(/<[A-Za-z]+>/g, 'K').split(/\s*·\s*/).map(t => t.trim()).filter(Boolean);
export function segWords(seg: string): number {
  const t = seg.split(/\s+/).filter(x => /[\p{L}\p{N}]/u.test(x)), fig = (x: string) => /\d/.test(x);
  if (t.length >= 2 && t.length <= 3 && !fig(t[0]!) && fig(t[1]!) && (t.length === 2 || UNIT.test(t[2]!))) return 1; // `siren 1`, `draws 1.0 s`: one mark
  let n = 0; for (let i = 0; i < t.length; i++) { n++; if (fig(t[i]!) && i + 1 < t.length && UNIT.test(t[i + 1]!)) i++; } return n;
}
