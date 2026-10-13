// Replay of a continued run (brief 007 F): inherited.json is the run's own pinned input, so this needs the new run's folder only. The parent's
// folder is looked at when it happens to be on disk, and then it must be the run that was inherited from.
import { resolve } from 'node:path';
import { root } from '../rag/config';
import { hash } from '../rag/corpus';
import { CONTINUABLE, INHERITED, rerunOf, type Continues, type Inherited } from './continue';
import { runsRoot } from './paths';
import type { Check, Plugin } from './plugin';
import { sha256 } from './sha';

/** Checks a continued run's link to its parent; returns what it inherited (undefined for a run that continues nothing). */
export async function checkContinued(c: Check, p: Plugin, rounds: any[]): Promise<Inherited | undefined> {
  const cont: Continues | undefined = c.run.continues;
  const text = await Bun.file(`${c.dir}/${INHERITED}`).text().catch(() => undefined);
  if (!cont) {
    if (text !== undefined) c.fail(`${INHERITED} is there, but run.json does not say this run continues another`);
    return undefined;
  }
  if (text === undefined) return void c.fail(`${INHERITED} is missing (run.json says this run continues ${cont.run})`);
  if (sha256(text) !== cont.sha256) return void c.fail(`${INHERITED} does not match the recorded sha256`);
  const inh: Inherited = JSON.parse(text);
  c.ok(`continues ${cont.plan}/${cont.run} round ${cont.round}, ${INHERITED} ${cont.sha256.slice(0, 12)}`);

  const { parent } = inh;
  const same = (['plan', 'run', 'outcome', 'round', 'version', 'runSha256', 'gdd', 'chunker'] as const).every(k => parent[k] === cont[k]);
  if (same && CONTINUABLE.includes(parent.outcome)) c.ok(`inherited parent matches run.json (a ${parent.outcome} run)`);
  else c.fail('inherited parent differs from run.json, or was not a run that can be continued');

  for (const [key, out] of Object.entries(inh.proposals)) {
    const parsed = p.replay.schema(c.run, key).safeParse(out);
    if (!parsed.success) c.fail(`${INHERITED}: proposal ${key} fails its schema: ${parsed.error.message.slice(0, 120)}`);
  }

  // round 1 reuses exactly the builders before the earliest one with a fault, as they were
  const first = rounds[0], skipped: string[] = first?.skipped ?? [];
  const rerun = rerunOf(inh, p.builders);
  const want = p.builders.filter((b, i) => i < rerun && !skipped.includes(b.key)).map(b => b.key);
  if (JSON.stringify(first?.reused ?? []) !== JSON.stringify(want)) c.fail(`round-1: reused ${JSON.stringify(first?.reused)}, the inherited faults give ${JSON.stringify(want)}`);
  else c.ok(`round-1: reuses ${want.length ? want.join(', ') : 'nothing'}, reruns from ${p.builders[rerun]!.key}, as the inherited faults say`);
  for (const key of want) {
    const out = await c.read(`round-1/${key}.json`), call = await c.read(`round-1/${key}.call.json`);
    const sameOut = Bun.deepEquals(out, inh.proposals[key]), sameCall = Bun.deepEquals(call, { ...inh.calls[key], reused: true });
    sameOut && sameCall ? c.ok(`round-1/${key}: the inherited proposal and its call, unchanged`) : c.fail(`round-1/${key}: differs from the inherited proposal${sameOut ? "'s call record" : ''}`);
  }

  const parentRun = await Bun.file(resolve(runsRoot(), parent.plan, parent.run, 'run.json')).text().catch(() => undefined);
  if (parentRun === undefined) c.ok(`parent ${parent.run} is not on this machine; the inherited state is the run's own pin`);
  else sha256(parentRun) === parent.runSha256 ? c.ok(`parent ${parent.run} on disk is the run inherited from`) : c.fail(`parent ${parent.run} on disk differs from the run inherited from`);
  return inh;
}

/** A reused call carried over from the parent: its card and passages are the parent's, compared with nothing of today's. A card changed since is labelled, not failed. */
export async function checkInheritedCall(c: Check, n: string, a: string, call: any, inh: Inherited): Promise<void> {
  const card = await Bun.file(resolve(root, call.card.source)).text().catch(() => undefined);
  const from = `inherited from ${inh.parent.run}${inh.parent.gdd ? `, recorded against GDD ${inh.parent.gdd}` : ''}`;
  if (card === undefined) c.fail(`${n}/${a}: card ${call.card.source} is missing`);
  else if (hash(card) !== call.card.sourceHash) c.warn(`${n}/${a}: card ${call.card.source} changed since the call was made (${from})`);
  c.ok(`${n}/${a}: call ${from}; its ${call.retrieval.hits.length} retrieved passages are pinned in ${INHERITED}`);
}
