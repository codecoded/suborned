// CW-1, building colourways (brief 074, Sky 2026-10-28: "Do building colors"). Every building (FacadeKit.HostOf of a `buildings[]` mass) carries one colourway, a
// facade.json wall family, on every one of its masses; every facade piece of it carries the same; no two street neighbours (colourways.ts streetNeighbours) share
// one; the civic block's café and Records Office are `civic`, and no other building is. The spread (families and counts, neighbour wall dE2000 min, p10, median)
// is reported in the value; the median is reported, not held (brief 074 asks about 22 or more; the floor in colourways.ts lands it near 27).
import type { World } from '../../../contracts/world/world.schema';
import type { Check } from '../../plan/checks';
import { type WCtx, rule } from '../../plan/world-ctx';
import { CIVIC, COLOURWAY_SPEC, civicHost, hostOf, spreadOf, streetNeighbours } from '../gen/colourways';

export type ColourwayFault = { id: string; why: string };

/** The faults and the spread of a world's colourways. */
export function colourwayFaults(w: World): { faults: ColourwayFault[]; checked: number; spread: ReturnType<typeof spreadOf> } {
  const faults: ColourwayFault[] = [], of = new Map<string, string>();
  for (const m of w.buildings) {
    const h = hostOf(m.id), c = m.colourway;
    if (!c) { faults.push({ id: m.id, why: 'mass has no colourway' }); continue; }
    if (!COLOURWAY_SPEC.families[c]) faults.push({ id: m.id, why: `colourway ${c} is not a facade.json family` });
    else if ((c === CIVIC) !== civicHost(h)) faults.push({ id: m.id, why: c === CIVIC ? 'civic colourway outside the civic block' : `civic block building in ${c}` });
    const seen = of.get(h);
    if (seen === undefined) of.set(h, c); else if (seen !== c) faults.push({ id: m.id, why: `mass in ${c}, its building ${h} in ${seen}` });
  }
  for (const m of w.civic) if (civicHost(m.id) && m.colourway !== CIVIC && !m.hidden) faults.push({ id: m.id, why: `civic block mass in ${m.colourway ?? 'none'}, not civic` });
  for (const f of w.facades) {
    const b = of.get(f.building);
    if (b === undefined) faults.push({ id: f.id, why: `facade piece of ${f.building}, which has no colourway` });
    else if (f.colourway !== b) faults.push({ id: f.id, why: `facade piece in ${f.colourway ?? 'none'}, its building ${f.building} in ${b}` });
    if (f.instance && f.colourway && f.instance !== `${f.kit.id}|${f.colourway}`) faults.push({ id: f.id, why: `instance ${f.instance} is not <piece>|<colourway>` });
  }
  const pairs = streetNeighbours(w.frontages, new Set(of.keys()));
  for (const [p, q] of pairs) if (of.get(p) === of.get(q)) faults.push({ id: `${p} | ${q}`, why: `street neighbours share ${of.get(p)}` });
  return { faults, checked: w.buildings.length + w.facades.length + pairs.length, spread: spreadOf(of, pairs) };
}

const SOURCE = 'brief 074 (Sky 2026-10-28: "Do building colors"; the contract: one facade.json wall family per building, on the building and every facade module, no two street neighbours alike, wall dE2000 median about 22 or more) · '
  + 'tools/world/gen/colourways.ts (pick and repair) · unity/Assets/Suborned/Editor/World/facade.json families, areas, hostAreas · '
  + 'failing examples (colourways.test.ts): a street neighbour given its neighbour\'s family; a SideBay module left in `base` on a coloured building; a tower podium in another family from its body';

/** CW-1: every building one colourway, its facade pieces in it, no street neighbours alike (the world rule; `bun tools/plan/cli.ts` and `bun run world` run it). */
export function colourwayRules(c: WCtx): Check[] {
  const r = colourwayFaults(c.w), s = r.spread, de = s.de;
  return [rule('CW-1', 'Colour', `Every building has one colourway (a facade.json wall family) on all its masses and facade pieces, and no two street neighbours share one; ${s.buildings} buildings, ${s.pairs} neighbour pairs, `
    + `wall dE2000 min ${de?.min ?? '-'} p10 ${de?.p10 ?? '-'} median ${de?.median ?? '-'}; families ${Object.entries(s.families).map(([k, v]) => `${k} ${v}`).join(', ')}`,
    r.checked, r.faults.map(f => `${f.id}: ${f.why}`), '0 faults', SOURCE)];
}
