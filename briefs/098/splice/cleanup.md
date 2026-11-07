# 098 cleanup: the batch places every change, the kit copy as Unity owns it, the budget's book and caps, the kit pin (Splice, 2026-11-08)

Sky approved the cleanup ("Do all"). Worktree `t3/batch-proof-kit-hud` from `work/098-living-playbook` at `0f75f128`.

## 1. Delta only: the batch places every change

**What was wrong.**
- On the L9 merge the batch found 353 changed paths no row of `tools/game/proofmap.json` placed (the route scripts, the adaptive plans, the population rules, `PlayStandOff.cs`, the engine's CLI). One was enough to run the whole proof and every step, named by the first path alone.
- The engine's and the kit's rows were `full` with no `steps`, so an engine change also ran the words, kit, world, Unity world rebuild (about 12 min) and draw-distance steps, which it cannot affect.
- An ordering fault: the world row (`tools/world/**`, full) came before the tests row, so editing a world test ran everything.

**What changed.**
- **Rows for every path of today's tree.** 135 rows became 163. Each new row names its readers, found by search of `tools/game`, `tools/sim/unity.ts`, `tools/world`, `tools/plan`, `tools/kit` and Unity's `Runtime` and `Editor`. The main ones:
  - The engine, its data and copies: the whole proof, and steps `sim, unity-match, scenarios, late-fight` only.
  - The engine's CLI: those four steps and no section (the player does not carry it).
  - The population and people rules and the rulebook's split schemas: on the rulebook's row (the engine's steps; the proof runs on their copies).
  - Route scripts: none, except the two the shots play (`route-a-board` and `route-g-board` retake their shots only). The adaptive plans and the people generator's inputs: none.
  - The world's sources (the plan, the civic block and look, the zone kit, `tools/plan`, `tools/geometry`, the style contract): `world, unity-world, late-fight, tp-long`, not the engine's or the kit's steps.
  - The kit: `kit, world, unity-world, late-fight, tp-long`.
  - The play camera's stand-off and the presets: the whole proof, no gated step. The project settings and packages: everything. The render pipeline and the shaders: the whole proof plus the frame-time steps.
  - The figures (`People/**`, git-ignored): FigureLibrary's sections and every shot.
  - A folder's `.meta` (Unity marks it `folderAsset: yes`): nothing.
- **Never silent.** A path no row places still runs the whole proof and every step (the fail-safe). The plan, each step gate and the shot gate now name every such path (`plan.unplaced`, "N changed paths the map cannot place (…)"). The batch's dry run and screen show them.
- **`bun tools/game/proofwhat.ts`** (new): `diff <from> <to>` shows what a commit range would run (sections, steps, shots), `what-if <path>…`, and `unplaced` lists the tree's paths with no row (exit 1). `--map <file>` judges with another map, for a before-and-after.
- **Tests** (`tools/game/proofwhat.test.ts`, new):
  - every tracked path, and every file of the ignored folders present, has a row;
  - the last row takes only folder metas;
  - a shot's route script, pinned script or checkpoint sits on a row that names the shot (this found `tools/game/pins/loud.seed3.json`: fight-loud-body's script, mapped as a late row's pin; and `briefs/027/checkpoints/cp-loud`, which had no row);
  - the gated steps are `batch.ts`'s;
  - an engine change, the engine's inputs, the CLI, data the batch never plays, a route a shot plays, the world's sources, the kit and the stand-off each run what the rows above say.
- **The engine's DLL no longer carries the commit.** `tools/sim/unity.ts` builds with `-p:IncludeSourceRevisionInInformationalVersion=false`. The SDK stamped HEAD into the DLL (`1.0.0+0f75f128…`), so any sim-step run after a commit changed the DLL's bytes and read as an engine change: a seats or cues copy, or a CLI edit, ran the whole proof. The scratch build reads `1.0.0`. Nothing reads the version.

**Dry run on the L9 merge** (`bun tools/game/proofwhat.ts diff 655435cf~1 0f75f128`, 91 paths):

| | before (the map at 0f75f128) | after |
|---|---|---|
| placed | 81 of 91 (10 unplaced: five route scripts, `sim/cli/Bodies.cs` and `Dial.cs`, the leaf shader, the deleted debrief stand-ins) | 91 of 91 |
| proof | full (unplaced) | full (an engine change: every section plays on it) |
| steps | all nine | sim, unity-match, scenarios, late-fight, tp-long (the leaf shader); not words, kit, world, unity-world |
| shots | all | all (the rulebook) |

**One path at a time** (`what-if`, before → after):

| change | before | after |
|---|---|---|
| engine (`sim/Suborned.Sim/GameBodies.cs`) | full proof, all nine steps | full proof; sim, unity-match, scenarios, late-fight |
| route script no shot plays (`routes/route-a.json`) | full, all steps (unplaced) | nothing: no section, step or shot |
| route script a shot plays (`routes/route-a-board.json`) | full, all steps (unplaced) | its eight shots only |
| adaptive plan (`adaptive/quiet-a.adaptive.json`) | full, all steps (unplaced) | nothing |
| population rules (`contracts/rules/population.json`) | full, all steps (unplaced) | the engine's four steps; the proof runs whole on the copy the sim step writes |
| engine CLI (`sim/cli/Dial.cs`) | full, all steps (unplaced) | the engine's four steps, no section |
| camera stand-off (`PlayStandOff.cs`) | full, all steps (unplaced) | full proof, no gated step, every shot |
| kit (`contracts/world/kit.json`) | full, all steps | full; kit, world, unity-world, late-fight, tp-long |

The step times, from the last full batch's logs on the Studio: the Unity world rebuild with the old town's swap and bake about 12 min, Unity's scenario match about 7 min, late-fight about 3 min, tp-long about 1 min.

## 2. The build no longer dirties the snapshot

**What happened.** `DK_Glass_Sheen.mat` is a kit copy that Unity owns once imported (`tools/kit/copy.ts` says so for every `.mat` and `.meta`). The game build's own material pass (`Look097Materials.Apply`: look097.json sets its colour to alpha 0.06 and its render queue to −1) and Unity's re-serialisation rewrote it after the batch's snapshot: 0.2.263 recorded `187f838e…`, the file became `12a0bc89…`, and 0.2.264's proof ran full for no source change. Today it differs from the catalogue's copy in exactly those fields and the `_Color` mirror Unity writes beside `_BaseColor`.

**The fix: the snapshot reads what Unity owns by what it was made from.**
- `proofdelta.ts` `kitFiles`: a kit `.mat` or `.meta` reads as the catalogue hash it was copied at (`out/kit/copied.json`, which the kit step already keeps). One Unity made itself (a prefab's or a model's meta, a colourway variant) is left out: its sources (kit.json, look097.json, the Editor code) are hashed already. Models, textures and prefabs stay hashed by their bytes.
- `changedPaths` reads a ledger written before this (Unity's bytes) and the new reading as the same file, so the first batch after the change does not run full for it.
- Not fixed at its source: `Look097Materials` (Editor, Latch's) still writes the material at every build that changes a row. It is now harmless to the delta. Latch could also set the `_Color` mirror when the row sets `_BaseColor`.

## 3. The HUD budget's caps and the book

- **Caps from the text schema** (`tools/hud-budget/speech.ts`, new):
  - a bubble: text.schema `MAX_WORDS` (12);
  - a conversation's line, one of text.json's `scenes.conversations` beats: `CUTAWAY_MAX_WORDS` (25), `CUTAWAY_MAX_SENTENCES` (2) and `CUTAWAY_MAX_QUESTIONS` (1);
  - the cards: `CARD_WORDS` a row, and as many rows as the schema's `learned` and `opens` allow (read from the zod schema: 4).
  - No copy of a number. With no text.json to read, every line is judged as a bubble (the stricter answer).
- **The book state** (`tools/hud-budget/book.ts`, new; `--state book`). A `page` manifest that carries the book's section signs is read as the book, with a warning, until the host writes `book` (`HudToolkit.BookManifest`'s default is `page`). Its rules:
  - B1: every word on the page is in the manifest, and a picture may not cover a word.
  - B2: at most 220 words.
  - B3: every piece at least 14 px from the screen's edge (the clock chip's inset), and the body inside the grid's 48 px margins. The frame may sit in the margins: the running head and clock above the body (BookLayout `Top` 60), the ways on below its floor (`AgainFloor` 952), and the signs. These numbers are tested against BookLayout.cs and HudToolkit.Book.cs.
- **Verdicts on real captures:**
  - Latch's nine 099 captures: every one failed the 070 page's rules (65 faults on the lookout page); now 8 of 9 pass. The two reader artefacts (a kicker listed a letter at a time, two listed words joined across a gap) count as listed.
  - The start page fails, for real: it reads 229 words (cap 220), and the dev dial ("THE DIAL · DEV") sits 4 px into the grid's left margin.
  - The 0.2.265 batch shots: 5 of 11 within budget, now 9 of 11. The two left are real, as asked: the loud fight's two cards (brief 101) and route A's refusal pop (Waymark).

## 4. The kit pinned for the pre-remap tests; the eleven retired towers rejected

- **The pin** (`tools/world/test/preremap.ts` and `preremap.kit.json`, new). kit.json at `0f75f128` with its file lists emptied (they are only for the copy and the importer), one asset a line, its sha256 held in the module, so an edit fails loudly.
  - `prePlan()` pins it for the rest of a test file (kitrefs `pinKit`; `afterAll` takes it off).
  - `withPreKit(fn)` pins it for one call, in files that also generate the live plan (`world-rules`, `kit-fit`, `colourways`).
  - `preKit()` is for tests that look up a piece the pre-remap world places.
  - 27 test files were moved onto it. Mutated catalogues (faces, roadfinish, partkit) and the export subprocess in gen.test start from the pin.
- **Proof.** With the pin and the kit as it was: Vela's ripple set (59 files) gives 759 pass and 1 fail (Pulse's presentation count, failing before too).
- **The rejection.** The eleven are rejected in `contracts/world/kit.json`, each with its reason. `bun run kit --catalogue` changed only those rows and dropped `Skyport_Tower_Portal` from the landmark livery. `REHOMED` in `tools/decokit/orders.ts` is empty.
- **Data refreshed with their own commands:** the cover file's kit hash (`bun run cover`), two kit.test.ts lists (the crown-gold pieces 31 → 24; the livery's Skyport tower is `Skyport_Tower`), and fit.test.ts (adopted pieces against the orders; the dropped ones rejected with their reason).
- **The skyline tests keep the eleven.** Their own fixture plan, cut from the district before the remap, stands the towers at their sites. Regenerating their digest from the live kit failed 5 skyline tests on NP-1.
  - Their digest is now made by `bun tools/levels/skyline/test/digest.ts`: the live catalogue's digest, with the pieces the pre-remap pin keeps adopted kept adopted. It reproduces the committed fixture byte for byte.
  - fixture.test.ts holds the file to that command (it used to say "regenerate from kit.json", which would now break the skyline tests).
- **Result.** None of the 25 moves again: the ripple set gives 753 pass and 7 fail before those refreshes, and the refreshed files then pass (cover, kit, fit: 53 of 53; skyline and levels: 105 of 105).

## Checks
- **`bun run test`** (this worktree, through the guard, with the main checkout's git-ignored inputs cloned in: out/world, out/people, out/decokit, Kit, People):
  - before, at 0f75f128: 2,648 pass, 9 skip, 9 fail of 2,666 (5:19);
  - after: 2,672 pass, 9 skip, 9 fail of 2,690 (5:15).
  - The same nine fail before and after; 24 tests are new. The fast loop left 18: Latch's looks and leaf shader (3), Fable's conversations, Latch's comment lint, Vela's fit (2), the mission record (c059) and simview have passed since.
- **The nine left, by owner** (all failing at 0f75f128 too):

| test | why | owner |
|---|---|---|
| `tools/decokit/collide` | BikeRack_Play's collision volume sits 1 cm past the 15 cm the test allows (−0.47 against −0.46) | Vela and Keel |
| `tools/people/test/day` | the r013 cast's first failing check is now the waitress's (no secret affair among the five who fit), not the door's | Pulse |
| `tools/people/test/trust` | another mission text gives the same people-file hash | Pulse |
| `tools/life/presentation` (counts) | one presentation walk loop drops (both pavements blocked on Sunset Way east) | Pulse |
| `tools/routines/rounds` | the rounds candidate is not what the bake makes today | Pulse |
| `tools/drift-auto` (2) | the people ledger's and the dialogue ledger's rule moves are undeclared | Pulse (people), Fable (dialogue) |
| `tools/sim/sim` | sim/data/handover.people.json is not what buildPeople makes from today's plan | Keel and Pulse |
| `tools/route-diff` | route-a-board ends in failure on the working-tree engine (the test expects success) | Keel and Waymark |

- **`bun run plan`**: exit 0, no rule fails.
- **`bun run typecheck`**: clean.
- **The planner's dry run on the L9 merge**: 91 of 91 paths placed (was 81). The full output is in section 1 (`bun tools/game/proofwhat.ts diff 655435cf~1 0f75f128`).
- **`bun run batch --dry-run`**: plans as before (no ledger in this worktree: a full proof).
- **The kit transition against the batch's real ledger** (a copy of the main checkout's `out/build/play/proofledger.json`):
  - since the 0.2.263, 0.2.264 and 0.2.265 passes, nothing under Kit/ reads as changed, `DK_Glass_Sheen.mat` included;
  - since the older shots and tp-long passes (0.2.231, 0.2.161), 286 kit paths changed, all of them real: 21 new catalogued materials, and 265 models and textures new or rebuilt.
- **The DLL without the commit stamp**: a scratch Release build of `sim/cli` with the new flag reads `1.0.0` (the main checkout's DLL read `1.0.0+0f75f128…`).
