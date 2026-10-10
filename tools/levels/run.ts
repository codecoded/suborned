// One transit run: Datum → Vantage → Pulse propose, the plan checker and Keel judge, faults go back to their owner (≤ 3 rounds).
import { createHash } from 'node:crypto';
import { mkdir } from 'node:fs/promises';
import { resolve } from 'node:path';
import { z } from 'zod';
import { runAgent, type Call } from '../rag/agent';
import { CHUNKER, hash, loadCorpus } from '../rag/corpus';
import type { retrieve } from '../rag/retrieval';
import { root, writeJson } from '../rag/config';
import { citedBlock, citeFaults } from './cite';
import { collectHandoffs, handoffFaults, handoffText, pendingFor } from './handoff';
import { apply, seedStationOnPlot, type Proposals } from './apply';
import { isFareSeed, seedFareClaim, SEEDED_AGENT, type Seed } from './seed';
import { applyDerived } from './timetable';
import { inProcess, ownersOf, viaCli } from './checker';
import { geometry, plots, transit } from './digest';
import { objectives } from './objectives';
import { agents, type Agent, type Pulse, type Timetable, type Verdict } from './schemas';
import { currentGdd } from './gdd';
import { toolsHash } from './toolhash';
import { candidateVersion, nextRun, outRoot, pad, runsRoot } from './paths';

export type Opts = { plan: string; seed?: Seed; rounds?: number; retriever: typeof retrieve; retrieval?: string };
type Fault = Verdict['faults'][number] & { synthesized?: boolean };
const BUILD: Agent[] = ['datum', 'vantage', 'pulse'], OWNER_AGENT = { Datum: 'datum', Vantage: 'vantage', Pulse: 'pulse' } as const;
const j = (x: unknown) => JSON.stringify(x), isLine = (p: string) => p.startsWith('line'), isRule = (r: string) => /^[A-Z]+-[A-Z]\d+$/.test(r);
const OBJECTIVE = 'Transit network (brief 001): sky rail and tram lines for the Harbour District, stops and stations so that every area is in reach, and a timetable; accepted only when the plan checker is clean (strict) and the critic passes.';

const checkerText = (R: { checks: any[] }, problems: string[]) => [...R.checks.map(c => `${c.ok ? 'PASS' : 'FAIL'} ${c.id} ${c.rule}: ${c.value} (${c.limit})${c.detail ? ' — ' + c.detail : ''}${c.source ? ` [source: ${c.source}]` : ''}`), ...problems.map(p => `PROBLEM ${p}`)].join('\n');
const stopTable = (R: { stops: any[]; stations: any[] }) => [...R.stops, ...R.stations].map(s => `${s.line}/${s.id} ${s.name} chainage ${Math.round(s.chainage)} m, to next ${s.next === undefined ? '—' : Math.round(s.next) + ' m'}`).join('\n');

export async function runLevels(o: Opts) {
  const raw = await Bun.file(resolve(root, o.plan)).text(), input = JSON.parse(raw), planId: string = input.id, run = nextRun(planId), runId = `r${pad(run)}`;
  const rdir = resolve(runsRoot(), planId, runId), odir = resolve(outRoot(), planId, runId), max = Number.isFinite(o.rounds) ? Math.min(3, Math.max(1, Math.trunc(o.rounds!))) : 3, tools = await toolsHash();
  await mkdir(rdir, { recursive: true }); await mkdir(odir, { recursive: true });
  await Bun.write(`${rdir}/input.plan.raw.json`, raw); await writeJson(`${rdir}/input.plan.json`, input);
  // pin the civic block the input plan names: exact bytes, so every check in this run (and its replay) sees the same block
  const blockSrc: string = input.civic.block, blockBytes = new Uint8Array(await Bun.file(resolve(root, blockSrc)).arrayBuffer()), blockFile = `${rdir}/input.block.json`;
  await Bun.write(blockFile, blockBytes);
  const inputBlock = { source: blockSrc, sha256: createHash('sha256').update(blockBytes).digest('hex') }, block = JSON.parse(new TextDecoder().decode(blockBytes));
  let corpusHash = '', keys = new Set<string>();
  const calls: Call[] = [], rounds: any[] = [], outs: Omit<Proposals, 'pulse'> & { pulse?: Pulse } = {}, saved: Record<string, any> = {};
  const feedback: Record<string, string[]> = { datum: [], vantage: [], pulse: [], keel: [] }, previous: Record<string, unknown> = {};
  const std = (() => { const s = structuredClone(input.standards); delete s.transit?.waived; return j(s); })();
  const label = o.seed ? `[SEEDED ${o.seed}] ` : '';
  const summary = (outcome: string, extra: object = {}) => ({
    run: runId, plan: planId, date: new Date().toISOString(), objective: 'transit', timetable: 'derived', handoffMode: 'answered', outcome, seeded: o.seed ?? false,
    inputPlan: { version: input.version, hash: hash(JSON.stringify(input)), rawHash: hash(raw), source: o.plan }, inputBlock, gdd: currentGdd(), toolsHash: tools, corpusHash, chunker: CHUNKER, embeddingModel: 'BAAI/bge-small-en-v1.5', retrieval: o.retrieval ?? 'hybrid (SurrealDB)',
    cards: Object.fromEntries(calls.map(c => [c.role, { source: c.card.source, sourceHash: c.card.sourceHash }])), rounds,
    cost: { usd: Math.round(calls.reduce((s, c) => s + (c.costUsd ?? 0), 0) * 1e4) / 1e4, calls: calls.length },
    calls: calls.map(c => ({ id: c.id, objective: c.objective, role: c.role, model: c.model, attempt: c.attempt, status: c.status, costUsd: c.costUsd, seconds: c.seconds })), ...extra,
  });

  try {
    const corpus = await loadCorpus(); keys = new Set(corpus.map(c => c.key)); corpusHash = hash(JSON.stringify(corpus));
    let rerun = 0, lastFaults: Fault[] = [], outcome = 'REJECTED', finalVersion = '';
    for (let round = 1; round <= max; round++) {
      const version = candidateVersion(input.version, run, round), rd = `${rdir}/round-${round}`, od = `${odir}/round-${round}`;
      await mkdir(rd, { recursive: true }); await mkdir(od, { recursive: true });
      const ran: string[] = [], reused: string[] = [], skipped: string[] = []; let seeded = false;
      const pre = () => apply(input, { datum: outs.datum, vantage: outs.vantage }, version); // lines and stops as proposed, before the timetable (which is derived from them)
      const run1 = async (a: Agent, task: string) => {
        const obj = objectives[a], fb = feedback[a], first = calls.length;
        const text = `${task}${a === 'datum' || a === 'keel' ? '' : handoffText(pendingFor(a, outs, ran))}${previous[a] ? `\n\nYour previous proposal (JSON):\n${j(previous[a])}` : ''}${fb.length ? `\n\nCritic faults on it, to be fixed (passages they cite are pinned above):\n${fb.map(f => '- ' + f).join('\n')}` : ''}`;
        const out = await runAgent(obj, text, agents[a] as z.ZodType<any>, calls, odir, fb, o.retriever);
        const c = calls.at(-1)!, rec = { id: c.id, objective: c.objective, role: c.role, model: c.model, modelsUsed: c.modelsUsed, tools: c.tools, cliVersion: c.cliVersion, costUsd: c.costUsd, seconds: c.seconds, attempts: calls.length - first, card: { source: c.card.source, sourceHash: c.card.sourceHash }, retrieval: c.retrieval, pinning: c.pinning };
        return { out, rec };
      };
      for (const [i, a] of BUILD.entries()) {
        if (a === 'pulse' && pre().problems.some(isLine)) { skipped.push('pulse'); continue; } // a line that cannot be built: no timetable on it
        if (i >= rerun) {
          const view = pre().plan;
          const task = a === 'datum' ? `Task: propose the lines.\nPlan standards (JSON): ${std}\nPlan geometry (JSON): ${geometry(input)}\nCurrent lines (JSON): ${transit(input)}`
            : a === 'vantage' ? `Task: place the stops and stations.\nPlan standards (JSON): ${std}\nPlan geometry (JSON): ${geometry(input)}\nCandidate lines from Datum (JSON): ${transit(view)}\nDatum's notes: ${(outs.datum?.notes ?? []).join(' | ')}`
            : `Task: write the timetable choices.\nCandidate lines (JSON): ${transit(view)}\nStops in order with chainage (from the plan checker; code derives every run time and journey total from these, you only state speeds, dwells, headways and the legs of each journey):\n${stopTable(await inProcess(view, true, block))}`;
          let { out, rec } = await run1(a, task) as { out: any; rec: any };
          if (o.seed && round === 1 && a === SEEDED_AGENT[o.seed]) {
            const s = isFareSeed(o.seed) ? seedFareClaim(out, rec.retrieval?.hits ?? [], o.seed === 'fare-claim') : seedStationOnPlot(out, view, plots(input));
            console.log(`${label}${s.note}`); rec = { ...rec, seeded: o.seed, seedNote: s.note, original: out }; out = s.out; seeded = true;
          }
          (outs as any)[a] = out; saved[a] = rec; previous[a] = out; ran.push(a);
        } else reused.push(a);
        await writeJson(`${rd}/${a}.json`, (outs as any)[a]); await writeJson(`${rd}/${a}.call.json`, { ...saved[a], ...(reused.includes(a) ? { reused: true } : {}) });
        feedback[a] = [];
      }
      const { plan, problems, timetable, faults: ttFaults } = await applyDerived(input, outs, version, block), planFile = `${rd}/plan.json`, checksFile = `${rd}/checks.json`;
      if (problems.some(isLine)) skipped.push('checker');
      let exit = -1, report: { ok: boolean; checks: any[]; stops: any[]; stations: any[] } = { ok: false, checks: [], stops: [], stations: [] };
      if (!skipped.includes('checker')) {
        await writeJson(planFile, plan);
        exit = await viaCli(planFile, `${odir}/sheets/${planId}-v${version}.html`, checksFile, `${od}/checker.txt`, blockFile);
        report = await Bun.file(checksFile).json();
      }
      const checkerOk = !skipped.length && exit === 0 && report.ok && !problems.length;
      if (timetable && !skipped.includes('pulse')) await writeJson(`${rd}/timetable.json`, timetable);
      console.log(`${label}round ${round} ${version}: checker ${skipped.includes('checker') ? 'SKIPPED (a line cannot be built)' : checkerOk ? 'clean' : 'FAIL ' + report.checks.filter(c => !c.ok).map(c => c.id).join(',')}${problems.length ? ` problems ${problems.length}` : ''}`);

      const schemas = Object.fromEntries(BUILD.map(a => [a, z.toJSONSchema(agents[a])]));
      const reportText = skipped.includes('checker') ? `Plan checker not run this round: a proposed line could not be built, so no timetable was written and the plan was not checked.\n${problems.map(p => `PROBLEM ${p}`).join('\n')}` : `Plan checker report on the candidate (strict):\n${checkerText(report, [...problems, ...ttFaults.map(f => 'timetable: ' + f)])}`;
      const built = Object.fromEntries(BUILD.map(a => [a, skipped.includes(a) ? undefined : { out: (outs as any)[a], call: saved[a] }]));
      const keelTask = `Task: judge the candidate.\nObjective under test: ${OBJECTIVE}\n\nOutput schemas (JSON): ${j(schemas)}\n\n${reportText}\n\nStops and stations as placed:\n${stopTable(report)}\n\nCandidate proposals (JSON): ${j({ lines: outs.datum, stops: outs.vantage, timetable: skipped.includes('pulse') ? null : { derivedByCode: 'run times and journey totals below are computed from the stop table; judge the choices, not the sums', ...(timetable as Timetable) } })}\n\nWhat each builder retrieved and cites. Code has already checked that every cited id was retrieved by its own builder: raise no fault for citation membership. Judge whether the claims are true to the cited text:\n${citedBlock(built)}${round > 1 ? `\n\nThis is a revision. The faults from last round (JSON), to recheck:\n${j(lastFaults)}` : ''}`;
      const k = await run1('keel', keelTask), verdict = k.out as Verdict;
      await writeJson(`${rd}/keel.json`, verdict); await writeJson(`${rd}/keel.call.json`, k.rec);
      const faults: Fault[] = verdict.faults.map(f => ({ ...f, owner: f.owner in OWNER_AGENT ? f.owner : isRule(f.rule) ? ownersOf(f.rule)[0]! : f.owner, // Keel's named owner wins; ownerOf only when it is missing/invalid
         chunks: f.chunks.filter(c => keys.has(c)) }));
      const uncited = faults.filter(f => !f.chunks.length).length;
      for (const c of report.checks.filter(c => !c.ok)) if (!faults.some(f => f.rule === c.id)) faults.push(...ownersOf(c.id).map(owner => ({ owner, rule: c.id, fault: `${c.rule}: ${c.value} (limit ${c.limit})${c.detail ? '; ' + c.detail : ''}`, fix: 'bring the plan inside the rule', chunks: [], synthesized: true as const })));
      for (const p of problems) faults.push({ owner: p.startsWith('timetable') ? 'Pulse' : p.startsWith('line') ? 'Datum' : 'Vantage', rule: 'apply', fault: p, fix: 'make the proposal reference what exists', chunks: [], synthesized: true });
      const ttF: Fault[] = ttFaults.map(f => ({ owner: 'Pulse', rule: 'timetable', fault: f, fix: 'make the journeys use existing stops and real interchanges', chunks: [], synthesized: true })); faults.push(...ttF);
      const cites = citeFaults(built); faults.push(...cites);
      const hand = collectHandoffs(outs, ran), handFaults = handoffFaults(hand, { outs, ran }); faults.push(...handFaults);
      const pass = checkerOk && verdict.verdict === 'pass' && !faults.length;
      rounds.push({ round, version, seeded, ran, reused, skipped, checkerExit: exit, checkerOk, failedChecks: report.checks.filter(c => !c.ok).map(c => c.id), problems, verdict: pass ? 'pass' : 'fail', keelVerdict: verdict.verdict, citeFaults: cites.length, timetableFaults: ttF.length, handoffs: hand, handoffFaults: handFaults.length, faults: faults.length, uncitedFaults: uncited, faultOwners: [...new Set(faults.map(f => f.owner))] });
      await writeJson(`${rd}/faults.json`, faults);
      console.log(`${label}round ${round}: Keel ${verdict.verdict}, ${faults.length} fault(s)${uncited ? ` (${uncited} without a valid cited passage)` : ''} → ${pass ? 'ACCEPTED' : faults.length ? [...new Set(faults.map(f => f.owner))].join(', ') : 'rejected'}`);
      await writeJson(`${rdir}/run.json`, summary('INCOMPLETE'));
      finalVersion = version;
      if (pass) { outcome = 'ACCEPTED'; break; }
      lastFaults = faults;
      for (const f of faults) feedback[OWNER_AGENT[f.owner]].push(`${f.fault} [${f.chunks.join(' ')}] → ${f.fix}`);
      rerun = Math.min(...BUILD.map((a, i) => (feedback[a].length ? i : 9)), 2);
    }
    await writeJson(`${rdir}/run.json`, summary(outcome, { finalVersion }));
    console.log(`${label}${runId} ${outcome} (${finalVersion}) → ${rdir}`);
    return { runId, outcome, rdir, odir, finalVersion };
  } catch (e) {
    await writeJson(`${rdir}/run.json`, summary('ERROR', { error: String(e) }));
    throw e;
  }
}
