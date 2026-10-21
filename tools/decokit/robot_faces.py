"""The robots' face and glance data as JSON Latch can load (brief 026 round 1): python3 tools/decokit/robot_faces.py [--check]
Reads art/robots/RobotGlyphs.txt (the old project's glyph table, verbatim) and the rig table below (the old DecoKitRobots.cs numbers), writes
art/robots/robot-faces.json and, when DECOKIT_OUT / out/decokit exists, a copy under <out>/Data/ with the txt.

Section 15: robots are dot-matrix faces, "no cartoon eyes". The old table has glyphs that read as cartoon: `heart`, `star`, and the wink (two different
eyes at once). They are KEPT in the data, marked needsSky, and held out of each robot's default `moods` into `moodsHeld`, so nothing is dropped silently and
turning one on is a one-line move. `calm`, `happy` (arc eyes), `round`, `wide` and the mouths are plain LED shapes. The alarm face (angry_l / angry_r + grit)
is the red pulsing glare of an alarmed robot: functional, red is allowed on a small emissive display (section 10 / 15), kept."""

import json
import os
import shutil
import sys
from pathlib import Path

HERE = Path(__file__).parent
ART = HERE / "art/robots"
FLAG = {"heart": "a heart glyph is a cartoon sticker, not an LED shape (section 15: no cartoon eyes); the Nanny's old mood",
        "star": "a star glyph is a cartoon sticker (section 15: no cartoon eyes); the Porter's and Shopkeeper's old mood"}
WINK = "a wink is two different eyes at once, a cartoon expression (section 15: no cartoon eyes)"


def E(left, right=None, mouth=""):
    return dict(left=left, right=left if right is None else right, mouth=mouth)


def is_wink(e):
    return e["left"] != e["right"]


# Rig table of DecoKitRobots.cs: range / turn / hold / home = RobotIdle; grid / corner / eye / mouth / neutral / moods / angry = RobotFace.
RIGS = {
    "RobotButler": dict(face="DK_Face_Amber", range=35, turn=.7, hold=[2, 5], home=.5, grid=[21, 7], corner=1.7, eye=[5, .3], mouth=[0, 0], neutral=E("calm", None, ""),
                        angry=E("angry_l", "angry_r", ""), moods=[E("happy", None, ""), E("calm", "happy", "")]),
    "RobotPorter": dict(face="DK_Face_Amber", range=70, turn=.35, hold=[.8, 2.5], home=.3, grid=[30, 13], corner=2.3, eye=[6.8, 2.3], mouth=[0, -3.2], neutral=E("wide", None, "grin"),
                        angry=E("angry_l", "angry_r", "grit"), moods=[E("happy", None, "beam"), E("wide", None, "o"), E("star", None, "grin")]),
    "RobotWaiter": dict(face="DK_Face_Teal", range=50, turn=.4, hold=[1, 3], home=.35, grid=[15, 12], corner=5, eye=[3.5, 2.1], mouth=[0, -2.9], neutral=E("round", None, "smile"),
                        angry=E("angry_l", "angry_r", "grit"), moods=[E("happy", None, "grin"), E("round", "happy", "smile"), E("round", None, "o")]),
    "RobotNanny": dict(face="DK_Face_Teal", range=30, turn=.6, hold=[1.5, 4], home=.6, grid=[26, 15], corner=7, eye=[6, 1.8], mouth=[0, -3.4], neutral=E("happy", None, "smile"),
                       angry=E("angry_l", "angry_r", "grit"), moods=[E("heart", None, "smile"), E("round", None, "smile"), E("happy", None, "beam")]),
    "RobotShopkeeper": dict(face="DK_Face_Teal", range=55, turn=.45, hold=[1, 3], home=.4, grid=[24, 9], corner=1.6, eye=[5.3, 1.3], mouth=[0, -3.2], neutral=E("round", None, "smile"),
                            angry=E("angry_l", "angry_r", "grit"), moods=[E("happy", None, "beam"), E("round", "happy", "smile"), E("star", None, "smile")]),
}


def parse(text):
    glyphs, cur = {}, None
    for raw in text.split("\n"):
        line = raw.strip()
        if line.startswith("="):
            cur = line[1:].strip()
            glyphs[cur] = []
        elif line and not line.startswith(";") and cur:
            glyphs[cur].append(line)
    return glyphs


def attention(r):
    """The four attention stages of brief 031 as expressions of this robot's own glyphs (Sky 2026-10-21: the old robots keep their faces). RobotFace.cs takes `_Stage` 0..3 and
    resolves it here: calm = the rig's neutral; noticed = `wide` eyes (+ `o` when the rig has a mouth), eyes jump `gazeCells` toward the agent; wary = the alarm eyes
    (angry_l / angry_r), mouth dark; reporting = `alarm_s` centred, alarm red, flashing 2 Hz. Stage 0 blinks and breaks into moods as before; stages 1 to 3 hold still (no blink, no mood), as the old Alarm did."""
    has_mouth = r["mouth"] != [0, 0]
    n = r["neutral"]
    return dict(
        stages=[dict(stage=0, name="calm", left=n["left"], right=n["right"], mouth=n["mouth"], blink=True, moods=True, glow="own"),
                dict(stage=1, name="noticed", left="wide", right="wide", mouth="o" if has_mouth else "", gaze=True, glow="own"),
                dict(stage=2, name="wary", left="angry_l", right="angry_r", mouth="", gaze=True, glow="own"),
                dict(stage=3, name="reporting", left="", right="", mouth="alarm_s", mouthAt=[0, 0], glow="alarm", flashHz=2, on=.3, off=.2)],
        glowOptions=dict(own="the robot's own LED colour (DK_Face_Amber / DK_Face_Teal)", phosphor="#BFF3E6 x1.6, the patrol robot's seafoam; shown beside the own colour in briefs/031/robot-face", alarm="#FF5A3C x2.2"),
        gazeCells=2, dropAfterS=2.0)


def build():
    glyphs = parse((ART / "RobotGlyphs.txt").read_text())
    g_out = {}
    for name, rows in glyphs.items():
        kind = "mouth" if name in ("smile", "grin", "beam", "o", "grit", "alarm_s") else ("alarm-eye" if name.startswith("angry") else "eye")
        row = dict(rows=rows, w=max(len(r) for r in rows), h=len(rows), kind=kind, lit=sum(r.count("#") for r in rows))
        if name in FLAG:
            row.update(needsSky=True, why=FLAG[name])
        g_out[name] = row
    rigs = {}
    for name, r in RIGS.items():
        r = json.loads(json.dumps(r))
        keep, held = [], []
        for m in r["moods"]:
            why = [FLAG[g] for g in (m["left"], m["right"]) if g in FLAG] + ([WINK] if is_wink(m) else [])
            (held.append(dict(m, needsSky=True, why=why[0])) if why else keep.append(m))
        r["moods"], r["moodsHeld"] = keep, held
        r["attention"] = attention(r)
        rigs[name] = r
    return dict(
        schema="suborned.robotfaces/1",
        note="Faces for DK_RobotFace.shader (unity/Assets/Suborned/Shaders/DecoKit) driven by RobotFace.cs / RobotIdle.cs of the old project (Scripts/Robots), which Latch ports. "
             "Glyph atlas: RobotGlyphs.txt packed into 8 x 8 tiles in a row, r = lit, g = glyph height / 8, glyphs bottom-left, point filtered, global texture _DK_RobotGlyphs. "
             "Screens are the renderers named *_Face on the model (here DK_<Robot>_Play_Head_Face); heads are *_Head. grid / corner / eye / mouth are LED cells; eye = the viewer's-right eye "
             "centre from the grid centre, the left is its mirror. Idle: range deg, turn s, hold s, home = chance a glance returns ahead. Moods held out need Sky.",
        timing=dict(blinkEvery=[2.5, 5.5], moodEvery=[8, 18], moodHold=[1.4, 2.6], blinkHalf=.04, blinkShut=.07, doubleBlink=.26, alarmFlicker=.25, alarmFlickerHz=16, alarmPulseHz=1.5, alarmPulse=.25,
                    alarmGlowLinear=[2.6, .1, .04], lead=.15, gazeCells=2, overshoot=1.1, snap=.2, snapOvershoot=1.7, settle=.6, stareMax=120),
        shader=dict(ghost=.05, dot=.36, glowMaterials={"DK_Face_Amber": dict(hex="#F28A1E", strength=1.6), "DK_Face_Teal": dict(hex="#3FC9A0", strength=1.4)}),
        glyphs=g_out, robots=rigs,
        playPieces={k: f"{k}_Play" for k in RIGS})


def main():
    out = json.dumps(build(), indent=1) + "\n"
    path = ART / "robot-faces.json"
    if "--check" in sys.argv:
        sys.exit(0 if path.exists() and path.read_text() == out else 1)
    path.write_text(out)
    dst = Path(os.environ.get("DECOKIT_OUT") or HERE.parents[1] / "out/decokit") / "Data"
    dst.mkdir(parents=True, exist_ok=True)
    shutil.copy2(path, dst / path.name)
    shutil.copy2(ART / "RobotGlyphs.txt", dst / "RobotGlyphs.txt")
    print(f"[robot-faces] {len(RIGS)} robots, {len(build()['glyphs'])} glyphs -> {path} and {dst}")


main()
