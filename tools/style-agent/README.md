# Style agent: conversations that reveal

A Generator → Evaluator → Refiner loop for the game's conversations (brief 054). **Fable** (dialogue designer) writes a conversation from its brief; **Motive** (systems designer) returns a SCORE (1–10), a REASON and findings; code checks run alongside; Fable revises from all of it. The loop stops at 10/10 with no findings and clean checks, or at its limits (3 revisions, 8 calls, 200,000 tokens, 15 minutes). Built for Assignment 6; the submission and its 24 runs are on branch `assignment-06`.

## Commands

```sh
bun run style:guide        # the style guide: REVEAL, VOICE, WORLD, FORM, built from GDD v0.24, the voice sheet and brief 054
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

## Files

`guide.ts` the style guide · `briefs.ts` the conversation briefs · `loop.ts` roles, limits, acceptance · `prompts.ts` · `checks.ts` code checks · `live.ts` crew cards through the Codex CLI · `evidence.ts` archive and checking · `report.ts` · `cli.ts`.

## Known

- The conversations it wrote for Assignment 6 reveal what they should but read flat (Sky, 2026-10-23); the guide is stronger on rules than on life. Fix the writing before the game takes them.
- Both roles run on `gpt-6.1-sol`; the GDD names Opus 5.5 for Motive.
