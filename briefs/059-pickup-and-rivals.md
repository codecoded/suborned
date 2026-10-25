# 059 · Route B's hand-off at the service door, and the rival car's arrival

**State:** designed 2026-10-24 (Waymark). Not built. Sky approves first; then the GDD changes (a new version), and only then the rulebook, sim, mission.json and Unity.
**Asked by:** Aster, from Sky's two notes after playing 0.2.99 (2026-10-24):
- "I'm bit confused on the driver moving car to service lane route. Vale walks there. I thought the driver should wait for Vale then take him to the service entrance?"
- "The rival gang also, need to be coming in from a distance, not just standing around at the pickup point."

**Read:** GDD v0.24.0 §01, §07, §09; `contracts/missions/handover/` (mission.json, routes, text.json); brief 018 (the getaway), brief 023 (`briefs/023/motive-0.8.2.md`: the service drive and car pace), brief 054 (Watch only when it shows something), brief 058 (cameras; Motive, in progress); engine 0.29.0 (`sim/Suborned.Sim/Game.cs`, `GameFight.cs`, `GameCars.cs`, `GameOrders.cs`); the host (`unity/Assets/Suborned/Runtime/Play/HostViews.cs`); mission check c034 (38/38).
**Picture:** `briefs/059/routes-top-down.png` (both routes over the site; drawn by `briefs/059/routes.py` from `sim/data/handover.site.json`; numbers in `briefs/059/measures.json`).
**Next owners:** Sky (approve, three questions) → Aster (GDD v0.25 §07) → Keel (engine), Pulse (rival routine), Motive (prices and balance), Vantage (points) → Splice (site export) → Glyph and Fable (what the player sees and reads) → Latch (port) → Waymark (re-compose and re-record the routes) → Suture (play).

---

## Recommendations

### 1 · Route B: the driver brings Vale to the service door
1. **The front stays ordinary.** The driver waits at the official bay as always. Vale comes out at 5:50, reaches his car at 6:10 and gets in (in by 6:13). The pickup camera, the official door and the rivals' lookout all see an ordinary pickup, which is the point.
2. **The turned driver then drives him round.** From the official bay along Apron Road and up the service drive to the service bay: 34 m (12.4 m of road at 5 m/s, 21.5 m of drive and bay at 2 m/s), **13.3 s**, parked at **6:25**. This is the car pace Motive set in brief 023, on the engine's drive grid.
3. **The hand-off is the Take from the back seat.** The Enforcer waits by the service door (route B already sends him there at 0:00) and takes Vale inside the car: 38 s, no struggle, nothing to witness (§07 the Take, unchanged). Only the Enforcer must be there for the Take. The window runs from 6:25 to **8:30**.
4. **If nobody comes, the driver gives up.** At 8:30 he drives Vale on east along Apron Road, and the mission fails ("Vale leaves in his car"). The close stays at 8:30 and keeps its meaning. A Take begun before 8:30 holds him: he waits for it to finish.
5. **The getaway is a 3.5 m carry, by design.** A new order on the getaway car, **Drive**: any operative in it drives it to one of the mission's car stands. The Handover gets a second stand on the service drive, nose out, 1 m behind Vale's car. From the square it is 121 m, **28 s**. The carry from Vale's back door to yours is **3.5 m (2.5 s)**, against today's **67 m (48 s)** on foot, which is where Sky's run was lost.
6. **The price of turning the driver doesn't change.** Pay is $70,000, 50 s, +2 T. Help on his cash need is $40,000, 50 s, +1 T. What he does is the same size as today: he moves Vale's car to the yard, only now with Vale in it. Only the line's words change ("Brings Vale round the back"). Drive costs $0 and 0 T, and takes the drive's seconds.
7. **The timings hold.**
   - The Take is done at about 7:05, **1:25 before the 8:30 close** (MC-5 needs 1:00).
   - With the rivals tipped at 3:10 (as route B does now), they are away until **7:44**, and the cell leaves at about 7:20.
   - The seal only runs in a loud fight. The near-zero carry removes the walk it used to catch.
8. **No new route needs the driver.** MC-4 is unchanged: without the driver, routes A and G still win.

### 2 · The rival crew drives in, drops a team, and parks
1. **Why they "stand around" today:**
   - The engine flips them on at 3:10 at one fixed point (`rival-park`, `Game.RivalsTimeline`, `RivalsPose`). Nothing drives or walks.
   - The host draws three figures on that point, 0.9 m apart and facing one way. It never draws the rival car: only `car/official` and `car/getaway` have views (`HostViews.cs`).
   - With a turned driver they jump to Vale's spot by the service door at 6:10.
   - When the final fight starts they vanish, and a fresh crew appears at the ambush spots once the ambush springs (`GameFight.StartFight`, `Present` false until then).
   - So they aren't drawn too early. They pop in from nothing, standing.
2. **A crew of five in one car, seen coming:**
   - **2:35** the car turns in from Orbit Walk (from the Sea Gates side).
   - **2:46** it stops by the square and drops three men, who post round your car.
   - It then goes round by Rocket Lane and Apron Road (16 s) and parks across from Vale's car at **3:10**, §07's beat.
   - A lookout gets out to watch the official bay. The wheelman stays in the car.
   - Before 2:35 they are nowhere on the map.
3. **They know your car.** That is why three of them post round it. Your car is the prize they mean to steal from you, so the ambush at the extraction is visible and readable. If you drive your car somewhere else, the three walk after it.
4. **Every count in the final fight now has a body:**
   - The base is 5: the three posted, the lookout and the wheelman.
   - The false tip sends the car and its wheelman off the way they came, for 3:50 (−1).
   - The turned driver warns the lookout off before 5:40: "the pickup's watched". The lookout walks away (−1).
   - So route B fights 3, as today.
5. **Rivals reach Vale is now physical.** If the rival car is still at its post when Vale's car leaves the bay, it follows (3.9 s to the drive mouth). The wheelman walks in (11 s) and takes Vale if he gets there before the Take is done. The false tip is still route B's answer.
6. **If you have Vale before they arrive** (route A-pressure has him at 2:41.6, before the 2:46 drop), the car comes straight to your car and all five get out there. The fight still always happens. Gunfire still draws them within 16 s.
7. **Watch can learn the timetable** (consistent with brief 054, which offers a Watch only where it shows something actionable):
   - A Watch on the rival car, once it is parked, shows their next move on a clock pip.
   - It also shows the 20 m ring at which the posted three spring.
   - A Listen on the driver adds one fact: "That car's tailed us all week". That is the tail rule.
8. **Everything replays exactly.** The car's entry tick is computed back from the 3:10 park, so the timetable is a fixed function of the site and the rulebook. Every move is on the drive grid or the walk grid, in whole ticks, and hashed.

---

## Questions for Sky (three, each with my pick)

1. **How does the cell get away from the service door?**
   - **(a) My pick:** bring your car round behind Vale's, using the new Drive order on the car. The carry is 3.5 m, and routes A and G can use it too, because they come out of the same service door.
   - (b) Leave the car in the square: a 67 m carry on foot, as in the run you lost.
2. **Should the rivals post round your car in plain sight from 2:46?**
   - **(a) My pick:** yes. You see them arrive and post, and the ambush is something you can read, tip, avoid or fight.
   - (b) They wait in their car by the pickup and drive to your car only when you have Vale. That keeps the square clear and the ambush hidden until it comes.
3. **When the driver isn't yours, what happens once Vale is in his car?**
   - **(a) My pick:** it drives off at 6:13, and the mission ends. That is your note read literally: "comes out as usual and gets in".
   - (b) He stands by his car until 8:30, as today. No shipping route uses that wait. Today it gives a Take at the front under the pickup camera, which fails.

---

## 1 · Route B in detail

### 1.1 The drive (drive grid, car pace)
| leg | from → to | length | road / drive | time | when |
|---|---|---|---|---|---|
| Vale gets in | the pavement at the official bay | — | — | 3 s (`vale-in-s` [S], new) | 6:10 → 6:13 |
| the drive round | officeBay → Apron Road (east, 12.4 m) → service drive → serviceBay, nose in | 34.0 m | 12.4 m at 5 m/s, 21.5 m at 2 m/s | 13.3 s | 6:13 → 6:25.3 |
| the wait | serviceBay | — | — | until the close | 6:25 → 8:30 |
| no Take: he drives on | serviceBay → service drive → Apron Road east, off the window | 47.9 m | 31.4 m road, 16.4 m drive | 14.5 s | from 8:30 (the fail lands as he pulls out) |

- The path is today's `S.CarServicePath`. `CarProfile` paces it. Only the start moves: from 5:40 (driverLast, with no one inside) to Vale-in.
- 5:40 stays "last chance to turn the driver": from then on he stands at the car, on duty, waiting for Vale. The deadline, the price card and the `driver-too-late` refusal stay as they are.

### 1.2 The hand-off at the service door
- **Who must be there:**
  - The Enforcer, within act reach of Vale's rear door. Only he takes Vale (§03).
  - The turned driver, standing. A driver who is down drove nowhere, as today.
  - Nobody else is needed for the Take.
- **The Take:**
  - It is a Take on Vale's car while it stands in the service bay with Vale inside.
  - It is in-car (`TakeInCar`): 38 s, no struggle, no witness.
  - At the front, a turned driver's car is never taken: he is already pulling away. A Take ordered there is refused with a new code: "He's bringing Vale round the back."
- **The window:** from the car parking (6:25) to 8:30, **2:05**. Rivals who reach the car first take Vale (fail `rivals-reach-vale`).
- **What happens next:**
  - The Enforcer carries Vale 3.5 m to your car and boards (4 s carrying, brief 018). The final fight starts when he has Vale, as now.
  - The yard is no hiding place: Records Office staff leave by the service door from 5:30 (the `records-handover` surge). The 3.5 m carry is seen by anyone a civilian cone covers (brief 054). The player reads those cones and times the carry. Keel measures this in the re-record.

### 1.3 What the front sees
- **The pickup camera** (74, −40; reach 18 m) sees Vale walk 2 m and get in. The drive mouth is 25.7 m from the pole, outside its reach, so it never sees the car turn in.
- **The official door's staff and the guard** see the car pull away and turn into the Office's own yard 16 m on. That is an ordinary thing for Vale's car to do.
- **The rivals' lookout** sees it too. In route B he has already been warned off.
- So there is nothing to report and nothing to blind. The driver's Call (park across the pickup camera for 2:30) has no route use any more; see Motive below.

### 1.4 Getting away: the Drive order and the service-drive stand
- **Drive:**
  - Any operative in the getaway car (Board, 1 s) orders it to a car stand.
  - It drives on the drive grid at car pace. The driver stays in the car, off the street.
  - Line: `Drive · 0:28`. It costs $0, 0 T and no Alert.
  - It is refused while the car is held (brief 018), and while the car's driver or carried person is mid-act.
- **The stands:**
  - The square bay, where the car starts.
  - The service drive: (69.35, −15.0), nose out toward Apron Road, 1 m behind serviceBay.
  - Each stand carries its own exit lane (brief 018, derived from a getaway route), five ambush spots and four police stands. Vantage places them for the service drive under MC-11 (outside 8 m and outside the exit lane).
  - The yard's exit runs east along Apron Road, away from the rival car.
- **The drive is one car wide:**
  - If your car is on it before Vale's arrives, the turned driver stops on Apron Road at the mouth. The Take is the same, but the carry is about 18 m, on a public pavement.
  - The plan's step is "bring the car round once Vale's is parked". The stand's ghost on the ground says so (Glyph).
- **Route B, as I would re-compose it:**
  - Tech boards at 6:25 (when Vale's car parks), drives, and is there at about 6:54.
  - Scout and Fixer walk over after their acts (about 75 m, 54 s).
  - Take done about 7:05; Vale in the car about 7:12; Go once the blocker is down, about 7:20 to 7:35.
  - Today: route-b 8:07.9, route-b-board 7:44.5.

### 1.5 Prices on the order line
| order | today | proposed | why |
|---|---|---|---|
| Pay the driver | $70,000 · 50 s · +2 T | the same | the same favour (move Vale's car to the yard), now with Vale in it; the investigation already questions him (`place-time`) |
| Help the driver | $40,000 · 50 s · +1 T | the same | as above |
| Drive (getaway car) | — | $0 · the drive's seconds · 0 T | a car on a public road; risk comes from who sees the carry, not the car |
| False tip | 44 s · +1 T · away 3:50 | the same, given at the rival car | it now lands on the car and its wheelman |

Copy (Fable to finish; one line each):
- Pay on the driver: "Slip the driver cash to bring Vale round the back. Cash adds Trail."
- Help on the driver: "Cover the driver's cash trouble. He'll bring Vale round the back."
- Status once turned: "Brings Vale to the service door · 6:25".

### 1.6 Timings against the seal and the rivals
| beat | today (route-b-board, c034) | proposed |
|---|---|---|
| driver turned | 1:52.9 | the same |
| rivals arrive / park | 3:10 (pop in) | 2:35 in, 2:46 drop, 3:10 park |
| false tip lands | 3:54.0 (away until 7:44) | the same, on the car |
| Vale's car moves | 5:40 (empty) | 6:13 (with Vale) |
| Vale at the hand-off | 6:10 (walks round) | 6:25.3 (driven round) |
| Take done | 6:48.0 | about 7:05 |
| carry to your car | 67 m on foot, ambush springs 7:27.2 | 3.5 m, fight from about 7:05 |
| car leaves | 7:44.5 | about 7:20 to 7:35 |
| close | 8:30 | 8:30 (the driver gives up) |

- **The close can't go back to 8:00.** The Take would be done 55 s before it, and MC-5 needs 1:00. It stays at 8:30, now as the driver's give-up time.
- **Rivals:** a tip given at 3:10 holds them off until 7:44. A slower cell meets the wheelman coming back, and he comes for your car.
- **The seal (S2, 2:25)** only runs loud. A loud fight at the yard is closer to escape than the square was. Motive should look at it (below).

---

## 2 · The rivals in detail

### 2.1 Why they stand around today (engine 0.29.0, host 0.2.99)
- **`Game.RivalsTimeline`:** at `RivalAt` (3:10, or 16 s after gunfire) it sets `RivalsArrived` and `rivalCarParked`, with no drive and no walk.
- **`Game.RivalsPose`:**
  - It returns one point: `rival-park`, facing +y.
  - With a turned driver it switches to the service lane point at 6:10, a jump of about 20 m.
  - Drawn by gunfire, they walk from `rival-entry` to the Office front. That is the only movement they have.
- **`HostViews.Tick`:** it draws `rivals/0..2` at that one point plus 0.9 m offsets. The rival car has no view at all, though `car:rivals` exists as a target.
- **`GameFight.StartFight` (safe):**
  - It makes fresh hostiles `rival-1..n` at the ambush spots with `Present` false.
  - They appear when the ambush springs (`FightStep`). The crew at the pickup is "absorbed".

### 2.2 The routine (for Pulse)
**The crew:** five in one car.
- **W**, the wheelman: never leaves the car unless the car can't reach where the crew is going.
- **L**, the lookout.
- **P1 to P3**, the posted three.

All times are mission clock and fixed by the site and the rulebook. The car's entry is computed back from the park, so the 3:10 beat holds exactly.

| at | who | does | where (block frame, m) | measured |
|---|---|---|---|---|
| before 2:35 | all | off the map | — | — |
| 2:35.4 | car | turns in from Orbit Walk at the window's east edge | (37.5, 15.75) | — |
| 2:35 → 2:46.5 | car | drives west along Orbit Walk | → drop stop (34, −38) | 55.4 m, 11.1 s |
| 2:46.5 → 2:54.5 | P1 to P3 | get out (8 s stop), walk to their posts round your car | ambush-1, -3, -2 | 14.5 m, 13.7 m, 54.5 m (9 s, 9 s, 34 s at 1.6 m/s) |
| 2:54.5 → 3:10 | car | on round by Rocket Lane and Apron Road | → rival-park | 77.6 m, 15.5 s |
| 3:10 | car | parks across from Vale's car | rival-park, re-sited to Apron Road's south kerb (90.5, −31) | — |
| 3:15 | L | gets out, posts on the far pavement facing the official door | (94, −27) | 6 m |
| while posted | P1 to P3 | stand their posts with idle business (paper, kiosk, bench), facing your car | — | — |
| your car drives off | P1 to P3 | walk after it to the new stand's ambush spots | e.g. ambush-1 → service drive | 59.5 m, 37 s |
| Vale's car leaves the bay (car here, not tipped) | car (W) | follows it, stops at the drive mouth; W walks to Vale's car | (82, −15) → Vale's door | 19.5 m drive 3.9 s; 17.6 m walk 11 s |
| a false tip lands | car (W) | drives off the way it came; back after `false-tip-s` 3:50; re-parks | rival-park → (37.5, 15.75) | 87.7 m, 17.5 s each way |
| a turned driver, L posted, before 5:40 | driver → L | the driver walks over, has a word (10 s), walks back; L walks off down Apron Road and is gone | — | about 17 m each way |
| the final fight starts (safe) | all present | P1 to P3 at their posts lie in wait (spring at 20 m, as now). L and W drive round to your car's stand and get out (to the square: 15.5 s). If the car hasn't come yet, it comes now, straight to your car. | — | — |
| gunfire | all | as today: on the street within `rivals-drawn-s` 16 s, making for the Office front. The car brings them if they aren't here yet. | — | — |

**The fight's count stays Motive's:** `fight-safe-hostiles` 5, −1 turned driver, −1 earlier tip. What changes is which bodies they are:
- The tip removes W.
- The turned driver removes L.
- The rest are real, already-placed people. A hostile still travelling when the ambush springs comes in on its path, as police do (`Hostile.Run`).

**What a Watch on the rivals teaches** (offered once the car is parked, while their moves are unknown; brief 054 "sight and marks"):
- the next move on a clock pip over the car;
- the 20 m spring ring round your car;
- that the car follows Vale's car.

A Listen on the driver gives the same tail fact. Rivals carry no need, secret or temperament (they are a crew, not cast people), so there is nothing else to Listen for.

### 2.3 What Keel's engine needs
**Part 1 (route B):**
- **K1:** Vale always walks to the official bay. A new `ValeAt.InCar` at `valeAtCar + vale-in-s`. Remove `PickupService`, the walk to `ValeService`, and the 5:40 drive with nobody inside.
- **K2:** with the driver unturned, the car pulls away at Vale-in: fail `vale-leaves` (if Sky picks 3a). With him turned, `carToService` starts at Vale-in. Add a note `car.parks` when it is in the bay.
- **K3:** Take target is Vale's car in the bay. In-car, no struggle. A refusal code for a Take at the front with a turned driver.
- **K4:** the wait runs until `over`. At `over`, if no Take has begun, the car drives out (service bay → east edge) and fails `vale-leaves`.
- **K5:** a car on the service drive blocks it. A turned driver who finds it full stops at the drive mouth on Apron Road, and the hand-off happens there.
- **K6:** the Drive order on the getaway car, and car stands from the site (`getaway.stands[]`: at, facing, exit lane, ambush, police). `S.GetawayCar`, `CarHeld`, the exit lane and the fight's spots read the current stand. Refusal codes for a held car, for no driver aboard, and for an unknown stand.

**Part 2 (the rivals):**
- **K7:** the rival car as a moving car: entry → drop stop → park on the drive grid at car pace, its entry tick computed back from `rivalsArrive`. It also drives away on a false tip, returns, follows Vale's car, and drives to your car's stand. `car:rivals` is a target from its entry, and is hashed.
- **K8:** the crew as five people with their own poses (`RivalPoses()` replaces `RivalsPose()`). The false tip targets the car (act reach to the car). `RivalsPresent` is per member.
- **K9:**
  - `StartFight` (safe) builds its hostiles from the crew members present, with no fresh spawns. The posted ones lie in wait at their posts; the others get run paths (car leg, then on foot).
  - The −1s remove W (tip) and L (turned driver).
  - `RivalReachesVale` gains the yard case: W within act reach of Vale's car before `take.done`.
- **K10:** site schema `rivals`: `car { entry, drop, park, facing }`, `posts { lookout, square: [ambush-1, ambush-3, ambush-2] }`, `dropS` 8, `outS` 5. `schedule.rivalsArrive` stays 190 (the park). Unknown keys reject, as `Site.cs` does now.
- **K11:** everything hashed. New rulebook values, each with its tag: `vale-in-s` 3 [S], `rivals-drop-s` 8 [S], `rivals-out-s` 5 [S], `driver-warns-s` 10 [S]. Motive writes them; Keel lands them with the engine.

---

## 3 · The checks: which routes change

**Baseline.** c034 (engine 0.29.0, tree): 38/38 scripts, SHIPS. The marks used here are its recorded per-play marks (`runs/missions/handover/c034/run.json`).
- I tried to play the route set again with `suborned-sim batch` through `bun run guard`. It waited the full 20 minutes because Sky was playing, and I did not force it.
- Nothing in the engine or inputs has changed since c034, so it would replay c034.
- Nothing proposed here is built yet, so the changes below come from the design, read against the scripts, not from a run.

**Re-record all 38.** The rival car moves and is hashed from 2:35, and the crew's poses are hashed, so every script's checkpoints from 2:35 change.

**Outcomes that change or need watching:**

| scripts | what changes | expected |
|---|---|---|
| route-b, route-b-board, route-b-block, route-b-block-board | re-composed from their plans: Vale driven round at 6:13, Take at about 6:27 in the bay, Drive the car round at `car.parks`, fight at the yard with 3 | win, about 25 to 45 s sooner, Trail 3/3/4/4 unchanged |
| careless-reporter-not-stopped, careless-scout-idles-pickup | the route B base re-composed under them | their point holds (reported; loiter seen) |
| fail-rivals-reach-vale | no tip: the rival car follows Vale's car; W reaches him at about 6:30, before the Take is done | fails `rivals-reach-vale`, earlier (it was the Take at 6:10) |
| fight-safe-rivals-reach-vale | the late tip at 5:20 lands at 6:04, so W is away; the fight is at the yard if the car is brought round | re-plan: Enforcer walks Vale into the posted three without cover |
| fail-vale-leaves | unturned driver: car pulls away at 6:13 (3a) | fails `vale-leaves` at 6:13 (was 8:30) |
| careless-guard-left-door-held | the Enter is refused, Vale comes out, gets in and goes (3a) | fails `vale-leaves` at 6:13 (was `rivals-reach-vale` at 8:30); still a careless loss |
| fail-take-seen-camera | the Take at 6:10.0 at the front, before Vale is in at 6:13 | unchanged outcome; check the tick |
| fail-vale-dead | Vale shot at about 6:12, before he is in | unchanged outcome; check the tick |
| route-a-pressure, -board (Vale at 2:41.6) | the rival car is 5 s from its drop stop by your car: all five get out there | fight with 5, all out by about 2:55 (ambush ring at 3:20.8) |
| route-g, route-g-fast and twins (Vale at 2:56 to 3:15) | the crew is between drop and park, or just parked: P1 to P3 posted, L and W drive round | fight with 5; watch MC-11 (no stop holds the car) |
| route-a, route-a-board, route-g-kill and twins | P1 to P3 posted by about 2:57 (ambush-1, -3) and 3:22 (ambush-2); L and W come by car | fight with 5, as today in count |
| loud, loud-fast, loud-fight, loud-left-*, fail-district-seals, fail-enforcer-down, fight-loud-enforcer-down | rivals drawn by gunfire now come by car | the loud fight is police (unchanged); check the rival bodies don't change the seal or the Go |

**New routes that open** (each a plan in `routes/plans/`, composed by `missions:check compose`):
- **route-b-square:** route B without Drive, the 67 m carry. It keeps the long way as a measured choice.
- **route-a-yard, route-g-fast-yard:** A or G with your car brought round to the service drive before the Enter (not by the Tech while he holds the Loop). The carry drops from 67 m to 3.5 m. This makes MC-6 (quiet ≤ loud) wider.
- **careless-car-blocks-drive:** your car on the drive before Vale's. The turned driver stops in the road, and the carry runs on the pavement.
- **careless-no-tip-tail** is the new fail-rivals-reach-vale; the name can stay.

**MC conditions after the change** (expected; the re-record decides):
- MC-3: every fail still reached. `vale-leaves` comes at 6:13 or 8:30, `rivals-reach-vale` by the tail.
- MC-4: unchanged.
- MC-5: route B 1:25 early, against 8:30.
- MC-11: needs Vantage's yard spots outside 8 m and the yard exit lane. rival-park at (90.5, −31) is 26.5 m from the yard stand and off its east lane.

---

## 4 · GDD §07: proposed text (present tense)

Replace these lines in §07 (agent and human GDD). The other lines stay.

> **cast:** Vale · Inez (café waitress, in debt; secretly the door guard's lover) · the door guard (Inez's lover; his pre-shift café stop is to see her) · official driver (needs cash) · the Office's other guards (official entrance; the relief at 5:30) · rival crew (feared by the public; five in one car: a wheelman, a lookout and three who post round the cell's car) · cameras · patrol. The Handover runs at security level S2.

> **schedule:**
> - 0:00 Vale inside; routines run; the cell stands loosely by the getaway car at the square's boarding pavilion, in view, and the camera opens on them.
> - The door guard checks in at 1:00, 2:30, 4:00 and 5:30 (shift end; other staff posts keep 90 s marks from their own shift start).
> - 2:35 the rival car turns in from Orbit Walk. 2:46 it drops three by the cell's car, who post round it.
> - 3:10 the rival car parks across from Vale's car; its lookout watches the official bay.
> - 5:30 handover chime.
> - 5:40 last chance to turn the driver.
> - 5:50 Vale comes out of the official door.
> - 6:10 Vale at his car, in by 6:13. His own driver drives him away (= over). A turned driver drives him round to the service bay (6:25) and waits there.
> - 8:30 the turned driver gives up and drives Vale on (= over).

> **route B, the pickup (from 6:10):**
> - Scout listens to the driver: he needs cash (42 s).
> - Fixer turns the driver before 5:40 (Pay $70,000, or Help $40,000). He warns the rivals' lookout off, and once Vale is in he drives him round to the service bay (13 s) and waits there until 8:30.
> - Scout gives the rival car a false tip (44 s), or it follows Vale's car and its wheelman reaches him first.
> - Once Vale's car is parked, the cell brings the getaway car round behind it (Drive, 28 s).
> - Enforcer takes Vale from the back seat (38 s) and carries him 3.5 m to the car.

> **the Take [S] (Sky):** at Vale's own car with his driver turned, in the service bay, the Take happens inside the car, off-screen, with nothing to witness. Any other Take has a 5 s struggle onlookers can see. Escorted on foot, Vale looks wrong only to his kin, or to anyone within 3 m who sees the grip.

> **getaway [S]:** the getaway car starts in the square's bay. An operative in it can Drive it to one of the mission's car stands: the square's bay, or the service drive behind Vale's car (nose out, once Vale's car is in; on the drive first, it blocks his way and he stops in the road). Each stand has its own exit lane, ambush spots and police stands.

> **rivals [S]:**
> - A crew of five in one car, after Vale on their own timetable. They know the cell's car.
> - They are seen coming: the car turns in from Orbit Walk, drops three who post round the cell's car, and parks across from Vale's car at 3:10. A lookout watches the official bay; the wheelman stays in the car.
> - A false tip sends the car and wheelman away the way they came for 3:50.
> - When Vale's car leaves the official bay, the rival car follows it.
> - Gunfire draws them to the Office front within 16 s.
> - If the cell has Vale before they arrive, their car comes straight to the cell's car.
> - Watch on the rivals shows their next move and where each will go.

> **final fight, safe (no shots, A ≤ 1):**
> - The rival crew ambushes the extraction at the cell's car. The three posted round it spring as Vale comes within 20 m; the lookout and the wheelman come by car.
> - The guard (never saw you, never taken down) fights them.
> - A helped Inez warns 10 s early.
> - A turned driver has warned the lookout off (−1 rival).
> - An earlier false tip keeps the wheelman away (−1 rival).

> **fail:** Vale killed · Vale leaves in his car · rivals reach Vale · district seals · Enforcer down · entry seen by guard, live camera or patrol. Withdraw ends the attempt. A takedown seen by a live camera is not an instant fail: it is reported (Alert 3 on landing).

> **reference results:** route B is re-measured after the change (c035).

Delete:
- "5:50 Vale exits north (service lane if driver turned) · 6:10 Vale at car · 8:30 rivals take Vale or car leaves = over";
- route B's "pickup moves to service lane, or driver blocks pickup camera 2:30";
- the safe fight's "rivals ambush extraction … turned driver blocks rival car (−1 rival); earlier false tip (−1 rival)".

The block call goes with 3a/1a unless Motive gives it a new job (below). §05 rivals ("own schedule") gets one pointer to §07.

---

## 5 · Handoffs

### Keel (simulation engine): build K1 to K11 (§2.3)
- **Inputs:** this brief; Pulse's routine (§2.2, refined); Motive's values; Vantage's points; Splice's site export.
- **Done-check:**
  - new scenarios: driven round and taken; nobody there at 8:30; the car on the drive first; the tail without a tip; the tip, then the return at 7:44; the rivals before arrival (A-pressure); gunfire before 2:35;
  - every scenario replays bit for bit;
  - `dotnet test` green; `bun test contracts/rules` and `bun run verify` no worse.
- **Next owner:** Waymark (re-compose and re-record).

### Pulse (schedule and AI): the rival routine (§2.2)
- **Inputs:** this brief and the picture.
- **What to write:**
  - the crew's poses and idle business at their posts (no standing still for minutes);
  - the follow rules (your car moves; Vale's car moves);
  - the lookout's exit when warned;
  - the late-arrival rule;
  - who takes which ambush spot at each stand.
- **Done-check:** a routine spec Keel can build without asking, with every number tagged [B] or [S], and a deterministic order for ties (by id).
- **Next owner:** Keel.

### Motive (systems): prices and balance
- **Inputs:** this brief; brief 058 (do not duplicate it).
- **To rule on:**
  1. Turn prices unchanged (§1.5): confirm.
  2. Drive: $0, 0 T, no Alert. Confirm, or price a strange car in the Office's service drive.
  3. The posted rivals can be shot or taken down before the fight (§02: anyone can be targeted). Rivals don't report. Rule the Trail, the Standing, and whether the fight's floor of 3 holds or a prepared cell can thin it.
  4. The fight with crew arriving on the move (A-pressure, G): balance against brief 027/039.
  5. Loud at the yard stand: the police come from the north street, about 130 m off. Rule whether a second entry is needed (Apron Road west) so loud stays dearer than quiet (MC-2, MC-6).
  6. The driver's Call (block the pickup camera 2:30) has no route use now that the front is ordinary. Keep it, retire it, or give it a new job. Decide alongside 058's Loop and Disable, which apply to the pickup camera the same way and need nothing new from this brief.
  7. Values in §2.3 K11.
- **Done-check:** rulebook entries written with tags and reasons, landed with Keel.
- **Next owner:** Keel.

### Vantage (level designer): points
- **Inputs:** §1.4 and §2.2.
- **To place:**
  - the getaway stand `service-drive` (69.35, −15.0, nose +x) with five ambush spots and four police stands under MC-11;
  - `rival-park` re-sited to Apron Road's south kerb (90.5, −31): the eastbound side, so the car parks on its own side of the road under the plan's right-hand traffic;
  - `rival-drop` (34, −38) on Orbit Walk's north lane;
  - `rival-lookout` (94, −27);
  - `rival-car-entry` (37.5, 15.75).
- **Done-check:** `bun tools/plan/cli.ts --overlay tools/missions/handover/overlay.ts` exits 0; V-1 at the heading each point is played at.
- **Next owner:** Splice.

### Splice (tools): site export
- **What to export:**
  - `getaway.stands[]`, with each stand's exit lane derived from a new mission route `getaway-service` (Apron Road east from the drive);
  - the rival car's route as a mission route `rivals` (off_seagates → Orbit Walk → Rocket Lane → Apron Road → rival-park), cut to the window;
  - SX rules for both.
- **Done-check:** `bun run sim:site` and tests green.
- **Next owner:** Keel.

### Glyph (interface artist): what the player sees
- **The rival car:** drawn (Vela's sedan in rival trim). As it turns in, a feed line "Rival car coming", and a thin violet approach line on the road from the car to its next stop, shortening as it drives.
- **After a Watch on the rivals:** a clock pip over their car (the next move) and the 20 m spring ring round your car on hover or Alt.
- **Vale's car with the driver turned:** on hover, a dashed line from the official bay to the service bay with "6:13 → 6:25", the wait pip "until 8:30", and a hand-off ring at the rear door.
- **Drive:**
  - with the getaway car or an op in it selected, car-outline ghosts on the ground at the stands;
  - on hover, the route preview and "Drive · 0:28";
  - the yard ghost marked "after Vale's car" until his car is parked.
- **Done-check:** lab boards at play zoom (yaw 210 for the yard, 30 for the square), budget checks, and no sentences beyond the one-line price.
- **Next owner:** Latch.

### Latch (Unity engineer): the port
- **Inputs:** engine build, Glyph's spec.
- **To build:**
  - views for `car/rivals` (moving, heading from the engine) and five rival figures from `RivalPoses()`, replacing the three-at-one-point offsets;
  - the official car's drive, park and drive-on;
  - the getaway car's Drive input (right-click a stand with the car or an op in it selected);
  - Glyph's markers;
  - remove the service-lane walk visuals;
  - update GameBoot's pickup spot.
- **Done-check:** Unity compiles with a clean console; one batch of shots: the rival car turning in, the drop, the park, Vale getting in, the drive round, the Take, the carry, the yard fight.
- **Next owner:** Suture.

### Also
- **Fable:** the lines in §1.5 and §2, plus the new refusal codes. The feed rows `vale.exits`, `vale.at-car` and `car.leaves` lose their "service" branch, and `car.parks` and `rivals.drop` are new.
- **Aster:** carries §4 into GDD v0.25 with Sky's answers, then `bun run crew`.
- **Waymark (me), after Keel:**
  - re-compose the route B family and the new plans, and re-record all 38 plus the new ones as c035;
  - retime mission.json's timeline and roles (the driver's `waitsFor`, the councillor's `leavesTo`), its points and a `rivals` block.

## Sky's calls (2026-10-25)
Sky, 2026-10-25, on the outstanding-work summary that listed each pick: "Your recommendation seem good".
1. **(a)** bring the cell's car round behind Vale's with the new Drive order (3.5 m carry).
2. **(a)** the rivals post round the cell's car in plain sight from 2:46.
3. **(a)** with the driver unturned, his car drives off at 6:13 and the mission ends.
Next: Aster writes the GDD lines (a new version, then `bun run crew`); then Keel, Waymark, Latch as above.
