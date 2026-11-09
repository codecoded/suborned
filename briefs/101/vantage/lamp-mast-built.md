# The square's lamp mast: the world steps built

Brief 101 · Vantage, with Datum's plan step and Splice's generator step · 2026-11-09 · Sky's pick B (`briefs/101-hud-sweep.md`, "The square's camera: Sky picks B").
Proposal: `square-camera.md`. This round builds its handoff steps 1–3. The kit (Vela), the engine (Keel), the routes and bands (Waymark), the words (Fable) and the build (Latch) come after the merge.

## What changed

1. **Plan (Datum), `contracts/world/harbour-district.plan.json`, v0.5.7.**
   - `streetscape.watchers.sites.square`:
     - `mainEntrance` names the square's boarding pavilion.
     - The mast stands at the square's edge nearest the pavilion, its three heads fanned 60° apart over the square, its back to the street. The block names the standard it stands on and its turn.
     - A block that names none (a run's pinned block) keeps the old corner rule.
   - `sites.square.robot`: the dock keeps its own rule, 2 m west of the square's corner point nearest the circus.
   - `types.mast-camera.headStep` is now set per site: square 60, circus 120 (with `headStepWhy`).
   - `on` says that a square's mast is the square's lamp standard, carried up to the mast's height.
   - Added a `notes.history` entry for 0.5.7.
2. **Block (Vantage), `contracts/world/civic-block.adjust.json`** (then `bun run block` → `civic-block.json`).
   - `lamp-terrace-w` moves to (−3.85, −42.55): 0.15 m north and 0.15 m west, away from `police-3`. It keeps its globe and banner (the lamp piece, yaw 240).
   - It carries the mast: `mast: { site: "square", headDeg: 60 }`, with the turn's reason.
     - `headDeg` is the first head's facing in the block frame.
     - The heads face block yaw 60, 120 and 180: compass 120 (ESE), 60 (ENE) and 0 (N).
   - Table 7 has its parasol back (8 of 8).
3. **World (Splice).**
   - New `tools/world/gen/street/mast.ts`:
     - `squareMast`: the mast's place and yaw from the block.
     - `mastSite`: the watcher site an item's edge stands for.
     - `headStepDeg`: the head step by site, or one number from a plan before 0.5.7.
   - `transit.ts`: the square's mast stands at the block's lamp, at its turn. If the block names a mast and it fails the street layer, the build stops: otherwise the dock would silently take the mast's id. The dock keeps today's corner (block (−18.5, −17.5)). Mast first, dock second, so the ids stay `mast-camera-z0` and `civic-robot-dock-z1`.
   - `quiet.ts`: the export's facings use the mast's site's head step.
   - The zone's kit pin for the mast moves with it (`harbour-district.zone-kit.json`: the mast's place and the plan's hash only).
     - `bun run world:pin-zone` also rewrote five other pins to the look's swapped pieces (Bollard → Bollard_Play, Streetlamp → Streetlamp_Play). Those changes were not kept: they are not this change, and ZK-1 passes with the pins as they were.
   - `tools/world/rules/civic-dress.test.ts`: 8 parasols, not 7.
4. **Derived data (`bun run sim:site`, `bun run cover`, `bun tools/sim/lanes.ts`).**
   - `sim/data/handover.takedown.json`: the mast's entry (below).
   - `sim/data/handover.site.json`:
     - Six walk cells change: two freed at the old mast by table 7; round the moved lamp, three freed and one blocked.
     - The 5 cm walk raster and its counts change too.
     - The cast's hash changes (see Merge).
   - `sim/data/handover.gaps.json`: the gaps round the old mast and the lamp.
   - `contracts/world/civic-block.cover.json` and `sim/data/handover.lanes.json`: only the input hashes they record (plan, block, site). Their points and lanes are unchanged.

## The engine's new mast

`sim/data/handover.takedown.json`:

```json
{ "id": "street/records-square/mast-camera-z0", "kind": "mast-camera", "at": [-385,-4255],
  "facings": [[500,866],[-500,866],[-1000,0]], "range": 3000, "coneDeg": 70, "inSim": false }
```

- Block cm.
- The facings are compass 120 (ESE), 60 (ENE) and 0 (N).
- The world export's item: plan (−172.55, −226.15), yaw −0.5236. Its facings are [−0.5236, 0.5236, 1.5708] rad.
- The dock is unchanged: `civic-robot-dock-z1` at block (−1850, −1750).

## The proof: no head sees the start or the car

`proof/proof.py` runs on the committed data. Its output is `proof/proof.txt`.

- **The rules:** the engine's cone, exactly (Geo.InCone in integer maths, ±34.99° to 30 m), and its sight lines (lens 7.68 m, the head or the chest), with no crowd.
- **The port:** `proof/sight.py` checks it against Keel's N67 for the old mast. All four facts hold.
- **The samples:**
  - every point of a 5 cm lattice within 2 m of each of the four marks (the loose stance, GDD §07), free or not;
  - a 10 cm lattice over the start zone, the car and the holds behind it (block x −2..18, y −53..−44);
  - the eight waits and holds the reference plans use.
- **What's seen:** none of them.
- **The margin is 18.7°.** The nearest is the Scout's stance at (−1.6, −42.3), 2.26 m from the mast, outside the ESE head's south edge.
  - The car area is 28.8° outside, and the waits and holds 39.7°.
  - Turned clockwise, the mast first sees the stance at 18.65°. Turned counterclockwise, it sees nothing of it within 60°.

## Vela's piece, folded in (Aster, 2026-11-09)

Vela's `MastCamera_Lamp` (adopted; Sky: "looks good") came in by merging `t3/square-mast-camera` (6c009678c). Her notes are `briefs/101/vela/lamp-mast.md`.

- **The square's mast takes the piece.**
  - On the block's lamp, with the piece adopted, the item is the variant `lamp` (`transit.ts`).
  - The chooser gives that variant the use `mast-camera:lamp` and a slot of its own, 1.8 × 1.3 m and 8.2 m (`kitrefs.ts` `LAMP_MAST`, `slotOf`), so it takes `MastCamera_Lamp` by its tag at scale 1 and the item's own yaw. No other mast can take it.
  - K-1 holds even a named kit ref to its slot, and the old mast slot (1.2 × 1.2 m) is too small for the piece's 1.77 × 1.25 m. Its own slot passes K-1, and the zone pins the piece like any other.
- **The item's footprint** is the piece's plinth, 0.58 × 0.58 × 8.2 m.
- **One piece at the site.** `dress.ts` no longer draws `lamp-terrace-w`'s own `PlazaSphereLamp_Play` (`mast.ts` `drawnByMast`): the mast's piece carries its globe and banner.
  - A kit without the piece draws today's MastCamera and the lamp, as before.
  - The plan still walks the site as the lamp's 0.52 m plinth (`civic.groundPieces`). The engine's site walks the piece's declared volumes, so no walk cell moves.
- **The zone pin** for the mast now names `MastCamera_Lamp`, at its place by the lamp. That was the only pin to change.
- **The export:** `street/records-square/mast-camera-z0` takes `{ "id": "MastCamera_Lamp", "scale": [1, 1, 1] }` at yaw −0.5236. Its camera k looks along `facings[k]`.
  - The ids are unchanged: `mast-camera-z0` and `civic-robot-dock-z1`. The dock is unchanged.
  - The circus masts keep `MastCamera`.
- **The engine's data:**
  - `sim/data/handover.takedown.json` is unchanged.
  - In `sim/data/handover.site.json`, only the solid digest (`grid.collide`) and the cast hash move.
  - In `handover.gaps.json`, only its `split` count moves.
  - The proof prints the same.
- **Splice's two items from Vela's notes:**
  - `MastCamera_Lamp` joins `BUILD` (`tools/decokit/orders.ts`).
  - The fit table skips rows whose piece `kit.json` has rejected (`tools/decokit/run.ts`). On both manifests, the main checkout's and Vela's, the table's 12 rows with 11 failing (exit 1) become 1 row that fits, SpaceHQ_Portal (exit 0).
  - This was checked on the table's lines alone. `bun run decokit` itself was not run here: it would write into the shared generated root.

## Clearances (1.2 m)

- On the ground, `police-3` (−2, −42) is 1.52 m from the piece's 0.58 m plinth (as turned) and 1.60 m from its sleeve.
- Overhead, the banner (2.4–3.8 m), the crown and the cameras (7.4–8.2 m) come within 1.09 m of `police-3`.
- The nearest Handover point is the Scout's mark, 2.54 m from the plinth.
- The wall's west arc is 2.45 m from the pivot.

## For the next owners

- **Latch (build), in the main checkout after the merge:**
  - Run `bun run decokit --only MastCamera_Lamp`, then `bun run kit`. The piece's files aren't in the shared `out/decokit` yet.
  - It's a world and kit change, so run a full batch.
  - Vela's items 4–6 are the host's:
    - the lens cue per head (`DK_MastCamera_Lamp_Lens{k}`);
    - the "?", the callout and the click reaching the crown (lenses at 7.68 m, top 8.2 m);
    - the 3.2 m stand-in cylinder at a world mast's foot.
  - `GameBoot.cs`'s camera proof stands the Enforcer 6 m along the mast's first facing. That's now ESE, over the plaza.
- **Merge (Aster):** the people file records the plan's and the block's hashes. After the merge, run `bun run people` before `bun run sim:site` or the sim tests. The people are unchanged; only that file's hash moves, and the site carries it.

## The tests that now fail, and whose they are

Every result below was compared against a clean checkout of 57133d46d set up the same way: `bun run people`, `bun run world`, the kit copied.

### Keel (`bun run sim:test`)

- **The count:** 31 of 3,374 fail. At 57133d46d none fail.
- **The cause:** run with only the new site file and the old mast, only DP1 fails. So the lamp's 0.15 m move and table 7's parasol change no play; the other 30 come from the mast's new place and heads.
- **The site's bytes:**
  - DP1: the drive session's start state. Re-record it with `suborned-sim drive-session`.
- **The mast's facts:**
  - N67.
  - SightTests.TheFanIsTheEnginesSightAtTheStart and SightTests.TheFanWithPeopleOnTheStreetIsCutByThem. The drawn fan and the engine's sight disagree on 15 and 16 samples at the N head's far edge, block (−31.65, −42.3 … −41.4): "fan in, engine doesn't".
- **A Pay or a Pressure under the mast (recorded):**
  - S24 (+3 T, want +2) and S25 (+4 T, want +3).
  - N12c (a Pressure at the Courtyard: the ledger's witness).
  - CAM-6 (a record during the Pay).
  - P85-MIXED. It crashes: once the Pressure is recorded there is no call window, and `GuardCallWindows(Inez)!.Value` throws.
  - Eight more tests run every scenario and meet the P85-MIXED crash:
    - ScenarioTests.ReplaysFromStartStateAndOrders(P85-MIXED)
    - ScenarioTests.SameOrdersSameChecksumsTwice(P85-MIXED)
    - ClosingHashTests.EveryScenario(P85-MIXED)
    - FailCauseTests.EveryScenarioEndsWithAWellFormedCauseThatReplays(P85-MIXED)
    - FailCauseTests.EveryFailKindTheHandoverReachesIsCoveredByAScenario
    - RoundsTests.WithTheRoundOffEveryScenarioPlaysAsWithNoRoundsFile
    - RulesTests.EveryRefusalHasAStableCodeAndTheHashReadsTheCodeNotTheWords
    - RedlineTests.E7_every_report_is_the_same_in_every_culture(part 13)
- **Standing in its view, and spots out of every cone:**
  - CAM-5: the Scout reported for loitering, Alert 1 at 1:06.8.
  - S22 (the Scout beside Inez).
  - N130 and N178 (the Listen's walk).
  - N20c (a run no watcher sees).
  - N23 (a draw seen: Alert 2, want 1).
  - S19 (an unseen shot answered sooner).
  - N37 (a weapon drawn at the Courtyard) and N40 (the Fixer and the Scout at the Courtyard through a Help and the call).
  - 096-HELP-9 and 096-HELP-9b (the Fixer's Helps run late: $60,000 and $50,000 left, want $10,000 and $0).
- **The posted rival under the mast:**
  - CREW-BODY, CREW-FREE and S1-FLOOR-WAKE: no unseen spot to take rival-3 down; `camera:mast-z0` leads the reasons why not.
- **Still passing:** N110, and the 096 scenario that searches for Inez in the mast's view.

### Waymark (`bun run test`)

- `tools/missions/check/check.test.ts`: the Handover, played headless › across seeds the references win as their bands say (the bands).
- `tools/simview/simview.test.ts`: a dump ends on the recorded final hash (the recorded hashes, re-record).

### Pulse (`bun run test`)

- `tools/routines/rounds.test.ts`, "the bake › the plain grid walk comes short of the drawn solids on the live world".
  - At 57133d46d the only case left was table 7. The old mast kept its service point on the far side, at (−20.3, −16.2), 2 cm short.
  - With the mast gone, the point is (−18.3, −15.2) and the walk keeps clear by itself.
  - The test's own comment says to retire the rule then, or give it a fixture.
  - The rounds file was not re-baked.

### Waiting on Latch's export (`bun run test`)

- **`tools/kit/kit.test.ts`, five tests:** the catalogue rebuild, the copy and the LOD checks.
  - They fail wherever the shared generated root lacks `MastCamera_Lamp`'s files ("missing in source").
  - Against Vela's root, which has them, all 20 pass.
  - They pass in the main checkout once `bun run decokit --only MastCamera_Lamp` has run.

After the lamp mast was folded in, `bun run sim:test` gives the same 31 failures as before it.

### Failing at 57133d46d as well (not this change)

- **The skyline runs:** three runs in `skyline.test.ts` and two tests in `units.test.ts`. On the frozen fixture plan, CAP-3 fails: `street/capstone/kerb-100` stands 1.15 m from 'Official drop-off'.
- **`drift-auto.test.ts`:** skyline r011 and people r012–r016.
- **`rounds.test.ts`,** "what the quiet play can reach": the recorded routes that touch Inez.
- **`geometry/legs.test.ts`:** Inez's legs to tables 3 and 6 at (−9.11, −27.66), against `cafe-palm-1`. It runs only where the kit is copied in.
