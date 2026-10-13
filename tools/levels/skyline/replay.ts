// Replay hooks of the skyline objective: what the engine's replay (tools/gen/replay.ts) cannot know. Rebuilds each
// round's candidate from the recorded proposals, the pinned concept index and the pinned kit digest; recomputes the
// apply faults; reruns the plan and world rules on it; and checks that the calls were shown the sheets the index names.
import type { Check, Pins, Replay, Rerun } from '../../gen/plugin';
import type { Plan } from '../../plan/plan';
import { checkBlock, checkInputPlan, checkRawPlan } from '../pins';
import { compareChecks, type RoundRecord } from '../rerun';
import type { Problem } from './apply';
import { candidatePlan, checkAll } from './candidate';
import type { Checked } from './check';
import { checkConcepts, type ConceptIndex } from './concepts';
import { checkGrammar } from './grammar';
import { knownFor, pinnedKit } from './known';
import { checkMission, type Mission } from './mission';
import { agents, type Agent } from './schemas';

const SHOWN: Agent[] = ['datum', 'vantage', 'keel'];
type Shown = { path: string; sha256: string };
type Fault = { owner: string; rule: string; fault: string };
type Skyline = RoundRecord & { version: string; round: number; criticRan?: boolean };

const pair = (i: Shown) => `${i.path}|${i.sha256}`;

/**
 * Every call of the round was shown the sheets of the pinned index, in its order (a call carried over from a parent run
 * only warns).
 */
async function checkShown(c: Check, n: string, r: Skyline, index: ConceptIndex) {
  const want = index.sheets.map(pair);
  for (const key of SHOWN) {
    if (key === 'keel' && r.criticRan === false) continue;
    const call: { images?: Shown[]; reused?: boolean } | undefined = await Bun.file(`${c.dir}/${n}/${key}.call.json`)
      .json().catch(() => undefined);
    if (!call) continue;
    const got = (call.images ?? []).map(pair);
    const here = `${n}/${key}`;
    if (JSON.stringify(got) === JSON.stringify(want)) c.ok(`${here}: shown the ${want.length} sheets of the index`);
    else if (call.reused) c.warn(`${here}: carried over from a parent run, shown other sheets than the index's`);
    else c.fail(`${here}: the call was not shown the concept sheets the index names`);
  }
}

/**
 * The apply faults of the round, recomputed: the problems in run.json and the 'apply' faults of faults.json both match.
 */
async function checkApply(c: Check, n: string, r: Skyline, problems: Problem[]) {
  const faults: Fault[] = (await c.read(`${n}/faults.json`)) ?? [];
  const got = faults.filter(f => f.rule === 'apply').map(f => `${f.owner}|${f.fault}`).sort();
  const want = problems.map(x => `${x.owner}|${x.text}`).sort();
  const same = JSON.stringify(got) === JSON.stringify(want)
    && Bun.deepEquals(r.problems ?? [], problems.map(x => x.text));
  if (same) c.ok(`${n}: apply faults match (${want.length})`);
  else c.fail(`${n}: apply faults differ from a recomputation (${got.length} recorded, ${want.length} expected)`);
}

async function candidate(c: Check, r: Skyline, version: string, p: Pins): Promise<Rerun | undefined> {
  const { read, ok, fail, warn } = c, n = `round-${r.round}`;
  const input: Plan | undefined = p.input, index: ConceptIndex | undefined = p.index;
  const mission: Mission | undefined = p.mission;
  const plan: Plan | undefined = await read(`${n}/plan.json`);
  const rec: Checked | undefined = await read(`${n}/checks.json`);
  if (!(plan && rec)) return undefined;
  if (plan.version === version) ok(`${n}: version ${version}`);
  else fail(`${n}: plan version ${plan.version}, expected ${version}`);
  if (r.version !== version) fail(`${n}: run.json version ${r.version}`);
  const outs = { datum: await read(`${n}/datum.json`), vantage: await read(`${n}/vantage.json`) };
  const valid = agents.datum.safeParse(outs.datum).success && agents.vantage.safeParse(outs.vantage).success;
  let recomputed: number | undefined; // the apply faults of the round, for the engine's critic gate
  if (input && index && valid) {
    const known = await knownFor(index, pinnedKit(c.dir));
    const applied = await candidatePlan(input, outs, version, known, p.block);
    recomputed = applied.problems.length;
    const same = Bun.deepEquals(JSON.parse(JSON.stringify(applied.plan)), plan);
    if (same) ok(`${n}: plan.json = input plan + the recorded proposals`);
    else fail(`${n}: plan.json is not the input plan plus the recorded proposals`);
    await checkApply(c, n, r, applied.problems);
  }
  if (index) await checkShown(c, n, r, index);
  if (!(p.block && mission)) {
    warn(`${n}: checker rerun skipped (no valid pinned block or mission)`);
    return undefined;
  }
  const now = await checkAll(plan, p.block, mission, pinnedKit(c.dir));
  if (now.error !== rec.error) {
    const was = 'builds now, it did not when recorded';
    const what = now.error ? `cannot be built now (${now.error.slice(0, 100)})` : was;
    if (c.rerunDrift) warn(`${n}: the candidate ${what} (the code changed since the run)`);
    else fail(`${n}: the candidate ${what}`);
  }
  const faults = recomputed === undefined ? {} : { assessFaults: recomputed };
  return { ...compareChecks(c, n, r, now.checks, rec), ...faults };
}

export const skylineReplay: Replay = {
  skipNote: 'not run', // no skyline builder is skipped
  inputVersion: run => run.inputPlan.version,
  checksOk: r => r.checkerOk,
  superseded: () => false,
  pins: {
    raw: checkRawPlan,
    input: async (c, p) => ({
      ...await checkInputPlan(c, p), index: await checkConcepts(c), grammar: await checkGrammar(c),
      mission: await checkMission(c),
    }),
    block: checkBlock,
  },
  schema: (_run, key) => agents[key as Agent],
  candidate,
};
