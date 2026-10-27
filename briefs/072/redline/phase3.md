# 072-D · Redline · phase 3: the holes, 072-F against 4.3, the projection, the one Air sitting (2026-10-28)

Inputs: Latch's `briefs/072/latch/textured/` (report.md 22:21; offscreen.json re-captured 22:53) and `briefs/072/latch/g/` (00:10, export g.world.json 6841dcfc…, kit.json d9586fcb…); a5234fdb (holes, palette, grade); 79283d9f (072-F, the switch). Everything is **batchmode** (offscreen, GRD off) or **computed**. Nothing is UnityStats or Air ms; the Game-view capture is still queued.

Re-run (3 s): `python3 -I briefs/072/redline/every_face_v4.py out/scratch/latch/072/g/g.world.json contracts/world/kit.json out/scratch/redline/072p3/every_face_g.json` → district L0 / L1 / L2 9,727,964 / 1,405,500 / **1,375,578** (Latch's log figure, exactly).

## 1. The +409 k proxy triangles: district-wide, not per view
- +409,116 is the sum over all 4,654 module L2s in the scene. Shadow casting is culled per cascade, so a view pays only for the proxies inside its two cascade spheres (r 57.5 m).
- **Measured per view (batchmode)**, isolated as (every − today) with holes minus (every − today) before. That cancels 072-G's common changes: the `today` side also moved −2.8 k at cell-open 30/300 with no proxies at all.

| view | 30 | 120 | 210 | 300 |
|---|---:|---:|---:|---:|
| office-front: holes' shadow tris | +272 | +362 | 0 | +142 |
| cell-open: holes' shadow tris | +596 | +1,596 | +892 | +212 |

- Casters, renderers, SetPass: unchanged (holes add triangles to meshes that already cast).
- **Cost at the opening: ΔG ≤ 0.7 ms/M × 1.6 k ≈ 0.001 ms.** Nil. My upper-bound sphere model (overcounts 5–20×) gives module L2 caster tris 11–41 k per play view (was 8–29 k), so ≤ +0.01 ms even there. The heaviest streets (parade-210, crown-circus-120): ≤ +0.03 ms.
- **Ground view, a real but small cost:** holes are in the L2 mesh itself, and at street level 471–485 roots draw L2 in the main view. Computed: tp-street 110 k → 155 k module tris, tp-worst 73 k → 98 k. With their shadows: **≤ +0.05 ms** at tp-worst. It is still brief 048's over-budget view.
- **Look:** F1 is closed. The proxy-against-L0 difference on the three test walls fell from 2.9–4.8 % of pixels to 0.2–0.3 %.

## 2. 072-F against phase 2 §4.3

**Read this first:** Latch's report.md (22:21) is stale. offscreen.json was re-captured at 22:53 after the DK_SurfaceDetail switch went in, and its SetPass figures differ. I use the json; the 072-G "textured v4" column matches it.

| # | criterion | verdict | evidence |
|---|---|---|---|
| 1 | SetPass Δ ≤ +4, not growing with textured rows | **pass (estimate)** | Every-face side against the 21:48 untextured capture: −1, +1, 0, 0, +1, +2, +2, +2. All of it is forward; dn and shadow are unchanged or −1. The ceiling is one detailed variant per DK shader, not one per row. The UnityStats confirmation is still owed. ("Summed over the 8 views" in 4.3 should read "per view"; the sum would be meaningless.) |
| 2 | batches / draw calls ≤ +1 %; materials, tris, casters Δ 0 | **materials, tris, casters pass; batches not measurable yet** | Main tris +0, materials +0 and casters +0 at all 8 views. The estimator has no batch or draw-call count, so that waits for UnityStats. |
| 3 | SRP batcher compatible, UnityPerMaterial unchanged | **pass by code review; Inspector report not captured** | FacadeLit adds only a `[NoScaleOffset]` 2DArray. `_SubornedSurfaceDetailOff` is a global outside every CBUFFER and not in Properties. The DK cutaways keep URP's LitInput CBUFFER. Latch should still paste the three shaders' "SRP Batcher: compatible" lines. |
| 4 | new set ≤ 24 MiB, total texture Δ ≤ 24 MiB | **pass on GPU size; player snapshot owed** | GPU +16.00 MiB (7 textures). The Editor runtime reads +32.01, but the whole scene also reads 2× in the Editor (158 → 317 MiB). All seven are `isReadable: 0`, so the player holds the GPU copy only. |
| 5 | area-weighted fetches per pixel Δ ≤ +1.0 | **pass (computed by me; Latch's per-shader list still owed)** | 26.07 % flat+detail × 3 (mask, albedo, normal) + 6.16 % FacadeLit × 1 = **+0.84**. That is an upper bound, because FacadeLit's glass and neon take no sample. |
| 6 | Studio GPU ms Δ ≤ +3 %, interleaved | **not measurable yet** | The batchmode ms are noise. The same textured v4 scene read 5.63–8.03 at 22:53 and 5.14–7.38 in the 072-G run, up to 2.9 ms apart on identical content. A development player with FrameTimingManager is needed. |
| 7 | Air: textured − flat ≤ +0.15 ms; every + texture ≤ +0.3 until A3 is measured | **not measurable yet** | This is the one sitting (section 4). |
| 8 | no seam (D1) | **fixed in code; crop owed** | FacadeSurface.hlsl takes `ddx/ddy(uv)` outside the branch and samples with `SAMPLE_TEXTURE2D_ARRAY_GRAD` scaled per lane. DK_SurfaceDetail branches on a uniform, so its implicit derivatives are safe. Glint or Suture still owe the 2× band-edge crop. |

- **The switch differs from my spec, and it is better.** I specified keyword off plus a neutral array. Latch built a uniform branch on a global float, so both sides draw the same materials and variants, and nothing is toggled at runtime that would itself perturb the SetPass count.
- **What it measures:** the detail fetches and their ALU. It does not measure the detailed variants' SetPass (+0–2) or the TBN interpolators on rows without `_NORMALMAP`, because both sides carry them. Read textured − flat as a slight under-estimate, and label it so.
- **D4** (detail stretch on pilasters and ShopFill scaled to 1.7× in x) is still a look call for Vela and Glint.

## 3. Projected warm Air mean at the opening (cell-open-30), 1920 × 1080, estimate

| step | Δ (ms) | warm mean |
|---|---:|---|
| T0, today (064's 1080 rescale, never measured warm) | | 12.7–14.2 (13.45) |
| + every face (phase 2) | +0.04 to +0.21 | 12.74–14.41 (13.55) |
| + holed L2 copies | +0.00 | same |
| + 072-F (shares as predicted: 26.1 % against 25.3 %; SetPass +0–2 against ≤ +4) | +0.06 to +0.24 | 12.80–14.65 (13.68) |
| + 072-G palette and grade (colour-grading LUT values only; same post passes, textures, materials) | 0 | 12.80–14.65 (13.68) |
| − 064 A3 no-look items 1, 5, 7 (item 4 is already in: PC_RPAsset `m_RequireOpaqueTexture: 0`) | −0.9 to −1.8 | **11.0–13.7 (12.3)** |

- **Against 13.0:**
  - **without A3:** 13.7, over;
  - **with items 1, 5, 7:** central 12.3 passes with ~0.7 ms in hand, but the worst corner is 0.7 over;
  - **p99:** 12.3 × 1.20–1.41 gives 14.8–17.3, against 16.7, borderline.
- **What A3 still has to deliver:**
  - **none of items 1, 5, 7 is in the code yet:** TacticalShadows has no SSAO toggle, PC_RPAsset still has `m_RequireDepthTexture: 1`, and HostFightLab still stacks the signals overlay camera;
  - **for the mean:** ≥ 0.7 ms at central T0, ≥ 1.65 ms in the worst corner. Items 1, 5, 7 give 0.9–1.8;
  - **for the worst corner and for p99 (mean ≤ ~11.9 at ×1.41):** Glint's look items as well, 3 soft shadows Medium and 6 bloom quarter (−0.4 to −1.2 at 1080).
- **The allowance after A3:** 13.0 minus the measured mean, capped at 1.5. The fight (30 combatants), night and p99 draw on it.

## 4. The one Air sitting: precondition and what it measures
**Precondition (Sky, "yeah wiat"):** ask for it only once **one development player** holds all of the following:
1. 064 A3's no-look items **1** (SSAO off for the ortho play camera), **5** (depth texture by request, plus the sun flare disabled at play) and **7** (signals overlay as a pass);
2. every face with the holed L2 copies (a5234fdb);
3. 072-F with `surfaceDetail` (79283d9f);
4. 072-G;
5. and the Studio CPU check (phase 2 F5: CPU main + render Δ ≤ 0.5 ms, today against every) has passed.

Items 2–4 are in now; items 1 and 5 are not.

**Files updated** (the specs only; nothing run):
- **`perf-072.json`:** variants with `surfaceDetail`:
  - `today` (everyFace off, detail off);
  - `every-flat` (on, proxy, detail off);
  - `every` (on, proxy, detail on: as shipped);
  - `every-ssao` (as shipped, ssao `depth`);
  - `every-l0cast` (kept, but in no pass).
  - ab072 = 6 stops × [today, every-flat, every, every-ssao] × 3 rotated repeats + A…A. capped072 = today against every.
- **`tplong-072.json`:** every row carries `surfaceDetail`. Eight rows: play-opening today / every / flat, tp-street every / today, tp-worst flat / today / every.
- **`run-air-072.sh`:**
  - header: the precondition and what is measured;
  - **two guards against JsonUtility's silent drop:** it refuses a resolved plan with fewer than 4 variants carrying `surfaceDetail`, or tplong with fewer than 8 rows carrying it; after the perf pass, it stops if the player's own result has no `"surfaceDetail": "off"`, because then the player predates the switch;
  - it logs `ssaoAsBuilt`.
  - `zsh -n` clean.

**What the sitting measures** (~40 min plus the cold gate; warm, uncapped, 1920 × 1080, that player):
1. **The as-shipped warm mean and p99** (`every`) at cell-open-30, against 13.0 / 16.7. This is the first measured warm 1080 number; it replaces T0 and the A3 estimate in one figure.
2. **Every face's price:** every-flat − today, at 6 stops and at tp-street / tp-worst (where the holed L2s draw). Rule: ≤ +0.3 ms until A3 is measured.
3. **072-F's fetch price:** every − every-flat, at the same stops and at play-opening / tp-worst. Rule: ≤ +0.15 ms. It slightly under-reads, as section 2 says.
4. **A3 item 1's saving:** every-ssao − every. This is valid only if Latch's item-1 code lets PerfSwitches' `ssao` override the per-camera toggle. Otherwise drop `every-ssao` before PerfBuild resolves the plan; items 5 and 7 have no switch and stay estimates.
5. **Missed vblanks past 25 ms** at the opening as shipped (capped072).
- **It does not measure:** T0 without A3 (no switch for items 5 and 7), the pre-072 scene (`today` keeps 072-G's colours and the RecordsTower, F7), fights, night.

## Open issues
- **Latch:**
  - refresh `textured/report.md` from its 22:53 json;
  - capture UnityStats (SetPass, batches, draw calls) for today / every-flat / every in the Game view;
  - paste the SRP batcher Inspector lines;
  - list fetches per shader (criterion 5).
- **A Memory Profiler snapshot of the development player:** texture Δ and the column and holed-L2 mesh memory (F3 fixed in 79283d9f; holes add mesh data).
- **The `every-ssao` variant** depends on how item 1 is built (Latch).
- **The unknown-key guard** lives only in my script. A PerfBuild / TpLongRun check that rejects unknown variant keys would close it at the source (Latch, low).
- **T0** is still the largest error bar, ±0.75 ms. Only the sitting closes it.
