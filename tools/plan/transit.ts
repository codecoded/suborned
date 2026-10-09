// A line is its paths in `order` (path ids; a leading "-" runs one against its drawn direction; default: as listed).
// Stops and stations are placed by chainage along that chain, so spacing reads across paths, not per path.
import { type V, chainage, dist, lineDist } from './geom';
import type { Derived, Line, PathGeo, Plan, Stop } from './plan';

export type Mode = 'tram' | 'sky';
export type Link = { id: string; reversed: boolean; offset: number; path: PathGeo };
export type Chain = { links: Link[]; total: number; unknown: string[] };
export type Placed = Omit<Stop, 'path'> & { path: PathGeo; local: number; s: number; off: number };

export const modeOf = (plan: Plan): [Mode, Line[]][] => [['tram', plan.tram], ['sky', plan.skyrail]];
export const stopsOf = (line: Line) => line.stops ?? line.stations ?? [];
/** On or beyond the sheet edge: the line carries on off-sheet there. */
export const offSheet = (plan: Plan, p: V) => p[0] <= plan.sheet.x[0] + .5 || p[0] >= plan.sheet.x[1] - .5 || p[1] <= plan.sheet.y[0] + .5 || p[1] >= plan.sheet.y[1] - .5;
/** Two tracks / beams of one line meet when their ends are within one track (beam) centre. */
export const joinTol = (plan: Plan, mode: Mode) => (mode === 'tram' ? plan.standards.tram.trackCentres : plan.standards.skyrail.beamCentres) + .05;

export function chain(d: Derived, line: Line, mode: Mode): Chain {
  const geo = d[mode].filter(p => p.line === line.id), links: Link[] = [], unknown: string[] = [];
  let offset = 0;
  for (const raw of line.order ?? line.paths.map(p => p.id)) {
    const reversed = raw.startsWith('-'), id = reversed ? raw.slice(1) : raw, path = geo.find(p => p.id === id);
    if (!path) { unknown.push(raw); continue; }
    links.push({ id, reversed, offset, path }); offset += path.length;
  }
  return { links, total: offset, unknown };
}

/** Start and end of a link in travel direction. */
export const ends = (l: Link): [V, V] => { const a = l.path.samples[0]!.p, b = l.path.samples.at(-1)!.p; return l.reversed ? [b, a] : [a, b]; };
export const isRing = (c: Chain, tol: number) => c.links.length > 0 && dist(ends(c.links[0]!)[0], ends(c.links.at(-1)!)[1]) <= tol;

export function place(c: Chain, stop: Stop): Placed | undefined {
  const link = c.links.find(l => l.id === stop.path);
  if (!link) return undefined;
  const local = chainage(link.path.samples, stop.at), off = lineDist(stop.at, link.path.samples.map(x => x.p));
  return { ...stop, path: link.path, local, s: link.offset + (link.reversed ? link.path.length - local : local), off };
}

/** Placed stops of one line sorted along the chain, with each one's gap to the next (a ring's last gap wraps round). */
export function placements(plan: Plan, d: Derived, line: Line, mode: Mode) {
  const c = chain(d, line, mode), items = stopsOf(line).flatMap(s => place(c, s) ?? []).sort((a, b) => a.s - b.s);
  const ring = isRing(c, joinTol(plan, mode)) && items.length > 1;
  const gaps = items.map((p, i) => (items[i + 1] ? items[i + 1]!.s - p.s : ring ? c.total - p.s + items[0]!.s : undefined));
  return { chain: c, items, ring, gaps };
}
