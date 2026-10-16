// The mission checker (brief 011 B): the route manifest and scripts parse; the variations are the transforms they claim; each §09
// condition fails on a failing example; and, where dotnet is installed, the Handover's routes play headless (route A and route B
// win) and the recorded check replays to the same hashes. Run: bun test tools/missions
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { Routes } from '../../../contracts/missions/routes.schema';
import { ROOT, engineAt, hasDotnet, type Script } from './engine';
import { judge, assessment, type Digest, type Plays } from './judge';
import { check, load, replay, runsDir } from './run';
import { contactsOf, lateFrom, stepsOf, withoutContact } from './vary';

setDefaultTimeout(120_000);
const DIR = join(ROOT, 'contracts/missions/handover/routes'), L = load(DIR), raw = readFileSync(join(DIR, 'routes.json'), 'utf8');
const broken = (edit: (m: any) => void) => { const m = JSON.parse(raw); edit(m); return Routes.safeParse(m).success; };

describe('the Handover route manifest and scripts', () => {
  test('parse, and every script is an engine order script for the Handover', () => {
    expect(L.manifest.scripts.length).toBeGreaterThanOrEqual(16);
    for (const s of Object.values(L.scripts)) {
      expect(s.schema).toBe('suborned.sim.script/1');
      expect(s.start).toEqual({}); // no fixture: every route starts from the mission's own start, the final fight and the crowd on
      expect([5100, 6000]).toContain(s.until); // the 8:30 close (a careless run that seals after it plays to 10:00)
      expect(s.orders.every((o, i, a) => i === 0 || a[i - 1]!.tick <= o.tick)).toBe(true);
    }
    // §07: route A and route B quiet, a loud route, every fail listed, both final-fight branches played (N10, N11 landed)
    const by = (k: string) => L.manifest.scripts.filter(s => s.kind === k).map(s => s.id);
    expect(by('quiet')).toEqual(['route-a', 'route-a-pressure', 'route-b', 'route-b-block', 'route-g', 'route-g-fast', 'route-g-kill']);
    expect(by('careless')).toEqual(['careless-takedown-seen-camera', 'careless-takedown-seen-crowd', 'careless-body-found', 'careless-reporter-not-stopped', 'careless-guard-left-door-held']); // brief 013 D
    expect(L.manifest.fails.map(f => f.gdd)).toEqual(['Vale killed', 'Vale leaves in official car', 'rivals reach Vale', 'district seals', 'Enforcer down', 'entry seen by guard', 'entry seen by live camera', 'entry seen by patrol']);
    expect(L.manifest.scripts.filter(s => s.pending).map(s => s.id)).toEqual([]);
    expect(L.manifest.scripts.filter(s => s.id.startsWith('fight-')).map(s => s.id)).toEqual(['fight-loud-enforcer-down', 'fight-safe-rivals-reach-vale']);
    // the close: 8:30 (Sky 2026-10-15), mission.json's last timeline entry; route B closes on it
    expect(L.manifest.closing.at).toBe('8:30');
    expect(L.manifest.scripts.filter(s => s.closes).map(s => `${s.id} ${s.closes!.at} ${s.closes!.secured}`)).toEqual(['route-a 5:50 enter.in', 'route-b 8:30 take.done', 'route-g-fast 5:50 enter.in']);
  });
  test('the manifest rejects what it must', () => {
    expect(broken(() => {})).toBe(true);
    expect(broken(m => { m.scripts[0].primary = false; })).toBe(false); // route A has no primary script
    expect(broken(m => { delete m.scripts[0].closes; })).toBe(false); // a primary quiet route names its closing event
    expect(broken(m => { m.scripts.find((s: any) => s.id === 'fail-vale-dead').expects.fail = 'sealed'; })).toBe(false); // reaches a fail by a code it doesn't list
    expect(broken(m => { delete m.scripts.find((s: any) => s.id === 'loud').route; })).toBe(false);
    expect(broken(m => { m.scripts[1].id = 'route-a'; })).toBe(false);
    expect(broken(m => { delete m.scripts.find((s: any) => s.kind === 'careless').twin; })).toBe(false); // a careless script names its careful twin
    expect(broken(m => { m.scripts.find((s: any) => s.kind === 'careless').twin = 'loud'; })).toBe(false); // and the twin is quiet
  });
});

describe('variations', () => {
  const a = L.scripts['route-a']!;
  test('without a contact drops exactly the orders that work through it', () => {
    expect(contactsOf(a, ['waitress', 'driver'])).toEqual(['waitress']);
    const w = withoutContact(a, 'waitress');
    expect(a.orders.length - w.orders.length).toBe(5); // watch, listen, turn (help) twice (r016: a Fearful Inez, §04), call
    expect(w.orders.some(o => o.target === 'waitress')).toBe(false);
    expect(withoutContact(a, 'driver').orders).toEqual(a.orders);
  });
  test('late from a step moves that step and every later order, and nothing before it', () => {
    const steps = stepsOf(a), at = steps[4]!, v = lateFrom(a, at, 7);
    a.orders.forEach((o, i) => expect(v.orders[i]!.tick).toBe(o.tick >= at ? o.tick + 70 : o.tick));
  });
});

describe('the §09 conditions, each with a failing example', () => {
  const win = (tick: number, trail: number, marks: Record<string, number> = {}): Digest => ({ outcome: 'success', fail: '', tick, cash: 60000, trail, alert: 0, gunfire: 4, startHash: 's', finalHash: 'f', replayIdentical: true, rejected: 0, scriptSha: 'x', marks, shots: 0, fight: 'safe' });
  const fail = (code: string): Digest => ({ ...win(4800, 0), outcome: 'failure', fail: code });
  /** Plays that meet every condition: A and B win, loud wins dearer, each fail is reached, each contact leaves the other route. */
  const good = (): Plays => {
    const p: Plays = {
      'route-a': win(3097, 3, { 'enter.in': 2329 }), 'route-a-pressure': win(3097, 6), 'route-b': win(4468, 2, { 'take.done': 4080 }), 'route-b-block': win(4468, 3),
      loud: { ...win(3200, 9), shots: 2, fight: 'loud' }, 'loud-fast': { ...win(3100, 9, { captured: 3100 }), shots: 2, fight: 'loud' }, withdraw: { ...win(300, 0), outcome: 'withdrawn' },
    };
    for (const s of L.manifest.scripts.filter(s => s.kind === 'fail' && !s.pending)) p[s.id] = fail(s.expects.fail!);
    p['route-b'] = win(4698, 4, { 'take.done': 4080 }); // taken 6:48: 1:42 before the 8:30 close
    // brief 013 D: route G takes the guard out (no contact); careless twins fail or win only loud and dearer
    p['route-g'] = { ...win(2474, 2), campaignTrail: 4, alert: 1, takedownS: 6 }; p['route-g-fast'] = { ...win(2170, 2, { 'enter.in': 1270 }), campaignTrail: 4, alert: 1, takedownS: 6 };
    p['route-g-kill'] = { ...win(2843, 2), campaignTrail: 2, alert: 1, takedownS: 34.4, openCases: 1 };
    p['careless-takedown-seen-camera'] = fail('entry-seen-camera'); p['careless-takedown-seen-crowd'] = fail('sealed'); p['careless-body-found'] = fail('sealed');
    p['careless-reporter-not-stopped'] = { ...win(4604, 10), alert: 2 }; p['careless-guard-left-door-held'] = fail('rivals-reach-vale');
    for (const s of ['route-a', 'route-a-pressure']) p[`${s}~no-waitress`] = fail('entry-seen-guard');
    for (const s of ['route-b', 'route-b-block']) p[`${s}~no-driver`] = fail('abduction-seen-camera');
    return p;
  };
  const ids = (p: Plays) => judge(L.manifest, L.scripts, p, []).conditions.filter(c => !c.ok).map(c => c.id);
  test('the good plays pass every condition', () => expect(ids(good())).toEqual([]));
  test('MC-0: a script that does not do what its manifest says', () => { const p = good(); p['fail-vale-dead'] = fail('sealed'); expect(ids(p)).toContain('MC-0'); });
  test('MC-1: one quiet route', () => { const p = good(); for (const g of ['route-g', 'route-g-fast', 'route-g-kill']) p[g] = fail('entry-seen-camera'); p['route-b'] = fail('vale-leaves'); p['route-b-block'] = fail('vale-leaves'); expect(ids(p)).toContain('MC-1'); });
  test('MC-1: a shot before the final fight makes a win loud; the fight\'s own shots do not', () => {
    const p = good(); p['route-b'] = { ...p['route-b']!, shots: 1, fight: 'loud' }; p['route-b-block'] = { ...p['route-b-block']!, shots: 1 }; for (const g of ['route-g', 'route-g-fast', 'route-g-kill']) p[g] = { ...p[g]!, shots: 1 };
    expect(ids(p)).toContain('MC-1');
    expect(good()['route-a']!.gunfire).toBeGreaterThan(0); // the good plays fire in the fight and still count as quiet
  });
  test('MC-6: loud faster than every informed quiet win (T2); a loud win that leaves someone captured is not raced', () => {
    const p = good(); p.loud = { ...p.loud!, tick: 2000 }; expect(ids(p)).toEqual(['MC-6']);
    const q = good(); q['loud-fast'] = { ...q['loud-fast']!, tick: 2000 }; expect(ids(q)).toEqual([]); // the capture variant is faster, and doesn't count
  });
  test('MC-2: a quiet variant (Pressure) as dear as loud fails, not only the primary routes', () => { const p = good(); p['route-a-pressure'] = { ...p['route-a-pressure']!, trail: 9 }; expect(ids(p)).toEqual(['MC-2']); });
  test('MC-2: the loud route no dearer than a quiet one', () => { const p = good(); p.loud = { ...p.loud!, trail: 3 }; expect(ids(p)).toContain('MC-2'); });
  test('MC-2 counts Trail in total: a statement carried into the campaign makes a quiet route as dear as loud', () => { const p = good(); p['route-g'] = { ...p['route-g']!, campaignTrail: 7 }; expect(ids(p)).toEqual(['MC-2']); });
  test('MC-7: a careless takedown that wins quietly, or at Alert 2 no dearer than its twin, is not worse', () => {
    const p = good(); p['careless-reporter-not-stopped'] = { ...p['careless-reporter-not-stopped']!, alert: 1 }; expect(ids(p)).toContain('MC-7');
    const q = good(); q['careless-reporter-not-stopped'] = { ...q['careless-reporter-not-stopped']!, trail: 4 }; expect(ids(q)).toContain('MC-7');
  });
  test('MC-8: takedown routes that win only above Alert 1, or cost the Enforcer no time, are not viable', () => {
    const p = good(); for (const s of ['route-g', 'route-g-fast', 'route-g-kill']) p[s] = { ...p[s]!, alert: 2 }; expect(ids(p)).toContain('MC-8');
    const q = good(); q['route-g'] = { ...q['route-g']!, takedownS: 0 }; expect(ids(q)).toContain('MC-8');
  });
  test('MC-9 prices an open case (case-trail-eq): a hidden kill as fast and cheap as every turning win stops dominating at 3 T a case', () => {
    const p = good(); p['route-g-kill'] = { ...p['route-g-kill']!, tick: 2000, trail: 0, campaignTrail: 2, openCases: 1 }; p['route-g-fast'] = { ...p['route-g-fast']!, tick: 2900 }; p['route-g'] = { ...p['route-g']!, tick: 2900 };
    expect(judge(L.manifest, L.scripts, p, []).conditions.find(c => c.id === 'MC-9')!.ok).toBe(false);
    expect(judge(L.manifest, L.scripts, p, [], { caseTrailEq: 3 }).conditions.find(c => c.id === 'MC-9')!.ok).toBe(true);
  });
  test('MC-9: a takedown win as fast and as cheap as every turning win dominates them', () => { const p = good(); p['route-g-fast'] = { ...p['route-g-fast']!, tick: 2000, trail: 0, campaignTrail: 1 }; expect(ids(p)).toEqual(['MC-9']); });
  test('MC-3: a fail no script reaches', () => { const p = good(); p['fail-district-seals'] = fail('enforcer-down'); expect(ids(p)).toContain('MC-3'); });
  test('MC-4: a contact without whom no quiet route wins', () => {
    const p = good(); p['route-b~no-waitress'] = fail('vale-leaves'); p['route-b-block~no-waitress'] = fail('vale-leaves'); for (const g of ['route-g', 'route-g-fast', 'route-g-kill']) p[g] = fail('entry-seen-camera'); 
    const r = judge(L.manifest, L.scripts, p, []);
    expect(r.conditions.find(c => c.id === 'MC-4')!.ok).toBe(false);
    expect(r.contacts.find(c => c.role === 'waitress')!.routesWithout).toEqual([]);
  });
  test('MC-4 names a contact only if it leaves fewer than 2 quiet routes (with route G, neither does)', () => {
    const r = judge(L.manifest, L.scripts, good(), []);
    expect(r.contacts.map(c => [c.role, c.needs, c.named])).toEqual([['waitress', ['A'], false], ['driver', ['B'], false]]); // route G turns nobody (brief 013), so neither contact leaves fewer than 2 quiet routes
    expect(r.conditions.find(c => c.id === 'MC-4')!.ok).toBe(true);
  });
  test('MC-5: Vale secured under a minute before a route\'s closing event', () => {
    const p = good(); p['route-b'] = win(4698, 4, { 'take.done': 4800 }); // taken 30 s before the 8:30 close
    expect(ids(p)).toEqual(['MC-5']);
    expect(assessment(judge(L.manifest, L.scripts, p, [])).failures.map(f => f.rule)).toEqual(['MC-5']);
  });
});

describe.skipIf(!hasDotnet())('the Handover, played headless (Keel\'s engine at HEAD)', () => {
  test('route A and route B win quietly, the loud route costs more Trail than every quiet win, every fail is reached, and the check ships', async () => {
    const c = await check(L, engineAt('HEAD'), () => {}, { slack: false }); // the committed engine with its rules and the mission's committed site
    expect([c.plays['route-a']!.outcome, c.plays['route-b']!.outcome]).toEqual(['success', 'success']);
    expect([c.plays['route-a']!.gunfire, c.plays['route-b']!.gunfire]).toEqual([0, 0]);
    expect(c.report.conditions.filter(x => !x.ok).map(x => x.id)).toEqual([]);
    expect(c.plays.loud!.trail).toBeGreaterThan(c.plays['route-a-pressure']!.trail); // MC-2 against every quiet variant (Motive's addendum)
    expect(Object.values(c.plays).every(p => p.replayIdentical)).toBe(true);
  });
  // The latest check pinned to a commit. Checks recorded on the working tree (c007, c008: the r016 cast, Keel's engine uncommitted)
  // replay only while the tree holds, and any uncommitted rule edit breaks them (c007 broke on the rulebook's sight section), so a
  // test pins a committed record: when the r016 engine is committed, record the check against that commit and point this at it.
  test('the latest commit-pinned check (c006, pinned to 94793d7) replays to its recorded hashes', async () => {
    const r = await replay(join(runsDir('handover'), 'c006'));
    expect(r.lines.filter(l => l.startsWith('FAIL'))).toEqual([]);
    expect(r.ok).toBe(true);
  });
});
