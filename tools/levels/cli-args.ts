// The command line of `bun run levels <objective> ...`: the words it knows. Anything else is an error, because a
// misspelt flag (--previw, --rounds=1) would otherwise start a live three-round run.
const VALUES = ['--plan', '--scope', '--seed', '--rounds', '--continue', '--max-usd', '--approved-by'] as const;
const FLAGS = ['--preview'] as const;

export type Args = { objective?: string; values: Map<string, string>; flags: Set<string> };

/** The words of the command line, or the first thing wrong with them. */
export function parseArgs(args: string[]): Args | { error: string } {
  const out: Args = { values: new Map(), flags: new Set() };
  for (let i = 0; i < args.length; i++) {
    const a = args[i]!;
    if (!a.startsWith('--')) {
      if (out.objective !== undefined) return { error: `unexpected argument ${JSON.stringify(a)}` };
      out.objective = a;
    } else if ((FLAGS as readonly string[]).includes(a)) out.flags.add(a);
    else if ((VALUES as readonly string[]).includes(a)) {
      const next = args[i + 1];
      if (next === undefined || next.startsWith('--')) {
        return { error: a === '--continue' ? '--continue needs a run id, e.g. --continue r003' : `${a} needs a value` };
      }
      out.values.set(a, next);
      i++;
    } else return { error: `unknown option ${a}; known: ${[...VALUES, ...FLAGS].join(' ')}` };
  }
  return out;
}

/** LEVELS_LOOP (tests only) names a rule version of tools/gen/rules.ts: 1 or 2. */
export function loopFrom(value: string | undefined): number | undefined | { error: string } {
  if (value === undefined || value === '') return undefined;
  if (value === '1' || value === '2') return Number(value);
  return { error: `LEVELS_LOOP must be 1 or 2, not ${JSON.stringify(value)}` };
}

/** `--max-usd`: a positive number of dollars. */
export function maxUsdFrom(value: string | undefined, fallback: number): number | { error: string } {
  if (value === undefined) return fallback;
  const n = Number(value);
  if (Number.isFinite(n) && n > 0) return n;
  return { error: `--max-usd must be a positive number, not ${JSON.stringify(value)}` };
}
