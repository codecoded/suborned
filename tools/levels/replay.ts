// usage: bun run levels:replay r001 [plan-id] [--allow-drift]
// Offline: no Docker, no model, no out/. Validates a recorded run from runs/ alone: schemas, card and corpus hashes, retrieved
// text against the corpus, the checker rerun on every round plan, versions and the outcome.
import { createHash } from 'node:crypto';
import { resolve } from 'node:path';
import { root } from '../rag/config';
import { CHUNKER, hash, loadCorpus } from '../rag/corpus';
import { apply } from './apply';
import { citeFaults } from './cite';
import { collectHandoffs, handoffFaults } from './handoff';
import { inProcess } from './checker';
import { currentGdd, inferGdd } from './gdd';
import { toolsHash } from './toolhash';
import { candidateVersion, runsRoot } from './paths';
import { schemaFor, type Agent, agents } from './schemas';
import { applyDerived } from './timetable';

const KINDS = Object.keys(agents) as Agent[];

export async function replay(planId: string, runId: string, allowDrift = false): Promise<{ ok: boolean; lines: string[] }> {
  const lines: string[] = []; let bad = 0;
  const fail = (m: string) => { bad++; lines.push('FAIL  ' + m); }, ok = (m: string) => lines.push('ok    ' + m), warn = (m: string) => lines.push('warn  ' + m);
  const drift = (m: string) => (allowDrift ? warn(m + ' (drift allowed)') : fail(m));
  const dir = resolve(runsRoot(), planId, runId), read = async (f: string) => { try { return JSON.parse(await Bun.file(`${dir}/${f}`).text()); } catch (e) { fail(`${f}: ${String(e).slice(0, 120)}`); return undefined; } };
  const run = await read('run.json');
  if (!run) return { ok: false, lines };
  // the input plan is the text the run recorded, never the file now on disk: promoting or editing the plan must not break replay
  const rawText = await Bun.file(`${dir}/input.plan.raw.json`).text().catch(() => undefined);
  if (rawText === undefined) fail('input.plan.raw.json is missing'); else hash(rawText) === run.inputPlan?.rawHash ? ok('raw input plan hash') : fail('input.plan.raw.json does not match the recorded raw hash');
  // the GDD the run was recorded against: its own record, else the GDD paths in its retrieved passages (runs before the field)
  const now = currentGdd(), hitSources: string[] = [];
  for (const r of run.rounds ?? []) for (const a of KINDS) for (const h of (await Bun.file(`${dir}/round-${r.round}/${a}.call.json`).json().catch(() => undefined))?.retrieval?.hits ?? []) hitSources.push(h.source);
  const rec = run.gdd ?? inferGdd(hitSources), older = !!rec && rec.version !== now.version, label = `recorded against GDD ${rec?.version}`;
  lines.push(rec ? `ok    GDD ${rec.version}${run.gdd ? '' : ' (inferred from retrieved sources)'}${older ? `; now ${now.version}: GDD drift is labelled "${label}"` : ''}` : 'warn  GDD version unknown (no record, no retrieved sources)');
  const gddDrift = (m: string) => (older ? warn(`${m} (${label})`) : drift(m));
  // chunker: how the corpus is cut into passages; a run without the field was cut by v1 (the whole § by paragraph). A different chunker explains changed GDD passages and the corpus hash, nothing else.
  const chunker = Number(run.chunker ?? 1), chunkerLabel = chunker !== CHUNKER ? `recorded with chunker v${chunker}, now v${CHUNKER}` : '';
  if (chunkerLabel) lines.push(`ok    chunker v${chunker} (${chunkerLabel}): GDD passage and corpus hash differences are labelled drift`);
  const corpusDrift = (m: string) => (chunkerLabel ? warn(`${m} (${chunkerLabel}${older ? `; ${label}` : ''})`) : gddDrift(m));
  const corpus = await loadCorpus(rawText === undefined ? {} : { [run.inputPlan?.source ?? '']: rawText }), byKey = new Map(corpus.map(c => [c.key, c])), corpusHash = hash(JSON.stringify(corpus));
  run.corpusHash === corpusHash ? ok(`corpus hash ${corpusHash.slice(0, 12)}`) : corpusDrift(`corpus hash changed: recorded ${String(run.corpusHash).slice(0, 12)}, now ${corpusHash.slice(0, 12)}`);
  const derived = run.timetable === 'derived'; // runs before the field (r001-r005) carry Pulse's own run times; they replay as recorded
  lines.push(derived ? 'ok    timetable derived by code' : 'ok    timetable as proposed (recorded before derivation)');
  const input = await read('input.plan.json');
  if (input) hash(JSON.stringify(input)) === run.inputPlan?.hash ? ok('input plan hash') : fail('input.plan.json does not match the recorded input hash');
  if (input && rawText !== undefined) { try { Bun.deepEquals(JSON.parse(rawText), input) ? ok('input.plan.json = the raw input plan') : fail('input.plan.json differs from input.plan.raw.json'); } catch { fail('input.plan.raw.json is not JSON'); } }
  const tools = await toolsHash();
  const supersededRun = !run.inputBlock; // pre-pinning: its checker cannot be rerun, and its code changed with the Oct 9 block work (tools/plan civic.ts, checks.ts C-0), so the hash adds nothing beyond "superseded"
  const toolsMsg = `CHECKER CODE changed since the run (tools/plan, tools/levels/apply.ts): recorded ${String(run.toolsHash).slice(0, 12)}, now ${tools.slice(0, 12)}; the record itself may be intact`;
  let toolsAt = -1; const added = new Set<string>(); let rerunDiffers = false; // toolsAt: line to resolve once the reruns say whether only rules were added
  run.toolsHash === tools ? ok(`checker code hash ${tools.slice(0, 12)}`) : supersededRun ? warn(`checker code changed since the run (superseded run, pre-pinning: recorded ${String(run.toolsHash).slice(0, 12)}, now ${tools.slice(0, 12)}; the checker cannot be rerun on it either way)`) : (toolsAt = lines.push('') - 1);
  // the block is pinned per run; a run with no record predates pinning and cannot be checked against today's block
  let block: unknown, superseded = false;
  const blockBytes = run.inputBlock ? await Bun.file(`${dir}/input.block.json`).bytes().catch(() => undefined) : undefined;
  if (!run.inputBlock) { superseded = true; const m = 'superseded: recorded against the Oct 5 block, replays at commit 3dbb9ca'; if (allowDrift) warn(m); else fail(m); }
  else if (!blockBytes) fail('input.block.json is missing');
  else if (createHash('sha256').update(blockBytes).digest('hex') !== run.inputBlock.sha256) fail('input.block.json does not match the recorded sha256');
  else { ok(`input block ${run.inputBlock.sha256.slice(0, 12)}`); block = JSON.parse(new TextDecoder().decode(blockBytes)); }
  const runNo = Number(runId.slice(1)), rounds: any[] = run.rounds ?? [];
  if (!rounds.length || rounds.length > 3) fail(`rounds: ${rounds.length} (expected 1-3)`); else ok(`${rounds.length} round(s)`);
  const seen = new Set<string>();

  for (const r of rounds) {
    const n = `round-${r.round}`, version = candidateVersion(run.inputPlan.version, runNo, r.round);
    const skipped: string[] = r.skipped ?? [];
    for (const a of KINDS) {
      if (skipped.includes(a)) { ok(`${n}/${a}: skipped (a line could not be built)`); continue; }
      const out = await read(`${n}/${a}.json`), parsed = out === undefined ? undefined : schemaFor(a, derived).safeParse(out);
      if (parsed && !parsed.success) fail(`${n}/${a}.json fails its schema: ${parsed.error.message.slice(0, 160)}`); else if (parsed) ok(`${n}/${a}.json schema`);
      const call = await read(`${n}/${a}.call.json`);
      if (!call) continue;
      const card = await Bun.file(resolve(root, call.card.source)).text().catch(() => undefined);
      if (card === undefined) fail(`${n}/${a}: card ${call.card.source} is missing`); else if (hash(card) !== call.card.sourceHash) gddDrift(`${n}/${a}: card ${call.card.source} changed since the run`); else ok(`${n}/${a}: card hash`);
      const id = `${call.id}`; if (seen.has(id) && !call.reused) fail(`${n}/${a}: duplicate call ${id}`); seen.add(id);
      let missing = 0, changed = 0, gm = 0, gc = 0; // gm/gc: GDD passages only (the only ones a GDD version change explains)
      for (const h of call.retrieval.hits as any[]) {
        const c = byKey.get(h.key);
        const g = String(h.source).startsWith('GDD/');
        if (!c) g ? gm++ : missing++; else if (c.text !== h.text || c.sourceHash !== h.sourceHash || c.section !== h.section) g ? gc++ : changed++;
      }
      if (gm || gc) corpusDrift(`${n}/${a}: retrieved GDD text no longer matches the corpus (${gm} missing, ${gc} changed of ${call.retrieval.hits.length})`);
      if (missing || changed) drift(`${n}/${a}: retrieved text no longer matches the corpus (${missing} missing, ${changed} changed of ${call.retrieval.hits.length})`);
      else if (!gm && !gc) ok(`${n}/${a}: ${call.retrieval.hits.length} retrieved passages match the corpus`);
    }
    const plan = skipped.includes('checker') ? undefined : await read(`${n}/plan.json`), rec = skipped.includes('checker') ? undefined : await read(`${n}/checks.json`);
    if (plan && rec) {
      if (!block && !superseded) warn(`${n}: checker rerun skipped (no valid pinned block)`);
      plan.version === version ? ok(`${n}: version ${version}`) : fail(`${n}: plan version ${plan.version}, expected ${version}`);
      if (r.version !== version) fail(`${n}: run.json version ${r.version}`);
      const outs = { datum: await read(`${n}/datum.json`), vantage: await read(`${n}/vantage.json`), pulse: await read(`${n}/pulse.json`) };
      const valid = !skipped.length && KINDS.filter(k => k !== 'keel').every(k => schemaFor(k, derived).safeParse(outs[k as 'datum']).success);
      if (input && valid && !derived) Bun.deepEquals(JSON.parse(JSON.stringify(apply(input, outs, version).plan)), plan) ? ok(`${n}: plan.json = input plan + the three recorded proposals`) : fail(`${n}: plan.json is not the input plan plus the recorded proposals`);
      if (input && valid && derived) { // the timetable is recomputed from the proposals and the checker's stop table, never read back
        if (!block) warn(`${n}: derived timetable not recomputed (no valid pinned block)`);
        else {
          const d = await applyDerived(input, outs, version, block);
          Bun.deepEquals(JSON.parse(JSON.stringify(d.plan)), plan) ? ok(`${n}: plan.json = input plan + the recorded proposals, timetable recomputed`) : fail(`${n}: plan.json differs from the input plan plus the recomputed timetable`);
          const tt = await read(`${n}/timetable.json`);
          Bun.deepEquals(JSON.parse(JSON.stringify(d.timetable)), tt) ? ok(`${n}: timetable.json matches the recomputation`) : fail(`${n}: timetable.json differs from the recomputed timetable`);
          const got = ((await read(`${n}/faults.json`)) ?? []).filter((f: any) => f.rule === 'timetable').map((f: any) => f.fault).sort();
          JSON.stringify([...d.faults].sort()) === JSON.stringify(got) && r.timetableFaults === got.length ? ok(`${n}: timetable faults match (${got.length})`) : fail(`${n}: timetable faults differ from a recomputation (${got.length} recorded, ${d.faults.length} expected)`);
        }
      }
      const R = block ? await inProcess(plan, rec.strict !== false, block) : undefined;
      if (R) {
        // a rule added since the run is labelled drift; a changed result on a rule the run recorded stays FAIL
        const had = new Set<string>(rec.checks.map((c: any) => c.id)), kept = R.checks.filter(c => had.has(c.id)), fresh = R.checks.filter(c => !had.has(c.id));
        for (const c of fresh) added.add(c.id);
        const same = JSON.stringify(kept) === JSON.stringify(rec.checks);
        if (!same) rerunDiffers = true;
        same ? ok(`${n}: checker rerun matches checks.json (${kept.filter(c => c.ok).length}/${kept.length})`) : fail(`${n}: checker rerun differs from checks.json`);
        if (fresh.length) warn(`${n}: checker rules added since the run: ${[...new Set(fresh.map(c => c.id))].join(', ')}${fresh.some(c => !c.ok) ? `; this plan would FAIL ${[...new Set(fresh.filter(c => !c.ok).map(c => c.id))].join(', ')}` : ''}`);
        const clean = kept.every(c => c.ok);
        if (r.checkerOk !== (clean && !(r.problems ?? []).length)) fail(`${n}: run.json checkerOk=${r.checkerOk} but rerun clean=${clean}`);
      }
    }
    if ('citeFaults' in r) { // rounds recorded with the citation check: recompute it from the recorded outputs and each call's own hits
      const built: Record<string, any> = {};
      for (const a of ['datum', 'vantage', 'pulse']) if (!skipped.includes(a)) built[a] = { out: await read(`${n}/${a}.json`), call: await read(`${n}/${a}.call.json`) };
      const want = citeFaults(built).map(f => `${f.owner}|${f.fault}`).sort(), got = ((await read(`${n}/faults.json`)) ?? []).filter((f: any) => f.rule === 'cite').map((f: any) => `${f.owner}|${f.fault}`).sort();
      JSON.stringify(want) === JSON.stringify(got) && r.citeFaults === want.length ? ok(`${n}: citation faults match (${want.length})`) : fail(`${n}: citation faults differ from a recomputation (${got.length} recorded, ${want.length} expected)`);
    }
    if ('handoffs' in r) { // rounds recorded with hand-offs: recompute from the outputs of the builders that ran
      const o: Record<string, any> = {}; for (const a of ['datum', 'vantage', 'pulse']) if (!skipped.includes(a)) o[a] = await read(`${n}/${a}.json`);
      const want = collectHandoffs(o, r.ran ?? []), got = ((await read(`${n}/faults.json`)) ?? []).filter((f: any) => f.rule === 'handoff').map((f: any) => `${f.owner}|${f.fault}`).sort();
      JSON.stringify(want) === JSON.stringify(r.handoffs) && JSON.stringify(handoffFaults(want, run.handoffMode === 'answered' ? { outs: o, ran: r.ran ?? [] } : undefined).map(f => `${f.owner}|${f.fault}`).sort()) === JSON.stringify(got) && r.handoffFaults === got.length ? ok(`${n}: hand-offs match (${want.length}, ${got.length} blocking)`) : fail(`${n}: hand-offs differ from a recomputation`);
    }
    const keel = await read(`${n}/keel.json`);
    if (keel && r.keelVerdict !== keel.verdict) fail(`${n}: run.json keelVerdict ${r.keelVerdict} vs keel.json ${keel.verdict}`);
    if (run.seeded && r.round === 1) {
      const v = await read(`${n}/vantage.call.json`);
      v?.seeded === run.seeded && r.seeded ? ok(`${n}: seeded ${run.seeded} is labelled`) : fail(`${n}: run is seeded but round 1 is not labelled`);
    }
  }

  if (toolsAt >= 0) lines[toolsAt] = !rerunDiffers && added.size ? `warn  ${toolsMsg.split(';')[0]} (labelled drift: checker rules added since the run: ${[...added].join(', ')}; every rule the run recorded reruns identically)` : (allowDrift ? 'warn  ' + toolsMsg + ' (drift allowed)' : (bad++, 'FAIL  ' + toolsMsg));
  const last = rounds.at(-1), expected = last && last.checkerOk && last.keelVerdict === 'pass' && last.verdict === 'pass' && !last.faults ? 'ACCEPTED' : 'REJECTED';
  run.outcome === expected ? ok(`outcome ${run.outcome}${run.outcome === 'REJECTED' ? ' (recorded)' : ''}`) : fail(`outcome recorded ${run.outcome}, recomputed ${expected}`);
  if (rounds.slice(0, -1).some((r: any) => r.verdict === 'pass')) fail('a round passed but the run went on');
  const usd = Math.round((run.calls ?? []).reduce((s: number, c: any) => s + (c.costUsd ?? 0), 0) * 1e4) / 1e4;
  usd === run.cost?.usd ? ok(`cost $${usd}`) : fail(`cost recorded ${run.cost?.usd}, calls sum ${usd}`);
  return { ok: !bad, lines };
}

if (import.meta.main) {
  const args = process.argv.slice(2).filter(a => !a.startsWith('--')), runId = args[0], planId = args[1] ?? 'harbour-district';
  if (!runId || !/^r\d{3}$/.test(runId)) { console.error('usage: bun run levels:replay r001 [plan-id] [--allow-drift]'); process.exit(2); }
  const { ok, lines } = await replay(planId, runId, process.argv.includes('--allow-drift'));
  console.log(lines.join('\n') + `\n${ok ? 'REPLAY OK' : 'REPLAY FAILED'} ${planId}/${runId}${ok && lines.includes('ok    outcome REJECTED (recorded)') ? ': outcome REJECTED (recorded)' : ''}`);
  process.exit(ok ? 0 : 1);
}
