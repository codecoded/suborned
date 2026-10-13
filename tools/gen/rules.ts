// The loop's rule version. A run records it in run.json as `loop`, so replay knows which rule it was made under; a run without the field was made under 1.
//   1  The ten A04 runs: Keel judges every round's candidate, also one the deterministic checks had already failed (11 such candidates in those runs).
//   2  (2026-10-14, brief 007 F) Keel judges only a candidate the deterministic checks pass: the objective's checks, its apply problems, the citation
//      check and the hand-off check. A candidate that fails any of them goes straight back to its owners with those faults; no critic call is made.
//      A round without a critic call still counts toward the 3-round cap. The cap bounds cost, and the builders' calls of that round were paid for;
//      to go on after a rejected run, continue it (tools/gen/continue.ts), which starts a new run with its own 3 rounds.
export const LOOP = 2;
export const loopOf = (run: { loop?: number }): number => run.loop ?? 1;

/** What the gate reads of a round: the same four facts when the run is made and when it is replayed from the record. */
export type Gate = { checksOk: boolean; assessFaults: number; citeFaults: number; handoffFaults: number };
/** Does Keel judge this round's candidate? */
export const criticRuns = (loop: number, g: Gate): boolean => loop < 2 || (g.checksOk && !g.assessFaults && !g.citeFaults && !g.handoffFaults);

/**
 * A critic that fails the candidate and lists no fault has still said something: its summary. Without a fault no builder
 * is told, the next round reruns blind and the run spends its rounds on the same proposal. Every builder gets the summary
 * as one fault (the critic did not say whose it is). Loop 2.
 */
export const silentFaults = (builders: { owner: string }[], summary: string) => builders.map(b => ({
  owner: b.owner, rule: 'critic', chunks: [] as string[], synthesized: true as const,
  fault: summary.trim() || 'the critic failed the candidate and gave no reason',
  fix: 'address what the critic wrote; the critic named no single fault',
}));
