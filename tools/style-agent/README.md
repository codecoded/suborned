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

Motive's rubric for each kind (`short-guide.ts`): USEFUL (tells the player something they can use; the cut opening barks are the failing example), TRUE (to what the engine did), PLAIN (everyday words, never a report's tone), SHORT (the kind's limit), NAMES (people as the player knows them), VOICE (the speaker's, or the book's). Guide /2: Motive also sees its own last findings, and the reviser changes only the lines they name.

```sh
bun tools/style-agent/cli.ts shorts              # the 41 families: items and lines each
bun run style:run bark/end $LIVE                 # one family
bun run style:chain --short $LIVE [--kind bark]  # every family not yet accepted
bun run style:replay runs/style-agent/short/<run>   # the same replay and verify as a conversation
bun tools/style-agent/cli.ts apply [--dry]       # each family's accepted lines into text.json, where they stand
```

Runs are in `runs/style-agent/short/<time>-<family>/`; they share the sources store.

`apply` writes every accepted family each time, so a family put back with `--restore` must be named again on every later `apply`: give the whole list in one call (`--restore a,b,c`). Calling it once per family leaves only the last one restored. The families restored on 2026-11-07 are listed in `briefs/099/fable/short-lines.md`.

## Files

`guide.ts` the style guide · `briefs.ts` the conversation briefs · `core.ts` the loop's engine (limits, calls, acceptance) · `loop.ts` the conversation loop · `prompts.ts` · `checks.ts` code checks · `live.ts` crew cards through the Codex CLI · `evidence.ts` archive and checking · `report.ts` · `cli.ts`.
Short lines: `short-types.ts` · `short-guide.ts` the rubric · `short-barks.ts`, `short-whys.ts`, `short-debrief.ts` the families · `short-briefs.ts` · `short-checks.ts` · `short-prompts.ts` · `short-loop.ts` · `short-evidence.ts` · `short-cli.ts` · `short-apply.ts` · `short.test.ts`.

## Known

- The conversations it wrote for Assignment 6 reveal what they should but read flat (Sky, 2026-10-23); the guide is stronger on rules than on life. Fix the writing before the game takes them.
- Both roles run on `gpt-6.1-sol`; the GDD names Opus 5.5 for Motive.
