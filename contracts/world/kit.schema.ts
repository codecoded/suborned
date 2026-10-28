// suborned.kit/1: the DecoKit catalogue, written by `bun run kit` (tools/kit) to contracts/world/kit.json.
// Source of the binaries: SUBORNED_OLD (default ~/Projects/the-suborned) / SUBORNED_STORE; paths below are relative to the DecoKit root.
// Frame: 1 unit = 1 m, pivot = footprint centre at ground, front = +Z. `status` is edited by hand (or by a brief); `bun run kit` keeps it.
export type KitStatus = 'candidate' | 'adopted' | 'rejected';
/** root: 'generated' = under the generator's output (tools/decokit: $DECOKIT_OUT, default out/decokit), else under the DecoKit root. */
export type KitFile = { root?: 'generated'; role: 'fbx' | 'lod' | 'fbx-meta' | 'prefab' | 'prefab-meta' | 'material' | 'material-meta' | 'texture' | 'texture-meta' | 'data'; path: string; sha256: string; bytes: number };
/** Generator tags. `scale` = allowed uniform scale on height; `squash` = the smallest allowed ratio of the width scale to the height scale (1 = uniform only). */
/** reach: the side the piece's working end extends to from the pivot (`axis` in kit axes, `m` metres), e.g. a streetlamp's head over the road. */
export type KitReach = { axis: '+x' | '-x' | '+z' | '-z'; m: number };
/** What the play camera sees through a piece (brief 008 B1, Vela): `open` = low or slender, a person behind it stays seen; `glazed` = a frontage with glass the view passes through but a wall of it, so people behind read only through the architecture fade. Absent = not stated (every piece adopted before brief 008). */
export type KitSight = 'open' | 'glazed';
/** `stash` (brief 013, Vantage/Motive): the piece is a closed container that can hold a body out of every sightline, `holds` bodies at a time. Size alone does not decide it (a lamp, a booth, a crate stack all pass a size test), so it is a tag Vela and the checker read: R-HIDE holds every world stash to it. Absent = the piece hides nothing. */
export type KitStash = { holds: number };
export type KitTags = { use: string[]; reach?: KitReach; trade?: string[]; storeys?: [number, number]; form?: 'bay' | 'corner' | 'end'; scale: [number, number]; squash: number; wave: 1 | 2 | 3; sight?: KitSight; stash?: KitStash };
/** A distance version: `file` relative to the same root as the piece's files (role 'lod'), `tris`, `screen` = Unity LOD transition (fraction of screen height at lodBias 1):
 *  the piece's `lodCategory` switch point (LOD1 at `l1`, LOD2 at `l2`). LOD0 = the fbx; up to LOD2 (about 25% then 6% of the triangles); absent for light pieces. */
export type KitLod = { file: string; tris: number; screen: number };
/** LOD categories (Vela, brief 007 round 3, out/review/lod-thresholds.html): street props, trees, facades, towers, signatures, horizon. */
export type KitLodCategory = 'street' | 'trees' | 'facades' | 'towers' | 'signatures' | 'horizon';
/** Switch points of a category as Unity LODGroup screen-relative heights at lodBias 1: LOD0 -> LOD1 below `l1`, LOD1 -> LOD2 below `l2`; no cull level. */
export type KitLodSwitch = { l1: number; l2: number };
/** Brief 072-D: distance classes of the every-face pieces (tools/kit/lod.ts LOD_CLASSES); `distances` = [L1, L2, cull] m from the follow camera (cull 0 = none). */
export type KitLodClass = 'bay3' | 'bay4' | 'storey' | 'cornerStorey' | 'column' | 'landmark' | 'crown';
export type KitLodClassSpec = { distances: [number, number, number] };
/** pivot 'origin-ground' = the pivot is not the footprint centre (a lamp's pole): `pivotOffset` = [x, z] of the footprint centre from the pivot in kit axes, i.e. the piece's centre sits there when its pivot is placed. */
export type KitAsset = {
  id: string; category: string; size: [number, number, number]; pivot: 'footprint-centre-ground' | 'origin-ground'; pivotOffset?: [number, number]; front: '+Z';
  tris: number; materials: string[]; parts?: unknown; lod1?: unknown; lods?: KitLod[]; lodCategory: KitLodCategory; /** brief 072-D: its distance class (lodClasses): the every-face pieces, RecordsTower and (072 F2) the roof crowns */ lodClass?: KitLodClass; files: KitFile[]; tags: KitTags; status: KitStatus;
  source?: 'generated'; order?: KitOrder; built?: KitBuilt;
  /** The tools/decokit script that built a piece made without a plan order (brief 008 civic pieces). */
  sourceScript?: string;
};
/** A piece the Blender generator built to a plan order (tools/decokit): `order.hash` = the order's hash (stale when the plan's order changes), `built` = extents [xmin, xmax, ymin, ymax] about the pivot below / above the first setback (or the frame) / of the crown, and the tip (pivot-relative `tip`, plan height `tipPlan`). */
export type KitBuilt = { split: number; roof: number | null; lower: [number, number, number, number] | null; upper: [number, number, number, number] | null; crown: [number, number, number, number] | null; tip: number; tipPlan: number };
export type KitOrder = { id: string; hash: string };
/** Brief 074-B (Vela, tools/decokit/colourways.py): building colourways. `materials[base][family]` = the material a module's base material slot draws in when its export
 *  `colourway` is that facade.json family; a colourway not listed (`base`, `civic`) keeps the base. `slots` = base material -> slot (wall, band, fascia, plinth); `colours` = each
 *  family's slot hexes (sRGB). Each material differs from its base only in base colour (same shader, keywords and textures). `files` = the .mat + .meta set, hash-checked and copied like a piece's. */
export type KitColourways = { schema: 'suborned.colourways/1'; brief: string; source: string; families: string[]; keep: string[]; slots: Record<string, 'wall' | 'plinth'>; materials: Record<string, Record<string, string>>; colours: Record<string, { wall: string; plinth: string; /** 074-H: the facade.json wall swatch before softening */ wallSwatch: string; swatches: { w: string; p: string } }>; /** 074-H: the wall's CIELAB lerp toward the old reel's cream (0 = the swatch) */ wallSoften: number; /** 074-H: the old facade_panel_v1 sheet's mean colour */ oldCream: string; files: KitFile[] };
/** lodCategories: the switch points per category (every asset's `lodCategory` names one); Latch builds one LODGroup per prefab from them at kit import. */
export type Kit = { schema: 'suborned.kit/1'; source: { manifest: string; materials: string; manifestSha256: string; materialsSha256: string }; lodCategories: Record<KitLodCategory, KitLodSwitch>; /** brief 072-D */ lodClasses?: Record<KitLodClass, KitLodClassSpec>; /** brief 072-F: kit-wide textures no piece's material references (the FacadeLit surface-detail array, the kit detail pairs the builder's Look materials take): `surface_*` from the generator's Textures/, hash-checked and copied like a piece's */ textures?: KitFile[]; /** brief 074-B */ colourways?: KitColourways; assets: KitAsset[] };
