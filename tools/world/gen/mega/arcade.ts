// Stacked arcade (Parade Galleries): each range is cut into three strips from the street face inward (plan `strips`): a 5 m gallery
// (deck level on a solid ground storey, shopfronts under a chrome canopy), an 8 m sky promenade (concourse level) and the tall
// body. Tower podiums of the megastructure's gate towers are cut out of the ranges. The `colonnade` edges lift the upper storeys
// over the pavement; cores stand at `cores.at`, then every `cores.every` m, and wherever maxTravel needs one more.
import type { Megastructure } from '../../../plan/plan';
import type { V } from '../../../plan/geom';
import { lineDist } from '../../../plan/geom';
import type { Ctx } from '../common';
import { type Rect, bbox, growRect, rectPts, rectsTouch, subtractRect } from '../geo';
import type { Site } from '../masses';
import { fillTravel } from '../travel';
import type { Core, Host, MegaOut } from '../types';
import { columnsOf, lifted, stripsOf } from './colonnade';
import { INSET, box, deckRec, faceOf, faceSide, host, rectMass, servedBy, sideEdge, slice, zOf } from './kit';

type Cores = { at: number[]; every: number; from: string; to: string };
type Canopy = { depth: number; surface: string; edges: { footprint: number; edge: 'x0' | 'x1' | 'y0' | 'y1' }[] };
type Range = { n: string; r0: Rect; axis: 0 | 1; dir: 1 | -1; lo: number; hi: number };

export function arcade(ctx: Ctx, m: Megastructure, sites: Site[]): MegaOut {
  const L = ctx.L, zd = zOf(ctx, 'deck'), zc = zOf(ctx, 'concourse'), td = L.structureDepth.deck, tb = L.structureDepth.bridge;
  const { gallery: gal, promenade: con } = (m as unknown as { strips: { gallery: number; promenade: number } }).strips, top = ctx.pav + m.height, id = `mega/${m.id}`;
  const co = (m as unknown as { cores: Cores }).cores, canopy = (m as unknown as { canopy: Canopy }).canopy, CORE = L.cores.arcade as [number, number];
  const out: MegaOut = { id, parts: [], prims: [], decks: [], hosts: [], cores: [], connectors: [], avoid: [] };
  const podiums = sites.filter(s => (m.towers as string[] | undefined)?.includes(s.id)).map(s => s.podium);
  const mixOf = (lv: string) => m.levels.find(l => l.level === lv)?.mix;
  const street: Host = host(`${id}/street`, 'street', ctx.pav, m.areas[0]!, mixOf('street'), [], 4);
  const deckH: Host = host(`${id}/deck`, 'deck', zd, m.areas[0]!, mixOf('deck'), [], 0);
  const concH: Host = host(`${id}/concourse`, 'concourse', zc, m.areas[0]!, mixOf('concourse'), [], 0);
  const faces: { f: Rect; axis: 0 | 1; dir: 1 | -1 }[] = [], ranges: Range[] = [];
  const coreAt = (r0: Rect, axis: 0 | 1, dir: 1 | -1, v: number): Rect => {
    const rc = slice(r0, axis, dir, gal + con + 0.3, gal + con + 0.3 + CORE[1]);
    return axis === 1 ? [v - CORE[0] / 2, v + CORE[0] / 2, rc[2], rc[3]] : [rc[0], rc[1], v - CORE[0] / 2, v + CORE[0] / 2];
  };
  m.footprint.forEach((f, i) => {
    const ce = canopy.edges.find(e => e.footprint === i), fromEdge = ce ? { axis: (ce.edge[0] === 'y' ? 1 : 0) as 0 | 1, dir: (ce.edge[1] === '0' ? 1 : -1) as 1 | -1 } : null;
    const { axis, dir } = fromEdge ?? faceOf(ctx, f as Rect); faces.push({ f: f as Rect, axis, dir });
    const strips = stripsOf(ctx, m, i);
    let pieces: Rect[] = [f as Rect];
    for (const p of podiums) pieces = pieces.flatMap(r => subtractRect({ p: rectPts(r), c: ['x', 'x', 'x', 'x'] }, p).map(q => bbox(q.p)));
    const foot = ctx.plan.foot.reduce((b, o) => (lineDist([(f[0] + f[1]) / 2, (f[2] + f[3]) / 2], o.pts) < lineDist([(f[0] + f[1]) / 2, (f[2] + f[3]) / 2], b.pts) ? o : b));
    pieces.forEach((r, k) => {
      const r0 = growRect(r, -INSET), n = `${id}/range-${i}-${k}`, depth = axis === 1 ? r0[3] - r0[2] : r0[1] - r0[0];
      const under = strips.some(s => r0[0] >= s.r[0] - 1e-6 && r0[1] <= s.r[1] + 1e-6 && r0[2] >= s.r[2] - 1e-6 && r0[3] <= s.r[3] + 1e-6);
      if (under || depth < gal + con + 4) { out.parts.push(...lifted(`${n}/body`, 'mega', r0, 0, top, strips)); return; } // wholly over the pavement: upper storeys only
      const A = slice(r0, axis, dir, 0, gal), B = slice(r0, axis, dir, gal, gal + con), C = slice(r0, axis, dir, gal + con, depth);
      out.parts.push(...lifted(`${n}/a`, 'mega', A, 0, zd - td, strips), ...lifted(`${n}/b`, 'mega', B, 0, zc - td, strips), ...lifted(`${n}/c`, 'mega', C, 0, top, strips));
      out.decks.push(deckRec(ctx, id, `${n}/gallery`, 'deck', A, [], td), deckRec(ctx, id, `${n}/promenade`, 'concourse', B, [], td));
      const fs = faceSide(axis, dir);
      street.edges.push(sideEdge(r0, fs, foot.kind === 'mall' ? 'mall' : 'walk', foot.id));
      deckH.edges.push(sideEdge(B, fs, 'deck', 'gallery')); concH.edges.push(sideEdge(C, fs, 'concourse', 'promenade'));
      if (ce) { // the continuous chrome canopy over the face's frontage zone (outside the walk)
        const len = axis === 1 ? r0[1] - r0[0] : r0[3] - r0[2], c: V = axis === 1 ? [(r0[0] + r0[1]) / 2, dir > 0 ? r0[2] - canopy.depth / 2 : r0[3] + canopy.depth / 2] : [dir > 0 ? r0[0] - canopy.depth / 2 : r0[1] + canopy.depth / 2, (r0[2] + r0[3]) / 2];
        out.prims.push(box(`${n}/canopy`, canopy.surface, c, axis === 1 ? [len, canopy.depth, 0.3] : [canopy.depth, len, 0.3], ctx.B.groundCeiling, 0));
      }
      // cores in the tall strip: from the listed positions, then every `every` m along the range
      const lo = axis === 1 ? r0[0] : r0[2], hi = axis === 1 ? r0[1] : r0[3], inRange = (v: number) => v >= lo + 6 && v <= hi - 6, pos: number[] = [];
      for (let v = co.at[0]! - Math.floor((co.at[0]! - lo) / co.every) * co.every; v <= hi; v += co.every) if (inRange(v)) pos.push(v);
      for (const v of co.at) if (inRange(v) && pos.every(q => Math.abs(q - v) > co.every / 2)) pos.push(v);
      pos.forEach((v, j) => out.cores.push({ id: `${n}/core-${j}`, host: id, rect: coreAt(r0, axis, dir, v), from: co.from, to: co.to, lift: true, stair: true }));
      ranges.push({ n, r0, axis, dir, lo, hi });
    });
  });
  // bridges: concourse bridges reach on over the gallery to the promenade of the footprint they join; every bridge is a deck
  for (const b of m.bridges ?? []) {
    const r = [...b.rect] as Rect, jf = (b as { joinsFootprint?: number }).joinsFootprint, fc = jf === undefined ? undefined : faces[jf];
    if (fc && b.level === 'concourse' && rectsTouch(r, fc.f, 0.5)) { const i = fc.axis === 1 ? (fc.dir > 0 ? 3 : 2) : fc.dir > 0 ? 1 : 0; r[i] += (fc.dir > 0 ? 1 : -1) * gal; }
    out.decks.push(deckRec(ctx, id, `${id}/bridge-${b.id}`, b.level, r, [], tb));
  }
  // gate towers carry a core each (their podium is solid at deck level): lifts from the street to the concourse
  podiums.forEach((p, j) => {
    const fc = faces.find(x => rectsTouch(x.f, p, 0.5)) ?? faces[0]!, c = slice(p, fc.axis, fc.dir, gal + 0.5, gal + 0.5 + CORE[1]);
    const core: Rect = fc.axis === 1 ? [p[0] + 0.5, p[0] + 0.5 + CORE[0], c[2], c[3]] : [c[0], c[1], p[2] + 0.5, p[2] + 0.5 + CORE[0]];
    out.cores.push({ id: `${id}/gate-core-${j}`, host: id, rect: core, from: co.from, to: co.to, lift: true, stair: true });
  });
  // maxTravel: where a deck point is further than standards.levels.spacing.deck.maxTravel from a way down, one more core in a tall strip
  const pieces = out.decks.filter(d => d.level === 'deck').map(d => ({ id: d.id, level: d.level, rect: d.rect })), limit = L.spacing.deck.maxTravel as number;
  const wayOf = (c: Core) => ({ rect: c.rect, serves: servedBy(c.rect, pieces) });
  const free = (r: Rect, taken: Rect[]) => ![...out.cores.map(c => c.rect), ...taken].some(o => rectsTouch(growRect(r, 0.5), o, 0));
  const named = new Map<string, string>();
  const fill = fillTravel({
    pieces, ways: out.cores.map(wayOf), obstacles: [], limit, serves: r => servedBy(r, pieces),
    candidates: (w, taken) => {
      const cs: { r: Rect; d: number }[] = [];
      for (const g of ranges) for (let v = g.lo + 6; v <= g.hi - 6; v += 1) { const r = coreAt(g.r0, g.axis, g.dir, v); if (free(r, taken)) { cs.push({ r, d: Math.hypot((r[0] + r[1]) / 2 - w[0], (r[2] + r[3]) / 2 - w[1]) }); named.set(r.join(), g.n); } }
      return cs.sort((a, b) => a.d - b.d).map(c => c.r);
    },
  });
  fill.added.forEach((r, j) => out.cores.push({ id: `${named.get(r.join())}/core-x${j}`, host: id, rect: r, from: co.from, to: co.to, lift: true, stair: true }));
  const cols = columnsOf(ctx, m, id);
  out.prims.push(...cols.prims); out.columns = cols.pts;
  out.hosts.push(street, deckH, concH);
  return out;
}
