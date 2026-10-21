"""Brief 031 (Vela): the robot face boards. python3 tools/decokit/compose031face.py faces|board [--dir briefs/031/robot-face]
faces: the dot-matrix face textures the 3D render (render031face.py) puts on the display: proposed_<stage>.png (16 x 10, the old glyphs) and old_<stage>.png (the old Waiter's 15 x 12 grid
with the old expressions), plus glyph_<stage>.png (Glyph's own bitmaps from briefs/031/cues.html) and the flat comparison strips.
board: robot-face-board.png and robot-face.html (old | Glyph | proposed, per stage, at play zoom (true 1x and 3x) and close)."""

import base64
import json
import re
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).parent
sys.path.insert(0, str(HERE))
import robot_face_stage as RF  # noqa: E402

REPO = HERE.parents[1]
DIR = Path(sys.argv[sys.argv.index("--dir") + 1]) if "--dir" in sys.argv else REPO / "briefs/031/robot-face"
STAGES = ["calm", "noticed", "wary", "reporting"]
CUES = REPO / "briefs/031/cues.html"
DATA = json.loads((HERE / "art/robots/robot-faces.json").read_text())
ROBOTS = ["RobotWaiter", "RobotPorter", "RobotShopkeeper", "RobotButler"]  # Sky 2026-10-21: the old robots keep their faces; the Nanny is not in the callout list
OWN = {"DK_Face_Amber": "#F28A1E", "DK_Face_Teal": "#3FC9A0"}
SEA, ALARM = "#BFF3E6", "#FF5A3C"


def old_grid(robot, stage):
    """A robot's face at an attention stage, from robot-faces.json (the old rig table + glyph table, and its `attention` stages), as the old RobotFace.Place puts it."""
    rig = DATA["robots"][robot]
    gw, gh = rig["grid"]
    st = rig["attention"]["stages"][stage]
    g = [[0] * gw for _ in range(gh)]

    def put(name, at):
        if not name:
            return
        rows = DATA["glyphs"][name]["rows"]
        w, h = max(len(x) for x in rows), len(rows)
        x = int((gw * .5 + abs(at[0]) - w * .5 + .5) // 1)
        if at[0] < 0:
            x = gw - w - x
        y = int((gh * .5 + at[1] - h * .5 + .5) // 1)
        for ry, row in enumerate(rows):
            for cx, ch in enumerate(row):
                if ch == "#" and 0 <= x + cx < gw and 0 <= gh - 1 - (y + h - 1 - ry) < gh:
                    g[gh - 1 - (y + h - 1 - ry)][x + cx] = 1
    put(st["left"], (-rig["eye"][0], rig["eye"][1]))
    put(st["right"], tuple(rig["eye"]))
    put(st["mouth"], tuple(st.get("mouthAt", rig["mouth"])))
    return g


def dots_any(g, col, px=24, k=4):
    gw, gh = len(g[0]), len(g)
    big = Image.new("RGB", (gw * px * k, gh * px * k), "#0D1F1D")
    d = ImageDraw.Draw(big)
    c = tuple(int(col[j:j + 2], 16) for j in (1, 3, 5))
    b = (13, 31, 29)
    gc = tuple(int(b[j] + (c[j] - b[j]) * .05) for j in range(3))
    r = .36 * px * k
    for y, row in enumerate(g):
        for x, v in enumerate(row):
            cx, cy = (x + .5) * px * k, (y + .5) * px * k
            d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=c if v else gc)
    return big.resize((gw * px, gh * px), Image.LANCZOS)


def glyph_grid(stage):
    """Glyph's own bitmaps, parsed from the cue board (BM.calm / noticed / wary / alarm), 16 x 10."""
    txt = CUES.read_text()
    bm = re.search(r"const BM=\{(.*?)\n\};", txt, re.S).group(1)
    out = {}
    for k, body in re.findall(r"(\w+):\[(.*?)\]", bm, re.S):
        out[k] = [[1 if c == "#" else 0 for c in row] for row in re.findall(r'"([.#]+)"', body)]
    return out[{"calm": "calm", "noticed": "noticed", "wary": "wary", "reporting": "alarm"}[stage]]


def faces():
    (DIR / "faces").mkdir(parents=True, exist_ok=True)
    for i, st in enumerate(STAGES):
        col = RF.HEX["alarm"] if i == 3 else RF.HEX["phosphor"]
        RF.dots(RF.lit(i), 24, col).save(DIR / "faces" / f"PatrolRobot_{st}.png")
        RF.dots(glyph_grid(st), 24, col).save(DIR / "faces" / f"glyph_{st}.png")
        for rb in ROBOTS:
            own = OWN[DATA["robots"][rb]["face"]]
            g = old_grid(rb, i)
            dots_any(g, ALARM if i == 3 else own).save(DIR / "faces" / f"{rb}-own_{st}.png")
            dots_any(g, ALARM if i == 3 else SEA).save(DIR / "faces" / f"{rb}-sea_{st}.png")
    print(f"[compose031] faces -> {DIR / 'faces'}")


def font(sz, bold=False):
    for p in ("/System/Library/Fonts/Helvetica.ttc", "/System/Library/Fonts/HelveticaNeue.ttc"):
        try:
            return ImageFont.truetype(p, sz, index=1 if bold else 0)
        except OSError:
            pass
    return ImageFont.load_default()


def crop(im, box):
    return im.crop(box)


def board():
    F = DIR / "frames"
    cw = 330
    rows = []
    bg = (233, 224, 198)
    W = 160 + 3 * (cw * 2 + 30)
    H = 70 + 4 * (cw + 20) + 20
    im = Image.new("RGB", (W, H), bg)
    d = ImageDraw.Draw(im)
    d.text((14, 12), "Robot attention faces: old robot | Glyph's spec | patrol robot (all from the old glyph table)", fill=(18, 48, 45), font=font(22, True))
    heads = ["old robot (Waiter, own colour, old glyphs + new stages)", "Glyph (cues.html 4: flat plate)", "patrol robot (PatrolRobot, 16 x 10, seafoam)"]
    for ci, h in enumerate(heads):
        d.text((160 + ci * (cw * 2 + 30), 44), h, fill=(18, 48, 45), font=font(16, True))
    for ri, st in enumerate(STAGES):
        y0 = 70 + ri * (cw + 20)
        d.text((14, y0 + cw // 2 - 20), f"{ri} {st}", fill=(18, 48, 45), font=font(20, True))
        for ci, tag in enumerate(["RobotWaiter-own", "glyph", "PatrolRobot"]):
            x0 = 160 + ci * (cw * 2 + 30)
            if tag == "glyph":
                f = Image.open(DIR / "faces" / f"glyph_{st}.png")
                f = f.resize((int(cw * f.width / f.height), cw), Image.LANCZOS)
                im.paste(f, (x0, y0))
                # Glyph's true-size claim: a 26 x 16 px plate
                t = Image.open(DIR / "faces" / f"glyph_{st}.png").resize((26, 16), Image.BOX).resize((78, 48), Image.NEAREST)
                im.paste(t, (x0 + cw * 2 - 90, y0 + cw - 50))
                d.text((x0 + cw * 2 - 110, y0 + cw - 66), "26 x 16 px, x3", fill=(18, 48, 45), font=font(11))
            else:
                play = Image.open(F / f"{tag}_{st}_play.png").convert("RGB")
                close = Image.open(F / f"{tag}_{st}_close.png").convert("RGB")
                pc = play.crop((100, 80, 220, 200)).resize((cw, cw), Image.NEAREST)  # 120 px of true 1x, shown at 2.75x, nearest
                im.paste(pc, (x0, y0))
                im.paste(close.resize((cw, cw), Image.LANCZOS), (x0 + cw + 10, y0))
                t = play.crop((130, 110, 190, 170))
                im.paste(t, (x0 + 4, y0 + cw - 64))
                d.text((x0 + 70, y0 + cw - 18), "true 1x", fill=(18, 48, 45), font=font(11))
    im.save(DIR / "robot-face-board.png", optimize=True)
    b64 = base64.b64encode((DIR / "robot-face-board.png").read_bytes()).decode()
    (DIR / "robot-face.html").write_text(f'<!doctype html><meta charset="utf-8"><title>Brief 031 robot face</title><body style="margin:0;background:#efe7d3"><img src="data:image/png;base64,{b64}" style="max-width:100%"></body>')
    print(f"[compose031] board -> {DIR / 'robot-face-board.png'}")


def robots_board():
    """Every old robot through the four stages (close, own colour), then the Waiter and the Butler in their own colour against the patrol robot's seafoam, close and true 1x."""
    F = DIR / "frames"
    cw = 300
    rows = [(f"{rb}-own", rb) for rb in ROBOTS] + [("RobotWaiter-sea", "RobotWaiter seafoam"), ("RobotButler-sea", "RobotButler seafoam"), ("RobotPorter-sea", "RobotPorter seafoam"), ("RobotShopkeeper-sea", "RobotShopkeeper seafoam")]
    im = Image.new("RGB", (190 + 4 * (cw + 8), 60 + len(rows) * (cw + 8)), (233, 224, 198))
    d = ImageDraw.Draw(im)
    d.text((14, 12), "Old robots through the four attention stages: own LED colour (rows 1-4) and the patrol robot's seafoam (rows 5-8); true 1x inset", fill=(18, 48, 45), font=font(20, True))
    for ci, st in enumerate(STAGES):
        d.text((190 + ci * (cw + 8), 38), f"{ci} {st}", fill=(18, 48, 45), font=font(16, True))
    for ri, (tag, label) in enumerate(rows):
        y0 = 60 + ri * (cw + 8)
        d.text((12, y0 + cw // 2 - 8), label, fill=(18, 48, 45), font=font(15, True))
        for ci, st in enumerate(STAGES):
            x0 = 190 + ci * (cw + 8)
            im.paste(Image.open(F / f"{tag}_{st}_close.png").convert("RGB").resize((cw, cw), Image.LANCZOS), (x0, y0))
            im.paste(Image.open(F / f"{tag}_{st}_play.png").convert("RGB").crop((130, 90, 190, 190)), (x0 + 4, y0 + cw - 104))
    im.save(DIR / "old-robots-board.png", optimize=True)
    print(f"[compose031] old-robots-board -> {DIR}")


if __name__ == "__main__":
    {"faces": faces, "board": board, "robots": robots_board}[sys.argv[1]]()
