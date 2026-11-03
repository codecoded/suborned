# 059 · The rival routine: five people, one car, a timetable (Pulse, 2026-10-25)

For Keel to build without asking. Brief 059 §2.2 and §2.3 (K7 to K11) say what the crew does; this file says **how each body moves, where it stands, what it does with its hands, what it follows, and what happens when the cell breaks the timetable**. Sky's answers (2026-10-25) are fixed: **1a** the cell's car is brought round by Drive · **2a** the rivals post round the cell's car in plain sight from 2:46 · **3a** an unturned driver drives off at 6:13 and the mission ends. GDD v0.25.0 §07 (`rivals`, `schedule`, `final fight`) is the source on any conflict; report a mismatch to Aster.

Nothing under `sim/`, `contracts/` or `unity/` is touched. Tags: **[B]** built, or fixed by the GDD · **[S]** spec, starting value, tune by playtest (Motive owns every number).

| file | what |
|---|---|
| `rival-routine.md` | this spec |
| `rival-routine.values.json` | the new rulebook values (same shape as `rulebook.json`), the beat lists, every point and route the spec names, what each reuses |
| `check.py` | `python3 -I briefs/059/pulse/check.py` (`--write` writes `measures.json`): every place and route named exists or is listed as new; the new points sit on the right grid; cones; the default timetable; the beats; the warn trip; the tail; exit 1 on a failure. Reads the site, mission.json and the two files above; writes nothing else |
| `measures.json` | the figures quoted below, from the site's grids (the router `briefs/059/routes.py` used; ticks as `CarProfile`). Keel's `S.Drive.CarRoute` and `S.Grid.Route` are the authority: compare to the tick or two |

## 1 · The names this spec uses

Every place, stand and route in the spec is in this table. `check.py` parses it, resolves each row against `sim/data/handover.site.json` and `mission.json`, and fails on any place-like token in the text that is not here.

| kind | name | status | what |
|---|---|---|---|
| place | `rival-park` | re-site | (84.0, -31.0) today → (90.5, -31.0): Apron Road's south kerb, eastbound lane, nose east (Vantage) |
| place | `rival-entry` | existing | (36.0, 10.0). Unused by the car after this change; kept until Keel's scenarios and `tools/cover` stop reading it, then Vantage removes it |
| place | `rival-car-entry` | new | (37.5, 15.75): Orbit Walk at the window's east edge; where the car appears and where a tipped car leaves the map |
| place | `rival-drop` | new | (34.0, -38.0): Orbit Walk's north lane by the square; the square stand's rival stop and the drop |
| place | `rival-lookout` | new | (94.0, -27.0): the south pavement of Apron Road, facing the official bay |
| place | `rival-lookout-exit` | new | (94.0, 15.75): the south pavement at the window's east edge; where a warned lookout leaves |
| place | `rival-tail` | new | (90.5, -17.0): Apron Road's eastbound lane abreast of the service drive's mouth; where the car stops when it follows Vale's car, and the service-drive stand's rival stop |
| place | `rival-vale-door` | new | (64.55, -16.9): the yard side of Vale's car in the service bay; where the wheelman walks |
| place | `rival-front-stop` | new | (63.75, -50.25): Rocket Lane's carriageway abreast of the Office approach, 5.9 m from it; where a crew drawn by gunfire stops |
| place | `rival-lookout-word` | derived | the free walk cell nearest the point 1.5 m from `d:Meet driver` toward `rival-lookout`; where the lookout speaks to the driver |
| place | `rival-front-offsets` | derived | a drawn crew's standing places at the Office front: `d:Office approach` + k × 90 cm along the perpendicular of its facing, k = 0, +1, -1, +2 for rival-1 to rival-4 |
| place | `ambush-1` | existing | (21.75, -44.25) the square stand's 1st spot |
| place | `ambush-2` | existing | (-5.75, -37.75), stand cell (-5.75, -36.25): 2nd |
| place | `ambush-3` | existing | (22.0, -42.0): 3rd |
| place | `ambush-4` | existing | (-6.0, -44.0): 4th (the lookout's in a fight) |
| place | `ambush-5` | existing | (14.0, -36.0): 5th (the wheelman's in a fight) |
| place | `d:Getaway bay` | existing | (8.5, -48.2): the square stand's car point |
| place | `d:Official drop-off` | existing | (78.2, -31.0): the official bay; the lookout's base facing |
| place | `d:Official entrance` | existing | (76.25, -30.0): the official door |
| place | `d:Meet driver` | existing | (78.1, -33.7): the driver's post |
| place | `d:Office approach` | existing | (64.0, -44.4): where a drawn crew stands |
| place | `service-bay` | existing | (63.75, -15.0): Vale's car in the yard |
| stand | `service-drive` | new | (69.35, -15.0), nose +x (out to Apron Road), 1 m behind Vale's car; its five ambush spots and four police stands are Vantage's (MC-11). Its rival stop is `rival-tail` |
| route | `getaway` | existing | the square stand's exit lane (r_orbit_w to the exits) |
| route | `official` | existing | Vale's car's arrival; not used by this routine |
| route | `rivals` | new | Splice: off_seagates → Orbit Walk → Rocket Lane → Apron Road → `rival-park`, cut to the window. The engine does not read it (it routes on the drive grid between the points); an SX rule checks its ends against `rival-car-entry`, `rival-drop`, `rival-park`, and Glyph draws the approach line from it |
| route | `getaway-service` | new | Splice: Apron Road east from the service drive, the exit lane of the `service-drive` stand |

Frame: block frame, metres in the text; the site's integer centimetres in `rival-routine.values.json` (x south, y east).

## 2 · Rules for the whole routine

1. **Integers only.** Positions in cm, paths in mm, speeds in mm a tick (cm a second), 10 ticks a second. A body on foot moves `rivals-speed-cm-s` 160 [B] mm a tick; the car moves as `CarProfile` does (500 on a carriageway, 200 on a drive or in a bay [S]). A leg's ticks are `ceil(path mm / speed)` on foot and `CarProfile(route).Length - 1` by car. No float, no `Random`; the beats' yaw uses the integer table in `rival-routine.values.json` (`beats.rule`).
2. **Ids.** The crew is `rival-1` to `rival-5` (the ids a final-fight hostile already has), the car is `car:rivals`. **Roles are by id and never change:** `rival-1` to `rival-3` the posted three (P1 to P3) · `rival-4` the lookout (L) · `rival-5` the wheelman (W).
3. **Order in a tick.** The car first, then its riders, then the others, in id order (ordinal string compare), after security (`briefs/011/pulse-fight-ai.md` §1.2, the engine's order). Every choice that can tie (the nearest spot, the next out of the car, two triggers in one tick, the blocker's walk) breaks on the **lower id**, spots included (`ambush-1` before `ambush-2`).
4. **Trigger precedence in one tick** (an earlier one is applied first, the later ones see its result): gunfire draws them (F5) > a false tip lands (F3) > the final fight starts (F6) > the cell's car starts to move (F1) > Vale's car starts to move (F2) > the lookout's word (F4) > the timetable (§4).
5. **State is a function of ticks and orders.** Every state below is hashed (§9). A beat is a pure function of the tick the member's post began, so it needs one hashed integer (that tick); the beat's name is a host read, never hashed, and **no rule reads a beat name**. What a beat can change in play is the facing (hashed through the pose) and the lookout's position.
6. **Rivals raise no Alert and no Trail by any of this.** Their moves are not acts a witness reports (§05: rivals don't report). Nobody blocks anybody's walk (pulse-fight-ai §1.6); cars do not collide with cars (see §11).
7. **Their sight is unchanged [B].** A rival on foot sees by a cone, reach 18 m, half 8 m (`perception.rivals`), from the tick it is on the map (`ai.intent.rivals.street`); a rival inside the car has no cone. Who a crew fires on is unchanged (`ai.intent.rivals`: calm → drawn, Vale; alerted → + seen), and `rivalsSaw` stays one set for the crew.

## 3 · The crew and the car

**The car** (`car:rivals`) is a car on the drive grid with these states. It is a target by id from `Entering`, and its cover points (from `tools/cover`, `car/rivals`) exist only while it is `Parked` or `Stopped` (a car that drives off takes its cover with it, as the official car's does).

| state | the car | leaves to |
|---|---|---|
| `Off` | not on the map | `Entering` at its entry tick |
| `Entering` | riding `rival-car-entry` → its stop | `Dropping` |
| `Dropping` | stopped at `rival-drop`; the riders step out | `Rounding`, or `Stopped` (§6 F6) |
| `Rounding` | riding the stop → `rival-park` | `Parked` |
| `Parked` | stopped at `rival-park`, nose east, W aboard | `Tail`, `TipOut`, `ToStand`, `Front` |
| `Tail` | riding `rival-park` → `rival-tail` | `Stopped` |
| `TipOut` | riding `rival-park` → `rival-car-entry` | `Away` (off the map) |
| `Away` | off the map | `TipBack` |
| `TipBack` | riding `rival-car-entry` → `rival-park` | `Parked` |
| `ToStand` | riding to the cell's stand's rival stop (§6 F6) | `Stopped` |
| `Stopped` | stopped at a stop that is not `rival-park`, riders stepping out | stays |
| `Front` | a crew drawn by gunfire: riding to `rival-front-stop` (§6 F5) | `Stopped` |

**A member** is in one of: `Aboard` (his pose is the car's) · `Stepping` (the tick he steps out is fixed) · `Walking` (a grid route at 160 mm a tick) · `Posted` (a place, a facing and a beat loop) · `Boarding` · `Gone` (left the map: the lookout's exit, off the roster for good) · `Down`. A member's `Gun` (health 3, [B] `health-rival`) exists from the tick he is on the map and is the **same gun** in a final fight (§7): a rival hit on the street is still hit in the fight.

**Who stands where at each stand** (the stand is where the cell's car is, or is driving to, at the tick that decides it):

| member | at the square stand (`d:Getaway bay`) | at `service-drive` | how |
|---|---|---|---|
| `rival-1` | `ambush-3` (13.7 m walk from the drop) | the nearest free of the stand's 1st to 3rd ambush spots, by grid walk from where he stands | the posted three take the stand's **first `rivals-posted` (3) [S]** spots, in the stand's own order: in id order, each takes the spot with the shortest grid walk from where he stands when he steps out (or when he re-plans), ties the lower spot id |
| `rival-2` | `ambush-1` (14.5 m) | as rival-1, after it | |
| `rival-3` | `ambush-2` (54.5 m) | as rival-2, after it | the farthest spot goes to the last chooser |
| `rival-4` | `ambush-4` | the stand's 4th spot | the lookout and the wheelman take the 4th and 5th by role, whatever their walks |
| `rival-5` | `ambush-5` | the stand's 5th spot | |

With fewer than three posted alive the farthest spots stay empty. A spot is a `Stand` cell (`ambush-2` stands on (-5.75, -36.25)).

## 4 · The default day (no order touches the crew)

The 3:10 park is [B] (`schedule.rivalsArrive` 190 s, §07). **Everything before it is computed back from it in ticks**, so the timetable is a fixed function of the site and the rulebook and a re-site cannot move the park:

```
tPark       = T(190)                                  [B] 1900
legB        = CarProfile(CarRoute(rival-drop -> rival-park)).Length - 1
tDropLeave  = tPark - legB
tDropArrive = tDropLeave - T(rivals-drop-s)           [S] 8 s
tEntry      = tDropArrive - (CarProfile(CarRoute(rival-car-entry -> rival-drop)).Length - 1)
```

Site reject (SX rule) if `tEntry <= 0`. With the grid's lengths (reference, `measures.json`):

| at | who | does | measured |
|---|---|---|---|
| before 2:35.3 | all | `Off`: nowhere on the map (GDD §07) | |
| **2:35.3** | car | `Entering`: appears at `rival-car-entry`, rides west along Orbit Walk | 55.5 m, 11.1 s |
| **2:46.4** | car | `Dropping`: stops at `rival-drop` | |
| 2:48.4 | rival-1 | steps out (`rivals-step-s` 2 [S] after the stop, then every 2 s in id order), walks to `ambush-3` | 13.7 m, 8.6 s |
| 2:50.4 | rival-2 | steps out, walks to `ambush-1` | 14.5 m, 9.1 s |
| 2:52.4 | rival-3 | steps out, walks to `ambush-2` | 54.5 m, 34.1 s |
| **2:54.4** | car | `Rounding`: leaves 2 s after the last is out, round by Rocket Lane and Apron Road | 77.6 m, 15.6 s |
| 2:57.0 · 2:59.5 · 3:26.5 | rival-1 · 2 · 3 | **posted**: the beat loop starts (§5) | |
| **3:10.0** | car | `Parked` at `rival-park` [B] | |
| 3:15.0 | rival-4 (L) | steps out (`rivals-out-s` 5 [S] after the park, the brief's 3:15), walks to `rival-lookout` | 6.0 m, 3.8 s |
| 3:18.8 | rival-4 | **posted**, facing the official bay; the loop starts | |
| 3:10 on | rival-5 (W) | stays aboard, never leaves the car unless the car cannot reach where the crew goes | |

Notes: the car's route is **the same wherever the cell's car is**, so the drop is one fixed event; where the three *walk to* is the cell's car's stand (§6 F1). The `rivals.enter`, `rivals.drop`, `rivals.park` (existing text) and `rivals.post` notes land at 2:35.3, 2:46.4, 3:10.0 and each posting (Fable writes the words; §11).

## 5 · Posts and idle business: nobody stands still for minutes

A posted body keeps **one place**, a **base facing** and a **beat loop**. Beat lists are data (`rival-routine.values.json` `beats`; a rulebook section `rivals.beats`, unknown = reject), each loop is a list of `{pose, seconds, yaw}`. The loop's tick is `(tick - postedTick + phase) mod period`.

| who | base facing | loop (pose seconds, yaw) | period |
|---|---|---|---|
| P1 to P3 (`posted`) | from the post toward the cell's car at the stand | watch 10 · read-paper 8 (+30°) · watch 8 · check-watch 3 · lean 9 · look-around 2 (-60°) · look-around 2 (+60°) | 42 s, phases 0, 10 and 32 s |
| L (`lookout`) | toward `d:Official drop-off` | watch 8 · pace-out 6 · watch 5 · pace-back 6 · read-paper 6 (+30°) · check-watch 3 | 34 s |
| W (`wheelman`) | the car's heading | wheel 9 · mirror 4 · wheel 8 · window-arm 8 · check-watch 3 · mirror 4 | 36 s (aboard: the host's view only) |

- **Position never changes** for the posted three: the beats are in place, so they stay "standing still beside cover" for the hit modifier (`mod-cover`) and the cover rules (§06) work as for any body that stands at a point. `lean` is played only where a cover point lies within `cover-snap-cm` (70) of the post; elsewhere it plays as `watch`. The only moving beat is L's: **pace-out** walks him `lookout-pace-cm` 200 [S] along +y (away from the bay, along the kerb) and **pace-back** returns him, linear over the beat; every cell on the line is free ground (checked).
- **Yaw** is an offset from the base facing: 0, ±30°, ±60°. A cone's half-angle is about 24° (18 m, half 8 m), so a +30° beat turns the car out of the cone and a ±60° look-around too: those are the **windows** a Watch shows. The phases are chosen so rival-1 and rival-2 are never looking away from the car together, nor rival-1 and rival-3 (`check.py` §5). At the square stand rival-3's post (`ambush-2`'s stand cell, 18.6 m) is beyond his own 18 m reach: the car is never in his cone, he is the far flank.
- **The longest run of one pose is 10 s** (limit `beats.maxBeatS` 12), every loop has 5 or more beats and is 30 s or longer, no two beats in a row repeat a pose and yaw. A rule for the next designer: a pose changes at least every 12 s; a test fails a loop that breaks it.
- **No world prop is used.** The civic block has no bench or kiosk (its objects are the fountain, planters and café tables), so the business uses only what a person carries: a paper, a watch, a hand on a wall.
- The pose vocabulary for Forma (**new**, ten): read-paper, check-watch, lean, look-around, talk, wheel, mirror, window-arm, step-out, board; existing: watch (idle stand), walk (pace and every walk).
- **Walking** bodies face their heading; they join the loop at beat 0 on arrival.
- **Idle in the car:** W's loop is for the host's window view only; the car itself needs nothing.

## 6 · What they follow, and when

### F1 · The cell's car moves: the three walk after it

- **Trigger:** the tick the getaway car leaves its stand on a Drive order (K6; the destination stand `ds` is known then). L and W do not follow (L's post is the bay; W's car follows Vale's car).
- **Effect, at `trigger + T(rivals-follow-s)` (2 s [S])** and for each of rival-1 to rival-3 that is Posted, Walking to a post, or still aboard at the drop (he decides at his step-out tick instead): re-plan. Take the **first 3 spots of `ds`**, assign by the greedy rule of §3 from **where he stands now**, walk the grid route at 160 mm a tick, post on arrival with the base facing toward `ds`'s car point.
- **Again:** a second Drive re-plans again from where he is; a Drive back to the stand he is already assigned to is a no-op. Walks are not re-timed by the car: the three arrive when they arrive (the square's `ambush-1` to the service drive is about 59.5 m, 37 s, so they land after the car).
- **Ends** when the ambush springs (`FightSprung`): the fight's own machine moves them. A Drive after the spring is refused by K6 anyway (the car is held).
- **A hostile in a started, unsprung fight** is the same body: its `Home` re-plans the same way and it stays `routine` (lurking).

### F2 · Vale's car moves: the car follows (the tail)

- **Trigger:** the tick Vale's car starts to drive from the official bay with Vale in it (`tValeIn` = 6:13.0 with `vale-in-s` 3), and **also** whenever the rival car is `Parked` while Vale's car is **not** in the bay and Vale is not yet `WithEnforcer`. The second clause closes the hole of a tip that ends after 6:13: the car parks again and follows.
- **Conditions:** the car is `Parked` at `rival-park` with W aboard and alive · no fight has started · the crew is not drawn · Vale is still in his car (not `WithEnforcer`).
- **Effect:** at `trigger + T(rivals-follow-s)` the car rides `rival-park` → `rival-tail` (14.0 m, 2.8 s) and is `Stopped`; W steps out `rivals-out-s` (5 s) after it stops and walks the grid to `rival-vale-door` (25.7 m, 16.1 s); there he is `Posted` (the `posted` loop, `lean` as `watch`) and **`RivalReachesVale`'s yard case reads him**: W within the Take's act reach of Vale's car before `take.done` fails `rivals-reach-vale` (K9).
- **Reference (c034-style, no tip):** Vale in 6:13.0 → car moves 6:15.0 → stops 6:17.8 → W out 6:22.8 → at the door **6:38.9**, 12.6 s after Vale's car parks (6:26.3: 13.3 s from 6:13.0; the brief's 6:25.3 counts from 6:12) and 25 s before the earliest Take could end (7:04.3). The brief said about 6:30; this one is 9 s later because the car stays in its own lane. If Waymark wants 6:30 the stop moves to the drive mouth (82, -15) and the car crosses the road: 17.6 m on foot instead of 25.7 m.
- **Does not apply** when Vale's driver is unturned (the mission ended at 6:13) or when the car is away (a tip), or W is down.

### F3 · A false tip lands: the car and W go

- **Trigger:** the Scout's Mislead lands (44 s [B] after the order). **Orderable only while the car is `Parked` at `rival-park` with W aboard**, the Scout within act reach (6 m [B]) of the car; the act is cancelled, with no effect, if the car leaves `Parked` before it lands (the F2 follow, a fight, a draw). The refusal reads "no car here" or "the crew is out of the car" (Fable).
- **Effect (at the landing tick, no reaction delay):** the car rides `rival-park` → `rival-car-entry` (87.7 m, 17.6 s) with W, `TipOut`, then `Away` off the map. It **parks again at `landing + T(false-tip-s)` (230 s [B])**: it re-enters at `parks − (CarProfile(entry → park).Length − 1)` (a 17.6 s ride) and rides `TipBack` to `rival-park`. `RivalsDelayedUntil` is that park tick (the HUD's "away until").
- **Reference (c034):** a tip landing at 3:54.0 → the car leaves, off the map 4:11.6, re-enters 7:26.4, parks 7:44.0 (the brief's 7:44).
- **L and the three stay where they are**; the tip takes the car and W only (−1 in the fight if the car is still away at the start, §7).
- A second tip after the car is back is allowed (it resets the away time); the −1 stays "an earlier tip" for Motive's count.

### F4 · The driver warns the lookout off

- **Meaning:** the turned driver's word, "the pickup's watched" (GDD §07), is a visit: **L walks over to the driver** (the driver does not leave his post: a unit's door is one person's post, §04 P-16), speaks `driver-warns-s` (10 s [S]), and leaves the map.
- **Trigger:** the driver is turned (the Pay or Help lands, tick U) and L is Posted. L leaves his post at `s0 = max(U + T(driver-warn-lag-s), tLpost)` (lag 3 s [S]; never before he is posted).
- **The visit is made only if the word is over by the driver's last chance:** `s0 + walk(rival-lookout → rival-lookout-word) + T(driver-warns-s) <= T(driverLastS)` (340 [B], 5:40). With the walk of 17.0 m (10.7 s) the latest turn that still warns him is **5:16.3**. A driver turned later makes no visit: L stays, and the fight has one more rival.
- **Sequence:** L `Walking` (grid, 160 mm a tick) to `rival-lookout-word` → both `talk` for 10 s (the driver's pose is the host's; a person at his post) → L `Walking` to `rival-lookout-exit` (55.0 m, 34.4 s) → `Gone`, for good (off the roster: not in the fight, §7).
- **It is cancelled** (L walks back to his post and resumes his loop; not retried) if before the word ends: the driver is down or no longer turned, L is down, the crew is drawn, or the final fight has started.
- **Reference (c034, turned 1:52.9):** L is posted 3:18.8, leaves at once, is at the driver 3:29.5, word over 3:39.5, gone about 4:13.9.
- W and the car are untouched.

### F5 · Gunfire draws them (any traceable gunfire, `DrawRivals`) [B]

The 16 s [B] (`rivals-drawn-s`) and "they make for the Office front" stay; now they come as a crew. `d:Office approach` is where they stand; the car goes to `rival-front-stop` (derived, 5.9 m from it).

| the crew at the shot | what happens |
|---|---|
| `Off` (before 2:35.3) | the car enters at `shot + T(16)` (the existing `RivalAt = min(RivalAt, now + 16 s)`), rides the direct route to `rival-front-stop` (90.2 m, 18.1 s), stops `Front` → `Stopped`; the riders step out `rivals-out-s` (5 s), then every 2 s in id order, W not (he stays aboard); they walk to `rival-front-offsets` (6.0 m) |
| `Entering` / `Dropping` / `Rounding` / `Parked` | **immediately** (no 16 s: they are already here, as `DrawRivals` does when `RivalsArrived`): every member on foot walks the grid from where he stands to his `rival-front-offsets` place; every member still aboard stays aboard; the car re-plans from where it is to `rival-front-stop` (from `Parked` 41.2 m, 8.3 s) and stops; the aboard step out as above |
| `Away` | the existing rule: they come when the tip ends; the car re-enters at the tip's end and rides to `rival-front-stop` instead of re-parking |
| `Tail` / `Stopped` at `rival-tail` | as `Parked`: W (out or aboard) re-plans to the front |

On arrival a drawn crew faces back the way it came (`Back(path)` [B]); from there the existing machine (`GameAi`, the intent ladder's "press for the Vale holder") takes them. Gunfire is loud: the final fight is the police branch; the crew at the front is as built.

### F6 · The cell has Vale (the final fight, safe): the late-arrival rule

`Vale == WithEnforcer` at tick H starts the safe fight. The car's timetable never changes; **what changes is where its stops are and who gets out**: each stand has a **rival stop** (`rival-drop` for the square stand, `rival-tail` for `service-drive`). A member who is Posted at his post is already where he should be (his place becomes his lurk); the rest come by the cases below.

| car at H | the car | the members still to come |
|---|---|---|
| `Off`, `Entering` | enters (or keeps riding) as planned and **stops at the stand's rival stop instead of continuing**: `Stopped` for good | all five are aboard: step out in id order at `stop + 2, 4, 6, 8, 10 s` (`rivals-step-s`); rival-1 to rival-3 to the first three spots, rival-4 and rival-5 to the 4th and 5th by role |
| `Dropping` | does not leave | the three already out are as above; rival-4 and rival-5 step out in their slots (`stop + 8, 10 s`, never earlier than H) |
| `Rounding` | re-plans from where it is to the rival stop (the grid route; no U-turn rule needed), `ToStand` | rival-4, rival-5 step out `rivals-out-s` (5 s) and `+2 s` after it stops |
| `Parked` | rival-4 (L) **walks to the car (6.0 m, 3.8 s) and boards (2 s)**; the car then rides to the rival stop (square: 77.6 m, 15.6 s) | rival-4 out at `stop + 5 s`, rival-5 at `+7 s` (rival-5 never left the car) |
| `Tail`, `Stopped` at the tail, `Away`, `TipOut`, `TipBack` | `Tail`/`Stopped`: rides to the rival stop; `Away`/`TipOut`: carries on its tip (W is the −1; it **re-enters and joins** when it parks again, §7) | **L never boards unless the car is `Parked` at `rival-park` at H**: he walks on foot from his post to the stand's 4th spot (square: 111.2 m, 69.6 s; `service-drive`: about 28 m, 17 s) |
| `Stopped` (W out at the yard door) | stays | W is **already a member on foot at a place**: his place is his lurk; he does not walk to the 5th spot (this is `RivalReachesVale`'s yard case, K9) |

- **A-pressure** (Vale at 2:41.6): the car is `Entering` (arrives 2:46.4). It stops at `rival-drop`; the five step out 2:48.4, 2:50.4, 2:52.4, 2:54.4, 2:56.4; walks (reference): rival-4 to `ambush-4` 43.7 m, 27.4 s (posted 3:21.8), rival-5 to `ambush-5` 20.8 m, 13.1 s (posted 3:09.5); rival-3 at `ambush-2` 3:26.5. All five are posted by 3:27.
- **Not "present" any more means "on the map".** A member not yet on the map has a `Hostile` with `Present` false and a pose of the car's (or none while `Off`); he becomes `Present` at his step-out tick.

### F7 · What is **not** a follow

Nothing else moves a member. They do not follow a cell *person* (only the car), they do not leave a post to look at a noise, and a member shot or taken down does not change the others' routine; Motive owns the Trail and the break count for those (§11).

## 7 · The final fight's bodies (K9)

- **The hostiles are the crew.** `StartFight` (safe) builds one `Hostile` per member that is on the roster: not `Gone` (the warned lookout), not `Down`, not `Away` (a tipped wheelman). It reuses the member's gun and id; **no fresh spawns**.
- **Where each begins:** a member Posted or Standing begins at his place (a posted one lurks there with `Gun.St = "routine"`, still doing his beats; the fight's start shows nothing); a member `Walking` keeps his route and then lurks at his spot; a member aboard or on his way by car is in the car's pose and gets his legs from §6 F6. **`Hostile.Run` becomes a list of legs** (K9): a car leg (the car's own ride, the rider is where the car is) then a foot leg. `Present` means "on the map and visible", true for every member who has stepped out and for the car's riders while the car is on the map; before the spring a lurking hostile is not hidden.
- **The count is a consequence, not a subtraction:** `Hostiles.Count == fight-safe-hostiles (5) - [the lookout is Gone] - [the wheelman is Away at H]` (the shot-down members are fewer). The old arithmetic (`SafeMinus` for a turned driver, for an earlier tip) becomes **a test** Keel asserts in `StartFight`, and Motive's rule should read "the lookout has been warned off" where it now reads "the driver is turned" (a driver turned after 5:16 does not warn, §6 F4). Route B with the tip and an early turn fights 3 (rival-1 to rival-3), as before.
- **The wheelman returns into a running fight:** a tip that ends while the fight is on and not over (`FightOverTick == INF`): at the tick the car would park again it instead rides to the stand's rival stop, W steps out `rivals-out-s` later and joins as a new `Hostile` (`Present`, `routine` then `advance`), and the fight cannot end before he is down or gone. A slower cell meets the wheelman coming back (brief 059 §1.6). *Decision [S]:* if the count assertion makes this awkward Keel may keep him away for the whole fight and tell Motive; the brief's wording prefers the return.
- **The spring** is unchanged [B/S]: the Enforcer with Vale, not inside, within `fight-ambush-m` (20 m) of the stand's car. Every hostile alerts together; the travellers keep their legs and "come in on their path, as police do".
- **The blocker** is unchanged (the on-foot ambusher with the shortest grid walk from where he stands to the lane post, **ties the lower id**); a rider still in the car at the spring is not a candidate until he is out.
- **Break** is unchanged (more than half the whole crew of five down, counting members not yet present, pulse-fight-ai §3.1); a member that breaks walks to his car as built and is gone within `rival-gone-cm` of it.
- The posted three at their spots **hold only if the spot does** (MC-11, outside 8 m and the exit lane): unchanged.

## 8 · What a Watch shows (for Glyph and the Watch rule, brief 054)

`RivalNext(id) -> (kind, tick | -1, place)`, a host read, never hashed. One line each; Glyph draws the pip and the ring.

| who | in state | next move shown |
|---|---|---|
| car | `Off` | nothing (nobody sees it before 2:35.3) |
| car | `Entering` / `Rounding` / `Tail` / `TipOut` / `TipBack` | `stops` at its next stop, with the arrival tick |
| car | `Dropping` | `leaves`, tick |
| car | `Parked`, W aboard, not tipped, Vale's car in the bay | `follows Vale's car` at `tValeIn + 2 s` |
| car | `Parked`, Vale's car already away | `follows` now |
| car | `Away` | `back` at the park tick |
| rival-1 to rival-3 | Posted | the **spring ring** (`fight-ambush-m` 20 m) round the cell's car; `walks after your car` if it moves |
| rival-4 | Posted | `leaves after the word` at its tick if the visit is scheduled (the driver is turned and the turn was in time); else nothing |
| rival-5 | aboard | nothing |
| any | Posted | the cone, facing and **the beat's yaw windows** (a Scout who has watched sees the cone, §05) |

A **Listen on the driver** adds one fact: "that car's tailed us all week" (the F2 rule), brief 059 §2.2.

## 9 · What Keel builds (K7 to K11, reconciled)

- **State to hash (new):** the car's state, the tick it entered it, its route id and leg index, `tEntry`, the park tick, `RivalsDelayedUntil`; per member: mode, the tick the mode began, his spot index and the stand it belongs to, his posted tick (the beat's origin), `Gone`, `Down`, health; `lookoutWarned`; the cell's car's destination stand; the visit's start tick. Beat names are not hashed.
- **Replace `RivalsPose()`** with `RivalPoses()` (five `(pos, facing, mode)`, plus the car's `(pos, heading, state)`). Callers (current, from a grep): `Hash.cs:111`, `GameCombat.cs:123,146` (a target's place), `GameOrders.cs:766,802` (Mislead and Take: the Mislead reads the **car's** pose; the Take's rivals check reads the members'), `GameIntent.cs:79,164` (cones: one per member on foot, none for riders), `GameSight.cs:154` (`rivals` watcher: becomes `rival-1..5`), `GameKite.cs:345` (the lab kite), `FailCause.cs:164` (who/where), and the lab scenarios. `RivalsGun` splits into five guns; `RivalsPresent` is per member.
- **`RivalAt`** stays the park tick (190 s) for the timetable; a crew drawn before 2:35.3 uses it as the entry tick (`min(RivalAt, now + 16 s)`) as today. **`RivalsArrived`** means "the car has entered". `rivalCarParked` is `state == Parked`.
- **New notes (codes; Fable writes the words):** `rivals.enter`, `rivals.drop`, `rivals.park` (existing code), `rivals.post`, `rivals.follow-car` (the three walk after your car), `rivals.tail` (the car follows Vale's), `rivals.tip-leaves`, `rivals.back`, `rivals.warned` (the lookout is told and leaves), `rivals.late` (the fight starts and the car comes to the stand).
- **Site (K10):** `rivals { car { entry, drop, park, facing }, lookout, tail, front, exit, vale-door }`, `stands[].rivalStop`, `dropS` 8, `outS` 5, `stepS` 2, `followS` 2. SX rules: every point on its grid (drive cell for a car point, a free walk cell for a foot point); `tEntry > 0`; the spots of a stand are on free cells; the rival route's ends equal the points.
- **Values (K11):** the eight in `rival-routine.values.json` (`rivals-drop-s` 8, `rivals-out-s` 5, `rivals-step-s` 2, `rivals-follow-s` 2, `rivals-posted` 3, `driver-warns-s` 10, `driver-warn-lag-s` 3, `lookout-pace-cm` 200, all [S]) plus the beats. `vale-in-s` 3 is Motive's. Reused, unchanged: `rivals-speed-cm-s` 160 [B], `rivals-drawn-s` 16 [B], `false-tip-s` 230 [B], `mislead-s` 44 [B], `act-reach-m` 6 [B], `fight-ambush-m` 20 [S], `fight-safe-hostiles` 5 [S] (now a test).
- **Cover (mine):** `tools/cover` already reads `site.rivals.park` and a `present` window `[rivalsArrive, over]` for `car/rivals`, so `bun run cover` follows the re-site by itself; the engine drops the car's points whenever the car is not `Parked` or `Stopped` (the window is the superset).
- **Scenarios** (each replays bit for bit; the brief's list plus these): ① no order: the poses at 2:35.3, 2:46.4, 2:54.4, 3:10.0, 3:18.8 and the loop at three sample ticks per member · ② Drive to `service-drive` at 1:00: the three re-plan at +2 s, post at the new stand · ③ Vale at 2:41.6 (A-pressure): five out at +2 to +10 s, five hostiles · ④ Vale at 3:00 (`Rounding`) and at 3:30 (`Parked`): L boards, car to the stop · ⑤ no tip: W at the door 6:38.9, `rivals-reach-vale` · ⑥ tip at 3:54.0, turn at 1:52.9: L gone by about 4:14, car parks 7:44.0, fight of 3; the same with a slow fight so the car returns while it runs · ⑦ turn at 5:20 (after 5:16.3): L stays, fight of 4 · ⑧ shot before 2:35 and after 3:10 · ⑨ two members stepping out in one tick, and two triggers in one tick, in id order · ⑩ a Down member on the street (shot): the fight has four, the routine of the rest is unchanged.

## 10 · Existing machine that does not change

The intent ladder (`ai.intent.rivals`), the target priority, the aim and react times, the blocker pick, the break rule, the 30-combatant cap, "rivals don't report", the spring at 20 m, the 16 s draw, `false-tip-s`, the hold (8 m and the exit lane), `rival-gone-cm`, Standing by opponent (`fight.public`). This spec places and moves the bodies; it adds no firing rule.

## 11 · Open issues, and who

1. **Motive:** the safe count is now bodies (§7): move "turned driver −1" to "lookout warned −1" in the rule text; confirm the wheelman returns into a running fight; rule what a shot or knocked-out rival costs on the street (Trail, Standing, found bodies: rivals don't report, but a body may be found by the cell's own contacts) and whether the break count is five.
2. **Motive / Fable / Glyph:** the warn has a **cliff**: a turn landing after 5:16.3 earns no warning (§6 F4). The driver's price line should show the warning only while it can still happen. Alternatively widen the deadline to 5:50 (Vale's exit): then the latest turn is 5:26.
3. **Waymark / Keel:** the tail reaches Vale's door at 6:38.9 (§6 F2), 9 s after the brief's "about 6:30"; the mouth stop would give 6:34.9 but parks the car across the westbound lane. Pick before the re-record.
4. **Vantage:** the new points in §1 (and the `service-drive` stand's five ambush spots and four police stands under MC-11). `rival-3`'s post at the square (`ambush-2`) is 18.6 m from the car (beyond a rival's 18 m cone): intentional, noted for Vantage, no action unless MC-11 wants it closer.
5. **Splice:** the `rivals` and `getaway-service` routes and their SX rules; `getaway.stands[]` with `rivalStop`; re-run `bun run cover` after Vantage's re-site (see below).
6. **Keel:** cars do not collide with cars: the rival car's way to the park and the tip's ride use Rocket Lane and Apron Road, which are also the getaway exit lane and Vale's car's way; a cell that leaves the square between 2:54 and 3:10 shares the road with the rival car. No rule reads it; the host draws two cars on one lane. Datum/Waymark: the driver's day in `handover.people.json` ends at 7:30 (`stops` to 450 s) while the turned driver now waits to 8:30; the engine places him itself, but the people checker's view should agree.
7. **Forma / Latch:** ten new poses (§5), the car's rider heads in the window for W (optional), the approach line (Glyph), five figures instead of three on one point (the current host draws `rivals/0..2` at one point plus 0.9 m offsets).
8. **Not decided here (Motive's questions 3 to 5 in brief 059):** the Trail and Standing of shooting a posted rival; the floor of 3 in the fight; the loud branch's second entry.

## 12 · Evidence

`python3 -I briefs/059/pulse/check.py` → every row of §1 resolves; the new points are on the right grids; the three outside the pickup camera's cone; the lookout's cone holds the bay, the driver and the door; the default timetable (2:35.3 · 2:46.4 · 2:54.4 · 3:10.0); the assignment rival-1 → `ambush-3`, rival-2 → `ambush-1`, rival-3 → `ambush-2`; the beats (no beat over 12 s, loops 34 to 42 s, rival-1 and rival-2 never both looking away); the warn trip; the tail; the tip's legs against `false-tip-s`; the drawn crew's front stop. See the last lines of its output for the count. `bun run test` shows three failures in `tools/cover`: not caused by this spec; the cover file's input hashes are stale (`from.block` and `from.plan`), see Aster's note.
