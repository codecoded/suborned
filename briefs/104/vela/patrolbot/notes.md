# Brief 104 · PatrolBot104, the ball-balancing patrol robot, modelled (Vela, 2026-11-10)

Sky picked the design and said "model it". It is modelled from the four committed sheets in `briefs/104/concepts/` (turnaround, details, chest-sidearm, pick).

- **Generator:** `tools/decokit/patrolbot104.py` (the piece and its parts) + `tools/decokit/patrolbot104_surf.py` (the egg surface and the raised-plate helpers). It is registered in `kit.FAMILIES`.
- **Export:** `bun run decokit --only PatrolBot104` → `out/decokit/Models/Robots/DK_PatrolBot104.fbx` (+ `_L1` 2,139 tris, `_L2` 508 tris), plus its row in `out/decokit/decokit_manifest.json`.
- **Not run (Aster's step, after Sky's look):** `bun run kit`, kit.json, collide, sim:site.
- **Renders** (git-ignored, on disk): `turnaround.png`, `three-quarter.png`, `hatch-open.png`, `beacon.png`, `play-zoom.png`, `face-atlas.png`; single tiles are in `tiles/` (also `hatch_front.png`, `hatch_side.png`, `play_person.png`). They are rendered from the **exported FBX**, so the poses use the pivots the engine will get (`tiles/pivots.json`).
- **To redo the renders:** `python3 -I make/faces.py DIR`, then `Blender -b -P make/render.py -- --out tiles --faces DIR`, then `python3 -I make/compose.py tiles .`

## Round 2 (Sky, 2026-11-10): beacon, lighter ball, the hatch as a mechanism
Sky approved the model and asked for three changes.

1. **Siren beacon instead of the antenna** ("I would put a siren light on his head instead of antenna"). The halo ring and its post are gone. A 1950s police beacon now sits on the crown's chrome bell: a stepped chrome base, a red glass dome (0.152 m across, about a quarter of the head's width), a four-bar chrome cage and a chrome knob. Inside it is a curved mirror and a bulb on their own part, `BeaconLamp`, which spins about the vertical axis. `beacon.png` shows it unlit and lit.
   - **Material:** one shared row, `DK_PatrolBot104_Beacon` (`emit` kind, #C4231A, emission 0 as built). It covers the dome and the bulb, and the game drives its emission when the alert rises. This is the same pattern as the hover camera's `DK_Eye_Lens`. The lit renders set it to (1.0, 0.012, 0) × 0.9 and add a small red point light in the dome, standing in for a light the game would put there. Under AgX the lit dome drifts toward salmon; Glint should judge the lit red in Unity with bloom.
   - **The spinning reflector only shows through a see-through dome.** The kit's `emit` kind is opaque, so in Unity the mirror is hidden unless Latch gives the dome a transparent glass. It costs 72 tris. Until then, the spin cue has to come from the game: an emission sweep on the dome, or a turning light.
2. **The ball is lighter.** The halves are now `DK_Gunmetal_Warm` (new row, `brass` kind, #6F675D): a warm satin gunmetal with a diffuse share, so it keeps its colour in shade and doesn't wash to pale grey as the nickel did. `play-zoom.png` shows both options at play zoom, beacons lit, beside the 1.8 m figure: on the left the cream-halves variant (halves cream, band gunmetal, a render-only swap), on the right the kit's gunmetal halves. **Pick: gunmetal halves.** The cream ball merges with the cream tip of the body, while the gunmetal one reads as a metal ball with a cream band, as on the details sheet. It no longer reads as a black bowling ball.
3. **The open hatch is a mechanism.**
   - The panel is now a 25 mm slab (9 mm proud of the skin, 16 mm under it).
   - Its inner face has a gunmetal liner with two nickel stiffeners, so when it is open you see the panel's inside.
   - Two barrel hinges sit on the hinge line: the panel's two knuckles turn between three fixed knuckles on the Body.
   - The well walls are gunmetal, no longer black iron. It has a chrome lip round the opening, two nickel ribs on the back plate, each with an amber lamp (`DK_Glow_Amber`, the existing row), and the nickel rail on an iron rear block and front stop.
   - The lamps sit low (z 0.552) so the open panel doesn't hide them from above. The gadget is unchanged, apart from fewer segments on its barrel.
   - Close-ups: `hatch-open.png`, plus `tiles/hatch_front.png` and `tiles/hatch_side.png`.
- **Cuts to pay for it:** the ball 24 → 20 segments (its outline shows a few facets in the 400 px/m side views; nothing at play zoom), the sidearm's barrel 12 → 10, the face frame 40 → 36 points round, the chrome lip every other rim point (corners kept), and the crown's bell cut off under the beacon's foot.
- **Old patrol robot unchanged:** I rebuilt `PatrolRobot` into `out/scratch/vela/oldbot`. Its geometry, UVs, materials and placement hash the same as `out/decokit` for LOD0, L1 and L2 (12,522 tris). Only 45 bytes of FBX timestamp differ. No shared code changed this round.

## Size
- 1.271 m to the top of the beacon's knob (was 1.296 to the halo's ball), 0.888 m across the hands, 0.48 m deep.
- It fits the dock bay (0.98 × 1.0 × 2.14 m).
- The body is 0.56 m wide, or 0.66 m across the side vents. The ball is 0.40 m.

**At play zoom** (`play-zoom.png`), the robot is about 57 px tall and 41 px wide, and the beacon's dome about 8 px across. The 1.8 m figure is about 85 px tall. The face glass is about 22 px wide, and the eyes and smile still read. Next to a person it looks like a small robot standing about shoulder high, which I think is right for a cute patrol unit. I didn't change the height.

## Triangles: 8,608 at LOD0 (was 7,988)

| Object | Tris | Pivot, Blender (x, y, z) | Pivot, kit/Unity (x, y, z) | Moves |
|---|---|---|---|---|
| `DK_PatrolBot104` (**Body**) | 4,262 | (0, 0, 0.40) | (0, 0.40, 0) | leans and turns about the ball contact; every part below except the Ball follows it |
| `_Ball` | 904 | (0, 0, 0.20) | (0, 0.20, 0) | rolls about its centre; its axle runs along x (hubs at the sides), cream band round its equator |
| `_ChestPanel` | 784 | (0, -0.2131, 0.6657) | (0, 0.6657, 0.2131) | hinged along its top edge, on the hinge barrels' axis (axis = x); open = bottom swings forward and up, about 150° (the shots) |
| `_Gadget` | 452 | (0, -0.045, 0.541) | (0, 0.541, 0.045) | slides forward along its rail; out = 0.11 m forward (kit +z) |
| `_ArmL` / `_ArmR` | 212 each | (±0.247, 0, 0.70) | (∓0.247, 0.70, 0) | turn at the shoulder. L = the robot's own left (Blender +x, kit −x) |
| `_ForearmL` / `_ForearmR` | 388 each | (±0.339, -0.030, 0.613) | (∓0.339, 0.613, 0.030) | the hand, turning about the elbow ball's centre; a shoulder turn must carry it too |
| `_Beacon` | 710 | (0, 0, 1.105) | (0, 1.105, 0) | the siren's base, dome, cage and knob; stays put (it can bob); the game lights its glass |
| `_BeaconLamp` | 72 | (0, 0, 1.178) | (0, 1.178, 0) | the mirror and bulb inside the dome; spins about kit y |
| `_Screen` | 224 | (0, -0.237, 0.91) | (0, 0.91, 0.237) | the face glass. UVs run 0..1 across the visible glass: u toward the robot's left as you face it, v up |

The parts export as siblings, the same as every kit piece. Latch puts them under the Body in the prefab, keeping their world placement, with the Ball left outside it.

**Checked in Blender:** `three_open` and `hatch-open.png` re-import the FBX and pose it. The ball is rolled 40°, the hatch opened 150°, the sidearm slid 0.11 m, the right arm raised 55° with its hand bent 25°, and the whole body leaned 6° about the contact point. No gaps show at the hinge, the opening's rim, the shoulders, the elbows or the contact.

**Not checked in Unity:** that each part imports with identity rotation and its local position at the pivot. Blender's FBX re-import adds an axis turn to each object, which is why the render script poses in world axes.

## Materials: 8, all shared
No per-robot property block is needed.

| Material | Use |
|---|---|
| `DK_RobotEnamel_Sage` (`DK_Enamel_Sage` #7E8C54) | body green, the hatch. It is the robot variant, so it carries the shared grain/patina detail pair (`robot_tex.py`) like the other robots |
| `DK_RobotEnamel_Cream` (existing, #F3DDB0) | face frame, side panels, tip, slats, arms, hands, ball band, sidearm body |
| `DK_Nickel_Satin` (existing, civic_common) | all bright metal: vents, crown bell, the beacon's base, cage and knob, its mirror, ball hubs, shoulder collars, elbows, barrel, the hinges, the well's lip, ribs, lamp bezels and rail, the hatch's stiffeners. I used it instead of `DK_Chrome` because brief 103 found mirror chrome reads as pale-blue glass under the zone's one probe |
| `DK_Iron` (existing) | the dark metal: the sidearm's shoe and cowl, the vents' backs, the rail's rear block and front stop |
| `DK_Gunmetal_Warm` (**new**, `brass` kind, #6F675D) | the ball's halves, the well, the hatch's inner liner |
| `DK_Glow_Amber` (existing, robot_parts.py) | the well's two lamps |
| `DK_PatrolBot104_Beacon` (**new**, `emit` kind, #C4231A, emission 0) | the beacon's glass dome and bulb; the game drives its emission |
| `DK_PatrolBot104_Screen` (**new**, enamel #191613) | the blank dark glass, which is also the "off" face |

## Proposal: choosing the six expressions
The six faces are drawn from the details sheet as a 34 × 18 amber dot grid: calm, curious, alert, cross, dazed, off (`face-atlas.png`). They are render-only for now. Recommended route for Latch:

1. **One material per expression:** `DK_PatrolBot104_Face_Calm`, `_Curious`, `_Alert`, `_Cross`, `_Dazed` and `_Off` (`_Off` is the blank Screen material above).
2. **One shader and one atlas texture:** a 3 × 2 atlas, with each material holding its own cell offset as a material property. All six are the same shader variant, so the SRP Batcher keeps them together. GPU Resident Drawer instances robots that wear the same face: in a crowd of patrol robots the faces form at most six instance groups.
3. **Swap with `renderer.sharedMaterials`** on the Screen's slot when the expression changes. This is the one change from `RobotFace.cs` today: that script sets the face through a MaterialPropertyBlock, which drops the renderer out of the GPU Resident Drawer (briefs/064/research/mpb-batching.md).
4. **Blinks and pulses:** if they're wanted, the shader can animate on `_Time` from a per-material speed, again with no property block.

## Where it differs from the sheets, and why
- **Arms:** they angle down 42° and forward 18°, against the front view's ~30° straight out. The sheet's span is 0.97 m; this keeps the hands inside 0.9 m. The side view on the same sheet already shows them lower.
- **Ball orientation:** the sheets draw the ball three ways. I took the details sheet's: the band round the equator, a hub on each pole, and the axle across the robot. So the front and back views show the band as a vertical stripe, and the side views show the hub face-on.
- **Ball halves:** warm satin gunmetal (round 2; round 1's `DK_Iron` read as a bowling ball), not mirror chrome. Nickel washed them out to pale grey, and true chrome in Unity would reflect the zone probe and look pale. The hubs stay nickel.
- **Sidearm body:** cream, with a dark shoe and cowl and a nickel finned barrel. The brief said dark metal, but a dark body vanished in the dark well. The details sheet shows it cream.
- **Hatch angle:** it opens about 150°, steeply up in front of the chin, as on the details sheet. At 105–130° it stood over the opening and hid the sidearm from any camera above the robot.
- **Beacon:** Sky's change on the sheets' halo antenna (round 2). It follows the beacons of the P5 and P6 lineups (`paid/104-patrol-robot/codex/`): a chrome base, a domed lens and a chrome knob, with a cage added for the 1950s police lamp.
- **Small cuts for the budget:** no seam lines on the front and back centre lines, and no screws on the ball band. The ball hubs are slightly faceted stadiums (8 × 2 quads), which don't show at play zoom.
- **Green:** it is my reading of the sheets' sage. It shifts a little bluer than the paintings under the studio light. Glint can judge it in Unity.

## Shared-code changes (small, generic; other pieces unchanged)
- **`geo.Shape.part(name, pivot=)` and `Shape.pivot`:** a part, or the root, can stand at its own origin. The mesh is stored about the pivot; UVs are projected before the move, so robot detail still runs by height.
- **`export_kit.baked`:** carries an object's world matrix when it isn't identity. Every existing piece is at identity, so its FBX bytes are unchanged.
- **`kit.FAMILIES`:** + `patrolbot104`.
- **Caveat:** `kit.asset(floor=...)` edits vertices in local space, so don't combine it with pivoted parts. This piece doesn't use it.
