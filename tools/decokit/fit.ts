// The fit table: each generated piece's built size against its site, from the order (plan) and the Blender measurement (`built`).
// Frame: the piece's pivot = its footprint centre, placed at the site rect's centre; extents are [xmin, xmax, ymin, ymax] about it.
import type { Orders } from './orders';

export type Ext = [number, number, number, number] | null;
export type Built = { split: number; roof: number | null; lower: Ext; upper: Ext; crown: Ext; tip: number; tipPlan: number };
export type FitRow = { piece: string; order: string; size: string; tip: number; limit: number; fits: boolean; why: string[] };
export const TOL = 0.05; // the order's tip is rounded to 0.1 (built 167.83 vs 167.8); the disc rim lip allowance below
export const LIP = 0.1;  // a landing disc's rim bead stands this far proud of its radius

const half = (e: Ext) => e ? [Math.max(-e[0], e[1]), Math.max(-e[2], e[3])] as const : [0, 0] as const;
const inside = (e: Ext, w: number, d: number, tol = 0) => { const [hx, hy] = half(e); return hx * 2 <= w + tol + 1e-9 && hy * 2 <= d + tol + 1e-9; };

export function fitRow(orders: Orders, id: string, order: { hash: string }, b: Built): FitRow {
  const o = orders.pieces.find(p => p.id === id);
  if (!o) return { piece: id, order: '?', size: '', tip: b.tipPlan, limit: 0, fits: false, why: ['no such order in the plan'] };
  const site = o.sites[0]!, [x0, x1, y0, y1] = site.rect, w = x1 - x0, d = y1 - y0, g = orders.grid, why: string[] = [];
  const reg = o.regen as { tip: number; at?: [number, number]; discsInPlan?: string };
  const lim = reg.tip + TOL;
  if (order.hash !== o.hash) why.push('order hash differs: the plan changed, regenerate');
  if (!inside(b.lower, w, d)) why.push(`footprint ${half(b.lower).map(h => h * 2)} > site ${w}×${d}`);
  if (b.tipPlan > lim) why.push(`tip ${b.tipPlan} > ${reg.tip}`);
  const shaft = o.kind === 'tower' && !('tiers' in o.regen); // Needle, DiscTower: a round shaft with a disc that overhangs the site over the podium (the order's why states it)
  if (o.kind === 'signature') {
    if (b.tipPlan < reg.tip - 0.5) why.push(`tip ${b.tipPlan} short of the dominant ${reg.tip}`);
  } else if (shaft) {
    // footprint (shaft + foot) and tip are checked above; the disc may overhang the site over the podium
  } else if (o.kind === 'tower') {
    // the tip is bounded by the order (standards.skyline.rivalMax and tiers), not the old towers.crownTip 106
    if (!inside(b.upper, w - 2 * g.halfBay, d - 2 * g.halfBay)) why.push(`above the setback ${half(b.upper).map(h => h * 2)} > ${w - 2 * g.halfBay}×${d - 2 * g.halfBay}`);
    if (!inside(b.crown, w, d)) why.push(`crown ${half(b.crown).map(h => h * 2)} > site`);
  } else {
    const m = /rims inside x (\d+)–(\d+), \|y\| ≤ ([\d.]+)/.exec(reg.discsInPlan ?? ''), [ax, ay] = reg.at ?? [0, 0];
    if (!m) why.push('no rim bound in the order');
    else if (b.upper) {
      const [rx0, rx1, ry] = [Number(m[1]) - ax, Number(m[2]) - ax, Number(m[3])], u = b.upper;
      if (u[0] < rx0 - LIP || u[1] > rx1 + LIP || Math.max(-u[2], u[3]) > ry + LIP) why.push(`discs x ${u[0]}…${u[1]}, y ${u[2]}…${u[3]} outside x ${rx0}…${rx1}, |y| ≤ ${ry}`);
    }
  }
  const lo = b.lower ?? [0, 0, 0, 0];
  return { piece: id, order: o.hash.slice(0, 8), size: `${(lo[1] - lo[0]).toFixed(1)} × ${(lo[3] - lo[2]).toFixed(1)} × ${b.tipPlan.toFixed(1)}`, tip: b.tipPlan, limit: reg.tip, fits: why.length === 0, why };
}

export function printTable(rows: FitRow[]): string {
  const pad = (s: string, n: number) => s.padEnd(n), W = [34, 10, 22, 8, 5];
  const out = [[pad('piece', W[0]!), pad('order', W[1]!), pad('built W×D×tip', W[2]!), pad('tip≤', W[3]!), 'fits'].join(' ')];
  for (const r of rows) out.push([pad(r.piece, W[0]!), pad(r.order, W[1]!), pad(r.size, W[2]!), pad(String(r.limit), W[3]!), r.fits ? 'yes' : 'NO ' + r.why.join('; ')].join(' '));
  return out.join('\n');
}
