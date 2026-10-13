//: street frontage sampling from the plan (read-only)
import plan from '../../contracts/world/harbour-district.plan.json';
type P = any;
const pl: P = plan;
export const nodes: Record<string, [number, number]> = pl.nodes;
export const plots: any[] = pl.plots;
export const megas: any[] = pl.megastructures;
const inRect = (r: number[], x: number, y: number) => x >= r[0] && x <= r[1] && y >= r[2] && y <= r[3];
export function lookup(x: number, y: number) {
  for (const m of megas) if (m.footprint.some((f: number[]) => inRect(f, x, y))) {
    const st = m.levels.find((l: any) => l.level === 'street');
    const plot = plots.find(p => p.rect && inRect(p.rect, x, y));
    return { area: plot?.area ?? m.areas[0], use: plot?.use ?? 'mixed', mix: st?.mix ?? null, mega: m.id };
  }
  const p = plots.find(p => p.rect && inRect(p.rect, x, y));
  if (!p) return null;
  return { area: p.area, use: p.use, mix: pl.standards.frontage.mix[p.area], mega: null };
}
export function areaAt(x: number, y: number): string | null {
  for (const a of pl.areas) if (inRect(a.rect, x, y)) return a.id;
  return null;
}
export type Street = { id: string; name: string; cls: string; w: number; median: number; len: number; segs: [number, number, number, number][]; kind: 'road' | 'mall' | 'walk' };
export function streets(): Street[] {
  const out: Street[] = [];
  for (const r of pl.roads) {
    if (r.gyratory) continue;
    const segs: Street['segs'] = []; let len = 0;
    for (let i = 0; i + 1 < r.path.length; i++) { const a = nodes[r.path[i]], b = nodes[r.path[i + 1]]; segs.push([a[0], a[1], b[0], b[1]]); len += Math.hypot(b[0] - a[0], b[1] - a[1]); }
    out.push({ id: r.id, name: r.name, cls: r.class, w: r.w, median: r.median ?? 0, len, segs, kind: 'road' });
  }
  for (const f of pl.foot) {
    const segs: Street['segs'] = []; let len = 0;
    for (let i = 0; i + 1 < f.pts.length; i++) { const a = f.pts[i], b = f.pts[i + 1]; segs.push([a[0], a[1], b[0], b[1]]); len += Math.hypot(b[0] - a[0], b[1] - a[1]); }
    out.push({ id: f.id, name: f.name, cls: f.kind, w: f.w, median: 0, len, segs, kind: f.kind });
  }
  return out;
}
/** sample points 2 m inside the plot line each side of a street, every `step` m. */
export function frontage(s: Street, pavement: number, step = 4) {
  const half = s.kind === 'road' ? s.w / 2 + pavement + 2 : s.w / 2 + 2;
  const res: { side: number; hit: ReturnType<typeof lookup> }[] = [];
  for (const [x0, y0, x1, y1] of s.segs) {
    const L = Math.hypot(x1 - x0, y1 - y0), dx = (x1 - x0) / L, dy = (y1 - y0) / L;
    for (let t = step / 2; t < L; t += step) for (const side of [-1, 1])
      res.push({ side, hit: lookup(x0 + dx * t - dy * half * side, y0 + dy * t + dx * half * side) });
  }
  return res;
}
