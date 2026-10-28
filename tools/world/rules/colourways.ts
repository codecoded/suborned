// CW-1, building colourways (brief 074, Sky 2026-10-28: "Do building colors"; brief 075 old palette, Sky 2026-10-29: "it needs to go back to old town"). Every
// building (FacadeKit.HostOf of a `buildings[]` mass) carries one colourway, a facade.json family, on every one of its masses; every facade piece of it carries
// the same; the civic block's café and Records Office are `civic`, and no other building is. The old DecoKitColourways scheme: a building's colourway is its
// area's ensemble (facade.json areas) unless the old high street's walk painted it (oldStreetFamilies: the Soft palette on the old-street roads), and no two
// painted buildings stand side by side in one run (street neighbours on one plot; old HighStreet: "painted buildings always have cream or stone neighbours";
// across a side street the walk starts again, painted first). Neighbours sharing an
// ensemble is the scheme, not a fault (the 074 "no two neighbours alike" rule went with the 12 families). The spread (families and counts) is reported.
import type { World } from '../../../contracts/world/world.schema';
import type { Check } from '../../plan/checks';
import { type WCtx, rule } from '../../plan/world-ctx';
import { ctxOf as genCtx } from '../gen/common';
import { CIVIC, COLOURWAY_SPEC, civicHost, ensembleOf, hostOf, oldStreetFamilies, piecesAt, spreadOf, streetNeighbours } from '../gen/colourways';
import { OLD_STREET } from '../gen/oldstreet';

export type ColourwayFault = { id: string; why: string };
/** What a building should carry: its area's ensemble, or the high street's paint (from the generator's buildings: id, area; `painted` = oldStreetFamilies). */
export type ColourwayWant = { ensemble: Map<string, string>; painted: Map<string, string> };

/** The faults and the spread of a world's colourways (`want` absent: the scheme part is not checked, only one colourway per building and its pieces). */
export function colourwayFaults(w: World, want?: ColourwayWant): { faults: ColourwayFault[]; checked: number; spread: ReturnType<typeof spreadOf> } {
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
  if (want) for (const [h, c] of of) {
    const p = want.painted.get(h), e = want.ensemble.get(h);
    if (p !== undefined ? c !== p : e !== undefined && c !== e) faults.push({ id: h, why: p !== undefined ? `in ${c}, the high street paints it ${p}` : `in ${c}, its area's ensemble is ${e}` });
  }
  const P = OLD_STREET.colour.palette, pairs = streetNeighbours(w.frontages, new Set(of.keys()));
  const plotOf = (h: string) => h.split('/')[1]; // a run is one plot's wall: across a side street the old walk starts again, painted first (HighStreet)
  for (const [p, q] of pairs) if (plotOf(p) === plotOf(q) && P.includes(of.get(p)!) && P.includes(of.get(q)!)) faults.push({ id: `${p} | ${q}`, why: `painted street neighbours (${of.get(p)}, ${of.get(q)})` });
  return { faults, checked: w.buildings.length + w.facades.length + pairs.length, spread: spreadOf(of, pairs) };
}

/** What the scheme gives each generated building (CW-1's reference): the area ensemble and the high street's paint. */
export function colourwayWant(c: WCtx): ColourwayWant | undefined {
  const blds = (c.gen as any)?.blds as { id: string; area?: string; plot: string; tower?: boolean; ring: { p: [number, number][]; c: string[] } }[] | undefined;
  if (!blds?.length) return undefined;
  const at = piecesAt(c.w.facades);
  return { ensemble: new Map(blds.map(b => [b.id, ensembleOf(b.id, b.area, at.get(b.id), b.ring.p)])), painted: oldStreetFamilies(genCtx(c.plan, c.d, c.civ), blds) };
}

const SOURCE = 'brief 074 (Sky 2026-10-28: "Do building colors"; one facade.json family per building, on the building and every facade module) · brief 075 old palette (Sky 2026-10-29: "our red is too red ... back to old town"; '
  + 'the old DecoKitColourways scheme plan+livery+soft: ensembles by area, the high street\'s Soft paint on alternate buildings) · tools/world/gen/colourways.ts, oldstreet.ts · unity/Assets/Suborned/Editor/World/facade.json families, areas, hostAreas · '
  + 'failing examples (colourways.test.ts): a building given another area\'s ensemble; two painted street neighbours; a SideBay module left in `base` on a coloured building; a tower podium in another family from its body';

/** CW-1: every building one colourway (its area's ensemble or the high street's paint), its facade pieces in it, no painted neighbours (the world rule). */
export function colourwayRules(c: WCtx): Check[] {
  const r = colourwayFaults(c.w, colourwayWant(c)), s = r.spread;
  return [rule('CW-1', 'Colour', `Every building has one colourway (a facade.json family: its area's old ensemble, or the high street's Soft paint) on all its masses and facade pieces, and no two painted buildings are street neighbours; `
    + `${s.buildings} buildings, ${s.pairs} neighbour pairs; families ${Object.entries(s.families).map(([k, v]) => `${k} ${v}`).join(', ')}`,
    r.checked, r.faults.map(f => `${f.id}: ${f.why}`), '0 faults', SOURCE)];
}
