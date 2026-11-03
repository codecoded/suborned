// suborned.kit/1: the DecoKit catalogue, written by `bun run kit` (tools/kit) to contracts/world/kit.json.
// Source of the binaries: SUBORNED_OLD (default ~/Projects/the-suborned) / SUBORNED_STORE; paths below are relative to the DecoKit root.
// Frame: 1 unit = 1 m, pivot = footprint centre at ground, front = +Z. `status` is edited by hand (or by a brief); `bun run kit` keeps it.
export type KitStatus = 'candidate' | 'adopted' | 'rejected';
/** root: 'generated' = under the generator's output (tools/decokit: $DECOKIT_OUT, default out/decokit); 'extra' = under the asset's `extra` folder (brief 077, tools/kit/extra.ts),
 *  at `src` there when the Kit/ path (`path`) is renamed for the asset's id; else under the DecoKit root. */
export type KitFile = { root?: 'generated' | 'extra'; src?: string; role: 'fbx' | 'lod' | 'fbx-meta' | 'prefab' | 'prefab-meta' | 'material' | 'material-meta' | 'texture' | 'texture-meta' | 'data'; path: string; sha256: string; bytes: number };
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
  /** Brief 077: the extra generated root (repo-relative, e.g. out/decokit-077) the piece's 'extra' files come from (tools/kit/extra.ts). */
  extra?: string;
  /** Brief 077: the adopted piece this candidate rebuilds (catalogued as `<replaces>_TB`; civic-look.json swaps it in on adoption, and the old one is rejected). */
  replaces?: string;
  /** Brief 077: the LODGroup reference size, m, Unity's KitLods sets in place of the group's extent (the leaf-scale switch heights on 20-35 m companions). */
  lodSize?: number;
  /** Brief 077 (Latch's KitLods play-zoom guard, briefs/077/READY-build): this row's own LODs switch at play zoom by design (the testbed planting rule), so the guard counts it and never
   *  fails it. Declared, never inferred from an id. */
  lodAtPlay?: boolean;
  /** Brief 077: per FBX object, its materials and whether it casts shadows (the soil objects: off), for KitImport. */
  objects?: { name: string; materials: string[]; shadows: 'on' | 'off' }[];
  /** The tools/decokit script that built a piece made without a plan order (brief 008 civic pieces). */
  sourceScript?: string;
};
/** A piece the Blender generator built to a plan order (tools/decokit): `order.hash` = the order's hash (stale when the plan's order changes), `built` = extents [xmin, xmax, ymin, ymax] about the pivot below / above the first setback (or the frame) / of the crown, and the tip (pivot-relative `tip`, plan height `tipPlan`). */
export type KitBuilt = { split: number; roof: number | null; lower: [number, number, number, number] | null; upper: [number, number, number, number] | null; crown: [number, number, number, number] | null; tip: number; tipPlan: number };
export type KitOrder = { id: string; hash: string };
/** Brief 074-B (Vela, tools/decokit/colourways.py): building colourways. `materials[base][family]` = the material a module's base material slot draws in when its export
 *  `colourway` is that facade.json family; a colourway not listed (`base`, `civic`) keeps the base. `slots` = base material -> slot (wall, band, fascia, plinth); `colours` = each
 *  family's slot hexes (sRGB). Each material differs from its base only in base colour (same shader, keywords and textures). `files` = the .mat + .meta set, hash-checked and copied like a piece's. */
/** Brief 075 old palette (was 074-B/H): the old DecoKitColourways scheme plan+livery+soft. `materials[base][family]` = the variant a slot draws in (a slot a family leaves alone
 *  has no entry: the kit material); `colours[family][slot]` = the sRGB hex it paints; `check[material]` = its base colour against the old Paint .mat (`oldDelta`). */
export type KitColourways = { schema: 'suborned.colourways/1'; brief: string; source: string; families: string[]; keep: string[]; slots: Record<string, 'wall' | 'fascia' | 'sills' | 'trim' | 'plinth'>; materials: Record<string, Record<string, string>>; colours: Record<string, Partial<Record<'wall' | 'fascia' | 'sills' | 'trim' | 'plinth', string>>>; old?: { source: string; ensembles: Record<string, { wall: string | null; fascia: string | null; sills: string | null }>; soft: Record<string, string>; ivory: string; lighter: string; tint: string }; check?: Record<string, { hex: string; baseFrom: string; baseColor: number[]; oldPaint: string | null; tintedOver?: { texture: string; srgbMean: number[] }; oldBaseColor?: number[]; oldDelta?: number }>; files: KitFile[] };
/** Brief 075-C (Vela, tools/decokit/livery.py): landmark livery, the old DecoKitColourways `livery` layer. `keys[key]` = a colour key (vermilion, brick, gold): its base material,
 *  the variant `material` an instance's base slot draws in, the sRGB `hex`, and the old rule's `pieces` (ids, or kit categories). `pieces[id][base]` = the key a placed piece takes on that
 *  base slot (the old rule resolved over this catalogue's non-rejected pieces; Splice's export table briefs/075/landmarks.json is checked against it). Each variant differs from its base only
 *  in base colour (same shader, keywords and textures). `files` = the .mat + .meta set, hash-checked and copied like a piece's. */
export type KitLiveryKey = { base: string; material: string; hex: string; source: string; baseColor: [number, number, number]; /** brief 075 old palette: the old Paint .mat's finish (the gold) */ finish?: { metallic: number; smoothness: number }; pieces: string[] | { categories: string[] } };
export type KitLandmarks = { schema: 'suborned.livery/1'; brief: string; source: string; keys: Record<string, KitLiveryKey>; pieces: Record<string, Record<string, string>>; files: KitFile[] };
/** Brief 075-F1 (Vela, tools/decokit/posters.py): billboard and poster variants. Two sheets in one layout: A = the old billboard_ads_v1.png (byte-identical), B = billboard_ads_v2.png
 *  (new art, same style). `ads[key]` = one of 16 ads (adOrder): its sheet, words, the UV offset from the canonical cell and `materials[base]` (base DK_Billboard_Ads or DK_Billboard_Ads_Lit
 *  -> the variant). `adPieces[id]` = the base slot a billboard swaps (every billboard face bakes the canonical cell, so any ad fits any board); `backFace[key]` = what a pylon's back face then
 *  shows. `sheetPieces[id]` = pieces that keep their old cells (posters, letter boards): sheet A = keep the base, sheet B = `sheets.B.materials[base]`. `defaults[id]` = the ad each billboard
 *  showed before (its old cell). Each variant differs from its base only in texture guid and offset (same shader, keywords, colour). `files` = the .mat + .meta set and sheet B + .meta. */
export type KitPosterAd = { sheet: 'A' | 'B'; words: string; offset: [number, number]; materials: Record<string, string> };
export type KitPosters = { schema: 'suborned.posters/1'; brief: string; source: string; rule: string; sheets: { A: { texture: string; guid: string; source: string }; B: { texture: string; guid: string; source: string; materials: Record<string, string> } }; ads: Record<string, KitPosterAd>; adOrder: string[]; backFace: Record<string, string>; adPieces: Record<string, string>; sheetPieces: Record<string, string>; defaults: Record<string, string>; posters: { A: string[]; B: string[] }; boards: { A: string[][]; B: string[][] }; files: KitFile[] };
/** lodCategories: the switch points per category (every asset's `lodCategory` names one); Latch builds one LODGroup per prefab from them at kit import. */
/** Brief 075 roll-out (Vela, tools/decokit/carpaint.py): 1950s car paints for presentation traffic. A key = a body and a roof material (DK_CarPaint_*, base colour
 *  variants of DK_CarEnamel_Ivory); `slots[piece]` names the base material of each paint slot on that sedan, which a traffic car swaps for the key's. */
export type KitCarPaintKey = { body: string; roof: string; bodyHex: string; roofHex: string; family: 'pastel' | 'saturated' | 'reversed' | 'old' };
export type KitCarPaints = { schema: 'suborned.carpaint/1'; brief: string; source: string; base: string; rule: string; slots: Record<string, { body: string; roof: string }>; keys: Record<string, KitCarPaintKey>; colours: Record<string, string>; files: KitFile[] };
export type Kit = { schema: 'suborned.kit/1'; source: { manifest: string; materials: string; manifestSha256: string; materialsSha256: string }; lodCategories: Record<KitLodCategory, KitLodSwitch>; /** brief 072-D */ lodClasses?: Record<KitLodClass, KitLodClassSpec>; /** brief 072-F: kit-wide textures no piece's material references (the FacadeLit surface-detail array, the kit detail pairs the builder's Look materials take): `surface_*` from the generator's Textures/, hash-checked and copied like a piece's */ textures?: KitFile[]; /** brief 074-B */ colourways?: KitColourways; /** brief 075-C */ landmarks?: KitLandmarks; /** brief 075-F1 */ posters?: KitPosters; /** brief 075 roll-out */ carPaints?: KitCarPaints;
  /** Brief 077 (Sky 2026-10-30, planting is see-through): materials that are drawn but stop no sight and no walker, wherever a piece carries them; the geometry audit measures a piece without them (tools/geometry/pieces.ts). */
  seeThrough?: KitSeeThrough; assets: KitAsset[] };
export type KitSeeThrough = { materials: string[]; src: string };
