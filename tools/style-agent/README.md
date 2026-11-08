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

**Guide /13** (brief 102; Sky 2026-11-08, on the /12 sample: "Maybe even drop them? They will feel repetitive", and on "He bought it. He'll be back, and he knows my face.": "that's exactly the kind of scripting that bugs me. 'I will remember you.' lines etc - so unnatural"). The agents are quiet in play: their acknowledgements, arrivals, landings and reads leave the loop, and every agent row outside a scene with people carries `silent` in `text.json` with its reason (`text.schema.ts` `SCENE_ROWS` lists the 14 that speak: the Fixer's opener, last word and telephone call, the Enforcer's words to Popovic, the Fixer's answer in the car). The bark families are the world's people plus three of the cell's scene lines (`bark/fixer-scenes`, `bark/take`, and `bark/reply-guard` out of the old guard-and-take family). NATURAL and TRUE hold the no-mechanics rule (no line tells the player a rule or a consequence, whoever says it), with brief 102's failing examples, Sky's two kept lines and a calibration from `briefs/102/fable/natural.json`; a reply's brief splits what the person says from the facts the game shows (the /4–/12 briefs put "they report any approach" on the side to say); a code check (`FORETELLS`) holds the family Sky named. The rubric is in `short-natural.ts`; the why block, the page and the labels read as at /12. The conversation guide is at **/4**: its NATURAL holds the same rule, and the refused Pressure no longer asks Inez to say she'll report it. No /13 bark run applies before Sky's yes to `briefs/102/fable/index.html`.

**Guide /14** (brief 102's sweep; Sky 2026-11-08 said yes to the /13 sample, "yes - rewrites are much better", and on the four lines it kept on purpose: "not into those e.g. 'Citizen. Walking is encouraged. Running is noted.' - that noted is AI sounding"). NATURAL also fails AI-sounding words whoever says them (noted, understood, proceeding, acknowledged, affirmative, rest assured and their kind), with a code check; the words, Sky's four rejected lines and today's lines with such a word are `briefs/102/fable/ai-words.json`, and the /13 pass Sky turned down ("I remember them, officer.") leaves the bar. The Mislead is a scene with a person: `bark/mislead` holds the Scout's tip to the rival lookout and his answer. Rows marked `sky` in `text.json` (Sky's approved rewrites) are not items; each family's context gives them, and `apply` never replaces them, as "Turn driver by". One new label family, `label/rivals-follow`: the news card for the rivals following our car out (`book.feed.cards.rivals-follow`). The rest reads as at /13; /12 bark runs never apply (Sky quieted the agents), /13 and later do.

**Guides /15 and /16** (brief 102's sweep, two protocol fixes; the rubric and the families read as at /14). /15: a rule scored below 10 needs a finding that names the line and what would lift it (Opus scored a family 9 on every rule with no finding, four rounds running, so the writer had nothing to revise). /16: a cut item's field with no lines counts as no lines (the writer cut a row as `fields: [{ lines: [] }]` four rounds running, and the old check blocked a 10 each time).

**Continuing a run** (`chain --short --continue`, brief 102). Opus judges strictly: three revisions often end on an 8 or a 9, and starting again from today's lines repeats the climb. `--continue` picks up each family's latest live run that ran out of revisions on the same rubric (or one a later protocol shares: /14 → /15 → /16), family and approval, and revises its last draft from its last findings, within the same limits. The run records the round and the run it came from (`continues`); `style:verify` checks the earlier run ended on exactly that round.

**The Claude judge** (brief 102; Sky 2026-11-08, on Opus 5.5 judging the sweep: "we can try - sure"). `--judge-model claude-opus-5-5` runs Motive's evaluations on Claude through the subscription's `claude` CLI while Fable writes on Codex (`--model`). Each evaluation is one tool-less call (`live-claude.ts`): Motive's Claude card (`.claude/agents/systems-designer.md`) as the system prompt, structured output the only tool, effort medium, no CLAUDE.md, settings, hooks, MCP servers or side traffic, in an empty folder. The run records the CLI's stream-json transcript and the launch (`audit`: argv, schema, prompt hash), the approval names both models (`authorization.judge`), and the Claude card is archived with the sources, so `style:replay` and `style:verify` rebind every judged call to its transcript, card, schema and model without a model. Tokens count every model the CLI used. Live as for Codex: `SUBORNED_LIVE=1` and `--approved-by`.

**Guide /17** (brief 102's sweep: `bark/witnesses-crowd` and `bark/summons` didn't reach 10 in six passes each, so Fable wrote their 13 failing lines by hand, `briefs/102/fable/hand-barks.json`). Rows marked `hand` in `text.json` (Fable's hand edits) are kept like Sky's: not items, given in their family's context, and `apply` never replaces them. The summons' no-visit rows are briefed as the engine plays them: only a call on the turned guard gets that code (his tie, Inez, has no post to leave), so he answers the Fixer, and the host's card shows Inez's line after his. `judge FAMILY --hand FILE` scores a hand-written family once: today's lines with the file's edits in place, Motive on Claude, no writer call and no revision (`hand` in the run, `provider: hand` in its approval); it replays and verifies like any run. The rubric reads as at /16.

```sh
bun tools/style-agent/cli.ts shorts              # the families: items and lines each
bun run style:run bark/end $LIVE                 # one family (add --judge-model claude-opus-5-5 for Motive on Claude)
bun run style:chain --short $LIVE [--kind bark]  # every family not yet accepted
bun run style:chain --short $LIVE --continue --only bark/a,bark/b   # pick up each family's last unfinished run from its last round
bun run style:replay runs/style-agent/short/<run>   # the same replay and verify as a conversation
bun tools/style-agent/cli.ts apply [--dry]       # each family's accepted lines into text.json, where they stand
SUBORNED_LIVE=1 bun tools/style-agent/cli.ts judge bark/summons --hand briefs/102/fable/hand-barks.json --approved-by NAME --judge-model claude-opus-5-5   # score hand edits once
```

Runs are in `runs/style-agent/short/<time>-<family>/`; they share the sources store.

`apply` writes every accepted family each time, so a family put back with `--restore` must be named again on every later `apply`: give the whole list in one call (`--restore a,b,c`). Calling it once per family leaves only the last one restored. The families restored on 2026-11-07 are listed in `briefs/099/fable/short-lines.md`. After `apply`, export the text to the player (`bun tools/sim/unity.ts --tree`) and run `bun run hostlogic`: the HUD's and the debrief page's tests read the words there, and `contracts/missions/handover/text.test.ts` holds rules the loop does not.

## Files

`guide.ts` the style guide · `briefs.ts` the conversation briefs · `core.ts` the loop's engine (limits, calls, acceptance) · `loop.ts` the conversation loop · `prompts.ts` · `checks.ts` code checks · `live.ts` crew cards through the Codex CLI (and the routed judge) · `live-claude.ts` Motive's evaluations through the Claude CLI, and their check · `evidence.ts` archive and checking · `report.ts` · `cli.ts`.
Short lines: `short-types.ts` · `short-guide.ts` the rubric · `short-voice.ts` guide /12's rubric, frequency and calibration · `short-natural.ts` guide /13's rubric, families and calibration (and the code check the conversations share at /4) · `short-natural14.ts` guide /14's AI-sounding words, the Mislead and its families · `short-barks.ts`, `short-whys.ts`, `short-debrief.ts`, `short-labels.ts` the families · `short-briefs.ts` · `short-checks.ts` · `short-prompts.ts` · `short-loop.ts` · `short-evidence.ts` · `short-cli.ts` (with `judge`, the hand edits' scoring) · `short-apply.ts` · `short.test.ts` (/1–/11) · `voice.test.ts` (/12) · `natural.test.ts` (/13, and the conversations' /4) · `natural14.test.ts` (/14) · `judge.test.ts` (the Claude judge) · `hand.test.ts` (/17).

## Known

- The conversations it wrote for Assignment 6 reveal what they should but read flat (Sky, 2026-10-23); the guide is stronger on rules than on life. Fix the writing before the game takes them.
- Until brief 102 both roles ran on `gpt-6.1-sol`, so the model that wrote the flat lines also passed them. From brief 102's sweep Motive judges on Opus 5.5 (`--judge-model`), as GDD §14 names; a writer A/B with `gpt-6-astra` is still open.
