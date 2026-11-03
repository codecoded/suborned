# Vela · focused spray arrangement · 2026-10-29

## Changed paths / decision

- `tools/decokit/recovery075_foliage.py`: one deterministic rigid arrangement pass for **BedTuft_A and PlanterRun_4_B only**, inside the existing opt-in/unregistered candidate builder. Stores existing spray groups, rotates/translates them and rotates their bent normals. No additional sprays, triangles, slots, remapping, new scaling, mesh clipping or post-import fitting. Other candidate families do not invoke the pass.
- New artifacts only in this folder and `out/scratch/vela/foliage-arrangement/`. `previous_recipe.py` pins the rejected prior arrangement; prior reviewed renders/metrics/FBXs remain immutable.

**REJECT adoption and Unity trial for BOTH resulting selections.** This focused correction does not meet the visual done-check. It improves several flower faces and breaks the horizontal shelves, but exposes smooth core domes and produces leaf collars/cabbage-like crowns. Some trough flowers remain partial. Full1080p comparisons do not establish a positive silhouette benefit. The mechanical PASS is not an appearance pass.

## Evidence

Read Glyph's entire `../foliage-appearance-review/README.md`. Inspected the actual flagged prior tuft/trough PNGs and `briefs/026/vela/testbed/streamline-daylight-cafe.png` at original resolution. Process inspection before generation found no running Blender generator; only MCP service processes/thumbnailer, plus existing Claude processes which were not contacted or stopped. No child, paid call, Unity, global kit generator or player/bot/Air round.

Commands:

```
bun run guard --wait-timeout 1 /Applications/Blender.app/Contents/MacOS/Blender -b -t 4 -P briefs/075/recovery/foliage-arrangement/check_render.py
python3 briefs/075/recovery/foliage-arrangement/make_report.py
git diff --check -- tools/decokit/recovery075_foliage.py briefs/075/recovery/foliage-arrangement
```

Blender: **FOLIAGE ARRANGEMENT PASS**,16 renders. The marker explicitly separates appearance. No Python exception in the final log. Four threads under play guard. All16 full PNGs inspected with `view_image(detail="original")`: both previous/corrected, close/play, yaw30/210. Same camera/lighting as prior studies: pitch38, Cycles16samples/AgX, world.35, area1800W at(-3,-4,9),size7; close1200×800 at1.2m/5m horizontal scale, play1920×1080 at49.7778m horizontal/28m vertical span. Not Unity lighting/grade or actual imported-vertex evidence.

### Mechanical proof

| Selection | Triangles previous → arrangement | Material slots | Sprays | Maximum rigid pairwise distance error |
|---|---:|---:|---:|---:|
| BedTuft_A |1044 →1044|4 →4|80 →80|9.61e-8 m|
| PlanterRun_4_B |7028 →7028|8 →8|640 →640|1.83e-7 m|

Both have identical per-material triangle counts and UV coordinates. Every non-spray polygon's material and vertex-position list is identical to the prior candidate, **including all blooms, opaque cores and troughs**. Exact seeded mound/bloom call lists match. Existing curved spray pairwise distances prove rigid transformations within2µm numerical tolerance; no spray is shrunk/deformed. Current source construction's earlier per-spray sizing remains unchanged before rearrangement; the new pass only rotates/translates those existing shapes. All existing geometry outside plant triangles remains unchanged. No trunk/cypress regenerated.

Material function `leaf_material` is AST-identical to the prior generator. Exact slot mapping and current civic-green recipe preserved:75% seeded civic green +25% original source RGB in linear light, unchanged alpha threshold.5, roughness.85 and specularIOR.1. Original source texture SHA/dimensions match prior metrics and current bytes. No texture copy/repaint/downscale.

Both unchanged **source-envelope constraints** and exact proposed placement preflights pass at1mm. Poses/source-world boxes equal prior metrics; no excluded tufts. Actual occupied extrema differ after reorientation, so this is containment within fixed bounds, not identical mesh extrema. `metrics.json` records every before/after bound and placed plant box. Source-space FBX handedness/pose preflight is not actual Unity import validation. No imported mesh is scaled/clamped.

`verification.json`: all40 pinned prior evidence/FBX hashes unchanged; three recorded source hashes and both new FBX hashes match; texture and material-function checks pass. Prior geometry delta remains+7305 against actual Unity baseline (trough+6356, tuft+949); no increased material/renderer proposal. Cypress baseline remains5004tri, delta0.

## Visual failures / limits

Tuft yaw30: the cream front flower no longer has a leaf crossing its upper/right face; yaw210 floral faces are clearer too. But clearance creates a broad bare core beneath the flowers at both headings. Upright side sprays now read as a collar around a smooth dome. This fails continuous convincing planting.

Trough yaw30: fifth pale front flower clears the old upper-edge bite, third/sixth rear marks are fuller, and directions vary. The row now has bare domes and repeated upright collars. Yaw210 still contains partial lower/rear flower crescents. The search recorded69 conservative projected bloom-rectangle overlaps among640sprays; **not69 proven visible cuts**. Tuft has0 such residuals, yet fails appearance. The unchanged opaque core may contribute to remaining floral concealment; that occlusion source was not independently isolated. No all-flower-clearance claim.

At full1080p both headings, the changed row edge is perceptible but is a broken/noisier silhouette rather than a convincing gain; the tuft remains a small mark. The café reference's directional leaves and small clear blooms over a continuous edge are not met. This rejects **this arrangement**, not every mathematically possible arrangement. One focused correction only; no extra variants/triangle addition were pursued to conceal failure.

GPU/alpha/shadow cost, motion seams, shimmer, orthographic LOD and target frame time remain unmeasured. No claimed cost improvement from unchanged counts. Geometry/sight HOLD, G-3a59>58 blocked and production foliage default-off persist. Palms, topiary and embedded planting remain open.

## Handoff · inputs → done-check → next owner

**Aster → Glyph for a NEW independent review.** Inputs: self-contained `report.html`,16 adjacent full PNGs, `metrics.json`, `verification.json`, `reference-hashes.json` and two scratch `*_Arrangement075.fbx` files plus blend. Done-check: independently assess flower clearance, lost canopy coverage and absence of a positive play-scale silhouette gain; confirm no Unity trial/adoption proceeds from this rejected result. Review has not been performed by this implementation task, and no child was started.

**Latch gets no integration authorization.** No changes to his inputs, tram helper, Unity, accepted/captured evidence, source art, global production kit/catalogue, restyle, plan/sim/ratchets. No commits. Aster decides any subsequent work after Glyph's independent review; this round does not silently expand scope or change bloom/core geometry.
