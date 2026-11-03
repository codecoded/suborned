# Brief 069 · handoff from Latch to Redline: the 60-person crowd bench, old against new (done-check 5a)

Latch, 2026-10-26. This is a scratch Unity project at `out/scratch/latch/char-pipeline/unity/`, separate from the game's `unity/`, on the same editor (6000.6.2f1) with the game's URP asset, renderer (with SSAO) and quality settings copied in as files. Nothing in `unity/` was opened or touched.

## What it draws
- **Two crowds of the same 60 people.** The ids are `crowd/0..45` (passers), `office/0..1`, `cafe/0..2`, `guard`, `security/0..2` and `rival-1..5`.
  - **old:** picked from today's 17 the way HostViews does it: FNV-1a by person id within the family pool, the door guard as `guard`, security as `security`, rivals by member number.
  - **new:** each pool picked by the same id from Forma's 50 (`cp_commuter_*`, `cp_cafe_*`, `cp_guard_*`, `cp_rival_*`).
  - The picks are logged in `logs/setup.log` ("Proof: crowd old/new") and stored in `unity/Assets/Proof/Generated/Crowd_{old,new}.asset`.
- **Each person is set up the way `HostViews.Dress` does it:**
  - the FBX instanced as imported (its LODGroup, with the manifest's `lodScreenHeights` applied);
  - an Animator with one controller (a state per clip, built like FigureBuild), `applyRootMotion = false` and `CullCompletely`.
- **Movement:**
  - 20 % stand (idle or idle_alt; guards and rivals: aim_pistol half the time);
  - 10 % run;
  - the rest walk (women walk_f);
  - movers go round small circles at the clip's natural speed, so `anim.speed = 1`.
- **Camera and light:**
  - the play camera: orthographic, zoom 14 at 38°, yaw 30, 1920×1080, SMAA, HDR, post on;
  - the game's tactical shadow fit (`TacticalShadows`, copied unchanged, with light.json's `shadows.tactical`);
  - the street's daylight: the sun 1.8 at pitch 38 / yaw 330, trilight ambient, and the ACES grade with bloom, colour, white balance, SMH and vignette, resolved from light.json;
  - a flat pavement ground. No city geometry: only the people differ between the two runs.
- **Timing:**
  - vSync is off and `targetFrameRate = -1`, so the run measures cost, not the 60 cap;
  - after a warmup it records every frame: unscaled dt, plus `FrameTimingManager` CPU frame, GPU frame, CPU main and CPU render (Frame Timing Stats is on in the player);
  - it writes JSON (mean, median, p95, p99, max) and quits;
  - the JSON's `scene` block carries the skinned mesh renderer count, the distinct materials, the bones per SMR and the triangle sums by LOD.

## Run it: standalone development build (preferred)
The build is already made: `out/scratch/latch/char-pipeline/build/CrowdBench069.app` (development, macOS, 1920×1080 windowed). On the Studio, from the repo root:

```sh
S=$PWD/out/scratch/latch/char-pipeline
for w in old new; do
  bun run guard "$S/build/CrowdBench069.app/Contents/MacOS/CrowdBench069" \
    -crowd $w -warmup 5 -measure 30 -benchOut $S/bench/crowdbench-$w.json -logFile $S/bench/player-$w.log
done
```
- Flags:
  - `-crowd old|new` picks the crowd (default `new`);
  - `-warmup` and `-measure` are in seconds (defaults 5 and 20);
  - `-benchOut` is the JSON path (default: `Application.persistentDataPath/crowdbench-<which>.json`);
  - `-vsync` turns vSync on;
  - `-noquit` keeps the window open after it writes.
- Run each a few times and alternate old and new, so thermal and background load fall on both. Keep the window in front and don't move it.
- For a release-build number, rebuild with `BuildOptions.None` in `CharProof.Player` (one line). The development player carries the profiler hooks and dev overhead on both sides alike.
- The Profiler can attach to the development player for Animator, skinning and draw-call detail (Window → Analysis → Profiler, the CrowdBench069 target).

## Or: the editor's play mode
Open `out/scratch/latch/char-pipeline/unity/` in Unity 6000.6.2f1, open `Assets/Proof/Crowd_Old.unity` or `Crowd_New.unity`, set the Game view to 1920×1080 and press Play. The `bench` object's `which`, `warmup`, `measure` and `outPath` fields set the run; play stops itself after it writes. Editor numbers carry editor overhead, so use them only for Frame Debugger and Profiler inspection, not for the comparison.

## Rebuild from scratch (if needed)
```sh
U=/Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity; P=$PWD/out/scratch/latch/char-pipeline/unity
bun run guard $U -batchmode -projectPath $P -executeMethod Proof.EditorTools.CharProof.Setup  -logFile $P/../logs/setup.log   # import report, controller, crowd sets, scenes
bun run guard $U -batchmode -projectPath $P -executeMethod Proof.EditorTools.CharProof.Player -logFile $P/../logs/player.log  # ~3 min
```

## Smoke run (Latch, Studio, 3 s each: proves it runs, not a measurement)
`smoke/crowdbench-{new,old}.json`:

| | frame ms (median, p95) | GPU ms (median) | SMRs | materials | bones/SMR | tris LOD0 sum / other LODs |
|---|---|---|---|---|---|---|
| old | 0.70, 0.74 | 1.28 | 126 | 37 | 23 | 222,209 / 109,473 |
| new | 1.35, 1.77 | 1.18 | 120 | 1 | 52 | 205,072 / 94,674 |

An early signal to check properly: one material and fewer triangles per person in the new crowd, but its CPU frame is about twice the old one in this short run. The likely cause is skinning and Animator cost for 52 bones (fingers) against 23. If that holds over a real run, the options for Forma are:
- an LOD1 skeleton without finger bones;
- a lower bone count at play zoom;
- (GPU skinning is already on in both runs: `gpuSkinning: 1`, copied from the game.)
