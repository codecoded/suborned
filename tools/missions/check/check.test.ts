// The mission checker (brief 011 B): the route manifest and scripts parse; the variations are the transforms they claim; each §09
// condition fails on a failing example; and, where dotnet is installed, the Handover's routes play headless (route A and route B
// win) and the recorded check replays to the same hashes. Run: bun test tools/missions
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { Routes } from '../../../contracts/missions/routes.schema';
import { ROOT, engineAt, hasDotnet, type Script } from './engine';
import { judge, assessment, inLane, type Digest, type Getaway, type Plays } from './judge';
import { blockerOut, carFree } from './blocker';
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
    // brief 018 §5: each quiet route as fight it out (the id) and board and go (<id>-board)
    // brief 059 (c038): the driver's Call is retired (route-b-block and its twin go), route-b-thin shows a posted rival thinned; loud-fast
    // goes (on engine 0.32+ the cheapest loud win must break the crew gunfire draws, so it is loud itself)
    expect(by('quiet')).toEqual(['route-a', 'route-a-board', 'route-a-pressure', 'route-a-pressure-board', 'route-a-disable', 'route-b', 'route-b-board', 'route-b-thin', 'route-g', 'route-g-board', 'route-g-fast', 'route-g-fast-board', 'route-g-kill', 'route-g-kill-board']);
    expect(by('loud')).toEqual(['loud', 'loud-fight']);
    expect(by('outcome')).toEqual(['fight-loud-wait-yard', 'withdraw', 'loud-left-downed', 'loud-left-standing']); // brief 018 §5: an op left downed, an op left standing; brief 059 MC-13: the yard's loud-wait twin withdraws once its Go is refused
    expect(L.manifest.scripts.filter(s => s.expects.left).map(s => `${s.id} ${JSON.stringify(s.expects.left)}`)).toEqual(['loud-left-downed {"scout":"captured downed"}', 'loud-left-standing {"scout":"captured fighting"}', 'careless-thin-all {"scout":"captured fighting","fixer":"captured fighting"}']); // brief 059: over-thinning goes loud, and the car goes once the police blocker is down, without the two; engine 0.35.1 (brief 039, Waymark): the Scout, fighting on his own, is at 1 health when it goes, so captured fighting (0.34.2: downed)
    expect(L.manifest.scripts.filter(s => s.holds).map(s => `${s.id} ${s.holds}`)).toEqual(['fight-loud-wait-square square', 'fight-loud-wait-yard service-drive']); // brief 059 MC-13: one loud-wait twin per stand
    expect(Object.values(L.scripts).some(s => s.orders.some(o => o.do === 'extract'))).toBe(false); // brief 018: extract is retired into Board and Go
    expect(by('careless')).toEqual(['careless-takedown-seen-camera', 'careless-takedown-seen-crowd', 'careless-body-found', 'careless-reporter-not-stopped', 'careless-guard-left-door-held', 'careless-enforcer-watches-guard', 'careless-scout-idles-pickup', 'careless-g-disable', 'careless-loop-runs-out', 'careless-thin-all']); // brief 013 D; brief 031; brief 058 (Loop v2, Disable); brief 059 S1
    // GDD v0.28.0 §07's fail line, in its order: entry seen by patrol is no fail (Sky 2026-10-26; engine 0.34.2 makes it a report, Alert 1),
    // so its line and its pending script go; no line needs a site fact today
    expect(L.manifest.fails.map(f => f.gdd)).toEqual(['Vale killed', 'Vale leaves in his car', 'rivals reach Vale', 'district seals', 'Enforcer down before Vale is in the car', 'the cell down (every op down)', 'entry seen by guard', 'entry seen by live camera']);
    expect(L.manifest.fails.some(f => f.id === 'entry-seen-patrol' || f.codes.includes('entry-seen-patrol'))).toBe(false);
    expect(L.manifest.fails.filter(f => f.needs)).toEqual([]);
    expect(L.manifest.scripts.filter(s => s.pending).map(s => s.id)).toEqual([]);
    expect(L.manifest.scripts.some(s => s.id === 'fail-entry-seen-patrol')).toBe(false);
    expect(L.manifest.scripts.filter(s => s.id.startsWith('fight-')).map(s => s.id)).toEqual(['fight-loud-enforcer-down', 'fight-safe-rivals-reach-vale', 'fight-safe-unguarded', 'fight-loud-wait-square', 'fight-loud-wait-yard']);
    // the close: 8:30 (Sky 2026-10-15), mission.json's last timeline entry; route B closes on it
    expect(L.manifest.closing.at).toBe('8:30');
    expect(L.manifest.scripts.filter(s => s.closes).map(s => `${s.id} ${s.closes!.at} ${s.closes!.secured}`)).toEqual(['route-a-board 5:50 enter.in', 'route-b-board 8:30 take.done', 'route-g-fast-board 5:50 enter.in']);
  });
  test('the manifest rejects what it must', () => {
    expect(broken(() => {})).toBe(true);
    expect(broken(m => { m.scripts[1].primary = false; })).toBe(false); // route A has no primary script (route-a-board since brief 018)
    expect(broken(m => { delete m.scripts[1].closes; })).toBe(false); // a primary quiet route names its closing event
    expect(broken(m => { m.scripts.find((s: any) => s.id === 'loud-left-downed').expects.left = { scout: 'captured' }; })).toBe(false); // a fate names its why (brief 018 §3)
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

describe('blocker.out and car.free (brief 018 §1), the engine\'s own codes', () => {
  const L0 = (t: number, code: string, text = '') => ({ tick: t, code, text });
  test('the first of each code, or undefined', () => {
    const log = [L0(10, 'fight.blocker', 'rival-1 goes for the road (cover at cv-088).'), L0(40, 'fight.down', 'The tech drops rival-1.'), L0(40, 'blocker.out'), L0(41, 'car.free')];
    expect([blockerOut(log), carFree(log)]).toEqual([40, 41]);
    expect([blockerOut([L0(10, 'fight.blocker'), L0(40, 'fight.down', 'The tech drops rival-1.')]), carFree([L0(10, 'fight.over')])]).toEqual([undefined, undefined]); // the words are not read
  });
});

describe('the §09 conditions, each with a failing example', () => {
  // brief 059 (MC-14): every win Goes after blocker.out, so a good win is free 5 s before it leaves
  const win = (tick: number, trail: number, marks: Record<string, number> = {}): Digest => ({ outcome: 'success', fail: '', tick, cash: 60000, trail, alert: 0, gunfire: 4, startHash: 's', finalHash: 'f', replayIdentical: true, rejected: 0, scriptSha: 'x', marks, shots: 0, fight: 'safe', carFreeS: 5 });
  const fail = (code: string): Digest => ({ ...win(4800, 0), outcome: 'failure', fail: code, carFreeS: undefined });
  /** Plays that meet every condition: A and B win, loud wins dearer, each fail is reached, each contact leaves the other route. */
  const good = (): Plays => {
    const p: Plays = {
      'route-a': win(3097, 3, { 'enter.in': 2329 }), 'route-a-pressure': win(3097, 6), 'route-b': win(4468, 2, { 'take.done': 4080 }), 'route-b-thin': { ...win(4468, 3), takedownS: 6 },
      loud: { ...win(3200, 9), alert: 2, shots: 2, fight: 'loud' }, 'loud-fight': { ...win(3220, 9), alert: 2, shots: 2, fight: 'loud' }, withdraw: { ...win(300, 0), outcome: 'withdrawn', carFreeS: undefined },
      'loud-left-downed': { ...win(3200, 9), alert: 2, shots: 2, fight: 'loud', campaignTrail: 4, left: { scout: 'captured downed' } }, 'loud-left-standing': { ...win(3200, 9), alert: 2, shots: 2, fight: 'loud', campaignTrail: 4, left: { scout: 'captured fighting' } },
    };
    for (const s of L.manifest.scripts.filter(s => s.kind === 'fail' && !s.pending)) p[s.id] = fail(s.expects.fail!);
    p['route-b'] = win(4698, 4, { 'take.done': 4080 }); // taken 6:48: 1:42 before the 8:30 close
    // brief 013 D: route G takes the guard out (no contact); careless twins fail or win only loud and dearer
    p['route-g'] = { ...win(2474, 2), campaignTrail: 4, alert: 1, takedownS: 6 }; p['route-g-fast'] = { ...win(2170, 2, { 'enter.in': 1270 }), campaignTrail: 4, alert: 1, takedownS: 6 };
    p['route-g-kill'] = { ...win(2843, 2), campaignTrail: 2, alert: 1, takedownS: 34.4, openCases: 1 };
    p['careless-takedown-seen-camera'] = fail('entry-seen-camera'); p['careless-takedown-seen-crowd'] = fail('vale-leaves'); p['careless-body-found'] = fail('enforcer-down');
    p['careless-reporter-not-stopped'] = { ...win(4522, 13), alert: 1 }; p['careless-guard-left-door-held'] = fail('vale-leaves');
    for (const s of ['route-a', 'route-a-pressure']) p[`${s}~no-waitress`] = fail('entry-seen-guard');
    for (const s of ['route-b', 'route-b-thin']) p[`${s}~no-driver`] = fail('vale-leaves');
    // brief 031: the Enforcer watching the guard from his side is sensed, and the blow fails; the Scout idling in the pickup camera's view is reported
    p['careless-enforcer-watches-guard'] = fail('enforcer-down'); p['careless-scout-idles-pickup'] = { ...win(4739, 3, { 'attention.reporting': 1553 }), alert: 1 };
    // brief 058: route A with Disable wins one Trail dearer; route G with Disable wins at Alert 2; a loop planted first runs out before the Enter
    p['route-a-disable'] = win(3097, 4, { 'enter.in': 2329 }); p['careless-g-disable'] = { ...win(2396, 7), alert: 2, campaignTrail: 4 };
    p['careless-loop-runs-out'] = fail('entry-seen-camera'); p['route-a-disable~no-waitress'] = fail('entry-seen-guard');
    // brief 059: over-thinning goes loud and leaves two behind; the stands' loud-wait twins (MC-13): the square's is lost before Vale is in, the yard's Go is refused
    p['careless-thin-all'] = { ...win(4493, 19), alert: 2, campaignTrail: 8, left: { scout: 'captured fighting', fixer: 'captured fighting' } }; // engine 0.35.1 (brief 039): the Scout left standing
    p['fight-loud-wait-square'] = { ...fail('enforcer-down'), fight: 'loud' };
    p['fight-loud-wait-yard'] = { ...win(4495, 9), outcome: 'withdrawn', carFreeS: undefined, fight: 'loud', valeIn: 4493, goRefused: [{ tick: 4494, code: 'car-held' }] };
    for (const s of L.manifest.scripts) if (s.expects.alert !== undefined && p[s.id]) p[s.id] = { ...p[s.id]!, alert: s.expects.alert }; // each ends at the Alert its manifest states
    return p;
  };
  /** Brief 018 §5: each board-and-go twin (<route>-board, the primary of A, B and G) plays as its fight-it-out route here, so a test
   *  edits the route and the twin follows (the closing marks reach the primaries). */
  const mirror = (p: Plays): Plays => { const q = { ...p }; for (const [k, v] of Object.entries(p)) { const m = k.match(/^(route-[a-z-]+?)(~.*)?$/); if (m && !m[1]!.endsWith('-board')) q[`${m[1]}-board${m[2] ?? ''}`] = v; } return q; };
  // brief 067: today's site places no patrol on an entry (suborned-sim facts), so its fail line is not counted unless a test says so
  // brief 039: the cell-down line is covered by Keel's K3-CELL-DOWN scenario, which passes here unless a test says otherwise
  const K3 = { 'K3-CELL-DOWN': 'K3-CELL-DOWN: ok' };
  const J = (p: Plays, o: Parameters<typeof judge>[4] = {}) => judge(L.manifest, L.scripts, mirror(p), [], { facts: { patrolWatchesEntry: false }, scenarios: K3, ...o });
  const ids = (p: Plays, o: Parameters<typeof judge>[4] = {}) => J(p, o).conditions.filter(c => !c.ok).map(c => c.id);
  /** the two stands of the Handover's site (brief 059 K6), as getawayAt reads them: the square's bay and the service drive */
  const square: Getaway = { id: 'square', car: [850, -4820], holdCm: 800, lane: [[850, -5600], [3850, -5600]], laneHalfCm: 600, posts: [{ id: 'ambush-1', at: [2175, -4425] }, { id: 'police-1', at: [-775, -4125] }] };
  const yard: Getaway = { id: 'service-drive', car: [6935, -1500], holdCm: 800, lane: [[8600, -1500], [8600, 1500]], laneHalfCm: 600, posts: [{ id: 'svc-ambush-1', at: [7735, -800] }, { id: 'svc-police-2', at: [5575, -1425] }] };
  test('the good plays pass every condition', () => { expect(ids(good())).toEqual([]); expect(ids(good(), { getaway: [square, yard] })).toEqual([]); });
  test('MC-0: a script that does not do what its manifest says', () => { const p = good(); p['fail-vale-dead'] = fail('sealed'); expect(ids(p)).toContain('MC-0'); });
  test('MC-1: one quiet route', () => { const p = good(); for (const g of ['route-g', 'route-g-fast', 'route-g-kill']) p[g] = fail('entry-seen-camera'); p['route-b'] = fail('vale-leaves'); p['route-b-thin'] = fail('vale-leaves'); expect(ids(p)).toContain('MC-1'); });
  test('MC-1: a shot before the final fight makes a win loud; the fight\'s own shots do not', () => {
    const p = good(); p['route-b'] = { ...p['route-b']!, shots: 1, fight: 'loud' }; p['route-b-thin'] = { ...p['route-b-thin']!, shots: 1 }; for (const g of ['route-g', 'route-g-fast', 'route-g-kill']) p[g] = { ...p[g]!, shots: 1 };
    expect(ids(p)).toContain('MC-1');
    expect(good()['route-a']!.gunfire).toBeGreaterThan(0); // the good plays fire in the fight and still count as quiet
  });
  test('MC-6: loud faster than every informed quiet win (T2); a loud win that leaves someone captured is not raced', () => {
    const p = good(); p.loud = { ...p.loud!, tick: 2000 }; expect(ids(p)).toEqual(['MC-6']);
    const q = good(); q['loud-fight'] = { ...q['loud-fight']!, tick: 2000, marks: { captured: 2000 }, campaignTrail: 4 }; expect(ids(q)).toEqual([]); // the capture variant is faster, and doesn't count
  });
  test('MC-2: a quiet variant (Pressure) as dear as loud fails, not only the primary routes', () => { const p = good(); p['route-a-pressure'] = { ...p['route-a-pressure']!, trail: 9 }; expect(ids(p)).toEqual(['MC-2', 'MC-10']); });
  test('MC-2: the loud route no dearer than a quiet one', () => { const p = good(); p.loud = { ...p.loud!, trail: 3 }; expect(ids(p)).toContain('MC-2'); });
  test('MC-2 counts Trail in total: a statement carried into the campaign makes a quiet route as dear as loud', () => { const p = good(); p['route-g'] = { ...p['route-g']!, campaignTrail: 7 }; expect(ids(p)).toEqual(['MC-2', 'MC-10']); });
  // brief 018 §5: quiet beats loud on Trail (a captive's +4 T is campaign Trail), Alert, Standing and the seal, against every loud win
  test('MC-10: a quiet win dearer than a loud variant, as alert, lower in Standing, or under a seal clock is not better', () => {
    const t = good(); t['loud-fight'] = { ...t['loud-fight']!, trail: 6 }; expect(ids(t)).toEqual(['MC-10']); // a loud variant, not the primary: MC-2 holds
    const a = good(); a['route-b-thin'] = { ...a['route-b-thin']!, alert: 2 }; expect(ids(a)).toEqual(['MC-0', 'MC-10']); // and not the Alert its manifest states
    const st = good(); st.loud = { ...st.loud!, standing: 3 }; st['route-a'] = { ...st['route-a']!, standing: 2 }; expect(ids(st)).toEqual(['MC-10']);
    const se = good(); se['route-g-kill'] = { ...se['route-g-kill']!, marks: { 'seal.starts': 2600 } }; expect(ids(se)).toEqual(['MC-10']);
    const cap = good(); cap['loud-fight'] = { ...cap['loud-fight']!, trail: 2, campaignTrail: 4 }; expect(ids(cap)).toEqual(['MC-10']); // 2 + the captive's 4 = 6: route-a-pressure's 6 is not under it
  });
  test('MC-0: a run that leaves behind someone other than its manifest says', () => {
    const p = good(); p['loud-left-standing'] = { ...p['loud-left-standing']!, left: { scout: 'captured sealed' } }; expect(ids(p)).toEqual(['MC-0']);
    const q = good(); q['loud-left-downed'] = { ...q['loud-left-downed']!, left: { scout: 'captured downed', tech: 'walked' } }; expect(ids(q)).toEqual(['MC-0']);
  });
  test('MC-11: an ambush spot or police stand inside the hold ring or the exit lane of any stand holds the car', () => {
    expect(ids(good(), { getaway: square })).toEqual([]);
    const near = { ...square, posts: [...square.posts, { id: 'ambush-2', at: [1300, -4500] as [number, number] }] }; // 5.5 m from the car
    expect(ids(good(), { getaway: near })).toEqual(['MC-11']);
    const lane = { ...square, posts: [...square.posts, { id: 'police-2', at: [3000, -5900] as [number, number] }] }; // 24 m out, but on the exit lane
    expect(J(good(), { getaway: lane }).conditions.find(c => c.id === 'MC-11')!.detail).toContain('square police-2 at 24.1 m HOLDS the car (in the exit lane)');
    const yardNear = { ...yard, posts: [...yard.posts, { id: 'svc-ambush-9', at: [6935, -1000] as [number, number] }] }; // brief 059 K6: the yard's posts are read too, against its own car
    expect(J(good(), { getaway: [square, yardNear] }).conditions.find(c => c.id === 'MC-11')!.detail).toContain('service-drive svc-ambush-9 at 5.0 m HOLDS the car');
    const unfree = good(); for (const k of Object.keys(unfree)) if (unfree[k]!.outcome === 'success') unfree[k] = { ...unfree[k]!, carFreeS: undefined };
    expect(ids(unfree, { getaway: square })).toEqual(['MC-11', 'MC-14']); // no blocker out logged: not free when it left (and the fight never came)
    expect(inLane(square, [3850, -5000])).toBe(true); expect(inLane(square, [3900, -5600])).toBe(false); expect(inLane(square, [850, -4820])).toBe(false);
  });
  test('MC-11 (brief 067, M067-SQ1): a hold logged by a body that did not come for the car fails; the blocker, a press, an advance and the response unit\'s join do not', () => {
    const at = (held: NonNullable<Digest['held']>) => { const p = good(); p['route-a'] = { ...p['route-a']!, held }; return ids(p, { getaway: square }); };
    expect(at([{ tick: 2400, how: 'blocker', who: 'rival-2' }, { tick: 2410, how: 'press', who: 'rival-1' }, { tick: 2420, how: 'advance', who: 'rival-5' }])).toEqual([]);
    expect(at([{ tick: 2400, how: 'answer', who: 'rival-4' }])).toEqual(['MC-11']); // an answer to fire never comes for the car
    expect(at([{ tick: 2400, how: 'post', who: 'rival-4' }])).toEqual(['MC-11']); // a rival at, or on his way to, his ambush spot
    expect(at([{ tick: 2400, how: 'post', who: 'officer-1' }])).toEqual(['MC-11']); // a final-fight officer to his police stand
    expect(at([{ tick: 2400, how: 'post', who: 'security-2', join: 'cv-033' }])).toEqual([]); // the response unit's join point: the cell waited at the car
    expect(at([{ tick: 2400, how: 'post', who: 'police-1' }])).toEqual([]); // a search unit at the stash it opens: no post the mission places
    expect(at([{ tick: 2400, how: 'answer', who: 'police-1' }])).toEqual(['MC-11']); // an answer to fire always counts
  });
  test('MC-13 (brief 059 M059-5.3): every stand needs a loud-wait twin that is held: lost before Vale is in, or its Go refused car-held with Vale aboard', () => {
    const both = { getaway: [square, yard] };
    const p = good(); p['fight-loud-wait-yard'] = { ...p['fight-loud-wait-yard']!, goRefused: [] }; expect(ids(p, both)).toEqual(['MC-13']); // Vale aboard and no Go refused
    const q = good(); q['fight-loud-wait-yard'] = { ...q['fight-loud-wait-yard']!, goRefused: [{ tick: 4490, code: 'car-held' }] }; expect(ids(q, both)).toEqual(['MC-13']); // refused before Vale was in: a withdrawal's refusal, not the hold at his Board
    const w = good(); w['fight-loud-wait-square'] = { ...win(2400, 8), alert: 2, fight: 'loud' }; expect(ids(w, both)).toEqual(['MC-0', 'MC-13']); // the square's twin wins: an unfought loud exit
    const n = good(); expect(ids(n, { getaway: [square, yard, { ...yard, id: 'pier' }] })).toEqual(['MC-13']); // a stand with no loud-wait twin
    expect(J(good(), both).conditions.find(c => c.id === 'MC-13')!.value).toBe('2/2 stands held');
  });
  test('MC-14 (brief 059 S1): a win that Goes before blocker.out, or with none logged, left unfought; variations count too', () => {
    const p = good(); p['route-g'] = { ...p['route-g']!, carFreeS: -2.5 }; expect(ids(p)).toEqual(['MC-14']);
    const q = good(); q['route-a~no-waitress'] = { ...win(3200, 3), carFreeS: undefined }; expect(ids(q)).toEqual(['MC-14']); // a variation's win the same
    expect(J(p).conditions.find(c => c.id === 'MC-14')!.detail).toContain('route-g: Go 2.5 s before blocker.out (UNFOUGHT)');
  });
  test('MC-15 (brief 059 M059-S1.5): a reference route that destroys the rival crew (rivals.wiped) fails; a careless one may', () => {
    const p = good(); p['route-g-kill'] = { ...p['route-g-kill']!, marks: { 'rivals.wiped': 2600 } }; expect(ids(p)).toEqual(['MC-15']);
    const q = good(); q['careless-thin-all'] = { ...q['careless-thin-all']!, marks: { 'rivals.wiped': 4400 } }; expect(ids(q)).toEqual([]);
  });
  test('MC-7: a careless takedown that wins quietly, or at Alert 2 no dearer than its twin, is not worse', () => {
    const p = good(); p['careless-reporter-not-stopped'] = { ...p['careless-reporter-not-stopped']!, alert: 0 }; expect(ids(p)).toContain('MC-7'); // at its twin's Alert
    const q = good(); q['careless-reporter-not-stopped'] = { ...q['careless-reporter-not-stopped']!, trail: 4 }; expect(ids(q)).toContain('MC-7');
  });
  test('MC-0: a script that ends at another Alert than its manifest states (brief 031: a loiterer reported is Alert 1, with no Trail)', () => {
    const p = good(); p['route-b'] = { ...p['route-b']!, alert: 1 }; expect(ids(p)).toContain('MC-0');
    expect(J(p).scripts.find(x => x.id === 'route-b')!.got).toBe('success at A 1');
  });
  test('MC-12: a quiet route reported for loitering, or a loiter twin that costs no more than its careful twin, fails', () => {
    const p = good(); p['route-b'] = { ...p['route-b']!, marks: { ...p['route-b']!.marks, 'attention.reporting': 1524 } }; expect(ids(p)).toEqual(['MC-12']);
    const q = good(); q['careless-scout-idles-pickup'] = { ...q['careless-scout-idles-pickup']!, marks: {} }; expect(ids(q)).toEqual(['MC-12']); // never reported
    const r = good(); r['careless-scout-idles-pickup'] = { ...r['careless-scout-idles-pickup']!, alert: 0 }; expect(ids(r)).toEqual(['MC-0', 'MC-12']); // a win at its twin's Alert
  });
  test('MC-8: takedown routes that win only above Alert 1, or cost the Enforcer no time, are not viable', () => {
    const p = good(); for (const s of ['route-g', 'route-g-fast', 'route-g-kill', 'route-b-thin']) p[s] = { ...p[s]!, alert: 2 }; expect(ids(p)).toContain('MC-8');
    const q = good(); q['route-g'] = { ...q['route-g']!, takedownS: 0 }; expect(ids(q)).toContain('MC-8');
  });
  test('MC-9 prices an open case (case-trail-eq): a hidden kill as fast and cheap as every turning win stops dominating at 3 T a case', () => {
    const p = good(); p['route-g-kill'] = { ...p['route-g-kill']!, tick: 2000, trail: 0, campaignTrail: 2, openCases: 1 }; p['route-g-fast'] = { ...p['route-g-fast']!, tick: 2900 }; p['route-g'] = { ...p['route-g']!, tick: 2900 };
    expect(J(p).conditions.find(c => c.id === 'MC-9')!.ok).toBe(false);
    expect(J(p, { caseTrailEq: 3 }).conditions.find(c => c.id === 'MC-9')!.ok).toBe(true);
  });
  test('MC-9: a takedown win as fast and as cheap as every turning win dominates them', () => { const p = good(); p['route-g-fast'] = { ...p['route-g-fast']!, tick: 2000, trail: 0, campaignTrail: 1 }; expect(ids(p)).toEqual(['MC-9']); });
  test('MC-3: a fail no script reaches', () => { const p = good(); p['fail-district-seals'] = fail('enforcer-down'); expect(ids(p)).toContain('MC-3'); });
  test('MC-3 counts the eight fails of GDD v0.28.0 §07, whatever the site facts say (no line needs one)', () => {
    const c = (o: Parameters<typeof judge>[4]) => J(good(), o).conditions.find(x => x.id === 'MC-3')!;
    for (const o of [{}, { facts: { patrolWatchesEntry: true } }, { facts: {} }]) expect([c(o).ok, c(o).value]).toEqual([true, '8/8']);
    expect(c({}).detail).not.toContain('entry seen by patrol');
  });
  test('MC-3 (brief 067): a fail line that needs a site fact still counts only where it holds; with no facts (an engine before 0.34.0) it counts', () => {
    // the mechanism stays (§09: a fail that needs a watcher the site doesn't place is not counted), shown on a hypothetical line
    const m = structuredClone(L.manifest); m.fails.push({ id: 'patrol-line', gdd: 'a line that needs a patrol on an entry', codes: ['entry-seen-patrol'], needs: 'patrol-watches-entry' });
    const c = (o: Parameters<typeof judge>[4]) => judge(m, L.scripts, mirror(good()), [], { facts: { patrolWatchesEntry: false }, scenarios: K3, ...o }).conditions.find(x => x.id === 'MC-3')!;
    expect([c({}).ok, c({}).value]).toEqual([true, '8/8 (1 not on this site)']);
    expect(c({}).detail).toContain('a line that needs a patrol on an entry: not on this site (no patrol watches an entry)');
    expect([c({ facts: { patrolWatchesEntry: true } }).ok, c({ facts: { patrolWatchesEntry: true } }).value]).toEqual([false, '8/9']);
    expect(c({ facts: {} }).ok).toBe(false);
  });
  test('MC-3 (brief 039, Keel\'s spec): the cell down is reached by K3-CELL-DOWN printing ": ok", and by nothing else', () => {
    expect(L.manifest.fails.find(f => f.id === 'cell-down')!.scenario).toBe('K3-CELL-DOWN');
    expect(L.manifest.scripts.filter(s => s.reaches === 'cell-down')).toEqual([]); // no route reaches it: the scenario covers it
    const c = (o: Parameters<typeof judge>[4]) => J(good(), o).conditions.find(x => x.id === 'MC-3')!;
    expect([c({}).ok, c({}).value]).toEqual([true, '8/8']);
    expect(c({}).detail).toContain('the cell down (every op down): scenario K3-CELL-DOWN');
    for (const line of ['K3-CELL-DOWN: FAIL DIFFERS', 'K3-CELL-DOWN: FAIL cell-down at the last fall', 'K3-CELL-DOWN: THREW InvalidOperationException: x', 'K3-CELL-DOWN: not in this engine', 'K3-CELL-DOWN: ok but']) {
      const r = c({ scenarios: { 'K3-CELL-DOWN': line } }); expect([r.ok, r.value], line).toEqual([false, '7/8']); expect(r.detail).toContain(`the cell down (every op down): NOT REACHED (${line})`);
    }
    expect(c({ scenarios: {} }).detail).toContain('NOT REACHED (scenario K3-CELL-DOWN not run)');
    // a route that reaches the line still counts, scenario or not
    const m = structuredClone(L.manifest), s = m.scripts.find(x => x.id === 'fight-safe-unguarded')!; s.expects.fail = 'cell-down'; s.reaches = 'cell-down';
    const p = good(); p['fight-safe-unguarded'] = fail('cell-down');
    const r = judge(m, L.scripts, mirror(p), [], { facts: {}, scenarios: {} }).conditions.find(x => x.id === 'MC-3')!;
    expect([r.ok, r.value]).toEqual([true, '8/8']);
  });
  test('the schema: a fail line\'s scenario is an engine scenario id', () => {
    const m = structuredClone(JSON.parse(L.texts['routes.json']!)); m.fails.find((f: { id: string }) => f.id === 'cell-down').scenario = 'k3 cell down';
    expect(() => Routes.parse(m)).toThrow();
  });
  test('MC-4: a contact without whom no quiet route wins', () => {
    const p = good(); p['route-b~no-waitress'] = fail('vale-leaves'); p['route-b-thin~no-waitress'] = fail('vale-leaves'); for (const g of ['route-g', 'route-g-fast', 'route-g-kill']) p[g] = fail('entry-seen-camera');
    const r = J(p);
    expect(r.conditions.find(c => c.id === 'MC-4')!.ok).toBe(false);
    expect(r.contacts.find(c => c.role === 'waitress')!.routesWithout).toEqual([]);
  });
  test('MC-4 names a contact only if it leaves fewer than 2 quiet routes (with route G, neither does)', () => {
    const r = J(good());
    expect(r.contacts.map(c => [c.role, c.needs, c.named])).toEqual([['waitress', ['A'], false], ['driver', ['B'], false]]); // route G turns nobody (brief 013), so neither contact leaves fewer than 2 quiet routes
    expect(r.conditions.find(c => c.id === 'MC-4')!.ok).toBe(true);
  });
  test('MC-5: Vale secured under a minute before a route\'s closing event', () => {
    const p = good(); p['route-b'] = win(4698, 4, { 'take.done': 4800 }); // taken 30 s before the 8:30 close
    expect(ids(p)).toEqual(['MC-5']);
    expect(assessment(J(p)).failures.map(f => f.rule)).toEqual(['MC-5']);
  });
});

describe.skipIf(!hasDotnet())('the Handover, played headless (Keel\'s engine on the working tree)', () => {
  test('route A and route B win quietly, the loud route costs more Trail than every quiet win, every fail is reached, and the check ships', async () => {
    const c = await check(L, engineAt('tree'), () => {}, { slack: false }); // the working tree's engine, rules, site and people: the route scripts are written against Keel's brief 018 engine (0.7.0: Board and Go), not yet committed; once it is, HEAD and the tree agree
    expect([c.plays['route-a']!.outcome, c.plays['route-b']!.outcome]).toEqual(['success', 'success']);
    expect([c.plays['route-a']!.gunfire, c.plays['route-b']!.gunfire]).toEqual([0, 0]);
    expect(c.report.conditions.filter(x => !x.ok).map(x => x.id)).toEqual([]);
    expect(c.plays.loud!.trail).toBeGreaterThan(c.plays['route-a-pressure']!.trail); // MC-2 against every quiet variant (Motive's addendum)
    // brief 018 §5: the board-and-go primaries leave nobody behind; the fight-it-out references leave three to walk out; loud leaves nobody
    for (const id of ['route-a-board', 'route-b-board', 'route-g-fast-board', 'loud', 'loud-fight']) expect([id, c.plays[id]!.left]).toEqual([id, undefined]); // c038: loud-fight's whole cell, the Enforcer's carbine too, downs the two officers before it boards
    for (const id of ['route-a', 'route-a-pressure', 'route-g', 'route-g-fast', 'route-g-kill']) expect([id, c.plays[id]!.left]).toEqual([id, { scout: 'walked', fixer: 'walked', tech: 'walked' }]); // brief 027 (tuning.md §10.5): fought out from the car's west corners, the three walk out
    for (const id of ['route-b', 'route-b-thin']) expect([id, c.plays[id]!.left]).toEqual([id, { scout: 'walked', fixer: 'walked' }]); // brief 059: at the yard the Tech drove the car round and is in it
    expect([c.plays['loud-left-downed']!.left, c.plays['loud-left-downed']!.campaignTrail]).toEqual([{ scout: 'captured downed' }, 4]);
    expect(Object.values(c.plays).every(p => p.replayIdentical)).toBe(true);
  });
  // The latest check pinned to a commit. Checks recorded on the working tree (c007-c011: the r016 cast, Keel's engine uncommitted)
  // replay only while the tree holds, and any uncommitted rule edit breaks them (c007 broke on the rulebook's sight section), so a
  // test pins a committed record: c038, brief 067 as committed in cb1604f6 (engine 0.34.1, GDD v0.26.0; c012 before it). On the next
  // engine commit, record the check against that commit and point this at it.
  test('the latest commit-pinned check (c038, pinned to cb1604f6) replays to its recorded hashes', async () => {
    const r = await replay(join(runsDir('handover'), 'c038'));
    expect(r.lines.filter(l => l.startsWith('FAIL'))).toEqual([]);
    expect(r.ok).toBe(true);
  }, 420_000); // about 900 plays (the slack scans included): about 25 s on the Studio
});
