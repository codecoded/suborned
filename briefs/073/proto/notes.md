# 073 vintage-print prototype (Glint, scratch, 2026-10-28)

Throwaway. Off by default. The finish of an aged print poster (Sky's sky-board-2: paper, soft halftone, softened highlights, a faded warm cast) laid over the built world as post. Concept direction only: nothing shipped changed (no grade asset, light.json, lens.json, style contract, GDD, PC_Renderer.asset or scene).

## For Latch (064 A3)
- **No renderer-asset entry.** Two passes are enqueued from `RenderPipelineManager.beginCameraRendering`, and only while the toggle is on, on Game base cameras with post on.
- **Signals stay on top.** The print pass is at `AfterRenderingPostProcessing` and is enqueued before your `SignalsFeature` pass (same event; URP sorts stably and features enqueue after beginCameraRendering), so the signal layer draws after it, untouched. Checked: the opaque signal-line pixels are within 1 level of the print-off frame in all three variants (1244/1244, `shots/print073.json` signalCheck).
- **Depth texture (your item 5).** Only the ink-line part needs it (`ConfigureInput(Depth)`, requested only when inkLines > 0). The subtle preset has no ink and needs nothing. Default and strong ask for it, which buys back the depth texture your item 5 dropped. If a real round keeps ink lines, budget for the depth texture or find a cheaper edge source.
- **Your `After072.SignalFrame` signature.** It gained `Vector2 at` mid-run. My shots call it by reflection and handle the 4-parameter and 5-parameter forms.
- **Compile error, now fixed.** Early in this spike a compile error from my scratch file (`UniversalCameraData.pixelWidth`) broke Suborned.Runtime for about 2 minutes. If one of your runs failed then, that was me.

## What each part does
| part | where | what | knobs (Print073Grade) |
|---|---|---|---|
| specular cap | BeforeRenderingPostProcessing, scene-linear HDR | a soft shoulder on the max channel above a knee (asymptote knee + range), so chrome and glass hot spots stop reading glossy CG and feed less bloom. `RenderSettings.reflectionIntensity` is also scaled while on (in memory, restored on off) | capKnee, capRange, capMix; reflection scale per preset (0.7 / 0.5 / 0.35) |
| faded warm grade | after post, display gamma | saturation with extra highlight desaturation; a warm cast (R up, B down); then a remap of black..white onto ink..paper (lifted teal-black ink #1a2126, paper #f7f0db) | fade, saturation, highlightDesat, warmth, ink, paper |
| ink lines | after post | depth second difference (Laplacian, 5 taps; flat planes are linear in ortho depth so they draw nothing): creases and silhouettes, laid in the ink colour, 1 px at 1080p scaled with height | inkLines, inkLow, inkHigh (metres) |
| halftone | after post, screen-fixed | a round-dot screen in 1080p-normalised px. It keeps mean tone (tone × (1 − k·(dot − coverage))) and stays out of paper-white highlights. Mono K screen at 60°, optionally mixed with CMY screens at 15/75/0° | halftone, halftoneCell (px), halftoneAngle, halftoneColour |
| paper | after post, screen-fixed | fine grain (1.3 px and 3.7 px value noise) and mottling (45 px and 140 px), multiplicative; a paper-tinted edge fade | grain, mottle, vignette |

All patterns are fixed to the screen with no time term, so nothing swims: the image moves under the paper as a print moves under a screen. They scale with output height (`h/1080`).

The global volume (priority 1000, the layer of the shipped volumes) holds one `Print073Grade` built from a preset. The shipped volume profiles are never touched.

## Presets
- **subtle**: cap 0.7 mix at knee 1.8, fade 0.45, halftone 0.10 (3.5 px), grain 0.035, no ink. Reads as "less CG" (the chrome loses its gloss, whites go to paper); hard to spot as a print at 1x.
- **default**: cap at knee 1.5, fade 0.6, ink 0.4, mono halftone 0.22 (4 px), grain 0.05, mottle 0.045. Reads as a printed picture at 1x; at 2x the dot screen is visible on the paving.
- **strong**: cap at knee 1.35, fade 0.8, saturation 1.08, ink 0.85, halftone 0.38 (5 px, 25 % CMY), grain 0.065. Comic-book: outlines on every building and planter, a clear dot screen.

Knee choice, from the HDR probe (post off, scene-linear max channel): sunlit stone sits at p90 1.07–1.22 and p99 1.36–1.50. Only 0.25–1 % of pixels are above 1.5 (the hot spots). A knee under about 1.35 starts to flatten sunlit stone.

## Cost
Synced grabs on the Studio (render, blit and ReadPixels, 1920 × 1080; median of 3 × 30): off 13.81 ms · subtle 14.20 (+0.4) · default 14.70 (+0.9) · strong 16.55 (+2.7). Default and strong include the depth texture the ink lines force back on.

Rough GPU estimate for the 15-inch Air M4 at play zoom:
- subtle: about 0.4–0.7 ms (two full-screen passes plus one extra intermediate copy)
- default: about 1–1.5 ms (plus the depth texture)
- strong: about 2–3 ms

Cheapest wins if it goes further:
- bake the dot screens and noise into one small tiling texture (ALU → 1 fetch);
- fold the print pass into URP's own uber pass as a custom LUT plus one texture, or into the existing grade LUT (the grade part then costs nothing);
- drop ink lines, or take them from the SSAO normals.

Redline should time it on the Air before any real round.

## Open issues (a real round would need these)
1. **The crossing heads' glow and the cones are world objects in the main pass**, so the print fades them like everything else; only the signal layer is protected. The hold says the crossing heads keep their own colours under every light. Fix: a stencil bit on CrossGlow, SignalDots and the cone materials, which the print pass skips.
2. **The specular cap is screen-space**, so it also clamps any legitimately bright emissive (lamps at night, glows). The real fix is per material (lower smoothness and reflection on chrome and glass; matte-painted materials), with the cap as a safety net.
3. **Night, evening and the ground view were not shot.** Day only, play zoom.
4. **Player builds.** The shader is found by name and is not in Resources, so `-print073` works in the Editor only.

## Shots (offscreen, batchmode, Capture's HDR path, 1920 × 1080, people hidden, runtime lens installed as Glint072 does)
- `shots/<variant>-<view>.png`: variants off, subtle, default and strong; views office-front-y030, office-front-y210, cell-open-y030, cell-open-y210
- `shots/<variant>-signals-office-front-y030.png` and `-nosignals-`: the fight-lab signal frame (After072.SignalFrame) drawn through SignalsFeature
- `shots/print073.json`: the HDR probe, timings and signal check
- `composites/sbs-<variant>-<view>.png`: off | variant side by side
- `composites/crop2x-<variant>-<view>.png`: the centre 480 × 270 of each, nearest ×2
- `composites/crop2x-<variant>-signals-lines.png`: ×2 on the signal lines
- `composites/board4-<view>.png`: off, subtle / default, strong

Rerun: `bun run guard <Unity 6000.6.2f1> -batchmode -quit -projectPath unity -executeMethod Suborned.Scratch.Print073Shots.Cli -logFile out/scratch/glint/073/run.log` (add `-variants off,default` to limit). Composites are made with ffmpeg (hstack, crop, scale flags=neighbor).

## Delete list (if Sky says no)
`rm -rf unity/Assets/Suborned/Scratch unity/Assets/Suborned/Scratch.meta` removes all of it. Nothing outside the folder references it. The folder holds:
- Scratch.meta, Scratch/Print073.meta, Print073/README.txt(.meta)
- Runtime/: Print073.Runtime.asmref (folds into Suborned.Runtime), Print073.cs, Print073Grade.cs (+ .meta, Runtime.meta)
- Editor/: Print073.Editor.asmref (folds into Suborned.World.Editor), Print073Shots.cs, Print073Video.cs (the videos, 2026-10-28) (+ .meta, Editor.meta)
- Shaders/: Print073.shader (+ .meta, Shaders.meta)

Evidence, git-ignored: `briefs/073/proto/` (shots, composites, this note), `briefs/073/video/` (mp4s, about 2 GB: `rm -rf briefs/073/video`). The page `briefs/073/videos.html` goes with it. Scratch logs and the measuring scripts: `out/scratch/glint/073/`, `out/scratch/glint/073v/` (tools/flicker.py, tools/stick.py, strips/).

## Videos (2026-10-28, round 2: Sky "third person and a video recording of the vintage print, and possibly others")
- **Print073Video.cs** (Editor): offscreen batchmode, a fixed 1/30 s step, Capture's HDR path at 1920 × 1080, RGB24 piped into ffmpeg (H.264 CRF 14). Clips: `tp` is a scratch third-person presentation camera (perspective 55°, far 350, 3.6 m back and 2.3 m up; the Scout posed through her own Animator, walking the last 45 m of an A* path on the host frame's walk kinds from Plaza west to the Office approach). `play` is an eased pan from the opening view to the Office front at yaw 30, then a quarter turn to 120, with the fight-lab signal frame. `night` is the play clip at night. AtmosphereLens and DistanceHaze are installed in memory (edit mode). The crowd is hidden; water, fountain and crossing heads are still.
- **New presets in Print073** (same volume component, same passes): `haze` (lifted lilac ink, pale paper, a top-of-frame peach haze, a Bloom override: threshold 0.9, intensity 0.42, scatter 0.85), `comic` (ink 1.0, 4 cel bands, a 70 % colour dot screen, saturation 1.3), `gouache` (pass 2: a smoothed 4-sector Kuwahara, r 4 px at 1080, plus a matte grade). New knobs: `cel`, `celSoft`, `hazeTop`, `hazeColour`, `paint`, `anchor`.
- **`preset@ground`** fixes paper and dots to a world point's screen offset (the pan follows the ground, ortho only, translation only). The y sign was measured with `-probe` (Metal: AnchorFlipY −1, error 0.0025 vs 0.040 screen-fixed).
- **Verdict**: the screen-fixed grain and dots read as a screen door in motion (5.2× today's unexplained frame change on the pan; comic 4.9×). Ground-anchored is 1.6×, from the 1.3 px grain aliasing and the depth ink stepping. Haze 1.14×, gouache 1.22×. Gouache erases the crossing dots and the head's icon; the print and haze fade the cones and crossing heads; at night the print lifts the base and caps the lamp-head glow. Page: `briefs/073/videos.html`.
