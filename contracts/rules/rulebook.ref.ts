// Reference evaluator for contracts/rules/rulebook.json (Keel, brief 010): the person-level rules run straight from the data —
// fit, price lines and the "?" preview, outcomes, ladder moves, memory, the Talker answer and the seeded need replacement.
// It is the golden behaviour the C# engine (brief 011 A) must match on rulebook.test.ts's cases; it is not the engine.
// Spatial and campaign conditions (reach, sightline, ties, exposure) need the world: the caller states them in `given`.
import { isTalker, type PeopleRules, type Scales } from '../people/people.rules.schema.ts';
import { num, type Cond, type Eff, type Event, type Rulebook } from './rulebook.schema.ts';

export type Band = 'hero' | 'neutral' | 'menace';
export type Fit = 'suits' | 'worse' | 'refused';
export type Know = 'routine' | 'need' | 'temperament' | 'refused' | 'secret';
export type Mem = { act: string; by: string; place: string; time: number; witnessed: boolean; told?: string[] };
export type Subject = {
  id: string; tier: 'simulated' | 'background'; temperament: string; need: string;
  scales: Scales; memory: Mem[]; turned: boolean; needMet?: boolean;
};
export type Ctx = {
  subject: Subject;
  band: Band;
  knows: ReadonlySet<Know>; // what the cell knows of the subject
  refusedKnown?: ReadonlySet<string>; // approaches the cell has seen this person refuse (a refusal teaches only that: Aster, 2026-10-15)
  cash?: number;
  trail?: number;
  outcome?: string; // set while `also` and `witnesses` are read
  given?: Partial<Record<Cond['k'], boolean>>; // world-side conditions the caller decides
};
type Tri = boolean | '?';

export const event = (rb: Rulebook, id: string): Event => {
  const e = rb.events.find((x) => x.id === id);
  if (!e) throw new Error(`no event ${id}`);
  return e;
};

export function bandOf(rb: Rulebook, people: PeopleRules, standing: number): Band {
  if (standing >= num(rb, people, rb.standing.hero)) return 'hero';
  if (standing <= num(rb, people, rb.standing.menace)) return 'menace';
  return 'neutral';
}

/** Sky Q4: a standing row wins, then an approach row, then people.rules respondsTo (suits) / refuses (refused) / otherwise worse. */
export function fitOf(rb: Rulebook, people: PeopleRules, approach: string, temperament: string, band: Band): Fit {
  const row = rb.fit.standing.find((r) => r.band === band && r.temperament === temperament && r.approaches.includes(approach));
  if (row) return row.fit;
  const a = rb.fit.approaches[approach];
  if (a) return a.fit;
  const t = people.temperaments[temperament as keyof PeopleRules['temperaments']];
  if (!t) throw new Error(`temperament ${temperament} is unknown (unknown = reject)`);
  if ((t.refuses as readonly string[]).includes(approach)) return 'refused';
  if ((t.respondsTo as readonly string[]).includes(approach)) return 'suits';
  return 'worse';
}

const approachOf = (e: Event) => (e.id.startsWith('turn.') ? e.id.slice(5) : undefined);
const readsTemperament = (c: Cond): boolean => ['fit', 'temperament', 'reports', 'reports-approach', 'orders-reached'].includes(c.k);

/** The facts a person holds: each memory entry's act facts, plus the face fact of who did it, plus what they were told. */
export function factsOf(rb: Rulebook, s: Subject, how: 'any' | 'witnessed' = 'any'): Set<string> {
  const out = new Set<string>();
  for (const m of s.memory) {
    const act = rb.memory.acts[m.act];
    if (!act) throw new Error(`memory act ${m.act} is unknown`);
    if (act.facts === 'as-told') { if (how === 'any') for (const f of m.told ?? []) out.add(f); continue; }
    for (const f of act.facts) out.add(f);
    const face = rb.memory.faceFacts[m.by];
    if (face) out.add(face);
  }
  return out;
}

const has = (s: Subject, act: string) => s.memory.some((m) => m.act === act);
const cmp = (a: number, op: string, b: number) => (op === '<' ? a < b : op === '<=' ? a <= b : op === '==' ? a === b : op === '>=' ? a >= b : a > b);

/** One condition, against the truth (the engine knows every temperament). */
export function holds(rb: Rulebook, people: PeopleRules, c: Cond, x: Ctx, ev?: Event): boolean {
  const s = x.subject;
  switch (c.k) {
    case 'tier': return s.tier === c.is;
    case 'turned': if (c.of) break; return s.turned === c.is;
    case 'known': return x.knows.has(c.what) === (c.is ?? true);
    case 'leverage': {
      const r = people.approaches[c.approach as 'ask'].requires;
      const need = (r === 'nothing' || x.knows.has(r)) && ((people.needs.list[s.need]?.metBy as readonly string[] | undefined)?.includes(c.approach) ?? false);
      return need || x.knows.has('secret');
    }
    case 'requires': {
      const r = people.approaches[c.approach as 'ask'].requires;
      return r === 'nothing' || x.knows.has(r);
    }
    case 'need-met-by': return (people.needs.list[s.need]?.metBy as readonly string[] | undefined)?.includes(c.approach) ?? false;
    case 'need-is': return c.in.includes(s.need);
    case 'need-met': return !!s.needMet === c.is;
    case 'cash-covers': return x.cash === undefined || !ev || x.cash >= priceLines(rb, people, ev, x).worst.usd;
    case 'memory': if (c.of || c.this) break; return has(s, c.act) === c.has;
    case 'fit': { const a = ev && approachOf(ev); if (!a) throw new Error('fit outside a turn event'); return c.is.includes(fitOf(rb, people, a, s.temperament, x.band)); }
    case 'temperament': return c.in.includes(s.temperament);
    case 'standing': return c.band.includes(x.band);
    case 'orders-reached': {
      if (!ev) throw new Error('orders-reached outside an event');
      const given = s.memory.filter((m) => m.act === rb.memory.reads.helped).length + 1;
      return (given >= priceLines(rb, people, ev, x).orders) === c.is;
    }
    case 'reports': return rb.reports.some((r) => r.when.every((rc) => holds(rb, people, rc, x, ev)));
    case 'reports-approach': return people.temperaments[s.temperament as 'loyal']?.reportsApproach ?? false;
    case 'outcome': return x.outcome !== undefined && c.is.includes(x.outcome);
    case 'talker': return isTalker(people, s.scales) === c.is;
    case 'lies': {
      const w = people.talker.lie.when;
      const lies = isTalker(people, s.scales) === w.talker && has(s, rb.memory.reads.helped) === w.helped && s.scales.bravery >= w.braveryAtLeast;
      return lies === c.is;
    }
    case 'holds-fact': return (factsOf(rb, s, c.how ?? 'any').size > 0) === c.is;
    case 'scale': {
      const b = typeof c.than === 'string' ? num(rb, people, c.than) : s.scales[c.than.scale as keyof Scales];
      return cmp(s.scales[c.scale as keyof Scales], c.op, b);
    }
    case 'trail': return (x.trail ?? 0) >= num(rb, people, c.atLeast);
    default: break;
  }
  const g = x.given?.[c.k];
  if (g === undefined) throw new Error(`condition ${c.k} needs the world: state it in ctx.given`);
  return g;
}

export type QuoteLine = { line: string; usd: number; s: number; trail: number; orders: number; holds: Tri };
export type Quote = {
  lines: QuoteLine[];
  perOrder: { usd: number; s: number; trail: number }; // what one order charges: the lines that hold
  worst: { usd: number; s: number; trail: number }; // lines that hold or show '?': the cash check (preview)
  orders: number; // orders of this approach that turn the person
  outcome: string; // the outcome the card shows: an id, or '?' while the temperament is unknown (Sky Q3)
};

/**
 * The action card (§02, K8, Sky Q3). Lines are evaluated against the truth for the charge; for the card, a line that reads the
 * temperament shows '?' while the cell does not know it. `perOrder` and `orders` are the truth; `worst` is what the card asks cash for.
 */
export function quote(rb: Rulebook, people: PeopleRules, ev: Event, x: Ctx): Quote {
  const p = priceLines(rb, people, ev, x);
  const a = approachOf(ev);
  const outcome = a && x.refusedKnown?.has(a) ? 'refused' : shownFor(people, x, (y) => resolve(rb, people, ev, { ...y, cash: undefined }).outcome);
  return { ...p, outcome };
}

/** Every temperament people.rules knows: what the card weighs an unknown temperament against. */
const temperaments = (people: PeopleRules) => Object.keys(people.temperaments);
/** The card's answer for a value that may read the temperament: the truth if the cell knows it, else the answer every temperament
 *  in this band gives alike, else '?' (Motive's review, other findings 3: Ask in a neutral district reads 'refused', not '?'). */
function shownFor<T>(people: PeopleRules, x: Ctx, f: (y: Ctx) => T): T | '?' {
  if (x.knows.has('temperament')) return f(x);
  const answers = temperaments(people).map((t) => f({ ...x, subject: { ...x.subject, temperament: t } }));
  return answers.every((a) => a === answers[0]) ? answers[0]! : '?';
}

function priceLines(rb: Rulebook, people: PeopleRules, ev: Event, x: Ctx): Omit<Quote, 'outcome'> {
  const lines: QuoteLine[] = (ev.price ?? []).map((l, i) => {
    const conds = l.when ?? [];
    const truth = conds.every((c) => holds(rb, people, c, x, ev));
    const shown: Tri = conds.some(readsTemperament) ? shownFor(people, x, (y) => conds.every((c) => holds(rb, people, c, y, ev))) : truth;
    const n = (r?: string) => (r ? num(rb, people, r) : 0);
    return { line: l.line, usd: n(l.usd), s: n(l.s), trail: n(l.trail), orders: l.orders ? n(l.orders) : i === 0 ? 1 : 0, holds: shown };
  });
  const truthOf = (q: QuoteLine, i: number) => (ev.price![i]!.when ?? []).every((c) => holds(rb, people, c, x, ev));
  const sum = (pick: (q: QuoteLine, i: number) => boolean) => lines.reduce((a, q, i) => (pick(q, i) ? { usd: a.usd + q.usd, s: a.s + q.s, trail: a.trail + q.trail } : a), { usd: 0, s: 0, trail: 0 });
  const perOrder = sum(truthOf);
  const worst = sum((q, i) => truthOf(q, i) || q.holds === '?');
  const orders = lines.reduce((n, q, i) => n + (truthOf(q, i) ? q.orders : 0), 0) || 1;
  return { lines, perOrder, worst, orders };
}

/** Whether the order can be given at all: every `needs` condition holds. */
export const needsHold = (rb: Rulebook, people: PeopleRules, ev: Event, x: Ctx): boolean => ev.needs.every((c) => holds(rb, people, c, x, ev));

/** The outcome that applies (first whose conditions all hold) and every change, `also` included, in order. */
export function resolve(rb: Rulebook, people: PeopleRules, ev: Event, x: Ctx): { outcome: string; changes: Eff[]; witnessed: string[] } {
  const o = ev.outcomes.find((oc) => oc.when.every((c) => holds(rb, people, c, x, ev)))!;
  const y = { ...x, outcome: o.id };
  const changes = [...o.changes, ...(ev.also ?? []).filter((a) => a.when.every((c) => holds(rb, people, c, y, ev))).flatMap((a) => a.changes)];
  const witnessed = (ev.witnesses ?? []).filter((w) => (w.when ?? []).every((c) => holds(rb, people, c, y, ev))).map((w) => w.act);
  return { outcome: o.id, changes, witnessed };
}

/** The person-side changes (move, remember, turn, need-met) applied to a copy; cell and world changes are left to the engine. */
export function apply(rb: Rulebook, people: PeopleRules, s: Subject, changes: Eff[], at: { by: string; place: string; time: number; witnessed: boolean }): Subject {
  const out: Subject = { ...s, scales: { ...s.scales }, memory: [...s.memory] };
  const cap = num(rb, people, rb.memory.cap);
  for (const c of changes) {
    if (c.do === 'turn') out.turned = true;
    if (c.do === 'need-met') out.needMet = true;
    if (c.do === 'move') for (const m of rb.ladders.moves[c.on]!.add) {
      const sc = people.scales[m.scale as keyof PeopleRules['scales']];
      const k = m.scale as keyof Scales;
      out.scales[k] = Math.max(sc.min, Math.min(sc.max, out.scales[k] + num(rb, people, m.by)));
    }
    if (c.do === 'remember' && out.tier === 'simulated') {
      out.memory.push({ act: c.act, ...at });
      if (out.memory.length > cap) out.memory.splice(forgetIndex(out.memory), 1);
    }
  }
  return out;
}

/** rulebook memory.whenFull: the oldest saw-act, saw-runner or told entry (lowest time, then first written); only when none is held, the oldest
 *  entry. People forget what they saw before what was done to them (Motive's review #9). */
export function forgetIndex(memory: readonly Mem[]): number {
  const pick = (ok: (m: Mem) => boolean) => {
    let i = -1;
    for (let j = 0; j < memory.length; j++) if (ok(memory[j]!) && (i < 0 || memory[j]!.time < memory[i]!.time)) i = j;
    return i;
  };
  const seen = pick((m) => ['saw-act', 'saw-runner', 'saw-gunfire', 'saw-violence', 'saw-weapon-drawn', 'saw-takedown', 'saw-carry', 'found-body', 'heard-alarm', 'told'].includes(m.act));
  return seen >= 0 ? seen : pick(() => true);
}

/** What an investigator gets from this person (investigator.question's outcome), read from the rulebook and people.rules. */
export function answer(rb: Rulebook, people: PeopleRules, s: Subject, band: Band, trail = 1): { outcome: string; gives: string[] } {
  const ev = event(rb, 'investigator.question');
  const r = resolve(rb, people, ev, { subject: s, band, knows: new Set(), trail });
  const facts = [...factsOf(rb, s)].sort((a, b) => people.talker.facts.indexOf(a as never) - people.talker.facts.indexOf(b as never));
  const gives = r.outcome === 'gives' || r.outcome === 'volunteers' ? facts : r.outcome === 'lies' ? [people.talker.lie.gives[band === 'hero' ? 'hero' : 'otherwise']] : []; // volunteers: a reporter gives what they hold without the Talker test (brief 013)
  return { outcome: r.outcome, gives };
}

// ---- seeded streams (rulebook.streams): FNV-1a 32 + mulberry32, integer draws ----

export function fnv1a32(key: string): number {
  let h = 0x811c9dc5;
  for (let i = 0; i < key.length; i++) { h ^= key.charCodeAt(i); h = Math.imul(h, 0x01000193); }
  return h >>> 0;
}

/** mulberry32 seeded by fnv1a32(key) (0 → 1): the same stream as tools/world/gen/common.ts rng(key), as 32-bit integers. */
export function stream(key: string): () => number {
  let s = fnv1a32(key) || 1;
  return () => {
    s = (s + 0x6d2b79f5) >>> 0;
    let t = s;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return (t ^ (t >>> 14)) >>> 0;
  };
}

/** Integer weighted draw: x = floor(u × total ÷ 2^32); walk the items in order subtracting each weight. */
export function drawWeighted<T>(key: string, items: readonly { item: T; weight: number }[]): T {
  if (!items.length) throw new Error(`nothing to draw for ${key}`);
  const total = items.reduce((n, i) => n + i.weight, 0);
  if (!items.every((i) => Number.isInteger(i.weight) && i.weight > 0)) throw new Error('weights are positive integers');
  let x = Number((BigInt(stream(key)()) * BigInt(total)) >> 32n);
  for (const i of items) { x -= i.weight; if (x < 0) return i.item; }
  return items[items.length - 1]!.item;
}

export type NeedHolder = { id: string; temperament: string; need: string; home: boolean; work: boolean; ties: readonly string[] };

/** need.replaced's candidates: people.rules needs.list order, `requires` met, metBy ∩ respondsTo, never the need just met. */
export function replacementNeeds(people: PeopleRules, p: NeedHolder): string[] {
  const resp = people.temperaments[p.temperament as keyof PeopleRules['temperaments']]!.respondsTo as readonly string[];
  return Object.entries(people.needs.list).filter(([n, d]) => {
    if (n === p.need) return false;
    const r = d.requires;
    const ok = r === 'nothing' || (r === 'home' && p.home) || (r === 'work' && p.work) || (r === 'family-tie' && p.ties.includes('family')) || (r === 'rival-tie' && p.ties.includes('rival'));
    return ok && (d.metBy as readonly string[]).some((a) => resp.includes(a));
  }).map(([n]) => n);
}

/** Sky Q2: the replacement need, from the person's own stream `{person}/need/{mission}`; weights are integers (per mille). */
export function replaceNeed(rb: Rulebook, people: PeopleRules, p: NeedHolder, mission: number, weights: Readonly<Record<string, number>>): string {
  const key = rb.streams.keys['need-replace']!.replace('{person}', p.id).replace('{mission}', String(mission));
  const cands = replacementNeeds(people, p);
  const weighted = cands.map((n) => ({ item: n, weight: weights[n] ?? 0 })).filter((w) => w.weight > 0);
  return drawWeighted(key, weighted.length ? weighted : cands.map((n) => ({ item: n, weight: 1 })));
}

/** A shot's accuracy in per mille (Sky 2026-10-15, briefs/011/motive-fights.md): the band's, times each modifier that applies, in the engine's
 *  order (Rules.Accuracy): the gait (moving = a walk, running = a run toward or across, runningAway; tuning.md §8.3), then cover, or a running
 *  target past point blank in its place (never both), then vale, then untrained; each rounded down. Inside point blank cover counts only with
 *  weapons.pointBlankCover (Sky 2026-10-21, tuning.md §8.4: false, as the lab). */
export type AccMod = 'moving' | 'running' | 'runningAway' | 'targetRunning' | 'cover' | 'vale' | 'untrained';
export function accuracy(rb: Rulebook, people: PeopleRules, weapon: 'pistol' | 'carbine', distanceM: number, mods: readonly AccMod[]): number {
  const w = rb.weapons, n = (r: string) => num(rb, people, r), pb = distanceM <= n(w.pointBlank);
  if (distanceM > n(w[weapon].max)) return 0;
  let a = pb ? n(w.acc.point) : distanceM <= n(w[weapon].eff) ? n(w.acc.eff) : n(w.acc.long);
  const step = (r: string) => { a = Math.floor((a * n(r)) / 1000); };
  if (mods.includes('moving')) step(w.mod.moving); else if (mods.includes('running')) step(w.mod.running!); else if (mods.includes('runningAway')) step(w.mod.runningAway!);
  if (mods.includes('targetRunning') && !pb) step(w.mod.targetRunning!); else if (mods.includes('cover') && (!pb || w.pointBlankCover)) step(w.mod.cover);
  if (mods.includes('vale')) step(w.mod.vale);
  if (mods.includes('untrained')) step(w.mod.untrained);
  return a;
}

/** Brief 027 (EXPERIMENTAL, fight.lab): a playground shot's pips (0 beyond the max ring, else 1–4) and its chance in per mille, as the engine's
 *  Rules.LabPips: the band (4 within weapons.pointBlank, 3 within the weapon's eff, 2 within its max), −1 untrained and −1 cover beyond point
 *  blank, −1 moving, +1 high, clamped 1–4; the chance is fight.lab.chance[pips − 1]. Tuning.md §8.3: running (toward or across) −runPips,
 *  floor 0; runningAway 0 pips whatever the band; 0 pips inside the max ring is fight.lab.pip0 (beyond max: pips 0, acc 0); targetRunning
 *  past point blank takes weapons.mod.targetRunning on the chance. */
export type LabWeaponId = 'sidearm' | 'carbine' | 'machine' | 'riot' | 'rifle';
export function labPips(rb: Rulebook, people: PeopleRules, weapon: LabWeaponId, distanceM: number, edges: readonly ('untrained' | 'cover' | 'moving' | 'running' | 'runningAway' | 'targetRunning' | 'high')[]): { pips: number; acc: number } {
  const lab = rb.fight.lab!, w = lab.weapons[weapon], n = (r: string) => num(rb, people, r), pb = n(rb.weapons.pointBlank);
  if (distanceM > n(w.max)) return { pips: 0, acc: 0 };
  let p = distanceM <= pb ? 4 : distanceM <= n(w.eff) ? 3 : 2;
  if (distanceM > pb) { if (edges.includes('untrained')) p--; if (edges.includes('cover')) p--; }
  const run = edges.includes('running');
  if (edges.includes('moving')) p--;
  if (run) p -= n(lab.runPips!);
  if (edges.includes('high')) p++;
  p = edges.includes('runningAway') ? 0 : Math.max(run ? 0 : 1, Math.min(4, p));
  let acc = p === 0 ? n(lab.pip0!) : n(lab.chance[p - 1]!);
  if (edges.includes('targetRunning') && distanceM > pb) acc = Math.floor((acc * n(rb.weapons.mod.targetRunning!)) / 1000);
  return { pips: p, acc };
}

/** Deterministic hits: the accumulator starts at acc.start; each shot adds the accuracy, and at 1000 the shot hits and 1000 comes off. */
export function hits(rb: Rulebook, people: PeopleRules, acc: number, shots: number): boolean[] {
  let a = num(rb, people, rb.weapons.acc.start);
  const out: boolean[] = [];
  for (let i = 0; i < shots; i++) { a += acc; const hit = a >= 1000; if (hit) a -= 1000; out.push(hit); }
  return out;
}

/** Brief 014 (Sky 2026-10-16, briefs/014/motive-standing.md): one district's Standing for the next mission, as the engine's StandingNext
 *  (sim GameInvestigation.cs). `moves` are the mission's seen and found moves and any killing found after it, in order; `tellers` the
 *  standing.word subjects (helped, pressured or knocked out; alive; with a tie in standing.word.ties) in person-id order. */
export type StandingMove = { delta: number; cause: string; who: string };
export type Teller = { who: string; done: readonly ('helped' | 'pressured' | 'knocked-out')[] };
export function standingNext(rb: Rulebook, people: PeopleRules, from: number, moves: readonly StandingMove[], tellers: readonly Teller[]) {
  const n = (r: string) => num(rb, people, r), w = rb.standing.word!, step = n(w.step), cap = n(w.cap);
  const all: StandingMove[] = [...moves];
  for (const t of tellers) { // harm outranks help: a threat outweighs a kindness
    const how = t.done.includes('pressured') ? 'pressured' : t.done.includes('knocked-out') ? 'knocked-out' : 'helped';
    all.push({ delta: how === 'helped' ? step : -step, cause: `word-${how}`, who: t.who });
  }
  const seen = moves.reduce((a, m) => a + m.delta, 0), wordUncapped = all.slice(moves.length).reduce((a, m) => a + m.delta, 0);
  const word = Math.max(-cap, Math.min(cap, wordUncapped)), at = from + seen + word, moved = all.length > 0;
  const gap = n(rb.standing.start) - at, drift = moved ? 0 : Math.sign(gap) * Math.min(n(rb.standing.drift!), Math.abs(gap));
  const to = at + drift, net = to - from - drift;
  let cause = !moved ? (drift < 0 ? 'drift-down' : drift > 0 ? 'drift-up' : 'steady') : net === 0 ? 'balanced' : '', who = '';
  if (moved && net !== 0) {
    const by = new Map<string, { sum: number; who: string }>();
    for (const m of all) { const g = by.get(m.cause) ?? { sum: 0, who: m.who }; g.sum += m.delta; by.set(m.cause, g); }
    let best = 0; for (const [c, g] of by) if (g.sum * Math.sign(net) > best) { best = g.sum * Math.sign(net); cause = c; who = g.who; }
  }
  return { to, band: bandOf(rb, people, to), cause, who, word, wordUncapped, drift };
}

/** Sky 2026-10-16 (briefs/014/motive-standing.md, a public fight): what one fight moves Standing by when it ends, as the engine's
 *  FightOutcome (sim GameCombat.cs): nothing unless public, else the first fight.public outcome whose conditions hold. */
export type Fight = { public: boolean; firedOn: readonly ('security' | 'rivals')[]; firstByCell: boolean; rivalsBroken: boolean; rivalsPublic?: 'menace' | 'neutral' };
export function fightStanding(rb: Rulebook, people: PeopleRules, f: Fight): { outcome: string; delta: number; cause: string } {
  const e = event(rb, 'fight.public'), pub = f.rivalsPublic ?? rb.fight.rivalsPublic ?? 'menace';
  if (!f.public) return { outcome: 'unheard', delta: 0, cause: '' };
  const ok = (c: Cond): boolean => {
    switch (c.k) {
      case 'fired-on': return c.in.some((x) => f.firedOn.includes(x));
      case 'rivals-public': return c.in.includes(pub);
      case 'fired-first': return f.firstByCell === c.is;
      case 'rivals-broken': return f.rivalsBroken === c.is;
      default: throw new Error(`fight.public: condition ${c.k} is not a fight's`);
    }
  };
  for (const o of e.outcomes) {
    if (!o.when.every(ok)) continue;
    const ch = o.changes.find((x) => x.do === 'fight-standing');
    if (!ch || ch.do !== 'fight-standing') return { outcome: o.id, delta: 0, cause: '' };
    return { outcome: o.id, delta: (ch.toward === 'hero' ? 1 : -1) * num(rb, people, ch.step), cause: ch.cause };
  }
  return { outcome: 'none', delta: 0, cause: '' };
}
