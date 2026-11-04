# Assignment 6 · Style Guide Agent

A **Generator → Evaluator → Refiner** loop that writes The Suborned's conversations and enforces the game's style on them. The game's crew agents do the work: **Fable** (dialogue designer) writes and revises, **Motive** (systems designer) scores each draft with a **SCORE** and a **REASON**, and the loop repeats until a draft scores 10/10. Nobody steps in.

**Submission page:** [`index.html`](index.html) has the style guide, the loop, three before-and-after demonstrations, the pipeline sentence, what we found, and the 17 conversations it wrote.

## Why

In the Handover mission you turn Inez, the café waitress, so she calls the door guard (her secret lover) off his post. You learn how to turn her by watching and listening. The game's conversations were too short to teach that. The Listen scene packed the affair, the debt and her temperament into 4–6 lines, and the Fixer's whole Help offer was one line. The agent writes conversations that make the facts plain and set up the next move. When you listen to Inez you overhear her with the guard (the affair) and with a customer (her debt), so you know both levers: Help with the debt, or Pressure over the affair. The decisions are recorded in [brief 054](../../briefs/054-conversations-that-reveal.md).

## Review it (offline, under a minute, no model, no login)

Needs [Bun](https://bun.sh). From the repository root:

```sh
bun install --frozen-lockfile
bun run style:replay tone        # watch the loop: draft → SCORE + REASON → revision → SCORE …
bun run style:replay lore
bun run style:replay length
bun run style:report             # re-check all 24 saved runs, rebuild index.html
bun test tools/style-agent       # 13 tests of the loop, with labelled test data
```

`style:replay` takes a demo name, a brief id (for example `inez/listen/loyal`, listed by `bun run style:briefs`) or a run folder. It checks the run before playing it. Every saved run is rebuilt from its archived sources and recorded model answers: the guide, the brief, every prompt, code check, score and stop decision. Any edit to a run makes the check fail.

## Deliverables

| Requirement | Where |
| --- | --- |
| **Style guide** from the game, with at least three constraint types | Four rules: **REVEAL** (what the conversation teaches and sets up), **VOICE** (character and tone), **WORLD** (lore and vocabulary), **FORM** (length and shape). Built only from the approved GDD v0.24, the mission's voice sheet and text contract, and brief 054. Each source is fingerprinted, and extraction fails if a GDD sentence moves. Print it with `bun run style:guide`. |
| **Evaluator and refiner loop** | Fable writes. Motive returns a score from 1 to 10, a reason and findings (rule, exact quote, why, fix), and code checks run alongside. Fable revises automatically from all of it, and every revision is scored again. A draft is accepted only at 10/10 with no findings and clean code checks. Limits: 3 revisions, 8 calls, 200,000 tokens, 15 minutes. A run that hits a limit is recorded as incomplete. |
| **Three before/after demonstrations** | Tone (rom-com gush): 4 → 8 → 8 → 10. Lore and vocabulary (hardboiled slang and invented names, which the world bans): 4 → 9 → 8 → 10. Length and format (the draft came back far too short): 5 → 10. Each is a real conversation from the storyline, with a deliberate wrong push on the first draft only. The evaluator and refiner never see the push. |
| **Pipeline connection** | The sentence below. |

## Pipeline connection

This Style Guide Agent runs straight after Fable generates a mission's conversations: Motive scores each one against the GDD's reveal, voice, world and form rules, and Fable revises it until it scores 10/10, before Keel's text checks and Latch's build.

## Results

- All 17 conversations in Inez's storyline were accepted at 10/10: Watch, Listen and the call for each of her four temperaments, plus Help and Pressure for each.
- One conversation passed on its first draft. The rest needed 1–3 revisions.
- 24 live runs used about 1.9M tokens. 4 of them stopped at their limits; each of those was rerun and accepted. The incomplete runs are kept and listed on the page.
- What the runs showed:
  - The usual fault is characters explaining themselves for the player's benefit, not lore errors.
  - The hardest part is revealing the affair and the debt to a close listener without announcing them in a busy café.
  - The GDD says what each temperament responds to, but not how it sounds.
- The conversations are proposals in [`proposals/inez-conversations.json`](proposals/inez-conversations.json). The game's text, its scene limits and the build are unchanged until they are reviewed.

## Run it live (optional)

This needs the [Codex CLI](https://developers.openai.com/codex) signed in with a ChatGPT plan (`codex login`). It runs on the subscription, not a paid API. One run takes about 50,000–100,000 tokens and 2–5 minutes.

```sh
export SUBORNED_LIVE=1
LIVE="--model gpt-6.1-sol --approved-by <your-name> --max-calls 8 --max-tokens 200000 --max-revisions 3"
bun run style:demo tone $LIVE                 # one demonstration (also: lore, length)
bun run style:run inez/listen/loyal $LIVE     # one conversation from the storyline
bun run style:chain $LIVE                     # every storyline conversation not yet accepted
bun run style:report                          # then rebuild the page
```

Each run is saved in `runs/style-agent/<time>-<brief>/` as `run.json.zst` (every prompt, transcript, score and revision, as zstd-compressed JSON) with an `index.html` page. A missing Codex CLI stops the run before anything is written.

## Files

| Path | What |
| --- | --- |
| `tools/style-agent/guide.ts` | The style guide, extracted from its sources |
| `tools/style-agent/briefs.ts` | The 17 conversation briefs: act, length, what to reveal and withhold, what it sets up |
| `tools/style-agent/loop.ts` | The loop: roles, limits, acceptance |
| `tools/style-agent/prompts.ts` | The generator, evaluator and refiner prompts |
| `tools/style-agent/checks.ts` | Code checks: length, bubble size, cast, monologues, the word filter |
| `tools/style-agent/live.ts` | Model calls: each crew card through the Codex CLI, no tools |
| `tools/style-agent/evidence.ts` | Source archive and run checking |
| `tools/style-agent/report.ts` | The run pages and the submission page |
| `tools/style-agent/cli.ts` | The commands above |
| `runs/style-agent/` | The 24 saved runs (0.9 MB): each a compressed `run.json.zst` and an `index.html`; their sources are archived once in `sources/` |
| `.codex/agents/dialogue-designer.toml`, `systems-designer.toml` | Fable's and Motive's crew cards, generated from GDD §14 |

## Limits

- A 10/10 means Motive found nothing to fix, not that the writing is perfect. The conversations are reviewed before they go into the game.
- Both roles ran on `gpt-6.1-sol`. The GDD names that model for Fable, but Opus 5.5 for Motive; one provider kept this simple.
