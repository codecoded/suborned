// What a people run pins besides the plan and the block (tools/levels/pins.ts): life.json, the people rules and the Handover mission, as the exact
// text the run read. Replay reads these, never the files now on disk, and hands the corpus the same text (overlay), so a later edit breaks nothing.
import type { Check, Pins } from '../../gen/plugin';
import { sha256 } from '../../gen/sha';
import type { Texts } from '../../people/inputs';
import { RULES_FILE } from '../../people/rules';
import { MISSION_FILE, lifeFileOf } from '../../people/inputs';
import { catalogueFileOf, NEEDS_RULES_FILE } from '../../people/needs';

type Rec = { source: string; sha256: string };
export const PINNED = { life: 'input.life.json', rules: 'input.rules.json', mission: 'input.mission.json' } as const;
export const FIELD = { life: 'inputLife', rules: 'inputRules', mission: 'inputMission' } as const;
type Key = keyof typeof PINNED;
/** Brief 017: the need kinds and the catalogue, pinned when the run read them (a run made before them has neither, and replays without records). */
export const PINNED_NEEDS = { rules: 'input.needs.rules.json', catalogue: 'input.needs.json' } as const;
export const FIELD_NEEDS = { rules: 'inputNeedsRules', catalogue: 'inputNeeds' } as const;

/** Run start: copies the three files into the run folder; returns what run.json records. */
export async function pinInputs(rdir: string, t: Texts, planId: string, files: { rules?: string; mission?: string } = {}) {
  const src: Record<Key, string> = { life: lifeFileOf({ id: planId }), rules: files.rules ?? RULES_FILE, mission: files.mission ?? MISSION_FILE };
  const record: Record<string, Rec> = {};
  for (const k of Object.keys(PINNED) as Key[]) { await Bun.write(`${rdir}/${PINNED[k]}`, t[k]); record[FIELD[k]] = { source: src[k], sha256: sha256(t[k]) }; }
  if (t.needs) {
    const nsrc = { rules: NEEDS_RULES_FILE, catalogue: catalogueFileOf(planId) };
    for (const k of Object.keys(PINNED_NEEDS) as (keyof typeof PINNED_NEEDS)[]) { await Bun.write(`${rdir}/${PINNED_NEEDS[k]}`, t.needs[k]); record[FIELD_NEEDS[k]] = { source: nsrc[k], sha256: sha256(t.needs[k]) }; }
  }
  return record;
}

/** Replay, first: each pinned text matches its sha256; the files become the corpus's text (overlay), and the texts the checker reruns on. */
export async function checkInputs(c: Check, plan: string | undefined): Promise<Pins> {
  const texts: Partial<Texts> = {}, overlay: Record<string, string> = {};
  for (const k of Object.keys(PINNED) as Key[]) {
    const rec: Rec | undefined = c.run[FIELD[k]];
    if (!rec) { c.fail(`run.json has no ${FIELD[k]}`); continue; }
    const raw = await Bun.file(`${c.dir}/${PINNED[k]}`).text().catch(() => undefined);
    if (raw === undefined) c.fail(`${PINNED[k]} is missing`);
    else if (sha256(raw) !== rec.sha256) c.fail(`${PINNED[k]} does not match the recorded sha256`);
    else { c.ok(`${k} ${rec.sha256.slice(0, 12)}`); texts[k] = raw; overlay[rec.source] = raw; }
  }
  // the need kinds and the catalogue, only if the run pinned them (both or neither)
  const nk = Object.keys(PINNED_NEEDS) as (keyof typeof PINNED_NEEDS)[], pinnedNeeds = nk.filter(k => c.run[FIELD_NEEDS[k]]), nt: Partial<Record<keyof typeof PINNED_NEEDS, string>> = {};
  if (pinnedNeeds.length && pinnedNeeds.length !== nk.length) c.fail('run.json pins one of the need kinds and the catalogue, not both');
  else for (const k of pinnedNeeds) {
    const rec: Rec = c.run[FIELD_NEEDS[k]], raw = await Bun.file(`${c.dir}/${PINNED_NEEDS[k]}`).text().catch(() => undefined);
    if (raw === undefined) c.fail(`${PINNED_NEEDS[k]} is missing`);
    else if (sha256(raw) !== rec.sha256) c.fail(`${PINNED_NEEDS[k]} does not match the recorded sha256`);
    else { c.ok(`needs ${k} ${rec.sha256.slice(0, 12)}`); nt[k] = raw; overlay[rec.source] = raw; }
  }
  const block = await Bun.file(`${c.dir}/input.block.json`).text().catch(() => undefined);
  if (block !== undefined && c.run.inputBlock) overlay[c.run.inputBlock.source] = block;
  return { texts: { ...texts, ...(nt.rules !== undefined && nt.catalogue !== undefined ? { needs: { rules: nt.rules, catalogue: nt.catalogue } } : {}), ...(plan !== undefined ? { plan } : {}), ...(block !== undefined ? { block } : {}) }, overlay };
}
