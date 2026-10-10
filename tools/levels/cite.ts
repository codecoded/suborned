// Citation checks that need no model: a builder's `sources` must be ids its OWN call retrieved. Keel's view of what each builder rests on.
import type { Verdict } from './schemas';

type Hit = { key: string; section?: string; text?: string };
export type Built = { out?: { sources?: string[] }; call?: { retrieval?: { hits?: Hit[] } } };
export type CiteFault = Verdict['faults'][number] & { synthesized: true };
export const OWNERS = { datum: 'Datum', vantage: 'Vantage', pulse: 'Pulse' } as const;

/** One synthesized 'cite' fault per builder whose sources fall outside its own call's retrieved hits (the call that produced the output, also when reused). */
export function citeFaults(built: Record<string, Built | undefined>): CiteFault[] {
  const faults: CiteFault[] = [];
  for (const [a, owner] of Object.entries(OWNERS)) {
    const b = built[a]; if (!b?.out) continue;
    const own = new Set((b.call?.retrieval?.hits ?? []).map(h => h.key)), bad = [...new Set(b.out.sources ?? [])].filter(id => !own.has(id));
    if (bad.length) faults.push({ owner, rule: 'cite', fault: `sources cite ids this call did not retrieve: ${bad.join(' ')}`, fix: 'cite only passage ids from your own retrieved passages; drop the rest', chunks: [], synthesized: true });
  }
  return faults;
}

/** Keel's task block: per builder the ids and sections retrieved, and the text of what it cites, so claims can be checked against what they rest on. */
export function citedBlock(built: Record<string, Built | undefined>, cap = 900): string {
  const done = new Set<string>(), parts: string[] = [];
  for (const [a, owner] of Object.entries(OWNERS)) {
    const b = built[a]; if (!b?.out) { parts.push(`${owner}: no proposal this round`); continue; }
    const hits = b.call?.retrieval?.hits ?? [], byKey = new Map(hits.map(h => [h.key, h]));
    parts.push(`${owner} retrieved: ${hits.map(h => `${h.key} (${h.section ?? '?'})`).join('; ') || 'nothing'}\n${owner} cites: ${(b.out.sources ?? []).join(' ') || 'nothing'}`);
    for (const id of b.out.sources ?? []) {
      const h = byKey.get(id); if (!h || done.has(id)) continue; done.add(id);
      parts.push(`[${id}] (${h.section ?? '?'}, cited by ${owner}) ${(h.text ?? '').slice(0, cap)}`);
    }
  }
  return parts.join('\n');
}
