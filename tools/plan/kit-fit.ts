// K-1: every kit ref in the generated world fits the slot the plan gives it. Landmarks: scaled piece inside the plan disc (r) and under
// `top`; the skyport's pads inside its caisson. Roofs: the kit piece inside the usable roof rect. Street: inside the item's slot (kerb zone,
// size catalogue). Gateways: the feet inside the piece and clear of the quay edge, the pier walk, the pylons and the beam. Regenerated towers:
// tip <= towers.crownTip. K-2: a regenerated tower stands to the tip its order states. Reads the world in process; the kit's sizes come from
// contracts/world/kit.json.
import type { KitRef } from '../../contracts/world/world.schema';
import { scaled } from '../kit/fit';
import { ctxOf as genCtx } from '../world/gen/common';
import { adopted, slotOf } from '../world/gen/kitrefs';
import { PART_USE, fitsPart } from '../world/gen/street/partkit';
import type { Check } from './checks';
import { type V, inPoly, lineDist } from './geom';
import { quayBlock } from './street-zones';
import { type R4, type WCtx, TIP_TOL, TOL, f1, rule } from './world-ctx';

const rectPts = (r: R4, step = 0.5): V[] => {
  const o: V[] = [];
  for (let x = r[0]; x <= r[1] + 1e-9; x += step) o.push([x, r[2]], [x, r[3]]);
  for (let y = r[2]; y <= r[3] + 1e-9; y += step) o.push([r[0], y], [r[1], y]);
  return o;
};
/** The four corners of a piece's footprint (kit x along yaw, kit z across), centred at `at`. */
const corners = (size: [number, number, number], at: V, yaw: number): V[] =>
  ([[-1, -1], [1, -1], [1, 1], [-1, 1]] as const).map(([a, b]) => [at[0] + Math.cos(yaw) * a * size[0] / 2 - Math.sin(yaw) * b * size[2] / 2, at[1] + Math.sin(yaw) * a * size[0] / 2 + Math.cos(yaw) * b * size[2] / 2]);

export function kitFitChecks(c: WCtx): Check[] {
  const { plan, d, w, S } = c, G = 'Kit', by = new Map(adopted().map(a => [a.id, a])), bad: string[] = [];
  let total = 0;
  const piece = (where: string, k: KitRef) => { total++; const a = by.get(k.id); if (!a) bad.push(`${where}: ${k.id} is not adopted`); return a && { a, s: scaled(a, k.scale) }; };

  // landmarks: inside the plan disc and under top; pads inside the caisson
  for (const l of w.landmarks) {
    const p = plan.landmarks.find(x => `landmark/${x.id}` === l.id)!;
    if (l.kit) { const k = piece(l.id, l.kit); if (k && (Math.max(k.s[0], k.s[2]) > 2 * p.r + TOL || k.s[1] > (p.top ?? Infinity) + TOL)) bad.push(`${l.id}: ${k.a.id} ${k.s.map(f1).join('×')} m over disc Ø${2 * p.r} × ${p.top}`); }
    for (const [i, kr] of (l.kits ?? []).entries()) {
      const k = piece(`${l.id}/pad-${i}`, kr); if (!k) continue;
      const cr = p.caisson?.r ?? p.r, far = Math.max(...corners(k.s, kr.at, kr.yaw ?? 0).map(q => Math.hypot(q[0] - l.centre[0], q[1] - l.centre[1])));
      if (far > cr + TOL) bad.push(`${l.id}/pad-${i}: corner ${f1(far)} m from the centre, caisson r ${cr}`);
      const up = (p.caisson?.deck ?? 0) + k.s[1]; if (p.top !== undefined && up > p.top + TOL) bad.push(`${l.id}/pad-${i}: top ${f1(up)} m over ${p.top}`);
    }
  }
  // roofs: the kit piece inside the usable roof rect (the programme's minRoof is SF-15)
  for (const r of w.roofs) if (r.kit) {
    const k = piece(r.id, r.kit); if (!k) continue;
    const cs = corners(k.s, r.kit.at, r.kit.yaw ?? 0), ux: V = [Math.cos(r.yaw), Math.sin(r.yaw)];
    const off = cs.map(q => [(q[0] - r.c[0]) * ux[0] + (q[1] - r.c[1]) * ux[1], -(q[0] - r.c[0]) * ux[1] + (q[1] - r.c[1]) * ux[0]] as V);
    if (off.some(o => Math.abs(o[0]) > r.size[0] / 2 + TOL || Math.abs(o[1]) > r.size[1] / 2 + TOL)) bad.push(`${r.id}: ${k.a.id} ${k.s.map(f1).join('×')} m outside the roof ${r.size.map(f1).join('×')}`);
  }
  // street: inside the item's slot
  const g = genCtx(plan, d, c.civ);
  for (const s of w.street) if (s.kit) {
    const k = piece(s.id, s.kit), slot = slotOf(g, s); if (!k) continue;
    if (PART_USE[s.kind]) { // a crossing's part: its slot is its own size, and the piece is the one of its use, unscaled
      if (!k.a.tags.use.includes(PART_USE[s.kind]!) || s.kit.scale.some(v => v !== 1) || !fitsPart(k.a.size, s)) bad.push(`${s.id}: ${k.a.id} ${k.a.size.map(f1).join('×')} m is not the ${PART_USE[s.kind]} piece of the part's ${s.size.map(f1).join('×')} m`);
      continue;
    }
    if (!slot) { bad.push(`${s.id}: a ${s.kind} has no slot`); continue; }
    if ((slot.along !== undefined && k.s[0] > slot.along + TOL) || (slot.depth !== undefined && k.s[2] > slot.depth + TOL) || (slot.maxH !== undefined && k.s[1] > slot.maxH + TOL)) bad.push(`${s.id}: ${k.a.id} ${k.s.map(f1).join('×')} m over slot ${slot.along ?? '-'}×${slot.depth ?? '-'}×${slot.maxH ?? '-'}`);
  }
  // gateways: the plan's feet inside the piece's footprint; clear of the quay edge (SF-14 1.5 m), the pier walk, the pylons (3 m) and the beam envelope
  const sky = d.sky.map(p => p.samples.map(s => s.p as V)), pylonHalf = (S.skyrail?.pylonSize ?? c.plan.standards.skyrail.pylonSize ?? 1.6) / 2, beamEnv = plan.standards.skyrail.beamCentres / 2 + 0.75;
  for (const m of plan.megastructures ?? []) {
    const gw = (m as any).gateway as { kit: string; feet: R4[] } | undefined, ref = w.megastructures.find(x => x.id === `mega/${m.id}`)?.gateway;
    if (!gw) continue;
    if (!ref) { total++; bad.push(`${m.id}: gateway ${gw.kit} not emitted`); continue; }
    const k = piece(`${m.id}/gateway`, ref); if (!k) continue;
    const box = corners(k.s, ref.at, ref.yaw ?? 0);
    gw.feet.forEach((f, i) => {
      const id = `${m.id}/gateway/foot-${i}`, pts = rectPts(f);
      if (rectPts(f, 6.4).some(q => !pointInQuad(q, box, TOL))) bad.push(`${id}: outside the piece's footprint`);
      if (!pts.every(p => plan.quay.some(r => inPoly(p, r)))) bad.push(`${id}: not on the quay`); else if (quayBlock(c, pts)) bad.push(`${id}: within 1.5 m of the quay edge`);
      for (const fp of plan.foot) { const e = Math.min(...pts.map(p => lineDist(p, fp.pts))) - fp.w / 2; if (e < -TOL && (m.spans.foot ?? []).includes(fp.id)) bad.push(`${id}: on the ${fp.id} walk`); }
      for (const p of d.pylons) { const e = Math.min(...pts.map(q2 => Math.hypot(q2[0] - p.p[0], q2[1] - p.p[1]))) - pylonHalf; if (e < 3 - TOL) bad.push(`${id}: ${f1(e)} m from pylon ${f1(p.p[0])},${f1(p.p[1])} (3)`); }
      const bd = Math.min(...sky.flatMap(l => pts.map(p => lineDist(p, l)))); if (bd < beamEnv - TOL) bad.push(`${id}: ${f1(bd)} m from the beam (envelope ${beamEnv})`);
    });
  }
  // regenerated towers: the tip stays under the crown ceiling (the Skyport Tower, the district's tallest mark, is not a tower site)
  const T = plan.standards.buildings.towers, sites = (T.sites ?? []) as { id: string; regen?: { tip?: number; same?: string } }[];
  for (const s of sites) if (s.regen?.tip !== undefined) { total++; if (s.regen.tip > T.crownTip + TOL) bad.push(`tower ${s.id}: tip ${s.regen.tip} > crownTip ${T.crownTip}`); }

  // K-2: a regenerated piece is fixed-size (regenerate, never stretch), so the plan's stated tip and the placed piece can part. The built tip is
  // the world's own placement of the site's piece: its base (the podium deck) + the piece's height x its scale. A matched pair (regen.same) takes its partner's tip.
  const bad2: string[] = [];
  let total2 = 0;
  for (const s of sites) {
    const stated = s.regen?.tip ?? sites.find(o => o.id === s.regen?.same)?.regen?.tip, placed = w.signatures.find(x => x.id === `tower/${s.id}`), a = placed?.kit && by.get(placed.kit.id);
    if (stated === undefined || !placed?.kit || !a) continue; // no tip stated, or the piece is not adopted yet (pending: the procedural tower stands): nothing built to hold against it
    total2++;
    const built = placed.base + scaled(a, placed.kit.scale)[1], off = built - stated;
    if (Math.abs(off) > TIP_TOL) bad2.push(`${s.id}: ${placed.kit.id} built to ${f1(built)} m, ${s.regen?.tip === undefined ? `its pair ${s.regen!.same}'s ` : ''}regen.tip ${stated} (${off > 0 ? '+' : ''}${f1(off)} m)`);
  }
  return [
    rule('K-1', G, 'Every kit ref is an adopted piece that fits its slot (landmark disc and top, roof rect, kerb zone, gateway feet clear of quay edge, walk, pylons and beam); regenerated tower tips <= towers.crownTip', total, bad, 'all fit', 'brief 006 B; plan landmarks, roofs.kit, streetscape, towers.crownTip'),
    rule('K-2', G, `A regenerated tower stands to the tip its order states: the placed piece's top (podium deck + height x scale) is within ${TIP_TOL} m of regen.tip (a pair takes its partner's); a site whose piece is not adopted yet is not judged`, total2, bad2, `±${TIP_TOL} m`, 'standards.buildings.towers.sites[].regen.tip · tools/decokit (a piece is built to its order and never stretched; a height change is regenerated) · failing example: out/plan/variants/heights-cut.plan.json, avenue-needle cut to 26 storeys (regen.tip 153.4) with the 158.4 m Tower_Needle still placed (+14.4 m), and the avenue gates cut to 30 (154.8) with the DiscTowers (+10.8 m)'),
  ];
}

const pointInQuad = (p: V, q: V[], m: number): boolean => { // convex quad, any winding: no edge has p more than m outside
  const area = q.reduce((n, a, i) => n + (a[0] * q[(i + 1) % 4]![1] - q[(i + 1) % 4]![0] * a[1]), 0) > 0 ? 1 : -1;
  return q.every((a, i) => { const b = q[(i + 1) % 4]!; return area * ((b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0])) / Math.hypot(b[0] - a[0], b[1] - a[1]) >= -m; });
};
