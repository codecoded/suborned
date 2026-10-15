// Where the people of a dialogue run come from: the promoted cast of the plan, expanded as `bun run people` does it. Only the command line and the plug-in's `open` read this;
// the checker and its replay read the pinned copy (texts.ts).
import { Cast } from '../../contracts/people/cast.schema';
import { peopleHash } from '../levels/people/hash';
import { expand } from '../people/expand';
import { readTexts, world } from '../people/inputs';
import { castFileOf, checkPromotion } from '../people/promoted';
export { castFileOf };
export * from './texts';

/** The promoted cast of the plan, expanded: the people file text, and the cast's name pool. Refuses a promoted cast that drifted from its run. */
export async function peopleOf(planFile: string): Promise<{ text: string; pool: string[]; plan: { id: string; version: string } }> {
  const texts = await readTexts(planFile), w = world(texts, 'civic-block'), promo = checkPromotion(w.plan.id);
  if (!promo.ok) throw new Error(`the promoted cast of ${w.plan.id} does not hold: ${promo.problems.join('; ')}`);
  const cast = Cast.parse(JSON.parse(promo.castText!)), gen = expand(w.ctx(cast));
  const written = { ...gen.people, inputs: { ...gen.people.inputs, tools: await peopleHash() } };
  return { text: JSON.stringify(written, null, 1) + '\n', pool: [...cast.names.given, ...cast.names.family].map(n => n.name), plan: { id: w.plan.id, version: w.plan.version } };
}
