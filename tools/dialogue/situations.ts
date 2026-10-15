// The situations a person's words must cover, derived from the rulebook and the people rules, never typed here: every reply the rulebook can produce
// (approach × temperament × outcome, `suspicious` when the approach is reported), a confirmation per temperament, questioning, the world events.
// The reply derivation is the Handover checker's (contracts/missions/handover/text.test.ts replyCells), moved here so the generator and the Handover agree.
import type { SceneT } from '../../contracts/dialogue/voices.schema';
import * as R from '../../contracts/rules/rulebook.ref';
import type { DialogueRules, PeopleRules, Rulebook, Temperament } from './rules';
import { groupTemperaments, lexiconNeeds, tieKinds, TEMPERAMENTS } from './rules';

export type Key = { event: string; verb?: string; outcome?: string; temperament?: Temperament };
export const keyOf = (k: Key): string => [k.event, k.verb, k.outcome, k.temperament].filter(x => x !== undefined).join('/');

/** Every (approach, temperament, outcome) the rulebook can produce in any band; `suspicious` when the approach is reported. Sorted. */
export function replyCells(rb: Rulebook, people: PeopleRules): { verb: string; temperament: Temperament; outcome: string }[] {
  const cells = new Set<string>(), ALL = new Set<R.Know>(['routine', 'need', 'temperament']);
  for (const temp of TEMPERAMENTS) for (const band of ['hero', 'neutral', 'menace'] as const) for (const a of ['ask', 'pay', 'help', 'pressure']) {
    let s: R.Subject = { id: 'x', tier: 'simulated', temperament: temp, need: 'debt', scales: { ...people.temperaments[temp].start }, memory: [], turned: false };
    const evt = R.event(rb, `turn.${a}`);
    for (let i = 0; i < 4; i++) {
      const r = R.resolve(rb, people, evt, { subject: s, band, knows: ALL, given: { reach: true } as never });
      const reported = r.changes.some(c => c.do === 'report');
      cells.add(`${a}/${temp}/${reported ? 'suspicious' : r.outcome}`);
      s = R.apply(rb, people, s, r.changes, { by: 'fixer', place: 'p', time: 10 + i, witnessed: false });
      if (a !== 'help' && i >= 1) break;
    }
  }
  return [...cells].sort().map(c => { const [verb, temperament, outcome] = c.split('/') as [string, Temperament, string]; return { verb, temperament, outcome }; });
}

/** The cells a group's table must have, one each: replies and a confirmation per temperament the group can have, then the one-voice situations. */
export function requiredCells(dr: DialogueRules, rb: Rulebook, people: PeopleRules, group: string): Key[] {
  const temps = groupTemperaments(dr, people, group), s = dr.situations, out: Key[] = [];
  for (const c of replyCells(rb, people)) if (temps.includes(c.temperament)) out.push({ event: s.reply.event, verb: c.verb, outcome: c.outcome, temperament: c.temperament });
  for (const t of temps) out.push({ event: s.confirm.event, verb: s.confirm.verb, temperament: t });
  for (const o of s.questioned.outcomes) out.push({ event: s.questioned.event, outcome: o });
  for (const e of s.world) out.push({ event: e });
  return out;
}

/** What a person needs of the tables: the group's cells for their temperament. */
export const requiredFor = (dr: DialogueRules, rb: Rulebook, people: PeopleRules, group: string, temperament: Temperament): Key[] =>
  requiredCells(dr, rb, people, group).filter(k => k.temperament === undefined || k.temperament === temperament);

export type SceneKey = { verb: SceneT['verb']; tie?: SceneT['tie']; group?: string; temperament?: Temperament };
export const sceneKeyOf = (k: SceneKey): string => [k.verb, k.tie ?? '-', k.group ?? '-', k.temperament ?? '-'].join('/');

/** The scenes a scope's `scenes` block must have: Listen by tie kind (or none) × temperament, the lovers' call, the lovers' Watch. */
export function requiredScenes(people: PeopleRules): SceneKey[] {
  const ties = [...tieKinds(people), 'none'] as NonNullable<SceneT['tie']>[], out: SceneKey[] = [];
  for (const tie of ties) for (const temperament of TEMPERAMENTS) out.push({ verb: 'listen', tie, temperament });
  if (tieKinds(people).includes('lover')) { for (const temperament of TEMPERAMENTS) out.push({ verb: 'call', tie: 'lover', temperament }); out.push({ verb: 'watch', tie: 'lover' }); }
  return out;
}
/** The Watch scene each group of the scope's groups block has. */
export const requiredWatch = (groups: string[]): SceneKey[] => groups.map(group => ({ verb: 'watch' as const, group }));

export { lexiconNeeds };
