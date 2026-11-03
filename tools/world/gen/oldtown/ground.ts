// Brief 075 remap (Aster, Sky 2026-10-28: "remap the world around the Handover using old buildings ... And old props"): the old town's ground, generated
// from the data Datum carried into the plan (plan.oldtown.gardens, plan.oldtown.harbour; tools/plan/oldtown/transcribe.ts) by the old builders' rules
// (~/Projects/the-suborned/game/Assets/TheSuborned/Editor/DecoKit/DecoKitGardens.cs and DecoKitHarbour.cs, read only). Nothing is placed by hand: every
// shape is the old rule over plan data (the canal, basin and pavilion Datum stored, the garden plots, the quay, the harbour's terrace, steps and promenade
// lines), so the same plan gives the same ground. A plan without `oldtown` (a pinned plan) gets none of it.
//
// What is ported (old file · rule → ours):
//   OG-L  lawns       (Gardens: the lawn is a bed inside its paths; Sky 2026-10-28 "no hard tiled edge") every park plot's lawn stands inside a stone border
//                     LAWN.border m wide with corners rounded at LAWN.radius: the slab is the border (`bed-kerb`), the lawn a flat on it.
//   OG-G1 gate        (Gardens: terrazzo "gate promenade" Pool.y ± 6 across the gardens) a terrazzo band over the gate mall, the canal garden's width.
//   OG-G2 basin       (Gardens: raised round basin r 12, granite coping 0.6, rim 0.45 over the walk, water 0.1 under the rim, a terrazzo ring 5 m round it)
//   OG-G3 canal       (Gardens: canal x ± 3 from 6 m inside the garden's far edge to 4 m short of the basin's coping, coped as the basin; terrazzo walks
//                     0.6..5 m off both sides) clipped to the garden plot the canal runs in (Datum's y extent reaches the old west lane, which the remap gave to buildings).
//   OG-H1 terrace     (Harbour: "an upper terrace of flags in front of the blocks' sea walls") flags on the quay from its landward edge to the steps line.
//   OG-H2 steps       (Harbour: five 0.2 m risers, 1.2 m treads, terrace 0.15 → promenade -0.85, DK_Limestone_Tex) the steps band in limestone, laid flat: the plan's quay is one slab at
//                     the pavement top and the promenade walk and both piers stand on it, so the flight's drop is a plan level (Datum's): logged as a gap, not forced.
//   OG-H3 promenade   (Harbour: the quay promenade in flags the whole coast) flags from the steps' foot to the coping.
//   OG-H4 coping      (Harbour: granite quay wall 0.6 m wide, 0.05 proud of the promenade) along the water edge, broken at the pier roots.
//   OG-C  circus      (DecoKitCircus / StreetLayout.Paved: the walks run round the circus to the crescents) the pavement ring between the circus carriageway and the
//                     crescents' line (plan circus.ring[1]..pavement) is a slab at the pavement top, cut where a road or a kerb return enters: it was bare land
//                     under the circus's palms and lamps (they stood 0.21 m over it).
import type { Flat, Mass } from '../../../../contracts/world/world.schema';
import { type V, inPoly, lineDist } from '../../../plan/geom';
import { type Ctx, r3, rv } from '../common';
import { areaOf, bbox } from '../geo';
import { fillRing, returnsOf, slabRing } from '../kerbreturn';

/** The old numbers each rule reads beside the plan's (DecoKitGardens / DecoKitHarbour constants). */
export const OLD_GROUND = {
  lawn: { border: 1.2, radius: 4, surface: 'bed-kerb' },
  gate: { half: 6, surface: 'terrazzo' },
  basin: { ring: 5, waterDrop: 0.1, surface: 'granite', path: 'terrazzo' },
  canal: { walk: [0.6, 5] as [number, number], tail: 6, short: 4, walkPast: 0 },
  harbour: { coping: 0.6, proud: 0.05, risers: 5, surface: { terrace: 'flags', steps: 'limestone', promenade: 'flags', coping: 'granite' } },
};

/** m above the pavement top each flat lies at, so none z-fights (the capstone pieces' 1-3 mm lesson). */
export const LAYERS = { border: 0.003, lawn: 0.006, path: 0.009, ring: 0.012 };

export type OldGroundLog = { lawns: string[]; gardens: string[]; harbour: string[]; gaps: { rule: string; why: string }[] };
export type OldGround = {
  flats: Flat[]; // paths, terraces, promenade, lawn borders and lawns
  raised: Mass[]; // basin and canal coping, the basin's terrazzo ring, the quay coping (ground.quays: drawn as solids, read by nothing else)
  water: Flat[]; // the basin's and the canal's water (ground.water: walkers keep off it)
  keep: { id: string; ring: V[] }[]; // where nothing stands (water and coping): the props keep off it and evict what stood there
  gardens?: { canal: { x: number; half: number; y0: number; y1: number }; pool: { c: V; r: number; coping: number; water: number }; pavilion: { at: V; yaw: number }; plots: string[]; x: [number, number] };
  harbour?: { terrace: [number, number]; steps: [number, number]; promenade: [number, number]; quay: number; piers: [number, number][]; z: number };
  log: OldGroundLog;
};

/** Half annulus (r0..r1) from compass-free plan angle a0 through a0 + π, as one simple ring. */
function halfRing(c: V, r0: number, r1: number, a0: number, n = 24): V[] {
  const out: V[] = [];
  for (let k = 0; k <= n; k++) { const a = a0 + (Math.PI * k) / n; out.push([c[0] + r1 * Math.cos(a), c[1] + r1 * Math.sin(a)]); }
  for (let k = n; k >= 0; k--) { const a = a0 + (Math.PI * k) / n; out.push([c[0] + r0 * Math.cos(a), c[1] + r0 * Math.sin(a)]); }
  return out.map(rv);
}
const disc = (c: V, r: number, n = 48): V[] => Array.from({ length: n }, (_, k) => rv([c[0] + r * Math.cos((2 * Math.PI * k) / n), c[1] + r * Math.sin((2 * Math.PI * k) / n)]));
const rect = (x0: number, x1: number, y0: number, y1: number): V[] => [[x0, y0], [x1, y0], [x1, y1], [x0, y1]].map(p => rv(p as V));
/** A rectangle with its corners rounded at r (counter-clockwise). */
export function roundedRect(x0: number, x1: number, y0: number, y1: number, r: number, n = 6): V[] {
  const R = Math.max(0, Math.min(r, (x1 - x0) / 2, (y1 - y0) / 2)), out: V[] = [];
  const corner = (cx: number, cy: number, a0: number) => { for (let k = 0; k <= n; k++) { const a = a0 + (Math.PI / 2) * (k / n); out.push([cx + R * Math.cos(a), cy + R * Math.sin(a)]); } };
  corner(x1 - R, y0 + R, -Math.PI / 2); corner(x1 - R, y1 - R, 0); corner(x0 + R, y1 - R, Math.PI / 2); corner(x0 + R, y0 + R, Math.PI);
  return out.map(rv);
}
/** Sutherland-Hodgman: the part of `poly` with x (axis 0) or y (axis 1) between lo and hi. */
export function clipAxis(poly: V[], axis: 0 | 1, lo: number, hi: number): V[] {
  const cut = (pts: V[], keep: (p: V) => boolean, at: (a: V, b: V) => V) => {
    const out: V[] = [];
    pts.forEach((b, i) => { const a = pts[(i + pts.length - 1) % pts.length]!; if (keep(b)) { if (!keep(a)) out.push(at(a, b)); out.push(b); } else if (keep(a)) out.push(at(a, b)); });
    return out;
  };
  const lerp = (v: number) => (a: V, b: V): V => { const t = (v - a[axis]) / (b[axis] - a[axis]); return [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t]; };
  return cut(cut(poly, p => p[axis] >= lo, lerp(lo)), p => p[axis] <= hi, lerp(hi));
}

/** Is the plot a plain axis-aligned rectangle (four corners on its bounding box)? */
const isRect = (poly: V[]) => { const b = bbox(poly); return poly.length === 4 && poly.every(p => (Math.abs(p[0] - b[0]) < 1e-6 || Math.abs(p[0] - b[1]) < 1e-6) && (Math.abs(p[1] - b[2]) < 1e-6 || Math.abs(p[1] - b[3]) < 1e-6)); };

export function oldGround(ctx: Ctx): OldGround | undefined {
  const ot = (ctx.plan as { oldtown?: any }).oldtown;
  if (!ot) return undefined;
  const pav = ctx.pav, out: OldGround = { flats: [], raised: [], water: [], keep: [], log: { lawns: [], gardens: [], harbour: [], gaps: [] } }, L = OLD_GROUND;
  const flat = (id: string, surface: string, ring: V[], level: number) => { if (ring.length >= 3 && Math.abs(areaOf(ring)) > 0.05) out.flats.push({ id, surface, ring: ring.map(rv), level: r3(level) }); };
  const solid = (id: string, surface: string, ring: V[], base: number, top: number) => out.raised.push({ id, surface, ring: ring.map(rv), base: r3(base), height: r3(top - base) });

  // OG-L: every park plot (never civic) is a bed: the slab its stone border, the lawn a rounded flat inside it
  for (const p of ctx.d.plots) {
    if (p.use !== 'park' || (p as { civic?: unknown }).civic) continue;
    if (!isRect(p.polyV)) { out.log.gaps.push({ rule: 'OG-L', why: `${p.id}: not a rectangle, its lawn keeps the slab's edge` }); continue; }
    const b = bbox(p.polyV), e = L.lawn.border;
    flat(`oldtown/lawn/${p.id}/border`, L.lawn.surface, slabRing(ctx, p), pav + LAYERS.border);
    flat(`oldtown/lawn/${p.id}`, p.use, roundedRect(b[0] + e, b[1] - e, b[2] + e, b[3] - e, L.lawn.radius), pav + LAYERS.lawn);
    out.log.lawns.push(p.id);
  }

  // OG-C: the circus pavement ring, sector by sector between the roads that enter it (each end follows the road's edge and its kerb return, radius by radius)
  {
    const cc = ctx.plan.circus, r0 = cc.ring[1], r1 = cc.pavement, c = cc.c;
    const roads = ctx.d.roads.filter(r => !r.gyratory).map(r => ({ line: r.line as V[], half: r.w / 2 }));
    const fills = returnsOf(ctx).filter(r => r.convex && !r.nose).map(r => fillRing(r) as V[]);
    const at = (r: number, a: number): V => [c[0] + r * Math.cos(a), c[1] + r * Math.sin(a)];
    const free = (p: V) => !roads.some(r => lineDist(p, r.line) < r.half + 0.02) && !fills.some(f => inPoly(p, f));
    const N = 1440, radii = Array.from({ length: 13 }, (_, k) => r0 + ((r1 - r0) * k) / 12), ok = Array.from({ length: N }, (_, i) => radii.every(r => free(at(r, (2 * Math.PI * i) / N))));
    if (ok.every(Boolean)) { solid('oldtown/circus/pavement-0', 'pavement', halfRing(c, r0, r1, 0, 96), 0, pav); solid('oldtown/circus/pavement-1', 'pavement', halfRing(c, r0, r1, Math.PI, 96), 0, pav); }
    else {
      const start = ok.findIndex((v, i) => v && !ok[(i + N - 1) % N]); let n = 0;
      for (let k = 0; k < N; k++) {
        const i0 = (start + k) % N; if (!ok[i0] || ok[(i0 + N - 1) % N]) continue;
        let i1 = i0; while (ok[(i1 + 1) % N] && (i1 + 1) % N !== i0) i1 = (i1 + 1) % N;
        const a0 = (2 * Math.PI * i0) / N, a1 = (2 * Math.PI * (i1 < i0 ? i1 + N : i1)) / N;
        // each end: at every radius, step outward from the sector's last fully free angle while that radius stays free (the road edge is not radial)
        const edge = (a: number, dir: 1 | -1) => radii.map(r => { let b = a; for (let s = 1; s <= 40; s++) { const t = a + (dir * s * Math.PI) / N; if (!free(at(r, t))) break; b = t; } return at(r, b); });
        const lo = edge(a0, -1), hi = edge(a1, 1), steps = Math.max(2, Math.ceil(((a1 - a0) * 180) / Math.PI));
        const outer = Array.from({ length: steps + 1 }, (_, j) => at(r1, a0 + ((a1 - a0) * j) / steps)).slice(1, -1), inner = Array.from({ length: steps + 1 }, (_, j) => at(r0, a0 + ((a1 - a0) * j) / steps)).slice(1, -1);
        const ring = [...lo, ...outer, ...[...hi].reverse(), ...[...inner].reverse()];
        if (a1 - a0 > 0.02) solid(`oldtown/circus/pavement-${n++}`, 'pavement', ring, 0, pav);
        k += (i1 - i0 + N) % N;
      }
    }
  }

  // Orbit Gardens (DecoKitGardens.Build)
  const G = ot.gardens as { canal: { x: number; half: number; y: [number, number] }; pool: { c: V; r: number; coping: number; rim: number }; pavilion: { at: V; yaw: number } } | undefined;
  if (G) {
    const P = G.pool, C = G.canal, top = pav + P.rim, water = top - L.basin.waterDrop;
    const parks = ctx.d.plots.filter(p => p.use === 'park' && !(p as { civic?: unknown }).civic);
    const canalPlot = parks.find(p => inPoly([C.x, (C.y[0] + C.y[1]) / 2], p.polyV) || inPoly([C.x, C.y[1] - 1], p.polyV));
    const near = parks.filter(p => (canalPlot ? p.area === canalPlot.area : true) && (() => { const b = bbox(p.polyV); return b[0] < P.c[0] + P.r + 20 && b[1] > P.c[0] - P.r - 20 && b[2] < P.c[1] + P.r + 20 && b[3] > P.c[1] - P.r - 20; })()); // the gardens' own lawns (the canal plot's area), as the old GardensX x GardensZ
    if (!canalPlot) out.log.gaps.push({ rule: 'OG-G3', why: `no park plot holds the canal at x ${C.x}` });
    // OG-G3: the canal runs from 6 m inside its garden's far edge (the old canal stopped 6 m short of the west lane's path) to 4 m short of the basin's coping
    const cb = canalPlot ? bbox(canalPlot.polyV) : undefined, y1 = P.c[1] - P.r - P.coping - L.canal.short;
    const y0 = cb ? Math.max(C.y[0], cb[2] + L.canal.tail) : C.y[0];
    if (cb && C.y[0] < cb[2] + L.canal.tail) out.log.gaps.push({ rule: 'OG-G3', why: `the canal's south end (plan y ${C.y[0]}, old ${ot.gardens.canal.oldY?.[0] ?? '?'}) runs past ${canalPlot!.id}'s edge (y ${cb[2]}) onto ground the remap gave to buildings: it starts at y ${r3(y0)}` });
    const gx: [number, number] = cb ? [cb[0], cb[1]] : [P.c[0] - 32, P.c[0] + 24];
    // OG-G1: the gate promenade across the gardens, on the gate mall
    const gate = ctx.plan.foot.find(f => f.pts.some(q => Math.abs(q[1] - P.c[1]) < 1) && f.pts.every(q => Math.abs(q[1] - P.c[1]) < 1));
    if (gate) flat('oldtown/gardens/gate', L.gate.surface, rect(gx[0], gx[1], P.c[1] - L.gate.half, P.c[1] + L.gate.half), 0.02 + 0.004);
    else out.log.gaps.push({ rule: 'OG-G1', why: 'no gate walk on the basin\'s axis' });
    // OG-G2: the basin's terrazzo ring and coping (two half rings each: a simple ring per solid), its water
    for (const [k, a0] of [[0, 0], [1, Math.PI]] as const) {
      solid(`oldtown/gardens/basin/ring-${k}`, L.basin.path, halfRing(P.c, P.r + P.coping, P.r + L.basin.ring, a0), 0, pav + LAYERS.ring);
      solid(`oldtown/gardens/basin/coping-${k}`, L.basin.surface, halfRing(P.c, P.r, P.r + P.coping, a0), 0, top);
    }
    out.water.push({ id: 'oldtown/gardens/basin/water', surface: 'water', ring: disc(P.c, P.r), level: r3(water) });
    out.keep.push({ id: 'oldtown/gardens/basin', ring: disc(P.c, P.r + P.coping) });
    // OG-G3: the canal, coped all round, and its walks
    if (y1 - y0 > 4) {
      const x0 = C.x - C.half, x1 = C.x + C.half, k = P.coping;
      solid('oldtown/gardens/canal/coping-w', L.basin.surface, rect(x0 - k, x0, y0 - k, y1 + k), pav, top);
      solid('oldtown/gardens/canal/coping-e', L.basin.surface, rect(x1, x1 + k, y0 - k, y1 + k), pav, top);
      solid('oldtown/gardens/canal/coping-s', L.basin.surface, rect(x0, x1, y0 - k, y0), pav, top);
      solid('oldtown/gardens/canal/coping-n', L.basin.surface, rect(x0, x1, y1, y1 + k), pav, top);
      out.water.push({ id: 'oldtown/gardens/canal/water', surface: 'water', ring: rect(x0, x1, y0, y1), level: r3(water) });
      out.keep.push({ id: 'oldtown/gardens/canal', ring: rect(x0 - k, x1 + k, y0 - k, y1 + k) });
      const [w0, w1] = L.canal.walk, wy1 = Math.min(y1 + L.canal.short, cb ? cb[3] : Infinity) + L.canal.walkPast; // the old walks ran 4 m past the canal's end (canal1 + 4) to meet the basin's ring
      flat('oldtown/gardens/canal/walk-w', L.basin.path, rect(x0 - w1, x0 - w0, y0, Math.min(wy1, cb ? cb[3] : wy1)), pav + LAYERS.path);
      flat('oldtown/gardens/canal/walk-e', L.basin.path, rect(x1 + w0, x1 + w1, y0, Math.min(wy1, cb ? cb[3] : wy1)), pav + LAYERS.path);
      out.log.gardens.push(`canal x ${C.x} ± ${C.half}, y ${r3(y0)}..${r3(y1)} on ${canalPlot?.id ?? '?'}`);
    } else out.log.gaps.push({ rule: 'OG-G3', why: `no room for the canal between y ${r3(y0)} and ${r3(y1)}` });
    out.log.gardens.push(`basin r ${P.r} at ${P.c.join(', ')} (coping ${P.coping}, rim ${P.rim}), gate ${gate?.id ?? 'none'} over x ${gx.join('..')}`);
    out.gardens = { canal: { x: C.x, half: C.half, y0: r3(y0), y1: r3(y1) }, pool: { c: P.c, r: P.r, coping: P.coping, water: r3(water) }, pavilion: G.pavilion, plots: near.map(p => p.id), x: gx };
  }

  // The harbour (DecoKitHarbour.Build): terrace, steps, promenade and coping on the coast quay
  const H = ot.harbour as { terrace: [number, number]; steps: [number, number]; promenade: [number, number]; quay: number; levels: { terrace: number; promenade: number; water: number }; piers: Record<string, [number, number]> } | undefined;
  const coast = ctx.plan.quay.find(q => q.some(p => Math.abs(p[0] - (H?.quay ?? NaN)) < 1e-6) && bbox(q as V[])[3] - bbox(q as V[])[2] > 100) as V[] | undefined;
  if (H && coast) {
    const S = L.harbour.surface, qb = bbox(coast), lip = H.quay - L.harbour.coping, z = pav + LAYERS.border;
    flat('oldtown/harbour/terrace', S.terrace, clipAxis(coast, 0, qb[0], H.terrace[1]), z);
    flat('oldtown/harbour/steps', S.steps, clipAxis(coast, 0, H.steps[0], H.steps[1]), z);
    flat('oldtown/harbour/promenade', S.promenade, clipAxis(coast, 0, H.promenade[0], lip), z);
    const piers = Object.values(H.piers).map(s => [Math.min(...s), Math.max(...s)] as [number, number]).sort((a, b) => a[0] - b[0]);
    let y = qb[2], n = 0;
    for (const [a, b] of [...piers, [qb[3], qb[3]] as [number, number]]) { if (a - y > 1) solid(`oldtown/harbour/coping-${n++}`, S.coping, rect(lip, H.quay, y, Math.min(a, qb[3])), -2.5, pav + L.harbour.proud); y = Math.max(y, b); }
    out.keep.push(...out.raised.filter(m => m.id.startsWith('oldtown/harbour/coping-')).map(m => ({ id: m.id, ring: m.ring })));
    out.log.gaps.push({ rule: 'OG-H2', why: `the steps (${L.harbour.risers} risers, terrace ${H.levels.terrace} → promenade ${H.levels.promenade}) are laid flat: the plan's quay is one slab at ${pav} and the harbour promenade walk and both piers stand on it; the drop is a plan level (Datum)` });
    out.log.harbour.push(`terrace x ${r3(qb[0])}..${H.terrace[1]}, steps ${H.steps.join('..')}, promenade ${H.promenade[0]}..${r3(lip)}, coping ${r3(lip)}..${H.quay} in ${n} runs (piers ${piers.map(p => p.join('..')).join(', ')})`);
    out.harbour = { terrace: [qb[0], H.terrace[1]], steps: H.steps, promenade: [H.promenade[0], lip], quay: H.quay, piers, z: r3(pav) };
  } else if (H) out.log.gaps.push({ rule: 'OG-H1', why: `no coast quay on x ${H.quay}` });
  return out;
}
