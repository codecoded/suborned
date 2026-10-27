# 069 · Character pipeline: the stage log (2026-10-26 to 27)

The record of the proof so far: what was tried, what was measured, what Sky decided. Brief: `briefs/069-character-pipeline.md`. This folder keeps the notes and numbers (committed). The pictures, videos and big HTML reports stay on disk under `out/scratch/*/char-pipeline/` (each folder has a `KEEP`) and in `paid/069-intake/`.

## Sky's decisions, in order
1. 2026-10-26: "forget existing animations - this is all fresh stuff so nothing from previous carries over"; start with walk and run.
2. "whole point is a super clean pipeline" · "shouldn't need to be adjusting and fine tuning if we can help it": stages, data not special cases, correct by construction, checks that fail loudly.
3. "the first thing should be signing off the models / clothes pipeline so I can see all the combinations": stage by stage, Sky signs off each stage.
4. On the rule-made wardrobe: "look awful third person - and lose any real appeal to how we do things now." The procedural wardrobe is shelved, and the look stays generated from concept art.
5. "I still want to be able to easily generate unique characters without worrying about this every time ... we can fix up every figure if it's cheap and easy in meantime" · "a very precise prompt for wrapping" · "a very dynamic and diverse character building pipeline that can have great animations".
6. 2026-10-27: yes to the end-to-end test. 3–4 meshes from the intake stills at about $2.70 (under $5.40 with retries), through the hybrid, into Unity.

## What is proven (measured, scratch only)
| | Evidence |
|---|---|
| MPFB base bodies are licence-clean (GPL add-on at build time; body, proxies, rig and weights CC0; 147 asset files checked) | `forma/licence-and-candidates.md` |
| Byte-identical rebuilds (157/157, then 263 files) | `forma/determinism.json` |
| One 52-bone Humanoid skeleton with fingers: 50 figures imported with zero per-figure work and zero warnings | `latch/import-report.json` |
| Frame cost: +8 % per person with finger bones; the 22-bone crowd variant comes in about 4 % under today's crowd; one material instead of 37 doesn't cut draw calls for skinned crowds | `redline/cost.md` |
| **The left-foot and bow-leg fault** (on today's figures and new ones alike) comes from Unity building each clip's and each figure's Humanoid reference from its own first frame or rest. Fix: one shared neutral-leg reference avatar for every clip, and neutral legs written into each figure's meta. The planted foot comes within 1° of the source, and hip abduction drops from 6–9° to 0.3–2°. Feet spacing and leg twist settings have no effect. | `latch/leftfoot.md`, `latch/leftfoot.json`, `latch/bowleg.json` |
| The intake prompt (a precise A-pose sheet) plus an automatic pose check: 7/8 Codex stills pass in round 2; Sky's art-bible commuter passed with the style board attached | `intake/`, `tools/figures/charpipe/intake*.{json,py}` |
| The fit test ($0, five of today's source meshes): A (keep the generated mesh, weights from the fitted base) holds through seven clips once unmatched shells take their nearest body weights. G (garments bound to the base) follows every slider and every head size. | `fit-test/`, `tools/figures/charpipe/fit/verdicts.json` |

## What failed or is open
- **The rule-made wardrobe:** failed on look (Glyph `glyph/read.md`; Sky). Shelved.
- **Capes:** fail in both approaches (they web in crouch and sit; arms pass through). They need cloth or a cape rig; keep capes short until then.
- **Heavy bodies:** arm–torso collisions. An arm-out layer at 28° tied to the weight slider removes 56–85 %. Thigh contact needs corrective shapes.
- **Armpit stretch:** p99 is over threshold everywhere going from rest to arms-down. The intake pose should reduce it, and the threshold should be read per body region.
- **Not measured yet:** hat swaps between different people; aim grip (fingers splayed); fresh walk and run in Unity (under 1 % slide in Blender); A/G frame cost; the carried pose missing for A in the fit-test sheet (unexplained).
- **The animation fix is not in the game yet.** It goes in `tools/figures/unitymeta.py` (clips) plus the figure metas, held until batch 0.2.123 lands; then Forma applies it to all 55 clips and every installed figure and Latch checks it in a build.
- **Arm angle:** the art bible's model-sheet type says arms about 30°; the intake uses 45°, the wrap target. One of the two is to be reconciled with Sky.

## The direction now: generate → fit (the hybrid)
Concept-led Codex still (the intake prompt, mannequin pose reference, plus the commuters style board) → image-to-3D, textured, not rigged → fit the MPFB base inside it (sliders, then joints) → A by default: the generated mesh keeps its look, takes the base skeleton and weights, and every unmatched shell takes its nearest body weights → G's pieces for what must vary (hats and hair, attachment points, skirts that must fit several bodies) → the standard rest with neutral legs → the shared neutral clip avatar → role rules and seed by id → LOD'd FBX and manifest rows.

## End-to-end test (2026-10-27)
Four intake stills (coat, dress, guard, Sky's flamingo commuter) became meshes with Hunyuan3D 3.1 Pro on Runware: $2.70 in total, within the $5.40 pass (ledger `paid/069-e2e/`). Sky's pick for future meshes is Tripo 3.1. The meshes were fitted by the hybrid. The static check (raw mesh beside the fitted figure; `out/scratch/forma/char-pipeline/e2e/models.png`, `shoulders.png`) now matches. Evidence: `e2e/`.
- **Our processing introduced three defects, and Forma fixed them:**
  - Specks: a step that deleted small loose pieces was removing real surface, because the meshes are hundreds of pieces. That step is now off.
  - The shoulder crease: sleeve and body took different weights. Weights near the armpit are now averaged over everything within 1.5 cm. The rest is now the A-pose the figure was generated in, and the T-pose avatar is a separate skeleton-only FBX.
  - A stray strap: props now go in their own FBX.
- **Determinism:** 31/31 deliverables are byte-identical. The `.blend` working files aren't deterministic (Blender's internal order) and moved to `work/`. Tangents are no longer exported, keys are sorted, and FBX paths are bare.
- **The intake pose helps** (`e2e/old_vs_new.json`, medians, old fit-test meshes → intake-pose meshes):

  | | old | new |
  |---|---|---|
  | hem self-intersections | 166 | 4.5 |
  | arm–torso intersections | 364 | 120 |
  | flipped triangles per clip | 203 | 138 |
  | armpit stretch p99 | 2.46 | 1.98 |

- **Hats:** the guard's cap cut cleanly into a separate rigid piece. The flamingo's pillbox shares shells with the hair, so it stays welded.
- **Open:** LOD0 is the full generated mesh (47–50k faces, about 4× today's 12k), and the reduction level is Sky's call with Redline's numbers. Arms raised to horizontal still leave a mild fold at the shoulder. Clips, metrics and Unity takes wait for Sky's OK on the static check.

## Where the rest is (on disk, not in git)
- Round 1 proof: `out/scratch/forma/char-pipeline/proof.html`; sign-off sheet: `signoff/signoff.html`.
- Unity frames and videos: `out/scratch/latch/char-pipeline/frames/` (incl. `video/leftfoot_unity*.mp4`, `bowleg_unity*.mp4`); the scratch Unity project at `out/scratch/latch/char-pipeline/unity/`.
- Web reference of the raw walk: `out/scratch/aster/foot069/` (`leftfoot_web.mp4`, `bowleg_web_front.mp4`, `web/walk_web.html`).
- Intake: `out/scratch/forma/char-pipeline/intake/intake.html`; stills: `paid/069-intake/codex/`.
- Fit test: `out/scratch/forma/char-pipeline/fit-test/fit-test.html` and `media/`.

## The Fixer redone (2026-10-27)
Sky asked to redo the Fixer from her concept (`Cell/picks/fixer_P4-6.png` in the store) through the intake prompt, to bring back the long dress, and to join her sleeves. **Sky's final pick: `paid/069-intake/codex/fixer_v7_cutout.png`** (Codex, $0). Each step is an edit of the one before. Sky's picks: set-in sleeves (v4_set) → "more aubergine": blocks (v5_blocks) → "combine" with the accents: take 2 (v6_combo_2) → the sleeves "lost that seductive look": cut-outs (v7_cutout).
- **The final design:**
  - ivory with aubergine blocks: the collar, shoulders, upper sleeves and side blades;
  - aubergine piping on the wrap edge, the hem, the split and the back seam, plus a waist band and cuffs;
  - the sleeves joined to the dress by an aubergine band over each shoulder, with a large pointed cut-out below it showing bare shoulder and upper arm.
- **The dress:** the concept's long asymmetric ivory wrap to mid-calf, with an aubergine pinstripe inner panel and a deep split at her left hip at the front. The back of the skirt is closed in every view.
- **The sleeves:** set into the dress, one garment from collar to wrist, with diagonal aubergine blades on each arm.
- **Kept from the concept:** the open back with its halter straps, the top knot and ponytail, and the block-heel ankle boots.
- **How it was made:** two passes. `fixer/prompt_v3_keyhole.txt` is the intake template with a hero exception for the long hem. `fixer/prompt_v4_set.txt` is an edit of that sheet. Regenerating from scratch kept the sleeves detached and put the back split on the wrong side; editing fixed both. `prompt_v5_blocks.txt`, `prompt_v6_combo.txt` and `prompt_v7_cutout.txt` are the later edits.
- **For the mesh and rig:**
  - The long hem is an art-bible exception (hems at the knee or above) for a hero, to be recorded at the next GDD change.
  - The skirt and ponytail need extra bones or cloth.
  - The pose check needs a long-skirt rule (measure from the visible ankles): the skirt hides the leg gap.
- **Next:** the Tripo 3.1 mesh (about $0.60) waits for Sky's OK.

### The Fixer's mesh (2026-10-27)
- **The mesh:** Tripo 3.1 multi-view (front, left side, back) via Runware, $0.60 in one call (`paid/069-fixer/`). 18,841 triangles and one 4K texture.
- **Cut-outs, open back and split:** painted on, not holes. The mesh is one closed surface, so it can't tear, but the split can't open in a stride; that waits for the skirt bones or cloth.
- **The pose check's long-skirt rule:** it measures the stance below the hem. The Fixer passes, and the wrong pose and a floor-length skirt still fail.
- **Fit:** same scripts plus one data file (`tools/figures/charpipe/fit/fixer.json`). Byte-identical 9/9. The new Fixer stands 1.67 m against today's 1.75 m (the height slider can change it), with LOD0 at 18.8k triangles against today's 12k.
- **Pictures:** `out/scratch/forma/char-pipeline/fixer/` (`models.png`, `beside.png`, `shoulders.png`, `play.png`). Evidence: `fixer/mesh/`.
- **Remade at 30k** (Sky, 2026-10-27: "Player Models should be created at 30k triangles"). Tripo 3.1 `faceLimit: 30000` via Runware returned 29,116 triangles for $0.60. Runware does accept 30k, despite its docs page. The rule lives in data (`fixer.json` `faceLimitByKind.cell`), and the Fixer's spend totals $1.20. The extra triangles went mostly to the skirt and legs, the head and the feet. The ragged cut-out edges are in the texture, so they're the same at 20k and 30k. The 30k take has squared-off strands across the face and a pointed ear, both from the generator. Byte-identical: Fixer 9/9, e2e 31/31. Pictures: `out/scratch/forma/char-pipeline/fixer/compare_20k_30k.png`, `beside.png`.
- **Animation pass** (seven clips, no extra bones; Sky: "try with existing model and see where we get to"):
  - **The four e2e figures:** hold through all seven clips and can go to Unity.
  - **The new Fixer:** the upper body, face, ponytail (walk and run) and painted cut-outs hold. The long wrap skirt tears in every moving clip: Tripo builds the wrap as overlapping folds of one surface, and with nearest-body weights each fold follows a different leg (`out/scratch/forma/char-pipeline/fixer/media/skirt_closeups.jpg`).
  - **The fix, in order:**
    1. Average weights within about 2 cm across the skirt band, the armpit tool used again, as a data change.
    2. A skirt rig of about 8 chains × 3 bones on spring or bone cloth with leg colliders.
    3. Or cloth simulation.
  - **The ponytail:** sinks into her back in crouch and carried; a 3–4 bone chain later.
- **Skirt step 1 done:** a band merge (`fit.json` `transfer.bandMerge`; it fires only where a garment bridges the knee band, so only the Fixer's long skirt, with the e2e figures unchanged) stops the tearing in all seven clips. Leg-to-leg stretch p99 drops about 3.5× (walk 49→14, run 63→19). Walk, aim, sit and carry are acceptable for play. In run the sheet bulges around the forward thigh and the split can't open, so it still needs the skirt rig (step 2, about 8×3 bones with thigh colliders). Byte-identical: Fixer 9/9, e2e 31/31.
- **Skirt rig (step 2), built but not adopted.** 8×3 chains, solved and baked per clip in Blender, deterministic, with loops closed by a 6-frame blend; data in `fit.json` `garmentRigs`. On this mesh it doesn't beat the band merge and is clearly worse in crouch, sit and carried (crouch: skirt p99 3.0→8.1, flips 446→2,816). Tripo modelled the skirt as a closed solid joined to the legs under the hem, so any free swing stretches skin-coloured faces to the shin. A free skirt needs the hem opened and real legs under it first (the G-path garment cut), and the rig code can be reused then. Recommended for now: the band-merge build (`fixer/variant_norig/`). The ponytail chain is off for the same reason.
- **Latch's take-1 fixes, as rules:**
  - The avatar FBX root layout (`Armature`, plus a marker root) works, but the meta-write route stays the recommended one.
  - Every LOD0 is suffixed `_LOD0`.
  - Hands: finger weights are kept only when the generated fingers line up with the bones; otherwise the hand goes rigid. All ten hands are rigid now: no shredding, no finger curl. Fitting the finger bones to the mesh is later hero work.
  - A 5 mm stacked-layer merge for the elbow flecks.
  - Per-clip speeds go in the manifest rows, with crouch walk measured relative to the hips. Latch's in-engine value is the one the game should use.
  - Height must come from data (the person spec → the height slider); every woman built so far shares one skeleton.
  - The coat man's hair speckle is in the Hunyuan texture.
  - Byte-identical: e2e 31/31, Fixer 9/9.
