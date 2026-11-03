// The Handover's fixed legs and the poles its host stands, as DATA for the geometry audit's leg rule (Pulse, 2026-10-24; Sky playing 0.2.99: "the robot patrol is
// able to go through the camera pole"). The rule itself (a leg keeps standards.walking.body.r from every drawn solid, never enters a building, and every round and
// cast leg stands on the drawn floor) is one rule with one owner, Splice's `bun run geometry` (tools/geometry/rules.ts, with its ratchet). This file only says what
// is walked and what the world does not place; it checks nothing and is read by no overlay rule.
//
// A fixed leg is a straight line the engine walks without routing: the patrol's sweep (patrol-a to patrol-b, Game.SweepPos, a straight lerp) and the place the
// rerouted patrol waits (patrol-reroute, a leg of no length). Every other walker is routed on the walk grid: the guard, Vale, the rounds (sim/data/handover.rounds.json
// paths) and the cast's routes; those the audit reads from their own files.
import type { V } from '../../plan/geom';

/** The mission camera pole's scale in the host (unity/.../HostViews.Attention.cs PoleScale: tools/world/gen/kitrefs.ts fits CCTVPole's 3.18 m to the plan's 5.5 m mount). */
export const POLE_SCALE = { x: 1.182, y: 1.728, z: 1.23 } as const;
/** kit.collide.json's rule: a walker needs HEAD of room over the floor and climbs at most STEP (the same two numbers tools/sim/solids.ts holds). */
const HEAD = 1.8, STEP = 0.35;

/** A fixed leg in the block frame (m). `a` and `b` are where the engine stands the two ends (see `stand`). */
export type Leg = { id: string; what: string; a: V; b: V };
type M = { points: { id: string; at?: V }[]; cameras?: { id: string; kind?: string; at: V }[] };

/** The mission's fixed legs. `stand` is Site.Stand: the point itself, or the nearest free cell centre of the walk grid when the point is not on free ground. */
export function fixedLegs(m: M, stand: (p: V) => V = p => p): Leg[] {
  const at = (id: string): V => { const p = m.points.find(x => x.id === id); if (!p?.at) throw new Error(`mission.json has no point ${id}`); return stand(p.at); };
  const a = at('patrol-a'), b = at('patrol-b'), r = at('patrol-reroute');
  return [
    { id: 'patrol sweep patrol-a to patrol-b', what: 'the patrol robot walks the straight line between the two (Game.SweepPos)', a, b },
    { id: 'patrol-reroute', what: 'the rerouted patrol robot waits here', a: r, b: r },
  ];
}

type Piece = { volumes: ({ shape: 'box'; min: number[]; max: number[] } | { shape: 'cylinder'; c: number[]; r: number; y0: number; y1: number })[] };
/** The pole of a mission camera as one circle that holds every blocking volume of the kit piece (kit.collide.json CCTVPole) at the host's scale, centred on the pole's foot. */
export function poleCircle(piece: Piece, at: V): { c: V; r: number } {
  const S = POLE_SCALE;
  let r = 0;
  for (const v of piece.volumes) {
    const y0 = (v.shape === 'box' ? v.min[1]! : v.y0) * S.y, y1 = (v.shape === 'box' ? v.max[1]! : v.y1) * S.y;
    if (!(y0 < HEAD && y1 > STEP)) continue; // blocks a walker on the pavement
    if (v.shape === 'cylinder') r = Math.max(r, Math.hypot(v.c[0]! * S.x, v.c[1]! * S.z) + v.r * Math.max(S.x, S.z));
    else for (const x of [v.min[0]!, v.max[0]!]) for (const z of [v.min[2]!, v.max[2]!]) r = Math.max(r, Math.hypot(x * S.x, z * S.z));
  }
  return { c: at, r };
}

/** The mission cameras' poles, which the world does not place (the host stands Vela's CCTVPole on each pole camera): id and footprint circle, block frame. */
export function cameraPoles(m: M, collide: { pieces: Record<string, Piece> }): { id: string; c: V; r: number }[] {
  const pole = collide.pieces.CCTVPole;
  if (!pole) throw new Error('kit.collide.json has no CCTVPole: the mission cameras\' poles have no footprint');
  return (m.cameras ?? []).filter(c => (c.kind ?? 'pole-camera') === 'pole-camera').map(c => ({ id: `camera pole ${c.id}`, ...poleCircle(pole, c.at) }));
}
