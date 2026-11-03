# Brief 067 · Forma · idles sliding left and right (Sky on 0.2.122)

## Cause
Main found it. Every Humanoid clip meta had `loopBlendPositionXZ: 0`, which leaves Root Transform Position (XZ) unbaked. The figures play with `applyRootMotion = false` (HostViews.Figures.cs:91, GameBoot.cs:371), so Unity removed each clip's sideways root travel. That held the body's centre still and made the planted feet slide. Breathing_Idle (`idle_alt`, played by half the crowd) shifts its hips 18.8 cm, so its feet slid about 19 cm.

## Fix (generator, tools/figures)
- `unitymeta.py`: `bakes_xz(entry)` sets the rule and `clip_meta(..., bake_xz)` writes `loopBlendPositionXZ: 1` (Bake Into Pose, based on Original; `keepOriginalPositionXZ: 1` as before). `clips.py` passes `bake_xz=um.bakes_xz(c)`.
- Rule: every clip that stays in place bakes XZ. Two kinds don't:
  - Locomotion (`loco`: walk, walk_f, walk_old, run, walk_armed, run_armed, crouch_walk, carry_shoulder, serve_walk, run_lean) stays as it was.
  - One-shots whose body ends somewhere else (`travelsM`, new in clips.json, measured in Blender: hit_large 1.37 m, cower 0.41, stunned 1.07, knockout 1.39, knockout_alt 1.49, kill_victim 0.34, kill_victim_back 0.90, kill_attacker 1.46, struggle_strangled 1.19). If these were baked, the body would end up to 1.5 m away from the object and jump back on the next clip (`lying` is in place). They stay unbaked as before, and none of them is played in the scene today. **This departs from Aster's list for hit_large and cower; see the open issue below.**
- That gives 32 clips baked and 19 not (51 in clips.json).
- `idle_alt` is now **Standing_Idle__c9c831cf** (6.0 s, hips 1.1 cm sideways), replacing Breathing_Idle. Once baked, Breathing_Idle's feet stay put, but its whole body still swings 18.8 cm = 7.2 px at play scale every 10 s. On half the crowd that still reads as "sways left and right" (proof below). The new clip is calm, and at 6 s against idle's 8.8 s a crowd still doesn't breathe in step.
- `figures.json`: rival_d's label now says "the hood is up" (it said "bare head (the hood is down)"). `assemble.py` now takes each figure's label from figures.json, so the manifest has the corrected wording without rebuilding the figure.
- The manifest's clips convention now describes the bake (`assemble.py`).
- `cell_generic.py` (CellGeneric) is not changed. It is Generic with no motion node (`motionNodeName` empty), so Unity extracts no root motion and the hips play as authored. Nothing there slides, and that build is not installed in Unity.

## Evidence
- `bun test tools/figures`: 23 pass, 0 fail. `tools/figures/sway.test.ts` holds the rule through the generator itself (python `bakes_xz` + `clip_meta` per clips.json entry): the named stationary clips bake, the walks and runs don't, only one-shots can carry `travelsM` (> 0.25 m), idle_alt is never Breathing_Idle, and clips.py uses the rule. It also checks the metas on disk. In the build it fails on any mismatch. In the Unity install it only warns until Latch reinstalls (32 clips are listed today).
- `bun run typecheck`: clean.
- Regenerated: `bun run guard <Blender> -b -P tools/figures/clips.py -- tools/figures/clips.json out/people-models/People` (51 clips, 24 s), then `python3 -I tools/figures/assemble.py out/people-models/People 5.2.2`. In `out/people-models/People/Clips/*.meta`, the 51 clips write 32 × `loopBlendPositionXZ: 1` and 19 × `0`. The other 4 metas there are the stale sit_c and sit_f1-3, see below.
- The re-authored clips (sit*, serve*, carry_shoulder*, carried_drape, run_lean) have new FBX bytes (export metadata) but the same motion: re-measured in Blender, every clip except idle_alt is identical to before (`briefs/067/forma-sway/all-before.json` vs `all.json`).
- Proof frame: `briefs/067/forma-sway/sway-proof.png` (git-ignored, on disk). Each clip is drawn at play scale (38.571 px/m, 38° down) and enlarged 4×. Grey is frame 0, blue and red are the frames where the hips are furthest each way, green is the toe path over the whole clip. In each "baked (new)" panel the feet stay on their marks and the hips and torso sway. In each "centre held (0.2.122)" panel the toes slide (green streak). For Breathing_Idle the toes slide about 7 px when the centre is held, and once baked the body swings 7.2 px. idle, Idle__c9c981a2 and Standing_Idle__c9c831cf all move under 1 px. Scripts: `briefs/067/forma-sway/{dump,draw,measure_all}.py`.

## Latch: reinstall
1. Copy the clips and the manifest from the build into Unity (the figures did not change, so there is no need to rebuild them with `run.ts --install`):
   `rsync -a out/people-models/People/Clips/ unity/Assets/Suborned/People/Clips/` (clips.rows.json included, as `run.ts --install` does)
   `cp out/people-models/People/manifest.json unity/Assets/Suborned/People/manifest.json`
   (or a full `bun tools/figures/run.ts --install` if you'd rather; it is slower and rebuilds all 26 figures.)
2. Let Unity reimport the 51 clip FBXs. The GUIDs are unchanged (same paths), so the controller's references hold. idle_alt is now 6.0 s instead of 10.0 s: if FigureBuild caches clip lengths or the library in `People/Generated`, re-run it.
3. Done-check: `bun test tools/figures` gives no `[sway]` warning (the install matches the rule). In play, an idle crowd's feet stay planted at play zoom (look at the crowd outside the café and the posted rivals). The café sitters sit where they did: the sit loops move under 1 cm, and frame 0 is placed the same way baked or not, so seat.json stands.

## Open issues
- **hit_large and cower are not baked** (Aster listed them). Both travel (1.37 m, 0.41 m). Baked, the body would end that far from the object and snap back on the next clip; unbaked, the feet slide during the stagger or duck. When the falls and staggers are wired, the fix is either root motion on for those one-shots with the sim taking the end position (Keel, Latch), or a flat `travelsM` removal to bake them and accept the snap. One line in clips.json flips each clip. Aster to decide.
- `unity/Assets/Suborned/People/Clips` and `out/people-models/People/Clips` still hold sit_c and sit_f1-3 (19 Oct, unbaked). They aren't in clips.json and no script names them. Removing them is Latch's call (destructive), not done here.
- aim_pistol (baked now) shuffles its feet 30 cm over 7 s and ends 10 cm from where it starts. The loop blend covers the seam, but if the pistol aim looks restless at play zoom it is a candidate for a calmer library aim.
