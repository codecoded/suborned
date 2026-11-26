# Harbour street trades

Lantern Lane trades under a striped canvas awning. Six slatted trays put the fruit within reach and a mechanical scale stands above it. Timber risers carry the back row. Full and empty packing crates sit beneath the canopy. Angela Hardin works the flower barrow at Lantern Court. Gustav Lindahl keeps the nearby fruit stall. Jamie Smalls, a regular at the newsstand, knows Angela, and she visits Gustav. All three are existing Harbour residents. Their introduction runs through the ordinary SurrealDB ties and the same in-game routes as other acquaintances.

## Art direction

The GDD puts information in paper records, transfer tubes and the people who handle them. The Harbour post shows the loading door, brass-trimmed pressure dial, pipe and return capsules at hand height. Enamel and brushed silver cover the mechanism. The ferry porter’s trolley carries strapped cases with printed Harbour Line tags. Stall stock, work clothes and vehicle clearances establish the scale of the street. The [GDD](../../GDD/the-suborned-gdd-v0.39.0-2026-11-05.html) and [art bible](../../GDD/the-suborned-art-bible-v0.39.0-2026-11-05.html) are the design references.

The court keeps its globe, benches and through-walk. Sixteen of the existing testbed planter assemblies sit on the lower balconies of three residential wings. Their different lengths and species give individual homes a presence above the street. Records Square retains the civic fountain and its distinctive paving.

The original Studio paintings are under `~/Projects/the-suborned-store/concepts/`.

| Study | Detail carried into the player | SHA-256 prefix |
| --- | --- | --- |
| `Props/scenes/market_street.png` and `Props/sheets/market_stalls.png` | Open frontage, changing depth of stock, cloth shade | `b1d291d6d4b9`, `b0419ed7822b` |
| `Props/scenes/tram_stop.png` | Cut flowers in a wheeled barrow on the public approach | `7b072b1cf292` |
| `Props/sheets/post_parcel.png` | Physical transfer capsule, spun enamel housing and analogue dial | `b7abd36bc0fe` |
| `Blocks/hanging_terraces.png` | Planting on occupied ledges with a gap through to the skyline | `7193657b6477` |

## Assets and placement

The walk-grid bake places four fruit stalls, seven flower barrows, nine pneumatic posts and three porters’ trolleys at 17 venues. It accounts for their footprints when it places residents and calculates routes. Four stalls and seven barrows each have a resident with a work stop beside the counter. All 667 existing names and all 772 earlier introductions survive this bake. The florist’s new connection brings the total to 773. The post and trolley are scenery. The freestanding props use the ordinary street rendering layer in both the command and follow cameras.

Each new prop has three authored distance meshes. Each visible mesh draws from one shared material. Only the flower stock needs alpha cutout. The canopy bulbs use an emission map. Modelled fruit gives way to coloured mounds in the far view.

| Prop | Physical size in metres, W × D × H | Close / middle / far triangles after FBX reimport |
| --- | --- | --- |
| Flower barrow | 1.94 × 1.01 × 1.68 | 1,720 / 1,308 / 656 |
| Fruit stall | 3.00 × 1.77 × 2.61 | 11,572 / 4,748 / 1,500 |
| Pneumatic post | 0.94 × 0.89 × 1.83 | 2,060 / 1,540 / 1,188 |
| Luggage trolley | 1.84 × 0.88 × 1.24 | 3,684 / 2,068 / 1,012 |

The botanical sheet is cropped from a single Codex generation of four isolated sprays. The atlas can be rebuilt from the committed [sprays](../../tools/district/art/cut-flowers.png) using [the palette source](../../tools/district/street_trade_palette.py). The [Blender source](../../tools/district/street_trades.py) exports all four FBXs and a working blend file with their three authored levels.

## Studio check

The ten [player views](street-trades.json) cover the scout walking, the command camera, the flower barrow, market frontage, capsule post, ferry landing and evening market. The same poses and orders were captured on build 0.2.381. A second [scout approach](trader-approach.json) sends the player to the fruit stall and checks the market at command zoom and after sunset. The [district tests](../../sim/Suborned.Sim.Tests/DistrictTests.cs) check the two sellers’ accessible work positions and Jamie’s introduction through Angela. Sixteen district and citizen-order tests passed. The Unity engine bundle replayed all 495 headless scenarios and all 99 range scenarios identically.

Build 0.2.383 booted into the Handover briefing, played to tick 40 and drew its 20 HUD elements. All 15 player captures completed on the M5 Max Studio at 1920 × 1080. The original court walking shot averaged 6.85 ms per frame, with a 9.56 ms 99th percentile. The same walk on 0.2.381 averaged 7.31 ms. The dedicated Lantern Lane approach averaged 6.98 ms with 44 citizens, nine vehicles and three robots in the district renderer. The command view of the stalls averaged 5.34 ms. The settled night approach averaged 12.14 ms with a 27.04 ms 99th percentile. The latter still has hitches. These measurements cover the Studio only.

The offline verifier reports the same nine archived content-pipeline replays and five civic geometry counts as 0.2.381. The matched captures, frame reports and before/after viewer are local under `out/scratch/aster/harbour-street-trades/`.
