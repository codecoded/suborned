import plan from '../../contracts/world/harbour-district.plan.json';
import { streets, frontage, plots, plotArea } from './geom';
import { BANDS, DRAW, CAT, CATEGORY, K, RES_TRIPS, PROM, inOpen, bandOf, type Band } from './rules';
const T: Record<string, any> = (plan as any).standards.frontage.trades;
/** per metre of frontage at hour h (day clock): street presence by term */
export function mixTerms(mix: Record<string, number>, h: number) {
  let spill = 0, walk = 0, inside = 0, draw = 0, tot = 0;
  for (const [t, wt] of Object.entries(mix)) {
    const c = CATEGORY[t], tr = T[t]; const cp = CAT[c];
    const o = inOpen(tr.hours, h) ? 1 : 0, d = DRAW[c][bandOf(h)] * o, f = wt / 100;
    spill += f * K.spillDepth * cp.spillRho * d;
    const C = tr.depth * cp.floorRho * d;               // customers inside per m frontage
    inside += f * C;
    walk += f * K.walkersMultiplier * (C / cp.dwell) * (K.lastLeg / K.walkSpeed); // Little's law, last leg
    if (t !== 'lobby' && t !== 'hotel-lobby') draw += f * d; tot += f;
  }
  return { spill, walk, inside, draw: draw / tot };
}
const sh = (h: number) => bandOf(h);
export function homesPop(p: any) {
  const m = (plan as any).standards.buildings.bands[p.use]; if (!m || !['residential', 'mixed'].includes(p.use)) return 0;
  const a = plotArea(p);
  const foot = p.use === 'mixed' ? m.mode - m.podium : m.mode; // mixed: podium storeys are frontage, body above is housing
  return Math.round(a * 0.7 * foot * K.efficiency / K.homeM2 * K.perHome); // 0.7 = built share of the plot (courts, setbacks) [S]
}
export function streetDensity(h: number) {
  const out: any[] = [];
  for (const s of streets()) {
    const f = frontage(s, K.pavement);
    const per = s.len; let spill = 0, walk = 0, res = 0, n = 0;
    const fr = s.cls === 'primary' ? [] : f; // Buchanan: no frontage access on primary distributors
    let cover = 0;
    for (const x of fr) {
      if (!x.hit || ['park', 'open', 'square'].includes(x.hit.use) || !x.hit.mix) continue;
      cover++;
      const t = mixTerms(x.hit.mix, h); spill += t.spill; walk += t.walk;
      const p = plots.find(p => p.area === x.hit!.area && p.use === x.hit!.use);
      const pop = x.hit.use === 'residential' ? 1 : 0; // residents: area-level proxy handled below
      res += pop * 0;
    }
    const band = bandOf(h);
    const step = 4;
    const spillTot = spill * step / per, walkTot = walk * step / per; // people per m of street, both sides
    const placeType = s.kind === 'road' ? 'road' : s.kind;
    const foot = s.kind === 'road' ? 2 * K.pavement + s.median : s.w;
    const prom = PROM[placeType][band] * foot / 1; // people per m of street
    out.push({ id: s.id, cls: s.cls, len: s.len, foot, corridor: s.kind === 'road' ? s.w + 2 * K.pavement : s.w, spill: spillTot, walk: walkTot, prom, cover: cover / Math.max(1, f.length) });
  }
  // feeders: access-street trips ending on a main street ride it toward a stop (plan transit.reach 250, mean 125 m)
  const R: any = plan; const byId: Record<string, any> = {}; for (const o of out) byId[o.id] = o; for (const o of out) o.through = 0;
  const nodesOf = (id: string) => (R.roads.find((r: any) => r.id === id)?.path ?? []) as string[];
  for (const a of out.filter(o => o.cls === 'access')) {
    const flow = a.walk * a.len * K.walkSpeed / K.lastLeg;
    const mains = out.filter(m => (m.cls === 'district' || m.cls === 'mall') && nodesOf(m.id).some(n => nodesOf(a.id).includes(n)));
    for (const m of mains) m.through += K.feederShare * flow / mains.length * (K.reach / 2 / K.walkSpeed) / m.len;
  }
  return out;
}
