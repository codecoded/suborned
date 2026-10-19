// The drive grid (brief 023, engine 0.8.2, Keel; Aster 2026-10-19): where a car may be, in the walk grid's window and cells (sim/data/handover.site.json
// `drive`, block frame). A cell is drivable when its centre lies on a carriageway (within w/2 of a plan road's centreline: plan.roads w is the
// carriageway's width, as getaway.ts SX-1 reads it), inside a kerb bay of the civic block (civic-block.json bays) or inside a drive (a vehicle
// crossing from a carriageway to a bay or yard: `drives`, block frame polygons). Pavements, yards and squares are not. The engine routes every
// moving car on it (rulebook car.drive): the turned driver reaches the service bay by the road and the drive, never across the pavement.
// Brief 023 (Motive 2026-10-19, rulebook car.pace): each drivable cell says its surface, 'r' a carriageway (driven at car.pace.road) or 'y' a drive
// or a bay (car.pace.yard; a bay or drive wins where it overlaps a carriageway), '#' not drivable. `lane` (cm) is half the city's lane width
// (harbour-district.life.json traffic.laneWidth): a car on a carriageway keeps its centre that far from the edge where it can (the engine's car route).
// Deterministic: the same plan and block give the same rows.
import { toPlanFn } from '../plan/civic';
import { inPoly, segDist, type V } from '../plan/geom';
import type { Derived, Plan } from '../plan/plan';
import { CELL, WINDOW, type Window } from '../plan/walk-grid';

export type Drive = { id: string; polygon: V[] };
export type DriveGrid = { x0: number; y0: number; cell: number; nx: number; ny: number; lane: number; rows: string[]; src: string };

const cm = (m: number) => Math.round(m * 100);

/** The drive grid over `win` (block frame, m): 'r' carriageway, 'y' drive or bay, '#' not drivable. `bays` and `drives` are block-frame
 *  polygons; `laneWidth` (m) is the city's lane width (life.json traffic.laneWidth). */
export function driveGrid(plan: Plan, d: Derived, bays: { id: string; polygon: V[] }[], drives: Drive[], laneWidth: number, win: Window = WINDOW, cell = CELL): DriveGrid {
  if (!(laneWidth > 0)) throw new Error('drive grid: the lane width must be positive (life.json traffic.laneWidth)');
  const toPlan = toPlanFn(plan.civic);
  const nx = Math.round((win.x1 - win.x0) / cell), ny = Math.round((win.y1 - win.y0) / cell);
  const roads = d.roads.map((r) => ({ w2: r.w / 2, line: r.line, x0: Math.min(...r.line.map((p) => p[0])) - r.w, x1: Math.max(...r.line.map((p) => p[0])) + r.w, y0: Math.min(...r.line.map((p) => p[1])) - r.w, y1: Math.max(...r.line.map((p) => p[1])) + r.w }));
  const onRoad = (p: V) => roads.some((r) => p[0] >= r.x0 && p[0] <= r.x1 && p[1] >= r.y0 && p[1] <= r.y1 && r.line.slice(1).some((b, i) => segDist(p, r.line[i]!, b) <= r.w2));
  const surfaces = [...bays.map((b) => b.polygon), ...drives.map((x) => x.polygon)];
  const rows: string[] = [];
  for (let i = 0; i < nx; i++) {
    let row = '';
    for (let j = 0; j < ny; j++) {
      const q: V = [win.x0 + (i + 0.5) * cell, win.y0 + (j + 0.5) * cell];
      row += surfaces.some((s) => inPoly(q, s)) ? 'y' : onRoad(toPlan(q)) ? 'r' : '#';
    }
    rows.push(row);
  }
  return {
    x0: cm(win.x0), y0: cm(win.y0), cell: cm(cell), nx, ny, lane: cm(laneWidth / 2), rows,
    src: `where a car may be (rulebook car.drive) and at what pace (car.pace): a cell is drivable when its centre lies on a carriageway ('r', within w/2 of a plan road's centreline), or in a civic-block bay (${bays.map((b) => b.id).join(', ') || 'none'}) or on a drive (${drives.map((x) => x.id).join(', ') || 'none'}) ('y', which wins over a carriageway); lane: half of life.json traffic.laneWidth ${laneWidth} m; tools/sim/drive.ts`,
  };
}

/** The drive grid's free cells as a predicate, block frame, m. */
export const driveFree = (g: DriveGrid) => (q: V): boolean => {
  const i = Math.floor((cm(q[0]) - g.x0) / g.cell), j = Math.floor((cm(q[1]) - g.y0) / g.cell);
  return i >= 0 && j >= 0 && i < g.nx && j < g.ny && g.rows[i]![j] !== '#';
};
