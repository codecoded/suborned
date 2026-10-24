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
| 15 | lag late in a run | Q-FRAME, Q-MEM | host | whole real-time runs, measured on the Studio (the Air gets its own row later): p99 ≤ 16.7 ms; no frame over 50 ms except boot or load; the last 2 minutes' p99 ≤ 1.2× the first 2 minutes' (after warm-up); memory (managed and native) grows ≤ 5 MB a minute [S] after warm-up. These are the late-fight gates from `tools/game/late.ts`, applied to every minute | all four |

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
