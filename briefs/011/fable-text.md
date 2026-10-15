# The Handover's words (Fable, brief 011)

Data: `contracts/missions/handover/text.json` (`suborned.text/1`), schema and lookups in `text.schema.ts`, checker in `text.test.ts`. Build-time only. 315 entries, 672 spoken lines, 76 refusal codes, 24 actions, 50 events (updated for engine 0.4.0: hostile AI and the re-scripted Handover).

## Voice, character by character

| who | how they talk | a line |
|---|---|---|
| Scout | terse, observant: fragments, positions, what is where | "Right beside them." / "Guard's back at the door." |
| Fixer | warm, persuasive, says "we"; steel under the kindness | "Let's lift that weight." / "I don't like this. Doing it." |
| Tech | calm, practical: minutes, what is tied to what, a little dry about old wiring | "Looped. I'm tied here." / "Old wiring. Easy." |
| Enforcer | dry, older, one to four words | "Going in." / "Mind your head, Councillor." |
| Inez | café patter, tired about the lender; her temperament shows in what she will take | Idealist: "Keep your money. I'm not for sale." Mercenary: "Count it twice. Then I'm yours." Fearful: "Please. Don't. I'll do whatever you say." Loyal: "That goes in the book." |
| the driver | gruff, practical, "the Councillor" | "Deal. Where do I put the car?" |
| the guard | officialese; the mercenary one talks bonuses | "State your business or move along." |
| Vale | formal and pompous, then frightened (loyal blusters, mercenary bargains, fearful begs) | "Name your figure. I'll double it." |
| rivals | cold, a contract and a schedule, no names | "Same man. Our contract." |
| patrol (clockwork) | fixed polite phrases that are orders | "Walking is encouraged. Running is reported." |

The coercive-underneath tone lives in the city's voices (patrol, guard, police), not in the cell: the city is polite while it watches you. The Loyal temperament always answers a wrong approach with a report ("That goes in my log"), which is the `suspicious` outcome.

## Sample exchange, route A (Inez an Idealist, helped; seed `handover-1`, picked by the reference `pickLine`)

```
Scout     A debt. Proud. Cash would insult her.        [Listen lands]
Fixer     Their trouble becomes ours.                   [Help ordered]
Inez      You paid my debt? I'm yours. Say when.        [Help lands: turned]
Tech      Looped. I'm tied here.                        [Loop lands]
Fixer     Making the call.                              [Call ordered]
Inez      Right. For the block. Watch the door.         [she confirms the call]
Guard     Nobody's hurt. Just coffee.                   [the spill: guard at the café]
Enforcer  Going in.                                     [Enter ordered]
```

Same mission, other voices: a Fearful Inez pressured ("Please. Don't. I'll do whatever you say."), the guard seeing a runner ("Hold it! No running in the square!"), the patrol ("Citizen. Walking is encouraged. Running is reported."), the rivals arriving ("Eyes on the door. Wait for him."), Vale in the car, unnamed cliffhanger ("Someone owns the Council. The next list is signed.").

## What is in the file

- `briefing`: card (5 short sentences), objective, win, each fail in plain words, clock and close (8:30), purse, the two hints, the tracker line, the status pill's timeline (the rival arrival is `known: false`, so the pill shows "?"), one line per agent.
- `targets`: one line on each thing the player can right click (who it is, only what the cell may know at the start).
- `actions`: all 24 panel verbs (label, who gives it, one plain line with its trade-off, and `onTarget` lines for Inez, the driver, the guard, Vale, the door, the cabinet). Includes `cancel`, which Keel added today after Sky's playtest, with `cancelCosts` for each cancel result.
- `activity`: what each agent is doing now, for the roster ("Watching {who}", "Carrying Vale to the car"). Not asked for; it answers Sky's "nothing showing me what the cell is doing when busy".
- `locks`: 76 refusal codes, each with the patterns that find it from the engine's text, the line saying what unlocks it, and samples the checker runs.
- `events` + `dialogue`: event × verb × speaker × outcome × temperament, 2–4 variants. `exchange` gives, per verb, the order its events play (for the collapsible dialogue: show the first beat collapsed, the whole run expanded).

## For Latch (how to use it)

- Refusals: the engine returns free text, not codes. `lockOf(text, why)` turns it into a code and a line. Replace `Humanise()`'s substring checks with it. The host's own `Locked()` reasons are in the samples too.
- Lines: `lookup(query)` then `pickIndex(seed, key, n, count)` where `n` counts how often this key has spoken this run. Port both; `pick.vectors` in the file are test vectors for the C# port. The FNV is the rulebook's.
- The collapse/expand: the panel row shows `actions[verb].does` (or `onTarget[target]`); on a click, play `exchange[verb]` in order. Collapsed view shows the last beat.
- Outcome mapping: the rulebook outcome (`turned`, `refused`, `not-yet`, `kept`), but `suspicious` replaces it when the engine also logs `report` (a Loyal person reports the approach). The cell's own reaction is `done/turn/<outcome>`.

## Checks (all in `text.test.ts`, 42 tests)

- Verbs: every rulebook verb and every `Check` order in `GameOrders.cs` has an action.
- Refusals: the test reads every `return "…"` refusal in `GameOrders.cs`, `GameCombat.cs` and `GameCancel.cs`, plus every `Describe` case, and fails on any text with no lock. It caught Keel's new `cancel` order while I was writing.
- Fails: every `Fail("…")` code has plain words and a spoken line. Every engine `Note("…")` log code is spoken by an event or listed as silent with a reason.
- Replies: derived from the rulebook with `rulebook.ref.ts` (every approach × temperament × band): each reply it can produce has a line from each role the temperament allows (73 entries), and no entry is a dead cell.
- Lines at most 80 characters (placeholders at their widest), spoken lines 1–12 words, no duplicates; no one named but Inez and Vale (the district's whole name pool is checked); tone and profanity filters; every m:ss matches the mission timeline; the purse matches the engine's; Halcyon appears only in lines marked pending.
- Mutation-checked: dropping a reply, a lock or a fail text, adding a long line, a pool name, modern speech or an unmarked Halcyon each fails a test.

## Open for Sky or others

1. **Halcyon** (pending Sky): the `cliffhanger` lines come in two sets, `named` (Halcyon, marked `pending`) and `unnamed`. Ship `unnamed` until Sky decides. The brief card does not name Halcyon, the Tomorrow Plan, Meridian or the Chrome Choir (all Story proposals).
2. ~~Inez's secret is parked~~ Corrected by Sky: Inez and the door guard are lovers (canon); see the last update below.
3. **The Fixer is not gendered** in any line (the Story spine calls her a woman); say if the Fixer should be.
4. **Keel**: refusals are free text and the replay hash covers them (`Orders` hashes `why`). A stable code beside the text would let `lockOf` retire its patterns; the patterns work until then. The `cancel` order is in Keel's working tree, uncommitted: if its refusals change, `text.test.ts` fails and says which.
5. **Motive** reviews the temperament voices (the §14 handoff); **Chime** voices the contact confirmations (`confirm`); **Glyph** lays out the card, the panel rows and the collapsible beats.
6. Not done: Hero and Menace band variants of the replies (the cells exist in the rulebook, the voice is the neutral one), the investigator's questioning lines, and the debrief lines for between-missions changes (`rulebook.debrief.lines`: turned-how, kept, informer, and so on), which belong to the debrief text, not the Handover's.

`text.json` is the source from here on: edit it directly, then `bun test contracts/missions/handover/text.test.ts`. (I assembled it from scratch scripts in `out/fable/`, which are not shipped.)

## Update: engine 0.4.0 (hostile AI) and with-target labels

- **New engine codes, now spoken.** `ai.cover`, `ai.break`, `ai.gone`, `ai.join`, `guard.stands-down`, `patrol.resumes`, `patrol.sweep` each have an event (rivals, police, guard, patrol, with a Scout or Fixer reaction where it reads). `shot.hit` and `shot.miss` are silent on purpose (tracer and pips speak).
- **AI states.** The engine does not log state changes, so the host speaks `ai-state` when a body's `Gun.St` changes, with the new state as `outcome`. Spoken states: `alerted`, `advance`, `engage`, `hold`, for rivals, police ("Hold this line."), the guard ally ("Drop your guns! Last warning.") and the patrol ("Halt. Halt. Halt."). `break` and `gone` are the `ai-break` / `ai-gone` events; `routine` and `down` are silent (`silentStates`). The drift test reads the state list from `Model.cs` and fails on a new state.
- **With-target forms (for Latch).** Each action that takes a target has `withTarget` ("Listen to {target}", "Pay {target}", "Go in by {target}", "Put Vale in {target}") and `then`, the queued clause ("listens to {target}"), so a queued line reads "{agent} walks to {target}, then {then}". `{target}` is `names.targets[key]` ("Inez", "the driver", "the entrance camera"). Verbs with no target (draw, holster, cease, stance, panic, cancel, withdraw) have neither.
- **Test.** The log-code scan now reads `Note(cond ? "a" : "b", …)` too. 46 tests green, typecheck clean.

## Request for Keel: stable refusal codes (done: adopted in fec836f; see the last update below)

Today `Game.Issue` returns `(ok, why)` with `why` free English, and `Hash.State` hashes `why`. Every consumer (the host's `Humanise()`, my `lockOf`) pattern-matches the prose, and any rewording silently breaks the match or changes a replay hash.

Ask: return `(ok, code, why)`, where `code` is a stable kebab-case id and `why` stays for the log. Hash the code, not the prose. I have already named the 76 codes the Handover can produce, each with the engine text it matches today, in `locks` in `text.json` (`code`, `match`, `samples`). Suggested set, one per existing refusal:

`mission-over` `engine-error` `unknown-verb` `agent-down` `enforcer-inside` `agent-busy` `guard-down` `guard-not-at-door` `person-down` `person-away` `no-place` `unwalkable` `no-walk` `run-carrying-vale` `run-carrying-downed` `run-tethered` `only-scout` `only-fixer` `only-tech` `only-enforcer` `too-far` `needs-routine` `needs-need` `no-act-need` `need-met` `refusal-remembered` `already-turned` `no-cash` `driver-too-late` `turn-needs-approach` `vale-not-contact` `not-a-contact` `car-gone` `car-busy` `call-too-late` `nothing-to-call` `loop-wrong-target` `loop-holds` `stand-at-cabinet` `patrol-down` `patrol-away` `no-rivals` `stand-near-rivals` `vale-not-inside` `stand-at-door` `vale-not-at-car` `stand-beside-vale` `no-vale-yet` `stand-at-getaway` `car-held` `no-weapon` `already-drawn` `not-drawn` `stance-unknown` `panic-not-cell` `panic-no-fight` `panic-nobody` `fire-running` `fire-carrying` `no-target` `fire-on-cell` `target-unknown` `civilian-target` `out-of-range` `no-clear-line` `carry-vale` `already-carrying` `carry-names-downed` `carry-self` `not-down` `already-carried` `nothing-to-cancel` `keeps-vale` `holstering` `stand-beside-op`

Also: `needs-*` are today one string built from `Describe(c)`; give each condition kind its own code (`needs-routine`, `needs-need`, `no-act-need`, `need-met`, `refusal-remembered`, `already-turned`, `no-cash`), and carry the numbers a line needs (`m` for `too-far`, the worst-case `usd` for `no-cash`) as fields, not prose. `call-not-now` (the host's own wording) can become the engine's `guard-not-at-door` / `car-busy` / `call-too-late`. When the codes exist, `text.json` keeps `locks[].code` and `line`, and `match` goes; `text.test.ts` then checks the engine's code list against `locks` directly.

## Update: training hints, run toast, going-loud loss line

In `text.json`: `hints` (the layer's rules, and `items` H1–H11), `toasts`, `goingLoud`.

- **Hints.** Each item has `priority`, `agent`, `verb`, `target`, `line` (all ≤ 110 characters, most 66–87), and Motive's `trigger`, `rings` and `goneWhen` copied word for word from `motive-fights.md` §8 (the test compares them with that file, so a change there fails the test and says which hint). Latch implements the trigger text; the idle gate, once-per-run and priority are in `hints.rules`.
- **Rules held.** Every line names an agent, a verb and a target, introduces only its own verb, and names no approach (Ask, Pay, Help, Pressure), time, digit or sequence word. H3 and H9 say "turn", not how. H7 names the Tech's Reroute and the N key, not the gap. H10 is a question. H8 says "false tip", not when.
- **One deviation.** H11 says "Another agent can Carry them", so its `agent` is `any`: any of the four can carry. Say so if it should name one.
- **Toast.** `toasts.run-seen`: "Security saw someone run and is looking into it (Alert 1)." (first `run.seen`, shown on the next order).
- **Going-loud card.** `goingLoud.captured`: "Anyone downed is captured unless carried."
- 50 tests green, typecheck clean.

## Update: engine refusal codes, H11, the crowd

- **Locks are keyed by code.** `locks` in `text.json` is now a record keyed by the engine's refusal code, each `{ line, verbs }`; `match` and `samples` are gone. Fill a line with `fillLock(text, answer, { who, op, clock })`: `{who}` and `{op}` come from the order, `{m}` is `Answer.m` (or `cm / 100`), `{usd}` is `Answer.usd`, `{tick}` is `Answer.tick` as a mission-clock m:ss. Numbers now ride in the lines: "Too far from Inez. Walk within 4 m first.", "Not enough cash for the worst case ($90,000).", "Scout is busy until 3:03.", "Too late to turn the driver: his last turn was 5:40."
- **Eight codes are new to my list** (Keel's): `needs-temperament`, and `needs-tier`, `needs-reach`, `needs-need-is`, `needs-standing`, `needs-fit`, `needs-scale`, `needs-trail` for rulebook needs of other kinds. All have lines. I dropped `call-not-now`: it was the host's wording, not an engine code; the host's own `Locked()` reasons should become the engine's `guard-not-at-door`, `car-busy` or `call-too-late`.
- **Tests check against the engine.** `text.json` locks must equal `Answer.Codes` (read from `Model.cs`) and `suborned-sim codes` (the test runs the built CLI when `dotnet` is present; `DOTNET_ROOT` defaults to `/opt/homebrew/opt/dotnet/libexec`). Every `No("code", …)` in the engine must be in the list. A line must use exactly the numbers the engine sends with its code: a line that shows `{tick}` where none is sent fails, and so does an ignored number.
- **H11** now reads "A downed agent left behind is captured. Select another agent and right click them to carry them." (no key letter). A test now checks every key a line names (`N`, `F`, `Ctrl`, `Return`) against `HOW-TO-PLAY.md` and fails on any other, which would have caught the `(C)` (cancel).
- **The crowd.** `crowd-state` (speaker: a passer-by; `outcome` is startled, fleeing, sheltered, returning, or `back` when `crowd.back` logs) and `saw-gunfire` (a witness who remembers a shot: a passer-by, Inez or the guard). The host should speak one passer-by near the cell per change, not the whole crowd. The people cast by role do not scatter, so they have no crowd lines. 323 entries, 694 lines; 54 tests green, typecheck clean.

## Update: scenes for the update feed, and Inez and the door guard (canon)

**Correction applied.** Inez and the door guard are lovers, per the Story spine, and never were parked. The "affair nowhere" test and every `canon` hedge are gone. It is written plainly and with tact (a secret worth keeping, nothing explicit): she is in debt (her need: Help clears it) and the guard is her secret (Pressure threatens to expose it). The cell learns it by Listening at the café; he takes his café stop to see her; when the turned Inez calls him over, he leaves his door for her, and the coffee spill is the cover.

**`scenes`** in `text.json` (`rules` + `items`; schema `Scene`, reference `lookupScene`, `playScene`, `scenesFor`):
- **Overheard** (Watch, Listen): one scene per role (Watch) and per role × temperament (Listen), 2 variants, 4–6 beats. Listen on Inez is always the lovers' exchange, in all four temperaments and both variants.
- **Approach**: one scene per rulebook reply cell for Inez, the driver and the guard (73 scenes, 2 variants each): the Fixer arrives, the contact opens, the Fixer pitches, the contact replies, the Fixer reacts. The arrive, reply and done beats are `ref`s into `dialogue`, so a scene extends the exchange and cannot contradict it.
- **Call**: Inez calls the guard over and he leaves his post, with the spill as cover; the driver parks across the pickup camera and the Tech says so. Four voices each.
- **City**: the rival car arriving (in sight only), the chime (heard anywhere, and a second scene seen only within sight of the Office), the guard's café stop (in sight only).
- **Result line** per scene, with `{who}` `{need}` `{temperament}` `{usd}` `{alert}` filled from engine state.

Sample, Listen on Inez (Fearful):
```
Guard     I'll come by after my shift.
Inez      Please don't. The lender's man is round.
Guard     He frightens you?
Inez      Everything does lately. Come later. Quietly.
Guard     Then by the back way.
=> Learned: Inez is in debt · Fearful · Secret: she's seeing the door guard.
```
Sample, the call (Fearful Inez):
```
Fixer     Making the call.
Inez      Okay. Okay. I'll call him. Be quick.
Inez      Please, come over. Quickly. Quietly.
Guard     Inez calling me over? Five minutes.
Inez      Oh! Oh no, I'm sorry!
=> Inez called the guard over. He has left his door for her.
```

**Existing text changed for the canon.** The briefing's front-door hint ("the guard takes his coffee at Inez's café, and she is close to him") and pickup hint (no longer a plan: "Vale rides from the official bay, and his driver is short of cash"); the target lines for Inez and the guard; the action cards for Listen, Pressure and Call on Inez; the Scout's Listen read-outs for Inez (now name the guard); Inez's Pressure replies (about being told on, not the debt); her confirm lines when called; the guard's line when she calls him; and new `questioned` lines (the debrief hooks: a pressured Inez may identify the Fixer, a helped one lies; the helped warning is `fight-warned`).

**For the engine (Keel/Motive).** The text assumes Pressure on Inez threatens the secret and that the call's window is the guard leaving for her. Neither is text's to decide: if the engine models the secret (a need of kind `secret` on Inez), the Listen result line and the Pressure scenes already read right. Nothing in the text carries a mechanic. 329 dialogue entries, 706 lines, 102 scenes (73 approach, 17 overheard, 8 call, 4 city; two variants each); 62 tests green, typecheck clean.
