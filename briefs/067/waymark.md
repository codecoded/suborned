# Brief 067 · Waymark: the Handover re-composed on engine 0.34.1 (c038)

2026-10-26, for Aster. One re-compose of the whole route set on the final engine (0.34.1, cb1604f6, GDD v0.26.0), with briefs 059 and 067 folded in; the checker's new conditions; c038 recorded as the reference. Nothing in `sim/`, `contracts/rules`, Unity or `tools/qa` was touched; engine tests were run on a scratch copy (`out/scratch/waymark/eng`).

## 0 · Result
- **`bun tools/missions/check/cli.ts handover` ships:** MC-0 42/42 (1 pending), MC-1 to MC-15 all ok, 897 plays in 21 s (`waymark/missions-check-c038.txt`).
- **c038** is recorded pinned to cb1604f6 (`runs/missions/handover/c038`) and replays 897/897 on its hashes in 11 s.
- **Robust:** every reference route wins 16 of 16 per-operative jitter samples (Motive's JMODE=op stream, each op 0–2 s late); every other script keeps its outcome 8 of 8 (`waymark/ensembles-c038.txt`).
- **Tests:** `bun test tools/missions` 74/74 (the headless "route A and route B win quietly …" test and the c038 replay included); `bun run typecheck` clean.
- **One engine-test handoff (Keel):** two new loud-wait twins move under the final-intent control rule, so `FinalIntentTests.F15_ROUTES` needs them in `Moves` (§5).

## 1 · The checker (mine: `tools/missions/check`, `contracts/missions/routes.schema.ts`)
| MC | what changed | why |
|---|---|---|
| MC-3 | A fail line may carry `needs: "patrol-watches-entry"` (strict schema). The check reads `suborned-sim facts` (`patrolWatchesEntry`, false on today's site) and counts the line only where the fact holds; otherwise "not on this site (no patrol watches an entry)". An engine without the command counts it, as before. Facts are recorded in run.json and the report. | Motive briefs/067/motive-square.md §5.3; GDD v0.26.0 §09. |
| MC-3 | The fail list follows GDD v0.26.0 §07: "Vale leaves in his car", "Enforcer down before Vale is in the car", and the new "the cell down (every op down)" (code `cell-down`). | §07's fail line changed. |
| MC-11 | Reads every stand of the site (`getaway.stands[]`: the square and the service drive, each post against its own car and exit lane: 18 posts). New clause: no played script logs a hold that did not come for the car. `car.held.answer` always counts; `car.held.post` counts when the holder is a rival or a final-fight officer (the bodies the mission sends to its ambush spots and police stands); a response-unit member at its join point (`ai.join`) and a search unit at its stash are listed, not counted. Blocker, press and advance came for the car. | Aster's clause with Motive's refinement (motive-flips.md §9); Vantage's note that MC-11 saw only the square. |
| MC-13 | New: every stand has a loud-wait twin (`holds: <stand>`) that is held: it never wins, and with Vale aboard its Go is refused `car-held`, or it is lost before Vale is in. | brief 059 M059-5.3. The square's twin can't get Vale in at all (the Enforcer alone is shot down on the 65 m walk), so "lost first" counts as held. |
| MC-14 | New: every win the checker plays (variations too) Goes at or after `blocker.out`: 840/840. | brief 059 S1 (Sky: the fight always comes). |
| MC-15 | New: no quiet or loud script logs `rivals.wiped`. | brief 059 M059-S1.5. |
| composer | A plan step may say `refused: "<code>"`: placed at the first tick the engine refuses the order with that code (the twin's Go on a held car). | MC-13 needs the refused Go in the record. |

Reading the log's words: `held` takes the holder from the first word of `car.held.*`, the join point from `ai.join`'s text, Vale's boarding from `board.in`'s "with Vale". Codes say what; only these three lines are read for who.

## 2 · The route set (`contracts/missions/handover/routes`, plans in `plans/`)
| script | plan from | c038 | c037 (0.31) | what moved |
|---|---|---|---|---|
| route-a | Motive ra-h3, no Reroute | 4:37.3, 4 T, A 0, St +3 | 4:59.7, 5 T, A 1 | holds Vale at (20.0, −65.0); runs are no Alert; the Reroute is optional (the patrol sweeps the yard) |
| route-a-board (primary) | c037 plan, no Reroute | 4:15.5, 4 T, A 0 | 4:27.9, 5 T, A 1 | as above |
| route-a-pressure | new (rap-240) | 4:22.6, 6 T, A 0 | 3:53.7 (lost on 0.34.1) | §3 |
| route-a-pressure-board | new (rapb-240, walks Vale in at blocker.out) | 4:13.8, 6 T, A 0 | 3:20.5 | §3; motive-square.md §7 |
| route-a-disable | Motive rad-h3, no Reroute | 4:37.3, 5 T, A 1 | 4:59.7, 6 T | as route-a |
| route-b | Motive rb-rr | 7:32.2, 4 T, A 0 | 8:08, 3 T | Reroute at 5:00, the Enforcer at the square to 5:50 (the loiter report at 1:53.4 is gone), the Scout's lock 0.5 s after the Fixer's |
| route-b-board (primary) | Motive rbb-rr | 7:35.4, 4 T, A 0 | 7:44.5, 3 T | as route-b |
| route-b-thin | new | 7:32.2, 4 T, A 0 | – | §4 |
| route-g | Motive rg-ff, no Reroute | 3:59.6, 5 T, A 1 | 4:44.7, 6 T | focus fire (M059-B7) |
| route-g-board | rgb-ff, no Reroute | 3:59.6, 5 T, A 1 | 4:11.5, 6 T | as route-g |
| route-g-fast | rgf-ff, no Reroute | 2:58.6, 5 T, A 1 | 3:44.2, 6 T | the walk-in at 1:17 (where the rerouted patrol used to leave), knockout ~1:26 |
| route-g-fast-board (primary) | rgfb-ff, no Reroute | 3:02.0, 5 T, A 1 | 3:11, 6 T | as route-g-fast |
| route-g-kill | Motive rgk-h3 | 5:43.3, 2+2+3 T, A 1 | 6:11.7 | the kill now waits for `loop.on` (§3) |
| route-g-kill-board | Motive rgkb-bo | 5:36.1, 2+2+3 T, A 1 | 5:38.5 | walks Vale in once the blocker is out; `loop.on` |
| loud (primary) | Motive loud-x4 (S11's shape) | 5:01.7, 27+3 T, A 2, St −1 | 5:10, 13 T | breaks the crew gunfire draws before Vale is walked in (traced shots) |
| loud-fight | new on loud-x4 | 5:06.8, 26+3 T | 5:12.8 | the Enforcer steps out and brings his carbine: three sidearms alone lose to the two trained officers |
| loud-left-downed, -standing | new on loud-x4 | 5:04.7, the Scout captured downed / fighting | 5:13 | as loud |
| fight-safe-cell-down | new | cell-down 3:20.9 | – | §07's new fail: route-g-fast-board to the spring, the cell drawn and holding fire |
| fight-loud-wait-square | new | enforcer-down 3:58.4 | – | MC-13: the loud front, the three waiting in the car; the Enforcer is shot down walking Vale in |
| fight-loud-wait-yard | new (outcome) | Go refused car-held 7:29.4, withdrawn 7:29.5 | – | MC-13: route-b-board, the pickup camera shot as Vale's car parks; held; the player withdraws |
| careless-thin-all | new | 7:29.3, 19+8 T, A 2, two captured | – | §4 |
| careless-reporter-not-stopped | Motive crns-rr, re-sited | 7:32.2, 4+9 T, A 1 | 8:08, A 2 | guard 73's walk runs 3.5 m south of the old spot now: the Enforcer waits beside the new line; nobody finds the body before the car leaves (A 1, a missing person and an open case: still dearer and higher than its twin) |
| careless-scout-idles-pickup | Motive csip-rr | 7:32.2, A 1 (reported 2:32.3) | 8:08, A 1 | as route-b |
| careless-g-disable | Motive cgd-ff | 3:59.6, 3+4 T, A 2 | 4:44.7, A 2, three captured | the second incident is the found guard at the door before the fight ends; nobody captured now; keeps the Reroute (without it the patrol answers the dead screen and the run seals) |
| careless-loop-runs-out | re-derived from route-a | entry-seen-camera 2:52.9, A 0 | A 1 | runs aren't Alerts |
| careless-enforcer-watches-guard | re-derived from route-g-fast | enforcer-down 2:53.6, A 2 | same | the Enforcer lingers 25 s beside the guard (his own Watch is refused once the Scout has watched) |
| careless-takedown-seen-crowd, careless-guard-left-door-held, fail-vale-leaves | hand scripts, unchanged orders | vale-leaves at 6:13 | other fails / A 1 | since 059 the unturned driver drives Vale off at 6:13; fail-vale-leaves ends at A 0 |
| fail-entry-seen-patrol | pending | – | entry-seen-patrol | the patrol watches no entry (MC-3 `needs`) |
| retired | route-b-block, route-b-block-board (the driver's Call is retired, 059), loud-fast (on 0.32+ the cheapest loud win must break the crew gunfire draws: it is loud) | | | |

The plan notes and step whys say the plan as it is now; the manifest's history note gains a c038 paragraph.

## 3 · Two routes that needed more than the lists
- **route-a-pressure (Aster's ask).** Pressure turns Inez in one order, so her call comes at about 1:46 and the Enforcer used to be out with Vale at 2:24, before the rival car posts. With no warning (Pressure's price) the five arrive hot at 2:46, with no posts, and hunt the Vale holder: the c037 script lost rivals-reach-vale at 3:17.2, and composed fresh it won 3 of 8 jitters. The fix is a timing a player would choose: the Enforcer goes to the door only from 2:40 (in 2:42, out about 3:20), so Vale comes out to the posted crew and the fight is route A's, without the warning. Tried 2:30, 2:40, 2:47, 2:52: 2:30 loses the board twin (0/8), and its fight-out leaves the three captured (the guard ally, shot down in the fight, misses his 4:00 mark and the seal clock starts); 2:40 is the earliest that wins both 16/16. It is no longer the fastest A (4:22.6 against route-a's 4:37.3; c037's 3:53.7 is gone); it stays the Pressure trade: one order and no money, +2 T and Standing +2 instead of +3. The board twin walks Vale in at blocker.out as motive-square §7 asks (16/16; at the spring it also won 16/16, 9 s sooner).
- **route-g-kill and its twin.** 14 of 16 jitters lost: with the Tech 1.4 s late at the cabinet the loop landed at 2:31.4, after the 2:31 kill, and the camera saw it. The kill now also waits for `loop.on`: 16/16, the reference run unchanged.

## 4 · Brief 059's list
- **route-b-thin.** At about 5:31 the Enforcer, waiting at the square, knocks out rival-3 at his lone post by the café terrace from behind (1.3 m off his back), 28 m from the pair, beyond their pistols' 16 m. He wakes after the fight. At the Take the floor recalls the warned lookout and the tipped car: a fight of the two posted plus the late two, never skipped. A passer-by finds the body at 7:30 and is still walking over when the car leaves, so the knockout is free here (4 T, A 0, as route-b).
- **careless-thin-all.** The lone one goes down unseen, but the pair on the square's east side cannot be thinned quietly (M059-S1.6): rival-2 sees rival-1 taken down and draws, the Enforcer shoots him, the run is loud (traced, Alert 2 when the bodies are found) and the yard fight is the official entrance's police; the car goes when their blocker is down, leaving the Scout and Fixer. 27 T in total against route-b's 4. The s1-rulings shape (all three down, the wait for the recalled two) isn't reachable quietly on the square.
- **MC-13's twins**: §2. **MC-14, MC-15**: §1.

## 5 · Open issues
1. **Keel: `FinalIntentTests.F15_ROUTES` fails on the two new loud-wait twins** as committed: their checkpoints move under `every` (the final fight's officers fire on the cell in the car, who gave them no cause), outcome and fail code equal. Add `"fight-loud-wait-square", "fight-loud-wait-yard"` to `Moves` (and drop `"loud-fast"`, retired). On a scratch copy with that one line, F15 is 43/43 and the whole suite 2518/2518 (`waymark/dotnet-f15-routes.txt`). The yard twin withdraws after its refused Go because waiting on would end sealed under `agent` and cell-down under `every`, an outcome move the test can't carry.
2. **Keel: `tools/sim/fixtures059.ts --check` reports DIFF for `sq-route-a-board`**, composed from route-a-board.plan.json, which takes no Reroute now (`waymark/fixtures059-check.txt`). The embedded fixture still plays and SQ1-WALK passes. Re-compose it (the new plan's only hold is rival-2's, the blocker's), or point it at a frozen copy.
3. **Latch (batch):** `tools/game/late.json`'s rows play loud-fight and loud-left-downed: they now end at 5:06.8 (tick 3068) and 5:04.7; its note still says tick 3124. Fight-lab P-A replays route-a-board's recording to a fixed tick (Motive flips §4.4): Keel and Motive re-pick it on c038.
4. **Motive:** route-a-pressure is now slower than in c037 and slower than route-a-board (4:22.6 / 4:13.8 against 4:15.5). If Pressure should stay the fast A, that is a rule or price question (the warning, or the crew's hot arrival), not a route one.
5. **Aster (GDD text, no rule change):** §07 route A still reads "then reroutes the patrol (optional)", which c038 follows; Pressure's line could say the Enter waits for the crew to post. The c037 route-a-pressure note's "the fastest A" is gone from the manifest.
6. MC-13 at the square is "lost before Vale is in", not a refused Go: there is no loud-wait at the square where the Enforcer reaches the car alone.

## 6 · Handoff
- **Inputs:** this file; `runs/missions/handover/c038`; the route set and plans; `waymark/` (evidence and the probe tools: `tools/probe.ts` composes and plays plans with `JITTER=n`).
- **Done-check:** `bun tools/missions/check/cli.ts handover` SHIPS (MC-0 42/42, MC-1..MC-15); `bun tools/missions/check/cli.ts replay handover c038` REPLAY OK; `bun test tools/missions` 74/74; `bun run typecheck` clean.
- **Next owner:** Latch, for batch 0.2.123; Keel for §5.1 and §5.2 before the batch's engine tests run.
