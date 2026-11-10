# Brief 104 · PatrolBot104, the ball-balancing patrol robot, modelled (Vela, 2026-11-10)

Sky picked the design and said "model it". It is modelled from the four committed sheets in `briefs/104/concepts/` (turnaround, details, chest-sidearm, pick).

- **Generator:** `tools/decokit/patrolbot104.py` (the piece and its parts) + `tools/decokit/patrolbot104_surf.py` (the egg surface and the raised-plate helpers). It is registered in `kit.FAMILIES`.
- **Export:** `bun run decokit --only PatrolBot104` → `out/decokit/Models/Robots/DK_PatrolBot104.fbx` (+ `_L1` 1,986 tris, `_L2` 471 tris), plus its row in `out/decokit/decokit_manifest.json`.
- **Not run (Aster's step, after Sky's look):** `bun run kit`, kit.json, collide, sim:site.
- **Renders** (git-ignored, on disk): `turnaround.png`, `three-quarter.png`, `hatch-open.png`, `play-zoom.png`, `face-atlas.png`; single tiles are in `tiles/`. They are rendered from the **exported FBX**, so the poses use the pivots the engine will get (`tiles/pivots.json`).
- **To redo the renders:** `python3 -I make/faces.py DIR`, then `Blender -b -P make/render.py -- --out tiles --faces DIR`, then `python3 -I make/compose.py tiles .`

## Size
- 1.296 m to the top of the halo's ball, 0.888 m across the hands, 0.48 m deep.
- It fits the dock bay (0.98 × 1.0 × 2.14 m).
- The body is 0.56 m wide, or 0.66 m across the side vents. The ball is 0.40 m.

**At play zoom** (`play-zoom.png`), the robot is about 58 px tall and 41 px wide. The 1.8 m figure is about 85 px tall. The face glass is about 22 px wide, and the eyes and smile still read. Next to a person it looks like a small robot standing about shoulder high, which I think is right for a cute patrol unit. I didn't change the height.

## Triangles: 7,988 at LOD0

| Object | Tris | Pivot, Blender (x, y, z) | Pivot, kit/Unity (x, y, z) | Moves |
|---|---|---|---|---|
| `DK_PatrolBot104` (**Body**) | 3,882 | (0, 0, 0.40) | (0, 0.40, 0) | leans and turns about the ball contact; every part below except the Ball follows it |
| `_Ball` | 1,024 | (0, 0, 0.20) | (0, 0.20, 0) | rolls about its centre; its axle runs along x (hubs at the sides), cream band round its equator |
| `_ChestPanel` | 616 | (0, -0.210, 0.6665) | (0, 0.6665, 0.210) | hinged along its top edge (axis = x); open = bottom swings forward and up, about 150° (the shots) |
| `_Gadget` | 506 | (0, -0.045, 0.541) | (0, 0.541, 0.045) | slides forward along its rail; out = 0.11 m forward (kit +z) |
| `_ArmL` / `_ArmR` | 212 each | (±0.247, 0, 0.70) | (∓0.247, 0.70, 0) | turn at the shoulder. L = the robot's own left (Blender +x, kit −x) |
| `_ForearmL` / `_ForearmR` | 388 each | (±0.339, -0.030, 0.613) | (∓0.339, 0.613, 0.030) | the hand, turning about the elbow ball's centre; a shoulder turn must carry it too |
| `_Halo` | 536 | (0, 0, 1.105) | (0, 1.105, 0) | post + ring + ball tip; spins or bobs about the post's foot |
| `_Screen` | 224 | (0, -0.237, 0.91) | (0, 0.91, 0.237) | the face glass. UVs run 0..1 across the visible glass: u toward the robot's left as you face it, v up |

The parts export as siblings, the same as every kit piece. Latch puts them under the Body in the prefab, keeping their world placement, with the Ball left outside it.

**Checked in Blender:** `three_open` and `hatch-open.png` re-import the FBX and pose it. The ball is rolled 40°, the hatch opened 150°, the sidearm slid 0.11 m, the right arm raised 55° with its hand bent 25°, and the whole body leaned 6° about the contact point. No gaps show at the hinge, the opening's rim, the shoulders, the elbows or the contact.

**Not checked in Unity:** that each part imports with identity rotation and its local position at the pivot. Blender's FBX re-import adds an axis turn to each object, which is why the render script poses in world axes.

## Materials: 5, all shared
No per-robot property block is needed.

| Material | Use |
|---|---|
| `DK_RobotEnamel_Sage` (**new**: `DK_Enamel_Sage` #7E8C54) | body green, the hatch. It is the robot variant, so it carries the shared grain/patina detail pair (`robot_tex.py`) like the other robots |
| `DK_RobotEnamel_Cream` (existing, #F3DDB0) | face frame, side panels, tip, slats, arms, hands, ball band, sidearm body |
| `DK_Nickel_Satin` (existing, civic_common) | all bright metal: vents, crown bell, halo, ball hubs, shoulder collars, elbows, barrel. I used it instead of `DK_Chrome` because brief 103 found mirror chrome reads as pale-blue glass under the zone's one probe |
| `DK_Iron` (existing) | the dark metal: ball halves, the hatch's well and rail, the sidearm's shoe and cowl, the vents' backs |
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
- **Ball halves:** dark metal (`DK_Iron`), not satin nickel. The sheets' "chrome" halves are dark and smoky, and nickel washed them out to pale grey (first render). The hubs stay nickel. If Sky wants true chrome halves, that's a one-word change, but in Unity they'd reflect the zone probe and look pale.
- **Sidearm body:** cream, with a dark shoe and cowl and a nickel finned barrel. The brief said dark metal, but a dark body vanished in the dark well. The details sheet shows it cream.
- **Hatch angle:** it opens about 150°, steeply up in front of the chin, as on the details sheet. At 105–130° it stood over the opening and hid the sidearm from any camera above the robot.
- **Halo ring:** it floats round the post with no spokes, as drawn.
- **Small cuts for the budget:** no seam lines on the front and back centre lines, and no screws on the ball band. The ball hubs are slightly faceted stadiums (8 × 2 quads), which don't show at play zoom.
- **Green:** it is my reading of the sheets' sage. It shifts a little bluer than the paintings under the studio light. Glint can judge it in Unity.

## Shared-code changes (small, generic; other pieces unchanged)
- **`geo.Shape.part(name, pivot=)` and `Shape.pivot`:** a part, or the root, can stand at its own origin. The mesh is stored about the pivot; UVs are projected before the move, so robot detail still runs by height.
- **`export_kit.baked`:** carries an object's world matrix when it isn't identity. Every existing piece is at identity, so its FBX bytes are unchanged.
- **`kit.FAMILIES`:** + `patrolbot104`.
- **Caveat:** `kit.asset(floor=...)` edits vertices in local space, so don't combine it with pivoted parts. This piece doesn't use it.
