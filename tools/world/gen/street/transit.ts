// Transit kits and watchers: a tram stop's shelter, benches, ticket machine, timetable column, bin and bollards; the
// kiosks and crossings round it; each sky rail station's entrances, kiosks and clocks; and the cameras and robots at the
// sites standards.streetscape.watchers lists (stations, tram stops, gates, the circus, the square: its mast where the civic block puts it, mast.ts).
import type { Connector, Mass } from '../../../../contracts/world/world.schema';
import { type V, dist, lineDist, norm, sub } from '../../../plan/geom';
import { boxRing, quayBlock, samples9 } from '../../../plan/street-zones';
import type { Ctx } from '../common';
import { kindOf, plotRing } from '../edges';
import { levelAt } from './ground';
import { axisClears } from '../connectors';
import { type Rect, bbox, edgeN, inRect } from '../geo';
import type { DeckRec } from '../types';
import { KERB_ARC_CLEAR, type Layer } from './kit';
import { squareMast } from './mast';
import { cutHits } from '../kerbreturn';

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
    // brief 020: both platforms of a stop share the platform's yaw, so one of the pair would face away from its track. A shelter piece's open front is its +Z (yaw + 90 degrees; BusShelter_Play's glass back is at -Z, measured in the engine): turn it to face the track, i.e. toward -out
    const shYaw = Math.cos(yaw + Math.PI / 2) * out[0] + Math.sin(yaw + Math.PI / 2) * out[1] > 0 ? Math.atan2(Math.sin(yaw + Math.PI), Math.cos(yaw + Math.PI)) : yaw;
    L.add('tram-shelter', at(0, w / 2 - 0.75), shYaw, top, id, { force: true });
    for (const s of [-2, 2]) L.add('bench', at(s, w / 2 - 0.55), yaw, top, id, { force: true });
    L.add('ticket-machine', at(len / 2 - 1.2, 0), yaw, top, id, { size: [0.8, 0.6, 1.8], force: true });
    L.add('timetable-column', at(-len / 2 + 1.2, 0), yaw, top, id, { size: [0.4, 0.4, 2.6], variant: 'coral-dial', force: true });
    L.add('bin', at(len / 2 - 3, w / 2 - 0.4), 0, top, id, { force: true });
    for (const s of [-(len / 2 + tstd.platform * 0 + 3.6 + 0.6), len / 2 + 3.6 + 0.6]) L.add('bollard', at(s, 0), 0, z, id, { size: S.bollards.size, r: 0.2, force: true });
    L.add('pole-camera', at(-len / 2 + 0.6, w / 2 - 0.2), yaw, top, id, { size: [0.25, 0.25, S.watchers.types['pole-camera'].mount], variant: 'pole-camera', force: true });
    if (!done.has(`${line}/${stop}`)) { // once per stop: crossings at both platform ends (one is kept: coverage.rules.stops), kiosks and phone booths on the pavement facing it
      done.add(`${line}/${stop}`);
      // a patch lies at the level of the surface under it (a plan with crossings.strip: the carriageway, not the pavement top above it)
      for (const s of [-(len / 2 + 5), len / 2 + 5]) L.add('crossing', at(s, 0), yaw + Math.PI / 2, S.crossings.strip ? levelAt(ctx, at(s, 0)) : z, id, { size: [8, S.crossings.w, 0.02], r: 2, force: true }); // both ends are candidates: pairCrossings keeps the end coverage.rules.stops names (the other end's patch still keeps the furniture off it, so nothing else moves)
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
    // brief 075: the bank's furniture is laid in the line's own frame, as the bank is (connectors.ts: the tower's yaw is 0 on an east-west line, a quarter turn on a
    // north-south one): `s` along the line, `o` out from it on the bank's side. Before, it stepped in x and out in y whatever the line, so on a north-south station
    // (S2's Harbour and Skyport) the newsstand, booths, camera and dock stepped across the line, not along it, and a booth landed in the terrace's walk (SF-1)
    const bb = bbox(t.ring), c: V = [(bb[0] + bb[1]) / 2, (bb[2] + bb[3]) / 2], id = t.host, horiz = Math.abs(Math.sin(t.yaw)) < 0.5;
    const plat = stations.find(m => m.id === t.host.replace(/^bank\//, 'station/')), pc = plat ? bbox(plat.ring) : undefined;
    const side = horiz ? (c[1] >= 0 ? 1 : -1) : (pc && c[0] < (pc[0] + pc[1]) / 2 ? -1 : 1); // east-west: away from the line at y 0 (S1, as before); north-south: away from the bank's platform
    const u: V = horiz ? [1, 0] : [0, 1], out: V = horiz ? [0, side] : [side, 0], yaw0 = horiz ? 0 : Math.PI / 2;
    keep.push([bb[0] - 1, bb[1] + 1, bb[2] - 1, bb[3] + 1]);
    const clears = axisClears(ctx), mid = (r: Rect) => (r[0] + r[1]) / 2;
    const at = (s: number, o: number): V => { // furniture never stands in a forecourt's clear axis (an east-west forecourt's x band): it moves to the nearer side
      const p: V = [c[0] + u[0] * s + out[0] * o, c[1] + u[1] * s + out[1] * o], a = horiz ? clears.find(r => inRect(p, r, 0.5)) : undefined;
      return a ? [p[0] < mid(a) ? a[0] - 0.8 : a[1] + 0.8, p[1]] : p;
    };
    // the crossing first (flat, it keeps its place); then each item takes the nearest spot that keeps the walk zones, the beam and the crossing's 2 m clear
    // the crossing lies across the road beyond the bank; with the tower now outside the platform it is moved in until it is 1.7 m clear of a quay edge (SF-14)
    const offQuay = (p: V) => !quayBlock(ctx, samples9(boxRing(p, 3, S.crossings.w, yaw0 + Math.PI / 2)));
    const cross = [8, 6.5, 5, 3.5, 2].map(o => at(0, 3.5 + o)).find(offQuay) ?? at(0, 3.5 + 2);
    L.add('crossing', cross, yaw0 + Math.PI / 2, z, id, { size: [3, S.crossings.w, 0.02], r: 2, force: true }); // brief 061 (coverage.rules.banks false): no crossing at the stair foot, but the patch still keeps the furniture off it (pairCrossings drops it)
    const near = (kind: string, s: number, o: number, yaw: number, opt: { size?: [number, number, number]; variant?: string } = {}) => {
      for (const ds of [0, 3, -3, 6, -6, 9, -9, 12, -12, 15, -15, 18, -18, 24, -24]) for (const dd of [0, -1.5, -3, 1.5, -4.5, -6, 3, 4.5, -7.5, -9]) if (L.add(kind, at(s + ds, o + dd), yaw, z, id, opt)) return true;
      return false;
    };
    near('station-entrance', 0, 3.5 + 2.5, yaw0);
    near('newsstand', -7, 3.5 + 1, yaw0); near('phone-booth', 7, 3.5 + 1, yaw0); near('phone-booth', 8.5, 3.5 + 1, yaw0);
    near('pole-camera', -4.5, 3.5 + 6, yaw0, { size: [0.25, 0.25, S.watchers.types['pole-camera'].mount], variant: 'pole-camera' });
    if (id.endsWith('/l')) near('civic-robot-dock', 5, 3.5 + 6, yaw0); // the main bank
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
    // brief 028: 10 m in from the node; where a rounded kerb return takes that spot the camera stands further in, the first metre past the arc's 0.45 m
    let p: V = [0, 0];
    for (let k = 0; k <= 30; k++) { const d = 10 + k; p = [node[0] + dir[0] * d * toArea - dir[1] * (road.w / 2 + 1), node[1] + dir[1] * d * toArea + dir[0] * (road.w / 2 + 1)]; if (!cutHits(ctx, boxRing(p, 0.25, 0.25, 0), KERB_ARC_CLEAR)) break; }
    L.add('pole-camera', p, Math.atan2(-dir[1] * toArea, -dir[0] * toArea), z, g.id, { size: [0.25, 0.25, S.watchers.types['pole-camera'].mount], variant: 'pole-camera', force: true });
  }
  const sq = ctx.d.plots.find(p => p.civic === 'square');
  if (sq) {
    // brief 101 (Sky's pick B): the mast stands where the block puts it (mast.ts: the lamp standard that carries it, and its turn), at the square's edge nearest its main
    // entrance; a block that names none stands it at the corner point below, yaw 0, as before. The robot's dock keeps its own rule: 2 m west of the square's corner point
    // nearest the circus (the circus centre held 7.5 m inside the square's edges). Mast first, dock second, as before, so their zone ids stay mast-camera-z0 and
    // civic-robot-dock-z1 (brief 078: a changed mast id silently dropped its words and orders; zone-stable.test.ts holds them).
    const b = bbox(sq.polyV), cc = ctx.plan.circus.c, corner: V = [Math.min(b[1] - 7.5, Math.max(b[0] + 7.5, cc[0])), Math.min(b[3] - 7.5, Math.max(b[2] + 7.5, cc[1]))], m = squareMast(ctx);
    const placed = L.add('mast-camera', m?.at ?? corner, m?.yaw ?? 0, z, sq.id, { size: [0.4, 0.4, 8], variant: 'mast', force: true });
    if (m && !placed) throw new Error(`the square's mast on ${m.lamp} (civic-block dressing) fails the street layer's tests there (kerb return, drive, crossing or door clearance): move the lamp, or the dock would take the mast's id`);
    L.add('civic-robot-dock', [corner[0] - 2, corner[1]], 0, z, sq.id, { force: true });
  }
  const circ = kerbNear(segs, [0, -60], 60, 1.1);
  if (circ) { // brief 075 (Datum): the pair stands 10 and 13 m along the kerb from the point nearest (0, -60); where a kerb return or a crossing takes that spot (the Boulevard at w 20 brings the circus mouth's return to it), the pair steps back along the kerb, away from the circus, a metre at a time, to the first spot where both pass every test (the walk, the tram's space, crossings, doors)
    const at = (d: number): V => [circ.q[0] + Math.cos(circ.yaw) * d, circ.q[1] + Math.sin(circ.yaw) * d];
    const pair = (d: number, force: boolean) => L.fits('civic-robot-dock', at(d), 0, z, 'circus', { force }) && L.fits('civic-robot-dock', at(d + 3), 0, z, 'circus', { force });
    let d = 10; if (!pair(d, true)) for (d = 9; d > -30 && !pair(d, false); d--);
    for (const o of [0, 3]) L.add('civic-robot-dock', at(d + o), 0, z, 'circus', { force: true });
  }
  return keep;
}
