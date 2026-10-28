# 075 · Redline · the cost of the first 075 round (A, B, C 1–2) and 074-H; the Air sitting's 075 switches (2026-10-28)

**Inputs**
- HEAD 61698d63 (075-A, 075-C 1–2), fc1020d8 (075-B) and b1661058 (074-H). I also read Latch's uncommitted builder work for the liveries (`KitLiveries.cs` and its call sites) without changing it.
- The export `out/world/harbour-district.world.json`, sha256 ec0339f9…1155. It is the same file as Latch's pin `out/scratch/latch/075/pin/075.world.json`. It has 13,539 kit facades, 20 `hidden` tower masses and 14 livery placements.
- The 074 export `out/scratch/latch/074/pin/074.world.json` (67a3a07b…) as "before", and `contracts/world/kit.json` at HEAD.
- **Latch's counts arrived while I worked, and I used them:** `briefs/075/latch/{census,offscreen}.json` (12:45). I compare them with `briefs/074/latch/after/` (11:00, the 5-slot colourways). They are batchmode offscreen counts on the Studio (M5 Max) with the GPU Resident Drawer **off**. None of them are UnityStats or Air milliseconds.

**What is computed and what is measured**
- **Computed by me:** draws under the drawer, triangles, road and water shares, per-pixel fetches, memory and the projection.
- **Counted by Latch (offscreen):** renderers, triangles, materials, the SetPass estimate and shadow casters.
- **Measured:** nothing on the Air.

**Re-runs (read-only, 3 s each):**
- `python3 -I briefs/074/redline/colourway_draws.py out/world/harbour-district.world.json contracts/world/kit.json briefs/075/redline/colourway_draws_075.json`
- `python3 -I briefs/075/redline/tk1_tris.py out/scratch/latch/074/pin/074.world.json out/world/harbour-district.world.json contracts/world/kit.json briefs/075/redline/tk1_tris.json`
- `python3 -I briefs/075/redline/road_share.py out/world/harbour-district.world.json`

## Verdict

| part | SetPass | draws / renderers | per pixel at play | memory | at the opening (cell-open-30) |
|---|---|---|---|---|---|
| **075-A** sky, sea, bloom, grade | +0 (the sky material and the water shader are each swapped one for one) | +0 | sky **0**: the ortho play frame shows no sky pixels. Sea **0** at the opening, which has no water in frame. Bloom **0**: it is the same pass, and URP's bloom cost does not depend on threshold, intensity or scatter | **+6.0 MiB** (3 new textures; 5.3 of it is the far-city band) | **+0.00** |
| **075-B** road | **+1 forward, not +0** (Latch: 5 of 5 views, scene variants 29 → 30) | +0 | **+3 fetches on every road pixel** (the asphalt detail pair: mask, detail albedo and detail normal), on 24–34 % of the frame | ledger net 0 (true). In the player, **+1.84 M texels resident** (≈ +1.2 to 2.3 MiB) | **+0.04 to +0.21** |
| **074-H** colourways, 3 slots, 36 materials | +0 (verified) | play ≤ **+34**, tp-street **+168**, tp-worst **+180** | 0 | 0 | +0.00 (+2 draws) |
| **075-C 1** T-K1 | 0 | **−352 instances** (about −1,056 renderers), −1.83 M L0 / −351 k L1 / −325 k L2 (proxy) triangles district-wide; Handover views unchanged | 0 | mesh assets 495 → 447 | **−0.03 to 0** (fewer shadow casters, shared with the sun's new 44.7° pitch) |
| **075-C 2** liveries, 3 materials on 14 instances | +0 (keywords enforced in code, and the files diff only in base colour) | +0: every livery piece is tinted, so no draw splits | 0 | 0 | 0 |
| **Depth texture at play zoom** | | | **stays off** (section 1.4) | | |

**Against 064 A5 (frame-neutral, ≤ +0.3 ms until a warm sitting): pass (estimate).**
- The round costs **+0.01 to +0.23 ms** at the opening.
- Almost all of that is the road's detail pair, and it is the one part of the round with a real per-pixel price. At office-front, where 33 % of the frame is road, it is +0.07 to +0.29 ms.
- **Projected warm Air mean at the opening: 12.4 ms (11.0–13.9) against 13.0.** It was 12.3 (11.0–13.7). The details are in section 4.

## 1. 075-A (Glint): sky, sea, bloom, grade

### 1.1 Sky: DK_SpaceSky against Suborned/SkyGradient
- **At play zoom it costs nothing.**
  - The play camera is orthographic at 38°, so every view ray points 38° down.
  - At zoom 14 the frame covers about 50 × 46 m of ground around the focus. The horizon land and sea sectors cover everything, out to ±8 km.
  - The skybox draws after the opaques at the far plane and fails the depth test everywhere, so the GPU's hidden-surface removal skips its fragments. With no fragments shaded, it is one draw on both sides, the same as before.
  - AtmosphereLens also leaves the sky globals off in the play tier (`Sky(on && tier != Play …)`).
- **At street level (follow, ground, long) it costs more than the old sky** on every pixel where sky shows:
  - The old sky did about 50 ALU and at most one band fetch per pixel. The new one does about 130–180 ALU and 4–5 fetches: four cirrus noise samples, the far-city band, `Air()` twice, the airmass term and the planet, ring and moon code.
  - The planet, ring and moon code runs even though light.json sets their radii to 0. `RaySphere` returns −1, and the ring path divides by `pr = 0`, giving inf or NaN, so its tests fail. It is harmless, but it is wasted ALU on every sky pixel (F4).
  - With 25–45 % of a street-level frame showing sky at 1080, that is **+0.05 to +0.15 ms GPU (estimate)**. tp-worst is already over budget (brief 048's view), and this adds to it.
- **Textures, all newly resident**, because the scene's skybox material references them in every view, including play:

| texture | size | format (Standalone default; nothing overridden) | GPU with mips |
|---|---|---|---|
| dk_sky_noise | 512² RGBA | BC7 | 0.33 MiB |
| dk_skyline_far | 8192 × 512 RGBA | BC7 | **5.33 MiB** |
| dk_sea_normal | 512² RGB | BC7 | 0.33 MiB |

  - Total **+6.0 MiB**. The old sky and water had no textures, and `Atmosphere/farband` is still absent from Resources.
  - The far-city band is sampled only by the perspective tiers.
  - The Standalone row in the meta reads `maxTextureSize 2048`, but `overridden: 0`, so the default 8192 ships.
  - These textures sit in `Resources/`, so they ship even if nothing references them. The builder loads them through AssetDatabase, so `Resources/` isn't needed (cosmetic).

### 1.2 Sea: DK Ocean against Suborned/Water
- **Per water pixel:**
  - two wave-sheet fetches (512², BC7, cache-resident);
  - `SkyAir()` (about 4 pow and 1 exp), a glint pow, per-pixel fog and SH;
  - about +60 ALU and +2 fetches over the old Water, which had no textures.
- **Where water is in frame:**
  - **None at the opening** (cell-open, office-front: 0 % water).
  - **28–29 % at harbour-quay** (ground footprint, computed). That is about 0.6 M pixels at 1080, so **+0.02 to +0.1 ms**.
  - The sea is sector rings, not a quad under the land: SkylineKit throws if the export has no sea sectors, because "the square sea plane is retired". So there is no hidden overdraw under the district. Even if there were, the base DK cutaway variants have no discard, so hidden-surface removal would remove it.
- **Variants:**
  - DK Ocean adds `multi_compile_fog`, so the shader has 4 × 4 × 5 × 2 = 160 variants against Water's 40. That costs build time and size only, unless fog-mode stripping drops them.
  - One shader replaces the other, so SetPass is +0 wherever water draws.
  - Neither shader has a DOTS-instancing path, so the sea stays off the drawer, as before.

### 1.3 Bloom and grade
- **The play-only bloom** (`bloomPlay` 1.05 / 0.45 / 0.75):
  - The play camera already ran bloom: the scene grade's bloom had intensity 0.22, now 0.6 at 0.48.
  - The play volume now overrides its parameters only. URP 17's bloom passes and their sizes depend on resolution, downscale (half, default) and max iterations. None of those moved, and threshold, intensity and scatter are shader constants.
  - **Δ 0 by construction.** The follow and long tiers moving to the old bloom (0.48 / 0.6 / 0.85) is also Δ 0.
- **The rest is free:**
  - post exposure (`gradePlay.exposureDelta` +0.083, scene −0.063), contrast and saturation are folded into the 32-point LUT;
  - the Trilight ambient, sun 1.8 at 44.7° and shadow strength 1.0 are light constants;
  - the long-tier fog moving from 0.0006 to 0.0005 changes a fog constant.
- **One side effect worth knowing:** the sun pitch moving from 38° to 44.7° shortens shadows, so fewer far casters reach the two cascades. Part of Latch's caster drop (section 3) is the pitch, not T-K1.

### 1.4 Depth texture at play zoom: still off
- `PC_RPAsset`: `m_RequireDepthTexture: 0` and `m_RequireOpaqueTexture: 0`.
- 075-A adds no renderer feature and no camera request:
  - bloom, colour adjustments and the LUT never ask for depth;
  - depth of field stays on the long tier only;
  - the flare is still disabled wherever it doesn't draw (AtmosphereLens, A3 item 5);
  - neither ported shader samples `_CameraDepthTexture`;
  - DK Ocean's DepthNormals pass is used only by SSAO, which TacticalShadows turns off for the ortho camera (A3 item 1, now in code).
- The SignalsFeature pass (A3 item 7) is in.
- **Nothing in this round brings depth back.**

## 2. 075-B (Vela): road sheet at 16 m with the asphalt maps

**Vela's "+0 MiB" holds for her budget ledger. Her "+0 SetPass" does not hold in the scene, and the road is not +0 per pixel.**
- **Keywords:**
  - `DK_Ground_RoadEnamel` used to draw on DK Lit Cutaway with `_METALLICSPECGLOSSMAP _NORMALMAP _OCCLUSIONMAP`, the same set as RoadPearl (kerbs) and the module variant.
  - It now takes `DK_Ground_Asphalt`'s set, which adds `_DETAIL_MULX2`.
  - That set is "no new variant" in the kit. In the scene it is new, because **no export surface draws `DK_Ground_Asphalt`** (0 `asphalt` surfaces in ec0339f9; the only adopted kit piece using it is `Ground_Swatches`).
  - Latch's census agrees: scene shader variants 29 → 30, and **forward SetPass +1 at all 5 views**. Depth-normals and shadow are unchanged; the shadow variant keys on alpha test only.
- **Per pixel:**
  - The detail pair sits behind 072-F's global branch (`DK_SurfaceDetail.hlsl`), so on road pixels it adds the detail mask, detail albedo and detail normal fetches plus the TBN blend.
  - Road share of the ground footprint (computed, an upper bound with no building or car occlusion): **cell-open 24–25 %, office-front 33–34 %, café square 25–26 %, heaviest 29 %, wide-lamplight 28–30 %, parade 0 %, crown-circus 5–6 %**.
  - That is +0.55 to 0.72 fetches per pixel, area-weighted, at the opening (after 18–24 % occlusion), and up to +1.0 at office-front.
  - At 072-F's rate (+0.84 fetches per pixel cost +0.06 to +0.24 ms), the road costs **+0.04 to +0.21 ms at the opening and +0.07 to +0.29 at office-front (estimate)**.
  - Added to 072-F's +0.84, the area-weighted fetches would be +1.4 to 1.6, past 072-F's own +1.0 line. That line was 072-F's, though; the road is 075's own spend.
- **Memory in the player** (computed; to confirm with a Memory Profiler snapshot of the development player):
  - The four shared maps are `ground_asphalt_v1_normal` 512², `_mask` 512², `ground_asphalt_detail_v1` 512² and `_detail_v1_normal` 1024², 1.84 M texels in all.
  - Only DK_Ground_Asphalt referenced them, and nothing drew it, so they weren't resident before. Now they are.
  - The enamel's own three maps (2.36 M texels) leave, and the 1536² albedo (2.36 M) arrives. **Net +1.84 M texels ≈ +1.2 to 2.3 MiB** (0.5–1 byte per texel by format).
  - The ledger's "net 0" counts the asphalt maps as already loaded, because they are in the ground-set budget.
- **Import check (low):** `ground_roadenamel_v1.png` is 1536² non-power-of-two, with `nPOTScale: 1` (ToNearest) and `maxTextureSize: 1536`, which is not one of Unity's steps. The lawn has the same pattern. Latch should read the imported size in the Inspector. If it imports at 2048, the road albedo is 5.3 MiB, not 3.0.
- **The cheap fix, and truer to the port:**
  - Vela's own table says the old DK_Asphalt_Road had "normal strength 1, no detail maps".
  - Dropping `detail` and `detailNormal` from `DK_Ground_RoadEnamel` in `road_ribbon.py` (one line) puts the road back in RoadPearl's keyword set. That gives SetPass +0, three fewer fetches per road pixel and −1.3 M resident texels.
  - Whether the 1.5 m aggregate reads at play zoom is Vela's and Sky's look call; I propose it, I don't decide it.

## 3. 075-C 1–2 and 074-H

### 3.1 T-K1: stand-in tower blocks hidden, 4,288 panels gone
- **Export (computed):** 20 tower masses are `hidden`. The facades removed are SideBayStack_Mid 3,148, CornerPier_Out_Mid 984, SideBayStack_Top 116 and CornerPier_Out_Top 40, which is **352 roots**.
  - District triangles: L0 −1,829,040, L1 −351,424, L2 (the shadow proxies) −325,264.
  - Latch's census: columns placed 2,265 → 1,913, layout prefabs 165 → 149, meshes 495 → 447, triangles of placed meshes 573,512 → 414,604, shadow proxies 6,458 → 6,106.
- **The builder honours it:** `FacadeKit.Buildings` (Latch, uncommitted) throws if a `hidden` building mass has no stand-in, so a hidden box can't silently draw.
- **Per view:**
  - **Handover views unchanged.** The nearest removed module is 228–292 m from the office front, cell-open and wide-lamplight foci.
  - Latch's offscreen counts agree: renderers and main triangles are identical at all 5 views (230 / 213 / 292 / 273 / 244).
  - Where it shows: parade@300 loses 23 of 31 roots (95 k → 9 k L0 triangles), crown-circus@120 140 k → 41 k, parade@210 41 k → 32 k, tp-street −36 roots (209 k → 188 k triangles). tp-worst is unchanged.
- **Shadows:** Latch's casters fell at four views: office-front@30 168 → 150 (−40 k triangles), cell-open@210 330 → 315 (−48 k), wide-lamplight 343 → 323 (−26 k).
  - Tall towers cast into the cascades from outside the frame, and the higher sun shortens shadows (section 1.3), so the two causes share this.
  - **≤ −0.03 ms.** The drawer culls about 6 % fewer instances, so the main thread saves about −0.01 to −0.03 ms everywhere. The real saving is in the perspective and long views (the flyby).

### 3.2 Liveries (3 materials on 14 instances)
- **Placed:** SkyPad_Roof ×5, SkyRailStation_Twin ×2, and SphereStation, Skylon, LinerDock@liner-berth, SpaceHQ_Portal, DiscTower@avenue-gate-w/e and Skyport_Tower_Portal ×1 each.
- **No splits:** I walked every kit reference in the export and found **no untinted instance of any of these pieces**. The swap replaces a draw; it never splits one, so **draws +0**.
- **Materials on disk:** `DK_Steel_Ivory__vermilion` and `DK_Hull_Ivory__brick` differ from their bases only in `m_Name` and `_BaseColor`. `DK_Crown_Gold__gold` also differs in `_Color`. Same shader GUID and keywords.
- **In the builder (uncommitted):** `KitLiveries.Load` rejects a different shader or keyword set. It writes `sharedMaterials` with per-renderer prefab overrides and makes no instance. `KitColourways.AddBase` registers the tints, so shadow proxies map them back to base. **SetPass +0, textures 0.**
- Latch's scene material count, 349 → 328, is −24 colourway tints plus 3 liveries.

### 3.3 074-H: colourways on walls and plinths only (3 slots, 36 materials), re-run against my marks

| mark | 074 (5 slots) | **074-H (075 export)** | verdict |
|---|---|---|---|
| SetPass | +0 | **+0** (module variants: the same 6, Latch's census) | pass |
| play zoom 14, batches ≤ +40 | worst +70 (wide-lamplight@30) | worst **+34** (wide-lamplight@30). Handover views: office-front +0 / +4 / +0 / +0, cell-open +2 / +5 / +6 / +0, heaviest +8 to +12 | **pass** |
| street level, module draws ≤ +260 | tp-street +320, tp-worst +252 | tp-street **+152** (+168 with street pieces), tp-worst **+114** (+180) | **pass** |
| district materials | 26 → 81 | 26 → 59 | |

- These match my 074 what-if for "wall ×2 + plinth" to within one draw. The difference is the T-K1 roots: tp-street 153 → 152.
- Wide 18.5 at wide-lamplight@30: +55 (no mark).
- **Render thread at tp-worst:** about 4.6 ms after every face (072), plus +180 draws at 1.5–3 µs each, gives **4.9–5.1 ms**. With 5 slots it was 5.1–5.7. That is at the 5 ms line, 072's trigger for the 32 m cell HLOD; only the sitting can say which side it falls.
- At the opening, +2 draws is nothing.
- 074 F1 is closed. 074 F2–F5 still apply and now cover the liveries too: F3, `Base` depending on static state, extends to `KitLiveries`.

## 4. Projected warm Air mean at the opening (cell-open-30), 1920 × 1080, estimate

| step | Δ (ms) | warm mean |
|---|---:|---|
| 072 phase 3 with 064 A3 items 1, 5, 7 (all three now in code: TacticalShadows' ortho SSAO off, RequireDepthTexture 0, SignalsFeature) | | 11.0–13.7 (12.3) |
| 074-H colourways (+2 kit draws) | 0 | |
| 075-A: sky (no pixels), sea (none in frame), bloom (same pass), LUT, light constants | 0 to +0.01 | |
| 075-B: road detail pair, +3 fetches on 18–24 % of the frame; +1 forward SetPass (CPU, about 5 µs) | +0.04 to +0.21 | |
| 075-C: T-K1 (casters −0 to −48 k triangles, 6 % fewer instances culled); liveries | −0.03 to 0 | |
| **075 round** | **+0.01 to +0.23** | **11.0–13.9 (12.4)** |

- **Against 13.0:** the central 12.4 passes with about 0.6 ms in hand. The worst corner is 0.9 over; it was 0.7.
- **p99:** 12.4 × 1.20–1.41 gives 14.9–17.5 against 16.7. Still borderline, as before.
- **Allowance:** unchanged: 13.0 minus the measured warm mean, capped at 1.5, and the fight, night and p99 draw on it.
- **This round's share:** the road detail pair takes up to 0.21 of A5's 0.3. Dropping it (section 2) brings the round to +0.01 at the opening.
- **Ground (tp-worst):** 14.1–15.7 plus every face, plus about +0.1 to +0.4 from 074-H, the sky and the road detail, less T-K1's −0.01. Still over 13.0. It remains brief 048's view.
- **Latch's offscreen frame milliseconds rose in all 5 views** (medians +0.84, +1.33, +1.09, +1.07, +2.21; p90 roughly doubled). The content I can find accounts for at most +0.2 at play.
  - It is batchmode with the drawer off, and Latch marks it indicative only. 072 phase 3 saw identical content read up to 2.9 ms apart.
  - But 5 of 5 views moving the same way is a pattern, not proof of noise.
  - **Before anything is read into it**, Latch should run an **interleaved** Studio check in one session, in memory and unsaved: as built against (skybox = the gradient material, water = Suborned/Water, RoadEnamel = a copy without `_DETAIL_MULX2`), N frames each, alternating, at cell-open-30 and office-front-30.
  - If the gap survives interleaving, something I haven't found costs about 1 ms, and that blocks the sitting.

## 5. The Air sitting: the 075 switches

The sitting stays unscheduled (072 phase 3 §4 precondition, plus the 075 round in the same player).

**Switches the harness has today** (`PerfVariant`: ssao, proxies, fade, everyFace, moduleShadows, surfaceDetail, cascades, shadowsOff, fadeOff; `TpLongRun.RowVariant`: everyFace, moduleShadows, surfaceDetail). Of these, only **surfaceDetail** touches 075:
- it now switches the road's detail pair too, so `every − every-flat` includes 075-B's fetches;
- it does not include the +1 SetPass, which both sides carry;
- the `-atmosphere off` player flag exists, but it is a whole-run flag. It removes the lens and haze layer, not the ported sky or sea (they are baked into the scene), and the scene's own bloom stays on. It is not a 075 A/B, and it is not used.

**The 075 toggles** (spec: `briefs/075/redline/perf-075-pending.json`, which the harness does not read):

| toggle | field and values | exists? | expected | where it is measured | priority |
|---|---|---|---|---|---|
| old sky on/off | `sky: old \| gradient` | **pending (Latch)** | play 0; street +0.05 to +0.15 | tp-street, tp-worst | medium |
| ocean on/off | `ocean: dk \| flat` | **pending (Latch)** | opening 0; harbour-quay +0.02 to +0.1 | harbour-quay-30 | medium |
| play bloom | `playBloom: lens \| off` | **pending (Latch), optional** | lens against the scene's bloom is 0 by construction, so not worth a field. Lens against off prices the bloom pass (about 0.2 to 0.5 at 1080), which A3's look item 6 wants | cell-open-30 | low |
| colourways on/off | `colourways: family \| base` | **pending (Latch)**; proposed in 074 §4 and never added | opening +0; wide-lamplight +34 draws; tp +168 / +180 draws, render thread +0.25 to 0.55 | wide-lamplight-30, tp-street, tp-worst | **high** |
| liveries on/off | `liveries: on \| off` | not recommended | 0 by construction (section 3.2); check with counts, not a sitting | | none |
| road detail | `surfaceDetail: on \| off` | **exists** (79283d9f); now includes the road | the road's share of `every − every-flat` | the existing stops plus wide-lamplight-30 | (in place) |
| T-K1 | none | no switch (the boxes can't come back without a rebuild); none needed | absolute number at parade-210 and crown-circus-120, already stops | | |

**Before the sitting, Latch must add** `colourways`, `sky` and `ocean`, and optionally `playBloom`. For each:
- add it to both PerfVariant and RowVariant, to `Describe()` and to `PerfSwitches.Apply` / `Restore`, and make the player echo it in its result;
- make it dev-build only, applied once before warm-up, off (as built) by default, so every ordinary run replays byte-identical;
- **colourways must swap before FadeReach is constructed** (074 F2);
- **sky must swap before AtmosphereLens caches the scene sky** (`SceneSky` reads it once).

**Files updated** (specs only; nothing run):
- **`briefs/072/redline/perf-072.json`:**
  - the note and content now carry the 075 precondition and the list of pending switches;
  - **two stops added, using existing fields only:** `wide-lamplight-30` (the worst colourway play view, about 30 % road) and `harbour-quay-30` (28–29 % water);
  - ab072 runs at 8 stops (about +5 min).
  - **No 075 variant fields are written**, because JsonUtility would drop them.
- **`briefs/072/redline/tplong-072.json`:** the note only. The rows are unchanged; the pending rows (tp-street and tp-worst × colourways base and sky gradient) are in the pending file.
- **`briefs/072/redline/run-air-072.sh`:**
  - the header carries the 075 precondition, and the stop count is 8;
  - **a new guard:** if the resolved plan or tplong names any of `sky`, `ocean`, `colourways`, `playBloom` or `liveries`, the player's result must echo it, or the run stops as void. It does nothing until Latch lands a field.
  - `zsh -n` is clean, and the guard logic was tested on fixture files (none → skip; named but not echoed → STOP).
- **`briefs/075/redline/perf-075-pending.json`:** the fields, how each must be built, the variants, an `ab075` pass (cell-open-30, wide-lamplight-30, harbour-quay-30 × every, cw-base, ocean-flat and the optional bloom-off; about 9–12 min), the tplong rows, and the verdict rules. Merge it into the 072 kit once the fields exist.

## 6. Findings

- **F1 · Medium (075-B, perf and claim):** the road picked up the asphalt detail pair.
  - Cost: **+1 forward SetPass in every road view** (Latch's census confirms it) and +3 fetches per road pixel, which is +0.04 to +0.21 ms at the opening and up to +0.29 at office-front.
  - That contradicts "+0 SetPass, no new variant".
  - Within A5, but it is most of the round's cost.
  - Fix: drop the detail pair (one line, truer to the old road). Next owner: Vela, via Aster, with Sky's look call.
- **F2 · Low (075-B, memory):** the shared asphalt maps weren't resident before, so the player gains about +1.84 M texels (+1.2 to 2.3 MiB) while the ledger says net 0. Separately, check the imported size of the 1536² NPOT albedo (Latch, Inspector).
- **F3 · Low (075-A, memory):** +6.0 MiB of new textures, 5.3 of it the 8192 × 512 far-city band, which the play view never samples.
  - Halving it needs Sky's sign-off (no downscaling, §01) and risks Glint's banding.
  - Noted, not proposed.
- **F4 · Low (075-A, street level only):** DK_SpaceSky evaluates the planet, ring and moons for bodies with radius 0 on every sky pixel.
  - A uniform skip would save about 30–40 % of its ALU.
  - It is a verbatim port, so this is Glint's call, and only if the tp rows show the sky.
- **F5 · Watch (Latch's offscreen milliseconds):** +0.8 to +2.2 ms in 5 of 5 views, against at most +0.2 from content. The interleaved check in section 4 is needed before the sitting.
- **Note for Splice and Vela (data, not perf):** the export's `liveries.colours.gold` says metallic 1 and smoothness 0.45, but `DK_Crown_Gold__gold.mat` keeps 0.7 / 1 (Vela's no-new-anything choice). Nothing applies the export's finish. Either drop it from the export or record that it is unused.

## Open issues
- **F1:** the road detail call (Vela, Sky, via Aster).
- **The pending switches:** Latch adds `colourways`, `sky`, `ocean` (and optionally `playBloom`); then I merge `perf-075-pending.json` into the 072 kit.
- **Latch's interleaved offscreen check (F5),** and the imported size of the 1536² road albedo (F2).
- **Still owed from 072:** UnityStats in the Game view and a Memory Profiler snapshot of the development player (texture Δ, now including 075's +6.0 MiB and the road maps).
- **T0** is still the biggest error bar (±0.75 ms). Only the sitting closes it.

**Next owner:** Aster.
- **Inputs:** this file, `briefs/075/redline/*`, and Latch's `briefs/075/latch/*`.
- **Done-check:**
  1. Sky's call on F1 is recorded in brief 075.
  2. Latch lands the switches. `PerfPlan.cs` / `TpLongRun.cs` carry the fields, and a test player echoes them.
  3. Latch's interleaved Studio check is in `briefs/075/latch/`.
  4. I merge the pending spec and re-run the three scripts (3 s each) on the next export.
