// SVG drawings of the plan: the district sheet (1:2500) and the Records Precinct inset (1:500). One SVG unit = 1 m;
// layers are <g data-layer> groups the sheet's toggles show and hide. Colours live in the sheet's CSS classes.
import { type V, add, centroid, mul, norm, sub, left, dist } from './geom';
import type { Derived, Plan, PathGeo } from './plan';
import type { Civic } from './civic';
import type { Report } from './checks';

const n = (x: number) => +x.toFixed(2);
const esc = (s: string) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;');

class Pen {
  constructor(readonly x0: number, readonly x1: number, readonly y0: number, readonly y1: number, readonly m: number) {}
  get w() { return this.x1 - this.x0 + 2 * this.m; }
  get h() { return this.y1 - this.y0 + 2 * this.m; }
  /** Content clipped to the drawing frame. */
  clip = (id: string, body: string) => `<defs><clipPath id="clip-${id}"><rect x="${this.m}" y="${this.m}" width="${n(this.x1 - this.x0)}" height="${n(this.y1 - this.y0)}"/></clipPath></defs><g clip-path="url(#clip-${id})">${body}</g>`;
  X = (x: number) => n(x - this.x0 + this.m);
  Y = (y: number) => n(this.y1 - y + this.m);
  pt = (p: V) => `${this.X(p[0])},${this.Y(p[1])}`;
  d = (pts: V[], close = false) => 'M' + pts.map(this.pt).join('L') + (close ? 'Z' : '');
  poly = (pts: V[], cls: string, extra = '') => `<path class="${cls}" d="${this.d(pts, true)}"${extra}/>`;
  line = (pts: V[], cls: string, extra = '') => `<path class="${cls}" d="${this.d(pts)}"${extra}/>`;
  circle = (c: V, r: number, cls: string) => `<circle class="${cls}" cx="${this.X(c[0])}" cy="${this.Y(c[1])}" r="${n(r)}"/>`;
  /** Text at p, rotated to run along direction dir (kept upright). */
  text(p: V, s: string, cls: string, dir: V = [1, 0], size?: number) {
    let a = (-Math.atan2(dir[1], dir[0]) * 180) / Math.PI;
    if (a > 90) a -= 180; if (a < -90) a += 180;
    const fs = size ? ` font-size="${n(size)}"` : '';
    return `<text class="${cls}"${fs} x="${this.X(p[0])}" y="${this.Y(p[1])}" transform="rotate(${n(a)} ${this.X(p[0])} ${this.Y(p[1])})">${esc(s)}</text>`;
  }
  /** A rect of `along` × `across` centred at c, long side along dir, shifted sideways by `side`. */
  box(c: V, dir: V, along: number, across: number, side = 0): V[] {
    const u = norm(dir), v = left(u), o = add(c, mul(v, side));
    return [[-1, -1], [1, -1], [1, 1], [-1, 1]].map(([a, b]) => add(o, add(mul(u, (a * along) / 2), mul(v, (b * across) / 2))));
  }
  /** CAD dimension: a line offset from a→b with slash ticks, extension lines and the measure. */
  dim(a: V, b: V, off: number, size = 2.4) {
    const u = norm(sub(b, a)), nv = left(u), sg = off < 0 ? -1 : 1, A = add(a, mul(nv, off)), B = add(b, mul(nv, off)), t = mul(norm(add(u, nv)), size * .45);
    const ext = (p: V, q: V) => (off ? this.line([add(p, mul(nv, sg * .8)), add(q, mul(nv, sg * 1.2))], 'dim') : '');
    return `<g class="dimg">${ext(a, A)}${ext(b, B)}${this.line([A, B], 'dim')}${this.line([sub(A, t), add(A, t)], 'dim')}${this.line([sub(B, t), add(B, t)], 'dim')}` +
      this.text(add(mul(add(A, B), .5), mul(nv, sg * size * .5)), dist(a, b).toFixed(1), 'dimt', u, size) + '</g>';
  }
}

const layer = (id: string, body: string) => `<g data-layer="${id}">${body}</g>`;
const at = (path: PathGeo, s: number) => path.samples.reduce((b, x) => (Math.abs(x.s - s) < Math.abs(b.s - s) ? x : b));

function roads(P: Pen, plan: Plan, d: Derived) {
  const order = ['access', 'district', 'primary'] as const;
  const sorted = [...d.roads].sort((a, b) => order.indexOf(a.class) - order.indexOf(b.class));
  const kerb = sorted.map(r => P.line(r.line, 'kerb', ` stroke-width="${r.w + 1.6}"`)).join('');
  const foot = plan.foot.map(w => P.line(w.pts, `foot-k`, ` stroke-width="${w.w + 1.2}"`)).join('') + plan.foot.map(w => P.line(w.pts, `foot ${w.kind}`, ` stroke-width="${w.w}"`)).join('');
  const fill = sorted.map(r => P.line(r.line, `road ${r.class}`, ` stroke-width="${r.w}"`)).join('');
  const res = d.roads.filter(r => r.reservation).map(r => P.line(r.line, 'reservation', ` stroke-width="${r.reservation}"`)).join('');
  const c = plan.circus;
  const island = P.circle(c.c, c.ring[0] + .8, 'kerb-c') + P.circle(c.c, c.island, 'island') + P.circle(c.c, c.garden, 'garden');
  const centre = d.roads.filter(r => r.class !== 'access').map(r => P.line(r.line, 'cl')).join('');
  const med = d.zones.filter(z => z.kind === 'median').map(z => P.poly(z.poly!, 'median')).join('');
  return layer('roads', kerb + foot + fill + res + med + island) + layer('centrelines', centre);
}

function rail(P: Pen, d: Derived, plan: Plan, kind: 'tram' | 'sky', R: Report, size = 7) {
  const paths = kind === 'tram' ? d.tram : d.sky, half = kind === 'tram' ? plan.standards.tram.trackCentres / 2 : plan.standards.skyrail.beamCentres / 2;
  let out = '';
  for (const p of paths) {
    const pts = p.samples.filter((_, i) => i % 4 === 0 || i === p.samples.length - 1);
    if (p.double) for (const s of [-1, 1]) out += P.line(pts.map(x => add(x.p, mul(left(x.dir), s * half))), kind);
    else out += P.line(pts.map(x => x.p), kind);
  }
  if (kind === 'sky') out += d.pylons.map(p => P.circle(p.p, 1.7, 'pylon')).join('');
  const lines = kind === 'tram' ? plan.tram : plan.skyrail;
  for (const l of lines) for (const s of (kind === 'tram' ? l.stops : l.stations) ?? []) {
    const path = paths.find(p => p.line === l.id && p.id === s.path)!, x = at(path, R[kind === 'tram' ? 'stops' : 'stations'].find(q => q.line === l.id && q.id === s.id).chainage);
    if (kind === 'tram') for (const side of [-1, 1]) out += P.poly(P.box(x.p, x.dir, plan.standards.tram.platform, 3, side * (half + 3)), 'platform');
    else out += P.poly(P.box(x.p, x.dir, s.length ?? 50, 2 * half + 8), 'station');
    const lab = kind === 'tram' ? add(x.p, mul(left(x.dir), -(half + size + 1))) : add(x.p, [0, half + size * 1.7]);
    out += P.text(lab, `${kind === 'tram' ? '◆ ' : '▲ '}${s.name}`, `lab ${kind}-lab`, kind === 'tram' ? [1, 0] : x.dir, size);
  }
  return layer(kind, out);
}

function gates(P: Pen, plan: Plan, size: number, labels: boolean) {
  return layer('gates', plan.gates.map(g => {
    const p = plan.nodes[g.node], road = plan.roads.find(r => r.id === g.road)!, i = road.path.indexOf(g.node);
    const q = plan.nodes[road.path[i === 0 ? 1 : i - 1]], u = norm(sub(q, p)), c = add(p, mul(u, size * 2.2));
    const dia = [[0, 1], [1, 0], [0, -1], [-1, 0]].map(([a, b]) => add(c, [a * size, b * size]) as V);
    return P.poly(dia, `gate${g.area === 'records' ? ' seal' : ''}`) + (labels ? P.text(add(c, [size * 1.4, -size * .4]), g.name, 'lab gate-lab', [1, 0], size * 1.2) : '');
  }).join(''));
}

function frame(P: Pen, grid: number, gl: boolean) {
  let g = '';
  for (let x = Math.ceil(P.x0 / grid) * grid; x <= P.x1; x += grid) g += P.line([[x, P.y0], [x, P.y1]], 'grid');
  for (let y = Math.ceil(P.y0 / grid) * grid; y <= P.y1; y += grid) g += P.line([[P.x0, y], [P.x1, y]], 'grid');
  if (gl) {
    const cols = Math.ceil((P.x1 - P.x0) / grid), rows = Math.ceil((P.y1 - P.y0) / grid);
    for (let i = 0; i < cols; i++) { const x = P.x0 + (i + .5) * grid, t = String.fromCharCode(65 + i); g += P.text([x, P.y1 + P.m * .45], t, 'gref', [1, 0], 11) + P.text([x, P.y0 - P.m * .75], t, 'gref', [1, 0], 11); }
    for (let j = 0; j < rows; j++) { const y = P.y1 - (j + .5) * grid; g += P.text([P.x0 - P.m * .5, y - 4], `${j + 1}`, 'gref', [1, 0], 11) + P.text([P.x1 + P.m * .5, y - 4], `${j + 1}`, 'gref', [1, 0], 11); }
  }
  const border = `<rect class="border" x="${P.m}" y="${P.m}" width="${n(P.x1 - P.x0)}" height="${n(P.y1 - P.y0)}"/>`;
  return layer('grid', g) + border;
}

function northScale(P: Pen, c: V, bar: number[], size: number) {
  const [x, y] = c, k = size;
  const arrow = `<g class="north">${P.poly([[x, y + 3 * k], [x - k, y - k], [x, y], [x + k, y - k]], 'n-arrow')}${P.text([x, y + 3.6 * k], 'N', 'n-lab', [1, 0], k * 1.6)}</g>`;
  const bx = c[0] - bar[bar.length - 1] - 4 * k, by = c[1] - 4 * k;
  let s = '';
  bar.slice(1).forEach((b, i) => s += P.poly([[bx + bar[i], by], [bx + b, by], [bx + b, by + k * .5], [bx + bar[i], by + k * .5]], i % 2 ? 'sb-w' : 'sb-b'));
  bar.forEach(b => s += P.text([bx + b, by + k * 1.1], `${b}`, 'sb-lab', [1, 0], k * .9));
  s += P.text([bx + bar[bar.length - 1] / 2, by - k * 1.3], 'metres', 'sb-lab', [1, 0], k * .9);
  return layer('frame', arrow + s);
}

export function district(plan: Plan, d: Derived, R: Report) {
  const s = plan.sheet, P = new Pen(s.x[0], s.x[1], s.y[0], s.y[1], 34);
  const base = layer('base', plan.water.map(w => P.poly(w, 'water')).join('') + plan.quay.map(q => P.poly(q, 'quay')).join('') + plan.beyond.map(b => P.poly(b, 'beyond')).join(''));
  const plots = layer('plots', d.plots.map(p => P.poly(p.polyV, `plot ${p.use}`, ` data-id="${p.id}"`)).join(''));
  const areas = layer('areas', d.areas.map(a => P.poly(a.polyV, 'area')).join(''));
  const areaNames = d.areas.map(a => P.text(a.label ?? centroid(a.polyV), a.name.toUpperCase(), 'area-lab', [1, 0], a.size ?? 8)).join('');
  const marks = layer('landmarks', plan.landmarks.map(l => P.circle(l.c, l.r, `mark ${l.id}`) + (l.id === 'monument' ? '' : P.text(add(l.c, [0, -3]), l.name, 'lab mark-lab', [1, 0], 8))).join(''));
  const names = layer('labels', areaNames + d.roads.filter(r => !r.gyratory).map(r => {
    const g = r.segs.filter(x => x.kind === 'line').reduce((b, x) => (x.len > b.len ? x : b)), a = g.a, b = g.b;
    const mid = mul(add(a, b), .5), off = r.class === 'access' ? 0 : r.w / 2 + 5;
    const lab = r.id === 'circus-road' ? add(mid, [60, 0]) : r.id === 'boulevard' ? add(plan.nodes.m_boulevard, [0, -150]) : mid;
    return P.text(add(lab, mul(left(norm(sub(b, a))), -off)), r.name, `lab road-lab ${r.class}`, sub(b, a), r.class === 'primary' ? 9 : r.class === 'district' ? 7.5 : 5);
  }).join('') + plan.foot.map(w => P.text(add(mul(add(w.pts[0], w.pts[1]), .5), [w.kind === 'mall' ? 30 : 0, w.kind === 'mall' ? -2.5 : 40]), w.name, 'lab foot-lab', sub(w.pts[1], w.pts[0]), w.kind === 'mall' ? 7 : 5)).join('')
    + P.text([0, 0], 'CROWN CIRCUS', 'lab circus-lab', [1, 0], 4.5)
    + d.plots.filter(p => p.name && !p.civic && !d.areas.some(a => a.name === p.name)).map(p => P.text(centroid(p.polyV), p.name!, 'lab plot-lab', [1, 0], 6)).join('')
    + plan.links.map(l => P.text(add(plan.nodes[l.node], l.node === 'off_west' || l.node === 'off_circus_west' ? [24, 14] : l.node === 'off_seagates' ? [0, -16] : [22, 22]), `→ ${l.to}`, 'lab link-lab', [1, 0], 7)).join(''));
  const civic = layer('civic', d.plots.filter(p => p.civic).map(p => P.poly(p.polyV, 'civic-hi')).join(''));
  const walks = layer('walks', R.walks.map(w => P.line(w.path, 'walk-route') + P.text(w.path[w.path.length - 1], `${Math.round(w.m)} m`, 'lab walk-lab', [1, 0], 5)).join(''));
  const body = P.clip('district', base + plots + areas + roads(P, plan, d) + civic + rail(P, d, plan, 'tram', R) + rail(P, d, plan, 'sky', R) + walks + gates(P, plan, 5, false) + marks + names)
    + frame(P, s.grid, true) + northScale(P, [700, 240], [0, 50, 100, 200], 10);
  return `<svg id="district" viewBox="0 0 ${n(P.w)} ${n(P.h)}" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="${esc(plan.name)} master plan">${body}</svg>`;
}

export function inset(plan: Plan, d: Derived, civ: Civic, R: Report) {
  const s = plan.sheet.inset, P = new Pen(s.x[0], s.x[1], s.y[0], s.y[1], 10);
  const within = (pts: V[]) => pts.some(p => p[0] > s.x[0] - 60 && p[0] < s.x[1] + 60 && p[1] > s.y[0] - 60 && p[1] < s.y[1] + 60);
  const plots = layer('plots', d.plots.filter(p => within(p.polyV)).map(p => P.poly(p.polyV, `plot ${p.use}`)).join('')
    + civ.blocks.map(b => P.poly(b.outer, 'pavement') + P.poly(b.core, `core ${b.id}`)).join(''));
  const detail = layer('civic', civ.buildings.map(b => P.poly(b.poly, `bldg ${b.id}`)).join('')
    + civ.structures.map(x => P.poly(x.poly, 'struct')).join('')
    + civ.objects.map(o => o.kind === 'fountain' ? P.circle(centroid(o.poly), 3.3, 'fountain') : P.poly(o.poly, `obj ${o.kind}`)).join('')
    + civ.bays.map(b => P.poly(b.poly, 'bay')).join('')
    + P.poly(civ.crossing, 'crossing')
    + civ.buildings.map(b => P.text(add(centroid(b.poly), [0, -1]), b.id === 'cafe' ? 'CAFÉ' : 'RECORDS OFFICE', 'bldg-lab', [1, 0], 2.6)).join(''));
  const pts = civ.walkFrom.filter((p, i, A) => A.findIndex(q => dist(q.p, p.p) < .3) === i);
  const points = layer('points', pts.map((p, i) => P.line([add(p.p, [-1, 0]), add(p.p, [1, 0])], 'pt') + P.line([add(p.p, [0, -1]), add(p.p, [0, 1])], 'pt')
    + P.text(add(p.p, [1.4, .5]), `${i + 1}`, 'pt-lab', [1, 0], 1.8)).join(''));
  const sq = civ.blocks.find(b => b.id === 'square')!.outer, of = civ.blocks.find(b => b.id === 'office')!.outer;
  const xs = (b: V[]) => [Math.min(...b.map(p => p[0])), Math.max(...b.map(p => p[0]))], ys = (b: V[]) => [Math.min(...b.map(p => p[1])), Math.max(...b.map(p => p[1]))];
  const [sx0, sx1] = xs(sq), [sy0, sy1] = ys(sq), [ox0, ox1] = xs(of), [oy0, oy1] = ys(of);
  const dims = layer('dims', P.dim([sx0, sy1], [sx1, sy1], 8) + P.dim([sx0, sy0], [sx0, sy1], 10) + P.dim([ox0, oy0], [ox1, oy0], -16) + P.dim([ox1, oy0], [ox1, oy1], -10)
    + P.dim([sx0, oy1], [sx0, sy0], 22) + P.dim([sx1, sy1 - 20], [sx1 + 12, sy1 - 20], 0) + P.dim([sx0 - 12, sy0 - 40], [sx0, sy0 - 40], 0));
  const lab = (p: V, t: string, dir: V = [1, 0], z = 3) => P.text(p, t, 'lab road-lab access', dir, z);
  const names = layer('labels', lab([-186, -350], 'ROCKET LANE', [0, 1]) + lab([-60, -350], 'SATELLITE STREET', [0, 1]) + lab([-100, -199], 'STARLINE STREET')
    + lab([-132, -232], 'GARDEN ROW', [0, 1]) + lab([-95, -268.5], 'ORBIT WALK') + lab([-120, -292], 'PYLON ROW', [0, 1]) + lab([-150, -317], 'APRON ROAD')
    + lab([-100, -411], 'MARKET STREET', [1, 0], 4) + lab([8, -300], 'THE BOULEVARD', [0, 1], 4) + lab([-160, -184], 'GARDEN WALK', [0, 1], 2.6)
    + P.text([-123, -380], 'ORBIT GARDENS', 'lab plot-lab', [1, 0], 3.5) + P.text([-96, -232], 'GARDEN ROW EAST', 'lab plot-lab', [1, 0], 2.6));
  const body = P.clip('inset', plots + roads(P, plan, d) + detail + rail(P, d, plan, 'tram', R, 3) + gates(P, plan, 2.4, true) + points + dims + names) + frame(P, 10, false)
    + northScale(P, [-200, -192], [0, 5, 10, 20], 2.4);
  const legend = pts.map(p => `<li>${esc(p.name)}</li>`).join('');
  return { svg: `<svg id="inset" viewBox="0 0 ${n(P.w)} ${n(P.h)}" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="Records Precinct">${body}</svg>`, legend };
}
