# Brief 069 · done-check 5a: crowd frame cost, old against new (Redline, 2026-10-26)

Studio only (Mac17,14, Apple M5 Max), development player, Unity 6000.6.2f1, 1920×1080 windowed, vSync off, play camera (zoom 14, 38°, yaw 30), the game's URP asset, quality and lighting. No Air run.

## How it was measured
- Bench: Latch's CrowdBench, copied as `Assets/Proof/Redline/Runtime/CrowdBenchR.cs` (new file) with levers, a crowd count and ProfilerRecorder splits. Latch's files are unchanged.
- Editor tool: `Assets/Proof/Redline/Editor/RedlineBench.cs` (new). It makes the masked controller, the finger-stripped copies of the new figures (`Generated/NoFinger/`), the crowd set `newnf`, the scene `Crowd_R.unity`, and three players.
- Players: `build/deform{0,1,2}`, which are CPU, GPU and GPU batched skinning. GPU batched is the game's setting. `ProjectSettings.asset` was restored byte-identical after the builds.
- Runs: every run went through `bun run guard`. Each had a 3 s warm-up that was dropped and 6–8 s measured (about 3,000–10,000 frames).
  - Configurations were interleaved in rotated rounds, so background load fell on all of them.
  - Each figure in the tables is the median of the per-run medians, with the min–max across runs.
  - Pass A was 60 people, 3 rounds. Pass B was 150 and 600 people, 3 rounds. Pass C was 60 people with no recorders, 5 rounds. Pass D was 300 people, 4 rounds.
  - Machine time was about 30 min across the four passes.
- Load caveat: the Studio was busy for every run. Load average was 8–36 (Forma's Blender was at about 760 % CPU, plus other agents). Absolute numbers would be lower on an idle machine. The comparisons hold because the runs were interleaved.
- Raw data:
  - `raw/{A,B,C,D}/*.rN.json`: one file per run, holding frame, CPU main, render and GPU timings, plus about 180 marker and counter medians.
  - `summary-*.json`: the per-configuration rollups.
  - `scripts/` (`pass.sh`, `pass*.cfg`, `summarise.py`).

## Old against new, 60 people (the Hold's table)

| | today's crowd (old) | new pipeline (new) |
|---|---|---|
| frame ms, 60 people (pass C, 5 runs) | 0.83 [0.74–1.20] | 0.91 [0.81–1.33] |
| empty-scene floor (base0) | 0.71 | 0.71 |
| crowd cost per person (600-person slope) | 8.66 µs | 9.35 µs (+8 %) |
| → crowd cost of 60 (Studio) | **0.52 ms** | **0.56 ms** (+0.04 ms) |
| main-thread animation (Director Begin + End), 600 people | 3.42 ms | 3.92 ms (+15 %) |
| main thread with Animators off, 600 people (render + skinning only) | 2.70 ms | 2.61 ms |
| GPU frame, 600 people | 2.54 ms | 2.64 ms |
| skinned mesh renderers | 126 (LOD0/1/2 SMRs; 60 visible) | 120 (60 visible) |
| LOD drawn at play zoom | LOD1 ×54, LOD2 ×6 | LOD1 ×60 |
| distinct materials | 37 | 1 |
| SetPass calls | 33 | 32 |
| draw calls (Standard) | 124 | 124 |
| triangles drawn, including the shadow pass | 180,578 | 189,976 (+5 %) |
| bones per SMR | 23 | 52 (22 body + 30 finger) |
| transforms per person | 27 | 56 |
| LOD1 per figure: triangles / Unity vertices | 1,498–4,499 / 1,382–3,820 (passers 1,500 / 1,487–2,370) | 1,520–1,718 / 937–1,174 |

- **At 60 people the difference is inside the noise.** The 60-person frame on the Studio is about 0.2 ms above an empty-scene floor of 0.7 ms. Run-to-run spread is larger than the difference: the same build ran from 0.71 to 1.15 ms in different runs.
- **Latch's 0.70 against 1.35 ms smoke result is not a real gap.** It is that bimodal run-to-run noise, which comes from scheduling on a loaded machine. The 600- and 300-person runs resolve the true difference: new costs about 0.7 µs more per person, which is +8 %.
- Paired by round, new minus old was:
  - at 600: +0.81, +0.48, +0.41 ms;
  - at 300: +0.71, +0.08, −0.02, +0.41 ms.

## Cause: the finger bones, on the Animator side. Confirmed.
- With the Animators off, new costs the same as old (2.61 against 2.70 ms at 600). Render, culling and GPU skinning are not the gap.
- The gap is all animation: main-thread Director time is +0.50 ms at 600 (3.92 against 3.42).
- Summed over worker threads at 600 (new against old):

| | old | new |
|---|---|---|
| Animator.EvaluateRetargeter | 3.45 | 4.07 ms |
| Animator.EvaluateIK | 2.30 | 3.55 ms |
| Animator.WriteTransforms | 0.39 | 0.78 ms (twice the transforms) |
| MeshSkinning.CalcMatrices | 3.01 | 3.33 ms |

- **Proof by removal:** the same 36 new figures with the fingers stripped run at 5.71 ms at 600. That is 0.19 ms below old and 0.60 ms below new.
  - Stripping means finger weights folded into the hand, finger transforms removed, and a Humanoid avatar with 22 bones.
  - Paired against old, the stripped crowd was +0.12, −0.30, −0.19 ms at 600 and −0.09, −0.50, −0.22, +0.05 ms at 300.
  - Its main-thread animation is 3.36 ms, against 3.42 for old.
- The GPU is not where the cost goes. New LOD1 has about 5 % more triangles but about 40 % fewer vertices, because the old meshes have many UV seams. CPU skinning shows the vertex side directly: summed `MeshSkinning.Skin` is 16.4 ms for new against 24.9 ms for old at 600.

## Levers (measured; crowd cost per person from the 600-person runs, base0 0.70 ms subtracted)

| lever | old µs/person | new µs/person | Δ against new as delivered | note |
|---|---|---|---|---|
| as delivered (GPU batched skinning, CullCompletely, 4 weights) | 8.66 | 9.35 | 0 | |
| **fingers stripped (22-bone avatar; the mesh keeps its fingers, posed by the hand)** | n/a | **8.35** | **−11 %** | now below old; meets the Hold |
| **Optimize Game Objects** (`AnimatorUtility.OptimizeTransformHierarchy`, nothing exposed) | 7.80 (−10 %) | **7.99** | **−15 %** | 300-person runs: −0.90 ms new, −0.84 ms old; `MeshSkinning.CalcMatrices` falls to 0 |
| AvatarMask with the fingers masked out (the controller layer) | n/a | 9.32 | 0 (noise) | masking does not skip retargeting or transform writes for mapped finger bones: no lever |
| quality `skinWeights` 2 instead of 4 | n/a | 9.82 | 0 (noise, +) | skinning is on the GPU and the frame is CPU-bound: no lever on the Studio |
| CPU skinning (meshDeformation 0) | 10.69 | 11.52 | +23 % worse | the game's GPU batched setting (2) is right |
| GPU non-batched (1), 60 people | 1.00 ms frame | 1.19 ms frame | noise to worse | keep GPU batched |
| Animator culling mode | n/a | n/a | n/a | every bench person is on screen, so CullCompletely, which HostViews already uses, has nothing to cull; it only matters off screen |
| stripped + Optimize | n/a | not measured | expected about −20 % | my scratch-stripped prefab broke under Optimize ("Bones do not match bindpose", 50 ms frames). This is an artefact of building the avatar in a runtime-stripped prefab. A Blender-exported 22-bone FBX would not have it. |

How cost scales with crowd size (frame ms, median of runs):

| people | old | new | new, stripped | new + Optimize | old + Optimize |
|---|---|---|---|---|---|
| 0 | 0.70 | | | | |
| 60 | 0.83 | 0.91 | 1.11 (noise) | 0.91 | |
| 150 | 1.79 | 2.39 | 2.10 | | |
| 300 | 3.35 | 3.55 | 3.27 | 2.65 | 2.51 |
| 600 | 5.90 | 6.31 | 5.71 | 5.50 | 5.38 |

## Player settings: bench against game (`unity/ProjectSettings/`, read only)
- **Identical:** QualitySettings (current level 1, PC; `skinWeights` 4), GraphicsSettings, URP settings, TagManager, and in ProjectSettings `gpuSkinning: 1`, `meshDeformation: 2` (GPU batched), `enableFrameTimingStats: 1`, static and dynamic batching off, and graphics jobs at the default.
- **Different, with no effect on the crowd:**
  - product and company name;
  - default resolution: 1920×1080 windowed in the bench, 1024×768 fullscreen-window in the game;
  - the game preloads one asset (input actions);
  - TimeManager is serialised as a rational (141120000/2822399 = 50 Hz, the same 0.02 s step);
  - Audio, Physics2D, Dynamics, Input and VFX asset diffs come from the serialiser version.
- **Different by design:**
  - the bench has no city, no simulation and no physics load;
  - vSync is off and the target frame rate is uncapped;
  - it is a development build, as the game's perf harness is.
- So the per-person costs carry over. The 60-person ms do not carry over as an Air number: the Air's cores are slower and fewer, and it has no fan. A rough ×2 would put the new crowd's extra cost at about +0.08 ms on the Air. That figure is not measured.

## Recommendation for the pipeline (meets the Hold)
1. **Give the crowd figures a 22-bone body skeleton.** The finger weights fold into the hand bone and the modelled fingers stay in the mesh, in a relaxed pose.
   - It has to be a separate crowd export with its own avatar, not only a LOD1 mesh. One Animator drives both LODs, so a LOD1 mesh without fingers saves only the skinning matrices; the retargeter, IK and transform-write costs come from the avatar's finger bones.
   - At 40–60 px the hands still read as hands, not mittens, because the mesh has fingers; they just don't animate.
   - Keep the 52-bone skeleton for heroes, conversation close-ups, portraits and third person.
   - The same names and hierarchy, minus the fingers, mean every Humanoid clip still plays.
   - Measured: −11 % against the new crowd as delivered, and about −4 % against today's crowd. That meets "no more than today".
2. **Turn on Optimize Game Objects for crowd figures,** old and new, and expose only the bones the game reads.
   - The bones to expose are the hands (tray, pistol), the head, and whatever the carry and seat code reads. Latch should list them from HostViews.
   - Measured: −15 % against the new crowd as delivered, −10 % on today's.
   - It is a runtime and import change in Latch's figure code. Check prop attachment before shipping it.
3. **Keep GPU batched skinning, 4 weights and CullCompletely.** The 2-weight setting, the AvatarMask and CPU or non-batched skinning are not levers.
4. **On draw calls:** the single shared material does not cut draws for skinned crowds.
   - Every visible SMR is its own draw, in the main pass and the shadow pass: 124 against 124. It saves one SetPass call (32 against 33) and per-figure texture memory.
   - Fewer draws would need baked vertex animation with GPU instancing. That is out of scope and would lose Humanoid retargeting; I don't recommend it now.
5. **Small:** new LOD1 is 1,520–1,718 triangles against today's 1,500. Forma could cap LOD1 at 1,500 to take back the +5 % GPU triangles. This matters on the Air's 10-core GPU more than on the Studio.

## Open
- Stripped + Optimize together was not measured, because of the scratch-prefab artefact above. Forma's 22-bone crowd FBX would let this be measured cleanly in one 4-minute pass.
- There is no Air number (Sky's OK is needed). Measuring on the Air is the only way to turn the per-person costs into the Hold's 60 fps figure.
- All runs were on a loaded Studio. A rerun on an idle machine would tighten the ranges but should not change the ordering.
