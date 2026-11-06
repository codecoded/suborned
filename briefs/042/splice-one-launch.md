# Brief 042: one player launch per perf step (Splice, 2026-10-26)

Sky saw the game "spin up a lot" on the Studio while `bun run batch --only late-fight,tp-long` ran.

## What was there already

Every step in `tools/game/batch.ts` that starts the built player was **already one launch with its whole list**:

| step | how the player takes the list | launches per step |
|---|---|---|
| shots | `-shots <resolved file>`: every shot in one run (ShotRunner.cs) | 1 |
| late-fight | `-late <resolved file>`: every row in one run (LateRun.cs loops `file.rows`) | 1; a 2nd only when a row with findings is run again on a fresh player (brief 045, on purpose) |
| tp-long | `-tplong <resolved file>`: every row in one run (TpLongRun.cs) | 1 |
| perf064 | one scene, four windows in one run (Perf064.cs) | 1 |

So no list mode had to be added to the player, and no host change is needed for it. The batch log of Sky's run (`out/batch/logs/late-fight.log`, 21:35) has one `$ ... -late ...` line for both rows.

What looked like spinning up: each launch opened its window **at the size the last run left in the player's prefs** (after tp-long or perf064 that is 2880x1864, a 1440x964-point window on the 6K display), on top of the other windows, and then jumped to the step's size a few seconds later (watcher, run `t`: `z0 1440x964` at 1.1 s, then `960x572` at 5.4 s). A full batch also starts the player in other places: the build's proof, game-check's scenarios (`-batchmode -nographics`, no window) and briefing stills (tools/game/cli.ts), plus shots, late-fight and tp-long. That is about five windows, one per check, not one per route.

## What changed (tools/game/batch.ts, batch.test.ts)

- `launchPlayer` / `playerCommand`: every player step starts the player through one helper. It opens **windowed at the step's size from the first frame** (`-screen-fullscreen 0 -screen-width W -screen-height H`: 1920x1080 for shots and late-fight, 2880x1864 for tp-long and perf064; CaptureSize still sets each row's own size). No more jump from the last run's size.
- The count is logged: each launch writes `# player launch N of step <id>` in `out/batch/logs/<id>.log`, prints `<id>: player launch N (direct, 1920x1080 windowed)`, adds `N player launch(es) (k rows)` to the step's detail, and the screen gets a `players late-fight 1 launch · tp-long 1 launch` line.
- `--rows a,b`: late-fight and tp-long play only those rows (a short test of one route). An unknown name is an error. The verdict files are unchanged.
- `--behind` (opt-in): starts the player with `open -g -n -W`. It never takes the keyboard and opens under the window in front. A timeout kills it by PID, found from its `-logFile` path. **Off by default** (see below).

The verdict JSON format is unchanged: `late.verdict.json` keys are still `ok, problems, notes, noisy, limits, attempts` (checked on every run below), and `tplong.verdict.json` and `perf064.verdict.json` are untouched.

## Front vs behind: frame times (same route, both ways)

The route is late-fight `loud-fight`: the last 60 s at real time, 1920x1080, `-novsync`. The runs went through `bun run guard`, `bun tools/game/batch.ts --only late-fight --rows loud-fight [--behind]`, in pairs on the same build, back to back. Front = the direct launch (its window on top). Behind = `--behind` (front app unchanged, window under the front one).

| pair | build | load (18 cores) | front mean / p99 ms | behind mean / p99 ms | behind vs front |
|---|---|---|---|---|---|
| 1 | 0.2.132 | 24.5 / 16.7 | 5.559 / 9.271 | 5.275 / 8.417 | mean -5.1 %, p99 -9.2 % |
| 2 | 0.2.133 | 9.5 / 10.1 | 5.121 / 7.922 | 5.015 / 7.909 | mean -2.1 %, p99 -0.2 % |

The same-mode spread on this machine was larger than the difference. Scratch runs of the same row, all launched in front (direct): mean 4.82 to 5.22 ms, p99 6.52 to 7.22 ms, an 8 % spread. Every run was at load 10 to 50 on 18 cores (NOISY MACHINE by the batch's rule; solana-test-validator at 90 %, Sky's apps and other agents). Behind was never slower. On the quietest pair, though, the mean differed by 2.1 %, just over the 2 % bar. **So the default stays as it was, the window in front, and `--behind` is opt-in** until a run on a quiet machine shows both ways within 2 %. (The player was already not full screen in these modes: CaptureSize runs them windowed. "Keep" here means keep the window in front.)

Focus: in all 10 watched launches (a CGWindowList and frontmost-app watcher, out/scratch/splice/winwatch), the player never became the front app, so neither way takes the keyboard. The difference is the window. Launched directly it opens at z0, over everything. With `open -g` it opens at z1, under the front window (3008x1570 here, so fully hidden). `open -g -j` (hidden) is not usable: Unity's window still came up, and in that run its size changed partway through.

## Time saved, measured

Through the batch, late-fight, build 0.2.133:

- one launch per route: `--rows loud-fight` 75.5 s + `--rows loud-left-downed` 75.8 s = **151.3 s**
- one launch for both rows: `--only late-fight` **141.6 s** (log: one `# player launch 1 of step late-fight`; screen: `players late-fight 1 launch`)
- **9.7 s saved per launch not made** (start, scene, 3 s settle, quit). Scratch check: one row 73.9 s and two rows 139.1 s give 8.7 s.

Measured against today's batch, which already launched once per step, the saving is 0 s. Measured against one launch per route, the batch saves 1 launch on late-fight (9.7 s) and 2 on tp-long (about 19 s). On shots it saves one launch for every shot after the first.

## For Latch (only if wanted; nothing is needed for this brief)

1. **Chain steps in one launch.** This saves about 10 s for each step folded in, so about 30 s a full batch for shots + late-fight + tp-long + perf064. It needs a host change: each runner (ShotRunner, LateRun, TpLongRun, Perf064) hands on to the next mode on the command line instead of calling `Application.Quit`, and the last one quits. I would **not** do it for the perf steps: their frame times would then be taken after a shots tour in the same process (warm caches, GC state), not on a fresh player, and late-fight's re-run must stay fresh. Not recommended.
2. **A window truly behind every other window.** No Unity API orders a macOS window back. It would need a native plugin (`[NSApp.mainWindow orderBack:nil]` after the first frame) and its own frame-time comparison. Not recommended; `--behind` already keeps the window under the front one.

## Evidence

- runs: out/scratch/splice/batch/{f1,b1,f2,b2,one2,both}.{verdict.json,screen.txt,late-fight.log} · scratch rows and watcher logs out/scratch/splice/late-*/
- `bun test tools/game/batch.test.ts`: 40 pass (new: playerCommand, one launch line in each player step's plan, --rows, the players line) · `bun run typecheck` clean
- `bun run test`: 2120 pass, 6 fail, none in tools/game/batch, tools/plan or tools/world. The failures belong to others' work in progress: the engine 0.35.0 WIP (headless Handover, route-diff, quiet reach), ProofFire.cs:117 (comment lint, Latch) and drift-auto (HEAD 1f7d2b51 has no clean base).
