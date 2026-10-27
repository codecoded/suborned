# Brief 069 · the five new figures in Unity, take 1 (Latch, 2026-10-27)

Everything here was made in the scratch project `out/scratch/latch/char-pipeline/unity/`; nothing in `unity/` was touched.

- Inputs: Forma's `e2e/handoff-latch.md`. I copied the five figures from `e2e/a/` and `fixer/a/` before 11:59 (the Fixer as the band-merge build, with no skirt rig) and the seven fresh Mixamo clips from the store.
- Code: `unity/Assets/Proof/Editor/FootDiag.E2E*.cs` and `unity/Assets/Proof/Runtime/FigureRig.cs`.

## Files
- **Close videos:** `video/<pair>_close.mp4` for `e2e_coat`, `e2e_dress`, `e2e_guard`, `e2e_flamingo` and `fixer_v7`.
  - The shot: 1280×720 at 30 fps, perspective 35° vertical FOV, a low three-quarter view from (1.5, 1.05, 4.3) looking at (0, 0.85, 0).
  - The new figure is on the left and today's matching figure on the right.
  - The order: the rest A-pose, then walk (1× and ¼×), run (1× and ¼×), crouch_walk, sit, aim_pistol, carry and carried.
  - Each figure carries its bag; the tray is shown in the stills.
- **Play zoom:** `video/play_zoom.mp4` and `play_stills/play_<clip>.png`, with `_2x.png` crops (zoom 14 at 38°, yaw 30, 1920×1080).
  - All ten figures, in pairs. The new figures are the back row.
  - In walk, run and crouch_walk each figure moves at its own predicted speed: the clip's speed on the source skeleton × the figure's humanScale.
- **Stills:** in `stills_take1/`:
  - `hands_<id>_{front,side}.png`: aim_pistol at LOD0, all ten figures;
  - `cap_<clip>_<n>_{34,side}.png`: the guard's cap in walk, run, crouch_walk, carried and aim_pistol;
  - `props_<id>_<clip>.png`: the bag and tray in walk, sit, aim_pistol and carry.
- **Raw frames:** `video_<pair>_take1/` and `play_take1/`, each with a `segments.txt`.
- **Numbers:** `numbers.json` (foot slide, the left-foot measures and the leg measures in walk, against the raw source) and `import-report.json` (avatars, LODs, materials, the props' hierarchy).

## Import: what it took, and the bugs for Forma
The settings follow the hand-off:
- tangents MikkTSpace, normals imported, Read/Write off, Optimize Game Objects off;
- the URP Lit material made from the FBX's material description, with the albedo found beside the FBX;
- the clips: Humanoid, Copy From Other Avatar from the shared neutral clip avatar (`Assets/Proof/FootTest/walk_neutralLegs.fbx`), with the game's walk loop settings and foot IK off;
- the props rebound to the figure's bones by name;
- the LODGroup heights `[0.18, 0.08, 0.012]`, today's three-LOD convention.

Nothing was done by hand per figure. Two rules apply to all five, and two things are Forma's to fix:

1. **FORMA BUG 1: Copy From Other Avatar from `_avatarT.fbx` lays every figure flat.**
   - What I measured in walk: the upper leg at −157° (pointing up), planted foot pitch +16 to +35°, and planted-foot speeds about half what they should be.
   - **Cause:** the T-pose file's armature is its FBX's only root node, and Unity folds a single root into the prefab root.
     - The avatar ends up as `root (−90° X) / Hips`.
     - The figure keeps `root / Armature (−90° X) / Hips`.
     - The node is also named `Armature.001`, not `Armature`.
   - With Preserve Hierarchy on, the reference avatar fails to build: `Rig Error: Avatar creation failed: Transform 'Armature.001' not found in HumanDescription.`
   - A prefab imported with Copy From Other also carries no Animator.
   - **What I used instead (the hand-off's fallback, as a rule for all five):**
     - The figure is set to Create From This Model (auto-mapped).
     - Then each bone's rest is copied by name from the `_avatarT` skeleton into the figure meta's `humanDescription.skeleton`. The figure keeps its own root and `Armature` entries.
     - Auto-mapping is off, so it persists.
     - The result matches the source (see the numbers).
   - **Fix for Forma:** either name the T-pose file's node `Armature` and keep the same root layout as the figure FBX, or skip the second file and write the human and skeleton arrays straight into the figure meta (that's what my rule does).
2. **FORMA BUG 2: the guard's cap at LOD0 is named `e2e_guard_hat`, with no `_LOD0`.**
   - Unity leaves it outside the LODGroup, so it draws at every distance, on top of `e2e_guard_hat_LOD1` and `_LOD2`. That's a double cap at play zoom.
   - My rule (`FigureRig`): a renderer left outside the group whose name plus `_LOD1` is in LOD1 joins LOD0.
   - The fix is to rename it `e2e_guard_hat_LOD0`.
3. **Not a bug, a decision for the game:** `FigureLibrary`/`HostViews.Dress` adds an Animator without an avatar. The figure meta should carry the avatar, which my rule above gives.
4. **Material smoothness:** the FBX brings 0.2 (the hand-off asks for 0.15). My folder rule didn't take, because URP's material-description import runs after it. The effect is small; I left it.

## Numbers (walk; source = the fresh `Walk__c9ccf814` read raw)

| | foot yaw L / R | planted pitch L / R | abduction L / R | knee L / R | ankle L / R | step |
|---|---|---|---|---|---|---|
| **source** | 6.8 / 1.0 | −38.8 / −39.2 | 0.0 / 0.3 | 0.000 / 0.002 | −0.013 / −0.008 | 0.085 |
| e2e coat | 6.0 / 1.9 | −25.9 / −25.5 | 1.9 / −1.7 | 0.015 / −0.012 | −0.018 / −0.018 | 0.183 |
| e2e dress, guard, flamingo, Fixer v7 (identical MPFB women) | 6.2 / 1.8 | −26.8 / −26.4 | 2.2 / −1.6 | 0.016 / −0.011 | −0.017 / −0.020 | 0.17 |
| today's c6, d6, fixer | 4.7 / 1.0 | −42 / −43 | **6.6 / 4.3** | 0.051 / 0.032 | 0.034 / 0.041 | 0.213 |
| today's guard | 0.4 / 6.7 | −46 / −47 | **8.4 / 7.3** | 0.060 / 0.046 | 0.039 / 0.061 | 0.269 |

- **Both fixes hold on the new figures.**
  - The left-foot fix: foot yaw is within 1° of the source on both feet, and the planted pitch is each body's own flat stance.
  - The bow-leg fix: abduction is within 2°, and the knee and ankle are within 0.02 leg-lengths.
  - Step width is still wider than the source (0.17 against 0.085), because the feet sit under each body's own hip joints (as in round 2).
- **Today's figures still bow** (abduction 6 to 8°): their avatars don't have the neutral legs yet.

**Foot slide** (`numbers.json` `figures.<id>.<clip>`): the planted-foot speed in place, against the predicted speed (the clip's speed per humanScale on the source skeleton × the figure's humanScale; walk 1.568, run 3.191, crouch_walk 1.100).
- New figures, walk: within +2.5%. Run: within −2.2%.
- **crouch_walk: −15 to −18%.** Scaling by humanScale doesn't carry the crouch (the knees are bent). The generator should write each figure's measured speed per clip instead: crouch_walk 0.82 to 0.93 m/s on these five.
- Today's figures: −13 to −21% (walk and run) and −30 to −37% (crouch_walk) against the same prediction. Their proportions differ from the source skeleton.

## One line per figure
- **e2e coat:** imports by rule (with the fallback avatar). Feet and legs match the source, and walk and run slide within 2.5%. The bag and tray ride their points.
  - **Hands tear in aim_pistol:** the generated fingers shred into spikes at LOD0 (`hands_e2e_coat_H_front.png`).
  - Small white flecks (holes) show at the elbows in aim.
- **e2e dress:** as the coat. The short dress holds in walk, run, sit and crouch. Hands tear in aim.
- **e2e guard:** as the coat. The cap rides the head in every clip (FORMA BUG 2 for its LOD0 name). Hands tear in aim.
- **e2e flamingo:** as the coat. The welded pillbox and the coat skirt hold. Hands tear in aim.
- **Fixer v7 (take 1: band merge, no skirt rig):**
  - No tears.
  - The skirt holds in walk, aim, sit and carry.
  - In run it moves as one stiff sheet that bulges over the forward thigh, and in crouch_walk the dark inner panel shows.
  - Her hands hold in aim (`hands_fixer_v7_H_front.png`).
  - Her humanScale is 0.884, exactly the e2e women's, though the hand-off gives her 1.67 m (and the 11:59 rig build's json 1.711 m) against their 1.60 m. Worth Forma checking which body her take-1 file has.
- **carried** lies on the ground for all ten, as authored (hips at 0.22 m). In the game, the carrier's shoulder places it.

## Take 2 (the Fixer's skirt rig): waiting for Forma's hand-off
At 11:59 to 12:03, while I worked, `fixer/a/` changed: 76 bones (8 skirt chains × 3), plus `chains/fixer_v7_chains_<clip>.fbx` and `chains_clips.json`, apparently the skirt bones baked per clip. Nothing says it's finished, so take 1 uses the copy from before.

Playing the rig in Unity needs a rule first:
- the Humanoid clip on the body;
- the same-named Generic chain clip on the `Skirt_*` bones, as a second layer;
- an AvatarMask holding only the skirt transforms, both through one PlayableGraph layer mixer.

Forma: say when the rig is final, and whether the chain clips are meant to be played as they are or as a starting pose for spring or cloth bones. Then I'll take it.

## For Redline: the cost of the five new figures beside today's five
These scenes are in the same development player as the crowd bench: `out/scratch/latch/char-pipeline/build/CrowdBench069.app`. The bench's flags and output are as in `../handoff-redline.md`.
- `-scene E2E_Ten`: all ten (five new, five today).
- `-scene E2E_New5`: the five new only.
- `-scene E2E_Today5`: today's five only.
- `-zoom <ortho size>` changes the camera from play zoom 14.
- `-forceLod 0|1|2` pins a LOD. At play zoom the figures draw LOD2 (1.5k), and LOD0 (29k to 50k against today's 12k) only comes in close.

Each figure is dressed by `FigureRig`: the reference avatar, the LOD heights and the bag. They walk, run or stand on small circles, like the crowd bench.

```sh
S=$PWD/out/scratch/latch/char-pipeline
for sc in E2E_Today5 E2E_New5 E2E_Ten; do for lod in none 0; do
  bun run guard "$S/build/CrowdBench069.app/Contents/MacOS/CrowdBench069" -scene $sc $( [ $lod = none ] || echo -forceLod $lod ) \
    -warmup 5 -measure 30 -benchOut $S/bench/e2e-$sc-lod$lod.json -logFile $S/bench/e2e-$sc-lod$lod.log
done; done
```
I didn't measure. The 2-second smoke runs only prove that all three scenes load and run with no exceptions. The scene stats in each JSON (renderers, materials, bones per renderer, triangle sums by LOD) are written by the bench.

---

# Take 2 (Latch, 2026-10-27): Forma's rebuilt figures
- Inputs: all five rebuilt (`e2e/a/` from 12:30; the Fixer from `fixer/variant_norig/a/`, band merge, 52 bones), the manifests' `clipSpeedMs`, and, as an optional take, the rigged Fixer (`fixer/a/` plus `chains/`).
- The import rules are the same as take 1, plus one change: **the figure's human map now comes from `_avatarT` too.** Unity's auto-map on the A-pose figure finds only 22 human bones (no fingers, toes or shoulders). Take 1's meta route had that same thin map: its walk numbers matched the source, but the fingers weren't mapped.
- I applied one reset that is scratch-only: the take-1 metas stored an avatar description that no longer matched the new `_avatarT` layout (`Rig Error: Avatar creation failed: Transform 'Armature' not found in HumanDescription`). So the setup now clears it before every import. A fresh project, or the game's meta writer, wouldn't hit this.

## Files (take 2)
- **Close videos:** `video/take2/<pair>_close.mp4` for all five, beside today's matches (same shot as take 1).
- **Play zoom:** `video/take2/play_zoom.mp4`; `play_stills/take2/play_<clip>.png` and `_2x.png`.
  - Walk and run move at the manifests' speeds; crouch_walk at the Unity-measured speed (below).
- **Stills:** `stills_take2/` (hands, cap, props).
- **Numbers:** `numbers.json` (take 2, replaces take 1's).
- **The rigged Fixer (optional):** `video/take2/fixer_v7_rig_vs_bandmerge.mp4` (left: rigged, its chain clips on a masked second layer; right: band merge). `rig-take.txt` has the binding counts.

## What changed from take 1
1. **The avatar route:** both routes now work, and they give identical walk numbers.
   - **Copy From Other Avatar:** `_avatarT.fbx` keeps `Armature` and builds a valid 52-bone humanoid, 30 of them finger bones. The figure imported this way still carries **no Animator**, so the code that instances it has to add one with the avatar (`FigureRig` does).
   - **The meta route** (Create From This Model, then the human map and rest copied from `_avatarT` by name, auto-mapping off): a 52-bone avatar inside the figure, with its own Animator.
   - It stays the recommended route: one avatar per figure, and no runtime avatar wiring.
2. **The guard's cap:** fixed. `e2e_guard_hat_LOD0/1/2` each sit in their LOD, and nothing is left outside the group. My orphan rule now has nothing to do.
3. **Hands:** fixed. There's no shredding in aim on any of the five (`stills_take2/hands_*`). The hands are rigid open hands now, so there's no finger curl and no grip; fitting the fingers is the later hero fix.
   - **Elbows** in aim: the white flecks are gone on all four e2e figures.
4. **Speeds:** walk and run in the manifests match Unity. Crouch walk doesn't.
   - Each value is the planted-foot speed in place in Unity against the manifest's `clipSpeedMs`, with the slide at the manifest speed.
   - The adopted crouch_walk values (Unity) are 0.932 for the coat, 0.824 for the dress, 0.831 for the guard, 0.824 for the flamingo and 0.832 for the Fixer.

   | figure | walk Unity / manifest | slide | run Unity / manifest | slide | **crouch_walk Unity** / manifest | slide at manifest |
   |---|---|---|---|---|---|---|
   | e2e_coat | 1.568 / 1.565 | +0.2% | 3.127 / 3.200 | −2.3% | **0.932** / 1.170 | −25.5% |
   | e2e_dress | 1.386 / 1.384 | +0.1% | 2.760 / 2.824 | −2.3% | **0.824** / 1.025 | −24.3% |
   | e2e_guard | 1.389 / 1.387 | +0.2% | 2.761 / 2.829 | −2.5% | **0.831** / 1.031 | −24.1% |
   | e2e_flamingo | 1.384 / 1.384 | −0.0% | 2.757 / 2.823 | −2.4% | **0.824** / 1.023 | −24.2% |
   | fixer_v7 | 1.391 / 1.387 | +0.2% | 2.766 / 2.831 | −2.3% | **0.832** / 1.033 | −24.1% |

   - **For the generator:** adopt the Unity value for crouch_walk; run could take Unity's too (2 to 2.5% lower). Walk already matches.
5. **Feet and legs in walk:** unchanged from take 1, and still matching the source.
   - Foot turn: 6.0–6.2° left and 1.8–1.9° right, against the source's 6.8° and 1.0°.
   - Upper legs splay outward 1.9–2.2° on the left and turn 1.6–1.7° inward on the right, against the source's 0.0° and 0.3°.
   - Knee and ankle are within 0.02 leg-lengths. Step width is 0.17–0.18 against the source's 0.085 (the hip-width leftover, as before).
6. **The Fixer:** the band-merge build (52 bones), as recommended. Her skirt looks as in take 1: it holds in walk, aim, sit and carry; in run it bulges over the forward thigh; in crouch_walk the inner panel shows. Her body scale is the e2e women's (as Forma confirms).

## The rigged Fixer (optional): the route works; the look doesn't beat band merge
- Her chain clips import Generic with Preserve Hierarchy on. Their single `Armature` root has to stay, or the curve paths lose the `Armature/` prefix and nothing binds.
- Each chain clip carries 260 curves, 240 of them on `Skirt_*` bones, at the paths `Armature/Hips/Skirt_*`. Every clip is the same length as its body clip.
- An AvatarMask holding only the 24 `Skirt_*` transforms (of 86), with every humanoid body part off, drives the second layer of one AnimationLayerMixerPlayable over the Humanoid clip. I saw no import messages.
- **What it looks like:** the skirt bones play as baked. In sit, the rigged skirt hangs straight down to the floor, so she reads as kneeling. In run and crouch it swings differently from band merge, but no better. That agrees with Forma's call not to adopt it.

## One line per figure (take 2)
- **e2e coat:** imports by rule with no messages. Feet and legs match the source. Walk slides 0.2% at the manifest speed and run −2.3%; crouch_walk needs Unity's 0.932 m/s. Hands are rigid and clean in aim; the elbows are clean; the bag and tray ride their points.
- **e2e dress:** as the coat; crouch_walk 0.824 m/s; the short dress holds.
- **e2e guard:** as the coat; crouch_walk 0.831 m/s. The cap sits in all three LODs with no double at play zoom, and rides the head in every clip.
- **e2e flamingo:** as the coat; crouch_walk 0.824 m/s; the pillbox and the coat skirt hold.
- **Fixer v7 (band merge, adopted):** imports by rule; crouch_walk 0.832 m/s. Her hands are clean in aim. The skirt holds except for the run bulge and the inner panel in crouch_walk (unchanged from take 1). The rigged build plays correctly in Unity but looks worse in sit, as Forma found.

## For Redline: the E2E_* scenes are final
`E2E_Ten`, `E2E_New5` and `E2E_Today5` now hold the take-2 builds (the player was rebuilt and all three scenes were smoke-run with `-forceLod 0`, with no exceptions). The run loop is in "For Redline" above.
- The new figures' LOD0 triangles: coat 50,000; dress 50,000; guard 46,897 plus the 3,103-triangle cap; flamingo 50,000; Fixer 29,116.
- The bench's scene block shows E2E_New5 with 22 skinned renderers, 9 materials and up to 57 bones per renderer, against E2E_Today5 with 12, 12 and 23.
