// Brief 075 (Sky 2026-10-28: "where's the rounded corners etc - why can't we just use the old buildings ?"): the old DecoKit frontage system, ported as the
// facade system of every non-Handover block. Sources (read only): ~/Projects/the-suborned/game/Assets/TheSuborned/Editor/DecoKit/DecoKitFrontage.cs (runs,
// caps, Bays, Crescent), DecoKitBlock.cs (the free-standing block: 14 m bays round it, short walls split between the long walls), DecoKitCityBlocks.cs (the
// authored walls: rhythm per wall, low crescents round the circus).
//
// The plan is the source: Datum lays the blocks (contracts/world/harbour-district.plan.json top-level `blocks`, `standards.blocks`, `circus.crescents`; checked by
// BG-1..BG-5). Each block's runs are straight lines of 6 m bays between two caps; this module builds them, as the old DecoKitFrontage.Run did:
//   - corner: the 8 x 8 m cell turning the block on a rounded StreamCorner, shared by the two runs that meet there and placed once, by the longer run (the old
//     long faces ran corner to corner, the short walls started 8 m in);
//   - end: the run's last bay rounds off (StreamEnd; at a run's head its mirrored twin `_M`, Vela's caps075.py, since the export has no mirror);
//   - open / join / arc: flush (joining a party wall or a neighbour's run, a reserved mass, or the crescent that continues the wall);
//   - a streamline segment taller than its neighbour (by more than 1 m) rounds off toward it (DecoKitFrontage.Bays), and an open run end does where the building
//     beyond is lower or there is none;
//   - every segment of a run is one building (one colourway); a short wall's bays inside a long wall's band belong to that wall's end building and take its class
//     (DecoKitBlock.Ends; DecoKitCityBlocks.Short); a block too shallow for two 14 m bands back to back splits at the short wall's bay line (k - k/2 | k/2);
//   - a crescent (circus.crescents, the old Crescent) is bays on 6 m chords of the circle facing its centre, one building, its last bay the end cap; where its fanned
//     14 m bodies reach into a block's band (the plan's runs meet the circle), it and the run buildings it reaches are one building.
// Rhythm per wall is the old authored walls' (OLD_WALLS: segment lengths and class counts per wall kind, never the same class twice in a row), seeded by the run.
// Heights follow Aster's remap (briefs/075/notes.md, Sky 2026-10-28: "lower to around 3 stories high for most of the buildings"): every class is the old 3-storey
// variant; a building that owns a corner cell may stand at 4 (the hero corners); crescents at 3. Pieces stand at true size: scale [1, 1, 1], no piers, no squash; a
// building's height is its class's coping (the old storey grid: ground 5.0 + c x 3.4 + the 1.1 parapet), its storeys the code (B-2 reads `old`). A cap piece is
// used only where its storey code's coping matches the building's.
// A plan without `blocks` (a pinned plan) gets none of this: today's grammar stands, so recorded runs replay as they were. Deterministic, seeded by ids.
import type { Facade } from '../../../contracts/world/world.schema';
import type { KitAsset } from '../../../contracts/world/kit.schema';
import { type V, dist, dot, norm, sub } from '../../plan/geom';
import { type Ctx, type Rng, r3, r4, rng } from './common';
import { adopted } from './kitrefs';
import type { Ring } from './geo';
import { isStreetClass } from './grammar';
import { kindOf } from './edges';
import { cornerPlace } from './stream';

/** The grammar's numbers (old DecoKitFrontage / DecoKitBlock; the plan's standards.blocks carries the same). */
export const OLD_BLOCK = {
  /** The port's switch (a probe sets it false to compare with today's grammar everywhere). */
  on: true,
  bay: 6, cell: 8, body: 14,
  tol: 0.06,
  /** A run's end rounds off where the neighbour beyond is this much lower (old Bays: h > neighbour + 1). */
  taller: 1,
  /** Segment lengths: at least this many bays (today's massing.width 18: a lobby and a shop on the narrowest public face; the old walls' 1- and 2-bay segments
   *  left out). A short wall's own stretch under it joins the building beside it. */
  minSeg: 3,
  /** Aster's remap: storeys of an old-frontage building (B-2), and the share of corner buildings at the higher. */
  storeys: [3, 4] as [number, number], cornerHigh: 0.5,
  /** The crescent's class (old: four crescents of S6; the remap's low class). */
  crescent: 'S3',
  /** m: a wall this near a landmark gateway's foot in the frontage zone (the Crown Avenue arch) carries no tenancy (K-1: no doorway within 1.5 m of the plinth). */
  footClear: 4,
  /** Head-side ends use this mirrored twin of the piece (Vela, caps075.py). */
  mirrorSuffix: '_M',
};

/** The old class table (DecoKitFrontage.Of), our kit ids, the remap's storeys; the first adopted id of each list stands. `play`: only in (true) or only outside
 *  (false) the play streets. */
type ClassRow = { bay: string[]; end: string[]; corner: string[]; code: number; stream: boolean; play?: boolean };
export const OLD_CLASSES: Record<string, ClassRow> = {
  S3: { bay: ['StreamBay_3'], end: ['StreamEnd_3'], corner: ['StreamCorner_3'], code: 3, stream: true },
  S3W: { bay: ['StreamBay_3W'], end: ['StreamEnd_3W'], corner: ['StreamCorner_3'], code: 3, stream: true },
  P3: { bay: ['PortholeBay_3'], end: ['StreamEnd_3W'], corner: ['StreamCorner_3'], code: 3, stream: true, play: false },
  P3L: { bay: ['PortholeBay_3L'], end: ['StreamEnd_3W'], corner: ['StreamCorner_3'], code: 3, stream: true, play: true },
  B3: { bay: ['BalconyBay_3'], end: ['StreamEnd_3'], corner: ['StreamCorner_3'], code: 3, stream: true },
  A3: { bay: ['ArcadeBay_3'], end: [], corner: [], code: 3, stream: false, play: false }, // the arcade: mid-run only (its end and corner, HallCornerWing, are the hall's)
  S4: { bay: ['StreamBay_4'], end: ['StreamEnd_4'], corner: ['StreamCorner_4'], code: 4, stream: true },
  P4: { bay: ['PortholeBay_4'], end: ['StreamEnd_4'], corner: ['StreamCorner_4'], code: 4, stream: true, play: false },
  P4L: { bay: ['PortholeBay_4L'], end: ['StreamEnd_4'], corner: ['StreamCorner_4'], code: 4, stream: true, play: true },
  B4: { bay: ['BalconyBay_4'], end: ['StreamEnd_4'], corner: ['StreamCorner_4'], code: 4, stream: true },
};
/** The remap: each class of the old walls as its 3-storey variant (the old 6 / 9 / 10 height contrast becomes the family contrast), and each 3-storey class's
 *  4-storey sibling for a corner building. The play streets take the storey-true porthole (no fin over the roof) and no arcade (its mast stands over it). */
export const LOW_OF: Record<string, string> = { S6: 'S3', S9: 'S3W', S9W: 'S3W', D10: 'B3', P7: 'P3', B8: 'B3', A3: 'A3' };
export const HIGH_OF: Record<string, string> = { S3: 'S4', S3W: 'S4', P3: 'P4', P3L: 'P4L', B3: 'B4' };
const PLAY_OF: Record<string, string> = { P3: 'P3L', P4: 'P4L' };

/** The authored walls of DecoKitCityBlocks.Build, as data (the rules below are counted from them). `street`: the boulevard walls (they face the main street);
 *  `terrace`: the terrace, garden and gardens-square walls (the quieter streets). H (the civic hall) is left out: it is the civic block's. */
export const OLD_WALLS: Record<'street' | 'terrace', [string, number][][]> = {
  street: [
    [['S9', 4], ['D10', 3], ['S6', 6]], [['S9W', 5], ['S9', 3]], [['S9', 5], ['S6', 1]], [['S6', 3], ['D10', 2], ['P7', 3], ['S9W', 4], ['S9', 5]],
    [['S6', 2], ['S9W', 4], ['D10', 3], ['S9', 4], ['S6', 3]], [['S6', 3], ['D10', 2], ['S9', 4], ['B8', 3], ['S9W', 5]],
  ],
  terrace: [
    [['S6', 4], ['B8', 5], ['P7', 4]], [['D10', 3], ['S6', 5]], [['S9W', 5]], [['A3', 3], ['S9', 4], ['S9W', 5], ['S6', 4]],
    [['S6', 4]], [['S6', 10], ['S9W', 5], ['S6', 6], ['S9', 6]], [['S6', 4], ['S9', 4], ['S6', 4], ['S9W', 4]],
    [['S6', 6], ['B8', 5], ['S6', 4], ['D10', 3], ['S6', 6], ['P7', 5], ['S6', 5], ['S9W', 5], ['S6', 6]],
  ],
};
/** Per wall kind: how often each (remapped) class opens a segment, and how long segments run (bays), counted from OLD_WALLS. */
export const OLD_RHYTHM = (Object.keys(OLD_WALLS) as ('street' | 'terrace')[]).reduce((o, k) => {
  const cls: Record<string, number> = {}, len: Record<number, number> = {};
  for (const w of OLD_WALLS[k]) for (const [c, n] of w) { const l = LOW_OF[c] ?? c; cls[l] = (cls[l] ?? 0) + 1; len[n] = (len[n] ?? 0) + 1; }
  o[k] = { cls, len };
  return o;
}, {} as Record<'street' | 'terrace', { cls: Record<string, number>; len: Record<number, number> }>);
export const wallKind = (cls: string): 'street' | 'terrace' => (['district', 'circus', 'primary'].includes(kindOf(cls)) ? 'street' : 'terrace');

/** The coping of storey code c (skyline.py: ground 5.0 + c floors of 3.4 + the 0.95 parapet and its 0.15 cap); DecoBay_10's parapet tops at 39.95. */
export const copingOf = (code: number): number => (code === 10 ? 39.95 : r3(6.1 + 3.4 * code));
const codeOfId = (id: string): number => Number(/_(\d+)/.exec(id)?.[1] ?? 0);

/** A class resolved against the adopted kit: its bay, its end (and mirrored twin) and corner where their storey code's coping is the class's, its coping. */
export type Resolved = { cls: string; bay: KitAsset; end?: KitAsset; endM?: KitAsset; corner?: KitAsset; code: number; top: number; stream: boolean };
let RES: { key: string; map: Map<string, Resolved> } | null = null;
export function resolveClasses(): Map<string, Resolved> {
  const all = adopted(), key = all.map(a => a.id).join(',');
  if (RES?.key === key) return RES.map;
  const by = new Map(all.map(a => [a.id, a])), map = new Map<string, Resolved>();
  for (const [cls, row] of Object.entries(OLD_CLASSES)) {
    const bay = row.bay.map(id => by.get(id)).find(a => !!a && codeOfId(a.id) === row.code); if (!bay) continue;
    const top = copingOf(row.code), fits = (a: KitAsset | undefined) => !!a && Math.abs(copingOf(codeOfId(a.id)) - top) < 0.5;
    const end = row.end.map(id => by.get(id)).find(fits), corner = row.corner.map(id => by.get(id)).find(fits);
    const endM = end ? by.get(end.id + OLD_BLOCK.mirrorSuffix) : undefined;
    map.set(cls, { cls, bay, ...(end ? { end } : {}), ...(endM ? { endM } : {}), ...(corner ? { corner } : {}), code: row.code, top, stream: row.stream });
  }
  RES = { key, map };
  return map;
}

/** The 3-storey classes a wall draws (resolved in our kit; the play streets' storey-true set). */
export function poolFor(play: boolean): string[] {
  const R = resolveClasses();
  return Object.keys(OLD_CLASSES).filter(c => OLD_CLASSES[c]!.code === OLD_BLOCK.storeys[0] && R.has(c) && (OLD_CLASSES[c]!.play === undefined || OLD_CLASSES[c]!.play === play));
}
const weightOf = (kind: 'street' | 'terrace', c: string) => OLD_RHYTHM[kind].cls[c] ?? OLD_RHYTHM[kind].cls[Object.entries(PLAY_OF).find(([, v]) => v === c)?.[0] ?? ''] ?? 0.5;

/** One wall's segments (DecoKitCityBlocks' rules, seeded): lengths drawn from the old walls' lengths for this kind (at least minSeg, no remainder under it),
 *  classes from the old walls' class counts, never the class of the segment before, a capped end (a corner cell) taking a class with its own corner piece. */
export function wallSegs(r: Rng, kind: 'street' | 'terrace', k: number, pool: string[], capTail: boolean, capHead: boolean, prev = ''): [string, number][] {
  const R = resolveClasses(), lens = Object.entries(OLD_RHYTHM[kind].len).map(([n, w]) => [Number(n), w] as [number, number]).filter(([n]) => n >= OLD_BLOCK.minSeg);
  const sizes: number[] = []; let left = k;
  while (left > 0) {
    let n = r.weighted(lens, x => x[1])[0];
    if (n >= left || left - n < OLD_BLOCK.minSeg) n = left;
    sizes.push(n); left -= n;
  }
  const out: [string, number][] = [];
  sizes.forEach((n, i) => {
    const capped = (i === 0 && capTail) || (i === sizes.length - 1 && capHead);
    let cands = pool.filter(c => c !== prev && (!capped || !!R.get(c)?.corner));
    if (!cands.length) cands = pool.filter(c => !capped || !!R.get(c)?.corner);
    if (!cands.length) cands = pool;
    const c = r.weighted(cands, x => weightOf(kind, x));
    out.push([c, n]); prev = c;
  });
  return out;
}

/** The old class a plan run's authored segment takes (the remap's low class; H, the old civic hall at 11.1 m, is the Handover's and stands under the 3-storey
 *  floor with a corner wing over its body, so its wall takes the low streamline; A3, the arcade, has no end or corner of its own), in our kit and this street. */
export function planClass(old: string, pool: string[], need: { corner?: boolean; end?: boolean }): string {
  const R = resolveClasses(), low = old === 'H' ? 'S3' : LOW_OF[old] ?? old, c = PLAY_OF[low] && pool.includes(PLAY_OF[low]!) ? PLAY_OF[low]! : low, x = R.get(c);
  const ok = !!x && pool.includes(c) && (!need.corner || !!x.corner) && (!need.end || !!x.end);
  return ok ? c : pool.find(y => y.startsWith('S3') && R.get(y)?.corner && R.get(y)?.end) ?? pool[0]!;
}
/** A run's authored segments (plan `segs`, the old Wall / Short Seg lists) over bays [q, e) in tail → head order, as [class, bays]: one building per segment, a
 *  segment under minSeg joining the one before it (else after: today's massing width), the class remapped (planClass). null: the run carries none, or they do not sum
 *  to its bays (the grammar's seeded rhythm stands, logged). */
export function planSegs(segs: { cls: string; n: number }[] | undefined, bays: number, fwd: boolean, q: number, e: number, pool: string[], need: (i: number, n: number, last: boolean) => { corner?: boolean; end?: boolean }): [string, number][] | null {
  if (!segs?.length || segs.reduce((s, x) => s + x.n, 0) !== bays) return null;
  const per: { cls: string; g: number }[] = [];
  (fwd ? segs : [...segs].reverse()).forEach((x, g) => { for (let i = 0; i < x.n; i++) per.push({ cls: x.cls, g }); });
  const groups: { cls: string; n: number }[] = [];
  for (const b of per.slice(q, e)) { const l = groups[groups.length - 1]; if (l && (l as any).g === b.g) l.n++; else groups.push(Object.assign({ cls: b.cls, n: 1 }, { g: b.g })); }
  for (let i = 0; i < groups.length && groups.length > 1;) {
    if (groups[i]!.n >= OLD_BLOCK.minSeg) { i++; continue; }
    const j = i > 0 ? i - 1 : i + 1; groups[j]!.n += groups[i]!.n; groups.splice(i, 1); i = 0;
  }
  let at = 0;
  return groups.map((x, i) => { const c = planClass(x.cls, pool, need(at, x.n, i === groups.length - 1)); at += x.n; return [c, x.n] as [string, number]; });
}

// ---------------------------------------------------------------- the plan's blocks: buildings and face runs

type Cap = 'corner' | 'end' | 'open' | 'join' | 'arc';
export type PlanRun = { face: string; a: V; b: V; n: V; faces: string; caps: [Cap, Cap]; bays: number; gap?: [number, number]; segs?: { cls: string; n: number }[]; old?: string };
export type PlanBlock = { id: string; plot: string; kind: 'perimeter' | 'inner'; rect: [number, number, number, number]; depth: number; court: 'solid' | 'open'; runs: PlanRun[]; corners: V[] };
export type PlanCrescent = { id: string; plot: string; block: string; from: string; c: V; r: number; rEnds: number; stepDeg: number; deg: [number, number]; sweep: 'cw' | 'ccw'; bays: number; caps: [Cap, Cap]; band: number };

/** One building of an old block: a run segment (with any corner cell and short-wall bays it owns), or a crescent. `extra`: further masses (a short wall's bay body
 *  only partly inside the band it joins). */
export type OldBld = { id: string; plot: string; area: string; use: string; ring: Ring; extra: V[][]; cls: string; code: number; top: number; crescent?: boolean };
/** A slot along a face run: a corner cell (at `v`, between the incoming face's normal `n0` and the leaving face's `n1`) or a 6 m bay from `a` to `b`. `force`: an
 *  end cap the plan names on this bay ('tail' rounds toward the run's tail, 'head' toward its head). */
type Slot = { kind: 'corner'; v: V; n0: V; n1: V; t0: V; bld: string } | { kind: 'bay'; a: V; b: V; n: V; bld: string; force?: 'tail' | 'head' };
/** A face run in CCW order: its slots, and what lies past its tail and head. `open` = the end may round off toward a lower neighbour (open, join). */
export type FaceRun = { id: string; slots: Slot[]; tail: RunEnd; head: RunEnd; crescent?: boolean };
type RunEnd = { open: boolean; at: V; t: V };
export type OldPlot = { plot: string; blds: OldBld[]; runs: FaceRun[] };
export type OldPlotLog = { plot: string; fit: 'blocks' | 'fallback'; why?: string; blocks?: number; buildings?: number; authored?: number; segMiss?: string[]; crescentBays?: [number, number] };

const lerp = (a: V, b: V, s: number): V => [a[0] + (b[0] - a[0]) * s, a[1] + (b[1] - a[1]) * s];
type R4 = [number, number, number, number]; // x0, x1, y0, y1
const rectRing = (r: R4): V[] => [[r[0], r[2]], [r[1], r[2]], [r[1], r[3]], [r[0], r[3]]];
const overlapR = (a: R4, b: R4) => Math.max(0, Math.min(a[1], b[1]) - Math.max(a[0], b[0])) * Math.max(0, Math.min(a[3], b[3]) - Math.max(a[2], b[2]));
const inR = (q: V, x: R4) => q[0] > x[0] && q[0] < x[1] && q[1] > x[2] && q[1] < x[3];
/** The gap between segment pq and an axis-aligned rect (0 where they meet), sampled every 0.5 m. */
const segRectGap = (p: V, q: V, r: R4) => { const n = Math.max(1, Math.ceil(dist(p, q) / 0.5)); let m = Infinity; for (let i = 0; i <= n; i++) { const z = lerp(p, q, i / n), dx = Math.max(r[0] - z[0], 0, z[0] - r[1]), dy = Math.max(r[2] - z[1], 0, z[1] - r[3]); m = Math.min(m, Math.hypot(dx, dy)); } return m; };
/** Two rects share an edge at least 1 m long. */
const touches = (a: R4, b: R4) => { const ox = Math.min(a[1], b[1]) - Math.max(a[0], b[0]), oy = Math.min(a[3], b[3]) - Math.max(a[2], b[2]); return (Math.abs(ox) < 0.01 && oy > 1) || (Math.abs(oy) < 0.01 && ox > 1); };
/** The axis-aligned rect of a run's span s0..s1 (m from its CCW tail P along t) and depth 0..d behind its facade (outward normal n). */
const bandRect = (P: V, t: V, n: V, s0: number, s1: number, d: number): R4 => {
  const q = [s0, s1].flatMap(s => { const p: V = [P[0] + t[0] * s, P[1] + t[1] * s]; return [p, [p[0] - n[0] * d, p[1] - n[1] * d] as V]; });
  return [Math.min(...q.map(p => p[0])), Math.max(...q.map(p => p[0])), Math.min(...q.map(p => p[1])), Math.max(...q.map(p => p[1]))].map(r4) as R4;
};

/** The plan's blocks and crescents of one plot as old-frontage buildings and runs (null: the plan lays no blocks on the plot). */
export function planBlocks(ctx: Ctx, plot: { id: string; use: string; area: string }): { out: OldPlot; log: OldPlotLog } | null {
  if (!OLD_BLOCK.on) return null;
  const blocks = (((ctx.plan as any).blocks ?? []) as PlanBlock[]).filter(b => b.plot === plot.id);
  if (!blocks.length) return null;
  const { bay: BAY, cell: CELL, body: BODY, tol: TOL } = OLD_BLOCK, play = !!ctx.B.caps?.[plot.area]?.play, pool = poolFor(play), R = resolveClasses();
  const blds: OldBld[] = [], runsOut: FaceRun[] = [], rects = new Map<string, R4[]>(), segMiss: string[] = []; let k = 0, segUsed = 0;
  const fail = (why: string) => ({ out: { plot: plot.id, blds: [], runs: [] }, log: { plot: plot.id, fit: 'fallback' as const, why } });
  const newB = (c: string, r: R4) => { const id = `bld/${plot.id}/${k++}`, x = R.get(c)!; blds.push({ id, plot: plot.id, area: plot.area, use: plot.use, ring: { p: [], c: [] }, extra: [], cls: c, code: x.code, top: x.top }); rects.set(id, [r]); return id; };
  const feet = (((ctx.plan as any).landmarks ?? []) as { gateway?: { feet: R4[]; zones?: string[] } }[]).flatMap(l => (l.gateway?.zones ?? []).includes('frontage') ? l.gateway!.feet : []);
  const ringCls = (faces: string) => (kindOf(faces) === 'passage' ? 'none' : faces); // a passage is paved ground inside the plot: a public wall, no tenancy
  for (const blk of blocks) {
    // the runs in CCW form: tail P → head Q along t, outward n, caps at tail / head
    const runs = blk.runs.map((r, i) => {
      const tc: V = [-r.n[1], r.n[0]], fwd = dot(norm(sub(r.b, r.a)), tc) > 0, P = fwd ? r.a : r.b, Q = fwd ? r.b : r.a, caps = fwd ? r.caps : [r.caps[1], r.caps[0]] as [Cap, Cap];
      return { i, P: P as V, Q: Q as V, t: norm(sub(Q, P)), n: r.n as V, L: dist(P, Q), cls: ringCls(r.faces), faces: r.faces, capT: caps[0], capH: caps[1], bays: r.bays, fwd, segs: r.segs };
    });
    type Run = (typeof runs)[number];
    const bad = runs.find(r => Math.abs(r.L - (CELL * ((r.capT === 'corner' ? 1 : 0) + (r.capH === 'corner' ? 1 : 0)) + BAY * r.bays)) > TOL);
    if (bad) return fail(`${blk.id} run ${bad.i}: ${r3(bad.L)} m is not its caps + 6 x ${bad.bays} (BG-2)`);
    // corner owners: of the two runs meeting at a corner cell, the one with more bays (the old long faces)
    const near = (p: V, q: V) => dist(p, q) < 0.05;
    const cornerOf = (r: Run, end: 'T' | 'H'): Run | null => runs.find(x => x !== r && (end === 'T' ? x.capH === 'corner' && near(x.Q, r.P) : x.capT === 'corner' && near(x.P, r.Q))) ?? null;
    const owns = (r: Run, end: 'T' | 'H') => { const o = cornerOf(r, end); return !o || r.bays > o.bays || (r.bays === o.bays && r.i < o.i); };
    // primary axis: the longest run's; the depth across it, and the split of a solid block (two bands back to back) at the short wall's bay line
    const longest = runs.reduce((a, b) => (b.L > a.L ? b : a)), alongX = Math.abs(longest.t[0]) > 0.5, D = alongX ? blk.rect[3] - blk.rect[2] : blk.rect[1] - blk.rect[0];
    const primary = runs.filter(r => (Math.abs(r.t[0]) > 0.5) === alongX), secondary = runs.filter(r => !primary.includes(r));
    const solid = blk.court === 'solid' && D < 2 * BODY + 12 + TOL; // the old free-standing block (28 back to back; up to 39 with the middle decked)
    const shortC = secondary.find(r => r.capT === 'corner' && r.capH === 'corner');
    const split = shortC ? CELL + BAY * Math.ceil(shortC.bays / 2) : D / 2; // from the low side (south / west)
    const depthOf = (r: Run) => {
      if (!solid || !primary.includes(r)) return BODY;
      const low = alongX ? r.n[1] < 0 : r.n[0] < 0, opp = primary.some(o => o !== r && dot(o.n, r.n) < -0.5);
      return opp ? (low ? split : D - split) : D;
    };
    type Bay = { j: number; s0: number; s1: number; bld?: string; force?: 'tail' | 'head' };
    const baysOf = new Map<number, Bay[]>();
    for (const r of runs) {
      const s0 = r.capT === 'corner' ? CELL : 0, list: Bay[] = [];
      for (let j = 0; j < r.bays; j++) list.push({ j, s0: s0 + BAY * j, s1: s0 + BAY * (j + 1), ...(j === 0 && r.capT === 'end' ? { force: 'tail' as const } : j === r.bays - 1 && r.capH === 'end' ? { force: 'head' as const } : {}) });
      baysOf.set(r.i, list);
    }
    // primary runs first: segments → buildings (each segment's band, with the corner cells it owns)
    for (const r of primary) {
      const list = baysOf.get(r.i)!, need = (at: number, n: number) => ({ corner: (at === 0 && r.capT === 'corner') || (at + n === r.bays && r.capH === 'corner'), end: (at === 0 && r.capT === 'end') || (at + n === r.bays && r.capH === 'end') });
      const authored = planSegs(r.segs, r.bays, r.fwd, 0, r.bays, pool, need); if (r.segs && !authored) segMiss.push(`${blk.id}/run${r.i}`); else if (authored) segUsed++;
      const segs = authored ?? wallSegs(rng(`oldblock/${blk.id}/run${r.i}`), wallKind(r.faces), r.bays, pool, r.capT === 'corner', r.capH === 'corner');
      let j = 0;
      segs.forEach(([c0, n], g) => {
        const first = list[j]!, last = list[j + n - 1]!, cT = g === 0 && r.capT === 'corner', cH = g === segs.length - 1 && r.capH === 'corner';
        const s0 = cT ? 0 : first.s0, s1 = cH ? r.L : last.s1, high = (cT && owns(r, 'T') || cH && owns(r, 'H')) && rng(`oldblock/${blk.id}/run${r.i}/seg${g}/high`).chance(OLD_BLOCK.cornerHigh);
        const c = high && HIGH_OF[c0] && R.has(HIGH_OF[c0]!) ? HIGH_OF[c0]! : c0;
        const id = newB(c, bandRect(r.P, r.t, r.n, s0, s1, depthOf(r)));
        for (let q = j; q < j + n; q++) list[q]!.bld = id;
        j += n;
      });
    }
    // secondary runs: a bay whose body stands in a building's band joins it (a body only partly in it adds its body as a further mass); the rest form the run's own segments
    for (const r of secondary) {
      const list = baysOf.get(r.i)!;
      for (const b of list) {
        const body = bandRect(r.P, r.t, r.n, b.s0, b.s1, BODY);
        let best = '', most = 0.5;
        for (const [id, rs] of rects) { const o = rs.reduce((s, x) => s + overlapR(x, body), 0); if (o > most) { most = o; best = id; } }
        if (best) { b.bld = best; if (most < BAY * BODY - 0.5) { blds.find(y => y.id === best)!.extra.push(rectRing(body)); rects.get(best)!.push(body); } }
      }
      let q = 0;
      while (q < list.length) {
        if (list[q]!.bld) { q++; continue; }
        let e = q; while (e < list.length && !list[e]!.bld) e++;
        const prevB = q > 0 ? blds.find(y => y.id === list[q - 1]!.bld) : undefined;
        if (e - q < OLD_BLOCK.minSeg) { // too short for a building of its own: it joins the building beside it on the run, else one its band touches
          const body = bandRect(r.P, r.t, r.n, list[q]!.s0, list[e - 1]!.s1, BODY), nextB = e < list.length ? list[e]!.bld : undefined;
          const touch = prevB?.id ?? nextB ?? [...rects].find(([, rs]) => rs.some(x => touches(x, body)))?.[0];
          if (touch) { blds.find(y => y.id === touch)!.extra.push(rectRing(body)); rects.get(touch)!.push(body); for (let z = q; z < e; z++) list[z]!.bld = touch; q = e; continue; }
        }
        const authored = planSegs(r.segs, r.bays, r.fwd, q, e, pool, (at, n) => ({ end: (q + at === 0 && r.capT === 'end') || (q + at + n === r.bays && r.capH === 'end') })); if (r.segs && !authored) segMiss.push(`${blk.id}/run${r.i}`); else if (authored) segUsed++;
        const segs = authored ?? wallSegs(rng(`oldblock/${blk.id}/run${r.i}/${q}`), wallKind(r.faces), e - q, pool, false, false, prevB?.cls ?? '');
        let j = q;
        for (const [c, n] of segs) { const id = newB(c, bandRect(r.P, r.t, r.n, list[j]!.s0, list[j + n - 1]!.s1, BODY)); for (let z = j; z < j + n; z++) list[z]!.bld = id; j += n; }
        q = e;
      }
    }
    // face runs: corner cells (placed by their owner) and bays
    const ownerAt = (r: Run, s0: number, s1: number) => { const p = lerp(r.P, r.Q, (s0 + s1) / 2 / r.L), q: V = [p[0] - r.n[0] * 2, p[1] - r.n[1] * 2]; return blds.find(b => (rects.get(b.id) ?? []).some(x => inR(q, x)))?.id ?? ''; };
    for (const r of runs) {
      const slots: Slot[] = [], list = baysOf.get(r.i)!;
      if (r.capT === 'corner' && owns(r, 'T')) { const o = cornerOf(r, 'T'); if (o) slots.push({ kind: 'corner', v: r.P, n0: o.n, n1: r.n, t0: o.t, bld: ownerAt(r, 0, CELL) }); }
      for (const b of list) slots.push({ kind: 'bay', a: lerp(r.P, r.Q, b.s0 / r.L), b: lerp(r.P, r.Q, b.s1 / r.L), n: r.n, bld: b.bld!, ...(b.force ? { force: b.force } : {}) });
      if (r.capH === 'corner' && owns(r, 'H')) { const o = cornerOf(r, 'H'); if (o) slots.push({ kind: 'corner', v: r.Q, n0: r.n, n1: o.n, t0: r.t, bld: ownerAt(r, r.L - CELL, r.L) }); }
      runsOut.push({ id: `${blk.id}/run${r.i}`, slots, tail: { open: r.capT === 'open' || r.capT === 'join', at: r.P, t: r.t }, head: { open: r.capH === 'open' || r.capH === 'join', at: r.Q, t: r.t } });
    }
    if (runsOut.some(r => r.slots.some(x => !x.bld))) return fail(`${blk.id}: a slot without its building`);
    // building rings: the main band rect, edges classed by what lies beyond (the run it fronts, another building, else the court)
    for (const b of blds) {
      if (b.ring.p.length) continue;
      const pts = rectRing(rects.get(b.id)![0]!), c: string[] = [];
      for (let i = 0; i < 4; i++) {
        const p = pts[i]!, q = pts[(i + 1) % 4]!, m = lerp(p, q, 0.5), u = norm(sub(q, p)), nn: V = [u[1], -u[0]], probe: V = [m[0] + nn[0] * 0.1, m[1] + nn[1] * 0.1];
        const fr = runs.find(r => Math.abs(dot(sub(m, r.P), r.n)) < TOL && dot(nn, r.n) > 0.5 && dot(sub(m, r.P), r.t) > -TOL && dot(sub(m, r.P), r.t) < r.L + TOL);
        const cls = fr ? (feet.some(f => segRectGap(p, q, f) < OLD_BLOCK.footClear) ? 'none' : fr.cls) : [...rects].some(([id, rs]) => id !== b.id && rs.some(x => inR(probe, x))) ? 'party' : 'court';
        c.push(cls);
      }
      b.ring = { p: pts.map(q => [r4(q[0]), r4(q[1])] as V), c };
    }
  }
  // crescents (circus.crescents): chord bays facing the centre, one building. The old crescent's fanned bodies overlapped the boulevard walls' last bodies (the old
  // never minded); ours must stand inside their building's masses (K-6) and no two buildings may overlap (B-4), so where the bodies reach into a block's band the
  // crescent and the run buildings it reaches are one building (the crescent "continues the wall", the plan's open join), at the crescent's class
  const cres = (((ctx.plan as any).circus?.crescents ?? []) as PlanCrescent[]).filter(c => c.plot === plot.id);
  let kept = 0, asked = 0;
  for (const cr of cres) {
    asked += cr.bays;
    const sgn = cr.sweep === 'cw' ? -1 : 1, step = (cr.stepDeg * Math.PI) / 180, a0 = (cr.deg[0] * Math.PI) / 180, c = cr.c as V;
    const ray = (j: number, rad: number): V => [c[0] + rad * Math.cos(a0 + sgn * step * j), c[1] + rad * Math.sin(a0 + sgn * step * j)];
    const Rb = (cr.r + BODY + 0.05) / Math.cos(step / 2), J = Array.from({ length: cr.bays + 1 }, (_, j) => ray(j, cr.rEnds)), Bk = Array.from({ length: cr.bays + 1 }, (_, j) => ray(j, Rb));
    const quads = Array.from({ length: cr.bays }, (_, j) => [J[j]!, J[j + 1]!, Bk[j + 1]!, Bk[j]!]);
    const hit = [...rects].filter(([, rs]) => rs.some(x => quads.some(q => quadHitsRect(q, x)))).map(([id]) => id).sort((x, y) => Number(x.split('/').pop()) - Number(y.split('/').pop()));
    kept += cr.bays;
    // CCW round the building is decreasing angle: a cw sweep runs CCW (the open join at the tail, the end at the head); a ccw sweep the other way
    const idx = Array.from({ length: cr.bays + 1 }, (_, j) => j), ord = sgn < 0 ? idx : [...idx].reverse();
    const front = ord.map(j => J[j]!), back = ord.map(j => Bk[j]!).reverse(), x = R.get(OLD_BLOCK.crescent)!, fan = [...front, ...back].map(q => [r4(q[0]), r4(q[1])] as V);
    let id: string;
    if (!hit.length) {
      id = `bld/${plot.id}/${k++}`;
      blds.push({ id, plot: plot.id, area: plot.area, use: plot.use, ring: { p: fan, c: [...front.slice(0, -1).map(() => 'circus:crown-circus'), 'party', ...back.slice(0, -1).map(() => 'party'), 'party'] }, extra: [], cls: x.cls, code: x.code, top: x.top, crescent: true });
    } else {
      id = hit[0]!;
      const m = blds.find(y => y.id === id)!;
      for (const o of hit.slice(1)) { // the other buildings it reaches join too: their masses, their slots
        const ob = blds.find(y => y.id === o)!;
        m.extra.push(ob.ring.p, ...ob.extra); rects.get(id)!.push(...rects.get(o)!);
        for (const run of runsOut) for (const sl of run.slots) if (sl.bld === o) sl.bld = id;
        blds.splice(blds.indexOf(ob), 1); rects.delete(o);
      }
      m.extra.push(fan); m.cls = x.cls; m.code = x.code; m.top = x.top; m.crescent = true;
    }
    const slots: Slot[] = [];
    for (let q = 0; q + 1 < front.length; q++) { const a = front[q]!, b = front[q + 1]!, u = norm(sub(b, a)); slots.push({ kind: 'bay', a, b, n: [u[1], -u[0]], bld: id }); }
    if (cr.caps[1] === 'end') { const sl = slots[sgn < 0 ? slots.length - 1 : 0] as Extract<Slot, { kind: 'bay' }>; sl.force = sgn < 0 ? 'head' : 'tail'; } // the plan's end cap
    runsOut.push({ id: cr.id, slots, crescent: true, tail: { open: false, at: front[0]!, t: norm(sub(front[1]!, front[0]!)) }, head: { open: false, at: front[front.length - 1]!, t: norm(sub(front[front.length - 1]!, front[front.length - 2]!)) } });
  }
  return { out: { plot: plot.id, blds, runs: runsOut }, log: { plot: plot.id, fit: 'blocks', blocks: blocks.length, buildings: blds.length, ...(segUsed ? { authored: segUsed } : {}), ...(segMiss.length ? { segMiss } : {}), ...(cres.length ? { crescentBays: [kept, asked] as [number, number] } : {}) } };
}

/** A convex quad and an axis-aligned rect overlap by more than a sliver (separating axes, 0.05 m slack). */
export function quadHitsRect(q: V[], r: R4): boolean {
  const rp = rectRing(r), axes: V[] = [[1, 0], [0, 1], ...q.map((p, i) => { const e = sub(q[(i + 1) % q.length]!, p); return norm([-e[1], e[0]]); })];
  for (const ax of axes) {
    const pa = q.map(p => dot(p, ax)), pb = rp.map(p => dot(p, ax));
    if (Math.max(...pa) <= Math.min(...pb) + 0.05 || Math.max(...pb) <= Math.min(...pa) + 0.05) return false;
  }
  return true;
}

// ---------------------------------------------------------------- the pieces

export type OldCounts = { plots: number; crescents: number; buildings: number; trueSize: number; bays: number; corners: number; ends: number; endsMirror: number; square: number; crescentBays: number };
export type OldLog = { plots: OldPlotLog[]; counts: OldCounts; classes: Record<string, number> };

/** The pieces of every old plot's runs: bays at true size, the corner cells, the ends (the plan's end caps; where a streamline segment is taller than the one beyond
 *  it; an open run end with nothing as tall beyond). `beyond(q, self)` = the height (m above the pavement) of the building standing at q, else null. */
export function layOld(ctx: Ctx, plots: OldPlot[], beyond: (q: V, self: string) => number | null): { facades: Facade[]; metres: Map<string, { street: number; kit: number }>; counts: Omit<OldCounts, 'plots'>; classes: Record<string, number> } {
  const R = resolveClasses(), pav = ctx.pav, facades: Facade[] = [], metres = new Map<string, { street: number; kit: number }>(), classes: Record<string, number> = {};
  const counts = { crescents: 0, buildings: 0, trueSize: 0, bays: 0, corners: 0, ends: 0, endsMirror: 0, square: 0, crescentBays: 0 };
  const blds = new Map(plots.flatMap(p => p.blds).map(b => [b.id, b])), perB = new Map<string, number>();
  const yawOf = (n: V) => r4(Math.atan2(n[1], n[0]) - Math.PI / 2);
  // export point (footprint centre) of a piece whose kit origin stands at `pivot`, front along n: pivot + kx cx + kz cz (kx = (cos yaw, sin yaw), kz = n)
  const place = (a: KitAsset, pivot: V, n: V): { at: V; yaw: number } => { const yaw = yawOf(n), kx: V = [Math.cos(yaw), Math.sin(yaw)], [cx, cz] = a.pivotOffset ?? [0, 0]; return { at: [r3(pivot[0] + kx[0] * cx + n[0] * cz), r3(pivot[1] + kx[1] * cx + n[1] * cz)], yaw }; };
  // the ring edge a piece stands on: the one whose line holds both its ends (nearest by its middle among those), else the nearest by its middle (a crescent wing's bay)
  const edgeOf = (ring: Ring, a: V, b: V): number => {
    const m = lerp(a, b, 0.5), line = (p: V, q: V, z: V) => { const u = sub(q, p), L = Math.hypot(u[0], u[1]) || 1; return Math.abs(((z[0] - p[0]) * u[1] - (z[1] - p[1]) * u[0]) / L); };
    let best = 0, bd = Infinity;
    for (const strict of [true, false]) {
      ring.p.forEach((p, i) => { const q = ring.p[(i + 1) % ring.p.length]!; if (strict && (line(p, q, a) > 0.01 || line(p, q, b) > 0.01)) return; const u = sub(q, p), L = Math.hypot(u[0], u[1]) || 1, s = Math.max(0, Math.min(L, dot(sub(m, p), u) / L)), d = dist(m, [p[0] + u[0] / L * s, p[1] + u[1] / L * s]); if (d < bd) { bd = d; best = i; } });
      if (bd < Infinity) return best;
    }
    return best;
  };
  const push = (bld: OldBld, f: Omit<Facade, 'id' | 'building' | 'storeys' | 'base'>, drop = 0) => {
    const i = perB.get(bld.id) ?? 0; perB.set(bld.id, i + 1);
    facades.push({ id: `${bld.id}/f${i}`, building: bld.id, storeys: bld.code, base: r3(pav - drop), ...f });
    counts.trueSize++;
  };
  for (const pl of plots) for (const run of pl.runs) {
    const S = run.slots, bays = S.map((x, i) => (x.kind === 'bay' ? i : -1)).filter(i => i >= 0), hOf = (id: string) => blds.get(id)!.top;
    for (const i of S.keys()) {
      const x = S[i]!, b = blds.get(x.bld)!, rs = R.get(b.cls)!;
      if (x.kind === 'corner') {
        const piece = rs.corner ?? R.get('S3')!.corner!; // a corner building draws a class with its own corner (wallSegs)
        const cp = cornerPlace(x.v, x.n0, x.n1, piece, 1), a: V = [x.v[0] - x.t0[0] * OLD_BLOCK.cell, x.v[1] - x.t0[1] * OLD_BLOCK.cell];
        push(b, { form: 'corner', kit: { id: piece.id, scale: [1, 1, 1], yaw: cp.yaw, at: [r3(cp.at[0]), r3(cp.at[1])] }, a: [r3(a[0]), r3(a[1])], b: [r3(x.v[0]), r3(x.v[1])], edge: edgeOf(b.ring, a, x.v), normal: [r4(x.n0[0]), r4(x.n0[1])] });
        counts.corners++; continue;
      }
      // rounding: toward the tail where the slot before is another building lower by more than `taller` (or an open run end with nothing as tall beyond), toward
      // the head likewise, or where the plan names an end cap; the tail is the end piece's natural side (stream.ts KIT_X), the head its mirrored twin
      const prev = S[i - 1], next = S[i + 1], k = bays.indexOf(i), drop = run.crescent && k % 2 === 1 ? 0.005 : 0; // old Crescent: odd bays 5 mm down (the chords' canopies overlap at the joints)
      const lowerThan = (h: number | null) => h === null || b.top > h + OLD_BLOCK.taller;
      const t = norm(sub(x.b, x.a)), nb = (q: V, s: number): V => [q[0] + t[0] * s - x.n[0] * 2, q[1] + t[1] * s - x.n[1] * 2];
      const tailN = x.force === 'tail' || (x.force !== 'head' && (prev ? prev.bld !== b.id && lowerThan(hOf(prev.bld)) : run.tail.open && lowerThan(beyond(nb(x.a, -1), b.id))));
      const headN = x.force === 'head' || (next ? next.bld !== b.id && lowerThan(hOf(next.bld)) : run.head.open && lowerThan(beyond(nb(x.b, 1), b.id)));
      const mid = lerp(x.a, x.b, 0.5), base = { a: [r3(x.a[0]), r3(x.a[1])] as V, b: [r3(x.b[0]), r3(x.b[1])] as V, edge: edgeOf(b.ring, x.a, x.b), normal: [r4(x.n[0]), r4(x.n[1])] as V }; // the face's outward normal (K-6 reads it: a building with a crescent wing has mass on both sides of a run's line)
      if (rs.stream && rs.end && tailN) {
        const p = place(rs.end, mid, x.n);
        push(b, { form: 'end', kit: { id: rs.end.id, scale: [1, 1, 1], yaw: p.yaw, at: p.at }, ...base }, drop); counts.ends++;
      } else if (rs.stream && rs.end && headN && rs.endM) {
        const p = place(rs.endM, mid, x.n);
        push(b, { form: 'end', kit: { id: rs.endM.id, scale: [1, 1, 1], yaw: p.yaw, at: p.at }, ...base }, drop); counts.ends++; counts.endsMirror++;
      } else {
        if (rs.stream && rs.end && headN) counts.square++;
        const p = place(rs.bay, mid, x.n);
        push(b, { form: 'bay', kit: { id: rs.bay.id, scale: [1, 1, 1], yaw: p.yaw, at: p.at }, ...base }, drop); counts.bays++;
      }
      if (run.crescent) counts.crescentBays++;
    }
  }
  for (const b of blds.values()) {
    counts.buildings++; if (b.crescent) counts.crescents++;
    classes[b.cls] = (classes[b.cls] ?? 0) + 1;
    const street = b.ring.p.reduce((s, p, i) => s + (isStreetClass(b.ring.c[i]!) ? dist(p, b.ring.p[(i + 1) % b.ring.p.length]!) : 0), 0);
    const kit = facades.filter(f => f.building === b.id).reduce((s, f) => s + (f.form === 'corner' ? 2 * OLD_BLOCK.cell : OLD_BLOCK.bay), 0);
    metres.set(b.id, { street: r3(street), kit: r3(Math.min(street, kit)) });
  }
  return { facades, metres, counts, classes };
}

/** The last generation's log per plan (masses.ts writes it; the report CLI and the tests read it). */
const LOGS = new WeakMap<object, OldLog>();
export const setOldLog = (ctx: Ctx, log: OldLog) => { LOGS.set(ctx.plan, log); };
export const oldLogOf = (ctx: Ctx): OldLog | undefined => LOGS.get(ctx.plan);
