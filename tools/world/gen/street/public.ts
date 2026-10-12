// Street dressing that is not a plot edge: medians, malls, walks and the pier, the circus island ring, park lawns and the
// edge bands of decks and concourses.
import { type V, dist, lineDist, polyline } from '../../../plan/geom';
import type { Ctx } from '../common';
import { rng } from '../common';
import { type Rect, areaOf, bbox, pointInRing } from '../geo';
import type { DeckRec } from '../types';
import { boxRing, samples9, tramBlock } from '../../../plan/street-zones';
import type { Layer } from './kit';

const WASH = (S: any) => S.trees.species.washingtonia, PHX = (S: any) => S.trees.species.phoenix;

export function dressMedians(ctx: Ctx, L: Layer): void {
  const S = ctx.S, z = ctx.pav, sky = ctx.d.sky.map(p => polyline(p.segs, 4));
  const t2 = ctx.plan.tram.find(l => l.id === 'T2')?.stops ?? [], slews: Rect[] = [];
  for (const rd of ctx.plan.roads) for (const run of (rd as any).tram?.runs ?? []) if (run.slew) { const [f, t] = [run.from, run.to] as V[]; slews.push([Math.min(f[0]!, t[0]!) - 10, Math.max(f[0]!, t[0]!) + 10, Math.min(f[1]!, t[1]!) - 10, Math.max(f[1]!, t[1]!) + 10]); }
  ctx.d.zones.filter(zn => zn.kind === 'median' && zn.poly).forEach((zn, zi) => {
    const p = zn.poly!, e0 = dist(p[0]!, p[1]!), e1 = dist(p[1]!, p[2]!), long = e0 >= e1, w = long ? e1 : e0, len = long ? e0 : e1;
    const a: V = long ? [(p[0]![0] + p[3]![0]) / 2, (p[0]![1] + p[3]![1]) / 2] : [(p[0]![0] + p[1]![0]) / 2, (p[0]![1] + p[1]![1]) / 2];
    const b: V = long ? [(p[1]![0] + p[2]![0]) / 2, (p[1]![1] + p[2]![1]) / 2] : [(p[3]![0] + p[2]![0]) / 2, (p[3]![1] + p[2]![1]) / 2];
    const u: V = [(b[0] - a[0]) / len, (b[1] - a[1]) / len], at = (s: number): V => [a[0] + u[0] * s, a[1] + u[1] * s], id = `median/${zi}`, wide = w >= 6;
    const ok = (q: V, tram: boolean) => !(tram && w < 6 && t2.some(s => dist(s.at, q) < 22)) && !slews.some(r => q[0] >= r[0] && q[0] <= r[1] && q[1] >= r[2] && q[1] <= r[3]);
    const nT = Math.floor((len - 12) / 12) + 1, t0 = (len - (nT - 1) * 12) / 2;
    for (let k = 0; k < nT; k++) { const q = at(t0 + k * 12); if (ok(q, true) && !sky.some(l => lineDist(q, l) < 4.5)) L.add('tree', q, 0, z, id, { size: wide ? [0.9, 0.9, PHX(S).top] : [0.5, 0.5, WASH(S).top], variant: wide ? 'phoenix' : 'washingtonia', r: wide ? 2.4 : 1.5 }); }
    for (let s = t0 + 6; s < len - 4; s += 36) { const q = at(s); if (ok(q, true)) L.add('lamp', q, 0, z, id, { size: [0.4, 0.4, 10], variant: 'twin-boomerang', r: 1 }); }
  });
}

export function dressFoot(ctx: Ctx, L: Layer): void {
  const S = ctx.S, z = ctx.pav;
  for (const f of ctx.plan.foot) {
    const [a, b] = [f.pts[0]!, f.pts[f.pts.length - 1]!], len = dist(a, b), u: V = [(b[0] - a[0]) / len, (b[1] - a[1]) / len], nr: V = [-u[1], u[0]], id = `foot/${f.id}`, yaw = Math.atan2(u[1], u[0]);
    const at = (s: number, off: number): V => [a[0] + u[0] * s + nr[0] * off, a[1] + u[1] * s + nr[1] * off];
    if (f.kind === 'mall') {
      for (const side of [1, -1]) {
        const off = side * 11;
        for (let s = 12, k = 0; s < len - 6; s += 12, k++) {
          L.add('tree', at(s, off), 0, z, id, { size: [0.5, 0.5, WASH(S).top], variant: 'washingtonia', r: 1.5 });
          L.add('bench', at(s + 3, off), yaw, z, id, { r: 1 }); L.add('lamp', at(s + 6, off), 0, z, id, { size: [0.25, 0.25, 4.5], variant: 'sputnik', r: 0.5 });
          if (k % 4 === 1) L.add('bin', at(s + 4.5, off), 0, z, id, { r: 0.4 });
        }
      }
      // centre band, off the beam: kiosks every 60 m, clocks every 120, advertising columns >= 60 apart, vending robots
      for (let s = 30, k = 0; s < len - 20; s += 60, k++) {
        const side = k % 2 ? 1 : -1, off = side * 7;
        L.add('newsstand', at(s, off), yaw, z, id); L.add('kiosk-food', at(s + 20, -off), yaw, z, id);
        if (k % 2 === 0) L.add('public-clock', at(s + 30, off), 0, z, id); if (k % 3 === 1) L.add('advertising-column', at(s + 40, -off), 0, z, id);
        if (k % 2 === 1) L.add('vending-robot', at(s + 10, off), yaw, z, id);
      }
    } else if (f.id === 'skyport-pier') {
      for (let s = 12, k = 0; s < len - 6; s += 24, k++) { L.add('tub-palm', at(s, k % 2 ? 5 : -5), 0, z, id, { size: [1.5, 1.5, S.trees.species.tub.top], variant: 'tub', r: 1 }); }
      for (let s = 24; s < len - 6; s += 24) for (const side of [1, -1]) L.add('lamp', at(s, side * 5.2), 0, z, id, { size: [0.25, 0.25, 4.5], variant: 'sputnik', r: 0.5, force: true });
      for (let s = 60; s < len - 20; s += 120) L.add('kiosk-food', at(s, 4.5), yaw, z, id);
    } else {
      for (let s = 12, k = 0; s < len - 6; s += 12, k++) L.add('tree', at(s, k % 2 ? 3 : -3), 0, z, id, { size: [0.5, 0.5, WASH(S).top], variant: 'washingtonia', r: 1.5 });
      for (let s = 18, k = 0; s < len - 6; s += 18, k++) L.add('lamp', at(s, k % 2 ? -3.4 : 3.4), 0, z, id, { size: [0.25, 0.25, 4.5], variant: 'sputnik', r: 0.5 });
      for (let s = 24, k = 0; s < len - 6; s += 24, k++) { L.add('bench', at(s, k % 2 ? 3 : -3), yaw, z, id, { r: 1 }); L.add('bin', at(s + 1.8, k % 2 ? 3 : -3), 0, z, id, { r: 0.4 }); }
    }
  }
}

export function dressCircus(ctx: Ctx, L: Layer): void {
  const S = ctx.S, c = ctx.plan.circus, z = ctx.pav, id = 'circus', rm = (c.garden + c.island) / 2;
  const ring = (r: number, step: number, f: (a: number, p: V, k: number) => void, off = 0) => { const n = Math.floor((2 * Math.PI * r) / step); for (let k = 0; k < n; k++) { const a = off + (k / n) * 2 * Math.PI; f(a, [c.c[0] + r * Math.cos(a), c.c[1] + r * Math.sin(a)], k); } };
  // masts and cameras stand on the island ring band at the first radius the tram loop does not sweep (SF-3)
  const onBand = (a: number, size: [number, number, number]): V => {
    for (const r of [rm, rm - 2.3, rm + 3, rm - 3.2, rm + 3.4, c.garden + 0.8, c.island - 0.4]) { const p: V = [c.c[0] + r * Math.cos(a), c.c[1] + r * Math.sin(a)]; if (!tramBlock(ctx, samples9(boxRing(p, size[0], size[1], a)))) return p; }
    return [c.c[0] + rm * Math.cos(a), c.c[1] + rm * Math.sin(a)];
  };
  for (let k = 0; k < 4; k++) { const a = Math.PI / 4 + (k * Math.PI) / 2, size: [number, number, number] = [0.5, 0.5, 14]; L.add('lamp', onBand(a, size), 0, z, id, { size, variant: 'starburst-mast', r: 1.2, force: true }); }
  for (const a of [Math.PI / 2, -Math.PI / 2]) { const size: [number, number, number] = [0.4, 0.4, 8]; L.add('mast-camera', onBand(a, size), a, z, id, { size, variant: 'mast', r: 0.4, force: true }); }
  ring(rm, 12, (a, p, k) => { if (k % 2 === 0) L.add('tree', p, 0, z, id, { size: [0.9, 0.9, PHX(S).top], variant: 'phoenix', r: 2.4 }); else L.add('lamp', p, 0, z, id, { size: [0.25, 0.25, 4.5], variant: 'sputnik', r: 0.5 }); }, 0.05);
  // the pavement ring: palms at the kerb every 12 m clear of the arms that meet the circus
  const rk = c.ring[1] + 1.2, arms = ctx.d.roads.filter(r => !r.gyratory);
  ring(rk, S.trees.spacing, (a, p) => { if (arms.some(r => lineDist(p, r.line) < r.w / 2 + 5)) return; L.add('tree', p, a, z, id, { size: [0.5, 0.5, WASH(S).top], variant: 'washingtonia', r: 1.5 }); }, 0.03);
  ring(c.ring[1] + 0.9, 48, (a, p) => { if (!arms.some(r => lineDist(p, r.line) < r.w / 2 + 5)) L.add('lamp', p, 0, z, id, { size: [0.3, 0.3, 8], variant: 'atom-ring', r: 0.6 }); }, 0.2);
}

export function dressParks(ctx: Ctx, L: Layer): void {
  for (const pl of ctx.d.plots) {
    if (!['park', 'open'].includes(pl.use) || pl.civic) continue;
    const r = rng(`lawn/${pl.id}`), bb = bbox(pl.polyV), n = Math.floor(areaOf(pl.polyV) / 900);
    for (let i = 0, tries = 0; i < n && tries < n * 20; tries++) {
      const p: V = [bb[0] + r.f() * (bb[1] - bb[0]), bb[2] + r.f() * (bb[3] - bb[2])];
      if (pointInRing(p, pl.polyV) && L.add('tree', p, 0, ctx.pav, `lawn/${pl.id}`, { size: [0.9, 0.9, PHX(ctx.S).top], variant: 'phoenix', r: 4 })) i++;
    }
  }
}

/** Edge-band items on decks and concourses wide enough for them: planters with tub palms, sputnik lamps, benches, bins. */
export function dressDecks(ctx: Ctx, L: Layer, decks: DeckRec[]): void {
  const band = ctx.S.levels;
  for (const d of decks) {
    const [x0, x1, y0, y1] = d.rect, w = Math.min(x1 - x0, y1 - y0), z = ctx.L.list.find((l: any) => l.id === d.level)?.z ?? 0;
    if (w < ctx.L.minWidth.deck || d.id.includes('/bridge-') || d.id.includes('/gallery')) continue;
    const e = (band[d.level]?.edgeBand ?? 1.5) / 2, run = (a: V, b: V, yaw: number) => {
      const len = dist(a, b), u: V = [(b[0] - a[0]) / len, (b[1] - a[1]) / len];
      for (let s = 6, k = 0; s < len - 3; s += 12, k++) {
        const p: V = [a[0] + u[0] * s, a[1] + u[1] * s];
        L.add('planter', p, yaw, z, d.id, { r: 1, ground: false }); L.add('tub-palm', p, 0, z, d.id, { size: [1.5, 1.5, ctx.S.trees.species.tub.top], variant: 'tub', r: 0.8, ground: false, force: true });
        const q: V = [p[0] + u[0] * 6, p[1] + u[1] * 6]; L.add('lamp', q, 0, z, d.id, { size: [0.25, 0.25, 3.5], variant: 'sputnik', r: 0.5, ground: false });
        if (k % 2 === 1) { L.add('bench', [p[0] + u[0] * 3, p[1] + u[1] * 3], yaw, z, d.id, { r: 1, ground: false }); L.add('bin', [p[0] + u[0] * 4.8, p[1] + u[1] * 4.8], 0, z, d.id, { r: 0.4, ground: false }); }
      }
    };
    run([x0 + e, y0 + e], [x1 - e, y0 + e], 0); run([x0 + e, y1 - e], [x1 - e, y1 - e], 0);
    if (y1 - y0 > 20) { run([x0 + e, y0 + e], [x0 + e, y1 - e], Math.PI / 2); run([x1 - e, y0 + e], [x1 - e, y1 - e], Math.PI / 2); }
  }
}
