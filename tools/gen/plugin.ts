// The one interface between the generation engine (loop.ts, replay.ts) and an objective (brief 007 F). The engine owns rounds, calls,
// retrieval, faults, feedback, the run record and its offline replay. An objective brings its crew, its task texts, its apply step and
// its deterministic checks, and nothing else: transit is the first (tools/levels/transit.ts); skyline and missions follow.
import type { z } from 'zod';
import type { Objective } from '../rag/agent';
import type { Ledger } from './ledger';
import type { retrieve } from '../rag/retrieval';

/** A crew member. `key` names its files and call records (`<key>.json`), `owner` is how faults and hand-offs name it. */
export type Member = { key: string; owner: string; objective: Objective; schema: z.ZodType<any> };
export type Fault = { owner: string; rule: string; fault: string; fix: string; chunks: string[]; synthesized?: boolean };
/** What the critic (Keel) returns; the owner enum in its schema is the objective's own. */
export type Verdict = { verdict: 'pass' | 'fail'; summary: string; faults: Fault[]; sources: string[] };
/** A builder's output with the call that produced it: what citation checks and the critic's evidence block read. */
export type Built = { out?: { sources?: string[] }; call?: { retrieval?: { hits?: { key: string; section?: string; text?: string }[] } } };
/** `continue`: the id of a REJECTED run of the same subject to start from (continue.ts). `loop`: the rule version to run under, the current one unless a test needs the old (rules.ts). */
export type Opts = {
  input: string; seed?: string; rounds?: number; retriever: typeof retrieve; retrieval?: string; continue?: string; loop?: number;
  preview?: boolean; // no model call: each member answers with the objective's stand-in, so the prompts a real run would send can be read (preview.ts)
  // stop before a call whose estimate would take the run past this much; the run ends INCOMPLETE (ceiling.ts)
  maxUsd?: number;
  // who allowed a run that reaches the real model; recorded in run.json
  approvedBy?: string; live?: boolean;
};

/** The round in progress, as the hooks see it. `outs`: each builder's latest output (a reused builder keeps last round's). rd/od: this round's folder under runs/ and out/, odir: the run's folder under out/. */
export type Round = { n: number; version: string; outs: Record<string, any>; ran: string[]; skipped: string[]; rd: string; od: string; odir: string };

/** What an objective's deterministic checks made of the round's candidate. */
export type Assessment = {
  ok: boolean;                                              // every check is clean; ACCEPTED needs this and a passing critic and no faults
  line: string;                                             // printed as `round N <version>: <line>`
  record: Record<string, unknown>; more?: Record<string, unknown>; // round-record fields: before the verdict fields, and after citeFaults (recorded order kept)
  skipped?: string[];                                       // steps this round could not run (e.g. 'checker'); recorded with the builders skipped by `skip`
  failures: { rule: string; owners: string[]; fault: string; fix: string }[]; // failed checks: each owner hears one unless the critic already raised the rule
  faults: Fault[];                                          // synthesized faults that are always raised, in order
  criticTask(cited: string, lastFaults: Fault[]): string;  // the critic's task: the objective's evidence, the engine's block of what each builder retrieved and cites (`cited`), last round's faults
};
export type Seeds<S> = { names: readonly string[]; agent: Record<string, string>; apply(s: S, r: Round, name: string, out: any, rec: any): { out: any; note: string } };
export type { Ledger } from './ledger';
/** The code a verdict rests on (plan checker, apply step). Its hash is recorded per run; a changed hash is drift, labelled by cause in the ledger. */
export type Code = {
  label: string; hash(): Promise<string>; ledger(): Ledger; ledgerFile: string; declare: string;
  /** The code is expected to move between a run and its replays (the world generator): see replay-code.ts. */
  moves?: boolean;
  /** At the end of a run: writes the run's code state into the ledger (`recorded`), so a later replay knows it. */
  record?(hash: string, label: string): void;
};
export type Subject<S> = { id: string; version: string; pin(rdir: string): Promise<{ state: S; record: Record<string, unknown> }> }; // record: the pinned-input fields run.json carries

/** What a replay hook sees: the recorded run and a reporter. ok/warn/fail add a line; `drift` is a fail unless --allow-drift. */
export type Check = {
  dir: string; run: any; allow: boolean; lines: string[]; ok(m: string): void; warn(m: string): void; fail(m: string): void;
  drift(m: string): void; read(file: string): Promise<any>;
  /**
   * The code changed since the run and the objective's code moves: a rerun that differs is drift to be labelled
   * against the ledger, not a failure by itself.
   */
  rerunDrift: boolean;
};
export type Pins = { overlay?: Record<string, string>; [k: string]: any }; // overlay: recorded source text the corpus uses instead of the file on disk
/**
 * The candidate's checks were rerun: did a recorded rule change (`changed`: which; `failing`: which of those fail
 * now), which rules are new, and (when the objective can) the apply faults recomputed from the proposals.
 */
export type Rerun = {
  differs: boolean; added: string[]; changed?: string[]; failing?: string[]; assessFaults?: number;
};
export type Replay = {
  skipNote: string; inputVersion(run: any): string; superseded(run: any): boolean; // superseded: recorded before inputs were pinned, cannot be rerun
  checksOk(r: any): boolean;                               // did the deterministic checks pass in this recorded round (the field the objective's `record` wrote)
  pins: { raw(c: Check): Promise<Pins>; input(c: Check, p: Pins): Promise<Pins>; block(c: Check, p: Pins): Promise<Pins> }; // run in this order around the engine's GDD, corpus and code checks
  schema(run: any, key: string): z.ZodType<any>;           // the schema a recorded output of this member answers to
  candidate(c: Check, r: any, version: string, p: Pins): Promise<Rerun | undefined>; // recompute the round's candidate from the recorded proposals and rerun the checks
  seeded?(c: Check, r: any, call: any): Promise<void>;     // seed-specific recomputation (call: the seeded member's round-1 record)
};

export type Plugin<S = any> = {
  id: string; defaultInput: string;                         // id: run.json `objective` and the CLI word
  builders: Member[]; critic: Member;                       // builders run in this order; the critic is Keel
  header: Record<string, unknown>;                          // run.json fields replay needs to read this objective's runs (after `objective`)
  code: Code;
  open(input: string): Promise<Subject<S>>;
  task(s: S, r: Round, key: string): Promise<string>;       // a builder's task text (the engine adds hand-offs, its previous proposal and the critic's faults)
  skip?(s: S, r: Round, key: string): boolean;              // a builder that cannot run this round
  assess(s: S, r: Round): Promise<Assessment>;              // apply the proposals, run the checks
  ownersOf?(rule: string): string[] | undefined;            // who hears a rule; used when the critic names an invalid owner
  query?(s: S, r: Round, key: string): string[];            // retrieval queries for this builder or the critic, appended to its objective's (e.g. the sites a revision names)
  standIn?(s: S, key: string): unknown;                     // preview only: a valid, empty proposal for a builder, standing for what it would return
  seeds?: Seeds<S>;
  replay: Replay;
};
