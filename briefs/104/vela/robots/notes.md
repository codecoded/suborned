# Brief 104 · Every robot model we've made (Vela, 2026-11-10)

Board: `briefs/104/vela/robots/board.png` (1000 × 5955 px, for a phone). Like other renders under `briefs/`, it is git-ignored and stays on disk.
Numbers below match the board.

## What we have: 14 models in 2 places

**In the game's kit** (this repo, `unity/Assets/Suborned/Kit/Models/`, built by `tools/decokit`):

| # | Model | File | Triangles | Dot-matrix face | Height | In the game today |
|---|---|---|---|---|---|---|
| 1 | Butler | `Robots/DK_RobotButler.fbx` (+ `_Play`, `_Far`) | 16,216 (LODs 4,045 / 968) | yes, amber | 1.80 m | old-town street life |
| 2 | Porter | `Robots/DK_RobotPorter.fbx`, `DK_RobotPorter_Play.fbx` | 18,810 (4,639 / 1,122) | yes, amber | 1.56 m | **response units** (with a red lamp) and street life |
| 3 | Waiter | `Robots/DK_RobotWaiter.fbx` (+ `_Play`) | 15,324 (3,827 / 912) | yes, teal | 1.52 m | street life |
| 4 | Nanny | `Robots/DK_RobotNanny.fbx` (+ `_Play`) | 19,688 (4,921 / 1,174) | yes, teal | 1.46 m | street life |
| 5 | Shopkeeper | `Robots/DK_RobotShopkeeper.fbx` (+ `_Play`) | 14,642 (3,603 / 844) | yes, teal | 1.71 m | street life |
| 6 | Patrol robot | `Robots/DK_PatrolRobot.fbx` | 12,522 (3,066 / 740) | yes, 16 × 10, four attention stages | 1.38 m | **the patrol** |
| 7 | Service robot | `Vehicles/DK_ServiceRobot_Play.fbx` | 9,814 (2,452 / 588) | no: one camera-lens eye | 1.11 m | one in the old-town plaza |
| 8 | Civic robot dock | `Street/DK_CivicRobotDock.fbx` | 2,546 (558 / 129) | not a robot | 2.58 m | **the square's dock**, empty |

**In the Suborned store** (`~/Projects/the-suborned-store/testbed-art/`), never in the game:

| # | Model | File | Triangles | Dot-matrix face | Height | Used before |
|---|---|---|---|---|---|---|
| 9 | Concierge | `mobility/Robots/RFM_RobotConciergeV1.glb` | 15,264, no LODs | no: two lamp eyes and a smile on a black plate | 2.36 m | the old testbed's kiosk robot, drawn at 1.75 m |
| 10 | Parcel courier | `mobility/Robots/RFM_RobotParcelCourierV1.glb` | 9,052 | no: lamp eyes | 1.72 m | the old testbed's charging robot, drawn at 1.5 m |
| 11 | Tracked repair | `mobility/Robots/RFM_RobotTrackedRepairV1.glb` | 17,804 | no: lamp eyes | 2.00 m | never |
| 12 | Street sweeper | `mobility/Robots/RFM_RobotStreetSweeperV1.glb` | 6,500 | no: one lamp eye | 1.42 m | never |
| 13 | Robot service booth | `mobility/Vending/RFM_RobotServiceV1.glb` | 11,784 | no face: a booth, not a robot that moves | 2.48 m | never |
| 14 | Civic robot statue | `civic-street-props/civic-robot-v1.glb` (+ identical `web/` copy) | 3,970 | blank opal screen | 2.00 m with its 0.2 m plinth | the butler's far mesh (1) on a plinth: the old testbed's "civic steward monument" |

**The same designs, not drawn twice:**
- The `_Play` pieces are the same builder with LODs (`tools/decokit/play020.py`, `restore026.py`). `_Far` is the old 4k LOD.
- The old monorepo (`~/Projects/the-suborned/game/Assets/TheSuborned/Art/DecoKit/Models/Robots/DK_Robot*.fbx` and `Vehicles/DK_ServiceRobot.fbx`) has older exports of 1–5 and 7. The Nanny and every `_Far` file are byte-identical to the kit's.
- The store's `source-art/DecoKit/icons/Robot*.png` are icons of 1–5 and 7.

**Checked and left out:** `testbed-art/mobility/Ground/RFM_ServiceStreetSweeperV1.glb` is a 5.2 m sweeper truck with a cab (a vehicle). `RFM_PeopleMechanicV2` and `RFM_PeopleCourierV2` are people. The old `DK_Automat.fbx` is a food automat. `paid/` has no robot 3D jobs, only stills and sounds. The store's `characters/`, `library/` and `migration/` have no robots.

**Concepts on the board:** the store's `concepts/Props/sheets/robots_service.png` (1–5 came from it) and `robots_civic.png` (none built); `concepts/Robots/lean/` corp, service, robots and makers (comic, none built); `concepts/Robots/review/robots_comic_2026-10-02.jpg` (the four lean sheets stacked); this repo's `briefs/031/robot-face/` (the patrol robot, built as 6) and `paid/097-props/codex/civic-robot-dock.png` (the dock, empty by design, built as 8). The store's `concepts/Style/probes/2026-10-06_robots_*` style tests are not on the board.

## How it's drawn
- Every model gets the same light (the DecoKit studio HDRI and sun), the same Cycles settings and one fixed orthographic camera per view, so every tile has the same metres per pixel.
- **Studio rows:** three-quarter front (35° off the front, 12° down), true relative size. The grey figure is a 1.75 m person.
- **Play rows:** the play camera. Orthographic size 10 on a 1080 px tall screen is 54 px per metre, 38° down, 30° off the front. The person comes out about 82 px tall.
- Kit robots are built by `tools/decokit`, which is what the exporter writes. Their screens show each robot's calm dot-matrix face from the brief 031 tables; in the game the engine draws those faces. Store models are shown as made.

## Proposed, per role
1. **Response units: the patrol robot (6) with the red lamp they already carry.** It's the only robot we built for security (shield badge, baton, light bar) and the only dot-matrix face big enough to read at play zoom: about 26 × 15 px, against the porter's 18 × 7 and the butler's 10 × 3. It's lighter too: 12.5k triangles against the porter's 18.8k. The porter is a hotel porter pushing a luggage trolley into the fight.
2. **The patrol: keep the patrol robot (6).** It was built for this in brief 031, with the four-stage face. Patrol and response then share one silhouette, the city's police robot. The patrol walks alone; the response comes in numbers with lamps on.
3. **The square's dock: the patrol robot (6) waiting in it.** The engine sends response robots from the nearest dock (`sim/Suborned.Sim/GameResponse.cs:159`), so a robot waiting there shows the player where the response comes from. It fits the 0.98 m bay, and its face stays visible under the hood from the play camera. The other choice is the butler (1). It fits the dock's 1.8 m design (the plan's robot is 0.8 × 0.8 × 1.8 m), but from 38° up the hood hides its face.
4. **Old-town street life: keep 1–5 and 7.** They're the old town's own robots, each with one prop and a dot-matrix face. The porter goes back to carrying luggage only.

No store robot is proposed. As made, none meets GDD §15: they have lamp eyes instead of dot-matrix faces, none has LODs, and their finish is simpler than the kit's. Using one would need a dot-matrix screen over its black faceplate, LODs, and a set scale (the testbed drew 9 at 1.75 m and 10 at 1.5 m).

**Budget:** crowd figures are about 12k triangles at LOD0. At play zoom a robot is always drawn at LOD0, because it never shrinks below the 0.8 % screen-height switch (`play020.py`). The patrol robot (12.5k) is about on budget. The porter (18.8k), nanny (19.7k) and butler (16.2k) are over.

## Sky decides
1. **The response robots' look.** Recommended: the patrol robot with its red lamp, the same model as the patrol. If they should look different from the patrol, there are two ways. A darker paint on the same model is a small job. A new robot built from the civic sheet's beacon robot (`robots_civic.png`, top middle) is a new kit piece and a bigger job.
2. **Who stands in the square's dock.** Recommended: the patrol robot. The butler is the right height, but the hood hides its face from the play camera. Or leave the dock empty, as it is today. A robot that leaves the dock when the response is sent would be a behaviour change, for Pulse and Latch; it's not part of this pick.
3. **The store robots (9–14).** Recommended: leave them in the store. The courier (10) and sweeper (12) could join the old town later, once they have a dot-matrix face.

## For Latch, once Sky picks (not done; models only)
- Response: `host.robotPorter` → `Kit/Models/Robots/DK_PatrolRobot.fbx` (`PlayInstall.cs:122`). The lamp code in `HostViews.Drum` already sits on any model. The response faces then use the patrol robot's `M_RobotFace` stages, as the patrol does.
- Dock: a `PatrolRobot` (or `RobotButler_Play`) standing on the dock's charging pad, about 0.17 m up and its back about 0.38 m behind the dock's centre, as in `make/render.py` (`DockPatrol`).

## Remake
From the repo root, one Blender job at a time, $0:
```
python3 -I briefs/104/vela/robots/make/faces.py out/scratch/vela/robots/faces
B=/Applications/Blender.app/Contents/MacOS/Blender
$B -b -P briefs/104/vela/robots/make/render.py -- --view studio --out out/scratch/vela/robots/studio --faces out/scratch/vela/robots/faces --samples 64
$B -b -P briefs/104/vela/robots/make/render.py -- --view play --out out/scratch/vela/robots/play --faces out/scratch/vela/robots/faces --samples 128
python3 -I briefs/104/vela/robots/make/compose.py out/scratch/vela/robots briefs/104/vela/robots/board.png
```
`make/inspect.py` is the triangle and face-part count: `$B -b -P make/inspect.py -- <out.json> <files…>`. `make/board_data.py` holds the board's words.
