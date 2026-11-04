# Brief 096 · Keel's report: the lend, engine 0.42.0 (2026-11-05)

Branch `t3/c800160e` (from `work/096`, with `t3/rival-block-and-lookout` merged first: engine 0.41.0). Motive's lend addendum
(briefs/096/motive/lend.html, lend.json); Sky 2026-11-05 "build the lend now". GDD text for Aster: gdd-text-lend.md.

## What changed (engine 0.42.0)

- **The lend (car.lend, GameLend.cs; switch `car.lend.split` = `getaway-split` 1).** The in-car Take lends Popovic's car: the Enforcer at
  the wheel (`Op.InLent`), Popovic in the back (`ValeAt.Lent`), the driver walks off to the Meet-driver point and off the map. Board, Go and
  Step out name the car (`car:official`; none = ours). One standing seat (`board-lent-full`); never Driven (`drive-lent`). Its hold: within
  8 m or on its lane (the first 30 m of its way out, down the drive on the drive's polygon, then Apron Road at 6 m). Go refused boxed / held /
  gated (a loud fight at it: its blocker goes for its lane, 10 m down the drive) / nobody standing aboard. Its Go is `vale.out`: the win is
  fixed, the mission runs on; the crew springs that tick (and, while Popovic is lent, on an agent within 20 m of our car). After vale.out the
  police close in 60 s later, or at the seal, or at 8:10: a success, everyone not in a car that has left captured (`LeftBehindAtGo`, the
  custody projection as before). It ends sooner when nobody is left outside a car that left, when our car leaves, or at End (Withdraw
  becomes End). The seal, the close, the cell down and our car wrecked no longer fail after vale.out. Our car left standing is lost (+1 T,
  hunted, debrief car-seen "left"); Popovic's car seen leaving is +1 T and a debrief lead (`lent-seen`). Host reads: `Lent`, `LentGone`,
  `GetawayGone`, `ValeOutTick`, `SplitPoliceAt` (the countdown), `LentHolders()`, `LentGoQuery()`, `LentHoldsAt(p)`.
- **L10, the drive-round block point.** With the lend on, a stand whose car is on a drive blocks across the drive's mouth: the engine walks
  the kerb line from its middle in 25 cm steps for the first point that boxes both our car on the drive and the official car at its kerbside
  stop, ≥ 10 m from ours and ≥ blast + 0.5 m from the kerbside car: (82.36, -14.25), against Motive's hand-worked (82.40, -14.50). The
  car stops square to the kerb. Companion rule, found in testing: the official car's way out from the pickup ran 30 m on along Apron Road,
  through the mouth, so Motive's point boxed Popovic at the pickup for good; with the drive taken, that way out now runs only to his kerbside
  stop (lend on only).
- **F1** the wheelman stays at the wheel (`rivals.block.wheelmanNear` retired); out only when his car is disabled (after the others) or
  burning; at the break he drives off at once. **F2** a broken man holsters and runs off the map at `rivals.block.brokenPace` (280 cm/s):
  no holder, no hostile, no weapons-free target; an order still shoots him, traced. A crew broken before the final fight makes for its car
  holstered and out of every fight. **F3** the break counts those in the fight, not the wheelman at the wheel. K3 (rivals go for a car nobody
  guards) now counts the rival car's box as the hold (with F1 nobody stands within 8 m; the box is already the gate).
- **V1** a burning car's riders all get out at once, unhurt. **V2** the gang shooting is placed by a gun seen by any security watcher, an
  agent within `security.unclaimed.scene` (30 m) of the scene in the hold, or a known face; the scene is where the unclaimed shots came from
  and where the rival car stopped.
- Rulebook: `car.lend`, `security.unclaimed.scene`, `rivals.block.brokenPace`, values getaway-split, lend-seats, split-police-s,
  unclaimed-scene-m; rivals-wheelman-near-m retired. Schema rulebook.schema096l.ts; test rulebook096l.test.ts. Refusal codes stand-at-car,
  board-lent-full, drive-lent, car-left, car-disabled, not-out, agent-gone; verb `end`; log codes lend.lent, vale.out, split.police,
  lend.spring, lent.seen, car.leaves-split, car.lost, lend.goes-on. text.json carries placeholder words for Fable (locks plain, silentLogs
  marked PLACEHOLDER). Engine version suborned.sim/0.42.0; DP1's drive session re-recorded.
- Plans (Waymark's part, written by Keel): quiet-b, quiet-b-thinned, quiet-b-lookout-ignored, drive-round, drive-early, quiet-b-taped and
  veteran on the lend; prepared-loud kept as today's front-door plan (it never uses the lend: in band) with the lend version as a new variant
  `prepared-loud-lend`. The plan runner (sim/cli/Adaptive.cs) reads F2 (a broken man is no foe on the map; fight bodies no longer counted
  twice), a `$at:<op>` move target (the map shows where an agent fell) and the lend's visible logs. route-b's reference plan recomposed on
  the lend (the lent car out at 7:03, our car Go at 7:47.2): record c052.

## Scenarios (Motive's list is the test list)

096-LEND-1..13, 15..21, 096-FIX-1..3, 096-FIX-4a/b, 096-VET-1..3 (ScenariosLend096.cs, ScenariosLend096b.cs). Not built: LEND-14 (a
downed agent carried into our car: the carry and Board are 0.41.0's, unchanged; the lend plans' rearguard carries a downed agent when the countdown leaves time), VET-4 (a
late placing: needs a veteran that survives to be placed late; the veteran's band reads it). LEND-B: the bands below and the check's replay
of every scenario. One-car twins (Start.OneCar, test only: split read as 0): S20, S21, N19b, N33, N77, N78, P59-FIRST, KERB-3, 096-RIV-2,
096-RIV-30; LEND-11 plays its own split-0 twin (0.41.0's point boxes ours, not Popovic's). Retired: 096-RIV-20 (the wheelman shot outside at
the spring: F1). BOX-4 now has only the wheelman aboard and the cell firing together (F1 kept the wheelman in, so the old lone Scout was shot).

## Bands (200 seeds; before = 0.41.0, bands-rivals-after-200.txt; after = bands-lend-after-200.txt)

See the final message's table. Misses, causes and Motive's levers are there too.
