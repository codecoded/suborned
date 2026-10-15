# The Handover's words (Fable, brief 011)

Data: `contracts/missions/handover/text.json` (`suborned.text/1`), schema and lookups in `text.schema.ts`, checker in `text.test.ts`. Build-time only. 285 entries, 612 spoken lines, 76 refusal codes, 24 actions, 42 events.

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
2. **Inez's secret** (the affair) is parked and appears nowhere. Her need in every line is the debt.
3. **The Fixer is not gendered** in any line (the Story spine calls her a woman); say if the Fixer should be.
4. **Keel**: refusals are free text and the replay hash covers them (`Orders` hashes `why`). A stable code beside the text would let `lockOf` retire its patterns; the patterns work until then. The `cancel` order is in Keel's working tree, uncommitted: if its refusals change, `text.test.ts` fails and says which.
5. **Motive** reviews the temperament voices (the §14 handoff); **Chime** voices the contact confirmations (`confirm`); **Glyph** lays out the card, the panel rows and the collapsible beats.
6. Not done: Hero and Menace band variants of the replies (the cells exist in the rulebook, the voice is the neutral one), the investigator's questioning lines, and the debrief lines for between-missions changes (`rulebook.debrief.lines`: turned-how, kept, informer, and so on), which belong to the debrief text, not the Handover's.

`text.json` is the source from here on: edit it directly, then `bun test contracts/missions/handover/text.test.ts`. (I assembled it from scratch scripts in `out/fable/`, which are not shipped.)
