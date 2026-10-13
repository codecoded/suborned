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
import { overlay } from './overlay';

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
    expect(mission.schema).toBe('suborned.mission/2');
    expect(broken(() => {})).toBe(true);
    expect(broken(m => { m.schema = 'suborned.mission/1'; })).toBe(false);
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

  test('the overlay still passes on the world, and every event names a plan area', async () => {
    const plan: Plan = JSON.parse(await text('contracts/world/harbour-district.plan.json')), block = JSON.parse(await text(plan.civic.block));
    const R = check(plan, derive(plan), civic(plan.civic, block), false, overlay(plan, block)), by = (id: string) => R.checks.find(c => c.id === id)!;
    expect(R.checks.filter(c => !c.ok).map(c => c.id)).toEqual([]);
    expect(by('C-3').value).toBe(`${mission.destinations.length}/${mission.destinations.length}`);
    expect(by('W-4').value).toBe('2/2'); // the overlay's walk origins (standards.walking.from): Café door, Office entrance
    expect(by('M-1').ok).toBe(true);
    expect(R.routes.map(r => r.id)).toEqual(mission.routes.map(r => r.id));
    expect(R.routes.every(r => r.length < Infinity)).toBe(true);
    expect(mission.events.filter(e => !plan.areas.some(a => a.id === e.area)).map(e => e.id)).toEqual([]);
  }, 120000); // the whole plan check (walks included): 14-24 s on a busy machine
});
