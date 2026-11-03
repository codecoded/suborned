# 072-D · Redline · phase 2: every face measured, the builder reviewed, 072-F costed (2026-10-27)

Inputs:
- export v4 `out/world/harbour-district.world.json` (sha256 a50c649a…, the same file as Latch's `out/scratch/latch/072/v4/v4.world.json`);
- `contracts/world/kit.json` (fbe1f593…);
- Latch's offscreen capture (`briefs/072/latch/after/offscreen.{md,json}`, batchmode, GRD off, **not UnityStats**);
- his baseline (`briefs/072/latch/baseline/`, UnityStats, Editor);
- the build log `out/scratch/latch/072/logs/build-v4.log`;
- builder commit db30a0af;
- Vela's `briefs/072/pieces/pieces.md` and `briefs/072/texture/{design.md,flat.md,area.json}`;
- Latch's uncommitted 072-F shader work in the tree (FacadeLit.shader, FacadeSurface.hlsl, Look.cs, Combine.cs), read only.

Labels: **computed** (my script, from the export and the catalogue) · **batchmode** (Latch's offscreen estimate) · **UnityStats** (Editor Game view) · **estimate** (brief 064's models) · **Studio** (any ms on the M5 Max).

To re-run my numbers (5 s): `python3 -I briefs/072/redline/every_face_v4.py out/world/harbour-district.world.json contracts/world/kit.json briefs/072/redline/every_face_v4.json`. It reads the export's own 16,903 modules and prices them with the catalogue's real triangles. Phase 1's `every_face.py` tiled walls itself with assumed triangles, and it is kept unchanged so phase 1 still reproduces.

## 0. In short
1. **Gates passed. The model now matches the build.**
   - Module roots in frustum, computed against Latch's count: 5 / 14 / 0 / 0 / 18 / 29 / 15 / 4 on both sides at all 8 views. That is an exact match, inside the ±20 % gate.
   - Module L0 triangles in view: computed 6–14 % below Latch's mesh counts (catalogue tris against imported index counts), inside the ±25 % gate.
   - District L2 total 966,462, the same figure as the build log's module proxies.
   - Phase 1's tiling was 2.3× too heavy: 22.6 M L0 tris assumed, 9.73 M real.
2. **Every face is cheap at play zoom.**
   - All-pass triangles grow +2 k to +110 k per view (+0.1 to +5.1 %).
   - SetPass estimate: unchanged.
   - Materials: +0 to +2.
   - Shadow triangles: −0.3 k to +8.4 k.
   - Casters: +1 to +36.
   - Modules plus the old kit facades cover at most 2.3 % of the frame at these views (area.json). Most of the play frame is ground (55 %) and the civic block (33 %).
   - **Projected cost of every face at the opening: +0.04 to +0.21 ms warm on the Air (estimate).** That is inside 064 A5's frame-neutral +0.3 ms.
3. **Projected warm Air mean at the opening, 1920 × 1080: 13.55 (12.7–14.4) with every face, and 12.2 (10.9–13.5) once 064 A3's no-look items land, against 13.0.** Phase 1 had 12.5. It moved down because the pixel term collapsed. T0 is still unmeasured warm at 1080, and it is the biggest error bar.
4. **Air time: don't ask Sky yet.**
   - Every face's expected delta is near the run's resolution.
   - 072-F has not landed, and its textured build needs its own switch.
   - Ask for one sitting once 072-F, A3's no-look items and the window-shadow fix (5 below) are in the same development player.
   - That sitting measures T0, every face, 072-F and A3 together.
   - The variants did not change. I updated the run files' notes; see section 5.
5. **Builder review: SRP batcher and instancing are intact.** 165 column layouts for 2,265 columns, no MaterialPropertyBlock, no static batching, fadeMode None. Findings:
   - **one look defect (high):** the shadows-only shell, and the L2 proxies, shadow every window pocket on sunlit walls;
   - **the crown guard (medium):** 3 of the 7 roof crowns are already at L1 at the default zoom 14;
   - **mesh memory (medium):** the column meshes are kept readable, with 32-bit indices;
   - **CPU to check (medium):** the build adds ~18.6 k renderers and 4,654 LODGroups;
   - **two latent issues (low).**
6. **072-F has not landed** (no `briefs/072/latch/textured/`, no `area-after.json`). Expected cost: **+0.06 to +0.24 ms warm at the opening, SetPass up to +4 (not +2), +15.98 MiB.**
   - One shader defect to fix before the capture: the detail sample sits in a divergent branch with a per-triangle UV scale.
   - Pass/fail criteria are in section 4.

## 1. Latch's counts against the projection

### 1.1 Modules per view (gate: ±20 %)

| view | yaw | roots in frustum, computed (v4) | Latch batchmode | module L0 tris, computed | Latch | phase 1 projection (old export, stand-in tiling; columns) |
|---|---:|---:|---:|---:|---:|---:|
| office-front | 30 | 5 | 5 | 2,928 | 3,426 | 2 |
| office-front | 120 | 14 | 14 | 9,922 | 11,546 | 5 |
| office-front | 210 | 0 | 0 | 0 | 0 | 2 |
| office-front | 300 | 0 | 0 | 0 | 0 | 1 |
| cell-open | 30 | 18 | 18 | 20,770 | 22,946 | 6 |
| cell-open | 120 | 29 | 29 | 50,280 | 53,498 | 20 |
| cell-open | 210 | 15 | 15 | 17,406 | 19,918 | 9 |
| cell-open | 300 | 4 | 4 | 4,696 | 5,360 | 2 |


- Phase 1's projection missed by 2–3× per view. It used the old export with my own stand-in tiling: no pilasters, no shop fillers, a different service rule.
- **Re-derived from export v4, the frustum model matches the build exactly, so the gate passes.**
- Triangles: the computed figure is the catalogue's `tris`, and it reads 6–14 % below Unity's index counts.
- District, computed:
  - 16,903 modules;
  - 4,654 roots (2,265 baked columns holding 14,514, plus 2,389 singles);
  - **L0 9.73 M / L1 1.41 M / L2 0.97 M tris** (phase 1 assumed 22.6 / 5.5 / 1.3).

### 1.2 Everything else, every face against today on the same build (batchmode)

| view | yaw | main tris Δ | all-pass tris Δ (% of today) | materials Δ | SetPass est. Δ | casters Δ (unique) | shadow tris Δ | phase 1 projected view Δ / caster Δ (L2) |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| office-front | 30 | +3.6 k | +11.1 k (0.7 %) | +1 | 0 | +9 | +4.0 k | 35 k / 28 k |
| office-front | 120 | +10.8 k | +26.5 k (1.6 %) | +1 | 0 | +17 | +4.9 k | 50 k / 32 k |
| office-front | 210 | +1.9 k | +5.2 k (0.3 %) | 0 | 0 | +1 | +1.5 k | 31 k / 22 k |
| office-front | 300 | +1.2 k | +2.0 k (0.1 %) | 0 | 0 | +1 | −0.3 k | 30 k / 12 k |
| cell-open | 30 | +19.9 k | +44.8 k (2.0 %) | +2 | 0 | +21 | +5.0 k | 28 k / 38 k |
| cell-open | 120 | +50.9 k | +110.1 k (5.1 %) | +2 | 0 | +36 | +8.4 k | 53 k / 40 k |
| cell-open | 210 | +18.1 k | +43.1 k (2.0 %) | +1 | 0 | +27 | +6.9 k | 47 k / 35 k |
| cell-open | 300 | +5.7 k | +17.3 k (0.8 %) | +1 | 0 | +13 | +5.9 k | 11 k / 27 k |

- **Triangles:** inside 064 A5's Studio rule (≤ +10 %) at every view.
- **SetPass:** the batchmode estimator shows no growth. It counts distinct variants, and the module materials are the kit facades' own.
  - The estimator reads 36–41 against UnityStats' 55–69. Read its deltas, not its level.
  - The UnityStats rerun in the Game view is still owed (Latch, when the Studio is free).
- **Casters:** +1 to +36. This breaks the letter of A5's "casters may not grow", but at +5–8 k shadow triangles. Every face is Sky's deliberate spend (brief 072), so I record it rather than block on it.
- **My caster model overcounts.** It sweeps a sphere toward the sun and gives 68–232 module roots casting per view, L2 8–29 k tris. Latch's census reproduces URP's own split planes and sees far fewer. Mine stays an upper bound; Latch's is the number used below.
- **The batchmode ms are not usable** as a GPU estimate:
  - every / today medians: 5.6 / 2.89, 3.48 / 2.67, 3.98 / 2.78, 4.0 / 5.17, 6.84 / 6.36, 7.16 / 3.83, 6.56 / 4.53, 6.21 / 5.92;
  - the differences run from −1.17 to +3.33, median +1.0 ms;
  - that is 8 warm frames and 30 timed, not interleaved, with GRD off, and one of eight goes the other way.
  - The M5 Max draws these views far faster than 3–7 ms, so this is mostly CPU: culling and LOD selection over a scene whose renderer count every face roughly doubles (section 2, F5).
  - It flags a CPU question for the player (F5). It does not price the GPU.

### 1.3 Ground view (computed, the tp rows of phase 1, distance classes 45 / 120 / 330 m)

| row | roots in frustum | L0 / L1 / L2 / culled | module tris drawn | roots ≥ 150 m | phase 1 |
|---|---:|---|---:|---:|---|
| tp-street | 731 | 1 / 58 / 485 / 187 | 110 k | 623 | 2,619 modules, 0.21 M |
| tp-worst | 781 | 0 / 15 / 471 / 295 | 73 k | 740 | 1,921 modules, 0.11 M |

- That is +486–544 drawn module renderers, plus their L2 proxies within the 200 m shadow distance.
- At 064's 1.7 µs per drawn renderer (a GRD-off rate, so an upper bound): **+0.8–0.9 ms render thread** (phase 1 said +1.4). It runs inside a GPU-bound frame.
- The 32 m cell HLOD (plan 3.2, step 2) stays unneeded unless the A/B's render thread exceeds 5 ms.

## 2. The model, updated: projected warm Air mean against 13.0

`T_warm = T0 + ΔG + ΔP + ΔD − S_A3`. Air, warm, 1920 × 1080, `-novsync`. Constants as in plan.md 4.1.
- **T0:** 12.7–14.2 ms (central 13.45), still the estimate from 064's 1080 rescale. It has not been measured warm at 1080.
- **ΔG = k · Δ(all-pass tris),** k ≤ 0.7 ms per M (064 A5, warm, shadows included).
  - Measured: +45 k (y30) to +110 k (y120) at the opening, so **ΔG ≤ 0.03–0.08**.
- **ΔP = S_mod · 2.07 Mpx · c_px · r:**
  - S_mod ≤ 2.3 %: the whole `HarbourDistrict/facades` layer in area.json (v4 build, 8 views), old kit fronts included, so an upper bound for the modules' share;
  - c_px 2.1–2.5 ms per Mpx warm;
  - r 0.10–0.25, the extra cost of a DK material over the one-sample atlas;
  - side windows opaque, so no glass term;
  - **ΔP = 0.01–0.03.** Phase 1 took 35–50 % of the frame to be wall and got 0.12–0.51. The material census shows that was wrong for these views.
- **ΔD:** the frame is GPU-bound and the visible renderers grow by +0 to +28, so **0–0.1** (the CPU side is F5).
- **Every face at the opening: +0.04 to +0.21 ms (central ~0.1).** That is inside the pre-A3 frame-neutral rule (≤ +0.3 ms).

| step | warm mean, opening (ms) |
|---|---|
| today (T0, estimate) | 12.7–14.2 (13.45) |
| + every face (measured counts, L2 proxies, opaque side windows) | **12.74–14.41 (13.55)** |
| + 064 A3 no-look (1 SSAO off at play, 5, 7: −0.9 to −1.8) | **10.9–13.5 (12.2)** |
| + A3 look items (Glint: −0.43 to −1.2) | 10.5–13.1 (11.4) |
| + 072-F as expected (section 4: +0.06 to +0.24), after A3 no-look | 11.0–13.8 (12.35) |

- **Against 13.0:**
  - central passes once A3's no-look items land;
  - the worst corner (T0 14.2 with every factor pessimistic) is 0.5 over, or 0.8 over with 072-F;
  - p99 at 12.2 mean is 14.6–17.2 (×1.20–1.41), borderline against 16.7.
- **Heaviest module views** (not A5 rows): crown-circus@120 and parade@300 at zoom 14, computed:
  - +95–128 k view tris, plus ≤ 60–87 k L2 caster tris, so ΔG ≤ 0.15;
  - modules there may cover 20–35 % of the frame (not measured), so ΔP 0.09–0.45;
  - **+0.1 to +0.6 ms.** If Glint or Sky care about those streets, a material-id pass at those two views (After072) closes the guess.
- **Ground (tp-worst):** ΔG ≤ 0.05 plus ΔP ≤ 0.1. It stays 14.1–15.7 + ~0.1, over 13.0 as before; that is brief 048's problem.

## 3. Builder review (db30a0af), by severity

### F1 · High (look, correctness): the shell and the L2 proxies shadow every window pocket on sunlit walls
- **What's wrong:**
  - The L0 window film sits **0.14 m behind the building line** (`sides.py`: SKIN −0.02, REVEAL 0.2, film at SKIN + REVEAL − 0.04, Blender +y into the wall).
  - The shadows-only shell is drawn **on the building line**: ProcFacade `shell.Wall(p0, p1, …)` on the ring edge, both sides.
  - The module's L2 proxy is a plain box slab (`_slab`: "lod 1/2: a plain box") with the film quad 3 cm proud (`_window` lod 2 at SKIN − 0.01).
  - Both casters stand in front of the pocket. A 2048 cascade texel here is ~5.6 cm (sphere r 57.5 m), and the bias is about a texel.
- **Effect:** every pocket and film on a face toward the sun (sun yaw 330, pitch 38) is in full shadow, where real reveals would shade only a ~0.1 m strip at the head and one jamb.
  - The shell does it in **both** A/B variants. That includes `every-l0cast`, which would therefore misprice nothing but mis-show the look Glint is judging.
- **Fix:**
  1. **Latch:** offset the shell 0.36 m inward, behind the 0.35 m slab back (`BACKY`), so it is behind every pocket. It still catches light leaks at proxy gaps.
  2. **Glint:** judge every against every-l0cast (shell fixed) on a sun-facing wall at yaw 30 and 210. Latch's After072 shots can do this on the Studio.
  3. **If the proxy look loses:** Vela cuts the window openings in the L2 slab (holes, film quad at pocket depth). That is about +8–16 L2 tris per window: district proxy tris ~0.97 → ~1.2 M, view casters still ≤ ~35 k.
  4. **The cheap alternative:** a builder-only proxy mesh that drops L2's DK_Window_Film submesh does **not** work, because the slab behind has no hole.
- **Cost:** shell inset 0. Holed L2 ≤ +0.02 ms (ΔG). L1 proxies instead: +49–455 k caster tris (plan 3.4).

### F2 · Medium (GDD §12, plan 3.1): 7 roof crowns switch to L1 inside 36 m; 3 are at L1 at the default zoom
- **What the log says:** `roofs/DK_Crown_Dish x3 from 26.6 m; Crown_Finial x2 32.0; Crown_Starburst x1 33.9; Crown_Atom x1 34.7`.
- **Ortho equivalence:** a zoom z equals the follow view at d = z / tan 27.5° = 1.921 z. So the switch zooms are:
  - Dish at L1 above **zoom 13.85**, so already L1 at the opening's 14;
  - Finial above 16.7;
  - Starburst above 17.6;
  - Atom above 18.1.
- **Fix:**
  - **Splice:** a `lodClass` for the `Crowns` category in `tools/kit` (lod.ts), for example a `crown` class at [45, 120, 330]. The `towers` category's fixed 0.3 / 0.1 is what puts them below.
  - **Latch:** add `roofs` to `PlayZoomGuard`'s strict set, so this fails the build in future.
- **Cost:** L0 − L1 is +2.1 k (Atom), +0.6 k ×3 (Dish), +0.9 k ×2 (Finial), +0.5 k (Starburst): **+6.4 k tris district-wide**, at most one or two in a view. That is nil.

### F3 · Medium (memory): column meshes are readable and use 32-bit indices
- **What's wrong:**
  - `KitColumns.Bake` saves `UploadMeshData(false)` meshes, so every column asset has `m_IsReadable: 1`.
  - It forces `IndexFormat.UInt32`. The largest column L0 has 23,768 vertices; all are under 65,535.
  - 490 meshes, ~0.8–1.6 M vertices (101 MB of YAML under Built/Columns).
- **Effect:** a readable mesh keeps a CPU copy in the player beside the GPU copy, so roughly +50–90 MB of RAM for nothing, and double the index memory.
- **Fix (Latch):**
  - pick UInt16 when `vertexCount < 65535`;
  - after `CreateAsset`, clear `m_IsReadable` (SerializedObject) or mark it no longer readable. The builder needs only `GetIndexCount`, which works in the Editor either way.
  - Check with a Memory Profiler snapshot of the development player.
- **Cost:** frame-neutral; saves memory and load time.

### F4 · Medium (correctness, singles): a proxy casts only the first renderer of its level
- **What's wrong:** `ShadowProxies.Build` takes the first renderer with a mesh in `lods[level]` as the proxy, then sets **every** LOD renderer of the group to Off.
  - Baked columns have one renderer per level, so they are safe.
  - For the 2,389 single modules, and the kit facades and trees as before, any extra renderer in L2 loses its shadow.
  - Proxies = roots = 4,654 says every root got one, not that each is whole.
- **Fix (Latch):** log, or fail on, the count of groups whose proxy level holds more than one mesh renderer. If there are any, combine that level's renderers into the proxy mesh.

### F5 · Medium (CPU, to measure): scene renderers roughly double
- **What's added:** 4,654 roots, each L0 + L1 + L2 + a proxy: about **+18.6 k renderers and +4,654 LODGroups** scene-wide (today: 5,668 batch renderers plus ~13 k kit renderers).
- **What we know:** the GPU-bound play frame hides CPU growth, but not on p99 or under the fight's 30 combatants. The batchmode medians (+1.0 ms, GRD off) hint at it and prove nothing.
- **Check (Latch, Studio, before the Air):**
  - a development player at 1920 × 1080;
  - the perf harness's ab072 pass with `today` and `every` only, interleaved ×3;
  - read FrameTimingManager CPU main / render and GPU, plus the culling profiler markers.
- **Gate:** CPU main + render Δ ≤ 0.5 ms at the opening, labelled Studio. If it is over, the lever is merging each root's L0 / L1 / L2 into one renderer with LOD by mesh swap. That is not a step-1 change; it comes back to me first.

### F6 · Low (latent): the guard divides by lodBias instead of multiplying
- `PlayZoomGuard` computes `DistanceAt(size, h · lodBias) = size / (K h bias)`.
- Unity's effective switch is at `size · bias / (K h)`, and lodBias scales ortho and perspective alike.
- It is right only at lodBias 1, which both quality levels are today (QualitySettings `lodBias: 1`). Fix: `DistanceAt(size, h / QualitySettings.lodBias)`.

### F7 · Low (reading the A/B): `today` is this build's walls without modules, not the pre-072 scene
- The CrownTol rule (no procedural parapet over a piece that reaches the roof line) and the RecordsTower are in both variants.
- That is the right like-for-like for the switch. Never set `today` against the 18:13 baseline as if they were the same scene; Latch's table already keeps them apart.

### Checked and fine
- **Columns:**
  - one renderer per level, a submesh per DK material (the pieces' own materials), so the SRP batcher holds;
  - 165 layouts for 2,265 columns (13.7 instances per mesh), so the GPU Resident Drawer instances them;
  - layout key: piece, millimetre offsets and colourway;
  - static flags copied from the template, harmless with static batching off;
  - `LODFadeMode.None`;
  - LOD size from the baked column, so the switch is by distance whatever the height;
  - x-scale on the instance, which LODGroup sizing ignores as it should, because height dominates.
- **KitLods:** distance classes give a nearest building-layer L1 of 41.4 m (CornerPier_Out_3), at or above 36 m. The build throws otherwise.
- **Shell:** ShadowsOnly, no probes, faded with its modules (FadeTags Whole), off with `everyFace off`.
- **Proxies:** they take L2's rendering layers after FadeTags, so the cut reaches their shadow. They are children of the module root, so `everyFace off` hides them.
- **EveryFace / PerfSwitches / TpLongRun:**
  - every row and variant sets both switches;
  - `off` fails without the `today` walls;
  - Strip072 drops `today` from release builds only.
- **ProcFacade:** walls with no module stay in the shared `buildings` layer for both variants. Only walls a module stands on are split into `-every` and `-today`. Correct.

## 4. 072-F: design review, expected cost, pass/fail (the textured capture has not landed)

### 4.1 Design and in-progress code
- **D1 · Medium (artefact, fix before the capture).** In Latch's FacadeSurface.hlsl the array sample sits inside `if (cls != 3 && cls != 5)`, and its UV is `uv / kSurfaceTile[lay]` with `lay` per triangle.
  - The class is constant per triangle, not per quad. On every class edge (bands are 5–15 px tall at zoom 14) a 2×2 quad mixes lanes, so the implicit derivatives come out mixed or undefined:
    - between branches, on Metal;
    - between tiles of 0.5–3 m, so the wrong mip along every band edge.
  - Fix, same cost:
    - take `ddx(uv)`, `ddy(uv)` outside any branch;
    - `SAMPLE_TEXTURE2D_ARRAY_GRAD(_SurfaceDetail, sampler_SurfaceDetail, uv / t, lay, ddx(uv) / t, ddy(uv) / t)`;
    - and either keep the branch or sample always and set k = 0 for classes 3 and 5.
- **D2 · SetPass is up to +4, not +2.**
  - `_DETAIL_MULX2` splits each DecoKit shader (Lit Cutaway, Complex Lit Cutaway) into detailed and plain variants in **two** passes: forward, and DepthNormals, which declares the detail keywords for the SSAO prepass.
  - Shadow casters are unaffected.
  - The criterion: the growth may not depend on how many rows are textured.
- **D3 · Fetches per textured kit pixel are +3, not +2.**
  - URP's `_DETAIL` path also samples `_DetailMask` (a white default when unassigned).
  - On rows without `_NORMALMAP` it switches on the tangent interpolator and the TBN.
  - Tangents are there: the FBX imports Mikk (`tangentImportMode: 3`), Latch's Combine change recalculates for `_DETAIL_MULX2` batches, and the column bake keeps them.
- **D4 · Instance stretch.** Pilasters scale 0.33–1.7 and ShopFill 0.5–1.7 in x, so the 1 m box-UV detail stretches up to 1.7× on them. Look call: Vela and Glint.
- **D5 · Fine:**
  - the textures sit outside `UnityPerMaterial`, so FacadeLit's CBUFFER and the SRP batcher are unchanged;
  - no new float properties;
  - stone loses its two value-noise octaves (ALU down);
  - world-space UVs stay in float;
  - 15.98 MiB inside the 24 MiB budget;
  - BC7 / BC5 are native on Apple silicon under macOS;
  - aniso 8 on the array is worth a Studio check against 4. That is filtering, not resolution, so it is no decimation question.
- **D6.** `#pragma require 2darray` is in one pass. Check every pass that includes FacadeSurface compiles (DepthNormals, Meta). On Metal it is harmless either way.

### 4.2 Expected cost (estimate)

| part | share of frame (area.json) | added per pixel | warm Air, opening (ms) |
|---|---:|---|---:|
| kit rows with the pair | 25.3 % | +3 fetches, +TBN: r 0.05–0.15 of the pixel's cost | +0.055–0.20 |
| same, in the DepthNormals prepass (gone if SSAO is off at play) | 25.3 % | +2 fetches | +0.01–0.03 |
| FacadeLit | 6.2 % | +1 array fetch, −8 hashes on stone | ±0.01 |
| builder Look rows | 1.0 % | +3 fetches | ≤ +0.01 |
| **total** | | | **+0.06 to +0.24 (central ~0.13)** |

- SetPass: +0 to +4.
- Batches, triangles, casters, materials: unchanged.
- GPU texture memory: +15.98 MiB.
- Area-weighted fetches per frame pixel: about +0.8.
- **Against 064 A5:**
  - alone, it is inside the frame-neutral +0.3;
  - **every face + 072-F together: +0.10 to +0.45 (central ~0.23).** The central figure is inside the +0.3 that holds until A3 is measured; the top end is over.
  - After A3, both fit the allowance (13.0 minus the measured mean, capped at 1.5).

### 4.3 Pass/fail
Studio, same v4 scene: before = the 21:48 capture, after = textured. UnityStats in the Game view with GRD as the player has it, plus batchmode for the shares.
1. **SetPass Δ ≤ +4** summed over the 8 views. Fail also if it grows with the number of textured rows. Remedy: give every row of the split shader the pair, so no plain variant remains.
2. **Batches and draw calls Δ ≤ +1 %; materials Δ = 0; triangles Δ = 0; shadow casters Δ = 0.** The same material set means the same GRD instancing keys.
3. **SRP batcher:** FacadeLit and both DecoKit cutaway shaders report "SRP Batcher: compatible", and FacadeLit's `UnityPerMaterial` is unchanged (only a texture property added).
4. **Texture memory:** the new textures' runtime size ≤ 24 MiB (expected 15.98), and total texture memory Δ ≤ 24 MiB (Profiler.GetRuntimeMemorySizeLong, or a Memory Profiler snapshot).
5. **Sample cost:**
   - area-weighted fetches per frame pixel Δ ≤ +1.0;
   - computed from `area-after.json` × each shader's fetch count (D3);
   - Latch lists the per-shader counts.
6. **Studio GPU ms**, labelled Studio:
   - FrameTimingManager `gpuFrameTime` in a development player at 1920 × 1080, or an Xcode GPU capture;
   - interleaved ABAB ×3 at the 8 views;
   - Δ ≤ +3 % at every view counts as neutral on the Studio;
   - over that, it goes to the Air with the switch below.
7. **Air (the combined run):**
   - textured − flat ≤ +0.15 ms warm mean at the opening and tp-worst;
   - every face + texture ≤ +0.3 ms until A3's round is measured;
   - after A3, within 13.0 − the measured mean (cap 1.5).
8. **No seam:** a 2× crop of band edges at zoom 14 after D1 (Glint, Suture).
- **The switch the Air needs (Latch, development builds only):**
  - a `surfaceDetail` on | off on PerfVariant and on the tplong row;
  - kit rows: runtime `DisableKeyword("_DETAIL_MULX2")` on the textured materials (the plain variant is already in the build);
  - FacadeLit: `_SurfaceDetail` swapped to a 1 × 1 × 6 neutral array. This measures the bandwidth, not the fetch instruction; label it so.
  - **Until it exists, do not add the field to perf-072.json:** JsonUtility drops unknown keys silently, and the A/B would compare a build with itself. A PerfBuild check that rejects unknown variant keys would close this for good.

## 5. Air time and the run files
- **Not now.** The run spends ~35 min of Sky's Air for:
  - a delta (+0.04–0.21) near its resolution (3 rotated repeats; repeatFirst gate 3 %, about 0.4 ms at 13 ms);
  - it would have to be repeated when 072-F lands.
- **Ask Sky for one sitting (~40 min) when these four are in one development player:**
  1. 072-F textured, with the `surfaceDetail` switch;
  2. A3's no-look items 1, 5 and 7 (SSAO off at play at least);
  3. the shell inset (F1), with Glint's proxy call made;
  4. the Studio CPU check (F5) passed.

  That one run measures:
  - T0 warm at 1080, owed since 064;
  - every face;
  - 072-F;
  - the A3 projection.

  Then I add the `every-flat` variant (every face, surfaceDetail off) to ab072, and textured/flat rows to tplong.
- **Variants did not change** (today, every, every-l0cast, every-ssaooff; the tplong `variant`). Latch's fields match the spec, and PerfPlanner carries `PerfVariant` whole. Updated in this folder:
  - `run-air-072.sh`: header (the switches exist in db30a0af; the hold above). `zsh -n` clean, never run.
  - `perf-072.json`: note (the JsonUtility warning); the stops' reasons with v4 numbers (cell-open-120 29 roots, parade-210 318 caster roots, crown-circus-120 +128 k).
  - `tplong-072.json`: note.

## 6. Open issues
- **T0 at 1080 warm is still an estimate.** It is the largest term, and every ms here moves with it.
- **UnityStats every / today in the Game view** (Latch, when the Studio is free): SetPass and draw calls with GRD as the player has it.
- **The caster model** (mine) overcounts. Latch's census is used; the two disagree about 5–20× on module casters.
- **The heaviest module streets** (crown-circus@120, parade@300) have no material-id share. Their +0.1–0.6 ms is a guess until one After072 pass there.
- **Shop glass** on ShopFill and the street bays stays transparent DK_GlassDark (Vela pass 2): unfaded in the cut, blended. It is a look call, ask first; the cost is small at ≤ 2.3 % of the frame for all facades.

## Evidence
- `python3 -I briefs/072/redline/every_face_v4.py out/world/harbour-district.world.json contracts/world/kit.json briefs/072/redline/every_face_v4.json` (5 s):
  - 16,903 modules, 4,654 roots (2,265 columns + 2,389 singles);
  - district L0 / L1 / L2 9,727,964 / 1,405,500 / 966,462;
  - per-view roots 5, 14, 0, 0, 18, 29, 15, 4.
- `shasum -a 256`: export a50c649a… (same as v4.world.json), kit.json fbe1f593….
- Build log (`out/scratch/latch/072/logs/build-v4.log`):
  - line 3408: 2265 columns in 165 layouts, 2389 singles;
  - line 3711: 4654 module proxies, 966,462 tris;
  - line 3726: the 7 crowns;
  - line 3742: nearest strict L1 41.4 m.
- Column assets: `m_IsReadable: 1`, `m_IndexFormat: 1` on all; largest vertex count 23,768.
- `zsh -n briefs/072/redline/run-air-072.sh`: ok. perf-072.json and tplong-072.json parse.
