# Brief 096 · Keel's report: the rivals round, engine 0.41.0 (2026-11-05)

Branch `t3/rival-block-and-lookout` (worktree t3-a2394298, from `work/096`, with `t3/engine-rules-getaway` merged first: engine 0.40.0, c051).
Motive's rivals page (briefs/096/motive/rivals.html, rivals.json), Sky 2026-11-04 "all recommended". GDD text: gdd-text-rivals.md.

## What changed (engine 0.41.0)

- The block on arrival, every route (GameRivalBlock.cs): the rival car turns in on its timetable (2:35 at seed 0, ±15 s by seed, unchanged)
  and drives to the block point of the stand our car is at or driving to (the stand's own exit lane, 10 m out: Rocket Lane on the square,
  the drive's mouth on the service drive); a Drive before it stops turns it. Three step out, then the lookout; the wheelman sits in until
  the spring, a shot at or within 30 m of his car, or a disable. Drawn crew walk to the Office front; the car keeps its block. The block ends
  when the broken crew's wheelman is back aboard in the final fight (he drives off for good) or the car explodes. A pre-fight break takes the
  car away 2:55, then it blocks again. A carless crew walks off for good. Start.rivalBlock (0.40.0's test seam) is retired.
- The lookout is the cell's job: he follows Popovic's car on foot (seen along his post's facing) and reaches him within 6 m of the car;
  the turned driver's warning (F4) and the car's tail (F2) are retired.
- Thinning (GameCrewTurn.cs): Listen, Pay ($30,000, Mercenary), Mislead (the lookout to the Office's public door, a posted man to the
  lookout's post, 60 s, back) on any member on foot, with the walk in; takedowns as before. Temperaments by id: rival-1 L, rival-2 M,
  rival-3 L, rival-4 (lookout) M, rival-5 L. A Loyal refusal makes the Fixer every member's foe at the spring.
- The false tip on the rival car is retired (tip-retired).
- The gang shooting (GameGangShooting.cs): unclaimed shootings seal nothing until the cell is placed; −1 Standing for a cell-lit blast with
  city people within 30 m (blast-near).
- The car as the fight's block: no blocker is picked where it boxes us, the box is the gate, the fight isn't over while it stands, its shots
  are untraced in the safe fight, a shot at it springs the ambush, and a moving car is a running target (−10).
- Rulebook: rivals.block, rivals.lookout, rivals.turn, security.unclaimed, car.target.blastStanding, standing cause blast-near; retired
  values driver-warns-s, driver-warn-lag-s, false-tip-s, cell.mislead.away. Schema: rulebook.schema096r.ts; test rulebook096r.test.ts.
- New refusal codes: tip-retired, crew-no-need, crew-gone, crew-fighting, crew-walking, refused-before (no-rivals, stand-near-rivals
  retired). New log codes: rivals.block, rivals.lookout-posted (the crucial card, item 8), rivals.lookout-follows, rivals.retarget,
  rivals.misled-back, rivals.paid, rivals.refuses, rivals.listen.heard, rivals.listen-missed, rivals.pay-missed, rivals.drives-off,
  rivals.away, car.unboxed, security.gang, security.gang-claimed, standing.blast. text.json carries PLACEHOLDER lines for Fable.

## Scenarios

30 RIV scenarios as 096-RIV-1..30 (ids starting R are the range's), ScenariosRivals096.cs and ScenariosRivals096b.cs. RIV-16 is
S1-SEE-TAKEDOWN (updated); RIV-28 is the bands; RIV-29 is the check's own replay of every scenario. Legacy scenarios on retired rules were
retired (list in ScenariosPickup059.cs header and below) or given a no-crew start (the existing Start.CrewGone seam) where the rivals were
only in the way of another system.

Retired: P59-DAY, P59-B, P59-TAIL, P59-TIP, P59-EARLY, P59-GUN, P59-S3-RUN, P59-S3-WALK, P59-LOUD-YARD, P59-BEAT, S1-FLOOR-TIP,
S1-FLOOR-LOOKOUT, S1-FLOOR-NO-SPAWN, S1-FLOOR-ZERO, S1-SPRING-LATE, S1-GATE-LOUD-YARD, BAL-YARD-AT-WILL, BAL-YARD-FOCUS-LATE,
BAL-YARD-NEVER-DRAWN, BAL-YARD-POST, BAL-LANE, SQ1-WALK, SQ1-STEP, SQ2-NO-STALL, N11b, N111, N113, N165, N185, S1-GATE-EARLY-YARD, BOX-3, BOX-6, BOX-8.
No crew (NoCrew): S05, S10, S20, S21, N19b, N33, N57b, N73, N77, N78, N81, N82, N87, N115, N116, N124, P59-FIRST, P59-NOBODY, KERB-2.

## Bands (200 seeds; before = c051's engine 0.40.0, bands-rivals-before-200.txt; after = bands-rivals-after-200.txt)

See the final message's table.
