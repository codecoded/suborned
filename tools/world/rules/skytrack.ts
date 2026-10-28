// SK-T1..SK-T3, the old sky track (brief 075 district roll-out, step 2; tools/world/gen/street/skytrack.ts): what the generator places is what is judged.
//   SK-T1 one old stack per plan pylon: every transit.skyRail pylon has a stack at its own point (no pylon moved, none added), the head's top at the beam's underside
//         (height - depth), the shaft from the foot's top to the head's bottom (scale[1] = that height, > 0.5 m), the twin head on a double path, the single on a single.
//   SK-T2 a pylon's foot (the Tall plinth or the Foot, its kit footprint turned along the track) stands on the plan's pylon ground (standards.skyrail.pylonZones),
//         out of every walk zone (SF-1), and clear of steps, landmarks, bays, drives, buildings and stops (SF-18's things, 2 m from crossings and doorways) and of
//         every standing street item (trees, lamps, furniture).
//   SK-T3 the beams clear the buildings: no building mass, megastructure part or kit landmark rises into a beam's swept box (the section's width with its stripes,
//         from its underside to its top) except the line's own stations (transit.stations) and the portals and stations it is planned through (signatures kind
//         station / portal, megastructures the plan names on the line).
// Failing examples: tools/world/rules/skytrack.test.ts.
import type { Check } from '../../plan/checks';
import type { V } from '../../plan/geom';
import { inZone } from '../../plan/plan';
import { FLAT, ringGap, walkBlock } from '../../plan/street-zones';
import { TOL, type WCtx, itemRing, rule } from '../../plan/world-ctx';
import { ctxOf as genCtx } from '../gen/common';
import { BEAM, PYLON, footRing } from '../gen/street/skytrack';
import { adoptedBy } from '../gen/kitrefs';
import { groundThings } from './pylons';

const G = 'Sky rail';
const SOURCE = 'brief 075 roll-out (the sky track to the old reel\'s profile; old DecoKitSkyRail.cs Pylons / Beam) · standards.skyrail (height, beamCentres, pylonZones) · SF-1 · SF-18 · failing examples: tools/world/rules/skytrack.test.ts';
const key = (p: V) => `${p[0].toFixed(3)},${p[1].toFixed(3)}`;

/** A beam line's swept box per segment (plan ring), the section's width with its stripes. */
function beamBoxes(line: V[], half: number): V[][] {
  const out: V[][] = [];
  for (let i = 0; i + 1 < line.length; i++) {
    const a = line[i]!, b = line[i + 1]!, L = Math.hypot(b[0] - a[0], b[1] - a[1]); if (L < 1e-6) continue;
    const t: V = [(b[0] - a[0]) / L, (b[1] - a[1]) / L], n: V = [-t[1] * half, t[0] * half];
    out.push([[a[0] + n[0], a[1] + n[1]], [b[0] + n[0], b[1] + n[1]], [b[0] - n[0], b[1] - n[1]], [a[0] - n[0], a[1] - n[1]]]);
  }
  return out;
}
const bb = (r: V[]) => { let x0 = Infinity, x1 = -Infinity, y0 = Infinity, y1 = -Infinity; for (const p of r) { x0 = Math.min(x0, p[0]); x1 = Math.max(x1, p[0]); y0 = Math.min(y0, p[1]); y1 = Math.max(y1, p[1]); } return [x0, x1, y0, y1] as const; };

export function skyTrackRules(c: WCtx): Check[] {
  const st = c.w.transit.skyTrack; if (!st) return [];
  const out: Check[] = [];
  // ---- SK-T1
  const bad1: string[] = [], at = new Map(st.pylons.map(p => [key(p.at), p]));
  let n1 = 0;
  for (const r of c.w.transit.skyRail) for (const p of r.pylons as V[]) {
    n1++;
    const s = at.get(key(p)); if (!s) { bad1.push(`${r.id} pylon ${key(p)} has no stack`); continue; }
    const soffit = r.height - r.depth, headTop = s.head.z, shaftTop = s.shaft.z + s.shaft.scale[1], headBottom = soffit - PYLON.headH;
    const baseH = !s.foot ? 0 : s.foot.id === PYLON.tall ? PYLON.tallH : PYLON.footH, baseZ = s.foot?.z ?? s.ground;
    if (Math.abs(headTop - soffit) > TOL) bad1.push(`${s.id}: head top ${headTop} is not the beam underside ${soffit}`);
    if (Math.abs(s.shaft.z - (baseZ + baseH)) > TOL || Math.abs(shaftTop - headBottom) > TOL) bad1.push(`${s.id}: the shaft (${s.shaft.z} to ${shaftTop.toFixed(3)}) does not join the ${s.foot ? 'foot' : 'ground'} (${(baseZ + baseH).toFixed(3)}) to the head (${headBottom.toFixed(3)})`);
    if (s.shaft.scale[1] < 0.5) bad1.push(`${s.id}: the shaft is ${s.shaft.scale[1]} m`);
    const want = r.spacing > 0 && adoptedBy(PYLON.headTwin) ? PYLON.headTwin : PYLON.head;
    if (s.head.id !== want) bad1.push(`${s.id}: head ${s.head.id} on a ${r.spacing > 0 ? 'double' : 'single'} path (want ${want})`);
  }
  const extra = st.pylons.filter(p => !c.w.transit.skyRail.some(r => (r.pylons as V[]).some(q => key(q) === key(p.at))));
  for (const p of extra) bad1.push(`${p.id} at ${key(p.at)} is no plan pylon`);
  out.push(rule('SK-T1', G, 'One old pylon stack per plan pylon (none moved or added): the head\'s top at the beam\'s underside, the shaft from the foot\'s top to the head\'s bottom, the twin head on a double path', n1, bad1, 'all', SOURCE));
  // ---- SK-T2
  const gc = genCtx(c.plan, c.d, c.civ, 0.18), kinds = new Set(c.plan.standards.skyrail.pylonZones as string[]), zones = c.d.zones.filter(z => kinds.has(z.kind));
  const railOf = (id: string) => c.w.transit.skyRail.find(r => id.startsWith(r.id + '/'))!;
  const things = groundThings(c.w), items = c.w.street.filter(s => s.z < 1 && !FLAT.has(s.kind) && s.size[2] > 0.3), bad2: string[] = [];
  for (const s of st.pylons) {
    if (!s.foot) { const sh = adoptedBy(s.shaft.id); if (sh && (sh.size[0]! > railOf(s.id).pylonSize + TOL || sh.size[2]! > railOf(s.id).pylonSize + TOL)) bad2.push(`${s.id}: the bare shaft ${sh.size[0]} x ${sh.size[2]} is wider than the plan's ${railOf(s.id).pylonSize} m post`); continue; } // a bare shaft stands inside the plan's own post, which SF-18 judges
    const a = adoptedBy(s.foot.id); if (!a) { bad2.push(`${s.id}: foot ${s.foot.id} is not adopted`); continue; }
    const t: V = [Math.cos(s.yaw + Math.PI / 2), Math.sin(s.yaw + Math.PI / 2)], ring = footRing(s.at, t, a.size[0]!, a.size[2]!);
    const mids = ring.map((p, i) => { const q = ring[(i + 1) % 4]!; return [(p[0] + q[0]) / 2, (p[1] + q[1]) / 2] as V; });
    const off = [...ring, ...mids].find(p => !zones.some(z => inZone(p, z)));
    if (off) { bad2.push(`${s.id} (${s.foot.id}): its foot leaves the pylon ground at ${key(off)}`); continue; }
    const walk = walkBlock(gc, [...ring, ...mids], 'skyrail'); if (walk) { bad2.push(`${s.id} (${s.foot.id}): its foot stands in ${walk}`); continue; }
    const th = things.find(x => ringGap(ring, x.ring) < (x.clear > 0 ? x.clear - TOL : TOL)); if (th) { bad2.push(`${s.id} (${s.foot.id}): its foot stands on or by ${th.what}`); continue; }
    const it = items.find(x => ringGap(ring, itemRing(x)) < TOL); if (it) bad2.push(`${s.id} (${s.foot.id}): its foot stands on the ${it.kind} ${it.id}`);
  }
  out.push(rule('SK-T2', G, 'A sky rail pylon\'s foot (Tall plinth or Foot) stands on pylon ground, out of the walks, clear of SF-18\'s things and of every standing street item; a pylon with no plinth is a bare shaft inside the plan\'s post', st.pylons.length, bad2, 'none', SOURCE));
  // ---- SK-T3
  const half = BEAM.width / 2 + BEAM.stripeProud, through = new Set<string>([...c.w.transit.stations.map(m => m.id)]);
  const portals = c.w.signatures.filter(x => x.kind === 'station' || x.kind === 'portal').map(x => x.id.split('/').slice(0, 2).join('/'));
  const solids = [...c.w.buildings.map(m => ({ id: m.id, ring: m.ring as V[], lo: m.base, hi: m.base + m.height })), ...c.w.megastructures.flatMap(m => m.parts.map(p => ({ id: p.id, ring: p.ring as V[], lo: p.base, hi: p.base + p.height })))]
    .filter(s => !through.has(s.id) && !portals.some(p => s.id.startsWith(p + '/') || s.id === p));
  const bad3: string[] = []; let n3 = 0;
  for (const b of st.beam.beams) {
    const lo = b.top - BEAM.depth, boxes = beamBoxes(b.line as V[], half); n3 += boxes.length;
    const hit = new Set<string>();
    for (const box of boxes) {
      const [x0, x1, y0, y1] = bb(box);
      for (const s of solids) {
        if (s.hi <= lo + TOL || s.lo >= b.top - TOL || hit.has(s.id)) continue;
        const [a0, a1, c0, c1] = bb(s.ring); if (a1 < x0 || a0 > x1 || c1 < y0 || c0 > y1) continue;
        if (ringGap(box, s.ring) < TOL) { hit.add(s.id); bad3.push(`${b.id} runs through ${s.id} (${s.lo.toFixed(1)}-${s.hi.toFixed(1)} m against the beam's ${lo.toFixed(1)}-${b.top} m)`); }
      }
    }
  }
  out.push(rule('SK-T3', G, 'The sky rail beams clear every building mass and megastructure part (the line\'s own stations and the portals it is planned through excepted)', n3, bad3, 'none', SOURCE));
  return out;
}

