// Regen orders from the plan: every tower site's `regen` (standards.buildings.towers.sites[]) and megastructures[].kit.regen, resolved and hashed.
// run.ts writes them to <out>/orders.json; regen.py builds one parameterised piece per order. Nothing the order states is hard-coded in the generator.
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';

export const PLAN = resolve(import.meta.dir, '../../contracts/world/harbour-district.plan.json');
type Rect = [number, number, number, number];
export type Order = {
  id: string; from: string; kind: 'tower' | 'megastructure' | 'signature'; hash: string;
  sites: { id: string; rect: Rect; storeys?: number }[]; regen: Record<string, unknown>; limits: Record<string, unknown>;
};
/** The plain pieces (no regen order) a bare `bun run decokit` also builds, beside the plan's regen orders (brief 061, Sky 2026-10-25; Splice): the crossing signal head and its clock ring, the dot rows the plan's lightsFit is fitted to, and the kerb and road ribbon cut without
 *  their brass. Each is a builder in tools/decokit (crossing_lens.py, crossing_flat.py, pave026.py, road_ribbon.py); the plan's sizes (signal.halo/top/hover/size, lights.dots) are fitted to what they build, and K-1 holds the export to them. A piece that is retired
 *  (kit.asset retired=) is not listed: its builder stays, `bun run decokit --only <id>` rebuilds it, and the catalogue keeps its last bytes until it is rejected. */
export const BUILD: readonly string[] = ['SignalLens', 'HaloRing', 'LightDotRow_2', 'LightDotRow_1', 'Kerb_Pearl_0', 'Kerb_Pearl_1', 'RoadRibbon_Straight_6m', 'RoadRibbon_Arc90_R4', 'RoadRibbon_Arc90_R8', 'PolicePost', 'MastCamera', 'CivicRobotDock', 'Dumpster_Civic', 'Lockers_Play', 'RecyclingBins_Play', 'VendingMachine_Cola_Play', 'VendingMachine_Snack_Play', 'PhoneBooth_Play', 'Hydrant_Play', 'BikeRack_Play', 'Bench_Play', 'UtilityCabinet_Play'];
export type Orders = { plan: string; grid: { storey: number; bay: number; halfBay: number; deck: number; crownTip: number; setbackAbove: number }; pieces: Order[]; /** plain pieces built beside the orders */ build: string[] };
const sha = (v: unknown) => createHash('sha256').update(JSON.stringify(v)).digest('hex');
export function readOrders(planPath = PLAN): Orders {
  const p = JSON.parse(readFileSync(planPath, 'utf8')), st = p.standards, b = st.buildings, t = b.towers;
  const deck = st.levels.list.find((l: { id: string }) => l.id === 'deck').z as number;
  const grid = { storey: b.upperStorey as number, bay: b.bay as number, halfBay: b.halfBay as number, deck, crownTip: t.crownTip as number, setbackAbove: b.setback.above as number };
  const sites = t.sites.filter((s: { regen?: unknown }) => s.regen) as { id: string; rect: Rect; storeys: number; regen: { from: string; same?: string } & Record<string, unknown> }[];
  const pieces: Order[] = [];
  for (const s of sites) { // a `same` site takes its pair's order (the placement mirrors it); each site is its own piece
    const regen = { ...(s.regen.same ? sites.find(x => x.id === s.regen.same)!.regen : s.regen) } as Record<string, unknown> & { from: string };
    const sitesOut = [{ id: s.id, rect: s.rect, storeys: s.storeys }];
    pieces.push({ id: `${regen.from}@${s.id}`, from: regen.from, kind: 'tower', sites: sitesOut, regen, limits: { crownTip: grid.crownTip }, hash: sha({ regen, sites: sitesOut, grid }) });
  }
  for (const m of p.megastructures as { id: string; footprint: Rect[]; legs: Rect[]; height: number; kit?: { regen?: { from: string; variant: string } & Record<string, unknown> } }[]) {
    const r = m.kit?.regen; if (!r) continue;
    const sitesOut = [{ id: m.id, rect: m.footprint[0]! }], limits = { legs: m.legs, height: m.height };
    pieces.push({ id: r.variant, from: r.from, kind: 'megastructure', sites: sitesOut, regen: r, limits, hash: sha({ regen: r, sites: sitesOut, limits, grid }) });
  }
  // brief 007 A: signatures[] (SpaceHQ_Portal): a regen order on a drum site; limits = its ground rect and portal
  for (const g of (p.signatures ?? []) as { id: string; ground: Rect; c: [number, number]; height: number; portal?: unknown; regen?: { from: string; variant: string } & Record<string, unknown> }[]) {
    const r = g.regen; if (!r) continue;
    const sitesOut = [{ id: g.id, rect: g.ground }], limits = { c: g.c, height: g.height, portal: g.portal };
    pieces.push({ id: r.variant, from: r.from, kind: 'signature', sites: sitesOut, regen: r, limits, hash: sha({ regen: r, sites: sitesOut, limits, grid }) });
  }
  return { plan: 'contracts/world/harbour-district.plan.json', grid, pieces, build: [...BUILD] };
}
export const writeOrders = (to: string, planPath?: string) => writeFileSync(to, JSON.stringify(readOrders(planPath), null, 1) + '\n');
