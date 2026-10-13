// Proposals for the skyline tests, built from the frozen plan (fixtures/plan.json): each restates the fields an edit
// must state and changes what a test needs changed. The plan the tests run on never moves, so these stay valid when the
// live plan does.
import { resolve } from 'node:path';
import type { Plan } from '../../../plan/plan';
import type { PortalEdit, SignatureEdit, SiteEdit, SkylineOut, StationItem, StationsOut } from '../schemas';
import { NO_SKYLINE, NO_STATIONS } from '../schemas';

export const fixtures = resolve(import.meta.dir, 'fixtures');
export const PLAN = `${fixtures}/plan.json`;
export const KIT = `${fixtures}/kit.digest.json`;
export const loadPlan = async (): Promise<Plan> => Bun.file(PLAN).json();

type Rec = Record<string, unknown>;
const pick = (from: Rec, keys: string[]) =>
  Object.fromEntries(keys.filter(k => from[k] !== undefined).map(k => [k, from[k]]));
const byId = (list: unknown[], id: string): Rec => (list as Rec[]).find(x => x.id === id)!;
const SOURCES = ['__CHUNK__']; // the stub puts the first passage id of the prompt here

export function site(plan: Plan, id: string, over: Partial<SiteEdit> = {}): SiteEdit {
  const s = byId(plan.standards.buildings.towers.sites, id);
  const keys = ['id', 'plot', 'rect', 'storeys', 'crownType', 'crownWhy', 'why'];
  return { ...pick(s, keys), concepts: ['blocks/mixed_skyline'], ...over } as SiteEdit;
}

export function signature(plan: Plan, id: string, over: Partial<SignatureEdit> = {}): SignatureEdit {
  const s = byId(plan.signatures ?? [], id);
  const keys = ['id', 'name', 'kind', 'role', 'areas', 'c', 'ground', 'groundWhy', 'crownType', 'crownWhy', 'why'];
  return { ...pick(s, keys), concepts: ['blocks/saucer_spires'], ...over } as SignatureEdit;
}

export function portal(plan: Plan, structure: string, id: string, over: Partial<PortalEdit> = {}): PortalEdit {
  const m = byId(plan.megastructures ?? [], structure), p = byId(m.portals as unknown[], id);
  const opening = pick(p.opening as Rec, ['clearHalfWidth', 'z']);
  const keys = ['id', 'kind', 'beams', 'rect', 'z', 'axis', 'why'];
  return { structure, ...pick(p, keys), opening, concepts: ['blocks/rail_through'], ...over } as PortalEdit;
}

/** A sky rail line's stations as Vantage restates them: the transit fields only (in, kit, kitWhy stay the plan's). */
export function stations(plan: Plan, line = 'S1'): StationItem[] {
  const l = plan.skyrail.find(x => x.id === line)!;
  const keys = ['id', 'name', 'path', 'at', 'length', 'interchange', 'fronts'];
  return (l.stations as Rec[]).map(s => ({ line, ...pick(s, keys) }) as StationItem);
}

export const datum = (over: Partial<SkylineOut>): SkylineOut => ({ ...NO_SKYLINE, sources: SOURCES, ...over });
export const vantage = (over: Partial<StationsOut> = {}): StationsOut =>
  ({ ...NO_STATIONS, sources: SOURCES, ...over });
export const PASS = { verdict: 'pass', summary: 'The candidate holds the standards.', faults: [], sources: SOURCES };

/** A proposal the checker has nothing against: three upserts restating entries with concept tiles, a piece request. */
export function tidy(plan: Plan): SkylineOut {
  return datum({
    signatures: [signature(plan, 'ocean-liner')],
    towerSites: [site(plan, 'crescent-terrace')],
    portals: [portal(plan, 'parade-galleries', 'parade-gate')],
    pieceRequests: [
      { site: 'crescent-terrace', piece: 'Tower_Terrace', size: [30, 30, 120], why: 'a wider crown terrace' },
    ],
    notes: ['restated three entries with their concept tiles'],
  });
}
