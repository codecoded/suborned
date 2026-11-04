// Done-check for the briefing's mission data (Waymark, 2026-10-26; Glyph N1 on 0.2.122): brief.json's ids all name something
// in mission.json, target.at is Vale's official entrance, and an unknown place, role or shape is rejected (failing examples).
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { type Brief, type Mission, checkBrief } from './brief.ts';

const dir = join(import.meta.dir, '..', '..', '..', 'contracts', 'missions', 'handover');
const brief = JSON.parse(readFileSync(join(dir, 'brief.json'), 'utf8')) as Brief & { target: { role: string; at: string } };
const mission = JSON.parse(readFileSync(join(dir, 'mission.json'), 'utf8')) as Mission;
const withTarget = (target: unknown): Brief => ({ ...brief, target });

describe('brief.json against mission.json', () => {
  test('the shipped brief passes', () => {
    expect(checkBrief(brief, mission)).toEqual([]);
  });
  test('target.at is the official entrance Vale leaves by (timeline 5:30), not the guard\'s public door', () => {
    expect(brief.target).toEqual({ role: 'councillor', at: 'd:Official entrance' });
    expect(mission.destinations!.some((d) => d.name === 'Official entrance')).toBe(true);
    expect((mission.timeline as { at: string; what: string }[]).find((t) => t.at === '5:30')?.what).toMatch(/official door/);
  });
});

describe('unknown = reject (failing examples)', () => {
  test('a target.at naming no destination', () => {
    expect(checkBrief(withTarget({ role: 'councillor', at: 'd:Back door' }), mission)).toEqual(['target.at: "d:Back door" names no mission.json destination']);
  });
  test('a bare destination name, "door" or a missing at is not a target place id', () => {
    expect(checkBrief(withTarget({ role: 'councillor', at: 'Official entrance' }), mission)[0]).toMatch(/^target\.at: "Official entrance" is not a place id/);
    expect(checkBrief(withTarget({ role: 'councillor', at: 'door' }), mission)[0]).toMatch(/^target\.at: "door" is not a place id/);
    expect(checkBrief(withTarget({ role: 'councillor' }), mission)).toEqual(['target.at: not a place id']);
  });
  test('the old string target, an unknown role and an unknown key', () => {
    expect(checkBrief(withTarget('councillor'), mission)).toEqual(['target: must be { role, at }']);
    expect(checkBrief(withTarget({ role: 'mayor', at: 'd:Official entrance' }), mission)).toEqual(['target.role: "mayor" names no mission.json role']);
    expect(checkBrief(withTarget({ role: 'councillor', at: 'd:Official entrance', via: 'x' }), mission)).toEqual(['target.via: unknown key']);
  });
  test('a way\'s contact or at, the camera place and a clock mark must be real', () => {
    const ways = [{ id: 'pickup', contact: 'chauffeur', steps: ['car'], at: 'd:Nowhere' }];
    expect(checkBrief({ ...brief, ways }, mission)).toEqual(['ways[0].contact: "chauffeur" names no mission.json role', 'ways[0].at: "d:Nowhere" names no mission.json destination']);
    expect(checkBrief({ ...brief, camera: { place: 'd:Pier' } }, mission)).toEqual(['camera.place: "d:Pier" names no mission.json destination']);
    expect(checkBrief({ ...brief, clock: [{ id: 'chime', at: '5:31' }] }, mission)).toEqual(['clock[0].at: "5:31" is not a mission.json timeline mark']);
  });
});
