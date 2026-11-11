# 107 · Weather and night in the old town: results (Latch, 2026-11-11)

**In short:** the old town has a real night now, and three weathers besides clear: overcast, rain and mist. Each game rolls its weather and light from its seed (Keel's engine 0.54.0). The briefing's Day / Night gives the game's order. Replay keeps the look.
- **Night:** the moon, the lamps, lit tables and rooms, glowing planter feet, and the day's baked bounce taken down. The batch now fails a night shot that is drawn as day.
- **Rain:** wet ground and puddles in our shaders, falling rain and splash rings, and lamp reflections smeared on the wet. All of it draws under the rings.
- **Mist:** a ground mist pass of our own.
- **Cost on the Air at full screen:** rain about +1 ms, mist +0.7 to +1.1 ms, night about +0.6 ms, overcast free. Every look stays above 60 fps.

Build for Sky: **0.2.314** (c064, f1c806eb9+), `out/build/play/The Suborned.app`, made by `bun run batch --sky --then-full --skip unity-world` in 254 s:
- sim 490/490; host logic 202/202; build in 23 s.
- The smoke was OK: OldTown with the host, the briefing, play to tick 40, 20 HUD elements, the pause menu, no exception.
- The town rebake was skipped because the scene on disk was already this kit and this code (the round's one rebake, below).
- The full batch runs behind (pid 81370, `out/batch/full-behind/verdict.json`). It rebakes and runs the proof and the shots, night check included.

## 1 · Night in the old town (Glint's fix)
- **The bug, confirmed:** at the start, `HostNight.SetNight` found no LightPhase, quietly stayed day, and set `NightLook = false`.
  - On 0.2.314-scratch before the fix, the night shots measured −1.8 % and −4.9 % against their day pairs (brighter, not darker).
- **One driver of the play light:**
  - `PlayView107` (Runtime) draws `Game.Light` and `Game.Weather`, or an override (a shot's night, `-night`, `-weather`).
  - It sends the play camera's OldTownPlayLook the message `PlayLight107` (night, sun ×, ambient ×, desaturate, shadow ×, night exposure). The OldTown assembly and Runtime don't reference each other; GradeReview already crosses that line by reflection.
  - `OldTownPlayLook.Weather107.cs` writes the look's own sun, ambient and probe from TimeOfDay097's night at 21:30: the moon and the dark trilight.
  - It also places warm shadowless lamps at the old town's lamp heads, lighting the nearest 48 to the view and none within 2.5 m of a carried lamp.
  - At night it switches the play grade off in favour of the carried night grade. While TimeOfDay097's dev clock (`-tod`) runs, it writes nothing.
- **A LightPhase in the old town:**
  - `SwapNight107` carries HarbourDistrict's `lighting/phase` across before the swap drops the lighting root. That brings the night state, the night grade and the night lamps.
  - The 46 lamps inside the Handover zone are kept and the 237 outside are dropped (swap log).
  - The phase keeps the old town's own day (`LightPhase.sceneDay`) and drives no sun.
  - RoadLight, SignalLight's night colours, the globe opal and the glows read it as in HarbourDistrict.
- **The day's bounce at night:**
  - The zone's lightmaps hold the day's indirect light, so night first read as dusk (16–19 % darker).
  - Our three lit shaders (`DK_LitCutaway` ours and the old art's, `FacadeLit`) now scale the surface's indirect light by `_SubornedNight`: `weather107.json` `nightIndirect` 0.6. The moon and the lamps are direct light and stay.
  - The night exposure is `nightExposure` (0): the carried night grade has its own exposure, and TimeOfDay097's +1 stop on top washed the frame out.
- **Lit rooms:** `look097.json` rooms `nightShare` 0.5, `night` 1.4. Look097 now publishes the rooms dark, and PlayView107 lights them at night only. Before, the global was the same by day and night.
- **No hidden fallback:**
  - `SetNight(true)` in a scene with no LightPhase logs an error and returns false.
  - ShotRunner fails a shot that asks for night and can't have it.
  - The briefing hides Night where the scene can't draw it.
- **The batch's check:**
  - `tools/game/nightcheck.ts`: every `<x>-night` shot with a `<x>-day` pair must be more than 10 % darker in mean luma, or the shots step fails. `batch.ts` calls it in one line and its line count went down.
  - Now: **cafe-front-night 38.7 % darker, start-overhead-night 34.1 % darker**.
  - Pictures: `stills/start-overhead-pair.jpg`, `stills/cafe-front-pair.jpg` (day left, night right), and the full PNGs beside them.
- **The look:** it is lighter and cooler than Aster's concept (`concepts/night.png`). Glint can tune it without a rebuild: `nightIndirect` and `nightExposure` in `weather107.json`, or a loose copy passed with `-weatherJson <file>`.

## 2 · Rain
- **Wet ground and puddles: `Shaders/DecoKit/DK_Wet107.hlsl`, one global (`_SubornedWet`), no keyword, no texture.**
  - Every dry day it returns at once.
  - Non-metal surfaces darken by up to 28 % and gain gloss; walls take less; metal only beads. Smoothness couldn't be the test, because the civic paving's is 1.
  - Puddles: a world-space noise mask on flat ground at the plan's levels (street, deck 9.43, concourse 24), within a kerb's height. Planters and roofs never collect.
  - Ripples: one drop per 0.8 m cell, on the unscaled clock.
  - Hooked into our forward pass (`DK_Forward097.hlsl`), into the old art's `DK_LitCutaway` (a macro after URP's own surface read, in the forward pass only) and into `FacadeLit` (`FacadeForward.hlsl`), which draws the zone's merged ground.
- **Falling rain, splashes and lamp reflections: `Rain107` + `Resources/Shaders/Rain107.shader`.**
  - Three static meshes animated on the GPU, so three draws in all:
    - 4,000 streaks (0.8 m long, 2.6 cm wide, pale);
    - 700 splash rings;
    - one soft vertical smear on the wet ground under each lamp head, toward the camera, warm cream only. Glint's rule: never a signal's hue.
  - The drops are anchored in the world, so they don't swim when the view pans. They fill a box round the focus that grows with the zoom.
  - They draw at queue 3005, before the ground rings (3010 and 3011), so **the rings and outlines stay on top**. The signals draw after post anyway.
- **Two departures from the brief:**
  - **Not VFX Graph:** the package isn't in the project, and a `.vfx` graph can't be written from code. This is the same GPU-only idea in our own shader, with no particles and no per-drop CPU.
  - **The reflections are ground-laid smears, not URP DecalProjectors:** they draw in our own shader, so no decal shader has to ride in the build. The coral bands get no smear yet; I had no generic way to find them.
- **Light:** rain dims the sun to 0.35, softens the shadows to 0.35 and cools the ambient a little, plus a light mist (0.25). Overcast is the same light with no water.
  - OldTownPlayLook took the lens's shadow strength over its own field, so I added `shadowScale` on top. Without it, overcast's softer shadows never showed.
- **Pictures:** `stills/board.jpg` (Studio):
  - top left: rain by day;
  - top right: rain at night;
  - bottom left: third person in rain;
  - bottom right: third person in mist.
  - The Air's own frames are `stills/air-*.jpg`.

## 3 · Ground mist
- `MistPass107` + `Resources/Shaders/Mist107.shader`: one full-screen pass of our own on the Render Graph, enqueued on the main camera only, **before transparents**.
- **How it works:** it reads the scene depth and finds each pixel's world position (the orthographic play camera too). Then it lays mist by the pixel's height over the level below it, plus drifting world-space noise and a faint height haze.
- **The people stay clear:** above knee height the mist is capped at 15 %, so the 80 px people keep their feet. It thins to nothing within 2.5–8 m of the focus.
- **What draws over it:** the rings, the outlines and the signals.
- **Off when there's no mist:** at amount 0 (every clear day) nothing is enqueued.

## 4 · Weather and light from the start state (Keel's 0.54.0, already merged here)
- **New game:** `HostLook.LookForBegin` calls `RollLook(PlayInputs.Rules)` after the seed, **only when that seed was rolled**.
  - A pinned or given seed (proofs, batch rows, pins, `-seed n`, tests) plays its start byte for byte.
  - The first `--sky` caught this: host logic failed 2 of 202 (`Why100`: "the why changed the run"). The loud pin, seed 3, had gained a rolled weather in its start. It is fixed, and the rerun passed 202/202.
- **Replay and Restart:** they keep the game's own look (`LookKeep`, beside DialKeep).
- **Briefing:** Day / Night gives the game's `light` order (`ChooseLight`), so the choice is saved and replayed. The PlayerPrefs choice is retired; the roll decides.
- **The view:** draws `Game.Weather` / `Game.Light`.
- **Dev flags:** `-weather clear|overcast|rain|mist` and `-night` (view only). Per `-tplong` row: `weather` and `night`.
- **Not built:** a weather picker on the briefing. That's a HUD piece for Glyph's design. Until then the weather is the roll's.

## 5 · Vela's night glows: one kit step
- **The kit step, by Vela's recipe:**
  - `bun run decokit --only` the five inlays (57 s);
  - `bun run kit --extra out/decokit-107-extra`: 33 rows changed, as Vela's dry run said: 14 planters, 5 inlays, 14 path only. 402 adopted, unchanged.
  - `Materials/Glow/DK_Glow_PlanterFoot.mat` (a copy of DK_Road_Pool, new guid, #FFD49A, mode 3, alpha 0).
- **RoadLight:**
  - two new modes, `night` (emission from black) and `alpha` (a Cross Glow pool's `_Alpha` from 0);
  - Vela's two entries in `roadlight.json`. The night frames show the planter feet glowing.
- **The town rebake:** the one rebake for this round (784 s). It carried the kit, the night carry and 106's network merge.
  - Two earlier tries stopped at once, with no bake time lost:
    - 106's full batch still had Unity open;
    - an assembly reference (PlayView107 named OldTown types), now split as described in part 1.

## Cost on the Air (MacBook Air M4, full screen 3840×2410, the 106 harness, `air/table.md`)
- Run at 13:53–14:00. Load was about 3, on AC power, with no thermal warning.
- Two interleaved repeats, 8 s windows after a 3 s settle. Each row sets the weather and the night.
- Aster asked for this run. Sky's earlier OK was for 106's measurement, so Aster should confirm it covers this one.

| view | look | median ms | p95 ms | GPU ms | triangles | SetPass | vs clear |
|---|---|---:|---:|---:|---:|---:|---:|
| play | clear | 10.0 | 12.8 | 15.4 | 1.24 M | 156 | — |
| play | overcast | 10.1 | 12.9 | 14.7 | 1.24 M | 157 | +0.0 |
| play | rain | 11.0 | 13.7 | 16.3 | 1.25 M | 160 | +1.0 |
| play | mist | 10.7 | 13.0 | 15.8 | 1.24 M | 157 | +0.7 |
| play | night | 10.7 | 13.5 | 16.1 | 1.27 M | 168 | +0.6 |
| play | rain at night | 11.6 | 14.2 | 17.4 | 1.28 M | 171 | +1.6 |
| tp-street | clear | 13.3 | 17.3 | 18.1 | 4.34 M | 317 | — |
| tp-street | overcast | 13.4 | 17.3 | 18.1 | 4.34 M | 317 | +0.1 |
| tp-street | rain | 14.6 | 18.9 | 18.7 | 4.35 M | 321 | +1.3 |
| tp-street | mist | 14.4 | 18.6 | 19.0 | 4.34 M | 317 | +1.1 |
| tp-street | night | 13.3 | 17.5 | 16.8 | 3.99 M | 313 | −0.1 |
| tp-street | rain at night | 13.7 | 18.2 | 18.4 | 4.00 M | 326 | +0.4 |

- Glint estimated 0.4–1.1 ms for the rain stack at 1080p. At the Air's full screen (about 2.6 times the pixels) we measured +1.0 to +1.3 ms.
- The worst frame median, rain in third person, is 14.6 ms (68 fps), and its p95 is 18.9 ms.
- Nothing is switched off.

## Readability
- Checked by eye on the frames, at play zoom and in third person. Glyph should do the one-frame check per state that Glint asked for.
- **Holding:** the cell's rings, the target rings and the ring all stay readable in rain, mist and night, and the people keep their silhouettes.
- **To watch:** the warm planter feet beside the guard's amber at night (Vela's note), and the streaks crossing a cone.

## Files
- **New, Runtime:**
  - `Runtime/Weather/PlayView107.cs`, `Weather107.cs`, `Rain107.cs`, `MistPass107.cs`;
  - `Runtime/Play/HostLook.cs`;
  - `Runtime/OldTown/OldTownPlayLook.Weather107.cs`.
- **New, Editor:** `Editor/OldTown/Swap/SwapNight107.cs`.
- **New, shaders and data:**
  - `Shaders/DecoKit/DK_Wet107.hlsl`;
  - `Resources/Shaders/Rain107.shader`, `Mist107.shader`;
  - `Resources/Atmosphere/weather107.json`;
  - `Materials/Glow/DK_Glow_PlanterFoot.mat`.
- **New, tools:** `tools/game/nightcheck.ts` and `.test.ts`.
- **Changed:**
  - `HostNight.cs` (rewritten), `HostFightLab.cs` and `HostSeed.cs` (one call each), `HudToolkit.cs` (the briefing tab), `ShotRunner.cs` (no fallback);
  - `LightPhase.cs` (sceneDay), `RoadLight.cs` (two modes), `Look097.cs` (rooms at night only), `OldTownPlayLook.cs` (partial, shadowScale), `TimeOfDay097.cs` (`Running`);
  - `TpLongRun.cs` (row fields, same line count) and `TpLongVariants.cs` (weather and night);
  - `SwapOurs.cs` (one call);
  - `DK_Forward097.hlsl`, the old art's `DK_LitCutaway.shader`, `FacadeForward.hlsl`;
  - `roadlight.json`, `look097.json`;
  - `contracts/world/kit.json` (the kit step);
  - `tools/game/batch.ts` (one line, −3 lines overall), `tools/game/proofmap.json`.
- **Evidence:** `briefs/107/latch/air/` (tplong.json, machine.log, table.md, run-air.sh, make_rows.py) and `briefs/107/latch/stills/` (git-ignored).

## Checks
- `runtime-compile` is clean. `bun run typecheck` is clean.
- Tests: nightcheck 2/2, the proof map's 73/73 with 0 unplaced, tp-long's 8/8.
- Unity: the rebake and three scratch builds compiled and ran.
- `--sky`: sim 490/490, host logic 202/202 (after the fix above), smoke OK.

## Open
- **Glint:** tune night (`nightIndirect`, `nightExposure`), the rain's values and the mist's colour in `weather107.json` against the concepts.
- **Glyph:** a weather picker on the briefing (today it is the roll's), and the per-state readability frame.
- **Chime's sounds:** after Sky picks them. **Umbrellas** (Forma and Pulse): later, as the brief says.
- **The coral bands' rain reflections:** not done yet.
- **106's full batch never finished.** My rebake asked it to stop at unity-match. It runs again behind this build.
- **The GDD:** it doesn't yet say that the look is in the start state (Keel's note). That's Aster's.

Next owner: Aster (commit), then Sky (play), and Glint (tune).

## Follow-up (Aster, the same day): the briefing owns both, and the dev flag is gone
- **Build:** **0.2.316** (c064, f1c806eb9+), made by `bun run batch --sky --then-full --skip unity-world` in 156 s.
  - Host logic 203/203 (a new test, below); smoke OK.
  - The full batch runs behind (pid 99937). 0.2.315 came between: its build carried the first fix below, and its full batch was superseded by this one.
- **Where the choice lives:** since brief 096 the briefing is the book's **Job page**, which had no Day / Night at all. The old facts-row tab is no longer drawn.
  - The start page now carries both choices at the right of its kicker line, while the clock hasn't run: **Day · Night**, then **Clear · Overcast · Rain · Mist** (`HudToolkit.Look107.cs`, called from `BkKicker`).
  - **The orders:** a word gives the engine's `light` or `weather` order (`ChooseLight`, `ChooseWeather`), so the engine owns both and a saved run replays them.
  - **The look:** the chosen word is in the book's deep teal with a 2 px line, the others in its soft ink. Night is left out where the scene has none.
  - Pictures: `stills/job-page-look.jpg` and `stills/job-page-look-2x.jpg`. The words are placeholders for Fable; Glyph should check the place.
- **Fixed, my own bug from 107:** in the old tab, my comment had swallowed `briefPhase[i] = w.Root; briefPhaseW[i] = w;`. Its highlight and the hide-Night rule never ran. Fixed, though that tab is no longer drawn.
- **Dropped:** the `-weather` dev flag. `-night` stays as the proofs' view override, and a `-tplong` row still sets `weather` for the harness.
- **New test:** `tools/game/hostlogic/HostLogic.Look107.cs` passes. On a given start with no look:
  - the briefing's weather and light orders change the game;
  - once the clock has run, the weather is refused;
  - the saved run verifies and replays with rain at night.
