import { z } from 'zod';

export const CheckSchema = z.object({ id: z.string().regex(/^[a-z][a-z0-9-]*$/), filter: z.string().min(1), minTests: z.number().int().positive() }).strict();
export type CheckSpec = z.infer<typeof CheckSchema>;
export const SourceSchema = z.object({ path: z.string().min(1), quote: z.string().min(12) }).strict();
export const ScopeSchema = z.object({
  id: z.string().regex(/^[a-z][a-z0-9-]*$/), goal: z.string().min(1),
  sources: z.array(SourceSchema).min(1),
  requirements: z.array(z.object({
    id: z.string().regex(/^[a-z][a-z0-9-]*$/), title: z.string().min(1), source: z.number().int().nonnegative(),
    owner: z.enum(['simulation-engineer', 'tools-engineer']), dependsOn: z.array(z.string()), checks: z.array(z.string()).min(1),
  }).strict()).min(1),
  context: z.array(z.object({ path: z.string(), from: z.number().int().positive().optional(), to: z.number().int().positive().optional() }).strict()).min(1),
  writable: z.array(z.string()).min(1), testProject: z.string().min(1),
  checks: z.array(CheckSchema).min(1), regression: z.string(),
}).strict();
export type Scope = z.infer<typeof ScopeSchema>;

export const AnalysisSchema = z.object({
  summary: z.string(), features: z.array(z.object({
    id: z.string(), state: z.enum(['satisfied', 'missing', 'partial', 'blocked', 'unknown']),
    explanation: z.string().min(1),
    evidence: z.array(z.object({ path: z.string(), quote: z.string().min(8) }).strict()).min(1),
    value: z.number().int().min(1).max(5), effort: z.number().int().min(1).max(5), risk: z.number().int().min(1).max(5),
    reason: z.string().min(1),
  }).strict()),
}).strict();
export type Analysis = z.infer<typeof AnalysisSchema>;
export const PatchSchema = z.object({
  summary: z.string().min(1),
  edits: z.array(z.object({ path: z.string(), search: z.string(), replacement: z.string() }).strict()).min(1).max(20),
}).strict();
export type CodePatch = z.infer<typeof PatchSchema>;
export const ReviewSchema = z.object({
  verdict: z.enum(['pass', 'fail']), summary: z.string().min(1),
  faults: z.array(z.object({ path: z.string(), problem: z.string(), fix: z.string() }).strict()),
}).strict();
export type Review = z.infer<typeof ReviewSchema>;

export type CheckResult = {
  id: string; argv: string[]; exitCode: number; timedOut: boolean;
  total: number; passed: number; failed: number; ok: boolean;
  outputFile: string; reportFile: string; outputHash: string; seconds: number;
};
