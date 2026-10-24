// KR-2 and KR-3 on the export (brief 028, Sky 2026-10-24: rounded pavement corners; and "some odd curb / road layout going on here": kerb stones lying across a sunk bay's mouth).
// KR-2 the kerb returns hold in the world: each convex return has its carriageway fill (a flat `ground/return/<id>` at the carriageway's level, in its road's finish) and its plot slab is cut by the arc (the old corner is outside the slab);
//      nothing stands in the carriageway a return takes: no street item (kept the kerb's 0.45 m off the arc, SF-5), crossing landing or strip, flat ground piece or inlay lies in it; and the kerb, the light ribbon and the pinstripe
//      follow the arc: kerb stones (kind kerb-return) stand on every arc (the arc is covered to within a stone, less what crossings and drives drop), no `kerb` stone lies across one.
// KR-3 a kerb stands only where pavement meets carriageway: no kerb stone, no kerb-return stone, lies across the mouth of a kerbed bay (a lay-by is open to the road); the bay outline's stones lie on its end, back and arcs, 0.15 m inside the pavement edge.
// KR-4 a kerb stone (kind kerb or kerb-return) lies on a pavement edge: within 0.5 m of the ring of a plot slab, a bend's pad or a median (its centre stands 0.15 m inside the kerb face); never out on the carriageway. The circus mouths' stones stand on the circus band, which has no slab, and the sheet-edge corners' on a beyond block (a flat under the road's own edge, no slab).
// Failing examples: kerbreturn.test.ts.
import type { StreetItem } from '../../../contracts/world/world.schema';
import type { Check } from '../../plan/checks';
import { type V, dist, inPoly, segDist } from '../../plan/geom';
import { type WCtx, itemRing, rule, stat } from '../../plan/world-ctx';
import { ctxOf as genCtx } from '../gen/common';
import { cutHits, returnsOf, slabRing } from '../gen/kerbreturn';
import { overlaps } from '../gen/street/crossings';

const G = 'Streetscape';
const SRC = 'standards.streetscape.kerbReturn (Datum 2026-10-24; kerb, ribbon, pinstripe and walk follow the arc) · brief 028 (Sky 2026-10-24) · tools/world/gen/kerbreturn.ts, street/kerbarcs.ts';
/** Kinds that lie in or on the carriageway or kerb by design: the road's finish and the arc's own kerb. */
const OWN = new Set(['road-ribbon', 'road-shoulder', 'road-pinstripe', 'road-lozenge', 'road-seam', 'road-lane-dash', 'road-edge-line', 'kerb-return', 'drain', 'manhole']);
const CLEAR = 0.45 - 0.011; // SF-5: a kerb-zone item stands 0.45 m off the kerb face (the export rounds to a mm)

export function kerbReturnRules(c: WCtx): Check[] {
  const gc = genCtx(c.plan, c.d, c.civ, 0.18), list = returnsOf(gc).filter(r => r.convex && !r.nose);
  if (!list.length) return [];
  const bad2: string[] = [], bad3: string[] = [];
  const fills = new Map(c.w.ground.flats.filter(f => f.id.startsWith('ground/return/')).map(f => [f.id, f]));
  const slabs = new Map(c.w.ground.plots.map(p => [p.id.replace(/^plot\//, ''), p.ring as V[]]));
  for (const r of list) {
    const f = fills.get(`ground/return/${r.id}`);
    if (!f) { bad2.push(`${r.id}: no carriageway fill under the corner the arc takes off`); continue; }
    if (Math.abs(f.level - (r.level ? 0.02 : 0)) > 0.001) bad2.push(`${r.id}: its fill lies at ${f.level}, the carriageway's level there is ${r.level ? 0.02 : 0}`);
    const slab = slabs.get(r.plot); // the slab is cut: the old corner is outside it (a plot's own return only: a bay tip's corner is in the slab's notch)
    if (slab && !r.bay && !r.mouth && inPoly(r.at, slab) && dist(r.at, r.a) > 0.5) bad2.push(`${r.id}: the plot slab still covers the old corner at ${r.at}`);
  }
  // nothing stands in the carriageway a return takes
  const items: StreetItem[] = [...c.w.street.filter(s => s.z < 1 && !s.kind.startsWith('crossing') && s.kind !== 'signal-orb' && !s.kind.startsWith('stop') && !s.edge.startsWith('stop/')), ...(c.w.ground.pieces ?? []).filter(s => !OWN.has(s.kind))];
  for (const s of items) { const flat = (c.w.ground.pieces ?? []).includes(s), r = cutHits(gc, s.kind === 'kerb' ? shrink(itemRing(s), 0.08) : itemRing(s), flat ? 0 : CLEAR); if (r) bad2.push(`${s.id} (${s.kind}) stands in or within 0.45 m of the kerb return ${r.id}`); } // a straight kerb stone ends on the tangent point: only one that runs on over the arc fails
  for (const x of c.w.crossings ?? []) for (const [what, ring] of [['strip', x.strip as V[]], ...x.landings.map((l, i) => [`landing ${i}`, itemRing(l)] as [string, V[]])] as [string, V[]][]) { const r = cutHits(gc, ring, 0); if (r) bad2.push(`${x.id} ${what} lies in the carriageway of ${r.id}`); }
  for (const s of c.w.street) if (s.kind.startsWith('crossing-') && s.kind !== 'crossing-arc') { const r = cutHits(gc, itemRing(s), 0); if (r) bad2.push(`${s.id} (${s.kind}) lies in the carriageway of ${r.id}`); }
  // KR-2: the arc carries a kerb stone all the way round (what crossings and drives drop aside)
  const stones = (c.w.ground.pieces ?? []).filter(s => s.kind === 'kerb-return');
  const cutters: V[][] = [...(c.w.crossings ?? []).map(x => x.strip as V[]), ...(c.w.ground.drives ?? []).map(x => x.ring as V[])];
  let arcM = 0, stoneM = 0;
  for (const r of returnsOf(gc).filter(x => !x.nose)) {
    const len = r.r * r.theta; arcM += len;
    const mine = stones.filter(s => s.edge === `kerbreturn/${r.id}`); const m = mine.reduce((t, s) => t + s.size[0], 0); stoneM += m;
    // to within a stone, less what crossings and drives drop: an arc with no strip or drive beside it (within a metre of its stones) may miss one stone's length; one that has may drop what they cover (no more than half)
    const one = Math.max(0.9, ...mine.map(x => x.size[0])), beside = r.arc.some(q => cutters.some(ring => inPoly(q, ring) || ring.some((p, i) => segDist(q, p, ring[(i + 1) % ring.length]!) < 1)));
    if (m < (beside ? len * 0.5 : len - one)) bad2.push(`${r.id}: ${m.toFixed(1)} m of kerb stone on an arc of ${len.toFixed(1)} m${beside ? ' (beside a crossing or a drive)' : ''}`);
  }
  // KR-3: no kerb across a lay-by's mouth, and the lay-by's own stones lie on its outline
  const bays = c.w.ground.flats.filter(f => f.id.startsWith('civic/bay/') && c.gen.roadFinish && (c.S.roads?.kerbs?.bays?.kerbed ?? []).some((k: { bay: string }) => `civic/bay/${k.bay}` === f.id));
  const kerbs = [...(c.w.ground.pieces ?? []).filter(s => s.kind === 'kerb' || s.kind === 'kerb-return')];
  for (const b of bays) {
    const ring = b.ring as V[], mid = ring.reduce((t, p) => [t[0] + p[0] / ring.length, t[1] + p[1] / ring.length] as V, [0, 0] as V);
    for (const s of kerbs) {
      const r = itemRing(s), cx = r.reduce((t, p) => [t[0] + p[0] / r.length, t[1] + p[1] / r.length] as V, [0, 0] as V);
      if (dist(cx, mid) < 12 && overlaps(shrink(r, 0.06), ring) && !s.edge.startsWith('kerbreturn/layby/')) bad3.push(`${s.id} (${s.kind}) lies across the mouth of ${b.id}: a lay-by is open to the road`);
    }
  }
  // KR-4
  const rings = [...c.w.ground.plots.map(p => p.ring as V[]), ...c.w.ground.medians.map(m => m.ring as V[])], bad4: string[] = [];
  const near = (p: V) => rings.some(r => { const xs = r.map(q => q[0]), ys = r.map(q => q[1]); return p[0] >= Math.min(...xs) - 0.5 && p[0] <= Math.max(...xs) + 0.5 && p[1] >= Math.min(...ys) - 0.5 && p[1] <= Math.max(...ys) + 0.5 && r.some((q, i) => segDist(p, q, r[(i + 1) % r.length]!) <= 0.5); });
  for (const s of kerbs) if (!/^kerbreturn\/(circus-mouth|edge)\//.test(s.edge) && !near(s.at as V)) bad4.push(`${s.id} (${s.kind}) lies at ${s.at.map(x => x.toFixed(1))}, on no pavement edge`);
  // KR-5 (Glyph 0.2.113 nit 1): a reservation's kerb face or lawn-end arc lies along a lawn flat of the reservation (a stray bar or arc in the road is a face with no lawn beside it)
  const lawns = c.w.ground.flats.filter(f => f.id.startsWith('reservation/')).map(f => f.ring as V[]), bad5: string[] = [];
  const onLawn = (q: V) => lawns.some(r => inPoly(q, r) || r.some((p, i) => segDist(q, p, r[(i + 1) % r.length]!) <= 0.6));
  const faces = (c.w.ground.pieces ?? []).filter(s => /^roadfinish\/(lawn\/)?[^/]+\/reservation\//.test(s.edge));
  for (const s of faces) if (!onLawn(s.at as V)) bad5.push(`${s.id} (${s.kind}) lies at ${s.at.map(x => x.toFixed(1))}, beside no lawn track: a lone kerb piece in the road`);
  void slabRing; void stat;
  return [
    rule('KR-2', G, 'Kerb returns: each convex return has its carriageway fill and a cut slab; nothing stands in the carriageway it takes or within the kerb\'s 0.45 m of its arc; the arc carries its kerb stones', list.length, bad2, 'none', SRC),
    rule('KR-4', G, 'A kerb stone lies on a pavement edge (a plot slab, a bend pad or a median), never out on the carriageway', kerbs.length, bad4, 'none', SRC),
    rule('KR-5', G, 'A reservation\'s kerb piece lies beside its lawn track: no bar or arc of kerb stands alone in the road', faces.length, bad5, 'none', SRC),
    rule('KR-3', G, 'A kerb stands only where pavement meets carriageway: no kerb stone lies across the mouth of a kerbed bay', bays.length, bad3, 'none', SRC),
    stat('KR-2/n', G, 'Kerb returns (informational): returns, arc metres, kerb stone metres on them', `${returnsOf(gc).length} returns`, '-', true, SRC, `convex ${list.length} (junction corners, lay-by mouths, circus mouths), concave ${returnsOf(gc).filter(x => !x.convex && !x.bend).length} (a lane end, lay-by backs), bend outsides ${returnsOf(gc).filter(x => x.bend).length}, median noses ${returnsOf(gc).filter(x => x.nose).length}; ${arcM.toFixed(0)} m of arc, ${stoneM.toFixed(0)} m of stone; ${c.gen.roadFinish?.returns.chords ?? 0} ribbon and pinstripe chords`),
  ];
}
/** A ring pulled in on all sides by about `d` m (a box: about its centre), so a stone that only touches a bay's edge is not "across" it. */
function shrink(r: V[], d: number): V[] { const c: V = [r.reduce((t, p) => t + p[0], 0) / r.length, r.reduce((t, p) => t + p[1], 0) / r.length]; return r.map(p => { const l = dist(p, c) || 1, k = Math.max(0, l - d) / l; return [c[0] + (p[0] - c[0]) * k, c[1] + (p[1] - c[1]) * k] as V; }); }
