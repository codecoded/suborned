# 034 · Iteration tools: route-diff, drift:auto, batch

**Status (2026-10-21): tools 1 and 2 built and proven; tool 3 built and everything that can be proven without Unity is proven; the Unity parts of tool 3 have not run.** Owner: Splice. Asked by Sky (2026-10-21): cut token cost and speed iteration. Coordination: Latch is mid-batch in the Editor, so nothing here opened, drove or wrote into the Unity project. Next owner for item 3: Latch (first real run), then Suture (shot rows).

Why: three kinds of work were being done by hand and read back by agents. Keel and Waymark hand-traced which routes moved after an engine change; whoever changed code a verdict rests on worked out which of four ledgers to declare and with what words; Latch wrote a scratch C# script per round to take a picture. Each is now one command with one table.

## 1 · `bun run route-diff` (built, proven)

Replays every recorded Handover route (`contracts/missions/handover/routes/*.json`, the set `tools/sim/unity.ts --routes` plays) on two engines, headless, and prints one table of what moved. Default: HEAD against the working tree. Plays are cached by DLL bytes + inputs + script text (`out/route-diff/cache`), so the side that did not change costs nothing the second time (a full run is 3 to 6 s).

```
bun run route-diff                          HEAD (its engine, its inputs, its scripts) vs the working tree
bun run route-diff --scripts tree           both sides play the tree's scripts: only the engine moved
bun run route-diff --matrix                 also the fight-lab matrix's prepared rows (P-A, P-G, P-B, today and lab; --matrix-runs L1,L2,P-A to choose)
bun run route-diff --a <commit> --b <commit>
bun run route-diff --a old.dll --b new.dll [--a-root <dir> --b-root <dir>]    two built suborned-sim.dll, inputs from the tree or the given roots
bun run route-diff --a-scripts <dir> --b-scripts <dir>                        a recomposed set against the committed one
flags: --only id,id  --all (list unchanged too)  --json <file>  --no-cache  --fail-on-move (exit 1 if anything moved)
```

Columns: outcome, fail code, end time, Trail, Alert, health lost (percent of the cell's 18, read from the event log's "N health left" lines, since the play report carries no health). Unchanged values print `=`. Under each moved route, the first log line where the two runs part, with both lines. Notes: "the route script differs between A and B" (so part of the move is the script's); a log line that is only reworded is not a move; hashes count only when both runs began from the same start state (the start state hashes the inputs, so any input edit changes every hash).

Tested: `bun test tools/route-diff` (12 tests): each column, wording-only, hash rule, added/removed, health from the log (also a lab start), the matrix parser; with the real engine, the tree against itself moves nothing and a route script made 5 s late moves. Real run today (HEAD against the uncommitted engine, tree scripts on both): 37 of 38 routes move; the table shows where (for example `loud`: health lost 33 % → 61 %, first log line apart at 3:09.0). HEAD's engine has no `fight-lab`, so `--matrix` against HEAD lists the tree's six rows as "only on B" and says so.

## 2 · `bun run drift:auto` (built, proven)

When `bun run verify` fails on "CHECKER CODE changed": finds the ledgers that need a declaration (**people, skyline, dialogue, transit**; there is no fifth ledger: the plan checker, `tools/plan`, is part of transit's hash), lists the files that changed behind each (the files of that ledger's hash, against HEAD), and declares each with a cause that names them (`--cause` to override). `--check` reports and writes nothing (exit 1 if a declaration is needed).

```
bun run drift:auto --check
bun run drift:auto [--objective people,dialogue] [--cause "words"] [--base <rev>]
```

**It never declares a drift that changes a replay's result.** For each objective with a failing run it replays that objective's runs at the base state (HEAD, with HEAD's own ledger, in `out/drift-auto/base/<commit>`) and at the working tree with the declaration in place provisionally (a scratch ledger through `LEVELS_LEDGER`), then compares line for line, with the code line reduced to "rules whose result differs" and hashes taken out. Any run that still fails, or whose replay differs from the base's, refuses the whole objective and prints the lines apart (`-` at HEAD, `+` now) and the rule ids no cause names. A declaration it makes has `--rules none`, carries the earlier causes forward (as `declare` does), and is confirmed by replaying the objective's runs under the real ledger. A run whose own recorded state is not in the ledger is a record problem; it says so and declares nothing.

Tested: `bun test tools/drift-auto` (15 tests, 33 s). Pure reading of replay lines is built from the real `codeLine`, so a wording change there breaks the test. End to end on a scratch copy of the repo with its own git: nothing changed → nothing to declare; an edit to the dialogue checker that changes D-7 → refused, nothing written; an edit to the people checker that changes P-3 → refused with "P-3" named; a comment appended to a dialogue file → `--check` names the file and writes nothing, the real run declares it and the three runs replay OK, and a second `--check` finds nothing. Also seen by hand: a comment in `tools/people/needs.ts` fails people and skyline (their standing causes move rules for old runs) and is declared as unmoved against HEAD.

## 3 · `bun run batch` (built; Unity parts not yet run)

```
bun run batch                    all steps, stop at the first failure, one screen at the end
bun run batch --dry-run          what each step would run; validates tools/game/shots.json; touches nothing
bun run batch --from unity-world | --only sim,world | --skip kit | --no-shots | --rebuild | --editor | --batchmode
        --shots <file>  --shots-out <dir>
```

Steps: `sim` (`tools/sim/unity.ts --tree`) · `compile` · `kit` · `world` · `unity-world` (reimport + HarbourWorldBuilder rebuild from `out/world/harbour-district.world.json`) · `unity-match` (Unity's scenarios against headless) · `game-build` · `game-check` · `shots` · `budget` (HUD budget + layout audit). The screen: a line per step (ok / FAILED / skipped / not run, seconds, detail; a failure's last lines), the build (`out/build/play/The Suborned.app`, version, recording, commit), frame times (play zoom, the slowest of the spots, each shot's perf sample), where the shots are. Logs: `out/batch/logs/<step>.log`.

Two additions to the order you gave, both cheap: **`compile`** compiles all of `unity/Assets/Suborned/Runtime` with dotnet against Unity's own reference assemblies and the project's `Library/ScriptAssemblies` (1.5 s, no Editor; `bun tools/game/runtime-compile.ts`), so a script that does not compile is found before Unity is asked for anything; and **`game-check --reuse-proof`** (a flag I added to `tools/game/cli.ts`): `game:build` already takes the first-frame proof, which is the same 4-minute `-gameProof` run that `game:check` takes again, so the check now uses the proof the build just took when it is complete and newer than the build (`bun run game:check` alone is unchanged).

**Unity, two ways, chosen by whether the Editor has the project open** (as `tools/game/cli.ts` does; `--editor` / `--batchmode` force one). Editor open: through the Unity CLI's menus (`Assets/Refresh`, `Suborned/World/Build Harbour District (Export)`, `Suborned/Sim/Run Scenarios (Unity vs headless)`), refusing while in Play, waiting on the scene file and `out/sim-unity/unity-editor.json`, and reading the Editor log from where the step started for `error CS…` and exceptions. Closed: `Unity -batchmode -quit -executeMethod …HarbourWorldBuilder.Build -world …` and `…SimScenarios.RunBatch`.

### The shot runner

`tools/game/shots.json` is the data (`tools/game/shots.schema.ts` documents every key); `tools/game/shots.ts` validates it (a bad row stops the batch before the build, naming the shot) and resolves it to a flat file; **`ShotRunner.cs` is the one runner in the player** (`The Suborned -shots <resolved.json> <dir>`; it registers itself, so `GameBoot.cs` is untouched). Per shot: start (the plain start, a route script's orders given as a click gives them, a fight-lab checkpoint, or an engine scenario exported as a script), tick, day/night, overhead or follow, the point to look at (a view key such as `op/scout`, `waitress`, `car/getaway`, `camera/entrance`, or engine cm), yaw, zoom, a free pose camera, agent selected, verb armed, cursor, Intel, settle time, a frame-time sample, HUD on (frame + HUD layer + manifest + parts, then the layout audit) or off (world only), or a clip of n seconds at real time (frames 1/fps apart). Shots that share a start go on in one run while their ticks do not go back. It writes `<dir>/shots.json` (status, files, audit, frame times per shot) and exits 0 only if every shot was taken and every audit was clean. The default `shots.json` has 10 rows (briefing, opening day and night, café front, the Records Office front at yaw 210, Pay armed, the follow view, the getaway car, the loud-fight checkpoint, a 3 s clip) and one skipped example (a free side camera, what `gamecap2.cs` did by hand).

Not covered, deliberately: `crowdcap.cs`'s per-frame foot-slip log is an analysis, not a shot; it stays a scratch script until it is wanted again.

### What Latch needs to know (I changed nothing under `unity/`)

- **`tools/game/unity/ShotRunner.cs` is staged outside the Unity project** so that no file in `Assets/` changed under your open Editor. The first `bun run batch` (the `compile` step) moves it to `unity/Assets/Suborned/Runtime/Play/ShotRunner.cs` and deletes the staged copy; Unity imports it and makes the `.meta`. From then on the Assets copy is the source. To install by hand: `cp tools/game/unity/ShotRunner.cs unity/Assets/Suborned/Runtime/Play/ && rm tools/game/unity/ShotRunner.cs`.
- It compiled against the current Runtime sources and Unity 6000.6.2f1's assemblies with dotnet (a deliberately broken copy fails with file and line). It has **not** run in Unity or in a player. I read `HandoverHost`, `HudToolkit`, `GameProof` and `Shots033` for every call it makes; the things most likely to need a first-run fix are the view keys in the default rows, whether `FightLab.Dir("checkpoints")` finds `briefs/027/checkpoints` from inside the built app (the checkpoint row reports its error in `shots.json` if not), and `Time.captureFramerate` pacing for the clip.
- `tools/game/cli.ts`: only `--reuse-proof` added (a 12-line change in `check()`).
- `HudToolkit.AuditText()` is what the runner uses for the layout audit, as `Shots033` does.

## Done-check and evidence

| | command | result |
|---|---|---|
| route-diff | `bun test tools/route-diff` | 12 pass |
| drift:auto | `bun test tools/drift-auto` | 15 pass (e2e on a scratch repo, two refusals, one declaration) |
| batch | `bun test tools/game` | 27 pass: stop at first failure, step picker, screen, shot judgement, Unity match file, runner install, shots.json rules (every cross-rule has a failing row), runtime compile with a broken script |
| all | `bun run typecheck` | clean |
| nothing else moved | `bun run verify` | VERIFY OK (18 runs replayed, 66 play shots within budget); none of the new files is in a ledger's hash |
| dry run | `bun run batch --dry-run --editor` and `--batchmode` | every step planned, `shots.json` valid (10 shots) |

## Open

1. **Latch:** first real `bun run batch` once the Editor is free (say whether it should run with the Editor open or closed; I wrote both). Expect to adjust view keys and a few default rows. Nothing in the Unity steps has been run.
2. `unity-world` in Editor mode detects completion by the scene file's mtime settling, and failure by scanning the Editor log; a build that fails without logging `error CS` or an exception would look like success. Batchmode mode uses the exit code.
3. Done (coordinator, 2026-10-21): `game-build` is skipped, no build number spent, when nothing under `unity/Assets`, `ProjectSettings` or `Packages` is newer than the app's `build.json`; the step line and the summary say NOT REBUILT. `--rebuild` forces it. The first batch after the shot runner is installed rebuilds once (the install touches `Assets`), which is right: the runner is in the player.
4. `drift:auto` compares against HEAD. If HEAD itself fails the same replay (someone committed code without declaring), the lines differ and it refuses with the diff; pass `--base <an earlier verified commit>` or declare by hand.
5. `route-diff --scripts side` (the default) mixes script changes with engine changes, flagged per route; use `--scripts tree` to isolate the engine.

## 4 · Captures are 1920 × 1080 (Sky, 2026-10-21: shots were 20 to 26 MB at the Studio's native 6016 × 3384)

- **One seam, so every helper follows.** `HudToolkit.Shoot` (the frame, and the HUD layer drawn into a render texture of the screen's size), `GameProof`'s shots, `Shots031`, `Shots033` and `ShotRunner` all capture at the **screen's size**, none sets its own. So the fix is the screen's size, not each helper. New `CaptureSize.cs` (staged beside `ShotRunner.cs`, installed with it by the `compile` step; no existing file edited) sets the window to 1920 × 1080 windowed for any run that takes pictures (`-gameProof`, `-shots`, `-shots031`, `-shots033`). The HUD's whole-number scale is then 1, the size the HUD budget and the layout audit are defined at, so they measure there. `-capture-size WxH` picks another size; `-capture-native` (`bun tools/game/cli.ts --check --native`) keeps the display's own. Not covered by it: `PerfHarness` (plan-sized, its own resolution) and the Editor's `Capture.cs` (caller-sized).
- **`shots.json`:** every row is 1920 × 1080; `size: [w, h]` overrides it for a detail shot; `format` is png or jpg. HUD shots stay PNG (the budget reads the frame and its layer lossless; the schema refuses jpg for them); world-only shots and clip frames are JPEG quality 90. `bun run batch`'s shots step reads each PNG's header and fails a shot that is not the size its row says (a display that would not give 1920 × 1080 is named, not shipped).
- **Retina:** the project has `macRetinaSupport: 1`, so a window of 1920 may come out 3840 pixels wide. `CaptureSize.Apply` measures what it got and asks again for the size that gives exactly the one wanted. Not yet seen in a player.
- **Frame times are the Air's size, pictures stay 1080p (coordinator, 2026-10-21).** The limit is 60 fps on the 15-inch Air, so `game:check` runs the proof a second time with `-capture-size 2880x1864` (into `out/build/play/proof-air/`, whose pictures are not kept) and puts those frames in `proof.json`, labelled `framesAt` and `framesNote`: "on the reference MacBook Air" when `hw.model` is Mac16,13, otherwise "an Air-sized proxy on <model>, not the Air's own GPU". The pictures' own run is kept as `picturesFrames`. The batch screen prints the label. `--native` runs everything at the display's own size, `--capture-size WxH` everything at that size, `--no-air` takes the frames at 1920 × 1080. **Cost:** a second ~4-minute proof run per `game:check` (skipped when `proof.json` already carries Air-sized frames); a one-pass version needs `HudToolkit.ShootCo` to switch the window to 1080p around each picture, a small edit to Latch's file that I did not make. Not run in a player yet.
- **`bun run shots:shrink --check`** (ready, not run; `--apply` writes, `--jpg` converts, `--list` shows all): today it finds **938 screenshots wider than 1920, 8.3 GB → about 1.7 GB (estimate), saving about 6.6 GB**, mostly `out/perf` (17 MB each) and `briefs/022/latch`. It leaves alone: files git tracks (4), HUD layers (320) and the shots that have one (320, 3.3 GB: history, left as they are), square atlases and textures, `out/decokit*`, `out/build`. Method: macOS `sips`, resample to a temp file, verify width, move over the original, keep the modified time. `--jpg` also lists the briefs and reports that name each PNG, since the file name changes. Tests: `bun test tools/game` (sips run on generated files only). Old budget sets stay as they are (history; the resampler is not wanted).
