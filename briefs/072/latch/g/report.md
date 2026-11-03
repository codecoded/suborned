# 072-G rebuild against the textured v4 capture (batchmode offscreen), 2026-10-28 00:10

Textured v4: out/scratch/latch/072/v4/v4.world.json sha256 a50c649a5c84b23ab799a32c9cff945fd77fde25a01013483ea0702c5fbb4fc2 (2026-10-27 22:53). 072-G: out/scratch/latch/072/g/g.world.json sha256 6841dcfc6f91ddea79ec6f275e5191f516dc91ffad884005c85724decbe1ce88 (2026-10-28 00:10). Same views, camera and estimator (cameras/play posed as HandoverHost.PlaceCamera: orthographic 14, pitch 38, standOff 160 m; cell-open from HandoverHost.frame.World(OpeningFocus); Camera.Render into Capture's HDR target 1920x1080 (no Game view)).

**Nothing here is UnityStats.** No number here is UnityStats (that needs the visible Game view; queued for when the Studio is free). census* = Baseline072's census method (16:9 frustum + LOD pick; counts shadows-only renderers as visible, as the baseline did). main* = the same without shadows-only renderers. setPassEstimate = distinct shader variants: forward (every visible material, its keywords) + SSAO depth-normals prepass (opaque, _ALPHATEST_ON/_NORMALMAP) + shadow (per cascade, its casters, _ALPHATEST_ON); fullscreen passes (sky, SSAO, post, copies) not counted. shadow* = casters per cascade by URP's split for this camera reproduced (CullingResults culling planes and sphere, bounds test; LOD-picked meshes): an estimate. frameMs = batchmode offscreen Camera.Render, GPU waited on, indicative only.

## Counts, as built (every face): textured v4 → 072-G

| view | yaw | main tris | materials | SetPass est. (fwd + dn + shadow) | casters | shadow tris | batchmode ms (median) |
|---|---:|---:|---:|---:|---:|---:|---:|
| office-front | 30 | 653,222 → 653,254 (+32) | 88 → 88 | 35 → 35 (19 + 5 + 11; +0) | 168 → 168 | 418,741 → 418,785 (+44) | 6.29 → 4.11 |
| office-front | 120 | 649,646 → 649,714 (+68) | 90 → 90 | 38 → 38 (20 + 6 + 12; +0) | 200 → 200 | 430,321 → 430,679 (+358) | 6.41 → 6.37 |
| office-front | 210 | 635,546 → 635,578 (+32) | 85 → 85 | 36 → 36 (18 + 6 + 12; +0) | 147 → 147 | 384,005 → 384,005 (+0) | 5.91 → 3.51 |
| office-front | 300 | 624,136 → 624,128 (-8) | 83 → 83 | 36 → 36 (18 + 6 + 12; +0) | 133 → 133 | 300,617 → 300,615 (-2) | 5.84 → 5.41 |
| cell-open | 30 | 768,845 → 768,881 (+36) | 137 → 137 | 41 → 41 (22 + 7 + 12; +0) | 333 → 333 | 777,755 → 772,663 (-5,092) | 7.38 → 5.15 |
| cell-open | 120 | 788,387 → 788,345 (-42) | 140 → 140 | 42 → 42 (22 + 7 + 13; +0) | 370 → 370 | 714,211 → 715,703 (+1,492) | 5.14 → 4.11 |
| cell-open | 210 | 752,426 → 752,384 (-42) | 134 → 134 | 42 → 42 (22 + 7 + 13; +0) | 330 → 330 | 695,351 → 696,259 (+908) | 7.38 → 4.08 |
| cell-open | 300 | 757,208 → 757,166 (-42) | 136 → 136 | 43 → 43 (23 + 7 + 13; +0) | 251 → 251 | 616,438 → 613,806 (-2,632) | 6.93 → 6.7 |

District module shadow proxies (builder log): 4,654 every-face module proxies from L2, 966,462 → 1,375,578 triangles (+409,116, the window holes); all shadow proxies 6,458, 3,711,900 → 4,121,016 triangles.

## Texture memory of the scene

| | textures | GPU (format x size x mips x layers) | Editor runtime | surface_* set GPU |
|---|---:|---:|---:|---:|
| textured v4 (2026-10-27 22:52) | 79 | 174.45 MiB | 348.97 MiB | 16.00 MiB |
| 072-G (2026-10-28 00:10) | 79 | 174.45 MiB | 348.97 MiB | 16.00 MiB |
| delta | +0 | +0.00 MiB | +0.00 MiB | budget 24 MiB |

## Screen area by map kind (mean of the 8 play views; all materials drawn opaque)

| kind | materials textured v4 → 072-G | mean share textured v4 → 072-G |
|---|---:|---:|
| textured | 33 → 33 | 56.06 % → 56.07 % |
| flat+detail | 36 → 36 | 26.11 % → 26.11 % |
| flat | 55 → 55 | 11.66 % → 11.66 % |
| swatch-atlas+detail | 2 → 2 | 6.16 % → 6.16 % |

Flat colour left (flat + swatch-atlas without detail): 11.66 % → 11.66 % of the frame.

## F1: module shadows from the L2 proxies against their own LODs (same three walls as briefs/072/shots/f1/f1.json)

| wall | before holes: mean abs diff / px > 24 | with holes: mean abs diff / px > 24 |
|---|---:|---:|
| bld/lamplight-south-w/2/k8 (SideBay_3, yaw 300) | 1.66 / 2.87 % | 0.49 / 0.33 % |
| bld/lamplight-east/11/k10 (SideBay_3, yaw 300) | 2.04 / 3.70 % | 0.52 / 0.25 % |
| bld/parade-south/5/k0 (SideBayStack_Base, yaw 300) | 2.32 / 4.76 % | 0.34 / 0.20 % |

## Crossing heads by day (signals.json `target`, runtime look installed; Signals072, 3840 x 2160, face-on yaw)

face-on yaw only (within 45 deg of facing+180); top 1% most vivid median in the head figure rect; rim_blue_excluded drops hue 195-265 (the cobalt band on the ring back); CIEDE2000 via briefs/061/glint/grade.py

| shot | state | target | measured (rim blue excluded) | dE2000 | model `shows` |
|---|---|---|---|---:|---|
| h1-walk-y210.png | walk | #55E6B0 | #66E9BC | 2.4 | #54EAB4 |
| h2-walk-y030.png | walk | #55E6B0 | #67E9BC | 2.4 | #54EAB4 |
| h3-walk-y210.png | walk | #55E6B0 | #66E9BC | 2.4 | #54EAB4 |
| h1-clearing-y210.png | clearing | #FFB624 | #FEA900 | 3.9 | #FFB500 |
| h2-clearing-y030.png | clearing | #FFB624 | #FEAA00 | 3.5 | #FFB500 |
| h3-clearing-y210.png | clearing | #FFB624 | #FEA900 | 3.9 | #FFB500 |
| h1-stop-y210.png | stop | #F2432E | #F33A30 | 1.9 | #FE2D23 |
| h2-stop-y030.png | stop | #F2432E | #F43C30 | 1.5 | #FE2D23 |
| h3-stop-y210.png | stop | #F2432E | #F33A30 | 1.9 | #FE2D23 |
