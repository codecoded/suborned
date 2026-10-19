# 020 · Figures in the real scene: why they shine, what is broken, who fixes what

Glint, 2026-10-18. Inputs: `briefs/020/play/*.png` (Sky's play shots, 6016x3384), Sky's Air recording `briefs/020/people/anim/sky-1821/` (f03, f06), the figure materials in `unity/Assets/Suborned/People/Figures/*/Materials`, the scene grade and lights, and live checks in Play Mode of `HarbourDistrict.unity` (read-only: I made temporary cameras and material *instances* that were destroyed on exit; no asset, scene or C# was edited). Crops are in `briefs/020/render/`. Scratch scripts and full-size captures are in `out/glint/` (not shipped).

## Verdict in three lines

1. The shine is the figures' own materials, not the light or the grade. 37 of the 55 figure materials (every `_512` LOD2 material, and every crowd material) are URP Lit with **`_Metallic = 1` and no metallic map**, so they are mirrors tinted by the albedo. The other 18 (cell and cast, LOD0/1) carry a metallic/smoothness map whose smoothness is 0.46 to 0.74 on cloth, multiplied by `_Smoothness = 1`: wet latex.
2. At play zoom the figures are on **LOD2 (512, no map, metal 1)**, so the overhead view shows the mirror materials; the follow view shows LOD0 (map, latex). Sky saw both.
3. The grade and the lights are fine. Turning the grade off changes contrast only (`grade-on-vs-off.png`). I change nothing in lighting. Forma owns almost all of this fix; Latch has two small view-side items.

## 1. Why they are too shiny

### What the materials say

| Group | Count | Metallic | Smoothness | Result |
|---|---|---|---|---|
| Cell and cast `*_2048`, `*_1024` (scout, fixer, tech, enforcer, waitress, guard, security, driver, councillor, cafe_w2/3/5, rival) | 18 | map R (`_METALLICSPECGLOSSMAP`), scalar ignored | map A x `_Smoothness 1` | latex / wet cloth |
| Every `*_512` of those figures (the LOD2 that draws at play zoom) | 19 | **scalar 1, no map** | 0.2 | mirror, albedo-tinted |
| Crowd (c1, c6, d2m, d4, d5c, d6, el1, sh2, workers_d5) all LODs | 18 | **scalar 1, no map** | 0.2 | mirror, albedo-tinted |

(37 no-map materials: `grep -L _METALLICSPECGLOSSMAP`; all 37 have `_Metallic: 1`. The manifest says the crowd is "albedo and normal only, smoothness 0.2", and nothing sets metallic to 0.)

Metal maps measured (R metallic, A smoothness; the PNGs are 1024 px even where the material is named 2048):

| Figure | R mean / p95 | A mean (p5 to p95) |
|---|---|---|
| scout | 0.05 / 0.16 | 0.46 (0.20 to 0.72) |
| fixer | 0.03 / 0.06 | 0.61 (0.51 to 0.76) |
| tech body | 0.09 / 0.61 | 0.33 (0.20 to 0.49) |
| tech visor | 0.01 / 0.05 | 0.97 |
| enforcer | 0.01 / 0.05 | 0.39 (0.25 to 0.60) |
| waitress | 0.02 / 0.06 | 0.48 (0.20 to 0.78) |
| guard, security | **0.39 / 0.98** | 0.66 (0.53 to 0.77) |
| cafe_w5 | **0.32 / 0.99** | 0.74 (0.38 to 0.99) |
| cafe_w2, cafe_w3 | 0.07, 0.03 | 0.68, 0.71 |

The world is matte: `DK_Limestone/Render/Concrete/Terrazzo/Cloth_*` smoothness 0.25, `Fabric_Ivory` 0.15, foliage 0.15 to 0.25, rubber 0.2, metallic 0; satin brass 0.5 / 0.3; enamel 0.68 only on small accents; chrome 0.95 / 1.0 only on lamp balls and rails. Figures at 0.46 to 0.74 smoothness over cloth are far shinier than the stone they stand on.

### Crops

- `sky-f03-scout-wet-jacket.png`, `sky-f06-scout-wet-jacket.png`: Sky's frames. The Scout's jacket and shorts show wet, rolling highlights: LOD0, smoothness map 0.2 to 0.72, sun 1.6 on a 38 degree key.
- `sky-f06-crowd-silver-blotches.png`: the two blue-and-white figures at the café table (guard-family models). Large silver-white blotches on shoulders and chest: the guard metal map is 39 % metal (R up to 0.98 on the navy panels), and their LOD2 (`guard_body_512`) is metal 1 over the whole body, so they mirror the sky cubemap.
- `overhead-live-cell-lod2-blotches.png`: live, play zoom. Fixer's dress and the Tech's black outfit carry white/black smears in blocks; that is `*_512` metal 1 mirroring bright pavement and dark sky. (The cell is on LOD2 here: confirmed by `isVisible` on every cell and cast renderer; scout, fixer, tech, enforcer, waitress all `*_LOD2` visible.)
- `overhead-live-crowd-holes.png`, `crowd-lod0-base-vs-fix.png`: crowd under metal 1: dark spots, shine on the mustard tunic.
- `cell-lod0-base-vs-fix.png`, `cell-lod2-base-vs-fix.png`, `cell-front-lod0-base-vs-remap.png`: before and after (below).

### Not the cause (checked)

- **Grade**: `grade-on-vs-off.png`, same materials, grade weight 0 vs 1: the sheen is in both. The grade (Neutral tonemap, contrast +14, saturation +14, exposure +0.12, shadow tint, bloom threshold 1.1 / intensity 0.18) adds punch only.
- **Light**: sun 1.6, warm white, 38 / 330, soft shadows; ambient trilight (0.67 sky, 0.79 equator, 0.44 ground); custom reflection cubemap, probes `office-front`, `civic-square`. The world's stone uses the same light and reads matte.
- **Environment reflection / specular flags**: all figure materials have `_EnvironmentReflections 1`, `_SpecularHighlights 1`: standard; they only matter because of smoothness and metallic.

### Target values for Forma (per class)

Match the world: stone and cloth 0.25, rubber 0.2, satin brass 0.5 / 0.3.

| Class | Metallic | Smoothness | Notes |
|---|---|---|---|
| Cloth, jackets, dress, shorts, trousers, hoodie | 0 | **0.15** (range 0.12 to 0.20) | world `Cloth_*` 0.25, `Fabric_Ivory` 0.15; crowd jackets stay here |
| Skin | 0 | **0.30** (0.25 to 0.35) | a soft sheen, no wet look; crowd skin shares the cloth atlas: 0.15 to 0.2 is fine |
| Hair | 0 | **0.25** | |
| Leather, boots, gloves, belts, harness | 0 | **0.30** (0.25 to 0.35) | rubber soles 0.2 |
| Chrome / brass trim (thin lines, buckles, bracers, the Enforcer's gold) | **0.6** | **0.45** | like `DK_Brass_Civic` 0.3 / 0.5 and satin chrome; never 1.0 / 0.95 (mirrors the sky cubemap into a white blotch) |
| Tech visor | 0 | 0.75 (was 0.97) | emissive stays |
| Emission trim (Tech cyan) | n/a | n/a | unchanged |

How to apply, in order of effort:

1. **No-map materials (37: all `_512`, all crowd): `_Metallic 0`, `_Smoothness 0.15`.** One-line material change. Tested live: `cell-lod2-base-vs-fix.png`, `crowd-lod1-512-base-vs-fix.png`. The cell on LOD2 loses its chrome trim; at 55 px that is invisible. If a trim must survive, bake it into the 512 albedo.
2. **Mapped materials (18): rebake the metal PNGs** (do not rely on scalars: with a map present `_Metallic` is ignored). What I tested live, per pixel: `t = smoothstep(0.3, 0.7, R)`; `R' = 0.6 * t`; `A' = lerp(min(0.45 * A, 0.28), min(A, 0.55), t)`; G untouched; `_Smoothness 1`. Result `cell-front-lod0-base-vs-remap.png`: the Scout jacket, Fixer dress and Tech vest go matte, the Enforcer's coat and gold trim keep their look, the guard's mirror panels calm down. A scalar-only shortcut that needs no rebake: `_Smoothness 0.22` on the mapped materials (`cell-lod0-base-vs-fix.png`), but the guard / security / cafe_w5 metal zones stay metallic, so do the rebake for those three at least.
3. Make LOD0, LOD1 and LOD2 of one figure use the same classes, so there is no shading change when a figure crosses a LOD threshold (today it flips from map-latex to no-map-mirror between LOD1 and LOD2 on the cell).

## 2. The "serious rendering issues"

| # | Issue | Where | Crop | Cause | Owner |
|---|---|---|---|---|---|
| 1 | Mirror blotches, silver / black smears | cell and cast at play zoom, all crowd, guard family in the follow view | `overhead-live-cell-lod2-blotches`, `sky-f06-crowd-silver-blotches` | `_Metallic 1` with no map (37 materials); guard map 39 % metal | Forma |
| 2 | Wet latex cloth | cell, LOD0/1, follow view | `sky-f03/f06-scout-wet-jacket`, `cell-lod0-base-vs-fix` | smoothness map 0.46 to 0.74 x `_Smoothness 1` | Forma |
| 3 | Cracks and shards in cloth: white and dark slivers on jackets, capes, tunics | crowd c1 (cape), workers_d5 (jacket), d2m (tunic), sh2 (hem) | `follow-cafe-patrons`, `crowd-lod0-base-vs-fix`, `crowd-cull-front-vs-off` | Mesh defects of the generated crowd (cloth shells with cracks and overlapping layers); visible at LOD0 close up, so not mip bleed. Setting `_Cull 0` does not fix it: the cracks show the dark inside faces (`crowd-cull-front-vs-off.png`). Not alpha sorting: no alpha, no transparent material in the set (all `_Surface 0`, `_AlphaClip 0`) | Forma (close the shells / weld; or drop those figures) |
| 4 | Baked-in dark spots and patches (mustard tunic, hat band) | sh2, workers_d5 | `crowd-lod0-base-vs-fix` | content in the Hunyuan albedo atlas (island texture, seen in `sh2_body_1024_albedo.jpg`) | Forma |
| 5 | Jagged edges on every figure | overhead and follow | edges in `overhead-live-crowd-holes` | no AA anywhere: URP asset MSAA 1, camera AA None, post has no SMAA/FXAA | Latch sets, Glint advises |
| 6 | LOD pop (shine flips) | figures crossing LOD thresholds, follow view | n/a | `LODGroup.fadeMode None` (hard switch) plus issue 1 making LOD1 and LOD2 look different; heights 0.18 / 0.08 / 0.012 from the manifest | solved by Forma point 3; no Latch change needed |
| 7 | "Dithered" officer and carried body in fights | `overhead-fight` | `overhead-fight-officer-carried.png` | **Not the figures' materials.** Figures are plain URP Lit (rendering layer 1); they never take the cut. The whole street patch (road, trees, officer, the body) carries one screen-door pattern: the architecture ghost (20 % left, `ArchitectureFade.ghost 0.2`) and its ghost shadow (0.4) over that ground. The officer and the carried body stand outside the focus discs (`SetPoints` held 5 points at the start; it takes 10). I did not reproduce this fight frame live (the fight is late in the mission), so this comes from the crop and the code | Latch |
| 8 | Black on black (Tech) | follow view, shaded side | `cell-front-lod0-base-vs-remap` | Tech albedo is near black everywhere, so he reads as a hole in shade | Forma (lift the vest and shorts base to about sRGB 45 to 55) |

Checked and **not** present: shadow acne (none on skin or cloth; sun bias 0.05 / 0.4, asset 0.1 / 0.5); peter-panning (shadows attach at the feet; a follow-like camera with figure shadows on vs off differs by 31.6 k pixels at 1600x900, so figures cast and are shadowed); z-fighting from the camera (ortho near 0.3 / far 1683 is about 0.1 mm per depth step; follow near 0.2 / far 600; the layered-cloth flicker is the mesh issue 3); SSAO halos (intensity 0.4, radius 0.3, after opaque: no halo round any figure); alpha-hair sorting (hair is opaque in the albedo; nothing transparent); inverted faces (back faces are culled; the only holes are the crowd cracks); figures not receiving the shadows or ambient (`receiveShadows True`, lit by the trilight and sun like the stone).

Shadows are present but light: sun 1.6 against ambient 0.7 to 0.8 gives a soft lavender shadow, so figures ground weakly on bright pavement. That is the world's look; the cell has its cyan ring. I would not darken it.

## 3. Fix split, with numbers

**Forma (materials and textures; the whole shine fix)**
- 37 no-map materials: `_Metallic 0`, `_Smoothness 0.15`.
- 18 mapped materials: rebake the metal PNG per section 1 (R x 0.6 through a smoothstep, A capped 0.28 on cloth and 0.55 on trim) and keep `_Smoothness 1`; or `_Smoothness 0.22` as a stop gap.
- Guard, security, cafe_w5: their R channel must go to about 0 outside the trim.
- Tech visor 0.97 to 0.75; Tech base albedo lifted.
- Crowd cracks (c1, workers_d5, d2m, sh2): close and weld the cloth shells, or take those four out of the pool until they are fixed. Dark spots in the sh2 and workers_d5 albedo.
- The "2048" metal maps are 1024 px: say so or rebake at 2048.

**Latch (view side; the scene is Latch's)**
- Anti-aliasing: set `UniversalAdditionalCameraData.antialiasing = SMAA`, quality Medium, on the play camera. SMAA runs after the grade; on the Air at 1080p-class resolution it should cost a few tenths of a millisecond (not measured here; measure it). MSAA is a worse fit: the camera also needs the depth-normals pass for SSAO.
- Fights: put the combatants and the carried body in `ArchitectureFade.SetPoints` (limit 10; 5 are used at the start) so they sit inside a clear disc; the same fix Glyph asked for in brief 016.
- Do **not** change render order or layers: figures are not drawn through the fade (the fade is a per-pixel cut in the architecture shader, not an overlay), and shadow casting, LOD bias and layers are fine as they are.
- Leave `shadowCastingMode On` on all figures and `LODGroup.fadeMode None`; heights 0.18 / 0.08 / 0.012 give LOD2 at play zoom (a 1.8 m figure is 0.064 of the 28 m view) and LOD0 in the follow view (0.31 at 5.5 m).

**Glint (lighting and grade): no change.** The grade is not the cause (`grade-on-vs-off.png`), the light matches the matte world, and the shading is right once the materials are right. If after Forma's pass the cell is still too bright in sun I will lower the cubemap's contribution on figures through a probe, not the grade. I will re-check the Scout, Fixer, crowd and guard family in the real scene when Forma's materials land; Latch should capture the follow-cafe, pickup and fight spots again.

## Frame cost

- Counted: with figures at play zoom, 21 figures are on screen at the start (cell 4, cast 4, crowd about 13), each with three LOD renderers but one drawn, so about 1.5k triangles each at LOD2 or LOD1 (crowd): about 30 k triangles in the main pass. Each is skinned in up to four passes (depth-normals for SSAO, main, one or two shadow cascades): about 90 skinned draws now, about 200 at the 50-figure target. Shadow casting is about 75 k triangles per cascade at 50 figures: small on an M4. The cost to watch is draw count and skinning, as the manifest says.
- Measured by the game's own proof (`briefs/020/play/proof.json`, 6016x3384, the Mac Studio): square with figures 138.8 fps, mean 7.2 ms, p99 8.9 ms, GPU 16.1 ms; with capsules 141.6 fps, 7.1 ms, p99 7.4 ms, GPU 7.6 ms. So +8.5 ms of reported GPU time for the figures (frame time barely moves, so the GPU number is a pipelined sample; treat it as an upper bound) and p99 up 1.5 ms. The follow spots are 83 to 99 fps mean there.
- My own A/B (figure shadows on / off, timed with FrameTimingManager in the editor) was too noisy to quote: the editor was shared and the GPU figure moved from 2.6 ms to 19 ms with nothing changed.
- Not yet measured on the Air. Latch: at the 50-figure spot, A/B (a) figure shadows off for the crowd beyond about 25 m, (b) SSAO off, (c) SMAA on, with the game's gpu-ms row. The budget stays 4.5 ms for people (Redline). The materials fix costs nothing at runtime.

## Evidence

- Materials: `grep -L _METALLICSPECGLOSSMAP unity/Assets/Suborned/People/Figures/*/Materials/*.mat` (37 of 55), `_Metallic: 1` on all 37; metal-map statistics by script on the PNGs.
- Live (Play Mode, read-only): `isVisible` per LOD renderer (cell on `*_LOD2`, crowd on LOD1 or LOD0); base vs fix renders with temporary material instances; grade weight 0 vs 1; follow-like camera with figure shadows on / off; ghost shadow 0.4 / 1.0 / 0.0 (`ghost-shadow-stipple-0.4-1.0-0.0.png`: no stipple falls on a figure in that frame).
- Scene state: Play Mode left stopped; no scene or asset saved or changed. Scratch scripts in `out/glint/scripts/` (`GlintShots.cs`, `GlintGhost.cs`, `GlintCost.cs`, `GlintFollowShadow.cs`); the editor is shared with other agents.

## Next owner

Forma (materials, metal maps, crowd meshes: first the 37 scalar fixes, they are ten minutes), then Latch (SMAA, SetPoints, one perf pass on the Air), then Glint and Glyph (review in game).
