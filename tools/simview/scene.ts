// The Handover zone for the look tool (brief 081): the real world export and site, thinned to what a low-poly view draws. World metres, plan frame (x, y); nothing here is hand placed.
export type Ring = [number, number][];
export type Scene = {
  bbox: [number, number, number, number];
  frame: { origin: [number, number]; x: [number, number]; y: [number, number] };
  flats: [Ring, number, string][];
  walls: [Ring, number, number, string][];
  solids: [Ring, number, number, string][];
  /** roofs people walk under: drawn see-through, so nobody is hidden */
  canopies: [Ring, number, number, string][];
  boxes: [number, number, number, number, number, number, number, string][];
  trees: [number, number, number, number][];
  labels: [number, number, string, string][];
  cover: [number, number][];
  grid: { x0: number; y0: number; cell: number; nx: number; ny: number; rows: string[] };
  cameras: { id: string; at: [number, number]; facing: [number, number] }[];
};

/** Block centimetres to world metres (the walk file's frame: civic-block x south, y east). The world export's frame is the plan's. */
export const blockToWorld = (x: number, y: number, o: [number, number] = [-130, -230]): [number, number] => [o[0] + y / 100, o[1] - x / 100];

const FLOOR: Record<string, string> = { 'road-asphalt': '#5d636c', bay: '#747a80', foot: '#e3d8bf', pavement: '#e6dcc6', flags: '#e6dcc6', square: '#efe6d4', civic: '#ece2cc', residential: '#e2d8c4', mixed: '#e2d8c4', commercial: '#e2d8c4', open: '#d8e0b8', park: '#9ccb8d', limestone: '#ece2cc', terrazzo: '#efe6d4', 'bed-kerb': '#b9b09a' };
const WAY: Record<string, string> = { boulevard: '#efd9c2', southside: '#e9cdb4', gardens: '#d5e2cd', harbour: '#cfe0e0', 'soft-rose': '#efccc6', 'soft-celadon': '#cfe3d3', 'soft-apricot': '#f2d3ae', 'soft-ochre': '#e8d49a' };
const CIVIC: Record<string, string> = { limestone: '#f3e6cf', trim: '#d8c9a8', column: '#b7bcc2', enamel: '#6fbdb6', park: '#7fbf78', 'bed-kerb': '#b9b09a' };
// props stay neutral: the sides own the saturated colours on this page (readable > rich)
const PROP: Record<string, string> = { lamp: '#8a8d90', bench: '#a39a88', bin: '#7d7f80', 'mast-camera': '#d24b4b', 'planter-bed': '#8fb487', 'crossing-dots': '#fff2a8', 'drinking-fountain': '#9fc4cc' };
const SKIP = new Set(['hanging-basket', 'window-box']);

export function buildScene(world: any, site: any, bbox: [number, number, number, number] = [-245, -75, -375, -150]): Scene {
  const [x0, x1, y0, y1] = bbox;
  const inb = (p: number[]) => p[0]! >= x0 && p[0]! <= x1 && p[1]! >= y0 && p[1]! <= y1;
  const near = (r: number[][]) => r.some(inb);
  const round = (r: number[][]): Ring => r.map(p => [+p[0]!.toFixed(2), +p[1]!.toFixed(2)]);
  const s: Scene = { bbox, frame: { origin: [-130, -230], x: [0, -1], y: [1, 0] }, flats: [], walls: [], solids: [], canopies: [], boxes: [], trees: [], labels: [], cover: [], grid: { x0: site.grid.x0, y0: site.grid.y0, cell: site.grid.cell, nx: site.grid.nx, ny: site.grid.ny, rows: site.grid.rows }, cameras: [] };
  const g = world.ground;
  // the land under it all, then the plots' slabs, then the roads as ribbons (2 m points in the export: long straights joined), then bays and kerb returns
  s.flats.push([[[x0, y0], [x1, y0], [x1, y1], [x0, y1]], -0.03, '#d9cfb9']);
  for (const p of g.plots) if (near(p.ring)) s.solids.push([round(p.ring), p.base ?? 0, Math.max(0.05, p.height ?? 0.18), FLOOR[p.surface] ?? '#e2d8c4']);
  for (const list of [g.roads, g.foot]) for (const r of list) {
    const line: number[][] = r.line; if (!near(line)) continue;
    let a = line[0]!;
    const out: number[][] = [a];
    for (let i = 1; i + 1 < line.length; i++) {
      const b = line[i]!, c = line[i + 1]!;
      if (Math.abs((b[0]! - a[0]!) * (c[1]! - b[1]!) - (b[1]! - a[1]!) * (c[0]! - b[0]!)) > 1e-6) { out.push(b); a = b; }
    }
    out.push(line[line.length - 1]!);
    const hw = r.width / 2, col = FLOOR[r.surface] ?? '#5d636c', z = (r.level ?? 0) + 0.01;
    for (let i = 0; i + 1 < out.length; i++) {
      const p = out[i]!, q = out[i + 1]!, dx = q[0]! - p[0]!, dy = q[1]! - p[1]!, n = Math.hypot(dx, dy); if (!n) continue;
      const ux = dx / n, uy = dy / n, nx = -uy * hw, ny = ux * hw;
      s.flats.push([round([[p[0]! + nx, p[1]! + ny], [p[0]! - nx, p[1]! - ny], [q[0]! - nx, q[1]! - ny], [q[0]! + nx, q[1]! + ny]]), z, col]);
    }
  }
  for (const f of g.flats) if (f.id !== 'ground/land' && near(f.ring) && f.surface !== 'beyond') s.flats.push([round(f.ring), (f.level ?? 0) + 0.015, FLOOR[f.surface] ?? '#d9cfb9']);
  for (const d of g.drives) if (near(d.ring)) s.flats.push([round(d.ring), (d.level ?? 0) + 0.015, FLOOR[d.surface] ?? '#747a80']);
  for (const p of g.pieces) {
    if (!inb(p.at)) continue;
    if (p.kind === 'kerb' || p.kind === 'kerb-return') s.boxes.push([p.at[0], p.at[1], p.z ?? 0, p.size[0], p.size[1], p.size[2], p.yaw ?? 0, '#cfc6ae']);
    else if (p.kind === 'road-lane-dash') s.boxes.push([p.at[0], p.at[1], (p.z ?? 0) + 0.015, p.size[0], p.size[1], 0.01, p.yaw ?? 0, '#e8e4d0']);
  }
  // masses
  for (const b of world.buildings) if (near(b.ring)) s.walls.push([round(b.ring), b.base ?? 0, b.height, WAY[b.colourway] ?? '#e9cdb4']);
  for (const m of world.megastructures) for (const p of m.parts ?? []) if (p.ring && near(p.ring)) s.walls.push([round(p.ring), p.base ?? 0, p.height ?? 1, '#e2d4bc']);
  for (const r of world.roofs) { if (!near([r.c])) continue; for (const p of r.parts ?? []) if (p.shape === 'box') s.boxes.push([p.c[0], p.c[1], p.base, p.size[0], p.size[1], p.size[2], p.yaw ?? 0, '#d8ccb4']); }
  for (const c of world.civic) {
    if (!c.ring || !near(c.ring)) continue;
    if (/\/(planting|interior)$/.test(c.id)) continue;
    if (/pavilion$|parasol$|\/terrace$/.test(c.id)) { const top = (c.base ?? 0) + (c.height ?? 0.5); s.canopies.push([round(c.ring), Math.max(c.base ?? 0, top - 0.25), top, CIVIC[c.surface] ?? '#d8c9a8']); continue; }
    const col = /cyp/.test(c.id) ? '#5aa860' : c.id === 'civic/cafe' || c.id === 'civic/cafe/ground' ? '#f2a48a' : /civic-office|facade|office-/.test(c.id) ? '#f6ead4' : CIVIC[c.surface] ?? '#d8c9a8';
    (/^civic\/(cafe|cafe\/ground|civic-office.*|civic-facade.*|office-.*)$/.test(c.id) ? s.walls : s.solids).push([round(c.ring), c.base ?? 0, c.height ?? 0.5, col]);
  }
  for (const it of world.street) {
    if (!inb(it.at) || SKIP.has(it.kind)) continue;
    if (it.kind === 'tree') { s.trees.push([it.at[0], it.at[1], it.z ?? 0, it.size?.[2] ?? 8]); continue; }
    s.boxes.push([it.at[0], it.at[1], it.z ?? 0, it.size[0], it.size[1], it.size[2], it.yaw ?? 0, PROP[it.kind] ?? '#b3ad9f']);
  }
  // labels: the doors the mission turns on, the buildings, the site's places
  for (const e of world.takedown?.entrances ?? []) s.labels.push([e.at[0], e.at[1], e.name, 'door']);
  s.labels.push([-159, -214, 'Café', 'big'], [-157, -296, 'Records Office', 'big'], [-160, -258, 'Records Square', 'big']);
  const placeName: Record<string, string> = { cabinet: 'Cabinet' };
  for (const [id, p] of Object.entries<any>(site.places)) {
    if (!p.at) continue;
    const name = placeName[id] ?? (id.startsWith('d:') ? id.slice(2) : ''); if (!name) continue;
    const [wx, wy] = blockToWorld(p.at[0], p.at[1]); s.labels.push([+wx.toFixed(2), +wy.toFixed(2), name, 'place']);
  }
  for (const c of site.cameras) { const a = blockToWorld(c.at[0], c.at[1]); s.cameras.push({ id: c.id, at: a, facing: [c.facing[1], -c.facing[0]] }); }
  for (const p of site.cover?.points ?? []) s.cover.push(blockToWorld(p.at[0], p.at[1]).map(v => +v.toFixed(2)) as [number, number]);
  return s;
}
