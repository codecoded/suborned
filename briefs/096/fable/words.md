# 096 · Fable's words: the end card, Step out, the getaway in the book, the held-car card

Fable, 2026-11-04. Brief 096 objectives 3, 6 and 7, and the Step out word. All of it is mission data in
`contracts/missions/handover/text.json`. No time strings changed (Keel is moving the shift +40 s in another worktree).

Checks: `bun test contracts/missions/handover/` passes, 200 tests and 0 failures (6 of them new, for brief 096). `bun run typecheck` is clean.
`bun run test` gives 2557 pass and 30 fail. None of the 30 are about these files. They are world, kit, skyline, site and people hashes,
plus three lists that were already behind: proofwords is missing "District seals in" from commit 885bc9c3, HostFollow r29 has no row,
and the dialogue hash list is missing book.schema.ts and conversations.schema.ts.

## 1. The end card, by cause (objective 6)

Sky (2026-11-04): "the End card needs to be plain english and clear but not too formal". The card used to have one line per
fail code. Now it has one per **cause**: the engine's own reason for the fail.

**Key shape, for Latch:** `debrief.cause[Game.Cause.Fail][key] = { head, line, story? }`. `Cause.Fail` is the fail code, or `withdraw` on a withdrawal.
- `key` is `Game.Cause.Why`. There are two exceptions. `sealed` is keyed by `Cause.Car` (`held`, `ready` or `empty`, and `unknown` when the
  engine gave none). `withdraw` is keyed by its why (`withdrew`, `car-left-without-vale`).
- `head` is the card's title (at most 32 characters; the card wraps it to two lines at most). `line` is the sentence under it.
- `story` lists keys of `debrief.story`. Their lines follow "Popovic reached the getaway car." in the route, so the story runs to the end.
- Fills: `{by}` and `{agent}` work as in less.failcam. `{holders}` is names.holders through holdersPhrase over `Game.CarHolders()` at the
  end tick, e.g. "2 rivals". A story line with `{holders}` and no holders is left out.
- A cause with no entry (why `unresolved`) falls back to `less.end.fail` (the title) and `briefing.fails` (the line), as today.
- It lives in `debrief`, not `less`, because `less` bans gendered words and these lines need "he" and "him" for Popovic.

| code | cause (why) | engine site | head | line | story |
|---|---|---|---|---|---|
| rivals-reach-vale | beat-to-pickup | GameOrders.cs:789 | Rivals got to Popovic first | Their crew was watching his car and grabbed him before the Take. | |
| rivals-reach-vale | tail | GameCrew.cs:354 | Rivals got to his car first | Their driver walked up to Popovic's car and took him before the Take was done. | |
| rivals-reach-vale | downed-enforcer | GameCombat.cs:540 | The Enforcer went down | He was shot while carrying Popovic, and the rivals took him. | |
| rivals-reach-vale | reached-car (fight.unguarded) | GameFight.cs:117 | Rivals took Popovic from the car | Nobody stood guard outside with a gun drawn, so they walked up and took him. | blocked, unguarded |
| closed-in | on-foot | GameSeed.cs:109 | Time ran out on foot | Popovic was still on foot when the district closed in. | |
| closed-in | no-driver | GameSeed.cs:109 | Nobody left to drive | Popovic was aboard, but everyone in the car was down. | no-driver |
| closed-in | car-held | GameSeed.cs:109 | The car was blocked | {by} blocked the car, so Go wouldn't work, and time ran out. | blocked |
| closed-in | no-go | GameSeed.cs:109 | Nobody pressed Go | Popovic was aboard and the car was free, but nobody pressed Go in time. | no-go |
| sealed | Car held | Game.cs:466 | Sealed in, car blocked | {holders} blocked the car, so Go wouldn't work before the district sealed. | blocked |
| sealed | Car ready | Game.cs:466 | Sealed in, nobody pressed Go | Popovic was aboard and the car was free, but nobody pressed Go. | no-go |
| sealed | Car empty | Game.cs:466 | Sealed in before the car | Security sealed the district before Popovic was in the car. | |
| sealed | unknown | Game.cs:466 | District sealed | The getaway car hadn't left when security sealed the district. | |
| vale-leaves | driven-away | Game.cs:573 | Popovic got away | He got into his car and his driver drove him off, right on time. | |
| vale-leaves | too-late | Game.cs:473 | Out of time | Time ran out before we had Popovic, and he was driven off. | |
| vale-dead | shot | GameCombat.cs:576 | Popovic is dead | He was hit in the shooting. We needed him alive. | |
| vale-dead | killed | GameTakedown.cs:240 | Popovic is dead | {agent} killed him in a takedown. We needed him alive. | |
| enforcer-down | shot | GameCombat.cs:541 | The Enforcer went down | He was shot before Popovic was in the car, and only he can take him. | |
| cell-down | shot | GameUnguarded.cs:94 | The whole cell is down | Everyone was shot, so nobody was left to finish the job. | |
| driver-shot | shot | GameCombat.cs:586 | The driver was shot | Security heard it and pulled Popovic to safety. | |
| entry-seen-guard | seen | GameOrders.cs:758 | The guard saw the Enforcer | He was back at the door as the Enforcer went in, and warned Popovic. | |
| entry-seen-camera | seen | GameOrders.cs:759 | The camera saw the Enforcer | The entrance camera caught him going in, and the Office locked down. | |
| abduction-seen-camera | seen | GameOrders.cs:787 | The camera saw the Take | The pickup camera caught the Enforcer taking Popovic. Security got him back. | |
| getaway-lost | wrecked | GameCars.cs:112 | The getaway car is wrecked | It took too many hits to drive, so Popovic had no way out. | |
| engine-error | error | Game.cs:425 | This run doesn't count | Something broke on our side. Start again. | |
| withdraw | withdrew | FailCause.cs Withdrew | You withdrew | The cell pulled out. You can try again. | |
| withdraw | car-left-without-vale | GameGetaway.cs Leave | Left without Popovic | Go was pressed before Popovic was aboard, so the cell pulled out. | |

`entry-seen-patrol` (GameOrders.cs:762) is retired. It runs only under old pinned rulebooks, so it gets no card.

**The story after "Popovic reached the getaway car."** (`debrief.story`):
- `blocked`: "{holders} blocked the car, so Go wouldn't work." This is why Go was refused, e.g. "2 rivals blocked the car, so Go wouldn't work."
- `unguarded`: "Nobody stood guard outside it with a gun drawn, so the rivals took him."
- `no-go`: "Nobody pressed Go."
- `no-driver`: "Everyone aboard was down, so nobody could drive."

Sky's run (seed 1562486586) now reads: **Rivals took Popovic from the car** · "Nobody stood guard outside with a gun drawn, so
they walked up and took him." · route: "… Popovic reached the getaway car. 2 rivals blocked the car, so Go wouldn't work. Nobody stood
guard outside it with a gun drawn, so the rivals took him."

**Other changes from the same review (and why):**
- Glossary: the car is **blocked**, never "held". "Blocked" is the ring's own word (`locks.car-held`: "The car is blocked by {holders}").
  To match it I changed `less.sealed.line.held`, `less.sealed.story.held` ("{holders} blocked the car, so Go wouldn't work."),
  `less.sealed.story.ready` ("Nobody pressed Go."), `less.closedIn.line.car-held` and `.no-go`, and `less.failcam.caption.closed-in.car-held`.
  `less.sealed.line.ready` now says "pressed Go", not "gave Go". The card's new lines and these older ones say the same thing, and a test holds them together.
  Once Latch wires `debrief.cause`, it replaces `less.sealed.line`, `less.sealed.story` and `less.closedIn.line`.
- `less.failcam` was missing two causes the engine really gives. Captions: `rivals-reach-vale.tail` "{by} reached Popovic's car.", `vale-leaves.driven-away`
  "Popovic's car left on time." Tips: "Down or delay their driver first.", "Turn the driver to buy time." The failcam test's cause list
  now includes both.
- Lines that blamed the player: `less.failcam.caption.vale-leaves.too-late` "Popovic's car left. Too slow." → "Time ran out. Popovic left.".
  `briefing.fails.vale-leaves` "…We were too slow." → "Popovic left in his car before we had him.".
- `briefing.fails.rivals-reach-vale` (the code-level fallback): "The rival crew got to Popovic first." → "The rivals took Popovic before
  we got away." The old line was wrong for three of the four causes.
- `book.job.ends` row: "Rivals reach Popovic first" → "Rivals take Popovic". This covers all four causes.

## 2. Step out (the car ring's new way)

`actions.step-out`: label **Step out** · by all four · engine `move` · does "Get out and stand beside the car. Popovic stays in."
"Step out" is the GDD's own phrase (§06 Board: "steps out at the car's side"). It is plain, and it pairs with Board.
Schema: `actions` keys were locked to engine verbs, so `text.schema.ts` gains `HOST_WAYS = ['step-out']`: a way the host builds from an
engine verb. The host already reads `actions.step-out` (HostGetaway.CarStepOutRows), so nothing needs wiring.

## 3. The getaway in the book (objectives 3 and 7)

Under both ways, after "Popovic reaches the getaway car" (`book.job.needs`):
1. live: `go` "Go gets Popovic out, and the job's done", check `log:success` (the engine's Note at Leave with Popovic aboard).
2. waiting for a check word (`book.job.needs.pending`; the host reads none of it, and ProofBook holds every live check to the vocabulary):
   - `guarded` "While blocked, someone guards the car outside, gun drawn". It goes after `getaway`. **Check for Latch: `car-guarded`**:
     true when Popovic is aboard and either the car is not blocked (CarHolders empty) or an agent outside it guards it (Game.Guards: up,
     on the street, gun drawn and not holstering). That is the opposite of fight.unguarded's CarUnguarded.
   - `left-safe` "Anyone left outside walks out, if no fight or seal is on". It goes after `go`. **Check for Latch: `left-safe`**:
     every fate in Game.LeftBehind() is a walk-out (GDD §06: a standing agent walks out unless a combatant is up and not broken or
     the seal clock runs; a downed one is captured). This replaces the brief's draft, "the rest get clear before the seal". The engine
     decides who is left behind at the moment of Go, so "before the seal" alone would not be true.
   Once Latch adds the two words to HostBook.Needs.cs, each entry moves into `ways` after its `after`, and its `note` is dropped.
- Cell page: `book.cell.car` (new, optional in book.schema.ts): word "The getaway car" · icon `car` · line "Anyone can Board, Step out,
  Drive or Go. With Popovic aboard, Go wins." The host doesn't read it yet.

## 4. The feed card: rivals moving on the held car (objective 7)

`cards.items.rivals.for-car`: title **Rivals moving on the car** · result "Nobody outside the car has a gun drawn. Step out and draw,
or down them." The old line ended "Draw one there (F)". It now gives the fix in words with no key (Sky: no key-only features).
The fate stays `icon`. The feed already holds its maximum of 11 beats, and the card that stays until clicked is Glyph's objective 5.
When that card type exists, this card belongs on its list. The log fires once per rival, so the host should show one card per walk.

## For Latch to wire
1. End card: read `debrief.cause[Cause.Fail][Cause.Why]` (sealed: `Cause.Car`, else `unknown`; withdraw: its why) for the title and line in
   HudModel.EndCard. Keep `less.end.fail` and `briefing.fails` as the fallback. Then drop the special cases for `less.closedIn` and `less.sealed`.
2. Story: when the route says `vale-reaches-getaway-car`, add the cause's `story` lines after it, in order. Fill `{holders}` from
   `Game.CarHolders()` at the end tick, and leave out a `{holders}` line when there are no holders.
3. HudToolkit EndCardAudit: also measure every `debrief.cause.*.head`, filled with the longest values.
4. Book: add the check words `car-guarded` and `left-safe` to BookCheck, then move the two `pending` entries into `ways`.
5. Cell page: show `book.cell.car` on every agent's page.
6. Feed: put `rivals.for-car` on the crucial list once the feed has that card type (Glyph 5). Show one card per walk, not one per rival.

## Files
- `contracts/missions/handover/text.json`: the words above.
- `contracts/missions/handover/text.schema.ts`: `HOST_WAYS`; `debrief.cause` and `debrief.story`.
- `contracts/missions/handover/book.schema.ts`: `job.needs.pending`; `cell.car`.
- `contracts/missions/handover/text.test.ts`: HOST_WAYS in the verbs test; the failcam cause list (tail, driven-away); the held-car
  card's new words; the sealed story; two new describe blocks for brief 096. One checks that every engine Fail site's cause has a card,
  using a scan of sim/. The other checks plain words, "blocked" not "held", Step out, and the book's live and pending checks.
