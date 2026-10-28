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

## Vela 075 golden: tram (the old tram and track, the stop's kiosk and column; StreamCornerStack_Top LODs), 15:48
- Files I touch: tools/decokit/ground.py (Tile_TramRail as tram_rail(s, n); new Tile_TramRail_12m), tram075.py (new: TicketKiosk, TimetableColumn), kit.py FAMILIES (+tram075), streamstack.py (StreamCornerStack_Top L1/L2 keep the mast), tools/kit/tags.ts (use + scale for the five pieces), kit.json status (adoptions).
- 15:48 START Vela 075 golden: bun run guard bun run decokit --only Tram,Tile_TramRail,Tile_TramRail_12m,TicketKiosk,TimetableColumn,StreamCornerStack_Top
- 15:49 END   Vela 075 golden decokit --only (ok, 54 s): Tram 36644 / 9145 / 2156 (L0 = the old DK_Tram's 36644), Tile_TramRail 3140 / 784 / 174 (= the old 3140), Tile_TramRail_12m 9540 / 2384 / 518, TicketKiosk 21342 / 5335 / 1266, TimetableColumn 3302 / 825 / 197 (+ part Hand), StreamCornerStack_Top 3806 / 770 / 342
- 15:50 Vela 075 golden: Blender -b review075_tram.py (renders only, reads out/decokit; not a decokit run)
- 15:52 note: the track tiles' L2 (6 % collapse) folded the rails and grew the tile to 2.05 m; ground.py now gives both a hand-built L2 (tram_rail_far). Rerun:
- 15:52 START Vela 075 golden: bun run guard bun run decokit --only Tile_TramRail,Tile_TramRail_12m
- 15:53 END   Vela 075 golden decokit --only (ok)
- 15:54 Vela 075 golden: bun run guard bun run kit --catalogue; adopt (status) Tram, Tile_TramRail, Tile_TramRail_12m, TicketKiosk, TimetableColumn; bun run guard bun run kit

## Splice 075 golden (boulevard: palms, frontage, the old high-street colourways), 15:52
- Files (mine): tools/world/gen/oldstreet.ts (new: OLD_STREET, the old boulevard's rules by road id), street/roads.ts (palms + lamps on old-street edges), masses.ts (old storey codes on old-street buildings), facades.ts (streamline only + old end rule there), colourways.ts + build.ts (oldStreetFamilies: the old HighStreet rule).
- Shared, additive: unity/Assets/Suborned/Editor/World/facade.json (+4 families oldapricot, oldceladon, oldrose, oldochre and their 12 swatches: the old DecoKitColourways Soft palette, exact; never drawn by an area); tools/decokit/colourways.py (+OLD_FAMILIES: exact walls, no softening, wall + trim (DK_Enamel_Ivory = old Lighter) slots, no plinth); tools/kit/kit.test.ts + contracts/world/kit.schema.ts (a family's slots may be a subset; `trim` slot).
- **Vela:** colourways.py is yours; my edit is one table + a per-family slot set, the 12 families' output unchanged (same bytes). Shout here if you want it shaped otherwise.
- 15:55 Vela 075 golden: bun run guard bun run collide; collide --check; cover; cover --check; typecheck; kit + decokit tests
- 15:56 START Splice 075 golden: bun run guard bun run decokit --mats-only (colourways.py OLD_FAMILIES: 4 old high-street families x wall, wall, trim = 12 .mats)
- 15:57 END   Splice 075 golden decokit --mats-only (ok, 56 s): 12 old high-street .mats (DK_Limestone_Tex/DK_Render_White/DK_Enamel_Ivory __oldapricot/oldceladon/oldrose/oldochre)
- 15:57 Splice 075 golden: bun run guard bun run kit
- 16:10 Vela 075 golden: Blender -b review075_tram.py (renders + lods.json; reads out/decokit) -> briefs/075/golden/tram/contact_sheet.png
- Results: collide 5 measured, --check ok (347 pieces); cover written (hashes only), --check exit 0 (the two getaway-safe ambush warnings are as before); typecheck 0; kit + decokit tests 45 pass 0 fail.
- Side effect, for Splice: the 15:48 decokit run also wrote the .mats of your colourways.py OLD_FAMILIES (oldapricot, oldceladon, oldrose, oldochre; DK_Enamel_Ivory trim slot), so kit.json `colourways` carries them now. tools/world tests: 316 pass, 7 fail (wave2 facade squash, kit020 roof tanks 89/90, billboards wall 8/9, colourway neighbours, surfaces x3); none reads a tag I changed (my five pieces' uses are new and no slot or picker asks for them), so they follow the in-flight world edits and the stale export: please recheck after your rebuild.

## Golden: tram (Vela, brief 075 task e)
What the old boulevard had (old Editor/DecoKit/DecoKitStreetLayout.cs + StreetLife.cs, read only): one tram lane of Tile_TramRail laid every 2 m (yaw 90, y 0), the Tram on it, a stop island with TramCanopy x2, BusShelter + TicketMachine at the stop, CurvedKiosk on the walks. **No catenary or poles** in the old kit or layouts (the tram carries a pantograph, nothing overhead); none built: overhead wires would also cross the play camera. Ask Aster if Sky wants them.

Kit status per piece (contracts/world/kit.json):
| piece | status | source | size (x, h, z) m | tris L0 / L1 / L2 | use tag |
|---|---|---|---|---|---|
| Tram | **adopted** (was candidate) | generator binary (vehicles.py, = the old 36644-tri car), old .meta/prefab GUID kept | 2.69 x 4.92 x 11.38 | 36644 / 9145 / 2156 | vehicle:tram |
| Tile_TramRail | **adopted** (was candidate) | generator (ground.py, = the old 3140) + hand-built L2 | 2.0 x 0.15 x 2.0 | 3140 / 784 / 120 | ground:tram-rail |
| Tile_TramRail_12m | **new, adopted** | generator (same section, six 2 m periods) + hand-built L2 | 12.0 x 0.15 x 2.0 | 9540 / 2384 / 360 | ground:tram-rail:12m |
| TicketKiosk | **new, adopted** | tram075.py | 2.0 x 2.7 x 1.5 | 21342 / 5335 / 1266 | kiosk-tickets |
| TimetableColumn | **new, adopted** | tram075.py (+ part DK_TimetableColumn_Hand) | 0.8 x 2.6 x 0.4 | 3302 / 825 / 197 | timetable-column |
| TramCanopy_Play, BusShelter_Play | adopted already (tram-shelter, placed 10 + 10) | generator | as before | 15028 / 12592 | tram-shelter |
| CurvedKiosk, Newsstand, CoffeeCart_Play, TicketMachine | adopted already | | | | kiosk-pavilion, newsstand, kiosk-food, ticket-machine |
TramCanopy and BusShelter (full ids) stay candidates: the _Play ids are the same designs, same triangles. No new material, no new shader variant: kit rows only (DK_Accent_Coral is the coral transit, DK_Dial, DK_Screen_Amber, DK_Glass, chrome; no brass). Tram's beacon slot is now DK_Beacon_Rose (the generator's alias for DK_Signal_Red; KitImport remaps by name, the .mat ships).

Pivots and placement (Splice places by rule, Latch draws):
- **Track tiles**: pivot footprint centre, top surface at z 0, body to -0.15 (place at the track surface, world `transit.tram[].level`). Track runs along kit X; rail heads at ±0.5 m from the tile centre (1.0 m gauge = the Tram's wheel treads at ±0.5), so the tile centre is the track centreline. Edge strips (mint 0.4 + red 1.6 per 2 m period) out to ±1.0: the tile is 2.0 m wide against `transit.tram` width 1.6 (two tracks at spacing 3.6 leave 1.6 m between tiles for the separator or lawn). Symmetric, so no mirror question. The world's tram lines are 2 m-step polylines: one Tile_TramRail per step on curves (R 50: 1 cm chord error), Tile_TramRail_12m on straights (any run of 12 m; the two butt seamlessly, same pattern phase when a 12 m starts on an even 2 m step). Instances: the 3.67 km of line (T1 trunk + loop + T2, 2 m steps) x 2 tracks is ~7,350 two-metre tiles; with 12 m on the straights that drops ~5x. Latch: static-batch or instance them (the old city merged them, DecoKitTileMerge).
- **Tram**: pivot origin-ground, pivotOffset [0, 0.108] (footprint centre 0.108 m forward of the pivot along its length; legacy claim was centre), front +Z = the nose. Pivot on the track centreline, yaw along the line, scale 1 only (tags scale [1, 1]: scaled, it leaves the gauge). Pantograph top 4.92. It moves, so it is live 3D (Pulse's timetable / Latch's mover); an export item `vehicle:tram` is Splice's call.
- **TicketKiosk**: pivot footprint centre on the walk/platform top; front +Z (ticket window, fare screen, transit disc) faces the walk. Size equals the plan's kiosk-tickets item exactly. **slotOf has no case for `kiosk-tickets`** (kitrefs.ts returns null, so all 7 stay procedural): Splice, add e.g. `case 'kiosk-tickets': return { h, hTol: 0.05, along: w * 1.05, depth: d * 1.05 };`.
- **TimetableColumn**: pivot footprint centre; the dial faces ±Z (front and back), 0.8 m dial centred at y 2.0. Plan item says 0.4 x 0.4 x 2.6, so the slot needs `along` ≥ 0.82 (dial width) and depth 0.4: add `case 'timetable-column': return { h, hTol: 0.05, along: 0.85, depth: 0.45 };` (20 procedural today). The next-arrival hand is part DK_TimetableColumn_Hand (both faces), at 12 o'clock as built; Latch turns it about the kit Z axis through (0, 2.0, 0), angle from Pulse's next arrival.

For Datum / Aster (plan): the plan says the Boulevard's T1 reservation is **lawn track** (standards trees.boulevard, SF-3) — the green stripes in sky-ours.png. The old reel's look is this paved bed (grey track slabs, ivory outer slabs, mint/red strips). Matching the old means laying the tiles over the reservation instead of lawn; that is a plan change (Datum, Sky via Aster). I built no lawn variant; one is quick if Sky wants grass between the rails.

StreamCornerStack_Top (open item): L1 and L2 now keep the mast (L1 the full mast, L2 a 4-sided whip + beacon). Heights L0/L1/L2 = 12.21 / 12.21 / 12.21 m (were 12.21 / 8.85 / 8.85); tris 3806 / 770 / 342 (L1 +102, L2 +46). L0 unchanged.

Evidence: briefs/075/golden/tram/contact_sheet.png (stop vignette; each piece old | L0 | L1 | L2 with tris and size), lods.json (per file tris and bounds), renders beside it. Script tools/decokit/review075_tram.py.
- 16:13 Splice 075 golden: my generator side is in (oldstreet.ts rules by road id, so they follow the boulevard's new section). The plan checker fails right now on the in-progress boulevard edit (RF-1, SF-9, KR-2; plan saved 16:12): **Datum**, please add a line here when the plan passes again; I rerun world x2, faces, plan, cover, sim:site then.
- 16:34 Splice 075 golden done (briefs/075/golden/READY-splice): export e39d7839… on plan 48e18cc2… (Datum's new boulevard section). Open for Datum: 20 world tests (road finish, crossings, lawn-track, median noses, SF-21 counts) fail on the new section with my rules off as on; they are the section change's, not mine. Latch: rebuild needs bun run kit (done, 0 to copy) and the new facade.json families.

## Street section (Datum, brief 075, 2026-10-28 ~17:00): the plan passes again
Sky approved the 1950s street-running tram (brief 075, "Golden view and the street section"). Plan: contracts/world/harbour-district.plan.json; the rule is `standards.streetscape.sections.tramStreet` (read it whole: offsets, islands, kerbside, wires, corners). `bun tools/plan/cli.ts` exit 0; `--overlay tools/missions/handover/overlay.ts` exit 0, V-1 64/65, C-0, M-1 (667 / 1064 / 126 / 1473 m) unchanged; typecheck 0. Plan sha256 below in READY-street-datum.

What changed in the plan
- **The Boulevard** (T1): w 28 + `reservation` 10 → **w 20, no reservation**, `roads[boulevard].tram` added (centre run, stops). Per half from the road line: tram lane 0–3.6 (setts, mixed traffic, track at 1.8) · traffic lane 3.6–7.2 · kerbside parking/loading 7.2–10.0 · kerb at 10 · pavement 10–18 (district 8: kerb 2 / walk 3 / frontage 3). Street wall 36 m (was 44). Plots fronting it moved 4 m out to the new kerb: boulevard-west, lamplight-east, lamplight-south-e, harbour-boulevard, docks-boulevard, lantern-boulevard, lantern-south (edge x ±14 → ±10). Circus mouth faces `circus-mouth/boulevard/e|w` now x ±10 (a 41.821, b 50.01).
- **Sunset Way, Comet Row, Harbour Road** (T2): `median` 4 removed, **w 24 kept** (kerbs, plots, beyond, quay at x 312 and the Parade/Market Deck footprints untouched). T2 moved from side-running (offset 6, contraflow inner track, the slew across Sunset Way's median) to the **centre lanes**: path [[300,320],[300,-720,25],[-340,-720,25],[-340,286,25],[-400,286]]; stops 6 m in: harbour (300,60), harbour-court (300,-245), docks (300,-550), comet (-30,-720), lantern (-340,-585), rocket (-340,-280), terraces (-340,150). Per half: tram lane 0–3.6 · traffic 3.6–7.2 · kerbside 7.2–12.0 (30° angle bays with a kerbed palm island every 24 m: the primaries' palms now the median is gone; [S], Datum's proposal, the alternative w 20 + 7 m pavements is for Sky).
- **Corners**: `standards.tram.radiusRunning` 50 → 25 (with `radiusRunningWhy`). Centre-running at R 50 swept 6 m over the ring corners' pavement (T-C1 failed 15 m off); at R 25 the inner envelope clears the primary kerb return (R 6) by 0.5 m. Same as radiusLoop.
- **Crown Road** keeps its 4 m median (no tram on the sheet); T2's 50 m off-sheet stub keeps the north half (offset 6).
- Removed: `sections.primary.tram` (the side-running section; no code read it). `roads.marks.lanes` keeps crown-road only (RF-1: an undivided road takes no lane count). Texts updated: trees.boulevard, transit.tramStop, roads.kerbs.reservation, notes.

For Splice (generator; the plan is the source)
1. **Road finish on a tram street**: the two tram lanes (|offset| ≤ 3.6) are granite setts, a band 7.2 m wide down the road line, the rail tiles (Tile_TramRail / _12m) laid flush in it; asphalt (or the enamel set) outside. No lozenge, seam, dash or ribbon inside the setts band; the material edge is the lane line. RF-1's lane rule now sees these roads as undivided: please give tram streets their own marks (no centre dash; a 0.12 dash at ±7.2 between traffic and kerbside, or none) rather than the centre line.
2. **No lawn track**: with no `reservation` on any road the lawn flats, reservation kerb faces and their quarter rounds vanish by themselves; nothing to keep.
3. **Boarding islands**: the stop platforms build.ts already places (track ± 1.4 to the island's track edge, 3.0 wide, 37.2 long) are now islands in the carriageway with traffic passing on the kerb side: give them a kerb all round (ribbon round the island like a median), half-round noses (R 1.5) with a bollard pair at each end, shelter at the island's back (span [4.7, 6.2]). The stop's crossing runs kerb to kerb across both islands (the islands are its refuges; floating-eye heads on each island, GDD §15).
4. **Kerbside strip**: Boulevard parallel bays 6.0 × 2.4 and loading bays at trades that take deliveries; primaries 30° angle bays 2.6 × 4.8 with palm islands (2.0 × 4.8, kerbed, flush with the pavement, CoconutPalm in a 1.5 pit, a district lamp on alternate islands) every 24 m. No bays within 10 m of a kerb return's tangent, 5 m of a crossing, along an island and 15 m before / 10 m after it. Parked cars themselves are Keel's/Pulse's (life), the bays are the generator's.
5. **Span wires**: contact wire 5.8 m over each track centre (zig-zag ±0.2), span wire at 7.0 every 30 m (12 m on curves) from wall rosettes (chrome boss in a 0.6 m deco disc on the facade at 7.0) where both ends have a wall within 2 m of the building line; else paired slim side poles (chrome, Ø 0.3, 8.5 m, kerb zone 0.45 off the kerb face, a kerb-zone item: SF-5, SF-11 3 m off trees and lamps, may carry a lamp bracket in place of the nearest lamp). No centre poles (track centres 3.6 leave no room). Round the circus loop: poles on the island ring band and outer pavement. Vela noted (above) the old kit had no catenary and wires cross the play camera: keep them thin (≈ 12 mm wire, 20 mm span), Glint to judge at play zoom; Sky asked for them.
6. **The T1 × T2 flat crossing** at s_boulevard (0,-720): a square of setts over the junction box with the rails crossing.

Code I touched (outside my role, please review and keep or redo):
- `tools/world/gen/street/transit.ts`: the circus civic-robot-dock pair (fixed 10 / 13 m from the kerb point nearest (0,-60)) fell on the circus mouth's kerb return once the Boulevard narrowed (SF-9: 0 of 2). Now: the old spot if it fits, else step back along the kerb a metre at a time to the first spot where both pass every test (no force). Unchanged output when the old spot fits.
- `tools/plan/checks.ts`: the tram-crossings list excluded only `boulevard` as a line's own street; it now excludes any road whose `roads[].tram.line` is that line (T2 on the road lines listed Sunset Way hundreds of times). Informational list only; no rule reads it.

Tests that follow the section (live-plan counts and the lawn track; 21 new fails against a tree copy of HEAD's plan with the same suite: tools/plan, tools/world, tools/missions, tools/life): road finish (lawn-track ends, every kerb face kind incl. reservation, kerbed bay faces, RF-3 live, the sheet-edge corners), KR-1 live counts (160 returns, 50 median ends), KR-5's reservation example, median noses (25 medians), crossings counts (16 kerb pairs, 10 stop patches, 274 dot rows, 41 heads), surfaces counts (478 + 315 parts, booth/tub, nothing else moved), SF-21 counts (brass parts > 1000: 945; 41 heads: 40; 32 service bays: 31), kit020 roof programmes, pylon-clear remnant (S1 pylons no longer in Harbour Road's / Sunset Way's medians), RF-1 asphalt set (the lanes map). They are fixtures of the old section: yours to re-pin once the generator lands.

Others
- **Pulse**: T2 timings move by about a second (docks → comet 489 m, comet → lantern 434 m on R 25 corners; plan.timetable lists 62 and 55 s); T1 unchanged.
- **Keel / Pulse (life)**: tools/life/cli.ts `lanesBoulevard: lanes(28, 10)` and its 'T1 reservation (10 m)' note describe the old Boulevard; it is now 20 m, undivided, 2 lanes + parking a side.
- **Cover**: the plan hash changed (`bun run cover` restamps); the cover test fails the same 3 ways before and after this edit (stale inputs already in the working tree).
- Handover: civic block, Rocket Lane's square bay, Apron Road's office bay, the walk window and the patrol are 70 m or more west of the Boulevard's new kerb (x -10); no point, route or timing moved.

## Vela 075 golden: the vista (Crown Avenue arch, the gold Crown Vista tower) and the tram's span wires, 16:51
- Files I touch: tools/decokit/vista075.py (new: ParabolicArch@crown-avenue, DiscTower@crown-vista), landmarks.py (fluted_shaft / disc_tower_at take optional materials; defaults unchanged, so DiscTower and its regens build the same), tram075.py (+5 wire pieces), kit.py FAMILIES (+vista075), crown.py (the two new pieces are crown gold), livery.py (gold key also on DiscTower@crown-vista), tools/kit/tags.ts (use + scale for the seven), kit.test.ts (the crown list), kit.json status.
- 16:51 START Vela 075 golden vista: bun run guard bun run decokit --only ParabolicArch@crown-avenue,DiscTower@crown-vista,TramWire_Contact,TramWire_Span,TramWire_Hanger,TramWire_Rosette,TramWire_Pole
- 16:53 END   Vela 075 golden vista decokit --only (ok): ParabolicArch@crown-avenue 8156 / 2038 / 480 (34.0 x 75.74 x 11.64), DiscTower@crown-vista 8596 / 2149 / 514 (44.2 x 156.6 x 40.2, = DiscTower), TramWire_Contact 12, TramWire_Span 12, TramWire_Hanger 60, TramWire_Rosette 100, TramWire_Pole 216 (no LODs: under the 400 floor)
- 16:53 Vela 075 golden vista: Blender -b review075_vista.py (renders only, reads out/decokit; not a decokit run)
- 16:54 note: L1 / L2 of DiscTower@crown-vista read pale (the collapse drops the gold ribs and shows the ivory core): the core is gold now, the ribs fine ivory; the rosette disc 10 -> 12 sides (it read as a polygon close up). Rerun:
- 16:54 START Vela 075 golden vista: bun run guard bun run decokit --only DiscTower@crown-vista,TramWire_Rosette
- 16:55 END   Vela 075 golden vista decokit --only (ok): DiscTower@crown-vista 8596 / 2149 / 515, TramWire_Rosette 120
- 16:55 Vela 075 golden vista: Blender -b review075_vista.py (renders only)
- 16:56 Vela 075 golden vista: bun run guard bun run kit --catalogue; adopt (status) the seven; bun run guard bun run kit
- 16:57 note: kit.test 'brass over gold' holds a crown piece to one gold: the arch plinths' hairlines are chrome now (were DK_Accent_Gold). Rerun:
- 16:57 START Vela 075 golden vista: bun run guard bun run decokit --only ParabolicArch@crown-avenue
- 16:58 END   Vela 075 golden vista decokit --only (ok)
- 16:58 Vela 075 golden vista: kit --catalogue, kit, collide, collide --check, cover, cover --check, typecheck, kit + decokit tests
- 16:58 Vela 075 golden vista: Blender -b review075_vista.py + review075_vista_sheet.py (renders only) -> briefs/075/golden/vista/contact_sheet.png
- Results: collide 7 measured, --check ok (354 pieces, 4106 volumes); cover written, --check exit 0 (the two getaway-safe ambush warnings as before); typecheck 0; kit + decokit tests 45 pass 0 fail. kit.json: the seven adopted (status), kit copy done.

## Golden: the vista and the wires (Vela, brief 075), for Splice, Datum, Latch, Glint
Contact sheet: briefs/075/golden/vista/contact_sheet.png (renders, lods.json beside it; scripts tools/decokit/review075_vista.py + review075_vista_sheet.py). Not the game's lighting.
All seven **adopted** in contracts/world/kit.json, own use tags (nothing generic: no slot picks them by accident). No new material or shader variant: DK_Limestone_Tex, DK_Enamel_Teal, DK_Enamel_Ivory, DK_Crown_Gold, DK_Chrome, DK_Iron, DK_Window_Tower, the DiscTower's own rows.

| piece | use tag | scale | size (x, h, z) m | tris L0 / L1 / L2 | pivot |
|---|---|---|---|---|---|
| ParabolicArch@crown-avenue | gateway:crown-avenue | [1, 1] | 34.0 x 75.74 x 11.64 | 8156 / 2038 / 480 | footprint centre, ground (between the feet) |
| DiscTower@crown-vista | landmark:crown-vista | [0.9, 1.1] (as DiscTower) | 44.2 x 156.6 x 40.2 (= DiscTower) | 8596 / 2149 / 515 | origin-ground, pivotOffset [-2, 0] (= DiscTower's shaft axis; the jib makes the box off-centre) |
| TramWire_Contact | tram-wire:contact | [1, 1]; stretch X | 1.0 x Ø 0.012 | 12 | centre of the 1 m strand, centreline at the pivot |
| TramWire_Span | tram-wire:span | [1, 1]; stretch X | 1.0 x Ø 0.020 | 12 | the same |
| TramWire_Hanger | tram-wire:hanger | [1, 1] | 0.08 x 1.21 x 0.05 | 60 | on the contact wire centreline; top +1.2 = the span wire centreline |
| TramWire_Rosette | tram-wire:rosette | [1, 1] | 0.6 x 0.6 x 0.17 | 120 | on the wall surface at the span height (disc centre); the boss out along kit +Z (Unity front); pivotOffset [0, 0.085] |
| TramWire_Pole | tram-wire:pole | [1, 1] | Ø 0.52 skirt, Ø 0.3 shaft, 8.5 high | 216 | foot centre on the pavement top; span-eye collar all round at 7.0 (yaw free) |

**Arch (Splice places it from plan landmarks[crown-vista].gateway):** at the gateway's c (0, 160), kit +X = plan +x (feet on x), so yaw 0 in plan terms (not the pier's π/2, which turns the feet onto y). Feet: rib centreline at ±15.5, plinths rounded 3.0 across x 6.0 along (x 14–17, y 157–163), so they match `feet` exactly; rib crest (centreline) 68.0, the atom's sphere top 75.2, its rings 75.74 (the pier arch is the same, 75.79: the plan's 75.2 is the sphere). Depth 11.64 (the atom's rings), plinths 6.0. The same design as the pier arch and the old DecoKit arch (limestone rib, teal face inlays, crown gold beads and atom); the rib is 2.5 m across at the foot (the kit's is 3.2) so rib and beads stay inside the plinth. Plinths are limestone with a teal band and chrome hairlines: no bronze panel, no gold at street level (kit.test allows a crown piece one gold only). L2 is 34.38 wide (the collapse puffs the rib 0.19 m a side, far LOD only). **pier.ts's gateway code takes `adoptedBy(gw.kit)` and yaw π/2: generalise it (designs.json C3), and the plan's `gateway.status` text is Datum's to update.**

**Gold tower:** DiscTower@crown-vista has the kit DiscTower's size and silhouette, with a gold shaft: a DK_Crown_Gold core, 40 fine ivory ribs and the kit's ivory ring bands every 12 m (gold in the core, so L1 and L2 stay gold). **Datum:** set landmarks[crown-vista].kit to "DiscTower@crown-vista" (landmarkKit takes adoptedBy(plan.kit) first; with "DiscTower" it keeps the plain one). The fit is unchanged (same bounds, scale ×1.1 inside [0.9, 1.1]). **Splice, the livery key:** add to tools/world/gen/livery.ts LIVERY.own `DiscTower: [{ slot: 'DK_Crown_Gold', colour: 'gold' }]` (baseOf strips the @site; only the crown-vista variant has a DK_Crown_Gold slot, so the gate pair and the plain DiscTower are untouched) → slot DK_Crown_Gold → material key **DK_Crown_Gold__gold** (#F2C14E, metallic 1, smoothness 0.45, the old SpaceHQ gold; the .mat already ships from livery.py). tools/decokit/livery.py lists DiscTower@crown-vista under `gold` already, so kit.json `landmarks` carries it after the catalogue. Optional, your call: the same entry for ParabolicArch would paint both arches the old #F2C14E (today crown gold #E3D681).

**Wires (Splice places them along the tram streets from sections.tramStreet.wires; Latch draws):**
- Contact: one TramWire_Contact per straight run between two hangers, at = the run's midpoint at height 5.8 (+ the track level), yaw along the run (and pitch if the track climbs), scale [L, 1, 1] with L = the run's length. The zig-zag ±0.2 makes each run a slight diagonal: the hanger points alternate +0.2 / −0.2 off the track centre.
- Span: one TramWire_Span from anchor eye to anchor eye at 7.0, scale [L, 1, 1]. Eyes: rosette = wall point + 0.17 along the facade normal into the street; pole = the pole's axis at 7.0 less 0.16 (the collar's radius) toward the street.
- Hanger: one per track per span, pivot at the contact wire point under the span (track centre ± 0.2), yaw along the track.
- Rosette: pivot on the facade at 7.0, kit front (+Z, Unity) = the facade's outward normal.
- Pole: foot centre 0.45 off the kerb face in the kerb zone, at the pavement top; paired across the street. No lamp bracket built (the plan lets a pole carry one: ask if you want the variant).
- **Tags carry no stretch field today**: KitTags has a uniform `scale` (height) and `squash`; the two wires need X-only scaling [L, 1, 1]. A `stretch: 'x'` tag (contracts/world/kit.schema.ts, tools/kit/tags.ts) is yours if a rule should check it.
- Cost: 12 tris a run and a span, 60 a hanger, 120 a rosette, 216 a pole; two shared materials (DK_Iron, DK_Chrome) plus DK_Enamel_Teal and DK_Enamel_Ivory. Latch: instance or batch them.
- **Glint:** at the about-play view (wires_play) the 12 / 20 mm strands are a pixel or less: they may alias or vanish. Judge them in the game; the thickness is one constant in tram075.py (WIRE_C, WIRE_S), and a minimum-width shader is the other route.

## Golden: vista (Datum, brief 075, 2026-10-28), for Splice
contracts/world/harbour-district.plan.json, landmarks[crown-vista] only (no road, plot, streetscape, line or Handover geometry changed):
- `kit`: "DiscTower" → **"DiscTower@crown-vista"**; added `kitWhy` (text). `c`, `r` 24.5, `top` 172.4, `crownType` disc unchanged (same bounds as DiscTower, fit ×1.1). `liveryWhy` text updated: the gold key (LIVERY.own DiscTower → DK_Crown_Gold gold) is Splice's.
- `gateway` (fields the generator reads; all unchanged except `status` and `yaw`):
  - `kit`: "ParabolicArch@crown-avenue" (adopted; place via adoptedBy(kit), scale [1, 1, 1])
  - `c`: [0, 160] (the piece's pivot: footprint centre, ground between the feet)
  - `yaw`: **0** (number, radians in plan terms; was a text string). Kit +X = plan +x, feet on x across the avenue. New `yawWhy` holds the text. The pier's gateway still has a text `yaw`; pier.ts hard-codes π/2 for it, so a generalised reader should take a numeric `yaw` when present and fall back to π/2 for the pier (or the pier's can be made numeric π/2 on request).
  - `feet`: [[-17, -14, 157, 163], [14, 17, 157, 163]] (x0, x1, y0, y1; = the plinths, in the frontage zone x ±14–17)
  - `crest` 68, `top` 75.2 (sphere top; rings 75.74)
  - `zones`: ["frontage"] (the feet's only allowed zone; the pier's is ["quay"])
  - `keep`: text, the keep-clear list: plinths only in the frontage zone (never walk, kerb zone, carriageway); no doorway, spill or vending within 1.5 m of a plinth; no palm or lamp crown/head within 1 m of a leg (leg x ±14.5 at 8 m up); shopfronts behind the plinths (x ±17, y 157–163) keep their doors, moved along the facade, never under a plinth.
  - `status`: "proposed …" → **"planned: …"** (piece made and adopted; the generator places it from these fields).
  - text only, not read: `name`, `piece`, `whyNot38`, `siteWhy`.
- Checks: `bun tools/plan/cli.ts` exit 0; `--overlay tools/missions/handover/overlay.ts` exit 0. K-1 passes 3736/3736 today because kit-fit.ts checks megastructure gateways only; the arch is not emitted yet, so nothing checks its feet. When Splice emits it, K-1's gateway branch should cover landmark gateways too (feet inside the piece's footprint, in `zones`, clear of the walk).

## Splice 075 street (the tram street in the generator), 2026-10-28 ~18:30 · READY-street-splice
- Files: tools/world/gen/street/tramstreet.ts (new) + tramstreet.test.ts (new, 7 tests with failing examples); build.ts (stops at sections.tramStreet.<class>.island.span = 3.2-6.2 m; setts flats, rails, kerbside, islands, wires, dressing into the export); gen/street.ts + gen/index.ts (the kerbside, palm islands and span poles placed last of all, explicit ids, so no other street id moves); gen/street/roadfinish.ts (a boarding island's kerb ribbon all round, `island` faces); gen/kitrefs.ts (slots kiosk-tickets, timetable-column (Vela's), span-pole); contracts/world/world.schema.ts (ground.kerbside, ground.islands, transit.wires, dressing.trams; KerbBay, TramWires, WireEnd, ParkedTram) + the example json; report.ts (a tram street line). Datum's transit.ts and checks.ts edits kept as committed (read, sound).
- Fixes found on the way (mine): livery.ts survives a digest kit with no `materials` (it crashed every levels skyline candidate: GEN-1); billboards.ts BB-3 deck test reads a level's z where the plan gives no `walk` (the skyline fixture); surfaces.test.ts line 79 had a for loop swallowed into a comment (mine, 075 golden); tools/drift-auto replays now carry unity/.../facade.json (since 074 every replay in its scratch copies died at import, so its e2e tests passed or failed hollow).
- Rails: whole 2 m periods per segment (k = L / 2N, under 1 % on straights), six at a time as Tile_TramRail_12m; z 0.02 = the setts top (the setts flats at PROV.foot). The T1 x T2 diamond is the two lines' tiles crossing over the setts square (no diamond piece: Vela's if wanted).
- Wires: Vela's TramWire_Contact / _Span / _Hanger / _Rosette in transit.wires.pieces (kit refs with scale [L, 1, 1] on the strands), TramWire_Pole on the span-pole street items; eyes per her note (rosette disc + 0.17, pole collar 0.16). Each pole takes the nearest free spot within 4 m of its station on its own (the Boulevard's staggered palms leave no spot 3 m off a palm on both walks opposite each other); the Boulevard's 24 spans are all rosettes. No lamp-bracket variant.
- Kept as data, not drawn yet (Latch): ground.kerbside (bay rings, kind, stall yaw), ground.islands (raised 0.18 masses, surface pavement), transit.wires (pieces drawn as kit; contact/spans as lines), dressing.trams (Tram kit ref, pivot and yaw: +Z the nose). The track strips (transit.tram, level 0.04) still draw over the rail tiles (top 0.02): Latch, skip the strips where ground.pieces carries tram-rail. look.json: + `setts` (DK_Gutter_Setts, the granite kerb's material) so the build does not reject the surface; **Vela**, a proper setts sheet is yours.
- Not done (gaps, for Datum / Latch): the stop crossing is still the one-landing `stop` kind, not kerb to kerb across both islands with heads on each (CrossingKit throws on a refuge that stands on no median: Latch first); island ramps (the 3.6 m 1:12 ends) and half-round noses on the track side are not drawn (the far corners are R 1.5); RF marks on the asphalt set for tram streets (a dash at +-7.2) not added (the enamel set lays no lozenge or seam in the setts: tested).
- Handover: sim/data/handover.site.json moved (5 rows, walkCells 2449 -> 2453: the Records stop's islands 0.1 m out to 3.2 m, its TimetableColumn and TicketKiosk now kit pieces with their collide volumes); missions:check handover --inputs tree SHIPS, 893 plays; cover restamped, --check exit 0.
- Re-pinned with notes (22, the old section's fixed counts): KR-1 160 -> 142 returns; pylon-clear remnant (520 now a 28 m span, the third stub gone); RF-1 lanes (crown-road only); crossings 9 -> 8 refuges, 315 -> 316 parts, 274 -> 276 dot rows (212/62 -> 218/58), 41 -> 40 heads (crossings, partkit, surfaces, trim tests), circus moved furniture 1 -> 2; sheet-edge corners +-14 -> +-10; median noses 50 -> 32 (R 2: 24 -> 6); road-finish chords > 1500 -> > 1400; RF-3 lozenges > 1000 -> > 700; SF-21 brass > 1000 -> > 900, service bays 32 -> 31; street count 2941 -> 3264 (2938 + 326 tram street items); the lawn-track, quarter-round, reservation-face and KR-5 tests moved onto tools/world/gen/reservation.fixture.ts (the old 10 m reservation on today's Boulevard). tools/levels skyline fixtures: kit digest regenerated (Vela's adoptions), fixture plan's café scale 34.8 -> 16.8 (brief 066's cap; C-0).
- Still red in bun run test (not the street's): people day r013 hash and people trust "plan version 0.3.0" (the people read the plan only: Datum's section moved T2's stops; the trust test replaces a version string the plan no longer has, 0.4.0) -> Pulse; drift-auto "nothing changed" (now that its replays run: skyline r011's world rules and people r012-r016's P rules moved with no declared cause) -> Aster (a person declares causes); swallowed code in unity AvatarCheck069.cs:136 and ProofFire.cs:117 -> Forma / Latch.

## Vela 075 golden rails (flush track + setts), 2026-10-28
- 19:10 START Vela 075 golden rails: bun run guard bun run decokit --only Tile_TramRail_Flush,Tile_TramRail_Flush_12m
- 19:11 END   Vela 075 golden rails decokit --only (ok, exit 0): Tile_TramRail_Flush 454 / 216 / 96, Tile_TramRail_Flush_12m 1574 / 696 / 96 (hand-built L1 and L2); setts_tex.py wrote ground_setts_v1 (+ normal, mask) and DK_Ground_Setts.mat

## Splice 075 no overhead (Sky 2026-10-28: "no overhead wires though!"), for Datum, Latch, Vela
- Removed district-wide: span wires, contact wires, hangers, rosettes, span poles. tools/world/gen/street/tramstreet.ts has no wire rule any more (tramWires gone; the std no longer reads `wires`); street.ts / index.ts / build.ts / report.ts carry none; the `span-pole` slot is gone from kitrefs.ts; contracts/world/world.schema.ts has no `transit.wires`, `TramWires` or `WireEnd`; the example json drops its wires block.
- Catalogue: TramWire_Contact / _Span / _Hanger / _Rosette / _Pole status **rejected** in kit.json, use tags [] and scale entries dropped in tools/kit/tags.ts (the reason there); `bun run kit` pruned their fbx from unity/Assets/Suborned/Kit. tools/decokit/tram075.py still builds them (Vela's; harmless, nothing places them).
- New world rule **TS-W0** (tools/world/rules/overhead.ts, registered in rules/index.ts): fails an export with a `transit.wires` key, any TramWire_* kit ref anywhere, any `span-pole` street item or a `tram/<line>/<path>/span-<n>` id. Tests tools/world/rules/overhead.test.ts (live export passes; failing examples: wires block put back, a TramWire_Hanger ref on a rail piece, a span pole in the street; the catalogue's five rejected with no use tag).
- **Datum:** done in 52286c42 (the plan's tramStreet has no `wires`, no overhead text). The generator never reads `wires` either way.
- **Latch:** TramStreet.cs draws wires from `transit.wires`; the export no longer has that key (TS-W0 fails if it comes back), so it draws nothing. WorldDto's `Wires` defaults to an empty list and TramStreet.Wires returns on Count 0, so the build is safe as it stands; remove the wire drawing (TramStreet.Wires, the HarbourWorldBuilder.cs:146 call, WirePieceDto, the WiresLayer case in Combine.cs) when convenient. No `span-pole` street items any more.
- Street count 3264 -> 3052 (the 212 span poles; surfaces.test re-pinned). Nothing else moved: sim:site unchanged (no diff in sim/data), so Handover c041 stands; cover.json restamped (kit hash only).
- Rails: tramGround takes Vela's Tile_TramRail_Flush_12m / Tile_TramRail_Flush when adopted at 2 m / 12 m (same gauge, pivots, layout), else the track-bed tiles; the report line says which. Now: 552 + 237 flush tiles, the same placements; Tile_TramRail / _12m placed nowhere (left adopted, no track bed anywhere now: retire on request). Vela's look.json `setts` -> DK_Ground_Setts is not done here (Latch / Vela, or ask me).
- K-1's landmark-gateway test (tools/plan/kit-fit.test.ts) now reads the arch's site from the plan (it pinned y 160; vista 2 moved it to 53).
- Marker briefs/075/golden/READY-nowire-splice: export 71317443…, same twice.
- 19:13 START Vela 075 golden rails: bun run guard bun run decokit --only Tile_TramRail_Flush,Tile_TramRail_Flush_12m (rerun: edge stones on DK_Granite_Rail, the kerb sheet tinted down)
- 19:14 END   Vela 075 golden rails decokit --only (rerun, ok)
- Checks after the rerun: `bun run guard bun run kit` exit 0 (the two adopted, 16 files copied: FBX + L1/L2, DK_Ground_Setts.mat, DK_Granite_Rail.mat, ground_setts_v1 maps); `bun run collide` (2 measured, flat) and `--check` ok (356 pieces, 4106 volumes); `bun run cover` and `--check` exit 0 (the two getaway-safe ambush warnings as before); typecheck 0; kit + decokit tests 45 pass 0 fail.

**Names for Splice (swap in tramGround):** `Tile_TramRail_Flush_12m` (straights) and `Tile_TramRail_Flush` (curves and remainders). Status adopted, tags `ground:tram-rail:flush` / `ground:tram-rail:flush:12m` (tools/kit/tags.ts, scale [1, 1]). Same gauge (heads at +-0.5, gauge faces +-0.4625), pivot (footprint centre, ground, track along kit +X) and lengths (2 m, 12 m: the 1 m edge stones repeat, so 2 m and 12 m butt end to end) as Tile_TramRail / _12m.
- What it is: two grooved rails (crowned 75 mm head, 35 mm flangeway, keeper flats, the old tile's section), a 120 mm granite edge stone each side of each rail (1 m stones, 4 mm joints, rounded arris away from the rail), and the setts between the rails. No bed, no sleepers, no coloured strips.
- Size [2 | 12, 0.153, 1.355]: **1.355 m wide** (edge stones' outer faces at +-0.6775), not the old 2 m, so the builder's setts cut (Layers.cs, by the export's piece size) is only the track's own width; the tile carries the setts it covers between the rails in DK_Ground_Setts, the same material the flats draw with.
- Top **3 mm over the pivot** (body to -0.15): placed at the setts top (z 0.02) as today it stands 3 mm proud, invisible at play zoom, and it wins the 2 cm bands where Latch's cut stops short of the tile's edges and ends (with a top at 0 the setts flat z-fights the rail heads at every tile joint).
- Cost: 2 m 454 / 216 / 96 tris, 12 m 1574 / 696 / 96 (hand-built L1 keeps the groove and the stone joints; L2 is a steel strip per rail on the stone and setts bands). The old tiles were 3140 and 9540. Materials DK_Stone_Dark, DK_Steel_Satin, DK_Steel, DK_Iron, DK_Granite_Rail (new .mat: the kerb granite sheet tinted #CBD4DE, a step over the setts), DK_Ground_Setts (new). Both use the shader and keywords of DK_Ground_Pavement / DK_Granite_Kerb_B (_NORMALMAP, _METALLICSPECGLOSSMAP): no new variant.
- Old tiles: Tile_TramRail / _12m stay adopted until the export places none; retiring them is Splice's call (drop the use tag, then status rejected).

**Setts sheet (look.json `setts`, Splice / Latch):** set `"material":"DK_Ground_Setts","tile":1` (world-metre UVs as every ground/ surface; the .mat tiles the sheet every 2 m). tools/decokit/setts_tex.py (run by `bun run decokit` after road_tex): granite setts 9 cm (radial) by about 10 cm (along) in a **fan (segmental arc) bond**, fans 1.0 m across, rows 0.5 m apart, alternate rows offset; mean #8F9498 (cool blue-grey, a step darker than the road's #8FAFC1, far from the old bed's near-black), a share of warm grey and fawn setts for the old reel's warmth, granite speckle, sand-grey 8 mm joints. Seeded (75), periodic in 2 x 2 m.
- Texture budget: albedo 1024, normal 1024, mask 512 = 2.36 M texels, BC7/BC5 at 1 byte a texel, **3.0 MiB with mips** (1.95 mm a texel over the 2 m tile; play zoom is ~2.6 cm a pixel, so the fans and the per-sett tone read and the joints fall into the mips as a darker line). Report in out/decokit-ground/setts.json.
- The flats' UVs are world-fixed, so on a street not along x or y the fans in the flat run at an angle to the track, while the strip between the rails (the tile's own UVs) follows it. The edge stones separate the two, so no seam shows. If it reads wrong at play zoom, the fix is in the builder (UVs along the path), not in the sheet: Latch.

**Crop (Blender, golden camera, not the game's lighting or grade):** briefs/075/golden/rails/compare.png (top: old.png's tram corner for reference; middle: today's Tile_TramRail_12m; bottom: the flush tiles on the new setts), plus before.png / after.png at the golden framing, *-crop.png and after-zoom.png. Script: tools/decokit/review075_rails.py.

## Golden: vista 2 (Datum, brief 075, 2026-10-28), for Splice, Vela, Glint, Latch
Pick: Glint's arch move, and his tower option (a) at 250 (not 252). Plan: contracts/world/harbour-district.plan.json (sha256 in READY-vista2-datum). Angles from the golden eye (cameras.json ours: (1.93, −214.57), 8.11 m, fov 58 vertical, frame top 29.5° up).
- Rejected (b): y 250 on the axis is Crown Avenue's carriageway (the avenue runs to n_avenue y 280); the nearest site on the axis is past Crown Road, and SK-2 holds a disc crown ≥ 333.4 from the gate pair, so 350 stays. Rejected (c): SpaceHQ (96, 0) stands 23.7° right of the axis, behind the Boulevard's east wall from this eye, its top (46°) above the frame; its drum carries S1 and Crown Circus station, so it cannot move onto the axis. Kept: the avenue-gate pair (±44, 190, 165.6 → 21.3°): it is the old reel's white pair flanking the gold tower.
- **Arch** `landmarks[crown-vista].gateway`: `c` [0, 160] → **[0, 53]** (the circus mouth of Crown Avenue; 267.6 m from the eye); `feet` → **[[-17, -14, 50, 56], [14, 17, 50, 56]]** (the avenue's frontage zone; the circus pavement and the notch r 51 leave x 14–17 at y 48.1–49.0, the kerb returns end at (±9, 48.1)); `crest` 68 → **95.2**, `top` 75.2 → **105.3**; `yaw` 0, `zones` ["frontage"], `kit` unchanged. Today the adopted 75.74 m piece stands at the new site and passes K-1 (feet in the frontage zone, nothing within 1.5 m, 53 m off S1). After the regen: top 105.3 at **20.0°** (old 21.7°, today 10.2°), width 34 at **7.3°** (old 13.3°, today 5.2°: the span cannot widen, the feet are held to the avenue's 3 m frontage zone x ±14–17).
- **Tower** `landmarks[crown-vista]`: `c` [0, 350] unchanged; `top` 172.4 → **250**; `r` 24.5 → **30**; kit id unchanged (DiscTower@crown-vista). Today the × 1.1 piece still stands (172.4, 16.0°). After the regen: **23.2°** (old gold HQ 42°, past the frame top), 3.2° over the arch top, 1.9° over the pair. New **`standards.skyline.vista`** { id crown-vista, tip 250 }: the one exception to rivalMax 200, under the dominant 252, so SpaceHQ stays the tallest in the district (and the tower is off the sheet, outside it). This bends GDD §10's skyline line "rival spires … ceiling 200 (≤ 0.8 × SpaceHQ)" for this one off-sheet landmark: **Aster, put it to Sky**; at 200 the tip (18.8°) would sit under the moved arch's top (20.0°) and the axis would no longer end over the gate.
- **Tram street**: `standards.streetscape.sections.tramStreet.wires` deleted whole; "Overhead wires on span wires." gone from `rule`; `checks` reads "the generator's tram-street finish, islands and angle bays". The 2026-10-28 street-section history note keeps its old text (history).

**Vela** (regens; ids unchanged, both crown gold as now):
1. ParabolicArch@crown-avenue: the vista 1 piece × 1.4 in height only. Span 34.0 foot to foot, foot centres ±15.5, plinths 3.0 across × 6.0 along (unchanged, so `feet` still match), pivot at the footprint centre on the ground. Rib crest (centreline) **95.2**, atom sphere top **105.3** (its rings a little over). The rib may thicken with the rise; depth ≤ 16.3 (the atom), the plinths stay 6.0. The leg at 8 m up stands at about x ±14.8 (keep-clear text updated). Marks tier 80–106: hold the rings under 106.
2. DiscTower@crown-vista → **top 250 at scale 1** (stand, never stretch), everything inside **Ø 60** in plan including the jib (K-1 takes max(x, z) ≤ 2r; give its pivotOffset). The old reel's gold banded tower: a gold fluted shaft about Ø 24 (DK_Crown_Gold core, ivory ribs, ivory ring bands every 12 m) carrying two ivory discs, the lower about z 150 (it shows inside the arch, 14°) and the upper about z 225 (21°, over the arch top and the pair), then the mast to 250. Tag scale [1, 1] (or keep [0.9, 1.1]: the fit takes the largest scale under 250 and inside Ø 60, so a 250 piece places at 1.0). The livery key stays (LIVERY.own DiscTower → DK_Crown_Gold gold).

**Splice**:
1. SK-1 (tools/plan/signature-checks.ts): read `standards.skyline.vista` { id, tip }: the landmark `landmark/<id>` may pass rivalMax but must stay ≤ vista.tip and < dominant.tip; everything else as now. Failing example: a second landmark at 210, or crown-vista at 252. Without it SK-1 fails the moment Vela's 250 piece is adopted (today it passes: the placed piece is 172.4).
2. kit-fit.test.ts lines 38–45 pin the arch at y 160: `at: [0, 53]`; the walk foot `[-13, -10, 50, 56]`; the out foot `[16, 19, 50, 56]`; the lamp `at = [12.8, 53]`. This is the one test red from this edit (13 pass, 1 fail in kit-fit + kitrefs).
3. The gateway code reads `c`, `yaw` and `kit` already (gatewayRef); nothing else in the generator changes. Rerun world, cover, sim:site (the arch is 311 m from the civic block; no Handover row should move).

**Latch**: rebuild only (the arch moves; the tower changes when Vela's piece lands). **Glint**: re-measure from the golden eye after the regens: expected arch top 20.0°, width 7.3°, tower 23.2°.
Checks (this tree, 2026-10-28): `bun tools/plan/cli.ts` exit 0 (K-1 3525/3525, SK-1 345/345, SK-2 15/15, TS-W0 pass); `--overlay tools/missions/handover/overlay.ts` exit 0 (C-0 café 22 × 11.3 × 16.8; C-3 21/21; V-1 64/65; M-1 667 / 1064 / 126 / 1473 m unchanged). `bun run typecheck`: 3 errors in tools/life/presentation.ts:295 (implicit any; not this edit, tools/life is being edited by someone else).

## Golden: life (Pulse, brief 075, 2026-10-28) · READY-life-pulse
Presentation-only city life for the flyby (Flyby074) and the free-fly camera: never sim data. The sim, its site builder and the mission tools never read it (`tools/life/presentation.test.ts` fails if any file under tools/sim, tools/missions or sim/ names it); play and the Handover keep the sim's crowd.

- Generate: `bun run life:presentation` (tools/life/presentation.ts) → `out/world/harbour-district.presentation.json` (~2.9 MB, git-ignored like the world export; rerun after `bun run world`). Options `--hour 14.5 --people 3000 --cars 220 --world <file> --out <file>`. Seeded by id, byte-identical on rerun. Schema: `contracts/world/presentation.schema.ts` (`suborned.presentation/1`). Reference sampler: `tools/life/presentation-sample.ts` (`sample(p, t)` gives every pose at t; the tests use it).
- Inputs: the world export (road lines, tram and sky rail lines, stops, stations, kerbside bays, frontages, street items, buildings for the clear checks), the plan (nodes, roads, sections, timetable), the life rules (`streetDensity` in tools/life/model.ts at the hour gives walkers and spill per street, scaled to `--people`; life.json loads per service, speeds) and tools/figures/figures.json (the 10 townsperson figures).
- Counts at 14:30 (afternoon): 2,867 figures (1,514 walking on 76 loops, 1,353 standing, sitting or waiting: frontage spill in pairs and singles, tram islands, sky rail platforms, benches, kiosks, phone booths); 614 car trips, 221 moving at any moment on average; 299 parked in the 388 kerbside bays (vans in loading bays); 10 tram runs per loop (T1 4, T2 3 + 3), 2 to 6 on the sheet at once; 6 sky rail trains (two head cars each); 24 aircars on 3 sky lanes. The old reel had 2,851 figures and 232 cars on one street.

Format (all positions in the plan frame, x east, y north; Unity = (x, z, y); z is a hint: snap to the drawn ground):
- `clock.periodSec` 720: everything loops exactly every 720 s (T1 180, T2 240, S1 120 headways divide it). `tau = t mod 720`.
- `routes[]`: shared timed polylines, `xy` flat [x, y, ...] and `t` [s since the route start], one t per point; a repeated point with a later t is a dwell. Linear in t between points is exact to 0.1 m. `z` per route (0 car, 0.35 hover scooter, 0.02 tram, 24 sky rail). Tram and sky rail routes carry `stops` (tArrive, tDepart).
- `timed[]`: {model, route, t0}. `d = (tau - t0) mod 720`; active while `d <= route.t[last]`, else hidden (each trip starts and ends 40-60 m beyond the sheet's edge, so nothing pops in view). Position = interpolate the route at d; heading = the segment's direction, held through a dwell. `consist` (sky rail): further cars `back` metres behind along the route polyline, `flip` = turned 180°.
- `loops[]`: shared closed xyz polylines (walk loops round each block face: one pavement forward, cross, the other back; the mall, Garden Walk, the pier, the circus pavement; 3 sky lanes). `looping[]`: {model, loop, s0, v, lat}: `s = (s0 + v * tau) mod len`, v < 0 walks the loop backwards, heading = loop direction (+ pi when v < 0), offset `lat` metres to the mover's left (walkers keep right, so lat is negative). Every looper makes a whole number of laps per period. Aircars: `bob` 0.25 m, z += bob sin(2 pi tau / 3 + any phase).
- `still[]`: {model, at, z, heading, clip}: parked cars (pivot already moved by the kit's pivotOffset), people with `idle`, `idle_alt`, `talk` (pairs face each other), `phone`, `look_around`, `sit` (on the bench, facing its front).
- Heading: radians, 0 = +x, pi/2 = +y; every model fronts +Z, so Unity eulerY = 90 - deg(heading). Walk clip speed: play `walk` at v / 1.29 (its natural speed), as HostViews does.
- `models{}`: kit ids (status from kit.json at generation) and figure ids. Adopted: every adopted kit car (today Sedan_Official, Sedan_Getaway, Sedan_Rival; the rival kept to 4 %, see "Golden: life roll-out"), Tram. Candidates, not in Unity yet: BoxTruck_Play (fallback car/official), HoverScooter, SkyRail_Car_Head, AirCar_Coral / Teal / Saucer (fallback null: skip until adopted).

Rules behind it [S]
- Cars: right-hand traffic. A trip enters at a sheet-edge node (off_*), wanders the plan's road graph (Boulevard weighted 6, primaries and districts 3, access 1, no dead ends), then takes the shortest way out. Lanes: tram streets use the traffic lane (centre 5.4) and swing into the passing lane (8.35 / 8.55) round every boarding island (sections.tramStreet.*.island.passing), so cars never share the tram lane; other roads one lane a side at the half's centre (two on Crown Road). The circus runs anticlockwise at r 40.5 (outside the T1 loop). Speeds from life.traffic (primary 11, district 8, access 5 m/s), 6 at junctions, curves capped at 2.5 m/s² lateral, acceleration 1.8 / braking 2.2. A trip is dropped if it ever comes within 4.5 m of an earlier car heading the same way.
- Trams: T1 north on the east track, round the circus loop anticlockwise, south on the west track; T2 through-running both ways on the centre tracks (track centres ± 1.8). Dwell 15 s; Comet → Records → Circus and the T2 straight runs hit the timetable to the second (39 s). The R 25 corners slow T2 to 5 m/s, so docks → comet runs 69-70 s (timetable 62) and comet → lantern 63 s (55): the timetable is D / speed and ignores the corners (for Datum / Motive if the price of a T2 ride should include them). Departures follow life.transit.schedule: T1 leaves Comet Row northbound at k x 180; T2 leaves Harbour Gate at k x 240 and Crown Terraces at 120 + k x 240.
- Sky rail S1: east on the south beam, round the skyport loop, west on the north beam, dwell 12 s, every 120 s. The timetable's 22 / 21 / 18 s runs need no acceleration; at 1.2 m/s² they take 26-29 s.
- Pedestrians: walkers and spill per street from `streetDensity(14.5)`, scaled so the whole set is `--people` (default 3,000; the full model is ~7,000 at 14:30). Walk loops run down the walk zone's centre (kerb + walk / 2 from the kerb face) and cross at the block's ends; a loop with a building, megastructure, civic mass, landmark or water on one pavement keeps to the clear one (Sunset Way 3, Crown Road 1 x 2, Apron Road 0). Tram islands hold half the stop's load per service x the afternoon factor; benches 22 % occupied, kiosks 1-2 customers, phone booths 15 %.
- Aircars: ring lanes over the primary ring (z 38 anticlockwise, z 46 clockwise), and the spine over the Boulevard and Crown Avenue (x ± 7, z 36, under the Crown Avenue arch). Every lane point is checked clear of buildings, megastructures, landmarks and signatures within 4 m of its height.

For Latch (Flyby074 and the free-fly camera)
1. Read the JSON at scene start (only in those scenes: never in play, never in the Handover); build each loop's cumulative lengths once; per frame do what `presentation-sample.ts` does (binary search on t or s). Instance by model: GPU-instanced crowd LODs (the 10 townsperson figures, 3k / 1.5k), cars, trams.
2. Animate figures: `walk` scaled by v / 1.29 for loopers, the clip named in `still` for the rest (offset each one's clip start by a hash of its id so the crowd is not in step). Turn the TimetableColumn hand from the stop's next `tArrive` if you like.
3. Draw candidates with their `fallback` or skip them until Vela adopts them; tell me (Pulse) if the flyby wants more or fewer of anything (`--people`, `--cars`, `--hour`), it is one rerun.
4. Hide `dressing.trams` (the two parked trams) in these scenes, or they sit on the track the live trams run through.
Later (Forma): the old RetroFutureMobility traffic is over budget; a civilian set at budget (sedan colourways, coupe, delivery van, hover-car) replaces the mission sedans as traffic.

## Golden: vista 2 (Vela, brief 075)
- Files I touch: tools/decokit/vista075.py (ParabolicArch@crown-avenue x 1.4 in height; DiscTower@crown-vista rebuilt at 250, hand-built L1 / L2 for both), tools/kit/tags.ts (DiscTower@crown-vista scale [1, 1]), tools/decokit/review075_vista.py + _sheet.py (vista 2 sites), kit.json (catalogue).
- 19:26 START Vela 075 golden vista 2: bun run guard bun run decokit --only ParabolicArch@crown-avenue,DiscTower@crown-vista
- 19:27 END   Vela 075 golden vista 2 decokit --only (ok, 56 s): ParabolicArch@crown-avenue 9820 / 4044 / 1364 (34.0 x 105.80 x 11.64), DiscTower@crown-vista 22224 / 11720 / 5372 (50.2 x 250.0 x 50.2, centred)
- 19:27 START Vela 075 golden vista 2: bun run guard bun run decokit --only DiscTower@crown-vista (rerun: ring bands at 64 / 32 / 16 segments)
- 19:28 END   Vela 075 golden vista 2 decokit --only (rerun, ok): DiscTower@crown-vista 17424 / 9320 / 4172 (50.2 x 250.0 x 50.2)
- 19:29 Vela 075 golden vista 2: Blender -b review075_vista.py --nowires (renders only, reads out/decokit; not a decokit run)
- 19:30 START Vela 075 golden vista 2: bun run guard bun run decokit --only DiscTower@crown-vista (rerun: discs r 29 / 23, thicker, as the reel; ribs 32 / 24)
- 19:31 END   Vela 075 golden vista 2 decokit --only (rerun, ok): DiscTower@crown-vista 16976 / 9320 / 4172 (58.2 x 250.0 x 58.2, centred)
- 19:31 Vela 075 golden vista 2: Blender -b review075_vista.py --nowires (renders only)
- 19:31 Vela 075 golden vista 2: kit, collide --check, cover, cover --check, typecheck, kit + decokit tests
- Results: kit catalogue + copy done (both adopted, footprint-centre-ground, no pivotOffset: the tower is centred now, the crane gone); collide 2 measured, --check ok (356 pieces, 4059 volumes); cover written, --check exit 0 (the two getaway-safe ambush warnings as before); typecheck 0; kit + decokit tests 45 pass 0 fail. Plan checker: K-1 3525/3525; SK-1 344/345, `landmark/crown-vista tip 250.2 over rivalMax 200` (expected until Splice's vista exception lands).

| piece | scale tag | size (x, h, z) m | tris L0 / L1 / L2 (all hand-built) | pivot |
|---|---|---|---|---|
| ParabolicArch@crown-avenue | [1, 1] | 34.0 x 105.80 x 11.64 | 9820 / 4044 / 1364 | footprint centre, ground (between the feet) |
| DiscTower@crown-vista | [1, 1] (was [0.9, 1.1]) | 58.2 x 250.0 x 58.2 | 16976 / 9320 / 4172 | footprint centre, ground (shaft axis; no pivotOffset) |

- **Arch:** the vista 1 piece x 1.4 in height only: span 34.0, foot centres ±15.5, plinths 3.0 x 6.0 (feet unchanged), rib crest 95.2, atom sphere top 105.3 (centre 102.7, r 2.6), rings 105.80 (under 106). Leg at 8 m up x ±14.83. Rib 2.5 across at the foot, 5.0 deep (was 4.4). Gold as now (DK_Crown_Gold beads and rings; plinths limestone, teal, chrome). L1: 28 sweep points, 4-sided beads; L2: 16 points, no beads or plinth hairlines.
- **Tower:** the old reel's gold banded tower, tip 250.0 at scale 1, max plan radius 29.1 (lower disc) inside Ø 60. Limestone podium drum r 22 h 4 (teal band; no gold at street level), eight gold buttress fins (r 21 to the shaft at z 46), gold fluted shaft r 12 (DK_Crown_Gold core, 32 ivory ribs) with ivory ring bands every 12 m (z 16–148), ivory collar, lower ivory disc at z 150 (r 29, 10 thick, glazed band), observation drum, upper gold fluted shaft r 9 (24 ribs, bands every 12 m), upper disc at z 225 (r 23, 8 thick), glazed drum, stepped gold crown, chrome needle to 250.0. **No crane** (Glint's flag; the old gold tower had none). Livery key unchanged (DK_Crown_Gold slot → DK_Crown_Gold__gold). L1 drops the ribs (gold core stays gold), L2 coarse lathes, no glazing.
- **For Splice (SK-1):** the checker reads the tower's tip as `base + size` = 250.2 (the landmark base 0.2). Your vista rule `≤ vista.tip` needs the same 0.18–0.2 ground allowance the dominant check uses (`tip - 0.18`) or TIP_TOL, or it fails on a piece that is exactly 250.0.
- **For Datum:** the gateway `keep` text: leg at 8 m up stands at x ±14.83 (vista 1: ±14.5).
- Sheet: briefs/075/golden/vista/v2/contact_sheet.png (renders, lods.json beside it; `review075_vista.py -- <dir> 640 --nowires`, `review075_vista_sheet.py <dir>`). The plain-ground vista from the golden eye: tower tip over the arch top and the gate pair, the lower disc inside the arch. Eevee shape and colour, not the game's lighting.

## Golden: life roll-out (Pulse, brief 075, 2026-10-28) · rollout/READY-1-pulse
Fixes s3-life.png (the parked cars nearly all one orange sedan). Presentation only, as before: positions, routes, loops and figures are byte-identical to the last file; only car models and paints changed. Never sim data (`bun run sim:site` gives the same sim/data bytes).

- **Models:** traffic and the bays draw from every adopted kit car (`adoptedCars`: Vehicles, adopted, car-sized, not tram, robot, cart, scooter or van). A car Vela or Forma adopts later joins with no code change (key `car/<kit id>`). Today Sedan_Official and Sedan_Getaway share 96 % between them, Sedan_Rival 4 % (was 8-10 %), never repainted, never two within 40 m in the bays. Moving: vans 8 %, hover scooters 4 %. 830 car looks in all: official 425, getaway 381, van 56, scooter 27, rival 24.
- **Paints** (`paints{}` in the file; `paint` on each car in `timed` and `still`): 14 1950s two-tones picked by seed per car. The old reel's eight enamels (DecoKitCitizens.Enamels: turquoise, tomato, canary, seafoam, cherry, sky, navy, Kelly green) with an ivory roof, three the other way round (ivory with a turquoise, tomato or canary roof), flamingo pink, and the two sedans' own (coral/seafoam, teal/ivory). No violets or lilacs: violet is the rival car. Bays: no two cars within 9 m share a paint (0 in today's file); moving: a car never shares the paint of the last three in at its gate. Spread: 48 to 73 cars per paint.
- **For Latch** (PresentationLife / LifeInstall; Flyby074 and free-fly only):
  1. Per car with `paint`: on the drawn model (a candidate van draws as its `fallback`, and paints that model), swap each renderer slot whose material is `models[m].paintSlots.body` / `.roof` (DK_CarEnamel_Teal / DK_CarEnamel_Ivory on the official sedan, DK_CarEnamel_Coral / DK_CarEnamel_Seafoam on the getaway) for `paints[k].materials.body` / `.roof` when present, else a copy of that slot's material with `_BaseColor` = `paints[k].hex.body` / `.roof` (one shared copy per slot and key, so 14 x 4 materials; keep the car_tex detail pair, only the base colour moves, as the old Tint did). No `paint` = draw as is (the rival). Use MaterialPropertyBlock only if it keeps the SRP batcher; shared copies are simpler.
  2. **Far tier** (`lod` in the file; presentation scenes only, never play, the Handover or the figure library's own heights): figures draw as `library` down to 1.2 % of screen height (as now, about 130 m at fov 60), then `last-lod-slow` down to 0.5 % (the last LOD, pose stepped every 4th frame; about 300 m), then `far-card` down to 0.15 % (one instanced camera-facing quad per figure from a 2-frame atlas of its last LOD baked at install, or a 2-colour capsule; about 1 km), then cull. Cars: kit LODs down to 0.4 %, then `far-box` (an instanced 12-tri box in the paint's body hex with a roof-hex top) down to 0.12 %. Read the thresholds from the file, not constants, so I can tune them by rerun. Hold 60 fps on the Air in the flyby; if the far tiers cost too much, say which and I lower them.
- **For Vela** (when READY-1-vela lands): add your colour variants to kit.json as `carPaints` `{ source, keys: { <key>: { name, body: <material>, roof: <material>, hex: { body, roof } } } }` and they replace my table (same keys if you like mine; any keys work); or adopt them as separate kit cars and they join traffic as models. Either way one `bun run life:presentation` picks them up.
- Checks (this tree): `bun run life:presentation` (2,862 figures, 614 car trips, 221 moving on average, 299 parked; carModels and carPaints in `counts`; parkedNeighboursSamePaint 0) · `bun test tools/life/` 14 pass 0 fail (new: every adopted car used, at least 8 paints, no paint over a fifth of the bays, no same-paint neighbours, rival under 6 % and never two within 40 m, tiers presentation-only and ordered) · `bun run typecheck` clean · `bun run sim:site` sim/data sha1 unchanged.

## Vela 075 roll-out step 1 (sky track, cranes, presentation vehicles and car paints)
- Files I touch: tools/decokit/skyrail075.py (new: SkyRail_Beam_4m, SkyRail_Pylon_HeadTwin), tools/decokit/landmarks.py (disc_tower_at: crane removed), tools/decokit/carpaint.py (new) + unity_mats.py hook, tools/decokit/kit.py (FAMILIES), tools/kit/tags.ts, tools/kit/catalogue.ts + copy.ts + contracts/world/kit.schema.ts (`carPaints`), kit.json (catalogue + statuses).
- 20:04 START Vela 075 roll-out: bun run guard bun run decokit --only DiscTower,DiscTower@avenue-gate-e,DiscTower@avenue-gate-w,SkyRail_Beam_4m,SkyRail_Pylon_HeadTwin
- 20:05 END   Vela 075 roll-out decokit --only (ok): DiscTower@avenue-gate-w/e 6768 / 1652 / 406 (22.1 x 22.1 x tip 165.6, fits; was 8276 with the crane), SkyRail_Beam_4m 56, SkyRail_Pylon_HeadTwin 3964 / 990 / 233; DiscTower (plain) rebuilt; carpaint 11 paints
- 20:08 Vela 075 roll-out: bun run collide (Blender measure of the 15 changed / adopted pieces; reads out/decokit, writes contracts/world/kit.collide.json; not a decokit run)

## Vela 075 old buildings (caps: mirrored twins, the intended P7 / B8 ends, the hall class)
- Files I touch: tools/decokit/caps075.py (new), kit.py FAMILIES (+caps075), tools/kit/tags.ts (MIRROR_075 + four PLAIN_USE rows), tools/kit/lod.ts (two regexes take _M), kit.json (catalogue + statuses).
- 20:11 START Vela 075 old buildings: bun run guard bun run decokit --only PortholeEnd_7,BalconyEnd_8,<24 x <cap>_M>
- 20:12 END   Vela 075 old buildings decokit --only (ok, 26 pieces)
- 20:12 START Vela 075 old buildings: bun run guard bun run decokit --only HallBay,HallCornerWing (the old hall class H through the generator, so kit.json measures their pivots; tris = the store's)
- 20:13 END   Vela 075 old buildings decokit --only HallBay,HallCornerWing (ok)
- 20:14 Vela 075 old buildings: bun run kit (28 adopted: 26 caps + HallBay, HallCornerWing), collide, collide --check, cover, cover --check, typecheck, kit + decokit tests

## Old buildings: pieces (Vela, brief 075) · rollout/READY-old-vela
Source: the old Editor/DecoKit/DecoKitFrontage.cs `Of` table (every class, its bay / end / corner and fallbacks), DecoKitCityBlocks.cs, DecoKitSouthside.cs, DecoKitCircus.cs, DecoKitBlock.cs; counts placed in the old street (out/scratch/vela075/oldcounts.json). The old placer mirrored ends and corners with a -1 X scale (`mirror:`); our export has none, so every cap now has an explicit twin `<id>_M` (tools/decokit/caps075.py: the same builder under -X about the pivot, so it is the old mirrored instance exactly). True size = the old piece's (ends and corners are 1 cm wider and a few tris more than the old: brief 048's flank walls, 1 cm proud). Nothing is scaled; all adopted, copied to Unity.

| class | storeys / body top m | bay | end (round on kit +X) | end mirrored (kit -X) | corner | corner mirrored | old placed (bay / end / corner) |
|---|---|---|---|---|---|---|---|
| S2 | 2 / 12.9 | StreamBay_2 | StreamEnd_2 | StreamEnd_2_M | StreamCorner_2 | StreamCorner_2_M | low-rise table (Southside not in the counted scene) |
| S3 | 3 / 16.3 | StreamBay_3 | StreamEnd_3 | StreamEnd_3_M | StreamCorner_3 | StreamCorner_3_M | 79 / – / 32 |
| S3W | 3 / 16.3 white | StreamBay_3W | StreamEnd_3W | StreamEnd_3W_M | StreamCorner_3 | StreamCorner_3_M | table |
| S4 | 4 / 19.7 | StreamBay_4 | StreamEnd_4 | StreamEnd_4_M | StreamCorner_4 | StreamCorner_4_M | 74 / – / 24 |
| S6 | 6 / 26.5 | StreamBay_6 | StreamEnd_6 | StreamEnd_6_M | StreamCorner_6 | StreamCorner_6_M | 98 / 12 / 8 |
| S9 | 9 / 36.7 | StreamBay_9 | StreamEnd_9 | StreamEnd_9_M | StreamCorner_9 | StreamCorner_9_M | 30 / 11 / 6 |
| S9W | 9 / 36.7 white | StreamBay_9W | StreamEnd_9W | StreamEnd_9W_M | StreamCorner_9 | StreamCorner_9_M | 35 / 14 |
| D10 | 10 / 43.6 (corner 57.4 spire) | DecoBay_10 | DecoEnd_10 | DecoEnd_10_M | DecoCorner_10 | DecoCorner_10_M | 5 / 12 / 1 |
| P3 | 3 | PortholeBay_3 (3L) | StreamEnd_3W | StreamEnd_3W_M | StreamCorner_3 | StreamCorner_3_M | 9 |
| P4 | 4 | PortholeBay_4 (4L) | StreamEnd_4 | StreamEnd_4_M | StreamCorner_4 | StreamCorner_4_M | 9 |
| P7 | 7 / 29.9 (bay 32.26 with fin) | PortholeBay_7 | old: StreamEnd_6 (fallback, 3.4 m low) · **new PortholeEnd_7** | StreamEnd_6_M · **PortholeEnd_7_M** | StreamCorner_6 | StreamCorner_6_M | 10 |
| B3 | 3 | BalconyBay_3 | StreamEnd_3 | StreamEnd_3_M | StreamCorner_3 | StreamCorner_3_M | 8 |
| B4 | 4 | BalconyBay_4 | StreamEnd_4 | StreamEnd_4_M | StreamCorner_4 | StreamCorner_4_M | 1 |
| B8 | 8 / 33.3 | BalconyBay_8 | old: StreamEnd_9W (fallback, 3.4 m high) · **new BalconyEnd_8** | StreamEnd_9W_M · **BalconyEnd_8_M** | StreamCorner_9 | StreamCorner_9_M | 9 |
| A3 | 3 / 16 (21.2 roof) | ArcadeBay_3 | HallCornerWing (old: the wing is its end) | HallCornerWing_M | HallCornerWing | HallCornerWing_M | 5 |
| H (civic hall) | 11.1 parapet, 8.3 deep | **HallBay** (now adopted) | HallCornerWing | HallCornerWing_M | HallCornerWing (now adopted) | HallCornerWing_M | 18 / – / 3 |
| Tower cap | 12 / 67.3 | (S6 run) | – | – | CornerTower_12 (10 m cell, 2 m proud) | CornerTower_12_M | 2 |
| stack (075-E) | per band | StreamBayStack_* | StreamEndStack_Base/Mid/Top | StreamEndStack_*_M | StreamCornerStack_Base/Mid/Top | StreamCornerStack_*_M | ours |

For Splice:
- Mirrors: `<id>_M` = the old `mirror: true`. Pivot as the base with pivotOffset x negated (StreamEnd_n_M [-0.81, -6.19], StreamCorner_n_M [-0.81, -3.19], DecoCorner_10_M [-0.958, -3.042], CornerTower_12_M [-0.062, -4.937], HallCornerWing_M [-0.15, -3.85]). Under your KIT_X -1 frame: a base end closes a run's tail, its `_M` closes the head (the old Run, last == End); a corner at the head of a run (the old Corner(..., n, +dir)) is the `_M`. Tags: the base's tags with every use prefixed `mirror:` (mirror:facade, mirror:skyline, mirror:face:stream-end ...), so no picker draws one by chance; place by name. Crown gold follows the base (StreamEnd_6/9/9W_M, StreamCorner_9_M are Skyline, crown gold).
- STREAM_ROWS: the old table's real pieces are above. Differences from yours: A3's end and corner are HallCornerWing (old), not StreamEnd_3 / StreamCorner_3; HallCornerWing and HallBay are adopted now (use `stream:hall-corner` / `stream:hall-bay`, category Plaza, so no `form` tag; pivots measured: HallBay [0, -3.85], HallCornerWing [0.15, -3.85]). Old hall-as-end rule: `End` with HallCornerWing places it as a corner cell at bay centre + 3 m toward the end (old DecoKitFrontage.End). The hall is 8.3 m deep against 14 m bays: the old closed the gap with a limestone deck at the parapet (HallFill, 6 x 11 m box, DK_Limestone).
- P7 / B8 ends: the old used fallbacks (StreamEnd_6, StreamEnd_9W, wrong height by 3.4 m); the old code names PortholeEnd_7 / BalconyEnd_8 as the intended pieces. Both are built now (skyline.py stream_end at 7 floors white render / 8 floors limestone, so the return tops at the bay's 29.9 / 33.3), use `stream-end:porthole-7` / `stream-end:balcony-8`, form end. Your pick: the fallback for "exactly the old", the new end for matching height (I'd take the new ones). ArcadeEnd_3 not built: the old never needed it (A3 mid-run or after a hall corner).
- Crescents (old Crescent): no special pieces in the old. `count` StreamBay_6 (class S6, 4 bays) on chords of R (old 35) facing the centre, chord angle d = 2 asin(3 / R); bay k at angle phi0 + d (k + 0.5), radius R cos(d / 2), odd bays 5 mm down (the canopies overlap at the joints); the last bay is the end (StreamEnd_6 rounding toward the tangent phi + 90 sign(step): two of the four crescents take StreamEnd_6_M). Behind the facade the chords fan apart: the old filled each joint with a procedural limestone prism (joint, and the two points 14 m back along the neighbouring bays' radials, from walk to walk + height - 0.2, DK_Limestone): keep it procedural (source: procedural), as the old did. Bays tile any R >= 35 (facades.py keeps every trim inside x ±3).
- Blocks (old DecoKitBlock): long faces corner to corner W = 8 + 6k + 8, depth 16 + 6k (28 back to back), the short ends (D - 16) / 6 bays taking the adjoining long face's class (split half and half when the two differ), both short runs open (no caps).
- Watch: CornerTower_12_M mirrors the HOTEL blade sign's raised letters (as the old instance did). Where it reads at play zoom, prefer the base on that corner or tell me and I'll make a lettered mirror.
- Contact sheet: briefs/075/rollout/old/contact_sheet.png (per class: A = corner + 3 bays + end_M, B = end + 3 bays + corner_M, true size; P7 / B8 with the old fallback and the new end side by side; renders + sheet.py / compose.py beside it). Eevee shape check, not the game's lighting.
- Checks: decokit --only (26 + HallBay, HallCornerWing) ok; `bun run kit` 430 assets, adopted 353, copied, no errors; collide 28 measured, --check ok (396 pieces, 4668 volumes); cover written, --check exit 0 (the two getaway-safe ambush warnings as before); typecheck 0; `bun test tools/kit tools/decokit` 46 pass 0 fail (kit.test crown list + the four mirrored skyline caps).
- Results: kit catalogue + copy (adopted 313 → 325; copied into unity/Assets/Suborned/Kit); collide 15 measured, --check ok (368 pieces, 4148 volumes); cover written, cover --check exit 0 (the two getaway-safe ambush warnings as before); typecheck 0; kit + decokit tests 46 pass 0 fail (one new test: the roll-out); plan checker exit 0 (K-1 3729/3729, SK-1 345/345). tools/levels/skyline/test/fixtures/kit.digest.json regenerated (the 12 adoptions). Renders: `Blender -b -P tools/decokit/review075_rollout.py -- briefs/075/rollout 960` (renders only, not a decokit run); sheet briefs/075/rollout/contact_sheet.png.

## Roll-out: sky track (Vela, brief 075 step 1), for Splice and Latch
The old reel's line (old Editor/DecoKit/DecoKitSkyRail.cs, read only): one swept ivory beam with a proud teal stripe each side on the old kit pylons every 30 m. All parts adopted in kit.json, each tagged by its own use and scale [1, 1] (never scaled except the shaft's height):

| piece | use tag | size x, h, z (m) | tris | what / pivot |
|---|---|---|---|---|
| SkyRail_Beam_4m (new, tools/decokit/skyrail075.py) | skyrail:beam | 1.27 x 1.6 x 4.0 | 56 | straight 4 m tile along kit Y (Unity +Z); **top centre at the pivot height** (z 0 = rail path = beam top), tile middle at the pivot |
| SkyRail_Pylon_HeadTwin (new) | skyrail:head-twin | 8.0 x 2.2 x 1.45 | 3964 / 990 / 233 | twin cross-head for the double trunk: the old head's cradle flaring into a rounded cross-arm, bearing pads under beams at x ±3.0; **top at z 0 = beam underside**, hangs 2.2 below |
| SkyRail_Pylon_Head (old) | skyrail:head | 3.0 x 2.2 x 2.88 | 368 | single-beam cradle, top at z 0 = beam underside: the S1 loop |
| SkyRail_Pylon_Tall (old, skyport_civic.py) | skyrail:foot-tall | 4.46 x 12 x 5.26 | 308 | 12 m flared trunk + plinth (4.46 across, 5.26 along); its top section = the shaft's base. Use it where the beam runs 22–34 m up (all of S1, height 24) |
| SkyRail_Pylon_Foot (old) | skyrail:foot | 2.86 x 1.2 x 2.86 | 200 | the low plinth (only for a beam under ~15 m; S1 doesn't need it) |
| SkyRail_Pylon_Shaft (old) | skyrail:shaft | 1.1 x 1.0 x 1.5 | 40 | 1 m unit, **stretched in height only** (scale y = length) from the foot's top to the head's bottom |

**Beam section** (old Section(.22); x across, z up, top at 0): 1.2 wide x 1.6 deep (x ±0.6, z 0 to −1.6), every corner filleted r 0.22, 4 points a corner (0, 30, 60, 90° on each fillet; 16 points, closed CCW). Stripe: z −1.05 to −0.85 on both flanks, from x ±0.595 to ±0.635 (0.035 proud, the outer edges rounded r 0.012). Materials: body **DK_Hull_Ivory** (#F4EFE3, the kit's hull plating texture, 8 m metric: the textured beam; the old was flat DK_Enamel_Ivory), stripe **DK_Enamel_Teal** (#1F6C67). Both already in Unity (no new material, no new shader variant). Exact points: `beam_section()` and `stripe_section(side)` in tools/decokit/skyrail075.py.

**Sweep (Latch, in the builder; the old way, DecoKitSkyRail.Beam/Sweep):**
1. Per export `transit.skyRail[]` line (already sampled ~2 m): a ring every ≤ 2 m of arc length s; at each ring p = line point at height `height` (24 = the beam top), tangent t, side = normalize(cross(up, t)), up = world up (no banking). Vertex = p + side·x + up·z for each section point; for a double line (`spacing` > 0: the trunk, 6 m) sweep twice with p offset ±spacing/2 along side.
2. UVs metric so the tile and the sweep match: **u = s in metres along the line, v = metres of perimeter round the section from the first point** (DK_Hull_Ivory's .mat already scales by 1/8). Smooth normals within the fillets (the section's 30° steps), the old CCW-outward winding. Two submeshes: body (DK_Hull_Ivory), stripes (DK_Enamel_Teal, two open strips or the closed band above). The loop's R 40 curve needs no special piece: the sweep follows the line. Straight runs may use SkyRail_Beam_4m end to end instead (same section, same UVs).
3. Export `width` 1.5 → **1.2** (Splice): the old beam; `depth` 1.6 stays; the cars' skirts (inner faces x ±1.0) clear either. skyrailEnvelope half 3.75 still holds (beam outer edge 3.6).

**Pylons (export `pylons[]`, yaw = the line's tangent):** at each pylon point, ground g: SkyRail_Pylon_Tall at (g), SkyRail_Pylon_Shaft from g + 12 to the head's bottom (scale height = (height − depth − 2.2) − (g + 12): 8.2 m on S1 at ground 0), then the head at height − depth (22.4): **HeadTwin where `spacing` > 0** (trunk), **SkyRail_Pylon_Head** on the single loop. Splice (rule S-K1): put these kit refs in the export per pylon (foot / shaft / head ids and the shaft length) and drop the procedural `pylonSize` boxes and `capDepth` cross-head; the Tall plinth is 4.46 across x 5.26 along, inside Circus Road's 6 m median (0.77 clear a side). The export's `ground: −2.5` on the trunk looks like a placeholder: pylons stand on the median's pavement.
**Station look:** the old reel had one station (old SkyRailStation, single beam); ours is SkyRailStation_Twin (already adopted, built from it for the twin beams, platform 24.0), so stations need nothing new. The beam should run through it at the same section.
**Livery:** DK_Steel_Ivory on both heads (the old head's cap, the twin's pads) takes the old `vermilion` key (Sky category), as the old town did; nothing else on the track is recoloured.
**Trains (075-D / Pulse):** SkyRail_Car_Head and _Mid adopted; car origin = beam top centre, 18.3 m long (old consist spacing 18.4).

## Roll-out: cranes (Vela)
Removed from tools/decokit/landmarks.py `disc_tower_at`: the lattice crane (king post, jib and counter-jib, counterweight, stays) and the offset lattice mast. In their place one ringed mast on the axis to the same tip (ivory tapered stem, three chrome collars, chrome needle, a chrome ball at +24). Affects DiscTower (the plain piece, now the generator's binary), DiscTower@avenue-gate-e/w (the pair flanking the arch; regen orders fit, tip 165.6 unchanged; 8276 → 6768 tris, upper extent now symmetric ±20.1). No other piece carries that crane (Tower_Needle and the rest checked; the vista tower was done before). The towers no longer carry DK_Steel_Ivory or DK_Concrete, so the vermilion livery no longer lists them. Latch: rebuild picks it up.

## Roll-out: cars (Vela), for Pulse and Latch
Adopted for presentation: **HoverScooter** (the candidate Pulse names; the other `Scooter` stays candidate), **SkyRail_Car_Head**, **SkyRail_Car_Mid**, **AirCar_Coral**, **AirCar_Teal**, **AirCar_Saucer** (old DecoKit binaries, at their own size; tags vehicle:scooter / vehicle:skyrail-head / -mid / vehicle:aircar, never the generic `vehicle`, so nothing in the world places them). Pulse: drop the fallbacks for these.
**Car paints** (tools/decokit/carpaint.py → kit.json `carPaints`, the .mats copied with the kit): 11 materials `DK_CarPaint_*`, each DK_CarEnamel_Ivory's own .mat with only the base colour changed (same flake / orange-peel detail pair, clear coat; no new SetPass). A key = body + roof. Slots: Sedan_Official body DK_CarEnamel_Teal, roof DK_CarEnamel_Ivory; Sedan_Getaway body DK_CarEnamel_Coral, roof DK_CarEnamel_Seafoam (the roof slot also paints the A/C posts and the flank flash). Latch swaps those two slot materials per instance; chrome, glass, lamps, whitewalls stay.

| key | body | roof | family |
|---|---|---|---|
| tropical-turquoise | DK_CarPaint_Turquoise #3FA9A5 | DK_CarPaint_Cream #F2E8CF | pastel |
| larkspur-blue | DK_CarPaint_Larkspur #7FA7D3 | Cream | pastel |
| dusk-pink | DK_CarPaint_DuskPink #E8A3AE | Cream | pastel |
| seafoam-green | DK_CarPaint_Seafoam #9CCFB4 | Cream | pastel |
| canary-yellow | DK_CarPaint_Canary #F0CF6A | Cream | pastel |
| orchid | DK_CarPaint_Orchid #B5A2D4 | Cream | pastel |
| matador-red | DK_CarPaint_MatadorRed #B3262C | Cream | saturated |
| harbor-blue | DK_CarPaint_HarborBlue #2D57A3 | Cream | saturated |
| tangerine | DK_CarPaint_Tangerine #E0773A | Cream | saturated |
| onyx | DK_CarPaint_Onyx #24272B | Cream | saturated |
| cream-turquoise | DK_CarPaint_Cream | DK_CarPaint_Turquoise | reversed |

Pulse: mix from these keys (suggest pastels ~55 %, saturated ~35 %, reversed ~10 %, no two neighbours in a queue alike); keep the unpainted Sedan_Official / Sedan_Getaway out of traffic, so Vale's car and the getaway stay readable as themselves (no key repeats their pairs). Rival stays as is.

## Remap: heights and levels (Aster, Sky 2026-10-28) — read this before planning or placing buildings
Sky: "the new town had the skytrak going through stations, and had stair cases, multilevel aspects." / "That said, we wanted to lower to around 3 stories high for most of the buildings so game plan view wasn't just always removing buildings floors just to see characters. We either make the spaces a lot larger, or lower the floors level"
- MOST BUILDINGS 3 STOREYS (old _3 variants); 4 only for corner and hero buildings that don't block play views. NO 6/9/10-storey slabs in the district (drop OLD_STREET's 6 and 9 codes on the boulevard; drop the canyon south of Market Street and the boulevard's east side). Height comes from slender landmarks, towers and the skyline beyond the edge.
- KEEP OUR MULTILEVEL NETWORK: sky track through stations, stairs, ramps, lifts; levels 0 / 9.43 / 24 m. The old town gives the ground layout and the look; where the old sky rail route differs from our station and deck network, keep ours, restyled with the old route character and pieces.

## Old buildings: plan (Datum, brief 075, 2026-10-28) · rollout/READY-old-datum
Sky: "where's the rounded corners etc - why can't we just use the old buildings ?" The plan now carries the old DecoKit block grammar, so the old pieces stand at true size with no scaling or piers. Source: DecoKitFrontage.cs (runs, caps, Crescent, Wedges), DecoKitBlock.cs (8 + 6k + 8, D = 16 + 6k, 14 m bays), DecoKitCityBlocks.cs (walls broken at lanes with end caps, four crescents round the circus), DecoKitStreetLayout.cs (R 2 kerb corner cells).

**Where it is (contracts/world/harbour-district.plan.json)**
- `standards.blocks`: the grammar (bay 6, cell 8, band 14, backToBack 28, solidBelow 40, innerFrom 48, innerClear 10, passage 6, split 156, caps, rank), `plots` (the 18 laid plots), `exempt` (11, each with its reason), `zone` (the capstone zone, the same as `zoneOf`), `kerbReturn` (R 2, staged), `fields` (this schema in words).
- Top-level `blocks`: 44 blocks (32 perimeter, 12 inner), 157 runs, 92 corner cells.
- `circus.crescents`: four crescents (sw 7, nw 7, ne 5, se 5 bays) plus `crescentsWhy`.
- Checked by BG-1..BG-5 (tools/plan/block-grammar.ts, tests in block-grammar.test.ts). A plan without `standards.blocks` gets no BG check, so pinned plans replay as they were.

**Block fields (what the generator reads)**
- `{ id, plot, kind: perimeter | inner, rect [x0, x1, y0, y1], depth 14, court: solid | open, runs[], corners[] }`. `rect` is the block's outer faces, on or inside the building line. A perimeter block is a ring of 14 m bands behind its runs. Solid means under 40 m deep: 28 is back to back, 29 to 39 has the middle decked at the podium. Open means a court. An inner block is free-standing in a big court, with corner cells all round and faces `court`.
- Run: `{ face: n|e|s|w, a, b, n, faces, caps: [at a, at b], bays, gap? }`. The run lies on the facade line, a to b in +x or +y. `n` is the outward normal (the facade faces it). `faces` is the class:id it fronts, `passage` for a lane inside the plot, or `court`. Length = caps + 6 × bays exactly (BG-2).
- Caps:
  - `corner`: the 8 × 8 cell at that end, shared with the perpendicular run that also caps there; the point is in `corners`. Place the corner piece once per cell: the old Run gave it to the long faces, and the short walls start 8 m in with open ends. Here both runs list the cell, so take it from either, never both.
  - `end`: the run's last bay rounds off (StreamEnd, or `_M` at the head). Ends face a passage (the old 6 m lanes) or a plaza.
  - `open`: the run joins a party wall or a neighbour's run. It is flush.
  - `join`: the run stops at a reserved mass (tower podium, megastructure, signature ground). `gap` (under 6 m) is closed by that mass: grow the podium or the mega's wall to meet the run, or close it with a blank pier in the podium's material.
  - `arc`: the run meets its crescent at the circus. `gap` is the distance to where the facade line meets the circle.
- Passage faces: a block deeper than 28 shows a run of (D − 28) / 6 bays toward the lane, open at both ends, between the long faces' end bays. `gap` is the split leftover at its two ends; the end bays' flanks close it.
- Ground outside blocks inside a plot (passages, forecourt leftover under 6 m, courts) is paved or planted, never built. Passages are 6 m (up to about 7.5 where they absorb the leftover). They are walkable (only buildings block) and need flags, lamps and maybe a gate arch, not roads.
- Rhythm, classes and heights are yours, as the old per-wall Seg lists were: one building per segment, taller segments rounding toward lower neighbours. Heights follow Aster's remap above: most buildings at the old _3, 4 on corners and heroes. The lamplight area also keeps its play-street cap.

**Crescents (circus.crescents)**
- Each crescent is 6 m chords on the building-line circle `r` 51 about (0, 0). Chord midpoints sit on r, so nothing stands past the line, and the joints are on `rEnds` 51.09. One chord turns `stepDeg` 6.7446°.
- `deg [start, end]` are plan degrees, counter-clockwise from +x. `sweep` is cw or ccw. Bay k is centred at deg0 ± (k + 0.5) × step, radius r, facing the centre (the old Crescent with R 51).
- Caps are `open` then `end`. The crescent starts where the N-S street wall's facade line meets rEnds, within a bay of that run's arc end, so it overlaps the run by under a metre. Make that join open: the crescent continues the wall. Its last bay is the end cap, rounding off toward the E-W face. `gapM` is the arc left before the E-W face; the E-W run's own arc gap sits there too. Leave it paved, as the old crescent's mouth into the parade was.
- Behind the facade, each 14 m band fans apart. Close every joint with the old procedural limestone prism (DecoKitFrontage.Wedges).
- The four crescents:
  - crescent-sw (lamplight-east): from the Boulevard's west wall to Circus Road, 7 bays.
  - crescent-nw (avenue-court/a): from Crown Avenue's west wall to Circus Road, 7 bays.
  - crescent-ne (parade-north/a) and crescent-se (harbour-boulevard/c): toward Crown Parade, 5 bays each. Both stop where the band would enter the SpaceHQ ground (x ≥ 56), and the SpaceHQ plaza takes the mouth.
- The old crescents were S6. Ours take the remap's low class.

**Plan changes beyond the new keys**
- towers.avenue-needle: rect y 46..66 → 58..78, the same tower 12 m north. Its podium stood on the NW crescent's band. All checks hold, including SK, K-1 and S-C1.
- lamplight-court-w: the L poly → rect [-244, -164, -114, -33]. Its arm on Circus Road (24 m deep, too shallow for a 28 m block) is the new open plot `lamplight-green` [-244, -190, -33, -9], a small green next to the Gardens Forecourt.
- Roads, tram, sky rail, stops and stations are unchanged. No block forced a road change.

**Exempt (keep today's massing)**
- Handover zone: records-square, records-office, garden-row-east, pylon-row-east, rocket-west, lamplight-south-w, lamplight-south-e. Each reaches into the capstone zone [-226, -84] × [-372, -148].
- Megastructure: crown-terraces.
- Sheds: docks-court (under the Market Deck), docks-south, docks-east.
- harbour-east (the ferry offices) is laid.

**R 2 kerb returns (staged, not live)**
`streetscape.kerbReturn.byClass` stays 6 / 8 / 6 for now. I tried 2 / 2 / 2 everywhere:
- Every rule passed but SF-22. The island crossing street/island/circus-road/l/a stands 1.0 m from the circus-mouth tangent point, and landings.clear asks for 3.
- The capstone programme inside the zone also changed: 1589 → 1632 pieces, kerb 909 → 949 m. V-1 (64/65) and the four car routes held.

Splice, two asks:
1. A zone-aware radius in kerbreturn.ts. A corner inside `standards.blocks.zone` keeps byClass; outside it takes `standards.blocks.kerbReturn.r` (2).
2. The SF-22 island landing re-solve for R 2.

Then I flip it on.

**Evidence (2026-10-28, 20:40)**
- `bun tools/plan/cli.ts` exits 0. BG-1..BG-5 all PASS:
  - BG-1: 18 laid, 11 exempt.
  - BG-2: 157 runs, 92 corner cells.
  - BG-3: 44 blocks.
  - BG-4: 41/41 street corners.
  - BG-5: crescents 7, 7, 5, 5.
- `bun tools/plan/cli.ts --overlay tools/missions/handover/overlay.ts` exits 0:
  - V-1 64/65 (+1 by design), M-1, C-0, C-1, C-3 and W-1 unchanged.
  - Routes unchanged: getaway 667, official 1064, getaway-service 126, rivals 1473 m.
  - An earlier run in the shared tree failed only on world rules from the generator work then in progress; it has since passed.
- `bun run typecheck` 0. `bun test tools/plan/block-grammar.test.ts` 7 pass.
- `bun test tools/plan`: 236 pass, 1 fail, run while the generator WIP still failed. Re-run afterwards, `bun test tools/plan/world-rules.test.ts`: 65 pass, 0 fail.

## Old buildings: frontage (Splice, brief 075, 2026-10-28) · rollout/READY-old-splice
The old frontage system builds every block Datum lays (plan `blocks`, `circus.crescents`) from the old DecoKitFrontage / DecoKitBlock / DecoKitCityBlocks rules, at true size. New module `tools/world/gen/oldblocks.ts`; report `bun tools/world/oldblocks-report.ts [--json briefs/075/old/oldblocks.json]`; tests `tools/world/gen/oldblocks.test.ts` (10, with the failing examples).
- Runs: 6 m bays between the plan's caps. corner = the 8 x 8 cell on the class's StreamCorner, placed once by the longer run (the old long faces); end = StreamEnd at a run's tail, its `_M` twin at the head (Vela's caps); open / join flush, rounding off only toward a lower neighbour (or none) beyond; arc flush. A streamline segment taller than the next by more than 1 m rounds toward it (old Bays). One building per segment; a short wall's bays inside a long wall's band belong to that wall's end building (old Ends). A solid block shallower than 40 m splits at the short wall's bay line (k - k/2 | k/2). Masses are the bands (14 m), each building one body at its class coping.
- Rhythm: OLD_WALLS is the old CityBlocks walls as data; segment lengths and class weights per wall kind (street: district / circus / primary faces; terrace: the rest) are counted from it, never the same class twice in a row, a corner segment always a class with its own corner, segments at least 3 bays (a shorter leftover of a short wall joins the building beside it). Seeded by block and run id.
- Heights (Aster's remap): every class is the old 3-storey variant (S6 → S3, S9 / S9W → S3W, D10 / B8 → B3, P7 → P3, A3 mid-run only); a corner building takes its 4-storey sibling on a seeded half (S4, P4, B4); the play streets take PortholeBay_3L / _4L and no arcade. B-2 reads `old`: coping on the old 3.4 m grid, storeys 3-4.
- Crescents: the plan's four, chords on rEnds facing the centre, the end cap at the sweep's end. Their fanned 14 m bodies reach into the boulevard / Crown Avenue / Circus Road runs' bands (the runs meet the circle), so each crescent and the run buildings it reaches are one building (the crescent continues the wall; B-4 and K-6 hold); the crescent's fan is a wing mass of it.
- Today's grammar stays on the Handover zone and the exempt plots, and on every plot of a plan without `blocks` (pinned plans replay as they were).
- Also: roofs.ts keeps the sign frame off the old 3-storey roofs (the long 14 m roofs drew it on 90 of them; 6 now, on the 4-storey corners, 5 boards); colourways.ts lets two old-street palette buildings that meet across a cross street step to the next palette colour (CW-1); frontage.ts gives the end anchor another trade when it would repeat the start anchor with no middle (F-3); facades.ts refuses a bay with a ring vertex inside its body (K-6 at a concave notch). None of these moves anything the checks passed before.
- Counts (export 85559b33…): 18 plots, 324 buildings, 1675 true-size pieces (1436 bays, 92 corners = the plan's 92 cells, 147 ends of which 72 `_M`), 4 crescents with 24 bays; 0 old pieces scaled; whole export 3513 facades, 1891 true size, 1622 scaled (the every-face modules on backs and courts, and today's grammar's 128 street pieces in the Handover zone and exempt plots).
- For Datum: (1) the crescents' first and last bodies stand inside the arc-capped runs' bands; I merge them, but runs that stop where the crescent's band begins would let each crescent be its own building as the old was; (2) `join` gaps are left as ground (no pier or podium growth yet). For Latch: rebuild; the crescent chords and the `_M` ends are new placements. For Vela: no new piece needed; HallBay / HallCornerWing (the hall class) are not used (the civic hall is the Handover's).

## Remap: plan (Datum, brief 075, 2026-10-28) · remap/READY-draft, remap/READY-swap

**What:** the district plan is the old DecoKit town, transcribed into `suborned.plan/1` by `tools/plan/oldtown/transcribe.ts` (reads the old C# through `tools/plan/oldtown/old.ts`, sha256 of every source in `plan.oldtown.sources`; base = `briefs/075/remap/base.plan.json`, the live plan before the swap). Draft `contracts/world/harbour-district.oldtown.plan.json` (sha256 1042dbf7…) is now also `contracts/world/harbour-district.plan.json` (v0.5.0). Comparison sheet: `briefs/075/remap/sheet.html`, CAD sheet `briefs/075/remap/plan-sheet.html`. Every hand decision is listed in `plan.oldtown.manual` (38).

**Frame:** plan (x, y) = old Unity (x, z − 206), unscaled. Crown Circus at the origin, the boulevard on x 0, walls at x ±18.

**Handover zone, unchanged:** civic block, `civic.*`, its 8 streets, nodes, gates, plots; lamplight-south-w/-e and their neighbours on every edge (Lamplight Lane at y −120, lamplight-east, boulevard-west, the garden walk), so their buildings and building lines are the same. `sim/data/handover.site.json` rebuilt from the new plan is byte-identical (sim.test passes). The takedown file differs only in item id counters (phone-booth-452 → -302 …; same items, same places).

**For Splice (the frontage port):**
- Blocks: `plan.blocks[].runs[]` (BG format) with `segs` (old classes and bay counts) and `old` (the C# call) on the boulevard walls: boulevard-west (old W1), lamplight-east (old W2 from y −126, 12 bays off its south end), west-wall-n (W4), east-wall-s (E1, 11 bays: 6.5 m side-street pavements), east-wall (E2 7 bays, E3), east-wall-n (E4). Passages: the west lane (y −204..−198) in boulevard-west, the east lane (y −72..−66) in east-wall. Caps: corner / end / arc as the old Corner / End / Open-at-the-circus. The other laid plots (terrace-s, lamplight-west, n1, n2, ss1–ss4, lantern-west, lantern-south) are fitted by the grammar with no old classes.
- Crescents: `circus.crescents` (4 × 4 bays S6, chords on r 35, r_ends 35.13).
- Old data for props and presentation: `plan.oldtown` (walls, shorts, crescents, hallFill, southside blocks marked kept/trimmed/gone, gardens canal/pool/pavilion, harbour terrace/steps/promenade/quay lines and levels, piers, every landmark Put incl. ParabolicArch and SkyBridge, background towers).
- Sky rail geometry: `plan.oldtown.skyrail.main` (closed spline, x height y), `.harbour`, `.planned.S1/S2` (radii, spans, stations) and `.levels`.

**Asks (blockers I did not work around):**
1. Splice: station banks are laid only for east-west lines on y 0 (`tools/world/gen/connectors.ts`: "banks are laid for an east-west line", and the bank rects use ±(t+c) from y 0). Every old station is on a north–south line, so neither old line is in `plan.skyrail`. The plan's S1 (Circus Road, Western Park and Orbit Gardens stations, north past the Sphere) is the only line that can be built now. Switch on with `transcribe.ts --with-skyrail` once N–S banks exist.
2. Splice: the old main loop's boulevard leg had pylons on the west promenade. `skyrail.pylonZones` has no pavement or promenade zone. Does a promenade pylon zone go in, or does the track move?
3. Splice: BG-5 reads crescent points from the origin (fine here, the circus is at 0,0). `pier.ts` lounges and lift names assume two legs on y 0. The block-grammar test fixtures name plots that are gone.
4. Keel: `tools/sim/getaway.test.ts` uses node `sw` (gone; `w_market` is its place). Takedown needs `bun run sim:site` for the counters.
5. Pulse: timetable lines regenerated (T1, S1); journeys cleared; `tools/life/presentation.ts` and life.json still name T2.
6. Sky's calls (Aster): the gate towers stand at 108 m (harbour cap), not the old 156.6. The ParabolicArch isn't placed (its 34 m span needs a district pavement; Crown Avenue is access). The Skyport is the old Skyport_Tower landmark at the old place, not today's portal megastructure. The crown-vista tower is gone (SpaceHQ closes the axis again). The two megastructures are terraces (West Terraces, Lantern Terraces), kept only for MS-1.

## Remap: frontage, stations, arch (Splice, brief 075, 2026-10-28) · remap/READY-splice
**1. Old frontage on the new blocks.** Every laid plot (16) is built by `tools/world/gen/oldblocks.ts`, none falls back; exempt plots (Handover zone, the two terraces' plots) keep today's grammar. A run that carries `segs` (Datum's old Wall / Short lists on the boulevard walls) now builds those segments, one building each, at the remap's low class (S6 → S3, S9 / S9W → S3W, D10 / B8 → B3, P7 → P3; a corner building may take its 4-storey sibling); a segment under 3 bays joins its neighbour (today's massing width); H (the old 11.1 m hall, its corner wing over its body) takes S3; A3 is kept mid-run only. Segs that do not sum to the run's bays are ignored and logged (`segMiss`). Runs without segs keep the seeded rhythm counted from the old walls. Counts (`briefs/075/remap/oldblocks.json`): 16 plots, 85 buildings, 522 old pieces at true size (420 bays, 54 corners = the plan's 54 cells, 48 ends of which 24 `_M`), 4 crescents with 16 bays, 0 old pieces scaled. Export: 648 street pieces, 522 true size, 126 scaled (today's grammar in the Handover zone); 1492 facades in all, 603 true size, 889 scaled (the every-face modules on backs and courts).
**2. North-south stations.** `connectors.ts` lays a station's bank in the line's own frame (along / across the line's centre, the platform's side read from the platform), so a north-south station gets the same bank turned a quarter (yaw π/2); an east-west bank is unchanged (the export hash did not move). The plan's forecourt bank (Garden Walk) stays east-west only and says so. `mega/pier.ts` lounges, apron and lounge edges are in the line's frame too. Test: `tools/world/gen/connectors.test.ts` (the old S1 in the live plan: The Sphere's banks both sides, yaw π/2).
- `--with-skyrail` stays off. With N-S banks the old S1 builds its banks (Circus Road and The Sphere), but the plan fails on rules outside the generator: S1/sphere stands on a plot (N-P2), the T1/terraces interchange names S1/west-end (S-I1), and the old S1 replaces our S1 (same id, same Circus Road leg), which Sky said to keep. The old S2 (harbour) needs the Skyport Tower megastructure the remap took out (S2/skyport: no walk, never reaches the street, L-1, L-2, W-2, W-3, S-S1), its r side stands over the water with no public ground for a bank (L-5 apollo/r, harbour/r), its spacing is 192 m (S-S2) and the trunk has 73 m with no pylon ground (S-P1). Datum, if wanted: S2 on the quay needs its stations' far side on the quay or a pier (or one-sided banks, which the rulebook does not have) and an end that is not the old portal.
- Promenade pylons: no promenade pylon zone. The boulevard's pavement is the tram street's walk and frontage (SF-1, SF-13 keep it clear), and Sky's call keeps our network, so the old main loop's boulevard leg stays data in `oldtown.skyrail.main`; the old character comes from the old pieces on our line (SK-T1..3).
- Also: a pylon's plinth now keeps off a landmark disc (the frozen skyline fixture stood its plinths on the monument steps; SK-T2): the bare shaft stands there instead. No change to the live export.
**3. The Crown Avenue arch.** K-1 now takes a gateway foot in the frontage zone up to the building line where a block's run fronts the street (the paved setback between the access section's 6.5 m pavement and the old wall at x ±18). With `landmarks.disc-west.archWas` moved to `gateway` (the feet at x ±14–17, y 41–47 as Datum carried them), the plan passes every rule (scratch check). Datum: please move it (drop `status`/`name` or keep `name`), or give it a landmark of its own; nothing else in the plan has to change. Failing examples in `tools/plan/kit-fit.test.ts` (no run on the avenue → not in its zone; walk, outside the piece, a lamp, not emitted).
**4. Fixtures.** BG-5 reads each crescent about its own centre (`c`); the BG fixtures name today's blocks. The census and failing-example tests written on the plan before the remap (towers, pier tower, galleries, Crown Road, T2, the market deck…) run on that plan pinned (`briefs/075/remap/base.plan.json`), generated by today's code; the live plan is held by the world rules. Re-pinned on the live plan: RF-1 lanes (none), the 8 mouth faces, KR-1 69 returns, the takedown dock id (-315 → -165, the counter only), oldblocks counts.
**For Vela / Aster (kit):** `SpaceHQ_Portal`'s order changed with the remap (moved to (0, 244), no portal), so its kit order hash is stale, and the tower and Skyport pieces' orders are no longer in the plan (`tools/decokit/fit.test.ts`, 2 red). Regenerating SpaceHQ_Portal to the new order would change the piece (no beam opening): Vela's call. Nothing deleted from kit.json.

## The Crown Avenue arch's ground (Splice → Datum, brief 075, 2026-10-28)
`kit-fit.test.ts` now reads the live `landmarks.disc-west.gateway` (no more `archWas`); K-1 and the plan pass. For the arch's text (`piece`, `whyNot38`, `siteWhy`, `keep` still describe the vista-2 site at y 53, plinths y 50–56, frontage x 14–17, building line x 17), the measured ground at its site, both sides alike, every y from 38 to 50 (probe `out/scratch/splice/arch-zone.ts`, K-1's own test): Crown Avenue's kerb face at |x| 10, kerb + walk zones to |x| 13.5 (3.5 m), the 6.5 m access pavement to |x| 16.5, then the 1.5 m paved setback to the old wall's building line at |x| 18 (block runs facing `access:crown-avenue`). So the frontage zone at y 41–47 is |x| 13.5–18 (4.5 m), and the feet at |x| 14–17 stand 0.5 m inside the walk's edge and 1 m off the building line; the plinths have buildings behind them here (the old walls), so the `keep` line about shopfronts behind the plinths now applies (|x| 18, y 41–47).

## Remap: roofs (Splice-roofs, brief 075, 2026-10-28) · remap/READY-roofs-splice
**What:** the old town's roof programmes (DecoKitRoofline.cs + DecoKitRoofThemes.cs, read only) are generator rules in `tools/world/gen/roofline.ts` (RL-1..RL-4), called from `tools/world/gen/roofs.ts` (`oldRoof`). Every ordinary roof outside the Handover zone (plan programmes antenna-mast, water-sphere, roof-garden, sun-terrace-pool, tennis-court, solar-dish; not towers, crowns, sign-frame, hover-pad, saucer or the industrial forms) takes programme `roofline`: the building's top inset 0.3 m, turned so +v faces its street; the plan's always-items keep their counts in one plant bay (plant house + rocket vents) at the end away from a hero corner; the rest splits at 6 m joints into 12-30 m plots (RL-1), each a weighted theme that fits and is never the plot before's (RL-2: lido 2, tennis 1.5, garden 2, cafe 2, glass 1.5, machine 1.5, sun 1; else planters), laid from the old items at true size on a terrazzo deck (lawn + kerb on gardens) (RL-3). RL-4: no lattice masts; a 4-storey corner of the old frontage gives its corner plot one Roof_Mast, at least 120 m from any other. The Handover zone's roofs (`zoneOf`, building top centroid) are built exactly as before. Seeded by roof id.
**Counts (same tree, roofline off → on; 240 roofs):** masts 164 → 11 (4 in the Handover zone as before, 7 hero Roof_Mast; Roof_LatticeMast 1 → 0) · pools 1 → 22 (21 lido plots + the zone's one) · gardens 26 → 53 (27 garden plots + the zone's 26 roof-garden roofs) · cafés 0 → 39 · sun decks 0 → 36 · also glass 11, tennis 1, machine 58, planters 15; 182 roofline roofs, 0 repeats in a row.
**Shared edits (small):** `contracts/world/world.schema.ts` Roof `plots?`, `kits?` (each KitRef with its own `z`); `tools/plan/street-rules.ts` SF-15 checks a roofline roof plot by plot; `tools/plan/kit-fit.ts` K-1 checks each `kits[]` piece in its roof rect and on its slab. Failing examples in `tools/plan/world-rules.test.ts`; rules in `tools/world/gen/roofline.test.ts`. Re-pinned: kit020 (water-sphere 39 → 10, mast roofs only in the zone), surfaces (roof tubs 394 → 385), gen.test (roofline fits plot by plot).
**For Latch:** `RoofKit.cs` draws `roofs[].kit` and `.crown` only; please also place each `roofs[].kits[]` piece (KitRef at `at`, height `z`, its own `yaw`; the KitLiveries call as for `kit` with role "kits") and rebuild. Until then the roofline roofs show only their decks, lawns, plant and vents. 1322 roof pieces (CafeSet_Tulip 314, Deckchair 265): worth GPU instancing / a perf look on the Air.
**For Datum:** the plan's `standards.buildings.roofs` doesn't name `roofline`; SF-15 accepts it through the generator's theme table. If you want it in the plan, add a `roofline` programme (setback 0.3, the theme table) and I'll read it from there.
**For Vela:** every old roof piece the themes name is adopted (Roof_Bandstand, Roof_Radome, Roof_Dish and Roof_Drum too); `STAND_IN` in roofline.ts is only used if one is set back to candidate.

## Remap: pieces (Vela, brief 075, 2026-10-28) · remap/READY-pieces-vela
- Files I touch: contracts/world/kit.json (statuses + catalogue), tools/kit/tags.ts (USE_075R + scale [1, 1] for the remap's old pieces), tools/decokit/ground_tex.py (lawn: stripes out), tools/kit/kit.test.ts (the remap test). Not tools/world, not the plan.
- 23:41 START Vela remap: bun run guard bun run decokit --only <the 34 old pieces adopted for the remap; list below>
- 23:42 END   Vela remap decokit --only (29 assets exported, lawn sheet re-made by ground_tex.py; exit 1 only from the fit table's stale tower / SpaceHQ_Portal orders the remap dropped, as Splice logged, none of them in this run)
- 23:43 START Vela remap: bun run guard bun run decokit --only DecoPavilion,PlanterBed (the batch run measured DecoPavilion's depth as 2.3 m, the FBX is 6.99 as the old; PlanterBed's LODs did not reduce)
- 23:46 END   (DecoPavilion still 2.3 m: the screen outline's doubled point at its 0.35 m step made the bevel emit 4 NaN vertices; structures.py drops repeated points, outline unchanged)
- 23:46 START Vela remap: bun run guard bun run decokit --only DecoPavilion
- 23:47 END   Vela remap decokit --only DecoPavilion (ok: 7.78 x 7.4 x 6.99 as the old; exit 1 only from the stale tower / SpaceHQ_Portal orders)

## Remap: asphalt road (Vela, brief 075, 2026-10-28) · remap/READY-asphalt-vela
- Files I touch: tools/decokit/road_tex.py (asphalt(): the old DK_Asphalt_Road sheet + normal byte for byte, hash-pinned; the 055 made sheet retired), road_ribbon.py (DK_Ground_RoadAsphalt = the old .mat: 16 m, smoothness .28, no mask; DK_Road_Mark = the old DK_Road_Paint #E8E2D2 matte), unity_mats.py (SUPPORTED + roadpaint), look.json `road-asphalt` (tile 1, note, palette mean), road_board.py (crops: setts, islands, road flats). Not the plan, not tools/world.
- 23:50 START Vela asphalt: bun run guard bun run decokit --mats-only (road_tex asphalt = the old sheet; DK_Ground_RoadAsphalt, DK_Road_Mark)
- 23:52 END   Vela asphalt decokit --mats-only (305 materials written; exit 1 only from the fit table's stale tower / SpaceHQ_Portal orders, as before)
- Checks: `bun run guard bun run kit` exit 0 (430 assets: adopted 353 → 382, candidate 33, rejected 15; 262 files copied; kit.json changes = the 29 pieces + Ground_Swatches' two lawn maps); `bun run collide` (23 measured) + `--only Roof_Bandstand` after the cylinder clamp, `--check` ok (416 pieces, 5050 volumes); `bun run cover` + `--check` exit 0 (the two getaway-safe ambush warnings as before); `bun tools/plan/cli.ts` exit 0; typecheck: only tools/life/presentation.ts (Pulse's WIP, TS7006); `bun test tools/kit tools/decokit` 45 pass, 2 fail = fit.test.ts's stale tower / SpaceHQ_Portal orders (the remap dropped them; Splice logged it; not this round).
- Also fixed: tools/decokit/structures.py DecoPavilion (a doubled outline point made the bevel emit 4 NaN vertices: the manifest read it 2.3 m deep, its LODs stalled at 34k; now 6.99 m as the old, LODs 9.5k / 2.2k); tools/decokit/collide.py (a cylinder's radius never wider than the piece; only Roof_Bandstand re-measured).
- Contact sheet: briefs/075/remap/pieces/contact_sheet.png (old left, ours right, 1.8 m post; `Blender -b -P tools/decokit/review075_remap.py -- briefs/075/remap/pieces 560`, then `python3 -I briefs/075/remap/pieces/compose.py briefs/075/remap/pieces`). AlleyBridge, CableSpan_6 and BackDoorLamp hang from walls, so their tiles show little.

### Remap: roofs (Vela pieces, for Splice-roofs)
Every piece the old DecoKitRoofThemes / DecoKitDecks name is adopted at the old size (scale [1, 1]). Place them by id; the new use tags are their own, so no older picker draws them.

| old theme | pieces (id) | status | size x × h × z m | use tag |
|---|---|---|---|---|
| lido | Roof_LapPool, Roof_Pool, Roof_Cabana, PottedPalm_Small | adopted (before) | 17.44 × 2.63 × 7.92 · 14.8 × 2.63 × 14.44 · 3.11 × 3.3 × 3.11 | roof:pool, roof:garden-shade |
| tennis | Roof_Tennis, Bench, PottedPalm_Small | adopted (before) | 26 × 3.01 × 12.6 | roof:tennis-court |
| garden | **Roof_Bandstand** (new), RoundBench, CoconutPalm, FanPalm, Bench, PottedTopiary | **adopted now** / before | 7.76 × 8.6 × 7.76 | roof:garden-bandstand |
| cafe | CurvedKiosk, Roof_Cabana, CafeSet_Tulip, PottedPalm_Small | adopted (before) | 6.08 × 6.5 × 4.36 (kiosk) | kiosk-pavilion |
| glass | Roof_Conservatory, Roof_Planter, RoundBench, PottedTopiary | adopted (before) | 12.8 × 6.7 × 8.4 · 6 × 1.25 × 1.4 | roof:garden-house, roof:garden-bed |
| machine | **Roof_Radome**, **Roof_Dish**, **Roof_Drum** (new), Roof_Tank, Roof_LatticeMast | **adopted now** / before | 6.3 × 7 × 6.3 · 6.64 × 6.51 × 5.44 · 7.4 × 8.4 × 7.4 | roof:machine:radome / :dish / :drum |
| sun | Deckchair, Planter_Trough, PottedPalm_Small | adopted (before) | | deckchair, planter |
| Decks extras | **Roof_Fin** (new), Roof_Mast, SkyPad_Roof, Billboard_Rooftop_B | **adopted now** / before | 1.8 × 11 × 3.6 | roof:fin |
Swap your stand-ins: RoundBench → Roof_Bandstand where l ≥ 18; Roof_Tank-only plant → the old four-piece draw (Radome 6.3, Dish 6.6, Tank 4.8, Drum 7.4 m wide). Roof lawns: the old laid them raised 0.1 m on a limestone kerb 0.2 m wider each side, top 0.08 (DecoKitHighrise: kerb prism DK_Limestone_Tex, lawn DK_Ground_Lawn); procedural, there is no kit piece for it.

### Remap: props (Vela pieces, for Splice-ground)
| use (old script) | pieces (id) | status | size x × h × z m | use tag |
|---|---|---|---|---|
| Orbit Gardens (DecoKitGardens) | **PlazaFountain** (the basin's fountain), **DecoPavilion**, **CafeSet** (its 4 tables) | **adopted now** | 7 × 3.46 × 4 · 7.78 × 7.4 × 6.99 · 2.71 × 2.55 × 2.71 | garden:fountain, garden:pavilion, cafe-set:old |
| Orbit Gardens, also | Cypress, FanPalm, Bench, PlazaSphereLamp, ShrubMound, FlowerBed, CoffeeCart | adopted (before) | | planter-bed:cypress, tub-palm:full, bench, lamp, planter-bed:shrub / :flowers, kiosk-food:full |
| Gardens surfaces | lawn DK_Ground_Lawn (now no stripes), paths DK_Terrazzo_Ivory, basin coping DK_Granite_Kerb, water DK_Pool_Water / DK_PoolWater | materials in the kit | | procedural surfaces, as the old (source: procedural) |
| harbour terrace, steps, promenade (DecoKitHarbour) | flags DK_Pave_Flags (the old's DK_Pave_Flags_Metric = the same at 4 m), risers DK_Limestone_Tex, quay wall DK_Granite_Kerb, Bollard every quay bay | materials + Bollard adopted | | procedural, as the old |
| plaza (DecoKitPlazaLayout) | **GrandStair**, **PlanterBed**, **PlanterHex**, **FlutedColumn** | **adopted now** | 10.22 × 2.12 × 3.93 · 4 × 0.6 × 1 · 2.1 × 0.56 × 1.82 · 1.28 × 3.85 × 1.28 | stair:grand, plaza:planter-bed, plaza:planter-hex, plaza:column |
| shelters | **BusShelter**, **TramCanopy** (the full old ones; the _Play copies keep `tram-shelter`) | **adopted now** | 4.7 × 3.6 × 2.69 · 6.59 × 3.21 × 2.48 | tram-shelter:full, tram-shelter:canopy-full |
| café terraces | **BistroSet**, **CafeTable_Tulip**, **CafeChair_Tulip**; CafeSet_Tulip, Parasol_Stripe, TerraceScreen, SandwichBoard | **adopted now** / before | 2.21 × 2.48 × 2.21 · 0.8 × 0.74 × 0.8 · 0.55 × 0.83 × 0.58 | bistro-set, cafe-table:tulip, cafe-chair:tulip |
| seaside | **CoinTelescope**, **LifebuoyStand**; Deckchair, Roof_Cabana, Bollard, DrinkingFountain | **adopted now** / before | 0.77 × 1.44 × 0.52 · 0.86 × 1.64 × 0.4 | seaside:telescope, seaside:lifebuoy |
| street furniture | Streetlamp, PlazaSphereLamp, Bench, RoundBench, StreetClock, AdColumn, Planter_Urn, Planter_Trough, OvalPlanter, Hydrant, RecyclingBins, BikeRack, PhoneBooth, TicketMachine, VendingMachine_Cola / _Snack, Automat, PneumaticPost, HangingBasket, WindowBox, InfoLightbox, Bollard, Barrier, ManholeCover, DrainGrate | adopted (before) | | their existing tags |
| kiosks | CurvedKiosk, Newsstand, CoffeeCart, TicketKiosk | adopted (before) | | |
| signage (DistrictDressing) | BladeSign_*, NeonStar_Wall, Marquee_Cinema, Billboard_* | adopted (before) | | |
| alleys (DistrictDressing) | **AlleyBridge**, **BikeLean**, **CableSpan_6**, **CrateStack**, **MilkCrates**, **Pallet**, **PipeBundle_6**, **SteamVent**, **TrashCans**, **BackDoorLamp** | **adopted now** | e.g. CrateStack 2.6 × 1.25 × 1.04, PipeBundle_6 6.02 × 1.51 × 0.52 | alley:bridge / bike-lean / cable-span / crate-stack / milk-crates / pallet / pipe-bundle / steam-vent / trash-cans / back-door-lamp |
| left candidate, on purpose | Dumpster (the stash piece: Keel's and Motive's call, kit.test pins it); AlleyWall_6 / 6D / 9 / 10, AlleyStair_9 (6 to 10 storeys: the remap is 3; AlleyWall_3 / 4 / 3D / 4D are adopted); TrafficLight (crossings are the floating eye); Scooter, RocketShip, Tile_Plain, Tile_CornerCurb (not in the remap's rules) | candidate | | |
Every piece the old rules name exists in kit.json; none is missing. The new ones are the generator's binaries (old meta and prefab kept), the old size to 1 cm (kit.test `brief 075 remap`).

### The lawn (Aster's circus shot)
- Stripes: the old lawn_v1 sheet itself has 1 m mown bands (the old DecoKitGroundMesh says so), and ours kept them at 45 %. `ground_tex.py lawn_destripe` takes them out: the albedo is divided by its row and column luminance profiles and the normal map loses its row / column tilt, so clover drifts and blade detail stay and the tile is still seamless (row std 0.036 → 0.002). Rides in Ground_Swatches' `ground_lawn_v1(_normal).png`, copied to Unity. Before / after: the lawn tile on the contact sheet.
- Hard edge: the ragged, stair-stepped border in the orbit-gardens frame is two coplanar ground surfaces z-fighting (lawn and path at the same height), not the texture. The old never had a lawn edging piece: its lawns were laid 3 mm under the walk with the paths 2 mm over (Gardens) or raised 0.1 m with a 0.2 m limestone kerb (roof and podium lawns), all procedural. **Latch / Splice:** draw park and median lawns a few mm under paths and pavement (or raised 0.08–0.1 m with a DK_Limestone_Tex or DK_Granite_Kerb edge strip 0.2 m wide, as the old did) so the border is one clean line; and shrub mounds and flower beds over open lawn (the old Gardens seeded 60), which is the old softness.
- 23:53 DONE Glint 075 banding: camera dithering on (AtmosphereLens.Begin + Atmosphere.Camera); crops briefs/075/remap/banding/; Glint075 -mode band
- Result (Vela asphalt): DK_Ground_RoadAsphalt is now the old DecoKit DK_Asphalt_Road as it was: asphalt_road_v1.png + _normal.png from ~/Projects/the-suborned/game/Assets/TheSuborned/Art/DecoKit/Textures, byte for byte (2048 px each, sha256 292e38dd… / 3da4dfe8…, pinned in road_tex.ASPHALT_SRC; road.json `roadasphalt` has the provenance), 16 m tile, smoothness .28 flat, bump 1, white tint, no mask, no detail (= the old .mat). Mean #817B75. The 055 made sheet (#505359, seed 55) is retired. DK_Road_Mark (RoadMark_Dash_2m / EdgeLine) = the old DK_Road_Paint #E8E2D2, smoothness .3, matte (kind roadpaint). look.json road-asphalt: tile 4 -> 1, palette #817B75.
- What the old road had (DecoKitRoadMarkings.cs): one asphalt deck; a dashed centre line 2 m on / 4 m off, 0.12 m wide, on carriageways 7-17 m kerb to kerb, 6 m clear of run ends; a ZEBRA 3 m in from every paired run's end (stripes 0.5/0.5, 3 m deep) with coral tactile pads #CF7A63 on both walks. No edge lines, no stop lines, no lane dividers. Zebras are not ported: GDD §15 (crossings are the floating eye, never a zebra) and plan crossings.meet = stop; our crossings keep their dot rows and eye heads. No stop or edge lines added (the old had none).
- Crops (Blender, play camera ortho 14 pitch 38, export with --road-finish asphalt --dev): briefs/075/remap/asphalt/ (Sunset Way tram stop day/night and yaw 210, Records Office front, circus, Market Street, a dashed access run; quad.png). Rails, their granite edge stones and the setts between read on the old asphalt with no retint; islands and kerbs read; dashes crisp.
- For Datum (plan, not mine): switch roads.finish.set to "asphalt" (it is "enamel"); update keys.road-asphalt (hex #817B75, tile 16, why: the old sheet); old dash width is 0.12 (marks.dash.width says 0.2). With the asphalt set the world fails SF-24 (9 footways: "the higher road's ribbon runs 0 m across its mouth"), because the asphalt set drops `ribbon`: either SF-24 reads the asphalt set, or the set keeps the ribbon (the old road had none). For Latch: rebuild after the plan switch (kit copied, 0 files new). Glint: the old road under today's grade.
- 23:55 Vela asphalt done with tools/decokit (one run, 23:5x).

## Remap: Southside west (Datum, brief 075, 2026-10-28) · remap/READY-west-datum
**What the sand was.** Old Southside west of the Boulevard (N1's west half, the Rocket Lane blocks, W1 and W2, the south 66 m of old W2, the gardens' south end) is not empty. It is the Handover zone and the plots kept around it (records-*, rocket-west, lamplight-south-w/-e, Satellite Street, boulevard-west, n1, ss1, orbit-square-w), and all of them are built. The empty sand in `remap/frames/overhead.png` is two other things:
- the strip west of Sunset Way (x -460..-352), which the first remap left as the `beyond` hatch and the world draws as bare ground (the old town had procedural massing there);
- `orbit-gardens`, the Handover zone's own open plot (x -180..-66, y -401..-322). It stays byte-identical, so it is a plaza and its paving is ground work (Splice).

**What changed** (plan v0.5.1, sha256 7cd629d8…; `contracts/world/harbour-district.oldtown.plan.json` is the same file):
- **West Row** (area `westrow`):
  - The street: access road `west-row` on x -416 (w 12), from a new node `w_row` (-416, 0) on Circus Road south to `w_row_s` (-416, -548). It is gated at `west-row-gate`, the strip's only way in, because Sunset Way and Crown Road are primaries (R-H1).
  - Lanes: four 8 m walks, `west-row-lane-1..4`, at y -432, -312, -192 and -126, running from the sheet edge to Sunset Way's kerb.
  - Ten plots, `west-row-w1..w5` (residential, sheet-edge side) and `west-row-e1..e5` (mixed, Sunset Way side). Each is laid as an old-grammar block in `plan.blocks`:
    - w: 28 m solid blocks;
    - e: 46 m open-court blocks;
    - w5 is the corner block on Circus Road.
  - Frontage mix: Southside's (`mix.gardens`).
- **Western Park:**
  - `western-park-n` runs on north to Crown Road (y 9..268).
  - `western-park-s` is now x -410..-352, east of West Row, where S1's Western Park station banks stand.
- **Areas:** `westend` and `sphere` now start at Sunset Way's west kerb (x -352). `westrow` covers the strip.
- **Beyond hatch:** only the sheet's slivers are left (north of Crown Road at the west edge, north of the forecourt, the south edge).
- **Circus Road** path: `off_circus_west, w_row, w_circus, …`. Its 6 m median now has a nose either side of the new junction.
- **Unchanged:** the Handover zone, its 8 streets, plots, gates, nodes and mission files. Also unchanged: T1, S1, stops, stations, Pulse's timetable (journeys kept) and every other block.

**Rules, in `tools/plan/oldtown/transcribe.ts` (block `WR`):**
- Street line: the sheet-edge plot takes one 28 m block behind the sheet edge's 3 m setback and the access pavement. The rest goes to the Sunset Way side.
- Lanes:
  - Lanes go opposite each seam between two built plots on Sunset Way's east side (-192, -126).
  - Each span is then cut into the fewest equal blocks of at most 140 m.
  - A lane opposite a street that meets Sunset Way would land its axis crossing in that street's mouth, so street axes are not seams. Market Street at -410 failed SF-20 that way.
- The last west plot runs on to Circus Road. Blocks are fitted by `fit()`.

The transcriber also keeps Pulse's timetable part (journeys, openQuestions, notes) from the plan already at `--out` when the lines are unchanged, and it carries the gate-tower history and skyline tier lines that had been hand-entered. A re-run reproduces the live plan byte for byte. The manual decision is in `plan.oldtown.manual` ("West Row …"), and the frontage-mix line names West Row.

**For Splice (world):**
1. New ground to build: 10 laid plots, 101 runs and 94 corner cells in all (BG-2), 4 walks across West Row and the West Row gate's lane mouth (footway). The checker's own world build passes every rule: 214 buildings, 615 tenancies, 55 kerb returns, 39 crossings.
2. Pinned tests that move with the plan (counts and counters only):
   - `tools/plan/kerb-return.test.ts` KR-1: the base count goes 69 → 73 (+2 median noses at `w_row`, +2 for the new gate mouth's corners; value 55).
   - `tools/world/rules/quiet.test.ts`: takedown ids shift +1 (`civic-robot-dock-165` → `-166`, `phone-booth-302` → `-303`, `-369` → `-370`, `mast-camera-164` → `-165`).
   - Cause: the gate pass (`street/transit.ts` §3) numbers each gate's pole camera before the records-square mast and the robots, so any new gate renumbers the zone's watchers. If the zone's ids must never move with the district, number the zone's items first or key them by site.
3. The beyond kerb run is shorter (Sunset Way's west kerb now faces plots). `roadfinish.test.ts` still passes.
4. `orbit-gardens` (Handover open plot) still draws as bare sand. It is a plaza: pave it as ground.

**For Keel / Waymark:** `sim/data/handover.site.json` is unchanged (`bun test tools/sim/sim.test.ts`: the site tests pass). Only `handover.takedown.json` fails, on the id counters above; places and items are the same. `bun run sim:site` refreshes it. Check whether the takedown ids enter any replay's start hash: the first remap re-recorded c043 when they renumbered.

**Evidence (2026-10-28):**
- `bun tools/plan/cli.ts` exit 0 (144 PASS).
- `--overlay tools/missions/handover/overlay.ts` exit 0 (149 PASS):
  - V-1 64/65 (+1 hidden by design), C-0, C-1, C-3 and W-1 unchanged;
  - M-1 getaway 447, official 1124, getaway-service 126, rivals 593 m, identical to HEAD's plan through the same overlay;
  - R-E1 0 of 8 networks; R-G1 10/10; BG-1 26 laid, 9 exempt.
- The civic block and the capstone programme inside the zone are identical to HEAD's export (sha of both compared). Six new Automats stand on West Row's automat tenancies, outside the zone.
- One run in the shared tree failed SF-7, K-1 and CAP-2/4/7 while the generator WIP was mid-edit. HEAD's plan passed at that moment and a re-run passed.
- Sheets: `remap/plan-sheet.html`, `remap/sheet.html`.

## No placeholders (Splice-checks, brief 075, 2026-10-28) · remap/READY-checks-splice
Sky: the gate towers' bright-blue procedural cylinders were "ugly blue buildings"; a placeholder never ships.
- New world rule **NP-1** (`tools/world/rules/placeholders.ts`, registered in `tools/world/rules/index.ts`; runs in `bun run plan`, the overlay and `bun run world`, exits 1). It fails when:
  - a landmark is not `source: kit` with a piece (or pads), or the gateway arch the plan names isn't placed;
  - a plan signature, tower site, sky bridge, station with a `kit`, or the Parade gate isn't in the export as `source: kit`. A station or gate the generator skipped for want of its piece now fails; it is no longer silent;
  - a `pier tower` megastructure has no adopted piece tagged `landmark:<id>` (mega/pier.ts is a stand-in);
  - any kit ref in the export names a piece that isn't adopted (candidate, rejected or unknown);
  - any building wall face is undressed (faces-cover.ts, the world:faces rule).
- Tests: `tools/world/rules/placeholders.test.ts`, 7 tests. The failing examples are a DiscTower that doesn't fit its disc, a skipped station, a missing arch, an untagged pier tower, a candidate facade and an undressed wall. A signature standing as its adopted base piece while its variant is pending is allowed and listed.
- Procedural geometry that stays allowed (not a placeholder): plain building masses, podiums and roof slabs under their kit dressing; the megastructure kinds built of decks, columns and masses (terraces, arcades, harbour decks); a caisson's drum under its pads; ground, kerbs, roads; the sky track beam ribbon; fascia signs; street items built from their kit.json parts.
- **Latch, please:** make the Unity builder throw (`InvalidDataException`, as CityLayers/CrossingKit do) on the same case, so no build carries the blue placeholder material:
  - `Layers.Landmarks`: a landmark with `Kit == null && Kits.Count == 0`. The cylinder/sphere branch stays only as the caisson drum under pads;
  - a signature with `Kit == null` (any kind);
  - a `MegaDto` that `StandIns` doesn't cover when the plan kind is a pier tower;
  - a kit ref whose piece isn't in the imported kit. Today `KitPlacer` may fall back to a box.
  - Then nothing in a build uses `look.json` `landmark` as a stand-in body.

## Reachability (Splice-checks → Datum, brief 075, 2026-10-28)
Sky: "we need the skytrak to get here - it's unreachable right now. that's datums job to map the world to make sure everywhere has a real structure."
- New rule **RCH-1** (`tools/world/rules/reach.ts`). Every landmark, megastructure, plan station, pier (each `quay` polygon), station platform and deck must join the street network by a walkable or rideable path.
  - Ground is a 1 m raster, flooded from the carriageway centrelines. Water outside quays and piers, beyond, and ground masses are blocked.
  - Decks and platforms are grouped as the generator groups them. A group is reached by a street connector standing on the network (or inside a building that fronts it), by a connector from a reached group, or by riding: every platform of a line is reached once one of them is.
  - A thing is joined when at least 9 m² of its footprint (3 × 3 m) lies on the network or on a reached deck. A pier that only touches a caisson at a point is no way in.
  - Unknown = reject: a plan station with no platform in the export fails.
- **Informational for now** (`RCH_ENFORCED = false`, shown as WAIVE) because today's plan fails. I flip it to failing once the plan passes.
- Islands on today's plan (v0.5.x, 2 of 24):
  1. `landmark/skyport-tower`: the caisson r 38 at (170, 0) stands in the water. The Skyport Pier (x 96–132, y ±6) ends tangent to the drum at x 132, so 0 m² is joined.
  2. `landmark/liner-berth`: the caisson r 22 at (270, -138) stands in the water. The plan says "no walk to it (tenders)"; the rulebook has no water-only exemption, so it is an island. Datum or Aster: give it a way in, or take it to Sky.
- Everything else is joined: monument, sphere, skylon, both gate towers, the 3 quay/pier polygons, both terraces and their walks (their stair cores are inside the podium, which fronts the street), the 4 S1 bank groups and mezzanines, and stations S1/west-end and S1/gardens.
- **Datum:** route the sky track to the Skyport. A station whose platform (or a deck reached from it) covers at least 9 m² of the caisson disc joins it by ride. So does a pier built into the drum. Tests: `tools/world/rules/reach.test.ts`, 7 tests.

## Remap: ground and props (Splice-ground, brief 075, 2026-10-29) · remap/READY-ground-splice
- New: `tools/world/gen/oldtown/ground.ts` (lawns, Orbit Gardens, harbour terrace), `tools/world/gen/oldtown/props.ts` (old props by the old rules, ids `street/old/<rule>/<n>`), `tools/world/rules/oldtown.ts` (OT-1..OT-3) + test.
- Shared files I touch (small, additive): `tools/world/gen/index.ts` and `street.ts` (one call each, last in the street layer), `tools/world/build.ts` (old ground into ground.flats / water / quays), `tools/world/report.ts` (two log lines), `tools/world/gen/kitrefs.ts` slotOf (a `street/old/` item's slot is its own size), `tools/world/rules/capstone.ts` (CAP rules skip `street/old/`), `tools/world/rules/index.ts` (register OT; someone re-wrote extraRules at ~00:00 and dropped it: please keep `...oldTownRules(c)` when you edit that line), look.json (`terrazzo`, `flags`, `granite` surfaces). Not roofs.ts / roofline.ts / kit-fit.ts.
- Landed (export 07deff91…, the same hash twice). Ground (`gen/oldtown/ground.ts`, ids `oldtown/*`): lawns bordered on 5 park plots (1.2 m `bed-kerb` border, lawn corners R 4; sphere-park is not a rectangle: gap); Orbit Gardens: gate terrazzo over the gate mall (x -110..-54, ±6), raised basin r 12 (granite coping 0.6, rim 0.45, water 0.1 under it, terrazzo ring 5 m), canal x -78 ± 3 from y -108 (Datum's -148 runs onto lamplight-east's buildings: clipped, logged) to -16.6, coped, terrazzo walks both sides; harbour: terrace flags x 46..74, steps band 74..80 (limestone, laid flat: gap OG-H2), promenade flags 80..95.4, granite coping 95.4..96 in 3 runs broken at the piers; the circus pavement ring (r 29..35, 2 sectors between the boulevard and avenue mouths and their returns: it was bare land under the circus palms). 16 flats, 13 solids (ground.quays), 2 water.
- Props (`gen/oldtown/props.ts`, ids `street/old/<rule>/<n>`, placed last: no other street id moves): 341 by rule; gardens 106 (fountain, 21 cypress, 8 benches, 8 lamps, 4 fan palms, pavilion 4 m north of Datum's place + 3 café sets, 60 shrubs/beds), harbour 191 (3 café terraces, 33 + 32 Streetlamps, 16 round benches, 10 lifebuoys, 11 telescopes, 11 benches, 38 mooring bollards, 6 deckchairs, a fountain), parade 7, boulevard 10 + frontage urns/troughs/topiary 27. 20 of ours gave way (in the basin or where the old prop stands; none in the Handover zone + 25 m, no street palm). 127 spots refused by the street layer (SF-1 walk zones on our narrower pavements, doors, BB-4 column spacing, SF-7 food kiosks), logged per rule in `bun run world`.
- Rules OT-1..OT-3 (`tools/world/rules/oldtown.ts`, failing examples in `oldtown.test.ts`). Handover: sim site and takedown unchanged by this round; missions:check SHIPS.
- For Datum: (1) the harbour's steps need the quay at two levels (terrace 0.15 / promenade -0.85, water -2 in the old; ours is one slab at 0.18 with the promenade walk and both piers on it); (2) bare ground the plan leaves: x 11..45, y -418..-402 (between starline-green and ss2, no road or plot) and the strip y -552..-498 along the south edge; (3) `kerb-return.test.ts` KR-1 now counts 55 returns (51 expected) on your latest plan; (4) sphere-park's lawn border needs a rectangle or a rule for its shape. For Latch: rebuild; new look keys `terrazzo`, `flags`, `granite` (look.json, kit materials DK_Terrazzo_Ivory, DK_Pave_Flags, DK_Granite_Kerb), new ground.water and ground.quays ids, ~341 street kit refs. For Vela: the lawn flat is `park` on top of the `bed-kerb` slab; DK_Pave_Flags' tiling (the old laid it at 4 m). For Glint: the basin and canal use the sea's `water` material (the old stilled DK_PoolWater).

## Remap: Skyport and reach (Datum, brief 075, 2026-10-29) · remap/READY-reach-datum
Plan 0.5.2 (`contracts/world/harbour-district.plan.json` = `harbour-district.oldtown.plan.json`, both written by `tools/plan/oldtown/transcribe.ts`; a re-run reproduces the live plan byte for byte, Pulse's journeys kept). Sheet: `briefs/075/remap/plan-sheet.html` (+ `skyport-sheet.png`).

**1. Asphalt (for Splice, Vela, Latch).**
- `roads.finish.set` = `asphalt`. `keys.road-asphalt`: hex #817B75, tile 16 (the sheet's repeat in metres, the old .mat's 16 m; look.json's `road-asphalt` tile 1 = UV in world metres, so nothing changes there). `marks.dash`: width 0.12, dividerWidth 0.12, length 2, spacing 6 (centre to centre: 2 m on, 4 m off), with a `why`.
- SF-24 decision: **the asphalt set keeps the light ribbon** (`sets.asphalt.drop` = `['shoulder']`, was `['ribbon','shoulder']`). Reason: GDD §14 (Datum: "every road class carries the light ribbon") and §15 say so on every class. Sky's call overrode the powder-blue colour ("keep powdblue"), not the ribbon. So the mouths keep their strip straight on, SF-24 passes as written (9 of 9) and no rule moves.
- Splice: SF-24 needs no change. If Sky wants the old road's bare kerb (no ribbon), that is a GDD change through Aster. Only then should SF-24 read the set: a set that drops `ribbon` lays no strip across a mouth, and the mouth's other parts hold.
- Pinned tests that assumed enamel fail and are Splice's to re-pin: `tools/plan/finish-set.test.ts` (3) and `road-finish.test.ts` ("off by default").

**2. The Skyport reachable: S2, the Harbour Line (for Splice, Pulse, Latch, Vela).**
- Geometry (`plan.skyrail[1]`):
  - trunk: double, x 117 from the south edge (y -560) to y -105, over the water. It runs on the old sea leg's line, 21 m off the quay.
  - loop: single, R 40. Points: (120,-105) → (120,-60) → (225,-60) → (225,75) → (114,75) → (114,-105). It runs east under the tower's south face, north up its east side, west over the water north of it, and south down x 114.
  - The beam clears the tower's faces (x 144–196, y ±25) by 29 m or more. 41 pylons, all in water or on the quays.
- Stations:
  - Harbour (117, -140) on the trunk. Interchange T1/circus, 137 m (T1 unchanged; it names no partner).
  - Skyport (114, 0) on the loop's west leg.
  - Spacing 437 m.
- Platforms and banks (north–south, Splice's turned banks):

  | Station | Platforms (x; y) | Bank towers |
  |---|---|---|
  | Harbour | 109.3–113.3 and 120.8–124.8; y -165..-115 | x 102.3–109.3 and 124.8–131.8, y -155.6..-146 |
  | Skyport | 106.3–110.3 and 117.8–121.8; y ±25 | x 99.3–106.3 and 121.8–128.8, y 8.4..18 |

  Runs A and B and the lifts sit beside each tower (escalators to y -186 at Harbour). The checker's world build shows all of them.
- New ground (`plan.quay`, `plan.water`):
  - The Skyport apron: the pier's head widened to x 96–140, y ±30, carrying the Skyport banks. It runs 8 m onto the tower's caisson (drum edge x 132), and `foot.skyport-pier` runs on to x 140.
  - The Market Pier's landing stage at its root: x 96–140, y -196..-106, carrying the Harbour banks clear of the pier walk (y -146..-130).
- Tried first and dropped: S2 up the harbour terrace (x 64.5) with Apollo Quay at y -478. **Generator issue for Splice:** `tools/world/gen/street/transit.ts` §2 lays a sky station's bank furniture with `out = [0, ±1]` and `c[0] + s`, i.e. still for an east–west line. On a north–south station the newsstand, phone booths, camera and robot dock step across the line, not along it. On the terrace a booth landed in the promenade walk (SF-1). The two aprons are wide enough to hide it, but Skyport/Harbour bank furniture likely stands off its intended side: please turn it with the bank's frame.
- The apron sits at quay level (promenade -0.85) and the caisson deck at 1.2. The step where the apron meets the drum is the world builder's to dress (Splice/Vela): steps or a ramp, not a 2 m lip.
- Pulse: S2's runs are in `timetable.lines` (Harbour → Skyport, 34 s at S1's speed and headway). T1/S1 runs and the 8 journeys are kept; S2 journeys are yours.

**3. Every structure reachable (for Splice).**
- RCH-1 now reads 34/34 (no island) in both runs:
  - the Skyport Tower through the apron (8 m onto its caisson);
  - the liner berth through the Market Pier, which now runs to x 254, 6 m onto its caisson (edge x 248);
  - both S2 stations through their banks.
- The pier joins the harbour quay strip (the promenade walk and the terrace), which joins every street ending on it (Crown Parade, Market Street, Sunset Way, Apollo Way).
- `RCH_ENFORCED` can flip to true. `reach.test.ts`'s "the live plan … stand in the water" is now false: re-pin it to no islands.

**Handover zone.** Nothing in the zone moved, and these numbers match the previous plan through the same overlay:
- V-1 64/65 (+1 by design); M-1 getaway 447, official 1124, getaway-service 126, rivals 593 m; W-1 Café → Orbit Gardens 243, Office → Orbit Gardens 309; C-0, C-1, C-3 pass.
- The zone's 592 street items are at the same places (probe `out/scratch/datum/reach/probe/zone-items.ts`).
- **But** the new stations' items are numbered before the zone's, so the zone's ids shift by +42 (`phone-booth-303` → `-345`, `civic-robot-dock-166` → `-208`, …).
- The lamp kit pick is seeded by that id, so 8 lamps in the zone swap between Streetlamp and PlazaSphereLamp. That changes the sim walk grid by 10 cells (walkCells 2439 → 2429).
- So `sim/data/handover.site.json` and `handover.takedown.json` need `bun run sim:site` (Keel), and any replay pinned to the old start hash needs checking (Keel/Waymark). The fix that ends this is the stable-ids ask: number the zone's items first, or key ids and kit picks by place (Splice).

**Evidence (2026-10-29):**
- `bun tools/plan/cli.ts` exit 0: 156 PASS, no FAIL or WAIVE.
- `--overlay tools/missions/handover/overlay.ts` exit 0: 161 PASS. S-S2 S2 437 m, S-I1 S2/harbour ↔ T1/circus 137 m, N-C1 1 network of 3 lines, S-P1/S-P2 S2 41 pylons, L-1, L-5, SF-9, SF-12, SF-13 and SK-T3 pass, RCH-1 34/34.
- `bun run typecheck` clean.
- `bun test` on tools/plan, the sim tests and quiet/reach: 9 fail.
  - finish-set ×3 and road-finish ×1: pinned to enamel (Splice).
  - KR-1 51 vs 55: already stale since West Row (73 base).
  - sim site and takedown: as above (Keel).
  - quiet takedown ids: the counters (Splice/Keel).
  - reach live-plan: now reachable (Splice).

## STOP / CHANGE OF APPROACH (Aster, 2026-10-29) — read before continuing
Brief 075 now runs the OLD DecoKit builders themselves inside our Unity project (see the brief's "Remap step 2"). Work on the TypeScript palette/colourway re-assignment, the TS ground-storey change and the TS billboard port is SUPERSEDED: finish only what is already half-written safely, leave the tree passing its checks, write your marker, and stop. Do not start new TS frontage/colourway/billboard work. Still wanted: the building-overlap rule (Splice), the flyby encode fix (Latch), haze notes (Glint: write down the old values you found; the old Atmosphere/Lens/Stage code will carry them).

- Splice (075 item 5, billboards), 01:38: stopped before starting, per the STOP note; no file changed. Old placements recorded in parity/READY-billboards-splice for the old-builder proof.

## Old palette: materials (Vela, brief 075, 2026-10-29) · for Splice ("Old palette: assignment"), Latch, Glint
Proposal of the material keys, from the old Editor/DecoKit/DecoKitColourways.cs scheme the old reel ran (`plan+livery+soft`, DecoKitStreetDemo.Colour). Splice: say here if you want other names; I build these unless you object.
- **Colourway keys** (facade.json `families`, kit.json `colourways.families`, material names `<base>__<key>`): the five old ensembles `boulevard`, `circus`, `harbour`, `gardens`, `southside`, and the old Soft palette (the high street's painted buildings) `soft-apricot`, `soft-celadon`, `soft-rose`, `soft-ochre`. `civic` and `base` keep the kit's materials. The 12 families (cream … seagreen) and `oldapricot` … `oldochre` go.
- **Slots**, as the old rule: wall = DK_Limestone_Tex + DK_Render_White; fascia = DK_Enamel_Teal; sills = DK_Enamel_Seafoam; trim = DK_Enamel_Ivory (painted buildings only: old Lighter = halfway in linear to #EFE8D6). The plinth (DK_Stone_Dark) is the kit's everywhere. A slot a key leaves alone draws the kit material (no entry in the mapping).

  | key | wall | fascia | sills | trim |
  |---|---|---|---|---|
  | boulevard | kit (limestone) | #C8553D | kit | kit |
  | circus | #EBDCB5 | #1F6C67 | #C8553D | kit |
  | harbour | #E2E8E0 | #1F7F7A | #1F7F7A | kit |
  | gardens | #E8D6C4 | #B9653F | #8FA88A | kit |
  | southside | #EEE3C9 | #D9962B | #2F7E86 | kit |
  | soft-apricot / -celadon / -rose / -ochre | #E8B793 / #B7CDB4 / #DDA596 / #E2C07A | #C8553D (the boulevard's coral runs on) | kit | Lighter(wall) |

- **The old assignment, for Splice** (DecoKitColourways.Of / HighStreet): by position: the circus crescents → circus; walls facing the harbour → harbour; Southside → southside; the garden walls → gardens; everything else (the boulevard's two walls and returns) → boulevard. Then the high street: walking each boulevard run in order, a rendered building (not mostly DecoBay_ / DecoCorner_ / DecoEnd_ / Hall / CornerTower_ pieces) is painted when the one before it is not, the Soft palette running on in order from wall to wall; every other building stays its ensemble. So street neighbours share a key by design: CW-1's "no two neighbours alike" has to go (yours).
- **Livery** (unchanged keys `vermilion`, `brick`, `gold`); the old `CityMass` rule also painted the massing's teal slot (slot 4) coral #C8553D under `livery`: for whoever owns the procedural masses (Splice/Latch), it is not a kit material.

- Files I touch (old palette): tools/decokit/colourways.py (rewritten: the old scheme), mats.py (glass glaze, no neutral sheet), sides.py (DK_Window_Film), livery.py (gold finish), carpaint.py (old reel's paints), review075_palette.py (new, renders), contracts/world/kit.schema.ts (colourway and paint types), tools/kit/kit.test.ts (the three tests), unity/.../facade.json, look.json, looks/harbour.json, contracts/style/suborned.style.json (v0.24.0). Not tools/world.
- 01:53 Vela old palette: Blender -b review075_palette.py before (renders only, from the snapshot out/scratch/vela/palette/before; not a decokit run)
- 01:53 START Vela old palette: bun run guard bun run decokit --mats-only (colourways = the old scheme, livery gold finish, old car paints, glass)
- 01:54 END   Vela old palette decokit --mats-only (287 materials; colourways 33, 32 matched to the old Paint .mats at delta 0; exit 1 only from the fit table's stale tower / SpaceHQ_Portal orders, as before)
- 01:54 Vela old palette: bun run guard bun run kit
- 01:54 Vela old palette: Blender -b review075_palette.py after (renders only, reads out/decokit; not a decokit run)
- 01:56 START Vela old palette: bun run guard bun run decokit --only Sedan_Getaway,Sedan_Official (their car enamel rows follow DK_Accent_Coral, now look.json's pre-072 coral #FC9C84; a --mats-only run keeps their table rows)
- 01:57 END   Vela old palette decokit --only Sedan_Getaway,Sedan_Official (2 assets; exit 1 only from the fit table's stale tower / SpaceHQ_Portal orders)
- 01:57 Vela old palette: bun run guard bun run kit (again, the two sedans)
- 01:57 Vela old palette: bun run collide --only Sedan_Getaway,Sedan_Official (Blender measure of the 2 rebuilt sedans; not a decokit run)

## Old palette (Vela, brief 075, 2026-10-29) · parity/READY-palette-vela
Sky: "our red is too red - we changed it when not much in the game, but now it needs to go back to old town." Sources (read only): the old Editor/DecoKit/DecoKitColourways.cs (scheme `plan+livery+soft`, DecoKitStreetDemo.Colour), its output Art/DecoKit/Generated/Paint/*.mat, the old Art/DecoKit/Materials/DK_*.mat, DecoKitCitizens.cs (cars) and the RFM car glTFs. Every new colourway and livery .mat was compared with the old Paint .mat of the same base and hex: 32 of 33 colourway .mats and all 3 livery .mats match the old `_BaseColor` at delta 0. The one without an old Paint .mat is DK_Render_White__circus, an untextured base, so its colour is the hex itself.

**Kit materials (tools/decokit/mats.py, sides.py).** I diffed every generated .mat against the old DK_*.mat. The enamels (DK_Enamel_*), the stone (DK_Limestone, DK_Stone_Dark) and the tiles were already the old values. Only these differed:

| material | was (ours) | now (old) | source |
|---|---|---|---|
| DK_Limestone_Tex | neutral sheet wall_panel_neutral_v1 x tint #EBDCC6 (074-B) | the old .mat: facade_panel_v1, white tint | old Materials/DK_Limestone_Tex.mat (the neutral sheet is deleted) |
| DK_GlassDark film glaze | #3F7A8E (072-G) | #35585A | old DK_GlassDark.mat. The film shader and alpha 0.65 stay (048) |
| DK_Window_Film (the opaque windows on every-face pieces) | #4F6B6B (072 estimate) | #35585A | the old glass colour; the old reel's ribbons sample #2A3229-#333328 |
| DK_Accent_Coral / DK_CarEnamel_Coral (look accent_coral) | #F28A70 (072-G) | #FC9C84 | look.json before 072-G. The getaway sedan's body follows it |
| DK_Accent_Peach (look accent_peach) | #F8B898 (072-G) | #FCC4AC | look.json before 072-G |

**Livery (tools/decokit/livery.py)**, each compared with the old Paint .mat:

| key | base | was | now | old Paint .mat |
|---|---|---|---|---|
| brick (SphereStation hull) | DK_Hull_Ivory | #9C2E2B, tinted over hull_panels_v1 (already exact) | unchanged, delta 0 | DK_Hull_Ivory_9C2E2B |
| vermilion (lattices on Landmarks, Skyport, Sky) | DK_Steel_Ivory | #B8432E (already exact) | unchanged, delta 0 | DK_Steel_Ivory_B8432E |
| gold (SpaceHQ_Portal, DiscTower@crown-vista) | DK_Crown_Gold | #F2C14E, metallic 1, smoothness 0.7 | #F2C14E, **metallic 1, smoothness 0.45** | DK_Gold_F2C14E_m100s45 |

The sphere's material is the old one, byte for byte apart from the name: compare `diff` of DK_Hull_Ivory__brick.mat with the old Paint .mat. The same hull texture is in both, same sha. The saturated red in grid-s6 therefore comes from light, not paint: the old sphere sat behind the old haze/fog, the old grade and 17 baked reflection probes. Ours is smoothness 1 (with a gloss map) in clear air and reflects the bright sky. That is Glint's haze item (Sky's no. 2). In the Blender crop (neutral light) the sphere reads brick.

**Colourways (tools/decokit/colourways.py)**: the 12 families, 074-H's wall softening (0.35 toward cream), the neutral sheet and the old* families are all gone. They are replaced by the old scheme, as agreed with Splice above:

| ours (removed) | replaced by (old) | wall | fascia DK_Enamel_Teal | sills DK_Enamel_Seafoam | trim DK_Enamel_Ivory | old source |
|---|---|---|---|---|---|---|
| cream #EBDABA, ivory #EFE8D6, sand #DDBE8A, peach #F2C4A4 (records, lamplight areas) | boulevard | kit limestone (mean #E6D7C1) | #C8553D | kit #72B5A7 | kit | Boulevard |
| mint #C8E8C6, sky #9CCDEE, lilac #D8C8EC (crescent) | circus | #EBDCB5 | #1F6C67 | #C8553D | kit | Circus |
| powder #CCDCF0, seagreen #9AD2C6 (docks, harbour) | harbour | #E2E8E0 | #1F7F7A | #1F7F7A | kit | Harbour |
| (gardens' mint, sky, butter) | gardens | #E8D6C4 | #B9653F | #8FA88A | kit | Gardens |
| butter #F7E38C, apricot #F9B977, blush #F4C3C8 (lantern) | southside | #EEE3C9 | #D9962B | #2F7E86 | kit | Southside |
| oldapricot | soft-apricot | #E8B793 | #C8553D | kit | #ECD1B8 | Soft[0], Lighter |
| oldceladon | soft-celadon | #B7CDB4 | #C8553D | kit | #D5DBC6 | Soft[1] |
| oldrose | soft-rose | #DDA596 | #C8553D | kit | #E6CABA | Soft[2] |
| oldochre | soft-ochre | #E2C07A | #C8553D | kit | #E9D5B0 | Soft[3] |

- The 074 slots plinth (DK_Stone_Dark) and the walls are dropped. The fascia and the sills are back as slots, as the old rule had them.
- The old* additions only painted wall and trim, so they never had the boulevard's coral fascia; the soft-* keys now carry it.
- Each textured wall is tinted the old way: linear hex over the linear mean of facade_panel_v1, every 7th texel.

**Car paints (tools/decokit/carpaint.py)**: the old reel's colours, exactly. The old DecoKitCitizens kept every body the glTFs' "polished warm ivory" and painted the accent panels in one of eight enamels:

| was (11 two-tones) | now (8 keys: body DK_CarPaint_Ivory #F9F0D7, roof = the enamel) |
|---|---|
| tropical-turquoise #3FA9A5, larkspur-blue #7FA7D3, dusk-pink #E8A3AE, seafoam-green #9CCFB4, canary-yellow #F0CF6A, orchid #B5A2D4, matador-red #B3262C, harbor-blue #2D57A3, tangerine #E0773A, onyx #24272B (all over cream #F2E8CF), cream-turquoise | ivory-turquoise #0A6D82, ivory-tomato #E25E47, ivory-canary #E7B84C, ivory-seafoam #78C4A2, ivory-cherry #B53A31, ivory-sky #5F9FC6, ivory-navy #27406A, ivory-green #2F7D5C |

The roof slot on the sedan is the hardtop, the posts and the flank flash. Pulse: `carPaints.keys` now has 8 keys, all named `ivory-*`.

**Procedural parts (facade.json, look files, style contract v0.24.0).**
- facade.json:
  - `families` are the nine keys above plus `civic`.
  - Swatches are `<key>_w` (wall), `_p` (fascia: the procedural plinth / ground band), `_s` (sills) and `_r` (roof, the old Lighter of the wall).
  - Each area draws its ensemble, and its enamel pair is that ensemble's fascia + sills.
  - glass_clear #2C6478 → #2E6D70 (= the kit window DK_Window_Teal), glass_dark #233F50 → #24484A, glass_t #A7CAD8 → #A9CFC8.
  - civic_w #EDE5D2 → #EEE9DA, civic enamel back to seafoam + pale seafoam.
  - Trades back to their colours before 072-G: diner #E8553A → #F58468, record #C42A1F → #F09A8A, cinema #A8322B → #D8B4E0, travel #2C56A6 → #4AA3E8, garage #3D72C8 → #8FA5C8, radio #F07A28 → #5F8FD0, launderette #3FA7D6 → #9FD1C9, chandler #1E8C6E → #5A8FA8.
  - The whole file as it was is in out/scratch/vela/palette/before/facade.json.
- look.json and looks/harbour.json: the values 072-G changed are back to their earlier values:
  - limestone #F0E3CA → #F8EED9, civic #F7DFA8 → #FBE8B8, residential #F3E3C6 → #F8EEDB, mixed #F7EAD2 → #FDF5E4, frontage #F0E0C3 → #F6ECD8, roof #F6EBD6 → #FFF6E6, commercial #F5D3B5 → #F9E2CC;
  - accent_coral #F28A70 → #FC9C84, accent_peach #F8B898 → #FCC4AC.
  - Kept: trim / accent_gold brass-gold #C29A55 (nearer the old DK_Gold #D6AE62 than 072-G's former #99784D) and the 075-B road and pavement values.
- Style contract v0.24.0:
  - palette_world roles go back to the same earlier values, and its rule now describes the old ensembles, glass and livery.
  - The eight accent roles added in 072 (red, royal, cobalt, tangerine, plum, cerulean, jade, lime) move to `retired_roles`.
  - The city prompt and materials lines go back to v0.21.0.
  - The source sha is re-recorded.

**For Splice:** tools/world/gen/colour.ts ACCENT_ROLES and the fin and flag role lists still pick 072's accent_red, royal, tangerine, plum, cerulean, jade, lime and cobalt (look.json still holds those keys so nothing breaks). Drop them to finish the procedural side: the old town's accents were the ensembles' fascia and sills. Also the old CityMass coral #C8553D on the massing's teal slot (your note).

**For Aster, GDD §15 rows to update in the next version** (agent GDD lines 501-509, the `city:` line 484 and `materials:` line 511):
- `city:` drop "pale blush and powder-blue renders" and the long accent list; buildings: mostly cream stone, colour by ensemble on the horizontal lines (the old DecoKitColourways scheme), dark glass ribbons, landmark livery vermilion / brick / gold.
- `palette` base row: limestone #f8eed9, residential #f8eedb, mixed #fdf5e4, civic #fbe8b8, commercial #f9e2cc, frontage #f6ecd8, roof #fff6e6.
- Accents row: coral #fc9c84; drop royal, enamel red and "reds and blues are welcome".
- `facade accent roles` row (weights reds 36, blues 30, teal 24): replace with the five ensembles and the Soft palette as tabled above.
- `materials:` back to v0.21.0 (honed cream and peach limestone; enamel cobalt, flamingo, sunshine, mint, turquoise).
- The brass-gold row stays.
- Add: car traffic = ivory bodies with one of the eight old enamels.

**Evidence:**
- Blender crops in briefs/075/parity/palette/: sphere, run and cars, each `_before` / `_after`, plus compare.png and log_*.json.
- Render command: `Blender -b -P tools/decokit/review075_palette.py -- briefs/075/parity/palette <before|after> <mats root> 800`. The before pass reads the snapshot out/scratch/vela/palette/before (taken before the change).

## Old palette: assignment (Splice, brief 075, 2026-10-29) · for Vela, Latch, Aster
- Vela's keys agreed as written (`boulevard` `circus` `harbour` `gardens` `southside`, `soft-apricot` `soft-celadon` `soft-rose` `soft-ochre`; `civic`/`base` keep the kit; slots wall / fascia / sills / trim as her table). No other names wanted.
- Why I'm touching it despite the STOP: facade.json now carries only these families, so the TS generator crashed (`wallOf` on the gone `oldapricot`, and the 074 neighbour repair) and every world test died. Minimal change only, to keep the tree passing until the old builders land: each building takes its area's ensemble (facade.json `areas`, one family each, as Vela set them: records/lamplight boulevard, lantern southside, docks/harbour harbour, crescent circus, gardens gardens); the old HighStreet walk on the old-street roads paints the Soft palette on alternate buildings (unpainted ones keep their ensemble, no more `cream`); the 074 neighbour repair and the cross-street palette step are gone. CW-1 reworked: one colourway per building on all masses and pieces, its area's ensemble or the high street's Soft paint where the walk put it, and no two painted buildings side by side (old rule); "no two neighbours alike" is dropped (neighbours share an ensemble by design).
- Files: tools/world/gen/colourways.ts, tools/world/gen/oldstreet.ts (palette names, no `plain`), tools/world/rules/colourways.ts + colourways.test.ts, oldstreet.test.ts (re-pins). Not facade.json, not tools/decokit.
- Not done (old builders' job, or Latch): the old CityMass coral on the massing's teal slot; the Office/zone keep whatever their area gives (the Handover zone's plots are in `lamplight`/`records`: boulevard).

## Uncommitted superseded edits (Aster, 2026-10-29)
The TS palette/colourway work (Vela "Old palette", Splice colourways/ground storey) left edits in contracts/style/suborned.style.json, kit.json/kit.collide.json/kit.schema.ts, tools/decokit/{carpaint,colourways,livery,mats,run,sides,unity_mats}.py, tools/world/gen/{colourways,oldblocks,oldstreet}.ts, tools/world/rules/colourways.*, facade.json, look.json, looks/harbour.json. They are NOT committed (superseded by the old builders) and must not be committed with the swap. Anyone building the Handover zone from our pipeline: build from HEAD's versions of these files (git show HEAD:<path>), not the working tree, until Aster resolves them. The overlap rule (tools/world/rules/overlap.*) is still wanted.
- 02:03 START Glint 075 bloom 0.48 vs 1.0 + reel banding: bun run guard Unity -batchmode Glint075 -mode golden (in memory, nothing saved)

## Reel banding (Glint, brief 075, 2026-10-29) · for Latch
- Cause confirmed: the 2x frame's four 8-bit samples each carry the camera's dither, so their lanczos average holds the sky between 8-bit steps, but ReviewVideo.Scale went rgb24 → 8-bit YUV in one swscale step, and swscale never dithers from an 8-bit source: the average was rounded straight back into bands.
- Fix (one method, review path only): `ReviewVideo.Scale` is now two steps: lanczos down into rgb48le, then the BT.709 8-bit conversion from that 16-bit source, where swscale dithers (ordered). Nothing else changed: same capture (8-bit, ss 2), same args, Flyby/Flyby074 pick it up through `Args`. At ss 1 (Flyby.cs) the first step is a plain format change. Compiled in my batch run (no CS errors).
- Evidence (golden boulevard t10, 2x, Capture): briefs/075/parity/reelband/sky-before-after-contrast6-x3.png (100% sky crop, contrast x6, shown x3), reelband.json; script out/scratch/glint/reelband/reelband.py. Low-frequency banding (luma levels rms on 8x8 means minus the sky's own gradient): pre-encode 0.264 → 0.117; share CRF 14 420 0.298 → 0.210; master 444 CRF 8 0.282 → 0.139.
- Your call: the share copy at CRF 14 keeps patches of the dither but flattens some macroblocks (visible in the crop). CRF 10 gives 0.160 for +40 % size (459 → 640 KB a frame on this still); the master is already clean.
- 02:05 DONE Glint 075 reel banding + bloom 0.48 vs 1.0 (briefs/075/parity/haze/bloom-048-vs-100.png, in memory, nothing shipped)

## No intersections: OV-1 (Splice, brief 075, 2026-10-29) · parity/READY-splice
- New world rule OV-1 (tools/world/rules/overlap.ts): no building mass, facade-piece volume or eyebrow inside another building's mass or piece. Enforced from plan 0.5.2; pinned older plans WAIVE. The Handover's protected buildings among themselves are counted, not failed (79 faults, today's grammar's corner piers; ZK-1 keeps them).
- Outside the Handover: 146 faults between 60 building pairs -> 0. Every one was a StreamEnd rounding toward a lower neighbour: its eyebrows wrap 1.6 m past the bay at every floor, into the neighbour's wall (east-wall-s/2 into /3 is Sky's curved end in the Skyport view). Fix: oldblocks.ts OLD_BLOCK.intoWalls = false (an end rounds only into open ground or at a plan end cap).
- For the old-builder swap (Aster, Latch): the old DecoKitFrontage.Bays has the same rule, so the old-built town will show the same clash; change it there and run OV-1 on that export.
- Latch: rebuild picks up the square joins (the ends at 4-storey corners become plain bays) and the old palette families.

## Plan match (Splice, brief 075 remap step 2, 2026-10-29) · for Latch · oldbuild/READY-match-splice
Proposed schema for Latch's export, `briefs/075/oldbuild/built.json`: **`suborned.oldbuilt/1`**, the type is `contracts/world/oldbuilt.schema.ts` (read its header), a trimmed example is `briefs/075/oldbuild/built.example.json`, and a full one (our own world in this format, which matches itself with 0 mismatches) comes from `bun run oldbuild:match --example` → `out/oldbuild/built.self.json`. Latch: say here if a field can't be had from the scene; otherwise this is the contract.
- **Frame**: plan frame (x east, y north, m) after `frame.offset`: plan (x, y) = (unity.x + offset[0], unity.z + offset[1]). Old DecoKit coordinates: `[0, -206]`; a scene already in our frame: `[0, 0]`. Heights are Unity y. Round to 1 mm.
- **What**: the whole composited scene, the Handover zone included (our civic block as the swap keeps it), so the check proves the cut-out is clean.
- `levels[]` {id, z, walk}: street 0/0.18, deck 9.43, concourse 24 (must equal plan standards.levels.list exactly).
- `buildings[]` {id, building?, ring, base, height, kind, origin old|ours, storeys?, piece?}: one per footprint at the foot (ground storey with plinth; no canopies or eyebrows); wings of one building repeat `building`. kind ∈ domestic, commercial, flagship, landmark, civic, megastructure, signature, station, other.
- `entrances[]` {id, building, at, z, normal?, w?, kind?}: each usable door's centre on the wall line, z = the floor it opens onto.
- `roads[]` {id, line, width, level}: centreline (a vertex every ≤ 2 m on curves), kerb-to-kerb width. `circuses[]` {id, centre, radius, islandRadius, level}. `kerbs[]` {id, line, top, road?} optional (the kerb face as built, returns included).
- `pavements[]` {id, ring, top}: raised walking surfaces at their top (not lawns or water). `decks[]` {id, ring, z, level}: our walkways at deck/concourse.
- **The check**: `bun run oldbuild:match` (tools/world/oldmatch; tests match.test.ts, a failing example each). Tolerances: footprints 0.5 m (outline of the union of footprints, both ways; a gap under 0.5 m between neighbours is a touch), kerbs and pavement edges 0.5 m (kerbs derived from roads and circuses on both sides; pavement edge = the outline of pavement ∪ footprints), entrances 1 m at the same level, levels, deck and pavement tops exact (1 mm). Every mismatch listed in `briefs/075/oldbuild/match.json`; any mismatch or OV-1 fault exits 1. Inside the Handover zone (standards.blocks.zone) a mismatch is a hard failure, never patched; outside it the old-built world wins, so `briefs/075/oldbuild/plan-patch.json` (`suborned.plan-patch/1`) proposes ops for Datum (block/plot rects, new plots, road reviews with the built width and centreline, door reviews per block), plus `builderFixes` for Latch (levels, decks, pavement tops: our network, kept as planned). The plan is never edited by the check.
- **OV-1** also runs on the old-built footprints (landmarks left out, as OV-1 leaves them out of our world: a landmark's ring is its reserve).
- Heights are reported (`heights.byKind`, tall > 17 m vs low), not compared: they are Sky's flagship/domestic rule, not mission maths.
- Overlap rule: OV-1 (tools/world/rules/overlap.ts + overlap.test.ts) is finished and registered; `bun run plan`: OV-1 PASS, 0 faults outside the Handover (79 among the zone's protected buildings, reported). The frontage fix that goes with it is `OLD_BLOCK.intoWalls` (off) in tools/world/gen/oldblocks.ts: that file's working-tree diff is only this OV-1 fix, not palette work, and should be committed with overlap.* (Aster).

### Plan match: Latch's export read as is (Splice, 2026-10-29, after cc903ae0) · for Latch, Datum
- **Schema agreed by adapting the reader**, not Latch's file. `briefs/075/oldbuild/swap/built.json` (`latch.oldbuilt/1`) is read by `tools/world/oldmatch/latch.ts` into `suborned.oldbuilt/1`; `bun run oldbuild:match` takes it by default when `oldbuild/built.json` is absent. The schema gained four optional fields (`contracts/world/oldbuilt.schema.ts`):
  - entrance kind `bay` (with `normal`, `w`);
  - `coverage` (where ground was laid piece by piece);
  - `zone: 'ours'`;
  - `notes`.
- How the pieces are read:
  - **Points** are already in the plan frame (offset 0, 0).
  - **Buildings**: kind mass → domestic, frontage → commercial, building → flagship, landmark → landmark. Height = top − base.
  - **Bay fronts stand in for doors** outside the zone. A door of ours within w/2 + 1 m along a bay and 1 m across it, at the same level, is matched. An old building (run segment) none of whose bays carries a door of ours is built-only, one per building. Inside the zone the check uses our real doors.
  - **Kerbs** come from the stones: 2 m along the stone, square to its forward. Corners are R 2 arcs about the cell's walk-side corner, noses R 1 half-rounds. Stones are joined end to end into lines.
  - **Asphalt meshes are not used**: they are decks laid under the pavements, not carriageway extents (Street road deck is x −8..10, its kerbs are at ±8).
  - **Flags** are 2 m modules turned to their grid. The file has no yaw, so the turn is read from each module's 2 m neighbour (Southside's grid is turned).
  - **Levels**: old/road and old/walk = the street level. Ringed levels = our decks and platforms. old/water is not compared. old/promenade is reported.
  - **The zone** is taken from our export, clipped to the zone rectangle. Any old piece strictly inside it is an `intrusion`, a hard failure.
  - **Where ground is compared**: only inside the coverage (each stone or flag cell grown 6 m) and on the plan sheet (x −460..380, y −560..300). Beyond the sheet the old CityMass is backdrop and is counted as not compared.
- **Asks of Latch** (optional, they sharpen the check):
  - (1) a `yaw` on each pavement module;
  - (2) real ground-storey outlines for masses and bays, not bounds boxes. OV-1 can't give a verdict on boxes (below);
  - (3) re-export the swap on the 0.5.3 export: built.json was made from v0.5.2 (sha d9262e4f…), so S1 Crown Circus's platforms and banks are plan-only.
- **Result** (`bun run oldbuild:match`, exit 1): **0 Handover-zone hard failures, 0 intrusions**. Outside the zone, 1273 mismatches. Areas: core = the old builders' piece-laid streets, district = the rest of the sheet.

  | kind | built-only | plan-only |
  |---|---|---|
  | footprints | 261 runs, 7469 m (core 127, district 134) | 189 runs, 10316 m (core 57, district 132) |
  | kerbs | 226 runs, 3057 m (core) | 83 runs, 2078 m (core) |
  | pavement edges | 196 runs, 4078 m (core) | 103 runs, 2469 m (core) |
  | doors / bays | 102 old buildings with no door of ours on their bays (core 76, district 26) | 106 doors of ours on no bay (core) |
  | levels | promenade −0.85 (not a plan level) | none |
  | decks | none | 4 (Crown Circus, stale export) |

  - **Levels differ**: the street is z −0.002 / walk 0.15, the plan's is 0 / 0.18.
  - **Pavement tops**: all 2661 flags are at 0.15, ours at 0.18 (one grouped mismatch).
  - **Not compared**: 70.6 km of old footprint outline beyond the sheet, 5.0 km of our kerb and 9.3 km of our pavement edge outside the coverage, 384 of our doors outside the coverage.
- **Road widths** read from the stones (patch `to`): boulevard and crown-avenue are 16 m built against 20 m planned (kerbs at ±8). harbour-market 16 and sunset-east 12 agree. apron-road is 12 m but shifted 4 m. market-street's 26 m reading is low confidence: Southside's diagonal kerbs fall inside its search band.
- **OV-1 on the old-built footprints: 1299 faults over 1927 footprints. Treat these as box artefacts, not a verdict.**
  - 876 are beyond the sheet: CityMass component axis boxes.
  - 423 are on the sheet:
    - 235 bay × bay: prefab bounds 14 m deep crossing at corners and back to back; 27 of them are within one run;
    - 78 bay × a tower standing on its podium: the tower base is 16.32, under the bay box top of 16.45;
    - 66 bay × CityMass;
    - 44 CityMass boxes.
- **Plan patch** for Datum: `briefs/075/oldbuild/plan-patch.json`, 175 ops plus 7 builder fixes for Latch.
  - Ops: 42 block/plot rect sets, 36 new plots grouped by old building group, 14 removes, 61 block/plot reviews (pavement edges and doors), 11 road reviews, 1 circus review, 8 landmark/signature reviews, 2 general reviews.
  - Builder fixes: street level and pavement tops 0.15 → 0.18, the promenade, 4 Crown Circus decks (stale).
  - The plan is not edited.

## SpaceHQ switch (Datum, brief 075 remap step 2, 2026-10-29) · oldbuild/READY-spacehq-datum
The setting is `plan.spacehq.form`, `"old"` (the default) or `"ours"`. To flip it, run `bun tools/plan/oldtown/transcribe.ts --spacehq ours|old --out <plan>`; without the flag it keeps the form already in `--out`. It writes the chosen form into `signatures[space-hq]`, `skyrail[0]` (S1), Crown Walk (`foot`), the Sphere Park and Crown Forecourt plots and `standards.skyline.dominant`. Both forms are kept in full in `plan.spacehq.forms.{old,ours}` (signature, S1, forecourt, dominant). The old → ours → old round trip comes back byte for byte, and Pulse's 8 journeys are kept in both forms.

**Where it is:** draft `contracts/world/harbour-district.oldtown.plan.json` (v0.5.3, form old). This is the transcribe default and reproduces byte for byte. **The live `harbour-district.plan.json` is still 0.5.2 (unchanged, exit 0)**, because the old form fails one world rule, NP-1 (see Blocker). Once that is cleared, the swap is `cp contracts/world/harbour-district.oldtown.plan.json contracts/world/harbour-district.plan.json`.

**"old" (default): what Latch builds**
- The old gold SpaceHQ, placed by the old builders exactly as the old reel had it: `DecoKitStreetSkyline.Landmarks`, `Put("SpaceHQ", (0, 0, 450), yaw 0, scale 1.35)`, with DK_Gold #F2C14E m1 s0.45 and the showpiece probe.
  - Plan centre (0, 244).
  - Ground (the kit box 80 × 252 × 88.8, pivotOffset z -4.4, × 1.35): x -54..54, y 178.12..298. The grand stair's foot is at y 178.1 (old z 384), facing the circus.
  - Tip 340.2 = `standards.skyline.dominant` (SK-1 passes; rivalMax 200 kept). No exception is needed: the dominant is exempt from the harbour band.
  - No beam goes through it.
- Crown Forecourt (open) runs from y 144 to 178.12, ending at the stair foot.
- **S1 Crown Circus station** (`S1/crown-circus`) sits beside the HQ on S1's north leg: line x -95, station at (-95, 238), platforms y 213..263, north–south, `SkyRailStation_Twin`.
  - S1 is now 3 stations: Western Park, Orbit Gardens, Crown Circus. Spacing 204 and 304 m.
  - Banks as the checker's world lays them (L-1, L-5, SF-9, SF-12 and SF-13 pass):

  | side | stair tower (to 27.6) | run A (to deck 9.43) | run B (to concourse 24) | lift (to 24) |
  |---|---|---|---|---|
  | west (l) | x -109.7..-102.7, y 244..253.6 | x -104.9..-102.7, y 257..278 | x -109.3..-107.1, y 253.6..283.8 | x -112.3..-109.9, y 244..246.4 |
  | east (r) | x -87.2..-80.2, y 244..253.6 | x -87.2..-85, y 257..278 | x -82.8..-80.6, y 253.6..283.8 | x -80..-77.6, y 244..246.4 |

  - Station entrances at (-115.25, 257.8) and (-74.75, 257.8).
  - Watchers: pole cameras at (-115.75, 244.3) and (-74.25, 244.3), the robot dock at (-118.75, 253.8), plus canopy cameras on the platforms.
- **Crown Walk** (`foot.crown-walk`, walk, 12 m, y 238, x -115 → -54) runs from the west bank through the station east to the HQ's west face. The station stands on it (N-P2) and fronts it (W-3).
- Sphere Park has a slot cut out of its east edge for the walk (x -121..-54, y 232..244). S1's pylons stand on the park north and south of the walk.
- **Latch, from the old-built scene:** cut nothing round the HQ (the old piece stands as it is). Add our S1 beam and pylons along x -95 and the station with its banks, as in the table above. The station is the newer build kept under "newer builds", with the old finish.

**"ours" (data, `plan.spacehq.forms.ours`):** the SpaceHQ_Portal remake (252 m), drum r 40 at (0, 244), ground x ±40, y 204..284, legs y 204..224 and 264..284.
- S1 runs north on x -95, then east on y 244 (R 70) through the drum and off the sheet east over the quay.
- Crown Circus station is inside the drum at (0, 244), `in: signatures/space-hq`. Crown Walk runs on y 244 from x -54 to the quay (x 48).
- The portal is kept as `portalOnAxis`, because P-1 and `portalsOf` read a portal corridor only on y 0.
- **Not buildable yet:** with `--spacehq ours` the checker fails, and every fault is the generator's (Splice). The station banks are not laid for an east–west station off y 0 inside a drum (L-1, L-5, SF-9, SF-12). There is no pylon ground over x -54..48 (S-P1). The R 70 turn at (-95, 244) ends at x -25, so the platform has no straight (S-S1), and the corner clips Sphere Park by 4 m (S-C1). Both are mine to re-lay (a tighter turn further west, or the drum's line further north) if Sky picks "ours". Kept as data until then.

**Blocker for "old" in the live plan (Splice and Vela; Aster to route):** NP-1 fails with "signature/space-hq: procedural stand-in for SpaceHQ (no adopted kit piece SpaceHQ)". The old SpaceHQ is the old builders' piece. Our kit.json has `SpaceHQ` **rejected**, so the TS world can only carry it as a procedural signature to its tip (data only; Latch draws nothing from it). Two honest fixes, either one:
- (a) Kit (Vela/Splice): adopt `SpaceHQ` (the same old piece, sha already in kit.json), and Splice's `tools/world/gen/signatures.ts` reads `signatures[].scale` (1.35: kit scale [k,k,k], tip = base + size.y × k). The TS world then places the old piece as the old builders do, SG-1 and SK-1 read 340.38, and NP-1 passes.
- (b) Splice: NP-1 accepts a signature the plan marks `oldBuilder` (placed by the old builders, not our kit).

I did not touch either file (not mine). The plan already carries `kit: SpaceHQ`, `scale: 1.35` and `oldBuilder`.

**Generator bug found (Splice):** `tools/world/gen/signatures.ts` places every station's kit at `[st.at[0], 0]` with yaw π/2.
- In the live export, `station/S2/harbour` stands at (117, 0) instead of (117, -140).
- In the draft, `station/S1/crown-circus` stands at (-95, 0) instead of (-95, 238).
- North–south stations need the station's y and yaw 0.
- Latch: take station places from `plan.skyrail[].stations[].at`, not from the export's station kit, until this is fixed.

**Handover zone untouched.** Through the overlay on the draft, V-1, M-1 (getaway 447, official 1124, getaway-service 126, rivals 593), W-1 (Café → Orbit Gardens 243), C-0, C-1 and C-3 are identical to the live plan's. The zone's 592 street items are the same pieces at the same places. Their counters shift by +21 (e.g. pole-camera-197 → -218) because the new station's items are numbered first. The takedown uses zone ids (`-z57`); after the swap, run `bun test tools/sim` to confirm.

**Evidence (2026-10-29):**
- `bun tools/plan/cli.ts` (live): exit 0, 158 PASS. With `--overlay tools/missions/handover/overlay.ts` (live): exit 0.
- `bun tools/plan/cli.ts contracts/world/harbour-district.oldtown.plan.json`: exit 1, 157 PASS, 1 FAIL (NP-1 only). With the overlay: 162 PASS, NP-1 only.
- transcribe re-run into a copy: cmp identical. Round trip old → ours → old: identical. `bun run typecheck`: clean.

## SpaceHQ live (Splice, brief 075 remap step 2, 2026-10-29) · oldbuild/READY-spacehq-splice · for Latch, Datum
**SpaceHQ live: plan sha256 `e104fcb1584ed5f316d9a3c1e6080955e878c3595c7accf3d6688f822f6fbcbc`** (`contracts/world/harbour-district.plan.json` = Datum's draft 0.5.3, form "old"; the 0.5.2 copy is in out/scratch/splice/plan.live-0.5.2.json).
- Kit: `SpaceHQ` adopted (kit.json status only; the entry is otherwise unchanged). It is the old repo's binary (fbx sha f5399b46…), never the generator's brief 007 rebuild of the same name: `OLD_PIECES` in tools/kit/catalogue.ts keeps the old fbx and skips the generator LODs. `bun run kit` copied it into unity/Assets/Suborned/Kit (and pruned the rebuild's L1/L2).
- tools/world/gen/signatures.ts: a signature's `scale` scales the kit ref ([1.35, 1.35, 1.35]; tip = base + size.y × k = 340.38); `oldYaw` (the old builders' Unity yaw) gives kit yaw −oldYaw, so SpaceHQ exports `at (0, 244)`, yaw 0, as `Put("SpaceHQ", (0,0,450), 0, 1.35)`. (The old piece's stair is on its kit −Z, so plan yawDeg 180 is the stair heading, not the kit yaw.)
- Stations: each at `plan.skyrail[].stations[].at`, yaw from its path's direction there (folded to [0, π)): S2 Harbour (117, −140) yaw 0, Skyport (114, 0) yaw 0, S1 Crown Circus (−95, 238) yaw 0; west-end and gardens keep (x, 0) yaw π/2. Latch: the export's station kit refs are now right.
- Handover zone unchanged: ZK-1 PASS, zone-stable test PASS, C-0/C-3/M-1 as before (getaway 447, official 1124, service 126, rivals 593), `bun run sim:site` left sim/data byte-identical (site sha c9b862c3… unchanged, so missions:check not rerun); civic-block.cover.json changed only its kit/plan provenance hashes.
- "ours" (`transcribe --spacehq ours`): exit 1, 151 PASS. Datum's: S-P1, S-S1, S-C1. Generator side (L-1, L-5, SF-9, SF-12) all come from one gap: the drum's ground hall (x ±40, y 224..264, between the legs) is not public ground for the bank search (connectors.ts `publicOk` takes only park/open/square plots, foot ways and quays; Crown Walk's 12 m covers ±6 m of the line, a bank needs 4.75..15 m off it). Either the plan gives the hall an open/square plot, or the generator takes a signature's ground less its legs as bank ground. Not done: kept as data until Sky picks "ours".
Evidence: `bun run world` ×2 exit 0, same sha 7ffe56b9…; `bun run world:faces` exit 0 (every wall face is kit); `bun run plan` exit 0, 158 PASS (NP-1, SG-1, SK-1 pass); with the overlay exit 0, 163 PASS; `bun run cover` and `--check` exit 0; typecheck clean; tests tools/world tools/plan tools/kit tools/missions 750 (1 skip, 0 fail after the kit test was updated for the adoption), tools/sim 56 pass; new tools/world/gen/signatures.test.ts (2 pass).

## Perf A/B (Redline, brief 075, 2026-10-29) · for Latch · Aster routes · old-built town vs our district
Sky asked "what's the performance difference in rendering?" between OldTown.unity (OldTownSwap, cc903ae0) and HarbourDistrict.unity. My static census (YAML only, no Unity) and the table are in `oldbuild/perf.md`; the census data is in `redline/oldtown-census-075ab.json`. Two asks, both from the same commit, nothing saved into either scene.

**A. Studio counts (batchmode offscreen; no Air).** Use After072's estimator, the same one behind `latch/billboards/counts.md` (a `-tag 075ab` beside `075f1`), on **both** scenes. Add Baseline072's UnityStats (Game view) only if the Studio's Editor is free. Run four configurations:
1. HarbourDistrict, GRD on (as built).
2. HarbourDistrict, GRD off.
3. OldTown inside `OldTownSwap.WithOldSettings`, as Sky saw it: the old PC_RPAsset with GRD off as built, then the same with GRD on.
4. OldTown under **our** pipeline asset and renderer. This one separates content from pipeline.

- **Views:** cell-open-30 (= cafe-square-30), office-front 30/210, cafe-square 210, circus 30, harbour-terrace 30, harbour-station 30, southside 30. These are SwapShots.PlayViews at play zoom (ortho 14, pitch 38, stand-off 160). Add the golden view (SwapShots' reel t 10, perspective) and one street-level tp-street pose per scene. OldTown foci are plan y + 206.
- **Per view:**
  - SetPass estimate (forward, depth-normals, shadow per cascade).
  - Draws (main and shadow), main tris and shadow tris, casters.
  - Visible renderers, distinct materials, distinct shader variants.
  - The share of renderers and tris under `District crowd and traffic` + `Southside crowd and traffic`.
  - The share under the DK_Tile_TramRail / DK_Pave_Flags / DK_Kerb_Granite ground instances.
  - Reflection probes touching the frame.
  - Batchmode ms (indicative).
- **Per scene:** texmem (After072 `texmem`), plus the probe cubemaps and the glTF textures listed separately.
- **Write to:** `briefs/075/latch/ab/counts.md` and `.json`.
- **Check first:** does the play camera in OldTown carry TacticalShadows (cascade refit, SSAO off at ortho)? Does the sun's LensFlareComponentSRP stay active at play? Say which.

**B. The Air build (make it, don't send it: the run needs Sky's OK).** Spec: `redline/perf-075ab.json`; script: `redline/run-air-075ab.sh`.
1. `PerfBuild` gains `-perfScene <path>` (default HarbourDistrict) and resolves stops given in plan coordinates per scene (OldTown z = plan y + 206; WorldStamp and fingerprint checks as now).
2. Build two development players from one commit, people layer stripped in both:
   - "The Suborned Perf Ours.app" (HarbourDistrict).
   - "The Suborned Perf OldTown.app" (OldTown). Its build carries the old PC_RPAsset as the quality level's pipeline (in memory, restored, both settings files byte-checked as OldTownSwap does), with lodBias 2, realtime probes on and mip streaming off. Both pipeline assets go in the build.
3. New PerfVariant switches. Each must be echoed in the result, and each must throw on a scene without its target:
   - `oldCrowd` on|off: renderers off under the two crowd roots.
   - `msaa` 1|4: the active asset's msaaSampleCount.
   - `oldLens` on|off: DOF and the sun flare off, depth and opaque textures by request.
   - `pipeline` old|ours: swaps QualitySettings.renderPipeline at start.
4. Resolve `perf-075ab.ours.resolved.json` and `perf-075ab.old.resolved.json` beside the script.
- **Done-check:** both players open on the Studio with `-perf <plan> -perfNoWarm`, the results echo all four switches, and the stops frame the same plan points as SwapShots' play-*.png.
- **Next owner:** Redline reads A and fills perf.md's Studio rows. Aster asks Sky about B.

## Civic restyle (Vela, brief 075, 2026-10-29) · civic/READY-vela · for Latch, then Waymark
Sky: "could we bring the civic block into looking more aligned without ruining the game logic?" This is a visual-only restyle of the Records Office, café and square in the old town's materials. Table: `civic/restyle.md`. Data for Latch: `civic/restyle.json`.
- **No decokit run and no piece regenerated.** A mesh's sha256 keys `kit.collide.json`, so every change is one of: a material slot on an existing renderer, a ground material, or a scene-only overlay with no collider.
- **Blender runs (renders only, none at the same moment as anyone else's; no headless Blender was running):** `tools/decokit/review075_civic.py`, run as:
  - `before`
  - `after` ×3 (round 1: terrazzo inlays and flagged forecourt, rejected; round 2: blade signs, dropped; round 3: final)
  - `after cafe-stone`

  Output in `civic/*.png`, with the strip in `civic/compare-before-after-alt.png`.
- **Proposal:**
  - Records Office: DK_Limestone → DK_Limestone_Tex (the old 2 m panel sheet); charcoal spandrels → DK_GlassDark (ribbon windows).
  - Café: Circus render (DK_Render_White__circus) with coral sill ribbons (DK_Enamel_Seafoam__circus) and a teal shop fascia (DK_Enamel_Teal__circus).
  - Planters, terrace edge and bed kerbs: DK_Limestone_Tex.
  - Square and forecourt: DK_Terrazzo_Ivory (the old plaza floor). Inlays keep their enamel, for readability.
  - Two HangingBaskets on the café's outer piers.
- **Locks:**
  - `bun run sim:site`: sim/data byte-identical (8 files, sha256 compared before and after).
  - `git diff` on kit.collide.json, civic-block.json and sim/data: empty.
  - `bun run collide --check`: exit 1 on **SpaceHQ only**. This predates the restyle: Splice's adoption (5bb7109d) has no volumes yet. The fix is `bun run collide --only SpaceHQ`, which adds one entry and changes no existing hash. It is not run here because kit.collide.json is locked for this task; Aster to route.

## Swap: network (Datum, brief 075 remap step 2, 2026-10-29) · oldbuild/READY-network-datum · for Latch
**Plan 0.5.4, sha256 `aa27b31df1722a72699e3d6dc3e4c472fe4c6eb932699226d082a300e48687c7`** (live = draft `harbour-district.oldtown.plan.json`; `bun tools/plan/oldtown/transcribe.ts --out <plan>` reproduces it byte for byte; the SpaceHQ old → ours → old round trip is byte for byte). Read against Latch's swap (cc903ae0, swap-rule.json). Splice's plan-patch.json had not landed, so no block or kerb op from it is applied here.

**S1 and the Sphere (changed).**
- The old builders stand the SphereStation at ×1, as the old kit piece: legs r 58–67 at the ground and 53–61 at beam height, a ring girder r 54 at 34 m, service towers to r 72.4. S1's old north leg on x -95 ran 35 m from the centre, through the leg ring. Measured on the old fbx, the old line came within 0.0 m of the mesh; the new line comes no closer than 5.8 m.
- The plan's Sphere landmark is now the old ×1: c (-130, 124), r 72.5, top 188.4, scale 1. It was ×0.425 (r 31, top 80). This is Sky's rule that every landmark keeps its old height, and the derive now keeps pylons outside the real disc.
- New S1 'old' path (`skyrail[S1].paths.trunk`): `[-460,0] → [-52,0 R70] → [-52,156 R70] → [-95,198 R70] → [-95,300]`.
  - It runs east on Circus Road's line past both stations (Western Park, Orbit Gardens: unchanged).
  - It turns north over the Gardens Gate and the north lawn onto x -52, 78 m off the Sphere's centre.
  - An S of two R 70 curves takes it between the Sphere and the west gate tower's shaft onto x -95, north of the Sphere. That shaft is r 11 below 40 m; the disc is at 112–136 m. The beam clears the shaft by 6.4 m.
  - From there it runs off the sheet north. Every radius is ≥ 70.
- **Crown Circus station** moves north 15 m on the straight to (-95, 253): platforms y 227.8..277.8, north–south, `SkyRailStation_Twin`.
  - Crown Walk moves with it to y 253 (x -115 → -54, the HQ's west face), and so does Sphere Park's slot (y 247..259).
  - The banks keep the same layout, 15 m further north:

  | side | stair tower | run A (to 9.43) | run B (to 24) | lift |
  |---|---|---|---|---|
  | west (l) | (-106.25, 263.6) | x -103.85, y 271.8 → 292.8 | x -108.25, y 298.6 → 268.4 | (-111.15, 260.0) |
  | east (r) | (-83.75, 263.6) | x -86.15, y 271.8 → 292.8 | x -81.75, y 298.6 → 268.4 | (-78.85, 260.0) |

  - Gardens → Crown Circus is now 374 m (was 304). The timetable run goes from 25 to 31 s; headways are unchanged (the Handover's 0:00 phase holds).
- **Pylons:** S1 now has 31 (was 26); 72 across the network. Take the positions from the export (`transit.skyRail[].pylons`). Near the Sphere they stand at (-52, 82.4), (-52, 111.6), (-52.1, 129.5), (-54.2, 143.8), (-68, 171.1), (-79.9, 184.1) and (-89.1, 199.4), all at least 77.8 m off its centre.
  - S1 pylons 13 and 14 no longer stand on the old west terrace's north stub, so the 4 StreamBay_3 at x -116, y 9..27 stay down only because of the plan's lawn cut (below).
  - Over the Gardens Gate, pylons 12 (-125.5, 0) and 13 (-107.6, 1.5) stand just either side of the old terrace strip (x -124..-110).
- **Park edge:** Sphere Park and the north lawn take x -54..-50 of west-wall-n's back yard for y 9..144, so the line and its pylons stand on park (S-C1, S-P2). The old W4 wall's back is at x -46, so no old piece moves. The beam is 6 m off those backs and 4.1 m off CornerTower_12.

**What Latch needs to do in the old-built scene:**
1. Lay S1's beam and pylons from the new export, not the old x -95 leg. The Sphere stands as the old piece; nothing round it is cut.
2. Place Crown Circus station and its banks at the new place (y 253). Crown Walk is at y 253.
3. Cut the old west terrace (DecoKitCityBlocks garden-square wall at x -110) where the plan has the Gardens Gate (y -9..9) and the north lawn (y 9..50). This is the plan's deliberate seam: S1 and the Gardens Gate cross there. Today StreamBay_3/3W at y -12..6 and the stub at y 30..46 stand under the beam (5 m below its underside) and on the gate.
4. Export bug: in swap/built.json the SphereStation ring is about 2× the piece (x -276..16, y -20..268; the mesh is r 72.4). That ring made the "flagship clash" in the log, and it will throw off plan-match.
5. Pylon 14 (-90.7, 7.4) stands on the old basin's terrazzo ring, 1.3 m off its coping (OT-2 passes). It is acceptable as is; say if it reads badly at play zoom and I'll move it.

**T1: not re-routed, and why.**
- T1's streets are the Handover's mission streets. The official arrival (1124 m) is Crown Road → Sunset Way → Market Street; the getaway (447 m) is Market Street → Sunset Way; the rivals' route (593 m) uses Market Street x -60..0. Market Street also carries the zone's gates.
- Every stop is kept (terraces, lamplight, rocket on Sunset Way and Market Street).
- The old town has no street west of the Boulevard: there it is procedural CityMass on a 70 m grid with gaps, not roads. In the Southside the old blocks stand across Market Street's line.
- Moving the tram off these streets means dropping or moving stops (N-R1, N-C1 and the S1 interchange at Sunset Terraces depend on them) or moving Handover streets (M-1 timings). The brief rules both out.
- So T1's path is unchanged, and the clashes Latch found are seams, fixed on the old side:
  1. **Market Street, Southside seam (x -158..0).** The carriageway is kerb to kerb y -419..-401. The plan's plots start at the kerbs: ss1 and orbit-gardens to the north, lantern-court and ss3 to the south.
     - Standing in it today: the old SS1's north end (StreamCorner_3 at (-32.8, -407.2) and (-11.2, -407.2), StreamBay_3 at y -410..-415), RL1's and RL2's corners (x -146..-68, y -400..-419) and their palms, cypress and lamp.
     - These 15 bays and the corners give way to Market Street, as at the zone edge.
     - Better than a cut: shift those old blocks onto their plan plots when Splice's plan-patch lands (block ops). I'll apply its ops to the plan then.
  2. **West of the Southside** (Sunset Way x -340, Crown Road y 280, Market Street x -340..-158, plus Circus Road, West Row and Lamplight Lane). The old ground has no road deck there.
     - Lay our road from the plan: the plan's finish is the old DK_Asphalt_Road, with kerbs and pavements.
     - Keep the old CityMass off those streets with its own mechanism: in `DecoKitCityMass.Cap`, return 0 for a cell whose lot (±28.7 m with jitter) meets a plan road's corridor (kerb to kerb plus the pavement), a station bank or a stop.
     - That stands down the 21 CityMass parts the tram removed (x -333..-352, the x -350 column) and the 4 cut by S1's bank and newsstands at y -30, rather than cutting them after the build.
- The boulevard leg (x 0) is the old line's own street, and the old rails stand there.

**S2 (unchanged in the plan): builder fixes.**
- S2 pylon-14 at (117, -140) stands on the Market Pier's landing stage. A pylon on a pier stands on the deck; it must not cut the pier mesh (swap-rule: "mesh:Harbour/Market pier" gave way).
- The Skyport station's platforms are at 22.8–24 m. They do not clash with the Skyport pier, its piles or the quay wall at the ground, which the swap cut in plan. Compare heights.

**Evidence (2026-10-29):**
- `bun tools/plan/cli.ts`: exit 0, 158 PASS. With `--overlay tools/missions/handover/overlay.ts`: exit 0, 163 PASS.
- Overlay diff against the 0.5.3 plan: only S1 counts, the pylon totals (67 → 72) and item counts change. C-0, C-3, M-1 (447 / 1124 / 126 / 593), M-2, M-3, V-1, W-1 and ZK-1 are identical.
- RCH-1 39/39, L-1, L-5, S-R1, S-P1 (longest 30.9 on a disc span), S-P2, S-S1, S-S2 (204, 374), S-C1, N-R1, N-C1, SK-1 (sphere 188.4 under 200), SK-T1/T2/T3, SF-18 72/72, NP-1, K-1 all pass.
- `bun run world` exit 0. `bun run sim:site` left sim/data byte-identical. `bun run cover` changed only the plan hash in civic-block.cover.json, and `--check` passes.
- `bun run typecheck` is clean. `bun run test tools/plan tools/world tools/missions tools/sim`: 791 pass, 1 skip after one fixture fix (kit-fit.test.ts: the "scaled past its disc" case now uses ×1.2, since ×0.6 fits the ×1 disc).
- Map: briefs/075/oldbuild/network-s1.png. The gold box in it is the 2× Sphere ring from built.json; read the line against r 72.5.
