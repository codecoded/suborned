// Harbour deck (Market Deck): cargo street at ground level (bonded sheds and repair bays in a perimeter band on each plot's building
// line, 12 m column grid under the deck), the open market on the deck (barrel-vaulted halls and 8 m aisles), and the crane walk on
// the quay. The deck overhangs every street edge as a colonnade. Cores stand every `cores.every` m along the street edges, then
// wherever maxTravel needs one more; halls keep clear of them.
import type { Megastructure } from '../../../plan/plan';
import { type V, lineDist, nearLine } from '../../../plan/geom';
import type { Connector } from '../../../../contracts/world/world.schema';
import { type Ctx, rng, rv } from '../common';
import { pavementOf, plotRing } from '../edges';
import { type Rect, bbox, growRect, inRect, pointInRing, rectCentre, rectPts, rectsTouch } from '../geo';
import { CRANE } from '../surfaces';
import { fillTravel, reach } from '../travel';
import type { Core, Host, MegaOut } from '../types';
import { columnsOf, stripsOf } from './colonnade';
import { INSET, box, deckRec, host, prim, rectMass, servedBy, sideEdge, zOf } from './kit';

type Fixed = { kind: string; id: string; rect: Rect; foot?: 'x0' | 'x1' | 'y0' | 'y1' };
const overlaps = (a: Rect, list: Rect[], m = 0) => list.some(b => rectsTouch(a, b, -m));
const OPP = { x0: 'x1', x1: 'x0', y0: 'y1', y1: 'y0' } as const;
/** Midpoint of one side of a rect. */
const sideMid = (r: Rect, s: 'x0' | 'x1' | 'y0' | 'y1'): V => (s === 'x0' ? [r[0], (r[2] + r[3]) / 2] : s === 'x1' ? [r[1], (r[2] + r[3]) / 2] : s === 'y0' ? [(r[0] + r[1]) / 2, r[2]] : [(r[0] + r[1]) / 2, r[3]]);

/** Runs along [lo, hi] cut into lengths that are multiples of `g` (runBays[0] to runBays[1] bays; the last run takes the remainder if it would be shorter than the minimum). */
function runs(lo: number, hi: number, g: number, bays: [number, number], r: ReturnType<typeof rng>): [number, number][] {
  const out: [number, number][] = []; let at = lo;
  while (hi - at > 0.01) {
    let w = g * r.int(bays[0], bays[1]); if (hi - at - w < bays[0] * g) w = hi - at;
    out.push([at, Math.min(hi, at + w)]); at += w;
  }
  return out;
}

export function harbourDeck(ctx: Ctx, m: Megastructure): MegaOut {
  const L = ctx.L, zd = zOf(ctx, 'deck'), td = L.structureDepth.deck, tb = L.structureDepth.bridge, id = `mega/${m.id}`, r = rng(id);
  const x = m as unknown as {
    quayWalk: Rect; connectors: Fixed[]; craneMasts: number; cargoBand: { depth: number; runBays: [number, number] };
    halls: { w: number[]; len: number[]; lenStep: number; aisle: number; inset: number; rise: string }; crossAisle: { x: [number, number]; w: number };
    cranes: { every: number; first: number; lastClear: number };
  };
  if (x.halls.rise !== 'w / 6') throw new Error(`megastructure ${m.id}: halls.rise "${x.halls.rise}" is not in the rulebook (w / 6)`);
  const grid = (m.supports.grid as number) ?? 12, top = ctx.pav + m.height, BAND = x.cargoBand.depth, CORE = L.cores.stairLift as [number, number];
  const out: MegaOut = { id, parts: [], prims: [], decks: [], hosts: [], cores: [], connectors: [], avoid: [] };
  const fp = m.footprint[0] as Rect, dr = growRect(fp, -INSET), qw = x.quayWalk, fixed = x.connectors, fixedRects = fixed.map(f => f.rect);
  const mixOf = (lv: string) => m.levels.find(l => l.level === lv)?.mix;
  const streetH: Host = host(`${id}/street`, 'street', ctx.pav, m.areas[0]!, mixOf('street'), [], 3);
  const deckH: Host = host(`${id}/deck`, 'deck', zd, m.areas[0]!, mixOf('deck'), [], 0);
  const strips = stripsOf(ctx, m, 0).map(s => s.r), cols = columnsOf(ctx, m, id);

  // 1. the cargo band round each covered plot, on the plot's building line (plot edge set back by the section's pavement)
  const blocks: Rect[] = [];
  for (const pid of m.spans.plots) {
    const plot = ctx.d.plots.find(p => p.id === pid)!, pb = bbox(plot.polyV), ring = plotRing(ctx, plot);
    const pr0: Rect = [Math.max(pb[0], fp[0]), Math.min(pb[1], fp[1]), Math.max(pb[2], fp[2]), Math.min(pb[3], fp[3])];
    // the class of the plot edge a side lies on (nearest edge by midpoint), and the side's offset: the pavement, or INSET off a neighbour
    const kindOfSide = (mid: V) => ring.c.reduce((best, c, i) => {
      const a = ring.p[i]!, b = ring.p[(i + 1) % ring.p.length]!, d = lineDist(mid, [a, b]);
      return d < best.d ? { d, c } : best;
    }, { d: Infinity, c: 'none' }).c;
    const cls = { y0: kindOfSide([(pr0[0] + pr0[1]) / 2, pr0[2]]), y1: kindOfSide([(pr0[0] + pr0[1]) / 2, pr0[3]]), x0: kindOfSide([pr0[0], (pr0[2] + pr0[3]) / 2]), x1: kindOfSide([pr0[1], (pr0[2] + pr0[3]) / 2]) };
    const off = (s: keyof typeof cls) => pavementOf(ctx, cls[s]) || INSET;
    const pr: Rect = [pr0[0] + off('x0'), pr0[1] - off('x1'), pr0[2] + off('y0'), pr0[3] - off('y1')];
    const sides: { rect: (a: number, b: number) => Rect; lo: number; hi: number; side: 'x0' | 'x1' | 'y0' | 'y1' }[] = [
      { rect: (a, b) => [a, b, pr[2], pr[2] + BAND], lo: pr[0], hi: pr[1], side: 'y0' },
      { rect: (a, b) => [a, b, pr[3] - BAND, pr[3]], lo: pr[0], hi: pr[1], side: 'y1' },
      { rect: (a, b) => [pr[0], pr[0] + BAND, a, b], lo: pr[2] + BAND, hi: pr[3] - BAND, side: 'x0' },
      { rect: (a, b) => [pr[1] - BAND, pr[1], a, b], lo: pr[2] + BAND, hi: pr[3] - BAND, side: 'x1' },
    ];
    for (const s of sides) {
      const c = cls[s.side], kind = c.split(':')[0]!, street = c.split(':')[1] ?? '';
      for (const [a, b] of runs(s.lo, s.hi, grid, x.cargoBand.runBays, r)) {
        const br = s.rect(a, b);
        if (overlaps(br, fixedRects, 0.5)) continue;
        blocks.push(br);
        out.parts.push(rectMass(`${id}/shed-${blocks.length}`, 'industrial', br, 0, zd - td));
        if (['district', 'access', 'quay'].includes(kind)) streetH.edges.push(sideEdge(br, s.side, kind, street));
      }
    }
  }

  // 2. decks: the market deck over plots and the road between, the crane walk on the quay, the quay bridges
  const colOk = (p: V, extra: Rect[] = []) =>
    !ctx.d.roads.some(rd => !rd.gyratory && lineDist(p, rd.line) < rd.w / 2 + 0.8) && !overlaps(growRect([p[0], p[0], p[1], p[1]], 0.6), [...blocks, ...fixedRects, ...strips, ...extra]);
  const dcols: V[] = [];
  for (let px = dr[0] + 6; px < dr[1]; px += grid) for (let py = dr[2] + 6; py < dr[3]; py += grid) if (colOk([px, py])) dcols.push([px, py]);
  out.decks.push(deckRec(ctx, id, `${id}/market`, 'deck', dr, dcols, td));
  const qd = growRect(qw, -INSET), qcols: V[] = [];
  for (let py = qd[2] + 6; py < qd[3]; py += grid) qcols.push([(qd[0] + qd[1]) / 2, py]);
  out.decks.push(deckRec(ctx, id, `${id}/crane-walk`, 'deck', qd, qcols, td));
  const med = ctx.d.zones.filter(z => z.kind === 'median');
  for (const b of m.bridges ?? []) {
    const joins = (b as { joinsDeck?: string }).joinsDeck, bc = rectCentre(b.rect as Rect), mc: V[] = [];
    if (joins && !(x as unknown as Record<string, unknown>)[joins]) throw new Error(`megastructure ${m.id}: bridge ${b.id} joins deck "${joins}", which the megastructure does not have`);
    if (b.supports?.includes('median')) for (const z of med) if (z.poly && pointInRing(bc, z.poly) === false) { const bb = bbox(z.poly), c: V = [(bb[0] + bb[1]) / 2, bc[1]]; if (rectsTouch(growRect(b.rect as Rect, 1), bb, 0) && pointInRing(c, z.poly)) mc.push(c); }
    out.decks.push(deckRec(ctx, id, `${id}/bridge-${b.id}`, b.level, b.rect as Rect, mc, tb));
  }
  const pieces = out.decks.map(d => ({ id: d.id, level: d.level, rect: d.rect }));

  // 3. fixed connectors from the plan: the foot is the named side, the head the opposite side (lifts stand on one spot)
  const conn = (f: Fixed): Connector => {
    const lift = f.kind === 'goodsLift', c = rectCentre(f.rect), [x0, x1, y0, y1] = f.rect;
    const foot = lift ? c : sideMid(f.rect, f.foot!), head = lift ? c : sideMid(f.rect, OPP[f.foot!]);
    return {
      id: `${id}/${f.id}`, kind: f.kind as Connector['kind'], host: id, serves: [], from: 'street', to: 'deck', foot: rv(foot), head: rv(head), bottom: ctx.pav, top: zd,
      width: Math.min(x1 - x0, y1 - y0), stepFree: lift, ring: rectPts(f.rect).map(rv), yaw: 0,
    };
  };
  for (const f of fixed) { out.connectors.push(conn(f)); out.avoid.push(f.rect); }
  for (const f of fixed.filter(q => q.kind === 'goodsLift')) out.parts.push(rectMass(`${id}/${f.id}-shaft`, 'stair', f.rect, 0, zd + ctx.L.headroom.walk + 0.6));
  const headStrip = (f: Fixed): Rect => { const d = 3, [x0, x1, y0, y1] = f.rect; return f.foot === 'y0' ? [x0, x1, y1 - d, y1] : f.foot === 'y1' ? [x0, x1, y0, y0 + d] : f.foot === 'x0' ? [x1 - d, x1, y0, y1] : [x0, x0 + d, y0, y1]; };
  const fixedWays = fixed.filter(f => f.kind !== 'goodsLift').map(f => ({ rect: headStrip(f), serves: servedBy(headStrip(f), pieces, 3) }));

  // 4. cores in the cargo band every `every` m along street edges (first at `firstAt`); 5. extra cores where maxTravel needs one; 6. halls on the deck
  const { every, firstAt } = L.cores as { every: number; firstAt: number }, limit = L.spacing.deck.maxTravel as number;
  const sEdges = streetH.edges.filter(e => Math.hypot(e.b[0] - e.a[0], e.b[1] - e.a[1]) >= firstAt);
  const nearRoads = ctx.d.roads.filter(rd => !rd.gyratory && rd.line.some(p => inRect(p, growRect(fp, 40)))), reachOf = (rd: (typeof nearRoads)[number]) => rd.w / 2 + pavementOf(ctx, `${rd.class}:${rd.id}`);
  const clearAt = new Map<string, boolean>(); // the spot scan below probes the same lattice points again and again
  const pointClear = (p: V) => { const k = `${p[0]},${p[1]}`; let c = clearAt.get(k); if (c === undefined) { c = nearRoads.every(rd => !nearLine(p, rd.line, reachOf(rd), true)); clearAt.set(k, c); } return c; };
  const roadsClear = (rc: Rect) => ([[rc[0], rc[2]], [rc[1], rc[2]], [rc[0], rc[3]], [rc[1], rc[3]], rectCentre(rc)] as V[]).every(pointClear);
  const coreRects = (): Rect[] => {
    const res: Rect[] = [], taken = [...out.avoid];
    for (const e of sEdges) {
      const len = Math.hypot(e.b[0] - e.a[0], e.b[1] - e.a[1]), u: V = [(e.b[0] - e.a[0]) / len, (e.b[1] - e.a[1]) / len];
      for (let s = firstAt; s <= len - CORE[0]; s += every) {
        const c: V = [e.a[0] + u[0] * s - e.n[0] * (CORE[1] / 2 + 1), e.a[1] + u[1] * s - e.n[1] * (CORE[1] / 2 + 1)];
        const horiz = Math.abs(e.n[1]) > 0.5, rc: Rect = horiz ? [c[0] - CORE[0] / 2, c[0] + CORE[0] / 2, c[1] - CORE[1] / 2, c[1] + CORE[1] / 2] : [c[0] - CORE[1] / 2, c[0] + CORE[1] / 2, c[1] - CORE[0] / 2, c[1] + CORE[0] / 2];
        if (!blocks.some(b => inRect([rc[0], rc[2]], b) && inRect([rc[1], rc[3]], b)) || overlaps(rc, taken, 0.5)) continue;
        res.push(rc); taken.push(rc);
      }
    }
    return res;
  };
  // halls: columns of `halls.w` wide, `aisle` m between, `inset` m off the deck edge, none in the cross aisle or within 5 m of a core
  const hallsFor = (taken: Rect[]) => {
    const usable = growRect(dr, -x.halls.inset), clear = [...out.avoid, ...taken].map(a => growRect(a, 5)).concat([[x.crossAisle.x[0], x.crossAisle.x[1], usable[2], usable[3]] as Rect]);
    const hr = rng(`${id}/halls`), res: { r: Rect; w: number }[] = [];
    let px = usable[0];
    while (px < usable[1] - x.halls.w[0]) {
      const w = hr.pick(x.halls.w); if (px + w > usable[1]) break;
      let py = usable[2];
      while (py < usable[3] - x.halls.len[0]) {
        const len = x.halls.lenStep * hr.int(x.halls.len[0] / x.halls.lenStep, x.halls.len[1] / x.halls.lenStep), y1 = Math.min(usable[3], py + len), rc: Rect = [px, px + w, py, y1];
        if (y1 - py >= x.halls.len[0] && !overlaps(rc, clear)) res.push({ r: rc, w });
        py = y1 + x.halls.aisle;
      }
      px += w + x.halls.aisle;
    }
    return res;
  };
  // halls first (round the band cores), then extra cores wherever maxTravel needs one: on the deck, clear of roads and pavements, fixed
  // connectors, the cross aisle, the halls and the cranes, and wholly inside one shed or wholly outside them
  const cranesAt: V[] = [], cx = (qd[0] + qd[1]) / 2;
  for (let y = qd[2] + x.cranes.first; y < qd[3] - x.cranes.lastClear; y += x.cranes.every) cranesAt.push([cx + 4, y]);
  const bandCores = coreRects(), hallsAll = hallsFor(bandCores), walls = hallsAll.map(h => h.r);
  const spots: { r: Rect; cx: number; cy: number; pen: number }[] = [];
  const hard = [...fixedRects, ...strips, ...bandCores, ...out.avoid.map(a => growRect(a, 1))];
  for (const d of [dr, qd]) for (let px = d[0] + 1; px < d[1] - 1; px += 1) for (let py = d[2] + 1; py < d[3] - 1; py += 1) for (const [w, h] of [[CORE[0], CORE[1]], [CORE[1], CORE[0]]] as const) {
    const rc: Rect = [px, px + w, py, py + h];
    if (rc[1] > d[1] - 1 || rc[3] > d[3] - 1 || (rc[0] < x.crossAisle.x[1] && rc[1] > x.crossAisle.x[0]) || overlaps(rc, hard, 0)) continue;
    if (overlaps(rc, blocks, 0) && !blocks.some(b => rc[0] >= b[0] && rc[1] <= b[1] && rc[2] >= b[2] && rc[3] <= b[3])) continue;
    if (cranesAt.some(c => inRect(c, growRect(rc, 2.5))) || !roadsClear(rc)) continue;
    spots.push({ r: rc, cx: px + w / 2, cy: py + h / 2, pen: walls.filter(q => rectsTouch(rc, q, -0.01)).length }); // pen: halls the spot would take the place of
  }
  const ways = [...fixedWays, ...bandCores.map(rc => ({ rect: rc, serves: servedBy(rc, pieces) }))];
  // fill at 2 m cells aiming a little inside the limit (the walk is measured on a grid), then check at 1 m cells; tighten until it holds
  let fill!: ReturnType<typeof fillTravel>, fin!: ReturnType<typeof reach>;
  for (const margin of [3, 5, 8]) {
    fill = fillTravel({
      pieces, ways, obstacles: walls, limit: limit - margin, cell: 2, serves: rc => servedBy(rc, pieces),
      candidates: (c, taken, tier) => spots.filter(s => (tier || !s.pen) && Math.hypot(s.cx - c[0], s.cy - c[1]) < 45 && !overlaps(s.r, taken.map(t => growRect(t, 1)), 0)).map(s => ({ r: s.r, d: Math.hypot(s.cx - c[0], s.cy - c[1]) })).sort((a, b) => a.d - b.d).map(q => q.r),
    });
    fin = reach(pieces, [...ways, ...fill.added.map(rc => ({ rect: rc, serves: servedBy(rc, pieces) }))], fill.obstacles, limit, 1);
    if (fin.max <= limit) break;
  }
  if (fin.max > limit) throw new Error(`${m.id}: no core layout keeps every deck point within ${limit} m of a way down (worst ${fin.max.toFixed(1)} m at ${fin.worst})`);
  const coreList = [...bandCores, ...fill.added], halls = hallsAll.filter(h => fill.obstacles.includes(h.r));
  coreList.forEach((rc, ci) => {
    out.cores.push({ id: `${id}/core-${ci}`, host: id, rect: rc, from: 'street', to: 'deck', lift: ci % 2 === 0, stair: true } satisfies Core);
    out.parts.push(rectMass(`${id}/core-${ci}-head`, 'stair', rc, zd, zd + ctx.L.headroom.walk + 0.6));
    out.avoid.push(rc);
  });
  halls.forEach((h, hi) => {
    const hr = h.r, rise = h.w / 6, wall = top - rise, n = `${id}/hall-${hi}`;
    out.parts.push(rectMass(`${n}/walls`, 'mega', hr, zd, wall));
    out.prims.push(prim(`${n}/vault`, 'vault', 'roof', rectCentre(hr), [hr[3] - hr[2], h.w, rise], wall, Math.PI / 2));
    deckH.edges.push(sideEdge(hr, 'x0', 'deck', 'market-aisle'), sideEdge(hr, 'x1', 'deck', 'market-aisle'));
  });
  out.blocks = halls.map(h => h.r);

  // 7. gantry cranes on the crane walk
  cranesAt.forEach(([px, py], k) => {
    const n = `${id}/crane-${k}`;
    out.prims.push(box(`${n}/mast`, CRANE, [px, py], [2.4, 2.4, x.craneMasts - zd], zd), box(`${n}/boom`, CRANE, [px + 10, py], [40, 1.6, 1.6], x.craneMasts - 2), box(`${n}/cab`, 'enamel', [px, py], [4, 3, 3], x.craneMasts - 4.5));
  });
  out.prims.push(...cols.prims); out.columns = cols.pts;
  out.hosts.push(streetH, deckH);
  return out;
}
