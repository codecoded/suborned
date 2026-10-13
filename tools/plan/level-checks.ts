// Rules on levels (L-1…L-7) and megastructures (MS-1…MS-5): conditions in standards.levels and the plan's megastructures[]
// (Datum's A1 hand-off); MS-3, MS-4, B-6 (world-checks) and L-5 are Datum's scratch check out/datum/mega-check.ts as rules.
import { deckGroups, isWayDown } from '../world/gen/connectors';
import type { Check } from './checks';
import { type V, inPoly, lineDist, lineToPoly } from './geom';
import { type R4, type WCtx, TOL, bb, f1, rectDist, ringGap, rule } from './world-ctx';

const zOf = (c: WCtx) => Object.fromEntries((c.L.list as { id: string; z: number }[]).map(l => [l.id, l.z])) as Record<string, number>;
const skyLines = (c: WCtx) => c.d.sky.map(p => p.samples.map(s => s.p as V));
const lineD = (c: WCtx, r: R4) => Math.min(...c.d.sky.flatMap(p => p.samples.map(s => rectDist(s.p as V, r))));

// ------------------------------------------------------------------ megastructures
export function megaChecks(c: WCtx): Check[] {
  const { plan, d, w, S } = c, out: Check[] = [], G = 'Megastructures', ms = plan.megastructures ?? [], z = zOf(c);
  const stations = (plan.skyrail as any[]).flatMap(l => (l.stations ?? []).map((s: any) => ({ ...s, line: l.id })));

  // MS-1 the program: 2–4 named, sited structures, each with footprint, levels, height, what it spans and why
  const bad1: string[] = [];
  ms.forEach(m => {
    for (const k of ['id', 'name', 'kind', 'why', 'source'] as const) if (!String(m[k] ?? '').trim()) bad1.push(`${m.id}: no ${k}`);
    if (!m.footprint?.length || !(m.height > 0) || !m.levels?.length) bad1.push(`${m.id}: footprint, height or levels missing`);
    for (const l of m.levels ?? []) if (!(l.level in z) && !(typeof l.z === 'number')) bad1.push(`${m.id}: level ${l.level} is not in standards.levels`);
    for (const p of m.spans.plots) if (!plan.plots.some(q => q.id === p)) bad1.push(`${m.id}: spans unknown plot ${p}`);
    for (const r of m.spans.roads ?? []) if (!plan.roads.some(q => q.id === r)) bad1.push(`${m.id}: spans unknown road ${r}`);
    for (const f of m.spans.foot ?? []) if (!plan.foot.some(q => q.id === f)) bad1.push(`${m.id}: spans unknown foot path ${f}`);
    if (!w.megastructures.some(g => g.id === `mega/${m.id}` && g.parts.length)) bad1.push(`${m.id}: nothing generated`);
  });
  if (ms.length < 2 || ms.length > 4) bad1.push(`${ms.length} megastructures (2–4)`);
  out.push(rule('MS-1', G, '2–4 named megastructures, each with footprint, levels, height, what it spans, a reason and a source; every one generated', ms.length, bad1, '2–4, complete', 'brief 004 A1.4 · standards.levels'));

  // MS-2 siting: every footprint cell lies on land its spans name (its plots, the spanned roads and foot paths with their pavements, the spanned zones) and footprints never overlap each other
  const bad2: string[] = [], kinds = (m: (typeof ms)[number]) => new Set(m.spans.zones ?? []);
  const roadsBy = new Map(d.roads.map(r => [r.id, r])), footBy = new Map(plan.foot.map(f => [f.id, f]));
  for (const m of ms) for (const f of m.footprint as R4[]) {
    let miss = 0, tot = 0;
    for (let x = f[0] + 2; x < f[1]; x += 4) for (let y = f[2] + 2; y < f[3]; y += 4) {
      tot++; const p: V = [x, y];
      const ok = m.spans.plots.some(id => inPoly(p, d.plots.find(q => q.id === id)!.polyV))
        || (m.spans.roads ?? []).some(id => { const r = roadsBy.get(id)!; return lineDist(p, r.line) <= r.w / 2 + S.sections[r.class].pavement; })
        || (m.spans.foot ?? []).some(id => { const q = footBy.get(id)!; return lineDist(p, q.pts) <= q.w / 2; })
        || d.zones.some(zn => kinds(m).has(zn.kind) && (zn.poly ? inPoly(p, zn.poly) : false))
        || (kinds(m).has('quay') && plan.quay.some(q => inPoly(p, q)))
        || m.footprint.some(g => g !== f && x >= (g as R4)[0] && x <= (g as R4)[1] && y >= (g as R4)[2] && y <= (g as R4)[3]);
      if (!ok) miss++;
    }
    if (miss) bad2.push(`${m.id} [${f}]: ${miss}/${tot} cells off its spans`);
  }
  for (let i = 0; i < ms.length; i++) for (let j = i + 1; j < ms.length; j++) for (const a of ms[i]!.footprint as R4[]) for (const b of ms[j]!.footprint as R4[]) if (a[0] < b[1] && b[0] < a[1] && a[2] < b[3] && b[2] < a[3]) bad2.push(`${ms[i]!.id} × ${ms[j]!.id} overlap`);
  out.push(rule('MS-2', G, 'Footprints stand on the plots, streets and zones the structure spans, and never on each other', ms.length, bad2, 'all', 'megastructures[].spans · supports.zones'));

  // MS-3 (mega-check) a bridge passes ≥ 3 m clear of any sky rail pylon, and no pylon stands inside a footprint (the Skyport portal carries the beam on purpose)
  const bad3: string[] = [], nb = ms.reduce((n, m) => n + (m.bridges?.length ?? 0), 0);
  for (const m of ms) {
    for (const b of m.bridges ?? []) { const near = Math.min(...d.pylons.map(p => rectDist(p.p as V, b.rect))); if (near < 3 - TOL) bad3.push(`${m.id}/${b.id} nearest pylon ${f1(near)} m`); }
    if (!(m.supports as any).portal) for (const f of m.footprint as R4[]) { const hit = d.pylons.filter(p => rectDist(p.p as V, f) === 0); if (hit.length) bad3.push(`${m.id} holds pylons at ${hit.map(p => p.p.map(Math.round).join(',')).join(' ')}`); }
  }
  out.push(rule('MS-3', G, 'Bridges stay ≥ 3 m from every sky rail pylon; no pylon inside a footprint (the Skyport portal excepted)', nb + ms.length, bad3, '≥ 3 m', 'standards.levels.bridges · out/datum/mega-check.ts MS-3'));

  // MS-4 (mega-check) a pier tower with a portal: legs ≥ clearHalfWidth off the line, the station's platforms inside the portal, no pylon within 1 m of a leg
  const bad4: string[] = [], portals = ms.filter(m => (m.supports as any).portal);
  for (const m of portals) {
    const clear = (m.supports as any).portal.clearHalfWidth as number, legs = (m as any).legs as R4[], fp = m.footprint[0] as R4;
    for (const l of legs) { const off = lineD(c, l); if (off < clear - TOL) bad4.push(`${m.id} leg [${l}] ${f1(off)} m off the line (< ${clear})`); }
    const st = stations.find(s => m.spans.skyrail?.startsWith(`${s.line}/${s.id}`));
    if (st) { const h = st.length / 2; const inside = st.at[0] - h >= fp[0] - TOL && st.at[0] + h <= fp[1] + TOL, centred = Math.abs(st.at[0] - (fp[0] + fp[1]) / 2) < 0.5; if (!inside && !centred) bad4.push(`${m.id}: station ${st.id} ${st.at[0] - h}–${st.at[0] + h} neither inside nor centred in the portal ${fp[0]}–${fp[1]}`); }
    else bad4.push(`${m.id}: spans.skyrail names no station`);
    const clash = d.pylons.filter(p => legs.some(l => rectDist(p.p as V, l) < 1));
    if (clash.length) bad4.push(`${m.id}: ${clash.length} pylons within 1 m of a leg`);
  }
  out.push(rule('MS-4', G, 'A pier tower\'s legs stand ≥ clearHalfWidth off the sky rail line, its station\'s platforms lie inside the portal, no pylon clashes with a leg', portals.length, bad4, 'all', 'megastructures[].supports.portal · out/datum/mega-check.ts MS-4'));

  // MS-5 supports land in allowed zones: every deck column, colonnade column and leg stands in the zones the structure lists (own plots, median, quay), never in a carriageway or on the tram reservation
  const bad5: string[] = [], mzones = d.zones.filter(zn => zn.kind === 'median'), tracks = d.tram.map(p => p.samples.map(s => s.p as V));
  const swept = plan.standards.tram.trackCentres / 2 + 0.5, quayRings = plan.quay;
  const okAt = (m: (typeof ms)[number], p: V) => {
    const zs = m.supports.zones as string[], own = zs.includes('own plots') && (m.spans.plots.some(id => inPoly(p, d.plots.find(q => q.id === id)!.polyV)) || (m.footprint as R4[]).some(f => rectDist(p, f) === 0)), med = zs.includes('median') && mzones.some(zn => inPoly(p, zn.poly!)), qy = zs.includes('quay') && quayRings.some(q => inPoly(p, q));
    return own || med || qy;
  };
  const inCarriage = (p: V) => d.roads.some(r => !r.gyratory && lineDist(p, r.line) < r.w / 2 - 0.01) && !mzones.some(zn => inPoly(p, zn.poly!));
  let cols = 0;
  for (const m of ms) {
    const g = w.megastructures.find(x => x.id === `mega/${m.id}`);
    const pts: { id: string; p: V }[] = [
      ...w.levels.flatMap(l => l.decks.filter(x => x.id.startsWith(`mega/${m.id}/`)).flatMap(x => x.columns.map((p, i) => ({ id: `${x.id} col ${i}`, p })))),
      ...(g?.prims ?? []).filter(p => /colonnade-\d+-col-/.test(p.id)).map(p => ({ id: p.id, p: p.c })),
      ...(g?.parts ?? []).filter(p => /\/leg-\d+$/.test(p.id)).map(p => ({ id: p.id, p: [bb(p.ring)[0] / 2 + bb(p.ring)[1] / 2, bb(p.ring)[2] / 2 + bb(p.ring)[3] / 2] as V })),
    ];
    for (const { id, p } of pts) {
      cols++;
      if (!okAt(m, p)) bad5.push(`${id} at ${p.map(Math.round)} outside ${(m.supports.zones as string[]).join(' / ')}`);
      else if (inCarriage(p)) bad5.push(`${id} in a carriageway`);
      else if (tracks.some(t => lineDist(p, t) < swept)) bad5.push(`${id} on the tram's swept envelope`);
    }
  }
  out.push(rule('MS-5', G, 'Columns, colonnade columns and legs stand only in the zones the structure lists (own plots, median, quay), never in a carriageway or the tram\'s swept envelope', cols, bad5, 'all', 'megastructures[].supports.zones · levels.bridges'));
  return out;
}

// ------------------------------------------------------------------ levels
const pieceKind = (id: string, level: string, joinsStation: boolean) =>
  /\/bridge(-|$)/.test(id) ? (level === 'concourse' ? (joinsStation ? 'skybridge' : 'bridge') : 'bridge') : /gallery/.test(id) ? 'gallery' : /promenade/.test(id) ? 'concourse' : /mezzanine/.test(id) ? 'deck' : level === 'concourse' ? 'concourse' : /crane-walk|\/walk$/.test(id) ? 'deck' : 'deck';

export function levelChecks(c: WCtx): Check[] {
  const { plan, d, w, gen, L, S } = c, out: Check[] = [], G = 'Levels', z = zOf(c), pav = 0.18;
  const decks = w.levels.flatMap(l => l.decks.map(x => ({ id: x.id, level: l.name, z: l.height, ring: x.ring as V[], rect: bb(x.ring as V[]), group: x.group, thickness: x.thickness, columns: x.columns })));
  const plats = w.transit.stations.map(s => ({ id: s.id, level: 'concourse', rect: bb(s.ring as V[]) }));
  const groups = deckGroups([...decks.map(x => ({ id: x.id, level: x.level, rect: x.rect })), ...plats]);
  const byGroup = new Map<string, string[]>();
  for (const [id, g] of groups) byGroup.set(g, [...(byGroup.get(g) ?? []), id]);
  const conns = w.connectors, servesGroup = (g: string) => conns.filter(k => k.serves.some(s => groups.get(s) === g));

  // L-1 every level reaches the street: each walkable piece has ≥ minWaysDown ways down, one step-free, and the pieces join up to street level
  const bad1: string[] = [];
  const reachG = new Set<string>(), edges = new Map<string, Set<string>>();
  for (const k of conns) {
    const gs = [...new Set(k.serves.map(s => groups.get(s)).filter((x): x is string => !!x))];
    if (k.from === 'street') gs.forEach(g => reachG.add(g));
    for (const a of gs) for (const b of gs) if (a !== b) (edges.get(a) ?? edges.set(a, new Set()).get(a)!).add(b);
  }
  for (let moved = true; moved;) { moved = false; for (const g of [...reachG]) for (const h of edges.get(g) ?? []) if (!reachG.has(h)) { reachG.add(h); moved = true; } }
  for (const [g, ids] of byGroup) {
    const level = decks.find(x => x.id === ids[0])?.level ?? 'concourse', std = L.spacing[level] as { minWaysDown: number } | undefined;
    if (!reachG.has(g)) { bad1.push(`${g} (${level}) never reaches the street`); continue; }
    const ways = servesGroup(g).filter(k => isWayDown(k.kind, level)), min = std?.minWaysDown ?? 1;
    if (ways.length < min) bad1.push(`${g}: ${ways.length} ways down (< ${min})`);
    else if (level === 'deck' && !servesGroup(g).some(k => k.stepFree)) bad1.push(`${g}: no step-free way`);
  }
  for (const l of L.list as { id: string }[]) if (l.id !== 'street' && !decks.some(x => x.level === l.id) && !plan.megastructures?.some(m => m.levels.some(v => v.level === l.id && v.z))) bad1.push(`level ${l.id} has no walkable surface`);
  out.push(rule('L-1', G, 'Every level reaches the street: each walkable piece has ≥ 2 ways down (one step-free on a deck) and joins street level through connectors', byGroup.size, bad1, 'all', 'standards.levels.spacing · brief 004 (every one connects to the street)'));

  // L-2 travel: nowhere on a deck is further than maxTravel (walked) from a way down; a lift within liftEvery of every deck point
  const bad2: string[] = [], seen = new Set(gen.travel.map(t => t.group));
  for (const t of gen.travel) if (t.max > t.limit + TOL) bad2.push(`${t.group} worst ${f1(t.max)} m (> ${t.limit}) at ${t.worst.map(Math.round)}`);
  for (const [g, ids] of byGroup) { const lv = decks.find(x => x.id === ids[0])?.level ?? 'concourse', lim = L.spacing[lv]?.maxTravel; if (lim && !seen.has(g)) bad2.push(`${g} (${lv}) not measured`); }
  const liftD = L.spacing.deck.liftEvery as number, lifts = conns.filter(k => k.kind === 'lift' || k.kind === 'goodsLift');
  for (const [g, ids] of byGroup) {
    if (decks.find(x => x.id === ids[0])?.level !== 'deck') continue;
    const mine = lifts.filter(k => k.serves.some(s => groups.get(s) === g));
    for (const id of ids) {
      const x = decks.find(q => q.id === id); if (!x) continue;
      const [x0, x1, y0, y1] = x.rect;
      let worst = 0;
      for (let px = x0; px <= x1 + 5; px += 10) for (let py = y0; py <= y1 + 5; py += 10) { const p: V = [Math.min(px, x1), Math.min(py, y1)], dd = Math.min(...mine.map(k => Math.hypot(k.foot[0] - p[0], k.foot[1] - p[1])), Infinity); worst = Math.max(worst, dd); }
      if (worst > liftD) bad2.push(`${id}: a deck point is ${worst === Infinity ? 'with no lift' : f1(worst) + ' m'} from a lift (> ${liftD})`);
    }
  }
  out.push(rule('L-2', G, 'Travel: no point on a deck or concourse is further than maxTravel (30 m / 45 m, walked round halls) from a way down, nor liftEvery (120 m) from a lift', byGroup.size, bad2, 'all', 'standards.levels.spacing'));

  // L-3 widths: pieces by kind (deck 6, gallery 5, deck aisle 8, bridge 4.5, skybridge 5, concourse 8), stairs 3, ramps 2.4, cart ramps 7, station platforms 4
  const bad3: string[] = [], mw = L.minWidth as Record<string, number>, joins = new Set(plan.megastructures?.flatMap(m => (m.bridges ?? []).filter((b: any) => b.joinsStation).map(b => `mega/${m.id}/bridge-${b.id}`)) ?? []);
  for (const x of decks) {
    const k = pieceKind(x.id, x.level, joins.has(x.id)), min = x.id.startsWith('bank/') && k === 'bridge' ? x.rect[1] - x.rect[0] : Math.min(x.rect[1] - x.rect[0], x.rect[3] - x.rect[2]); // a bank bridge crosses north-south: its width is across the line
    if (min < mw[k]! - TOL) bad3.push(`${x.id} ${f1(min)} m < ${k} ${mw[k]}`);
  }
  const need: Record<string, number> = { stair: mw.stair!, stairTower: mw.stair!, ramp: mw.ramp!, cartRamp: mw.cartRamp! };
  for (const k of conns) if (need[k.kind] !== undefined && k.width < need[k.kind]! - TOL) bad3.push(`${k.id} ${k.kind} ${k.width} m < ${need[k.kind]}`);
  const platW = S.transit.stationEntrance ? 4 : 4; // levels.station: side platforms 4 m (beam edge 4.75 → outer edge 8)
  for (const p of plats) { const min = Math.min(p.rect[1] - p.rect[0], p.rect[3] - p.rect[2]); if (min < platW - TOL) bad3.push(`${p.id} ${f1(min)} m < ${platW}`); }
  out.push(rule('L-3', G, 'Minimum widths: deck 6, gallery 5, aisle 8, bridge 4.5, skybridge 5, concourse 8, stair 3, ramp 2.4, cart ramp 7, platform 4', decks.length + conns.length + plats.length, bad3, 'all', 'standards.levels.minWidth · levels.station'));

  // L-4 headroom: a deck or bridge over a carriageway keeps headroom.road under its soffit; colonnades clear 4.6; plan colonnades list the clear height
  const bad4: string[] = [], roads = d.roads.filter(r => !r.gyratory);
  for (const x of decks) for (const r of roads) {
    if (lineToPoly(r.line, x.ring) >= r.w / 2 - 0.01) continue;
    const clear = x.z - x.thickness - pav;
    if (clear < L.headroom.road - TOL) bad4.push(`${x.id} over ${r.id}: ${f1(clear)} m < ${L.headroom.road}`);
  }
  for (const m of plan.megastructures ?? []) for (const k of ((m as any).colonnade ?? []) as { faces: string; clearHeight: number }[]) if (k.clearHeight < L.headroom.colonnade - TOL) bad4.push(`${m.id} colonnade on ${k.faces}: ${k.clearHeight} m < ${L.headroom.colonnade}`);
  for (const x of decks) if (x.level !== 'street' && x.z - x.thickness - pav < L.headroom.walk) bad4.push(`${x.id} soffit under walk headroom`);
  out.push(rule('L-4', G, 'Headroom: ≥ 5.5 m under a deck or bridge over a carriageway, ≥ 4.6 m under a colonnade, ≥ 3 m on a walk', decks.length + (plan.megastructures?.length ?? 0), bad4, 'all', 'standards.levels.headroom · structureDepth'));

  // L-5 stations: each side has a stair bank, an escalator run and a lift on public ground (never a plot), outside the beam envelope + 1 m; a megastructure concourse within 30 m joins the station by a skybridge ≥ 5 m wide
  const bad5: string[] = [], lines = skyLines(c), half = S.skyrailEnvelope.half as number, built = new Set(['residential', 'mixed', 'commercial', 'industrial', 'port', 'civic']);
  for (const s of plats) {
    const id = s.id.replace('station/', ''), bank = conns.filter(k => k.id.startsWith(`bank/${id}/`));
    for (const [kind, name] of [['stairTower', 'stair bank'], ['escalator', 'escalator run'], ['lift', 'lift']] as const) if (!bank.some(k => k.kind === kind)) bad5.push(`${s.id}: no ${name}`);
    for (const k of bank) {
      const ring = k.ring as V[], [x0, x1, y0, y1] = bb(ring), ctr: V = [(x0 + x1) / 2, (y0 + y1) / 2];
      const plot = d.plots.find(p => built.has(p.use) && inPoly(ctr, p.polyV));
      if (plot) bad5.push(`${k.id} stands on plot ${plot.id}`);
      if (/\/(tower|run-a|lift)$/.test(k.id) && lines.some(l => lineToPoly(l, ring) < half + 1 - TOL)) bad5.push(`${k.id} inside the beam envelope + 1 m`);
      // brief 007: the stair tower and the lift stand outside the platform's outer edge (platformClear 7.75 = envelope half + platform 4), never under it: bank/S1/circus/l/tower at y 4.75–11.75 fails
      const pc = S.transit.stationEntrance.platformClear as number | undefined;
      if (pc && /\/(tower|lift)$/.test(k.id) && lines.some(l => lineToPoly(l, ring) < pc - TOL)) bad5.push(`${k.id} under the platform (< ${pc} m from the line)`);
    }
  }
  const stations = (plan.skyrail as any[]).flatMap(l => (l.stations ?? []).map((s: any) => ({ ...s, line: l.id })));
  for (const m of plan.megastructures ?? []) {
    if (!m.levels.some(l => l.level === 'concourse') || (m.supports as any).portal) continue;
    const conc = (m.bridges ?? []).filter(b => b.level === 'concourse');
    for (const s of stations) {
      const plat: R4 = [s.at[0] - s.length / 2, s.at[0] + s.length / 2, -8, 8], near = Math.min(...(m.footprint as R4[]).map(f => Math.hypot(Math.max(0, f[0] - plat[1], plat[0] - f[1]), Math.max(0, f[2] - plat[3], plat[2] - f[3]))));
      if (near > 30) continue;
      const sb = conc.filter((b: any) => b.joinsStation ? b.joinsStation.station === s.id && b.joinsStation.line === s.line : String(b.joins).includes(`${s.line}/${s.id}`));
      if (!sb.length) bad5.push(`${m.id} is ${f1(near)} m from ${s.line}/${s.id} with no skybridge`);
      for (const b of sb) if (Math.min(b.rect[1] - b.rect[0], b.rect[3] - b.rect[2]) < mw5(L) - TOL) bad5.push(`${m.id}/${b.id} narrower than a skybridge`);
    }
  }
  out.push(rule('L-5', G, 'Stations: every side has a stair bank, escalator run and lift on public ground outside the beam envelope + 1 m (stair tower and lift outside the platform, ≥ platformClear); a megastructure concourse within 30 m joins by a skybridge ≥ 5 m', plats.length, bad5, 'all', 'standards.levels.station · out/datum/mega-check.ts L-5'));

  // L-6 ids and heights: ids are unique and well formed across every layer; connectors serve ids that exist; levels sit at the standard heights
  const bad6: string[] = [], all: string[] = [];
  const push = (xs: string[]) => all.push(...xs);
  push(w.buildings.map(m => m.id)); push(w.civic.map(m => m.id)); push(w.frontages.flatMap(f => [f.id, f.front.id, f.entrance.id, f.canopy.id, f.sign.id, ...(f.service ? [f.service.id] : [])]));
  push(w.roofs.flatMap(r => [r.id, ...r.parts.map(p => p.id)])); push(conns.flatMap(k => [k.id, ...(k.dress ?? []).map(p => p.id)])); push(w.street.map(s => s.id)); push(decks.map(x => x.id));
  push(w.megastructures.flatMap(m => [...m.parts.map(p => p.id), ...m.prims.map(p => p.id)])); push(w.transit.stations.map(s => s.id)); push(w.transit.stops.map(s => s.id)); push(w.ground.plots.map(p => p.id));
  const seenId = new Set<string>(), dup = new Set<string>();
  for (const id of all) { if (seenId.has(id)) dup.add(id); seenId.add(id); if (!/^[A-Za-z0-9][A-Za-z0-9_\-./]*$/.test(id)) bad6.push(`${id}: malformed id`); }
  for (const id of [...dup].slice(0, 20)) bad6.push(`${id}: duplicate id`);
  const known = new Set([...decks.map(x => x.id), ...plats.map(p => p.id)]);
  for (const k of conns) for (const s of k.serves) if (!known.has(s)) bad6.push(`${k.id} serves unknown ${s}`);
  const list = L.list as { id: string; z: number }[], deckZ = pav + plan.standards.buildings.plinth + plan.standards.buildings.groundStorey + plan.standards.buildings.upperStorey;
  if (list.map(l => l.id).join() !== 'street,deck,concourse') bad6.push(`levels ${list.map(l => l.id)} ≠ street, deck, concourse`);
  if (z.street !== 0) bad6.push('street z ≠ 0');
  if (Math.abs((z.deck ?? 0) - deckZ) > TOL) bad6.push(`deck z ${z.deck} ≠ ${deckZ} (pavement + plinth + ground + one upper storey)`);
  if (Math.abs((z.concourse ?? 0) - plan.standards.skyrail.height) > TOL) bad6.push(`concourse z ${z.concourse} ≠ skyrail.height ${plan.standards.skyrail.height}`);
  for (const l of w.levels) if (Math.abs(l.height - (z[l.name] ?? NaN)) > TOL) bad6.push(`${l.id} height ${l.height} ≠ plan ${z[l.name]}`);
  out.push(rule('L-6', G, 'Ids are unique and well formed in every layer; connectors serve existing decks; levels are street 0, deck 9.43, concourse = sky rail height', all.length, bad6, 'all', 'brief 004 (every element has a stable id) · standards.levels.list'));

  // L-7 a lift is attached along its whole run: at every height from its bottom to its top the shaft stands inside or within attachGap of a solid that is there
  // at that height (a building, megastructure part or prim, civic mass, gate, stair tower or stair core, deck slab, station platform or landmark). Failing example (flyby
  // 2026-10-13): mega/skyport-tower/lift-sw ran 0.18 -> 89.6 from the south-west leg, which stops at 34: above it the shaft stood alone 29 m from the stem.
  const gap7 = (L.connectors.lift.attachGap as number | undefined) ?? ATTACH_GAP, bad7: string[] = [], shafts = conns.filter(k => k.kind === 'lift' || k.kind === 'goodsLift');
  const solids = attachSolids(c);
  for (const k of shafts) {
    const kb = bb(k.ring as V[]), near = solids.filter(x => x.id !== k.id && x.bb[0] <= kb[1] + gap7 + 1 && x.bb[1] >= kb[0] - gap7 - 1 && x.bb[2] <= kb[3] + gap7 + 1 && x.bb[3] >= kb[2] - gap7 - 1);
    const zs: number[] = []; for (let zz = k.bottom + 0.05; zz < k.top - 0.05; zz += 1) zs.push(zz); zs.push(k.top - 0.05);
    let from = NaN, to = NaN, far = 0;
    for (const zz of zs) {
      const g = Math.min(Infinity, ...near.filter(x => x.z0 <= zz + 1e-6 && x.z1 >= zz - 1e-6).map(x => ringGap(k.ring as V[], x.ring)));
      if (g > gap7 + TOL) { if (isNaN(from)) from = zz; to = zz; far = Math.max(far, g); }
    }
    if (!isNaN(from)) bad7.push(`${k.id} stands alone from z ${f1(from)} to ${f1(to)} (${isFinite(far) ? `${f1(far)} m` : 'nothing'} from the nearest structure at that height)`);
  }
  out.push(rule('L-7', G, `Every lift is attached to a structure along its whole run: at every height of the shaft a solid stands within ${gap7} m`, shafts.length, bad7, `≤ ${gap7} m`, 'standards.levels.connectors.lift (attachGap [S]; the kit lift_tube stands 0.7 m off the Skyport stem on a spine) · brief 007 round 2 (Skyport lifts floated above the legs)'));
  return out;
}

/** L-7's default attach gap (m) until standards.levels.connectors.lift.attachGap is in the plan (Datum): the kit's lift tube stands LIFT_R 9.2 - r 1.5 - stem 7 = 0.7 m off the Skyport stem, tied by a spine. */
export const ATTACH_GAP = 0.75;
type Solid = { id: string; ring: V[]; z0: number; z1: number; bb: R4 };
/** Everything a lift may be attached to, as plan rings with a z range. */
export function attachSolids(c: WCtx): Solid[] {
  const { w } = c, out: Solid[] = [], add = (id: string, ring: V[], z0: number, z1: number) => out.push({ id, ring, z0, z1, bb: bb(ring) });
  const circle = (cc: V, r: number): V[] => { const R = r / Math.cos(Math.PI / 16); return Array.from({ length: 16 }, (_, i) => [cc[0] + R * Math.cos((i * Math.PI) / 8), cc[1] + R * Math.sin((i * Math.PI) / 8)] as V); }; // circumscribed: the polygon holds the whole round solid
  const primRing = (p: { shape: string; c: V; size: [number, number, number]; yaw: number }): V[] => {
    if (p.shape === 'cyl' || p.shape === 'sphere' || p.shape === 'saucer') return circle(p.c, p.size[0] / 2);
    const [a, b] = [p.size[0] / 2, p.size[1] / 2], cs = Math.cos(p.yaw), sn = Math.sin(p.yaw);
    return ([[-a, -b], [a, -b], [a, b], [-a, b]] as V[]).map(([x, y]) => [p.c[0] + x * cs - y * sn, p.c[1] + x * sn + y * cs] as V);
  };
  for (const m of [...w.buildings, ...w.civic, ...w.gates, ...w.transit.stations, ...w.megastructures.flatMap(g => g.parts)]) add(m.id, m.ring as V[], m.base, m.base + m.height);
  for (const p of w.megastructures.flatMap(g => g.prims)) add(p.id, primRing(p as any), p.base, p.base + p.size[2]);
  // stair towers and stair cores (a vertical stair: foot = head, the core's enclosure) are structures a lift car rides beside or inside
  for (const k of w.connectors) if (k.kind === 'stairTower' || (k.kind === 'stair' && k.foot[0] === k.head[0] && k.foot[1] === k.head[1])) add(k.id, k.ring as V[], k.bottom, k.top);
  for (const l of w.levels) for (const dk of l.decks) add(dk.id, dk.ring as V[], l.height - dk.thickness, l.height);
  for (const l of w.landmarks) add(l.id, circle(l.centre as V, l.radius), l.base, l.base + (l.shape === 'sphere' ? 2 * l.radius : l.height));
  return out;
}
const mw5 = (L: any) => L.minWidth.skybridge as number;
