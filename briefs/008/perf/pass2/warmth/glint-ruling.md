# Glint ruling: why the player is warmer than the Editor shot (008 B6 pass 2, 2026-10-15)

Inputs: Latch's `editor-home.png/.txt`, `player-home.png/.txt`, `compare.txt`; my sign-off `../glint-signoff.html`; URP source in `unity/Library/PackageCache/com.unity.render-pipelines.universal@1e1cf3dcbd40`; the grade asset `unity/Assets/Scenes/HarbourDistrict-grade.asset`. Unity MCP used read-only (`editor_status` ready, play mode stopped; one `eval` that only reads settings). No settings, scenes or lights changed.

## Ruling

**The player is the true look. The Editor shot is a clipped preview of it, not a second look.** Latch's guess (the capture path) is right, but the cause is not the colour space or the blit. It is the render target's *format*.

## Cause (read from the URP source, then checked against the pixels)

When a camera has a `targetTexture`, URP builds its intermediate colour buffer from the render texture's format, not from the HDR format the asset asks for. The package says so in `UniversalRenderPipelineCore.CreateRenderTextureDescriptor`: "the intermediate color buffer inherits the external texture's format instead of using the configured internal rendering format. This is incorrect but kept for backward compatibility." `CreateBackbufferInfo` sets `backbufferColor.format = camera.targetTexture.graphicsFormat`, and the renderer creates `_CameraColorAttachment` with that format.

- Every Editor shot (`Shots.cs`, `Camera.Render` into `ARGB32`, which is `R8G8B8A8_SRGB` in this linear project) therefore has an **8-bit LDR intermediate**. Scene colour is clamped to [0,1] before post-exposure, bloom, tonemap and grade, although `allowHDR` is true and the asset has HDR on. The two `.txt` files cannot show this: they print the flags, not the buffer format.
- The player draws to the screen, so its intermediate is `B10G11R11` float (asset `hdrColorBufferPrecision _32Bits`) and keeps values above 1.0.

Why it reads warm, and only in sun: the low warm sun on cream paving puts the scene's **R channel a little above 1.0** while G and B stay below it. The Editor path clips R only, so lit stone loses its red. Shade stays under 1.0 and is untouched, which is exactly what `compare.txt` shows (shade within 2 levels, sun R +5, B −2.4). The grade then amplifies it: `shadowsMidtonesHighlights` highlights is (1, 0.85, 0.66) as a **multiplier at full strength** (w = 0 is a neutral offset, not "off"; `Atmosphere.Vec`, `ColorUtils.PrepareShadowsMidtonesHighlights`), applied by luma from 0.55 to 1.0. More luma in the player means more G and B removed.

## Evidence

All numbers from `python3 -I glint-hist.py <this folder>` and `python3 -I glint-model.py` (both in this folder).

| check | Editor | Player |
|---|---|---|
| R value, whole frame | **229 on 45.8 % of pixels** (228-230: 50.7 %), max 235; R of 240 or more: 0 % | R of 240 or more: 20.8 %, 246 or more: 8.6 %, max 255 |
| the Editor's 228-230 pixels, read in the player | flat at 229 | 5th / 50th / 95th percentile 229 / 238 / 247; correlation of player R with Editor G (tile brightness) 0.93: the player's R follows the tile, the Editor's cannot |
| brightest pixels | (229, 209, 195); 0 pixels with all channels 250 or more | 249 pixels at 250 or more in all channels (chrome and bloom core) |
| model, scene colour clamped at 1.0 (G 0.85, B 0.62) | (228.5, 207.5, 174.0) vs observed (229, 207.8, 174.0) | |
| model, same scene colour with R = 1.175 unclamped | | (237.9, 206.0, 171.1) vs observed (238, 206.3, 169.2) |
| model, clamped white (1, 1, 1) | (228, 211, 190), the Editor's ceiling | |

The model is URP's LDR chain as built here (post-exposure 2^0.12, Neutral tonemap then saturate, contrast in LogC, colour filter, shadows/highlights, saturation), taken from `Common.hlsl`, `LutBuilderLdr.shader` and `Color.hlsl`. Bloom, vignette and the 32-point LUT interpolation are left out. Only R was fitted for the player; G and B are predictions and land within 0.3 and 1.9 levels. The scene R needed is only 17.5 % over 1.0.

## Candidates

| candidate | verdict | why |
|---|---|---|
| colour space, sRGB conversion on RT vs backbuffer | **Ruled out as the cause.** The RT's *format* is the cause | the RT is sRGB, so the hardware encodes it like the backbuffer; shade and mids agree to 1-2 levels over hundreds of thousands of pixels, and a wrong transfer would move every tone |
| HDR output, display P3, macOS EDR | **Ruled out** | `useHDRDisplay` 0, `allowHDRDisplaySupport` 0, `m_ColorGamuts` sRGB only; `HDROutputSettings.main` available false, active false (read now, Editor); the build strips the `_HDR_GRADING` variants (`PrefilterHDROutput`, LDR grading, no HDR display), so an HDR-output player would show garbage, not a warm cast; EDR would lift all tones, not only sun; P3 changes what a screen shows, not the captured bytes. Not measured in the player itself: add the line to PerfLook (below) |
| final blit and dithering | **Ruled out** | dithering false, antialiasing None, renderScale 1 in both; the clamp is upstream of the blit |
| bloom tint, full-res vs scaled | **Not a resolution effect; a secondary contributor** | both 1920 x 1080, scale 1. Bloom threshold 1.1 reads a buffer that cannot exceed 1.0 in the Editor, so bloom is nearly dead there (soft knee only) and live in the player. It can only add light, so it cannot explain G and B falling; the model without bloom already lands within 2 levels |
| different quality levels | **Ruled out** | both PC (index 1) -> `PC_RPAsset`: msaa 1, HDR on, LDR grading, drawer InstancedDrawing, scale 1 (read now) |

## Two more Editor-vs-game differences found on the way

- **MSAA:** for a camera with a render texture, URP takes the RT's `antiAliasing` (the Editor shots use 4), for the screen it takes the asset's `m_MSAA` (1). Editor shots have smoother edges than the shipped game.
- **Timings:** `Review/PerfProbe.cs` renders the same 8-bit path with 4x MSAA; its numbers are not the game's cost. The player stays the measure of record (as the pass already treats it).

## What makes Editor shots honest (owner: Latch)

No project or player setting does it: HDR is already on and is ignored in this path. Do not change `hdrColorBufferPrecision` or enable HDR display output to chase it. It is a code change in every Editor preview render:

1. Render the camera into `RenderTextureFormat.ARGBHalf` (depth 24), with `antiAliasing = UniversalRenderPipeline.asset.msaaSampleCount` (1 today), not `ARGB32` with 4. The intermediate is then HDR, as in the player.
2. URP does not encode sRGB for an RT target (`requireSrgbConversion` is `targetTexture == null && ...`), so the half RT holds linear values. `Graphics.Blit` it into an `ARGB32` RT (sRGB write: the hardware does the same encode as the backbuffer), then `ReadPixels` from that. Do not read the half RT directly.
3. Sites: `unity/Assets/Suborned/Editor/World/Shots.cs:88` (Render Shots), `Review/Render.cs:54`, `Review/Walk/WalkRig.cs:155`, `Perf/WarmthProbe.cs:44`, and `Review/PerfProbe.cs:52` (timing only). `Runtime/Perf/PerfNaN.cs` already uses a float RT.
4. In Play mode, `ScreenCapture` of the Game view takes the screen path and is honest as it is. If the MCP `capture_game_view source=camera` renders through an ARGB32 RT it has the same clip; `source=screen` (Play mode) does not. Latch to confirm.
5. Add to `PerfLook`: `HDROutputSettings.main.available/active` and the intermediate colour format, so the two `.txt` files can show what they could not.

Done-check for the change (Latch, same build as `player-home.png`): the new Editor home frame against the player frame with Latch's `compare.txt` regions. Pass when every region is within 2 levels per channel, the sun paving R median is at least 235 (Editor now 229 flat) and R-B on the sun paving is within 2 of the player. If it does not close, the next suspects are the GPU Resident Drawer (the probe turns it off; the player runs InstancedDrawing) and `B10G11R11` vs half-float precision, in that order. This is a mechanism read from the source and a model that fits within 2 levels; the one Editor render above is what confirms it.

## What this changes for the look (mine)

- **Perf look sign-offs stand.** The A/B diffs (SSAO off, cascades, LOD1 proxies) compared player frames with player frames.
- **B5 Editor tuning does not.** I tuned `light.json` (sun 1.35, ambient, grade) on clipped frames, which hide how hot lit stone really is. The player's sun paving is (238, 206, 169), R-B 69. The 26 Sep paintings' lit paving is about (236-242, 215-222, 191-198), R-B about 45 (medians of plaza patches in `briefs/008/concepts/`). The Editor shot (229, 208, 174, R-B 55) was already too peach; the player is further out, and 8.6 % of the frame sits at R 246 or more (the Editor: none), so form on lit paving is at risk of flat-topping in R.
- I retune `light.json` only against honest frames, once Latch lands the change above. First lever: the highlights tint, which is a full-strength grade and contradicts my own note that the warmth comes from the sun, not the grade. The model puts the player's sun paving at (237, 213, 185), R-B 52, with the tint off. Then sun colour and intensity, aiming at about (240, 218, 195). Those numbers are a starting point, not a result.
- For Aster: any look image Sky has reviewed that came through these Editor paths (B5 shots in `out/shots/008/`, review sheets) was the clipped one. Sky should see honest frames before the palette is signed off.

Open: the player's mids are 1-2 levels darker than the Editor's; I did not chase it (intermediate formats and bloom/vignette are enough to explain it).
