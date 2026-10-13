// Applies Datum's and Vantage's skyline proposals to a copy of the plan. Pure: the same inputs give the same plan.
// Rail lines and stations go through the transit apply (tools/levels/apply.ts, unchanged); signatures, tower sites
// and portals are upserts by id. It never edits a standard other than the tower sites (a run must not move the
// goalposts): the transit waivers the transit apply drops are put back. Only sky rail lines are removed or replaced;
// tram lines are not this objective's. What it cannot apply is a problem routed to its owner, never a silent fix: an
// unknown plot, area, structure, line or kit piece, a rejected piece, a concept tile outside the index. The timetable
// the stations move is derived again afterwards (retime.ts).
import type { Plan } from '../../plan/plan';
import { apply } from '../apply';
import type { Stops } from '../schemas';
import type { PortalEdit, SignatureEdit, SiteEdit, SkylineOut, StationItem, StationsOut } from './schemas';

export type Owner = 'Datum' | 'Vantage';
export type Problem = { owner: Owner; text: string };
/**
 * What an edit may name besides the plan: the concept tiles, and the kit pieces with their status (adopted, candidate,
 * rejected).
 */
export type Known = { tiles: ReadonlySet<string>; kit: ReadonlyMap<string, string> };
export type Applied = { plan: Plan; problems: Problem[] };
type Entry = { id: string; [key: string]: unknown };
type Part = Record<string, unknown> | undefined;

const sameId = (id: string) => (e: Entry) => e.id === id;
const datum = (text: string): Problem => ({ owner: 'Datum', text });

/** Replaces the entry that has the id, or appends. */
function upsert(list: Entry[], next: Entry) {
  const i = list.findIndex(sameId(next.id));
  if (i < 0) list.push(next); else list[i] = next;
}

/** `edit` laid over `old`, one level deeper for the named nested objects: a nested field left out keeps its value. */
function merged(old: Entry | undefined, edit: Record<string, unknown>, nested: string[]): Entry {
  const next: Entry = { ...old, ...edit } as Entry;
  for (const k of nested) {
    if (edit[k] !== undefined) next[k] = { ...(old?.[k] as object | undefined), ...(edit[k] as object) };
  }
  return next;
}

function checkTiles(here: string, tiles: string[], known: Known, problems: Problem[]) {
  for (const t of tiles) {
    if (!known.tiles.has(t)) problems.push(datum(`${here}: concept tile "${t}" is not in the concept index`));
  }
}

function checkPiece(
  owner: Owner, here: string, field: string, piece: string | undefined, known: Known, problems: Problem[],
) {
  if (piece === undefined) return;
  const status = known.kit.get(piece), what = `${here}: ${field} ${piece}`;
  if (status === undefined) problems.push({ owner, text: `${what} is not a piece in the kit catalogue` });
  else if (status === 'rejected') problems.push({ owner, text: `${what} is rejected in the kit catalogue` });
}

function twice(kind: string, ids: string[], problems: Problem[]) {
  const seen = new Set<string>();
  for (const id of ids) {
    if (seen.has(id)) problems.push(datum(`${kind} ${id} is proposed twice`));
    seen.add(id);
  }
}

function editSignature(plan: Plan, e: SignatureEdit, known: Known, problems: Problem[]) {
  const here = `signature ${e.id}`, areas = new Set(plan.areas.map(a => a.id));
  const { concepts, ...rest } = e;
  checkTiles(here, concepts, known, problems);
  const list = (plan.signatures ??= []) as unknown as Entry[];
  const next = merged(list.find(sameId(e.id)), { ...rest, conceptTiles: concepts }, ['regen', 'portal']);
  checkSignaturePiece(here, e, (next.regen as { variant?: string } | undefined)?.variant, known, problems);
  for (const a of e.areas) if (!areas.has(a)) problems.push(datum(`${here}: no such environmental area ${a}`));
  upsert(list, next);
}

/**
 * The piece a signature is built from. When it is regenerated (`regen.variant`), the variant is the piece that is
 * built, so the variant is judged and the base piece is not: the plan's SpaceHQ is a rejected piece with an adopted
 * variant, and that is correct as it stands. A variant that is not in the catalogue is a problem.
 */
function checkSignaturePiece(
  here: string, e: SignatureEdit, variant: string | undefined, known: Known, problems: Problem[],
) {
  const named = e.regen?.variant !== undefined || (e.kit !== undefined && variant !== undefined);
  if (named) checkPiece('Datum', here, 'regen.variant', variant, known, problems);
  else checkPiece('Datum', here, 'kit', e.kit, known, problems);
}

function editSite(plan: Plan, e: SiteEdit, known: Known, problems: Problem[]) {
  const here = `tower site ${e.id}`, { concepts, ...rest } = e;
  checkTiles(here, concepts, known, problems);
  if (!plan.plots.some(p => p.id === e.plot)) problems.push(datum(`${here}: no such plot ${e.plot}`));
  checkPiece('Datum', here, 'regen.from', e.regen?.from, known, problems);
  const list = plan.standards.buildings.towers.sites as Entry[];
  upsert(list, merged(list.find(sameId(e.id)), { ...rest, conceptTiles: concepts }, ['regen']));
}

function editPortal(plan: Plan, e: PortalEdit, known: Known, problems: Problem[]) {
  const here = `portal ${e.structure}/${e.id}`, { structure, concepts, ...rest } = e;
  checkTiles(here, concepts, known, problems);
  const megas = plan.megastructures as { id: string; portals?: Entry[] }[] | undefined;
  const mega = megas?.find(m => m.id === structure);
  if (!mega) { problems.push(datum(`${here}: no such megastructure ${structure}`)); return; }
  const list = (mega.portals ??= []);
  upsert(list, merged(list.find(sameId(e.id)), { ...rest, conceptTiles: concepts }, ['opening']));
}

function remove(plan: Plan, gone: SkylineOut['remove'], problems: Problem[]) {
  const sites = plan.standards.buildings.towers.sites as Entry[];
  const signatures = (plan.signatures ?? []) as unknown as Entry[];
  const drop = (list: Entry[], ids: string[], kind: string) => {
    for (const id of ids) {
      const i = list.findIndex(sameId(id));
      if (i < 0) problems.push(datum(`remove: no such ${kind} ${id}`)); else list.splice(i, 1);
    }
  };
  drop(sites, gone.towerSites, 'tower site');
  drop(signatures, gone.signatures, 'signature');
}

/**
 * The sky rail ids Datum may remove. A tram line, or an id that is no line at all, is a problem and is not passed on:
 * the transit apply would delete a tram line and ignore the unknown id.
 */
function railToRemove(input: Plan, ids: string[], problems: Problem[]): string[] {
  const sky = new Set(input.skyrail.map(l => l.id)), tram = new Set(input.tram.map(l => l.id));
  const valid: string[] = [];
  for (const id of ids) {
    if (sky.has(id)) valid.push(id);
    else if (tram.has(id)) {
      problems.push(datum(`rail.remove ${id}: a tram line; the skyline objective removes sky rail lines only`));
    }
    else problems.push(datum(`rail.remove: no such sky rail line ${id}`));
  }
  return valid;
}

/** The sky rail line ids once Datum's rail edits are in: Vantage's stations may only be on these. */
function skyLines(input: Plan, lines: { id: string }[], removed: string[]): Set<string> {
  const ids = new Set(input.skyrail.map(l => l.id));
  for (const l of lines) ids.add(l.id);
  for (const id of removed) ids.delete(id);
  return ids;
}

/** A station laid over the one of the same id in the plan: `in`, `kit` and `kitWhy` left out keep their value. */
function stationOver(input: Plan, s: StationItem): StationItem {
  const old = input.skyrail.find(l => l.id === s.line)?.stations?.find(x => x.id === s.id) as Part;
  const keep = ['in', 'kit', 'kitWhy'].filter(k => old?.[k] !== undefined).map(k => [k, old![k]]);
  return { ...Object.fromEntries(keep), ...s } as StationItem;
}

function checkStations(plan: Plan, stops: StationItem[], known: Known, problems: Problem[]) {
  const megas = (plan.megastructures ?? []) as { id: string }[];
  const inside = new Set([
    ...(plan.signatures ?? []).map(s => `signatures/${s.id}`), ...megas.map(m => `megastructures/${m.id}`),
  ]);
  for (const s of stops) {
    const here = `station ${s.line}/${s.id}`;
    checkPiece('Vantage', here, 'kit', s.kit, known, problems);
    if (s.in !== undefined && !inside.has(s.in)) {
      problems.push({ owner: 'Vantage', text: `${here}: no such structure ${s.in}` });
    }
  }
}

export function applySkyline(
  input: Plan, outs: { datum?: SkylineOut; vantage?: StationsOut }, version: string, known: Known,
): Applied {
  const problems: Problem[] = [], { datum: d, vantage: v } = outs;
  const removed = railToRemove(input, d?.rail.remove ?? [], problems);
  const sky = skyLines(input, d?.rail.lines ?? [], removed);
  const stops = (v?.stops ?? []).filter(s => sky.has(s.line));
  for (const s of v?.stops ?? []) {
    if (sky.has(s.line)) continue;
    const text = `station ${s.line}/${s.id}: the skyline objective places sky rail stations only`;
    problems.push({ owner: 'Vantage', text });
  }
  const rail = d && { lines: d.rail.lines, remove: removed, notes: [], sources: [], handoffs: [] };
  const placed = v && {
    stops: stops.map(s => stationOver(input, s)), notes: [], sources: [], handoffs: [], answers: [],
  };
  const base = apply(input, { datum: rail, vantage: placed as unknown as Stops | undefined }, version);
  const plan = base.plan as Plan;
  // the transit apply drops the waivers; a skyline run keeps every standard
  const waived = input.standards.transit?.waived;
  if (waived !== undefined && plan.standards.transit) plan.standards.transit.waived = structuredClone(waived);
  for (const text of base.problems) problems.push({ owner: text.startsWith('stop') ? 'Vantage' : 'Datum', text });
  if (d) {
    twice('signature', d.signatures.map(e => e.id), problems);
    twice('tower site', d.towerSites.map(e => e.id), problems);
    remove(plan, d.remove, problems);
    for (const e of d.signatures) editSignature(plan, e, known, problems);
    for (const e of d.towerSites) editSite(plan, e, known, problems);
    for (const e of d.portals) editPortal(plan, e, known, problems);
  }
  if (v) checkStations(plan, stops, known, problems);
  return { plan, problems };
}

/** The kit catalogue (a kit.json or its digest) as piece id → status. */
export const kitStatus = (catalogue: { assets: { id: string; status: string }[] }): Map<string, string> =>
  new Map(catalogue.assets.map(a => [a.id, a.status]));
