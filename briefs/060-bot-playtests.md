# Brief 060: bot playtests (play it for real, measure it, try to break it)

Sky, 2026-10-24: "I'm noticing a lot of issues playing the game ... and just odd things. Can we create some bots to play the game for real and measure it, try to break it, find out what works and what doesn't. is that practical?"

**Yes, it is practical.** Most of the parts exist already. The engine plays thousands of games in seconds (brief 042). Waymark's player already drives the engine closed-loop from C#. The built player already takes scripted input through the Input System (ProofInput), gives orders through the click paths at real time (LateRun) and captures shots with HUD manifests (ShotRunner). What we don't have yet is three things: bots that pick their own orders, a set of invariants checked on every run, and one report that ranks what they find.

- **State:** designed 2026-10-24 (Suture). Nothing is built. Probes were read-only; no load was run, because Sky was playing (guard: pid 47722).
- **Owner:** Suture owns the checks, the thresholds and the report (§14). Keel owns the engine side, Waymark the playstyle bots, Latch the player harness and Splice the runner, storage and geometry.
- **Done-check (first slice):** one `bun run qa` pass. Tier 1 runs in under 10 minutes on the Mac Studio, plus one long host run while Sky is away. The pass writes `out/qa/<run>/report.html` with at most 10 issues for Sky. On the current build (0.2.100, before next-batch items 22-33 land) it must re-find, without being told where to look, at least the 8 of Sky's 15 hand-found issues that the slice covers (marked **S** in the table below).
- **Next owner:** Aster (assign the slice; Sky's decisions at the end).
- **Not a replacement for T1-T10.** Bots find breaks, stuck states, lies and lag. People judge whether the game reads and whether it is fun. The bots do give proxies for T2 (does loud dominate?) and T9 (does a takedown cost what it shows?), described in tier 1.

## 1. Tiers

### Tier 1: sim fuzz bots (engine only, no Unity)

1. **Mutation fuzz (day 1, no engine change).** The seeds are the 40 Handover routes and Sky's 177 real sessions. The sessions are the saved `handover-*.script.json` files over 8 KB in `~/Library/Application Support/com.Unity-Technologies.com.unity.template.urp-blank/play/`. The other ~5,900 files there are tiny proof sessions. Each mutant changes one or two things:
   - moves an order by 1-30 s;
   - drops an order;
   - repeats an order (the repeat Watch);
   - gives the order to another agent, including a downed or carried one;
   - swaps the target;
   - inserts an order drawn from the vocabulary of every order the seeds contain.

   The mutants play through `suborned-sim batch` with replay on. The checks read the play JSON (`orders` with refusal codes, `ledger`, `log`, `cause`, `debrief`).
2. **Closed-loop bots (day 2-3, Keel).** `suborned-sim bots --policy <p> --seeds a..b`, modelled on `briefs/027/waymark/player`. It runs in-process against `Game` and reads state every 1-5 s of game time. It chooses from a candidate set (rulebook verbs × people, hostiles, cameras and points) and gives orders through `Issue`. Policies:
   - `random`;
   - `greedy` (the cheapest price toward the next objective step);
   - `adversary`: orders at window edges, on agents mid-walk or busy, from downed givers, while carrying, and repeats of the same act.

   Waymark adds `cautious`, `loud`, `cash-greedy` and `chaotic`. Every run saves its orders as a script, so any finding replays exactly.
3. **Trace (Keel, small).** `batch --trace <every n ticks>`, read-only and never hashed. It records the position, level and state of every actor (cell, Vale, cast, crowd, patrol, robots, police, rivals, cars), plus each act's knowledge diff (the facts it added to what the cell knows). Splice's checker reads the traces against `out/world/<plan>.walk.json` (floor heights and solids at 0.25 m) and the host height port in `tools/geometry/sim.ts`. Most traces are checked as they stream and dropped; only anomalous plays keep theirs.
4. **What works (balance output).** For each policy and route: the win rate, the price paid ($, Trail, Alert, time), the fail codes, and the refusal codes by frequency. It also flags a dominant strategy: one policy that wins ≥ 80% [S] while being no worse on every price than every other winning policy.
   - T2 proxy: in the same scenario, does `loud` win faster and cheaper than `cautious`?
   - T9 proxy: an unseen takedown route wins at Alert ≤ 1, and the same takedown seen and reported ends in a fail or a loud run, in ≥ 95% of seeds [S].

- **Build:** mutation fuzz plus checks, 1 day (Suture writes the checks, Splice the runner). Trace, 0.5 day (Keel). Closed-loop bots, 1-1.5 days (Keel), plus 0.5 day of playstyles (Waymark).
- **Run time:** under 10 minutes, target 5. Measured in brief 042: 3,000 scripted plays take 11.3 s with replay. A closed-loop play costs more, because it queries previews each decision. Waymark's player measured ~0.3 s a play, so say 2,000 bot plays take 1-3 minutes on 18 cores. Budget per pass: 5,000 mutants plus 2,000 bot plays plus the checks.
- **Storage:** under 30 MB a pass. The pass keeps counts, the failing plays' scripts (~2 KB each) and their traces only.

### Tier 2: player bots in the built game (Unity host, Mac Studio, never while Sky plays)

Latch adds a `-bot <plan.json> <outDir>` flag to the built player, following the LateRun and TpLongRun pattern (a resolved file in, a report written after every row, then quit). The bot drives the Input System devices as ProofInput does: left click, hold, the ring, right hold to fire, verbs 1-4, F1-F4, Q/E turns, T for third person, WASD.

It chooses from the host's own offers (`HandoverHost.ContextFor`, which builds the ring, panel and pill rows), so "offered then refused" is measured on the real offer. Plan rows:
- a replay of a tier-1 bot script through the click paths;
- a seeded random player that uses the same policies but reads the host's offers.

Per-frame checks run in-host and are cheap: Q-GIVER, Q-OFFER, Q-INTENT, Q-CANCEL, Q-DRAWN, Q-FACE-T, console errors and exceptions (`Application.logMessageReceived`; remember RobotIdle.Yaw's 1M exceptions), frame time every frame, and memory every second. The host also writes three files for offline checks:
- figure feet (x, y, z) every 0.5 s, as CSV, for Q-FEET and Q-SOLID;
- the HUD manifest every 2 s, for the existing hud-budget layout audit, Q-NAMES and Q-WARN-HUD;
- the end card's lines, for Q-CARD.

At every anomaly the bot captures one 1080p HudToolkit shot with its manifest, and the replay (the session script up to that tick plus the bot seed). It dedupes to one capture per check per 2 m cell per 30 s.

- **Build:** 2 days for Latch: day 1 is the `-bot` row over LateRun's click paths with the cheap checks; day 2 is random input through the devices and third person. 1 day for Suture (checks and offline readers). Splice wires it into `bun run qa` behind the guard.
- **Run time:** a run plays the whole Handover at real time (8:30 close, ~10 minutes with boot). One player at a time, so ~35 runs a night. It runs only through `bun run guard`. The runner rechecks between runs, so a game Sky opens stops the next run from starting. Non-perf rows may run at 2× once Latch has shown that holds and the ring read scaled time the same way. Until then they run at real time.
- **Storage:** per run, the frames CSV (~5 MB) plus at most 60 shots, shrunk with `shots:shrink` (~300 KB each), about 25 MB. A night is under 1 GB. Runs rotate as batch shots do (brief 050): the last 2 nights are kept, `KEEP` protects a run, and `bun run storage` reports them.

### Tier 3: look review (build time only, never at runtime)

After a night's runs, a reviewer reads only the deduped anomaly shots (at most 40) plus 20 sampled frames: play zoom at each of the four yaws, and third person. It works through `tools/rag` `runAgent` with a structured answer per image: `{odd, what, where (box), severity}`, with the check that raised the shot as context.

- **What a model can judge:** gross oddities a player would see at 1080p, such as:
  - an upside-down face;
  - a person standing on nothing, seen from the side;
  - stray kerbs or geometry in the road;
  - text clipped or overlapping;
  - one overlay dominating the frame;
  - a figure inside a wall;
  - a "something looks off here" next to the concept art.
- **What it can't judge:** offsets under ~10 cm at play zoom (characters are 40-60 px), lag, rule correctness, determinism, whether a fail was fair, or what a HUD element is meant to mean. It also answers differently from run to run.
- **So it is never a gate.** It adds weight to a ranked issue and catches what no check covers yet. Any "odd" it finds twice becomes a mechanical check.
- **Cost:** $0 on the subscription, but slow: 20-40 s an image, so 60 images take 20-40 minutes once a night. It shares rate limits with the crew, so it runs at night. Owner: Suture (prompt and triage). Glyph checks its HUD calls.

## 2. Sky's 15 (2026-10-24) and the check that catches each

Layers: **sim** (engine and trace, tier 1), **host** (built player, tier 2), **render** (pixels), **world** (static export). Thresholds marked [S] are starting values, tuned once the first pass shows the real distribution. **S** = covered by the first slice.

| # | Sky found | check | layer | what it measures | pass |
|---|---|---|---|---|---|
| 1 S | a repeat Watch logged "Learned" twice | Q-LEARN | sim + host | sim: every completed Watch or Listen adds ≥ 1 fact to the cell's knowledge (trace knowledge diff), or the engine refused it. host: every feed or scene line with a "Learned" footer names a fact that entered knowledge at that act's end, and no fact appears in two Learned lines | 0 |
| 2 S | a downed agent offered as the doer on the ring | Q-GIVER | host | each time `ContextFor` builds the ring, hold, panel, verbs 1-4, Intel next step or portrait drop, no giver is down, carried, taken, inside or in the car (engine state at that frame). The offers are built in the host (`HostContext.cs` Givers), so the sim can't see this | 0 |
| 3 | a move click turning into a carry | Q-INTENT | host | every bot click is logged with what is under the cursor (ground, person, body, ally). Ground gives a move or nothing; a body opens the ring and gives no order. The order the host issues must match. ≥ 500 clicks a run, ≥ 100 of them within 2 m of a body | 0 mismatches |
| 4 S | the patrol walking through a camera pole | Q-SOLID | sim (trace) + host | each actor's body circle (r 0.25 m) at every trace sample, and the leg between samples, against walk.json kind 2 or 4 (solid) | 0 overlaps > 5 cm |
| 5 S | Inez on the café window sill (feet above the drawn floor) | Q-FEET | sim (predicted) + host (real) | feet height against walk.json `floor` (the drawn floor) at the actor's cell. sim: the trace plus the host height port; host: the real figure feet from the CSV | \|Δ\| ≤ 5 cm on ≥ 99.9% of samples; no run of > 0.5 s over 15 cm |
| 6 | kerb stones lying on the carriageway | Q-KERB | world | every kerb stone in the world export lies within the kerb band (pavement edge ± 10 cm) and not on carriageway. A new rule beside G-1..G-3 and K-1 in `bun run geometry`, under the ratchet. It is static, so no bot is needed | 0 |
| 7 | the robot face upside down | Q-FACE-T, Q-FACE-R | host, render | T: each face screen's local up · world up > 0.7, every 10 frames. R: a close shot of each robot kind's face, matched against the RobotFace glyph bitmap upright and turned 180° | T 0; R upright beats flipped by ≥ 1.5× |
| 8 S | the fail card contradicting itself | Q-DEBRIEF, Q-CARD | sim + host | sim: every debrief claim is backed by the log ("vale-reaches-getaway-car" needs Vale's `board.in`; a failed run makes no success claim). host: the card's lines against a table of contradicting text keys (Fable keeps the table) | 0 |
| 9 S | the seal ending a run with no clock shown | Q-WARN, Q-WARN-HUD | sim + host | sim: every fail has a warning the player could see (`seal.starts` for sealed, and the same for each fail code) at least 30 s [S] before it. host: from the warning to the end, the HUD manifest shows that warning's element (the seal countdown) on screen in every frame, not just in a feed card | 100% of fails |
| 10 S | Vale at the service-lane car, never getting in | Q-STUCK | sim | `vale.at-car` is followed by `board.in`, or Vale's state changes, within 10 s [S]. Generally, no actor with an open order or schedule step keeps the same position and state for longer than its longest scheduled wait plus 10 s | 0 |
| 11 S | rivals standing at the pickup from the start | Q-SPAWN, Q-DRAWN | sim + host | sim: at tick 0 and at each arrival, every scheduled actor is where its schedule puts it (absent until it arrives). host: a figure is drawn if and only if the engine places it, within 0.5 m of the engine's position (every 10 frames) | 0 |
| 12 | a camera cone dominating the frame | Q-CONE | render | the share of the frame the signal layer covers at play zoom (drawn alone, as HudToolkit splits layers), sampled every 10 s at each yaw | ≤ 8% of the frame, no single cone ≥ 5% [S; Glyph sets the final number with brief 057] |
| 13 | "Reroute" not naming what it moves | Q-NAMES | host | every offered way whose effect lands on something other than the clicked target (reroute → patrol, loop → camera, call → person; a verb → affected-thing table from the rulebook) carries that thing on its pill (cameo id or name in the manifest) | 0 |
| 14 | third person stopping actions | Q-CANCEL | host | every act that ends early has a cause: a logged bot input, or an engine refusal or interrupt code. Also, the same order set in third person and on the map gives the same engine log | 0 unexplained; logs identical |
| 15 | lag late in a run | Q-FRAME, Q-MEM | host | whole real-time runs, measured on the Studio (the Air gets its own row later): p99 ≤ 16.7 ms; no frame over 50 ms except boot or load; the last 2 minutes' p99 ≤ 1.2× the first 2 minutes' (after warm-up); memory (managed and native) shows no held growth after a 120 s warm-up: the floor of each third of the window rises by no more than 3 MB a minute (total) and 1 MB a minute (managed) on both steps (set in the smoke triage below; was ≤ 5 MB a minute as one slope). These are the late-fight gates from `tools/game/late.ts`, applied to every minute | all four |

The general invariants that tiers 1 and 2 add:
- **Q-OFFER (host):** every way the host offers is accepted, or its refusal was shown on the pill or plate before the commit.
- **Q-PREVIEW (sim proxy):** whatever `Quote`, `Preview` or `FireQuery` says is taken is accepted by `Issue` at the same tick.
- **Q-ERR:** no `engine-error` refusal and no exception (sim), no console error (host).
- **Q-DET:** `replayIdentical` is true for every play (sim). A host bot session replayed headless gives the same final hash; game:check already does this for the driven session.
- **Q-FAIL:** every fail carries a fail code and a FailCause with a culprit, and its code has card text.

Found while reading, for #8 (one line, for Keel and Fable). `Debrief.Of` adds "vale-reaches-getaway-car" to every front, service and pickup route whatever happened (`sim/Suborned.Sim/Debrief.cs:60` and `:68`). The host builds the debrief on every ending (`HandoverHost.cs:1423`) and renders that line (`HudModel.cs:391`). Brief 057's sub-line fix stops the card's own words from contradicting each other, but the debrief data still claims that Vale reached the car. Q-DEBRIEF fails on today's tree.

## 3. The report

`out/qa/<run>/report.html`, self-contained, with shots embedded as small JPEGs.
- **Sky's list first:** at most 10 issues, ranked by severity × frequency (the share of runs affected). Severity levels:
  - S1, the run breaks: a crash, a stuck state, an unfair fail, a card that contradicts itself;
  - S2, the rules lie: offered then refused, the wrong order, a missing warning;
  - S3, a visible oddity: feet, solids, faces, kerbs, spawns;
  - S4, polish: cone share, a frame spike.
- **Each issue carries:**
  - one shot;
  - the replay command (`suborned-sim play <script>`, or a `-shots` row at that tick);
  - steps to reproduce;
  - expected against observed;
  - the first build it was seen in;
  - the owner (by §14).
- **Then:** the full table, the balance tables from tier 1, and the change since the last run (new, fixed, regressed).
- **Committed evidence:** `briefs/060/qa/<run>.summary.json`, holding the counts per check plus the build and engine, like the geometry summary. The report and shots stay in `out/qa/` (git-ignored, rotated).

## 4. First slice (value in a day or two)

| day | who | what | estimate |
|---|---|---|---|
| 1 | Suture | `tools/qa/checks/`: Q-ERR, Q-DET, Q-FAIL, Q-DEBRIEF, Q-WARN and Q-STUCK (from the log: `vale.at-car` without `board.in`, `seal.starts` lead time), Q-SPAWN, the T2 and T9 proxies, and win and price tables over play JSON | 1 day |
| 1 | Splice | `bun run qa`: mutation fuzz over the routes and Sky's 177 sessions through `suborned-sim batch`, behind `bun run guard`; `out/qa/<run>/` rotation; the report shell. Q-KERB in `bun run geometry` (alongside next-batch item 27) | 1 day |
| 1 | Keel | `batch --trace` (positions, levels, states, knowledge diff; read-only, not hashed; byte-identical play lines without it) | 0.5 day |
| 1-2 | Splice | the trace checker: Q-SOLID and Q-FEET (predicted) against walk.json, reusing `tools/geometry` | 0.5 day |
| 1-2 | Latch | `-bot` row, cheap version: plays a closed-loop or tier-1 script through LateRun's click paths at real time, with Q-ERR (console), Q-GIVER, Q-OFFER, feet CSV, Q-FRAME and Q-MEM. One long run (Sky's longest session, `handover-20261022-201505`, plus 2 tier-1 bot scripts) | 1 day |
| 2 | Keel | `suborned-sim bots` with `random` and `adversary` | 1-1.5 days |
| 2 | Suture | run the pass: tier 1 (≤ 10 minutes) plus 3 long host runs while Sky is away; write the report and the first issue list; send each bug to its owner | 0.5 day |

The slice covers #1 (sim half), #2, #4, #5, #8, #9 (sim half), #10 and #11 (sim half), plus Q-OFFER, Q-ERR, Q-DET and the first balance table. That is 8 of the 15. Latch keeps the current player as the known-bad baseline: one `game:pack` zip with a `KEEP` file, not a player copy in a scratch folder. That way the done-check still means something if next-batch items 22-33 land first.

Later, in order:
- random device input and third person (#3, #14);
- the HUD manifest checks (#9 host half, #13, overlaps through the hud-budget audit);
- the render checks (#7, #12);
- Waymark's playstyles;
- tier 3.

## 5. Sky decides

1. **When the Studio is free for overnight host runs.** The guard already refuses while a player is open. Is "every night from a set hour, unless the game is open" fine, or should runs happen only when you say?
2. **How the list reaches you.** Is a short list of at most 10 items, after each nightly pass, the right size? And where do you want it: the report link in chat, or Taildrop to the Air (only when you ask)?
3. **Tier 3 at all, now or later.** It costs nothing in money but uses the subscription's rate limits overnight. My suggestion: start it once tiers 1 and 2 have run for a week and the mechanical checks are quiet.
4. **The starting thresholds marked [S],** above all the cone share (≤ 8% of the frame) and Q-STUCK's 10 s. I'll tune them after the first pass unless you want to set any yourself.

## Probes (2026-10-24, read-only)

- `bun tools/game/playing.ts --check` returned "Sky is playing (pid 47722)", exit 1. So I ran no batch or timing probe, and the run times above come from brief 042.
- Read: GDD §13 and §11; brief 042; `next-batch.md` items 22-33 (Sky's notes of today); the `sim/cli` commands; `Script.cs` (`Report.Json`); `Debrief.cs`; `FailCause.cs`; `GameOrders.cs` (`Issue`, `Quote`, `Preview`); `GameSquadFire.cs` (`FireQuery`); `HostContext.cs` (`ContextFor` and Givers are host-only); the headers of ProofInput, LateRun, TpLongRun, ShotRunner, SimCheck and Proof29; `tools/game/playing.ts` and `batch.ts`; `tools/geometry` (walk.json, ratchet); `briefs/027/waymark/player`.
- Engine log codes the checks can use today: `vale.at-car`, `board.in`/`board.out`, `seal.starts`, `order.rejected`, `order.walk-in-ends`, `cancel.carry`, `carry.*`, `car.*` (178 codes in all).
- Sky's saved sessions: 6,085 files, 177 of them over 8 KB (real play). None from today were there yet.

## Sky's calls (2026-10-24: "yes to all your recommendations")
- Start the first slice.
- Game-bot runs nightly from 02:00 (Sky's local time) unless the game is open (the guard); change the hour any time.
- After each nightly pass: a short list (≤ 10 issues) as a link in chat; Taildrop only when Sky asks.
- Look review (tier 3) starts a week after the first pass, once the mechanical checks run quiet.

Order (owners are mid-round elsewhere, so the slice is staged): Suture's invariant checks and win/price tables now; Keel's traces after FailCause.Car (brief 057); Splice's `bun run qa`, rotation, report shell and the kerb rule after the rounded corners (brief 028); Latch's `-bot` run after the 0.2.100 batch. The nightly schedule is set once `bun run qa` and `-bot` exist.

## Tier 2 built: the `-bot` row (Latch, 2026-10-24; code in, not yet run: no batch until Aster says)
- **Player:** `-bot <plan.json> <outDir>` (`unity/Assets/Suborned/Runtime/Play/BotRun.cs`). One fresh Handover a row, at real time, through the Input System devices only: F1-F4 select, a left click on a thing opens the ring (item 33), the bot reads `HandoverHost.Ring` (built by `ContextFor`) and clicks a wedge (held 1.4 s for a held or harmful wedge: the wedge's hold and the going-loud card's), ground clicks walk, double clicks run, WASD pans, Q/E turns. Policies: `random` (any able agent, busy ones too; any thing on screen, 15% one with nothing open; any wedge, shut ones too; 22% walks), `greedy` (an idle agent; the way that moves the mission on, cheapest), `cautious` (no takedown, Fire, Pressure or Trail; Watch and Listen first). Conversation scenes are off for the bot (`ConversationScenes.Never`, as LateRun); third person (T) is not driven yet.
- **Checks in the player** (thresholds are constants at the top of BotRun.cs and written into each report's `thresholds`; Suture owns the numbers):
  - Q-GIVER: each ring (its wedges and its plan's givers) and each panel, once as built: no available way given by an agent who is down, carried, inside or in the car. Not yet: verbs 1-4, the Intel next step, the portrait drop.
  - Q-OFFER: a way taken from the ring, then refused by the engine within 5 s (same act and target). Every refusal is also counted by code.
  - Q-WARN-HUD: every frame the seal runs with no modal, the seal marker is on screen (`HudToolkit.SealWords`); at a `sealed` fail, the marker was shown at least once. Other fail codes have no host predicate yet (Suture's table).
  - Q-CARD: at the end card, the head against the result and `less.end.fail[code]`; no "escapes with Vale" on a failure; no "Vale reached the getaway car" without Vale's `board.in`; a sealed card's line against `SealedWords(FailCause)`. `card.json` keeps the lines for Fable's contradiction table.
  - Q-FEET: a drawn figure's feet more than 0.15 m off the walk-file floor (`SiteFrame.Height`) for 0.5 s (seated, lifted, laid down and carried figures skipped). Q-SOLID: a figure on a cell the engine cannot walk, 0.3 m or more from a free one, for 0.5 s.
  - Q-ERR (console errors and exceptions), Q-FRAME (a frame over 50 ms after 20 s that held no save, order, end, shot or bot work), Q-MEM (managed or total memory slope over 5 MB/min after the first minute), Q-STUCK (no end within the row's cap).
- **At each anomaly:** a 1080p HudToolkit shot kept as JPEG (quality 82) with its manifest (PNG, HUD layer and parts deleted), and the replay `anomalies/<n>-<check>-<tick>.script.json` (the session to that tick; `bun tools/qa/run.ts --replay <it> --engine build`). One per check per 2 m cell per 30 s, at most 60 shots a row. Per row also: `session.script.json`, `frames.csv`, `mem.csv`, `feet.csv` (every figure every 0.5 s), `hud.jsonl` (the HUD manifest every 2 s, for the budget audit and Q-NAMES offline), `card.json`. The bot's sessions are written to its own folder, never to the player's play folder (Sky's sessions are the tier 1 seeds).
- **Runner:** `tools/qa/bot.ts` (rows in `tools/qa/bot.json`: random-a, greedy-a, cautious-a; each seed mixed with the night's date and reported), one player process per row, killed by PID after the row's cap + 150 s, the guard asked before each row. Wired as lane `bot` in `tools/qa/run.ts`; `bun run qa --nightly` = lanes build, tree, bot (`--bot-rows`, `--bot-minutes`); a `--dev` pass never starts the player. Output under `out/qa/<run>/bot/`, rotated with the pass. Findings carry severity and owner by check (`CHECK_META`), the shot and the replay; the report embeds the shot (720 px JPEG) on the issue card and gives the replay on the build engine. Changes in Suture's files, additive: `Finding.shot/script` (types.ts), `Exemplar.shot` (issues.ts), the shot and the bot replay in report.ts.
- **Schedule:** the 02:00 run is Sky's app's scheduled task (Aster: `bun run world && bun run qa --nightly`); no LaunchAgent. A built player without `-bot` (0.2.112 and earlier: `playerHasBot` looks for the report schema in Suborned.Runtime.dll) skips the bot lane with a note, no finding, exit 0.
- **Tests:** `tools/qa/bot.test.ts` (plan seeds, findings, issue exemplars, the runner with a stand-in player, guard stop between rows, the clean skip on an old or missing player).

## Smoke triage (Suture, 2026-10-24)

Two `random-a` smoke rows of Latch's `-bot` (seed 121577): **0303** on 0.2.113 (`briefs/060/smoke-0303/`, row files in `out/scratch/latch/bot-smoke/`) and **0304** on 0.2.115 (`briefs/060/smoke-0304/`, row files in `out/scratch/latch/bot-smoke3/`). A third row on 0.2.115 with the same seed (`out/scratch/latch/bot-smoke2/`, not committed) was used as a cross-check. Each anomaly was checked against the engine's walk grid (`sim/data/handover.site.json`, 0.5 m cells), against what is drawn (`out/world/harbour-district.walk.json`, 0.25 m cells, kind 2 or 4 = solid), and against the collision volumes and Keel's 0.3 m face margin (`tools/sim/solids.ts`). The scratch probes are in `out/scratch/suture/q-solid/`.

### Q-SOLID: counts by cause

| cause | 0303 (21 counted, 20 kept) | 0304 (23) | verdict | owner |
|---|---|---|---|---|
| café customers at the opening, seated. `HostViews.FigureFeet` skips a seated figure by `sitTick >= 0`, but a figure seated at the opening gets `sitTick = tick − 23`, which stays negative for the first 2.3 s. crowd/29, 31, 34, 108, 109 and 113 at 0:00 | 6 | 6 | check | Latch |
| café customers sitting down or getting up. The chair sits inside the table set, so the last metre to it lies inside the grid's 0.65 m clearance, by design. 0303: crowd/35, 24, 25 and 109 (2:30). 0304: crowd/109 (0:16, 2:30), 24 and 25 | 4 | 4 | check | Latch |
| the followers of the rival crew and the response unit: security/1, rivals/1 and rivals/2. The host draws them at a fixed 0.9 m offset from the one pose the engine gives the group (`HostViews.cs` 175-177: rivals/1 +0.9 m on block x, rivals/2 and security/1 +0.9 m on block y), so they walk through whatever lies 0.9 m to the side of the leader's path: pavilion supports, cypresses, the café terrace wall, the Office steps | 7 (5 overlap a drawn solid by 9-22 cm) | 12 (6 overlap by 10-24 cm) | **real** (S3) | Latch |
| a walker in the grid's clearance, 32 cm from the nearest drawn solid (crowd/109 passing the café palm) | 1 | 1 | check | Latch |
| off the walk map (crowd/121 at (-160, 0), crowd/124 at (0, -380)); bounded to the grid since 0.2.115 | 2 | 0 | check, fixed | — |

- **Real: 19** (7 + 12), all from the follower offset: a visible fault (S3). 11 of them overlap a drawn solid by more than the 5 cm tolerance. The other 8 sit within 4 cm or clear, but have the same cause and the same fix.
- **The check's fault: 24** (13 + 11).
- **Pulse and Keel: nothing.** The group leaders (rivals/0, security/0) and every officer (unit/police-1..6) never tripped. The crowd tripped only at the café chairs and in one 14 cm graze past free ground, 32 cm clear of anything drawn. The engine's paths keep to free ground, and the grid matches what is drawn.
- **The check logic was wrong in four ways:**
  1. the seated skip (above);
  2. it had no café-seat skip;
  3. it measured to the nearest free cell's *centre*, not its edge, so 5 cm past free ground could trip it;
  4. it judged on the walk grid, whose blocked cells include the 0.65 m body clearance round ground solids. So "on a blocked cell" is not "inside something". For example, rivals/2 at 2:04 was flagged 89 cm from anything drawn.

### Q-OFFER: one Carry by a doer who went down

**Decision: a race.** Replaying `out/scratch/latch/bot-smoke3/random-a/anomalies/027-Q-OFFER-2049.script.json` on the build engine shows the sequence: police-1 hits the scout at 204.9 s (tick 2049), the scout goes down, and the carry order is rejected in the same tick. The scout was up on every frame the ring showed (Q-GIVER, checked as the ring was built, stayed quiet). Nothing on screen could have shown the down before the click.

- **What changes in the check:** a refusal for the doer's own state counts as a race, not Q-OFFER, when the doer went down in the order's own tick or the tick before it (≤ 1 tick, 0.1 s). That covers `agent-down`, `enforcer-inside` and `in-car`.
- **The latent HUD gap is real:** the ring is built once (`HostContext.OpenRing`) and never rechecked while open. If a doer goes down 1 s before the click, the wedge still looks available.
  - Latch: the open ring rechecks its givers each engine tick. A wedge whose giver goes down, is carried, goes inside or gets into the car re-ranks to another able giver, or shuts with its lock (agent-down).
  - Q-GIVER proves it: the open ring is checked every tick, not only as built, with a 0.2 s grace. A wedge still offered by a giver who has been down longer than 0.2 s is the HUD bug, S2.
- **The other Q-OFFER hits are known and fixed in code** (Latch, next build):
  - 0303: Fire on a busy agent, ×6 (`agent-busy`). Fire now waits for, or cancels, the act (Proof29 `fireBusy`).
  - 0304: Fire on a camera already shot dark, ×9 (`no-target`). The ring and the panel now drop it (FireQuery).
- **Cross-check row:** it adds one more, Listen on Inez refused `needs-routine`. No Watch on her ever completed in that run (every one was cancelled), so the routine was never known.
  - The random policy also clicks shut wedges. The check cannot tell yet whether that wedge showed its lock. If it was shut, the refusal is honest.
  - Latch: Q-OFFER skips a wedge that was shut at the click, and its observed text says "wedge shut" when it was. Until then this one ranks as S2 (Latch / Keel).

### Q-MEM: 12 MB a minute over the 3-minute window

**Not a leak, and not only warm-up.** The OLS slope from 60 s mixes three things:
- the post-boot dip and refill: total memory falls to about 1,250 MB at 30-75 s and is back to about 1,280 MB by 100-110 s, so a 60 s warm-up starts at the bottom of the dip;
- the fight's spawn step: about +20 MB at 180-195 s, when the response units (unit/police-5 and police-6, then police-3 and police-4) and the gunfire come in;
- shot transients: spikes of up to +70 MB around captures.

The same seed on the same build gave 12.1 MB/min (0304), 3.9 MB/min (cross-check) and -2.1 MB/min (0303). After 120 s, the 30 s floors are flat apart from that one step. Managed memory rises 0.4-0.5 MB/min after 120 s. That is about the size of the bot's own in-memory `frames.csv` and `feet.csv` buffers (StringBuilders plus the frame list, about 3 MB over 4 minutes).

**The rule now** (tools/qa/grade.ts, and the same values for the player):
- warm-up 120 s;
- the rest of the run is split into thirds, and each third's floor is its lowest 1 Hz sample, so transients drop out;
- it is a leak only when the floor rises on **both** steps by more than **3 MB/min total** or **1 MB/min managed**. A single step, such as the response spawning in, moves only one of them;
- it is judged only when each third lasts **at least 60 s**, so rows under 5 minutes of real time are reported "not judged". The random rows seal at about 3:30-4:00, so they are not judged. Greedy and cautious rows that play to the 8:30 close are.

In a synthetic test, 6 MB/min sustained trips it, while one 25 MB step and the boot dip do not. **No owner now.** If it trips on a long row, it goes to Latch, with Redline for the profiling.

### The other thresholds

| check | value | evidence |
|---|---|---|
| Q-WARN-HUD | **confirmed**: the seal's marker hidden for 0.5 s or more while the seal runs and no modal is up. A sealed fail whose marker never showed is a finding | 0303 had 1 hit on the seal's first frame, before the 0.5 s rule; 0304 had 0 |
| Q-STUCK | **confirmed**: 11 minutes a row (`tools/qa/bot.json` default), with the runner killing the player at cap + 150 s. The mission closes at 8:30, and shots hold the clock for at most about 60 s | both rows ended (sealed at 3:23.8 and 3:48.6) |
| Q-CARD | **confirmed**: zero contradictions (head against result and fail code, no "escapes with Vale" on a failure, no "reached the getaway car" without `board.in`, the sealed line against `SealedWords`) | both cards clean: "District sealed" / "Vale wasn't in the car when security sealed the district.", no problems |
| Q-FEET | **confirmed in the player**: 15 cm for 0.5 s. **Added offline**: the brief's share, feet within 5 cm of the drawn floor on 99.9% of samples, with carried and lifted figures left out | 0 hits; 99.95% (0303) and 99.98% (0304) within 5 cm; one 0303 sample over 15 cm (crowd/24, 18.8 cm, a single sample) |
| Q-FRAME | **confirmed**: no frame over 50 ms after 20 s that holds no save, order, end or bot work | 0 hits; p99 6.2 and 8.4 ms, max 25 and 34 ms |
| tier 1 (`DEFAULT_RULES`) | **unchanged**: warning lead 30 s, stuck 10 s, Vale at the car 10 s | — |

### For Latch: exact values and changes in BotRun.cs and the host

| what | now | set to |
|---|---|---|
| seated skip (`HostViews.FigureFeet`) | `sitTick >= 0` | a `seated` flag on Fig, set where the seat is applied (`f.seated = seat is { }`); skip when seated |
| café-seat skip (Q-SOLID) | none | `SeatSkipM = 1.5f`: a crowd figure within 1.5 m of a café chair (`host.CafeSeats.Chairs`) is not judged |
| Q-SOLID test | grid cell not free and ≥ `SolidFreeM` 0.3 m from a free cell *centre* | against what is drawn: `SolidBodyR = 0.25f`, `SolidOverlapM = 0.05f` (feet within 0.20 m of a walk-file cell of kind 2 or 4, for `SolidRunS` 0.5 s). Bake the walk file's `kind` into SiteFrame beside `heights` (`PlayInstall.WalkFloor` reads the same file). Until that is baked: `SolidFreeM = 0.3f` measured to the free cell's *edge* |
| feet.csv | `t,tick,key,x,y,z,floor,free` | add `skip` (1 when the feet checks skip the figure) |
| frames.csv, feet.csv | StringBuilders held all run | stream to disk as written (as hud.jsonl); p99 from a preallocated buffer or a 0.1 ms histogram |
| Q-MEM | `MemWarmS 60`, OLS slope > `MemSlopeMBMin 5` | `MemWarmS = 120f`; `MemThirdMinS = 60f`, `MemTotalMBMin = 3f`, `MemManagedMBMin = 1f` (held growth by thirds, as above); keep the slope in the report as information |
| Q-OFFER | any refusal of the taken way within 5 s | `OfferRaceTicks = 1`: a doer-state refusal (`agent-down`, `enforcer-inside`, `in-car`) whose doer was able in the tick before the order is a race, counted under refusals, not Q-OFFER; a wedge that was shut at the click is not Q-OFFER (say "wedge shut") |
| Q-GIVER | each ring once, as built | the open ring every engine tick, `GiverGraceTicks = 2` (0.2 s) |
| Q-WARN-HUD | 0.5 s | keep; skip frames while `capturing` |
| FeetTolM, FeetRunS, FrameSpikeMs, WarmS, dedupe 2 m / 30 s, 60 shots | as built | keep |
| the real fault | rivals/1-2 and security/1 drawn at a fixed 0.9 m offset | draw each follower on free ground: trail it along the leader's recent path (0.9 m and 1.8 m back), or offset it and then snap to the nearest free walk-grid cell, eased. Keel only if the engine should carry members |
| shots for figure checks | the view as it is (`Focus()` = the camera's look point) | for Q-SOLID and Q-FEET, centre the view on the figure for the capture (optional) |

### Tonight's report (02:00, `bun run qa --nightly`)

- The bot lane's anomalies are regraded before they rank (`gradeRows` in `tools/qa/bot.ts`, rules in `tools/qa/grade.ts`), so tonight's player (0.2.115, without the BotRun changes above) still reads cleanly. What a regrade clears stays in the full table as `info`, named by its cause, and never reaches Sky's list.
- Titles are plain:
  - "The ring offers Fire, then the game refuses it: there is nothing there to act on any more";
  - "The rival crew and the response unit walk through trees, walls and steps";
  - "Café customers at their chairs counted as inside a solid (the check, not the game)".
- Issues are keyed by cause, so the crowd, the rivals and the officers no longer split one fault three ways.
- The report heads itself "tiers 1 and 2" when the bot lane ran. The bot's issue cards give the policy, the seed, the mission clock and the replay.
- On the smoke rows, the list would be three items:
  1. S2: Fire offered on a dark camera (fixed in code, next build);
  2. S2: Listen offered without the routine (the cross-check row; Latch or Keel to confirm whether the wedge was shut);
  3. S3: the followers walking through things (Latch).
- The ranking is unchanged: severity × share, as the qa tests hold. With 3 bot rows, a bot share is 0, 33, 67 or 100%, so bot issues rank high next to tier 1's thousands of plays. That is intended: they are what the built player shows.
- Verified:
  - `bun test tools/qa`: 37 pass;
  - `bun run typecheck` clean;
  - the regrade and ranking on the smoke rows (`out/scratch/suture/q-solid/lane-check.ts`), with the carry race confirmed by replay on the build engine;
  - a `--dev` pass through the guard rendered the report (`out/qa/dev-20261024-161647/`).

## Human profile (Suture, 2026-10-25)

Sky, 2026-10-25: "I think the biggest influence would be a bot that's indecisive. Humans aren't consistent with their actions." Latch's `human` policy (`tools/qa/human.ts`, `BotRun.Human.cs`, rows human-a and human-b) follows a recorded winning route and puts indecision on top. The rates of that indecision come from Sky's own saved sessions, measured by `tools/qa/human-measure.ts` and written to `tools/qa/human-profile.json`. Latch's `readProfile` takes every value, with none defaulted. The full measures are in `briefs/060/qa/human-measures.json`.

### Whose sessions these are

- **The "177 real sessions" were mostly not Sky's.** The play folders hold 7,150 saved sessions, counting both the player's folder and the editor's `out/play`. A person plays at real time, so the wall clock from a file's first save to its last is at least 0.9 × the mission time it holds; pauses only make it longer. Automation fast-forwards, or plays one order sequence again in every batch, timed a little differently each time.
  - Of the 176 seeds `tools/qa/seeds.ts` takes ("over 8 KB"):
    - 121 are one 17-second shape (the Scout drives 135 ticks, then one Watch): a tp-long proof;
    - about 40 are late-row replays of Sky's session of 2026-10-22 17:54 (`sky-0.2.58-session1`);
    - about 10 are Sky's.
  - `s-20261024-022456` (brief 041 port-progress calls it "Sky's session") is a late-row replay: it ran 66 s of wall clock for 301 s of mission. `handover-20261022-202331` is the `loud-fight` late row.
- **Kept as Sky's:** 56 Handover sessions and 27 Fight Lab range sessions, from 2026-10-15 to 2026-10-23 (engines 0.3.0 to 0.26.0). They hold 1,077 decisions over 156 minutes of mission.
  - Dropped: 2,287 fast-forwarded files, 244 repeated real-time proof sequences, 23 copies of an earlier session, 4,512 too short or with no orders, and 1 host repeat (20 identical follow toggles in one tick).
- **Refusal codes need the engine Sky played.** 30 sessions replay exactly, every recorded checkpoint matching, on the committed engine of their version. Only those are read for refusals. Engines 0.8.1, 0.10.0, 0.14.1, 0.20.0, 0.24.0 and 0.25.0 were never committed and their builds are gone, so those sessions give timings only.
- **Sky tests; he doesn't play to win.** 75% of his sessions end with no ending: he leaves after a median 57 s, and 32% restart within 2 minutes. The bot shouldn't copy that. It plays each row to its end, and early quitting stays out of the profile (it is in `measured` only).

### The profile (`values`; one plan step = one decision)

| field | value | source |
|---|---|---|
| cancelProb | 0.223 | measured. An order undone before it landed. Acts: 0.34 (43 of 158 given another order before their time was up, 11 cancelled). Moves: 0.21 (230 of 1,178 sent > 3 m and > 30° off their heading, 14 stopped). Steering along the way (356) and walks cut short by the act they led to (113) are not counted. Move arrival assumes a straight walk at 1.4 m/s, so the move share is a floor |
| cancelDelayS | p50 1.9, p90 8.5 | measured: from the order to its undoing. Acts median 3.6 s, explicit cancels 1.0 s, redirected moves 1.7 s |
| repeatProb (new) | 0.221 | measured: per act step on a target, the same agent gives the same act on the same target again (38 of 172): "the repeat Watch". 53% of refused acts on a target are given again within 30 s. Latch adds the repeat behaviour |
| reorderProb | 0.15 | **estimated**: a step out of the plan's order needs a plan, and Sky has none. Bounds: 0.29 of his decisions go to a different agent than the one before (an upper bound, mostly the plan's own turn-taking); 0.006 are switches that did not stick (a floor) |
| switchProb | 0.006 | measured, **a floor**. Per decision: an order to the wrong agent undone within 5 s and given to another (4), or a decision on another agent and straight back within 6 s (3 of 207 such A-B-A triples). Selecting an agent without ordering is not recorded |
| gapS | p10 0.6, p50 2.6, p90 8.5 | measured: mission time between inputs (decisions, and taking an agent in third person), up to 15 s; pauses don't count. Across all gaps: median 3.1 s, p90 16.5 s. First order after the start: median 1.1 s |
| idleProb / idleS | 0.113 / p50 22.8, p90 50.5 | measured: gaps over 15 s (128 of 1,129) |
| abandonProb | 0.019 | measured: per decision, a line given up (20 of 1,077). Per line it is 0.45: 20 of 44 Watches or Listens on a target were never followed by a turn, call, loop, reroute, mislead, takedown or fire on it, counting only lines with 60 s of run after them. Inez 8, cameras 7, guard 2, patrol 2, driver 1 |
| misclickProb | 0.06 | **estimated**. Missed clicks that open nothing aren't recorded. Measured anchor: 8.7% of Sky's moves (90 of 1,032 in the faithful replays) landed on ground that can't be walked, and 56% of those were re-clicked within 3 s. Pointing studies run at about 4% errors (Soukoreff and MacKenzie 2004), and people here are moving targets 40-60 px tall. About half a route's steps are moves |
| ringOpenCloseProb | 0.12 | **estimated**. Ring opens aren't recorded. Every order shows its price before you commit, so looking and backing out is designed play. In the faithful replays 1 ring order in 30 was refused for knowledge or reach, and a lock seen first would be closed instead, several times as often. Novices browse radial menus (Kurtenbach and Buxton 1993) |
| cameraTurnProb | 0.05 | **estimated**. Q/E turns aren't recorded. The front is played at yaw 210 and the run opens at 30, so a run needs at least two quarter steps over about 50 decisions |
| wrongVerbProb | 0.03 | **estimated**. The trace a wrong wedge would leave (another verb on the same target by the same agent within 5 s, the first undone) is 0 of 172. 8-way radial menus run at about 2-5% wrong picks |
| thirdPersonProb / thirdPersonS | 0.10 / p50 2.3, p90 20 | measured: 108 third-person stretches (drive samples on one agent less than 5 s apart). 12 of 56 sessions used it, for 8% of mission time. It clusters by session, so a row-level "uses third person" flag (p 0.21), then 0.27 a decision in those rows, would be truer than one flat rate |

Every draw comes from a stream seeded by the row seed and the noise's name (`human.ts` `stream`), so a row replays exactly. `measured` in the profile holds the calibration targets:
- refused per order 0.137 (unwalkable ground 90, busy 29, fight orders 21, knowledge or reach 17, agent down 9);
- runs 0.20 of moves;
- squad orders 0.16 of decisions;
- wall clock 1.27 × mission time (median).

**Not in the sessions:** ring opens and closes, hovers, misses that open nothing, selection without an order, Q/E turns, pans, zoom, pause count (only its total, from the wall-clock ratio), and reading time.

### What the human bot should catch, and the checks that say it

These are new checks in `tools/qa/checks/human.ts`, plus `tools/qa/bot-human.ts` for the bot lane, wired into `run.ts` after `gradeRows`.
- **Where they run:** on every tier-1 seed play, and on each bot row's whole `session.script.json` replayed on the build engine.
- **What a human row adds:** its `human.events` say what the bot meant. A stop it meant is info; one it didn't mean is S2.
- **Where they are relaxed:** mutants are exempt where a mutation makes the finding on purpose. On a session the engine no longer replays as Sky saw it, the findings drop to info (as Q-OFFER does).

| check | in plain words | first seen in Sky's own sessions |
|---|---|---|
| Q-CANCEL | "A new order stopped the Listen the agent was doing" (the host's cancel and new order in one gesture); "A new order quietly ended an agent's walk to an act: the act never happened" (`order.walk-in-ends`); "Taking an agent in third person stopped their turn" (S2, Sky's #14) | 20261021-151046 at 4:12.8: the Fixer's Turn on Inez was aborted by a third-person drive. 20261021-151951 at 6:33.9: the Tech's Loop was lost the same way |
| Q-LOOP-LEFT | "The Tech left the camera loop for a move" (brief 058 §5). A route's planned walk-off at the Enter is info | 20261018-003824 at 2:06.7 |
| Q-INTENT | human rows: "The bot meant move, the game gave carry" (next-batch 31). Reads Latch's `given` events (tick, op, act, target) | needs the human rows |
| Q-PILE | "Orders piled up on a busy scout: 3 orders in 10 s refused as busy", or each stopping the act before | 11 times in the 0.3-0.4 sessions, where an act locked the agent |
| Q-STALE-RING | "The ring still offered fire on patrol, who was already gone": the target side of Q-GIVER. Within 0.1 s it is a race (info) | 20261019-221751 at 2:43.0: Fire on the patrol 2.3 s after it went down |
| Q-LEAK | "A cancelled watch still happened" (S1) or "still cost Trail" (S2), with no new order of it in between. It holds on every play | none (one false hit on cautious-a was another agent's own Watch of Inez landing; fixed) |
| Q-BOT-STALL | "A bot row stalled: it took 168 wedges and the game got 4 orders" (S3) | last night's greedy-a |
| Q-HUMAN-RATE | info for Latch: a human row's count of each indecision kind against steps × Sky's rate, flagged when it is more than 2.5× off | needs the human rows |

**greedy-a (2026-10-25 02:05, seed 788720): a bot bug, not "a re-order cancels the walk-in".** The engine got 4 orders, all by 0:10: the Enforcer and the Fixer moved to the getaway bay and boarded. After that it got none, and refused none. A walk-in starts only once the engine has taken the order, so there was nothing for a new decision to cancel.
- The policy ranks Go first (`BotRun.cs` GreedyOrder). With Vale outside, Go leaves the Scout and the Tech behind, so it opens a going-loud card that would fail the mission.
- The bot backs off such a card (`Source == "wait"`, `BotRun.cs:297`), then ranks Go first again, about 164 times. Board for the Scout and the Tech never wins.
- The HUD agrees: "Go · leaves Scout, Tech" for 23 frames, and the car's Board pill on hover for the rest.
- Fix (Latch): drop a way the bot backed off from for that target for a while, or rank Board above a Go that leaves people behind while Vale is outside.

### Handoffs

- **Latch:**
  - read `repeatProb`; until then `readProfile` lists it under `ignored`;
  - write `given` events (tick, op, act, target) for every step, so Q-INTENT can judge;
  - use the event kinds `cancel`, `reorder`, `repeat`, `switch`, `misclick`, `wrong`, `ring-close`, `idle`, `turn`, `peek`, `abandon`, `wander` (any case);
  - fix the greedy Go loop;
  - optional: a row-level third-person flag.
- **Seeds (Aster, 2026-10-25):** all 176 "over 8 KB" files stay mutation seeds, since automation still exercises the rules. Each is tagged `by: 'sky'` or `'automation'` by the real-time test (`seeds.ts`, `skySessions`). The size floor applies to automation only: Sky's own play is a seed at any size. **187 seeds: 22 are Sky's, 165 are automation.**
  - Only Sky's say "An order Sky gave", "(Sky)" or "what Sky saw" in Q-OFFER, the human checks and the report's steps. Automation says "a saved proof session" and "the recording", and its Q-OFFER key is `Q-OFFER:automation:...`.
  - The report's lane line counts Sky's sessions apart. The summary JSON gains `skySessions`.
  - Brief 041 port-progress no longer calls s-20261024-022456 Sky's.
  - Sky's sessions under 8 KB are seeds now, among them 20261022-202219 and 180154, both cited in next-batch.
- **Third person (Sky's decision, via Aster 2026-10-25):** going third person never stops the current act, so any Q-CANCEL:drive is a bug: S2, Latch.
- **Rerun:** `bun run guard bun tools/qa/human-measure.ts --replay --write` as Sky plays more. The replays are cached in `out/qa/human/plays/`.
- **Verified:**
  - `bun test tools/qa`: 63 pass;
  - `bun run typecheck` clean;
  - `bun run guard bun tools/qa/human-measure.ts --replay --write`;
  - `--dev` passes `out/qa/dev-20261024-215306/` and `-215350/`: 14.9 s; the stale ring reached the list on the first, then dropped to info as replay-differs;
  - the lane hook on last night's rows (`out/qa/20261024-190031/bot/`).
