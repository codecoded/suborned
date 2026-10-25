# Brief 055: levels and collision, measured by rule

**Objective:** Sky, 2026-10-23, playing (screenshot: the door guard sunk to the hips in the Office landing): "need to work on the levels - and proper collision detection / hit box geometry throughout." Find every place the game stands, walks or shoots against something different from what is drawn, count them, say why, and put the fix where it belongs as a rule with an owner, with a checker that stops it coming back.
**Owner:** Splice (audit, export, checker) · **Fix owners:** Latch (host), Keel (grid, occluders), Vela (pieces), Vantage (layout), Datum (plan standards) · **Next:** Aster reads this, picks the order below, and hands Latch step 1.
**Done-check (this brief):** `bun run geometry` writes `out/geometry/report.html` + `geometry.json` and `briefs/055/geometry.summary.json`; `bun run geometry --check` exits 0 at the ratchet and 1 above it; `bun run verify` runs it; `bun test tools/geometry` and `bun run typecheck` pass; no Unity editor, no player, no paid call.
**Evidence:** below (commands and results). Report: `out/geometry/report.html` (self-contained, maps and pictures inline); the pictures also in `briefs/055/shots/`.

## 1. Sky's screenshot

It is the door guard at the Office public entrance (`d:Office entrance`, block (64.0, -41.25), plan (-171.25, -294)). The sim puts him at the post; the host stands him at **0.18 m** (the plot slab); the world draws the public landing under him at **0.93 m** (0.75 m above the pavement, six steps up: `contracts/missions/handover/mission.json` says so in prose, "the public landing 0.75 m up six steps"; the `RecordsEntrance` mesh agrees). He stands **0.75 m too low**. `briefs/055/shots/office-entrance.jpg` is the game's camera (yaw 30, pitch 38) on that spot: red figure where the host puts him, green disc on the floor that is drawn.

**Why:** `PlayInstall.Heights` bakes the host's ground (`SiteFrame.heights`, a 1 m grid over the site window) from the plot slabs only (0.18 m) and the drive/bay cuts (0.02 m). No stair, landing, terrace, kerb stone or tree collar is in it. The sim has no height at all (x, y in cm). So any person on anything but the slab is at the wrong height.

It is not the Pave_Slab 0.15 m model (that is already laid on its footprint by `CityLayers.FootLift`; flagged under K-1 for Vela to re-export at 0).

## 2. The counts (civic block site window, block x -52..112 m, y -66..16 m; 0.25 m raster; world export `da8a3528ad`)

| rule | what | count today | with the fix in step 1 |
|---|---|---|---|
| G-1a | mission stand points more than 5 cm off the drawn floor | **1** person (the guard, 0.75 m) | 0 |
| G-1b | mission routes crossing a stretch more than 30 cm off | **10 of 55** (7 reach 0.5 m): guard to the café and back, the four crew to the Office door, four of Inez's walks to the outer tables (the café front, 0.45 to 0.55 m) | 0 |
| G-1c | free walk cells more than 5 cm off the floor | **1101 of 45 749** (275 m²): 1014 are kerb stones and slab edges the 1 m height grid puts on the wrong side (0.16 to 0.19 m, and 0.16 the other way at the service drive and bay); 42 are the Office landing and steps (to 1.05 m at the sill); the rest tree collars, planter beds (to 0.69 m), lamp feet, the café front | 0 |
| G-2a | free walk cells inside a drawn solid | **666** (167 m²), 127 solids | n/a |
| G-2b | blocked cells with nothing drawn within a body | **121**: the fountain's 89, the two stair cheeks of the Office entrance (24, nothing like them is drawn), the pavilion supports and civic robot | n/a |
| G-2c | free cells under less than 1.8 m | **158** (window boxes, phone booths, benches) | n/a |
| G-3a | drawn solids spanning eye height (1.6 m) that are no occluder | **109 objects**, 1110 cells (277 m²): phone booths 8, vending machines 10, police posts 7, palms 29, shopfront doors 7, cypresses, arcade and corner bays | n/a |
| G-3b | occluders whose outline is not drawn along 90 % | **3**: the fountain (0 %: a 3.3 m radius, 4.2 m cylinder over a basin no taller than a step), the civic robot (81 %), plot-04 (84 %) | n/a |
| K-1 | placed kit pieces off their pivot (3 to 30 cm) | **6**: Pave_Slab 0.15, RobotButler(_Play) 0.06, TreePit_Pearl 0.05, ShrubMound and WindowBox -0.04 to -0.06 (32 of 294 catalogued pieces differ from their row) | n/a |

"With the fix": `ifHostReadsWalkFile` in `geometry.summary.json`: the same audit with the host standing people on `out/world/harbour-district.walk.json` gives G-1a/b/c = 0/0/0.

What is measured, in a sentence each (details in the report):
- **Ground (G-1).** For every free walk cell, every named stand point (45 sim places, 83 people places, 35 cast, crew and round stops) and 55 mission routes (A* on the sim grid between the places the mission uses), the floor the host gives against the floor drawn there. Drawn floor = the ground layers (slab, road, kerb strip, bay) plus every kit piece's walkable top, a tread's open riser bridged (0.5 m), reached from open ground by steps of at most 0.35 m, with 1.8 m of head room.
- **Walk (G-2).** The grid is built by `walk-grid.ts` from the building lines, the civic footprints and `civic-block.json`'s dressing solids (beds, tables, fountain, planter runs, pots, bollards, lamp bases) only. **Nothing the world generator puts in the street is in it**: benches, bins, phone booths, vending machines, police posts, service robots, palms, bollards and planter beds are walked through (the 127 solids). The shopfront boxes stand 0.6 m proud of the building line the grid blocks at (7 frontages, 115 cells). The café's drawn front (`CafeBay_v2`, the curve) stands 1 to 1.5 m outside the café footprint the sim blocks (32 cells; its bounding box is 5.9 m off the civic ring).
- **Sight and fire (G-3).** The engine asks one thing: does an occluder span 1.6 m. Its occluders are the buildings, the plots behind their lines and **four** cover blockers (atom fountain, civic robot, two pavilion supports). Planter runs (1.1 m), cabinets, lamps, kiosks, phone booths, vending machines, police posts, palm trunks and the cars (1.45 m) stop no line; cover points only lower a hit chance (planter runs 30, walls 28, cars 14, fountain 11, cabinets 9, piers 8). At chest height (1.2 m, where the fight lab draws its lines) the list grows by the planter runs, window boxes and terrace pieces (1431 cells). The café and Office are walled on 100 % of their outline, so those hold.
- **Levels.** The sim has no level: positions are x, y; occluders z and h from the pavement; one grid layer. The host knows two heights (0.18, 0.02). The world has three levels (street 0, deck 9.43 m, sky concourse 24 m; 20 decks, 11 concourse decks, 113 connectors) and **none is inside the window**, so no Handover person can be on a deck today and nothing breaks there; the first mission on a deck breaks every one of the rules above. The street level is also two datums: the carriageway 0 and the walking slab 0.18.

## 3. Rules and owners (rules in `tools/geometry/rules.ts`, counts held in `contracts/world/geometry.ratchet.json`, target 0)

| id | rule | source | failing example (today) | owner |
|---|---|---|---|---|
| G-1a | every stand point of a person the mission places is within 5 cm of the floor drawn there | PlayInstall.Heights; GDD §10 exteriors | cast:guard at the Office landing, host 0.18 m, drawn 0.93 m | Latch (host reads the floor), Splice (export) |
| G-1b | no mission route crosses a stretch more than 30 cm off | the routes on the sim grid | guard: Office entrance to Café door, 7 cells, 0.75 m | Latch, Splice |
| G-1c | every free walk cell within 5 cm of the floor drawn there | grid vs export | office-public-entrance 42 cells, worst 1.05 m | Splice, Latch |
| G-2a | no free walk cell centre inside a drawn solid a walker cannot step onto | walk-grid.ts inputs | 8 phone booths, 10 vending machines, 7 police posts, 4 benches, 2 round benches | Keel (grid input), Splice (solids export), Vantage |
| G-2b | a sim solid has something drawn within a body of it | civic-block.json dressing vs kit meshes | atom-fountain 89 cells, RecordsEntrance cheeks 24 | Vantage, Vela |
| G-2c | 1.8 m of head room over every free cell | drawn awnings, boxes, benches | window boxes 48 cells | Vela, Vantage |
| G-3a | every drawn solid spanning eye height is an occluder | rulebook perception.eye; Site.cs | phone booths 8 (46 m²), police posts 7 (28 m²) | Keel, Splice |
| G-3b | every occluder outline is drawn along 90 % | GameSight / Occluder.Blocks | fountain 0 % | Keel, Vela |
| K-1 | a placed kit piece stands on its pivot within 3 cm and is its catalogued size | contracts/world/kit.json vs the shipped FBX | Pave_Slab, mesh at 0.15 m | Vela |

Not yet a rule (needs a decision): **L-1** every stand, route and occluder names its level (street, deck, concourse) and the host has a height raster per level (Datum + Keel, when the first deck mission is briefed); **S-1** a seated person's height is the chair seat's (the 24 café seat points lie inside the tables' solids by design; the host plays the seated clip at the pavement height).

## 4. The fixes, in the order that pays

1. **Latch, host: stand people on the drawn floor (closes G-1a/b/c; my estimate half a day).** `bun run geometry` writes `out/world/harbour-district.walk.json` (`suborned.walk/1`, 861 KB: block frame, window x -52..112, y -66..16, 0.25 m cells, 656 x 328; `floor` = int16 LE centimetres base64, index `i * ny + j`, i along block x (south), j along y (east); `kind` = uint8 base64: 0 open, 1 raised floor, 2 solid, 3 low ceiling). In `PlayInstall.Heights` read it instead of baking slabs, and make `SiteFrame` 0.25 m (`cell`, `nx`, `ny`; `Height()` and `Ground()` keep their form). One caution: a figure walking up the stair then steps 0.1 m per cell; smooth `y` over 0.1 s (it already eases the figure's yaw). Verify with the guard shot (`briefs/055/shots/office-entrance.jpg` is what it should look like) and `bun run geometry --check` after switching `loadSim`'s host to the walk file (the port of `PlayInstall.Heights` in `tools/geometry/sim.ts`; I will flip it once Latch has landed it and re-ratchet). The world generator does not change; the file is made from the kit meshes, so it needs Blender (cached by mesh hash; 8 s cold).
2. **Keel + Splice, walk grid from the drawn solids (G-2a, 2b; my estimate a day, re-records every replay).** `walk-grid.ts` takes its solids from the walk file's `kind` 2 and 3 (with the 0.65 m body clearance) in place of the dressing polygons, plus the building line moved to the outer face of the shopfront and facade pieces. Everything that walks changes, so `missions:rerecord` and a Suture pass follow; do it once, after step 1 and after Vantage's layout changes below, so the grid moves a single time. Until then the ratchet holds the 666.
3. **Keel + Splice, occluders from the drawn solids (G-3a, 3b; my estimate half a day).** I add `solids` to the export: for each placed element the footprint at eye height (1.6 m) and at chest height (1.2 m), with its height; Keel's `tools/sim/site.ts` writes occluders from those (a thin column, not a disc, for the fountain) and keeps cover points as cover. This is a design call for Motive/Pulse first: whether a shot at 1.2 m is stopped by a planter run (it is drawn to stop it) or the low cover only lowers the hit chance as the fight rules say; the eye-height class is not in doubt. The audit reports both.
4. **Vela, pieces (K-1).** Re-export the flat paving at 0 (Pave_Slab, Pave_Flags_0..3, InlayArc_R6/R10/R16, PaveInlay_Band_6m, PaveInlay_Ring_Q: modelled at z 0.15, catalogue height 0), the robot feet (0.06), ShrubMound, TreePit, WindowBox. The RecordsEntrance stair reads to the voxeliser as thin treads with open risers (`0.15-0.25, 0.4-0.5, 0.65-0.75` in every column of the landing): fine to walk and for this audit, but if it is meant as a solid (stringers, risers, an underside), close the mesh so it can collide. `CafeBay_v2`'s front stands 1 to 1.5 m outside the café ring, and the atom fountain's basin is no taller than a step where the sim keeps a 3.3 m disc 4.2 m high: state the footprint and height the piece actually has.
5. **Vantage, layouts (G-2).** The shopfront boxes 0.6 m proud of the building line (7 frontages); the stair cheeks `civic-block.json` keeps at the Office entrance (nothing drawn); benches, phone booths, vending machines, police posts and service robots that stand on the walk (127 solids): they stay and become solids (step 2), or move off it.
6. **Datum, plan standards.** (a) One walking datum: `standards.levels` states the pavement (0.18) beside the carriageway datum (0), and every named stand point states the floor it stands on (`d:Office entrance`: landing 0.75), so the rule G-1a has a plan value to hold the drawn one to and mission.json's prose becomes data. (b) Landings, step runs and terraces a person stands on are declared in the civic block (height, rise, tread), not found in the kit. (c) Levels: `level` on every mission point and people place (default street), ready for L-1. The threshold values I chose are starting values [S]: step 0.35 m, bridge 0.5 m, head 1.8 m, body 0.3 m, eye 1.6 m (rulebook), chest 1.2 m.
7. **Splice (me), next.** Vectorise the solids for step 3; a `kit.json` `collide` block (footprint and floor height per piece, measured, sha-keyed) so Vela's pieces carry their own collision and the grid never has to be told; put `bun run geometry` in the batch's pre-flight once the first fix lands.

## 5. What I built (all new; nothing existing edited except `tools/verify.ts` and `package.json`)

- `tools/geometry/` (`bun run geometry`): `cli.ts`, `pieces.py` + `pieces.ts` (Blender voxelises every kit piece the window uses to 0.125 m, folds to 0.25 m columns of height intervals, caches by the FBX's hash under `out/scratch/splice/geometry/`), `bounds.py` (mesh bounds of all 294 catalogued pieces, 13 s), `visible.ts` (the drawn world: ground layers as `Layers.Ground` makes them, kit placement as `KitPlacer`/`FacadeKit.Civic`/`CityLayers.Street` do it, masses, buildings, frontage boxes), `raster.ts` (a column's floor, roof, steps and reach), `sim.ts` (the walk grid, places, people, occluders, and a port of `PlayInstall.Heights`), `audit.ts` (G-1..G-3 and the routes), `rules.ts`, `walk-export.ts`, `heat.ts` (top-down maps), `report.ts`, `shots.ts` + `scene.py` (Blender clay renders at the game's camera, orthographic, pitch 38, yaw 30 or 210), `geometry.test.ts` (6 tests).
- `contracts/world/geometry.ratchet.json`: the counts each rule may not exceed (written by `bun run geometry --ratchet`; fixes lower it).
- `bun run verify` now runs `geometry --check` (skipped with a line when the world export, Blender or the ratchet is missing).
- Outputs: `out/geometry/report.html`, `out/geometry/geometry.json`, `out/geometry/maps/*.png`, `out/geometry/shots/*.jpg` (mirrored to `briefs/055/shots/`), `out/world/harbour-district.walk.json`; committed: `briefs/055/geometry.summary.json`.
- Commands: `bun run geometry` (about 6 s warm, 15 s cold, mostly Blender), `bun run geometry --no-shots`, `bun run geometry --check`, `bun run geometry --ratchet`.

## 6. Open issues and what the audit does not claim

- The drawn world is the shipped kit meshes plus the export's masses and boxes; procedural facades (`skins`), roofs and the Unity-side stand-ins that have no kit piece are boxes or absent. It is a model of the Unity scene, checked against the pictures and against `mission.json`'s own "0.75 m up six steps", not read from the scene. Latch's first screenshot after step 1 is the real test.
- Voxels are 0.125 m across: a rail or pole thinner than that is kept as a voxel-wide post (never lost), so thin things read a little thick.
- Lines are not traced: G-3 compares masks (what blocks at 1.6 m and 1.2 m) with the occluders; it does not fire rays. People as blockers (`crowd.occlude`) and the cars (dynamic) are not in the raster.
- Only the civic block's window is audited (the only place the sim and the host have people). The rest of the district has no sim, so no stand points to check; the decks and concourse are outside it.
- The seat points (24) are reported, not counted (S-1).
- The Blender cold run needs Blender 5.x at `/Applications/Blender.app` (or `BLENDER=`); a machine without it skips the verify step and says so.
- Nothing heavy ran: Blender for 8 s a time; "Sky is playing" was checked before the first run (nobody) and `bun run verify` (27 s) and `bun run test` ran with the game closed.

## 7. Camera mount height and sightlines in 3D (Motive, 2026-10-24; for Keel's rulebook)

**Why.** Keel's drawn occluders (198 solids from `kit.collide.json`) break the Handover because every watcher sees from one height, `eye-cm` 160, above one floor. A camera on a pole then sees from 1.6 m like a person, so the two 2 m topiaries at the Office door (`top-office-e`, `top-office-w`) cut the entrance camera's line to the door, and the guard on the 0.75 m landing is treated as standing on the pavement. The rule below gives every watcher its own eye height above its own floor. A line is tested in 3D from that eye to the target's head and chest.

**Rule `perception.mount` (proposed; numbers [S], measured from the drawn pieces).**
1. **Eye.** A watcher's eye is the floor under it (`grid.floor`, block z cm) plus the height for its kind. A camera's eye is its lens, the centre of the drawn glass, not the top of the pole.

| kind | eye above its floor | read from |
|---|---|---|
| person (the guard, staff, passers-by, police, rivals, the cell) | 160 (`eye-cm`, unchanged) | rulebook; the guard at his post stands on the landing: 75 + 160 = **235** |
| pole camera (the mission's `entrance` and `pickup`; the world's `pole-camera`) | **479** | `CCTVPole` glass z 2.642..2.900 m, centre 2.771, at the world's height scale 1.728 (plan `watchers.types.pole-camera.mount` 5.5 m ÷ piece 3.182 m) |
| mast camera (`mast-314`, the circus masts) | **768** | the Unity stand-in head (`kit.json` mast-camera, 0.94..0.98 × the 8 m mast); there is no kit piece |
| canopy camera (`DomeCamera`) | 300 [S], open | none in the Handover. The world places them on the concourse floor (z 24) with no mount, while the plan says 3.2 m under the canopy. That placement is for Splice and Vantage to fix. |
| patrol robot | **95** | `PatrolRobot` face (`M_RobotFace`) z 0.80..1.09 m |
| the response's robots | **128** | the host's drum lamp (`HostViews.Drum`, 1.28 m) until Vela gives them a piece. The plan says civic-robot eye 1.7 and `RobotButler_Play`'s face is at 1.54, so Datum should align the plan with whatever piece is chosen. |

2. **Target.** A person is seen when the line from the eye to **their head (floor + 160) or their chest (floor + 120)** is clear; either one is enough. In plain words: "if it can see your head or your chest, it sees you". For an occluder standing on the ground, the head line decides; the chest line only matters under an overhang. A camera as a target (Watch, shooting it out) is its lens point.
3. **Line test.** An occluder (`z`, `h`, footprint) blocks a line when the line passes through the prism z..z+h over the footprint. To test it, clip the 2D segment to the footprint. On each stretch inside, the line's height is linear, so compare the stretch's two end heights with z..z+h. Keep it in integers: crossings are rationals num/den and heights are compared by cross-multiplying. A line that only touches the outline (a corner) is blocked if its height at the touch is within z..z+h, as the 2D test has it; scenario N41's "behind a solid" spots are corner grazes. A watcher's own piece never blocks its own lines.
4. **People in the way.** A person on the street blocks a line that passes within `crowd-occlude-cm` (30) of them in plan, but only where the line is below their head top (their floor + 175 [S], the "head height above the eye" `crowd-occlude-cm`'s reason already gives). A mast sees over a crowd; a person's 1.6 m line does not.
5. **The fan.** The drawn cone (`ConeFan`) uses the same test. Seen from an eye at height L above a target's floor, a ground occluder of top h < L at distance d hides a head only out to d·(L − 160)/(L − h); past that the target is seen. Example: the topiary 7.9 m from the entrance lens (L 479, h 200) hides 1.1 m behind it, and the door at 11.6 m is clear.
6. **Fire lines.** Fire lines read the engine's `Sees` and stay on the same line as sight, from eye to head or chest above each floor. Whether the 1.1 m planters stop a chest-high shot is still step 3's open question for Motive and Pulse; this rule does not decide it.

**Robots and the guard.** Yes, both need it, and the measurements show it. The guard's eye is his feet plus 1.6 m (2.35 m on the landing). Anyone looking at him sees his head at 2.35 m and his chest at 1.95 m. That clears the 2 m topiaries and fixes N68 (the unit confirms the dead guard on time) and route G (the Scout's Watch of the guard needs no walk-in). The patrol robot sees from 0.95 m, so a 1.1 m planter close to it hides you from it; nothing measured moves on that. **Bodies:** a body lying on the ground counts as a standing person (head and chest) for now, so no find time moves. A lying body's own height is my next call, with the takedown rules, because it changes every find time. Civilians also find a body within 1.5 m whatever their facing (brief 054 §9.1, after engine 0.28.0's cone left the dead guard in the doorway unfound in N68); that line test reads the same body height.

**What it moves** (275 scenarios and the Handover mission check, each variant through `bun run guard`, about 25 s a pass; evidence `briefs/055/motive-mount-sight.json`; prototype `out/scratch/motive/b055/sight3d-prototype.patch`, which uses doubles where the engine needs integers, plus debug traces to drop):

| drawn occluders | sight | scenarios failing | of which throw | mission check |
|---|---|---|---|---|
| off | today | 0 | 0 | SHIPS |
| off | the rule | 0 (7 logs shift by 0.2–6 s) | 0 | SHIPS, no script moves |
| on | today | 19 + DP1 | N49 N69 N69b N74 N98 | HOLDS: MC-0 29/38, 12 scripts move |
| on | cameras only from the lens (479 or 325: identical) | 7 + DP1 | N49 N98 | HOLDS: MC-0 33/38; the route-G family pays +4 to +12 campaign Trail |
| on | **the rule** | **6 + DP1** | N49 N98 | HOLDS: MC-0 33/38 (the five loud scripts only) |

- **The rule alone moves nothing that is checked.** The 7 log shifts happen because people standing in the carriageway are 18 cm below the pavement datum, so a 1.6 m line passes over them; for the same reason, people no longer hide the mast. It can land before the occluders do.
- **Fixed by the lens height** (the entrance camera sees the door over the topiaries again): S01 (covers the door), S26 and N30 (live again after the loop), N66 (records the kill, Alert 3), N67 and N72 (sees the plaza, stays live), N20e (a run only a camera sees), N115 and N128 (the camera's report starts the seal clock), and N69, N69b and N74 (takedown cases that read the camera's report; they threw). Mount 4.79 m and 3.25 m give the same results. I chose 4.79 so that every pole camera looks the same and sees the same. **Latch** should draw the mission's two poles with `CCTVPole` at the plan's 5.5 m mount, in place of the 3.2 m stand-in.
- **Fixed by eyes above the floor:** N68 (the unit sees the dead guard on the landing over the topiaries; it was 2.5 s late) and the route-G family in the check (the Scout watches the guard on the landing from where she stands).
- **Still failing under the rule, each for a drawn solid that really stands there, none of them a camera:**
  - N25 and S13: the 2 m topiary `top-office-w` stands between the scenario's `BesideDoor` (69.25, -42.5) and the guard's post. The Enforcer's fire order walks in instead of firing, so the patrol never sees the draw in S13 and the guard wins in N25. **Keel:** move `BesideDoor`, as you did for `cyp-office-w`.
  - N101 and N98 (N98 throws when no shot lands): the shot from `ShotCorner` (43, -26) at the patrol sweeping before the door is cut by `cyp-office-e`, `cyp-facade-w` (6.2 m), the public entrance pier (7.5 m) and a topiary. **Keel:** move `ShotCorner`.
  - N49 (throws: no camera report): guard 29 at the official door stands behind the official entrance pier `office-official-entrance#0` (0.5 × 0.7 m, 5.65 m tall) as seen from the pickup camera (74, -40). The pier blocks at any lens height. **Keel** can move the takedown spot, or **Waymark/Vantage** the camera. **Vela:** confirm that the pier is a 5.65 m solid.
  - N84: guard 73 finds the knocked-out guard on the landing 3 s late (5:05.1 against 5:02.1; 8.5 s late with cameras alone) because the door planting cuts his first look. **Keel:** re-pin the time once the rule lands.
  - DP1: a fixture recorded on the old inputs; re-record (`suborned-sim drive-session`) with any input change.
  - Mission check, the five loud scripts (`loud`, `loud-fight`, `loud-fast`, `loud-left-downed`, `loud-left-standing`) fail with enforcer-down. At 2:31 the fire order steps the Enforcer 0.56 m north off `BesideDoor` to get a line past the drawn planting. The scripted Enter at 2:37.4 then finds him 0.5 s short of the door, and the police take him. MC-2, MC-6 and MC-10 fail only because no loud win is left. **Waymark/Keel:** re-compose the loud scripts (`missions:check compose`) after the rule.

**Hand-off to Keel.** *Inputs:* this section; the evidence JSON; the prototype patch; the probes in `out/scratch/motive/b055/tools/` (`los.py` names the occluders that cut a 3D line, `run.sh`/`all.sh` run the scenario variants, `mcheck.ts` runs the mission check on a scratch root, `lens.py`/`mats.py` take measurements in Blender). *Do:* write `perception.mount` into the rulebook with the values above (kinds and lens heights, a chest height of 120, a head top of 175). Give the site every camera's kind: mission.json `cameras[].kind` "pole-camera" (Waymark), and the world watchers already carry theirs. Make sight read the floor under both ends. Give the line test its integer 3D form, with the fan built the same way. Land it with the occluders still off. Then move `BesideDoor` and `ShotCorner`, settle N49's spot, re-pin N84 and re-record DP1. After that, turn the occluders on and re-compose the loud scripts with Waymark. *Done-check:* with occluders off, 275 scenarios pass and `bun run missions:check` SHIPS with no script outcome moved. With occluders on and the rule in place, the only failures left are the six named above until their fixes land, then none. *Next owner:* Keel, then Waymark (camera kinds, the loud re-compose), Latch (draw the mission poles as `CCTVPole` at 5.5 m), Splice (a geometry rule that holds each rulebook lens height within 10 cm of the drawn glass, so the two cannot drift), Datum (the civic-robot eye in the plan).

## Aster's fix order (2026-10-25)

What has landed since §4 (from `contracts/world/geometry.ratchet.json` and the code):
- the host's walk-floor code (`PlayInstall.WalkFloor`, step 1);
- walk solids (G-2a 645 → 0);
- K-1 at 0;
- the fountain's sim height (G-3b 3 → 2);
- drawn occluders, plus §7's `perception.mount` in the rulebook (engine 0.28–0.30).

Still counted: G-1a 1, G-1b 6, G-1c 709, G-2b 122, G-2c 2, G-3a 61, G-3b 2.

The order that pays from here:
1. **Latch, batch 0.2.118 item G (step 1 close-out).** Prove the walk-floor host in the build:
   - the log line says the ground comes from the walk file, not the slab bake;
   - the door guard stands on the Office landing, as in `briefs/055/shots/office-entrance.jpg`;
   - steps ease.
2. **Splice, right after that build.** Flip `loadSim`'s host in `tools/geometry/sim.ts` to the walk file and re-ratchet G-1a, G-1b and G-1c. The target is 0, as `ifHostReadsWalkFile` predicts. Then put `bun run geometry --check` in the batch's pre-flight.
3. **Motive, then Keel and Splice (G-3a, 61 objects).** Phone booths, vending machines, police posts, palms and shopfront doors span eye height but don't block sight.
   - Motive first rules §4 step 3's open question: does a 1.1 m planter stop a chest-high shot, or only lower the hit chance?
   - Then Splice exports their eye- and chest-height footprints and Keel's `site.ts` makes them occluders. Thin pieces stay out, by the 0.6 m rule (Sky: liberties for performance).
   - It moves every replay, so it runs once, with Waymark's re-record.
4. **Vantage, with Vela (G-2b 122, G-2c 2).**
   - The atom fountain's 89 cells and the Records Entrance cheeks' 24: either the sim solid follows the drawn piece, or the piece gets what the sim keeps.
   - The pavilion supports and civic robot (9).
   - The two low-headroom cells.
5. **Datum (§4 step 6).**
   - One walking datum in `standards.levels` (pavement 0.18 beside carriageway 0).
   - Landings and step runs declared in the civic block (height, rise, tread).
   - `level` on every mission point and people place, ready for L-1 when the first deck mission is briefed. That can wait until then.

L-1 and S-1 stay unwritten until a deck mission or a seat rule needs them.
