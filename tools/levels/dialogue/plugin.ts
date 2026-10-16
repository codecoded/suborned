// The dialogue objective (brief 012), a plug-in of the generation engine (tools/gen), copied from the people template: Fable writes the voice tables for a scope (the lexicon,
// the scenes between tied people, or the groups of a part of the city), tools/dialogue expands them into every person's lines and the dialogue checker (D-0..D-12) holds
// tables and lines to the dialogue rules, Keel critiques tables that passed, faults go back to Fable. The model writes tables; code makes the lines. No model call is
// needed to run it end to end (--preview answers with a stand-in).
//   bun run levels dialogue --scope lexicon        first: the shared words; promote it (tools/dialogue/promote.ts) before the others, which fill their slots from it
//   bun run levels dialogue --scope scenes|street|civic
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { z } from 'zod';
import { Voices, type Voices as VoicesT } from '../../../contracts/dialogue/voices.schema';
import { RunRefused } from '../../gen/continue';
import type { Fault, Plugin } from '../../gen/plugin';
import { checkPromotion } from '../../dialogue/promoted';
import { peopleOf } from '../../dialogue/inputs';
import { parseTexts, readText, readTexts_, type Parsed, type Texts } from '../../dialogue/texts';
import { parseRules, RULES_FILE } from '../../dialogue/rules';
import { standIn } from '../../dialogue/standin';
import { isLive } from '../../rag/claude';
import { root, writeJson } from '../../rag/config';
import { pinPlan, readPlan } from '../pins';
import { checkRound } from './cast';
import { dialogueLedger, dialogueLedgerFile, recordDialogue } from './drift';
import { dialogueHash } from './hash';
import { objectives } from './objectives';
import { MISSION_FILE, pinInputs } from './pins';
import { dialogueReplay } from './replay';
import { agents, type FableOut } from './schemas';
import { sampleView, scopeView, type View } from './view';
import type { TextFile } from '../../../contracts/missions/handover/text.schema';

type State = { texts: Texts; parsed: Parsed; text: TextFile; scope: string; context: VoicesT[]; lexiconText?: string; lexiconSource?: string; rdir: string; planId: string };
const j = (x: unknown) => JSON.stringify(x);
// the scopes are the dialogue rules' (read once here: the plug-in's `scopes` is static); the first is the default, and it is the lexicon every other scope fills from
const SCOPES = Object.keys(parseRules(readFileSync(resolve(root, RULES_FILE), 'utf8')).scopes);
const OBJECTIVE = 'Dialogue (brief 012): the voice tables of a scope, written by voice group × temperament × situation, and the shared words; accepted only when the dialogue checker (D-rules: every situation covered,'
  + ' lines that fit, slots filled, no names, tone and rating, signal words, no repeats, the knowledge rule, scenes, lexicon, the Handover as reference) is clean on the tables and on every person\'s expanded lines, and the critic passes.';

/** The lexicon a non-lexicon scope fills its slots from: the promoted one, or (a preview only, no model reached) the stand-in. */
async function lexiconFor(planId: string, scope: string, p: Parsed, text: TextFile): Promise<{ voices?: VoicesT; text?: string; source?: string }> {
  if (scope === 'lexicon') return {};
  const promo = checkPromotion(planId, 'lexicon');
  if (promo.ok) return { voices: Voices.parse(JSON.parse(promo.text!)), text: promo.text, source: promo.record!.voices.file };
  if (process.env.DIALOGUE_LEXICON === 'stand-in' && !isLive()) {
    const voices = standIn('lexicon', p.dr, p.rb, p.prules, text), t = JSON.stringify(voices, null, 1);
    return { voices, text: t, source: 'stand-in lexicon (preview; not words)' };
  }
  throw new RunRefused(`scope ${scope} fills its slots from the lexicon, and ${promo.label} is not promoted (${promo.problems[0] ?? 'missing'}): run \`bun run levels dialogue --scope lexicon\`, then promote the accepted run with tools/dialogue/promote.ts`);
}

export const dialoguePlugin: Plugin<State> = {
  id: 'dialogue', defaultInput: 'contracts/world/harbour-district.plan.json', scopes: SCOPES,
  builders: [{ key: 'fable', owner: objectives.fable.owner, objective: objectives.fable, schema: agents.fable }],
  critic: { key: 'keel', owner: 'Keel', objective: objectives.keel, schema: agents.keel },
  header: { proposals: 'voice tables' },
  code: {
    label: 'tools/dialogue (expand, check), contracts/dialogue, contracts/people, contracts/rules, tools/levels/dialogue',
    hash: dialogueHash, ledger: dialogueLedger, ledgerFile: dialogueLedgerFile,
    declare: 'bun tools/levels/dialogue/drift.ts declare', record: recordDialogue,
  },
  async open(path, scope = SCOPES[0]) {
    if (!SCOPES.includes(scope)) throw new Error(`unknown scope ${scope}`);
    const plan = await readPlan(path), gen = await peopleOf(path), texts = await readTexts_({ people: gen.text, names: gen.pool }), parsed = parseTexts(texts), text = await readText();
    const lex = await lexiconFor(plan.id, scope, parsed, text);
    return { id: plan.id, version: plan.version, async pin(rdir) {
      const { record } = await pinPlan(plan, rdir);
      const inputs = await pinInputs(rdir, { ...texts, text: JSON.stringify(text, null, 1), mission: await Bun.file(resolve(root, MISSION_FILE)).text(), ...(lex.text ? { lexicon: lex.text } : {}) }, lex.source ? { lexicon: lex.source } : {});
      return { state: { texts, parsed, text, scope, context: lex.voices ? [lex.voices] : [], lexiconText: lex.text, lexiconSource: lex.source, rdir, planId: plan.id }, record: { ...record, ...inputs, scope } };
    } };
  },
  async task(s, _r, _key) {
    const v: View = { scope: s.scope, p: s.parsed, text: s.text, context: s.context };
    return `Task: write the voice tables for the scope ${s.scope}.\n${scopeView(v)}\nOutput the full tables (schema suborned.voices/1, scope "${s.scope}").`;
  },
  async assess(s, r) {
    const out = r.outs.fable as FableOut, checked = checkRound(s.texts, s.scope, out.voices, s.context, s.text);
    const failed = checked.checks.filter(c => !c.ok);
    await writeJson(`${r.rd}/checks.json`, { plan: s.planId, version: r.version, scope: s.scope, ok: checked.ok, counts: checked.counts, checks: checked.checks });
    if (checked.lines) await writeJson(`${r.od}/lines.json`, checked.lines);
    return {
      ok: checked.ok, line: `checker ${checked.ok ? 'clean' : `FAIL ${failed.map(c => c.id).join(',')}`} (${checked.counts.persons} people, ${checked.counts.entries} entries, ${checked.counts.scenes} scenes)`,
      record: { checkerOk: checked.ok, failedChecks: failed.map(c => c.id), problems: [], persons: checked.counts.persons },
      failures: failed.map(c => ({ rule: c.id, owners: ['Fable'], fault: `${c.rule}: ${c.value}; ${c.detail ?? ''} [source: ${c.source}]`, fix: 'bring the tables inside the rule' })),
      faults: [] as Fault[],
      criticTask(cited, lastFaults) {
        const revision = r.n > 1 || lastFaults.length ? `\n\nThis is a revision. The faults from last round (JSON), to recheck:\n${j(lastFaults)}` : '';
        const v: View = { scope: s.scope, p: s.parsed, text: s.text, context: s.context };
        return `Task: judge the voice tables.\nObjective under test: ${OBJECTIVE}\n\n`
          + `Output schema (JSON): ${j(z.toJSONSchema(agents.fable))}\n\n`
          + `${scopeView(v)}\n\nDialogue checker report on the tables and on the lines they expand to:\n${checked.checks.map(c => `${c.ok ? 'ok  ' : 'FAIL'} ${c.id} ${c.rule}: ${c.value}${c.ok ? '' : ` | ${c.detail}`}`).join('\n')}\n\n`
          + `What the tables expand to (a sample, as the player will read it):\n${sampleView(checked.lines)}\n\n`
          + `Candidate tables (JSON): ${j(out)}\n\n`
          + `What the builder retrieved and cites. Code has already checked that every cited id was retrieved by its own call: raise no fault for citation membership. Judge whether the claims are true to the cited text:\n${cited}${revision}`;
      },
    };
  },
  ownersOf: rule => (/^D-\d+$/.test(rule) ? ['Fable'] : undefined),
  standIn: (s, key) => (key === 'fable' ? { voices: standIn(s.scope, s.parsed.dr, s.parsed.rb, s.parsed.prules, s.text), notes: [], sources: [] } satisfies FableOut : undefined),
  replay: dialogueReplay,
};
