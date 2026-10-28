// BB-1..BB-4 (brief 075-F category 1): the billboards, pylons, wall boards and advertising columns the generator places by rule (tools/world/gen/billboards.ts),
// measured on the kit pieces' own footprints and heights, against the export's own geometry. The rules know nothing of how the generator placed a piece.
//   BB-1 a rooftop board stands on a sign-frame roof, faces a street its building fronts, replaces the procedural frame, keeps clear of the plant, and never
//        lifts a play-street roof past its cap (nor any roof past the crown tip); every sign-frame roof has its board or is logged with why.
//   BB-2 a wall board hangs on a wall of a building taller than its top, in front of that wall, its foot clear of the lower neighbour's roof and all on it.
//   BB-3 a pylon or advertising column blocks nothing it must not: its real footprint is out of the clear walking zone, the tram's space and the quay edge, 2 m off
//        every crossing landing and doorway, 6 m off every junction corner (it blocks sight), and nothing is over it below its top + 1.5 m.
//   BB-4 spacing and trade: advertising columns 60 m apart, pylons 60 m apart but for a vista pair, wall boards BB.wallGap apart and one per building, no BB piece
//        in the Handover window, and a blade or wall sign only on a trade its piece names.
//   BB-5 (brief 075-F1b) CornerTower_12 carries a neon HOTEL blade sign: it stands only on a building with a street-level hotel tenancy (stream.ts JUNCTION.hotel).
import type { KitAsset } from '../../../contracts/world/kit.schema';
import type { StreetItem, World } from '../../../contracts/world/world.schema';
import type { Check } from '../../plan/checks';
import { type V, dist, dot, sub } from '../../plan/geom';
import { quayBlock, roadAt, samples9, tramBlock, walkBlock } from '../../plan/street-zones';
import { type WCtx, boxRing, ringGap, rule, f1 } from '../../plan/world-ctx';
import { footOf } from '../civic/foot';
import { BB, BOARD_KINDS, POSTER_VIEW, streetEdgesOf } from '../gen/billboards';
import { JUNCTION } from '../gen/stream';
import { ctxOf as genCtx } from '../gen/common';
import { SIGHT_CLEAR, junctionCorners, plotRing } from '../gen/edges';
import { areaOf, edgeN, pointInRing } from '../gen/geo';
import { adoptedBy, catalogued, kitPosters } from '../gen/kitrefs';
import { inRect4 } from '../gen/street/capstone-rules';
import { zoneOf } from '../gen/street/capstone';
import { landingsOf } from '../gen/street/crossings';

const G = 'Signage';
const SOURCE = 'brief 075-F (Sky: "the old town had billboards / posters well placed"); the old DecoKitDistrictDressing (rooftop boards on low roofs turned to the street, wall boards on blank walls, pylons at the parade vista and the promenade, ad columns at the circus mouths, blade signs 4.6 m on cafés, hotel, cinema)';
const uses = (a: KitAsset | undefined, use: string) => !!a?.tags.use.includes(use);
const front = (yaw: number): V => [-Math.sin(yaw), Math.cos(yaw)];
const ownerOf = (id: string) => id.split('/').slice(0, id.startsWith('tower/') ? 2 : 3).join('/');
const areaOfPlot = (c: WCtx, building: string) => c.plan.plots.find(p => p.id === building.split('/')[1])?.area ?? '';

/** BB-1 faults. */
export function rooftopFaults(c: WCtx): { n: number; bad: string[] } {
  const bad: string[] = [], tip = (c.B.towers.crownTip as number) ?? Infinity, blds = new Map(c.gen.blds.map(b => [b.id, b])), log = c.gen.billboards?.rooftop;
  let n = 0;
  for (const r of c.w.roofs) {
    const a = r.kit && catalogued(r.kit.id);
    if (r.programme === 'sign-frame' && !r.form && !uses(a, 'billboard:rooftop') && !log?.left.some(l => l.roof === r.id) && !log?.cap.some(l => l.roof === r.id)) bad.push(`${r.id}: a sign-frame roof with no board and no reason logged`);
    if (!r.kit || !a || !uses(a, 'billboard:rooftop')) continue;
    n++;
    if (r.programme !== 'sign-frame') bad.push(`${r.id}: ${a.id} on a ${r.programme} roof (only the plan's sign-frame programme carries a board)`);
    const top = r.z + a.size[1] * r.kit.scale[1], cap = c.B.caps?.[areaOfPlot(c, r.building)] as { m?: number; play?: boolean } | undefined;
    if (cap?.play && cap.m !== undefined && top > cap.m + 0.011) bad.push(`${r.id}: ${a.id} tops out at ${f1(top)} m over the play-street cap ${cap.m} (${areaOfPlot(c, r.building)})`);
    if (top > tip + 0.011) bad.push(`${r.id}: ${a.id} tops out at ${f1(top)} m over the crown tip ${tip}`);
    const b = blds.get(r.building), f = front(r.kit.yaw ?? 0);
    const plot = b && c.d.plots.find(p => p.id === b.plot), faces = b ? (streetEdgesOf(b).length ? streetEdgesOf(b) : plot ? streetEdgesOf({ ring: plotRing(genCtx(c.plan, c.d, c.civ, 0.18), plot) }) : []) : []; // a court building: its plot's streets
    if (!faces.some(e => dot(e.n, f) > 0.99)) bad.push(`${r.id}: ${a.id} faces no street its building (or, for a court building, its plot) fronts`);
    const ring = footOf(a, r.kit);
    for (const p of r.parts) {
      if (/\/(leg-0|leg-1|frame)$/.test(p.id)) { bad.push(`${r.id}: the procedural frame ${p.id} stands with the board`); break; }
      if (/\/parapet/.test(p.id)) continue;
      if (ringGap(boxRing(p.c as V, p.size[0], p.size[1], p.yaw), ring) < 1e-6) { bad.push(`${r.id}: ${a.id} stands in ${p.id}`); break; }
    }
  }
  return { n, bad };
}

/** BB-2 faults. */
export function wallFaults(w: World): { n: number; bad: string[] } {
  const bad: string[] = [], walls = w.street.filter(s => s.kind === 'billboard-wall'), masses = new Map(w.buildings.map(m => [m.id, m]));
  for (const s of walls) {
    const a = s.kit && adoptedBy(s.kit.id); if (!a || !s.kit || !uses(a, 'billboard:wall')) { bad.push(`${s.id}: not an adopted billboard:wall piece`); continue; }
    const [mid, ei] = s.edge.split('#'), A = mid ? masses.get(mid) : undefined, i = Number(ei);
    if (!A || !Number.isInteger(i) || i < 0 || i >= A.ring.length) { bad.push(`${s.id}: its wall ${s.edge} is not a wall of the export`); continue; }
    const p = A.ring, ea = p[i]!, eb = p[(i + 1) % p.length]!, n0 = edgeN(ea, eb), n: V = areaOf(p) < 0 ? [-n0[0], -n0[1]] : n0, L = dist(ea, eb), u: V = [(eb[0] - ea[0]) / L, (eb[1] - ea[1]) / L];
    const ring = footOf(a, { ...s.kit, at: s.at }), off = ring.map(q => dot(sub(q, ea), n)), along = ring.map(q => dot(sub(q, ea), u)), top = s.z + a.size[1] * s.kit.scale[1];
    if (Math.min(...off) < -0.011 || Math.min(...off) > 3.1) bad.push(`${s.id}: its back stands ${f1(Math.min(...off))} m off the wall line (0 to 3.1: in front of the wall modules)`);
    if (Math.min(...along) < -0.011 || Math.max(...along) > L + 0.011) bad.push(`${s.id}: it runs past the ends of its wall`);
    if (dot(front(s.kit.yaw ?? 0), n) < 0.99) bad.push(`${s.id}: it does not face out of its wall`);
    if (top > A.base + A.height + 0.011) bad.push(`${s.id}: its top ${f1(top)} m rises over the wall's ${f1(A.base + A.height)} m (a wall board adds no height)`);
    // its foot clears every other building's mass under it, with its roof's parapet, and what stands on those roofs
    for (const m of w.buildings) {
      if (ownerOf(m.id) === ownerOf(A.id) || !samples9(ring).some(q => pointInRing(q, m.ring))) continue;
      if (m.base + m.height + 0.6 > s.z + 0.011) { bad.push(`${s.id}: its foot ${f1(s.z)} m is not clear of ${m.id} (top ${f1(m.base + m.height)} m + parapet 0.6)`); break; }
    }
    for (const r of w.roofs) {
      for (const q of r.parts) if (q.base + q.size[2] > s.z + 0.011 && ringGap(boxRing(q.c as V, q.size[0], q.size[1], q.yaw), ring) < 1e-6) bad.push(`${s.id}: ${q.id} rises into it`);
      for (const k of [r.kit, r.crown]) { const ka = k && catalogued(k.id); if (k && ka && r.z + ka.size[1] * k.scale[1] > s.z + 0.011 && ringGap(footOf(ka, k), ring) < 1e-6) bad.push(`${s.id}: ${k.id} on ${r.id} rises into it`); }
    }
  }
  return { n: walls.length, bad };
}

/** The ground pieces the BB rules judge: pylons and every advertising column, with their kit footprint. */
const groundOf = (w: World) => w.street.filter(s => (s.kind === 'billboard-pylon' || s.kind === 'advertising-column') && s.kit).map(s => { const a = catalogued(s.kit!.id)!; return { s, a, ring: footOf(a, { ...s.kit!, at: s.at }) }; });

/** BB-3 faults. */
export function groundFaults(c: WCtx): { n: number; bad: string[] } {
  const bad: string[] = [], gc = genCtx(c.plan, c.d, c.civ, 0.18), corners = junctionCorners(gc), lands = landingsOf(c.w).map(l => boxRing(l.at, l.size[0], l.size[1], l.yaw)), items = groundOf(c.w);
  const doors = c.w.frontages.filter(f => f.level === 'street').map(f => boxRing(f.entrance.c as V, f.entrance.size[0], f.entrance.size[1], f.entrance.yaw));
  const lv = (c.L.list as { id: string; walk: number }[]), zOf = (id: string) => lv.find(x => x.id === id)?.walk ?? 0;
  const decks = c.w.levels.filter(l => l.height > 1).flatMap(l => l.decks.map(d => ({ id: d.id, z: zOf(l.name) || l.height, ring: d.ring as V[] })));
  const over = c.w.megastructures.flatMap(m => m.parts.filter(p => p.base > 1).map(p => ({ id: p.id, z: p.base, ring: p.ring })));
  for (const { s, a, ring } of items) {
    const pts = samples9(ring), top = s.z + a.size[1] * s.kit!.scale[1], tag = `${s.kind} ${s.id}`;
    const wb = walkBlock(gc, pts, s.edge); if (wb) bad.push(`${tag}: its ${a.id} stands in ${wb}`);
    const tb = tramBlock(gc, pts); if (tb) bad.push(`${tag}: its ${a.id} stands on ${tb}`);
    const qb = quayBlock(gc, pts); if (qb) bad.push(`${tag}: within 1.5 m of ${qb}`);
    const rc = /(\/e\d+|^circus)$/.test(s.edge) ? roadAt(gc, s.at, s.edge) : null; // a kerb-zone item (its centre in the kerb zone of the street its edge faces): SF-5 and SF-6 on the piece
    if (rc && rc.dk >= -0.01 && rc.dk <= rc.sec.kerb) { const ds = pts.map(p => roadAt(gc, p, s.edge)?.dk ?? rc.dk), lo = Math.min(...ds), hi = Math.max(...ds); if (lo < 0.45 - 0.011) bad.push(`${tag}: its ${a.id} ${f1(lo)} m from the kerb face (< 0.45)`); if (hi > rc.sec.kerb + 0.011) bad.push(`${tag}: its ${a.id} reaches ${f1(hi)} m (kerb zone ${rc.sec.kerb})`); }
    if (lands.some(l => ringGap(l, ring) < 2 - 0.011)) bad.push(`${tag}: within 2 m of a crossing landing`);
    if (doors.some(d => ringGap(d, ring) < 2 - 0.011)) bad.push(`${tag}: within 2 m of a doorway`);
    const k = corners.find(q => pts.some(p => dist(p, q) < SIGHT_CLEAR - 0.011)); if (k) bad.push(`${tag}: within ${SIGHT_CLEAR} m of the junction corner at ${k.map(Math.round)} (it blocks sight)`);
    for (const o of [...decks, ...over]) if (o.z < top + 1.5 - 0.011 && ringGap(o.ring, ring) < 0.5) { bad.push(`${tag}: ${o.id} at ${f1(o.z)} m stands over its ${f1(top)} m top`); break; }
  }
  return { n: items.length, bad };
}

/** BB-4 faults. */
export function spacingFaults(c: WCtx): { n: number; bad: string[] } {
  const bad: string[] = [], w = c.w, zone = zoneOf(genCtx(c.plan, c.d, c.civ, 0.18)), by = (k: string) => w.street.filter(s => s.kind === k);
  const pairs = (list: StreetItem[], gap: number, ok: (a: StreetItem, b: StreetItem) => boolean = () => false, what = '') => { for (let i = 0; i < list.length; i++) for (let j = i + 1; j < list.length; j++) if (dist(list[i]!.at, list[j]!.at) < gap - 0.51 && !ok(list[i]!, list[j]!)) bad.push(`${what || list[i]!.kind}: ${list[i]!.id} and ${list[j]!.id} ${f1(dist(list[i]!.at, list[j]!.at))} m apart (< ${gap})`); };
  const cols = by('advertising-column'), pyl = by('billboard-pylon'), wal = by('billboard-wall');
  pairs(cols, BB.columnGap);
  pairs(pyl, 60, (a, b) => a.edge === b.edge && a.edge.startsWith('foot/') && dist(a.at, b.at) <= 20); // a vista pair stands together
  pairs(pyl.filter(s => s.edge.startsWith('quay/')), BB.quayGap, undefined, 'quay pylon');
  pairs(wal, BB.wallGap);
  const owners = new Map<string, string>(); for (const s of wal) { const o = ownerOf(s.edge.split('#')[0]!); if (owners.has(o)) bad.push(`${s.id}: a second wall board on ${o} (${owners.get(o)})`); owners.set(o, s.id); }
  for (const s of [...pyl, ...wal]) if (inRect4(s.at, zone)) bad.push(`${s.id}: in the Handover window (the capstone zone)`);
  for (const r of w.roofs) if (r.kit && uses(catalogued(r.kit.id), 'billboard:rooftop') && inRect4(r.kit.at, zone)) bad.push(`${r.id}: a rooftop board in the Handover window`);
  let signs = 0;
  for (const f of w.frontages) { const k = f.signKit, a = k && catalogued(k.id); if (!a) continue; signs++; if (!a.tags.trade?.includes(f.trade)) bad.push(`${f.id}: ${a.id} on a ${f.trade} (its trades: ${(a.tags.trade ?? []).join(', ')})`); }
  // the ad each board shows (Vela's posters): every placed board and poster piece has its pick, naming the kit's materials; no two boards in view show one ad (a pylon's back face counts)
  const kp = kitPosters<{ adPieces: Record<string, string>; sheetPieces: Record<string, string>; ads: Record<string, { materials: Record<string, string> }>; sheets: { B: { materials: Record<string, string> } } }>();
  let posters = 0;
  if (kp) {
    const picks = new Map((w.posters?.placed ?? []).map(p => [p.host, p])), boards: { host: string; at: V; edge: string; ads: string[] }[] = [];
    const inst = [...w.roofs.flatMap(r => (r.kit ? [{ host: r.id, piece: r.kit.id, at: r.kit.at as V, edge: r.building }] : [])), ...w.street.flatMap(s => (s.kit ? [{ host: s.id, piece: s.kit.id, at: s.at, edge: s.edge }] : [])), ...w.civic.flatMap(m => (m.kit ? [{ host: m.id, piece: m.kit.id, at: m.kit.at, edge: 'civic' }] : []))];
    for (const i of inst) {
      const base = kp.adPieces[i.piece] ?? kp.sheetPieces[i.piece]; if (!base) continue;
      posters++;
      const p = picks.get(i.host); if (!p || p.piece !== i.piece) { bad.push(`${i.host}: ${i.piece} has no poster pick (it would show the canonical cell)`); continue; }
      const want = kp.adPieces[i.piece] ? kp.ads[p.key]?.materials[base] : p.key === 'B' ? kp.sheets.B.materials[base] : p.key === 'A' ? undefined : null;
      if (want === null || (want === undefined ? Object.keys(p.slots).length : p.slots[base] !== want)) bad.push(`${i.host}: poster pick ${p.key} names ${JSON.stringify(p.slots)}, not the kit's material for ${base}`);
      if (kp.adPieces[i.piece]) boards.push({ host: i.host, at: i.at, edge: i.edge, ads: [p.key, ...(p.back ? [p.back] : [])] });
    }
    for (let x = 0; x < boards.length; x++) for (let y = x + 1; y < boards.length; y++) { const A = boards[x]!, B2 = boards[y]!; if ((A.edge === B2.edge || dist(A.at, B2.at) < POSTER_VIEW) && A.ads.some(k => B2.ads.includes(k))) bad.push(`${A.host} and ${B2.host} show the same ad in one view (${A.ads.filter(k => B2.ads.includes(k)).join(', ')})`); }
  }
  return { n: cols.length + pyl.length + wal.length + signs + posters, bad };
}

/** BB-5 faults: every CornerTower_12 (its blade sign reads HOTEL) on a building with a street-level hotel tenancy. */
export function hotelTowerFaults(w: World): { n: number; bad: string[] } {
  const hotels = new Set(w.frontages.filter(f => f.level === 'street' && f.trade === JUNCTION.hotel).map(f => f.host)), towers = w.facades.filter(f => f.kit.id === JUNCTION.tower);
  return { n: towers.length, bad: towers.filter(f => !hotels.has(f.building)).map(f => `${f.id}: ${JUNCTION.tower} (neon HOTEL blade sign) on ${f.building}, which has no street-level ${JUNCTION.hotel} tenancy`) };
}

export function billboardRules(c: WCtx): Check[] {
  const r1 = rooftopFaults(c), r2 = wallFaults(c.w), r3 = groundFaults(c), r4 = spacingFaults(c), r5 = hotelTowerFaults(c.w);
  void BOARD_KINDS;
  return [
    rule('BB-1', G, 'A rooftop board stands on a sign-frame roof in place of its frame, faces a street its building fronts, keeps clear of the plant, and never lifts a play-street roof past its cap or any roof past the crown tip; a sign-frame roof without one is logged with why', r1.n, r1.bad, 'none', `${SOURCE} · standards.buildings.roofs.kit.programmes.sign-frame · standards.buildings.caps (brief 066) · failing example: the board on bld/boulevard-west/3 (records, roof 20.58 m) tops out at 29.75 m over the 20.4 m cap`),
    rule('BB-2', G, 'A wall board hangs in front of a wall of a building taller than its top, facing out, its foot clear of the lower neighbour\'s roof, parapet and everything on that roof', r2.n, r2.bad, 'none', `${SOURCE} · failing example: a wall board lifted past the top of its wall`),
    rule('BB-3', G, `A pylon or advertising column, on its kit piece's own footprint, stands out of the clear walking zone, the tram's space and 1.5 m of the quay edge, in the kerb zone 0.45 m off the kerb face (a kerb-zone item), 2 m off every crossing landing and doorway, ${SIGHT_CLEAR} m off every junction corner, and under nothing lower than its top + 1.5 m`, r3.n, r3.bad, 'none', `${SOURCE} · streetscape SF-1, SF-2, SF-3, SF-8, SF-14 and keepClear.underDeckOrBridge, on the piece rather than the plan's catalogue box · failing example: the Parade's vista pylon moved into the mall's walk band`),
    rule('BB-4', G, `Advertising columns ${BB.columnGap} m apart, pylons 60 m apart but for a vista pair (quay pylons ${BB.quayGap}), wall boards ${BB.wallGap} m apart and one per building; no board in the Handover window; a blade or wall sign only on a trade its piece names; every board and poster piece has its poster pick (kit.json posters) and no two boards in one view show the same ad`, r4.n, r4.bad, 'none', `${SOURCE} · streetscape.catalogue.advertising-column.count · kit.json tags.trade · failing example: a BladeSign_Hotel on a diner`),
    rule('BB-5', G, `${JUNCTION.tower} (its blade sign reads HOTEL in neon) stands only on a building with a street-level ${JUNCTION.hotel} tenancy; elsewhere the junction takes ${JUNCTION.deco} or the class's StreamCorner`, r5.n, r5.bad, 'none', `brief 075-F1b (Aster); briefs/072/pieces/pieces.md 075-E (Vela: the tower's sign reads HOTEL); tools/decokit/facades_tower.py · failing example: ${JUNCTION.tower} on bld/docks-boulevard/2 (no hotel), as 075-E placed it`),
  ];
}
