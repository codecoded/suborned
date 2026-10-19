import type { Call } from '../rag/agent';
import type { BootstrapReport } from './bootstrap';
import type { FileState } from './files';
import type { ChangedFile } from './patch';
import type { Ranked, Scan } from './scan';
import type { Analysis, CheckResult, CodePatch, Review, Scope } from './schema';

export type RoundRecord = {
  index: number; selected: string; ranking: Ranked[]; patch: CodePatch;
  changes: ChangedFile[]; checks: CheckResult[]; review?: Review; scan?: Scan; analysis?: Analysis;
};
export type RunRecord = {
  version: 1; id: string; scopePath: string; scope: Scope; status: 'INCOMPLETE' | 'COMPLETE' | 'ERROR';
  approvedBy: string; startedAt: string; finishedAt?: string;
  limits: { maxUsd: number; maxIterations: number; maxCalls: number };
  bootstrap?: BootstrapReport; inputs: Record<string, string>; baseline: FileState;
  baselineChecks: CheckResult[]; rounds: RoundRecord[]; calls: Call[];
  finalScan?: Scan; finalAnalysis?: Analysis; ranking?: Ranked[]; error?: string; manualEdits: string[];
};
