// L-8: a connector's ends meet the levels it joins. Latch's walk found stair-bank escalators ending 8 m short of their
// mezzanine and 2 m short of the tower, and core landings off their decks: L-1..L-7 count connectors and ways down but
// never ask whether the foot of a run stands on its lower level or its head on its upper one. Two measures from the
// export alone: the bottom and top heights against the levels named in `from` and `to`, and each end (a run's foot and
// head; a lift's or stair core's footprint) against the surfaces it serves at that level.
//
// Tolerances come from levels.connectors.stair: a riser (0.16) vertically; horizontally two goings (2 × 0.3), the
// joint between two bodies that each stand 0.3 inside their rects (mega/kit.ts INSET). The plan holds no horizontal
// tolerance. Latch's walker steps a gap of up to 0.8 (WalkSurface.Stride), so a joint inside this one is walked.
import type { Connector, World } from '../../../contracts/world/world.schema';
import type { Check } from '../../plan/checks';
import { type V, inPoly, lineDist } from '../../plan/geom';
import { ringGap } from '../../plan/street-zones';
import { TOL, type WCtx, rule } from '../../plan/world-ctx';

const SOURCE = 'standards.levels.list (a level is a height) · levels.connectors.stair (riser 0.16: the vertical '
  + 'tolerance; going 0.3: the horizontal is two goings, a joint between two decks each inset 0.3) · '
  + 'stationEntrance (escalator runs A and B, stair tower, mezzanine) · brief 007 round 3 walk: escalators and core '
  + 'landings missing their levels';

type Piece = { ring: V[]; level: string };
const pointGap = (p: V, ring: V[]): number => (inPoly(p, ring) ? 0 : lineDist(p, [...ring, ring[0]!]));
const isRun = (k: Connector): boolean => Math.hypot(k.head[0] - k.foot[0], k.head[1] - k.foot[1]) > 0.1;

/** The heights a level name stands for at a connector's host: the world's levels, or the megastructure's own. */
function heightsOf(c: WCtx, k: Connector, name: string): number[] | null {
  if (name === 'street') return [c.w.ground.plots[0]?.height ?? 0]; // the pavement top
  const lv = c.w.levels.find(l => l.name === name);
  if (lv) return [lv.height];
  const mega = (c.plan.megastructures ?? []).find(m => `mega/${m.id}` === k.host);
  if (!mega) return null;
  if (name === 'frame') return [(mega['frame'] as [number, number])[1]]; // frame, discs, saucer: the Skyport's own
  const own = mega.levels.find(l => l.level === name) as { z?: number; tops?: number[] } | undefined;
  return own?.z === undefined ? null : [own.z, ...(own.tops ?? [])];
}

/** Why a connector misses a level, or nothing. */
export function endProblems(c: WCtx, k: Connector, pieces: Map<string, Piece>, towers: Connector[]): string[] {
  const stair = c.L.connectors.stair, riser = stair.riser as number, joint = 2 * (stair.going as number);
  const towerTop = c.S.transit.stationEntrance.stairTower.size[2] as number, out: string[] = [];
  const ends: [string, string, number, V][] = [['bottom', k.from, k.bottom, k.foot], ['top', k.to, k.top, k.head]];
  for (const [name, level, z, at] of ends) {
    const zs = heightsOf(c, k, level);
    // a stair tower rises past the concourse by its roof and headroom: its top is anywhere up to its own height
    const roof = k.kind === 'stairTower' && name === 'top' && zs && z >= zs[0]! - TOL && z <= towerTop + TOL;
    if (zs && !roof && !zs.some(h => Math.abs(z - h) <= riser + TOL)) {
      out.push(`${name} ${z} m is not at ${level} (${zs.join(' / ')} m)`);
    }
    if (level === 'street' || !c.w.levels.some(l => l.name === level)) continue;
    const gap = (ring: V[]) => (isRun(k) ? pointGap(at, ring) : ringGap(k.ring, ring));
    const lands = k.serves.flatMap(id => {
      const piece = pieces.get(id);
      return piece?.level === level ? [{ id, g: gap(piece.ring) }] : [];
    });
    // on the concourse a stair tower is a landing too: the lift and the escalator run B meet it, it meets the platform
    const hubs = level === 'concourse' ? towers.filter(t => t !== k).map(t => ({ id: t.id, g: gap(t.ring) })) : [];
    const near = [...lands, ...hubs].sort((a, b) => a.g - b.g)[0], end = name === 'bottom' ? 'foot' : 'head';
    if (!near) out.push(`${end} meets no ${level} surface it serves`);
    else if (near.g > joint + TOL) out.push(`${end} ${near.g.toFixed(1)} m from ${near.id}`);
  }
  return out;
}

const piecesOf = (w: World): Map<string, Piece> => new Map<string, Piece>([
  ...w.levels.flatMap(l => l.decks.map(d => [d.id, { ring: d.ring, level: l.name }] as const)),
  ...w.transit.stations.map(s => [s.id, { ring: s.ring, level: 'concourse' }] as const),
]);

/** Each connector that misses a level, with why. */
export function connectorProblems(c: WCtx): string[] {
  const pieces = piecesOf(c.w), towers = c.w.connectors.filter(k => k.kind === 'stairTower');
  return c.w.connectors.flatMap(k => {
    const why = endProblems(c, k, pieces, towers);
    return why.length ? [`${k.id}: ${why.join(', ')}`] : [];
  });
}

export function connectorRules(c: WCtx): Check[] {
  return [rule('L-8', 'Levels', 'A connector\'s bottom and top stand at the levels it joins, and its ends touch a '
    + 'surface at that level it serves', c.w.connectors.length, connectorProblems(c), 'none', SOURCE)];
}
