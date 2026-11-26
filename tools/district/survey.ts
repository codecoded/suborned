// Project the played OldTown's ground-storey outlines and paving onto the district's two-metre cells.
// After a town swap, run the Unity editor method Suborned.OldTown.Editor.Swap.DistrictLandmarkSurvey.Export,
// then run bun tools/district/survey.ts out/batch/swap/built.json.
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';

type Point = [number, number];
export type StreetSurvey = {
  planHash: string;
  origin: Point;
  cell: number;
  // For each northing, half-open eastward runs of surveyed stone (1) or solid architecture (2).
  rows: number[][];
};
const cell = 2;
function inside(ring: Point[], x: number, y: number) {
  let hit = false;
  for (let i = 0, j = ring.length - 1; i < ring.length; j = i++) {
    const [ax, ay] = ring[i]!,
      [bx, by] = ring[j]!;
    if (ay > y !== by > y && x < ax + ((bx - ax) * (y - ay)) / (by - ay)) hit = !hit;
  }
  return hit;
}
if (import.meta.main) {
  const builtPath = process.argv[2];
  if (!builtPath) throw Error('Pass the built.json from the OldTown swap');
  const built = JSON.parse(readFileSync(builtPath, 'utf8'));
  const text = readFileSync(built.plan.path, 'utf8');
  const planHash = createHash('sha256').update(text).digest('hex');
  if (built.plan.sha256 !== planHash) throw Error('Rebuild OldTown against the current district plan first');
  const { sheet } = JSON.parse(text);
  const [west, east] = sheet.x,
    [south, north] = sheet.y;
  const width = Math.ceil((east - west) / cell),
    height = Math.ceil((north - south) / cell);
  const bits = new Uint8Array(width * height);
  function stamp(ring: Point[], kind: number) {
    const xs = ring.map((p) => p[0]),
      ys = ring.map((p) => p[1]);
    const ix = (x: number) => Math.floor((x - west) / cell),
      iy = (y: number) => Math.floor((y - south) / cell);
    for (let y = Math.max(0, iy(Math.min(...ys))); y <= Math.min(height - 1, iy(Math.max(...ys))); y++)
      for (let x = Math.max(0, ix(Math.min(...xs))); x <= Math.min(width - 1, ix(Math.max(...xs))); x++)
        if (inside(ring, west + (x + 0.5) * cell, south + (y + 0.5) * cell)) bits[y * width + x] = kind;
  }
  for (const p of built.pavements) {
    const [x, y] = p.at,
      h = p.size / 2,
      a = (p.yaw * Math.PI) / 180;
    stamp(
      [
        [-h, -h],
        [h, -h],
        [h, h],
        [-h, h],
      ].map(([u, v]) => [x + u! * Math.cos(a) - v! * Math.sin(a), y + u! * Math.sin(a) + v! * Math.cos(a)]),
      1,
    );
  }
  const landmarks = JSON.parse(readFileSync(join(dirname(builtPath), 'landmark-ground.json'), 'utf8'));
  for (const b of built.buildings)
    if (b.base < 2 && b.top > 1 && b.ring?.length >= 3 && !landmarks.pieces.includes(b.piece)) stamp(b.ring, 2);
  for (const span of landmarks.spans)
    for (let e = span.east; e < span.end; e += cell) {
      const x = Math.floor((e - west) / cell),
        y = Math.floor((span.north - south) / cell);
      if (x >= 0 && y >= 0 && x < width && y < height) bits[y * width + x] = 2;
    }
  const rows = Array.from({ length: height }, (_, y) => {
    const runs: number[] = [];
    for (let x = 0; x < width; ) {
      const first = x,
        value = bits[y * width + x++];
      while (x < width && bits[y * width + x] === value) x++;
      if (value) runs.push(first, x, value);
    }
    return runs;
  });
  const data: StreetSurvey = { planHash, origin: [west, south], cell, rows };
  writeFileSync('contracts/world/harbour-district.street-survey.json', JSON.stringify(data) + '\n');
}
