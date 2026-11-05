# Brief 096 · new words for Fable: the lend and the rules round (Latch, 2026-11-05)

Engine 0.42.0 (the lend), 0.41.0 (rivals' block, lookout) and 0.40.0 (bribes on camera, box-in, parked car), wired in the host. Every word below is my
placeholder: change them freely. The code reads them only from these keys. The `book.*` blocks are optional in `book.schema.ts`, and a text without
them shows the same placeholder from `HostBook.WordsLend.cs`. Keel's own PLACEHOLDER lines (`locks.board-lent-full`, `drive-lent`, `car-left`,
`car-disabled`, `not-out`, `agent-gone`, `silentLogs.lend.*`, `vale.out`, `rivals.*`, …) are untouched and still yours.

Plain English, the real cause, not too formal (Sky). Titles four words or fewer, a card line 40 characters at most (schema).

## Crucial cards (`book.feed.cards`)

| Key | Placeholder | Where it shows |
|---|---|---|
| `vale-out` title | Popovic is out | slim feed, the top card (above police): Popovic's car has gone with him, so the win is fixed |
| `vale-out` line | rest clear by {police} | under it; `{police}` is the clock when the police close in (`Game.SplitPoliceAt`) |
| `lent` title | Popovic's car is ours | the in-car Take lent the car (`lend.lent`), ranked with "Popovic is out" of the Office |
| `lent` line | Go when the way is clear | 〃 |
| `lookout` title | Rival lookout posted | `rivals.lookout-posted`, ranked with "Rivals for our car"; it leaves on its own once he's down or paid |
| `lookout` line | watching Popovic's car | 〃 |
| `boxed` title | Our car boxed in | `rivals.block` (or `rivals.back`), ranked with "Rivals for our car"; it leaves on its own once nothing stands across our way out |
| `boxed` line | rival car across the way | 〃 |

## The two cars and the win (`book.cars`)

| Key | Placeholder | Where it shows |
|---|---|---|
| `goLent` | Go · Popovic out | the Go on Popovic's car (its ring, panel and pill) once it's ours |
| `lentName` | Popovic's car, ours now | the car's name (hover, panel header, walk line) from the lend; ours stays `names.targets.car-getaway` ("the getaway car") |
| `endWinHead` | Popovic is out | the end card's head after a win by the lent car's Go (was "Popovic is in the car") |
| `endWinLine` | Popovic is out in his own car. | the end card's line in the same case (was `less.end.win`) |
| `carLost` | We left our car behind. The police will trace it. Trail +{trail}. | debrief: our car still standing at the end (the engine's car-seen "left"). Before this it read "left saw the car leave" |
| `lentSeen` | {by} saw Popovic's car leave: a lead, not our car. Trail +{trail}. | debrief: security saw the lent car go (`lent-seen`); `{by}` from `debrief.getaway.by` |

## The police countdown (`book.police`)

One plate under the clock, in the seal's place, from vale.out to the end (it already counts the seal and the close). The seal plate never shows at the same time.

| Key | Placeholder | Where it shows |
|---|---|---|
| `head` | Police close in {time} | the plate's big line; `{time}` m:ss left; amber in the last 30 s, like the seal's |
| `why` | Popovic is out. Get the rest clear. | the small line under it |
| `left` | Still out: {list} | the action line while anyone isn't in a car that has left; `{list}` the agents' names, `{n}` how many |
| `clear` | Everyone is clear | the action line once nobody is left |
| `end` | End now | the plate's button: ends the mission now (End, in Withdraw's place). Everyone not in a car that has left is captured |

## Bribes on camera (`book.tape`)

| Key | Placeholder | Where it shows |
|---|---|---|
| `seen` | {camera} would tape it: Alert {alert} in {s} s, +{trail} T | a Pay or Pressure card where a camera the cell has watched would record it: the red eye's hover and a panel row |
| `maybe` | A camera might see this: watch {camera} first | the same where the camera hasn't been watched (the "?" eye) |
| `loopEnds` | loop ends {t} | added after either when a loop on that camera runs out inside the act; Alt shows the clock too |

## The parked car (`book.parked`)

| Key | Placeholder | Where it shows |
|---|---|---|
| `seen` | Parked here, {who} would see it: reported after {s} s | the Drive card for a stand on private ground (the service drive) where a known watcher would see our car; eye hover and panel row |
| `maybe` | Parked here, {who} might see it: watch first | the same for a watcher the cell hasn't learned (the patrol before its Watch) |
| `back` | patrol back {t} | added while the patrol is rerouted |

## Small additions to existing blocks

| Key | Placeholder | Where it shows |
|---|---|---|
| `less.go.why.police` | police | the Go line's bracket after vale.out ("Go · leaves Scout (police)") |
| `less.go.why.lent` | stays on | spare: the engine's why for someone who stays on while Popovic is lent (shown only if a fate turns captured) |
| `less.go.fate.stays` | {who} stays to get clear. | the roster flag's hover before vale.out: the rest stay on, nobody is captured yet |
| `debrief.getaway.why.police` | the police closed in | the left-behind line's why when the countdown ran out |

## Words still in code, for you to move or keep

- `HudModel.cs`, the Go's Alt does-line: "Gets the rest clear" (beside the old hard-coded "Wins the mission" and "Withdraws"). Our car's Go once Popovic is lent.
- `HostFeed.cs`, Mislead's result line: "He takes the word and walks off. He'll be back." / "The word missed." (the false tip's lines went with the tip in 0.41.0).

## The rival crew on foot (`book.crew`, and the lookout card's chips), Latch 2026-11-05

Engine 0.41.0 (`rivals.turn`) wired in the host: Watch, Listen, Pay and Mislead on a member of the rival crew on foot (the lookout and the three posted
round our car), beside the takedowns and the body rows he already had. Code: `HostCrewTurn.cs`; words read in `HostBook.WordsCrew.cs`, which keeps the same
placeholders if the block is absent. The block is optional in `book.schema.ts`. A ring row's name is `actions.<verb>.withTarget` filled with the man
(`lookout` for the lookout, else `names.holders.rival`).

| Key | Placeholder | Where it shows |
|---|---|---|
| `lookout` | the lookout | the lookout's name on his rows ("Pay the lookout"), the feed and the footers |
| `timetable` | their timetable | Watch's outcome on a member (as on their car) |
| `temperament` | money or loyalty | Listen's outcome on a member |
| `watchDoes` | Learn the crew's timetable: where each of them goes, and when. | Watch's card line on a member |
| `listenDoes` | Hear whether he's in it for the money or loyal to his crew. | Listen's card line |
| `payDoes` | {usd} to walk away. A loyal man refuses and tells his crew. | Pay's card line before a Listen (`{usd}` the engine's $30,000) |
| `payDoesMercenary` | He's in it for the money: he takes {usd} and walks off for good. | Pay's card line, a known Mercenary |
| `payDoesLoyal` | He's loyal: he refuses, tells his crew about the Fixer, and we keep the money. | Pay's card line, a known Loyal man |
| `payUnknown` | takes it, or refuses and tells his crew: Listen first | Pay's card row (icon "?") before a Listen |
| `payTakes` | takes it and walks off for good | Pay's card row, a known Mercenary |
| `payRefuses` | refuses: his crew will know the Fixer | Pay's card row, a known Loyal man |
| `misleadDoesLookout` | Send him to watch the other door for a while. He comes back knowing the Scout's face. | Mislead's card line on the lookout |
| `misleadDoesPosted` | Send him to the lookout's post for a while. He comes back knowing the Scout's face. | Mislead's card line on a posted man |
| `back` | back at his post {t} | Mislead's card row: `{t}` the clock he'd be back (`Game.MisleadBackTick`) |
| `face` | he'll know the Scout's face | Mislead's card row |
| `sees` | He sees you coming and fires: come from behind | a Knock out or Kill card on a rival who would see it coming (the engine's `Aware` yes, armed) |
| `armed` | Armed: if he sees you coming, he fires | the same where the engine doesn't know (`Aware` ?) |
| `heardMercenary` | {who} is in it for the money. | the feed card's footer after a Listen on a Mercenary |
| `heardLoyal` | {who} is loyal to his crew. | the same, a Loyal man |
| `heardNothing` | Nothing learned. | a Listen that landed with nothing (he left) |
| `paid` | {who} takes the money and walks off. | the feed footer after a Pay a Mercenary took |
| `refused` | {who} won't take it, and tells his crew. The money stays with us. | the feed footer after a Loyal man's refusal |
| `payMissed` | {who} was gone before the Fixer paid. The money stays with us. | the feed footer when he left before the Pay landed |

The lookout card (`book.feed.cards.lookout`) now has three chips, each with an `agent` (new, optional in the schema; `fixes` may hold three now):
`Mislead` (scout), `Pay` (fixer), `Take` (enforcer). Each is a button: that agent is selected, the view goes to the lookout, and his ring opens with
that agent's ways on him; the card stays. Its line now pans to him, not to Popovic's car. Chips without an `agent` (the rivals-for-the-car card's) stay labels.

### Fable's lines that now read wrong (yours, not touched)

- `actions.mislead`: label "False tip", ring word "Tip", `does` "Send the rivals the wrong way for a while", `onTarget.rivals` "Whisper the rivals a bad
  address". The tip on their car is retired, so the Scout's word now goes to one man on foot. My crew rows use `book.crew` for the line, but the knob
  and ring word still say "False tip" / "Tip". Motive calls it "Mislead".
- `hints.items.H8`: "Another crew watches the pickup. The Scout can give them a false tip." H8 now rings the lookout at his post (not their car) and
  goes away once anyone speaks to the crew.
- `HostFeed.cs` (in code): the `rivals.misled` event card "Rivals sent away" still points at their car.

## Engine 0.43.0, Motive's misses (Latch, 2026-11-05)

Placeholders in `text.json` for Fable. The schema (`book.schema.ts`, `book.cars`) has the three new keys, all optional.

### `book.cars`: G1, a Go that waits for one of us climbing in

A Go on either car while an agent is climbing in (a Board under way) waits and lands the tick his Board lands (rulebook `car.go.waits`). The Go card already
counts him aboard (`LeftBehind`), the car marker offers Go while he climbs in, and the Go row shows the seconds until he is in (the plain time field, no words).

| key | placeholder | where |
|---|---|---|
| `goOnce` | goes once {list} is in | added after " · " to the Go row's line before the click, and capitalised as its Alt does-line. `{list}` the job words of those climbing in ("Tech") |
| `goWaiting` | Go · waiting for {list} | the Go row's line and Alt does-line once the Go is given, until he is in |
| `goWaitsRoster` | then the car goes | the climber's roster line under his portrait, after "→", while the Go waits for him |

### Engine log codes with no card yet (Keel's placeholders in `silentLogs`)

- `go.waits`: logged when the Go is given and waits. The Go row and the roster already show it, so it may stay silent.
- `go.missed`: the waiting Go didn't land (he was shot down or called off, or a holder came, and the car can't leave). The player clicked Go and nothing
  happened, so it probably needs a card in `cards.items`. I didn't add one: the card count in `text.test.ts` is yours to move, and the fate is your call
  with Glyph. A beat would take the feed past its eleven beats.

### Also now from the engine (no new words)

- **Pay on one of the crew needs a Watch first** (`rivals.turn.needs`). The row is locked `needs-routine` and uses `locks.needs-routine` as it stands
  ("Learn {who}'s routine first: Watch them."). You may want a line for the crew ("Watch the crew first").
- **The "+1 T if seen" on Popovic's car's Go** reads `less.go.seen` as our car's does, from `Game.LentCarWatcher()`.

### Old fallbacks in C# now match text.json

- `HostFeed.cs` FeedEvents: `rivals.misled` "A rival walks off", `car.leaves` "{vale}'s car leaves", `security.called` "Police are coming", `run.seen`
  "Seen running", `rivals.for-car` "Rivals moving on the car". `rivals.arrive` is gone: the engine stopped logging it in 0.41.0. The `rivals-arrive` scene's
  card title (it plays on `rivals.block` now) is "Our car is boxed in", from your `rivals.block` card.
- `HostBook.WordsLend.cs` and `HostBook.WordsCrew.cs`: every default is now `book.cars`, `police`, `tape`, `parked`, `crew` as written.
- `HudModel.OutcomeWords` (Alt) now reads `less.alt.outcome` (ok, worse, no, half, unk, learns-sight, learns-person, tethered, call-ready, call-not-now,
  wins, loud). The text's "Works, but less well" replaces the code's "Works, one step worse". Still in code because the text has no key for them yet:
  the Watch on a person ("Reads their mood"), Disable ("Cuts the camera for good; the desk notices"), Board ("In the car"), and the Go withdraw ("Withdraws").
