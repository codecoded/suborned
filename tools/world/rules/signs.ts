// F-7: a kit sign (blade or wall) stands clear of every canopy, sign board, shopfront, door and bay, and of the
// megastructure parts and buildings near it. Vela's walk (Parade café, bld/parade-north/12) found the blade passing
// through the canopy slab: the blade hung from the fascia down to head height, through the awning between. The rule
// measures the sign's box from the kit piece's size, pivot and scale against the export's own boxes; it knows nothing
// of how the generator placed the sign.
import type { World } from '../../../contracts/world/world.schema';
import type { Check } from '../../plan/checks';
import { type WCtx, rule } from '../../plan/world-ctx';
import { signBox, signObstacles } from '../gen/box';
import { adoptedBy } from '../gen/kitrefs';

const SOURCE = 'standards.frontage (a trade is a frontage: canopy, sign blank, door, bay) · brief 007 round 3 walk, '
  + 'note 2 (blade signs through canopies) · groundCeiling 4.6, canopy slab 0.25 m at groundCeiling − 0.8';

/** Each kit sign that passes through something, with the first thing it passes through. */
export function signClashes(w: World): { sign: string; mount: string; through: string }[] {
  const obstacles = signObstacles(w.frontages, w.megastructures, w.buildings);
  const out: { sign: string; mount: string; through: string }[] = [];
  for (const f of w.frontages) {
    const k = f.signKit, a = k && adoptedBy(k.id);
    if (!k || !a) continue;
    const hit = obstacles.hits(signBox(a, k))[0];
    if (hit) out.push({ sign: f.id, mount: k.mount, through: hit.what });
  }
  return out;
}

export function signRules(c: WCtx): Check[] {
  const signed = c.w.frontages.filter(f => f.signKit);
  const bad = signClashes(c.w).map(h => `${h.sign} ${h.mount} sign passes through ${h.through}`);
  return [rule('F-7', 'Frontage',
    'A blade or wall sign stands clear of every canopy, sign board, shopfront, door, bay, megastructure part '
    + 'and building',
    signed.length, bad, 'none', SOURCE)];
}
