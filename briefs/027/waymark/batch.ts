// Waymark's player, many variants at a time (brief 042, Splice). `playMany` returns, for each env set and in input order, exactly what one
// `dotnet player.dll <base>` process with that env prints to stdout. If the built player has the batch mode (`--batch`, Program.cs here) it plays
// them all in ONE process on every core; if it is an older build, or SUBORNED_PLAYER_BATCH=0, it spawns a process per variant as before
// (`par` at a time). Same bytes either way: briefs/027/waymark/batch-proof.ts is the proof.
import { mkdtempSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
const DOTNET_ROOT = '/opt/homebrew/opt/dotnet/libexec';
const env0 = () => ({ ...process.env, DOTNET_ROOT } as Record<string, string>);
export async function hasBatch(dll: string, cwd: string): Promise<boolean> {
  if (process.env.SUBORNED_PLAYER_BATCH === '0') return false;
  const f = join(mkdtempSync(join(tmpdir(), 'player-probe-')), 'empty.jsonl'); writeFileSync(f, '');
  const p = Bun.spawn(['dotnet', dll, '--batch', f], { cwd, env: env0(), stdout: 'pipe', stderr: 'pipe' });
  const out = await new Response(p.stdout).text(); await new Response(p.stderr).text();
  return (await p.exited) === 0 && out === '';
}
export async function playMany(dll: string, cwd: string, base: string, envs: Record<string, string>[], opts: { par?: number; mode?: 'batch' | 'process' | 'auto' } = {}): Promise<string[]> {
  const mode = opts.mode ?? 'auto';
  if (mode === 'batch' || (mode === 'auto' && await hasBatch(dll, cwd))) {
    const f = join(mkdtempSync(join(tmpdir(), 'player-batch-')), 'jobs.jsonl');
    writeFileSync(f, envs.map(env => JSON.stringify({ base, env })).join('\n') + '\n');
    const p = Bun.spawn(['dotnet', dll, '--batch', f], { cwd, env: env0(), stdout: 'pipe', stderr: 'inherit' });
    const text = await new Response(p.stdout).text(); await p.exited;
    const lines = text.split('\n').filter(Boolean).map(l => JSON.parse(l) as { out: string; err: string; code: number });
    if (lines.length !== envs.length) throw new Error(`player batch: ${lines.length} results for ${envs.length} variants (exit ${p.exitCode})`);
    for (const l of lines) if (l.err) process.stderr.write(l.err);
    return lines.map(l => l.out);
  }
  const res: string[] = new Array(envs.length); let next = 0;
  await Promise.all(Array.from({ length: opts.par ?? 16 }, async () => {
    while (next < envs.length) {
      const k = next++;
      const p = Bun.spawn(['dotnet', dll, base], { cwd, env: { ...env0(), ...envs[k]! }, stdout: 'pipe' });
      res[k] = await new Response(p.stdout).text();
    }
  }));
  return res;
}
