// Hand-offs: a builder's optional asks for another builder. Blocking ones become synthesized faults for their owner (rule 'handoff'),
// routed like any fault; advisory ones are only recorded. Only builders that ran this round count: a reused output's hand-off was delivered already.
import type { Verdict } from './schemas';

export type Handoff = { from: string; owner: 'Datum' | 'Vantage' | 'Pulse'; blocking: boolean; ask: string };
export type HandoffFault = Verdict['faults'][number] & { synthesized: true };
const FROM = { datum: 'Datum', vantage: 'Vantage', pulse: 'Pulse' } as const;

export const collectHandoffs = (outs: Partial<Record<keyof typeof FROM, { handoffs?: Omit<Handoff, 'from'>[] } | undefined>>, ran: string[]): Handoff[] =>
  (Object.keys(FROM) as (keyof typeof FROM)[]).filter(a => ran.includes(a)).flatMap(a => (outs[a]?.handoffs ?? []).map(h => ({ from: FROM[a], ...h })));

export const handoffFaults = (hs: Handoff[]): HandoffFault[] =>
  hs.filter(h => h.blocking).map(h => ({ owner: h.owner, rule: 'handoff', fault: `${h.from} hands off to ${h.owner} (blocking): ${h.ask}`, fix: h.ask, chunks: [], synthesized: true }));
