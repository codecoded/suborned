# 106 · Unity's free built-ins on the Air: results (Latch, 2026-11-11)

**In short:** Mesh LOD is on, and it is the one that wins. On the MacBook Air at full screen it takes 2.0 ms off Sky's street in third person and 1.8 ms off the worst heading. The play view stays the same. GPU occlusion and STP both work, and both make every view slower on the Air, so they stay off. Each one is still a launch switch, so they can be tried again without a rebuild.

Build for Sky: **0.2.313** (c064, f1c806eb9+), `out/build/play/The Suborned.app`. Made by `bun run batch --sky --then-full` in 893 s: unity-world took 727 s with the levels added after the bake (MeshLod106: 119 meshes); the build and smoke took 56 s. The smoke was OK: OldTown with the host, the briefing, play to tick 40, 20 HUD elements, the pause menu, no exception. The full batch is running behind (out/batch/full-behind/verdict.json). The --sky ran over its 600 s target because it carried a town rebake.

## How it was measured
- **Machine:** the 15-inch MacBook Air M4. Sky OK'd this one sitting.
  - Run at 11:49–12:03 over SSH. The script is `air/run-air.sh`; the rows come from `air/make_rows.py`.
  - On AC power, no thermal warning at the start or the end.
  - Load was 3.4 at the start and 5.1 at the end (`air/machine.log`). Chrome was open on the Air; I left it alone.
- **Screen:** full screen as the game opens it. On this Air that is **3840 × 2410**: the display is set to a scaled "more space" mode, so RenderCap already draws the 3D pass at 0.75 (2880 × 1808) and upscales it.
- **Player:** a release scratch build, 0.2.313-scratch. It has the same content as the build for Sky; the scratch build takes no build number.
- **Harness:** `-tplong`, the batch's own draw-distance harness (`Runtime/Play/TpLongRun.cs`). It is the one wired to the OldTown host, with a play row and the two third-person rows.
  - Runtime/Perf's `-perf` needs a plan resolved in the Editor and drives the HarbourDistrict rig, so I didn't use it.
  - The rows sit on the batch's tp-long rows: play at zoom 10, yaw 30; tp-street; tp-worst. Vsync is off, the clock is running and the HUD is on.
- **Timing:** every row sets all three switches. Each row settles for 3 s, then records 8 s.
  - Two repeats, in the opposite order the second time, so the Air warming up doesn't fall on one variant.
  - The table gives the mean of the two repeats. Each repeat's own numbers are in `air/tplong.json`.
- **Draw calls:** Unity's own "Draw Calls" and "Batches" counters can't count the GPU Resident Drawer's draws. They read 0 in a development player and don't exist in a release one.
  - So the table gives SetPass calls (draw-state changes) and renderers drawn (the harness's census) instead.

## Off vs on, MacBook Air, full screen (`air/table.md`)

| view | variant | median ms | p95 ms | GPU ms | main ms | triangles | SetPass | renderers drawn | median vs off |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| play | all off (as before 106) | 11.0 | 14.7 | 15.8 | 9.1 | 1.24 M | 151 | 216 | — |
| play | GPU occlusion on | 12.2 | 15.9 | 15.3 | 10.7 | 0.66 M | 227 | 216 | +1.1 (+10 %) |
| play | **Mesh LOD on (threshold 16)** | 11.2 | 15.6 | 15.9 | 9.9 | 1.24 M | 151 | 216 | +0.1 (noise) |
| play | Mesh LOD on (threshold 8) | 11.2 | 15.5 | 16.7 | 9.9 | 1.24 M | 151 | 216 | +0.2 |
| play | STP, 3D pass at 75 % | 15.9 | 19.9 | 19.1 | 10.7 | 1.85 M | 164 | 216 | +4.9 (+44 %) |
| play | STP, 3D pass at 70 % | 16.0 | 20.4 | 19.6 | 11.1 | 1.85 M | 164 | 216 | +5.0 (+45 %) |
| play | all three on | 16.3 | 20.6 | 24.4 | 11.8 | 0.72 M | 240 | 216 | +5.3 |
| tp-street | all off | 17.6 | 21.9 | 22.6 | 15.2 | 6.33 M | 311 | 1,508 | — |
| tp-street | GPU occlusion on | 18.7 | 23.8 | 19.3 | 19.5 | 2.10 M | 414 | 1,508 | +1.1 (+6 %) |
| tp-street | **Mesh LOD on (threshold 16)** | **15.6** | **20.2** | 20.3 | 16.1 | **4.34 M** | 311 | 1,508 | **−2.0 (−11 %)** |
| tp-street | Mesh LOD on (threshold 8) | 17.3 | 21.9 | 22.6 | 15.5 | 5.86 M | 311 | 1,508 | −0.3 |
| tp-street | STP 75 % | 24.4 | 27.8 | 24.0 | 13.3 | 10.50 M | 361 | 1,508 | +6.8 (+38 %) |
| tp-street | STP 70 % | 24.3 | 28.1 | 24.1 | 14.1 | 10.48 M | 361 | 1,508 | +6.7 (+38 %) |
| tp-street | all three on | 23.3 | 27.0 | 32.2 | 16.0 | 1.42 M | 464 | 1,508 | +5.7 |
| tp-worst | all off | 18.2 | 22.8 | 23.0 | 16.6 | 8.22 M | 298 | 1,612 | — |
| tp-worst | GPU occlusion on | 22.6 | 30.1 | 15.9 | 23.4 | 2.24 M | 410 | 1,612 | +4.3 (+24 %) |
| tp-worst | **Mesh LOD on (threshold 16)** | **16.5** | **20.3** | 21.1 | 16.3 | **6.40 M** | 298 | 1,612 | **−1.8 (−10 %)** |
| tp-worst | Mesh LOD on (threshold 8) | 17.8 | 22.1 | 22.4 | 15.9 | 7.52 M | 299 | 1,612 | −0.5 |
| tp-worst | STP 75 % | 26.3 | 29.0 | 25.7 | 12.5 | 14.19 M | 340 | 1,612 | +8.0 (+44 %) |
| tp-worst | STP 70 % | 26.5 | 29.6 | 26.3 | 14.1 | 14.17 M | 340 | 1,612 | +8.2 (+45 %) |
| tp-worst | all three on | 23.4 | 31.1 | 22.5 | 24.4 | 1.90 M | 451 | 1,612 | +5.2 |

GPU ms on Metal is the span of the frame's command buffers, and they overlap on a tile GPU. So it can exceed the median frame: read it as an upper bound (PerfRecorder's note).

## What each switch did, and what is left on

### Mesh LOD: ON, threshold 16
- **What it covers:** the ground chunks: the street's 48 m chunks, Southside's, and the tram network's rails and kerbs.
  - The network's rails and kerbs were 2,907 separate renderers. They are now merged into 66 chunks by the same tile merge (`DecoKitTileMerge`).
  - That family (Tile_TramRail and Kerb_Granite) was tp-street's most-drawn far piece: 309 + 453 far renderers in the 0.2.311 census.
  - 119 chunk meshes now carry levels: 6.45 M triangles at level 0, 159 k at their last level.
- **Unity's default threshold does nothing here.** At 1, Unity's selector never left level 0 for these big flat chunks.
  - On the Studio (`studio/tplong-diag-dev.json`), tp-street read 6.28 M either way. Forcing the last level gave 4.05 M, so the levels do draw.
  - Threshold 4 gave 6.13 M, threshold 8 gave 5.86 M, threshold 16 gave 4.34 M.
  - 16 is the build's default (`BuiltIns.DefaultMeshLodThreshold`; `-meshLodThreshold` changes it). Only these chunks carry Mesh LOD, so nothing else moves.
- **Look:** I found no visible change at threshold 16. The near pavement, the far kerbs and the street end read the same.
  - Stills: `stills/tp-street-base-vs-t16.jpg`, `stills/tp-street-far-base-vs-t16.png` (far street, at 2×) and `stills/tp-worst-base-vs-t16.jpg`.
- **Play view:** unchanged (1.24 M). The play view sees the zone's own ground, which isn't in these chunks.
- **Why on:** −2.0 ms in tp-street and −1.8 ms in tp-worst on the Air, p95 down by about 2 ms, and play unchanged.
- **Brief's target:** tp-street is now under 6 M (4.34 M on the Air, against 6.33 M). tp-worst is still over (6.40 M, against 8.22 M).
- **A deviation from the brief:** the brief asked to move one kit family from LOD Groups to Mesh LOD. I gave Mesh LOD to the network's rail and kerb pieces instead, which had no LOD at all.
  - The kit families that do have LOD Groups (palms, facades, alley walls, shop fills) already carry authored levels at 3–20 %, which Sky has reviewed. Swapping those for automatic levels would cost reviewed art for little gain.
  - The rails and kerbs were the biggest far-triangle source with no levels.

### GPU occlusion: OFF
- It cuts triangles hard: tp-street 6.33 → 2.10 M, tp-worst 8.22 → 2.24 M, play 1.24 → 0.66 M.
- But SetPass rises by 75–100 and the main thread grows by 1.6–6.8 ms. The median gets slower in every view: +1.1 ms at play, +1.1 ms in tp-street, +4.3 ms in tp-worst.
- The GPU time does fall in third person (tp-worst 23.0 → 15.9 ms), so the cost is on the CPU side.
- This matches what Redline measured in brief 048 on the Studio (+36 % in tp-worst).
- `-gpuOcclusion on` turns it on for a launch.

### STP: OFF
- It works on Metal. The log reads `filter STP`, and the 3D pass is drawn at 0.55 of 3840 × 2410 on the Air.
- **It costs, it doesn't save.** STP needs motion vectors, which is a second pass over the scene (tp-street draws 10.5 M triangles, against 6.3 M), and its upscale runs at the full screen size. The fewer pixels don't pay for that.
  - +4.9 ms at play, +6.8 ms in tp-street, +8.0 ms in tp-worst.
  - 70 % and 75 % cost the same.
- **Readability:** it holds. STP is not why it's off.
  - Same frame, paused, STP off then on. 2× crops, off on the left, on on the right:
    - `stills/cell-rings-moving-people.still.2x.png` and `.moving.2x.png`: the cell's rings and markers, and people walking;
    - `stills/lamp-camera-brass.moving.2x.png`: the café camera with its eye mark, and the brass hairline on the café band;
    - `stills/bench-brass-car.*.2x.png`;
    - `stills/armillary-brass.*.2x.png`: the fountain's thin gold armillary.
  - Full frames: `stills/play.stills.*`, `stills/play-y210.stills.*` and `stills/tp-street.stills.*` (`.still.` = the same frame, `.moving.` = the clock running).
  - The rings, the eye mark, the outlines and the brass hairlines read as well as or crisper than the bilinear upscale. People in motion leave no trails.
  - The one artefact is a fine fizz along the armillary's thin rings. They turn in real time even while the game is paused, so that pair isn't quite the same frame.
- `-stp on` and `-stpScale 50–100` turn it on for a launch.
- The build keeps STP's shaders (`BuiltInsBuild`). Unity strips them when no pipeline asset selects STP, so without this a `-stp on` launch would have done nothing.

## Something Sky and the crew should know: Mesh LOD and the Editor's probe bake
With the Mesh LOD levels inside the chunk meshes, the swap's reflection-probe bake (`DecoKitStage.BakeEnvironment`) stalled the GPU. Unity went silent for about 80 s, then macOS killed it (exit 128).
- This happened three times. The third time the renderers were pinned to level 0, and it still happened.
- With no levels in the meshes, the bake runs as before.
- **The fix:** the levels are now added to the saved chunk meshes **after** every Editor bake. `ZoneBake097.BakeTown` calls `MeshLod106.Apply` once its bake is saved, and `-executeMethod Suborned.World.Play.MeshLod106.Cli` adds them on their own (about a minute). Only the index buffers grow.
- **For Glint, or anyone who bakes OldTown.unity's lighting or reflection probes in the Editor:** expect the same stall. Rebake the town through `unity-world` instead. A runtime player is a different matter: every Air and Studio run drew the levels without trouble.
- **Cost of finding it:** three rebakes died at about 6 minutes each, and one bisect ran in full (695 s). With the build for Sky that makes five unity-world runs, not the one the brief allowed.

## Switches, all at launch or per `-tplong` row
- **At launch:**
  - `-gpuOcclusion on|off`;
  - `-meshLod on|off`, `-meshLodThreshold <n>`;
  - `-stp on|off`, `-stpScale <pct>`.
- **Per `-tplong` row:** variant fields `occlusion`, `meshLod` (`low` = force the last level, a check), `meshLodThreshold`, `stp`, `stpScalePct`, and `stills: "on"` (STP's same-frame pair, then the pair in motion, as PNG).
- **New in each row's report:** `medianMs`, `p95Ms`, `drawCalls` and `batches` (−1 when Unity can't count them), and `builtIns` (what each switch was during the window).
- **Not saved in Options.** These are switches for testing, not settings for players.

## Files changed
- **New:**
  - `unity/Assets/Suborned/Runtime/Play/BuiltIns.cs` (97 lines): the three switches and their defaults;
  - `Runtime/Play/TpLongStills.cs` (50): the STP stills;
  - `Runtime/Play/TpLongRun.Census.cs` (77): the census, moved out of TpLongRun.cs by topic;
  - `Editor/World/Play/MeshLod106.cs` (54): levels after the bakes;
  - `Editor/World/Play/BuiltInsBuild.cs` (42): STP's shaders kept in the build;
  - and their `.meta` files.
- **Changed:**
  - `Runtime/Play/TpLongRun.cs` (618 → 554 lines: the new row fields, median, p95, draw calls, builtIns, the stills hook);
  - `Runtime/Play/TpLongVariants.cs` (the built-in switches per row; `upscale` now goes through RenderCap);
  - `Runtime/Play/RenderCap.cs` (the STP factor, and the filter in use set every time the scale is set; a row's own filter as an override);
  - `Runtime/Play/CaptureSize.cs` (a row of size 0 × 0 keeps the screen as it comes, for full screen);
  - `Runtime/Play/TpLongGpu.cs` (STP and occlusion passes in the GPU split);
  - `Editor/OldTown/DecoKit/DecoKitTileMerge.cs` (`keep` filter);
  - `Editor/OldTown/Swap/OldTownSwap.cs` (merges the network's rails and kerbs after the tram audit);
  - `Editor/World/Play/ZoneBake097.cs` (calls MeshLod106 after its bake);
  - `Editor/World/Play/GameBuild.cs` (one line: `BuiltInsBuild.CarryStp()` around BuildPlayer);
  - `unity/Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` (Unity registered STP's runtime resources);
  - `tools/game/proofmap.json` (the three new runtime files placed).
- **Unchanged:** `PC_RPAsset.asset` stays as it was. Its filter is Automatic and occlusion is off.
- **Evidence:**
  - `briefs/106/latch/air/`: tplong.json, rows.json, machine.log, table.md, run-air.sh, make_rows.py;
  - `briefs/106/latch/studio/`: the Studio Mesh LOD check;
  - `briefs/106/latch/stills/`: the stills. Under briefs these are git-ignored and stay on disk.

## Checks
- `runtime-compile` is clean. Host logic passes 202/202. The tp-long tool tests pass 8/8, and the proof-map tests 73/73 with 0 unplaced paths.
- Unity: the Editor code compiled in every batch run, and MeshLod106 ran (rc 0).

## Open
- tp-worst is still over 6 M triangles (6.40 M). The next lever is draw distance on the old art's far pieces (brief 086's slice).
- At full screen the Air plays at about 90 fps in the play view (11.2 ms) and about 64 fps in tp-street (15.6 ms). tp-worst's median is 16.5 ms, just over the 60 fps line, and its p95 is 20 ms.

Next owner: Aster (commit), then Sky (play).
