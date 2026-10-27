# 072-F textured against untextured (v4, batchmode offscreen), 2026-10-27 22:21

Untextured: out/scratch/latch/072/v4/v4.world.json sha256 a50c649a5c84b23ab799a32c9cff945fd77fde25a01013483ea0702c5fbb4fc2 (2026-10-27 21:48). Textured: out/scratch/latch/072/v4/v4.world.json sha256 a50c649a5c84b23ab799a32c9cff945fd77fde25a01013483ea0702c5fbb4fc2 (2026-10-27 22:21). Same views, camera and estimator (cameras/play posed as HandoverHost.PlaceCamera: orthographic 14, pitch 38, standOff 160 m; cell-open from HandoverHost.frame.World(OpeningFocus); Camera.Render into Capture's HDR target 1920x1080 (no Game view)).

**Nothing here is UnityStats.** No number here is UnityStats (that needs the visible Game view; queued for when the Studio is free). census* = Baseline072's census method (16:9 frustum + LOD pick; counts shadows-only renderers as visible, as the baseline did). main* = the same without shadows-only renderers. setPassEstimate = distinct shader variants: forward (every visible material, its keywords) + SSAO depth-normals prepass (opaque, _ALPHATEST_ON/_NORMALMAP) + shadow (per cascade, its casters, _ALPHATEST_ON); fullscreen passes (sky, SSAO, post, copies) not counted. shadow* = casters per cascade by URP's split for this camera reproduced (CullingResults culling planes and sphere, bounds test; LOD-picked meshes): an estimate. frameMs = batchmode offscreen Camera.Render, GPU waited on, indicative only.

## Counts, as built (every face): untextured → textured

| view | yaw | main tris | materials | SetPass est. (fwd + dn + shadow) | casters | shadow tris | batchmode ms (median) |
|---|---:|---:|---:|---:|---:|---:|---:|
| office-front | 30 | 653,222 → 653,222 (+0) | 88 → 88 | 36 → 38 (20 + 6 + 12; +2) | 168 → 168 | 418,741 → 418,741 | 5.6 → 5.81 |
| office-front | 120 | 649,646 → 649,646 (+0) | 90 → 90 | 37 → 39 (21 + 6 + 12; +2) | 200 → 200 | 430,321 → 430,321 | 3.48 → 6.22 |
| office-front | 210 | 635,546 → 635,546 (+0) | 85 → 85 | 36 → 37 (19 + 6 + 12; +1) | 147 → 147 | 384,005 → 384,005 | 3.98 → 5.63 |
| office-front | 300 | 624,136 → 624,136 (+0) | 83 → 83 | 36 → 37 (19 + 6 + 12; +1) | 133 → 133 | 300,617 → 300,617 | 4 → 5.67 |
| cell-open | 30 | 768,845 → 768,845 (+0) | 137 → 137 | 40 → 43 (23 + 7 + 13; +3) | 333 → 333 | 777,755 → 777,755 | 6.84 → 7.57 |
| cell-open | 120 | 788,387 → 788,387 (+0) | 140 → 140 | 40 → 43 (23 + 7 + 13; +3) | 370 → 370 | 714,211 → 714,211 | 7.16 → 8.03 |
| cell-open | 210 | 752,426 → 752,426 (+0) | 134 → 134 | 40 → 43 (23 + 7 + 13; +3) | 330 → 330 | 695,351 → 695,351 | 6.56 → 7.58 |
| cell-open | 300 | 757,208 → 757,208 (+0) | 136 → 136 | 41 → 44 (24 + 7 + 13; +3) | 251 → 251 | 616,438 → 616,438 | 6.21 → 7.16 |

## Texture memory of the scene (every texture an enabled renderer's material binds)

| | textures | GPU (format x size x mips x layers) | Editor runtime (Profiler.GetRuntimeMemorySizeLong) | surface_* set GPU |
|---|---:|---:|---:|---:|
| untextured (v4 scene, 2026-10-27 22:06) | 72 | 158.45 MiB | 316.96 MiB | 0.00 MiB |
| textured (2026-10-27 22:21) | 79 | 174.45 MiB | 348.97 MiB | 16.00 MiB |
| delta | +7 | +16.00 MiB | +32.01 MiB | budget 24 MiB |

New textures: surface_detail_v1, surface_enamel_detail_v1, surface_enamel_detail_v1_normal, surface_limestone_detail_v1, surface_limestone_detail_v1_normal, surface_metal_detail_v1, surface_metal_detail_v1_normal.

## Screen area by map kind (mean of the 8 play views; all materials drawn opaque)

| kind | materials before → after | mean share before → after |
|---|---:|---:|
| textured | 33 → 33 | 56.06 % → 56.06 % |
| flat | 91 → 55 | 37.77 % → 11.71 % |
| flat+detail | 0 → 36 | 0.00 % → 26.07 % |
| swatch-atlas | 2 → 0 | 6.16 % → 0.00 % |
| swatch-atlas+detail | 0 → 2 | 0.00 % → 6.16 % |

Flat colour left (kinds flat + swatch-atlas without detail): 43.94 % → 11.71 % of the frame. `flat` still counts excluded rows (glass film, emissives, contact shades, the crossing glow) and any kit row whose .mat has no detail pair; `swatch-atlas+detail` still holds FacadeLit's excluded classes (glass 3, neon 5), which the census cannot split from the textured classes.

Largest flat rows left:

| material | shader | mean share |
|---|---|---:|
| DK_GlassDark | Suborned/DecoKit/GlassSky | 4.56 % |
| DK_Road_Pool | Suborned/Cross Glow | 2.78 % |
| contact/foot | Suborned/Contact Shade | 1.55 % |
| DK_Leaf_CivicDark | DecoKit/Lit Cutaway | 0.73 % |
| DK_Leaf_Civic | DecoKit/Lit Cutaway | 0.40 % |
| DK_Glass_Clear | DecoKit/Lit Cutaway | 0.39 % |
| DK_Road_Lens | DecoKit/Lit Cutaway | 0.21 % |
| DK_GroundCover | DecoKit/Lit Cutaway | 0.19 % |
| DK_Glass_Sheen | DecoKit/Lit Cutaway | 0.18 % |
| DK_Window_Film | DecoKit/Complex Lit Cutaway | 0.11 % |
| DK_Fountain_Gold | DecoKit/Lit Cutaway | 0.11 % |
| contact/kerb | Suborned/Contact Shade | 0.10 % |
| DK_Fountain_Silver | DecoKit/Lit Cutaway | 0.07 % |
| DK_Bloom_Coral | DecoKit/Lit Cutaway | 0.06 % |
| DK_Bloom_Cream | DecoKit/Lit Cutaway | 0.06 % |
