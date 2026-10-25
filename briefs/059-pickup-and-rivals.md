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

## Pulse (2026-10-25)
The rival routine (§5 "Pulse") is written: **`briefs/059/pulse/rival-routine.md`**, with `rival-routine.values.json` (the eight new [S] values, the beat lists, every point and route named), `check.py` (`python3 -I briefs/059/pulse/check.py`, 104 checks, exit 0) and `measures.json`. Keel builds from it (K7 to K11 reconciled in its §9).
- **The crew** is `rival-1` to `rival-5` plus `car:rivals`, roles by id: the posted three, the lookout (`rival-4`), the wheelman (`rival-5`). The timetable is computed back from the 3:10 park: enters 2:35.3, stops 2:46.4, leaves 2:54.4, parks 3:10.0; the three step out 2 s apart and post at 2:57.0, 2:59.5 and 3:26.5; the lookout posts at 3:18.8.
- **Idle business:** loops of 34 to 42 s, no pose longer than 10 s, in place (the posted keep their cover), yaw windows a Watch can show. No bench or kiosk exists in the civic block, so the business uses only what a person carries.
- **Follows:** the three walk after the cell's car 2 s after a Drive (first 3 spots of the new stand, shortest walk, ties the lower id); the car follows Vale's car 2 s after 6:13 and again whenever it parks with Vale's car away (a tip that ends late); W reaches Vale's door at 6:38.9 untipped.
- **Warn:** the lookout walks to the driver, a 10 s word, leaves the map; only if the word is over by 5:40 (turn by 5:16.3). Late arrival: a table by the car's state when the cell has Vale; the car stops at the stand's rival stop and the missing riders step out in id order.
- **New points for Vantage:** `rival-car-entry`, `rival-drop`, `rival-lookout`, `rival-tail` (also the service-drive stand's rival stop), `rival-vale-door`, `rival-front-stop`, `rival-lookout-exit`; `rival-park` re-sited to (90.5, -31.0); the `service-drive` stand and its spots. Routes for Splice: `rivals`, `getaway-service`. All are on the right grids and outside the pickup camera's cone (checked); `rival-entry` is retired when nothing reads it.
- **Open:** the fight's count becomes bodies (Motive); the warn has a cliff at 5:16.3 (Motive, Fable, Glyph); the tail reaches the door at 6:38.9, not the brief's about 6:30 (Waymark); no car-car collision (Keel). The brief's 6:25.3 for Vale's car parking counts from 6:12; from 6:13.0 it is 6:26.3.
Next owner: Keel.

## Motive (2026-10-25)
The rulebook-ready entries (key, value, tag, reason, source) are in `briefs/059/motive/rulings.json`. The probes are in `briefs/059/motive/probe/`, run on engine 0.30.0 with HEAD's inputs. Those inputs replay c036 38/38. Keel lands the entries in his 059 round.
1. **Turn prices: unchanged.** Pay $70,000 · 50 s · +2 T. Help $40,000 · 50 s · +1 T. 5:40 and the false tip (44 s, +1 T, 3:50) are unchanged too; the tip is now given at the rival car.
2. **Drive: $0 · 0 T · no Alert, confirmed.** No camera, guard or sweep sees the service-drive stand: it is 25.4 m from the pickup pole, whose reach is 18 m. Three additions:
   - Drive is refused once the final fight has started, so nobody drives away from a sprung ambush.
   - The posted three run after a Driven car (2.8 m/s: 21 s against the Drive's 28 s; Pulse F1 has them walk, 37 s), so the ambush can't be shed. **Sky: S3.**
   - The turned driver learns the cell's car when it parks behind him. He gives it up only if he talks (the Talker test).
3. **Rivals targeted before the fight.**
   - The five are people. Shot: +2 T per hit, +2 more if he goes down. Taken down: +4 or +6, charged only when someone else's report of it is confirmed. Rivals never report, give statements or go missing.
   - Standing works as for anyone: seen acts and found killings are −1 each, and a pre-fight fight is judged once by opponent.
   - A knocked-out rival wakes after 2:00 and rejoins his crew.
   - Shooting the posted three makes the run loud, so it never thins the safe fight.
   - The floor of 3 holds against the tip and the driver. Takedowns can thin the fight further, priced by the systems: the three stand 2.3 m apart facing your car, the square is busy, and a body left on it is found. **Sky: S1.**
4. **The fight with crew on the move.**
   - The count is whoever is on the map at the fight's start, plus a wheelman coming back. At most 5.
   - Arrivals never fire from the car. They get out at the stand (5 s) and come in on their path.
   - The blocker is picked only from the crew on foot. The break counts the travellers.
   - At the square, A-pressure and G fight c036's five at the same spots: the car is free 6.5 to 8 s after the spring. Brief 027's numbers are unchanged, and nothing depends on brief 039.
   - Pulse's fixed drop at the square stands. For Vantage: at the service drive, the blocker's spot is within 5 s of its lane post, so the 6.5 s carry and Board can't beat it; each stand's rival stop is beyond 8 m and off the exit lane.
5. **Loud at the service drive: yes, a second entry is needed, and only a near one works.** The loud police come from the entry nearest the car's stand:
   - the square keeps the north street;
   - the service drive gets the official entrance (the Office's own guards, 19.5 m away).
   
   Probe, the loud front with the cell waiting in the car at the service drive:
   - north-street police: unfought, 3:55.3, 8 T;
   - an Apron Road entry at the window's edge: also unfought, 3:55.3;
   - the official entrance: the car is held, and the cell must fight.
   
   At the square, loud is 5:10.0 with 13 T. **Sky: S2.**
6. **The driver's Call: retired.** It has no job left, and the pickup camera already has Loop, Disable and Shoot (brief 058). Routes route-b-block and route-b-block-board go. A Call on the driver is not offered. call-trail stays for Inez. The lookout warning stays automatic (Pulse F4). The cut-off at 5:16.3 is intended, and the driver's Turn line shows it until then.
7. **K11:** `vale-in-s` 3, `rivals-drop-s` 8, `rivals-out-s` 5 and `driver-warns-s` 10, all [S], plus `rivals-follow-cm-s` 280 and `rivals-crew` 5 (it renames `fight-safe-hostiles`). `fight-modifier` is retired: the −1s are now people who have left. Pulse's five routine values (step 2 s, follow beat 2 s, posted 3, warn lag 3 s, lookout pace 2 m) are confirmed.

**For Sky (each with my pick):**
- **S1:** a fast or prepared cell may leave without a final fight (A-pressure with the car at the service drive goes by about 2:51; takedowns can thin the crew to none). Pick: allow it. It forgoes the fight's +1 Standing, and loud always fights.
- **S2:** the loud police come from the nearest entry (the official entrance for the service drive). Pick: yes.
- **S3:** the posted three run after a Driven car instead of walking. Pick: run.

## Glyph (2026-10-25)
Spec for Latch (§5 "Glyph"), in the Suborned HUD kit, for Sky's calls above (1a Drive, 2a the rivals post round the cell's car in plain sight from 2:46, 3a an unturned driver leaves at 6:13). Boards at play zoom, 2 px per game px, placement indicative (Latch projects from the site export): `briefs/059/board-01-rival-car-arrives.png` · `board-02-watch-reads-the-rivals.png` · `board-03-vale-car-driver-turned.png` · `board-04-drive.png`. The square at yaw 30 (`start-overhead-day`), the Office front and the bay at yaw 210 (`done-call-window`), the service door at yaw 210 (`briefs/023/latch/service-door-210.png`). Redraw: `python3 -I briefs/059/carsprite.py && python3 -I briefs/059/boards.py` (library `briefs/058/glib.py`; numbers `briefs/059/budget.json`). Stand-ins: the violet car in boards 01 and 02 is cut from the official car (Vela's sedan in rival trim replaces it); the cars in the yard and at the Office front are footprints (the models draw in game). Words are Fable's keys where they exist; the marks are icons and times, no sentence.

### 0 · Words that already exist (Fable, text.json)
`cards["rivals.coming"]` (beat "Rival car coming.") · `cards["rivals.drop"]` (icon, "Rivals post round our car") · `cards["rivals.arrive"]` (icon, the park) · `less.done.gain.parked` ("Parked", a Drive that has arrived) · the refusal codes for a held car, no driver aboard, an unknown stand (his, §Also). **Name clash to settle (Aster/Fable):** `actions.drive` is today's third-person "Take control" (HostDrive.cs); the new getaway order's verb is also "Drive" (Sky's 1a). The pill prints the order's own label, so give the new one its own key (e.g. `actions.drive-car`, label "Drive") and keep "Take control" as the other's label.

### 1 · The rival car and its approach (boards 01, 02)
- **The car** is a view (`car/rivals`, heading from the engine, Latch's §5). It is drawn the whole way: nothing is invented for the HUD. Violet is the rivals' signal colour (`#8a4fd0`, style contract `signal_colours.rivals`), used on the ground marks and the ring only; the model's trim is Vela's.
- **Approach line** (the car's next leg, from its nose to its next stop, on the site's `rivals` route, never typed): violet `#8a4fd0`, 2.2 px, over an ink-deep underlay (4.8 px, 60 %), **diamond ticks** every 2.4 m (half-size 3.4 px, violet with an ink-deep rim), trimmed from the nose each frame so it shortens as the car drives. The stop is an `arrive` icon (cream, 13 px) on a `disc_dark_24` (22 px). Ground marks depth-test as the rest (brief 046): behind a building it hides. The pointer chip (below) takes over when the car leaves the screen. It is drawn from the car's first frame in sight (2:35), with no Watch: it is a car driving past; the Watch adds what is next (§2).
- **The drop (2:46):** three people step out and walk to their posts: a dotted violet walk line (1.8 px, 2.5 on / 5 off) from the stop to each post, gone when they stand; each post is the **rival ring as built**: solid ring 2.4 px at r 0.55 m and a dashed ring (3.5 on / 3.5 off, 1.8 px, 82 %) at r 0.95 m, violet over the ink-deep underlay. Rings only; no icon over them (the budget).
- **The car's onward leg** (round by Rocket Lane, Apron Road): the same approach line to its next stop; the **pointer chip** at the screen edge when it is off screen: `plate_dark` 22 px high, 52 px wide, the kit's `badges/signal_rival` in the cameo ring (16 px) and the `pointer` icon (14 px) turned toward it.
- **The park (3:10):** a violet footprint stands in for the model in board 02 (4.6 × 1.9 m, 0.5 m corner radius, 60/255 fill); the **lookout** is one ring on the far kerb facing the official door; the wheelman has no mark (he stays in the car). The feed: the card as built; `rivals.coming` is the only beat.
- **After a Watch on the rivals** (brief 054's "sight and marks": the three facts it teaches, three marks, nothing written):
  1. **the next move:** the as-built window marker over the car (anchor +56 px), `nextevent` icon (the double arrow), the seconds to its next move, the underline draining over the leg: "24" at 0:24 (m:ss only if the leg is 60 s or longer);
  2. **how close is too close:** a thin dashed violet ellipse (`7 on / 6 off`, 1.8 px, 78 %), the ground circle of the spring radius (the rulebook's 20 m, read, never typed; on screen 772 px wide by 475 tall at play zoom, so the arc crosses the screen: the board crops it) round the cell's **current** stand. Shown on hover of the rival car or any rival post, or with Alt; never at rest. The three posts are the rings already on the ground;
  3. **it follows Vale's car:** a dotted violet line (2 px, 2.5/5) with filled chevrons (5.5 px, every 3 m) from the rival car to Vale's car, drawn once learned and only while the follow is possible.
  A Listen on the driver ("that car's tailed us all week") adds nothing new to draw.

### 2 · Vale's car with the driver turned (board 03)
- **The route** (hover the car, or select the turned driver): the official bay to the service bay along Apron Road and the drive, on the drive grid: **cream over ink-deep, dashed** (2.4 px, 7 on / 6 off), the walk-and-run language, filled chevrons every 3 m (6 px). It is cream, not violet: it is the cell's own read of its contact's act, not a signal.
- **The times:** a `plate_dark` 28 px high on the route: `car` icon 14 px, `6:13`, `pointer` icon 12 px, `6:25` (Jost Bold 13.5, cream). The engine writes both numbers (Pulse measured 6:26.3 from 6:13.0: the plate shows the engine's, the boards the brief's). Nothing else on the line.
- **Parked (6:25 to 8:30):** the car stands where the model is; the **wait pip** is the as-built window marker over it: `car` icon + the countdown to 8:30 (m:ss, e.g. 1:52), the underline draining, amber and blinking in the last 10 s. Before it parks the same marker shows the fixed 8:30 with no underline. The **hand-off ring** is where the Enforcer stands to take: a cream ring on the ground (0.8 m, 2.6 px over the underlay) at the rear door (0.3 m behind the car's centre, 1.5 m to the door side), with a `disc_dark_24` 22 px carrying `take` at its side. It is the Take's act reach, the same ring the other acts use.
- **Off screen:** an edge chip (`plate_dark` 22 px, `car` icon 14 px + `pointer` turned toward it) at the screen edge when the route leaves it (board 03, left).
- **The front** (6:10): nothing new on the official door; the car's leaving is the route above. A Take ordered there is refused with the new code ("He's bringing Vale round the back": Fable's).
- **The warn's cliff** (Pulse's open): the lookout's ring is the read; if the driver is turned after 5:16.3 the lookout is still on his ring when Vale gets in, which is visible. No new mark. If Sky wants the cliff on screen, it is one window marker on the driver (`nextevent` + the seconds to 5:16.3), in the grammar above.

### 3 · Drive (board 04)
- **Select the getaway car, or an operative in it:** every stand shows a **footprint ghost** on the ground: a dashed cream rounded rectangle 4.6 × 1.9 m (corner radius 0.5 m), 2.2 px over the ink-deep underlay, a filled chevron at the nose, a `disc_dark_24` 24 px with `car` at its centre. The stand the car is on is the same footprint under the car (no disc).
- **Off screen:** an edge chip toward the stand (`car` + `pointer`).
- **Shut stand** (the yard, until Vale's car is parked): the footprint at 50 % with a short dash (3 on / 5 off); a `plate_dark` 26 px high, 60 wide, above it: the `lock` icon 14 px and Vale's cameo (`badges/npc_councillor`, 18 px, in the chrome ring), the way a shut Go carries its lock and holder. No route. A hover or a try gives the refusal in the pill as Go's does. No sentence on the ground.
- **Hover a live stand:** the footprint turns solid turquoise-light; the **route preview** from the car along the drive grid: cream dashed 2.4 px (7/6) with chevrons every 3 m, ending in the footprint; the verb pill (as built: `pill_status` 38 px, `disc_dark_24` with `car`, the dim head "Hold", the body `Drive · 28 s`, the driver's face, tail 22 px above the footprint). The brief writes "Drive · 0:28"; the game prints every time as seconds, so the pill prints **28 s**. A hold commits (0.5 s as every hold; the sweep, "Let go", the ring: as built), right click opens the panel with the same line.
- **Footprint orientation:** from the stand's facing (nose out at the service stand; the board draws it behind Vale's car, 1 m behind: `CAR_L + 1.0` m back along the axis). Latch reads it from `getaway.stands[]`.
- **The arrival:** the chip `gain.parked` ("Parked") on the car, one row, `car` icon 16 px.

### 4 · HUD budget (rest ≤ 15 %, 22 % with a card or the ring open, 30 % with Alt)
Baselines: the square at rest 4.70 %, the Office front 6.66 % (the captures' own HUD). Added: the approach line 4.71 %; the drop 4.74 %; the Watch's pip and line 6.77 %, and in the square 4.80 %; the route and plate at the bay 6.82 %, in the yard 6.80 %; parked with its pip and ring 6.79 %; the stand ghosts 4.74 %; the shut stand 6.73 %; the hover with its pill 7.11 %. With the world marks counted (lines, rings, ellipses): 4.82 to 7.45 %. Largest 7.45 %: far inside every limit. At most two icons over a person: rings only on the rivals; the car carries one disc or one marker.

### 5 · Done-check and open
- Done: the four boards; budget; no new kit asset (all icons exist: `car`, `arrive`, `pointer`, `lock`, `take`, `nextevent`, `p_alert`; badges `signal_rival`, `npc_councillor`). Not done: Unity (Latch); a 2160 shot of each for the 2x review once built; Vela's rival sedan (placeholder cut from the official car, `briefs/059/rival_car_standin.png` and its script).
- Open: the "Drive" name clash (§0); the plate's two numbers come from the engine; the 20 m ring's name in the rulebook is Keel's; the 2x crop review waits for real frames.
- **Next owner: Latch** (markers and the Drive input), Fable (the refusal words and the clash), Vela (the sedan).

## Aster: inputs for Keel's 059 round (2026-10-25)
Order: Splice's 061 round lands → Splice's 059 site export → Keel K1–K11 → Waymark → Latch (batch 0.2.118 item I, or the next batch).
- **Spec:** Pulse's `briefs/059/pulse/` (rival-routine.md, values); Motive's `briefs/059/motive/rulings.json` (32 entries); Vantage's 17 points in mission.json; Glyph's and Fable's sections above; Fable's `pending` codes in text.json, to confirm or rename.
- **Sky's open questions (decision log 23–25):** S1 (leave without a final fight), S2 (loud police from the entry nearest the stand) and S3 (posted rivals run after a moved car). Build each as a rule value set to Motive's pick, so Sky's answer is a value change, not a rebuild. The §07 lines change only once Sky answers.
- **The getaway Drive** gets its own engine id, separate from Take control's `drive`. Record whether a dead camera was cut or shot, for Glyph's two discs.
- **Rulings Keel makes with Motive and Pulse, from Vantage's second report:**
  - Riders walking from `rival-tail` cross the first metres of the exit lane, and count as blocking while they are in it. Re-measure once Splice exports `getaway-service`.
  - `rival-front-stop` is 0.25 m from the kerb, so the car hangs 0.66 m over the pavement. (63.75, −51.0) clears it but breaks Pulse's nearest-cell rule.
  - The front stop is northbound, so a car arriving from `rival-car-entry` stops in the oncoming lane.
  - `rival-vale-door` is 5.2 m from the yard stand, inside the 8 m fight-hold, so a posted wheelman holds the cell's car before any fight.
- **Pulse's notes:**
  - Cars don't collide: the rival car shares Rocket Lane and Apron Road with the getaway exit.
  - The turned driver waits until 8:30, although his day in handover.people.json ends at 7:30 (an engine rule for a turned driver).
  - The driver's warning is promised only while 5:16.3 can still be reached.
- **Then Waymark:**
  - re-compose the route B family;
  - retire `route-b-block` and `route-b-block-board` (the driver's Call is retired);
  - add MC-13 (every car stand's wait-in-the-car loud run is held);
  - land mission.json's timeline and route-A strings for GDD v0.25 §07, with site.ts's word patterns (the fold's report lists them);
  - re-record.
- **Glyph:** `rival-front-stop` and Rocket Lane by the Office are 0 % seen at yaw 30, so the approach line there reads at 210.
- **Also for Keel:**
  - Fight-lab L3 replays route-g-fast-board to fixed tick 1236, where the guard is already down, so L3 and L3:charge read "not loud". Tie L3 to the Enforcer's move (Waymark, 0.31 retimes).
  - tools/people/test/handover-fix.cast.json:619 still says "marked block" (Fable).
- **The site, Aster's call (2026-10-25):** the 059 keys (`rivals.car`, `getaway.stands`) stay out of the live sim/data/handover.site.json until Keel's 059 engine reads them. Engine 0.31.0 rejects an unknown key, and batch 0.2.118 ships on the pre-059 site plus 061. Splice keeps the 059 output in a side file, or behind a site.ts switch, and turns it on in the same change as Keel's engine.
- **`rival-park` is held at (84.0, -31.0) for batch 0.2.118 (Aster, 2026-10-25; Splice).** The live `mission.json` point and the live `sim/data/handover.site.json` place carry the pre-059 position; Vantage's target is **(90.5, -31.0)** (Apron Road's south kerb, the eastbound lane, `on: apron-road`, floor -0.18; the full entry is kept below).
  - **The point's 059 entry, kept for Keel to paste back** (mission.json, as Vantage wrote it): `"at": [90.5, -31.0]`, `"src": "GDD §07: 3:10 rival car parks near pickup; re-sited 2026-10-25 from (84.0, -31.0) (brief 059 §5, Vantage; Sky's answer 2a: the rival crew drives in, drops a team and parks)"`, `"reason": "on Apron Road's south kerb, in the eastbound lane (the carriageway is x 80..92, centreline 86), 12.3 m south of Vale's car in the official bay (78.2, -31.0), across the road from it. Re-sited from (84.0, -31.0) so the car parks on its own side of the road under the plan's right-hand traffic: it comes up Rocket Lane and along Apron Road eastbound, and stops at the kerb it drives on, not across the westbound lane. The engine moves with it (Game.RivalsPose, the rival car's park; Keel's 059 round)."`, `"on": "apron-road"`, `"floor": -0.18`.
  - **Where the target lives now:** `RIVAL_PARK_059 = [9050, -3100]` in `tools/sim/stands.ts` (with `placesFor059`). The side file `sim/data/handover.stands.json` is built with the park there (`rivals.car.route.chain.park` 14550, facing [0, 100]), SX-3 checks the target, and `tools/sim/stands.test.ts` pins both positions. Keel's 059 round moves the mission point to (90.5, -31.0), deletes the constant and `placesFor059` (and the test lines naming the hold), and runs `bun run sim:site` and `bun run cover`.
  - **The 13 scenarios whose outcome moves at (90.5, -31.0)** (measured by running the whole suite with the site at the target: S05; S20, S21, N11b, N19b, N33, N78; N31, N81, N82, N87, N181, FW-SEEN). Keel re-sites them in the same round. DP1 also fails there, but it fails on the site's inputs hash at either position (see the site note below).
  - **Also read the park:** `contracts/missions/handover/routes/routes.json` `fail-vale-leaves` (it expects Alert 1 at (84, −31), where the camera and the patrol report the Scout. With the park at the 059 target the Scout can wait out of the pickup camera's view and it reads Alert 0: Waymark re-checks it in the 059 round, together with the Scout's wait spots in careless-scout-idles-pickup and fight-safe-rivals-reach-vale), and the points `rival-drop`, `rival-tail` and the stand prose, which cite the target by number. Waymark's c037 sees the route at (84, -31) until Keel's round.
  - **Cover:** `bun run cover` regenerated `contracts/world/civic-block.cover.json`: the four rival-car points `cv-011..cv-014` (of `car/rivals`) moved back 6.5 m with the park, to (82.25, -31.25), (83.75, -34.25), (83.75, -27.75), (85.75, -31.25) (they stood at 88.75/90.25/90.25/92.25 at the target). Pulse: the rival car's four cover points are the only change. Run `bun run cover` again after the move.
- **mission.json strings for GDD v0.25 §07** (from the GDD fold; mission.test.ts compares the timeline with §07's schedule line word for word, so the GDD line is the source):
  - Timeline:
    - "3:10 rival car parks near pickup" becomes three entries: "2:35 the rival car turns in from Orbit Walk", "2:46 it drops three by the cell's car, who post round it", and "3:10 the rival car parks across from Vale's car; its lookout watches the official bay".
    - "5:40 last chance to turn driver" → "5:40 last chance to turn the driver".
    - "5:50 Vale exits north (service lane if driver turned)" → "5:50 Vale comes out of the official door".
    - "6:10 Vale at car" → "6:10 Vale at his car, in by 6:13: his own driver drives him away (over); a turned driver drives him round to the service bay (6:25) and waits there".
    - "8:30 rivals take Vale or car leaves = over" → "8:30 the turned driver gives up and drives Vale on (over)".
  - `windows[0].gdd` → "the guard walks to her side (about 57 s), stays 60 s, walks back = about a 2:54 window".
  - `prep[A].gdd` → "listens to Inez (1:02)".
  - tools/sim/site.ts reads the timeline by its words: `/rival car/` must match exactly one entry, and `/Vale exits/`, `/Vale at car/` and `/= over$/` must change with the entries (the new ones end "(over)").

## Sky's calls on S1–S3 (2026-10-25)
1. **S1, leave without a final fight: no.** Sky: "no, it's to demonstrate the features of the games". This is not Motive's pick: the switch flips to "no skip". Motive defines the rule that holds the fight. Two leaks to close: (a) A-pressure with the car at the service drive leaving by about 2:51, before the rivals post; (b) takedowns thinning the posted crew to none. He prices the rule, then Keel builds it and Waymark re-composes. The rule stays a value.
2. **S2, loud police from the entry nearest the stand: yes** ("yea"). The built value stands.
3. **S3, the posted rivals run after a Driven car: run.** The built value stands.
GDD §07 (final fight, loud, rivals) and §14 Chime are updated.

## Motive: S1 = no, in rules (2026-10-25)
The rulebook-ready entries are in **`briefs/059/motive/s1-rulings.json`**: key, value, tag, reason and source, plus what they supersede in rulings.json, the measured effect on each route, and the checks. The probe is in `briefs/059/motive/s1/`, with its numbers in `results.txt`. It runs engine 0.31.0 plus two scratch switches (`engine-probe.patch`). With both off it replays c037 byte for byte. Keel lands the entries.

**The rule, as two values:**
- **`fight-always` 1 [S].** Once the final fight has started, Go with Vale aboard is refused until the fight's blocker is out (down or gone) or the fight is over.
  - The refusal stays `car-held`. Its holders are the blocker wherever he is, or, before he is named, whoever is coming: the rival car, with the seconds to its stop, or the officers running in.
  - Board and go and fight it out are unchanged.
  - 0 gives back the old pick.
- **`rivals-floor` 3 [S].** At the safe fight's start, if fewer than 3 able crew are coming, away bodies come back:
  - the tipped car cuts its tip short and brings the wheelman (on foot 19.8 s later at the yard);
  - the warned lookout walks back (21.7 s).
  - Nothing spawns. Killed or bound members stay gone.
- **Two supporting rules:**
  - **The late spring.** The blocker is picked at the first step-out when nobody is on foot at the spring.
  - **Seen takedowns.** A crewmate who sees a takedown fires, so the run goes loud.
- **One edge.** With all five killed or bound, the fight is over: "crew destroyed". This is not reachable quietly, because the wheelman never leaves his car before the fight except in route B's untipped tail.

**The leaks:**
- **(a) Closed by the gate.**
  - Measured leak: A-pressure at the yard leaves at 2:32.1 and G-fast at 2:22.6, both before the rival car enters at 2:35.3.
  - With the gate, both board and go at **2:59.7**: the first arrival, the blocker, steps out at 2:52.1 and is down at 2:54.7.
  - Each is still faster than its square twin (3:20.5 and 3:11.0).
  - G-fast pays +4 T (2 → 6): the guard's missed 2:30 mark is reported while the cell waits.
- **(b) Closed by the floor and the gate.**
  - Measured leak: route G at the yard with the posted three down goes unfought at 3:23.1.
  - With the floor, the recalled wheelman and lookout hold Go until 3:42.4 (board) or 3:46.9 (fight out, Standing +3).
  - Thinning one rival makes the fight easier, not skipped: the car is free 4.0 s after the spring against 5.6 s, and the crew breaks at 6.7 s against 8.7 s.
  - Thinning all three only trades the ambush for a wait.
- **The gate doesn't move c037:** all 41 of its scripts replay with the same final hash with the gate on, and all 23 wins already Go after `blocker.out`.

**Keel adds** (s1-rulings.json `checks.keel`, 12 scenarios):
- S1-GATE-EARLY-YARD, S1-GATE-SWITCH-OFF, S1-GATE-C037, S1-GATE-LOUD-YARD
- S1-SPRING-LATE
- S1-FLOOR-TIP, S1-FLOOR-LOOKOUT, S1-FLOOR-NO-SPAWN, S1-FLOOR-WAKE, S1-FLOOR-ZERO
- S1-CREW-WIPED, S1-SEE-TAKEDOWN

**Waymark re-composes:**
- route-a-pressure-yard and route-g-fast-yard (Go about 2:59.7);
- route-g-yard;
- the route-b family at the yard, with focus fire;
- new route-b-thin and careless-thin-all;
- route-a-pressure and route-g-fast at the square, against F6's arrivals at rival-drop. The probe's stand-in loses c037's unchanged scripts there with the gate on or off: a re-compose item, not S1.
- New checks: MC-14 (every win Goes at or after `blocker.out`) and MC-15 (no reference route reaches crew-wiped).

**Open (for Aster):**
- §07's "3–5 hostiles" can't hold 3 when the cell has killed or bound the posted three. Proposed wording: "fewer only when the cell has killed or bound crew members before it".
- §06's hold gets one line for the gate.
- Vantage: svc-ambush-1 is 5 m from the service door. Three posted round the yard shoot the carrying Enforcer down unless the cell focus-fires.

Next owner: Keel.

## Aster: routing for S1 = no (2026-10-25)
- GDD §06 (the getaway: "the fight always comes") and §07 (3–5 hostiles, fewer only after crew killed or bound) carry Motive's rule.
- Order: Keel's 059 round (in its worktree) merges first. Then Vantage rules on svc-ambush-1, 5 m from the service door, which makes the yard carry deadly. Then Keel builds the 12 S1-* scenarios with fight-always and rivals-floor (s1-rulings.json checks.keel; fight-always 0 reproduces the leak; c037 is unchanged with the gate on). Then Waymark:
  - re-composes the yard variants (A-pressure, G-fast, G with focus fire, the B family);
  - adds route-b-thin and careless-thin-all;
  - re-checks A-pressure and G-fast at the square (M059-4.5 is in doubt: "rivals reach Vale" with the drop at rival-drop);
  - adds MC-14 and MC-15;
  - holds careless-loud-b-yard until the police blocker is down.

## Aster: after Keel's 0.32.0 (2026-10-25)
- Engine 0.32.0 is on branch `worktree-agent-ad654047fa24cd1cb` (3e3a0bb8..100b5e3a). It merges into the work branch once batch 0.2.118 reports. The merge moves rival-park and the site, so c037 gives way to the next record.
- S1 was built as `fight.safe.leaveUnfought` true, before Sky's answer. Sky said no: Keel's next round flips it and builds s1-rulings.json.
- Order:
  1. Motive's single balance pass on 0.32 (running): loud against the carry, the early quiet routes, route B at the yard and svc-ambush-1, MC-5, with S1 folded in.
  2. Vantage places any moved point.
  3. Keel's round: the S1 flip, s1-rulings, Motive's balance entries, rival-entry's readers in tools/cover and tools/geometry, and the not-built items Motive keeps (rival-body takedown, carry, restrain and stash; a guard-ally point and car cover per stand; the front-stop overhang).
  4. Waymark re-composes once and retires the -block scripts.
  5. Fable: car.blocks' text test, the retired driver's Call dialogue, the drive-here and drive-fight locks, the 20 silent placeholder codes, Keel's draft timeline labels.
  6. Glyph: the shut-stand refusal.
  7. Latch: the 059 port, in the next batch.
- Redline: S10 is 15–25 % slower per run on 0.32 (Combat plus the crew step, about 4 ms a run). Read it with the next perf pass.

## Motive: balance pass on 0.32 (2026-10-26)
The 2026-10-25 run died at the usage limit. Rerun on engine 0.32.0 (100b5e3a) plus the scratch switches in `briefs/059/motive/balance/engine-probe.patch`; with every switch off it reproduces Keel's 0.32 check. Entries for Keel: **`briefs/059/motive/balance-rulings.json`** (M059-B1 to B8; verdicts, effects, checks, re-compose list, open items). Evidence: `balance/results/` (`verdicts-2026-10-26.txt` from `verdicts.sh`; `cand-r` and `cand-c5` from `cand.sh`, the full mission check on a candidate route set; the sweeps; `c037-check-0.31` as the baseline).
- **Base R = the GDD as written:** the S1 gate, late spring and floor, a rival blocker keeps its lane, plus one engine fault fixed (B1).
  - **B1:** 0.32's `JoinFight` starts every posted or walking rival's fight body at (0,0). Keel's 0.32 safe-fight numbers are void.
  - **Not reused from the dead run:** its two switches outside the GDD. The first-down break contradicts §06's more-than-half (B5); Vale as cover for his carrier is a new modifier (B6). Its "SHIPS" rested on them.
- **1. Loud against the carry: holds at both stands.**
  - **Square:** loud, re-composed to drop 3 of the 4 drawn rivals as §06's break needs, wins 5:20.1 at 27 T (+3 case), A 2, Standing −1, seal clock running. The quiet carries: 3–11 T, A ≤ 1, Standing ≥ +2, no seal (MC-2, MC-10 ok).
  - **Yard:** loud is held and lost. The official-entrance officers and the response unit down the Enforcer at 7:42.5, with 11 T spent. Route B: 7:32.6, 3 T, A 0, +3.
- **2. Early quiet routes: both win, and both §07 lines hold.**
  - **Square:** A-pressure 3:20.5 (7 T); G-fast with focus fire 3:11.0 (6 T).
  - **Yard:** A-pressure 3:07.4 (7 T, +1); G-fast 3:07.4 (2 T, +2).
  - **The gate:** it holds Go from Vale-in (2:32.0 / 2:22.5) until the late car's blocker is down (3:03.5). With the gate off both leave unfought, at 2:32.0 and 2:22.5.
  - **Fastest:** quiet 3:07.4–3:11.0 against loud 5:20.1 (MC-6: 129.1 s).
- **3. Route B at the yard: svc-ambush-1 moves to (77.35, −8.0) [S] (B4, Vantage).**
  - **At will loses at all six spots tried** (rivals-reach-vale 7:30.0). All three posted shoot the carrying Enforcer at 562 ‰.
  - **Focus fire wins at every spot**, two locks from the Take (B7, Waymark).
  - **Today's spot:** route-b-board can slip 0 s, and locks 1.0 s late are the limit.
  - **The new spot:** 3 s of slack; locks 3.0 s late still hold. The spot is 15.6 m from Vale's rear door (the long band) and off the service door's line, and it becomes the stand's blocker.
  - **Times:** fight out 7:32.2, board 7:36.0; 3 T, A 0, +3.
- **4. MC-5 holds** (it failed on raw 0.32): A 2:56 early (slip 7 s), B 1:07 (slip 0 s → 3 s with B4), G 4:13 (slip 1 s). MC-1 to MC-12 all pass on the candidate set. MC-0 is 33/39, the six left on Waymark's list.
- **Open for Aster:**
  - **A mismatch.** §06 says hostiles fire nothing while they move and the blocker walks without firing. The engine has fired on the move since 0.15 (my 027 §8.3, Sky 2026-10-21). With B4, the yard result doesn't depend on which reads.
  - **Slack fell from c037** (B 66 s → 3 s, G-fast 19 s → 1 s); the re-record measures it.
  - **The yard's lane post** is 92–340 m by walk from every yard spot.
- **Next owner: Vantage** (svc-ambush-1; the lane-post finding), **then Keel** (B1–B3 and the six BAL-* scenarios with the s1 entries), **then Waymark** (the re-compose list in balance-rulings.json).

## Glyph: the shut stand and Drive's refusals (2026-10-26)
Aster's item 6. Spec only: no code, no Unity, no paid calls. It supersedes the "Shut stand" bullet of "3 · Drive (board 04)" above (that one shut the yard with a lock and gave no route; the engine does not refuse it).

### 0 · A mismatch first (Aster, then Sky/Motive)
**The engine has no refusal for a stand that is not yet open.** `GameDriveCar.DriveCar` (sim 0.32.0) refuses seven ways, in this order: `unknown-verb`, `unknown-stand` (an id that is no `getaway.stands` entry), `drive-no-driver`, `drive-fight`, `drive-busy`, `drive-here`, `drive-held`. rulebook `cell.driveCar` and host-interface.md list the same codes and no other. A Drive to `service-drive` before Vale's car is parked is **accepted**, by design: GDD §07 ("on the drive first, it blocks his way and he stops in the road"), K5, scenario P59 (`car.blocked`, `CarAtMouth`; the turned driver stops at the drive's mouth on Apron Road and the Take is about 18 m of carry on a public pavement). So "not yet open" is a **price**, not a lock. GDD §10 says only "a stand not yet open is marked so".
- **Spec below (default):** the mark is a caution, the order stays orderable, nothing refuses. A lock here would teach that a lock means the engine will refuse, and it would hide a play §07 keeps (the early car as a cost).
- **If Sky wants a hard refusal instead:** Keel adds one code, `stand-shut`, to `DriveCar` (no new rule needed beyond "the stand opens when `ValeCarParked`"). §4 below is ready for it. Do not build it unless Aster says so; unknown = reject.

### 1 · When the mark shows (told and learned only)
Shown while all of: the getaway car, or an agent in it, is selected (the ghosts' own condition) · the driver is turned (the player knows route B: Vale's car will come round; unturned, the yard is just a stand and shows no mark, so the ghost never leaks the plan) · `!ValeCarParked`. It clears the tick `car.parks` logs (Vale's car in the bay or stopped at the mouth). Hint H20 ("Drive the [car] round.") fires on the same trigger, so the mark clearing and the hint arriving are one moment.

### 2 · The yard stand at rest (selected car), shut-looking and orderable
Over the board-04 footprint (dashed cream rounded rectangle 4.6 × 1.9 m, radius 0.5 m, 2.2 px over the ink-deep underlay, chevron at the nose, `disc_dark_24` with `car` at the centre):
- **Footprint at 50 % alpha, dash 3 on / 5 off** (the live stand is 7/6 at full alpha). The centre disc drops to 50 % too.
- **Plate:** `plate_dark`, 60 × 26 px, centred 14 px above the footprint's far edge, never over the nose. Left: `nextevent` 14 px, cream. Right: Vale's cameo (`badges/npc_councillor`) 18 px in the chrome ring (2 px #cbc6c3, 1 px ink-deep). Reads "wait for Vale's car", two icons, no words. Not `lock`: the lock is kept for what the engine refuses.
- **Motion cue:** while Vale's car is under way round (`car.leaves` at 6:13 to `car.parks` at about 6:25.9) the dashes crawl along the outline at 6 px/s (the car is coming); parked or not yet moving they are still. No colour change.
- **Off screen:** the edge chip keeps its `car` + `pointer` and takes the same 50 % alpha.
- **Opening:** at `car.parks` the plate fades out in 0.25 s, the dash goes to 7/6 and full alpha in 0.25 s, then the cream outline gives one 0.6 s pulse (scale 1.0 to 1.04 and back). Alt on the plate shows the glossary word `stand-after-vale`, "After Vale's car", in the 13.5 Jost tag (Alt only; the plate has no sentence).

### 3 · Hover and the order anyway
- **Hover (still shut-looking):** the footprint turns solid at 70 % alpha, cream, not turquoise-light: the turquoise is the live stand's. The route preview is drawn as for a live stand (cream dashed 2.4 px, 7/6, chevrons every 3 m) **and** Vale's car's route in the same style from the official bay with the board-03 label "6:13 → 6:25" (only when the Watch on the driver has taught it; else just the pill). Where the two lines meet at the drive's mouth the preview shows the board-03 hand-off ring (dashed, 1.2 m).
- **Pill (as built, `pill_status` 38 px, slice 0.43):** `disc_dark_24` with `car`, dim head "Hold ·", body `Drive · 28 s`, the driver's face. Added right of the faces: **the caution plate** the Watched-spot offer already uses: `plate_dark` 26 px with the kit's `stripes_warn` block 9 px at its left, then Vale's cameo 18 px and `nextevent` 14 px. Nothing new in the kit: one plate pattern, reused.
- **Alt (held) on the hover:** the zero fields and the consequence, numbers and icons only: at the mouth ring the carry, `s_carrying` 14 px + `18 m` (read from the stand and the bay, never hard-coded; ~18 m today).
- **Commit:** the usual 0.5 s hold (sweep, "Let go", the ring). The engine accepts it. No pop, no confirmation card, no Trail or Alert (price $0, 0 T, no Alert). When the car lands and Vale's car stops short (`car.blocked`, `CarAtMouth`), the hand-off ring sits at the mouth on Apron Road instead of at the bay, the board-03 pill moves with it, and Fable's beat shows (§6).
- **Budget:** the ghost plate 1,560 px (0.075 % of 1080p), the pill plate about 1,100 px (0.05 %); the shut stand's captured 6.73 % in board 04 becomes 6.80 %; the hover 7.11 % becomes 7.2 %. Far inside 15 / 22 / 30 %.

### 4 · If the engine refuses it (`stand-shut`): the pop, ready
Same footprint, plate and hover as §2 but with **no route preview** and the pill replaced by the shut pill (struck, no colour; the same grammar as a shut Go). The refusal pop on a try: `plate_dark` solid, 26 px high, icon `p_time` 16 px (the code is a wait, like `driver-too-late`; HostLess.PopIcon already maps waits to it) then the short in Jost Bold 13.5 cream: **"After Vale's car"** (the glossary's `stand-after-vale`, 3 words). Anchored at the footprint's world point (not the cursor), 2 s, `Feedback.Refused` with one footprint pulse. Never `lock`.

### 5 · The real refusals of Drive, and how each differs from "a fight blocks the car"
All pops: `plate_dark` solid, 26 px high, the icon 16 px cream then the short in Jost Bold 13.5; 2 s; anchored at the **stand's footprint** (world point, edge chip if off screen), plus one footprint pulse. Words are text.json's `lock` entries (Fable owns them). HostLess.PopIcon today has no case for any `drive-*` code (they fall to `badges/mark_no`): Latch adds these.

| code | when | short (text.json today) | pop icon | other marks |
|---|---|---|---|---|
| `drive-held` | a combatant holds the car before the fight (CarHolders, `ids` names them) | "Blocked · down {holders}" | `lock` | the car-held grammar, unchanged: every other stand's ghost shut (struck, 45 % alpha as `goBlocked`, no route); the shut pill's tail shows `lock` + one `signal_police` / `signal_rival` cameo badge per holder (057 §4.2); holder marks and tethers pulse (`HolderPulseAt`) |
| `drive-fight` | the final fight has started | "Fight on" | `car` struck through (a 2 px cream diagonal, the shut-way strike), no lock | no ghosts are drawn at all once the fight starts, no holder pulse; Go is the lit way on the car's ring |
| `drive-busy` | someone aboard is mid-act | "Car busy" | `p_time` | the busy chip's bar on that agent's portrait is already the cause |
| `drive-no-driver` | the ordering agent is not in the car (stray order) | "No driver" | `car` | the car marker's seat pips are empty |
| `drive-here` | the stand it is at (stray) | "Already here" | `car` | the stand under the car has no disc, so it is never offered |
| `unknown-stand` | never from the UI | "Not a stand" | `badges/mark_no` | none |

**The difference, in one line:** the lock-and-holders is the **fixable** refusal (down the holders and Go or Drive works: it is Go's `car-held` and Drive's `drive-held`, one grammar, one lesson); the struck car is the **permanent-until-the-end** one (nothing on the street clears it; the only way out is Go). Pre-fight, a lock on Drive pulses the same holders as a lock on Go. Once the fight is on, the engine answers `drive-fight` before `drive-held`, so Drive never shows the lock in a fight: the holders stay on Go's lock alone.

### 6 · Draft words for Fable (≤ 4 words; yours to change)
- `stand-after-vale` (exists): "After Vale's car". Alt on the plate only.
- `car.blocked` / `car.parks` at the mouth, the beat: "Vale's car stops short." (4). The story is the cost, said once.
- `drive-no-driver`: "Board first" (the fix, 2 words) instead of "No driver".
- `drive-here`: "Already here" is fine; "Car's here" if shorter is wanted.
- `drive-fight`: "Fight on" is fine; "Go, don't drive" is the fix-first alternative (3).
- If `stand-shut` is added: the short is "After Vale's car"; the long line (Alt) "Vale's car isn't round yet. Wait, or block his way." (placeholder).

### 7 · Hand-off
- **Latch (059 port):** build §2, §3 and §5 (the six pop mappings in PopIcon, the footprint anchor in `Pop`, the struck-car composite); §4 only if Keel adds `stand-shut`. Kit pieces by name: `plate_dark`, `pill_status`, `disc_dark_24`, `stripes_warn`, `icons/singles/{car, nextevent, lock, p_time, s_carrying, pointer}`, `badges/{npc_councillor, signal_police, signal_rival, mark_no}`. No new asset. Check each state cropped at 2x before it goes to Sky; I will review the 2x.
- **Fable:** §6.
- **Aster:** §0, whether the engine should refuse (Sky's call; my default is no, the mark is a caution) and the contradicted line in board 04's shut-stand bullet, now superseded here. Board 04's picture still shows the lock; I have not redrawn it (spec only).

## Vantage: svc-ambush-1 and the yard's lane post (2026-10-26)
- **svc-ambush-1 moves to (77.35, −8.0)**, B4's spot as Motive gave it (was (57.25, −17.75)): `contracts/missions/handover/mission.json`.
  - **Checked:** 10.6 m from the car, 15.6 m from rival-vale-door, 17.3 m from 'Service lane', a line to both, outside the lane. Pavement, floor 0, a free cell. V-1 at yaw 210 is 100 %.
  - **Cover:** none. The site's cover has no point in the yard, so all five yard spots stand in the open.
  - **Nearby spots are no better:**
    - (77.35, −7.5), (77.85, −8.5) and (76.85, −7.0) give the same 3 s.
    - (77.85, −7.0) and five spots 6.8–8.0 m from the post lose route B. route-b and route-b-board no longer compose.
    - The reason: the blocker posts sooner and fires standing (562 ‰ against 281 ‰ walking). He downs the Fixer at 7:31.5.
  - **Evidence:** `out/scratch/vantage/059b4/` (cand-*.txt, late-sweep.txt), on a copy of Motive's probe tree with his R switches. c5 reproduces his check: route-b-board slip 3 s, MC-0 33/39, MC-1..12 ok.
- **The lane post: Motive's 92–340 m is a units slip.**
  - `Path.Length` is in mm, and the probe's `Row()` divides it by 100. The true walks to (86, −5) are 9.16 m from (77.35, −8.0), 11.5 m from svc-ambush-5 and 14.5 m from svc-ambush-4, up to 34 m from today's svc-ambush-1.
  - The yard's pavement runs onto Apron Road (the grid is open across the kerb), and the blocker fights: rival-3 is out at 7:32.1. No point or route moves for reach.
- **M059-4.4(c) against B4 (Motive):** (77.35, −8.0) walks 9.16 m (5.7 s), 1.16 m over (c)'s 8 m. Every spot inside 8.4 m loses route B, so the two can't both hold. (c)'s own reason, the blocker in the lane inside the 6.5 s carry and Board, still holds at 5.7 s. I ask Motive to state (c) as ≤ 6.5 s at 1.6 m/s (≤ 10.4 m).
- **The real fault in the post (Motive and Keel; Splice exports):**
  - The yard's lane starts on Apron Road, 16.65 m from the car. The open post, `LaneAt(fight-block-near)`, is therefore 19.4 m from the car.
  - That is beyond fight-block-far (13 m) and pistol-max (16 m), against block-far's own reason ("in each other's effective reach").
  - Starting the lane at the car, along the drive (Motive's PROBE_YARDLANE), puts the post at the drive's mouth (79.35, −15.0), 10 m out. But it takes route-b-board's slack from 3 s to 0 s with this spot.
  - So it is a rule and a re-balance, not a point or route move. Nothing for Waymark.
- **The checks:**
  - MC-11 reads only the committed site's start-stand posts (the square's 9), so it can't see the yard's. Extend it to every `getaway.stands[]`.
  - The committed site still holds the old point: `tools/sim/sim.test.ts`'s site test fails on svc-ambush-1 alone until `bun run sim:site`.
- **Next: Keel.**
  - Run `bun run sim:site` first.
  - Re-measure at (77.35, −8.0): BAL-YARD-FOCUS (win; both locks 3.0 s late), route-b-board's slack line (3 s), and the blocker's walk to (86, −5) (9.16 m).

## Motive: the units slip, (c) and the yard's lane post (2026-10-26)
Aster's three rulings on Vantage's section. Entries in `briefs/059/motive/balance-rulings.json` (`corrections`, M059-B4 fixed, new M059-B9 and M059-B10, `checks.keel` + `checks.keel_after_sky_B10`). Evidence in `briefs/059/motive/lane/` (`results/yard-geometry.txt`, `results/outcomes.txt`, `results/cand-*.txt`; plans ylA–ylJ, `mk.py`, `cand.sh`). The probe is `balance/engine-probe.patch`, regenerated, which still applies to 100b5e3a.

**1. The units slip: fixed.** The probe's `Row()` divided `Path.Length` (mm) by 100. It now divides by 1000, in the patch and in the scratch tree.
- The true walks to (86, −5) are 9.16 m from (77.35, −8.0), 11.46 m from svc-ambush-5, 14.46 m from svc-ambush-4 and 31.5 m from the old svc-ambush-1.
- B4's 91.6 m and vantage[1]'s 92–340 m are corrected. No verdict, slack or outcome moves, because they all come from play, not from `Row()`.

**2. M059-4.4(c): restated in seconds (M059-B9) [S].** The yard's blocker reaches his post within the carry plus Board from Vale's rear door: 6.5 s, or 10.4 m at his 1.6 m/s.
- The limit is read from the rules, never fixed in metres.
- It holds at 9.16 m / 5.7 s on today's lane and at 7.49 m / 4.7 s on the lane in ruling 3.
- With the gate nothing leaks if he is late, so (c) is now about readability.

**3. The yard's lane: it starts at the car (M059-B10), and that needs a GDD clause.**
- **What the export does now.** `tools/sim/getaway.ts` projects the car onto Apron Road, so the lane starts 16.65 m out at (86, −15) and the post lands 19.42 m from the car.
- **Measured on today's lane** (svc-ambush-1 at (77.35, −8.0), base R):
  - No plan that goes for the blocker wins: ylA and ylB lose to rivals-reach-vale at 7:33.0.
  - Every yard win breaks the crew (Standing +3), so §06's choice at the car (board and go, or fight it out) is gone at the yard.
  - A rival standing in the drive 8–16.65 m ahead of the car, its only way out, doesn't hold it.
- **On the lane from the car** (PROBE_YARDLANE=2: the drive 3 m wide to Apron Road's centreline, then the road 12 m wide, 30 m in all; post (79.35, −15.0), 10.0 m out):
  - Board and go composes: ylA downs the blocker at 7:28.0 and Goes at 7:31.9, Standing +2, no break.
  - Fight it out is unchanged (7:32.2).
  - The early yard routes Go 7.1 s sooner (apy-1, gfy-1: 3:00.3 against 3:07.4).
  - loud-yard is unchanged.
  - MC-0 to MC-12 are as cand-c5 except route-b-board's line.
- **The cost.** route-b-board's slack falls from 3 s to 0 s with today's locks. Re-composed it gets 2 s (ylB: the Scout locks the blocker so he answers her, and the Fixer locks rival-1 then rival-2; ylF: the Tech steps out). Today's lane gives 3–4 s. Moving the spot doesn't change this: three other spots also give 2 s.
- **The drive must keep its own width.** At the road's 12 m, rival-tail (90.5, −17) falls inside the lane and breaks 4.4(d). Every svc-ambush, svc-police and rival point stays outside the lane at 3 m.
- **Ruling: the lane from the car.** I accept 2 s for keeping board and go at the yard, and the post's own reason.
- **It is not inside §06 as written.** §06's Drive line separates "a carriageway" from "a drive", so "the getaway route's carriageway" excludes the drive. That letter puts the post outside the reach its own sentence gives as the reason. Proposed wording for Sky, in §06 the hold:
  > or on the exit lane: the first 30 m of the getaway route from the car, at the width of the way it runs on (a drive at the drive's width, then the carriageway at the road's), derived from the stand's getaway route (no route, no mission: unknown = reject).
  - The blocker sentence stands. "10 m along it" is then 10 m from the car at every stand.

**For Keel.**
- **Now:**
  - Build nothing for B10.
  - Add BAL-YARD-POST: the blocker reaches his post within 6.5 s, read from the rules.
- **If Sky says yes:**
  - Build the stand's exit lane as parts, each a polyline with its own half-width and square ends at the junction. `InExitLane`, `NearestInExitLane` and the hold read the parts. `LanePointAt` runs along the whole line from the car.
  - Splice's `exitLane`/`stands.ts`: a car on a drive starts the lane at the car and follows the drive at its width to the first road's centreline, then the road. The site exports the parts. The square's lane stays byte-identical.
  - Scenarios: BAL-YARD-LANE, BAL-YARD-BOARD, BAL-YARD-FOCUS (2 s) and BAL-YARD-EARLY (the `keel_after_sky_B10` list).
  - Waymark then re-composes route-b-board as ylB.
- **If Sky says no:** today's lane stands. The yard has no board and go, and route-b-board stays a crew break (3 s).

**For Sky (hostile fire on the move).**
- **The record.** Sky answered on 2026-10-21, in brief 027 `motive/tuning.md` §9.7 item 3, not under Q11: "Hostiles firing as they walk in `today`: OK".
- **What the engine does now** (since 0.15, GameAi):
  - Every hostile fires as it walks, at the walk's odds: ×500, so 281 ‰ instead of 562 ‰ untrained at effective range. A step to cover fires too (`ai.cover.stepFires` true). Only the lab's `hostilesFireMoving` can turn it off.
  - The blocker has no exception: he fires on the way to his post.
- **What changes if the engine follows §06.**
  - Every advance and step to cover becomes silent, so the cell can walk-and-fire at police who can't answer. That is the kite §8.3 closed: loud fights get cheaper.
  - Every fight number since 0.15 (c037, the fight lab, this pass) would need re-measuring.
  - Holding only the blocker's fire changes nothing measurable at the yard: outcomes are identical on both lanes, and the slack is 2 s / 0 s as without it.
- **Recommendation:** keep the engine and bring §06's text to Sky's answer:
  - hostiles: "firing as they walk, at the walk's odds";
  - the blocker: "It walks there without stopping, firing as it walks".

Next owner: Aster (the B10 clause to Sky), then Keel.
