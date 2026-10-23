// Retired 2026-10-22 (Splice): Sky said "can try" and the rows and keys below are now the committed ones (contracts/world/kit.json adopted, the plan's roads.shoulder and roads.pinstripe), so on the committed
// files every helper here changes nothing. Kept for the board tools (Vela). The proposal kit and plan keys the road finish was developed against (brief 028, Vela, 2026-10-22): contracts/world/kit.json with the ribbon, lozenge and seam candidates read as adopted and the
// four new pieces (the pearl shoulder, its two arcs, the pavement pinstripe; sizes and pivots from tools/decokit/road_ribbon.py's build, out/vela028-road/pieces/decokit_manifest.json) appended,
// and the plan's standards.streetscape.roads with the `shoulder` and `pinstripe` keys Datum has not written yet. Nothing here touches contracts/; Splice adopts the rows (kit catalogue, `bun run kit`)
// and Datum the keys (draft in briefs/028/road/plan-keys.json) when Sky agrees the road.
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import type { Plan } from '../plan/plan';

const root = resolve(import.meta.dir, '../..');
export const json = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));

const row = (id: string, size: number[], off: number[] | null, tris: number, materials: string[], use: string[]) => ({
  id, category: 'Ground', size, pivot: off ? 'origin-ground' : 'footprint-centre-ground', ...(off ? { pivotOffset: off } : {}), front: '+Z', tris, materials, lodCategory: 'street', files: [], tags: { use, scale: [1, 1], squash: 1, wave: 2 }, status: 'adopted', source: 'generated',
});
export const NEW_ROWS = [
  row('RoadShoulder_Straight_6m', [6, 0.001, 0.655], [0, -0.588], 12, ['DK_Ground_RoadPearl', 'DK_Chrome'], ['ground:road-shoulder']),
  row('RoadShoulder_Arc90_R4', [3.8126, 0.001, 3.8126], [-1.835, -2.166], 60, ['DK_Ground_RoadPearl', 'DK_Chrome'], ['ground:road-shoulder']),
  row('RoadShoulder_Arc90_R8', [7.7917, 0.001, 7.7917], [-3.845, -4.156], 84, ['DK_Ground_RoadPearl', 'DK_Chrome'], ['ground:road-shoulder']),
  row('PaveEdge_Pinstripe_6m', [6, 0, 0.04], null, 2, ['DK_Brass_Civic'], ['ground:pave-pinstripe']),
];

/** The committed kit with the road candidates adopted and the new pieces added, as an object. */
export function proposalKit(): any {
  const kit = json('contracts/world/kit.json');
  for (const a of kit.assets) if (/^Road(Ribbon|Mark)_/.test(a.id)) a.status = 'adopted';
  for (const r of NEW_ROWS) if (!kit.assets.some((a: any) => a.id === r.id)) kit.assets.push(r);
  return kit;
}
export function writeProposalKit(file: string): string {
  mkdirSync(dirname(file), { recursive: true });
  writeFileSync(file, JSON.stringify(proposalKit()));
  return file;
}

/** The plan's standards.streetscape.roads keys the shoulder and the pinstripe would live under. */
export const SHOULDER = { piece: 'RoadShoulder_Straight_6m', arcs: { 4: 'RoadShoulder_Arc90_R4', 8: 'RoadShoulder_Arc90_R8' }, from: 0.36, to: 0.98, chrome: 0.035, why: 'the testbed\'s pearl streamline band beside the kerb (civic-road-finish.js bandStart .36, bandEnd .98, chromeWidth .035), chrome-edged' };
export const PINSTRIPE = { piece: 'PaveEdge_Pinstripe_6m', offset: 0.5, width: 0.04, why: 'the testbed\'s pavement pinstripe (civic-roads.js inlayParts, 0.47 to 0.52 in from the kerb face), brass not gold' };
export function proposalPlan(plan: Plan): Plan {
  const p = structuredClone(plan), R = (p.standards as any).streetscape.roads;
  R.shoulder ??= SHOULDER; R.pinstripe ??= PINSTRIPE;
  return p;
}
