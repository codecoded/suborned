# Brief 101 · Keel's report: the square's lamp mast in the engine, engine 0.52.0 (2026-11-09)

Branch `t3/fix-mast-sim-scenarios`, from `work/098-living-playbook` (99bebe7d8), with Vantage's `t3/square-lamp-mast` merged in (71cf3d420: the mast at block
(−385, −4255) cm on the terrace's west lamp, Vela's `MastCamera_Lamp`). `bun run people`, then `bun run sim:site`: only the cast's sha256 in the site moved.
Read: brief 101 "The square's camera: Sky picks B" with Motive's ruling and Sky's answers, `briefs/101/vantage/lamp-mast-built.md`,
`briefs/101/motive/mast-pressure.md`, brief 102's open item on "he's not answering".

## 1. The crash (P85-MIXED)

- **Why it threw.** Under the mast the Pressure at the Courtyard is taped (Alert 1 at 1:06.8). Inez is Loyal by the scenario's start, so at the act's
  end she reports it too. The engine counted the report as a second incident: Alert 2, the Call refused `summon-alert`, so `GuardCallWindows` was
  null and `!.Value` threw.
- **The law it broke.** The rulebook's alert rule ("however many people see or report it, an incident ... raises Alert once"), the camera rule ("one
  act is one record and one incident"), GDD §05 witness ("one act is one incident however many people and cameras see it") and my own hold (one
  incident counts once toward Alert and dispatches once).
- **The fix (engine).** A Pay or Pressure a camera tapes and its subject reports is one incident, counted toward Alert by whichever lands first. The
  other raises Alert no higher than that incident's level; the record still raises it to at least camera.alert. The desk dispatches once.
  - `Report(p, act)` moved from `GameOrders.cs` (683 → 672 lines) to `GameCameraRecord.cs`.
  - The record holds its incident's number (`CamRec.Incident`, hashed with the records).
  - One sentence added to the rulebook's `camera.rule`. No value moved.
- **No exception from a missing window.** Every scenario that read `GuardCallWindows(...)!.Value` (4 in `ScenariosPressedCall.cs`, 4 in
  `ScenariosCallOffer.cs`) now uses `Window(r, role, out w)`. With no window that is a failed check naming the Call's refusal, never a throw.
- **What P85-MIXED tests now.** The same law as before (helped, then pressured: the call is pressed), plus the mast's facts: the mast taped the
  Pressure, she reported it, `(Alert, Reported) == (1, 1)`, and the guard still answers.
- **The eight every-scenario tests** pass, since P85-MIXED no longer throws.

## 2. The 31 tests

**Moved setup** means the mast's new sight only broke the scenario's setup, so the setup now avoids it and the scenario tests the same law.
**Changed expectation** means the law's outcome changed, and the scenario says why. No value was tuned.

| test | what broke under the mast | what I did |
|---|---|---|
| DP1 | the start state and inputs | re-recorded (`suborned-sim drive-session`). Its drive orders moved too: the recorder corrects itself against the engine's walk, and the lamp's move changed walk cells |
| SightTests ×2 (and N67's fan) | **the fan was wrong**, not the engine (§3) | fixed the fan |
| N67 | the old mast's facts | rewritten for the lamp mast, moved to `ScenariosMast101.cs`. Its place; each head sees its side (Fountain, Courtyard, West side); it sees the café front; no point within 2 m of any start mark or the getaway bay is seen (10 cm lattice); the Office side isn't. Watch, loop and one loop at a time as before |
| S24, S25 | the Pay and Pressure at her post are taped (+1 T) | moved setup: the Tech loops the mast first, and the act's Trail is read from just before it (the loop's own +1 T is the loop's). Each also checks "no camera taped it" |
| N12c | taped first, so the witnesses added no Trail (one act, one incident) | moved setup: the loop (the Tech walks while the Scout watches, and loops before the mast can report her) |
| CAM-6 | Inez is in the mast's view 92 % of the time; the scenario's `ForHer` then walked her back to her post | moved setup: the Pay is given at the tick the card reads "no", at the far table her round serves out of every view; the act holds her there. Out of every view she is only there: 0:38–0:58 and 4:21–4:42 |
| CAM-5 | the Scout loitered beside Inez in the mast's view, so its report raised Alert 1 before the record could | moved setup: the Scout watches and listens from her start (17 m, out of its view) |
| S22 | its Listen spot is now out of the mast's cones: a 37 s walk, over a fixed 30 s wait | moved setup: the wait is the card's own walk (38 s) + 1 s, "within what the card said" |
| N130 | the old mast's cone covered `cafe-table-6-seat-2`; now free and nearer, it is picked for her walk home 12 s ahead, and the act starts on arrival at 8.7 s | **the card was wrong** (§4). Fixed, the card says 9 s. The loop first also keeps the Listen's ground as it was |
| N178 | her walk home is in the mast's view, so the meeting on her path was refused (a 46 s spot) | moved setup: the loop first |
| N20c | every run past people now crosses the mast | moved setup: looped over 3:00–5:00 |
| N23, N37 | the draw at the Courtyard is seen by the mast (Alert 2, police) | moved setup: the loop first |
| S19 | the Enforcer's wait spot (14, −33) m and his run to the north street, weapon drawn, are in its view | moved setup: the nearest spot no camera sees (14.25, −34.25) m; the Tech loops the mast after his reroute, and the Enforcer runs once it's on |
| N40 | the Listen during the guard's visit walked 88 s to a spot out of its cones; he had gone | moved setup: the Scout waits at her start, the Tech loops as the call is made (to 3:06), then the Scout comes in. The Listen walks 0.8 s |
| 096-HELP-9, 9b | the Listen's long walk and a loiter report pushed the driver's Help past 5:40 | moved setup: `ListenToInez(r, loopMast: true)`, the loop after the Watch |
| CREW-BODY, CREW-FREE | ambush-2, rival-3's post, is in its view | moved setup: the Enforcer waits out of view; the loop lands as rival-3 posts |
| S1-FLOOR-WAKE | the same, and route A's Enter needs the entrance loop (one at a time) | moved setup: the mast loop 2:35–4:35 over rival-3's post, then the entrance loop for the Enter. The Scout watches and listens from her start, and the Fixer steps back out of view after the Help. The knockout waits for a moment Inez's round keeps it out of her cone for the whole act (`InezAway`): she had seen it 3 s in |

rival-3 stays the target: rival-1 and rival-2 post 2.25 m apart (ambush-1, ambush-3), so either would see the other taken.

## 3. The fan (SightTests): the fan was wrong

- **North head.** From the 7.68 m lens, the line to a head on the −18 cm carriageway 27.8 m out passes 1 cm under `police-post-1`'s 2.82 m top. The
  engine hides those targets. The fan read the floor just behind the post (the pavement, 0), so it drew them in sight.
- **Seen-over shadows** (`Hidden`, `HiddenTo`) now end where a target on its own floor shows over the top. The ray is cut at the walk cells' edges
  and each stretch is solved exactly: a rising floor (the Office landing) ends a shadow sooner, a falling one later, and a fall further on can hide
  a target again.
- **People's shadows** are `CrowdBlocksZ`'s own shape: the wedge of lines within crowd.occlude of them, from two bodies' width (60 cm in plan, a
  chord over each half) to where the line clears their head top on each target's floor, one piece a hidden stretch. It was their octagon scaled along
  the ray: it shaded from 30 cm behind them and dropped the wedge's edges.
- **The reach.** Shadows reach the cone's far corners (36.6 m for the mast), not its 30 m ahead.
- **The file split.** `GameSight.cs` (800 lines) is now `GameSight.cs` (461), `GameFan.cs` (249, moved byte for byte) and `GameFanShadows.cs` (153).
- **Evidence:** a sweep of every watcher's fan against `InSight`, crowd on and off, at 91 ticks across the mission (the test's own sampling and
  8 cm band). 0 disagreements; 913 on HEAD's engine with the same data. The fan is a read: no hash moves.

## 4. Card reads (never hashed)

- **A Listen's start** (`StartsAt`, `GameListenSpot.cs`): the first tick the person is within reach of its spot. It was the look-ahead moment the
  spot was chosen for (N130).
- **A crew member's Pay card** (`CrewQuote`) carries the tape mark, as a person's does (`QuoteRecord`). Since engine 0.41.0 the engine tapes a member
  paid in a live camera's view, and the card said "A 0" with no eye. The host's card (`HostCrewTurn.cs`) still writes "A 0, no tape": Latch's, below.

## 5. N18 (Sky kept it)

- Inez's ladder expects job-exposed when she is questioned **or** a watcher logged a cell operative beside her (the engine's `ExposedByWatcher`),
  else job-unexposed.
- Route A at her post today is both questioned and logged: the investigators reach her by the mast's own sighting lead.
- **M101-LOGGED / M101-LOOPED** (new) show the law and its counter. With the mast live, its log of the Fixer beside the turned Inez exposes her job.
  With the mast looped across the Help and the call, and the Fixer gone before the loop ends, no watcher logs anyone beside her, nobody questions her,
  and her ladder is job-unexposed.

## 6. Motive's ask: CAM-10

rival-3 is posted under the mast (ambush-2 on seed 0; Mercenary by the start, test only). The Scout watches the mast, and the Fixer watches the crew
(rivals.turn.needs) and pays him there.
- Before the commit the Pay card reads `yes mast-z0 +1 T ▲1 in n s`.
- The record lands when the card said: the Pay's Trail + 1, Alert 1, one incident.
- The dial's robots go to his post.

CAM-11 (new) holds the one-incident law. The card reads ▲1 for the tape and ▲1 for her report. The record lands first (Alert 1, one dispatch), and
her report is the same incident. The driver's Pay under the pickup camera is the second incident: Alert 2.

## 7. The read the host needs: `Game.KnowsDown(who, of)`

- **What it says.** Whether `who` knows `of` is down: they saw the takedown, the carry or the body (the act's `Noticed`); they saw the shot that
  dropped him (the gunfire's witnesses, the act's lead); or a tie told them (the act's `Told`). False while `of` is up. It takes roles; read-only.
- **The scenarios:**
  - C102-UNSEEN: false. "He's not answering" is true there.
  - C102-SAW-KILLED: true.
  - C102-KNOWS-CARRY (new): knocked out unseen, then carried past her. True, and her Call is still `summon-busy`: a carry is no memory of violence by
    step 4's list, so the line has to read this.
  - xunit `KnowsDownReadsWhatTheyWereToldToo`: told by a tie. Nothing in the Handover tells Inez in play, so the tell is written as `Tell` writes it.

## Evidence (on the tree)

- `bun run sim:test`: **3,400 passed, 0 failed** (150 s wall). It was 3,343 / 31 failed before this round.
- `suborned-sim check`: **489/489** scenarios, each with its replay. There were 484; the new ones are CAM-10, CAM-11, M101-LOGGED, M101-LOOPED and
  C102-KNOWS-CARRY.
- `bun tools/lint/comments.ts`: 0 hits. `bun run typecheck`: clean.
- `bun run test` (TS): 2,744 pass, 10 fail. All 10 are on Vantage's list:
  - three skyline runs and two units tests (CAP-3 on the frozen plan), and drift-auto (r011, r012–r016): failing before this round;
  - Pulse's `rounds.test.ts` bake case and its recorded routes that touch Inez;
  - Waymark's `simview` recorded hash and `check.test.ts` bands: re-record.
- **What the engine changes move.** `hashes --start-text keel-101`, HEAD's engine against this one on the same inputs.
  - 45 runs change bytes. Outside the scenarios edited above, all but one keep their outcome, end tick, Trail, Alert, cash and event log: they hold a
    camera record, which now hashes its incident number.
  - Behaviour moved only where a taped act's subject also reports it: P85-MIXED and N93, Alert 2 → 1 (N93 tests Standing and passes either way).
  - No route script's outcome or log moved.
- Files over 500 lines were split or kept: `GameSight.cs` 800 → 461, `GameOrders.cs` 683 → 672, `ScenariosTakedown.cs` 630 → 610,
  `ScenariosHandover.cs` 1,073 → 1,072, `ScenariosFights.cs` 541 → 541.
- The mission record was not re-recorded (Waymark's, on top of this branch).

## What the mast does to play (for Waymark and Motive)

- **A discreet Listen on Inez** needs a spot out of every cone. Beside her post that was a 0.6 s step; under the mast it is a 37 s walk, or a wait for
  her round to bring her by.
- **Waiting at `beside-inez`** gets the Scout reported as a loiterer after about 45 s (Alert 1, the dial's robots).
- **A Pay or Pressure at her post is always taped** unless the mast is looped, or the act is done at the far table during the two 20 s windows she's
  out of its view.
- **A rival takedown at ambush-2 and route A's Enter** need the two loops in turn: the mast's over the post, then the entrance's.

## Next owners

1. **Latch:**
   - `HostCrewTurn.cs`'s Pay card: call `Tape(i, q)` on the `CrewQuote`, as the people's cards do, and drop the "no tape" comment.
   - "He's not answering" only where `!Game.KnowsDown(caller, leaver)`.
   - The DLL through `bun tools/sim/unity.ts`.
   - The new engine files are `GameFan.cs`, `GameFanShadows.cs` and `ScenariosMast101.cs`.
2. **Fable:** the shut line for a caller who knows he's down (she found him, saw him carried, or was told). Today only "he's not answering" exists.
3. **Waymark:** re-plan the routes and record on this branch.
   - The reference plans meet everything in "What the mast does to play".
   - Motive's start-stance rigid line has N67's lattice as a seed-0 proof.
4. **Pulse:** `rounds.test.ts` "the plain grid walk comes short of the drawn solids": its own comment says to retire the rule now the old mast is gone.
5. **Aster (GDD catch-up):** the one-incident sentence (§05 on camera: a taped act its subject reports is one incident), and `KnowsDown` for the
   host's call lines.
