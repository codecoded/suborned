# 098 fast loop: `bun run test` and the C# suite (Splice, 2026-11-07)

## `bun run test`
- **Before:** 2,577 pass, 3 skip, 53 fail of 2,633 in 7:50, on the Studio through the guard (`out/scratch/splice/test-before.log`).
- **After:** 2,635 pass, 3 skip, 18 fail of 2,656 in 6:06 (`out/scratch/splice/test-final.log`). Every one of the 18 is named below with its owner. In a checkout without the generated inputs, the run fails 15 of the same and skips the rest with their reasons.

### Fixed (stale against the tree as it stands; the repo's own tools)
| Test | What was stale | Fix |
|---|---|---|
| `tools/plan/road-pavement` (2), `pylon-clear` (1), `block-grammar` (1), `frontage-square` (1) | Since 075's plan-match, the plan carries the opt-in fields: two roads' own pavements, the pylon walk clause, the composed massing, the square-frontage clause. The tests still read "the committed plan has none". | Each "without it" case now strips the field from a copy of the plan. The committed plan's own values are asserted too. The walk example is four pylons now, not two: since the S1 reroute through the SpaceHQ drum, two S1 trunk pylons by Crown Circus also stand in a clear walk without the clause. |
| `tools/levels/skyline/test/fixture` (1); `skyline` (3) and `units` (2) after it | The kit digest fixture was behind kit.json. | Regenerated with the command the test prints. |
| `tools/levels/skyline/test/apply` (2) | "SpaceHQ is rejected": the 075 swap adopted SpaceHQ again. | The rejected example is now RecordsTower, which is still rejected. |
| `tools/levels/test/toolhash`, `skyline/test/units` (code hash), `dialogue/test/dialogue` (code hash) | `tools/people` imports `tools/names/ban.ts` (brief 089). The dialogue verdict reaches the Handover text schema's own schemas and the rulebook's split schema. | Both added to the hash lists. The dialogue list takes `contracts/missions/handover/*.schema.ts` and `contracts/rules/rulebook.schema*.ts` by name, so a new one (Fable's `debrief.schema.ts`, not yet committed) joins without an edit. |
| `tools/levels/skyline/drift.json` | r011 (recorded Oct 14) replayed with "CHECKER CODE changed" and 12 rules undeclared. | Declared, as the owner of the plan checker and world rules: 075's old-built world and 097's zone pieces. r011's verdicts recompute unchanged; `bun run levels:replay r011` passes. |
| `tools/world/rules/overlap` (2), `tools/world/oldmatch/match` (1) | 097's audit re-laid the east-wall-s block (/2 and /3 now stand side by side), so the pinned building pairs no longer touch. | The examples now take their pair from the export: two masses touching along an edge, every StreamEnd wrap from the taller into the lower. The footprint example uses /0, which has no twin. |
| `tools/world/gen/kit020` (1) | **A real regression.** 097 audit pass 2 put a `//` comment mid-line in `tools/kit/tags.ts`, which swallowed the use tags of Roof_Pool, Roof_LapPool, Roof_Tennis and Roof_LatticeMast. So no pool or court roof got its kit piece. | Restored on their own line. The lattice mast is now marked retired (`:full`), as the comment meant, so it is still not placed. kit.json rewritten with `bun run kit --catalogue`: only those four rows' tags change. |
| `tools/cover/cover` (3) | The cover file pinned kit.json's old hash. | `bun run cover`: only `from.kit` changes. The points, blockers and refuge are identical, and the sim's site hashes only those. |
| `tools/life/presentation` (1 of 2) | The "never read" check matched a doc comment. Keel's `GameTrafficHost.cs` names the presentation file for its car models' kinds; it does not read it. | The check reads code, not comment lines. |
| `tools/levels/test/levels` (11), `replay-cli` (1), `tools/names` (1) | They fail in the 7:50 baseline and pass since. The levels CLI loads every objective, dialogue included, while its schemas were mid-edit; names followed text that has since changed. | Nothing to fix. They pass in the final run below. |

### Named, with owners (not mine to change)
| Test | Why it fails | Owner |
|---|---|---|
| `tools/simview/simview` (1), `tools/missions/check/check` (1), `tools/route-diff/route-diff` (1) | The engine has moved past the last mission record (c055) with brief 098's traffic: final hashes differ, quiet-route bands move ("B-11 quiet-b" and others), and route-a-board fails on the working-tree engine. Re-recording would accept those, so it is a judgement, not a refresh. | Keel and Waymark: review, then `bun run missions:rerecord` (c056) |
| `tools/sim/sim` (1) | `sim/data/handover.people.json` is not what `buildPeople` makes from today's plan: station places moved in the 075 swap (s1-gardens, t1-rocket). Rebuilding changes the engine's inputs. | Keel and Pulse (rebuild with `bun run sim:site`, then re-record) |
| `tools/routines/rounds` (1) | The rounds candidate is not what the bake makes today (the café entrance's span moved 40 cm). It feeds the engine. | Pulse (`bun run rounds`) |
| `tools/people/test/day`, `trust`, `tools/life/presentation` (counts) | The r013 cast's first failing check is now the waitress's, not the door's. Another mission text gives the same mission hash. One presentation walk loop drops ("both pavements blocked" on Sunset Way east, whose own 3.25 m pavement came with 075). | Pulse |
| `tools/people/test/looks` (1) | A new figure pick in the host (`crowdKeys[i], PersonFigure(w.P)`) that the replay audit does not list. | Forma and Latch |
| `tools/kit/leaf-shader` (2) | DK_LitCutawayLeaf.shader is not what `make_leaf_hlsl.py` writes from today's DK_LitCutaway.shader (the banner cloth keyword came in this morning). The test names the fix: re-run the generator. It is a shader change. | Latch |
| `tools/decokit/fit` (2) | The plan has carried one regen order (SpaceHQ_Portal) since the 075 swap. kit.json still holds eleven generated towers with the orders the swap dropped, and SpaceHQ_Portal was not rebuilt to the swapped order (hash differs). | Vela (rebuild SpaceHQ_Portal with `bun run decokit --only SpaceHQ_Portal`; retire or re-home the eleven) and Datum |
| `tools/decokit/collide` (1) | BikeRack_Play's collision volume sits 16 cm outside the rebuilt piece. 097 pinned collision to the old pieces so the engine's bands would not move. | Vela and Keel (move the volume, or let the test know the pin) |
| `tools/drift-auto/drift-auto` (2) | The people ledger (r012–r016: rules P-6, P-7 and P-11 to P-16 moved or added) and the dialogue ledger (r017–r019: the checker reruns differently under Fable's uncommitted text work) need a person to name the rule moves (`bun tools/levels/<objective>/drift.ts declare …`). Skyline is declared (above). | Pulse (people), Fable (dialogue, once the text work lands) |
| `contracts/missions/handover/conversations` (and, in earlier runs, `tools/style-agent/short`) | Fable's text and style-agent work in progress. | Fable |
| `tools/lint/comments` | `Runtime/Play/HudToolkit.BookStart.cs:13` has code swallowed into a comment (Latch's work in progress; the same mistake as the tags line above). | Latch |
| `tools/game/proofdelta` (an earlier run; passing in the final one) | "HostDebrief.cs has no row": a new host file (briefs 099/100) that the proof map did not list yet. | Latch |

### A checkout without the generated inputs
I ran the suite in a temporary detached worktree: today's changes overlaid, no `out/`, no kit models, no Library. It failed 21 more tests and 3 files at load: ENOENT on `out/world`, `out/people`, `out/decokit` (models, materials, manifest) and `out/decokit-097-extra`.
- **The fix:** `tools/needs.ts`. A test that reads a git-ignored input calls `needs(path, command)`. When the path is missing, it registers a skipped test naming the path and the command that makes it, and prints the same line, because bun's report names no skipped test. For example: `tools/world/tram-topology.test.ts: skipped what reads out/world/harbour-district.world.json (git-ignored; \`bun run world\` makes it)`.
- **Guarded files:** `tools/geometry/legs`, `seethrough`; `tools/decokit/cars067`, `cars070`; `tools/kit/kit`, `extra`; `tools/sim/solids`, `stands`; `tools/people/test/needs`; `tools/world/tram-topology`.
- **Result:** the fresh worktree now fails 15, all among this checkout's own failures above (same causes and owners), with 67 skips and no load errors.

## The C# suite: `bun run sim:test`
- **What it does:** builds `sim/Suborned.Sim.Tests` once in Release, then runs it in shards from the built DLL (`tools/sim/shards.ts`). Shards go by test method, then by theory case for the five 473-case theories and the four slow culture cases, about total ÷ (2 × jobs) seconds each, the longest first. Each shard is single-threaded; 16 run at once on the Studio.
- **Planning data:** the shards are planned from the last run's per-test times (`out/sim-tests/timings.json`). A fresh checkout plans from the committed `tools/sim/shards.seed.json` (per-method cases and seconds; `--write-seed` rewrites it).
- **Time:** 126 s wall for 3,260 tests (1,391 s of tests). As one Debug process the suite outlasted 30 minutes; as one Release process it waited on its longest class. The floor is the slowest single test, `RedlineTests.E7` at about 120 s per culture.
- **Result:** 3,259 passed, 1 failed: `RulesTests.Q4GridMatchesTheTypeScriptEvaluator(mercenary, help)` expects (20000, 2, not-yet) and gets (50000, 1, turned). Keel's 098 report has it failing identically on the base commit. Owner: Keel.
- **Commands:**
  - `bun run sim:test`: through the guard; exit 1 on any failure; TRX files and `summary.json` in `out/sim-tests/<time>/`.
  - `--class A,B`, `--plan`, `--jobs N`, `--debug`, `--no-build`.
  - `bun run check` now ends with it.
  - Noted in CLAUDE.md (Checks stay under 10 minutes) and the README's command table.
- **Why the DLL:** `dotnet test <project> --filter` goes through MSBuild, which rejects a theory case's quotes and brackets. `dotnet test <dll>` takes the same filter as written.
