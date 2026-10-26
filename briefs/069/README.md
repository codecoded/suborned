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

## Where the rest is (on disk, not in git)
- Round 1 proof: `out/scratch/forma/char-pipeline/proof.html`; sign-off sheet: `signoff/signoff.html`.
- Unity frames and videos: `out/scratch/latch/char-pipeline/frames/` (incl. `video/leftfoot_unity*.mp4`, `bowleg_unity*.mp4`); the scratch Unity project at `out/scratch/latch/char-pipeline/unity/`.
- Web reference of the raw walk: `out/scratch/aster/foot069/` (`leftfoot_web.mp4`, `bowleg_web_front.mp4`, `web/walk_web.html`).
- Intake: `out/scratch/forma/char-pipeline/intake/intake.html`; stills: `paid/069-intake/codex/`.
- Fit test: `out/scratch/forma/char-pipeline/fit-test/fit-test.html` and `media/`.
