# Brief 067 · Motive: pace and punishment (B), and Keel's four 0.33.1 items

Aster's ask: item B of Sky's 0.2.122 notes ("Players move pretty slowly and I think maybe the game is too punishing. Running shouldn't be that badly punished") and four open items from Keel's engine 0.33.1. Values for Keel: **`briefs/067/motive-values.json`** (rulebook ids, old → new, every one [S]). Engine 0.33.1 at d4361806, played in a scratch copy with switches only (`briefs/067/motive/engine-probe.patch`, `mkengine.sh`). With every switch unset the probe gives Keel's 0.33.1 numbers.

## 0 · What to land
**The walk: 1.8 m/s, with 1.6 as the fallback.**
- At 1.6 every winning plan still wins as composed today.
- At 1.8 five fight plans need small re-compositions, which Waymark does in c038 anyway. They are here, and they win at 1.4 too.
- I pick 1.8 because Sky felt the pace: +14 % is hard to notice, +29 % is not.


| id | old | new | why, in one line |
|---|---|---|---|
| `op-speed-cm-s` (cell.speed) | 140 | **180** | The cell walks briskly (1.8 m/s). Legs are 22 % shorter. |
| `people-walk-cm-s` (new) | — | **140** | The city keeps its 1.4 m/s: the summoned guard, reporters, the crowd, the rounds bake. |
| `op-run-speed-cm-s` | 280 | **360** | Still twice the walk. |
| `carry-untrained-cm-s` | 70 | **90** | Still half the walk. |
| `run-alert` (cell.runAlert) | 1 | **retired** | A run is seen and suspicious, never an Alert on its own (new rule `attention.run`). |
| `mod-running` | 250 | **375** | Firing on the run: three-eighths, between the walk (500) and running away (125). |

Rules:
- M067-B1: the walk split.
- M067-B2: `attention.run`.
- Keel's items:
  - K1: "a Drive can't be cancelled" is confirmed.
  - K2: 50 cm and 1 m are accepted.
  - K3: three rules close BAL-YARD-AT-WILL's gap: hiding in a held car loses, the cell down fails, Go needs a standing driver.
  - K4: 12 against 14 is one down, not two readings; §06's text lacks the +2 T a shooting.
- The patrol move (§3): measured; route B now needs the Reroute.
- The GDD text is in §4, for Aster.

## 1 · B: pace and punishment

### 1.1 What Sky's play shows
- **He stopped running.** I replayed Sky's 0.2.122 session on its own engine (0.31.0); it matches exactly (`results/sky0122-replay-0.31.txt`).
  - It has 316 orders and **not one run**: 0 of 30 moves, 0 of 266 driven samples.
  - In his 56 sessions of 15–23 October he ran 266 of 1,338 moves (19.9 %; Suture's `briefs/060/qa/human-measures.json`).
  - Since engine 0.22.0 a run seen by any security watcher is Alert 1 on the spot. That is the likely reason he walked, and the walk is 1.4 m/s.
- **The walk cost him the mission's clock.** Turning Inez took him to 6:47, past Vale's exit at 5:50:
  - Watch at 0:05; Listen 2:06–2:48 (the Scout walked in late);
  - Help at 3:22 ("not yet"); Ask refused at 5:51; the second Help turned her at 6:47.
- **What punished him was priced, and he saw the price:**
  - the Disable at 1:09 (Alert 1 at 1:17, and the patrol left its sweep for the alarm);
  - two loiter reports by that patrol: the Tech watching the camera in its sight (2:06), then the Enforcer (2:44);
  - a shot at the rivals at 5 m in front of civilians (6:52: Alert 3, lockdown);
  - the Take at Vale's car under the pickup camera and in front of the driver (7:04);
  - the police downing the Enforcer (8:10).
- **The one rule that changed his play without showing in the run is the run's Alert.**

### 1.2 The pace (M067-B1)
- **The cell walks at 1.8 m/s and runs at 3.6 m/s; the city keeps 1.4 m/s.**
  - 1.8 m/s is a brisk walk (6.5 km/h): still a walk on screen, faster than the rivals (1.6), slower than the response unit's run (2.2).
  - The run stays twice the walk, so the chip still reads half the time. It is faster than a fleeing civilian (2.6), the posted rivals chasing a Driven car (2.8) and the police (2.2). The police's threat stays cones, guns and the seal, never a foot race.
  - Untrained carriers keep half the walk (0.9 m/s). The Enforcer carries Vale or a body at the walk.
- **The split is needed.** Engine 0.33.1 reads `cell.speed` for everyone it walks itself:
  - the guard Inez calls (so the window would shrink from 58 s each way to 45 s);
  - reporters and tellers walking to a phone (so reports would land sooner);
  - a witness walking to a body, and the crowd walking back from shelter;
  - the rounds bake checks its pace against it (so the rounds file would need a rebake).
  - `people-walk-cm-s` (140) takes all of these. `routines.rules.json` `rounds.pace.of` points at it. Nothing the city does changes.
- **Measured: the windows hold.**
  - Inez's call still sends the guard for 58 s, 60 s and 58 s (2:51 → back by about 5:47) at every pace.
  - The loop (1:45–3:45) still covers route A's Enter: 2:52.9–3:30.9 at 1.8, 2:53.4–3:31.4 at 1.4.
  - The service lane to the getaway bay (65.0 m) takes 46.4 s at 1.4 and 36.1 s at 1.8, so §07's "about 46 s" becomes "about 36 s".

### 1.3 Running: seen and suspicious, never an Alert by itself (M067-B2)
- **The rule.** Only a watcher that keeps an attention clock counts a run: a live camera, the door guard, the patrol, the response's officers and robots.
  - At the first tick a run is in its sight, its clock on that runner jumps to the noticed mark, and its cue shows "noticed".
  - While the run stays in its sight, the clock adds 1 s a second, as standing still does.
  - Wary and report are attention's own marks. A report lands as a loiterer's: a found fault, A ≥ 1, no Trail, a sighting.
  - A run in sight therefore costs:

    | Heat band | wary after | reported after |
    |---|---|---|
    | unknown (Heat 0) | 12 s | 37 s |
    | infamous | 9 s | 15 s |

  - A single pass costs only the "noticed" cue.
- **What stays.**
  - Shift guards on the street (no clock) and the door guard still remember the runner's face, place and time: a sighting, no T, no Alert.
  - A guard who saw a runner still doesn't fight at your side.
  - From Alert 2, civilians who see a run remember the face. This is Sky's 054 call; I keep it (§1.6).
  - Security stops counting a run once a fight is on, the run is loud, the op is under fire, or Alert is 2 or more (as built).
- **Why this shape.**
  - It shows on the street, not in text: the first run in a watcher's view lights its "noticed" cue. The player learns that running is seen without an Alert to learn it from.
  - Its cost grows with time in sight and with Heat, the way loitering's does.
- **Measured: `results/run-camera.txt`** (the Scout runs 16 legs across the entrance camera's view).

  | rule | the run first seen | noticed | wary | reported | landed |
  |---|---|---|---|---|---|
  | today | 0:20.9, Alert 1 at once | — | — | — | — |
  | proposed | 0:16.3, no Alert | 0:16.3 | 0:28.2 | 0:53.2 | 0:57.6 (Alert 1) |

- **On the route set:**
  - Route A's Alert 1 came only from runs: the shift guard Astrid Tanaka saw the Scout at 0:02.8 and the Fixer at 0:03.7, and the door guard saw the Enforcer at 2:52.6. With the rule, route A ends at Alert 0.
  - No other reference route moves (`results/ens-r0c2.tsv`).
  - No quiet route gains a report (MC-12 13/13 at every candidate).

### 1.4 The running modifier
`mod-running` 250 → 375. A run toward or across now fires at three-eighths:
- Untrained at the effective ring: 140 → 211 ‰ (about 1 in 5 instead of 1 in 7). Trained: 187 → 281 ‰.
- The ladder stays stand 1000 > walk 500 > run 375 > run away 125, so firing on the run is allowed and never the best way to fight.
- `mod-running-away` 125 and `mod-target-running` 850 stay.
- Measured on the fight lab's today matrix (43 rows, `results/fight-lab-today.txt`):
  - 375 and 500 move no row's outcome. The only Trail moves: circle-kite 30 → 28, charge 49 → 53 at 500.
  - No loud row and no kite becomes a win, at the old pace or the new.

### 1.5 Measured on the route set
Base: engine 0.33.1, the c037 plans with my 059 focus-fire re-compositions under the reference ids (Waymark's c038 list), 059's `routes.json`. This is the set Waymark re-composes next. On 0.33.1 it holds every check but MC-0 (the manifest's expectations) and MC-5 (route-g-fast loses) (`results/cand-base.txt`). Each candidate re-composes every plan on its own rules.
- **Mission check** (`results/cand-<tag>.txt`; table: `python3 -I briefs/067/motive/table.py base r0c2 w180`):

  | check | base (1.4 m/s) | 1.8 m/s with the run rule, plans as composed today (`cand-w180`) |
  |---|---|---|
  | MC-1 | 3 quiet routes (A, B, G) | A, G (route B's yard plan, below) |
  | MC-2, MC-10 | hold: loud 27 + 3 T, A 2, Standing −1 against quiet 3–7 T, A ≤ 1, Standing ≥ 2 | hold, same numbers |
  | MC-6 | route-g 4:11.1 against loud 5:18.5 (67.4 s faster) | route-g-fast 3:00 against loud 5:02.6 (122.6 s faster) |
  | MC-12 | 13/13 quiet scripts unreported; careless-scout-idles-pickup reported at 2:33.8 | 13/13; reported at 2:32.2 |
  | MC-7 | holds | careless-g-disable now gets away before the 4:00 check-in mark, so it ends at Alert 1, not 2 (Waymark: §1.5, last bullet) |

- **Ensembles.** The final fights turn on single ticks, so one run per value says little. Each plan is re-composed in two ways (`results/ens-*.tsv`; table: `python3 -I briefs/067/motive/enstable.py <tags>`):
  - with the whole cell 0–3 s late (7 delays, `ens-all.sh`);
  - with each operative its own delay, 0–2 s (8 samples, `ens-op.sh`).
  - Wins of n (first win's end). Every candidate includes the run rule; `ra-h3`, `rad-h3`, `rb-st`, `rbb-st` and `rgkb-h3` are this pass's re-compositions (`briefs/067/motive/plans/`).

  | plan | 0–3 s late: 1.4 | 1.6 | 1.8 | 2.0 | each op 0–2 s: 1.4 | 1.6 | 1.8 |
  |---|---|---|---|---|---|---|---|
  | route-a | 7/7 (4:52.8) | 7/7 (4:33.9) | 0/7 | 0/7 | 8/8 (4:52.8) | 8/8 (4:33.9) | 0/8 |
  | **ra-h3** (hold at 20.0, −65.0) | – | – | – | – | 8/8 (4:50.5) | 8/8 (4:34.2) | 8/8 (4:56.8) |
  | route-a-disable | 7/7 (4:52.8) | 7/7 (4:33.9) | 0/7 | 0/7 | 8/8 (4:52.8) | 8/8 (4:33.9) | 0/8 |
  | **rad-h3** | – | – | – | – | 8/8 (4:50.5) | 8/8 (4:34.2) | 8/8 (4:56.8) |
  | route-a-board | 7/7 (4:27.5) | 7/7 (4:20.7) | 7/7 (4:15.5) | 7/7 (4:11.3) | 8/8 (4:27.5) | 8/8 (4:20.7) | 8/8 (4:15.5) |
  | route-a-pressure | 0/7 | 0/7 | 1/7 (3:37.2) | 5/7 (3:21.6) | 0/8 | 0/8 | 0/8 |
  | route-a-pressure-board | 0/7 | 0/7 | 3/7 (3:10.3) | 7/7 (3:07.7) | 0/8 | 0/8 | 6/8 (3:10.2) |
  | route-b | 7/7 (7:32.2) | 7/7 (7:32.2) | 0/7 | 0/7 | 8/8 (7:32.2) | 8/8 (7:32.2) | 0/8 |
  | **rb-st** (the Scout's lock 0.5 s later) | – | – | – | – | 8/8 (7:32.2) | 8/8 (7:32.2) | 8/8 (7:32.2) |
  | route-b-board | 7/7 (7:36.1) | 7/7 (7:35.7) | 0/7 | 0/7 | 8/8 (7:36.1) | 8/8 (7:35.7) | 0/8 |
  | **rbb-st** | – | – | – | – | 8/8 (7:36.1) | 8/8 (7:35.7) | 8/8 (7:35.4) |
  | route-g | 7/7 (4:11.1) | 7/7 (4:04.6) | 7/7 (3:59.6) | 7/7 (3:58.6) | 8/8 (4:11.1) | 8/8 (4:04.6) | 8/8 (3:59.6) |
  | route-g-board | 7/7 (4:11.1) | 7/7 (4:04.6) | 7/7 (3:59.6) | 7/7 (3:55.6) | 8/8 (4:11.1) | 8/8 (4:04.6) | 8/8 (3:59.6) |
  | route-g-fast | 6/7 (3:11.0) | 7/7 (3:02.0) | 7/7 (3:00.0) | 7/7 (2:59.5) | 7/8 (3:10.7) | 8/8 (3:02.0) | 8/8 (3:00.0) |
  | route-g-fast-board | 6/7 (3:13.1) | 7/7 (3:03.2) | 7/7 (3:00.2) | 7/7 (2:59.6) | 7/8 (3:13.0) | 8/8 (3:03.2) | 8/8 (3:00.2) |
  | route-g-kill | 7/7 (5:50.5) | 7/7 (5:56.3) | 0/7 | 0/7 | 8/8 (5:50.5) | 8/8 (5:56.3) | 0/8 |
  | rgk-h3 | – | – | – | – | 8/8 (6:07.9) | 8/8 (6:01.3) | 0/8 |
  | **rgkb-h3** (board and go, hold at 20.0, −65.0) | – | – | – | – | 0/8 | – | 8/8 (5:26.2) |
  | loud | 7/7 (5:18.5) | 7/7 (5:09.4) | 7/7 (5:02.6) | 7/7 (4:57.0) | 8/8 (5:18.5) | 8/8 (5:09.4) | 8/8 (5:02.6) |

- **What moves with the pace, and why:**
  - **At 1.6 every winning plan still wins, unchanged.** At 1.8 five plans need the small re-compositions above. All of them win at 1.4 too, so they are safe for Waymark either way.
  - **Board and go gets easier.** The Enforcer walks Vale from the 20 m spring in 11.1 s instead of 14.3 s.
    - route-a-pressure-board goes from 0/8 to 6/8.
    - route-g-kill-board goes from 0/8 to 8/8 (with the hold below).
  - **Fight it out with the Enforcer holding Vale in the open loses at 1.8** (route-a, route-a-disable, route-g-kill):
    - The cell now springs the ambush about 9 s earlier: 4:01.4 against 4:10.6.
    - The blocker is then picked from the crew on foot (rival-2), not from the late car's man (rival-4), who stepped out at 3:58.
    - rival-4 then walks past the hold point (2400, −6000), which is 15.9 m from ambush-1, and shoots the Enforcer down.
    - **Holding at (2000, −6500)** fixes it: 20.4 m from the car and 20.8 m or more from every ambush spot, beyond the pistol's 16 m, so the cell's fire springs the ambush. ra-h3 and rad-h3 win 8/8 at every pace.
    - route-g-kill with that hold (rgk-h3) still stalls at 1.8. Two rivals answer the Fixer by stepping out of his line behind the café's wall arc. With 2 of 4 down the crew never breaks, so fight.over never comes. The car is free from 5:17.3; the player's move is to board and go (rgkb-h3, 8/8).
  - **Route B at the yard loses at 1.8 on a tie:**
    - The Scout no longer needs a walk-in to her line, so her first shot lands on the same tick as the Fixer's (7:25.0).
    - rival-1 answers the Fixer, who already carries rival-3's fire, and he goes down.
    - The plan's Go waits until nobody would be captured, so it never comes.
    - Staggering the Scout's lock 0.5 s after the Fixer's restores the split of fire: rb-st and rbb-st win 8/8 at every pace. The same stagger lets the two yard twins compose again (crns-st, csip-st).
  - **careless-g-disable:**
    - At 1.8 the cell is gone at 3:59.6, 0.4 s before the guard's 4:00 check-in mark.
    - The Disable is then the only incident, so the twin ends at Alert 1 (7 T against route G's 6). MC-7 fails on that one line.
    - GDD §07 says "the guard's missed mark becomes the second (Alert 2)". That holds only if the cell is still there at the mark.
    - Waymark should re-time the twin so the mark comes before Go, or accept it: at the new pace the Disable on route G is "an honest alternative" when you beat the next mark, as it already is on route A.
- **The mission check with those re-compositions** under the reference ids, at all six values (`results/cand-final-w180.txt`; at 1.4: `cand-final-base.txt`):
  - **Holds:**
    - MC-1: 3 quiet routes (A, B, G).
    - MC-2: loud 27 + 3 T against quiet 3–7 T.
    - MC-5: route A 2:57, B 1:07 and G 4:17 before their closes. It fails at 1.4, where route-g-fast loses.
    - MC-6: route-g-fast 3:00 against loud 5:02.6, 122.6 s faster.
    - MC-10, MC-11 (11/11 wins free at Go), MC-12 (13/13 unreported; careless-scout-idles-pickup reported at 2:32.2).
  - **Fails:**
    - MC-7, on careless-g-disable alone.
    - MC-0: the manifest's expectations; route A's Alert is 0 now, and the c037 loud variants and route-a-pressure are still on Waymark's c038 list.
  - **Times, 1.4 → 1.8:**

    | route | 1.4 | 1.8 |
    |---|---|---|
    | route-a-board | 4:27.5 | 4:15.5 |
    | route-g | 4:11.1 | 3:59.6 |
    | route-g-fast | fails | 3:00 |
    | route-b | 7:32.2 | 7:32.2 (the yard is bound to Vale's schedule) |
    | loud | 5:18.5 | 5:02.6 |

  - `mod-running` 375 is in this run and moves nothing on the route set: its plans don't fire on the run.

### 1.6 Sky's 054 call: civilians remember a runner from Alert 2
**Kept.** It costs nothing in the mission (0 T, 0 Alert, 0 Standing). It feeds only the campaign's leads (the investigators, [L]), and at Alert 2 the search is already on.

### 1.7 Where the game punishes too hard: the read
| rule | the number | verdict |
|---|---|---|
| a run seen by security | Alert 1 at once | **change**: §1.3 |
| the cell's walk / run | 1.4 / 2.8 m/s | **change**: 1.8 / 3.6 |
| untrained carry | 0.7 m/s | **change**: 0.9 (half the walk) |
| firing on the run | ×250 | **change**: ×375 |
| a guard who saw a runner doesn't fight at your side | route A's guard sees the Enforcer's run at 2:52 at every pace | keep: it is "seen", and the run's one lasting price below Alert 2 |
| attention marks at Heat 0 | 8 / 20 / 45 s | keep: Sky's two reports came from Watching 45 s in the patrol's sight; a faster walk doesn't change a 20 s Watch; MC-12 holds |
| Watch reach | Scout 30, Tech 24, Fixer 12, Enforcer 3 m | keep: the split recce is the point (T12) |
| a Disable | +2 T, an incident, the response to the camera | keep: priced before commit |
| a missed check-in | Alert 1 | keep |
| entry seen by the guard, a live camera or the patrol | fail | keep: the core stealth line, shown on the ring ("who would see it") |
| gunfire / violence seen by a civilian | Alert 2 / Alert 3 and lockdown | keep: loud is the last resort (MC-10 at every pace) |
| Ask refused, a Fearful Inez needs Help twice | +$10,000 +1 T on later offers; 2 × 50 s | keep: the training teaches temperament |
| act times (Listen 42, Help/Pay 50, Enter 38 s) | route A secures Vale 2:56–2:57 before 5:50 | keep: faster acts would only add waiting for the windows |
| the Enforcer holding Vale is the rivals' target | 6 health; two rivals at the effective ring down him in about 8 s; the 20 m walk + Board is 14.3 + 4 s today, 11.1 + 4 s at 1.8 | the walk softens it now; the rest (Board carrying 4 s, cover for Vale's holder) belongs to the fight proposal (brief 067 M) |
| the seal, the loop's 2:00, Trail for takedowns, Pressure | as GDD | keep |

## 2 · Keel's four 0.33.1 items

### 2.1 "A Drive under way can't be cancelled" (M067-K1): confirmed [S]
- The car drives on to its stand (`drives-on`); a cancel is refused `car-driving`.
- Its riders are busy until it stands: any order is refused `agent-busy`, so nobody steps out of a moving car.
- Go during a Drive is taken and is a withdrawal. A Drive never carries Vale: `drive-fight` refuses it once the final fight has started.
- Why:
  - A stand is the only place the car's rules exist: its exit lane, ambush spots, police stands and cover. A car stopped on the road would be a stand with none of them (unknown = reject).
  - The order shows its stand and seconds before commit (28.4 s from the square to the service drive).
  - Turning back mid-drive is not worth its rules (readable > rich).
- For Glyph and Fable: the HUD shows the stand and the seconds left while it drives; the order line says it can't be stopped.

### 2.2 The car footprint, 50 cm and 1 m (M067-K2): accepted [S]
- **50 cm is one walk cell.**
  - The first free ring round a standing car is always "at it" (the 50–100 cm band is one full cell wide), so no side loses its Board.
  - A string-pulled walk still passes the body about a shoulder clear.
  - The building clearance (65 cm) would leave a 35 cm band, narrower than a cell.
- **1 m reach from the body** covers about 8.9 m² outside the clearance, against the 7 m² of the old 1.5 m circle. "Rivals reach Vale" is no easier.
- The 6 m guard ring of that rule stays measured from the car's point.
- GDD text in §4.

### 2.3 BAL-YARD-AT-WILL: the fight the cell can no longer fight (M067-K3) [S]
- **What happens** (`results/yard-at-will-0.33.1.txt`):
  - The Enforcer boards with Vale at 7:29.9 and is downed in the car at 7:30.0.
  - The Fixer falls at 7:31.5 and the Scout at 7:38.3.
  - The Tech sits in the car and is no foe: he has done nothing, and `ai.intent.final` is agent.
  - Go is refused at 7:36.1 (car-held: rival-3). The rivals hold their cover, and the run is idle to its end (None at 8:30.0).
- **The ruling:**
  - **(a) Hiding in a held car loses.**
    - In the safe fight, Vale is in the car and no op stands outside it (all down, or every standing one in the car).
    - Then the rivals up and not broken, the blocker excepted (he keeps his lane), walk to the car, firing as they walk.
    - The first within 1 m of its body takes Vale (rivals-reach-vale, as today).
    - An op standing outside again sends them back to cover. That last part is Pulse's to build; the probe doesn't model it.
  - **(b) The cell down.** Once every op is down (in the car or not), the mission fails at once: `cell-down`, the fail camera on the last to fall.
  - **(c) Go needs a driver.** Go is taken only with a standing op in the car. With only the downed aboard it is refused `car-no-driver`. Any standing op may give it, from anywhere, once one is aboard.
  - **No new timer.** The safe fight ends by the cell's acts, (a), (b), Withdraw, or (loud) the seal. §07's "up to 60 s" stays the fight's design length, not a clock: an open question for the fight proposal (M).
- **Why:**
  - §06 already says the Enforcer is "any op" once Vale is in, and that hiding in a held car loses. The engine had no way to make that loss happen.
  - (a) fires only when nobody outside can fight. The "6 m" support rule (rivals reach Vale only with no op within 6 m) stays passive, so board and go keeps its shape.
- **Measured on the probe** (`PROBE_K3=1`, `results/yard-at-will-k3.txt`):
  - The Scout falls at 7:38.3; rival-1 and rival-2 go for the car at 7:38.4; rival-1 reaches it at 7:46.5: rivals-reach-vale.
  - On every script of HEAD's route set and of the candidate set, (a)–(c) move no outcome.
  - route-a-board's final hash moves at its Go tick (2675): the three's boards land, the rivals' walk starts and Go is taken in the same tick. Reading (a) after the tick's orders keeps every hash.
- Scenarios for Keel are in the values file (BAL-YARD-AT-WILL, K3-CELL-DOWN, K3-DRIVER). Fable needs words for `cell-down`, `car-no-driver` and the rivals' walk.

### 2.4 Loud Trail, 12 under `agent` against 14 under `every` (M067-K4)
- **Both are §06's rule, applied the same way.** The 2 T is one down, not a different reading:
  - Under every, rival-1 falls at 4:18.3 (+2 T `down:rival-1`) before the Enforcer does.
  - Under agent the hostiles don't fire on the operatives who did nothing, so the fight runs differently and rival-1 is still up when the Enforcer falls at 4:26.4.
  - Every other ledger line is the same: loop 1, reroute 1, the guard 2 + 2 + 2, rival-1 2 + 2.
- **12 is the game's number,** because agent is the rulebook's rule (provisional; Sky "sounds right").
- The reference loud win (loud-x4) costs 27 T under both. The test's shape (Trail under agent ≤ under every) is right.
- **The real mismatch is the GDD text.** §06 says "+2 T per person hit (once), +2 T more if they go down (a dropped guard = 4 T)". The rulebook also charges `gunfire-trail` +2 T once per shooting (the first shot at each target, hit or miss: my fights addendum Q1, Sky-approved 2026-10-15). So the engine's dropped guard is 6 T. Wording in §4.

## 3 · The patrol move (item A, Vantage)
Vantage's points landed in the working tree's `mission.json` while I worked, and Splice's site export took them at 06:29.
- **How I measured it.** I took the move alone, on HEAD's site (`briefs/067/motive/mksite.py`):
  - the sweep from (61.25, −7.25) to (76.25, −7.25), facing the wing's east face;
  - the reroute point at (46.25, −9.25).
  - Then the full working-tree site, with J, K and L as well.
- **One thing to fix first (Splice).** The exported site still faces the sweep [0, 100], east, away from the Office.
  - `tools/sim/site.ts` hard-codes it, with the old src text.
  - Vantage's reasons need [0, −100] for the sweep and [−100, 0] for the reroute. I patched both into my copies.
- **The move alone, at the new values** (`results/cand-patrolA-w180.txt`, `cand-patrolA-w180rr.txt`):
  - **The front routes don't change.**
    - route-a-board 4:15.5, ra-h3 4:56.8, route-g 3:59.6, route-g-fast 3:00, all at the same T.
    - Their Reroute no longer buys anything at the door; it is still taken, and sends the robot round the corner.
  - **Route B now needs the Reroute.**
    - As composed, the Enforcer waits at 'Service lane' from 0:00 and is reported loitering at 1:53.4.
    - The Scout and the Fixer, posted in the yard at 5:55, are reported at 7:12.
    - In the yard fight the patrol fires on the drawn Scout. Route B ends 9 T at Alert 2, and route-b-board withdraws.
  - **The fix is a Reroute timed to cover the yard** (`plans/rb-rr`, `rbb-rr`, and the two twins `crns-rr`, `csip-rr`):
    - The Tech reroutes from the cabinet at 5:00. It lands at 5:45, and the patrol is away to 7:40.
    - A rerouted patrol never breaks to gunfire (`WakePatrol`), so it stays out of the fight 14–31 m away.
    - The Tech then runs to the car and drives it round. The Enforcer waits at the square until 5:50.
    - Route B: 7:32.2, 4 T (+1 for the Reroute), A 0. route-b-board: 7:35.4, 4 T.
  - **With those plans,** MC-1 (A, B, G), MC-2, MC-4, MC-5, MC-6, MC-8 to MC-12 hold.
  - **MC-3 fails: "entry seen by patrol" is no longer reached.** The robot never sees the public door now, sweeping or rerouted.
    - Waymark can reach it with an alarm that walks the patrol to the front (`patrol.alarm`).
    - Or Sky drops "patrol" from §07's "entry seen by guard, live camera or patrol".
  - So **the Reroute moves from "clears the door" to "clears the back way"**, as item A asks.
  - Fable: `patrol.rerouted` still says "leaves the entrance sweep".
- **The full working-tree site (A + J + K + L) at 1.8** (`results/cand-tree067-w180rr.txt`, `cand-tree067-base.txt`):
  - K's planter removal moves the square's fights.
  - route-a-board's whole cell is aboard at 4:15.5, but Go is refused through 5:16 (car-held: rival-4).
    - Answering the Tech's fire, rival-4 stepped "out of the line" to a spot inside the car's 8 m hold.
    - MC-11 checks only the posts and stands, not where a kite step lands.
  - With K3 (a) built, that run becomes rivals-reach-vale, which is what §06's "hiding in a held car loses" means.
  - Either way the square's board-and-go plans need re-composing on the final site: the cell downs rival-4 before boarding.
  - For Vantage and Pulse: a kite step should not end inside the getaway car's hold.
  - Route B (rr) and every route G plan win there; ra-h3 and route-g-kill-board need re-composing too.

## 4 · GDD wording (for Aster, v0.26.0)
- **§03 all ops**, replacing "Every op can run … at 2.8 m/s, twice the walk, and is noticed (§05). Any op can carry a body (the Enforcer at the walk, 1.4 m/s; the others at 0.7 m/s)":
  > Every op walks at 1.8 m/s, brisker than the city's 1.4, and can run (double-click, or G on a moving op) at 3.6 m/s, twice the walk; a run is seen and suspicious, never an Alert by itself (§05). Any op can carry a body (the Enforcer at the walk; the others at half of it) …

  and "nobody fires while running or carrying" becomes "nobody fires while carrying (a runner fires at the run's odds, §06)".
- **§05 running**, replacing it:
  > running [S]: below Alert 2 only security notice a runner: the cones of the guard, patrol, live cameras and response unit, and a security person's 3 m. A run is seen and suspicious, never an Alert by itself: a watcher that keeps a clock (attention) notices the runner at once, its clock jumping to the noticed mark, and counts each second of the run in its sight as standing still, so a long run in its sight makes it wary and a longer one is reported as a loiterer is (a found fault, A ≥ 1). Security staff who keep no clock remember the runner's face, place and time. From Alert 2 ordinary people who see a run in their cone remember where and when and the face: a lead, no T, no standing, no Alert. A guard who saw a runner no longer fights at your side. Security stops counting a run once a fight is on, the run is loud, the op is under fire, or Alert is 2 or more. The police never need to win a foot race: Vale can't run, so the getaway is decided at him (§06).
- **§05 attention**, the "each second" line, adding "· running in its sight → the clock jumps to the noticed mark, then +1 s".
- **§06 modifiers:**
  > shooter walking 500 · running toward or across 375 · running away (within 60° of straight away from the target) 125 · target running, past point blank, 850 (never with cover) · target still beside cover 500 · untrained (Scout, Fixer, Tech, rivals) 750
- **§06 orders,** fire (Sky 2026-10-21, tuning.md §8.2; the fire-on-the-move text brief 067 lists):
  > fire (until the target is down, or the final fight is over; holds aim 3 s if the line breaks; a fire order stops a walker, who stands and fires, and never stops a runner, who fires at the run's odds; a move given while a fire order stands keeps it, firing on the move; a fire order out of range walks into the weapon's effective ring on a clear line first, then fires)
- **§06 cost,** "+2 T per person hit (once), +2 T more if they go down (a dropped guard = 4 T)" becomes:
  > +2 T once per shooting (the first shot at each target, hit or miss), +2 T per person hit (once), +2 T more if they go down (a dropped guard = 6 T)

  §02's Trail list: "people shot at or hit by gunfire".
- **§06 getaway:**
  - Board: "by any op within 1 m of the car's body".
  - Drive: add "A Drive under way runs to its stand: it can't be stopped, and nobody steps out until the car stands."
  - Go: "an order on the car once a standing op is in it (a downed op can't drive)".
  - Rivals reach Vale:
    > a rival within 1 m of the car's body while Vale is in it, with no op standing within 6 m of the car, fails the mission; with nobody of the cell standing outside the car, the rivals walk to it and take Vale: hiding in a held car loses.
- **§07:**
  - Route A: "walks him to the car (about 36 s)".
  - Route G's Disable variant: "so the guard's next missed mark, if the cell is still there for it, becomes the second (Alert 2, a loud final fight)".
  - Fail: add "the cell down (every op down)".
- **§10 third person:** "The paces are the rulebook's (1.8 m/s walk, 3.6 m/s run)".

## 5 · Handoffs
- **Keel** (next owner).
  - **Inputs:** `motive-values.json`, `engine-probe.patch` (the probe's switches show where each change reads).
  - **Done-check:**
    - the six values land;
    - `attention.run` and the retired `cell.runAlert` are built; route A ends at Alert 0;
    - the walk split is built (PACE-SPLIT: the call still logs 58 s, reports land at 0.33.1's ticks, the rounds file loads unchanged);
    - K3 (a)–(c) are built (BAL-YARD-AT-WILL fails rivals-reach-vale near 7:46.5; K3-CELL-DOWN; K3-DRIVER);
    - the scenarios in the values file pass; `sim-check` replays identical.
  - **Then:** Waymark.
- **Pulse:** how the rivals walk to an unguarded car in K3 (a) (as the press for the Vale holder), and their return to cover when an op stands outside again.
- **Waymark.**
  - **Inputs:** the engine with the values, and `briefs/067/motive/plans/`.
  - **Done-check:** c038 re-composes on it, with MC-1 to MC-12 as in `results/cand-final-w180.txt`:
    - route-a and route-a-disable hold Vale at (2000, −6500) (`ra-h3`, `rad-h3`);
    - route B, route-b-board and their two yard twins stagger the Scout's first lock 0.5 s after the Fixer's (`rb-st`, `rbb-st`, `crns-st`, `csip-st`);
    - with the patrol in the yard (item A), the same four also reroute it from the cabinet at 5:00 and keep the Enforcer at the square until 5:50 (`rb-rr`, `rbb-rr`, `crns-rr`, `csip-rr`);
    - fail-entry-seen-patrol is re-composed with an alarm that walks the patrol to the front, or the fail line goes (Aster to Sky);
    - route-g-kill-board holds at (2000, −6500) (`rgkb-h3`); route-g-kill's fight-it-out stalls at 1.8 on two hidden rivals (car free from 5:17.3): keep it as the board variant or drop it;
    - careless-g-disable is re-timed so the guard's 4:00 mark comes before Go, or its 7 T against 6 at Alert 1 is accepted;
    - the manifest's expectations follow (route A at Alert 0).
  - **Then:** Keel records the run.
- **Fable:** words for the run's "noticed" (the cue already exists), `run.seen` without "a found fault", `cell-down`, `car-no-driver`, the rivals' walk to the car.
- **Glyph and Latch:**
  - read the paces from the engine, not 1.4 / 2.8: Glyph's "about to see" spec and Latch's third-person drive;
  - the cell's walk clip must play at 1.8 m/s with no foot slide (Forma if a brisker clip is needed);
  - Suture's `tools/qa/human-measure.ts` hard-codes 140 / 280.
- **Splice:** `tools/sim/site.ts` exports the sweep's facing as [0, 100] (east, away from the Office) and the reroute as [0, −100]. Vantage's points need [0, −100] and [−100, 0] (§3). Its src text still says "the forecourt".
- **Vantage and Pulse:** on the planter-free square, rival-4's step out of a shooter's line ended inside the getaway car's 8 m hold and held it after the cell boarded. MC-11 checks only posts and stands. A kite step should not end inside the hold (§3).
