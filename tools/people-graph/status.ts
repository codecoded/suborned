// Whether the graph of the current promoted cast is loaded: one line for `bun run doctor`, never a failure (the graph is a derived view; nothing a run needs).
import { loadedRecord } from './load';
import { prepare } from './source';

export type GraphLine = { level: 'PASS' | 'WARN'; msg: string; fix?: string };

export async function promotedGraphLine(): Promise<GraphLine> {
  try {
    const p = await prepare();
    if (p.source.problems.length) return { level: 'WARN', msg: `the promoted cast cannot be expanded cleanly (${p.source.problems[0]})`, fix: 'bun run verify' };
    if (await loadedRecord(p.db, p.hash)) return { level: 'PASS', msg: `loaded for the promoted cast (${p.people.persons.length} people, ${p.hash.slice(0, 12)}, database ${p.db})` };
    return { level: 'WARN', msg: `not loaded for the promoted cast (${p.hash.slice(0, 12)}); optional, a derived view`, fix: 'bun run people:graph' };
  } catch (e) { return { level: 'WARN', msg: `could not tell: ${String((e as Error).message).slice(0, 80)}`, fix: 'bun run people:graph' }; }
}
