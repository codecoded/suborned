// The engine from bun (brief 011 A): the site the engine reads is fresh against its inputs, and the C# CLI plays a scenario and a
// script with identical checksums twice. Needs the .NET SDK (dotnet on PATH); without it the CLI tests are skipped, not failed.
// Run: bun test tools/sim
import { describe, expect, test } from 'bun:test';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { buildSite, SITE_FILE } from './site';

const root = join(import.meta.dir, '..', '..');
const env = { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1', ...(process.env.DOTNET_ROOT || !existsSync('/opt/homebrew/opt/dotnet/libexec') ? {} : { DOTNET_ROOT: '/opt/homebrew/opt/dotnet/libexec' }) };
const dotnet = Bun.which('dotnet');
const sim = (...args: string[]) => {
  const r = Bun.spawnSync(['dotnet', 'run', '--project', 'sim/cli', '--', ...args], { cwd: root, env, stdout: 'pipe', stderr: 'pipe' });
  return { code: r.exitCode, out: r.stdout.toString(), err: r.stderr.toString() };
};

describe('sim/data/handover.site.json', () => {
  test('is what tools/sim/site.ts makes from the plan, the civic block and mission.json today (bun run sim:site)', () => {
    expect(readFileSync(join(root, SITE_FILE), 'utf8')).toBe(buildSite().json);
  });
});

describe.skipIf(!dotnet)('the C# engine through its CLI', () => {
  test('route A (S10) wins, and plays to byte-identical checksums twice', () => {
    const a = sim('play', 'S10', '--json'), b = sim('play', 'S10', '--json');
    expect(a.code, a.err).toBe(0);
    const ra = JSON.parse(a.out), rb = JSON.parse(b.out);
    expect(ra.outcome).toBe('success');
    expect(ra.replayIdentical).toBe(true);
    expect(rb.checkpoints).toEqual(ra.checkpoints);
    expect(ra.checks.every((c: { ok: boolean }) => c.ok)).toBe(true);
  }, 180_000);

  test('an order script plays to the outcome its scenario reached', () => {
    const s = sim('script', 'S20');
    expect(s.code, s.err).toBe(0);
    mkdirSync(join(root, 'out/sim'), { recursive: true });
    const file = join(root, 'out/sim/s20.script.json');
    writeFileSync(file, s.out);
    const p = sim('play', file, '--json');
    expect(p.code, p.err).toBe(0);
    const r = JSON.parse(p.out);
    expect(r.outcome).toBe('success');
    expect(r.gunfire).toBe(0);
    expect(r.replayIdentical).toBe(true);
  }, 180_000);
});
