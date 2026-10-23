// RF-3 (brief 028 Road, Sky 2026-10-22 "can try"): the road finish the generator lays (tools/world/gen/street/roadfinish.ts: ground.pieces kinds road-ribbon, road-shoulder, road-pinstripe, road-lozenge, road-seam)
// is what standards.streetscape.roads says. Every piece is an adopted kit piece of its family's use tag, lies at the level of the surface it is laid on, and nothing of it lies in a crossing strip, a drive or a
// tram platform or within stopShort of one (the breaks: the road's light and the crossing's light never join, a drive's kerb is dropped, a platform edge is its own); lozenges on one carriageway keep their
// spacing. The plan-side clauses (the standard's own keys, the kerb faces) are RF-1 in tools/plan/road-finish.ts. Failing examples: road-finish.test.ts in this folder.
import type { StreetItem } from '../../../contracts/world/world.schema';
import type { Check } from '../../plan/checks';
import { type V, inPoly, segDist } from '../../plan/geom';
import { type WCtx, rule, stat } from '../../plan/world-ctx';
import { adopted } from '../gen/kitrefs';

const G = 'Streetscape';
const SRC = 'streetscape.roads (breaks, lozenges, shoulder, pinstripe) · brief 026/028 Road, Sky 2026-10-22 · tools/world/gen/street/roadfinish.ts';
export const ROAD_FINISH_USE: Record<string, string> = { 'road-ribbon': 'ground:road-ribbon', 'road-shoulder': 'ground:road-shoulder', 'road-pinstripe': 'ground:pave-pinstripe', 'road-lozenge': 'ground:road-lozenge', 'road-seam': 'ground:road-seam' };
/** The levels a piece lies at: the carriageway, a kerbed bay's floor (FLAT_FOOT); a pinstripe at the pavement top, a foot way or the land under a circus band. */
const LEVEL: Record<string, number[]> = { 'road-ribbon': [0, 0.02], 'road-shoulder': [0, 0.02], 'road-lozenge': [0], 'road-seam': [0], 'road-pinstripe': [0.18, 0.02, -0.03] };

const near = (p: V, ring: V[], g: number) => inPoly(p, ring) || ring.some((q, i) => segDist(p, q, ring[(i + 1) % ring.length]!) < g);
/** The points of a piece that must stand clear of a break: its centre and the two ends of its length. */
export function endsOf(i: Pick<StreetItem, 'at' | 'yaw' | 'size' | 'kind'>): V[] {
  const across = i.kind === 'road-seam', len = across ? i.size[1] : i.size[0], u: V = across ? [Math.sin(i.yaw), -Math.cos(i.yaw)] : [Math.cos(i.yaw), Math.sin(i.yaw)];
  return [i.at, [i.at[0] + u[0] * len / 2, i.at[1] + u[1] * len / 2], [i.at[0] - u[0] * len / 2, i.at[1] - u[1] * len / 2]];
}

export function roadFinishRules(c: WCtx): Check[] {
  const R = (c.plan.standards as any).streetscape?.roads, mine = (c.w.ground.pieces ?? []).filter(s => ROAD_FINISH_USE[s.kind]);
  if (!R?.breaks || (!mine.length && !c.gen.roadFinish)) return [];
  const by = new Map(adopted().map(a => [a.id, a])), stop: number = R.breaks.stopShort, lz = R.lozenges;
  const rings: { what: string; ring: V[] }[] = [
    ...(c.w.crossings ?? []).map(x => ({ what: 'crossing strip', ring: x.strip as V[] })), ...(c.w.ground.drives ?? []).map(x => ({ what: 'drive', ring: x.ring as V[] })), ...c.w.transit.stops.map(x => ({ what: 'tram platform', ring: x.ring as V[] })),
  ];
  const bad: string[] = [], badBreak: string[] = [], badLevel: string[] = [], badSpace: string[] = [];
  for (const s of mine) {
    const a = s.kit && by.get(s.kit.id);
    if (!a || !a.tags.use.includes(ROAD_FINISH_USE[s.kind]!)) bad.push(`${s.id}: ${s.kit?.id ?? 'no piece'} is not an adopted ${ROAD_FINISH_USE[s.kind]} piece`);
    if (!LEVEL[s.kind]!.some(z => Math.abs(z - s.z) < 0.001)) badLevel.push(`${s.id}: ${s.kind} at z ${s.z}, not ${LEVEL[s.kind]!.join(' or ')}`);
    const pts = endsOf(s);
    for (const r of rings) if (pts.some(p => near(p, r.ring, stop - 0.25))) { badBreak.push(`${s.id} lies on or within ${stop} m of a ${r.what}`); break; } // 0.25: the generator's 0.2 m sampling step
  }
  const lines = new Map<string, V[]>();
  for (const s of mine.filter(x => x.kind === 'road-lozenge')) (lines.get(s.edge) ?? lines.set(s.edge, []).get(s.edge)!).push(s.at);
  for (const [edge, pts] of lines) for (let i = 0; i < pts.length; i++) for (let j = i + 1; j < pts.length; j++) if (Math.hypot(pts[i]![0] - pts[j]![0], pts[i]![1] - pts[j]![1]) < lz.length + 2 * stop - 0.01) badSpace.push(`${edge}: two lozenges ${Math.hypot(pts[i]![0] - pts[j]![0], pts[i]![1] - pts[j]![1]).toFixed(2)} m apart (< ${lz.length} + 2 × ${stop})`);
  const log = c.gen.roadFinish, counts = Object.entries(log?.counts ?? {}).map(([k, n]) => `${k} ${n}`).join(', ');
  return [
    rule('RF-3', G, 'Every road finish piece is an adopted kit piece of its own kind at the level of the surface it lies on; nothing of it lies in a crossing strip, a drive or a tram platform or within stopShort of one; lozenges on one carriageway keep their length and stopShort apart', mine.length, [...bad, ...badLevel, ...badBreak, ...badSpace], 'none', SRC),
    stat('RF-3/n', G, 'Road finish (informational): pieces by kind, metres of kerb face laid, and the road edge no face covers', `${mine.length} pieces`, '-', true, SRC, `${counts}; kerb face ${Math.round(log?.kerb.edgeM ?? 0)} m, ${Math.round(log?.kerb.laidM ?? 0)} m ribbon laid; uncovered road edge ${log?.kerb.uncoveredM ?? 0} m`),
  ];
}
