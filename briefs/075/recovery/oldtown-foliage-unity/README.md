# 075 · Unity trial: original OldTown FlowerBed and ShrubMound in the café troughs

Latch (Opus 5.5), 2026-10-29. One bounded review trial, default-off. No production enablement, player build, Air run, paid call or commit. Direction: Vela's `../opus-oldtown-foliage/README.md` and `placements.json`.

**Recommendation: HOLD.** Technically the trial is clean: fit, original bindings, scope, alpha stability and SRP Batcher all pass. As a look, though, it is a step down from the current domes at play zoom. The FlowerBed in B is the only part worth Sky's look as a candidate.

## What changed (paths)

- **New:** `unity/Assets/Suborned/Editor/World/Review/OldTownFoliage075.cs` (+ Unity `.meta`). This is the only source file I touched. It has four parts:
  - `Audit`: material, shader and texture audit, read-only.
  - `Apply`: refuses without `-oldtownFoliage trial`; no builder calls it.
  - `Census`: placement counts, plus an SRP Batcher query after a real render.
  - fit checks.
- **Generated, review only (untracked, like `Generated/Review075/`):**
  - `unity/Assets/Suborned/Generated/OldTownFoliage075/`: six batch-mesh copies with the two troughs' baked plant triangles removed, 164 KB.
  - `unity/Assets/Scenes/probe-oldtown-foliage075.unity`: the trial scene, git-ignored.
- **Evidence:** this folder. The exact steps are in `run.sh`. The 12 MB manifests and the logs stay in `out/scratch/latch/oldtown-foliage-unity/` (`KEEP`).
- **Not touched:** baseline `probe-oldtown-ours.unity` (sha `3d058e6f…` before and after), Recovery075, Swap, Kit, catalogue, normal builder, restyle JSON, plan, world, walk, sim, nav, colliders, tram scenes and evidence, and Sol's custom foliage (still default-off).
  - `inputs-before.json` → `inputs-after.json`: 2,341 pinned files, **0 changed**.

## What the trial does

- **Baseline:** the pinned civic-paving scene. It is opened and saved under a new name, never written.
- **Removal:** in that scene both troughs are baked into the shared civic batches. `Apply` removes only their leaf and bloom triangles, by exact world-triangle match, and refuses a partial match:
  - B: 612/612 removed
  - A: 672/672 removed
  - Slot 0 stone (60 tris each, `DK_Limestone_Tex` restyle) stays verbatim in its batch.
- **Instances:** each plant is a prefab instance of the source FBX, so it uses the FBX's own shared, immutable mesh. Instance renderers sit under `<placement>/oldtown-foliage075/`.
  - Uniform scale and quarter-turn yaw only.
  - No collider and no LODGroup, so the full mesh is what draws.
  - Layer 0, rendering-layer mask 1 (cutaway bit 20 off), and probes as the batch they replace.

| Trough | Source (sha256) | Instances (world centre x, y, z · Unity yaw) | Tris |
|---|---|---|---|
| `run-west-cafe/1` (B) | `DK_FlowerBed.fbx` 77dfd48e… | FlowerBed_0 (-179.5, 0.835, -223.5) · 90°; FlowerBed_1 (-179.5, 0.835, -221.5) · 90°; scale 1, z 0.655 (fallback 0.687 **not** needed) | 2 × 2,718 |
| `run-west-cafe/0` (A) | `DK_ShrubMound.fbx` 857103fb… | ShrubMound_0…5 at world z -220.05/-219.43/-218.81/-218.19/-217.57/-216.95, y 0.825, Unity yaw 90/270/0/180/270/90 (Blender 0/180/90/270/180/0); scale 0.64 | 6 × 2,526 |

Exact poses are in `apply.json` → `instances`. The yaw is within 0.0002° of the exact quarter turn.

**Materials:** each slot is bound explicitly by GUID, and each GUID is checked to resolve to the OldTown path. The importer's own defaults are the same GUIDs.

| Material | GUID |
|---|---|
| `DK_Leaf_CoreShrub` | de6d3a36… (opaque, cull back) |
| `DK_Leaf_Shrub` | 00b375d6… (`_ALPHATEST_ON`, cutoff 0.5, cull off) |
| `DK_Flower_Coral` | 65f10738… (same as `DK_Leaf_Shrub`) |

- **Shader:** `OldTown/DecoKit/Lit Cutaway` (92902edf…).
  - Supported, no shader messages, and the ShadowCaster pass is enabled.
  - SRP Batcher code 0 (OK), queried after a render framed on the troughs (`census-trial.json`). Before anything drew, the batch query only said "Not initialized".
- **Textures:** byte-identical to the Kit twins (same sha256). Trilinear, aniso 8, mips with coverage preserved, sRGB, CompressedHQ.
- **Fallback:** the current-twin fallback was **not** used.

**One correction to placements.json:** the importer maps Blender to Unity as (−x, z, −y), not "(-x,z,y)". I measured this on both models: it is the unique signed permutation matching the native bounds to 0.06 mm. It agrees with `sides.py` for the kit. Every instance sits at local y 0 and the coping is symmetric, so no pose changes.

## Fit (`apply.json` → `fit`): both PASS

- **Measured trough:**
  - coping |y| ≤ 0.340 for z 0.600 to 0.655
  - body |y| ≤ 0.315 below the coping
  - |x| ≤ 2.0
- **Current renderer envelope:**
  - B: top 1.0616, x −2.138…2.164
  - A: top 1.1201, x −2.189…2.180
- **B, FlowerBed:**
  - Plants span z 0.568 to 1.029 (top ≤ 1.0616).
  - The world bounds sit inside the current envelope to 1 mm.
  - 26 droop vertices fall below the coping, and all are inside the 0.315 body. That is why the 0.687 fallback was not triggered.
  - 28 triangles lie exactly on the coping top. They are the opaque core's back-culled, downward-facing bottom cap (0.74 m²), so they can't z-fight from the camera.
- **A, ShrubMound:**
  - Plants span z 0.606 to 1.111 (top ≤ 1.1201).
  - Inside the envelope, with nothing below the coping.
- **Visual check:** the y030 street-side crop shows no leaf through the body (`pairs/trial-flowerbed-streetside-y030-2x.png`).

## Scope preservation (`comparison.json`, PASS)

- **Renderers:** +8 renderers, all under `oldtown-foliage075`. No renderer removed.
  - Only the six expected civic plant batches changed: Coral, Cream, Flamingo, Peach, Leaf_Civic, Leaf_CivicDark at `@-3,-4#-12,-14`.
  - Their materials and non-foliage fingerprints are unchanged.
- **Transforms:** +10, all under the group. None changed or removed.
- **Colliders and nav:** colliders identical; nav hash identical (50e591f9…).
- **Baseline match:** the baseline still matches Latch's earlier `paving.json` exactly (renderers, transforms, colliders, nav).
- **Restyle manifest:** it differs only in those six plant batches and the eight added kit entries. `DK_Limestone_Tex` and every other material are identical.
- **Sight and HOLD:** untouched (render-only, no sim, walk or sight input).

## Cost (measured counts; no timings)

| | |
|---|---:|
| Triangles | +19,308 (26,623,507 → 26,642,815; LOD0 is the only level) |
| Renderers | +8 |
| Material slots / submesh draws | +18 (2 × 3 + 6 × 2) |
| Shadow casters | +8 |
| Alpha-clipped renderers | +8 |

Vela's "10 → 20 draws" assumed the trough is its own renderer. Here the current planting is baked into shared batches, so no draws are removed and the trial adds 18.

**Not measured:**
- draw calls, SetPass calls, overdraw and frame time
- player shader stripping of the OldTown fork

Studio timing was skipped: an unrelated Unity 6000.6.5f1 job from another project was running during this round, and an editor batch render isn't a play frame. No Air run and no 60 fps claim.

## Captures (all from the same Glint075 play path, same build, grade, probes and cameras; people hidden)

- `baseline/` and `trial/`:
  - café square and office front at zoom 14 / 38°, yaw 30 and 210, day and night
  - trough detail at yaw 210 (the earlier framing)
  - trough pair at yaw 30 and 210
- `pairs/`:
  - `full-*` side-by-side frames
  - `play-y030/y210-day/night.png`: play-zoom crops, 3× nearest
  - the street-side crop
  - the motion strip
- `trial-alpha-motion-y210.mp4` (+ gif): 24 frames at play zoom, panning 1 cm per frame (sub-pixel).
  - `alpha-motion-stats.txt`: after re-alignment, the per-frame residual is FlowerBed 4.8, ShrubMound 3.4 and paving/stone 2.2–2.4 (8-bit).
  - Each maximum is within 0.6 of its mean, with no frame bursts. Leaf edges stay stable; the higher figure is the real leaf detail resampling.
- **Shadows:** cut-out clusters, not card rectangles.

## What I see (inspected at full size)

- **Mass drops at play zoom.** The current domes read as a full planted hedge up to about 1.1 m. In the trial:
  - **A** reads as six separate small olive balls, with cream coping showing between each pair. It is a dotted line, not a hedge. At 0.64 the mounds' leafy silhouette is narrower than their bounds, so the "they touch" estimate doesn't hold on screen.
  - **B** reads as a dense but low, flat carpet. It is the better of the two, and its density matches the fountain-garden beds in the same frame.
- **Colour.** Under the play grade the coral bloom reads **burnt orange / rust**, not coral-pink, and it sits close to the asphalt's warm brown. The OldTown leaf reads olive and yellow-green. Both are duller than the café terrace's own hedge, the topiary and the fountain beds next to it, so the trough looks like it comes from a different kit.
- **Detail.** The leaf texture is richer, but at 40–60 px scale it reads as speckle. Against "readable > rich" it doesn't clearly win.
- **Night.** Both plants go near-black against the teal night. The current domes keep some green and their bloom dots.

## Recommendation and Sky's choices

**HOLD: don't adopt this trial as it stands.** If Sky likes the old-town texture:
1. **B only: FlowerBed at scale 1 in `run-west-cafe/1`.** This is literally the old café-edge recipe and the strongest read. A stays on the current domes. This needs Sky's call on the rust-orange coral against the flamingo/peach blooms.
2. **A: ShrubMound at 0.64 doesn't work.** It is too gappy. The only literal fix is full-size ShrubMound, which overhangs the coping onto the walk and kerb. That needs a clearance and sight waiver (Keel and Vantage) while the HOLD stands, so it is not authorised here.
3. **Otherwise keep the current domes.** No palms, topiary, cypress or tram work was touched.

## Next owner

- **Redline:** an independent visual and code review. Inputs are this folder, `OldTownFoliage075.cs` and `run.sh`. Done-check: the fit, scope and binding claims reproduce.
- **Then Sky,** via Aster: the colour and mass choice from the `pairs/` and `full-*` captures.
- **If anything is adopted:** a separate authorised round adds the frame-time and draw-call measurement and the player stripping check on the Air.
