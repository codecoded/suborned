# 072 after, offscreen (batchmode), 2026-10-27 21:48

Scene: out/scratch/latch/072/v4/v4.world.json sha256 a50c649a5c84b23ab799a32c9cff945fd77fde25a01013483ea0702c5fbb4fc2. cameras/play posed as HandoverHost.PlaceCamera: orthographic 14, pitch 38, standOff 160 m; cell-open from HandoverHost.frame.World(OpeningFocus); Camera.Render into Capture's HDR target 1920x1080 (no Game view). GPU Resident Drawer off (RenderSession), as the baseline counts.

**Nothing here is UnityStats.** No number here is UnityStats (that needs the visible Game view; queued for when the Studio is free). census* = Baseline072's census method (16:9 frustum + LOD pick; counts shadows-only renderers as visible, as the baseline did). main* = the same without shadows-only renderers. setPassEstimate = distinct shader variants: forward (every visible material, its keywords) + SSAO depth-normals prepass (opaque, _ALPHATEST_ON/_NORMALMAP) + shadow (per cascade, its casters, _ALPHATEST_ON); fullscreen passes (sky, SSAO, post, copies) not counted. shadow* = casters per cascade by URP's split for this camera reproduced (CullingResults culling planes and sphere, bounds test; LOD-picked meshes): an estimate. frameMs = batchmode offscreen Camera.Render, GPU waited on, indicative only.

The UnityStats rerun (Baseline072.AfterCli: visible Editor, Game view at 1920x1080) is queued until the Studio is free.

## Baseline census method: before (export d6f7560d, Editor, 18:13) against after (v4 a50c649a, batchmode)

| view | yaw | visible renderers before → after | census tris before → after (Δ) | baseline UnityStats tris / SetPass / casters (before only) |
|---|---:|---:|---:|---:|
| office-front | 30 | 221 → 237 | 611,861 → 653,852 (+41,991) | 977,671 / 55 / 469 |
| office-front | 120 | 234 → 270 | 604,365 → 651,482 (+47,117) | 1,002,215 / 59 / 473 |
| office-front | 210 | 207 → 215 | 596,799 → 635,666 (+38,867) | 945,857 / 57 / 289 |
| office-front | 300 | 214 → 217 | 586,831 → 624,236 (+37,405) | 854,331 / 58 / 268 |
| cell-open | 30 | 272 → 314 | 750,641 → 771,385 (+20,744) | 1,454,243 / 69 / 913 |
| cell-open | 120 | 273 → 342 | 740,075 → 792,367 (+52,292) | 1,405,678 / 66 / 859 |
| cell-open | 210 | 258 → 292 | 736,128 → 755,238 (+19,110) | 1,379,150 / 68 / 674 |
| cell-open | 300 | 268 → 280 | 753,864 → 758,120 (+4,256) | 1,322,310 / 67 / 574 |

## Every face against today on the same build (EveryFace off: the procedural walls the modules replace), batchmode estimates

| view | yaw | main tris today → every (Δ) | materials | SetPass est. today → every (fwd + dn + shadow) | casters today → every | shadow tris today → every (Δ) | all-pass tris est. every | modules in frustum (columns) | batchmode ms every / today |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| office-front | 30 | 649,644 → 653,222 (+3,578) | 87 → 88 | 36 → 36 (18 + 6 + 12) | 159 → 168 | 414,753 → 418,741 (+3,988) | 1,718,992 | 5 (5) | 5.6 / 2.89 |
| office-front | 120 | 638,802 → 649,646 (+10,844) | 89 → 90 | 37 → 37 (19 + 6 + 12) | 183 → 200 | 425,469 → 430,321 (+4,852) | 1,723,148 | 14 (14) | 3.48 / 2.67 |
| office-front | 210 | 633,674 → 635,546 (+1,872) | 85 → 85 | 36 → 36 (18 + 6 + 12) | 146 → 147 | 382,521 → 384,005 (+1,484) | 1,648,898 | 0 (0) | 3.98 / 2.78 |
| office-front | 300 | 622,986 → 624,136 (+1,150) | 83 → 83 | 36 → 36 (18 + 6 + 12) | 132 → 133 | 300,883 → 300,617 (-266) | 1,542,586 | 0 (0) | 4 / 5.17 |
| cell-open | 30 | 748,919 → 768,845 (+19,926) | 135 → 137 | 40 → 40 (20 + 7 + 13) | 312 → 333 | 772,761 → 777,755 (+4,994) | 2,307,089 | 18 (18) | 6.84 / 6.36 |
| cell-open | 120 | 737,507 → 788,387 (+50,880) | 138 → 140 | 40 → 40 (20 + 7 + 13) | 334 → 370 | 705,823 → 714,211 (+8,388) | 2,282,597 | 29 (29) | 7.16 / 3.83 |
| cell-open | 210 | 734,330 → 752,426 (+18,096) | 133 → 134 | 40 → 40 (20 + 7 + 13) | 303 → 330 | 688,443 → 695,351 (+6,908) | 2,191,585 | 15 (15) | 6.56 / 4.53 |
| cell-open | 300 | 751,512 → 757,208 (+5,696) | 135 → 136 | 41 → 41 (21 + 7 + 13) | 238 → 251 | 610,560 → 616,438 (+5,878) | 2,121,534 | 4 (4) | 6.21 / 5.92 |

Cascades (every, first view): TacticalShadows on, focusDepth 160; 2 cascades, shadow distance 200 m, splits 0.500/0.000/0.000, tile 2048; per cascade casters / tris: 9 / 33,276, 168 / 385,465.

Reading it: the all-pass triangle estimate (main + SSAO depth-normals prepass over opaque + shadow per cascade) is the number to set against UnityStats triangles once the Game-view rerun exists; the baseline's UnityStats SetPass (55-69) against this estimator's 36-41 says the estimator undercounts by the fullscreen and per-camera passes it leaves out, so read its deltas, not its level. ms are Studio batchmode offscreen, indicative only.
