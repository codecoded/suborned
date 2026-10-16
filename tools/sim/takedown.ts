// The takedown places as the C# engine reads them (brief 013 C, Splice for Keel): sim/data/handover.takedown.json, block frame, integer centimetres, the same frame
// and grid as sim/data/handover.site.json. Derived from the world export (`w.takedown`: tools/world/gen/street/quiet.ts, rules R-HIDE, R-STASH, R-REPORT, R-DOCK) and the
// civic block, so the engine reads the stash containers, the service doors, the report points (call boxes and staffed doors), the response origins (docks and posts) and the
// watchers the world stands, from data, and nothing here is written for one mission.
//
// It is a second file, beside the people extract, and not a key of the site: the engine's site reader rejects a key it does not know, and the engine hashes the site text, so a
// new key in the site is a drift of every replay until Keel's reader takes it. This file changes no recorded run. Keel reads it with Site.cs when the engine side lands; until then
// the file is checked here (tools/sim/sim.test.ts) and by R-STASH / R-REPORT / R-DOCK.
// Run: bun run sim:site (writes it with the site and the people); bun test tools/sim (checks it).
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import type { StreetItem, Takedown } from '../../contracts/world/world.schema';
import { civic } from '../plan/civic';
import type { V } from '../plan/geom';
import { derive } from '../plan/plan';
import { boxRing } from '../plan/street-zones';
import { toBlockFn, walkGrid, CELL, WINDOW } from '../plan/walk-grid';
import { generated } from '../plan/world-ctx';
import { pretty } from './site';

const root = join(import.meta.dir, '..', '..');
export const TAKEDOWN_FILE = 'sim/data/handover.takedown.json';
const PLAN = 'contracts/world/harbour-district.plan.json';
const cm = (m: number) => Math.round(m * 100);

type Pt = number[];

export function buildTakedown(): { json: string; data: unknown } {
  const read = (f: string) => readFileSync(join(root, f), 'utf8');
  const plan = JSON.parse(read(PLAN)), block = JSON.parse(read(plan.civic.block));
  const d = derive(plan), civ = civic(plan.civic, block), toBlock = toBlockFn(plan.civic);
  const w = generated(plan, d, civ).w, t: Takedown | undefined = w.takedown;
  if (!t) throw new Error('the world has no takedown section: the civic block has no service door (brief 013)');
  const G = walkGrid(plan, d, civ, WINDOW, CELL);
  const P = (p: V): Pt => { const q = toBlock(p); return [cm(q[0]), cm(q[1])]; };
  /** A stand must be free ground on the site's grid, or the engine's walkers can't stand there. */
  const S = (p: V, what: string): Pt => { const q = toBlock(p); if (!G.free(q)) throw new Error(`${what} stands on blocked ground (${q})`); return [cm(q[0]), cm(q[1])]; };
  const byId = new Map<string, StreetItem>(w.street.map((s) => [s.id, s]));
  const solid = (id: string) => { const s = byId.get(id)!; return { id, poly: boxRing(s.at, s.size[0], s.size[1], s.yaw).map(P), h: cm(s.size[2]) }; };
  const items = new Set<string>([...t.stashes.flatMap((s) => (s.item ? [s.item] : [])), ...t.report.flatMap((r) => (r.item ? [r.item] : [])), ...t.origins.flatMap((o) => (o.item ? [o.item] : []))]);
  const civicItems = [...items].filter((id) => id.startsWith('street/civic/')).sort();
  const dir = (yaw: number): Pt => { const dx = Math.cos(yaw), dy = Math.sin(yaw); return [Math.round(-dy * 1000), Math.round(dx * 1000)]; }; // plan heading → block heading, per mille

  const data = {
    schema: 'suborned.sim.takedown/1',
    mission: 'handover',
    frame: 'civic-block.json block frame in integer centimetres: x south, y east (as sim/data/handover.site.json, whose grid this file shares). A `stand` is a free cell of that grid.',
    from: [PLAN, plan.civic.block, 'contracts/world/kit.json', 'tools/world/gen/street/quiet.ts (the rules that place them)', 'tools/world/rules/quiet.ts (R-HIDE, R-STASH, R-REPORT, R-DOCK)'],
    grid: { x0: cm(WINDOW.x0), y0: cm(WINDOW.y0), cell: cm(CELL) },
    reach: { reportCm: cm(t.reach.report), originCm: cm(t.reach.origin), latticeCm: cm(t.reach.lattice), src: 'Pulse briefs/013/pulse-witnesses.values.json report-reach-m, dock-reach-m (proposed, 40 m): the walk on the site grid from a lattice sample to the nearest point' },
    entrances: t.entrances.map((e) => ({ id: e.id, building: e.building, name: e.name, kind: e.kind, at: P(e.at), stand: S(e.stand, e.id) })),
    stashes: t.stashes.map((s) => ({
      id: s.id, kind: s.kind, ...(s.of ? { of: s.of } : {}), at: P(s.at), stand: S(s.stand, s.id), holds: s.holds,
      ...(s.item ? { item: s.item } : {}), ...(s.bare ? { bare: s.bare } : {}),
      src: s.kind === 'door' ? 'civic-block.json entrance with kind service: a body put through it is a timed off-screen action (Motive 013 §2.4, 10 s)' : 'a street item whose kit piece is tagged stash in kit.json (Dumpster, holds 1): a body put in it is a carry on the street (Motive 013 §2.4, 5 s)',
    })),
    reportPoints: t.report.map((r) => ({ id: r.id, kind: r.kind, at: P(r.at), stand: S(r.stand, r.id), ...(r.door ? { door: r.door } : {}), ...(r.item ? { item: r.item } : {}) })),
    origins: t.origins.map((o) => ({ id: o.id, kind: o.kind, at: P(o.at), stand: S(o.stand, o.id), ...(o.door ? { door: o.door } : {}), ...(o.item ? { item: o.item } : {}) })),
    coverage: t.coverage,
    solids: civicItems.map(solid),
    solidsSrc: 'the footprints and heights of the pieces the rules placed (containers, call boxes, posts). The site grid does not hold them: holding them would change the grid, and with it every walk and every replay',
    watchers: t.watchers.map((x) => ({ id: x.id, kind: x.kind, at: P(x.at), facings: x.facings.map(dir), range: cm(x.range), coneDeg: x.coneDeg, inSim: false })),
    watchersSrc: 'the watchers the world stands in the window (plan streetscape.watchers.sites). The engine reads none of them yet: its cameras are mission.json cameras (entrance, pickup). briefs/013 C open issue 3; whose reading is right is Aster\'s call',
  };
  return { json: pretty(data) + '\n', data };
}
