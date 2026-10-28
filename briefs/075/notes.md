# Brief 075 · coordination notes (Vela 074-H and Vela 075-B share tools/decokit)

Log every decokit run here before starting it (start / end, who, command). Do not run decokit while another run is open.

- 11:58 START Vela 074-H: bun run guard bun run decokit --mats-only (colourways.py: walls + plinth only, WALL_SOFTEN 0.35)
- 11:59 END   Vela 074-H decokit --mats-only (ok, 52 s)
- 12:01 Vela 074-H done with tools/decokit (colourways.py only; no further decokit runs from 074-H). Note for 075-B: SLOTS is wall+plinth, WALL_SOFTEN 0.35 lerps toward the old sheet's mean; if 075-B swaps the wall sheet, the neutral sheet/oldCream in colourways.py follow SRC_SHEET.

## Vela 075-B (textures and materials), 12:06
- Files I touch: tools/decokit/ground_tex.py (pavement and lawn targets), road_tex.py + road_ribbon.py (DK_Ground_RoadEnamel: the old asphalt_road_v1 sheet in powder blue), mats.py (GLASS_FILM sky colours only), unity look.json `road-enamel` tile and smoothness only. Not colourways.py (074-H's; done).
- Water split (proposal to Glint, 075-A): Glint owns the sea and harbour water (look.json `water`, Look.Water, the old DK_Ocean shader, DK_Sea_Normal, sky/probes/grade). Vela owns only kit water materials on pieces (DK_Pool_Water, the fountain's DK_Water/DK_Spray), which stay at the old values (they are the legacy .mats unchanged). Glint: say here if you want otherwise.
- Glass: the old windows' look came from 17 baked box-projected reflection probes (DecoKitAtmosphere.Probe, 256 px) plus the grade; that is Glint's (075-A). I port only the film's fake sky to the old street gradient (DK_Sky_Street.png).
- 12:07 START Vela 075-B: bun run guard bun run decokit --mats-only (road enamel from asphalt_road_v1, pavement old mean, lawn bands off, film sky)
- 12:08 END   Vela 075-B decokit --mats-only (ok, 53 s)
- 12:08 Vela 075-B: bun run guard bun run kit
- 12:11 START Vela 075-B: bun run guard bun run decokit --mats-only (road stain contrast 0.65)
- 12:12 END   Vela 075-B decokit --mats-only (ok)
- 12:12 Vela 075-B: bun run guard bun run kit
- 12:14 Vela 075-B done with tools/decokit (READY-B written). Open for 074-H: wall_panel_neutral grain (2.5 %) vs the old sheet's 6.7 % tonal cloud.

## Water ownership (Glint 075-A, agreed with Vela's 075-B split in textures.md)
- Glint owns the sea and the district water that the builder makes (look.json `water` → Look.Water, one material named `water`): its shader and light response. 075-A ports the old DecoKit/Ocean as `Suborned/DK Ocean` (Shaders/DecoKit/DK_Ocean.shader) with DecoKitHarbour.Ocean's values and the old DK_Sea_Normal sheet (Resources/Atmosphere/dk_sea_normal.png), from light.json `ocean`; Look.Water uses it when that key exists. The builder's fountain and pool basins that draw in `water` take it too (2 renderers in today's scene).
- Vela owns the kit's own water materials (DK_Pool_Water, DK_Fountain_Water, DK_Water: the old .mat files, as textures.md says) and any water albedo or texture on kit pieces.
- No decokit run from Glint. 12:40 Glint 075-A: Unity batchmode captures only (Glint075.cs, offscreen), no scene saved.

## Vela 075-C step 2 (landmark livery materials), 12:50 · proposal to Splice for briefs/075/landmarks.json
Source: the old Editor/DecoKit/DecoKitColourways.cs (`livery` layer) and its output in the old Art/DecoKit/Generated/Paint/. The old rule, exactly:
- every landmark in kit categories Landmarks, Skyport, Sky: DK_Steel_Ivory (the lattices, trusses, gantries, masts) -> vermilion #B8432E (old `Vermilion`; Paint/DK_Steel_Ivory_B8432E.mat)
- SphereStation only: DK_Hull_Ivory (the hull) -> brick #9C2E2B (old `Signature`; Paint/DK_Hull_Ivory_9C2E2B.mat; textured base: tint = linear hex / linear mean of hull_panels_v1, which reproduces the old .mat's _BaseColor to 1e-6)
- SpaceHQ: gold #F2C14E (old `Signature` on DK_Gold, metallic 1 smoothness 0.45). Our SpaceHQ_Portal's ribs, beads, orbits and crown are DK_Crown_Gold (#E3D681, the pale crown gold), so the key goes on DK_Crown_Gold, base colour only (metallic 1 and smoothness 0.7 kept: Aster's no-new-anything rule; 0.45 is a Sky/Glint call)
Colour keys and the materials I write (tools/decokit/livery.py -> Data/livery.json -> kit.json `landmarks`): `vermilion` DK_Steel_Ivory__vermilion · `brick` DK_Hull_Ivory__brick · `gold` DK_Crown_Gold__gold. Name = <base>__<key>, as colourways.
Splice: please key landmarks.json slots on these three keys and bases. The old rule puts vermilion on every Landmarks/Skyport/Sky piece with DK_Steel_Ivory (in our kit: SphereStation, DiscTower(@avenue-gate-e/w), LinerDock(@liner-berth), Skylon, Skyport_Tower(_Portal), SkyPad_Roof, SkyRailStation_Twin, SkyRail_Pylon_Head); brick on SphereStation only; gold on SpaceHQ_Portal only. Which of those the export lists is yours; kit.json `landmarks.pieces` carries the same old-rule default so the two can be diffed (test: every piece -> slot -> key in landmarks.json is a material in kit.json `landmarks`).
- 12:23 START Vela 075-C: bun run guard bun run decokit --mats-only (livery.py: DK_Steel_Ivory__vermilion, DK_Hull_Ivory__brick, DK_Crown_Gold__gold + Data/livery.json)
- 12:24 END   Vela 075-C decokit --mats-only (ok, 57 s)
- 12:24 Vela 075-C: bun run guard bun run kit
- 12:26 note: the 12:23 run stopped in livery.py (no Pillow inside Blender) after the vermilion .mat; Blender exited 0, so the guard printed no error. Fixed (bpy fallback), rerun:
- 12:26 START Vela 075-C: bun run guard bun run decokit --mats-only (rerun)
- 12:27 END   Vela 075-C decokit --mats-only (rerun)
- 12:27 Vela 075-C: bun run guard bun run kit
- 12:33 Vela 075-C done with tools/decokit. Result for Splice and Latch:
  - kit.json `landmarks` (schema suborned.livery/1): `keys` {vermilion: DK_Steel_Ivory -> DK_Steel_Ivory__vermilion #B8432E, brick: DK_Hull_Ivory -> DK_Hull_Ivory__brick #9C2E2B, gold: DK_Crown_Gold -> DK_Crown_Gold__gold #F2C14E}; `pieces` = the old rule over our catalogue (15 pieces: every non-rejected Landmarks/Skyport/Sky piece with DK_Steel_Ivory takes vermilion; SphereStation also brick; SpaceHQ_Portal gold); `files` = the 3 .mat + 3 .meta, copied by `bun run kit`.
  - Base colours equal the old Paint .mats (oldDelta 0 for all three; brick tinted over hull_panels_v1's mean the old way). Only m_Name/_BaseColor/_Color differ from the base .mat (kit test 075-C).
  - Splice: landmarks.json can key slots by these names; if your export lists a subset (placed pieces only), each row should match kit.json `landmarks.pieces[id][base]`.
  - Latch: per instance, for each renderer slot whose material is `base`, swap to `keys[pieces[id][base]].material` (same as the colourway swap).
  - C5: the Portal regen kept the tower's bands. The red bands in the old reel are the DK_Steel_Ivory shaft rings painted by livery (DK_Enamel_Coral is only trim: 72 m2 Portal vs 78 m2 legacy). The Portal lost the back service gantry (DK_Steel_Ivory 3173 vs 3832 m2), not the bands. The legacy FBX and our generated Skyport_Tower have identical slot areas. Renders: briefs/075/landmarks/sheet.png, slots.json.
