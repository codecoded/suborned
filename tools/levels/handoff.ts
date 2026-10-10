// Hand-offs: a builder's optional asks for another builder. Builders run Datum → Vantage → Pulse in one round.
// Rule (rounds recorded `handoffMode: 'answered'`): a blocking hand-off to an owner that runs LATER in the same round is delivered in that
// owner's task, which answers it in `answers: [{ from, ask, done, note }]`; it is a fault only if that answer is missing or `done: false`.
// A blocking hand-off to an owner that ran earlier (or did not run this round) cannot be answered this round: it is a fault. Advisory ones are only recorded.
// Rounds recorded before the field fault every blocking hand-off (`answers` undefined). Only builders that ran this round count: a reused output's hand-off was delivered already.
import type { Verdict } from './schemas';

export type Handoff = { from: string; owner: 'Datum' | 'Vantage' | 'Pulse'; blocking: boolean; ask: string };
export type Answer = { from: string; ask: string; done: boolean; note: string };
export type HandoffFault = Verdict['faults'][number] & { synthesized: true };
const FROM = { datum: 'Datum', vantage: 'Vantage', pulse: 'Pulse' } as const;
type Outs = Partial<Record<keyof typeof FROM, { handoffs?: Omit<Handoff, 'from'>[]; answers?: Answer[] } | undefined>>;
const ORDER = ['datum', 'vantage', 'pulse'];

export const collectHandoffs = (outs: Outs, ran: string[]): Handoff[] =>
  (Object.keys(FROM) as (keyof typeof FROM)[]).filter(a => ran.includes(a)).flatMap(a => (outs[a]?.handoffs ?? []).map(h => ({ from: FROM[a], ...h })));

/** Blocking hand-offs the builder `to` must answer this round: from builders that ran before it (in `ran`, in build order). */
export const pendingFor = (to: 'datum' | 'vantage' | 'pulse', outs: Outs, ran: string[]): Handoff[] =>
  collectHandoffs(outs, ran.filter(a => ORDER.indexOf(a) < ORDER.indexOf(to))).filter(h => h.blocking && h.owner === FROM[to]);

const norm = (s: string) => s.trim().replace(/\s+/g, ' ');
const answered = (h: Handoff, outs: Outs, ran: string[]) => {
  const to = (Object.keys(FROM) as (keyof typeof FROM)[]).find(a => FROM[a] === h.owner)!;
  if (!ran.includes(to) || ORDER.indexOf(to) <= ORDER.indexOf(h.from.toLowerCase())) return false;
  return (outs[to]?.answers ?? []).some(x => x.done && norm(x.from) === h.from && norm(x.ask) === norm(h.ask));
};

export const handoffFaults = (hs: Handoff[], answers?: { outs: Outs; ran: string[] }): HandoffFault[] =>
  hs.filter(h => h.blocking && !(answers && answered(h, answers.outs, answers.ran))).map(h => ({ owner: h.owner, rule: 'handoff', fault: `${h.from} hands off to ${h.owner} (blocking): ${h.ask}`, fix: h.ask, chunks: [], synthesized: true }));

export const handoffText = (hs: Handoff[]) => hs.length ? `\n\nBlocking hand-offs addressed to you this round. Answer each in \`answers\` ({ from, ask (copied exactly), done, note }); done: true only if your output now does it, otherwise it becomes a fault for you:\n${hs.map(h => `- from ${h.from}: ${h.ask}`).join('\n')}` : '';
