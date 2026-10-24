// Top-down heat maps for the geometry report (brief 055, Splice): the drawn world as a quiet base, one audit's cells on it. Block frame: x runs down the page (south), y runs right (east),
// which is the plan frame with north up: the plan's y is block -x and its x is block y.
import { encodePng } from '../hud-budget/png';
import type { V } from '../plan/geom';
import type { Sim } from './sim';
import type { Visible } from './visible';

export type Layer = { cells: { b: V; v?: number }[]; size: number; colour: (v: number | undefined) => [number, number, number]; alpha?: number };
export type Marker = { b: V; label: string; colour: string };
export type HeatMap = { w: number; h: number; png: string; box: { x0: number; y0: number; x1: number; y1: number }; px: number };

const rampSink = (v = 0): [number, number, number] => { const t = Math.min(1, Math.max(0, (Math.abs(v) - 0.05) / 0.75)); return v < 0 ? [60, 110, 230] : [255, Math.round(215 - 175 * t), Math.round(40 - 30 * t)]; };
export const colours = { sink: rampSink, through: () => [214, 40, 160] as [number, number, number], brush: () => [240, 160, 210] as [number, number, number], invisible: () => [20, 160, 200] as [number, number, number], eye: () => [240, 130, 20] as [number, number, number], chest: () => [250, 190, 60] as [number, number, number], ceiling: () => [120, 70, 200] as [number, number, number] };

/** Render the block-frame box {x0,y0,x1,y1} (metres) at `px` pixels per metre. */
export function renderMap(sim: Sim, vis: Visible, box: { x0: number; y0: number; x1: number; y1: number }, px: number, layers: Layer[], outlines: { poly: V[]; colour: [number, number, number] }[] = []): HeatMap {
  const w = Math.round((box.y1 - box.y0) * px), h = Math.round((box.x1 - box.x0) * px), data = new Uint8Array(w * h * 4);
  const R = vis.R;
  const put = (x: number, y: number, c: [number, number, number], a = 1) => { if (x < 0 || y < 0 || x >= w || y >= h) return; const k = (y * w + x) * 4; data[k] = Math.round(data[k]! * (1 - a) + c[0] * a); data[k + 1] = Math.round(data[k + 1]! * (1 - a) + c[1] * a); data[k + 2] = Math.round(data[k + 2]! * (1 - a) + c[2] * a); data[k + 3] = 255; };
  // base: ground light, roads slate, solids darker with height
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const b: V = [box.x0 + (y + 0.5) / px, box.y0 + (x + 0.5) / px], [i, j] = R.cellOf(sim.frame.toPlan(b));
    if (!R.inside(i, j)) { put(x, y, [236, 234, 228]); continue; }
    const col = R.column(i, j), rise = col.top - col.ground;
    let c: [number, number, number];
    if (rise > 0.35 && !col.walkable) { const t = Math.min(1, rise / 10); c = [Math.round(150 - 80 * t), Math.round(150 - 80 * t), Math.round(158 - 70 * t)]; }
    else if (col.ground < 0.1) c = [205, 212, 214]; else c = [238, 234, 224];
    if (col.walkable && rise > 0.05) c = [214, 205, 188];
    put(x, y, c);
  }
  for (const L of layers) for (const cell of L.cells) {
    const half = L.size / 2, x0 = Math.floor((cell.b[1] - half - box.y0) * px), x1 = Math.ceil((cell.b[1] + half - box.y0) * px), y0 = Math.floor((cell.b[0] - half - box.x0) * px), y1 = Math.ceil((cell.b[0] + half - box.x0) * px), c = L.colour(cell.v);
    for (let y = y0; y < y1; y++) for (let x = x0; x < x1; x++) put(x, y, c, L.alpha ?? 0.85);
  }
  for (const o of outlines) for (let k = 0; k < o.poly.length; k++) {
    const a = o.poly[k]!, e = o.poly[(k + 1) % o.poly.length]!, n = Math.ceil(Math.hypot(e[0] - a[0], e[1] - a[1]) * px * 1.5);
    for (let m = 0; m <= n; m++) { const t = m / Math.max(1, n); put(Math.round((a[1] + (e[1] - a[1]) * t - box.y0) * px), Math.round((a[0] + (e[0] - a[0]) * t - box.x0) * px), o.colour, 0.9); }
  }
  const png = Buffer.from(encodePng({ w, h, data })).toString('base64');
  return { w, h, png, box, px };
}

/** The map as an <img> with an SVG overlay of labelled markers. */
export function mapHtml(m: HeatMap, markers: Marker[] = [], caption = ''): string {
  const at = (b: V) => [(b[1] - m.box.y0) * m.px, (b[0] - m.box.x0) * m.px] as const;
  const dots = markers.map(k => { const [x, y] = at(k.b); return `<circle cx="${x.toFixed(1)}" cy="${y.toFixed(1)}" r="4" fill="none" stroke="${k.colour}" stroke-width="2"/><text x="${(x + 7).toFixed(1)}" y="${(y + 3).toFixed(1)}" font-size="11" fill="#1b1b1b" stroke="#fff" stroke-width="2.5" paint-order="stroke">${k.label}</text>`; }).join('');
  const grid: string[] = [];
  for (let x = Math.ceil(m.box.x0 / 10) * 10; x <= m.box.x1; x += 10) { const y = (x - m.box.x0) * m.px; grid.push(`<line x1="0" y1="${y}" x2="${m.w}" y2="${y}" stroke="#0003" stroke-width="0.5"/><text x="2" y="${y - 2}" font-size="9" fill="#0008">x ${x}</text>`); }
  for (let y = Math.ceil(m.box.y0 / 10) * 10; y <= m.box.y1; y += 10) { const x = (y - m.box.y0) * m.px; grid.push(`<line x1="${x}" y1="0" x2="${x}" y2="${m.h}" stroke="#0003" stroke-width="0.5"/><text x="${x + 2}" y="10" font-size="9" fill="#0008">y ${y}</text>`); }
  return `<figure class="map"><div style="position:relative;width:${m.w}px;height:${m.h}px"><img width="${m.w}" height="${m.h}" src="data:image/png;base64,${m.png}"><svg width="${m.w}" height="${m.h}" style="position:absolute;left:0;top:0">${grid.join('')}${dots}</svg></div><figcaption>${caption}</figcaption></figure>`;
}
