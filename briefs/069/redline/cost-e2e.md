# Brief 069 e2e: frame cost of the five new figures against today's five (Redline, 2026-10-27)

Studio only (M5 Max), development player, GPU-batched skinning (the game's setting), play camera at zoom 14, 38°, yaw 30, 1920×1080, vSync off. No Air run.

## How it was run
- **Bench:** `Assets/Proof/Redline/Runtime/CrowdBenchR.cs` in Latch's scratch project, extended. New figures are dressed by Latch's `FigureRig` (avatar, LOD heights, bag), as his E2E scenes do. New flags:
  - `-crowd e2enew|e2etoday|e2enewnf` picks the set;
  - `-only <figure>` uses one figure;
  - `-forceLod` pins a LOD;
  - `-mipLimit` drops the top mips;
  - `-noProps` leaves the bag off.
- **Scene and player:** `Crowd_R2.unity` (new), built by `RedlineBench.Setup2` and `Build` into `build/e2e/`.
  - `Setup2` also writes the 22-bone strips of the five (`Generated/NoFinger/*_H_nf*`).
  - `ProjectSettings` and `EditorBuildSettings` were checked byte-identical after the builds.
- **Crowd size:** five figures can't resolve a per-person cost, so each set repeats its five figures to 300 people at play zoom or 60 at forced LOD0. µs per person = (frame − empty-scene floor of 0.69 ms) / people.
- **Runs:** every run started through `bun run guard`. 3 s warm-up dropped, 6 s measured, configurations interleaved in rotated rounds. Pass E was 4 rounds (4 min); pass F was 3 rounds (7 min).
- **Load:** the machine was quieter than yesterday (load 7–16). Run ranges are tight; for example, today's 300-person crowd ran 3.10–3.19 ms.
- **Raw data:** `raw/{E,F}/*.json` and `summary-E.json`, `summary-F.json`.

## Per figure, new against today

| | today's five | new five (as delivered) | new, 22-bone strip | new, no bag | new, strip + no bag | new + Optimize Game Objects |
|---|---|---|---|---|---|---|
| **play zoom, µs per person** (300 people) | **8.28** (8.77 in a second pass) | **11.58 (+40 %)** | 10.71 | 10.37 | **9.39 (+7 to 13 %)** | 9.31 |
| LOD drawn at play zoom | LOD1 / LOD2 | LOD2 + the bag (outside the LODGroup) | | | | |
| triangles per person drawn | 1,499 | 1,741 | | 1,577 | | |
| skinned vertices per person drawn | 1,851 | 2,168 | | 2,018 | | |
| skinned renderers per person | 2.4 | 4.4 (3 LODs + bag, + cap on the guard) | | 3.6 | | |
| draw calls, 300 people (shadow pass included) | 604 | **1,204** | | 724 | | |
| bones per renderer / transforms per person | 23 / 27 | 57 / 63 | 25 / 31 | | | |
| **forced LOD0, µs per person** (60 people) | 10.27 (6,780 triangles on average) | **15.02** (45,986 triangles) | 13.48 | | | |
| GPU ms, 60 people at forced LOD0 (5.5 M against 0.8 M triangles with shadows) | 0.97 | 1.24 | | | | |
| materials / textures for the five | 12 / 24 (1K albedo + normal per figure, 512 at the far LOD) | 9 / 5 (one albedo per figure, plus the bag's material) | | | | |
| texture memory per figure, DXT1 with mips | about 2 MB | 2.7 MB as imported. **The 4K source is cut to 2,048 by Unity's default max size.** At 4K it would be 10.7 MB; at 1K, 0.7 MB. | | | | |

## A 60-person play-zoom crowd (per-person costs × 60, Studio CPU frame)
- Today's crowd: **0.50 ms**.
- New, as delivered: **0.69 ms (+0.20 ms, +40 %)**. This fails the Hold.
- New with the 22-bone strip and the bag merged: **0.56 ms (+0.06 ms)**. Adding Optimize Game Objects is expected to bring it under today's. That combination wasn't measured: the scratch strip and Optimize don't combine, as in pass B.
- Where the +40 % goes, at 300 people:
  - **The bag and the guard's cap are separate skinned meshes, outside the LODGroup.** Every person gets two extra draws (main and shadow pass) and an extra skin job at full detail at every distance. That is twice the draws and +0.36 ms per 300. Removing them saves 1.2 µs per person (10 %).
  - **The finger bones:** animation +0.35 ms per 300. The 22-bone strip saves 0.9–1.0 µs per person (8–9 %). This repeats yesterday's finding on the new figures.
  - **The rest:** LOD2 has about 9 % more vertices and 5 % more triangles than today's far LODs.
- Texture size made no frame-time difference at play zoom or at forced LOD0. Dropping one mip changed nothing; dropping two was within noise. Texture size is a memory question, not a frame-time one.

## The cell at 30k, worst case (all four on screen at LOD0)
- The Fixer v7 at 29,116 triangles costs 10.4 µs per person at forced LOD0, against 7.1 µs for today's Fixer at 12k.
- Four on screen: **0.042 ms against 0.028 ms of Studio CPU**, and about 233k triangles drawn including shadows.
- This is negligible on the Studio, and 30k for the cell is safe. The skirt-rigged Fixer (76 bones) wasn't measured; it isn't adopted.

## Recommendations
1. **Crowd LOD0 budget: about 12k triangles,** today's guard and hero level, with LOD1 about 4.5k and LOD2 1.5k or less.
   - Today's 47–50k LOD0 cost +46 % CPU per person when forced. They also draw 7× the triangles: on the Studio GPU, 5.5 M against 0.8 M for 60 people, +0.27 ms.
   - Only LOD0 is drawn in the street-level view, where 20–30 people can be close. At 50k each that is 2–3 M triangles with shadows, which belongs to the Air's 10-core GPU, not the Studio's 40-core one. That Air figure is an estimate, not measured.
   - 30k stays for the cell only (Sky's rule).
2. **Textures: 1K for crowd figures,** 2K for the cell and heroes. Keep the 4K source in the store.
   - There was no frame cost either way, but every generated figure brings its own unique texture.
   - 50 crowd figures cost about 530 MB at 4K, 135 MB at 2K and 34 MB at 1K.
   - A crowd figure is 40–60 px at play zoom, so 1K is already more than its LOD0 close view needs.
   - Set the import max size explicitly: today the 4K files are silently cut to 2K.
3. **Yes, the crowd still needs the 22-bone variant** (fingers folded into the hand). It was measured again on these five: −8 to −9 % per person. The 52 bones stay for the cell, heroes and close-ups.
4. **New, from this pass: merge the bag, the cap and other carried or worn props into the body mesh at each LOD for crowd figures.** At minimum, put them in the LODGroup with far-LOD versions.
   - As delivered they double the draw calls and are skinned at full detail at every distance.
   - Props that must come and go (tray, pistol) stay separate and attached, as in the game.
5. **Optimize Game Objects for crowd figures** (expose hands and head for props): −20 % on the new figures as delivered. That is Latch's figure code, as I said yesterday.

With recommendations 2–5 the new crowd should meet the Hold (no more than today's). Before adoption, a confirmation pass on a Forma build with a 22-bone skeleton and merged props takes about 4 minutes.
