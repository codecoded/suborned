// What the engine and the host believe about the ground (brief 055, Splice): the walk grid, its places and people, the sight occluders, and the host's ground height.
// The host's height is a port of PlayInstall.Heights + SiteFrame.Height: baked on a 1 m grid over the site window from the plot slabs and the drive/bay cuts only.
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { World } from '../../contracts/world/world.schema';
import { civic as civicOf, type Civic } from '../plan/civic';
import { inPoly, type V } from '../plan/geom';
import { derive, type Plan } from '../plan/plan';
import { blockersOf } from '../plan/walk';
import { bodyOf, Solids } from '../plan/walk-solids';
import { ROOT } from './pieces';

const json = (p: string) => JSON.parse(readFileSync(resolve(ROOT, p), 'utf8'));

export type Frame = { toPlan(b: V): V; toBlock(p: V): V };
export type Occ = { id: string; z: number; h: number; poly: V[] }; // block frame, metres
export type Place = { id: string; at: V; src: 'site' | 'people'; people: number };
export type Person = { id: string; role: string; at: V; place: string; src: string };

export type Sim = {
  plan: Plan; civ: Civic; frame: Frame;
  /** Walk grid, block frame m: free(i,j) cell centres */
  grid: { x0: number; y0: number; cell: number; nx: number; ny: number; free(i: number, j: number): boolean; centre(i: number, j: number): V; cellOf(b: V): [number, number] };
  occluders: Occ[]; eye: number;
  /** why a cell is blocked: named solid (with BODY clearance), a building/footprint polygon, a landmark, or water */
  blockedBy(b: V): { kind: 'solid' | 'building' | 'landmark' | 'free'; id: string };
  solids: { id: string; poly: V[] }[]; body: number;
  places: Place[]; persons: Person[];
  host: { heights: Float32Array; nx: number; ny: number; x0: number; y0: number; cell: number; at(b: V): number };
  coverPoints: { id: string; at: V; kind: string; of: string; h: number }[];
  window: { x0: number; x1: number; y0: number; y1: number };
};

export function loadSim(world: World): Sim {
  const plan: Plan = json('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civicOf(plan.civic, json(plan.civic.block));
  const o = plan.civic.blockToPlan.origin as V, ax = plan.civic.blockToPlan.x as V, ay = plan.civic.blockToPlan.y as V;
  const det = ax[0] * ay[1] - ay[0] * ax[1];
  const frame: Frame = {
    toPlan: ([bx, by]) => [o[0] + bx * ax[0] + by * ay[0], o[1] + bx * ax[1] + by * ay[1]],
    toBlock: p => { const dx = p[0] - o[0], dy = p[1] - o[1]; return [(dx * ay[1] - ay[0] * dy) / det, (ax[0] * dy - dx * ax[1]) / det]; },
  };
  const site = json('sim/data/handover.site.json'), g = site.grid;
  const rows: string[] = g.rows, cm = (v: number) => v / 100;
  const grid = {
    x0: cm(g.x0), y0: cm(g.y0), cell: cm(g.cell), nx: g.nx as number, ny: g.ny as number,
    free: (i: number, j: number) => i >= 0 && j >= 0 && i < g.nx && j < g.ny && rows[i]![j] === '.',
    centre: (i: number, j: number): V => [cm(g.x0) + (i + 0.5) * cm(g.cell), cm(g.y0) + (j + 0.5) * cm(g.cell)],
    cellOf: (b: V): [number, number] => [Math.floor((b[0] - cm(g.x0)) / cm(g.cell)), Math.floor((b[1] - cm(g.y0)) / cm(g.cell))],
  };
  const occluders: Occ[] = [...site.occluders, ...site.cover.blockers.map((b: Occ & { id: string }) => ({ ...b, id: 'cover:' + b.id }))].map((q: { id: string; z: number; h: number; poly: number[][] }) => ({ id: q.id, z: cm(q.z), h: cm(q.h), poly: q.poly.map(p => [cm(p[0]!), cm(p[1]!)] as V) }));
  const eye = (json('contracts/rules/rulebook.json').values['eye-cm'].v as number) / 100;

  // the blockers as the grid is made (tools/plan/walk-grid.ts blockedAt), with the reason
  const { polys, rounds } = blockersOf(plan, d, civ);
  const body = bodyOf(plan), sol = new Solids(civ.solids, body);
  const ids: string[] = civ.solidIds ?? [];
  const solids = civ.solids!.map((poly, k) => ({ id: ids[k] ?? `solid-${k}`, poly: poly.map(p => frame.toBlock(p)) }));
  const boxed = polys.map(b => ({ b, x0: Math.min(...b.map(v => v[0])), x1: Math.max(...b.map(v => v[0])), y0: Math.min(...b.map(v => v[1])), y1: Math.max(...b.map(v => v[1])) }));
  const blockedBy = (b: V): { kind: 'solid' | 'building' | 'landmark' | 'free'; id: string } => {
    const p = frame.toPlan(b);
    const hit = boxed.find(k => p[0] >= k.x0 && p[0] <= k.x1 && p[1] >= k.y0 && p[1] <= k.y1 && inPoly(p, k.b));
    if (hit) return { kind: 'building', id: `poly${boxed.indexOf(hit)}` };
    const r = rounds.find(l => Math.hypot(p[0] - l.c[0], p[1] - l.c[1]) < l.r);
    if (r) return { kind: 'landmark', id: r.id };
    if (!sol.clear(p, p, sol.near(p[0], p[0], p[1], p[1]))) {
      // the named solid: nearest edge distance below BODY, or inside
      let best = '', bd = Infinity;
      civ.solids!.forEach((poly, k) => { let dd = Infinity; for (let i = 0; i < poly.length; i++) { const a = poly[i]!, c = poly[(i + 1) % poly.length]!; dd = Math.min(dd, segDist(p, a, c)); } if (inPoly(p, poly)) dd = 0; if (dd < bd) { bd = dd; best = ids[k] ?? `solid-${k}`; } });
      return { kind: 'solid', id: best };
    }
    return { kind: 'free', id: '' };
  };

  // the host's ground: PlayInstall.Heights on the 1 m grid over the site window
  const W = { x0: -52, y0: -66, cell: 1, nx: 164, ny: 82 };
  const slabs = world.ground.plots.filter(p => p.base + p.height < 1).map(p => ({ ring: p.ring as V[], top: p.base + p.height, x0: Math.min(...p.ring.map(v => v[0])), x1: Math.max(...p.ring.map(v => v[0])), y0: Math.min(...p.ring.map(v => v[1])), y1: Math.max(...p.ring.map(v => v[1])) }));
  const cuts: { ring: V[]; level: number }[] = [];
  for (const dr of world.ground.drives ?? []) cuts.push({ ring: dr.ring as V[], level: dr.level });
  for (const f of world.ground.flats) if (f.id.includes('/bay/')) cuts.push({ ring: f.ring as V[], level: f.level }); // brief 055: every bay is sunk (Layers.IsBay)
  const heights = new Float32Array(W.nx * W.ny);
  for (let i = 0; i < W.nx; i++) for (let j = 0; j < W.ny; j++) {
    const p = frame.toPlan([W.x0 + (i + 0.5) * W.cell, W.y0 + (j + 0.5) * W.cell]); let top = 0;
    for (const s of slabs) if (s.top > top && p[0] >= s.x0 && p[0] <= s.x1 && p[1] >= s.y0 && p[1] <= s.y1 && inPoly(p, s.ring)) top = s.top;
    for (const c of cuts) if (inPoly(p, c.ring)) { top = c.level; break; }
    heights[i * W.ny + j] = top;
  }
  const host = { heights, nx: W.nx, ny: W.ny, x0: W.x0, y0: W.y0, cell: W.cell, at: (b: V) => { const i = Math.floor((b[0] - W.x0) / W.cell), j = Math.floor((b[1] - W.y0) / W.cell); return i < 0 || j < 0 || i >= W.nx || j >= W.ny ? 0 : heights[i * W.ny + j]!; } };

  // places and people
  const people = json('sim/data/handover.people.json'), perPlace = new Map<string, number>();
  for (const q of people.people) for (const st of q.stops as [string, number, number, number][]) if (st[3] === 1 && st[1] <= 510 && st[2] >= 0) perPlace.set(st[0], (perPlace.get(st[0]) ?? 0) + 1);
  const places: Place[] = [];
  for (const [id, v] of Object.entries(site.places as Record<string, { at: number[] }>)) places.push({ id, at: [cm(v.at[0]!), cm(v.at[1]!)], src: 'site', people: 0 });
  for (const [id, a] of Object.entries(people.places as Record<string, number[]>)) places.push({ id, at: [cm(a[0]!), cm(a[1]!)], src: 'people', people: perPlace.get(id) ?? 0 });
  const rounds2 = json('sim/data/handover.rounds.json');
  const persons: Person[] = [];
  const role = (r: string, pl: string, src: string) => { const a = site.places[pl]?.at; if (a) persons.push({ id: `cast:${r}`, role: r, at: [cm(a[0]), cm(a[1])], place: pl, src }); };
  role('guard', site.guard.post, 'site.guard.post'); role('guard-cafe', site.guard.cafe, 'site.guard.cafe');
  role('councillor', site.vale.exit, 'site.vale.exit'); role('councillor-car', site.vale.car, 'site.vale.car'); role('driver', site.car.driver, 'site.car.driver');
  role('waitress', 'd:Café door', 'cast waitress post');
  for (const c of ['fixer', 'tech', 'scout', 'enforcer']) role(`crew-${c}`, `crew:${c}`, 'crew start');
  role('rival-car-entry', site.rivals.car?.entry ?? 'rival-car-entry', 'rivals.car'); // brief 059 (engine 0.33.0): the crew comes by car; rival-entry is read by nothing now role('security-entry', site.security.entry, 'security'); role('patrol-a', site.patrol.a, 'patrol'); role('patrol-b', site.patrol.b, 'patrol');
  for (const [k, s] of Object.entries(rounds2.guard.stops as { at: number[] }[])) persons.push({ id: `guard:round-${k}`, role: 'guard-round', at: [cm(s.at[0]!), cm(s.at[1]!)], place: 'round stop', src: 'rounds.guard.stops' });
  for (const [pid, v] of Object.entries(rounds2.servers as Record<string, { excursions?: never; spans: { excursions?: { at: number[] }[] }[] }>)) for (const sp of v.spans) for (const [k, e] of (sp.excursions ?? []).entries()) persons.push({ id: `${pid}:table-${k}`, role: 'server-round', at: [cm(e.at[0]!), cm(e.at[1]!)], place: 'table', src: 'rounds.servers' });
  const coverPoints = (site.cover.points as { id: string; at: number[]; kind: string; of: string; h: number }[]).map(c => ({ id: c.id, at: [cm(c.at[0]!), cm(c.at[1]!)] as V, kind: c.kind, of: c.of, h: cm(c.h) }));
  return { plan, civ, frame, grid, occluders, eye, blockedBy, solids, body: body, places, persons, host, coverPoints, window: { x0: -52, x1: 112, y0: -66, y1: 16 } };
}

function segDist(p: V, a: V, b: V) {
  const abx = b[0] - a[0], aby = b[1] - a[1], t = Math.max(0, Math.min(1, ((p[0] - a[0]) * abx + (p[1] - a[1]) * aby) / (abx * abx + aby * aby || 1)));
  return Math.hypot(p[0] - (a[0] + abx * t), p[1] - (a[1] + aby * t));
}
