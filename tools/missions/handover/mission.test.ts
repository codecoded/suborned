// The Handover mission file (contracts/missions/handover/mission.json): it parses as suborned.mission/2, its destinations,
// crew and routes are the ones moved out of the world, its timeline is GDD §07's schedule, and the overlay still passes.
// Preservation lock (Sky 2026-10-14): re-siting or re-timing the Handover (brief 003) changes PLACES and the HEAD test with it.
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
const PLACES = 'db183672fada0e2cebc2328a37ed7623af55374085ce68f25a2c8ba00fa31e9d'; // sha256 of places() at commit 15af5dd (suborned.mission/1)
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

  test('destinations, crew and routes are byte-for-byte the ones moved out of the world', () => {
    expect(createHash('sha256').update(places(raw)).digest('hex')).toBe(PLACES);
    const git = Bun.spawnSync(['git', 'show', `HEAD:${FILE}`], { cwd: root }), head = git.stdout.toString();
    expect(git.exitCode).toBe(0);
    expect(places(raw)).toBe(places(head));
    const was = JSON.parse(head);
    for (const k of ['destinations', 'crew', 'routes'] as const) expect(mission[k] as unknown).toEqual(was[k]);
  });

  test('the timeline is GDD §07\'s schedule, entry for entry', async () => {
    const gdd = readdirSync(resolve(root, 'GDD')).filter(f => /^the-suborned-agent-gdd-v.*\.md$/.test(f));
    expect(gdd).toHaveLength(1);
    const s07 = (await text(`GDD/${gdd[0]}`)).split(/^## /m).find(s => s.startsWith('§07 '))!;
    const schedule = s07.split('\n').find(l => l.startsWith('schedule: '))!.slice('schedule: '.length).replace(/\.$/, '').split(' · ');
    expect(mission.timeline.map(e => `${e.at} ${e.what}`)).toEqual(schedule);
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
    expect(by('M-1').ok).toBe(true);
    expect(R.routes.map(r => r.id)).toEqual(mission.routes.map(r => r.id));
    expect(R.routes.every(r => r.length < Infinity)).toBe(true);
    expect(mission.events.filter(e => !plan.areas.some(a => a.id === e.area)).map(e => e.id)).toEqual([]);
  }, 120000); // the whole plan check (walks included): 14-24 s on a busy machine
});
