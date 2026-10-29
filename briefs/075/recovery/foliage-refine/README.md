# Vela · foliage refinement · 2026-10-29

## Changed paths

- `tools/decokit/recovery075_foliage.py`: opt-in **unadopted** BedTuft_A/B/C and PlanterRun_4_A/B/C recipe. Original/testbed cutout sprays and bent normals retained; softer linear-light civic-green colour response, civic-green inner core, shallow five-lobed blooms. No production registration. Cypress branch unchanged.
- This folder: `report.html` (self-contained full-resolution gallery), `metrics.json` (all geometry/material/seed/bounds data), `delta.json` (proposed Latch integration delta), reproducible Blender script, previous generator snapshot, final log and rejected first-study evidence.
- Scratch only: `out/scratch/vela/foliage-refine/` contains six FBXs and `foliage-refine.blend`. No production or Latch input is overwritten.

## Evidence

```
bun run guard --wait-timeout 1 /Applications/Blender.app/Contents/MacOS/Blender -b -t 4 -P briefs/075/recovery/foliage-refine/check_render.py
python3 briefs/075/recovery/foliage-refine/make_report.py
git diff --check -- tools/decokit/recovery075_foliage.py
```

Final Blender pass: **FOLIAGE REFINE PASS**, 22 renders, six exact seeded-call comparisons, six non-plant polygon-position comparisons, six source-envelope containment checks and two placement-aware preflights against Latch's final-three selection. Cypress is unchanged. Four threads, guarded; no overlapping Blender generator was observed before starting. No unknown process was stopped. Scope does not require Unity or another full build/test suite.

Final generated FBXs and three source hashes independently rechecked: **match**. The report embeds the PNGs; full-size PNGs also remain adjacent. Inspected with view_image at original resolution: existing Unity paving/foliage trough detail, existing isolated before/candidate trough, all six refined close views, both source/previous/refined representatives at play yaw30, refined representatives at yaw210. Camera vertical span28 m at1080px, pitch38; play horizontal span49.7778 m. Isolated Cycles lighting/AgX is not Unity lighting/grade or a scene-level test.

Recipe trials: rejected first study left sparse patches over a smooth core; two representative images retained in attempt1-rejected. Its end-of-run generator hash was invalid after an edit during rendering and is explicitly removed/marked there. Subsequent assertions caught original ten-sided Y extrema and an incorrect preflight handedness mapping; both corrected before the authoritative final pass. Final run asserts generator source did not change during generation/rendering. Blender exits zero on some Python exceptions: use the final PASS marker and hash checks, not process exit alone.

Triangle counts (source → previous unadopted → refined):

| Asset | Source | Previous | Refined |
|---|---:|---:|---:|
| BedTuft_A |95|1946|1044|
| BedTuft_B |95|1946|1044|
| BedTuft_C |80|1442|926|
| PlanterRun_4_A |732|11628|7500|
| PlanterRun_4_B |672|9612|7028|
| PlanterRun_4_C |642|8604|6792|

This designs an unadopted recipe; no adopted asset is simplified, compressed, decimated or given changed LODs. More cutout sprays (48→80) replace expensive inflated petal solids. Lower triangle totals **do not prove lower alpha/GPU/shadow cost**.

## Findings and open issues

**HOLD adoption.** Close views materially improve leaf tonal response: less near-black/yellow mottling, more consistent civic-green leaves. Flowers lose the chunky bead silhouette. Leaf sprays still form scalloped layers, flowers can look like thin discs, and BedTuft_B/C and some C trough flowers intersect the canopy. **Reject B/C bloom presentation as finished art.** The full play-scale views show only modest benefit; this round does not establish old-town/testbed quality across all planting.

The original envelope constraint values remain fixed. Actual occupied geometry bounds are contained and smaller/different; **not identical extrema**. Flattened blooms lower occupied height. No global/source mesh clamp or imported-mesh fitting is used for the refined candidates. A spray is uniformly fitted about its attachment in recipe construction, preserving its curved shape. Previous candidates in the comparison reproduce their original exporter clamp for historical fidelity; this does not touch old captured evidence or refined output.

Two placement preflights use exact kit poses from current world data and source-world envelopes from Latch's final `bounds-proof.json`; the final cypress id is explicitly unchanged. FBX handedness convention (-BlenderX, BlenderZ, BlenderY), Unity rotation -yaw. These checks **do not replace actual Unity imported-vertex validation**, and do not reintroduce excluded tufts. No collider/nav/sight/route change; source dense topiary sight behaviour remains HOLD.

Material copies require an explicit review-only linear-light mix: 75% original DK_Leaf_Civic (#6A9B5C) or DK_Leaf_CivicDark (#46794C), 25% full-resolution shrub RGB. Same source alpha threshold .5, roughness .85, Blender specular IOR .1. New names `DK_Leaf_Refine075_Civic` / `...CivicDark` deliberately prevent accidental mapping to unchanged original leaf materials. FBX does not reproduce this node graph: material work is required before a Unity comparison. Source art was not edited/resized. Full source dimension/hash in metrics.

GPU timings, alpha overdraw, shadow cost, moving normals/alpha seams, shimmer and orthographic LOD behaviour remain unmeasured. Potted topiary, palms, arc/oval and embedded planting remain open. No full player/bot round, Air run, paid/image-model call, global kit generation, sim/plan/ratchet change, production enablement or commits.

## Handoff · inputs → done-check → next owner

**Aster → Redline/Glyph:** report.html, metrics.json, delta.json and six scratch FBXs. Review improved tone versus remaining layers/flat/intersecting flowers and modest play-scale benefit. No automatic adoption; B/C floral presentation rejected here.

**Latch, only after that review:** leave `civic/cyp-office-e` unchanged (actual Unity baseline 5004tri; delta0). Candidate replacement proposals remain only `civic/run-west-cafe/1` and `civic/civic-office-north/tuft-0`; no other id. Retain original trough/trunk and copy only plant triangles. Proposed refined additions:6968 +1044 plant triangles; remove original612 +95; net **+7305** against actual Unity baseline, versus previous +10791 (difference -3486). Same strategy would add two renderers and eleven plant material slots, not previously measured draw calls. All counts are proposals until integrated.

Create explicit scoped review materials with the recorded mix; never silently map them to DK_Leaf_Shrub. Independently validate imported normals/UVs, material resolution, exact retained non-plant attributes and actual placed1mm bounds **without clamping/rescaling**, unchanged transforms/colliders/nav and excluded-id set. Measure moving alpha/normal and actual cost only when Aster settles content. Keep all production/default-off behaviour. No Latch input, Unity source/scene or accepted evidence was edited by this task.
