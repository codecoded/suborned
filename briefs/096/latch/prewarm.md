# Brief 096 · Latch: the first third-person freeze, fixed by a shader warm-up (2026-11-06)

Sky on 0.2.252 (the Air): "yeah that 5 seconds jerk is bad....". Redline's audit item 5 (prewarm), already approved: `briefs/096/redline/render-audit.html`.
Build **0.2.253** (`bun tools/game/cli.ts --no-open`: built in 17 s, 0 errors, 71 warnings as before, draw check ok). Nothing committed.

## Cause

The play view draws with **one** shadow cascade (option B, commit 53db41f2), so its lit shaders use the `_MAIN_LIGHT_SHADOWS` variants. Third person's perspective camera has four cascades, so its first frame needs the `_MAIN_LIGHT_SHADOWS_CASCADE` variant of every material in view. Unity and Metal built them all in that one frame: 69 shader programs, 41 of 41 ForwardLit fragment programs cascaded. That frame took **6.1 s on a cold Metal cache** on the Studio.

Evidence:
- **Cold vs warm.** 0.2.252 `-viewTrace`: 6.3 s + 2.3 s at entry with the cache cold, 0.58 s with it warm (that frame includes a capture).
- **Shader log.** A development player with `GraphicsSettings.logWhenShaderIsCompiled` (`-hitchShaderLog`) logged 69 programs in the third-person phase, 1.2 s of Unity's own upload time. The rest is Metal's pipeline compile.
- **No other cause.** GC count rises by 1–2 per phase, the same as on the map. No asset loads in the log.
- **The cache is per build.** 0.2.252 run on a cache that the new build had just filled still froze for 4.5 + 1.5 s. So every new build Sky installs starts cold, not only a fresh install.
- Also cold, in the same way: the map's first quarter turns (1.0 s and 0.55 s) and the first conversation cutaway (0.45–0.49 s).

## Fix (the standard Unity 6 route: GraphicsStateCollection)

- **`unity/Assets/Suborned/Runtime/Play/ShaderPrewarm.cs`** (new, 127 lines). Loads `Resources/Prewarm/play-metal.graphicsstate` while the front end loads the street. Once the street scene is active, it warms 2 states a frame (`WarmUpProgressively`) on `Application.onBeforeRender`. The loading bar's last stage ("Plan", no new words) waits for it, at most `BarCapS` = 6 s. The rest warms behind the menu and the Job page.
  - A collection recorded for another API or platform is skipped. `-noPrewarm` turns it off for an A/B.
  - Recording: `-gscTrace file` traces from boot and appends to the file if it already exists. Tracing works only in a development player: a release player saved 0 states.
- **`unity/Assets/Suborned/Resources/Prewarm/play-metal.graphicsstate`** (new): 103 variants, 130 states. These are the states that third person, the drive, the cutaway and the map's turns add to the opening play view. The play view's own states still compile with the street's first frames, as before.
- **`unity/Assets/Suborned/Runtime/Play/FrontEnd.cs`**: three lines changed in place (the file stays 829 lines). The warm-up begins beside the street's load on both paths, and the bar waits for it.
- **`unity/Assets/Suborned/Runtime/Play/HitchTrace096.cs`** (new, 133 lines). The probe, `-hitchTrace dir`:
  - per-frame times with no captures, written to `frames.csv` and `hitch.json`;
  - map at four yaws, third person by Inez (walk, back, in again), third person by the driver, and route A's Listen cutaway;
  - `-hitchOrder convo` plays the cutaway first; `-hitchStop map` records the opening view only;
  - engine steps it takes to reach the scene count as `setup` and are not judged.
- **`unity/Assets/Suborned/Editor/World/Play/GameBuild.cs`**: `-scratchDev` gives a scratch build as a development player, for recording. Game builds are unchanged.
- **`tools/game/prewarm-delta.ts`** (new): full recording minus base recording, state by state. 64-bit ids are kept exact (a round trip is byte-identical as JSON).

What the warm-up must **not** do: start while the street loads asynchronously. In the front end's `LoadSceneAsync` the warm-up and the load waited on each other and neither finished. So it starts the frame after the street is active.

### Re-recording it (after a shader, material or camera change)

1. Scratch dev build: `Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Play.GameBuild.CliScratch -scratchBuild out/scratch/latch/prewarm/build-dev -scratchDev`.
2. Run that build twice with `-hitchTrace D -gscTrace full.graphicsstate -fastExit`, once with `-hitchOrder convo`. The second run appends to the first.
3. Run it once with `-hitchStop map -gscTrace base.graphicsstate`.
4. `bun tools/game/prewarm-delta.ts full.graphicsstate base.graphicsstate unity/Assets/Suborned/Resources/Prewarm/play-metal.graphicsstate`.

The recordings used are in `prewarm/` (`full`, `base`). The run scripts are `prewarm/run.sh` (move the cache aside, run) and `prewarm/person.sh` (front-end path).

## Cold Metal cache, Studio (M5 Max), 1920×1080, vsync off: max frame per phase

| first use | before (`-noPrewarm`) | after, 0.2.253 |
|---|---|---|
| third person (T), third first | **6,141 ms** | **28.6 ms** |
| third person (T), after a cutaway | **5,574 ms** | **11.7 ms** |
| conversation cutaway, cutaway first | **454 ms** | **13.0 ms** |
| conversation cutaway, after third person | (not reached in that run) | **13.7 ms** |
| first quarter turns (Q/E) | 987 / 554 ms | 5.2 / 4.4 ms |
| walking in third person | 21.5 ms | 20.3 ms |

The "drive view" here is the drive of an agent in third person (T), which is the same camera as third person. All phases after the warm-up are under 50 ms in both orders. The raw results are in `prewarm/hitch-*.json`.

## Start-up cost (front-end path, Studio)

| Metal cache | warm-up | loading bar waits |
|---|---|---|
| warm (every launch after the first of a build) | 1.1 s, 68 frames | **+0.18 s** |
| cold (first launch of a build) | 19 s, 508 frames | **+6.0 s** (the cap; then 69% left) |

On a cold cache the remaining 13 s warm behind the menu and the Job page. There, a frame stalls now and then; the longest was 1.0 s. The street's first frame is unchanged: 2.76 s, against 2.78 s on 0.2.252.

The warm-up compiles one state after another. Splitting it into 8 parallel collections was no faster (22.6 s against 21.6 s for all at once), and warming everything in one call freezes a single frame for 22 s. So it goes 2 states a frame.

Front-end runs used `-frontEndShots -novsync`. The Studio's display was asleep, and a vsync launch parks in `WaitForLastPresentation` and never presents.

## Open

- **The Air is not measured** (no sitting). Expect the cold warm-up to take longer than the Studio's 19 s, so more of it falls behind the menu after the 6 s cap. A player who reaches T in under about 20 s on a fresh build may still meet a smaller hitch. The cap is one constant (`ShaderPrewarm.BarCapS`). It is a choice for Sky: a longer wait on first launch, or more warming behind the menu.
- **What the recording doesn't cover is still cold:** night, fights (muzzle flash, effects), the fail camera, the fight range, and Options' free zoom at 60 (2.2 s cold on 0.2.252; a debug option). Add them with a probe pass and re-record.
- **SSAO runs in the conversation cutaway.** Its passes compile there, even though third person has SSAO off. That's for Glint / Redline if unintended.
- A shader, material or URP keyword change needs a re-recording. A stale collection only warms less; it never breaks.
