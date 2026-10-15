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
import { Mission, offsetS } from '../../../contracts/missions/mission.schema';
import { check } from '../../plan/checks';
import { civic, toPlanFn } from '../../plan/civic';
import type { V } from '../../plan/geom';
import { derive, type Plan } from '../../plan/plan';
import { readTexts, world } from '../../people/inputs';
import { expand } from '../../people/expand';
import { standInCast } from '../../people/standin';
import { castRoles } from './cast';
import { castRules as castRulesFile, overlay, type PeopleIn } from './overlay';
import { engineWalkS, summonRules } from './summon';

setDefaultTimeout(30000);
const root = resolve(import.meta.dir, '../../..'), FILE = 'contracts/missions/handover/mission.json';
const text = (p: string) => Bun.file(resolve(root, p)).text();
const raw = await text(FILE), mission = Mission.parse(JSON.parse(raw));
/** Destinations, crew and routes as written: the file from `"destinations"` to its end. */
const places = (s: string) => s.slice(s.indexOf('\n "destinations": ['));
const sha = (s: string) => createHash('sha256').update(s).digest('hex');
const WAS = 'db183672fada0e2cebc2328a37ed7623af55374085ce68f25a2c8ba00fa31e9d'; // sha256 of places() at commit 15af5dd (suborned.mission/1): as moved out of the world
const PLACES = '0456823d4d8f845ca7dbb4c5b18750e813e41b2cbf2772b40d4f134e0bd725b0'; // sha256 of places() now: WAS with MOVED and RENAMED applied
/** Every point re-sited since WAS: name, from, to (block frame, m). */
const MOVED: [name: string, from: [number, number], to: [number, number]][] = [
  ['Office approach', [52, -30.5], [48.6, -30]], // the foot of the public door steps, 0.6 m before the first riser
  ['Office entrance', [54.65, -30.5], [51.75, -30]], // the public door landing, 0.75 m off tower-public's north face
  ['Official entrance', [73.25, -31], [76.25, -30]], // the flush official door, 0.75 m off tower-official's south face
  ['Meet Vale', [73.25, -31], [76.25, -30]], // the same point as Official entrance, as before
  ['Under parasol', [-7.92, -23.88], [-8.1, -23.7]], // off cafe-table-2's footprint (1.16 m from its centre, inside body clearance) onto free ground 1.41 m from it, still under the parasol's canopy, 0.3 m inside its ring (Pulse, Aster; Vantage's civic-dress test: the canopy is overhead)
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
    expect(broken(m => { m.clock.provisional = true; })).toBe(false); // a ruled start is settled
    expect(broken(m => { m.windows[0].walk.s = 30; })).toBe(false); // the walk is its metres at its pace, not the GDD's figure
    expect(broken(m => { m.windows[0].windowS = 120; })).toBe(false); // out, stay and back
    expect(broken(m => { m.windows[0].role = 'doorman'; })).toBe(false); // a role the mission does not name
    expect(broken(m => { m.roles[1].tiedTo[0].role = 'driver'; })).toBe(false); // a tie to one's own role
    expect(broken(m => { m.roles[1].waitsFor = 'waitress'; })).toBe(false); // waits for a role that never leaves for a point
    expect(broken(m => { m.roles[3].leavesTo.at = 'Nowhere'; })).toBe(false);
    expect(broken(m => { delete m.roles[2].at; })).toBe(false); // alone, and nowhere
    // the engine's points and cameras (brief 011 B, Suture O-6): mission content, read into the site by tools/sim/site.ts
    expect(mission.points!.map(p => p.id)).toEqual(['cabinet', 'beside-inez', 'rival-park', 'rival-entry', 'security-entry', 'patrol-a', 'patrol-b', 'patrol-reroute', 'block-spot',
      'ambush-1', 'ambush-2', 'ambush-3', 'ambush-4', 'ambush-5', 'police-1', 'police-2', 'police-3', 'police-4', 'guard-ally']); // the final fight's posts since engine 0.4.0
    expect(mission.arms?.sidearms).toBe(3); // GDD §07 Arms 3
    expect(mission.cameras!.map(c => `${c.id}>${c.looksAt}`)).toEqual(['entrance>Office entrance', 'pickup>Official drop-off']);
    expect(broken(m => { m.points[0].structure = 'civic-control-cabinet'; })).toBe(false); // a structure's centre and a point at once
    expect(broken(m => { m.points[1].id = 'cabinet'; })).toBe(false);
    expect(broken(m => { m.cameras[0].looksAt = 'Nowhere'; })).toBe(false);
    expect(broken(m => { delete m.points[2].reason; })).toBe(false); // every point says why it stands there
  });

  test('destinations, crew and routes are byte-for-byte the ones moved out of the world, but for the declared moves and rename', () => {
    expect(sha(places(raw))).toBe(PLACES);
    let was = places(raw);
    for (const [name, from, to] of MOVED) was = swap(was, point(name, to), point(name, from));
    for (const [from, to] of RENAMED) was = swap(was, point(to), point(from));
    expect(sha(was)).toBe(WAS);
  });

  test('the timeline is GDD §07\'s schedule, entry for entry, but for the entries retimed with the world (each declared here)', async () => {
    const gdd = readdirSync(resolve(root, 'GDD')).filter(f => /^the-suborned-agent-gdd-v.*\.md$/.test(f));
    expect(gdd).toHaveLength(1);
    const s07 = (await text(`GDD/${gdd[0]}`)).split(/^## /m).find(s => s.startsWith('§07 '))!;
    const schedule = s07.split('\n').find(l => l.startsWith('schedule: '))!.slice('schedule: '.length).replace(/\.$/, '').split(' · ');
    /** Sky 2026-10-15 (brief 011 Decisions): the close moves 8:00 → 8:30; GDD §07 text owed in the next version. */
    const RETIMED: Record<string, string> = { 'rivals take Vale or car leaves = over': '8:30' };
    expect(mission.timeline.map(e => `${RETIMED[e.what] && e.at === RETIMED[e.what] ? schedule.find(x => x.endsWith(` ${e.what}`))!.split(' ')[0] : e.at} ${e.what}`)).toEqual(schedule);
    for (const [what, at] of Object.entries(RETIMED)) expect(mission.timeline.find(e => e.what === what)).toMatchObject({ at, note: expect.stringContaining('Retimed') });
    expect(mission.events.map(e => [e.id, e.at, e.windowS, e.surge])).toEqual([['records-handover', '5:30', 300, 1.8]]); // the chime's crowd
  });

  test('the day start is Sky\'s ruling: the 5:30 chime is the Office\'s 17:30 shift end; timings and placements stay adjustable', async () => {
    expect(mission.clock).toMatchObject({ startsAt: '17:24:30', provisional: false, band: 'peak', ruling: { by: 'Sky', on: '2026-10-15', was: '17:00' } });
    expect(mission.clock.contradiction).toBeUndefined(); // the ruling settled it
    const life = JSON.parse(await text('contracts/world/harbour-district.life.json'));
    const chime = mission.timeline.find(e => e.what === 'handover chime')!.at, [h, mi, se] = mission.clock.startsAt.split(':').map(Number);
    expect(h! * 3600 + mi! * 60 + se! + offsetS(chime)).toBe(17.5 * 3600); // 17:24:30 + 5:30 = 17:30
    const units = ((await people()) as unknown as { units: { id: string; shifts: { from: number; to: number }[] }[] }).units;
    expect(units.find(u => u.id === 'records-office')!.shifts.map(x => x.to)).toEqual([17.5 * 60]); // the Office's one shift ends at 17:30
    const band = life.clock.bands.find((b: any) => b.id === mission.clock.band); expect(h! >= band.from && h! < band.to).toBe(true);
    expect(mission.status).toMatchObject({ state: 'preserved', adjustable: ['timings', 'placements'] });
  });

  test('route A\'s window reads the rules: the summons\' stay and walk limit, the walk graph\'s 66 m at the rulebook\'s pace', async () => {
    const win = mission.windows![0]!, plan: Plan = JSON.parse(await text('contracts/world/harbour-district.plan.json')), civ = civic(plan.civic, JSON.parse(await text(plan.civic.block)));
    expect(win).toMatchObject({ rule: 'summon', walk: { from: 'Office entrance', to: 'Café door' } });
    expect(Object.keys(win)).not.toContain('stayS'); // no number the rules hold is restated
    const sr = summonRules();
    expect([sr.stayS, sr.walkMaxS, sr.paceCmS]).toEqual([60, 90, 140]); // rulebook summon: stay 60 s, at most 90 s away; cell.speed 1.4 m/s
    const walkS = engineWalkS(win.walk.from, win.walk.to); // the engine's grid walk: the one number (Keel, Aster 2026-10-15)
    if (Bun.which('dotnet')) { expect(walkS).toBeGreaterThan(40); expect(walkS!).toBeLessThanOrEqual(sr.walkMaxS!); } // about 46 s: within the summons' reach
    const gdd = readdirSync(resolve(root, 'GDD')).find(f => /^the-suborned-agent-gdd-v.*\.md$/.test(f))!;
    const routeA = (await text(`GDD/${gdd}`)).split('\n').find(l => l.startsWith('route A, front door'))!;
    expect(routeA).toContain(win.gdd); // the GDD's figure, verbatim, beside the restated one
    // the entry window: the earliest call (prep A: Watch and Listen by the rulebook, then Help's timeS) to the chime, when the outgoing guard's shift ends
    const guard = mission.roles.find(r => r.id === win.role)!, rules = JSON.parse(await text('contracts/people/people.rules.json')), book = JSON.parse(await text('contracts/rules/rulebook.json'));
    const prepA = mission.prep!.find(p => p.route === 'A')!;
    expect(offsetS(prepA.at)).toBe(prepA.from.reduce((a, k) => a + book.values[k].v, 0)); // Watch 20 + Listen 42 = 1:02
    expect(routeA).toContain(prepA.gdd); // §07's figure, verbatim, beside the restated one (text owed)
    expect(offsetS(guard.shift.from)).toBe(offsetS(prepA.at) + rules.approaches.help.timeS);
    expect(guard.shift.to).toBe(mission.timeline.find(e => e.what === 'handover chime')!.at);
  });

  test('the overlay still passes on the world (people that meet the roles), and every event names a plan area', async () => {
    const plan: Plan = JSON.parse(await text('contracts/world/harbour-district.plan.json')), block = JSON.parse(await text(plan.civic.block));
    const R = check(plan, derive(plan), civic(plan.civic, block), false, overlay(plan, block, play(await people()))), by = (id: string) => R.checks.find(c => c.id === id)!;
    // V-1 (Vantage, 2026-10-15) fails until Sky picks an option for the public door (briefs/011/vantage-visibility.md); then this expects []
    expect(R.checks.filter(c => !c.ok).map(c => c.id)).toEqual(['V-1']);
    expect(by('C-3').value).toBe(`${mission.destinations.length}/${mission.destinations.length}`);
    expect(by('W-4').value).toBe('2/2'); // the overlay's walk origins (standards.walking.from): Café door, Office entrance
    expect(by('M-1').ok).toBe(true);
    expect(by('M-2').value).toBe(mission.roles.map(r => `${r.id} ${STANDIN_PINS[r.id]}`).join(', '));
    expect(R.routes.map(r => r.id)).toEqual(mission.routes.map(r => r.id));
    expect(R.routes.every(r => r.length < Infinity)).toBe(true);
    expect(mission.events.filter(e => !plan.areas.some(a => a.id === e.area)).map(e => e.id)).toEqual([]);
  }, 120000); // the whole plan check (walks included): 14-24 s on a busy machine
});

// Brief 010 D: the mission names roles, never people; the overlay casts each role from the generated people (here the stand-in
// cast, expanded in process, so the test does not read out/) and pins one person by id (rule M-2).
/** The people the stand-in cast expands to, as `bun run people` writes them. */
const people = async (): Promise<PeopleIn> => { const w = world(await readTexts('contracts/world/harbour-district.plan.json'), 'civic-block'); return expand(w.ctx(standInCast(w.scope, w.rules))).people; };
/** Who M-2 casts from the playable stand-in (`play` below): the first who fits by id. The stand-in is the tests' own fixture, so
 *  these stay while the live cast moves. */
const STANDIN_PINS: Record<string, string> = { waitress: 'p-cafe-11', driver: 'p-records-office-01', guard: 'p-records-office-15', councillor: 'p-records-office-61' };
/** Who M-2 casts from the promoted cast (people run r016, promoted by Sky 2026-10-16; contracts/people/harbour-district.cast.json),
 *  as the engine plays them (sim/data/handover.site.json cast, Keel). A new promotion moves these (re-rolls over hand-tuning: promote,
 *  then re-check); the roles stay. Nobody is pinned in the mission until a cast passes M-2. */
const PINS: Record<string, string> = { waitress: 'p-cafe-14', driver: 'p-records-office-31', guard: 'p-records-office-01', councillor: 'p-records-office-07' };
type Who = PeopleIn['persons'][number];
/** A copy of the people with `edit` applied to the named persons (default: none). */
const met = (p: PeopleIn, edit: Record<string, (x: Who) => void> = {}): PeopleIn => {
  const c: PeopleIn = structuredClone(p);
  for (const [id, f] of Object.entries(edit)) f(c.persons.find(x => x.id === id)!);
  return c;
};
/** The stand-in's six guards share the door, none is a regular of a 12:00 server, no councillor goes to the bay and no driver is
 *  tied to one: it fails M-2's playability (the next test). `play` makes it playable, the least edit per condition, as Pulse's
 *  routines must: one guard on the door (the others posted off it), his regular tie to Inez, Vale to the bay at 17:31, and his
 *  driver tied to him and waiting at the bay to 17:33. */
const GUARDS = ['p-records-office-15', 'p-records-office-29', 'p-records-office-30', 'p-records-office-36', 'p-records-office-57', 'p-records-office-67'];
const offDoor = (x: Who) => { for (const s of x.routine.stops) if (s.place === 'office-entrance') s.place = 'beyond'; };
const toBay = (from: number) => (x: Who) => { x.routine.stops = [...x.routine.stops.filter(s => s.from < 1050 || s.from >= from + 2), { place: 'office-bay', doing: 'commute', from, to: from + 2 }].sort((a, b) => a.from - b.from); };
const waitTo = (to: number) => (x: Who) => { x.routine.stops.find(s => s.place === 'office-bay' && s.to === 1050)!.to = to; };
const tie = (kind: string, to: string) => (x: Who) => { x.ties = [...(x.ties ?? []), { kind, with: to }]; };
const both = (...fs: ((x: Who) => void)[]) => (x: Who) => { for (const f of fs) f(x); };
const play = (p: PeopleIn) => met(p, {
  ...Object.fromEntries(GUARDS.slice(1).map(id => [id, offDoor])),
  'p-records-office-15': both(tie('regular', 'p-cafe-11'), tie('lover', 'p-cafe-11'), x => { x.secret = 'affair'; }),
  'p-cafe-11': both(tie('lover', 'p-records-office-15'), x => { x.secret = 'affair'; }), // Sky: Inez and the door guard are lovers
  'p-records-office-61': both(toBay(1051), tie('workmate', 'p-records-office-01')),
  'p-records-office-01': both(waitTo(1053), tie('workmate', 'p-records-office-61')),
});

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
    // there when the mission needs them (Sky 2026-10-15): Inez all mission, the guard in route A's entry window, the driver and Vale to the chime
    expect(mission.roles.map(r => `${r.id} ${r.shift.from}-${r.shift.to}`)).toEqual(['waitress 0:00-8:30', 'driver 0:00-5:30', 'guard 1:52-5:30', 'councillor 0:00-5:30']);
    // the static playability conditions M-2 reads (Suture 2026-10-15 §3)
    expect(mission.roles.map(r => [r.id, r.tiedTo ?? null, r.leavesTo ?? null, r.waitsFor ?? null, r.alone ?? null])).toEqual([
      ['waitress', [{ kind: 'lover', role: 'guard', cast: true }], null, null, null], // Sky: Inez and the door guard are lovers; her call summons him
      ['driver', [{ kind: 'workmate', role: 'councillor', cast: true }], null, 'councillor', null],
      ['guard', [{ kind: 'regular', role: 'waitress' }], null, null, true],
      ['councillor', null, { at: 'Meet driver', by: '6:10' }, null, null],
    ]);
    expect(mission.roles.find(r => r.id === 'waitress')!.secret).toBe('affair'); // the affair is her secret; her need stays debt, Help the reference
    expect(mission.roles.every(r => !r.pin)).toBe(true); // nothing pinned until a cast passes M-2
    expect(raw).not.toMatch(/"p-[a-z]/); // no person id written in the mission
    expect(broken(m => { m.roles[0].role = 'guard'; })).toBe(false); // both position and role
    expect(broken(m => { delete m.roles[0].position; })).toBe(false); // neither
    expect(broken(m => { m.roles[0].at = 'Nowhere'; })).toBe(false); // a point the mission does not name
    expect(broken(m => { m.roles[1].id = 'waitress'; })).toBe(false); // duplicate role id
    expect(broken(m => { m.roles[0].pin = 'p-cafe-11'; m.roles[3].pin = 'p-cafe-11'; })).toBe(false); // one person, two roles
    expect(broken(m => { m.roles[0].pin = 'Inez'; })).toBe(false); // a pin is a person id
  });

  test('the stand-in cast fails M-2\'s playability, naming each role and condition (Pulse fixes the routines; the live run must pass)', async () => {
    const s = await setup(), c = m2(s), lines = c.detail!.split('; ');
    expect(c).toMatchObject({ ok: false, value: 'waitress -, driver -, guard -, councillor -' });
    expect(lines.map(l => l.replace(/:.*?(has|·)/, ' $1').replace(/: p-.*?( ·|$)/g, '$1'))).toEqual([ // every role fails: the stand-in has no lovers either
      'waitress has secret affair',
      'driver has a workmate tie to a councillor of records-office who can be cast as councillor',
      'guard has a regular tie to a server of cafe in place for waitress in this window · and at most one guard of records-office at Office entrance in 1:52-5:30: 6 at 17:26:22 (p-records-office-15, p-records-office-29, p-records-office-30, p-records-office-36, p-records-office-57, p-records-office-67)',
      'councillor has a routine that reaches Meet driver by 6:10 (17:30:40)',
    ]);
    expect(lines[3]).toContain('p-records-office-61 t1-records@17:34'); // Vale takes the tram home, never the official car
  });

  test('every role is cast, pinned by id, deterministically; the pinned names land on the pinned people', async () => {
    const s = { ...(await setup()) }; s.p = play(s.p);
    const c = m2(s);
    expect(c.ok).toBe(true);
    const a = castRoles(s.p.persons, mission, { toPlan: (q) => q, placeAt: () => undefined, polyOf: () => undefined }); // no place resolves: only the councillor (no point) casts
    expect(a.map(x => x.ok)).toEqual([false, false, false, true]);
    const shuffled = { ...s.p, persons: [...s.p.persons].reverse() }; // the tie-break is the id, not the file's order
    expect(m2(s, mission, shuffled).value).toBe(c.value);
    expect(c.value).toBe(mission.roles.map(r => `${r.id} ${STANDIN_PINS[r.id]}`).join(', '));
    const name = (id: string) => s.p.persons.find(p => p.id === id)!.name;
    expect(name(STANDIN_PINS.waitress!)).toStartWith('Inez '); // people.rules.json names.pinned: Inez is the waitress the mission casts
    expect(name(STANDIN_PINS.councillor!)).toEndWith(' Vale');
  });

  test('the promoted cast (r016) passes M-2 and pins the people the engine plays', async () => {
    const s = await setup(); // no people given: the overlay expands the promoted cast (tools/people/current.ts), as the plan checker does
    const c = overlay(s.plan, s.block, undefined, mission).civic({ plan: s.plan, d: s.d, civ: s.civ } as never).find(x => x.id === 'M-2')!;
    expect(c.ok).toBe(true);
    expect(c.value).toBe(mission.roles.map(r => `${r.id} ${PINS[r.id]}`).join(', '));
    const site = JSON.parse(await text('sim/data/handover.site.json')).cast; // Keel's engine plays the same people (sim:site, M-2)
    expect(Object.fromEntries(mission.roles.map(r => [r.id, site[r.id].id]))).toEqual(PINS);
    expect(site.waitress.name).toStartWith('Inez '); // people.rules.json names.pinned
    expect(site.councillor.name).toEndWith(' Vale');
    expect([site.waitress.temperament, site.waitress.lover, site.guard.lover]).toEqual(['fearful', 'guard', 'waitress']); // r016: a Fearful Inez takes Help ×2 (route A)
  });

  test('failing example: a role nobody can fill fails M-2, naming the role', async () => {
    const s = { ...(await setup()) }; s.p = play(s.p); const m = JSON.parse(raw);
    m.roles.push({ id: 'official-door-guard', what: 'a guard at the official door', employer: 'records-office', role: 'guard', at: 'Official entrance', near: 2, shift: { from: '0:00', to: '8:00' }, src: 'test' });
    expect(Mission.safeParse(m).success).toBe(true); // a valid mission: the role is well formed, nobody stands there
    const c = m2(s, m);
    expect(c.ok).toBe(false);
    expect(c.detail).toBe('official-door-guard: nobody is a guard of records-office on shift 0:00-8:00 at Official entrance');
  });

  test('failing examples for each role condition: need, a refused approach, a regular of the café, an unsound role', async () => {
    const s = await setup(), p = play(s.p);
    const one = (m: any, q: PeopleIn = p) => m2(s, m, q).detail!.split('; ').filter(x => !/ of \d+ on shift/.test(x)); // the failing roles only
    let m = JSON.parse(raw); m.roles[1].need = 'advancement'; // the drivers at the bay need cash, cash, advancement (p-records-office-46), papers
    delete m.roles[1].tiedTo; delete m.roles[1].waitsFor; // the need alone: p-records-office-46 is not Vale's driver
    expect(m2(s, m, p)).toMatchObject({ ok: true, value: expect.stringContaining('driver p-records-office-46') });
    m.roles[1].need = 'housing';
    expect(one(m)).toEqual(['driver: none of the 4 who fit (official-driver of records-office on shift 0:00-5:30 at Meet driver) has need housing: p-records-office-01 cash, p-records-office-39 cash, p-records-office-46 advancement, p-records-office-64 papers']);
    // Q4: the wrong temperament still works (a Mercenary helped), a refusal fails: an Idealist refuses Pay
    m = JSON.parse(raw); m.roles[0].approach = 'pay';
    const idealists = met(p, Object.fromEntries(['p-cafe-11', 'p-cafe-12', 'p-cafe-14', 'p-cafe-15', 'p-cafe-16'].map(id => [id, (x: any) => { x.need = 'debt'; x.temperament = 'idealist'; }])));
    expect(one(m, idealists)).toEqual(['waitress: none of the 5 who fit (server of cafe on shift 0:00-8:30 at Café door) has a temperament that does not refuse pay: p-cafe-11 idealist, p-cafe-12 idealist, p-cafe-14 idealist, p-cafe-15 idealist, p-cafe-16 idealist']);
    expect(m2(s, JSON.parse(raw), idealists).ok).toBe(true); // Help is refused by no temperament
    expect(one(m, met(idealists, { 'p-cafe-11': x => { x.temperament = 'shy'; } }))[0]).toContain('p-cafe-11 shy (unknown)'); // unknown = reject
    // §05, §08: the guard is a regular of the café only by a regular tie to its staff (one definition with P-6 and the review): a pre-shift stop alone is not enough; a tie restores one
    const noStop = met(p, Object.fromEntries(GUARDS.map(id => [id, (x: any) => { x.routine.stops = x.routine.stops.filter((t: any) => t.doing !== 'pre-shift'); x.ties = (x.ties ?? []).filter((t: any) => !t.with.startsWith('p-cafe')); }])));
    expect(one(JSON.parse(raw), noStop)).toEqual(['waitress: none of the 1 who fit (server of cafe on shift 0:00-8:30 at Café door) has a lover tie to a guard of records-office who can be cast as guard: p-cafe-11 p-records-office-15', 'guard: none of the 1 who fit (guard of records-office on shift 1:52-5:30 at Office entrance) has a regular tie to the staff of cafe: p-records-office-15 none']); // the one guard on the door
    const tied = met(noStop, { 'p-records-office-15': tie('regular', 'p-cafe-11') });
    expect(m2(s, JSON.parse(raw), tied)).toMatchObject({ ok: true, value: expect.stringContaining('guard p-records-office-15') });
    const workmate = met(noStop, { 'p-records-office-15': x => { x.ties = [{ kind: 'workmate', with: 'p-cafe-11' }]; } }); // only a regular tie counts
    expect(m2(s, JSON.parse(raw), workmate).ok).toBe(false);
    // a pin that lacks a condition names it; a role the rules cannot hold fails before anyone is cast
    m = JSON.parse(raw); m.roles[0].pin = 'p-cafe-12';
    expect(one(m)).toEqual(['waitress: pinned p-cafe-12 lacks secret affair: p-cafe-12 no secret']); // she isn't the guard's lover
    m = JSON.parse(raw); m.roles[0].need = 'fame';
    expect(one(m)).toEqual(['waitress: need fame is not in the rules\' needs']);
    m = JSON.parse(raw); m.roles[1].approach = 'pressure'; // Pressure leans on debt, never on cash
    expect(one(m)).toEqual(['driver: pressure does not act on need cash (metBy help, pay)']);
  });

  test('failing examples for each playability condition: alone, the guard\'s tie, Vale to the bay, the driver\'s tie and wait', async () => {
    const s = await setup(), p = play(s.p), m = JSON.parse(raw);
    const why = (q: PeopleIn, role: string) => { const c = m2(s, m, q); return c.detail!.split('; ').find(x => x.startsWith(`${role}: `))!; };
    expect(m2(s, m, p).ok).toBe(true);
    // alone: a second guard back on the door in the entry window
    const two = met(p, { 'p-records-office-29': x => { x.routine.stops.find(t => t.place === 'beyond' && t.to === 1050)!.place = 'office-entrance'; } });
    expect(why(two, 'guard')).toBe('guard: p-records-office-15 cast, but at most one guard of records-office at Office entrance in 1:52-5:30: 2 at 17:26:22 (p-records-office-15, p-records-office-29)');
    // the guard's regular tie: to a server off shift then fails; to another server of Inez's shift passes (Inez or her shift)
    const morning = met(p, { 'p-records-office-15': x => { x.ties = x.ties!.filter(t => t.with !== 'p-cafe-11'); x.ties.push({ kind: 'regular', with: 'p-cafe-02' }); } });
    expect(why(morning, 'guard')).toStartWith('guard: none of the 1 who fit (guard of records-office on shift 1:52-5:30 at Office entrance) has a regular tie to a server of cafe in place for waitress in this window: p-records-office-15 p-cafe-04+p-cafe-02');
    expect(m2(s, m, met(morning, { 'p-records-office-15': tie('regular', 'p-cafe-12') })).ok).toBe(true);
    // in place throughout: Inez away from the door for a minute in the mission falls to the next server by id
    const away = met(p, { 'p-cafe-11': x => { const w = x.routine.stops.find(t => t.place === 'cafe-entrance' && t.from <= 1045 && 1045 < t.to)!; x.routine.stops.push({ ...w, from: 1047, to: w.to }, { place: 'cafe-table-0', doing: 'meal', from: 1046, to: 1047 }); w.to = 1046; x.routine.stops.sort((a, b) => a.from - b.from); } });
    expect(m2(s, m, away).value).toStartWith('waitress -,'); // no other server of her shift is the guard's lover, so nobody can call him off the door
    // leavesTo: Vale at the bay a minute late (17:32 > 17:30:40, rounded up to 17:31)
    const late = met(p, { 'p-records-office-61': x => { x.routine.stops.find(t => t.place === 'office-bay')!.from = 1052; } });
    expect(why(late, 'councillor')).toContain('has a routine that reaches Meet driver by 6:10 (17:30:40): p-records-office-61 office-bay@17:32');
    // waitsFor: the driver leaves the bay as Vale arrives
    const gone = met(p, { 'p-records-office-01': x => { x.routine.stops.find(t => t.place === 'office-bay' && t.to === 1053)!.to = 1051; } });
    expect(why(gone, 'driver')).toBe('driver: p-records-office-01 cast, but waits at Meet driver for councillor p-records-office-61, who arrives 17:31: p-records-office-01 leaves 17:31');
    // tiedTo cast: the councillor cast follows the driver's tie (p-records-office-65 too goes to the bay; the driver is his)
    const other = met(p, {
      'p-records-office-65': both(toBay(1051), tie('workmate', 'p-records-office-01')),
      'p-records-office-01': x => { x.ties = x.ties!.filter(t => t.with !== 'p-records-office-61'); x.ties.push({ kind: 'workmate', with: 'p-records-office-65' }); },
    });
    expect(m2(s, m, other)).toMatchObject({ ok: true, value: 'waitress p-cafe-11, driver p-records-office-01, guard p-records-office-15, councillor p-records-office-65' });
    // and with no councillor tied to a driver who can be cast, the driver names it
    const untied = met(p, { 'p-records-office-01': x => { x.ties = x.ties!.filter(t => t.kind !== 'workmate'); } });
    expect(why(untied, 'driver')).toBe('driver: none of the 2 who fit (official-driver of records-office on shift 0:00-5:30 at Meet driver) has a workmate tie to a councillor of records-office who can be cast as councillor: p-records-office-01 none, p-records-office-39 p-records-office-23+p-records-office-30');
  });

  test('a pin that no longer fits fails, and so does a missing people file', async () => {
    const debt = (x: PeopleIn['persons'][number]) => { x.need = 'debt'; };
    const s = { ...(await setup()) }; s.p = met(play(s.p), { 'p-cafe-11': debt, 'p-cafe-14': both(debt, tie('lover', 'p-records-office-15'), x => { x.secret = 'affair'; }), 'p-records-office-15': tie('lover', 'p-cafe-14') }); const m = JSON.parse(raw);
    m.roles[0].pin = 'p-cafe-02'; // a server on the morning shift, gone by 17:00
    expect(m2(s, m).detail).toContain('waitress: pinned p-cafe-02 is no longer a server of cafe on shift 0:00-8:30 at Café door');
    m.roles[0].pin = 'p-cafe-14';
    expect(m2(s, m)).toMatchObject({ ok: true, value: expect.stringContaining('waitress p-cafe-14') }); // a pin that fits wins over the tie-break
    const other = { ...s.plan, version: '0.0.0-test' };
    expect(overlay(other, s.block, s.p).civic({ plan: other, d: s.d, civ: s.civ } as never).find(c => c.id === 'M-2')!.ok).toBe(false); // the people are of another plan version
  });

  test('Inez and the guard are lovers (Sky; Motive briefs/011/motive-lovers.md): the waitress\'s lover tie and secret, and the summons, each with a failing example', async () => {
    const s = await setup(), rules = { ...castRulesFile(), secrets: { affair: {}, record: {} } };
    const lovers = JSON.parse(raw); lovers.roles[0].tiedTo = [{ kind: 'lover', role: 'guard', cast: true }]; lovers.roles[0].secret = 'affair';
    expect(Mission.safeParse(lovers).success).toBe(true);
    // no lover pair: M-2 fails loudly, naming the waitress
    const c = m2(s, lovers, met(play(s.p), { 'p-cafe-11': x => { x.ties = x.ties!.filter(t => t.kind !== 'lover'); } }));
    expect(c.ok).toBe(false); expect(c.detail).toMatch(/^waitress: none of the 1 who fit .* has a lover tie to a guard of records-office/);
    const place = new Map(s.p.places.map(q => [q.id, q])), where = {
      toPlan: toPlanFn(s.plan.civic), placeAt: (id: string) => place.get(id)?.at as V | undefined,
      polyOf: (id: string) => { const q = place.get(id); return q?.kind === 'bay' ? s.civ.bays.find(b => b.id === q.name)?.poly : undefined; },
    };
    const summon = (maxWalkS?: number) => ({ play: true, summon: { ...(maxWalkS === undefined ? {} : { maxWalkS }), walkS: (a: string, b: string) => (new Set([a, b]).has('Office entrance') && new Set([a, b]).has('Café door') ? 47 : undefined) } });
    const pair = met(play(s.p), { 'p-cafe-11': both(tie('lover', 'p-records-office-15'), x => { x.secret = 'affair'; }), 'p-records-office-15': both(tie('lover', 'p-cafe-11'), x => { x.secret = 'affair'; }) });
    const cast = (q: PeopleIn, o: object) => castRoles(q.persons, lovers, where, rules, o).filter(c => !c.ok).map(c => `${c.role}: ${c.why}`);
    expect(cast(pair, summon(90))).toEqual([]); // the pair: 47 s apart, the summons reaches 90 s
    expect(cast(pair, summon(40))).toEqual(['waitress: p-cafe-11 cast, but a lover summons to guard p-records-office-15: 47 s from Office entrance to Café door, over the summons\' 40 s']);
    expect(cast(pair, summon())).toEqual(['waitress: p-cafe-11 cast, but a lover summons to guard: the rulebook has no summon-walk-max-s']);
    expect(cast(pair, { play: true })[0]).toContain('no summons rule given');
    // no pair: M-2 fails loudly, naming the role and the tie
    expect(cast(met(play(s.p), { 'p-cafe-11': x => { x.ties = x.ties!.filter(t => t.kind !== 'lover'); } }), summon(90))[0]).toStartWith('waitress: none of the 1 who fit (server of cafe on shift 0:00-8:30 at Café door) has a lover tie to a guard of records-office who can be cast as guard');
  });
});

