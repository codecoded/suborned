// suborned.routines.rules/1: contracts/people/routines.rules.json (Pulse, brief 022). How a person works their post (a server walks her tables) and what two
// partners do when they meet. Unknown = reject: every key is listed here, every rule cites a source, and a proposed rule gives its reason (as people.rules.json does).
// The file is read by tools/routines/rounds.ts to bake the rounds; Keel's engine reads the baked file, and the numbers join the rulebook when he lands the reader.
import { z } from 'zod';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';

const text = z.string().min(1);
const range = z.tuple([z.number().int().positive(), z.number().int().positive()]).refine(([a, b]) => a <= b, 'a range is [low, high]');
const ground = { src: text, proposed: z.literal(true).optional(), reason: text.optional() };
const needsReason = (v: unknown) => { const o = v as { proposed?: true; reason?: string }; return !o.proposed || !!o.reason; };
const rule = <T extends z.ZodRawShape>(shape: T) => z.strictObject({ ...shape, ...ground }).refine(needsReason, 'a proposed rule needs a reason');

export const RoutinesRules = z.strictObject({
  schema: z.literal('suborned.routines.rules/1'),
  author: text, gdd: text, status: text, rule: text,
  rounds: z.strictObject({
    who: rule({ jobs: z.array(text).min(1), stop: text, terraces: z.record(text, text) }),
    pace: rule({ cmPerS: z.number().int().positive(), of: text }),
    opening: rule({ settleS: z.number().int().nonnegative() }),
    rest: rule({ s: range }),
    serve: rule({ s: range }),
    demand: rule({ greetS: z.number().int().nonnegative(), revisitS: range }),
    servicePoint: rule({ ringCm: z.array(z.number().int().positive()).min(1), clearOfSeatCm: z.number().int().positive(), face: text }),
    leashM: rule({ v: z.number().positive() }),
    horizonS: rule({ v: z.number().int().positive() }),
    clock: rule({ rule: text }),
    holds: rule({ verbs: z.array(z.enum(['listen', 'ask', 'pay', 'help', 'pressure'])).min(1), from: text, until: text, lingerS: z.number().int().nonnegative(), where: text }),
    ends: rule({ on: z.array(z.enum(['alert-2', 'gunfire', 'body-found', 'final-fight'])).min(1), then: text }),
    called: rule({ when: text, does: text, resumes: text }),
  }),
  partners: z.strictObject({
    kinds: z.array(z.enum(['lover'])).min(1),
    closeCm: rule({ v: z.number().int().positive() }),
    partS: rule({ v: z.number().int().nonnegative() }),
    clearCm: rule({ v: z.number().int().positive() }),
    summoned: rule({ meetAt: text, stay: z.strictObject({ s: z.number().int().positive(), of: text }), face: text, side: rule({ stepDeg: z.number().int().positive().max(90), order: text, pick: text }) }),
    chance: rule({ noticeM: z.number().positive(), stayS: z.number().int().positive(), perPair: z.number().int().positive(), who: text, shape: text }),
  }),
});
export type RoutinesRules = z.infer<typeof RoutinesRules>;

export const RULES_FILE = 'contracts/people/routines.rules.json';
const root = join(import.meta.dir, '..', '..');
export const readRulesText = (file = RULES_FILE) => readFileSync(join(root, file), 'utf8');
export const parseRules = (t: string): RoutinesRules => RoutinesRules.parse(JSON.parse(t));
export const loadRules = (file = RULES_FILE): RoutinesRules => parseRules(readRulesText(file));
