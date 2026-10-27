# 072-F · texture for flat-colour surfaces: design (Vela, 2026-10-27)

Input for Latch (shader, builder), Splice (catalogue) and Redline (cost). Texture, not palette: every map is colourless (mean neutral, x2 convention), so every hue stays the swatch's or the material's.

## Priority from Latch's census (area.json, 8 play views)
Flat-colour surfaces cover 32.4 % of the frame and will take texture: kit rows 25.3 %, the facade atlas 6.2 %, builder Look materials 1.0 %. The excluded rows cover 11.3 %: glass film, the crossing pool glow, contact shades and emissives.
The single biggest is **DK_Limestone at 11.45 %** (the civic block's stone). After it come DK_Enamel_Ivory 4.4 %, DK_Enamel_Seafoam 3.0 %, DK_Inlay_Seafoam 2.6 % and DK_Chrome 1.35 %.
So the **kit pairs come first**: those five rows are about 23 % of the frame, they cost no new material and no shader change, and the change is Vela's (unity_mats.py). FacadeLit (6.2 %) comes second.

## The set (made, build-time, deterministic)
`python3 tools/decokit/surface_tex.py` (run.ts runs it before Blender; export_kit copies `surface_*`). Output in `out/decokit-ground/`, report `surface-tex.json` (sources with sha256, stats, MiB). Same sources give the same bytes (checked: two runs, same sha256).

| file | what | size | import | MiB (mips) |
|---|---|---|---|---:|
| `surface_detail_v1.png` | FacadeLit layer array, 6 layers stacked vertically | 1024 x 6144 | Texture2DArray (`textureShape: 2`, `flipbookRows: 6`), linear, BC7, aniso 8, repeat | 8.00 |
| `surface_enamel_detail_v1[_normal].png` | kit detail pair: glaze | 1024² + 1024² | detail albedo linear BC7 / normal BC5 | 2.67 |
| `surface_metal_detail_v1[_normal].png` | kit detail pair: brushed | 1024² + 1024² | as above | 2.67 |
| `surface_limestone_detail_v1[_normal].png` | kit detail pair: limestone | 1024² + 1024² | as above | 2.67 |
| **total** | | | | **15.98** |

**Texture-memory budget: 24 MiB for the whole 072-F set** (the generator fails over it). 8 MiB is left for a foliage/bark layer or a fabric layer if Sky wants them.

Array channels: R albedo detail (128 = neutral, multiply by 2R), G height (0..1), B smoothness offset (128 = none), A spare (255).

| layer | name | source (old DecoKit maps, store, read-only) | tile m | albedo sd | used by |
|---:|---|---|---:|---:|---|
| 0 | render | limestone_polished_v1, turned 90 degrees, softened | 3.0 | 3.5 % | class 0 matte, vertical faces |
| 1 | limestone | limestone_polished_v1 (+ normal) | 2.0 | 6 % | class 1 stone (joints stay procedural) |
| 2 | enamel | glazed_tile_ivory_v1 (+ normal) | 1.0 | 2.5 % | class 2 enamel (grid stays) |
| 3 | metal | brushed_steel_v1 (+ normal) | 0.5 | 5 % | class 4 chrome at 0.35, class 6 brass at 1 |
| 4 | roof | asphalt_fine_v1 (+ normal) | 1.5 | 8 % | class 0 matte, up-facing (n.y > 0.7) |
| 5 | panel | facade_panel_v1 (DK_Limestone_Tex's sheet) | 2.0 | 5 % | QuadKit stone walls, picked by the vertex B channel |

At play zoom a pixel is about 2.6 cm, so millimetre grain mips away. Every layer except metal and panel carries a soft tonal cloud about a fifth of the tile across (`macro`). That cloud is what shows at zoom 14 (preview_play.png). Values are [S]; Glint and Suture judge them in game light.

## FacadeLit (Latch)
Goal: one array sample per fragment. Same two materials (`facade`, `facade-gloss`), same keywords and passes, SRP batcher kept.
1. `FacadeLit.shader` Properties: `[NoScaleOffset] _SurfaceDetail("Surface detail", 2DArray) = "" {}`. In `FacadeSurface.hlsl`: `TEXTURE2D_ARRAY(_SurfaceDetail); SAMPLER(sampler_SurfaceDetail);`. Textures sit outside `UnityPerMaterial`, so the CBUFFER is unchanged and the SRP batcher still holds. Add no new float properties. Per-layer tile and strength go in as `static const` arrays (values in the table above):
   `static const float kTile[6] = {3.0, 2.0, 1.0, 0.5, 1.5, 2.0};`
2. Layer pick in `FacadeSurface`: `int lay = col.b < 0.99 ? (int)floor(col.b * 8.0) : default(cls, n)`, where the default is cls 0 → `n.y > 0.7 ? 4 : 0`, 1 → 1, 2 → 2, 4 → 3 (k 0.35), 6 → 3 (k 1), and 3 and 5 → none (skip the sample: the class is per vertex, so the branch is coherent). Vertex B is 255 everywhere today (Pb.C and the MeshKit padding), so nothing changes until the builder writes it.
3. `float4 d = SAMPLE_TEXTURE2D_ARRAY(_SurfaceDetail, sampler_SurfaceDetail, uv / kTile[lay], lay);` with `uv = FcPlane(p, n)`, which is already computed.
   `s.albedo *= lerp(1, 2 * d.r, k); height += (d.g - 0.5) * 0.008 * k; s.smoothness = saturate(s.smoothness + (d.b - 0.5) * 0.5 * k);`
   The existing `FacadeBumpNormal` turns the height into the normal: no tangents and no vertex-format change.
4. Class 1: drop the two `FcNoise` grain octaves (`grain`, `fine`). The sample replaces them, so the stone wall's ALU goes down. Keep the joints and the per-block tone.
5. `Atlas.Make`: `m.SetTexture("_SurfaceDetail", AssetDatabase.LoadAssetAtPath<Texture2DArray>("Assets/Suborned/Kit/Textures/surface_detail_v1.png"))` on both materials. If the array is missing, throw (unknown = reject).
6. Builder: `Pb.C(ao, cls, layer = -1)` writes B = `(layer + 0.5) / 8` when layer ≥ 0. QuadKit gets an optional layer: FacadeKit body walls and gate-tower walls (the `*_w` wall swatches) take layer 5 panel, so a far-LOD or gate wall reads as the DK_Limestone_Tex stone beside it. FabricKit cloth takes layer 0 (so a sloped awning never gets the roof layer). MegaKit keeps the default.
7. Look.Mat surfaces with no `material` (flat.md "look" rows: pylon, column, trim, canopy, girder, booth and the rest) are URP Lit. Give them the kit pair of their kind in the existing `_DetailAlbedoMap` / `_DetailNormalMap` slots plus `_DETAIL_MULX2`. That is one texture set on a material that already exists, not a new material. Ground base surfaces with a `ground/` twin (pavement, park, median) should draw the twin's kit material.

## Untextured kit rows (Vela, first, after Redline's before-capture)
The census shaders are the cutaway copies (DecoKit/Lit Cutaway, Complex Lit Cutaway). Both keep `_DETAIL_MULX2` in every pass, so the detail slots work as they do in URP Lit.
`unity_mats.py`: every row the list marks `texture` gets the pair of its kind in its own .mat: `_DetailAlbedoMap` and `_DetailNormalMap` at `m_Scale = 1 / tile` on the 1 m box-projected UVs, `_DETAIL_MULX2`, and `_DetailNormalMapScale` = the pair's bump. The robot and car enamels already work this way (`robot_enamel_text`). There are no new materials and no colour change. Legacy-kept rows (DK_Chrome, DK_Steel, DK_Iron and others) are rewritten with their legacy guid, as the retuned rows are today. The catalogue picks the textures up from the .mat guids (tools/kit/catalogue.ts `matFiles`).

## Cost (Redline measures, same scene before and after)
- **SetPass:** the facade stays at 2 materials, with no new keyword. The kit gains one variant per shader (Lit and Complex Lit, with `_DETAIL_MULX2`), because detailed and plain rows sit in different SRP batches: expect at most +2 SetPass in total, never one per colour. Option if that shows: give every enamel and metal row the pair, so no plain variant is left.
- **Batches and instancing:** unchanged. Material count, meshes and the instancing keys stay the same.
- **Fragment:** FacadeLit gains 1 array fetch and loses 2 value-noise octaves (8 hashes) on stone. Kit rows with the pair gain 2 fetches each, as robots and cars already do.
- **Memory:** +15.98 MiB GPU (budget 24).
- **What to measure:** SetPass, batches, texture memory and Studio ms at the four yaws of office-front and cell-open (briefs/072/latch/baseline). The Air A/B goes in the same run as every-face.
