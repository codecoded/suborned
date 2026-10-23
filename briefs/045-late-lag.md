# 045 · Late lag: every order in the final fight replays the whole mission

**State:** measured, 2026-10-22. Owner: Redline (code-performance-reviewer). Next owners: Latch (host fix), Keel (engine closing hash, gate query), Splice (the batch check).

## Why
Sky played the Handover on the Mac Studio (M5 Max, 6016 x 3384 display, full screen, vsync 60 Hz): "the game went very laggy towards the end". The app was 0.2.58 (Player.log; Sky said 0.2.56). There were two sessions in one process:
- session 1 ended `Failure (rivals-reach-vale) at 7:03.2`
- session 2 ended `Failure (enforcer-down) at 3:28.9`

Both scripts are in `briefs/045/runs/`.

## Objective
Find what makes play slow late in the mission, with numbers. Name the fix and its owner, and add a check so it cannot come back.

## The cause: `HandoverHost.Save()` replays the whole run on the main thread while a public fight is open
`Save()` (HandoverHost.cs:1336) runs on every order (`Issue`, line 909), every 5 s of clock (line 254), on pause, restart and quit. While `Game.OpenFight != null` it calls `ClosingHash()` (line 1380), which is `Engine.Play(PlayInputs, Game.Start, orders, Game.Tick)`: a full replay from tick 0 to now, run synchronously in that frame. The cost grows linearly with mission time. The comment there says "~50 ms at the Handover's length". At 7 minutes in the player it is several hundred ms per save, and the getaway's public fight is exactly where Sky gives orders fastest.

`End()` adds three more full replays in the frame the mission fails: `Engine.Replay` for "replay identical", then `FailCamStart` builds `g` and `g2`, each a replay to the deciding tick.

### Evidence
- **The player, sampled in the final fight** (`sample` on the built player, managed frames named through `mono_pmip`; tools in `briefs/045/probe/`). These are 6 s main-thread samples, late window from tick 3950. Only clock saves happened in them, because the shot runner gives no orders inside a perf window:

  | run | main-thread samples | `HandoverHost.Save → ClosingHash → Engine.Play` | time |
  |---|---|---|---|
  | native 6016 x 3384 | 1407 | 169 (12.0 %) | ≈ 0.72 s in 6 s |
  | 1920 x 1080 | 1471 | 157 (10.7 %) | ≈ 0.64 s in 6 s |

  In the early window (tick 700, no fight open) it is 0 %. A 6 s window holds one or two clock saves, so each save costs **≈ 0.3–0.7 s at tick ~4000** in the player. Files: `briefs/045/runs/el-*.{early,late}.main-thread.txt`.
- **The same, headless** (`briefs/045/probe/`: replays a script and times `ClosingHash` exactly where the host calls it; .NET 10, so a lower bound on Unity Mono). Output is in `briefs/045/runs/save-replay-probe.txt`:
  - Sky's session 1 has 8 full replays in the last 30 s (7 on orders), 150 ms mean and 273 ms max each in .NET.
  - Sky's session 2 has 79 replays from 1:00 to 3:28 (61 on orders), 35–57 ms each in .NET. That session averaged 51.6 fps against 58.4 for session 1 (frames ÷ wall time from the `.end.txt` files and Player.log).
  - loud-fight has 36 replays over its last 3 minutes, growing from 68 ms to 188 ms each.
- **The end of the mission** (1080p, a window that ran into the failure): `HandoverHost.End` is 534 of 2261 samples, about 1.9 s in one frame (`FailCamStart` 356, `Engine.Replay` and `Save` the rest).
- **What is not the cause:**
  - The engine step stays cheap: 0.127 ms mean and 6.7 ms max in Sky's own `.end.txt`. Headless it is 0.05 ms at the start and 0.2 ms in the final fight.
  - Frame times with the clock running stay flat over Sky's whole session replayed at 1080p: 4.5 ms early, 5.5–7.9 ms in the final fight (`runs/rt1.shots.json`).
  - The HUD suspects are bounded:
    - `FireLog`: 6000, capped.
    - `DoneChips`: 4, and the chip elements are pooled.
    - `Pops`: 6. `Talk`: 200. `FeedHistory`: 60.
    - `ShotFx`: 24. `Pulses`: 12.
  - Nothing grows per frame.

## Smaller costs found on the way (every frame, early and late)
1. **`DoneFrame → Gates()` every frame** (HostDone.cs:84). It makes 3 `LearnQuery("scout","listen",role)` calls. Each one runs `Intercept`: up to 120 `Grid.Route` A* searches along the person's round. That is 5.5–8 % of the main thread, about 0.5–1.3 ms a frame. Headless it is 0.46–0.66 ms a frame in the final fight, 7.6 ms max.
2. **The GPU Resident Drawer rebuilds its draw lists every frame.** `CPUDrawInstanceData.RebuildDrawListsIfNeeded` is 12–16 % of the main thread, plus `MeshRendererProcessor.DestroyInstances` every frame late. Renderers are being enabled, disabled or destroyed every frame. Suspects: `ArchitectureFade`/`FadeReach` twins, and `HostViews` show/hide.
3. **Native 6K leaves no headroom at 60 Hz.**

   | | mean frame | GPU |
   |---|---|---|
   | 6016 x 3384, early | 15.6 ms | 14.7 ms |
   | 6016 x 3384, late | 17.6 ms | 15.0 ms |
   | 1080p, early | 8.7 ms | 5.2 ms |
   | 1080p, late | 9.8 ms | 5.3 ms |

   All with `-novsync`. The machine was shared with Blender and Chrome, so GPU numbers are noisy. Under vsync any late extra halves the rate. This is not what Sky hit, but it is the next thing he will.

## The fix
| # | What | Owner |
|---|---|---|
| F1 | No full replay on the main thread in play. On order and clock saves while a fight is open, write the script without the closing checkpoint, or compute `ClosingHash` on a worker thread (Engine.Play on shared Inputs is thread-safe: `sim batch` does it on every core), coalesced so only the latest save runs. Compute it synchronously only on pause, quit, restart and end. Replays stay identical: the hash is the same, only when it is taken changes. | Latch |
| F2 | `End()`: move `Engine.Replay` (the "replay identical" check) to a worker, and let the end card fill it in when it lands. `FailCamStart` builds `g2` with a second replay to `failTo`; read the operative's position from `g` stepped on to `failTo` (or one replay to `failTo` that keeps the earlier tick's state) instead. Target: no frame over 50 ms when the mission ends. | Latch |
| F3 | A closing hash without a replay: `Game.ClosingHash()` that hashes the state as `Finish()` would leave it (the open fight closed) without mutating the live game. Then `Save` costs O(1) and F1's worker is not needed. | Keel |
| F4 | `Gates()` once per tick (when `Game.Tick` or `Game.Log.Count` changes), not every frame. | Latch |
| F5 | A cheap gate read. Listen's gate needs only "would it be taken", not the intercept path. Or cache `Intercept` per (op, person, reach, tick). | Keel |
| F6 | Find what toggles renderers every frame (the GRD rebuild). | Latch |
| F7 | Cap the 3D render size on large displays (URP render scale so the 3D pass is at most about 2880 x 1864 pixels, HUD native). | Latch |

## The check, so it cannot come back
Today's checks miss it. GameProof samples 10 s at the start of the mission. The shot runner's `perf` window gives no orders and reports only mean and p99, so a save stall on an order is never inside a measured window, and a clock-save stall is one frame that p99 hides.

1. **Player (Splice, with Latch for ShotRunner):**
   - In a `perf` window, keep giving the script's orders at their ticks, as play does.
   - Add `maxMs` and `over50msFrames` to `shots.json`.
   - New batch row `late-fight`:
     - Scripts: `loud-fight` and `briefs/045/runs/sky-0.2.58-session1.script.json`.
     - Window: the run's last 60 s, 1920 x 1080, `-novsync`.
   - Fail on any of these: a frame over 50 ms, p99 over 16.7 ms, or a frame over 100 ms in the 2 s after the mission ends.
   - About 2–3 minutes of player time.
2. **Headless (Keel, seconds, in `bun run test`):**
   - Use `briefs/045/probe` as the starting point.
   - For loud-fight and Sky's session 1, at every order tick while a fight is open, `Game.ClosingHash()` must equal `Engine.Play(...)`'s closing checkpoint.
   - The same call must cost under 1 ms at the run's last tick. A full replay costs 70–270 ms, so this catches any return to it.

## Done-check (for this brief)
- The cause, with numbers, named to a line: done.
- Fixes with owners: done.
- The check: specified above, to be built (Splice, Keel).
- After F1–F2: rerun `briefs/045/probe/el.sh` (the early and late windows) and the `late-fight` row. In the sample, `Save` must be under 0.5 % of the main thread in the final fight, and no frame may exceed 50 ms.

## How it was measured (reproducible, under 10 minutes each)
- **Session replay in the built player through the shot runner.** Shots at every order tick, with `perf` filling the gaps:

  ```
  "The Suborned" -shots <resolved.json> <dir> -novsync [-capture-native]
  ```

  Resolve a shots file with `resolveShots` from `tools/game/shots.ts`. The result: `runs/rt1.shots.json`. The replay reproduced Sky's ending at 7:03.2.
- **Early and late profile.** `briefs/045/probe/el.sh <run> [-capture-native]` runs 12 s windows at tick 700 and tick 3950 and calls `sample` on the main thread. `lldb` then calls `mono_pmip` to name the managed frames, and `incl.py` sums them inclusively.
- **Headless.** From `briefs/045/probe`: `dotnet build -c Release && dotnet bin/Release/net10.0/probe.dll <repo> <script.json>`.
- Nothing in `unity/`, `sim/` or `tools/` was edited. Scratch output is in `out/redline/`.

## The check as built (Splice, 2026-10-22)

**Tools side, done.** `bun run batch` has a last step, `late-fight` (`--only late-fight` runs just it; `--dry-run` prints it). Files: `tools/game/late.json` (the rows), `tools/game/late.ts` (resolve and verdict), `tools/game/late.test.ts`, `tools/game/batch.ts` (the step and a `late` line on the summary screen).
- Rows: `loud-fight` (the engine's own loud twin; the mission ends at tick 3124, Success) and `sky-session1` (`briefs/045/runs/sky-0.2.58-session1.script.json`; ends at tick 4232, Failure `rivals-reach-vale`). Each end is what the engine says (`play --json`), not the script's `until`.
- Window: the last 60 s of mission, `[end - 600, end]` ticks, then 2 s of tail after the mission ends. 1920 x 1080, `-novsync`, HUD on, the play camera.
- Fails (each named with the row): any frame over 50 ms (window or tail); window p99 over 16.7 ms; a tail frame over 100 ms (the end card, `End()`, `FailCam`); the run did not end at the engine's tick and outcome (the drive diverged, so the numbers are not that script's); fewer than 100 frames timed; a row the player did not report.
- The step refuses at once, without opening the player, when the built player lacks the mode (it looks for the report's schema literal `suborned.late.report/1` in `Suborned.Runtime.dll`): "the built player has no -late mode yet". **Until Latch adds the hook, the step fails (it is last, after budget); use `--skip late-fight`.** Expect it to fail on the first build with the hook and before F1 and F2: that is the check working (17 saves of 68–188 ms in loud-fight's last 60 s; 8 of 150–273 ms in Sky's).
- Time: two rows, about 5 s of fast-forward + 63 s each, plus launch: 2.5 to 3 minutes. The step's player timeout is 8 minutes.
- Not done: `maxMs` and `over50msFrames` on the shot runner's `perf` windows. The late row reads every frame itself, so the shot runner stays as it is.

### The player's hook (Latch): `-late`

```
"The Suborned" -late <resolved.json> <outDir> -novsync -logFile <outDir>/player.log
```

`resolved.json` (`suborned.late.resolved/1`, written by `tools/game/late.ts`, plain public fields for `JsonUtility`; the real one is `out/batch/late.resolved.json`):

```
{ "schema": "suborned.late.resolved/1", "outDir": "...",
  "rows": [ { "name": "loud-fight", "scriptPath": "/abs/loud-fight.json",
              "warmTick": 2494, "startTick": 2524, "endTick": 3124, "outcome": "success", "failCode": "",
              "tailSeconds": 2, "w": 1920, "h": 1080 }, ... ] }
```

For each row, a fresh run from the mission's own start (as `-shots` does for a `script` start), in file order:
1. Size the player to `w` x `h` the way the shot runner does (`CaptureSize`), HUD on, the play camera GameBoot opens with, no camera commands. Leave the briefing (`StartClock`).
2. Orders = the script's `orders`, sorted by `(tick, file index)` as the engine does. Paused, fast-forward to `warmTick`, giving each order as `ShotRunner` does (`RunTo(o.Tick)`, then `GiveScripted` for watch, listen, turn, call and `Issue` for the rest). None of this is timed.
3. Unpause at `warmTick` and play on at real time. **Orders are given inside the host's per-tick loop, immediately before the step that leaves tick `o.Tick`** (so a slow frame that advances three ticks still gives each order on its own tick), through the same paths a click uses, so `Save()` and every other cost of an order runs as in play. This is what the shot runner's perf window did not do. Do not pause, do not skip saves, do not call `Save` yourself.
4. Frame time = real time between consecutive frames' `Update` (`Time.realtimeSinceStartupAsDouble` difference, ms; not `deltaTime`, which is clamped). Phases: `w` = frames from the first one that ends with `Game.Tick >= startTick` up to the one before `Game.Over`; `t` = the frame in which `Game.Over` first became true (it holds `End()` and `FailCamStart`), and every frame until `tailSeconds` of real time after it. Frames before `startTick` are not timed. If the game is not over 5 s after `endTick`, stop the row and report `status: failed`, `error: "ran past the end"`.
5. Write `<outDir>/late.json` after every row (so a crash leaves what was done) and quit when the rows are done: exit 0 if every row is ok, 1 if not, 2 if the file would not read. Also `<outDir>/<name>.frames.csv`: `ms,tick,phase` per timed frame (for people; the tools read only late.json).

`<outDir>/late.json` (`suborned.late.report/1`; the types are `LateReport` and `LateReportRow` in `tools/game/late.ts`):

```
{ "schema": "suborned.late.report/1", "status": "ok", "screen": "1920x1080", "build": "Build 0.2.61 · ...",
  "rows": [ { "name": "loud-fight", "status": "ok", "endTick": 3124, "outcome": "success", "failCode": "",
              "ordersGiven": 12, "ordersRefused": 0,
              "window": { "count": 7003, "meanMs": 8.1, "p99Ms": 12.3, "maxMs": 31.0, "over50": 0 },
              "tail":   { "count": 241,  "maxMs": 22.4, "over50": 0, "over100": 0 },
              "worst": [ { "ms": 31.0, "tick": 2611, "what": "save" }, ... ],
              "frames": "loud-fight.frames.csv" } ] }
```
- `outcome` is `success` or `failure` (lower case, as `play --json` says), `endTick` and `failCode` are the game's `Tick`, `Result` and `FailCode` when it ended.
- `p99Ms` is over the window frames only: the value at index `ceil(0.99 n) - 1` of the sorted times. `over50` counts frames above 50.0 ms.
- `worst`: the five worst frames of window and tail, worst first. `what` is optional, free text: the cheapest useful version is a static string the host sets when `Save()` or `End()` runs during the frame ("save", "order", "end").
- `status: "failed"` with `error` for a row that could not run (no such order verb, the script would not read). A row that ran but ended at another tick is `ok` here; the tools judge it.
- Where it can live: `ShotRunner.cs` already has the script start, `RunTo`, `GiveScripted`, `Copy(o)` and the boot flag; a `-late` branch beside `-shots` is the least work. The tools only need the flag, the two files and the schema literal `suborned.late.report/1` in `Suborned.Runtime.dll`.

Done-check for Latch: `bun run batch --only late-fight` runs and reports both rows. Before F1 and F2 it must FAIL (frames over 50 ms, 100 ms at the end of Sky's row); after them it must pass.

### Headless check, done (`bun run test`, 3 s)

`tools/game/closing-hash.test.ts` with a small harness, `tools/game/closing-hash/` (a .NET program over `sim/Suborned.Sim`; Keel's CLI is untouched). It plays loud-fight and Sky's session 1 as the host does (a save on every order and every 5 s of clock) and at each save with a fight open compares `Game.ClosingHash()` with `Engine.Play(...)`'s closing checkpoint, then times `ClosingHash()` at the last tick with a fight open (best of 20, warm).
- Result now (engine 0.25.0 in the tree): loud-fight 36 of 36 saves equal, 0.06 ms at tick 3073; Sky's session 1 8 of 8 equal, 0.24 ms at tick 4194. Limit 1 ms (a replay is 70–270 ms).
- It is skipped only while `public ulong ClosingHash()` is absent from `sim/Suborned.Sim`. It protects the engine call, not the host: the host still has to call it (F1); the player row is what proves that.

## The `late-fight` row under load (Splice, 2026-10-22)

**Why.** Latch saw the row fail twice on the shared Studio on 0.2.65 (a good build): stray 51-97 ms frames that moved between runs and coincided with no save, order or end. The hook (`LateRun.cs`) already marks each frame with what the host did in it (`what`: save, order, end), so the tools now sort slow frames by that. Files: `tools/game/late.ts` (`findLate`, `runLate`), `tools/game/batch.ts` (the step, the load watch), `tools/game/late.test.ts` (22 tests, no Unity).

**What fails now** (limits unchanged: 50 ms, p99 16.7 ms, 100 ms at the end):
| class | what | rule |
|---|---|---|
| event | a frame over 50 ms that held a save, an order or the end; any tail frame over 100 ms | must recur on the rerun |
| freeze | any frame of 250 ms or more, whatever it held | must recur on the rerun |
| stray-many | 3 or more frames over 50 ms with no event in them | must recur; advisory when the run was noisy |
| p99 | window p99 over 16.7 ms | must recur; advisory when the run was noisy |
| structural | the drive ended elsewhere, under 100 frames, row not run, no report | always fails on the second run |

- A lone stray over 50 ms with no event is **noted, never failed**, and costs no rerun.
- A row with findings is **run once more (only that row)**; it fails only if a finding of the same class returns. One that does not return passes with a note ("first run saw event ...; the rerun did not repeat it: not counted").
- **Machine load** is sampled while each run plays (`os.loadavg` 1 min, every 2 s: start, peak, end, cores, and the busiest other processes at the peak). Peak at or above 0.6 x cores flags **NOISY MACHINE** on the summary and in `out/batch/late.verdict.json` (every attempt with its load, findings, strays and the whole report).
- Why p99 and stray crowds go advisory on a noisy machine: the first try, with them hard, failed a good 0.2.65 on both runs (12 then 31 stray frames, p99 23.5 ms, load 15 then 31 on 18 cores from another agent's Swift build) while no frame held a save, order or the end. Load does that to any frame loop. A save, an order, the end and a freeze are still judged on a noisy machine.
- Cost: a pass is one run of both rows, 140-166 s measured (the budget was ~3 min); a rerun adds only the failing row, about 70-80 s.

**Evidence** (build 0.2.65, `bun run batch --only late-fight`; the machine had another agent's Swift build, Blender, Chrome and a second player on it all the time):
| run | extra load | result | time | worst frames |
|---|---|---|---|---|
| 1 | none of mine (peak load 22 on 18 cores) | ok | 140 s | 49 ms loud-fight, 53 ms Sky (1 stray, noted) |
| 2 | 12 `yes` busy loops (peak 36.9) | ok | 166 s | 21.3 / 44.9 ms (the end frame) |
| 3 | 12 `yes` busy loops (peak 35) | ok | 159 s | 24.5 / 45.1 ms (the end frame) |
The burners were killed by PID afterwards. Logs: `out/batch/splice-run{0,1,2,3}.txt`.

**Still fails on the old behaviour.** The built player has no flag to bring back the per-save replay (F1 is in the tree, `Game.ClosingHash()`), so this is a synthetic injection: 150 ms added to every save-tagged frame of the real run-3 report. Both runs injected: FAIL, naming `loud-fight: 3 frames over 50 ms held a save, order or the end: 174.5 ms at tick 3124 (order+save+end), 167.6 ms at tick 3113 ...` and the same for sky-session1, "seen on both runs". Injected on the first run only: passes with the noise note. `late.test.ts` also covers: the 1.9 s end freeze recurring (fails as end and as freeze) and not recurring (passes), a 400 ms unattributed stall recurring, 5 strays recurring, p99 recurring, a drive that diverges, a rerun that wrote no report, and a noisy run (p99 and strays advisory, a save stall still fails).

**Asks, none done in unity/ by me:**
- Latch: add a `what` column to `<row>.frames.csv` (`ms,tick,phase,what`), and list every frame over 25 ms in late.json (`slow`), not only the five worst. Today a row with more than five frames over 50 ms is classed from the five listed plus the count `over50`.
- Latch / Redline: `HudToolkit.cs` ~789 rebuilds the feed history from scratch when its key changes: `if (key != historyKey) { ... historyRows.Clear(); ... }` re-merges the whole `m.History` (up to 60 cards) and rebuilds every row. The key includes `m.History.Count`, `Unread`, the open rows and the newest card's id and line count, so a new line of speech or a card change rebuilds all rows in that frame. It is bounded (FeedHistory 60) but it is a per-event spike on the main thread; consider updating only the newest rows (or pooling rows as `DoneChips` does). Not shown to be the cause of any frame in the late-fight runs so far (no event-tied frame over 50 ms in the 0.2.65 passes).
