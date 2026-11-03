# 067 · Vela: the rivals' sedan (`Sedan_Rival`)

Item L of brief 067 ("a rival car that doesn't read as Vale's"). Owner Vela. Piece status: **candidate** (kit.json). Next: Aster adopts, Latch imports.

## What changed

- New: `tools/decokit/cars067.py` (the piece and its three materials), `tools/decokit/cars067.test.ts` (4 tests), `tools/decokit/cars067_plates.py`, `tools/decokit/cars067_compose.py`, `tools/decokit/cars067_boards.ts` (the pictures).
- Edited: `tools/decokit/cars020.py` (lifts `SIDES`, `TUB`, `WHEELS_Y` to module level; the two sedans' bytes unchanged), `tools/decokit/kit.py` (`FAMILIES` += `cars067`), `tools/kit/tags.ts` (three table entries for `Sedan_Rival`; Splice's file).
- Generated and tracked: `contracts/world/kit.json` (+ `Sedan_Rival`, candidate), `contracts/world/kit.collide.json` (+1), `tools/paid/spend.json` (a $0 Codex row).
- Generated, git-ignored: `out/decokit/Models/Vehicles/DK_Sedan_Rival.fbx`, `out/decokit/Materials/DK_Enamel_{Plum,Ink,Pearl}.mat` (+ `.meta`), `paid/067-rival-car/` (the concept and its ledger), `briefs/067/vela/*.png`.
- Not touched: Unity, the GDD, the style contract, the siblings' binaries.

## The answer

`Sedan_Rival` is a variant of the adopted sedan, built by the DecoKit generator (`tools/decokit/cars067.py`), at full detail (9,994 triangles, no LODs, no cap, no lean) on the sedans' footprint (1.81 x 4.73 m, same pivot, same wheel stations, same 0.92 scale), so it parks in the same bays and the sim's car footprint is unchanged.

| | Vale's official | the cell's getaway | today's rival stand-in | **Sedan_Rival** |
|---|---|---|---|---|
| livery | deep teal, ivory roof | coral, seafoam roof | the official car in one flat navy | **plum body, ink-lacquer bubble, pearl twin stripes, chrome** |
| roof read (L* roof minus body) | +51 (light roof) | -5 (level) | 0 (none) | **-14 (the only dark roof)** |
| body colour vs Sedan_Rival (dE2000) | 36.0 | 58.0 | 21.7 | |
| roof colour vs Sedan_Rival (dE2000) | 85.0 | 59.2 | 17.4 | |
| silhouette | slab ends, pill roof, small blade fins | the same | the same | **torpedo plan, teardrop bubble, tall swept fins, wrap bumper and slatted mouth** |

Why it reads as the rivals': the paint is their figures' colour (the rival figure's dominant jacket colour, `#422F42` in `People/Figures/rival/Textures/rival_body_1024_albedo.jpg`, is 9.4 dE from the plum `#482A5A`; their ink `#1B1723` is 1.9 dE from the figure's near-black `#19161F`), and it is a violet that is not the signal violet (the body is 23.1 dE from `#8A4FD0`, much darker and quieter; the marks draw flat over it, board 4). Nothing glows but the sedans' own lamps. No cream, ivory, teal, coral or seafoam on it, no brass or gold (chrome only).

## Why today's stand-in reads as Vale's (a finding)

`HostViews.Rivals.cs` `Trim()` sets one `MaterialPropertyBlock` per renderer: `_BaseColor` = the renderer's **first** material's colour x `RivalTrim`. A property block reaches every material slot of its renderer, and the sedan FBX is one mesh with ten slots, so the whole car (roof, glass, chrome, tyres) takes slot 0's colour, the body teal x (0.46, 0.43, 0.55) = `#0E2E39`. The stand-in is therefore Vale's car, same shape, in one flat dark teal-navy: the ivory roof pill and the whitewalls are gone, the outline is identical. Board 8 shows it as the code runs and as its comment means (each slot x trim, which would keep a mauve roof); both read as the official car. I have not seen it in a Unity frame (no rival-car shot exists in the proof folders I could reach, 0.2.122 and earlier), so the "today" column is my emulation: Latch to confirm on a 2:35 or 3:10 frame. `Scorch()` (burning range cars) does the same thing and is harmless there.

## Pictures

All in `briefs/067/vela/` (git-ignored, on disk). Blender Cycles previews with the game's sun (pitch 38, yaw 330, `light.json`), a warm flat ambient and a street sky for reflections, Khronos neutral tone map, checked against the official car's crop in the game's own frame (cream roof, teal body match within a little). Relative reads only: not the Unity grade. The road and pavement colours are sampled from the game's frames (`#A4B1B6`, `#DDC4AA`). Play camera = orthographic, 38 degrees, 38.57 px per metre (28 m over 1080 px).

| file | what it shows |
|---|---|
| `3-in-the-street.png` | **start here.** The game's own 0.2.122 frame (`briefs/041/build/0311/proof-1080/overhead-pickup.png`: yaw 30, the Office bay, Vale's car at the kerb, the cell mid-road), 1 : 1, the car across the road from Vale's as at 3:10. Left today's stand-in, right Sedan_Rival |
| `1-play-zoom-yaw30.png`, `2-play-zoom-yaw210.png` | true scale, the four cars side by side, four headings of the nose each (toward us / away, left / right), with the L* read and a 2 m bar and a 1.75 m person |
| `4-with-the-rivals-marks.png` | the same street with Glyph's 059 violet marks over the car (approach line, diamond ticks, rival ring, `#8A4FD0` flat): the plum does not swallow them and they do not tint it |
| `5-close.png` | the fitted play angle (38 degrees, 140 px per metre), nose toward us and away, the four cars |
| `6-street-level.png` | eye height 1.5 m, 38 mm, front and rear three-quarter. Shows the siblings' blank nose (see open issues) |
| `7-shape-and-value.png` | plan outlines, then flat shapes, greyscale and squinted (a third of the size): the read does not lean on hue alone |
| `8-stand-in-two-ways.png` | today's stand-in as `Trim()` runs and as its comment means, beside Vale's car and the proposal |
| `9-hero-sedan-rival.png` | the piece alone, large, front and rear three-quarter |
| `0-concept-rival-sedan-options.png` | the concept pass this came from (below) |

Honest read of `7`: by outline alone at play zoom the proposal is different but not hugely (the fins, the pointed nose, the dome bump); the main read is the livery and the value pattern, the silhouette backs it. The fins are the strongest outline cue when the tail is toward the camera.

## How it was made

- **Concept first** (project rule): one Codex image pass at $0, `paid/067-rival-car/codex/rival-sedan-options.png` (ledger row 1 in `paid/067-rival-car/ledger.jsonl`, prompt in the row; style-contract prop-sheet frame, the two sedans as the reference, three options in plan and three-quarter). All three came back plum with a smoked bubble canopy, swept fins, chrome grille and bumper bullets; I took option 1's hood stripes, bubble and fins, option 3's slatted mouth, and moved the colour from the concept's wine toward the figures' jacket. No paid call.
- **The piece** (`tools/decokit/cars067.py`, registered in `kit.py` FAMILIES): the sedan's own tub sections (`cars020.TUB` heights and half-widths, side stations, wheel stations) re-planned as a torpedo; wheel arches cut with the box truck's `carve`; a bubble canopy as a loft of teardrop plans (round front cap, pointed rear cap), lacquer ink over a chrome rim and three pillars; fins with rounded chrome edges and tail lamps; a wrap-round chrome bumper front and rear, bumper bullets, a slatted mouth that follows the nose curve; door lines and handles; two pearl stripes; wheels at 24 segments (the siblings' 14). Three new enamel materials: `DK_Enamel_Plum #482A5A`, `DK_Enamel_Ink #1B1723`, `DK_Enamel_Pearl #D9D3E3`. `REGISTRY["Sedan_Rival"]["nolod"] = True` (the precedent is the avenue palm and the testbed fountain).
- `tools/decokit/cars020.py` is edited only to lift `SIDES`, `TUB` and `WHEELS_Y` to module level; the two sedans rebuild to identical bytes before and after (Official `dc58285b...`, Getaway `bbaad904...`, scratch builds).
- Pictures: `cars067_plates.py` (Blender, transparent plates) and `cars067_compose.py` (PIL, the boards); `bun tools/decokit/cars067_boards.ts` runs both start to finish (about 7 minutes; `--quick` about 2, a look not the boards; it waits while Sky plays). The sets it renders: `play` (the four cars and a person, yaws 30 and 210, four headings), `insitu`, `close`, `tint`, `eye` (also the plan and side views), `hero`.

## Evidence

| check | result |
|---|---|
| `bun run decokit --only Sedan_Rival` | ok: `Sedan_Rival Vehicles tris=9994`; size 1.8142 x 1.5359 x 4.7268 m (siblings 1.8142 x 1.288 x 4.7288); bounds x +-0.907, kit z -2.286..2.441 (siblings +-0.91, -2.28..2.45); FBX 262,412 bytes `d8239fd4...`; no LODs |
| reproducible | a second run into another folder gives the same FBX bytes and the same three `.mat` and `.meta` |
| `bun run kit --catalogue` | kit.json +167 lines: one asset added (`Sedan_Rival`, Vehicles, `source: generated`, `status: candidate`, no `lods`, tags `vehicle:car-rivals`, scale [1,1], sight open); no other entry changed. Nothing copied to Unity (candidate) |
| `bun run collide` | 1 piece measured; `kit.collide.json` +1 line; bounds [-0.91, 0, -2.29] .. [0.91, 1.54, 2.44] |
| `bun test tools/kit tools/decokit` | **29 pass, 0 fail** (25 before; `tools/decokit/cars067.test.ts` adds 4: catalogued and fits the footprint at full detail; body and roof dE from the other two cars; roof darker than body; the figures' colour; no cream, ivory, teal, coral, seafoam, mustard, glass film, gold, brass or signal materials; nothing lit beyond the sedans' lamps) |
| `bun run typecheck` | clean |
| wider (not in the done-check) `bun test --parallel=8 tools/geometry tools/world` | 276 pass, 2 fail, both in `tools/world/rules/walking.test.ts` (29 walk points where 28 is expected): from the working tree's mission and civic-block edits that are not mine; nothing to do with the sedans |

Colour numbers (CIEDE2000, the repo's `deltae.py`; the test carries its own TypeScript copy checked against Sharma pair 1): body official `#1F6C67`, getaway `#FC9C84`, rival `#482A5A`, stand-in `#0E2E39`; roof ivory `#EFE8D6`, seafoam `#72B5A7`, ink `#1B1723`. The rival body is 40.5 dE from the cone red, 67.9 from the guard amber, 23.1 from the rivals' violet.

## For Latch (nothing edited in Unity)

The piece after adoption: `Assets/Suborned/Kit/Models/Vehicles/DK_Sedan_Rival.fbx` (the same frame as the other two: pivot on the footprint centre at the ground, nose toward Blender -Y = Unity +Z, so `carNose` stays identity). `KitImport` remaps its nine materials by name onto `Kit/Materials` (the three new ones are copied with it by `bun run kit`). Loaded as the others are, by path:

1. `unity/Assets/Suborned/Runtime/Play/HandoverHost.cs:39` add `sedanRival` to `public GameObject sedanOfficial, sedanGetaway, robotPorter;`.
2. `unity/Assets/Suborned/Editor/World/Play/PlayInstall.cs:118` after `host.sedanGetaway = Kit("Vehicles/DK_Sedan_Getaway.fbx");` add `host.sedanRival = Kit("Vehicles/DK_Sedan_Rival.fbx");` (null if the kit is not copied yet).
3. `unity/Assets/Suborned/Runtime/Play/HostViews.Rivals.cs:33` (`RivalViewsInit`): the view takes the rival model and keeps `Trim` only as the fallback while it is missing:
   `var rm = host.sedanRival ? host.sedanRival : host.sedanOfficial;`
   `if (Visual("car/rivals", rm) is Transform sr) { carModels["car/rivals"] = sr; if (!host.sedanRival) Trim(sr, RivalTrim); }`
   Delete `Trim`, `RivalTrim` and the fallback once the kit is in (`Scorch` stays for the range).

Things to look at in the proof shots (2:35 turn-in, 2:46 drop, 3:10 park): the car is 1.54 m at the fin tips (25 cm over the others; the violet marker anchored over the car is 2.2 m up, clear); no LODGroup (full detail, one car); 9,994 triangles. The click box stays 1.8 x 1.4 x 4.6 m. The tail lamps and headlamps are the sedans' own lamp materials (`DK_Beacon_Rose`, `DK_Lamp`): nothing new is emissive. The range's spawned rival cars (`HostViews.cs:330`) still use the official model; say if they should take this one.

## For Aster: adopting

Edit `contracts/world/kit.json`, `Sedan_Rival` `"status": "candidate"` to `"adopted"`, then `bun run kit` (copies the FBX and `DK_Enamel_Plum`, `DK_Enamel_Ink`, `DK_Enamel_Pearl` with their metas into `unity/Assets/Suborned/Kit/`, hash-checked), then `bun test tools/kit tools/decokit`. If the plum, the fin height or the stripes should change, they are single values in `cars067.py` (`register("DK_Enamel_Plum", ...)`, `_fin_outline`, `_stripes`): rebuild with `bun run decokit --only Sedan_Rival`, `bun run kit --catalogue`, `bun run collide`.

## Open issues

1. **Unity unconfirmed.** The "today" column is my emulation of `Trim()` (above). The proposal and the numbers do not depend on it.
2. **The two sedans look unfinished beside it.** Their front bumper, grille and headlamps are modelled inside the nose (the tub's nose is at y -2.66, the bumper at -2.36), so only the tail lamp pods show; their wheels are 14-sided (2,016 of their 3,408 triangles; the rival's are 24-sided, 3,456 of its 9,994); no arches. Board 6 shows it. By the floor rule ("nothing ships below the old city and the testbed") the testbed's authored sedan (`civic-sedan-v1.glb`, 8,128 triangles, door lines, glazing, grille, lamps, whitewalls) is the bar: Sedan_Rival is above it, the two stand-ins are below it. If the three should match in finish I can lift the nose, arch and wheel details into shared builders; the siblings would rebuild (new hashes, re-adoption), so it is a call for you (and Sky, since adopted binaries change).
3. **Grade.** Not seen in Unity's ACES grade. Glint to check the plum and the ink dome there (the dome will mirror the sky a little; the plum may drift warmer under the sun) and at night.
4. **Palette.** The three new colours are not in `suborned.style.json`. If the rivals' livery is to be a named role, add them (derived from the concept sheets' `violet_set #6b4f8f`): rival plum `#482A5A`, ink lacquer `#1B1723`, pearl `#D9D3E3`. GDD §15 "stand-ins" still reads "the rivals' sedan in rival trim": once adopted it is `Sedan_Rival` (your text; no mismatch with the contract today).
5. **Tags file.** I added `Sedan_Rival` to three tables of `tools/kit/tags.ts` (use `vehicle:car-rivals`, scale [1,1], sight open): Splice's file, a tag-only edit that the catalogue needed so the entry matches its siblings. Splice to confirm.
6. **kit.collide.json quirk.** The voxel measure lists the right tail fin as a solid box and the left as thin. Nothing reads a car's volumes (the sim uses the footprint, rulebook `car-len-cm` and `car-w-cm`); declare volumes with `Shape.solid` if a reader appears.
7. **Triangles.** 9,994 against the sedans' 3,408 (the testbed's sedan is 8,128). One instance in the scene; Redline to say if it matters.
8. **Previews.** Preview lighting is not the game's; the render script needs `out/decokit/HDRI/city.exr` (the exporter copies it) and the compose script uses `out/concepts/ui-011-kit/fonts/Jost-Bold.ttf` if present (Arial otherwise).

## Next owner

Aster adopts (or asks for a change), then Latch imports (three lines above, with 2:35 / 2:46 / 3:10 proof shots), then Glint checks the grade and Suture the read at 3:10 against Sky's complaint.
