# Brief 109 · Juice · round 4 (Latch, 2026-11-11)

Sky's ruling on 0.2.327, built end to end. Same rules as before: view only, replays unchanged, nothing screen-wide.

**The build for Sky:** **0.2.331** (sections 6–7: the day flicker and the ending; 1,039 s, the town rebaked for the contact shade). Before it, **0.2.329** (`out/build/play/The Suborned.app`; `bun run batch --sky`, 200 s, BATCH OK). It has round 4's four parts and the two night fixes (section 5). 0.2.328 had the four parts only.

## 1 · Lights done properly
Sky on the night searchlight: "it needs to be done properly, same for car head, tail and streak lights. Not just some flat opacity". Round 3's beam was a flat grey cone with hard edges.

**The new shaders are two Shader Graphs,** `Generated/Juice109/Juice_Beam.shadergraph` and `Juice_Streak.shadergraph`, written by `tools/juice/author.ts`:
- **Structure:** a URP Unlit graph (transparent, additive, both faces, no depth write). Unity's own nodes (UV, world position, world view direction, world normal, vertex colour, time, Scene Depth in Eye mode, which handles the orthographic camera) and two properties (`_Tint` HDR, `_Params`) feed one Custom Function from `JuiceLight.hlsl`.
- **Unity's importer:** both compile with no messages (`authoring.json`).

**The beam** (`JuiceBeam`), the standard volumetric-looking beam for URP:
- **Soft edges by view angle:** the surface's facing to the view, so a silhouette fades to nothing and the core stays.
- **Falloff:** along the length, brighter by the lamp.
- **Depth fade:** where it meets geometry, so there are no hard intersection lines. It's also faded right by the lens.
- **Dust:** two octaves of drifting 3D noise.
- **HDR:** the play bloom takes the hot core.
- **The cone:** a proper mesh with outward normals and UVs (`Juice109Lights.BeamMesh`).

**The streak** (`JuiceStreak`): a soft core across the width with a hot centre line, a taper and fade along its length, and HDR.

**The searchlight:**
- now the beam shader: tint about 1.1, with the brightness held by the lamp;
- a lamp glow at its base from URP's **Lens Flare (SRP)**: procedural data, a soft round glow, a small hot core and a faint thin streak, occluded by depth;
- the roof choice from round 3 stays (only roofs in the frame's upper part), so shafts rise over the far city;
- at first build the lens glow was far too big, since Lens Flare (SRP) sizes are fractions of the screen; it is now small.

**The cars** (`Juice109Lights.cs`), every drawn car with all positions from its own model. The Editor reads every vehicle model's lamps at build time: the front `DK_Lamp` and the rear `DK_Beacon_Rose`. That gives the sedans head and tail lamps, and the scooter a headlamp.
- **Headlamps:**
  - a Lens Flare (SRP) lens glow at each lamp;
  - **at night or in rain while it drives:** two beams angled down the road (the beam shader) and **one real spot light** for the pool on the street (no shadows);
  - by day, only our car's lamps glow faintly while it drives.
- **Tail lamps:** red HDR glows that the bloom takes, at night always and by day only on braking. **Brighter on braking:** the car losing speed faster than 1.4 m/s².
- **Streaks:** at night while it drives, a short streak behind each tail lamp (the streak shader).
- **Sky lanes:** the trails now use the streak shader: a hot core that tapers and fades, no longer a flat ribbon.
- **Round 1's getaway flare quads are gone,** replaced by the lamps rig.

**Volumetric Light Beam vs Volumetric Fog & Mist 2: recommendation** (no purchase; Sky decides):
- **Volumetric Light Beam** (Tech Salad):
  - **Price:** $44.90 (sales seen at $22.45). Latest version 2.2.9, Sep 2026.
  - **Support:** Unity 6 and URP. I found nothing confirming its Render Graph path or Metal.
  - **What it adds:** a per-beam mesh-and-shader approach, the same idea as ours. Its "HD" mode adds true raymarched volumetrics with shadows and cookies.
  - **Verdict:** for a lit-from-inside look on searchlights and headlights, our Shader Graph beam gives the same look at a known cost. VLB beats it only if Sky wants beams that geometry *shadows* (a beam cut by a building). That's the one reason to trial it.
- **Volumetric Fog & Mist 2** (Kronnect):
  - **Price:** about $69.99 (third-party listing; check the store).
  - **Support:** Unity 6 and URP Render Graph. Native spot and point light volumetrics with shadows (8 lights in Forward, 64+ in Forward+). Metal isn't stated.
  - **What it is:** a full-screen raymarched fog volume: atmosphere and light shafts through fog for the whole view.
  - **Verdict:** a heavier, screen-wide technique. For this look it's more cost on the Air than the beams need, and it brings fog the play view doesn't have (the play look is crisp, with no fog). Not recommended for this.
- **Recommendation:** keep the Shader Graph beams. Consider VLB only if shadowed beams become a goal; it's cheap enough to trial against ours on the Air.
- **Sources:**
  - [Volumetric Light Beam (Asset Store)](https://assetstore.unity.com/packages/vfx/shaders/volumetric-light-beam-99888)
  - [Volumetric Light Beam (deals listing)](https://www.gameassetdeals.com/asset/99888/volumetric-light-beam)
  - [Volumetric Fog & Mist 2 (Kronnect)](https://kronnect.com/products/volumetric-fog/)
  - [Volumetric Fog & Mist 2 (Asset Store)](https://assetstore.unity.com/packages/vfx/shaders/fullscreen-camera-effects/volumetric-fog-mist-2-162694)
  - [Fog vs Volumetric Lights (Kronnect docs)](https://kronnect.com/docs/volumetric-lights-urp/dynamic-vs-volumetric/)
  - [Volumetric Fog & Mist 2 (deals listing)](https://www.gameassetdeals.com/asset/162694/volumetric-fog-mist-2)

## 2 · The social win (Sky: "I like that turning idea. Can use it more")
**The signature, at whoever it landed on, on screen or not:**
- **A warm halo of light:** additive saturated warm orange over the pale stone reads as warm light. Round 3's filled alpha-blended gold read brown.
- **A thin gold ring** opening at their feet: alpha-blended, lighter gold, giving the shape.
- **Motes:** they rise, fall like coins, or drift away, by kind.
- **The chip pop:** the done chip's bobble-head cameo pops. It is bigger for a turn or takedown, smaller for the others.

**The events used** (all the engine's own log codes; who it lands on is read from the line):

| kind | log code | on | style |
|---|---|---|---|
| turn | `turn.<approach>.turned` (help, pay, …) | the person, from "(role)" | the full signature: big halo, ring, a burst of rising motes |
| paid | `rivals.paid` | the rival, from the first word | smaller halo, motes that pop up and fall like coins |
| misled | `rivals.misled` | the rival, or the lookout by `Game.Crew` role | no halo; ring and motes drifting off the way he goes |
| call | `call.<role>` | the contact | two rings one after the other, a few motes |
| heard | `*.listen.heard` (ours and the rivals') | the person heard | small: faint halo, narrow ring, a few motes |
| watched | `*.watch.watched` | the watcher (our agent) | the same, small |
| lent | `lend.lent` | the councillor's car | halo and a wide ring at the car |

`rivals.paid` occurs in no recorded route, so it has no clip; the cost demo cycles it.

## 3 · Hit flash (Sky: "when someone gets shot, they should flash if not already, and same for your own players when you get hit")
- **Which hits:** every engine hit (`shot.hit`, via `HostFight.Shot` → `JuiceShot`) on rivals, guards, police, our four, or a car.
- **The flash:** a quick white-hot flash on the body itself. One more material (the Shader Graph glow on white, additive) is appended to its own renderers for 0.18 s and draws the same mesh again in its own pose: full for 0.04 s, then fading.
- **Not the emission keyword:** I didn't switch `_EMISSION` on the figure's own material, because that variant may be stripped from the build.
- **Never twice at once:** a body already flashing isn't flashed again.
- **Which renderers:** skinned bodies and URP-lit meshes only, so a vertex-animated crowd body is never flashed in its bind pose. A multi-submesh skinned body flashes its last submesh only; the figures checked flash 3 renderers each.
- **Nothing screen-wide.**

## 4 · Tilt-shift (Sky: "cool but yeah, need to play with it. Think it might be too much for this game")
- **Off by default.**
- **In the pause menu's Display settings:** a **Miniature look** switch and a **Miniature strength** slider, which greys out while the switch is off. Stored in `settings.json` as `display.miniature` and `display.miniatureStrength`.
- **Live:** `Juice109Tilt` installs the pass once, inactive, and follows `Settings.Changed`. Strength maps the sharp band 0.32 → 0.06 and the blur 2 → 14 px.
- **Captures:** `-tiltshift on|off` still overrides it for a launch.

## 5 · Night bugs in the round-3 clip (Sky: "I see the windows flicker and there's a clipping / occlusion on the rooftop here")
Both were there before juice. Each shows in the round-3 clip's `-juice off` half, frame for frame.

**The window flicker: the café's stair glazing fighting its stone.** Sky was right: it's the window layer fighting the surface behind it.
- **Where:** the café's back stair bay (`DK_CafeUpper_3_TB`), the tall glazed strip behind the fin.
- **Cause:** `cafe_upper.py` builds that glazing as a box from y 11.28 to 11.30 m. Its outer face lies exactly on the stone's face (the spandrels, the stair stone and the head, all at 11.300 m), facing the same way, 0 mm apart.
- **Why only at night:** the glass is `Suborned/DecoKit/GlassSky`, which is transparent and writes no depth, so it is depth-tested against the stone at the same depth. Per pixel and per frame it won or lost. By day that was dark film on stone, so it was hard to see. Since Glint's 107 night port lit the panes, it flickers amber against stone.
- **The same fault as 097:** there, the tower slots' charcoal backing lay flush on the stone (fixed with `SLOT_BACK`, 1 cm).

**How I found it across the town.** A new Editor probe, `Editor/World/Review/WindowFightProbe.cs` (batchmode, read only, about 3 min):
- It tests every triangle drawn with a night-lit window material (`roadlight.json`'s `DK_GlassDark*`, `DK_Window_Tower*`, `DK_Window_Teal`, `DK_Window_Film`, `DK_Cabin`) in `OldTown.unity`.
- It samples each one on a 15 cm grid against every surface nearby.
- It counts a pair as a fight only if the surfaces are parallel, within a set gap, overlapping, facing the same way, and at least one of them writes depth. LOD siblings, shadow proxies and two transparent films don't count.
- Within 1 mm the town has these:
  - **the café's stair glazing:** 18 m² on LOD0, as above. This is what Sky saw.
  - **a Records Office service door:** 1.3 m² where its dark recess meets the stone reveal.
  - **old art (the old monorepo's pieces):** StreamBay/DecoEnd joints, a bay's glass on the next piece's stone or gold, 3 m² each; tower crowns, `Window_Tower` against `Window_Teal` or chrome, 1–2 m².
  - **the skyline towers (`Tower_Setback`, `Tower_Stepped`):** 0 mm, but these are the window box's top hidden inside the parapet box, so never seen.

**The fix, at the source, two ways:**
1. **Shader (live in this build).** `DK_GlassSky.shader` gets `Offset -1, -1`.
   - A pane laid on a stone face now always wins the depth tie. The bias is one depth step plus the slope, so stone even 1 mm in front still hides it.
   - It is one consistent rule for every GlassSky pane, including RoadLight's night copies: the café, the Records door and every kit facade pane.
   - Cost: none, since it is a raster state.
2. **Generator (not re-exported this round).** `tools/decokit/cafe_upper.py`:
   - The stair pane now stands 1 cm proud of the stone (`STAIR_GLASS`, inside the 5 cm chrome bars), the 097 approach.
   - The solarium roof's chrome edge sits 1 cm over the slab. Its top lay on the slab's top, which made a thin flickering line along the café's roof edge in the close clip. That surface is opaque, so the shader bias doesn't reach it.
   - Both take effect when the café is next exported (`bun run decokit` → `domes077_final` → `vertex_ao_extra --only CafeUpper_3` → `glows107_extra` → `bun run kit --extra out/decokit-107-extra`). That chain is Vela's, so I didn't run it.
- **Old art:** the URP Lit night windows have no shader to bias, and the old art has no generator here. I left those few square metres alone and listed them under Open.

**The rooftop "clipping": the cut-away under the desk's mouse, in captures only.**
- **What it was:** in Sky's crop, a building at the frame's bottom right stood open-topped, showing its inside walls, with a ghost-dither edge along the roof.
- **Cause:** the cut-away keeps a disc clear round the cursor's ground point (`HostTurn.FadePoints`, brief 016). That's right in play: the roof opens where you point.
- **Why it showed in the clip:** a capture has no player, so the "cursor" was the Studio's mouse, left wherever it rested over the game window. The cut opened the roof under it.
- **Not the near plane:** `PlayStandOff` keeps every roof clear of it, and frames with the cut on and off match once the mouse is away.
- **Fix:** `ShotRunner.Capturing` (set when the launch has `-shots`); `FadePoints` skips the OS cursor while it is set. Play is unchanged.

## 6 · Day flicker on the kerb and a roof (Sky on 0.2.330: "got flicker here on the curb and top here")
**How I looked:** a new Editor probe, `Editor/World/Review/SurfaceFightProbe.cs` (batchmode, read only).
- It generalises WindowFightProbe to every surface in a box round the café block (x -250..-70, z -100..80).
- It compares every pair within 2 mm that overlap, face the same way, and where one writes depth. That includes the same material across renderers, and each Mesh LOD level of a merged chunk against its neighbours.
- Result: 2,718 renderers, 4.3 M triangles, 8,068 pairs. **No renderer in the zone carries Mesh LOD levels**, so my 106 change isn't in this picture.

**The kerb: the contact strip lay on the kerb's light lens.**
- `ContactShade` (the baked contact shade, brief 026) lays its strip along every kerb's foot 4 mm up (`Lift = 0.004`).
- `road_ribbon.py` lays the kerb's strip-light lens (`DK_Road_Lens`) at `Z_LENS = 0.004`, the same height.
- The probe: `contact/kerb` against `DK_Road_Lens`, 0.0 mm, along every gutter (27 m² in the box).
- The contact strip is a multiply blend, depth-tested against the opaque lens at the same depth, so it won or lost per pixel. That reads as dark speckle along the kerb and gutter, crawling as the view moves.
- **Fix, at the source:** `ContactShade.Lift` 4 → 12 mm, above every inlay the kit lays on the ground (lens 4, hairline 7, pool 10 mm). The town is rebaked for it.

**The roof: shadow edges crawling, from the shadow fit, not a second layer.**
- The probe finds no coplanar layers on that roof (flat pairs above the street are only on the Records tower).
- The play view has one shadow cascade, its distance `focusDepth + far` (TacticalShadows, brief 096).
- `PlayStandOff` eases `focusDepth` every frame as the frame passes roofs of other heights, so while you pan, the cascade's size changes frame to frame.
- URP keeps a cascade on its texel grid only while its size holds. So every shadow edge re-sampled and crawled: the parapet's shadow along the roof edge, and the posts' streaks.
- With a still camera, and the fixed day sun (`Weather107`), it holds still. That's why the stills look clean and play doesn't.
- **Fix:** `TacticalShadows.depthStep = 10`. The fit takes the stand-off in 10 m steps, rounded up so the shadows still reach the street, and holds it while the stand-off moves within 1.5 steps below. Panning no longer refits the cascade. Cost: up to 10 m more shadow distance on about 70 m.

## 7 · The ending plays out (Sky on 0.2.330: "if popovic drives away and mission ends, you should see him get in the car and drive away with the driver - never just end abruptly")
**Cause:** the engine ends the run on purpose (brief 059 K1, Sky 3a) on the tick Popovic sits in his car. The view stopped there.
- **In the engine:** `Game.ValeTimeline` handles the moment he reaches the car and his `ValeInS` seconds pass. On that one tick it:
  - sets `Vale = InCar`;
  - notes `car.leaves` ("Popovic gets into his car and his driver drives him away.");
  - calls `Fail("vale-leaves", …, Because("driven-away", …))`.
- **Never simulated:** the driver getting in (`DriverAboard` stays false) and the car moving, since nothing steps after `Over`.
- **What the view showed:** the frozen tick. The fail camera's replay stops on the same tick, so you saw the driver on the kerb, the car in its bay, then the card.
- **No engine change needed:** the loss on that tick is the right outcome. Only the playing-out was missing, and that is view-only.

**The fix (view only):** `HostViews.LeavingOfficial.cs` plays it out on the wall clock from the frame the run ends.
1. **Popovic:** steps from his door into the car's side, gone inside the body. No fade.
2. **His driver:** walks round to the driver's door on the street side, past the near end of the car if he stood on the kerb side, and gets in.
3. **A beat:** the car has no opening doors (the "doors up" are its tailfins). Then it pulls out along the site's way out (`SiteWayOut`, shared with the lend). It steers round the cars drawn or waits behind one, using our getaway's driving logic, now shared as `ViewDrive`. Then it leaves the frame.

**The end card waits** (`HostEndPlayout.cs`):
- **Fail camera:** it follows the car and holds while it leaves (`failLeaveFrom`, capped at 20 s). The run as it ended doesn't play it twice.
- **Without the fail camera:** the card waits while Popovic's car or our getaway is still in view, up to 20 s. The view follows the car (`JuiceFollow`) until you move the camera. A click shows the card at once; Esc still opens the pause menu.
- **When it runs:** on for a player's launch, off for automation (`-endplay on|off`, `Juice109.Decide`'s rule), so proofs and checks see the card as before.

## Clips (30 fps; `*-side.mp4` is before on the left, after on the right; before = `-juice off`)
All in `briefs/109/latch/clips/`:
- `juice-searchlight-side.mp4`: night, the searchlight over the Office, with its lamp's glow.
- `juice-carlights-night-side.mp4`: night, the getaway pulls out and drives off: lens glows, beams, the pool, tails, streaks, brakes as it slows behind the rival car.
- `juice-carlights-rain-side.mp4`: rain: the beams and pools come on.
- `juice-streaks-side.mp4`: night, the sky lanes' streaks.
- `juice-win-watched-side.mp4`, `juice-win-heard-side.mp4`, `juice-win-turn-side.mp4`, `juice-win-call-side.mp4`, `juice-win-misled-side.mp4`, `juice-win-lent-side.mp4`: the social wins.
- `juice-hit-rival-side.mp4`: the Scout hits rival-1 three times.
- `juice-hit-ours-side.mp4`: rival-3 hits our Tech.
- `juice-ending-driven-away-side.mp4` (`shots4-ending.json`; before 0.2.330 `-juice on`, after 0.2.331 `-juice on -endplay on`): the route where nobody touches Popovic and his driver drives him off. Before: the run ends as he sits in, the driver stays on the kerb, the car stays in its bay, the card. After: the fail camera's replay, the driver walks round the front to his door and gets in, a beat, the car pulls out along the pickup's way with the camera on it (the 20 s clip ends as it drives off; the card comes once it's gone).
- `juice-day-kerbroof-side.mp4` and `juice-day-moving-side.mp4` (`shots4-day.json`; before 0.2.330, after 0.2.331, both `-juice on`): the kerb by the square's planter and the roof across the road, still and with the camera following the getaway over the zone's roofs.
- `juice-facade-night-side.mp4` (both halves `-juice on`; before = 0.2.328, after = this build): the café's back at night, close (zoom 9, step 1), following the Scout on the roof terrace. The stair glazing beside the fin, and the roofs next to it.

## Cost (Studio, Mac Studio M5 Max): `briefs/109/latch/costs.json` (`round4`)
| variant | frame mean (ms), 3 runs | Δ mean vs base | GPU (ms), 3 runs | Δ GPU |
|---|---|---|---|---|
| off | 4.33 · 4.05 · 4.27 | base | 1.70 · 2.44 · 2.60 | base |
| on, idle by day | 4.67 · 4.13 · 4.31 | +0.04 | 1.43 · 1.89 · 2.29 | -0.55 |
| social wins (one every 0.8 s, cycling the kinds) | 4.64 · 4.10 · 4.23 | -0.04 | 1.44 · 1.91 · 2.51 | -0.53 |
| hit flash (one every 0.25 s) | 4.53 · 4.27 · 4.33 | +0.06 | 1.53 · 1.80 · 2.77 | -0.64 |
| shots (one every 0.15 s; with the flash) | 4.42 · 4.12 · 4.49 | +0.15 | 1.68 · 2.33 · 2.61 | -0.12 |
| night-off | 6.21 · 6.07 · 6.31 | base | 3.52 · 3.15 · 3.54 | base |
| night, on (city + parked cars' lights) | 6.15 · 6.12 · 6.47 | -0.06 | 1.54 · 2.78 · 3.43 | -0.74 |
| drive-off | 6.32 · 6.53 · 6.69 | base | 1.70 · 1.35 · 1.65 | base |
| night drive-off, on (car lights: glows, beams, pools, tails, streaks) | 6.91 · 6.75 · 6.43 | +0.23 | 1.51 · 1.69 · 1.75 | +0.05 |

- **Setup:** 1920×1080 windowed, play zoom. Day and night are route-a-board at 0:40; the drive is route-a-board at the Go at night (`perf-drive.json`). Three launches each, medians. Load average was 5–8.
- **Every effect is within the noise on CPU (±0.15 ms), under the 0.3 ms aim.**
- **One cost found and fixed:** the car lights' first pass cost **+0.67 ms** of CPU while driving at night. Every lens flare component was on, with occlusion sampling, and off-screen cars were still processed. Now a dark lamp's component is off, car lamps take no occlusion (top-down, a lamp is rarely hidden), and off-screen cars are skipped. Re-measured: **+0.22 ms**, inside the 0.3 ms aim.
- **GPU:** readings swing run to run here, so the GPU deltas are noise.
- **Air:** not run (ask Aster).

## Checks
- `bun run batch --sky` (0.2.329): compile ok; host logic **208 pass, 0 fail**; kit and world ok; the build's smoke ok (OldTown with the host, the briefing, play to tick 40, the HUD drew 20 elements, the pause menu, no exception).
- `bun tools/game/runtime-compile.ts`: compiles (1 warning, not new).
- The window flicker, frame by frame (`juice-facade-night`, 120 frames at 30 fps, both `-juice on`):
  - **Method:** a pixel counts as flicker when it jumps by more than 35/255 into a frame and back out of it the next frame.
  - **The stair glazing:** **608 px before (0.2.328), 0 after (0.2.329).**
  - **What still flickers:** 865 px over the whole frame, from the café roof's chrome edge line (generator fix; it goes live with Vela's re-export) and from moving people and petals.
- Earlier this round: the full `bun run test` had 12 fails. One was mine (the comment lint, fixed); the other 11 are in suites this round didn't touch.

## Files
**New**
- `Juice109Lights.cs`
- `Juice109Fx.Hit.cs`
- `HostViews.Juice109.cs`
- `FrontEnd.Juice109.cs`
- `Generated/Juice109/Juice_Beam.shadergraph`, `Juice_Streak.shadergraph`, `JuiceLight.hlsl` (+`.meta`)
- `briefs/109/latch/shots4.json`, `shots4-facade.json`, `route-a-board.rain.json`, `perf-drive.json`, `round4.md`
- `Editor/World/Review/WindowFightProbe.cs` (the window/surface fight probe, `Run`; `Lods` and `Box` for one piece)

**Changed**
- `Juice109.cs`: Lights installed; `SocialWin`, `HitFlash`.
- `Juice109Fx.cs`: the old flares gone; the demos.
- `Juice109Fx.Turn.cs`: `SocialWin` by kind, the warm gold.
- `Juice109City.cs`: the beam shader, the lamp glow, the streak shader on the sky lanes.
- `Juice109Tilt.cs`: the setting, live.
- `HostJuice109.cs`: `JuiceLog` maps the social wins; the hit flash in `JuiceShot`.
- `HudToolkit.Juice109.cs`: every social win's chip pops.
- `Settings.cs`: `display.miniature`, `miniatureStrength`.
- `FrontEnd.Panel.cs`: one call.
- `Editor/World/Play/Juice109Assets.cs`: every vehicle's lamps; the Beam and Streak materials.
- `Editor/World/Play/Juice109Authoring.cs`: the two graphs in the test.
- `tools/juice/author.ts`: `lightGraph`, `JuiceLight.hlsl`.
- `tools/game/proofmap.json`: `HostViews.Juice109`.
- `briefs/109/latch/clips.ts`: `--round 4` perf variants.
- `Shaders/DecoKit/DK_GlassSky.shader`: `Offset -1, -1` (the film wins a depth tie).
- `ShotRunner.cs`: `Capturing`; `HostTurn.cs`: no cursor cut while capturing.
- `tools/decokit/cafe_upper.py`: `STAIR_GLASS` (1 cm proud), the solarium's chrome edge 1 cm over its slab.

## Open
- **Vela:** re-export the café (`CafeUpper_3`) through the extra chain so the generator's 1 cm offsets go live. The roof edge's chrome line needs that; the stair glazing is already held by the shader. The old art's few 0 mm window joints (StreamBay/DecoEnd, tower crowns; `WindowFightProbe` lists them) have no generator here. Bring them over piece by piece if they show.
- **Glyph:** review the pop sizes on the social chips.
- **Chime:** sounds for the social wins and the hit flash.
- **Volumetric Light Beam:** a trial only if shadowed beams become a goal.
- **Searchlight placement:** a searchlight's roof is picked at night's start for the view of that moment. If the camera moves far, a shaft can end up across the play; it's dim now, but Sky should judge it.
