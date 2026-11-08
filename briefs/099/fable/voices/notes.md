# Brief 099 voices · step 1: fix the bar, a sample for Sky (Fable, 2026-11-08)

Sky, playing: "I've noticed we have a lot of poor wording stil throughout. Especially agents talk to player". Nothing here touches `text.json`: the sample waits for Sky's yes.

**The sample for Sky:** `briefs/099/fable/voices/index.html`. It has the four voices in three lines each, and 25 moments shown before and after. 7 of the moments go silent; the page also says how often the agents speak and what I found on the way.

## The bar: guide /12 (`tools/style-agent/short-voice.ts`, `short-types.ts`)

- **WORTH and VOICE lead.** The bark rubric is now WORTH, VOICE, NATURAL, VARIANTS, MECHANICS, TRUE, NAMES, SHORT. It used to be USEFUL, TRUE, PLAIN, SHORT, NAMES, VOICE.
  - WORTH: a line exists only if it tells the player something new or lands the character; otherwise it is silence. Sky's cut opening barks stay the first failing example.
  - VOICE is scored on its own. A line another agent could say unchanged fails.
  - NATURAL: a person talking, never a status code. VARIANTS: different takes, not synonyms. MECHANICS: no seconds, counts or rule words, under Sky's rule on numbers in play.
  - TRUE now also holds the one-glossary "out": Popovic is out only once a car of ours has left with him.
- **Each rule scored on its own.** A /12 bark evaluation carries `scores` for all eight rules, and only a 10 on every rule is accepted (`core.ts` takes the acceptance test per version).
  - Before, one overall 10 let true, short, plain status codes through.
  - The reviser sees the rule scores.
- **The voice sheet and the calibration** are in `briefs/099/fable/voices/sheet.json`, staged there until Sky's yes.
  - Each agent: traits, habits, how they speak to the commander, what they care about, and what they never say.
  - 20 failing lines from today's `text.json`, and 24 gold lines, one set per agent.
  - The prompt shows the sheet for the family's agents and the whole calibration.
- **Only rows that speak are written.** The sheet's `speech` tiers decide which agent rows the host speaks. A silent row keeps its reason (`speech12`), so no tokens go on lines nobody hears.
- **Smaller fixes in the bar:**
  - SHORT no longer says "an acknowledgement is often two or three words" (the /4 nudge towards "Holstered.").
  - The Enter landing is briefed as it plays: back out with Popovic, not "looking for him".
  - The loop's landing no longer hands the writer "two minutes".
- **Older runs rebuild exactly.** The why block, the debrief page and the labels read as at /11. The /1–/11 rubric, families, prompts, evaluation schema and its hash are unchanged, and `short.test.ts` is pinned to /11.
- **Safety:**
  - No /12 bark run applies before Sky's yes (`BARKS_12_APPLY`).
  - Sky's own words win over any run (`SKY_PICKS`). The accepted `label/hud` run still says "Driver cutoff", so the next `apply` would have put it back over "Turn driver by". With the guard, an apply with Aster's restore list writes `text.json` byte for byte.

## The cuts

At /12, 130 of the 253 live agent rows (320 of 616 lines) are silent, each with its reason:

| why | rows | lines |
|---|---|---|
| reads: silent since brief 044 (the chip says it) | 31 | 80 |
| acks the screen confirms (walk, run, cancel, holster, cease, stance, board, drive, put down, fire, volley, field, spawn, Go, Withdraw, the plain takedown) | 52 | 122 |
| arrivals: the ring starting shows it (all but the Fixer's scene openers and the struggle) | 12 | 36 |
| landings the player sees (knockout, hide, tie, the plain takedown) | 10 | 30 |
| one agent per moment (two at the end); people first as today | 22 | 46 |
| a landing said twice (rivals-misled, patrol-rerouted) | 2 | 4 |
| the Enforcer's agent-down (it's a fail) | 1 | 2 |

The rest, 123 rows (296 lines) in 13 families, are what step 2 rewrites.

## How often they speak: the proposal for Latch (after Sky's yes)

Speak on a decision, not a click. Today the host speaks every order (`HostActivity.SayOrder`, `HostActivity.cs:261`): the row's variant is picked by `PlayText.PickIndex` (seed, key, use count, never twice running). With no row it says the stock "On it." or "Heading to …" (`:265`). Walks and runs are silent (`HandoverHost.cs:723`), and so is the fight path's list (`HostFight.cs:56`: move, run, cancel, stance, cease, holster).

1. **Tiers** (the sheet's `speech.ack`; data in `text.json` after the yes, so Sky can turn the dial):
   - every time: Call, Enter, Take, a knockout, a kill, End, everyone fire;
   - the first time each agent takes it in a game: Watch, Listen, Mislead, Ask, Pay, Help, Pressure, Loop, Disable, Reroute, draw, Carry, Hide, Tie up;
   - never: the fight path's six, plus Board, Drive, Put down, Fire, volley, field, spawn, Go and Withdraw. Go and Withdraw are followed at once by the end's own lines.
2. **No stock line:** an order with no line is silence (`:265` goes).
3. **One voice per order:** a squad order speaks once, as `HostFight.Send` already does.
4. **Pass the takedown's way:**
   - The ack gets `o.How` as its outcome.
   - In the landing, `takedown.out` plays the knockout row and `takedown.dead` the kill row.
   - Today only the plain rows can play: "Taking them down." and "That's one down." are said even for a kill.
5. **Arrivals:** no Journal line, except two:
   - the Fixer's approach openers, which the scenes play;
   - the struggle, spoken as a bubble on `takedown.starts` when the target "sees it coming". Today it never plays, because `arrive` is an order-kind event and `SayLanded` skips those.
6. **Landings:** they speak for Mislead, Loop, Disable, Reroute, Enter (out with Popovic), Take and a kill. A knockout stays silent, and so do hide and tie: today no hide or tie landing plays, since there is no path for `stash.*` or `restrain.done`.
7. **One landing line per agent:** skip an exchange event's line for the agent whose `done` just spoke (Mislead's rivals-misled, Reroute's patrol-rerouted).
8. **Moments in the street** (`WorldBeat`): at most one agent per moment, people as today; the end (win, fail) keeps two. The win's `most` goes from 3 to 2.

In the scripted route-A win (30 orders), that is about 8 order lines where today there are 13 to 16.

## Found in the host (Latch to confirm)

About 80 of the 253 agent rows (about 190 of 616 lines) never play today:
- the 31 reads;
- 24 silent acks;
- the takedown's knockout and kill rows, acks and landings;
- the 8 hide and tie landings;
- the struggle;
- the Enforcer's agent-down;
- about a dozen event lines queued behind other speakers, for example `chime/fixer`, `last-turn/fixer` and `win/enforcer`.

Another 13 rows reach only the Journal. Several of the lines Aster quoted never reach the screen ("Holstered.", the reads), and the old loop spent runs on all of them.

## A Claude read of the old loop's 10/10 (`read.json`)

Today's `bark/ack-acts` and `bark/done` lines are exactly gpt-6.1-sol's accepted 10/10 runs. I read them line by line against /12, in session, as Claude Opus 5.5; this was not a recorded model call.
- 26 of 126 lines pass: 8 of 57 order lines and 18 of 69 landings.
- Failures by rule: WORTH 66, NATURAL 31, VARIANTS 18, MECHANICS 13, VOICE 6, TRUE 4.
- Of the 69 landing lines, 36 never play at all.

## The model: proposal and cost

The model that wrote the flat lines also passed them. GDD §14 already names Opus 5.5 for Motive, but the loop runs both roles on gpt-6.1-sol.

1. **The judge on Claude Opus 5.5** (recommended).
   - Tooling: a Claude caller for the loop (`tools/rag/claude.ts` `invokeClaude` exists) and evidence that binds Claude transcripts the way `verifyProvider` binds Codex's. About 200–300 lines (Splice or Fable).
   - Cost: about 4 evaluations × 15k tokens per family run, so about 1M Claude tokens for step 2's agent families. $0 marginal on the subscription.
   - `liveGuard` refuses a Claude call from an agent session until Aster sets `SUBORNED_LIVE` with Sky's go-ahead. I did not set it.
2. **A writer A/B:** gpt-6.1-sol against **gpt-6-astra** ("frontier" on Sky's ChatGPT plan, in `~/.codex/models_cache.json`), one family each under /12.
   - The adapter takes `--model gpt-6-astra` today, but for both roles; separate writer and judge models need about 20 lines in `live.ts`.
   - Cost: about 0.3M tokens of plan quota, $0.
   - Not run: it needs a recorded approval and `SUBORNED_LIVE`, and a /12 run would wait on Sky anyway.
3. **The cheapest gate, if (1) waits:** one Claude read per accepted family before `apply` (a `style:read` command, about 150 lines), about 20k tokens a family, about 0.3M in all.

## Step 2 (after Sky's yes; planned, not run)

1. **Aster:** Sky's yes or changes on the voices, the cuts and the frequency.
2. **Fable:**
   - Move the four voices into `text.json` `tone.voices` and the tiers into `less.speech`.
   - Add silent rows (`silent: <reason>`, no lines) to `text.schema.ts` and `text.test.ts`, and have `apply` write a /12 cut as a silent row.
   - Add guide /13, which reads the voices from `tone.voices`; /12 stays for replay. Set `BARKS_12_APPLY`.
3. **Latch:** the host changes above, then `bun run hostlogic`.
4. **The A/B** on `bark/done`; Fable reads both and the writer is picked. The judge moves to Opus 5.5 if the adapter lands, else the read gate.
5. **The agent families:** the 13 families (123 rows, 296 lines).
   - Sky reads `bark/ack-acts` and `bark/done` before the rest run.
   - Then `apply` with Aster's restore list, the export to the player, `bun run hostlogic` and the text tests.
   - Kept unless the new bar clearly beats them: Sky's picks (`SKY_PICKS`), lines tests pin (`keep` and patterns), and the ten restored families.
6. **The sweep** (guide /13, the book's voice):
   - people's barks (11 families), why (6), the debrief (7), the labels (1);
   - a new `card` kind for `text.json` `cards` (about 400 strings, about 8 families).
   - Refusals, actions and hints are a later pass.

**Tokens:**
- The agent families: about 2.8M Codex tokens (the A/B 0.3M, plus 13 families × about 150k × 1.3 for reruns). With the Opus judge, that becomes about 1.5M Codex and 1.0M Claude.
- The sweep: about 5.5M (people's barks 2.1M, why, debrief and labels 2.0M, cards 1.4M).
- In all, about 8.3M tokens of loop calls, about what all 70 short runs have used so far (7.7M). On top of that is Fable's own session work, about 0.5–1M.
- Money: $0 marginal; no paid API.
- Wall time: about 8 minutes a run, so the agent families take about 2.5 h sequential (about 1 h at concurrency 3) and the sweep about 4 h.

The estimate rests on 35 saved bark runs averaging 118k tokens each, and /12 prompts about 40% longer (about 40k characters).

## Files

- New:
  - `tools/style-agent/short-voice.ts`: the /12 rubric, frequency and prompt blocks.
  - `tools/style-agent/voice.test.ts`: 11 tests.
  - `briefs/099/fable/voices/sheet.json`, `read.json`, `index.html` and this file.
- Changed:
  - `tools/style-agent/short-types.ts`: /12, per-rule evaluation, the sheet schema.
  - `short-guide.ts`, `short-barks.ts`, `short-prompts.ts`, `short-loop.ts`, `core.ts` (`accepts`), `short-checks.ts`.
  - `short-cli.ts`: the gate. `short-labels.ts`: `SKY_PICKS`.
  - `short.test.ts`: pinned to /11. `README.md`.
- The sheet's generator: `out/scratch/fable/voices/make.py` (scratch).

## Checks

- `bun test tools/style-agent`: 49/49.
  - All 70 saved short runs rebuild exactly, each with the guide version it ran with, as do the conversation runs.
  - A /12 fixture run is not accepted on a 10 with a 9 on VOICE, and it rebuilds from its archive (which holds the sheet).
- `bun run typecheck`: clean.
- `apply` with Aster's restore list, onto a temp copy: `text.json` byte-identical.

## Open, and for whom

- **Aster:**
  - Show Sky the sample. The model choice and `SUBORNED_LIVE` for a Claude judge are Sky's go-ahead.
  - Bubble words are still 10 in agent GDD §10 but 12 in §14 and the human GDD; the bar uses 12.
- **Latch:** the frequency above, and confirm the host reading behind "never plays".
- **Fable, next:** step 2 once Sky has answered.
