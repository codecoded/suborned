# Brief 109 · the patrol robot's green (Vela, 2026-11-12)

Sky, on 0.2.330: "our patrol robots green doesn't quite look right compared to the blender model".

Board: `board.png` (this folder, git-ignored). Left is the Blender model at the play angle; right is the game by day at the same angle (0.2.330, route-a-board tick 200, enlarged). Under them: swatches and the cause.

## What differs, and why
| | body green |
|---|---|
| the kit row `DK_Enamel_Sage` / `DK_RobotEnamel_Sage` (what Blender draws) | #7E8C54: hue 75, sat 0.25 |
| Blender render, sunlit (AgX Punchy, clear coat) | #8C956E: hue 74, sat 0.15 |
| in game 0.2.330, sunlit (measured) | **#7F8106: hue 61, sat 0.91, blue almost gone** |

- **The Unity material is right as written.**
  - `Kit/Materials/DK_RobotEnamel_Sage.mat`: DecoKit/Lit Cutaway, `_BaseColor` (0.494, 0.549, 0.329), which is #7E8C54 as sRGB, as Unity stores it.
  - Smoothness 0.4, metallic 0, reflections off (Glint's 096 matte).
  - The robot detail albedo (`robot_enamel_detail_v1.png`, linear, ×2) is neutral grey: mean ×2 = 0.956, with no tint in any channel.
- **Nothing at runtime tints the body.**
  - `PatrolBot.cs` swaps only the beacon dome's material.
  - The face sheet's property block is on its own renderer.
  - The 109 face glow is a separate cyan quad at the face.
- **The cause is the play look.**
  - The robot stands in warm sun: the light fitted on its cream is red 1.61, green 1.15, blue 0.83.
  - The day grade (`Scenes/HarbourDistrict-grade.asset`) adds saturation +25 and contrast +10, then ACES.
  - The sheets' sage is a yellow-green with little blue. Warm light cuts its blue further, and the saturation and ACES push what's left out, so it reads chartreuse.
  - Blender's AgX and its clear coat, which reflects the studio, keep it sage.
- **The model checks out.** `tools/decokit/play_look.py` (new) models the grade in URP's order, with ACES by Hill's fit. With the light fitted on the robot's cream, it predicts the sage at #898001 against the measured #7F8106.

## The fix (in the kit pipeline, the Unity material only)
- `tools/decokit/unity_mats.py`: `GAME_ALBEDO_109 = {"DK_RobotEnamel_Sage": "#789682"}`, applied where robot enamel .mats are written.
  - #789682 is the albedo the play look shows, sunlit, as **#7F8D53**: hue 74, sat 0.26. That is the sheets' sage.
  - The solve is `python3 -I tools/decokit/play_look.py`. It has the same shape as 096's `EMIT_BASE_096`, a Unity-only base colour.
- The kit row and Blender keep #7E8C54, so the references and the turnaround are unchanged. The FBX is unchanged.
- **Kit step done (nobody was playing):**
  - `bun run decokit --mats-only`: in `out/decokit/Materials`, only `DK_RobotEnamel_Sage.mat` changed.
  - `bun run kit --extra out/decokit-107-extra`:
    - in kit.json, only PatrolBot104's `files` changed (the .mat hash);
    - one file was copied into `unity/Assets/Suborned/Kit`.
  - Collide: unchanged, since it is keyed by the FBX.
  - `sim:site`: sim/data is byte-identical.
  - `bun run cover`: only `from.kit` changed.
  - The skyline digest fixture still passes.
  - decokit and kit tests: 91 pass.

## To build and check
1. Aster: the next `bun run batch --sky` carries it. The material is already in the Kit folder, and no rebake is needed (the robot isn't lightmapped).
2. **After check**, with the built player and nobody playing:
   ```
   R=/Users/sky/Projects/suborned; A=$R/out/scratch/vela/109bot/after; mkdir -p $A
   (cd $R/out/scratch/glint/107/run && bun resolve.ts $R/briefs/109/vela/patrolbot-green/shots.json $A > $A.resolved.json)
   bun run guard open -g -n -W "$R/out/build/play/The Suborned.app" --args -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -shots $A.resolved.json $A -novsync -fastExit -logFile $A/player.log
   ```
   - Crop the robot at (880, 350, 1010, 540) in `patrol-t200-z4.jpg`.
   - The sunlit body should read about #7F8D53 (hue about 74), not #7F8106.

## Seen, not changed (Sky's call)
- **The robot's cream** goes butter-yellow in game for the same reason: #F3DDB0 shows as #ECCA7E (sat 0.74, against 0.25 in Blender). The same solve would fix it, but Sky asked only about the green.
- **The face glass** reads warm brown in game, where Blender shows near-black glass. That is the LED face sheet over the dark screen in warm light. It's a separate item, if Sky wants it.

## Files
- **Changed:**
  - `tools/decokit/unity_mats.py`
  - `contracts/world/kit.json`
  - `contracts/world/civic-block.cover.json`
  - `briefs/104/vela/patrolbot/make/render.py`: the `play_close` and `play_close_60` shots.
- **New:**
  - `tools/decokit/play_look.py`
  - this folder's `notes.md` and `shots.json`
- **Not committed:** `board.png`, and `unity/Assets/Suborned/Kit/Materials/DK_RobotEnamel_Sage.mat`, which is git-ignored and copied by the kit step.
