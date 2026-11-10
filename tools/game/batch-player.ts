// The batch's player steps' helpers (Splice, 2026-11-11: out of batch.ts, which is over 500 lines): whether the built player carries a step's hook, a script's end
// from the engine, and the machine's load while a player runs.
import { existsSync, readFileSync } from 'node:fs';
import { cpus, loadavg } from 'node:os';
import { join } from 'node:path';
import { build as buildEngine, engineAt } from '../missions/check/engine';
import type { End, LoadSample } from './late';
import { MARKER as TPLONG_MARKER } from './tplong';
import { ROOT } from './shots';
import { APP, env, rel } from './batch-paths';

export const LATE_FILE = join(ROOT, 'tools/game/late.json'), LATE_MARKER = 'suborned.late.report/1';
/** Does the built player carry the -late mode? Its report's schema literal is in its code only when Latch's hook is. (Without the hook the player would just open the game and sit; this stops that after a second.) */
export function playerHasLate(managed = join(APP, 'Contents/Resources/Data/Managed/Suborned.Runtime.dll')): boolean {
  if (!existsSync(managed)) return false;
  return readFileSync(managed).includes(Buffer.from(LATE_MARKER, 'utf16le'));
}
export const PERF064_MARKER = 'suborned.perf064/1';
/** Does the built player carry the -perf064 probe? Its report's schema literal is in its code only when the probe is. */
export function playerHasPerf064(managed = join(APP, 'Contents/Resources/Data/Managed/Suborned.Runtime.dll')): boolean {
  if (!existsSync(managed)) return false;
  return readFileSync(managed).includes(Buffer.from(PERF064_MARKER, 'utf16le'));
}
export const TPLONG_FILE = join(ROOT, 'tools/game/tplong.json');
/** Does the built player carry the -tplong mode? Its report's schema literal is in its code only when Latch's hook is. */
export function playerHasTpLong(managed = join(APP, 'Contents/Resources/Data/Managed/Suborned.Runtime.dll')): boolean {
  if (!existsSync(managed)) return false;
  return readFileSync(managed).includes(Buffer.from(TPLONG_MARKER, 'utf16le'));
}
/** Where a script's mission ends, from the working tree's engine (`play --json`: the tick and outcome the player's drive must reach too). */
export function engineEnd(path: string): End {
  const dll = buildEngine(engineAt('tree'));
  const r = Bun.spawnSync(['dotnet', dll, 'play', path, '--json'], { cwd: ROOT, env, stdout: 'pipe', stderr: 'pipe' });
  const line = r.stdout.toString().split('\n').find(l => l.startsWith('{'));
  if (!line) throw new Error(`the engine could not play ${rel(path)}: ${r.stderr.toString().trim().slice(0, 200)}`);
  const j = JSON.parse(line);
  return { tick: j.tick, outcome: j.outcome, fail: j.fail ?? '' };
}

/** Machine load while a player runs: the 1-minute load average at the start, its peak while sampled (every 2 s), at the end; and the busiest other processes at the peak. */
export function watchLoad(): { stop(): Promise<LoadSample> } {
  const ncpu = cpus().length, start = loadavg()[0]!; let peak = start, top: string[] = [], busy = false;
  const sample = async () => {
    if (busy) return; busy = true;
    try {
      const l = loadavg()[0]!;
      if (l >= peak) {
        peak = l;
        const p = Bun.spawn(['ps', '-Ao', 'pcpu=,pid=,comm=', '-r'], { stdout: 'pipe', stderr: 'ignore' });
        const text = await new Response(p.stdout).text();
        top = text.split('\n').slice(0, 8).map(x => x.trim().match(/^([\d.]+)\s+(\d+)\s+(.*)$/)).filter((m): m is RegExpMatchArray => !!m && Number(m[1]) >= 25 && !/The Suborned|\/ps$/.test(m[3]!))
          .slice(0, 3).map(m => `${m[3]!.split('/').pop()} ${Math.round(Number(m[1]))}%`);
      }
    } finally { busy = false; }
  };
  const timer = setInterval(sample, 2000);
  return { async stop() { clearInterval(timer); await sample(); return { ncpu, start: Math.round(start * 10) / 10, peak: Math.round(Math.max(peak, loadavg()[0]!) * 10) / 10, end: Math.round(loadavg()[0]! * 10) / 10, top }; } };
}
