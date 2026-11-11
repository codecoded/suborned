# Brief 111 · Rules architecture: evidence for the crew (Redline, 2026-11-12)

Review only. Nothing was changed, built or run heavily. The measurements are reads of the tree at `03f053a57` plus the working tree (Latch's uncommitted brief 110 host edits are in it), git history since 2026-10-15, and the last sim:test timings (`out/sim-tests/timings.json`, 2026-11-11). Sky's page is `plan.html`. This file is the evidence behind it.

## 0. Aster's observations, checked

| Aster saw | Verdict | Evidence |
|---|---|---|
| Engine ~185 files, 40k lines, Unity-free, deterministic | **confirmed** | 185 `.cs` files, 39,760 lines in `sim/Suborned.Sim` |
| 547 values, 37 events, 30 need kinds | **confirmed** (29 need kinds are used in `needs`, plus 17 `when` kinds in outcomes; 20 effect kinds) | `contracts/rules/rulebook.json`; all 547 values are referenced by a section or an event (0 dead dials) |
| 34 sections carry their rule as prose | **confirmed** | 34 top-level sections have `rule`; 72 `rule` strings in all. Prose (`rule`, `src`, `reason`, `note`) is 301,593 of the file's 450,152 characters (67%) |
| The whole rulebook is in the hash, prose included | **confirmed, and wider than stated** | `Debrief.cs:194` hashes the canonical text of the rulebook, people rules, site, people, provisional, takedown and rounds files. `Debrief.cs:259` passes that hash as `startText`. `Game.cs:104` folds it into `StartHash`. `Hash.cs:80` folds `StartHash` into **every checkpoint**. So any byte of any input moves every checkpoint of every run, even where behaviour is identical |
| DP1 re-recorded and digests churn every round | **DP1 confirmed; skyline and cover not from the rulebook** | DP1 (`sim/Suborned.Sim/Fixtures/drive-session.script.json`) was re-recorded **45 times since 2026-10-15**. 40 of those kept the same orders, the same checkpoint ticks and the same outcome: only the hashes, MVID and engine string moved. No outcome changed in any of the 45. I found no skyline or cover digest that hashes the rulebook. `tools/geometry/sim.ts:52` reads one value (`eye-cm`), and the skyline `kit.digest.json` (8 changes) follows the kit. The **dialogue** pins do hash the whole rulebook (`tools/levels/dialogue/pins.ts:13`, `drift.json` changed 10 times) |
| Decisions live in the Unity host (~20k lines of `Host*.cs`) | **confirmed** | 121 `Host*.cs` files, 20,125 lines (the Play folder has 300 files, 53.5k lines). Details in §2 |
| Tests pin exact timings: 42 route scripts; pace 50 moves 9 | **confirmed** (routes); the 9 is Keel's figure, not re-run | `contracts/missions/handover/routes/routes.json` lists 42 scripts, each with `expects.outcome`. `briefs/110/keel/orders.md` reports 6 moved at pace 70 and 9 at pace 50 |

One nuance changes the order of the plan. Only **8 of 56** rulebook commits since 2026-10-15 were prose-only (`src`, `reason`, `rule` and so on, checked by stripping those keys and comparing before and after). A semantic inputs hash alone saves those 8. The larger waste is that **checkpoints carry the inputs hash at all**. If checkpoints hashed only state, DP1 would have kept its hashes in 40 of its 45 re-records.

## 1. Where rules live today

| Layer | What sits there | Examples |
|---|---|---|
| **Data** (`rulebook.json`, `people.rules.json`, `population.json`) | Numbers (547 values, `v:` refs resolved at load, `Rules.cs:1117-1130`). Needs, price and outcomes for 37 events. Presence switches: a sub-object's presence turns behaviour on (`cell.driveCar`, `Rules.cs:504`). Brief 110's `cell.listen.follow` plays 0.55.0 only when present, and the engine plays 0.54.0 without it | Listen's needs, Pay's `routine` requirement (`people.rules.json` approaches), `act-pace-pct` |
| **Engine code** | Everything the prose describes. Only the cell's learning and turning events are interpreted (`Rules.cs:138-141` `MissionKinds` and `MissionEffects`; `GameOrders.cs:443` `Holds`, `:376` `Apply`, `:267` `R.Ev("turn."+…)`). The takedown, sight, response, fights, traffic, rounds, getaway and bodies are hand-written modules that read named `Rules` fields (236 public fields). Refusals use 129 stable codes (`Model.cs:265` `Answer.Codes`, `GameOrders.cs:33` `No()`) | `GameListenFollow.cs` (new in 0.55.0), `GameTakedown.cs`, `GameSight.cs`, `GameResponse.cs:283-291` (Motive's police stall) |
| **Host code** (Unity) | A second legality check, order translation, hints, and what-the-cell-knows views. See §2 | `HandoverHost.cs:1238-1289` `Locked`; `:997` `Playable`; `HostValeSteps.cs:24,61,178` door state; `HostHints.cs` H2 |
| **Words** | `text.json` locks restate which verbs a refusal applies to (`text.json:489` `needs-routine` `verbs: ["pay"]`, edited in 110) | 154 locks |
| **Tests** | 491 scenarios in the DLL, each with ticks and checks. 42 fixed-tick route scripts. 18 adaptive plans judged in bands (brief 080). DP1 pinned by hash | `ScenariosListenSpot.cs` rewritten in 110; `ScenariosDriveProof.cs:34-35` |

### What a typical rule change touched (case studies)

**Brief 110** (Listen needs no Watch; orders act at once; act-pace): commit `2949c5701` touched **32 files** (16 engine, 6 bot plans, 4 rulebook and schema, 1 text). It added a schema file (`rulebook.schema110.ts`), a test file (`rulebook110.test.ts`) and one line in `tools/sim/unity.ts:36` `SCHEMAS`, and re-recorded DP1. It left **8 host handoffs** for Latch (`orders.md` "Open / handoffs"), plus words for Fable and Waymark's re-plans if the dial moves. Of the three parts:
- the need edit was a one-line data change, but its words, hint and HUD "Next" are in the host;
- the follow is a new mechanic, so it is engine code under any architecture;
- the dial is data plus a small loader.

**Brief 105** (Take car; instant ends): five commits, about 114 files, five agents (Motive rules page → Keel 31 files → Latch 35 → Waymark 9 → Fable 7). Take car is a new mechanic, so it would still be engine code under "verbs as data". What data would remove is the host's mirrored lock (`HostLent.cs:74-75`, `HostTakeCar`) and the duplicated words-to-verbs lists.

**The provenance failure** that started this: before 110, `scout.listen.needs` had `{"k":"known","what":"routine"}`. Its event `src` ran: "…top-down test listenInez needs the café watched first; Sky 2026-10-15: Listen reveals need and temperament…" (`git show 2949c5701^:contracts/rules/rulebook.json`). The need came from the prototype test. Sky's ruling covered only what Listen *reveals*. The decision log now says so (decision log line 56: "its routine-known need came from a crew test, not a ruling"). `src` is per event, not per need, so nothing could flag that clause on its own.

## 2. The host decides rules (file:line)

The host is meant to be a view. Today it also:
- **Mirrors legality for the ring.** `HandoverHost.cs:1238-1289` `Locked` re-derives the engine's answer verb by verb. Turn needs at `:1255` (its own `needs-routine` words: "Watch first"). The loop at `:1263` and the take at `:1278` are commented "the engine's Take, mirrored for the card". Plus `HostCrewTurn.cs:93`, `HostLent.cs:74-75`. The engine has partial queries: `GameOrders.cs:243` `LearnQuery`, `GameCoverPin.cs:251` `PreviewMove`, `Preview` for prices, `CrewActQuery`. It has no general dry-run of `Check`.
- **Translates orders before the engine sees them.** `HandoverHost.cs:997` `Playable`:
  - re-points a move up to 3 m;
  - under HoldOn, cancels a busy agent's act before an engaging order (`:1011`);
  - cancels the act for any Fire (`:1016`);
  - re-walks and queues a patrol Watch in `pending[]` (`:1023`).

  The recorded order log carries the result, so replay holds. But the adaptive bots call `Game.Issue` directly (`sim/cli/Adaptive.cs:6`), so **the sweep does not play the player's order rules**.
- **Keeps its own model of what the cell knows.** `HostValeSteps.cs:24` `DoorState`: Glyph's ruling, "the door as the cell knows it". `:61` `EnterAvailable` and `:178` refuse the door marker on "seen". The bots have a separate projection (`Adaptive.cs:46` `VisibleLog`, and the `Sees` conditions). Two lists of "what the player can see" can drift.
- **Hints and suggestions** carry rule order: `HostHints.cs` H2 (Listen after routine, fixed in the working tree) and `HudModel.cs:117` "Next".
- **Its own refusals:** 27 distinct `PopKey` codes in the host (for example `door-seen`, `no-way`, `fire-carrying-vale`). Several mirror engine codes (`never-vale` exists in `GameCombat.cs`).
- **Mission-specific literals:** 412 role-string literals (`"waitress"`, `"driver"`, `"guard"`, `"councillor"`, `"patrol"`) across `Host*.cs` and `HudModel*.cs`.

Latch's working tree is already removing the 110 queues (comments at `HandoverHost.cs:1011`, `:1401`). The structural point stands: legality exists twice.

## 3. Tests and their cost

From `out/sim-tests/timings.json` (3,425 tests, 3,195 CPU-s; 174 s wall per Keel's 110 run):

| Method | Cases | CPU-s | Reads rule values? |
|---|---|---|---|
| `RedlineTests.E7_every_report_is_the_same_in_every_culture` | 20 | 1,269 | no: an engine property (culture invariance) |
| `ScenarioTests.SameOrdersSameChecksumsTwice` | 491 | 341 | no: determinism |
| `ClosingHashTests.EveryScenario` | 491 | 230 | no: an engine property |
| `ScenarioTests.ReplaysFromStartStateAndOrders` | 491 | 218 | no: replay |
| `FailCauseTests.EveryScenarioEndsWithAWellFormedCauseThatReplays` | 491 | 216 | mostly an engine property |
| `ScenarioTests.Passes` | 491 | 201 | **yes**: the rule checks |
| `FailCauseTests.EveryFailKindTheHandoverReachesIsCoveredByAScenario` | **1** | 161 | yes: one 161 s test, the wall-time floor |
| `RulesTests.EveryRefusalHasAStableCodeAndTheHashReadsTheCodeNotTheWords` | **1** | 158 | no: an engine property, one test |
| `RoundsTests.WithTheRoundOffEveryScenarioPlaysAsWithNoRoundsFile` | 1 | 76 | no: a back-compat property |

About **72% of the suite's CPU checks engine properties** (determinism, culture, replay, closing hash) that a rulebook-only edit cannot break. The sweep, with replay on, catches the rare value that steers into a non-deterministic path. The two single 160 s tests set the wall-time floor, so split them by case. The seed sweep (`suborned-sim batch … --seeds 100`) takes about 26 s with `--no-replay` (brief 105).

Fan-out of a core verb: Listen appears in 19 of 62 scenario files, 16 of 18 adaptive plans and 17 of 42 route scripts. Dependency tracing therefore saves little on the core verbs and a lot on peripheral systems (traffic, bodies, rivals, look).

Already right and to keep: the adaptive bots see only what a player sees (`Adaptive.cs:9-11`, unknown = reject); the bands checker splits RIGID from BAND (`tools/missions/check/bands.ts:3-6`); `proofdelta.ts` uses a declared map with a fail-safe (unplaced = run all), already field-level for `mission.json#…`. **But the rulebook row is file-level with `"shots": "all"`** (`tools/game/proofmap.json:47`), so a `src` edit reshoots every picture.

A manual behaviour-hash tool exists: `sim/cli/Program.cs:22-25` `hashes --start-text <t>` "replaces the inputs hash in the start state (a behaviour-only comparison across an input edit)". Also `--standing-as` and `--legacy-start`. Step 2 makes this the default.

## 4. Legacy and compatibility cost

- `Start.cs`: `Pre039`, `Pre080`, `OldClock` switches.
- 41 "plays as before / pinned before" branches in the engine.
- 76 "optional only for runs pinned before it" mentions in `contracts/rules/*.ts` (for example `rulebook.schema.ts:474`, `:502`).
- 8 per-brief schema files (`rulebook.schema096*.ts` … `schema110.ts`) and 10 per-brief test files. Each needs a line in `tools/sim/unity.ts:36` `SCHEMAS` and in `proofmap.json`.
- Pinned runs replay from their own `git archive` tree (`tools/sim/unity.ts:61`; the missions check `--engine tree`), so the current engine need not parse every old rulebook. A retirement policy can drop these as their pins move to tree replay.
- Two validators of one format: zod (`rulebook.schema.ts`) and C# `Only()` (155 calls in `Rules.cs`). Keep both, since they serve different readers, but every new key is written twice.

## 5. Hygiene found on the way (small, for the step that touches them)
- `Debrief.cs:209` `Engine.Version` carries a **42,453-character changelog on one line**. Move it to a `sim/CHANGELOG.md`; keep the const short.
- `Engine` (load, play, inputs) lives in `Debrief.cs`; move it to `Engine.cs`.
- Over 500 lines: `HandoverHost.cs` 1,616, `Rules.cs` 1,226, `rulebook.test.ts` 798, `rulebook.schema.ts` 673, `Game.cs` 682, `GameOrders.cs` 672.
- Saves: `FrontEnd.cs:696` resumes a session only on the same `Engine.Version`. Every engine bump drops Sky's in-progress save. That is unchanged by this plan, but worth knowing.

## 6. The staged plan (details for implementers)

### S1 · Test tiers (Splice, about 3-4 h, low risk)
- Tag test classes or methods as `rules` (Passes, FailCause coverage, Bodies, Sight, Dial, Traffic and so on) or `engine` (E7, SameOrdersTwice, ClosingHash, Replays, Refusal codes, Rounds-off).
- `sim:test --tier rules` runs when only `contracts/**` or `sim/data/**` change. The `engine` tier runs when `sim/**/*.cs` changes, nightly, and behind a Sky build (it is already the full batch's job).
- Split `EveryFailKind…` into a theory by fail kind.
- Done when: a rulebook-only edit runs the rules tier plus the sweep with replay in ≤ 90 s on the Studio; the full suite still runs behind.

### S2 · Behaviour hash (Keel 4-6 h, Splice 2 h, medium-low risk)
- (a) Inputs hash over the semantic form. Drop the string values of `rule`, `src`, `reason`, `note`, `author`, `gdd`, `proposed` and `tag`, but **keep the keys and the objects**: presence is a switch (`Rules.cs:504`; follow in 0.55.0).
- (b) `StartHash` = start state + dial only; stop folding `Inputs.Hash` in (`Debrief.cs:259`, `Game.cs:104`). `Expect` still carries `inputs` and `engine` (`Script.cs:46`, `:54-57`) for exact-replay verification.
- (c) DP1 asserts behaviour (same checkpoints and outcome) and records `engine` and `inputs` as information, not as a fail.
- (d) `proofdelta` and the dialogue pins read the semantic hash.
- Proof test: load the rulebook with every prose string replaced by `"x"`; every scenario's checkpoints are identical.
- One-time re-record of every pin (DP1, `tools/game/pins/*`); old runs replay from their tree. Bump the engine version (in-progress saves drop once, as at every bump).

### S3 · Legality parity test (Latch, about 3-4 h, no game risk)
- In `tools/game/hostlogic`: at every order tick of every scenario (and a sweep sample), for every ring row, compare `Locked(...)` with the engine's answer to the same order: same `ok` and same `code`.
- Until S5 lands, "the engine's answer" is `Issue` on a snapshot (`Engine.Play` to the tick, then Issue). That is slow but only a test.
- Expect mismatches at Turn needs, `door-seen` and fire on Vale.

### S4 · Provenance, stage 1, code only (Splice 3-4 h + an Aster convention, no risk)
- `src` per need and per law clause (schema: an optional `src` on each `needs[]` item and on each section's `rule`).
- A check on the semantic diff (from S2): every *changed need, refusal or law* (not numbers; 481 of 547 values are `proposed: true` crew numbers, which is normal) must cite "Sky YYYY-MM-DD" or a decision-log entry. Otherwise it is flagged "crew invention — ask Sky".
- Back-test: the commit before 110 flags `scout.listen` `known routine` (src: the top-down prototype).

### S5 · One legality in the engine (Keel 12-20 h, Latch 6-10 h, medium risk)
- `Game.Query(Order) → Answer`: split `Check` into a pure validate and a start, verb group by verb group: learn and turn, then camera and patrol, then the takedown family, then Vale and the car.
- A cell's-view read model (what the cell has learned: the door's watchers, routines, "seen") is served to the host, the hints and the Adaptive bots.
- Order intent moves into the engine. "An act order to a busy agent replaces its act at the cancel line's cost" becomes a rulebook law, not host code. A move re-pointed within 3 m becomes the engine's `NearestFree`.
- The host's `Locked` becomes `Query` plus words. A new engine version; recorded logs change shape only for translated orders.

### S6 · Bands over tick pins (Waymark 6-10 h, Keel 4-6 h, low risk)
- Route scripts become adaptive plans, or report-only regression examples (an outcome move is a BAND report, not MC-0 fail), except where a route proves a RIGID line.
- Scenario checks that pin act durations read them from the rulebook (`Game.T(R.…)`). Find them by running the suite at `act-pace-pct` 50 once.
- Done when: pace 50 fails no rigid check.

### S7 · Exploit hunter, GER G2 (Splice 8-12 h, Motive 2 h)
See plan.html. The generator writes `suborned.mission.adaptive/1` plans (rejected at load if they read anything a player can't see). The evaluator is `suborned-sim batch --digest --seeds 100` through `bun run guard`. The score is code: win %, median finish, total Trail and Heat against the prepared-quiet bands. The refiner reads the digest's fail causes. Output: a band report for Motive, new rows of `style: exploit`.

### S8 · Rule-dependency tracing (Splice 8-12 h, Keel 2 h, low risk)
- (a) Value → `Rules` field map by perturbation: load the rules with each of the 547 values nudged and diff the `Rules` fields by reflection (seconds).
- (b) Field → code → tests: static references to `R.Field`, plus per-test coverage nightly on the Studio. Batch plans covered the same way.
- (c) `bun run rules:affected <diff>` lists scenarios, plans, route scripts and proof sections; `sim:test --affected`. Unmapped = run all.
- Back-test on 20 past rule commits: the selection includes every test whose result changed.

### S9 · Provenance, stage 2, model + RAG (Splice and Fable 6-8 h for the loop, 5-7 h for the corpus)
See plan.html. Add `ruling` chunkers to `tools/rag/corpus.ts`:
- decision-log entries, with authority `Sky` or `Aster` by the entry's lead;
- each rulebook rule, need and value with its `src`/`reason` (authority `rulebook`);
- Sky's quotes extracted from `briefs/**/*.md` (the quoted string after `Sky, <date>:`).

The corpus hash changes on every rulebook edit, so keep the rulings in their own database, or ingest by chunk key incrementally. Otherwise every rule commit re-embeds the GDD too. The evaluator must quote, and code checks each quote verbatim against the source.

### S10 · Verbs as data (Keel 30-50 h over 4-6 rounds, medium-high risk unless each round moves no behaviour hash)
- The cell's verbs become events: needs (reach, knowledge, busy, carrying, target state), price, act time, effects, and a refusal code per need.
- Locks' `verbs` lists are generated from them. Behaviours such as follow-within-reach and walk-in stay code modules that a verb names.
- Every module declares the dials it reads, held by a test using S8's map.
- Each round moves one verb group with **identical behaviour hashes** (S2 makes "no change" provable).

### S11 · Rule-to-data drafting, GER G3 (6-10 h, after S10, optional)

### S12 · Retire legacy (Keel 4-6 h per pass, continuous)
Drop the `Pre0xx` switches and the "optional for pinned runs" fields once their pins replay from their own tree. Fold the per-brief schemas into section files.

## 7. Keep (don't change)
- The Unity-free, integer, tick-based engine; FNV state hashing; canonical inputs (no BOM, LF).
- "No bytes until state" hashing (Hash.cs throughout).
- Strict loading (`Only()`, unknown = reject); every value referenced; stable refusal codes with words outside the hash.
- Scenarios in the DLL (Unity, the CLI and tests run the same code); the hostlogic harness outside Unity.
- The adaptive bots' visibility limit and the RIGID/BAND checker; the sharded sim:test; the one-process sweep; proofdelta's declared map with its fail-safe; `src` and `reason` on every value.
- Not wanted: a rewrite, a rules-engine library, a scripting language, any model at runtime, auto-tuning.

## 8. GER and RAG for the rules work (Aster's two additions)

Today's loop: `tools/style-agent` (`core.ts` `runRounds`, limits `maxCalls 8 / maxTokens 200k / maxRevisions 3 / 15 min`). About 2 min and 50-100k tokens a run, on the Codex plan or the Claude subscription (`--judge-model`). Lesson from its own history: the rubric went through about 20 versions (/4 → /24), and Opus once scored 9 with no findings four rounds running. A model's score drifts with its rubric. So in every use below, **code decides pass or fail** and the model only proposes or flags.

| Use | Generator | Evaluator | Refiner | Cost | Risk | Done when |
|---|---|---|---|---|---|---|
| **G1 provenance** (S4 code, S9 model) | the change's semantic diff (S2) plus the author's note | code: every changed need, law or refusal cites `Sky <date>` or a decision-log entry. Model: reads the retrieved rulings and names any conflict, quoting it; code checks each quote verbatim | rewrites the `src` or drafts the "ask Sky" question for Aster | S4 3-4 h; S9 6-8 h loop + 5-7 h corpus; ~50-100k tokens a run | none: it flags only | the commit before 110 flags `scout.listen` `known routine`, and retrieval returns decision-log line 56 and Sky's 2026-11-11 quotes ("Scout can always listen and watch", "No commands should be waiting") in the top 5. Limit: host-code rules (the 110 queues) only come under it after S5 |
| **G2 exploit hunter** (S7) | adaptive plans (`suborned.mission.adaptive/1`; unknown = reject; only `VisibleLog`/`Sees`) for themes: shoot through, rush the car, bribe everyone, kill the guard, ignore Vale's clock | `suborned-sim batch --digest --seeds 100` through `bun run guard`. The score is code: win %, median finish, total Trail and Heat against the prepared-quiet bands | sharpens the plan from the digest's fail causes toward the easiest win | 8-12 h + Motive 2 h for the scoring; per hunt 5-8 rounds × (one call + a ~5 s one-plan sweep) ≈ 15 min, 300-600k tokens | none to the game | it reproduces or rules out Sky's "I ended up just shooting my way through" (crew prepared-loud: 68 % wins, 5:53; unprepared-loud 0 %). Found plans land as `style: exploit` band rows for Motive, never as fails |
| **G3 rule-to-data** (S11, after S10) | the data entry drafted from Sky's ruling | zod + C# load (unknown = reject), S8's affected tests, the sweep bands, G1 | fixes from the failures | 6-10 h; ~100k tokens a draft | low: Keel reviews every draft | Keel accepts drafts with small edits |

**Not for:** auto-tuning values until the bands pass (Sky: "laws, not sums", brief 098; CLAUDE.md "don't tune or pin a script to win one replay"); judging correctness; writing engine C# unattended; deciding what Sky meant (it flags "ask Sky"); anything at runtime.

**RAG (SurrealDB, `tools/rag`):** chunks carry `authority` and `version` (`corpus.ts:14`), so a ruling chunk can say Sky or crew.
- New sources: decision log by entry; rulebook rules, needs and values with `src`/`reason`; Sky's quotes from `briefs/**/*.md`.
- Because the corpus hash changes on every rulebook edit (`corpus.ts:2`), put the rulings in their own database, or ingest by chunk key.
- **Wrong tool** for the rules themselves (the engine reads exact JSON; nothing is ever retrieved into play), for crew coding (exact `rg` with file and line), and for any verdict.
