# Brief 069 · the left foot in walk: diagnosis (Latch, 2026-10-26)

Scratch project only (`out/scratch/latch/char-pipeline/unity/`); nothing in `unity/` touched. Code: `unity/Assets/Proof/Editor/FootDiag.cs` (`Run` = numbers, `Videos -footVariant neutralLegs` = the two takes). Raw numbers: `leftfoot/leftfoot.json` (summaries, every variant), `leftfoot/leftfoot-frames.json` (per frame: yawL, yawR, pitchL, pitchR, heightL, heightR).

## Cause (one line)
Each clip FBX builds its own Humanoid avatar from its own rest pose. For these Mixamo "without skin" downloads, that rest pose is the clip's frame 0, not a T-pose. In walk, frame 0 has the left heel up (Foot→Toe pitch −65° against −39° on the right) and the right foot toed out 12°. Humanoid takes that pose as muscle zero, so on every other body the left foot is planted about 20° toe-up and both feet are turned in.

## Measures
They match `foot.py`:
- Yaw is Foot→ToeBase on the ground against the hips' facing, where forward = (RightUpLeg − LeftUpLeg) × up. Positive is toward the figure's left, so left toe-out is + and right toe-out is −.
- Pitch is that vector's elevation.
- Planted means the foot bone is within 3 cm of its lowest point.
- The clip is sampled at 36 frames at 30 fps, root motion off, foot IK off (as the game's states have it).

| | L yaw planted (median, range) | L pitch planted | R yaw planted (median, range) | R pitch planted |
|---|---|---|---|---|
| Aster, Blender source | 0.8 (−3.6..8.8) | | −12.9 | |
| Source read raw in Unity (Generic) | 0.5 (−3.6..8.9) | −40.2 | −12.8 (−19.3..−10.9) | −39.2 |
| Humanoid on its own skeleton (encode, then decode) | 0.9 (−3.5..8.9) | −40.6 | −13.0 (−18.9..−11.1) | −39.2 |
| **today, `c6`** (rest pitch −43/−44) | −5.2 (−30.0..3.5) | **−20.3** | **+9.9** | −42.9 |
| **today, `guard`** (rest −47.5/−48.3) | −10.3 (−38.9..−4.9) | **−23.9** | **+15.0** (9.4..72.7) | −46.6 |
| **today, `cp_commuter_01`** (rest −26.7/−26.7) | −11.3 (−23.1..−8.5) | **−4.5** | **+9.5** | −26.5 |
| **today, `cp_guard_02`** (rest −26.4/−26.4) | −10.7 (−21.9..−7.7) | **−4.1** | **+8.7** | −26.1 |
| **fix, `c6`** | 0.5 (−5.7..5.5) | −41.8 | −11.2 (−16.0..−7.4) | −42.0 |
| **fix, `guard`** | −6.0 (−13.3..3.7) | −46.9 | −7.4 | −46.3 |
| **fix, `cp_commuter_01`** | −6.2 (−10.1..1.2) | −27.2 | −8.2 | −25.0 |
| **fix, `cp_guard_02`** | −4.9 (−7.6..1.9) | −26.6 | −8.7 | −24.6 |

**What the numbers say:**
- The source is clean, and Humanoid round-trips it on its own skeleton within 0.4°. The fault appears only when the clip is played on another avatar.
- With today's import, the planted left foot sits 20 to 23° toe-up against the body's own flat rest pitch. That is the "stuck out at a wrong angle" Sky sees.
- Both feet also turn inward: the right foot goes from the source's 13° toe-out to 9 to 15° toe-in.
- The same error shows on the old Meshy rigs and the new MPFB rigs, so it lives on the clip side.

**The walk avatar's rest is frame 0, exactly.**
- In `leftfoot.json` `walkAvatar` (from `avatar.humanDescription.skeleton` applied to the model), the rest is: yaw L 3.685 / R −12.068, pitch L −65.389 / R −39.179.
- In `leftfoot-frames.json` source frame 0 is: yaw 3.685 / −12.068, pitch −65.389 / −39.179.

## Hypotheses, one at a time (same four figures)
1. **Loop settings: not the cause.**
   - Unity's loop-match readout (`AnimationUtility.GetMuscleClipQualityInfo`) no longer exists in 6000.6, so I measured it directly. Source frame 35 equals frame 0 in yaw, pitch and height on both feet, so start and end match and loop blend has nothing to spread.
   - Loop blend off; all three loop blends off; the take's own range (which is the meta's 0..35); and the take's range with blend off: every planted number is identical to today's. (With all three blends off, the clip's own figure also changes, because loopBlendPositionY then stops levelling the body.)
2. **Avatar: this is the cause.**
   - The bone map is correct on both sides: `mixamorig:LeftUpLeg/LeftLeg/LeftFoot/LeftToeBase`, mirrored on the right.
   - The rest pose is the problem, as shown above.
   - Configure → Enforce T-Pose (Unity's own `AvatarSetupTool.MakePoseValid`, called through reflection) does not touch the feet: the rest stays at −65.4/−39.2, and the numbers are identical to today's.
   - Copy From Other Avatar using `idle.fbx`'s avatar (a different, also crooked rest: yaw L 15.3 / R −14.9) moves the error. The left foot looks right, but the right foot is now 15 to 18° toe-in. So every clip in `People/Clips` carries its own frame-0 offset.
   - **Fix that works (`neutralLegs`):** the walk's avatar is given a neutral leg rest; everything else stays as Unity generated it, including its arm T-pose. Each leg points straight down, and Foot→ToeBase points along the hips' facing, 40° down (the source's planted flat-foot pitch, which is the same on both sides), with the toe level. This is written into the clip meta's `humanDescription.skeleton` with auto-mapping off. Result:
     - the planted pitch matches each body's own flat rest within 1°, on all four figures;
     - `c6` reproduces the source's yaw: L 0.5 / R −11.2 against 0.5 / −12.8.
3. **Twist and IK: not the cause.**
   - Lower and upper leg twist set to 0, then 1, on the clip's avatar: identical numbers.
   - Height From Feet off: identical numbers.
   - Foot IK on: it pulls the yaw back toward 0 (it masks the turn-in), but leaves the left foot 20° toe-up on the old figures and flat-but-wrong on the new ones. It is not a fix, and the game's states run with it off.
4. **Anything else:**
   - Humanoid muscle limits are not clamping: the round trip on the clip's own skeleton is within 0.4°.
   - Residual after the fix: on `guard` and both MPFB figures, both feet sit about 6 to 8° rotated toward the figure's right (L −5..−6, R −7..−9). `c6` matches the source. This is a common turn of both feet, not a left-foot error. It is most likely each rig's pelvis or hip axes against how I measure facing, and it is a small open item.

## Is the fix an import setting? Yes, on the clip side, so it carries into the new pipeline by rule
The fix lives entirely in the clips' import (their `.fbx.meta`). No figure changes. Two ways to apply it by rule:
- **(a) One shared Mixamo clip avatar (recommended).** Make one avatar from one clip with this neutral leg rest (its arms as Unity's T-pose), saved in its meta. Then every clip in `People/Clips` imports with `avatarSetup: 2` (Copy From Other Avatar) and `sourceAvatar` pointing at it. All clips share one zero reference, so no clip's frame 0 leaks in. The bone names are identical across the Mixamo library, so it applies to all 55 clips and to any new library clip.
- **(b) Per clip.** Write the same neutral leg rest into each clip meta's `humanDescription` (human map + skeleton, auto-mapping off).

Either way it is generator work in the clip meta writer. That is `tools/figures/unitymeta.py` (the clip metas), which Forma owns and which is on the brief's hold until batch 0.2.123 lands. I didn't edit it.

**Other clips are affected too.** The idle's rest is yaw L 15.3 / R −14.9, so idle, run and the rest each carry their own frame-0 offset. Once the fix is in the meta writer, re-measure the run and idle feet.

## Pictures
- `frames/video/leftfoot_unity.mp4` is today's import and `frames/video/leftfoot_unity_fixed.mp4` has the fix. Both are Aster's shot:
  - perspective 35° vertical FOV, 1280×720, 30 fps;
  - the camera at (0.8, 0.5, 2.6) looking at (0, 0.5, 0);
  - `cp_commuter_01` on the left half and `c6` on the right, 1.2 m apart, facing +Z;
  - the walk in place: 3 loops at 1× then 3 loops at ¼×, 525 frames, H.264, yuv420p;
  - labelled top-left of each half, with the speed and take in the bottom-left corner.
- `leftfoot/leftfoot_before_after.png` (copied to `frames/strips/leftfoot_before_after.png`) shows the feet at ¼× at clip times 0, 0.29, 0.58 and 0.88 s: today's import on top, the fix below.

## Round 2: the bow legs, "legs swinging outwards" (Latch, 2026-10-26)
Code: `unity/Assets/Proof/Editor/FootDiag.Bow.cs` (`Bow` for the numbers, `BowVideos` for the takes). Numbers: `leftfoot/bowleg.json`.

### Cause (one line)
The same fault on the figure side of the retarget. Each figure's avatar takes its own rest pose as muscle zero, and Unity's auto T-pose doesn't straighten the legs. Every figure stands with its legs spread (upper leg 5 to 6.4° out from vertical, ankle 0.07 to 0.12 leg-lengths out from its hip), and the walk is played on top of that spread. So the legs bow out and the feet land wide. This also explains round 1's leftover 6 to 8° turn of both feet to the right.

### Measures
All in the hips' frame:
- lateral = RightUpLeg − LeftUpLeg on the ground; "out" = away from the midline on each side;
- **abduction** = the UpLeg→Leg sideways angle from vertical in the frontal plane, + outward;
- **knee** and **ankle** = that joint's outward offset from its own hip ÷ leg length (UpLeg→Leg + Leg→Foot);
- **step** = the right planted foot minus the left planted foot on the lateral axis ÷ leg length, each relative to the hips' centre, taken while that foot is planted (within 3 cm of its lowest);
- values are medians over the walk's 36 frames, at 30 fps, foot IK off.

Rest poses as the avatar holds them today (abduction L/R; ankle offset; feet apart ÷ leg length):
- `cp_commuter_01`: 5.8/5.8°; 0.106; 0.47
- `cp_commuter_02`: 5.6/5.6°; 0.116; 0.49
- `c6`: 5.2/4.8°; 0.074; 0.36
- `guard`: 6.4/6.4°; 0.10; 0.44

With neutral legs: 0°, 0, and 0.21 to 0.26 (just the hip joints' own spacing).

| | abd L | abd R | knee L | knee R | ankle L | ankle R | step | foot yaw L / R (round 1) |
|---|---|---|---|---|---|---|---|---|
| **source, raw (Generic)** | −0.8 | 3.1 | −0.006 | 0.026 | −0.019 | 0.010 | 0.058 | 0.5 / −12.8 |
| source, Humanoid on its own skeleton (clip fix) | −0.8 | 2.7 | −0.006 | 0.023 | −0.018 | 0.009 | 0.055 | |
| `cp_commuter_01`, clip fix only | 7.4 | 7.1 | 0.054 | 0.057 | 0.070 | 0.071 | 0.262 | −6.2 / −8.2 |
| `cp_commuter_01`, **clip + figure neutralLegs** | 1.1 | 1.1 | 0.009 | 0.009 | −0.009 | 0.003 | 0.153 | **−0.7 / −12.5** |
| `cp_commuter_02` (MPFB woman), clip fix only | 6.5 | 7.1 | 0.052 | 0.055 | 0.071 | 0.070 | 0.281 | −9.4 / −4.5 |
| `cp_commuter_02`, **clip + figure neutralLegs** | 0.8 | 1.2 | 0.006 | 0.009 | −0.011 | 0.001 | 0.159 | **0.7 / −12.6** |
| `c6`, clip fix only | 5.7 | 6.9 | 0.046 | 0.057 | 0.044 | 0.072 | 0.198 | 0.5 / −11.2 |
| `c6`, **clip + figure neutralLegs** | 0.3 | 2.1 | 0.002 | 0.017 | −0.016 | 0.017 | 0.090 | **1.4 / −13.7** |
| `guard`, clip fix only | 7.0 | 9.0 | 0.052 | 0.072 | 0.050 | 0.080 | 0.207 | −6.0 / −7.4 |
| `guard`, **clip + figure neutralLegs** | 0.4 | 2.1 | 0.004 | 0.018 | −0.012 | 0.016 | 0.126 | **2.3 / −15.0** |

Today's clip import with no fix gives the same spread, made lopsided by round 1's foot fault. For example, `cp_commuter_01`: abduction 4.5 / 8.0, ankle −0.020 / 0.088.

### The three tests, one at a time (all on the fixed clip)
1. **neutralLegs on the figures' avatars: matches the source.**
   - The rule: legs straight down; each foot pointed along the hips' facing at its own body's flat-foot pitch (the mean of its rest pitches); toes level. This is written to the figure meta's `humanDescription.skeleton`, with auto-mapping off; arms and the rest stay as Unity generated them.
   - Abduction moves from 6 to 9° to 0.3 to 2.1° against the source's −0.8 / 2.7 to 3.1.
   - Knee and ankle offsets come within 0.01 to 0.02 leg-lengths of the source.
   - The planted feet's yaw now reproduces the source on all four bodies: L −0.7 to 2.3 and R −12.5 to −15.0, against 0.5 / −12.8. So round 1's 6 to 8° leftover is gone.
   - Planted pitch stays flat for each body.
2. **feetSpacing −0.2 and +0.2 on the figure avatar: no effect.** Every number is identical to the clip-fix-only row.
3. **upperLegTwist / lowerLegTwist 0 and 1 on the figure avatar: no effect.** Identical numbers. Round 1 tried the same on the clip's avatar, also with no effect.

### Residual
Step width comes down from 0.20 to 0.28 to 0.09 to 0.16 leg-lengths, but stays wider than the source's 0.058. With straight legs the feet now sit under each body's own hip joints. Humanoid carries joint angles, not foot placement, so a body with wider-set hip joints than Mixamo's (MPFB most of all) still steps a little wider. If Glyph still sees that at play zoom, two options: Forma could narrow the hip joints in MPFB's rig fit, or the game could use foot IK (which pins the feet to the clip's placement). I don't think it needs either; the bow and the outward swing are gone in the take.

### The fix is import settings, by rule, on both sides
- **Clips:** one neutral-leg clip avatar (round 1).
- **Figures:** the same neutral-leg rest written into each figure meta's `humanDescription`. That is the T-pose write I confirmed in `tpose-probe.txt` (the human and skeleton arrays persist once auto-mapping is off), with this leg rule on top.

Both are generator work in Forma's meta writer (`tools/figures/unitymeta.py` for today's figures and clips, `tools/figures/charpipe` for the new ones), not per-figure hand work. `unitymeta.py` stays on the brief's hold until batch 0.2.123 lands.

### Videos
Aster's front shot: camera (0, 0.6, 3.2) looking at (0, 0.55, 0), 35° vertical FOV, 1280×720, 30 fps, H.264 yuv420p; `cp_commuter_01` on the left and `c6` on the right; 3 loops at 1× then 3 at ¼×, 525 frames each.
- `frames/video/bowleg_unity.mp4`: the clip fix only.
- `frames/video/bowleg_unity_fixed.mp4`: the clip fix plus both figures' neutral-leg avatars (`Assets/Proof/FootTest/figs/{cp_commuter_01,c6}_neutralLegs.fbx`).
