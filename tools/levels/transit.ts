// The transit objective (brief 001), the first plug-in of the generation engine (tools/gen): Datum → Vantage → Pulse propose, the plan checker and
// Keel judge, faults go back to their owner. Behaviour is the A04 pipeline's, unchanged: same prompts, same record, same hashes.
import { z } from 'zod';
import type { Fault, Plugin, Round } from '../gen/plugin';
import { writeJson } from '../rag/config';
import { apply, seedStationOnPlot } from './apply';
import { inProcess, ownersOf, viaCli } from './checker';
import { geometry, plots, transit as lines } from './digest';
import { driftLedger } from './drift';
import { objectives } from './objectives';
import { pinPlan, readPlan } from './pins';
import { transitReplay } from './transit-replay';
import { agents, type Timetable } from './schemas';
import { isFareSeed, seedFareClaim, SEEDED_AGENT, SEEDS } from './seed';
import { applyDerived } from './timetable';
import { toolsHash } from './toolhash';

type State = { input: any; block: unknown; blockFile: string; planId: string; std: string };
type Report = { ok: boolean; checks: any[]; stops: any[]; stations: any[] };
const BUILD = ['datum', 'vantage', 'pulse'] as const;
const j = (x: unknown) => JSON.stringify(x), isLine = (p: string) => p.startsWith('line'), isRule = (r: string) => /^[A-Z]+-[A-Z]\d+$/.test(r);
const OBJECTIVE = 'Transit network (brief 001): sky rail and tram lines for the Harbour District, stops and stations so that every area is in reach, and a timetable; accepted only when the plan checker is clean (strict) and the critic passes.';

const checkerText = (R: { checks: any[] }, problems: string[]) => [...R.checks.map(c => `${c.ok ? 'PASS' : 'FAIL'} ${c.id} ${c.rule}: ${c.value} (${c.limit})${c.detail ? ' — ' + c.detail : ''}${c.source ? ` [source: ${c.source}]` : ''}`), ...problems.map(p => `PROBLEM ${p}`)].join('\n');
const stopTable = (R: { stops: any[]; stations: any[] }) => [...R.stops, ...R.stations].map(s => `${s.line}/${s.id} ${s.name} chainage ${Math.round(s.chainage)} m, to next ${s.next === undefined ? '—' : Math.round(s.next) + ' m'}`).join('\n');
const pre = (s: State, r: Round) => apply(s.input, { datum: r.outs.datum, vantage: r.outs.vantage }, r.version); // lines and stops as proposed, before the timetable (which is derived from them)

export const transit: Plugin<State> = {
  id: 'transit', defaultInput: 'contracts/world/harbour-district.plan.json',
  builders: BUILD.map(key => ({ key, owner: objectives[key].owner, objective: objectives[key], schema: agents[key] })),
  critic: { key: 'keel', owner: 'Keel', objective: objectives.keel, schema: agents.keel },
  header: { timetable: 'derived' },
  code: { label: 'tools/plan, tools/levels/apply.ts', hash: toolsHash, ledger: driftLedger, ledgerFile: 'tools/levels/drift.json', declare: 'bun tools/levels/drift.ts declare' },
  async open(path) {
    const plan = await readPlan(path);
    return { id: plan.id, version: plan.version, async pin(rdir) {
      const { block, blockFile, record } = await pinPlan(plan, rdir), std = (() => { const x = structuredClone(plan.input.standards); delete x.transit?.waived; return j(x); })();
      return { state: { input: plan.input, block, blockFile, planId: plan.id, std }, record };
    } };
  },
  skip: (s, r, key) => key === 'pulse' && pre(s, r).problems.some(isLine), // a line that cannot be built: no timetable on it
  async task(s, r, key) {
    const view = pre(s, r).plan;
    return key === 'datum' ? `Task: propose the lines.\nPlan standards (JSON): ${s.std}\nPlan geometry (JSON): ${geometry(s.input)}\nCurrent lines (JSON): ${lines(s.input)}`
      : key === 'vantage' ? `Task: place the stops and stations.\nPlan standards (JSON): ${s.std}\nPlan geometry (JSON): ${geometry(s.input)}\nCandidate lines from Datum (JSON): ${lines(view)}\nDatum's notes: ${(r.outs.datum?.notes ?? []).join(' | ')}`
      : `Task: write the timetable choices.\nCandidate lines (JSON): ${lines(view)}\nStops in order with chainage (from the plan checker; code derives every run time and journey total from these, you only state speeds, dwells, headways and the legs of each journey):\n${stopTable(await inProcess(view, true, s.block))}`;
  },
  async assess(s, r) {
    const { outs, version, n, rd, od, odir } = r, { plan, problems, timetable, faults: ttFaults } = await applyDerived(s.input, outs, version, s.block), planFile = `${rd}/plan.json`, checksFile = `${rd}/checks.json`;
    const added = problems.some(isLine) ? ['checker'] : [], skipped = [...r.skipped, ...added];
    let exit = -1, report: Report = { ok: false, checks: [], stops: [], stations: [] };
    if (!skipped.includes('checker')) {
      await writeJson(planFile, plan);
      exit = await viaCli(planFile, `${odir}/sheets/${s.planId}-v${version}.html`, checksFile, `${od}/checker.txt`, s.blockFile);
      report = await Bun.file(checksFile).json();
    }
    const checkerOk = !skipped.length && exit === 0 && report.ok && !problems.length, failed = report.checks.filter(c => !c.ok);
    if (timetable && !skipped.includes('pulse')) await writeJson(`${rd}/timetable.json`, timetable);
    const ttF: Fault[] = ttFaults.map(f => ({ owner: 'Pulse', rule: 'timetable', fault: f, fix: 'make the journeys use existing stops and real interchanges', chunks: [], synthesized: true }));
    return {
      ok: checkerOk, skipped: added,
      line: `checker ${skipped.includes('checker') ? 'SKIPPED (a line cannot be built)' : checkerOk ? 'clean' : 'FAIL ' + failed.map(c => c.id).join(',')}${problems.length ? ` problems ${problems.length}` : ''}`,
      record: { checkerExit: exit, checkerOk, failedChecks: failed.map(c => c.id), problems }, more: { timetableFaults: ttF.length },
      failures: failed.map(c => ({ rule: c.id, owners: ownersOf(c.id), fault: `${c.rule}: ${c.value} (limit ${c.limit})${c.detail ? '; ' + c.detail : ''}`, fix: 'bring the plan inside the rule' })),
      faults: [...problems.map((p): Fault => ({ owner: p.startsWith('timetable') ? 'Pulse' : p.startsWith('line') ? 'Datum' : 'Vantage', rule: 'apply', fault: p, fix: 'make the proposal reference what exists', chunks: [], synthesized: true })), ...ttF],
      criticTask(cited, lastFaults) {
        const schemas = Object.fromEntries(BUILD.map(a => [a, z.toJSONSchema(agents[a])]));
        const reportText = skipped.includes('checker') ? `Plan checker not run this round: a proposed line could not be built, so no timetable was written and the plan was not checked.\n${problems.map(p => `PROBLEM ${p}`).join('\n')}` : `Plan checker report on the candidate (strict):\n${checkerText(report, [...problems, ...ttFaults.map(f => 'timetable: ' + f)])}`;
        return `Task: judge the candidate.\nObjective under test: ${OBJECTIVE}\n\nOutput schemas (JSON): ${j(schemas)}\n\n${reportText}\n\nStops and stations as placed:\n${stopTable(report)}\n\nCandidate proposals (JSON): ${j({ lines: outs.datum, stops: outs.vantage, timetable: skipped.includes('pulse') ? null : { derivedByCode: 'run times and journey totals below are computed from the stop table; judge the choices, not the sums', ...(timetable as Timetable) } })}\n\nWhat each builder retrieved and cites. Code has already checked that every cited id was retrieved by its own builder: raise no fault for citation membership. Judge whether the claims are true to the cited text:\n${cited}${n > 1 || lastFaults.length ? `\n\nThis is a revision. The faults from last round (JSON), to recheck:\n${j(lastFaults)}` : ''}`;
      },
    };
  },
  ownersOf: rule => (isRule(rule) ? ownersOf(rule) : undefined),
  seeds: { names: SEEDS, agent: SEEDED_AGENT,
    apply: (s, r, name, out, rec) => (isFareSeed(name) ? seedFareClaim(out, rec.retrieval?.hits ?? [], name === 'fare-claim') : seedStationOnPlot(out, pre(s, r).plan, plots(s.input))) },
  replay: transitReplay,
};
