# Brief 109 · Juice · round 3: the rest (Latch, 2026-11-11)

Sky: "continue with the rest". Moments 4–8 are built on top of round 2's fixes (no shake, no vignette, the takedown on the pips, the headlights locked, the drive-through fixed).

The rule for every moment is Sky's: **no screen-wide effects for events.** Feedback comes at the event, in the world, or on the bobble-head pips and the HUD's own marks. All of it sits behind the same `-juice` switch except the tilt-shift, which has its own. It is all view only, and replays are unchanged.

**The build for Sky:** **0.2.327**, `out/build/play/The Suborned.app`. `bun run batch --sky`: BATCH OK, logged in runs/builds/builds.json. Opened from Finder, the juice is on. `-juice off` shows the before; `-tiltshift on` adds the miniature look.

## The moments

**4 · Turning someone** (the engine's `turn.<approach>.turned`, at the person named in the log line). Everything is at the person:
- **Gold halo:** a 2.8 m halo round them that swells and fades over 1.2 s.
- **Ring and motes:** a 3.4 m gold ring opening at their feet, and warm motes rising.
- **Core:** the Shader Graph glow's warm core over the halo.
- **Why alpha-blended gold:** the first build used an additive warm glow, and in daylight on cream stone it washed to near white with no colour. The gold now uses URP's own particle shader, alpha-blended, so it reads gold on the pale paving. The additive core gives the light.
- **The pip:** the turn's done chip ("Inez turned") pops like a takedown's (round 2's bobble-head pop), and the doer's rail portrait bumps when it lands off screen.
- **Files:** `Juice109Fx.Turn.cs`; `HostJuice109.JuiceLog`; `HudToolkit.Juice109` (`JuiceChip`, `JuiceRail`).

**5 · Alert rising**, on the HUD's Alert pips only, never a screen tint:
- **The pop:** each lamp a rise lights pops to 1.9× and settles back with an EaseOutBack overshoot, under a cream flash that fades over 0.45 s. The lamps light one after another, 90 ms apart, when a rise lights two.
- **The flash:** it is a disc laid over the lamp, because the clock bar keeps setting the lamp's own colour every frame.
- **Falls:** a fall only dims, as before.
- **How it's built:** UI Toolkit transitions.
- **Files:** `HudToolkit.Juice109.JuiceAlert`, plus one call in `HudToolkit.Clock`.

**6 · Gunfights**, one call per engine shot (`HostFight.Shot` → `JuiceShot`), all in the world at the shot, no shake:
- **Muzzle light:** a pooled point light at the gun (6 of them, no shadows, warm), fading over 0.08 s, so the street around the shooter is lit for a frame or two.
- **Muzzle flash:** a glow at the gun.
- **Tracer:** a stretched streak from the gun to where the round went, at 140 m/s, using the Shader Graph glow.
- **Stone chips:** on a miss or into cover, grey-cream flakes with weight, kicked up and back toward the shooter. These are on top of Fx097's grit and chip marks.
- **Glass:** on a hit on a car or a camera, pale shards bursting and falling.
- **Unchanged:** the HUD's own star flash, miss tracer and sparks (`HudToolkit.Fire`).
- **File:** `Juice109Fx.Fight.cs`.

**7 · The living city** (`Juice109City.cs`). Each effect attaches to something the city already has, found by name at the start and never placed by hand:
- **Sky traffic:** a fading light trail behind each flying car of the old town's sky lanes (`SkyTraffic` units; 30 trails).
- **Neon:** three of the eight neon star walls (`DK_NeonStar_Wall`), chosen by place and the same three every run, stutter now and then: a 0.5 s dip and catch, each on its own 7–16 s.
- **The robots' faces:** a soft glow parented to each robot's face display (waiters, porters, butlers, shopkeepers and the patrol; 36), stronger at night.
- **Night searchlights:**
  - **Where:** at night, slow-sweeping beams from the tallest roofs within 160 m of the view's centre. This tilted orthographic camera sees a beam rise *up the screen*, so only roofs in the frame's upper part qualify: the shafts rise over the far city and never cross the play.
  - **Tuning:** a test beam at the view's centre whited out the café under it, so this is readability first. The beams are dim (tint 0.5).
  - **Stable choice:** ties are broken by place, so it's the same roofs every run.
- **Tram pantograph sparks: not built.**
  - No tram runs in the played scene: only the rails (`tram/T1`), the shelters, and a far tram's *sound* every 40–90 s (`HostAudio.Weather107`).
  - The world has no overhead wires to spark on (art bible: "rails flush in the road, no overhead wires").
  - When trams run as traffic, the sparks can ride them, at the trolley's contact with the rail rather than a pantograph.

**8 · The miniature look trial**, with its own switch: `-tiltshift on|off`, off by default whatever `-juice` says. Two tuning flags work without a rebuild:
- `-tiltBand`: the sharp band's half-height, 0–0.5 of the screen; default 0.16.
- `-tiltBlur`: px at 1080 at the top and bottom edges; default 7.

How it's built:
- **A screen-space band blur, not depth of field.** The play camera is orthographic, so its depth says little about near and far.
- **The shader:** a URP Fullscreen Shader Graph, `Generated/Juice109/Juice_TiltShift.shadergraph`, authored by `tools/juice/author.ts` from Unity's "Fullscreen Basic URP" template. Its colour chain is replaced by a Custom Function node from a file (`JuiceTiltShift.hlsl`: 16 taps on a golden-angle spiral). The blit source needs a file-scope declaration, which a string body can't make, so the function is a file. The two properties feed it.
- **The pass:** added for the launch as a Full Screen Pass after post-processing, so the HUD (UI Toolkit, drawn over the camera) stays sharp.
- **Unity's importer:** compiled with no messages, and `_TiltBand` and `_TiltBlur` are exposed (`authoring.json`).

## Shader Graph for the new effect shaders
The new shaders are Shader Graphs authored as text: `Juice_Glow` (round 1, used by every glow: flares, tracer, glass, turn core, sky trails, robot faces, searchlights) and `Juice_TiltShift` (new: a Fullscreen graph with a file-source Custom Function, the case round 1 didn't cover). Unity's importer compiled every graph, with no messages (`authoring.json`). The alpha-blended effects (turn halo and ring, stone chips, dust) use URP's own particle shader through Fx097's material. That is a material, not a new shader.

## Clips (30 fps, constant timestamps; `*-side.mp4` is before on the left, after on the right; before = `-juice off` unless noted)
All in `briefs/109/latch/clips/`:
- `juice-turn-side.mp4`: route-a-board 2:00.5, the Fixer's Turn (help) lands on Inez.
- `juice-alert-side.mp4`: the loud pin 2:33, the shot at the Office door raises Alert 2.
- `juice-gunfight-side.mp4`: the loud pin at the car.
- `juice-city-night-side.mp4`: route-a-board 0:40 at night.
- `juice-tiltshift-side.mp4`: before `-juice on`, after `-juice on -tiltshift on`.
- The fixes, re-captured on the final build:
  - `juice-takedown-side.mp4`
  - `juice-takedown-offscreen-side.mp4`
  - `juice-getaway-side.mp4`

## Cost (Studio, Mac Studio M5 Max): `briefs/109/latch/costs.json`
| variant | frame mean (ms), 3 runs | Δ mean vs its base | GPU (ms), 3 runs | Δ GPU |
|---|---|---|---|---|
| off | 4.16 · 4.16 · 4.12 | base | 1.81 · 3.46 · 3.18 | base |
| on, idle by day (sky trails, robot faces, neon) | 4.05 · 4.08 · 4.14 | -0.08 | 2.04 · 2.01 · 2.76 | -1.15 |
| turn (a flare every 0.8 s) | 4.20 · 4.09 · 4.17 | +0.01 | 1.71 · 1.75 · 2.84 | -1.43 |
| shot (one every 0.15 s: hit, miss, glass) | 4.23 · 4.78 · 4.17 | +0.08 | 2.60 · 1.44 · 2.83 | -0.58 |
| tilt-shift (-juice off -tiltshift on) | 4.16 · 4.84 · 4.14 | +0.01 | 3.60 · 1.64 · 3.51 | +0.33 |
| night-off | 6.01 · 6.10 · 5.99 | base | 3.39 · 3.30 · 2.95 | base |
| night, on (+ searchlights) | 5.96 · 6.04 · 6.25 | +0.03 | 2.41 · 3.36 · 3.49 | +0.06 |

- **Setup:** 1920×1080 windowed, route-a-board 0:40 at play zoom (`perf.json`; night is `perf-night.json`), 8 s with the clock running, three launches per variant, medians. Load average was 3–6.
- **CPU:** steady this time (off: 4.12–4.16 ms). Every moment is within ±0.08 ms of its base, so **none is measurable above the 0.3 ms aim on CPU**.
- **GPU:** the reading swings 1.4–3.5 ms run to run here, so its deltas are noise. The one plausible signal is the tilt-shift's **+0.33 ms** (16 taps over the full 1080p frame). On the Air at 2880×1864 that would scale with 2.6× the pixels, so it is the one effect to measure on the Air (with Aster's OK) before adopting it.
- **Worst cases:** the `-juicedemo` variants repeat a moment without end. An effect in play costs less.

## Checks
- `bun tools/game/runtime-compile.ts`: compiles.
- `bun run hostlogic`: 208 pass, 0 fail.
- `bun run typecheck`: clean.
- `bun tools/juice/author.ts --check`: up to date.
- Editor authoring test (`authoring.json`): every Shader Graph compiled, the tilt-shift included.
- `bun test` (proof map, proof delta, shots, runtime-compile): 84/89. The 5 "gate at the CLI" tests fail only on round 2's uncommitted deletion of `Juice109Cam.cs`, which `cli.ts` sees as "changed since the build". They should pass once Aster commits.
- `bun run batch --sky`: **BATCH OK, 0.2.327.**
  - compile ok, host logic 208/0, unity-world ok.
  - game-build ok, with the smoke ok: OldTown, the briefing, play to tick 40, the HUD drew 20 elements, the pause menu, no Exception.
  - It took 881 s, over the 600 s target: the new assets under `unity/Assets` tripped the world rebuild (731 s).
- **Clips:** all eight are from Sky's own 0.2.327 app, with no exception in any player log. Each is 30 fps with constant timestamps (ffprobe: 30/1; 120 frames for 4 s, 480 for 16 s).
- **Frames checked:**
  - the Alert pips pop to about twice their size in cream and settle back to orange;
  - the muzzle light washes the paving around the shooter for a frame or two, with a tracer to the target;
  - the tilt-shift blurs the top and bottom, with the HUD sharp;
  - the robot face glow;
  - a searchlight rises over the Office's upper floors, clear of the people;
  - the turn's gold halo and ring at Inez.

## Files
**New**
- `unity/Assets/Suborned/Runtime/Play/Juice109Fx.Fight.cs`
- `unity/Assets/Suborned/Runtime/Play/Juice109Fx.Turn.cs`
- `unity/Assets/Suborned/Runtime/Play/Juice109City.cs`
- `unity/Assets/Suborned/Runtime/Play/Juice109Tilt.cs`
- `unity/Assets/Suborned/Generated/Juice109/Juice_TiltShift.shadergraph`, `JuiceTiltShift.hlsl` (+`.meta`)
- `briefs/109/latch/shots3.json`, `shots-tilt.json`, `perf-night.json`, `round3.md`

**Changed**
- `Juice109.cs`: `Shot`, `Turned`, City installed.
- `Juice109Fx.cs`: partial, the fight and turn rigs, the demos.
- `HostJuice109.cs`: `JuiceLog` for turns, `JuiceShot`.
- `HandoverHost.cs`: the Drain hook back, for turns.
- `HostFight.cs`: one call in `Shot`.
- `HudToolkit.Juice109.cs`: `JuiceAlert`, turn chips.
- `HudToolkit.Clock.cs`: one call.
- `Editor/World/Play/Juice109Assets.cs`: the tilt-shift material.
- `Editor/World/Play/Juice109Authoring.cs`: the tilt graph in the test.
- `tools/juice/author.ts`: `tiltShift`.
- `briefs/109/latch/clips.ts`: `--before`/`--after`, the round-3 perf variants.

## Open
- **Sky to judge:**
  - the tilt-shift trial;
  - the searchlights' place and strength (only roofs in the frame's upper part; one qualified round the café at 0:40);
  - the neon flicker, which a 6 s clip may or may not catch, since each star is on its own 7–16 s.
- **Glyph:** review the Alert-pip pop and the turn chip's pop.
- **The turn's gold reads brown-gold, not warm light, at play zoom in daylight.** Alpha-blended gold over sunlit cream darkens it. A tuning pass would pick a lighter, yellower gold at lower alpha, or a thin bright rim instead of a filled halo. Sky's call from the clip.
- **Chime:** sounds for the moments.
- **Tram sparks:** wait for trams to run.
