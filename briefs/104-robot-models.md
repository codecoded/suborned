# 104 · The robots' models

**Asked by:** Sky (2026-11-10), after playing 0.2.303: "game played well. Robots are wrong models though - check our suborned store for all robot models we made in past".

## Objective
Put the right robot models in the Handover, picked by Sky from every robot model we've already made.

**Today in the game:**
- **The response dial's robot units** (sent when the Alert rises) are drawn with `DK_RobotPorter_Play`, a luggage porter with a signal lamp on top (`HostViews.cs:379`, `PlayInstall.cs:122`).
- **The patrol** is `DK_PatrolRobot` (brief 031), with the attention face.
- **The square's dock** is `CivicRobotDock`.
- **Street life in the old town** uses `RobotWaiter`, `RobotShopkeeper`, `RobotNanny`, `RobotButler` and `RobotPorter` (`DecoKitStreetLife.cs`, `DecoKitSouthsideLife.cs`).

**Made before, in `~/Projects/the-suborned-store`:**
- `testbed-art/mobility/Robots/RFM_Robot{Concierge,ParcelCourier,StreetSweeper,TrackedRepair}V1.glb`
- `testbed-art/mobility/Vending/RFM_RobotServiceV1.glb`
- `testbed-art/civic-street-props/civic-robot-v1.glb` (the butler's far mesh on a plinth)
- the concepts in `concepts/Robots/` and `concepts/Props/sheets/robots_{civic,service}.png`

There are also the kit's DecoKit robots and anything in `~/Projects/the-suborned`.

## Owner
Vela (the board and the mapping). Then Latch (install) once Sky picks.

## Done-check
1. One board shows every robot model we've made, rendered the same way and at play size, plus the robot concepts.
2. It marks what each role uses today and proposes a model for each role.
3. Sky picks.
4. The build shows the picked models in their roles.

## Evidence
`briefs/104/vela/robots/` (board and notes), then the build's shots.

## Next owner
Vela, then Sky, then Latch.

## Vela's board (2026-11-10, cdd312770)
14 models are on one board (`briefs/104/vela/robots/`).
- **Proposed:**
  - the Patrol robot with the red lamp for the response units;
  - a Patrol robot standing in the square's dock;
  - the store robots stay in the store.
- **Aster's note:** the small blue-domed service robot reads close to a famous film robot. Repaint or reshape it later.

## A new patrol robot: concept work (Sky, 2026-11-10: "create some concept work for a suborned patrol robot")
**Vela, $0 Codex stills from an open brief (the style contract's `concept_prompt`):**
- two or three lineup sheets of about six designs;
- a play-size strip at about 80 px;
- her top three.

**The robot's world facts, given to the art:**
- it walks a round and is sent from street docks when the alert rises;
- it reports people who linger;
- it is clockwork-smart, with no networks;
- a dot-matrix face shows its four attention stages, with no cartoon eyes;
- an unlit emissive alarm signal;
- the city's "optimistic surface, coercive underneath";
- original, like no film or game robot.

**Deliverables:** `briefs/104/vela/patrol-concepts/`, with images in `paid/104-patrol-robot/`.
