// suborned.rulebook/1: every event the city simulation runs, contracts/rules/rulebook.json (Keel, brief 010).
// Each event carries what it needs (conditions), what it charges (price lines), what it changes (outcomes, also) and who sees it
// (witnesses), as GDD §11 says. Conditions are keyed by `k` and effects by `do`, always the first key, so the C# engine reads them
// as tagged unions (System.Text.Json polymorphism). Every number lives once in `values`, tagged [B]/[S]/[L], or is read from
// people.rules.json through a `p:` ref; nothing people.rules.json defines (scales, temperaments, needs, ties, talker, knowledge)
// is restated here, and checkRulebook() rejects any id that file does not know (GDD §01: unknown = reject).
import { z } from 'zod';
import type { PeopleRules } from '../people/people.rules.schema.ts';

const id = z.string().regex(/^[a-z][a-z0-9-]*$/);
const eventId = z.string().regex(/^[a-z][a-z0-9-]*\.[a-z][a-z0-9-]*$/);
const text = z.string().min(1);
/** A number: `v:<value id>` (this file's values) or `p:<path>` (a number in people.rules.json). */
export const Ref = z.string().regex(/^(v:[a-z][a-z0-9-]*|p:[a-zA-Z][a-zA-Z0-9.-]*)$/);
export const TAGS = ['B', 'S', 'L'] as const;
export const BANDS = ['hero', 'neutral', 'menace'] as const;
export const FITS = ['suits', 'worse', 'refused'] as const;
export const KNOWS = ['routine', 'need', 'temperament', 'refused'] as const; // refused: the cell knows this event's approach is refused by this person
export const PHASES = ['mission-start', 'mission', 'between-missions'] as const;
export const BY = ['scout', 'fixer', 'tech', 'enforcer', 'contact', 'person', 'investigator', 'city'] as const;

const Band = z.enum(BANDS);
const Fit = z.enum(FITS);
const Know = z.enum(KNOWS);
const grounded = { tag: z.enum(TAGS), src: text, proposed: z.literal(true).optional(), reason: text.optional() };
const needsReason = (v: unknown) => { const o = v as { proposed?: true; reason?: string }; return !o.proposed || !!o.reason; };
const reasonMsg = { message: 'a proposed rule needs a reason' };
const ground = <T extends z.ZodRawShape>(shape: T) => z.strictObject({ ...shape, ...grounded }).refine(needsReason, reasonMsg);

/** Conditions: a closed set. Each reads the event's subject (the person acted on, the witness, the contact) unless `of` names another. */
export const Cond = z.discriminatedUnion('k', [
  z.strictObject({ k: z.literal('tier'), is: z.enum(['simulated', 'background']) }), // people.rules tiers
  z.strictObject({ k: z.literal('turned'), is: z.boolean(), of: z.literal('friend').optional() }),
  z.strictObject({ k: z.literal('known'), what: Know }), // the cell knows this of the subject
  z.strictObject({ k: z.literal('requires'), approach: id }), // people.rules approaches[a].requires is known
  z.strictObject({ k: z.literal('need-met-by'), approach: id }), // the subject's need lists the approach in people.rules metBy
  z.strictObject({ k: z.literal('need-is'), in: z.array(id).min(1) }),
  z.strictObject({ k: z.literal('need-met'), is: z.boolean() }), // the need was met this mission
  z.strictObject({ k: z.literal('reach'), m: Ref }), // the acting operative stands within m
  z.strictObject({ k: z.literal('cash-covers') }), // cash ≥ the worst-case price of one order (preview)
  z.strictObject({ k: z.literal('memory'), act: id, has: z.boolean(), this: z.literal('mission').optional(), of: z.literal('friend').optional() }),
  z.strictObject({ k: z.literal('fit'), is: z.array(Fit).min(1) }), // fit table; reads the temperament
  z.strictObject({ k: z.literal('temperament'), in: z.array(id).min(1) }), // reads the temperament
  z.strictObject({ k: z.literal('standing'), band: z.array(Band).min(1) }), // the district's band
  z.strictObject({ k: z.literal('orders-reached'), is: z.boolean() }), // Help orders given, this one included, ≥ orders needed
  z.strictObject({ k: z.literal('reports') }), // any `reports` row holds
  z.strictObject({ k: z.literal('reports-approach') }), // people.rules temperaments[t].reportsApproach
  z.strictObject({ k: z.literal('outcome'), is: z.array(id).min(1) }), // in `also` and `witnesses`: the outcome that applied
  z.strictObject({ k: z.literal('talker'), is: z.boolean() }), // people.rules talker.test
  z.strictObject({ k: z.literal('lies'), is: z.boolean() }), // people.rules talker.lie.when
  z.strictObject({ k: z.literal('holds-fact'), is: z.boolean(), how: z.enum(['any', 'witnessed']).optional() }), // memory yields a people.rules talker fact
  z.strictObject({ k: z.literal('scale'), scale: id, op: z.enum(['<', '<=', '==', '>=', '>']), than: z.union([Ref, z.strictObject({ scale: id })]) }),
  z.strictObject({ k: z.literal('trail'), atLeast: Ref }), // campaign Trail
  z.strictObject({ k: z.literal('keeper-position') }), // the subject's position is one of people.rules knowledge.needsKnown[].position
  z.strictObject({ k: z.literal('introduces-along') }), // a tie to the friend of a kind people.rules says introduces
  z.strictObject({ k: z.literal('tie'), kinds: z.array(id).min(1) }), // the subject tie is one of these people.rules kinds
  z.strictObject({ k: z.literal('tie-between-holds'), is: z.boolean() }), // people.rules ties.kinds[k].between still holds
  z.strictObject({ k: z.literal('act'), in: z.array(id).min(1) }), // the traceable act seen
  z.strictObject({ k: z.literal('within'), m: Ref, sightline: z.literal('3d') }), // §05 witness range, clear sightline in 3D
  z.strictObject({ k: z.literal('cell'), is: z.boolean() }), // the subject is a cell operative
  z.strictObject({ k: z.literal('called'), is: z.boolean() }), // the Fixer called this contact in (§03 Call)
  z.strictObject({ k: z.literal('exposed'), is: z.boolean() }), // `exposure` rule
  z.strictObject({ k: z.literal('count'), event: eventId, below: Ref }), // times this event ran for the subject this break
]);
export type Cond = z.infer<typeof Cond>;

/** Effects: a closed set. */
export const Eff = z.discriminatedUnion('do', [
  z.strictObject({ do: z.literal('charge'), parts: z.array(z.enum(['usd', 's', 'trail'])).min(1) }), // the price lines that hold
  z.strictObject({ do: z.literal('turn') }),
  z.strictObject({ do: z.literal('move'), on: id }), // a ladders.moves row (K2), clamped to people.rules scales
  z.strictObject({ do: z.literal('need-met') }),
  z.strictObject({ do: z.literal('learn'), what: z.array(Know).min(1), of: z.enum(['subject', 'needs-known', 'friend']) }), // the cell learns
  z.strictObject({ do: z.literal('forget'), what: z.array(Know).min(1) }), // the cell no longer knows
  z.strictObject({ do: z.literal('remember'), act: id }), // a memory entry on the subject (K1)
  z.strictObject({ do: z.literal('report') }), // a witness report: an incident for Alert, a lead for investigators
  z.strictObject({ do: z.literal('expose') }), // the subject's jobs this mission count as exposed
  z.strictObject({ do: z.literal('trail'), add: Ref, per: z.literal('incident') }),
  z.strictObject({ do: z.literal('standing'), step: Ref, per: z.literal('incident') }), // sign from traceable[act].standing
  z.strictObject({ do: z.literal('alert'), atLeast: Ref, per: z.literal('incident') }),
  z.strictObject({ do: z.literal('lead'), from: z.enum(['facts', 'lie']) }),
  z.strictObject({ do: z.literal('spend-hop') }),
  z.strictObject({ do: z.literal('tell'), along: z.array(id).min(1), hops: Ref }), // K3
  z.strictObject({ do: z.literal('lapse-tie') }),
  z.strictObject({ do: z.literal('replace-need'), stream: id }), // a streams.keys key
  z.strictObject({ do: z.literal('schedule'), fight: z.enum(['ambush']) }),
  z.strictObject({ do: z.literal('debrief'), line: id }), // a debrief.lines key (K7)
]);
export type Eff = z.infer<typeof Eff>;

/** The bounds of a value by its unit, the same in the C# loader (Rules.cs): a duration is at least 1 s (Redline E3), nothing wraps (E9). */
export const UNIT_BOUNDS = {
  s: [1, 3600], usd: [-1e9, 1e9], m: [0, 1000], cm: [0, 100000], 'cm-per-s': [1, 10000], trail: [0, 100], alert: [0, 3], standing: [-1000, 1000],
  orders: [0, 10], 'scale-steps': [-10, 10], entries: [1, 100], hops: [0, 10], count: [0, 100], 'per-mille': [0, 1000000], ticks: [1, 36000],
} as const;
const Value = ground({
  v: z.number().int(), // integer state (§11)
  unit: z.enum(Object.keys(UNIT_BOUNDS) as [keyof typeof UNIT_BOUNDS, ...(keyof typeof UNIT_BOUNDS)[]]),
}).refine((x) => { const [lo, hi] = UNIT_BOUNDS[x.unit]; return x.v >= lo && x.v <= hi; }, { message: 'a value is outside its unit\'s bounds (a duration is at least 1 s)' });
export const Line = z.strictObject({
  line: id, // the preview label key (K8)
  when: z.array(Cond).optional(),
  usd: Ref.optional(), s: Ref.optional(), trail: Ref.optional(),
  orders: Ref.optional(), // Help orders this line adds (base: the orders one approach takes)
}).refine((l) => [l.usd, l.s, l.trail, l.orders].some((x) => x !== undefined), { message: 'a price line prices something' });
export type Line = z.infer<typeof Line>;
const Outcome = z.strictObject({ id, when: z.array(Cond), changes: z.array(Eff).min(1) });
const Also = z.strictObject({ when: z.array(Cond).min(1), changes: z.array(Eff).min(1) });
const Witness = z.strictObject({ when: z.array(Cond).optional(), act: id });

const Weapon = z.strictObject({ damage: Ref, every: Ref, eff: Ref, max: Ref });

export const Event = z.strictObject({
  id: eventId,
  by: z.enum(BY),
  phase: z.enum(PHASES),
  ...grounded,
  needs: z.array(Cond),
  price: z.array(Line).optional(),
  outcomes: z.array(Outcome).min(1),
  also: z.array(Also).optional(),
  witnesses: z.array(Witness).optional(), // the traceable act this event is, for witness.sees; absent: not traceable
  how: z.array(text).optional(), // ordering and selection the engine must follow exactly
  order: text.optional(), // the player's choice this event takes, held in the replay as an order
  band: z.strictObject({ what: text, s: z.tuple([Ref, Ref]) }).optional(), // a check band for the mission checker, not a parameter
})
  .refine(needsReason, reasonMsg)
  .refine((e) => e.outcomes[e.outcomes.length - 1]!.when.length === 0, { message: 'the last outcome has no conditions, so every event resolves' })
  .refine((e) => new Set(e.outcomes.map((o) => o.id)).size === e.outcomes.length, { message: 'outcome ids repeat' });
export type Event = z.infer<typeof Event>;

const StandingRow = ground({ band: Band, temperament: id, approaches: z.array(id).min(1), fit: Fit });
const ApproachFit = ground({ fit: Fit });
const Report = ground({ id, when: z.array(Cond).min(1) });
const Move = ground({ add: z.array(z.strictObject({ scale: id, by: Ref })).min(1) });
const MemAct = z.strictObject({ facts: z.union([z.array(id).min(1), z.literal('as-told')]) });
const Traceable = ground({ standing: z.enum(['hero', 'menace', 'none']) });
const DebriefLine = z.strictObject({ fields: z.array(id).min(1), src: text });

export const Rulebook = z.strictObject({
  schema: z.literal('suborned.rulebook/1'),
  author: text, gdd: text, rule: text,
  reads: z.strictObject({
    people: z.strictObject({ path: z.literal('contracts/people/people.rules.json'), schema: z.literal('suborned.people.rules/1'), reads: z.array(text).min(1) }),
  }),
  tags: z.strictObject({ B: text, S: text, L: text }),
  units: z.record(id, text),
  values: z.record(id, Value),
  standing: ground({ start: Ref, step: Ref, hero: Ref, menace: Ref, rule: text }),
  alert: ground({
    levels: z.tuple([z.literal('routine'), z.literal('inquiry'), z.literal('search'), z.literal('lockdown')]),
    byIncident: z.array(Ref).min(1), gunfire: Ref, violenceSeen: Ref, rule: text,
  }),
  fit: ground({
    from: z.strictObject({ suits: z.literal('respondsTo'), refused: z.literal('refuses'), otherwise: z.literal('worse') }),
    approaches: z.record(id, ApproachFit),
    standing: z.array(StandingRow),
    rule: text,
  }),
  reports: z.array(Report),
  ladders: ground({ rule: text, moves: z.record(id, Move) }),
  memory: ground({
    entry: z.tuple([z.literal('act'), z.literal('by'), z.literal('place'), z.literal('time'), z.literal('witnessed')]),
    cap: Ref, whenFull: text,
    faceFacts: z.record(id, id),
    acts: z.record(id, MemAct),
    reads: z.strictObject({ helped: id, refused: id }),
    rule: text,
  }),
  exposure: ground({ rule: text }),
  traceable: z.record(id, Traceable),
  // The cell's other verbs, security's answer to gunfire, and the cones that see (brief 011 A; Suture M-2, M-3): numbers only, every one a ref.
  cell: ground({
    speed: Ref, runSpeed: Ref, runAlert: Ref, reach: Ref, atReach: Ref,
    loop: z.strictObject({ s: Ref, trail: Ref, tether: Ref }),
    reroute: z.strictObject({ s: Ref, trail: Ref, away: Ref }),
    mislead: z.strictObject({ s: Ref, trail: Ref, away: Ref }),
    call: z.strictObject({ trail: Ref }),
    enter: z.strictObject({ s: Ref, alarm: Ref }),
    carry: z.strictObject({ s: Ref }),
    take: z.strictObject({ s: Ref, trailUnturned: Ref }),
    rule: text,
  }),
  security: ground({
    response: z.strictObject({ witness: Ref, seen: Ref, unseen: Ref, civilian: Ref, checkIn: Ref, rivalsUnseen: Ref }),
    checkIn: Ref, seal: Ref, speed: Ref,
    rivals: z.strictObject({ speed: Ref, drawn: Ref, retreat: Ref }),
    patrol: z.strictObject({ breakTo: Ref, period: Ref, chase: Ref }),
    rule: text,
  }),
  // Weapons, health and what a shot costs (Sky 2026-10-15, briefs/011/motive-fights.md): deterministic hits by accumulator.
  weapons: ground({
    pistol: Weapon, carbine: Weapon, pointBlank: Ref,
    acc: z.strictObject({ point: Ref, eff: Ref, long: Ref, start: Ref }),
    mod: z.strictObject({ moving: Ref, cover: Ref, vale: Ref, untrained: Ref }),
    draw: Ref, holster: Ref, aimNew: Ref, react: Ref, lineLapse: Ref, sidearmArms: Ref,
    health: z.strictObject({ enforcer: Ref, op: Ref, guard: Ref, officer: Ref, rival: Ref, patrol: Ref, civilian: Ref, responseOfficers: Ref }),
    costs: z.strictObject({ hit: Ref, down: Ref, drawSeenSecurity: Ref, drawSeenPeople: Ref, loudHold: Ref }),
    rule: text,
  }),
  // Cancel (Sky 2026-10-15 playtest): what stopping each verb mid-way costs or keeps; each line one of a closed set.
  cancel: ground({
    move: z.literal('stop'), draw: z.literal('holster'), fire: z.literal('cease'), watch: z.literal('lost'), listen: z.literal('lost'),
    turn: z.enum(['aborted', 'lost']), loop: z.literal('lost'), loopHeld: z.literal('ends'), reroute: z.literal('lost'), mislead: z.literal('lost'),
    enter: z.enum(['staff-report', 'quiet']), take: z.enum(['vale-reports', 'quiet']), carry: z.literal('lost'), carrying: z.literal('put-down'),
    rule: text,
  }),
  perception: ground({
    near: Ref, eye: Ref,
    door: z.strictObject({ reach: Ref, half: Ref }),
    facing: z.strictObject({ reach: Ref, half: Ref }),
    security: z.strictObject({ reach: Ref, half: Ref }),
    rivals: z.strictObject({ reach: Ref, half: Ref }),
    patrol: z.strictObject({ depth: Ref, halfBase: Ref, halfPerMille: Ref }),
    rule: text,
  }),
  fight: ground({
    safe: z.strictObject({ hostiles: Ref, minus: Ref, warn: Ref, ambush: Ref }),
    loud: z.strictObject({ officers: Ref, alert: Ref, civilian: Ref }),
    hold: Ref, traced: z.boolean(), tracedReason: text, rule: text,
  }),
  investigation: ground({ base: Ref, perAlert: Ref, hops: Ref, rule: text }),
  preview: ground({ rule: text, unknown: z.literal('?'), knownBy: z.array(eventId).min(1), knownByRule: text }),
  streams: ground({
    hash: z.literal('fnv1a-32'), generator: z.literal('mulberry32'), draw: z.literal('integer'),
    keys: z.record(id, z.string().regex(/^[a-z{}/-]+$/)), weightScale: Ref, weights: text, rule: text,
  }),
  phases: ground({ 'mission-start': z.array(eventId), 'between-missions': z.array(eventId), iterate: text }),
  debrief: ground({ lines: z.record(id, DebriefLine), rule: text }),
  events: z.array(Event).min(1),
}).superRefine((rb, ctx) => {
  const bad = (path: (string | number)[], message: string) => ctx.addIssue({ code: 'custom', path, message });
  // Every v: ref names a value, and every value is read by something (a value nothing reads is trivia: the Legion lesson).
  const refs = collectRefs(rb);
  for (const r of refs) if (r.startsWith('v:') && !rb.values[r.slice(2)]) bad(['values'], `${r} names no value`);
  const used = new Set(refs.filter((r) => r.startsWith('v:')).map((r) => r.slice(2)));
  for (const v of Object.keys(rb.values)) if (!used.has(v)) bad(['values', v], `${v} is read by no rule`);
  // Events: unique ids; phases list events of that phase, each once, and every event of a listed phase is listed.
  const byId = new Map<string, Event>();
  for (const [i, e] of rb.events.entries()) { if (byId.has(e.id)) bad(['events', i, 'id'], `${e.id} repeats`); byId.set(e.id, e); }
  for (const ph of ['mission-start', 'between-missions'] as const) {
    const listed = rb.phases[ph];
    if (new Set(listed).size !== listed.length) bad(['phases', ph], 'an event is listed twice');
    for (const x of listed) if (byId.get(x)?.phase !== ph) bad(['phases', ph], `${x} is not a ${ph} event`);
    for (const e of rb.events) if (e.phase === ph && !listed.includes(e.id)) bad(['phases', ph], `${e.id} is a ${ph} event but not in the phase order`);
  }
  for (const k of rb.preview.knownBy) if (!byId.has(k)) bad(['preview', 'knownBy'], `${k} is no event`);
  // Cross-references inside the file.
  const conds = collect(rb, 'k') as Cond[];
  const effs = collect(rb, 'do') as Eff[];
  for (const c of conds) {
    if (c.k === 'act') for (const a of c.in) if (!rb.traceable[a]) bad(['traceable'], `act ${a} is not traceable`);
    if (c.k === 'memory' && !rb.memory.acts[c.act]) bad(['memory', 'acts'], `memory act ${c.act} is unknown`);
    if (c.k === 'count' && !byId.has(c.event)) bad(['events'], `count reads unknown event ${c.event}`);
  }
  for (const f of effs) {
    if (f.do === 'move' && !rb.ladders.moves[f.on]) bad(['ladders', 'moves'], `move ${f.on} is unknown`);
    if (f.do === 'remember' && !rb.memory.acts[f.act]) bad(['memory', 'acts'], `remember ${f.act} is unknown`);
    if (f.do === 'debrief' && !rb.debrief.lines[f.line]) bad(['debrief', 'lines'], `debrief line ${f.line} is unknown`);
    if (f.do === 'replace-need' && !rb.streams.keys[f.stream]) bad(['streams', 'keys'], `stream ${f.stream} is unknown`);
  }
  for (const e of rb.events) {
    const outs = new Set(e.outcomes.map((o) => o.id));
    for (const c of [...(e.also ?? []).flatMap((a) => a.when), ...(e.witnesses ?? []).flatMap((w) => w.when ?? [])])
      if (c.k === 'outcome') for (const o of c.is) if (!outs.has(o)) bad(['events', e.id], `outcome ${o} is not one of ${e.id}'s`);
    for (const w of e.witnesses ?? []) if (!rb.traceable[w.act]) bad(['events', e.id, 'witnesses'], `${w.act} is not traceable`);
    if (e.price && e.price[0]?.line !== 'base') bad(['events', e.id, 'price'], 'the first price line is the base');
  }
  for (const v of Object.values(rb.memory.reads)) if (!rb.memory.acts[v]) bad(['memory', 'reads'], `${v} is not a memory act`);
  // Fit: one row per band × temperament × approach, so the table is a function.
  const seen = new Set<string>();
  for (const r of rb.fit.standing) for (const a of r.approaches) {
    const key = `${r.band}/${r.temperament}/${a}`;
    if (seen.has(key)) bad(['fit', 'standing'], `${key} has two rows`);
    seen.add(key);
  }
});
export type Rulebook = z.infer<typeof Rulebook>;

/** Every Ref string anywhere in the rulebook, by walking it. */
export function collectRefs(v: unknown): string[] {
  const out: string[] = [];
  const walk = (x: unknown): void => {
    if (typeof x === 'string') { if (/^(v|p):/.test(x) && Ref.safeParse(x).success) out.push(x); return; }
    if (Array.isArray(x)) { x.forEach(walk); return; }
    if (x && typeof x === 'object') for (const c of Object.values(x)) walk(c);
  };
  walk(v);
  return out;
}

/** Every object in the rulebook carrying `key` as a property (k: conditions, do: effects). */
function collect(v: unknown, key: string): unknown[] {
  const out: unknown[] = [];
  const walk = (x: unknown): void => {
    if (Array.isArray(x)) { x.forEach(walk); return; }
    if (x && typeof x === 'object') { if (key in x) out.push(x); for (const c of Object.values(x)) walk(c); }
  };
  walk(v);
  return out;
}

/** Resolve a Ref to its integer. */
export function num(rb: Rulebook, people: PeopleRules, ref: string): number {
  if (ref.startsWith('v:')) {
    const v = rb.values[ref.slice(2)];
    if (!v) throw new Error(`${ref} names no value`);
    return v.v;
  }
  let x: unknown = people;
  for (const part of ref.slice(2).split('.')) x = x && typeof x === 'object' ? (x as Record<string, unknown>)[part] : undefined;
  if (typeof x !== 'number' || !Number.isInteger(x)) throw new Error(`${ref} is not an integer in people.rules.json`);
  return x;
}

/** Keys people.rules.json owns as definitions; the rulebook may name them as values, never as its own keys (never redefine).
 *  FIELDS may appear nowhere in the rulebook; SECTIONS may not be top-level sections of it (an event's `needs` is its conditions). */
export const PEOPLE_OWNED_FIELDS = ['respondsTo', 'refuses', 'reportsApproach', 'steps', 'metBy', 'maxPerPerson', 'maxIncoming', 'introduces', 'between', 'allow', 'needsKnown', 'timeS'] as const;
export const PEOPLE_OWNED_SECTIONS = ['person', 'tiers', 'scales', 'temperaments', 'institutions', 'approaches', 'needs', 'ties', 'knowledge', 'talker', 'positions', 'temperamentsBy', 'names'] as const;

/**
 * Check the rulebook against the people rules it reads (GDD §01: unknown = reject). Returns the problems; empty = clean.
 * Every p: ref resolves to an integer; every temperament, approach, scale, need, tie kind, fact and position named exists there;
 * Turn events map onto people.rules approaches; and no people-owned definition is restated.
 */
export function checkRulebook(rb: Rulebook, people: PeopleRules): string[] {
  const out: string[] = [];
  for (const r of collectRefs(rb)) {
    if (!r.startsWith('p:')) continue;
    try { num(rb, people, r); } catch (e) { out.push((e as Error).message); }
  }
  const T = new Set(Object.keys(people.temperaments));
  const A = new Set(Object.keys(people.approaches).filter((k) => k !== 'src'));
  const S = new Set(Object.keys(people.scales));
  const N = new Set(Object.keys(people.needs.list));
  const K = new Set(Object.keys(people.ties.kinds));
  const F = new Set<string>(people.talker.facts);
  const want = (set: Set<string>, what: string, xs: readonly string[], where: string) => { for (const x of xs) if (!set.has(x)) out.push(`${where}: ${what} ${x} is not in people.rules.json`); };
  for (const r of rb.fit.standing) { want(T, 'temperament', [r.temperament], 'fit.standing'); want(A, 'approach', r.approaches, 'fit.standing'); }
  want(A, 'approach', Object.keys(rb.fit.approaches), 'fit.approaches');
  for (const [m, mv] of Object.entries(rb.ladders.moves)) want(S, 'scale', mv.add.map((a) => a.scale), `ladders.moves.${m}`);
  for (const [a, ma] of Object.entries(rb.memory.acts)) if (Array.isArray(ma.facts)) want(F, 'fact', ma.facts, `memory.acts.${a}`);
  want(F, 'fact', Object.values(rb.memory.faceFacts), 'memory.faceFacts');
  const walkConds = (rbAny: unknown) => collect(rbAny, 'k') as Cond[];
  for (const c of walkConds(rb)) {
    if (c.k === 'temperament') want(T, 'temperament', c.in, 'condition');
    if (c.k === 'requires' || c.k === 'need-met-by') want(A, 'approach', [c.approach], 'condition');
    if (c.k === 'need-is') want(N, 'need', c.in, 'condition');
    if (c.k === 'scale') { want(S, 'scale', [c.scale], 'condition'); if (typeof c.than === 'object') want(S, 'scale', [c.than.scale], 'condition'); }
    if (c.k === 'tie') want(K, 'tie kind', c.kinds, 'condition');
  }
  for (const f of collect(rb, 'do') as Eff[]) if (f.do === 'tell') want(K, 'tie kind', f.along, 'tell');
  // Each Turn event is one people.rules approach, and every approach has its event.
  const turns = rb.events.filter((e) => e.id.startsWith('turn.')).map((e) => e.id.slice(5));
  want(A, 'approach', turns, 'events turn.*');
  for (const a of A) if (!turns.includes(a)) out.push(`approach ${a} has no turn.${a} event`);
  // The talker test, the lie and the needsKnown positions are read where they live; they must exist to be read.
  if (!people.knowledge.needsKnown.length) out.push('keeper-position reads people.rules knowledge.needsKnown, which is empty');
  // Never redefine: no people-owned key appears as a key anywhere in the rulebook.
  for (const k of Object.keys(rb)) if ((PEOPLE_OWNED_SECTIONS as readonly string[]).includes(k)) out.push(`rulebook.${k}: people.rules.json owns the ${k} section; read it, never restate it`);
  const owned = new Set<string>(PEOPLE_OWNED_FIELDS);
  const walk = (x: unknown, path: string): void => {
    if (Array.isArray(x)) { x.forEach((c, i) => walk(c, `${path}[${i}]`)); return; }
    if (x && typeof x === 'object') for (const [k, c] of Object.entries(x)) { if (owned.has(k)) out.push(`${path}.${k}: people.rules.json owns ${k}; read it, never restate it`); walk(c, `${path}.${k}`); }
  };
  walk(rb, 'rulebook');
  return out;
}
