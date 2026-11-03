# 075-E · The old street's composition, and streamline as the default grammar

Splice, 2026-10-28. Sky: "we've still lost all the lovely curved buildings etc - there's still so much missing from the old in terms of style."
The data is in `composition.json`, and the grammar it drives is in `tools/world/gen/stream.ts`, `faces.ts` (STREAMLINE) and `facades.ts`.

## How the old DecoKit street was put together (read only)
Sources: `Editor/DecoKit/DecoKitFrontage.cs` (the class table and caps), `DecoKitCityBlocks.cs` and `DecoKitSouthside.cs` / `DecoKitBlock.cs` (the runs), `DecoKitRoofline.cs` (the roofs), `workbench/Tools/Blender/decokit/skyline.py` (the elevation), and `Scenes/DecoKitStreet.unity` (instances counted by prefab GUID).

- **Runs, not plots.** Every wall was a run of `(class, bays)` segments between two caps. Each segment was one building, and its pieces were named `#run.segment`. The runs were written by hand per wall, with 6 m bays on 28 m deep blocks (14 m bodies back to back).
- **Classes.** A class is a bay with its own end and corner. The streamline family is S2, S3, S3W, S4, S6, S9 and S9W. The feature classes are P (porthole: P3, P4, P7), B (balcony: B3, B4, B8), D10 (deco, with DecoEnd and DecoCorner) and A3 (arcade, whose end and corner were the civic hall's wing).
- **Recipe, per building.** Of 102 segments in the code (the civic hall left out), 76 are streamline, 10 porthole, 7 balcony, 6 deco and 3 arcade. In bays that is S 254, P 28, B 22, D 16, A 5. On Southside the streamline flanks a feature (S, P×3, S; S, B×2, S; S, A, S). The boulevard alternates stream codes and heights by segment (S9, D10, S6, S9W, P7, B8). A block's short end takes the class of the long face it adjoins.
- **Old scene, as saved.** Bays: StreamBay 316, PortholeBay 28, BalconyBay 18, ArcadeBay 5, DecoBay 5, so streamline is 85 % of the bays. Corners: StreamCorner 70, CornerTower_12 2, DecoCorner_10 1. Ends: StreamEnd 37, DecoEnd 12. AlleyWall 13. In all, **97 % of the facade pieces were streamline.**
- **Caps.** Every block corner was an 8 × 8 m quarter-round cell, with ribbons and canopy wrapping it and an ivory fin and mast over the round. A run that stopped at a lane or plaza rounded off with a StreamEnd (R 3 quarter round, ribbons wrapping the 14 m side), and so did a segment taller than its neighbour, toward that neighbour. The parade corners took CornerTower_12, 2 m proud of both faces.
- **Bands and glass.** The ground storey has a black granite plinth (0–0.45), shopfront glazing, a teal fascia (4.0–5.0) with two gold beads, and an ivory canopy 1.6 m out at 3.85–4.0. Each 3.4 m floor has a sill band on a 1.05 m spandrel, a full-width ribbon window 2.35 m tall with chrome mullions every 1.5 m, and an ivory eyebrow 0.45 m proud. Above the top floor come a parapet band and the ivory coping. **The glass is 0.69 of every upper storey's height, across the whole bay.**
- **Rooflines and crowns.** Every roof on the straight walls was programmed. Each building has a 14 m strip at its own height, split at bay joints into 12–30 m plots, each with a theme unlike its neighbour's: Cabana 34, Planter 32, Tank 10, Conservatory 8, Pool 6, LapPool 6, Dish 5, Drum 4, Bandstand 4, Radome 4, Mast 3, LatticeMast 3, Fin 1. Five crowned towers stood across the block halves. StreamCorner fins and DecoBay stepped crowns crowned the walls.
- **Tall walls.** Nothing was stacked. The whole-wall pieces of 2–10 storeys (12.9–43.6 m) were the walls, and anything taller was a tower prefab standing on the blocks.

## What the generator does now
- **Street fronts** (`facades.ts`). A building's class is drawn with the old recipe's weights (stream.ts `RECIPE`), and the neighbour contrast still holds. Corners use the old pieces' frame: the front takes the leaving face. Ends close the run's tail (stream.ts `KIT_X`, see open issues). One street-junction corner per plot takes CornerTower_12, outside the play streets, on buildings of 9 storeys or more (3 placed).
- **Every other public face** (`faces.ts` STREAMLINE). A run of a building's whole wall takes the building's class: the bay its street front carries, else one drawn by the recipe. The bay must fit the wall at 0.9–1.1 (storey-true in the play streets), and its 14 m body must stand inside the building. Walls the whole pieces cannot reach take Vela's stacks: StreamBayStack, and at corners StreamCornerStack or StreamEndStack. That covers 7-storey walls, walls taller than DecoBay_10, setback tiers, and the strip of a wall standing above a lower neighbour (`FACES.streamParty`, Aster's call). At acute corners the body pulls back and the plain column takes the strip. A run no whole number of streamline bays tiles keeps the plain column.
- **Caps at square outside corners** (3°). Where both faces are streamline the corner takes CornerTower_12, then DecoCorner_10 at a junction where code 10 fits, then the classes' StreamCorner, then StreamCornerStack. Where only one face is streamline (a public face meeting a service back) it takes a StreamEnd or StreamEndStack, with the front on the leaving face and the rounded return on the entering one.
- **Unchanged:** SideBay and AlleyWall on service backs, shallow wings (bodies under 14 m), shop runs (ShopFill and ShopStack_Base stay; a tall shop run takes stream stacks over its shop row), and inside corners (piers).

## Counts per family (export, before → after)
Public faces only, metres along the face (a column counted once, a corner both legs):

| family | before m | after m |
|---|---|---|
| StreamBay (whole) | 2,082 | 4,300 |
| StreamBayStack | – | 4,862 |
| StreamCorner + Stack + CornerTower | 780 | 1,020 |
| StreamEnd + Stack, DecoEnd | 657 | 711 |
| PortholeBay | 1,423 | 665 |
| BalconyBay | 1,238 | 265 |
| ArcadeBay, DecoBay | 140 | 195 |
| SideBayStack | 8,820 | 2,706 |
| SideBay | 757 | 454 |
| SidePilaster | 691 | 1,047 |
| ShopFill / ShopStack | 1,723 | 1,715 |
| **streamline share of public metres** | **34.5 %** | **67.0 %** |

Placed facade triangles (catalogue L0 / L1 / L2): 14.53 / 2.63 / 1.47 M before, 12.71 / 2.48 / 1.37 M after. There are fewer pieces: a whole-wall stream bay replaces a column of 6–9 stack modules.

## Curves in the massing (the next step): what faces.ts would need
- **The plan:** bow fronts and rounded ends as arcs on corner plots (Datum's standard), with the masses' rings carrying the arc as short chords. The café, the Records Office, C-0 and the Handover footprint stay unchanged.
- **Corner on an arc.** A rounded massing corner arrives as a run of chords that `chainTurn` (15°) already joins into one run. The pass then needs to (a) recognise a quarter arc of radius 3 or 6 m (the old pieces' R 3 end and R 6 corner) and put StreamEnd or StreamCorner on it, with the cell's frame corner at the arc's tangent intersection, not at a ring vertex; (b) on larger radii, tile the chord bays with each module stepped back by its sagitta (done today for concave bends, K-6) and close the wedges between chords behind the facade (the old `Crescent` wedges); (c) at any other radius, fall back to chord bays.
- **Rules to hold:** K-6 measures against the chord ring, the coverage check counts the arc's chords as faces, and bodies on a convex arc fan apart (no overlap) while bodies on a concave arc converge. So the containment sampling must be per module, which it already is through `bodyIn`.
