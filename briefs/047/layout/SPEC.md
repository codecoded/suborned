# Loading screen: layout spec for Latch (brief 047, Glyph, 2026-10-23)

Picture B (`briefs/047/fix/B_after.png`, 2560x1440) with the title and a slim progress bar over it, in the HUD style lab's language (Revised A, Jost Regular, kit colours only). Mockups: `mock-1920x1080.png`, `mock-2880x1864.png`, `mock-2560x1440.png` (made by `mock.py`, which is also a reference implementation of the maths below: `python3 -I mock.py loading.json ../fix/B_after.png out.png W H 0.62`).

## Replaceable: one image, one JSON
- `unity/Assets/Suborned/Resources/Loading/loading_bg.png` (copy of B) and `.../Loading/loading.json` (copy of `briefs/047/layout/loading.json`).
- To swap the picture: drop in a new png, set `image.name`, and move `title.rect` to the calm patch if it differs. Nothing in code names the picture, the title position or a colour. The main menu, Options, Controls and the mission brief (brief 051) read the same two files, so one swap changes the whole front end.
- Import: sRGB, no mipmaps, bilinear, uncompressed (or high-quality compressed), max size 4096, not in an atlas. A 4K screen scales the 2560 picture up; the title is vector text and stays sharp.

## Fields of `loading.json`
| Field | Meaning |
|---|---|
| `refHeight` 1080 | every `...Ref` value is px at 1080p screen height; device px = ref x (screen height / 1080). Same rule as the HUD. |
| `image.fit` "cover" | the picture fills the screen and crops, never letterboxes. `focus` [0.5, 0.5] = which part stays when it crops (0 = keep left/top, 1 = keep right/bottom). `pushIn` = optional slow zoom toward the focus (scale +0.02 over 24 s; 0 = still). |
| `frame.safeMarginRef` 64 | text and the bar stay this far from the screen edge. |
| `scrims[]` | gentle gradient bands over the picture: `edge` (top / bottom / left / right / full), `heightFrac` (of the screen height; of the width for left and right), `color`, `alphaAtEdge`, `curve` smoothstep, optional `holdFrac` (the share of the band held at full alpha before the ramp). Brief 051 also uses `edge` left / right (heightFrac = share of the width) and `full`. Baked by code to a 1 x 64 px texture (alpha along the band) and stretched; no shader. They are what makes text placement survive a different picture. |
| `title.rect` | the calm patch, **in picture space** (0..1 of the picture), so it follows the crop. The lockup is fitted inside it. |
| `title.align`, `maxScale` | where the lockup sits inside the rect; never scaled above `maxScale` x the ref size. |
| `title.lines[]` | text, font, size (ref px), tracking (em), colour, opacity. `rule` = brass hairline under the title, as wide as the named line. `shadow` = text-shadow. |
| `bar` | position (left / right / bottom margins, height, radius), `track`, `rim`, `fill`, `cap`, `shimmer`. |
| `loadingLine` | "LOADING · stage" over the bar's left end and a whole-number percent over its right end. |
| `build` | the build line (BuildStamp.Line), bottom right. |
| `timing` | seconds for the fades; see below. |

## Where things go on B, and why
- **Title: the open sky right of the Enforcer's head** (picture x 0.57-0.785, y 0.04-0.19). It clears all four faces (the Enforcer's head ends at x 0.54; his pauldron top reaches x 0.60 only below y 0.20) and stops before the right tower (x 0.78 at y 0.08). Stacked lockup: a small tracked "THE" over "SUBORNED" (Jost Medium 64, tracking 0.12 em) and a 1 px brass hairline under it.
- **Top scrim** (ink-deep, 0.50 at the top edge fading out by 34 % of the height): the sky behind the title is a mid blue (148, 183, 205) and cream on it is only 2.0:1; with the scrim it is about 4.9:1 (median, measured on the 1080p mockup), and it reads as a vignette, not a box.
- **Bottom scrim** (ink-deep, 0.68 at the edge fading out by 28 %): carries the loading line, the bar and the build number over the paving and the long shadows.
- **Bar:** the full width between the 64 ref margins, 5 ref px tall, 66 ref px from the bottom: it runs through the shadows below the boots, touching no face or hand. **Loading line:** above the bar's left end, "LOADING" in text-dim, "·", the stage in cream; the percent at the right end. **Build number:** bottom right, under the bar (the quiet corner), `BuildStamp.Line` ("Build 0.2.80 · c023 · sim 0.8.2 · 8bf8ce6"; in the Editor "Editor · sim 0.8.2").
- The robot, the café and the right-hand people sit under the scrims' edges only; no scrim reaches a face.

## Scaling and crops
- Picture: cover scale = max(W / 2560, H / 1440), centred on `focus`. Title and bar are placed after that mapping (picture-space rect -> screen), then clamped to the safe frame.
- **1920x1080 and 2560x1440 (16:9):** no crop. **2880x1864 (16:10, the Air):** the picture is scaled by height (1.294), so it is 3314 px wide and **434 px (13 %) are cropped, 217 px (6.5 %) off the left and right**: a sliver of the café on the left and of the right-hand tower and the standing man on the right. All four faces stay in. The title rect (x 0.57-0.785) is inside the visible range (0.065-0.935) and keeps its place over the sky. Wider than 16:9 (21:9, 32:9) crops top and bottom instead (the Enforcer's head is at y 0.09: at 21:9 only the very top of his hair is at risk; `focus` [0.5, 0.35] keeps him in if Sky wants ultrawide to be safe).
- **Title scale:** the lockup is fitted into `title.rect` after mapping, then capped at `maxScale` x (screen height / 1080). At 16:9 and 16:10 that is exactly the ref size x H/1080 (64 ref px at 1080p = 64 px; 111 px at 1864 tall; 85 px at 1440). Never smaller than the rect allows: if the rect is too narrow the lockup shrinks to fit, never overflows.
- Text, bar and margins all scale by H / 1080, so the screen at 2880x1864 is the same layout as 1080p at 1.726x. Checked at 2x crops (title, bar and build line): no clipping, even spacing.

## Fonts and colours (all from the style lab export `testbed/hud-style/export/hud.tokens.json`, Revised A, and `kit.uss`)
- Jost Regular (400) for body, Jost Medium (500) for the title line: files `Kit/fonts/Jost-Regular-Tab.ttf` (tabular digits, for the percent and the build line), `Kit/fonts/Jost-Medium.ttf` and `Kit/fonts/Jost-Regular.ttf` for the title. Barlow is speech only and is not used here.
- Colours: cream #f7f4ec (all type), text-dim #b0cdc7 ("LOADING" and the build line), ink-deep #0b1615 (scrims, track, shadow), chrome #cbc6c3 (the bar's 1 px rim at 40 %), turquoise-light #60e0ea (the fill, the live colour), brass #99784d (the 1 px title rule only). Nothing else, no red, amber or violet.

## Progress bar
- Value = the loader's real progress 0..1 (steps weighted by their cost; Latch's list: world, people, plan, mission). Never goes backwards.
- Drawn value eases to the real value with a critically damped smooth (`barSmoothSeconds` 0.25), so steps glide.
- The fill is turquoise-light with a 3 ref px cream cap at its head. A soft 120 ref px cream highlight (35 % alpha) sweeps along the fill every 1.6 s so a long step still looks alive; it stops at 100 %.
- A stall (no change for 2 s) changes nothing visible but the shimmer: no spinner, no text.
- Percent: tabular Jost, whole numbers, "62%", right end above the bar.
- Stage text: a short noun from the loader ("Harbour District", "People", "Plan"). Empty = "LOADING" alone.

## Fade in and out (seconds; ease-out in, ease-in-out out)
In: black (ink-deep) to the picture over 0.5 (the push-in starts); the title fades up over 0.6 starting at 0.25, rising 8 ref px; the bar, loading line and build line fade in over 0.4 starting at 0.5.
Minimum show: 1.5 s after the fade-in starts, even if loading is instant (no flash of a screen).
Out: bar reaches 100 %, holds 0.25, the bar, loading line and build line fade out in 0.25; then the picture and title cross-fade to what comes next in 0.6 (the main menu keeps the same picture and title, so only the bar group goes and the menu buttons fade in; the picture does not blink). The loading screen takes input (blocks clicks) until its last frame.
Everything is a USS transition on opacity/translate; no coroutines needed beyond the load steps.

## Done-check for the build
- Capture at 1920x1080, 2880x1864 and 2560x1440 at ~60 %: compare to the mockups (title inside the sky patch, bar in the shadows, build line bottom right).
- Replace `loading_bg.png` with any other 16:9 picture and `title.rect`: the screen draws with no code change (Latch to try once with picture A or D from `briefs/047/picks/` and set `title.rect` to its sky).
- hud-budget does not apply (not in play); everything in the file is sourced (the build check can read colours from the JSON).

## Open for Sky
- Title wording: the GDD names the game "THE SUBORNED" (heading, §01) but has no title treatment; this mockup sets it as a stacked "THE" over "SUBORNED". Alternatives: one line "THE SUBORNED", or no "THE".
