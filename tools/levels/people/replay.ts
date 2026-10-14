// Replay hooks of the people objective: what the engine's replay (tools/gen/replay.ts) cannot know. Checks the pinned texts, then reruns
// the expander and the people checker on every round's recorded cast tables and compares with the checks.json the run recorded.
import type { Check, Pins, Replay, Rerun } from '../../gen/plugin';
import { world, type Texts } from '../../people/inputs';
import { checkBlock, checkInputPlan } from '../pins';
import { compareChecks, type RoundRecord } from '../rerun';
import { checkRawPlan } from '../pins';
import { checkInputs } from './pins';
import { agents, PulseOut, type Agent } from './schemas';
import { checkCast } from './cast';

type PeopleRound = RoundRecord & { round: number };

async function candidate(c: Check, r: PeopleRound, version: string, p: Pins): Promise<Rerun | undefined> {
  const n = `round-${r.round}`, texts: Partial<Texts> = p.texts ?? {};
  const rec = await c.read(`${n}/checks.json`), out = await c.read(`${n}/pulse.json`);
  if (!(rec && out)) return undefined;
  if (rec.version === version) c.ok(`${n}: version ${version}`); else c.fail(`${n}: checks.json version ${rec.version}, expected ${version}`);
  const cast = PulseOut.safeParse(out);
  if (!cast.success || !(texts.plan && texts.block && texts.life && texts.rules && texts.mission)) {
    c.warn(`${n}: checker rerun skipped (no valid cast or pinned inputs)`); return undefined;
  }
  const w = world(texts as Texts, rec.scope ?? c.run.scope ?? 'civic-block');
  const { checked } = checkCast(w, cast.data.cast);
  return compareChecks(c, n, r, checked.checks, rec);
}

export const peopleReplay: Replay = {
  skipNote: 'not run',
  inputVersion: run => run.inputPlan.version,
  checksOk: r => r.checkerOk,
  superseded: () => false,
  pins: {
    raw: async c => {
      const plan = await checkRawPlan(c), inputs = await checkInputs(c, plan.raw);
      return { ...plan, ...inputs, overlay: { ...plan.overlay, ...inputs.overlay } };
    },
    input: async (c, p) => ({ ...await checkInputPlan(c, p) }),
    block: checkBlock,
  },
  schema: (_run, key) => agents[key as Agent],
  candidate,
};
