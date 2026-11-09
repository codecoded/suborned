# Brief 102 · natural words: the bar, the offenders, the sample (Fable, 2026-11-08)

Sky, on the 099 voices sample: "Maybe even drop them? They will feel repetitive". On "He bought it. He'll be back, and he knows my face.": "that's exactly the kind of scripting that bugs me. 'I will remember you.' lines etc - so unnatural".

**The sample for Sky:** `briefs/102/fable/index.html`. It shows 22 rows before and after, the 239 agent rows that go silent, and the model question. No line of `text.json` changes before Sky's yes; the only change to the file is the `silent` marks (below), which change no word in play.

## The bar

- **Short lines, guide /13** (`tools/style-agent/short-natural.ts`; `SHORT_NOW` = 13).
  - The agents' families leave the loop: acknowledgements, arrivals, landings and reads.
  - The bark families are the world's people plus the cell's scene lines: `bark/fixer-scenes` (her opener, last word and telephone call), `bark/take` (the Enforcer's words to Popovic, and Popovic's), and `bark/reply-guard`.
  - **NATURAL** gains the no-mechanics rule. No line tells the player a rule or a consequence, whoever says it. It quotes brief 102's failing examples and Sky's two kept lines, and gives the test: "would this person say it aloud, now, to whoever is there, for their own reasons?"
  - **TRUE** gains "nobody knows the game's future": no line foretells what the game will do, even where it will. A person may still say what they mean to do now ("Say when.").
  - **WORTH:** the cell's lines are said to the person in the scene, never to the commander. **MECHANICS** drops /12's forecast example ("It'll be back when they run out.").
  - The calibration is `briefs/102/fable/natural.json`: Sky's 2 kept lines, 19 failing lines and 8 passing ones. Every line is checked against `text.json`.
  - **The briefs, as well as the rubric.** A reply's brief now says what the person says apart from the facts the game shows ("Facts, never said").
    - The /4–/12 reply briefs handed the writer "No, and they remember the asking: a later offer costs more", with the temperament's "they report any approach" on the side to say. That is where "Even a favour gets reported" and "Next time, my fee goes up" came from.
    - The witnesses, shots remembered, the call box and the guard at the fight are briefed the same way.
  - **A code check** (`FORETELLS`) holds the family Sky named: remember, forget, know a face, he'll be back, a search could, they'll notice, a sore head, goes in my log, favours get reported. A questioned row is exempt, because there remembering is the answer.
  - The why block, the debrief page and the labels read as at /12.
- **Conversations, guide /4** (`GUIDE_NOW` = 4). NATURAL holds the same rule, and the same code check runs. The refused Pressure brief no longer asks Inez to "make plain she will report the approach".
- **Older runs rebuild as they ran.** All 94 saved runs (24 conversations at /2, 70 short-line families at /1–/11) rebuild exactly. /12's families, rubric and prompts are unchanged: /12 reads the voice sheet's tiers, never the new `silent` field.

## The silent rows (task 4)

- **`text.json`:** 239 agent rows (575 lines) carry `silent: the agents go quiet in play, Sky 2026-11-08; <reason>`.
  - By kind: acks 90, arrivals 13, landings 15, reads 31, street moments 62, hurt 6, the end 22.
  - Their lines stay, so one moment can come back by removing its mark.
  - The host ignores the field (`PlayText` reads named keys only), so nothing breaks before or after Latch's change.
- **The 14 that speak** are `speech.schema.ts` `SCENE_ROWS`:
  - `arrive/{ask,pay,help,pressure}/fixer`, `done/turn/fixer/*` (6) and `ack/call/fixer` (the call scene's first beat);
  - `done/take/enforcer` and `done/enter/enforcer`;
  - `cliffhanger/fixer/unnamed` (and the pending named one).
- **The checks:**
  - `speech.test.ts` holds the rule: every agent row outside a scene is silent, no person's row is, and every scene row speaks.
  - `text.test.ts` loses the pin on "You said no. You'll remember me." The line itself waits on the sweep.
- **Latch's host agrees by key.** `HostQuiet.cs` `CellSpeaks` keeps arrive/ask|pay|help|pressure, done/turn, done/take, done/enter and cliffhanger. The call's opener plays through the scene path, which the rule never meets.
  - Latch to confirm: which of the 239 never played. The 099 reading was about 80, among them the reads, the knockout and kill rows, the hide and tie landings, and the struggle.
  - The host could read `silent` as its dial instead of the hard-coded set; either way the two agree today.

## The offenders (`offenders.json`; checked against `text.json` by the scratch script)

133 lines still in play voice a rule or a consequence. 92 fail and 41 are close (natural words that still lean on a rule).

**Where:**
- People's rows: 123 lines in 76 rows (89 fail, 34 close).
- The scenes' own lines: 7 (1 fail, in seven Ask scenes: "I can't pay"; 6 close, the patrol explained).
- The cutaway conversations: 3 (Inez's "I'll find a way to warn you", in two of them; one close line in the street after the pressured call).

**What the lines do:**
- announce the report: 36
- recite a rule: 34
- narrate a reaction as a plan: 25
- promise memory: 16
- tell the player what to do next: 10
- offer a Call that doesn't exist: 6
- say what the speaker couldn't know: 4
- announce a later feature: 2

**The worst places:**
- the alarmed answers: every row where someone takes fright has a line announcing the report;
- the guard's answers: rules, and Calls he doesn't have;
- the witnesses' "I'll look first, then tell the police";
- the Fixer's last word after a no, a fright or a small offer.

The regex finds nothing I missed. Popovic's lines and the calls are clean.

**Kept on purpose (Sky's call):**
- the patrol's "Walking is encouraged. Running is noted." (the city's regulation, in the voice sheet's words);
- the driver's "I'll give you a couple of minutes there" (his own terms);
- "I remember them, officer" (questioned, not played yet);
- police radio.

## The sweep, after Sky's yes (planned, not run)

1. **The loop at /13 on 13 families.** Twelve have listed lines. The thirteenth is the take, whose Enforcer lines are said to the commander, not to Popovic. The other 8 families keep today's lines.
2. **10 lines by hand:** 7 in the scenes and 3 in the conversations, which Sky has read and liked.
3. **Then:** apply with Aster's restore list, the export to the player (`bun tools/sim/unity.ts --tree`), `bun run hostlogic` and the text tests.

**Tokens**, at 35 saved bark runs averaging 118k, /13 prompts about a quarter longer, and a third of the families rerun:
- about 2.5M in all;
- with Opus judging, about 1.1M Claude tokens and 1.4M Codex tokens;
- $0 marginal: Claude is on the subscription, Codex on the ChatGPT plan;
- about 8 minutes a family: 1¾ h one at a time, 40 min three at a time.

**The model, my pick:**
- Claude Opus 5.5 judges (Motive), with the writer as today (gpt-6.1-sol). The model that wrote the flat lines also passed them, and GDD §14 names Opus for Motive.
- It needs a Claude caller for the loop, so its calls are recorded and rebuild like Codex's. `tools/rag/claude.ts` has the CLI, schema and transcript parts; this is about 200 lines (Splice or Fable).
- It also needs `SUBORNED_LIVE` with Sky's go-ahead.
- If the adapter waits: a Claude read of each accepted family before apply, about 20k tokens a family, 0.3M in all.

## Files

**New:**
- `tools/style-agent/short-natural.ts` and `natural.test.ts` (9 tests);
- `contracts/missions/handover/speech.schema.ts` and `speech.test.ts`;
- `briefs/102/fable/` (index.html, sample.json, offenders.json, natural.json, these notes).

**Changed:**
- `contracts/missions/handover/text.json` (the silent marks only), `text.schema.ts` (the `silent` field, +2 lines), `text.test.ts` (the pin goes);
- `tools/style-agent`: short-types, short-guide, short-barks, short-prompts, short-checks, short-cli (a comment), types, guide, briefs, checks, README.

The scratch generators are in `out/scratch/fable/102/` (mark-silent, offenders, sheet, verify-all, regex-scan).

## Checks

- `bun test tools/style-agent contracts/missions/handover tools/dialogue`: 336/336.
- The suites downstream of `text.json` (proofdelta, hud-budget, names, levels/dialogue): 163/163. The levels dialogue code hash caught the new file until it took the `.schema.ts` name, as the folder's other split files do.
- `bun run typecheck`: clean.
- Every saved style-agent run rebuilds exactly: 24 conversations and 70 short-line families.
- Not run: the export to the player and `bun run hostlogic`. No words changed; those belong to the sweep.


## The sweep (Fable, 2026-11-08, after Sky's yes)

Sky on the sample: "yes - rewrites are much better" (the test and the 22 rewrites); "we can try - sure" (Opus 5.5 judging, gpt-6.1-sol writing); and on the four lines I'd kept on purpose: "not into those e.g. 'Citizen. Walking is encouraged. Running is noted.' - that noted is AI sounding". Aster's defaults: a news card for the rivals following our car out (`book.feed.cards.rivals-follow`), and the Mislead as a scene with a person (the Scout's tip comes back, `arrive/mislead/scout` in `SCENE_ROWS`).

**The sheet for Sky:** `briefs/102/fable/sweep.html`.

### The Claude caller (`tools/style-agent/live-claude.ts`, 111 lines; `live.ts` routes it)

- `--judge-model claude-opus-5-5` runs Motive's evaluations through the subscription's `claude` CLI; Fable still writes on Codex (`--model gpt-6.1-sol`). Live only with `SUBORNED_LIVE=1` and `--approved-by`, as for Codex.
- One tool-less call per evaluation: Motive's Claude card (`.claude/agents/systems-designer.md`, GDD §14: Opus 5.5, effort medium) as the system prompt, structured output the only tool (the CLI's init lists exactly `StructuredOutput`, no MCP servers), no CLAUDE.md, no settings, hooks or side traffic, in an empty temp folder. `CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC=1` stops the CLI running a small helper model over each prompt (about 10k tokens a call; the first run, the Mislead, still has it on record).
- The record: the CLI's stream-json transcript, the launch (`audit`: argv with the card and schema, the prompt's hash, effort, timeout, model), the role card's hash, the models' token split. The approval names both models (`authorization.judge`), and the Claude card is archived with the sources.
- `style:replay` and `style:verify` rebind every judged call to its transcript (output and tokens read again), the archived card, the schema, the effort and the model, and rebuild the loop without a model. A judged call's tokens count every model the CLI used.
- Tests (`judge.test.ts`, offline): the call's shape, the token count, a judged run rebuilding from its archive, eight forged edits failing, the refusals.

### Sky's words first

- The 22 rewrites are in `text.json`: 20 bark rows carry `sky: "Sky's words, 2026-11-08 (…)"` (`speech.schema.ts` `SkyWords`, on the `parked` line of `text.schema.ts`, so it doesn't grow), the seven Ask scenes' "It's nothing much." and Inez's "Whatever this is, please be careful." in the two full-Help conversations.
- They win over any run, as "Turn driver by": `apply` never writes a `sky` row, and from guide /14 those rows are not items; each family's context gives them, so no line repeats one.
- `conversations.test.ts` still holds every conversation to the 096 sample Sky read, with brief 102's lines on top (`briefs/102/fable/hand.json`).

### Guide /14 (`short-natural14.ts`; older runs rebuild as they ran)

- NATURAL also fails AI-sounding words whoever says them, the patrol and the police on the radio too: noted, understood, proceeding, acknowledged, affirmative, rest assured, and their kind (copy that, confirmed, logged, standing by, duly, as requested, be advised, negative, processing, compliance). A code check holds them. The source is `briefs/102/fable/ai-words.json`: the words, Sky's four rejected lines, and today's five lines with such a word ("Running noted. Please walk.", "Alarm noted. Proceeding to the scene." …).
- The /13 pass Sky turned down ("I remember them, officer.") leaves the bar; Sky's four go into the calibration as failing.
- The patrol's voice note no longer quotes "Running is noted."; the police's is "a policeman's plain words", not "procedural".
- `bark/mislead`: the Scout's tip to the rival lookout (briefed as a lie, told as a friendly stranger, about the Office's public door, where he goes to look) and his answer, neither cut. `bark/rivals` loses the answer.
- `label/rivals-follow`: the news card, a family of its own so the HUD's other words stay as they are.
- /12 bark runs never apply (Sky quieted the agents); /13 and later do.

### A run can continue another (`--continue`), and two protocol fixes (/15, /16)

- Opus is a strict judge: on the first pass no family of more than two items reached 10 in three revisions (they ended on 7 to 9, mostly on small overlaps between items). Starting again from today's lines would repeat the same climb, so `chain --short --continue` picks up a family's latest run that ran out of revisions on the same rubric, family and approval, and revises its last draft from its last findings, within the same limits (8 calls, 15 minutes). The record keeps that round and the run it came from (`continues`); verify checks the earlier run ended on exactly it. `core.ts` gained the start (4 lines); a run without it rebuilds as before.
- **/15**: Opus scored `bark/witnesses-crowd` 9 on every rule, MECHANICS and SHORT included, with no finding, four rounds running ("I'd ship it as written … none has the bite of Sky's standard lines"). The writer had nothing to revise. From /15 a rule below 10 needs a finding naming the line and what would lift it.
- **/16**: the writer cut `alarm/watcher` as `fields: [{ lines: [] }]` four rounds running, and the old check ("a cut item has no lines") blocked a clean 10 each time. From /16 a field with no lines is no lines, and the check says what to return.
- Both leave the rubric, the sources and the families as at /14 (`sameRubric`), so a /15 or /16 run may continue a /14 one. Older runs rebuild as they ran.

### The sweep

**19 families through the loop:** gpt-6.1-sol writing, Claude Opus 5.5 judging, approved by Sky, `SUBORNED_LIVE=1`. That's my 13, plus four that held Sky's rejected lines or AI-sounding words (`noticed`, `alarm`, `machines`, `gunmen`), the new `bark/mislead` and `label/rivals-follow`.

- **17 accepted, and I read each against the bar before `apply`.**
- **2 not accepted:** `bark/summons` and `bark/witnesses-crowd`, six passes each. Today's lines stay for them.
- **`apply`** ran once with Aster's restore list (`bark/ack-bodies,bark/arrive,bark/guard-call,bark/read-people,why/gunfire,why/police,why/on-the-way`), less the three families whose /14+ runs are now accepted (`reply-inez-help-pressure`, `reply-driver-help-pressure`, `final-fight`). Restoring those would have undone the sweep.
- **`apply` changes:**
  - A row two families have is written by the newer run (the families split at /13 and /14).
  - A row marked `sky` is never written.
  - A cut that takes an event's last speaker quiets the event: its log codes go into silentLogs with the cut's reason. That was `patrol-resumes`; until now `apply` stopped there.
- **What changed:** 183 bark rows (349 new lines), 5 of them cut as filler: the patrol's three status lines, the camera room's alarm, a rival's "I'm out. Keep driving." With Sky's 20, that's 203 rows and 390 new lines since the round began.

| family | passes (scores) | result | tokens |
|---|---|---|---|
| `bark/mislead` | 7→8→9→10 (/14) | accepted, pass 1 | 171k |
| `bark/fixer-scenes` | 6→5→7→8 (/14); 9→9→8→8 (/14); 8→8→8→8 (/14); 8→9→9→9 (/14); 9→9→9→9 (/15); 10 (/15) | accepted, pass 6 | 841k |
| `bark/reply-guard` | 6→7→8→8 (/14); 9→10 (/14) | accepted, pass 2 | 248k |
| `bark/take` | 5→8→8→8 (/14); 8→9→9→9 (/14); 9→9→9→10 (/14) | accepted, pass 3 | 462k |
| `bark/reply-inez-ask-pay` | 6→7→7→8 (/14); 8→8→8→7 (/14); 8→9→9→9 (/14); 9→10 (/14) | accepted, pass 4 | 701k |
| `bark/reply-inez-help-pressure` | 7→7→8→9 (/14); 9→10 (/14) | accepted, pass 2 | 284k |
| `bark/reply-driver-ask-pay` | 5→6→8→9 (/14); 8→9→9→9 (/14); 9→10 (/14) | accepted, pass 3 | 436k |
| `bark/reply-driver-help-pressure` | 5→6→7→7 (/14); 7→8→8→9 (/14); 8→8→8→9 (/14); 9→9→9→9 (/14); 10 (/15) | accepted, pass 5 | 839k |
| `bark/summons` | 6→6→7→7 (/14); 7→7→8→8 (/14); 7→8→8→9 (/14); 9→9→9→9 (/14); 9→8→8→8 (/15); 8→7→8→9 (/16) | **not accepted** | 1123k |
| `bark/machines` | 7→7→8→9 (/14); 8→8→9→9 (/14); 9→9→8→8 (/14); 9→9→9→8 (/14); 9→10 (/15) | accepted, pass 5 | 657k |
| `bark/noticed` | 6→5→7→7 (/14); 6→7→7→7 (/14); 7→7→8→8 (/14); 9→9→9→10 (/15) | accepted, pass 4 | 698k |
| `bark/witnesses-crowd` | 5→6→7→7 (/14); 8→8→8→8 (/14); 8→9→9→8 (/14); 9→9→9→9 (/14); 8→8→8→9 (/15); 9→9→8→8 (/16) | **not accepted** | 887k |
| `bark/witnesses-cast` | 5→6→7→8 (/14); 9→8→9→9 (/14); 9→8→8→9 (/14); 10 (/15) | accepted, pass 4 | 568k |
| `bark/aftermath` | 6→7→8→8 (/14); 8→8→9→9 (/14); 10 (/14) | accepted, pass 3 | 434k |
| `bark/alarm` | 4→6→6→7 (/14); 7→8→7→8 (/14); 9→9→10→9 (/15); 10 (/16) | accepted, pass 4 | 456k |
| `bark/final-fight` | 5→7→7→8 (/14); 8→9→8→9 (/14); 9→10 (/15) | accepted, pass 3 | 401k |
| `bark/gunmen` | 6→6→7→7 (/14); 7→8→8→8 (/15); 9→9→9→10 (/16) | accepted, pass 3 | 543k |
| `bark/rivals` | 6→8→8→7 (/14); 9→10 (/15) | accepted, pass 2 | 212k |
| `label/rivals-follow` | 10 (/14) | accepted, pass 1 | 21k |

**By hand (`briefs/102/fable/hand.json`):** the scenes and the conversations aren't in the loop's families.
- Sky's two lines: the Ask scenes ×7 and Inez's line in the two full Help conversations.
- My 10 lines, in 20 places:
  - the driver's Ask opener in 10 scenes ("Yes? Be quick, I'm waiting on the Councillor.");
  - the Fixer's "Free of charge" in 2 scenes;
  - the guard's patrol timing and his "Five minutes" in the Watch scene;
  - the shift change's patrol talk;
  - the guard and Inez at the café;
  - the guard's street line after the pressed call ("Stay inside, Inez. I'm getting back to that door.");
  - the driver's "couple of minutes", one of Sky's four ("The east yard. Fine, but I'll keep the engine running.").
- `conversations.test.ts` holds the conversations to the 096 sample plus these.

### Tokens

All 67 runs: **gpt-6.1-sol 4,691,634 · Claude Opus 5.5 5,249,922 · Claude Haiku 5.5 40,613** (the CLI's helper, in the Mislead run only, before I switched it off). That's 9.98M in all, against my 2.5M estimate: Opus seldom passed a first or second draft. Both are plan or subscription calls, no paid API. The subscription's weekly use went from 64% to 68%.

### For Latch (keys)

Latch reads by key, and no key moved but these.
- **`dialogue`:**
  - `arrive/mislead/scout` speaks now: no `silent`, three tips about the front door. It's in `SCENE_ROWS` and Latch's `CellSpeaks`.
  - `rivals-misled/rivals` has new answers.
  - 5 rows are gone: `patrol-resumes/patrol`, `patrol-sweep/patrol`, `patrol-stands-down/patrol`, `alarm/watcher`, `ai-gone/rivals`.
  - 20 rows carry `sky`, which the host ignores.
- **`events` and `silentLogs`:**
  - `patrol-resumes` is gone, and `patrol.resumes` is in silentLogs.
  - `patrol-sweep`, `patrol-stands-down`, `alarm` and `ai-gone` each lost one speaker.
- **`book.feed.cards.rivals-follow`:** "Rivals after our car" / "three ran from their posts". `HostCrucial` already reads it.
- **`scenes.items` and `scenes.conversations`:** lines as in `hand.json`.
- **`tone.voices`:** `patrol` and `security` are reworded.
- The export to the player is done (`bun tools/sim/unity.ts --tree`).

### Checks

- **The export** (`bun run guard bun tools/sim/unity.ts --tree`): ok, before and after `apply`.
- **`bun run hostlogic`:** 180/180 before `apply` and again after it, on the exported words.
- **`bun test tools/style-agent contracts/missions/handover tools/dialogue tools/names tools/drift-auto`:** 386/387. The one failure is drift-auto's "nothing changed", for the skyline (r011) and people (r012–r016) objectives. Those ledgers declare nothing for their current code hash, and that is already so at HEAD 28a38799 with no file changed in the tree. It's not this round: owners Datum/Splice (skyline) and Pulse (people).
- **The dialogue objective:** this round's three schema edits, on top of 341ff4fc's `speech.schema.ts`, moved its hash. I declared it with `bun tools/drift-auto/cli.ts --base 97e3251c --objective dialogue`: all 3 runs replay as at 97e3251c, confirmed 3/3 OK.
- **`bun run typecheck`:** clean.
- **Every saved style-agent run rebuilds exactly, without a model:** 24 conversations and 137 short runs, the sweep's 67 among them (`out/scratch/fable/102/verify-all.ts`).
- **Tests changed:**
  - pins on today's words, the rule kept: the count of bark items (497 → 492), /12's failing calibration lines (the row stays, the line was rewritten), the patrol voice pin, the old refused pin;
  - new: `judge.test.ts` (4) and `natural14.test.ts` (10).

### Open

- **`bark/witnesses-crowd` isn't accepted** after six passes (8 to 9; 887k tokens).
  - Each row plays in several situations: seeing a takedown, finding a body alone, a body carried. Every line must hold for all of them, and the judge finds a new case each round.
  - 7 of my offending lines stay in play: "I'll go and report this.", "No. I can't tell anyone.", "Tell someone? No. I can't.", "I'll radio this in." and three close ones.
  - Options:
    - brief the rows by situation (Pulse or Keel: does the event know whether anyone else is there?);
    - accept the best 9;
    - or I write those 7 lines by hand.
- **`bark/summons` isn't accepted** after six passes (ended on 9; 1.12M tokens).
  - 19 similar refusals overlap on VARIANTS.
  - 6 lines stay, among them Sky's rejected "I remember them, officer.", in `questioned/waitress/gives`, which is not played yet.
  - Options: a seventh pass, splitting the family (the refusals and the questioned rows), or by hand.
- **Small things on the sheet's "What I'd look at":** two idioms shared across families, and Inez's warning at the car, now "Stop, love! Those men by your car have guns!".
- **The conversation guide** (still /4) doesn't carry the AI-words rule yet; no conversation ran this round. A test-level check could hold every line in play against `ai-words.json` in the meantime.

### Files

- **New:**
  - `tools/style-agent/live-claude.ts`, `short-natural14.ts`, `judge.test.ts`, `natural14.test.ts`;
  - `briefs/102/fable/ai-words.json`, `hand.json`, `sweep.html`;
  - 67 runs in `runs/style-agent/short/` (2026-11-08T14-38 to 15-17).
- **Changed:**
  - `tools/style-agent`: `core.ts` (a run's start), `live.ts`, `evidence.ts`, `short-evidence.ts`, `short-cli.ts`, `cli.ts`, `types.ts`, `short-types.ts`, `short-guide.ts`, `short-prompts.ts`, `short-checks.ts`, `short-barks.ts`, `short-labels.ts`, `short-apply.ts`, `README.md`, and the tests `short.test.ts`, `voice.test.ts`, `natural.test.ts`;
  - `contracts/missions/handover`: `text.json`, `text.schema.ts` (`sky` on the `parked` line, so no growth), `speech.schema.ts`, `book.schema.ts`, `text.test.ts`, `conversations.test.ts`;
  - `tools/levels/dialogue/drift.json`;
  - `briefs/102/fable/sample.json` (status).
- **Scratch:** `out/scratch/fable/102b/`: the sheet's generator, the status, token and read scripts, and the logs.


## By hand: the 13 lines the loop didn't finish (Fable, 2026-11-08)

Aster, taking the option I offered: write the 13 failing lines of `bark/witnesses-crowd` (7) and `bark/summons` (6) by hand, have Opus score them once through the Claude caller with Sky's approval, and apply them as hand edits that no run replaces.

**The lines and the judge's read:** `briefs/102/fable/hand-barks.json` (before, after, why; `judged` where Opus found fault) and the sheet's section "By hand: the two families the loop didn't finish" in `briefs/102/fable/sweep.html`.
- Opus (Motive, Claude Opus 5.5, effort medium) scored each family once, as written, no writer call: `runs/style-agent/short/2026-11-08T15-58-54.321Z-bark-witnesses-crowd` (18,791 tokens) and `…T15-58-56.270Z-bark-summons` (25,928 tokens). Subscription, no paid API.
- The family scores are for every line in the family, today's included: **4** for the witnesses (WORTH 7 · VOICE 6 · NATURAL 6 · VARIANTS 4 · MECHANICS 8 · TRUE 7 · NAMES 10 · SHORT 10) and **3** for the summons (WORTH 5 · VOICE 4 · NATURAL 3 · VARIANTS 4 · MECHANICS 7 · TRUE 3 · NAMES 10 · SHORT 10). Of 26 findings, 5 are on my lines; the other 21 are on lines I didn't touch.
- **8 of my 13 drew no finding** and stand as judged. Opus named four of them among the best in the family: "That could have been me.", "I want to go home.", "I've been off this door enough today, Inez." and "That's the one, officer. I'm not taking the blame for anybody."
- **5 drew a finding, and I rewrote them from it.** To keep to "score once", they were not scored again:
  - the guard's "Never, in all my years on the door." (TRUE: an invented service history) is now "Good grief. What's gone on here?";
  - "Easy, now. Do it properly, by the book." (VOICE: coaching himself) is now "That's a coward's work, that is.";
  - the passer-by's "Here? In this city? I won't have it." (VOICE: stagey) is now "This can't wait.";
  - the guard's no-visit pair "Leave Inez out of this. That's final." / "I said I'd help you. I never said Inez would." (TRUE: an invented motive) is now "Fetch Inez? What good would that do you?" / "Inez has her tables, and I have my door."

**Situations (the witness rows):** every new line is true for all three cases a row plays in: a takedown seen, a body carried, a body found. None needs splitting. The engine narrows two rows (sim `GameTakedown.cs`): crowd `avoid` is only a body found (a fearful person who sees the act flees), and the guard who sees a takedown engages as security, so his `radio` row is a body found, or a takedown when he is the ally in the final fight.

**A brief that was wrong:** `summon-no-visit` is given only when the contact's tie has no post to leave (`GameOrders.cs` SummonRefusal). In the Handover's cast that means a call on the turned guard, whose tie is Inez. So the guard answers the Fixer, and the host's card (`HostFeed.cs` FeedSummonRefused) shows Inez's no-visit line after his. The /4–/16 briefs had the guard "answering Inez". Guide /17 briefs both rows as played.

**Rows that need their situations split (not mine to fix):**
- `summon-refused/waitress/summon-no-visit`: in the Handover it plays only as the tie's line on the guard's card, and Inez isn't on that call. No line of hers can be true there. Latch could leave the tie's line off that card.
- `summon-refused/guard/summon-off-street` (and too-far): the event says the guard speaks "if he is within earshot", but the host plays him every time. The new too-far line holds from his door; off-street needs the earshot check (Latch).

**What Opus found in the rest of these families (not changed; on the sheet):** the guard's four radio calls ("incident to report" four ways); "Calling the desk about this." (labels the act); two near-synonym pairs (avoid, quiet); the questioned rows ("I'd know that face", "I don't remember anything", "I notice nothing" in two mouths, the driver's lie about moving the car); "Try me again." and "Ask later." (tell the player what to do); "Not again this shift" (recites the rule); "the violence" (the game's category word); the alarm lines reading the HUD back; Inez's "I'm afraid" in five lines; Inez's no-visit lines ("send for", "call over"). They wait on Sky's word or another round.

### The tooling (guide /17; older runs rebuild as they ran)

- **`hand` on a row** (`speech.schema.ts` `HandWords`, on the Entry's `parked` line of `text.schema.ts`, so it doesn't grow): Fable's hand edits. Like `sky` rows, they are not items from /17, their family's context gives them, and `apply` never replaces them.
- **`judge FAMILY --hand FILE`** (`short-cli.ts`): today's family with the file's edits in place, scored once by the Claude judge (`maxRevisions 0`, one call). `core.ts` takes a given first draft (`given`). The run records `hand` (who wrote it, why, the set), and its approval says `provider: hand` (`types.ts`). `verifyShort` rebuilds it and refuses a hand run with a writer call, or a hand set that differs from the one judged. `--continue` never picks up a hand run.
- **The no-visit tells at /17** (`short-barks.ts` `NO_VISIT_17`). The rubric reads as at /16 (`sameRubric`).
- **Tests:** `hand.test.ts` (5): /17 reads as /16; a hand set; a hand run calls only the judge and rebuilds, five forged edits fail; hand rows are not items and the no-visit tells; text.json carries every edit and `apply` holds a hand row. `natural14.test.ts`'s pin on `SHORT_NOW` is now "at least 16".
- **The dialogue objective:** the two schema edits moved its code hash. Declared with `bun tools/drift-auto/cli.ts --objective dialogue`, no rule moved, confirmed 3/3 replay as at HEAD b451d108.

### Checks

- **Export** (`bun run guard bun tools/sim/unity.ts --tree`): ok. Headless 472/472 and range 99/99 with identical replay; text.json a0550b48f5c3 parses.
- **`bun run hostlogic`:** 180 pass, 0 fail.
- **`bun test tools/style-agent contracts/missions/handover tools/dialogue`:** 355/355.
- **`bun test tools/drift-auto tools/names tools/levels/dialogue`:** 45/46. The one failure is drift-auto's "nothing changed" for skyline (r011) and people (r012–r016), as before this round (owners Datum/Splice, Pulse). The dialogue objective is clean after the declaration.
- **`bun run typecheck`:** clean.
- **Every saved style-agent run rebuilds exactly, without a model:** 24 conversations and 139 short runs, the two hand runs at /17 among them (`out/scratch/fable/102c/verify-all.ts`).

### Files

- **New:** `briefs/102/fable/hand-barks.json`, `tools/style-agent/hand.test.ts`, and the 2 runs in `runs/style-agent/short/`.
- **Changed:**
  - `contracts/missions/handover/text.json`: 9 rows, 13 lines, each marked `hand`. No other key moved, so Latch reads them by the same keys.
  - `contracts/missions/handover/speech.schema.ts` and `text.schema.ts` (the same line count);
  - `tools/style-agent`: `core.ts`, `short-loop.ts`, `short-types.ts`, `short-evidence.ts`, `evidence.ts`, `types.ts`, `short-barks.ts`, `short-apply.ts`, `short-cli.ts`, `cli.ts`, `README.md`, `natural14.test.ts`;
  - `tools/levels/dialogue/drift.json`;
  - `briefs/102/fable/sweep.html` (the new section, and a line in "For your read") and these notes.
- **Scratch:** `out/scratch/fable/102c/` (check-hand, apply-hand, sheet, verify-all, the judge logs).


## By hand, a second round: the judge's other 21 findings (Fable, 2026-11-08)

Aster: the judge's 26 findings on `bark/witnesses-crowd` and `bark/summons` included 21 on lines I wasn't asked to touch (the guard's four "incident to report" calls, "I'd know that face", "Try me again.", "Ask later.", "the violence"…). Fixed the same way: by hand, scored once per family with Sky's approval, rewritten from the findings, applied as `hand` rows.

**The lines and the judge's read:** `briefs/102/fable/hand-barks-2.json` (37 edits: before, after, why; `judged` where Opus found fault) and the sheet's section "By hand, a second round" in `briefs/102/fable/sweep.html`.
- **Briefed from the engine first.** The guard's radio call to the desk (`report-begins/guard/radio`) plays only for his attention report on one of the cell standing about or running in his sight (HostActivity.cs, GameAttention.cs), never a body, so the judge's "Somebody's down out front" would be untrue. A busy guard (`summon-busy`) is down, held after coming round, in a fight, or already away on her last call (GameOrders.cs SummonRefusal), so "I can't leave the door right now." was untrue most times it played: rewritten too (the one line beyond the 21).
- **Guide /18** (`tools/style-agent/short-hand18.ts`, `short-barks.ts`, `short-briefs.ts`, `short-evidence.ts`, `cli.ts`, `short-types.ts`): six of the faulty lines sat in rows already marked `hand`, which /17 leaves out of a family's items. A hand run now scores its family as it plays, hand rows included; writer runs keep them out. The rows above are told as the engine plays them. Older runs rebuild as they ran.
- **Opus scored each family once** (Motive, Claude Opus 5.5, subscription): `runs/style-agent/short/2026-11-08T16-42-41.031Z-bark-witnesses-crowd` (**5**, up from 4; 7 findings, 18,258 tokens) and `…T16-42-42.986Z-bark-summons` (**6**, up from 3; 10 findings, 24,766 tokens). 18 of my 32 new lines drew no finding; 14 did, and 5 more on last round's or untouched lines (three of last round's guard lines, "This can't wait.", and two old lines). All 19 rewritten from the findings, not scored again. Where the judge's own fix was untrue (a man down by the door, "hanging about" for a runner, "Ring me back", a sergeant, the bonnet up all morning), the why says so.

**Still for the host (Latch):**
- `summon-refused/guard/summon-busy`: when the guard is down the host still plays his line; leave it off then.
- `summon-refused/waitress/summon-no-visit`: written now as the judge read the card, Inez's own reply after the guard's line ("Me? And who'd mind the café?"). If the host drops her line from that card, as suggested last round, the row needs briefing again.
- `summon-refused/guard/summon-off-street` and `…/summon-too-far`: the earshot split, unchanged. Off the street never happens in the Handover (the guard's pose is null only when he is down; Inez always has her post).

### Checks
- **Export** (`bun run guard bun tools/sim/unity.ts --tree`): ok. Headless 472/472 and range 99/99 with identical replay; text.json f761ea2febc9 parses.
- **`bun run hostlogic`:** 180 pass, 0 fail.
- **`bun test tools/style-agent contracts/missions/handover tools/dialogue`:** 357/357 (`hand.test.ts` now 7: two rounds, /18's hand families and tells).
- **`bun run typecheck`:** clean.
- **Every saved style-agent run rebuilds exactly, without a model:** 24 conversations and 141 short runs, the two /18 hand runs among them (`out/scratch/fable/102d/verify-all.ts`).
- `bun test tools/drift-auto tools/names tools/levels/dialogue`: 45/46, the same drift-auto failure as before (skyline r011, people r012–r016); the dialogue objective did not move.

### Files
- **New:** `briefs/102/fable/hand-barks-2.json`, `tools/style-agent/short-hand18.ts`, the 2 runs in `runs/style-agent/short/`.
- **Changed:** `contracts/missions/handover/text.json` (23 rows, each marked `hand`; no key moved); `tools/style-agent`: `short-types.ts`, `short-barks.ts`, `short-briefs.ts`, `short-evidence.ts`, `cli.ts`, `hand.test.ts`, `README.md`; `briefs/102/fable/sweep.html` (the new section, a line in "For your read") and these notes.
- **Scratch:** `out/scratch/fable/102d/` (check-hand, apply-hand, sheet, verify-all, peek18, the judge, test and export logs).


## The guard's end of the call, and the loitering card (Fable, 2026-11-09)

Sky's calls on the guard (option 1: he leaves his door when called; step 4: nobody acts on a call after the cell's violence), built in engine 0.51.0 (Keel, `briefs/102/keel/report-turned-guard-call.md`; Motive `briefs/102/motive/turned-guard.md` §1.3, §1.4, §2.2). Then Aster's question from Sky on the loitering card. **Sky's read:** `briefs/102/fable/guard-call.html` (self-contained, Jost Medium embedded so the lozenge draws at its real width). **Record:** `guard-call.json` (14 rows, 4 scenes, 20 edits, 4 of Keel's placeholders kept) and `hand-whys-3.json` (the loiter cause). Every row marked `hand`; not scored (a judge run needs Sky's approval).

### The keys (Latch reads them)
- **`about` is the one the Fixer called**, on rows and scenes where the words differ by end, as Latch's `HostCall.SummonAbout` already reads it. A row with no `about` stays Inez's call's, so her lines don't move.
- **The call scene:** `scenes.items` `kind call, about guard`, Mercenary and Loyal (a guard is never Idealist or Fearful). The Fixer's ask is a literal beat, since a `ref: ack` would play Inez's ack row ("Inez, we're counting on you…"); his yes is `confirm/call/guard/<temperament>` (`events.confirm` now lists the guard and `call.guard`).
- **Refused, his end:** `summon-refused/guard/<code>/guard` for busy, came-once, alert, too-far and off-street: what the engine can give from his end (the one called is the one who leaves, so his memory of violence is `call-saw-violence`, and no-visit never comes from his end). Inez's `…/waitress/<code>/guard` for what she can overhear: busy, came-once, alert, too-far; off the street she can't hear.
- **`summon-unanswered`** (new event, no log code): Inez's call to a guard who is down and she doesn't know it. Latch plays it in place of her summon-busy line when the one who'd leave is down.
- **The café, his own call:** `guard-at-cafe/<speaker>/guard` rows and a city scene `about guard`, after Inez's scene so today's host still plays hers first. **His warning on a pressed visit:** a city scene `outcome he-warns`. The street lines are picked by who remembers the Pressure (Inez first, as `GameCall.cs` `visitWarner`), not by who called. Latch's `StreetFor(LureCalled)` would show "she warns him" on his pressured call, and nothing when he was the one pressured on hers.
- **Locks:** `byTarget.tie-unknown`, a new lock variant (`text.schema.ts` `LOCK_VARIANTS`): the general form before a Listen (Motive §1.3), naming only `{who}`. The host passes it as `ctx.variant`. The Handover's briefing names the pair, so its base lines stay.
- **Debrief:** `debrief.page.how.answered` ("on the Fixer's call") for `call.guard`. Latch's working tree maps `call.guard` to `called`, which is "Inez called him over".

### What changed beyond the brief, and why
- **The guard's four turned results** said "There is nothing to call {who} for yet." Now: "{who} is turned. Call {who} off the door."
- **`actions.call.onTarget.guard`:** the base line ("Call Inez to draw the guard off the door") was wrong on his Call card. It names nobody he'd go to.
- **The Trail row** read "the guard called the guard over" on his call. Now: "The Fixer's call to {who}".
- **`summon-saw-violence`:** its short was "Saw violence", the same as step 4's `call-saw-violence`, so on Inez's row it read as if she saw it. Now "Guard saw violence", and the line drops "and won't come now" (a rule in a shut lock, Motive §1.4).
- **`names.targets.camera-mast-z0`:** "the square's mast camera" became "the mast camera". It is true wherever Vantage stands it, and the Intel already says "Mast camera". It is also what lets the loitering card name it.

### The loitering card (`why.alert.loiter`, guide /24)
- Title "Seen hanging about"; short "{who} saw {agent} idling"; line "{who} saw {agent} hanging about {where}."
- The lozenge's short has 142.9 px with a place, and "the square's mast camera" alone is 144.8. So the short drops the place (guide /24, `NO_PLACE_24`; Latch adds `loiter` to `HostWhy.cs` `NoPlace`), and the camera's name is shorter.
- At the widest pair, "the entrance camera saw the Enforcer idling", the short is 252.2 px of 255.6. Sky's case, "the mast camera saw the Fixer idling · 2:46", is 247.8 px of the 296 px box. "Waiting" was 5 px over and wrong for a Watch.
- /19 to /23 keep a place after it, so their runs rebuild with their room.

### Open
- **Keel:** "He's not answering" is untrue if Inez found the guard down, saw him carried or was told. Those memories aren't in the violence list, so her Call isn't shut. The host needs a read of whether she knows.
- **Latch:** the keys above. Also: HostWhy `NoPlace` + `loiter`; HowCodes `call.guard` → `answered`; StreetFor by who warns; `About` on the café's WorldBeat and CityScene (LookupScene, as `witness-reacts` does).
- **Hint H5** ("Inez acts on a call. Click her…") rings only for Inez today. If it rings for the guard, its line needs `{who}`.
- **Vantage:** the mast camera's Intel lines say "over the square" and "covers the square". They need its new place if it moves.
- **No feed beat for his call.** None of the eleven fits ("Inez called the guard." is hers), and the cap is Sky's. His call shows as bubbles and in the history.
- **Not run:** the export to the player (Latch is in `unity/Assets`) and `bun run hostlogic`.

### Checks
- `bun test contracts/missions/handover`: 213/213 (the new `call.test.ts` 10).
- `bun test tools/style-agent`: 101/101 (the new `hand-whys-3.test.ts` 3).
- Every saved style-agent run rebuilds exactly (`out/scratch/fable/102e/verify-all.ts`).
- `bun tools/lint/comments.ts`: 0 hits.
- `bun run typecheck`: clean (exit 0). An earlier run caught 7 errors in `tools/hud-budget/budget.ts` while someone else was mid-edit there; not this round, and gone once their edit landed.
- **The dialogue objective:** the two schema edits (and 101's `why.schema.ts`) moved its hash. Declared with `bun tools/drift-auto/cli.ts --objective dialogue --base d7d83de8c`: 3/3 runs replay OK.

### Files
- **New:**
  - `contracts/missions/handover/call.test.ts`;
  - `tools/style-agent/short-guard102.ts`, `hand-whys-3.test.ts`;
  - `briefs/102/fable/guard-call.html`, `guard-call.json`, `hand-whys-3.json`.
- **Changed:**
  - `contracts/missions/handover/text.json`, `text.schema.ts` (`tie-unknown`; same lines), `speech.schema.ts` (`HAND_WORDS` takes any date), `text.test.ts` (in place: `call.guard`, the guard's call scenes, the confirm pins);
  - `tools/style-agent/short-barks.ts`, `short-lozenge.ts`, `short-whys19.ts`, `short-types.ts` (/24), `lozenge.test.ts`, `short.test.ts` (the row count 492 → 506), `README.md`;
  - `tools/levels/dialogue/drift.json`.
- **Scratch:** `out/scratch/fable/102e/` (apply, record, page, the loiter fits, verify-all).
