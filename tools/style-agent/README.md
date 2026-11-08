# Style agent: conversations that reveal

A Generator → Evaluator → Refiner loop for the game's conversations (brief 054). **Fable** (dialogue designer) writes a conversation from its brief; **Motive** (systems designer) returns a SCORE (1–10), a REASON and findings; code checks run alongside; Fable revises from all of it. The loop stops at 10/10 with no findings and clean checks, or at its limits (3 revisions, 8 calls, 200,000 tokens, 15 minutes). Built for Assignment 6; the submission and its 24 runs are on branch `assignment-06`.

## Commands

```sh
bun run style:guide        # REVEAL, VOICE, WORLD, FORM from the current approved GDD, voice sheet and brief 054
bun run style:briefs       # the 17 Inez conversations: act, length (act seconds ÷ 3 s per bubble), what to reveal and withhold
bun run style:replay RUN   # play a saved run step by step (a brief id, a demo name or a run folder); no model
bun run style:verify RUN   # rebuild a saved run from its archived sources and check it; no model
bun run style:report       # check every saved run and write out/style-agent/index.html
bun test tools/style-agent # the loop with labelled test data; no model
```

Live (Codex CLI signed in with ChatGPT; about 2 minutes and 50,000–100,000 tokens a run):

```sh
export SUBORNED_LIVE=1
LIVE="--model gpt-6.1-sol --approved-by <name> --max-calls 8 --max-tokens 200000 --max-revisions 3"
bun run style:run inez/listen/loyal $LIVE   # one conversation
bun run style:chain $LIVE                   # every conversation not yet accepted
bun run style:demo tone $LIVE               # an evaluator stress test (also lore, length)
```

Each run is saved in `runs/style-agent/<time>-<brief>/` as `run.json.zst` (every prompt, transcript, score and revision) and an `index.html`; sources are archived once in `runs/style-agent/sources/`.

## Short lines: barks, the why block, the debrief page

Since 2026-11-07 (Sky, asked whether the loop covers more than the Inez conversations: "sure") the same loop scores the game's short lines, so nothing ships unscored. A **family** is one run: all of one event's barks (or a few small events), a group of why causes, or a part of the debrief page.

- **bark**: one row of `text.json` `dialogue` per item: the event, the speaker and their voice, and what the line must tell the player (the event's trigger, the verb's own line, what a Watch or Listen taught, the GDD's temperaments). A bark the loop finds says nothing useful may be cut, unless a test, the host or the generator needs it (the item says why).
- **why**: a cause of the `why` block with its fills: title, short, line, plate; held to `why.schema.ts` at the widest fills.
- **debrief**: the debrief page's words (brief 099, concept A), Glyph's stand-ins by key with the data each one shows; written to `debrief.page`.
- **label** (guide /10, brief 101): the words the new HUD draws in play at a glance, one family, `label/hud`: the first shot's row (`less.street.loud`, also written to the Alt outcome `less.alt.outcome.loud`), the next key event's word in the top right (`briefing.timeline[].short`), and the news lozenge's cards in the bottom right (`book.feed.cards.*` titles and lines, `book.feed.of`). Each item's today is what the HUD shows now; the rubric adds Sky's rule on numbers in play (no act's time, no chance; a label types no digit of its own, and a count, the health left or a key event's time comes in through a fill). From /11 the lines a host test reads are held by pattern (the hurt cards' live health, the vale-out card's police time): the /10 run dropped them, and only `bun run hostlogic` caught it. `apply` replaces each string where it stands.

Motive's rubric for each kind (`short-guide.ts`): USEFUL (tells the player something they can use; the cut opening barks are the failing example), TRUE (to what the engine did), PLAIN (everyday words, never a report's tone), SHORT (the kind's limit), NAMES (people as the player knows them), VOICE (the speaker's, or the book's). Guide /2: Motive also sees its own last findings, and the reviser changes only the lines they name. Guide /9 (`short-types.ts` lists every version): a police title names who or what is coming (Police coming, More police, Robots coming, Lockdown) and the cause goes in its short; the meter's head keeps its level; the why words brief 100's HUD tests read are held word for word or by pattern (`HostLogic.Why100.cs`); a debrief item's today is what the page shows now, kept where it holds; and `debrief/folds` writes the keys Latch's page reads that had no item.

**Guide /12** (brief 099 voices; Sky 2026-11-08, playing: "a lot of poor wording stil throughout. Especially agents talk to player"). The /4–/11 bark rubric rewarded plain, short and true, so status codes passed at 10/10 ("Holstered.", "Here. Watching."). From /12 a bark family is scored by WORTH (the line tells the player something new or lands the character, else silence), VOICE (the speaker and nobody else), NATURAL (a person talking), VARIANTS (different takes), MECHANICS (no seconds, counts or rule words: Sky's rule on numbers in play), TRUE, NAMES and SHORT. Motive scores every rule on its own (`scores`), and only a 10 on every rule is accepted. The prompt carries Fable's voice sheet and the calibration (today's flat lines failing, a gold set passing) from `briefs/099/fable/voices/sheet.json`. Only the agent rows the host speaks under Fable's frequency (the sheet's `speech`: on a decision, not a click) are written; the rest are silent with their reason (`short-voice.ts`), and the read rows have been silent since brief 044. The why block, the page and the labels read as at /11, and the labels keep Sky's own words (`SKY_PICKS`: "Turn driver by", which `apply` also holds against any run). **No /12 bark run applies until Sky says yes to the sample** (`BARKS_12_APPLY`); the sheet goes into `tone.voices` and the tiers to the host in step 2.

```sh
bun tools/style-agent/cli.ts shorts              # the families: items and lines each
bun run style:run bark/end $LIVE                 # one family
bun run style:chain --short $LIVE [--kind bark]  # every family not yet accepted
bun run style:replay runs/style-agent/short/<run>   # the same replay and verify as a conversation
bun tools/style-agent/cli.ts apply [--dry]       # each family's accepted lines into text.json, where they stand
```

Runs are in `runs/style-agent/short/<time>-<family>/`; they share the sources store.

`apply` writes every accepted family each time, so a family put back with `--restore` must be named again on every later `apply`: give the whole list in one call (`--restore a,b,c`). Calling it once per family leaves only the last one restored. The families restored on 2026-11-07 are listed in `briefs/099/fable/short-lines.md`. After `apply`, export the text to the player (`bun tools/sim/unity.ts --tree`) and run `bun run hostlogic`: the HUD's and the debrief page's tests read the words there, and `contracts/missions/handover/text.test.ts` holds rules the loop does not.

## Files

`guide.ts` the style guide · `briefs.ts` the conversation briefs · `core.ts` the loop's engine (limits, calls, acceptance) · `loop.ts` the conversation loop · `prompts.ts` · `checks.ts` code checks · `live.ts` crew cards through the Codex CLI · `evidence.ts` archive and checking · `report.ts` · `cli.ts`.
Short lines: `short-types.ts` · `short-guide.ts` the rubric · `short-voice.ts` guide /12's rubric, frequency and calibration · `short-barks.ts`, `short-whys.ts`, `short-debrief.ts`, `short-labels.ts` the families · `short-briefs.ts` · `short-checks.ts` · `short-prompts.ts` · `short-loop.ts` · `short-evidence.ts` · `short-cli.ts` · `short-apply.ts` · `short.test.ts` (/1–/11) · `voice.test.ts` (/12).

## Known

- The conversations it wrote for Assignment 6 reveal what they should but read flat (Sky, 2026-10-23); the guide is stronger on rules than on life. Fix the writing before the game takes them.
- Both roles run on `gpt-6.1-sol`; the GDD names Opus 5.5 for Motive. The model that wrote the flat lines also passed them; brief 099 voices proposes Motive on Opus 5.5 (a Claude adapter for this loop) and a writer A/B with `gpt-6-astra`.
