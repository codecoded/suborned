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
    needs: z.strictObject({ rule: z.string().min(1), ways: z.record(z.string().min(1), z.array(z.strictObject({ id: z.string().regex(/^[a-z-]+$/), words: z.string().min(1).max(60), check: z.string().min(1), known: z.string().min(1).optional() })).min(1)),
      pending: z.array(z.strictObject({ ways: z.array(z.string().min(1)).min(1).describe('the ways it joins'), after: z.string().regex(/^[a-z-]+$/).describe('the id it follows in each'), id: z.string().regex(/^[a-z-]+$/), words: z.string().min(1).max(60), check: z.string().min(1).describe('a check word the host\'s vocabulary lacks yet (HostBook.Needs.cs): Latch adds it, then the entry moves into `ways`'), note: z.string().min(1).describe('what the check is true for, in engine terms') })).optional().describe('brief 096: conditions whose words are written but whose check the host cannot read yet; the host reads none of it (the book proof holds every live check to the vocabulary)'),
    }).describe('what each way in needs, as conditions the host checks live; never a method'),
    stops: z.array(z.strictObject({ at: z.string().regex(/^\d+:\d\d$/), icon, log: z.string().describe('the engine log code that says when it really happened; empty: the clock mark itself') })),
  }),
  cell: z.strictObject({ name: z.record(z.enum(['scout', 'fixer', 'tech', 'enforcer']), z.string().regex(/^\p{Lu}\p{Ll}+$/u).max(12)).describe('each agent\'s own name, shown beside the job word on the cell page (brief 089, Sky 2026-11-02: Etruscan names). The job words stay the game\'s word for each agent; no city person learns these (GDD §04: the cell is known by face)'), who:z.record(z.enum(['scout', 'fixer', 'tech', 'enforcer']), z.string().min(1).max(80)).describe('who each agent is, under the name (Fable, briefs/088/fable/who.md)'), weapon: z.strictObject({ rule: z.string().min(1), stats: z.array(z.strictObject({ id: z.enum(['damage', 'rate', 'range', 'aim']).describe('the engine value it rates'), word, full: z.number().positive().describe('the value that fills the row'), pips: z.number().int().min(2).max(8) })).min(1).max(6) }).describe('the weapon block: ratings as pips against data scales'), health: word, reach: z.string().includes('{m}'), drawn: word, holstered: word, weapons: z.record(z.string().min(1), word).describe('the rulebook weapon id in the player\'s word'), does: z.record(z.string().min(1), z.strictObject({ word, icon, line: z.string().min(1).max(80) })).describe('a plate verb actions.* has no words for (the Enforcer\'s act knob)'), car: z.strictObject({ word, icon, line: z.string().min(1).max(80) }).optional().describe('brief 096: one shared line about the getaway car on every agent\'s Cell page (Board, Step out, Drive, Go); not wired yet') }),
  // brief 096 (Glyph's mocks briefs/096/glyph; Sky 2026-11-04 "all. recommended"): placeholders for Fable, each optional so a text without them still parses
  start: z.strictObject({ rule: z.string().min(1), begin: word, shared: z.string().min(1).max(90), purse: word, heat: word,
    cell: z.record(z.enum(['scout', 'fixer', 'tech', 'enforcer']), z.string().min(1).max(60)).describe('one line under each face in the start page\'s cell strip'),
    stations: z.record(z.string().regex(/^\d+:\d\d$/), word).describe('a station\'s word on the Job page\'s clock, before briefing.timeline[].short') }).optional().describe('the start briefing as the book\'s Job page'),
  journal: z.strictObject({ rule: z.string().min(1), sub: z.string().min(1).max(60), new: z.string().includes('{n}'), begins: word, empty: word,
    filters: z.array(z.strictObject({ id: z.enum(['all', 'crucial', 'learned', 'turned', 'city']), word })).min(1) }).optional().describe('the Journal: the full log'),
  key: z.strictObject({ rule: z.string().min(1), word, stop: word.optional() }).optional().describe('the HUD\'s Book button and the fire line\'s Stop button'),
  feed: z.strictObject({ rule: z.string().min(1), of: z.string().includes('{n}'),
    cards: z.record(z.enum(['seal', 'police', 'down', 'for-car', 'alert', 'seen', 'target-out', 'rival-car', 'turned', 'refused', 'body']),
      z.strictObject({ title: z.string().min(1).max(32), line: z.string().min(1).max(40).optional(), fixes: z.array(z.strictObject({ icon, word })).max(2).optional() })) }).optional().describe('the slim feed\'s crucial cards, most urgent first'),
  heldGo: z.strictObject({ rule: z.string().min(1), heldBy: word, fix: z.string().min(1).max(60) }).optional().describe('Go on the car\'s ring while rivals hold it'),
});
