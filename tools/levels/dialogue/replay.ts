// Replay hooks of the dialogue objective: what the engine's replay (tools/gen/replay.ts) cannot know. Checks the pinned texts, then reruns the dialogue checker on every
// round's recorded tables and compares with the checks.json the run recorded.
import { Voices } from '../../../contracts/dialogue/voices.schema';
import { TextFile } from '../../../contracts/missions/handover/text.schema';
import type { Check, Pins, Replay, Rerun } from '../../gen/plugin';
import { type Texts } from '../../dialogue/texts';
import { checkBlock, checkInputPlan, checkRawPlan } from '../pins';
import { compareChecks, type RoundRecord } from '../rerun';
import { checkRound } from './cast';
import { checkInputs, type Pinned } from './pins';
import { agents, FableOut, type Agent } from './schemas';

type DialogueRound = RoundRecord & { round: number };

async function candidate(c: Check, r: DialogueRound, version: string, p: Pins): Promise<Rerun | undefined> {
  const n = `round-${r.round}`, t: Pinned = p.texts ?? {};
  const rec = await c.read(`${n}/checks.json`), out = await c.read(`${n}/fable.json`);
  if (!(rec && out)) return undefined;
  if (rec.version === version) c.ok(`${n}: version ${version}`); else c.fail(`${n}: checks.json version ${rec.version}, expected ${version}`);
  const parsed = FableOut.safeParse(out);
  if (!parsed.success || !(t.people && t.names && t.peoplerules && t.rulebook && t.rules && t.text)) { c.warn(`${n}: checker rerun skipped (no valid tables or pinned inputs)`); return undefined; }
  const texts: Texts = { people: t.people, names: t.names, peoplerules: t.peoplerules, rulebook: t.rulebook, rules: t.rules };
  const scope = rec.scope ?? c.run.scope ?? 'lexicon', context = t.lexicon ? [Voices.parse(JSON.parse(t.lexicon))] : [];
  const checked = checkRound(texts, scope, parsed.data.voices, context, TextFile.parse(JSON.parse(t.text)));
  return compareChecks(c, n, r, checked.checks, rec);
}

export const dialogueReplay: Replay = {
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
