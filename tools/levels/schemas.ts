// One zod schema per generated output (brief 001: lines, stops and stations, timetable) plus the critic's verdict.
import { z } from 'zod';

const id = z.string().regex(/^[A-Za-z0-9][A-Za-z0-9_-]*$/).describe('short id, e.g. T2, trunk, circus');
const chunk = z.string().regex(/^[0-9a-f]{24}$/).describe('a 24-hex passage id from the retrieved passages');
const xy = z.array(z.number()).length(2).describe('exactly [x, y] in metres [x east, y north], plan origin at Crown Circus centre');
const pt = z.array(z.number()).min(2).max(3).describe('[x, y] or [x, y, fillet radius m]');
const sources = z.array(chunk).describe('passage ids the proposal rests on');
const notes = z.array(z.string().max(400)).max(8);

export const Owner = z.enum(['Datum', 'Vantage', 'Pulse']);
export const handoffs = z.array(z.object({
  owner: Owner, blocking: z.boolean().describe('true: the candidate cannot be accepted until the owner acts; false: advisory'), ask: z.string().min(1).max(400),
})).optional().describe('asks for another builder (not yourself), e.g. a line that must move for a stop to fit; omit when none');

export const answers = z.array(z.object({
  from: Owner, ask: z.string().min(1).max(400).describe('the hand-off ask, copied exactly'), done: z.boolean().describe('true only if your output now does what was asked'), note: z.string().max(300),
})).optional().describe('one entry per blocking hand-off addressed to you in the task; omit when there are none');

export const LinesOut = z.object({
  lines: z.array(z.object({
    id, mode: z.enum(['tram', 'skyrail']), name: z.string().max(60), beyond: z.string().max(160).describe('where the line goes off-sheet, or "" if it does not'),
    paths: z.array(z.object({ id, role: z.enum(['running', 'line', 'loop']).describe('tram: running|loop; sky rail: line|loop'), double: z.boolean(), pts: z.array(pt).min(2) })).min(1),
    order: z.array(z.string()).describe('path ids end to end ("-id" runs a path against its drawn direction); [] = as listed'),
  })).min(1),
  remove: z.array(id).describe('ids of existing lines to delete; [] for none'),
  notes, sources, handoffs,
});

export const StopsOut = z.object({
  stops: z.array(z.object({
    line: id, id, name: z.string().max(60), path: id, at: xy,
    length: z.number().optional().describe('sky rail station length m'),
    interchange: z.string().optional().describe('"<line>/<stop id>" of the paired stop on another line'),
    fronts: id.describe('id of the road or foot path whose public space the entrance fronts'),
  })),
  notes, sources, handoffs, answers,
});

const ref = z.string().regex(/^[A-Za-z0-9_-]+\/[A-Za-z0-9_-]+$/).describe('"<line>/<stop id>"');
const oq = z.array(z.object({ question: z.string().max(300), for: z.enum(['Motive', 'Sky']), why: z.string().max(300) })).describe('what the GDD has not decided; never a price or a rule invented here');
/** What Pulse proposes (runs recorded `timetable: 'derived'`): choices only. Run times and journey sums are computed by code (timetable.ts). */
export const PulseOut = z.object({
  lines: z.array(z.object({ line: id, speed: z.number().positive().describe('cruising speed, m/s'), headwaySec: z.number().positive(), dwellSec: z.number().positive() })),
  journeys: z.array(z.object({ legs: z.array(z.object({ from: ref, to: ref })).min(1).describe('ordered rides, one line per leg; a leg ends where you change lines and the next leg starts at its interchange partner') })),
  openQuestions: oq, notes, sources, handoffs, answers,
});
/** The timetable as the plan carries it (and as Pulse's output was before derivation: runs recorded without `timetable: 'derived'`). */
export const TimetableOut = z.object({
  lines: z.array(z.object({ line: id, headwaySec: z.number().positive(), dwellSec: z.number().positive(), speed: z.number().positive().optional(), runs: z.array(z.object({ from: ref, to: ref, sec: z.number().positive() })) })),
  journeys: z.array(z.object({ from: ref, to: ref, sec: z.number().positive(), via: z.array(ref).describe('interchanges used; [] for none') })),
  openQuestions: oq, notes, sources, handoffs,
});

export const VerdictOut = z.object({
  verdict: z.enum(['pass', 'fail']),
  summary: z.string().max(600),
  faults: z.array(z.object({
    owner: Owner, rule: z.string().max(40).describe('plan checker rule id, or "critic" for a fault the checker cannot see'),
    fault: z.string().max(500), fix: z.string().max(400), chunks: z.array(chunk).min(1).describe('passage ids that show the rule being broken'),
  })).describe('empty when verdict is pass'),
  sources,
});

export type Lines = z.infer<typeof LinesOut>;
export type Stops = z.infer<typeof StopsOut>;
export type Pulse = z.infer<typeof PulseOut>;
export type Timetable = z.infer<typeof TimetableOut>;
export type Verdict = z.infer<typeof VerdictOut>;
export const agents = { datum: LinesOut, vantage: StopsOut, pulse: PulseOut, keel: VerdictOut } as const;
export type Agent = keyof typeof agents;

/** The schema a recorded Pulse output answers to: derived runs use PulseOut, older runs the proposed-times shape. */
export const schemaFor = (a: Agent, derived: boolean): z.ZodType<any> => (a === 'pulse' && !derived ? TimetableOut : agents[a]);
