# Brief 110: every order acts at once (Keel, engines 0.55.0 to 0.57.0, 2026-11-12)

Sky, 2026-11-11, playing 0.2.330:
1. "why does scout have to watch first before they can listen? Scout can always listen and watch"
2. "I think scout has to wait to listen to Inez - which is crazy. If I order a listen command, then it should be actioned. No commands should be waiting. It either is accepted or not available."
3. "I think we need to reduce the time things take to happen. Like paying Inez felt forever (though I think that was because it was blocked by scout) but still - some of these things take a while. I ended up just shooting my way through the game."

## For Sky (short)

- **Listen needs no Watch.** Watch and Listen are both open to the Scout from the start; neither waits for the other.
- **Listen acts at once.** The Scout walks to the person right away. She listens as soon as she's close enough (5 m) and keeps close as they move. When they stop, she sits down or stands by a wall nearby (0.56.0: a chair first). The listening time only counts while she's close enough. Inez no longer freezes while she listens: she goes on with her work (0.56.0: only at the tables in the Scout's reach).
- **"Blocked by scout":** nothing in the rules makes the Fixer wait for the Scout. The two can now work on Inez at the same time, and both finish (a new test, LS-BOTH). What held the Pay up was most likely Pay's own condition: it needs her routine, which comes from a Watch.
- **One speed dial on every act** (`act-pace`, 100 = today). The table shows what it does:

| plan | today (0.54.0) | 0.55.0, pace 100 | pace 70 | pace 50 |
|---|---|---|---|---|
| quiet-a (prepared quiet) | 99 wins · 4:45 | 98 · 4:45 | 98 · 4:28 | 96 · 4:18 |
| thin-quiet | 92 · 4:15 | 93 · **4:06** | 91 · 3:40 | 94 · 3:32 |
| quiet-g | 94 · 6:07 | 94 · 6:07 | 96 · 5:54 | 95 · 5:45 |
| quiet-b (the driver) | 100 · 8:00 | 100 · 8:00 | 100 · 7:48 | 100 · 7:40 |
| prepared-loud | 68 · 5:51 | 68 · 5:53 | 61 · 5:31 | 52 · 5:09 |
| unprepared-loud | 0 | 0 | 0 | 0 |

(wins out of 100 seeds · median finish time of the wins on the mission clock)

- **Loud is not the fastest plan for the bots at today's speed.** Quiet-a finishes in 4:45 and thin-quiet in 4:06. Prepared-loud finishes in 5:53 and wins only 68 of 100. Unprepared-loud never wins. Turning the dial down speeds up every plan. It costs the quiet plans nothing (they win about as often). The loud plan gets faster but wins less often (68, then 61, then 52).
- **The acts are not where most of the time goes.** The big wait is the city's own timetable. Quiet-a's player waits for the rival car (2:35) before making the call, and then for Popovic to come out. At 50, quiet-a's Help is given at 0:47 instead of 1:15, but the call still comes at 2:38. So the dial saves 20–35 s on a quiet route, not minutes.
- **Recommendation: 50** (Listen 15 s, Pay 18 s, Enter 19 s, Take 19 s). Quiet plans keep their win rate and are 25–35 s quicker, and going loud gets riskier. 70 is the cautious step. **I left the rulebook at 100**: changing it alters 6 (at 70) or 9 (at 50) of Waymark's timed route scripts, and the feel is Sky's call. Changing it is one number: `values.act-pace-pct`.

## What changed

### 1. Listen needs no Watch (ruling 1)
- `contracts/rules/rulebook.json` `scout.listen.needs`: the `known routine` need is gone. Its src quotes Sky and retires the top-down test's listenInez line. Watch needs nothing either. Each still teaches what it taught: Watch the routine (and mood, sight); Listen the need, temperament and secret.
- **Pay keeps its own need (routine known, from a Watch, §04 "Pay, routine known").** It was not there to force the Watch → Listen order: it is the approach's own price line. So I left it. Aster can put it to Sky if Sky reads "Watch first" on Pay the same way.
- `text.json` lock `needs-routine`: its verbs are now `["pay"]`, no longer listen.
- The ring's Listen row reads the engine (`LearnQuery`), so it opens without host edits.
- The bot plans: 8 Listens on Inez waited for the Watch's log line. They are now `after: watch-inez, when: free:scout`: same order, no knowledge gate. The plans affected: quiet-a, prepared-loud, answer-the-shots, careless, take-rival-car, thin-quiet, undrawn-at-car. The sweep is identical with and without this edit.

### 2. Every order acts at once (ruling 2): the law, and Listen under it
- **The law** is in the rulebook (`cell.rule`): "Every order acts at once … an order taken starts now, its act or the walk to it, and an order that cannot start now is refused with its reason (the ring's lock), never taken to wait for a moment, a person or a window." It names one exception, `car.go.waits` (see the audit).
- **Listen** (`cell.listen.follow` = `v:listen-follow-cm` 300, `sim/Suborned.Sim/GameListenFollow.cs`, new, 105 lines):
  - Out of reach, the Scout walks in at once (cell.approach: aimed at where she will be). No spot ahead, no waiting at a spot. The act starts on the first tick in reach.
  - Nobody is held for a Listen. `rounds.holds` is now `[ask, pay, help, pressure]`.
  - The act's clock runs only on ticks in reach. Out of reach, `BusyEnd` (and `BusyStart`, so the HUD's bar holds) moves on a tick.
  - While the person walks, the Scout follows once farther than 3 m and stops within 1.5 m (`nearWalking`).
  - When the person stands (a server whose round keeps her within `atSpot` for the next 2 s; anyone else, where they stand), the Scout settles near them, no nearer than 3 m. That is the 0.39.0 choice for someone who stays put: a free chair first, else a wall out of the way. If nothing is free, she stays where she is in reach. (0.55.0 required the chair out of every cone, and a Scout already by a wall stayed there; 0.56.0 changes both, see below.)
  - If the person leaves the street (inside, down, gone), or no walk reaches them, the Listen ends there: nothing heard, nothing charged, `order.dropped`.
  - A cancel stops the follow's walk too (`GameCancel.cs`). A following listener with a walk is a walking body, not a fixed one (`GameBodies.cs`).
- With no `follow` (a rulebook pinned before 0.55.0), the old spot, its wait and the hold still play. LS-OLD proves it.
- `Scenarios.Done` now waits for a Listen whose end moves.

### 3. The act-pace dial (item 3)
- `contracts/rules/rulebook.json` `pace` (new section) + `values.act-pace-pct` (100). Schema `contracts/rules/rulebook.schema110.ts` (hooked into `rulebook.schema.ts` on existing lines). Engine `sim/Suborned.Sim/RulesPace.cs` (new): at load, every ref in `pace.acts` reads as round(v × act / 100) s, at least 1 s. Every reader sees the scaled value (engine, cards, host). The dial runs 10 to 100. World clocks are not in the list: the round, guard, rivals, response, loop run, reroute away, attention, seal and shots.
- The acts today and at each setting:

| act | ref | 100 | 70 | 50 |
|---|---|---|---|---|
| Watch | watch-s | 20 | 14 | 10 |
| patrol's routine (two Watches) | watch-routine-s | 40 | 28 | 20 |
| Listen | listen-s | 30 | 21 | 15 |
| Ask / Pay / Help / Pressure | approaches.*.timeS | 20 / 35 / 35 / 25 | 14 / 25 / 25 / 18 | 10 / 18 / 18 / 13 |
| Pay a rival | rivals-pay-s | 35 | 25 | 18 |
| Mislead | mislead-s | 30 | 21 | 15 |
| Loop / Disable / Reroute | loop-s, disable-s, reroute-s | 20 / 10 / 30 | 14 / 7 / 21 | 10 / 5 / 15 |
| Enter (+ alarm) | enter-s, enter-alarm-s | 38 (+30) | 27 (+21) | 19 (+15) |
| Take (struggle part) | take-s, take-struggle-s | 38 (5) | 27 (4) | 19 (3) |
| Board / Carry / Drop | board-s, carry-s, drop-s | 1 / 3 / 1 | 1 / 2 / 1 | 1 / 2 / 1 |
| Knockout / Kill / struggle | ko-s, kill-s, struggle-s | 6 / 3 / 3 | 4 / 2 / 2 | 3 / 2 / 2 |
| Restrain / Stash door / container | restrain-s, stash-door-s, stash-container-s | 8 / 10 / 5 | 6 / 7 / 4 | 4 / 5 / 3 |
| Call | (no act time: it lands at once) | – | – | – |

## "Blocked by scout" (item 3.1)
In 0.54.0 the engine had no rule that makes one cell member wait for another on the same person. Host conversation scenes pause the clock, but they don't hold orders. The likely causes, in order:
- **(a) Pay needs her routine.** The ring locks Pay with "Watch first", and the host's words say "Watch them first (Scout)". So the Pay waited for the Scout to Watch, even though any agent can Watch since brief 031.
- **(b) The old discreet Listen.** The Scout waited at a spot for Inez, then held her still for 30 s plus the linger.

Under 0.55.0, (b) is gone. I chose **both happen**: nothing in the fiction stops a man listening while another talks to her. LS-BOTH gives the Listen and the Pay on one tick: both are taken, both act at once, the Pay holds her, the Listen keeps to her, and both land. (a) stays as Pay's own need (see 1).

## The audit: orders that were accepted and then waited
| order | today | changed? |
|---|---|---|
| Listen (person) | walked to a spot ahead on her walk, waited there, held her | **yes**: walks in at once, follows, clock in reach, holds nobody |
| Watch on a person, the four approaches, Pay/Listen/Mislead on a rival | walk in at once (cell.approach), aimed where she will be, act on the first tick in reach | no: already acts at once |
| Watch on a camera / the patrol / a container | refused too-far / no-clear-line / nothing-to-see with its reason | no |
| Loop, Disable, Reroute | refused `stand-at-cabinet` away from it, `loop-holds` / `patrol-away` / `camera-dead` with the tick | no: unavailable with reason |
| Enter | refused `vale-not-inside`, `door-held`, `doors-locked`, `stand-at-door` | no. Inside, "both doors held: he waits inside" is the act running against the world, not an order waiting to start |
| Take | refused `vale-not-at-car`, `car-not-parked`, `driver-brings-vale`, `stand-beside-vale` | no: Popovic's exit is unavailable-until, with the reason on the ring |
| Takedown, Carry, Restrain, Stash, Drop | refused too-far / person-away / already-carrying | no |
| Board | refused `stand-at-getaway`, `board-running`, `board-tethered` | no |
| Turn the driver | refused `driver-too-late` after 5:40, the walk counted | no |
| Call | lands at once (the guard walks) | no |
| Fire | a lasting order: walks into range, then fights on its own when out of line (the sticky focus) | no: it acts at once |
| **Go during a Board** (`car.go.waits`, Motive G1) | accepted, then the car leaves the tick the climb lands (≤ board.s + carry.s = 1–4 s) | **kept, flagged.** It is a short wait inside an order already under way (the car waits for the door). Making it "unavailable while someone climbs in" would refuse Go for 1–4 s. **Aster to put to Sky.** |
| Host (Latch's, `HandoverHost.cs`) | three host-side queues: (1) an act given within 2 s of the agent's current act ending waits for it (`pending`, FollowUp); (2) a Fire within 2 s the same; (3) a Watch with a passer-by across the line "waits a moment" (`CrowdBlocked`). The click-to-act walk then act is a walk, so acting | **not mine: a handoff to Latch.** (1)/(2) are the HoldOn preset's queue. (3) is exactly an accepted-then-waiting order. |

## Evidence
- `bun run sim:test`: **3435 passed, 0 failed** (174 s), with every new scenario (LS-BOTH, LS-OLD, P110-PACE).
- The scenarios that pinned the old rules, now rewritten to the rulings:
  - N102, N106 and NQ-2: Listen open with no Watch.
  - WM-1 and WM-3: Listen needs no Watch.
  - N130 and N178: never held; the clock runs in reach (`ListenFollows`).
  - N131: the hold is the Help's.
  - LS-SPOT, LS-CHAIR, LS-WAIT, LS-STILL, LS-PLACED, LS-NONE, LS-UNSEEN, LS-READS and LS-CANCEL: the follow law (same ids).
  - The new ones: LS-OLD (old rulebook), LS-BOTH, P110-PACE.
  - CAM-5: the Fixer arrives just before Inez passes, instead of waiting in the mast's view.
  - 096-HELP-9: the Scout walks to the car during the Help, as 9b does.
  - `BodiesIdleTests` counts each settle's arrival.
- **Sweep** (`suborned-sim batch contracts/missions/handover/adaptive --seeds 100 --digest`, 18 plans, 1800 plays, with replay; 0.54.0 from a `git archive HEAD` copy): every play replays identically at every pace. From 0.54.0 to 0.55.0 at pace 100, 469 of 1800 plays move and 59 change outcome. The bands hold plan by plan: wins quiet-a 99→98, thin-quiet 92→93, prepared-loud 68→68, veteran 47→50, take-rival-car 83→86. thin-quiet's wins got faster in 73 of 93 seeds (median 4:15→4:06). The full table is above, with pace 70 and 50.
- **Route scripts** (42 played): at pace 100, 0 outcomes moved. At 70, 6 move: careless-body-found, loud-left-downed, loud-left-standing, loud, route-a-pressure and route-g-kill-board. At 50, 9 move. They are fixed-tick scripts: their orders land at other moments when the acts shorten.
- **DP1** re-recorded (`suborned-sim drive-session`): the same 142 orders, the same 3 checkpoint ticks; only the pinned hashes and the engine version moved.
- `bun test contracts/rules contracts/missions/handover`: pass (with the new `rulebook110.test.ts`, 4 tests). `bun run typecheck`: clean. `bun run test`: 2808 pass, 9 fail. The 9 are the same ones the 107 report found failing on a clean `git archive HEAD` copy: drift-auto ×2, levels skyline ×3, skyline checks, the SK-3 label, rounds "quiet play can reach" and simview's final hash. I didn't recheck them on a clean copy this round.
- Files kept under the 500-line rule. Game.cs, Rules.cs and rulebook.schema.ts (already over) were changed in place on existing lines; the new code is in new files.

## Files
- Engine:
  - `sim/Suborned.Sim/GameListenFollow.cs` (new), `RulesPace.cs` (new)
  - `Game.cs` (one line), `GameListenSpot.cs` (one line), `GameBodies.cs` (one line), `GameCancel.cs` (+1), `Rules.cs` (two lines in place)
  - `Debrief.cs` (version `suborned.sim/0.55.0`)
- Rulebook: `contracts/rules/rulebook.json`, `rulebook.schema.ts`, `rulebook.schema110.ts` (new), `rulebook110.test.ts` (new); `contracts/missions/handover/text.json` (one lock's verbs).
- Plans: `contracts/missions/handover/adaptive/{quiet-a,prepared-loud,answer-the-shots,careless,take-rival-car,thin-quiet,undrawn-at-car}.adaptive.json`.
- Scenarios and tests:
  - `sim/Suborned.Sim/ScenariosListenSpot.cs` (rewritten)
  - `ScenariosRepeat.cs`, `ScenariosSealCar.cs`, `ScenariosNotice.cs`, `ScenariosRounds.cs`, `ScenariosCamera096.cs`, `ScenariosHelpOnce096.cs`, `Scenarios.cs`
  - `sim/Suborned.Sim.Tests/BodiesIdle.cs`
  - `sim/Suborned.Sim/Fixtures/drive-session.script.json`
- Tools (Splice's, one line): `tools/sim/unity.ts` SCHEMAS now copies `rulebook.schema110.ts`.

## GDD lines Aster must update (a new version, then `bun run crew`)
- **§03 Scout:** "Watch reveals routine; Listen reveals need …". Add: Watch and Listen are both always open; neither needs the other (Sky 2026-11-11).
- **§04 approaches / knowledge:**
  - Drop any "Listen needs routine known" / "Watch then Listen" ordering.
  - "Help requires need known (Listen 42 s)": the Listen is 30 s today, × act-pace.
  - Pay keeps "routine known", unless Sky says otherwise.
  - A Listen holds nobody; the approaches hold her as before.
- **§05 / §10 (the discreet Listen, brief 085):** replace "walks to a spot on her walk and waits" with: walks in at once, follows within reach, the clock runs only in reach, settles in a chair or by a wall when she stands.
- **§01 / §11 house rules:** add the law "every order acts at once; an order that cannot start now is unavailable with its reason, never accepted to wait". Name the `car.go.waits` exception if Sky keeps it.
- **§11:** the act-pace dial (`pace`, one value), and the value Sky picks.
- **§13 T15:** wherever it checks a Listen refused before a Watch (needs-routine), it now expects the Listen accepted. Suture owns T15's wording.

## Open / handoffs
- **Sky (via Aster):**
  - the act-pace value (recommend 50; the rulebook says 100 until then)
  - Pay's "routine known"
  - `car.go.waits`
- **Latch (Unity host):**
  - `HostHints.cs:154` H2 hints Listen only after the routine is known.
  - `HudModel.cs:117`'s "Next" still suggests Watch before Listen. A suggestion is fine; it is no lock.
  - `HostHoldStatus.cs:195`'s "waiting" plate can no longer show. It is dead under follow.
  - The `Proof085Beats` rows (`r085listenSpot` and the chair-ahead proofs) read `ListenSpotAhead`, which is now always null. They must move to the settle (`ListenSpotNow` during the act, `SeatedAt`).
  - `HostViews.Listen.cs` draws the spot only while the Scout stands. Under follow she walks during the act, and the figure should walk with her.
  - The progress bar may creep while she is out of reach. `BusyStart` moves with `BusyEnd`, so it holds.
  - The host queues in the audit: (1)/(2) the 2 s FollowUp queue, (3) `CrowdBlocked`'s waiting Watch.
- **Fable / Latch (words):** `PlayText.cs:558` "Watch them first (Scout) to learn their routine." Only Pay uses that lock now, and any agent can Watch.
- **Waymark:** if the dial moves off 100, re-plan the 6 (70) or 9 (50) route scripts listed above.
- **Suture:** T15.
- `tools/levels/dialogue/drift.json` will see `rulebook.schema.ts` and `text.json` changed (drift:auto, at commit).


---

# 0.56.0: the Scout prefers a chair, and the person stays in earshot

Sky, 2026-11-12, on the new Listen (Latch's proof found the Scout never took a chair): "yeah, the scout (she) prefers a chair. Once someone has got to someone to listen to them, the other person should remain in earshot."

## For Sky (short)
- **The Scout is "she"** in the rule text, logs, scenarios and notes I touched.
- **Chair first.** When the Scout settles to listen, she takes a free chair in reach (3–5 m from the person). If there is none, she stands by a wall. If there is no wall either, she stays where she is. If she's standing by a wall and a chair frees up, she moves to it.
- **Why she never sat in 0.55.0.** Two reasons:
  - Every chair near Inez's post is in the square mast's camera view, and the old rule only allowed chairs out of every camera's view. A chair in view is now allowed when no out-of-view chair is free. While she listens, no watcher's clock counts her, so she still isn't noticed.
  - At the start, all four chairs in Inez's ring have customers in them. The café is busy, so on a full café she stands by a wall, as before.
- **Earshot.** Once the Scout is listening to Inez, Inez keeps serving, but only the tables within the Scout's reach. A table that would take her out of earshot is skipped, and she waits at her post until her next table. Her timetable doesn't move: the first table after the Listen goes out at its usual time.
- **Schedules still win.** These moves go on as before, and the Scout follows the person:
  - Inez's own round moves: the Fixer's Call, and the round ending at Alert 2 or the fight.
  - Other people's schedules: the driver's drive, Popovic's exit, the crew.
  - If the person leaves the street, the Listen ends, with the reason in the log.

## Bands (100 seeds per plan, act-pace 100; 0.55.0 = 2949c5701)
Nothing moves. All 18 plans have the same wins and median times. For example:
- quiet-a: 98 wins · 4:45, before and after
- thin-quiet: 93 · 4:06
- prepared-loud: 68 · 5:53
- veteran: 50 · 8:10

37 of 1800 plays changed their finish time, Trail or Alert (none their outcome); every play replays identically. The route scripts: 0 of 42 outcomes move.

How often earshot skips a table (full logs, 100 seeds each): 15 quiet-a plays, 18 thin-quiet, 9 prepared-loud. At act-pace 50 the bands are the same as 0.55.0 at 50 (quiet-a 96 · 4:18, thin-quiet 93 · 3:33, prepared-loud 52 · 5:09), so the recommendation stands.

## What changed
- **Rulebook:**
  - `cell.listen.earshot: true`.
  - The `cell.listen.rule` text: the Scout as she; chair first, out of view if one is free, else in view; earshot; schedules win. Its src quotes Sky.
  - `text.json` silent log `round.earshot`.
  - Schema: `earshot: z.boolean().optional()` (`rulebook.schema.ts`, in place).
  - `rulebook110.test.ts` holds the flag and the quote.
- **Engine:**
  - `GameListenFollow.cs`: `SettleChair` comes first, a wall spot is swapped for a chair once one is free, `EarshotMask` is called while the Scout is in reach, and `ListenChairFor(agent, role)` is a new host read (chooses nothing, hashes nothing) for Latch.
  - `GameListenSpot.cs`: `BestChair` and `BestGround` take `also` (a server's post must be in reach too, so the Scout doesn't have to move when the server goes home), and `BestChair` takes `inView`.
  - `GameRounds.cs`: `RoundEvents.Masked`, the round skipping masked excursions, and `EarshotMask`. A table is skipped when its service point, or any point of its way out from the post, is beyond reach of the Scout's spot (or of where she stands). This only happens while the round clock runs; the Help hold, the call and the end are untouched. It adds hash bytes only once a table is skipped.
  - `Rules.cs`: `ListenEarshot`, one line changed in place. `earshot` without `follow` is rejected.
  - `Debrief.cs`: `suborned.sim/0.56.0`.
  - A rulebook without `earshot` plays and hashes as 0.55.0 (LS-OLD).
- **Scenarios and tests:**
  - LS-CHAIR: she sits in a chair at the first moment one is free.
  - LS-EARSHOT (new): Inez never leaves the settled Scout's reach while she listens, and her next table goes out on time.
  - LS-UNSEEN measures the act's own ticks. On the landing tick she is idle where she sits, and a seat in view starts a watcher's clock from then: move her after the Listen.
  - LS-READS reads `ListenChairFor` too, and LS-OLD removes both flags.
  - `PlaysTheRound` gains LS-CHAIR and LS-EARSHOT.
  - `BodiesIdleTests.AListeningScoutSitsInHerChairOrStandsByAWallOutOfTheWay` checks that a wall is only ever chosen when no chair is free. On those route plays the chairs are full, so she stood by a wall 6 times and sat 0 times.
  - Scout pronouns in `ScenariosListenSpot.cs`.

## Evidence
- `bun run sim:test`: **3440 passed, 0 failed** (238 s).
- `bun test contracts`: 364 pass, 0 fail. `bun run typecheck`: clean.
- `suborned-sim batch … --seeds 100` (with replay), 0.55.0 from `git archive HEAD` against 0.56.0: as above. Every play replays identically.
- **DP1** re-recorded: the same 142 orders and the same 3 checkpoint ticks, outcome none; only the pinned hashes and the engine version changed.

## Open
- **Motive / Sky:** after the Listen lands, a Scout sitting at a café chair in a camera's view is an idle loiterer, and that watcher's clock starts. Should a seated operative read as a café customer to the attention law? That's a design question, not changed here.
- **Latch:** `ListenChairFor` tells the host which chair she'd take. The r085 proof rows can now expect a chair whenever one is free (the café is often full).
- **GDD (Aster):**
  - §05/§10, the discreet Listen: chair first (in view if none out of it), then a wall, then where she stands.
  - The earshot law: the person keeps their activity within the listener's reach for the Listen; schedules win.
  - The Scout is "she".


---

# 0.57.0: a seated cell member is a café customer

Sky, 2026-11-12, answering 0.56.0's open question (should a cell member seated at a café table read as a café customer?): "Yes".

## For Sky (short)
- **A cell member sitting in a public seat is a customer, not a loiterer.** While she sits, no camera, guard, robot or officer counts her as loitering, with or without a Listen, just like the café's own customers. "Public seat" means any seat the street's customers use: every café terrace seat today (24), and any seat the crowd is given later. It isn't limited to Inez's tables.
- **Everything else still notices her as before.** A seen act is still seen (a Pay or Pressure on camera, a takedown, a drawn weapon). Security that knows her face still knows it. A watcher that was already wary stays wary. A report already made still lands.
- **Standing up and standing still in view** starts the clock again from where it stopped. Walking into or out of the seat counts as walking.
- **The act-now law still holds.** No order changed and none waits. Today the only way into a seat is the Listen's settle: she stays seated after the Listen lands until her next order. There is no "sit" order, and a Move onto a seat isn't walkable ground. Adding a sit order would be a design change for Sky.

## Bands
- **Nothing moves.** 0 of 1800 sweep plays change outcome, time, Trail or Alert (100 seeds × 18 plans, 0.56.0 = ac8f379b4 against 0.57.0). Every play replays identically. 0 of 42 route scripts move.
- **Why:** the bot plans walk the Scout off the terrace once a Listen lands (quiet-a's `scout-back`), and while the Listen runs the act already holds every clock. So the law only shows when a player leaves her seated. SEAT-CUSTOMER proves it.

## What changed
- **Rulebook:**
  - `attention.seated` (`counts: customer`, its rule, src quoting Sky).
  - The attention rule's own text: seated in a public seat, the clock holds.
  - Schema: `seated: ground({ counts: 'customer', rule })`, optional only for pinned runs (`rulebook.schema.ts`, in place).
  - `rulebook110.test.ts` holds it.
- **Engine:**
  - `RulesGetaway.cs` reads it (`AttnSeated`).
  - `GameAttention.cs`: in a watcher's sight, a seated operative (`SeatOf`: in a people-file seat, no walk, on the street) holds the clock, as an act does. One line.
  - `Debrief.cs`: `suborned.sim/0.57.0`.
  - A rulebook without `seated` plays and hashes as 0.56.0.
- **Scenario SEAT-CUSTOMER (new):** the Scout listens to Inez from a free café chair and stays seated after it lands, with no order. For 80 s (past every Heat-0 report mark), in the square mast's sight, no loiter clock on her rises and nobody reports her. On a rulebook without `seated`, the same seat in sight runs the clock.

## Evidence
- `bun run sim:test`: **3445 passed, 0 failed** (265 s).
- `bun test contracts/rules contracts/missions/handover`: 350 pass, 0 fail. `bun run typecheck`: clean.
- Sweep as above, with replay.
- **DP1** re-recorded: the same 142 orders and the same 3 checkpoint ticks, outcome none; only the pinned hashes and the engine version changed.

## Open
- **Sky (via Aster):** a "sit" order, or a Move ending on a public seat seating the agent, would let any of the cell use the law outside a Listen. A design change; not made.
- **GDD (Aster):** §05 attention: "an operative seated in a public seat is a customer: no watcher's loiter clock runs on them; every other way of being noticed unchanged".

Branch: `t3/scout-listen-always-available`, on ac8f379b4, not committed (crew don't commit).
