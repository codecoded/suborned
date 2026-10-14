// The parts of a crossing (brief 009 B; Sky 2026-10-14: no zebra, no paint, no landing slab; light set flush in the road, floating signal eyes). Everything is placed from the crossing the
// pairing made and the plan's streetscape.crossings.strip standard, never by hand, and every part is a street item that names its crossing, so Unity draws what is listed and the rules check it:
//   crossing-dots  one module of a light line (LightDotRow_2 / _1: a row of flush dots in a brass frame), laid end to end along the walk   crossing-bar  the same line made of LightBar_4 (the plan's other `form`)
//   signal-orb     the SignalEye over each kerb end and each refuge: chrome orb, a display on each face, a beam to a halo disc on the ground       crossing-stud  a brass dome stud on the kerb edge of a landing
//   crossing-arc   a quarter ring of seafoam inlay that rounds each corner of the crossing's mouth (CrossInlayArc: tangent to the kerb line and to the strip's edge line)
// The light runs ALONG the walk, kerb to kerb: two lines, one near each edge of the strip, never stripes across it (that is a zebra drawn in light; Aster 2026-10-14). The brass frame lines on the strip's long edges
// are owed by Vela (no piece yet), so none is emitted. A crossing lays no slab: its landings are keep-clear footprints (Crossing.landings), their paving is the street's.
import type { Crossing, Landing, StreetItem } from '../../../../contracts/world/world.schema';
import { type V, add, dist, mul, sub } from '../../../plan/geom';
import { r3, r4, rv } from '../common';
import { discRing } from '../box';
import { GLOW, INLAY, ORB } from '../surfaces';

/** Street item kinds that are parts of a crossing. */
export const PART_KINDS = ['crossing-bar', 'crossing-dots', 'crossing-stud', 'crossing-arc', 'signal-orb'] as const;
export type PartKind = (typeof PART_KINDS)[number];

/** The plan's look of a crossing (standards.streetscape.crossings.strip: lights, signal, studs, arc), `arc.sweep` in degrees there and radians here. */
export type Look = {
  form: 'bars' | 'dots';
  /** Both light lines: `inset` = strip edge to the line's outer edge; `end` = kerb or refuge edge to the line's end; `thick` = how far the piece stands proud. */
  lines: { inset: number; end: number; thick: number };
  /** The pieces' lengths, longest first, and their frame width: a line is whole modules end to end. */
  bars: { modules: number[]; width: number };
  dots: { modules: number[]; width: number; pitch: number; d: number };
  signal: { state: 'walk' | 'stop'; hover: number; size: number; halo: number; top: number };
  studs: { count: number; d: number; h: number; off: number };
  arc: { r: number; w: number; hair: number; sweep: number };
};
export function lookOf(strip: any): Look {
  for (const k of ['lights', 'signal', 'studs', 'arc']) if (!strip?.[k]) throw new Error(`streetscape.crossings.strip.${k} is missing: a crossing's parts are placed from it (brief 009)`);
  const form = strip.lights.form ?? 'dots', { inset, end, thick, bars, dots } = strip.lights;
  if (form !== 'bars' && form !== 'dots') throw new Error(`streetscape.crossings.strip.lights.form is '${form}', not bars or dots`);
  return { form, lines: { inset, end, thick }, bars, dots, signal: strip.signal, studs: strip.studs, arc: { ...strip.arc, sweep: (strip.arc.sweep * Math.PI) / 180 } };
}
/** The outer radius of an arc: the band's outer edge and the brass hairline. */
export const outerOf = (a: { r: number; w: number; hair: number }) => r4(a.r + a.w / 2 + a.hair);

/** A kerb end of a kerb crossing: its landing, the kerb point the strip starts from and the unit vector from the kerb into the pavement. */
export type End = { landing: Landing; k: V; inward: V };
/** What the site offers a part: `clear(ring, at)` = the ground under a halo is free of furniture; `pave(p)` = a point of an inlay arc lies on paving (not lawn, not carriageway);
 *  `take(pts, w)` = an arc band `w` wide along the centre line `pts` overlaps no other crossing's arc (and, if so, claims the ground). */
export type Site = { clear: (ring: V[], at: V) => boolean; pave: (p: V) => boolean; take: (pts: V[], w: number) => boolean };

const wrap = (t: number) => r4(Math.atan2(Math.sin(t), Math.cos(t)));
/** The ids of a crossing's parts: `<crossing>/<what>/<n>`, stable because the crossing id is. */
export const partId = (crossing: string, what: string, n: string | number) => `${crossing}/${what}/${n}`;

/** The spans of a way (metres from its start) that are off its refuges: the lights stop where a refuge begins. */
export function freeSpans(a: V, u: V, len: number, refuges: V[][]): [number, number][] {
  const taken = refuges.map((ring): [number, number] => { const s = ring.map(p => (p[0] - a[0]) * u[0] + (p[1] - a[1]) * u[1]); return [Math.min(...s), Math.max(...s)]; }).sort((x, y) => x[0] - y[0]);
  const out: [number, number][] = []; let at = 0;
  for (const [lo, hi] of taken) { if (lo > at) out.push([at, Math.min(lo, len)]); at = Math.max(at, hi); }
  if (at < len) out.push([at, len]);
  return out;
}

/** The whole modules, longest first, that fit in `room` metres: 5 m of [2, 1] is [2, 2, 1]. */
export function modulesIn(room: number, modules: number[]): number[] {
  const out: number[] = []; let left = room + 1e-9;
  for (const m of [...modules].sort((p, q) => q - p)) while (left >= m) { out.push(m); left -= m; }
  return out;
}

/** Points the overlap test samples along an arc's centre line, here and in SF-20. */
export const ARC_SAMPLES = 24;
/** The points of an arc's centre line (centre `c`, radius `r`, `n` + 1 points over `sweep` radians about `yaw`). */
export const arcPoints = (c: V, r: number, yaw: number, sweep: number, n = 12): V[] => Array.from({ length: n + 1 }, (_, i) => { const t = yaw - sweep / 2 + (sweep * i) / n; return [c[0] + r * Math.cos(t), c[1] + r * Math.sin(t)] as V; });

/** The least distance between two arc centre lines (their sample points). */
export const bandGap = (a: V[], b: V[]): number => Math.min(...a.flatMap(p => b.map(q => dist(p, q))));

/** Where the two arcs of a kerb end stand: for each side of the mouth, the circle's centre and the heading from it toward the mouth corner. The circle (outer radius R) is tangent to the kerb line and to the strip's edge line,
 *  so its centre is R along the kerb past the strip's edge and R into the pavement. */
export function arcSites(e: End, width: number, outer: number): { c: V; yaw: number }[] {
  const t: V = [-e.inward[1], e.inward[0]];
  return [1, -1].map(sg => {
    const corner = add(e.k, mul(t, (sg * width) / 2)), c = add(add(corner, mul(t, sg * outer)), mul(e.inward, outer));
    return { c, yaw: wrap(Math.atan2(corner[1] - c[1], corner[0] - c[0])) };
  });
}

/** Metres a halo keeps clear of any furniture standing on the ground it lights. */
const HALO_CLEAR = 0.1;
/** Where along a landing's width an orb may hang, in metres from the middle, nearest first. */
const SHIFTS = [0, 1, -1];

/** The parts of one crossing, in drawing order: the two light lines, then (a kerb crossing) orbs, studs, arcs. `ends` = a kerb crossing's two kerb ends (none for a stop or bank crossing); `refugeEdges[i]` = the id of the median refuge i stands on;
 *  `level` = the surface the lights lie on. Orbs only where the crossing has a control (signal). The orb moves along its landing, or round its refuge, to the nearest spot where `site.clear` says the ground under its halo is free of furniture: skip, never squeeze;
 *  an orb with no such spot, or an arc that would run over lawn or carriageway, is left out and named in `skipped`. */
export function partsOf(c: Pick<Crossing, 'id' | 'line' | 'strip' | 'width' | 'refuges' | 'control'>, look: Look, level: number, ends: End[], refugeEdges: string[], refugeLevel: number, site: Site = { clear: () => true, pave: () => true, take: () => true }, skipped: string[] = []): StreetItem[] {
  const [a, b] = c.line, len = dist(a, b), u: V = mul(sub(b, a), 1 / len), nrm: V = [-u[1], u[0]], way = Math.atan2(u[1], u[0]);
  const item = (id: string, kind: PartKind, at: V, yaw: number, z: number, size: [number, number, number], surface: string, edge: string, extra: Partial<StreetItem> = {}): StreetItem => ({
    id, kind, at: rv(at), yaw: wrap(yaw), z: r3(z), size: [r3(size[0]), r3(size[1]), r3(size[2])], surface, edge, sight: 'open', crossing: c.id, castShadows: false, ...extra,
  });
  const out: StreetItem[] = [];

  // two light lines along the walk, kerb to kerb, one near each edge of the strip: whole modules end to end, centred in each free span, stopping short of a kerb or a refuge
  const bars = look.form === 'bars', mod = bars ? look.bars : look.dots, { inset, end, thick } = look.lines, off = c.width / 2 - inset - mod.width / 2, what = bars ? 'bar' : 'dots';
  const spans = freeSpans(a, u, len, c.refuges);
  [1, -1].forEach((sg, line) => {
    let i = 0;
    for (const [s0, s1] of spans) {
      const lo = s0 + end, hi = s1 - end, ms = hi > lo ? modulesIn(hi - lo, mod.modules) : [];
      let s = lo + (hi - lo - ms.reduce((t, m) => t + m, 0)) / 2;
      for (const m of ms) {
        const at = add(add(a, mul(u, s + m / 2)), mul(nrm, sg * off)), id = partId(c.id, what, `${'lr'[line]}${i++}`);
        out.push(bars ? item(id, 'crossing-bar', at, way, level, [m, mod.width, thick], GLOW, c.id)
          : item(id, 'crossing-dots', at, way, level, [m, mod.width, thick], GLOW, c.id, { dots: { n: Math.round(m / look.dots.pitch), pitch: look.dots.pitch, d: look.dots.d } }));
        s += m;
      }
    }
  });

  // studs along the kerb edge of each landing, the outer two at the corners of the strip
  const { count, d, h, off: sOff } = look.studs, studs: StreetItem[] = [];
  ends.forEach((e, i) => {
    const t: V = [-e.inward[1], e.inward[0]];
    for (let j = 0; j < count; j++) studs.push(item(partId(c.id, 'stud', `${'ab'[i]}${j}`), 'crossing-stud', add(add(e.k, mul(t, (count > 1 ? j / (count - 1) - 0.5 : 0) * (c.width - d))), mul(e.inward, sOff)), way, e.landing.z, [d, d, h], 'trim', e.landing.edge));
  });

  // a signal orb over each kerb end, its halo on the landing; one over each refuge, its halo on the island
  const { hover, size, halo, state, top } = look.signal, foot = 2 * halo;
  const orb = (name: string, at: V, yaw: number, ground: number, edge: string) => item(partId(c.id, 'signal', name), 'signal-orb', at, yaw, ground, [foot, foot, top], ORB, edge,
    { castShadows: true, signal: { state, hover, size, faces: [wrap(yaw), wrap(yaw + Math.PI)], halo: { at: rv(at), z: r3(ground), r: halo } } });
  const spot = (cands: V[]) => cands.find(p => site.clear(discRing(p, halo + HALO_CLEAR), p));
  const signals: StreetItem[] = [];
  if (c.control === 'signal') {
    ends.forEach((e, i) => {
      const t: V = [-e.inward[1], e.inward[0]], p = spot(SHIFTS.map(o => add(e.landing.at, mul(t, o))));
      if (p) signals.push(orb('ab'[i]!, p, way + (i ? Math.PI : 0), e.landing.z, e.landing.edge)); // display A faces the way across, toward the far kerb
      else skipped.push(`${c.id}/signal/${'ab'[i]}: no clear ground for its halo on the landing`);
    });
    c.refuges.forEach((ring, i) => {
      const s = ring.map(q => (q[0] - a[0]) * u[0] + (q[1] - a[1]) * u[1]), mid = (Math.min(...s) + Math.max(...s)) / 2, along = Math.max(0, (Math.max(...s) - Math.min(...s)) / 2 - halo - HALO_CLEAR), across = c.width / 2 - halo - HALO_CLEAR;
      const cands: V[] = []; // from the middle of the refuge out, along the way and across it, every 0.5 m, the whole disc inside the strip and the median
      for (let ds = 0; ds <= along + 1e-9; ds += 0.5) for (const sg of ds ? [1, -1] : [1]) for (let dl = 0; dl <= across + 1e-9; dl += 0.5) for (const lg of dl ? [1, -1] : [1]) cands.push(add(add(a, mul(u, mid + sg * ds)), mul(nrm, lg * dl)));
      const p = spot(cands);
      if (p) signals.push(orb(`r${i}`, p, way, refugeLevel, refugeEdges[i] ?? c.id));
      else skipped.push(`${c.id}/signal/r${i}: no clear ground for its halo on the refuge`);
    });
  }
  out.push(...signals, ...studs);

  // two quarter arcs at each kerb end, one round each corner of the mouth, where they lie on paving
  const { r, w, sweep } = look.arc, R = outerOf(look.arc);
  ends.forEach((e, i) => arcSites(e, c.width, R).forEach(({ c: at, yaw }, j) => {
    const id = partId(c.id, 'arc', `${'ab'[i]}${j}`);
    if (!arcPoints(at, r, yaw, sweep, 12).every(p => site.pave(p))) { skipped.push(`${id}: its band would run over lawn or carriageway`); return; }
    if (!site.take(arcPoints(at, r, yaw, sweep, ARC_SAMPLES), w)) { skipped.push(`${id}: its band would overlap another crossing's arc`); return; }
    out.push(item(id, 'crossing-arc', at, yaw, e.landing.z, [R, R, look.lines.thick], INLAY, e.landing.edge, { arc: { r, w, sweep: r4(sweep), outer: R } }));
  }));
  return out;
}
