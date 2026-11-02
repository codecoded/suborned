// Brief 088: the `book` block of text.json: the book's own words (draft for Fable), only what no other block has. Sections are a list, so a section is one entry.
import { z } from 'zod';
const word = z.string().min(1).max(40), icon = z.string().regex(/^[a-z_]+$/);
export const Book = z.strictObject({
  rule: z.string().min(1),
  back: word,
  sections: z.array(z.strictObject({ id: z.string().regex(/^[a-z]+$/), icon, word })).min(1).describe('the round signs, in order; a section with no page shows dim'),
  job: z.strictObject({
    name: word, sub: word, endsHead: word,
    ends: z.array(z.strictObject({ icon, word, fails: z.array(z.string().min(1)).min(1).describe('the engine fail codes this row stands for') })).min(1).max(12),
    stops: z.array(z.strictObject({ at: z.string().regex(/^\d+:\d\d$/), icon, log: z.string().describe('the engine log code that says when it really happened; empty: the clock mark itself') })),
  }),
  cell: z.strictObject({ weapon: z.strictObject({ damage: z.string().includes('{n}'), every: z.string().includes('{s}'), guard: z.string().includes('{n}'), guardOne: word, chance: z.string().min(1).max(60) }).describe('the weapon block: {n} damage, {s} seconds, {n} hits'), health: word, reach: z.string().includes('{m}'), drawn: word, holstered: word, weapons: z.record(z.string().min(1), word).describe('the rulebook weapon id in the player\'s word'), does: z.record(z.string().min(1), z.strictObject({ word, icon, line: z.string().min(1).max(80) })).describe('a plate verb actions.* has no words for (the Enforcer\'s act knob)') }),
});
