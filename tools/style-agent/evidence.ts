import { existsSync, mkdirSync, readdirSync, readFileSync, renameSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { isDeepStrictEqual } from 'node:util';
import { z } from 'zod';
import { interpretCodex, tokenUsage } from '../goals/codex';
import { briefsFromFiles } from './briefs';
import { fingerprintOnly, guideFromFiles, hash } from './guide';
import { corrected, runLoop } from './loop';
import { ResultSchema, type Evidence, type Guide, type Result } from './types';

export const RUNS = 'runs/style-agent';
/** Source snapshots are stored once by content hash, zstd-compressed, and shared by every run. */
export const STORE = `${RUNS}/sources`;
/** A run is one zstd-compressed JSON file: every prompt, transcript, score and revision. Read it with `bun run style:replay`. */
export const RUN_FILE = 'run.json.zst';
const pack = (text: string) => Bun.zstdCompressSync(Buffer.from(text), { level: 19 });
const unpack = (path: string) => Bun.zstdDecompressSync(readFileSync(path)).toString('utf8');
export const readRun = (path: string): unknown => JSON.parse(path.endsWith('.zst') ? unpack(path) : readFileSync(path, 'utf8'));
export function writeRun(dir: string, r: Result) {
  writeFileSync(resolve(dir, `${RUN_FILE}.tmp`), pack(JSON.stringify(r)));
  renameSync(resolve(dir, `${RUN_FILE}.tmp`), resolve(dir, RUN_FILE));
}
export const ROLE_FILES = ['.codex/agents/dialogue-designer.toml', '.codex/agents/systems-designer.toml'];
const normalized = (value: unknown) => JSON.parse(JSON.stringify(value));
export const equal = (a: unknown, b: unknown) => isDeepStrictEqual(normalized(a), normalized(b));
function requireThat(condition: unknown, message: string): asserts condition { if (!condition) throw new Error(message); }

/** Hash the tooling that produced a run. A record only: the replay below is the check, since any change that alters a prompt, check or score fails it. */
export function toolingManifest(base: string) {
  const files = readdirSync(resolve(base, 'tools/style-agent')).filter(f => f.endsWith('.ts') && !f.endsWith('.test.ts')).map(f => `tools/style-agent/${f}`);
  files.push('tools/goals/codex.ts', 'tools/rag/claude.ts', 'tools/rag/config.ts', 'tools/rag/gdd-sources.ts', 'package.json', 'bun.lock');
  return files.sort().map(file => ({ file, sha256: hash(readFileSync(resolve(base, file), 'utf8')) }));
}

/** Snapshot every source the guide and briefs read, plus the role cards, into the shared store. */
export function archiveEvidence(base: string, guide: Guide, store = resolve(base, STORE)): Evidence {
  mkdirSync(store, { recursive: true });
  const sources = [...new Set([...guide.sources.map(s => s.file), ...ROLE_FILES])].sort().map(file => {
    const text = readFileSync(resolve(base, file), 'utf8'), sha256 = hash(text), path = resolve(store, `${sha256}.zst`);
    if (!fingerprintOnly(file) && !existsSync(path)) writeFileSync(path, pack(text));
    return { file, sha256 };
  });
  for (const s of guide.sources) requireThat(sources.find(f => f.file === s.file)?.sha256 === s.sha256, `Source changed while archiving: ${s.file}`);
  const tooling = toolingManifest(base);
  return { sources, tooling, toolingSha256: hash(JSON.stringify(tooling)) };
}
export function archivedFiles(evidence: Evidence, store: string): Record<string, string> {
  requireThat(new Set(evidence.sources.map(s => s.file)).size === evidence.sources.length, 'Duplicate source paths');
  return Object.fromEntries(evidence.sources.filter(s => !fingerprintOnly(s.file)).map(s => {
    const path = resolve(store, `${s.sha256}.zst`);
    requireThat(existsSync(path), `Missing archived source ${s.file}`);
    const text = unpack(path);
    requireThat(hash(text) === s.sha256, `Archived source hash mismatch: ${s.file}`);
    return [s.file, text];
  }));
}

function verifyProvider(r: Result, files: Record<string, string>) {
  requireThat(r.authorization, 'Missing live approval');
  for (const call of r.calls) {
    // A call that failed before the adapter ran has no transcript and cannot lead to acceptance.
    if (call.raw === undefined && call.output === undefined && call.error && r.status === 'ERROR') continue;
    const raw = call.raw as Record<string, any> | undefined;
    requireThat(raw && typeof raw.stdout === 'string' && Number.isInteger(raw.exitCode), 'Missing Codex transcript');
    const source = `.codex/agents/${call.role}.toml`, card = files[source];
    requireThat(card && raw.role === call.role && raw.roleCard?.source === source && raw.roleCard?.sha256 === hash(card), 'Role card differs from the archived card');
    requireThat(raw.requestedModel === r.authorization.model && raw.approvedBy === r.authorization.approvedBy, 'Approval metadata mismatch');
    const parsed = interpretCodex(raw.stdout, raw.exitCode, r.authorization.model, hash(call.prompt));
    let tokens: number | null = null;
    try { tokens = tokenUsage(parsed.usage); } catch { /* unknown usage stays unknown */ }
    const output = parsed.ok ? parsed.output : { providerError: parsed.error ?? 'Model call failed' };
    requireThat(equal(output, call.output) && tokens === call.tokens, 'Transcript output or usage differs from the recorded call');
    if (parsed.ok) {
      const audit = JSON.parse(raw.stdout.split('\n').find((line: string) => line.trim())!);
      const body = (Bun.TOML.parse(card) as Record<string, unknown>).developer_instructions;
      requireThat(typeof body === 'string' && audit.config.at(-1) === `developer_instructions=${JSON.stringify(body)}`, 'The call did not use the archived role card');
      requireThat(hash(JSON.stringify(audit.schema)) === call.schemaSha256 && audit.timeoutMs === call.timeoutMs && audit.effort === 'medium', 'Schema, effort or timeout mismatch');
    }
  }
}

export type Verification = { ok: boolean; errors: string[]; notes: string[]; mode: Result['mode'] | null; status: Result['status'] | null; corrected: boolean; result?: Result };
/** Rebuild the guide, the brief, every prompt, check, score and stop decision from the archived sources, without a model. */
export async function verifyRun(input: unknown, base: string, store = resolve(base, STORE)): Promise<Verification> {
  try {
    const r = ResultSchema.parse(input) as Result;
    requireThat(r.evidence, 'Missing archived sources');
    requireThat(r.finishedAt !== null, 'The run has no final checkpoint');
    requireThat(hash(JSON.stringify(r.evidence.tooling)) === r.evidence.toolingSha256, 'The tooling record was edited');
    const changed = toolingManifest(base).filter(t => r.evidence!.tooling.find(x => x.file === t.file)?.sha256 !== t.sha256).map(t => t.file);
    const notes = changed.length ? [`Replayed with code changed since the run: ${changed.join(', ')}`] : [];
    const files = archivedFiles(r.evidence, store), shas = Object.fromEntries(r.evidence.sources.map(s => [s.file, s.sha256]));
    const guide = guideFromFiles(files, shas);
    requireThat(equal(Object.keys(shas).sort(), [...new Set([...guide.sources.map(s => s.file), ...ROLE_FILES])].sort()), 'Unexpected or missing sources');
    requireThat(equal(guide, r.guide), 'The recorded guide differs from its sources');
    const brief = briefsFromFiles(files, shas).find(b => b.id === r.brief.id);
    requireThat(brief && equal(brief, r.brief), 'The recorded brief differs from its sources');
    let previous = r.startedAt;
    for (const call of r.calls) {
      requireThat(call.finishedAt !== null && call.startedAt >= previous && call.finishedAt >= call.startedAt, 'Invalid or unfinished call times');
      previous = call.finishedAt;
    }
    requireThat(r.finishedAt >= previous, 'The final time precedes a call');
    if (r.mode === 'live') verifyProvider(r, files);
    else requireThat(r.authorization === null, 'Fixtures cannot carry live approval');
    const times = [r.startedAt, ...r.calls.flatMap(c => [c.startedAt, c.finishedAt!]), r.finishedAt];
    let t = 0, index = 0;
    const rebuilt = await runLoop({ guide, brief, steer: r.steer, mode: r.mode, limits: r.limits, evidence: r.evidence,
      authorization: r.authorization ?? undefined, now: () => times[Math.min(t++, times.length - 1)]!,
      invoke: async request => {
        const call = r.calls[index++];
        requireThat(call && request.stage === call.stage && request.role === call.role && request.prompt === call.prompt && request.timeoutMs === call.timeoutMs,
          'A recorded call does not match the rebuilt stage, role, prompt or timeout');
        requireThat(hash(JSON.stringify(z.toJSONSchema(request.schema))) === call.schemaSha256, 'Recorded schema mismatch');
        if (call.output === undefined && call.error) throw new Error(call.error);
        return { output: call.output, tokens: call.tokens, raw: call.raw };
      } });
    requireThat(index === r.calls.length && equal(rebuilt, r), 'The record differs from the rebuilt loop (calls, reasons, checks, limits or status)');
    return { ok: true, errors: [], notes, mode: r.mode, status: r.status, corrected: r.mode === 'live' && corrected(r), result: r };
  } catch (error) {
    return { ok: false, errors: [error instanceof Error ? error.message : String(error)], notes: [], mode: null, status: null, corrected: false };
  }
}
