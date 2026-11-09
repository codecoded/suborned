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

**Pass 1 rejected (Sky, 2026-11-10): "none of those really meet our retro future suborned vibe really".** The sheets were `paid/104-patrol-robot/codex/A-civic.png`, `B-stern.png` and `C-signal.png`.
- **Aster's read:**
  - flat illustration on beige paper, like a vintage toy catalogue, instead of the 26 Sep paintings' cinematic painted realism (glossy cream enamel, chrome, sun);
  - legs, springs, horns and toy proportions instead of 1950s industrial design (Streamline Moderne, Googie, atomic-age appliance and car lines) carrying effortless, centuries-ahead technology (our city's tech floats);
  - no city around them.
- **Pass 2 (Vela):**
  - references: the Records Office and city hero paintings, plus our in-game robot family (butler, waiter, nanny) as the family it joins;
  - two or three painted plates on the civic square;
  - one glossy lineup;
  - the play-size strip.

**Passes 2 and 3 (Aster in the main thread, on Sky's word: "just you did it here, no need for vela"; Vela's task cancelled).** All $0 Codex, through `paid.py`; images in `paid/104-patrol-robot/codex/`, with references in `paid/104-patrol-robot/refs/`.
- **Pass 2** (prompts start from `concept_prompt`; references: the Records Office and city hero paintings, plus the in-game robot family):
  - `P2-plate-1`: a hovering enamel robot with jukebox chrome, a red tail-light and a paper-tape printer;
  - `P2-plate-2`: a long 1950s car-body hover robot, too big for the dock;
  - `P2-plate-3`: a tall doorman robot on a hover disc.
- **Sky:** "think they would have more warning lights on them, and more primary colors to stand out".
- **Pass 3** adds a security livery: signal red, cobalt blue and chrome yellow in big blocks over cream enamel and chrome, with a light bar or beacon, side markers and tail-lights.
  - `P3-lineup-1`: six designs. 2 (light bar) and 5 (three beacons) read most as security.
  - `P3-plate-1`: on the square, it hands a printed ticket and holds a stamp; close to lineup 2.
- **Next, Sky's call:** refine a pick, push another sheet, or a turnaround of the pick for the 3D piece.

**Pass 4 (Aster, "show me more concepts").** All $0 Codex, in the security livery:
- `P4-lineup-objects`: six designs built on 1950s street objects (parking meter, petrol pump, jukebox, fire-alarm call box, locomotive nose, traffic signal). The call box reports by handset, which fits how information moves in the city.
- `P4-lineup-vehicles`: robot riders on hover-scooters and tiny patrol cars. They read as traffic police; 2 and 4 are close to real car and scooter designs.
- `P4-plate-dusk`: two petrol-pump patrol robots answering an alarm at dusk outside the Records Office, light bars on.

Strongest so far: `P3-lineup-1` #2, the call-box and petrol-pump robots, and `P3-plate-1`. 11 images in all, $0.
