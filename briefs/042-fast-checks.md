# Brief 042: fast checks

Standing rule (Sky, 2026-10-22): no crew check takes over 10 minutes without being sped up.

- **Objective:** the checks that play the engine many times (Motive's §15 robustness run, the mission composer and checker, fight-lab, `bun test`) stop paying a process start and `Engine.Load` per script, and run on every core. Same bytes out.
- **Owner:** Splice.
- **Done-check:** batch output byte-identical to per-process `play --json` on a 794-script sample (mix) in six modes; composed routes and the mission check identical before and after; every check below under 10 minutes.
- **Next owner:** Aster (tell the crew: `bun run test`, not `bun test`). Waymark may re-record route hashes: the `tools/missions/check` path is stable from now (see "Stable").

## What was built

1. **`suborned-sim batch`** (`sim/cli/Batch.cs`; `Program.cs` only gets the `case "batch"` and the play body moved out):
   - `batch <script | dir | scenario id | -> ... [--list f] [--no-replay] [--jobs n] [--out f]`: inputs load once, plays run on all cores (dedicated 64 MB-stack threads, one `Game` each), one JSON line per script **in item order**, the exact line `play --json` prints. A script that cannot play gives `{"id":..,"error":..}` and the batch goes on. Exit 1 if anything failed its checks, replayed differently or errored.
   - `batch --serve`: a server for callers that ask in rounds (the composer): stdin `tag<TAB>replay|noreplay<TAB>path`, stdout `tag<TAB>json` as each finishes.
   - **The self-check replay stays on by default** (as `play`). `--no-replay` / `{ replay: false }` leaves it out: **about 40% less wall time** (3000 scripts: 11.3 s with, 6.9 s without) and the line then says `"replayIdentical": null` (never `true`). Only the composer's search probes use it; every route it keeps is played with the replay in the check.
2. **Thread safety, nothing edited in `sim/Suborned.Sim`.** Read before parallelising: the engine's scratch (`Grid.Pool`, `Hash.HTick/HLapse/HLab/HGame`, the lab policy statics, `supportTech`) is already `[ThreadStatic]`; `Engine.Inputs` is read-only once loaded; `Start.LegacyCanonical` and `Hash.StandingAs` are set only by the `hashes` command, never by `play` or `batch`; the lazy `Scenarios.All` is forced before any worker starts. The equality proof below is the check.
3. **`tools/missions/check/engine.ts`** (the shared player; `run.ts`, `compose.ts`, `summon.ts`, and Waymark's `robust-plan.ts` use it unchanged): `play`/`playAll` talk to one persistent `batch --serve` per engine (server GC, no concurrent GC, no PGO: measured 20–25% faster than defaults). `playAll`'s `n` is moot with the server. `SUBORNED_SIM_BATCH=0` forces a process a play; `SUBORNED_SIM_STATS=1` prints the play count at exit. `compose.ts` passes `replay: false` for its probes.
4. **Pinned old engines** (a recorded check replays on its commit's engine, whose `sim/cli` has no batch mode and must stay as committed, since the engine hash covers it): `sim/batch/` (BatchHost.cs, batch.csproj) is copied with `Batch.cs` to `out/missions/engine/<commit>/batch/` and built against that engine's own library, so the old library plays and the recorded hash is untouched. Checked on 306b15e: 26/26 byte-identical to its own `play --json`. If a host does not build, it falls back to a process a play.
5. **`fight-lab`** (`sim/cli/FightLab.cs`, Keel's): each (preset, run) is an independent game, now played in parallel (`--jobs n`, `--jobs 1` = as before), printed and written in the old order. Stdout and the written `fight-lab.json` and checkpoints are identical (compared jobs 1 vs all cores, and against the old binary's output).
6. **`tools/sim/unity.ts`** played 253 scenarios (plus routes) one process at a time; now one `batch` call when the CLI has it. Typechecked only: it writes into the Unity project, so I did not run it end to end. Please run it once when Latch next syncs the sim.
7. **`package.json`**: `test` is `bun test --parallel=8 --timeout=60000` (`check` uses it). Plain `bun test` still works and is still slow.
8. **`tools/drift-auto/cli.ts`**: the base materialised from HEAD had none of the git-ignored images that skyline runs pin, so every skyline declaration was refused ("not on this machine" at base, "checked" in the tree). It now reads `briefs/` from this tree. With that, drift:auto declared the `engine.ts` drift for skyline and people (`--rules none`; replays identical to HEAD).
9. **`tools/sim/batch-proof.ts`**: the equality proof, repeatable. `bun tools/sim/batch-proof.ts [--root <frozen engine root>]`.

## Evidence

Equality (`bun tools/sim/batch-proof.ts`, 794 scripts = 500 rob-loud plays from Motive's s15 set, 41 route and motive-fight scripts, 253 scenarios; run on a frozen copy of the engine Motive's set was made on, and again on the working tree at 0.20.0, 797 scripts):

| mode | result |
|---|---|
| batch, all cores / 1 thread / 3 threads | 794/794 byte-identical to one process per script, each |
| batch from stdin (`-`) | 794/794 |
| `--serve` | 794/794 |
| `--no-replay` | 794/794 identical except `replayIdentical` is `null` |
| working tree 0.20.0 | 797/797 in all of the above |

End to end, before (`SUBORNED_SIM_BATCH=0`, a process a play) vs after, same engine and inputs:

- Motive's robustness composer (`robust-plan.ts`, one wait, 16 variants, 14,806 plays): the 16 result lines identical. Composed route scripts for all 17,510 plays of `missions:compose`: `diff -r` identical.
- Mission check output identical but for the stats line; MC-0 37/38 as before.

| check | before | after | note |
|---|---|---|---|
| Motive robustness, 14,806 plays | 1142 s | 33–38 s | before ran at load 100–250, after at 10–17: say 20×–30× fair. Full 81,418 plays: about 3.5 min, was 90 min |
| `missions:compose` (17,510 plays) | 630 s | 28 s | composed routes identical |
| mission check `--engine tree --inputs tree` (2,336 plays) | 215 s | 20–27 s | 37/38 (MC-0), unchanged |
| `missions:rerecord` (compose + check + record) | about 14 min | about 1 min | |
| replay of recorded check c012 on pinned 306b15e (3,010 plays) | 164 s | under 20 s (whole check.test.ts 22 s) | via sim/batch host |
| fight-lab, all 7 presets | 21.5 s | 6.8–7.3 s | tail is the longest run; more with long `--runs` lists |
| `dotnet test sim/Suborned.Sim.Tests` | 111 s | 111 s | untouched, 1509 pass; xunit runs a class serially |
| `bun test` | 1213 s (20 min) | 276 s with `--parallel=8` | 1466 pass, 1 fail (MC-0, Keel's pending re-record) |
| `bun run verify` | 21 s | 20–30 s | OK |
| `bun run typecheck` | clean | clean | |

Per-file `bun test` (6 at once, so inflated): world-rules 242 s, skyline 204 s, world/rules 187 s, check 171 s (was the c012 replay: now 22 s), levels 67 s, drift-auto 43 s. The world and plan tests, not the sim, are now the longest.

## Still over 10 minutes

Nothing I run. Left, with an owner:
- **Plain `bun test`**: 20 min, because it runs one file after another. Use `bun run test` (Aster: tell the crew).
- **Waymark's `briefs/027/waymark/robust.ts` and `sweep.ts`** spawn `player.dll` (a scratch program with its own env-driven play, not `suborned-sim`) once per variant: about 1 s each, so a sweep of 10,000 is about 10 min on 16 at a time. Not a mechanical swap: the player needs a batch mode (a list of env sets). Owner: Waymark; Splice can add it on request.
- **Motive's and Suture's `sweep.sh`** chunk 3,792 policies into 16 `fight-lab --runs` processes. They still work and each process is now multi-core; one `fight-lab --runs <all>` is simpler and keeps the cores busy to the end. Not edited (scratch scripts under `out/` and `briefs/027/suture`).
- **`tools/route-diff` and `tools/game/batch.ts`** still spawn a process per script (40 scripts, about 3 s; one `script <id>` per shot): under a minute, left alone.
- Old pinned engines whose library API predates `Script.Expect` may not build the host and fall back to a process a play (slow, still correct).

## Stable

`tools/missions/check/engine.ts` and `compose.ts` will not change again in this brief; the `engine.ts` drift is declared in `tools/levels/skyline/drift.json` and `tools/levels/people/drift.json` (verify OK). Any later edit to `engine.ts` needs `bun run drift:auto` again. Note: `engine.ts` is in the mission check's tool hash, so recorded checks replay as "judged by the current checker; its code changed since the check" (a note, not a failure).

## Notes

- `hashes` runs scenarios without inputs, so Keel's F15-CLEAN skips its control run there (Aster's note); not a regression.
- Changed files: `sim/cli/Batch.cs` (new), `sim/cli/Program.cs`, `sim/cli/FightLab.cs`, `sim/batch/BatchHost.cs` + `batch.csproj` (new), `tools/missions/check/engine.ts`, `tools/missions/check/compose.ts`, `tools/sim/unity.ts`, `tools/sim/batch-proof.ts` (new), `tools/drift-auto/cli.ts`, `package.json`, `tools/levels/skyline/drift.json`, `tools/levels/people/drift.json` (declarations). Scratch under `out/batch-proof/`.
- Measured on the Mac Studio (18 cores) while other agents were running; load averages are noted above. Nothing committed.

## Follow-up (Splice, 2026-10-22): Waymark's player and the fight-lab sweeps

**1. Waymark's player, batch mode** (additive; the one-play path is unchanged).
- `briefs/027/waymark/player/Program.cs`: `player --batch <jobs.jsonl | -> [--jobs n]`. Each input line `{"base":..,"env":{..}}`, each output line (input order) `{"out":<stdout a process prints>,"err":..,"code":..}`. Inputs load once; plays run on all cores (64 MB-stack threads); the player's `Env()` reads a per-thread override first, then the process env, and its output goes to a per-play writer. A play that throws gives `code 1` and the batch goes on.
- `briefs/027/waymark/batch.ts` (`playMany`): batch if the built player has `--batch` (probed), else a process a play as before; `SUBORNED_PLAYER_BATCH=0` forces the old path. `robust.ts` and `sweep.ts` call it (`PLAYER=` and `E=` override the dll and engine root; defaults as before). `robust-plan.ts` and `loud-plans*.ts` never spawned the player (robust-plan already runs through `engine.ts`'s batch server; loud-plans only writes plan files): nothing to switch.
- Proof (`bun briefs/027/waymark/batch-proof.ts --n 600`): 600 variants (waits moved over the cover points, five lock rules, ENF, LOG=1, STANCE, DRAWAT, QUIET, LOCK, WRITE): 600/600 stdout byte-identical to one process each, and the 27 scripts the variants WRITE are identical too. `sweep.ts` (480 plays) and `robust.ts` (96 plays) with batch on and off: output files identical.
- Time (machine at load 40–130, other agents running; same job lists back to back): 120 plays 11.0–11.5 s as processes (16 at a time) → 4.5–5.6 s batch; `sweep.ts` 480 plays 101 s → 25.5 s. About 2–4×, not 20×: unlike the sim's `play`, a Waymark play is about 0.3 s of real work (it previews every shot each tick), so process start was only 2/3 of the old cost. 10,000 variants: about 10 min → about 3 min on an idle machine.
- **Waymark's built player at `out/waymark027/player/bin` is an older build with no `--batch`**: the scripts fall back to a process a play with it. To get the speed, rebuild from the `Program.cs` in briefs (I built mine at `out/splice042/p19` against a copy of e19; I did not touch Waymark's builds). Waymark had `robust-plan.ts` in flight when I switched the scripts; it does not use them.

**2. Motive's and Suture's `sweep.sh`** → `tools/sim/fight-sweep.sh <tree> <preset> <out-prefix> [rulebook] [policies]`: one `fight-lab` call with the whole list when the tree's CLI has the parallel fight-lab, the old 16 chunks when it does not. Needed, because the pinned CLIs (`out/motive027/s15/tree`, `out/suture027/r3/cli`) are serial: one call there would be 16× slower.
- Rows identical: 3,792 policies, preset lab, current-tree CLI: one call vs 16 chunks, `cmp` identical (3,792 lines), three rounds. The `.replay` file reads `1 every run replays identically…` instead of `16 …` (it counts calls).
- Time: with default GC one call was slower than 16 chunks (63–66 s vs 37–47 s); with server GC, no concurrent GC, no PGO (set in the script) one call is 22–23 s vs 24–25 s chunked, load 35–85. The sweep was never the long pole (about 25 s); the gain is one process and no `split`.
- Edited: `out/motive027/s15/bin/sweep.sh` now delegates to it (same args and output paths; checked rows identical on s15, serial CLI, so chunked); `briefs/027/suture/probe/retest3/sweep.sh` takes one call if its `r3` CLI is parallel (it is serial today, so unchanged in effect; that branch is not run). Older Keel/Motive sweep.sh copies under `out/` untouched.
- Scratch: `out/splice042/` (p19 player build, e19 copy, fl tree, sw outputs). Typecheck clean. Nothing in `sim/Suborned.Sim` touched; nothing committed.
