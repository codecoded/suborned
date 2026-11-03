// The front door of a tools CLI (Splice, 2026-10-25): `--help`, `-h` or any flag the CLI does not know prints its usage and exits 0 before
// the CLI does anything (a batch Unity build, a kit copy). Two agents ran `--help` and got the real work.
export interface FlagSpec {
  /** Flags that take no value. */
  flags: string[];
  /** Flags that take a value: the next argument when it does not start with `-` (required for `required`, otherwise optional). */
  valued?: { flag: string; required?: boolean }[];
}
export type FlagVerdict = { run: true } | { run: false; reason: 'help' | 'unknown'; unknown?: string };

/** Whether the CLI should go on. `help` for --help or -h anywhere; `unknown` for the first flag or stray word that is not in the spec. */
export function checkFlags(args: string[], spec: FlagSpec): FlagVerdict {
  if (args.includes('--help') || args.includes('-h')) return { run: false, reason: 'help' };
  const valued = new Map((spec.valued ?? []).map(v => [v.flag, !!v.required]));
  for (let i = 0; i < args.length; i++) {
    const a = args[i];
    if (spec.flags.includes(a)) continue;
    if (valued.has(a)) {
      const next = args[i + 1];
      if (next !== undefined && !next.startsWith('-')) i++;
      else if (valued.get(a)) return { run: false, reason: 'unknown', unknown: `${a} needs a value` };
      continue;
    }
    return { run: false, reason: 'unknown', unknown: a };
  }
  return { run: true };
}

/** At the top of a CLI: prints the usage and exits 0 when the arguments are not a run. */
export function usageGate(args: string[], spec: FlagSpec, usage: string, name: string): void {
  const v = checkFlags(args, spec);
  if (v.run) return;
  if (v.reason === 'unknown') console.error(`${name}: unknown option ${v.unknown}; nothing was done`);
  console.log(usage);
  process.exit(0);
}
