// C-5 (brief 055, Datum): the floors a person stands on above the pavement are declared in the civic block (civic-block.json `floors`, from
// civic-block.adjust.json `floor` ops): each names the structure drawn there and its height; a flight's risers, rise and tread climb exactly to its
// landing, within standards.levels.connectors.stair; and every door stands on the floor the block declares where it is (a door 0.75 m up stands on a
// declared landing, a door on the pavement at 0). Opt-in on standards.levels.datum: a pinned plan and its block are read as they were made.
import type { Check } from './checks';
import { type Civic, floorAt } from './civic';
import { inPoly, lineDist, type V } from './geom';
import type { Derived, Plan } from './plan';

/** Where a person stands on the street level at p (plan frame), as height above the pavement: a floor, bay or drive the block declares (floorAt); else the pavement
 *  on a plot, a median or the circus island (0); else a carriageway or a foot path, which are walked at carriageway level (datum.carriageway − datum.pavement; the
 *  world draws foot paths 0.02 up, like bays), named by road or path id; else the pavement. */
export function standAt(plan: Plan, d: Derived, civ: Civic, p: V): { floor: number; on?: string; level: string } {
  const D = plan.standards.levels?.datum ?? { carriageway: 0, pavement: 0.18, bay: 0 }, b = floorAt(civ, p, D);
  if (b.on) return b;
  if (d.plots.some(x => inPoly(p, x.polyV)) || d.zones.some(z => z.kind === 'median' && z.poly && inPoly(p, z.poly))) return b;
  const c = plan.circus; if (c && Math.hypot(p[0] - c.c[0], p[1] - c.c[1]) <= c.ring[0]) return b; // the island (ring[0]) is pavement
  const low = Math.round((D.carriageway - D.pavement) * 1000) / 1000;
  const road = d.roads.find(r => lineDist(p, r.line) <= r.w / 2); if (road) return { floor: low, on: road.id, level: 'street' };
  const foot = plan.foot.find(f => lineDist(p, f.pts) <= f.w / 2); if (foot) return { floor: low, on: foot.id, level: 'street' };
  return b;
}

const TOL = 0.011, f2 = (x: number) => x.toFixed(2);

export function floorChecks(plan: Plan, civ: Civic): Check[] {
  const L = plan.standards.levels, D = L?.datum as { pavement: number; bay: number } | undefined;
  if (!D) return [];
  const bad: string[] = [], levels = new Set((L.list as { id: string }[]).map(l => l.id)), stair = L.connectors?.stair as { riser: number; going: number } | undefined;
  const near = (a: number, b: number, t = TOL) => Math.abs(a - b) <= t;
  for (const fl of civ.floors) {
    const s = civ.structures.find(x => x.id === fl.id), tag = `${fl.id} (${fl.kind})`;
    if (!levels.has(fl.level)) bad.push(`${tag}: level ${fl.level} is not in standards.levels`);
    if (!s) { bad.push(`${tag}: no structure ${fl.id} is drawn`); continue; }
    if (s.poly.length !== fl.poly.length || s.poly.some((p, i) => Math.hypot(p[0] - fl.poly[i]![0], p[1] - fl.poly[i]![1]) > TOL)) bad.push(`${tag}: its polygon is not its structure's`);
    if (fl.kind !== 'steps') { if (!near(fl.top, s.z + s.height)) bad.push(`${tag}: top ${fl.top} ≠ the structure's ${f2(s.z + s.height)}`); continue; }
    const { risers, rise, tread, foot, head } = fl as Required<typeof fl>, land = civ.floors.find(x => x.id === fl.landing);
    if (!risers || !rise || !tread || !foot || !head) { bad.push(`${tag}: a flight states risers, rise, tread, foot and head`); continue; }
    if (!land || land.kind === 'steps') { bad.push(`${tag}: climbs to ${fl.landing ?? 'nothing'}, which is no declared landing`); continue; }
    if (!near(risers * rise, land.top)) bad.push(`${tag}: ${risers} × ${rise} = ${f2(risers * rise)} ≠ its landing's top ${land.top}`);
    if (!near((risers - 1) * rise, fl.top) || !near(fl.top, s.z + s.height)) bad.push(`${tag}: top tread ${fl.top} ≠ ${risers - 1} × ${rise} or the structure's ${f2(s.z + s.height)}`);
    if (!near(Math.hypot(head[0] - foot[0], head[1] - foot[1]), (risers - 1) * tread, 0.02)) bad.push(`${tag}: foot to head ${f2(Math.hypot(head[0] - foot[0], head[1] - foot[1]))} m ≠ ${risers - 1} treads of ${tread}`);
    if (lineDist(head, [...land.poly, land.poly[0]!]) > 0.02 && !inPoly(head, land.poly)) bad.push(`${tag}: its head is not on its landing ${land.id}`);
    if (inPoly(foot, land.poly)) bad.push(`${tag}: its foot is on its landing`);
    if (stair && (rise > stair.riser + 1e-9 || tread < stair.going - 1e-9)) bad.push(`${tag}: rise ${rise} / tread ${tread} outside standards.levels.connectors.stair (riser ≤ ${stair.riser}, going ≥ ${stair.going})`);
  }
  const doors = [...civ.entrances, ...civ.serviceDoors];
  for (const e of doors) { const at = floorAt(civ, e.p as V, D); if (!near(at.floor, e.z)) bad.push(`${e.name}: the door stands at ${e.z} m, the block declares ${at.floor} m there${at.on ? ` (${at.on})` : ' (the pavement)'}`); }
  return [{
    id: 'C-5', group: 'Civic', rule: 'Every floor a person stands on above the pavement is declared in the block: its structure and height, a flight climbing exactly to its landing within the stair standard; every door stands on the floor declared where it is',
    value: `${civ.floors.length} floors (${civ.floors.map(x => `${x.id} ${x.top}`).join(', ') || 'none'}), ${doors.length} doors`, limit: `all, ± ${TOL} m`, ok: !bad.length, ...(bad.length ? { detail: bad.join('; ') } : {}),
    source: 'standards.levels.datum · civic-block.json floors (civic-block.adjust.json `floor`) · standards.levels.connectors.stair · brief 055 step 6b',
  }];
}
