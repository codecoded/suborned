// Brief 060 §3 (Suture): findings → ranked issues. One issue per check key (the same fault in many plays), ranked by severity × frequency
// (the share of a lane's plays it touched, with a 1% floor so a rare crash still ranks). Each keeps a few exemplar plays to replay: unmutated
// seeds first, then the fewest mutations, then the earliest tick.
import type { Finding, Item, Severity } from './types';

export const WEIGHT: Record<Severity, number> = { S1: 8, S2: 4, S3: 2, S4: 1, info: 0 };
export type Exemplar = { lane: string; item: string; seed: string; seedKind: string; /** a session's: sky or automation */ seedBy?: string; mutations: string[]; finding: Finding; script?: string; /** the bot lane's shot of it */ shot?: string };
export type Issue = {
  key: string; check: string; severity: Severity; title: string; owner: string;
  plays: Record<string, number>; share: Record<string, number>; score: number; exemplars: Exemplar[];
  firstSeen?: string; change?: 'new' | 'known' | 'regressed';
};

export class IssueBook {
  private m = new Map<string, Issue>();
  private seenIn = new Map<string, Set<string>>(); // key → lane:item, for the play counts
  constructor(private keep = 3) {}
  add(lane: string, it: Item, fs: Finding[]) {
    for (const f of fs) {
      let is = this.m.get(f.key);
      if (!is) this.m.set(f.key, is = { key: f.key, check: f.check, severity: f.severity, title: f.title, owner: f.owner, plays: {}, share: {}, score: 0, exemplars: [] });
      if (WEIGHT[f.severity] > WEIGHT[is.severity]) { is.severity = f.severity; is.title = f.title; }
      const id = `${lane}:${it.id}`, s = this.seenIn.get(f.key) ?? new Set(); this.seenIn.set(f.key, s);
      if (!s.has(id)) { s.add(id); is.plays[lane] = (is.plays[lane] ?? 0) + 1; }
      const ex: Exemplar = { lane, item: it.id, seed: it.seed.id, seedKind: it.seed.kind, ...(it.seed.by ? { seedBy: it.seed.by } : {}), mutations: it.mutations.map(m => `${m.family}: ${m.what}`), finding: f, ...(f.script ? { script: f.script } : {}), ...(f.shot ? { shot: f.shot } : {}) };
      if (is.exemplars.some(e => e.lane === lane && e.item === it.id)) continue;
      is.exemplars.push(ex);
      is.exemplars.sort((a, b) => a.mutations.length - b.mutations.length || (a.seedKind === 'session' ? 0 : 1) - (b.seedKind === 'session' ? 0 : 1) || (a.finding.tick ?? 0) - (b.finding.tick ?? 0));
      if (is.exemplars.length > this.keep) is.exemplars.length = this.keep;
    }
  }
  ranked(playsPerLane: Record<string, number>): Issue[] {
    const out = [...this.m.values()];
    for (const is of out) {
      for (const [ln, n] of Object.entries(is.plays)) is.share[ln] = n / Math.max(1, playsPerLane[ln] ?? 1);
      is.score = WEIGHT[is.severity] * Math.max(0.01, ...Object.values(is.share));
    }
    return out.sort((a, b) => b.score - a.score || a.key.localeCompare(b.key));
  }
}
