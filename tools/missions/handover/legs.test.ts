// The Handover's fixed legs and mission camera poles, the data the geometry audit's leg rule reads (tools/missions/handover/legs.ts; Pulse, 2026-10-24).
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { cameraPoles, fixedLegs, poleCircle } from './legs';

const ROOT = resolve(import.meta.dir, '../../..');
const mission = JSON.parse(readFileSync(resolve(ROOT, 'contracts/missions/handover/mission.json'), 'utf8'));
const collide = JSON.parse(readFileSync(resolve(ROOT, 'contracts/world/kit.collide.json'), 'utf8'));

describe('fixed legs', () => {
  test('the patrol sweep is patrol-a to patrol-b and the reroute is a stand', () => {
    const [sweep, wait] = fixedLegs(mission);
    const at = (id: string) => mission.points.find((p: { id: string }) => p.id === id).at;
    expect(sweep!.a).toEqual(at('patrol-a'));
    expect(sweep!.b).toEqual(at('patrol-b'));
    expect(wait!.a).toEqual(at('patrol-reroute'));
    expect(wait!.b).toEqual(wait!.a);
  });
  test('the engine\'s stand moves an end that is off free ground', () => {
    const [sweep] = fixedLegs(mission, p => [p[0] + 1, p[1]]);
    expect(sweep!.a[0]).toBe(mission.points.find((p: { id: string }) => p.id === 'patrol-a').at[0] + 1);
  });
  test('a mission without the patrol points is rejected', () => {
    expect(() => fixedLegs({ points: [] })).toThrow(/no point patrol-a/);
  });
});

describe('mission camera poles', () => {
  test('CCTVPole at the host\'s scale holds in a circle about half a metre wide', () => {
    const c = poleCircle(collide.pieces.CCTVPole, [53, -44.5]);
    expect(c.c).toEqual([53, -44.5]);
    expect(c.r).toBeGreaterThan(0.4);
    expect(c.r).toBeLessThan(0.6);
  });
  test('every pole camera in the mission has one', () => {
    const poles = cameraPoles(mission, collide);
    expect(poles.map(p => p.id)).toEqual(mission.cameras.filter((c: { kind?: string }) => (c.kind ?? 'pole-camera') === 'pole-camera').map((c: { id: string }) => `camera pole ${c.id}`));
  });
});
