// Streetscape rules SF-1 … SF-9 (standards.streetscape.rules), on the generator's street items. SF-10 … SF-17 are in street-rules.ts.
// Each rule is the plan's "if" sentence made measurable: items are placed from their footprints (street-geo.ts), zones from the
// plan's sections, so a generator that breaks the standard fails here whatever it meant to do.
import { ctxOf as genCtx } from '../world/gen/common';
import { SIGHT_CLEAR, junctionCorners, kindOf, plotRing } from '../world/gen/edges';
import { landingsOf } from '../world/gen/street/crossings';
import { PART_KINDS } from '../world/gen/street/parts';
import type { Check } from './checks';
import { type V, inPoly } from './geom';
import { speciesOf } from './trees';
import { FLAT, Grid, type Spot, WATCHERS, indexes, roadAt, spotsOf, tramBlock, walkBlock } from './street-geo';
import { type WCtx, TOL, bb, boxRing, f1, ringGap, rule } from './world-ctx';

const G = 'Streetscape';
const dist = (a: V, b: V) => Math.hypot(a[0] - b[0], a[1] - b[1]);
const tag = (s: Spot) => `${s.s.kind} ${s.s.id.replace(/^street\//, '')}`;
const decksOf = (c: WCtx) => c.w.levels.filter(l => l.height > 1).flatMap(l => l.decks.map(x => ({ id: x.id, z: l.height, level: l.name, rect: bb(x.ring as V[]) })));
/** Distance from p to the boundary of a deck rect (0 on it, negative inside is not needed: items are inside). */
const toEdge = (p: V, r: [number, number, number, number]) => Math.min(p[0] - r[0], r[1] - p[0], p[1] - r[2], r[3] - p[1]);

export function streetChecks(c: WCtx): Check[] {
  const out: Check[] = [], { plan, d, w, S } = c, spots = spotsOf(c), gnd = spots.filter(x => x.ground), gc = genCtx(plan, d, c.civ, 0.18);
  const decks = decksOf(c), ix = indexes(gc);
  const half = S.skyrailEnvelope.half as number, env = S.skyrailEnvelope;

  // SF-1 the clear walking zone is never blocked: no item, spill, tree pit, kit, bank or watcher in a walk zone (road, mall, walk) or in a deck's clear width
  const bad1: string[] = [];
  for (const sp of gnd) { if (FLAT.has(sp.s.kind)) continue; const why = walkBlock(gc, sp.pts, sp.s.edge); if (why) bad1.push(`${tag(sp)} in ${why}`); }
  for (const sp of spots.filter(x => !x.ground)) {
    const dk = decks.find(x => Math.abs(x.z - sp.s.z) < 0.5 && toEdge(sp.s.at, x.rect) >= -0.01); if (!dk) continue;
    const band = (S.levels[dk.level] ?? S.levels.deck).edgeBand as number;
    if (sp.pts.some(p => toEdge(p, dk.rect) > band + TOL) && !/\/(mezzanine|bridge)/.test(dk.id)) bad1.push(`${tag(sp)} stands in ${dk.id}'s clear width`);
  }
  out.push(rule('SF-1', G, 'The clear walking zone is never blocked (road, mall and walk zones; a deck\'s clear width)', spots.length, bad1, 'none', 'streetscape.sections · levels.deck/concourse · Inclusive Mobility'));

  // SF-2 nothing but bollards, crossing beacons, a crossing's own parts and tram stop kits within 2 m of a crossing (its landing: the footprint where people step on or off, drawn or not) or a doorway jamb
  type Door = { c: V; ring: V[]; f: (typeof w.frontages)[number] };
  const bad2: string[] = [], cross = landingsOf(w).map(l => ({ s: l, ring: boxRing(l.at, l.size[0], l.size[1], l.yaw) })), doors = new Grid<Door>(10, f => f.c), own = PART_KINDS as readonly string[];
  const exempt = (s: Spot) => ['bollard', 'beacon', 'crossing'].includes(s.s.kind) || own.includes(s.s.kind) || s.kit;
  const DC = (S.doors ?? {}) as { clear?: number; wallUnit?: number; wallUnits?: string[] }, doorLim = (s: Spot) => (DC.wallUnits ?? []).includes(s.s.kind) ? DC.wallUnit ?? 2 : DC.clear ?? 2; // streetscape.doors (brief 026): 2 m, a wall unit nobody queues at 0.6
  for (const f of w.frontages.filter(q => q.level === 'street')) doors.add({ c: f.entrance.c as V, ring: boxRing(f.entrance.c as V, f.entrance.size[0], f.entrance.size[1], f.entrance.yaw), f });
  const drives = (w.ground.drives ?? []).map(x => ({ id: x.id, ring: x.ring as V[] })); // brief 023: nothing stands on a drive; nothing but bollards and beacons within 2 m of one
  for (const sp of gnd) {
    if (!FLAT.has(sp.s.kind)) for (const x of drives) { const gap = ringGap(sp.ring, x.ring); if (gap < TOL) bad2.push(`${tag(sp)} stands on the drive ${x.id}`); else if (!exempt(sp) && gap < 2 - TOL) bad2.push(`${tag(sp)} ${f1(gap)} m from the drive ${x.id}`); }
    if (exempt(sp)) continue;
    for (const x of cross) if (x.s.edge === sp.s.edge && dist(x.s.at, sp.s.at) < 12 && ringGap(sp.ring, x.ring) < 2 - TOL) { bad2.push(`${tag(sp)} ${f1(ringGap(sp.ring, x.ring))} m from a crossing`); break; }
    const lim = doorLim(sp);
    for (const f of doors.near(sp.s.at, 6)) if (ringGap(sp.ring, f.ring) < lim - TOL) { bad2.push(`${tag(sp)} ${f1(ringGap(sp.ring, f.ring))} m from the door of ${f.f.id} (< ${lim})`); break; }
  }
  out.push(rule('SF-2', G, `Nothing but bollards, crossing beacons and tram stop kits within 2 m of a crossing or ${DC.clear ?? 2} m of a doorway's jambs (a wall unit nobody queues at: ${DC.wallUnit ?? 2} m), or 2 m of a drive; nothing at all on a drive`, gnd.length, bad2, 'none', 'streetscape.crossings · bollards · doors (brief 026) · brief 023 (drives: Sky 2026-10-19)'));

  // SF-3 the tram's own space: nothing but track, lawn and the stop kit's platforms in a reservation or a track's swept envelope (± 1.325 + 0.5)
  const bad3: string[] = [];
  for (const sp of gnd) { if (sp.kit || FLAT.has(sp.s.kind)) continue; const why = tramBlock(gc, sp.pts); if (why) bad3.push(`${tag(sp)} on ${why}`); }
  out.push(rule('SF-3', G, 'Nothing stands on a tram reservation or within a track\'s swept envelope (centre ± 1.825 m) except track, lawn and the stop kit', gnd.length, bad3, 'none', 'streetscape.sections.primary.tram · transit.tramStop'));

  // SF-4 trees clear the beam: canopy ≥ 5 m (3D) from the envelope (3.75 m each side, underside to the platform level); no pit in the envelope's plan footprint
  const bad4: string[] = [], trees = gnd.filter(x => x.s.kind === 'tree'), clear4 = S.trees.keepClear.beam ? 5 : 5, top = plan.standards.skyrail.height;
  for (const t of trees) {
    const sp = speciesOf(S, t.s.variant) as { crownBase: number; top: number; spread: number; pit: V };
    const gap = ix.sky.dist(t.s.at, 60);
    if (t.pts.some(p => ix.sky.dist(p, half + 1) < half - TOL)) { bad4.push(`${t.s.id}: pit inside the beam envelope`); continue; }
    const h = Math.max(0, gap - half - sp.spread / 2), v = Math.max(0, env.underside - (t.s.z + sp.top), t.s.z + sp.crownBase - top), d3 = Math.hypot(h, v);
    if (d3 < clear4 - TOL) bad4.push(`${t.s.id}: canopy ${f1(d3)} m from the beam`);
  }
  out.push(rule('SF-4', G, 'Trees: canopy ≥ 5 m (3D) from the sky rail beam envelope, no pit in its plan footprint', trees.length, bad4, '≥ 5 m', 'streetscape.trees.keepClear.beam · skyrailEnvelope'));

  // SF-5 a kerb-zone item stands ≥ 0.45 m from the kerb face (a hydrant ≥ 0.6)
  const bad5: string[] = [], onRoad: { sp: Spot; dk: number; sec: ReturnType<typeof roadAt> }[] = [];
  for (const sp of gnd) {
    if (FLAT.has(sp.s.kind) || sp.kit || !/(\/e\d+|^circus)$/.test(sp.s.edge)) continue; // plot edges and the circus ring: the items a street section dresses
    const r = roadAt(gc, sp.s.at, sp.s.edge); if (!r || r.dk < -0.01 || r.dk > r.sec.pavement) continue;
    onRoad.push({ sp, dk: r.dk, sec: r });
    if (r.dk > r.sec.kerb) continue; // a kerb-zone item: its centre is in the kerb zone
    const near = Math.min(...sp.pts.map(p => roadAt(gc, p, sp.s.edge)!.dk)), lim = sp.s.kind === 'hydrant' ? c.S.catalogue.hydrant.kerbOffset : 0.45;
    if (near < lim - TOL) bad5.push(`${tag(sp)} ${f1(near)} m from the kerb face (< ${lim})`);
  }
  out.push(rule('SF-5', G, 'A kerb-zone item stands ≥ 0.45 m from the kerb face (hydrant ≥ 0.6)', onRoad.length, bad5, '≥ 0.45 / 0.6 m', 'streetscape.sections · catalogue.hydrant.kerbOffset · Manual for Streets'));

  // SF-6 skip, never squeeze: an item in the kerb zone fits it (offset + depth ≤ zone width); a frontage-zone item stays inside the frontage zone; a widening item stands only where the kerb zone has room (or off the road)
  const bad6: string[] = [];
  for (const { sp, sec: r } of onRoad) {
    const cat = S.catalogue[sp.s.kind] as { zone?: string } | undefined, zone = String(cat?.zone ?? (['tree', 'lamp'].includes(sp.s.kind) ? 'kerb' : ''));
    const ds = sp.pts.map(p => roadAt(gc, p, sp.s.edge)!.dk), lo = Math.min(...ds), hi = Math.max(...ds), s = r!.sec;
    if (zone.startsWith('kerb') && !zone.includes('widening') && hi > s.kerb + TOL) bad6.push(`${tag(sp)} reaches ${f1(hi)} m (kerb zone ${s.kerb})`);
    else if (zone === 'frontage' && (lo < s.kerb + s.walk - TOL || hi > s.pavement + TOL)) bad6.push(`${tag(sp)} outside the frontage zone`);
    else if (zone === 'widening' && hi > s.kerb + TOL) bad6.push(`${tag(sp)} needs a widening (${f1(hi)} m > kerb ${s.kerb})`);
  }
  out.push(rule('SF-6', G, 'Skip, never squeeze: items fit their zone (offset + depth ≤ zone width); widening items only where a widening is', onRoad.length, bad6, 'all', 'streetscape.catalogue[].zone · rule'));

  // SF-7 counts and spacing: per street edge, the catalogue's spacing between like items (trees 12, lamps by type, benches, hydrants, mailboxes, columns, kiosks)
  const bad7: string[] = [], bySpacing: [string, (s: Spot) => boolean, (s: Spot) => number][] = [
    ['tree', s => s.s.kind === 'tree', () => S.trees.spacing],
    ['lamp', s => s.s.kind === 'lamp', s => (S.lamps.types[s.s.variant ?? '']?.spacing ?? 12) * (s.s.variant === 'atom-ring' && s.s.size[2] <= 6.5 ? 18 / 24 : 1)],
    ['bench', s => s.s.kind === 'bench' && !s.kit && !s.s.edge.startsWith('bank/'), () => 36], ['hydrant', s => s.s.kind === 'hydrant', () => 90], ['mailbox', s => s.s.kind === 'mailbox', () => 250],
    ['advertising-column', s => s.s.kind === 'advertising-column', () => 60], ['kiosk-food', s => s.s.kind === 'kiosk-food', () => 30], ['kiosk-flowers', s => s.s.kind === 'kiosk-flowers', () => 120],
  ];
  let n7 = 0;
  for (const [name, is, gap] of bySpacing) {
    const dressed = (x: Spot) => /(\/e\d+|^circus|^median\/\d+)$/.test(x.s.edge) || (name === 'tree' && !x.s.edge.startsWith('lawn/')); // spacing is a street edge's rule; park lawns are planted free, decks and malls by their own bands
    const by = new Map<string, Spot[]>(); for (const sp of spots.filter(x => is(x) && dressed(x))) by.set(sp.s.edge + (name === 'lamp' ? '|' + sp.s.variant : ''), [...(by.get(sp.s.edge + (name === 'lamp' ? '|' + sp.s.variant : '')) ?? []), sp]);
    for (const [k, list] of by) for (let i = 0; i < list.length; i++) { n7++; for (let j = i + 1; j < list.length; j++) { const lim = gap(list[i]!); if (name !== 'tree' && name !== 'lamp' && ['advertising-column', 'kiosk-food', 'kiosk-flowers'].includes(name)) continue; if (dist(list[i]!.s.at, list[j]!.s.at) < lim - 0.51) bad7.push(`${k}: two ${name} ${f1(dist(list[i]!.s.at, list[j]!.s.at))} m apart (< ${lim})`); } }
  }
  for (const [name, min] of [['advertising-column', 60], ['kiosk-food', 30], ['kiosk-flowers', 120]] as const) {
    const l = spots.filter(x => x.s.kind === name && !x.s.edge.startsWith('bank/') && !x.kit);
    n7 += l.length; for (let i = 0; i < l.length; i++) for (let j = i + 1; j < l.length; j++) if (dist(l[i]!.s.at, l[j]!.s.at) < min - 0.51) bad7.push(`two ${name} ${f1(dist(l[i]!.s.at, l[j]!.s.at))} m apart (< ${min})`);
  }
  const roadEdge = (sp: Spot) => { const r = roadAt(gc, sp.s.at, sp.s.edge); return r && r.dk >= -0.01 && r.dk <= r.sec.pavement ? r.sec.cls : ''; };
  for (const sp of spots.filter(x => x.s.kind === 'advertising-column' && !x.s.edge.startsWith('bank/'))) if (['access', 'primary'].includes(roadEdge(sp))) bad7.push(`${tag(sp)} on a ${roadEdge(sp)} edge (never)`);
  const dn = spots.filter(x => x.s.kind === 'newsstand' && !x.kit && !x.s.edge.startsWith('bank/') && roadEdge(x) === 'district');
  for (let i = 0; i < dn.length; i++) for (let j = i + 1; j < dn.length; j++) if (dist(dn[i]!.s.at, dn[j]!.s.at) < 120 - 0.51) bad7.push(`two district newsstands ${f1(dist(dn[i]!.s.at, dn[j]!.s.at))} m apart (< 120)`);
  // the curved kiosk (catalogue.kiosk-pavilion, brief 026): on a square, open plot or park (its `uses`), ≤ 1 per plot, never on a civic square or a plot kept for a station bank
  const KP = S.catalogue['kiosk-pavilion'] as { uses?: string[] } | undefined, kp = spots.filter(x => x.s.kind === 'kiosk-pavilion'), perPlot = new Map<string, number>();
  n7 += kp.length;
  for (const sp of kp) {
    const pl = d.plots.find(p => inPoly(sp.s.at, p.polyV));
    if (!pl || !(KP?.uses ?? []).includes(pl.use)) { bad7.push(`${tag(sp)} on ${pl ? `${pl.id} (${pl.use})` : 'no plot'}: a kiosk pavilion stands on a ${(KP?.uses ?? []).join(', ')} plot`); continue; }
    if ((pl as { civic?: unknown }).civic || (pl as { bank?: unknown }).bank) { bad7.push(`${tag(sp)} on ${pl.id}: never on a civic square or ground kept for a station bank`); continue; }
    perPlot.set(pl.id, (perPlot.get(pl.id) ?? 0) + 1);
  }
  for (const [id, n] of perPlot) if (n > 1) bad7.push(`${id}: ${n} kiosk pavilions (≤ 1 per plot)`);
  out.push(rule('SF-7', G, 'Counts and spacing per edge: trees 12 m, lamps by type, benches 36, hydrants 90, mailboxes 250, columns 60, food kiosks 30, flower kiosks and district newsstands 120 apart; kiosk pavilions ≤ 1 per square, open plot or park', n7 + dn.length, bad7, 'all', 'streetscape.trees.spacing · lamps.types · catalogue[].count'));

  // SF-8 corner visibility: nothing that blocks sight within 6 m of a junction corner's kerb tangent point
  const bad8: string[] = [], corners = junctionCorners(gc);
  const blockers = gnd.filter(x => x.s.sight === 'blocks');
  for (const sp of blockers) { const k = corners.find(q => Math.min(...sp.pts.map(p => dist(p, q))) < SIGHT_CLEAR - TOL); if (k) bad8.push(`${tag(sp)} ${f1(Math.min(...sp.pts.map(p => dist(p, k))))} m from the corner at ${k.map(Math.round)}`); }
  out.push(rule('SF-8', G, 'No sight-blocking item within 6 m of a junction corner\'s kerb tangent point', blockers.length, bad8, '≥ 6 m', 'streetscape.sight · rules SF-8'));

  // SF-9 watchers stand at every site the plan lists and nowhere else (stations, tram stops, gates, the square, the circus)
  const bad9: string[] = [], wk = spots.filter(x => WATCHERS.includes(x.s.kind)), count = (kind: string, test: (e: string) => boolean) => wk.filter(x => x.s.kind === kind && test(x.s.edge)).length;
  const want = (label: string, kind: string, test: (e: string) => boolean, n: number) => { const got = count(kind, test); if (got !== n) bad9.push(`${label}: ${got} ${kind} (want ${n})`); };
  let sites = 0;
  for (const l of plan.skyrail) for (const s of l.stations ?? []) {
    sites++;
    for (const side of ['l', 'r']) { want(`${l.id}/${s.id}/${side} platform`, 'canopy-camera', e => e === `station/${l.id}/${s.id}/${side}`, 2); want(`${l.id}/${s.id}/${side} bank foot`, 'pole-camera', e => e === `bank/${l.id}/${s.id}/${side}`, 1); }
    want(`${l.id}/${s.id} main bank`, 'civic-robot-dock', e => e.startsWith(`bank/${l.id}/${s.id}/`), 1);
  }
  for (const m of plan.megastructures ?? []) for (const b of m.bridges ?? []) if ((b as any).joinsStation) { sites++; want(`${m.id}/${b.id} skybridge`, 'canopy-camera', e => e === `mega/${m.id}/bridge-${b.id}`, 1); }
  for (const l of plan.tram) for (const s of l.stops ?? []) { sites++; for (const side of ['l', 'r']) want(`${l.id}/${s.id}/${side} tram stop`, 'pole-camera', e => e === `stop/${l.id}/${s.id}/${side}`, 1); }
  for (const g of plan.gates) { sites++; want(`gate ${g.id}`, 'pole-camera', e => e === g.id, 1); }
  for (const p of plan.plots.filter(q => q.use === 'square')) { sites++; want(`square ${p.id}`, 'mast-camera', e => e === p.id, 1); want(`square ${p.id}`, 'civic-robot-dock', e => e === p.id, 1); }
  sites++; want('circus', 'mast-camera', e => e === 'circus', 2); want('circus', 'civic-robot-dock', e => e === 'circus', 2);
  const okEdge = (e: string) => /^(stop|station|bank)\//.test(e) || plan.gates.some(g => g.id === e) || plan.plots.some(p => p.use === 'square' && p.id === e) || e === 'circus' || /^mega\/[^/]+\/bridge-/.test(e);
  for (const x of wk) if (!okEdge(x.s.edge)) bad9.push(`${tag(x)} stands at no watcher site`);
  out.push(rule('SF-9', G, 'Every station, skybridge, tram stop, gate, square and the circus has its watchers.sites set, and watchers stand nowhere else', sites, bad9, 'all', 'streetscape.watchers.sites · GDD §05'));
  return out;
}
