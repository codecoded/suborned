# Brief 050: storage (where the disk went)

Sky, 2026-10-22, watching batches capture screenshots: "where do they go? Feels like storage leak."

- **Owner:** Splice. **Next owner:** Aster (Sky approves the first prune; then tell the crew the scratch rule).
- **Done-check:** `bun run storage` reports every folder with size, files, dates, maker and what depends on it; `bun run storage --prune` is a dry run and deletes only with `--yes`; nothing was deleted in making this.
- **Run:** `bun run storage` (about 20 s; writes `out/storage/report.json`, `--html` adds `report.html`, `--top n`, `--json`). `bun run storage --prune [--rule a,b] [--list]`, add `--yes` to delete (logged in `out/storage/pruned-<time>.json`). Knobs: `--scratch-days --build-days --zips-keep --engine-keep --image-days`.

## Where it is (2026-10-22, 14:30Z)

| | size | files |
|---|---|---|
| `out/` | **51.4 GB** | 851,401 |
| `briefs/` | 6.4 GB, of which 5.8 GB are 3,791 git-ignored screenshots | |
| `unity/Library` (live cache) | 5.1 GB | 39,220 |
| `unity/Logs` | 1.0 GB | |

By kind in `out/`: agent scratch folders 17.3 GB (110 folders), old-world copy 8.9, the build 6.2, model runs 5.3, agent shots 2.6, mission checker 2.5, brief probes 2.0, DecoKit 1.5, sim 1.2, concepts 1.1.

## The batch is not the leak

The batch's own shots are `out/batch/shots` (48 MB), wiped and rewritten each run; the player proof `out/build/play/proof` (155 MB) is overwritten each build. What grows is what happens around the batch: agents copy the player and the tree into their own folders, take rounds of shots into `briefs/` and `out/<agent><n>/`, and one tool wrote a file per play.

## The leaks (what grows per round and is never cleaned)

1. **Temp play scripts, `out/missions/plays`: 621,754 files, 2.5 GB on disk (about 0.8 GB written today alone).** `tools/missions/check/engine.ts` wrote one script per play and never removed it; a composer run is 15,000 plays. **Fixed:** the file is removed after a successful play (a failed play keeps it for the error). Proved: one real play through the engine, file gone. The 134,087 files older than a day are in the prune; today's go tomorrow.
2. **Whole copies, not scratch: 12.9 GB.** `out/old-world/unity` (copy of the old project including its 7.0 GB `Library` import cache, Latch, Oct 19-20); `out/keel038/mcbase` 5.4 GB and `out/keel037/mcbase` 0.4 GB (the tree materialised for a base-versus-tree check, mostly `briefs/` screenshots), `out/review/engine` and `head`.
3. **Copies of the built player: 1.1 GB now, 0.5-1.5 GB per agent round** (`out/latch-0266/app`, `out/redline/048/app` are 1.8 GB each and hot, so held today; `out/sim-unity/players` IL2CPP check player 1.1 GB, Oct 15).
4. **Player zips and perf builds in `out/build`: 3.2 GB.** Five old numbered zips (275-362 MB each) beside the newest two, and two `SubornedPerf` builds (886 + 884 MB, Oct 14-15) that nothing reads.
5. **Agent scratch trees: 17.3 GB.** Written per round (Oct 20: 5.7 GB, Oct 22: 4.1 GB in one day), one folder per agent per brief, no owner, no date rule. Plus 26 model runs (`ivory-pistol-vN`, `enforcer-textured-vN`, ...), 5.3 GB, each with a 120-220 MB Package.zip.
6. **`unity/Logs/Editor.log` 919 MB** (+ `Editor-prev.log` 92 MB): one `RobotIdle.Yaw` IndexOutOfRange exception thrown 1,029,629 times in play mode on Oct 20 (`Assets/Suborned/Runtime/Robots/RobotIdle.cs`). A **code bug for Latch**; the log is only its symptom.
7. **Screenshots in `briefs/`: 5.8 GB, +~1 GB/day lately** (046: 387 MB, 047: 336, 044: 259 in a day or two; 026: 2.0 GB). `shots:shrink --check` would save only ~200 MB (70 native-size captures; HUD sets are left alone), so shrinking does not fix this. 1.5 GB is 105 images over 8 MB.
8. Side note: **24 `unity mcp` processes** are running (oldest 4 h, started with agent sessions; the count was about 12 when I started this task). They hold no disk; likely one per session left behind. Not touched (kill by PID only; Sky's call).

## What `--prune` would free (dry run, default policy)

| rule | frees | what |
|---|---|---|
| unity-caches | 7.0 GB | `Library` of a Unity project copy under out/ older than 1 d |
| repo-copies | 5.9 GB | a folder holding `tools/` and `briefs/` under out/, older than 1 d |
| scratch | 2.4 GB | agent/brief/model/concept folders untouched 14+ d, agent shot rounds 7+ d (`out/shots/flyby` 1.9 GB) |
| build-old | 1.7 GB | `out/build/perf`, `perf-aswas`, proof-night, drawcheck older than 7 d |
| build-zips | 1.5 GB | player zips beyond the newest 2 and older than 3 d |
| player-copies | 1.1 GB | `*.app` copies outside `out/build/play` older than 1 d |
| plays-tmp | 0.5 GB | temp play scripts older than 1 d (more tomorrow) |
| unity-logs | 1.0 GB | `unity/Logs/*.log` over 50 MB and a day old |
| engine-pins, brief-images | 0 | nothing over the limits today |
| **total** | **21.1 GB in 188,009 files (104 units)** | `out/` 51.4 GB -> about 31 GB |

Held back today (shown as HELD in the dry run): `out/shots/skyline-heights` 51 MB (a contract names it), `out/old-world` as a whole (`tools/decokit/palms026.py` reads `out/old-world/faithful`), `out/concepts/parade` and `out/datum` (recorded runs name them), `out/motive027/s15` (`tools/sim/batch-proof.ts` reads it), everything changed in the last 24 h.

More if Sky wants it: `--scratch-days 7` frees 5.2 GB from scratch (instead of 2.4), `--scratch-days 3` 7.9 GB; `--image-days 3` frees 871 MB of unnamed screenshots (`--image-days 7`: 74 MB). Those are Sky's numbers to choose; defaults are the careful ones.

## Retention policy (in `tools/storage/policy.ts`, one rule each, tests in `storage.test.ts`)

Never pruned: **pinned** (a tracked file under `runs/`, `contracts/` or `sim/data` names the path; an engine commit a recorded mission run in `runs/missions` pins; plus the inputs a tool reads by default, `READ_BY_CODE`), **KEEP** (a file called `KEEP` in the folder or any folder above), **tracked** (git tracks a file inside), **hot** (changed in the last 24 h), **named** (a screenshot a tracked file or brief names, or that `verify`'s HUD budget, the figures install or drift:auto reads: `briefs/014/play`, `022/latch`, `020/people`, `007`, `008/concepts`, `009`). Code and doc mentions of an `out/` path do not protect it (most are comments); they are listed in the report as C and D.

Replays are offline: `tools/levels/replay.ts` validates a run "from runs/ alone, no out/", and images pinned by sha256 are checked only if present (`images-replay.ts`), so no prune can fail `bun run verify`. Pinned engines under `out/missions/engine` are rebuilt by `git archive` on demand, so even they are only held if their commit is missing from git.

Rules and starting values (all `[S]`, tune by use): plays 1 d; zips keep newest 2 and 3 d; perf and proof folders 7 d; player copies, Unity `Library` copies and repo copies 1 d; engines keep newest 6 + 14 d + commit in git; scratch 14 d (shot rounds in `out/shots` 7 d; `out/scratch/<agent>/*` 7 d); Unity logs over 50 MB; ignored screenshots 14 d and unnamed.

## Convention (written into CLAUDE.md, Layout, `out/`)

Scratch goes under `out/scratch/<agent>/<what>` (never `out/<agent><n>/`, never a player copy in your own folder); the batch's shots rotate (below); a `KEEP` file protects a folder. Existing `out/<agent><n>` folders are classified by name and age; nothing was moved.

## Changed

- `tools/storage/{scan,refs,policy,cli}.ts`, `storage.test.ts` (new); `package.json` (`storage`).
- `tools/missions/check/engine.ts`: the temp play script is removed after the play (two lines + import).
- `tools/game/batch.ts`: `rotateShots`. `out/batch/shots` is the newest run, `shots-prev1` and `shots-prev2` the two before it, older deleted; a `--shots-out` elsewhere is cleared as before. Cost at most 3 x 48 MB. Test in `batch.test.ts`.
- `CLAUDE.md`: the one convention line.

## Evidence

- `bun run storage`: out/ 51.4 GB / 851,401 files in 20 s; `bun run storage --prune`: would free 21.1 GB, nothing deleted.
- `bun test tools/storage tools/game`: 110 pass, 0 fail (policy: age, pin, KEEP, tracked, hot, engines, zips, logs, images; `--prune --yes` on a temp tree deletes the old folder and keeps the fresh and KEEP ones). `bun run typecheck`: clean.
- One real mission-engine play after the change leaves no file in `out/missions/plays`.
- Not run: `--prune --yes` (Sky approves the first one); `bun run verify` (heavy; the claim above is read from `replay.ts` and `images-replay.ts`).

## Open

1. Sky approves the first prune (default policy, 21.1 GB), or a different `--scratch-days`.
2. Latch: the `RobotIdle.Yaw` exception (1 M per play session; also fills the console).
3. Aster: tell the crew the scratch rule; Keel/Redline/Latch stop copying the player and the tree into `out/<agent><n>` (use a path under `out/scratch/<agent>/` and delete after the check).
4. Briefs screenshots (5.8 GB) are the one thing a prune barely touches: they are Sky's evidence. Options for Sky: lossy JPEG for old rounds (`shots:shrink --jpg`), or a rule that a round older than N days keeps only the picks. Not done.
5. `unity mcp` processes (24): a session-end cleanup, not a disk issue.
6. Unity `Library` (5.1 GB, live) holds `PlayerDataCache` 1.2 GB and `BuildHistory` 0.8 GB that Unity manages; left alone.
