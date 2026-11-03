# 074-D · Redline · building colourways: draws, CPU, code review, the Air rows (2026-10-28)

**Inputs:**
- the export `out/scratch/latch/074/pin/074.world.json`, sha256 67a3a07b…c6d4. It is byte-identical to `out/world/harbour-district.world.json`: 17,827 kit facades, every one with a colourway; 12 families, no `civic` facade;
- `contracts/world/kit.json` `colourways` (5 recoloured base slots, 60 tinted materials);
- Latch's `briefs/074/latch/{counts.md,counts.json,after/census.json}`;
- commit 0c39f98c: KitColourways.cs, KitColumns.cs, FacadeKit.cs, ProcGrammar.cs, ShadowProxies.cs;
- the scene `unity/Assets/Scenes/HarbourDistrict.unity` (11:00 build).

**Re-run** (3 s, read-only): `python3 -I briefs/074/redline/colourway_draws.py out/scratch/latch/074/pin/074.world.json contracts/world/kit.json briefs/074/redline/colourway_draws.json [slots]`. The what-if rows are in `briefs/074/redline/whatif.jsonl`.

**Everything in §1–§2 is computed.** None of it is UnityStats. The model is phase 3 §5's: under the GPU Resident Drawer, one instanced draw per distinct (mesh variant [+LOD], submesh material) in view. PC_RPAsset has `m_GPUResidentDrawerMode: 1`, static batching is off and BRG stripping is Keep All, so this is what ships. It changes from phase 3 in three ways:
- the real export colourways replace my stand-in hash;
- Vela's five recoloured slots replace my three: wall `DK_Limestone_Tex` and `DK_Render_White`, band `DK_Enamel_Seafoam`, fascia `DK_Enamel_Teal`, plinth `DK_Stone_Dark`;
- the 924 non-module street pieces are counted too, because Latch swaps them.

**Why Latch's +0 does not cover this:** Latch's offscreen counts run with the GPU Resident Drawer **off** (RenderSession). With it off, renderers are unchanged and the SRP batcher batches by shader variant, so the colourways really are about +0 there. The draw split only appears with the drawer on, which is the shipped path.

## Verdict against the pass mark

| mark | result |
|---|---|
| no new SetPass | **PASS.** Verified, not only estimated (§3) |
| play zoom 14: batches ≤ +40 | **PASS at the 32 phase-3 views** (max **+23**, heaviest@210; the Handover views +0 to +12). **FAIL at Latch's new district street:** the wide-lamplight focus at zoom 14 gives **+53 to +70** |
| street level: module draws ≤ +260 | **tp-street +320: FAIL.** **tp-worst +252: PASS**, just. The street pieces add +31 / +122 on top: **+351 / +374** kit draws in all |

**Frame time:** frame-neutral at play. At street level it is a render-thread cost on a frame that is GPU-bound and already over budget (§2). The pass mark is a count rule, and it fails at two places; the frame cost behind those counts is small. That is for Sky to weigh via Aster: I propose, I don't decide.

## 1. Draws: today (every facade `base`) against the colourways (GPU Resident Drawer on)

| view | kit roots | buildings | module draws | street-piece draws | all kit draws | kit materials |
|---|---:|---:|---|---|---|---|
| district (all) | 5,578 | 319 | 801 → 2,149 | 607 → 1,300 | 1,408 → 3,449 | 26 → 81 |
| **tp-street** | 633 | 47 | 414 → 734 (**+320**) | 374 → 405 (+31) | 788 → 1,139 (**+351**) | 25 → 74 |
| **tp-worst** | 703 | 48 | 340 → 592 (**+252**) | 507 → 629 (+122) | 847 → 1,221 (**+374**) | 26 → 77 |
| office-front @30 / @210, zoom 14 | 8 / 1 | 2 / 1 | +0 / +0 | +0 / +0 | **+0 / +0** | |
| cell-open @30 / @210, zoom 14 | 22 / 17 | 3 / 4 | +4 / +12 | +0 / +0 | **+4 / +12** | |
| cafe-square, worst yaw, zoom 14 | 21 | 4 | +6 | +0 | +6 | |
| heaviest @210, zoom 14 (phase 3's worst) | 58 | 7 | +12 | +11 | **+23** | 21 → 44 |
| **wide-lamplight @30, zoom 14** | 62 | 9 | 98 → 156 (+58) | +12 | 192 → 262 (**+70**) | 21 → 47 |
| wide-lamplight @300, zoom 14 | 64 | 8 | +53 | +12 | +65 | |
| wide-lamplight @30, zoom 18.5 (Latch's wide view) | 98 | 12 | 98 → 194 (+96) | +16 | +112 | |

The full table (9 foci × 4 yaws at zoom 14 and at 18.5, plus the ground rows) is in `colourway_draws.json`.

- **Phase 3 under-called this.** I modelled three slots, a stand-in family pick that let neighbours share a family, and modules only. I also wrote that a zoom-14 frame holds at most 7 module buildings: wide-lamplight holds 9, with 62 roots. With the real export and three slots, tp-street would be +263 module draws, where phase 3 predicted +256, so the model agrees. The overage over phase 3 is:
  - the plinth slot: +57 module draws at tp-street;
  - the street pieces: +31;
  - a focus my 32 views did not include.
- **Materials:** 60 tinted (scene 289 → 349; module 14 → 58). That is over phase 3 §5.3's cap of ≤ 36 tinted / 47 module materials, because of the plinth and render-white slots. The cap was a means to the draw marks, so the draw marks above are what counts.

**What-if** (same export, fewer recoloured slots; `whatif.jsonl`). It shows where the draws come from. It is not a look recommendation:

| slots recoloured | tp-street module / all | tp-worst module / all | worst play-14 view | district materials |
|---|---|---|---|---:|
| all 5 (as built) | +320 / +351 | +252 / +374 | +70 | 81 |
| no plinth (wall ×2, band, fascia) | +263 / +286 | +214 / +304 | +55 | 70 |
| wall ×2 + plinth (band and fascia stay seafoam / teal) | +153 / +169 | +114 / +180 | +34 | 59 |
| wall ×2 only | +96 / +104 | +76 / +110 | +19 | 48 |

- Only "wall + plinth" and "wall only" meet every mark, wide-lamplight included.
- Dropping the plinth alone brings tp-street to the line (+263 against +260), but the play mark still fails at wide-lamplight (+55).

## 2. CPU and render thread

Per extra instanced draw: ~1.5–3 µs render thread and ≤ 0.5 µs GPU (phase 3's rate; estimate).

- **Street level** (tp-street +351, tp-worst +374 kit draws):
  - **render thread: +0.5–1.1 ms**;
  - main thread ≤ +0.1 ms: the drawer culls per instance, which is unchanged; only its draw-command output grows;
  - GPU ≤ +0.1–0.2 ms (command processor and CBUFFER binds; per pixel 0).
- **Where that lands:** the ground view is already over budget. The Air estimate for tp-worst is 14.1–15.7 ms, GPU-bound, with the render thread ~3.2 → 4.6 ms after every face (072 plan §4). The colourways take it to **~5.1–5.7 ms**. That crosses 072 §4.3's 5 ms render-thread line, the trigger for the 32 m cell HLOD.
  - While the frame stays GPU-bound (render thread well under the ~14 ms GPU frame), the frame moves by about the GPU share only: **+0.1–0.2 ms**.
  - It does not make the ground view's problem worse in kind. Brief 048's geometry items or render scale still decide it.
- **Play zoom:** at most +70 draws (wide-lamplight) gives ≤ +0.2 ms render thread and ≤ +0.03 ms GPU in a GPU-bound frame. Frame-neutral (A5 ≤ +0.3).
  - At the Handover views (+0 to +12): ≤ +0.04 ms render thread. Nothing measurable.
- **Load:** +60 FadeReach cutting twins at play start (see F2): well under 10 ms, once.

## 3. Code review (commit 0c39f98c): SRP batcher, instancing, runtime materials

**OK, verified:**
- **No new SetPass, by construction and on disk.**
  - `KitColourways.Load` rejects a tint whose shader or keyword set differs from its base.
  - I diffed all 60 `Kit/Materials/*__*.mat` against their base. Each differs only in `m_Name`, `_BaseColor` and `_Color`: the same shader GUID, keywords, textures (the same albedo, normal and mask GUIDs: no tinted textures, so no texture memory), render queue (-1), `disabledShaderPasses`, `stringTagMap` and `m_EnableInstancingVariants` (0). Under the drawer the last one is irrelevant, because it draws through the DOTS variant (BRG Keep All).
  - UnityPerMaterial layout is unchanged, so SRP batcher compatibility is unchanged.
- **No material instance from the colourways, at build or at runtime.**
  - KitColourways lives in the Editor assembly. It loads `.mat` assets from `Look.KitMaterials` and assigns `sharedMaterials` only. There is no `new Material`, no `.material` / `.materials` access and no MaterialPropertyBlock (an MPB would also take the renderer off the drawer).
  - The runtime has no colourway code.
- **Prefab overrides per instance.**
  - `Apply` writes `sharedMaterials` and calls `RecordPrefabInstancePropertyModifications` per changed renderer.
  - The scene shows per-slot overrides (`'m_Materials.Array.data[3]'` → tinted GUID; e.g. DK_Limestone_Tex__peach is referenced 1,161 times), not whole-array copies.
  - A player build flattens prefab instances, so this costs scene size and editor load only (the scene is 653 MB, generated and git-ignored).
- **Material arrays per LOD.**
  - `Apply` walks `GetComponentsInChildren<MeshRenderer>(true)`, so every LOD child is tinted alike and there is no colour pop at a LOD switch.
  - The build log reads 16,682 renderers on 5,578 instances (~3 levels each), 55,253 slots.
  - Ground-view L1/L2 draws split by colourway too. They are included in §1.
- **Columns baked once per layout.** The key is `"column"` plus the layout, with no colourway. Census: 165 layouts and 495 meshes before and after, triangles unchanged. `Bakeable` still refuses a column whose modules disagree, and Splice's CW check makes module equal building.
- **Shadow proxies on base materials.**
  - `ShadowProxies.Build` runs after the colourway pass (HarbourWorldBuilder line ~164) and maps each slot back with `KitColourways.Base`.
  - Census: 29 proxy materials before and after, so shadow draws do not split.
- **The FacadeKit and ProcGrammar changes** are editor-time and deterministic: the draw is still spent, so roof and enamel picks are unchanged. A mass that disagrees with its building's colourway is rejected (CW-1). There is no frame cost: Latch's renderer counts are unchanged in every view, with +42 to +162 triangles.

**Findings:**
- **F1 · Medium (perf, the pass mark):** the slot set is wider than costed. The plinth slot and the street pieces put tp-street over the +260 mark, and the play mark fails at wide-lamplight (§1). This is a scope and look question, not a code defect. Options are below (next owners).
- **F2 · Low (runtime materials, pre-existing):** `FadeReach` (Runtime) builds a cutting twin (`new Material(m)` + `_DK_CUT`) per distinct material of every fade-tagged renderer at play start.
  - The colourways raise that set by up to 60, so ~60 more runtime materials, created once. That is not a per-frame cost, and twins share one cut variant per shader, so no SetPass.
  - It is the only runtime material creation that touches the colourways. It is by design, not a colourway instance.
  - **Gotcha for any runtime colourway switch:** FadeReach snapshots `sharedMaterials` in its constructor and writes them back on un-reach. A switch that swaps materials after FadeReach is built gets reverted bay by bay. It must run before FadeReach is constructed (see §4).
- **F3 · Low (robustness):** `KitColourways.Base` relies on the static `toBase` filled by `Load` in the same build. Any future caller of `ShadowProxies.Build` without a prior `Load` (a review tool, a re-proxy after a domain reload) would silently give proxies tinted materials and split the shadow draws. A one-line guard would close it: in `Build`, throw when a slot's name contains `__` and `!KitColourways.Loaded` (Latch).
- **F4 · Low:** `Load` checks the shader and keywords, but not the render queue, `disabledShaderPasses` or the double-sided / cull state. Today all three are equal on disk, but a later Vela regeneration could drift one and add a SetPass or a sort split without failing. Add those comparisons, or compare every property except `_BaseColor` / `_Color` (Latch, low).
- **F5 · Note:** with the shadow proxies switched off (a perf-harness variant), `ShadowProxy.Use(false)` puts the pieces' own casting back, and the pieces carry tinted materials. In that variant only, the shadow draws split by colourway too. Don't read a proxies-off row as the colourways' shadow price.

## 4. What the Air sitting should add

The sitting stays unscheduled (brief 074, Sky's precondition in 072 phase 3 §4). When it runs:
- **The cheap switch, if Latch wants a variant rather than a second player:** a development-build PerfSwitches key `colourways: family | base`.
  - The builder stores the 60 tinted → base pairs on the world root (the base materials are already in the build through the proxies).
  - The harness swaps every kit renderer's `sharedMaterials` back **before FadeReach is constructed** (F2), once, before warm-up: ~16.7 k renderer assignments, tens of ms.
  - `base` must reproduce today's draw count. Check it with the counters below.
  - It's a dev-only harness swap, not a material instance. With it off (the default), every run is byte-identical.
- **Rows:**
  - ab072's cell-open@30 stop × [family, base];
  - **wide-lamplight@30 at zoom 14** (the new worst play view) × [family, base];
  - tplong **tp-street and tp-worst** × [family, base];
  - 3 rotated repeats, as ab072.
- **Read in the player, no UnityStats needed:** the perf harness already reads every Render-category ProfilerRecorder counter, so take `SetPass Calls Count`, `Draw Calls Count` and `Batches Count` with FrameTiming GPU / main / render thread.
- **Rules:**
  - SetPass Δ 0;
  - play-zoom batches Δ ≤ +40, and warm-mean Δ ≤ +0.1 ms;
  - street level module draws Δ ≤ +260 and render thread Δ ≤ +1.0 ms;
  - if the render thread passes 5 ms at tp-worst, that is the 32 m cell HLOD trigger (072 §4.3), whatever the colourways' share.
- **Before the Air, on the Studio in batchmode:** Latch's After072.C074 could wrap its offscreen `Camera.Render` with those three ProfilerRecorder counters with the **drawer on**, for the five views plus tp-street and tp-worst. That turns §1's model into measured counts without a visible editor, if the Render counters record under offscreen Camera.Render, which Latch should check.

## Open issues
- **F1 is a look and scope call (Sky via Aster, with Vela).** Either:
  - accept the ground-view overage as priced (render thread +0.5–1.1 ms on a GPU-bound frame; frame about +0.1–0.2 ms), or
  - narrow the slots: "wall + plinth" meets every mark; "no plinth" reaches the street mark but not wide-lamplight.
- **The play-mark miss at wide-lamplight** is frame-neutral. If Aster prefers, the mark could be restated as ≤ +0.3 ms warm instead of a batch count. That's for Aster and Sky.
- **Measured counts** (drawer on, ProfilerRecorder) replace §1. This is Latch's when the Studio is free, or the Air sitting.
- **F3 and F4:** small guards in Latch's builder.
- My phase 3 §5 tables under-read (3 slots, modules only, 32 views). This file supersedes them for 074.

**Next owner:** Aster, for the F1 call. Inputs: this file, `colourway_draws.json`, `whatif.jsonl`. Done-check: Sky's pick recorded in brief 074; if the slots narrow, Vela regenerates the mapping, Latch rebuilds, and I re-run `colourway_draws.py` (3 s) on the new export.
