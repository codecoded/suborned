# Brief 075 Perf A/B · Studio counts (batchmode, offscreen; estimates, not UnityStats)

Machine: Studio, Unity 6000.6.2f1 batchmode, offscreen: Mac17,14, Apple M5 Max (Metal). Method: After072's estimator via After072.AbCapture (OldTownSwap.PerfAB), 1920×1080; draws = sub-meshes before batching or instancing; ms = batchmode Camera.Render with the GPU waited on, indicative only. Command: `bun briefs/075/oldbuild/swap/tools/unityrun.ts <log> -- -executeMethod Suborned.OldTown.Editor.Swap.OldTownSwap.PerfAB -abScene OldTown|HarbourDistrict [-abPipeline old|ours] [-abShadow 200]`, then `bun briefs/075/oldbuild/perf-capture/summary.ts`.

## HarbourDistrict, our pipeline

Asset Assets/Settings/PC_RPAsset.asset (Assets/Settings/PC_Renderer.asset); MSAA 1, shadows 1 cascades at 260 m, depth texture false, opaque texture false, lodBias 1, GRD as built InstancedDrawing. Export: out/scratch/latch/golden075/pin/s6-oldtown.world.json sha256 d9262e4f74534a4497f9fa984896297df9bbfc0f818188b3d4a2ff4fc3d2b052.

| view | SetPass est. (fwd/dn/shadow) | draws main / shadow | main renderers | main tris | shadow casters | shadow tris | materials | shaders | crowd renderers / tris | ground tiles renderers / tris | probes | ms GRD off | ms GRD on |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| cell-open-30 | 42 (22/7/13) | 672 / 1,083 | 292 | 740,536 | 332 | 726,011 | 142 | 8 | 0 / 0 (0% / 0%) | 4 / 998 | 2 | 3.14 | 3.25 |
| office-front-30 | 35 (19/5/11) | 375 / 407 | 231 | 597,578 | 149 | 308,590 | 89 | 9 | 0 / 0 (0% / 0%) | 4 / 998 | 2 | 2.48 | 2.42 |
| office-front-210 | 36 (18/6/12) | 291 / 277 | 212 | 593,824 | 142 | 314,619 | 86 | 9 | 0 / 0 (0% / 0%) | 4 / 998 | 2 | 2.47 | 2.27 |
| cafe-square-210 | 42 (22/7/13) | 572 / 787 | 272 | 724,283 | 313 | 615,211 | 142 | 9 | 0 / 0 (0% / 0%) | 4 / 998 | 2 | 4.88 | 3.28 |
| circus-30 | 34 (19/5/10) | 571 / 685 | 177 | 296,768 | 121 | 330,095 | 87 | 8 | 0 / 0 (0% / 0%) | 2 / 6 | 0 | 4.93 | 2.54 |
| harbour-terrace-30 | 32 (14/6/12) | 338 / 480 | 78 | 176,247 | 78 | 288,365 | 71 | 8 | 0 / 0 (0% / 0%) | 2 / 6 | 0 | 2 | 2.12 |
| harbour-station-30 | 21 (12/3/6) | 133 / 169 | 61 | 121,484 | 49 | 167,429 | 45 | 6 | 0 / 0 (0% / 0%) | 2 / 6 | 0 | 1.75 | 1.8 |
| southside-30 | 29 (16/6/7) | 335 / 316 | 98 | 186,012 | 54 | 177,054 | 66 | 8 | 0 / 0 (0% / 0%) | 2 / 6 | 0 | 2.07 | 2.7 |
| golden | 45 (25/6/14) | 4,233 / 1,890 | 1,090 | 1,869,221 | 313 | 913,892 | 185 | 9 | 0 / 0 (0% / 0%) | 2 / 6 | 0 | 6.73 | 10.84 |
| tp-street | 43 (24/7/12) | 6,444 / 1,355 | 1,745 | 2,254,210 | 414 | 641,098 | 186 | 10 | 0 / 0 (0% / 0%) | 4 / 998 | 2 | 10.82 | 10.38 |

Textures: probe cubemaps 2 (0.25 MiB GPU), glTF crowd textures 0 (0 MiB GPU); texmem (every texture the enabled renderers bind): 88 textures, 194.2 MiB GPU (388.48 MiB Editor runtime) (texmem-harbour-ours.json).

## OldTown, old pipeline (as built: shadows 50 m)

Asset Assets/Suborned/OldTownArt/Settings/PC_RPAsset.asset (Assets/Suborned/OldTownArt/Settings/PC_Renderer.asset); MSAA 4, shadows 4 cascades at 50 m, depth texture true, opaque texture true, lodBias 2, GRD as built Disabled. Export: OldTown, heights rule, SpaceHQ old.

| view | SetPass est. (fwd/dn/shadow) | draws main / shadow | main renderers | main tris | shadow casters | shadow tris | materials | shaders | crowd renderers / tris | ground tiles renderers / tris | probes | ms GRD off | ms GRD on |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| cell-open-30 | 47 (27/8/12) | 673 / 48 | 289 | 699,854 | 8 | 21,120 | 147 | 10 | 0 / 0 (0% / 0%) | 2 / 992 | 2 | 6.74 | 7.1 |
| office-front-30 | 42 (24/6/12) | 371 / 52 | 223 | 556,214 | 9 | 25,216 | 94 | 11 | 0 / 0 (0% / 0%) | 2 / 992 | 3 | 3.73 | 6.11 |
| office-front-210 | 41 (23/7/11) | 288 / 47 | 205 | 551,722 | 9 | 18,006 | 91 | 11 | 0 / 0 (0% / 0%) | 2 / 992 | 3 | 3.88 | 5.96 |
| cafe-square-210 | 43 (27/8/8) | 574 / 43 | 270 | 684,032 | 11 | 3,596 | 148 | 11 | 0 / 0 (0% / 0%) | 2 / 992 | 2 | 3.83 | 7.4 |
| circus-30 | 40 (23/7/10) | 2,283 / 74 | 1,427 | 1,521,934 | 14 | 36,908 | 255 | 9 | 1,243 / 1,025,976 (87.1% / 67.4%) | 30 / 92,940 | 7 | 7.14 | 9.08 |
| harbour-terrace-30 | 28 (12/4/12) | 1,726 / 74 | 1,403 | 1,192,800 | 11 | 37,176 | 172 | 4 | 1,347 / 1,022,784 (96% / 85.7%) | 0 / 0 | 5 | 9.72 | 7.91 |
| harbour-station-30 | 33 (17/4/12) | 204 / 78 | 113 | 210,064 | 11 | 96,812 | 91 | 9 | 64 / 78,818 (56.6% / 37.5%) | 0 / 0 | 3 | 3.64 | 6.36 |
| southside-30 | 28 (12/4/12) | 2,104 / 60 | 1,705 | 1,794,119 | 11 | 25,832 | 232 | 5 | 1,640 / 1,581,188 (96.2% / 88.1%) | 10 / 30,980 | 4 | 9.5 | 8.88 |
| golden | 61 (30/7/24) | 24,470 / 2,420 | 18,033 | 18,911,197 | 1,080 | 2,202,586 | 417 | 12 | 16,867 / 14,754,006 (93.5% / 78%) | 218 / 675,364 | 13 | 58.15 | 54.88 |
| tp-street | 80 (37/10/33) | 25,754 / 770 | 20,058 | 20,207,267 | 207 | 468,324 | 436 | 14 | 18,800 / 16,595,914 (93.7% / 82.1%) | 318 / 1,044,945 | 6 | 62.54 | 58.06 |

Textures: probe cubemaps 27 (17 MiB GPU), glTF crowd textures 35 (118.67 MiB GPU); texmem (every texture the enabled renderers bind): 149 textures, 451.85 MiB GPU (903.85 MiB Editor runtime) (texmem-oldtown-old.json).

## OldTown, old pipeline, shadows 200 m (as SwapShots rendered the frames Sky saw)

Asset Assets/Suborned/OldTownArt/Settings/PC_RPAsset.asset (Assets/Suborned/OldTownArt/Settings/PC_Renderer.asset); MSAA 4, shadows 4 cascades at 200 m, depth texture true, opaque texture true, lodBias 2, GRD as built Disabled. Export: OldTown, heights rule, SpaceHQ old.

| view | SetPass est. (fwd/dn/shadow) | draws main / shadow | main renderers | main tris | shadow casters | shadow tris | materials | shaders | crowd renderers / tris | ground tiles renderers / tris | probes | ms GRD off | ms GRD on |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| cell-open-30 | 61 (27/8/26) | 673 / 951 | 289 | 699,854 | 278 | 534,228 | 147 | 10 | 0 / 0 (0% / 0%) | 2 / 992 | 2 | 4.52 | 7.83 |
| office-front-30 | 53 (24/6/23) | 371 / 473 | 223 | 556,214 | 178 | 317,936 | 94 | 11 | 0 / 0 (0% / 0%) | 2 / 992 | 3 | 3.57 | 6.51 |
| office-front-210 | 56 (23/7/26) | 288 / 703 | 205 | 551,722 | 244 | 419,922 | 91 | 11 | 0 / 0 (0% / 0%) | 2 / 992 | 3 | 4.38 | 6.36 |
| cafe-square-210 | 59 (27/8/24) | 574 / 1,140 | 270 | 684,032 | 359 | 626,737 | 148 | 11 | 0 / 0 (0% / 0%) | 2 / 992 | 2 | 7.44 | 7.9 |
| circus-30 | 49 (23/7/19) | 2,283 / 2,782 | 1,427 | 1,521,934 | 1,582 | 1,843,812 | 255 | 9 | 1,243 / 1,025,976 (87.1% / 67.4%) | 30 / 92,940 | 7 | 13.16 | 11.34 |
| harbour-terrace-30 | 30 (12/4/14) | 1,726 / 2,151 | 1,403 | 1,192,800 | 1,556 | 1,499,254 | 172 | 4 | 1,347 / 1,022,784 (96% / 85.7%) | 0 / 0 | 5 | 11.02 | 8.19 |
| harbour-station-30 | 41 (17/4/20) | 204 / 517 | 113 | 210,064 | 298 | 505,055 | 91 | 9 | 64 / 78,818 (56.6% / 37.5%) | 0 / 0 | 3 | 4.42 | 6.42 |
| southside-30 | 31 (12/4/15) | 2,104 / 2,503 | 1,705 | 1,794,119 | 1,864 | 2,022,253 | 232 | 5 | 1,640 / 1,581,188 (96.2% / 88.1%) | 10 / 30,980 | 4 | 10.23 | 9.89 |
| golden | 78 (30/7/41) | 24,470 / 31,168 | 18,033 | 18,911,197 | 14,247 | 24,497,096 | 417 | 12 | 16,867 / 14,754,006 (93.5% / 78%) | 218 / 675,364 | 13 | 67.53 | 71.33 |
| tp-street | 92 (37/10/45) | 25,754 / 15,975 | 20,058 | 20,207,267 | 9,945 | 11,331,344 | 436 | 14 | 18,800 / 16,595,914 (93.7% / 82.1%) | 318 / 1,044,945 | 6 | 66.68 | 75.13 |

Textures: probe cubemaps 27 (17 MiB GPU), glTF crowd textures 35 (118.67 MiB GPU); texmem (every texture the enabled renderers bind): 149 textures, 451.85 MiB GPU (903.85 MiB Editor runtime) (texmem-oldtown-old-sd200.json).

## OldTown, our pipeline

Asset Assets/Settings/PC_RPAsset.asset (Assets/Settings/PC_Renderer.asset); MSAA 1, shadows 1 cascades at 260 m, depth texture false, opaque texture false, lodBias 1, GRD as built InstancedDrawing. Export: OldTown, heights rule, SpaceHQ old.

| view | SetPass est. (fwd/dn/shadow) | draws main / shadow | main renderers | main tris | shadow casters | shadow tris | materials | shaders | crowd renderers / tris | ground tiles renderers / tris | probes | ms GRD off | ms GRD on |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| cell-open-30 | 46 (27/8/11) | 673 / 1,207 | 289 | 699,854 | 348 | 615,313 | 147 | 10 | 0 / 0 (0% / 0%) | 2 / 992 | 2 | 3.74 | 4.28 |
| office-front-30 | 40 (24/6/10) | 371 / 492 | 223 | 556,214 | 241 | 374,692 | 94 | 11 | 0 / 0 (0% / 0%) | 2 / 992 | 3 | 3.02 | 4.08 |
| office-front-210 | 41 (23/7/11) | 288 / 981 | 205 | 551,722 | 320 | 517,264 | 91 | 11 | 0 / 0 (0% / 0%) | 2 / 992 | 3 | 3.43 | 6.04 |
| cafe-square-210 | 46 (27/8/11) | 574 / 1,343 | 270 | 684,032 | 426 | 668,892 | 148 | 11 | 0 / 0 (0% / 0%) | 2 / 992 | 2 | 3.77 | 4.33 |
| circus-30 | 39 (23/7/9) | 2,283 / 3,011 | 1,427 | 1,521,934 | 1,873 | 2,081,285 | 255 | 9 | 1,243 / 1,025,976 (87.1% / 67.4%) | 30 / 92,940 | 7 | 6.79 | 7.04 |
| harbour-terrace-30 | 23 (12/4/7) | 1,726 / 2,618 | 1,403 | 1,192,800 | 2,120 | 1,840,399 | 172 | 4 | 1,347 / 1,022,784 (96% / 85.7%) | 0 / 0 | 5 | 9.59 | 10.6 |
| harbour-station-30 | 30 (17/4/9) | 204 / 583 | 113 | 210,064 | 426 | 572,612 | 91 | 9 | 64 / 78,818 (56.6% / 37.5%) | 0 / 0 | 3 | 6.43 | 3.79 |
| southside-30 | 23 (12/4/7) | 2,104 / 2,911 | 1,705 | 1,783,421 | 2,124 | 2,361,505 | 232 | 5 | 1,640 / 1,581,188 (96.2% / 88.7%) | 10 / 30,980 | 4 | 8.58 | 8.73 |
| golden | 49 (30/7/12) | 24,381 / 23,633 | 18,024 | 18,817,408 | 17,970 | 18,486,735 | 416 | 12 | 16,867 / 14,754,006 (93.6% / 78.4%) | 218 / 675,364 | 13 | 50.02 | 50.58 |
| tp-street | 60 (36/10/14) | 25,448 / 20,957 | 19,979 | 19,960,567 | 15,980 | 15,978,926 | 431 | 13 | 18,800 / 16,595,914 (94.1% / 83.1%) | 318 / 1,044,945 | 6 | 49.56 | 45.54 |

Textures: probe cubemaps 27 (17 MiB GPU), glTF crowd textures 35 (118.67 MiB GPU); texmem (every texture the enabled renderers bind): 149 textures, 451.85 MiB GPU (903.85 MiB Editor runtime) (texmem-oldtown-ours.json).

## Redline's three questions (from the OldTown capture under the old pipeline)

1. **TacticalShadows / SSAO on the old play camera: no TacticalShadows.** The scene's one camera: DK street study camera: perspective, TacticalShadows no, ArchitectureFade no, renderShadows True, postProcessing True, AA SubpixelMorphologicalAntiAliasing; TacticalShadows in the scene: 0. So nothing refits the cascades and nothing turns SSAO off at ortho: the old renderer's ScreenSpaceAmbientOcclusion 'ScreenSpaceAmbientOcclusion' active (Source DepthNormals, AfterOpaque False, Intensity 0.9, Radius 0.45, Downsample False), FullScreenPassRendererFeature 'Harbour fog' active runs on the play view as on any other. The cascades are the asset's (4 at 50 m, measured from the camera): with the play camera 160 m back (SwapShots' pose, the stand-off the old-town plan resolves) almost nothing in the frame casts (shadow casters per play view: cell-open-30 8, office-front-30 9, office-front-210 9, cafe-square-210 11, circus-30 14, harbour-terrace-30 11, harbour-station-30 11, southside-30 11; ours: cell-open-30 332, office-front-30 149, office-front-210 142, cafe-square-210 313, circus-30 121, harbour-terrace-30 78, harbour-station-30 49, southside-30 54). The old town as built would play nearly shadowless; the frames Sky saw were rendered at 200 m (DecoKitStage.WithShadows), the oldtown-old-sd200 rows. Add the depth-normals prepass (dn column) and SSAO to its cost.
2. **The sun flare at play: enabled, never on screen.** LensFlareComponentSRP on Sun: enabled, occlusion on, intensity 1, data DK_SunFlare; Sun: forward (0.503, -0.704, 0.503), elevation 44.7°, shadows Soft. At every play view the sun is off the frame (angle from the view axis: 166.923° / 83.812°; an orthographic frame only holds directions along its axis), and at the golden and street poses too (golden off frame, tp-street off frame). URP skips an off-screen flare (no allowOffScreen), so its cost at play is the depth texture it asks for, which the old asset keeps on anyway. Ours has no LensFlareComponentSRP on the sun.
3. **GRD with the old shader copies: yes.** OldTown/DecoKit/Lit Cutaway and Complex Lit Cutaway include URP's DOTS.hlsl (DOTS_INSTANCING_ON in their keyword space), as does the glTF crowd's shader: Shader Graphs/glTF-pbrMetallicRoughness: DOTS instancing (71957 renderer-material uses); OldTown/DecoKit/Lit Cutaway: DOTS instancing (19547 renderer-material uses); DecoKit/Complex Lit Cutaway: DOTS instancing (7329 renderer-material uses); DecoKit/Lit Cutaway: DOTS instancing (6525 renderer-material uses); OldTown/DecoKit/Complex Lit Cutaway: DOTS instancing (5576 renderer-material uses); Suborned/Signal Dots: NO DOTS instancing (274 renderer-material uses). Enabled mesh renderers whose every shader has DOTS instancing: 76530; with a shader without it: 482 (our Signal Dots, Cross Glow, Contact Shade and the old RobotFace); with a MaterialPropertyBlock: 70. Renderer: PC_Renderer: ForwardPlus (GRD needs Forward+). With GRD set to Instanced Drawing the drawer reported active in batchmode. Its ms effect offscreen is within this capture's noise (the off/on columns); cut 4 needs the Air.

Render counters (ProfilerRecorder: SetPass, Draw Calls, Batches, Triangles, Shadow Casters) read 0 around offscreen Camera.Render in batchmode, so every count above is the estimator's. UnityStats needs the visible Game view (not run: offscreen only).

