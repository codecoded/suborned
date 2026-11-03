# Brief 070 · Glint · a day-to-night cycle on the mission clock (assessment, no build)

Glint, 2026-10-26, for Aster. Sky: "we have natural day / night cycle if that's easy enough, have it dark by 6:30pm perhaps?"
Read from code and data only. No Unity edits, no MCP calls: Latch is in the editor.

## Verdict

**Fairly easy: medium, not small.** Every part of the look is already realtime. There are no lightmaps, light probes or baked shadows in the Harbour scene. The only baked things are the reflection probes and cubes, and they are already dimmed at runtime. Most parts already blend: the evening phase blends the sun, ambient, reflections, haze, sky, grade, road strip, windows, people and signals by `LightPhase.Dusk` (0..1). The work is the second half. Night is a hard switch today (`LightPhase.Night` is a bool, and six files branch on it). It needs to become a second 0..1 value (call it `Dark`) on the same code pattern, and both values have to be driven from the sim's tick instead of an ease on wall time.

On cost, the cycle itself adds microseconds of CPU a frame. The open question is what the night look costs on the Air, because that has never been timed. The Air run should come first (step 0 below).

## A clock mismatch Aster must settle first

The Handover's clock is **m:ss elapsed**, not time of day. `contracts/missions/handover/mission.json` clock.ruling (Sky, 2026-10-15) says 0:00 = **17:24:30** city time, so the 5:30 chime is 17:30 and Vale reaches his car (6:10) at **17:30:40**. GDD v0.27 §10's briefing stations are m:ss offsets ("5:30 the handover chime … 6:10 Vale reaches his car").

- Read literally ("dark by 6:30 pm" = 18:30 city), dark would come 66 minutes into a mission that ends at 8:30 to 12:00. The cycle would be invisible.
- Main's reading ("dark by 6:30 on the mission clock") gives dusk to dark in about 6.5 real minutes. That is a time-lapse about 10 times faster than a real dusk. As a look it is fine, and I recommend it. But anything that shows the city's time of day ("5:30 pm" on the briefing, or "Evening, 5:30") would contradict the ruling.

Ask Sky which he means. If it's main's reading, the clock ruling or the briefing wording needs a line (Waymark / Fable through Aster). This is a GDD §07/§10 mismatch, not a lighting choice. Below I assume main's reading: m:ss 6:30 is dark.

## What can blend live

| part | today | in a cycle |
|---|---|---|
| Sun: angle, colour, intensity | Day State → lens.json `evening` by Dusk (`LightPhase.ApplyDusk`: lerp and slerp). Night snaps to the moon. | Lerp day → evening → **sunset** (intensity to 0 around 5:45), then fade the moon in on the moon's own rotation. The direction changes only while the intensity is near 0, so the shadows never visibly jump. The main-light shadow map renders every frame anyway, so moving the sun costs nothing. |
| Ambient (trilight) | Lerp by Dusk. Night = `NightFloor` values. | Lerp evening → night floor by Dark. `DynamicGI.UpdateEnvironment` is called only at the ends of an ease today. Keep it throttled (about once a second, or every 0.05 of t), never every frame. |
| Haze / fog | `DistanceHaze` leans by Dusk. At night the fog is the scene's own night value (`if (!Night)`). | Lerp the fog colour, start and end toward the night State by Dark. |
| Post grade | Day and night are two global Volumes, toggled with `enabled`. The evening is deltas on AtmosphereLens's tier volumes by Dusk. The night lift is a volume at weight 0/1. | **Crossfade the volume weights** (day 1−Dark, night Dark, `lens/night` weight Dark, the tier and play overrides scaled by 1−Dark). Both profiles have the same six overrides (ColorAdjustments, Bloom, Vignette, Tonemapping, WhiteBalance, SMH), and both tonemap with mode 2, so URP interpolates them cleanly with no enum pop. The grade is the LUT, so a crossfade costs no GPU. |
| Sky | Day material plus AtmosphereLens's evening sky globals by Dusk. Night is a separate `sky-night` material on the same shader (`Suborned/SkyGradient`). | `Material.Lerp` into one runtime copy (same shader), or lerp the gradient globals. |
| Reflections | A custom global cube (day and night cubes) plus box probes with intensities scaled at evening and night. | Lerp the intensities. **Swap the global cube** day → night at sunset, when the reflection intensity is lowest (a cube can't blend). Option: the builder also makes an evening cube. `ReflectionCube.Make` generates cubes from the sky gradient, so a third one is cheap at build time. |
| Lamp pools (390 built: street 364, table 8, interior 3, awning 4, palm 6, office 5) | All off by day. At night the nearest **28 within 45 m** of the focus are lit, re-picked every 0.2 s. | Each lamp fades in over its own 20 s window, staggered between about 5:30 and 6:00 by a hash of its name. The city's lamps switch on one by one, and the result is the same every run. Keep the cap: dusk never has more than 28 live, the same as night. |
| Emissives (road strip, lit windows, signs, table opal, people's lift) | `RoadLight` eases each toward its day, evening or night level by `Current`. | Drive the level from (Dusk, Dark). The window share (`_Lit`) rises with Dark, so windows light up progressively. |
| Signals (crossing heads, silhouettes, capsule rim) | `SignalLight.PhasedColour` / `HeadEmission`: lerp day → evening by Dusk, then night **switches whole**. | Add an evening → night lerp by Dark in linear (a few lines, the same shape as today's). They still draw unlit and outside the grade, so their readability doesn't depend on the light. |

## What is baked, and what happens to it

- **Lightmaps: none.** The scene has baked GI enabled but holds no LightingData, and every light is realtime. Nothing to lose.
- **Light probes: none.** The ambient is trilight from RenderSettings, which is live.
- **Baked shadows: none.** The sun casts realtime shadows (4096, one cascade). `ContactShade` is geometry, independent of the sun's direction, so it stays right at every hour. `ShadowProxies` are only shadow casters.
- **Reflection probes and cubes: baked by day** (`Probes.cs`, sun as light.json sets it). They keep the day picture, and their intensity is lerped down (evening probeScale 0.85, night 0.45). At night chrome mirrors a dimmed day street, as it already does in today's night look. Acceptable at zoom 14; unchanged from today.

## Frame cost on the Air (15" M4)

Baseline (briefs/064/air-0122/README.md, build 0.2.122, 2880 × 1864): warm run 2 means 21.0 / 21.9 / 23.3 ms (p99 26 to 31 ms) against 16.7 ms. The Air is GPU-bound (main thread 6 to 9 ms). The cycle can only make this worse where the night look costs more than day.

- **The cycle's own per-frame work:** about 10 RenderSettings and light writes, three volume weights, one sky material lerp, a few material property writes (RoadLight already uses runtime material copies), and SignalLight already evaluated per frame. All CPU, well under 0.1 ms, and no GPU.
- **Day to about 5:30 (evening half):** costs what day costs. The grade is a LUT, and the sky and haze are the same shaders.
- **About 5:30 on (lamps, windows, pools):** this costs what night costs today. **Nobody has timed it on the Air.** Code review 064 counted "391 lights". The real figure is 390 built, but at most 28 enabled, all unshadowed, no cookies, range 3.2 to 6.5 m, on Forward+ (PC_Renderer rendering mode 2). Note that light.json `rules.renderer` still says Deferred, which is stale. My estimate is +0.5 to 2 ms GPU at 2880 for the 28 small lights, plus the additive road-pool quads and bloom on more emissives. That is an estimate, not a measurement.
- **Dusk is not worse than night:** the cap holds, so the fade-in never has more than 28 lights on at once.
- **A saving to offset it:** at night the moon still renders the full main-light shadow map (535 to 889 casters, 4096). Its shadows are faint (shadowStrength 0.6, intensity 0.42). Turning main-light shadows **off once the moon is up** (after the sunset swap) skips the shadow pass. On a GPU-bound Air that probably saves more than the lamps cost, but Glint has to judge the look first: contact shade still grounds the figures.
- **A hitch to avoid:** RoadLight scans the figures and copies their materials the first time the look leaves day. In a cycle that happens during play. Do the scan behind the briefing (at Begin) instead.

## Recommended version

One function of the mission clock, `look(tick)` → (Dusk, Dark), keyed in lens.json (a new `cycle` block, Glint's data, all [S]):

| m:ss | look |
|---|---|
| 0:00 | late afternoon: Dusk 0.25 (sun already lowering, warm), Dark 0 |
| 4:30 | evening complete: Dusk 1 (lens.json `evening`), windows starting |
| 5:30 | the chime: the sun near the horizon, the first lamps on, Dark 0.3 |
| 5:45 | sunset: sun intensity 0, the reflection cube swaps, the moon fades in, main-light shadows off |
| 6:00 | every lamp has faded in (staggered by id), Dark 0.75 |
| 6:30 | full night (today's night look with its floor and lift); stays to the end, so the getaway and late fights are at night |

Rules:
- **The look reads only `Game.Tick / Game.Hz`** and writes nothing back. The same orders give the same run, and replays stay byte-identical. No rule reads light (GDD §10). Pause freezes the light, because the clock stops. Today's ease runs on unscaled time, and that ease goes away. A pure function of the tick also survives replay seeks and jumps with no ease state.
- **Change the evening sun's travel for the cycle.** Today the evening sun slerps from yaw 330 to 120, a 150° swing. Over 4.5 minutes the shadows would visibly sweep round every object. In the cycle the sun should mostly drop (pitch 38 → 10) and swing about 40° at most. The static `evening` values remain for the `-evening` proof frames.
- The proofs keep fixed looks: `-night` / `-evening` become `-tod <m:ss>`, which pins the look at a clock time (ShotRunner, Proof118, Shots033, TpLongRun, HudToolkit, GameBoot).

## Risks

1. **The clock reading** (above). Time-lapse versus city time is Sky's call through Aster.
2. **Night cost on the Air is untimed.** If the night rows come in more than 1 ms over day, cut `liveCap` (28 → 16) or the street lamps' range before shipping the cycle, or drop the moon's shadows.
3. **Crossfade states that were never seen:** the frames halfway between evening and night have never been looked at (crushed walls, Glyph's 0.2.113 finding, olive greens). Glint has to look at frames every 30 s from 5:00 to 6:30 at yaw 30 and 210.
4. **Shadow swim** while the sun moves: possible edge crawl on a single 4096 cascade. Keep the sun's travel small, and look at a 60 s capture.
5. **Signals:** the heads and the capsule rim must hold their colours at every Dark between 0 and 1, not only at the three keyed states. Check them in the 5:30 to 6:30 frames.
6. **Late-run readability:** the getaway and late fights are now always at night. Before, night was a choice. The people's emissive lift and the signals carry them, and Suture / Glyph should check it in play.

## Work and size

**Step 0 (Latch or main, about 1 hour, before anything else):** add night rows to the Air run (`briefs/064/air-0122/run-air.sh`: play-opening and tp-street with `-night`) and time them warm. This answers the cost question.

**Glint (about 1 day):** a lens.json `cycle` block (keys and m:ss stations, the cycle sun path, the lamp stagger window, moon shadows on or off). signals.json values between evening and night if needed. A roadlight.json mapping from (Dusk, Dark). Fix light.json `rules.renderer` (Forward+). Then frames every 30 s at yaw 30 and 210, a check of the signals across the band, and a sign-off with Latch in game.

**Latch (about 2 to 3 days), files:**
- `Runtime/Play/LightPhase.cs`: replace Set / SetEvening and the wall-time ease with `SetClock(seconds)` → Dusk and Dark. Blend the day and night States, swap the cube and the moon at sunset, throttle `UpdateEnvironment`, add the lamp stagger by id hash under the cap, and turn moon shadows off. Keep `Night` as Dark ≥ 1 and `Current` as derived, for the readers that switch whole.
- `Runtime/AtmosphereLens.cs`: volume weights by Dark (day grade, night grade, `lens/night` lift, the tier and play overrides fading out).
- `Runtime/DistanceHaze.cs`: lerp the fog toward the night value by Dark.
- `Runtime/RoadLight.cs`: levels from (Dusk, Dark), and the figure scan at Begin.
- `Runtime/SignalLight.cs`: an evening → night lerp by Dark.
- `Runtime/Play/HostNight.cs`: drive SetClock from `Game.Tick` each frame, remove the PlayerPrefs choice, add `-tod`. `Runtime/Play/Briefing.cs`: remove the Day / Night words (already in hand for 070).
- The proof callers (`ShotRunner`, `Proof118`, `Shots033`, `TpLongRun`, `HudToolkit`, `GameBoot`) move to `-tod`.
- Optional: `Editor/World/Light/NightPhase.cs` / `ReflectionCube` builds an evening cube.

**Others:** Redline reviews the per-frame writes and the cost rows. Suture confirms replays stay byte-identical (the sim is untouched, so this should be routine). Glyph checks the HUD over the night band. If Sky picks main's reading, Aster updates the §10 look line ("open (brief 070)").

**Total:** about 4 to 5 crew-days, and one Air run up front that decides whether the night half needs a budget cut.
