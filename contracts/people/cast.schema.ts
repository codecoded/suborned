// suborned.cast/1: the cast tables the model proposes (brief 010 B). Per area and occupation: the positions in the unit,
// weighted temperaments and needs, tie patterns, routine variants fitted to the life.json bands, and one name pool for the
// district. The model writes the mix, never a person: tools/people turns the tables into individuals, seeded by plot and unit id.
// Which temperaments, needs, ties and positions exist is people.rules.json's; anything outside it is rejected by the checker (P-3), not here.
import { z } from 'zod';

const word = z.string().regex(/^[a-z][a-z0-9-]*$/);
const prose = (n: number) => z.string().max(n);
const weight = z.number().positive().max(100);

export const Position = z.object({
  position: word.describe('a position of the unit\'s category (server, cook, keeper, attendant, ...) or, for a civic employer, a role of people.rules.json (clerk, guard, official-driver, councillor); "resident" for residents'),
  weight: weight.describe('share of the staff on shift; every position with a weight gets at least one person per shift when the staff allow. The keeper is made by the expander, one per unit'),
  post: z.enum(['door', 'inside', 'bay']).describe('where the position works: at the unit\'s door, inside (off-screen), or at the nearest bay'),
});

export const Routine = z.object({
  id: word,
  skeleton: z.enum(['worker', 'resident']).describe('a skeleton of life.generator.routines'),
  weight,
  leaveBeforeMin: z.array(z.number().int().min(0).max(240)).length(2).describe('[min, max] minutes between leaving home and the shift start, beyond the walk'),
  commute: z.enum(['walk', 'tram']).describe('walk: lives on the sheet and walks from a home plot; tram: lives beyond the sheet and arrives at the nearest tram stop'),
  meal: z.enum(['none', 'on-site', 'square']).describe('where the mid-shift meal is: at work, at a seat in the square, or none'),
  preShift: z.enum(['none', 'cafe']).describe('a stop at the café before the shift (the guard\'s, GDD §05)'),
  evening: z.enum(['home', 'square']).describe('after the shift: straight home, or an hour in the square'),
});

export const Tie = z.object({
  kind: word.describe('a tie kind of people.rules.json'),
  share: z.number().min(0).max(1).describe('share of the unit\'s people who carry one tie of this kind'),
});

export const Row = z.object({
  area: word, occupation: word.describe('a trade of life.generator.occupations, an employer of people.rules.json ("records-office"), or "resident"'),
  why: prose(300).describe('what the row rests on in the unit\'s trade, hours and staff'),
  positions: z.array(Position).min(1).max(8),
  temperaments: z.array(z.object({ temperament: word, weight })).min(1).max(4),
  needs: z.array(z.object({ need: word, weight })).min(1).max(9),
  ties: z.array(Tie).max(5),
  routines: z.array(Routine).min(1).max(4),
});

const Name = z.object({ name: z.string().min(2).max(16), origin: word });
export const NamePool = z.object({
  given: z.array(Name).min(60).max(90), family: z.array(Name).min(80).max(110),
});

export const Cast = z.object({
  schema: z.literal('suborned.cast/1'), scope: word,
  rows: z.array(Row).min(1).max(24), names: NamePool,
});
export type Cast = z.infer<typeof Cast>;
export type Row = z.infer<typeof Row>;
export type Routine = z.infer<typeof Routine>;
