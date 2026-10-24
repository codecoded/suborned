// Brief 060 tier 1.1 (Suture): the mutation generator. A mutant is a seed (a Handover route or one of Sky's saved sessions) with one or two
// changes, seeded by the seed's id and the mutant's number, so the same seed set always gives the same mutants (any finding replays exactly):
//   shift        an order moves by 1-30 s (earlier or later)
//   drop         an order is left out
//   repeat       an order is given again 2-20 s later (half the time a Watch or Listen: the repeat Watch)
//   reassign     an order goes to another agent (who may be down, carried, inside or in the car by then)
//   swap-target  an order names another target (one the same verb names somewhere in the seeds, else any target)
//   insert       an order drawn from the vocabulary of every order the seeds contain, to a random agent at a random tick
// Drive samples (third person, brief 025) are context only: never picked, never inserted (a session holds thousands of them).
import type { Family, Item, Mutation, Order, Script, Seed } from './types';
import { FAMILIES } from './types';

export const OPS = ['scout', 'fixer', 'tech', 'enforcer'] as const;
const CLOSE_TICK = 5100; // 8:30, the Handover's close (sim/data/handover.site.json schedule.over)

/** FNV-1a over a string: a 32-bit seed. */
export function hash32(s: string): number {
  let h = 0x811c9dc5;
  for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 0x01000193); }
  return h >>> 0;
}
/** mulberry32: a small, fast, seeded PRNG in [0, 1). */
export function rng(seed: number): () => number {
  let a = seed >>> 0;
  return () => { a = (a + 0x6d2b79f5) >>> 0; let t = a; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; };
}
const pick = <T>(r: () => number, xs: readonly T[]): T => xs[Math.floor(r() * xs.length)]!;
const int = (r: () => number, lo: number, hi: number) => lo + Math.floor(r() * (hi - lo + 1));

/** Every order the seeds contain (drive samples left out), deduplicated without tick and agent; the targets each verb names. */
export type Vocab = { orders: Order[]; targetsByVerb: Map<string, string[]>; targets: string[] };
export function vocabulary(seeds: Seed[]): Vocab {
  const seen = new Set<string>(), orders: Order[] = [], byVerb = new Map<string, Set<string>>(), all = new Set<string>();
  for (const s of seeds) for (const o of s.script.orders) {
    if (o.do === 'drive') continue;
    const { tick: _t, op: _o, ...rest } = o; const k = JSON.stringify(rest);
    if (!seen.has(k)) { seen.add(k); orders.push(o); }
    if (o.target) { all.add(o.target); if (!byVerb.has(o.do)) byVerb.set(o.do, new Set()); byVerb.get(o.do)!.add(o.target); }
  }
  orders.sort((a, b) => JSON.stringify(a).localeCompare(JSON.stringify(b))); // input order never moves the vocabulary
  const targetsByVerb = new Map([...byVerb].map(([k, v]) => [k, [...v].sort()]));
  return { orders, targetsByVerb, targets: [...all].sort() };
}

const label = (o: Order) => `${o.op ?? 'cell'} ${o.do}${o.target ? ' ' + o.target : o.approach ? ' ' + o.approach : ''}`;
const clock = (tick: number) => { const s = Math.floor(tick / 10); return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`; };

/** Apply one mutation of `family` to `orders` (in place); returns what it did, or null when the family has nothing to act on here. */
export function applyOne(family: Family, orders: Order[], r: () => number, v: Vocab, until: number): Mutation | null {
  const idx = orders.map((o, i) => (o.do === 'drive' ? -1 : i)).filter(i => i >= 0);
  const end = until > 0 ? until : CLOSE_TICK;
  switch (family) {
    case 'shift': {
      if (!idx.length) return null;
      const i = pick(r, idx), o = orders[i]!, m = int(r, 1, 30) * 10;
      const d = r() < 0.5 && o.tick - m >= 0 ? -m : m, t = o.tick + d; // an order at 0:00 can only move later
      orders[i] = { ...o, tick: t };
      return { family, index: i, what: `${label(o)} moved ${clock(o.tick)} → ${clock(t)} (${d > 0 ? '+' : ''}${d / 10} s)` };
    }
    case 'drop': {
      if (!idx.length) return null;
      const i = pick(r, idx), o = orders[i]!; orders.splice(i, 1);
      return { family, index: i, what: `${label(o)} at ${clock(o.tick)} left out` };
    }
    case 'repeat': {
      if (!idx.length) return null;
      const looks = idx.filter(i => orders[i]!.do === 'watch' || orders[i]!.do === 'listen');
      const i = looks.length && r() < 0.5 ? pick(r, looks) : pick(r, idx), o = orders[i]!, t = o.tick + int(r, 2, 20) * 10;
      orders.push({ ...o, tick: t });
      return { family, index: i, what: `${label(o)} at ${clock(o.tick)} given again at ${clock(t)}` };
    }
    case 'reassign': {
      const has = idx.filter(i => OPS.includes(orders[i]!.op as any));
      if (!has.length) return null;
      const i = pick(r, has), o = orders[i]!, op = pick(r, OPS.filter(x => x !== o.op));
      orders[i] = { ...o, op };
      return { family, index: i, what: `${label(o)} at ${clock(o.tick)} given to the ${op}` };
    }
    case 'swap-target': {
      const has = idx.filter(i => !!orders[i]!.target);
      if (!has.length) return null;
      const i = pick(r, has), o = orders[i]!;
      const same = (v.targetsByVerb.get(o.do) ?? []).filter(t => t !== o.target), any = v.targets.filter(t => t !== o.target);
      const pool = same.length && r() < 0.7 ? same : any; if (!pool.length) return null;
      const target = pick(r, pool);
      orders[i] = { ...o, target };
      return { family, index: i, what: `${label(o)} at ${clock(o.tick)} aimed at ${target} instead` };
    }
    case 'insert': {
      if (!v.orders.length) return null;
      const base = pick(r, v.orders), t = int(r, 0, Math.floor(end / 10)) * 10, op = base.op ? pick(r, OPS) : undefined;
      const o: Order = { ...base, tick: t, ...(op ? { op } : {}) };
      orders.push(o);
      return { family, index: orders.length - 1, what: `${label(o)} inserted at ${clock(t)}` };
    }
  }
}

/** The seed's script as a mutant starts from it: its record dropped (a mutant has none). */
const bare = (s: Script): Script => { const { expect: _e, ...rest } = s; return { ...rest, orders: s.orders.map(o => ({ ...o })) }; };

/** `n` mutants of one seed: 70% one mutation, 30% two (families drawn evenly). Deterministic by seed id and mutant number. */
export function mutants(seed: Seed, n: number, v: Vocab): Item[] {
  const out: Item[] = [];
  for (let k = 0; k < n; k++) {
    const r = rng(hash32(`${seed.id}#${k}`)), script = bare(seed.script), muts: Mutation[] = [];
    const want = r() < 0.7 ? 1 : 2;
    for (let tries = 0; muts.length < want && tries < 12; tries++) {
      const m = applyOne(pick(r, FAMILIES), script.orders, r, v, script.until ?? 0);
      if (m) muts.push(m);
    }
    if (!muts.length) continue;
    out.push({ id: `${seed.id}~m${k}`, seed, mutations: muts, script });
  }
  return out;
}

/** The family a mutant is counted under in the tables: its one family, or "pair" for two. */
export const familyOf = (it: Item): string => (it.mutations.length === 0 ? 'seed' : it.mutations.length === 1 ? it.mutations[0]!.family : 'pair');
