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

## Vela 075-E (streamline stack modules; CornerTower_12), 13:49
- Files I touch: tools/decokit/streamstack.py (new: StreamBayStack / StreamCornerStack / StreamEndStack _Base/_Mid/_Top), kit.py FAMILIES (+streamstack), facades_tower.py (CornerTower_12's door canopy and columns: brass -> ivory / chrome), tools/kit/lod.ts + tags.ts (the stack modules' distance class and face:stream-* use), kit.json status (adoptions).
- 13:49 START Vela 075-E: bun run guard bun run decokit --only <9 stack ids>,CornerTower_12
- 13:50 END   Vela 075-E decokit --only (ok, 55 s)
- 13:50 Vela 075-E: bun run guard bun run kit --catalogue, adopt (status), bun run guard bun run kit
- 13:51 Vela 075-E: bun run guard bun run collide; collide --check
- 13:51 Vela 075-E: bun run guard bun run cover; cover --check
- 14:06 Vela 075-E done with tools/decokit (READY-E-vela). Contract for Splice: briefs/072/pieces/pieces.md "075-E"; sheet briefs/075/stream/contact_sheet.png.

## Splice 075-E (streamline grammar in the generator), 15:10
- Files: tools/world/gen/stream.ts (new: the class table, RECIPE, the corner and end frame), faces.ts (STREAMLINE), facades.ts (recipe pick, ends at tails, corner frame, CornerTower_12), export.ts (faces.json carries `stream`), tests faces.test.ts / wave2.test.ts / stream.test.ts (new); briefs/075/composition.md + .json. No decokit or kit runs.
- Vela: thank you for the stacks and CornerTower_12, all adopted when I got there. I place them with the whole-wall pieces' frames, as streamstack.py says: StreamBayStack as StreamBay (6 m, 14 m body); StreamCornerStack and StreamEndStack with the corner and end frames, the cell read from the bounds (half = size/2 − pivotOffset x = 4.0). StreamCornerStack_Top stands on the Top band (3.95), its fin above. CornerTower_12 is at one street-junction corner per plot, outside the play streets, on buildings of 9 storeys or more, at the corner code's scale: 3 placed (bld/harbour-court/7, bld/docks-boulevard/2, bld/lantern-boulevard/3).
- Vela (ask): the whole-wall streamline pieces (StreamBay_n, PortholeBay_n, BalconyBay_n, ArcadeBay_3, DecoBay_10) now also stand as every-face modules (about 600 of them). They carry no kit.json lodClass, so they switch on the 'facades' category like the street pieces. If Redline wants them on a distance class (bay3 / bay4 or a new one), please tag them in tools/kit/lod.ts; the export copies whatever the catalogue gives.
- Latch (please verify in the rebuild): the old pieces are not symmetric, and the export has no mirror. In kit axes (Unity), StreamEnd's rounded return and StreamCorner's round stand on +X, and at the facade yaw kit +X runs along −t of the ring, so an unmirrored end closes a run's tail. Brief 007's street pass placed ends at the head and fronted corners on the incoming face, so by this reading the round sat on the wrong side and the cell overhung the corner by 1.63 m. Both passes now use stream.ts KIT_X = −1. If any StreamEnd or StreamCorner in the rebuild rounds the wrong way, set KIT_X = 1: one switch turns every end and corner of both passes. A mirror flag in the export would also let a run's head end round off (today only tails do): that is a schema and builder change, Aster's call.
- Export after 075-E: sha256 a6c76f6a9f041e1b6f542631ad01f2797ddb85397e429e01eeba562f83e782f5

## Splice 075-F1 (billboards, posters, signage: rules BB-1..4), 14:30
- New files (mine): tools/world/gen/billboards.ts (the rules), tools/world/rules/billboards.ts (the checks BB-1..BB-4) + billboards.test.ts.
- Shared edits, each one or two lines, additive: tools/world/gen/street.ts (one call at the end of generateStreet, after the booths and before streetKit: the ground adverts take what everything else left, so no other street id moves), tools/world/gen/index.ts (one call after generateStreet: rooftop boards onto sign-frame roofs, wall boards onto party walls; `billboards` field on Generated), tools/world/gen/kitrefs.ts (slotOf: the two new street kinds billboard-pylon / billboard-wall sized as their item, like the capstone kinds), tools/world/rules/index.ts (register), contracts/world/world.schema.ts (comment only). Not faces.ts, facades.ts, stream.ts, frontage.ts or export.ts.
- **Splice 075-E (ask):** Vela's note on CornerTower_12 (briefs/072/pieces/pieces.md: its blade sign reads HOTEL): place it only on a corner whose tenancy is a hotel (trade hotel-lobby or travel-bureau, as BladeSign_Hotel's tag). Today 1 of the 3 is on a hotel building (bld/harbour-court/7 has a hotel-lobby; bld/docks-boulevard/2 and bld/lantern-boulevard/3 do not). The pick is in your facades.ts (towerV) and faces.ts (JUNCTION.tower), which run before generateFrontages, so the trade is not known there yet; either hand the frontage trades in, or ask Vela for the blank-panel variant. I add no check for it (it would fail your export mid-round); say here when it holds and I'll add BB-5.

## Vela 075-F1 (billboard and poster art), 14:40
- Files I touch: tools/decokit/signage_art.py (sheet B: ADS_B/POSTERS_B/BOARD_B, cells_b, pack_b), signage.py (the 8 billboards bake one canonical ad cell; DEFAULTS), posters.py (new: the variant .mats + Data/posters.json), unity_mats.py (one call after the livery), tools/decokit/art/posters/ (new: 12 cells, billboard_ads_v2.png, PROVENANCE.json), tools/kit/catalogue.ts + copy.ts + kit.test.ts, contracts/world/kit.schema.ts (KitPosters). Art: Codex image tool, $0 (paid/075-posters, 13 rows).
- 14:28 START Vela 075-F1: bun run guard bun run decokit --only Billboard_Rooftop_A,Billboard_Rooftop_B,Billboard_Rooftop_C,Billboard_Pylon_A,Billboard_Pylon_B,Billboard_Wall_A,Billboard_Wall_B,Billboard_Wall_C
- 14:29 END   Vela 075-F1 decokit --only (exit see below)
- 14:29 note: the 14:40 run stopped in posters.py (an assert; Blender exited 0, so no manifest merge). Fixed, rerun:
- 14:29 START Vela 075-F1: bun run guard bun run decokit --only <the 8 billboards> (rerun)
- 14:30 END   Vela 075-F1 decokit --only (rerun)
- 14:30 Vela 075-F1: bun run guard bun run kit
- 14:31 Vela 075-F1: bun run guard bun run collide; collide --check; typecheck; kit + decokit tests
- 14:58 Vela 075-F1 done with tools/decokit (READY-F1-vela). Runs: decokit --only <8 billboards> twice (the first stopped in posters.py, see above), kit, collide + --check, typecheck, kit + decokit tests (45 pass).

### For Splice (075-F1): billboard and poster variants, how to pick one
Catalogue: contracts/world/kit.json `posters` (schema suborned.posters/1; type KitPosters in contracts/world/kit.schema.ts; source out/decokit/Data/posters.json from tools/decokit/posters.py).
- Two sheets, one layout. Sheet A = the old billboard_ads_v1.png (byte-identical to the old town's). Sheet B = billboard_ads_v2.png (new: Textures/billboard_ads_v2.png, guid d3547b3dfb6bc766d1f5e12e23299ab5).
- **Ad key** (16, `posters.adOrder`): mars fizz aircar skyrail robot planetarium cruise cinema (sheet A, the old ads) · luna suds range venus tidy pearls mints coral (sheet B, new).
- **Billboards** (`posters.adPieces`): Billboard_Rooftop_A/B/C and Billboard_Wall_A/B/C swap base slot `DK_Billboard_Ads` -> `DK_Billboard_Ads__<key>`; Billboard_Pylon_A/B swap `DK_Billboard_Ads_Lit` -> `DK_Billboard_Ads_Lit__<key>`. Exact names: `posters.ads[key].materials[base]`. Every billboard face now bakes the same canonical cell, so any key fits any board (the 3 rooftop, 3 wall and 2 pylon pieces differ only in frame and accent colour).
- **Pylon back face** shows `posters.backFace[key]` (the other ad in the same sheet row), with no extra material. Count it as a second visible ad for the neighbour rule.
- **Poster and board pieces** (`posters.sheetPieces`): AdColumn (4 posters), SandwichBoard (2 posters), Marquee_Cinema (letter boards) keep their old cells. Sheet A = keep the base material; sheet B = `DK_Billboard_Ads__sheetB` (AdColumn, SandwichBoard) or `DK_Billboard_Ads_Lit__sheetB` (Marquee_Cinema). Two choices each; turn the AdColumn by placement for more.
- **Default** (no variant chosen): `posters.defaults[id]`, the ad each piece showed in the old town (Rooftop_A mars, _B fizz, _C skyrail, Pylon_A planetarium, _B cruise, Wall_A cinema, _B robot, _C aircar). Without a swap every billboard now shows "mars" (the canonical cell), so the builder must always apply a key: the default, or the seed's pick.
- **Proposed pick** (yours to set): key = adOrder[hash(seed, instance id) mod 16]. While a key (or a pylon's backFace) matches one already used within the same view (≈ 60 m, or the same street run and vista), step to the next index. Adjacent duplicates: 0. Sheet pieces: sheet = hash(seed, id) mod 2, alternating along a run.
- **Cost**: 34 variants, all on the base's shader and keywords (only the texture guid and the _BaseMap/_MainTex/_EmissionMap offset change; kit test 075-F1 checks it). SRP Batcher keeps SetPass flat. One extra 2048 texture (sheet B) when any sheet-B key is placed.
- **Latch**: the same per-instance swap as KitLiveries (for each renderer slot whose material is the base, use the key's material). The offset relies on Repeat wrap, which both sheets' .meta keep (wrapU 0).
- Words on the boards are brand words only (16 ads, 8 posters, 4 letter-board titles); the list is in `posters.ads[key].words`.
- 15:40 Splice 075-F1 done (READY-F1-splice). Shared edits as listed above, plus tools/world/build.ts (one line: `posters` after `liveries`), tools/world/report.ts (one log line), contracts/world/world.schema.ts (`posters`, PosterPlaced, the two street kinds), the world example (`posters`), and tests surfaces.test.ts (3058 -> 3094 street items) and crossings.test.ts (the BB items stand last, like the capstone's). No faces.ts / facades.ts / stream.ts / frontage.ts / export.ts edits.
  - **Vela:** your posters contract is used as proposed: key = adOrder[hash(host) mod 16], stepping past an ad (or a pylon's back face) already shown within 60 m or on the same edge; sheet pieces A/B by hash, flipped against the nearest on the same edge. 67 picks, 15 distinct ads, 0 duplicates in view (BB-4 checks it).
  - **Latch:** (1) the rooftop boards are `roofs[].kit` (drawn today by RoofKit); the pylons, wall boards and new advertising columns are `street[]` items with kit refs (drawn today by CityLayers.Street; `billboard-wall` hangs at `z` = its foot). (2) New: `world.posters.placed[]` = { host, piece, key, back?, slots }: for each placed instance whose element id is `host`, swap each renderer slot whose material is a `slots` key for its value, as KitLiveries. Without it every board shows the canonical "mars" cell.
  - **Splice 075-E:** the CornerTower_12 HOTEL ask above still stands (2 of 3 on non-hotel buildings); BB-5 waits for it.

## Splice 075-F1b (CornerTower_12 only on a hotel), 16:30
- Files: tools/world/gen/stream.ts (JUNCTION.hotel), frontage.ts (tradeHosts; buildingHost takes id/area/ring), masses.ts (hotels resolved before generateFacades), facades.ts (FacadeBld.hotel gates towerV), faces.ts (FacesIn.hotels gates the junction tower), index.ts (hands the hotels to dressFaces), rules/billboards.ts (BB-5) + billboards.test.ts.
- CornerTower_12 3 -> 1 (bld/harbour-court/7, a hotel-lobby). bld/docks-boulevard/2 and bld/lantern-boulevard/3 take StreamCorner_9. Travel-bureau is not counted as a hotel (Aster: "hotel tenancy").
- Export sha256 2de0d88e618ccbe5ab34ad33a93ed37a10413901e359da30de0ff905de13f254 (READY-F1b). Handover site unchanged.
