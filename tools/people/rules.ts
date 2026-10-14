// The people rules (contracts/people/people.rules.json, Motive brief 010 A) as the expander and checker read them.
// Anything the file does not allow is rejected (GDD §01: unknown = reject). `--rules <file>` points the tools at another file.
import { PeopleRules, isTalker, allowedTemperaments } from '../../contracts/people/people.rules.schema';
export { PeopleRules, isTalker, allowedTemperaments };
export type Rules = PeopleRules;
export const RULES_FILE = 'contracts/people/people.rules.json';
export const parseRules = (text: string): Rules => PeopleRules.parse(JSON.parse(text));

/** The employer of a block building (rules temperamentsBy.employers[x].block), if any. */
export const employerOfBlock = (r: Rules, building: string) => Object.entries(r.temperamentsBy.employers).find(([, e]) => e.block === building);
/** The positions a trade's category has, keeper first. */
export const positionsOf = (r: Rules, category: string) => r.positions.byCategory[category] ?? [];
