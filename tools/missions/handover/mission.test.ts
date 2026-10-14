// The Handover mission file (contracts/missions/handover/mission.json): it parses as suborned.mission/2, its destinations,
// crew and routes are the ones moved out of the world but for the changes declared below, its timeline is GDD §07's
// schedule, and the overlay still passes.
// Preservation lock (Sky 2026-10-14): re-siting the Handover changes PLACES and MOVED or RENAMED; re-timing it changes the
// timeline test (GDD §07, entry for entry).
// Re-pinned 2026-10-14 (Waymark). Sky, 2026-10-13: "the handover mission is crucial we keep, though the timings and
// placements can be adjusted given we've regenerated the world". What moved: the Records Office doors now stand on its tower
// faces (brief 008 B2, Vantage), so the four door points in MOVED follow them; 'Steps' is 'Terrace gate' (the café door is
// flush, there are no steps; the point has not moved). No time moved: the schedule is GDD §07's clock offsets and names no
// leg and no walking pace, so none follows from a length that changed.
// Any other change still fails: PLACES pins the bytes as they are, and undoing MOVED and RENAMED must give back WAS, the
// bytes moved out of the world, so a further change has to be declared here before it passes.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { createHash } from 'node:crypto';
import { readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { Mission } from '../../../contracts/missions/mission.schema';
import { check } from '../../plan/checks';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { readTexts, world } from '../../people/inputs';
import { expand } from '../../people/expand';
import { standInCast } from '../../people/standin';
import { castRoles } from './cast';
import { overlay, type PeopleIn } from './overlay';

setDefaultTimeout(30000);
const root = resolve(import.meta.dir, '../../..'), FILE = 'contracts/missions/handover/mission.json';
const text = (p: string) => Bun.file(resolve(root, p)).text();
const raw = await text(FILE), mission = Mission.parse(JSON.parse(raw));
/** Destinations, crew and routes as written: the file from `"destinations"` to its end. */
const places = (s: string) => s.slice(s.indexOf('\n "destinations": ['));
const sha = (s: string) => createHash('sha256').update(s).digest('hex');
const WAS = 'db183672fada0e2cebc2328a37ed7623af55374085ce68f25a2c8ba00fa31e9d'; // sha256 of places() at commit 15af5dd (suborned.mission/1): as moved out of the world
const PLACES = 'beb0218692b618c80f6ba815e0b0dc5a2e054e7851325de1e96095d1d2b445fa'; // sha256 of places() now: WAS with MOVED and RENAMED applied
/** Every point re-sited since WAS: name, from, to (block frame, m). */
const MOVED: [name: string, from: [number, number], to: [number, number]][] = [
  ['Office approach', [52, -30.5], [48.6, -30]], // the foot of the public door steps, 0.6 m before the first riser
  ['Office entrance', [54.65, -30.5], [51.75, -30]], // the public door landing, 0.75 m off tower-public's north face
  ['Official entrance', [73.25, -31], [76.25, -30]], // the flush official door, 0.75 m off tower-official's south face
  ['Meet Vale', [73.25, -31], [76.25, -30]], // the same point as Official entrance, as before
];
/** Every point renamed since WAS, in place: from, to. */
const RENAMED: [from: string, to: string][] = [['Steps', 'Terrace gate']];
const point = (name: string, at?: number[]) => `"name": "${name}",` + (at ? `\n   "at": [${at.join(', ')}],` : '');
/** `s` with its one occurrence of `a` replaced by `b`. */
const swap = (s: string, a: string, b: string) => { expect(s.split(a).length - 1, a).toBe(1); return s.replace(a, b); };
const broken = (edit: (m: any) => void) => { const m = JSON.parse(raw); edit(m); return Mission.safeParse(m).success; };

describe('handover mission file', () => {
  test('parses as suborned.mission/2, and the schema rejects what it must', () => {
    expect(mission.schema).toBe('suborned.mission/3');
    expect(broken(() => {})).toBe(true);
    expect(broken(m => { m.schema = 'suborned.mission/2'; })).toBe(false); // /2 has no roles: migrate (mission.schema.ts)
    expect(broken(m => { delete m.timeline; })).toBe(false);
    expect(broken(m => { m.timeline[2].at = '17:05:30'; })).toBe(false); // a day time where an offset belongs
    expect(broken(m => { m.timeline.reverse(); })).toBe(false);
    expect(broken(m => { m.events[0].at = '5:31'; })).toBe(false); // the event has left its timeline entry
    expect(broken(m => { delete m.timeline[0].src; })).toBe(false);
    expect(broken(m => { m.cast = []; })).toBe(false); // unknown key
  });

  test('destinations, crew and routes are byte-for-byte the ones moved out of the world, but for the declared moves and rename', () => {
    expect(sha(places(raw))).toBe(PLACES);
    let was = places(raw);
    for (const [name, from, to] of MOVED) was = swap(was, point(name, to), point(name, from));
    for (const [from, to] of RENAMED) was = swap(was, point(to), point(from));
    expect(sha(was)).toBe(WAS);
  });

  test('the timeline is GDD §07\'s schedule, entry for entry', async () => {
    const gdd = readdirSync(resolve(root, 'GDD')).filter(f => /^the-suborned-agent-gdd-v.*\.md$/.test(f));
    expect(gdd).toHaveLength(1);
    const s07 = (await text(`GDD/${gdd[0]}`)).split(/^## /m).find(s => s.startsWith('§07 '))!;
    const schedule = s07.split('\n').find(l => l.startsWith('schedule: '))!.slice('schedule: '.length).replace(/\.$/, '').split(' · ');
    expect(mission.timeline.map(e => `${e.at} ${e.what}`)).toEqual(schedule);
    expect(mission.events.map(e => [e.id, e.at, e.windowS, e.surge])).toEqual([['records-handover', '5:30', 300, 1.8]]); // the chime's crowd
  });

  test('the day start is held provisional, and both timings and placements stay adjustable', () => {
    expect(mission.clock).toMatchObject({ startsAt: '17:00', provisional: true });
    expect(mission.clock.contradiction?.readings.map(r => r.chime)).toEqual(['17:05:30', '17:30']);
    expect(mission.status).toMatchObject({ state: 'preserved', adjustable: ['timings', 'placements'] });
  });

  test('the overlay still passes on the world (people that meet the roles), and every event names a plan area', async () => {
    const plan: Plan = JSON.parse(await text('contracts/world/harbour-district.plan.json')), block = JSON.parse(await text(plan.civic.block));
    const R = check(plan, derive(plan), civic(plan.civic, block), false, overlay(plan, block, met(await people()))), by = (id: string) => R.checks.find(c => c.id === id)!;
    expect(R.checks.filter(c => !c.ok).map(c => c.id)).toEqual([]);
    expect(by('C-3').value).toBe(`${mission.destinations.length}/${mission.destinations.length}`);
    expect(by('W-4').value).toBe('2/2'); // the overlay's walk origins (standards.walking.from): Café door, Office entrance
    expect(by('M-1').ok).toBe(true);
    expect(by('M-2').value).toBe(mission.roles.map(r => `${r.id} ${PINS[r.id]}`).join(', '));
    expect(R.routes.map(r => r.id)).toEqual(mission.routes.map(r => r.id));
    expect(R.routes.every(r => r.length < Infinity)).toBe(true);
    expect(mission.events.filter(e => !plan.areas.some(a => a.id === e.area)).map(e => e.id)).toEqual([]);
  }, 120000); // the whole plan check (walks included): 14-24 s on a busy machine
});

// Brief 010 D: the mission names roles, never people; the overlay casts each role from the generated people (here the stand-in
// cast, expanded in process, so the test does not read out/) and pins one person by id (rule M-2).
/** The people the stand-in cast expands to, as `bun run people` writes them. */
const people = async (): Promise<PeopleIn> => { const w = world(await readTexts('contracts/world/harbour-district.plan.json'), 'civic-block'); return expand(w.ctx(standInCast(w.scope, w.rules))).people; };
/** Who the stand-in cast pins once the waitress has the need (see `met`): the first who fits by id. A live cast (brief 010 C)
 *  moves these; the roles stay. The guard is p-records-office-29, the one guard whose routine stops at the café pre-shift. */
const PINS: Record<string, string> = { waitress: 'p-cafe-11', driver: 'p-records-office-01', guard: 'p-records-office-29', councillor: 'p-records-office-61' };
/** The stand-in fails one role condition: no café server on the 17:00 shift is in debt (Motive §3: Inez came out with
 *  `advancement`). The live run must meet it; here Inez is given the need so the rest can be tested. */
const STANDIN_FAILS = 'waitress: none of the 5 who fit (server of cafe on shift 0:00-8:00 at Café door) has need debt: p-cafe-11 advancement, p-cafe-12 care, p-cafe-14 papers, p-cafe-15 secret, p-cafe-16 advancement';
/** A copy of the people with `edit` applied to the named persons (default: Inez in debt, which the roles need). */
const met = (p: PeopleIn, edit: Record<string, (x: PeopleIn['persons'][number]) => void> = { 'p-cafe-11': x => { x.need = 'debt'; } }): PeopleIn => {
  const c: PeopleIn = structuredClone(p);
  for (const [id, f] of Object.entries(edit)) f(c.persons.find(x => x.id === id)!);
  return c;
};

describe('handover roles', () => {
  const setup = async () => {
    const plan: Plan = JSON.parse(await text('contracts/world/harbour-district.plan.json')), block = JSON.parse(await text(plan.civic.block));
    return { plan, block, civ: civic(plan.civic, block), d: derive(plan), p: await people() };
  };
  /** M-2 alone: the overlay's civic rules on the world, for a mission and people. */
  const m2 = (s: Awaited<ReturnType<typeof setup>>, m: any = mission, p: PeopleIn = s.p) =>
    overlay(s.plan, s.block, p, m).civic({ plan: s.plan, d: s.d, civ: s.civ } as never).find(c => c.id === 'M-2')!;

  test('the mission names four roles, by employer and position or role, and no person', () => {
    expect(mission.roles.map(r => [r.id, r.employer, r.position ?? r.role, r.at ?? null])).toEqual([
      ['waitress', 'cafe', 'server', 'Café door'], ['driver', 'records-office', 'official-driver', 'Meet driver'],
      ['guard', 'records-office', 'guard', 'Office entrance'], ['councillor', 'records-office', 'councillor', null],
    ]);
    // §07: Inez in debt and helped ($40,000), the driver needs cash and is turned ($40,000): purse $80,000; §05 the guard's café stop
    expect(mission.roles.map(r => [r.id, r.need ?? null, r.approach ?? null, r.regularOf ?? null])).toEqual([
      ['waitress', 'debt', 'help', null], ['driver', 'cash', 'help', null], ['guard', null, null, 'cafe'], ['councillor', null, null, null],
    ]);
    expect(broken(m => { m.roles[0].approach = 'bribe'; })).toBe(false); // an approach not in the rules' four
    expect(mission.roles.every(r => r.shift.from === '0:00' && r.shift.to === '8:00' && !r.pin)).toBe(true); // the whole mission; nothing pinned until a cast is accepted
    expect(raw).not.toMatch(/"p-[a-z]/); // no person id written in the mission
    expect(broken(m => { m.roles[0].role = 'guard'; })).toBe(false); // both position and role
    expect(broken(m => { delete m.roles[0].position; })).toBe(false); // neither
    expect(broken(m => { m.roles[0].at = 'Nowhere'; })).toBe(false); // a point the mission does not name
    expect(broken(m => { m.roles[1].id = 'waitress'; })).toBe(false); // duplicate role id
    expect(broken(m => { m.roles[0].pin = 'p-cafe-11'; m.roles[3].pin = 'p-cafe-11'; })).toBe(false); // one person, two roles
    expect(broken(m => { m.roles[0].pin = 'Inez'; })).toBe(false); // a pin is a person id
  });

  test('the stand-in cast fails M-2 on the waitress\'s need only (the live run must meet it)', async () => {
    const s = await setup(), c = m2(s);
    expect(c).toMatchObject({ ok: false, detail: STANDIN_FAILS });
    expect(c.value).toBe('waitress -, driver p-records-office-01, guard p-records-office-29, councillor p-records-office-61');
  });

  test('every role is cast, pinned by id, deterministically; the pinned names land on the pinned people', async () => {
    const s = { ...(await setup()) }; s.p = met(s.p);
    const c = m2(s);
    expect(c.ok).toBe(true);
    const a = castRoles(s.p.persons, mission, { toPlan: (q) => q, placeAt: () => undefined, polyOf: () => undefined }); // no place resolves: only the councillor (no point) casts
    expect(a.map(x => x.ok)).toEqual([false, false, false, true]);
    const shuffled = { ...s.p, persons: [...s.p.persons].reverse() }; // the tie-break is the id, not the file's order
    expect(m2(s, mission, shuffled).value).toBe(c.value);
    expect(c.value).toBe(mission.roles.map(r => `${r.id} ${PINS[r.id]}`).join(', '));
    const name = (id: string) => s.p.persons.find(p => p.id === id)!.name;
    expect(name(PINS.waitress!)).toStartWith('Inez '); // people.rules.json names.pinned: Inez is the waitress the mission casts
    expect(name(PINS.councillor!)).toEndWith(' Vale');
  });

  test('failing example: a role nobody can fill fails M-2, naming the role', async () => {
    const s = { ...(await setup()) }; s.p = met(s.p); const m = JSON.parse(raw);
    m.roles.push({ id: 'official-door-guard', what: 'a guard at the official door', employer: 'records-office', role: 'guard', at: 'Official entrance', near: 2, shift: { from: '0:00', to: '8:00' }, src: 'test' });
    expect(Mission.safeParse(m).success).toBe(true); // a valid mission: the role is well formed, nobody stands there
    const c = m2(s, m);
    expect(c.ok).toBe(false);
    expect(c.detail).toBe('official-door-guard: nobody is a guard of records-office on shift 0:00-8:00 at Official entrance');
  });

  test('failing examples for each role condition: need, a refused approach, a regular of the café, an unsound role', async () => {
    const s = await setup(), p = met(s.p);
    const one = (m: any, q: PeopleIn = p) => m2(s, m, q).detail!.split('; ').filter(x => !/ of \d+ on shift/.test(x)); // the failing roles only
    let m = JSON.parse(raw); m.roles[1].need = 'debt'; // the drivers on shift need cash, care, debt (p-records-office-46), cash
    expect(m2(s, m, p)).toMatchObject({ ok: true, value: expect.stringContaining('driver p-records-office-46') });
    m.roles[1].need = 'housing';
    expect(one(m)).toEqual(['driver: none of the 4 who fit (official-driver of records-office on shift 0:00-8:00 at Meet driver) has need housing: p-records-office-01 cash, p-records-office-39 care, p-records-office-46 debt, p-records-office-64 cash']);
    // Q4: the wrong temperament still works (a Mercenary helped), a refusal fails: an Idealist refuses Pay
    m = JSON.parse(raw); m.roles[0].approach = 'pay';
    const idealists = met(s.p, Object.fromEntries(['p-cafe-11', 'p-cafe-12', 'p-cafe-14', 'p-cafe-15', 'p-cafe-16'].map(id => [id, (x: any) => { x.need = 'debt'; x.temperament = 'idealist'; }])));
    expect(one(m, idealists)).toEqual(['waitress: none of the 5 who fit (server of cafe on shift 0:00-8:00 at Café door) has a temperament that does not refuse pay: p-cafe-11 idealist, p-cafe-12 idealist, p-cafe-14 idealist, p-cafe-15 idealist, p-cafe-16 idealist']);
    expect(m2(s, JSON.parse(raw), idealists).ok).toBe(true); // Help is refused by no temperament
    expect(one(m, met(idealists, { 'p-cafe-11': x => { x.temperament = 'shy'; } }))[0]).toContain('p-cafe-11 shy (unknown)'); // unknown = reject
    // §05: the guard is a regular of the café: without p-records-office-29's pre-shift stop nobody is; a regular tie restores one
    const noStop = met(p, { 'p-records-office-29': x => { x.routine.stops = x.routine.stops.filter(t => t.doing !== 'pre-shift'); } });
    expect(one(JSON.parse(raw), noStop)).toEqual(['guard: none of the 6 who fit (guard of records-office on shift 0:00-8:00 at Office entrance) has a regular of cafe (a regular tie to its staff or a pre-shift stop at it): p-records-office-15 neither, p-records-office-29 neither, p-records-office-30 neither, p-records-office-36 neither, p-records-office-57 neither, p-records-office-67 neither']);
    const tied = met(noStop, { 'p-records-office-36': x => { x.ties = [...(x.ties ?? []), { kind: 'regular', with: 'p-cafe-01' }]; } });
    expect(m2(s, JSON.parse(raw), tied)).toMatchObject({ ok: true, value: expect.stringContaining('guard p-records-office-36') });
    const workmate = met(noStop, { 'p-records-office-36': x => { x.ties = [{ kind: 'workmate', with: 'p-cafe-01' }]; } }); // only a regular tie counts
    expect(m2(s, JSON.parse(raw), workmate).ok).toBe(false);
    // a pin that lacks a condition names it; a role the rules cannot hold fails before anyone is cast
    m = JSON.parse(raw); m.roles[0].pin = 'p-cafe-12';
    expect(one(m)).toEqual(['waitress: pinned p-cafe-12 lacks need debt: p-cafe-12 care']);
    m = JSON.parse(raw); m.roles[0].need = 'fame';
    expect(one(m)).toEqual(['waitress: need fame is not in the rules\' needs']);
    m = JSON.parse(raw); m.roles[1].approach = 'pressure'; // Pressure leans on debt, never on cash
    expect(one(m)).toEqual(['driver: pressure does not act on need cash (metBy help, pay)']);
  });

  test('a pin that no longer fits fails, and so does a missing people file', async () => {
    const debt = (x: PeopleIn['persons'][number]) => { x.need = 'debt'; };
    const s = { ...(await setup()) }; s.p = met(s.p, { 'p-cafe-11': debt, 'p-cafe-14': debt }); const m = JSON.parse(raw);
    m.roles[0].pin = 'p-cafe-02'; // a server on the morning shift, gone by 17:00
    expect(m2(s, m).detail).toContain('waitress: pinned p-cafe-02 is no longer a server of cafe on shift 0:00-8:00 at Café door');
    m.roles[0].pin = 'p-cafe-14';
    expect(m2(s, m)).toMatchObject({ ok: true, value: expect.stringContaining('waitress p-cafe-14') }); // a pin that fits wins over the tie-break
    const other = { ...s.plan, version: '0.0.0-test' };
    expect(overlay(other, s.block, s.p).civic({ plan: other, d: s.d, civ: s.civ } as never).find(c => c.id === 'M-2')!.ok).toBe(false); // the people are of another plan version
  });
});
