"""Brief 104 (Vela): the board's words and numbers. Triangle counts are the files as the game or the store has them (Blender's count of the
FBX in unity/Assets/Suborned/Kit and of the store's glTF, inspect.py; LODs from contracts/world/kit.json). Heights are the rendered models'."""

from pathlib import Path

REPO = Path(__file__).resolve().parents[5]
STORE = Path.home() / "Projects/the-suborned-store"
INK, TEAL, DIM = (23, 48, 47), (28, 120, 108), (120, 128, 124)

MODELS = {
    "Person": dict(short="1.75 m person"),
    "RobotButler": dict(n=1, short="Butler", h="1.80 m", tris="16.2k tris", face="dot face, amber", dots=True, today=["STREET LIFE"],
                        where="Butler · Kit/Models/Robots/DK_RobotButler.fbx, 16,216 tris (+ DK_RobotButler_Play with LODs 4,045 / 968, _Far 3,970) · "
                              "tools/decokit/robots.py · the old monorepo's game/Assets/TheSuborned/Art/DecoKit/Models/Robots has the same five designs"),
    "RobotPorter": dict(n=2, short="Porter", h="1.56 m", tris="18.8k tris", face="dot face, amber", dots=True, today=["RESPONSE", "STREET LIFE"],
                        where="Porter · Kit/Models/Robots/DK_RobotPorter.fbx and DK_RobotPorter_Play.fbx, 18,810 tris (LODs 4,639 / 1,122) · "
                              "the response units draw the _Play piece with a red lamp on top (HostViews.cs:379, PlayInstall.cs:122)"),
    "RobotWaiter": dict(n=3, short="Waiter", h="1.52 m", tris="15.3k tris", face="dot face, teal", dots=True, today=["STREET LIFE"],
                        where="Waiter · Kit/Models/Robots/DK_RobotWaiter.fbx (+ _Play), 15,324 tris (LODs 3,827 / 912)"),
    "RobotNanny": dict(n=4, short="Nanny", h="1.46 m", tris="19.7k tris", face="dot face, teal", dots=True, today=["STREET LIFE"],
                       where="Nanny · Kit/Models/Robots/DK_RobotNanny.fbx (+ _Play), 19,688 tris (LODs 4,921 / 1,174)"),
    "RobotShopkeeper": dict(n=5, short="Shopkeeper", h="1.71 m", tris="14.6k tris", face="dot face, teal", dots=True, today=["STREET LIFE"],
                            where="Shopkeeper · Kit/Models/Robots/DK_RobotShopkeeper.fbx (+ _Play), 14,642 tris (LODs 3,603 / 844) · "
                                  "street life: DecoKitStreetLife.cs, DecoKitSouthsideLife.cs (1–5)"),
    "PatrolRobot": dict(n=6, short="Patrol robot", h="1.38 m", tris="12.5k tris", face="dot face, 16 × 10", dots=True, today=["PATROL"],
                        where="Patrol robot · Kit/Models/Robots/DK_PatrolRobot.fbx, 12,522 tris (LODs 3,066 / 740) · tools/decokit/robot_attention.py "
                              "(brief 031): the four-stage attention face"),
    "ServiceRobot": dict(n=7, short="Service robot", h="1.11 m", tris="9.8k tris", face="lens eye", dots=False, today=["OLD-TOWN PLAZA"],
                         where="Service robot · Kit/Models/Vehicles/DK_ServiceRobot_Play.fbx, 9,814 tris (LODs 2,452 / 588) · tools/decokit/vehicles.py · "
                               "one in the old-town plaza (DecoKitPlazaLayout.cs:156); it was the patrol before brief 031"),
    "CivicRobotDock": dict(n=8, short="Dock", h="2.58 m", tris="2.5k tris", face="not a robot", dots=None, today=["SQUARE'S DOCK"],
                           where="Civic robot dock · Kit/Models/Street/DK_CivicRobotDock.fbx, 2,546 tris (LODs 558 / 129) · tools/decokit/civic_props097.py "
                                 "(brief 097) · the bay inside is 0.98 × 1.0 × 2.14 m; nothing stands in it today"),
    "Concierge": dict(n=9, short="Concierge", h="2.36 m", tris="15.3k tris", face="lamp eyes", dots=False,
                      where="Concierge · testbed-art/mobility/Robots/RFM_RobotConciergeV1.glb, 15,264 tris, no LODs · two amber lamp eyes and a smile on "
                            "a black plate · the old testbed's kiosk robot (\"Ask the concierge\"), drawn there at 1.75 m"),
    "ParcelCourier": dict(n=10, short="Courier", h="1.72 m", tris="9.1k tris", face="lamp eyes", dots=False,
                          where="Parcel courier · testbed-art/mobility/Robots/RFM_RobotParcelCourierV1.glb, 9,052 tris · the old testbed's charging robot, "
                                "drawn there at 1.5 m"),
    "TrackedRepair": dict(n=11, short="Repair bot", h="2.00 m", tris="17.8k tris", face="lamp eyes", dots=False,
                          where="Tracked repair · testbed-art/mobility/Robots/RFM_RobotTrackedRepairV1.glb, 17,804 tris · never used"),
    "StreetSweeper": dict(n=12, short="Sweeper", h="1.42 m", tris="6.5k tris", face="one lamp eye", dots=False,
                          where="Street sweeper · testbed-art/mobility/Robots/RFM_RobotStreetSweeperV1.glb, 6,500 tris · never used"),
    "RobotService": dict(n=13, short="Service booth", h="2.48 m", tris="11.8k tris", face="no face", dots=False,
                         where="Robot service booth · testbed-art/mobility/Vending/RFM_RobotServiceV1.glb, 11,784 tris · a booth with a hatch, not a robot "
                               "that moves · never used"),
    "CivicRobot": dict(n=14, short="Civic statue", h="2.00 m", tris="4.0k tris", face="blank opal face", dots=False,
                       where="Civic robot statue · testbed-art/civic-street-props/civic-robot-v1.glb (+ identical web/ copy), 3,970 tris · the butler's far "
                             "mesh (1) on a 0.2 m plinth; the old testbed's \"civic steward monument\""),
    "PorterLamp": dict(short="Porter + lamp"), "PatrolLamp": dict(short="Patrol + lamp"), "DockPatrol": dict(short="Dock + patrol"), "DockButler": dict(short="Dock + butler"),
}

KIT_ROWS = [["Person", "RobotButler", "RobotPorter", "RobotWaiter", "RobotNanny", "RobotShopkeeper"],
            ["Person", "PatrolRobot", "ServiceRobot", "CivicRobotDock"]]
STORE_ROWS = [["Person", "Concierge", "ParcelCourier", "TrackedRepair"],
              ["Person", "StreetSweeper", "RobotService", "CivicRobot"]]
PLAY_ROWS = [["Person", "RobotButler", "RobotPorter", "RobotWaiter", "RobotNanny", "RobotShopkeeper", "PatrolRobot", "ServiceRobot", "CivicRobotDock"],
             ["Person", "Concierge", "ParcelCourier", "TrackedRepair", "StreetSweeper", "RobotService", "CivicRobot"]]

C = STORE / "concepts"
CONCEPTS = [  # (path, caption, crop as fractions or None)
    (C / "Props/sheets/robots_service.png", "Store · Props/sheets/robots_service.png. The service sheet: 1–5 were built from it.", None),
    (C / "Props/sheets/robots_civic.png", "Store · Props/sheets/robots_civic.png. Sweeper, beacon robot, signal unicycle, walker, gardener, saucer. None built.", None),
    (C / "Robots/lean/corp__codex.png", "Store · Robots/lean/corp__codex.png. Patrols, enforcers and watchers (comic). None built.", None),
    (C / "Robots/lean/service__codex.png", "Store · Robots/lean/service__codex.png. Service robots (comic). None built.", None),
    (C / "Robots/lean/robots__codex.png", "Store · Robots/lean/robots__codex.png. City robots, open brief (comic). None built.", None),
    (C / "Robots/lean/makers__codex.png", "Store · Robots/lean/makers__codex.png. Six rival makers (comic). None built.", None),
    (C / "Robots/review/robots_comic_2026-10-02.jpg", "Store · Robots/review/robots_comic_2026-10-02.jpg: the four comic sets, stacked for review (top shown).", (0, 0, 1, 0.24)),
    (REPO / "briefs/031/robot-face/robot-face-board.png", "This repo · briefs/031/robot-face: the patrol robot's attention face, designed here, built as 6.", (0.685, 0.035, 1.0, 0.505)),
    (REPO / "paid/097-props/codex/civic-robot-dock.png", "This repo · paid/097-props/codex/civic-robot-dock.png: the dock, empty by design, built as 8.", (0, 0.1, 1, 0.9)),
]

ROLES = [
    dict(title="Response units",
         what="Sent as the Alert rises. They set out from the nearest dock and come for your agents.",
         panels=[("TODAY", ["PorterLamp"], "Porter (2) + red lamp", INK), ("PROPOSED", ["PatrolLamp"], "Patrol robot (6) + red lamp", TEAL)],
         why="The only robot we built for security: shield badge, baton, light bar, and the one dot-matrix face big enough to read at play zoom (about 26 × 15 px; "
             "the porter's is about 18 × 7). Lighter too: 12.5k triangles against the porter's 18.8k. The porter pushes a luggage trolley into the fight."),
    dict(title="The patrol",
         what="Walks its round alone and sees only the way its face points.",
         panels=[("TODAY = PROPOSED", ["PatrolRobot"], "Patrol robot (6): keep", TEAL)],
         why="Built for this job in brief 031, with its four-stage face (calm, noticed, wary, reporting). Patrol and response then share one silhouette: "
             "the city's police robot. The patrol is the one walking alone; the response comes in numbers, lamps on."),
    dict(title="The square's dock",
         what="The response's robots set out from the nearest dock (the engine's rule). Today the dock stands empty.",
         panels=[("TODAY", ["CivicRobotDock"], "Empty dock (8)", INK), ("PROPOSED", ["DockPatrol"], "Patrol robot (6) waiting in it", TEAL),
                 ("OR", ["DockButler"], "Butler (1) in it", DIM)],
         why="A robot waiting in its dock shows where the response comes from. The patrol robot fits the 0.98 m bay and its face clears the hood from "
             "the play camera. The butler fits the dock's 1.8 m height, but the hood hides its face from 38° up."),
    dict(title="Old-town street life",
         what="Fills the old town with robots at work. They never watch or report.",
         panels=[("TODAY = PROPOSED", ["RobotButler", "RobotPorter", "RobotWaiter", "RobotNanny", "RobotShopkeeper", "ServiceRobot"], "1–5 and 7: keep", TEAL)],
         zoom=1.5,
         why="The old town's own robots, each with one prop (tray, trolley, cloche, pram, shop case) and a dot-matrix face. The porter goes back to "
             "luggage only. The service robot (7) has a camera-lens eye, not a dot face: a small keep-or-swap call."),
]

DECIDE = [
    "Response robots: the patrol robot with its red lamp (recommended), the same model as the patrol. Or should they look different from the "
    "patrol? Then a darker paint on the same model (small job), or a new robot built from the civic sheet's beacon robot (a new kit piece, bigger job).",
    "The square's dock: the patrol robot waiting in it (recommended), or the butler (right height, but the hood hides its face from the play "
    "camera), or leave it empty as today?",
    "Store robots 9–14: none fits a role as made (lamp eyes instead of dot-matrix faces, no LODs, a simpler finish than the kit's). Recommended: "
    "leave them in the store. The courier (10) and sweeper (12) could join the old town later with a dot face.",
]
