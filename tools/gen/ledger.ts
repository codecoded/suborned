// The drift ledger of an objective's code: which code states recorded runs were made under, and the causes a person
// declared for the state the code is in now. A cause names the rules it covers: replay accepts a changed result for a
// rule a declared cause names, and for no other (brief 007 F retest, N1).
//   recorded: first 16 hex digits of a code hash → the runs made under it
//   declared: full code hash → its causes. A cause is `{ cause, rules }`; a bare string is a cause written before
//             rules were named, and covers none.

export type Cause = { cause: string; rules: string[] };
export type Ledger = { recorded: Record<string, string>; declared: Record<string, (string | Cause)[]> };

/** What a rule id looks like: B-1, SF-13, S-C1, W-1h. */
export const RULE_ID = /^[A-Z]+-[A-Z]?\d+[a-z]?$/;

const normal = (c: string | Cause): Cause => (typeof c === 'string' ? { cause: c, rules: [] } : c);

/** The causes declared for the code hash `hash`, bare strings read as causes that cover no rule. */
export const declaredFor = (ledger: Ledger, hash: string): Cause[] => (ledger.declared[hash] ?? []).map(normal);

/** The rules a set of causes covers. */
export const covered = (causes: Cause[]): Set<string> => new Set(causes.flatMap(c => c.rules));

/** `--rules SK-1,F-7`, or `none` for a change that moves no recorded rule: the rules, or the problem with the list. */
export function parseRules(list: string | undefined): string[] | { error: string } {
  if (list === undefined || list.trim() === '') {
    return { error: '--rules is required: the rule ids the cause covers, or none' };
  }
  if (list.trim() === 'none') return [];
  const rules = list.split(',').map(r => r.trim());
  const bad = rules.filter(r => !RULE_ID.test(r));
  if (bad.length) return { error: `not rule ids: ${bad.join(', ')} (expected e.g. SK-1,F-7,S-C1)` };
  return [...new Set(rules)];
}

/** The cause added to the ones declared for `hash`; a cause declared again names the union of its rules. */
export function withCause(ledger: Ledger, hash: string, cause: string, rules: string[]): Cause[] {
  const list = declaredFor(ledger, hash), same = list.find(c => c.cause === cause);
  if (same) same.rules = [...new Set([...same.rules, ...rules])];
  else list.push({ cause, rules });
  ledger.declared[hash] = list;
  return list;
}

/** `SK-1, F-7` for a cause that covers rules; `no rule` for one that covers none. */
export const rulesText = (c: Cause): string => (c.rules.length ? c.rules.join(', ') : 'no rule');
