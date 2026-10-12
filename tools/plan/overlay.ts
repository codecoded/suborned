// An overlay is a layer of rules that reads a world but is not part of it (the world stays places only). It supplies where
// walks start, extra civic rules and route rules, and is handed to check() by whoever runs one.
import type { Check } from './checks';
import type { Civic } from './civic';
import type { Edge } from './graph';
import type { Derived, Plan } from './plan';

export type Route = { id: string; name: string; to: string; length: number; roads: string[]; gates: string[] };
export type Ctx = { plan: Plan; d: Derived; civ: Civic; E: Edge[]; roadById: Map<string, Derived['roads'][number]> };
export type Overlay = {
  origins: (plan: Plan, civ: Civic) => Civic['walkFrom'];
  civic: (c: Ctx) => Check[];
  routes: (c: Ctx) => { checks: Check[]; routes: Route[] };
};
