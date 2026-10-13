// Replay: the checker reran on a round's candidate; what it says against the checks.json the run recorded. Shared by
// every plan objective. A rule added since the run is labelled drift; a changed result on a rule the run recorded is a
// FAIL, unless the objective's code moves and has changed since the run (c.rerunDrift): then the engine labels it
// against the drift ledger (tools/gen/replay-code.ts), or fails it when no cause is declared.
import type { Check as Rule } from '../plan/checks';
import type { Check, Rerun } from '../gen/plugin';

/** The fields of a round's record in run.json that the comparison reads. */
export type RoundRecord = { checkerOk: boolean; problems?: string[] };

/** The ids of the recorded rules whose result is not what the rerun gives now (a rule that is gone counts). */
export function changedRules(now: Rule[], recorded: Rule[]): string[] {
  const of = (list: Rule[], id: string) => JSON.stringify(list.filter(x => x.id === id));
  return [...new Set(recorded.map(x => x.id))].filter(id => of(now, id) !== of(recorded, id));
}

/** `now`: the rules as they rerun; `rec`: the round's checks.json; `r`: the round's record in run.json. */
export function compareChecks(
  c: Check, n: string, r: RoundRecord, now: Rule[], rec: { checks: Rule[] },
): Pick<Rerun, 'differs' | 'added' | 'changed'> {
  const had = new Set(rec.checks.map(x => x.id));
  const kept = now.filter(x => had.has(x.id)), fresh = now.filter(x => !had.has(x.id));
  const same = JSON.stringify(kept) === JSON.stringify(rec.checks), changed = same ? [] : changedRules(now, rec.checks);
  if (same) c.ok(`${n}: checker rerun matches checks.json (${kept.filter(x => x.ok).length}/${kept.length})`);
  else if (c.rerunDrift) {
    c.warn(`${n}: checker rerun differs from checks.json in ${changed.join(', ')} (the code changed since the run)`);
  }
  else c.fail(`${n}: checker rerun differs from checks.json`);
  if (fresh.length) {
    const names = [...new Set(fresh.map(x => x.id))].join(', ');
    const bad = [...new Set(fresh.filter(x => !x.ok).map(x => x.id))].join(', ');
    c.warn(`${n}: checker rules added since the run: ${names}${bad ? `; this plan would FAIL ${bad}` : ''}`);
  }
  // the record against itself: what the checks the run recorded say, whatever the code says now
  const clean = rec.checks.every(x => x.ok);
  if (r.checkerOk !== (clean && !(r.problems ?? []).length)) {
    c.fail(`${n}: run.json checkerOk=${r.checkerOk} but checks.json clean=${clean}`);
  }
  return { differs: !same, added: fresh.map(x => x.id), changed };
}
