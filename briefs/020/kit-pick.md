# Brief 020 · the kit pick (Vela, 2026-10-18)

Judged at zoom 14 / 38° (orthographic 28 m over 1080 p, 38.6 px/m, a person 40 to 60 px). Renders: `briefs/020/kit/pick-cars-robots.png`, `pick-roofs.png`, `pick-street.png` (one frame per piece at the play angle beside a 1.75 m person; the per-piece frames are the `<Id>.play.png` beside them). Counts are the world export of 17 Oct (`out/world/harbour-district.world.json`): how many of each stand-in the district draws today.

## Two things the brief had wrong

- **The six crossing pieces are adopted already.** `kit.json` has `LightBar_4`, `LightDotRow_2/1`, `CrossEdgeLine_2/1`, `CrossInlayArc_R24_W08`, `LightField_2/1` as `adopted`, and they are copied into `unity/Assets/Suborned/Kit/` (`out/decokit/Models/Ground/`). Nothing to adopt. (`LightDotBand*`, `LightBar_1/2`, `LightPool_*` in `out/decokit-009/` were options Sky did not pick; they are not in `kit.json`.)
- **The kit has no car.** The five "vehicles" are a tram, a box truck, two scooters and a wheeled service robot; the AirCars (Sky category) are 7.7 m hovering rockets. The official and getaway cars are the one prop the whole mission stands round, so I built them: `Sedan_Official`, `Sedan_Getaway` (below).

## What I made so Splice has something to adopt (all `candidate` in `kit.json`, sources in `tools/decokit`)

| piece | what | tris |
|---|---|---|
| `Sedan_Official`, `Sedan_Getaway` | 1950s-shaped sedan, 1.81 x 4.73 x 1.29 m, nose -Y, pivot = footprint centre on the ground, so it drops in for the 1.8 x 1.4 x 4.6 box. Teal + ivory roof / coral + seafoam roof. `cars020.py` | 3.4k |
| `RobotPorter_Play`, `ServiceRobot_Play`, `RobotWaiter_Play`, `RobotButler_Play` | the base robot (15 to 19k tris) with an L0 triangle cap: same builder, pivot, size and parts (`_Head`, `_Head_Face`). `play020.py` | 2.6k / 1.8k / 2.2k / 2.6k |
| `BoxTruck_Play`, `CoffeeCart_Play`, `BusShelter_Play`, `CurvedKiosk_Play`, `TramCanopy_Play` | the same for the heavy street pieces (29k, 25k, 12.6k, 17k, 15k) | 3.6k / 3.0k / 2.8k / 3.2k / 3.0k |
| `SkyRailStation_Blank` | `SkyRailStation` without HARBOUR on the fin and boards (the name was baked in). The world places `SkyRailStation_Twin` / `_Decks`, not this one, so it is a spare | 15.6k |

Why the caps: the street LOD category switches to L1 only below 0.8 % of the screen height, which a robot or a car never reaches at zoom 14, so an uncapped Porter (18.8k) would draw whole, eight of them at S4. Render check that the collapse keeps the shape: `briefs/020/kit/pick-cars-robots.png` and `pick-street.png`.

## Pick list, in priority order

Where "stand-in today" is what the play view shows instead (`source: procedural` in the export or a primitive in `HostViews.cs`).

1. **`Sedan_Official` + `Sedan_Getaway`**: two grey/coloured boxes at the centre of every shot of the pickup and the getaway. Need nothing first. (Fallback if Sky wants no new car: `AirCar_Teal`/`_Coral` at scale 0.6, 4.6 m, but they are wheel-less rockets.)
2. **`Roof_Tank`** (water-sphere roofs: 92): the yellow balls on legs in every top-down shot (briefs/018). 2.1k tris, a panelled sphere on four legs with a teal band. Needs the roof generator to name the kit piece (`tools/world/gen/roofs.ts` places `water-sphere` as primitive parts today).
3. **`Roof_Mast` + `Roof_LatticeMast`** (antenna-mast roofs: 161, the most common roof): the tall boxes/orange blocks on roofs. 1.8k / 1.9k tris, lattice with a beacon (the beacon is the kit's rose, not a signal). Same roof mapping.
4. **`RobotPorter_Play`** (the response dial's robots, drawn as grey drums by `HostViews.Drum`): a tracked porter reads as a machine on a job at 60 px and is wider than a person, so a swarm is readable. Keep the unlit signal lamp as a separate sphere on the head.
5. **`ServiceRobot_Play`** (patrol robot, dock robots, vending robot: 10 + 7 in the world): the small wheeled rover with a lamp-eye. Smallest silhouette in the set (0.8 x 0.7 x 1.1 m), so it will not hide a person behind it.
6. **`Planter_Trough` + `Planter_Urn`** (planters: 186): 2.7k / 3.6k tris, teal-faience trough with planting; seen at the street level of every block. (`OvalPlanter`, `PlanterRun_*` are adopted and civic only.)
7. **`PottedFern`** (tub-palms: 198): 2k tris. `FanPalm` is adopted with the `tub-palm` tag yet all 198 are still procedural in the export, so first find why the fit refuses it (scale range 0.9 to 1.1 of 2.8 m against the world's 1.5 to 2 m tubs is my guess), then use whichever fits.
8. **Roof gardens and pools**: `Roof_Pool` (12 sun-terrace pools), `Roof_LapPool`, `Roof_Conservatory`, `Roof_Planter`, `Roof_Cabana` (roof-garden: 50), `Roof_Tennis` (1), `Roof_Bandstand`. 0.1 to 2.2k tris each; seafoam, teal and coral, the colours of the paintings. Seen from above on every roof the play camera looks over.
9. **`BusShelter_Play`, `TramCanopy_Play`** (tram-shelter: 20): both draw as procedural boxes today. A swoop-roof shelter and a long canopy on two chrome piers are the first thing that says "tram stop" at 38°.
10. **Kiosks and clocks**: `CurvedKiosk_Play` (newsstand: 15), `CoffeeCart_Play` (kiosk-food: 3), `StreetClock` (public-clock: 6, 3.4k, a 4.5 m teal clock post that breaks the sky line of a corner), `PneumaticPost` (mailbox: 30, red post box with a chrome stack).

Then, lower:

- **Roof dressing**: `Roof_Dish` (3), `Roof_Radome`, `Roof_Drum` (observation), `Roof_Fin`; `Billboard_Rooftop_A/B/C` for the 20 `sign-frame` roofs (5.8k tris each, the posters carry baked lettering such as DOUBLE FEATURE: fine as in-world ads, cap them first).
- **Vending and kiosks**: `Automat`, `VendingMachine_Cola/_Snack` (vending-robot 10: read as machines, 4k each, baked word COLA), `InfoLightbox` (timetable-column: 20, 5.4k, travel poster), `Dumpster` (1).
- **Frontage spill** (910 procedural frontages): `SandwichBoard`, `WindowBox`, `HangingBasket` (2k each; one per shopfront would be 3k to 5k instances: place on anchors only), `Marquee_Cinema` (the one cinema).
- **City-life robots**: `RobotWaiter_Play` (café, shops), `RobotButler_Play` (the civic foreground robot, 16k adopted, an easy saving).
- **Ground** (`Kerb_Granite_0/1`, `_Corner`, `_Nose`, `Tile_TramRail`): worth it only when Sky wants kerbs; the road is a flat plane today and the kerb is a 15 cm step you barely see at 38°. `ManholeCover` and `DrainGrate` are 5.4k / 4.1k tris for a 0.9 m disc: slim to a quad with a texture before anyone places them.
- **Not picked**: `TrafficLight` (22.7k tris, and the district's crossings use the floating signal eyes: GDD §15), `Tram` (36.6k, no tram runs in play; needs a slim copy and a mover first), `Scooter` / `HoverScooter` (nothing spawns them), `AirCar_*`, `RocketShip`, `SkyRail_*` (skyline, not play), `Alleys` (no alley in the playable block), `Lockers`, `Barrier`, `BikeRack` (7.6k for a rack), `LookdevBalls`, `Tower_Setback/Stepped/Saucer`, `Hall*`, `DecoPavilion`, `GrandStair`, `PlazaFountain` (the civic block has its own).

## For Latch: which piece for each vehicle and robot in `HostViews.cs`

| drawn today | use | note |
|---|---|---|
| `Box("car/official")` | `Sedan_Official` | keep the 1.8 x 1.4 x 4.6 box as the collider and click target; the mesh is the visual. Pivot is the footprint centre (Y offset +0.08 m); nose is -Y, so yaw it along the car's heading. |
| `Box("car/getaway")` | `Sedan_Getaway` | same |
| `Drum(...)` for `u.Robot` (response units) | `RobotPorter_Play` | keep the unlit `host.coneColour` lamp sphere; the `_Head` / `_Head_Face` parts are the turning head and the face the engine draws |
| the patrol, if it is a robot (`Person("patrol")` today) | `ServiceRobot_Play` | a wheeled rover; no head part |
| dock robots, vending robot (world `civic-robot-dock`, `vending-robot`) | `ServiceRobot_Play` | |
| `foreground-civic-robot` (adopted `RobotButler`) | `RobotButler_Play` | same pivot and parts; 16k to 2.6k |
| city-life robots (café, shops) | `RobotWaiter_Play`; `RobotShopkeeper`, `RobotNanny` (14.6k / 19.7k, uncapped, not in my list) | |
| ambient delivery truck, if wanted | `BoxTruck_Play` | nothing spawns one |

## For Splice

1. Adopt in this order: `Sedan_Official`, `Sedan_Getaway`, the `_Play` robots, then the roofs (they are already candidates with sha256 in `kit.json`: `Roof_*`), then street pieces. The new pieces are `candidate` rows with `root: generated` under `out/decokit-020/`; adopting = `bun run decokit --only <ids>` (writes `out/decokit/`) then flip `status`, then `bun run kit`.
2. `tools/world/gen/roofs.ts` has to name a kit piece per roof programme: `water-sphere` -> `Roof_Tank`, `antenna-mast` -> `Roof_Mast` or `Roof_LatticeMast` (seeded by id), `roof-garden` -> `Roof_Planter` / `Roof_Cabana` / `Roof_Conservatory`, `sun-terrace-pool` -> `Roof_Pool`, `solar-dish` -> `Roof_Dish`, `observation-saucer` -> `Roof_Drum`, `sign-frame` -> `Billboard_Rooftop_*`, `tennis-court` -> `Roof_Tennis`. The kit tags (`use: ['roof']`, scale 0.9 to 1.1) will refuse a roof whose parts are a different size; the programmes' sizes are in the export.
3. Street procedural kinds to map: `planter` -> `Planter_Trough/_Urn`, `tub-palm` -> `PottedFern` (and see item 7), `mailbox` -> `PneumaticPost`, `public-clock` -> `StreetClock`, `tram-shelter` -> `BusShelter_Play` / `TramCanopy_Play`, `newsstand` -> `CurvedKiosk_Play`, `kiosk-food` -> `CoffeeCart_Play`. Also check `phone-booth` (68 procedural although `PhoneBooth` is adopted): same refusal as the palms.
4. `unity_mats.py`, `mats.py` and `kit.py` changed (see the café notes): re-adopting touches no existing material bytes, but run `bun run kit` and the kit tests (9 pass here).

## Follow-ups after adoption (Vela, 2026-10-18)

Renders: `briefs/020/kit/followups-play.png` (each base piece beside its replacement) and `followups-eye.png`. All five are new `candidate` rows in `kit.json` with sha256, built to `out/decokit-020/` by `tools/decokit/street020.py`.

| piece | replaces | size | tris |
|---|---|---|---|
| `FanPalm_Play` | `FanPalm` (4.2k, 189 placed) | same crown (3.0 x 2.8 x 2.5 m), 24 leaves of 6 quads, 4-band trunk | 1.07k |
| `Planter_Urn_Play` | `Planter_Urn` (3.6k) | same size, 10-sided urn, 14 foliage cards | 1.11k |
| `PneumaticPost_Play` | `PneumaticPost` as the mailbox | 0.6 x 0.6 x 1.3 m (the plan's box), rocket-nose pillar, POST plaque, hatch, amber beacon, no stack or return pipe | 1.37k |
| `PhoneBooth_Single` | `PhoneBooth` (2.58 m twin) | 0.91 x 0.91 x 2.5 m, one glazed capsule; the plan's slot is 1.0 x 1.0 with cap 2.7 | 1.68k |
| `Newsstand` | `CurvedKiosk_Play` scaled to 0.43 | 3.2 x 1.9 x 2.8 m with the rack and fin (plan 3.0 x 1.5 x 2.8; the slot's 1.1 x 1.3 allowance is 3.3 x 1.95) | 3.14k |

The two Play copies are rebuilt lean, not decimated: a collapse decimate tears alpha leaf cards. Tags: the Play copies take the base piece's (`tub-palm`, `planter`, `mailbox`), the booth `phone-booth` (sight glazed), the stand `newsstand`. `Newsstand` is a stadium-plan kiosk in the CurvedKiosk's ivory, royal blue, mustard and chrome. `PhoneBooth_Single` faces -Y (the opening), so yaw it to the walk like any booth.

**Determinism.** Two causes, both fixed, three runs of every piece byte-identical (plus a second full run of all 19 pieces: no unstable file):
1. `vehicles_parts.plathe` set a face's material from `{set of BMFaces}`, which iterates by memory address, and the first use of a material fixes its slot: `BoxTruck_Play`, the Sedans and anything with `wheel()` got `DK_Whitewall` / `DK_Steel` in either order. Now iterates in vertex order.
2. The Bevel modifier interpolates UVs in an address-dependent order, so a few dozen UVs differ in the last bit (1.2e-7 on `CoffeeCart`, 20 of 50k), for any piece with `bevel > 0` (`CoffeeCart`, `ServiceRobot_Play`, `BusShelter`, `TramCanopy`, `CurvedKiosk`, `BoxTruck`). `export_kit.baked()` now snaps those pieces' UVs to 1e-3 (as the LODs always were). Pieces with bevel 0 are untouched. Residual risk: a UV within ~1e-7 of a 1e-3 boundary could still flip: about 0.2 % per bevelled piece per build by my count; the three-run checks did not hit it.

**Splice: 8 adopted pieces are stale.** The fixes change their bytes once (same geometry): `Sedan_Official`, `Sedan_Getaway`, `ServiceRobot_Play`, `BoxTruck_Play`, `CoffeeCart_Play`, `BusShelter_Play`, `CurvedKiosk_Play`, `TramCanopy_Play`. `kit.json` still has their old sha256 (I did not touch adopted rows: the kit copy test compares them to `out/decokit`). Re-adopt: `bun run decokit --only <ids>` into `out/decokit`, then `bun run kit`. `python3 -I tools/decokit/proposal020.py --write-kit` prints a STALE line for each. `CurvedKiosk_Play` can retire once `Newsstand` is placed.
