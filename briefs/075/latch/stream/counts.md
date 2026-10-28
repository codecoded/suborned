# 075-E step 1 counts: the 075 rebuild against the streamline rebuild (batchmode offscreen, estimates, not UnityStats)

Before: out/scratch/latch/075/pin/075.world.json sha256 ec0339f9e26ffc1b65a7f496d585d32c5f9e1efd8f3bd10796956bce3b3c1155 (2026-10-28 12:45). After: out/scratch/latch/075e/pin/075e.world.json sha256 a6c76f6a9f041e1b6f542631ad01f2797ddb85397e429e01eeba562f83e782f5 (2026-10-28 14:37).

| view | materials | SetPass est. (fwd) | shadow variants | main renderers | main tris | shadow tris | casters | modules in frustum | frame ms (median, indicative) |
|---|---|---|---|---|---|---|---|---|---|
| office-front y030 | 90 → 89 (-1) | 36 → 36 (+0) (20 → 20 (+0)) | 11 → 11 (+0) | 230 → 232 (+2) | 653,254 → 639,464 (-13,790) | 379,052 → 353,890 (-25,162) | 150 → 152 (+2) | 5 → 8 (+3) | 3.71 → 5.4 |
| office-front y210 | 86 → 86 (+0) | 37 → 37 (+0) (19 → 19 (+0)) | 12 → 12 (+0) | 213 → 213 (+0) | 635,578 → 635,710 (+132) | 353,609 → 353,609 (+0) | 144 → 144 (+0) | 0 → 0 (+0) | 3.62 → 2.91 |
| cell-open y030 | 143 → 142 (-1) | 42 → 43 (+1) (23 → 23 (+0)) | 12 → 13 (+1) | 292 → 293 (+1) | 769,027 → 768,971 (-56) | 774,571 → 758,691 (-15,880) | 332 → 333 (+1) | 18 → 19 (+1) | 4.62 → 6.4 |
| cell-open y210 | 142 → 142 (+0) | 43 → 43 (+0) (23 → 23 (+0)) | 13 → 13 (+0) | 273 → 273 (+0) | 752,426 → 752,718 (+292) | 648,531 → 654,201 (+5,670) | 315 → 315 (+0) | 15 → 15 (+0) | 4.64 → 6.17 |
| wide-lamplight y030 | 78 → 76 (-2) | 36 → 36 (+0) (18 → 18 (+0)) | 12 → 12 (+0) | 244 → 254 (+10) | 476,148 → 460,052 (-16,096) | 448,800 → 434,074 (-14,726) | 323 → 330 (+7) | 78 → 87 (+9) | 6.24 → 6.65 |

## Census

| | before | after |
|---|---|---|
| columns.placed | 1,913 | 1,821 |
| columns.singleModules | 2,389 | 2,467 |
| columns.layoutPrefabsPlaced | 149 | 151 |
| columns.layoutPrefabsInFolder | 149 | 151 |
| columns.meshAssetsInFolder | 447 | 453 |
| columns.distinctMeshesPlaced | 447 | 453 |
| columns.trianglesAllLodsOfPlacedMeshes | 414,604 | 429,558 |
| materials.scene | 328 | 330 |
| materials.modules | 36 | 48 |
| materials.shadowProxies | 29 | 33 |
| materials.sceneShaderVariants | 30 | 30 |
| materials.moduleShaderVariants | 6 | 8 |

Shadow proxy materials after (33): DK_Bark_Fibre, DK_Bark_Palm, DK_Beacon_Rose, DK_Bronze, DK_Chrome, DK_Coconut, DK_Crown_Gold, DK_Enamel_Charcoal, DK_Enamel_Coral, DK_Enamel_Ivory, DK_Enamel_Mustard, DK_Enamel_Seafoam, DK_Enamel_Teal, DK_Glass, DK_GlassDark, DK_Gold, DK_Hull_Ivory, DK_Iron, DK_Lamp, DK_Leaf_PalmFeather, DK_Leaf_Shrub, DK_Limestone_Tex, DK_Lobby, DK_Neon_Coral, DK_Render_White, DK_Signal_Amber, DK_Soil, DK_Steel, DK_Steel_Ivory, DK_Stone_Dark, DK_Terrazzo_Ivory, DK_Window_Film, DK_Window_Tower

Module shader variants before: ['DecoKit/Complex Lit Cutaway|_CLEARCOAT', 'DecoKit/Complex Lit Cutaway|_CLEARCOAT,_DETAIL_MULX2', 'DecoKit/Lit Cutaway|_DETAIL_MULX2', 'DecoKit/Lit Cutaway|_EMISSION', 'DecoKit/Lit Cutaway|_METALLICSPECGLOSSMAP,_NORMALMAP,_OCCLUSIONMAP', 'Suborned/DecoKit/GlassSky|']

after: ['DecoKit/Complex Lit Cutaway|_CLEARCOAT', 'DecoKit/Complex Lit Cutaway|_CLEARCOAT,_DETAIL_MULX2', 'DecoKit/Lit Cutaway|_DETAIL_MULX2', 'DecoKit/Lit Cutaway|_EMISSION', 'DecoKit/Lit Cutaway|_METALLICSPECGLOSSMAP,_NORMALMAP,_OCCLUSIONMAP', 'DecoKit/Lit Cutaway|_NORMALMAP', 'Suborned/DecoKit/GlassSky|', 'Universal Render Pipeline/Lit|']

Module roots by wall material after: {'DK_Limestone_Tex__apricot': 225, 'DK_Limestone_Tex__blush': 338, 'DK_Limestone_Tex__butter': 560, 'DK_Limestone_Tex__cream': 478, 'DK_Limestone_Tex__ivory': 266, 'DK_Limestone_Tex__lilac': 347, 'DK_Limestone_Tex__mint': 499, 'DK_Limestone_Tex__peach': 323, 'DK_Limestone_Tex__powder': 362, 'DK_Limestone_Tex__sand': 229, 'DK_Limestone_Tex__seagreen': 395, 'DK_Limestone_Tex__sky': 240, 'DK_Render_White__butter': 6, 'DK_Render_White__cream': 4, 'DK_Render_White__powder': 13, 'DK_Render_White__sand': 3}
