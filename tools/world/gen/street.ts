// Street furniture, trees, lamps, tram stop kits, station entrances and watchers: per street edge from
// standards.streetscape, with the catalogue's counts and spacings. Nothing is placed item by item.
import type { Connector, Crossing, Flag, Frontage, Mass, StreetItem } from '../../../contracts/world/world.schema';
import type { V } from '../../plan/geom';
import type { Ctx } from './common';
import { type Rect, bbox, growRect } from './geo';
import type { DeckRec, MegaOut } from './types';
import { boxRing, roadAt } from '../../plan/street-zones';
import { type V as PV, dist, dot, norm, sub } from '../../plan/geom';
import { flagRow } from './colour';
import { edgeN } from './geo';
import { streetKit } from './kitrefs';
import { bindPart } from './street/partkit';
import { type CrossingLog, bankFeet, civicLandings, pairCrossings } from './street/crossings';
import { Layer } from './street/kit';
import { dressCircus, dressDecks, dressFoot, dressMedians, dressParks } from './street/public';
import { dressRoads } from './street/roads';
import { dressTransit } from './street/transit';

export function generateStreet(ctx: Ctx, o: { buildings: Mass[]; megas: MegaOut[]; decks: DeckRec[]; stops: Mass[]; stations: Mass[]; connectors: Connector[]; frontages: Frontage[] }): { items: StreetItem[]; flags: Flag[]; crossings?: Crossing[]; crossingLog?: CrossingLog } {
  const ground = (m: Mass) => m.base < 1;
  const solids: V[][] = [...o.buildings.filter(ground).map(m => m.ring), ...o.megas.flatMap(m => m.parts.filter(ground).map(p => p.ring))];
  const post = (p: V): V[] => [[p[0] - 0.7, p[1] - 0.7], [p[0] + 0.7, p[1] - 0.7], [p[0] + 0.7, p[1] + 0.7], [p[0] - 0.7, p[1] + 0.7]];
  solids.push(...o.megas.flatMap(m => (m.columns ?? []).map(post))); // colonnade columns
  const keep: Rect[] = o.connectors.filter(c => c.id.startsWith('bank/')).map(c => growRect(bbox(c.ring), 1));
  const legs = o.megas.flatMap(m => m.parts.filter(p => /\/leg-\d+$/.test(p.id)).map(p => { const b = bbox(p.ring); return [(b[0] + b[1]) / 2, (b[2] + b[3]) / 2] as V; }));
  const cols = [...o.decks.flatMap(d => d.columns), ...o.megas.flatMap(m => m.columns ?? []), ...legs], under = o.decks.filter(d => d.level !== 'street').map(d => d.rect);
  const doors = o.frontages.filter(f => f.level === 'street').map(f => boxRing(f.entrance.c as V, f.entrance.size[0], f.entrance.size[1], f.entrance.yaw));
  const L = new Layer(ctx, solids, under, cols, keep, ctx.d.pylons.map(p => p.p), doors);
  dressTransit(ctx, L, o.stops, o.stations, o.connectors, o.decks); // transit kits first: their places are fixed
  dressRoads(ctx, L); dressMedians(ctx, L); dressFoot(ctx, L); dressCircus(ctx, L); dressParks(ctx, L);
  const hall = o.megas.flatMap(m => m.parts.filter(p => /\/(hall|core)-/.test(p.id)).map(p => p.ring));
  const D = new Layer(ctx, hall, [], [], keep, []);
  dressDecks(ctx, D, o.decks.filter(d => !/\/(mezzanine|bridge)/.test(d.id)));
  const flags = dressFlags(ctx, L, o.frontages); // last: flags take what the furniture leaves
  if (ctx.S.crossings.strip) bankFeet(ctx, L, o.connectors.filter(k => k.kind === 'stairTower'));
  const civicIds = ctx.S.crossings.strip ? civicLandings(ctx, L) : [];
  const strips = ctx.S.crossings.strip ? pairCrossings(ctx, L, civicIds) : undefined; // opt-in (streetscape.crossings.strip): crossings take the spots the furniture left
  return { items: streetKit(ctx, [...L.items.filter(x => x.kind !== 'flag-mast'), ...D.items, ...(strips?.parts ?? [])]).map(bindPart), flags, ...(strips ? { crossings: strips.crossings, crossingLog: strips.log } : {}) };
}

/** Grammar §2a flag masts (8 m, 9 m pitch): both sides of Crown Parade in the mall's kerb band (outside SpaceHQ's ground hall), along hotel and civic
 *  bureau frontages in the kerb band of their road, and flanking the Records Office entrance. Each must pass the street layer (keep-clear, walk zone, doors). */
function dressFlags(ctx: Ctx, L: Layer, frontages: Frontage[]): Flag[] {
  const z = ctx.pav, out: Flag[] = [], halls = (ctx.plan.signatures ?? []).flatMap((g: any) => (g.ground ? [g.ground as [number, number, number, number]] : []));
  const put = (edge: string, id: string) => (at: PV, r: number) => !halls.some(g => at[0] >= g[0] - 2 && at[0] <= g[1] + 2 && at[1] >= g[2] - 2 && at[1] <= g[3] + 2)
    && L.add('flag-mast', at, 0, z, edge, { size: [0.3, 0.3, 8], r, id });
  const mall = ctx.S.sections.mall.bands.kerb as [number, number], off = (mall[0] + mall[1]) / 2 - 0.5; // the kerb band's centre-band half: palms stand on its middle
  for (const f of ctx.plan.foot.filter((q: any) => q.id === 'crown-parade')) {
    const a = f.pts[0] as PV, b = f.pts[f.pts.length - 1] as PV, u = norm(sub(b, a)), nl: PV = [-u[1], u[0]];
    for (const side of [1, -1]) { const o: PV = [nl[0] * side * off, nl[1] * side * off];
      out.push(...flagRow(`flags/parade/${side > 0 ? 'n' : 's'}`, 'parade', `foot/${f.id}`, [a[0] + o[0], a[1] + o[1]], [b[0] + o[0], b[1] + o[1]], z, (p, r) => put(`foot/${f.id}`, `flags/parade/${side > 0 ? 'n' : 's'}/${p[0].toFixed(1)}`)(p, r))); }
  }
  for (const t of frontages.filter(x => x.level === 'street' && ['hotel-lobby', 'credit-bureau', 'employment-bureau'].includes(x.trade))) {
    const mid: PV = [(t.a[0] + t.b[0]) / 2, (t.a[1] + t.b[1]) / 2], at = roadAt(ctx as any, mid, t.street); if (!at || at.dk < at.sec.kerb) continue;
    const k = at.dk - at.sec.kerb / 2, o: PV = [t.normal[0] * k, t.normal[1] * k], kind = t.trade === 'hotel-lobby' ? 'hotel' : 'civic', ext = Math.max(0, 9.5 - t.w) / 2, u = norm(sub(t.b, t.a));
    const a: PV = [t.a[0] - u[0] * ext + o[0], t.a[1] - u[1] * ext + o[1]], b: PV = [t.b[0] + u[0] * ext + o[0], t.b[1] + u[1] * ext + o[1]];
    out.push(...flagRow(`flags/${t.id}`, kind, t.id, a, b, z, (p, r) => put(t.street, `flags/${t.id}/${p[0].toFixed(1)}`)(p, r)));
  }
  // the Records Office: a row along the face its entrance stands on, 4 m out, the entrance itself kept clear
  const office = ctx.civ.buildings.find(x => x.id === 'civic-office'), door = ctx.civ.entrances.find(e => e.building === 'civic-office');
  if (office && door) {
    const P = office.poly as PV[], N = P.length;
    let best = 0, bd = Infinity; for (let i = 0; i < N; i++) { const a = P[i]!, b = P[(i + 1) % N]!, u = norm(sub(b, a)), s = dot(sub(door.p, a), u), L0 = dist(a, b), d = Math.abs((door.p[0] - a[0]) * u[1] - (door.p[1] - a[1]) * u[0]); if (s > 0 && s < L0 && d < bd) { bd = d; best = i; } }
    const a = P[best]!, b = P[(best + 1) % N]!, u = norm(sub(b, a)), area = P.reduce((n, p, i) => n + p[0] * P[(i + 1) % N]![1] - P[(i + 1) % N]![0] * p[1], 0), n0 = edgeN(a, b), n: PV = area > 0 ? n0 : [-n0[0], -n0[1]];
    const s = dot(sub(door.p, a), u), o: PV = [n[0] * 4, n[1] * 4], at = (q: number): PV => [a[0] + u[0] * q + o[0], a[1] + u[1] * q + o[1]];
    out.push(...flagRow('flags/records-office/a', 'civic', 'civic/civic-office', at(1), at(s - 3), z, (p, r) => put('civic', `flags/records-office/a/${p[0].toFixed(1)}`)(p, r)));
    out.push(...flagRow('flags/records-office/b', 'civic', 'civic/civic-office', at(s + 3), at(dist(a, b) - 1), z, (p, r) => put('civic', `flags/records-office/b/${p[0].toFixed(1)}`)(p, r)));
  }
  return out;
}
