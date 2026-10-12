// Rules on the generated buildings (B-1…B-6) and frontages (F-1…F-6): the conditions are in standards.buildings and
// standards.frontage (Datum's A1 hand-off); each rule reads the generator's output in process. Every rule carries its source.
import { ctxOf as genCtx } from '../world/gen/common';
import { buildingHost } from '../world/gen/frontage';
import type { Host } from '../world/gen/types';
import type { Check } from './checks';
import { type V, lineToPoly, nearLine, lineDist } from './geom';
import { type R4, type WCtx, TOL, bb, f1, rule } from './world-ctx';

const BUILT = ['residential', 'mixed', 'commercial', 'industrial', 'port'];
const lineOf = (c: WCtx) => c.d.sky.map(p => p.samples.map(s => s.p as V));
const hOf = (B: any, n: number) => B.plinth + B.groundStorey + (n - 1) * B.upperStorey + B.roofCrown;
/** Points of a ring on a 1.5 m grid strictly inside it (ray casting), for overlap tests on small polygons. */
function inside(p: V, r: V[]) { let c = false; for (let i = 0, j = r.length - 1; i < r.length; j = i++) if ((r[i]![1] > p[1]) !== (r[j]![1] > p[1]) && p[0] < ((r[j]![0] - r[i]![0]) * (p[1] - r[i]![1])) / (r[j]![1] - r[i]![1]) + r[i]![0]) c = !c; return c; }
function overlapArea(a: V[], b: V[], step = 1.5): number {
  const A = bb(a), Bb = bb(b), x0 = Math.max(A[0], Bb[0]), x1 = Math.min(A[1], Bb[1]), y0 = Math.max(A[2], Bb[2]), y1 = Math.min(A[3], Bb[3]);
  if (x0 >= x1 || y0 >= y1) return 0;
  let n = 0;
  for (let x = x0 + step / 2; x < x1; x += step) for (let y = y0 + step / 2; y < y1; y += step) if (inside([x, y], a) && inside([x, y], b)) n++;
  return n * step * step;
}

export function buildingChecks(c: WCtx): Check[] {
  const { plan, d, gen, w, B, S } = c, out: Check[] = [], pav = 0.18, G = 'Buildings';
  const bands = B.bands, blds = gen.blds, byPlot = new Map<string, number>();
  for (const b of blds) byPlot.set(b.plot, (byPlot.get(b.plot) ?? 0) + 1);

  // B-1 every built plot resolves to a height: its use has a band, and it carries a building, a megastructure or a civic block
  const megaPlots = new Set((plan.megastructures ?? []).flatMap(m => m.spans.plots));
  const bad1: string[] = [];
  for (const p of plan.plots) {
    const band = bands[p.use];
    if (!band) { bad1.push(`${p.id}: no band for use ${p.use}`); continue; }
    if (p.civic ? !c.civ.buildings.length : BUILT.includes(p.use) && !byPlot.has(p.id) && !megaPlots.has(p.id)) bad1.push(`${p.id} (${p.use}) has no building`);
  }
  out.push(rule('B-1', G, 'Every plot resolves to a height (a band for its use, and a building, megastructure or civic block on every built plot)', plan.plots.length, bad1, 'all', 'standards.buildings.bands · brief 004 B'));

  // B-2 heights: H(n) on the storey arithmetic, n inside the use's band (+1 on a park edge), sheds inside heightM, area caps hold
  const bad2: string[] = [];
  for (const b of blds) {
    const band = bands[b.use], H = b.z - pav;
    if (b.tower) { const s = B.towers.sites.find((x: any) => `tower/${x.id}` === b.id); if (!s || b.n < B.towers.storeysMin || Math.abs(H - hOf(B, b.n)) > TOL) bad2.push(`${b.id} H ${f1(H)} n ${b.n}`); continue; }
    if (b.kind === 'shed') { const [lo, hi] = band.heightM as V; if (H < lo - TOL || H > hi + TOL) bad2.push(`${b.id} shed ${f1(H)} outside ${lo}–${hi}`); continue; }
    const [lo, hi] = band.storeys as V, cap = B.caps?.[b.area]?.m as number | undefined;
    if (b.n < lo || b.n > hi + 1 || Math.abs(H - hOf(B, b.n)) > TOL) bad2.push(`${b.id} n ${b.n} H ${f1(H)} (band ${lo}–${hi})`);
    if (cap && H > cap + TOL) bad2.push(`${b.id} H ${f1(H)} over the ${b.area} cap ${cap}`);
  }
  out.push(rule('B-2', G, 'Heights follow H(n) = plinth + ground + (n−1) × upper + crown, n inside the use band; sheds inside heightM; area caps hold', blds.length, bad2, 'all', 'standards.buildings.height · bands · caps'));

  // B-3 building line: every vertex of a building mass stands in its plot and at least the street section's pavement from the kerb face
  const bad3: string[] = [], roads = d.roads.filter(r => !r.gyratory);
  const plotOf = new Map(d.plots.map(p => [p.id, p.polyV]));
  for (const b of blds) {
    const poly = plotOf.get(b.plot)!, parts = w.buildings.filter(m => m.id.startsWith(`${b.id}/`));
    for (const m of parts) for (const v of m.ring) {
      if (!inside(v, poly) && lineDist(v, [...poly, poly[0]!]) > TOL) { bad3.push(`${m.id} leaves its plot`); break; }
      const hit = roads.find(r => nearLine(v, r.line, r.w / 2 + S.sections[r.class].pavement - 0.05, true));
      if (hit) { bad3.push(`${m.id} inside ${hit.id}'s pavement`); break; }
    }
  }
  out.push(rule('B-3', G, 'Buildings stand in their plot, on or behind the building line (plot edge set back by the street section\'s pavement)', blds.length, bad3, 'all', 'standards.buildings.colonnade · streetscape.pavement (brief 004 decisions)'));

  // B-4 no two buildings overlap, and nothing of a plot stands under a megastructure
  const bad4: string[] = [], masses = w.buildings.filter(m => m.base < 1), ids = (m: { id: string }) => m.id.split('/').slice(0, m.id.startsWith('tower/') ? 2 : 3).join('/');
  for (let i = 0; i < masses.length; i++) for (let j = i + 1; j < masses.length; j++) {
    const a = masses[i]!, b = masses[j]!; if (ids(a) === ids(b)) continue;
    if (overlapArea(a.ring as V[], b.ring as V[]) > 1) bad4.push(`${ids(a)} × ${ids(b)}`);
  }
  const towersOf = new Map((plan.megastructures ?? []).map(m => [m.id, (m.towers ?? []) as string[]]));
  for (const m of plan.megastructures ?? []) for (const f of m.footprint as R4[]) for (const b of masses) {
    if (b.id.startsWith('tower/') && towersOf.get(m.id)!.includes(b.id.split('/')[1]!)) continue;
    const fr: V[] = [[f[0], f[2]], [f[1], f[2]], [f[1], f[3]], [f[0], f[3]]];
    if (overlapArea(b.ring as V[], fr) > 0.5) bad4.push(`${ids(b)} under ${m.id}`);
  }
  out.push(rule('B-4', G, 'No two buildings overlap; no plot building stands under a megastructure (its own gate towers excepted)', masses.length, [...new Set(bad4)], 'none', 'standards.buildings (massing) · megastructures[].footprint'));

  // B-5 towers only at towers.sites: one per site at its storeys, floorplate ≤ 30 × 30, ≥ setback.by back from the podium edge where the podium is not cut by the building line
  const bad5: string[] = [], sites = B.towers.sites as { id: string; rect: R4; storeys: number; plot: string }[];
  const towers = blds.filter(b => b.tower);
  for (const s of sites) {
    const t = towers.find(b => b.id === `tower/${s.id}`);
    if (!t) { bad5.push(`${s.id} missing`); continue; }
    if (t.n !== s.storeys) bad5.push(`${s.id} storeys ${t.n} ≠ ${s.storeys}`);
    const lower = w.buildings.find(m => m.id === `tower/${s.id}/lower`)!, pod = w.buildings.find(m => m.id === `tower/${s.id}/podium`)!, L = bb(lower.ring as V[]), P = bb(pod.ring as V[]);
    if (L[1] - L[0] > B.towers.floorplateMax[0] + TOL || L[3] - L[2] > B.towers.floorplateMax[1] + TOL) bad5.push(`${s.id} floorplate ${f1(L[1] - L[0])} × ${f1(L[3] - L[2])}`);
    const grown = [s.rect[0] - B.towers.podiumGrow, s.rect[1] + B.towers.podiumGrow, s.rect[2] - B.towers.podiumGrow, s.rect[3] + B.towers.podiumGrow];
    const gaps = [L[0] - P[0], P[1] - L[1], L[2] - P[2], P[3] - L[3]], cut = [P[0] > grown[0]! + TOL, P[1] < grown[1]! - TOL, P[2] > grown[2]! + TOL, P[3] < grown[3]! - TOL];
    gaps.forEach((g, i) => { if (g < B.setback.by - TOL && !cut[i]) bad5.push(`${s.id} side ${i} ${f1(g)} m back`); });
    const poly = plotOf.get(s.plot); if (!poly || !inside([(s.rect[0] + s.rect[1]) / 2, (s.rect[2] + s.rect[3]) / 2], poly)) bad5.push(`${s.id} not in plot ${s.plot}`);
  }
  for (const t of towers) if (!sites.some(s => `tower/${s.id}` === t.id)) bad5.push(`${t.id} not a tower site`);
  out.push(rule('B-5', G, 'Towers stand only at towers.sites, at their storeys, floorplate ≤ 30 × 30, ≥ 6 m back from the podium edge', sites.length, bad5, 'all', 'standards.buildings.towers · setback'));

  // B-6 beam clearance (Datum's mega-check): nothing of a building or megastructure reaches into the sky rail's beam envelope; a deck or bridge that crosses the line keeps headroom.underBeam under the beam
  const bad6: string[] = [], env = S.skyrailEnvelope, lines = lineOf(c), under = env.underside, low = under - c.L.headroom.underBeam, topZ = plan.standards.skyrail.height + c.L.headroom.walk;
  const portal = new Set((plan.megastructures ?? []).filter(m => (m.supports as any).portal).map(m => `mega/${m.id}`));
  const hits = (ring: V[]) => lines.some(l => lineToPoly(l, ring) <= env.half + 0.001);
  for (const m of [...w.buildings, ...w.megastructures.flatMap(x => portal.has(x.id) ? [] : x.parts)]) if (m.base + m.height > low + TOL && m.base < topZ && hits(m.ring as V[])) bad6.push(`${m.id} z ${f1(m.base)}–${f1(m.base + m.height)} in the beam envelope`);
  for (const m of plan.megastructures ?? []) for (const b of m.bridges ?? []) {
    const z = c.L.list.find((l: any) => l.id === b.level)?.z as number, r = b.rect, ring: V[] = [[r[0], r[2]], [r[1], r[2]], [r[1], r[3]], [r[0], r[3]]];
    if (b.level !== 'concourse' && hits(ring) && z + c.L.headroom.underBeam > under + TOL) bad6.push(`${m.id}/${b.id} ${b.level} z ${z} + ${c.L.headroom.underBeam} over the beam underside ${under}`);
  }
  out.push(rule('B-6', G, 'Beam clearance: no building or megastructure part in the sky rail envelope; a deck bridge crossing the line clears the beam underside by headroom.underBeam', w.buildings.length + w.megastructures.length, bad6, 'none', 'streetscape.skyrailEnvelope · levels.headroom.underBeam · out/datum/mega-check.ts'));
  return out;
}

// ---------------------------------------------------------------- frontage
type Trade = { w: [number, number]; hours: [number, number] | 'service'; wants: string[]; serviceBay: boolean; sign: [string, string, string]; spill: string; depth: number; anchor?: boolean };
const QUIET: Record<string, number> = { access: 0, park: 1, quay: 2, walk: 3, district: 4, circus: 4, mall: 5, deck: 1, concourse: 2 };

export function frontageChecks(c: WCtx): Check[] {
  const { plan, d, gen, w, F } = c, out: Check[] = [], G = 'Frontage', T = F.trades as Record<string, Trade>, P = F.placement;
  const gc = genCtx(plan, d, c.civ, 0.18), hosts = new Map<string, Host>();
  for (const b of gen.blds) if (!b.tower) { const h = buildingHost(gc, b); if (h) hosts.set(h.id, h); }
  for (const m of gen.megaOut) for (const h of m.hosts) hosts.set(h.id, h);
  const fr = w.frontages, edgeOf = (f: (typeof fr)[number]) => { const h = hosts.get(f.host), i = Number(/\/e(\d+)-\d+$/.exec(f.id)?.[1]); return h && Number.isFinite(i) ? { h, e: h.edges[i]! } : null; };
  const primary = new Set(plan.roads.filter(r => r.class === 'primary').map(r => r.id));
  const len = (a: V, b: V) => Math.hypot(b[0] - a[0], b[1] - a[1]);

  // F-1 fit: a tenancy lies on its host facade run, inside its ends, with a width in its trade's range on the 3 m snap
  const bad1: string[] = [];
  for (const f of fr) {
    const x = edgeOf(f), t = T[f.trade]; if (!x || !t) { bad1.push(`${f.id}: ${!x ? 'no host edge' : 'no trade ' + f.trade}`); continue; }
    const { a, b } = x.e, L = len(a, b);
    for (const q of [f.a, f.b]) { const s = ((q[0] - a[0]) * (b[0] - a[0]) + (q[1] - a[1]) * (b[1] - a[1])) / L; if (nearLine(q, [a, b], 0.02) === false || s < -0.02 || s > L + 0.02) { bad1.push(`${f.id} off its facade`); break; } }
    if (f.w < Math.min(t.w[0], L) - TOL || f.w > t.w[1] + TOL) bad1.push(`${f.id} ${f.trade} w ${f.w} outside ${t.w[0]}–${t.w[1]}`);
  }
  // widths snap to widthSnap; a run may absorb its remainder in one tenancy (the last bay shorter than a snap)
  const runsOf = new Map<string, typeof fr>();
  for (const f of fr) { const k = f.id.replace(/-\d+$/, ''); runsOf.set(k, [...(runsOf.get(k) ?? []), f]); }
  for (const [k, list] of runsOf) { const off = list.filter(f => Math.abs(f.w - Math.round(f.w / P.widthSnap) * P.widthSnap) > TOL); if (off.length > 1) bad1.push(`${k}: ${off.length} tenancies off the ${P.widthSnap} m snap (${off.map(f => f.w).join(', ')})`); }
  out.push(rule('F-1', G, 'Every tenancy lies on its building line, inside its facade run, at its trade\'s width on the 3 m snap', fr.length, bad1, 'all', 'standards.frontage.trades[].w · placement.widthSnap · brief 004 decisions (frontage fit on the building line)'));

  // F-2 placement: only streets the placement allows, never a primary, and a street kind its trade wants (deck and concourse by level)
  const bad2: string[] = [];
  for (const f of fr) {
    const x = edgeOf(f), t = T[f.trade]; if (!x || !t) continue;
    const kind = f.level === 'street' ? x.e.kind : f.level;
    if (primary.has(f.street)) bad2.push(`${f.id} on primary ${f.street}`);
    else if (!P.streets.includes(kind)) bad2.push(`${f.id} on a ${kind}`);
    else if (!t.wants.includes(kind) && !t.wants.includes(f.level) && !(kind === 'park' && t.wants.includes('park'))) bad2.push(`${f.id} ${f.trade} does not want ${kind}`);
  }
  out.push(rule('F-2', G, 'Tenancies stand only on allowed street kinds (never a primary distributor) and the kinds their trade wants', fr.length, bad2, 'all', 'standards.frontage.placement.streets · never · trades[].wants'));

  // F-3 the same trade never stands side by side on one facade run
  const bad3: string[] = [], runs = new Map<string, typeof fr>();
  for (const f of fr) { const k = f.id.replace(/-\d+$/, ''); runs.set(k, [...(runs.get(k) ?? []), f]); }
  for (const [k, list] of runs) {
    const s = [...list].sort((p, q) => Number(/-(\d+)$/.exec(p.id)![1]) - Number(/-(\d+)$/.exec(q.id)![1]));
    const at = (f: (typeof fr)[number]) => (f.a[0] - s[0]!.a[0]) * Math.sign(s[s.length - 1]!.b[0] - s[0]!.a[0] || 1) + (f.a[1] - s[0]!.a[1]) * Math.sign(s[s.length - 1]!.b[1] - s[0]!.a[1] || 1);
    const byPos = [...s].sort((p, q) => at(p) - at(q));
    for (let i = 1; i < byPos.length; i++) if (byPos[i]!.trade === byPos[i - 1]!.trade && len(byPos[i - 1]!.b, byPos[i]!.a) < 0.05) bad3.push(`${k}: ${byPos[i]!.trade} twice`);
  }
  out.push(rule('F-3', G, 'The same trade never stands side by side on one facade run', runs.size, bad3, 'none', 'standards.frontage.placement.repeat'));

  // F-4 service bays (roller 4.5 × 4.6) only on access roads, the quay or under a deck; never on a district road or the mall; and only for trades that list one
  const bad4: string[] = [];
  for (const f of fr) {
    if (!f.service) continue; const x = edgeOf(f), t = T[f.trade], under = f.level === 'street' && f.host.startsWith('mega/');
    if (!t?.serviceBay) bad4.push(`${f.id} ${f.trade} lists no service bay`);
    else if (!x || !(['access', 'quay'].includes(x.e.kind) || under)) bad4.push(`${f.id} bay on a ${x?.e.kind ?? '?'}`);
    else if (Math.abs(f.service.size[0] - 4.5) > TOL || Math.abs(f.service.size[2] - 4.6) > TOL) bad4.push(`${f.id} bay ${f.service.size}`);
  }
  out.push(rule('F-4', G, 'Service bays (4.5 × 4.6 roller) only on access roads, the quay or under a deck, and only for trades that list one', fr.filter(f => f.service).length, bad4, 'all', 'standards.frontage.placement.service'));

  // F-5 every building has a lobby on its quietest street; a corner on two streets takes an anchor trade where the mix has one
  const bad5: string[] = [], bk = [...hosts.values()].filter(h => h.id.startsWith('bld/') && h.level === 'street');
  for (const h of bk) {
    const mine = fr.filter(f => f.host === h.id), lobby = mine.filter(f => f.trade === 'lobby');
    if (!lobby.length) { if (h.edges.some(e => len(e.a, e.b) >= 6)) bad5.push(`${h.id} has no lobby`); continue; }
    const best = Math.min(...h.edges.map(e => QUIET[e.kind] ?? 9)), got = Math.min(...lobby.map(f => QUIET[edgeOf(f)?.e.kind ?? ''] ?? 9));
    if (got > best) bad5.push(`${h.id} lobby not on its quietest street`);
  }
  // a corner is where one run ends at the next run's start; one anchor on either run is enough
  for (const h of bk) h.edges.forEach((e, i) => {
    const nx = h.edges[(i + 1) % h.edges.length]!;
    if (nx === e || len(e.b, nx.a) > 0.05 || len(e.a, e.b) < 12 || len(nx.a, nx.b) < 12) return;
    const mix = (F.mix[h.area] ?? {}) as Record<string, number>, hasAnchor = Object.keys(mix).some(t => T[t]?.anchor && T[t]!.w[1] >= 18);
    const mine = fr.filter(f => f.id.startsWith(`${h.id}/e${i}-`) || f.id.startsWith(`${h.id}/e${(i + 1) % h.edges.length}-`));
    if (hasAnchor && mine.length && !mine.some(f => f.anchor)) bad5.push(`${h.id} corner e${i}/e${(i + 1) % h.edges.length} without an anchor`);
  });
  out.push(rule('F-5', G, 'Every building has a lobby on its quietest street; corners on two streets take an anchor trade', bk.length, bad5, 'all', 'standards.frontage.placement.lobby · corner'));

  // F-6 catalogue: the trade exists in the area's (or the megastructure level's) mix, hours and sign match its entry, spill within the spill depth, door canopy and sign blank present
  const bad6: string[] = [], mega = new Map((plan.megastructures ?? []).map(m => [`mega/${m.id}`, m]));
  for (const f of fr) {
    const t = T[f.trade]; if (!t) { bad6.push(`${f.id}: unknown trade ${f.trade}`); continue; }
    const m = mega.get(f.host.split('/').slice(0, 2).join('/')), lvMix = m?.levels.find(l => l.level === f.level)?.mix, mix = lvMix ?? (F.mix[f.area] as Record<string, number>) ?? {};
    if (!(f.trade in mix) && f.trade !== 'lobby' && !/kiosk/.test(f.trade) && !(f.anchor && ['diner', 'hotel-lobby'].includes(f.trade))) bad6.push(`${f.id} ${f.trade} not in the ${lvMix ? 'level' : f.area} mix`);
    if (JSON.stringify(f.hours) !== JSON.stringify(t.hours)) bad6.push(`${f.id} hours ${f.hours} ≠ ${t.hours}`);
    if (f.sign.motif !== t.sign[0] || f.sign.icon !== t.sign[1] || f.sign.palette !== t.sign[2]) bad6.push(`${f.id} sign ≠ ${t.sign}`);
    if (f.spill.depth > P.spill.depth + TOL) bad6.push(`${f.id} spill ${f.spill.depth} > ${P.spill.depth}`);
    if (!f.front || !f.entrance || !f.canopy || !f.sign) bad6.push(`${f.id} missing front, door, canopy or sign`);
  }
  out.push(rule('F-6', G, 'Tenancies come from the area\'s mix (or the megastructure level\'s) with the trade\'s hours, sign and spill; shopfront, door, canopy and sign blank present', fr.length, bad6, 'all', 'standards.frontage.mix · trades · placement.spill · signs'));
  return out;
}
