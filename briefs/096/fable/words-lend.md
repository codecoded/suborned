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
