// What a dialogue run pins besides the plan and the block (tools/levels/pins.ts): the people it expanded for, the people rules, the rulebook, the dialogue rules, the Handover's text (the
// reference), and for a scope that fills slots from the lexicon, that lexicon: the exact text the run read. Replay reads these, never the files now on disk, so a later edit breaks nothing.
import type { Check, Pins } from '../../gen/plugin';
import { sha256 } from '../../gen/sha';
import { PEOPLE_RULES_FILE as PEOPLE_RULES, RULEBOOK_FILE, RULES_FILE } from '../../dialogue/rules';
import { TEXT_FILE } from '../../dialogue/texts';

type Rec = { source: string; sha256: string };
export const PINNED = { people: 'input.people.json', names: 'input.names.json', peoplerules: 'input.people.rules.json', rulebook: 'input.rulebook.json', rules: 'input.dialogue.rules.json', text: 'input.handover.text.json', lexicon: 'input.lexicon.json' } as const;
export const FIELD = { people: 'inputPeople', names: 'inputNames', peoplerules: 'inputPeopleRules', rulebook: 'inputRulebook', rules: 'inputDialogueRules', text: 'inputHandoverText', lexicon: 'inputLexicon' } as const;
export type Key = keyof typeof PINNED;
export type Pinned = Partial<Record<Key, string>>;
const SOURCE: Record<Key, string> = { people: 'out/people/<plan>.people.json (the promoted cast, expanded)', names: 'contracts/people/<plan>.cast.json names (the district\'s name pool)', peoplerules: PEOPLE_RULES, rulebook: RULEBOOK_FILE, rules: RULES_FILE, text: TEXT_FILE, lexicon: 'contracts/dialogue/<plan>.lexicon.voices.json (promoted) or the stand-in' };
/** The files a replay cannot do without (the lexicon is pinned only for a scope that fills slots from it). */
export const REQUIRED: Key[] = ['people', 'names', 'peoplerules', 'rulebook', 'rules', 'text'];

/** Run start: writes each text into the run folder; returns what run.json records. */
export async function pinInputs(rdir: string, t: Pinned, files: Partial<Record<Key, string>> = {}) {
  const record: Record<string, Rec> = {};
  for (const k of Object.keys(PINNED) as Key[]) {
    const text = t[k];
    if (text === undefined) continue;
    await Bun.write(`${rdir}/${PINNED[k]}`, text);
    record[FIELD[k]] = { source: files[k] ?? SOURCE[k], sha256: sha256(text) };
  }
  return record;
}

/** Replay, first: each pinned text matches its sha256; the people rules become the corpus's text (overlay). */
export async function checkInputs(c: Check, plan: string | undefined): Promise<Pins> {
  const texts: Pinned = {}, overlay: Record<string, string> = {};
  for (const k of Object.keys(PINNED) as Key[]) {
    const rec: Rec | undefined = c.run[FIELD[k]];
    if (!rec) { if (REQUIRED.includes(k)) c.fail(`run.json has no ${FIELD[k]}`); continue; }
    const raw = await Bun.file(`${c.dir}/${PINNED[k]}`).text().catch(() => undefined);
    if (raw === undefined) c.fail(`${PINNED[k]} is missing`);
    else if (sha256(raw) !== rec.sha256) c.fail(`${PINNED[k]} does not match the recorded sha256`);
    else { c.ok(`${k} ${rec.sha256.slice(0, 12)}`); texts[k] = raw; if (k === 'peoplerules') overlay[rec.source] = raw; }
  }
  const block = await Bun.file(`${c.dir}/input.block.json`).text().catch(() => undefined);
  if (block !== undefined && c.run.inputBlock) overlay[c.run.inputBlock.source] = block;
  return { texts: { ...texts, ...(plan !== undefined ? { plan } : {}), ...(block !== undefined ? { block } : {}) }, overlay };
}
