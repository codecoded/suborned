// SF-18: a sky rail pylon's footprint stands clear of everything that is already on the ground there. Vela's walk
// (note 3, circus/crown) found S1/trunk's pylon at 6.5, 0 standing on the Crown Circus monument's steps: the pylon
// zones (island, median, mall, park, quay) say where a pylon may stand, not what else stands there. The monument's
// steps are part of its kit piece, so the landmark's plan disc stands for them. Doorways and crossings keep SF-2's
// 2 m.
import type { World } from '../../../contracts/world/world.schema';
import type { Check } from '../../plan/checks';
import type { V } from '../../plan/geom';
import { boxRing, ringGap } from '../../plan/street-zones';
import { TOL, type WCtx, itemRing, rule } from '../../plan/world-ctx';
import { discRing, primBox } from '../gen/box';
import { nearestLine } from './line';

const SOURCE = 'standards.skyrail.pylonZones (where a pylon may stand) · brief 007 round 3 walk, note 3 (pylons on '
  + 'the Crown Circus monument steps) · streetscape.crossings and SF-2 (2 m from a crossing or a doorway)';
/** Metres a pylon keeps from a crossing or a doorway: SF-2's own distance. */
const DOORWAY_CLEAR = 2;
const STEPS = new Set(['stair', 'ramp', 'cartRamp', 'escalator', 'stairTower']);

type Thing = { what: string; ring: V[]; clear: number };

/** What stands on the ground and a pylon must keep off, with the clear distance each needs. */
function groundThings(w: World): Thing[] {
  const onGround = (z: number) => z < 1; // the pavement is 0.18
  const keep = (what: string, ring: V[], clear = 0): Thing => ({ what, ring, clear });
  const solids = [...w.buildings, ...w.megastructures.flatMap(m => m.parts), ...w.transit.stops];
  return [
    ...w.connectors.filter(k => STEPS.has(k.kind) && onGround(k.bottom))
      .map(k => keep(`the ${k.kind} ${k.id}`, k.ring)),
    ...w.landmarks.map(l => keep(`the landmark ${l.id} (and its steps)`, discRing(l.centre, l.radius))),
    ...w.street.filter(s => s.kind === 'crossing').map(s => keep(`the crossing ${s.id}`, itemRing(s), DOORWAY_CLEAR)),
    ...w.street.filter(s => s.kind === 'station-entrance')
      .map(s => keep(`the entrance ${s.id}`, itemRing(s), DOORWAY_CLEAR)),
    ...w.ground.flats.filter(f => f.surface === 'bay').map(f => keep(`the bay ${f.id}`, f.ring)),
    ...w.frontages.filter(f => onGround(f.z)).flatMap(f => [
      keep(`the door of ${f.id}`, primBox(f.entrance).ring, DOORWAY_CLEAR),
      ...(f.service ? [keep(`the service bay of ${f.id}`, primBox(f.service).ring)] : []),
    ]),
    ...solids.filter(m => onGround(m.base)).map(m => keep(m.id, m.ring)),
  ];
}

export function pylonRules(c: WCtx): Check[] {
  const things = groundThings(c.w), bad: string[] = [];
  let total = 0;
  for (const s of c.w.transit.skyRail) {
    for (const p of s.pylons) {
      total++;
      const ring = boxRing(p, s.pylonSize, s.pylonSize, nearestLine([s.line], p).yaw);
      for (const t of things) {
        const gap = ringGap(ring, t.ring);
        if (gap >= (t.clear > 0 ? t.clear - TOL : TOL)) continue; // SF-2's own test: nearer than its clear distance
        bad.push(`${s.id} pylon ${p[0]}, ${p[1]} stands ${gap < TOL ? 'on' : `${gap.toFixed(1)} m from`} ${t.what}`);
        break;
      }
    }
  }
  return [rule('SF-18', 'Sky rail', 'A sky rail pylon\'s footprint stands clear of steps, landmarks, bays and '
    + 'buildings, and 2 m from crossings and doorways', total, bad, 'none', SOURCE)];
}
