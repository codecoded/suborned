// Brief 098 step 1 (law L8): the Handover zone's lanes are what tools/sim/lanes.ts makes from the plan, keep right, join up, and leave by mouths.
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { buildLanesFile, LANES_FILE, type LanesFile } from './lanes';

const root = join(import.meta.dir, '..', '..');
const file = JSON.parse(readFileSync(join(root, LANES_FILE), 'utf8')) as LanesFile;

describe('sim/data/handover.lanes.json (brief 098 step 1, L8)', () => {
  test('is what tools/sim/lanes.ts makes from the plan, life.json and the drive grid today (bun tools/sim/lanes.ts)', () => {
    expect(readFileSync(join(root, LANES_FILE), 'utf8')).toBe(buildLanesFile(root));
  });
  test('every lane keeps to the right of its road\'s centreline, two ways on every road', () => {
    for (const l of file.lanes) {
      const a = l.line[0]!, b = l.line[l.line.length - 1]!, h = [b[0] - a[0], b[1] - a[1]], n = Math.hypot(h[0]!, h[1]!);
      const twin = file.lanes.find((x) => x.id === l.id.slice(0, -2) + (l.dir === '+' ? '-' : '+') + l.k)!;
      expect(twin).toBeDefined();
      // the twin runs the other way; this lane lies on its own right: (h.y, -h.x) points from the twin to it
      const t = twin.line[0]!, side = ((a[0] - t[0]) * h[1]! - (a[1] - t[1]) * h[0]!) / n;
      expect(side).toBeGreaterThan(0);
    }
  });
  test('every turn starts where its lane in ends and ends where its lane out starts, and no turn is a U-turn', () => {
    const byId = new Map(file.lanes.map((l) => [l.id, l]));
    for (const j of file.junctions) for (const t of j.turns) {
      const a = byId.get(t.from)!, b = byId.get(t.to)!;
      expect(a.to).toBe(j.id); expect(b.from).toBe(j.id);
      expect(t.line[0]).toEqual(a.line[a.line.length - 1]!); expect(t.line[t.line.length - 1]).toEqual(b.line[0]!);
      expect(a.id.slice(0, -2)).not.toBe(b.id.slice(0, -2));
    }
  });
  test('the four roads leaving the window are its mouths, each a way in and a way out, and every mouth reaches every other', () => {
    expect(file.mouths.map((m) => m.road).sort()).toEqual(['apron-road', 'orbit-walk', 'rocket-lane', 'starline-street']);
    const next = new Map<string, string[]>(); for (const j of file.junctions) for (const t of j.turns) next.set(t.from, [...(next.get(t.from) ?? []), t.to]);
    for (const m of file.mouths) {
      expect(m.in.length).toBeGreaterThan(0); expect(m.out.length).toBeGreaterThan(0);
      const seen = new Set(m.in), q = [...m.in];
      while (q.length) for (const n of next.get(q.shift()!) ?? []) if (!seen.has(n)) { seen.add(n); q.push(n); }
      for (const o of file.mouths) if (o !== m) expect(o.out.some((x) => seen.has(x))).toBe(true);
    }
  });
});
