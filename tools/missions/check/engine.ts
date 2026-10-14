// The headless engine as the mission checker calls it (brief 011 A, B): Keel's C# CLI (sim/cli), built once per process and run as a
// plain `dotnet <dll> play <script> --json`, many plays in parallel. Each play is a fresh process: the same script gives the same
// bytes (Keel's determinism), so a play here is the play anyone else gets.
// The engine is pinned to a commit (default HEAD): its sim/ (sources and the data it reads: the site, its people) and the rules it
// reads (contracts/rules, contracts/people/people.rules.json) are taken with `git archive` into out/missions/engine/<commit>, built
// and run there, so a check plays one consistent commit and a replay rebuilds the same one, whatever the working tree holds (Keel's
// sim/ and the rulebook move under his hands). `--engine tree` builds and plays the working tree instead. The route scripts are the
// caller's (the working tree's, or a recorded copy); the record holds the sha256 of every input the engine read.
import { existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { join, resolve } from 'node:path';

export const ROOT = resolve(import.meta.dir, '../../..');
const TMP = join(ROOT, 'out/missions/plays');
export const env = { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1', ...(process.env.DOTNET_ROOT || !existsSync('/opt/homebrew/opt/dotnet/libexec') ? {} : { DOTNET_ROOT: '/opt/homebrew/opt/dotnet/libexec' }) };
export const hasDotnet = () => !!Bun.which('dotnet');

/** An order in the engine's format (suborned.sim.script/1, sim/Suborned.Sim/Script.cs). */
export type Order = { tick: number; op?: 'scout' | 'fixer' | 'tech' | 'enforcer'; do: string; to?: string | number[]; target?: string; approach?: string };
export type Script = { schema: 'suborned.sim.script/1'; mission: 'handover'; note?: string; start: Record<string, unknown>; orders: Order[]; until: number };
/** What a play reports (sim/Suborned.Sim/Script.cs Report.Json); the checker keeps all of it in memory and records a digest. */
export type Play = {
  id: string; outcome: 'success' | 'failure' | 'withdrawn' | 'none'; fail: string; tick: number; cash: number; trail: number; alert: number; gunfire: number; standing: number;
  startHash: string; finalHash: string; replayIdentical: boolean; checkpoints: { tick: number; hash: string }[];
  orders: { order: Order; ok: boolean; why?: string }[]; log: { tick: number; code: string; text: string }[]; debrief?: { route?: string[]; chain?: string[]; contacts?: number; loud?: boolean; publicSecurity?: string };
};

/** sha256 of the engine's source (library and CLI, .cs and .csproj, by path) under `dir` (the repo root or a pinned copy). */
export function engineHash(dir = ROOT): string {
  const h = createHash('sha256');
  for (const sub of ['sim/Suborned.Sim', 'sim/cli']) {
    for (const f of readdirSync(join(dir, sub)).filter(f => /\.(cs|csproj)$/.test(f)).sort()) h.update(`${sub}/${f}\n`).update(readFileSync(join(dir, sub, f)));
  }
  return h.digest('hex');
}
const git = (...a: string[]) => { const r = Bun.spawnSync(['git', ...a], { cwd: ROOT, stdout: 'pipe', stderr: 'pipe' }); if (r.exitCode !== 0) throw new Error(`git ${a.join(' ')}: ${r.stderr}`); return r.stdout.toString().trim(); };
/** The files the engine reads, relative to its root (sim/Suborned.Sim/Debrief.cs Engine.Load): the rules and every sim/data file. */
export function engineInputs(dir: string): string[] {
  return ['contracts/rules/rulebook.json', 'contracts/people/people.rules.json', ...readdirSync(join(dir, 'sim/data')).filter(f => f.endsWith('.json')).sort().map(f => `sim/data/${f}`)];
}
/** The engine a check runs: `rev` a commit (or HEAD), resolved to its full sha, or 'tree' for the working tree. */
export type EngineAt = { rev: string; commit: string; dir: string; hash: string };
export function engineAt(rev = 'HEAD'): EngineAt {
  if (rev === 'tree') return { rev, commit: 'tree', dir: ROOT, hash: engineHash() };
  const commit = git('rev-parse', `${rev}^{commit}`), dir = join(ROOT, 'out/missions/engine', commit.slice(0, 12));
  if (!existsSync(join(dir, 'sim/cli/cli.csproj'))) {
    mkdirSync(dir, { recursive: true });
    const tar = Bun.spawnSync(['sh', '-c', `git archive ${commit} sim/Suborned.Sim sim/cli sim/data contracts/rules contracts/people/people.rules.json | tar -x -C '${dir}'`], { cwd: ROOT, stdout: 'pipe', stderr: 'pipe' });
    if (tar.exitCode !== 0) throw new Error(`git archive ${commit}: ${tar.stderr}`);
  }
  return { rev, commit, dir, hash: engineHash(dir) };
}

const built = new Map<string, string>();
/** Build the engine's CLI (Release) once per process; a no-op build when nothing changed. Throws with the compiler's output. */
export function build(e: EngineAt = engineAt()): string {
  const have = built.get(e.dir); if (have) return have;
  const out = join(e.dir === ROOT ? join(ROOT, 'out/missions/engine/tree') : e.dir, 'bin'), dll = join(out, 'suborned-sim.dll');
  const r = Bun.spawnSync(['dotnet', 'build', join(e.dir, 'sim/cli'), '-c', 'Release', '-o', out, '-v', 'q', '-nologo'], { cwd: ROOT, env, stdout: 'pipe', stderr: 'pipe' });
  if (r.exitCode !== 0 || !existsSync(dll)) throw new Error(`dotnet build ${e.rev === 'tree' ? 'sim/cli' : `sim/cli at ${e.commit.slice(0, 12)}`} failed:\n${r.stdout}${r.stderr}`);
  built.set(e.dir, dll);
  return dll;
}

/** Play one script; `name` labels its temp file (out/missions/plays). */
export async function play(script: Script, name: string, e?: EngineAt): Promise<Play> {
  const dll = build(e);
  mkdirSync(TMP, { recursive: true });
  const file = join(TMP, `${name.replace(/[^a-z0-9.-]+/gi, '_')}.json`);
  writeFileSync(file, JSON.stringify(script));
  const p = Bun.spawn(['dotnet', dll, 'play', file, '--json'], { cwd: (e ?? engineAt()).dir, env, stdout: 'pipe', stderr: 'pipe' });
  const [out, err, code] = await Promise.all([new Response(p.stdout).text(), new Response(p.stderr).text(), p.exited]);
  if (!out.trim().startsWith('{')) throw new Error(`play ${name} failed (exit ${code}): ${err || out}`);
  return JSON.parse(out) as Play;
}

/** Play many, at most `n` at once, in input order. */
export async function playAll(jobs: { script: Script; name: string }[], e?: EngineAt, n = 8): Promise<Play[]> {
  const out: Play[] = new Array(jobs.length);
  let next = 0;
  await Promise.all(Array.from({ length: Math.min(n, jobs.length) }, async () => {
    while (next < jobs.length) { const i = next++; out[i] = await play(jobs[i]!.script, jobs[i]!.name, e); }
  }));
  return out;
}
