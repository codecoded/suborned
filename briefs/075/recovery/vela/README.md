# Vela · cafe/civic asset recovery · 2026-10-29

## Changed and recovered

`tools/decokit/recovery075_foliage.py` adds opt-in, **unregistered** foliage candidates. `check_render.py` builds seven LOD0 candidates into `out/scratch/vela/recovery/`, exports FBXs using the kit's axes, enforces each original mesh's exact bounding envelope and renders local before/after evidence. It never runs the shared kit pipeline. Production meshes, materials, restyle JSON, colliders, plan and Unity files are not edited.

Recovered work: `briefs/075/civic/restyle.json`, its before/after/alternative renders and `index.html`, and Latch's `READY-restyle` record. The existing restyle already textured the planter walls and bed kerbs, but expressly **kept** the coarse foliage and put both civic floors on pale ivory terrazzo. That is the outstanding work, not a lost completed foliage pass. No conflicting active generator/writer was observed; an older Suborned Claude process remained present and was neither contacted nor stopped.

The candidates reuse `plants.py`'s original shrub cutout and bent-normal card construction. A/B/C keep their seeded planting positions and flower colours. Rounded petal geometry replaces the pentagonal bloom caps. Cypress reuses the original `plants.cypress` curved cards (same seed/profile as `Cypress_Play`), contained to the original play piece's bounds. No new textures, paid generation, caps, LODs, decimation or texture resizing.

Texture source paths, full dimensions and SHA256s, candidate FBX paths/hashes, exact triangle counts, bounds and material slots: **`metrics.json`**. Keep `out/scratch/vela/recovery/foliage-candidates.blend` and its adjacent FBXs until integration; regenerate with the script if scratch is removed. The source-art store is read-only.

## Evidence

Command (three sequential guarded passes, final pass is authoritative):

```
bun run guard --wait-timeout 1 /Applications/Blender.app/Contents/MacOS/Blender -b -t 4 -P briefs/075/recovery/vela/check_render.py
```

Final success requires `RECOVERY075 PASS` in `out/scratch/vela/recovery/render.log`; Blender may exit zero on a Python exception, so exit code alone is insufficient. Seven bounds-containment assertions and exact polygon/position comparisons for **all non-plant parts** (troughs and cypress trunk), plus no DECIMATE modifiers and both full-size texture existence checks. The envelope check is on source mesh geometry before FBX export; Unity import has not been checked.

Eight local renders: before/candidate for `BedTuft_A`, `PlanterRun_4_B`, `Cypress_Play`, and ivory terrazzo/old flag paving on the same slab. Inspected shrub and trough candidates: leaf edges and rounded blooms replace the smooth polygon caps. A/B/C have invariant checks; only the stated representative variants are rendered. These are neutral Cycles asset studies, **not** Unity play-zoom, grade, occlusion or performance evidence.

`bun test tools/decokit`: **23 pass / 2 fail**. Log copied to `generator-tests.log`. Failures are in `fit.test.ts`: catalogue has 11 tower orders absent from the current plan, and SpaceHQ's generated order hash differs from the plan. No civic foliage piece appears in the failure table. This run does not fix those other owners' state.

## Latch handoff (inputs → done-check → next owner)

Input: `delta.json` + `metrics.json` + seven scratch FBXs + existing civic restyle. **Do not replace the global kit meshes or regenerate kit.collide.** These are scoped render-only candidates for the existing cafe/civic placements. Keep all original object ids, transforms, troughs, collider components, cover/nav data, plant/bed bounds and route boundaries. Substitute the visual mesh only. Resolve material slots by their exact kit material names, never by index; continue the existing DK_Limestone → DK_Limestone_Tex restyle on troughs. No collider from a candidate.

Paving: Sky sent the pale flat field back; use the **existing** kit `DK_Pave_Flags.mat` on `plot/records-square` and `plot/records-office`, with `tile: 4` (world-metre UV / 4; material BaseMap scale is 1). This reuses the old-town bond and its normal/mask, preserving inlays and all ground vertices. `delta.json` is a proposal for your later integration; shared `restyle.json` and `civic-restyle.json` are untouched. Scope does not include the cafe terrace's own floor mesh or any district-wide ground.

Done-check, when integration is scheduled: compare the same four cafe/office cameras against READY-restyle; ensure flags read at zoom 14/yaws 30 and 210 and inlays stay legible; inspect leaves for alpha halos, hard clamped edges, shimmer and normals after FBX import. Compare collider/nav/sim hashes and every placement/transform; geometry differences must be confined to foliage render meshes. Keep the baskets and all door/gate clearances. Measure renderer/material count, visible triangles, shadow passes and alpha overdraw with the same views before deciding how many candidates to adopt. Latch owns integration, Suture/Glyph readability review as appropriate; Aster schedules these, with no Air run in this recovery.

## Open issues and later measurement needs

This is an asset handoff, **not a shipping performance verdict**. BedTuft A/B rise 95 → 1946 triangles, C 80 → 1442; 4 m trough A/B/C rise 732/672/642 → 11628/9612/8604; cypress 1488 → 5004. Rounded flowers account for much of the increase. Blanketing all 92 bed tufts and 12 troughs with these near-detail candidates would add substantial geometry and alpha overdraw. No timings, GPU cost, memory delta or draw-call delta measured; texture reuse does not establish zero performance cost. Do not silently enable every candidate.

Later LOD need: measure projected plant size and visibility through all four play headings first; the orthographic camera may never select distance LODs. Any later LOD or flower/card reduction needs a separate authorized round and silhouette/alpha comparison. No compression performed here.

PottedTopiary_Play's three flat tiers remain unresolved; PottedPalm_Small, PlanterArc/OvalPlanter and CafeTerrace's embedded planting are unchanged. Their old/testbed equivalents should be compared at fixed bounds before another scoped pass; swapping whole old props would change planter footprints. The new details deliberately stop at the seven concrete candidates, rather than expand into every district plant.
