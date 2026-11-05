# 096 · Fable's words for the lend round: the book, the slim feed, the two Go's, the police countdown, the rival crew on foot

Fable, 2026-11-05. One words pass over what the 096 round left as placeholders: Latch's lists (`briefs/096/latch/new-words.md`,
`new-words-lend.md`), Keel's PLACEHOLDER lines for engines 0.40.0–0.42.0 (`briefs/096/keel/report-*.md`), the start page, the stale lines.
Most of it is in `contracts/missions/handover/text.json`. Three of Latch's lines that were still hard-coded in C# now read from it.

## Checks
- `bun test contracts/missions/handover`: 200 pass, 0 fail.
- `bun run hostlogic`: 121 pass, 0 fail. That is 120 plus Latch's new hurt-card test from 65d8bbb7.
- `bun run typecheck`: clean.
- `bun tools/game/words.ts`: the schema, text.test.ts, the proofs' words and the name check all pass. It also copied text.json into StreamingAssets/sim.
- `bun test tools/names`: 18 pass.

## Glossary decisions (they apply everywhere below)
- **"Popovic is out"** now means only the lend's win (`vale.out`): the Enforcer has driven him off in his own car. When he walks out of the Office,
  he **comes out**: `cards.items.vale.exits.beat` "Popovic is out." → "Popovic comes out.", `book.feed.cards.target-out.title` "Popovic is out" → "Popovic comes out",
  `briefing.timeline[5].short` "Popovic out" → "Popovic exits". The old words would have used the same title for two cards with opposite meanings.
- **Clear** is the word for getting away in a car before the police close in ("get the rest clear", "Everyone is clear", "Gets the rest clear").
- **Boxed in** means a car stands across a way out (box-in). **Blocked** means holders stop Go. Never "held": `book.heldGo.heldBy` "held by" → "blocked by", as the GDD glossary has it.
- **Mislead** is the Scout's word to one rival on foot (Motive's word, Sky's call 2 on the rivals page). "False tip" and "Tip" are gone from every line the player sees.
- **Captured** is the word for those left behind (as in the Go card's "would be captured"), never "taken".
- The police countdown always fills in its own number. `book.police.head` "Police close in {time}" fills {time} from `Game.SplitPoliceAt`. No line says "60 s", so a 75 s countdown needs no word change.

## 1. The start page (the book's Job page)

| key | old | new | why |
|---|---|---|---|
| `book.start.shared` | Both end at our car: Popovic in · Go · the rest clear before the seal | Go gets Popovic out. If he leaves in his car, the rest follow in ours. | The lend: the Enforcer drives him off in his own car and the other three leave in ours. This is the longest line that fits the column (hostlogic measures it). The police are in the condition under it. |
| `book.job.needs.ways.*.left-safe` words | Anyone left outside walks out, if no fight or seal is on | Nobody left behind gets caught by the police | True for both cars: on the one-car way the left-behind walk out unless a fight or the seal is on; after the lend, anyone not in a car that has left is captured. |
| `left-safe` `known` | (none) | `vale:Car,Lent,Out` | **The tick at 0:00.** At the start nobody is left behind, so the check was honestly true and showed ticked. It now waits at "?" until Popovic is in a car. Data only (the `known` field), no code. hostlogic now checks it (`Book096.TheStartIsTheJobPage…`: left-safe is Unknown and not Met at 0:00). |
| `go` check (both ways) | `log:success` | `log:vale.out \| log:success` | Under the lend the win is `vale.out`, the Go on Popovic's car. `success` comes only at the end. |
| pickup `has-vale` check | `vale:WithEnforcer,Car` | `vale:WithEnforcer,Car,Lent,Out` | The in-car Take puts Popovic in `ValeAt.Lent`, so the old check never ticked on the lend. |
| pickup `getaway` | Popovic reaches the getaway car (`vale:Car`) | Popovic is in our car, or in his with the Enforcer driving (`vale:Car,Lent,Out`) | Either car. |
| pickup `guarded` | While blocked, someone guards the car outside, gun drawn | (dropped from the pickup; kept on the front door) | `car-guarded` only reads our car with Popovic aboard, so on the lend it never ticked. The shared tail under the line is now Go and nobody caught. Both ways keep their own getaway line, and the front door also keeps its guard line. |
| pickup `driver` | The driver is turned | The driver is turned by {t:driverLast} | Glyph's proposal: the 5:20 deadline matters. |
| `book.start.cell.scout` | Watches from furthest; listens in. | Watches and listens; misleads the rival lookout. | The rival lookout is now the Scout's job. |
| `briefing.page.cell.scout` | …Gives the rivals a false tip. | …Misleads the rival lookout. | The old page's line, still the cell strip's fallback. |
| `briefing.page.ending` | Both ways end here: Popovic in the getaway car as it leaves. | Go gets Popovic out, in the getaway car or in his own. | Read by PlayText; nothing shows it now. |

The `book.job.needs.rule` note records the change.

## 2. The slim feed's crucial cards (`book.feed.cards`)

| key | old | new |
|---|---|---|
| `vale-out` line | rest clear by {police} | get the rest clear by {police} |
| `lent` line | Go when the way is clear | Go once the way out is clear |
| `lookout` line | watching Popovic's car | follows Popovic's car if it goes (the cost of ignoring him: route B fails) |
| `boxed` line | rival car across the way | their car is across our way out |
| `for-car` title | Rivals for our car | Rivals going for our car |
| `target-out` title | Popovic is out | Popovic comes out (see the glossary) |
| `hurt-carry` (Latch, 65d8bbb7, uncommitted words) | The Enforcer is badly hurt · {n} health, carrying {vale}: cover him | Enforcer badly hurt · {n} health, carrying Popovic: cover him |
| `hurt` | The Enforcer is badly hurt · {n} health left: get him into cover | Enforcer badly hurt · (line kept) |

The titles now keep to four words. `{vale}` tripped the name check; the host filled it with "Popovic" anyway.
Kept as Latch and Glyph had them: seal, police, down, alert, seen, rival-car, turned, refused, body; the journal, key and Menu words; `heldGo.fix`.

## 3. The two cars and the win (`book.cars`)

| key | old | new | why |
|---|---|---|---|
| `goLent` | Go · Popovic out | Go · drive him off | A ring label is an order, and the old one read like a status. |
| `goClear` (new) | (HudModel.cs: "Gets the rest clear") | Gets the rest clear | Moved out of code; the words are unchanged. |
| `lentName` | Popovic's car, ours now | Popovic's car, now ours | |
| `endWinLine` | Popovic is out in his own car. | We drove Popovic off in his own car. | Names who did it and finishes the story. |
| `carLost` | We left our car behind. The police will trace it. Trail +{trail}. | We left our car behind, and the police will trace it. Trail +{trail}. | |
| `lentSeen` | {by} saw Popovic's car leave: a lead, not our car. Trail +{trail}. | {by} saw Popovic's car leave: a lead, but not to our car. Trail +{trail}. | |
| `endWinHead` | Popovic is out | (kept) | |

## 4. The police countdown (`book.police`)
`head` "Police close in {time}" (kept: the number is filled in). `why` "Popovic is out. Get the rest clear." → "Popovic is out. The rest get clear in our car." (says how).
`left` "Still out: {list}" → "Still here: {list}" ("out" now means Popovic away). `clear` "Everyone is clear" and `end` "End now" kept.
`less.go.fate.stays` "{who} stays to get clear." → "{who} stays behind for now." (before vale.out nobody is captured yet). `less.go.why.police`, `.lent` and
`debrief.getaway.why.police` kept.

## 5. Bribes on camera, the parked car (`book.tape`, `book.parked`)
`tape.maybe` "A camera might see this: watch {camera} first" → "A camera might tape this: Watch {camera} to be sure". `parked.seen` "…would see it: reported after {s} s"
→ "Parked here, {who} would see it and report it in {s} s". `parked.maybe` "…might see it: watch first" → "…might see it: Watch first to be sure". `tape.seen`, `loopEnds` and `parked.back` kept.
Watch is capitalised as the verb's label, as in the hints.

## 6. The rival crew on foot (`book.crew`, `actions.mislead`, H8)
- `book.crew`: Latch's lines kept. They are plain and they fit. New keys `misled` "He takes the word and walks off. He'll be back." and `misleadMissed` "The word missed him:
  he'd gone, or was already after Popovic's car." (the engine's two misses, `mislead.missed`). Both were hard-coded in HostFeed.cs ("The word missed.").
- `actions.mislead`: label "False tip" → "Mislead"; does → "Send one of the rival crew off for a while. He'll know the Scout's face."; onTarget.rivals → "Send him off for a while.
  He comes back knowing the Scout's face."; withTarget "Give {target} a false tip" → "Mislead {target}"; then → "misleads {target}". `ring-mislead` "Tip" → "Mislead".
  `activity.acting.mislead` "Giving the rivals a false tip" → "Misleading a rival".
- `hints.items.H8`: target "the rival car" → "the rival lookout"; line → "Another crew's lookout watches Popovic's car. The Scout can mislead him."; short → "Mislead the rival [lookout]."
  **Left for Motive:** H8's `trigger` and `rings` (and `goneWhen`) are Motive's table word for word (briefs/011/motive-fights.md, and a test holds them to it). They still say "3:10 has
  passed (the rivals have parked)" and "the rival car". Latch's code rings the lookout and clears the hint once anyone speaks to the crew; the table should say the same.
- `cards.items.rivals.misled`: "Rivals sent away · They may be back." → "A rival walks off · He took the Scout's word. He'll be back, and he'll know her face." (the Scout is Larthi, she).
- `cards.items.rivals.back`: "The false tip wore off…" → "It has stopped across our way out again." (rivals.back is now the block's return).
- `targets.rivals.watched` → "Five in one car. It boxes ours in. Their lookout tails Popovic's car." (70 characters max).
- Dialogue: the Scout's Mislead ack, arrive and done lines now speak of one man ("On it. I'll send him somewhere else.", "I'm close to him now.", "He bought it. He's walking off."…);
  `rivals-misled` lines (rivals: "Fine. I'll go and look.", "Right. Back in a minute."; Scout: "He believed it. He's gone.", "It worked. He's walking off."); the Watch read on the rivals
  (Scout and Tech) no longer says their car follows Popovic's: "They'll box ours in", "Their lookout follows Popovic's car on foot". The `rivals-misled` trigger note is updated.
- The block: the `rivals-arrive` scene title "Rival car arrives" → "Rivals box our car in"; the Scout's two lines tightened ("They've parked right across our car. We're boxed in.",
  "Rival car across our way out. We can't drive off."); Keel's PLACEHOLDER note dropped from the trigger.

## 7. Keel's refusals (engines 0.40.0–0.42.0), `locks`
| code | old line · short | new line · short |
|---|---|---|
| tip-retired | The false tip is retired. Mislead one of the rival crew on foot. · Tip retired | Mislead works on one of their crew on foot, not on their car. · Not their car |
| only-scout | Only the Scout can listen or give a false tip. | Only the Scout can do this. Select the Scout. (as only-fixer, only-tech) |
| crew-no-need | The rival crew have no need to meet. Only Pay works on them. | He's here for the job. Only Pay works on the crew. |
| board-lent-full | Only one of us drives Popovic's car, and someone is at the wheel. · At the wheel | Someone is already at the wheel of Popovic's car. The rest leave in ours. · Seat taken |
| drive-lent | Popovic's car isn't driven round. Go takes it out. | Popovic's car can't be moved to a bay. Press Go to drive him off. |
| car-left | That car has already gone. · Gone | That car has already left. · Already left |
| car-disabled | …Walk him to ours. | Popovic's car is wrecked. The Enforcer carries him to ours. (only the Enforcer carries him) |
| not-out | End comes once Popovic is out. Withdraw calls it off. · Not yet | End works once Popovic is out. To call it off, Withdraw. · Popovic not out |
| agent-gone | They've left in a car. · Gone | {op} has already left in a car. · Already left ({op} is filled by LockLine) |
| crew-gone | He took the money and is gone. · Gone | He took the money and left. · Paid off |
| crew-fighting | He is in the fight now. No talking. | He's in the fight now. Talking is over. |
| crew-walking | He is on the move. Speak to him where he stands. | He's on the move. Wait until he stops. (the old fix contradicted itself) |
| refused-before | short Refused | short Refused before (as refusal-remembered) |
| car-boxed, drive-boxed | Boxed in: a car is across your way out. It must leave or burn first. | Boxed in: a car is across the way out. It has to drive off or be blown up. ("the way": also Popovic's car) |
| stand-boxed | Can't get in: a car is across that drive. · Can't get in | Can't park there: a car is across its way out. · Can't park there (the square's bay has no drive) |
| stand-at-car | (kept) | |
| `actions.end.does` | Popovic is out: end it now. Anyone not in a car that left is taken. | Popovic is out. End now: anyone not in a car that has left is captured. |

## 8. Cards for Keel's new log codes (`cards.items`, the Journal's rows)
These codes were "PLACEHOLDER, card pending (Fable)" in `silentLogs`. Each now has a card. Its `silentLogs` entry says "card only (cards.items, Fable 2026-11-05)" and names the
crucial card where one exists. The feed already holds its ten beat lines, so none of these is a beat: crucial ones are `icon` (the slim feed card, the ring or the plate says
it), the rest `alt` (Journal) or `debrief`.

| code | fate | title | result |
|---|---|---|---|
| rivals.block | icon | Our car is boxed in | The rival car stopped across our way out, and their crew got out. |
| car.unboxed | icon | Our way out is clear | Nothing stands across it now. |
| rivals.drives-off | alt | The rival car drives off | Their wheelman gave up and left. Our way out is clear. |
| rivals.lookout-posted | icon | Rival lookout posted | He watches Popovic's car. If it goes round the back, he follows it. |
| rivals.lookout-follows | alt | The lookout follows | He's going after Popovic's car on foot. Stop him before he reaches it. |
| lend.lent | icon | Popovic's car is ours | His driver walked off. The Enforcer is at the wheel, Popovic in the back. |
| vale.out | icon | Popovic is out | {agent} drove him off in his own car. Now get the rest clear. |
| split.police | icon | Police closing in | Anyone not in a car that has left when they arrive is captured. |
| lend.spring | alt | The rivals turn on our car | They saw Popovic's car go, and now they're coming for ours. |
| lent.seen | debrief | Popovic's car was seen | Security saw it leave. It's a lead for them, but it isn't our car. |
| car.leaves-split | alt | Our car leaves | Everyone aboard is clear. |
| car.lost | debrief | Our car was left behind | The police will trace it. |
| lend.goes-on | alt | The job goes on | Popovic is out, or his car can still go, so it isn't over. |
| security.gang | alt | Filed as a gang shooting | Nobody saw who fired. Police come, but seal nothing unless they tie it to us. |
| security.gang-claimed | alt | Tied to the shooting | The police know the cell was at the shooting. The seal comes next. |
| camera.record-lands | alt | Caught on camera | A camera taped the Fixer with {who}. Security has it now. |
| car.report-lands | alt | Our car was reported | It was seen parked where cars don't stand. Security will check it. |
| car.kerbside | alt | Popovic's car pulls in | Our car is on the service drive, so his stops at the kerb. Take him there. |
| car.boxed-in | alt | Popovic's car is boxed in | A car is across its way out, so his driver waits. |

`rivals.paid`, `rivals.refuses` and `rivals.listen.heard` stay silent. The Pay's or the Listen's own card already says it (`book.crew.paid`, `refused`, `heardMercenary`,
`heardLoyal`, through HostCrewTurn.CrewFooter), so a second card would only repeat it. The fills used are the ones TextCard knows: `{who}` (WhoIn) and `{agent}` (the first agent the
log names). split.police has no `{clock}`, because TextCard would fill it with now. The plate carries the time.

## 9. The boxed-in close (Keel's cause, engine 0.40.0)
`debrief.cause.closed-in.boxed.line` "{by} stood across the way out, and time ran out." → "{by} boxed the car in, and the district closed in." The failcam gains
`caption.closed-in.boxed` "{by} boxed us in." and `tip.closed-in.boxed` "Break the crew; their car leaves." (six words at most, filled). `less.closedIn.line` is
a strict object and debrief.cause replaces it once Latch wires it, so it gets no boxed line.

## Code (the smallest read-from-text changes; no logic touched)
- `HostBook.WordsLend.cs`: `GoClear`, read from `book.cars.goClear`. `HudModel.cs:453` reads it in place of "Gets the rest clear".
- `HostBook.WordsCrew.cs`: `CrewMisled`, `CrewMisleadMissed`, read from `book.crew.misled` and `misleadMissed`. `HostFeed.cs:203` (Mislead's result) reads them.
- `book.schema.ts`: `cars.goClear`, `crew.misled`, `crew.misleadMissed`, all optional.
- `tools/names/scan.ts`: `known` joins `check` in SKIP_KEYS. It holds the same check vocabulary (`vale:Car`) and never shows. Splice's tool: one word, flagged here.
- `tools/dialogue/conversations-page.ts`: "a 7:30 mission" → "an 8:10 mission".

## Tests changed (only where they pinned placeholder or retired words)
- text.test.ts: the false tip pins (`briefing.page.cell.scout`, the hint verb word, the Next Step lead `/^Mislead\b/`, the withTarget special case: Mislead is now a direct verb);
  the book's getaway order per way (front door: getaway, guarded, go, left-safe; pickup: getaway, go, left-safe; go reads either Go; left-safe waits at "?"; no "seal");
  Mislead, Withdraw, End in the capitalised-verb list; the failcam's closed-in causes add `boxed`; the card count 76 → 95; the timeline short "Popovic exits".
- hostlogic: `Book096.TheStartIsTheJobPage…` (the tail is go and left-safe, and left-safe is "?" at 0:00, never ticked) and `EndCard.CarGuardedAndLeftSafeAreLiveWordsOnBothWays` (the per-way order above).

## Left for others
- **Latch:** the host's fallback titles in `HostFeed.cs` FeedEvents (`rivals.misled` "Rivals sent away", `rivals.arrive` "Rival car arrives") and the fallback placeholders in
  `HostBook.WordsLend.cs` and `WordsCrew.cs` still have the old words. They show only when text.json lacks the block, so this is cosmetic. HudModel's other Alt does-lines
  ("Wins the mission", "Withdraws", …) are still in code.
- **Waymark:** `mission.json` timeline still has 2:46 "it drops three by the cell's car" and 3:10 "the rival car parks across from Popovic's car"; since 0.41.0 the car turns in at
  2:35 ±15 s and stops across our way out (~2:53). `briefing.timeline` must equal it (a test), so its labels "Three rivals take posts round our car" and "The rival car parks.
  Its lookout watches the official door" wait on that data. I'll word them once the marks move.
- **Motive:** H8's trigger, rings and goneWhen in briefs/011/motive-fights.md (see §6).
- **Splice:** the one-word SKIP_KEYS change in tools/names/scan.ts.

# Engine 0.43.0 words: G1's waiting Go, the crew's Watch, the timeline (Fable, 2026-11-05)

From Latch's "Engine 0.43.0, Motive's misses" (briefs/096/latch/new-words-lend.md) and Waymark's notes (briefs/096/waymark/timeline.md).

## 1. G1: a Go that waits for one of us climbing in (`book.cars`)

| key | was (placeholder) | now | why |
|---|---|---|---|
| `goOnce` | goes once {list} is in | waits for {list} to get in | Reads right with two climbing in ("waits for Tech, Scout to get in"; "is in" broke on two). Row: "Go · leaves Scout · waits for Tech to get in"; Alt: "Waits for Tech to get in". |
| `goWaiting` | Go · waiting for {list} | kept | Short and plain on the row. |
| `goWaitsRoster` | then the car goes | kept | Reads well after the arrow under his portrait. |

## 2. Log codes `go.waits` and `go.missed`

- `go.waits` stays silent: the Go row (`goWaiting`) and the roster (`goWaitsRoster`) already say it. The `silentLogs` entry says so.
- `go.missed` gets a card (`cards.items`), so a Go that waited and then didn't land no longer shows nothing:
  `{"kind":"warning","fate":"icon","title":"The car didn't go","result":"The one it waited for never got in, or the way is blocked. Go again."}`
  It's `icon` (the slim feed card), not a beat: the feed stays at eleven beat lines. No `{agent}` fill, because the log names the car and the refusal, not the climber.
  It covers both cars and both causes (he was shot down or called off, or a holder came). `silentLogs` says "card only".
- text.test.ts: the card count goes from 95 to 96.

## 3. Pay on one of the rival crew before a Watch (`locks.needs-routine`)

A new lock variant, `crew` (`text.schema.ts` `LOCK_VARIANTS`), and `needs-routine.byTarget.crew`:
line "Watch one of their crew first. One Watch covers them all.", short "Watch the crew first". The engine's `RivalsWatched` is one flag for the whole crew,
so one Watch on any of them unlocks the Pay. The base line ("Learn {who}'s routine first: Watch them.") stays for people.
Code: one line in `HostLess.cs` `TargetKindOf`: a crew member's id is "crew" (`IsCrew`), so `ShortOf` and `LockLine` pick the variant. No other lock has a `crew` variant, so nothing else changes.

## 4. Waymark's notes

- `briefing.timeline` 2:53: "Three rivals take posts round our car" / "Rivals post" → "The rival car boxes ours in; three post round it" / "Boxed in". Its `at` is unchanged, so mission.json and §07 still match. text.test.ts pins the shorts ('Boxed in').
- `hints.items.H8`: trigger "3:10 has passed (the rivals have parked)…" → "the rival lookout stands at his post watching Popovic's car, and no order has gone to their crew yet". Rings "the rival car" → "the rival lookout" (as Latch's HostHints rings him). goneWhen kept.
  text.test.ts ties H1–H11 word for word to Motive's table, so the H8 row in `briefs/011/motive-fights.md` follows (trigger, gist and rings), with a dated note under the table. **Motive:** please look over that row; it's your table.
- `conversations.rivals-coming.note`: "The 2:46 drop and the 3:10 park stay in sight" → the box-in at about 2:53 and the lookout at his post stay in sight.

## 5. Alt outcome `less.alt.outcome.worse`

"Works, but less well" → "Works, but costs more". HudModel shows it when a turn takes more than one order (GDD §04: a wrong approach still works "one step worse": one more Help, or a higher price). It doesn't work any less well, it costs more, so this says what the player pays.

## Checks
- `bun test contracts/missions/handover`: 200 pass, 0 fail.
- `bun test tools/missions/handover`: 38 pass, 0 fail.
- `bun run hostlogic`: 124 pass, 0 fail.
- `bun run typecheck`: exit 0.

## Left for others
- **Latch:** the C# fallbacks still have the old words: `HostBook.WordsLend.cs` `GoOnce` ("goes once {list} is in") and HudModel's "Works, but less well". They only show if text.json has no such key.
  HostHints H8 still gates on `190 * Game.Hz` (Waymark's note); the trigger text now says what the hint waits for: the lookout at his post.
- **Motive:** the H8 row in briefs/011/motive-fights.md (above).

# Conversations in full sentences, step 2 (Fable, 2026-11-05)

Sky read the second draft (`briefs/096/fable/conversations-sample.html`, `.json`): "conversations definitely improved"; on the Scout's last line in Listen, "Fables rewrite is better" (her plain report of what she heard). The Fixer, Hasti, is a woman.

## What changed
- **The rules, cutaway only** (as the sample page proposed). `conversations.schema.ts`: `CONVERSATION_MAX_BEATS` 10 → 16; new `CUTAWAY_MAX_WORDS` 25, `CUTAWAY_MAX_LINE` 160, `CUTAWAY_MAX_SENTENCES` 2, `CUTAWAY_MAX_QUESTIONS` 1; a cutaway line is `Said` (≤ 160 characters, ≤ 25 words); street lines stay `Spoken` (bubbles, 80 characters, 12 words in the test). Two sentences and one question a line are held by the scene checks (`tools/dialogue/conversations.ts` `check`) and `conversations.test.ts`, not the schema, so the two Help scenes still parse until they're rewritten (below). `text.schema.ts`, `text.test.ts`, `tools/dialogue/check.ts`: comments only; the 80-character, 12-word limits stay for everything said in play.
- **The scene checks** (`tools/dialogue/conversations.ts`): every spec's `maxBeats` is `CONVERSATION_MAX_BEATS`; a fault for > 25 words, > 160 characters, > 2 sentences, > 1 question, a line that teaches the game (`TUTORIAL`: "we could … or …", "you can now", "we can pressure her") and a line that pays at the Bureau or stamps anything (`PAYS_AT_BUREAU`). The sheets: the Fixer is "she" throughout; Milena has no debt of her own and is sorry for Inez; the Scout's last beat is a plain report of what she heard; Help hands Inez the money (nobody goes to the Bureau); Lukac's opens as a stranger with work, paid now, the amount on the card. `briefs/085/fable/writer.md` and `editor.md` carry the same limits.
- **The style loop** (`tools/style-agent`): guide `/3` with the cutaway's limits (picked from `conversations.schema.ts`, so they can't drift), FORM rewritten (clock held, up to 16 lines however long the act, one or two full sentences a line), VOICE gains "people react and hesitate, ask one thing at a time, each line answers what was just said", a new NATURAL rule, the evaluator told to read it aloud; briefs are 10–16 lines for every act (the seconds stay as context); the code checks flag NATURAL for > 2 sentences or > 1 question. The Pressure brief's Fixer is "she", and the call brief's spilled coffee is now her reason. Guide `/2` still builds exactly as before for the brief-054 runs: all 24 committed runs in `runs/style-agent` still verify (`bun tools/style-agent/cli.ts verify <run>`), and a test holds one of them.
- **text.json `scenes.conversations`**: listen-inez, pressure-inez, pay-driver, call-inez and call-inez-pressure are the sample's second draft word for word, view fields included (held moments re-sited to the new line numbers), cards unchanged, each `awaiting-sky` (Sky read them on the page and agrees them in play). call-inez-pressure's street bubbles are unchanged. The `rule` line says the new limits and what the status means.
- **No Bureau counter anywhere**: the fearful Help lines in `scenes.items` (the bubbles and the fallback) said "I can clear your debt with the Bureau", "Your debt's cleared at the Bureau", "I've settled what you owe the Bureau": now "Here's money toward what you owe the Bureau.", "Here's all you owe the Bureau. It's yours.", "This is for what you owe the Bureau. No strings." and the like. A test scans every line in a cell member's, Inez's or Lukac's mouth.

## Waiting for engine 0.44.0: the two Help scenes
help-inez-1 and help-inez-2 are untouched: Sky's option 1 changes the rule (a Help before the Scout's Listen is a small offer she refuses, no money paid; after the Listen, Help clears the whole loan at $40,000 and turns her first time), and Keel is building it as engine 0.44.0 in a worktree. After it merges Fable writes the two scenes (the refused small offer; the full offer that lands) and empties `AFTER_044` in `conversations.test.ts`. Until then the two still have the old short lines, and three of them still say the cell paid at the Bureau ("I've just paid part of your Bureau loan", "It's stamped at the Bureau now", "That's the rest paid. You owe the Bureau nothing"). The test lists exactly those three, so none can be added and their removal shows. Their specs' sheets already say the money goes to her.

## GDD text for Aster
- **§10 conversation scene**, after "held as long as it takes to read;": "a line is one or two full sentences (at most 25 words, 160 characters and one question), up to 16 lines however long the act, since the clock is held; nobody explains the game in a line: what was learned and what's open is the cards' job after the cut;"
- **§14 Fable, hold**: "… bubbles ≤ 12 words;" → "… bubbles ≤ 12 words (the street, clock running); a cutaway conversation's lines are full sentences, ≤ 25 words or two sentences, ≤ 16 lines, and no line in a mouth teaches the game;"
- **§14 Fable, delivers**: "… Motive reviews; Latch builds them in." → "… Motive reviews them for fit and for naturalness (whole sentences, one question at a time, nothing a real person in that spot wouldn't say aloud); Latch builds them in."
- **§14 Motive** (if its block names Fable's scoring): add "scores a conversation for naturalness as well as fit".

## The cutaway's line box (for Latch and Glyph; no Unity edit made)
`HudToolkit.Convo.cs`: the line box is bottom-anchored, `min(66 % of width, 1180)` wide in the 1920×1080 reference panel (match height), the face 136 px + 24 px beside it, so about 1020 px of text at 36 px Barlow Medium; the words are one label each in a wrapping row, so a long line wraps word by word and grows upward, inside the shade that starts at 62 % of the height. `HostConvo.cs` holds each line `len / 16` s (floor 1 s). Today's longest lines are 110 characters (pay-driver lines 1 and 16): two rows, about 6.9 s. A full 160-character line would be about three rows at 1080p (about 10 s), and its top still sits inside the shade. **No layout change is needed.** Latch: one capture of pay-driver's first line in look C, and Glyph to look at it, is enough to confirm.

## Checks
- `bun test contracts/missions/handover`: 203 pass, 0 fail.
- `bun test --timeout=60000 tools/style-agent tools/dialogue`: 91 pass, 0 fail. With bun's default 5 s timeout, `dialogue.test.ts`'s `beforeAll` times out when run beside the others; `bun run test` uses 60 s.
- `bun run hostlogic`: 124 pass, 0 fail.
- `bun run typecheck`: exit 0.
- `bun tools/game/words.ts`: schema, text.test.ts, the proofs' words and the name check pass; text.json copied into StreamingAssets/sim.

# Engine 0.44.0 words: the small offer and the full offer (Fable, 2026-11-05)

Engine 0.44.0 (merged at b1087f6e; Motive `briefs/096/motive/help-fearful.md`, Keel `briefs/096/keel/report-help-fearful.md`): before the Scout's Listen a Help is a small offer everyone refuses ($0, 35 s, +1 T, a remembered refusal); after it a Fearful Inez takes the full offer once ($40,000; $50,000 after a refusal) and is turned.

## The conversations (`scenes.conversations`, all `awaiting-sky`)
- **help-inez-1, "Help Inez, a small offer"** (`outcome: small-offer, helped: false`). The Fixer has only the briefing's word, so she offers "a little money toward what you owe them" and can't say how much. Inez: a month or two, then "I'd owe you as well as the Bureau"; "Please keep your money, and don't come to my tables again"; "Go now, please, before anyone asks me who you are." No money changes hands. 14 lines. Cards: *Too little. Inez refused it.* · *She knows the Fixer's face.* → *Listen first: learn what she owes*.
- **help-inez-2, "Help Inez, the full offer"** (`outcome: turned, helped: false`). The Fixer knows what she owes and brings enough to clear all of it, handed to her ("It's yours, Miss Nikolaou, and you needn't pay a penny of it back"). The ask (when I telephone, call Arthur over from his door), her fright at Arthur, what happens to him, her reason aloud (something that can't go down a telephone), "not another loan I'll owe you for later?", her offer to warn of trouble coming. Nobody pays at the Bureau or stamps anything. 14 lines. Cards: *Inez took it. She's with us.* → *Call Inez*.
- **help-inez-2-known, "Help Inez again, the full offer"** (`outcome: turned, helped: true`): new. `helped` is the host's existing flag (an earlier `turn.help.*` log on her, HostConvo.Lines.cs `ConvoQuery`), so a refused small offer already sets it: no new machinery. She opens "You again. I told you before, I won't take a little money from a stranger."; from "A small thing" on it is help-inez-2 word for word. 15 lines.
- A second small offer (helped true) has no conversation and plays the bubbles below ("I've never seen you before" would be false).
- Leans: help-inez-1 line 10 *Today*; help-inez-2 line 1 *all*, line 5 *Arthur*; help-inez-2-known line 2 *all*, line 6 *Arthur*.
- Source for the page: `briefs/096/fable/conversations-sample.json` `help044` (the test holds text.json to it word for word); `conversations-sample.html` shows each beside the second draft Sky read, with a box at the top.

## `turn.help.small-offer` (bubbles, feed, card)
- `dialogue`: `reply/help/small-offer` for the waitress, the driver (Lukac) and the guard, **one line set a role with no temperament**: everyone refuses a small offer, so the words must not show a temperament either (Motive H1). `done/turn/small-offer` for the Fixer ("Too little, then. I'll come back with more." and two more).
- `scenes.items`: `approach/help/<role>/small-offer` for the three roles, no temperament, appended at the end of the list (the style loop pins scenes by index). Result (the feed line): "{who} refused a small offer, and will remember it. Listen first."
- The card's mark in words: `less.alt.outcome.small-offer` "Refuses a small offer: Listen first". If the card needs a refusal of ≤ 4 words (§14 hold), "Too small: Listen first".
- `silentLogs.turn.help.small-offer`: PLACEHOLDER gone; says where each word lives. `events.done` lists small-offer. text.schema `OUTCOMES` gains `small-offer` (the plate and gain words were already Keel's).
- The debrief line `small-offer` (rulebook debrief.lines) has no words in text.json, as `refused` and `not-yet` have none: nothing to word here.

## Tests changed
- `conversations.test.ts`: the three Bureau lines and the `AFTER_044` allowance are gone (no scene may say it now; every scene passes `check` with no exceptions); the three Help scenes are held to the sample word for word; routing (`small-offer` → help-inez-1, turned → help-inez-2, turned after a Help → help-inez-2-known; a second small offer and a Fearful not-yet play no conversation); the old "two visits" test now checks the rulebook's small offer ($0) and the one-visit full offer through the reference evaluator. AGREED is empty: help-inez-2 was the last agreed scene, and it is rewritten.
- `text.test.ts` (no new lines): `replyCells` adds `help/undefined/small-offer` when the told-only Help is a small offer; the reply and scene coverage accept that temperament-free cell; done turn rows 5 → 6.
- `tools/dialogue`: the city tables don't carry the small offer yet, so `fixture.ts` `handoverOnly` keeps it out of the generator's reference (D-12, compareToReference); the waitress set in dialogue.test.ts skips it; seven scene sheets with Inez, not six. Sheets in `conversations.ts` rewritten for the three scenes.
- `tools/style-agent/briefs.ts`: not mine to break, but it was already red: Keel's 0.44.0 took two scenes out of `scenes.items` and the chain's pinned indices moved. The chain now keeps the archived index when it still fits and otherwise finds the same scene by its key, so all 24 committed runs still verify.
- `tools/game/hostlogic/HostLogic.cs`: TheHelpRoutePlaysEachScene expects listen-inez, help-inez-2, call-inez to 1600 (route-a's call is at 1556 now); the held-moments test reads step 2's line numbers; OtherTemperaments checks the three Help picks.

## Checks
- `bun test contracts/missions/handover`: 203 pass, 0 fail.
- `bun test --timeout=60000 tools/style-agent tools/dialogue`: 91 pass, 0 fail. `bun tools/style-agent/cli.ts verify` on all 24 runs: ok.
- `bun run typecheck`: exit 0.
- `bun tools/game/words.ts`: pass; text.json copied into StreamingAssets/sim.
- `bun run hostlogic`: 123 pass, 1 fail: TheHelpRoutePlaysEachScene. Unity's installed engine is still 0.43.0 (`unity/Assets/Suborned/Sim/Plugins/Suborned.Sim.dll`, and StreamingAssets/sim/rulebook.json has no small-offer), so Help on route A still lands not-yet. It was red before this pass too (it expected two Help scenes). It turns green once Latch installs 0.44.0.

## Left for others
- **Latch**: install engine 0.44.0 into Unity (DLL + rulebook), then hostlogic's route test; Proof067 (r067-convo-inez-1/-2) and Proof085Scenes (r085help1/2) still expect two Help visits and help-inez-1 as the first visit's scene; HudModel.OutcomeWords for the small-offer mark (`less.alt.outcome.small-offer`).
- **Glyph**: the refusal mark on the Help card before the Listen.
- **Motive**: help-inez-1 is written for a Fearful Inez (the schema requires a temperament), and her caution shows before the Listen. It reads as anyone's caution with a stranger who knows her debt, and the bubbles (reply, done, scene) are the same for every temperament; say if it should be flatter.
- **Fable (later)**: the small offer in the city's dialogue tables (tools/dialogue `requiredCells`, every group), then drop `handoverOnly`.

# Engine 0.45.0 words: Help on Lukac (Fable, 2026-11-05)

Keel's 0.45.0 (merged at 2d891114, `briefs/096/keel/report-lukac-help.md` "For Fable"): after his Listen a Mercenary Lukac takes Help in one visit ($50,000, 35 s, +1 T) and is turned; after a refused small offer it's $60,000, +2 T.

## The conversations (`scenes.conversations`, both `awaiting-sky`, after pay-driver)
- **help-driver, "Help the driver"** (`mercenary, outcome: turned, helped: false`). The Fixer knows his wages are late and it's been a lean month; she brings enough to see him through until they come, his before she asks anything. He distrusts money that asks for nothing ("costs the most in the end") and asks what it's for; the ask is pay-driver's job (at the chime, round to the service bay, a couple of minutes, then he drives on); he wants neither "we" nor the yard explained. Ends on his reaction: "That'll see me to payday, and the garage off my back", and he'll send anyone hanging about the bay off. 14 lines. Cards: *Lukac took it.* · *Lukac waits a couple of minutes.* → *Be at the service bay*.
- **help-driver-known, "Help the driver again"** (`helped: true`, set by the refused small offer's `turn.help.small-offer` log, as help-inez-2-known). Opens "You again. If it's more of that small change, my answer's the same."; three more lines meet the refusal; from "Mine, just like that?" on it is help-driver word for word. 15 lines.
- No amount spoken, no lean fields. His small offer before the Listen keeps the street bubbles (no conversation), as Inez's second small offer does.
- Source for the page: `briefs/096/fable/conversations-sample.json` `help045` (the test holds text.json to it word for word); `conversations-sample.html` has scenes 5b and 5c, two table rows and a box at the top; pay-driver's stale note ("Help on Lukac has no scene of its own yet") now points at them.

## Tests and sheets
- `conversations.test.ts`: `DRIVER` held to the sample; AWAITING gains both; routing (`help`/driver/mercenary/turned → help-driver, helped → help-driver-known; small offer either way and other temperaments → none); the one-visit check and a no-amount check (`$`, digits, thousand, hundred) on both; Help on the driver still has its bubbles scene.
- `tools/dialogue/conversations.ts`: two specs (sheets for the generator; the test requires one per scene). Both pass `check` with no faults.

## Checks
- `bun test contracts/missions/handover`: 203 pass, 0 fail.
- `bun run hostlogic`: 128 pass, 0 fail.
- `bun run typecheck`: exit 0.

## Left for others
- **Latch**: `bun tools/game/words.ts` to copy text.json into StreamingAssets/sim (not run here: checks were limited to the three above). The host's lookup is data-driven (HostConvo.Lines.cs ConvoQuery), so no code change is expected; a proof of Help on the driver after his Listen would show help-driver.
- **Motive**: read Lukac's distrust of a gift for fit with a Mercenary taking Help in one visit.
