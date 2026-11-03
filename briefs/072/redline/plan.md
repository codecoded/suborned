# 072-D · Redline · DecoKit on every face: what it costs and how it fits (phase 1)

2026-10-27. Phase 1 is reading and arithmetic only: no Unity, no player, no Air. Every number is labelled:
- **computed:** from the world export and the catalogue by `every_face.py` (this folder);
- **assumed:** a piece's triangle count, taken from a similar piece until Vela's 072-A table replaces it;
- **estimate:** ms from brief 064's models;
- **Studio editor:** Latch's baseline capture (`briefs/072/latch/baseline/`, GRD off, indicative only).

Inputs:
- `out/world/harbour-district.world.json` (sha256 d6f7560d…, the same export Latch's baseline was built from);
- `contracts/world/kit.json` (1dba2e9b…);
- the builder (`unity/Assets/Suborned/Editor/World/`: KitLods, KitPlacer, KitCover, Combine, ShadowProxies, FadeTags);
- runtime (TacticalShadows, ArchitectureFade, HostCameraLab, HostFollow, PerfHarness/PerfSwitches);
- PC_RPAsset and QualitySettings;
- the old DecoKit game (`~/Projects/the-suborned/game`).

To re-run (about 16 s): `python3 -I briefs/072/redline/every_face.py out/world/harbour-district.world.json contracts/world/kit.json out/scratch/redline/every_face.json`

## 0. In short
1. **At play zoom, LODs do nothing. The column is what limits the cost.** The play camera is orthographic. In ortho, Unity's LOD level is `size / (2 · orthographicSize)`, the same everywhere on screen and independent of distance. Every module therefore draws at L0 at every play zoom (12.5–18.5). What the camera sees is a fixed 49.8 × 28 m frame column, so the cost per view is the modules inside it. That is small (computed): **1–39 new modules and +11–53 k view triangles at zoom 14 in the cell's opening and Office views.** The worst of 11 foci × 4 yaws is +158 k at zoom 14 and +281 k at zoom 18.5.
2. **The real risk at play zoom is shadows, not the view.** URP's casters for the play camera's two cascades take in 100–1,500 modules per view (computed, an upper-bound model). Casting at L0 adds **+0.2–1.9 M shadow triangles**, against today's 0.85–1.45 M total at the same views (Studio editor). Casting from an L2 proxy adds **+12–109 k**. Policy: modules never cast; an L2 shadow proxy casts for them, with a shadows-only mass shell behind.
3. **No static combine and no HLOD in step 1.** Under the GPU Resident Drawer (on in PC_RPAsset; static batching is off for the project), draws scale with distinct (piece, LOD, material, colourway), not with modules: **+11–84 instanced draws per play view** (computed). Combining would cost three things:
   - instancing;
   - ~0.5–1 GB of unique mesh;
   - the cut's fine reach: the fade's discarding variant is per renderer, so a combined chunk would discard over its whole area.

   The one bake worth doing is **stack columns**: storey modules of one bay merged into one instanceable mesh per storey count. That takes 16,164 renderers to 5,516 (computed).
4. **Neither the procedural facade nor an impostor is needed as a far LOD.** Only the ground view sees far, and L2 modules there are 0.10–0.21 M triangles (computed). A 32 m cell HLOD is specified as step 2, used only if the A/B shows the module renderers cost the ground view.
5. **The LOD thresholds must be per piece and set by distance.** The catalogue's fixed `facades` switch (0.2 / 0.07 of screen height) would put the 6 m storey modules into **L1 at play zoom above ~15** (computed: up to 246 modules below L0 in one view at zoom 18.5). Rule: a switch distance of at least 36 m keeps every piece at L0 at every play zoom (zoomMax 18.5 / tan 27.5° = 35.5 m).
6. **Projected warm Air mean at the cell's opening, 1920 × 1080: about 12.5 ms (range 11.0–14.0) with every face plus 064 A3's no-look items, against 13.0.** Every face alone is +0.12–0.68 ms (estimate). It fits if A3's SSAO-off and the other no-look items land. It fits with margin if Glint takes A3's look items too (central 11.7). The ground view stays over the bar either way (14.4–16.5 estimate); that is brief 048's problem, not this one's.

## 1. What the camera sees (computed)

### 1.1 The census: every face of every building
- Faces: every edge of the 716 building masses, tiled in 6 m bays on a 1 × 1 m grid.
- Excluded: wall inside another mass (party walls, 404,668 m²) and wall under the 935 existing kit facades (176,131 m²).
- **Exposed wall left to dress: 388,830 m², 2.2× what the kit covers today.**
- Piece rules:
  - SideBay;
  - AlleyWall when another mass stands within 12 m of the face (a stand-in for Splice's service rule);
  - _3 up to 17.5 m, _4 up to 21.5 m, storey stacks above;
  - corner piers where two dressed faces meet.

| family | modules | L0 / L1 / L2 tris placed (M, assumed) |
|---|---:|---|
| SideBay_3 / _4 | 1,264 | 5.68 / 1.39 / 0.33 |
| SideBayStack Base / Mid / Top | 7,765 | 9.29 / 2.27 / 0.54 |
| AlleyWall_3 / _4 | 511 | 1.59 / 0.39 / 0.09 |
| AlleyWall stack Mid / Top | 4,815 | 4.65 / 1.14 / 0.28 |
| CornerPier Out / In (+ Mid) | 1,808 | 1.40 / 0.34 / 0.08 |
| RecordsTower | 1 | 0.03 / 0.01 / 0.00 |
| **total** | **16,164 (5,516 bay columns)** | **22.6 / 5.5 / 1.3** |

- Vela's estimate was ~4,200 modules and 17.6 M; these 5,516 columns and 22.6 M are the same order. Tris per piece are assumed:
  - SideBay from PortholeBay_3L/_4L (4,322 / 5,478 at L0);
  - storeys from PortholeBay_7 per storey;
  - AlleyWall from AlleyWall_6;
  - corners and RecordsTower are guesses.

  The full table is in `every_face.json` → `cost_table_assumed`. Vela's real table replaces it, and the script re-runs.

### 1.2 Per play view (orthographic, pitch 38, 1920 × 1080; zoom 14, and 18.5 as zoomMax)
- **Visibility:** world AABB against the four frustum side planes, as Unity culls.
- **Casters:** URP's two TacticalShadows cascades. Air [0, s−60] and street [s−60, s+40], with s = 160 m. Each cascade's culling sphere is swept toward the sun (pitch 38, yaw 330). This is an upper-bound model; Latch's frame-debugger counts replace it.
- **Facing:** the modules whose front faces the camera at that yaw. This is the step-2 facing cull (section 3.6).
- **Today:** Latch's baseline at the same views (Studio editor, GRD off), for scale: 0.85–1.45 M tris, 55–69 SetPass, 268–913 shadow casters. Procedural walls in view (`batches/buildings` + `batches/skins`) are only ~13–19 k tris, so the delta below is close to net.

| view | zoom | new modules (columns) | facing cam | view tris L0 | instanced draws (pieces) | casters | caster tris L0 / L1 proxy / L2 proxy |
|---|---:|---|---:|---:|---|---:|---|
| cell-open@30 | 14 | 6 (6) | 2 | 28 k | 32 (3) | 287 | 653 k / 160 k / 38 k |
| cell-open@120 | 14 | 21 (20) | 7 | 53 k | 58 (6) | 264 | 683 k / 167 k / 40 k |
| cell-open@210 | 14 | 9 (9) | 5 | 47 k | 17 (2) | 175 | 591 k / 145 k / 35 k |
| cell-open@300 | 14 | 2 (2) | 2 | 11 k | 11 (1) | 153 | 454 k / 111 k / 27 k |
| office-front@30 | 14 | 2 (2) | 1 | 35 k* | 27 (2) | 212 | 483 k / 118 k / 28 k |
| office-front@120 | 14 | 5 (5) | 1 | 50 k* | 38 (3) | 279 | 553 k / 135 k / 32 k |
| office-front@210 | 14 | 2 (2) | 1 | 31 k* | 27 (2) | 103 | 376 k / 92 k / 22 k |
| office-front@300 | 14 | 1 (1) | 1 | 30 k* | 16 (1) | 57 | 200 k / 49 k / 12 k |
| cell-open@120 | 18.5 | 39 (31) | 13 | 111 k | 84 (8) | 333 | 828 k / 202 k / 48 k |
| heaviest@210 | 14 | 20 (20) | 12 | 91 k | 36 (4) | 225 | 568 k / 139 k / 33 k |
| parade@210 | 14 | 95 (43) | 14 | 100 k | 58 (6) | 1,361 | 1,680 k / 411 k / 99 k |
| parade@300 | 14 | 155 (26) | 90 | 158 k | 48 (5) | 869 | 1,123 k / 275 k / 66 k |
| crown-circus@120 | 14 | 160 (24) | 93 | 155 k | 52 (6) | 878 | 1,041 k / 254 k / 61 k |
| parade@210 | 18.5 | 255 (88) | 87 | 281 k | 75 (8) | 1,494 | 1,861 k / 455 k / 109 k |

\* includes RecordsTower (30 k assumed).

- All 11 foci × 4 yaws × 2 zooms are in `every_face.json` → `play`. The table shows the cell's opening and Office views, plus the worst found.
- North-edge and harbour-quay get 0–4 new modules: there is no wall there.

### 1.3 Ground view (the follow camera, perspective 55°, far 350 m; the tplong rows; computed)
- Positions: the Scout's start (block (−1.6, −44.3) → plan (−174.3, −228.4)) plus the row's walk.
- Camera: 5.5 m back, 3.06 m up.

| row | modules in frustum | tris if all L0 | tris, today's catalogue LODs | tris, proposed LODs | levels, proposed (L0 / L1 / L2 / culled) | module renderers ≥ 150 m | 32 m cells ≥ 150 m |
|---|---:|---:|---:|---:|---|---:|---:|
| tp-street | 2,619 | 3.73 M | 0.33 M | 0.21 M | 1 / 91 / 1,878 / 649 | 2,431 | 56 |
| tp-worst | 1,921 | 2.77 M | 0.24 M | 0.11 M | 0 / 44 / 796 / 1,081 | 1,854 | 85 |

- The project has no occlusion culling, so everything in the frustum to 350 m draws.
- Today's tp-worst is 5.14 M tris and 1,894 renderers drawn (Air 0.2.122). Every face adds little in triangles but ~840–1,970 renderers.

## 2. How the old DecoKit city held it (read from ~/Projects/the-suborned/game)
- **Per-piece LODGroup:** L0 to 10 % of the screen, L1 to 1.2 %, then culled (`DecoKitModels.AddLods`). PC quality had `lodBias 2`, shadow distance 90, 4 cascades.
- **Static flags and batching:** BatchingStatic | OccluderStatic | OccludeeStatic on walkable kit; static batching on in the player.
- **Umbra occlusion baked** over the walkable district (`DecoKitOcclusion`, smallest occluder 5 m). In its perspective FPS walk, the street canyon's facades culled what stood behind them. That is why ~2,100 facade bays played fine on the Air.
- **Merging:** ground tiles merged per 64 m chunk and material (`DecoKitTileMerge`); no facade merging.
- **None of this transfers to the play view:** it is orthographic (no distance LOD) and looks down over the roofs (occlusion culls little).
  - What transfers to the ground view is per-piece LOD with a cull.
  - Umbra is unproven with the GPU Resident Drawer, and our street has no walls on the near side. It is listed as a step-2 option, not a step-1 fix.

## 3. The plan

### 3.1 LOD switches per piece class, set by distance (owner: Splice in kit.json, Latch in KitLods)
The builder converts each distance D into the LODGroup's screen-relative height from the piece's own unscaled size s: `screen = s / (2 · D · tan 27.5°) = s / (1.041 · D)`, at the follow view's 55° FOV and lodBias 1.

| class | pieces | s (m) | L1 at 45 m | L2 at 120 m | cull at 330 m | ortho at zoom 18.5 (must exceed L1) |
|---|---|---:|---:|---:|---:|---:|
| bay 3 | SideBay_3, AlleyWall_3(D), CornerPier_*_3 | 16.3 | 0.348 | 0.130 | 0.047 | 0.44 |
| bay 4 | SideBay_4, AlleyWall_4(D), CornerPier_*_4 | 19.7 | 0.420 | 0.158 | 0.057 | 0.53 |
| storey (if not baked into columns) | SideBayStack_*, AlleyWall stack | 6.0 | 0.128 | 0.048 | 0.017 | 0.162 |
| column (baked stack, height H) | column prefabs | H | H/46.8 | H/124.9 | H/343.5 | H/37 |
| corner storey | CornerPier_*_Mid | 3.4 | 0.073 | 0.027 | 0.0099 | 0.092 |
| landmark | RecordsTower | ~60 | at 150 m: 0.38 | at 400 m: 0.144 | none (the clip) | 1.6 |

- **Rule (Latch, a build check):** every L1 switch distance is at least 36 m. Ortho relative height equals perspective at D = zoom / tan 27.5° = 35.5 m for zoomMax 18.5, so a piece switched by distance ≥ 36 m is L0 at every play zoom, whatever its size. The build fails otherwise.
- **Why the catalogue's fixed `facades` category (0.2 / 0.07) must not be used for the new pieces:** at zoom 18.5 it puts storey modules (s = 6, ortho 0.162 < 0.2) at L1. That is up to 246 modules in one view (computed, `below_l0_today` in every_face.json). The existing kit facades (s ≥ 15.6 with their 14 m body) are safe (ortho ≥ 0.42).
- **Distances are a starting value [S]:** 45 / 120 / 330 m. Vela and Glint judge L1 at 45 m in the ground view; L0 detail is unchanged (GDD §12). Instance scale 0.85–1.15 shifts the switch ±15 %, which is acceptable.
- **The KitLods cull stays as it is for small props.** For modules the last level is the 330 m cull, inside the 350 m haze clip.

### 3.2 Combining and HLOD (owner: Latch)
- **No static combine of modules, no StaticBatching, no MaterialPropertyBlocks on modules.** Keep prefab instances so the GPU Resident Drawer instances them. Reasons:
  - **Draws:** computed +11–84 per play view, and ≤ ~345 in the ground view, all instanced.
  - **Memory:** 22.6 M L0 tris as unique meshes would be roughly 0.5–1 GB of vertex data (15–22 M vertices at 32–48 bytes); instanced, it is a few dozen meshes.
  - **The cut (GDD §10):** ArchitectureFade puts its discarding variant only on the renderers its reach touches (FadeReach). A 6 m module is finer than today's 16 m fade cells. A combined chunk would discard, and lose the Apple GPU's hidden-surface removal, over its whole extent (008 B6: discard halved the heaviest stop).
  - **Ortho culling:** a chunk sticks out of the 50 × 28 m column, so its off-screen vertices are drawn too.
- **Bake stack columns (combine group = column).** At build time the storeys of one bay column (Base + Mid × k + Top) become one mesh per (family, storey count, colourway, LOD), one prefab per variant. That is still instanced and still one renderer per bay for the cut. Renderers go 16,164 → 5,516 (computed), along with LODGroups and GameObjects (about 65 k → 22 k with LOD children and proxies).
- **Step 2, only if the A/B demands it (trigger in 4.3): a 32 m cell HLOD for the ground view.**
  - Per 32 m plan cell, one mesh per material from the modules' L2, shown by a LODGroup whose LOD0 is empty and whose LOD1 is the HLOD.
  - Gapless rule: module cull distance = HLOD switch distance + the cell half-diagonal (22.6 m). A module is then never gone before its cell's HLOD is drawn. In the overlap band both draw the same L2 surface in the same material, which is invisible.
  - At play zoom the cell's group is always in its empty LOD0 (s ≥ 32 m, ortho ≥ 0.86), so the play view never sees it.
  - Computed: it would replace 1,854–2,431 module renderers ≥ 150 m with 56–85 cells × ≤ 10 materials.

### 3.3 Instancing and SRP batcher keys (owners: Splice for the fields, Vela for the materials, Latch for the builder)
- **Keep these settings:** GPU Resident Drawer on (PC_RPAsset `m_GPUResidentDrawerMode: 1`), SRP batcher on, static and dynamic batching off.
- **Instancing key** = (piece or column variant id, LOD level, material slot set). KitSlots' per-placement colourway remap makes each slot set a separate key.
  - Cap: ≤ 4 colourways per piece in a district area. Otherwise draws multiply: draws ≈ Σ visible pieces × materials × colourways.
  - The style loosening (072-B) must respect this cap, or say what it costs.
- **Shaders:** the module materials use DK_LitCutaway, which already has DOTS instancing and the rendering-layer instancing option, so it cuts and instances. Glass, where kept, uses DK_GlassSky, DOTS-instanced since 0.2.80.
- **Materials:** ≤ 11 per piece, one shared set across SideBay, stacks and AlleyWall. Brief 072-A's ~10 for AlleyWall fits.

### 3.4 Shadow casters (owners: Latch for the builder; Glint for the look call)
- **Modules:** `ShadowCastingMode.Off` on every LOD. Each module (or column) gets one shadows-only proxy carrying its **L2** mesh. ShadowProxies is extended: module classes use LOD2; kit facades and trees keep LOD1.
  - Computed caster tris at play: L0 0.2–1.9 M → L2 12–109 k.
  - In ortho, a module's LOD is always L0, so this is the only lever on its shadow.
- **Mass shell:** KitCover stops deleting the procedural walls under modules. It keeps the plain mass walls (no skin) as **ShadowsOnly**, about 12 tris a mass. This is light-leak insurance where an L2 proxy has gaps; drawn in colour, they would z-fight with the modules' back planes.
- **The existing 935 kit facades:** switch `shadowProxies` on (LOD1) in the same A/B. It has been held since 008 B6 "until the Air shows a gain".
- **Glint judges** the relief shadows (L2 against L0 casting) on yaw 30 and 210 shots. If L2 loses too much, use L1 proxies: +49–455 k caster tris (computed).
- **Far modules:** the play camera has no far. The air cascade takes in tall modules near the camera, and the L2 proxy keeps them cheap. A3 item 9 (small-mesh shadow cull on the last cascade) can drop the corner-storey proxies; that is Glint's call.

### 3.5 Pixels (owners: Vela for materials; Glint for the look)
- **Windows on the new side pieces: opaque.** Use DK_Window_Tower-style enamel, as AlleyWall_6 already does, not the transparent DK_GlassDark (alpha 0.72, queue 3000). Two reasons:
  - it saves an estimated 0.09–0.31 ms warm at play (a blended lit layer over 8–15 % of the screen);
  - it removes a visible bug: FadeTags leaves transparent glass unfaded, so the cut would leave glass panes floating.
- **No alpha-test or discard in module materials,** except the cut's own variant.
- **Any new textured or normal-mapped wall material gets its own warm A/B** (064 A5). DK_Limestone_Tex (base + normal + mask) is the heaviest of today's set.

### 3.6 CPU and step-2 levers (owner: Latch, only on the A/B's verdict)
- **Facing cull at play zoom (step 2):**
  - Modules go on 4 facing layers by outward normal. The play camera's cullingMask drops the modules that face away at the current quarter yaw; everything shows during a free turn and in the ground view.
  - Computed: 30–100 % of view modules face the camera (cell-open@120: 21 → 7; parade@210 zoom 14: 95 → 14).
  - It needs the modules not to cast. The proxies stay on an always-drawn layer.
- **Umbra baked occlusion for the ground view (step 2, unproven):** check first that it works with the GPU Resident Drawer. The drawer's own GPU occlusion measured +36 % on the Studio (064), so it is not that.
- **Scene size:** 5,516 column prefabs plus proxies. Build time and scene load are Latch's to report in phase 2.

### 3.7 Brief 064 A3 settings, at their 1080 estimates (owners as in 064)
Brief 064 rescaled the per-pixel items to 1080 by 0.39×.

| item | change | est. warm saving at 1080, play | look | judge |
|---|---|---|---|---|
| 1 | SSAO off for the ortho play camera (TacticalShadows Begin/End, or a second renderer) | 0.8–1.4 | none expected (Falloff 100 m < the 142–178 m to the street) | Glint confirms |
| 4 | Opaque texture off | already 0 in PC_RPAsset; counted 0 | none | done |
| 5 | Depth texture by request + the sun's flare component off in the play tier | 0.12–0.23 | none | Latch |
| 7 | Signals overlay camera → a pass in the base camera | 0.15–0.5 (part CPU) | none if signals draw the same | Latch, Glint |
| 3 | Soft shadows High → Medium (asset) | 0.27–0.59 | firmer penumbra | Glint |
| 6 | Bloom from quarter resolution | 0.12–0.27 | wider glow | Glint |
| 8 | SMAA Medium → Low | 0.12–0.27 | figure edges | Glint |
| 9 | Small-mesh shadow cull, last cascade | 0–0.3 | tiny far props lose shadows | Glint |

- **No-look set (1, 5, 7):** 1.07–2.13, ×0.85 for overlap → **0.9–1.8 ms**.
- **Look set (3, 6, 8, 9):** 0.51–1.43, ×0.85 → **0.43–1.2 ms**.

## 4. The model, and what it projects (estimates; the Air decides)

### 4.1 Model
`T_warm = T0 + ΔG + ΔP + ΔD − S_A3`, warm Air, 1920 × 1080, `-novsync`.
- **T0:** today's warm play-opening at 1080 is estimated at 12.7–14.2 ms (064 "The 1080p bar"; not yet measured warm at 1080). tp-worst: 14.1–15.7.
- **ΔG (geometry) = k · (Δview tris + Δcaster tris),** with k ≤ 0.7 ms per million drawn triangles, shadows included. This is 064 A5's upper bound from 0.2.122, warm. Vertex work does not scale with pixels, so k holds at 1080.
- **ΔP (pixels) = A_wall · f_new · c_px · r + glass:**
  - A_wall: 35–50 % of 2.07 Mpx is wall (estimate; Latch can measure it with a wall-ID render);
  - f_new: 0.79 of the seen wall goes from the procedural atlas to kit (Vela);
  - c_px: 2.1–2.5 ms per Mpx warm (064's pixel model);
  - r: 0.10–0.25, how much more a DK material costs than the one-sample atlas facade (texture, normal and mask samples, enamel clear coat);
  - glass: 0 with opaque side windows, else 0.09–0.31.
- **ΔD (draws, CPU):** +11–84 instanced draws per play view. The play frame is GPU-bound (render thread 2.0 ms against a 15–21 ms frame), so 0–0.1 ms.

### 4.2 Projection, cell's opening view (cell-open@30–120, zoom 14)

| step | ΔG | ΔP | ΔD | warm mean (ms) |
|---|---|---|---|---|
| today (T0, estimate) | | | | 12.7–14.2 (central 13.45) |
| + every face, as planned (L2 proxies, opaque side glass) | ≤ 0.07 (53 k + 40 k tris) | 0.12–0.51 | 0–0.1 | **12.8–14.9** (13.85) |
| same, but modules cast at L0 and transparent glass | ≤ 0.53 (53 k + 683 k) | 0.21–0.82 | 0–0.1 | 12.9–15.6 |
| + A3 no-look (1, 5, 7) | | | | **11.0–14.0 (central 12.5)** |
| + A3 look items (3, 6, 8, 9; Glint) | | | | 10.6–13.6 (central 11.7) |

- **Against A5's 13.0:**
  - the central projection passes once A3's no-look items land;
  - the worst corner (T0 at 14.2 with every pessimistic factor) is 1.0 over, or 0.6 over with the look items.
- **Every face costs +0.12–0.68 ms.** That is inside A5's post-A3 allowance (13.0 minus the measured warm mean, capped at 1.5 ms), but not frame-neutral (068). This is Sky's deliberate spend, as brief 072 says.
- **p99:** at 12.5 mean, the Air's p99/mean of 1.20–1.41 gives 15.0–17.6, borderline against 16.7. At 11.7 it gives 14.0–16.5.
- **Worst play views:** parade@300 and crown-circus@120 at zoom 14 add ≤ 0.16 ms ΔG. At zoom 18.5, parade@210 adds ≤ 0.27. The ΔP term is the same, so they are within ~0.2 ms of the opening.
- **Ground view (tp-worst):**
  - estimate +0.2–0.5 ms GPU (≤ 0.07 tris, 0.1–0.4 pixels), plus ~840 more drawn renderers;
  - render thread at 064's 1.7 µs a renderer: +1.4 ms CPU (3.2 → 4.6), inside the GPU-bound frame;
  - result: 14.3–16.2 before A3, still over 13.0 as today. The ground view needs brief 048's geometry items, or render scale (Sky); SSAO stays on there.

### 4.3 Decision rules for the A/B (section 5)
- every − today ≤ +0.7 ms warm at the play stops: as projected, keep it.
- \> +1.0 ms: apply step-2 levers in this order, then re-measure:
  1. the facing cull;
  2. the column bake (if not in yet);
  3. drop the corner-storey proxies (A3 item 9).
- every-l0cast − every gives the price of the shadow policy (projected 0.15–0.5 ms at the play stops), for Glint's call between L2, L1 and L0 casting.
- every-ssaooff at cell-open@30 with mean ≤ 13.0 and p99 ≤ 16.7: it fits. Otherwise A3's look items next (Glint), then render scale (Sky).
- Ground: tp rows every − today ≤ +0.5 ms and render thread ≤ 5 ms. Otherwise step 2: the 32 m cell HLOD.

## 5. The Air A/B run (needs Sky's OK; specified, not run)
- **Files:** `run-air-072.sh` (zsh, `zsh -n` clean, never run), `perf-072.json` (the perf-harness plan) and `tplong-072.json` (the ground rows), all in this folder.
- **Player:** one development build (the perf harness runs only in development players) with every face built in and the switches below.
- **Switches Latch must add first:**
  - `PerfVariant.everyFace` `on | off`. The every-face build keeps today's procedural walls and skin under a root that `off` shows while hiding the modules (development builds only).
  - `PerfVariant.moduleShadows` `proxy | lod0`.
  - `PerfStop.yaw`. PerfBuild resolves the stand-off with the stop's yaw; today the rig's yaw 30 is global.
  - A `variant` on tplong rows (PerfSwitches applied before the row).
- **Size:** plan width × height 1920 × 1080, window `Windowed`. Each pass's real size is read back (`atsize`) and a pass not at 1920 × 1080 is labelled.
- **Warm-up and controls:**
  - the harness's thermal plateau, `minSeconds` 600 and `maxSeconds` 900, so the A/B runs warm, the same ~10 min after which 0.2.122 had settled into its ×1.4–1.56;
  - a cold gate (thermal state 0, GPU < 45 °C) and macmon when it is installed (Aster asks Sky);
  - AC power, not charging, Low Power Mode off, apps closed. These are soak-air.sh's controls.
- **Pass `ab072`:**
  - uncapped (`vSync` 0), settle 2 s, record 10 s, 3 rotated repeats, `repeatFirst`;
  - stops: cell-open@30, cell-open@120, office-front@30, office-front@210, parade@210, crown-circus@120;
  - variants: `today`, `every`, `every-l0cast`, `every-ssaooff`;
  - 72 segments × 12 s, about 15 min after the warm-up.
- **Then:** tplong-072 (tp-street, tp-worst × today and every; play-opening × today and every), `-novsync`. Then one capped pass: vSync on, 30 s, cell-open@30, today and every.
- **Total:** about 30–35 min with the warm-up.
- **Delivery:** scp to and from the Air (`sky@Skys-MacBook-Air.local`, `-o HostKeyAlias=skys-macbook-air-1`), not Taildrop. The script zips the results; whoever runs it fetches them by scp.
- **Verdict (Redline reads it):** section 4.3, plus noise gates:
  - the repeatFirst segment within 3 % of its first;
  - the plateau reached;
  - with macmon, the GPU clock flat within 5 % over the A/B window. If not, the A/B is labelled thermal-noisy and rerun.
- **Before the Air:** Latch's phase-2 Studio capture of the every-face scene, at the same views and with GRD on as the player has it.
  - Gates: module counts within ±20 % of section 1.2, and view tris within ±25 %. Otherwise Redline re-derives before any Air time is asked for.

## 6. What the export must carry, per module (owner: Splice, `tools/world/gen/facades.ts` and `contracts/world/world.schema.ts`)
- **Identity:**
  - `id` (stable, seeded by building id and face);
  - `piece` (kit id);
  - `building`, `mass`, `edge` (face index), `a` / `b` (face span), `at`, `yaw`, `z` (base), `scale` [x, y, z];
  - `storeys`;
  - `family` (`side` | `alley` | `corner-out` | `corner-in` | `stack-base` | `stack-mid` | `stack-top` | `landmark`);
  - `service` (bool) with `why` (the rule that made it AlleyWall).
- **LOD:** `lodClass` (`bay3` | `bay4` | `storey` | `column` | `cornerStorey` | `landmark`). Distances live in kit.json per class (`lodDistances: [45, 120, 330]`), not per module.
- **Combine group:** `column` (the id shared by the storeys of one bay position on one face), with `columnStoreys`. The builder bakes one prefab per (family, columnStoreys, colourway).
- **Instancing key:** `colourway` (the material slot set id, explicit, never derived in Unity), so draws per view are countable from the export. The key = piece or column variant + colourway.
- **Shadows:** `casts`: `proxy-l2` (default for modules) | `none`.
- **Facing:** `normal` [x, y] (outward), for the step-2 facing cull.
- **HLOD (step 2 only):** `cell` = [floor(x/32), floor(y/32)].
- **Coverage, per face:** dressed by [module ids] | kit facade id | party (with length and height). This feeds the 072-C "0 undressed faces" check.
- **District summary:** modules per piece; placed tris per LOD (from kit.json); columns; colourways per piece.
- **kit.json, per new piece:**
  - real `size` (the mesh bounds) and `lods[].tris`;
  - `lodClass`;
  - a `materials` list that respects the shared set.

## 7. Builder changes (owner: Latch)
1. **KitLods:**
   - each piece's LOD screens come from its class's `lodDistances` and its unscaled LODGroup size (`s / (1.041 · D)`), in place of the fixed category switch, for module classes;
   - a build check: every L1 switch distance ≥ 36 m (the play-zoom guard), else fail.
2. **Column bake:** storey modules of one `column` merged into one mesh per (family, storeys, colourway, LOD). Prefabs under Built/, LODGroup per column (class `column`), not combined across columns.
3. **Shadows:**
   - module renderers `ShadowCastingMode.Off`;
   - ShadowProxies casts module classes from LOD2;
   - KitCover keeps a ShadowsOnly mass shell under modules instead of deleting the walls;
   - `shadowProxies` per class in the builder's kit.json, on for modules, and in the A/B for the existing facades.
4. **Instancing:**
   - modules stay prefab instances under the facades layer: GRD on, no MaterialPropertyBlock, no StaticEditorFlags.BatchingStatic;
   - FadeTags tags them (the facades layer already is) and FadeReach works per renderer.
5. **Perf switches** for the A/B (section 5): `everyFace`, `moduleShadows`, `PerfStop.yaw`, a tplong row `variant`, and the kept procedural root (development builds only).
6. **Phase-2 capture** at section 1.2's views plus tp-street and tp-worst, GRD on and off, in the same format as the baseline: tris, SetPass, draws, shadow casters, visible renderers, and census per layer.
7. **Still open from 064:** Perf064.cs honours `-capture-size`; A3 items 1, 5 and 7, which are this plan's main source of frame time.

## 8. Open issues
- **Piece tris are assumed** (SideBay from PortholeBay_3L/_4L, storeys from PortholeBay_7, corners guessed). Vela's 072-A table replaces them; then re-run `every_face.py` (~16 s).
- **The service rule here** (a mass within 12 m) is a stand-in. Splice's rule changes the SideBay/AlleyWall split, not the count.
- **Caster counts** come from a sphere-sweep model of URP's cascade culling, and are an upper bound. Latch's frame-debugger count replaces them.
- **T0 is not measured warm at 1080.** The 064 soak and A3's second sitting are still owed. Every ms here moves with them.
- **Latch's baseline ran with GRD off** (the editor). The player has it on, and SetPass and draws differ under the drawer.
- **The A_wall and r factors** in ΔP are estimates. A wall-ID render (Latch) and the A/B replace them.
- **GRD with Umbra baked occlusion:** unconfirmed. One search found nothing either way ([Unity 6 GPU occlusion culling](https://docs.unity3d.com/6000.2/Documentation/Manual/urp/gpu-culling.html), [Make objects compatible with GPU rendering](https://docs-alpha.unity3d.com/Manual/urp/make-object-compatible-gpu-rendering.html), [LOD cross-fade with the GPU Resident Drawer, forum](https://discussions.unity.com/t/lod-crossfade-in-graphics-resident-drawer/1561993)). LODGroups here use fadeMode None, so cross-fade does not matter.
- **The procedural frontages** (1,077 ground-floor shopfronts, procedural boxes) are not counted. Whether a SideBay stands behind or above them is Splice's and Vela's call.
