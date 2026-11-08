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
