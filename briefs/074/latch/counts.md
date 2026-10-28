# 074-C counts: today against colourways (batchmode offscreen, estimates, not UnityStats)

Before: out/scratch/latch/072/g/g.world.json sha256 6841dcfc6f91ddea79ec6f275e5191f516dc91ffad884005c85724decbe1ce88 (2026-10-28 10:42). After: out/scratch/latch/074/pin/074.world.json sha256 67a3a07b6ffcc1b4ae9bcc8a822e6a3a8b61b56bbc38595deba19a009960d6c4 (2026-10-28 11:00).

| view | materials | SetPass est. (fwd) | shadow variants | main renderers | main tris | shadow tris | modules in frustum |
|---|---|---|---|---|---|---|---|
| office-front y030 | 88 → 94 (+6) | 35 → 35 (+0) (19 → 19 (+0)) | 11 → 11 (+0) | 230 → 230 (+0) | 653,254 → 653,254 (+0) | 418,785 → 418,785 (+0) | 5 → 5 (+0) |
| office-front y210 | 85 → 88 (+3) | 36 → 36 (+0) (18 → 18 (+0)) | 12 → 12 (+0) | 213 → 213 (+0) | 635,578 → 635,578 (+0) | 384,005 → 384,005 (+0) | 0 → 0 (+0) |
| cell-open y030 | 137 → 149 (+12) | 41 → 41 (+0) (22 → 22 (+0)) | 12 → 12 (+0) | 292 → 292 (+0) | 768,881 → 769,027 (+146) | 772,663 → 772,955 (+292) | 18 → 18 (+0) |
| cell-open y210 | 134 → 150 (+16) | 42 → 42 (+0) (22 → 22 (+0)) | 13 → 13 (+0) | 273 → 273 (+0) | 752,384 → 752,426 (+42) | 696,259 → 696,405 (+146) | 15 → 15 (+0) |
| wide-lamplight y030 | 62 → 91 (+29) | 36 → 36 (+0) (17 → 17 (+0)) | 13 → 13 (+0) | 244 → 244 (+0) | 475,986 → 476,148 (+162) | 474,338 → 474,704 (+366) | 78 → 78 (+0) |

## Census

| | before | after |
|---|---|---|
| columns.placed | 2,265 | 2,265 |
| columns.singleModules | 2,389 | 2,389 |
| columns.layoutPrefabsPlaced | 165 | 165 |
| columns.layoutPrefabsInFolder | 165 | 165 |
| columns.meshAssetsInFolder | 495 | 495 |
| columns.distinctMeshesPlaced | 495 | 495 |
| columns.trianglesAllLodsOfPlacedMeshes | 573,512 | 573,512 |
| materials.scene | 289 | 349 |
| materials.modules | 14 | 58 |
| materials.shadowProxies | 29 | 29 |
| materials.sceneShaderVariants | 29 | 29 |
| materials.moduleShaderVariants | 6 | 6 |

Shadow proxy materials after (29): DK_Bark_Fibre, DK_Bark_Palm, DK_Beacon_Rose, DK_Bronze, DK_Chrome, DK_Coconut, DK_Crown_Gold, DK_Enamel_Charcoal, DK_Enamel_Coral, DK_Enamel_Ivory, DK_Enamel_Mustard, DK_Enamel_Seafoam, DK_Enamel_Teal, DK_GlassDark, DK_Gold, DK_Iron, DK_Lamp, DK_Leaf_PalmFeather, DK_Leaf_Shrub, DK_Limestone_Tex, DK_Lobby, DK_Render_White, DK_Soil, DK_Steel, DK_Steel_Ivory, DK_Stone_Dark, DK_Terrazzo_Ivory, DK_Window_Film, DK_Window_Tower

Module shader variants before: ['DecoKit/Complex Lit Cutaway|_CLEARCOAT', 'DecoKit/Complex Lit Cutaway|_CLEARCOAT,_DETAIL_MULX2', 'DecoKit/Lit Cutaway|_DETAIL_MULX2', 'DecoKit/Lit Cutaway|_EMISSION', 'DecoKit/Lit Cutaway|_METALLICSPECGLOSSMAP,_NORMALMAP,_OCCLUSIONMAP', 'Suborned/DecoKit/GlassSky|']

after: ['DecoKit/Complex Lit Cutaway|_CLEARCOAT', 'DecoKit/Complex Lit Cutaway|_CLEARCOAT,_DETAIL_MULX2', 'DecoKit/Lit Cutaway|_DETAIL_MULX2', 'DecoKit/Lit Cutaway|_EMISSION', 'DecoKit/Lit Cutaway|_METALLICSPECGLOSSMAP,_NORMALMAP,_OCCLUSIONMAP', 'Suborned/DecoKit/GlassSky|']

Module roots by wall material after: {'DK_Limestone_Tex__apricot': 214, 'DK_Limestone_Tex__blush': 344, 'DK_Limestone_Tex__butter': 557, 'DK_Limestone_Tex__cream': 567, 'DK_Limestone_Tex__ivory': 256, 'DK_Limestone_Tex__lilac': 334, 'DK_Limestone_Tex__mint': 588, 'DK_Limestone_Tex__peach': 337, 'DK_Limestone_Tex__powder': 474, 'DK_Limestone_Tex__sand': 227, 'DK_Limestone_Tex__seagreen': 490, 'DK_Limestone_Tex__sky': 266}
