import { z } from 'zod';
import type { Read } from './guide';
import { BARK_13, calibration13, scene13, TASKS_13, type GuideNatural } from './short-natural';
import type { ShortRule } from './short-types';

/** Guide /14 (brief 102's sweep; Sky 2026-11-08, on the four lines the /13 bar kept on purpose: "not into those e.g. 'Citizen. Walking is encouraged. Running is noted.' -
 *  that noted is AI sounding"): /13, with AI-sounding words failing NATURAL whoever says them, Sky's four rejected lines among the failing ones (none of them passes now),
 *  the Mislead as a scene with a person (the Scout's tip to the rival lookout, Aster's default), and Sky's approved rewrites kept out of the loop (rows marked `sky`). */
export const AI_WORDS_FILE = 'briefs/102/fable/ai-words.json';
const RowKey = z.string().regex(/^[a-z-]+(\/[a-z0-9-]+)+$/), Said = z.string().min(1), Why = z.string().min(1);
export const AiWords = z.object({ schema: z.literal('suborned.ai-words/1'), status: z.string().min(1), why: z.string().min(1),
  words: z.array(z.string().min(1)).min(6), kind: z.array(z.string().min(1)).min(1),
  rejected: z.array(z.object({ where: z.string().regex(/^(conv:[a-z0-9-]+|[a-z-]+(\/[a-z0-9-]+)+)$/), line: Said, why: Why }).strict()).length(4),
  fail: z.array(z.object({ key: RowKey, line: Said, why: Why }).strict()).min(3) }).strict();
export type AiWords = z.infer<typeof AiWords>;
/** What a /14 guide carries of it: the words, Sky's four, and today's lines with such a word. */
export const GuideAi = AiWords.pick({ words: true, kind: true, rejected: true, fail: true });
export type GuideAi = z.infer<typeof GuideAi>;
export const readAiWords = (read: Read) => AiWords.parse(JSON.parse(read(AI_WORDS_FILE)));
/** /14's bar: /13's, less the passing lines Sky turned down ("I remember them, officer." was a pass at /13). */
export const natural14 = (bar: GuideNatural, ai: GuideAi): GuideNatural =>
  ({ kept: bar.kept, fail: bar.fail, pass: bar.pass.filter(p => !ai.rejected.some(r => r.where === p.key && r.line === p.line)) });

const esc = (s: string) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
/** The code check's words: Sky's and their kind, as whole words, any case. */
export const aiWords = (ai: Pick<GuideAi, 'words' | 'kind'>) => new RegExp(`\\b(?:${[...ai.words, ...ai.kind].map(esc).join('|')})\\b`, 'i');
const quoted = (xs: string[]) => xs.map(x => `"${x}"`).join(', ');
const AI_RULE = (ai: GuideAi) => ` And no AI-sounding words (Sky 2026-11-08, on the patrol's "Citizen. Walking is encouraged. Running is noted.": "that noted is AI sounding"): ${quoted(ai.words)} and their kind (${quoted(ai.kind)}) are a computer's or a form's words, and no person in this world says them: not the guard, not the police on the radio, not the person at a camera screen, and not the clockwork patrol either, which is a 1950s machine speaking the city's polite regulations like a recorded announcement ("Walking is encouraged, citizen."), never a computer logging what it saw. A line with one fails NATURAL outright, however true and short.`;

/** Guide /14's bark rubric: /13's, NATURAL with the AI-sounding words. */
export function BARK_14(words: number, chars: number, named: string, cell: string[], world: string, opening: string, brief: string, bar: GuideNatural, ai: GuideAi): ShortRule[] {
  return BARK_13(words, chars, named, cell, world, opening, brief, bar).map(r => r.id !== 'NATURAL' ? r : { ...r, sources: [...r.sources, 'ai-words'], instruction: r.instruction + AI_RULE(ai) });
}
const listing = (xs: { key?: string; where?: string; line: string; why: string }[]) => xs.map(x => `- ${x.key ?? x.where}: ${JSON.stringify(x.line)} ${x.why}`).join('\n');
/** The calibration at /14: /13's (without the passes Sky turned down), then the AI-sounding lines and Sky's four. */
export const calibration14 = (bar: GuideNatural, ai: GuideAi) => [calibration13(bar),
  `These fail too: AI-sounding words (Sky 2026-11-08: "that noted is AI sounding"; ${quoted(ai.words)} and their kind). Lines in today's text.json:\n${listing(ai.fail)}`,
  `Sky turned down these four, which the last bar kept on purpose ("none stays as it is"): they fail, and their rows are rewritten:\n${listing(ai.rejected)}`,
].join('\n\n');
const NO_AI = ' No AI-sounding word (noted, understood, proceeding, acknowledged, affirmative, rest assured, or their kind) in anyone\'s mouth, the patrol\'s and the police\'s too.';
export const TASKS_14 = {
  generate: TASKS_13.generate + NO_AI,
  evaluate: `${TASKS_13.evaluate} Any AI-sounding word (noted, understood, proceeding, acknowledged, affirmative, rest assured, and their kind: copy that, confirmed, logged, standing by) fails NATURAL outright, whoever says it, the clockwork patrol and the police on the radio too. Lines the family's context gives as Sky's own words are not items: never score them, and fail any line that repeats one.`,
  refine: TASKS_13.refine + NO_AI,
};
/** Guide /15 (brief 102's sweep, Fable 2026-11-08): /14, and a score below 10 says what holds it back. Opus scored a whole family 9 on every rule, MECHANICS and SHORT
 *  included, with no finding, four rounds running ("I'd ship it as written … none has the bite of Sky's standard lines"): the writer had nothing to revise, and the run
 *  could not end but on the revision limit. Nothing else moves, so a /15 run may continue a /14 run of the same family. */
export const TASKS_15 = { ...TASKS_14,
  evaluate: `${TASKS_14.evaluate} Every rule you score below 10 needs at least one finding: the line that holds it back, and what would lift it. If you would ship every line unchanged and want to hear it in play, every rule is 10; a score below 10 with no finding gives the writer nothing to revise.` };

// ---------------------------------------------------------------- the Mislead, a scene with a person (Aster's default, brief 102)

type Row = { event: string; verb?: string; speaker: string; outcome?: string };
type Family = { id: string; title: string; pick: (r: Row) => boolean };
const TIP = (r: Row) => r.event === 'arrive' && r.verb === 'mislead' && r.speaker === 'scout';
export const MISLEAD_14: Family = { id: 'bark/mislead', title: 'The Mislead: the Scout\'s tip to the rival lookout, and his answer', pick: r => TIP(r) || r.event === 'rivals-misled' };
/** /14 moves the rival's answer from the rival crew's family into the Mislead's. */
export const family14 = <R extends Row>(f: { id: string; title: string; pick: (r: R) => boolean }) => f.id === 'bark/rivals' ? { ...f, pick: (r: R) => f.pick(r) && r.event !== 'rivals-misled' } : f;
/** The cell's scene lines at /14: /13's, and the Scout's tip. */
export function scene14(r: Row, does: string): { tells: string; where: string } | null {
  if (TIP(r)) return { tells: 'Say: the Scout\'s tip to the rival lookout, face to face, as she comes up to him at his post watching Popovic\'s car: a lie, told as a friendly stranger in the street, that sends him off to look somewhere else. He answers next and walks off to watch the Office\'s public door; so the tip is about that door, for instance the Councillor coming out by the front today, or someone asking for him there. Said to him, quick and warm: she never says who she is, and never names the cell or his crew. Facts, never said: he comes back to his post after a while, and knows her face.', where: 'The Mislead\'s cutaway, face to face, before his answer (a scene with a person, brief 102), then the Journal.' };
  return scene13(r, does);
}
/** The rival's answer at /14: to the Scout's face, in the Mislead's cutaway. */
export const ANSWER_14 = { tells: 'Say: the rival lookout\'s answer to the stranger\'s tip, said to her: he takes it and goes to look. Cold and businesslike, a man on a job: no names, no thanks he doesn\'t mean. Facts, never said: he walks off to watch the Office\'s public door, then back to his post, and he knows her face.', where: 'The Mislead\'s cutaway, after the Scout\'s tip, then a bubble as he walks off, then the Journal.' };
