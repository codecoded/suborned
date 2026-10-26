# Brief 067 · Vantage (level designer): the patrol, the planters, the car points, the building left of the Office

2026-10-26, for batch 0.2.123 (engine 0.33.1, d4361806). Items A, K, L and N of `briefs/067-sky-play-0122.md`.
- **Edited:** `contracts/missions/handover/mission.json` and `contracts/world/civic-block.adjust.json`.
- **Regenerated, not edited:** `contracts/world/civic-block.json` (`bun run block`), `contracts/world/civic-block.cover.json` (`bun run cover`) and `out/world/harbour-district.world.json` (`bun run world`).
- **Scratch:** `out/scratch/vantage/067/`. `tree/` is HEAD plus my two files. It also holds a probe engine: `VantageProbe.cs` and the `vprobe`, `vpath` and `vsee` commands. `cand-*.txt` are the mission checks, and `mk.sh`, `final.sh` and `cand.sh` rebuild them.
- **Picture:** `briefs/067/vantage/yard-patrol-top.png`, a top-down map of the new sweep, its cones, the wait spot and the escort.

## A · The patrol robot moves off the front, to the side of the service entrance

### Where it goes
Block frame, metres (x runs south, y east).

| | was | now |
|---|---|---|
| `patrol-a` (the sweep's north end; it stands here at 0:00) | (67.75, −46.25), yaw 30 | **(61.25, −7.25)**, yaw 210 |
| `patrol-b` (the south end, reached at 50 s) | (54.25, −43.25), yaw 30 | **(76.25, −7.25)**, yaw 210 |
| `patrol-reroute` (where the rerouted robot waits) | (19.2, −47.0) | **(46.25, −9.25)**, yaw 210 |
| site `patrol.facing` (`tools/sim/site.ts`, not my file) | [0, 100] (east, toward the public door) | **[0, −100]** (west, toward the east face it guards) |
| site `patrol.rerouteFacing` (site.ts) | [0, −100] | **[−100, 0]** (north, away from the yard) |

**The sweep keeps the top-down test's shape, so Pulse's routine needs no change.** It is still a straight line, there and back in 100 s, with the rulebook's cone.
- It runs 15 m across the open yard, parallel to the Office's east face.
- It stands 11.65 m out from the service door and faces it. serviceBay and the low bed civic-office-west lie between them.
- It reads the way the forecourt sweep did: a robot walking in front of the door it guards.

**It stays out of the yard's neck by the Dumpster, at (58.25, −17.25).** Every walk into the yard from the north passes through that neck: the escort out to the square, the response unit, and the yard police.

### What it sees
Measured with the engine's own cone and eye height (InSweepCone, EyeSees at 95 cm, no crowd) on the scratch engine (`vprobe`). Figures are seconds of each 100 s round.

| place | seen |
|---|---|
| 'Service lane' (where Vale and the Enforcer come out) | **63**, at 0–31 s and 69–100 s (the forecourt's door was 0–29 s and 71–100 s) |
| the service door | 73 |
| Vale's rear door in the bay (`rival-vale-door`) | 77 |
| the yard stand `service-drive` | 93 |
| the neck by the Dumpster | 37 |
| 'Office entrance', 'Office approach', the cabinet | **0** |

### Checks on the new points
- **V-1 at yaw 210:** patrol-a, patrol-b, the sweep's ground and patrol-reroute are all 100 %. All are also 100 % at yaw 30.
- **G-1d clearance** (`clear.ts`, the audit's body 0.65 m):
  - the line keeps 2.90 m from the bed, 2.98 m from its tufts, 3.2 m from the kerb's police post and 4.5 m from its call box;
  - the wait spot has no solid within 6 m.
- **Placement:**
  - both ends and the wait spot are free cells on cell centres;
  - the line keeps 6.25 m off the service drive, so no car, Vale walk or carry crosses it;
  - svc-ambush-1 is 1.33 m from the south end, and svc-ambush-2 1.12 m from the north end.

### The Reroute still takes it out of the way
- **New wait spot:**
  - From any point of the sweep, the false orders' walk out to (46.25, −9.25) and back is 41–66 s of the 115 s, at 85 cm/s on the walk grid.
  - So the robot always arrives and waits 49–74 s before walking back.
  - It stands on the office plot's north edge by Orbit Walk: 13.8 m from the escort's line, 7.5 m from the response unit's walk and about 10 m from the rival car's way down Orbit Walk.
- **The old spot (19.2, −47) no longer works.** From the yard it needs 121–141 m and 142–166 s, so the robot would turn back short and wander across the square.

### The 30 m gunfire break, and the cone against the yard carry
**The gunfire break takes in the whole yard.**
- An unrerouted patrol breaks into the yard fight and joins it. It then holds the car from within 8 m.
- Route B as composed today ended at Alert 2 with 9 T, the patrol firing on the Scout.

**The cone does not stop the carry itself.**
- By §07 and the engine (TakeScan / ScanEscort), an escorted Vale is seen only by kin or by someone within 3 m. Machines never see it.
- The Take in the bay happens in the car, with nothing to witness.

**What bites is loitering.**
- Route B's Enforcer waits at 'Service lane' from 0:00: noticed at 1:18, reported at 1:58, Alert 1.
- The Scout and Fixer wait in the yard from 5:55: the Scout is reported at 7:14.

**With the Tech's Reroute timed over the wait and the fight, route B wins clean.** Probe plans `plans/rb-rr.plan.json` and `rbb-rr.plan.json` change Motive's rb-ff and rbb-ff in three places:
- the Tech moves to the cabinet at 4:30 and reroutes at 5:05; the patrol is away from 5:50 to 7:45;
- the Tech boards the getaway car once the reroute lands, and drives at car.parks as before;
- the Enforcer comes to 'Service lane' only at 5:50.

### Probe numbers: quiet routes
Mission check on engine 0.33.1 with Motive's 059 candidate plans, composed on each site (`cand-<tag>.txt`).
- MC-1 is **3 routes (A, B, G) in every run**.
- MC-0 and MC-5 failures that are already in `base` are the 059 re-compose list, not this move.

| run | A | B | G | changed checks |
|---|---|---|---|---|
| `base`: HEAD | a 4:52.8 5 T · board 4:27.5 5 T · disable 6 T | b 7:32.2 3 T · board 7:36.1 3 T | g 4:11.1 6 T · board 6 T · kill 2+2+3 | MC-0 28/39 |
| `wb`: the patrol in the yard, plans as they are | as base | b 7:36.2 **9 T, A 2** · board none | as base | MC-3 7/8, MC-10, MC-12 fail |
| `wb-rr`: plus the Reroute in route B | as base | b 7:32.2 **4 T, A 0** · board 7:36.1 4 T | as base | MC-10 and MC-12 ok, MC-3 7/8 |
| `final2`: plus K and L (planters gone, ambush-5 moved, car points) | a 4:42.2 5 T · board 4:27.5 · pressure 3:20.1 7 T (composes now) · disable | b 4 T · board 4 T | g 6 T · board 6 T · kill 2+**6**+3 | MC-0 27/39, MC-3 7/8 |
| `wb-rr-exit` / `final2-exit`: plus the proposed exit rule (below) | a, board and pressure win; disable fails exit-seen-patrol | as above | g and board win; kill and kill-board fail exit-seen-patrol | |

How `final2` differs from `base` in MC-0:
- route-a-pressure now composes and wins.
- fail-entry-seen-patrol can no longer reach its fail.
- careless-scout-idles-pickup ends at Alert 2 instead of 1: its plan still parks the Enforcer at 'Service lane' from 0:00.
- route-g-kill's extra 4 T is a square-fight change (item K): the Scout is downed and captured. It is not the patrol.

### What else follows from the move
1. **The front loses its patrol.**
   - "Entry seen by patrol" (§07 fail; `Enter` tests PatrolWatching at the public door) can no longer happen. MC-3 drops to 7/8.
   - Routes A and G no longer need the Reroute for the entry. It still costs them +1 T and 45 s.
   - Motive decides between two options:
     - drop the Reroute from A and G (A 4 T, G 5 T);
     - adopt the rule in point 2, under which the Reroute clears their way out.
2. **Without a new rule, the yard patrol bites only route B and bodies carried into the yard.** It does not bite the back way that routes A and G use.
   - The proposal for Motive, then Sky: a mirror of entry-seen-patrol at Enter's landing. If the patrol watches 'Service lane' when the Enforcer and Vale come out, the attempt fails as "the way out seen by the patrol".
   - With this rule, the Reroute (or the 38 s gap in each round) is how the cell clears the back way.
   - Probe switch: `PROBE_EXITPATROL=1`, scratch engine only, in GameOrders.cs's enter landing.
   - What it does:
     - route-a, route-a-board, route-a-pressure, route-g and route-g-board still win, because their Reroute already covers the exit;
     - route-a-disable, route-g-kill and route-g-kill-board fail, because their exit at 3:31 or 4:42 falls after the false orders end;
     - fail-entry-seen-patrol's script reaches the new fail at Alert 0, so MC-3 is 8/8 once the fail list names it.
3. **Route B needs the Reroute.** Its plans and the careless twins that park in the yard need re-composing (Waymark): rb-ff, rbb-ff, csip-ff and crns-ff.
4. **Words.** "the entrance sweep" appears in two places:
   - the engine's log texts, `patrol.rerouted` (GameOrders.cs 303) and `patrol.returns` (Game.cs 487);
   - Fable's text.json: `patrol.about` and `watched` ("sweeps the square"), the line "A patrol sweeps this door" (1123), "The patrol sweeps past the door" (1851), and "Mind the entrance" (2247).
5. **GDD for v0.26 (Aster).**
   - **§07 places:** the service door is no longer "out of sight of … the sweep". The patrol sweeps the yard in front of it, and the Tech's Reroute clears it.
   - **§07 routes A and G:** "the Enforcer waits out of … the sweep" no longer applies at the front.
   - **§07 route B:** add the Reroute.
   - **§07 fail list:** "patrol" leaves "entry seen by", or becomes "the way out seen by the patrol" under point 2.
   - **§10 views:** "the patrol sweep" moves from the yaw-30 list to the yaw-210 list.
   - **§10 V-1:** 64/65 points (+1 hidden by design), 19 read from a turned heading. The "47 of 48" is out of date.

## K · The odd planters by the getaway car
**Which ones:**
- **run-sq-south** (2 modules, 12.7 m and 14.5 m from the car) and **run-sq-west** (1 module, 11.2 m from the car).
- These are the two freestanding PlanterRun_4 runs on the square's south-west quarter. I placed them on 2026-10-15 (brief 011, CV-2) only as fight cover for ambush-1, -3 and -5.
- I checked everything with planter, bed, urn or pot in it within 30 m of the car or 22 m of the fountain. The rest is the fountain's four curved beds, the Rocket Lane kerb runs, and the café's palms and pots, all of which belong where they are.

**Removed, not moved**, in `civic-block.adjust.json` → `dressing.planterRuns.removed[]`, with their geometry and a reason each.
- No edge near the car could take them and still shield a post. The car stands on the square's own kerb, so a kerb run lies along the line of fire, not across it.
- The only shield left on the square is the café's terrace wall.

**The blocker's post at the square stand does not move.** "The planter cover on the lane's north kerb, 11 m from the car" is the kerb run run-west-south's road-side point.
- It was cv-072 and is **cv-066** in the regenerated file: (19.25, −50.75), 11.0 m from the car, inside the exit lane.
- run-west-south stays, so §07's sentence still holds.

**ambush-5 moves from (14, −36) to (−3.25, −34.75)**, outside the front-west corner of the café's terrace wall. Without it, CV-2 would hold for only 2 of 5 hostile posts.
- **Cover:** cv-032 (q 589) at 2.7 m, so CV-2 holds at 3/5.
- **Range:** 17.9 m from the car with a clear line to it (Site.Sight), outside 8 m and outside the exit lane (MC-11).
- **The wheelman's walk:** from rival-drop it is 38.4 m and keeps 12.4 m off the car.
- **What I rejected:**
  - (−7.25, −46.75): its walk passes 4.7 m from the car, and the wheelman reached Vale in route-g-board.
  - (−4.25, −40.25): the boarding pavilion's west pier hides the car from it.

**What it costs the square's safe fight (Motive to re-measure):**
- ambush-1 and ambush-3, where the dropped rivals post first, now lurk in the open. guard-ally loses cv-063 and cv-062.
- In `final2`, route-g-kill still wins but the Scout is downed and captured: campaign Trail 2+6+3 against 2+2+3.

`bun run cover`: CV-1 ok (77 points), CV-2 ok (safe 3/5 and 5/5, loud 4/4 and 4/4). It reports ambush-1 and ambush-3 as uncovered, and I have deliberately not taken its planter proposals.

## L · Car points (Keel's ask)
Vale's car is parked at the pickup with footprint 1.82 × 4.73 m and car-clear-cm 50, so it blocks x 76.79–79.61, y −33.865 to −28.135.
- **'Meet driver'** moves from (78.1, −33.7) to **(78.1, −34.2)**: 0.84 m off the tail (it was 0.34 m), still in officeBay, V-1 100 %.
  - The briefing camera (`brief.json camera` = d:Meet driver + (5, −5) m) shifts 0.5 m with it.
- **'Boarding pavilion'** moves from (78.3, −31), the middle of the car, to **(76.75, −34.25)** on the pavement at the car's tail.
  - It loses its `on` and `floor` keys and is a free cell.
  - It is 1.35 m from the driver, so the Fixer's walk ends where it is sent and his turn of the driver is in reach. V-1 is 100 %.
  - The name stays, because routes and scenarios read `d:Boarding pavilion`.
- **block-spot is not moved.**
  - It sits 0.4 m behind the parked car's tail corner, but nothing stands or drives there since brief 059 retired the camera block.
  - It can't be removed from my file alone. Three things read it:
    - `tools/sim/site.ts`: `car.block` and blockFacing, which is the heading Vale's car keeps in the bay;
    - `Site.cs`: CarBlock is required;
    - scenarios N57 and N57b, which send the Enforcer to block-spot + (−3, −3) m.
  - The retirement steps are under Keel and Splice below. Its reason in mission.json now says so.

## N · The building to the left of the Office
**bld/rocket-west/17**: the Rocket Lane terrace on plot rocket-west (area records, residential, 8 storeys, 34.98 m).
- **Where:** block x 77.6–110.5, y −84.5 to −68.5 (plan x −214.5 to −198.5, y −340.5 to −307.6), across Rocket Lane south-west of the Office front.
- **The measure** (`v1by.ts`, a figure every metre over x 40–80, y −60 to −20):
  - at the opening yaw 30, 776 of the 1,303 ground figures round the Office are under half seen;
  - rocket-west/17 hides 532 of them, the Office itself 224, and **bld/rocket-west/18** (27.8 m, x 50.5–80.5) 186;
  - at yaw 120 the culprits are /19 and /18.
- No change now. It is the first case for brief 066's low-city mock (Datum).

## Evidence
- `bun tools/plan/cli.ts --overlay tools/missions/handover/overlay.ts`: **exit 0**, 142 PASS, the same count as before the edits.
  - C-3: 21/21.
  - M-3: 62/62.
  - V-1: 64/65 + 1 hidden by design, 19 read from a turned heading (15 before).
  - CD-1 ok.
- `bun run world`: exit 0.
- `bun run cover`: exit 0 (CV-1 ok; CV-2 safe 3/5 and 5/5, loud 4/4 and 4/4).
- `bun run typecheck`: exit 0.
- Probe runs: `out/scratch/vantage/067/cand-{base,wb,wb-rr,wb-rr-exit,final2,final2-exit}.txt`, plus the vprobe and vpath outputs quoted above.
- Engine scenarios: on HEAD inputs in the scratch tree, `suborned-sim check` gives **341/341 ok** (`scen-base.txt`).
  - I started the 123 scenarios that read the patrol or the moved points on the candidate site. I stopped it at 10:42 without a result: the machine was at load 35–55 from other runs.
  - The suite on the new site is Keel's re-run.
- Tests that pin what moved, as expected: 9 failures in my scratch tree with only my changes. Owners are below.
  - `tools/missions/handover/mission.test.ts`: the PLACES lock.
  - `tools/cover/cover.test.ts`: post-to-cover picks, the empty report, the conformance vectors.
  - `tools/plan/walk.test.ts`: 3 tests that use run-sq-south/0 and the 15-module count.
  - `tools/people/test/walk.test.ts`: 2 tests that use run-sq-south and run-sq-west as fixtures.
  - In the main tree, 3 more cover tests fail ("is what … make today", "CV-1 holds", and the CV-1 failing example). These follow the site and cover being rebuilt out of order, by me and someone else, and clear once sim:site runs after cover.

## Open issues and next owners
- **Splice or Keel, before the batch builds (blocking):**
  - In `tools/sim/site.ts`, set `patrol.facing` to `[0, -100]`, `rerouteFacing` to `[-100, 0]`, and fix its `src` text (the sweep is in the service yard, facing the east face).
  - Then run `bun run sim:site` → `bun run cover` → `bun run sim:site` again, so the site picks up the cover's new sha.
  - Someone rebuilt the main tree's site at 06:29 with my interim points and the **old facing**. It still has ambush-5 at (−7.25, −46.75).
- **Splice: test re-pins.**
  - `mission.test.ts` MOVED gets `['Meet driver', [78.1, -33.7], [78.1, -34.2]]` and `['Boarding pavilion', [78.3, -31], [76.75, -34.25]]`.
  - PLACES becomes `63af1d5f7ff52e2858fdf6ba91237222f0462196ae6d18cba06aa134ef000e13`. With these rows, WAS rebuilds exactly (`places.ts`).
  - Update cover.test.ts and walk.test.ts for the removed runs. Pulse updates people/test/walk.test.ts.
- **Motive (balance):**
  - the exit rule (A2), or dropping the now-useless Reroute from A and G;
  - route B with the Reroute (4 T against 3 T);
  - the square's safe fight with ambush-1 and -3 uncovered and ambush-5 on the café side.
- **Keel (site and engine):**
  - re-run the suite on the new site;
  - the exit rule if Motive and Sky take it;
  - the two log texts in A4;
  - retire block-spot: blockFacing from officeBay's long axis, drop `car.block` and CarBlock, give N57 and N57b their own wait point (today block-spot + (−3, −3) = (74.25, −36.75));
  - expect movement wherever the front sweep's timing or the patrol is read. N57 and N57b also carry a body into the yard, which the patrol now watches.
- **Pulse:**
  - no routine change is needed;
  - the crowd and rounds walk on a grid that has lost three planter modules.
- **Waymark:** re-compose route B (and its careless twins) with the Reroute: `plans/rb-rr.plan.json` and `rbb-rr.plan.json` are the working probes. Do the same for route-a-disable and route-g-kill if the exit rule lands.
- **Fable:** the words in A4.
- **Aster:** the GDD lines in A5.
- **Latch:** the proof shots of the new post and the cleared square, at play zoom. The patrol is played at yaw 210.
