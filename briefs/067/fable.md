# Fable · batch 0.2.123 words (brief 067, Aster's small round) · 2026-10-26

Items 1 to 4 are done, and item 5 was there (Glyph's `briefs/067/glyph.md`, "Words for Fable"), so it is done too. Words are in `contracts/missions/handover/text.json`; schema `text.schema.ts`; checker `text.test.ts` (154 tests). Nothing in sim/, contracts/rules, mission.json or Unity was edited. Next owner: **Latch** (his reads are below).

## Why the live tree is not green yet, and none of it comes from my files
1. **Keel's rulebook schema.** d4361806 put `cancel.driveCar`, `car.footprint` and four `car-*-cm` values into `contracts/rules/rulebook.json` but not into `rulebook.schema.ts`, so on HEAD, before any change of mine, `rulebook.test.ts`, `text.test.ts` (fails at import, 0 tests run) and `dialogue.test.ts` are red ("Unrecognized key driveCar / footprint"). Verify and typecheck never parse it, so they stayed green. The fix is two optional keys: `briefs/067/fable/rulebook-schema-0.33.1.patch` (from the repo root, `git apply` it; `--check` is clean). Whoever applies it moves the dialogue hash (rulebook.schema.ts is in it): run `bun run drift:auto` after.
2. **Other agents' drift in flight (brief 067 L, the people side).** `bun run verify` fails on r011 (skyline) and r012 to r016 (people) with "CHECKER CODE changed": tools/people/*, `people.schema.ts` (an optional `Place.beside`), tools/plan/block.ts. `drift:auto` refuses the people ones (P-0 and P-6 now differ at r012): a person declares those. drift-auto's own "nothing to declare" test is red for the same reason.

## The words
| item | key (text.json) | words |
|---|---|---|
| 1 | `locks.car-driving` (verbs: cancel) | line "The car is driving and can't be stopped. Wait until it parks." · pop "Car driving · wait" |
| 1 | `cancelCosts.drives-on` · `less.cancel.drives-on` | "The car drives on to where it was sent, with everyone aboard." · "car drives on" |
| 1 | `locks.stand-at-getaway` | "Too far from the car. Walk within {m} m of it, then Board." ({m} is 1 now; pop "Too far · {m} m" unchanged) |
| 2 | `actions.listen.withTarget` (was already right) | "Listen to {target}" gives "Listen to the guard". A new test sorts every verb with a target into direct or with its preposition and fills each with every name it can take. No other template slipped |
| 3 | `cards.items["rivals.coming"]` | `sight: "always"` (a new value), `at: "rival-car"` kept. The Scout's two lines now speak of what he hears ("Car turning in. Orbit Walk, by the sound." · "Engine on Orbit Walk. Not ours."), since nobody sees the turn-in. The 2:46 drop and 3:10 park stay in sight |
| 4 | `less.done.gain.timetable` | "Timetable" (a bare noun on the car, as "Rounds" is on the patrol) |
| 5 | `less.done.board.*` (the why chip) | down · inside · stop first · hands full · looping, as Glyph wrote; busy, too far, mission over, restart for the other codes a Board can meet. Same words as the Fire table where the cause is shared. `in-car`: none |
| 5 | `less.go.noVale` | "Go without Vale calls off the attempt. The cell gets away to try again." (the engine's Leave ends Withdrawn; same call as Withdraw) |
| 5 | `actions.ring-turn` | "Turn" |
| 5 | H21 · H22 (priority -8, -9; shown at once, once, ring nothing) | "Cease fire: press X." (Glyph's "X stops fire.", made an imperative with the game's own verb) · "Scroll to switch." New dialogue verb `step-target`; `X` is allowed in the key test (PlayControls binds Cease to X) |
| 5 | goal-line clash | `less.objective.go` "Leave in the [car]." · H18 "Go: leave in the [car]." · Go's `does` "Leave with whoever is in the car…", `withTarget` "Leave in {target}", `then` "leaves in {target}". "Drive" is the Drive order's label now; the briefing story and speech keep their own "drive off" |
| 5 | Glyph's twelve lock shorts | all exist as written and each reads alone; pinned by a test. "Who can": unchanged |

## For Latch
- **Pill "Listen the guard"** is in `HandoverHost.Verb()`: the watch/listen case and the Fixer's four build `$"{plate label} {t.Label}"`. Give `Intent.Label` the panel's name (`Words()`, or `withTarget` filled from `names.targets`); then both `label == "Listen" ? "Listen to"` special cases (`ActName`, `Words`) can go. Same family, same lines: `Words()` prints "Fire on {t.Label}" where the pill and text.json say "Fire at"; `TargetOf` hard-codes "the response unit" (glossary and `names.targets.security`: "the police") and "the official car" (`names.targets["car-official"]`: "Vale's car"). A Drive wedge that names its stand uses `actions.drive-car.withTarget` ("Drive into the square's bay"), never "Drive {stand}" ("Drive the square's bay").
- **Drive cancel:** Cancel stays hidden (CanCancel false). `HostActivity` already prefers `cancelCosts[a.CancelCost]` to the engine's text. `car-driving` is what a new order to the driver or a rider meets when the host cancels first.
- **rivals.coming:** your `TextCard` already tests only `c.Sight == "in-sight"` and the `heard` override is gone from the working tree, so an `always` card shows as Aster wants. Nothing more to do. `ring-turn` is read already (`HostContext`, the merged wedge).
- **Readers still to add:** `less.go.noVale` (the `go` block in `PlayText`); hints H21 and H22 (`HostHints` Trigger and Gone cases; the host skips unknown ids until then; they show at once, as H13's door cause does). `DoneWord("board.<code>")` and `("gain.timetable")` arrive through `DoneFlat` with no reader change; `PlayText.ObjGo`'s fallback "Drive off in the [car]." is stale.

## Evidence
- **Clean room** (`out/scratch/fable/head`: HEAD d4361806 plus my three files, nothing else): `bun tools/verify.ts` fails on r017 to r019 until the dialogue drift is declared, then **VERIFY OK** (18 replays, typecheck clean). With the rulebook patch applied there: `bun test contracts/missions tools/dialogue tools/drift-auto` **217 pass, 0 fail**; typecheck clean. Without the patch: 15 pass, 2 fail, 1 error, the same counts as HEAD.
- **Live tree:** `bun run typecheck` clean. `bun run verify`: dialogue r017 to r019 REPLAY OK; red only on the two causes above. Done-check tests: 14 pass, 3 fail, 1 error (the rulebook gap twice, plus drift-auto's "nothing to declare").
- **Dialogue drift declared** (`tools/levels/dialogue/drift.json`, `drift:auto --objective dialogue`, 3/3 runs replay as at HEAD): two files of the hash moved, my `text.schema.ts` (widened) and the people side's `people.schema.ts` (`Place.beside`); the cause names both. If either file moves again, `bun run drift:auto` declares it.
- Mutation checks bite: the Listen slip, a lost board word, the beat's sight, the chip, the stand line, H18, H21's priority, a key in H22. `suborned-sim codes` matches text.json (car-driving).

## Open
- `dialogue.test.ts`'s stand-in test takes about 4.5 s and misses bun's default 5 s when the machine is busy (one red in three runs here); `bun run test` (60 s) is steady.
- "Hide in the service door" (a door stash) reads oddly; "Hide behind the service door" needs a per-target label field. Left.
- Aster's, from Glyph: the Go-before-Vale commit hold, and whether boarded agents leave the selection. The goal-line pick ("Leave", not "Go in") and "Cease fire: press X." are mine to change if you prefer Glyph's words.
