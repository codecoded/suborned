// Terraced housing over a podium (Crown Terraces): a two-storey podium to the deck, the Terrace Walk along its south edge,
// housing stepping up one storey per step from the walk to Crown Road with 9 m breaks, stair-lift cores in the podium
// every `cores.every` m and a stair-lift tower on the park side of each deck bridge. All of it is read from the plan's fields
// (walk, steps, podium, breaks, cores, bridges[].joinsRect, colonnade); the colonnade lifts the walk and the podium's upper
// storey over the pavement, and the ground floor stands on the building line.
import type { Megastructure } from '../../../plan/plan';
import type { V } from '../../../plan/geom';
import { type Ctx, heightOf } from '../common';
import { type Rect, bbox, growRect, pointInRing, rectCentre, rectPts, rectsTouch, subtractRect } from '../geo';
import { fillTravel } from '../travel';
import type { Core, MegaOut, Terrace } from '../types';
import { colonnadeOf, columnsOf, groundRect, lifted, stripsOf } from './colonnade';
import { INSET, deckRec, host, rectMass, servedBy, sideEdge, zOf } from './kit';

type Bridge = { id: string; level: string; rect: Rect; supports?: string[]; joinsRect?: Rect; joinsHead?: Rect };
type Cores = { every: number; firstAt: number; from: string; to: string; liftEvery: number };

export function terraces(ctx: Ctx, m: Megastructure): MegaOut {
  const id = `mega/${m.id}`, zd = zOf(ctx, 'deck'), td = ctx.L.structureDepth.deck, tb = ctx.L.structureDepth.bridge, CORE = ctx.L.cores.stairLift as [number, number];
  const x = m as unknown as { storeys: [number, number]; walk: { edge: string; w: number }; steps: { storeys: [number, number] }; podium: { storeys: number }; breaks: [number, number][]; cores: Cores; bridges: Bridge[] };
  if (x.walk.edge !== 'y0') throw new Error(`megastructure ${m.id}: walk edge "${x.walk.edge}" is not in the rulebook (y0)`);
  const out: MegaOut = { id, parts: [], prims: [], decks: [], hosts: [], cores: [], connectors: [], avoid: [], terraces: [] };
  const fp = m.footprint[0] as Rect, r0 = growRect(fp, -INSET), [n0, n1] = x.steps.storeys, strips = stripsOf(ctx, m, 0), g0 = groundRect(m, 0, r0);
  const mixOf = (lv: string) => m.levels.find(l => l.level === lv)?.mix;
  out.parts.push(...lifted(`${id}/podium`, 'limestone', r0, 0, zd - td, strips));
  const walk: Rect = [r0[0], r0[1], r0[2], r0[2] + x.walk.w];
  out.decks.push(deckRec(ctx, id, `${id}/walk`, 'deck', walk, [], td));
  // the park-side towers named by the bridges (joinsRect, with joinsHead where the landing overhangs): ground floor on the building line
  const towers: { shaft: Rect; head: Rect }[] = [];
  for (const b of x.bridges) if (b.joinsRect) towers.push({ shaft: b.joinsRect, head: b.joinsHead ?? b.joinsRect });
  towers.forEach((t, i) => {
    const inside = (f: Rect) => t.head[0] >= f[0] && t.head[1] <= f[1] && t.head[2] >= f[2] && t.head[3] <= f[3], fi = m.footprint.findIndex((f, k) => k > 0 && inside(f as Rect)), st = fi > 0 ? stripsOf(ctx, m, fi) : [];
    out.parts.push(...lifted(`${id}/tower-${i}`, 'stair', t.head, 0, zd + ctx.L.headroom.walk + 0.6, st));
  });
  const deckPieces = (): { id: string; level: string; rect: Rect }[] => out.decks.filter(d => d.level === 'deck').map(d => ({ id: d.id, level: d.level, rect: d.rect }));
  const med = ctx.d.zones.filter(z => z.kind === 'median');
  for (const b of x.bridges) {
    const rc = rectCentre(b.rect), cols: V[] = [];
    if (b.supports?.includes('median')) { const c: V = rc; if (med.some(z => z.poly && pointInRing(c, z.poly))) cols.push(c); }
    out.decks.push(deckRec(ctx, id, `${id}/bridge-${b.id}`, b.level, b.rect, cols, tb));
  }
  // cores in the podium behind the walk: first at `firstAt`, then every `every` m
  const cores: Rect[] = [], coreAt = (cx: number): Rect => [cx - CORE[0] / 2, cx + CORE[0] / 2, walk[3], walk[3] + CORE[1]];
  for (let cx = r0[0] + x.cores.firstAt; cx < r0[1] - x.cores.firstAt / 2; cx += x.cores.every) cores.push(coreAt(cx));
  const sites = [...cores];
  const pieces = deckPieces(), limit = ctx.L.spacing.deck.maxTravel as number;
  const ways = [...cores, ...towers.map(t => t.shaft)].map(rc => ({ rect: rc, serves: servedBy(rc, pieces) }));
  const fill = fillTravel({
    pieces, ways, obstacles: [], limit, serves: rc => servedBy(rc, pieces),
    candidates: (w, taken) => {
      const cs: Rect[] = [];
      for (let cx = r0[0] + CORE[0] / 2 + 1; cx < r0[1] - CORE[0] / 2 - 1; cx += 1) { const rc = coreAt(cx); if (![...sites, ...taken, ...towers.map(t => t.shaft)].some(o => rectsTouch(growRect(rc, 0.5), o, 0))) cs.push(rc); }
      return cs.sort((a, b) => Math.abs((a[0] + a[1]) / 2 - w[0]) - Math.abs((b[0] + b[1]) / 2 - w[0]));
    },
  });
  cores.push(...fill.added);
  cores.sort((a, b) => a[0] - b[0]).forEach((rc, i) => {
    out.cores.push({ id: `${id}/core-${i}`, host: id, rect: rc, from: x.cores.from, to: x.cores.to, lift: i % x.cores.liftEvery === 0, stair: true } satisfies Core);
    out.parts.push(rectMass(`${id}/core-${i}-head`, 'stair', rc, zd, zd + ctx.L.headroom.walk + 0.6));
  });
  towers.forEach((t, i) => out.cores.push({ id: `${id}/tower-core-${i}`, host: id, rect: t.shaft, from: x.cores.from, to: x.cores.to, lift: true, stair: true }));
  // housing: one storey per step from the walk's back to the far street, broken at the gaps and round the cores
  const breaks: Rect[] = x.breaks.map(([a, b]) => [a, b, r0[2], r0[3]]);
  const y0 = walk[3], nSteps = n1 - n0 + 1, dep = (r0[3] - y0) / nSteps;
  for (let i = 0; i < nSteps; i++) {
    let pcs: Rect[] = [[r0[0], r0[1], y0 + i * dep, y0 + (i + 1) * dep]];
    for (const cut of [...breaks, ...cores]) pcs = pcs.flatMap(p => subtractRect({ p: rectPts(p), c: ['x', 'x', 'x', 'x'] }, cut).map(q => bbox(q.p)));
    const top = ctx.pav + heightOf(ctx.B, n0 + i);
    pcs.forEach((p, k) => {
      if (p[1] - p[0] < 6) return;
      out.parts.push(rectMass(`${id}/step-${i}-${k}`, 'residential', p, zd, top));
      (out.terraces as Terrace[]).push({ id: `${id}/step-${i}-${k}`, host: id, rect: p, z: top });
    });
  }
  out.hosts.push(
    host(`${id}/street`, 'street', ctx.pav, m.areas[0]!, mixOf('street'), colonnadeOf(m).filter(c => c.footprint === 0).map(c => sideEdge(g0, c.edge, c.class, c.faces)), 3),
    host(`${id}/deck`, 'deck', zd, m.areas[0]!, mixOf('deck'), [{ a: [r0[0], walk[3]], b: [r0[1], walk[3]], n: [0, -1], kind: 'deck', street: 'terrace-walk', cornerA: false, cornerB: false }], 0));
  const cols = columnsOf(ctx, m, id);
  out.prims.push(...cols.prims); out.columns = cols.pts;
  return out;
}
