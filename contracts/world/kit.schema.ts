// suborned.kit/1: the DecoKit catalogue, written by `bun run kit` (tools/kit) to contracts/world/kit.json.
// Source of the binaries: SUBORNED_OLD (default ~/Projects/the-suborned) / SUBORNED_STORE; paths below are relative to the DecoKit root.
// Frame: 1 unit = 1 m, pivot = footprint centre at ground, front = +Z. `status` is edited by hand (or by a brief); `bun run kit` keeps it.
export type KitStatus = 'candidate' | 'adopted' | 'rejected';
/** root: 'generated' = under the generator's output (tools/decokit: $DECOKIT_OUT, default out/decokit), else under the DecoKit root. */
export type KitFile = { root?: 'generated'; role: 'fbx' | 'lod' | 'fbx-meta' | 'prefab' | 'prefab-meta' | 'material' | 'material-meta' | 'texture' | 'texture-meta'; path: string; sha256: string; bytes: number };
/** Generator tags. `scale` = allowed uniform scale on height; `squash` = the smallest allowed ratio of the width scale to the height scale (1 = uniform only). */
/** reach: the side the piece's working end extends to from the pivot (`axis` in kit axes, `m` metres), e.g. a streetlamp's head over the road. */
export type KitReach = { axis: '+x' | '-x' | '+z' | '-z'; m: number };
/** What the play camera sees through a piece (brief 008 B1, Vela): `open` = low or slender, a person behind it stays seen; `glazed` = a frontage with glass the view passes through but a wall of it, so people behind read only through the architecture fade. Absent = not stated (every piece adopted before brief 008). */
export type KitSight = 'open' | 'glazed';
export type KitTags = { use: string[]; reach?: KitReach; trade?: string[]; storeys?: [number, number]; form?: 'bay' | 'corner' | 'end'; scale: [number, number]; squash: number; wave: 1 | 2 | 3; sight?: KitSight };
/** A distance version: `file` relative to the same root as the piece's files (role 'lod'), `tris`, `screen` = Unity LOD transition (fraction of screen height at lodBias 1):
 *  the piece's `lodCategory` switch point (LOD1 at `l1`, LOD2 at `l2`). LOD0 = the fbx; up to LOD2 (about 25% then 6% of the triangles); absent for light pieces. */
export type KitLod = { file: string; tris: number; screen: number };
/** LOD categories (Vela, brief 007 round 3, out/review/lod-thresholds.html): street props, trees, facades, towers, signatures, horizon. */
export type KitLodCategory = 'street' | 'trees' | 'facades' | 'towers' | 'signatures' | 'horizon';
/** Switch points of a category as Unity LODGroup screen-relative heights at lodBias 1: LOD0 -> LOD1 below `l1`, LOD1 -> LOD2 below `l2`; no cull level. */
export type KitLodSwitch = { l1: number; l2: number };
/** pivot 'origin-ground' = the pivot is not the footprint centre (a lamp's pole): `pivotOffset` = [x, z] of the footprint centre from the pivot in kit axes, i.e. the piece's centre sits there when its pivot is placed. */
export type KitAsset = {
  id: string; category: string; size: [number, number, number]; pivot: 'footprint-centre-ground' | 'origin-ground'; pivotOffset?: [number, number]; front: '+Z';
  tris: number; materials: string[]; parts?: unknown; lod1?: unknown; lods?: KitLod[]; lodCategory: KitLodCategory; files: KitFile[]; tags: KitTags; status: KitStatus;
  source?: 'generated'; order?: KitOrder; built?: KitBuilt;
  /** The tools/decokit script that built a piece made without a plan order (brief 008 civic pieces). */
  sourceScript?: string;
};
/** A piece the Blender generator built to a plan order (tools/decokit): `order.hash` = the order's hash (stale when the plan's order changes), `built` = extents [xmin, xmax, ymin, ymax] about the pivot below / above the first setback (or the frame) / of the crown, and the tip (pivot-relative `tip`, plan height `tipPlan`). */
export type KitBuilt = { split: number; roof: number | null; lower: [number, number, number, number] | null; upper: [number, number, number, number] | null; crown: [number, number, number, number] | null; tip: number; tipPlan: number };
export type KitOrder = { id: string; hash: string };
/** lodCategories: the switch points per category (every asset's `lodCategory` names one); Latch builds one LODGroup per prefab from them at kit import. */
export type Kit = { schema: 'suborned.kit/1'; source: { manifest: string; materials: string; manifestSha256: string; materialsSha256: string }; lodCategories: Record<KitLodCategory, KitLodSwitch>; assets: KitAsset[] };
