# Brief 075 · civic block restyle (Vela, 2026-10-29)

Sky asked: "could we bring the civic block into looking more aligned without ruining the game logic?" This restyle changes only how the testbed's Records Office, café and square look, using the old town's materials. Every piece stays the mesh Sky built, in the same place. The data Latch applies is `restyle.json` (the same folder). The renders are `*-before.png`, `*-after.png` and `*-after-alt-cafe-stone.png`, side by side in `compare-before-after-alt.png`: before | proposal | alternative. The rows are café square y030, office front y210, office entrances y030, and the street (orbit-walk, golden-style).

## What moves and what cannot
- **No piece is regenerated. No FBX is written.** A mesh's sha256 is the key in `kit.collide.json`, so any regeneration, even a visual-only one, changes that file. The task locks it, so every change here is one of three kinds:
  - a material slot on a renderer the builder already makes;
  - a ground material;
  - a scene-only overlay with no collider.
- Footprints, heights, doors, mission points, bays, cover, colliders, walkable surfaces and furniture positions are untouched. So are `civic-block.json`, the world export and `sim/data`.
- The old builders' facade pieces (StreamBay, HallBay, ArcadeBay, coping) are **not** swapped in. That would replace Sky's café and office, which the brief rules out. Their language comes over through the materials instead:
  - the old 2 m stone panel sheet;
  - the Circus ensemble's cream render with its coral sill ribbons;
  - the dark teal ribbon glass;
  - the old plaza's terrazzo floor.

## Surfaces and pieces: today → old equivalent → action

| Civic surface / piece (count) | Today | Old town equivalent | Action |
|---|---|---|---|
| Records Office mass `RecordsTower` (1) | DK_Limestone, flat cream #E7DECA + detail | Hall / DecoBay / CornerTower: the stone anchors stay limestone on the `facade_panel_v1` sheet (DK_Limestone_Tex, 2 m panels, white tint) | **DK_Limestone → DK_Limestone_Tex**. UVs are world metres (probe 0.97–1.00 UV/m²), so panels land at 2 m |
| Records spandrels (`RecordsTower` DK_Enamel_Charcoal, 1080 m²) | charcoal enamel between windows | ribbon windows: continuous dark teal glass (DK_GlassDark #35585A) | **→ DK_GlassDark**. Each storey reads as one ribbon. Doors are not in this rule |
| Records teal fascia / seafoam sills / Stone_Dark plinth / Brass_Civic | kit colours | the same values in the old kit (#1F6C67, #72B5A7, #4E5152); old rule: plinth stays the kit's | keep (brass stays a hairline) |
| `RecordsEntrance`, `RecordsEntrance_Official` (1 + 1) | DK_Limestone + banners, canopy, doors | Hall entrance tower | **DK_Limestone → DK_Limestone_Tex**. Doors, banners and canopy kept |
| Café upper storeys `CafeUpper_3` (1) | DK_Limestone walls, seafoam bands | a streamline building: old town paints these in smooth render, here the Circus ensemble (wall Naples cream #EBDCB5, sills coral #C8553D) | **DK_Limestone → DK_Render_White__circus**, **DK_Enamel_Seafoam → DK_Enamel_Seafoam__circus** (coral pinstripe ribbons) |
| Café shopfront `CafeBay_v2` (1) | DK_Limestone piers, seafoam fascia, CoralSoft/Cream awning, clear glass | old shopfront ground storey: cream piers, deep teal shop fascia | **DK_Limestone → DK_Render_White__circus**, **seafoam → DK_Enamel_Teal__circus** (#1F6C67). Awning and glass kept (testbed character) |
| Café fit-out `CafeInterior_Testbed`, `CafeLamp_Testbed_*` (8), `Parasol_Testbed` (7), `CafeSet_Tulip_Play` (8) | testbed atlas | old CafeSet_Tulip (the same family) | keep |
| Terrace edge `CafeTerrace` (1), planter runs `PlanterRun_4_A/B/C` (12) | DK_Limestone | old panel stone | **→ DK_Limestone_Tex** |
| Bed kerbs (5 `bed-kerb` masses, no kit) | look.json bed-kerb (harbour.civic) | old panel stone | **→ DK_Limestone_Tex** (`surfaces` in restyle.json) |
| Hedges, `BedTuft_A/B/C` (92), `Cypress_Play` (6), `PottedTopiary_Play` (6), `PottedPalm_Small` (6), `PlanterArc` (4), `OvalPlanter` (2) | kit planting | old Plants / Planter_* (same families) | keep |
| Square floor `plot/records-square` (surface `square`) | DK_Pave_Civic (warm tan, 4 m sheet) | old plaza field (DecoKitPlazaLayout.Floor): ivory terrazzo with seafoam rings | **→ DK_Terrazzo_Ivory** (tile 1; the .mat repeats every 1.5 m, the old tile) |
| Office forecourt `plot/records-office` (surface `civic`) | DK_Pave_Civic | the same civic floor (see below) | **→ DK_Terrazzo_Ivory** |
| Square inlays `InlayBand_*` (12), `InlayArc_*` (24) | DK_Inlay_Seafoam enamel | old TerrazzoArc (DK_Terrazzo_Seafoam) | **keep** (tried; see below) |
| `PlazaSphereLamp_Play` (8), `BollardSphere` (12) | chrome, teal, seafoam | old PlazaSphereLamp; old Bollard is red-banded | keep (already old-family; bollards keep seafoam on the civic floor) |
| `AtomFountain_Testbed`, `BoardingPavilion_TB_Square`, `CivicCabinet`, `UtilityCabinet` (2) | testbed / kit | PlazaFountain, DecoPavilion, UtilityCabinet | keep (fountain and pavilion are Sky's testbed pieces) |
| Overlays (new, scene-only) | none | old DistrictDressing.Terrace: baskets on the piers under the canopy | **2 × HangingBasket** on the café front's outer piers (x −164.75 and −153.35, y −219.7, yaw π), 2.57–3.34 m up. No collider. They take the café's fade |

## Decisions the renders made
- **Inlays stay enamel.** In seafoam terrazzo on the ivory field, the rings and the café-door spine lost almost all contrast at play zoom. They are the square's walk lines, so readable beats rich.
- **The forecourt is terrazzo, not the old pavement flags.** OldTownArt DK_Pave_Flags, laid at 4 m as the old builders did, came out busy and cool grey at play zoom. It also split the square and the office into two places. One pale civic floor matches the Records Office painting (`briefs/008/concepts`).
- **No second café blade sign.** The café already carries its CAFE fin, which meets the old rule of one blade per café. Its upper side walls only start at 10 m (the probe is in `after-probe.json`), and two more fins read as clutter.
- **Left out:**
  - TerraceScreen, PottedFern and SandwichBoard: they stand on the terrace and the walk.
  - Rooftop billboards: one would sit between the camera and the terrace at yaw 210, and the office is civic.
- **Alternative (`cafe-stone`):** the café in Naples-cream panel stone, keeping its seafoam bands. It is closer to today but makes the café and the office the same kind of building. The proposal paints the café, so the two neighbours differ.

## For Latch (after the current swap fixes land)
Apply this in the scene build (or in SwapOurs after the import), only under the `civic` group:
1. **Material slots.** For each renderer of a kit instance whose piece id is in a `remap` rule, swap the slot whose kit material name matches `from` for the .mat at `to`. All paths are under `unity/Assets/Suborned/Kit/Materials`. Every target is already a kit material or colourway variant, so nothing depends on the git-ignored OldTownArt and no new SetPass family is added.
2. **Ground.** Give the two plots' ground renderers DK_Terrazzo_Ivory, with UV in world metres.
3. **Bed kerbs.** Give the bed-kerb masses DK_Limestone_Tex.
4. **Overlays.** Instantiate the 2 HangingBasket kit prefabs at `at`, `z`, `yaw` (KitPlacer convention: euler y = −yaw°), under the café's element. Strip their colliders and add them to the civic/cafe fade.

None of this touches the export, `sim/data` or the kit.

## Evidence
- Renders: `tools/decokit/review075_civic.py` (Blender 5.2.2, renders only), 3 passes logged in `briefs/075/notes.md`.
- `bun run sim:site`: sim/data byte-identical (sha256 of all 8 files compared before and after).
- `git diff contracts/world/kit.collide.json contracts/world/civic-block.json sim/data`: empty.
- `bun run collide --check`: exit 1, but only for **SpaceHQ**. That comes from Splice's SpaceHQ adoption (5bb7109d) and is not from this restyle. No civic piece is listed.
