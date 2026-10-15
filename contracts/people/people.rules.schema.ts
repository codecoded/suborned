// suborned.people.rules/1: the rules a generated person must obey, contracts/people/people.rules.json (Motive, brief 010 A).
// Anything not in the file is rejected (GDD §01: unknown = reject). Every entry cites its GDD § in `src`; an entry the GDD
// does not state carries `proposed: true` and a `reason` for Sky. The schema checks shape and the file's own cross-references
// (approach, need, tie, temperament, position and employer ids); coverage of the plan's trades and life.json's occupations is
// checked by contracts/people/people.rules.test.ts. Splice's checker reads this file (P-rules); Keel critiques it.
import { z } from 'zod';

const id = z.string().regex(/^[a-z][a-z0-9-]*$/);
const text = z.string().min(1);

export const TEMPERAMENTS = ['idealist', 'mercenary', 'fearful', 'loyal'] as const;
export const APPROACHES = ['ask', 'pay', 'help', 'pressure'] as const;
export const TIE_KINDS = ['family', 'workmate', 'regular', 'debtor', 'rival', 'lover'] as const;
export const SCALES = ['bravery', 'resentment', 'fear', 'loyalty'] as const;
export const NEED_REQUIRES = ['nothing', 'home', 'work', 'family-tie', 'rival-tie'] as const;
export const TIE_BETWEEN = ['any', 'same-employer', 'routine-stop', 'same-trade-or-employer', 'routine-meet'] as const;
export const SECRETS = ['affair', 'record'] as const;
export const FACTS = ['fixer-face', 'cell-car', 'place-time'] as const;
export const IDENTITY_STATES = ['face', 'alias', 'name'] as const; // DF's three (Adams 2017); the file says which are in play
export const KNOWN_VIA = ['workmate', 'unit-regular'] as const;

const temperament = z.enum(TEMPERAMENTS);
const approach = z.enum(APPROACHES);
const scale = z.enum(SCALES);

/** Grounding every rule carries: a GDD citation, and for new design `proposed: true` with its reason. */
const grounded = {
  src: text,
  proposed: z.literal(true).optional(),
  reason: text.optional(),
};
const needsReason = (v: unknown) => { const o = v as { proposed?: true; reason?: string }; return !o.proposed || !!o.reason; };
const reasonMsg = { message: 'a proposed rule needs a reason' };
const ground = <T extends z.ZodRawShape>(shape: T) => z.strictObject({ ...shape, ...grounded }).refine(needsReason, reasonMsg);
/** As ground(), for entries whose src lives on their parent (a trade row in the trades table). */
const maybeProposed = <T extends z.ZodRawShape>(shape: T) =>
  z.strictObject({ ...shape, proposed: z.literal(true).optional(), reason: text.optional() }).refine(needsReason, reasonMsg);

const Scale = ground({
  min: z.literal(0), max: z.number().int().positive(), integer: z.literal(true),
  steps: z.array(id).optional(), // named steps, index = value
  to: z.literal('cell').optional(),
}).refine((s) => !s.steps || s.steps.length === s.max + 1, { message: 'a scale with named steps has one per value' });

const start = z.strictObject({ bravery: z.number().int(), resentment: z.number().int(), fear: z.number().int(), loyalty: z.number().int() });

const Temperament = ground({
  name: text,
  respondsTo: z.array(approach).min(1),
  refuses: z.array(approach),
  reportsApproach: z.boolean(),
  institution: z.boolean(),
  hero: text, menace: text, // §04 temperament table, as written
  start,
});

const Approach = z.strictObject({
  requires: z.enum(['nothing', 'routine', 'need']),
  usd: z.number().int().nonnegative(), timeS: z.number().positive(), trail: z.number().int().nonnegative(), leaves: text,
});

const Need = ground({
  metBy: z.array(approach).min(1),
  help: z.enum(['cash', 'favour']),
  requires: z.enum(NEED_REQUIRES),
});

const Tie = ground({
  direction: z.enum(['both', 'from-customer', 'from-debtor']),
  maxPerPerson: z.number().int().positive(),
  maxIncoming: z.number().int().positive().optional(), // directed ties: the cap on the receiving end
  between: z.enum(TIE_BETWEEN),
  introduces: z.boolean(), // §04 introduces a friend: a turned contact can introduce the people this tie joins them to
  secret: z.boolean().optional(), // the lover tie: nobody in the city is told it, and the cell learns it only with the secret (Motive, motive-lovers.md)
  maxShare: z.number().positive().max(1).optional(), // the share of a district's simulated people who may hold it
  formsAlong: z.array(z.enum(TIE_KINDS)).optional(), // the generator draws it first from these ties
}).refine((t) => (t.direction === 'both') === (t.maxIncoming === undefined), { message: 'a directed tie sets maxIncoming; a two-way tie does not' });

const Trade = maybeProposed({ category: id, allow: z.array(temperament).min(2) });
const Role = ground({ employer: id, allow: z.array(temperament).min(2) });
const Employer = ground({ kind: z.enum(['civic', 'trade']), plot: id.optional(), block: id.optional(), trade: id.optional() })
  .refine((e) => (e.kind === 'trade') === (e.trade !== undefined), { message: 'a trade employer names its trade; a civic one does not' });

const Pinned = z.strictObject({
  part: z.enum(['given', 'family']), name: text, src: text, employer: id,
  position: id.optional(), role: id.optional(),
}).refine((p) => (p.position === undefined) !== (p.role === undefined), { message: 'a pinned name is cast by a position or a role, not both' });

export const PeopleRules = z.strictObject({
  schema: z.literal('suborned.people.rules/1'),
  author: text, gdd: text, rule: text,
  person: z.strictObject({
    // a person is routine + need + temperament + memory, and sometimes a secret; rules pinned before the secret (recorded runs) say four
    is: z.union([z.tuple([z.literal('routine'), z.literal('need'), z.literal('temperament'), z.literal('memory'), z.literal('secret')]), z.tuple([z.literal('routine'), z.literal('need'), z.literal('temperament'), z.literal('memory')])]),
    needsPerPerson: z.literal(1),
    memoryAtStart: z.literal('empty'),
    src: text, reason: text,
    readBy: ground({ fields: z.record(id, text) }), // Legion lesson: every field a person carries names the rule that reads it
  }),
  tiers: z.strictObject({
    simulated: ground({ carries: z.array(id).min(1) }),
    background: ground({ carries: z.array(id) }),
  }),
  scales: z.strictObject({ bravery: Scale, resentment: Scale, fear: Scale, loyalty: Scale }),
  temperaments: z.strictObject({ idealist: Temperament, mercenary: Temperament, fearful: Temperament, loyal: Temperament }),
  institutions: ground({ kinds: z.array(z.enum(['employer', 'civic'])).min(1), rule: text }),
  approaches: z.strictObject({ ask: Approach, pay: Approach, help: Approach, pressure: Approach, src: text }),
  needs: z.strictObject({
    rule: text,
    list: z.record(id, Need).refine((l) => Object.keys(l).length > 0, { message: 'the need list is empty' }),
    requiresKinds: z.strictObject(Object.fromEntries(NEED_REQUIRES.map((k) => [k, text])) as Record<(typeof NEED_REQUIRES)[number], typeof text>),
  }),
  // optional so that the rules a recorded run pinned (before the lover and the secret) still parse: the tools read an absent `secrets` as none
  secrets: ground({
    rule: text,
    perPerson: z.strictObject({ min: z.literal(0), max: z.literal(1) }),
    list: z.strictObject(Object.fromEntries(SECRETS.map((k) => [k, ground({ requires: z.enum(['nothing', 'lover-tie']), learnedBy: z.array(z.string().regex(/^[a-z]+\.[a-z-]+$/)).min(1) })])) as Record<(typeof SECRETS)[number], ReturnType<typeof ground>>),
  }).optional(),
  ties: ground({
    kinds: z.strictObject({ family: Tie, workmate: Tie, regular: Tie, debtor: Tie, rival: Tie, lover: Tie.optional() }),
    betweenKinds: z.strictObject({ ...(Object.fromEntries(TIE_BETWEEN.filter((k) => k !== 'routine-meet').map((k) => [k, text])) as Record<Exclude<(typeof TIE_BETWEEN)[number], 'routine-meet'>, typeof text>), 'routine-meet': text.optional() }),
    perPerson: z.strictObject({ min: z.number().int().nonnegative(), max: z.number().int().positive() }),
    noSelf: z.literal(true), onePerKindPerPair: z.literal(true), simulatedOnly: z.literal(true),
  }),
  knowledge: ground({
    rule: text,
    identity: z.array(z.enum(IDENTITY_STATES)).min(1),
    learnedBy: z.array(z.enum(['witnessed', 'told'])).min(1),
    cellAtStart: z.literal('nothing'),
    needsKnown: z.array(z.strictObject({ position: id, of: z.array(z.enum(KNOWN_VIA)).min(1), src: text })),
  }),
  talker: z.strictObject({
    when: z.literal('questioned'),
    test: z.strictObject({ left: z.array(scale).min(1), op: z.literal('>'), right: z.array(scale).min(1) }),
    deterministic: z.literal(true),
    gives: z.literal('known-facts'),
    facts: z.array(z.enum(FACTS)).min(1),
    src: text,
    lie: ground({
      when: z.strictObject({ talker: z.literal(false), helped: z.literal(true), braveryAtLeast: z.number().int().nonnegative() }),
      gives: z.strictObject({ hero: z.enum(['false-lead', 'nothing']), otherwise: z.enum(['false-lead', 'nothing']) }),
    }),
  }),
  positions: ground({ rule: text, byCategory: z.record(id, z.array(id).min(1)) }),
  temperamentsBy: z.strictObject({
    rule: text,
    trades: z.record(id, Trade),
    tradesSrc: text,
    roles: z.record(id, Role),
    employers: z.record(id, Employer),
    mix: ground({
      rule: text,
      rowMaxShare: z.number().gt(0).lte(1),
      unitMinDistinct: z.number().int().min(1),
      whenStaffAtLeast: z.number().int().min(1),
    }),
  }),
  names: ground({
    scope: z.literal('district'), pool: text,
    form: z.strictObject({ parts: z.tuple([z.literal('given'), z.literal('family')]), pattern: text }),
    noEraDate: text,
    avoid: z.array(text),
    minGiven: z.number().int().positive(), minFamily: z.number().int().positive(),
    origins: z.array(id).min(1),
    minOrigins: z.number().int().positive(),
    maxOriginShare: z.number().gt(0).lte(1),
    uniqueFullName: z.literal(true),
    background: z.literal('unnamed'),
    pinned: z.array(Pinned),
  }),
}).superRefine((r, ctx) => {
  const bad = (path: (string | number)[], message: string) => ctx.addIssue({ code: 'custom', path, message });
  // Starting values sit on their scales.
  for (const [t, v] of Object.entries(r.temperaments)) {
    for (const s of SCALES) {
      const sc = r.scales[s];
      if (v.start[s] < sc.min || v.start[s] > sc.max) bad(['temperaments', t, 'start', s], `${s} ${v.start[s]} is off its scale ${sc.min}–${sc.max}`);
    }
    for (const a of v.respondsTo) if (v.refuses.includes(a)) bad(['temperaments', t], `${a} is both responded to and refused`);
  }
  // Every need is met by an approach that needs the need known (§04: Help and Pressure require it).
  for (const [n, v] of Object.entries(r.needs.list)) {
    if (!v.metBy.some((a) => r.approaches[a].requires === 'need')) bad(['needs', 'list', n, 'metBy'], 'no approach that needs the need known meets it');
    if (v.help === 'cash' && !v.metBy.includes('help')) bad(['needs', 'list', n, 'help'], 'help is priced but not an approach for this need');
  }
  // Talker sides use each scale at most once.
  const sides = [...r.talker.test.left, ...r.talker.test.right];
  if (new Set(sides).size !== sides.length) bad(['talker', 'test'], 'a scale appears twice in the Talker test');
  if (r.talker.lie.when.braveryAtLeast > r.scales.bravery.max) bad(['talker', 'lie', 'when'], 'braveryAtLeast is off the bravery scale');
  if (r.ties.perPerson.min > r.ties.perPerson.max) bad(['ties', 'perPerson'], 'min > max');
  // Every field a simulated person carries has a reader, and nothing is read that is not carried.
  const carried = r.tiers.simulated.carries, readers = Object.keys(r.person.readBy.fields);
  for (const f of carried) if (!readers.includes(f)) bad(['person', 'readBy', 'fields'], `${f} is carried but no rule reads it`);
  for (const f of readers) if (!carried.includes(f)) bad(['person', 'readBy', 'fields', f], `${f} is read but not carried`);
  // Introductions (§04) need at least one tie kind that carries them.
  if (!Object.values(r.ties.kinds).some((t) => t.introduces)) bad(['ties', 'kinds'], 'no tie kind introduces');
  // Knowledge: a need is known only through a known position; the cell is unknown at the start (memory empty).
  const allPositions = new Set(Object.values(r.positions.byCategory).flat());
  for (const [i, k] of r.knowledge.needsKnown.entries()) if (!allPositions.has(k.position)) bad(['knowledge', 'needsKnown', i], `position ${k.position} is unknown`);
  // Mix: a share cap no row can meet, or more distinct temperaments than exist, is an error.
  const mix = r.temperamentsBy.mix, nT = Object.keys(r.temperaments).length;
  if (mix.rowMaxShare * nT < 1) bad(['temperamentsBy', 'mix', 'rowMaxShare'], 'rowMaxShare × temperaments < 1: no row can satisfy it');
  if (mix.unitMinDistinct > nT) bad(['temperamentsBy', 'mix', 'unitMinDistinct'], 'more distinct temperaments required than exist');
  if (mix.unitMinDistinct > mix.whenStaffAtLeast) bad(['temperamentsBy', 'mix'], 'unitMinDistinct > whenStaffAtLeast: a unit that small cannot carry that many');
  // Positions: every category has exactly one keeper, first.
  for (const [c, ps] of Object.entries(r.positions.byCategory)) {
    if (ps[0] !== 'keeper' || ps.filter((p) => p === 'keeper').length !== 1) bad(['positions', 'byCategory', c], 'a category leads with exactly one keeper');
    if (new Set(ps).size !== ps.length) bad(['positions', 'byCategory', c], 'a position is listed twice');
  }
  // Trades name a known category; roles and employers resolve.
  const { trades, roles, employers } = r.temperamentsBy;
  for (const [t, v] of Object.entries(trades)) {
    if (!r.positions.byCategory[v.category]) bad(['temperamentsBy', 'trades', t], `category ${v.category} has no positions`);
    if (new Set(v.allow).size !== v.allow.length) bad(['temperamentsBy', 'trades', t], 'a temperament is allowed twice');
  }
  for (const [ro, v] of Object.entries(roles)) {
    if (trades[ro]) bad(['temperamentsBy', 'roles', ro], 'a role shares a trade id');
    if (v.employer !== 'none' && !employers[v.employer] && !trades[v.employer]) bad(['temperamentsBy', 'roles', ro], `employer ${v.employer} is unknown`);
    if (new Set(v.allow).size !== v.allow.length) bad(['temperamentsBy', 'roles', ro], 'a temperament is allowed twice');
  }
  for (const [e, v] of Object.entries(employers)) if (v.trade && !trades[v.trade]) bad(['temperamentsBy', 'employers', e], `trade ${v.trade} is unknown`);
  // Names: the pattern compiles, origins are distinct, a mixed cast is possible, pins resolve.
  let re: RegExp | undefined;
  try { re = new RegExp(r.names.form.pattern, 'u'); } catch { bad(['names', 'form', 'pattern'], 'the name pattern does not compile'); }
  if (new Set(r.names.origins).size !== r.names.origins.length) bad(['names', 'origins'], 'an origin is listed twice');
  if (r.names.minOrigins > r.names.origins.length) bad(['names', 'minOrigins'], 'more origins required than exist');
  if (r.names.minOrigins * r.names.maxOriginShare < 1) bad(['names', 'maxOriginShare'], 'minOrigins × maxOriginShare < 1: no pool can satisfy both');
  for (const [i, p] of r.names.pinned.entries()) {
    if (re && !re.test(p.name)) bad(['names', 'pinned', i], `${p.name} fails the name pattern`);
    if (r.names.avoid.includes(p.name)) bad(['names', 'pinned', i], `${p.name} is on the avoid list`);
    if (!employers[p.employer]) bad(['names', 'pinned', i], `employer ${p.employer} is unknown`);
    if (p.role && !roles[p.role]) bad(['names', 'pinned', i], `role ${p.role} is unknown`);
    if (p.position) {
      const e = employers[p.employer];
      const cat = e?.trade ? trades[e.trade]?.category : undefined;
      if (!cat || !r.positions.byCategory[cat]?.includes(p.position)) bad(['names', 'pinned', i], `position ${p.position} is not one of ${p.employer}'s`);
    }
  }
});

export type PeopleRules = z.infer<typeof PeopleRules>;
export type Temperament = (typeof TEMPERAMENTS)[number];
export type Scales = Record<(typeof SCALES)[number], number>;

/** §04 Talker test, read from the rules: questioned, a person gives up known facts if left > right. Deterministic. */
export const isTalker = (rules: PeopleRules, s: Scales): boolean => {
  const sum = (ks: readonly (typeof SCALES)[number][]) => ks.reduce((n, k) => n + s[k], 0);
  return sum(rules.talker.test.left) > sum(rules.talker.test.right);
};

/** Temperaments an occupation (trade) or role may carry; undefined = unknown, which the checker rejects. */
export const allowedTemperaments = (rules: PeopleRules, occupationOrRole: string): readonly Temperament[] | undefined =>
  rules.temperamentsBy.trades[occupationOrRole]?.allow ?? rules.temperamentsBy.roles[occupationOrRole]?.allow;
