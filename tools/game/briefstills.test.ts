// Brief 070: the briefing page's stills are derived from the mission's data (tools/game/briefstills.ts), one per pinned place, never by hand.
import { describe, expect, test } from 'bun:test';
import { derive, nearestFace, shotsFile } from './briefstills';

describe('the briefing stills (brief 070)', () => {
  const s = derive('handover');
  test('the Handover: two ways of two pins each, numbered 1 to 4 in way order, then the getaway car', () => {
    expect(s.pins.map(p => [p.n, p.way, p.place])).toEqual([
      ['1', 'front-door', 'd:Café door'], ['2', 'front-door', 'd:Office entrance'],
      ['3', 'pickup', 'd:Meet driver'], ['4', 'pickup', 'service-bay'], ['', '', 'd:Getaway bay'],
    ]);
    expect(s.pins.map(p => p.text)).toEqual(['waitress', 'guard', 'driver', 'car-official', 'car-getaway']);
  });
  test('ticks and routes from the data: Inez at 0:03, the guard at 1:00 (route A); the driver at Vale at his car 6:10, the bay at 6:25 + 1 s (route B)', () => {
    expect(s.pins.map(p => p.tick)).toEqual([30, 600, 3700, 3860, 0]);
    expect(s.pins.map(p => p.route.split('/').pop())).toEqual(['route-a-board.json', 'route-a-board.json', 'route-b-board.json', 'route-b-board.json', 'route-b-board.json']);
  });
  test('cameras: Inez and the guard in the conversation profile off their door; the car places on the play camera facing the face; the getaway car off its end', () => {
    expect(s.pins.map(p => [p.camera.kind, p.camera.normal])).toEqual([['person', [1, 0]], ['person', [0, -1]], ['play', [1, 0]], ['play', [0, 1]], ['car', [1, 0]]]);
    expect(s.pins.slice(2, 4).map(p => p.camera.kind === 'play' ? [p.camera.yaw, p.camera.zoom] : null)).toEqual([[-1, 9], [-1, 9]]);
    expect(s.targetDoor).toEqual({ place: 'd:Official entrance', at: [76.25, -30], normal: [1, 0] });
  });
  test('the plan holds every pin and both buildings', () => {
    const [x0, y0, x1, y1] = s.plan.bounds;
    for (const p of s.pins) { expect(p.at[0]).toBeGreaterThan(x0); expect(p.at[0]).toBeLessThan(x1); expect(p.at[1]).toBeGreaterThan(y0); expect(p.at[1]).toBeLessThan(y1); }
    expect(s.plan.buildings.map(b => b.name).sort()).toEqual(['Corner café', 'Records Office']);
  });
  test('nearestFace: the outward normal of the closest edge', () => {
    const sq: [number, number][] = [[0, 0], [10, 0], [10, 10], [0, 10]];
    expect(nearestFace([5, -1], [{ polygon: sq }])!.normal).toEqual([0, -1]);
    expect(nearestFace([11, 5], [{ polygon: sq }])!.normal).toEqual([1, 0]);
  });
  test('the shot runner file: world only, JPEG, one run per route with rising ticks, the cut off where the yaw faces a door', () => {
    const f = shotsFile(s, '/tmp/x');
    expect(f.shots.map((x: any) => [x.name, x.tick, x.hud, x.jpg, x.pose.on, x.nocut])).toEqual([
      ['brief-waitress', 30, false, true, true, false], ['brief-guard', 600, false, true, true, false],
      ['brief-getaway', 0, false, true, true, false], ['brief-driver', 3700, false, true, false, true], ['brief-service-bay', 3860, false, true, false, true],
    ]);
  });
});
