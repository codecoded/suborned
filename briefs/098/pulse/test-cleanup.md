# 098 test cleanup: Pulse's six (2026-11-08)

Sky approved the cleanup ("Do all"). The list is Splice's `briefs/098/splice/tests.md` plus item 6 from Keel's L9 report (`briefs/098/keel/give-and-take.html`, "Open, and whose"). Branch `t3/fix-schedule-ai-test-cleanup`, from `work/098-living-playbook` at 0f75f128 (engine 0.47.1). Nothing is re-recorded: Keel re-records once, after this round and Vantage's merge.

## 1. `tools/sim/sim`: the people extract, rebuilt
- **Command:** `bun run people`, then `bun run sim:site`. This writes `sim/data/handover.people.json`, and the site's `cast.sha256`, from the promoted cast (r016) on today's plan (0.5.6). The takedown and provisional files come out byte-identical.
- **Why it was stale:** the extract was last rebuilt in full before the 075 swap. Since then 080, 089 and 096 edited it in place (clock shifts, names only) to keep the engine's inputs still. 096's commit says so: "a full people regen here also brings drift that isn't this change".
- **What the rebuild brings, all of it from 075:**
  - **The tram stop:** `t1-records` is now `t1-rocket`, 271 m away, with its 24 standing points. Its three kiosk staff `p-t1-records-kiosk-0n` become `p-t1-rocket-kiosk-0n` and are redrawn on their new ids (temperament, need, home and day).
  - **`s1-gardens`:** moves 31 m south (block y -30 → -61 m).
  - **Homes:** 8 people's homes are redrawn by 075's resident counts (`harbour-district.life.json` at ce780639, the 066 height cap: garden-row-east 351 → 70, pylon-row-east 177 → 35, rocket-west 3,502 → 1,751). With them, family ties change for 59 people (54 family ties lost, 52 made; 10 workmate ties lost, 6 made). 79 names follow, because a family shares a surname and a heritage.
- **`contracts/people/handover.seats.json`:** re-baked with `bun run seats`, since it reads the extract. One sit goes: the old kiosk-02 at table 5. The tables and chairs, which give the extract its seat facings, are unchanged.
- **Fixpoint:** running `sim:site` again after the seats bake writes the same bytes. `bun tools/routines/seats.ts --check` and `rounds.ts --check` both pass.

## 2. `tools/routines/rounds`: re-baked
- **Command:** `bun run rounds` writes the candidate (`briefs/022/handover.rounds.json`). `bun tools/routines/rounds.ts --out sim/data/handover.rounds.json` writes the engine's copy, which the test holds to the same bytes because it carries `guard.stops`.
- **It wasn't 40 cm.** The copy was baked by 2026-10-26 (e17d8ca7) at the 17:24:30 clock and never again: not after 080 (17:25:30), not after 096 (17:24:50).
  - Inez's café-door span moves 20 s: -6630..2130 → -6650..2110.
  - Her round follows who sits where, which moved with the city's shifted day. The 15 excursions start up to 4 s earlier or later (the first 0:34 → 0:30), and tables 3 and 6 trade places three times. The "called" vectors move 2.5 s earlier.
  - The guard's visit and the meetings (none) are unchanged.
- **The rebuild in item 1 changes none of this:** the bake before and after it is the same bytes.

## 3. `tools/people/test/day` and `trust`: brought to today's truth
- **`day`, "the r013 cast cannot":** r013 still can't play, now for four reasons:
  - the waitress: none of the 5 has the affair (the lovers' canon since r016; r013 drew no secrets);
  - the driver: no tie to a councillor;
  - 11 guards at the Office door when the guard's window opens;
  - the councillor doesn't reach Meet driver by the meet.

  The window, the clock time and the meet now come from `mission.json` (roles `guard` and `councillor`, `clock.startsAt`, through `cast.ts` `windowOf`) instead of 080's literals (1:52-5:30, 17:26:22, 6:10; today 1:52-5:10, 17:26:42, 5:50). The counts 5, 7, 11 and 11 are r013's and stay literal.
- **`trust`, "another mission or plan text is another file":** the test replaced `"startsAt": "17:24:30"`, which the mission no longer has (17:24:50 since 096). So the text was unchanged and the hashes matched. It now replaces the mission's own `startsAt` and asserts that the text changed, as its plan half already does.

## 4. `tools/life/presentation`: Sunset Way east's loop
`walkOff` took the class section for every road. Sunset Way east has its own 3.25 m pavement since 075 (kerb 1.25, walk 2, no frontage band), so the access class's walk band (kerb 1.5) ran 25 cm into the buildings on both sides and the loop dropped. `walkOff` now reads the road's section through `tools/plan/sections.ts` `sectionOf`, the reader the world generator and the plan checker use.
- **What changes:** only the two roads with their own pavement. `sunset-east/0/0` is back, with both pavements. `harbour-market/0/0` (Market Street, its own 2.75 m) goes from one pavement to both.
- **Counts:** figures 2,694 → 2,707, walkers 1,375 → 1,388, cars unchanged, dropped loops 1 → 0.
- **Where it lands:** in the git-ignored `out/world/…presentation.json`. The next build's street life picks it up (Latch).

## 5. `tools/drift-auto`: the people ledger
- **Command:** `bun run drift:auto --objective people --base 472cb99a --cause "…"`. 472cb99a is the ledger's last declared state (Pulse's 089 names), where r012–r016 replay OK.
- **What moved the hash:** four files since then.
  - 096's bay `use` (`tools/plan/block.ts`).
  - 097's zone twins and `_Play` pieces (`tools/world/build.ts`, `gen/kitrefs.ts`, `gen/street/capstone-rules.ts`). The people hash takes these because the overlay's sight draws the world.
- **Result:** every one of the 5 runs replays as it does at 472cb99a. So the cause covers no rule (`--rules none`), and the causes declared for earlier hashes are carried forward. Confirmed: 5/5 people runs replay OK under the real ledgers.
- **Why I didn't name P-6, P-7 or P-11 to P-16 again:** those moves are each run's differences from its own recording (Oct 14–16), and they're already declared for earlier hashes. Splice's run used HEAD as the base, and HEAD's own ledger declared nothing for its code, so the tool refused ("HEAD is no state to compare with").
- **Dialogue is still red here, and it's Fable's.** Fable's declaration (8d39c4af) was for dialogue hash 9c8f54a8. Keel's L9 rounds (26c03d32, 6a5c0e75, 2e449f1d) then changed `contracts/rules/rulebook.schema.ts` and added `rulebook.schema098b.ts`, which the dialogue hash takes by name. So today's hash is 953431aa, with nothing declared. `bun run drift:auto --check --objective dialogue --base 8d39c4af` shows every one of r017–r019 replaying as at 8d39c4af, so it's a no-rule declaration: `bun run drift:auto --objective dialogue --base 8d39c4af`.

## 6. The dial on the cast's spots: the engine's, written up for Keel
**What happens.**
- **The code:** `GameDialPeople.Passing` (`sim/Suborned.Sim/GameDialPeople.cs:108, 123`). A passer-by whose destination draw is a door (`population.json` `passers.dest`: door 3 of 10) picks among the site's public entrances that are crowd places.
- **The doors:** the Handover has two, and both are cast posts:
  - `cafe-entrance`: Inez's post all mission (the people file's p-cafe-14, out of doors there from -6650 to 2110 s; the site's `d:Café door`);
  - `office-entrance`: the door guard's post (the site's `guard.post` `d:Office entrance`, held by his role through the mission).
- **The point:** the stop's point is the people file's door point (`CrowdPlace`). That is the cast's own stand: `cafe-entrance` [-1030, -2900] with Inez at the entrance's stand [-975, -2925], and `office-entrance` [6400, -4125], which is `d:Office entrance` itself.
- **The result:** every door visit, about 30 % of the passers-by, heads for a cast member's own spot. Under L9 the cast is a tenet that is never moved, so they wait beside it. Keel's report counted seven on Inez's spot.

**Why it isn't a data fix.** `population.json` can't say it: `PopulationRules` rejects any key it doesn't know (`Population.cs:40`, `n.Only`). The door choice is code, and the people file's door point is the right point for the door itself.

**The law I propose (crowds are Pulse's):** an added person's stop is never a place a cast member holds over that stop.
- **(a) Doors.** At a door a cast member holds, an added person goes in from beside the holder. Its going-in point is the nearest free point within the door's reach that stands at least two body radii clear of the cast's stand. That is how the beat already keeps its points off the door (`Population.cs:196`, `doorOutCm`). Where there is no such point, the door is no destination while it is held, and the draw falls to a seat or the way through.
- **(b) Chairs.** `ChairFree` (`GameListenSpot.cs:121`) checks `Walkers` only, and the crowd leaves the cast out (`GameCrowd.cs:151`). So a chair a cast member's stop names over the stay should not count as free. No cast chair falls within the clock today (the door guard's café chair is from 8:10, the close), so (b) moves nothing now; it keeps the law whole for a mission whose cast sits down.
- **(c) Beats.** Check that no beat point (2 m out of a door) or police post falls within two radii of a cast stand.

**A question for Keel.** The people file's own crowd also goes in and out at the Office door while the door guard holds it: 38 of them between 0:00 and 8:10, the shift change at 5:10. None goes through the café door in that window. If they wait beside him the way the added people do, the same going-in point (a) fixes them too.

## What the input change moves (engine 0.47.1)
- **How I compared:** `suborned-sim hashes --start-text pulse` with one DLL, on a `git archive` of HEAD's inputs and on this branch's. The inputs hash is held the same, so only behaviour counts. 489 of 517 items move.
- **Route scripts:** all 42 move from their first checkpoint (0:10), because the city's people walk otherwise from the start. None changes its outcome, fail code or end tick; route A still wins at 4:20.2 (tick 2602).
- **Motive's fight scripts:** all 3 move, with the same outcomes and ends.
- **Scenarios:** 444 of 472 move. These 28 hold: S03, S04, S06, S07, S19, S24, S28, N20e, N93, the six SEED-*, CLOSE-VERDICT, SHOT-INTAKE, CARD-COMING, SHOT-STREAK, CAM-5, CAM-8, CAM-9, BOX-1, KERB-2, KERB-3, KERB-5, PARK-1 and PARK-8.
- **`suborned-sim check`:** 467/472, was 472/472. All five are Keel's:
  - **DP1:** the drive session's start state differs. Re-record with `suborned-sim drive-session`.
  - **N130, N131:** written to the old round ("table 3 at 0:34"). Today's round goes to table 6 at 0:30 and table 3 at 1:31. N130 fails "after 5.8 s of walking (the card said 8 s)"; N131 fails "out 9 s later (13 s …, her round due out at 0:34)".
  - **LS-CHAIR:** on Inez's new walk the Listen takes no chair spot, and `n!.Value` throws instead of failing (`ScenariosListenSpot.cs:66-68`).
  - **N65:** clerk 34 (p-records-office-34) is redrawn into a rocket-west home, so he no longer walks past the drop spot, and `g.Reps.First` throws (`ScenariosTakedown.cs:411`).
- **Which input does what:** on a copy with only the new rounds file, 423 items move, and DP1, N130, N131 and LS-CHAIR fail. The people extract adds N65 and 66 more moves.

## For Keel, after Vantage's merge
1. Re-record: the mission record (c059 → next), DP1's drive session, and whatever the record pins (simview's final hash).
2. Rewrite N65, N130, N131 and LS-CHAIR to their rules, not to one round.
3. Item 6: the law above, (a) to (c), and the question.

## Regenerating after Vantage's merge (the café passage, Popovic's car stand)
The generated files read one another, so order matters:
1. `bun run people`
2. `bun run sim:site` (site, people extract, takedown, provisional)
3. `bun run seats` (it reads the extract)
4. `bun run rounds`, then `bun tools/routines/rounds.ts --out sim/data/handover.rounds.json` (they read the extract and the site)
5. `bun run sim:site` once more: it must write the same bytes (the seat facings come from the seats file)

Finish with `bun tools/routines/rounds.ts --check` and `bun tools/routines/seats.ts --check`. If Vantage's change touches a `.ts` file the people hash takes (`tools/plan`, `tools/world/gen`, `tools/world/civic`), declare people again: `bun run drift:auto --objective people --base <this branch's head>`.

## Checks (Mac Studio, through `bun run guard`)
- **`bun run test tools/people tools/routines tools/life tools/sim tools/drift-auto`:** 416 tests, 412 pass, 1 skip, 3 fail. All six of this round's own failures pass. The 3 left:
  - drift-auto ×2: dialogue only (r017–r019; Fable's, above).
  - `tools/simview` "a dump ends on the recorded final hash": c059's hash, which this input change moves (Keel's re-record). The `tools/sim` filter takes simview's folder too.
- **Full `bun run test`:** 2,668 tests in 5:24, 5 fail, 44 skip. The skips are the git-ignored kit and DecoKit outputs this fresh worktree lacks. The 5 fails:
  - the 3 above;
  - `tools/route-diff` "a route script made 5 s late moves": route-a-board loses (closed-in) at HEAD too, as Keel's report says;
  - `tools/decokit/collide`: BikeRack_Play's volume, Vela and Keel's on Splice's list.
- **`suborned-sim check`:** 467/472, was 472/472 at HEAD. The five are DP1, N65, N130, N131 and LS-CHAIR (above).
- **`bun run typecheck`:** clean.
- **Files:** `tools/life/presentation.ts` stays at 677 lines, as at HEAD (over 500 already; not grown). The two people tests are 353 and 134 lines.
