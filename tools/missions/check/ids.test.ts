// Brief 078 B1: the Handover's words and points name only ids the site has (ids.ts). Fails on a remap rename such as brief 075's
// mast-camera-314 → mast-camera-z0, which once silently dropped the mast camera's name, hover text and order lines.
import { expect, test } from 'bun:test';
import { missionIdProblems, siteIds } from './ids';

test('the Handover names only ids its site has (cameras, stands, cars, points, destinations)', () => {
  expect(missionIdProblems('handover')).toEqual([]);
});

test('the site reads the mast camera as the sim does (Site.cs: the street id\'s last segment, "-camera" dropped)', () => {
  const s = siteIds('handover');
  expect([...s.cameras].some((c) => c.startsWith('mast-'))).toBe(true);
  for (const c of ['entrance', 'pickup']) expect(s.cameras.has(c)).toBe(true);
});
