// The skyline objective (brief 007 F), a plug-in of the generation engine (tools/gen): Datum proposes the signature
// sites, portals and the threaded sky rail, Vantage the stations (inside buildings at the concourse), the plan and
// world rules judge the candidate, Keel critiques a candidate that passed, faults go back to their owner. Missions plug
// in beside it (brief 003).
import { z } from 'zod';
import { RunRefused } from '../../gen/continue';
import type { Fault, Plugin, Round } from '../../gen/plugin';
import type { Check } from '../../plan/checks';
import type { Plan } from '../../plan/plan';
import { writeJson } from '../../rag/config';
import { geometry } from '../digest';
import { pinPlan, readPlan } from '../pins';
import { applySkyline, type Known, type Owner } from './apply';
import { candidatePlan, checkAll } from './candidate';
import { BUILD_RULE, ownersOf, reportText, stationTable } from './check';
import { pinConcepts, type ConceptIndex } from './concepts';
import { recordSkyline, skylineLedger, skylineLedgerFile } from './drift';
import { pinGrammar, sectionsOf } from './grammar';
import { skylineHash } from './hash';
import { knownFor, pinnedKit } from './known';
import { pinnedWorld } from '../pinned';
import { baselineFailures, brokenBy, failedMission, missionChecks, pinMission, type Mission } from './mission';
import { objectives } from './objectives';
import { skylineReplay } from './replay';
import { agents, NO_SKYLINE, NO_STATIONS, type SkylineOut, type StationsOut } from './schemas';
import * as view from './view';

type State = {
  input: Plan; block: unknown; planId: string; rdir: string; index: ConceptIndex; grammar: string; mission: Mission;
  known?: Known;
};
const BUILD = ['datum', 'vantage'] as const;
const j = (x: unknown) => JSON.stringify(x);
const isRule = (r: string) => /^[A-Z]+-[A-Z]?\d+[a-z]?$/.test(r);
const OBJECTIVE = 'Skyline (brief 007): the signature structures of the Harbour District and the sky rail that threads'
  + ' its buildings, with stations inside buildings at the sky concourse; accepted only when the plan checker (plan and'
  + ' world rules, strict) is clean and the critic passes.';

const knownOf = async (s: State) => (s.known ??= await knownFor(s.index, pinnedKit(s.rdir)));

/** Who broke each failed Handover rule: found by judging the candidate with Datum's edits alone (mission.ts). */
async function handoverOwners(s: State, r: Round, failed: string[]) {
  if (!failed.length) return new Map<string, Owner[]>();
  let alone;
  try {
    const only = await candidatePlan(s.input, { datum: r.outs.datum }, r.version, await knownOf(s), s.block);
    alone = missionChecks(only.plan, s.block, s.mission);
  } catch { alone = undefined; }
  return brokenBy(failed, alone);
}
/** The plan with the proposals of this round so far applied (what a later builder is shown). */
async function candidateOf(s: State, r: Round) {
  return applySkyline(s.input, { datum: r.outs.datum, vantage: r.outs.vantage }, r.version, await knownOf(s));
}

/**
 * A failed rule once, however many times it is listed (the transit rules list one line per line or interchange).
 * `broke`: who broke each failed Handover rule (the others have their owners by id: check.ts).
 */
function failures(checks: Check[], broke: Map<string, Owner[]>) {
  const by = new Map<string, Check[]>();
  for (const c of checks.filter(x => !x.ok)) by.set(c.id, [...(by.get(c.id) ?? []), c]);
  return [...by].map(([id, list]) => {
    const what = list.map(c => `${c.rule}: ${c.value} (limit ${c.limit})${c.detail ? '; ' + c.detail : ''}`);
    return {
      rule: id, owners: broke.get(id) ?? ownersOf(id),
      fault: what.join(' | ') + (list[0]!.source ? ` [source: ${list[0]!.source}]` : ''),
      fix: 'bring the plan inside the rule',
    };
  });
}

function datumTask(s: State): string {
  const p = s.input;
  return `Task: propose the skyline.\nStandards that rule it (JSON): ${view.skylineStandards(p)}\n`
    + 'Signatures now (JSON; prose over 200 characters is left out; an optional field you leave out keeps its value): '
    + `${view.signaturesView(p)}\nTower sites now (JSON): ${view.sitesView(p)}\n`
    + `Megastructures now (JSON): ${view.structuresView(p)}\nSky rail now (JSON): ${view.skyrailView(p)}\n`
    + `Environmental areas (JSON): ${view.areasView(p)}\nPlan geometry (JSON): ${geometry(p)}\n${sheetsText(s)}\n`
    + `Massing grammar, sections 4, 6 and 8 (the pinned copy):\n${sectionsOf(s.grammar)}`;
}

const sheetsText = (s: State) =>
  `Concept sheets (attached as images, in this order) and the tile ids to cite:\n${view.conceptsView(s.index)}`;

async function vantageTask(s: State, r: Round): Promise<string> {
  const p = (await candidateOf(s, r)).plan, notes = (r.outs.datum as SkylineOut | undefined)?.notes ?? [];
  return `Task: place the sky rail stations.\nStandards that rule a station (JSON): ${view.stationStandards(p)}\n`
    + `Candidate sky rail from Datum (JSON): ${view.skyrailView(p)}\n`
    + `Candidate signatures (JSON): ${view.signaturesView(p)}\n`
    + `Megastructures and their portals (JSON): ${view.structuresView(p)}\nPlan geometry (JSON): ${geometry(p)}\n`
    + `${sheetsText(s)}\nDatum's notes: ${notes.join(' | ')}`;
}

export const skyline: Plugin<State> = {
  id: 'skyline', defaultInput: 'contracts/world/harbour-district.plan.json',
  builders: BUILD.map(key => ({ key, owner: objectives[key].owner, objective: objectives[key], schema: agents[key] })),
  critic: { key: 'keel', owner: 'Keel', objective: objectives.keel, schema: agents.keel },
  header: { proposals: 'upserts' },
  code: {
    label: 'tools/plan, tools/world, the Handover overlay, tools/levels/skyline (apply, retime, check, mission)',
    hash: skylineHash, ledger: skylineLedger, ledgerFile: skylineLedgerFile,
    declare: 'bun tools/levels/skyline/drift.ts declare', moves: true, record: recordSkyline,
  },
  async open(path) {
    const plan = await readPlan(path);
    const broken = await baselineFailures(plan.input as Plan);
    if (broken.length) {
      throw new RunRefused(`the base plan already fails the Handover rules (${broken.join(', ')}): a skyline run would `
        + 'only spend its rounds on them. Fix the plan or the mission first.');
    }
    return { id: plan.id, version: plan.version, async pin(rdir) {
      const { block, record } = await pinPlan(plan, rdir);
      const concepts = await pinConcepts(rdir), grammar = await pinGrammar(rdir), mission = await pinMission(rdir);
      const state: State = {
        input: plan.input as Plan, block, planId: plan.id, rdir, index: concepts.index, grammar: grammar.text,
        mission: mission.mission,
      };
      const inputs = { inputConcepts: concepts.record, inputGrammar: grammar.record, inputMission: mission.record };
      return { state, record: { ...record, ...inputs } };
    } };
  },
  task: async (s, r, key) => (key === 'datum' ? datumTask(s) : vantageTask(s, r)),
  async assess(s, r) {
    const outs = { datum: r.outs.datum, vantage: r.outs.vantage };
    const { plan, problems } = await candidatePlan(s.input, outs, r.version, await knownOf(s), s.block);
    const checked = await checkAll(plan, s.block, s.mission, pinnedWorld(s.rdir));
    const text = problems.map(x => x.text), failed = [...new Set(checked.checks.filter(c => !c.ok).map(c => c.id))];
    const broke = await handoverOwners(s, r, failedMission(checked.checks));
    await writeJson(`${r.rd}/plan.json`, plan);
    await writeJson(`${r.rd}/checks.json`, { plan: s.planId, version: r.version, ...checked });
    const checkerOk = checked.ok && !problems.length;
    const state = checked.error ? 'FAIL (the candidate cannot be built)'
      : checked.ok ? 'clean' : `FAIL ${failed.join(',')}`;
    return {
      ok: checkerOk, line: `checker ${state}${problems.length ? ` problems ${problems.length}` : ''}`,
      record: { checkerOk, failedChecks: failed, problems: text },
      failures: failures(checked.checks, broke),
      faults: problems.map((x): Fault => ({
        owner: x.owner, rule: 'apply', fault: x.text, fix: 'make the proposal reference what exists',
        chunks: [], synthesized: true,
      })),
      criticTask(cited, lastFaults) {
        const schemas = Object.fromEntries(BUILD.map(a => [a, z.toJSONSchema(agents[a])]));
        const revision = r.n > 1 || lastFaults.length
          ? `\n\nThis is a revision. The faults from last round (JSON), to recheck:\n${j(lastFaults)}` : '';
        return `Task: judge the candidate.\nObjective under test: ${OBJECTIVE}\n\n`
          + `Output schemas (JSON): ${j(schemas)}\n\n`
          + `Plan checker report on the candidate (strict, plan and world rules):\n${reportText(checked, text)}\n\n`
          + `Sky rail stations as placed:\n${stationTable(checked)}\n\n`
          + `Candidate proposals (JSON): ${j({ skyline: r.outs.datum, stations: r.outs.vantage })}\n\n`
          + 'What each builder retrieved and cites. Code has already checked that every cited id was retrieved by'
          + ' its own builder: raise no fault for citation membership. Judge whether the claims are true to the cited'
          + ` text:\n${cited}${revision}`;
      },
    };
  },
  ownersOf: rule => (isRule(rule) || rule === BUILD_RULE ? ownersOf(rule) : undefined),
  query(_s, r, key) {
    if (key === 'datum') return [];
    const d = r.outs.datum as SkylineOut | undefined;
    const named = [
      ...(d?.signatures ?? []).map(e => `signature ${e.id}`), ...(d?.towerSites ?? []).map(e => `tower site ${e.id}`),
      ...(d?.portals ?? []).map(e => `portal ${e.structure} ${e.id}`),
    ];
    return named.slice(0, 4).map(x => `${x} plan`);
  },
  standIn: (_s, key): SkylineOut | StationsOut | undefined =>
    (key === 'datum' ? NO_SKYLINE : key === 'vantage' ? NO_STATIONS : undefined),
  replay: skylineReplay,
};
