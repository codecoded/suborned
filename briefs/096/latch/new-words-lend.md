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
