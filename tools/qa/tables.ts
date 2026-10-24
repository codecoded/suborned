// Brief 060 §1.4 (Suture): what works. Win and price tables per seed and per mutation family, the refusal codes by frequency, and the
// T2 and T9 proxies. Built from one small row per play, so a pass never keeps its plays.
import type { Item, Play } from './types';
import { familyOf } from './mutate';

export const PURSE = 100000; // sim/data/handover.site.json purse
export type Row = {
  lane: string; seed: string; seedKind: string; routeKind: string; family: string; mutant: boolean;
  outcome: string; fail: string; tick: number; spent: number; trail: number; alert: number; gunfire: number;
  refusals: string[]; takedown: 'none' | 'unseen' | 'seen'; error?: boolean;
};

/** A takedown is "seen" when a witness line lands between its start and its end (takedown.starts .. takedown.out / takedown.dead). */
export function takedownSeen(p: Play): Row['takedown'] {
  const s = p.log.find(l => l.code === 'takedown.starts'); if (!s) return 'none';
  const e = p.log.find(l => l.tick >= s.tick && (l.code === 'takedown.out' || l.code === 'takedown.dead' || l.code === 'takedown.fails'));
  const end = e ? e.tick : p.tick;
  return p.log.some(l => l.code.startsWith('witness.') && l.tick >= s.tick && l.tick <= end) ? 'seen' : 'unseen';
}

export function rowOf(lane: string, p: Play, it: Item): Row {
  const base = { lane, seed: it.seed.id, seedKind: it.seed.kind, routeKind: it.seed.routeKind ?? '', family: familyOf(it), mutant: it.mutations.length > 0 };
  if (p.error) return { ...base, outcome: 'error', fail: '', tick: 0, spent: 0, trail: 0, alert: 0, gunfire: 0, refusals: [], takedown: 'none', error: true };
  return {
    ...base, outcome: p.outcome, fail: p.fail, tick: p.tick, spent: PURSE - p.cash, trail: p.trail, alert: p.alert, gunfire: p.gunfire,
    refusals: p.orders.filter(o => !o.ok && o.order.do !== 'drive').map(o => `${o.order.do}:${o.code}`), takedown: takedownSeen(p),
  };
}

export type Agg = { plays: number; wins: number; winRate: number; fails: Record<string, number>; other: Record<string, number>; win: { s: number; spent: number; trail: number; alert: number } | null };
const mean = (xs: number[]) => (xs.length ? xs.reduce((a, b) => a + b, 0) / xs.length : 0);
export function agg(rows: Row[]): Agg {
  const wins = rows.filter(r => r.outcome === 'success'), fails: Record<string, number> = {}, other: Record<string, number> = {};
  for (const r of rows) {
    if (r.outcome === 'failure') fails[r.fail] = (fails[r.fail] ?? 0) + 1;
    else if (r.outcome !== 'success') other[r.outcome] = (other[r.outcome] ?? 0) + 1;
  }
  return {
    plays: rows.length, wins: wins.length, winRate: rows.length ? wins.length / rows.length : 0, fails, other,
    win: wins.length ? { s: mean(wins.map(r => r.tick / 10)), spent: mean(wins.map(r => r.spent)), trail: mean(wins.map(r => r.trail)), alert: mean(wins.map(r => r.alert)) } : null,
  };
}

const group = <K extends string>(rows: Row[], key: (r: Row) => K) => { const m = new Map<K, Row[]>(); for (const r of rows) { const k = key(r); if (!m.has(k)) m.set(k, []); m.get(k)!.push(r); } return m; };

export type Tables = {
  bySeed: { lane: string; seed: string; seedKind: string; routeKind: string; seedOutcome: string; mutants: Agg }[];
  byFamily: { lane: string; seedKind: string; family: string; agg: Agg; flipped: number; topRefusals: [string, number][] }[];
  refusals: { lane: string; code: string; n: number }[];
  t2: { lane: string; loud: Agg; quiet: Agg; loudFasterAndCheaper: boolean | null; note: string }[];
  t9: { lane: string; unseen: Agg & { winAtAlert1: number }; seen: Agg & { failOrLoud: number }; pass: boolean | null; note: string }[];
};

export function tables(rows: Row[]): Tables {
  const out: Tables = { bySeed: [], byFamily: [], refusals: [], t2: [], t9: [] };
  for (const [ln, lr] of group(rows, r => r.lane)) {
    for (const [seed, sr] of group(lr, r => r.seed)) {
      const own = sr.find(r => !r.mutant);
      out.bySeed.push({ lane: ln, seed, seedKind: sr[0]!.seedKind, routeKind: sr[0]!.routeKind, seedOutcome: own ? (own.outcome === 'failure' ? own.fail : own.outcome) : '?', mutants: agg(sr.filter(r => r.mutant)) });
    }
    const seedOut = new Map(lr.filter(r => !r.mutant).map(r => [r.seed, r.outcome + ':' + r.fail]));
    for (const [k, fr] of group(lr.filter(r => r.mutant), r => `${r.seedKind}|${r.family}` as string)) {
      const [seedKind, family] = k.split('|') as [string, string];
      const ref = new Map<string, number>(); for (const r of fr) for (const c of r.refusals) ref.set(c, (ref.get(c) ?? 0) + 1);
      out.byFamily.push({ lane: ln, seedKind, family, agg: agg(fr), flipped: fr.filter(r => seedOut.get(r.seed) !== r.outcome + ':' + r.fail).length, topRefusals: [...ref].sort((a, b) => b[1] - a[1]).slice(0, 5) });
    }
    const ref = new Map<string, number>(); for (const r of lr) for (const c of r.refusals) { const code = c.split(':')[1]!; ref.set(code, (ref.get(code) ?? 0) + 1); }
    for (const [code, n] of [...ref].sort((a, b) => b[1] - a[1])) out.refusals.push({ lane: ln, code, n });
    // T2 proxy: the loud routes and the quiet ones, each with their mutants: does loud win more, faster and cheaper?
    const routes = lr.filter(r => r.seedKind === 'route');
    if (!routes.length && !lr.some(r => r.takedown !== 'none')) continue; // a lane of sessions only: no route kinds to compare
    const loud = agg(routes.filter(r => r.routeKind === 'loud')), quiet = agg(routes.filter(r => r.routeKind === 'quiet'));
    let lfc: boolean | null = null;
    if (loud.win && quiet.win) lfc = loud.winRate >= quiet.winRate && loud.win.s <= quiet.win.s && loud.win.spent <= quiet.win.spent && loud.win.trail <= quiet.win.trail;
    out.t2.push({ lane: ln, loud, quiet, loudFasterAndCheaper: lfc, note: lfc === null ? 'not enough wins on one side' : lfc ? 'loud dominates: it wins as often, faster and no dearer (T2 risk)' : 'loud does not dominate quiet on every price' });
    // T9 proxy: every play with a takedown, by whether the act itself was witnessed
    const un = routes.concat(lr.filter(r => r.seedKind === 'session')).filter(r => r.takedown === 'unseen'), se = lr.filter(r => r.takedown === 'seen');
    const ua = agg(un), sa = agg(se);
    const winAtAlert1 = un.length ? un.filter(r => r.outcome === 'success' && r.alert <= 1).length / un.length : 0;
    const failOrLoud = se.length ? se.filter(r => r.outcome === 'failure' || r.outcome === 'withdrawn' || r.alert >= 2 || r.gunfire > 0).length / se.length : 0;
    out.t9.push({ lane: ln, unseen: { ...ua, winAtAlert1 }, seen: { ...sa, failOrLoud }, pass: se.length ? failOrLoud >= 0.95 : null, note: `seen takedowns end in a fail or a loud run in ${(failOrLoud * 100).toFixed(0)}% (pass ≥ 95% [S])` });
  }
  return out;
}
