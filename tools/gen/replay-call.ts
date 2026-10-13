// Replay: one recorded call against what is on disk now: its card, the brief and the schema it answered, and the
// passages it was given against the corpus the run's pins rebuild.
import { resolve } from 'node:path';
import type { Chunk } from '../rag/corpus';
import { hash } from '../rag/corpus';
import { root } from '../rag/config';
import type { Check, Plugin } from './plugin';
import { z } from 'zod';

type Hit = { key: string; text: string; sourceHash: string; section: string; source: string };
/** A call as its `<builder>.call.json` records it (the fields replay reads). */
export type RecordedCall = {
  id: string | number; reused?: boolean; card: { source: string; sourceHash: string };
  brief?: { source: string; sourceHash: string }; schemaSha256?: string; retrieval: { hits: Hit[] };
};
export type CallEnv = {
  c: Check; p: Plugin; n: string; a: string; byKey: Map<string, Chunk>; seen: Set<string>;
  /** A difference a GDD version change explains is labelled; otherwise it is `c.drift`. */
  gddDrift(m: string): void; corpusDrift(m: string): void;
};

const text = (file: string) => Bun.file(resolve(root, file)).text().catch(() => undefined);

/**
 * The brief and the schema a call answered, by hash. A call recorded before they were hashed has none. A change is
 * labelled, not failed: editing a brief or a schema after a run must not break replay, but it must not be silent.
 */
async function checkAnswered(e: CallEnv, call: RecordedCall): Promise<void> {
  const { c, n, a } = e, here = `${n}/${a}`;
  if (call.brief) {
    const now = await text(call.brief.source);
    if (now === undefined) c.warn(`${here}: brief ${call.brief.source} is gone`);
    else if (hash(now) !== call.brief.sourceHash) {
      c.warn(`${here}: brief ${call.brief.source} edited since the call (the record's hash is what it answered)`);
    }
    else c.ok(`${here}: brief hash`);
  }
  if (call.schemaSha256) {
    const schema = e.p.replay.schema(c.run, a), now = hash(JSON.stringify(z.toJSONSchema(schema)));
    if (now !== call.schemaSha256) {
      c.warn(`${here}: the output schema changed since the call (the record's hash is what it answered)`);
    }
    else c.ok(`${here}: schema hash`);
  }
}

export async function checkCallRecord(e: CallEnv, call: RecordedCall): Promise<void> {
  const { c, n, a } = e;
  const card = await text(call.card.source);
  if (card === undefined) c.fail(`${n}/${a}: card ${call.card.source} is missing`);
  else if (hash(card) !== call.card.sourceHash) e.gddDrift(`${n}/${a}: card ${call.card.source} changed since the run`);
  else c.ok(`${n}/${a}: card hash`);
  await checkAnswered(e, call);
  const id = `${call.id}`;
  if (e.seen.has(id) && !call.reused) c.fail(`${n}/${a}: duplicate call ${id}`);
  e.seen.add(id);
  // gm/gc: GDD passages only (the only ones a GDD version change explains)
  let missing = 0, changed = 0, gm = 0, gc = 0;
  const hits: Hit[] = call.retrieval.hits;
  for (const h of hits) {
    const x = e.byKey.get(h.key), g = String(h.source).startsWith('GDD/');
    if (!x) { if (g) gm++; else missing++; }
    else if (x.text !== h.text || x.sourceHash !== h.sourceHash || x.section !== h.section) {
      if (g) gc++; else changed++;
    }
  }
  const of = `of ${hits.length}`, here = `${n}/${a}: retrieved`;
  if (gm || gc) e.corpusDrift(`${here} GDD text no longer matches the corpus (${gm} missing, ${gc} changed ${of})`);
  if (missing || changed) {
    c.drift(`${here} text no longer matches the corpus (${missing} missing, ${changed} changed ${of})`);
  } else if (!gm && !gc) c.ok(`${n}/${a}: ${hits.length} retrieved passages match the corpus`);
}
