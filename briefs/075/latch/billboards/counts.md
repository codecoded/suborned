# 075-F1 counts: the 075-E streamline rebuild against the billboard rebuild (batchmode offscreen, estimates, not UnityStats)

Before: out/scratch/latch/075e/pin/075e.world.json sha256 a6c76f6a9f041e1b6f542631ad01f2797ddb85397e429e01eeba562f83e782f5 (2026-10-28 14:37). After: out/scratch/latch/075f1/pin/075f1.world.json sha256 2de0d88e618ccbe5ab34ad33a93ed37a10413901e359da30de0ff905de13f254 (2026-10-28 15:21).

| view | materials | SetPass est. (fwd) | shadow variants | main renderers | main tris | shadow tris | casters | modules in frustum | frame ms (median, indicative) |
|---|---|---|---|---|---|---|---|---|---|
| office-front y030 | 89 → 89 (+0) | 36 → 36 (+0) (20 → 20 (+0)) | 11 → 11 (+0) | 232 → 232 (+0) | 639,464 → 639,464 (+0) | 353,890 → 353,890 (+0) | 152 → 152 (+0) | 8 → 8 (+0) | 5.4 → 2.43 |
| office-front y210 | 86 → 86 (+0) | 37 → 37 (+0) (19 → 19 (+0)) | 12 → 12 (+0) | 213 → 213 (+0) | 635,710 → 635,710 (+0) | 353,609 → 353,609 (+0) | 144 → 144 (+0) | 0 → 0 (+0) | 2.91 → 2.41 |
| cell-open y030 | 142 → 142 (+0) | 43 → 43 (+0) (23 → 23 (+0)) | 13 → 13 (+0) | 293 → 293 (+0) | 768,971 → 768,971 (+0) | 758,691 → 758,691 (+0) | 333 → 333 (+0) | 19 → 19 (+0) | 6.4 → 6.12 |
| cell-open y210 | 142 → 142 (+0) | 43 → 43 (+0) (23 → 23 (+0)) | 13 → 13 (+0) | 273 → 273 (+0) | 752,718 → 752,718 (+0) | 654,201 → 654,201 (+0) | 315 → 315 (+0) | 15 → 15 (+0) | 6.17 → 6.15 |
| wide-lamplight y030 | 76 → 76 (+0) | 36 → 36 (+0) (18 → 18 (+0)) | 12 → 12 (+0) | 254 → 254 (+0) | 460,052 → 460,052 (+0) | 434,074 → 434,074 (+0) | 330 → 330 (+0) | 87 → 87 (+0) | 6.65 → 3.97 |

## Census

| | before | after |
|---|---|---|
| columns.placed | 1,821 | 1,823 |
| columns.singleModules | 2,467 | 2,467 |
| columns.layoutPrefabsPlaced | 151 | 151 |
| columns.layoutPrefabsInFolder | 151 | 151 |
| columns.meshAssetsInFolder | 453 | 453 |
| columns.distinctMeshesPlaced | 453 | 453 |
| columns.trianglesAllLodsOfPlacedMeshes | 429,558 | 429,558 |
| materials.scene | 330 | 358 |
| materials.modules | 48 | 48 |
| materials.shadowProxies | 33 | 33 |
| materials.sceneShaderVariants | 30 | 30 |
| materials.moduleShaderVariants | 8 | 8 |

Shadow proxy materials after (33): DK_Bark_Fibre, DK_Bark_Palm, DK_Beacon_Rose, DK_Bronze, DK_Chrome, DK_Coconut, DK_Crown_Gold, DK_Enamel_Charcoal, DK_Enamel_Coral, DK_Enamel_Ivory, DK_Enamel_Mustard, DK_Enamel_Seafoam, DK_Enamel_Teal, DK_Glass, DK_GlassDark, DK_Gold, DK_Hull_Ivory, DK_Iron, DK_Lamp, DK_Leaf_PalmFeather, DK_Leaf_Shrub, DK_Limestone_Tex, DK_Lobby, DK_Neon_Coral, DK_Render_White, DK_Signal_Amber, DK_Soil, DK_Steel, DK_Steel_Ivory, DK_Stone_Dark, DK_Terrazzo_Ivory, DK_Window_Film, DK_Window_Tower

Module shader variants before: ['DecoKit/Complex Lit Cutaway|_CLEARCOAT', 'DecoKit/Complex Lit Cutaway|_CLEARCOAT,_DETAIL_MULX2', 'DecoKit/Lit Cutaway|_DETAIL_MULX2', 'DecoKit/Lit Cutaway|_EMISSION', 'DecoKit/Lit Cutaway|_METALLICSPECGLOSSMAP,_NORMALMAP,_OCCLUSIONMAP', 'DecoKit/Lit Cutaway|_NORMALMAP', 'Suborned/DecoKit/GlassSky|', 'Universal Render Pipeline/Lit|']

after: ['DecoKit/Complex Lit Cutaway|_CLEARCOAT', 'DecoKit/Complex Lit Cutaway|_CLEARCOAT,_DETAIL_MULX2', 'DecoKit/Lit Cutaway|_DETAIL_MULX2', 'DecoKit/Lit Cutaway|_EMISSION', 'DecoKit/Lit Cutaway|_METALLICSPECGLOSSMAP,_NORMALMAP,_OCCLUSIONMAP', 'DecoKit/Lit Cutaway|_NORMALMAP', 'Suborned/DecoKit/GlassSky|', 'Universal Render Pipeline/Lit|']

Module roots by wall material after: {'DK_Limestone_Tex__apricot': 225, 'DK_Limestone_Tex__blush': 339, 'DK_Limestone_Tex__butter': 560, 'DK_Limestone_Tex__cream': 478, 'DK_Limestone_Tex__ivory': 266, 'DK_Limestone_Tex__lilac': 347, 'DK_Limestone_Tex__mint': 500, 'DK_Limestone_Tex__peach': 323, 'DK_Limestone_Tex__powder': 362, 'DK_Limestone_Tex__sand': 229, 'DK_Limestone_Tex__seagreen': 395, 'DK_Limestone_Tex__sky': 240, 'DK_Render_White__butter': 6, 'DK_Render_White__cream': 4, 'DK_Render_White__powder': 13, 'DK_Render_White__sand': 3}

## Poster pieces (Probe075F1, scene as built)

67 placed pieces (37 billboards, 19 advertising columns, 11 sandwich boards), 95 renderers over all LODs (67 at LOD0), 318,064 LOD0 triangles (362,332 over all LODs), 95 renderers cast shadows; 46 distinct materials on them, 23 of them poster materials (the base, 22 variants). Shader variants on poster pieces that appear on no other renderer: 0 (none): the swap adds no SetPass variant.

| piece | instances | renderers (all LODs) | LOD0 tris | all-LOD tris |
|---|---|---|---|---|
| DK_AdColumn | 19 | 19 | 91,010 | 91,010 |
| DK_Billboard_Pylon_A | 2 | 4 | 15,672 | 19,560 |
| DK_Billboard_Pylon_B | 7 | 14 | 53,508 | 66,794 |
| DK_Billboard_Rooftop_A | 6 | 12 | 34,848 | 43,404 |
| DK_Billboard_Rooftop_B | 7 | 14 | 40,656 | 50,638 |
| DK_Billboard_Rooftop_C | 6 | 12 | 34,848 | 43,404 |
| DK_Billboard_Wall_A | 5 | 5 | 13,470 | 13,470 |
| DK_Billboard_Wall_B | 3 | 3 | 8,082 | 8,082 |
| DK_Billboard_Wall_C | 1 | 1 | 2,694 | 2,694 |
| DK_SandwichBoard | 11 | 11 | 23,276 | 23,276 |

## Plain URP/Lit materials under the every-face modules

- DK_Crown_Gold @ Assets/Suborned/Kit/Models/Skyline/DK_StreamBay_6.fbx (embedded: True): 49 module renderers, 174 in all; pieces DK_StreamBay_6
- DK_Crown_Gold @ Assets/Suborned/Kit/Models/Skyline/DK_StreamBay_9.fbx (embedded: True): 16 module renderers, 104 in all; pieces DK_StreamBay_9
- DK_Crown_Gold @ Assets/Suborned/Kit/Models/Skyline/DK_StreamBay_9W.fbx (embedded: True): 10 module renderers, 135 in all; pieces DK_StreamBay_9W
- DK_Crown_Gold @ Assets/Suborned/Kit/Models/Skyline/DK_StreamCorner_9.fbx (embedded: True): 1 module renderers, 13 in all; pieces DK_StreamCorner_9
