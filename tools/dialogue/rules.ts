// The dialogue rules (contracts/dialogue/dialogue.rules.json, Fable brief 012) as the expander and the checker read them.
// Anything the file does not allow is rejected (GDD §01: unknown = reject). The people rules and the rulebook are the other two rule files read here.
import { DialogueRules } from '../../contracts/dialogue/dialogue.rules.schema';
import { PeopleRules, TEMPERAMENTS, type Temperament } from '../../contracts/people/people.rules.schema';
import { Rulebook } from '../../contracts/rules/rulebook.schema';
export { DialogueRules, PeopleRules, Rulebook, TEMPERAMENTS };
export type { Temperament };

export const RULES_FILE = 'contracts/dialogue/dialogue.rules.json';
export const RULEBOOK_FILE = 'contracts/rules/rulebook.json';
export const PEOPLE_RULES_FILE = 'contracts/people/people.rules.json';
export const parseRules = (text: string): DialogueRules => DialogueRules.parse(JSON.parse(text));

/** The group a position (of a trade's category) or a role (of a civic employer) belongs to; undefined = unknown, which the checker rejects. */
export const groupOf = (r: DialogueRules, cover: string): string | undefined => Object.entries(r.groups).find(([, g]) => g.covers.includes(cover))?.[0];

/** Every position and role the people rules know. The groups must cover exactly these. */
export function coversOf(people: PeopleRules): string[] {
  return [...new Set([...Object.values(people.positions.byCategory).flat(), ...Object.keys(people.temperamentsBy.roles)])].sort();
}

/** The temperaments a group can have: the union over what it covers. A position takes the temperaments of any trade that has it; a role takes its own (people.rules temperamentsBy). */
export function groupTemperaments(r: DialogueRules, people: PeopleRules, group: string): Temperament[] {
  const out = new Set<Temperament>();
  for (const c of r.groups[group]?.covers ?? []) {
    const role = people.temperamentsBy.roles[c];
    if (role) { for (const t of role.allow) out.add(t); continue; }
    for (const t of Object.values(people.temperamentsBy.trades)) if (people.positions.byCategory[t.category]?.includes(c)) for (const x of t.allow) out.add(x);
  }
  return TEMPERAMENTS.filter(t => out.has(t));
}

/** The needs a lexicon words: the rules' list, less the retired `secret` when the rules carry `secrets` (it became the `record` share). */
export const lexiconNeeds = (people: PeopleRules): string[] => Object.keys(people.needs.list).filter(n => !(people.secrets && n === 'secret')).sort();

/** The tie kinds the rules know. */
export const tieKinds = (people: PeopleRules): string[] => Object.keys(people.ties.kinds);

/** The rulebook's debrief lines and their fields. */
export const debriefFields = (rb: Rulebook): Record<string, readonly string[]> =>
  Object.fromEntries(Object.entries((rb as unknown as { debrief: { lines: Record<string, { fields: string[] }> } }).debrief.lines).map(([k, v]) => [k, v.fields]));
