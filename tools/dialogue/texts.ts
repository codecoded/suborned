// The texts of a dialogue run, read from files or from what a run pinned: the people file, the three rule files, the district's name pool, the Handover's text. Nothing here reads the
// people tools, so the checker and its replay depend only on these and on the contracts (tools/levels/dialogue/hash.ts).
import { resolve } from 'node:path';
import { z } from 'zod';
import { People } from '../../contracts/people/people.schema';
import { TextFile } from '../../contracts/missions/handover/text.schema';
import { root } from '../rag/config';
import { DialogueRules, PeopleRules, PEOPLE_RULES_FILE, Rulebook, RULEBOOK_FILE, RULES_FILE } from './rules';

export const TEXT_FILE = 'contracts/missions/handover/text.json';
export const tablesFileOf = (planId: string, scope: string) => `contracts/dialogue/${planId}.${scope}.voices.json`;
export const promotionFileOf = (planId: string, scope: string) => `contracts/dialogue/${planId}.${scope}.voices.promotion.json`;

/** The texts a dialogue run reads (and pins). `people` is the people file as `bun run people` writes it; `names` the district's name pool, a JSON array of strings. */
export type Texts = { people: string; peoplerules: string; rulebook: string; rules: string; names: string };
export type Parsed = { people: People; prules: PeopleRules; rb: Rulebook; dr: DialogueRules; pool: string[] };
export const parseTexts = (t: Texts): Parsed => ({
  people: People.parse(JSON.parse(t.people)), prules: PeopleRules.parse(JSON.parse(t.peoplerules)), rb: Rulebook.parse(JSON.parse(t.rulebook)), dr: DialogueRules.parse(JSON.parse(t.rules)),
  pool: z.array(z.string()).parse(JSON.parse(t.names)),
});
const rd = (f: string) => Bun.file(resolve(root, f)).text();
export async function readTexts_(o: { people: string; names?: string[]; peoplerules?: string; rulebook?: string; rules?: string }): Promise<Texts> {
  return { people: o.people, names: JSON.stringify(o.names ?? []), peoplerules: await rd(o.peoplerules ?? PEOPLE_RULES_FILE), rulebook: await rd(o.rulebook ?? RULEBOOK_FILE), rules: await rd(o.rules ?? RULES_FILE) };
}
export const readText = async (file = TEXT_FILE) => TextFile.parse(JSON.parse(await rd(file)));
