// Streetscape rules SF-10 … SF-17 (standards.streetscape.rules): staffed items, tree keep-clear, transit kits, station banks,
// the quay edge, roofs, spill and colours. SF-1 … SF-9 are in street-checks.ts.
import { ctxOf as genCtx } from '../world/gen/common';
import { kindOf, plotRing } from '../world/gen/edges';
import { landingsOf } from '../world/gen/street/crossings';
import type { Check } from './checks';
import { type V, lineToPoly } from './geom';
import { type Spot, indexes, spotsOf, walkBlock } from './street-geo';
import { FLAT, quayBlock } from './street-zones';
import { speciesOf } from './trees';
import { type WCtx, TOL, bb, f1, ringGap, rule } from './world-ctx';

const G = 'Streetscape';
const dist = (a: V, b: V) => Math.hypot(a[0] - b[0], a[1] - b[1]);
const inRing = (p: V, r: V[]) => { let c = false; for (let i = 0, j = r.length - 1; i < r.length; j = i++) if ((r[i]![1] > p[1]) !== (r[j]![1] > p[1]) && p[0] < ((r[j]![0] - r[i]![0]) * (p[1] - r[i]![1])) / (r[j]![1] - r[i]![1]) + r[i]![0]) c = !c; return c; };
const tag = (s: Spot) => `${s.s.kind} ${s.s.id.replace(/^street\//, '')}`;
/** The §15 city palette a sign may use (frontage.placement.signs); coral is the transit kiosks'. The palette is closed: a name outside it fails. Signal colours are not reserved (Sky, 2026-10-16), so none is barred by name; one joins the sign palette when the plan lists it. */
const PALETTE = ['cream_stone', 'paper_cream', 'teal_enamel', 'mint_enamel', 'gold_trim', 'chrome', 'sky_cerulean', 'zenith_cobalt', 'warm_sand', 'stone_shadow', 'ink_black'];

export function streetRules(c: WCtx): Check[] {
  const out: Check[] = [], { plan, d, w, S, F } = c, spots = spotsOf(c), gnd = spots.filter(x => x.ground), gc = genCtx(plan, d, c.civ, 0.18), ix = indexes(gc);
  const T = F.trades as Record<string, { hours: unknown; sign: [string, string, string] }>;

  // SF-10 a staffed item has a trade (in the frontage catalogue) or an occupation, and hours: shifts and people come from them (§09)
  const bad10: string[] = [], staffed = spots.filter(x => x.s.staffed), want = Object.entries(S.catalogue as Record<string, { staffed?: unknown }>).filter(([, v]) => v.staffed && typeof v.staffed === 'object').map(([k]) => k);
  const okHours = (h: unknown) => h === 'service' || (Array.isArray(h) && h.length === 2 && h.every(n => typeof n === 'number' && n >= 0 && n <= 27) && h[0] !== h[1]) // a window may run past midnight: [17, 3] or [16, 26];
  for (const sp of staffed) {
    const st = sp.s.staffed!;
    if (!(st.trade ? st.trade in T : !!String(st.occupation ?? '').trim())) bad10.push(`${tag(sp)}: ${st.trade ? `trade "${st.trade}" is not in standards.frontage.trades` : 'no trade or occupation'}`);
    if (!okHours(st.hours)) bad10.push(`${tag(sp)}: hours ${JSON.stringify(st.hours)}`);
  }
  for (const sp of spots) if (want.includes(sp.s.kind) && !sp.s.staffed) bad10.push(`${tag(sp)}: catalogue says staffed, item is not`);
  for (const f of w.frontages) if (f.staffed && !okHours(f.hours)) bad10.push(`${f.id}: hours ${JSON.stringify(f.hours)}`);
  out.push(rule('SF-10', G, 'A staffed item names a trade from the frontage catalogue (or an occupation) and has hours', staffed.length + w.frontages.length, bad10, 'all', 'streetscape.catalogue[].staffed · GDD §09'));

  // SF-11 trees keep clear: junction corner 6, lamp 3, hydrant 2, pylon face 3, megastructure support 6, and never under a deck or bridge
  const bad11: string[] = [], trees = gnd.filter(x => x.s.kind === 'tree'), K = S.trees.keepClear, ROADS = ['primary', 'district', 'access'];
  const corners: V[] = [], seen = new Set<string>();
  for (const pl of d.plots) {
    if (pl.civic) continue; const r = plotRing(gc, pl), n = r.p.length;
    for (let i = 0; i < n; i++) if (ROADS.includes(kindOf(r.c[(i + n - 1) % n]!)) && ROADS.includes(kindOf(r.c[i]!))) { const q = r.p[i]!, k = `${Math.round(q[0] * 2)},${Math.round(q[1] * 2)}`; if (!seen.has(k)) { seen.add(k); corners.push(q); } }
  }
  const lamps = gnd.filter(x => x.s.kind === 'lamp'), hyd = gnd.filter(x => x.s.kind === 'hydrant'), pylons = d.pylons.map(p => p.p as V), half = c.plan.standards.skyrail && 0.8;
  const supports: V[] = [...w.levels.flatMap(l => l.decks.flatMap(x => x.columns)), ...w.megastructures.flatMap(m => [...m.prims.filter(p => /colonnade-\d+-col-/.test(p.id)).map(p => p.c as V), ...m.parts.filter(p => /\/leg-\d+$/.test(p.id)).map(p => { const b = bb(p.ring as V[]); return [(b[0] + b[1]) / 2, (b[2] + b[3]) / 2] as V; })])];
  const decks = w.levels.filter(l => l.height > 1).flatMap(l => l.decks.map(x => ({ id: x.id, ring: x.ring as V[], under: l.height - x.thickness })));
  for (const t of trees) {
    const sp = speciesOf(S, t.s.variant) as { top: number }, p = t.s.at;
    const q = corners.find(k => dist(k, p) < K.junctionCorner - TOL); if (q) bad11.push(`${t.s.id}: ${f1(dist(q, p))} m from a junction corner (< ${K.junctionCorner})`);
    const l = lamps.find(x => dist(x.s.at, p) < K.lamp - TOL); if (l) bad11.push(`${t.s.id}: ${f1(dist(l.s.at, p))} m from ${l.s.id}`);
    const h = hyd.find(x => dist(x.s.at, p) < K.hydrant - TOL); if (h) bad11.push(`${t.s.id}: ${f1(dist(h.s.at, p))} m from a hydrant`);
    const py = pylons.find(x => dist(x, p) - (half as number) < K.pylonFace - TOL); if (py) bad11.push(`${t.s.id}: ${f1(dist(py, p) - 0.8)} m from a pylon face`);
    const sup = supports.find(x => dist(x, p) < K.megaSupport - TOL); if (sup) bad11.push(`${t.s.id}: ${f1(dist(sup, p))} m from a megastructure support`);
    const dk = decks.find(x => inRing(p, x.ring) && t.s.z + sp.top + 1.5 > x.under); if (dk) bad11.push(`${t.s.id}: under ${dk.id}`);
  }
  out.push(rule('SF-11', G, 'Trees keep clear of junction corners 6, lamps 3, hydrants 2, pylon faces 3, megastructure supports 6 and any deck or bridge above', trees.length, bad11, 'all', 'streetscape.trees.keepClear'));

  // SF-12 every tram stop platform and every station side has its whole transit kit
  const twin = S.catalogue['phone-booth']?.twin as { variant: string; counts: number } | undefined, worth = (x: Spot) => twin && x.s.kind === 'phone-booth' && x.s.variant === twin.variant ? twin.counts : 1; // brief 026: a twin booth serves a pair
  const bad12: string[] = [], by = (kind: string, test: (e: string) => boolean) => kind === 'crossing' ? landingsOf(w).filter(l => test(l.edge)).length : spots.filter(x => x.s.kind === kind && test(x.s.edge)).reduce((a, x) => a + worth(x), 0); // a crossing is its landing, a street item only in a plan without streetscape.crossings.strip
  const need = (label: string, kind: string, test: (e: string) => boolean, n: number) => { const got = by(kind, test); if (got < n) bad12.push(`${label}: ${got} ${kind} (needs ${n})`); };
  const stops = w.transit.stops;
  for (const m of stops) for (const [kind, n] of [['tram-shelter', 1], ['bench', 2], ['ticket-machine', 1], ['timetable-column', 1], ['bin', 1], ['bollard', 2], ['pole-camera', 1]] as const) need(m.id, kind, e => e === m.id, n);
  let units = stops.length;
  for (const l of plan.tram) for (const s of l.stops ?? []) {
    units++;
    const onPrimary = [...ix.roads.nearest(s.at, 30)].some(([id, dl]) => d.roads.find(r => r.id === id)?.class === 'primary' && dl < d.roads.find(r => r.id === id)!.w);
    const mine = (e: string) => e.startsWith(`stop/${l.id}/${s.id}/`);
    need(`${l.id}/${s.id} stop`, 'crossing', mine, 2);
    if (!onPrimary) { // the pavement kit; a primary's narrow kerb zone may skip it (SF-6)
      need(`${l.id}/${s.id} stop`, 'kiosk-tickets', mine, 1); need(`${l.id}/${s.id} stop`, 'newsstand', mine, 1); need(`${l.id}/${s.id} stop`, 'phone-booth', mine, 2);
      if (String(s.interchange ?? '').startsWith('S1')) need(`${l.id}/${s.id} interchange`, 'public-clock', mine, 1);
    }
  }
  for (const l of plan.skyrail) for (const s of l.stations ?? []) {
    units++;
    for (const side of ['l', 'r']) {
      const bank = `bank/${l.id}/${s.id}/${side}`, label = `${l.id}/${s.id}/${side} bank`, mine = (e: string) => e === bank;
      for (const [kind, n] of [['station-entrance', 1], ['newsstand', 1], ['phone-booth', 2], ['crossing', 1]] as const) need(label, kind, mine, n);
      const ids = w.connectors.filter(k => k.id.startsWith(`${bank}/`)).map(k => k.id.slice(bank.length + 1));
      for (const part of ['tower', 'run-a', 'run-b', 'lift']) if (!ids.includes(part)) bad12.push(`${label}: no ${part}`);
    }
    const st = (e: string) => e.startsWith(`station/${l.id}/${s.id}/`);
    need(`${l.id}/${s.id} station`, 'kiosk-tickets', st, 1); need(`${l.id}/${s.id} station`, 'public-clock', st, 1); need(`${l.id}/${s.id} station`, 'vending-robot', st, 2);
    need(`${l.id}/${s.id} station`, 'civic-robot-dock', e => e.startsWith(`bank/${l.id}/${s.id}/`), 1);
  }
  out.push(rule('SF-12', G, 'Every tram stop platform and station side has its whole transit kit (shelter, benches, ticket machine, timetable column, bin, bollards, crossings, kiosks, clocks, banks)', units, bad12, 'all', 'streetscape.transit.tramStop.kit · stationEntrance.kit'));

  // SF-13 a station bank (stair tower, escalator run A, lift kiosk) stands on public ground, outside the beam envelope + 1 m, and leaves every walk zone clear
  const bad13: string[] = [], banks = w.connectors.filter(k => /^bank\/.+\/(tower|run-a|lift)$/.test(k.id)), env = S.skyrailEnvelope.half as number, built = new Set(['residential', 'mixed', 'commercial', 'industrial', 'port', 'civic']);
  const HL = (S.transit.stationEntrance.hallLane as { half?: number } | undefined)?.half, inGround = (p: V) => (plan.signatures ?? []).some(sg => p[0] >= sg.ground[0] && p[0] <= sg.ground[1] && p[1] >= sg.ground[2] && p[1] <= sg.ground[3]);
  const lineGap = (ring: V[]) => Math.min(...d.sky.map(sp => lineToPoly(sp.samples.map(q => q.p as V), ring)));
  for (const k of banks) {
    const ring = k.ring as V[], pts = [...ring, [(bb(ring)[0] + bb(ring)[1]) / 2, (bb(ring)[2] + bb(ring)[3]) / 2] as V], pl = d.plots.find(p => built.has(p.use) && inRing(pts[4]!, p.polyV));
    if (pl) bad13.push(`${k.id} stands on plot ${pl.id}`);
    if (pts.some(p => ix.sky.dist(p, env + 2) < env + 1 - TOL) || ringGap(ring, [[0, 0]]) < 0) bad13.push(`${k.id} inside the beam envelope + 1 m`);
    const pc = S.transit.stationEntrance.platformClear as number | undefined; // brief 007: tower and lift stand outside the platform's outer edge
    if (pc && /\/(tower|lift)$/.test(k.id) && d.sky.some(sp => lineToPoly(sp.samples.map(q => q.p as V), ring) < pc - TOL)) bad13.push(`${k.id} under the platform (< ${pc} m from the line)`);
    const inHall = inGround(pts[4]!); // a bank inside a signature's ground hall (SpaceHQ): the mall's walk bands do not apply, the hall lane does (stationEntrance.hallLane)
    const why = inHall ? (HL && lineGap(ring) < HL - TOL ? `the hall lane (${f1(lineGap(ring))} m from the line, lane ±${HL} m)` : '') : walkBlock(gc, [...pts, ...ring.map((p, i) => [(p[0] + ring[(i + 1) % 4]![0]) / 2, (p[1] + ring[(i + 1) % 4]![1]) / 2] as V)], '');
    if (why) bad13.push(`${k.id} stands in ${why}`);
  }
  // hall lane: any street-level item standing in a signature's ground keeps the lane along the line clear too
  const hallItems = HL ? gnd.filter(x => inGround(x.s.at)) : [];
  for (const sp of hallItems) { const g = lineGap(sp.pts); if (g < HL! - TOL) bad13.push(`${tag(sp)} in the hall lane (${f1(g)} m from the line, lane ±${HL} m)`); }
  out.push(rule('SF-13', G, 'Station banks (stair tower, escalator run A, lift) stand on public ground, outside the beam envelope + 1 m (tower and lift outside the platform, ≥ platformClear), and leave every walk zone clear', banks.length + hallItems.length, bad13, 'all', 'streetscape.transit.stationEntrance.siting · hallLane · levels.station'));

  // SF-14 nothing but lamps and mooring cleats within 1.5 m of a quay edge (the edge that faces water)
  const bad14: string[] = [];
  const q14 = gnd.filter(x => !['lamp', 'mooring-cleat'].includes(x.s.kind) && (x.s.kind === 'crossing' || !FLAT.has(x.s.kind)) && plan.quay.some(q => inRing(x.s.at, q))); // a crossing's studs, arcs, lights and orbs are paving and light, not furniture; the landing slab of a plan without crossings.strip is still judged, as it always was
  for (const sp of q14) if (quayBlock(gc, sp.pts)) bad14.push(`${tag(sp)} within 1.5 m of the quay edge`);
  out.push(rule('SF-14', G, 'Nothing but lamps and mooring cleats within 1.5 m of a quay edge', q14.length, bad14, '≥ 1.5 m', 'streetscape.sections.quay.edge'));

  // SF-15 every roof programme fits its roof (minRoof), and every roof item keeps the programme's setback from the parapet
  const bad15: string[] = [], kit = c.B.roofs.kit.programmes as Record<string, { minRoof: [number, number]; setback?: number }>, over: Record<string, number> = { 'observation-saucer': 3, 'crane-gantry': 1 };
  for (const r of w.roofs) {
    if (r.form === 'terrace') continue;
    const k = kit[r.programme], [mw, md] = k?.minRoof ?? [0, 0], [W, D] = r.size;
    if (!k) { bad15.push(`${r.id}: unknown programme ${r.programme}`); continue; }
    if (!((W >= mw - TOL && D >= md - TOL) || (W >= md - TOL && D >= mw - TOL))) bad15.push(`${r.id} ${r.programme} on ${f1(W)} × ${f1(D)} (minRoof ${mw} × ${md})`);
    const ext = over[r.programme] ?? 0, cs = Math.cos(r.yaw), sn = Math.sin(r.yaw);
    for (const p of r.parts) {
      if (/\/(plant|rocket-vent|parapet)/.test(p.id)) continue;
      const rel: V = [(p.c[0] - r.c[0]) * cs + (p.c[1] - r.c[1]) * sn, -(p.c[0] - r.c[0]) * sn + (p.c[1] - r.c[1]) * cs], phi = p.yaw - r.yaw;
      const hx = (Math.abs(p.size[0] * Math.cos(phi)) + Math.abs(p.size[1] * Math.sin(phi))) / 2, hy = (Math.abs(p.size[0] * Math.sin(phi)) + Math.abs(p.size[1] * Math.cos(phi))) / 2; // the part's footprint (every shape's a × b) in the roof's frame
      if (Math.abs(rel[0]) + hx > W / 2 + ext + TOL || Math.abs(rel[1]) + hy > D / 2 + ext + TOL) { bad15.push(`${p.id} reaches past the ${r.programme} roof's setback`); break; }
    }
  }
  out.push(rule('SF-15', G, 'Every roof gets a programme whose minRoof fits it; roof items keep their setback from the parapet', w.roofs.length, bad15, 'all', 'standards.buildings.roofs.kit · streetscape.rules SF-15'));

  // SF-16 spill depth never exceeds the street section's frontage zone (and the placement's spill depth)
  const bad16: string[] = [], roadBy = new Map(d.roads.map(r => [r.id, r]));
  for (const f of w.frontages) {
    if (f.level !== 'street') continue;
    const r = roadBy.get(f.street), foot = plan.foot.find(x => x.id === f.street), sec = r ? S.sections[r.class] : foot?.kind === 'mall' ? S.sections.mall : null;
    const zone = r ? ((r.class === 'access' && S.trees.avenues.access.avenue.includes(f.area) ? sec.avenue?.frontage : null) ?? sec.frontage) : foot?.kind === 'mall' ? sec?.frontage : S.sections.walk.edgeBand; // a walk's or the quay's spill stays in its edge band
    if (f.spill.depth > Math.min(F.placement.spill.depth, zone ?? 0) + TOL) bad16.push(`${f.id}: spill ${f.spill.depth} m > frontage zone ${zone ?? 0} m`);
  }
  out.push(rule('SF-16', G, 'Frontage spill depth ≤ the street section\'s frontage zone', w.frontages.filter(f => f.level === 'street').length, bad16, 'all', 'streetscape.sections · frontage.placement.spill'));

  // SF-17 colours: a sign uses the city palette; coral_transit only on transit kiosks and their kit (signal colours are free since 2026-10-16: no colour is barred for being one)
  const bad17: string[] = [];
  for (const f of w.frontages) {
    const p = f.sign.palette;
    if (p === 'coral_transit' && !/-kiosk$/.test(f.trade)) bad17.push(`${f.id}: coral_transit on ${f.trade}`);
    else if (p !== 'coral_transit' && !PALETTE.includes(p)) bad17.push(`${f.id}: sign colour ${p} is not in the city palette`);
  }
  const coralOk = new Set(['kiosk-tickets', 'station-entrance', 'timetable-column']), surfaces = new Set(['enamel', 'trim', 'park', 'crossing', 'booth', 'tub', 'glow', 'inlay', 'orb']); // crossing, booth, tub, glow, inlay, orb: the landing slab's (a plan without crossings.strip), the phone booth body's, the palm tub base's and a crossing's light, arc and orb's own look keys (so the trim colour does not decide them)
  for (const sp of spots) { if (!surfaces.has(sp.s.surface)) bad17.push(`${tag(sp)}: surface ${sp.s.surface}`); if (sp.s.variant === 'coral-dial' && !coralOk.has(sp.s.kind)) bad17.push(`${tag(sp)}: coral off a transit item`); }
  out.push(rule('SF-17', G, 'A sign uses the city palette and every street item a known surface; coral_transit only on transit items', w.frontages.length + spots.length, bad17, 'none', 'frontage.placement.signs · §15 city palette'));
  return out;
}
