// SF-24 (brief 061, Sky 2026-10-25: lane mouths at the gates cross on a continuous footway): every gate's lane mouth is the plan's standard (streetscape.continuousFootway) in the export. The pavement runs on across the mouth at kerb height:
// ONE table (a flat at the pavement top, in the plots' paving) between the two plot corners, a ramp at its back edge (the lane's carriageway rising from 0 to the table's level, 1 in 13), a pearl lip at its front edge, two pearl threshold bands
// and the lane's two posts at its back corners; the mouth's two corners are square (no kerb return, no upstand kerb across), the higher road's light ribbon runs straight on along the table's front edge, the lane's stops short of the ramp's foot band,
// and nothing else stands in the mouth: no crossing, landing, crossing part or furniture. A mouth whose corners still carry kerb returns has no footway (the returns and the table cannot stand on one ground); SF-24 says so as a stat, never silently.
// Failing examples: footway.test.ts.
import type { Footway, StreetItem } from '../../../contracts/world/world.schema';
import type { Check } from '../../plan/checks';
import { type V, dist, inPoly } from '../../plan/geom';
import { TOL, type WCtx, itemRing, rule, stat } from '../../plan/world-ctx';
import { ctxOf as genCtx } from '../gen/common';
import { returnsOf } from '../gen/kerbreturn';
import { overlaps } from '../gen/street/crossings';
import { activeMouths, blockRing, footwayOf, footwayStd, mouthsOf } from '../gen/street/footway';

const SRC = 'streetscape.continuousFootway (Datum 2026-10-25; table z 0.18, ramp 1 in 13 over 2.4 m, lip 0.38 m, two bands, posts) · brief 061 (Sky 2026-10-25) · Vela\'s sketch briefs/061/footway-sketch.png · Buchanan, entrance to an environmental area';
const same = (a: V[], b: V[]) => a.length === b.length && a.every((p, i) => dist(p, b[i]!) <= TOL * 2);
/** A ring pulled in on all sides by `d`, about its centre (a box). */
const shrink = (r: V[], d: number): V[] => { const c: V = [r.reduce((t, p) => t + p[0], 0) / r.length, r.reduce((t, p) => t + p[1], 0) / r.length]; return r.map(p => { const l = dist(p, c) || 1, k = Math.max(0, l - d) / l; return [c[0] + (p[0] - c[0]) * k, c[1] + (p[1] - c[1]) * k] as V; }); };

export function footwayRules(c: WCtx): Check[] {
  const std = footwayStd(c.S); if (!std) return [];
  const gc = genCtx(c.plan, c.d, c.civ, 0.18), mouths = mouthsOf(gc), have = new Map((c.w.ground.footways ?? []).map(f => [f.id, f])), active = new Set(activeMouths(gc).map(m => m.id));
  const bad: string[] = [], flats = new Map(c.w.ground.flats.map(f => [f.id, f])), pieces = c.w.ground.pieces ?? [], street = c.w.street, ids = new Set(mouths.map(m => m.id));
  for (const f of have.values()) if (!ids.has(f.id)) bad.push(`${f.id} is a footway no gate of the plan makes`);
  for (const m of mouths) {
    const f = have.get(m.id);
    if (!f) { if (active.has(m.id)) bad.push(`${m.id}: the gate ${m.gate} has no footway, and no kerb return stands at its corners`); continue; }
    const want = footwayOf(m, std, f.table.surface, f.lip.surface), at = (what: string) => `${m.id}: ${what}`;
    if (!active.has(m.id)) bad.push(at('has a footway and a kerb return still stands at a corner of its mouth'));
    if (f.level !== std.table.z) bad.push(at(`its table is at ${f.level}, the plan's is ${std.table.z}`));
    for (const k of ['table', 'ramp', 'lip'] as const) if (!same(f[k].ring, want[k].ring)) bad.push(at(`its ${k} ring is not the mouth's (${m.half * 2} m wide, ${k === 'table' ? m.tangent : k === 'ramp' ? std.ramp.run : std.lip.d} m deep)`));
    if (f.bands.length !== std.bands.count || f.bands.some((b, i) => !same(b.ring, want.bands[i]!.ring) || b.level !== want.bands[i]!.level)) bad.push(at(`its threshold bands are not the plan's ${std.bands.count} of ${std.bands.w} m (one at the ramp's foot, one behind the lip)`));
    if (f.ramp.rise !== std.ramp.rise || f.ramp.run !== std.ramp.run || Math.abs(f.ramp.run / f.ramp.rise - 13.33) > 0.1) bad.push(at(`its ramp rises ${f.ramp.rise} m over ${f.ramp.run} m: 1 in ${(f.ramp.run / f.ramp.rise).toFixed(1)}, the plan's is 1 in 13`));
    if (f.lip.d !== std.lip.d || f.lip.drop !== std.lip.drop) bad.push(at(`its lip is ${f.lip.d} m falling ${f.lip.drop} m, the plan's ${std.lip.d} / ${std.lip.drop}`));
    const table = flats.get(f.table.id);
    if (!table) bad.push(at(`its table is not a flat in ground.flats (${f.table.id}): the walk floor and the audits would read the carriageway`));
    else if (!same(table.ring, f.table.ring) || table.level !== f.level) bad.push(at('its table flat is not the footway\'s table'));
    // the mouth's corners are square: no kerb return, and no upstand kerb stone across the table
    if (returnsOf(gc).some(r => r.convex && !r.nose && r.roads.includes(m.road) && m.corners.some(p => dist(p, r.at) < 0.3))) bad.push(at('a kerb return still rounds a corner of its mouth'));
    const tab = f.table.ring, ring = blockRing(m, std);
    for (const s of pieces) if ((s.kind === 'kerb' || s.kind === 'kerb-return') && overlaps(shrink(itemRing(s), 0.1), tab)) bad.push(at(`${s.id} (${s.kind}) lies across the table: the upstand kerb is not laid across a footway mouth`));
    // the higher road's ribbon runs on along the table's front edge, whole; the lane's stops short of the ramp: no light, lozenge or seam of any road inside the table, ramp and bands
    const face = pieces.filter(s => s.kind === 'road-ribbon' && s.edge === `roadfinish/${m.id}/kerb`), run = face.reduce((t, s) => t + s.size[0], 0), len = m.half * 2;
    if (run < len - 0.25) bad.push(at(`the higher road's ribbon runs ${run.toFixed(1)} m of the ${len} m across its mouth (it runs straight on)`));
    for (const s of pieces) if (/^road-(ribbon|lozenge|seam|shoulder|pinstripe|lane-dash|edge-line)$/.test(s.kind) && s.edge !== `roadfinish/${m.id}/kerb` && inPoly(s.at as V, ring) && !(s.kind === 'road-ribbon' && s.edge.startsWith(`roadfinish/${m.road}`))) bad.push(at(`${s.id} (${s.kind}) lies on the table or the ramp: the lane's road finish stops short of the foot band`));
    // nothing stands in the mouth: no crossing, landing or part, no furniture (the posts stand at its back corners, outside)
    for (const x of c.w.crossings ?? []) {
      if (overlaps(x.strip as V[], ring)) bad.push(at(`${x.id}'s strip lies in the mouth: a gate's mouth is marked by its gate, not by a crossing`));
      for (const l of x.landings) if (overlaps(itemRing(l), ring)) bad.push(at(`${l.id} (a landing of ${x.id}) stands in the mouth`));
    }
    const posts = new Set([`street/${m.id}/post-a`, `street/${m.id}/post-b`]);
    for (const s of street) if (s.z < 1 && !posts.has(s.id) && !s.kind.startsWith('crossing-') && s.kind !== 'signal-orb' && overlaps(itemRing(s), shrink(tab, 0.05))) bad.push(at(`${s.id} (${s.kind}) stands on the table`));
    for (const [i, id] of [...posts].entries()) {
      const p = street.find(s => s.id === id) as StreetItem | undefined;
      if (!p) bad.push(at(`its post ${id} is missing`)); else if (p.kind !== 'bollard' || dist(p.at as V, f.posts[i]!) > TOL * 2) bad.push(at(`${id} is not a bollard at the table's back corner (${f.posts[i]!.join(', ')})`));
    }
  }
  const n = (have.size);
  return [
    rule('SF-24', 'Streetscape', 'A gate\'s lane mouth is a continuous footway: one table at the pavement top between the plot corners, a ramp at its back edge and a pearl lip at its front, two threshold bands and the lane\'s two posts; no kerb return or upstand kerb across it, the higher road\'s ribbon runs straight on, the lane\'s stops short of the ramp, and no crossing, landing or furniture in the mouth',
      mouths.length, bad, 'none', SRC),
    stat('SF-24/n', 'Streetscape', 'Continuous footways (informational): gates, footways laid', `${n} of ${mouths.length}`, 'all', true, SRC,
      n === mouths.length ? 'every gate\'s mouth is a footway' : `${mouths.length - n} mouth(s) keep their kerb returns: ${mouths.filter(m => !have.has(m.id)).map(m => m.gate).join(', ')} (gen/kerbreturn.ts must leave a footway corner square: street/footway.ts isFootwayCorner)`),
  ];
}
void ({} as Footway);
