// Who answers whom in a crew, for the builders in build order: hand-offs (a builder's optional asks for another builder) and citations.
// Hand-offs. Rule (rounds recorded `handoffMode: 'answered'`): a blocking hand-off to an owner that runs LATER in the same round is delivered in that
// owner's task, which answers it in `answers: [{ from, ask, done, note }]`; it is a fault only if that answer is missing or `done: false`.
// A blocking hand-off to an owner that ran earlier (or did not run this round) cannot be answered this round: it is a fault. Advisory ones are only recorded.
// Rounds recorded before the field fault every blocking hand-off (`answers` undefined). Only builders that ran this round count: a reused output's hand-off was delivered already.
// Citations. A builder's `sources` must be ids its OWN call retrieved; the critic is shown what each builder retrieved and the text of what it cites.
import type { Built, Fault, Member } from './plugin';

export type Handoff = { from: string; owner: string; blocking: boolean; ask: string };
export type Answer = { from: string; ask: string; done: boolean; note: string };
type Outs = Record<string, { handoffs?: Omit<Handoff, 'from'>[]; answers?: Answer[] } | undefined>;
const norm = (s: string) => s.trim().replace(/\s+/g, ' ');

/** One fault as the builder it addresses reads it back (the passage ids it cites are pinned into that builder's retrieval). */
export const feedbackLine = (f: Fault) => `${f.fault} [${f.chunks.join(' ')}] → ${f.fix}`;

export const handoffText = (hs: Handoff[]) => hs.length ? `\n\nBlocking hand-offs addressed to you this round. Answer each in \`answers\` ({ from, ask (copied exactly), done, note }); done: true only if your output now does it, otherwise it becomes a fault for you:\n${hs.map(h => `- from ${h.from}: ${h.ask}`).join('\n')}` : '';

export function crewOf(builders: Pick<Member, 'key' | 'owner'>[]) {
  const keys = builders.map(b => b.key), from = Object.fromEntries(builders.map(b => [b.key, b.owner])) as Record<string, string>;
  const order = (k: string | undefined) => keys.indexOf(k ?? ''), keyOf = (owner: string) => keys.find(k => from[k] === owner);

  const collectHandoffs = (outs: Outs, ran: string[]): Handoff[] => keys.filter(a => ran.includes(a)).flatMap(a => (outs[a]?.handoffs ?? []).map(h => ({ from: from[a]!, ...h })));
  /** Blocking hand-offs the builder `to` must answer this round: from builders that ran before it (in `ran`, in build order). */
  const pendingFor = (to: string, outs: Outs, ran: string[]) => collectHandoffs(outs, ran.filter(a => order(a) < order(to))).filter(h => h.blocking && h.owner === from[to]);
  const answered = (h: Handoff, outs: Outs, ran: string[]) => {
    const to = keyOf(h.owner)!;
    if (!ran.includes(to) || order(to) <= order(keyOf(h.from))) return false;
    return (outs[to]?.answers ?? []).some(x => x.done && norm(x.from) === h.from && norm(x.ask) === norm(h.ask));
  };
  const handoffFaults = (hs: Handoff[], answers?: { outs: Outs; ran: string[] }): Fault[] =>
    hs.filter(h => h.blocking && !(answers && answered(h, answers.outs, answers.ran))).map(h => ({ owner: h.owner, rule: 'handoff', fault: `${h.from} hands off to ${h.owner} (blocking): ${h.ask}`, fix: h.ask, chunks: [], synthesized: true }));

  /** One synthesized 'cite' fault per builder whose sources fall outside its own call's retrieved hits (the call that produced the output, also when reused). */
  const citeFaults = (built: Record<string, Built | undefined>): Fault[] => {
    const faults: Fault[] = [];
    for (const b of builders) {
      const x = built[b.key]; if (!x?.out) continue;
      const own = new Set((x.call?.retrieval?.hits ?? []).map(h => h.key)), bad = [...new Set(x.out.sources ?? [])].filter(id => !own.has(id));
      if (bad.length) faults.push({ owner: b.owner, rule: 'cite', fault: `sources cite ids this call did not retrieve: ${bad.join(' ')}`, fix: 'cite only passage ids from your own retrieved passages; drop the rest', chunks: [], synthesized: true });
    }
    return faults;
  };
  /** The critic's block: per builder the ids and sections retrieved, and the text of what it cites, so claims can be checked against what they rest on. */
  const citedBlock = (built: Record<string, Built | undefined>, cap = 900): string => {
    const done = new Set<string>(), parts: string[] = [];
    for (const b of builders) {
      const x = built[b.key]; if (!x?.out) { parts.push(`${b.owner}: no proposal this round`); continue; }
      const hits = x.call?.retrieval?.hits ?? [], byKey = new Map(hits.map(h => [h.key, h]));
      parts.push(`${b.owner} retrieved: ${hits.map(h => `${h.key} (${h.section ?? '?'})`).join('; ') || 'nothing'}\n${b.owner} cites: ${(x.out.sources ?? []).join(' ') || 'nothing'}`);
      for (const id of x.out.sources ?? []) {
        const h = byKey.get(id); if (!h || done.has(id)) continue; done.add(id);
        parts.push(`[${id}] (${h.section ?? '?'}, cited by ${b.owner}) ${(h.text ?? '').slice(0, cap)}`);
      }
    }
    return parts.join('\n');
  };
  /** The first builder that must run again: the earliest of those named (they have a fault on them); the last builder always runs again. */
  const rerunFrom = (named: Set<string>) => Math.min(...keys.map((k, i) => (named.has(k) ? i : 9)), keys.length - 1);
  return { collectHandoffs, pendingFor, handoffFaults, citeFaults, citedBlock, keyOf, rerunFrom };
}
