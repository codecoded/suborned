// Offline replay of a recorded run (brief 007 F). No Docker, no model, no out/: validates a run from runs/ alone: schemas,
// card and corpus hashes, retrieved text against the corpus, the objective's checks rerun on every round (its `replay`
// hooks), citations, hand-offs, versions, and the verdict and outcome, which are recomputed (replay-verdict.ts).
// Drift is labelled by cause, never fixed by editing the record.
import { existsSync } from 'node:fs';
import { resolve } from 'node:path';
import { CHUNKER, hash, loadCorpus } from '../rag/corpus';
import { checkContinued, checkInheritedCall } from './continue-replay';
import { crewOf } from './crew';
import { checkImages } from './images-replay';
import { currentGdd, inferGdd } from './gdd';
import { candidateVersion, runsRoot } from './paths';
import { checkKit } from './pin-kit';
import { checkLook } from './pin-look';
import type { Check, Pins, Plugin } from './plugin';
import { checkCallRecord } from './replay-call';
import { codeLine } from './replay-code';
import { roundVerdict, type Verdict } from './replay-verdict';
import { loopOf } from './rules';
import { Tokens } from './tokens';

/** `notCompleted`: the run never finished (ERROR, INCOMPLETE, no run.json): nothing was replayed, and that is not a failure. */
export type ReplayResult = { ok: boolean; lines: string[]; notCompleted?: string };
const UNFINISHED = ['ERROR', 'INCOMPLETE'];

function reporter(allowDrift: boolean) {
  const lines: string[] = [];
  let bad = 0;
  const fail = (m: string) => { bad++; lines.push('FAIL  ' + m); };
  const ok = (m: string) => { lines.push('ok    ' + m); };
  const warn = (m: string) => { lines.push('warn  ' + m); };
  const drift = (m: string) => { if (allowDrift) warn(m + ' (drift allowed)'); else fail(m); };
  return { lines, ok, warn, fail, drift, failures: () => bad, countFailure: () => { bad++; } };
}

const unfinished = (lines: string[], why: string): ReplayResult => {
  lines.push(`not completed: ${why}`);
  return { ok: true, lines, notCompleted: why };
};

export async function replayRun(
  plugins: Record<string, Plugin>, planId: string, runId: string, allowDrift = false,
): Promise<ReplayResult> {
  const rep = reporter(allowDrift), { lines, ok, warn, fail, drift } = rep;
  const dir = resolve(runsRoot(), planId, runId);
  const read = async (f: string) => {
    try { return JSON.parse(await Bun.file(`${dir}/${f}`).text()); } catch (e) { fail(`${f}: ${String(e).slice(0, 120)}`); return undefined; }
  };
  if (existsSync(dir) && !existsSync(`${dir}/run.json`)) return unfinished(lines, 'no run.json (the run never wrote its first round)');
  const run = await read('run.json');
  if (!run) return { ok: false, lines };
  if (UNFINISHED.includes(run.outcome)) {
    const error = run.error ? `: ${String(run.error).slice(0, 120)}` : '';
    return unfinished(lines, `outcome ${run.outcome}${error}; its completed rounds can be continued, they are not replayed`);
  }
  const p = Object.hasOwn(plugins, run.objective) ? plugins[run.objective] : undefined;
  if (!p) {
    fail(`run.json objective ${JSON.stringify(run.objective)} is not a known objective (${Object.keys(plugins).join(', ')})`);
    return { ok: false, lines };
  }
  const c: Check = { dir, run, allow: allowDrift, lines, ok, warn, fail, drift, read, rerunDrift: false };
  const members = [...p.builders, p.critic], crew = crewOf(p.builders);
  // the inputs are the text the run recorded, never the files now on disk: promoting or editing them must not break replay
  let pins: Pins = await p.replay.pins.raw(c);
  // loop: the rule the run was made under (rules.ts); a run without the field is loop 1
  const chunker = Number(run.chunker ?? 1), loop = loopOf(run);
  // the kit digest the run pinned, so a later adoption in kit.json does not change its corpus
  pins = { ...pins, overlay: { ...pins.overlay, ...await checkKit(c, chunker) } };
  await checkLook(c); // the civic look the world rules read, pinned like the block (a run before the pin has none)
  // only the engine that has the loop rule pins a kit: a pinned kit with no loop field is a record with a field taken out
  if (run.inputKit && run.loop === undefined) fail('run.json pins a kit digest but has no loop field');
  // a run without the field (the A04 runs) is loop 1: nothing is added to its replay
  if (loop >= 2) lines.push(`ok    loop rule ${loop}: Keel judged only candidates that passed the checks`);
  // the GDD the run was recorded against: its own record, else the GDD paths in its retrieved passages (runs before the field)
  const now = currentGdd(), hitSources: string[] = [];
  for (const r of run.rounds ?? []) {
    for (const m of members) {
      const call = await Bun.file(`${dir}/round-${r.round}/${m.key}.call.json`).json().catch(() => undefined);
      for (const h of call?.retrieval?.hits ?? []) hitSources.push(h.source);
    }
  }
  const rec = run.gdd ?? inferGdd(hitSources), older = !!rec && rec.version !== now.version;
  const label = `recorded against GDD ${rec?.version}`;
  if (!rec) lines.push('warn  GDD version unknown (no record, no retrieved sources)');
  else {
    const inferred = run.gdd ? '' : ' (inferred from retrieved sources)';
    lines.push(`ok    GDD ${rec.version}${inferred}${older ? `; now ${now.version}: GDD drift is labelled "${label}"` : ''}`);
  }
  const gddDrift = (m: string) => { if (older) warn(`${m} (${label})`); else drift(m); };
  // chunker: how the corpus is cut into passages; a run without the field was cut by v1 (the whole § by paragraph).
  // A different chunker explains changed GDD passages and the corpus hash, nothing else.
  const chunkerLabel = chunker !== CHUNKER ? `recorded with chunker v${chunker}, now v${CHUNKER}` : '';
  if (chunkerLabel) lines.push(`ok    chunker v${chunker} (${chunkerLabel}): GDD passage and corpus hash differences are labelled drift`);
  const corpusDrift = (m: string) => {
    if (chunkerLabel) warn(`${m} (${chunkerLabel}${older ? `; ${label}` : ''})`); else gddDrift(m);
  };
  const corpus = await loadCorpus(pins.overlay ?? {}), byKey = new Map(corpus.map(x => [x.key, x]));
  const corpusHash = hash(JSON.stringify(corpus));
  if (run.corpusHash === corpusHash) ok(`corpus hash ${corpusHash.slice(0, 12)}`);
  else corpusDrift(`corpus hash changed: recorded ${String(run.corpusHash).slice(0, 12)}, now ${corpusHash.slice(0, 12)}`);
  pins = { ...pins, ...await p.replay.pins.input(c, pins) };
  const code = p.code, tools = await code.hash(), superseded = p.replay.superseded(run);
  // toolsAt: the line to resolve once the reruns say what changed; reran: rounds whose checker rerun was compared
  let toolsAt = -1, reran = 0, rerunDiffers = false;
  const added = new Set<string>(), changed = new Set<string>(), failing = new Set<string>();
  const hashNow = tools.slice(0, 12), hashThen = String(run.toolsHash).slice(0, 12);
  c.rerunDrift = !!code.moves && run.toolsHash !== tools;
  if (run.toolsHash === tools) ok(`checker code hash ${hashNow}`);
  else if (superseded) {
    warn(`checker code changed since the run (superseded run, pre-pinning: recorded ${hashThen}, now ${hashNow}; the checker cannot be rerun on it either way)`);
  } else toolsAt = lines.push('') - 1;
  pins = { ...pins, ...await p.replay.pins.block(c, pins) };
  const runNo = Number(runId.slice(1)), rounds: any[] = run.rounds ?? [];
  if (!rounds.length || rounds.length > 3) fail(`rounds: ${rounds.length} (expected 1-3)`); else ok(`${rounds.length} round(s)`);
  const inherited = await checkContinued(c, p, rounds);
  const seen = new Set<string>(), verdicts: Verdict[] = [];

  for (const r of rounds) {
    const n = `round-${r.round}`, version = candidateVersion(p.replay.inputVersion(run), runNo, r.round);
    const skipped: string[] = r.skipped ?? [];
    for (const m of members) {
      const a = m.key;
      if (skipped.includes(a)) { ok(`${n}/${a}: skipped (${p.replay.skipNote})`); continue; }
      if (m === p.critic && r.criticRan === false && loop >= 2) { // the candidate failed the checks, Keel was not called
        if (await Bun.file(`${dir}/${n}/${a}.json`).exists()) fail(`${n}/${a}.json exists, but the round records that Keel was not called`);
        else ok(`${n}/${a}: not called (the checks failed)`);
        continue;
      }
      const out = await read(`${n}/${a}.json`), parsed = out === undefined ? undefined : p.replay.schema(run, a).safeParse(out);
      if (parsed && !parsed.success) fail(`${n}/${a}.json fails its schema: ${parsed.error.message.slice(0, 160)}`);
      else if (parsed) ok(`${n}/${a}.json schema`);
      const call = await read(`${n}/${a}.call.json`);
      if (!call) continue;
      await checkImages(c, n, a, call);
      if (inherited && Bun.deepEquals(call, { ...inherited.calls[a], reused: true })) { // the parent's call, carried over
        await checkInheritedCall(c, n, a, call, inherited);
        continue;
      }
      await checkCallRecord({ c, p, n, a, byKey, seen, gddDrift, corpusDrift }, call);
    }
    const rr = await p.replay.candidate(c, r, version, pins);
    if (rr) {
      reran++;
      if (rr.differs) rerunDiffers = true;
      for (const id of rr.added) added.add(id);
      for (const id of rr.changed ?? []) changed.add(id);
      for (const id of rr.failing ?? []) failing.add(id);
    }
    if ('citeFaults' in r) { // rounds recorded with the citation check: recompute it from the recorded outputs and each call's own hits
      const built: Record<string, any> = {};
      for (const b of p.builders) {
        if (!skipped.includes(b.key)) built[b.key] = { out: await read(`${n}/${b.key}.json`), call: await read(`${n}/${b.key}.call.json`) };
      }
      const faults: { rule: string; owner: string; fault: string }[] = (await read(`${n}/faults.json`)) ?? [];
      const want = crew.citeFaults(built).map(f => `${f.owner}|${f.fault}`).sort();
      const got = faults.filter(f => f.rule === 'cite').map(f => `${f.owner}|${f.fault}`).sort();
      if (JSON.stringify(want) === JSON.stringify(got) && r.citeFaults === want.length) ok(`${n}: citation faults match (${want.length})`);
      else fail(`${n}: citation faults differ from a recomputation (${got.length} recorded, ${want.length} expected)`);
    }
    if ('handoffs' in r) { // rounds recorded with hand-offs: recompute from the outputs of the builders that ran
      const o: Record<string, any> = {};
      for (const b of p.builders) if (!skipped.includes(b.key)) o[b.key] = await read(`${n}/${b.key}.json`);
      const want = crew.collectHandoffs(o, r.ran ?? []);
      const faults: { rule: string; owner: string; fault: string }[] = (await read(`${n}/faults.json`)) ?? [];
      const got = faults.filter(f => f.rule === 'handoff').map(f => `${f.owner}|${f.fault}`).sort();
      const answers = run.handoffMode === 'answered' ? { outs: o, ran: r.ran ?? [] } : undefined;
      const wantFaults = crew.handoffFaults(want, answers).map(f => `${f.owner}|${f.fault}`).sort();
      const same = JSON.stringify(want) === JSON.stringify(r.handoffs) && JSON.stringify(wantFaults) === JSON.stringify(got);
      if (same && r.handoffFaults === got.length) ok(`${n}: hand-offs match (${want.length}, ${got.length} blocking)`);
      else fail(`${n}: hand-offs differ from a recomputation`);
    }
    const keel = r.criticRan === false && loop >= 2 ? undefined : await read(`${n}/${p.critic.key}.json`);
    if (keel && r.keelVerdict !== keel.verdict) fail(`${n}: run.json keelVerdict ${r.keelVerdict} vs keel.json ${keel.verdict}`);
    verdicts.push(await roundVerdict({ c, p, r, n, loop, superseded, rerun: rr }));
    if (run.seeded) { // the label is the engine's; recomputing what a seed changed is the objective's
      let call: any;
      if (r.round === 1) {
        const sa = p.seeds?.agent[run.seeded];
        call = sa ? await read(`${n}/${sa}.call.json`) : undefined;
        if (call?.seeded === run.seeded && r.seeded) ok(`${n}: seeded ${run.seeded} is labelled`);
        else fail(`${n}: run is seeded but round 1 is not labelled`);
      }
      await p.replay.seeded?.(c, r, call);
    }
  }

  if (toolsAt >= 0) {
    const drifted = codeLine({ run, tools, code, allowDrift, reran, rerunDiffers, added, changed, failing });
    lines[toolsAt] = drifted.line;
    if (drifted.failed) rep.countFailure();
  }
  // the outcome is recomputed from the last round's recomputed verdict, never read back from run.json
  const expected = verdicts.at(-1) === 'pass' ? 'ACCEPTED' : 'REJECTED';
  if (run.outcome === expected) ok(`outcome ${run.outcome}${run.outcome === 'REJECTED' ? ' (recorded)' : ''}`);
  else fail(`outcome recorded ${run.outcome}, recomputed ${expected}`);
  if (verdicts.slice(0, -1).includes('pass')) fail('a round passed but the run went on');
  // optional: runs made before it have none
  const withTokens = (run.calls ?? []).filter((x: { tokens?: unknown }) => x.tokens !== undefined);
  if (withTokens.length) {
    const malformed = withTokens.filter((x: { tokens: unknown }) => !Tokens.safeParse(x.tokens).success);
    if (malformed.length) fail(`run.json calls: ${malformed.length} malformed token counts`);
    else ok(`tokens recorded for ${withTokens.length} of ${run.calls.length} calls`);
  }
  const usd = Math.round((run.calls ?? []).reduce((s: number, x: any) => s + (x.costUsd ?? 0), 0) * 1e4) / 1e4;
  if (usd === run.cost?.usd) ok(`cost $${usd}`); else fail(`cost recorded ${run.cost?.usd}, calls sum ${usd}`);
  return { ok: !rep.failures(), lines };
}
