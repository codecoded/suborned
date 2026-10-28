// Brief 075 district roll-out, step 2 (the sky track to the old reel's profile; Vela's pieces, tools/decokit/skyrail075.py, "Roll-out: sky track" in briefs/075/notes.md).
// The old town's sky rail (~/Projects/the-suborned/game/Assets/TheSuborned/Editor/DecoKit/DecoKitSkyRail.cs, read only) was one swept streamline beam (1.2 x 1.6 m,
// fillet 0.22, a proud teal stripe each flank at z -1.05..-0.85, its top the rail path) on kit pylons every 30 m (PylonStep 30): SkyRail_Pylon_Tall (the 12 m
// plinth) where built, else SkyRail_Pylon_Foot, a SkyRail_Pylon_Shaft stretched from the foot's top to the head's bottom, and SkyRail_Pylon_Head hanging under the
// beam (top = beam underside, 2.2 m deep), the head turned along the track. Ours, by rule over plan data only:
//   pylons   where the plan derives them (tools/plan/plan.ts placePylons: standards.skyrail spanStraight 30 = the old PylonStep, spanCurve / spanTight on curves,
//            pylonZones, pylonClear), never moved here: the plan is the source and the Handover's site reads it. Each takes the old stack: the Tall plinth where its
//            footprint (4.46 across x 5.26 along) stands inside the pylon's own allowed ground (standards.skyrail.pylonZones) and clear of the walks (SF-1), else the
//            Foot (2.86 square) under the same test, else no plinth: the bare Shaft (1.1 x 1.5, inside the plan's 1.6 m post) runs to the ground, a gap logged where
//            the plan's post itself stands in a walk (the Skyport Pier: Datum's); the Shaft stretched between foot and head; the head: SkyRail_Pylon_HeadTwin
//            (Vela: the old cradle into an 8 m cross-arm, pads at +-beamCentres / 2) on a double path, SkyRail_Pylon_Head on a single one. Ground: the surface under
//            the pylon (the circus island, a median, a plot slab, the mall's foot level, a quay top, the water).
//   beam     sweep data for Latch (Unity sweeps the section along each beam line; SkyRail_Beam_4m carries the section and the materials, metric UVs, so a swept beam
//            and a tile take one material with no seam): per beam its centre line (the path's 2 m polyline, a double path's two lines +-beamCentres / 2), its top z
//            (standards.skyrail.height) and the section numbers. Not tiles: a 4 m straight laid round the loop's R 40 would open 6 cm gaps on the outside of every joint.
// Deterministic: no random choice. The coral ribbons and square posts (transit.skyRail line / pylons / width / depth) stay in the export for a builder that does not
// read `track` (Latch's to retire once the swept beam lands).
import type { KitRef, SkyRail, World } from '../../../../contracts/world/world.schema';
import { type V, dist, inPoly } from '../../../plan/geom';
import { type Derived, type Plan, inZone } from '../../../plan/plan';
import { r3, r4, rv } from '../common';
import { adoptedBy } from '../kitrefs';
import { discRing } from '../box';
import { ringGap } from '../../../plan/street-zones';

/** The old beam section (DecoKitSkyRail.cs BeamW .6 half width, BeamH 1.6, Section(.22); Vela's SkyRail_Beam_4m): m. */
export const BEAM = { piece: 'SkyRail_Beam_4m', width: 1.2, depth: 1.6, fillet: 0.22, stripe: [-1.05, -0.85] as [number, number], stripeProud: 0.035, uPerMetre: 1 };
/** The old pylon stack (DecoKitSkyRail.cs Pylons): the tall plinth's height, the stock foot's, the head's depth (m), and the pieces. */
export const PYLON = { tall: 'SkyRail_Pylon_Tall', foot: 'SkyRail_Pylon_Foot', shaft: 'SkyRail_Pylon_Shaft', head: 'SkyRail_Pylon_Head', headTwin: 'SkyRail_Pylon_HeadTwin', tallH: 12, footH: 1.2, headH: 2.2 };

export type SkyPylon = { id: string; at: V; yaw: number; ground: number; on: string; foot?: KitRef & { at: V; z: number }; shaft: KitRef & { at: V; z: number }; head: KitRef & { at: V; z: number } };
export type SkyBeam = { id: string; line: V[]; top: number };
export type SkyTrack = { beam: typeof BEAM & { beams: SkyBeam[] }; pylons: SkyPylon[] };
export type SkyTrackLog = { pylons: number; tall: number; foot: number; bare: number; twin: number; single: number; beams: number; beamM: number; gaps: { rule: string; where: string; why: string; at?: V }[] };

/** The unit tangent of a polyline at its segment nearest p. */
function tangentAt(line: V[], p: V): V {
  let best = Infinity, t: V = [1, 0];
  for (let i = 0; i + 1 < line.length; i++) {
    const a = line[i]!, b = line[i + 1]!, L = dist(a, b); if (L < 1e-9) continue;
    const u: V = [(b[0] - a[0]) / L, (b[1] - a[1]) / L], s = Math.max(0, Math.min(L, (p[0] - a[0]) * u[0] + (p[1] - a[1]) * u[1])), q: V = [a[0] + u[0] * s, a[1] + u[1] * s], d = dist(p, q);
    if (d < best - 1e-9) { best = d; t = u; }
  }
  return t;
}
/** A polyline offset d to its left (vertex normals averaged over the two segments). */
export function offsetLine(line: V[], d: number): V[] {
  const n = line.length;
  return line.map((p, i) => {
    const a = line[Math.max(0, i - 1)]!, b = line[Math.min(n - 1, i + 1)]!, L = dist(a, b) || 1, nx = -(b[1] - a[1]) / L, ny = (b[0] - a[0]) / L;
    return rv([p[0] + nx * d, p[1] + ny * d]);
  });
}
/** The footprint of a piece of `across` x `along` m at p, its `along` side on the unit tangent t. */
export function footRing(p: V, t: V, across: number, along: number): V[] {
  const n: V = [-t[1], t[0]], a = along / 2, c = across / 2;
  return [[-1, -1], [1, -1], [1, 1], [-1, 1]].map(([x, y]) => [p[0] + t[0] * a * x! + n[0] * c * y!, p[1] + t[1] * a * x! + n[1] * c * y!] as V);
}

/** The surface under a pylon: the circus island, a quay top, the water, a median, a plot slab, the mall's foot level, else the carriageway (0). */
export function groundUnder(w: Pick<World, 'ground'>, p: V): { z: number; on: string } {
  const g = w.ground;
  for (const c of g.circuses) { const r = dist(p, c.centre as V); if (r <= c.islandRadius) return { z: c.level + c.islandHeight, on: c.id }; }
  for (const q of g.quays) if (inPoly(p, q.ring as V[])) return { z: r3(q.base + q.height), on: q.id };
  for (const m of g.medians) if (inPoly(p, m.ring as V[])) return { z: r3(m.base + m.height), on: m.id };
  for (const m of g.plots) if (inPoly(p, m.ring as V[])) return { z: r3(m.base + m.height), on: m.id };
  for (const f of g.foot) { const L = f.line as V[]; for (let i = 0; i + 1 < L.length; i++) { const a = L[i]!, b = L[i + 1]!, ab = dist(a, b) || 1, s = ((p[0] - a[0]) * (b[0] - a[0]) + (p[1] - a[1]) * (b[1] - a[1])) / ab; if (s >= 0 && s <= ab && Math.abs(((p[0] - a[0]) * (b[1] - a[1]) - (p[1] - a[1]) * (b[0] - a[0])) / ab) <= f.width / 2) return { z: f.level, on: f.id }; } }
  for (const x of g.water) if (inPoly(p, x.ring as V[])) return { z: x.level, on: x.id };
  return { z: 0, on: 'carriageway' };
}

/** The pylon stacks and the beam sweep data for every sky rail path. `walkClear(ring)` = '' when the ring stands clear of every walk zone (SF-1), else why. */
export function skyTrack(plan: Plan, d: Derived, w: Pick<World, 'ground'> & { transit: { skyRail: SkyRail[] } }, walkClear: (ring: V[]) => string): { track: SkyTrack; log: SkyTrackLog } | undefined {
  const tall = adoptedBy(PYLON.tall), foot = adoptedBy(PYLON.foot), shaft = adoptedBy(PYLON.shaft), head = adoptedBy(PYLON.head), twin = adoptedBy(PYLON.headTwin), beam = adoptedBy(BEAM.piece);
  if (!foot || !shaft || !head || !beam) return undefined; // the old stack is not in the catalogue: the export keeps the ribbons and posts only
  const S = plan.standards.skyrail, kinds = new Set(S.pylonZones as string[]), zones = d.zones.filter(z => kinds.has(z.kind));
  const allowed = (q: V) => zones.some(z => inZone(q, z));
  const discs = (plan.landmarks ?? []).map(l => discRing(l.c as V, l.r));
  const log: SkyTrackLog = { pylons: 0, tall: 0, foot: 0, bare: 0, twin: 0, single: 0, beams: 0, beamM: 0, gaps: [] };
  const beams: SkyBeam[] = [], pylons: SkyPylon[] = [];
  const ref = (a: { id: string }, at: V, yaw: number, z: number, sy = 1): KitRef & { at: V; z: number } => ({ id: a.id, scale: [1, r4(sy), 1], yaw: r4(yaw), at: rv(at), z: r3(z) });
  for (const r of w.transit.skyRail) {
    const line = r.line as V[], double = r.spacing > 0, top = r.height, soffit = r3(r.height - BEAM.depth);
    for (const [k, dd] of (double ? [r.spacing / 2, -r.spacing / 2] : [0]).entries()) {
      const L = dd ? offsetLine(line, dd) : line.map(rv);
      beams.push({ id: `${r.id}/beam-${k}`, line: L, top });
      log.beams++; log.beamM = r3(log.beamM + L.slice(1).reduce((s, p, i) => s + dist(L[i]!, p), 0));
    }
    const hd = double ? twin ?? head : head;
    if (double && !twin) log.gaps.push({ rule: 'SK-T1', where: r.id, why: `${PYLON.headTwin} is not adopted: the single head stands under a double path` });
    (r.pylons as V[]).forEach((p, i) => {
      const t = tangentAt(line, p), yaw = Math.atan2(t[1], t[0]) - Math.PI / 2; // kit +Z (the pieces' along-track axis) on the tangent
      const g = groundUnder(w, p), fits = (across: number, along: number) => { const ring = footRing(p, t, across, along); return ring.every(allowed) && !walkClear(ring) && !discs.some(r => ringGap(ring, r) < 0.01); }; // a plinth keeps off a landmark and its steps (SK-T2, SF-18): a plan before pylonClear stood pylons on the monument
      const base = tall && fits(tall.size[0]!, tall.size[2]!) ? tall : fits(foot.size[0]!, foot.size[2]!) ? foot : undefined;
      // neither plinth fits: the bare shaft (1.1 x 1.5, inside the plan's own 1.6 m post) runs down to the ground, as the plan's post stood; logged for the plan's owner where that post itself stands in a walk
      if (!base) { const post = footRing(p, t, r.pylonSize, r.pylonSize), why = walkClear(post); log.bare++; if (why) log.gaps.push({ rule: 'SK-T2', where: `${r.id}/pylon-${i}`, why: `the plan's pylon at ${p.map(Math.round)} stands in ${why}: a bare shaft there (Datum: move the pylon or let the beam span it)`, at: p }); }
      const baseTop = g.z + (base === tall ? PYLON.tallH : base === foot ? PYLON.footH : 0), headBottom = soffit - PYLON.headH;
      if (headBottom - baseTop < 0.5) log.gaps.push({ rule: 'SK-T2', where: `${r.id}/pylon-${i}`, why: `the shaft would be ${r3(headBottom - baseTop)} m`, at: p });
      pylons.push({ id: `${r.id}/pylon-${i}`, at: rv(p), yaw: r4(yaw), ground: r3(g.z), on: g.on, ...(base ? { foot: ref(base, p, yaw, g.z) } : {}), shaft: ref(shaft, p, yaw, baseTop, Math.max(0.01, headBottom - baseTop)), head: ref(hd, p, yaw, soffit) });
      log.pylons++; if (base === tall) log.tall++; else if (base === foot) log.foot++; if (hd === twin && double) log.twin++; else log.single++;
    });
  }
  return { track: { beam: { ...BEAM, beams }, pylons }, log };
}
