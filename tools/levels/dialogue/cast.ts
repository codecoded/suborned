// The dialogue checker on the candidate of a round (the people plug-in's cast.ts): the tables of the scope, the tables it fills slots from, the pinned people and rules.
// Shared by the run and its replay, so both judge alike.
import type { Voices } from '../../../contracts/dialogue/voices.schema';
import { checkDialogue, type Checked } from '../../dialogue/check';
import { parseTexts, type Texts } from '../../dialogue/texts';
import type { TextFile } from '../../../contracts/missions/handover/text.schema';

/** The seed of a run's expansion: the plan's id, as `bun run dialogue` does, so the lines a run checks are the lines the command makes. */
export const seedOf = (planId: string) => `dialogue|${planId}`;

export function checkRound(t: Texts, scope: string, voices: Voices, context: Voices[], text: Pick<TextFile, 'dialogue' | 'scenes'>): Checked {
  const p = parseTexts(t);
  return checkDialogue({ dr: p.dr, rules: p.prules, rb: p.rb, people: p.people, voices: [voices, ...context], scope, seed: seedOf(p.people.plan.id), pool: [...p.pool, ...p.people.persons.flatMap(x => x.name.split(/\s+/))], reference: text });
}
