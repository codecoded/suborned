// The walk surface export (brief 055, Splice): what the host needs to stand a person on the floor that is drawn, and what Keel needs to hold the walk grid to the drawn solids.
// out/world/<plan id>.walk.json, `suborned.walk/1`. Block frame (x south, y east, metres), the civic block's site window, 0.25 m cells, row i along x, column j along y.
//   floor : int16 little-endian, centimetres above the carriageway datum, base64: the floor a walker stands on at the cell (the ground layer where nothing is drawn, the top of the
//           stair, landing or kerb stone where something is); `cells` of them, index i * ny + j
//   kind  : uint8 base64, same index: 0 open ground, 1 a floor above the ground (stair, landing, platform, kerb stone: walkable by steps), 2 solid (a wall to a walker: more than a step
//           up with no way up), 3 under a ceiling lower than 1.8 m, 4 a solid whose top is above head height at the cell
// Deterministic from the world export and the kit meshes; the host reads `floor` instead of baking heights from the plot slabs (PlayInstall.Heights).
import { FLUSH, HEAD } from './raster';
import type { Sim } from './sim';
import type { Visible } from './visible';

export type WalkFile = { schema: 'suborned.walk/1'; plan: string; world: string; window: { x0: number; y0: number; x1: number; y1: number }; cell: number; nx: number; ny: number; frame: { origin: [number, number]; x: [number, number]; y: [number, number] }; floor: string; kind: string; counts: Record<string, number> };

export function walkExport(sim: Sim, vis: Visible, planId: string, worldSha: string): WalkFile {
  const w = sim.window, cell = 0.25, nx = Math.round((w.x1 - w.x0) / cell), ny = Math.round((w.y1 - w.y0) / cell), R = vis.R;
  const floor = new Int16Array(nx * ny), kind = new Uint8Array(nx * ny), counts: Record<string, number> = { open: 0, raised: 0, solid: 0, lowCeiling: 0 };
  for (let i = 0; i < nx; i++) for (let j = 0; j < ny; j++) {
    const [ci, cj] = R.cellOf(sim.frame.toPlan([w.x0 + (i + 0.5) * cell, w.y0 + (j + 0.5) * cell])), k = i * ny + j;
    if (!R.inside(ci, cj)) { floor[k] = 0; kind[k] = 0; continue; }
    const c = R.column(ci, cj), rise = c.top - c.ground;
    if (c.walkable) { floor[k] = Math.round(c.top * 100); kind[k] = rise > FLUSH ? 1 : 0; counts[rise > FLUSH ? 'raised' : 'open']!++; }
    else { floor[k] = Math.round(c.ground * 100); kind[k] = rise > FLUSH ? 2 : c.roof - c.top < HEAD ? 3 : 0; counts[kind[k] === 2 ? 'solid' : kind[k] === 3 ? 'lowCeiling' : 'open']!++; }
  }
  const b64 = (a: Int16Array | Uint8Array) => Buffer.from(a.buffer, a.byteOffset, a.byteLength).toString('base64');
  const o = sim.plan.civic.blockToPlan;
  return { schema: 'suborned.walk/1', plan: planId, world: worldSha, window: { x0: w.x0, y0: w.y0, x1: w.x1, y1: w.y1 }, cell, nx, ny, frame: { origin: o.origin, x: o.x, y: o.y }, floor: b64(floor), kind: b64(kind), counts };
}

/** The host's floor under a block-frame point when it reads a walk file (what PlayInstall/SiteFrame would do with `floor`). */
export function walkFloorAt(f: WalkFile) {
  const buf = Buffer.from(f.floor, 'base64'), floor = new Int16Array(buf.buffer.slice(buf.byteOffset, buf.byteOffset + buf.byteLength));
  return (b: [number, number]) => { const i = Math.floor((b[0] - f.window.x0) / f.cell), j = Math.floor((b[1] - f.window.y0) / f.cell); return i < 0 || j < 0 || i >= f.nx || j >= f.ny ? 0 : floor[i * f.ny + j]! / 100; };
}
