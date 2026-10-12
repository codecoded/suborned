// The Handover mission as an overlay on the world (tools/plan/overlay.ts): its destinations give the walk origins, its points
// and routes get rules C-3 and M-1. The rules and their texts are unchanged from when they lived in the plan checker; recorded
// runs replay against them. The data is contracts/missions/handover/mission.json; a recorded run's pinned suborned.block/2
// and its plan's `routes` carry the same data and win over the file.
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Check } from '../../plan/checks';
import { toPlanFn } from '../../plan/civic';
import { dijkstra } from '../../plan/graph';
import { lineDist, inPoly, type V } from '../../plan/geom';
import type { Overlay, Route } from '../../plan/overlay';

const FILE = resolve(import.meta.dir, '../../../contracts/missions/handover/mission.json');
const f = (x: number, d = 0) => x.toFixed(d);

/** The layer a run recorded: destinations in its pinned block, routes in its plan. Undefined for a world-only block. */
export const legacyOverlay = (plan: any, block: any): Overlay | undefined => (block.destinations ? build(plan, block.destinations, plan.routes ?? []) : undefined);
/** The layer from contracts/missions/handover/mission.json, or the run's own when the block carries one. */
export const overlay = (plan: any, block: any): Overlay => legacyOverlay(plan, block) ?? ((m: any) => build(plan, m.destinations, m.routes))(JSON.parse(readFileSync(FILE, 'utf8')));

function build(plan: any, destinations: { name: string; at: V; surface: string }[], routes: { id: string; name: string; from: string; to: string[]; toName?: string }[]): Overlay {
  const toPlan = toPlanFn(plan.civic), points = destinations.map(d => ({ name: d.name, p: toPlan(d.at) }));
  return {
    origins: p => (p.standards.walking.from as string[]).map(name => ({ name, p: points.find(x => x.name === name)!.p })),
    civic: ({ d }) => {
      const lost = points.filter(pt => !d.plots.some(p => inPoly(pt.p, p.polyV)) && !d.roads.some(r => lineDist(pt.p, r.line) <= r.w / 2));
      return [{ id: 'C-3', group: 'Civic', rule: 'Mission points land on a plot or a street', value: `${points.length - lost.length}/${points.length}`, limit: 'all', ok: !lost.length, detail: lost.map(p => p.name).join(', ') || undefined }];
    },
    routes: ({ plan, d, E, roadById }) => {
      const gateAt = new Map(plan.gates.map(g => [`${g.node}|${g.road}`, g.name]));
      const out: Route[] = routes.map(r => {
        const { D, trace } = dijkstra(E, r.from), to = r.to.reduce((b, t) => ((D.get(t) ?? Infinity) < (D.get(b) ?? Infinity) ? t : b));
        const path = trace(to), names: string[] = [];
        for (const e of path) { const n = roadById.get(e.road)!.name; if (names[names.length - 1] !== n) names.push(n); }
        const gates = path.flatMap(e => [gateAt.get(`${e.a}|${e.road}`), gateAt.get(`${e.b}|${e.road}`)]).filter(Boolean) as string[];
        const link = plan.links.find(l => l.node === to);
        return { id: r.id, name: r.name, to: link ? `${link.to} (off-sheet)` : r.toName ?? to, length: D.get(to) ?? Infinity, roads: names, gates: [...new Set(gates)] };
      });
      const m1: Check = { id: 'M-1', group: 'Mission', rule: 'Every mission route reaches its end', value: out.map(r => `${r.id} ${f(r.length)} m`).join(', '), limit: 'finite', ok: out.every(r => r.length < Infinity) };
      return { checks: [m1], routes: out };
    },
  };
}
