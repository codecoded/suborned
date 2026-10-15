// Slots: `{name}` in a template, filled per person. Lower-case words from the lexicon are capitalised where a sentence starts.
export const SLOT = /\{([a-z][a-z0-9_-]*)\}/g;
export const slotsIn = (t: string): string[] => [...t.matchAll(SLOT)].map(m => m[1]!);
/** Capitalises the first letter and the first letter after a sentence end. */
export const sentenceCase = (s: string): string => s.replace(/(^|[.!?]\s+)(\p{Ll})/gu, (_, a: string, b: string) => a + b.toUpperCase());
export type Fill = { text: string; missing: string[] };
/** Fills `t` from `v`; a slot with no value stays in the text and is reported in `missing`. */
export function fill(t: string, v: Record<string, string | undefined>): Fill {
  const missing: string[] = [];
  const text = t.replace(SLOT, (m, k: string) => { const x = v[k]; if (x === undefined) { missing.push(k); return m; } return x; });
  return { text: sentenceCase(text), missing };
}
/** The words in a spoken line. */
export const wordCount = (s: string): number => s.trim().split(/\s+/).filter(Boolean).length;
/** The given name of a full name (`Sunita Ferrer`): the first word. */
export const givenName = (full: string): string => full.split(/\s+/)[0] ?? full;
