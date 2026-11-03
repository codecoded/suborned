# 075-A · the old reel's grade, light, sky, haze and water (Glint, 2026-10-28)

The old DecoKit reel's look, ported through our pipeline: the scene keys in `light.json` (the builder applies them), the runtime tiers in `lens.json` / `haze.json`, and two shaders copied over from the old project. Nothing is a lookalike. The old shaders are copied verbatim, the textures are the old files byte for byte, and the values are the old scene's. One deliberate restatement is explained under "Exposure match".

## Sources (read only)
- Grade: `~/Projects/the-suborned/game/Assets/TheSuborned/Art/DecoKit/Generated/DK_StreetGrade.asset` (sha256 1cc1a91f…), matching the light dump in `briefs/074/flyby/old-keys.json`.
- Sky: `DK_SpaceSky.shader` (f38bcd08…) and `DK_Sky_Street.mat` (40ca9140…), which are set by `DecoKitSky.Apply`.
- Sea: `DK_Ocean.shader` (70fcc0e8…), with the values from `DecoKitHarbour.Ocean()`.
- Textures: `Textures/Sky/DK_Sky_Noise.png` (7d40beff…), `DK_Skyline_Far.png` (ed2cb019…) and `DK_Sea_Normal.png` (055461da…).
  - They are copied to `unity/Assets/Suborned/Resources/Atmosphere/dk_*.png`, keeping the old import settings but with new GUIDs. This follows the precedent of `flare_starburst.png`.

## What was ported, and the values
| part | old | ours now | was |
|---|---|---|---|
| tonemap | ACES | ACES | ACES |
| post exposure | −0.8 at sun 3 | −0.063 at sun 1.8 (exact match, see below) | +0.02 |
| contrast / saturation | 10 / 25 | 10 / 25 | 10 / 25 |
| bloom | 0.8 / 0.6 / 0.85, tint (1, .96, .9) | 0.48 (= 0.8 × 0.6) / 0.6 / 0.85 #fff5e6: scene, follow and long | 1.1 / 0.22 / 0.6; follow 0.86 / 0.35; long 1.2 / 0.35 |
| shadows / midtones / highlights, vignette | none | neutral, 0 | shadows #d0d8ec, vignette 0.1 |
| sun | 3, (1, .93, .8), 44.7° up, shadow strength 1 | 1.8, #ffedcc, 44.7° up, yaw 330 kept, shadow strength 1 | 1.8 #ffe4c8, 38°, 0.68 |
| ambient | Trilight (.708,.729,.761) / (.755,.722,.673) / (.798,.735,.642) | the same colours at value 0.79 (about ×0.6 linear) | #dedce0×.69 / #faeadc×.71 / #e2c5a4×.41 |
| sky | DK_SpaceSky: teal zenith and mid, warm horizon haze, sun disc and aureole (the glare), cirrus, far-city band | the same shader with the material's values (`light.json spaceSky`); colours ×0.6 linear | Suborned/SkyGradient, cobalt |
| fog, long tier | exponential 0.0005, horizon × 1.5 | exponential 0.0005, #ebdbba (= 0.9 × linear #f6e5c3) | 0.0006, #fad9bd × 1.2 |
| depth of field, long tier | Gaussian 300–1800 m, radius 1.2 | unchanged (already ported in 048) | |
| sea | DK_Ocean: two wind layers of DK_Sea_Normal, teal body, back-lit crests, Fresnel sky reflection lifted toward the zenith, glitter at power 1200 × 40 | `Suborned/DK Ocean` with the old values (`light.json ocean`) | Suborned/Water, flat teal |

**Exposure match.** The old rig ran sun 3 under exposure −0.8. Every light value here is multiplied by 0.6 (1.8 / 3) in linear, and the exposure is raised by log2(3/1.8) = +0.737 EV. That covers the sun, ambient, sky colours, haze and bloom threshold.
- The image is unchanged for everything lit or emitted by the sun, the sky and the haze: stone, sea, sky and fog.
- The things the game calibrated at our exposure keep their fits:
  - the crossing heads
  - the silhouette rim, which must stay under the bloom
  - the day emissives
  - the night look
- Taking sun 3 literally was tried first. It darkened every unlit colour by about 1 EV: the heads went 10–18 dE off, and the silhouette rim would have needed linear values above 1, so it would have bloomed.
- The sky colours are solved channel by channel through Unity's sRGB-to-linear step on material colours, so the shader sees exactly 0.6 × the old values. The old stored values are kept in `spaceSky.coloursOld`.

**Play zoom (lens.json, two new keys, play tier only).**
- `bloomPlay` is 1.05 / 0.45 / 0.75 with the old tint.
  - The old reel's bloom is right for its own views, which are full of shade, sky and canyon.
  - At play zoom, 60–80 % of the frame is lit cream stone that crosses its threshold. The frame veils: median luminance went from 0.66 to 0.74 and saturation dropped by 0.03 (variants A–D in `out/scratch/glint/075`).
  - At 1.05 the glow stays on sunlit edges, chrome, glints and emissives, and stays above the silhouette rim (linear ≤ 1.0).
- `gradePlay.exposureDelta` is +0.083, which keeps the play camera's post exposure at 0.02 as before.
- The saturation, lift and greens curve of the play tier are unchanged; the play saturation is still 35.

**Sea reflection.** The old scene's default reflection was its skybox. Ours is the street-canyon cube that chrome needs (light.json `reflection.street`). With that cube the ported sea read black-navy.
- By day the sea now computes DK_SpaceSky's `Air()` verbatim in the reflected direction, using the sky's colours, which the builder copies onto the water material.
- At night (`_AtmNight`, set by AtmosphereLens) it falls back to the night cube.

**Evening.** It is re-based so its absolute exposure stays 0.30: `lens.json evening.grade.exposure` goes from 0.28 to 0.363. The ported sky takes the evening sun, glare and zenith blend through inert `_Atm*` hooks.

## Banding: where it comes from
- In the old stills, every sky gradient steps one 8-bit level at a time over runs of 4–6 px. Some steps move only one channel, which makes the faint tinted contour lines.
- I probed it on the port. The same flyby stills were rendered with the URP asset in memory at a 64-bit HDR buffer, and separately at a 64-point grading LUT:
  - the 64-bit buffer changed 7 % of sky pixels by at most 3 levels;
  - the 64-point LUT changed 35 % of sky pixels by at most 3 levels;
  - the step length stayed at 17–18 px in all three.
- So the banding is not the HDR buffer and not the LUT. It is the 8-bit encode of slow HDR gradients with no dithering (cameras have dithering off), plus the compressed noise sheet's terraces in the cirrus.
- Our pipeline is configured exactly like the old one: R11G11B10 HDR, LDR grading mode, LUT 32, no dithering, the noise sheet CompressedHQ. So the ported sky bands the same way by construction.
- (Superseded below.) To keep it deliberately: do not turn on camera dithering or HDR output, and keep the noise sheet's import as it is. No posterise was added.
- **Reversed, brief 075 banding (2026-10-28):** Sky calls the steps a defect. Every base camera that runs post now dithers (AtmosphereLens.Begin at runtime; Atmosphere.Camera on the built play camera). Probe (`Glint075 -mode band`, remap flyby t6/20/33/50 and the play view at 210): the mean run of one colour down the sky goes from 3.2-6.3 px to 1.25 px; frame means move by at most 0.01 of a level, so the grade and the golden stats are unchanged. HDR grading mode (LUT 32) was tried and rejected: over dithering it gains almost nothing (sky run 1.26 against 1.34 px at t33), and it shifts the sky (red about -13 levels), which would move the grade. The noise sheet loaded uncompressed changed under 0.4 % of sky pixels by at most 8 levels, so its BC7 import stays. LUT 32 and the R11G11B10 buffer stay. Crops: `remap/banding/`.

## Not ported, and why
- **The old HDRIs** (city, courtyard, studio, sunrise, sunset). The street reel never used them: its sky is procedural and its reflections come from that sky. The HDRIs lit the old studio and showroom only.
- **The old sun's azimuth** (−135° from the old boulevard). It belongs to the old street's frame. Yaw 330 keeps the civic square and the café terrace lit at play zoom.
  - The flyby's sea glitter therefore falls outside our views. `port-sun-ahead/` shows it with a demonstration sun (pitch 30, yaw 95) that is not the game's sun.
- **The sky's planet and moons.** Our horizon band already draws them as geometry, so their radii are 0 in the sky (they are not drawn twice).
- **A depth of field at play zoom or in the ground view.** It is not added: the long tier only, as before.

## Checks (offscreen, batchmode, `bun run guard`; `Glint075.cs`)
- **Play zoom**, office front and café square at yaw 30 and 210, day and night:
  - Files: `grade/today/`, `grade/port/`; sheets `playzoom-day.png`, `playzoom-night.png`.
  - By day: median luminance 0.67–0.79 against today's 0.66–0.76. Saturation 0.25–0.28 against 0.26–0.29. Shade is deeper, as in the old reel (old Trilight, shadow strength 1).
  - Night is unchanged: identical stats, fewer than 10 pixels per frame differ by more than 20 levels.
- **Crossing heads by day** (3 heads × walk / clearing / stop × 4 yaws, at 2160, `grade/*-heads/heads-measured.json`):
  - Every shot that measured within 3 dE today still does under the port, at 1.6–2.8 dE. No refit was needed.
  - The off rows are the same in both: h1/h2 walk read a vivid blue, and every y030 reads orange. The 1 % "most vivid" probe is picking up another object in the rect, not the head.
- **Flyby** (Flyby074's keys, long tier with the lens and haze installed): `grade/flyby-grid.png` shows old / today / port at 6, 20, 33, 50 and 61.5 s. `grade/sea-sun-ahead.png` shows the glitter.
- **Flyby sky caveat:** the port's sky reads paler than the old reel's in most grid moments. For example, at the top of t61.5 it measures (139, 218, 209) against the old (87, 184, 175).
  - The old sky is pale cyan up to its mid height (`_MidHeight` 0.95, curve 1.5) and only turns deep teal high up. The old reel looks up at towers, while today's flight (`new-keys.json`) mostly looks level or down.
  - The sky values are the old ones (exact below 1). If Sky wants the deep teal lower in the sky, that is a design change (`_MidHeight`), not a port.
- **Depth texture at play zoom:** still off. Nothing new needs it: the sky, sea, bloom and exposure are colour-only, and the flare and depth of field are still perspective-tier only.
