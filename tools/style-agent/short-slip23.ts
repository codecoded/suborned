import type { Field, ShortBrief, ShortItem } from './short-types';

/** Guide /23 (brief 101, Glyph's round 3, Sky's yes on 2026-11-09; Aster's two questions for Fable): two families for a hand run only, since no run of the loop writes
 *  these words. Inez's card after the Listen becomes a slip on her: each learned row's label word (Debt, Secret) is drawn as its kit icon and the rest of the row shows
 *  as written; each way it opened is a capsule you click, its act's word (Help, Pressure) then the rest of the row. And the pop over an agent when control of them ends,
 *  which names a key ("Take control: T") against Sky's rule that nothing is key-only. That family is retired (Fable, 2026-11-09): Latch dropped the pop in 0.2.290 and
 *  its word left text.json, so the family is built only from a text that still has the word, which is its one run's archive. */
const label = (words: number, chars: number, form: Field['form'] = 'label'): Field => ({ id: 'label', form, words, chars, count: 1, fills: [], keep: [], patterns: [] });
/** The names a card row may say mid-sentence: the scenes' own (text.json scenes.conversations.names; hand-whys-2.test.ts holds them equal), which the code checks'
 *  list of the game's names doesn't carry (it is the barks' and the HUD's: only Inez and Popovic are named there). */
export const CARD_NAMES: ReadonlySet<string> = new Set(['Scout', 'Fixer', 'Inez', 'Lukac', 'Arthur', 'Milena']);
/** A card row's key: the conversation by its id, then learned or opens and the row (labelPaths finds the item). */
export const CARD_KEY = /^scenes\.conversations\.([a-z0-9-]+)\.(learned|opens)\.(\d)$/;
const SLIP = 'Inez\'s slip after the Listen (Glyph\'s round 3): a row under her name';
const LEARNED = `${SLIP}, after its label's icon: the word before the colon is drawn as its kit icon on the slip, and the rest of the row shows as written; today's card, the book and the Journal show the whole row, the word and all`;
const OPENS = `${SLIP} after "New": a capsule you click, with the act's word before the colon, then the rest of the row as written`;
const ROWS: [part: 'learned' | 'opens', i: number, where: string, tells: string][] = [
  ['learned', 0, LEARNED, 'What the Listen taught: Inez\'s need, a debt (her Intel page shows "Debt" once learned). She owes the Bureau on a loan and is behind on it: in the scene she tells Milena she\'s three payments behind, whatever she pays they add more on, and their man comes asking for her at the café. On the slip "Debt" is the debt icon, so the rest reads after it on its own.'],
  ['learned', 1, LEARNED, 'What the Listen taught: Inez\'s secret, an affair with Arthur, the cast\'s guard on the Office door (her Intel page shows "Loves Arthur" once learned; the glossary names him "the guard"). In the scene she says it to Milena, and the Scout\'s last line sums it up: "And she\'s seeing Arthur, the guard on the Office door." On the slip "Secret" is the secret icon, so the rest reads after it on its own, saying who and what.'],
  ['opens', 0, OPENS, 'The way the Listen opens: Help, the Fixer clears all she owes the Bureau, and that turns her (after the Listen the full offer lands; before it she refuses a small one). On the slip "Help" is the capsule\'s word.'],
  ['opens', 1, OPENS, 'The way the Listen opens: Pressure, the Fixer threatens to tell the Office about her and Arthur, and that turns her too. On the slip "Pressure" is the capsule\'s word.'],
];
const POP = 'The pop over an agent for 2 s when control of them ends (HostDrive.DriveExit): after the player lets go (T, or the roster\'s Back to map tab, which glides the view back to the map), or when the game ends the drive (a move order, a correction the engine won\'t take), and then the follow view takes the agent back by itself within half a second, or with WASD. Facts, never said: taking control is the ground view, a playground feature (GDD §10 [lab]); from the map only T does it, and no click takes control today (a click on a face selects and frames the agent; once in the ground view, a click on a face swaps the agent you hold). Sky\'s rule: nothing is key-only, so a pop never names a key.';

export function slipBriefs23(text: any): ShortBrief[] {
  const conv = (text.scenes.conversations.items as { id: string; learned: string[]; opens: string[] }[]).find(x => x.id === 'listen-inez');
  if (!conv || conv.learned.length !== 2 || conv.opens.length !== 2) throw new Error('scenes.conversations listen-inez: not the card guide /23 briefs (two learned rows, two ways)');
  const rows = ROWS.map(([part, i, where, tells]): ShortItem => ({ key: `scenes.conversations.listen-inez.${part}.${i}`, where, tells, speaker: null, fields: [label(6, 44, 'line')], // a card row is a sentence or two, a capital and a full stop (CardLine: 6 words, 44 characters)
    current: { label: [conv[part][i]!] }, stays: 'The slip draws it (Glyph\'s round 3); Sky agreed the card\'s words (briefs/085/fable/approved.json).' }));
  const pop = text.less.pop['take-control'] as string | undefined; // retired: no word, no family (its run rebuilds from its archive, which has it)
  return [
    { id: 'label/inez-slip', kind: 'label', title: 'Inez\'s card after the Listen, as a slip on her (Glyph\'s round 3)', items: rows,
      context: 'Inez\'s card after the Listen (brief 085\'s look C; Sky agreed its words on 2026-11-01). Glyph\'s round 3 (brief 101, Sky\'s yes 2026-11-09) puts it on her as a slip of the book\'s cream page on a fine stem to her head, in 15 px type, her face at its corner and her name over the rows; it shows for its read time, and off screen it waits for her. Each learned row\'s label word, the part before its colon, becomes its kit icon (Debt, Secret), and the rest of the row follows as written. Each way it opened is a capsule you click, its act\'s word (Help, Pressure) then the rest of its row, after "New". The label word stays in the text: today\'s card, the book and the Journal show it as a word, and the host draws the icon from it. A row is at most 6 words and 44 characters, the label word and all (conversations.schema.ts CardLine).' },
    ...(pop === undefined ? [] : [{ id: 'label/take-control', kind: 'label' as const, title: 'The pop when control of an agent ends', items: [{ key: 'less.pop.take-control', where: 'a pop: an icon and at most three words over the agent, for 2 s', tells: POP,
      speaker: null, fields: [label(3, 24)], current: { label: [pop] }, stays: null }],
      context: 'The pops (less.pop): an icon and at most three words, for 2 s, over what they are about. Brief 101\'s round 3, which Sky said yes to on 2026-11-09, asks Fable for this one: the "Take control: T" pop names the click, not a key. A pop the host has no word for prints its key, so a pop that goes is the host\'s to drop.' }]),
  ];
}
