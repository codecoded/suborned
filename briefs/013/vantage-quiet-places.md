# 013 C · Quiet places on the civic block (Vantage, 2026-10-16)

Brief: `briefs/013-takedowns.md` part C. Inputs: the plan and civic block (`contracts/world/`), the world export (`out/world/harbour-district.world.json`), the sim site and promoted r016 people (`sim/data/handover.site.json`, `handover.people.json`), the rulebook values (`contracts/rules/rulebook.json`), `kit.json`. Nothing in the world contracts, the kit or the GDD is edited; everything below marked *proposed* is for Aster to route.

Evidence: `bun briefs/013/vantage/quiet.ts` (exit 0, about 6 s; deterministic, two runs gave byte-identical `quiet.json`) writes `briefs/013/vantage/quiet.json` (every number below) and `briefs/013/vantage/quiet.png` (the sheet).

**Method.** This is a static survey that uses the engine's own perception. Camera cones are `G.InCone`. The guard's door cone uses `door-reach-m` 26 / `door-half-m` 19. The patrol sweep uses `patrol-depth-m` 24 with half-width 2 + 0.65 × depth, and moves as `Game.SweepPos` (A to B and back over `patrol-period-s` 100). The parked rivals are included. For people, the witness rule applies: anyone within `witness-range-m` 13 on a clear line, with occluders that span `eye-cm` 160. Walking uses the site grid's A* (as in `Grid.cs`), and the crowd follows its routines (as in `GameCrowd.RoutinePos`). Nothing is done to the watchers: no loop, no reroute, no lure. The crowd is not counted as blocking sight (the engine's `CrowdBlocks` may block it), so crowd exposure is an upper bound. Carry runs at walk pace `op-speed-cm-s` 1.4 m/s, plus the lift `carry-s` 3 s. Block frame metres: x runs south, y runs east.

## 1 · Survey

There are three kinds of candidate:
- **K** = kit pieces in the world.
- **R** = recesses and service lanes (open ground).
- **D** = service doors. Both are *proposed*: neither is in the block.

Column notes for the table:
- **Watchers** = security sight on the spot, before the chime.
- **Crowd** = seconds before the chime when a person sees the spot / the time the first one does / the quiet spans before the chime.
- **Play** = the share of a 1.75 m figure that the play camera sees past architecture / through a fade.
- **Carry** = metres and seconds including the lift, from the guard's door post (51.75, -30), the pickup (the driver, 78.1, -33.7) and the café door (Inez, -10.3, -29).

| id | place | at | watchers on the spot | crowd before chime | play | carry: guard · pickup · café |
|---|---|---|---|---|---|---|
| K1 | service cabinet 0, lane | 57.8, -43.8 | patrol 17% of sweep | 47 s, from 4:18 | 1 / 0 | 22 m 19 s · 24 m 20 s · 72 m 54 s |
| K2 | service cabinet 1, lane | 69.8, -43.8 | none | 44 s, from 4:40 | 1 / 0 | 34 m 27 s · 13 m 12 s · 84 m 63 s |
| K3 | control cabinet, square | 23.9, -17 | none | 8 s, from 4:33 | 1 / 0 | 35 m 28 s · 70 m 53 s · 38 m 30 s |
| K4 | recycling bins, Rocket Lane W pavement | 55.4, -61.8 | none | none, all mission | 0 / 1 | 35 m 28 s · 37 m 29 s · 75 m 57 s |
| K5 | recycling bins, Rocket Lane W | 7.4, -61.8 | none | none, all mission | 0 / 1 | 56 m 43 s · 82 m 62 s · 40 m 32 s |
| K6 | recycling bins, Rocket Lane W | 46, -61.8 | none | none before; from 6:54 | 0 / 0 | 34 m 27 s · 44 m 34 s · 68 m 51 s |
| R1 | behind the café (Starline side) | -23.4, -35 | none | none, all mission | 0 / 1 | 87 m 65 s · 112 m 83 s · **27 m 22 s** |
| R2 | service lane, north mouth | 49, -44.5 | none | 45 s, from 4:03 | 1 / 0 | **17 m 15 s** · 33 m 27 s · 64 m 48 s |
| R3 | service lane, middle | 63, -44 | patrol 32% | 40 s, from 4:28 | 1 / 0 | **27 m 22 s** · **19 m 17 s** · 77 m 58 s |
| R4 | service lane, south end | 74, -43.5 | the driver (to 5:50) | 40 s, from 4:48 | 1 / 0 | 38 m 30 s · 11 m 11 s · 88 m 66 s |
| R5 | Office east side | 63.8, -18.8 | patrol 41% | none (from 5:48) | 0 / 1 | 29 m 24 s · 27 m 22 s · 77 m 58 s |
| R6 | official tower pocket | 72.8, -34.8 | the driver (to 5:50) | 15 s, from 5:10 | 1 / 0 | 41 m 32 s · 6 m 7 s · 91 m 68 s |
| R7 | café east end | -18.8, -16.3 | none in the sim (see open issue 3) | none (from 8:02) | 0 / 1 | 79 m 59 s · 109 m 81 s · 19 m 16 s |
| D1 | **Office service door** (proposed) | 63, -41.1 | none | 40 s, from 4:27 | 1 / 0 | **26 m 22 s** · **18 m 16 s** · 77 m 58 s |
| D2 | café kitchen door (proposed) | -22.4, -26 | none | none, all mission | 0 / 1 | 90 m 67 s · 121 m 89 s · **30 m 25 s** |

**Nothing placed in the world today can hold a body.**
- UtilityCabinet (1.29 × 1.28 × 0.64 m) and RecyclingBins (1.06 × 0.89 × 0.42 m) are too small. They screen a body but cannot hide one.
- Of the street-scale kit, only **Dumpster** (2.1 × 1.19 m, 1.51 m high, status *candidate*) is a closed container large enough. It is placed nowhere.
- Size alone is not enough to decide this. Streetlamp, PhoneBooth, CoffeeCart, CrateStack and the AlleyWall/Stair/Bridge pieces all pass the size test, so "can hold a body" has to be a tag in kit.json.

**The takedown spots themselves** (security sight excludes the target):
- **The guard's door post:**
  - The entrance camera sees it the whole mission.
  - The sweep sees it for 66% of its cycle.
  - The crowd is quiet **0:00–4:13** (then 4:35–5:05).
- **The driver at the pickup:**
  - The pickup camera sees it the whole mission.
  - **Every second before the chime is witnessed.** Office guard p-records-office-29 (crowd, routine "outside the Official entrance" until 5:30) stands 4.1 m from the driver.
  - So the driver cannot be taken unseen where he stands. He has to be taken where he goes.
- **Inez at the café door** is witnessed every second by servers and seated customers.

Ground share: of 7725 one-metre ground cells, 4849 (63%) are never seen by anyone before the chime and 1867 (24%) after it. The handover surge closes most of the block.

## 2 · The rule: what counts as quiet (for Motive and Keel)

The rule has two parts:
1. **Holding a body** comes from the world, never hand-placed.
2. **Being quiet** is computed every tick from the perception the engine already has, never tagged.

### 2.1 Where a body can be (from the world)
- **Container.** A placed street item whose kit asset carries `stash` (proposed kit.json field: `stash: { holds: 1 }`; the load time is a rulebook value, Motive's).
  - A body inside is never found by sight.
  - It is found only by a search, at the Alert level Motive sets, or by a person whose routine stops at that piece (none today).
  - Capacity: one body.
- **Service door.** A building entrance of kind `service` (proposed block field `entrances[].kind: public | official | service`).
  - Putting a body through it is a timed off-screen action (an exterior-only stash), and the body is then off-screen.
  - It is found only by a search, or when a routine uses that door. Routines already stop at entrances by id (`office-entrance`, `official-entrance` in `handover.people.json`), so a service door is just one more such place.
- **Ground.** Anywhere else. A body on the ground is never hidden. It lies in the open and is found by the test below, at every tick until it is moved.

### 2.2 The out-of-sight test (no new numbers)
`seenBy(p, t)` = the set of watchers that are up (not down, not the body itself, not looped) and see point p at tick t, through the engine's own functions:
- **camera:** `G.InCone(at, facing, reach, half, 0, p) && Sees(at, p)`, unless looped
- **guard:** `GuardWatching(p)` (door cone at the post; facing cone and `near-m` when walking)
- **patrol:** `PatrolWatching(p)` (sweep cone at `SweepPos(t)`, nothing while rerouted, facing cone while chasing)
- **response officers:** `SecOfficerWatching`; **rivals:** their cone
- **every simulated person on the street** (cast and crowd, `OnTheStreet`): `|p − q| ≤ witness-range-m && Sees(q, p)`, which is exactly `WitnessesOf`
- `Sees` = `Site.Sight`: occluders that span `eye-cm`, plus `CrowdBlocks` wherever the engine applies it

Quietness over a span of time:
- `quiet(p, t0..t1)` ⇔ `seenBy(p, t)` is empty for every tick in t0..t1.
- **An act is unseen** ⇔ its span is quiet at each tick at the actor's position. That covers the takedown at the target's spot, the carrier along the carry path while carrying, and the stash at the place while loading. A seen act is an incident under the existing witness rule.
- **A body on the ground is found** at the first tick its position is not quiet. A body in a container or through a service door is never found by sight.
- **The price preview** can run the same test forward, because routines are deterministic under the current loops and reroutes. "Seen by" lists the watchers who fail the test along the planned act and shows "?" until the Scout's Watch has revealed them.

Two choices for Motive:
- **(a) Eye height for bodies.** A lying body is low, and a planter or the café's 1.13 m terrace wall would hide one from a standing person. I propose the existing eye-height test for v1: one rule, readable. A body-height test (occluders that span 0.3 m also block) can come later.
- **(b) Carry pace.** The survey carries at walk pace (`op-speed-cm-s`) after `carry-s`. If Motive slows the carry, every carry time above scales by 1.4 / v.

### 2.3 What the world export needs (Splice; Vantage's block part)
1. **kit.json:** a `stash` field on assets. Adopt **Dumpster** (candidate → adopted is Vela's call on the art) with `stash: { holds: 1 }`.
2. **civic block** (my layout; proposed as an entry in `civic-block.adjust.json` with its reason, applied by `bun run block`, not hand-edited):
   - add `entrances[].kind`
   - add **D1, the Office service door**, on the west wing face into the service lane at about (63, -40.5). GDD §07 route A already says "exits service door with Vale", and the block has no such door.
   - add **D2, the café kitchen door**, on the café's north (back) face at about (-22.4, -26), onto the existing 'Behind café' strip.
3. **world generator, a container rule** (derived, never placed per mission): one Dumpster against the wall beside each service entrance. It stands clear of the door, outside the lane's walking corridor (the service lane is 5.5 m; a Dumpster is 1.19 m deep, leaving more than 4 m), and outside any keep-clear `bars`. On the civic block that puts one Dumpster beside D1 and one beside D2.
4. **world export → sim site:**
   - `street[]` items carry the resolved `stash` from their kit.
   - entrances carry `kind`.
   - the site gets `stashes: [{ id, kind: container | door, at, of }]`, derived from those two fields.
5. **Cameras agree:** the world builds a mast camera (`street/records-square/mast-camera-314`, at the café east end, -18.5, -15.5) that the sim does not watch with. Either the site reads world cameras or the world drops it (see open issue 3).

## 3 · The sheet

`briefs/013/vantage/quiet.png`, north up, 9 px/m. There are two panels: before the chime (0:00–5:30) and after it (5:30–8:30). Each 1 m cell is filled by the strongest watcher that would see a body lying there:
- red = camera
- amber = the guard at the door
- rose = the patrol sweep, deeper for more of its cycle
- violet = the rivals
- blue = the crowd or cast only, deeper for more seconds
- green = nobody

Markers: circles are recesses, diamonds are kit pieces in the world, dashed squares are the proposed service doors. Carry walks are dashed and labelled with the place and seconds (lift included): from the guard (amber) to D1 and K4, from the pickup (blue) to D1 and K4, and from the café door (green) to D2.

## 4 · Fit with the Handover

The patrol column gives the share of the 100 s sweep cycle in which a whole act stays clear of the sweep. An act is 3 s takedown (assumed; Motive's number) + 3 s lift + the carry + 5 s stash (assumed).

| route | carry | seen on the way (static, share of path) | clear of the sweep | crowd |
|---|---|---|---|---|
| guard post → **D1** | 26.4 m, **21.8 s** | entrance camera 51%, sweep 91% | 27 s window per 100 s cycle | the spot is quiet to 4:13; D1 is quiet to 4:27 |
| pickup → **D1** | 18.1 m, **16.0 s** | pickup camera 32% | whole cycle | the spot is never quiet before the chime (office guard 4.1 m away) |
| guard post → R3 | 26.8 m, 22.1 s | entrance camera 50%, sweep 100% | 21 s window per 100 s cycle | R3 is quiet to 4:28 |
| pickup → R3 | 19.2 m, 16.7 s | pickup camera 31% | 62% | as above |
| guard post → K4 (bins, too small) | 34.9 m, 27.9 s | entrance camera 40%, sweep 40% | 29 s window | quiet all mission, but crosses Rocket Lane |

- **One place meets the 15–40 s band from both: D1, the Office service door, with its Dumpster.** It is 21.8 s from the guard's post and 16.0 s from the pickup. R3, the open lane beside it, gives 22.1 / 16.7 s but only as ground, so a body there is found by the crowd from 4:28.
- **Not free from the guard's post.** The takedown at the post is inside the entrance camera's view all mission, and the carry runs half its length inside it. So it needs the Tech's Loop. The sweep sees the post for 66% of its cycle, so the act must start in a 27 s window per 100 s, or the Tech also reroutes the patrol. The crowd arrives at 4:13. That is the Tech's whole kit and a clock: real, and readable.
- **Not free from the pickup.** The pickup camera covers the spot and a third of the carry, so a Loop is needed (and the Tech holds one loop at a time). More decisive: an Office guard of the crowd stands 4.1 m from the driver until the chime, so a takedown at the car is always witnessed. The driver has to be moved first: a Call, a lure, or route B's turned driver moving the pickup to the service lane. That is a people > powers answer for Waymark.
- **Too close, and why that is still all right.** Two places are under the 15 s floor from the pickup:
  - R4, the lane's south end: 10.6 s.
  - R6, the tower pocket: 6.9 s.

  Both are open ground, not hiding places. A body there is found by the crowd from 4:48 (R4) and 5:10 (R6). They are drop-and-go spots that buy about a minute, not free takedowns. No *hiding* place is under 15 s from either target.
- **The café.** D2 (kitchen door) and R1 (behind the café) are 24.5 and 22.4 s from Inez's door and unwatched all mission. But Inez is never unseen at her door either: servers and customers watch her every second. A takedown of Inez only fits if she is drawn away first.
- **Clocks.**
  - Before about 4:00 the civic south is nearly empty (at most 9 people on the street before the chime).
  - Between 4:03 and 4:48 the Office crowd closes R2, the guard's post, R3, D1 and R4.
  - After the chime only 24% of the ground is unseen.

  So the guard takedown is an early-mission act. It also tightens the clock: the guard leaves his post at 5:30 anyway.

## Open issues
1. **Assumed times.** The takedown time (3 s) and the stash load time (5 s) are assumptions, Motive's to set. The 27 s sweep window shrinks one for one as they grow.
2. **Crowd blocking.** The survey ignores `CrowdBlocks`, so crowd and security exposure are upper bounds. Keel's engine scenarios are the real verdict.
3. **World and sim cameras disagree.** The world's mast camera at the café east end is not in the sim site. R7 sits under it: the sim reads R7 as quiet, while a player looking at the world sees a camera.
4. **Hidden from the play camera.** R1, R5, R7, D2 and K4/K5 are hidden by architecture from the play camera (D2 and R1 behind the café, R5 behind the Office, K4/K5 behind the Rocket Lane west buildings). The player sees them only through the fade, which strains the hold that every entrance reads at play zoom. D2 needs a quiet-place marker that draws through the fade (Glyph, E). K6 is not seen even through the fade (a street tree), and it is not a hiding place anyway.
5. **D1 shares the lane with route B.** On route B the turned driver moves the pickup to the service lane, and Vale exits there at 5:50. A body in D1's Dumpster stays hidden. A body on the lane's ground (R3/R4) would be found by that exit. Waymark should check this.

## Handoff
- **Motive** (rules A): inputs §2.1–2.2 of this file and `quiet.json`. Done-check: the takedown and stash entries in `briefs/013/motive-takedowns.md` name the stash kinds and the test exactly as above, or say where they differ. Then Keel.
- **Splice** (export, with Vantage): inputs §2.3. Done-check: `stash` and entrance kinds come through to the site, the Dumpster rule places exactly one Dumpster per service entrance on the civic block, and `bun tools/plan/cli.ts` passes. Vela adopts the Dumpster first.
- **Aster:** route D1/D2 as a civic-block adjustment (Vantage writes it once approved; it changes the block, which C-0 guards only for scale) and the camera mismatch (open issue 3) to Keel/Splice.
- **Waymark** (D): inputs §4. Done-check: the informed route takes the guard to D1 inside the Loop, the sweep window and the crowd clock, and the driver route moves the driver before the takedown.
