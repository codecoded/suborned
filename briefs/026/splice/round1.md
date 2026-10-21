# 026 · Splice round 1: the capstone block's ground, robots and props (generator + kit catalogue)

Scope: the Handover block (the walk window of `tools/plan/walk-grid.ts`, block frame x -52..112, y -66..16) grown 30 m, plan frame x -226..-84, y -372..-148: the café, the Records Office, the square and the six streets that approach them (Rocket Lane, Starline Street, Garden Row, Orbit Walk, Pylon Row, Apron Road). Nothing is a coordinate: every rule is over plan data (plot edges, kerbs, frontage trades, crossings, trees). Deterministic; same orders, same items.

## 1. Adopted in `contracts/world/kit.json` (58 + 3, hashes and sources as `bun run kit` writes them)

| set | adopted | notes |
|---|---|---|
| Ground | Tile_Tactile, Tile_Terrazzo, Tile_TerrazzoArc, ManholeCover, DrainGrate, InlayArc_R6/R10/R16, **Ground_Swatches** | Ground_Swatches (Vela) carries the DK_Ground_* materials and textures into the kit copy; use tag `ground:materials` |
| Robots | RobotPorter, RobotWaiter, RobotNanny, RobotShopkeeper (full, 14.6k-19.7k tris), RobotWaiter_Play, RobotNanny_Play, RobotShopkeeper_Play (+ existing RobotButler_Play, RobotPorter_Play) | the generator draws the lightest piece of a role (`robot:<role>` tag): always the _Play copy |
| Furnish | VendingMachine_Cola, VendingMachine_Snack, Automat, DrinkingFountain, RoundBench, HangingBasket, WindowBox, TerraceScreen, Deckchair | WindowBox, TerraceScreen, Deckchair adopted, not placed (see 4) |
| Booths | CoffeeCart (full, tag `kiosk-food:full`: retired from the slot for CoffeeCart_Play, like PneumaticPost), Lockers | |
| Signage | SandwichBoard, Marquee_Cinema, Billboard_Wall_A-C, Billboard_Pylon_A-B, Billboard_Rooftop_A-C | the billboards and the marquee are adopted, not placed (no cinema, no blank wall or roof programme in the zone) |
| Plants | TopiaryCone, ShrubMound, FlowerBed, PottedTopiary | tagged `planter-bed:*`, not `tree`: the street-tree slot would have picked them. ShrubMound, FlowerBed not placed (see 4) |
| Street / Signals | BikeRack, Barrier, FacetedPot, InfoLightbox | Barrier, FacetedPot adopted, not placed |

**Adopted after Vela's re-point (round 2):** Kerb_Granite_0/1, Pave_Flags_0-3, TreePit_Flags_0-3. Vela re-pointed their four legacy materials at the budgeted `ground_*_v1` sheets, so they no longer cost the ground budget (the kit copy now holds `ground_pavement/kerb/gutter`, 5.9 MB + small maps, and the six stale `pave_flags_v1*` files are pruned by `bun run kit`). **Kerb decision: the kerb is Kerb_Granite pieces placed by rule** (stone on the slab edge, its gutter setts on the road side, dropped at crossings and drives); Latch should hide the extruded kerb strip and slab-edge kerb inside the capstone zone (kerb items carry `edge: cap/kerb/<road>`), so only one draws. Not adopted: Kerb_GraniteCorner_*, Kerb_GraniteNose, Kerb_Straight (our slab corners are square; a rounded return needs a rounded slab first: Datum/Latch). **TrafficLight stays a candidate:** a signal mast with a head is what brief 009 replaced with floating signal eyes (22.7k tris as well).

**Full robot prefabs nest `_Far` models:** `bun run kit` now copies `DK_Robot{Butler,Porter,Waiter,Nanny,Shopkeeper}_Far.fbx` (+ meta) as files of the full pieces, hash-checked like the rest (`tools/kit/catalogue.ts`).

Other check results: no adopted piece fails the kit tests (`kit.test.ts`), K-1 or CD-3; the robots have LODs only through their _Play copies (the full models have none: do not place them).

## 2. Placement rules (`tools/world/gen/street/capstone-rules.ts` is the table; `capstone.ts` places; world rules CAP-1..CAP-5 in `tools/world/rules/capstone.ts` check, each with a failing example in `capstone.test.ts`)

Ground (flat pieces in the new **`world.ground.pieces`** array (schema: `StreetItem` with a kit ref), not in `world.street`: paving is not furniture, and keeping it out leaves the street rules and the plan checker's code untouched; heights over the 0.18 pavement: tile top +3 mm, flags +1 mm, crossing inlay stays +2 mm above flags):
- **Tactile paving:** Tile_Tactile, two across, at every kerb-crossing landing off the civic plots (22).
- **Terrazzo aprons:** Tile_Terrazzo, two across by one deep, at the door of every lobby, hotel, bureau and hall tenancy (40).
- **Drains:** DrainGrate in the gutter every 20 m along each kerbed run, long axis along the kerb, 0.62 m out from the kerb face; none across a crossing strip or a drive or within 1.2 m of a Handover point (47).
- **Manholes:** down each carriageway's lanes (alternating, every 36 m, clear of side-road mouths and tram reservations) and along each pavement's walk centre (every 48 m, clear of tiles and furniture) (14).
- **Kerb stones:** Kerb_Granite_0/1 on each kerbed plot edge facing a road (plan `plotRing` classes), modules refit to the edge (scale x 0.97-1.03; y 1.20 so the face is the full 0.185 m), centred on the edge with the front facing the road; right-angled corners: x-running edges run through, y-running edges stop at the other's face; dropped where a crossing strip or a drive crosses (453 stones, 905 of 1026 m; 122 m dropped).
- **Flags:** Pave_Flags_0..3 in a grid anchored to each plot's corner (2 m cells refit to the plot), the quadrant by cell parity, courses along plan y (paving.py); only on pavement plots (not parks, the civic square or office), outside buildings and out to the pavement's width (745 cells).
- **Tree pits:** TreePit_Flags under every pavement tree and every unplanted pavement tub palm (47).

Standing pieces (through `Layer.fits`, the same SF-1, SF-2, SF-8 and drive tests every street item passes; the spot must pass about both the footprint centre and the pivot):
- **Robots by job (18):** waiter at the open end of a terrace run of diners and soda fountains (9) and at the east or west end of the civic café terrace (1, clear of the tables and the Handover points); shopkeeper beside a shop wall (2); butler at an office or hotel door (2); **porter at the Records tram stops** (a stop within 100 m of the zone, on the platform beside the ticket machine: 2, one per platform); **nanny** beside a garden bench or on the lawn next to it (2).
- **Vending (18):** a Cola + Snack pair backed 6 cm onto the wall of a diner, soda fountain, launderette or hall tenancy of 12 m or more, at the end nearer a junction; **Automat** on an automat wall (0 fit).
- **Kerb programme:** bike racks in the kerb zone beside bike-trade frontages, one per 40 m of street side (10); info lightboxes 8-20 m up the street from a junction corner, 24 m apart (2).
- **Frontage:** sandwich boards at diners, soda fountains, florists, grocers of 9 m or more (11); hanging baskets every 3 m under the canopy front of those trades (69, z under the canopy); lockers on bureau walls (2); PottedTopiary and TopiaryCone flanking lobby, hotel and bureau doors of 9 m or more (16).
- **Planting (Vela round 2, the old city's siting):** a FlowerBed trough and a TopiaryCone at each frontage run end; Planter_Urn_Play flanking every fourth tenancy seam; WindowBox on shop sills every 3 m clear of the door (50, z 1.13: overhead of the pavement, not ground); ShrubMound in a garden's border every 10 m. **Palm and planter never share a footprint:** a planter with a palm planted in it takes the trough (`planter:planted`), never an urn; CAP-6 checks it world-wide (the sliced urn was an urn under a FanPalm_Play).
- **No shared footprint (CAP-7):** the programme's standing pieces never overlap each other or other ground furniture (checked on footprints in the generator and by the rule), and the metre in front of every vending machine, the Automat and a locker bank is keep-clear: the sandwich board that stood before a Cola machine now stands elsewhere or not at all.
- **Pits under pots:** every urn, potted topiary, cone and potted palm at street level stands in a TreePit_Flags piece (the old city's pit; 67 pits in all with the trees).
- **Garden:** drinking fountain on the Garden Walk's edge band every 80 m (1); RoundBench round every third lawn tree (3).

## 3. Counts

Capstone zone: **1604 programme items** (1388 flat ground pieces in `ground.pieces`, 216 standing or overhead street items). All are kit-ref'd (`source: "kit"`). Old DecoKit city (whole old world, `inventory.json`, prefab instances) for scale; it is hand-placed and ~10x the area:

| piece | capstone zone now | old city (whole) |
|---|---|---|
| robots (porter/waiter/nanny/shopkeeper/butler) | 2/10/2/2/2 (+ the civic Butler as built) | 3/5/4/4/5 |
| vending machines (Cola/Snack) / Automat | 9/9 / 0 | 3/2 / 1 |
| bike racks / info lightboxes | 10 / 2 | 19 / 14 |
| sandwich boards / hanging baskets | 11 / 69 | 7 / 15 |
| drains / manholes | 47 / 14 | 36 / 25 |
| round benches / drinking fountains | 3 / 1 | 37 / 3 |
| topiary, pots, beds, shrubs (planter-bed) | 35 | 54 + 4 + 41 + 64 |
| window boxes | 50 | n/a |
| tactile / terrazzo tiles | 22 / 40 | n/a |
| kerb stones / flags / pits | 453 / 745 / 47 | 1763 / 956 / 46 |

The capstone zone beats the old city per hectare on drains, hanging baskets, vending, bike racks and sandwich boards; it is below on lightboxes, ring benches and terrace screens (unplaced); planting beds are below the old city's per-hectare counts because SF-2 limits wall planting.

## 4. Places no rule could fill (coverage gaps: to Aster and Sky, never forced; 69 logged in `gen.capstone.gaps`, printed by `bun run world`)
- **Shopkeepers (16 of 18 tenancies):** SF-2 keeps 2 m off a door's jambs; a 6 m tenancy leaves 1.8 m beside its door. Only 8.4 m and wider tenancies can host a wall-backed robot or machine. Same reason: **Automat (2 of 2 automat tenancies, 6 m wide, wall unit 1.86 m)**, 6 door-flanking planter pots, one diner board. Options for Sky/Datum: a standard exception for staffed door attendants and wall units (0.6 m off the jamb), or automat and shop tenancies of 8.4 m minimum (the plan's trade table lets automat be 6).
- **Porter on the civic square:** the boarding pavilion is the crew's getaway staging (keepClear crew-line-getaway); a porter there would stand in the Handover crew line, so the block's porters are at the Records tram stops. The Circus stops (101-106 m) are outside the programme's reach.
- **Planting at run ends and seams (P-runend 24, P-urn 9):** SF-2 keeps 2 m off a door, and most tenancy walls are 3-9 m: the trough and cone fit only where a run end has free wall beyond the first door.
- **Not modelled by a rule yet:** TerraceScreen (no terrace tables outside the civic block), Deckchair (lido roofs and the quay, outside the zone), Barrier, FacetedPot, Marquee_Cinema (no cinema here), Billboard_* (no blank wall or roof programme in the zone).
- **Waiter on the civic café terrace:** placed at its end, clear of the tables and every Handover point (1.2 m). It stands near "East side" and "West side": Waymark should look at the staging (Inez is the café's human waitress).

## 4b. Kerb ownership (agreed split proposed to Latch)
Kit granite (Kerb_Granite_0/1, kerb items in `ground.pieces`, `edge: cap/kerb/<road>`) along every straight kerbed run of the capstone zone; Latch's extruded kerb and gutter only (a) outside the zone, and (b) at junction corners, within 2.4 m of a plot corner, where there is no stone. Kerb_GraniteCorner_* and Kerb_GraniteNose stay **candidates**: our plot slabs have square corners; a corner piece needs the slab rounded to its 2 m return and its orientation checked in Unity (Datum/Latch/Vela). If Aster wants corner returns now: slabs rounded in `tools/world/build.ts` (one function) and the corner placed by rule (`kerbRuns` already knows each right-angled corner); say so and I do it next. Unplaced and logged: **Automat** (SF-2, 6 m tenancies), **Marquee_Cinema** (no cinema in the zone), **Billboard_*** (the plan states no blank-wall, pylon or roof billboard programme: Datum or Sky), **TubPalm** (tub palms stand on decks and piers; on the zone's pavements a PottedPalm_Small now marks the end of each lobby, hotel and bureau run, and unplanted pavement tub palms get pits).

## 5. What Latch needs in the builder
- **Ground from the DK_Ground_* materials, not tiles:** road ribbon DK_Ground_Asphalt (16 m, + detail 1.5 m), kerb strip DK_Ground_Kerb (0.5 m), gutter DK_Ground_Gutter (4 x 0.375 m, u along the kerb, v road edge to kerb), plot slab DK_Ground_Pavement (4 m) and lawn DK_Ground_Lawn (8 m), paths DK_Ground_Path (2 m), all in **world-space planar UV** (`MeshKit.PlanarUv`, m_Scale = 1/tile), with Vela's seeded UV offset and mirror per patch so the repeat never spans a block. Ground_Swatches is only the carrier: never place it.
- **Gutter:** `DK_Ground_Gutter` is not needed as an export element: the gutter is the road side of each Kerb_Granite module (its own sett band, 0.3375 m) and, where Latch wants the longer 4 m x 0.375 m strip, it derives from `ground.roads` (the carriageway edge is `line` offset by `width / 2`; the drop at crossings and drives is in the kerb items' gaps). If Latch wants a gutter element in the export, say so and I add `ground.gutters` (edge runs with their drops).
- **Road markings (lane dashes, centre and edge lines):** not in the export; I add them (rule: plan road class, lane count, junction clearances) as soon as Latch reports them missing.
- **Heights:** tactile and terrazzo tiles are thick pieces sunk so their top is 3 mm over the slab (`z` is in the item, already below 0.18); drains lie on the carriageway at z 0 (top +0.02); manholes on the walk at 0.1805. Items carry their kit ref; `at` is the **pivot** (the Play robots are `origin-ground` with offsets 0.1-0.4 m: the generator already accounts for it). **New reader needed:** `ground.pieces[]` (kinds `tactile`, `terrazzo`, `drain`, `manhole`; also `kerb`, `pave-flags`, `tree-pit` once adopted) goes through the same `placer.Place(el, layer, s.Kit, s.At, s.Z, s.Yaw)` as a street item, under a `ground/pieces` node; `counts.ground` does not include them. Standing kinds: `service-robot` (variant waiter, shopkeeper, butler, nanny), `vending-machine`, `automat`, `bike-rack`, `info-lightbox`, `sandwich-board`, `hanging-basket` (z 3.2 under a canopy: hangs, casts nothing), `lockers`, `planter-bed`, `round-bench`, `drinking-fountain`. The standing kinds go through the existing `Street` placer with no new builder code; the flat ones are good candidates for `KitPlacer.Bake` (no fade, many).
- **Robot behaviour:** `RobotFace` and `RobotIdle` are not ported; the face glyph data is `tools/decokit/art/robots/robot-faces.json`. Robots here are static decor (no collider, not read by the sim or the sightline rules). `variant` names the role: use it for the idle table (butler slow and rare, porter snaps, nanny watches the pram) and a face towards the street or along the terrace as exported (`yaw`: front faces the street).
- **Tree pits and kerb returns** need the slab cut or rounded: see 1.

## 6. Checks (2026-10-20, after the fixes)
- `bun run plan`: 123 PASS (116 + CAP-1..CAP-7), exit 0. Handover overlay: 127 PASS (mission points, sightlines, V-1 unchanged; V-1 47/48 as before). `bun run world`: exports, capstone line printed. `bun run typecheck`: clean. `bun test tools/world/rules/capstone.test.ts`: 8 pass.
- The 6 failing tests of the first full run: `surfaces.test.ts` (new kinds had no look key in its table; the street count now excludes `street/capstone/`) and `crossings.test.ts` (the programme's items stand last, like the civic quiet places); the rest were the flat items in `world.street` tripping SF rules and the people replays, see below.
- `bun run verify`: the plan checker, the strict r008 check, the handover overlay, HUD budget and typecheck pass; the people (r011-r016) and skyline replays pass after declaring the code change in their drift ledgers (`tools/levels/people/drift.json`, `tools/levels/skyline/drift.json`; their hash covers `tools/world/gen`); rerun warning counts per run are unchanged (r012 11, r013 6, r014 9, r015 9, r016 2). **Still red, not mine:** r017-r019 dialogue replays: their code hash covers `contracts/rules`, which Keel has modified in the working tree (rulebook.json, .ref.ts, .schema.ts, .test.ts); Keel declares it with `bun tools/levels/dialogue/drift.ts declare`.
- `bun run kit`: copied 28 files, 1232 already present and verified; kit.json 304 assets, 219 adopted.
- Touched: `tools/kit/tags.ts`, `contracts/world/{kit.json,world.schema.ts}`, `tools/world/gen/street/{capstone,capstone-rules,capstone-civic,kit}.ts`, `tools/world/gen/{street,index,kitrefs,surfaces.test,crossings.test}.ts`, `tools/world/{build,report}.ts`, `tools/world/rules/{capstone,capstone.test,crossing-parts,index}.ts`, the two drift ledgers. Nothing in `tools/plan`.
