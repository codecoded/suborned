// Regen orders from the plan: every tower site's `regen` (standards.buildings.towers.sites[]) and megastructures[].kit.regen, resolved and hashed.
// run.ts writes them to <out>/orders.json; regen.py builds one parameterised piece per order. Nothing the order states is hard-coded in the generator.
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';

export const PLAN = resolve(import.meta.dir, '../../contracts/world/harbour-district.plan.json');
type Rect = [number, number, number, number];
export type Order = {
  id: string; from: string; kind: 'tower' | 'megastructure'; hash: string;
  sites: { id: string; rect: Rect; storeys?: number }[]; regen: Record<string, unknown>; limits: Record<string, unknown>;
};
export type Orders = { plan: string; grid: { storey: number; bay: number; halfBay: number; deck: number; crownTip: number; setbackAbove: number }; pieces: Order[] };
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
  return { plan: 'contracts/world/harbour-district.plan.json', grid, pieces };
}
export const writeOrders = (to: string, planPath?: string) => writeFileSync(to, JSON.stringify(readOrders(planPath), null, 1) + '\n');
