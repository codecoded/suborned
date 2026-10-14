# 011 · E · Test plan: the Handover, played by the machine and by people

Suture, 2026-10-15. Brief 011 E. Sources: GDD v0.21.0 §03, §04, §05, §06, §07, §09, §10, §11, §13; brief 011; the 010 QA (`briefs/010/review/suture-2026-10-15.md`).

This plan says:
- what each check needs from the build;
- who delivers it;
- how I accept it.

It covers the 28 scenarios (A, then D), Waymark's route scripts (B), T1–T6, the §11 gate, and the playtest protocol.

## 0. Baseline and how to read this
- **Baseline:** the 28 scenarios pass against the reference sim today.
  - Command: `node out/suture/proto/suborned-world-sim.test.js` → "Twenty-eight … scenarios passed", exit 0.
  - Inputs, copied unchanged: `~/Projects/the-suborned/testbed/reference/suborned-world-sim.cjs` (sha256 `0b3a7add…`, renamed `.js` because the test requires that name) and `~/Projects/game-source/docs/prototypes/suborned-world-sim.test.js` (sha256 `c41a8d78…`).
  - Keel ports against this pair.
- **Time:** the prototype ticks at 1 Hz in whole seconds; the engine ticks at 10 Hz. A prototype second s is engine tick 10·s. Mission 0:00 is `mission.json` `clock.startsAt`, still provisional (see O-3).
- **Names:** the prototype's `Robot` is the GDD's Tech (§03). Its "north entrance" is the plan's official entrance on the Records Office's south face (`mission.json` timeline note at 5:50).
- **Engine needs, as codes** (used in §1):
  - MOV: move on the walk graph.
  - ACT: action at a place with reach and duration.
  - CALL: contact call.
  - FIRE: fire, with health, 6 s cooldown and line of sight.
  - CAM: camera cone with occlusion, loop with tether, blocking.
  - GRD: guard post, lure, facing cone, return fire, check-in.
  - BOT: patrol sweep, reroute, chase, fire.
  - SEC: response unit, arrival delay by report source, seal at +2:25.
  - RIV: rival schedule, drawn by gunfire, false tip, fire.
  - VALE: Vale's exit, the official car, the driver.
  - WIT: witness within 13 m on a clear 3D sightline → Trail and Alert.
  - KNOW: known routine or need gating approaches.
  - PRICE: $ and Trail per approach, remembered refusal.
  - OUT: outcome with a fail cause.
  - DEB: debrief state (public security, investigation cause, route summary, extra interventions, contacts).
  - FIX: the test mutates state directly; the port must turn this into a hashed fixture start state or into orders, because replay = start state + orders.

## 1. The 28 scenarios: what each needs
**Done-check:**
- A (headless): all 28 pass under `dotnet`, and the checksums are identical twice.
- D (Unity): the same 28 pass in Unity on the same DLL, with headless and Unity checkpoint hashes equal.
- The "In Unity" column is what must be visible for a player, or me, to check the scenario by eye.

| # | proto line | intent (kept through the rebase) | engine needs | rebase data / rule values | in Unity (visible) | 010 / open |
|---|---|---|---|---|---|---|
| S01 | 13 | The pickup camera covers the car, the entrance camera covers the door, neither sees through the roof, and a loop blinds the entrance camera | CAM (3D occlusion, §05 levels), FIX (sets the loop directly) | camera sites and cones on the civic block | red cones, unlit; loop state shown | O-6 cameras not in `mission.json` |
| S02 | 23 | 5:50 Vale leaves; the unturned driver waits at the pickup; Vale walks in live time; 6:10 at the car | VALE, MOV | official entrance → official bay (south face) | Vale and car move | **B-2** Vale's routine never goes to the car |
| S03 | 36 | Called, the guard walks away in live time; firing from behind avoids his cone; a far shot leaves Inez unwitnessed; 2 hits drop him; A 2 | CALL, GRD, FIRE, WIT, FIX (`contacts.inez = true`) | guard walk 30 s; 2 hits [B] | amber guard cone turns with him | **B-1** 11 guards at the door; **B-7** 45 s walk |
| S04 | 56 | Shooting the guard at the café: Inez witnesses → A 3 at once; no instant fail; response +12 s | FIRE, WIT, SEC, FIX | 13 m; response delays as rulebook values | witness marker on Inez | B-7 timing |
| S05 | 72 | Front route, guard shot beside Inez, still a success; debrief: High alert, "Inez witnessed" | full verb set, DEB | — | debrief (week 4) | DEB is needed headless now |
| S06 | 95 | A shot on the guard's return walk (after 105 s) does not make Inez a witness | GRD (return phase), WIT, FIX | 30 + 60 + 15 s | — | B-7 |
| S07 | 108 | Shooting Inez: contact lost, can't call, A 3, mission continues | FIRE, CALL, OUT | — | — | — |
| S08 | 119 | Shooting the driver: contact lost, A 3, driver gone, the office route remains | FIRE, VALE, OUT | which driver is Vale's | — | **B-4** 7 drivers, none tied to Vale |
| S09 | 131 | Shooting Vale at 6:10 → failure | FIRE, OUT | — | fail screen with cause | — |
| S10 | 140 | **Route A, quiet:** arriving reveals nothing; Watch doesn't reveal the debt; Listen; loop; Help; call; wait for the patrol sweep to pass; enter; extract. Calm, 0 shots, 1 contact | all non-fire verbs, BOT, KNOW, DEB | patrol sweep geometry | sweep cone visible | B-1, O-6 |
| S11 | 171 | **Front, loud:** loop held while rerouting the patrol; the guard fight; success. 2 shots; Enforcer hit while escaping; Raised | FIRE, GRD, SEC, BOT, DEB | Enforcer health 3 [B] | — | — |
| S12 | 197 | An unprepared shot: the camera report escalates (+18 s), no instant fail; the guard's cone; he fires back after 3 s | FIRE, GRD, CAM, SEC | — | — | — |
| S13 | 210 | Out of the guard's cone but in the patrol's: the patrol reports gunfire (+18 s) | BOT, SEC, log text | — | — | — |
| S14 | 226 | Guard down → missed check-in +18 s → response unit arrives with a cone; escape still possible | GRD, SEC | — | response unit visible | — |
| S15 | 242 | An unseen shot → +45 s; the guard can't fire outside his cone; check-in → +20 s | SEC, GRD | — | — | — |
| S16 | 259 | Gunfire brings the rivals +16 s; an active patrol within 30 m breaks off to chase, cone visible | RIV, BOT | rivals at 3:10 | violet rival cone; patrol changes route | — |
| S17 | 274 | A rerouted patrol doesn't chase; the rivals still come, enter from the east, spot the Enforcer near the service lane and fire after 3 s | RIV, FIX (teleports the Enforcer) | rival entry street | violet cone | O-6 rival entry not in `mission.json` |
| S18 | 293 | 2 hits disable the patrol; its cone goes; the shots draw the rivals | FIRE, BOT, RIV | — | cone disappears | — |
| S19 | 307 | The response unit sees across the street, aims before firing (+3 s) and can be downed (2 hits); the seal clock keeps running | SEC, FIRE, FIX (teleport) | seal 2:25 | seal clock visible | — |
| S20 | 327 | **Route B, quiet:** Watch the car; turn the driver before 5:40; false tip at 5:20; 6:10 the service lane; intercept; extract; 0 shots | VALE, RIV, CALL, DEB | false tip 3:50 | car reroutes | **B-2**, **B-4** |
| S21 | 347 | Route B, camera-block variant ×2, plus reroute: the car blocks after 8 s and returns after 2:30 + 8 s; Vale walks to the moving car; debrief lists "2 camera blockades, 1 patrol reroute" | VALE, CAM (block), DEB | block 150 s | car parks in the sightline | B-2 |
| S22 | 386 | Gating: no action at a distance; an unknown person isn't callable; Fixer and Scout must stand beside Inez; a turned contact is callable | ACT reach, KNOW, CALL | reach 3–6 m | action card absent or greyed | — |
| S23 | 402 | Ask cold → refused and remembered, +1 T, can't repeat; routine alone doesn't unlock Help; Listen → Help at +$10,000, T 3 | PRICE, KNOW | §04 prices | price preview | — |
| S24 | 421 | Watch unlocks Pay ($70,000, +2 T); Help stays locked | PRICE, KNOW | §04 | price preview | **B-5** cast Inez is Idealist: Pay is refused |
| S25 | 434 | Pressure: $0, +3 T, callable | PRICE | §04 | — | **B-5** Pressure on an Idealist is "one step worse" (Q4) |
| S26 | 448 | Tech tether: leaving the cabinet ends the loop | CAM, MOV | cabinet site | loop icon clears | O-6 cabinet not in `mission.json` |
| S27 | 457 | Orders can't target the roof; the path bends around it; the operative never stands inside | MOV | Records Office footprint, walk graph | path preview | — |
| S28 | 471 | Engaging nearby stops the approach walk | MOV, ACT | — | — | — |

**What the 28 do not cover:** most §07 fail conditions, the final fight, people stepping their routines, K1–K9, the Talker test and the purse rule. Keel (engine) and Waymark (fail scripts, §2) add these:

| new | intent | source | owner |
|---|---|---|---|
| N1 | Vale leaves in the official car, a fail (no Enforcer action) | §07 fail | Waymark script |
| N2 | Rivals reach Vale, a fail (route B without the false tip) | §07 | Waymark |
| N3 | The district seals, a fail (loud, then linger past the seal) | §05, §07 | Waymark |
| N4 | Enforcer down, a fail | §07 | Waymark |
| N5–N7 | Entry seen by the guard, by a live camera, by the patrol, each a fail | §07 | Waymark |
| N8 | Withdraw ends the attempt (outcome withdraw, not a fail) | §07 | Keel |
| N9 | Purse: Pay Inez + driver = $110,000 is refused; Help + driver = $80,000 fits | §04 purse | Keel |
| N10 | Final fight, loud branch: police from the west street, 2–4 officers, seal clock running | §07 final fight [S] | Keel (week 4) |
| N11 | Final fight, safe branch: rival ambush; the guard fights them; Inez warns 10 s early; driver −1 rival; earlier false tip −1 rival | §07 | Keel (week 4) |
| N12 | People step their routines: at each mission tick the cast stands at its people-file stop | §04, brief 010 | Keel |
| N13 | Witnessing in 3D: a slab blocks the sightline, an open edge doesn't | §05 levels | Keel |
| N14 | A refused approach fails and leaves a refusal; the wrong approach costs one step more; the preview shows "?" until the temperament is known | §04 Q3, Q4 | Keel; Glyph for "?" |
| N15 | K1, K2, K8 inside the mission (memory entry, ladder moves, price line items). K3–K7 and K9 are between missions, later | K1–K9 | Keel |

## 2. Route scripts and the mission checker (B)
- **Format:** each script is a start state plus timed orders in the engine's format.
- **Runner:** `tools/missions/` plays each script and its variations, applies the §09 conditions, and records the results so they replay.
- **My acceptance:**
  1. I replay the recorded results offline.
  2. I play route A and route B myself in the Unity build.
  3. I check the hand-played outcome equals the script outcome, at the same checkpoint hashes when the orders are the same.

| script | from scenarios | must show | §09 condition it feeds |
|---|---|---|---|
| R-A quiet (Help) | S10, S22 | win, 0 shots, Calm | ≥ 2 quiet routes win |
| R-A Pay | S24 | win, or refusal if Inez stays Idealist (B-5) | price ladder |
| R-A Pressure | S25 | win, T +3 | — |
| R-A cold-recover | S23 | win, +$10,000, +1 T | — |
| R-B quiet | S20 | win, 0 shots | ≥ 2 quiet routes win |
| R-B camera-block | S21 | win | — |
| Loud front | S11, S05 | win, T > every quiet win | the loud route exists and costs more T |
| F-1 … F-8 | S09, N1–N7 | each named fail reached, with the right cause | every fail is reachable |
| V-contact: remove Inez | S07 | R-B still wins | no single contact is essential |
| V-contact: remove the driver | S08 | R-A still wins | no single contact is essential |
| V-delay | every order of R-A and R-B, delayed in steps | the latest still-winning delay | an informed player finishes ≥ 1:00 before the closing event |

Waymark must answer before the runner can judge: what is the "closing event" for each route? Route A must be inside before Vale's 5:50 exit; route B runs to 8:00.

## 3. Determinism and performance (A, D; Redline reviews)
| id | check | how I verify |
|---|---|---|
| D1 | The same orders give byte-identical checksums twice, headless | run the CLI twice per scenario; diff the outputs |
| D2 | Unity re-simulating a headless script gives identical checkpoint hashes | the Unity test runner (`mcp__unity__run_tests`) on the 28; compare hashes |
| D3 | A recorded Unity session replays headless to the same outcome and final hash | take one hand-played session log and run it through the CLI |
| D4 | Orders and the start state are the only inputs: no wall clock, no unseeded random, no floats in sim state | Redline's review; I spot-check by replaying on the Studio and the Air |
| P1 | sim-seconds per wall-second for one Handover run (A's figure) | read from the CLI |
| P2 | 60 fps warm at play zoom on the Air, with people and the engine in | Redline's harness in a development build; I note the frame time in every playtest log |

## 4. T1–T6: what each test needs from the build
- **Players:** 6 new players per test (§13), new to the game.
- **Pass bars:** where §13 gives none, I propose 5 of 6 (my hold); the owner confirms.
- **Logs:** every test reads the session log in §6.

| test | GDD owner | week | build needs | measure | pass bar |
|---|---|---|---|---|---|
| T1 tactical pause | Pulse | 3 | pause as a build flag (on or off, not a player toggle); live clock; Next Event; the fail cause shown and logged | 6 players with pause, 6 without, different people. Per failure, ask "why did that fail?" and score the answer against the logged cause. Tension rating 1–7 after play | §13: pause wins unless the no-pause arm explains failures at least as well AND rates tension higher (median) |
| T2 loud dominates? | Motive | 3 | first-shot tick, route class (A, B, loud, mixed) and an "informed" flag (Watch or Listen before the turn) in the log | first shot before 2:00 (tick 1200); finish times | < 25% shoot before 2:00 (≤ 1 of 6; ≤ 2 of 12); informed quiet wins finish faster than loud wins (median). With no loud win, compare against the loud script |
| T3 Trail readable? | Motive, Glyph | 4 | Trail in the HUD; the debrief investigation state with its named cause (§08 [B]) | ask "why is the investigation active?" and score it against the engine's cause | ≥ 5 of 6 |
| T4 witnesses add decisions? | Pulse | 4 | a crowd-witness build flag (on or off); crowds simulated and visible | 6 on, 6 off. Log paths against the shortest path near crowds; ask "did anyone on the street change your plan?" | §13 is qualitative. Proposed: ≥ 4 of 6 in the "on" arm make one route change they attribute to a crowd, and the log confirms it. Crowd witnesses are first in the cut order: if cut, T4 doesn't run |
| T5 kidnapping fits the tone? | Waymark | 3 | the §07 brief text in game; the cell look (§15) | after play: "who are these four?" (open), then a forced choice: resistance, criminals, spies, other | proposed ≥ 5 of 6 read them as resistance, not criminals |
| T6 final fight reads as consequence? | Waymark | 4 | final fight, both branches (N10, N11) | ask "why these enemies?" and score against the branch and its cause | ≥ 5 of 6 link it to loud or quiet |
| T7 [L] | Fable | later | — | — | — |

## 5. §11 gate checklist (I check it every week from week 3)
An uncoached player can do each item. The bar is ≥ 5 of 6 per item (my hold). Evidence: the log plus the answers to these questions, asked verbatim.

| # | the player can… | question or observation | evidence (log) | available from |
|---|---|---|---|---|
| G1 | state the objective and the fail conditions | "What were you trying to do? What would have made you lose?" | §07 win and fail list | week 3 |
| G2 | pick an approach from something they learned | observe: an approach chosen after Watch or Listen revealed something; then ask "why that approach?" | Watch or Listen before the approach | week 3 |
| G3 | explain its $, time and T cost | "What did that cost you?" | price shown against price paid | week 3 (needs Glyph's action card) |
| G4 | complete a chain | observe: one route plays through to its payoff (the call → guard leaves → entry; or turn → reroute → intercept) | route summary | week 3 |
| G5 | say why police or rivals | "Why did those people come after you?" | final fight branch | week 4 (N10, N11) |
| G6 | read what the next mission inherits | "What will be different next time?" | debrief | week 4 |

Wins and fails are judged by my hold: the Handover wins when Vale reaches the car alive. It fails if Vale dies, leaves or is taken, the district seals, the Enforcer goes down, or the entry is seen.

## 6. Playtest protocol
- **Who:**
  - 6 new players per test. "New" means they have never seen any build, the GDD or the crew's work.
  - Mix them: at least 2 who play real-time tactics games and at least 2 who don't.
  - Sky recruits them and runs the sessions (the only human). Any payment to players is new spend, so it goes to Sky.
- **Order:**
  1. Sky's first play: a smoke test of D, informed, not a playtest.
  2. Then the uncoached test in week 3.
  3. Then week 4 (freeze the rules) and week 5 (final gate), each with new players.
- **Entry criteria** (I say go or no-go):
  - A and B done: 28 headless, every fail reachable.
  - D done: 28 in Unity; hand-played route A matches its script; a player build runs on the Air at 60 fps warm, or the measured figure is noted.
  - Session logs replay.
  - The open 010 blockers B-1 to B-5 resolved.
  - The build hash is frozen for every player in the test.
- **Machine:** the Air, 1080p, mouse, on mains power, warmed up 5 min. Same build, same settings for everyone.
- **Facilitator card (Sky reads it verbatim):**
  - Intro: "This is a tactics game in development. Please think aloud as you play. I can't answer questions about how to play; that is what we're testing. There are no wrong answers."
  - Allowed replies only: "What do you think?" · "What would you try?" · "Please keep saying what you're thinking."
  - If the player gives no input for 3 minutes, say "Please keep going" and log a "stuck" mark.
- **Session timeline:**
  - 2 min intro;
  - play up to 30 min, at most 3 attempts (replay before Continue is allowed, §08);
  - 10 min interview: G1–G6, then the T questions for that test, verbatim, in that order;
  - 2 min ratings: tension 1–7, clarity 1–7.
- **What the build logs** (Latch, D):
  - build: commit, engine DLL sha, rules sha, people sha, cast hash, mission sha;
  - arm flags: pause, witnesses;
  - per attempt: start-state hash; every order with tick, operative, verb, target, and whether the action card was opened and the price shown ($, time, T, A risk); checkpoint hashes; outcome and fail cause with tick; first-shot tick; route summary; debrief state;
  - meta, never sim input: pauses (tick, wall duration), Next Event uses, frame-time stats.
  - Every attempt must replay exactly from its log.
- **Recordings:**
  - Screen and voice, with written consent.
  - Raw video stays off the repo on Sky's disk.
  - The repo gets anonymised IDs (P01…), order logs, transcribed answers and short clips. Clips can be re-rendered from the order log by replay in Unity, so I don't need the raw video for them.
  - No paid transcription without Aster's go-ahead.
- **Scoring:**
  - I score each answer against the log's truth (logged cause, route, price), using a rubric of correct and incorrect example answers per question, written before the test.
  - Aster spot-checks 2 players per test.
- **Report:** `briefs/011/playtests/<test-id>/`, one folder per player (log, answers, clip refs), plus `report.md`:
  - per question: the bar, the per-player result, pass or fail;
  - findings with steps to reproduce (the order log), expected against observed, and a clip;
  - each bug sent to its owner (§14); I retest by replaying the log on the fixed build.

## 7. Blockers and mismatches (to Aster)
| id | issue | needs | owner |
|---|---|---|---|
| B-1 | All 11 guards stand at the Office's public door, so route A can't be won (entry seen) | one door guard in the window (split door and inside posts, or a cast rule) | Pulse, Motive |
| B-2 | Vale's routine never goes to the official car; the driver goes to the café at 17:30 | a councillor routine to the car at shift end, or the mission's events override the routine (Keel rules which) | Pulse, Keel |
| B-3 | Mission start 17:00 or 17:24:30 is open; at 17:24:30, M-2 can't cast the guard, driver or councillor | Sky decides; Waymark re-times | Sky, Waymark |
| B-4 | 7 drivers at the bay and none tied to Vale; no role pins in `mission.json` | a "drives for" tie or rule; pins written into `mission.json` | Waymark, Motive |
| B-5 | The cast Inez is Idealist: S24 (Pay accepted) and S25 (Pressure) conflict with §04 (an Idealist refuses Pay; the wrong approach is one step worse) | keep the intent: recast Inez's temperament, or rewrite the S24/S25 expected values per the rulebook | Keel, Waymark, Motive |
| O-6 | The cameras, the Tech's cabinet, the patrol path, the rivals' entry and the security entry (west street) aren't mission points | add them to `mission.json` on the civic block | Waymark, Vantage |
| B-7 | The guard's 30 s walk is 45 s on the walk graph at 1.4 m/s | re-time it, or set the guard's pace in the rulebook | Waymark, Keel |
| M-1 | The prototype ends "Vale leaves in the official car" at 9:50 (590 s); §07 says the mission is over at 8:00 | the rulebook takes §07, or Sky changes §07 | Keel |
| M-2 | Prototype guard health 2 and Enforcer 3 are [B]; §06's table (guard 3, Enforcer 6) is [S][L], after the capstone | the capstone uses [B]; the rulebook states it | Keel |
| M-3 | The response delays (8, 12, 18, 20, 45 s), check-in 18 s, patrol reroute 115 s and aim windows (3 s) come from the prototype and are not in the GDD | the rulebook carries them with src = prototype [B] | Keel |

## 8. Hand-offs
- **Keel (A):** port S01–S28 with FIX turned into hashed fixtures or orders; N8, N9, N12–N15; D1, P1.
  Done-check: `dotnet` 28/28 twice with identical checksums.
- **Waymark (B):** R-A, R-B, Loud, F-1…F-8, the variations; answer the closing-event question; resolve O-6, B-3, B-4 and B-7 with Sky.
  Done-check: the runner's results replay, and every §09 condition is judged.
- **Latch (D):** the log fields in §6; pause and witness flags; D2, D3; a player build for the Air.
  Done-check: 28/28 in Unity, and a hand-played route A equals its script.
- **Redline:** D4, P2.
- **Sky (via Aster):** recruit 6 or 12 new players for week 3 (12 if T1 runs in full), run the sessions from the card, decide B-3.
- **Me next:** the scoring rubrics per question before week 3; Sky's first-play checklist after D; then the first uncoached test, reported in `briefs/011/playtests/`.
