# 075 — direct original OldTown foliage reuse

Review-only handoff to Latch after tram inputs settle. Production unchanged. Both rejected recovery recipes remain untouched/default-off. No Unity, shared generation/import, assets, collider/nav/simulation changes, paid calls or crew children.

## Deliverables

- `report.html`: static comparison report with full PNG links.
- `comparison-close.png`, `comparison-play.png`: inspected comparisons of current original civic planting and direct original OldTown reuse. Play sheet crops native pixels; full 1920×1080 images retain the equivalent 14 m vertical half-height (horizontal ortho span 49.7778 m), 38° pitch. Same area light, world, Cycles 24 samples, AgX, camera and resolution for both states. Yaws 30 and 210 are isolated asset-local camera headings, not scene captures at the actual world's orientation. Latch must check actual world yaws after tram.
- `source-gallery.png`: all nine intact original sources, with original original-source materials and full-resolution textures. Each original source PNG is also supplied.
- `manifest.json`: source SHA256s, exact FBX/material/texture paths and GUID bindings, texture dimensions, shader properties, placement transforms/world rings, original material-slot mapping, local/conservative world bounds, triangle costs, render cameras and filenames.
- `audit.json`: all nine original/current FBXs imported and compared, plus civic baselines. Full hashes, triangle counts/material slots, topology/UV/smoothing/normals/vertex distinctions.
- `distinctions.json`: original/current material properties, importer settings, prefab hashes, available LOD files.
- `verification.json`, `mesh-verification.json`: input pin and exact submesh checks.
- Reproduction scripts: `audit.py`, `compare.py`, `verify.py`, `finish.py`. Scratch scene: `out/scratch/vela/oldtown-foliage-reuse/direct-reuse.blend`. This is an evidence scene, not a production asset/export.

## What corresponds to the old look

`DecoKitDistrictDressing.cs:50–60` uses PottedFern at both terrace ends and HangingBasket on its piers. This is the actual old café terrace selection, not a newly invented fern spray. The full original fern is shown in the source gallery; no new fern placement is proposed because moving a whole pot requires scene clearance validation.

`DecoKitGardens.cs:58–88` uses Cypress along the canal, FanPalm around the pool, and deterministic 60/40 ShrubMound/FlowerBed ground planting. `DecoKitPlazaLayout.cs:104–115,126–128` combines PlanterBed with two intact FlowerBeds at local ±1 m, at its soil elevation; lines 138–148 pair FanPalm with PlanterHex and use CoconutPalm and ShrubMound. The flower trough test carries over those two original intact flower meshes and their ±1 m spacing. It keeps the current café container, not the oversized original PlanterBed/prefab collider.

Original shrubs and flowers are textured cutout-card meshes over a core. They are not the current opaque civic dome/bloom recipe (`civic_street.py`, `civic_common.py`). No internal leaf, bloom, card, silhouette, UV, normal or tint arrangement was changed. Original below-ground droop is retained; no clamp, decimation, resampling or anisotropic scale was used.

## Concrete scope and cost

| Existing placement | Original selection | Uniform render-only scale | Current triangles | Reuse triangles | Delta |
|---|---|---:|---:|---:|---:|
| civic/run-west-cafe/1 | two complete DK_FlowerBed meshes, x = −1,+1; local z .645 | .75 | 672 | 5,496 | +4,824 |
| civic/run-west-cafe/0 | seven complete DK_ShrubMound meshes, x = (i−3)×.52; local z .645 | .48 | 732 | 17,742 | +17,010 |
| civic/cafe-palm-0 | original DK_PottedPalm_Small slots 7 Petiole + 8 Leaf_PalmFan | 1 | 2,396 | 2,396 | 0 |

All local planting y offsets are 0. Source pivots are retained. The manifest contains exact world at/base/yaw, local transforms and world-bound conversion. Scale is fit-only, not an approval to reduce/warp geometry. Seven shrubs follow existing trough planting pockets; each shrub remains intact. This is a representative fit test, not a new production recipe.

Current trough slot 0 (60 triangles) remains exact, including the active café `DK_Limestone → DK_Limestone_Tex` restyle binding. Palm slots 0–6 (pot, soil and trunk) remain exact, including the existing Mustard→Seafoam and Coral→Ivory world material overrides. Only its original petiole/fan submeshes use the original OldTown material bindings. No whole original planter or collider was copied. No additional soil slab was invented to fill the spaces exposed between smaller intact plants.

Total: 25,634 vs 3,800 triangles (+21,834). Original reused alpha-test triangles: 21,760 (flower 5,120; shrub 16,128; palm 512), compared with 512 alpha-test triangles in these current baselines. Expected unbatched visible material submissions: 31 vs 19, across 13 vs 3 renderers; batching, shadow passes, depth passes, alpha coverage and actual GPU cost are unmeasured. Source flower and shrub data are shared by their instances. Retained slot arrays include unused slots; count only nonempty submeshes when implementing. This is not performance-free.

## Old/current distinctions

- PottedTopiary, TopiaryCone, Cypress, FanPalm, CoconutPalm are byte-identical source FBXs. They are not upgraded by swapping bytes.
- PottedPalm_Small and PottedFern differ in FBX bytes but import with identical vertices, topology, material slots, UVs, smoothing and corner normals. No geometry degradation established.
- ShrubMound: same 2,526 triangles, topology, UVs and slot assignment. Current clamps 40 below-ground vertices to z≈0, at most .0609274 m. FlowerBed: same 2,718 triangles; 200 vertices clamped, at most .0869781 m. Imported corner normals differ. No triangle reduction or above-ground rearrangement established.
- Compared importer fields match. Original source prefabs have no serialized LODGroup. Current small palm and Cypress have two additional LOD files; source-file presence does not establish which runtime LOD is selected. `KitLods.cs` replaces source LOD wiring and remaps LOD materials from the current library; Latch must inspect actual runtime choice, especially for the same-byte topiaries/Cypress.
- Original/current foliage material paths use different shader GUIDs (OldTown vs current cutaway shader namespaces). Relevant compared leaf tint/cutoff/cull/smoothness values match; investigate textures and runtime shading, not an invented degraded mesh. Original pot gold differs from current satin brass (metallic 1→.6, smoothness .7→.6); this task retains the current pot. Texture GUID changes alone do not demonstrate different image content.

## Material/shader fidelity and limits

Every imported FBX's external material GUID resolves to the named original/current `.mat`; the render script asserts it. All referenced texture files resolve. Original source material properties drive Blender nodes: sRGB BaseColor converted to linear; original BaseMap times that colour; original UV/ST; full source image dimensions; alpha > original Cutoff, two-sided where Cull=0; original metallic/smoothness/coat and normal maps. No convenient recolouring, generated atlases or new texture variants.

The original shader is present, not missing: `OldTownArt/DecoKit/Shaders/DK_LitCutaway.shader`, GUID `92902edf0ab892fb40f51a8701887fed`, derived from URP Lit; its Complex Lit companion is also pinned. Blender uses a documented Principled approximation, not the actual URP shader. Exact URP BRDF, MSAA AlphaToMask, reflection probes, cutaway, scene grade, filtering/LOD and shadow response cannot be certified by this isolated study. Cutaway is inactive in the isolated comparison. No missing texture or shader source blocker was found. Final old-look equivalence needs Unity.

## Inspection verdict

All 24 comparisons and nine source views were inspected through contact sheets. Current civic opaque dome silhouettes clearly differ from original textured shrub/flower cards. Direct reuse remains visibly detailed, with obvious card structure close up. Under the fit-only scales it is lower and more separated than the civic hedge: two flower strips have a central gap; seven shrubs leave exposed spaces. Those are visible consequences of preserving source proportions and the container, not permission to redesign the source meshes. The palm comparison is effectively unchanged, as its geometry/material semantics already match. At equivalent play zoom, fine texture detail diminishes strongly; the lower mass and gaps remain visible. This is auditable direct reuse evidence, not an aesthetic acceptance or clearance/performance pass.

## Commands and evidence

From repo root:

```sh
bun run guard /Applications/Blender.app/Contents/MacOS/Blender -b -t 2 --python briefs/075/recovery/oldtown-foliage-reuse/audit.py
bun run guard /Applications/Blender.app/Contents/MacOS/Blender -b -t 4 --python briefs/075/recovery/oldtown-foliage-reuse/compare.py
bun run guard /Applications/Blender.app/Contents/MacOS/Blender -b -t 2 --python briefs/075/recovery/oldtown-foliage-reuse/verify.py
python3 briefs/075/recovery/oldtown-foliage-reuse/finish.py
```

Blender 5.2.2 LTS: completed. 33 renders present. All 74 render-input SHA256s still match. Original retained/reused vertices, faces, material indices, UVs, smoothing and custom normals verified against freshly imported FBXs; all three combined local renderer envelopes fit their current envelope within 1 mm. Uniform scales verified. Render completion is supplemented by the mesh verification and visual inspection; no game behaviour claim.

## Next owner — Latch, once tram inputs settle

Inputs: this manifest, source FBXs/materials/textures, exact submesh selections and fit-only transforms, original/current distinctions, renders and mesh verification. Keep this evidence, the tram inputs and rejected foliage default-off state.

One scoped Unity review: bind the original foliage submeshes/materials explicitly (avoid automatic library-name remapping), retain existing trough/pot/trunk geometry and live transforms/colliders/nav/sim, verify imported renderer bounds and actual world yaws 30/210 at close and play zoom, door/route/sight boundaries, original texture import/filter/alpha/shadow/probe response, and actual LOD selection. Compare palm without claiming a same-geometry upgrade. Do not touch dense topiary sight behaviour. Check frame/submission/alpha cost; escalate aesthetic gaps or fit conflicts instead of warping original plants or filling them with invented foliage. No production adoption is authorized by this evidence handoff alone.
