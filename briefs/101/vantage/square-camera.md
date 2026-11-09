# The square's mast camera: where it could stand

Brief 101 · Vantage (level design) · 2026-11-09 · **proposal only**: nothing in the plan, the block, the world, the sim, the GDD or Unity has changed.

![The three places side by side](board.png)

## Sky's note

> "I think the issue is that main square camera isn't obvious it's looking over the square, it's hidden from start view. I think it would be good to move it more obvious, would also make it a good starting point for any player to interact. Maybe have it next to where players start."

**My pick: B, the lamp mast.**
- **Where:** the terrace lamp beside the cell becomes the square's camera mast.
- **How it looks:** its three heads fan out over the café and the square, with its back to the kerb.
- **Next to the start:** it stands 3 to 8 m from the four at 0:00, in the middle of the opening view. The cell and the car stand behind its lenses.
- **First touch:** three of the four can Watch it without taking a step.
- **The café:** it still watches the café's front, so the loitering card you saw stays, but now the player sees the camera coming.

If you'd rather the café stay a safe place to wait, **C** watches the plaza instead and leaves route A as it is.

---

## 1. Today

![Today's mast and what it sees](today-plan.png)

- **Where it stands:** `street/records-square/mast-camera-z0` (the sim's `mast-z0`) is at block (−18.5, −15.5) m, the square's north-east corner. It is 33 to 38 m from the four.
  - At the opening view (yaw 30, play zoom 10) its foot is 336 px above the top edge of the screen and its lenses 663 px above. You can't see it.
  - Nobody can Watch it from the start: the Scout reaches 30 m, the Tech 24 m.
- **What it sees:** 17 % of the square's open ground, mostly the street north of the square.
  - Its south-west head is cut off by the café's wall.
  - On the café side it sees only the terrace's east end and the plaza beside it ("East side", "Plaza east"). That's where your Fixer was reported.
  - Inez is in its view 8 % of the mission.
- **Why it stands there:** the camera isn't in any mission file. It comes from the world's rules, so the fix belongs there:
  1. **The plan** (Datum), `contracts/world/harbour-district.plan.json`, `streetscape.watchers.sites.square`: "1 mast-camera at the edge nearest the square's main entrance". The type is three heads 120° apart, 70°, 30 m.
  2. **The world generator** (Splice), `tools/world/gen/street/transit.ts:128`, reads "nearest the main entrance" as "the corner of the square nearest the circus". It sets yaw 0 (heads east, north-north-west, south-south-west) and puts the robot dock 2 m west of the mast.
  3. **The export** `tools/world/gen/street/quiet.ts` turns the mast's yaw into its three facings. `bun run sim:site` writes `sim/data/handover.takedown.json`, which the engine reads. That file is derived: never edit it.
- **Its trace on my block:** café table 7 has no parasol only because this mast stands 1.17 m from it (`civic-block.adjust.json`, parasols "open"). Moving the mast gives the terrace its eighth parasol back.

## 2. What a new place has to keep

- **The cell's start stays out of every head, on every seed.**
  - The seed stands each of the four within 2 m of their mark (§07).
  - A camera reports a loiterer after 45 s of idling in its view at Heat 0. It notices at 8 s and grows wary at 20 s, and wary lasts the whole mission.
  - A new player who stops to look around at 0:00 would be reported before they've moved.
  - This is the same promise the patrol makes ("none of the cell in its cone for the first 15 s"), held for the whole start.
- **The car area stays out too.**
  - Every reference plan waits by the car (around (7, −46…−50)) and holds behind it (around (12.5–17.5, −52)).
  - If the mast sees those, every route's ending changes.
- **What I tested:** every walkable point in the opening view, every turn of the mast in 1–4° steps, with:
  - today's head layout (three heads 120° apart);
  - a fanned layout (three heads 60° apart, the mast's back to one side).
- **What I found:**
  - **Today's layout:** only one stretch of pavement keeps the start and the car clear: the café terrace's west end (A). Anywhere nearer the cell, one of the three heads always lands on the cell or the car.
  - **The fan:** it works right beside the cell (B), and in the fountain garden facing away from the café (C).
- **How it's measured:** the engine's own sight rules.
  - The cone is a 30 m × ±35° wedge per head, and all three heads see at once.
  - The lens is at 7.68 m. A person is seen when the line to their head or chest is clear of the site's walls.
  - Crowds are off, as in Keel's tests.
  - I ported these into a scratch script and checked it against Keel's N67 for today's mast. All four facts hold: it sees Plaza east and East side; it doesn't see the café door (wall) or the Office entrance (too far). Keel's engine stays the authority (section 6).

---

## 3. Three places

| | today | **A** terrace end | **B** lamp mast | **C** fountain mast |
|---|---|---|---|---|
| Stands at (block, m) | (−18.5, −15.5) | (−11, −48) | (−3.85, −42.55) | (3, −33.5) |
| Head layout | 3 × 120° | 3 × 120° (today's) | 3 × 60° fan | 3 × 60° fan |
| In the opening view (1920 × 1080) | no, 336 px above the top | foot (380, 420), lenses (380, 93) | foot (828, 535), lenses (828, 208) | foot (1436, 582), lenses (1436, 255) |
| Inside the Air's narrower frame | no | yes | yes | yes |
| Distance to the four at 0:00 | 33–38 m | 10–14 m | 2.8–8 m | 11–14 m |
| Who can Watch it without moving | nobody | Scout, Tech, Fixer | Scout, Tech, Fixer | Scout, Tech |
| Square's open ground seen | 17 % | 21 % | 42 % | 64 % |
| Sees the cell's start or the car area | no | no (only 3–4° spare; its south-west head passes 1–2 m from the car waits) | no (18° spare on the cell's side) | no (±8° spare) |
| Sees the café's front (Inez's door, the Courtyard) | no | **yes** | **yes** | no |
| Inez in its view | 8 % | 85 % | 92 % | 26 % |
| Rival posts round our car in view | none | 2, 4, 5 | 2, 4, 5 | 1, 3 |
| Type change (Datum) and new kit piece (Vela) | | no | yes | yes |

Every option reads the same way once watched:
- **The first touch:** a click on the mast opens its ring. The left hold is Watch (§10: "a camera goes Watch, then Loop").
- **Loop or Disable:** done at the control cabinet, 45 m from the Tech's start (about 25 s walking, 13 s running, plus the 20 s Loop).
  - A Loop given at once runs from about 0:45 to 2:45.
  - One loop runs at a time. After a mast loop, route A's entrance loop goes on from about 3:05, still inside the guard's window from a 2:35 call: to about 5:29 after a Help, about 4:14 after a Pressure.
  - Disable is +2 Trail and the desk reports the dead screen (Alert 1, robots to the camera).
- **Walking round it:** walking through its view is free (a walk drains the clock). Standing in it is what counts: 45 s to a report.

### A. Terrace-end mast (today's mast, moved)

![A from the opening view](A-opening.png)
![A in plan](A-plan.png)

- **Where:** on the pavement at the café terrace's west end, block (−11, −48), plan (−178, −219), by the kerb rail.
  - Its heads: north along the pavement, east-south-east across the café front, south-west over Rocket Lane.
  - The cell and the car stand in the gap between the second and third heads.
- **Looks:** today's mast turned, with no new piece: a slim 8 m cream column with seafoam bands and a thin brass collar, under a chrome crown of three streamlined lens pods. The kerb rail breaks for it.
- **First minute:** in plain view at the top left, about 10 m from the cell. The Scout, Tech and Fixer can Watch it where they stand; the Enforcer walks about 11 m.
- **What it changes:** the same as B (below): it sees the café's front, the terrace, posts 2, 4 and 5, and route B-thin's takedown spot. It also watches Rocket Lane north and south of the square.
- **Weak points:**
  - It covers the least of the square.
  - It sits high in the frame, at the top-left corner.
  - Its margins are thin: the safe turn is a 7° window (97–104°), so 3–4° either way, and its south-west head passes 1–2 m from the waits by the car.
- **Who builds it, and how big:** small on the world side.
  - Datum: words for the plan's square rule. "The main entrance is the square's boarding pavilion; the mast stands on the square's pavement at the corner of its frontage nearest it, with the gap between two heads on the pavilion."
  - Splice: that rule in `transit.ts`.
  - Vantage: the rail break.
  - No new kit.
  - Medium on the mission side: Waymark's re-plans are the same as B's.

### B. Lamp mast (recommended)

![B at 0:00, before any Watch](B-start.png)
![B from the opening view, once watched](B-opening.png)
![B in plan](B-plan.png)

- **Where:** where `lamp-terrace-w` stands today, on the square at the terrace's south-west corner: block (−3.85, −42.55), plan (−172.55, −226.15).
  - That's 0.15 m off the lamp's own spot, so the police stand `police-3` keeps its 1.2 m clearance.
  - Its three heads fan 60° apart, to the north, east-north-east and east-south-east, over the café's front, the terrace and the fountain plaza.
  - Its back is to the kerb, so the cell, the pavilion, the car and the waits behind it are all out of view.
- **Looks:** the terrace's sphere lamp grows into the square's mast.
  - The globe and the café's green banner stay at lamp height (4.6 m).
  - The column carries on to a chrome crown at 7.7 m, where three lens pods fan out over the square.
  - It's a city that lights and watches its square from the same standard: 1950s in its shape, centuries ahead in the closed-circuit eye that a person at a desk watches live.
- **First minute:**
  - At 0:00 the player sees a mast with a "?" right over the four, its lenses plainly pointing away from them at the café and the square.
  - The Scout (2.8 m), the Tech (5.4 m) and the Fixer (5.5 m) can Watch it without a step. The Enforcer walks about 5 m.
  - Once watched, its cones show right beside the cell, which stands just behind them. "Behind a lens is safe, even close up" (§05) becomes the first thing the game teaches.
- **What it changes (route A and the posted rivals):**
  - **The Fixer's wait (route A).** She waits at the Courtyard twice: about a minute before the Help, and about 1:20 before the Call. Both are in view, so she'd be reported at 45 s.
    - Re-plan: she waits behind the mast, where the cell starts, 17–20 m from Inez, and walks in for the Help and again for the Call (about 10 s each way).
    - Or the Tech loops the mast first (the entrance loop then follows at about 3:05, still in time).
  - **The Scout's Watch of Inez.** Done from beside Inez, those 20 s count on the mast's clock and leave her wary for the mission. She can Watch Inez from where she starts instead: 17 m, a clear line. The Listen doesn't count (an act in view holds the clock).
  - **Pressure on Inez is on camera.** The mast sees the Fixer at Inez's side: +1 Trail and a report at the desk's next scan (Alert 1, robots), unless the mast is looped. Help, Listen and the Call are never recorded.
    - This touches route-a-pressure and thin-quiet ("one Pressure and the call").
  - **Posted rivals.** The rival posts by the café's west wall (2, 4, 5) are in view.
    - A takedown there is seen (Alert 3): route B-thin's takedown at (−7, −36) must move or come after a loop.
    - A Pay there is recorded: quiet-b-thinned on the seeds where its man posts there.
  - **Unchanged:** route B's work at the Office, route G, the waits and holds by the car, the veteran's corner, the Tech's run to the cabinet (it runs behind the mast).
- **Who builds it, and how big:** medium. One plan type change, one kit piece, one generator rule, one block entry; plus Waymark's route A re-plans. Handoff in section 5.

### C. Fountain mast (the café stays a safe place)

![C from the opening view](C-opening.png)
![C in plan](C-plan.png)

- **Where:** in the fountain's north-west flower bed, beside the garden's lamp: block (3, −33.5), plan (−163.5, −233).
  - Its three heads fan 60° apart, to the south-south-west, south-east and east-north-east, over the plaza, the fountain and the way across to the Records Office.
  - Its back is to the café and the cell.
- **Looks:** a mast rising from the flower bed with the same chrome crown, its three pods turned toward the plaza and the Office. It watches the approach to the city's records.
- **First minute:** right of centre in the opening view, 11–14 m from the four. The Scout and the Tech can Watch it where they stand; the Fixer steps 2 m.
- **What it changes (much less):**
  - **Route A holds:** the Courtyard and beside-Inez stay unwatched. Inez is in its view 26 % of the time (her tables at the south end). A Pressure timed while she serves there is on camera.
  - **Posted rivals:** rival posts 1 and 3, south of the fountain, are in view. A takedown or Pay there is on camera.
  - **The veteran's shot:** one of the four's spots, the Fixer's at (30, −51), is in view, so their shot is no longer unclaimed (the seal runs). Moving that spot about 2 m fixes it.
  - **The Tech's run:** his run to the cabinet at 0:00 crosses its south-south-west head for about 5 s. The lens notices him, but no report.
- **The catch:** the café's front, where your Fixer was reported, is no longer watched by anything, so the camera matters less to route A.
- **Who builds it, and how big:** medium on the world side (the same chain as B), small on the mission side.

---

## 4. My recommendation, and what to know

**B.** It answers each part of your note:
- **Obvious:** the tallest thing near the four, in the middle of the first frame.
- **Clearly looking over the square:** its lenses point at the café and the plaza, away from the cell.
- **A starting point for any player:** three of the four can Watch it at once.
- **Next to where players start:** 3 to 8 m from the four.

It gives the camera a real job in the route most players take first. The first minute becomes a choice the player can see: watch it, then loop it, or wait behind it.

What to know before you pick:
1. **It still sees the café's waiting spot. I think it should.** The loitering report you hit is fair once the camera is in plain view and the Watch shows its cones next to the cell. Waymark re-plans route A's waits behind the mast. If you'd rather the café stay a safe place to wait, pick C.
2. **Pressure on Inez gets dearer under B (and A).** It's on tape unless the mast is looped first; Help isn't affected. Motive should rule on whether that's the price Pressure should have.
3. **One loop at a time.** Looping the mast first pushes the entrance loop to about 3:05. Route A still fits (above), but it's a real choice between the two cameras.
4. **B and C change the mast's design.** For a square, the heads fan 60° over the square with their back to the street (the circus masts keep 120°). That's Datum's plan type and Vela's kit piece, whose pods are modelled 120° apart today.
5. **The robot dock stays at today's corner.** The generator puts it 2 m west of the mast today; at B that would be inside the cell's start. Keeping it where it is also keeps the response's robots on today's timings.
6. **Ids stay `mast-camera-z0` and `civic-robot-dock-z1`.** They come from the order the generator adds them. A changed mast id once silently dropped its words and orders (brief 078 B1); `zone-stable.test.ts` locks them.
7. **Fable:** the mast's words today say "It turns between three views". The engine sees through all three heads at once ("It watches three ways at once").
8. **No GDD rule changes.** If you like it built, a line in §07's places can name the mast beside the cell's start (your call).

## 5. If you pick B: the handoff

Each step: who, what, how it's checked, who's next.

1. **Datum** (plan)
   - **What:** `streetscape.watchers.sites.square` names the main entrance (the square's boarding pavilion) and says the square's mast stands at the edge nearest it, its three heads fanned 60° over the square, its back to the street. `types.mast-camera` gets a head step per site (square 60, circus 120). The robot keeps its own rule.
   - **Check:** `bun run plan` passes.
   - **Next:** Vantage.
2. **Vantage** (block)
   - **What:** `civic-block.adjust.json` dressing: `lamp-terrace-w` becomes the mast's place, at (−3.85, −42.55), with its globe and banner kept. Its turn has a reason (heads N, ENE, ESE; the cell's start and the car area behind the lenses, 18° spare). Table 7 gets its parasol. The 1.2 m clearances are re-checked.
   - **Check:** `bun run block` and `bun run plan` pass.
   - **Next:** Splice.
3. **Splice** (world)
   - **What:** `transit.ts` places the square's mast from the block (site and turn), not the corner nearest the circus. `quiet.ts` exports facings with the site's head step. The dock keeps today's corner rule, and both ids stay (z0, z1).
   - **Check:** `bun run world`, `bun run sim:site` (writes `handover.takedown.json`) and `bun run kit` run; `zone-stable.test.ts` passes.
   - **Next:** Vela, Keel.
4. **Vela** (kit)
   - **What:** the mast piece with three pods fanned 60° and the sphere lamp's globe, banner and seafoam bands, to the DecoKit finish.
   - **Check:** the kit catalogue adopts it.
   - **Next:** Latch.
5. **Keel** (engine)
   - **What:** no rule change; the engine reads any facings. Rewrite the mast's facts in N67 and N110 and the 096 Pressure-under-the-mast scenario for its new sight.
   - **Check:** `bun run sim:test` passes.
   - **Next:** Waymark.
6. **Waymark** (missions)
   - **What:** re-plan route A's waits and the Scout's Watch behind the mast, and route B-thin's takedown. Add one rigid line: no camera, guard or robot that keeps a clock sees any of the cell's starting stance on any seed. Re-measure the bands:
     - B-1 quiet-a (≥ 90 %), B-2 thin-quiet (60–90 %), B-3 careless (≤ 30 %), B-4/B-6 prepared-loud (its Inez work is under the mast), B-8 idle;
     - R96/L96 B-thinned (posts 2, 4, 5 in view);
     - the route scripts: route-a, route-a-pressure(-board), route-a-disable, route-b-thin, careless-thin-all.
   - **Check:** `bun run missions:check bands handover` (through `bun run guard`).
   - **Next:** Motive.
7. **Motive and Fable**
   - **What:** Motive rules on Pressure's price under the mast. Fable fixes the mast's words in `text.json`.
   - **Check:** Motive's ruling; text.json's tests pass.
   - **Next:** Latch.
8. **Latch** (build)
   - **What:** the build. It's a world and kit change, so a full batch.
   - **Check:** a fresh `start-overhead-day` shot shows the mast and its "?" in the opening view.
   - **Next:** Glyph and Suture (do the opening view and the mast read on the Air?).

For **C**, the chain is the same, with the site in the fountain's north-west garden and a lighter step 6: Waymark re-measures the veteran (moving its Fixer's spot 2 m), L96 B-thinned (posts 1 and 3) and B-3.
For **A**, there's no type change and no kit step.

## 6. How I measured

- **The pictures** are the real opening view from the last batch (`out/batch/shots/start-overhead-day.png`, build 0.2.290, 0:05), with the mast drawn in and its cones as they'd show once watched.
  - The projection is the play camera's own: orthographic, half-height 10 m (54 px/m), pitch 38, yaw 30, framed on the opening focus.
  - I checked it against the four agents' rings in the shot; it lands within a few pixels.
  - The cones are ground the engine's rules say the mast sees, faded with distance.
  - The dashes mark what the Air's fullscreen 2880 × 1864 frame shows.
- **The plan sketches** are turned to the opening view (yaw 30 up). Red is what the new mast sees; grey is what today's sees.
- **The sight numbers** come from my scratch port of the engine's rules: `out/scratch/vantage/101/cover.py`, with the site's walk grid, floors and 298 occluders from `sim/data/handover.site.json`, and the seeded 2 m stance sampled every 0.5 m.
  - It reproduces Keel's N67 facts for today's mast.
  - The searches are `yawscan.py`, `fanscan.py`, `nocafe.py` (the café-free search, about 10 minutes on one core) and `pinA.py` and `pinC.py`; the numbers are `facts.py`; the pictures are `pictures.py opts.json today` and `board.py`.
  - It's an estimate. Keel's engine and Waymark's bands are the measure once the world is regenerated.
- **What I didn't run:** Unity, the batch, the mission check, the plan checker. No source changed.
- **Pictures under `briefs/`** are git-ignored by the repo's rule and stay on disk next to this file.
