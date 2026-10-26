# Brief 069 end-to-end · hand-off to Latch (Forma, 2026-10-27)

Four people generated from the intake stills: Hunyuan 3D 3.1 Pro on Runware, multi-view, textured, $2.70 in all, ledger `paid/069-e2e/ledger.jsonl`. Each was fitted by the hybrid:

- the generated mesh and texture are kept;
- the MPFB base gives the skeleton and the weights;
- G's pieces come in where they apply: the guard's cap is a rigid Head piece; attachment points are on every figure; the props are in their own file.

Nothing in `unity/` was touched. Please build this in your scratch project only.

**Status: models first.** Sky's rule ("no point doing the video if the models are bad surely?") holds: the static check `e2e/models.png` waits for Sky's OK. Only after that do the clips, metrics and your Unity takes run. This hand-off is so you can set up the import now.

## Files (all in `out/scratch/forma/char-pipeline/e2e/a/`)
| person | figure | Humanoid reference | props | texture | stand beside (today's figure) |
|---|---|---|---|---|---|
| coat, man | `e2e_coat_H.fbx` | `e2e_coat_H_avatarT.fbx` | `e2e_coat_H_props.fbx` | `e2e_coat_H_albedo.png` | `People/Figures/c6` |
| shift dress, woman | `e2e_dress_H.fbx` | `e2e_dress_H_avatarT.fbx` | `e2e_dress_H_props.fbx` | `e2e_dress_H_albedo.png` | `People/Figures/cafe_w5` (a café dress); `waitress` (Inez) as the second |
| guard, woman, peaked cap | `e2e_guard_H.fbx` | `e2e_guard_H_avatarT.fbx` | `e2e_guard_H_props.fbx` | `e2e_guard_H_albedo.png` | `People/Figures/guard` |
| Sky's flamingo commuter, pillbox | `e2e_flamingo_H.fbx` | `e2e_flamingo_H_avatarT.fbx` | `e2e_flamingo_H_props.fbx` | `e2e_flamingo_H_albedo.png` | `People/Figures/d6` (hat, dress) |

- **The figure FBX:**
  - Contains `Armature` (52 deforming Humanoid bones, Mixamo names without prefix, plus the attachment bones) and the body in three LODs: `<id>_A_LOD0` is the generated mesh at full detail (47–50k faces, its own normals), with `_LOD1` at 4.5k and `_LOD2` at 1.5k.
  - The guard also has its cap as its own mesh, `e2e_guard_hat`, with `_LOD1` and `_LOD2`, weighted 100 % to Head.
  - Metres, -Z forward, Y up, pinned FBX header.
  - **The rest and bind pose is the A-pose the person was generated in, not a T-pose.** The mesh is never re-posed at rest, so it matches the generated model exactly.
- **`_avatarT.fbx`:** the same armature with nothing else in it, posed to the Humanoid reference and applied as its rest.
  - The reference is the clips' Mixamo T-pose for the upper body, with your neutral legs.
  - Measured on every person after the build: UpLeg abduction 0°, ankle offset 0, foot yaw 0°, foot pitch each body's own (26.8–27.8°).
- **`_props.fbx`:** the same armature plus `prop_bag` (a cross-body bag, the strap skinned from the nearest body weights, the bag body on Hips) and `prop_tray` (rigid on LeftHand). Props are never in the figure file.
- **`<id>_H.json`:** the build's stats: sex, macros, LOD triangles, weight transfer counts, the hat cut, `restLegs`, attachment bones.
- **`<id>_H_labels.npy`:** the per-vertex garment labels.
- **`textures.json`:** the PNGs, copied byte for byte out of the generated glbs (4096 px base colour), with their sha256.

## Import settings
1. **Figure FBX, Model tab:**
   - Scale Factor 1, Convert Units on.
   - Normals: Import (LOD0 carries the generated normals; don't recalculate).
   - Tangents: **Calculate Mikktspace.** The FBX carries none: exported tangents were the only part of the file that differed between two builds.
   - Read/Write off. Materials: the FBX's material references `<id>_H_albedo.png` by file name (it sits beside the FBX); make it matte (smoothness about 0.15, as today's figures).
2. **Figure FBX, Rig tab:**
   - Animation Type Humanoid, Avatar Definition **Copy From Other Avatar**.
   - Source: the avatar made from `<id>_H_avatarT.fbx` (that file: Humanoid, Create From This Model, auto-mapping).
   - Both files have the identical hierarchy and names, including `Attach_*`, so the copy maps one to one. Unity then takes the T-pose with neutral legs from that avatar as muscle zero while the mesh stays bound in its A-pose. That is your round-2 rule, delivered as a file instead of a meta write.
   - Fallback, if Copy From Other is fussy about the A-pose bind: Create From This Model on the figure, then write `humanDescription.skeleton` from `_avatarT.fbx`'s transforms (your `tpose-probe` write) with auto-mapping off. Running `NeutralLegs(importer, null)` on top is then a no-op check: the legs are already neutral.
3. **Optimize Game Objects off,** or list the attachment bones under Extra Transforms to Expose: `Attach_RightHand`, `Attach_LeftHand`, `Attach_HipRight`, `Attach_Back`, `Attach_ShoulderLeft`. They are non-deforming bones placed by rule on each body (palm centres, right hip at body width + 1 cm, centre back at Spine2, top of the left shoulder below the neck base).
4. **Props:**
   - `_props.fbx`: put its SkinnedMeshRenderers under the figure and rebind `bones` by name to the figure's own transforms; the armature in the file is identical.
   - Simpler for the tray: parent `prop_tray` under `Attach_LeftHand`, keeping its rest offset.
5. **LODGroup:** LOD0, LOD1, LOD2. The guard's cap goes in each LOD with its matching cap LOD.
6. **Clips:**
   - Seven fresh Mixamo files from `~/Projects/the-suborned-store/source-art/Anim/mixamo_library/`. Copy them as sourced:
     - `Walk__c9ccf814.fbx` (walk, loop)
     - `Run__c9c72c68.fbx` (run, loop)
     - `Crouch_Walking__c9c92c49.fbx` (crouch walk, loop)
     - `Sitting_Idle__c9cb95c1.fbx` (sit)
     - `Pistol_Aim__c9c8b958.fbx` (aim_pistol)
     - `Carrying__c9c82dfa.fbx` (carry)
     - `Being_Carried__c9c81d97.fbx` (carried: authored lying low, hips at 0.22 m against 1.04 m at rest, because the carrier is a separate clip; a figure on its own lies near the ground)
   - Every clip: Humanoid, **Copy From Other Avatar = one shared neutral-leg clip avatar** (your option (a); your `Assets/Proof/FootTest/walk_neutralLegs.fbx` is that avatar). Root motion as today's in-place clips.
   - Licence: Adobe Mixamo terms, royalty-free in the game, the raw files not redistributed on their own.
7. **Foot IK off** in the states, as the game has it.

## What I'd like back
- Each new figure beside today's matching one in your scratch scene: idle in the rest A-pose first (does it match `e2e/models.png`?), then walk, run, crouch walk, sit, aim_pistol and carry. Run these only after Sky's OK on the models.
- The import warnings, if any (expected: none).
- Whether Copy From Other Avatar took the A-pose bind cleanly, or you needed the fallback.

## Known and open
- **The pillbox (`e2e_flamingo`) stays welded** in the body mesh, rigid on Head by the weight rule. The clean-cut test (e2e.json `hat`) found only 70 % of its faces in its own shells (it needs 80 %): the hair top above the brow shares its shells. The guard's cap passed (93 %) and is its own piece; 68 scalp vertices were pulled under it by the hair-under-hat rule.
- **Arms up to horizontal** shows the usual skinning fold along the shoulder line (`e2e/shoulders.png`). The slit and crumpled sleeve seen before were fixed (armpit-band spatial weight merge).
- **Cost:** LOD0 at full detail is 47–50k faces against today's 12k LOD0. That follows GDD §01 (no decimation without Sky's sign-off), and it is Redline's question for frame cost: today's LOD0 budget would mean asking Sky.
- **Determinism:** two full builds give byte-identical deliverables, all 31 files (basis, fit, figure/avatar/props FBX, stats, labels, textures). That holds in the same folder (`e2e/determinism.json`) and across folders (`e2e/determinism_main_vs_det.json`). The FBX carries no absolute path: the texture reference is the bare `<id>_H_albedo.png` beside it.
