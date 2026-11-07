# Short lines through the style agent: barks, the why block, the debrief page

Fable, 2026-11-07, on `work/098-living-playbook`. Uncommitted; Aster commits after review. Unity wasn't run and the engine wasn't touched.

Sky asked "do we have a GER script process?". Told it covered only the 17 Inez conversations, Sky said "sure" to extending it, so nothing ships unscored. This file covers that extension, the runs, and what changed in `text.json`. It also covers brief 099's step 2: Fable turns Glyph's stand-ins into words.

**In short:**
- 66 live runs, 414 calls and 7.5M tokens, on `gpt-6.1-sol` through the Codex CLI on Sky's ChatGPT plan. No paid API.
- 29 families accepted at 10/10 and written to `text.json`:
  - 18 bark families: 268 of the 505 rows rewritten, 502 of 1,154 lines;
  - all six debrief families: `debrief.page`, new, 159 strings;
  - five of the six why families: 116 of 228 strings.
- 10 families never reached 10, and 2 accepted families were put back to today's lines. Motive's reasons are below.
- No bark was cut: every row that could be cut was judged to carry a useful fact once rewritten.
- The loop keeps lines true and plain, but in places it flattens voice. See "Read before committing".

## The new brief types (`tools/style-agent/short-*.ts`)

The loop is the same one the conversations use, and it now lives in `core.ts`, shared by both:
1. Fable writes.
2. Motive scores 1 to 10, with a REASON and findings (rule, item, exact quote, why, fix). Code checks run alongside.
3. Fable revises.

It stops at 10/10 with no findings and clean code checks, or at its limits. A **family** is one run: a few related events' barks, a group of why causes, or a part of the debrief page.

| type | an item is | what Fable is given | written to |
|---|---|---|---|
| **bark** | one row of `dialogue` (event / verb / speaker / outcome / temperament / about) | the event's trigger; the verb's own line (`actions.*.does`); what a Watch or Listen taught (`targets`); the GDD §04 temperaments; the fail's causes (`debrief.cause`); the speaker's voice; today's lines; whether the row may be cut | the row's `lines`, in place |
| **why** | a cause, or one of its variants, a way a report travels, a meter label, a glossary word | the engine's facts from brief 100 (ledger cause, log, the Alert it sets) and the fills it carries, each with its widest value | `why`, in place |
| **debrief** | one key of concept A | Glyph's stand-in (`briefs/099/glyph/words.md`); the data it shows (Glyph's notes); the fail camera's caption and tip; the end card's cause; the rulebook's Trail values | `debrief.page` (new) |

**Motive's rubric** (`short-guide.ts`). There are six rules for each type, each drawn from fingerprinted sources: the agent GDD §01, §10 and §14 (Fable, Motive), the human GDD's fingerprint, `text.json` (voices, names, Sky's cut of the opening barks in `silentLogs.start`, Sky's plain-English note in `debrief.rule`), `text.schema.ts`, `why.schema.ts` and brief 099. The six rules:
- **USEFUL**: the line tells the player something they can use, with no filler. The cut opening barks are quoted as the failing example. A bark that says nothing useful may be cut unless something needs it.
- **TRUE**: true to what the engine did, and true for every value its fills can take.
- **PLAIN**: everyday words, never a report's tone, no engine words.
- **SHORT**: the type's limit.
  - A bubble: 12 words and 80 characters (the human GDD: "twelve words or fewer").
  - The why forms: why.schema.ts's LIMITS.
  - The page: each slot's size.
- **NAMES**: people as the player knows them. Inez and Popovic by name; others by role. The cell by role in play and by their book name (`{op}`) on the page. Nobody outside the cell says the cell's names.
- **VOICE**: the speaker's voice from the voice sheet, or the book's voice (no exclamation marks).

**Code checks** (`short-checks.ts`). These run on every draft:
- every item once;
- the right number of variants;
- words and characters at the widest fills;
- each form's shape;
- only the item's own fills;
- the mission's word filter;
- the cell's names;
- capitals mid-sentence (the text test's list);
- no digits in a person's mouth;
- no engine words on the page;
- no repeated line;
- lines a test pins, kept word for word;
- patterns a test or a proof needs;
- a cut only where the item allows it.

For why, `why.schema.ts`'s own check also runs on the block with the family's lines merged in.

**Labelled test data** (`short.test.ts`, 17 tests, no model). They cover:
- the rubric's sources;
- that every live bark row, why cause and page key is in a family;
- a failing example for each code check;
- the loop accepting a revised draft, and stopping on a failed check, an invented quote or a limit;
- the protocol versions;
- every saved short run rebuilding from its archive;
- the apply step on a temporary copy of `text.json`: one row changes and nothing else moves, and restoring the family gives the original file back byte for byte.

## Protocol versions (`suborned.style-short-guide/n`)

A saved run replays with the version it ran with, as the conversations do with /2 and /3.
- **/1**: the first live run (`bark/end`). Its last two rounds caught Motive marking down a fix it had asked for ("We're ending this attempt.").
- **/2**: Motive sees its own last findings, so it holds the writer to them. The reviser changes only the lines those findings name.
- **/3**: the barks also carry what `text.test.ts` pins on their words:
  - rivals-coming names Orbit Walk and never sees the car;
  - the Scout's rivals-for-car line says "outside" or "unguarded";
  - the three rival moves never name the Alert;
  - run-seen never says "reported";
  - loop-ends never says live, sees or entrance;
  - the Scout's nobody-sent line never says anyone is coming;
  - driver-tail keeps "tailed us all week";
  - and the rows those tests need, which can't be cut.
- **/4**: the rubric every line now in `text.json` was written to; /5 to /7 only add checks and correct facts. The /2 chain's acknowledgements read like tooltips in the first person: "I'll make the camera repeat pictures for two minutes.", and three agents saying "I'll shoot to stop them." The cause was the brief: each item quoted the verb's price line as the thing to say. In /4:
  - a bark item says what to **say**, and lists **facts** that keep it true but aren't read out (an order's price is on its card already, GDD §02);
  - VOICE is the point of a bark: a line another speaker could say unchanged fails;
  - today's lines that already work in voice are kept;
  - acknowledgements are scored by verb, all four agents side by side.
- **/5**:
  - a bark is checked against the other rows of the table it ran on. Two families running in parallel had shipped "Going in." as both the Enforcer's ack and his arrive line;
  - each why cause gets its own meaning for each fill. In /4, `{who}` and `{about}` meant the same everywhere, so Motive rightly objected that a camera could "walk to the call box";
  - a fill counts as one word;
  - the Fixer's turn lines are said to the person's face (brief 085's pinned "You said no. You'll remember me.");
  - temperaments come in plain words beside the GDD row;
  - the page's names rule lets a role stand where no `{op}` is given.
- **/6**:
  - the witnesses' family in two (its scoring ran past the 3-minute call limit twice);
  - the page's word count also counts a fill as one word;
  - Popovic's Listen reads say how he takes being taken (the cell has no approach on him);
  - an unwatched check-in never says "check-in".
- **/7**: a why text keeps the fills today's has for who saw it, whom, the agent, the report point and the seconds. A counting plate keeps " · {s} s". The /4 and /5 families had dropped them: "the desk saw payment on tape", plates with no countdown.

## The runs (`runs/style-agent/short/`, each replays and verifies with `bun run style:verify`)

| family | runs (guide: scores) | in text.json |
|---|---|---|
| `bark/ack-acts` | /4 7→10 | written |
| `bark/ack-bodies` | /4 7→9→9→10 | written |
| `bark/ack-car` | /4 7→9→8→9 | today's lines (not accepted) |
| `bark/ack-enforcer` | /2 8→10 | superseded family |
| `bark/ack-feet` | /4 8→10 | written |
| `bark/ack-fire` | /4 7→9→9→10 | written |
| `bark/ack-fixer` | /2 8→8→10 | superseded family |
| `bark/ack-guns` | /4 7→9→9→10 | written |
| `bark/ack-scout` | /2 8→9→10 | superseded family |
| `bark/ack-tech` | /2 8→9→10 | superseded family |
| `bark/aftermath` | /4 6→9→8→9 | today's lines (not accepted) |
| `bark/alarm` | /4 7→9→9→9 | today's lines (not accepted) |
| `bark/arrive` | /2 9→10; /4 8→9→9→10; /5 8→10→8→10 | written |
| `bark/clock` | /4 8→9→9→10 | written |
| `bark/done` | /4 7→9→9→9; /5 7→9→10 | written |
| `bark/end` | /1 7→7→9→9; /4 7→8→10 | written |
| `bark/fail` | /4 7→8→8→8 | today's lines (not accepted) |
| `bark/final-fight` | /4 7→8→9→10 | written |
| `bark/guard-call` | /4 7→9→10 | today's lines (restored) |
| `bark/gunmen` | /4 7→9→8→9 | today's lines (not accepted) |
| `bark/machines` | /4 7→9→9→9 | today's lines (not accepted) |
| `bark/noticed` | /4 7→9→9→10 | written |
| `bark/read-machines` | /4 6→8→9→9 | today's lines (not accepted) |
| `bark/read-people` | /4 7→9→8→8; /5 7→10; /6 7→7→8→8 | today's lines (restored) |
| `bark/reply-driver-ask-pay` | /4 6→9→8→9 | today's lines (not accepted) |
| `bark/reply-driver-help-pressure` | /4 8→8→9→10 | written |
| `bark/reply-guard-popovic` | /4 8→10 | written |
| `bark/reply-inez-ask-pay` | /4 7→9→9→8 | today's lines (not accepted) |
| `bark/reply-inez-help-pressure` | /4 7→10 | written |
| `bark/rivals` | /4 8→9→10 | written |
| `bark/summons` | /4 7→9→10 | written |
| `bark/witnesses` | /4 error; /5 error | superseded family |
| `bark/witnesses-cast` | /6 7→9→10 | written |
| `bark/witnesses-crowd` | /6 7→10 | written |
| `debrief/city` | /4 8→8→8→10 | written |
| `debrief/head` | /4 10 | written |
| `debrief/next` | /4 7→9→8→8; /5 7→9→9→9; /6 8→10 | written |
| `debrief/numbers` | /4 8→9→9→8; /5 9→9→9→9; /6 10 | written |
| `debrief/strip` | /4 8→10 | written |
| `debrief/trail` | /4 8→9→9→10 | written |
| `why/gunfire` | /4 7→9→8→9; /5 8→10 | written |
| `why/on-the-way` | /4 6→8→7→5; /5 8→10; /7 9→10 | written |
| `why/police` | /4 7→8→8→8; /5 8→10; /6 8→10 | written |
| `why/reported` | /4 7→8→10; /7 7→8→10 | written |
| `why/witnessed` | /4 8→7→7→7; /5 8→10; /6 8→10 | written |
| `why/words` | /4 7→9→8→9; /5 7→9→8→9 | today's lines (not accepted) |

Runs that stopped are kept on record:
- `/1 bark/end`;
- the `/2` acknowledgement families and `/2 bark/arrive`, superseded by `/4`;
- `/4 bark/witnesses`, which twice ran past the 3-minute call limit while Motive was scoring it, and was split in `/6`.

Three runs I stopped mid-call, when I halted the `/2` chain, have no final checkpoint, so they can't verify. I moved them to `out/scratch/fable/style-short/aborted/`.

**Not accepted.** Today's lines stay for these families. Motive's last findings:
- `bark/ack-car` (9): the Tech's two boarding variants are near-duplicates; "I'll get myself into the car." is laboured.
- `bark/read-machines` (9): "Far end leaves the service door clear." is clipped; "It turns both ends." and "Timing needs longer." are awkward.
- `bark/reply-inez-ask-pay` (8):
  - the confirm variants repeat "I'll call the guard over";
  - "Favour bought, love.", "Call when needed." and "I'll help when called." read written, not spoken.
- `bark/reply-driver-ask-pay` (9): two report lines ("I'm telling the Office about this.") could be anyone's.
- `bark/machines` (9): the patrol's "No one to fight within reach." reads out the engine's condition.
- `bark/alarm` (9): one Scout variant repeats the other's opening.
- `bark/aftermath` (9):
  - the driver's waking lines are generic;
  - Inez's endearment is laid onto a line of distress;
  - the guard's missed-person lines sound like a form.
- `bark/gunmen` (9): "Stay here. That's the job." claims a task the facts don't give.
- `bark/fail` (8): the Scout's fail lines mirror the Fixer's.
- `why/words` (9): "in the square's east" and "west", and "at the driver's meetup".

**Accepted, then put back to today's lines.** Both were my brief's errors, and an accepted family is undone in one command (below).
- `bark/read-people`:
  - its `/5` acceptance had Popovic needing "help more than once", but the cell has no approach on Popovic. My `/5` plain-words temperament assumed it did.
  - The `/6` rerun, with the fix, stopped at 8: one line was 81 characters, plus voice notes.
- `bark/guard-call`: its accepted lines said the door is open "for about a minute". `conversations.test.ts` bans "a minute" and coffee near the call, and my facts carried the GDD's 60 s. Today's lines ("Stand with me a while.") also read better.

## What changed in text.json

All the edits are in place, keeping the file's own layout: a bark row, a why cause and a why word are each one line of the file, and only those lines change.
- **Barks:** 268 rows rewritten, every key kept, nothing cut, no event or speaker removed.
  - Most changed: the acknowledgements (70), replies (46), turns and done (19), witnesses (18), arrive (14), refused calls (11).
  - Kept as they were (unaccepted or restored): the car acknowledgements (board, go, drive, withdraw, end, spawn), the Ask and Pay replies, the machines, the alarm, the aftermath, the gunmen, the fails, the Watch and Listen reads, the guard and Inez's call.
  - Test-pinned lines are kept: "You said no. You'll remember me.", "Back to my post.", Orbit Walk, "tailed us all week", and the others.
- **why:** 116 strings changed, across the witnessed, reported, gunfire, police and on-the-way causes. Some examples:
  - a knockout is "Found knocked out", no longer "Body found";
  - a camera's tape names the camera and whom: "{who}: desk saw payment to {about}";
  - every report plate keeps its countdown ("Radioing a report · {s} s");
  - a check-in the cell hasn't watched never says "check-in".

  Two fills are gone, deliberately: the loiter line's `{where}` (the Journal shows a line's place beside it), and the meter head's `{alert}` (now "Why Alert rose"; the lamps show the level). `why.by`, `places` and `onWay.to` are unchanged (`why/words` didn't converge).
- **debrief.page** (new): concept A's words, under `debrief`, as asked, because Glyph's `why.alert.*` stand-ins clash with the why block. It has 10 sections: head, way, how, numbers, reasons, trail, city, beats, next and ways.
  - Its rule is the last part of `debrief.rule`. The text test walks every other debrief string as an 80-character line.
  - Keys added beyond Glyph's stand-ins, needed for the page to be true on every run:
    - `how.knocked`, `cut`, `shot-out`, `pressed` and `asked`;
    - `reasons.cash.both`, `reasons.trail.none` and `reasons.trail.list`;
    - a Trail row for each of the engine's 19 Trail causes (Glyph had 8);
    - `beats.report`, since `report.landed` is any report, not only a body found;
    - a Next time line and a time reason for each of the fail camera's 26 causes.
  - Glyph's "Watch the replay" is now "Watch this run": the end-card test keeps "replay" for the engine's verdict.
  - "False tip" is gone: the glossary says Mislead.
  - The Alert number's reason is the cause's own short, from the why block; Standing's is `debrief.standing.causes`.

**Outside text.json.** A `debrief` block needs a schema, and `TextFile.debrief` is strict:
- `contracts/missions/handover/debrief.schema.ts` (new): `Debrief`, moved out of `text.schema.ts` by topic (brief 084), plus `DebriefPage`.
  - A slot's size only makes sense at its widest fills, so the schema holds the raw text to one line. The style checks held each text to its slot.
- `text.schema.ts`: `Debrief` is moved out and re-exported, so the file goes from 650 to 617 lines.

## Read before committing

The loop's 10/10 means Motive found nothing to fix. Some accepted families read flatter than today's, though truer. To undo one family: `bun tools/style-agent/cli.ts apply --restore <family>`. Families I'd read first:
- `bark/reply-inez-help-pressure`:
  - "You paid my debt? I'm yours. Say when." is now "You've been kind. Of course I'll help.";
  - "Debt's gone. So is my price." is now "Well, that's worth a favour. I'll help.";
  - "Help me once more, and you can count on me." explains the rule rather than saying a line.
- `bark/reply-driver-help-pressure`: the "kept" lines are clunky ("Thanks for looking after your driver again.").
- `bark/final-fight`: "Civic Police! Drop your weapons!" is now "Officers, move in on the car."; "That's him! Take the car!" is now "Take the car before they leave.".
- `bark/ack-bodies`: the Fixer's put-down lines speak to the body ("We'll lay you down gently.").
- `bark/arrive` (Journal only): "Here." opens several of the Tech's lines.

## Checks run

- `bun test tools/style-agent`: 34/34. All 17 short-line tests pass, and every saved run rebuilds from its archive without a model: all 24 conversation runs (checked one by one) and all 66 short runs (the test).
- `bun run test tools/dialogue tools/game/proofdelta.test.ts contracts/missions/handover/text.test.ts contracts/missions/handover/conversations.test.ts tools/names`: 353/353 on the final file.
  - An earlier run had failed proofdelta's "every Host*.cs has a row", on Latch's new `HostDebrief*.cs`. It passes now.
  - Two CLI-gate timing tests once failed under the parallel runner and passed alone and on the rerun.
  - The names failure from brief 100 (`HostTraffic.cs` "Vale") is gone.
- `bun run typecheck`: clean.
- `SUBORNED_SIM_UNITY_OUT=out/scratch/fable/short-lines/unity-assets bun run guard -- bun tools/sim/unity.ts --tree`: the output went to scratch.
  - mission text: text.json 9fc4d4b52371 (suborned.text/1 parses), on the final file;
  - headless 473/473 and range 97/97 with identical replay.
- `apply` runs twice to the same bytes. A family applied, then restored, gives the original file back (tested).

## Open, and for whom

- **Aster:**
  - The agent GDD §10 says bubbles are at most 10 words; §14 Fable and the human GDD say twelve. I used twelve.
  - I touched `text.schema.ts` and added `debrief.schema.ts` (above).
  - The cost ran higher than it should have: the `/2` tooltip brief and the `/4` fill meanings were my errors, and each took a second pass.
- **Latch:**
  - `debrief.page` is ready to build. The keys, their fills and what each reads are in `debrief.schema.ts`'s descriptions and the last part of `debrief.rule`.
  - Fills: `{clock}` `{op}` (the book's cell name) `{who}` `{n}` `{trail}` `{alert}` `{missions}` `{guards}` `{cameras}` `{place}` `{by}` `{list}`.
  - Next time is keyed by the fail camera's keys (`less.failcam`).
  - The host's stock ack ("On it.", `HostActivity.cs:265`) is why acknowledgements can't be cut.
- **Glyph:**
  - Check the keys added beyond your stand-ins, and "Watch this run".
  - Check the page lines that read terse: "Guards +{guards}/cameras +{cameras}: {place}", "Pay tape arrived".
- **Fable, next:**
  - A second pass on the ten families that didn't converge (about 130k tokens each).
  - `bark/read-people` with the Popovic fix.
  - Ten minutes reading the families listed under "Read before committing".

## Review (Aster, 2026-11-07)

- **Put back to today's lines** (`apply --restore`, one call, the seven together), so every later `apply` must name them all:
  `bark/reply-inez-help-pressure,bark/reply-driver-help-pressure,bark/final-fight,bark/ack-bodies,bark/arrive,bark/guard-call,bark/read-people`.
  - The first five are the families listed under "Read before committing": truer but flatter, and a bark's voice is the point.
  - The last two are Fable's own restores. A loop of one-family `--restore` calls had undone them, and `conversations.test.ts` caught the "about a minute" lines again.
  - All 123 rows of the seven match the last commit.
- **Guide /8** (`short-types.ts`, `short-barks.ts`): the guard at the café is no longer briefed as "unguarded for about a minute". `conversations.test.ts`'s banned words (the spill, an envelope, a bonus, a promise, a time) now reach the 132 rows it reads, as a must-not pattern, so a rerun of `bark/guard-call` can't accept them. Runs on /1 to /7 rebuild as they ran (`short.test.ts`).
- **Dropped from the open list:** the VOICE rule's "no exclamation marks" covers only the why block and the debrief page, not barks. The flattened shouts were Motive's reading, not the rule.
- **Still open for the GDD:** a bubble is at most 10 words in agent GDD §10, but 12 in §14 and the human GDD. To fix at the next GDD version, with Sky.
- **Checks:** `bun test tools/style-agent contracts/missions/handover tools/names tools/dialogue tools/game/proofdelta.test.ts` 387/387; `bun run typecheck` clean.
