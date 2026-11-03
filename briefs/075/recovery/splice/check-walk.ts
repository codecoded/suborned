// Recovery verification only: reconstruct the walk in memory, preserve shipped outputs.
import { readFileSync, writeFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { loadSim } from '../../../../tools/geometry/sim';
import { loadPieces } from '../../../../tools/geometry/pieces';
import { buildVisible, kitIdsIn } from '../../../../tools/geometry/visible';
import { walkExport } from '../../../../tools/geometry/walk-export';
import { walkFileState } from '../../../../tools/game/walkfile';
const sha = (b: Uint8Array | string) => createHash('sha256').update(b).digest('hex');
const worldBytes = readFileSync('out/world/harbour-district.world.json');
const world = JSON.parse(worldBytes.toString()), worldSha = sha(worldBytes), sim = loadSim(world);
const corners = [[sim.window.x0, sim.window.y0], [sim.window.x1, sim.window.y0], [sim.window.x0, sim.window.y1], [sim.window.x1, sim.window.y1]].map(b => sim.frame.toPlan(b as [number, number]));
const win = { x0: Math.min(...corners.map(p => p[0])), x1: Math.max(...corners.map(p => p[0])), y0: Math.min(...corners.map(p => p[1])), y1: Math.max(...corners.map(p => p[1])) };
const pieces = loadPieces(kitIdsIn(world, win));
const vis = buildVisible(world, pieces, win);
const rebuilt = JSON.stringify(walkExport(sim, vis, sim.plan.id, worldSha));
const existing = readFileSync('out/world/harbour-district.walk.json'), walk = JSON.parse(existing.toString());
const result = {
  worldSource: world.source, planVersion: sim.plan.version, worldSha256: worldSha,
  walkSha256: sha(existing), rebuiltWalkSha256: sha(rebuilt),
  byteIdenticalToGenerator: existing.equals(Buffer.from(rebuilt)), freshness: walkFileState(process.cwd()),
  floorBytes: Buffer.from(walk.floor, 'base64').length, expectedFloorBytes: walk.nx * walk.ny * 2,
  kindBytes: Buffer.from(walk.kind, 'base64').length, expectedKindBytes: walk.nx * walk.ny,
  nx: walk.nx, ny: walk.ny, counts: walk.counts,
  countsMatchCellTotal: Object.values(walk.counts).reduce((a: number, b) => a + Number(b), 0) === walk.nx * walk.ny,
  frameMatchesPlan: JSON.stringify(walk.frame) === JSON.stringify(sim.plan.civic.blockToPlan),
  unmeasuredKitIds: [...vis.unmeasured.keys()],
};
writeFileSync('briefs/075/recovery/splice/walk-verification.json', JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
if (!result.byteIdenticalToGenerator || !result.freshness.fresh || result.floorBytes !== result.expectedFloorBytes || result.kindBytes !== result.expectedKindBytes || !result.countsMatchCellTotal || !result.frameMatchesPlan) process.exit(1);
