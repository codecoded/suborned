// Transit kits and watchers: a tram stop's shelter, benches, ticket machine, timetable column, bin and bollards; the
// kiosks and crossings round it; each sky rail station's entrances, kiosks and clocks; and the cameras and robots at the
// sites standards.streetscape.watchers lists (stations, tram stops, gates, the circus, the square).
import type { Connector, Mass } from '../../../../contracts/world/world.schema';
import { type V, dist, lineDist, norm, sub } from '../../../plan/geom';
import type { Ctx } from '../common';
import { kindOf, plotRing } from '../edges';
import { axisClears } from '../connectors';
import { type Rect, bbox, edgeN, inRect } from '../geo';
import type { DeckRec } from '../types';
import type { Layer } from './kit';

type Seg = { a: V; b: V; n: V; kind: string };
function roadEdges(ctx: Ctx): Seg[] {
  const out: Seg[] = [];
  for (const pl of ctx.d.plots) {
    if (pl.civic) continue;
    const r = plotRing(ctx, pl);
    r.p.forEach((a, i) => { const b = r.p[(i + 1) % r.p.length]!, k = kindOf(r.c[i]!); if (['primary', 'district', 'access'].includes(k)) out.push({ a, b, n: edgeN(a, b), kind: k }); });
  }
  return out;
}
/** Nearest point on a road edge's kerb zone to p, with the edge's direction; null beyond `max` metres. */
function kerbNear(segs: Seg[], p: V, max: number, d = 0.8) {
  let best: { d: number; q: V; yaw: number; kind: string } | null = null;
  for (const s of segs) {
    const ab = sub(s.b, s.a), l2 = ab[0] * ab[0] + ab[1] * ab[1], t = Math.max(0.1, Math.min(0.9, ((p[0] - s.a[0]) * ab[0] + (p[1] - s.a[1]) * ab[1]) / l2)), q: V = [s.a[0] + ab[0] * t - s.n[0] * d, s.a[1] + ab[1] * t - s.n[1] * d], dd = dist(p, q);
    if (dd < max && (!best || dd < best.d)) best = { d: dd, q, yaw: Math.atan2(ab[1], ab[0]), kind: s.kind };
  }
  return best;
}

export function dressTransit(ctx: Ctx, L: Layer, stops: Mass[], stations: Mass[], connectors: Connector[], decks: DeckRec[]): Rect[] {
  const S = ctx.S, z = ctx.pav, tstd = ctx.plan.standards.tram, segs = roadEdges(ctx), keep: Rect[] = [];
  // 1. tram stops
  const tracks = ctx.d.tram.map(p => p.samples.map(s => s.p));
  const done = new Set<string>();
  for (const m of stops) {
    const [, line, stop] = m.id.split('/') as [string, string, string], bb = bbox(m.ring), c: V = [(bb[0] + bb[1]) / 2, (bb[2] + bb[3]) / 2];
    const alongX = bb[1] - bb[0] > bb[3] - bb[2], u: V = alongX ? [1, 0] : [0, 1], w = alongX ? bb[3] - bb[2] : bb[1] - bb[0], len = alongX ? bb[1] - bb[0] : bb[3] - bb[2];
    const tr = tracks.reduce((best, l) => (lineDist(c, l) < lineDist(c, best) ? l : best)), nearest = tr.reduce((b, q) => (dist(q, c) < dist(b, c) ? q : b));
    const out = norm(sub(c, nearest)), yaw = Math.atan2(u[1], u[0]), top = 0.3, id = m.id;
    const at = (s: number, o: number): V => [c[0] + u[0] * s + out[0] * o, c[1] + u[1] * s + out[1] * o];
    L.add('tram-shelter', at(0, w / 2 - 0.75), yaw, top, id, { force: true });
    for (const s of [-2, 2]) L.add('bench', at(s, w / 2 - 0.55), yaw, top, id, { force: true });
    L.add('ticket-machine', at(len / 2 - 1.2, 0), yaw, top, id, { size: [0.8, 0.6, 1.8], force: true });
    L.add('timetable-column', at(-len / 2 + 1.2, 0), yaw, top, id, { size: [0.4, 0.4, 2.6], variant: 'coral-dial', force: true });
    L.add('bin', at(len / 2 - 3, w / 2 - 0.4), 0, top, id, { force: true });
    for (const s of [-(len / 2 + tstd.platform * 0 + 3.6 + 0.6), len / 2 + 3.6 + 0.6]) L.add('bollard', at(s, 0), 0, z, id, { size: S.bollards.size, r: 0.2, force: true });
    L.add('pole-camera', at(-len / 2 + 0.6, w / 2 - 0.2), yaw, top, id, { size: [0.25, 0.25, S.watchers.types['pole-camera'].mount], variant: 'pole-camera', force: true });
    if (!done.has(`${line}/${stop}`)) { // once per stop: crossings at both platform ends, kiosks and phone booths on the pavement facing it
      done.add(`${line}/${stop}`);
      for (const s of [-(len / 2 + 5), len / 2 + 5]) L.add('crossing', at(s, 0), yaw + Math.PI / 2, z, id, { size: [S.crossings.w, 8, 0.02], r: 2, force: true });
      const k = kerbNear(segs, c, 40);
      if (k && k.kind !== 'primary') {
        const off = (s: number): V => [k.q[0] + Math.cos(k.yaw) * s, k.q[1] + Math.sin(k.yaw) * s];
        L.add('kiosk-tickets', off(0), k.yaw, z, id, { force: true, trade: 'tram-kiosk' }); L.add('newsstand', off(4), k.yaw, z, id, { force: true });
        L.add('phone-booth', off(-3), k.yaw, z, id, { force: true }); L.add('phone-booth', off(-5), k.yaw, z, id, { force: true });
        if (stop === 'circus') L.add('public-clock', off(8), 0, z, id, { force: true });
      }
    }
  }
  // 2. sky rail stations: banks (entrance canopy, newsstand, phones, crossing, camera), ticket kiosk, clock, vending robots, canopy cameras
  const towers = connectors.filter(c => c.kind === 'stairTower');
  const zc = ctx.L.list.find((l: any) => l.id === 'concourse').z as number;
  for (const t of towers) {
    const bb = bbox(t.ring), c: V = [(bb[0] + bb[1]) / 2, (bb[2] + bb[3]) / 2], out: V = [0, c[1] >= 0 ? 1 : -1], id = t.host;
    keep.push([bb[0] - 1, bb[1] + 1, bb[2] - 1, bb[3] + 1]);
    const clears = axisClears(ctx), mid = (r: Rect) => (r[0] + r[1]) / 2;
    const at = (s: number, o: number): V => { // furniture never stands in a forecourt's clear axis: it moves to the nearer side
      const p: V = [c[0] + s, c[1] + out[1] * o], a = clears.find(r => inRect(p, r, 0.5));
      return a ? [p[0] < mid(a) ? a[0] - 0.8 : a[1] + 0.8, p[1]] : p;
    };
    // the crossing first (flat, it keeps its place); then each item takes the nearest spot that keeps the walk zones, the beam and the crossing's 2 m clear
    L.add('crossing', at(0, 3.5 + 8), Math.PI / 2, z, id, { size: [S.crossings.w, 3, 0.02], r: 2, force: true });
    const near = (kind: string, s: number, o: number, yaw: number, opt: { size?: [number, number, number]; variant?: string } = {}) => {
      for (const ds of [0, 3, -3, 6, -6, 9, -9, 12, -12, 15, -15, 18, -18, 24, -24]) for (const dd of [0, -1.5, -3, 1.5, -4.5, -6, 3, 4.5, -7.5, -9]) if (L.add(kind, at(s + ds, o + dd), yaw, z, id, opt)) return true;
      return false;
    };
    near('station-entrance', 0, 3.5 + 2.5, 0);
    near('newsstand', -7, 3.5 + 1, 0); near('phone-booth', 7, 3.5 + 1, 0); near('phone-booth', 8.5, 3.5 + 1, 0);
    near('pole-camera', -4.5, 3.5 + 6, 0, { size: [0.25, 0.25, S.watchers.types['pole-camera'].mount], variant: 'pole-camera' });
    if (id.endsWith('/l')) near('civic-robot-dock', 5, 3.5 + 6, 0); // the main bank
  }
  const perStation = new Set<string>();
  for (const m of stations) {
    const bb = bbox(m.ring), c: V = [(bb[0] + bb[1]) / 2, (bb[2] + bb[3]) / 2], [, line, st] = m.id.split('/') as [string, string, string], key = `${line}/${st}`;
    for (const sgn of [-1, 1]) L.add('canopy-camera', [c[0] + sgn * ((bb[1] - bb[0]) / 2 - 1), c[1]], sgn > 0 ? Math.PI : 0, zc, m.id, { size: [0.25, 0.25, 0.3], variant: 'canopy-camera', force: true, ground: false });
    L.add('vending-robot', [c[0] - 6, c[1]], 0, zc, m.id, { ground: false, force: true });
    if (!perStation.has(key)) {
      perStation.add(key);
      L.add('kiosk-tickets', [c[0] + 3, c[1]], 0, zc, m.id, { ground: false, force: true, trade: 'skyrail-kiosk' }); L.add('public-clock', [c[0] - 3, c[1]], 0, zc, m.id, { ground: false, force: true });
    }
  }
  // skybridges: one canopy camera under the soffit at the station end of every megastructure bridge that joins a station
  for (const m of ctx.plan.megastructures ?? []) for (const b of m.bridges ?? []) {
    if (!(b as { joinsStation?: unknown }).joinsStation) continue;
    const dk = decks.find(x => x.id === `mega/${m.id}/bridge-${b.id}`); if (!dk) continue;
    const [x0, x1, y0, y1] = dk.rect, atLine = Math.abs(y0) < Math.abs(y1), p: V = [(x0 + x1) / 2, atLine ? y0 + 0.4 : y1 - 0.4];
    L.add('canopy-camera', p, atLine ? Math.PI / 2 : -Math.PI / 2, zc, dk.id, { size: [0.25, 0.25, 0.3], variant: 'canopy-camera', force: true, ground: false });
  }
  // 3. gates: one pole camera 10 m inside the node on the area side, facing out over the junction; the circus robots; the square's mast and robot
  for (const g of ctx.plan.gates) {
    const node = ctx.plan.nodes[g.node]!, road = ctx.d.roads.find(r => r.id === g.road)!, area = ctx.plan.areas.find(a => a.id === g.area)!, ac: V = [(area.rect[0] + area.rect[1]) / 2, (area.rect[2] + area.rect[3]) / 2];
    const nearest = road.line.reduce((b, q) => (dist(q, node) < dist(b, node) ? q : b)), idx = road.line.indexOf(nearest), nb = road.line[Math.min(road.line.length - 1, idx + 1)]!, pr = road.line[Math.max(0, idx - 1)]!;
    const dir = norm(sub(nb, pr)), toArea = (ac[0] - node[0]) * dir[0] + (ac[1] - node[1]) * dir[1] >= 0 ? 1 : -1;
    const p: V = [node[0] + dir[0] * 10 * toArea - dir[1] * (road.w / 2 + 1), node[1] + dir[1] * 10 * toArea + dir[0] * (road.w / 2 + 1)];
    L.add('pole-camera', p, Math.atan2(-dir[1] * toArea, -dir[0] * toArea), z, g.id, { size: [0.25, 0.25, S.watchers.types['pole-camera'].mount], variant: 'pole-camera', force: true });
  }
  const sq = ctx.d.plots.find(p => p.civic === 'square');
  if (sq) { const b = bbox(sq.polyV), cc = ctx.plan.circus.c; const p: V = [Math.min(b[1] - 7.5, Math.max(b[0] + 7.5, cc[0])), Math.min(b[3] - 7.5, Math.max(b[2] + 7.5, cc[1]))]; L.add('mast-camera', p, 0, z, sq.id, { size: [0.4, 0.4, 8], variant: 'mast', force: true }); L.add('civic-robot-dock', [p[0] - 2, p[1]], 0, z, sq.id, { force: true }); }
  const circ = kerbNear(segs, [0, -60], 60, 1.1);
  if (circ) for (const o of [0, 3]) L.add('civic-robot-dock', [circ.q[0] + Math.cos(circ.yaw) * (10 + o), circ.q[1] + Math.sin(circ.yaw) * (10 + o)], 0, z, 'circus', { force: true });
  return keep;
}
