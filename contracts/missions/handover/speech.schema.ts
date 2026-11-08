import type { Entry } from './text.schema.ts';

/** Brief 102 (Sky 2026-11-08: "Maybe even drop them? They will feel repetitive"): the agents go quiet in play, and the HUD carries the news. A `silent` row starts with this. */
export const SILENT = 'silent: the agents go quiet in play, Sky 2026-11-08';
/** An agent's row speaks only in a scene with people: the Fixer's opener, last word and telephone in the approaches and the call, the Enforcer's words to Popovic as he
 *  takes him, the Fixer's answer to Popovic in the car (lineKey order). Every other agent row is `silent`, with its reason; nobody else's row is (speech.test.ts). */
export const SCENE_ROWS = ['arrive/ask/fixer', 'arrive/pay/fixer', 'arrive/help/fixer', 'arrive/pressure/fixer', 'done/turn/fixer/turned', 'done/turn/fixer/refused',
  'done/turn/fixer/not-yet', 'done/turn/fixer/kept', 'done/turn/fixer/suspicious', 'done/turn/fixer/small-offer', 'ack/call/fixer', 'done/take/enforcer', 'done/enter/enforcer',
  'cliffhanger/fixer/unnamed', 'cliffhanger/fixer/named'] as const;
/** A row the host plays: not parked, not waiting on Sky, not silent. */
export const speaks = (e: Pick<Entry, 'parked' | 'pending' | 'silent'>): boolean => !e.parked && !e.pending && !e.silent;
