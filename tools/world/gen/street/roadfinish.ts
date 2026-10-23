// The road finish (brief 026/028, Sky 2026-10-20 "try all", 2026-10-22 "also thought we were changing the road to be modern based on testbed?"): what the generator lays on every
// carriageway from standards.streetscape.roads, as the 2.5D testbed drew it (civic-road-guides.js, civic-road-finish.js, civic-roads.js):
//   ribbon     a continuous lens of warm light set flush in the carriageway 0.10 m out from every kerb face, between brass hairlines (RoadRibbon_Straight_6m)
//   shoulder   the pearl band beside it, 0.36 to 0.98 m out, chrome-edged (RoadShoulder_Straight_6m; standards.streetscape.roads.shoulder)
//   pinstripe  a brass hairline in the pavement 0.5 m in from the kerb face (PaveEdge_Pinstripe_6m; roads.pinstripe)
//   lozenge    the pearl diamond on each carriageway's middle line (RoadMark_Lozenge), 8.5 m apart: no painted lane line
//   seam       a chrome joint across the enamel (RoadMark_Seam), 9 m apart: the road reads as laid panels
// Where the kerb faces are: every plot edge on a carriageway edge (capstone.kerbRuns), the circus's two kerbs, every median's edges, and the faces no plot edge runs on (standards.streetscape.roads.kerbs, Datum
// 2026-10-22): the circus mouths and foot-way mouths the plan lists, a kerbed civic bay's back and ends (read from the block; its mouth cuts the plot run), a road's edges where they run in a beyond block or
// along the quay, and each side of a reservation. Rules over plan data only (plot edges via capstone.kerbRuns, road lines, medians, the circus ring, crossing strips, drives, tram platforms and tracks); nothing is a coordinate; deterministic (no random
// choice at all: the phase of a lozenge comes from its run). Anything that does not fit is dropped, never squeezed (SF-6), and counted in the log. Opt-in key by key: a plan without
// standards.streetscape.roads, or a kit without the ribbon pieces adopted, gets nothing (a pinned plan replays as it was made).
//
// Frame of every piece (tools/decokit/road_ribbon.py): ribbon, shoulder and pinstripe lie along +X, y 0 = the lens centre line = `offset` (0.10) out from the kerb face, the carriageway on +Y; the
// export's `at` is that pivot, `yaw` = atan2(n.x, -n.y) (plan yaw θ turns +X to (cos θ, sin θ); Blender +Y is Unity -Z, i.e. (sin θ, -cos θ) in the plan), `scale` [along, 1, 1] stretches a
// 6 m piece to fit its span (every section is flat colour or tiles in metres, so stretching along is free). Seams lie along Blender +Y (across the road): scale [1, 1, k]. z = the surface the piece
// lies on (the carriageway 0, the pavement top for the pinstripe): the pieces keep their own few millimetres of lift.
import type { KitAsset } from '../../../../contracts/world/kit.schema';
import type { StreetItem } from '../../../../contracts/world/world.schema';
import { type V, dist, inPoly, lineDist, segDist } from '../../../plan/geom';
import { FLAT_FOOT, LAND, type Ctx, r3, r4 } from '../common';
import { adoptedBy } from '../kitrefs';
import { kerbRuns } from './capstone';

export type RoadFinishLog = {
  /** pieces placed by item kind and by kit id */
  counts: Record<string, number>; pieces: Record<string, number>;
  /** the ribbon: kerb runs seen, metres of kerb face, metres laid, metres cut at breaks (by what cut them) */
  kerb: { runs: number; edgeM: number; laidM: number; cutM: Record<string, number>; /** metres of kerb face by where it comes from (plot, circus, median, mouth, bay, beyond, quay, reservation) */ faceM: Record<string, number>; /** road edge not covered by any face (metres, then where) */ uncoveredM: number; uncovered: string[] };
  lozenges: { lines: number; runs: number; placed: number }; seams: { placed: number; dropped: Record<string, number> };
  /** what the generator did not do, and why (to Datum and Splice) */
  gaps: { rule: string; where: string; why: string }[];
};

type Box = [number, number, number, number];
type Blocker = { kind: string; ring: V[]; bb: Box; /** a bay's own faces lie inside its ring: the blocker is not theirs */ owner?: string };
type Family = { id: 'ribbon' | 'shoulder' | 'pinstripe'; kind: string; piece: KitAsset; /** pivot offset from the kerb face, + toward the carriageway */ at: number; /** lateral extent of its section from the kerb face (a < b) */ a: number; b: number; z: number };
type Seg = { s0: number; s1: number };
type Line = { id: string; p0: V; u: V; n: V; len: number; road: string; edge: string; plot?: boolean; /** where the face comes from (log.kerb.faceM) */ from: string; /** the bay whose face this is */ bay?: string; /** the level it lies at, else the carriageway's 0 */ z?: number; /** no pavement top beside it: no pinstripe */ noPin?: boolean; /** the level of the pavement top beside it, else ctx.pav */ padZ?: number; /** cut inside the circus ring (a reservation edge runs on into it) */ circusCut?: boolean };

const norm2 = (a: V): V => { const l = Math.hypot(a[0], a[1]) || 1; return [a[0] / l, a[1] / l]; };
const wrap = (y: number) => r4(Math.atan2(Math.sin(y), Math.cos(y)));
const cross = (u: V, v: V) => u[0] * v[1] - u[1] * v[0];
const bboxOf = (r: V[]): Box => [Math.min(...r.map(p => p[0])), Math.max(...r.map(p => p[0])), Math.min(...r.map(p => p[1])), Math.max(...r.map(p => p[1]))];
const inBox = (p: V, b: Box, g: number) => p[0] >= b[0] - g && p[0] <= b[1] + g && p[1] >= b[2] - g && p[1] <= b[3] + g;
/** True if p is inside `ring` or within `grow` of its edge. */
function nearRing(p: V, ring: V[], grow: number): boolean {
  if (inPoly(p, ring)) return true;
  for (let i = 0; i < ring.length; i++) if (segDist(p, ring[i]!, ring[(i + 1) % ring.length]!) <= grow) return true;
  return false;
}

/** A grid of polyline segments (cell 16 m) so "is p within r of any line" does not walk every segment of every road. */
class Lines {
  private cell = 16; private g = new Map<string, { a: V; b: V; w: number; id: string }[]>();
  /** `pad`: how far past w a later query may reach (it only widens the cells the segment is filed in). */
  add(id: string, line: V[], w: number, pad = 0) {
    for (let i = 1; i < line.length; i++) {
      const a = line[i - 1]!, b = line[i]!, x0 = Math.floor((Math.min(a[0], b[0]) - w - pad) / this.cell), x1 = Math.floor((Math.max(a[0], b[0]) + w + pad) / this.cell), y0 = Math.floor((Math.min(a[1], b[1]) - w - pad) / this.cell), y1 = Math.floor((Math.max(a[1], b[1]) + w + pad) / this.cell);
      for (let x = x0; x <= x1; x++) for (let y = y0; y <= y1; y++) { const k = `${x},${y}`; (this.g.get(k) ?? this.g.set(k, []).get(k)!).push({ a, b, w, id }); }
    }
  }
  /** The id of a line whose segment passes within its own half width w + g of p (and is not `skip`). */
  within(p: V, g: number, skip = ''): string | undefined {
    for (const s of this.g.get(`${Math.floor(p[0] / this.cell)},${Math.floor(p[1] / this.cell)}`) ?? []) if (s.id !== skip && segDist(p, s.a, s.b) < s.w + g) return s.id;
    return undefined;
  }
}

/** The stretches [s0, s1] of `start..end` (m) whose samples (every `step` m) are not blocked; `cut` takes `step` m for every blocked sample under the blocker's name. A stretch starts at its first free sample, so it is never longer than the free ground. */
function spans(start: number, end: number, blockedAt: (s: number) => string | undefined, step: number, cut: Record<string, number>): Seg[] {
  const out: Seg[] = [], n = Math.ceil((end - start) / step - 1e-9);
  let first: number | undefined, last = 0;
  for (let i = 0; i <= n; i++) {
    const s = Math.min(end, start + i * step), why = blockedAt(s);
    if (!why) { if (first === undefined) first = s; last = s; continue; }
    if (first !== undefined) { out.push({ s0: first, s1: last }); first = undefined; }
    cut[why] = (cut[why] ?? 0) + step;
  }
  if (first !== undefined) out.push({ s0: first, s1: last });
  return out;
}

/** Pieces of `pieceLen` m (stretched to fit) filling [s0, s1]: [centre s, length]; nothing under `minLen`. */
function fill(s0: number, s1: number, pieceLen: number, minLen: number): { s: number; len: number }[] {
  const L = s1 - s0; if (L < minLen) return [];
  const n = Math.max(1, Math.round(L / pieceLen)), len = L / n;
  return Array.from({ length: n }, (_, k) => ({ s: s0 + (k + 0.5) * len, len }));
}

/** A polyline offset by d (left of travel positive) with mitred vertices: the middle line of a carriageway half beside a median. */
export function offsetLine(line: V[], d: number): V[] {
  return line.map((p, i) => { const a = line[Math.max(i - 1, 0)]!, b = line[Math.min(i + 1, line.length - 1)]!, u = norm2([b[0] - a[0], b[1] - a[1]]); return [p[0] - u[1] * d, p[1] + u[0] * d] as V; });
}

/** Where a pinstripe lies beside a face is the walking surface on its pavement side: a plot slab, a median or the circus island at the pavement top (ctx.pav), a foot way at FLAT_FOOT, the land under a circus band at LAND. */
export function dressRoadFinish(ctx: Ctx, o: { crossings: { strip: V[] }[]; /** tram platform rings (the world's transit.stops[].ring) */ platforms?: V[][] }): { items: StreetItem[]; log: RoadFinishLog } | undefined {
  const R = ctx.S.roads; if (!R?.ribbon || !R.breaks || !R.lozenges || !R.seams) return undefined;
  const straight = adoptedBy(R.ribbon.pieces.straight), lozenge = adoptedBy(R.lozenges.piece), seam = adoptedBy(R.seams.piece);
  const log: RoadFinishLog = { counts: {}, pieces: {}, kerb: { runs: 0, edgeM: 0, laidM: 0, cutM: {}, faceM: {}, uncoveredM: 0, uncovered: [] }, lozenges: { lines: 0, runs: 0, placed: 0 }, seams: { placed: 0, dropped: {} }, gaps: [] };
  const gap = (rule: string, where: string, why: string) => { log.gaps.push({ rule, where, why }); };
  if (!straight || !lozenge || !seam) { gap('RF', 'kit', `${R.ribbon.pieces.straight}, ${R.lozenges.piece}, ${R.seams.piece}: one is not adopted in the kit (candidate until Sky agrees the road)`); return { items: [], log }; }

  const stop: number = R.breaks.stopShort, off: number = R.ribbon.offset, half: number = R.ribbon.section / 2;
  const fams: Family[] = [{ id: 'ribbon', kind: 'road-ribbon', piece: straight, at: off, a: off - half, b: off + half, z: 0 }];
  const sh = R.shoulder, ps = R.pinstripe, shP = sh && adoptedBy(sh.piece), psP = ps && adoptedBy(ps.piece);
  if (sh && shP) fams.push({ id: 'shoulder', kind: 'road-shoulder', piece: shP, at: off, a: sh.from, b: sh.to + sh.chrome, z: 0 });
  else if (sh) gap('RF', 'shoulder', `${sh.piece} is not adopted: the pearl band beside the ribbon is not laid`);
  if (ps && psP) fams.push({ id: 'pinstripe', kind: 'road-pinstripe', piece: psP, at: -ps.offset, a: -ps.offset - ps.width / 2, b: -ps.offset + ps.width / 2, z: ctx.pav });
  else if (ps) gap('RF', 'pinstripe', `${ps.piece} is not adopted: the pavement pinstripe is not laid`);

  const items: StreetItem[] = [], n: Record<string, number> = {};
  const put = (kind: string, a: KitAsset, at: V, yaw: number, z: number, scale: [number, number, number], edge: string) => {
    items.push({ id: `street/roadfinish/${kind}-${(n[kind] = (n[kind] ?? 0) + 1) - 1}`, kind, at: [r3(at[0]), r3(at[1])], yaw: wrap(yaw), z: r3(z), size: [r3(a.size[0] * scale[0]), r3(a.size[2] * scale[2]), r3(a.size[1] * scale[1])], surface: 'trim', edge, sight: 'open', source: 'kit', kit: { id: a.id, scale } });
    log.counts[kind] = (log.counts[kind] ?? 0) + 1; log.pieces[a.id] = (log.pieces[a.id] ?? 0) + 1;
  };

  // ---- breaks: what everything stops short of (stopShort): a crossing's strip, a drive, a tram platform, a kerbed bay's mouth, a tram track's swept strip
  const blockers: Blocker[] = [];
  const addB = (kind: string, ring: V[], owner?: string) => { if (ring.length >= 3) blockers.push({ kind, ring, bb: bboxOf(ring), owner }); };
  for (const c of o.crossings) addB('crossing', c.strip);
  for (const d of ctx.civ.drives ?? []) addB('drive', d.poly as V[]);
  for (const p of o.platforms ?? []) addB('platform', p);
  /** A track's swept strip: 1.825 m either side of a track centre line (streetscape.sections.primary.tram.swept 2.375..9.625 about the two tracks 3.6 m apart); a double path is two tracks 1.8 m either side of its line. */
  const tracks = new Lines();
  ctx.d.tram.forEach((t, i) => tracks.add(`track/${t.id}/${i}`, t.samples.map(x => x.p), (t.double ? 1.8 : 0) + 1.825));
  const roadLines = new Lines();
  for (const r of ctx.d.roads) if (!r.gyratory) roadLines.add(r.id, r.line, r.w / 2);
  /** What p is too near to (g m of a crossing, a drive, a platform, a bay's mouth; a track's swept strip). A line `u` along a kerb meets a track only at its ends: it stops g short of the strip along its own direction, never for running beside it. `own`: the bay whose face this is. */
  const hit = (p: V, g: number, u?: V, own?: string): string | undefined => {
    for (const b of blockers) if (!(b.owner && b.owner === own) && inBox(p, b.bb, g) && nearRing(p, b.ring, g)) return b.kind;
    if (!u) return tracks.within(p, g) ? 'track' : undefined;
    return tracks.within(p, 0) || tracks.within([p[0] + u[0] * g, p[1] + u[1] * g], 0) || tracks.within([p[0] - u[0] * g, p[1] - u[1] * g], 0) ? 'track' : undefined;
  };

  // ---- 1. ribbon, shoulder and pinstripe along every kerb face: the plots' kerbed runs, the circus's two kerbs, the medians' edges, and the faces no plot edge runs on (roads.kerbs)
  const sheet = ctx.plan.sheet, zone: Box = [sheet.x[0], sheet.x[1], sheet.y[0], sheet.y[1]];
  const classes = R.ribbon.classes as string[];
  const runs = kerbRuns(ctx, zone).filter(r => classes.includes(r.cls));
  const key = (p: V) => `${Math.round(p[0] * 20)},${Math.round(p[1] * 20)}`;
  const endAt = new Map<string, typeof runs>();
  for (const r of runs) for (const p of [r.a, r.b]) { const k = `${r.plot}|${key(p)}`; (endAt.get(k) ?? endAt.set(k, []).get(k)!).push(r); }
  type Run = (typeof runs)[number];
  /** How far past (+) or short of (-) its vertex a run's line of family f ends at a right-angled corner of its plot: the through run (x-dominant) runs on to the other's outer edge and the other stops at
   *  the through run's inner edge, so the two sections meet as an L with nothing doubled; at a reflex corner the reverse (each trimmed). */
  const ext = (r: Run, atB: boolean, f: Family): number => {
    if (!(atB ? r.trimB : r.trimA)) return 0;
    const v = atB ? r.b : r.a, other = (endAt.get(`${r.plot}|${key(v)}`) ?? []).find(q => q !== r); if (!other) return 0;
    const convex = (atB ? cross(r.u, other.u) : cross(other.u, r.u)) > 0, through = Math.abs(r.u[0]) > Math.abs(r.u[1]);
    return through ? (convex ? f.b : -f.a) : (convex ? f.a : -f.b);
  };
  const lines: Line[] = runs.map((r, i): Line => ({ id: `${r.plot}#${i}`, p0: r.a, u: r.u, n: r.n, len: r.len, road: r.road, edge: `roadfinish/${r.road}`, plot: true, from: 'plot' }));
  const runOf = new Map<string, Run>(runs.map((r, i) => [`${r.plot}#${i}`, r]));
  const circus = ctx.plan.circus;
  const face = (id: string, from: string, road: string, a: V, b: V, toward: V, more: Partial<Line> = {}) => {
    const len = dist(a, b); if (len < 0.4) return;
    lines.push({ id, p0: a, u: norm2([b[0] - a[0], b[1] - a[1]]), n: norm2(toward), len, road, edge: `roadfinish/${id}`, from, ...more });
  };
  // the circus: the island kerb (the carriageway outside it) and the outer kerb (the carriageway inside it), as 2 m chords of the lens circle (a sagitta under 0.02 m)
  const arcLines = (rK: number, inward: boolean, id: string) => {
    const nSeg = Math.ceil((2 * Math.PI * rK) / 2);
    for (let k = 0; k < nSeg; k++) {
      const a0 = (2 * Math.PI * k) / nSeg, a1 = (2 * Math.PI * (k + 1)) / nSeg, p: V = [circus.c[0] + rK * Math.cos(a0), circus.c[1] + rK * Math.sin(a0)], q: V = [circus.c[0] + rK * Math.cos(a1), circus.c[1] + rK * Math.sin(a1)];
      const mid = (a0 + a1) / 2, radial: V = [Math.cos(mid), Math.sin(mid)];
      face(`${id}/${k}`, 'circus', 'crown-circus', p, q, inward ? [-radial[0], -radial[1]] : radial, { edge: `roadfinish/${id}`, padZ: inward ? LAND : ctx.pav });
    }
  };
  arcLines(circus.ring[0], false, 'circus-island'); arcLines(circus.ring[1], true, 'circus-outer');
  // the medians (the reservation between a road's two carriageways): every edge of each zone, the carriageway outside it; its host road is the one it lies in
  ctx.d.zones.filter(z => z.kind === 'median' && z.poly).forEach((z, zi) => {
    const P = z.poly!, c: V = [P.reduce((s, p) => s + p[0], 0) / P.length, P.reduce((s, p) => s + p[1], 0) / P.length];
    const host = ctx.d.roads.filter(r => !r.gyratory && r.median).map(r => ({ id: r.id, d: Math.min(...r.line.map(p => dist(p, c))) })).sort((a, b) => a.d - b.d)[0]?.id ?? 'median';
    P.forEach((p, i) => {
      const q = P[(i + 1) % P.length]!, L = dist(p, q); if (L < 1) return;
      const u = norm2([q[0] - p[0], q[1] - p[1]]); let nn: V = [u[1], -u[0]];
      if ((c[0] - p[0]) * nn[0] + (c[1] - p[1]) * nn[1] > 0) nn = [-nn[0], -nn[1]]; // away from the zone's centre: into the carriageway
      face(`median/${zi}/${i}`, 'median', host, p, q, nn, { edge: `roadfinish/median/${zi}` });
    });
  });
  // the faces with no plot edge (roads.kerbs): the plan lists the circus and foot-way mouths; a kerbed bay is read from the block; beyond, quay and reservation are read from the plan's own geometry
  const K = R.kerbs ?? {};
  for (const f of K.circusMouths?.faces ?? []) face(f.id, 'mouth', f.road, f.a, f.b, f.toward, { padZ: LAND });
  for (const f of K.footMouths?.faces ?? []) face(f.id, 'mouth', f.road, f.a, f.b, f.toward, { padZ: FLAT_FOOT });
  for (const kb of K.bays?.kerbed ?? [] as { bay: string; road: string }[]) {
    const bay = ctx.civ.bays.find(b => b.id === kb.bay), road = ctx.d.roads.find(r => r.id === kb.road);
    if (!bay || !road) { gap('RF-kerb', `bay ${kb.bay}`, `the block has no bay ${kb.bay}, or the plan no road ${kb.road}`); continue; }
    const P = bay.poly as V[], c: V = [P.reduce((s, p) => s + p[0], 0) / P.length, P.reduce((s, p) => s + p[1], 0) / P.length];
    const onEdge = (q: V) => Math.abs(lineDist(q, road.line) - road.w / 2) <= 0.1;
    const mouth = P.findIndex((q, i) => onEdge(q) && onEdge(P[(i + 1) % P.length]!));
    if (mouth < 0) { gap('RF-kerb', `bay ${kb.bay}`, `no edge of the bay lies on ${kb.road}'s carriageway edge: it has no mouth to cut`); continue; }
    addB('bay', P, kb.bay);
    P.forEach((q, i) => {
      if (i === mouth) return;
      const r = P[(i + 1) % P.length]!, u = norm2([r[0] - q[0], r[1] - q[1]]); let nn: V = [u[1], -u[0]];
      if ((c[0] - q[0]) * nn[0] + (c[1] - q[1]) * nn[1] < 0) nn = [-nn[0], -nn[1]]; // into the bay
      face(`bay/${kb.bay}/${i}`, 'bay', kb.road, q, r, nn, { bay: kb.bay, z: FLAT_FOOT });
    });
  }
  /** The faces so far, in a grid: a road edge already faced is not faced twice. */
  const faced = new Lines();
  const reg = (L: Line) => faced.add(L.id, [L.p0, [L.p0[0] + L.u[0] * L.len, L.p0[1] + L.u[1] * L.len]], 0, 0.4);
  lines.forEach(reg);
  /** Simplified offset polyline: collinear vertices go, so a straight road is one run, not a run per 2 m sample. */
  const merge = (pts: V[]): V[] => {
    const out: V[] = [pts[0]!];
    for (let i = 1; i < pts.length - 1; i++) { const a = out[out.length - 1]!, b = pts[i]!, c = pts[i + 1]!, u = norm2([b[0] - a[0], b[1] - a[1]]), v = norm2([c[0] - b[0], c[1] - b[1]]); if (Math.abs(cross(u, v)) > 1e-3 || u[0] * v[0] + u[1] * v[1] < 0) out.push(b); }
    out.push(pts[pts.length - 1]!); return out;
  };
  /** Faces along one offset line of a road (`d` left of travel +): where `test(p, n)` holds, `n` into the carriageway (toward the road line) or, for `away`, out of it (a reservation's edges). */
  const edgeFaces = (road: (typeof ctx.d.roads)[number], side: 1 | -1, d: number, from: string, test: (p: V, n: V) => boolean, more: Partial<Line>, away = false) => {
    const pts = merge(offsetLine(road.line, side * d));
    pts.slice(1).forEach((b, i) => {
      const a = pts[i]!, len = dist(a, b); if (len < 0.4) return;
      const u = norm2([b[0] - a[0], b[1] - a[1]]), sg = away ? -side : side, nn: V = [u[1] * sg, -u[0] * sg];
      spans(0, len, s => (test([a[0] + u[0] * s, a[1] + u[1] * s], nn) ? undefined : 'no'), 0.2, {}).forEach((sg2, k) => {
        if (sg2.s1 - sg2.s0 < 0.4) return;
        const L0 = lines.length; face(`${road.id}/${from}/${side > 0 ? 'l' : 'r'}${i}.${k}`, from, road.id, [a[0] + u[0] * sg2.s0, a[1] + u[1] * sg2.s0], [a[0] + u[0] * sg2.s1, a[1] + u[1] * sg2.s1], nn, more); if (lines.length > L0) reg(lines[L0]!);
      });
    });
  };
  const outside = (p: V, nn: V): V => [p[0] - nn[0] * 0.5, p[1] - nn[1] * 0.5];
  for (const rd of ctx.d.roads) {
    if (rd.gyratory || !classes.includes(rd.class)) continue;
    for (const side of [1, -1] as const) {
      // a carriageway edge that runs in a beyond block or along the quay: the city beyond keeps its kerbs (no pavement top beside it: no pinstripe); the quay's top is the pavement's
      edgeFaces(rd, side, rd.w / 2, 'beyond', (p, nn) => ctx.plan.beyond.some(b => inPoly(outside(p, nn), b)) && !faced.within(p, 0.25), { noPin: true });
      edgeFaces(rd, side, rd.w / 2, 'quay', (p, nn) => ctx.plan.quay.some(q => inPoly(outside(p, nn), q)) && !faced.within(p, 0.25), { padZ: ctx.pav });
      // a reservation's kerb each side (the lawn track between: no pinstripe on lawn)
      const res = (rd as { reservation?: number }).reservation;
      if (res) edgeFaces(rd, side, res / 2, 'reservation', () => true, { noPin: true, circusCut: true }, true);
    }
  }
  // coverage: every carriageway edge (the outer pair, the median's or reservation's) has a face within 0.35 m, or lies in another road's carriageway (a junction) or the circus
  const cov = new Lines(); lines.forEach(L => cov.add(L.id, [L.p0, [L.p0[0] + L.u[0] * L.len, L.p0[1] + L.u[1] * L.len]], 0, 0.5));
  const miss = new Map<string, V[]>();
  for (const rd of ctx.d.roads) {
    if (rd.gyratory || !classes.includes(rd.class)) continue;
    const res = (rd as { reservation?: number }).reservation;
    for (const d of [rd.w / 2, ...(rd.median ? [rd.median / 2] : []), ...(res ? [res / 2] : [])]) for (const side of [1, -1] as const) {
      const pts = offsetLine(rd.line, side * d);
      for (let i = 0; i < pts.length - 1; i++) for (const t of [0, 0.5]) {
        const p: V = [pts[i]![0] + (pts[i + 1]![0] - pts[i]![0]) * t, pts[i]![1] + (pts[i + 1]![1] - pts[i]![1]) * t];
        if (Math.abs(lineDist(p, rd.line) - d) > 0.15) continue; // the offset line's mitre at a bend lies nearer the road than its edge: not an edge
        if (roadLines.within(p, 0.5, rd.id) || Math.hypot(p[0] - circus.c[0], p[1] - circus.c[1]) < circus.ring[1] + 0.5 || cov.within(p, 0.35)) continue;
        const k = `${rd.id} ${side > 0 ? 'left' : 'right'} ${d}`; (miss.get(k) ?? miss.set(k, []).get(k)!).push(p); log.kerb.uncoveredM += 1;
      }
    }
  }
  for (const [k, ps] of miss) log.kerb.uncovered.push(`${k}: ${ps.length} m from ${ps[0]!.map(r3).join(',')} to ${ps[ps.length - 1]!.map(r3).join(',')}`);
  if (log.kerb.uncoveredM) gap('RF-kerb', `${miss.size} road edges`, `${log.kerb.uncoveredM} m of carriageway edge has no kerb face the ribbon can run on (log.kerb.uncovered)`);

  const okAt = new Map<string, Map<number, boolean>>();
  for (const f of fams) {
    for (const L of lines) {
      if (f.id === 'pinstripe' && L.noPin) continue;
      const run = runOf.get(L.id), start = run ? -ext(run, false, f) : 0, end = run ? L.len + ext(run, true, f) : L.len;
      if (end - start < 0.4) continue;
      const pos = (s: number): V => [L.p0[0] + L.u[0] * s + L.n[0] * f.at, L.p0[1] + L.u[1] * s + L.n[1] * f.at];
      const cut: Record<string, number> = {}, road = ctx.d.roads.find(q => q.id === L.road);
      const kerbed = (s: number): boolean => { // a plot edge runs on past a road that ends: there is no kerb face (as capstone.kerbs: the edge must lie on the road's carriageway edge)
        const k = Math.round(s / 0.2), c = (okAt.get(L.id) ?? okAt.set(L.id, new Map()).get(L.id)!);
        let v = c.get(k); if (v === undefined) { const q: V = [L.p0[0] + L.u[0] * k * 0.2, L.p0[1] + L.u[1] * k * 0.2]; v = !road || Math.abs(lineDist(q, road.line) - road.w / 2) <= 0.25; c.set(k, v); }
        return v;
      };
      const segs = spans(start, end, s => {
        const p = pos(s), b = hit(p, stop, L.u, L.bay); if (b) return b;
        if (run) return kerbed(s) || s < 0 || s > L.len ? undefined : 'no-kerb'; // the corner extensions past the plot's vertex lie on the other road's kerb
        if (L.circusCut && Math.hypot(p[0] - circus.c[0], p[1] - circus.c[1]) < circus.ring[1] + stop) return 'circus';
        return roadLines.within(p, stop, L.road) ? 'junction' : undefined; // a median's or the circus's line is cut where another road's carriageway runs over it
      }, 0.2, cut);
      if (f.id === 'ribbon') { log.kerb.edgeM += end - start; log.kerb.faceM[L.from] = r3((log.kerb.faceM[L.from] ?? 0) + end - start); if (run) log.kerb.runs++; for (const [k, v] of Object.entries(cut)) log.kerb.cutM[k] = (log.kerb.cutM[k] ?? 0) + v; }
      const yaw = Math.atan2(L.n[0], -L.n[1]), pieceLen = f.piece.size[0], z = f.id === 'pinstripe' ? (L.padZ ?? f.z) : (L.z ?? f.z);
      for (const sg of segs) for (const pc of fill(sg.s0, sg.s1, pieceLen, 0.5)) {
        put(f.kind, f.piece, pos(pc.s), yaw, z, [r3(pc.len / pieceLen), 1, 1], L.edge);
        if (f.id === 'ribbon') log.kerb.laidM += pc.len;
      }
    }
  }
  if (R.ribbon.pieces.arcs) gap('RF-arc', 'the kerb returns', 'the arc pieces (RoadRibbon_Arc90_R4/R8, RoadShoulder_Arc90_R4/R8) are adopted but not placed: the plot slabs have square corners (grammar §1), so every return is the mitred L of two straight runs (ribbon.corners)');

  // ---- 2. lozenges and seams on each carriageway's middle line
  const lozenges = R.lozenges, seams = R.seams, hl = lozenges.length / 2;
  const clear = sh && shP ? sh.to + sh.chrome : off + half + stop; // from a kerb face to where a seam starts: the shoulder's outer chrome (1.015 m), else the ribbon's brass and stopShort
  type Cw = { road: string; line: V[]; half: number };
  const carriageways: Cw[] = [];
  for (const r of ctx.d.roads) {
    if (r.gyratory || !classes.includes(r.class)) continue;
    const hw = r.w / 2, m = ((r.median ?? 0) || (r as { reservation?: number }).reservation || 0) / 2; // a median's or a reservation's two carriageways each take their own middle line, never the road line (which lies in it)
    if (!m) carriageways.push({ road: r.id, line: r.line, half: hw });
    else for (const side of [1, -1]) carriageways.push({ road: r.id, line: offsetLine(r.line, (side * (m + hw)) / 2), half: (hw - m) / 2 });
  }
  for (const cw of carriageways) {
    log.lozenges.lines++;
    const line = cw.line, cum: number[] = [0]; for (let i = 1; i < line.length; i++) cum.push(cum[i - 1]! + dist(line[i - 1]!, line[i]!));
    const total = cum[cum.length - 1]!; if (total < 4) continue;
    const at = (s: number): { p: V; u: V } => {
      let i = 1; while (i < line.length - 1 && cum[i]! < s) i++;
      const a = line[i - 1]!, b = line[i]!, t = (s - cum[i - 1]!) / (cum[i]! - cum[i - 1]! || 1);
      return { p: [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t], u: norm2([b[0] - a[0], b[1] - a[1]]) };
    };
    const blocked = (p: V, g: number): string | undefined => {
      if (roadLines.within(p, g, cw.road)) return 'junction';
      if (Math.hypot(p[0] - circus.c[0], p[1] - circus.c[1]) < circus.ring[1] + g) return 'circus';
      return hit(p, g);
    };
    const cut: Record<string, number> = {};
    for (const rn of spans(0, total, s => blocked(at(s).p, stop), 0.25, cut)) {
      log.lozenges.runs++;
      const mid = (rn.s0 + rn.s1) / 2, L = rn.s1 - rn.s0, lz: number[] = [];
      // one lozenge at the run's middle, then every spacing both ways while its tips stay inside the run
      for (let k = 0; k * lozenges.spacing <= L / 2; k++) for (const sg of k ? [1, -1] : [1]) {
        const s = mid + sg * k * lozenges.spacing; if (s - hl < rn.s0 - 1e-9 || s + hl > rn.s1 + 1e-9) continue;
        const { p, u } = at(s); lz.push(s);
        put('road-lozenge', lozenge, p, Math.atan2(u[1], u[0]), 0, [1, 1, 1], `roadfinish/lozenge/${cw.road}`); log.lozenges.placed++;
      }
      // seams: first half a spacing from the middle, then every spacing; dropped near a lozenge, where the clear span is not clear, or on a carriageway a tram runs in
      const half2 = cw.half - clear;
      for (let k = 0; seams.spacing / 2 + k * seams.spacing <= L / 2; k++) for (const sg of [1, -1]) {
        const s = mid + sg * (seams.spacing / 2 + k * seams.spacing); if (s < rn.s0 || s > rn.s1) continue;
        const drop = (why: string) => { log.seams.dropped[why] = (log.seams.dropped[why] ?? 0) + 1; };
        if (half2 < 0.5) { drop('narrow'); continue; }
        if (lz.some(q => Math.abs(q - s) < hl + stop)) { drop('lozenge'); continue; }
        const { p, u } = at(s), nrm: V = [-u[1], u[0]];
        let why: string | undefined;
        for (const f of [-1, -0.5, 0, 0.5, 1]) { why = blocked([p[0] + nrm[0] * half2 * f, p[1] + nrm[1] * half2 * f], stop); if (why) break; }
        if (why) { drop(why); continue; }
        const nPc = Math.ceil((2 * half2) / seams.length - 1e-9), each = (2 * half2) / nPc;
        for (let j = 0; j < nPc; j++) {
          const t = -half2 + (j + 0.5) * each;
          put('road-seam', seam, [p[0] + nrm[0] * t, p[1] + nrm[1] * t], Math.atan2(nrm[0], -nrm[1]), 0, [1, 1, r3(each / seam.size[2])], `roadfinish/seam/${cw.road}`); log.seams.placed++;
        }
      }
    }
  }
  return { items, log };
}
