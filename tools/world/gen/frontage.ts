// Ground-floor and deck tenancies: segments along the street edges of each building line (and the megastructure facades),
// each a trade from the area's mix following standards.frontage.placement: no primary-road frontage, widths snapped to 3 m,
// the same trade never side by side, anchors on corners, a lobby on the building's quietest street, service bays only on
// access roads, the quay or under a deck. Each has an entrance, a canopy, a sign blank (shape + icon + palette key) and spill.
import type { Frontage, Prim } from '../../../contracts/world/world.schema';
import { type V, dist } from '../../plan/geom';
import { type Ctx, type Rng, r3, r4, rng, rv } from './common';
import { frontKind, idOf, kindOf } from './edges';
import { edgeN } from './geo';
import type { Bld } from './masses';
import type { Host, HostEdge } from './types';

type Trade = { w: [number, number]; depth: number; service: string; hours: [number, number] | 'service'; wants: string[]; spill: string; sign: [string, string, string]; anchor?: boolean; areas?: string[] };
const QUIET: Record<string, number> = { access: 0, park: 1, quay: 2, walk: 3, district: 4, circus: 4, mall: 5, deck: 1, concourse: 2 };
const OPEN = new Set(['party', 'court', 'none', 'primary']);

/** Spill room in front of a facade: the section's frontage zone (never more than the spill depth), none where the pavement is under minPavement. */
function spillZone(ctx: Ctx, kind: string, area = ''): number {
  const base = ctx.S.sections[kind === 'walk' || kind === 'quay' || kind === 'park' ? 'none' : kind], avenue = kind === 'access' && (ctx.S.trees.avenues.access.avenue as string[]).includes(area);
  const sec = avenue && base?.avenue ? { ...base, frontage: base.avenue.frontage } : base; // an avenue's frontage zone gives 0.5 m to the tree pit
  if (kind === 'mall') return Math.min(ctx.F.placement.spill.depth, 4);
  return sec && sec.pavement >= ctx.F.placement.spill.minPavement ? Math.min(ctx.F.placement.spill.depth, sec.frontage) : 0;
}

/** Facade runs of a building that tenancies may use: street kinds only, never a primary road; short arc edges are merged into chords. */
export function buildingHost(ctx: Ctx, b: Bld): Host | null {
  const { p, c } = b.ring, n = p.length, edges: HostEdge[] = [];
  const street = (i: number) => !OPEN.has(kindOf(c[i]!));
  for (let i = 0; i < n;) {
    if (!street(i)) { i++; continue; }
    let j = i;
    while (j + 1 < n && street(j + 1) && c[j + 1] === c[i] && dist(p[j]!, p[j + 1]!) < 8 && dist(p[i]!, p[(j + 2) % n]!) <= 30) j++;
    const a = p[i]!, e = p[(j + 1) % n]!;
    if (dist(a, e) >= 3) edges.push({ a, b: e, n: edgeN(a, e), kind: frontKind(c[i]!), street: idOf(c[i]!), spill: spillZone(ctx, frontKind(c[i]!), b.area), cornerA: street((i + n - 1) % n), cornerB: street((j + 1) % n) });
    i = j + 1;
  }
  if (!edges.length) return null;
  return { id: b.id, level: 'street', z: ctx.pav, area: b.area, edges, free: false, pavement: 0 };
}

function candidates(ctx: Ctx, h: Host, kind: string, room: number, prev: string, relax: boolean, anchorOnly: boolean): [string, number][] {
  const T = ctx.F.trades as Record<string, Trade>, mix = h.mix ?? ctx.F.mix[h.area] ?? {};
  return Object.entries(mix as Record<string, number>).filter(([t]) => {
    const x = T[t]; if (!x || t === prev || x.w[0] > room) return false;
    if (x.areas && !x.areas.includes(h.area)) return false;
    if (t === 'tram-kiosk' || t === 'skyrail-kiosk') return false; // transit kiosks belong to the stops and stations
    if (anchorOnly && (!x.anchor || x.w[1] < 18)) return false;
    return relax || x.wants.includes(kind) || (kind === 'park' && x.wants.includes('park'));
  });
}

/** The sign board stands 0.8 m off the facade line (0.6 m shopfront + 0.2): on freestanding stalls and kiosks too, whose block is deeper (the 3.2 m of the first flyby). */
export const SIGN_OFF = 0.8;
const snap = (w: number, g: number) => Math.max(g, Math.round(w / g) * g);

function prims(ctx: Ctx, h: Host, id: string, t: string, T: Trade, a: V, u: V, n: V, w: number, kind: string, spill: number, r: Rng) {
  const c: V = [(a[0] + u[0] * w / 2), (a[1] + u[1] * w / 2)], yaw = Math.atan2(u[1], u[0]), z = h.z, cl = ctx.B.groundCeiling as number;
  const free = h.free || T.service === 'freestanding', depth = free ? Math.min(T.depth, 3) : 0.6, at = (d: number, off = 0): V => [c[0] + n[0] * d + u[0] * off, c[1] + n[1] * d + u[1] * off];
  const mk = (pid: string, shape: Prim['shape'], surface: string, ctr: V, size: [number, number, number], base: number): Prim => ({ id: `${id}/${pid}`, shape, surface, c: rv(ctr), base: r3(base), size: [r3(size[0]), r3(size[1]), r3(size[2])], yaw: r4(yaw) });
  const door = t === 'hotel-lobby' || t === 'department-store' ? 4 : 2.4;
  const canopyD = Math.min(spill, 2.4) || 1.2;
  const front = mk('front', 'box', 'frontage', at(depth / 2), [w, depth, cl], z);
  const entrance = mk('door', 'box', 'trim', at(depth + 0.1), [door, 0.2, 2.4], z);
  const canopy = mk('canopy', 'box', 'awning', at(depth + canopyD / 2), [w, canopyD, 0.25], z + cl - 0.8);
  const sw = Math.min(w * 0.75, 9), sign = { ...mk('sign', 'box', 'sign', at(SIGN_OFF), [sw, 0.3, 1.2], z + cl + 0.3), motif: T.sign[0], icon: T.sign[1], palette: T.sign[2] };
  let service: Prim | undefined;
  const bays = /roller|bays/.test(T.service) && (['access', 'quay'].includes(kind) || h.level === 'street' && h.id.startsWith('mega/'));
  if (bays && w >= 4.5 + 3) service = mk('bay', 'box', 'trim', at(depth + 0.1, -(w / 2 - 2.8)), [4.5, 0.2, 4.6], z);
  void r;
  return { front, entrance, canopy, sign, service };
}

export function generateFrontages(ctx: Ctx, hosts: Host[]): Frontage[] {
  const T = ctx.F.trades as Record<string, Trade>, out: Frontage[] = [];
  const snapG = ctx.F.placement.widthSnap as number;
  for (const h of hosts) {
    const r = rng(`frontage/${h.id}`), lobbyEdge = h.level === 'street' && !h.free && h.id.startsWith('bld/')
      ? h.edges.reduce((bi, e, i) => {
        const ok = (x: HostEdge) => !T.lobby || T.lobby.wants.includes(x.kind); // the lobby stands only on a street kind it wants (F-2)
        if (!ok(e)) return bi; if (bi < 0 || !ok(h.edges[bi]!)) return i;
        return (QUIET[e.kind] ?? 9) < (QUIET[h.edges[bi]!.kind] ?? 9) || ((QUIET[e.kind] ?? 9) === (QUIET[h.edges[bi]!.kind] ?? 9) && dist(e.a, e.b) > dist(h.edges[bi]!.a, h.edges[bi]!.b)) ? i : bi;
      }, -1) : -1;
    h.edges.forEach((e, ei) => {
      const L = dist(e.a, e.b), u: V = [(e.b[0] - e.a[0]) / L, (e.b[1] - e.a[1]) / L], segs: { t: string; w: number; anchor: boolean; lobby: boolean }[] = [];
      let t0 = 0, t1 = L, prev = '';
      const place = (t: string, w: number, anchor = false, lobby = false) => ({ t, w, anchor, lobby });
      const pickW = (t: string, room: number) => { const x = T[t]!; return Math.min(room, snap(x.w[0] + r.f() * (Math.min(x.w[1], room) - x.w[0]), snapG)); };
      const startSegs: typeof segs = [], endSegs: typeof segs = [];
      const tryPick = (room: number, anchorOnly: boolean, side: typeof segs) => {
        let cs = candidates(ctx, h, e.kind, room, prev, false, anchorOnly);
        if (!cs.length && anchorOnly && L >= 12) { const fb = ['diner', 'hotel-lobby'].filter(t => T[t]!.w[0] <= room && t !== prev); if (fb.length) cs = fb.map(t => [t, 1] as [string, number]); }
        if (!cs.length) cs = candidates(ctx, h, e.kind, room, prev, true, anchorOnly);
        if (!cs.length) return false;
        const [t] = r.weighted(cs, c => c[1]), w = pickW(t, room);
        side.push(place(t, w, anchorOnly)); prev = t; return true;
      };
      if (lobbyEdge === ei && L >= 6 && T.lobby) { const w = Math.min(snap(6 + r.f() * 3, snapG), L); startSegs.push(place('lobby', w, false, true)); t0 += w; prev = 'lobby'; }
      if (e.cornerA && L - t0 >= 12 && !startSegs.length && tryPick(L - t0, true, startSegs)) t0 += startSegs[0]!.w;
      if (e.cornerB && t1 - t0 >= 12) { const before = prev; prev = ''; if (tryPick(t1 - t0 - 0, true, endSegs)) t1 -= endSegs[0]!.w; prev = before; }
      let guard = 0;
      while (t1 - t0 >= 3 && guard++ < 60) {
        const room = t1 - t0, last = prev;
        if (!tryPick(room, false, segs)) break;
        const s = segs[segs.length - 1]!; if (room - s.w < 3 && room - s.w > 0 && s.w + (room - s.w) <= T[s.t]!.w[1]) s.w = room;
        t0 += s.w; void last;
      }
      // the corner tenancy at b must differ from the last middle one (placement.repeat): swap the middle one for another trade that fits its width
      const lastMid = segs[segs.length - 1], endT = endSegs[0]?.t;
      if (lastMid && endT && lastMid.t === endT) {
        const before = segs.length > 1 ? segs[segs.length - 2]!.t : startSegs[startSegs.length - 1]?.t ?? '';
        const alt = candidates(ctx, h, e.kind, lastMid.w, before, false, false).filter(([t]) => t !== endT && T[t]!.w[0] <= lastMid.w && lastMid.w <= T[t]!.w[1]);
        if (alt.length) lastMid.t = r.weighted(alt, q => q[1])[0];
      }
      const ordered = [...startSegs, ...segs, ...endSegs.reverse()];
      // lay out from a: start segments, the middle, the end segments flush with b
      let at = 0; const mid = ordered.length;
      ordered.forEach((s, k) => {
        const isEnd = k >= startSegs.length + segs.length, w = s.w, pos = isEnd ? L - ordered.slice(k).reduce((n, x) => n + x.w, 0) : at, a: V = [e.a[0] + u[0] * pos, e.a[1] + u[1] * pos];
        if (!isEnd) at = pos + w;
        const x = T[s.t]!, spill = x.spill === 'none' ? 0 : Math.min(ctx.F.placement.spill.depth, e.spill ?? (h.level === 'street' && !h.free && ['district', 'access'].includes(e.kind) ? spillZone(ctx, e.kind, h.area) : h.pavement)), id = `${h.id}/e${ei}-${k}`;
        const g = prims(ctx, h, id, s.t, x, a, u, e.n, w, e.kind, spill, r);
        out.push({
          id, host: h.id, level: h.level, z: h.z, trade: s.t, area: h.area, street: e.street, a: rv(a), b: rv([a[0] + u[0] * w, a[1] + u[1] * w]), w: r3(w), normal: [r3(e.n[0]), r3(e.n[1])], yaw: r4(Math.atan2(u[1], u[0])),
          hours: x.hours, staffed: true, spill: { kind: x.spill, depth: spill }, lobby: s.lobby, anchor: s.anchor, front: g.front, entrance: g.entrance, canopy: g.canopy, sign: g.sign, ...(g.service ? { service: g.service } : {}),
        });
      });
      void mid;
    });
  }
  return out;
}
