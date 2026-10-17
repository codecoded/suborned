// The engine from bun (brief 011 A): the site the engine reads is fresh against its inputs, and the C# CLI plays a scenario and a
// script with identical checksums twice. Needs the .NET SDK (dotnet on PATH); without it the CLI tests are skipped, not failed.
// Run: bun test tools/sim
import { describe, expect, test } from 'bun:test';
import { selectCover } from '../cover/select';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { buildProvisional, buildSite, PROVISIONAL_FILE, SITE_FILE, sourceOfSite } from './site';
import { buildPeople, PEOPLE_OUT } from './people';
import { buildTakedown, TAKEDOWN_FILE } from './takedown';

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
  test('sim/data/handover.provisional.json is generated from the plan and the mission: civic area base 2 + the mission\'s bonus 0 = the Handover\'s S2, the plan\'s id as the district', () => {
    const text = readFileSync(join(root, PROVISIONAL_FILE), 'utf8'), p = JSON.parse(text);
    expect(text).toBe(buildProvisional()); // bun run sim:site
    const plan = JSON.parse(readFileSync(join(root, 'contracts/world/harbour-district.plan.json'), 'utf8')), mission = JSON.parse(readFileSync(join(root, 'contracts/missions/handover/mission.json'), 'utf8'));
    expect(p.security.base).toBe(plan.areas.find((a: { id: string }) => a.id === 'records').securityBase);
    expect(p.security.bonus).toBe(mission.securityBonus);
    expect(p.security.base + p.security.bonus).toBe(2); // the level the recorded runs ran at
    expect(p.district.id).toBe(plan.id);
  });
  test('the cast is M-2\'s, and a fixture says it is one', () => {
    const c = JSON.parse(readFileSync(join(root, SITE_FILE), 'utf8')).cast;
    expect(Object.keys(c)).toEqual(expect.arrayContaining(['waitress', 'driver', 'guard', 'councillor', 'from', 'sha256', 'startsAt']));
    if (c.from !== 'out/people/harbour-district.people.json') expect(typeof c.fixture).toBe('string');
  });
});

describe('sim/data/handover.takedown.json (brief 013 C: stash containers, service doors, report points, response origins)', () => {
  const file = () => JSON.parse(readFileSync(join(root, TAKEDOWN_FILE), 'utf8'));
  test('is what tools/sim/takedown.ts makes from the world today (bun run sim:site)', () => {
    expect(readFileSync(join(root, TAKEDOWN_FILE), 'utf8')).toBe(buildTakedown().json);
  }, 120_000);
  test('shares the site\'s grid, and every stand is a free cell of it', () => {
    const site = JSON.parse(readFileSync(join(root, SITE_FILE), 'utf8')), t = file(), g = site.grid;
    expect(t.grid).toEqual({ x0: g.x0, y0: g.y0, cell: g.cell });
    const free = (p: number[]) => { const i = Math.floor((p[0]! - g.x0) / g.cell), j = Math.floor((p[1]! - g.y0) / g.cell); return site.grid.rows[i]?.[j] === '.'; };
    for (const e of [...t.entrances, ...t.stashes, ...t.reportPoints, ...t.origins]) expect(free(e.stand), `${e.id} stand ${e.stand}`).toBe(true);
  });
  test('names the two service doors, the Office\'s container with its footprint, the café\'s reason, the Office doors as posts, and keeps the site\'s places out of it', () => {
    const t = file();
    expect(t.entrances.filter((e: { kind: string }) => e.kind === 'service').map((e: { id: string }) => e.id)).toEqual(['cafe-kitchen-door', 'office-service-door']);
    expect(t.stashes.filter((s: { kind: string }) => s.kind === 'container').map((s: { of: string; holds: number }) => `${s.of} ${s.holds}`)).toEqual(['office-service-door 1']);
    expect(t.stashes.find((s: { id: string }) => s.id === 'stash/cafe-kitchen-door/door').bare).toMatch(/no container/);
    const rb = JSON.parse(readFileSync(join(root, 'contracts/rules/rulebook.json'), 'utf8')); // a service door holds the rulebook's stash-door-holds, the number the engine takes for a door (GameTakedown.HoldsOf)
    expect(t.stashes.filter((s: { kind: string }) => s.kind === 'door').map((s: { holds: number }) => s.holds)).toEqual([rb.values['stash-door-holds'].v, rb.values['stash-door-holds'].v]);
    expect(t.solids.map((s: { id: string }) => s.id)).toContain('street/civic/stash/office-service-door');
    expect(t.origins.filter((o: { kind: string; door?: string }) => o.kind === 'post' && o.door).map((o: { door: string }) => o.door)).toEqual(['office-entrance', 'official-entrance']);
    expect(t.watchers.every((w: { inSim: boolean }) => w.inSim === false)).toBe(true); // the engine reads none of the world's watchers (briefs/013 C open issue 3)
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
