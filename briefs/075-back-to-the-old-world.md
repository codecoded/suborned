# Brief 075 · Back to the old world: textures, materials, designs and look

Owner: Aster. Opened 2026-10-28. Sky, after the brief 074 flyby comparison (briefs/074/flyby.html): **"yeah, let's get moving back to the old world textures. designs etc"**.

The old DecoKit world is the reference for textures, materials, designs and look: ~/Projects/the-suborned/game (Assets/TheSuborned/Art/DecoKit, Editor/DecoKit, Scenes/DecoKitStreet.unity) and the reel re-rendered in brief 074 (briefs/074/flyby/old-flyby.mp4, old-keys.json, old-stills/; a working copy of the project at out/scratch/latch/oldworld/unity). Never modify ~/Projects/the-suborned; read from it, or from the copy.

Brief 073 folds in here: the answer to "too shiny / clean" is the old reel's own grade, not a new filter. Nothing from 073's Scratch prototype goes into production.

## The gaps (from 074's flyby read), in order
- **075-A · Glint · grade, light, sky, haze, water.** Port the old reel's study grade (ACES, post-exposure −0.8 at sun 3, contrast 10, saturation 25, bloom 0.6 above 0.8, a far depth of field 300–1800 m; old-keys.json has the light dump), including its slight colour banding (part of the painted-poster feel), its teal sky, warm sun, layered haze, glare and sparkling water. Use the old project's grade, sky, water settings and HDRIs, not lookalikes. Works at play zoom by day and night; signals stay unlit, outside the grade and readable; Redline costs it.
- **075-B · Vela · textures and materials.** Wherever the old DecoKit texture or material for a surface is better than ours, use the old one: walls, glass, paving, roads (the powder-blue colour stays; the texture can be the old one), water, metals, enamel. List each surface: ours against old, the pick and why.
- **075-C · Vela with Splice · designs.** Port the old pieces and landmarks: the rounded glass-slab boulevard buildings; the red spheres, the gold arch and gold disc towers; the red-and-white striped towers and the rocket gantry; the old palms (DK_CoconutPalm), lush and close to the kerb; billboards, kiosks and street props; a textured sky rail beam; a skyline that doesn't read as white cut-out boxes. Use what our kit has, adopt the rest through the catalogue; placement from the plan and the generator, never by hand.
- **075-D · motion in presentation scenes:** people and traffic in the flyby scene (lower priority).

## Constraints (respect, don't change)
- The height cap (brief 066; the café 3 storeys, 16.8 m). If the old boulevard canyon (8–10 storeys) can't be matched inside the cap, show Sky the trade-off with pictures as a question.
- The Handover's timings and points ship; C-0 holds.
- 064 A5 at play zoom; Redline costs each part. The Air sitting stays unscheduled; its spec gains the 075 switches.
- No paid APIs. Before/after at play zoom and the flyby grid for each part. Commit only this round's files.

## Order
A and B now, in parallel with 074-H (the tint back to walls and plinths). Then Latch rebuilds once and re-shoots the play-zoom compare and the flyby grid; Redline costs. Then C, then D.

## Status
- 2026-10-28, first pass landed (fc1020d8, 61698d63, 991d5fe5; 074-H in b1661058):
  - A (Glint): the old reel's grade, sun, Trilight ambient, DK_SpaceSky, exponential haze and DK_Ocean, scaled to our sun so calibrated colours hold; play-only bloom; banding kept by leaving dithering off; night unchanged; crossing heads within 3 dE (briefs/075/grade.md).
  - B (Vela): old road texture in powder blue, old pavement cream, no lawn bands, the old sky in the glass; the rest of our kit already matches the old DecoKit (briefs/075/textures.md).
  - C 1–2 (Splice, Vela, Latch): stand-in tower blocks hidden (T-K1, 4,288 panels gone, round towers read round); the old livery (brick sphere, vermilion lattices, gold SpaceHQ; LV-1, KitLiveries).
  - Shots: briefs/075/flyby.html (grid.png: old / 074 / 075), briefs/075/shots/compare/ (player-day, player-night).
  - Redline (briefs/075/redline.md): +0.01 to +0.23 ms at the opening, projected 12.4 ms (11.0–13.9) against 13.0; nearly all of it the road's borrowed detail maps (+1 SetPass); colourways at wall + plinth pass every draw mark; 075 Air switches specified, pending (sky, ocean, colourways).
  - Open: the long haze washes the far skyline to cream (Glint; or C10 mid-ground massing); the road detail maps (the old road had none: drop them, Aster's call, Vela); the y210 beacon on DK_StreamCorner_4 LOD0 (an embedded FBX material; Vela or Latch); C3–C10 wait on Sky's calls (designs.md).

## Top priority, Sky 2026-10-28: the curves
Sky: **"we've still lost all the lovely curved buildings etc - there's still so much missing from the old in terms of style."**
Diagnosis (Aster, from the export): brief 072's every-face pass dressed the district in flat generic modules (~9,200 SideBayStack, ~1,300 SidePilaster, ~690 CornerPier); the streamline family is a small minority (StreamBay ~330, StreamEnd ~110, StreamCorner ~50, DecoBay_10 8, DecoEnd_10 4); CornerTower_12 (candidate) and DecoCorner_10 are placed 0 times; corners are piers, not rounded wraps; masses are rectangular. Every face is dressed, in the wrong vocabulary.

### 075-E, in order
1. **Streamline is the default grammar** (Splice places, Vela builds). Every publicly seen face: the StreamBay family (with Porthole, Balcony and Arcade bays for rhythm, as the old street did); every outside corner a rounded StreamCorner wrap; every free end a StreamEnd; street-junction corners CornerTower_12 or DecoCorner_10 / DecoEnd_10 where height allows. SideBay and AlleyWall only on true service backs and blank party walls. Vela builds the missing stacked streamline variants for tall walls, StreamBayStack / StreamCornerStack / StreamEndStack at Base, Mid and Top, matching the old curves and bands.
2. **Curves in the massing** (Splice in the generator, Datum if the plan needs a standard): bow fronts and rounded ends on corner plots where the plan allows, so the kit wraps a curve. The café, Records Office, C-0 and the Handover footprint unchanged.
3. **The old street's composition**: study how the old DecoKit street mixed its pieces per building (share of Stream / Porthole / Balcony / Deco, bands, glass ratio, rooflines and crowns) and encode it as the facade grammar's weights.
4. **Re-shoot**: the flyby grid against the old reel, the play-zoom compares, before and after; counts per family before and after. Target: streamline is the majority on public faces.
Constraints: the height cap, the Handover, Redline costs (stream pieces are heavier), checks green. The six design questions wait until this lands.
