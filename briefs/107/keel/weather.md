# Brief 107, item 1: weather and light in the start state (Keel, engine 0.54.0, 2026-11-11)

Sky, 2026-11-11: "Yes to all three". The weather and the light only change how the street looks. They are rolled from the seed, can be changed at the briefing, and stay the same for the whole mission. No rule reads them yet. They sit in the start state so a later rule could read them, and so saved runs replay exactly.

## What changed
- **Engine** (`sim/Suborned.Sim/GameLook.cs`, new, 87 lines): `LookRules` (the dial), `Start.Weather` / `Start.Light` / `Start.RollLook(rules)`, `Game.Weather` / `Game.Light` / `Game.Night`, and the briefing's two orders. Small hooks in `Start.cs` (parse and write), `Rules.cs` (reads `look`), `Game.cs` (`InitLook()`), `GameOrders.cs` (routes the two verbs), `Model.cs` (two refusal codes). Version `suborned.sim/0.54.0` (`Debrief.cs`).
- **Rulebook** (`contracts/rules/rulebook.json`): a new `look` section with the odds as `v:` values, `readBy: []`, and a rule saying it is a look only. A new stream key `streams.keys.look = "{seed}/look/{stage}"`. Schema in `contracts/rules/rulebook.schema107.ts` (hooked into `rulebook.schema.ts` on existing lines, because that file is over 500 lines). Test file `contracts/rules/rulebook107.test.ts`.
- **Text** (`contracts/missions/handover/text.json`, `text.schema.ts`): the text tests need a line for every verb, refusal code and log code the engine has, so I added placeholder lines: actions `weather` and `light` (added to CONTROL_VERBS), locks `look-unknown` and `look-fixed`, and silentLogs `start.weather` and `start.light`. **Fable should reword them**; I only wrote enough for the tests.
- **Tools** (one line each, Splice's files): `tools/game/proofmap.json` now matches `rulebook.schema1*.ts`, and `tools/sim/unity.ts` SCHEMAS now copies `rulebook.schema107.ts` (the rulebook schema imports it).
- **DP1** (`sim/Suborned.Sim/Fixtures/drive-session.script.json`) re-recorded: the same 142 orders and the same checkpoint ticks; only the pinned hashes and the engine version changed (see the evidence).

## The odds (a dial, proposed)
Glint's page names the kinds but gives no odds. These are my starting weights, marked `proposed` in `values`, for Sky or Glint to change:

| weather | clear 55 | overcast 20 | rain 15 | mist 10 |
|---|---|---|---|---|
| **light** | **day 75** | **night 25** | | |

The draw is the rulebook's `streams.draw` on its own key, `{seed}/look/weather` and `{seed}/look/light`. Every other start draw uses a key with its own character id (`{seed}/{id}/{stage}`), so no existing roll moves. Seed 0, the neutral start, is clear and day. Unknown kinds are rejected in the rulebook (both the schema and the engine), in a start, and in an order. A rulebook that names a reader in `look.readBy` is also rejected: having a rule read the look is a design change for Sky.

## What Latch reads (field and order names)
- **New game:** after the seed, `s.RollLook(Bundle.Rules)` (in `HostFightLab.NewFightGame`, beside `s.Seed = SeedForBegin()`). This writes `start.weather` and `start.light` into the start, so the saved session carries them.
- **Replay / Restart (BeginSameSeed):** keep the game's own: `s.Weather = Game.Weather; s.Light = Game.Light` (then RollLook leaves them alone).
- **Draw from:** `Game.Weather` (`"clear" | "overcast" | "rain" | "mist"`), `Game.Light` (`"day" | "night"`), `Game.Night`. The kind lists are `LookRules.Weathers` and `LookRules.Lights`.
- **Briefing:** `Issue(new Order { Tick = 0, Op = "cell", Do = "weather", To = "rain" })` and `Do = "light", To = "night"`. Both are taken only at tick 0, before the clock runs. After that they are refused with `look-fixed`; an unknown kind is refused with `look-unknown`. The Day/Night tab (`HudToolkit.cs` ~546) should give the `light` order instead of only calling `SetNight`. Then the view follows `Game.Light`, the player's choice is replayed, and the `suborned.handover.night` PlayerPrefs no longer decides anything. `-night` can become `Start.Light = "night"`.
- **Log:** `start.weather` and `start.light` at tick 0, when the start sets them and again for each order taken. They are log-only codes with no card. The host's Status line shows the last log text, so filter them there if needed.
- **Old saves:** a start with neither field plays clear and day, logs nothing and hashes exactly as on 0.53.0.

## Evidence
- `bun run sim:test`: **3420 passed, 0 failed** (169 s). This includes `Look107Tests` (8 tests): the twin vectors, seed 0, no bytes without the look, RollLook round trip and rejects, the look changing nothing in play on four routes, the tick-0-only orders, and a replay with the orders that verifies.
- **Scenarios and route scripts, behaviour only:** `suborned-sim hashes --start-text 7575666061575172186` (the 0.53.0 inputs hash), compared with the 0.53.0 baseline: **535 of 535 identical, every checkpoint hash.** The start hash includes the inputs hash, so any rulebook edit moves every hash; this is the standard behaviour-only check.
- **Seed sweep:** `suborned-sim batch contracts/missions/handover/adaptive --seeds 100` (with replay; 18 plans, 1800 plays, about 45 s), 0.53.0 compared with 0.54.0: **all 1800 identical in every field** (outcome, fail and cause, ticks, cash, Trail, Alert, Standing, log, ledger, orders, debrief, stats, checkpoint ticks) except the hashes. Every play's start hash moved by the same XOR constant, `0xec5d5d9a744562b6`, which is the rulebook text alone. Wins are unchanged plan by plan (for example quiet-a 99, quiet-b 100, prepared-loud 68, veteran 47, veteran-drive 8). All replays are identical. The 43 route plays are the same.
- **DP1:** re-recorded with `suborned-sim drive-session`. The same 142 orders, 33 corrections and 3 checkpoints. The start hash moved by the same `0xec5d5d9a744562b6`.
- `bun run typecheck`: clean. `bun test contracts tools/game/proofwhat.test.ts tools/sim`: 439 pass, 0 fail.
- `bun run test` (the whole suite): 10 failures. 9 fail the same way on a clean `git archive HEAD` copy, so they were already failing before this change (drift-auto ×2, levels skyline ×3, skyline checks, SK-3 label, rounds "quiet play can reach", simview final hash). The 10th was the proof map's missing row for `rulebook.schema107.ts`, which is now fixed.

## Open
- The odds are mine, not Glint's or Sky's: they need to be confirmed.
- Fable should reword the six placeholder text lines.
- The GDD (§07 or §11, and decision-log Q7) does not yet say the look is in the start state: that needs a GDD version from Aster.
- `tools/levels/dialogue/drift.json` will see `rulebook.schema.ts` and `text.schema.ts` changed (drift:auto, at commit).

Next owner: **Latch** (HostFightLab / HostSeed / HostNight / HudToolkit: RollLook at Begin, keep the look on Replay, the briefing's orders, draw from `Game.Weather` / `Game.Light`). Done-check: a new game's saved session has `start.weather` and `start.light`, and replays with the briefing's pick.
