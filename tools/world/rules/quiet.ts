// R-HIDE, R-STASH, R-REPORT, R-DOCK (brief 013 C): the takedown, report and response places the world exports (`w.takedown`), held by what the engine will walk.
// R-HIDE    only a closed container (a street item whose kit piece carries `tags.stash`) or a service door hides a body; nothing else in the street carries `stash`.
// R-STASH   every service door is a stash, and has a container within 10 m of it that stands clear of every building and ground solid, or says why none stands there.
// R-REPORT  every walkable lattice sample of the block's window is within `reach.report` m (walk, on the engine's grid) of a door of a staffed unit, a phone booth or a call box.
// R-DOCK    every sample is within `reach.origin` m of a robot dock or a police post (the Records Office's doors are posts).
// Sources: briefs/013/vantage-quiet-places.md §2 (hide), briefs/013/pulse-witnesses.md §8.3 (R-REPORT, R-DOCK; reach 40 m, proposed rulebook values report-reach-m, dock-reach-m), briefs/013/motive-takedowns.md §2.4, §8.7.
import type { StreetItem } from '../../../contracts/world/world.schema';
import type { Check } from '../../plan/checks';
import { type V, dist } from '../../plan/geom';
import { ringGap } from '../../plan/street-zones';
import { INF, type WalkGrid, reachField, toBlockFn, walkGrid } from '../../plan/walk-grid';
import { type WCtx, rule, stat } from '../../plan/world-ctx';
import { QUIET, lattice } from '../civic/quiet';
import { catalogued, cataloguedFor } from '../gen/kitrefs';

const G = 'Takedown';
const SRC = 'briefs/013 (Vantage quiet places §2, Pulse witnesses §8.3, Motive §2.4 and §8.7) · Sky 2026-10-16 answers 5 and 7 · plan.civic block/4 entrance kinds';
const grids = new WeakMap<object, WalkGrid>();
const gridOf = (c: WCtx) => { let g = grids.get(c.civ); if (!g) grids.set(c.civ, g = walkGrid(c.plan, c.d, c.civ)); return g; };

export function quietRules(c: WCtx): Check[] {
  const t = c.w.takedown; if (!t) return [];
  const out: Check[] = [], toBlock = toBlockFn(c.plan.civic), grid = gridOf(c), byId = new Map<string, StreetItem>(c.w.street.map(s => [s.id, s]));

  // R-HIDE
  const bad: string[] = [], stashed = c.w.street.filter(s => s.stash);
  for (const s of stashed) {
    const a = (s.kit ? catalogued(s.kit.id) : undefined) ?? cataloguedFor(s.kind);
    if (!a?.tags.stash) bad.push(`${s.id}: kit piece ${a?.id ?? s.kind} carries no stash tag`);
    else if (a.tags.stash.holds !== s.stash!.holds) bad.push(`${s.id}: holds ${s.stash!.holds}, its piece ${a.id} holds ${a.tags.stash.holds}`);
  }
  for (const s of c.w.street) if (!s.stash && s.kit && catalogued(s.kit.id)?.tags.stash) bad.push(`${s.id}: ${s.kit.id} is a stash piece and the item does not say so`);
  for (const s of t.stashes) {
    if (s.kind === 'container') { const it = byId.get(s.item ?? ''); if (!it?.stash) bad.push(`${s.id}: ${s.item} is no container (no stash on the street item)`); }
    else if (t.entrances.find(e => e.id === s.of)?.kind !== 'service') bad.push(`${s.id}: ${s.of} is no service door`);
  }
  out.push(rule('R-HIDE', G, 'Only a closed container (a street item whose kit piece is tagged stash, holds >= 1) or a service door hides a body; a street item carries `stash` only if its piece does', stashed.length + t.stashes.length, bad, 'all', SRC));

  // R-STASH
  const bad2: string[] = [], service = t.entrances.filter(e => e.kind === 'service');
  for (const e of service) {
    const door = t.stashes.find(s => s.kind === 'door' && s.of === e.id), box = t.stashes.find(s => s.kind === 'container' && s.of === e.id);
    if (!door) { bad2.push(`${e.name} is no stash`); continue; }
    if (!box && !door.bare) { bad2.push(`${e.name} has no container and says no why`); continue; }
    if (box) {
      if (box.holds < 1) bad2.push(`${e.name}: its container holds ${box.holds}`);
      if (dist(box.at, e.at) > QUIET.kerbReach + 1) bad2.push(`${e.name}: its container is ${dist(box.at, e.at).toFixed(1)} m off`);
      if (!grid.free(toBlock(box.stand))) bad2.push(`${e.name}: nobody can stand at its container`);
      if (box.ring && [...c.civ.buildings.map(b => b.poly), ...(c.civ.solids ?? [])].some(r => ringGap(box.ring!, r) < 0.02 && !(r === c.civ.buildings.find(b => b.id === e.building)?.poly))) bad2.push(`${e.name}: its container stands in a solid`);
    }
  }
  out.push(rule('R-STASH', G, 'Every service door is a stash and has a container beside it (within 10 m, clear of every solid, reachable) or says why none stands there', service.length, bad2, 'all', SRC));

  // R-REPORT, R-DOCK
  const doors = t.report.filter(r => r.kind === 'door').map(r => toBlock(r.stand));
  const samples = lattice(grid, doors, t.reach.lattice);
  const reach = (pts: { stand: V }[], m: number) => { const f = reachField(grid, pts.map(p => toBlock(p.stand))); const lim = Math.round(m * 100); return samples.filter(s => f[s]! > lim || f[s] === INF); };
  const lost = (pts: { id: string; stand: V }[]) => pts.filter(p => !grid.free(toBlock(p.stand))).map(p => `${p.id}: nobody can stand there (no free ground)`);
  const missing = (label: string, pts: { id: string; at: V; item?: string }[]) => pts.flatMap(p => (p.item ? [byId.get(p.item)] : []).flatMap(it => !it ? [`${p.id}: no street item ${p.item}`] : dist(it.at, p.at) > 0.01 ? [`${p.id}: not where its street item is`] : []));
  const far1 = reach(t.report, t.reach.report), far2 = reach(t.origins, t.reach.origin);
  const where = (cells: number[]) => cells.slice(0, 4).map(s => { const i = Math.floor(s / grid.ny), j = s % grid.ny, q = grid.centre(i, j); return `${q[0].toFixed(0)},${q[1].toFixed(0)}`; });
  out.push(rule('R-REPORT', G, `Every walkable lattice sample (${t.reach.lattice} m) of the block's window is within ${t.reach.report} m (walk, on the engine's grid) of a door, phone booth or call box`, samples.length, [...far1.length ? [`${far1.length} samples farther, e.g. block ${where(far1).join(' · ')}`] : [], ...lost(t.report), ...(samples.length ? [] : ['no walkable ground reaches a door']), ...missing('report', t.report)], 'all', SRC));
  out.push(rule('R-DOCK', G, `Every walkable lattice sample is within ${t.reach.origin} m (walk) of a robot dock or police post (the Records Office's doors are posts)`, samples.length, [...far2.length ? [`${far2.length} samples farther, e.g. block ${where(far2).join(' · ')}`] : [], ...lost(t.origins), ...missing('origin', t.origins)], 'all', SRC));
  out.push(stat('R-REPORT/n', G, 'Call boxes, police posts, docks and containers the world stands for the block (informational)', `${t.report.filter(r => r.kind === 'call-box').length} call boxes, ${t.origins.filter(o => o.kind === 'post' && o.item).length} police posts, ${t.origins.filter(o => o.kind === 'dock').length} docks, ${t.stashes.filter(s => s.kind === 'container').length} containers`, '-', true, SRC, `report: median ${t.coverage.report.medianM} m, worst ${t.coverage.report.worstM} m, doors alone ${t.coverage.report.baselineWithin / 10}% within reach; origins: median ${t.coverage.origin.medianM} m, worst ${t.coverage.origin.worstM} m, docks and Office doors alone ${t.coverage.origin.baselineWithin / 10}%`));
  return out;
}
