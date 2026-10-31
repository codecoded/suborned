# Brief 086 · LOD plan (Redline, 2026-10-31)

Read-only audit. Nothing in Unity was opened, no player or batch ran, nothing was built. I read files, counted the scene YAML and did arithmetic.

Number labels: **MEASURED** = a harness or player wrote it. **COUNTED** = I counted it from files today. **ESTIMATE** = my arithmetic, with its basis.
Scripts and data, all in this folder, re-run with `python3 -I`:
`placed_census.py` → `placed-census.json` · `lod_groups.py` → `lod-groups.json` · `piece_truth.py` → `piece-truth.json` · `key_items.py` → `key-items.json`

## The answer

1. **LODs are not the biggest win for the hard limit, 60 fps at play zoom.** At play zoom the frame is limited by the GPU and mostly by pixels. Almost every kit piece must stay at L0 there, by design. My upper bound for what LODs can save at play zoom is about 0.5 ms (§3).
2. **LODs are the right fix for third person.** tp-worst's 6.9 M triangles against the 6 M limit has a plain cause: **the old town's 9,251 placed pieces have no LODs at all.** Only 19 robots do. They draw L0 out to the 350 m clip. Three pieces alone should take tp-worst to about 5.5 M.
3. **Nobody knows today's frame time on the Air.** The last Air run is five days old, on a different scene, at the wrong size. The current gate row also runs at zoom 14 and 2880 × 1864, while the bar is zoom 10 at 1920 × 1080. One Air sitting settles this, and it comes first.
4. **Crossfade will not do what it sounds like under this camera.** The drawer refuses timed fades, and the wheel jumps the zoom straight across any fade band in one frame (§4.1). In third person it works, but the palm figures say good levels may not need it.

## 1. Where the cost is

### 1.1 Measured numbers, and their age

| machine, build, date | scene | size, view | mean / p99 ms | triangles | SetPass | casters | drawn (beyond 150 m) | source |
|---|---|---|---:|---:|---:|---:|---:|---|
| Studio (M5 Max), 0.2.181, Oct 31 07:39, load 12.9–14.1 on 18 cores | OldTown | 2880 × 1864, play at **zoom 14** | 3.49 / 4.93 | 1.26 M | 111 | 337 | 202 (118) | `out/batch/tplong/tplong.json` |
| same | same | tp-street | 4.11 / 5.47 | 5.04 M | 222 | 882 | 2,069 (1,398) | same |
| same | same | tp-worst | 4.18 / 5.33 | **6.91 M** | 211 | 875 | 1,894 (1,284) | same |
| Air, 0.2.122, Oct 26, cold | **HarbourDistrict** | 2880 × 1864, play (zoom 14 row) | 14.98 / 19.27 | 1.81 M | 117 | n/a | n/a | `briefs/064/air-0122/` |
| same | same | tp-street | 14.30 / 18.47 | n/a | n/a | n/a | n/a | same |
| same | same | tp-worst | 14.93 / 17.87 | 5.14 M | 218 | 889 | 1,894 (1,253) | same |
| Air, 0.2.122, Oct 26, after about 6 min | same | play | **20.99 / 25.97** | 1.81 M | 117 | n/a | n/a | same |
| same | same | tp-street | 21.87 / 30.82 | 3.22 M | 210 | 793 | 1,362 (933) | `tplong-2/tplong.json` |
| same | same | tp-worst | **23.26 / 28.89** | 5.14 M | 218 | 889 | 1,894 (1,253) | same |
| Air, 0.2.122, Oct 26 | same | 1920 × 1080, late fight | 12.66 / 19.71, 14.11 / 23.14 | n/a | n/a | n/a | n/a | same |

What these rows do not contain:
- **No Air number exists for OldTown**, the scene Sky plays. `briefs/075/redline/perf-075ab.json` is a spec that never ran. Brief 078 says the same.
- **No row at zoom 10**, the play zoom since Oct 30. `play-opening` still defaults to 14 (`tplong.ts:68`).
- **No row at zoom 18.5**, the widest zoom.
- **No Air row at 1080 for play or third person.** Sky set the 1080 bar on Oct 26, but `DEFAULT_SIZE` is still 2880 × 1864 (`tplong.ts:16`). Redline's handoff to Splice in brief 064 is still open.
- **The soak never ran.** I found no result from `soak-air.sh`, so "warm" means about 6 minutes, and throttling is supported but not proven.
- **The per-pass GPU recorder was never built.** `gpuElapsedNanoseconds` appears nowhere under `unity/Assets/Suborned`.
- **Bars don't compare across machines.** The Studio's frame equals its main thread (3.49 / 3.49) and it runs under load, so it can't rank GPU costs. Its counts are good. Its milliseconds aren't.

**Rise from the scene change, MEASURED (brief 078):** tp-worst went 3.8 M → 6.9 M from HarbourDistrict to OldTown. The Studio's frame went 4.0 → 4.2 ms.

### 1.2 The split by what's drawn

**There is no measured milliseconds split by class anywhere.** Here is everything real we have:

| class | what we have | kind, age |
|---|---|---|
| whole frame vs triangles | Cold on the Air, tp-worst draws 2.8× play's triangles and 6.6× its renderers at the same 14.9 ms. Warm upper bound: about 0.7 ms per extra million triangles, shadows included (064 A5) | MEASURED ratio, Oct 26, HarbourDistrict |
| pixels | About 53–64 % of the warm play frame scales with pixels (064 A3) | two-point model, crude |
| shadows | Shadows off saved 24–40 % of the frame on the Air, 008 B6 | MEASURED, contaminated run, ratios only, old build |
| shadow triangles at play | cell-open 30: camera 700 k + shadow 615 k. office-front 30: 556 k + 375 k. Zoom 14, OldTown through our pipeline. So shadows are **35–47 % of play triangles** | estimator (Oct 28), not the Profiler, `briefs/075/oldbuild/perf-capture/counts.md` |
| discard and alpha test | The fade's discard alone halved the heaviest stop before it was limited to the reach (008 B6) | MEASURED, old build |
| DecoKit pieces, tp-worst | Old-art pieces in the census top 30: 817 drawn, **2.54 M triangles at L0** (census count × old manifest tris). tp-street: 905 drawn, 2.14 M | COUNTED, Oct 31. The census lists only the 30 pieces with the most far renderers |
| facades and buildings | tp-worst census: `facade` 86 drawn, plus old-art bays. No triangle split | COUNTED, renderers only |
| planting | 27 groups declare `lodAtPlay`: 459 k at L0, 253 k at zoom 10, 169 k at zoom 14. Those are scene-wide sums, not per view. No frame cost | COUNTED |
| figures | 14 in the play census, at most 12 k each by the GDD budget, so at most about 170 k (about 13 % of 1.26 M). No frame cost | ESTIMATE |
| lights | Day: the sun. Evening and night have **no Air row at all** (064 A3) | not measured |
| HUD | Air main thread at play 8.8 / 8.0 ms against 5.8 / 7.5 ms at tp-worst, so play carries more main-thread work. Not split | MEASURED total only, Oct 26 |

### 1.3 The one measurement that settles it

**One Air sitting of about 25 minutes, on the build after brief 085, with `soak-air.sh` adapted.** It needs a small player change and Sky's OK. Rows, warm, -novsync, OldTown:

1. `play-z10`, `play-z14`, `play-z18`, `tp-street`, `tp-worst` at 1920 × 1080.
2. `play-z10` at the shipped fullscreen size, which is what Sky sees.
3. Per pass, through `Recorder.gpuElapsedNanoseconds`: main-light shadow, opaques, transparents, bloom and post, SMAA, final blit, UI.
4. Three **ceiling variants**, interleaved A B A B, needing no art:
   - `old-far-off`: old-art pieces beyond 150 m hidden. This is the most any LOD pass on them can give.
   - `planting-off`: the `lodAtPlay` groups hidden. This prices the leaves' alpha test at play.
   - `shadows-off`.

Result: if `old-far-off` saves under 1 ms on tp-worst, the pass is about the 6 M gate and not the frame, and Sky hears that before forty pieces get made. The numbers below are estimates until this runs.

## 2. Today's LOD state

### 2.1 Who has LODs (COUNTED from `OldTown.unity`, 276 MB, Oct 31 07:20)

| what | count | LODs |
|---|---:|---|
| old-art prefab instances (`OldTownArt/DecoKit/Prefabs`) | 9,251, holding 17.27 M placed L0 tris | **None**, except 5 robot prefabs (19 placed). `OldTownArt/DecoKit/Models` holds 207 FBX and **no `_L1` file** |
| of those, on the tram and pylon network | 6.46 M placed tris | none |
| LODGroups in the scene | 2,192, **all under `Ours`** | 1,058 cull-only, 41 two entries, 1,073 three, 20 four |
| fade mode on all 2,192 | 0 (`None`), transition width 0 | set in `KitLods.cs:228`, `PlantingSwap077.cs:529` |
| ours, built-prefab instances | 70 | catalogue LODs |
| ours, merged batches (embedded meshes) | 1.51 M tris in 1,965 meshes | none, by design. Ground pieces 647 k, civic 206 k, buildings-every 125 k, roofs 87 k |
| `__ShadowProxy` | 1,044 | modules cast from L2; `moduleShadowProxies: true` |
| direct renderer shadow modes | 3,017 on, 3,804 off, 1,105 shadows-only | the scene sets no per-instance shadow override on old-art, so they cast as their prefab does (default on; I did not resolve each model) |

The catalogue, `contracts/world/kit.json`: 390 of 457 rows adopted, 299 of those with LOD rows. **A LOD row in the catalogue does not mean the game draws it.** Two reasons:

- **Old-art doesn't use the catalogue.** OldTown places old-art prefabs. Kit L1 and L2 files are never loaded for them.
- **865 cull-only groups have kit L1/L2 files that were dropped at build** (2.87 M placed L0). `lodCategories.street` puts L1 at 0.008 and L2 at 0.003, both below `KitLods.CullSmall` of 0.01, and `KitLods.cs:186` drops any level below the cull. So in our zone a PhoneBooth stays 17,776 tris until it vanishes.

**Other LOD code, read:**
- `DecoKitModels.cs:93` `AddLods`: L1 at 10 %, cull at 1.2 %. Only the robots use it.
- `ProcKit.cs`: distance detail is left to LODGroups, and tiers 0 and 1 stay in the grammar.
- `FacadeKit.cs:140`: a `lodAtPlay` row stands as an instance so it can switch at play, otherwise it bakes into a batch at L0.
- `ShadowProxies.cs`: facades and trees use L1, modules L2.
- `KitLods077Tests.cs`: covers planting levels, `lodSize` set after `SetLODs`, soil casting nothing, and the guard exempting declared rows only. It tests no fade, bounds or image difference.

**Geometry of old-art against the kit, COUNTED:**
- 53 of 142 placed old-art FBX are byte-identical to the kit's L0.
- 78 differ in bytes with equal triangles. Brief 028 saw this from a re-export, so **this needs a vertex compare, not a hash**.
- 11 really differ: PhoneBooth 34,744 old vs 17,776 kit, RecyclingBins 3,708 vs 1,676, StreamCorner_3, StreamCorner_4, StreamEnd_3, StreamEnd_6, StreamEnd_3W, CornerTower_12, DecoPavilion, DiscTower, BladeSign_Cafe. Kit L1s made from a slimmer L0 can't simply be attached to these.
- 49 old-art pieces have no kit levels at all, including kerbs, Deckchair, TerraceScreen, DrainGrate, PottedTopiary and BikeRack.

### 2.2 What pops today, and where

**At play zoom (orthographic).** Only pieces with a switch inside the wheel's 8 to 18.5, all switching in the same frame, no fade:

| pieces | switch zoom (rel = size / 2z) | note |
|---|---:|---|
| 20 planting groups at `lodSize` 4 (companions, civic beds, PlanterRun_4) | **12.0** | exactly on a wheel notch (8 + 0.5 n) |
| PlanterArc_TB × 4 | 12.37 | |
| FanPalm_Play × 30 | 17.75 | planning zoom |

Every other group has L0 across the whole wheel, as the `KitLods` play-zoom guard intends. That covers facades, palms and street props.

**Unasked consequence of zoom 10.** `briefs/077/READY-unity` says every planting piece draws its gameplay level at play zoom. That held at 14. At the new rest zoom of 10 they draw the level above ("near", about 1.75× a bed's triangles; 253 k vs 169 k, 1.5×, across the 27 groups). Maybe Sky prefers it. Nobody has shown him.

**In third person.**
- All 1,134 multi-level groups switch hard. Palms were measured fine in brief 028, by Blender renders only.
- **1,058 cull-only groups vanish hard.** A 0.82 m bollard goes at 79 m, about 11 px tall at 1080, before the haze starts at about 90 m. Hydrant 76 m, HangingBasket 75 m, DomeCamera 71 m. These are the likeliest visible pops today. Arithmetic from `ScreenAt`; **nobody has looked at them.**
- Old-art pieces don't pop. They draw L0 to the clip, which is the cost problem.

### 2.3 Key items, ranked by placed cost

**Rule:** 100,000+ placed L0 triangles, or 20+ drawn in a tp-long census, or named by Sky. `key-items.json` holds all 204 pieces, and a wider 76-row cut (50 k+). Mailbox isn't placed in OldTown, so Hydrant carries that test.

**How each number was got:**
- Placed: the scene YAML, active instances.
- Tris: old manifest for old-art, `kit.json` for ours.
- Kit levels: `kit.json`.
- Scene LODs: LODGroup docs in the scene, and the old-art prefab files.
- Drawn: `out/batch/tplong/census-tp-*.json` (0.2.181). "–" means not in that census's top 30, not zero. The census merges kerb variants and AlleyWall variants, so those read on one row or as "–".

| # | piece | old-art placed × tris | ours placed × tris | placed L0 tris | kit levels | LODs in the scene | drawn tp-worst / tp-street |
|---:|---|---:|---:|---:|---:|---|---:|
| 1 | Tile_TramRail | 1584 × 3,140 | 0 × 3,140 | 4,973,760 | 784 / 120 | old: none | 305 / 271 |
| 2 | CoconutPalm | 225 × 6,310 | 77 × 6,310 | 1,905,620 | 1,330 / 514 | old: none, ours: 2L + cull | 62 / 72 |
| 3 | StreamCorner_3 | 76 × 12,170 | 6 × 12,302 | 998,732 | 2,995 / 715 | old: none, ours: 2L + cull | – / – |
| 4 | Cypress | 163 × 5,004 | 0 × 5,004 | 815,652 | 1,251 / 300 | old: none | – / 20 |
| 5 | StreamBay_3 | 484 × 1,444 | 27 × 1,444 | 737,884 | 310 | old: none, ours: 1L + cull | 10 / 80 |
| 6 | Streetlamp | 103 × 5,782 | 20 × 5,782 | 711,186 | 1,445 / 345 | old: none, ours: cull only | 46 / 15 |
| 7 | FanPalm | 158 × 4,194 | 0 × 4,194 | 662,652 | 1,048 / 251 | old: none | 16 / 21 |
| 8 | PhoneBooth | 7 × 34,744 | 22 × 17,776 | 634,280 | 4,443 / 1,049 | old: none, ours: cull only | – / – |
| 9 | PortholeBay_3 | 136 × 4,354 | 0 × 4,354 | 592,144 | 1,062 / 253 | old: none | – / 26 |
| 10 | Kerb_Granite_0 | 1130 × 494 | 0 × 494 | 558,220 | none | old: none | 71 / 370 |
| 11 | Kerb_Granite_1 | 1127 × 494 | 0 × 494 | 556,738 | none | old: none | – / – |
| 12 | AlleyWall_4 | 0 × 0 | 116 × 4,418 | 512,488 | 190 / 206 | ours: 2L + cull | – / – |
| 13 | AlleyWall_3 | 0 × 0 | 111 × 3,776 | 419,136 | 168 / 168 | ours: 2L + cull | – / – |
| 14 | StreamCorner_4 | 16 × 14,002 | 10 × 14,170 | 365,732 | 3,439 / 823 | old: none, ours: 2L + cull | – / – |
| 15 | CafeSet_Tulip | 85 × 3,984 | 6 × 3,984 | 362,544 | 996 / 239 | old: none, ours: cull only | 33 / – |
| 16 | PlazaSphereLamp | 18 × 7,932 | 27 × 7,932 | 356,940 | 1,982 / 475 | old: none, ours: cull only | – / 14 |
| 17 | Bollard | 41 × 5,116 | 25 × 5,116 | 337,656 | 1,278 / 306 | old: none, ours: cull only | 28 / – |
| 18 | Bench | 41 × 3,412 | 40 × 3,412 | 276,372 | 852 / 378 | old: none, ours: cull only | 16 / – |
| 19 | PottedTopiary | 39 × 4,720 | 14 × 4,720 | 250,160 | none | old: none, ours: cull only | – / – |
| 20 | Deckchair | 66 × 1,888 | 66 × 1,888 | 249,216 | none | old: none, ours: cull only | 39 / – |
| 21 | Planter_Trough | 27 × 2,710 | 62 × 2,710 | 241,190 | 675 / 160 | old: none, ours: cull only | 15 / – |
| 22 | BalconyBay_3 | 62 × 3,340 | 3 × 3,340 | 217,100 | 774 / 202 | old: none, ours: 2L + cull | – / – |
| 23 | RobotWaiter | 5 × 15,324 | 8 × 15,324 | 199,212 | 3,827 / 912 | old: LOD, ours: cull only | – / – |
| 24 | Newsstand | 0 × 0 | 11 × 17,964 | 197,604 | 5,748 / 5,748 | ours: cull only | – / – |
| 25 | Hydrant | 10 × 6,840 | 18 × 6,840 | 191,520 | 1,710 / 409 | old: none, ours: cull only | – / – |
| 26 | AlleyWall_4D | 0 × 0 | 35 × 5,254 | 183,890 | 200 / 218 | ours: 2L + cull | – / – |
| 27 | Tram | 5 × 36,644 | 0 × 36,644 | 183,220 | 9,145 / 2,156 | old: none | – / – |
| 28 | BikeRack | 14 × 7,572 | 10 × 7,572 | 181,728 | none | old: none, ours: cull only | – / – |
| 29 | HangingBasket | 15 × 2,140 | 65 × 2,140 | 171,200 | 533 / 128 | old: none, ours: cull only | 18 / – |
| 30 | PottedPalm_Small | 45 × 2,396 | 26 × 2,396 | 170,116 | 598 / 143 | old: none, ours: cull only | 20 / – |
| 31 | AlleyWall_3D | 0 × 0 | 35 × 4,614 | 161,490 | 178 / 180 | ours: 2L + cull | – / – |
| 32 | RecyclingBins | 19 × 3,708 | 54 × 1,676 | 160,956 | 418 / 126 | old: none, ours: cull only | 10 / – |
| 33 | DomeCamera | 0 × 0 | 20 × 7,550 | 151,000 | 1,887 / 452 | ours: cull only | – / – |
| 34 | TramCanopy | 10 × 15,028 | 0 × 15,028 | 150,280 | 3,757 / 901 | old: none | – / – |
| 35 | DrainGrate | 35 × 4,068 | 0 × 4,068 | 142,380 | none | old: none | 13 / – |
| 36 | CurvedKiosk | 7 × 17,260 | 1 × 17,260 | 138,080 | 4,314 / 1,020 | old: none, ours: cull only | – / – |
| 37 | CCTVPole | 0 × 0 | 20 × 6,882 | 137,640 | 1,720 / 407 | ours: cull only | – / – |
| 38 | Planter_Urn | 36 × 3,642 | 0 × 3,642 | 131,112 | 905 / 214 | old: none | 10 / – |
| 39 | TicketKiosk | 0 × 0 | 6 × 21,342 | 128,052 | 5,335 / 1,266 | ours: cull only | – / – |
| 40 | FanPalm_Play | 0 × 0 | 30 × 4,194 | 125,820 | 1,048 / 251 | ours: 2L + cull | – / 18 |
| 41 | TerraceScreen | 40 × 3,048 | 0 × 3,048 | 121,920 | none | old: none | 26 / – |
| 42 | ShrubMound | 34 × 2,526 | 13 × 2,526 | 118,722 | none | old: none, ours: cull only | – / 16 |
| 43 | RobotNanny | 4 × 19,688 | 2 × 19,688 | 118,128 | none | old: LOD, ours: cull only | – / – |
| 44 | WindowBox | 0 × 0 | 48 × 2,298 | 110,304 | none | ours: cull only | – / – |
| 45 | StreamBay_4 | 0 × 0 | 60 × 1,706 | 102,360 | 364 / 132 | ours: 2L + cull | – / – |
| 46 | SignalLens | 0 × 0 | 35 × 822 | 28,770 | 234 | ours: cull only | – / 40 |
| 47 | CornerPier_Out_Top | 0 × 0 | 63 × 456 | 28,728 | 160 / 64 | ours: 2L + cull | – / 22 |
| 48 | StreamBayStack_Top | 0 × 0 | 69 × 414 | 28,566 | 158 / 86 | ours: 2L + cull | – / 32 |
| 49 | SkyRail_Pylon_Tall | 75 × 308 | 0 × 308 | 23,100 | none | old: none | 22 / – |
| 50 | SkyRail_Pylon_Shaft | 81 × 40 | 0 × 40 | 3,240 | none | old: none | 21 / – |

Merged batches and contact shadows aren't pieces, so they aren't here (§2.1). Figures are Forma's and not in this table. Their LOD chain is outside this audit unless the ceiling run asks.

## 3. The lens

### 3.1 Confirmed

- **Package source, `com.unity.render-pipelines.core`:** `LODRenderingUtils.cs:15-18` returns `2 × orthoSize` as the metric for an orthographic camera. `InstanceCuller.cs:319` uses that metric for every group with no distance term (`isOrtho ? sqrScreenRelativeMetric : …`). `PlantingSwap077.cs:558` computes the same rule. So **rel = size / (2 × zoom) × bias, the same near and far, changing only when the zoom changes.**
- **Unity's manual doesn't describe this.** I found no page stating the orthographic rule, so the source is the evidence.
- **Shadows:** `InstanceCuller.cs:2215-2220` builds its LOD inputs from one `lodParameters`, and brief 028 assumes shadow views use the main camera's level. I found no doc that settles it, and it isn't measured.

### 3.2 What LODs buy under each lens

Pixels at 1080: zoom 8 = 67.5 px/m, zoom 10 = 54, zoom 14 = 38.6, zoom 18.5 = 29.2. Half a pixel of error is 0.74 cm, 0.93 cm, 1.3 cm and 1.7 cm.

| view | orthographic | perspective, same framing |
|---|---|---|
| **play zoom 10** | All kit pieces at L0 by the guard. A LOD can only be a "this zoom" level for the whole frame. Triangles there are about 0.6–1.3 M (1.26 M at zoom 14; zoom 10 shows about half the ground, not measured). **Halving them all saves at most about 0.2–0.5 ms warm** (≤ 0.7 ms/M) | Same at frame centre. Near and far differ by the factors below, so a switch can **land inside the frame and sweep across it as you pan**. Orthographic can't do that |
| **zoom 14 to 18.5** | 3.4× the ground of zoom 10, each piece 46 % smaller. This is where a zoom-tier for small props could earn, if zoom 18.5 fails. No number exists | Same, with the same in-frame rule |
| **third person, FOV 55** | n/a, always perspective | **Full value.** 1,284 of 1,894 renderers beyond 150 m. Half a pixel is 1.85 cm at 38 m, 7.2 cm at 150 m, 17 cm at 350 m, and the haze helps from 90 m |

**In-frame depth for a perspective play lens** (ESTIMATE; 38° pitch, flat ground, buildings ignored; the ratio doesn't change with zoom):

| vertical FOV | stand-off at zoom 10 | near edge scale | far edge scale |
|---:|---:|---:|---:|
| 20° | 57 m | ×1.29 | ×0.82 |
| 30° | 37 m | ×1.52 | ×0.75 |
| 45° | 24 m | ×2.1 | ×0.65 |

### 3.3 What to author, by lens

| piece class | orthographic | perspective play lens | third person (both) |
|---|---|---|---|
| high-tri small props (lamp, bollard, booth, kiosk) | L0 at play. L1 only if zoom 18.5 fails | Same; keep switches outside the frame's range | L1, L2, cull |
| trees and palms | L0 at play (Sky approved L1 60 m, L2 140 m) | Same | L1, L2 |
| planting beds | Existing levels; move the switch off the notch | Check the sweep | Existing |
| facade modules | L0 at play, L2 shadow proxy, as built | Same, guard reworked | As built; **add to old-art** |
| ground (rail, kerb) | L0 at play; no cull | Same | L1, L2, never a hole |
| casters | L1/L2 proxy behind L0, at any zoom | Same | Same |

### 3.4 Does the plan need Sky's lens call first?

**Mostly no.**
- The meshes are the same under both lenses, so pieces, review and third-person work can start.
- **What waits for the call:** the switch-point table and `KitLods.GuardM` (36 m, derived from orthographic zoom 18.5).
- With a tool computing switches from measured error and the lens, a lens change later is a rerun, not a redo.
- Under a perspective lens the guard must become: at rest zoom, no switch inside rel × [far factor, near factor].
- Brief 086 is my only source for the lens question. I searched `briefs/085-play-notes-0177.md` and `briefs/085/` for "perspective" and found nothing.

## 4. Techniques, judged for this game

### 4.1 LOD crossfade with dithering

**Support is already there:**
- `m_EnableLODCrossFade: 1`, dither type 1 (BlueNoise), QualitySettings `enableLODCrossFade: 1`.
- `multi_compile _ LOD_FADE_CROSSFADE` in seven passes each of `DK_LitCutaway`, `DK_ComplexLitCutaway` and `DK_LitCutawayLeaf` (I did not map each pass by name, not run). **`FacadeLit` has it in one pass only** (line 182), so facade shadows and depth won't dither: Latch to check. `LODFadeCrossFade` is a blue-noise lookup and a `clip`.
- Not checked: Signal Dots, Cross Glow, Contact Shade.

**Limits that decide the design:**
- **GPU Resident Drawer: no timed fades.** "Animated cross-fading transitions are not supported"; transitions "fall back to static, distance-based cross-fading" (Unity manual). The 6000.7 reference goes further: a group with Animate Cross-fading is **excluded from the drawer**. The package has `LODAnimateCrossFading` as an exclusion reason. So keep timed fades off.
- **Under orthographic, a static band is a zoom band, and the wheel skips it.** `OverheadWheel` (`HostCameraLab.cs:95`) sets `orthographicSize` directly, 0.5 a notch, so the zoom crosses a band in one frame. A player resting inside a wider band stays dithered and draws both levels until he zooms again. **At play zoom, crossfade adds cost and no fade.**
- **The whole picture already jumps 5 % on that frame,** which should mask much of a level change. Not tested.
- **Apple GPU cost.** A fading draw uses `clip`, which Apple classes as a "feedback" fragment and which costs hidden-surface removal (WWDC20). Both levels draw during the fade. The Stencil dither type should avoid `clip` in colour passes (read, not run), but its seed pass runs whenever it's selected (`UniversalRenderPipeline.cs:2026`), and Unity calls its GPU cost "slightly higher". Our shaders write stencil bit 1 for motion, which needs checking against bits 4 and 8.

**Verdict:**
- **Third person: per piece, only where the metric fails.** Band 10–15 %. Price it with an interleaved Air A/B on a tp-street walk.
- **Play zoom: off.** If Sky sees a zoom pop on the slice, the fix is smoothing the wheel (his call on feel), which would also let a band work.
- **Evidence that it may not be needed:** brief 028's palms with no fade changed 41–71 of 2,073,600 pixels by more than 12/255 across a street frame. Blender only.

### 4.2 Mesh LOD (6000.x)

**Reject for the kit.**
- It stays in the drawer and has the only timed fade there (0.333 s in `InstanceCuller.cs:147`), and it handles orthographic.
- But it is generated on import with no silhouette controls on the pages I read: "edges to collapse", about half the indices per level.
- Unity says it "might produce undesired results on meshes with a high curvature or meshes that consist of many disconnected pieces". That describes chrome street furniture.
- Same materials at every level; combining it with LOD Group is "not recommended"; authored levels aren't covered on those pages.
- Sky reviews authored levels. This fights that.
- It could be tried later on a large organic piece. Not in this pass.

### 4.3 Silhouette-preserving simplification

Use three routes, chosen per piece by the metric in §5.2:

1. **Generator parameters for hard-surface DecoKit** (`lean`, bevel 0, `far=`/`near=` hand-built levels, `tools/decokit/kit.py`). Best silhouette and chrome. Brief 075's hand-built rail L1 "keeps the groove and the stone joints". Vela.
2. **meshoptimizer for what the generator can't rebuild:** `meshopt_simplifyWithAttributes`, normals weight about 1, UVs 10–100, `LockBorder` for pieces that join, `SimplifyPrune` for tiny parts. It **returns the achieved error**, which is what drives the switch point. Its docs warn off `simplifySloppy` for quality. Splice.
3. **Blender collapse (`DECIMATE`, ratio only)** as `kit.py:95` does today. It targets a ratio and reports no error, so it needs the render metric to judge it. Last resort. The slimmed hydrant is what this route risks.

Foliage stays card or hand levels, with leaf density held so the crown doesn't thin.

**QEM background:** Garland and Heckbert 1997; Hoppe's appearance-attribute metric. Search extracts only, not read.

### 4.4 Switch points: projected error, with hysteresis

**Adopt error-based switches.** Replace the catalogue's fixed `screen` values with one computed from each level's measured error:
- Perspective, from the meshoptimizer docs: `e = d × tan(fov/2) × 2 / screenHeight`, switch when `e × factor ≥ error`.
- Orthographic: one pixel is `2 × zoom / height`, so the rule becomes a zoom.
- Target: 1 px or less at the switch, tightened per piece if the image metric asks. Then `screen = size / (K × d)`, as `KitLods.ScreenAt`.
- **Tool work, no frame cost.**

**Hysteresis: not now.** Unity's LODGroup has none and the drawer's static band stands in for it. Orthographic switches happen only on a zoom notch. A scripted `ForceLOD` loop is possible (the package honours `forceLODMask`, `InstanceCuller.cs:305`, but a forum user reports it not working with the drawer), but it costs main-thread work for a problem nobody has shown.

**Move the planting switch off the notch** (12.0 → about 12.25). One value per row, no art.

### 4.5 Shadow LODs and proxies

**Good value, at any zoom, invisible if done right.** The system exists (`ShadowProxies.cs`).
- At the opening shadows are 35–47 % of triangles. Old-art pieces cast from L0 out to the perspective fit's 110 m.
- **Extend proxies to high-tri props and old-art:** L1/L2 casting behind an L0 at play. At about 5 cm a texel (064 A3), most prop detail is below a texel.
- ESTIMATE: up to about 0.3 M triangles at play, so up to about 0.2 ms. Modest, but it's the one geometry saving that works at play zoom without touching what you see.
- Cascades and distance are already fitted per camera (`TacticalShadows`: 2 cascades; 110 m in perspective). Leave them.
- `m_SoftShadowQuality: 3` (High) is still on. Moving it to Medium is estimated at 0.3–0.6 ms at 1080 (064 A3 item 3 rescaled) and is **not a LOD at all**.

### 4.6 HLOD and merged clusters

**Not now.** Epic's docs describe the purpose as reducing draw calls (search extract). Our far-band draws cost CPU, and the Air's render thread at tp-worst was 4.2 ms warm in a 23 ms GPU-bound frame. Ground and buildings are already merged by material in 64 m tiles. Revisit only if the Air row shows the render thread within about 2 ms of the GPU.

### 4.7 Impostors

**Reject for now.**
- At 350 m clip with 97 % haze, Sky has already approved palm L2 cards with no impostor. Brief 028's boards compared one.
- Every impostor pixel is alpha-tested, the Apple GPU's expensive case.
- It costs atlas memory, and the far buildings' L2 is already light (`StreamBay_3` 310, `AlleyWall_3` L2 168).
- Only the long-tier flyby would want them, and that's a demo.
- I could not open a primary source: Brucks' page returned no article body, and the one write-up I tried returned 404.

### 4.8 GPU Resident Drawer

**Keep on, occlusion off.**
- 064 measured the drawer's occlusion at +36 % on the Studio, render thread +3 ms.
- Requirements are met: Forward+, `m_BrgStripping: 2`, shaders with DOTS (075 counts).
- **Static batching is now off** (`m_StaticBatching: 0`), so brief 048's note that static batching was on is out of date.
- **`EditorDrawerGuard` question:** the guard takes the drawer off in the Editor only and restores it for builds. So Editor measurements, including batchmode counts, run without it. `PerfProbe` is an Editor probe and reads with whatever the Editor has. **Only player rows count.**
- Exclusions that matter here: per-renderer `MaterialPropertyBlock` (481 signal parts at 0.2.122), timed fades, render callbacks.
- **Small-mesh culling** (`m_SmallMeshScreenPercentage: 0`, off): a one-line trial for ungrouped old-art. The package's per-cascade shadow cull applies only to renderers with no LODGroup (`InstanceCuller.cs:605`), so it would stop applying once they get LODs. Unity warns it "might not work" with your own LOD meshes. It's a cull with no fade, and bollard-sized pops are already the worry, so it comes **after** the ceiling run.

### 4.9 SRP Batcher and instancing

**Nothing to change.** A new level file adds meshes, not shaders, so SetPass shouldn't grow if slots keep their materials. Every level must keep L0's material order, and `KitLods` enforces slot mapping. Check SetPass on the slice.

### 4.10 Material LOD

**Only at L2 in third person, later.** Fewer slots on far levels (one atlas) cuts draws. Clear coat off on far levels saves shader time only on far pixels, which are few. At play zoom L0 holds, so it can't apply there. Brief 075 prices clear coat at play at 0.3–0.8 ms by a different route: a material choice for Glint and Sky, not a LOD.

## 5. The proposal

### 5.1 Ranked steps

Ranked by what they do for the limit. Savings are **estimates until step 1**, on the Air, warm.

| # | step | expected saving | how it's proved | owner | LOD? |
|---|---|---|---|---:|---|
| 1 | **Air sitting** (§1.3): rows at zoom 10 and 1080, per-pass GPU, ceiling variants; `tplong.json` rows and sizes fixed | none: it ranks everything below | the report, against 13.0 / 16.7 ms | Latch (player), Splice (rows), Redline (reads), Sky (OK) | no |
| 2 | **Shipped render size on the Air.** Fullscreen draws up to 2880 × 1864; the bar is 1080 | **6.5–8.5 ms** (064 A3 item 2 at 0.62), the largest item known | `play-z10` at shipped size before and after | Sky, Glint, Latch | no |
| 3 | **Per-pixel settings:** soft shadows Medium, bloom quarter, leaves' alpha test priced | 0.4–0.9 ms at 1080; alpha unknown, possibly large | interleaved Air A/B | Glint | no |
| 4 | **LODs for old-art, third person.** Slice first, then the list | **−1.9 M tris** at tp-worst on the census top 30; **at most −1.4 ms** warm | tp-long counts, Air A/B, review page | Vela, Splice, Latch | **yes** |
| 5 | **Fix the cull pops:** smaller pieces vanish at 71–79 m | none, but it's what "no pop" asks | §5.2 on the worst five | Latch, Vela | **yes** |
| 6 | **Use the L1s we already have** on 865 cull-only groups, after review | part of 2.87 M placed; per-view not computed | tp-long, review page | Splice (rows), Vela, Latch | **yes** |
| 7 | **Shadow proxies** for props and old-art | up to about 0.3 M at play, more in third person; at most about 0.2 ms | shadow mask A/B, tp-long casters | Glint, Latch | **yes** |
| 8 | **Error-driven switch tool**, and bounds that fail the build | enables 4–7 | `bun run test`, kit check | Splice | **yes** |
| 9 | **Planting:** move the switch off the notch, show Sky zoom 10's level | at most 84 k tris scene-wide; about 0.06 ms | A/B stills | Latch, Sky | **yes** |
| 10 | **Crossfade**, third person, only where the metric fails | a cost, not a saving: prices it | tp-street walk A/B | Latch | **yes** |

**Honest reading.** Sky asked for LODs, and step 4 onward is that plan. It reaches the third-person limit and gives the no-pop result he wants. **It cannot by itself bring play zoom to 60 on the Air if play zoom is short:** steps 1–3 do that. Run them together, since they don't conflict.

**The Air today, ESTIMATE:** play at 1080 warm is about 12.7–14.2 ms (064), against a 13.0 target, on the old scene before A3's cuts. The SSAO cut, depth and opaque changes are in now, and OldTown's other costs are unknown. It may already pass at 1080 and still fail at the shipped size.

### 5.2 Proving no pop

Calibrated from the palm numbers in brief 028, so **these are proposals for Sky**, and the slice may move them.

**Per piece, per switch (tool, rendered in Unity, not Blender):** eight azimuths, at the switch distance or zoom, 1920 × 1080, same frame, the game's lighting and haze.

| check | limit | palm for reference |
|---|---:|---|
| bounds, each axis | within 1 % or 2 cm, whichever is larger; **fails the build** (today `KitLods.cs:296` only warns, at 8 %) | n/a |
| LOD size | `LODGroup.size` same across levels | n/a |
| soft silhouette error Σ\|a0−ak\| / Σa0 | L0→L1 ≤ 1 %; L1→L2 ≤ 4 % | 0.3–0.4 %; 3.5–3.7 % |
| largest edge move, from the mesh error | ≤ 1 px at the switch | not measured |
| piece's pixels differing > 12/255 after a 2 px blur | median ≤ 1.5 %, worst azimuth ≤ 3 % | 0.8–1.5 %; worst 1.6–3.0 % |
| mean colour | ≤ 1/255 | 0.2–0.7 |
| whole 1080 frame at the switch, real placements | ≤ 100 pixels over 12/255, none over 32/255 | 41–71; max 23–26 |
| shadow mask, same frame | same silhouette limits | not measured |
| chrome and colour slots | every L0 material present in L1 unless Sky passes it | n/a |

`palm_lod_metrics.py` already computes silhouette, pixels and colour. Reuse it on Unity captures.

**In motion.** Rendered paths, LODs on against L0 forced, same frames:
- third person: a 3 s walk through each switch;
- play: pan at rest zoom, and wheel from 8 to 18.5 and back.

Metric: the largest frame-to-frame jump in difference inside the piece's mask, with L0 forced as the baseline. **Sky then sees it in the player, in both views.** The numbers can pass and his eye can still fail it, and his eye wins.

**Frame.** Air, warm, interleaved, per step, not per piece.

### 5.3 The review page

Self-contained `.html`, one piece a screen, ordered by placed cost. Splice builds it; nothing replaces L0 until Sky has passed it. A catalogue field records the pass, and `KitLods` refuses an unpassed level. Full-detail sources stay.

Per piece:
- id, placed count (old town, zone, network), triangles per level, drawn in each view.
- **Before and after, side by side, with a flip:** play zoom 10, at the switch, and close-up.
- Difference ×6 and a silhouette overlay.
- All §5.2 numbers, pass or fail, bounds per axis.
- Switch points in metres and zoom, for the lens as set.
- Shadow caster per level, with its A/B.
- A motion clip per view.
- Frame cost: triangle share of each view, as counted. **Milliseconds per piece aren't measurable.** Show the class's measured A/B, labelled as a class figure, not a piece figure.
- Pass, change or reject, and a note, saved.

### 5.4 The first slice: three pieces

| piece | why it tests the pipeline | saving at tp-worst (ESTIMATE, census × tris) |
|---|---|---:|
| **Tile_TramRail** | Rank 1. Ground piece: can't cull, joins its neighbours, so borders must lock. Old vs kit L0 differ in bytes, so it tests the vertex compare. Levels exist: 784 / 120 | 305 far × 3,020 = **−0.92 M** |
| **CoconutPalm** (old-art × 225) | Foliage: alpha test, shadow, fade. Sky-approved levels already exist, byte-identical L0. It should be a pure wiring test | 44 far × 5,796 = **−0.26 M** |
| **Streetlamp** | The character test: thin pole, chrome, placed × 103, shown at play. Levels 1,445 / 345 exist; **may need a hand L1** | 45 far × 5,437 = **−0.24 M** |

**Total −1.42 M: tp-worst about 5.5 M, under the 6 M limit, from three pieces.** Assumes all far instances reach L2 by 150 m; I expect real figures near this, and the slice measures them.

**Slice done-check:**
1. Three pages on the review page, passed or sent back by Sky.
2. tp-long counts at the expected figures, no new SetPass.
3. Unity captures through §5.2.
4. Sky has walked and zoomed it.
5. One Air A/B at tp-worst.
6. A replay check: this is view-only and shouldn't touch the sim, but `bun run batch`'s match row must still pass.

**Hydrant goes first after the slice,** because it's the piece Sky named. It's 28 placed, so it won't move a number, but it decides whether route 3 is ever acceptable.

**Sequence.** After brief 085's build lands: step 1 → slice → Sky's pass → the list in rank order, stopping to report whenever the tp-long curve flattens. Delta rule: tp-long and the review page, not a full batch per piece. World and kit changes do run the full batch, so group pieces into rounds.

### 5.5 Who does what

| owner | work |
|---|---|
| **Vela** | Levels, by route (§4.3). L1s for pieces without them, and for the 11 where the old L0 differs. `tools/decokit` |
| **Splice** | Catalogue fields (review status, per-id switch, old-art rows), vertex compare, error-to-switch tool, bounds check, review page, `tplong.json` rows at zoom 10 and 1080 |
| **Latch** | LODGroups on old-art placements (`SwapNetwork.Put` and the old builders, or a built variant: his call), the planting value, `-look` ceiling variants, per-pass GPU column, Unity captures, the crossfade trial |
| **Glint** | Shadow proxies' look, soft shadows, bloom, whether a level's light or reflection shifts at the switch |
| **Forma** | Nothing yet; figures only if the ceiling run points there |
| **Redline** | Reads the Air report, sets final limits, retests each round |
| **Sky** | Each piece, and §6 |

### 5.6 Risks

- **Group count:** old-art would add up to about 9,200 LODGroups to today's 2,192. Brief 048 ran 9,049, so this has precedent. Watch main and render threads in tp-long.
- **Scene and memory:** extra level meshes add memory. `bun run storage` and the texture and mesh figures should be read on the slice.
- **Old-art placement passes through old builders** that also run under `-swapCrowd` and old settings. Latch should say whether `OldTownSwap.Verify` still passes.
- **Lens change mid-pass:** rerun the switch tool; no art lost.

## 6. Decisions for Sky

Each goes to him with its board, through Aster.
1. **OK the Air sitting** on the next build, with `brew install macmon` on the Air for the GPU clock. **Recommend yes.** Everything else rests on it.
2. **The shipped render size on the Air** (his look call, GDD §11). **Recommend** drawing the 3D at 1080-equivalent with FSR on the Air if `play-z10` at the shipped size misses 13 ms warm. Show him stills first.
3. **What the pass is for.** LODs reach the third-person limit and the no-pop goal, but not necessarily play zoom's 60. **Recommend** running steps 1–3 alongside and ranking the work this way.
4. **The lens.** **Recommend he needn't decide before pieces start.** Decide before switch points freeze, after the slice. If perspective, 30° or narrower keeps the frame's depth range tight; at wider, LOD switches will sweep through the view while panning.
5. **Zoom pops.** The wheel jumps a notch in one frame. **Recommend** accepting that for now, since the whole image scales 5 % on that frame, and smoothing the wheel only if he can see a pop on the slice.
6. **Tram rail.** The old tile (3,140) is what he kept; a hand-built one exists at 454 / 216 / 96 with a different section. **Recommend** the old tile with LODs, keeping his look, and showing the hand-built version beside it.
7. **Which L0 is the truth for the 11 differing pieces** (PhoneBooth 34,744 old vs 17,776 kit). **Recommend** the placed old-art stays L0 under character over triangles, with new levels made from it.
8. **The no-pop limits in §5.2**, and **planting at zoom 10** (near level, about 1.5× tris overall, against gameplay level). **Recommend** adopting the limits provisionally for the slice, and showing him the planting A/B.

## 7. What I could not measure or open

**Not measured:**
- Any frame time on the Air for OldTown; anything at zoom 10 or 18.5.
- Any per-class or per-pass GPU split today.
- Shadow triangles at tp-worst.
- Evening and night on the Air.
- Whether cull pops are visible (arithmetic only).
- Crossfade cost on Metal.
- Whether shadow views use the camera's LOD (read and assumed, no doc).
- Whether a level file's shadows pass the limits.
- The census's pieces outside its top 30.

**Limits of my counts:**
- `placed_census.py` reads the YAML with no Unity. It doesn't resolve nested prefab overrides, and it takes catalogue tris, so it ignores each placed scale's effect on LOD.
- Old-art casting is inferred from the absence of overrides.
- Scene file dated Oct 31 07:20; the 0.2.181 row is 07:39; the working tree has moved since.

**Sources I couldn't use:**
- Ryan Brucks' octahedral impostors post: the page returned no body.
- The SIGGRAPH 2015 GPU-driven pipelines PDF (Haar, Aaltonen): the fetch returned undecoded PDF data, so I make no claim from it.
- An octahedral impostor write-up: 404.
- `docs.unity3d.com` LOD Group pages for 6000.3: 404 or redirect; I used the redirect targets where listed below.
- No GDC talk was read, so "what AAA does" rests on the engine docs below, and I don't claim specifics from talks.

## 8. Sources

**Opened:**
- Unity, Mesh LOD introduction: https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/lod/mesh-lod/mesh-lod-introduction
- Unity, How the Mesh LOD generator works: https://docs.unity.com/en-us/engine/6000.3/manual/analysis/graphics-performance-profiling/lod/mesh-lod/mesh-lod-generator
- Unity, Mesh LOD runtime quality: https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/lod/mesh-lod/mesh-lod-quality
- Unity, LOD Group transitions: https://docs.unity.com/en-us/engine/6000.3/manual/analysis/graphics-performance-profiling/lod/lod-group/lod-transitions-lod-group
- Unity, Enable the GPU Resident Drawer (6000.4, the animated-fade limit): https://docs.unity3d.com/6000.4/Documentation/Manual/urp/gpu-resident-drawer.html
- Unity, same page in 6000.3: https://docs.unity.com/en-us/engine/6000.3/manual/analysis/graphics-performance-profiling/in-urp/reduce-draw-calls-urp/reduce-rendering-work-on-cpu/gpu-resident-drawer
- Unity, GRD exclusion reasons (6000.7 beta, newer than our 6000.6.2f1): https://docs.unity.com/en-us/engine/6000.7/manual/analysis/graphics-performance-profiling/in-urp/reduce-draw-calls-urp/reduce-rendering-work-on-cpu/gpu-resident-drawer-exclusion-reasons-reference
- meshoptimizer README, first 100,000 characters: https://github.com/zeux/meshoptimizer
- Apple, Optimize Metal Performance for Apple silicon Macs, WWDC20: https://developer.apple.com/videos/play/wwdc2020/10632/

**Read here:** URP and core package source in `unity/Library/PackageCache` (`LODRenderingUtils.cs`, `InstanceCuller.cs`, `LODGroupDataSystem.Jobs.cs`, `LODCrossFade.hlsl`, `StencilCrossFadeRenderPass.cs`, `UniversalRenderPipeline.cs`, `GPUResidentDrawerDebug.cs`).

**Search extracts only, page not opened:**
- Unity, URP asset reference (Small-Mesh Screen-Percentage): https://docs.unity.com/en-us/engine/6000.6/manual/render-pipelines/universal-render-pipeline/urp-reference/universalrp-asset
- Unity forum, LOD crossfade with the drawer: https://discussions.unity.com/t/lod-crossfade-in-graphics-resident-drawer/1561993
- Epic, HLOD: https://dev.epicgames.com/documentation/en-us/unreal-engine/hierarchical-level-of-detail-in-unreal-engine
- Garland and Heckbert 1997: https://www.cs.princeton.edu/courses/archive/fall06/cos526/papers/garland97.pdf
- Hoppe, new quadric metric: https://hhoppe.com/newqem.pdf
