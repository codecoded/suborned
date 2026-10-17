// What the people file and M-2 trust (Redline H1, M1, 2026-10-15) and what Keel's rulebook reads of the export (keel-rulebook.md point 8).
import { beforeAll, describe, expect, test } from 'bun:test';
import { cpSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import { Cast } from '../../../contracts/people/cast.schema';
import { People } from '../../../contracts/people/people.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { sha256 } from '../../gen/sha';
import { overlay } from '../../missions/handover/overlay';
import { checkPeople } from '../check';
import { currentPeople } from '../current';
import { expand, needWeightsOf, needsOf, rowOf, type Ctx } from '../expand';
import { readTexts, world, type World } from '../inputs';
import { castFileOf, promotionFileOf } from '../promoted';
import { castForNames } from '../roles';
import { withSecrets } from '../secrets';

const root = join(import.meta.dir, '..', '..', '..');
let w: World, ctx: Ctx, gen: ReturnType<typeof expand>, texts: Awaited<ReturnType<typeof readTexts>>;
const clone = <T>(x: T): T => JSON.parse(JSON.stringify(x));
const row = (id: string) => checkPeople(ctx, w.mission, w.plan, undefined).checks.find(c => c.id === id)!;
beforeAll(async () => {
  texts = await readTexts('contracts/world/harbour-district.plan.json');
  w = world(texts, 'civic-block');
  ctx = w.ctx(Cast.parse(JSON.parse(readFileSync(join(root, castFileOf('harbour-district')), 'utf8')))); gen = expand(ctx);
});

describe('the people file carries what it was made from', () => {
  test('hashes of the cast, life, block, rules, mission and plan text', () => {
    expect(gen.people.inputs).toEqual({
      cast: sha256(JSON.stringify(ctx.cast)), life: sha256(texts.life), block: sha256(texts.block), rules: sha256(texts.rules), mission: sha256(texts.mission), plan: sha256(texts.plan),
      needs: sha256(`${texts.needs!.rules}\n${texts.needs!.catalogue}`), // brief 017: the need kinds and the catalogue
    });
    expect(People.safeParse(gen.people).success).toBe(true);
    expect(People.safeParse({ ...gen.people, inputs: { ...gen.people.inputs, tools: 'x' } }).success).toBe(true); // `bun run people` adds the tools hash
  });
  test('another mission or plan text is another file', () => {
    const other = world({ ...texts, mission: texts.mission.replace('"startsAt": "17:24:30"', '"startsAt": "17:00"') }, 'civic-block');
    expect(other.hashes('x').mission).not.toBe(w.hashes('x').mission);
    expect(world({ ...texts, plan: texts.plan.replace('"version": "0.3.0"', '"version": "0.3.1"') }, 'civic-block').hashes('x').plan).not.toBe(w.hashes('x').plan);
  });
});

describe('M-2 casts from the promoted cast, not from the last file written', () => {
  const setup = () => { const plan: Plan = JSON.parse(texts.plan), block = JSON.parse(texts.block); return { plan, block, civ: civic(plan.civic, block), d: derive(plan) }; };
  const m2 = (s: ReturnType<typeof setup>, mission = JSON.parse(texts.mission)) => overlay(s.plan, s.block, undefined, mission).civic({ plan: s.plan, d: s.d, civ: s.civ } as never).find(c => c.id === 'M-2')!;
  test('with no people given it expands the promoted cast: the same people as bun run people', () => {
    const now = currentPeople(setup().plan, JSON.parse(texts.mission));
    expect(now.problems).toEqual([]);
    expect(JSON.stringify(now.people)).toBe(JSON.stringify(gen.people));
    // whatever stands in out/people: the casting is that of the people just expanded (the promoted r013 cannot cast the waitress under the lovers' canon, brief 011: she needs the affair)
    const s = setup(), given = overlay(s.plan, s.block, gen.people as never, JSON.parse(texts.mission)).civic({ plan: s.plan, d: s.d, civ: s.civ } as never).find(c => c.id === 'M-2')!;
    expect(m2(setup()).value).toBe(given.value);
  });
  test('it follows the mission it is given: a pin that Inez\'s row cannot carry fails', () => {
    const mission = JSON.parse(texts.mission); mission.roles.find((r: { id: string }) => r.id === 'waitress').pin = 'p-records-office-02';
    const r = m2(setup(), mission);
    expect(r.ok).toBe(false); expect(r.detail).toContain('pinned p-records-office-02');
  });
  test('a promoted cast that drifted from its run is no cast: M-2 refuses', () => {
    const dir = mkdtempSync(join(tmpdir(), 'current-'));
    try {
      const rec = JSON.parse(readFileSync(join(root, promotionFileOf('harbour-district')), 'utf8'));
      for (const f of [castFileOf('harbour-district'), promotionFileOf('harbour-district'), rec.source.file, rec.source.runFile]) { mkdirSync(dirname(join(dir, f)), { recursive: true }); cpSync(join(root, f), join(dir, f)); }
      writeFileSync(join(dir, castFileOf('harbour-district')), readFileSync(join(dir, castFileOf('harbour-district')), 'utf8').replace('"weight": 60', '"weight": 61'));
      const s = setup(), now = currentPeople(s.plan, JSON.parse(texts.mission), dir);
      expect(now.people).toBeUndefined(); expect(now.problems.join(' ')).toContain('was edited');
    } finally { rmSync(dir, { recursive: true, force: true }); }
  });
});

describe('P-9 is a second expansion', () => {
  test('a people file that is not the expansion of its cast fails, with the people who differ', () => {
    const p = clone(gen.people), cafe = p.persons.filter(q => q.work?.unit === 'cafe'), a = cafe[0]!, b = cafe.find(q => q.temperament !== a.temperament)!; const t = a!.temperament; a!.temperament = b!.temperament; b!.temperament = t;
    const r = checkPeople(ctx, w.mission, w.plan, { people: p, gaps: [] }).checks.find(c => c.id === 'P-9')!;
    expect(r.ok).toBe(false); expect(r.detail).toContain('two expansions of the same inputs differ'); expect(r.detail).toContain(a!.id);
  });
  test('an expander whose two runs differ fails', () => {
    let calls = 0, per = 0;
    const counting = { ...ctx, walk: { ...ctx.walk, min: (x: [number, number], y: [number, number]) => { calls++; return ctx.walk.min(x, y); } } };
    expand(counting); per = calls;
    expect(per).toBeGreaterThan(0);
    calls = 0;
    const flaky = { ...ctx, walk: { ...ctx.walk, min: (x: [number, number], y: [number, number]) => { const m = ctx.walk.min(x, y), later = Math.floor(calls++ / per) % 2 === 1; return m === Infinity || m === 0 ? m : later ? m + 1 : m; } } };
    const r = checkPeople(flaky, w.mission, w.plan).checks.find(c => c.id === 'P-9')!; // the people made first, then the second expansion
    expect(r.ok).toBe(false); expect(r.detail).toContain('differ');
  });
  test('the real expansion is the same twice', () => expect(row('P-9').ok).toBe(true));
});

describe('the export carries what the rulebook reads', () => {
  test('a position and the needs of the person\'s own row as integer per mille', () => {
    for (const p of gen.people.persons) {
      expect(p.tier).toBe('simulated');
      expect(p.position).toBe(p.work ? (p.work.position ?? p.work.role)! : 'resident');
      const cast = withSecrets(ctx.cast, ctx.rules), own = p.work ? rowOf(cast, p.work.area, p.work.trade) : rowOf(cast, w.scope.homes.find(h => h.plot === p.home.plot)!.area, 'resident');
      expect(Object.keys(p.needWeights).sort()).toEqual([...new Set(needsOf(own, p.position).map(n => n.need))].sort());
      expect(Object.values(p.needWeights).reduce((a, b) => a + b, 0)).toBe(1000);
      expect(Object.values(p.needWeights).every(v => Number.isInteger(v) && v > 0)).toBe(true);
      expect(p.needWeights[p.need]).toBeGreaterThan(0);
    }
    expect(gen.people.knowledge!.needsKnown.length).toBeGreaterThan(0); // the keepers' tip lists
  });
  test('largest remainder, ties by need id', () => {
    const r = { needs: [{ need: 'b', weight: 1 }, { need: 'a', weight: 1 }, { need: 'c', weight: 1 }] } as never;
    expect(needWeightsOf(r, 'x')).toEqual({ a: 334, b: 333, c: 333 });
    expect(needWeightsOf({ needs: [{ need: 'cash', weight: 100, positions: ['driver'] }, { need: 'debt', weight: 100, positions: ['server'] }] } as never, 'driver')).toEqual({ cash: 1000 });
    expect(needWeightsOf(undefined, 'x')).toEqual({});
  });
  test('P-3 holds the position and the weights to the row', () => {
    const p = clone(gen.people), x = p.persons[0]!; x.needWeights = { ...x.needWeights, wanderlust: 1 };
    expect(checkPeople(ctx, w.mission, w.plan, { people: p, gaps: [] }).checks.find(c => c.id === 'P-3')!.detail).toContain('needWeights are not their row');
  });
});

describe('names follow the cast M-2 pins', () => {
  test('the cast the names follow is the rules\' cast until the people play the mission, then the playable one', () => {
    const c = { scope: w.scope, mission: w.mission, plan: w.plan }, cast = castForNames(gen.people.persons, c, w.rules);
    expect(cast.length).toBe(w.mission.roles.length);
    // the waitress is whoever M-2 casts from these people (r013's p-cafe-11 until r016; the promoted cast's lover, Inez, p-cafe-14, now), never an id written here
    const plan: Plan = JSON.parse(texts.plan), block = JSON.parse(texts.block), civ = civic(plan.civic, block);
    const m2 = overlay(plan, block, gen.people as never, JSON.parse(texts.mission)).civic({ plan, d: derive(plan), civ } as never).find(x => x.id === 'M-2')!;
    expect(m2.ok, m2.detail).toBe(true);
    expect(cast.find(x => x.role === 'waitress')!.pinned).toBe(/waitress (p-[a-z-]+-\d+)/.exec(String(m2.value))![1]!);
  });
});
