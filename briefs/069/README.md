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
- **Unity take 2 (Latch):** all five import with no per-figure work. Both avatar routes work; the meta-write route is recommended and also copies the full 52-bone map (Unity's auto-map on the A-pose finds only 22). Fixed: the guard's cap, the hand shredding (hands are now rigid and open, with no grip), and the elbow flecks. Walk and run slide ≤2.5% at the manifest speeds. Crouch-walk speed in Unity is 0.82–0.93 m/s against the manifest's 1.02–1.17 (Blender), so the generator adopts the Unity values. The rigged Fixer works as a second layer but reads as kneeling in sit, which confirms it isn't adopted. Evidence: `latch/e2e-unity.md`, `latch/e2e-unity-numbers.json`. Next: Redline's frame cost.
- **Frame cost (Redline, Studio, `redline/cost-e2e.md`):**
  - **At play zoom the new five cost +40% per person** against today's five (11.58 vs 8.28 µs). A 60-person crowd costs 0.69 vs 0.50 ms, which fails the Hold.
  - **Causes:**
    - the bag and cap are separate skinned meshes outside the level-of-detail group: draw calls ×2, +10%;
    - the finger bones: +8–9%;
    - the far LOD has about 9% more vertices.
  - **Textures:** frame cost is the same at any size, so it's a memory question only (50 crowd figures: 530 MB at 4K, 135 at 2K, 34 at 1K). Unity silently caps imports at 2048.
  - **The cell:** four 29k Fixers at LOD0 cost 0.042 ms, so 30k for the cell is safe.
  - **Recommended:**
    - crowd LOD0 about 12k / LOD1 4.5k / LOD2 1.5k;
    - 1K crowd textures and 2K for the cell and heroes, with the import size set explicitly;
    - the 22-bone crowd skeleton;
    - worn props merged into the body per LOD;
    - Optimize Game Objects for crowd figures.
  - **Then:** one confirmation pass on Forma's rebuilt figures.
- **Rebuilt to Sky's budgets** (2026-10-27, "yes and yes"):

  | | Triangles (LOD0/1/2) | Bones | Texture |
  |---|---|---|---|
  | crowd (e2e four) | 12k / 4.5k / 1.5k | 22 | 1K |
  | the cell (Fixer) | 29.1k / 4.5k / 1.5k | 52 | 2K |

  - Worn props (cap, bag) are merged into the body per LOD.
  - The reduction carries the high mesh's normals, so faces and hands no longer crack.
  - Explicit texture import max size in the metas.
  - Height from data: the Fixer 1.75 m (Sky); the crowd's tall/average/short comes from the intake text via a table in `fit.json`.
  - Crouch-walk speeds are Latch's Unity values, scaled by height.
  - The Fixer keeps the band-merge skirt; the rig is parked (`fixer/parked_rig/`).
  - Byte-identical: e2e 35/35, Fixer 10/10.
  - Evidence: `e2e/manifest-budget.json`, `e2e/textures-budget.json`, `fixer/mesh/manifest-budget.json`. Next: Latch take 3 (Optimize Game Objects), then Redline's confirmation pass.
- **Unity take 3 (Latch):** all five import by one rule with Optimize Game Objects (Head, hands and the Attach bones exposed). LODs, bones and textures read as the manifest; the merged props show once at every LOD; the tray rides `Attach_LeftHand`. Walk and crouch walk are within 1.7%; run is 2–4% fast in the manifest, so Unity's run values should be adopted too.
- **The Hold passes (Redline, `redline/cost-take3.md`):**
  - **60-person crowd:** new 0.377 ms against 0.405 ms for today's crowd as the game draws it (7–8% cheaper per person, cheaper in every paired round). Against today's also optimised, it's +0.03 ms, closing to about 2% (noise) if the `Attach_*` bones are exposed only where the role carries a prop.
  - **Texture memory:** 0.7 MB per crowd figure against 2.2 MB.
  - **Draw calls:** the same (604 per 300 people).
  - **The cell at 30k:** negligible (+0.007 ms for four).
  - **Optimize Game Objects alone:** saves today's figures 15%. Worth moving into the game's figure code for everyone.

## A: the animation fixes in today's game (2026-10-27, Sky: "A: yes")
Forma put Latch's left-foot and bow-leg fix into the game's figure pipeline as generator rules. `tools/figures/avatar.py` ports Unity's avatar setup and matches Unity's own avatars to within 0.06° on five cases (`avatar_check.py`).
- **Clips:** `clips.json` `avatar` names `walk` as the shared neutral-leg reference. 42 clips copy it. 16 Blender-authored clips (under an extra `Armature` node) get their own avatar with the same neutral legs. 4 old `sit_c` / `sit_f1–3` leftovers are untouched: they're not in `clips.json`, nothing references them, and they're candidates to delete.
- **Figures:** all 26 installed figures have the neutral-leg rest with auto-mapping off (`build.py`).
- **Checks:** reinstalled with `run.ts --install`; `avatar_verify.py` reads the metas back. Tests 23/23, typecheck clean. Unity not run yet: next is Latch's build check plus Optimize Game Objects in `FigureBuild.cs`.
- **Fix and build check, all green.**
  - **The 16 lone-root clips:** they failed avatar creation (Unity folds a lone `Armature` root), which left `Figures.controller` half-built. Fixed by `preserveHierarchy: 1` by rule (b093984a). `FigureBuild.Cli` restored all 59 states.
  - **Clips and figures:** 0 avatar errors; 42 clips copy walk, 17 have their own avatar, all 26 figures are valid.
  - **Feet:** in the game, `c6`, `guard`, `d6` and the Enforcer plant within 1.5° of flat, yaw +1/−14, thighs about 2°.
  - **Optimize Game Objects** in `FigureBuild.cs`: Head and Hips on all, RightShoulder on the agents, RightHand on the servers; `HostViews` finds exposed bones by name. The tray seats on all four servers.
  - **Dev build 0.2.140**, not shipped: the proof passes, with before/after clips in `out/scratch/latch/069a/shots/video/`.
  - **To look at:** `sit_talk` plays with the legs swapped (the crossed leg mirrored), and `cover_fire`'s stance shifts sideways. Evidence: `latch/069a/`.

## Props (2026-10-27)
- **The cell's pistol:** Sky picked design 4 with the glossy, more-black finish (`paid/069-props/codex/pistol_v3_gloss_contrast.png`; 3D sheet `pistol_d4_sheet_1.png`). The first Codex wording was moderation-blocked; framing it as a stylised game prop passed.
  - **Mesh:** Tripo 3.1, $0.40, 2,631 triangles, 18 cm, 1K PBR gloss.
  - **Rules** (`tools/figures/charpipe/props/props.json`): +Z along the barrel, +Y up the slide, origin at the grip. One offset per skeleton on `Attach_RightHand` (fixed attach frames by rule in `fit.json`), with a seat check that fails loudly.
- **Holster per role** (Sky):
  - **The cell** hides the holstered gun and shows it only in the drawn states (`draw`, `aim_pistol`, `aim`, `shoot`, `cover_in/kneel/fire/out`, `run_armed`, `walk_armed`).
  - **Guards** wear it visibly as a guard cue, with no cross-body bag on that hip. A 10° arm-out on the holster side with the holster 7 cm back clears the walking arm (0 contacts).
  - **The Enforcer** gets their own carbine (Sky will provide the art).
  - **Guards get a distinct sidearm** (Sky): concepts in `paid/069-props/codex/guard_sidearm_1.png` and `_2.png`, awaiting Sky's pick.
- **Hands:** the hands are rigid, so the pistol sits on an open palm. Sky approved the finger-fitting rule for all full-skeleton figures, next after Latch's game check.
- **Round 2 of the game check** (build 0.2.141, not shipped): green. The `sit_talk` swap and `cover_fire` are fixed. Latch's apparent `cover_pin` and seated "thigh" errors were his front-plane angle metric: it blows up when the thigh points forward, with only 15–23% of it left in the plane. Measured in the hips' frame in Unity, every own-avatar clip is within 1.7° on lift, with a shared ~3° sideways pelvis shift from hip width (Forma's probe `out/scratch/forma/unityprobe`). The held avatar changes (walk's rest by bone name for the own-avatar clips) are committed.
- **Pistol seat fix:** Blender +Y exports as Unity −Z, so the barrel pointed back along the wrist in Unity. The mesh now puts the barrel along Unity +Z, and `prop_build.py` checks the written FBX in Unity's frame. The barrel follows the clip's level aim line (the bladed two-hand aim puts the forearm 55–60° across). Hand offset (0, 0, 0.025), rotation (−0.0096, 0.0555, 0, 0.9984), one value for both figures within 2.3°. Next: Latch's reimport and probe, then the finger rule.
- **Unity probe of the fixed pistol (Latch):** it seats in the hand (0.5–0.9 cm), follows the aim line on the ground within 0–3.7°, show/hide by role works, and the guard's holster clears with the arm out 10° (0/30 frames). But in Unity the barrel pitches 5–18° down and the hips' angle drifts 25→41° over the clip: Unity's Humanoid wrist differs from Blender's.
- **The finger rule (Forma)**, for 52-bone figures:
  - **Fit:** finger bones placed along the generated fingers (separate pieces past the webs, joined within 1.5 mm), rolled to curl to the palm. Fewer than 4 fingers found → rigid hand. Weights go in thirds along each finger.
  - **Fixer:** 4+4 fingers and the right thumb fitted; the left thumb stays rigid. Byte-identical 10/10, and run's finger curl plays.
  - **Closed grip:** a pose in data (`props.json` `grip`), seated at the closed middle finger's centre. Wrap check: middle 180°, ring 248° (pass). The fingers sink a little into the grip (330 pairs).
- **Unity is the source of truth for prop offsets:** `PropCal.cs` measures in Unity. No fixed offset holds the barrel level across aim_pistol's hold (residual pitch −13…+15°, yaw up to about 30°), so the drawn states need a runtime level-aim correction (Latch's IK). The offset rule is now grip seated by the hand, rotation identity, which agrees between Blender and Unity to 0.0°.
- **Grip, hands and support (Forma):**
  - **Finger limits:** wider Humanoid finger limits for finger-boned figures (`tools/figures/finger_limits.json`), written into each figure's avatar and the clips' reference avatar. Not installed in the game: today's figures have no finger bones.
  - **Support hand:** a left-hand `support` attach in the prop data, solved from the grip shape and reachable in Unity (PropCal), plus a left-hand pose.
  - **Rebuilds:** Fixer 11/11 and e2e 39/39, byte-identical.
  - **The grip tidy failed:** the pistol's grip is 6.4–7.2 × 3.9 cm, about 1.5× a real compact pistol's, and the fitted fingers can't close round it without crossing. Fixed grip: 330 crossings, wrap passes. Contact solver: 60 crossings, wrap fails. A slimmer grip settles it: Sky's call.
- **Latch's runtime job** (`PistolHold.cs`, scratch): grip muscles, level aim, the guard arm-out and the support-hand IK. The root yaw follows the barrel's per-frame blade, so the wrist correction is pitch only (−5…+17°) and the barrel lands within 0.3° of the target.
- **Best-practice weapon route** (Sky, 2026-10-27: "we want official best practice"):
  - a prop spec in data (standard grip block per class, pivot at the grip, Unity-frame axes, sockets `grip` / `support` / `shoulder` / `muzzle`);
  - props fitted to it by rule (the pistol's grip slimmed);
  - Mixamo weapon clip sets (pistol, rifle);
  - Unity's Animation Rigging (Multi-Aim, Two Bone IK, hand-pose override layers) in place of the custom job.
- **The Enforcer's carbine:** Sky's hand-built model (`out/enforcer-lods-v1/`, LODs 2,499 / 997 / 400, 2K / 1K / 512 PBR) is brought in at **0.9 m** (Sky; the source is 1.594 m), with pivot at the grip, axes to spec, the ORM repacked for URP, and support and shoulder sockets.
- **Weapon spec round (Forma):**
  - **Spec in data:** Unity-frame axes, pivot at the grip, handgun grip block 5×3×10 cm (`research/weapon-grip-spec.md`), and sockets mapped to Animation Rigging constraints.
  - **Pistol:** grip slimmed by rule to 5.0×3.0.
  - **Mixamo weapon clip sets** for pistol and rifle, chosen by a survey of closed-hand clips (`props/weapon_clips_survey.json`), built into `out/people-models`, not installed in the game.
  - **Carbine:** at 0.9 m, LODs as delivered, URP-repacked maps (clearcoat and anisotropy lost; coat shows as smoothness).
  - **Gates still fail:** the fitted fingers bend at the web, so the closed hand's loop (about 2 cm inside) is smaller than the grip.
  - **Also open:** pistol walk and run speeds read 2.5 and 4.4 m/s; the carbine's stock (grip to butt) is only 0.18 m at 0.9 m against a real carbine's about 0.33–0.36 m length of pull.
- **Animation Rigging (Latch, scratch):** package 6.6.0. Multi-Parent, Multi-Aim and Two Bone IK put the barrel within 0.27° of the target and both hands on their sockets, deterministically. It doesn't work with Optimize Game Objects (Unity staff confirm). Standard split: the crowd stays optimised; the cell runs unoptimised with the rig; guards, rivals and security deoptimise on draw and re-optimise on holster (switch being proven).
- **Weapon round 2 (Forma):**
  - **Pistol clip speeds:** fixed by a speed-band rule (walk 1.1–1.7, run 2.3–3.2 m/s). `pistol_walk` is walk with the pistol aim layered on (1.53 m/s); `pistol_run` is a different pack clip (2.86 m/s).
  - **Carbine grip depth:** the 1.9 cm was a mis-measure; the grip is 4.83 × 2.99 × 10 cm.
  - **Knuckle rule:** knuckle-to-tip from the hand's length.
  - **Per-class hand pose:** solved against the grip block, but the gates still fail (block centred too far back on the palm).
  - **Guards on Unity's Humanoid IK pass** (scratch): stays optimised, barrel 0.03–0.16° to target, 0 errors, deterministic; the support hand lands 0.7–3.5 cm off. The deoptimise-on-draw switch was rejected: after rig constraints bind, re-optimising stops the Animator.
- **Hands and grip, round 3 (Forma):**
  - **Hand size:** the Fixer's wrist bone sits at the wrist crease, and the generated hand is genuinely about 12% small (16 cm against about 18.5 cm expected); the knuckle rule uses straight wrist-to-tip. Byte-identical 11/11.
  - **Grip-block search:** still fails the gates (Fixer pistol middle/ring 105/77°, 238 crossings). The block's front sits ahead of the knuckles; next is seating it at the knuckle line.
  - **Carbine:** 35 cm length of pull by rule (stock ×2.14, forward ×0.75, 0.9 m kept), but the butt is still about 18.5 cm short of the shoulder in `rifle_aim`.
- **Sky's new Enforcer design** (`paid/069-enforcer/source/enforcer_design_sky.png`): converted to an intake sheet (`paid/069-enforcer/codex/enforcer_intake_1.png`, pose check PASS). Open: its heavy gold against §15's cell rule (no gold trim), and the Tripo mesh, which needs Sky's OK.
- **Time-boxed grip** (Forma): the knuckle-line seat still failed the gates, so the route ended on the standard one: one hand pose per weapon class, posed once (`spec.handPose.artist`) and stored as data for every finger-boned figure.
  - **Gates:** the wrap gate still fails numerically (Unity: middle/ring 69/54°, pistol 28 of 297 hand vertices inside), but the close third-person pictures read as a closed hand on the grip.
  - **Carbine:** placed the standard way, by the grip with the butt on the shoulder line (`placeBy` grip + shoulder). The shoulder gap is 2.1 cm (Blender) and 7.5 cm (Unity).
  - **Unity check tool:** `props/unity/WeaponCheck.cs`.
  - Picture: `out/scratch/forma/char-pipeline/props/weapons/sheet_weapons.png`.
- **Sky (2026-10-27):**
  1. Accepted the artist hand pose at its current quality; Latch does the final take (the cell on Animation Rigging, guards on the Humanoid IK pass).
  2. The Enforcer's gold is swapped for satin chrome, keeping §15's cell rule; the sheet is `paid/069-enforcer/codex/enforcer_intake_chrome_1.png` (pose check PASS; prompts in `enforcer/`).
  3. His Tripo 3.1 mesh at 30k is approved ($1.00 pass).
- **Final weapons take (Latch, scratch):**
  - **Fixer pistol** (Animation Rigging): barrel 0.43–0.45° to target, hands on their sockets in draw and aim (the right hand drifts up to 3.8 cm in fire), hidden after the holster.
  - **Fixer carbine:** barrel 0.72°, hands on, but the butt is 9–10 cm off the shoulder (it needs a shoulder-pull constraint).
  - **Guard pistol** (Humanoid IK, optimised throughout): barrel 0.26–0.58°; the IK leaves the right hand up to 4.6 cm short and the left 1.5–8 cm from support.
  - **All three:** 0 errors, deterministic.
  - **Grip layer:** all 15 finger bones reach the artist pose.
  - **Fire clips:** they use the aim clip's blade angle.
  - **Enforcer:** Tripo was called once on the badge sheet before the hold landed ($0.60). Re-take (pass to $1.20) or paint the badge out is Sky's call.
  - Evidence: `latch/weapons-final/final-check.txt`; videos in `out/scratch/latch/char-pipeline/weapons-final/video/`.
- **The new Enforcer (Forma):** Tripo 3.1 30k from the no-badge sheet (pass raised to $1.20 with Sky's OK; the badge take is parked).
  - **Build:** 28,908 / 4,500 / 1,500 triangles, 52 bones, 2K, 1.852 m skeleton, byte-identical 11/11.
  - **Coat:** open at the front, so the band merge doesn't fire (42% bridged); a hip-weight coat rule made it worse and is parked. The coat rides nearest-body weights (stretch p99 walk 1.3, run 2.3). The real fix is cutting the panels free.
  - **Gloves:** the right glove's fingers are fitted; the left glove is fused, so that hand is rigid.
  - **Carbine:** the butt is 8.2 cm from the shoulder in Blender.
- **Pistol set rebuilt around one stance:** aim `Pistol_Idle c9cdb599`; fire = the aim held plus an 8° kick; draw and holster = 0.6 s blends into and out of the aim. Transitions match to 0.0 cm on the Fixer; no body swing in fire. There's no hip-holster draw in the library.
- **Latch's spine aim** (Multi-Aim on Spine/Chest/UpperChest): the pistol hand stays on the grip (was 3.8 cm), but the old fire clip's foot drift remains (that's the clip). The carbine is worse with it, so it keeps the grip + shoulder setup.
- **Pistol rerun (Latch):**
  - **Fixer** (new one-stance set, spine aim): fixed. Feet drift 1–2 cm (was 20–33), no spin, hands on the gun throughout, barrel 0.45°.
  - **Guard** (Humanoid IK): barrel on target, hands 2–5 cm off their sockets.
  - **New Enforcer, carbine:** barrel and hands right, but the butt is 13–14 cm off the shoulder in Unity (it needs a shoulder pull); left foot drift 22.6 cm in the idle→aim move.
  - **All three:** 0 errors, deterministic. Evidence: `latch/weapons-final/final2-check.txt`.
- **Rifle set, one stance (Forma):**
  - **Clips:** idle is now `Rifle_Aiming_Idle` (gun held at aim; the pack has no lowered idle), with aim and fire from the same pack; new 0.6 s `rifle_raise` / `rifle_lower` blends from the unarmed idle.
  - **Transitions:** match within 1 mm (hands and feet).
  - **Feet:** move only in raise and lower (11–13 cm: different stance), which would need a step or entering from walk.
  - Strip: `out/scratch/forma/char-pipeline/props/sheet_rifle_set.png`.
- **Carbine shoulder pull (Latch):** the weapon pivots at the butt, pulled into a shoulder pocket off the right clavicle, with Multi-Aim to the target. Butt to pocket 0.0 cm, barrel 0.00°. To reach, the support hand slides about 11 cm back along the handguard and the left clavicle turns forward. Feet move only in raise and lower (about 21 / 5 cm). Every take also has a front-on video. Evidence: `latch/weapons-final/final3-check.txt`.
- **Animation library comparison:** `research/anim-packs.md`. Recommended: ActorCore clip by clip ($424.50, 38/50 covered).
- **ActorCore free-clip test (Forma, $0):** 8 free clips (relaxed walk set, gun fastrun set, shooting, turn90) imported through our clip stage.
  - **Bones:** Unity's auto-map works (55 bones). One bone-map rule in data (`tools/figures/bonemaps.json` `rigs.actorcore_cc3`, recognised by the `CC_Base_Hip` bone) covers every CC3+ clip. Frame 0 (T-pose) is the clip's own reference and is dropped from playback.
  - **Determinism:** byte-identical.
  - **Joins:** loops close exactly; start/loop/end joins are 2–8.6 cm (need about 0.15 s crossfades).
  - **Shared stance:** "same pack = same stance" holds (shooting, turn90 and fastrun start share a first frame to 0.0 cm).
  - **Caveats:**
    - the relaxed walk is a stroll (0.59 m/s, outside the walk band);
    - the fastrun's speed swings within each cycle (slip 25–35 cm/s at constant speed);
    - the shooting clip is a step-in crouch, not a recoil loop;
    - turn90 needs the character turned at its end;
    - start and end clips need their ground-speed profile or root motion.
  - **Pre-existing:** the 23 Blender-authored game clips aren't byte-identical across builds.
  - Evidence: `forma/actorcore-test.json`.
- **The new Enforcer's feet** (Sky: "oddly placed"):
  - **Cause:** his mesh stands each leg 2.26° outward, and the neutral-leg rest straightened it, so every clip turned his legs inward (ankles 0.30→0.23 m; long boots nearly touching).
  - **Rule:** `fit.json` `rest.stance` keeps a figure's own outward stance (up to 6°); inward stays straight as before.
  - **Result:** walk ankles 16.0→20.8 cm apart, boot gap 5.4→10.3 cm, no overlaps. The Fixer and e2e figures are unchanged; byte-identical.
  - **Open:** the right foot's ~11° toe-out is the Mixamo walk's own gait, on every figure. Evidence: `forma/enforcer-feet.json`.
- **Hands** (Sky: "hands pose looks unnatural"):
  - **Causes:**
    1. The figures' reference kept the generated wrist roll, so palms played 22–51° off the source.
    2. The take-2 import dropped the figure's finger limits.
    3. Rigid hands kept the generated straight, spread pose.
  - **Rules:**
    - `fit.json` `rest.hands` (`avatar.py` `reference_hands`): palm and finger lines matched to the clips' reference hand;
    - the `kept` import (copy the human map from our `_avatarT` meta);
    - `transfer.fingers.rigidRelax`: a 65° relaxed curl baked into rigid hands;
    - flat-fingered clips flagged, plus a `hand_relaxed` clip for a fingers-only override layer.
  - **Result:** palms within 3.3° of the source in walk, fingers within about 4°. Byte-identical.
  - **Open:** the crowd's 22-bone palms. Evidence: `forma/hands.json`.
