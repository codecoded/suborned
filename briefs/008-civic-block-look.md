# 008 · The civic block reads as the original paintings

**Status (2026-10-14): A done (`out/review/008-civic-parts.html`). B started:** Sky approved the first cut and the three calls ("go ahead, do what ya need to").

Goal (Sky, 2026-10-14, after walking the generated world): "with these placeholders, like the café, how can we get back to the high quality café we had before this?" The café is a plain mass with loose tables on bare ground; the Records Office and the square are no better. This is the block the Handover is played on, and Sky signs the world off by walking it.

## Decisions (Sky, 2026-10-14)
- **The reference is the eight paintings of 2026-09-26**, in `briefs/008/concepts/` (copied unchanged from `~/Projects/ai-game-dev/docs/`, hashes in `PROVENANCE.json`). They are the reference for the civic block and for the street-level character of the district: cream stone, seafoam teal bands, chrome spheres, banners, planted terraces, pale paving with teal bands, cypress and palms, parasol terraces.
- **The skyline keeps its own reference:** the AtomicAge and Blocks sheets of brief 007.
- **The block's geometry does not change.** The café stays 22 × 11.3 × 34.8 m and the Records Office 15.5 m, with their entrances and bays (`contracts/world/civic-block.json`, rule C-0). In the paintings the café is a curved glazed bay with a banded canopy at the foot of a taller terraced building, which is what the corner building becomes.
- **The 30 Sep capstone concept is not the reference**, nor are the café and Records Office models built from it (`~/Projects/the-suborned-store/testbed-art/cafe-3d`, `civic-office`): they are different buildings, a low L-shaped pavilion and a domed office. Their dressing parts may be reused: parasols, table sets, plants, fountain materials, the boarding pavilion.
- **This block is finished first**, ahead of the rest of the look work (stage 3), so Sky can judge the world and the camera on one real corner.
- Owed: record the reference in the next GDD version (§15).

## A · Vela · parts list (no Unity, nothing built)
objective: compare the paintings with the civic block as it stands in the export, and list what must exist for the block to read as the paintings from the play camera and at street level.
For each part: what the painting shows · what the world has now (export id) · where it comes from (adopted kit piece, candidate kit piece, reusable testbed-art part, new DecoKit piece, generator change, block adjustment) · owner · rough size and triangle budget.
Flag every conflict with the locked geometry, with the Handover (`contracts/missions/handover/mission.json`: stands, entrances, bays, sightlines) and with the style contract's palette.

done-check: every element visible in `mission-block` and `gameplay-plan` is in the list; every part names a source and an owner; the parts are ranked by how much they change the picture; nothing is built or adopted.
evidence: `out/review/008-civic-parts.html`.
next owner: Sky reads it and picks the cut; then B.

## Decisions on A (Sky, 2026-10-14)
- **The first cut is built in full:** parts P1 to P10 of the parts list, the Records Office entrance included.
- **The play camera keeps yaw 30.** The Records Office public front faces away from it, so that front is an eye-level part for now. What tall buildings hide is solved by the GDD rule (§15: tagged architecture ghosts to 0.2 with a ghost shadow), which is built now; the camera is not turned.
- **The entrance points move to the tower faces** (parts list L1, option B). The towers are not carved.
- **Palette on this block:** seafoam and deep teal are the kit enamels (`DK_Enamel_Seafoam` #72B5A7, `DK_Enamel_Teal` #1F6C67). Banners, flowers and any tram use the contract's coral, flamingo or peach, never the paintings' red-orange (ΔE 8.8 from the cones signal). Gold is hairline, frame and bead only; the full-height gold fins come off.
- Canopies, the marquee, planter shelves and parasols that stand outside a building's ring are dressing, not a change to the locked geometry (L3).
- Steps are real geometry. Vantage sets rise and landing so the entrance points stand on the landing; how the sim takes the height is Keel's, later (H4).

## B · build
One finished corner, generated like everything else: block adjustment or generator rule, never hand-placed.
- **B1 · Vela · pieces.** Lean DecoKit pieces at the parts list's triangle budgets: café bay with canopy and sign above it, Records Office entrance (marquee, arch and globe, banners, doors, piers, steps), striped parasol, sphere bollard, modular planter run in three planting variants, the square-bay shelter; play-zoom versions of the table set, sphere lamp and cypress; the paving material and band design; the colour roles and pool water. Delivered as candidates with a catalogue proposal; `kit.json` is not touched until B3.
- **B2 · Vantage · block.** In `civic-block.adjust.json`, each with a reason: entrance points to the tower faces, the official canopy clear of the tower, steps and landings, and the zones dressing may use and must keep clear (crew start line and getaway bay, the fountain's east and west axes, the Handover points under parasols: H2, H3, H7).
- **B3 · Splice · generator and catalogue**, after the walk-fault batch: adopt B1's pieces, swap the café's ground storey, emit paving bands, place planters, lamps, bollards, parasols and banners by B2's zones, apply the colour roles.
- **B4 · Latch · fade and rebuild.** The architecture fade of GDD §15, then the rebuild.
- **B5 · Glint · light.** Reflection probes for the square and the Office front, downlights, pool water.
- **B6 · Redline · measure** the block on the Air at play zoom against the 60 fps target.

done-check: `bun run check` and the Handover overlay pass; 0 console errors; three shots beside their paintings (play camera home frame, eye level at the café, eye level at the Office door); new LOD0 triangles on the block ≤ 110k; frame time reported.
next owner: Sky walks the block.

Owed, outside this cut: an entrance camera site at the Office door (route A loops it; Datum, H5) · `DK_Enamel_Coral` #E07560 is ΔE 12 from the cones signal and is bound by 59 kit pieces (Vela, C2) · the reference and palette decisions into the next GDD version.

## Known faults (Sky's walk, 2026-10-14)
- On this block: the official bay runs into the Records Office tower (`civic/bay/officeBay` against `civic/civic-office/tower-official`). Owner: Vantage.
- On the Parade, not this block: a café's blade sign passes through its canopy (`bld/parade-north/12`; Sky's note reads "Cafe sound is occluded", taken to mean the sign, not confirmed). A checks whether the civic café's frontage has the same fault. Owner: Splice.
