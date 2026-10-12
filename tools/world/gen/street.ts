// Street furniture, trees, lamps, tram stop kits, station entrances and watchers: per street edge from
// standards.streetscape, with the catalogue's counts and spacings. Nothing is placed item by item.
import type { Connector, Frontage, Mass, StreetItem } from '../../../contracts/world/world.schema';
import type { V } from '../../plan/geom';
import type { Ctx } from './common';
import { type Rect, bbox, growRect } from './geo';
import type { DeckRec, MegaOut } from './types';
import { boxRing } from '../../plan/street-zones';
import { streetKit } from './kitrefs';
import { Layer } from './street/kit';
import { dressCircus, dressDecks, dressFoot, dressMedians, dressParks } from './street/public';
import { dressRoads } from './street/roads';
import { dressTransit } from './street/transit';

export function generateStreet(ctx: Ctx, o: { buildings: Mass[]; megas: MegaOut[]; decks: DeckRec[]; stops: Mass[]; stations: Mass[]; connectors: Connector[]; frontages: Frontage[] }): StreetItem[] {
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
  return streetKit(ctx, [...L.items, ...D.items]);
}
