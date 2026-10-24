// The civic block's roofs (brief 048, Vela, 2026-10-24; Glyph: "the grey slab" in the Records Office frames; Latch: it is the office wing's bare 15.68 m roof in the towers' shade).
// The plot buildings get a roof programme from standards.buildings.roofs (gen/roofs.ts); the civic block's two buildings stand outside that list, so their roofs were the bare face of the mass. This gives
// each one a roof terrace in the civic block's own language (pale paving with seafoam bands and a border line, planted troughs, a finned vent, a cabana) from the same prims and the same kit piece
// the plot roofs use, so Unity draws it with RoofKit as it draws the rest. Nothing is placed by hand: the terrace is fitted to the mass's footprint, keeps clear of the towers it stands round, and
// keeps every item low (<= 2 m, the vents 3.5): from the play camera (pitch 38) an item hides only what lies within 1.3 x its height beyond it, and the 15.5 m wing already hides 20 m.
import type { Mass, Prim, Roof } from '../../../contracts/world/world.schema';
import type { V } from '../../plan/geom';
import { r3, r4, rv } from '../gen/common';
import { fit, pointInRing } from '../gen/geo';
import { rocketVent } from '../gen/roofs';

/** Civic buildings that carry a roof terrace: limestone masses straight under `civic/` (not a tower, a pavilion or a bed), tall enough to be a building. */
const isBuilding = (m: Mass): boolean => m.surface === 'limestone' && /^civic\/[^/]+$/.test(m.id) && m.height > 8 && !m.hidden && !m.kit;

export function civicRoofs(civic: Mass[]): Roof[] {
  const out: Roof[] = [];
  for (const m of civic.filter(isBuilding)) {
    const towers = civic.filter(t => t.id.startsWith(`${m.id}/tower-`)), top = r3(m.base + m.height), id = `${m.id}/roof`;
    const f = fit(m.ring, 0.9, false); // inside the cornice (0.35) and the parapet (0.6) and a little: the terrace is the usable rect
    if (f.W < 8 || f.D < 5) continue;
    const U: V = [Math.cos(f.yaw), Math.sin(f.yaw)], Vv: V = [-U[1], U[0]];
    const parts: Prim[] = [];
    const P = (n: string, shape: Prim['shape'], surface: string, lu: number, lv: number, size: [number, number, number], base = 0, yawAdd = 0): void => {
      parts.push({ id: `${id}/${n}`, shape, surface, c: rv([f.c[0] + U[0] * lu + Vv[0] * lv, f.c[1] + U[1] * lu + Vv[1] * lv]), base: r3(top + base), size: [r3(size[0]), r3(size[1]), r3(size[2])], yaw: r4(f.yaw + yawAdd) });
    };
    const W = f.W, D = f.D, T = 0.06; // T: the deck's thickness above the roof face
    const world = (lu: number, lv: number): V => [f.c[0] + U[0] * lu + Vv[0] * lv, f.c[1] + U[1] * lu + Vv[1] * lv];
    /** an item's footprint (a half-width and half-depth about its centre, in the roof's frame) overlaps a tower's plan (grown 0.8 m) */
    const onTower = (lu: number, lv: number, hu: number, hv: number): boolean => towers.some(t => [[-1, -1], [1, -1], [1, 1], [-1, 1], [0, 0]].some(([a, b]) => {
      const q = world(lu + a! * hu, lv + b! * hv), xs = t.ring.map(p => p[0]), ys = t.ring.map(p => p[1]);
      return q[0] > Math.min(...xs) - 0.8 && q[0] < Math.max(...xs) + 0.8 && q[1] > Math.min(...ys) - 0.8 && q[1] < Math.max(...ys) + 0.8;
    }));
    const inside = (lu: number, lv: number, hu: number, hv: number): boolean => [[-1, -1], [1, -1], [1, 1], [-1, 1]].every(([a, b]) => pointInRing(world(lu + a! * hu, lv + b! * hv), m.ring));

    // the deck: pale paving over the whole roof rect, then the border line (a seafoam inlay 0.3 m, 0.45 m in from the edge) and two bands the length of the roof (the plaza's bands, 0.5 m)
    P('deck', 'box', 'deck', 0, 0, [W, D, T]);
    const bw = 0.3, bi = 0.45;
    if (W >= 12 && D >= 7) {
      P('border-n', 'box', 'trim', 0, D / 2 - bi, [W - 2 * bi, bw, 0.05], T); P('border-s', 'box', 'trim', 0, -D / 2 + bi, [W - 2 * bi, bw, 0.05], T);
      P('border-e', 'box', 'trim', W / 2 - bi, 0, [bw, D - 2 * bi - 2 * bw, 0.05], T); P('border-w', 'box', 'trim', -W / 2 + bi, 0, [bw, D - 2 * bi - 2 * bw, 0.05], T);
    }
    if (D >= 12) for (const sg of [-1, 1]) if (inside(0, sg * D / 6, (W - 4) / 2, 0.25) && !onTower(0, sg * D / 6, (W - 4) / 2, 0.25)) P(`band-${sg > 0 ? 'n' : 's'}`, 'box', 'trim', 0, sg * D / 6, [W - 4, 0.5, 0.05], T);

    // planted troughs along both long edges, clear of the towers (hedge boxes 0.8 m, a topiary ball on each: 2 m)
    let k = 0;
    const trough = (lu: number, lv: number, len: number): void => {
      if (!inside(lu, lv, len / 2, 0.5) || onTower(lu, lv, len / 2, 0.5)) return;
      P(`trough-${k}`, 'box', 'park', lu, lv, [len, 1.0, 0.8], T); P(`ball-${k}`, 'sphere', 'park', lu, lv, [1.4, 1.4, 1.4], T + 0.5); k++;
    };
    const nT = Math.max(2, Math.floor((W - 3) / 7));
    for (let i = 0; i < nT; i++) for (const sg of [-1, 1]) trough(-W / 2 + 1.5 + ((i + 0.5) / nT) * (W - 3), sg * (D / 2 - 1.6), Math.min(5, (W - 3) / nT - 1.2));

    // a finned vent at two corners (no kit cabana: its coral awning was the loudest thing on the roof, the very thing Glyph flagged)
    for (const [i, [lu, lv]] of ([[-1, -1], [1, 1]] as [number, number][]).entries()) { const x = lu * (W / 2 - 1.6), y = lv * (D / 2 - 1.6); if (inside(x, y, 1, 1) && !onTower(x, y, 1, 1)) rocketVent(P, `rocket-vent-${i}`, x, y, [2, 2, 3.5]); }
    out.push({ id, building: m.id, programme: 'roof-garden', surface: 'roof', z: top, c: rv(f.c), size: [r3(W), r3(D)], yaw: r4(f.yaw), parapet: 0.6, parts, form: 'terrace' });
  }
  return out;
}
