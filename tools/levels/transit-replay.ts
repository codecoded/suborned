// Replay hooks of the transit objective: what the engine's replay (tools/gen/replay.ts) cannot know. Rebuilds the
// candidate plan from the recorded proposals, reruns the plan checker on it, and recomputes what a seed changed.
import type { Check, Pins, Rerun, Replay } from '../gen/plugin';
import { pinnedKit } from '../gen/pin-kit';
import { apply } from './apply';
import { checkBlock, checkInputPlan, checkRawPlan } from './pins';
import { inProcess } from './checker';
import { compareChecks } from './rerun';
import { schemaFor, type Agent } from './schemas';
import { fareClaimText, isFareSeed, seedFareClaim } from './seed';
import { applyDerived } from './timetable';

const BUILD = ['datum', 'vantage', 'pulse'] as const;
// runs before the field (r001-r005) carry Pulse's own run times; they replay as recorded
const derivedOf = (run: any) => run.timetable === 'derived';
const same = (a: unknown, b: unknown) => Bun.deepEquals(JSON.parse(JSON.stringify(a)), b);

export const transitReplay: Replay = {
  skipNote: 'a line could not be built',
  inputVersion: run => run.inputPlan.version, checksOk: r => r.checkerOk,
  // pre-pinning: its checker cannot be rerun, and its code changed with the Oct 9 block work (tools/plan civic.ts,
  // checks.ts C-0), so the hash adds nothing beyond "superseded"
  superseded: run => !run.inputBlock,
  pins: {
    raw: checkRawPlan,
    input: async (c, p) => {
      c.lines.push(derivedOf(c.run)
        ? 'ok    timetable derived by code' : 'ok    timetable as proposed (recorded before derivation)');
      return checkInputPlan(c, p);
    },
    block: checkBlock,
  },
  schema: (run, key) => schemaFor(key as Agent, derivedOf(run)),
  candidate,
  async seeded(c, r, call) {
    const seed = c.run.seeded, n = `round-${r.round}`, skipped: string[] = r.skipped ?? [];
    if (isFareSeed(seed) && call && r.round === 1) {
      // round-1 pulse.json = Pulse's untouched output + the seeded note, recomputed
      const hits = call.retrieval?.hits ?? [], got = await c.read(`${n}/pulse.json`);
      const want = call.original ? seedFareClaim(call.original, hits, seed === 'fare-claim').out : undefined;
      if (want && Bun.deepEquals(want, got)) c.ok(`${n}: pulse.json = the original + the seeded note`);
      else c.fail(`${n}: pulse.json is not the recorded original plus the seeded note`);
    }
    if (isFareSeed(seed) && r.round > 1 && !skipped.includes('pulse')) {
      const pulse = await c.read(`${n}/pulse.json`);
      const survived = (pulse?.notes ?? []).some((x: string) => x.includes(fareClaimText(seed)));
      const reused = r.reused?.includes('pulse') ? ' (reused, not re-proposed)' : '';
      c.lines.push(survived
        ? `warn  ${n}: seeded fare claim SURVIVED in Pulse's output${reused}; the run's outcome to judge`
        : `ok    ${n}: seeded fare claim is gone from Pulse's output (repaired)`);
    }
  },
};

async function candidate(c: Check, r: any, version: string, p: Pins): Promise<Rerun | undefined> {
  const { run, read, ok, fail, warn } = c, { input, block, superseded } = p, n = `round-${r.round}`;
  const skipped: string[] = r.skipped ?? [], derived = derivedOf(run), noChecker = skipped.includes('checker');
  const plan = noChecker ? undefined : await read(`${n}/plan.json`);
  const rec = noChecker ? undefined : await read(`${n}/checks.json`);
  if (!(plan && rec)) return undefined;
  if (!block && !superseded) warn(`${n}: checker rerun skipped (no valid pinned block)`);
  if (plan.version === version) ok(`${n}: version ${version}`);
  else fail(`${n}: plan version ${plan.version}, expected ${version}`);
  if (r.version !== version) fail(`${n}: run.json version ${r.version}`);
  const outs = {
    datum: await read(`${n}/datum.json`),
    vantage: await read(`${n}/vantage.json`),
    pulse: await read(`${n}/pulse.json`),
  };
  const valid = !skipped.length && BUILD.every(k => schemaFor(k, derived).safeParse(outs[k]).success);
  if (input && valid && !derived) {
    if (same(apply(input, outs, version).plan, plan)) ok(`${n}: plan.json = input plan + the three recorded proposals`);
    else fail(`${n}: plan.json is not the input plan plus the recorded proposals`);
  }
  // the apply faults of the round (apply problems and timetable faults), for the engine's critic gate
  let recomputed: number | undefined;
  if (input && valid && derived) {
    // the timetable is recomputed from the proposals and the checker's stop table, never read back
    if (!block) warn(`${n}: derived timetable not recomputed (no valid pinned block)`);
    else {
      const d = await applyDerived(input, outs, version, block);
      recomputed = d.problems.length + d.faults.length;
      if (same(d.plan, plan)) ok(`${n}: plan.json = input plan + the recorded proposals, timetable recomputed`);
      else fail(`${n}: plan.json differs from the input plan plus the recomputed timetable`);
      if (same(d.timetable, await read(`${n}/timetable.json`))) ok(`${n}: timetable.json matches the recomputation`);
      else fail(`${n}: timetable.json differs from the recomputed timetable`);
      const recorded = ((await read(`${n}/faults.json`)) ?? []).filter((f: any) => f.rule === 'timetable');
      const got = recorded.map((f: any) => f.fault).sort();
      if (JSON.stringify([...d.faults].sort()) === JSON.stringify(got) && r.timetableFaults === got.length) {
        ok(`${n}: timetable faults match (${got.length})`);
      } else {
        const counts = `${got.length} recorded, ${d.faults.length} expected`;
        fail(`${n}: timetable faults differ from a recomputation (${counts})`);
      }
    }
  }
  if (!block) return undefined;
  // the rules the run recorded: the world rules too, on the kit it pinned, where its plan carries the world standards (the A04 plans do not, and rerun as before)
  const R = await inProcess(plan, rec.strict !== false, block, { world: true, kit: pinnedKit(c.dir) });
  const faults = recomputed === undefined ? {} : { assessFaults: recomputed };
  return { ...compareChecks(c, n, r, R.checks, rec), ...faults };
}
