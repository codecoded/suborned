# 075 counts: the 074 build against the 075 rebuild (batchmode offscreen, estimates, not UnityStats)

Before: out/scratch/latch/074/pin/074.world.json sha256 67a3a07b6ffcc1b4ae9bcc8a822e6a3a8b61b56bbc38595deba19a009960d6c4 (2026-10-28 11:00). After: out/scratch/latch/075/pin/075.world.json sha256 ec0339f9e26ffc1b65a7f496d585d32c5f9e1efd8f3bd10796956bce3b3c1155 (2026-10-28 12:45).

| view | materials | SetPass est. (fwd) | shadow variants | main renderers | main tris | shadow tris | casters | modules in frustum | frame ms (median, indicative) |
|---|---|---|---|---|---|---|---|---|---|
| office-front y030 | 94 → 90 (-4) | 35 → 36 (+1) (19 → 20 (+1)) | 11 → 11 (+0) | 230 → 230 (+0) | 653,254 → 653,254 (+0) | 418,785 → 379,052 (-39,733) | 168 → 150 (-18) | 5 → 5 (+0) | 2.87 → 3.71 |
| office-front y210 | 88 → 86 (-2) | 36 → 37 (+1) (18 → 19 (+1)) | 12 → 12 (+0) | 213 → 213 (+0) | 635,578 → 635,578 (+0) | 384,005 → 353,609 (-30,396) | 147 → 144 (-3) | 0 → 0 (+0) | 2.29 → 3.62 |
| cell-open y030 | 149 → 143 (-6) | 41 → 42 (+1) (22 → 23 (+1)) | 12 → 12 (+0) | 292 → 292 (+0) | 769,027 → 769,027 (+0) | 772,955 → 774,571 (+1,616) | 333 → 332 (-1) | 18 → 18 (+0) | 3.53 → 4.62 |
| cell-open y210 | 150 → 142 (-8) | 42 → 43 (+1) (22 → 23 (+1)) | 13 → 13 (+0) | 273 → 273 (+0) | 752,426 → 752,426 (+0) | 696,405 → 648,531 (-47,874) | 330 → 315 (-15) | 15 → 15 (+0) | 3.57 → 4.64 |
| wide-lamplight y030 | 91 → 78 (-13) | 36 → 36 (+0) (17 → 18 (+1)) | 13 → 12 (-1) | 244 → 244 (+0) | 476,148 → 476,148 (+0) | 474,704 → 448,800 (-25,904) | 343 → 323 (-20) | 78 → 78 (+0) | 4.03 → 6.24 |

## Census

| | before | after |
|---|---|---|
| columns.placed | 2,265 | 1,913 |
| columns.singleModules | 2,389 | 2,389 |
| columns.layoutPrefabsPlaced | 165 | 149 |
| columns.layoutPrefabsInFolder | 165 | 149 |
| columns.meshAssetsInFolder | 495 | 447 |
| columns.distinctMeshesPlaced | 495 | 447 |
| columns.trianglesAllLodsOfPlacedMeshes | 573,512 | 414,604 |
| materials.scene | 349 | 328 |
| materials.modules | 58 | 36 |
| materials.shadowProxies | 29 | 29 |
| materials.sceneShaderVariants | 29 | 30 |
| materials.moduleShaderVariants | 6 | 6 |

Shadow proxy materials after (29): DK_Bark_Fibre, DK_Bark_Palm, DK_Beacon_Rose, DK_Bronze, DK_Chrome, DK_Coconut, DK_Crown_Gold, DK_Enamel_Charcoal, DK_Enamel_Coral, DK_Enamel_Ivory, DK_Enamel_Mustard, DK_Enamel_Seafoam, DK_Enamel_Teal, DK_GlassDark, DK_Gold, DK_Iron, DK_Lamp, DK_Leaf_PalmFeather, DK_Leaf_Shrub, DK_Limestone_Tex, DK_Lobby, DK_Render_White, DK_Soil, DK_Steel, DK_Steel_Ivory, DK_Stone_Dark, DK_Terrazzo_Ivory, DK_Window_Film, DK_Window_Tower

Module shader variants before: ['DecoKit/Complex Lit Cutaway|_CLEARCOAT', 'DecoKit/Complex Lit Cutaway|_CLEARCOAT,_DETAIL_MULX2', 'DecoKit/Lit Cutaway|_DETAIL_MULX2', 'DecoKit/Lit Cutaway|_EMISSION', 'DecoKit/Lit Cutaway|_METALLICSPECGLOSSMAP,_NORMALMAP,_OCCLUSIONMAP', 'Suborned/DecoKit/GlassSky|']

after: ['DecoKit/Complex Lit Cutaway|_CLEARCOAT', 'DecoKit/Complex Lit Cutaway|_CLEARCOAT,_DETAIL_MULX2', 'DecoKit/Lit Cutaway|_DETAIL_MULX2', 'DecoKit/Lit Cutaway|_EMISSION', 'DecoKit/Lit Cutaway|_METALLICSPECGLOSSMAP,_NORMALMAP,_OCCLUSIONMAP', 'Suborned/DecoKit/GlassSky|']

Module roots by wall material after: {'DK_Limestone_Tex__apricot': 214, 'DK_Limestone_Tex__blush': 344, 'DK_Limestone_Tex__butter': 521, 'DK_Limestone_Tex__cream': 495, 'DK_Limestone_Tex__ivory': 256, 'DK_Limestone_Tex__lilac': 334, 'DK_Limestone_Tex__mint': 524, 'DK_Limestone_Tex__peach': 337, 'DK_Limestone_Tex__powder': 398, 'DK_Limestone_Tex__sand': 227, 'DK_Limestone_Tex__seagreen': 418, 'DK_Limestone_Tex__sky': 234}
