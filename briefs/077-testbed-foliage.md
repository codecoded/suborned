# 077 · Testbed foliage, then the rest of the testbed

Owner: Aster. Opened 2026-10-30. Sky: "we had some really good work done on that in the testbed project" → "yes. - review performance as you go and see what other foliage we can use from testbed and other features".

## Why
The testbed's planting (botanical atlas v2, café trough planting, the four fountain garden islands with their globe lamps, the civic forest made from the same leaves, flower head shapes) was inventoried in brief 028 and left "to port". Only the street palm came over (CoconutPalm_Street). This week's foliage work used other sources: Vela's procedural candidates (recovery075_foliage.py) and the old town's DK_FlowerBed / DK_ShrubMound (Latch's trial, HOLD: a step down from the domes at play zoom). The material pilot (076) rejected a leaf pattern on the current domes. None of them tried the testbed set.

## Sources (read only)
- Art: `~/Projects/the-suborned-store/testbed-art/cafe-plants/` (botanical-atlas/normal/orm-v2, foliage-*, soil-*, plaza-garden*.bin.gz + manifest, garden-composition-report, botanical-report-v2, imagegen-prompt-v2), `palm/`, `civic-fountain/`, `reference/`.
- Code: `~/Projects/the-suborned/testbed/src/`: cafe-planting(-layout), cafe-garden-composition, cafe-botanical-geometry, garden-botanical-geometry, garden-stem-geometry, flower-look-shapes, flower-bed-layout, fountain-garden(-layout, -lights), civic-planting-layout, civic-forest, layout-planting, layout-vegetation.
- Inventory: `briefs/028/inventory.html` (status per module).

## Sky, 2026-10-30: the domes go
"I'm also talking about plants in front of the cafe - they shouldn't exist in our kit at all at that design - they're low poly placeholder looking". Scope widens from the café troughs to the whole dome family: PlanterRun_2/4/Corner/End (A/B/C), BedTuft_A/B/C (the civic beds, 92 placements), PlanterArc (the fountain islands) and CafeTerrace's embedded planting, each re-made with testbed planting on its existing stonework, same piece ids and footprints. The dome versions are rejected in the catalogue once each replacement is in (no empty planters meanwhile). Old-town plants outside the zone (DK_FlowerBed, DK_ShrubMound and the rest) are a separate call.

## Sky, 2026-10-30: go
"yep agree, and yes, use LoDs at game play". Decided: the testbed's authored LODs (gameplay at play zoom); bed-level civic beds; the four globe lamps; the Records Office crown and café roof troughs included. Next: Vela rebuilds the original piece ids (PlanterRun_*, BedTuft_* where still used, PlanterArc, CafeTerrace, the crown and roof troughs) with the testbed planting and LODGroups, fixes the 5.5 cm soil gap at run joints; Splice adds the bed-level rule to the world generator and catalogues the pieces (domes rejected once each lands); Latch (+ Glint) adds vertex colour, back-face normal flip and the ORM-derived mask to the leaf shader; then Latch places everything default-off and Redline measures in Unity and on the Air; then Sky judges the Unity capture.

## Sky, 2026-10-30: planting is see-through
"yeah, see through". Planting carries no sight or walk volumes; its stonework does. Keel: the site fingerprint counts only volumes that matter (a render-only piece with none adds nothing), proven with Splice's in-memory adoption probe; then Splice adopts, the site is regenerated once, every Handover recording replayed (Suture/Waymark), re-recorded only where an outcome changes.

## Order
1. **Café troughs first (Vela).** The testbed's café trough planting as generated kit pieces (tools/decokit, deterministic, provenance + sha256, candidate status), the testbed's own atlas and leaf/flower geometry, at full detail (028: no compression without Sky). Fit the current PlanterRun troughs and the civic restyle; don't recolour the restyle.
2. **Review render:** testbed | now (domes) | proposed, at play zoom (14, 38°, yaw 30 and 210), close, day and night. Self-contained HTML. Sky sees it before anything replaces anything.
3. **Performance as we go:** per piece tris, vertices, materials/SetPass, texture memory (with mips), alpha-test or not, LOD plan; and the total for the café square at play zoom against today's domes. Flag anything that threatens 60 fps at play zoom on the Air (13 ms budget in Redline's 075 numbers). Redline reviews the numbers.
4. **Then** the fountain islands and their lamps, civic planting and the civic forest, in that order, each with the same review and cost line.
5. **Survey:** what else in the testbed (foliage and other features) is worth bringing over to the old-built town, against what the swap shows today.

## Limits
Fixed limits (CLAUDE.md). Only Latch edits scenes; this brief writes no scene, catalogue adoption, plan, sim or mission data. Paid calls: none planned; Codex stills at $0 if a texture needs a still. Model routing (Sky, 2026-10-29): 3D asset and appearance work on Opus. Load goes through `bun run guard`.

## Done-check
`briefs/077/troughs/index.html` (step 2) with `metrics.json` (step 3) · `briefs/077/survey.html` (step 5) · each marker `briefs/077/READY-<step>`.
