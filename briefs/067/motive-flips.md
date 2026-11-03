# Brief 067 · Motive: engine 0.34.0's outcome flips, judged

2026-10-26, for Aster. Measured on a frozen copy of Keel's 0.34.0 build (`out/scratch/keel/bin`, 11:31/11:39) over the tree's inputs at 11:55. The inputs hash, 12390659450729014507, is the one in `keel/facts-0.34.0.txt`. Probes ran through `bun run guard` under `out/scratch/motive/067f/`; nothing in `sim/`, `mission.json` or `text.json` was touched. Evidence: `briefs/067/motive/flips/results.txt` (regenerate with `tools/results.sh`; what each probe tree is: `tools/README.txt`).

## 0 · Verdicts
None of the eight is a bug, and no value moves.

| # | flip | verdict |
|---|---|---|
| 1 | BAL-YARD-FOCUS-LATE's board and go now loses rivals-reach-vale | **the rule working** (K3 a: hiding in a held car loses); a step-out wins it back |
| 2 | route B with the Reroute is 4 T | **the rule working** (Pay 2 + Mislead 1 + Reroute 1) |
| 3 | agent and every differ in the safe fight | **the rule working**. `agent` is §06's rule and the reference. **The direction is the reverse of the note:** route-a-pressure *wins* under agent and *loses* under every. Trail is the same either way |
| 4 | fight-lab flips | **the rule working**: the today win is heavy, not cheap. Its 6.4.8a FAIL is the known police-post gap, which Sky kept in today (§4.1: his call, with numbers). Not on the list: P-A's new trigger is matrix upkeep (§4.4) |
| 5 | S11 re-composed | **the rule working**: 0.33.1 left the Enforcer at 1 health; the new play is the one §06 asks of the support |
| 6 | N125's culprit is a rival | **the rule working**: the yard patrol doesn't break into the guard fight at the first shot, so a rival lands the last hit |
| 7 | SQ1-WALK 8.7 m against my 8.9 m | **the rule working**: the rule is beyond the 8 m hold; 8.9 m was my probe's path |
| 8 | rivals shoot operatives in the car | **§06 as written** (Board: "can still be hit as a target beside cover (×500)"); N185 relies on it rightly |
| MC-11 | Aster's clause | **agreed**, with one refinement (§9) |

Two text nits for Keel: the `FinalIntent.cs` comment (§3) and SQ1-WALK's description (§7).

## 1 · BAL-YARD-FOCUS-LATE: board and go with the locks 3 s late
- **What happens** (`scripts/rbb-ff-late.script.json`, fire orders +3.0 s, no Go hook):
  - the Tech drove the car and sits in it; the Enforcer boards with Vale at 7:29.3;
  - the Scout boards at 7:33.1 while rival-3 (the blocker) still holds the car;
  - the Fixer falls outside at 7:33.2, so nobody of the cell stands outside a held car;
  - rival-2 goes for the car at 7:33.3 and fires as he walks: 140 ‰ from 10 m down to 5 m (750 × 750 untrained × 500 walking × 500 beside cover), then 375 ‰ at point blank, which downs the Enforcer in the car at 7:40.7;
  - rival-2 takes Vale at 7:42.2.
- **Why it's the rule:** M067-K3 (a) as amended (held car, nobody standing outside) and §06's "hiding in a held car loses". On 0.33.1 the same state sat in the car to the script's end, and my M059 sweep counted that as holding. That count was made on an engine that had no way to lose this state.
- **The player's way out works** (`rbb-ff-late-stepout`):
  - the Scout steps out at 7:35.0 (ordered at 7:34.0, 0.7 s after the walk shows);
  - rival-2 turns back to cover at 7:35.1;
  - she drops him at 7:37.5, the crew breaks and the car is free.

  The walk to the car takes 8.9 s, so the player has time to see it and act. K3-BACK already tests this.
- **M059-B4's "3 s of slack" now reads:** fought out (rb-ff), 3 s late still wins. Board and go, 3 s late, wins only if someone steps out when the rivals come for the car.

## 2 · Route B with the Reroute: 4 T
- bal-rb-ff's ledger: `turn.pay:driver` +2, `mislead` +1, `reroute` +1 = 4 T.
- That is motive-square §5.2's rb-rr (7:32.2, 4 T, A 0). Without the Reroute, route B is reported loitering at 1:53.4 (9 T, A 2), because the patrol now sweeps the yard.
- 3 T was route B before brief 067 A moved the sweep. The §07 route B line for v0.26.0 (motive-square §5.4) adds the Reroute by 5:05.

## 3 · "agent" and "every"
- **They are not Trail rules.** They are `ai.intent.final`: who a hostile has cause to fire on in the final fight.
  - GDD v0.25.0 §06 reads "Cause is per agent, in the final fight as before it … [S, on trial in play]". That is `agent`, and the rulebook ships it, provisional until Sky confirms after playing it.
  - `every` is the control (the rule before engine 0.20.0).
  - SQ2 names "cause" on purpose: an advance with no cause walks up and doesn't fire (motive-square §3). So it follows whichever setting is in force.
  - **The reference is agent.**
- **Measured both ways** (the 41 route scripts; `root-every` changes only that word):
  - route-a-pressure **wins under agent** (3:53.7, 7 T, A 0) and **loses under every** (rivals-reach-vale, 3:20.9).
    - Under every, rival-1 takes the Scout as his target at 2:59.1, before she draws, and the rivals drop her at 3:06.1.
    - Under agent, she is nobody's target until she draws (3:03.3).
  - fail-entry-seen-patrol: none (8:30) under agent, cell-down (6:15.9) under every.
  - Five failure rows move only in time.
  - **Trail is identical in all 41.**
- **Keel:** the comment in `FinalIntent.cs` F15_ROUTES says the reverse ("rivals-reach-vale under agent, a win under every"). Only the comment needs fixing; the assert holds either way.
- **The Trail rule Sky approved** is `gunfire-trail`: +2 T on the first shot at each person, camera or car, hit or miss, plus +2 hit and +2 down, so a dropped guard costs 6 T.
  - Sky approved it on 2026-10-15 (brief 011 addendum Q1). Brief 027 tuning §10.6 (2) confirmed the per-target reading, and the engine charges it per target.
  - Final-fight shots are untraced, so agent and every can't move it.
  - GDD v0.25.0 §06's cost line ("a dropped guard = 4 T") is stale. motive-square §6.3 has the v0.26.0 line.
- **For 0.34.1:** route-a-pressure's agent win leans on rival-5 firing nothing for 7 s: a kite step at 3:05.7, advances at 3:08.3 and 3:11.1, then the break at 3:12.7. That is the ping-pong 0.34.1 ends, so the outcome may move; the verdict doesn't. If it loses, that is the Pressure route's price (no warning, all five up), already on Waymark's re-compose list (motive-square §1.4).

## 4 · Fight lab
### 4.1 today `L3:tight-enfgo-carfree-runkite` wins
- **The price is heavy:**
  - 3:54.2, 30 T (34 with cases), A 2, Standing 2 → −1;
  - the Tech down and captured (a Talker at questioning 3), 61 % health lost;
  - 27 shots at the cell after Vale came out;
  - slower than every quiet reference win, at 4–6× their Trail.

  It is a heavy debrief (tuning.md §3.1), so §6.6's first condition, no cheap loud win, holds.
- **Its FAIL is 6.4.8a alone** (traced, results §4):
  - from 3:03 to the end, officer-1 holds police-1 (17.7 m from the car) and officer-2 the café-wall cover beside it, cv-030 (17.6 m);
  - the Enforcer stands beside the car from 3:18 to 3:50, 17.3–17.4 m from them: beyond their 15 m effective ring and their 16 m max;
  - nobody fires on the police side after security-1 goes down at 2:55.1, so the kite alarm lapses and they hold.
- **It is a known gap that Sky kept:**
  - Pulse measured it in brief 027 (`pulse/retest-fixes.md`): police-1 17.7 m, police-2 19.4 m, police-3 12.2 m, police-4 18.6 m; "nobody comes for it".
  - Sky kept today that way on 2026-10-20 (tuning.md §7.8): the police contest for Vale stays lab-only.
  - The lab's contest officer is why the same row loses under lab (enforcer-down 3:08.5).
  - The run-speed reason in the rulebook ("in the final fight the police go for him and the car") is true of the lab only.
- **Why it shows now:** on 0.33.1 the Tech kept firing on officer-2. The officers closed to a planter (cv-062) and to the car itself (cv-001), and shot the Enforcer down at 3:20.3. On the bare square, with SQ1 keeping police answers out of the hold and the run at 3.6 m/s, the kiter keeps them parked.
- **My "no kite row wins"** was a 0.33.1 measurement in op-run-speed's reason, not a rule. It is corrected in `motive-values.json`.
  - At 3.2 m/s (the fallback) the three tight enfgo-carfree rows all lose (3:05.7, 2:56.5, 3:06.2), and at 2.8 m/s too.
  - I keep 3.6: a heavy win by a 0.3 m-margin perfect player is inside tuning.md §10.3.
- **Options for Sky, via Aster** (measured on `root-pp`):
  - (a) leave it;
  - (b) Pulse's data alternative: police-1 to (−5.0, −44.0), 14.1 m from the car, and police-2 to (−6.0, −45.0), 14.8 m, each with a line to the car and outside the hold and the lane.
    - The row becomes a contested heavy debrief that passes: 31 T, 2 down, 1 captured, 72 %.
    - One more today kite row wins heavy: `L3:tight-focus-at208-runkite`, 4:25.7, 26 T, 44 %, pass.
    - Of the other 82 rows of the today and lab matrices, three lab rows move only in time or Trail, with the same outcomes.
    - Of the 41 routes, only fail-loud-enforcer-down moves: 3:18.8 to 2:53.3, the same fail.
    - The other five presets were not re-run.
  - (c) promote the lab's contest officer.

  (b) and (c) both change today in the way Sky declined on 10-20, so neither is a crew change. My lean is (b), the next time Sky plays a loud run: it is data, and it reads, because the officers stand where they can shoot the car.

### 4.2 lab-kite-off: six rows flip to wins
- The preset is the lab with the answer to fire switched off: a control, never the game.
- Its wins are the cheap ones the answer exists to stop. Its own triggers say so: 6.4.1 (`L3:tight-kite` wins with 1 hit taken, 5 % lost) and 6.6.2 (the Tech downs 3 before that side fires).
- They flip from sealed to wins because the faster pace gets Vale to the car (4:16–4:30) before the seal (4:39.3). Measured: at 1.4 m/s walk and 2.8 m/s run (`rb/pace140`), all six fail sealed at 4:39.3 again.
- So on the bare square the answer to fire carries the loud fight. Today and the lab both have it on. No action.

### 4.3 lab-cover L3 and lab-snap-off `km10-…-at208-runkite`: heavy wins become losses
- They now fail enforcer-down at 2:56.6 and 3:47.4.
- A loud row that fails is inside §10.3 (a heavy debrief or a failure). No action.

### 4.4 Not on the list: P-A's verdict, pass to FAIL
- today and lab-cover gain the trigger "6.4.5 P-A downs 1 op" (today: the Fixer down, 38 %). The outcome stayed a success, so Keel's outcome list didn't show it.
- **P-A replays route-a-board's recording to 4:09.0 (tick 2490), then hands over to the lab's player.**
  - With my 1.8 re-composition in its place (`sq-route-a-board`, which on its own wins 4:15.5 at 0 % lost), today's P-A passes (5 %) but the lab's fails (38 %).
  - The handover tick belongs to the old recording, so this is matrix upkeep, not a balance verdict.
- P-G and P-B failed rivals-reach-vale on 0.33.1 too: stale recordings, on Waymark's list.
- My fixtures on the bare square: sq-ra-h3 17 % lost, rb-ff and rbb-ff 33 %, nobody down.

## 5 · S11
- On 0.33.1 the loud front left the Enforcer at 1 health.
- Keel measured the old composition on 0.34.0: at 1.8 m/s the drawn crew is at its posts 17 s before Vale comes out, and one hit more downs him (3:33.5).
- **The re-composed play:** the support breaks the drawn crew while the Enforcer is inside (the crew breaks at 3:19.4, Vale comes out at 3:19.7). It wins at 3:59.8, 44 T, Alert 2.
- That is what a player does and what §06 asks of the support ("the others support him"). The scenario still tests what it did.
- **Waymark:** this is the shape for MC-2's loud route.

## 6 · N125
- On 0.33.1 the forecourt patrol broke into the guard fight at the first shot. The yard patrol doesn't break at the first shot (0:37.3): it leaves its sweep for the alarm at 0:45.4 and breaks toward the gunfire at 1:20.9.
- Gunfire draws the rivals at 0:37.3. Their car turns in 16 s later (0:53.3) and stops at the Office front at 1:10.9.
- rival-2 drops the Enforcer at 1:30.5, before the response unit (on the street from 0:51.4) reaches the front.
- The fail camera names the last hitter, a rival.

## 7 · SQ1-WALK at 8.7 m
- The rule is "a walk to a post goes round the hold", meaning beyond fight.hold's 8 m.
- 8.9 m was my probe's own path, which I wrote into the check as a number. Keel's assert (> 8 m) is the rule, and 8.7 m clears it by 0.7 m.
- **Keel:** the scenario's description still says "8.9 m or more from it all run". Make it "beyond fight.hold (8 m)".

## 8 · Operatives hit in the car (N185)
- §06 Board: "In the car an op is off the street and doesn't fire, and can still be hit as a target beside cover (×500): hiding in a held car loses."
- **In N185:**
  - the blocker, at 11 m, hits the drawn Fixer and Scout in the car at 281 ‰ (750 effective × 750 untrained × 500 beside cover);
  - they fall at 5:47.1 and 6:00.6;
  - the Enforcer aboard has no cause left (never drew, never fired, Vale in the car), so nobody fires on him after he boards: cause per agent, as §06 has it.
- N185 relies on this rightly.
- **An edge, no fix now:**
  - the Tech, holstered and nobody's foe, stands off and keeps K3 (a) off, and SQ2 has nobody to advance on;
  - with Vale aboard, the mission's close doesn't end the run (`horizon.open`, Redline E8);
  - N185 ends at the seal only because the guard's empty post called the response. A safe fight with no seal would wait on the player.
  - Go stays refused while the car is held, so waiting buys nothing. It is not an exploit.

## 9 · MC-11's clause (Aster's call): agreed
- **loud.json:**
  - the cell stands 1.9–3.3 m from the car from before 3:40;
  - the response unit notices the Fixer at 3:43.2 and 3:43.9;
  - when Vale comes out (3:45.7) the unit joins where it stands: security-1 at police-3 (the blocker), and security-2 5.7 m from the car, engaging, with his join cover cv-033 15.0 m out.

  That hold is the cell's own doing: they waited at the car in the unit's sight. It is a body that came for the car.
- **The refinement, for Waymark:**
  - count `car.held.post` when the holder's post is a mission-placed stop (an ambush spot or a police stand), whether he stands at it or walks to it: rival-4's walk to ambush-4 is the case SQ1 fixed;
  - always count `car.held.answer`: an answer to fire never comes for the car;
  - skip a response unit's join point.

## Open issues
1. The square's police posts (§4.1): Sky's call, via Aster.
2. P-A's handover tick (§4.4): Keel, after Waymark re-records route-a-board. I judge the row then.
3. Re-read route-a-pressure on 0.34.1 (§3).
4. GDD v0.26.0: §06's cost line (motive-square §6.3) and the held-car line (motive-square §6.2) stand as drafted.

## Handoffs
- **Keel**
  - **Inputs:** this file, §3 and §7.
  - **Done-check:** the `FinalIntent.cs` comment and SQ1-WALK's description corrected; after Waymark's re-record, P-A's handover tick re-picked and the matrix re-run.
  - **Next owner:** Motive (P-A, route-a-pressure on 0.34.1).
- **Waymark**
  - **Inputs:** §9's MC-11 clause; S11's play for the loud route.
  - **Done-check:** MC-11 passes with the clause as refined.
  - **Next owner:** Keel (the matrix).
- **Aster**
  - **Inputs:** §4.1's options, for Sky.
  - **Next owner:** Sky.
