import { z } from 'zod';
import type { Entry } from './text.schema.ts';

/** Sky's own words on a row (brief 102, Sky 2026-11-08 on Fable's sample: "yes - rewrites are much better"): `apply` never replaces them and the loop doesn't rewrite them, as "Turn driver by". */
export const SKY_WORDS = 'Sky\'s words, 2026-11-08';
export const SkyWords = z.string().startsWith(SKY_WORDS).optional().describe('brief 102: the row\'s lines are Sky\'s approved words; no run of the style loop replaces them');

/** Brief 102 (Sky 2026-11-08: "Maybe even drop them? They will feel repetitive"): the agents go quiet in play, and the HUD carries the news. A `silent` row starts with this. */
export const SILENT = 'silent: the agents go quiet in play, Sky 2026-11-08';
/** An agent's row speaks only in a scene with people: the Fixer's opener, last word and telephone in the approaches and the call, the Enforcer's words to Popovic as he
 *  takes him, the Fixer's answer to Popovic in the car, and the Scout's false tip to the rival in a Mislead (Aster's default, brief 102) (lineKey order). Every other agent row is `silent`, with its reason; nobody else's row is (speech.test.ts). */
export const SCENE_ROWS = ['arrive/ask/fixer', 'arrive/pay/fixer', 'arrive/help/fixer', 'arrive/pressure/fixer', 'arrive/mislead/scout', 'done/turn/fixer/turned', 'done/turn/fixer/refused',
  'done/turn/fixer/not-yet', 'done/turn/fixer/kept', 'done/turn/fixer/suspicious', 'done/turn/fixer/small-offer', 'ack/call/fixer', 'done/take/enforcer', 'done/enter/enforcer',
  'cliffhanger/fixer/unnamed', 'cliffhanger/fixer/named'] as const;
/** A row the host plays: not parked, not waiting on Sky, not silent. */
export const speaks = (e: Pick<Entry, 'parked' | 'pending' | 'silent'>): boolean => !e.parked && !e.pending && !e.silent;
