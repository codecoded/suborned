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
import { FLAT, boxRing, footprintOf, quayBlock, ringGap, roadAt, walkBlock } from './street-zones';
import { footOf } from '../world/civic/foot';
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
    const cs = footOf(k.a, r.kit), ux: V = [Math.cos(r.yaw), Math.sin(r.yaw)];
    const off = cs.map(q => [(q[0] - r.c[0]) * ux[0] + (q[1] - r.c[1]) * ux[1], -(q[0] - r.c[0]) * ux[1] + (q[1] - r.c[1]) * ux[0]] as V);
    if (off.some(o => Math.abs(o[0]) > r.size[0] / 2 + TOL || Math.abs(o[1]) > r.size[1] / 2 + TOL)) bad.push(`${r.id}: ${k.a.id} ${k.s.map(f1).join('×')} m outside the roof ${r.size.map(f1).join('×')}`);
  }
  // street: inside the item's slot
  const g = genCtx(plan, d, c.civ);
  for (const s of w.street) if (s.kit) {
    const k = piece(s.id, s.kit), slot = slotOf(g, s); if (!k) continue;
    if (PART_USE[s.kind]) { // a crossing's part: its slot is its own size, and the piece is the one of its use, unscaled
      if (!PART_USE[s.kind]!.some(u => k.a.tags.use.includes(u)) || s.kit.scale.some(v => v !== 1) || !fitsPart(k.a.size, s)) bad.push(`${s.id}: ${k.a.id} ${k.a.size.map(f1).join('×')} m is not the ${PART_USE[s.kind]!.join(' / ')} piece of the part's ${s.size.map(f1).join('×')} m`);
      continue;
    }
    if (!slot) { bad.push(`${s.id}: a ${s.kind} has no slot`); continue; }
    if ((slot.along !== undefined && k.s[0] > slot.along + TOL) || (slot.depth !== undefined && k.s[2] > slot.depth + TOL) || (slot.maxH !== undefined && k.s[1] > slot.maxH + TOL)) bad.push(`${s.id}: ${k.a.id} ${k.s.map(f1).join('×')} m over slot ${slot.along ?? '-'}×${slot.depth ?? '-'}×${slot.maxH ?? '-'}`);
  }
  // gateways (megastructures[].gateway on the pier quay, landmarks[].gateway on a pavement): the plan's feet inside the piece's footprint and in the gateway's `zones`
  // (default quay); clear of the pylons (3 m), the beam envelope and the walks. Quay feet: on the quay, 1.5 m off its edge (SF-14), off the walks the megastructure spans.
  // Frontage feet: in the frontage zone of their street (kerb + walk <= past the kerb face <= pavement), never a walk zone (SF-1) or a plan walk; and the keep-clear list:
  // no doorway, and no standing street item (spill, vending, palm pit, lamp...), within 1.5 m of a plinth.
  const sky = d.sky.map(p => p.samples.map(s => s.p as V)), pylonHalf = (S.skyrail?.pylonSize ?? c.plan.standards.skyrail.pylonSize ?? 1.6) / 2, beamEnv = plan.standards.skyrail.beamCentres / 2 + 0.75;
  type Gw = { kit: string; feet: R4[]; zones?: string[] };
  const gates: { id: string; gw: Gw; ref?: KitRef & { at: V }; spans: string[] }[] = [
    ...(plan.megastructures ?? []).filter(m => (m as any).gateway).map(m => ({ id: m.id, gw: (m as any).gateway as Gw, ref: w.megastructures.find(x => x.id === `mega/${m.id}`)?.gateway, spans: m.spans.foot ?? [] })),
    ...plan.landmarks.filter(l => l.gateway).map(l => ({ id: l.id, gw: l.gateway as Gw, ref: w.landmarks.find(x => x.id === `landmark/${l.id}`)?.gateway, spans: plan.foot.map(f => f.id) })),
  ];
  const KEEP = 1.5, standing = w.street.filter(s => !FLAT.has(s.kind) && s.z < 3), doors = w.frontages.filter(f => f.level === 'street').map(f => ({ id: f.id, ring: boxRing(f.entrance.c, f.entrance.size[0], f.entrance.size[1], f.entrance.yaw) }));
  for (const { id: gid, gw, ref, spans } of gates) {
    if (!ref) { total++; bad.push(`${gid}: gateway ${gw.kit} not emitted`); continue; }
    const k = piece(`${gid}/gateway`, ref); if (!k) continue;
    const box = corners(k.s, ref.at, ref.yaw ?? 0), zones = gw.zones ?? ['quay'];
    gw.feet.forEach((f, i) => {
      const id = `${gid}/gateway/foot-${i}`, pts = rectPts(f), ring: V[] = [[f[0], f[2]], [f[1], f[2]], [f[1], f[3]], [f[0], f[3]]];
      if (rectPts(f, 6.4).some(q => !pointInQuad(q, box, TOL))) bad.push(`${id}: outside the piece's footprint`);
      const onQuay = pts.every(p => plan.quay.some(r => inPoly(p, r))), inFront = pts.every(p => { const r = roadAt(g, p, ''); return !!r && r.dk >= r.sec.kerb + r.sec.walk - TOL && r.dk <= r.sec.pavement + TOL; });
      if (!(zones.includes('quay') && onQuay) && !(zones.includes('frontage') && inFront)) bad.push(`${id}: not in its zone (${zones.join(' / ')})`);
      else if (zones.includes('quay') && onQuay && quayBlock(c, pts)) bad.push(`${id}: within 1.5 m of the quay edge`);
      for (const fp of plan.foot) { const e = Math.min(...pts.map(p => lineDist(p, fp.pts))) - fp.w / 2; if (e < -TOL && spans.includes(fp.id)) bad.push(`${id}: on the ${fp.id} walk`); }
      if (zones.includes('frontage')) { const wb = walkBlock(g, pts, ''); if (wb) bad.push(`${id}: in ${wb}`); }
      for (const p of d.pylons) { const e = Math.min(...pts.map(q2 => Math.hypot(q2[0] - p.p[0], q2[1] - p.p[1]))) - pylonHalf; if (e < 3 - TOL) bad.push(`${id}: ${f1(e)} m from pylon ${f1(p.p[0])},${f1(p.p[1])} (3)`); }
      const bd = Math.min(...sky.flatMap(l => pts.map(p => lineDist(p, l)))); if (bd < beamEnv - TOL) bad.push(`${id}: ${f1(bd)} m from the beam (envelope ${beamEnv})`);
      if (!zones.includes('frontage')) return;
      const near = (r: V[]) => ringGap(r, ring) < KEEP - TOL, cx = (f[0] + f[1]) / 2, cy = (f[2] + f[3]) / 2, R = Math.hypot(f[1] - f[0], f[3] - f[2]) / 2 + KEEP + 10;
      for (const s of standing) if (Math.hypot(s.at[0] - cx, s.at[1] - cy) < R && near(footprintOf(S, s.kind, s.variant, s.at, s.size, s.yaw))) bad.push(`${id}: ${s.id} (${s.kind}) within ${KEEP} m of the plinth`);
      for (const dr of doors) if (Math.hypot(dr.ring[0]![0] - cx, dr.ring[0]![1] - cy) < R && near(dr.ring)) bad.push(`${id}: ${dr.id}'s doorway within ${KEEP} m of the plinth`);
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
    rule('K-1', G, 'Every kit ref is an adopted piece that fits its slot (landmark disc and top, roof rect, kerb zone, gateway feet inside the piece, in their zone (quay or frontage), clear of quay edge, walk, pylons and beam, no doorway or standing item within 1.5 m of a frontage plinth); regenerated tower tips <= towers.crownTip', total, bad, 'all fit', 'brief 006 B; brief 075 vista (landmarks[].gateway); plan landmarks, roofs.kit, streetscape, towers.crownTip · failing examples: kit-fit.test.ts (a skyport gateway foot on the beam, a Crown Avenue foot moved onto the walk, a lamp against a plinth)'),
    rule('K-2', G, `A regenerated tower stands to the tip its order states: the placed piece's top (podium deck + height x scale) is within ${TIP_TOL} m of regen.tip (a pair takes its partner's); a site whose piece is not adopted yet is not judged`, total2, bad2, `±${TIP_TOL} m`, 'standards.buildings.towers.sites[].regen.tip · tools/decokit (a piece is built to its order and never stretched; a height change is regenerated) · failing example: out/plan/variants/heights-cut.plan.json, avenue-needle cut to 26 storeys (regen.tip 153.4) with the 158.4 m Tower_Needle still placed (+14.4 m), and the avenue gates cut to 30 (154.8) with the DiscTowers (+10.8 m)'),
  ];
}

const pointInQuad = (p: V, q: V[], m: number): boolean => { // convex quad, any winding: no edge has p more than m outside
  const area = q.reduce((n, a, i) => n + (a[0] * q[(i + 1) % 4]![1] - q[(i + 1) % 4]![0] * a[1]), 0) > 0 ? 1 : -1;
  return q.every((a, i) => { const b = q[(i + 1) % 4]!; return area * ((b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0])) / Math.hypot(b[0] - a[0], b[1] - a[1]) >= -m; });
};
