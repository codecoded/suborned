// The parts of a crossing (brief 009 B; Sky 2026-10-14: no zebra, no paint, no landing slab; light set flush in the road, floating signal eyes; brief 061, Sky 2026-10-25: signal head A, no brass edge lines, kerb studs, inlay arcs or lit field).
// Everything is placed from the crossing the pairing made and the plan's streetscape.crossings.strip standard, never by hand, and every part is a street item that names its crossing, so Unity draws what is listed and the rules check it:
//   crossing-dots  one module of a light line (LightDotRow_2 / _1: a row of flush dots on a charcoal seat), laid end to end along the walk   crossing-bar  the same line made of LightBar_4 (the plan's other `form`)
//   signal-orb     the signal head over each kerb end and each refuge: a chrome lens facing along the walk, a lit rim, numerals, a beam to a ring in the paving where people wait (the kit piece SignalLens; its clock ring HaloRing stands at signal.halo and is no part of its own)
// The light runs ALONG the walk, kerb to kerb: two lines, one near each edge of the strip, never stripes across it (that is a zebra drawn in light; Aster 2026-10-14). A crossing lays no slab: its landings are keep-clear footprints (Crossing.landings), their paving is the street's.
// The head's state and clock are the runtime's (Latch's SignalHead reads the export's `crossings[].cycle`, cycleOf below); the heads are not turned at runtime, they keep the export's `faces` yaw.
import type { Crossing, Landing, SignalCycle, StreetItem } from '../../../../contracts/world/world.schema';
import { type V, add, dist, mul, sub } from '../../../plan/geom';
import { r3, r4, rng, rv } from '../common';
import { discRing } from '../box';
import { GLOW, ORB } from '../surfaces';

/** Street item kinds that are parts of a crossing. */
export const PART_KINDS = ['crossing-bar', 'crossing-dots', 'signal-orb'] as const;
export type PartKind = (typeof PART_KINDS)[number];

/** The plan's signal cycle (standards.streetscape.crossings.strip.signal.cycle; look only, brief 061): the arithmetic of `cycleOf`. */
export type CycleStd = { walk: number; clearing: { speed: number; round: 'ceil' | 'round' | 'floor' }; stop: number; arriving: number };
/** The plan's look of a crossing (standards.streetscape.crossings.strip: lights, signal). */
export type Look = {
  form: 'bars' | 'dots';
  /** Both light lines: `inset` = strip edge to the line's outer edge; `end` = kerb or refuge edge to the line's end; `thick` = how far the piece stands proud. */
  lines: { inset: number; end: number; thick: number };
  /** The pieces' lengths, longest first, and their frame width: a line is whole modules end to end. */
  bars: { modules: number[]; width: number };
  dots: { modules: number[]; width: number; pitch: number; d: number };
  signal: { state: 'walk' | 'stop'; hover: number; size: number; halo: number; top: number; cycle: CycleStd };
};
export function lookOf(strip: any): Look {
  for (const k of ['lights', 'signal']) if (!strip?.[k]) throw new Error(`streetscape.crossings.strip.${k} is missing: a crossing's parts are placed from it (brief 009)`);
  const form = strip.lights.form ?? 'dots', { inset, end, thick, bars, dots } = strip.lights, c = strip.signal.cycle;
  if (form !== 'bars' && form !== 'dots') throw new Error(`streetscape.crossings.strip.lights.form is '${form}', not bars or dots`);
  if (!c || !(c.walk > 0) || !(c.clearing?.speed > 0) || !(c.stop > 0) || !(c.arriving > 0)) throw new Error('streetscape.crossings.strip.signal.cycle is missing or incomplete (walk, clearing.speed, stop, arriving): a signal crossing exports its cycle (brief 061)');
  return { form, lines: { inset, end, thick }, bars, dots, signal: { ...strip.signal, cycle: { walk: c.walk, clearing: { speed: c.clearing.speed, round: c.clearing.round ?? 'ceil' }, stop: c.stop, arriving: c.arriving } } };
}
/** A signal crossing's cycle (plan strip.signal.cycle): WALK `walk` s, CLEARING = ceil(W / speed) s, STOP = `stop` + CLEARING s with its last `arriving` s ARRIVING; the phase at mission second t is (t + offset) mod cycle, offset a whole second in [0, cycle) from the seeded hash of `signal/<crossing id>`.
 *  W = the crossing's kerb-to-kerb length (refuges included). A 13 m crossing: 6 + 10 + 24 = 40 s; the 28 m Boulevard: 6 + 20 + 34 = 60 s. Look only: the sim, the timetables and the people read nothing from it. */
export function cycleOf(std: CycleStd, id: string, len: number): SignalCycle {
  const clearing = Math[std.clearing.round](len / std.clearing.speed), stop = std.stop + clearing, total = std.walk + clearing + stop;
  return { walk: std.walk, clearing, stop, arrive: std.arriving, offset: rng(`signal/${id}`).int(0, total - 1) };
}

/** A kerb end of a kerb crossing: its landing, the kerb point the strip starts from and the unit vector from the kerb into the pavement. */
export type End = { landing: Landing; k: V; inward: V };
/** What the site offers a part: `clear(ring, at)` = the ground under a halo is free of furniture. */
export type Site = { clear: (ring: V[], at: V) => boolean };

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

/** m: the clock ring's outer radius. HaloRing (tools/decokit signal_cells.py RING_R0 0.47, RING_R1 0.60) is lit in the paving on the head's own centre, 24 segments that fill and drain to show the seconds left; the light disc inside it is the plan's
 *  `signal.halo` (0.47). Anything standing within the ring hides segments and the clock reads wrong, so the ground a head keeps clear is the ring's reach (0.60 from the head's centre), not the halo's alone (0.57 = the halo and a hand's 0.1 until
 *  the ring was drawn). A built number, not a plan number: if HaloRing is re-cut, change it here with the piece and SF-20 follows. */
export const RING_OUTER = 0.6;
/** m: the radius of the ground a head keeps clear of furniture: the halo disc and the clock ring round it. */
export const clearReach = (look: Pick<Look, 'signal'>): number => Math.max(look.signal.halo, RING_OUTER);
/** Where along a landing's width an orb may hang, in metres from the middle, nearest first. */
const SHIFTS = [0, 1, -1];

/** m: a dot row's size along the walk. The plan's module is the laying STEP (2 m, 1 m); the piece drawn in it is module - pitch + width long (LightDotRow_2 1.8, _1 0.8: four and two 0.22 m dots on 0.30 m seats at the 0.5 m pitch, centred in the module), so rows laid end to end keep the pitch across every joint (brief 061; plan lightsFit). */
export const dotsLength = (module: number, dots: { pitch: number; width: number }): number => r3(module - dots.pitch + dots.width);
/** The laying step a dot row of this size along the walk stands in (the inverse of dotsLength). */
export const dotsModule = (length: number, dots: { pitch: number; width: number }): number => r3(length + dots.pitch - dots.width);

/** The parts of one crossing, in drawing order: the two light lines, then (a kerb crossing) the signal heads. `ends` = a kerb crossing's two kerb ends (none for a stop or bank crossing); `refugeEdges[i]` = the id of the median refuge i stands on;
 *  `level` = the surface the lights lie on. Orbs only where the crossing has a control (signal). The orb moves along its landing, or round its refuge, to the nearest spot where `site.clear` says the ground under its halo is free of furniture: skip, never squeeze;
 *  an orb with no such spot is left out and named in `skipped`. */
export function partsOf(c: Pick<Crossing, 'id' | 'line' | 'strip' | 'width' | 'refuges' | 'control'>, look: Look, level: number, ends: End[], refugeEdges: string[], refugeLevel: number, site: Site = { clear: () => true }, skipped: string[] = []): StreetItem[] {
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
          : item(id, 'crossing-dots', at, way, level, [dotsLength(m, look.dots), mod.width, thick], GLOW, c.id, { dots: { n: Math.round(m / look.dots.pitch), pitch: look.dots.pitch, d: look.dots.d } }));
        s += m;
      }
    }
  });

  // a signal orb over each kerb end, its halo on the landing; one over each refuge, its halo on the island
  const { hover, size, halo, state, top } = look.signal, foot = 2 * halo, reach = clearReach(look);
  const orb = (name: string, at: V, yaw: number, ground: number, edge: string) => item(partId(c.id, 'signal', name), 'signal-orb', at, yaw, ground, [foot, foot, top], ORB, edge,
    { castShadows: true, signal: { state, hover, size, faces: [wrap(yaw), wrap(yaw + Math.PI)], halo: { at: rv(at), z: r3(ground), r: halo } } });
  const spot = (cands: V[]) => cands.find(p => site.clear(discRing(p, reach), p));
  const signals: StreetItem[] = [];
  if (c.control === 'signal') {
    ends.forEach((e, i) => {
      const t: V = [-e.inward[1], e.inward[0]], p = spot(SHIFTS.map(o => add(e.landing.at, mul(t, o))));
      if (p) signals.push(orb('ab'[i]!, p, way + (i ? Math.PI : 0), e.landing.z, e.landing.edge)); // display A faces the way across, toward the far kerb
      else skipped.push(`${c.id}/signal/${'ab'[i]}: no clear ground for its halo on the landing`);
    });
    c.refuges.forEach((ring, i) => {
      const s = ring.map(q => (q[0] - a[0]) * u[0] + (q[1] - a[1]) * u[1]), mid = (Math.min(...s) + Math.max(...s)) / 2, along = Math.max(0, (Math.max(...s) - Math.min(...s)) / 2 - reach), across = c.width / 2 - reach;
      const cands: V[] = []; // from the middle of the refuge out, along the way and across it, every 0.5 m, the whole disc inside the strip and the median
      for (let ds = 0; ds <= along + 1e-9; ds += 0.5) for (const sg of ds ? [1, -1] : [1]) for (let dl = 0; dl <= across + 1e-9; dl += 0.5) for (const lg of dl ? [1, -1] : [1]) cands.push(add(add(a, mul(u, mid + sg * ds)), mul(nrm, lg * dl)));
      const p = spot(cands);
      if (p) signals.push(orb(`r${i}`, p, way, refugeLevel, refugeEdges[i] ?? c.id));
      else skipped.push(`${c.id}/signal/r${i}: no clear ground for its halo on the refuge`);
    });
  }
  out.push(...signals);

  return out;
}
