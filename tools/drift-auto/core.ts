// drift:auto core (brief 034 item 2): pure functions over the lines `tools/levels/replay.ts` prints. Nothing here touches a file.
//
// A replay says one thing about the code a verdict rests on (tools/gen/replay-code.ts): the hash is the recorded one (ok), or it changed and
//   - every rule the run recorded reruns identically  -> labelled drift (no declaration needed for an objective whose code `moves`),
//   - some rule's result differs, a declared cause names it -> labelled drift ("rules whose result differs: ..."),
//   - otherwise FAIL "CHECKER CODE changed" with the reason in brackets.
// drift:auto declares only a change that moves no replay result. It decides that by comparing each run's replay at the base state (HEAD, with
// its own ledger) with the replay at the working tree under a provisional declaration: the code line reduced to the rules whose result differs,
// every other line with its hashes taken out. Anything that differs, or fails, is a refusal that prints the lines apart.

export type CodeKind = 'same' | 'labelled' | 'failed' | 'superseded' | 'none';
export type Finding = {
  kind: CodeKind;
  /** the FAIL names the ledger file: the current hash has no declared cause there */
  undeclared?: boolean;
  /** the FAIL says the run's own recorded hash is no state in the ledger (a record problem, not a declaration) */
  unknownRecorded?: boolean;
  /** rules whose result differs and that no declared cause names (FAIL) */
  uncovered: string[];
  /** rules whose result differs (labelled drift: a declared cause names each) */
  differs: string[];
  /** labelled drift that says every rule the run recorded reruns identically */
  identical: boolean;
  ledgerFile?: string;
};

const CODE = /CHECKER CODE changed/;

/** What a replay's lines say about the checker code. */
export function codeFinding(lines: string[]): Finding {
  const f: Finding = { kind: 'none', uncovered: [], differs: [], identical: false };
  if (lines.some(l => /^ok\s+checker code hash /.test(l))) f.kind = 'same';
  if (lines.some(l => /^warn\s+checker code changed since the run \(superseded run/.test(l))) f.kind = 'superseded';
  const l = lines.find(x => CODE.test(x));
  if (!l) return f;
  const failed = l.startsWith('FAIL');
  f.kind = failed ? 'failed' : 'labelled';
  const file = /not declared in ([^\s:\]]+)/.exec(l)?.[1] ?? /a state in ([^\s\]]+)\]/.exec(l)?.[1];
  if (file) f.ledgerFile = file;
  if (failed) {
    f.unknownRecorded = /recorded hash is not a state in/.test(l);
    f.undeclared = /current code hash not declared in/.test(l);
    const u = /no declared cause names them: ([^;\]]*)/.exec(l)?.[1];
    if (u) f.uncovered = u.split(',').map(x => x.trim()).filter(Boolean);
  } else {
    const d = /rules whose result differs: (.*?)\)?$/.exec(l)?.[1];
    if (d) f.differs = d.split(',').map(x => x.trim()).filter(Boolean);
    f.identical = /every rule the run recorded reruns identically/.test(l);
  }
  return f;
}

/** A replay's lines as they are compared across two code states: hashes out, the code line reduced to what it says about results. */
export function signature(lines: string[]): string[] {
  const strip = (s: string) => s.replace(/\b[0-9a-f]{12,64}\b/g, '#');
  return lines.filter(l => l.trim()).map(l => {
    if (!CODE.test(l)) return strip(l);
    const f = codeFinding([l]);
    if (f.kind === 'failed') return `CODE FAILED${f.unknownRecorded ? ' (recorded state unknown)' : ''}${f.uncovered.length ? ` uncovered: ${[...f.uncovered].sort().join(', ')}` : ''}${f.undeclared ? ' (undeclared)' : ''}`;
    const added = /checker rules added since the run: ([^;)]*)/.exec(l)?.[1];
    return `CODE ${f.differs.length ? `differs: ${[...f.differs].sort().join(', ')}` : 'identical'}${added ? `; added: ${added}` : ''}`;
  });
}

export type SigDiff = { onlyBase: string[]; onlyNow: string[] };
/** The lines each side has that the other does not (multiset difference, order kept). */
export function diffSignatures(base: string[], now: string[]): SigDiff {
  const take = (from: string[], other: string[]) => { const pool = new Map<string, number>(); for (const l of other) pool.set(l, (pool.get(l) ?? 0) + 1); return from.filter(l => { const n = pool.get(l) ?? 0; if (n) { pool.set(l, n - 1); return false; } return true; }); };
  return { onlyBase: take(base, now), onlyNow: take(now, base) };
}

export type RunReplay = { id: string; objective: string; lines: string[]; ok: boolean };
export type RunVerdict = { id: string; objective: string; moved: boolean; reason?: string; diff?: SigDiff; failLines?: string[] };

/**
 * Whether the working tree's replay of a run (under the provisional declaration) is the base state's: no failing line, and the signatures agree.
 * `base` undefined: the run does not exist at the base (a new run), so only "does it pass" can be asked.
 */
export function judgeRun(now: RunReplay, base: RunReplay | undefined): RunVerdict {
  const failLines = now.lines.filter(l => l.startsWith('FAIL'));
  if (failLines.length || !now.ok) return { id: now.id, objective: now.objective, moved: true, reason: 'still fails with the declaration in place', failLines };
  if (!base) return { id: now.id, objective: now.objective, moved: false, reason: 'not at the base (a new run): passes' };
  const diff = diffSignatures(signature(base.lines), signature(now.lines));
  if (diff.onlyBase.length || diff.onlyNow.length) return { id: now.id, objective: now.objective, moved: true, reason: 'its replay differs from the base state\'s', diff };
  return { id: now.id, objective: now.objective, moved: false };
}

/** The cause words: the files behind the change, named. */
export function causeText(objective: string, baseLabel: string, files: string[], runs: number): string {
  const shown = files.slice(0, 8).join(', '), more = files.length > 8 ? ` and ${files.length - 8} more` : '';
  const what = files.length ? `${files.length} file${files.length === 1 ? '' : 's'} changed since ${baseLabel}: ${shown}${more}` : `no listed file differs from ${baseLabel} (the ledger lacked this state)`;
  return `drift:auto ${objective}: ${what}; none of the ${runs} recorded ${objective} run${runs === 1 ? '' : 's'} replays differently from ${baseLabel}`;
}
