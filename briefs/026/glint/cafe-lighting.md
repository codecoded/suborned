# Café lighting: what the 2.5D testbed did, and the Unity port plan

Glint, 2026-10-20. Frames in `briefs/026/glint/cafe/`. Testbed captured headless (Chrome over CDP from `out/glint026/capture.ts`, served read-only from `~/Projects/the-suborned/testbed`). Unity captured with a temporary hidden camera and temporary lights (HideAndDontSave), sun and ambient restored in `finally`; the scene was never dirty and no scene object was changed (`out/glint026/CafeProof.cs`).

## Frames

| file | what |
|---|---|
| `compare-night.png` | testbed night, Unity night today (no local lights), Unity night with 27 proof lights |
| `compare-evening-vs-proof.png` | testbed evening vs the Unity proof |
| `compare-day.png` | day, both |
| `testbed-night-wide.png` | testbed plaza and road at night |
| `testbed-{cafe,court,detail,play}-{day,evening,night}.png` | all raw testbed frames |
| `unity-cafe-{day,night-bare,night-lit}.png`, `unity-009cafe-day.png` | raw Unity frames |

## What made the testbed café look good

It was not one trick. It was value contrast plus many small, colour-varied, unshadowed pools, each paired with a glowing fixture.

1. **A dark, cool base.** Night: sun 0.18 `#a7c5df`, hemisphere sky 0.12 (`#fff4dd` / ground `#6b8c89`), env map on materials 0.08, background `#102b36`, exposure 1.05, ACES, **no bloom, no post stack**. Evening: sun 0.75 `#ffb973`, sky 0.28, env 0.3. The ground reads near-black teal, so every pool has about 10:1 contrast against it. Unity night today is a flat blue-grey (ambient just scaled down) with nothing to contrast against.
2. **Five table lamps, five hues.** Point lights, 3.5 cd, range 3.4 m, decay 2, cycling `#ffc066 #67d6ce #ffd099 #ffa891 #b79be4`, 0.16 m above a 1.5 m table top. The opal diffuser (emissive 2.2) is tinted by the same colour, so source and pool agree. This is the biggest "amazing" cue: a warm amber base with teal, coral and lilac islands.
3. **Interior as a lit room.** 3 warm spots (`#ffd39b/#ffcd92/#ffdfb7`, angle 1.05-1.15 rad, penumbra 0.6, range 3.4-4.8 m, intensity 20-34 at night; day 3-6) pointing straight down from ~3.1 m, plus 2 mood points (teal `#56c9b8` 6, amber `#ffb46d` 7, range 2.3-2.5) and 1 pendant spot with a cached shadow. Emissive pendants/cove 2.6 (night), sign 3.2, coral sign 2. Through the glass the room is the brightest thing in frame.
4. **Wall washers.** 7 terrace wall lights, white, spot angle 1.05, penumbra 1.0, range 4 m, **decay 1.3, not 2**, intensity 2.8 at night, aimed at the paving 1.15 m out. They make the scalloped pools along the terrace base. Decay 1.3 is why the pools are broad and soft rather than hot dots.
5. **Palm uplights.** 3 spots `#e4efc7`, half-angle 0.48 rad, penumbra 0.8, range 6.4, intensity 30 at night (16 evening), aimed up the trunk to +3 m. Lit trunks and fronds add depth and a vertical read at play zoom.
6. **Fixtures glow.** Wall-light opal emissive 4 (night), tree lens 2.3, chrome env 0.7 at night. Emissive read through ACES' shoulder is the only "bloom" there is.
7. **Almost no local shadows**: 1 table lamp (384 px) and 1 pendant (1024 px), both cached (`autoUpdate=false`). Sun shadow 2048, PCF. The other ~19 local lights are unshadowed.
8. **Cost control.** `cafe-static-light-assignments.js` compiles out lights that cannot reach a material's bounds; `cafe-local-light-culling.js` skips BRDF work at zero contribution; light slots stay fixed so day/night never recompiles. Unity deferred gives the first for free, so none of this is ported.
9. **Light budget in the café patch: 21 local lights** (5 table, 7 wall, 3 uplight, 6 interior) plus plaza lamps. Measured on this Mac (M5 Max, WebGL, 1.18 M triangles, 485 draws): GPU 3.4 ms mean / 6.3 ms p95 at night.

## Unity today

- Scene: one directional sun (1.8, `#ffe0d0`, soft shadows), trilight ambient, **no other lights and no night state**. Volume `HarbourDistrict-grade`: Tonemapping mode 2 (ACES), Bloom threshold 1.1 / intensity 0.22 / scatter 0.6, post exposure +0.15, contrast 6, saturation 18, vignette 0.1.
- URP asset `PC_RPAsset`: additional lights per-pixel, per-object limit 4, additional shadows supported (2048 atlas), light cookies on, HDR on, MSAA 1, main shadow 4096 / 1 cascade / distance 260. Renderer `PC_Renderer` is **Deferred** (`m_RenderingMode: 2`).
- The café (`DK_CafeBay_v2`, `DK_CafeInterior`) uses `DecoKit/Lit Cutaway` (has a GBuffer pass, so deferred lights reach it) and `Complex Lit Cutaway` for enamels (`UniversalForwardOnly`: the 4-per-object forward limit applies). `DK_CafeInterior` already has emissive `DK_Cafe_Opal` (about 2.1, 1.9, 1.4) and `DK_Cafe_Cove` (about 1.7, 1.2, 0.7); nothing else glows.
- Eight table positions, six palms and four street lamps within 40 m are listed in `out/glint026/CafeProof.cs`.

## Proof (not a deliverable): what 27 temporary lights do

`unity-cafe-night-lit.png`: sun 0.25 `#a7c5df`, ambient x0.16, plus 8 table points (2.2, range 3.8), 4 interior points (9, range 9), 5 awning spots (22, 110 deg, range 9), 6 palm spots (16, 55 deg), 4 street-lamp spots (14, 85 deg, range 11). Works in the live deferred renderer with no shader change; pools and colour variety read at once. Still missing against the testbed: the grade (shadows are blue-grey, not teal-black), a lit-room glow through the glass, glowing fixtures (lamp globes, awning, signs are not emissive), and palm uplights did not land on visible plants (scene palms are empty transforms; Latch must attach to the real meshes).

## Port plan

### Glint (lighting and grade); Latch wires the numbers
- Night and evening as data, one block per phase. Night: sun 0.18-0.25 `#a7c5df`; ambient 0.12-0.16 of day with a cool tint (0.75, 0.9, 1.15); reflection/environment 0.1. Evening: sun 0.75 `#ffb973`, low and long; ambient 0.28.
- Grade: shadows teal-black rather than blue-grey (ShadowsMidtonesHighlights shadows toward `#0f2a33`, lift down); keep ACES. Bloom at night: threshold 0.9, intensity 0.35. The testbed had none, but our emissives need it; test before committing. Day grade unchanged.
- Light palette: warm base `#ffc066 #ffd099`, accents `#67d6ce #ffa891 #b79be4`, accents at most 1 lamp in 3. Signals stay unlit/emissive outside the grade and are never driven by this.
- Rule for every pool: a glowing fixture (Vela) plus a matching light (Latch), or neither.
- I check in game with Latch against `testbed-detail-night.png` at zoom 14, yaw 30 and 210.

### Vela (emissive materials, lamp pieces)
- Night emissive (HDR) switched by one per-material night scalar so day stays 0: table opal diffuser 2.2, tinted per lamp; wall/awning lamp opal 4; palm uplight lens 2.3; pendants/cove 2.6 (interior opal and cove already exist: add the multiplier); café sign 3.2, sign coral 2.
- Pieces: atom-age table lamp (opal diffuser, brass body, 0.29 m); awning under-lamp or terrace wall light (chrome plate, opal strip, chrome eyelid); palm uplight can (chrome, recessed lens); street-lamp globe on the existing `DK_Lamp` heads (the head is only 0.2 m at y 4.3).
- Glow lives on its own material so signals stay unlit and out of the grade. Signage emissive values should come from this note, not be set separately.

### Latch (light objects, URP settings)
- Builder-spawned lights beside each fixture, no scene hand-edits: table point (colour per lamp, 1.2-2.2, range 3.6-3.8); interior 4 points (warm, range 9) or 3 downward spots; awning/wall-wash spots (100-110 deg, inner 35%, intensity about 20, range 9); palm spots (55 deg, range 7-8); street-lamp spots (85 deg, range 11). URP intensity is roughly three.js candela divided by pi; retune from the proof values.
- Additional lights: **no shadows**, no cookies. At most one cached shadow (interior pendant), only if the budget allows.
- Light slots always exist; intensity 0 by day. A controller enables only lights within about 60 m of the camera focus, cap 32 live.
- Keep `PC_Renderer` Deferred (Forward would let the 4-per-object limit starve large facades). Per-object limit stays 4; it only affects Complex Lit enamels.
- Night as a Volume profile swap or weight driven by the same phase value as sun and ambient; reflection probes dimmed to 0.1.

## Frame cost on the MacBook Air M4 (estimate; Latch/Redline to measure on the Air)
This session ran on an M5 Max, so there is no Air figure yet. My Unity timing delta (+27 ms per captured frame) is dominated by readback, PNG encode and light spawning, so it is not a GPU number. By coverage: at zoom 14 / 1080p (about 38 px/m) the 27 proof lights shade roughly 4-5 M pixels, about two full-screen light passes. A simple deferred light pass costs about 0.3-0.5 ms at 1080p on an M4, so **about 0.7-1.2 ms** for the café patch plus about 0.2 ms for night post. Testbed reference: 3.4 ms GPU at night on the M5 Max with 21+ forward lights.

Budget: local lights on screen at most 1.5 ms and 32 live. If over, drop table and palm lights first, keep interior and street lamps, then run bloom at half resolution. Measure with Frame Debugger light-pass count and a GPU capture on the Air in the café night view, lights on and off, 60 s each.

Baked lightmaps are not recommended: the world is generated and combined each build, and the look is dynamic. Emissive-only (with bloom, no light) is the fallback for anything past the cap: glass, signs, lamp globes.

## Open
- Unity has no night state yet; scheduling it is Aster/Latch (outside Glint's edit rights).
- Palm and table light positions come from empty transforms: Latch attaches to the real pieces.
- Next owners: Vela (emissive and lamp pieces) and Latch (lights, volume), then Glint check in game.

Evidence: testbed `window.__cafe` stats (night café GPU 3.4 ms mean, 485 draws, 1.18 M tris; exteriorLights 7 wall + 3 tree, 0 extra shadow maps; 5 table lamps, 1 shadow). Unity: after the captures `isDirty:false` and a single Light (`/lighting/sun`).

---

## Round 2 (2026-10-20): night data, hand-offs, chrome

### Night phase data (done, data only, no scene touched)
`unity/Assets/Suborned/Editor/World/light.json` has a new top-level `night` block, same shapes as the day keys (sun, ambient, fog, skyGradient, reflection, post), `night.*` palette entries, `night.lamps` (spawn rules) and `night.emissive` (for Vela). The builder ignores unknown keys, so day is byte-identical until Latch reads `night`. Numbers: moon 0.22 `#a7c5df` pitch 45 yaw 330; ambient 0.16 / 0.14 / 0.10 of `#8fb3d6 / #7fa6bf / #3d5b66`; haze `#102b36`; ACES, post exposure +0.2, contrast 8, saturation 14, bloom 0.9 / 0.35, vignette 0.18; shadows grade a mild teal multiplier `#6fa3a8` (URP multiplies below luma 0.3, so the black comes from the ambient, not from a near-black filter).
- **Latch:** read `night` with the day code into a second profile (`<scene>-grade-night.asset`) and lighting state selected by a `phase` argument; spawn lights from `night.lamps.kinds` with `rules`: no shadows, no cookies, at most 32 live within 60 m of the camera focus (others at intensity 0), Deferred kept, per-object limit 4. Attach lights to real fixtures, not empty transforms. Retune in game against `cafe/testbed-detail-night.png`.
- **Vela:** `night.emissive.night` (table opal 2.2, awning/wall lamp 4.0, uplight lens 2.3, pendant/cove 2.6, interior opal 2.6, sign 3.2, sign coral 2.0) on glow-only materials with a per-material night scalar (0 by day), and the four pieces in `night.emissive.pieces`: TableLamp_Opal, AwningLamp, UplightCan_Palm, StreetLampGlobe.
- Look only: nothing in the sim, schedules, sight or replays reads light. Signals stay unlit, emissive and outside the grade.

### Proposal for Motive (not built): does night change sight?
By default no. If Sky wants it to matter: (a) guard and camera cone range shrinks outside lamp pools (a lit/unlit tile flag from the lamp placements, data in the plan, not from rendered light); (b) witnesses in a lamp pool count as better lit for recognition. Both need Keel rulebook events and a replay-version bump; the cost is that the player must read pools as a rule, against "show rules in the street, less reading". The pools are readable, so (a) could be shown with no text. Decision is Sky's and Motive's.

### Chrome reads flat pale grey: cause and fix
Cause (matches Latch's measure): chrome outside the two probes mirrors the default cube, which is haze `#f3eee4` (L 0.93) from the horizon up with a short ramp to ground, so there is nothing dark to reflect (cube mean 0.56). Inside the civic-square probe the box is stone and sky (mean 0.62).
Fix in `light.json` (data; Latch applies):
1. `reflection.street` (day) and `night.reflection.street`: a street-canyon cube. Below the horizon `#4a4036` to `#1f1b18` over 55 deg; above it wall base `#2f3a3b` to wall `#7d8785` over 10 deg; blend to sky over 20 to 36 deg; sky = lerp(haze, zenith, 0.4 * y^0.55). Sun glow terms unchanged. Computed over the sphere: mean 0.56 to 0.40, below 0.35 from 0% to 54%, above 0.75 from 16% to 24%. A dark band, mid walls and a bright high streak, so chrome shows form.
2. Default reflection intensity 0.8 (`reflection.intensity`), probe intensity 0.75 (civic-square) and 0.85 (office-front) (`probes.list[].intensity`; Probes.cs must set `ReflectionProbe.intensity`).
3. Night: cube mean 0.13 with a warm lit-window band, intensity 0.25 (0.1 makes chrome go black; the point lights' speculars give the highlights).
4. Not done: a third street-level baked probe (box projection would distort over a district-wide box); revisit only if the cube is not enough.
Verification still needed: Latch rebuilds with the cube function and re-runs `briefs/027/latch/chrome/` close-ups; pass = chrome cap mean 0.35-0.5 with a visible dark band and one bright streak. My numbers are computed on the cube, not yet seen in the engine.

## Round 3 (night 0.2.8 retune; data in light.json `night.lamps` and `reflection.street`)

| kind | before | now | pixel-shaded area (sphere/cone footprint, play view) |
|---|---|---|---|
| awning | 6 spots, 22, range 9, 110 deg | **4** spots, 16, range **6.5**, **90** deg | about -70% |
| interior | 4 points, 9, range 9 | **3** points, 8, range **6.5** | about -60% |
| table | 8 points, 2.2, range 3.8 | 8 points, **1.7**, range **3.2** | about -30% |
| palm | 6 spots, 16, range 8, 55 deg | 6 spots, 14, range 6.5, **45** deg | about -40% |
| street | spot 14, range 11 | spot **19**, range **6.5** (floor pool unchanged, cone volume 3.4x smaller; x1.35 repays the URP range window) | about -65% of the volume |
| cap | 32 within 60 m | **28 within 45 m** (the play frame is about 30 m from its centre to a corner) | |

The 56.6 fps case was the awning/interior volumes (9 m range) covering most of the café frame in deferred. Expected GPU saving is about 30-40% of the light passes; I could not measure it (Studio only, no Air): Latch re-captures `cafe-yaw30` at 6016x3384 and 1080p, goal under 14 ms GPU at 6016x3384 with the same pools.
Blown table: the awning spot (22) landed on the table lamp pool (2.2) and the sum went past 1.0 into the 0.9 bloom threshold. Awning is now 16 and 90 deg (its pool is about 3 m across and falls between tables), the table lamp 1.7; the combined overlap peaks near 0.8 display. If one table still clips, Latch moves that table's awning aim 0.6 m along the front, not the intensity.
Latch: `awning` takes `count` (4) from data (it was hard-coded 6 in NightPhase.cs).

**Records Office front** (new kind `office`, 5 lights, no shadows): door point 9 (range 5.5) 2.9 m over the landing 0.6 m out; landing spot 15 (range 6.5, 75 deg) from 3.2 m, 1 m out, aimed at the landing centre; two pier uplights 12 (range 7, 55 deg) aimed up the piers +3.5 m, colour `night.uplight`; one forecourt lamp spot 14 (range 8, 85 deg) 4 m over (x 64, y -40.5 plan). Vela: the door lamps are two AwningLamp pieces flanking the door (opal 4.0).

**Chrome by day:** a streak is added (`reflection.street.streak`): a sky-window band 34-54 deg up, +-70 deg about the sun azimuth, displayed 0.97 (the cube is RGBA32 so 1.0 is the ceiling), about 4% of the sphere. Computed: mean 0.42, below 0.35 54%, above 0.8 9% (was 0), max 0.97.
**Chrome by night:** the hydrant outside a pool was black because the night cube was mean 0.13 at intensity 0.25 (0.033). Now: wall band `#8a6b48` up to 22 deg, ground horizon `#2b3d44`, a cool streak `#b3cde0` (30-50 deg, 0.7), cube mean 0.22, max 0.79, intensity **0.45**: about 0.10 effective (3x). Night ambient 0.16/0.14/0.10 to **0.20/0.18/0.12**; pools still keep about 8:1 over the stone. If the hydrant is still too dark, raise intensityOverride to 0.6 before touching ambient.
