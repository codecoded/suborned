// The Handover mission as an overlay on the world (tools/plan/overlay.ts): its destinations give the walk origins, its points
// and routes get rules C-3 and M-1. The rules and their texts are unchanged from when they lived in the plan checker; recorded
// runs replay against them. The data is contracts/missions/handover/mission.json; a recorded run's pinned suborned.block/2
// and its plan's `routes` carry the same data and win over the file.
// Rule M-2 (brief 010 D, suborned.mission/3): the file's roles are cast from the generated people by tools/missions/handover/cast.ts, one person pinned per
// role by id; it fails naming each role nobody can fill. The people are expanded here from the promoted cast (tools/people/current.ts; Redline H1, 2026-10-15),
// never read from out/people/<plan id>.people.json, which any run of `bun run people` may have written: a stand-in, a file made for another mission or plan, an edit.
// A recorded run's layer has no roles, so M-2 is never part of a replay. A role's need, approach and regularOf are checked against
// contracts/people/people.rules.json (temperaments' refusals, needs' approaches; Motive §3, Sky Q4). Until brief 011's engine plays
// the mission, M-2 also reads the roles' static playability conditions (castRoles `{ play: true }`: tiedTo, leavesTo, waitsFor,
// alone; Suture's review of r013, 2026-10-15), each failing with the role and the condition.
// Rule V-1 (brief 003 B; brief 011, Vantage 2026-10-15): every mission point is seen at the play camera past architecture, without the fade
// (tools/missions/handover/visibility.ts), measured on the generated world. Like M-2 it reads the file's mission only, so recorded runs never meet it.
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Check } from '../../plan/checks';
import { type Civic, toPlanFn } from '../../plan/civic';
import { standAt } from '../../plan/floors';
import type { World } from '../../../contracts/world/world.schema';
import { routeVia } from '../../plan/graph';
import { lineDist, inPoly, type V } from '../../plan/geom';
import type { Overlay, Route } from '../../plan/overlay';
import { currentPeople } from '../../people/current';
import { castRoles, type Castable, type CastMission, type CastRules } from './cast';
import { summonOpts } from './summon';
import { type Derived, derive } from '../../plan/plan';
import { generated } from '../../plan/world-ctx';
import { blockersFor, visibility, v1 } from './visibility';

const ROOT = resolve(import.meta.dir, '../../..'), FILE = resolve(ROOT, 'contracts/missions/handover/mission.json');
/** The people a cast reads: the expanded people file's places and persons (contracts/people/people.schema.ts). */
export type PeopleIn = { plan: { id: string; version: string }; places: { id: string; kind: string; name: string; at: number[]; plot?: string }[]; persons: Castable[] };
/** What M-2 reads of the people rules: each temperament's refusals and each need's approaches. */
export const castRules = (file = resolve(ROOT, 'contracts/people/people.rules.json')): CastRules => {
  const r = JSON.parse(readFileSync(file, 'utf8'));
  return { temperaments: r.temperaments, needs: r.needs.list, ...(r.secrets ? { secrets: r.secrets.list } : {}) };
};
/** Where `bun run people` writes the people for the Unity people layer. M-2 does not read it. */
export const peopleFile = (plan: { id: string }) => resolve(ROOT, `out/people/${plan.id}.people.json`);
const f = (x: number, d = 0) => x.toFixed(d);

/** The layer a run recorded: destinations in its pinned block, routes in its plan. Undefined for a world-only block. */
export const legacyOverlay = (plan: any, block: any): Overlay | undefined => (block.destinations ? build(plan, block.destinations, plan.routes ?? []) : undefined);
/** The layer from contracts/missions/handover/mission.json, or the run's own when the block carries one. `people`: the cast to read
 *  (default: the promoted cast of the plan, expanded when M-2 runs). `mission`: a mission in place of the file (tests). */
export const overlay = (plan: any, block: any, people?: PeopleIn, mission?: CastMission & { routes: any[]; destinations: any[] }): Overlay =>
  legacyOverlay(plan, block) ?? ((m: any) => build(plan, m.destinations, m.routes, m, people))(mission ?? JSON.parse(readFileSync(FILE, 'utf8')));

/** M-2: every role the mission names is cast from the people, one pinned per role. */
function castCheck(plan: any, civ: { bays: { id: string; poly: V[] }[] }, m: CastMission, given?: PeopleIn): Check {
  const rule = { id: 'M-2', group: 'Mission', rule: 'Every role the mission names is cast from the city\'s people, there when needed, one pinned by id, and the cast can play it', limit: 'all roles' };
  const now = given ? undefined : currentPeople(plan, m);
  if (now && !now.people) return { ...rule, value: `0/${m.roles.length} roles`, ok: false, detail: `no people to cast from: ${now.problems.join('; ')}` };
  const people: PeopleIn = given ?? (now!.people as PeopleIn);
  if (people.plan.id !== plan.id || people.plan.version !== plan.version) return { ...rule, value: `0/${m.roles.length} roles`, ok: false, detail: `the people are of plan ${people.plan.id} ${people.plan.version}, not ${plan.id} ${plan.version} (bun run people)` };
  const place = new Map(people.places.map(p => [p.id, p]));
  const c = castRoles(people.persons, m, {
    toPlan: toPlanFn(plan.civic), placeAt: id => place.get(id)?.at as V | undefined,
    polyOf: id => { const p = place.get(id); return p?.kind === 'bay' ? civ.bays.find(b => b.id === p.name)?.poly : undefined; },
    plotOf: id => place.get(id)?.plot,
  }, castRules(), { play: true, summon: summonOpts(plan, derive(plan), civ as any, m as any) });
  const bad = c.filter(x => !x.ok);
  return { ...rule, value: c.map(x => `${x.role} ${x.pinned ?? '-'}`).join(', '), ok: !bad.length, detail: (bad.length ? bad : c).map(x => `${x.role}: ${x.why}`).join('; ') };
}

/** M-3 (brief 055, Datum): every point the mission stands a person on names its level, and states the floor it stands on where that is not the walk of its level;
 *  the stated floor is the one the civic block declares there (a landing, a flight's tread, a bay at carriageway level; floorAt) or the carriageway or foot path
 *  it stands in (standAt), and a point on a deck or the
 *  concourse stands on a walkable piece of that level in the generated world. So a deck mission, or a door moved up a flight, cannot stand people on the slab
 *  below. Like M-2 and V-1 it reads the file's mission only, never a recorded run's layer. */
type LevelPoint = { name?: string; id?: string; at?: V; structure?: string; level?: string; on?: string; floor?: number };
export function levelCheck(plan: any, d: Derived, civ: Civic, m: { destinations: LevelPoint[]; crew: LevelPoint[]; points?: LevelPoint[] }, w: World): Check {
  const toPlan = toPlanFn(plan.civic), L = plan.standards.levels, ids = new Set((L.list as { id: string }[]).map(l => l.id));
  const centre = (sid: string): V | undefined => { const st = civ.structures.find(x => x.id === sid); return st ? [st.poly.reduce((a, q) => a + q[0], 0) / st.poly.length, st.poly.reduce((a, q) => a + q[1], 0) / st.poly.length] : undefined; };
  const walkOn = (lv: string): V[][] => [...w.levels.filter(l => l.name === lv || l.id === lv).flatMap(l => l.decks.map(x => x.ring as V[])), ...(lv === 'concourse' ? w.transit.stations.map(s => s.ring as V[]) : [])];
  const all = [...m.destinations.map(p => ({ ...p, tag: p.name! })), ...m.crew.map(p => ({ ...p, tag: `crew ${p.name}` })), ...(m.points ?? []).map(p => ({ ...p, tag: p.id! }))];
  const bad: string[] = [], raised: string[] = [];
  for (const p of all) {
    const at = p.at ? toPlan(p.at) : p.structure ? centre(p.structure) : undefined;
    if (!at) { bad.push(`${p.tag}: stands nowhere`); continue; }
    if (!p.level || !ids.has(p.level)) { bad.push(`${p.tag}: level ${p.level ?? 'not stated'}${p.level ? ' is not in standards.levels' : ''}`); continue; }
    if (p.level !== 'street') { if (!walkOn(p.level).some(r => inPoly(at, r))) bad.push(`${p.tag}: on level ${p.level}, but no ${p.level} piece is walkable at plan ${at.map(v => v.toFixed(1)).join(', ')}`); continue; }
    const want = standAt(plan, d, civ, at), said = p.floor ?? 0;
    if (Math.abs(said - want.floor) > 0.011 || (p.on ?? undefined) !== want.on) bad.push(`${p.tag}: states ${p.on ? `${p.on} ${said}` : 'the pavement'}, it stands on ${want.on ? `${want.on} ${want.floor}` : 'the pavement 0'}`);
    else if (want.on) raised.push(`${p.tag} ${want.on} ${want.floor}`);
  }
  return {
    id: 'M-3', group: 'Mission', rule: 'Every mission point names its level and, off the walk of its level, the floor it stands on: the floor the civic block declares there (landing, tread, bay) or the carriageway or foot path it stands in, or a walkable piece of its deck or concourse',
    value: `${all.length - bad.length}/${all.length}`, limit: 'all, ± 0.011 m', ok: !bad.length, detail: bad.length ? bad.join('; ') : `off the pavement: ${raised.join(', ') || 'none'}`,
    source: 'standards.levels.datum · civic-block.json floors, bays, drives · plan roads and foot paths · brief 055 step 6c',
  };
}

function build(plan: any, destinations: { name: string; at: V; surface: string }[], routes: { id: string; name: string; from: string; via?: string[]; to: string[]; toName?: string }[], cast?: CastMission, people?: PeopleIn): Overlay {
  const toPlan = toPlanFn(plan.civic), points = destinations.map(d => ({ name: d.name, p: toPlan(d.at) }));
  return {
    origins: p => (p.standards.walking.from as string[]).map(name => ({ name, p: points.find(x => x.name === name)!.p })),
    civic: ({ d, civ }) => {
      const lost = points.filter(pt => !d.plots.some(p => inPoly(pt.p, p.polyV)) && !d.roads.some(r => lineDist(pt.p, r.line) <= r.w / 2));
      const c3: Check = { id: 'C-3', group: 'Civic', rule: 'Mission points land on a plot or a street', value: `${points.length - lost.length}/${points.length}`, limit: 'all', ok: !lost.length, detail: lost.map(p => p.name).join(', ') || undefined };
      return cast ? [c3, castCheck(plan, civ, cast, people), levelCheck(plan, d, civ, cast as any, generated(plan, d, civ).w), v1(visibility(cast as any, civ, plan, blockersFor(generated(plan, d, civ).w, civ, plan)))] : [c3];
    },
    routes: ({ plan, d, E, roadById }) => {
      const gateAt = new Map(plan.gates.map(g => [`${g.node}|${g.road}`, g.name]));
      const out: Route[] = routes.map(r => {
        const rp = (() => { try { return routeVia(E, r); } catch { return undefined; } })(); // a route that reaches no end has length Infinity (M-1 fails it); `via` nodes make a route go the way it is meant to (brief 059: the rival car comes by Orbit Walk, not the shortest road)
        const path = rp?.edges ?? [], to = rp?.to ?? r.to[0]!, names: string[] = [];
        for (const e of path) { const n = roadById.get(e.road)!.name; if (names[names.length - 1] !== n) names.push(n); }
        const gates = path.flatMap(e => [gateAt.get(`${e.a}|${e.road}`), gateAt.get(`${e.b}|${e.road}`)]).filter(Boolean) as string[];
        const link = plan.links.find(l => l.node === to);
        return { id: r.id, name: r.name, to: link ? `${link.to} (off-sheet)` : r.toName ?? to, length: rp?.length ?? Infinity, roads: names, gates: [...new Set(gates)] };
      });
      const m1: Check = { id: 'M-1', group: 'Mission', rule: 'Every mission route reaches its end', value: out.map(r => `${r.id} ${f(r.length)} m`).join(', '), limit: 'finite', ok: out.every(r => r.length < Infinity) };
      return { checks: [m1], routes: out };
    },
  };
}
