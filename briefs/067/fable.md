# Fable · batch 0.2.123 words (brief 067) · 2026-10-26

Two rounds, both in `contracts/missions/handover/text.json` (schema `text.schema.ts`, checker `text.test.ts`). Nothing in sim/, contracts/rules, mission.json or Unity was edited. Next owner: **Latch** (reads below).

## Round 2 · engine 0.34.x (Aster's ask: Keel's new codes, the yard patrol, two missing words, running)

**1. Keel's codes**
| code | words |
|---|---|
| refusal `car-no-driver` (go) | line "Nobody aboard can drive. Board someone who isn't down, then Go." ("down" is the glossary's word) · pop "Nobody can drive" (Latch's stopgap `HostShort` "No driver aboard" can go) |
| fail `cell-down` | sentence "The whole cell is down. Nobody is left to get Vale out." · end card "Whole cell down." · fail camera "{by} shot {agent}." (why `shot`, as enforcer-down) with tip "Keep someone outside the car." · the Fixer and the Scout speak; `FAILS` in text.schema.ts |
| `ai.advance` (Motive asked for a line) | event `ai-advance`: a rival ("Nobody in reach. Close the gap.") and the Scout ("A rival is coming on.") |
| `rivals.for-car` | event `rivals-for-car` (rival, and the Scout who says what is wrong: "Nobody's outside. They're coming for the car.") and a card, title "Rivals going for Vale" (Latch's own title), "Nobody is standing outside the car, so they come for Vale. Walk someone out." ("walk them out" is `locks.in-car`'s word) |
| `rivals.back-to-cover` | event `rivals-back-to-cover` and a card "Rivals back in cover": one line for both causes (no `pick`, so no host change) |
| `car.held.<how>` | silent: one prefix entry `car.held.` with its reason (log only; the hold marks and Go's refusal already say it) |
| `run.seen` | card "{who} noticed {agent} running." (no Alert), event trigger, toast "A watcher noticed an agent running. Walk, or keep out of its sight, to stay quiet." |

The two held-car cards are there because your `HostFeed.FeedEvents` already makes fallback cards of both codes and the checker holds every such code to having words here. No `sight` or `at` (the cell is in the car, a `sight` rule could hide the very card it needs), no beat (the feed is at its ten).

**2. The patrol sweeps the service yard**
- `targets.patrol`: about "sweeps the service yard"; Watch "Its cone sweeps the yard and the service door."; rounds "It turns at each end. The service door is clear at the far end." (from Vantage's figures: the door is in its cone about three quarters of a round, out of it at the far end).
- Scout and Tech reads (`read/watch/patrol`): "square" and "the door" are now "the yard" and "the service door"; `patrol-returns`: "Mind the yard." / "Patrol is back in the yard."; event triggers `patrol-returns` and H7's line ("A patrol sweeps the service yard").
- The fail tip of `entry-seen-patrol` is now "The patrol was watching the door." (it can only happen if the patrol has left its sweep); three city lines said the patrol came round the door or the steps (the guard's talk to the clerk, the shift change, the café) and now say yard.
- A new test scans every line about the patrol for entrance, square, steps, forecourt, "due round" and "between sweeps".

**3. Two words the host made up, and one it was missing**: `names.targets["crowd-guard"]` "a guard" (and `crowd-officer` "an officer", which `CrowdSecurityName` also reads, with their walk-in plate words); `less.pop["fire-lapses"]` "Lost the target" (Sky's words on 0.2.122: "I kept losing the target"; `HostDone` already reads the key). Found by checking the host's literal keys against the file: `less.pop["door-seen"]` "Seen there" had no word (`PopKey("door-seen")` would have printed the key).

**4. Running is noticed, never an Alert on its own** (a run is noticed; from Alert 2 passers-by remember the face, which the eye field already says)
- `actions.run.does` "Run there, twice as fast. Watchers notice it; it raises no Alert by itself." · H12's Alt line says the same · the patrol's phrase is "Running is noted." (was "reported"; only a long run in sight is reported).
- **H19** (the notice cue, shown once) also meets runners now: "Walk, or leave its sight." (was "Keep moving", which tells a runner to run on) and the long line says "standing still or running".
- `less.done.run.noticed` "noticed by {who}" and `.unknown` "noticed by: ?": the Alt chip of a move. You already read the first (`RunNoticedWords`); `HostFight.UpdateMoveChip` still hard-codes " · seen by: ?" for the unknown watcher; `.unknown` is its word.
- A test now fails any line that mentions a run and the Alert unless it says "no Alert".

## Evidence (round 2)
- `bun test contracts/missions tools/dialogue`: **210 pass, 0 fail**. Baseline before my edits was 198 pass, 4 fail, the four being exactly Keel's new codes. Each of about twenty mutations of the new words (a word put back, a card or pop removed, an Alert put back in a run line) fails a test; the file was restored after each. One run at load 70 hit bun's 5 s hook limit in the tools/dialogue stand-in test (it takes about 4.5 s) and passed on the rerun; `bun run test` (60 s) is steady.
- `bun run typecheck`: clean. `bun test contracts/rules`: 67 pass (round 1's rulebook-schema gap is closed by e17d8ca7).
- Dialogue drift declared once, with a cause that says what moved (`tools/levels/dialogue/drift.json`; `drift:auto --objective dialogue --base 71bf4032 --cause ...`): my `text.schema.ts` (the `cell-down` fail, `Less.done.run`) and Keel's `rulebook.schema.ts` moved the hash; 3/3 runs replay as at 71bf4032. Against HEAD it refuses, since HEAD e17d8ca7 is not a clean base. If either file moves again, run it again. `bun run verify`: r017 to r019 REPLAY OK; red only r011 (skyline) and r012 to r016 (people), which a person declares.
- `bun test tools/drift-auto`: its "nothing to declare" test stays red until those skyline and people ledgers are declared (it is red for them, not for dialogue). `tools/missions` has one red, route A and B headless play ending `withdrawn`: Waymark and Keel's, it reads no text.
- The GDD moved to v0.26.0 while I worked: text.json's `gdd` and `author` now name it.

## For Latch
- **H7 can never show** now. Your `PatrolSeesDoor` gate is right (Motive's trigger is the cone on the public door), and H7 is the only place the file teaches the N key. If Reroute and N still need teaching, Motive re-aims H7 at the service door; the line already says the yard.
- `run.seen` card: `TextCard` fills `{who}` from `WhoIn` (a person), so, by my reading, a camera or "the guard" comes out as "Someone noticed the Scout running" (the old card had the same gap). `EventResult`'s `Between("The ", " sees")` parse gives the right name; use it for this code.
- `HostFight.UpdateMoveChip`: `seen + ", a guard"` should read `names.targets["crowd-guard"]`.
- Nothing to wire: `car-no-driver` (lock by code), the three events (the generic log path, once per 15 s each), the cell-down fail words, `fire-lapses`, `crowd-guard`. The two `FeedEvents` rows for the held-car codes can go.

## Open
- Aster, from Glyph's round: the Go-before-Vale commit hold, and whether boarded agents leave the selection. The goal-line pick ("Leave", not "Go in") and "Cease fire: press X." are mine to change if you prefer Glyph's words.
- `debrief.route.entry-timed-between-sweeps` cannot show on this site (the engine never says it); the line stays for a site whose sweep covers the entrance. The Alt times in `less.price.examples` (Pay 50 s, Ask 25 s) are illustrations from the old pace.
- Chime: `rivals.for-car` and `cell-down` have no sound cue.

---

## Round 1 · engine 0.33.1 (Aster's small round, earlier today)

Items 1 to 4 are done, and item 5 was there (Glyph's `briefs/067/glyph.md`, "Words for Fable"), so it is done too.

| item | key (text.json) | words |
|---|---|---|
| 1 | `locks.car-driving` (verbs: cancel) | line "The car is driving and can't be stopped. Wait until it parks." · pop "Car driving · wait" |
| 1 | `cancelCosts.drives-on` · `less.cancel.drives-on` | "The car drives on to where it was sent, with everyone aboard." · "car drives on" |
| 1 | `locks.stand-at-getaway` | "Too far from the car. Walk within {m} m of it, then Board." ({m} is 1 now; pop "Too far · {m} m" unchanged) |
| 2 | `actions.listen.withTarget` (was already right) | "Listen to {target}" gives "Listen to the guard". A test sorts every verb with a target into direct or with its preposition and fills each with every name it can take. No other template slipped |
| 3 | `cards.items["rivals.coming"]` | `sight: "always"` (a new value), `at: "rival-car"` kept. The Scout's two lines speak of what he hears ("Car turning in. Orbit Walk, by the sound." · "Engine on Orbit Walk. Not ours."), since nobody sees the turn-in. The 2:46 drop and 3:10 park stay in sight |
| 4 | `less.done.gain.timetable` | "Timetable" (a bare noun on the car, as "Rounds" is on the patrol) |
| 5 | `less.done.board.*` (the why chip) | down · inside · stop first · hands full · looping, as Glyph wrote; busy, too far, mission over, restart for the other codes a Board can meet. Same words as the Fire table where the cause is shared. `in-car`: none |
| 5 | `less.go.noVale` | "Calls off the attempt; try again" (the engine's Leave ends Withdrawn; same call as Withdraw) |
| 5 | `actions.ring-turn` | "Turn" |
| 5 | H21 · H22 (priority -8, -9; shown at once, once, ring nothing) | "Cease fire: press X." (Glyph's "X stops fire.", made an imperative with the game's own verb) · "Scroll to switch." New dialogue verb `step-target`; `X` is allowed in the key test (PlayControls binds Cease to X) |
| 5 | goal-line clash | `less.objective.go` "Leave in the [car]." · H18 "Go: leave in the [car]." · Go's `does` "Leave with whoever is in the car…", `withTarget` "Leave in {target}", `then` "leaves in {target}". "Drive" is the Drive order's label now; the briefing story and speech keep their own "drive off" |
| 5 | Glyph's twelve lock shorts | all exist as written and each reads alone; pinned by a test. "Who can": unchanged |

For Latch (round 1, still open where noted)
- **Pill "Listen the guard"** is in `HandoverHost.Verb()`: the watch/listen case and the Fixer's four build `$"{plate label} {t.Label}"`. Give `Intent.Label` the panel's name (`Words()`, or `withTarget` filled from `names.targets`); then both `label == "Listen" ? "Listen to"` special cases (`ActName`, `Words`) can go. Same family, same lines: `Words()` prints "Fire on {t.Label}" where the pill and text.json say "Fire at"; `TargetOf` hard-codes "the response unit" (glossary and `names.targets.security`: "the police") and "the official car" (`names.targets["car-official"]`: "Vale's car"). A Drive wedge that names its stand uses `actions.drive-car.withTarget` ("Drive into the square's bay"), never "Drive {stand}" ("Drive the square's bay").
- **Drive cancel:** Cancel stays hidden (CanCancel false). `HostActivity` already prefers `cancelCosts[a.CancelCost]` to the engine's text. `car-driving` is what a new order to the driver or a rider meets when the host cancels first.
- **Readers still to add:** `less.go.noVale` (the `go` block in `PlayText`); hints H21 and H22 (`HostHints` Trigger and Gone cases). `PlayText.ObjGo`'s fallback "Drive off in the [car]." is stale.
- "Hide in the service door" (a door stash) reads oddly; "Hide behind the service door" needs a per-target label field. Left.
- 2026-10-26 (Glyph N5, 0.2.125): `less.go.noVale` cut from 14 words in two sentences to "Calls off the attempt; try again" (6 words, a fragment: Glyph budget `altline`); it is the car ring's only Alt line (`HudToolkit.Ring.cs` draws one only for Go while Vale is struck); test pins the words, ≤ 6 and no full stop; dialogue hash unchanged (text.json is not hashed), no drift. Latch: the `PlayText.GoNoVale` fallback still holds the old line.
