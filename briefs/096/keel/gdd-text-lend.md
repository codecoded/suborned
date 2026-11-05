# Brief 096 · GDD text for Aster: the lend, the fight fixes, the veteran's placing (engine 0.42.0)

Keel, 2026-11-05. Source: Motive's lend addendum (briefs/096/motive/lend.html, lend.json); Sky 2026-11-05 "build the lend now" (decided
2026-10-26, brief 071 Option A; 082.1-082.4 2026-10-30). Built on branch `t3/c800160e` as engine 0.42.0. Keel doesn't edit the GDD: this is
the text for the next version (v0.39.0 or later), section by section, in the agent GDD's register. Every value [S].

## Decision-log line

2026-11-05 · brief 096 · Sky: build the lend now (071 A). Engine 0.42.0 (Keel, from Motive's lend addendum): the turned driver's car is lent
at the in-car Take's end, the Enforcer drives Popovic out in it (its Go is the win), the three get out through the block in the cell's car
before the police close in 60 s later; the wheelman stays at the wheel and drives off at the break; a broken rival runs off holstered; the
veteran's shot is placed only by a gun seen, an agent near the scene in the hold, or a known face. Switch: car.lend.split (1).

## §06 fights (the getaway)

- **Two cars [S].** With the lend (car.lend.split 1) a turned driver's car becomes a cell car when the in-car Take ends: the Enforcer at the
  wheel, the target in the back; the driver steps out and walks off. Each cell car has its own stand, seats, hold, lane, box, Go and
  seen-leaving; Board, Step out and Go name the car (the cell's own, car:getaway, or car:official once lent). The lent car is never Driven.
- **Seats [S].** The lent car takes one standing agent (the Enforcer); another boards only while nobody standing is aboard (to drive a downed
  Enforcer out). The cell's car seats the four and the target.
- **Go [S].** Refused while the car is boxed in, held (a hostile within 8 m or on its lane: the first 30 m of its way out, down the drive and
  on along Apron Road), gated (a loud fight at the car with the target: its blocker, sent to that lane, holds it while up), or with nobody
  standing aboard. The win is fixed the tick the car with the target leaves (vale.out); the mission runs on.
- **The spring [S].** The posted crew springs as today, and also, while the target sits in the lent car, on an agent standing within 20 m of
  the cell's car, and the tick the lent car leaves.
- **After vale.out [S].** 60 s later (or at the seal, or at 8:10, whichever is first) the police close in: the mission ends a win, and every
  agent not in a car that has left is captured, standing or down (Sky 082.1). It ends sooner when nobody is left on the map outside a car that
  has left, when the cell's car leaves, or at End (the menu's Withdraw becomes End). The seal, the close and the cell down no longer fail the
  mission after vale.out; the cell's car wrecked while the target is lent is no failure. Retired: the walk-out after the win.
- **Left behind [S].** The cell's car still standing at the end is lost: +1 T and hunted (the debrief's car-seen line says "left"). The lent
  car seen leaving is +1 T and a lead, not a hunted car. The driver holds the Enforcer's face, the place and the time, never the cell's car.
- **The drive-round [S].** On a stand whose car stands on a drive, the rival car's block point lies across the drive's mouth on the carriageway,
  so it boxes both the cell's car on the drive and the official car at its kerbside stop, at least 10 m from the cell's car and outside the
  5 m blast of the kerbside car (Motive's property; the engine finds the point along the kerb line). With the drive taken, the turned driver's
  way out from the pickup runs only to his kerbside stop.

## §07 rivals (the block)

- **The wheelman [S].** He stays at the wheel through the final fight; he gets out only when his car is disabled (after the others) or set
  burning (every rider at once, unhurt). When his crew breaks he drives off at once, for good, picking nobody up: the block ends at the break,
  or when the car explodes.
- **The break [S].** The crew breaks when more than half of those in the fight are down (shot, taken down or talked down): the posted, the
  lookout (in it from the spring, wherever he is) and anyone misled; not the wheelman at the wheel, nor a paid man. 4 on foot break at 3, 3 at
  2, 2 at 2, 1 at 1.
- **A broken man [S].** He is out of the fight at once: he puts his gun away and runs off the map by the crew's way off (2.8 m/s). He holds no
  car, is no hostile and never a target of weapons free. An order can still shoot him: a new shooting at a man running away, traced as any.
  A man who breaks in the final fight, or whose crew's car is gone, never comes back. A crew that breaks before the final fight with its car
  still able to drive boards it and drives off holstered, out of every fight while away, and comes back to block again
  security.rivals.retreat later (corrected 2026-11-05, Motive's misses page F2: the code was right, these words were wrong; destroying the
  car is what ends them).

## §05 pressure (the gang shooting)

- **Placed [S].** While a gang shooting is pending, the cell is placed (the seal runs from then) by: a security watcher seeing an agent's
  weapon drawn or fired, at any time; during the hold (60 s from the response's arrival) a response officer or robot seeing an agent within
  30 m of the scene (where the unclaimed shots came from and where the rival car stopped); a known face in the response's sight, at any time;
  a later shooting that is not unclaimed, any new incident, or Alert 3.

## §10 presentation (for Latch, Glyph, Fable, Chime)

- Two Go's: one on each cell car's card and ring, each with its own refusal (boxed, held, gated, nobody at the wheel) naming the car or the
  holders. Board on the lent car says who is at the wheel when full.
- The lent car labelled as ours from the lend (car:official, "Popovic's car"); the cell's car stays "our car".
- vale.out: Popovic is out, the card that stays; then the police countdown (Game.SplitPoliceAt) as the one plate under the clock.
- The left-behind flags: everyone not in a car that has left, captured when the countdown ends; End replaces Withdraw after vale.out.
- The debrief: our car lost ("left"), Popovic's car seen leaving (a lead).
