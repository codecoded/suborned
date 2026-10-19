// Done-check for brief 022 (Pulse): the rounds and partner rules, the baked file, and what they do to the Handover's recorded routes.
// The engine does not read the rounds file yet, so this holds the data and the reference: the rules parse and copy the rulebook's numbers, the bake is deterministic and
// fits the ground, the round is what the rules say, the guard's visit is unmoved, and every recorded order on Inez is still taken where it was recorded.
// Run: bun test tools/routines
import { describe, expect, test } from 'bun:test';
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { peopleFiles } from '../levels/people/hash';
import { Grid, d2, leg, type Pt } from './walk';
import { parseRules } from './rules';
import { Hz, ROUNDS_CANDIDATE, ROUNDS_ENGINE, build, findMeetings, occupancy, pickStop, poseAt, readInputs, stopsOf, summary, type Events, type PeopleIn, type Rounds } from './rounds';

const root = join(import.meta.dir, '..', '..');
const read = (f: string) => readFileSync(join(root, f), 'utf8');
const inp = readInputs(), { rounds: R, text } = build(inp), rules = inp.rules;
const rulebook = JSON.parse(read('contracts/rules/rulebook.json')).values as Record<string, { v: number }>;
const peopleRules = JSON.parse(read('contracts/people/people.rules.json')) as { approaches: Record<string, { timeS: number }> };
const g = new Grid(inp.site.grid);
const sv = R.servers[inp.site.cast['waitress']!.id]!, sp = sv.spans[0]!;
const P = (n: string): Pt => inp.site.places[n]!.at;
const near = (a: Pt, b: Pt, m: number) => d2(a, b) <= (m * 100) ** 2;

describe('the rules (contracts/people/routines.rules.json)', () => {
  test('parse with the schema; unknown = reject', () => {
    expect(rules.schema).toBe('suborned.routines.rules/1');
    const raw = JSON.parse(inp.rulesText);
    expect(() => parseRules(JSON.stringify({ ...raw, extra: 1 }))).toThrow();
    expect(() => parseRules(JSON.stringify({ ...raw, rounds: { ...raw.rounds, rest: { ...raw.rounds.rest, proposed: true, reason: undefined } } }))).toThrow(); // a proposed rule gives its reason
  });
  test('the numbers that copy the rulebook equal it', () => {
    expect(rules.rounds.pace.cmPerS).toBe(rulebook['op-speed-cm-s']!.v);
    expect(rules.partners.summoned.stay.s).toBe(rulebook['lover-stay-s']!.v);
    expect(rules.partners.closeCm.v / 100).toBeLessThan(rulebook['together-m']!.v); // a Watch that ends with them standing together still teaches the affair
    if (rulebook['partners-clear-cm']) expect(rules.partners.clearCm.v).toBe(rulebook['partners-clear-cm'].v); // Keel adds the value with the reader (brief 022 addendum)
  });
  test('the rest are inside what the engine can walk and a person can see', () => {
    expect(rules.rounds.rest.s[0]).toBeGreaterThanOrEqual(rules.rounds.holds.lingerS); // a recorded chain re-issues inside the linger; a rest is never shorter
    expect(rules.partners.chance.noticeM).toBeLessThan(rulebook['witness-range-m']!.v);
    expect(rules.rounds.opening.settleS).toBeGreaterThanOrEqual(30);
  });
});

describe('the bake', () => {
  test('is deterministic: the same inputs make the same bytes, and the candidate file is what they make', () => {
    expect(build(readInputs()).text).toBe(text);
    const cand = join(root, ROUNDS_CANDIDATE);
    if (existsSync(cand)) expect(read(ROUNDS_CANDIDATE)).toBe(text);
    const live = join(root, ROUNDS_ENGINE);
    // once Keel moves the addendum's file (guard.stops) the engine's copy is held to the same bytes; until then it is the file before the addendum and the engine still reads that one
    if (existsSync(live) && JSON.parse(read(ROUNDS_ENGINE)).guard.stops) expect(read(ROUNDS_ENGINE)).toBe(text);
  });
  test('builds nothing the people hash reaches (the people verdicts and out/people stay as they were)', () => {
    expect(peopleFiles().filter((f) => /routines|rounds/.test(f))).toEqual([]);
  });
  test('finds the Handover\'s waitress, at the café door, and no one else in the mission window', () => {
    expect(Object.keys(R.servers)).toEqual([inp.site.cast['waitress']!.id]);
    expect(sv.post).toBe('cafe-entrance');
    expect(sv.at).toEqual(P('d:Café door')); // her post is the mission's Café door point: the summons, M-2 and every window keep their ground
    expect(sp.excursions.length).toBeGreaterThanOrEqual(6);
  });
  test('every service point is free ground, clear of the chairs, facing the table, and the walk to it is the grid\'s', () => {
    for (const e of sp.excursions) {
      expect(g.isFree(e.at)).toBe(true);
      const chairs = Object.entries(inp.people.places).filter(([k]) => k.startsWith(e.table + '-seat-')).map(([, p]) => p);
      expect(chairs.length).toBe(3);
      for (const c of chairs) expect(d2(c, e.at)).toBeGreaterThanOrEqual(rules.rounds.servicePoint.clearOfSeatCm ** 2);
      const c = inp.tables[e.table]!; // she faces the table's centre
      const want = [c[0] - e.at[0], c[1] - e.at[1]], l = Math.hypot(want[0]!, want[1]!), f = Math.hypot(e.face[0], e.face[1]);
      expect((want[0]! * e.face[0] + want[1]! * e.face[1]) / (l * f)).toBeGreaterThan(0.99);
      const path = leg(g, sv.at, e.at);
      expect(e.path).toEqual(path.points);
      expect(e.out).toBe(Math.max(1, Math.ceil(e.m / rules.rounds.pace.cmPerS - 1e-9)));
      expect(e.out * rules.rounds.pace.cmPerS).toBeGreaterThanOrEqual(e.m); // never faster than the cell walks
      expect(e.m).toBeLessThanOrEqual(rules.rounds.leashM.v * 100);
      expect(e.back).toBe(e.out);
    }
  });
});

describe('the round is what the rules say', () => {
  const occ = occupancy(inp.people, new Set(Object.values(inp.site.cast).map((c) => c.id).filter((x): x is string => typeof x === 'string')), 'cafe-table-');
  const seated = (table: string, t: number) => !!occ[table]?.some(([a, b]) => t >= a && t < b);
  test('no excursion starts before the opening, and each rests at her post first', () => {
    const r = rules.rounds;
    expect(sp.excursions[0]!.start).toBeGreaterThanOrEqual(r.opening.settleS);
    sp.excursions.forEach((e, i) => {
      expect(e.serve).toBeGreaterThanOrEqual(r.serve.s[0]); expect(e.serve).toBeLessThanOrEqual(r.serve.s[1]);
      if (i > 0) { const p = sp.excursions[i - 1]!; expect(e.start - (p.start + p.out + p.serve + p.back)).toBeGreaterThanOrEqual(r.rest.s[0]); }
    });
  });
  test('she goes where people sit, and never to an empty table', () => {
    for (const e of sp.excursions) { expect(seated(e.table, sp.origin + e.start)).toBe(true); expect(seated(e.table, sp.origin + e.start + e.out)).toBe(true); }
  });
  test('she is at her post about half the time, and back within a minute', () => {
    const s = summary(R, inp)!;
    expect(s.share.atPost).toBeGreaterThan(45); expect(s.share.walking + s.share.serving).toBeGreaterThan(30);
    expect(s.longestAwayS).toBeLessThanOrEqual(60);
    expect(s.reachShare.listenAtBesideInez3m).toBeGreaterThan(50); // the Scout's Listen spot has her about as often as her door does
  });
  test('the reference pose: free, held, called and ended', () => {
    const E = sp.excursions[0]!, at0 = (sp.origin + E.start) * Hz;
    expect(poseAt(sv, R.rules, 0).phase).toBe('at post');
    expect(poseAt(sv, R.rules, at0 + E.out * Hz + 5).phase).toBe('serving');
    // held: stands where she was, turned to the operative, for the order and the linger; then the round goes on from the same moment
    const hold = { from: at0 + 50, to: at0 + 470, face: [100, 0] as Pt }, ev: Events = { holds: [hold] };
    const was = poseAt(sv, R.rules, hold.from, ev).pos;
    for (let t = hold.from; t < hold.to + R.rules.lingerS * Hz; t += 20) expect(poseAt(sv, R.rules, t, ev)).toMatchObject({ pos: was, phase: 'held' });
    expect(poseAt(sv, R.rules, hold.to + R.rules.lingerS * Hz + Hz * E.out, ev).phase).toBe('serving');
    // a chain of orders landing at the tick the next is taken is one hold (route A: Listen, Help, Help)
    const chain: Events = { holds: [hold, { from: hold.to, to: hold.to + 500, face: [100, 0] }, { from: hold.to + 500, to: hold.to + 1000, face: [100, 0] }] };
    for (let t = hold.from; t < hold.to + 1000; t += 50) expect(poseAt(sv, R.rules, t, chain).pos).toEqual(was);
    // called mid-walk: back at her post inside her own leg's seconds, waits, stands with him, the round resumes at a rest
    const c0 = at0 + Math.floor(E.out * Hz * 0.5), walk = Math.round((R.guard.stops[0]!.walkCm / R.rules.paceCmPerS) * Hz);
    const call: Events = { call: { at: c0, arrive: c0 + walk, until: c0 + walk + R.rules.stayS * Hz, guardStop: R.guard.stopAt } };
    expect(poseAt(sv, R.rules, c0 + E.out * Hz, call).pos).toEqual(sv.at);
    expect(poseAt(sv, R.rules, c0 + walk + 10, call).phase).toBe('together');
    expect(poseAt(sv, R.rules, c0 + walk + R.rules.stayS * Hz + R.rules.partS * Hz + 5, call).phase).toBe('at post');
    // ended in the middle of a pause: back at the post, and never leaves it
    const end: Events = { end: { at: at0 + E.out * Hz + 20 } };
    let back = -1;
    for (let t = end.end!.at; t < end.end!.at + 3000; t += 10) { const p = poseAt(sv, R.rules, t, end); if (back < 0 && p.phase === 'at post') back = t; if (back >= 0) expect(p.pos).toEqual(sv.at); }
    expect(back).toBeGreaterThan(0); expect(back - end.end!.at).toBeLessThanOrEqual(E.back * Hz);
  });
  test('the vectors in the file are the reference', () => {
    expect(Object.keys(R.vectors)).toEqual([inp.site.cast['waitress']!.id]);
    for (const rows of Object.values(R.vectors[inp.site.cast['waitress']!.id]!)) expect(rows.length).toBeGreaterThan(10);
    const row = R.vectors[inp.site.cast['waitress']!.id]!.free![20]!, p = poseAt(sv, R.rules, row[0]);
    expect([row[1], row[2], row[5]]).toEqual([p.pos[0], p.pos[1], p.phase]);
  });
});

describe('the guard\'s visit is the one the Handover was built on', () => {
  test('she is back at her post before he is, from the farthest table', () => {
    const farthest = Math.max(...sp.excursions.map((e) => e.out)), walkS = R.guard.walkCm / R.rules.paceCmPerS;
    expect(farthest).toBeLessThan(walkS - 20);
    for (const t of Object.keys(inp.tables)) { const s = sp.excursions.find((e) => e.table === t); if (s) expect(s.out).toBeLessThan(walkS - 20); }
  });
  test('the walk is the mission\'s Office entrance to Café door walk to a stop at her side, inside the summons\' limit at any table', () => {
    const whole = leg(g, P(inp.site.guard.post), sv.at);
    expect(R.guard.walkCm).toBe(Math.round(whole.length / 10)); // the summons' own walk, to her post, unchanged
    expect(R.guard.walkCm / R.rules.paceCmPerS).toBeLessThan(rulebook['summon-walk-max-s']!.v);
    const farS = (R.guard.walkCm + Math.max(...sp.excursions.map((e) => e.m))) / R.rules.paceCmPerS; // if the engine ever summoned him to her wherever she stands
    expect(farS).toBeLessThan(rulebook['summon-walk-max-s']!.v);
    for (const st of R.guard.stops) expect(st.walkCm - R.guard.walkCm).toBeLessThanOrEqual(R.rules.closeCm); // a stop ringCm from her post is never more than ringCm farther to walk to
    expect((R.guard.stops[0]!.walkCm + R.rules.closeCm) / R.rules.paceCmPerS).toBeLessThan(rulebook['summon-walk-max-s']!.v);
  });
});

describe('where the summoned partner stands (partners.summoned.side; brief 022 addendum)', () => {
  const stops = R.guard.stops, beside = P('beside-inez'), court = P('d:Courtyard');
  test('the stops are on the ring, on free ground, walkable, inside together-m, and the first is the default', () => {
    expect(stops.length).toBeGreaterThan(1);
    expect(R.guard.ringCm).toBe(R.rules.closeCm);
    for (const st of stops) {
      expect(Math.abs(Math.hypot(st.at[0] - sv.at[0], st.at[1] - sv.at[1]) - R.rules.closeCm)).toBeLessThan(2);
      expect(g.isFree(st.at)).toBe(true);
      expect(near(st.at, sv.at, rulebook['together-m']!.v)).toBe(true);
      expect(st.face[0] * (sv.at[0] - st.at[0]) + st.face[1] * (sv.at[1] - st.at[1])).toBeGreaterThan(0); // he faces her
    }
    expect(R.guard.stopAt).toEqual(stops[0]!.at); expect(R.guard.face).toEqual(stops[0]!.face);
    expect(new Set(stops.map((s) => s.at.join(','))).size).toBe(stops.length);
  });
  test('most lateral first, her left before her right, and the front before the back on a tie', () => {
    const keys = stops.map((s) => Math.abs(Math.abs(s.deg) - 90) * 4 + (Math.abs(s.deg) < 90 ? 0 : 1) * 2 + (s.deg >= 0 ? 0 : 1));
    expect([...keys].sort((a, b) => a - b)).toEqual(keys);
    expect(stops[0]!.deg).toBeGreaterThan(0); // her left: east of a south-facing post
    expect(stops[0]!.at[1]).toBeGreaterThan(sv.at[1]);
  });
  test('the alcove is two metres wide, so only the front quarter is free at 1.2 m; the list is rebuilt from the grid, never typed', () => {
    for (const st of stops) expect(Math.abs(st.deg)).toBeLessThanOrEqual(45);
    expect(stopsOf(g, P(inp.site.guard.post), sv.at, sv.face, R.rules.closeCm, rules.partners.summoned.side.stepDeg)).toEqual(stops);
  });
  test('Waymark\'s play note: with the Scout at beside-inez (2.1 m from her) he no longer stops on top of the Scout', () => {
    const was = Math.hypot(sv.at[0] + R.rules.closeCm - beside[0], sv.at[1] - beside[1]); // the old stop: 1.2 m straight ahead of her post, 0.9 m from the Scout
    expect(was).toBeLessThan(100);
    const pick = pickStop(stops, [beside, court], R.rules.clearCm)!;
    expect(pick).toBe(stops[0]);
    expect(Math.hypot(pick.at[0] - beside[0], pick.at[1] - beside[1])).toBeGreaterThanOrEqual(R.rules.clearCm * 1.4);
  });
  test('the pick: the first stop nobody stands near; her other hand if someone does; the farthest if all are taken; nothing if the file has none', () => {
    const at = (i: number): Pt => stops[i]!.at, nudge = (p: Pt, d: number): Pt => [p[0] + d, p[1]];
    expect(pickStop(stops, [], 100)).toBe(stops[0]);
    expect(pickStop(stops, [nudge(at(0), 40)], 100)).toBe(stops[1]); // a person on the first stop: the other side
    expect(pickStop(stops, [at(0), at(1)], 100)).toBe(stops[stops.length - 1]); // both sides taken: every stop of a 1.2 m ring is within a metre of one of them, so the farthest from both: dead ahead
    expect(pickStop(stops, [at(0)], 100)).toBe(stops[1]); // one side taken: the other
    const all = stops.map((s, i) => nudge(s.at, 10 + i)); // everyone taken: the stop whose nearest person is farthest, the earlier on a tie
    const nearest = (s: (typeof stops)[number]) => Math.min(...all.map((p) => Math.hypot(p[0] - s.at[0], p[1] - s.at[1])));
    const best = Math.max(...stops.map((s) => Math.floor(nearest(s))));
    expect(Math.floor(nearest(pickStop(stops, all, 100)!))).toBe(best);
    expect(pickStop([], [at(0)], 100)).toBeUndefined();
    expect(pickStop(stops, [nudge(at(0), 100)], 100)).toBe(stops[0]); // exactly clearCm away is clear (>=)
  });
  test('deterministic: a fixed order, so the same people give the same stop', () => {
    const ppl: Pt[] = [beside, court, [at0(), 0]];
    expect(pickStop(stops, ppl, 100)).toBe(pickStop([...stops], [...ppl], 100));
    function at0(): number { return stops[0]!.at[0]; }
  });
});

describe('what the quiet play can reach (recorded routes, taken against the reference)', () => {
  // The Inez orders of every recorded route, in order, each against where the reference puts her. Durations: the rulebook's. An order is taken where it was recorded if she is
  // inside its reach then; otherwise it waits. This is a static read of the scripts, not an engine run: the engine run is Keel's, when he reads the file.
  const routes = join(root, 'contracts/missions/handover/routes');
  type Order = { tick: number; op: string; do: string; target?: string; approach?: string; to?: string | number[] };
  const scripts = readdirSync(routes).filter((f) => f.endsWith('.json') && f !== 'routes.json').map((f) => ({ f, s: JSON.parse(readFileSync(join(routes, f), 'utf8')) as { orders?: Order[] } })).filter((x) => x.s.orders?.some((o) => o.target === 'waitress'));
  const dur = (o: Order) => (o.do === 'listen' ? rulebook['listen-s']!.v : o.do === 'turn' ? peopleRules.approaches[o.approach!]!.timeS : 0);
  const reachM = (o: Order) => (o.do === 'watch' ? rulebook['act-reach-m']!.v : o.do === 'listen' ? rulebook['listen-reach-m']!.v : rulebook['approach-reach-m']!.v);
  function walk(orders: Order[]) {
    const where = new Map<string, Pt>(), holds: NonNullable<Events['holds']> = [], taken: { o: Order; tick: number; waited: number }[] = [];
    let shift = 0;
    for (const o of orders) {
      if (o.do === 'move') { const to = typeof o.to === 'string' ? inp.site.places[o.to]?.at : (o.to as Pt | undefined); if (to) where.set(o.op, to); continue; }
      if (o.target !== 'waitress' || o.do === 'call') continue;
      const op = where.get(o.op); if (!op) throw new Error(`${o.op} has no place at tick ${o.tick}`);
      let tick = o.tick + shift;
      for (;; tick++) {
        const p = poseAt(sv, R.rules, tick, { holds });
        if (near(p.pos, op, reachM(o))) { if (dur(o)) holds.push({ from: tick, to: tick + dur(o) * Hz, face: [op[0] - p.pos[0], op[1] - p.pos[1]] }); break; }
        if (tick > o.tick + shift + 3000) throw new Error(`${o.do} never reaches her`);
      }
      taken.push({ o, tick, waited: tick - o.tick - shift }); shift = tick - o.tick;
    }
    return taken;
  }
  test('there are recorded routes that touch Inez, and each of their orders is taken at its recorded tick', () => {
    expect(scripts.length).toBe(7); // route A (Help, Pressure, and their board variants) and the three fail-entry-* routes
    for (const { f, s } of scripts) for (const t of walk(s.orders!)) expect([f, t.o.tick, t.o.do, t.waited]).toEqual([f, t.o.tick, t.o.do, 0]);
  });
  test('the check can fail: a Help given while she is at a table is refused by reach', () => {
    const E = sp.excursions[0]!, tick = (sp.origin + E.start + E.out + 3) * Hz; // mid-pause, at table 3's service point
    expect(near(poseAt(sv, R.rules, tick).pos, P('d:Courtyard'), rulebook['approach-reach-m']!.v)).toBe(false);
    const late = walk([{ tick: 0, op: 'fixer', do: 'move', to: 'd:Courtyard' }, { tick, op: 'fixer', do: 'turn', target: 'waitress', approach: 'help' }]);
    expect(late[0]!.waited).toBeGreaterThan(0); // she is back, and in reach, a few seconds on
    expect(late[0]!.waited).toBeLessThanOrEqual(30 * Hz);
  });
});

describe('partners who meet', () => {
  const g2 = g;
  const mk = (stopsA: PeopleIn['people'][number]['stops'], stopsB: PeopleIn['people'][number]['stops'], jobA = 'resident', jobB = 'resident', places?: Record<string, Pt>): PeopleIn => ({
    startsAt: 0,
    places: { 'sq-a': P('d:Terrace gate'), 'sq-b': P('d:Courtyard'), 'far-b': P('d:Fountain'), 'cafe-entrance': P('d:Café door'), ...places },
    people: [{ id: 'a', name: 'A', job: jobA, ties: [['lover', 'b']], stops: stopsA }, { id: 'b', name: 'B', job: jobB, ties: [['lover', 'a']], stops: stopsB }],
  });
  test('two partners on the street within noticeM stop, close and facing each other, once', () => {
    const m = findMeetings(g2, mk([['sq-a', -100, 500, 1]], [['sq-b', -100, 500, 1]]), new Set(), rules, 510);
    expect(m.length).toBe(1);
    const x = m[0]!; expect(x.start).toBe(0);
    expect(Math.abs(Math.hypot(x.atA[0] - x.atB[0], x.atA[1] - x.atB[1]) - rules.partners.closeCm.v)).toBeLessThan(40);
    expect(x.faceA[0] * x.faceB[0] + x.faceA[1] * x.faceB[1]).toBeLessThan(0); // opposite: each faces the other
    expect(g2.isFree(x.atA) && g2.isFree(x.atB)).toBe(true);
    expect(x.stay).toBe(rules.partners.chance.stayS);
  });
  test('not when they are too far apart, when one is on a post, or when one is a cast role', () => {
    expect(findMeetings(g2, mk([['sq-a', -100, 500, 1]], [['far-b', -100, 500, 1]]), new Set(), rules, 510)).toEqual([]);
    expect(findMeetings(g2, mk([['sq-a', -100, 500, 1]], [['cafe-entrance', -100, 500, 1]], 'resident', 'server'), new Set(), rules, 510)).toEqual([]);
    expect(findMeetings(g2, mk([['sq-a', -100, 500, 1]], [['sq-b', -100, 500, 1]]), new Set(['b']), rules, 510)).toEqual([]);
  });
  test('the Harbour cast has two lover pairs and no chance meeting in the mission window; the guard is met at his door only by the summons', () => {
    expect(R.meetings).toEqual([]);
  });
});
