// Drive rules DR-1..DR-3 (brief 023; Sky 2026-10-19: things go where a real city would put them). A drive is the vehicle crossing from a road's carriageway, over the pavement of
// the plot beside it, to a bay that touches no road (civic-block.json `drives`, block/4, plan frame in `Civic.drives`). The block states it; the world exports it as a flat at road
// level with the kerb dropped along its kerb segment (ground.drives); the engine routes a moving car on it (tools/sim/drive.ts, rulebook car.drive).
// DR-1  a bay that touches no road is reached by a drive: the drive meets the bay, leaves a road that exists at a kerb segment that lies on that road's carriageway edge and on the drive.
// DR-2  a drive crosses only its own plot's pavement: every point of it lies in the plot the bay is in, in no other plot and in no carriageway, and its kerb segment is on that plot's edge.
// DR-3  nothing of the block stands on a drive: no building, tower, structure or object of the civic block (the street generator keeps its items 2 m off: SF-2).
// A block without drives (block/2, /3 and older /4: every recorded run's pinned block) has no bay out of a road's reach unless it says so here, so these rules read it as they always would:
// a block with such a bay fails DR-1, and the drift ledgers name the rule as added.
import type { Check } from './checks';
import type { Civic } from './civic';
import { type V, inPoly, lineDist } from './geom';
import type { Derived, Plan } from './plan';

/** m: how close two edges are to be called touching (the plan's coordinates are rounded to 1 mm). */
export const TOUCH = 0.05;

const ringOf = (poly: V[]): V[] => [...poly, poly[0]!];
/** Points along a ring's edges, `step` m apart, vertices included. */
export function edgePoints(poly: V[], step = 0.25): V[] {
  const out: V[] = [];
  for (let i = 0; i < poly.length; i++) {
    const a = poly[i]!, b = poly[(i + 1) % poly.length]!, n = Math.max(1, Math.ceil(Math.hypot(b[0] - a[0], b[1] - a[1]) / step));
    for (let k = 0; k < n; k++) out.push([a[0] + ((b[0] - a[0]) * k) / n, a[1] + ((b[1] - a[1]) * k) / n]);
  }
  return out;
}
/** Points inside a ring on a `step` m lattice (cell centres, so none lies on an edge of an axis-aligned ring). */
export function insidePoints(poly: V[], step = 0.5): V[] {
  const xs = poly.map(p => p[0]), ys = poly.map(p => p[1]), out: V[] = [];
  for (let x = Math.min(...xs) + step / 2; x < Math.max(...xs); x += step) for (let y = Math.min(...ys) + step / 2; y < Math.max(...ys); y += step) if (inPoly([x, y], poly)) out.push([x, y]);
  return out;
}
const touches = (a: V[], b: V[]): boolean => edgePoints(a).some(p => inPoly(p, b) || lineDist(p, ringOf(b)) <= TOUCH) || edgePoints(b).some(p => inPoly(p, a) || lineDist(p, ringOf(a)) <= TOUCH);

export function driveChecks(plan: Plan, d: Derived, civ: Civic): Check[] {
  const add = (c: Omit<Check, 'group'>): Check => ({ group: 'Civic', ...c });
  const roadOf = new Map(d.roads.map(r => [r.id, r])), drives = civ.drives ?? [];
  const reaches = (p: V) => d.roads.some(r => lineDist(p, r.line) <= r.w / 2 + TOUCH);
  const out: Check[] = [];

  // DR-1
  const bad1: string[] = [], isolated = civ.bays.filter(b => !edgePoints(b.poly).some(reaches));
  for (const b of isolated) {
    const to = drives.filter(x => x.to === b.id);
    if (!to.length) bad1.push(`bay ${b.id} touches no road and no drive reaches it`);
  }
  for (const x of drives) {
    const bay = civ.bays.find(b => b.id === x.to), road = roadOf.get(x.road);
    if (!bay) { bad1.push(`drive ${x.id} reaches bay ${x.to}, which the block does not have`); continue; }
    if (!touches(x.poly, bay.poly)) bad1.push(`drive ${x.id} does not meet bay ${x.to}`);
    if (!road) { bad1.push(`drive ${x.id} leaves road ${x.road}, which the plan does not have`); continue; }
    const off = x.kerb.map(p => Math.abs(lineDist(p, road.line) - road.w / 2));
    if (off.some(o => o > TOUCH)) bad1.push(`drive ${x.id}: its kerb segment is ${Math.max(...off).toFixed(2)} m off ${road.name}'s carriageway edge`);
    const on = x.kerb.map(p => lineDist(p, ringOf(x.poly)));
    if (on.some(o => o > TOUCH)) bad1.push(`drive ${x.id}: its kerb segment is not on the drive (${Math.max(...on).toFixed(2)} m off)`);
  }
  out.push(add({ id: 'DR-1', rule: 'A bay that touches no road is reached by a drive: it meets the bay and leaves an existing road at a kerb segment on that road\'s carriageway edge', value: `${isolated.length} bays out of a road's reach, ${drives.length} drives`, limit: 'each reached by a drive', ok: !bad1.length, ...(bad1.length ? { detail: bad1.join('; ') } : {}), source: 'brief 023 · Sky 2026-10-19 (a bay behind a building is reached from the street by a drive, as a real service yard is) · contracts/world/civic-block.json drives' }));

  // DR-2
  const bad2: string[] = [], plots = d.plots;
  for (const x of drives) {
    const bay = civ.bays.find(b => b.id === x.to); if (!bay) continue;
    const c: V = [bay.poly.reduce((s, p) => s + p[0], 0) / bay.poly.length, bay.poly.reduce((s, p) => s + p[1], 0) / bay.poly.length];
    const own = plots.find(p => inPoly(c, p.polyV));
    if (!own) { bad2.push(`drive ${x.id}: bay ${x.to} is in no plot`); continue; }
    const pts = insidePoints(x.poly), out2 = pts.filter(p => !inPoly(p, own.polyV)), other = plots.find(p => p !== own && pts.some(q => inPoly(q, p.polyV)));
    if (out2.length) bad2.push(`drive ${x.id} leaves its own plot ${own.id} (${out2.length} of ${pts.length} sample points outside it, e.g. ${out2[0]!.map(v => v.toFixed(1)).join(',')})`);
    if (other) bad2.push(`drive ${x.id} crosses the pavement of plot ${other.id}, not its own (${own.id})`);
    const road = d.roads.find(r => pts.some(p => lineDist(p, r.line) < r.w / 2 - TOUCH));
    if (road) bad2.push(`drive ${x.id} lies in the carriageway of ${road.name}`);
    const off = x.kerb.map(p => lineDist(p, ringOf(own.polyV)));
    if (off.some(o => o > TOUCH)) bad2.push(`drive ${x.id}: its kerb segment is not on the edge of its own plot ${own.id} (${Math.max(...off).toFixed(2)} m off)`);
  }
  out.push(add({ id: 'DR-2', rule: 'A drive crosses only its own plot\'s pavement: it lies in the plot its bay is in, in no other plot and in no carriageway, and its kerb segment is on that plot\'s edge', value: `${drives.length} drives`, limit: 'within the bay\'s own plot', ok: !bad2.length, ...(bad2.length ? { detail: bad2.join('; ') } : {}), source: 'brief 023 · Sky 2026-10-19 (a plot\'s owner crosses the pavement of their own plot to reach their yard, never a neighbour\'s)' }));

  // DR-3
  const bad3: string[] = [], solids: { id: string; poly: V[] }[] = [
    ...civ.buildings.flatMap(b => [{ id: b.id, poly: b.poly }, ...(b.towers ?? []).flatMap(t => (t.poly ? [{ id: `${b.id}/${t.id}`, poly: t.poly }] : []))]),
    ...civ.structures.map(s => ({ id: s.id, poly: s.poly })), ...civ.objects.map(o => ({ id: o.id, poly: o.poly })),
  ];
  for (const x of drives) {
    const pts = insidePoints(x.poly);
    for (const s of solids) if (pts.some(p => inPoly(p, s.poly))) bad3.push(`${s.id} stands on drive ${x.id}`);
  }
  out.push(add({ id: 'DR-3', rule: 'Nothing of the civic block (building, tower, structure, object) stands on a drive', value: `${drives.length} drives, ${solids.length} things checked`, limit: 'none on a drive', ok: !bad3.length, ...(bad3.length ? { detail: bad3.join('; ') } : {}), source: 'brief 023 · Sky 2026-10-19 · street items: SF-2 (2 m off), SF-18 (pylons), SF-20 (crossings)' }));
  return out;
}
