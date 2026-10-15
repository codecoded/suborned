// The engine from bun (brief 011 A): the site the engine reads is fresh against its inputs, and the C# CLI plays a scenario and a
// script with identical checksums twice. Needs the .NET SDK (dotnet on PATH); without it the CLI tests are skipped, not failed.
// Run: bun test tools/sim
import { describe, expect, test } from 'bun:test';
import { selectCover } from '../cover/select';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { buildSite, SITE_FILE, sourceOfSite } from './site';
import { buildPeople, PEOPLE_OUT } from './people';

const root = join(import.meta.dir, '..', '..');
const env = { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1', ...(process.env.DOTNET_ROOT || !existsSync('/opt/homebrew/opt/dotnet/libexec') ? {} : { DOTNET_ROOT: '/opt/homebrew/opt/dotnet/libexec' }) };
const dotnet = Bun.which('dotnet');
const sim = (...args: string[]) => {
  const r = Bun.spawnSync(['dotnet', 'run', '--project', 'sim/cli', '--', ...args], { cwd: root, env, stdout: 'pipe', stderr: 'pipe' });
  return { code: r.exitCode, out: r.stdout.toString(), err: r.stderr.toString() };
};

describe('sim/data/handover.site.json', () => {
  const src = sourceOfSite();
  const present = existsSync(join(root, src.people));
  test.skipIf(!present)(`is what tools/sim/site.ts makes from the plan, the civic block, mission.json and ${src.people} today (bun run sim:site)`, () => {
    expect(readFileSync(join(root, SITE_FILE), 'utf8')).toBe(buildSite(src).json);
  });
  test.skipIf(!present)('sim/data/handover.people.json comes from the same people file', () => {
    expect(readFileSync(join(root, PEOPLE_OUT), 'utf8')).toBe(buildPeople(src));
  });
  test('the cast is M-2\'s, and a fixture says it is one', () => {
    const c = JSON.parse(readFileSync(join(root, SITE_FILE), 'utf8')).cast;
    expect(Object.keys(c)).toEqual(expect.arrayContaining(['waitress', 'driver', 'guard', 'councillor', 'from', 'sha256', 'startsAt']));
    if (c.from !== 'out/people/harbour-district.people.json') expect(typeof c.fixture).toBe('string');
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

  test('the engine takes the same cover as tools/cover/select.ts at every fight post (Pulse §8.3, on the site\'s own cover copy)', () => {
    const site = JSON.parse(readFileSync(join(root, SITE_FILE), 'utf8'));
    const at = (id: string): [number, number] => { const p = site.places[id]; const a = p.stand ?? p.at; return [a[0], a[1]]; };
    const car = at(site.getaway.car), want: Record<string, { foe: string; advance: string }> = {};
    for (const post of [...site.fight.ambush, ...site.fight.police, site.fight.guard] as string[]) {
      const me = at(post);
      want[post] = {
        foe: selectCover(site.cover.points, { at: me, objective: car, foe: car }, new Set(), { seekCm: 800, missionS: 200 })?.id ?? '',
        advance: selectCover(site.cover.points, { at: me, objective: car }, new Set(), { seekCm: 800, missionS: 200 })?.id ?? '',
      };
    }
    const c = sim('cover-vectors');
    expect(c.code, c.err).toBe(0);
    expect(JSON.parse(c.out)).toEqual(want);
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
