# 070 · Vela: the getaway car, finished (`Sedan_Getaway_v2`)

Asked by Aster (low priority), after Glyph's note on the briefing page's getaway still (build 0133, `brief-getaway.jpg`): "the car model has no detail and looks like a stand-in". Owner Vela. Piece status: **candidate** (kit.json). Not adopted, not copied to Unity. Aster adopts after Sky has seen it.

## The proposal

The same car as the adopted `Sedan_Getaway`, finished to `Sedan_Rival`'s level. It keeps what makes it the cell's car: the slab body, the pill hardtop, coral with a seafoam roof.

- **The nose comes out.** Today's bumper, grille and headlamps sit inside the nose (the tub's nose is at y -2.66, the parts at -2.36), so a close still sees a blank slab. Now the nose has:
  - a wrap-round chrome bumper laid on the nose's own outline, with a lower lip and two over-riders
  - an oval chrome grille mouth with vertical bars over a dark intake
  - round headlamps in body-colour pods under chrome brows, and parking lamps below them
  - a chrome hood spear with a low chrome comet on it
- **Body:**
  - wheel arches cut in
  - a seafoam two-tone flash down each flank, edged in chrome and kept above the rear arch
  - door lines, handles, a chrome rocker strip and a chrome beltline
- **Cabin:** chrome window base and drip rail, seafoam A and C posts and a chrome B post. It reads as a hardtop on posts, not a glass blob under a lid.
- **Tail:** rounded fins with chrome crests and bullet tail lamps, lamps in the tail panel, a chrome tail strip, a wrap-round rear bumper and a boot handle.
- **Wheels:** 24-sided (today's are 14), with whitewalls and chrome caps.
- **Body texture:** the paint is `DK_CarEnamel_Coral` and `DK_CarEnamel_Seafoam`. These are the same rows and hexes as `DK_Accent_Coral` and `DK_Enamel_Seafoam`, with a new car detail pair on top (`car_tex.py`), built the way the robot enamels carry theirs:
  - a 2 mm metallic flake
  - a faint 3 cm film mottle and 25 cm cloudiness (albedo sd 2.6 %)
  - an orange-peel normal (bump 0.25)
  - the pair is isotropic, tiled 1 m on the 1 m box UVs, 0.66 MiB with mips

  It reads close up and in highlights. At play zoom it does nothing: the chrome and the nose carry the read there.
- **Unchanged:** the chassis, the footprint, the pivot and the wheel stations. It uses the same `cars020.TUB` sections, `WHEELS_Y` (-1.38, 1.4) and 0.92 scale. Every added part sits inside the old bounds:
  - catalogued size 1.8142 x 1.288 x 4.7288 m and `pivotOffset` [0, 0.083], both identical to `Sedan_Getaway`
  - collide bounds [-0.91, 0, -2.28] .. [0.91, 1.29, 2.45]

  Nothing in the sim or the world moves. The host's click box stays 1.8 x 1.4 x 4.6 m.
- **Full detail:** 14,876 triangles, no LODs (`nolod`, as Sedan_Rival). Today's car is 3,408 with L1 and L2; Sedan_Rival is 9,994; the testbed sedan was 8,128. It's one car in the scene.
- **Palette:** no brass, no gold. Nothing new is lit: the lamps are the sedans' own `DK_Lamp` and `DK_Beacon_Rose`.

## Pictures (`briefs/070/vela-getaway/`, git-ignored, on disk)

Blender Cycles previews in the same light as brief 067's boards: the game's sun (pitch 38), a warm flat ambient, the street HDRI for reflections, Khronos neutral. These are relative reads, not the Unity grade.

| file | what it shows |
|---|---|
| `1-briefing-still.png` | **start here.** The still's framing at 1:1 in 1080p (ShotRunner's car pose: the camera at (1.8, 5.8, 11.5) m from the car, looking at (0, 0.4, 0), 32 degrees vertical; the car's heading fitted to the frame). Left: the game's own frame (build 0133). Middle: today's car. Right: the candidate |
| `2-play-zoom.png` | the play camera (orthographic, 38 degrees, yaw 30, 38.57 px per metre, the car about 70 px long), nose toward and away. Top row 1:1, bottom row the same pixels x3 |
| `3-close.png` | the play angle at 150 px per metre, front and rear three-quarter, today and the candidate |
| `4-street-level.png` | 1.5 m eye height, front and rear three-quarter |
| `5-vales-car.png` | Vale's official car today and with the same lift (render-only) |

At play zoom the gain is modest but real. At 1:1 today's car is a coral lozenge. The candidate shows a chrome nose and bumper glint, wheel arches, the seafoam flash along the flank and roof posts. In the still and close up it stops being a stand-in.

## Sedan_Official: yes, the same lift

Vale's car shares the chassis and has the same blank nose. It stands at the Office bay in the pickup shots, beside the cell's car and the rival's car. If only the getaway is lifted, Vale's car becomes the one stand-in left in that frame. `cars070.sedan_v2(s, body, roof)` takes any livery, and board 5 shows it in deep teal with an ivory roof and flash. I haven't catalogued it. Say the word and it becomes `Sedan_Official_v2` (one `@asset` line and three tag entries), with the same done-check.

## How it was made

- New:
  - `tools/decokit/cars070.py`: the builder and the piece
  - `tools/decokit/car_tex.py`: the detail pair
  - `tools/decokit/cars070_plates.py` and `cars070_compose.py`: the pictures
  - `tools/decokit/cars070.test.ts`: 3 tests
- Edited:
  - `tools/decokit/mats.py`: `CAR_DETAIL`, `car_enamel()` and `_detail_of()`. The robot enamels' detail path now serves both prefixes; the robot rows are unchanged, and all 22 `DK_RobotEnamel_*.mat` are byte-identical before and after.
  - `tools/decokit/export_kit.py`: copies `car_*` textures beside `robot_*`
  - `tools/decokit/run.ts`: runs `car_tex.py`
  - `tools/decokit/kit.py`: `FAMILIES += cars070`
  - `tools/kit/tags.ts`: three entries for `Sedan_Getaway_v2`, the same as the getaway's. This is Splice's file, a tag-only edit as for Sedan_Rival.
- Generated and tracked: `contracts/world/kit.json` (+1 asset, no other entry changed) and `contracts/world/kit.collide.json` (+1 line).
- Not touched: Unity, the adopted `Sedan_Getaway` and `Sedan_Official` (their builders and bytes), the GDD and the style contract.
- No paid calls and no concept pass: this is a finish on an adopted shape, not a new look.

## Evidence

| check | result |
|---|---|
| `bun run decokit --only Sedan_Getaway_v2` | `Sedan_Getaway_v2 Vehicles tris=14876`, FBX sha256 `bc4bc3ea…` |
| reproducible | a second build into `out/scratch/vela/070/rebuild` gives the same FBX (`bc4bc3ea…`) and the same `DK_CarEnamel_Coral.mat` (`6e0aacd2…`) |
| bounds probe (Blender, per material) | every part inside y -2.447..2.282, x ±0.907, z ≤ 1.288 (scaled), the old car's own extents |
| `bun run kit --catalogue` | 349 assets; `Sedan_Getaway_v2` candidate, size, pivot and pivotOffset equal to `Sedan_Getaway`; no `lods`; files include the two car textures; no other asset changed (diffed against the pre-run copy) |
| `bun run collide` | 1 piece measured, +1 line |
| `bun test tools/decokit tools/kit tools/world/gen/kit020.test.ts` | 24 pass, 0 fail |
| `bun run typecheck` | clean |

## Adopting (Aster, after Sky)

Two ways in. Either way, Unity sees the change only after `bun run kit` copies the files.

1. **Swap in place (recommended; no Unity code change):**
   - In `cars020.py`, make `Sedan_Getaway` call `cars070.sedan_v2(s, "DK_Accent_Coral", "DK_Enamel_Seafoam")` with `nolod`, and delete the `Sedan_Getaway_v2` asset and its tags.
   - Rebuild with `bun run decokit --only Sedan_Getaway`, then `bun run kit --catalogue`, `bun run collide` and `bun run kit`. This is my change to make once you say so.
   - The adopted binary changes hash and loses its L1 and L2 levels; that's why it needs Sky's sign-off. `PlayInstall.cs` keeps loading `Vehicles/DK_Sedan_Getaway.fbx`.
2. **Adopt the v2 as its own piece:**
   - Set `"status": "adopted"` on `Sedan_Getaway_v2`, then run `bun run kit`.
   - Latch changes `unity/Assets/Suborned/Editor/World/Play/PlayInstall.cs:118` to `Kit("Vehicles/DK_Sedan_Getaway_v2.fbx")`.

Either way, `KitImport` maps the two new materials by name and the two car textures come with them. Latch should then re-shoot `brief-getaway`, and Glint should check the coral and the chrome in the ACES grade and at night.

## Open issues

1. **Unity unseen.** Everything here is a Blender preview. The detail pair goes through the robot enamels' proven `.mat` path (Complex Lit, clear coat, `_DETAIL_MULX2`), but nobody has looked at it in a Unity frame yet.
2. **Triangles:** 14,876 against Sedan_Rival's 9,994. It's one view in the host, at full detail by the rule. Redline to say if it matters.
3. **Material key name:** `decokit_materials.json` carries the car pair under the existing `robotDetail` key, so `unity_mats.py` (unchanged) writes it the same way. Splice may want it renamed `detailPair` later; that would rewrite the robot rows' JSON, though not their `.mat` files.
4. **The still's heading** in board 1 is fitted by eye to the game's frame (181 degrees). The camera is the shot's own pose.
5. **The tags file** is Splice's. I added three entries, as for Sedan_Rival.

## Next owner

Aster: show Sky board 1 (and 5, for Vale's car), then pick adoption route 1 or 2. After that: Vela if route 1, Latch to re-shoot `brief-getaway` (plus the one-line path change if route 2), Glint for the grade and Suture for the read in the pickup shot.
