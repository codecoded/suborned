// Brief 060 (Suture): the shapes the QA checks read. A play is the line `suborned-sim play <script> --json` prints
// (sim/Suborned.Sim/Script.cs Report.Json) plus the QA host's read-only `qa` block (tools/qa/host/QaHost.cs).
import type { Order } from '../missions/check/engine';

export type { Order };
export type Script = { schema: 'suborned.sim.script/1'; mission: 'handover'; note?: string; start: Record<string, unknown>; orders: Order[]; until?: number; expect?: { outcome?: string; engine?: string } & Record<string, unknown> };

export type LogLine = { tick: number; code: string; text: string };
export type Blamed = { id: string; kind: string; role: string; x?: number; y?: number; level?: number };
export type Cause = { fail: string; why: string; tick: number; clock: string; single: boolean; culprits: Blamed[]; ops: string[]; origin?: { tick: number; report: string; because: string; reporter?: Blamed } };
export type Debrief = { publicSecurity?: string; loud?: boolean; route?: string[]; chain?: string[]; assets?: string[]; extraInterventions?: string[]; [k: string]: unknown };
export type Stuck = { op: string; kind: 'walk' | 'act' | 'busy-overdue'; order: string; from: number; to: number; x: number; y: number };
export type QaBlock = {
  record: string; debrief: Debrief | null; debriefError?: string;
  stuck: Stuck[]; vale: { tick: number; state: string }[]; seen: { id: string; tick: number; x: number; y: number }[]; lockdown: { tick: number; at: number }[];
};
export type Play = {
  id: string; error?: string;
  outcome: 'success' | 'failure' | 'withdrawn' | 'none'; fail: string; tick: number; cash: number; trail: number; alert: number; gunfire: number; standing: number;
  startHash: string; finalHash: string; replayIdentical: boolean | null; cause?: Cause;
  checkpoints: { tick: number; hash: string }[];
  orders: { order: Order; ok: boolean; code?: string; why?: string }[];
  ledger: { tick: number; what: string; delta: number; now: number; cause: string }[];
  log: LogLine[]; debrief?: Debrief; qa: QaBlock;
};

/** Where a script came from: a route (Waymark's), a saved session (Sky's), or a mutant of one. */
export type SeedKind = 'route' | 'session';
export type Seed = { id: string; kind: SeedKind; file: string; script: Script; expects?: { outcome?: string; alert?: number }; routeKind?: string };
export type Family = 'shift' | 'drop' | 'repeat' | 'reassign' | 'swap-target' | 'insert';
export const FAMILIES: Family[] = ['shift', 'drop', 'repeat', 'reassign', 'swap-target', 'insert'];
export type Mutation = { family: Family; index: number; what: string };
/** One script to play: a seed as it is, or a mutant (one or two mutations). `twin` marks the second copy of a seed played for Q-DET. */
export type Item = { id: string; seed: Seed; mutations: Mutation[]; script: Script; twin?: boolean };

export type Severity = 'S1' | 'S2' | 'S3' | 'S4' | 'info';
/** One finding of one check on one play. `key` groups findings into one issue (the same fault in many plays). */
export type Finding = { check: string; severity: Severity; key: string; title: string; expected: string; observed: string; tick?: number; owner: string; /** the bot lane (tier 2): its shot (JPEG) and replay script, absolute */ shot?: string; script?: string };
export type Ctx = { text: TextTables; rules: QaRules };
export type TextTables = { fails: Set<string>; endFails: Set<string>; failcam: Record<string, Record<string, string>>; beats: Set<string> };
export type QaRules = { warnLeadS: number; stuckS: number; valeStuckS: number };
export const DEFAULT_RULES: QaRules = { warnLeadS: 30, stuckS: 10, valeStuckS: 10 }; // [S] starting values, brief 060 §2
