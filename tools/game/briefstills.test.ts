// Brief 070: the briefing page's stills are derived from the mission's data (tools/game/briefstills.ts), one per pinned place, never by hand.
import { describe, expect, test } from 'bun:test';
import { derive, faceRuns, nearestFace, PLAN_MIN_M, PLAN_MIN_M2, polyArea, POSTED_KEY, POSTED_TURN, shortSide, shotsFile, WAIT_SHIFT, WAIT_ZOOM } from './briefstills';

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
    expect(s.pins.slice(2, 4).map(p => p.camera.kind === 'play' ? [p.camera.yaw, p.camera.zoom, p.camera.focus ?? '', p.camera.focus2 ?? ''] : null)).toEqual([[-1, WAIT_ZOOM, 'driver', 'car/official'], [-1, 9, '', '']]);
    // Glyph N1: the guard 40° off his door toward its shorter run (the Office's west wing runs 6.8 m to the south of the door, 5.1 m to the north), lit as Inez is
    const g = s.pins[1]!.camera; expect(g.kind === 'person' ? [g.turn, g.side, g.key] : null).toEqual([POSTED_TURN, [-1, 0], POSTED_KEY]);
    expect(s.targetDoor).toEqual({ place: 'd:Official entrance', at: [76.25, -30], normal: [1, 0] });
  });
  test('the plan holds every pin and both buildings', () => {
    const [x0, y0, x1, y1] = s.plan.bounds;
    for (const p of s.pins) { expect(p.at[0]).toBeGreaterThan(x0); expect(p.at[0]).toBeLessThan(x1); expect(p.at[1]).toBeGreaterThan(y0); expect(p.at[1]).toBeLessThan(y1); }
    expect(s.plan.buildings.map(b => b.name).sort()).toEqual(['Corner café', 'Records Office']);
    for (const g of s.plan.green) expect(shortSide(g)).toBeGreaterThanOrEqual(PLAN_MIN_M); // Glyph G8: no stray ticks
    for (const g of s.plan.green) expect(polyArea(g)).toBeGreaterThanOrEqual(PLAN_MIN_M2); // Glyph N7: no shape under 2 m²
    expect(polyArea([[0, 0], [2, 0], [2, 1], [0, 1]])).toBe(2);
  });
  test('nearestFace: the outward normal of the closest edge', () => {
    const sq: [number, number][] = [[0, 0], [10, 0], [10, 10], [0, 10]];
    expect(nearestFace([5, -1], [{ polygon: sq }])!.normal).toEqual([0, -1]);
    expect(nearestFace([11, 5], [{ polygon: sq }])!.normal).toEqual([1, 0]);
  });
  test('faceRuns: how far a face runs each way from a door', () => {
    const sq: [number, number][] = [[0, 0], [10, 0], [10, 10], [0, 10]];
    const r = faceRuns([3, -1], [{ polygon: sq }])!; expect(r.t).toEqual([1, 0]); expect(Math.abs(r.runs[0] - 7)).toBeLessThanOrEqual(0.25); expect(Math.abs(r.runs[1] - 3)).toBeLessThanOrEqual(0.25);
  });
  test('the shot runner file: world only, JPEG, one run per route with rising ticks, the cut off where the yaw faces a door', () => {
    const f = shotsFile(s, '/tmp/x');
    expect(f.shots.every((x: any) => x.bare)).toBe(true); // Glyph N4: no world marks
    const t = POSTED_TURN * Math.PI / 180;
    expect(f.shots.find((x: any) => x.name === 'brief-guard')).toMatchObject({ doorS: [-1, 0], keyLight: POSTED_KEY, pose: { offset: [+(3.6 * Math.sin(t)).toFixed(3), 1.9, +(3.6 * Math.cos(t)).toFixed(3)] } });
    expect(f.shots.find((x: any) => x.name === 'brief-driver')).toMatchObject({ focus: 'driver', focus2: 'car/official', shift: WAIT_SHIFT, zoom: WAIT_ZOOM });
    expect(f.shots.find((x: any) => x.name === 'brief-getaway').pose.offset).toEqual([1.8, 5.8, 11.5]);
    expect(f.shots.map((x: any) => [x.name, x.tick, x.hud, x.jpg, x.pose.on, x.nocut])).toEqual([
      ['brief-waitress', 30, false, true, true, false], ['brief-guard', 600, false, true, true, false],
      ['brief-getaway', 0, false, true, true, false], ['brief-driver', 3700, false, true, false, true], ['brief-service-bay', 3860, false, true, false, true],
    ]);
  });
});
