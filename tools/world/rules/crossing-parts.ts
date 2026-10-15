// SF-20 for a crossing's parts (brief 009 B): the two light lines (dot rows or bars end to end, along the walk), signal orbs, kerb studs and inlay arcs a crossing carries, read from the export and held to the plan's
// standard (streetscape.crossings.strip: lights, signal, studs, arc), so a part that is missing, off the strip, on a refuge, on the lawn, laid across the walk like a zebra or not what the plan says fails here and not in a shot.
// gen/street/parts.ts places them; the rules read the export and share with it only the plan's numbers and the span, module and arc-site arithmetic.
import type { Crossing, StreetItem } from '../../../contracts/world/world.schema';
import { type V, dist, inPoly, lineDist, norm, sub } from '../../plan/geom';
import { TOL, type WCtx, itemRing, ringGap } from '../../plan/world-ctx';
import { type Ctx } from '../gen/common';
import { areaOf, clipConvex } from '../gen/street/crossings';
import { footprintOf } from '../../plan/street-zones';
import { discRing } from '../gen/box';
import { ROAD_Z, lawn, onRoad } from '../gen/street/ground';
import { ARC_SAMPLES, type Look, PART_KINDS, arcPoints, arcSites, bandGap, freeSpans, modulesIn, outerOf } from '../gen/street/parts';
import { roadYaw } from './roadyaw';
import { GLOW, INLAY, ORB } from '../gen/surfaces';

/** The part ids of a world's street items. */
export const partsOfWorld = (w: WCtx['w']): Map<string, StreetItem> => new Map(w.street.filter(s => (PART_KINDS as readonly string[]).includes(s.kind)).map(s => [s.id, s]));

const near = (a: number, b: number, tol = TOL) => Math.abs(a - b) <= tol;
const at2 = (a: V, b: V, tol = TOL) => dist(a, b) <= tol;
const turn = (a: number, b: number) => Math.abs(Math.atan2(Math.sin(a - b), Math.cos(a - b)));
/** What stands on the ground a halo lights, as footprints: every ground item but a crossing's own parts, bollards and beacons (they flank a landing) and a tram stop's kit. */
export function groundItems(c: WCtx): { id: string; at: V; ring: V[] }[] {
  const skip = new Set<string>(['bollard', 'beacon', 'crossing', ...PART_KINDS]);
  return c.w.street.filter(s => s.z < 1 && !skip.has(s.kind) && !s.edge.startsWith('stop/')).map(s => ({ id: s.id, at: s.at, ring: footprintOf(c.S, s.kind, s.variant, s.at, s.size, s.yaw) }));
}

/** The least angle (radians) between two axes (directions taken mod pi). */
const axisGap = (a: number, b: number) => { const d = Math.abs(a - b) % Math.PI; return Math.min(d, Math.PI - d); };
/** A light piece's long axis keeps this far from the carriageway's axis (Aster 2026-10-14: the light runs along the walk, never as stripes across it). */
const MIN_OFF_ROAD = (30 * Math.PI) / 180;

/** The first thing wrong with a crossing's parts, or ''. `ground` = groundItems: a signal's halo lies on none of them. */
export function partProblem(c: WCtx, gc: Ctx, look: Look, x: Crossing, byId: Map<string, StreetItem>, ground: { id: string; at: V; ring: V[] }[] = [], taken: StreetItem[] = []): string {
  const mine = x.items.map(id => byId.get(id));
  if (mine.some(s => !s)) return 'names a street item that is not one of its parts';
  const ps = mine as StreetItem[], of = (k: string) => ps.filter(s => s.kind === k), W = x.width;
  if (ps.some(s => s.crossing !== x.id)) return 'has a part that names another crossing';
  const [a, b] = x.line, len = dist(a, b), u: V = norm(sub(b, a)), nrm: V = [-u[1], u[0]], level = x.kind === 'kerb' ? ROAD_Z : x.landings[0]!.z, ends = x.kind === 'kerb' ? x.landings : [];
  const along = (p: V) => (p[0] - a[0]) * u[0] + (p[1] - a[1]) * u[1], across = (p: V) => (p[0] - a[0]) * nrm[0] + (p[1] - a[1]) * nrm[1];
  if (turn(x.walk, Math.atan2(u[1], u[0])) > 0.001) return 'has a walk yaw that is not the way from its first kerb to its second';
  for (const s of ps) if (s.castShadows !== (s.kind === 'signal-orb')) return `${s.id} ${s.castShadows ? 'casts a shadow, and only the orb\'s body does' : 'does not cast a shadow, and the orb\'s body does'}`;

  // the lights: the plan's form only, in two lines along the walk, on the strip, off the refuges, short of a kerb or a refuge, no piece across the walk, whole modules end to end
  const bars = of('crossing-bar'), rows = of('crossing-dots'), lights = look.form === 'bars' ? bars : rows, other = look.form === 'bars' ? rows : bars;
  if (other.length) return `has ${other.length} ${look.form === 'bars' ? 'dot rows' : 'light bars'}, the plan's lights form is ${look.form}`;
  const mod = look.form === 'bars' ? look.bars : look.dots, { inset, end, thick } = look.lines, offLine = W / 2 - inset - mod.width / 2, axis = x.landings[0] ? roadYaw(c, x.landings[0]) : null;
  const edge = [...x.strip, x.strip[0]!], inStrip = (p: V) => inPoly(p, x.strip) || lineDist(p, edge) <= 0.02, spans = freeSpans(a, u, len, x.refuges);
  const wantMods = spans.flatMap(([s0, s1]) => (s1 - end > s0 + end ? modulesIn(s1 - s0 - 2 * end, mod.modules) : [])).sort().join(); // a strip too short for any module (a 3 m bank way and 4 m bars) has no light
  if (!lights.length && wantMods) return `has no ${look.form === 'bars' ? 'light bar' : 'dot row'} along it`;
  for (const s of lights) {
    const ring = itemRing(s), ss = ring.map(along), lat = across(s.at), long = s.size[0] >= s.size[1] ? s.yaw : s.yaw + Math.PI / 2, why = ((): string => {
      if (axis !== null && axisGap(long, axis) < MIN_OFF_ROAD - 0.001) return `lies ${((axisGap(long, axis) * 180) / Math.PI).toFixed(0)} degrees from the carriageway's axis: a light piece runs along the walk, 30 degrees or more off the road`;
      if (axisGap(long, Math.atan2(u[1], u[0])) > 0.01) return 'does not lie along the walk';
      if (ring.some(p => !inStrip(p))) return 'reaches off the strip';
      if (x.refuges.some(r => areaOf(clipConvex(ring, r)) > 0.01)) return 'lies over a refuge';
      if (!near(s.z, level)) return `is at z ${s.z} over a surface at ${level}`;
      if (s.surface !== GLOW) return `draws ${s.surface}, not ${GLOW}`;
      if (!mod.modules.some(m => near(s.size[0], m)) || !near(s.size[1], mod.width) || !near(s.size[2], thick)) return `is ${s.size[0]} × ${s.size[1]} m, the plan's ${look.form === 'bars' ? 'bar' : 'dot row'} modules are ${mod.modules.join(' or ')} × ${mod.width}`;
      if (Math.abs(Math.abs(lat) - offLine) > TOL) return `lies ${lat.toFixed(2)} m off the middle of the strip, the plan's lines are at ±${offLine.toFixed(2)}`;
      if (!spans.some(([s0, s1]) => Math.min(...ss) >= s0 + end - TOL && Math.max(...ss) <= s1 - end + TOL)) return `is within ${end} m of a kerb or a refuge`;
      if (s.kind === 'crossing-dots') { const d = s.dots; return d && near(d.pitch, look.dots.pitch) && near(d.d, look.dots.d) && d.n === Math.round(s.size[0] / look.dots.pitch) ? '' : 'is not a row of the plan\'s dots'; }
      return '';
    })();
    if (why) return `${s.id} ${why}`;
  }
  // each line: as many whole modules as fit in each free span, end to end (none overlapping), and the same in both lines
  const lines = [1, -1].map(sg => lights.filter(s => Math.sign(across(s.at)) === sg));
  if (lights.length && lines.some(l => !l.length)) return `has lights in ${lines.filter(l => l.length).length} lines, the plan's are 2, one near each edge of the strip`;
  for (const l of lights.length ? lines : []) {
    const sorted = [...l].sort((p, q) => along(p.at) - along(q.at));
    for (let i = 1; i < sorted.length; i++) if (along(sorted[i]!.at) - sorted[i]!.size[0] / 2 < along(sorted[i - 1]!.at) + sorted[i - 1]!.size[0] / 2 - TOL) return `${sorted[i]!.id} overlaps ${sorted[i - 1]!.id}`;
    const have = l.map(s => s.size[0]).sort().join();
    if (wantMods !== have) return `has a light line of [${have}] m, the strip and the plan's modules call for [${wantMods}] (as many whole modules as fit between the kerbs and refuges)`;
  }

  // the brass edge line: two lines, one on each long edge of the strip, centred on it, along the walk, the strip's whole length in whole modules end to end, thin, trim
  const edges = of('crossing-edge'), ed = look.edge, elo = ed.end, wantEdge = (len - 2 * elo > 0 ? modulesIn(len - 2 * elo, ed.modules) : []).sort().join();
  if (!edges.length && wantEdge) return 'has no brass edge line along its long edges';
  for (const s of edges) {
    const lat = across(s.at), s0 = along(s.at) - s.size[0] / 2, s1 = along(s.at) + s.size[0] / 2, why = ((): string => {
      if (axisGap(s.yaw, Math.atan2(u[1], u[0])) > 0.01) return 'does not lie along the walk';
      if (!near(s.z, level)) return `is at z ${s.z} over a surface at ${level}`;
      if (s.surface !== 'trim') return `draws ${s.surface}, not trim`;
      if (!ed.modules.some(m => near(s.size[0], m)) || !near(s.size[1], ed.width) || !near(s.size[2], thick)) return `is ${s.size[0]} × ${s.size[1]} m, the plan's edge line modules are ${ed.modules.join(' or ')} × ${ed.width}`;
      if (Math.abs(Math.abs(lat) - W / 2) > TOL) return `lies ${lat.toFixed(2)} m off the middle of the strip, the plan's edge lines are at ±${(W / 2).toFixed(2)}`;
      if (s0 < elo - TOL || s1 > len - elo + TOL) return 'runs past a kerb';
      return '';
    })();
    if (why) return `${s.id} ${why}`;
  }
  for (const sg of lights.length || edges.length ? [1, -1] : []) {
    const l = edges.filter(s => Math.sign(across(s.at)) === sg).sort((p, q) => along(p.at) - along(q.at));
    for (let i = 1; i < l.length; i++) if (along(l[i]!.at) - l[i]!.size[0] / 2 < along(l[i - 1]!.at) + l[i - 1]!.size[0] / 2 - TOL) return `${l[i]!.id} overlaps ${l[i - 1]!.id}`;
    const have = l.map(s => s.size[0]).sort().join();
    if (wantEdge !== have) return `has an edge line of [${have}] m on one side, the strip and the plan's modules call for [${wantEdge}]`;
  }
  // the light lines clear the edge line: the row's outer edge stays inside the line's inner edge (Vela: hairline 1.965, line 1.98)
  for (const s of lights) if (Math.abs(across(s.at)) + s.size[1] / 2 > W / 2 - ed.width / 2 + TOL) return `${s.id} reaches the brass edge line`;

  // the lit strip (Sky's d1): where the plan gives one, the glow across the whole strip under the lights, in whole modules end to end in each free span, centred on the walk, flat on the surface, light and not paving
  const fields = of('crossing-field'), fl = look.field;
  if (!fl && fields.length) return `has ${fields.length} lit strip pieces, the plan gives none`;
  if (fl) {
    const wantF = spans.flatMap(([s0, s1]) => (s1 - fl.end > s0 + fl.end ? modulesIn(s1 - s0 - 2 * fl.end, fl.modules) : [])).sort().join();
    if (!fields.length && wantF) return 'has no lit strip under its lights';
    for (const s of fields) {
      const ring = itemRing(s), ss = ring.map(along), lat = across(s.at), why = ((): string => {
        if (axisGap(s.yaw, Math.atan2(u[1], u[0])) > 0.01) return 'does not lie along the walk';
        if (ring.some(p => !inStrip(p))) return 'reaches off the strip';
        if (x.refuges.some(r => areaOf(clipConvex(ring, r)) > 0.01)) return 'lies over a refuge';
        if (!near(s.z, level)) return `is at z ${s.z} over a surface at ${level}`;
        if (s.surface !== GLOW) return `draws ${s.surface}, not ${GLOW}`;
        if (!fl.modules.some(m => near(s.size[0], m)) || !near(s.size[1], fl.width) || !near(s.size[2], fl.thick)) return `is ${s.size[0]} × ${s.size[1]} m, the plan's lit strip modules are ${fl.modules.join(' or ')} × ${fl.width}`;
        if (Math.abs(lat) > TOL) return `lies ${lat.toFixed(2)} m off the middle of the strip`;
        if (!spans.some(([s0, s1]) => Math.min(...ss) >= s0 + fl.end - TOL && Math.max(...ss) <= s1 - fl.end + TOL)) return `is within ${fl.end} m of a kerb or a refuge`;
        return '';
      })();
      if (why) return `${s.id} ${why}`;
    }
    const sorted = [...fields].sort((p, q) => along(p.at) - along(q.at));
    for (let i = 1; i < sorted.length; i++) if (along(sorted[i]!.at) - sorted[i]!.size[0] / 2 < along(sorted[i - 1]!.at) + sorted[i - 1]!.size[0] / 2 - TOL) return `${sorted[i]!.id} overlaps ${sorted[i - 1]!.id}`;
    const haveF = fields.map(s => s.size[0]).sort().join();
    if (wantF !== haveF) return `has lit strip modules [${haveF}] m, the strip and the plan's modules call for [${wantF}] (as many whole modules as fit between the kerbs and refuges)`;
  }

  // the orbs: one over each kerb end and each refuge where the plan gives the road a signal, none where it does not
  const orbs = of('signal-orb'), want = x.control === 'signal' ? [...ends.map((_, i) => 'ab'[i]!), ...x.refuges.map((_, i) => `r${i}`)] : [], have = orbs.map(s => s.id.slice(`${x.id}/signal/`.length));
  if ([...have].sort().join() !== [...want].sort().join()) return `has signal orbs [${have}], its kerb ends and refuges call for [${want}]`;
  const sg = look.signal;
  for (const s of orbs) {
    const k = s.id.slice(`${x.id}/signal/`.length), end = 'ab'.indexOf(k), sig = s.signal, why = ((): string => {
      if (!sig) return 'has no signal state';
      const own = end >= 0 ? itemRing(ends[end]!) : x.refuges[Number(k.slice(1))]!;
      if (!inPoly(s.at, own)) return end >= 0 ? 'hangs off its landing' : 'hangs off its refuge';
      if (sig.state !== sg.state) return `shows ${sig.state}, the plan's default is ${sg.state}`;
      if (!near(sig.hover, sg.hover) || !near(sig.size, sg.size)) return `hovers ${sig.hover} m (an orb ${sig.size} m across), the plan's hover is ${sg.hover} (${sg.size})`;
      if (!at2(sig.halo.at, s.at) || !near(sig.halo.r, sg.halo) || !near(s.z, sig.halo.z)) return 'has its halo off the orb\'s own ground or not the plan\'s size';
      const disc = discRing(sig.halo.at, sig.halo.r), on = ground.find(g => dist(g.at, s.at) < 6 && ringGap(g.ring, disc) < 0.001);
      if (on) return `has its halo on ${on.id}`;
      const under = end >= 0 ? ends[end]!.z : ((m) => (m ? m.base + m.height : NaN))(c.w.ground.medians.find(m => inPoly(s.at, m.ring))); // the surface under it: the landing's, or the median's top
      if (!near(sig.halo.z, under)) return `has its halo at z ${sig.halo.z} over a surface at ${under}`;
      if (!near(s.size[0], 2 * sg.halo) || !near(s.size[1], 2 * sg.halo) || !near(s.size[2], sg.top) || s.surface !== ORB) return `is not the plan's ${sg.size} m ${ORB} (finial ${sg.top} m up)`;
      if (turn(sig.faces[0], s.yaw) > 0.001 || turn(sig.faces[1], s.yaw + Math.PI) > 0.001) return 'has faces that are not its yaw and the way back';
      const way = end >= 0 ? norm(sub(x.line[1 - end]!, x.line[end]!)) : u, face: V = [Math.cos(sig.faces[0]), Math.sin(sig.faces[0])];
      return (end >= 0 ? face[0] * way[0] + face[1] * way[1] < 0.99 : Math.abs(face[0] * way[0] + face[1] * way[1]) < 0.99) ? 'does not face along the walk' : '';
    })();
    if (why) return `${s.id} ${why}`;
  }

  // the studs and arcs: on each kerb end, the plan's count of studs on the kerb edge, the outer two at the corners of the strip; two arcs, one round each corner of the mouth, wherever the band lies on paving
  const studs = of('crossing-stud'), arcs = of('crossing-arc'), st = look.studs, ar = look.arc, R = outerOf(ar);
  if (studs.length !== st.count * ends.length) return `has ${studs.length} studs, ${ends.length} kerb ends call for ${st.count * ends.length}`;
  for (const [i, e] of ends.entries()) {
    const k = x.line[i]!, inward = norm(sub(e.at, k)), t: V = [-inward[1], inward[0]];
    for (let j = 0; j < st.count; j++) {
      const o = (st.count > 1 ? j / (st.count - 1) - 0.5 : 0) * (W - st.d), want: V = [k[0] + t[0] * o + inward[0] * st.off, k[1] + t[1] * o + inward[1] * st.off], s = studs.find(q => q.id === `${x.id}/stud/${'ab'[i]}${j}`);
      if (!s) return `has no stud ${'ab'[i]}${j}`;
      if (!at2(s.at, want, 0.02)) return `${s.id} is not on the kerb edge of its landing (${dist(s.at, want).toFixed(2)} m off)`;
      if (!near(s.z, e.z) || s.size.some((v, q) => !near(v, [st.d, st.d, st.h][q]!)) || s.surface !== 'trim') return `${s.id} is not the plan's brass stud`;
      if (!inPoly(s.at, itemRing(e)) && lineDist(s.at, [...itemRing(e), itemRing(e)[0]!]) > 0.02) return `${s.id} stands off its landing`;
      if (lawn(gc, s.at)) return `${s.id} stands on the lawn`;
    }
    for (const [j, site] of arcSites({ landing: e, k, inward }, W, R).entries()) {
      const id = `${x.id}/arc/${'ab'[i]}${j}`, arc = arcs.find(q => q.id === id), fits = arcPoints(site.c, ar.r, site.yaw, ar.sweep, 12).every(p => !lawn(gc, p) && !onRoad(gc, p));
      const under = taken.some(q => q.crossing !== x.id && q.arc && dist(q.at, site.c) <= q.arc.outer + R && bandGap(arcPoints(q.at, q.arc.r, q.yaw, q.arc.sweep, ARC_SAMPLES), arcPoints(site.c, ar.r, site.yaw, ar.sweep, ARC_SAMPLES)) < ar.w - TOL); // another crossing's arc already rounds this corner
      if (!arc && fits && !under) return `has no arc ${'ab'[i]}${j} round the corner of its mouth, where its band lies on paving`;
      if (arc && !fits) return `${id} runs over lawn or carriageway`;
      if (!arc) continue;
      if (!arc.arc || !at2(arc.at, site.c, 0.02) || !near(arc.z, e.z) || arc.surface !== INLAY || turn(arc.yaw, site.yaw) > 0.001) return `${id} is not round the corner of its mouth, in ${INLAY}, facing the corner`;
      if (!near(arc.arc.r, ar.r) || !near(arc.arc.w, ar.w) || !near(arc.arc.sweep, ar.sweep, 0.001) || !near(arc.arc.outer, R) || arc.size.some((v, q) => !near(v, [R, R, thick][q]!))) return `${id} is not the plan's quarter arc (r ${ar.r}, w ${ar.w}, outer ${R})`;
    }
  }
  if (arcs.some(q => !ends.some((_, i) => q.id.startsWith(`${x.id}/arc/${'ab'[i]}`)))) return 'has an arc that is at no kerb end';
  return '';
}

/** Arcs of different crossings never overlap (two bands closer than their width). */
export function arcOverlaps(arcs: StreetItem[]): string[] {
  const out: string[] = [], pts = arcs.map(s => (s.arc ? arcPoints(s.at, s.arc.r, s.yaw, s.arc.sweep, ARC_SAMPLES) : []));
  for (const [i, s] of arcs.entries()) for (let j = i + 1; j < arcs.length; j++) {
    const t = arcs[j]!;
    if (s.crossing === t.crossing || dist(s.at, t.at) > s.arc!.outer + t.arc!.outer) continue;
    const w = Math.max(s.arc!.w, t.arc!.w), d = bandGap(pts[i]!, pts[j]!);
    if (d < w - TOL) out.push(`${s.id} and ${t.id} overlap (${d.toFixed(2)} m between their bands)`);
  }
  return out;
}
