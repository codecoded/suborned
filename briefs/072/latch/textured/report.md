# 072-F textured against untextured (v4, batchmode offscreen), 2026-10-27 22:53

Untextured: out/scratch/latch/072/v4/v4.world.json sha256 a50c649a5c84b23ab799a32c9cff945fd77fde25a01013483ea0702c5fbb4fc2 (2026-10-27 21:48). Textured: out/scratch/latch/072/v4/v4.world.json sha256 a50c649a5c84b23ab799a32c9cff945fd77fde25a01013483ea0702c5fbb4fc2 (2026-10-27 22:53). Same views, camera and estimator (cameras/play posed as HandoverHost.PlaceCamera: orthographic 14, pitch 38, standOff 160 m; cell-open from HandoverHost.frame.World(OpeningFocus); Camera.Render into Capture's HDR target 1920x1080 (no Game view)).

**Nothing here is UnityStats.** No number here is UnityStats (that needs the visible Game view; queued for when the Studio is free). census* = Baseline072's census method (16:9 frustum + LOD pick; counts shadows-only renderers as visible, as the baseline did). main* = the same without shadows-only renderers. setPassEstimate = distinct shader variants: forward (every visible material, its keywords) + SSAO depth-normals prepass (opaque, _ALPHATEST_ON/_NORMALMAP) + shadow (per cascade, its casters, _ALPHATEST_ON); fullscreen passes (sky, SSAO, post, copies) not counted. shadow* = casters per cascade by URP's split for this camera reproduced (CullingResults culling planes and sphere, bounds test; LOD-picked meshes): an estimate. frameMs = batchmode offscreen Camera.Render, GPU waited on, indicative only.

## Counts, as built (every face): untextured → textured

| view | yaw | main tris | materials | SetPass est. (fwd + dn + shadow) | casters | shadow tris | batchmode ms (median) |
|---|---:|---:|---:|---:|---:|---:|---:|
| office-front | 30 | 653,222 → 653,222 (+0) | 88 → 88 | 36 → 35 (19 + 5 + 11; -1) | 168 → 168 | 418,741 → 418,741 | 5.6 → 6.29 |
| office-front | 120 | 649,646 → 649,646 (+0) | 90 → 90 | 37 → 38 (20 + 6 + 12; +1) | 200 → 200 | 430,321 → 430,321 | 3.48 → 6.41 |
| office-front | 210 | 635,546 → 635,546 (+0) | 85 → 85 | 36 → 36 (18 + 6 + 12; +0) | 147 → 147 | 384,005 → 384,005 | 3.98 → 5.91 |
| office-front | 300 | 624,136 → 624,136 (+0) | 83 → 83 | 36 → 36 (18 + 6 + 12; +0) | 133 → 133 | 300,617 → 300,617 | 4 → 5.84 |
| cell-open | 30 | 768,845 → 768,845 (+0) | 137 → 137 | 40 → 41 (22 + 7 + 12; +1) | 333 → 333 | 777,755 → 777,755 | 6.84 → 7.38 |
| cell-open | 120 | 788,387 → 788,387 (+0) | 140 → 140 | 40 → 42 (22 + 7 + 13; +2) | 370 → 370 | 714,211 → 714,211 | 7.16 → 5.14 |
| cell-open | 210 | 752,426 → 752,426 (+0) | 134 → 134 | 40 → 42 (22 + 7 + 13; +2) | 330 → 330 | 695,351 → 695,351 | 6.56 → 7.38 |
| cell-open | 300 | 757,208 → 757,208 (+0) | 136 → 136 | 41 → 43 (23 + 7 + 13; +2) | 251 → 251 | 616,438 → 616,438 | 6.21 → 6.93 |

## Texture memory of the scene (every texture an enabled renderer's material binds)

| | textures | GPU (format x size x mips x layers) | Editor runtime (Profiler.GetRuntimeMemorySizeLong) | surface_* set GPU |
|---|---:|---:|---:|---:|
| untextured (v4 scene, 2026-10-27 22:06) | 72 | 158.45 MiB | 316.96 MiB | 0.00 MiB |
| textured (2026-10-27 22:52) | 79 | 174.45 MiB | 348.97 MiB | 16.00 MiB |
| delta | +7 | +16.00 MiB | +32.01 MiB | budget 24 MiB |

New textures: surface_detail_v1, surface_enamel_detail_v1, surface_enamel_detail_v1_normal, surface_limestone_detail_v1, surface_limestone_detail_v1_normal, surface_metal_detail_v1, surface_metal_detail_v1_normal.

## Screen area by map kind (mean of the 8 play views; all materials drawn opaque)

| kind | materials before → after | mean share before → after |
|---|---:|---:|
| textured | 33 → 33 | 56.06 % → 56.06 % |
| flat | 91 → 55 | 37.77 % → 11.66 % |
| flat+detail | 0 → 36 | 0.00 % → 26.11 % |
| swatch-atlas | 2 → 0 | 6.16 % → 0.00 % |
| swatch-atlas+detail | 0 → 2 | 0.00 % → 6.16 % |

Flat colour left (kinds flat + swatch-atlas without detail): 43.94 % → 11.66 % of the frame. `flat` still counts excluded rows (glass film, emissives, contact shades, the crossing glow) and any kit row whose .mat has no detail pair; `swatch-atlas+detail` still holds FacadeLit's excluded classes (glass 3, neon 5), which the census cannot split from the textured classes.

Largest flat rows left:

| material | shader | mean share |
|---|---|---:|
| DK_GlassDark | Suborned/DecoKit/GlassSky | 4.51 % |
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

## SRP batcher and texture reads per shader (2026-10-28 04:44, Unity 6000.6.2f1)

Method: SRP Batcher: ShaderUtil.GetSRPBatcherCompatibilityCode / GetSRPBatcherCompatibilityIssueReason (the Inspector's source), per subshader, after one play-camera render of the built scene (office-front y030). Texture reads: ShaderData pass ForwardLit (or the first pass tagged UniversalForward) fragment program, CompileVariant for Metal / StandaloneOSX (external-tool source), with each distinct local keyword set of the project's materials on the shader plus _MAIN_LIGHT_SHADOWS_CASCADE _SHADOWS_SOFT _SHADOWS_SOFT_HIGH _FORWARD_PLUS FOG_LINEAR; counted calls .sample( .sample_compare( .read( .gather( in the Metal source; static counts (a branch's samples count once).

SRP batcher (the Inspector's line, per subshader):

- `Suborned/FacadeLit` (Assets/Suborned/Shaders/FacadeLit.shader): subshader 0: SRP Batcher: compatible; subshader 1: SRP Batcher: compatible; subshader 2: SRP Batcher: compatible; subshader 3: SRP Batcher: compatible
- `DecoKit/Lit Cutaway` (Assets/Suborned/Shaders/DecoKit/DK_LitCutaway.shader): subshader 0: SRP Batcher: compatible; subshader 1: SRP Batcher: compatible; subshader 2: SRP Batcher: compatible
- `DecoKit/Complex Lit Cutaway` (Assets/Suborned/Shaders/DecoKit/DK_ComplexLitCutaway.shader): subshader 0: SRP Batcher: compatible; subshader 1: SRP Batcher: compatible; subshader 2: SRP Batcher: compatible

Texture reads in the forward fragment, per keyword set the materials use (play-view pipeline keywords added). `shadow compares` are the main light's soft-shadow taps (High: a 7×7 tent, 16 compares); `samples` are the material's own maps and the detail fetches.

| shader | local keywords | materials | samples | shadow compares | total | textures bound |
|---|---|---:|---:|---:|---:|---:|
| Suborned/FacadeLit | (none) | 2 | 3 | 16 | 19 | 4 |
| DecoKit/Lit Cutaway | _DETAIL_MULX2 | 60 | 5 | 16 | 21 | 6 |
| DecoKit/Lit Cutaway | _EMISSION | 53 | 3 | 16 | 19 | 4 |
| DecoKit/Lit Cutaway | (none) | 28 | 2 | 16 | 18 | 3 |
| DecoKit/Lit Cutaway | _METALLICSPECGLOSSMAP _NORMALMAP _OCCLUSIONMAP | 21 | 5 | 16 | 21 | 6 |
| DecoKit/Lit Cutaway | _ALPHATEST_ON _ENVIRONMENTREFLECTIONS_OFF _SPECULARHIGHLIGHTS_OFF | 8 | 1 | 16 | 17 | 2 |
| DecoKit/Lit Cutaway | _NORMALMAP | 7 | 3 | 16 | 19 | 4 |
| DecoKit/Lit Cutaway | _ALPHAPREMULTIPLY_ON _SURFACE_TYPE_TRANSPARENT | 7 | 2 | 16 | 18 | 3 |
| DecoKit/Lit Cutaway | _EMISSION _ENVIRONMENTREFLECTIONS_OFF _RECEIVE_SHADOWS_OFF _SPECULARHIGHLIGHTS_OFF | 4 | 2 | 0 | 2 | 2 |
| DecoKit/Lit Cutaway | _DETAIL_MULX2 _EMISSION | 4 | 6 | 16 | 22 | 7 |
| DecoKit/Lit Cutaway | _ALPHAPREMULTIPLY_ON _EMISSION _SURFACE_TYPE_TRANSPARENT | 3 | 3 | 16 | 19 | 4 |
| DecoKit/Lit Cutaway | _ALPHATEST_ON _EMISSION _ENVIRONMENTREFLECTIONS_OFF _RECEIVE_SHADOWS_OFF _SPECULARHIGHLIGHTS_OFF | 2 | 2 | 0 | 2 | 2 |
| DecoKit/Lit Cutaway | _METALLICSPECGLOSSMAP _OCCLUSIONMAP | 1 | 4 | 16 | 20 | 5 |
| DecoKit/Lit Cutaway | _DETAIL_MULX2 _METALLICSPECGLOSSMAP _NORMALMAP _OCCLUSIONMAP | 1 | 8 | 16 | 24 | 9 |
| DecoKit/Complex Lit Cutaway | _CLEARCOAT _DETAIL_MULX2 | 44 | 6 | 16 | 22 | 6 |
| DecoKit/Complex Lit Cutaway | _CLEARCOAT _DETAIL_MULX2 _EMISSION | 22 | 7 | 16 | 23 | 7 |
| DecoKit/Complex Lit Cutaway | _CLEARCOAT | 19 | 3 | 16 | 19 | 3 |
| DecoKit/Complex Lit Cutaway | _ALPHATEST_ON _CLEARCOAT | 1 | 3 | 16 | 19 | 3 |
| DecoKit/Complex Lit Cutaway | _CLEARCOAT _METALLICSPECGLOSSMAP _NORMALMAP _OCCLUSIONMAP | 1 | 6 | 16 | 22 | 6 |
