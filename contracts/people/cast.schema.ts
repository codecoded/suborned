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
  post: z.enum(['door', 'inside', 'bay', 'official']).describe('where the position works: at the unit\'s door, inside (off-screen), at the nearest bay, or at its official (service) entrance'),
  // The staffing vocabulary (Pulse, brief 010, Handover fix): every field is optional and absent means what the schema meant before.
  count: z.number().int().min(1).max(40).optional().describe('exactly this many of the position on every shift (the rest of the staff share out by weight): the one councillor and his driver'),
  posts: z.array(z.object({ post: z.enum(['door', 'inside', 'bay', 'official']), count: z.number().int().min(1).max(4) })).max(3).optional()
    .describe('of the position\'s people on a shift, the first `count` of each entry stand that post, the rest stand `post`: the one door guard, the service door, the rounds inside'),
  side: z.enum(['public', 'official']).optional().describe('the entrance an inside person of this position is counted at (default public): the guards\' and the councillor\'s indoor posts are at the official side, so the public door holds only whoever stands it'),
  watch: z.object({ count: z.number().int().min(1).max(4) }).optional().describe('a civic employer\'s watch: this position also works the 8 h shifts outside office hours, `count` on each; the office shift\'s own count is unchanged'),
});

export const Routine = z.object({
  id: word,
  skeleton: z.enum(['worker', 'resident']).describe('a skeleton of life.generator.routines'),
  weight,
  leaveBeforeMin: z.array(z.number().int().min(0).max(240)).length(2).describe('[min, max] minutes between leaving home and the shift start, beyond the walk'),
  commute: z.enum(['walk', 'tram', 'car']).describe('walk: lives on the sheet and walks from a home plot; tram: lives beyond the sheet and arrives at the nearest tram stop; car: lives beyond the sheet and is driven to and from the unit\'s bay (the councillor and his driver)'),
  meal: z.enum(['none', 'on-site', 'square']).describe('where the mid-shift meal is: at work, at a seat in the square, or none'),
  preShift: z.enum(['none', 'cafe']).describe('a stop at the café before the shift (the guard\'s, GDD §05)'),
  evening: z.enum(['home', 'square']).describe('after the shift: straight home, or an hour in the square'),
  positions: z.array(word).max(8).optional().describe('only these positions of the row take this variant (the guard\'s café stop is the guard\'s, not the clerk\'s); absent: every position'),
});

export const Tie = z.object({
  kind: word.describe('a tie kind of people.rules.json'),
  share: z.number().min(0).max(1).describe('share of the unit\'s people who carry one tie of this kind'),
  positions: z.array(word).max(8).optional().describe('only these positions of the row carry this pattern (the guard\'s regulars, the driver\'s councillor); absent: every position'),
  with: word.optional().describe('the tie is made with a person of this position (a server, a councillor); absent: anyone the kind allows'),
  posts: z.array(z.enum(['door', 'inside', 'bay', 'official'])).max(4).optional().describe('only people standing these posts carry this pattern: the door guard, not the guards on rounds'),
  at: z.enum(['pre-shift', 'evening']).optional().describe('a regular tie only: the stop of the carrier\'s day it is aimed at, by the staff on shift when they arrive; absent: the pre-shift stop first, then any other, as before'),
});

export const Row = z.object({
  area: word, occupation: word.describe('a trade of life.generator.occupations, an employer of people.rules.json ("records-office"), or "resident"'),
  why: prose(300).describe('what the row rests on in the unit\'s trade, hours and staff'),
  positions: z.array(Position).min(1).max(8),
  temperaments: z.array(z.object({ temperament: word, weight })).min(1).max(4),
  needs: z.array(z.object({ need: word, weight, positions: z.array(word).max(8).optional().describe('only these positions of the row are drawn this weight (the café server weights debt, the driver cash); absent: every position') })).min(1).max(12),
  ties: z.array(Tie).max(8),
  secrets: z.array(z.object({ secret: word, share: z.number().min(0).max(1) })).max(2).optional().describe('the share of the row\'s people who hold this secret: `record` (a past on file; it replaces the need `secret`, whose weight moves here). `affair` is never written here: everyone with a lover tie holds it'),
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

/** suborned.cast.promotion/1: the record beside a promoted cast (contracts/people/<plan id>.cast.json, written by promoting an accepted people run, as run r011's
 *  skyline text was promoted into the plan). The cast file is the run's accepted `cast` byte for byte (JSON.stringify(cast, null, 1), no trailing newline), so
 *  the record names the run, the round, the sha256 of the round's output file and of the cast file, and who promoted it and when; tools/people/promoted.ts holds
 *  the file to it (and to the run on disk) every time `bun run people`, `bun run verify` and the contracts tests run. */
export const CastPromotion = z.object({
  schema: z.literal('suborned.cast.promotion/1'),
  plan: word, scope: word,
  cast: z.object({ file: z.string(), sha256: z.string().regex(/^[0-9a-f]{64}$/), bytes: z.number().int().positive() }),
  source: z.object({
    run: z.string().regex(/^r\d{3}$/), round: z.number().int().min(1), version: z.string().describe('the round\'s plan version label in run.json (finalVersion for the accepted round)'),
    file: z.string().describe('the round\'s output file, under runs/levels/<plan id>/<run>/'), path: z.literal('cast').describe('where the cast stands in that file'),
    sha256: z.string().regex(/^[0-9a-f]{64}$/).describe('sha256 of the whole round output file'),
    runFile: z.string(), outcome: z.literal('ACCEPTED'),
  }),
  promoted: z.string().describe('who promoted it and when, as Sky\'s ruling was given: "Sky, 2026-10-15"'),
  why: z.string(),
});
export type CastPromotion = z.infer<typeof CastPromotion>;
