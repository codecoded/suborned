# Brief 069 take 3: confirmation pass (Redline, 2026-10-27)

Studio only (M5 Max), development player, GPU-batched skinning, play camera at zoom 14, 38°, yaw 30, 1920×1080, vSync off. No Air run.

## The sets compared
- **new:** Forma's take-3 five, as Latch imports them: Optimize Game Objects on, exposing `Head`, the hands and the five `Attach_*` bones.
- **today opt:** Latch's optimised copies of today's five.
- **today raw:** today's five as the game imports them now, unoptimised.
- **crowd4:** the four crowd figures without the Fixer, set against today's four without today's Fixer. The crowd answer comes from crowd4.

## How it was run
- Bench: `CrowdBenchR` in a new scene, `Crowd_R3.unity`, built by `RedlineBench.Setup3` and `Setup4` into `build/take3/`. Two flags are new: `-exclude <figure>` and the `e2etodayraw` set. `ProjectSettings` and `EditorBuildSettings` were byte-identical after the builds.
- Each set repeats its figures to 300 people at play zoom. µs per person = (frame − empty floor of 0.688 ms) / 300.
- Every run started through `bun run guard`. 3 s warm-up dropped, 6 s measured, configurations interleaved in rotated rounds. Pass G was 4 rounds (5.5 min); pass H was 3 rounds (5 min). Load average was 7–10.
- Raw data: `raw/G/`, `raw/H/`, `summary-G.json`, `summary-H.json`.

## Table

| | today raw (the game now) | today opt | **new (take 3)** | new without `Attach_*` exposed |
|---|---|---|---|---|
| **play zoom, µs per person, crowd4** (pass G / pass H) | **6.75** | **5.72 / 5.73** | **6.21 / 6.34** | 6.03 |
| play zoom, µs per person, all five (Fixer included) | 6.76 | 5.89 | 6.57 | n/a |
| **60-person crowd (crowd4 × 60)** | **0.405 ms** | 0.344 ms | **0.377 ms** | 0.362 ms |
| paired per 300 people: new − today raw (4 rounds) | n/a | n/a | −0.22, −0.19, −0.11, −0.21 ms | n/a |
| paired per 300 people: new − today opt (7 rounds, G + H) | n/a | n/a | +0.19, +0.10, +0.19, +0.11, +0.19, +0.11, +0.09 ms | +0.01, +0.10, −0.01 ms |
| forced LOD0, 60 people: frame (µs per person) | 0.791 ms (1.72) | 0.774 ms (1.43) | 0.799 ms (1.85) | n/a |
| forced LOD0 GPU, 60 people | 1.12 ms | 1.19 ms | 1.12 ms | n/a |
| triangles drawn per person: play zoom / LOD0 | 1,499 / 5,475 | same | 1,500 / 12,000 | same |
| skinned vertices drawn per person: play zoom / LOD0 | 1,792 / 4,456 | same | 1,966 / 9,058 | same |
| draw calls, 300 people (shadow pass included) | 604 | 604 | 604 | 604 |
| skinned renderers per person (1 visible) | 2.2 (2–3 LODs) | 2.2 | 3.0 (3 LODs) | 3.0 |
| materials per figure | 2–3 (12 for today's five) | same | 1 (5 for the five) | same |
| bones in each skin | 23 | 23 | 27 = 22 + 5 `Attach_*` (Fixer: 57) | 27 |
| transforms per person after optimise | 27 (not optimised) | 6.3 | 12 | 7 |
| texture memory per crowd figure (DXT1 with mips) | about 2.2 MB (1K albedo, 1K normal, 512 far albedo) | same | **0.7 MB** (one 1K albedo) | same |
| texture memory for the five | 17.0 MB | 17.0 MB | **5.3 MB** (Fixer at 2K: 2.7 MB) | same |

- The empty-scene floor is 0.688 ms. Forced LOD0 at 60 people sits only about 0.1 ms above it, so the LOD0 differences (≤ 0.03 ms per 60 on the CPU, none on the GPU) are inside noise.
- What matters about LOD0 is that it is now 12k against today's average of 5.5k, and its cost is negligible.

**The Fixer, forced LOD0, 60 people:** 0.898 ms against 0.789 ms for today's optimised Fixer (29k against 12k triangles). With all four of the cell at LOD0 that is about +0.007 ms of CPU: negligible.

## Verdict on the Hold: PASS
- **Against today's crowd as the game draws it, the new crowd is cheaper:** 6.2–6.3 against 6.75 µs per person, −7 to −8 %. Every paired round was in its favour. For 60 people that is 0.377 against 0.405 ms on the Studio. The Hold's "no more than today's, ideally less" is met.
- **Like for like (both optimised), new costs 8–10 % more per person** (+0.03 ms per 60 people). Most of that is the five exposed `Attach_*` transforms, which the Animator writes every frame:
  - `Animator.WriteTransforms` is 0.201 against 0.148 ms per 300 people.
  - Without them exposed, new is at parity with today opt: +0.03 ms per 300 people, about +2 %, within noise.
  - The rest is about 10 % more vertices at the far LOD.
- Draw calls are equal. Materials and texture memory are 3× lower.

## The `Attach_*` question (Latch's minor note)
- **Exposing them is what costs.** Exposing only `Head` and the hands saves 0.3 µs per person (−5 %), and that brings new level with optimised today.
- **Recommendation for Latch's import rule:** expose the `Attach_*` bones only on figures whose role carries a prop. For example, `Attach_LeftHand` on café staff for the tray, and the pistol hand on guards and rivals. Others get `Head` and the hands only. Alternatively, expose them lazily when a prop is attached. Not having the transforms costs nothing.
- **Dropping them from the skin cluster** (Forma) removes 5 of 27 matrices per skin. That isn't measured separately: with Optimize on, the skin matrices come out of the Animator's own job, which hardly moved between new and lite (1.464 against 1.404 ms). I expect ≤ 1 %. Do it when convenient in the generator, as tidying; it isn't worth a pass.

## Next
- **Latch:** the import rule changes above, Optimize with exposure by role, when the figure code goes into the game. That also gives the game's existing figures the 15 % Optimize win.
- **Air:** these per-person numbers are Studio only. An Air check of a play-zoom crowd needs Sky's OK.
