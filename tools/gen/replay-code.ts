// Replay: what to say when the code a verdict rests on has changed since the run (brief 007 F). Drift by cause, never a
// silent pass:
//   - transit's code (plan checker, apply) changes rarely and on purpose: the change must be declared in the ledger
//     (`code.declare "<cause>"`), and the run's own code state must be recorded there.
//   - the world generator (`code.moves`) changes every day. A change that leaves every rule the run recorded rerunning
//     identically is labelled drift with no declaration. A rule whose result differs needs a declared cause for the
//     current code, and a recorded state for the run's; with neither it is a failure.
import type { Code } from './plugin';

export type CodeFacts = {
  run: { toolsHash?: string }; tools: string; code: Code; allowDrift: boolean;
  /** Rounds whose checks were rerun, and the rules their rerun found differing or new. */
  reran: number; rerunDiffers: boolean; added: Set<string>; changed: Set<string>;
};

/** The line that replaces the placeholder left at the code-hash check, and whether it is a failure. */
export function codeLine(f: CodeFacts): { line: string; failed: boolean } {
  const { run, tools, code } = f, recorded = String(run.toolsHash);
  const message = `CHECKER CODE changed since the run (${code.label}): recorded ${recorded.slice(0, 12)}, `
    + `now ${tools.slice(0, 12)}; the record itself may be intact`;
  const ledger = code.ledger(), head = message.split(';')[0];
  const known = Object.keys(ledger.recorded).some(h => recorded.startsWith(h));
  const causes = ledger.declared[tools] ?? [];
  const rules = f.added.size ? [`checker rules added since the run: ${[...f.added].join(', ')}`] : [];
  const labelled = (text: string) => ({ line: `warn  ${head} (labelled drift: ${text})`, failed: false });
  if (!f.rerunDiffers && f.reran && (known && causes.length || code.moves)) {
    const reasons = causes.length ? causes : ['code changed since the run'];
    const text = [...rules, ...reasons, 'every rule the run recorded reruns identically'];
    return labelled(text.join('; '));
  }
  if (code.moves && f.rerunDiffers && known && causes.length) {
    return labelled([...rules, ...causes, `rules whose result differs: ${[...f.changed].join(', ')}`].join('; '));
  }
  if (f.allowDrift) return { line: `warn  ${message} (drift allowed)`, failed: false };
  const why = !known ? ` [recorded hash is not a state in ${code.ledgerFile}]`
    : !causes.length ? ` [current code hash not declared in ${code.ledgerFile}: ${code.declare} "<cause>"]` : '';
  return { line: `FAIL  ${message}${why}`, failed: true };
}
