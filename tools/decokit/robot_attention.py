"""The patrol robot (brief 031; Glyph's cue board section 4, Vela; Sky 2026-10-21: the new head becomes the patrol robot): a tracked civic patrol robot whose big head carries the four-state dot-matrix face, `PatrolRobot`.
The face is the old city's (robot_face_stage.py: RobotGlyphs.txt placed on a 16 x 10 grid); the model carries the dark glass and a UV'd sheet in M_RobotFace, and the engine
draws the stage (_Stage 0 calm, 1 noticed, 2 wary, 3 reporting, DK_RobotFaceStage.shader proposal in briefs/031/robot-face/).

Glyph's proportions at play zoom: a barrel body under a head 56 wide by 44 high (here 0.56 x 0.44 m), the display plate 48 x 30 (0.48 x 0.30 m, 0.86 of the head), 16 x 10 dots
at 3 cm. At the play camera (pitch 38) a vertical face loses a fifth of its height and the head hides the top of the glass, so the display is tilted back 16 degrees: its normal
leans toward the camera, the plate keeps more of its rows. The head is a movable part (Head > Face) so Forma's tracking turns it and the sway of the wary stage is a head yaw.
Front at -Y, resting on z = 0, 1.2 m tall, the kit livery: cream and ivory enamel, a seafoam skirt, chrome, a coral accent."""

import math

from geo import dome
from kit import asset
from mats import RobotPal, get, register

pal = RobotPal()
from robot_parts import antenna, arm, badge, lights, profile_rad, rivets, screen, shoulder, vents, vseam
from signals import ball, band, puck, soft
from vehicles_parts import lamp, wheel

register("M_RobotFace", "face", "#BFF3E6", 1.6)  # the attention face: pale seafoam phosphor, alarm #FF5A3C at stage 3 is the shader's; clear in Blender, drawn by the engine

PLATE = (0.48, 0.30)       # Glyph: 48 x 30 head units on a 56 x 44 head, at 1 unit = 1 cm
HEAD = (0.56, 0.34, 0.44)  # width, depth, height
HEAD_Z = 0.70
TILT = -16.0               # deg about X: negative leans the front up, toward a camera above the street

BODY = [(0.33, 0.28), (0.36, 0.36), (0.37, 0.48), (0.33, 0.62), (0.2, 0.69)]


@asset("PatrolRobot", "Robots", bevel=0, lod=4000)
def patrol_robot(s):
    """Tracked civic patrol robot: rubber treads with a seafoam deck and amber running lights, a cream barrel with a charcoal chest plate, a brass shield badge and a coral sash, bolted
    shoulders, short bellows arms (one carries a chrome baton), a radio aerial; a big ivory head (0.56 x 0.44 m) on a chrome turret ring with a roof light bar of amber lamps and a tilted
    16 x 10 dot-matrix display. Head > Face are movable parts (the head yaw tracks the agent, the face is the attention stage)."""
    for sg in (-1, 1):
        s.rbox((sg * 0.3 - 0.1, -0.38, 0.0), (sg * 0.3 + 0.1, 0.38, 0.26), 0.12, pal.Rubber)
        for k in range(15):  # track cleats across the tread's top
            y = -0.3 + 0.6 * k / 14
            s.box((sg * 0.3 - 0.1, y - 0.012, 0.25), (sg * 0.3 + 0.1, y + 0.012, 0.27), pal.Rubber)
        for y in (-0.26, 0.0, 0.26):
            wheel(s, (sg * 0.405, y, 0.13), 0.08, 0.03, rin=0.05, hub=0.5, seg=10, side=sg)
    s.rbox((-0.24, -0.34, 0.1), (0.24, 0.34, 0.31), 0.05, pal.Enamel_Seafoam)
    s.rbox((-0.23, -0.37, 0.12), (0.23, -0.33, 0.17), 0.018, pal.Chrome)  # bumper
    for x in (-0.12, 0.0, 0.12):
        lamp(s, (x, -0.34, 0.25), (0, -1, 0), 0.015, lens=pal.Signal_Amber, depth=0.01, seg=8)
    s.lathe(soft(BODY, 0.02), pal.Enamel_Cream, seg=32, cap_bottom=False, cap_top=False)
    br = profile_rad(BODY)
    band(s, 0.345, 0.365, 0.3, 0.34, pal.Chrome, seg=32)
    band(s, 0.37, 0.382, 0.52, 0.55, pal.Enamel_Coral, seg=32)  # sash
    rivets(s, br, 0.58, 14, r=0.007)
    s.rbox((-0.15, -0.375, 0.37), (0.15, -0.33, 0.51), 0.02, pal.Enamel_Charcoal)  # chest plate
    badge(s, (0.07, -0.386, 0.45), (0, -1, 0), 0.04)                            # the shield badge, small brass
    vents(s, -0.1, -0.03, -0.385, 0.38, 3, h=0.01, gap=0.035)
    for a in (-45, -135, 45, 135):
        vseam(s, br, 0.4, 0.58, a)
    s.lathe([(0.0, 0.66), (0.17, 0.66), (0.17, 0.72), (0.0, 0.72)], pal.Chrome, seg=24)  # turret ring
    for sg in (-1, 1):
        shoulder(s, (sg * 0.37, 0, 0.54), sg, 0.055)
    arm(s, [(0.38, 0, 0.54), (0.44, -0.05, 0.45), (0.38, -0.2, 0.46)], curl=0.5)
    arm(s, [(-0.38, 0, 0.54), (-0.44, -0.05, 0.45), (-0.38, -0.2, 0.46)], curl=0.8)
    s.sweep([(-0.38, -0.2, 0.46), (-0.38, -0.46, 0.56)], 0.016, pal.Chrome, seg=8)  # the baton
    w, d, h = HEAD
    hz = HEAD_Z + 0.02
    with s.part("Head") as hd:  # turns on the turret ring
        hd.rbox((-w / 2, -d / 2, hz), (w / 2, d / 2, hz + h), 0.075, pal.Enamel_Ivory)
        hd.rbox((-w / 2 + 0.05, -d / 2 - 0.004, hz + h - 0.045), (w / 2 - 0.05, d / 2 - 0.05, hz + h + 0.012), 0.02, pal.Enamel_Seafoam)  # brow plate
        hd.rbox((-0.2, -0.06, hz + h + 0.01), (0.2, 0.06, hz + h + 0.04), 0.012, pal.Chrome)  # light bar base
        for x in (-0.16, -0.08, 0.0, 0.08, 0.16):
            lamp(hd, (x, 0.0, hz + h + 0.04), (0, 0, 1), 0.022, lens=pal.Signal_Amber, depth=0.015, seg=10)
        with hd.at((0, -d / 2, hz + 0.215), (TILT, 0, 0)):  # the display plate, tilted toward the camera
            hd.rbox((-0.275, -0.034, -0.175), (0.275, 0.03, 0.175), 0.035, pal.Chrome)           # bezel
            hd.rbox((-0.25, -0.044, -0.155), (0.25, -0.01, 0.155), 0.018, pal.Bakelite)           # dark glass, 0.5 x 0.31 m under the 0.48 x 0.30 face
            with hd.part("Face") as f:
                screen(f, lambda x, z: (x, -0.0455, z), PLATE[0], PLATE[1], 0.0, get("M_RobotFace"), nu=16, nv=10)
        for sg in (-1, 1):
            with hd.at((sg * w / 2, 0, hz + h * 0.5), (0, sg * 90, 0)):
                hd.lathe(puck(0.05, -0.006, 0.022, 0.006), pal.Chrome, seg=14)  # ear caps
        antenna(hd, (0.22, 0.05, hz + h), 0.2, (0.02, 0.0), tip=pal.Enamel_Coral)
